using System;
using System.Collections.Generic;
using System.Text;

namespace VTOLAPICommons
{
    class Version
    {
        public int major;
        public int minor;
        public int patch;

        public Version(string rawVersion)
        {
            string[] subVersions = rawVersion.Split('.');

            // Malformed version
            if (subVersions.Length > 3)
            {
                major = -1;
                minor = -1;
                patch = -1;
                return;
            }

            major = int.Parse(subVersions[0]);

            if (subVersions.Length >= 2)
            {
                minor = int.Parse(subVersions[1]);
            }
            if (subVersions.Length >= 3)
            {
                patch = int.Parse(subVersions[2]);
            }
        }

        public static bool operator >(Version a, Version b)
        {
            if (a.major > b.major) return true;
            if (a.minor > b.minor) return true;
            if (a.patch > b.patch) return true;
            return false;
        }

        public static bool operator ==(Version a, Version b) => a.major == b.major && a.minor == b.minor && a.patch == b.patch;

        public static bool operator !=(Version a, Version b) => !(a == b);

        public static bool operator >=(Version a, Version b) => a > b || a == b;

        public static bool operator <=(Version a, Version b) => !(a > b);

        public static bool operator <(Version a, Version b) => !(a >= b);
    }
}
