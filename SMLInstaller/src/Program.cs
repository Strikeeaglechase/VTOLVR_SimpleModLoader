using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace VTOLImprovedModLoader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var needsRestart = MaybeElevate();
            if (needsRestart) return;

            try
            {
                var installer = new Installer();
                installer.Install();
            }
            catch (Exception e)
            {
                Logger.Log("Error installing: " + e);
            }
        }

        static bool MaybeElevate()
        {
            if (IsAdministrator() == false)
            {
                // Restart program and run as admin
                string exeName = Process.GetCurrentProcess().MainModule?.FileName;
                Console.WriteLine($"Not started as admin, trying to elevate permissions. Exec name: {exeName}");
                if (exeName == null) return false;

                if (!exeName.EndsWith(".exe"))
                {
                    Console.WriteLine("Needs to elevate but not an exe");
                    return false;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = exeName;
                startInfo.Verb = "runas";

                Process.Start(startInfo);
                return true;
            }

            return false;
        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    class Logger
    {
        private static bool hasCreatedFile = false;

        public static void Log(string message)
        {
            if (!hasCreatedFile) File.WriteAllText("./installer.log", "Start of installer (UDLL)");
            hasCreatedFile = true;
            File.AppendAllText("./installer.log", message + "\n");
            Console.WriteLine(message);
        }
    }
}