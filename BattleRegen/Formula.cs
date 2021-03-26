using HarmonyLib;
using MCM.Abstractions.Dropdown;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    /// <summary>
    /// Represents a formula used by the mod for health regen calculation. When the game starts, Battle Regeneration will search for
    /// all *.battleregen.cs files inside the ModuleData folder of every mod, if it exists, and then compile and load them,
    /// constructing a single instance of each class that derives from <see cref="Formula"/> for use by the main mod.
    /// </summary>
    public abstract class Formula : IComparable<Formula>
    {
        private static readonly string ModulesPath = System.IO.Path.Combine(BasePath.Name, "Modules");

        private static List<ModuleInfo> Modules { get; } = new List<ModuleInfo>();

        private static readonly HashSet<string> loadedFiles = new HashSet<string>();

        private static List<Formula> formulas = InitializeFormulas();

        /// <summary>
        /// Returns a list of formulas loaded by the mod.
        /// </summary>
        public static IEnumerable<Formula> Formulas => formulas.ToList();

        /// <summary>
        /// The name of the formula.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The ID of the formula.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// The priority of the formula in a list of <see cref="Formula"/>s. A <see cref="Formula"/> with a lower
        /// <see cref="Priority">Priority</see> is placed ahead in such a list. The default value is 0.
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Returns the localized name of the formula.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new TextObject(Name).ToString();
        }

        /// <summary>
        /// Performs calculation with this formula, given regeneration data, and returns a value. Subclasses should provide their own implementation of this method,
        /// for this method returns zero by default.
        /// </summary>
        /// <param name="data">The regeneration data information. See <see cref="RegenDataInfo"/>.</param>
        /// <returns>The modified regeneration rate.</returns>
        public virtual double Calculate(RegenDataInfo data)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return Calculate(data.Agent, data.RegenRate, data.OriginalRegenTime);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// <inheritdoc cref="Calculate(RegenDataInfo)"/>
        /// <para />
        /// Obsoleted in v1.2.4; override <see cref="Calculate(RegenDataInfo)"/> instead. This method only remains for compatibility reasons.
        /// </summary>
        /// <param name="agent">The agent whose stat will be used for calculation.</param>
        /// <param name="regenRate">The regeneration rate for the specified agent.</param>
        /// <param name="regenTime">The time it takes for the specified agent to be fully healed from zero.</param>
        /// <returns>The modified regeneration rate.</returns>
        /// <seealso cref="Calculate(RegenDataInfo)"/>
        [Obsolete("Superseded by Calculate(RegenDataInfo); override that method instead.")]
        public virtual double Calculate(Agent agent, double regenRate, double regenTime)
        {
            return 0.0;
        }

        /// <summary>
        /// Compares the current formula with the other formula. Formulas are sorted first by Priority and then by Id.
        /// </summary>
        /// <param name="other">The object to be compared to.</param>
        /// <returns>A value indicating the current object's relative position to the other.</returns>
        public int CompareTo(Formula other)
        {
            int comparison = Priority.CompareTo(other.Priority);
            return comparison == 0 ? Id.CompareTo(other.Id) : comparison;
        }

        #region internal methods
        internal static DropdownDefault<Formula> GetFormulas()
        {
            return new DropdownDefault<Formula>(formulas, 0);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("list_scripts", "battleregen")]
        internal static string ListScripts(List<string> strings)
        {
            return loadedFiles.Join(delimiter: "\n");
        }

        private static void CompileCode(FileInfo codeFile, Func<FileInfo, Compilation> function)
        {
            using (var stream = new MemoryStream())
            {
                var results = function(codeFile).Emit(stream);
                var diagnostics = results.Diagnostics;
                diagnostics.Do(x => Debug.Print($"[BattleRegen] {x}"));

                if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error || x.IsWarningAsError))
                {
                    Debug.Print($"[BattleRegen] Compilation of {codeFile.FullName} failed. See above for details.");
                    return;
                }
                else
                    Debug.Print($"[BattleRegen] Compilation of {codeFile.FullName} generated warnings. See above for details.");
                Debug.Print($"[BattleRegen] {codeFile.FullName} compiled successfully.");

                stream.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(stream.ToArray());
                loadedFiles.Add(codeFile.FullName);
                var types = assembly.GetTypes().Where(x => typeof(Formula).IsAssignableFrom(x));

                if (types.IsEmpty())
                    Debug.Print($"[BattleRegen] No class derived from {typeof(Formula).FullName} exists from {codeFile.FullName}.");
                else types.Do(x => AddFormula(x));
            }
        }

        private static Compilation GenerateCSharpCode(FileInfo codeFile)
        {
            using (var stream = codeFile.OpenRead())
            {
                var codestr = SourceText.From(stream);
                var options = CSharpParseOptions.Default.WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7_3);
                var syntaxTree = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(codestr, options);
                return CSharpCompilation.Create(System.IO.Path.GetRandomFileName(), new[] { syntaxTree },
                    GetReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
        }

        private static Compilation GenerateVisualBasicCode(FileInfo codeFile)
        {
            using (var stream = codeFile.OpenRead())
            {
                var codestr = SourceText.From(stream);
                var options = VisualBasicParseOptions.Default.WithLanguageVersion(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic16);
                var syntaxTree = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(codestr, options);
                return VisualBasicCompilation.Create(System.IO.Path.GetRandomFileName(), new[] { syntaxTree },
                    GetReferences(), new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).Select(x => x.Location).Where(x => !x.IsEmpty())
                .Select(x => MetadataReference.CreateFromFile(x));
        }

        internal static List<Formula> InitializeFormulas()
        {
            // Shamelessly copied from Custom Troop Upgrades because it's my mod
            if (!Modules.IsEmpty()) Modules.Clear();
            
            string[] moduleNames = Utilities.GetModulesNames();
            foreach (string moduleName in moduleNames)
            {
                ModuleInfo m = new ModuleInfo();
                m.LoadWithFullPath(ModuleHelper.GetModuleFullPath(moduleName));

                if (m.Id == "BattleRegeneration") Modules.Insert(0, m); // original mod should load first
                else Modules.Add(m);
            }

            formulas = new List<Formula>();
            CompileScripts("battleregen.cs", GenerateCSharpCode);
            CompileScripts("battleregen.vb", GenerateVisualBasicCode);
            return formulas;
        }
        #endregion

        /// <summary>
        /// Compiles scripts from source files with a given extension and the compilation function. If a directory is specified, source files
        /// from said directory will be compiled. Otherwise, source files from ModuleData folders of all modules will be loaded.
        /// </summary>
        /// <param name="extension">The extension of source files.</param>
        /// <param name="function">The function used to compile source files.</param>
        /// <param name="sourcePath">Directory of source files. If specified, source files from the directory will be compiled. Otherwise,
        /// source files from ModuleData folders of all modules will be loaded.</param>
        public static void CompileScripts(string extension, Func<FileInfo, Compilation> function, DirectoryInfo sourcePath = default)
        {
            Debug.Print($"[BattleRegen] Compiling formulas from source files of extension '{extension}'. This could take a while.");
            if (sourcePath != default && sourcePath.Exists)
            {
                Debug.Print($"[BattleRegen] Loading source files from {sourcePath.FullName}");
                sourcePath.EnumerateFiles($"*.{extension}").Where(x => !loadedFiles.Contains(x.FullName)).Do(x => CompileCode(x, function));
            }
            else
            {
                Debug.Print("[BattleRegen] Loading source files from the ModuleData folders (if available) of all modules");
                foreach (ModuleInfo module in Modules)
                {
                    DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Id, "ModuleData"));
                    if (dataPath.Exists)
                        dataPath.EnumerateFiles($"*.{extension}").Where(x => !loadedFiles.Contains(x.FullName)).Do(x => CompileCode(x, function));
                }
            }
            formulas.Sort();
            Debug.Print($"[BattleRegen] Loaded regeneration formulas from source files of extension '{extension}'. See mod entry in MCM for details.");
        }

        /// <summary>
        /// Adds a formula, given the type parameter. If two formulas with the same <see cref="Id"/> exists, the one being added will replace the other.
        /// </summary>
        /// <typeparam name="T">A subtype of <see cref="Formula"/>.</typeparam>
        /// <returns>Whether the addition is successful.</returns>
        /// <seealso cref="AddFormula(Type)"/>
        public static bool AddFormula<T>() where T : Formula => AddFormula(typeof(T));

        /// <inheritdoc cref="AddFormula{T}"/>
        /// <seealso cref="AddFormula{T}"/>
        public static bool AddFormula(Type type)
        {
            try
            {
                if (typeof(Formula).IsAssignableFrom(type) && !formulas.Any(x => x.GetType() == type))
                {
                    ConstructorInfo constructor = AccessTools.Constructor(type, Array.Empty<Type>());
                    if (constructor != null)
                    {
                        Formula formula = constructor.Invoke(Array.Empty<object>()) as Formula;
                        formulas.RemoveAll(x => x.Id == formula.Id); // remove old version before adding new version
                        formulas.Add(formula);
                        return true;
                    }
                    Debug.Print($"[BattleRegen] No constructor from {type.FullName} exists that take zero parameters");
                }
                else Debug.Print($"[BattleRegen] {type.FullName} is not derived from Formula or is already added");
            }
            catch (Exception e)
            {
                string error = $"[BattleRegen] Failed to add an instance of {type.FullName} as a formula due to an exception.\n{e}";
                Debug.Print(error);
                InformationManager.DisplayMessage(new InformationMessage(error));
            }

            return false;
        }
    }
}
