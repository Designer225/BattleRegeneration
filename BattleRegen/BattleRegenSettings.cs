using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Data;
using MCM.Abstractions.Settings.Base.Global;

namespace BattleRegen
{
    public partial class BattleRegenSettings : AttributeGlobalSettings<BattleRegenSettings>
    {
        public override string Id => "D225.BattleRegen";

        public override string DisplayName => ModNameTextObject.ToString();

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

        [SettingPropertyFloatingInteger(XpGainName, 0f, 10f, HintText = XpGainHint, Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float XpGain { get; set; } = 1f;

        [SettingPropertyFloatingInteger(CommanderXpGainName, 0f, 10f, HintText = CommanderXpGainHint, Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderXpGain { get; set; } = 0.1f;
        #endregion

        #region Regeneration Settings
        [SettingPropertyDropdown(RegenModelDropdownName, HintText = RegenModelDropdownHint, Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName, GroupOrder = 1)]
        public DefaultDropdown<Formula> RegenModelDropdown { get; set; } = Formula.GetFormulas();

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

        //#region Emergency Settings -- removed as MCM dropdown seems stable now
        //[SettingPropertyBool(UseSliderForRegenModelName, HintText = UseSliderForRegenModelHint, Order = 4, RequireRestart = false)]
        //[SettingPropertyGroup(UseSliderForRegenModelName, GroupOrder = 2, IsMainToggle = true)]
        //public bool UseSliderForRegenModel { get; set; } = false;

        //[SettingPropertyInteger(SliderRegenModelName, (int)BattleRegenModel.Linear, (int)BattleRegenModel.EveOnline, HintText = SliderRegenModelHint, Order = 5, RequireRestart = false)]
        //[SettingPropertyGroup(UseSliderForRegenModelName)]
        //public int SliderRegenModel { get; set; } = (int)BattleRegenModel.Linear;
        //#endregion

        public Formula RegenModel
        {
            get => RegenModelDropdown.SelectedValue;
            //{
            //    if (UseSliderForRegenModel) return (BattleRegenModel)SliderRegenModel;
            //    else return RegenModelDropdown.SelectedValue;
            //}
        }

    }
}
