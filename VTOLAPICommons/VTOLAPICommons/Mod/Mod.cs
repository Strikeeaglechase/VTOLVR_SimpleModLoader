using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace VTOLAPICommons
{
    public class Mod
    {
        private string infoPath;
        public string containingFolder;
        private bool loadOnStart;
        private string loadOnStartScene = "ReadyRoom";

        public ModInfo info;

        public bool valid { get; private set; } = false;
        public bool isLoaded { get; private set; } = false;

        // UI related objects
        public GameObject listObj;
        public GameObject settingsObj;
        public GameObject settingsHolderObj;

        private static int nextId = 1;
        public int id { get; private set; }

        private static List<string> loadedPaths = new List<string>();

        public Mod(string path)
        {
            id = nextId++;
            infoPath = path;
            loadOnStart = path.ToLower().Contains("load_on_start") && !Loader.settings.DisableLoadOnStart;
            containingFolder = Path.GetDirectoryName(path);

            Logger.Log($"Registering mod from {path}");
            LoadInfoFile();

            if (info == null)
            {
                Logger.Log($"Failed to load mod info from {path}");
                return;
            }

            if (info.load_on_start_scene != null && info.load_on_start_scene != "")
            {
                loadOnStartScene = info.load_on_start_scene;
            }
        }

        public static bool HasFileBeenLoaded(string path) => loadedPaths.Contains(path);

        private void LoadInfoFile()
        {
            try
            {
                var infoFile = File.ReadAllText(infoPath);
                info = JSONHelper.FromJSON<ModInfo>(infoFile);

                if (info.dll_file == null || info.dll_file == "")
                {
                    Logger.Log($"Info file {infoPath} has no dll_file specified");
                    valid = false;
                }
                else
                {
                    Logger.Log($"Registered mod info for {this}");
                    loadedPaths.Add(infoPath);
                    valid = true;
                }

            }
            catch (Exception e)
            {
                Logger.Log($"Unable to parse mod info file at {infoPath}: {e}");
                valid = false;
            }
        }

        public bool Load()
        {
            Logger.Log($"Loading {this}. Already loaded: {isLoaded}");

            if (isLoaded)
            {
                return true;
            }

            CheckForDependencies();

            var dllPath = Path.Combine(containingFolder, info.dll_file);
            if (!File.Exists(dllPath))
            {
                Logger.Log($"Unable to find dll file at {dllPath} to load {this}");
                return false;
            }

            Logger.Log($"Loading DLL at {dllPath}");

            var types = Assembly.Load(File.ReadAllBytes(dllPath)).GetTypes();
            IEnumerable<Type> source =
                from t in types
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;


            if (source == null || source.Count() != 1)
            {
                Logger.Log($"Unable to load {this} as the provided DLL does not appear to be a valid DLL file");
                return false;
            }


            GameObject newModGo = new GameObject(info.name, source.First());
            VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
            mod.containingFolder = containingFolder;
            newModGo.name = info.name;
            GameObject.DontDestroyOnLoad(newModGo);
            isLoaded = true;


            Logger.Log($"Fully loaded mod {this}");
            mod.ModLoaded();

            return true;
        }

        public bool LoadIfCorrectScene(string sceneName)
        {
            if (!valid || !loadOnStart || loadOnStartScene != sceneName || isLoaded) return false;
            Logger.Log($"Loading {this} for Load-On-Start");
            return Load();
        }

        private void CheckForUpdates()
        {

        }

        private void CheckForDependencies()
        {
            if (info.dependencies == null) return;
            var depsFolder = containingFolder + "/dependencies";
            if (!Directory.Exists(depsFolder))
            {
                Logger.Log($"{this} has listed dependencies but no folder at {depsFolder}");
                return;
            }

            FileInfo[] dlls = new DirectoryInfo(depsFolder).GetFiles("*.dll");
            foreach (var dll in dlls)
            {
                foreach (var modDep in info.dependencies)
                {
                    if (modDep == dll.Name) LoadDependencyDll(dll);
                }
            }
        }

        private void LoadDependencyDll(FileInfo dllFileInfo)
        {
            Assembly assembly = Assembly.Load(File.ReadAllBytes(dllFileInfo.FullName));
            Logger.Log($"Loaded assembly {assembly} for {this}");

            // Id love to refactor this logic...
            // Need to look at changes to allow back/forward compat with existing mod loader
            // Currently this logic does a weird sort of special mod load. It really should just be a normal mod load
            // Additionally dependency resolve logic should switch to using a custom assembly resolver rather than weird ordering and info.json stuff

            IEnumerable<Type> source =
                from t in assembly.GetTypes()
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;

            // This Dependency is a VTOL Mod
            if (source != null && source.Count() == 1)
            {
                GameObject newModGo = new GameObject(dllFileInfo.Name, source.First());
                VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                newModGo.name = dllFileInfo.Name;
                GameObject.DontDestroyOnLoad(newModGo);
                mod.ModLoaded();
                Logger.Log(" >Assembly was a VTOLVR mod and has been loaded");
                return;
            }
        }

        public override string ToString()
        {
            if (info != null)
            {
                return $"{info.name} ({info.pub_id}:{id})";
            }
            else
            {
                return $"Unknown Mod ({id})";
            }
        }
    }
}
