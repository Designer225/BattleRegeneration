using BattleRegen;
using System;
using TaleWorlds.MountAndBlade;

namespace BattleRegen.Formulas
{
    public sealed class LinearFormula : Formula
    {
        public override string Name => "{=BattleRegen_Linear}Linear";

        public override string Id => "00_Linear";

        // Built-in values must go to the very bottom. Linear should be the very, very bottom.
        public override int Priority => int.MinValue;

        public override double Calculate(Agent agent, double regenRate, double regenTime)
        {
            return regenRate;
        }
    }

    public sealed class QuadraticFormula : Formula
    {
        public override string Name => "{=BattleRegen_Quadratic}Quadratic";

        public override string Id => "01_Quadratic";

        // Built-in values must go to the very bottom. Quadratic should be right behind Linear.
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

    public sealed class EveOnlineFormula : Formula
    {
        public override string Name => "{=BattleRegen_EveOnline}EVE Online";

        public override string Id => "02_EveOnline";

        // Built-in values must go to the very bottom. EveOnline should be right behind Quadratic.
        public override int Priority => int.MinValue;

        public override double Calculate(Agent agent, double regenRate, double regenTime)
        {
            double healthToMaxRatio = agent.Health / agent.HealthLimit;
            return 10 * regenRate * (Math.Sqrt(healthToMaxRatio) - healthToMaxRatio);
        }
    }
}