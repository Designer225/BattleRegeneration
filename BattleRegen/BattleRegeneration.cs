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
        private readonly Dictionary<Agent, double> agentHealthPair;

        public BattleRegeneration(Mission mission)
        {
            settings = BattleRegenSettings.Instance;
            agentHealthPair = new Dictionary<Agent, double>();
            this.mission = mission;

            if (settings.Debug)
            {
                Debug.Print("[BattleRegeneration] Dumping settings: "
                    + string.Format("regen amount in percent total HP: {0}, Regen: player? {1}, ", settings.RegenAmount, settings.ApplyToPlayer)
                    + string.Format("companions? {0}, allied heroes? {1}, party troops? {2}, ",
                        settings.ApplyToCompanions, settings.ApplyToAlliedHeroes, settings.ApplyToPartyTroops)
                    + string.Format("allied troops? {0}, enemy heroes? {1}, enemy troops? {2}, mounts? {3}",
                        settings.ApplyToAlliedTroops, settings.ApplyToEnemyHeroes, settings.ApplyToEnemyTroops, settings.ApplyToMount));
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            foreach (Agent agent in mission.AllAgents)
            {
                try
                {
                    if (agent.Health >= agent.HealthLimit) continue;

                    if (CanAgentRegenerate(agent, dt))
                        Regenerate(agent, dt);
                }
                catch (Exception e)
                {
                    Debug.PrintError(string.Format("[BattleRegeneration] An exception has occurred attempting to heal {0}. Will try again next tick.\nException: {1}",
                        agent.Name, e), e.StackTrace);
                }
            }
        }

        private bool CanAgentRegenerate(Agent agent, float dt)
        {
            if (agent == null) return false;

            Team playerTeam = mission.PlayerTeam;
            Team allyTeam = mission.PlayerAllyTeam;

            if (agent.IsMount && settings.ApplyToMount)
            {
                if (settings.Debug)
                    Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to mount agent {2} (current HP: {3}, dt: {4})",
                        settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                return true;
            }
            else if (agent.IsPlayerControlled && settings.ApplyToPlayer)
            {
                if (settings.Debug)
                    Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to player agent {2} (current HP: {3}, dt: {4})",
                       settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                return true;
            }
            else if (agent.Team == playerTeam)
            {
                if (agent.IsHero && settings.ApplyToCompanions)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to companion agent {2} (current HP: {3}, dt: {4})",
                            settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                    return true;
                }
                else if (!agent.IsHero && settings.ApplyToPartyTroops)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to party troop agent {2} (current HP: {3}, dt: {4})",
                            settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                    return true;
                }
            }
            else if (agent.Team == allyTeam)
            {
                if (agent.IsHero && settings.ApplyToAlliedHeroes)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to allied hero agent {2} (current HP: {3}, dt: {4})",
                            settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                    return true;
                }
                else if (!agent.IsHero && settings.ApplyToAlliedTroops)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to allied troop agent {2} (current HP: {3}, dt: {4})",
                            settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                    return true;
                }
            }
            else
            {
                if (agent.IsHero && settings.ApplyToEnemyHeroes)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to enemy hero agent {2} (current HP: {3}, dt: {4})",
                            settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                    return true;
                }
                else if (!agent.IsHero && settings.ApplyToEnemyTroops)
                {
                    if (settings.Debug)
                        Debug.Print(string.Format("[BattleRegeneration] Adding {0} HP ({1}% of total) to enemy troop agent {2} (current HP: {3}, dt: {4})",
                            settings.RegenAmount * dt * agent.HealthLimit, settings.RegenAmount * dt, agent.Name, agent.Health, dt));
                    return true;
                }
            }

            return false;
        }

        private void Regenerate(Agent agent, double dt)
        {
            if (!agentHealthPair.ContainsKey(agent) || Math.Ceiling(agentHealthPair[agent]) < agent.Health || Math.Ceiling(agentHealthPair[agent]) > agent.Health)
                agentHealthPair[agent] = agent.Health;

            if (agentHealthPair[agent] > 0f && agentHealthPair[agent] < agent.HealthLimit)
            {
                double regenAmount = settings.RegenAmount * dt * agent.HealthLimit;
                if (agentHealthPair[agent] + regenAmount >= agent.HealthLimit)
                    agentHealthPair[agent] = agent.HealthLimit;
                else
                    agentHealthPair[agent] += regenAmount;
            }

            agent.Health = (float)agentHealthPair[agent];
            if (settings.Debug)
                Debug.Print(string.Format("[BattleRegeneration] Agent {0} expected health: {1}, actual health: {2}", agent.Name, agentHealthPair[agent], agent.Health));
        }

        public override void OnMissionRestart()
        {
            base.OnMissionRestart();
            agentHealthPair.Clear();
            Debug.Print("[BattleRegeneration] Mission reset, clearing existing data");
        }
    }
}
