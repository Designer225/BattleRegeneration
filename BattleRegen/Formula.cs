using HarmonyLib;
using MCM.Abstractions.Data;
using org.mariuszgromada.math.mxparser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    [DataContract(Namespace = "")]
    [KnownType(typeof(Variable))]
    public class Formula
    {
        private static readonly DataContractSerializer FormulaSerializer = new DataContractSerializer(typeof(Formula));

        public static string ModulesPath { get; private set; } = System.IO.Path.Combine(BasePath.Name, "Modules");

        public static List<ModuleInfo> Modules { get; private set; } = new List<ModuleInfo>();

        private static readonly string[] BuiltInVariables = new string[] { "regenRate", "regenTime", "answer" };

        [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 0)]
        public string Name { get; private set; }

        [DataMember(EmitDefaultValue = false, IsRequired = true, Name = "Expressions", Order = 1)]
        private string[] expressions;

        public IEnumerable<string> Expressions => expressions.ToList();

        [DataMember(EmitDefaultValue = false, IsRequired = true, Name = "Variables", Order = 2)]
        private Variable[] variables;

        public IEnumerable<Variable> Variables => variables.ToList();

        public Formula(string name, string[] expressions, Variable[] variables)
        {
            Name = name;
            this.expressions = expressions;
            this.variables = variables;
        }

        public override string ToString()
        {
            return new TextObject(Name).ToString();
        }

        public double Calculate(Agent agent, double regenRate, double regenTime)
        {
            // Built-in variables
            var regenRateArg = new Argument("regenRate", regenRate);
            var regenTimeArg = new Argument("regenTime", regenTime);

            List<Argument> args = new List<Argument>()
            {
                regenRateArg, regenTimeArg
            };

            foreach (Variable var in variables)
            {
                try
                {
                    double value;

                    if (var.Expression != null) // Always evaluate an expression if there is one available
                        value = new Expression(var.Expression, args.ToArray()).calculate();
                    else if (var.ValueOrType != null)
                    {
                        bool isNum = double.TryParse(var.ValueOrType, out value);
                        if (!isNum)
                        {
                            MemberInfo[] members = AccessTools.TypeByName(var.ValueOrType)?.GetMember(var.TypeMember, AccessTools.all);
                            if (members == null || members.Length == 0)
                                throw new ArgumentException($"No such member as {var.ValueOrType}.{var.TypeMember}");

                            foreach (var member in members)
                            {
                                value = ProcessMember(agent, member);
                                if (!double.IsNaN(value)) break;
                            }
                        }
                    }
                    else throw new ArgumentException($"{var.Name} is missing a definition: define with an expression, a value, or a type member");

                    if (double.IsNaN(value)) throw new ArgumentException($"{var.Name} evaluates to {double.NaN}. Check your expressions.");
                    args.Add(new Argument(var.Name, value));
                }
                catch (Exception e)
                {
                    string error = $"[BattleRegen] Variable failed to parse: {var.Name}. This will cause issues.\n\nError: {e.Message}\n\n{e.StackTrace}";
                    Debug.PrintError(error, e.StackTrace);
                    InformationManager.DisplayMessage(new InformationMessage(error));
                }
            }

            double answer = 0;
            foreach (string str in expressions)
            {
                List<Argument> arguments = args.ToList();
                arguments.Add(new Argument("answer", answer));
                answer = new Expression(str, arguments.ToArray()).calculate();
            }

            if (double.IsNaN(answer))
            {
                string error = "Final answer is not a number, defaulting to linear model.";
                Debug.PrintError(error);
                InformationManager.DisplayMessage(new InformationMessage(error));
                answer = regenRate; // default to linear model if model calculation fails
            }
            return answer;
        }

        private double ProcessMember(Agent agent, MemberInfo member)
        {
            double value = double.NaN;

            try
            {
                if (member is MethodInfo)
                {
                    MethodInfo method = member as MethodInfo;

                    if (IsNumerical(method.ReturnType))
                    {
                        int numParams = 0; // Only non-static methods of Agent can have one (for 'this') parameter.
                        if (method.DeclaringType == typeof(Agent) && !method.IsStatic) numParams = 1;
                        value = Convert.ToDouble(numParams == 0 ? method.Invoke(null, Array.Empty<object>()) : method.Invoke(agent, Array.Empty<object>()));
                    }
                }
                else if (member is PropertyInfo)
                {
                    PropertyInfo property = member as PropertyInfo;

                    if (property.CanRead && IsNumerical(property.PropertyType))
                        value = Convert.ToDouble(property.DeclaringType == typeof(Agent) && !property.GetMethod.IsStatic ? property.GetValue(agent) : property.GetValue(null));
                }
                else if (member is FieldInfo)
                {
                    FieldInfo field = member as FieldInfo;

                    if (IsNumerical(field.FieldType))
                        value = Convert.ToDouble(field.DeclaringType == typeof(Agent) && !field.IsStatic ? field.GetValue(agent) : field.GetValue(null));
                }
            }
            catch (Exception e)
            {
                string error = $"[BattleRegen] Failed to access {member}. This will cause issues.\n\nError: {e.Message}\n\n{e.StackTrace}";
                Debug.PrintError(error, e.StackTrace);
                InformationManager.DisplayMessage(new InformationMessage(error));
            }

            return value;
        }

        private static bool IsNumerical(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
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
                Modules.Add(m);
            }

            List<Formula> formulas = new List<Formula>();

            foreach (ModuleInfo module in Modules)
            {
                DirectoryInfo dataPath = new DirectoryInfo(System.IO.Path.Combine(ModulesPath, module.Alias, "ModuleData"));
                if (dataPath.Exists)
                {
                    foreach (FileInfo xmlFile in dataPath.EnumerateFiles("*.battleregen.xml"))
                    {
                        try
                        {
                            var formula = FormulaSerializer.ReadObject(xmlFile.OpenRead()) as Formula;

                            // Error checking
                            foreach (Variable var in formula.variables)
                            {
                                if (BuiltInVariables.Contains(var.Name))
                                    throw new ArgumentException($"regenRate, regenTime, and answer are reserved variables: {var.Name}");

                                if (var.Expression == null)
                                {
                                    if (var.ValueOrType == null)
                                        throw new ArgumentException($"{var.Name} is missing a definition: define it with an expression, a value, or a type member");
                                    else if (!double.TryParse(var.ValueOrType, out _) && var.TypeMember == null)
                                        throw new ArgumentException($"{var.Name} is missing a proper type member definition: ValueOrType and/or TypeMember are missing");
                                }
                            }

                            formulas.Add(formula);
                        }
                        catch (Exception e)
                        {
                            string error = $"[BattleRegen] Failed to load file {xmlFile.FullName}\n\nError: {e.Message}\n\n{e.StackTrace}";
                            Debug.PrintError(error, e.StackTrace);
                            InformationManager.DisplayMessage(new InformationMessage(error));
                        }
                    }
                }
            }

            InformationManager.DisplayMessage(new InformationMessage("[BattleRegen] Loaded all installed regeneration formulas. See mod entry in MCM for details."));
            return new DefaultDropdown<Formula>(formulas, 0);
        }
    }

    [DataContract(Namespace = "")]
    public struct Variable
    {
        [DataMember(EmitDefaultValue = false, IsRequired = true, Order = 0)]
        public string Name { get; private set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 1)]
        public string Expression { get; private set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 2)]
        public string ValueOrType { get; private set; }

        [DataMember(EmitDefaultValue = false, IsRequired = false, Order = 3)]
        public string TypeMember { get; private set; }

        public Variable(string variable, string expression = null, string valueOrType = null, string typeMember = null)
        {
            Name = variable;
            Expression = expression;
            ValueOrType = valueOrType;
            TypeMember = typeMember;
        }
    }
}
