using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IMLLoader
{
    public class LoaderSettings
    {
        public string BaseGamePath = "";
        public string[] ModsPaths = { "./SimpleModLoader/settings.json" };

        public bool AutoUpdate = true;
        public bool AutoUpdateMods = true;
        public bool DisableLoadOnStart = false;

        public static LoaderSettings Load(string path)
        {
            try
            {
                return JSONHelper.FromJSON<LoaderSettings>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to load settings from {path} because: {e}");
                return null;
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JSONHelper.ToJSON(this));
        }
    }
}
