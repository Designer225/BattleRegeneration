// The following (or their VB equivalent) should be what you need to put at the top of any code file.
using BattleRegen;
using System;
using TaleWorlds.MountAndBlade;

// Namespaces are recommended for organization purposes. You do not need to define your formulas in the BattleRegen.Formulas namespace. In fact, you are discouraged from
// doing so, since it's meant for Battle Regeneration's formulas.
// Supported languages: C# and Visual Basic. Make sure code files end in .battleregen.cs (C#) or .battleregen.vb (Visual Basic).
namespace BattleRegen.Formulas
{
    // Linear regen formula
    public sealed class LinearFormula : Formula
    {
        // You must define this property.
        public override string Name => "{=BattleRegen_Linear}Linear";

        // You must define this property. Battle Regeneration use this to sort when formulas have the same priority.
        public override string Id => "00_Linear";

        // Built-in values must be loaded first. Linear should be the very, very first.
        // By default this property is defined to return 0. Battle Regeneration use this to sort formulas first; see above in case of a priority tie (which is usually).
        public override int Priority => int.MinValue;

        // You must define this method as it is called by the game when regenerating.
        public override double Calculate(RegenDataInfo data)
        {
            return data.RegenRate;
        }
    }

    // Quadratic regen formula
    public sealed class QuadraticFormula : Formula
    {
        public override string Name => "{=BattleRegen_Quadratic}Quadratic";

        public override string Id => "01_Quadratic";

        // Built-in values must be loaded first. Quadratic should be right behind Linear.
        public override int Priority => int.MinValue;

        public override double Calculate(RegenDataInfo data)
        {
            // d = v0*t + (a*t^2)/2 -> 0 = (a*t^2)/2 + v0*t - d
            double maxRegenRate = 2 * data.RegenRate;
            double regenChangeRate = -maxRegenRate / data.OriginalRegenTime;

            if (SolveForFactors(regenChangeRate / 2.0, maxRegenRate, -data.Agent.Health, out double t1, out double t2))
            {
                if (t1 >= 0 && t1 < data.RegenTime)
                    return maxRegenRate * (data.RegenTime - t1) / data.RegenTime;
                else if (t2 >= 0 && t2 < data.RegenTime)
                    return maxRegenRate * (data.RegenTime - t2) / data.RegenTime;
            }

            return 0.0;
        }

        // Helper method for the above formula - yes, you can do that
        private bool SolveForFactors(double a, double b, double c, out double x1, out double x2)
        {
            x1 = 0;
            x2 = 0;
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;

            double sqrtDiscriminant = Math.Sqrt(discriminant);
            x1 = (-b + sqrtDiscriminant) / (2 * a);
            x2 = (-b - sqrtDiscriminant) / (2 * a);
            return true;
        }
    }

    // EVE Online regen formula
    public sealed class EveOnlineFormula : Formula
    {
        public override string Name => "{=BattleRegen_EveOnline}EVE Online";

        public override string Id => "02_EveOnline";

        // Built-in values must be loaded first. EveOnline should be right behind Quadratic.
        public override int Priority => int.MinValue;

        public override double Calculate(RegenDataInfo data)
        {
            double healthToMaxRatio = data.Agent.Health / data.Agent.HealthLimit;
            return 10 * data.RegenRate * (Math.Sqrt(healthToMaxRatio) - healthToMaxRatio);
        }
    }
}
