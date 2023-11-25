using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLImprovedModLoader
{
    public class LoaderSettings
    {
        public string BaseGamePath = "";
        public string[] ModsPaths = { };

        public bool AutoUpdate = true;
        public bool AutoUpdateMods = true;
        public bool DisableLoadOnStart = false;

        public static LoaderSettings Load(string path)
        {
            return JSONHelper.FromJSON<LoaderSettings>(path);
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JSONHelper.ToJSON(this));
        }
    }
}
