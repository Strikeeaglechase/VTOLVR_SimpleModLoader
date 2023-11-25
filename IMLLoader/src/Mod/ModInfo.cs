using System;
using System.Collections.Generic;
using System.Text;

namespace IMLLoader
{
    public class ModInfo
    {
        public string name;
        public string description;
        public string dll_file;
        public string pub_id;
        public string version;

        public List<string> dependencies;
        public List<string> mod_dependencies;
    }
}
