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
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;
        private readonly IBattleRegenSettings settings;
        private readonly Mission mission;
        private readonly ConcurrentQueue<Tuple<Hero, double>> heroXpGainPairs;

        public BattleRegeneration(Mission mission)
        {
            settings = BattleRegenSettingsUtil.Instance;
            this.mission = mission;
            heroXpGainPairs = new ConcurrentQueue<Tuple<Hero, double>>();

            Debug.Print("[BattleRegeneration] Mission started, data initialized");
            Debug.Print($"[BattleRegeneration] Debug mode on, dumping settings: " +
                $"medicine boost: {settings.RegenAmount}, regen model: {settings.MedicineBoost}, commander medicine boost: {settings.CommanderMedicineBoost}, " +
                $"xp gain: {settings.XpGain}, commander xp gain: {settings.CommanderXpGain}, " +
                $"regen in percent HP: player:{settings.RegenAmount}, companions:{settings.RegenAmountCompanions}, allied heroes:{settings.RegenAmountAllies}, " +
                $"party troops:{settings.RegenAmountPartyTroops}, allied troops:{settings.RegenAmountAlliedTroops}, enemy heroes:{settings.RegenAmountEnemies}, " +
                $"enemy troops:{settings.RegenAmountEnemyTroops}, animals:{settings.RegenAmountAnimals}");
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.AddComponent(new BattleRegenerationComponent(agent, mission, this));
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            OnAgentDeleted(affectedAgent);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            BattleRegenerationComponent component = affectedAgent.GetComponent<BattleRegenerationComponent>();
            if (component != default)
                affectedAgent.RemoveComponent(component);
        }

        public override void OnMissionTick(float dt)
        {
            //if (mission.MissionEnded() || mission.IsMissionEnding)
            //    return;
            //else
            //{
            //    var arenaController = mission.GetMissionBehaviour<ArenaPracticeFightMissionController>();
            //    if (arenaController != default && arenaController.AfterPractice) return;
            //}

            //// Multi-threading work mk4
            //Queue<Agent> agents = new Queue<Agent>(mission.Agents);
            //List<Task> tasks = new List<Task>();

            //while (agents.Count > 0)
            //{
            //    Agent agent = agents.Dequeue();
            //    Tuple<Agent, float> agentHpPair;
            //    lock (agentHpPairs.SyncRoot)
            //    {
            //        agentHpPair = agentHpPairs.FirstOrDefault(x => x.Item1 == agent);
            //        if (agentHpPair == default)
            //        {
            //            agentHpPair = new Tuple<Agent, float>(agent, agent.Health);
            //            agentHpPairs.Add(agentHpPair);
            //        }
            //    }
            //    tasks.Add(Task.Run(() => OnAction(agent, agentHpPair.Item2, dt)));
            //}

            //foreach (Task task in tasks)
            //    task.Wait();

            mission.MainAgent?.GetComponent<BattleRegenerationComponent>()?.OnTick(dt);
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
        }

        public void GiveXpToHealers(Agent agent, Team agentTeam, Healer healers, double regenAmount)
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

            Debug.Print("[BattleRegeneration] Mission reset, clearing existing data");
        }
    }

    [Flags]
    enum Healer
    {
        General = 1,
        Self = 2,
        Rider = 4
    }
}
