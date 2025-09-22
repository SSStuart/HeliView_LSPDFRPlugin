using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Net.Configuration;
using System.Reflection;
using System.Windows.Forms;

namespace HeliView
{
    public class Main : Plugin
    {
        public static string pluginName = "HeliView";

        static bool customCameraActive = false;
        static Camera customCamera = new Camera(false);
        static Vehicle heli = null;
        static Ped heliPilot = null;
        static LHandle pursuit;
        static Ped suspect;
        static int suspectIndex = 0;
        static float FOVsuspectLostOffset = 0;
        /*static Ped lostSuspectPos = null;
        static bool suspectLost = false;*/
        static Vehicle playerVehicle;
        static Ped playerCopy;

        //Initialization of the plugin.
        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
 
            Game.LogTrivial(pluginName + " Plugin " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " has been initialised.");
            Game.LogTrivial("Go on duty to fully load " + pluginName + ".");

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
        }

        public override void Finally()
        {
            StopHeliPursuitWithPlayer("immediately");
            Game.LogTrivial(pluginName + " has been cleaned up.");
        }
        
        private static void OnOnDutyStateChangedHandler(bool OnDuty)
        {
            if (OnDuty)
            {
                Game.DisplayNotification("mpinventory", "mp_specitem_heli", pluginName, "V 0.0.1", "~g~Loaded successfully !");

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
                            suspect = Functions.GetPursuitPeds(pursuit)[suspectIndex];
                            if (!customCameraActive)
                                StartHeliPursuitWithPlayer();
                            else
                            {
                                if (Game.IsShiftKeyDownRightNow)
                                    SwitchSuspect();
                                else
                                    StopHeliPursuitWithPlayer();
                            }
                        }
                        // DETECT END OF PURSUIT & OTHER EVENTS THAT SHOULD STOP THE CAMERA
                        if (customCameraActive && 
                            (!Functions.IsPursuitStillRunning(pursuit) 
                            || (heli.Exists() && heli.EngineHealth < 200)
                            || Functions.IsPedArrested(suspect)/*
                            || suspect.IsDead*/))
                        {
                            StopHeliPursuitWithPlayer();
                        }

                        // UPDATE CAMERA FOV
                        if (customCameraActive)
                        {
                            if (suspect.IsRendered)
                            {
                                FOVsuspectLostOffset = Math.Max(0, FOVsuspectLostOffset - 0.05f);
                            } else
                            {
                                FOVsuspectLostOffset = Math.Min(100, FOVsuspectLostOffset + 0.1f);
                            }
                            customCamera.FOV = Math.Max(4, (1 / heli.DistanceTo(suspect) * 2000) + (float)(Math.Sin(Game.GameTime / 10000.0) * 5 - 5) + FOVsuspectLostOffset);
                        }
                    }
                }
            }
        }

        private static void StartHeliPursuit(string mode = "normal")
        {
            if (mode == "normal")
            {
                customCameraActive = true;
                Game.LocalPlayer.HasControl = false;
                if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    Game.LocalPlayer.Character.CurrentVehicle.IsPositionFrozen = true;
                else
                    Game.LocalPlayer.Character.IsPositionFrozen = true;
                Game.FadeScreenOut(500);
                GameFiber.Wait(500);
            }
            heli = new Vehicle("polmav", suspect.GetOffsetPositionUp(100f), suspect.Heading)
            {
                IsPersistent = true,
                IsDriveable = false
            };
            heliPilot = new Ped("s_m_m_pilot_02", heli.GetOffsetPositionUp(-2f), 0f);
            heliPilot.WarpIntoVehicle(heli, -1);
            heli.Velocity = new Vector3(0, 0, 10f);
            heli.Driver.Tasks.ChaseWithHelicopter(suspect, new Vector3(0, -20f, 70f));
            customCamera.AttachToEntity(heli, new Vector3(0, 1, -2), true);
            customCamera.PointAtEntity(suspect, new Vector3(), true);
            customCamera.FOV = Math.Min(4, 1 / heli.DistanceTo(suspect) * 1050);
            customCamera.Shake("HAND_SHAKE", 1f);
            customCamera.Active = true;
            Rage.Native.NativeFunction.Natives.x198F77705FA0931D(suspect);
            GameFiber.Wait(2000);
            Game.FadeScreenIn(500);

            Game.DisplayHelp("~b~Ctrl + C~w~ : Exit HeliView\n" +
                "~b~Ctrl+Shift + C~w~ : Toggle suspect");
        }
        private static void StartHeliPursuitWithPlayer(string mode = "normal")
        {
            if (mode == "normal")
            {
                customCameraActive = true;
                Game.LocalPlayer.HasControl = false;
                if (Game.LocalPlayer.Character.CurrentVehicle != null)
                {
                    playerVehicle = Game.LocalPlayer.Character.CurrentVehicle;
                    playerVehicle.IsPositionFrozen = true;
                    playerVehicle.IsPersistent = true;
                }
                Game.FadeScreenOut(500);
                GameFiber.Wait(500);
                playerCopy = new Ped(Game.LocalPlayer.Character.Model, Game.LocalPlayer.Character.Position, Game.LocalPlayer.Character.Heading);
                playerCopy.IsPersistent = true;
                playerCopy.BlockPermanentEvents = true;
            }
            heli = new Vehicle("polmav", suspect.GetOffsetPositionUp(100f), suspect.Heading)
            {
                IsPersistent = true,
                IsDriveable = false
            };
            heliPilot = new Ped("s_m_m_pilot_02", heli.GetOffsetPositionUp(-2f), 0f);
            heliPilot.WarpIntoVehicle(heli, -1);
            heli.Velocity = new Vector3(0, 0, 10f);
            heli.Driver.Tasks.ChaseWithHelicopter(suspect, new Vector3(0, -20f, 70f));
            customCamera.AttachToEntity(heli, new Vector3(0, 1, -2), true);
            customCamera.PointAtEntity(suspect, new Vector3(), true);
            customCamera.FOV = Math.Min(4, 1 / heli.DistanceTo(suspect) * 1050);
            customCamera.Shake("HAND_SHAKE", 1f);
            customCamera.Active = true;
            Rage.Native.NativeFunction.Natives.x198F77705FA0931D(suspect);
            if (mode == "normal" || mode == "switch")
            {
                Game.LocalPlayer.Character.WarpIntoVehicle(heli, 0);
            }
            GameFiber.Wait(2000);
            Game.FadeScreenIn(500);

            Game.DisplayHelp("~b~Ctrl + C~w~ : Exit HeliView\n" +
                "~b~Ctrl+Shift + C~w~ : Toggle suspect");
        }

        private static void SwitchSuspect()
        {
            int oldSuspectIndex = suspectIndex;
            int nbSuspects = Functions.GetPursuitPeds(pursuit).Length;
            suspectIndex = (suspectIndex + 1) % nbSuspects;
            Game.DisplaySubtitle("Switching to suspect " + (suspectIndex + 1) + "/" + (nbSuspects));
            if (suspectIndex != oldSuspectIndex && !Functions.GetPursuitPeds(pursuit)[suspectIndex].IsDead)
            {
                StopHeliPursuitWithPlayer("switch");
                StartHeliPursuitWithPlayer("switch");
            }
        }

        private static void StopHeliPursuit(string mode = "normal")
        {
            if (mode == "normal" || mode == "switch")
            {
                Game.FadeScreenOut(500);
                GameFiber.Wait(500);
            }
            customCamera.Active = false;
            customCamera.Detach();
            if (heli.Exists())
            {
                if (heli.HasDriver)
                {
                    Functions.RemovePedFromPursuit(heli.Driver);
                    heli.Driver.Delete();
                }
                heli.Delete();
            }
            if (mode == "normal" || mode == "immediately")
            {
                Rage.Native.NativeFunction.Natives.x198F77705FA0931D(Game.LocalPlayer.Character);
                if (mode == "normal")
                {
                    GameFiber.Wait(500);
                    Game.FadeScreenIn(500);
                }
                Functions.RemovePedFromPursuit(Game.LocalPlayer.Character);
                if (Game.LocalPlayer.Character.Tasks.CurrentTaskStatus != TaskStatus.NoTask)
                    Game.LocalPlayer.Character.Tasks.Clear();
                Game.LocalPlayer.HasControl = true;
                if (playerVehicle.Exists())
                    playerVehicle.IsPositionFrozen = false;
                Game.LocalPlayer.Character.IsPositionFrozen = false;
                customCameraActive = false;
            }
        }
        private static void StopHeliPursuitWithPlayer(string mode = "normal")
        {
            if (mode == "normal" || mode == "switch")
            {
                Game.FadeScreenOut(500);
                GameFiber.Wait(500);
            }
            customCamera.Active = false;
            customCamera.Detach();
            if (heli.Exists())
            {
                if (heli.HasDriver)
                {
                    Functions.RemovePedFromPursuit(heli.Driver);
                    heli.Driver.Delete();
                    Game.LocalPlayer.Character.Position = playerCopy.Position;
                }
                heli.Delete();
            }
            if (mode == "normal" || mode == "immediately")
            {
                Rage.Native.NativeFunction.Natives.x198F77705FA0931D(Game.LocalPlayer.Character);
                playerCopy.Delete();
                if (mode == "normal")
                {
                    GameFiber.Wait(500);
                    Game.FadeScreenIn(500);
                }
                Functions.RemovePedFromPursuit(Game.LocalPlayer.Character);
                if (Game.LocalPlayer.Character.Tasks.CurrentTaskStatus != TaskStatus.NoTask)
                    Game.LocalPlayer.Character.Tasks.Clear();
                Game.LocalPlayer.HasControl = true;
                if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    Game.LocalPlayer.Character.CurrentVehicle.IsPositionFrozen = false;
                else
                    Game.LocalPlayer.Character.IsPositionFrozen = false;
                customCameraActive = false;
            }
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