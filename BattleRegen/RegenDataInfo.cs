using System;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    /// <summary>
    /// An immutable class representing the regeneration data for a given agent in the current regen tick.
    /// </summary>
    public struct RegenDataInfo
    {
        /// <summary>
        /// The agent to be healed.
        /// </summary>
        public readonly Agent agent;

        /// <summary>
        /// The health limit of the agent. The original health limit can be obtained through <see cref="Agent.HealthLimit"/>.
        /// </summary>
        public readonly float healthLimit;

        /// <summary>
        /// The regeneration rate for the agent.
        /// </summary>
        public readonly float regenRate;

        /// <summary>
        /// The time it takes for the agent to heal from zero to the health limit.
        /// </summary>
        public readonly float regenTime;

        /// <summary>
        /// The time it takes for the agent to heal from zero to full health.
        /// </summary>
        public readonly float originalRegenTime;

        internal RegenDataInfo(Agent agent, float healthLimit, float regenRate, float regenTime, float origRegenTime)
        {
            this.agent = agent;
            this.healthLimit = healthLimit;
            this.regenRate = regenRate;
            this.regenTime = regenTime;
            originalRegenTime = origRegenTime;
        }
    }
}
