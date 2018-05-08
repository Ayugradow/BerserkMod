using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InControl;

namespace BerserkMod
{
    internal class Toggle : PlayerActionSet
    {
        public Toggle()
        {
            berserkL = base.CreatePlayerAction("BerserkL");
            berserkR = base.CreatePlayerAction("BerserkR");
            berserkKb = CreatePlayerAction("BerserkKB");
        }

        public PlayerAction berserkL;
        public PlayerAction berserkR;
        public PlayerAction berserkKb;
    }
}
