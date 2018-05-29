using Modding;
using System;

namespace BerserkMod
{
    [Serializable]
    public class GlobalSettings : IModSettings
    {
        public int Keys
        {
            get => GetInt(65, "ToggleKeys");
            set => SetInt(value, "ToggleKeys");
        }

        public int Button
        {
            get => GetInt(10, "ToggleButton");
            set => SetInt(value, "ToggleButton");
        }
    }
}
