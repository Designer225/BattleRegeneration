using TaleWorlds.Localization;

namespace BattleRegen
{
    partial class BattleRegenSettings
    {
        internal const string ModNameText   = "{=BattleRegen_ModNameText}Battle Regeneration",

            RegenValuesName                 = "{=BattleRegen_RegenValuesName}Regeneration Values",
            RegenAmountName                 = "{=BattleRegen_RegenAmountName}Player Regen Amount (Percents) Per Second",
            RegenAmountHint                 = "{=BattleRegen_RegenAmountHint}What percent of total health to be regenerated every second. (Regen is applied continuously with this mod.) Default is 1%.",
            RegenAmountCompanionsName       = "{=BattleRegen_RegenAmountCompanionsName}Companion Regen Amount (Percents) Per Second",
            RegenAmountAlliesName           = "{=BattleRegen_RegenAmountAlliesName}Allied Hero Regen Amount (Percents) Per Second",
            RegenAmountPartyTroopsName      = "{=BattleRegen_RegenAmountPartyTroopsName}Party Troop Regen Amount (Percents) Per Second",
            RegenAmountAlliedTroopsName     = "{=BattleRegen_RegenAmountAlliedTroopsName}Allied Troop Regen Amount (Percents) Per Second",
            RegenAmountEnemiesName          = "{=BattleRegen_RegenAmountEnemiesName}Enemy Hero Regen Amount (Percents) Per Second",
            RegenAmountEnemyTroopsName      = "{=BattleRegen_RegenAmountEnemyTroopsName}Enemy Troop Regen Amount (Percents) Per Second",
            RegenAmountAnimalsName          = "{=BattleRegen_RegenAmountAnimalsName}Animal Regen Amount (Percents) Per Second",

            MedicineBoostName               = "{=BattleRegen_MedicineBoostName}Medicine Boost (Percents)",
            MedicineBoostHint               = "{=BattleRegen_MedicineBoostHint}Regen is increased by this percentage for every 50 points in a human agent's medicine skill. Riders affect regen for their mounts at standard efficiency. Bonuses stack additively. Default is 50%.",
            CommanderMedicineBoostName      = "{=BattleRegen_CommanderMedicineBoostName}Medicine Boost (Percents) For Commanders",
            CommanderMedicineBoostHint      = "{=BattleRegen_CommanderMedicineBoostHint}Regen is increased by this percentage for every 50 points in a commander's medicine skills. Bonuses stack additively. Default is 25%.",
            XpGainName                      = "{=BattleRegen_XpGainName}XP Gain Per Full Health",
            XpGainHint                      = "{=BattleRegen_XpGainHint}How much XP is gained when an agent or its rider (if applicable) heals enough to refill its health bar from 0 to max health. XP gain is continuous. Default is 5.",
            CommanderXpGainName             = "{=BattleRegen_CommanderXpGainName}XP Gain Per Full Health For Commanders",
            CommanderXpGainHint             = "{=BattleRegen_CommanderXpGainHint}How much XP is gained when a commander heals an agent enough to refill its health bar from 0 to max health. XP gain is continuous. Default is 0.5.",
            
            RegenSettingsName               = "{=BattleRegen_RegenSettingsName}Regeneration Settings",
            RegenModelDropdownName          = "{=BattleRegen_RegenModelDropdownName}Regeneration Model",
            RegenModelDropdownHint          = "{=BattleRegen_RegenModelDropdownHint}Determines the model used for regenerating health. See Nexus Mods page (https://www.nexusmods.com/mountandblade2bannerlord/mods/1432) for more details. Default is Linear.",
            HealToFullName                  = "{=BattleRegen_HealToFullName}Heal to Max Health",
            HealToFullHint                  = "{=BattleRegen_HealToFullHint}Enable to allow healing to max health, beyond your character's starting health in battle. Default is disabled. (Note: settings will not take effect until after (re)starting a battle.)",
            DebugName                       = "{=BattleRegen_DebugName}Debug Mode",
            DebugHint                       = "{=BattleRegen_DebugHint}Whether to print debug outputs to log (in C:/ProgramData/Mount and Blade II Bannerlord/logs folder). Default is false.";

            //UseSliderForRegenModelName      = "{=BattleRegen_UseSliderForRegenModelName}Use Slider for Regen Model",
            //UseSliderForRegenModelHint      = "{=BattleRegen_UseSliderForRegenModelHint}Should the mod use a slider for regen instead of dropdown options.",
            //SliderRegenModelName            = "{=BattleRegen_SliderRegenModelName}Regeneration Model",
            //SliderRegenModelHint            = "{=BattleRegen_SliderRegenModelHint}Determines the model used for regenerating health. 'Linear' is 1, 'Quadratic' is 2, and 'EVE Online' is 3. See Nexus Mods page (https://www.nexusmods.com/mountandblade2bannerlord/mods/1432) for more details. Default is Linear.";

        private static readonly TextObject ModNameTextObject = new TextObject(ModNameText);
    }
}
