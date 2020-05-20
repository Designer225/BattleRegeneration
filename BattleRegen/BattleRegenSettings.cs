using MBOptionScreen.Attributes;
using MBOptionScreen.Attributes.v2;
using MBOptionScreen.Data;
using MBOptionScreen.Settings;
using TaleWorlds.Localization;

namespace BattleRegen
{
    public class BattleRegenSettings : AttributeSettings<BattleRegenSettings>
    {
        public override string Id { get; set; } = "D225.BattleRegen";

        public override string ModName => "Battle Regeneration";

        public override string ModuleFolderName => "D225.BattleRegen";

        #region Regeneration Values
        [SettingPropertyFloatingInteger("Regen Amount (Percents) Per Second", 0f, 25f,
            HintText = "What percent of total health to be regenerated every second. (Regen is applied continuously with this mod.) Default is 1%.",
            Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Values")]
        public float RegenAmount { get; set; } = 1f;

        [SettingPropertyFloatingInteger("Medicine Boost (Percents)", 0f, 200f,
            HintText = "Regen is increased by this percentage for every 50 points in a human agent's medicine skill. Riders affect regen for their mounts at standard efficiency. Bonuses stack additively. Default is 50%.",
            Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Values", 0)]
        public float MedicineBoost { get; set; } = 50f;

        [SettingPropertyFloatingInteger("Medicine Boost (Percents) For Commanders", 0f, 200f,
            HintText = "Regen is increased by this percentage for every 50 points in a commander's medicine skills. Bonuses stack additively. Default is 25%.",
            Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Values")]
        public float CommanderMedicineBoost { get; set; } = 25f;

        [SettingPropertyFloatingInteger("XP Gain Per Full Health", 0f, 10f,
            HintText = "How much XP is gained when an agent or its rider (if applicable) heals enough to refill its health bar from 0 to max health. XP gain is continuous. Default is 1.",
            Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Values")]
        public float XpGain { get; set; } = 1f;

        [SettingPropertyFloatingInteger("XP Gain Per Full Health For Commanders", 0f, 10f,
            HintText = "How much XP is gained when a commander heals an agent enough to refill its health bar from 0 to max health. XP gain is continuous. Default is 0.1.",
            Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Values")]
        public float CommanderXpGain { get; set; } = 0.1f;
        #endregion

        #region Regeneration Settings
        [SettingPropertyDropdown("Regeneration Model",
            HintText = "Determines the model used for regenerating health. See Nexus Mods page (https://www.nexusmods.com/mountandblade2bannerlord/mods/1432) for more details. Default is Linear.",
            Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings", 1)]
        public DefaultDropdown<BattleRegenModel> RegenModelDropdown { get; set; } = new DefaultDropdown<BattleRegenModel>(new BattleRegenModel[]
        {
            BattleRegenModel.Linear,
            BattleRegenModel.Quadratic,
            BattleRegenModel.EveOnline
        }, 0);

        [SettingPropertyBool("Apply To Player",
            HintText = "Whether the player should receive passive regen. Default is true.",
            Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToPlayer { get; set; } = true;

        [SettingPropertyBool("Apply To Companions",
            HintText = "Whether the player's companions should receive passive regen. Default is true.",
            Order = 7, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToCompanions { get; set; } = true;

        [SettingPropertyBool("Apply To Allied Heroes",
            HintText = "Whether the player's allied heroes should receive passive regen. Default is true.",
            Order = 8, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToAlliedHeroes { get; set; } = true;

        [SettingPropertyBool("Apply To Party Troops",
            HintText = "Whether the player's troops should receive passive regen. Default is true.",
            Order = 9, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToPartyTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Allied Troops",
            HintText = "Whether the player's allied troops should receive passive regen. Default is true.",
            Order = 10, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToAlliedTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Enemy Heroes",
            HintText = "Whether the player's enemy heroes should receive passive regen. Default is true.",
            Order = 11, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToEnemyHeroes { get; set; } = true;

        [SettingPropertyBool("Apply To Enemy Troops",
            HintText = "Whether the player's enemy troops should receive passive regen. Default is true.",
            Order = 12, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToEnemyTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Mounts",
            HintText = "Whether mounts should receive passive regen. Mounts regenerate regardless of their rider's allegiance. Default is true.",
            Order = 13, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToMount { get; set; } = true;

        [SettingPropertyBool("Debug Mode",
            HintText = "Whether to print debug outputs to log (in C:/ProgramData/Mount and Blade II Bannerlord/logs folder). Default is false.",
            Order = 14, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool Debug { get; set; } = false;
        #endregion

        #region Emergency Settings
        [SettingPropertyBool("Use Slider for Regen Model",
            HintText = "Should the mod use a slider for regen instead of dropdown options.",
            Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Use Slider for Regen Model", 2, true)]
        public bool UseSliderForRegenModel { get; set; } = false;

        [SettingPropertyInteger("Regeneration Model", (int)BattleRegenModel.Linear, (int)BattleRegenModel.EveOnline,
            HintText = "Determines the model used for regenerating health. 'Linear' is 1, 'Quadratic' is 2, and 'EVE Online' is 3. See Nexus Mods page (https://www.nexusmods.com/mountandblade2bannerlord/mods/1432) for more details. Default is Linear.",
            Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Use Slider for Regen Model")]
        public int SliderRegenModel { get; set; } = (int)BattleRegenModel.Linear;
        #endregion

        public BattleRegenModel RegenModel
        {
            get
            {
                if (UseSliderForRegenModel) return (BattleRegenModel)SliderRegenModel;
                else return RegenModelDropdown.SelectedValue;
            }
        }
    }
}
