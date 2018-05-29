using InControl;

namespace BerserkMod
{
    internal class Toggle : PlayerActionSet
    {
        public Toggle()
        {
            berserkButton = CreatePlayerAction("BerserkButton");
            berserkKb = CreatePlayerAction("BerserkKB");
        }

        public PlayerAction berserkButton;
        public PlayerAction berserkKb;
    }
}
