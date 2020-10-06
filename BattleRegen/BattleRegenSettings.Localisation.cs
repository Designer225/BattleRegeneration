using TaleWorlds.Localization;

namespace BattleRegen
{
    partial class BattleRegenSettings
    {
        internal const string ModNameText   = "{=BattleRegen_ModNameText}Battle Regeneration",

            RegenValuesName                 = "{=BattleRegen_RegenValuesName}Regeneration Values",
            RegenAmountName                 = "{=BattleRegen_RegenAmountName}Regen Amount (Percents) Per Second",
            RegenAmountHint                 = "{=BattleRegen_RegenAmountHint}What percent of total health to be regenerated every second. (Regen is applied continuously with this mod.) Default is 1%.",
            MedicineBoostName               = "{=BattleRegen_MedicineBoostName}Medicine Boost (Percents)",
            MedicineBoostHint               = "{=BattleRegen_MedicineBoostHint}Regen is increased by this percentage for every 50 points in a human agent's medicine skill. Riders affect regen for their mounts at standard efficiency. Bonuses stack additively. Default is 50%.",
            CommanderMedicineBoostName      = "{=BattleRegen_CommanderMedicineBoostName}Medicine Boost (Percents) For Commanders",
            CommanderMedicineBoostHint      = "{=BattleRegen_CommanderMedicineBoostHint}Regen is increased by this percentage for every 50 points in a commander's medicine skills. Bonuses stack additively. Default is 25%.",
            XpGainName                      = "{=BattleRegen_XpGainName}XP Gain Per Full Health",
            XpGainHint                      = "{=BattleRegen_XpGainHint}How much XP is gained when an agent or its rider (if applicable) heals enough to refill its health bar from 0 to max health. XP gain is continuous. Default is 1.",
            CommanderXpGainName             = "{=BattleRegen_CommanderXpGainName}XP Gain Per Full Health For Commanders",
            CommanderXpGainHint             = "{=BattleRegen_CommanderXpGainHint}How much XP is gained when a commander heals an agent enough to refill its health bar from 0 to max health. XP gain is continuous. Default is 0.1.",
            
            RegenSettingsName               = "{=BattleRegen_RegenSettingsName}Regeneration Settings",
            RegenModelDropdownName          = "{=BattleRegen_RegenModelDropdownName}Regeneration Model",
            RegenModelDropdownHint          = "{=BattleRegen_RegenModelDropdownHint}Determines the model used for regenerating health. See Nexus Mods page (https://www.nexusmods.com/mountandblade2bannerlord/mods/1432) for more details. Default is Linear.",
            ApplyToPlayerName               = "{=BattleRegen_ApplyToPlayerName}Apply To Player",
            ApplyToPlayerHint               = "{=BattleRegen_ApplyToPlayerHint}Whether the player should receive passive regen. Default is true.",
            ApplyToCompanionsName           = "{=BattleRegen_ApplyToCompanionsName}Apply To Companions",
            ApplyToCompanionsHint           = "{=BattleRegen_ApplyToCompanionsHint}Whether the player's companions should receive passive regen. Default is true.",
            ApplyToAlliedHeroesName         = "{=BattleRegen_ApplyToAlliedHeroesName}Apply To Allied Heroes",
            ApplyToAlliedHeroesHint         = "{=BattleRegen_ApplyToAlliedHeroesHint}Whether the player's allied heroes should receive passive regen. Default is true.",
            ApplyToPartyTroopsName          = "{=BattleRegen_ApplyToPartyTroopsName}Apply To Party Troops",
            ApplyToPartyTroopsHint          = "{=BattleRegen_ApplyToPartyTroopsHint}Whether the player's troops should receive passive regen. Default is true.",
            ApplyToAlliedTroopsName         = "{=BattleRegen_ApplyToAlliedTroopsName}Apply To Allied Troops",
            ApplyToAlliedTroopsHint         = "{=BattleRegen_ApplyToAlliedTroopsHint}Whether the player's allied troops should receive passive regen. Default is true.",
            ApplyToEnemyHeroesName          = "{=BattleRegen_ApplyToEnemyHeroesName}Apply To Enemy Heroes",
            ApplyToEnemyHeroesHint          = "{=BattleRegen_ApplyToEnemyHeroesHint}Whether the player's enemy heroes should receive passive regen. Default is true.",
            ApplyToEnemyTroopsName          = "{=BattleRegen_ApplyToEnemyTroopsName}Apply To Enemy Troops",
            ApplyToEnemyTroopsHint          = "{=BattleRegen_ApplyToEnemyTroopsHint}Whether the player's enemy troops should receive passive regen. Default is true.",
            ApplyToAnimalName                = "{=BattleRegen_ApplyToAnimalName}Apply To Animals",
            ApplyToAnimalHint                = "{=BattleRegen_ApplyToAnimalHint}Whether animals should receive passive regen. Animals regenerate regardless of their rider's (if any) allegiance. Default is true.",
            DebugName                       = "{=BattleRegen_DebugName}Debug Mode",
            DebugHint                       = "{=BattleRegen_DebugHint}Whether to print debug outputs to log (in C:/ProgramData/Mount and Blade II Bannerlord/logs folder). Default is false.";

            //UseSliderForRegenModelName      = "{=BattleRegen_UseSliderForRegenModelName}Use Slider for Regen Model",
            //UseSliderForRegenModelHint      = "{=BattleRegen_UseSliderForRegenModelHint}Should the mod use a slider for regen instead of dropdown options.",
            //SliderRegenModelName            = "{=BattleRegen_SliderRegenModelName}Regeneration Model",
            //SliderRegenModelHint            = "{=BattleRegen_SliderRegenModelHint}Determines the model used for regenerating health. 'Linear' is 1, 'Quadratic' is 2, and 'EVE Online' is 3. See Nexus Mods page (https://www.nexusmods.com/mountandblade2bannerlord/mods/1432) for more details. Default is Linear.";

        private static readonly TextObject ModNameTextObject = new TextObject(ModNameText);
    }
}
