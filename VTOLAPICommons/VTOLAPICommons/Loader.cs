using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using UnityEngine;
//using Doorstop;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Harmony;
using System.IO.Compression;
using UnityEngine.CrashReportHandler;
using System.Reflection;

namespace VTOLAPICommons
{
    public class Loader
    {
        private const string logFileName = "loader.log";

        public static Loader instance;
        private bool hasPatched = false;
        private bool gameHasLoaded = false;

        private const string harmonyId = "com.strik.iml.loader";
        private HarmonyInstance harmony;
        // public List<Mod> allMods = new List<Mod>();
        public List<Mod> mods = new List<Mod>();

        public static LoaderSettings settings;
        private static bool needsGameExit;
        private InGameUIManager uiController;

        private bool isNoVr = false;
        private bool addGettersSetters = false;


        public void Start()
        {
            try
            {
                File.WriteAllText(logFileName, "Doorstop has started IMLLoader\n");
                instance = this;

                var args = Environment.GetCommandLineArgs();
                foreach (var arg in args)
                {
                    if (arg == "-novr")
                    {
                        isNoVr = true;
                        Log($"-novr flag detected, disabling VR");
                    }

                    if (arg == "-getset")
                    {
                        addGettersSetters = true;
                        Log($"-getset flag detected, patching with getters and setters");
                    }
                }

                Log("Args: " + string.Join(", ", args));

                SetupCustomAssemblyResolver();
                settings = LoaderSettings.Load("./SimpleModLoader/settings.json");
                if (settings == null)
                {
                    needsGameExit = true;
                    return;
                }

                Log("Registering on scene load handler and starting patch");

                SceneManager.sceneLoaded += SceneLoaded;

                CreatePatchedDLL();
            }
            catch (Exception e)
            {
                Log($"Error with Loader Start: {e}");
            }
        }

        private void CreatePatchedDLL()
        {
            var patcher = new VTPatcher(settings.BaseGamePath, isNoVr, addGettersSetters);
            patcher.Start();
        }

        public void Log(string message)
        {
            File.AppendAllText(logFileName, message + "\n");
            if (gameHasLoaded) Debug.Log($"[IML] {message}");
        }

        private void AddGameStartupPatch()
        {
            Log("A scene load has occured, setting up harmony and patching");
            harmony = HarmonyInstance.Create(harmonyId);

            try
            {
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                Log($"Unable to patch: {e}");
            }


            hasPatched = true;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Logger.Log($"Scene load: {scene.name}");
            if (needsGameExit)
            {
                Application.Quit();
                return;
            }

            if (!instance.hasPatched) instance.AddGameStartupPatch();

            if (scene.name == "ReadyRoom")
            {
                instance.DoGameReadyInit();
            }
            foreach (var mod in instance.mods)
            {
                mod.MaybeLoadForScene(scene.name);
            }
        }

        public void GameStartupAwake(GameStartup gv)
        {
            gameHasLoaded = true;
            Log("Game startup was called, beginning mod loader setup!");
            CrashReportHandler.enableCaptureExceptions = false;

            // Create mod loader object
            var mlObject = new GameObject("SimpleModLoader", typeof(ModLoaderObj));
            var obj = mlObject.GetComponent<ModLoaderObj>();
            uiController = new InGameUIManager(obj);
        }

        private void DoGameReadyInit()
        {
            uiController.CreateUI();
        }

        private void ExtractAnyZippedFolders(string path)
        {
            if (!Directory.Exists(path)) return;

            var subDirs = Directory.GetDirectories(path);
            foreach (var sd in subDirs) ExtractAnyZippedFolders(sd);

            var zips = Directory.GetFiles(path, "*.zip");
            foreach (var zipFile in zips)
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(zipFile);
                    var nameWithExt = Path.GetFileName(zipFile);
                    var pathWithoutName = zipFile.Substring(0, zipFile.Length - nameWithExt.Length);
                    Log($"Unzipping {zipFile}");
                    ZipFile.ExtractToDirectory(zipFile, pathWithoutName + name);
                    File.Delete(zipFile);
                }
                catch (Exception e)
                {
                    Log($"Unable to unzip file {zipFile} because: {e}");
                }
            }
        }

        private void LocateMods(string path)
        {
            if (!Directory.Exists(path)) return;

            var subDirs = Directory.GetDirectories(path);
            // Register load_on_start mods first, this is incase the user left a copy of the mod outside of los, but we want to make sure we load the los one
            foreach (var sd in subDirs)
            {
                if (sd.ToLower().Contains("load_on_start")) LocateMods(sd);
            }

            foreach (var sd in subDirs)
            {
                if (!sd.ToLower().Contains("load_on_start")) LocateMods(sd);
            }

            var modJsons = Directory.GetFiles(path, "*.json");
            foreach (var modInfoPath in modJsons)
            {
                if (Mod.HasFileBeenLoaded(modInfoPath)) continue; // Don't reload mods that have already been loaded

                var mod = new Mod(modInfoPath);
                if (!mod.valid) continue;
                var existing = GetModByPubId(mod.info.pub_id);
                if (existing != null)
                {
                    Logger.Log($"Mod {mod.info.name} has the same pub_id as {existing.info.name} ({mod.info.pub_id}) despite a different filepath");
                    // continue;
                }

                mods.Add(mod);

                if (uiController != null) uiController.SetupNewModObjectUi(mod);
            }
        }

        public void RunLoadModsRoutine()
        {
            foreach (var modsFolderPath in settings.ModsPaths)
            {
                ExtractAnyZippedFolders(modsFolderPath);
                LocateMods(modsFolderPath);
            }
        }

        public static Mod GetModByPubId(string pid)
        {
            foreach (var m in instance.mods)
            {
                if (m.info.pub_id == pid) return m;
            }

            return null;
        }

        public static Mod GetMod(int id)
        {
            foreach (var m in instance.mods)
            {
                if (m.id == id) return m;
            }

            return null;
        }

        private void SetupCustomAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name);
                var nameToUse = name.Name;// == "ModLoader" ? "ModLoaderPolyfill" : name.Name;

                var dllPath = Path.GetFullPath("./SimpleModLoader/" + nameToUse + ".dll");
                Logger.Log($"Assembly resolve req. Raw: {args.Name}, parsed: {name.Name}, chosen: {nameToUse}. Possible DLL: {dllPath}");
                if (File.Exists(dllPath))
                {
                    return Assembly.LoadFrom(dllPath);
                }

                return null;
            };
        }
    }

    public class Logger
    {
        public static void Log(string message)
        {
            if (Loader.instance != null)
            {
                Loader.instance.Log(message);
            }
            else
            {
                Console.WriteLine("[IML] " + message);
            }
        }
    }
}