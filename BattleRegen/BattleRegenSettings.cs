using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Dropdown;
using MCM.Abstractions.Settings.Base.Global;

namespace BattleRegen
{
    static class BattleRegenSettingsUtil
    {
        private static IBattleRegenSettings instance;

        public static IBattleRegenSettings Instance
        {
            get
            {
                instance = BattleRegenSettings.Instance ?? instance ?? new BattleRegenDefaultSettings();
                return instance;
            }
        }
    }

    interface IBattleRegenSettings
    {
        float RegenAmount { get; set; }

        float MedicineBoost { get; set; }

        float CommanderMedicineBoost { get; set; }

        float XpGain { get; set; }

        float CommanderXpGain { get; set; }

        DropdownDefault<Formula> RegenModelDropdown { get; set; }

        bool ApplyToPlayer { get; set; }

        bool ApplyToCompanions { get; set; }

        bool ApplyToAlliedHeroes { get; set; }

        bool ApplyToPartyTroops { get; set; }

        bool ApplyToAlliedTroops { get; set; }

        bool ApplyToEnemyHeroes { get; set; }

        bool ApplyToEnemyTroops { get; set; }

        bool ApplyToAnimal { get; set; }

        bool Debug { get; set; }

        Formula RegenModel { get; }
    }

    class BattleRegenDefaultSettings : IBattleRegenSettings
    {
        public float RegenAmount { get; set; } = 1f;

        public float MedicineBoost { get; set; } = 50f;

        public float CommanderMedicineBoost { get; set; } = 25f;

        public float XpGain { get; set; } = 5f;

        public float CommanderXpGain { get; set; } = 0.5f;

        public DropdownDefault<Formula> RegenModelDropdown { get; set; } = Formula.GetFormulas();

        public bool ApplyToPlayer { get; set; } = true;

        public bool ApplyToCompanions { get; set; } = true;

        public bool ApplyToAlliedHeroes { get; set; } = true;

        public bool ApplyToPartyTroops { get; set; } = true;

        public bool ApplyToAlliedTroops { get; set; } = true;

        public bool ApplyToEnemyHeroes { get; set; } = true;

        public bool ApplyToEnemyTroops { get; set; } = true;

        public bool ApplyToAnimal { get; set; } = true;

        public bool Debug { get; set; } = true;

        public Formula RegenModel => RegenModelDropdown.SelectedValue;
    }

    partial class BattleRegenSettings : AttributeGlobalSettings<BattleRegenSettings>, IBattleRegenSettings
    {
        public override string Id => "D225.BattleRegen";

        public override string DisplayName => ModNameTextObject.ToString();

        public override string FormatType => "json2";

        public override string FolderName => "D225.BattleRegen";

        #region Regeneration Values
        [SettingPropertyFloatingInteger(RegenAmountName, 0f, 25f, HintText = RegenAmountHint, Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName, GroupOrder = 0)]
        public float RegenAmount { get; set; } = 1f;

        [SettingPropertyFloatingInteger(MedicineBoostName, 0f, 200f, HintText = MedicineBoostHint, Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float MedicineBoost { get; set; } = 50f;

        [SettingPropertyFloatingInteger(CommanderMedicineBoostName, 0f, 200f, HintText = CommanderMedicineBoostHint, Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderMedicineBoost { get; set; } = 25f;

        [SettingPropertyFloatingInteger(XpGainName, 0f, 100f, HintText = XpGainHint, Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float XpGain { get; set; } = 5f;

        [SettingPropertyFloatingInteger(CommanderXpGainName, 0f, 100f, HintText = CommanderXpGainHint, Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderXpGain { get; set; } = 0.5f;
        #endregion

        #region Regeneration Settings
        [SettingPropertyDropdown(RegenModelDropdownName, HintText = RegenModelDropdownHint, Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName, GroupOrder = 1)]
        public DropdownDefault<Formula> RegenModelDropdown { get; set; } = Formula.GetFormulas();

        [SettingPropertyBool(ApplyToPlayerName, HintText = ApplyToPlayerHint, Order = 6, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToPlayer { get; set; } = true;

        [SettingPropertyBool(ApplyToCompanionsName, HintText = ApplyToCompanionsHint, Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToCompanions { get; set; } = true;

        [SettingPropertyBool(ApplyToAlliedHeroesName, HintText = ApplyToAlliedHeroesHint, Order = 8, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToAlliedHeroes { get; set; } = true;

        [SettingPropertyBool(ApplyToPartyTroopsName, HintText = ApplyToPartyTroopsHint, Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToPartyTroops { get; set; } = true;

        [SettingPropertyBool(ApplyToAlliedTroopsName, HintText = ApplyToAlliedTroopsHint, Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToAlliedTroops { get; set; } = true;

        [SettingPropertyBool(ApplyToEnemyHeroesName, HintText = ApplyToEnemyHeroesHint, Order = 11, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToEnemyHeroes { get; set; } = true;

        [SettingPropertyBool(ApplyToEnemyTroopsName, HintText = ApplyToEnemyTroopsHint, Order = 12, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToEnemyTroops { get; set; } = true;

        [SettingPropertyBool(ApplyToAnimalName, HintText = ApplyToAnimalHint, Order = 13, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToAnimal { get; set; } = true;

        [SettingPropertyBool(DebugName, HintText = DebugHint, Order = 14, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool Debug { get; set; } = false;
        #endregion

        public Formula RegenModel => RegenModelDropdown.SelectedValue;
    }
}
