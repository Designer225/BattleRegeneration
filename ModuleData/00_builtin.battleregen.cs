// The following (or their VB equivalent) should be what you need to put at the top of any code file.
using BattleRegen;
using System;
using TaleWorlds.MountAndBlade;

// Namespaces are recommended for organization purposes. You do not need to define your formulas in the BattleRegen.Formulas namespace. In fact, you are discouraged from
// doing so, since it's meant for Battle Regeneration's custom formulas.
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
        public override double Calculate(Agent agent, double regenRate, double regenTime)
        {
            return regenRate;
        }
    }

    // Quadratic regen formula
    public sealed class QuadraticFormula : Formula
    {
        public override string Name => "{=BattleRegen_Quadratic}Quadratic";

        public override string Id => "01_Quadratic";

        // Built-in values must be loaded first. Quadratic should be right behind Linear.
        public override int Priority => int.MinValue;

        public override double Calculate(Agent agent, double regenRate, double regenTime)
        {
            // d = v0*t + (a*t^2)/2 -> 0 = (a*t^2)/2 + v0*t - d
            double maxRegenRate = 2 * regenRate;
            double regenChangeRate = -maxRegenRate / regenTime;

            if (SolveForFactors(regenChangeRate / 2.0, maxRegenRate, -agent.Health, out double t1, out double t2))
            {
                if (t1 >= 0 && t1 < regenTime)
                    regenRate = maxRegenRate * (regenTime - t1) / regenTime;
                else if (t2 >= 0 && t2 < regenTime)
                    regenRate = maxRegenRate * (regenTime - t2) / regenTime;
                else regenRate = 0;
            }

            return regenRate;
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

        public override double Calculate(Agent agent, double regenRate, double regenTime)
        {
            double healthToMaxRatio = agent.Health / agent.HealthLimit;
            return 10 * regenRate * (Math.Sqrt(healthToMaxRatio) - healthToMaxRatio);
        }
    }

    // Sine regen formula - credit: WyrdOh (https://forums.nexusmods.com/index.php?showtopic=8702373/#entry86794963)
    public sealed class SineFormula : Formula
    {
        public override string Name => "{=BattleRegen_Sine}Sine";

        public override string Id => "03_Sine";

        // Built-in values must be loaded first. Sine should be right behind EveOnline.
        public override int Priority => int.MinValue;

        public override double Calculate(Agent agent, double regenRate, double regenTime)
        {
            double SinehealthToMaxRatio = agent.Health / agent.HealthLimit;
            return (25 * regenRate) * (Math.Sin(Math.PI * SinehealthToMaxRatio) / 100);
        }
    }
}