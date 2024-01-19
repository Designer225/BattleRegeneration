using SandBox;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    sealed class BattleRegenMissionBehavior : MissionBehavior
    {
        private const int AnticipatedAgentCount = 2048;

        private readonly IBattleRegenSettings _settings;
        private readonly Dictionary<Hero, float> _heroXpGainPairs;
        private readonly Dictionary<Agent, BattleRegenAgentComponent> _agentComponents;
        private readonly Queue<string> _messages;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public BattleRegenMissionBehavior()
        {
            _settings = BattleRegenSettingsUtil.Instance;
            _heroXpGainPairs = new Dictionary<Hero, float>();
            _agentComponents = new Dictionary<Agent, BattleRegenAgentComponent>(AnticipatedAgentCount);
            _messages = new Queue<string>();

            Debug.Print("[BattleRegeneration] Mission started, data initialized");
            Debug.Print($"[BattleRegeneration] Debug mode on, dumping settings: regen mode: {_settings.RegenModel}, " +
                $"medicine boost: {_settings.RegenAmount}, regen model: {_settings.MedicineBoost}, commander medicine boost: {_settings.CommanderMedicineBoost}, " +
                $"xp gain: {_settings.XpGain}, commander xp gain: {_settings.CommanderXpGain}, " +
                $"regen in percent HP: player:{_settings.RegenAmount}, subordinate:{_settings.RegenAmountCompanions}, allied heroes:{_settings.RegenAmountAllies}, " +
                $"party troops:{_settings.RegenAmountPartyTroops}, allied troops:{_settings.RegenAmountAlliedTroops}, enemy heroes:{_settings.RegenAmountEnemies}, " +
                $"enemy troops:{_settings.RegenAmountEnemyTroops}, animals:{_settings.RegenAmountAnimals}");
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            var behavior = new BattleRegenAgentComponent(agent);
            agent.AddComponent(behavior);
            _agentComponents[agent] = behavior;
        }

        private void RemoveAgent(Agent agent)
        {
            if (_agentComponents.TryGetValue(agent, out var component))
            {
                agent.RemoveComponent(component);
                _agentComponents.Remove(agent);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
            => RemoveAgent(affectedAgent);

        public override void OnAgentDeleted(Agent affectedAgent)
            => RemoveAgent(affectedAgent);

        public override void OnMissionTick(float dt)
        {
            var arenaController = Mission.GetMissionBehavior<ArenaPracticeFightMissionController>();
            if (arenaController != default && arenaController.AfterPractice) return;
            _agentComponents.Values.AsParallel().ForAll(x => x.OnTick(dt));
            foreach (var component in _agentComponents.Values)
                component.TransferInformation(_heroXpGainPairs, _messages);
            while (_messages.Count > 0)
                Debug.Print(_messages.Dequeue());
        }

        public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
        {
            if (_agentComponents.TryGetValue(agent, out var component)) component.UpdateAgentType();
        }

        public override void OnClearScene()
        {
            _heroXpGainPairs.Clear();
            _agentComponents.Clear();
        }

        protected override void OnEndMission()
        {
            foreach (var kv in _heroXpGainPairs)
            {
                var (hero, xp) = (kv.Key, kv.Value);
                try
                {
                    if (hero != default)
                    {
                        hero.AddSkillXp(DefaultSkills.Medicine, xp);
                        if (_settings.Debug)
                            Debug.Print($"[BattleRegeneration] hero {hero.Name} has received {xp} xp from battle");
                    }
                }
                catch (Exception e)
                {
                    Debug.Print($"[BattleRegeneration] An error occurred attempting to add XP to a hero.\n{e}");
                }
            }
        }
    }
}
