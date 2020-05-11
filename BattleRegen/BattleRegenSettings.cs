using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MBOptionScreen.Attributes;
using MBOptionScreen.Attributes.v2;
using MBOptionScreen.Settings;

namespace BattleRegen
{
    public class BattleRegenSettings : AttributeSettings<BattleRegenSettings>
    {
        public override string Id { get; set; } = "D225.BattleRegen";

        public override string ModName => "Battle Regeneration";

        public override string ModuleFolderName => "D225.BattleRegen";

        [SettingPropertyFloatingInteger("Regen Amount", 0f, 0.5f, HintText = "What fraction of total health to be regenerated every second. (Regen is applied continuously with this mod.)", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public float RegenAmount { get; set; } = 0.02f;

        [SettingPropertyBool("Apply To Player", HintText = "Whether the player should receive passive regen.", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToPlayer { get; set; } = true;

        [SettingPropertyBool("Apply To Companions", HintText = "Whether the player's companions should receive passive regen.", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToCompanions { get; set; } = true;

        [SettingPropertyBool("Apply To Allied Heroes", HintText = "Whether the player's allied heroes should receive passive regen.", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToAlliedHeroes { get; set; } = true;

        [SettingPropertyBool("Apply To Party Troops", HintText = "Whether the player's troops should receive passive regen.", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToPartyTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Allied Troops", HintText = "Whether the player's allied troops should receive passive regen.", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToAlliedTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Enemy Heroes", HintText = "Whether the player's enemy heroes should receive passive regen.", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToEnemyHeroes { get; set; } = true;

        [SettingPropertyBool("Apply To Enemy Troops", HintText = "Whether the player's enemy troops should receive passive regen.", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToEnemyTroops { get; set; } = true;

        [SettingPropertyBool("Apply To Mounts", HintText = "Whether mounts should receive passive regen. Mounts regenerate regardless of their rider's allegiance.", Order = 8, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool ApplyToMount { get; set; } = true;

        [SettingPropertyBool("Debug Mode", HintText = "Whether to print debug outputs to log (in C:/ProgramData/Mount and Blade II Bannerlord/logs folder).", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup("Regeneration Settings")]
        public bool Debug { get; set; } = false;
    }
}
