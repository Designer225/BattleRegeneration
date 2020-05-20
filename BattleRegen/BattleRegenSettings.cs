using MBOptionScreen.Attributes;
using MBOptionScreen.Attributes.v2;
using MBOptionScreen.Data;
using MBOptionScreen.Settings;

namespace BattleRegen
{
    public partial class BattleRegenSettings : AttributeSettings<BattleRegenSettings>
    {
        public override string Id { get; set; } = "D225.BattleRegen";

        public override string ModName => ModNameTextObject.ToString();

        public override string ModuleFolderName => "D225.BattleRegen";

        #region Regeneration Values
        [SettingPropertyFloatingInteger(RegenAmountName, 0f, 25f, HintText = RegenAmountHint, Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName, 0)]
        public float RegenAmount { get; set; } = 1f;

        [SettingPropertyFloatingInteger(MedicineBoostName, 0f, 200f, HintText = MedicineBoostHint, Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float MedicineBoost { get; set; } = 50f;

        [SettingPropertyFloatingInteger(CommanderMedicineBoostName, 0f, 200f, HintText = CommanderMedicineBoostHint, Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderMedicineBoost { get; set; } = 25f;

        [SettingPropertyFloatingInteger(XpGainName, 0f, 10f, HintText = XpGainHint, Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float XpGain { get; set; } = 1f;

        [SettingPropertyFloatingInteger(CommanderXpGainName, 0f, 10f, HintText = CommanderXpGainHint, Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderXpGain { get; set; } = 0.1f;
        #endregion

        #region Regeneration Settings
        [SettingPropertyDropdown(RegenModelDropdownName, HintText = RegenModelDropdownHint, Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName, 1)]
        public DefaultDropdown<BattleRegenModel> RegenModelDropdown { get; set; } = new DefaultDropdown<BattleRegenModel>(new BattleRegenModel[]
        {
            BattleRegenModel.Linear,
            BattleRegenModel.Quadratic,
            BattleRegenModel.EveOnline
        }, 0);

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

        [SettingPropertyBool(ApplyToMountName, HintText = ApplyToMountHint, Order = 13, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool ApplyToMount { get; set; } = true;

        [SettingPropertyBool(DebugName, HintText = DebugHint, Order = 14, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool Debug { get; set; } = false;
        #endregion

        #region Emergency Settings
        [SettingPropertyBool(UseSliderForRegenModelName, HintText = UseSliderForRegenModelHint, Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(UseSliderForRegenModelName, 2, true)]
        public bool UseSliderForRegenModel { get; set; } = false;

        [SettingPropertyInteger(SliderRegenModelName, (int)BattleRegenModel.Linear, (int)BattleRegenModel.EveOnline, HintText = SliderRegenModelHint, Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(UseSliderForRegenModelName)]
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
