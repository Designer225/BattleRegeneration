using HarmonyLib;
using MCM.Abstractions.Data;
using org.mariuszgromada.math.mxparser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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

        // for internal use
        private List<Expression> expressionCache;
        private List<CachedVariable> variableCache;

        public Formula(string name, string[] expressions, Variable[] variables)
        {
            Name = name;
            this.expressions = expressions;
            this.variables = variables;
            CacheExpressions();
            CacheVariables();
        }

        private void CacheExpressions()
        {
            expressionCache = new List<Expression>();

            foreach (string expression in expressions)
                expressionCache.Add(new Expression(expression));
        }

        private void CacheVariables()
        {
            variableCache = new List<CachedVariable>();

            foreach (Variable variable in variables)
            {
                if (variable.Expression != null)
                    variableCache.Add(new CachedVariable(variable.Name, expression: new Expression(variable.Expression)));
                else if (variable.ValueOrType != null)
                {
                    bool isNum = double.TryParse(variable.ValueOrType, out double value);

                    if (isNum)
                        variableCache.Add(new CachedVariable(variable.Name, value: value));
                    else
                    {
                        MemberInfo[] members = AccessTools.TypeByName(variable.ValueOrType)?.GetMember(variable.TypeMember, AccessTools.all);
                        if (members == null || members.Length == 0)
                            throw new ArgumentException($"No such member as {variable.ValueOrType}.{variable.TypeMember}");

                        foreach (var member in members)
                        {
                            if (member is MethodInfo)
                            {
                                MethodInfo method = member as MethodInfo;

                                if (method.GetParameters().Count() == 0 && (method.IsStatic || method.DeclaringType.IsAssignableFrom(typeof(Agent))))
                                {
                                    variableCache.Add(new CachedVariable(variable.Name, member: method));
                                    break;
                                }
                            }
                            else if (member is PropertyInfo)
                            {
                                PropertyInfo property = member as PropertyInfo;

                                if (property.CanRead && (property.GetMethod.IsStatic || property.DeclaringType.IsAssignableFrom(typeof(Agent))))
                                {
                                    variableCache.Add(new CachedVariable(variable.Name, member: property));
                                    break;
                                }
                            }
                            else if (member is FieldInfo)
                            {
                                FieldInfo field = member as FieldInfo;

                                if (field.IsStatic || field.DeclaringType.IsAssignableFrom(typeof(Agent)))
                                {
                                    variableCache.Add(new CachedVariable(variable.Name, member: field));
                                    break;
                                }
                            }
                        }
                    }
                }
                else throw new ArgumentException($"{variable.Name} is missing a definition: define with an expression, a value, or a type member");
            }
        }

        public override string ToString()
        {
            return new TextObject(Name).ToString();
        }

        // This piece of code is critical code, so making it multi-threadding-proof is essential
        public double Calculate(Agent agent, double regenRate, double regenTime)
        {
            // Built-in variables
            var regenRateArg = new Argument("regenRate", regenRate);
            var regenTimeArg = new Argument("regenTime", regenTime);

            List<Argument> args = new List<Argument>()
            {
                regenRateArg, regenTimeArg
            };

            foreach (CachedVariable variable in variableCache)
            {
                try
                {
                    double value;

                    if (variable.ExpressionObj != null) // Always evaluate an expression if there is one available
                    {
                        // Multi-threading friendly
                        Expression expression = new Expression(variable.ExpressionObj.getExpressionString());
                        expression.addArguments(args.ToArray());
                        value = expression.calculate();
                    }
                    else if (!double.IsNaN(variable.Value))
                        value = variable.Value;
                    else if (variable.Member != null)
                        value = ProcessMember(agent, variable.Member);
                    else throw new ArgumentException($"{variable.Name} is missing a definition: define with an expression, a value, or a type member");

                    if (double.IsNaN(value)) throw new ArgumentException($"{variable.Name} evaluates to {double.NaN}. Check your expressions.");
                    args.Add(new Argument(variable.Name, value));
                }
                catch (Exception e)
                {
                    string error = $"[BattleRegen] Variable failed to parse: {variable.Name}. This will cause issues.\n\nError: {e}";
                    Debug.Print(error);
                    InformationManager.DisplayMessage(new InformationMessage(error));
                }
            }

            double answer = 0;
            Argument[] arguments = args.ToArray();
            foreach (Expression exp in expressionCache)
            {
                // Multi-threading friendly
                Expression exp2 = new Expression(exp.getExpressionString());
                exp2.addArguments(arguments);
                exp2.defineArgument("answer", answer);
                answer = exp2.calculate();
            }

            if (double.IsNaN(answer))
            {
                string error = "[BattleRegen] Final answer is not a number, defaulting to linear model.";
                Debug.Print(error);
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

                    if (method.GetParameters().Count() == 0 && IsNumerical(method.ReturnType))
                        value = Convert.ToDouble(method.DeclaringType.IsAssignableFrom(typeof(Agent)) && !method.IsStatic
                            ? method.Invoke(agent, Array.Empty<object>()) : method.Invoke(null, Array.Empty<object>()));
                }
                else if (member is PropertyInfo)
                {
                    PropertyInfo property = member as PropertyInfo;

                    if (property.CanRead && IsNumerical(property.PropertyType))
                        value = Convert.ToDouble(property.DeclaringType.IsAssignableFrom(typeof(Agent)) && !property.GetMethod.IsStatic
                            ? property.GetValue(agent) : property.GetValue(null));
                }
                else if (member is FieldInfo)
                {
                    FieldInfo field = member as FieldInfo;

                    if (IsNumerical(field.FieldType))
                        value = Convert.ToDouble(field.DeclaringType.IsAssignableFrom(typeof(Agent)) && !field.IsStatic
                            ? field.GetValue(agent) : field.GetValue(null));
                }
            }
            catch (Exception e)
            {
                string error = $"[BattleRegen] Failed to access {member}. This will cause issues.\n\nError: {e}";
                Debug.Print(error);
                InformationManager.DisplayMessage(new InformationMessage(error));
            }

            return value;
        }

        public static bool IsNumerical(Type type)
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

                if (m.Id == "BattleRegeneration") Modules.Insert(0, m); // original mod should load first
                else Modules.Add(m);
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

    [DataContract(Namespace = "")]
    public class Variable
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

    class CachedVariable
    {
        public string Name { get; }

        public Expression ExpressionObj { get; }

        public double Value { get; }

        public MemberInfo Member { get; }

        public CachedVariable(string variable, Expression expression = null, double value = double.NaN, MemberInfo member = null)
        {
            Name = variable;
            ExpressionObj = expression;
            Value = value;
            Member = member;
        }
    }
}
