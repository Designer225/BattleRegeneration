using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    public sealed class BattleRegeneration : MissionBehaviour
    {
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        private readonly BattleRegenSettings settings;
        private readonly Mission mission;
        private float timeSinceLastRegen;

        public BattleRegeneration(Mission mission)
        {
            settings = BattleRegenSettings.Instance;
            this.mission = mission;
            timeSinceLastRegen = 0;

            if (settings.Debug)
            {
                Debug.Print("[BattleRegeneration] Dumping settings: "
                    + string.Format("regen amount: {0}, Regen: player? {1}, ", settings.RegenAmount, settings.ApplyToPlayer)
                    + string.Format("companions? {0}, allied heroes? {1}, party troops? {2}, ",
                        settings.ApplyToCompanions, settings.ApplyToAlliedHeroes, settings.ApplyToPartyTroops)
                    + string.Format("allied troops? {0}, enemy heroes? {1}, enemy troops? {2}, mounts? {3}",
                        settings.ApplyToAlliedTroops, settings.ApplyToEnemyHeroes, settings.ApplyToEnemyTroops, settings.ApplyToMount));
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            timeSinceLastRegen += dt;

            while (timeSinceLastRegen >= 1)
            {
                if (settings.Debug)
                    Debug.Print(string.Format("[BattleRegeneration] 1 second has elapsed. Regeneration tick active. (time since last regen: {0})", timeSinceLastRegen));
                timeSinceLastRegen -= 1;
                if (timeSinceLastRegen < 0)
                    timeSinceLastRegen = 0;

                foreach (Agent agent in mission.AllAgents)
                {
                    if (agent.Health >= agent.HealthLimit) continue;

                    if (CanAgentRegenerate(agent))
                        Regenerate(agent);
                }
            }
        }

        private bool CanAgentRegenerate(Agent agent)
        {
            if (agent == null) return false;

            Team playerTeam = mission.PlayerTeam;
            Team allyTeam = mission.PlayerAllyTeam;

            if (agent.IsPlayerControlled && settings.ApplyToPlayer)
            {
                if (settings.Debug)
                    Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to player agent {1} (current HP: {2})",
                        settings.RegenAmount, agent.Name, agent.Health));
                return true;
            }
            else if (agent.Team == playerTeam)
            {
                if (agent.IsHero && settings.ApplyToCompanions)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to companion agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
                else if (!agent.IsHero && settings.ApplyToPartyTroops)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to party troop agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
            }
            else if (agent.Team == allyTeam)
            {
                if (agent.IsHero && settings.ApplyToAlliedHeroes)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to allied hero agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
                else if (!agent.IsHero && settings.ApplyToAlliedTroops)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to allied troop agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
            }
            else
            {
                if (agent.IsMount && settings.ApplyToMount)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to mount agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
                else if (agent.IsHero && settings.ApplyToEnemyHeroes)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to enemy hero agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
                else if (!agent.IsHero && settings.ApplyToEnemyTroops)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP to enemy troop agent {1} (current HP: {2})",
                            settings.RegenAmount, agent.Name, agent.Health));
                    return true;
                }
            }

            return false;
        }

        private void Regenerate(Agent agent)
        {
            if (agent.Health > 0f && agent.Health < agent.HealthLimit)
            {
                if (agent.Health + settings.RegenAmount >= agent.HealthLimit)
                    agent.Health = agent.HealthLimit;
                else
                    agent.Health += settings.RegenAmount;
            }
        }
    }
}
