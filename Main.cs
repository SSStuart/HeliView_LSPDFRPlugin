using LSPD_First_Response.Mod.API;
using LucasRitter.Scaleforms;
using LucasRitter.Scaleforms.Generic;
using Rage;
using Rage.Native;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HeliView
{
    public class Main : Plugin
    {
        public static string pluginName = "HeliView";
        public static string pluginVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        static bool ENABLE_OVERLAY = true;
        static bool WARP_PLAYER = true;
        static string HELI_TYPE = "cop";

        static bool customCameraActive = false;
        static Camera customCamera = new Camera(false);
        static Vehicle heli = null;
        static Ped heliPilot = null;
        static LHandle pursuit;
        static Ped suspect;
        static int suspectIndex = -1;
        static float FOVsuspectLostOffset = 0;
        /*static Ped lostSuspectPos = null;
        static bool suspectLost = false;*/
        static Vehicle playerVehicle;
        static bool playerInVehicle = false;
        static bool playerVehicleWasPersistent = false;
        static string currentHeliType = "";
        static Vector3 playerPosition;
        static Scaleform newsScaleform = new Scaleform("breaking_news");
        static HeliCam heliCamScaleform = new HeliCam();
        static uint lastNewsUpdate = 0;

        //Initialization of the plugin.
        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
 
            Game.LogTrivial($"{pluginName} Plugin v{pluginVersion} has been initialised.");
            Settings.LoadSettings();
            ENABLE_OVERLAY = Settings.EnableOverlay;
            WARP_PLAYER = Settings.WarpPlayerInHeli;
            HELI_TYPE = Settings.HeliType;
            Game.LogTrivial("[" + pluginName + "] Enable Overlay: " + (ENABLE_OVERLAY? "Yes" : "No"));
            Game.LogTrivial($"[{pluginName}] Enable Overlay: {(ENABLE_OVERLAY ? "Yes" : "No")}");
            Game.LogTrivial($"[{pluginName}] Warp player in Heli: {(WARP_PLAYER ? "Yes" : "No")}");
            Game.LogTrivial($"[{pluginName}] Heli type: {HELI_TYPE}");
            Game.LogTrivial($"Go on duty to fully load {pluginName}.");

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
        }

        public override void Finally()
        {
            StopHeliPursuit(false, true);
            Game.LogTrivial($"{pluginName} has been cleaned up.");
        }
        
        private static void OnOnDutyStateChangedHandler(bool OnDuty)
        {
            if (OnDuty)
            {
                Game.DisplayNotification("mpinventory", "mp_specitem_heli", pluginName, $"V {pluginVersion}", "~g~Loaded successfully !");

                GameFiber.StartNew(ProcessMenus);

                void ProcessMenus()
                {
                    while (true)
                    {
                        GameFiber.Yield();

                        // TOOGLE THE CAMERA
                        if (Game.IsKeyDown(Keys.R) && Game.IsControlKeyDownRightNow && Functions.GetActivePursuit() != null)
                        {
                            pursuit = Functions.GetActivePursuit();
                            int nbSuspects = Functions.GetPursuitPeds(pursuit).Length;
                            suspectIndex = Math.Min(suspectIndex, nbSuspects - 1);
                            suspect = Functions.GetPursuitPeds(pursuit)[suspectIndex];
                            // If the camera is not active, start the heli camera
                            if (!customCameraActive)
                                StartHeliPursuit();
                            else
                            {
                                // If the camera is already active : If the shift key is also pressed, switch to the next suspect in the pursuit, else stop the heli camera
                                if (Game.IsShiftKeyDownRightNow)
                                    SwitchSuspect();
                                else
                                    StopHeliPursuit();
                            }
                        }
                        // DETECT END OF PURSUIT & OTHER EVENTS THAT SHOULD STOP THE CAMERA
                        if (customCameraActive && 
                            (!Functions.IsPursuitStillRunning(pursuit)      // Stop if pursuit ended
                            || (heli.Exists() && heli.EngineHealth < 200)   // or Stop if heli is too damaged
                            || Functions.IsPedArrested(suspect)/*           // or Stop if suspect arrested
                            || suspect.IsDead*/))
                        {
                            StopHeliPursuit();
                        }

                        // UPDATE CAMERA FOV and OVERLAY
                        if (customCameraActive)
                        {
                            if (ENABLE_OVERLAY)
                            {
                                if (currentHeliType == "news")
                                {
                                    // If News heli, display the news overlay
                                    string newsText = Functions.IsPursuitStillRunning(pursuit) ? "Pursuit in progress" : "Suspect under arrest";
                                    if (suspect.IsInAnyVehicle(false))
                                    {
                                        // If the suspect is in a vehicle, try to get the vehicle name and display it (only considering model name with letters)
                                        string vehName = suspect.CurrentVehicle.Model.Name;
                                        if (Regex.IsMatch(vehName, @"^[a-z]+$", RegexOptions.IgnoreCase) && !Functions.IsPedArrested(suspect))
                                            newsText += $". Suspect {(suspect.SeatIndex == -1 ? "driving" : "in")} a {vehName[0].ToString().ToUpper()} {vehName.Substring(1).ToLower()}";
                                    }
                                    // Update the overlay texts with the current area name every 10 seconds
                                    if (lastNewsUpdate < Game.GameTime - 1000 * 10)
                                    {
                                        newsScaleform.CallFunction("SET_TEXT", newsText, Functions.GetZoneAtPosition(suspect.Position).RealAreaName);
                                        lastNewsUpdate = Game.GameTime;
                                    }
                                    newsScaleform.Draw();
                                }
                                else if (currentHeliType == "cop")
                                {
                                    // If Cop heli, display the Heli overlay
                                    // Update the overlay camera parameters and draw it
                                    heliCamScaleform.Heading = customCamera.Rotation.Yaw;
                                    heliCamScaleform.Altitude = heli.Position.Z;
                                    //heliCamScaleform.FieldOfView = customCamera.FOV;      // Not sure what value to use here
                                    heliCamScaleform.Draw();
                                }
                            }
                            // Zoom in / out depending on the suspect visibility
                            if (suspect.IsRendered)
                                FOVsuspectLostOffset = Math.Max(0, FOVsuspectLostOffset - 0.05f);
                            else
                                FOVsuspectLostOffset = Math.Min(100, FOVsuspectLostOffset + 0.1f);
                            // Update the camera zoom (FOV) depending on the distance to the suspect with some oscillation, and suspect visibility
                            customCamera.FOV = Math.Max(4, (1 / heli.DistanceTo(suspect) * 2000) + (float)(Math.Sin(Game.GameTime / 10000.0) * 5 - 5) + FOVsuspectLostOffset);
                        }
                    }
                }
            }
        }

        private static void StartHeliPursuit(bool switching = false)
        {
            // Selecting suspect
            suspect = GetNextSuspect();
            if (suspect == null)
            {
                Game.LogTrivial($"[{pluginName}] No suspect found in pursuit");
                return;
            }
            
            // Select heli type (cop / news) according to settings
            currentHeliType = HELI_TYPE == "cop" ? "cop" : (HELI_TYPE == "news" ? "news" : (new Random().Next(2) == 1 ? "cop" : "news"));
            Game.LogTrivial($"[{pluginName}] StartHeliPursuit with Heli type '{currentHeliType}'");

            // Spawn the heli and pilot
            heli = new Vehicle((currentHeliType == "cop" ? "polmav" : "maverick"), suspect.GetOffsetPositionUp(100f), suspect.Heading)
            {
                IsPersistent = true,
                IsDriveable = false,
                IsCollisionEnabled = false
            };
            heliPilot = new Ped("s_m_m_pilot_02", heli.GetOffsetPositionUp(-2f), 0f);
            heliPilot.WarpIntoVehicle(heli, -1);
            heli.Velocity = new Vector3(0, 0, 10f);
            // Make the heli chase the suspect
            heli.Driver.Tasks.ChaseWithHelicopter(suspect, new Vector3(((float)Math.Sin(Game.GameTime / 1000) * 100f), ((float)Math.Sin(Game.GameTime / 1000) * -10f - 20f), 70f));
            // Add the pilot to the pursuit to avoid losing the suspect (if the player is not warped in the heli)
            if (!WARP_PLAYER)
                Functions.AddCopToPursuit(Functions.GetActivePursuit(), heliPilot);
            if (!switching)
            {
                // Save player (and player vehicle) position, make player invincible and remove control
                Game.LocalPlayer.HasControl = false;
                playerPosition = Game.LocalPlayer.Character.Position;
                Game.LocalPlayer.Character.IsInvincible = true;
                if (Game.LocalPlayer.Character.CurrentVehicle != null)
                {
                    playerVehicle = Game.LocalPlayer.Character.CurrentVehicle;
                    playerVehicle.IsPositionFrozen = true;
                    playerVehicleWasPersistent = playerVehicle.IsPersistent;
                    playerVehicle.IsPersistent = true;
                    playerInVehicle = true;
                }
                else if (Game.LocalPlayer.Character.LastVehicle.Exists())
                {
                    playerVehicle = Game.LocalPlayer.Character.LastVehicle;
                    playerVehicle.IsPositionFrozen = true;
                    playerVehicleWasPersistent = playerVehicle.IsPersistent;
                    playerVehicle.IsPersistent = true;
                    playerInVehicle = false;
                }
                else
                {
                    Game.LocalPlayer.Character.IsPositionFrozen = true;
                    playerVehicle = null;
                    playerInVehicle = false;
                }
                // Fade screen out and Hide radar (if overlay enabled)
                Game.FadeScreenOut(500);
                GameFiber.Wait(500);
                if (ENABLE_OVERLAY)
                    NativeFunction.Natives.DISPLAY_RADAR(false);
                customCameraActive = true;
            }
            // Warp player in heli (if setting enabled)
            if (WARP_PLAYER)
                Game.LocalPlayer.Character.WarpIntoVehicle(heli, 0);

            // Setup the custom camera
            customCamera.AttachToEntity(heli, new Vector3(0, 1, -2), true);
            customCamera.PointAtEntity(suspect, new Vector3(), true);
            customCamera.FOV = Math.Min(4, 1 / heli.DistanceTo(suspect) * 1050);
            customCamera.Shake("HAND_SHAKE", 1f);
            customCamera.Active = true;
            heli.IsCollisionEnabled = true; // Re-enable heli collision
            // If player not warped in heli, make the map load arround the suspect
            if (!WARP_PLAYER)
                NativeFunction.Natives.SET_FOCUS_ENTITY(suspect);

            // Fade screen in and display controls help
            GameFiber.Wait(2000);
            Game.FadeScreenIn(500);

            Game.DisplayHelp("~INPUT_VEH_HORN~ ~b~Ctrl + R~w~ : Exit HeliView\n" +
                "~b~Ctrl+Shift + R~w~ : Toggle suspect");
        }

        private static void SwitchSuspect()
        {
            Game.LogTrivial($"[{pluginName}] SwitchSuspect");
            // Select next suspect in pursuit
            Ped nextSuspect = GetNextSuspect();
            if (nextSuspect == null)
            {
                Game.LogTrivial($"[{pluginName}] No other suspect found in pursuit, aborting switch");
                return;
            }

            suspect = nextSuspect;
            if (heli.DistanceTo2D(suspect) < 200)
            {
                // Switch target
                Game.LogTrivial($"[{pluginName}] Switching to suspect already close to heli");
                heli.Driver.Tasks.ChaseWithHelicopter(suspect, new Vector3(((float)Math.Sin(Game.GameTime / 1000) * 100f), ((float)Math.Sin(Game.GameTime / 1000) * -10f - 20f), 70f));
                customCamera.PointAtEntity(suspect, new Vector3(), true);
                customCamera.FOV = Math.Min(4, 1 / heli.DistanceTo(suspect) * 1050);
            }
            else
            {
                // Restart the heli pursuit on the new suspect
                Game.LogTrivial($"[{pluginName}] Switching to suspect far from heli, restarting heli pursuit");
                StopHeliPursuit(true);
                StartHeliPursuit(true);
            }
        }

        private static void StopHeliPursuit(bool switching = false, bool immediately = false)
        {
            Game.LogTrivial($"[{pluginName}] StopHeliPursuit");

            if (!immediately)
            {
                // Fade screen out, and show radar again
                Game.FadeScreenOut(500);
                GameFiber.Wait(500);
                if (ENABLE_OVERLAY)
                    NativeFunction.Natives.DISPLAY_RADAR(true);
            }
            if (!switching)
            {
                // If the player had an active vehicle
                if (playerVehicle != null && playerVehicle.Exists())
                {
                    // Restore player vehicle attributes, and warp player back in it (if the player was in the vehicle), or just reposition the player on foot
                    playerVehicle.IsPositionFrozen = false;
                    playerVehicle.IsPersistent = playerVehicleWasPersistent;
                    if (WARP_PLAYER && playerInVehicle)
                        Game.LocalPlayer.Character.WarpIntoVehicle(playerVehicle, -1);
                    else if (WARP_PLAYER && !playerInVehicle)
                        Game.LocalPlayer.Character.Position = playerPosition;
                }
                // If the player had no vehicle, just reposition the player on foot
                else
                    Game.LocalPlayer.Character.Position = playerPosition;
            }
            // Restore player control and attributes
            Game.LocalPlayer.Character.IsInvincible = false;
            Game.LocalPlayer.Character.IsPositionFrozen = false;
            // Detach and disable the custom camera
            customCamera.Active = false;
            customCamera.Detach();
            // Remove the heli and pilot
            if (heli.Exists())
            {
                if (heli.HasDriver)
                {
                    Functions.RemovePedFromPursuit(heli.Driver);
                    heli.Driver.Delete();
                }
                heli.Delete();
            }
            if (!switching)
            {
                // If the player was not warped in the heli, make the map load arround the player again
                if (!WARP_PLAYER)
                    NativeFunction.Natives.SET_FOCUS_ENTITY(Game.LocalPlayer.Character);
                if (!immediately)
                {
                    // Fade screen in
                    GameFiber.Wait(500);
                    Game.FadeScreenIn(500);
                }
                // Bring back player control
                Game.LocalPlayer.HasControl = true;
                customCameraActive = false;
            }
        }

        private static Ped GetNextSuspect()
        {
            // Get the current active pursuit
            LHandle pursuitHandle = Functions.GetActivePursuit();
            if (pursuitHandle == null || !Functions.IsPursuitStillRunning(pursuitHandle))
            {
                Game.LogTrivial($"[{pluginName}] No active pursuit");
                return null;
            }

            // Get the list of suspects in the pursuit
            var suspects = Functions.GetPursuitPeds(pursuitHandle);
            if (suspects == null || suspects.Length == 0)
            {
                Game.LogTrivial($"[{pluginName}] No suspect in pursuit");
                return null;
            }

            // Cycle through the suspects to get the next one
            int nbSuspects = suspects.Length;
            int oldSuspectIndex = suspectIndex;
            suspectIndex = (suspectIndex + 1) % nbSuspects;
            suspect = suspects[suspectIndex];
            if (suspect == null || !suspect.Exists() || (suspectIndex == oldSuspectIndex && customCameraActive))
            {
                if (suspectIndex == oldSuspectIndex)
                    Game.LogTrivial($"[{pluginName}] Same suspect, no change");
                else
                    Game.LogTrivial($"[{pluginName}] Suspect {suspectIndex + 1}/{nbSuspects} does not exist anymore");
                return null;
            }

            Game.LogTrivial($"[{pluginName}] Switching to suspect {suspectIndex + 1}/{nbSuspects}");
            return suspect;
        }

        public static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
        }

        public static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName assemblyName = assembly.GetName();
                if (assemblyName.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || assemblyName.Version.CompareTo(minversion) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}