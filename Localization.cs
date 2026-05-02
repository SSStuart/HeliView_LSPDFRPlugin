using Rage;
using System.Collections.Generic;
using System.Globalization;

namespace HeliView
{
    public class Localization
    {
        private readonly string locale;
        private readonly Dictionary<string, Dictionary<string, string>> strings = new Dictionary<string, Dictionary<string, string>>
        {
            {"en",
                new Dictionary<string, string>{ 
                    { "updateAvailable", "~y~Update available!" },
                    { "newsHeliIncompatible", "~r~Disabled! ~w~You are using NewsHeli, which already includes a similar feature." },
                    { "loaded", "~g~Loaded successfully!" },
                    { "pursuitInProgress", "Pursuit in progress" },
                    { "suspectUnderArrest", "Suspect under arrest" },
                    { "vehicle", "vehicle" },
                    { "suspectVehicleDriver", ". Suspect driving a :vehicle" },
                    { "suspectVehiclePassenger", ". Suspect in a :vehicle" },
                    { "controlsHelp", "~b~Ctrl + R~w~ : Exit HeliView\n~b~Ctrl+Shift + R~w~ : Toggle suspect" },
                }
            },
            {"fr",
                new Dictionary<string, string>{
                    { "updateAvailable", "~y~Mise à jour disponible !" },
                    { "newsHeliIncompatible", "~r~Désactivé ! ~w~Vous utilisez NewsHeli, qui comprend déjà une fonctionnalité similaire." },
                    { "loaded", "~g~Chargé !" },
                    { "pursuitInProgress", "Poursuite en cours" },
                    { "suspectUnderArrest", "Suspect en état d'arrestation" },
                    { "vehicle", "véhicule" },
                    { "suspectVehicleDriver", ". Suspect conduisant un(e) :vehicle" },
                    { "suspectVehiclePassenger", ". Suspect dans un(e) :vehicle" },
                    { "controlsHelp", "~b~Ctrl + R~w~ : Quitter HeliView\n~b~Ctrl+Maj + R~w~ : Changer de suspect" },
                }
            }
        };

        public Localization(string locale = "auto")
        {
            if (locale == "auto")
                locale = CultureInfo.InstalledUICulture.Name.Split('-')[0];
            else
                locale = locale.Split('-')[0];

            if (!strings.ContainsKey(locale))
                locale = "en";
            Game.LogTrivial($"[{Main.pluginName}] Localization: Using locale '{locale.ToUpper()}'");

            this.locale = locale;
        }

        public string GetString(string key, params (string key, object value)[] replace)
        {
            string localizedString;
            if (strings[this.locale].ContainsKey(key))
                localizedString = strings[this.locale][key];
            else
            {
                localizedString = key;
                Game.LogTrivial($"[{Main.pluginName}] Localization: Missing translation for key '{key}'");
            }

            foreach (var replacement in replace)
            {
                localizedString = localizedString.Replace($":{replacement.key}", replacement.value?.ToString() ?? "");
            }

            return localizedString;
        }
    }
}
