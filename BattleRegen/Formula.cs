using HarmonyLib;
using MCM.Abstractions.Data;
using Microsoft.CSharp;
using org.mariuszgromada.math.mxparser;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    public abstract class Formula
    {
        public static readonly string ModulesPath = System.IO.Path.Combine(BasePath.Name, "Modules");

        public static List<ModuleInfo> Modules { get; } = new List<ModuleInfo>();

        public abstract string Name { get; }

        public int Priority { get; }

        public override string ToString()
        {
            return new TextObject(Name).ToString();
        }

        public virtual double Calculate(Agent agent, double regenRate, double regenTime)
        {
            return regenRate;
        }

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
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();

            CodeDomProvider provider = CodeDomProvider.CreateProvider("cs");

            foreach (ModuleInfo module in Modules)
            {
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "ModuleData"));
                if (dataPath.Exists)
                {
                    foreach (FileInfo xmlFile in dataPath.EnumerateFiles("*.BattleRegen.cs"))
                    {
                        try
                        {
                            var formula = FormulaSerializer.ReadObject(xmlFile.OpenRead()) as Formula;

                            // Error checking
                            foreach (Variable variable in formula.variables)
                            {
                                if (BuiltInVariables.Contains(variable.Name))
                                    throw new ArgumentException($"regenRate, regenTime, and answer are reserved variables: {variable.Name}");

                                if (variable.Expression == null)
                                {
                                    if (variable.ValueOrType == null)
                                        throw new ArgumentException($"{variable.Name} is missing a definition: define it with an expression, a value, or a type member");
                                    else if (!double.TryParse(variable.ValueOrType, out _) && variable.TypeMember == null)
                                        throw new ArgumentException($"{variable.Name} is missing a proper type member definition: ValueOrType and/or TypeMember are missing");
                                }
                            }

                            formula.CacheExpressions();
                            formula.CacheVariables();

                            formulas.Add(formula);
                        }
                        catch (Exception e)
                        {
                            string error = $"[BattleRegen] Failed to load file {xmlFile.FullName}\n\nError: {e}";
                            Debug.Print(error);
                            InformationManager.DisplayMessage(new InformationMessage(error));
                        }
                    }
                }
            }

            InformationManager.DisplayMessage(new InformationMessage("[BattleRegen] Loaded all installed regeneration formulas. See mod entry in MCM for details."));
            return new DefaultDropdown<Formula>(formulas, 0);
        }
    }
}
