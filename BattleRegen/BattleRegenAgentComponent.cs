using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    internal class BattleRegenAgentComponent : AgentComponent
    {
        private const int HumanFamilyType = 0;

        private readonly Stack<(Hero, float)> _xpGains;
        private readonly Queue<string> _messages;
        private readonly IBattleRegenSettings _settings;

        public float HealthLimit { get; }
        public float TimeSinceLastAttack { get; private set; }
        public AgentType AgentType { get; internal set; }

        public BattleRegenAgentComponent(Agent agent) : base(agent)
        {
            _settings = BattleRegenSettingsUtil.Instance;
            HealthLimit = _settings.HealToFull ? agent.HealthLimit : agent.Health;
            TimeSinceLastAttack = 0f;
            AgentType = default; // make the compiler happy
            _xpGains = new Stack<(Hero, float)>();
            _messages = new Queue<string>();
            UpdateAgentType();

            if (_settings.Debug)
                Debug.Print($"[BattleRegeneration] Initial {Agent.Name} ({Agent.GetHashCode()}) information: " +
                    $"HealthLimit {HealthLimit}, AgentType {AgentType}, initial team {Agent.Team}");
        }

        public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
            => TimeSinceLastAttack = 0f;

        public void TransferInformation(Dictionary<Hero, float> heroXpGainPairs, Queue<string> messages)
        {
            while (_xpGains.Count > 0)
            {
                var item = _xpGains.Pop();
                var check = heroXpGainPairs.TryGetValue(item.Item1, out var xp);
                heroXpGainPairs[item.Item1] = item.Item2 + (check ? xp : 0);
            }
            while (_messages.Count > 0)
                messages.Enqueue(_messages.Dequeue());
        }

        internal void OnTick(float dt)
        {
            TimeSinceLastAttack += dt;
            if (TimeSinceLastAttack < _settings.DelayedRegenTime) return;

            if (Agent.Health < Agent.HealthDyingThreshold) return;
            if (Agent.Health > HealthLimit) return;

            try
            {
                switch (AgentType)
                {
                    case AgentType.Mount:
                    case AgentType.Animal:
                        Regenerate(_settings.RegenAmountAnimals, dt);
                        break;
                    case AgentType.Player:
                        Regenerate(_settings.RegenAmount, dt);
                        break;
                    case AgentType.Companion:
                        Regenerate(_settings.RegenAmountCompanions, dt);
                        break;
                    case AgentType.Subordinate:
                        Regenerate(_settings.RegenAmountSubordinates, dt);
                        break;
                    case AgentType.PlayerTroop:
                        Regenerate(_settings.RegenAmountPartyTroops, dt);
                        break;
                    case AgentType.AlliedHero:
                        Regenerate(_settings.RegenAmountAllies, dt);
                        break;
                    case AgentType.AlliedTroop:
                        Regenerate(_settings.RegenAmountAlliedTroops, dt);
                        break;
                    case AgentType.IndependentHero:
                    case AgentType.EnemyHero:
                        Regenerate(_settings.RegenAmountEnemies, dt);
                        break;
                    case AgentType.IndependentTroop:
                    case AgentType.EnemyTroop:
                    default:
                        Regenerate(_settings.RegenAmountEnemyTroops, dt);
                        break;
                }
            }
            catch (Exception e)
            {
                _messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to heal {Agent.Name}. Will try again next tick.\nException: {e}");
            }
        }

        private void Regenerate(float ratePercent, float dt)
        {
            if (Agent.Health > 0f && Agent.Health < HealthLimit)
            {
                var modifier = GetHealthModifier();
                float baseRegenRate = ratePercent / 100f * Agent.HealthLimit; // regen rate is always based on all-time health limit
                float regenRate = ApplyRegenModel(baseRegenRate, modifier);
                float regenAmount = regenRate * dt;

                if (Agent.Health + regenAmount >= HealthLimit)
                    Agent.Health = HealthLimit;
                else
                    Agent.Health += (float)regenAmount;

                if (Game.Current.GameType is Campaign)
                    GiveXpToHealers(Agent, regenAmount, _settings);
                if (_settings.VerboseDebug)
                    _messages.Enqueue(
                        $"[BattleRegeneration] {Enum.GetName(typeof(AgentType), AgentType)} agent {Agent.Name} health: {Agent.Health}, health limit: {HealthLimit}, " +
                        $"health added: {regenAmount} (base: {baseRegenRate * dt}, multiplier: {modifier}), dt: {dt}");
            }
        }

        private float ApplyRegenModel(float baseRegenRate, float modifier)
        {
            float regenRate = baseRegenRate * modifier;
            float regenTime = HealthLimit / regenRate;
            float origRegenTime = Agent.HealthLimit / regenRate;

            try
            {
                var data = new RegenDataInfo(Agent, HealthLimit, regenRate, regenTime, origRegenTime);
                regenRate = _settings.RegenModel.Calculate(ref data);
            }
            catch (Exception e)
            {
                _messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to calculate regen value for {Agent.Name}. Using linear instead.\nException: {e}");
            }

            return regenRate;
        }

        private float GetHealthModifier()
        {
            float modifier = 1f;
            float percentMedBoost = _settings.MedicineBoost / 100f;

            // rewrite
            // for mounts, get rider skills
            // for humans, get unit skills, then commander skills - for now general only, but maybe sergeant later on
            switch (AgentType)
            {
                case AgentType.Mount:
                case AgentType.Animal:
                    if (Agent.MountAgent != default) modifier += Agent.MountAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                    break;
                default:
                    modifier += Agent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                    switch (AgentType)
                    {
                        case AgentType.Player:
                        case AgentType.Companion:
                        case AgentType.Subordinate:
                        case AgentType.PlayerTroop:
                            modifier += Agent.Main.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * _settings.CommanderMedicineBoost / 100f;
                            break;
                        case AgentType.AlliedHero:
                        case AgentType.AlliedTroop:
                        case AgentType.EnemyHero:
                        case AgentType.EnemyTroop:
                            if (Agent.Team?.GeneralAgent == null) break;
                            modifier += Agent.Team.GeneralAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * _settings.CommanderMedicineBoost / 100f;
                            break;
                    }
                    break;
            }

            if (_settings.VerboseDebug)
                _messages.Enqueue(
                    $"[BattleRegeneration] {Enum.GetName(typeof(AgentType), AgentType)} agent {Agent.Name} is receiving a {modifier} multiplier in health regeneration");
            return modifier;
        }

        private void GiveXpToHealers(Agent agent, float regenAmount, IBattleRegenSettings settings)
        {
            float xpGain = regenAmount / agent.HealthLimit; // xp gain is also based on all-time health limit

            switch (AgentType)
            {
                case AgentType.Mount:
                case AgentType.Animal:
                    float riderXpGain = xpGain * settings.XpGain;
                    var rider = (agent.MountAgent?.Character as CharacterObject)?.HeroObject;
                    if (rider != null)
                    {
                        _xpGains.Push((rider, riderXpGain));
                        if (settings.VerboseDebug && rider != null)
                            _messages.Enqueue($"[BattleRegeneration] rider agent {rider} has received {riderXpGain} xp");
                    }

                    break;
                default:
                    float selfXpGain = xpGain * settings.XpGain;
                    var hero = (agent.Character as CharacterObject)?.HeroObject;
                    if (hero != null)
                    {
                        _xpGains.Push((hero, selfXpGain));
                        if (settings.VerboseDebug)
                            _messages.Enqueue($"[BattleRegeneration] agent {agent.Name} has received {selfXpGain} xp");
                    }

                    float cdrXpGain = xpGain * settings.CommanderXpGain;
                    var commander = default(Hero);
                    switch (AgentType)
                    {
                        case AgentType.Player:
                        case AgentType.Companion:
                        case AgentType.Subordinate:
                        case AgentType.PlayerTroop:
                            commander = (Agent.Main?.Character as CharacterObject)?.HeroObject;
                            commander ??= (Mission.Current?.MainAgent?.Character as CharacterObject)?.HeroObject;
                            commander ??= (Mission.Current?.PlayerTeam?.ActiveAgents?.Find(x => x.IsPlayerUnit)?.Character as CharacterObject)?.HeroObject;
                            commander ??= Hero.MainHero;
                            break;
                        case AgentType.AlliedHero:
                        case AgentType.AlliedTroop:
                        case AgentType.EnemyHero:
                        case AgentType.EnemyTroop:
                            commander = (agent.Team?.GeneralAgent?.Character as CharacterObject)?.HeroObject;
                            break;
                    }
                    if (commander != default)
                    {
                        _xpGains.Push((commander, cdrXpGain));
                        if (settings.VerboseDebug)
                            _messages.Enqueue($"[BattleRegeneration] commander agent {commander.Name} has received {cdrXpGain} xp");
                    }
                    break;
            }
        }

        public void UpdateAgentType()
        {
            try
            {
                // rewrite
                // if agent is animal or mount, return animal or mount
                // if team is null, return independent
                // units in player-commanded teams are subordinates or player troops
                // units in other teams are handled accordingly
                if (Agent.IsMount) AgentType = AgentType.Mount;
                else if (Agent.Monster.FamilyType != HumanFamilyType) AgentType = AgentType.Animal;
                else if (Agent.IsPlayerUnit) AgentType = AgentType.Player;
                else if (Agent.IsHero && Agent.Character is CharacterObject chObj && chObj.HeroObject.IsPlayerCompanion)
                    AgentType = AgentType.Companion;
                else
                {
                    var team = Agent.Team;
                    if (team == null || !team.IsValid)
                    {
                        if (Agent.IsHero) AgentType = AgentType.IndependentHero;
                        else AgentType = AgentType.IndependentTroop;
                    }
                    else if (team.IsPlayerTeam)
                    {
                        if (team.IsPlayerGeneral || Agent.Formation.PlayerOwner != default && Agent.Formation.PlayerOwner.IsPlayerUnit)
                        {
                            if (Agent.IsHero) AgentType = AgentType.Subordinate;
                            else AgentType = AgentType.PlayerTroop;
                        }
                        else
                        {
                            if (Agent.IsHero) AgentType = AgentType.AlliedHero;
                            else AgentType = AgentType.AlliedTroop;
                        }
                    }
                    else if (team.IsPlayerAlly)
                    {
                        if (Agent.IsHero) AgentType = AgentType.AlliedHero;
                        else AgentType = AgentType.AlliedTroop;
                    }
                    else
                    {
                        if (Agent.IsHero) AgentType = AgentType.EnemyHero;
                        else AgentType = AgentType.EnemyTroop;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print($"[BattleRegen]An error has occurred retriving troop type. None will be returned; note this is not normal behavior.\n{e}");
                AgentType = AgentType.None;
            }
            if (_settings.Debug)
                Debug.Print($"[BattleRegen] Agent {Agent.Name} {Agent.GetHashCode()} is now AgentType {AgentType}");
        }
    }

    internal enum AgentType
    {
        None,
        Mount,
        Animal,
        Player,
        Companion,
        IndependentHero,
        IndependentTroop,
        Subordinate,
        PlayerTroop,
        AlliedHero,
        AlliedTroop,
        EnemyHero,
        EnemyTroop
    }
}
