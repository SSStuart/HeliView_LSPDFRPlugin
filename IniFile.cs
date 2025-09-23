

using Rage;

namespace HeliView
{
    internal static class Settings
    {
        internal static bool WarpPlayerInHeli= true;

        internal static string path = "Plugins/LSPDFR/HeliView.ini";
        internal static InitializationFile ini = new InitializationFile(path);

        internal static void LoadSettings()
        {
            Game.LogTrivial("[LOG]: Loading config file for SSStuart Tools.");

            ini.Create();
            WarpPlayerInHeli = ini.ReadBoolean("Settings", "WarpPlayerInHeli", true);
        }

        internal static void SaveSetting(string setting, bool value)
        {
            ini.Create();
            ini.Write("Features", setting, value);
        }
    }
}
