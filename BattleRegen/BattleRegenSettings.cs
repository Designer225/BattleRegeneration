using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace BattleRegen
{
    static class BattleRegenSettingsUtil
    {
        private static IBattleRegenSettings instance;

        public static int ExceptionCount { get; internal set; }

        private static FileInfo ConfigFile { get; } = new FileInfo(Path.Combine(BasePath.Name, "Modules", "BattleRegeneration.config.xml"));

        public static IBattleRegenSettings Instance
        {
            get
            {
                // attempt to load MCM config
                try
                {
                    instance = BattleRegenSettings.Instance ?? instance;
                }
                catch (Exception e)
                {
                    if (ExceptionCount < 100) // don't need this populating the log file
                    {
                        ExceptionCount++;
                        Debug.Print(string.Format("[BattleRegeneration] Failed to obtain MCM config, defaulting to config file.\n\nError: {1}\n\n{2}",
                            ConfigFile.FullName, e.Message, e.StackTrace));
                    }
                }

                // load config file if MCM config load fails
                if (instance == default)
                {
                    var serializer = new XmlSerializer(typeof(BattleRegenDefaultSettings));
                    if (ConfigFile.Exists)
                    {
                        try
                        {
                            using (var stream = ConfigFile.OpenText())
                                instance = serializer.Deserialize(stream) as BattleRegenDefaultSettings;
                        }
                        catch (Exception e)
                        {
                            Debug.Print(string.Format("[BattleRegeneration] Failed to load file {0}\n\nError: {1}\n\n{2}",
                                ConfigFile.FullName, e.Message, e.StackTrace));
                        }
                    }

                    if (instance == default) instance = new BattleRegenDefaultSettings();
                    using (var stream = ConfigFile.Open(FileMode.Create))
                    {
                        var xmlWritter = new XmlTextWriter(stream, Encoding.UTF8)
                        {
                            Formatting = Formatting.Indented,
                            Indentation = 4
                        };
                        serializer.Serialize(xmlWritter, instance);
                    }
                }
                return instance;
            }
        }
    }

    interface IBattleRegenSettings
    {
        float RegenAmount { get; set; }

        float RegenAmountCompanions { get; set; }

        float RegenAmountSubordinates { get; set; }

        float RegenAmountAllies { get; set; }

        float RegenAmountPartyTroops { get; set; }

        float RegenAmountAlliedTroops { get; set; }

        float RegenAmountEnemies { get; set; }

        float RegenAmountEnemyTroops { get; set; }

        float RegenAmountAnimals { get; set; }

        float MedicineBoost { get; set; }

        float CommanderMedicineBoost { get; set; }

        float XpGain { get; set; }

        float CommanderXpGain { get; set; }

        Dropdown<Formula> RegenModelDropdown { get; set; }

        bool HealToFull { get; set; }

        float DelayedRegenTime { get; set; }

        bool Debug { get; set; }

        bool VerboseDebug { get; set; }

        Formula RegenModel { get; }
    }

    [XmlRoot("BattleRegeneration", IsNullable = false)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member - suppressed because XmlSerializer complains about non-public classes
    public class BattleRegenDefaultSettings : IBattleRegenSettings
    {
        [XmlElement(DataType = "float")]
        public float RegenAmount { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountCompanions { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountSubordinates { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountAllies { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountPartyTroops { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountAlliedTroops { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountEnemies { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountEnemyTroops { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float RegenAmountAnimals { get; set; } = 1f;

        [XmlElement(DataType = "float")]
        public float MedicineBoost { get; set; } = 50f;

        [XmlElement(DataType = "float")]
        public float CommanderMedicineBoost { get; set; } = 25f;

        [XmlElement(DataType = "float")]
        public float XpGain { get; set; } = 5f;

        [XmlElement(DataType = "float")]
        public float CommanderXpGain { get; set; } = 0.5f;

        [XmlIgnore]
        public Dropdown<Formula> RegenModelDropdown { get; set; } = Formula.GetFormulas(); // not serialized as the list should be built only once

        [XmlElement]
        public string RegenModelString { get; set; } = "00_Linear"; // had to hard code this because of necessity

        [XmlElement(DataType = "boolean")]
        public bool HealToFull { get; set; } = false;

        [XmlElement(DataType = "float")]
        public float DelayedRegenTime { get; set; } = 0f;

        [XmlElement(DataType = "boolean")]
        public bool Debug { get; set; } = false;

        [XmlElement(DataType = "boolean")]
        public bool VerboseDebug { get; set; } = false;

        [XmlIgnore]
        public Formula RegenModel => RegenModelDropdown.Find(x => x.Id == RegenModelString); // also not serialized as this is supposed to return a value at runtime
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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

        [SettingPropertyFloatingInteger(RegenAmountCompanionsName, 0f, 25f, HintText = RegenAmountHint, Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountCompanions { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountSubordinatesName, 0f, 25f, HintText = RegenAmountHint, Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountSubordinates { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountAlliesName, 0f, 25f, HintText = RegenAmountHint, Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountAllies { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountPartyTroopsName, 0f, 25f, HintText = RegenAmountHint, Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountPartyTroops { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountAlliedTroopsName, 0f, 25f, HintText = RegenAmountHint, Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountAlliedTroops { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountEnemiesName, 0f, 25f, HintText = RegenAmountHint, Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountEnemies { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountEnemyTroopsName, 0f, 25f, HintText = RegenAmountHint, Order = 6, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountEnemyTroops { get; set; } = 1f;

        [SettingPropertyFloatingInteger(RegenAmountAnimalsName, 0f, 25f, HintText = RegenAmountHint, Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float RegenAmountAnimals { get; set; } = 1f;

        [SettingPropertyFloatingInteger(MedicineBoostName, 0f, 200f, HintText = MedicineBoostHint, Order = 8, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float MedicineBoost { get; set; } = 50f;

        [SettingPropertyFloatingInteger(CommanderMedicineBoostName, 0f, 200f, HintText = CommanderMedicineBoostHint, Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderMedicineBoost { get; set; } = 25f;

        [SettingPropertyFloatingInteger(XpGainName, 0f, 100f, HintText = XpGainHint, Order = 10, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float XpGain { get; set; } = 5f;

        [SettingPropertyFloatingInteger(CommanderXpGainName, 0f, 100f, HintText = CommanderXpGainHint, Order = 11, RequireRestart = false)]
        [SettingPropertyGroup(RegenValuesName)]
        public float CommanderXpGain { get; set; } = 0.5f;
        #endregion

        #region Regeneration Settings
        [SettingPropertyDropdown(RegenModelDropdownName, HintText = RegenModelDropdownHint, Order = 12, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName, GroupOrder = 1)]
        public Dropdown<Formula> RegenModelDropdown { get; set; } = Formula.GetFormulas();

        [SettingPropertyBool(HealToFullName, HintText = HealToFullHint, Order = 13, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool HealToFull { get; set; } = false;

        [SettingPropertyFloatingInteger(DelayedRegenTimeName, 0f, 100f, HintText = DelayedRegenTimeHint, Order = 14, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public float DelayedRegenTime { get; set; } = 0f;

        [SettingPropertyBool(DebugName, HintText = DebugHint, Order = 15, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool Debug { get; set; } = false;

        [SettingPropertyBool(VerboseDebugName, HintText = VerboseDebugHint, Order = 16, RequireRestart = false)]
        [SettingPropertyGroup(RegenSettingsName)]
        public bool VerboseDebug { get; set; } = false;
        #endregion

        public Formula RegenModel => RegenModelDropdown.SelectedValue;
    }
}
