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
        private readonly Mission mission;
        private readonly BattleRegeneration behavior;

        private bool healAgent;

        public BattleRegenerationComponent(Agent agent, Mission mission, BattleRegeneration behavior) : base(agent)
        {
            settings = BattleRegenSettingsUtil.Instance;
            healthLimit = settings.HealToFull ? agent.HealthLimit : agent.Health;
            this.mission = mission;
            this.behavior = behavior;

            healAgent = false;
        }

        internal void TickHeal() => healAgent = true;

        public override void OnTickAsAI(float dt)
        {
            //if (healAgent) await Task.Run(() => AttemptRegeneration(dt)).ConfigureAwait(false);
            if (healAgent) AttemptRegeneration(dt);
        }

        private void AttemptRegeneration(float dt)
        {
            //if (mission.MissionEnded() || mission.IsMissionEnding)
            //    return;
            //else
            //{
            //    var arenaController = mission.GetMissionBehaviour<ArenaPracticeFightMissionController>();
            //    if (arenaController != default && arenaController.AfterPractice) return;
            //}
            var arenaController = mission.GetMissionBehaviour<ArenaPracticeFightMissionController>();
            if (arenaController != default && arenaController.AfterPractice) return;

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
                    Debug.Print($"[BattleRegeneration] An exception has occurred attempting to heal {Agent.Name}. Will try again next tick.\nException: {e}");
                }
            }

            if (Agent.Health >= healthLimit) healAgent = false;
        }

        private void Regenerate(float ratePercent, float dt, Team agentTeam = null)
        {
            if (agentTeam == null) agentTeam = Agent.Team;

            if (Agent.Health > 0f && Agent.Health < healthLimit)
            {
                var (modifier, healers) = GetHealthModifier(agentTeam);
                double baseRegenRate = ratePercent / 100.0 * Agent.HealthLimit; // regen rate is always based on all-time health limit
                double regenRate = ApplyRegenModel(baseRegenRate, modifier);
                double regenAmount = regenRate * dt;

                if (Agent.Health + regenAmount >= healthLimit)
                    Agent.Health = healthLimit;
                else
                    Agent.Health += (float)regenAmount;

                if (Game.Current.GameType is Campaign)
                    behavior.GiveXpToHealers(Agent, agentTeam, healers, regenAmount);
                if (settings.Debug)
                    Debug.Print($"[BattleRegeneration] {GetTroopType(agentTeam)} agent {Agent.Name} health: {Agent.Health}, health limit: {healthLimit}, " +
                        $"health added: {regenAmount} (base: {baseRegenRate * dt}, multiplier: {modifier}), dt: {dt}");
            }
        }

        private double ApplyRegenModel(double baseRegenRate, double modifier)
        {
            double regenRate = baseRegenRate * modifier;
            double regenTime = healthLimit / regenRate;
            double origRegenTime = Agent.HealthLimit / regenRate;

            try
            {
                RegenDataInfo data = new RegenDataInfo(Agent, healthLimit, regenRate, regenTime, origRegenTime);
                regenRate = settings.RegenModel.Calculate(data);
            }
            catch (Exception e)
            {
                Debug.Print($"[BattleRegeneration] An exception has occurred attempting to calculate regen value for {Agent.Name}. Using linear instead.\nException: {e}");
            }

            return regenRate;
        }

        private (double, Healer) GetHealthModifier(Team agentTeam)
        {
            Healer healers = 0;
            double modifier = 1.0;
            double percentMedBoost = settings.MedicineBoost / 100.0;

            if (agentTeam != null && agentTeam.GeneralAgent != null)
            {
                modifier += agentTeam.GeneralAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50.0 * settings.CommanderMedicineBoost / 100.0;
                healers |= Healer.General;
            }
            if (Agent.Monster.FamilyType == HumanFamilyType) // Since only humans have skills...
            {
                modifier += Agent.Character.GetSkillValue(DefaultSkills.Medicine) / 50.0 * percentMedBoost;
                healers |= Healer.Self;
            }
            else if (Agent.IsMount && Agent.MountAgent != null)
            {
                modifier += Agent.MountAgent.Character.GetSkillValue(DefaultSkills.Medicine) / 50.0 * percentMedBoost;
                healers |= Healer.Rider;
            }

            if (settings.Debug)
                Debug.Print(string.Format("[BattleRegeneration] {0} agent {1} is receiving a {2} multiplier in health regeneration",
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
