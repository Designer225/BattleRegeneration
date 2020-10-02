using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    public sealed class BattleRegeneration : MissionBehaviour
    {
        private const int HumanFamilyType = 0;

        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        private readonly BattleRegenSettings settings;
        private readonly Mission mission;
        private readonly Dictionary<Hero, double> heroXpGainPairs;

        public BattleRegeneration(Mission mission)
        {
            settings = BattleRegenSettings.Instance;
            this.mission = mission;
            heroXpGainPairs = new Dictionary<Hero, double>();

            Debug.Print("[BattleRegeneration] Mission started, data initialized");
            Debug.Print("[BattleRegeneration] Debug mode on, dumping settings: "
                + string.Format("regen amount in percent total HP: {0}, medicine boost: {1}, regen model: {2}, ",
                    settings.RegenAmount, settings.MedicineBoost, settings.RegenModel)
                + string.Format("commander medicine boost: {0}, xp gain: {1}, commander xp gain: {2}, ",
                    settings.CommanderMedicineBoost, settings.XpGain, settings.CommanderXpGain)
                + string.Format("regen: player? {0}, companions? {1}, allied heroes? {2}, party troops? {3}, ",
                    settings.ApplyToPlayer, settings.ApplyToCompanions, settings.ApplyToAlliedHeroes, settings.ApplyToPartyTroops)
                + string.Format("allied troops? {0}, enemy heroes? {1}, enemy troops? {2}, animals? {3}",
                    settings.ApplyToAlliedTroops, settings.ApplyToEnemyHeroes, settings.ApplyToEnemyTroops, settings.ApplyToAnimal));
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Multi-threading work mk2
            Queue<Agent> agents = new Queue<Agent>(mission.AllAgents);

            while (agents.Count > 0)
            {
                Agent agent = agents.Dequeue();
                ThreadPool.QueueUserWorkItem(x => OnAction(agent, dt));
            }
        }

        private void OnAction(Agent agent, float dt)
        {
            try
            {
                if (agent.Health < agent.HealthLimit)
                    AttemptRegenerateAgent(agent, dt);
            }
            catch (Exception e)
            {
                Debug.Print($"[BattleRegeneration] An exception has occurred attempting to heal {agent.Name}. Will try again next tick.\nException: {e}");
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();

            foreach (KeyValuePair<Hero, double> heroXpGainPair in heroXpGainPairs)
            {
                heroXpGainPair.Key.AddSkillXp(DefaultSkills.Medicine, (float)(heroXpGainPair.Value));
                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] hero {heroXpGainPair.Key.Name} has received {heroXpGainPair.Value} xp from battle");
            }
            heroXpGainPairs.Clear();
        }

        private void AttemptRegenerateAgent(Agent agent, float dt)
        {
            if (agent.Monster.FamilyType != HumanFamilyType)
            {
                if (settings.ApplyToAnimal) Regenerate(agent, dt, agent.MountAgent?.Team);
            }
            else if (agent.IsPlayerControlled)
            {
                if (settings.ApplyToPlayer) Regenerate(agent, dt);
            }
            else
            {
                Team team = agent.Team;
                if (team == null)
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToEnemyHeroes) Regenerate(agent, dt);
                    }
                    else
                    {
                        if (settings.ApplyToEnemyTroops) Regenerate(agent, dt);
                    }
                }
                else if (team.IsPlayerTeam)
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToCompanions) Regenerate(agent, dt, team);
                    }
                    else
                    {
                        if (settings.ApplyToPartyTroops) Regenerate(agent, dt, team);
                    }
                }
                else if (team.IsPlayerAlly)
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToAlliedHeroes) Regenerate(agent, dt, team);
                    }
                    else
                    {
                        if (settings.ApplyToAlliedTroops) Regenerate(agent, dt, team);
                    }
                }
                else
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToEnemyHeroes) Regenerate(agent, dt, team);
                    }
                    else
                    {
                        if (settings.ApplyToEnemyTroops) Regenerate(agent, dt, team);
                    }
                }
            }
        }

        private void Regenerate(Agent agent, float dt, Team agentTeam = null)
        {

            if (agentTeam == null) agentTeam = agent.Team;

            if (agent.Health > 0f && agent.Health < agent.HealthLimit)
            {
                double modifier = GetHealthModifier(agent, agentTeam, out Healer healers);
                double baseRegenRate = settings.RegenAmount / 100.0 * agent.HealthLimit;
                double regenRate = ApplyRegenModel(agent, baseRegenRate, modifier);
                double regenAmount = regenRate * dt;

                if (agent.Health + regenAmount >= agent.HealthLimit)
                    agent.Health = agent.HealthLimit;
                else
                    agent.Health += (float)regenAmount;

                GiveXpToHealers(agent, agentTeam, healers, regenAmount);
                if (settings.Debug)
                    Debug.Print(string.Format("[BattleRegeneration] {0} agent {1} health: {2}, health added: {3} (base: {4}, multiplier: {5}), dt: {6}",
                        GetTroopType(agent, agentTeam), agent.Name, agent.Health, regenAmount, baseRegenRate * dt, modifier, dt));
            }
        }

        private double ApplyRegenModel(Agent agent, double baseRegenRate, double modifier)
        {
            double regenRate = baseRegenRate * modifier;
            double regenTime = agent.HealthLimit / regenRate;

            // New implementation using mXparser
            regenRate = settings.RegenModel.Calculate(agent, regenRate, regenTime);

            return regenRate;
        }

        private double GetHealthModifier(Agent agent, Team agentTeam, out Healer healers)
        {
            healers = 0;
            double modifier = 1.0;
            double percentMedBoost = settings.MedicineBoost / 100.0;

            if (agentTeam != null && agentTeam.GeneralAgent != null)
            {
                modifier += agentTeam.GeneralAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50.0 * settings.CommanderMedicineBoost / 100.0;
                healers |= Healer.General;
            }
            if (agent.Monster.FamilyType == HumanFamilyType) // Since only humans have skills...
            {
                modifier += agent.Character.GetSkillValue(DefaultSkills.Medicine) / 50.0 * percentMedBoost;
                healers |= Healer.Self;
            }
            else if (agent.IsMount && agent.MountAgent != null)
            {
                modifier += agent.MountAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50.0 * percentMedBoost;
                healers |= Healer.Rider;
            }

            if (settings.Debug)
                Debug.Print(string.Format("[BattleRegeneration] {0} agent {1} is receiving a {2} multiplier in health regeneration",
                    GetTroopType(agent, agentTeam), agent.Name, modifier));
            return modifier;
        }

        private string GetTroopType(Agent agent, Team agentTeam)
        {
            if (agent.IsMount) return "Mount";
            else if (agent.Monster.FamilyType != HumanFamilyType) return "Animal";
            else if (agent.IsPlayerControlled) return "Player";
            else if (agentTeam == null)
            {
                if (agent.IsHero) return "Independent hero";
                else return "Independent troop";
            }
            else if (agentTeam.IsPlayerTeam)
            {
                if (agent.IsHero) return "Companion";
                else return "Player troop";
            }
            else if (agentTeam.IsPlayerAlly)
            {
                if (agent.IsHero) return "Allied hero";
                else return "allied troop";
            }
            else
            {
                if (agent.IsHero) return "Enemy hero";
                else return "Enemy troop";
            }
        }

        private void GiveXpToHealers(Agent agent, Team agentTeam, Healer healers, double regenAmount)
        {
            double xpGain = regenAmount / agent.HealthLimit;

            if ((healers & Healer.General) == Healer.General && agentTeam.GeneralAgent.IsHero)
            {
                double cdrXpGain = xpGain * settings.CommanderXpGain;
                Hero commander = (agentTeam.GeneralAgent.Character as CharacterObject).HeroObject;

                if (!heroXpGainPairs.ContainsKey(commander))
                    heroXpGainPairs[commander] = 0.0;
                heroXpGainPairs[commander] += cdrXpGain;

                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] commander agent {agentTeam.GeneralAgent.Name} has received {cdrXpGain} xp");
            }
            if ((healers & Healer.Self) == Healer.Self && agent.IsHero)
            {
                double selfXpGain = xpGain * settings.XpGain;
                Hero hero = (agent.Character as CharacterObject).HeroObject;

                if (!heroXpGainPairs.ContainsKey(hero))
                    heroXpGainPairs[hero] = 0.0;
                heroXpGainPairs[hero] += selfXpGain;

                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] agent {agent.Name} has received {selfXpGain} xp");
            }
            if ((healers & Healer.Rider) == Healer.Rider && agent.MountAgent.IsHero)
            {
                double riderXpGain = xpGain * settings.XpGain;
                Hero rider = (agent.MountAgent.Character as CharacterObject).HeroObject;

                if (!heroXpGainPairs.ContainsKey(rider))
                    heroXpGainPairs[rider] = 0.0;
                heroXpGainPairs[rider] += riderXpGain;

                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] rider agent {agent.MountAgent.Name} has received {riderXpGain} xp");
            }
        }

        public override void OnMissionRestart()
        {
            base.OnMissionRestart();
            heroXpGainPairs.Clear();
            Debug.Print("[BattleRegeneration] Mission reset, clearing existing data");
        }

        public enum Healer
        {
            General = 1,
            Self = 2,
            Rider = 4
        }
    }

    public enum BattleRegenModel
    {
        Linear = 1,
        Quadratic = 2,
        EveOnline = 3
    }

}
