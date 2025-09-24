

using Rage;

namespace HeliView
{
    internal static class Settings
    {
        internal static bool EnableOverlay = true;
        internal static bool WarpPlayerInHeli = true;
        internal static string HeliType = "cop";

        internal static string path = "Plugins/LSPDFR/HeliView.ini";
        internal static InitializationFile ini = new InitializationFile(path);

        internal static void LoadSettings()
        {
            Game.LogTrivial("[LOG]: Loading config file for SSStuart Tools.");

            ini.Create();
            EnableOverlay = ini.ReadBoolean("Settings", "EnableOverlay", true);
            WarpPlayerInHeli = ini.ReadBoolean("Settings", "WarpPlayerInHeli", true);
            HeliType = ini.ReadString("Settings", "HeliType", "cop");
        }

        internal static void SaveSetting(string setting, bool value)
        {
            ini.Create();
            ini.Write("Features", setting, value);
        }
    }
}
