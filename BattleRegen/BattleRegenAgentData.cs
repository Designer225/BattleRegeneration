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
    internal struct BattleRegenAgentData
    {
        private const int HumanFamilyType = 0;

        public readonly Agent Agent { get;  }
        public readonly float HealthLimit { get; }
        public float TimeSinceLastAttack { get; private set; }

        public BattleRegenAgentData(Agent agent)
        {
            Agent = agent;
            var settings = BattleRegenSettingsUtil.Instance;
            HealthLimit = settings.HealToFull ? agent.HealthLimit : agent.Health;
            TimeSinceLastAttack = 0f;
        }

        internal void TickHeal() => TimeSinceLastAttack = 0f;

        internal (Queue<string>? messages, Stack<(Hero, float)>? xpGains) AttemptRegeneration(float dt, IBattleRegenSettings settings)
        {
            TimeSinceLastAttack += dt;
            if (TimeSinceLastAttack < settings.DelayedRegenTime) return (null, null);

            if (Agent.Health < Agent.HealthDyingThreshold) return (null, null);
            if (Agent.Health > HealthLimit) return (null, null);

            var messages = new Queue<string>();
            var xpGains = new Stack<(Hero, float)>();
            try
            {
                var troopType = GetTroopType(messages);
                switch (troopType)
                {
                    case TroopType.Mount:
                    case TroopType.Animal:
                        Regenerate(settings.RegenAmountAnimals, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.Player:
                        Regenerate(settings.RegenAmount, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.Companion:
                        Regenerate(settings.RegenAmountCompanions, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.Subordinate:
                        Regenerate(settings.RegenAmountSubordinates, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.PlayerTroop:
                        Regenerate(settings.RegenAmountPartyTroops, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.AlliedHero:
                        Regenerate(settings.RegenAmountAllies, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.AlliedTroop:
                        Regenerate(settings.RegenAmountAlliedTroops, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.IndependentHero:
                    case TroopType.EnemyHero:
                        Regenerate(settings.RegenAmountEnemies, dt, troopType, messages, xpGains, settings);
                        break;
                    case TroopType.IndependentTroop:
                    case TroopType.EnemyTroop:
                    default:
                        Regenerate(settings.RegenAmountEnemyTroops, dt, troopType, messages, xpGains, settings);
                        break;
                }
            }
            catch (Exception e)
            {
                messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to heal {Agent.Name}. Will try again next tick.\nException: {e}");
            }

            return (messages, xpGains);
        }

        private readonly void Regenerate(
            float ratePercent,
            float dt,
            TroopType troopType,
            Queue<string> messages,
            Stack<(Hero, float)> xpGains,
            IBattleRegenSettings settings)
        {
            if (Agent.Health > 0f && Agent.Health < HealthLimit)
            {
                var modifier = GetHealthModifier(troopType, messages, settings);
                float baseRegenRate = ratePercent / 100f * Agent.HealthLimit; // regen rate is always based on all-time health limit
                float regenRate = ApplyRegenModel(baseRegenRate, modifier, messages, settings);
                float regenAmount = regenRate * dt;

                if (Agent.Health + regenAmount >= HealthLimit)
                    Agent.Health = HealthLimit;
                else
                    Agent.Health += (float)regenAmount;

                if (Game.Current.GameType is Campaign)
                    GiveXpToHealers(Agent, troopType, regenAmount, messages, xpGains, settings);
                if (settings.VerboseDebug)
                    messages.Enqueue(
                        $"[BattleRegeneration] {Enum.GetName(typeof(TroopType), troopType)} agent {Agent.Name} health: {Agent.Health}, health limit: {HealthLimit}, " +
                        $"health added: {regenAmount} (base: {baseRegenRate * dt}, multiplier: {modifier}), dt: {dt}");
            }
        }

        private readonly float ApplyRegenModel(float baseRegenRate, float modifier, Queue<string> messages, IBattleRegenSettings settings)
        {
            float regenRate = baseRegenRate * modifier;
            float regenTime = HealthLimit / regenRate;
            float origRegenTime = Agent.HealthLimit / regenRate;

            try
            {
                var data = new RegenDataInfo(Agent, HealthLimit, regenRate, regenTime, origRegenTime);
                regenRate = settings.RegenModel.Calculate(ref data);
            }
            catch (Exception e)
            {
                messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to calculate regen value for {Agent.Name}. Using linear instead.\nException: {e}");
            }

            return regenRate;
        }

        private readonly float GetHealthModifier(TroopType troopType, Queue<string> messages, IBattleRegenSettings settings)
        {
            float modifier = 1f;
            float percentMedBoost = settings.MedicineBoost / 100f;

            // rewrite
            // for mounts, get rider skills
            // for humans, get unit skills, then commander skills - for now general only, but maybe sergeant later on
            switch (troopType)
            {
                case TroopType.Mount:
                case TroopType.Animal:
                    if (Agent.MountAgent != default) modifier += Agent.MountAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                    break;
                default:
                    modifier += Agent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                    switch (troopType)
                    {
                        case TroopType.Player:
                        case TroopType.Companion:
                        case TroopType.Subordinate:
                        case TroopType.PlayerTroop:
                            modifier += Agent.Main.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * settings.CommanderMedicineBoost / 100f;
                            break;
                        case TroopType.AlliedHero:
                        case TroopType.AlliedTroop:
                        case TroopType.EnemyHero:
                        case TroopType.EnemyTroop:
                            if (Agent.Team?.GeneralAgent == null) break;
                            modifier += Agent.Team.GeneralAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * settings.CommanderMedicineBoost / 100f;
                            break;
                    }
                    break;
            }

            if (settings.VerboseDebug)
                messages.Enqueue(
                    $"[BattleRegeneration] {Enum.GetName(typeof(TroopType), troopType)} agent {Agent.Name} is receiving a {modifier} multiplier in health regeneration");
            return modifier;
        }

        public readonly void GiveXpToHealers(
            Agent agent,
            TroopType troopType,
            float regenAmount,
            Queue<string> messages,
            Stack<(Hero, float)> xpGains,
            IBattleRegenSettings settings)
        {
            float xpGain = regenAmount / agent.HealthLimit; // xp gain is also based on all-time health limit

            switch (troopType)
            {
                case TroopType.Mount:
                case TroopType.Animal:
                    float riderXpGain = xpGain * settings.XpGain;
                    Hero? rider = (agent.MountAgent.Character as CharacterObject)?.HeroObject;
                    if (rider != null)
                        xpGains.Push((rider, riderXpGain));

                    if (settings.VerboseDebug)
                        messages.Enqueue($"[BattleRegeneration] rider agent {agent.MountAgent.Name} has received {riderXpGain} xp");
                    break;
                default:
                    float selfXpGain = xpGain * settings.XpGain;
                    Hero? hero = (agent.Character as CharacterObject)?.HeroObject;
                    if (hero != null)
                        xpGains.Push((hero, selfXpGain));
                    if (settings.VerboseDebug)
                        messages.Enqueue($"[BattleRegeneration] agent {agent.Name} has received {selfXpGain} xp");

                    float cdrXpGain = xpGain * settings.CommanderXpGain;
                    Hero? commander = default;
                    switch (troopType)
                    {
                        case TroopType.Player:
                        case TroopType.Companion:
                        case TroopType.Subordinate:
                        case TroopType.PlayerTroop:
                            commander = (Agent.Main.Character as CharacterObject)?.HeroObject;
                            break;
                        case TroopType.AlliedHero:
                        case TroopType.AlliedTroop:
                        case TroopType.EnemyHero:
                        case TroopType.EnemyTroop:
                            commander = (agent.Team.GeneralAgent.Character as CharacterObject)?.HeroObject;
                            break;
                    }
                    if (commander != default)
                    {
                        xpGains.Push((commander, cdrXpGain));
                        if (settings.VerboseDebug)
                            messages.Enqueue($"[BattleRegeneration] commander agent {commander.Name} has received {cdrXpGain} xp");
                    }
                    break;
            }
        }

        private readonly TroopType GetTroopType(Queue<string> messages)
        {
            try
            {

                // rewrite
                // if agent is animal or mount, return animal or mount
                // if team is null, return independent
                // units in player-commanded teams are subordinates or player troops
                // units in other teams are handled accordingly
                if (Agent.IsMount) return TroopType.Mount;
                else if (Agent.Monster.FamilyType != HumanFamilyType) return TroopType.Animal;
                else if (Agent.IsPlayerUnit) return TroopType.Player;
                else if (Agent.IsHero && Agent.Character is CharacterObject chObj && chObj.HeroObject.IsPlayerCompanion)
                    return TroopType.Companion;
                else
                {
                    var team = Agent.Team;
                    if (team == null || !team.IsValid)
                    {
                        if (Agent.IsHero) return TroopType.IndependentHero;
                        else return TroopType.IndependentTroop;
                    }
                    else if (team.IsPlayerTeam)
                    {
                        if (team.IsPlayerGeneral || Agent.Formation.PlayerOwner != default && Agent.Formation.PlayerOwner.IsPlayerUnit)
                        {
                            if (Agent.IsHero) return TroopType.Subordinate;
                            else return TroopType.PlayerTroop;
                        }
                        else
                        {
                            if (Agent.IsHero) return TroopType.AlliedHero;
                            else return TroopType.AlliedTroop;
                        }
                    }
                    else if (team.IsPlayerAlly)
                    {
                        if (Agent.IsHero) return TroopType.AlliedHero;
                        else return TroopType.AlliedTroop;
                    }
                    else
                    {
                        if (Agent.IsHero) return TroopType.EnemyHero;
                        else return TroopType.EnemyTroop;
                    }
                }
            }
            catch (Exception e)
            {
                messages.Enqueue($"[BattleRegen]An error has occurred retriving troop type. None will be returned; note this is not normal behavior.\n{e}");
                return TroopType.None;
            }
        }
    }

    internal enum TroopType
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
