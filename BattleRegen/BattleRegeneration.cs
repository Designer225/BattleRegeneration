using SandBox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    sealed class BattleRegeneration : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        private readonly IBattleRegenSettings settings;
        private readonly ConcurrentQueue<Tuple<Hero, float>> heroXpGainPairs;
        private readonly Dictionary<Agent, BattleRegenerationComponent> activeAgents;
        internal readonly ConcurrentQueue<string> messages;

        public BattleRegeneration()
        {
            settings = BattleRegenSettingsUtil.Instance;
            heroXpGainPairs = new ConcurrentQueue<Tuple<Hero, float>>();
            activeAgents = new Dictionary<Agent, BattleRegenerationComponent>(2048); // default max agent cap without mods
            messages = new ConcurrentQueue<string>();

            Debug.Print("[BattleRegeneration] Mission started, data initialized");
            Debug.Print($"[BattleRegeneration] Debug mode on, dumping settings: regen mode: {settings.RegenModel}, " +
                $"medicine boost: {settings.RegenAmount}, regen model: {settings.MedicineBoost}, commander medicine boost: {settings.CommanderMedicineBoost}, " +
                $"xp gain: {settings.XpGain}, commander xp gain: {settings.CommanderXpGain}, " +
                $"regen in percent HP: player:{settings.RegenAmount}, companions:{settings.RegenAmountCompanions}, allied heroes:{settings.RegenAmountAllies}, " +
                $"party troops:{settings.RegenAmountPartyTroops}, allied troops:{settings.RegenAmountAlliedTroops}, enemy heroes:{settings.RegenAmountEnemies}, " +
                $"enemy troops:{settings.RegenAmountEnemyTroops}, animals:{settings.RegenAmountAnimals}");
        }

        public override void OnAgentCreated(Agent agent)
        {
            var comp = new BattleRegenerationComponent(agent, this);
            activeAgents.Add(agent, comp);
            agent.AddComponent(comp);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            OnAgentDeleted(affectedAgent);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            if (activeAgents.TryGetValue(affectedAgent, out var component))
            {
                affectedAgent.RemoveComponent(component);
                activeAgents.Remove(affectedAgent);
            }
        }

        public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            if (victim != null && activeAgents.TryGetValue(victim, out var comp)) comp.TickHeal();
            if (attacker != null)
            {
                if (activeAgents.TryGetValue(attacker, out comp)) comp.TickHeal();
                if (attacker.MountAgent != null && activeAgents.TryGetValue(attacker.MountAgent, out comp)) comp.TickHeal();
            }
        }

        public override void OnMissionTick(float dt)
        {
            var arenaController = Mission.GetMissionBehavior<ArenaPracticeFightMissionController>();
            if (arenaController != default && arenaController.AfterPractice) return;

            Parallel.ForEach(activeAgents, kv => kv.Value.AttemptRegeneration(dt));
            while (!messages.IsEmpty)
            {
                if (messages.TryDequeue(out var msg))
                    Debug.Print(msg);
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();

            while (!heroXpGainPairs.IsEmpty)
            {
                if (!heroXpGainPairs.TryDequeue(out Tuple<Hero, float> heroXpGainPair)) continue;

                try
                {
                    if (heroXpGainPair.Item1 != default)
                    {
                        heroXpGainPair.Item1.AddSkillXp(DefaultSkills.Medicine, heroXpGainPair.Item2);
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

        public void GiveXpToHealers(Agent agent, Team agentTeam, Healer healers, float regenAmount)
        {
            float xpGain = regenAmount / agent.HealthLimit; // xp gain is also based on all-time health limit

            if ((healers & Healer.General) == Healer.General && agentTeam.GeneralAgent.IsHero)
            {
                float cdrXpGain = xpGain * settings.CommanderXpGain;
                Hero commander = (agentTeam.GeneralAgent.Character as CharacterObject).HeroObject;
                heroXpGainPairs.Enqueue(new Tuple<Hero, float>(commander, cdrXpGain));

                if (settings.Debug)
                    messages.Enqueue($"[BattleRegeneration] commander agent {agentTeam.GeneralAgent.Name} has received {cdrXpGain} xp");
            }
            if ((healers & Healer.Self) == Healer.Self && agent.IsHero)
            {
                float selfXpGain = xpGain * settings.XpGain;
                Hero hero = (agent.Character as CharacterObject).HeroObject;
                heroXpGainPairs.Enqueue(new Tuple<Hero, float>(hero, selfXpGain));

                if (settings.Debug)
                    messages.Enqueue($"[BattleRegeneration] agent {agent.Name} has received {selfXpGain} xp");
            }
            if ((healers & Healer.Rider) == Healer.Rider && agent.MountAgent.IsHero)
            {
                float riderXpGain = xpGain * settings.XpGain;
                Hero rider = (agent.MountAgent.Character as CharacterObject).HeroObject;
                heroXpGainPairs.Enqueue(new Tuple<Hero, float>(rider, riderXpGain));

                if (settings.Debug)
                    messages.Enqueue($"[BattleRegeneration] rider agent {agent.MountAgent.Name} has received {riderXpGain} xp");
            }
        }

        public override void OnMissionRestart()
        {
            base.OnMissionRestart();

            while (!heroXpGainPairs.IsEmpty)
                heroXpGainPairs.TryDequeue(out _);
            while (!messages.IsEmpty)
                messages.TryDequeue(out _);
            activeAgents.Clear();

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
