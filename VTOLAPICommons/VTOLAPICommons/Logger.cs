using System;
using System.Collections.Generic;
using System.Text;

namespace VTOLAPICommons
{
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
