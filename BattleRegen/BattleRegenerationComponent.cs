using SandBox;
using System;
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
                    if (Agent.Monster.FamilyType != HumanFamilyType)
                    {
                        Regenerate(settings.RegenAmountAnimals, dt, Agent.MountAgent?.Team);
                    }
                    else if (Agent.IsPlayerControlled)
                    {
                        Regenerate(settings.RegenAmount, dt);
                    }
                    else
                    {
                        Team team = Agent.Team;
                        if (team == null)
                        {
                            if (Agent.IsHero) Regenerate(settings.RegenAmountEnemies, dt);
                            else Regenerate(settings.RegenAmountEnemyTroops, dt);
                        }
                        else if (team.IsPlayerTeam)
                        {
                            if (Agent.IsHero) Regenerate(settings.RegenAmountCompanions, dt, team);
                            else Regenerate(settings.RegenAmountPartyTroops, dt, team);
                        }
                        else if (team.IsPlayerAlly)
                        {
                            if (Agent.IsHero) Regenerate(settings.RegenAmountAllies, dt, team);
                            else Regenerate(settings.RegenAmountAlliedTroops, dt, team);
                        }
                        else
                        {
                            if (Agent.IsHero) Regenerate(settings.RegenAmountEnemies, dt, team);
                            else Regenerate(settings.RegenAmountEnemyTroops, dt, team);
                        }
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
                if (settings.Debug)
                    behavior.messages.Enqueue($"[BattleRegeneration] {GetTroopType(agentTeam)} agent {Agent.Name} health: {Agent.Health}, health limit: {healthLimit}, " +
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
                regenRate = settings.RegenModel.Calculate(new RegenDataInfo(Agent, healthLimit, regenRate, regenTime, origRegenTime));
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

            if (settings.Debug)
                behavior.messages.Enqueue(string.Format("[BattleRegeneration] {0} agent {1} is receiving a {2} multiplier in health regeneration",
                    GetTroopType(agentTeam), Agent.Name, modifier));
            return (modifier, healers);
        }

        private string GetTroopType(Team agentTeam)
        {
            if (Agent.IsMount) return "Mount";
            else if (Agent.Monster.FamilyType != HumanFamilyType) return "Animal";
            else if (Agent.IsPlayerControlled) return "Player";
            else if (agentTeam == null)
            {
                if (Agent.IsHero) return "Independent hero";
                else return "Independent troop";
            }
            else if (agentTeam.IsPlayerTeam)
            {
                if (Agent.IsHero) return "Companion";
                else return "Player troop";
            }
            else if (agentTeam.IsPlayerAlly)
            {
                if (Agent.IsHero) return "Allied hero";
                else return "allied troop";
            }
            else
            {
                if (Agent.IsHero) return "Enemy hero";
                else return "Enemy troop";
            }
        }
    }
}
