using Microsoft.CodeAnalysis.Operations;
using NetworkMessages.FromServer;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    class BattleRegenerationComponent : AgentComponent
    {
        private const int HumanFamilyType = 0;

        private readonly float healthLimit;
        private readonly IBattleRegenSettings settings;
        private readonly BattleRegeneration behavior;

        private float timeSinceLastAttack;

        public BattleRegenerationComponent(Agent agent, BattleRegeneration behavior) : base(agent)
        {
            settings = BattleRegenSettingsUtil.Instance;
            healthLimit = settings.HealToFull ? agent.HealthLimit : agent.Health;
            this.behavior = behavior;

            timeSinceLastAttack = 0f;

            if (settings.Debug)
            {
                var team = agent.Monster.FamilyType != HumanFamilyType ? agent.MountAgent?.Team : agent.Team;
                Debug.Print($"[BattleRegen] agent is classified as {Enum.GetName(typeof(TroopType), GetTroopType())} at the time of creation");
            }
        }

        internal void TickHeal() => timeSinceLastAttack = 0f;

        internal void AttemptRegeneration(float dt)
        {
            timeSinceLastAttack += dt;
            if (timeSinceLastAttack < settings.DelayedRegenTime) return;

            if (Agent.Health > 0 && Agent.Health < healthLimit)
            {
                try
                {
                    var troopType = GetTroopType();
                    switch (troopType)
                    {
                        case TroopType.Mount:
                        case TroopType.Animal:
                            Regenerate(settings.RegenAmountAnimals, dt, troopType);
                            break;
                        case TroopType.Player:
                            Regenerate(settings.RegenAmount, dt, troopType);
                            break;
                        case TroopType.Subordinate:
                            Regenerate(settings.RegenAmountCompanions, dt, troopType);
                            break;
                        case TroopType.PlayerTroop:
                            Regenerate(settings.RegenAmountPartyTroops, dt, troopType);
                            break;
                        case TroopType.AlliedHero:
                            Regenerate(settings.RegenAmountAllies, dt, troopType);
                            break;
                        case TroopType.AlliedTroop:
                            Regenerate(settings.RegenAmountAlliedTroops, dt, troopType);
                            break;
                        case TroopType.IndependentHero:
                        case TroopType.EnemyHero:
                            Regenerate(settings.RegenAmountEnemies, dt, troopType);
                            break;
                        case TroopType.IndependentTroop:
                        case TroopType.EnemyTroop:
                        default:
                            Regenerate(settings.RegenAmountEnemyTroops, dt, troopType);
                            break;
                    }
                }
                catch (Exception e)
                {
                    behavior.messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to heal {Agent.Name}. Will try again next tick.\nException: {e}");
                }
            }
        }

        private void Regenerate(float ratePercent, float dt, TroopType troopType)
        {
            if (Agent.Health > 0f && Agent.Health < healthLimit)
            {
                var modifier = GetHealthModifier(troopType);
                float baseRegenRate = ratePercent / 100f * Agent.HealthLimit; // regen rate is always based on all-time health limit
                float regenRate = ApplyRegenModel(baseRegenRate, modifier);
                float regenAmount = regenRate * dt;

                if (Agent.Health + regenAmount >= healthLimit)
                    Agent.Health = healthLimit;
                else
                    Agent.Health += (float)regenAmount;

                if (Game.Current.GameType is Campaign)
                    behavior.GiveXpToHealers(Agent, troopType, regenAmount);
                if (settings.VerboseDebug)
                    behavior.messages.Enqueue(
                        $"[BattleRegeneration] {Enum.GetName(typeof(TroopType), troopType)} agent {Agent.Name} health: {Agent.Health}, health limit: {healthLimit}, " +
                        $"health added: {regenAmount} (base: {baseRegenRate * dt}, multiplier: {modifier}), dt: {dt}");
            }
        }

        private float ApplyRegenModel(float baseRegenRate, float modifier)
        {
            float regenRate = baseRegenRate * modifier;
            float regenTime = healthLimit / regenRate;
            float origRegenTime = Agent.HealthLimit / regenRate;

            try
            {
                var data = new RegenDataInfo(Agent, healthLimit, regenRate, regenTime, origRegenTime);
                regenRate = settings.RegenModel.Calculate(ref data);
            }
            catch (Exception e)
            {
                behavior.messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to calculate regen value for {Agent.Name}. Using linear instead.\nException: {e}");
            }

            return regenRate;
        }

        private float GetHealthModifier(TroopType troopType)
        {
            float modifier = 1f;
            float percentMedBoost = settings.MedicineBoost / 100f;

            // rewrite
            // for mounts, get rider skills
            // for humans, get unit skills, then commander skills - for now general only, but maybe sergeant later on
            switch(troopType)
            {
                case TroopType.Mount:
                case TroopType.Animal:
                    if (Agent.MountAgent != default) modifier += Agent.MountAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                    break;
                default:
                    modifier += Agent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                    switch(troopType)
                    {
                        case TroopType.Player:
                        case TroopType.Subordinate:
                        case TroopType.PlayerTroop:
                            modifier += Agent.Main.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * settings.CommanderMedicineBoost / 100f;
                            break;
                        case TroopType.AlliedHero:
                        case TroopType.AlliedTroop:
                        case TroopType.EnemyHero:
                        case TroopType.EnemyTroop:
                            modifier += Agent.Team.GeneralAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * settings.CommanderMedicineBoost / 100f;
                            break;
                    }
                    break;
            }

            if (settings.VerboseDebug)
                behavior.messages.Enqueue(
                    $"[BattleRegeneration] {Enum.GetName(typeof(TroopType), troopType)} agent {Agent.Name} is receiving a {modifier} multiplier in health regeneration");
            return modifier;
        }

        private TroopType GetTroopType()
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
                else
                {
                    var team = Agent.Team;
                    if (team == default)
                    {
                        if (Agent.IsHero) return TroopType.IndependentHero;
                        else return TroopType.IndependentTroop;
                    }
                    else if (team.IsPlayerTeam)
                    {
                        if (team.IsPlayerGeneral || (Agent.Formation.PlayerOwner != default && Agent.Formation.PlayerOwner.IsPlayerUnit))
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
                Debug.Print($"[BattleRegen]An error has occurred retriving troop type. None will be returned; note this is not normal behavior.\n{e}");
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
