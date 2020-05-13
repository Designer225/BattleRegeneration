using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MBOptionScreen.Attributes;
using MBOptionScreen.Attributes.v2;
using MBOptionScreen.Data;
using MBOptionScreen.Settings;

namespace BattleRegen
{
    public class BattleRegenSettings : AttributeSettings<BattleRegenSettings>
    {
        public override string Id { get; set; } = "D225.BattleRegen";

        public override string ModName => "Battle Regeneration";

        public override string ModuleFolderName => "D225.BattleRegen";

        [SettingPropertyFloatingInteger("Regen Amount (Percents) Per Second", 0f, 25f, HintText = "What percent of total health to be regenerated every second. (Regen is applied continuously with this mod.)",
            Order = 0, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public float RegenAmount { get; set; } = 1f;

        [SettingPropertyFloatingInteger("Medicine Boost (Percents)", 0f, 200f, HintText = "Regen is increased by this percentage for every 50 points in a human agent's medicine skill. Riders affect regen for their mounts at standard efficiency. Bonuses stack additively.",
            Order = 1, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public float MedicineBoost { get; set; } = 50f;

        [SettingPropertyFloatingInteger("Medicine Boost (Percents) For Commanders", 0f, 200f, HintText = "Regen is increased by this percentage for every 50 points in a commander's medicine skills. Bonuses stack additively.",
            Order = 2, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public float CommanderMedicineBoost { get; set; } = 25f;

        [SettingPropertyFloatingInteger("XP Gain Per Full Health", 0f, 10f, HintText = "How much XP is gained when an agent or its rider (if applicable) heals enough to refill its health bar from 0 max health. XP gain is continuous.",
            Order = 3, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public float XpGain { get; set; } = 1f;

        [SettingPropertyFloatingInteger("XP Gain Per Full Health For Commanders", 0f, 10f, HintText = "How much XP is gained when a commander an agent enough to refill its health bar from 0 to max health. XP gain is continuous.",
            Order = 4, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public float CommanderXpGain { get; set; } = 0.1f;

        [SettingPropertyDropdown("Regeneration Model", HintText = "Determines the model used for regenerating health. 'Static' uses a linear regen rate. 'Quadratic' uses a higher regen rate the lower an agent's health is, with a max regen rate of 2x base regen. 'EVE Online' mimics passive shield regen of the eponymous game, seeing maximum regen rate at 25% health with a max regen rate of 2.5x base regen.",
            Order = 5, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public DefaultDropdown<string> RegenModel { get; set; } = new DefaultDropdown<string>(new string[]
        {
            BattleRegenModel.Linear,
            BattleRegenModel.Quadratic,
            BattleRegenModel.EveOnline
        }, 0);

        [SettingPropertyBool("Apply To Player", HintText = "Whether the player should receive passive regen.",
            Order = 6, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToPlayer { get; set; } = true;

        [SettingPropertyBool("Apply To Companions", HintText = "Whether the player's companions should receive passive regen.",
            Order = 7, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToCompanions { get; set; } = true;

        [SettingPropertyBool("Apply To Allied Heroes", HintText = "Whether the player's allied heroes should receive passive regen.",
            Order = 8, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToAlliedHeroes { get; set; } = true;

        [SettingPropertyBool("Apply To Party Troops", HintText = "Whether the player's troops should receive passive regen.",
            Order = 9, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToPartyTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Allied Troops", HintText = "Whether the player's allied troops should receive passive regen.",
            Order = 10, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToAlliedTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Enemy Heroes", HintText = "Whether the player's enemy heroes should receive passive regen.",
            Order = 11, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToEnemyHeroes { get; set; } = true;

        [SettingPropertyBool("Apply To Enemy Troops", HintText = "Whether the player's enemy troops should receive passive regen.",
            Order = 12, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToEnemyTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Mounts", HintText = "Whether mounts should receive passive regen. Mounts regenerate regardless of their rider's allegiance.",
            Order = 13, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToMount { get; set; } = true;

        [SettingPropertyBool("Debug Mode", HintText = "Whether to print debug outputs to log (in C:/ProgramData/Mount and Blade II Bannerlord/logs folder).",
            Order = 14, RequireRestart = false)]
        //[SettingPropertyGroup("Regeneration Settings")]
        public bool Debug { get; set; } = false;
    }
}
