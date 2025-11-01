

using Rage;
using System.Text.RegularExpressions;

namespace HeliView
{
    internal static class Settings
    {
        internal static bool EnableOverlay = true;
        internal static bool WarpPlayerInHeli = true;
        internal static string PlayerBehaviour = "0";
        internal static string HeliType = "cop";
        internal static string OnPedArrestBehavior = "1";

        internal static string path = "Plugins/LSPDFR/HeliView.ini";
        internal static InitializationFile ini = new InitializationFile(path);

        internal static void LoadSettings()
        {
            Game.LogTrivial($"[{Main.pluginName}]: Loading config file:");

            ini.Create();
            EnableOverlay = ini.ReadBoolean("Settings", "EnableOverlay", true);
            Game.LogTrivial($"- Enable Overlay: {(EnableOverlay ? "Yes" : "No")}");
            WarpPlayerInHeli = ini.ReadBoolean("Settings", "WarpPlayerInHeli", true);
            Game.LogTrivial($"- Warp player in Heli: {(WarpPlayerInHeli ? "Yes" : "No")}");
            PlayerBehaviour = ini.ReadString("Settings", "PlayerBehaviour", "0");
            if (Regex.IsMatch(PlayerBehaviour, @"^0|1|2$") && !WarpPlayerInHeli)
            {
                Game.LogTrivial($"- Player behaviour while camera active: {(PlayerBehaviour == "0" ? "Do nothing" : (PlayerBehaviour == "1" ? "Teleport closer on end" : "Make the player chase the suspect" ))}");
            }
            else if (WarpPlayerInHeli)
            {
                PlayerBehaviour = "0";
                Game.LogTrivial($"- Player behaviour while camera active: Setting ignored, WarpPlayerInHeli is true.");
            }
            else
            {
                PlayerBehaviour = "0";
                Game.LogTrivial($"! Invalid setting for PlayerBehaviour: '{PlayerBehaviour}'. Using default value '0' (Do nothing).");
            }
            HeliType = ini.ReadString("Settings", "HeliType", "cop");
            Game.LogTrivial($"- Heli type: {HeliType}");
            OnPedArrestBehavior = ini.ReadString("Settings", "onPedArrestBehavior", "1");
            if (Regex.IsMatch(OnPedArrestBehavior, @"^0|1|2\+(0|1)$"))
            {
                Game.LogTrivial($"- On ped arrest behavior: {(OnPedArrestBehavior == "0" ? "Do nothing" : (OnPedArrestBehavior == "1" ? "Stop pursuit camera" : (OnPedArrestBehavior == "2+0" ? "Switch remaining suspect or Do nothing" : "Switch remaining suspect or Stop pursuit camera")))}");
            }
            else
            {
                OnPedArrestBehavior = "1";
                Game.LogTrivial($"! Invalid setting for OnPedArrestBehavior: '{OnPedArrestBehavior}'. Using default value '1' (Stop pursuit camera).");
            }

            Game.LogTrivial($"[{Main.pluginName}] Plugin settings loaded.");
        }

        internal static void SaveSetting(string setting, bool value)
        {
            ini.Create();
            ini.Write("Features", setting, value);
        }
    }
}
