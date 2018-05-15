using InControl;

namespace BerserkMod
{
    internal class Toggle : PlayerActionSet
    {
        public Toggle()
        {
            berserkL = CreatePlayerAction("BerserkL");
            berserkR = CreatePlayerAction("BerserkR");
            berserkKb = CreatePlayerAction("BerserkKB");
        }

        public PlayerAction berserkL;
        public PlayerAction berserkR;
        public PlayerAction berserkKb;
    }
}
