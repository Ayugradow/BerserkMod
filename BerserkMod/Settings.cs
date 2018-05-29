using System;
using Modding;

namespace BerserkMod
{
    [Serializable]
    public class Settings : IModSettings
    {
        public int LocalKey
        {
            get => GetInt(null, "Key");
            set => SetInt(value, "Key");
        }

        public int LocalButtons
        {
            get => GetInt(null, "Button");
            set => SetInt(value, "Button");
        }
    }
}
