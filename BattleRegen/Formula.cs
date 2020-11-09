using HarmonyLib;
using MCM.Abstractions.Dropdown;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
        /// Performs calculation with this formula, given the parameters, and returns a value. Subclasses must provide their own implementations of this method.
        /// </summary>
        /// <param name="agent">The agent whose stat will be used for calculation.</param>
        /// <param name="regenRate">The regeneration rate for the specified agent.</param>
        /// <param name="regenTime">The time it takes for the specified agent to be fully healed from zero.</param>
        /// <returns>The modified regeneration rate.</returns>
        public abstract double Calculate(Agent agent, double regenRate, double regenTime);

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

        internal static DropdownDefault<Formula> GetFormulas()
        {
            return new DropdownDefault<Formula>(formulas, 0);
        }

        private static void CompileCode(CodeDomProvider codeProvider, CompilerParameters parameters, FileInfo codeFile)
        {
            try
            {
                CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, codeFile.FullName);

                if (results.Errors.Count > 0)
                {
                    foreach (CompilerError error in results.Errors)
                        Debug.Print($"[BattleRegen] {error}");

                    if (!results.Errors.HasErrors)
                        Debug.Print($"[BattleRegen] Compilation of {codeFile.FullName} generated warnings. See above for details.");
                    else
                    {
                        Debug.Print($"[BattleRegen] Compilation of {codeFile.FullName} failed. See above for details.");
                        return;
                    }
                }
                Debug.Print($"[BattleRegen] {codeFile.FullName} compiled successfully.");

                Assembly compiledCode = results.CompiledAssembly;
                var types = compiledCode.GetTypes().Where(x => typeof(Formula).IsAssignableFrom(x));

                if (types.IsEmpty())
                    Debug.Print($"[BattleRegen] No class derived from {typeof(Formula).FullName} exists from {codeFile.FullName}.");
                else types.Do(x => AddFormula(x));
            }
            catch (Exception e)
            {
                string error = $"[BattleRegen] Failed to load file {codeFile.FullName}\n\nError: {e}";
                Debug.Print(error);
                InformationManager.DisplayMessage(new InformationMessage(error));
            }
        }

        internal static List<Formula> InitializeFormulas()
        {
            Debug.Print("[BattleRegen] Compiling all formulas. This could take a while.");

            // Shamelessly copied from Custom Troop Upgrades because it's my mod
            if (!Modules.IsEmpty()) Modules.Clear();

            string[] moduleNames = Utilities.GetModulesNames();
            foreach (string moduleName in moduleNames)
            {
                ModuleInfo m = new ModuleInfo();
                m.Load(moduleName);

                if (m.Id == "BattleRegeneration") Modules.Insert(0, m); // original mod should load first
                else Modules.Add(m);
            }

            formulas = new List<Formula>();
            // Set up compilers options
            ProviderOptions csOptions = new ProviderOptions(System.IO.Path.Combine(ModulesPath, Modules[0].Alias, "bin", "Win64_Shipping_Client", "roslyn", "csc.exe"), 60);
            ProviderOptions vbOptions = new ProviderOptions(System.IO.Path.Combine(ModulesPath, Modules[0].Alias, "bin", "Win64_Shipping_Client", "roslyn", "vbc.exe"), 60);
            CSharpCodeProvider csCodeProvider = new CSharpCodeProvider(csOptions);
            VBCodeProvider vbCodeProvider = new VBCodeProvider(vbOptions);
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            try
            {
                parameters.ReferencedAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).Select(x => x.Location).ToArray());
            }
            catch (Exception e)
            {
                Debug.Print($"[BattleRegen] Failed to add all required assemblies.\n\n{e}");
            }

            foreach (ModuleInfo module in Modules)
            {
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "ModuleData"));
                if (dataPath.Exists)
                {
                    foreach (FileInfo csFile in dataPath.EnumerateFiles("*.battleregen.cs"))
                        CompileCode(csCodeProvider, parameters, csFile);
                    foreach (FileInfo vbFile in dataPath.EnumerateFiles("*.battleregen.vb"))
                        CompileCode(vbCodeProvider, parameters, vbFile);
                }
            }

            formulas.Sort();
            Debug.Print("[BattleRegen] Loaded all installed regeneration formulas. See mod entry in MCM for details.");
            return formulas;
        }

        /// <summary>
        /// Adds a formula, given the type parameter.
        /// </summary>
        /// <typeparam name="T">A subtype of <see cref="Formula"/>.</typeparam>
        /// <returns>Whether the addition is successful.</returns>
        public static bool AddFormula<T>() where T : Formula => AddFormula(typeof(T));

        /// <summary>
        /// Adds a formula, given the <see cref="Type"/> parameter.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> representing the subtype of <see cref="Formula"/>.</param>
        /// <returns>Whether the addition is successful.</returns>
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
