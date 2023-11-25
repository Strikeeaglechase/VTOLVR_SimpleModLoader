using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VDFParser.Models;

namespace VTOLImprovedModLoader
{
    class Installer
    {
        public const string InstallFolderName = "VTOL VR Modded";
        public const string ModdedExeName = "VTOLVR.exe";
        public string RootAssetsPath = "../../../root_assets";
        public string AssetsPath = "../../../assets";
        public const string FolderName = "SimpleModLoader";

        private string vtolPath = "";
        private string moddedPath = "";
        private string steamPath = "";

        private List<string> rootCopyAssets = new List<string>();
        private List<string> filesToIgnore = new List<string> { "VTOLVR_ModLoader", "VTOLVR_Data", "Managed", "Assembly-CSharp.dll", ModdedExeName };

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);
        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }


        public void Install()
        {
            UpdateAssetPaths();

            vtolPath = LocateVTOLInstallDirectory();
            moddedPath = vtolPath.Substring(0, vtolPath.Length - 7) + InstallFolderName;
            steamPath = LocateSteamInstallDirectory();
            Logger.Log($"VTOL install path: {vtolPath}");
            Logger.Log($"Modded install path: {moddedPath}");
            Logger.Log($"Steam install path: {steamPath}");

            if (vtolPath == "" || moddedPath == "" || steamPath == "")
            {
                Logger.Log($"Unable to resolve one of the directories!");
                return;
            }

            if (Directory.Exists(moddedPath))
            {
                BoldLog($"A modded install already exists, do you want to overwrite it?");
                if (!PromptYesNo()) return;
            }
            else
            {
                Directory.CreateDirectory(moddedPath);
            }

            LoadRootCopyAssets();

            // Create copy of the game, without symlinking Assembly-CSharp.dll or VTOLVR.exe
            CreateSymLinks("");
            CreateSymLinks("VTOLVR_Data/");
            CreateSymLinks("VTOLVR_Data/Managed/");

            // Copy in the Assembly-CSharp.dll amd VTOLVR.exe
            CopyOverwrite(vtolPath + "/VTOLVR.exe", moddedPath + "/" + ModdedExeName);
            CopyOverwrite(vtolPath + "/VTOLVR_Data/Managed/Assembly-CSharp.dll", moddedPath + "/VTOLVR_Data/Managed/Assembly-CSharp.dll");

            // Copy in assets folder
            CopyAssetsToGame();
            MaybeCreateModsFolder();
            CreateSettingsFile();

            RegisterExternalSteamGame();

            Logger.Log("\n\n\n\n\n\n\n\n");
            Logger.Log("==================================================================");
            BoldLog($"Installation complete!\n YOU NEED TO RESTART STEAM. After restarting steam you should see \"VTOL VR (Modded)\" as a game\n\nPut mods into this folder: {moddedPath}/{FolderName}/mods");
            Logger.Log("==================================================================");

            Logger.Log("\n\nPress enter to exit...");
            Console.ReadLine();
        }

        private void UpdateAssetPaths()
        {
            if (!Directory.Exists(RootAssetsPath))
            {
                RootAssetsPath = "./root_assets";
                AssetsPath = "./assets";
                Logger.Log($"Switching to non-dev asset paths: {RootAssetsPath}, {AssetsPath}");
            }
        }

        private void MaybeCreateModsFolder()
        {
            var modsPath = moddedPath + "/" + FolderName + "/mods";
            if (!Directory.Exists(modsPath))
            {
                Logger.Log($"Creating mods folder at {modsPath}");
                Directory.CreateDirectory(modsPath);
            }
            else
            {
                Logger.Log($"Mods folder already exists!");
            }

            if (!Directory.Exists(modsPath + "/load_on_start"))
            {
                Logger.Log($"Creating load on start folder at {modsPath}/load_on_start");
                Directory.CreateDirectory(modsPath + "/load_on_start");
            }
            else
            {
                Logger.Log($"Load on start folder already exists!");
            }
        }

        private void CreateSettingsFile()
        {
            var settingsPath = moddedPath + "/" + FolderName + "/settings.json";
            var settings = new LoaderSettings();
            settings.BaseGamePath = vtolPath;
            settings.ModsPaths = new string[] { FolderName + "/mods", vtolPath + "/VTOLVR_ModLoader/mods" };

            settings.Save(settingsPath);
        }

        private void CopyAssetsToGame()
        {
            foreach (var ca in rootCopyAssets)
            {
                Logger.Log($"Copying root asset {ca} to {moddedPath}/{ca}");
                CopyOverwrite(RootAssetsPath + "/" + ca, moddedPath + "/" + ca);
            }

            if (!Directory.Exists(moddedPath + "/" + FolderName))
            {
                Logger.Log($"Creating new folder at {moddedPath + "/" + FolderName}");
                Directory.CreateDirectory(moddedPath + "/" + FolderName);
            }

            var assets = Directory.GetFiles(AssetsPath);
            foreach (var asset in assets)
            {
                var name = Path.GetFileName(asset);
                CopyOverwrite(asset, moddedPath + "/" + FolderName + "/" + name);
            }
        }

        private void LoadRootCopyAssets()
        {
            var files = Directory.GetFiles(RootAssetsPath);
            rootCopyAssets = files.Select(f => Path.GetFileName(f)).ToList();

            foreach (var ca in rootCopyAssets) filesToIgnore.Add(ca);
        }

        private List<string> GetSteamUserFolders()
        {
            var users = Directory.GetDirectories(steamPath + "/userdata");
            return users.Where(userPath => Directory.Exists(userPath + "/config")).ToList();
        }

        private void CopyOverwrite(string source, string dest)
        {
            if (File.Exists(dest))
            {
                File.Delete(dest);
                Logger.Log($"Deleting {dest}");
            }
            Logger.Log($"Copy: {source} -> {dest}");
            File.Copy(source, dest);
        }

        private List<VDFEntry> GetVDFEntries(string path)
        {
            try
            {
                return VDFParser.VDFParser.Parse(path + "/config/shortcuts.vdf").ToList();
            }
            catch (Exception e)
            {
                return new List<VDFEntry>();
            }
        }

        private void RegisterExternalSteamGame()
        {
            var users = GetSteamUserFolders();

            foreach (var steamUserPath in users)
            {
                var vdfEntries = GetVDFEntries(steamUserPath);

                VDFEntry entryToUpdate = null;
                foreach (var entry in vdfEntries)
                {
                    if (entry.Exe == null || entry.Exe == "") continue;
                    var exeName = Path.GetFileName(entry.Exe);
                    if (exeName == ModdedExeName)
                    {
                        entryToUpdate = entry;
                        Logger.Log($"Found an existing VDF entry for VTOL VR Modded");
                        break;
                    }
                }

                if (entryToUpdate == null)
                {
                    entryToUpdate = new VDFEntry();
                    vdfEntries.Add(entryToUpdate);
                    Logger.Log($"Creating a new VDF entry");
                }

                entryToUpdate.AppName = "VTOL VR (Modded)";
                entryToUpdate.Exe = moddedPath + "/" + ModdedExeName;
                entryToUpdate.StartDir = moddedPath;
                entryToUpdate.Tags = new string[0];

                var data = VDFParser.VDFSerializer.Serialize(vdfEntries.ToArray());
                File.WriteAllBytes(steamUserPath + "/config/shortcuts.vdf", data);
            }
        }

        private void CreateSymLinks(string prefix)
        {
            var files = Directory.GetFiles(vtolPath + "/" + prefix);
            foreach (var file in files) SymlinkFile(file, prefix, false);

            var dirs = Directory.GetDirectories(vtolPath + "/" + prefix);
            foreach (var dir in dirs) SymlinkFile(dir, prefix, true);
        }

        private void SymlinkFile(string filePath, string prefix, bool isDir)
        {
            var file = Path.GetFileName(filePath);
            if (filesToIgnore.Contains(file))
            {
                Logger.Log($"Skipping {file}");
                return;
            }

            if (!Directory.Exists(moddedPath + "/" + prefix)) Directory.CreateDirectory(moddedPath + "/" + prefix);

            var resultFileName = file;
            if (file == "VTOLVR.exe") resultFileName = ModdedExeName;

            var sourcePath = vtolPath + "/" + prefix + file;
            var destPath = moddedPath + "/" + prefix + resultFileName;
            Logger.Log($"Symlinking {sourcePath} -> {destPath}");

            if (isDir)
            {
                if (Directory.Exists(destPath)) Directory.Delete(destPath);
                // Directory.CreateSymbolicLink(destPath, sourcePath);
                // fac.SymlinkCreate(sourcePath, destPath);
                CreateSymbolicLink(destPath, sourcePath, SymbolicLink.Directory);
            }
            else
            {
                if (File.Exists(destPath)) File.Delete(destPath);
                // File.CreateSymbolicLink(destPath, sourcePath);
                // fac.SymlinkCreate(sourcePath, destPath);
                CreateSymbolicLink(destPath, sourcePath, SymbolicLink.File);
            }
        }

        private bool PromptYesNo()
        {
            BoldLog("[Y/N]:");
            while (true)
            {
                ConsoleKeyInfo val = Console.ReadKey();
                var key = val.KeyChar.ToString().ToLower();
                if (key == "y")
                {
                    Logger.Log("");
                    return true;
                }
                if (key == "n")
                {
                    Logger.Log("");
                    return false;
                }
            }
        }

        private string LocateVTOLInstallDirectory()
        {
            string[] pathFrags =
            {
                "Program Files (x86)/Steam/steamapps/common/VTOL VR",
                "Program Files/Steam/steamapps/common/VTOL VR",
                "steamapps/common/VTOL VR",
                "SteamLibrary/steamapps/common/VTOL VR"
            };

            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                foreach (var pathFrag in pathFrags)
                {
                    var pathToCheck = drive.Name + pathFrag;
                    if (Directory.Exists(pathToCheck))
                    {
                        return pathToCheck;
                    }
                }
            }

            return PromptForPath("Unable to automatically locate VTOL VR install directory! Please enter the path to VTOL VR:");
        }

        private string LocateSteamInstallDirectory()
        {
            string[] possiblePaths = { "C:/Program Files (x86)/Steam", "C:/Program Files/Steam", "C:/Steam" };
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path)) return path;
            }

            return PromptForPath("Unable to automatically locate Steam directory. Please manually enter the path:");
        }

        private string PromptForPath(string prompt)
        {
            Logger.Log(prompt);
            while (true)
            {
                string path = Console.ReadLine();
                if (path == null) Logger.Log("No path given..");

                if (Directory.Exists(path)) return path;

                Logger.Log($"No folder found at \"{path}\"");
            }
        }

        private void BoldLog(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Logger.Log(message);
            Console.ResetColor();
        }
    }
}
