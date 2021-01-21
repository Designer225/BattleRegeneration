using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    /// <summary>
    /// An immutable class representing the regeneration data for a given agent in the current regen tick.
    /// </summary>
    public sealed class RegenDataInfo
    {
        /// <summary>
        /// The agent to be healed.
        /// </summary>
        public Agent Agent { get; }

        /// <summary>
        /// The health limit of the agent. The original health limit can be obtained through <see cref="Agent.HealthLimit"/>.
        /// </summary>
        public float HealthLimit { get; }

        /// <summary>
        /// The regeneration rate for the agent.
        /// </summary>
        public double RegenRate { get; }

        /// <summary>
        /// The time it takes for the agent to heal from zero to the health limit.
        /// </summary>
        public double RegenTime { get; }

        /// <summary>
        /// The time it takes for the agent to heal from zero to full health.
        /// </summary>
        public double OriginalRegenTime { get; }

        /// <summary>
        /// Constructs a regen data information.
        /// </summary>
        /// <param name="agent">The agent to be healed.</param>
        /// <param name="healthLimit">The health limit of the agent.</param>
        /// <param name="regenRate">The regeneration rate for the agent.</param>
        /// <param name="regenTime">The time it takes for the agent to heal from zero to the health limit.</param>
        /// <param name="origRegenTime">The time it takes for the agent to heal from zero to full health.</param>
        public RegenDataInfo(Agent agent, float healthLimit, double regenRate, double regenTime, double origRegenTime)
        {
            Agent = agent;
            HealthLimit = healthLimit;
            RegenRate = regenRate;
            RegenTime = regenTime;
            OriginalRegenTime = origRegenTime;
        }
    }
}
