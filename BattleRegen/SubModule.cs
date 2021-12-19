using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace BattleRegen
{
    class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            new Harmony("d225.battleregen").PatchAll();

            // load config first
            try
            {
                _ = BattleRegenSettingsUtil.Instance;
            }
            catch (Exception e)
            {
                Debug.Print(e.ToString());
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(new BattleRegeneration());
        }
    }

    [HarmonyPatch(typeof(Agent), nameof(Agent.Health), MethodType.Setter)]
    static class Agent_SetHealth_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();

            int deleteEndIndex = -1;
            for (int i = 0; i < code.Count; i++)
            {
                // What's the point of using a ceiling value, TaleWorlds?
                if (code[i].opcode == OpCodes.Stloc_0)
                {
                    deleteEndIndex = i;
                    break;
                }
            }

            if (deleteEndIndex != -1)
            {
                code.RemoveRange(1, deleteEndIndex - 1);
                code.Insert(1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Agent_SetHealth_Patch), nameof(Agent_SetHealth_Patch.RedoneCompareValue))));
                Debug.Print("[BattleRegeneration] arbitrary float ceilinglator in Agent.Health settler smoothened");
            }

            return code.AsEnumerable();
        }

        private static float RedoneCompareValue(this float value)
        {
            return value.ApproximatelyEqualsTo(0.0f, 1E-05f) ? 0.0f : value;
        }
    }
}
