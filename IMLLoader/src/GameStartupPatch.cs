using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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

    // [HarmonyPatch(typeof(GameVersion), "Parse")]
    // class GameVersionParsePatch
    // {
    //     public static void Postfix(ref GameVersion __result)
    //     {
    //         var newVersion = new GameVersion(__result.majorVersion, __result.betaVersion, __result.alphaVersion, __result.buildVersion, GameVersion.ReleaseTypes.Modded);
    //         Logger.Log($"Patching struct! Result was: {__result}, changing to: {newVersion}");
    //         __result = newVersion;
    //         Logger.Log($"New version struct parse postfix: {__result}");
    //     }
    // }

    // [HarmonyPatch(typeof(GameVersion), "ConstructFromValue")]
    // class GameVersionConstructPatch
    // {
    //     public static void Prefix(ref string s)
    //     {
    //         Logger.Log("GameVersion.ConstructFromValue prefix called");
    //         Logger.Log($"GameVersion.ConstructFromValue input : {s}");
    //     }
    // 
    //     public static void Postfix(GameVersion __instance)
    //     {
    //         Logger.Log($"GameVersion.ConstructFromValue called");
    //         Logger.Log($"GameVersion.ConstructFromValue : {__instance}");
    // 
    //         __instance.releaseType = GameVersion.ReleaseTypes.Modded;
    //         Logger.Log($"GameVersion.ConstructFromValue : New version : {__instance}");
    // 
    //     }
    // 
    // }

    // [HarmonyPatch(typeof(VersionText), "Start")]
    // class VersionTextStartPatch
    // {
    //     public static void Prefix()
    //     {
    //         Loader.UpdateGameVersion();
    //     }
    // }

    // [HarmonyPatch(typeof(VTBitConverter.BitConverter), "GetNetConnectionID")]
    // class BitConvertNidPatch
    // {
    //     public static void Postfix()
    //     {
    //         string location = Assembly.GetCallingAssembly().Location;
    //         Logger.Log($"NID Location assembly: {location}");
    //     }
    // }
}
