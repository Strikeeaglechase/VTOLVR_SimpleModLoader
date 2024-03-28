using Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace IMLLoader
{
    [HarmonyPatch(typeof(GameStartup), "Awake")]
    class GameStartupPatch
    {
        public static void Prefix(GameStartup __instance)
        {
            Loader.instance.GameStartupAwake(__instance);
        }
    }

    [HarmonyPatch(typeof(Application), nameof(Application.Quit), new Type[] { } )] 
    class ApplicationQuitPatch
    {
        public static bool Prefix()
        {
            UnityEngine.Debug.Log($"Attempting to kill current process!");
            Process.GetCurrentProcess().Kill();
            return false;
        }
    }
}
