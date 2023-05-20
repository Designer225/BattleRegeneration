using Microsoft.CodeAnalysis.Operations;
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
                    switch(GetTroopType())
                    {
                        case TroopType.Mount:
                        case TroopType.Animal:
                            Regenerate(settings.RegenAmountAnimals, dt, Agent.MountAgent?.Team);
                            break;
                        case TroopType.Player:
                            Regenerate(settings.RegenAmount, dt);
                            break;
                        case TroopType.Subordinate:
                            Regenerate(settings.RegenAmountCompanions, dt);
                            break;
                        case TroopType.PlayerTroop:
                            Regenerate(settings.RegenAmountPartyTroops, dt);
                            break;
                        case TroopType.AlliedHero:
                            Regenerate(settings.RegenAmountAllies, dt);
                            break;
                        case TroopType.AlliedTroop:
                            Regenerate(settings.RegenAmountAlliedTroops, dt);
                            break;
                        case TroopType.IndependentHero:
                        case TroopType.EnemyHero:
                            Regenerate(settings.RegenAmountEnemies, dt);
                            break;
                        case TroopType.IndependentTroop:
                        case TroopType.EnemyTroop:
                        default:
                            Regenerate(settings.RegenAmountEnemyTroops, dt);
                            break;
                    }
                }
                catch (Exception e)
                {
                    behavior.messages.Enqueue($"[BattleRegeneration] An exception has occurred attempting to heal {Agent.Name}. Will try again next tick.\nException: {e}");
                }
            }
        }

        private void Regenerate(float ratePercent, float dt, Team agentTeam = null)
        {
            if (agentTeam == null) agentTeam = Agent.Team;

            if (Agent.Health > 0f && Agent.Health < healthLimit)
            {
                var (modifier, healers) = GetHealthModifier(agentTeam);
                float baseRegenRate = ratePercent / 100f * Agent.HealthLimit; // regen rate is always based on all-time health limit
                float regenRate = ApplyRegenModel(baseRegenRate, modifier);
                float regenAmount = regenRate * dt;

                if (Agent.Health + regenAmount >= healthLimit)
                    Agent.Health = healthLimit;
                else
                    Agent.Health += (float)regenAmount;

                if (Game.Current.GameType is Campaign)
                    behavior.GiveXpToHealers(Agent, agentTeam, healers, regenAmount);
                if (settings.VerboseDebug)
                    behavior.messages.Enqueue($"[BattleRegeneration] {agentTeam} agent {Agent.Name} health: {Agent.Health}, health limit: {healthLimit}, " +
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

        private (float, Healer) GetHealthModifier(Team agentTeam)
        {
            Healer healers = 0;
            float modifier = 1f;
            float percentMedBoost = settings.MedicineBoost / 100f;

            if (agentTeam != null && agentTeam.GeneralAgent != null)
            {
                modifier += agentTeam.GeneralAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * settings.CommanderMedicineBoost / 100f;
                healers |= Healer.General;
            }
            if (Agent.Monster.FamilyType == HumanFamilyType) // Since only humans have skills...
            {
                modifier += Agent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                healers |= Healer.Self;
            }
            else if (Agent.IsMount && Agent.MountAgent != null)
            {
                modifier += Agent.MountAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50f * percentMedBoost;
                healers |= Healer.Rider;
            }

            if (settings.VerboseDebug)
                behavior.messages.Enqueue(string.Format("[BattleRegeneration] {0} agent {1} is receiving a {2} multiplier in health regeneration",
                    agentTeam, Agent.Name, modifier));
            return (modifier, healers);
        }

        private TroopType GetTroopType()
        {
            try
            {
                if (Agent.IsMount) return TroopType.Mount;
                else if (Agent.Monster.FamilyType != HumanFamilyType) return TroopType.Animal;
                else if (Agent.IsPlayerControlled) return TroopType.Player;
                else if (Agent.Team == default)
                {
                    if (Agent.IsHero) return TroopType.IndependentHero;
                    else return TroopType.IndependentTroop;
                }
                else if (Agent.Team.IsPlayerTeam)
                {
                    if (Agent.IsHero) return TroopType.Subordinate;
                    else return TroopType.PlayerTroop;
                }
                else if (Agent.Team.IsPlayerAlly)
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
            catch (Exception e)
            {
                Debug.Print($"[BattleRegen]An error has occurred retriving troop type. None will be returned; note this is not normal behavior.\n{e}");
                return TroopType.None;
            }
        }

        private enum TroopType
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
}
