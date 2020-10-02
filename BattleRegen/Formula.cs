using HarmonyLib;
using MCM.Abstractions.Data;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    public abstract class Formula : IComparable<Formula>
    {
        public static readonly string ModulesPath = System.IO.Path.Combine(BasePath.Name, "Modules");

        public static List<ModuleInfo> Modules { get; } = new List<ModuleInfo>();

        public abstract string Name { get; }

        public abstract string Id { get; }

        public abstract int Priority { get; }

        public override string ToString()
        {
            return new TextObject(Name).ToString();
        }

        public abstract double Calculate(Agent agent, double regenRate, double regenTime);

        public static DefaultDropdown<Formula> GetFormulas()
        {
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

            List<Formula> formulas = new List<Formula>();
            // Set up compilers
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            foreach (ModuleInfo module in Modules)
            {
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "ModuleData"));
                if (dataPath.Exists)
                {
                    foreach (FileInfo csFile in dataPath.EnumerateFiles("*.battleregen.cs"))
                    {
                        try
                        {
                            CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, csFile.FullName);

                            if (results.Errors.Count > 0)
                            {
                                foreach (CompilerError error in results.Errors)
                                    Debug.Print($"[BattleRegen] {error}");

                                if (!results.Errors.HasErrors)
                                    Debug.Print($"[BattleRegen] Compilation of {csFile.FullName} generated warnings. See above for details.");
                                else
                                {
                                    Debug.Print($"[BattleRegen] Compilation of {csFile.FullName} failed. See above for details.");
                                    continue;
                                }
                            }
                            Debug.Print($"[BattleRegen] {csFile.FullName} compiled successfully.");

                            Assembly compiledCode = results.CompiledAssembly;
                            var types = compiledCode.GetTypes().Where(x => typeof(Formula).IsAssignableFrom(x));

                            foreach (Type type in types)
                            {
                                if (type != null)
                                {
                                    ConstructorInfo constructor = AccessTools.Constructor(type, Array.Empty<Type>());
                                    if (constructor != null)
                                    {
                                        Formula formula = constructor.Invoke(Array.Empty<object>()) as Formula;
                                        formulas.Add(formula);
                                    }
                                    else Debug.Print($"[BattleRegen] No constructor from {type.FullName} exists that take zero parameters");
                                }
                                else Debug.Print($"[BattleRegen] No class derived from {typeof(Formula).FullName} exists from {csFile.FullName}.");
                            }
                        }
                        catch (Exception e)
                        {
                            string error = $"[BattleRegen] Failed to load file {csFile.FullName}\n\nError: {e}";
                            Debug.Print(error);
                            InformationManager.DisplayMessage(new InformationMessage(error));
                        }
                    }
                }
            }

            formulas.Sort();
            InformationManager.DisplayMessage(new InformationMessage("[BattleRegen] Loaded all installed regeneration formulas. See mod entry in MCM for details."));
            return new DefaultDropdown<Formula>(formulas, 0);
        }

        public int CompareTo(Formula other)
        {
            int comparison = Priority.CompareTo(other.Priority);

            if (comparison == 0) return Id.CompareTo(other.Id);
            else return comparison;
        }
    }
}
