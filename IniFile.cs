

using Rage;
using System.Text.RegularExpressions;

namespace HeliView
{
    internal static class Settings
    {
        internal static bool EnableOverlay = true;
        internal static bool WarpPlayerInHeli = true;
        internal static string HeliType = "cop";
        internal static string onPedArrestBehavior = "1";

        internal static string path = "Plugins/LSPDFR/HeliView.ini";
        internal static InitializationFile ini = new InitializationFile(path);

        internal static void LoadSettings()
        {
            Game.LogTrivial("[HeliView]: Loading config file:");

            ini.Create();
            EnableOverlay = ini.ReadBoolean("Settings", "EnableOverlay", true);
            Game.LogTrivial($"Enable Overlay: {(EnableOverlay ? "Yes" : "No")}");
            WarpPlayerInHeli = ini.ReadBoolean("Settings", "WarpPlayerInHeli", true);
            Game.LogTrivial($"Warp player in Heli: {(WarpPlayerInHeli ? "Yes" : "No")}");
            HeliType = ini.ReadString("Settings", "HeliType", "cop");
            Game.LogTrivial($"Heli type: {HeliType}");
            onPedArrestBehavior = ini.ReadString("Settings", "onPedArrestBehavior", "1");
            if (Regex.IsMatch(onPedArrestBehavior, @"^0|1|2\+(0|1)$"))
            {
                Game.LogTrivial($"On ped arrest behavior: {(onPedArrestBehavior == "0" ? "Do nothing" : (onPedArrestBehavior == "1" ? "Stop pursuit camera" : (onPedArrestBehavior == "2+0" ? "Switch remaining suspect or Do nothing" : "Switch remaining suspect or Stop pursuit camera")))}");
            }
            else
            {
                onPedArrestBehavior = "1";
                Game.LogTrivial($"Invalid setting for onPedArrestBehavior: '{onPedArrestBehavior}'. Using default value '1' (Stop pursuit camera).");
            }
        }

        internal static void SaveSetting(string setting, bool value)
        {
            ini.Create();
            ini.Write("Features", setting, value);
        }
    }
}
