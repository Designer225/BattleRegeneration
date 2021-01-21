using SandBox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    sealed class BattleRegeneration : MissionBehaviour
    {
        private const int HumanFamilyType = 0;

        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        private readonly IBattleRegenSettings settings;
        private readonly Mission mission;
        private readonly ConcurrentQueue<Tuple<Hero, double>> heroXpGainPairs;
        private readonly SynchronizedCollection<Tuple<Agent, float>> agentHpPairs;

        public BattleRegeneration(Mission mission)
        {
            settings = BattleRegenSettingsUtil.Instance;
            this.mission = mission;
            heroXpGainPairs = new ConcurrentQueue<Tuple<Hero, double>>();
            agentHpPairs = new SynchronizedCollection<Tuple<Agent, float>>();

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

            if (mission.MissionEnded() || mission.IsMissionEnding)
                return;
            else
            {
                var arenaController = mission.GetMissionBehaviour<ArenaPracticeFightMissionController>();
                if (arenaController != default && arenaController.AfterPractice) return;
            }

            // Multi-threading work mk3
            Queue<Agent> agents = new Queue<Agent>(mission.AllAgents);
            List<Task> tasks = new List<Task>();

            foreach (Agent agent in agents)
            {
                lock (agentHpPairs.SyncRoot)
                {
                    if (!agentHpPairs.Any(x => x.Item1 == agent))
                        agentHpPairs.Add(new Tuple<Agent, float>(agent, agent.Health));
                }
            }

            while (agents.Count > 0)
            {
                Agent agent = agents.Dequeue();
                tasks.Add(Task.Run(() => OnAction(agent, dt)));
            }

            foreach (Task task in tasks)
                task.Wait();
        }

        private void OnAction(Agent agent, float dt)
        {
            try
            {
                float healthLimit;
                lock(agentHpPairs.SyncRoot)
                    healthLimit = agentHpPairs.FirstOrDefault(x => x.Item1 == agent)?.Item2 ?? agent.HealthLimit; // fallback
                if (agent.Health > 0 && agent.Health < healthLimit)
                    AttemptRegenerateAgent(agent, healthLimit, dt);
            }
            catch (Exception e)
            {
                Debug.Print($"[BattleRegeneration] An exception has occurred attempting to heal {agent.Name}. Will try again next tick.\nException: {e}");
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();

            while (!heroXpGainPairs.IsEmpty)
            {
                bool success = heroXpGainPairs.TryDequeue(out Tuple<Hero, double> heroXpGainPair);
                if (!success) continue;

                try
                {
                    if (heroXpGainPair.Item1 != default)
                    {
                        heroXpGainPair.Item1.AddSkillXp(DefaultSkills.Medicine, (float)(heroXpGainPair.Item2));
                        if (settings.Debug)
                            Debug.Print($"[BattleRegeneration] hero {heroXpGainPair.Item1.Name} has received {heroXpGainPair.Item2} xp from battle");
                    }
                }
                catch (Exception e)
                {
                    Debug.Print($"[BattleRegeneration] An error occurred attempting to add XP to a hero.\n{e}");
                }
            }

            lock (agentHpPairs.SyncRoot)
                agentHpPairs.Clear();
        }

        private void AttemptRegenerateAgent(Agent agent, float healthLimit, float dt)
        {
            if (agent.Monster.FamilyType != HumanFamilyType)
            {
                if (settings.ApplyToAnimal) Regenerate(agent, healthLimit, dt, agent.MountAgent?.Team);
            }
            else if (agent.IsPlayerControlled)
            {
                if (settings.ApplyToPlayer) Regenerate(agent, healthLimit, dt);
            }
            else
            {
                Team team = agent.Team;
                if (team == null)
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToEnemyHeroes) Regenerate(agent, healthLimit, dt);
                    }
                    else
                    {
                        if (settings.ApplyToEnemyTroops) Regenerate(agent, healthLimit, dt);
                    }
                }
                else if (team.IsPlayerTeam)
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToCompanions) Regenerate(agent, healthLimit, dt, team);
                    }
                    else
                    {
                        if (settings.ApplyToPartyTroops) Regenerate(agent, healthLimit, dt, team);
                    }
                }
                else if (team.IsPlayerAlly)
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToAlliedHeroes) Regenerate(agent, healthLimit, dt, team);
                    }
                    else
                    {
                        if (settings.ApplyToAlliedTroops) Regenerate(agent, healthLimit, dt, team);
                    }
                }
                else
                {
                    if (agent.IsHero)
                    {
                        if (settings.ApplyToEnemyHeroes) Regenerate(agent, healthLimit, dt, team);
                    }
                    else
                    {
                        if (settings.ApplyToEnemyTroops) Regenerate(agent, healthLimit, dt, team);
                    }
                }
            }
        }

        private void Regenerate(Agent agent, float healthLimit, float dt, Team agentTeam = null)
        {

            if (agentTeam == null) agentTeam = agent.Team;

            if (agent.Health > 0f && agent.Health < healthLimit)
            {
                double modifier = GetHealthModifier(agent, agentTeam, out Healer healers);
                double baseRegenRate = settings.RegenAmount / 100.0 * agent.HealthLimit; // regen rate is always based on all-time health limit
                double regenRate = ApplyRegenModel(agent, healthLimit, baseRegenRate, modifier);
                double regenAmount = regenRate * dt;

                if (agent.Health + regenAmount >= healthLimit)
                    agent.Health = healthLimit;
                else
                    agent.Health += (float)regenAmount;

                GiveXpToHealers(agent, agentTeam, healers, regenAmount);
                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] {GetTroopType(agent, agentTeam)} agent {agent.Name} health: {agent.Health}, health limit: {healthLimit}, " +
                        $"health added: {regenAmount} (base: {baseRegenRate * dt}, multiplier: {modifier}), dt: {dt}");
            }
        }

        private double ApplyRegenModel(Agent agent, float healthLimit, double baseRegenRate, double modifier)
        {
            double regenRate = baseRegenRate * modifier;
            double regenTime = healthLimit / regenRate;
            double origRegenTime = agent.HealthLimit / regenRate;

            try
            {
                RegenDataInfo data = new RegenDataInfo(agent, healthLimit, regenRate, regenTime, origRegenTime);
                regenRate = settings.RegenModel.Calculate(data);
            }
            catch (Exception e)
            {
                Debug.Print($"[BattleRegeneration] An exception has occurred attempting to calculate regen value for {agent.Name}. Using linear instead.\nException: {e}");
            }

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
            double xpGain = regenAmount / agent.HealthLimit; // xp gain is also based on all-time health limit

            if ((healers & Healer.General) == Healer.General && agentTeam.GeneralAgent.IsHero)
            {
                double cdrXpGain = xpGain * settings.CommanderXpGain;
                Hero commander = (agentTeam.GeneralAgent.Character as CharacterObject).HeroObject;
                heroXpGainPairs.Enqueue(new Tuple<Hero, double>(commander, cdrXpGain));

                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] commander agent {agentTeam.GeneralAgent.Name} has received {cdrXpGain} xp");
            }
            if ((healers & Healer.Self) == Healer.Self && agent.IsHero)
            {
                double selfXpGain = xpGain * settings.XpGain;
                Hero hero = (agent.Character as CharacterObject).HeroObject;
                heroXpGainPairs.Enqueue(new Tuple<Hero, double>(hero, selfXpGain));

                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] agent {agent.Name} has received {selfXpGain} xp");
            }
            if ((healers & Healer.Rider) == Healer.Rider && agent.MountAgent.IsHero)
            {
                double riderXpGain = xpGain * settings.XpGain;
                Hero rider = (agent.MountAgent.Character as CharacterObject).HeroObject;
                heroXpGainPairs.Enqueue(new Tuple<Hero, double>(rider, riderXpGain));

                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] rider agent {agent.MountAgent.Name} has received {riderXpGain} xp");
            }
        }

        public override void OnMissionRestart()
        {
            base.OnMissionRestart();

            while (!heroXpGainPairs.IsEmpty)
                heroXpGainPairs.TryDequeue(out _);
            lock (agentHpPairs.SyncRoot)
                agentHpPairs.Clear();

            Debug.Print("[BattleRegeneration] Mission reset, clearing existing data");
        }

        [Flags]
        public enum Healer
        {
            General = 1,
            Self = 2,
            Rider = 4
        }
    }
}
