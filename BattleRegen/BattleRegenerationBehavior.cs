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
    sealed class BattleRegenerationBehavior : MissionBehavior
    {
        private const int AnticipatedAgentCount = 2048;

        private readonly IBattleRegenSettings _settings;
        private readonly Dictionary<Hero, float> _heroXpGainPairs;
        private readonly Dictionary<Agent, int> _agentIndexPairs;
        private BattleRegenAgentData[] _agentData;
        private Stack<(Agent, bool)> _toAddOrRemove;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public BattleRegenerationBehavior()
        {
            _settings = BattleRegenSettingsUtil.Instance;
            _heroXpGainPairs = new Dictionary<Hero, float>();
            _agentIndexPairs = new Dictionary<Agent, int>(AnticipatedAgentCount);
            _agentData = new BattleRegenAgentData[AnticipatedAgentCount];
            _toAddOrRemove = new Stack<(Agent, bool)>();

            Debug.Print("[BattleRegeneration] Mission started, data initialized");
            Debug.Print($"[BattleRegeneration] Debug mode on, dumping settings: regen mode: {_settings.RegenModel}, " +
                $"medicine boost: {_settings.RegenAmount}, regen model: {_settings.MedicineBoost}, commander medicine boost: {_settings.CommanderMedicineBoost}, " +
                $"xp gain: {_settings.XpGain}, commander xp gain: {_settings.CommanderXpGain}, " +
                $"regen in percent HP: player:{_settings.RegenAmount}, subordinate:{_settings.RegenAmountCompanions}, allied heroes:{_settings.RegenAmountAllies}, " +
                $"party troops:{_settings.RegenAmountPartyTroops}, allied troops:{_settings.RegenAmountAlliedTroops}, enemy heroes:{_settings.RegenAmountEnemies}, " +
                $"enemy troops:{_settings.RegenAmountEnemyTroops}, animals:{_settings.RegenAmountAnimals}");
        }

        private void EnsureCapacity(int add = 1)
        {
            int capacity = _agentIndexPairs.Count + add;
            if (_agentData.Length < capacity)
            {
                int newCapacity = _agentData.Length * 2;
                if (newCapacity < capacity) newCapacity = capacity;
                Array.Resize(ref  _agentData, newCapacity);
            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner) => _toAddOrRemove.Push((agent, true));

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
            => _toAddOrRemove.Push((affectedAgent, false));

        public override void OnAgentDeleted(Agent affectedAgent) => _toAddOrRemove.Push((affectedAgent, false));

        public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            if (victim != null && _agentIndexPairs.TryGetValue(victim, out var index))
                _agentData[index].TickHeal();
            if (b.SelfInflictedDamage == 1 && attacker != null)
            {
                if (_agentIndexPairs.TryGetValue(attacker, out index))
                    _agentData[index].TickHeal();
                if (attacker.MountAgent != null && _agentIndexPairs.TryGetValue(attacker.MountAgent, out index))
                    _agentData[index].TickHeal();
            }
        }

        public override void OnMissionTick(float dt)
        {
            while (_toAddOrRemove.Count > 0)
            {
                var (agent, addIfTrue) = _toAddOrRemove.Pop();
                if (addIfTrue) AddAgent(agent);
                else RemoveAgent(agent);
            }

            var arenaController = Mission.GetMissionBehavior<ArenaPracticeFightMissionController>();
            if (arenaController != default && arenaController.AfterPractice) return;

            int messageCount = 0;
            const int infoMessageCap = 10, messageCap = 100;
            foreach (var (messages, xpGains) in ParallelEnumerable.Range(0, _agentIndexPairs.Count).Select(x => _agentData[x].AttemptRegeneration(dt, _settings)))
            {
                if (messages != null)
                    for (; messageCount < messageCap; ++messageCount)
                        if (messages.Count > 0)
                        {
                            var message = messages.Dequeue();
                            if (messageCount < infoMessageCap) InformationManager.DisplayMessage(new InformationMessage(message));
                            Debug.Print(message);
                        }
                if (xpGains != null)
                {
                    while (xpGains.Count > 0)
                    {
                        var (hero, xp) = xpGains.Pop();
                        if (_heroXpGainPairs.ContainsKey(hero))
                            _heroXpGainPairs[hero] = _heroXpGainPairs[hero] + xp;
                        else _heroXpGainPairs[hero] = xp;
                    }
                }
            }
        }

        private void AddAgent(Agent agent)
        {
            EnsureCapacity();
            int index = _agentIndexPairs.Count;
            _agentIndexPairs[agent] = index;
            _agentData[index] = new BattleRegenAgentData(agent);

            if (_settings.Debug)
                Debug.Print($"[BattleRegen] agent {agent.Name} registered");
        }

        private void RemoveAgent(Agent agent)
        {
            if (_agentIndexPairs.TryGetValue(agent, out var index))
            {
                _agentIndexPairs.Remove(agent);
                ref var dataRef = ref _agentData[index];
                // since index < Count always, after removal from dictionary index <= Count always.
                // we do not need to swap if index == Count (since the last item removed is the last added)
                // otherwise we swap and update accordingly
                var lastIndex = _agentIndexPairs.Count;
                if (index < lastIndex)
                {
                    // swap with last item
                    ref var lastDataRef = ref _agentData[lastIndex];
                    dataRef = lastDataRef;
                    lastDataRef = default;
                    _agentIndexPairs[dataRef.Agent] = index;
                }
                else dataRef = default;

                if (_settings.Debug)
                    Debug.Print($"[BattleRegen] agent {agent.Name} unregistered");
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();

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
