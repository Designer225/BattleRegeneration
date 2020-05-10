using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace BattleRegen
{
    public class SubModule : MBSubModuleBase
    {
        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            base.OnMissionBehaviourInitialize(mission);
            mission.AddMissionBehaviour(new BattleRegeneration(mission));
        }
    }
}
