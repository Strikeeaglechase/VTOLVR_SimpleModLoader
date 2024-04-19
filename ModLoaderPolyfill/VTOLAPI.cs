using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections;
using VTOLVR.Multiplayer;
using VTOLAPICommons;

/// <summary>
/// This is the VTOL VR Modding API which aims to simplify repetitive tasks.
/// </summary>
public class VTOLAPI : MonoBehaviour
{
    // public static 

    public enum ErrorResult { None, NotRegistered, KeyNotFound }

    /// <summary>
    /// This is the current instance of the API in the game world.
    /// </summary>
    public static VTOLAPI instance { get; private set; }

    /// <summary>
    /// This gets invoked when the scene has changed and finished loading. 
    /// This should be the safest way to start running code when a level is loaded.
    /// </summary>
    public static UnityAction<VTOLScenes> SceneLoaded;

    /// <summary>
    /// This gets invoked when the mission as been reloaded by the player.
    /// </summary>
    public static UnityAction MissionReloaded;

    /// <summary>
    /// The current scene which is active.
    /// </summary>
    public static VTOLScenes currentScene { get; private set; }

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
        instance = this;
        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }

    private void ActiveSceneChanged(Scene current, Scene next)
    {
        Debug.Log($"Active Scene Changed to [{next.buildIndex}]{next.name}");
        var scene = (VTOLScenes)next.buildIndex;
        switch (scene)
        {
            case VTOLScenes.Akutan:
            case VTOLScenes.CustomMapBase:
            case VTOLScenes.CustomMapBase_OverCloud:
                StartCoroutine(WaitForScenario(scene));
                break;
            default:
                CallSceneLoaded(scene);
                break;
        }
    }

    private IEnumerator WaitForScenario(VTOLScenes Scene)
    {
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady) yield return null;
        CallSceneLoaded(Scene);
    }

    private void CallSceneLoaded(VTOLScenes Scene)
    {
        currentScene = Scene;
        if (SceneLoaded != null) SceneLoaded.Invoke(Scene);
    }

    /// <summary>
	/// Creates a settings page in the `mod settings` tab.
	/// Make sure to fully create your settings before calling this as you 
	/// can't change it onces it's created.
	/// </summary>
	/// <param name="newSettings"></param>
	public static void CreateSettingsMenu(Setting newSettings)
    {
        if (ModLoaderObj.instance == null || ModLoaderObj.instance.uiManager == null)
        {
            Debug.LogError("The Mod Loaders Instance is null. We haven't reached the Main Room Scene yet");
        }
        else
        {
            ModLoaderObj.instance.uiManager.CreateSettingsMenu(newSettings);
        }
    }

#region Steam Related Methods

    /// <summary>
    /// Returns the steam ID of the player which is using this mod.
    /// </summary>
    [Obsolete]
    public ulong GetSteamID() => SteamClient.SteamId;

    /// <summary>
    /// Returns the steam ID of the player which is using this mod.
    /// </summary>
    public static ulong SteamId() => SteamClient.SteamId;

    /// <summary>
    /// Returns the current name of the steam user, if they change their name during play session, this doesn't update.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public string GetSteamName() => SteamClient.Name;

    /// <summary>
    /// Returns the current name of the steam user, if they change their name during play session, this doesn't update.
    /// </summary>
    public static string SteamName() => SteamClient.Name;

#endregion

    /// <summary>
    /// [MP Supported]
    /// Searches for the game object of the player by using the prefab name appending (Clone).
    /// For multiplayer it uses the lobby manager to get the local player
    /// </summary>
    /// <returns></returns>
    public static GameObject GetPlayersVehicleGameObject()
    {
        if (VTOLMPUtils.IsMultiplayer())
        {
            return VTOLMPLobbyManager.localPlayerInfo.vehicleObject;
        }

        string vehicleName = PilotSaveManager.currentVehicle.vehiclePrefab.name;
        return GameObject.Find($"{vehicleName}(Clone)");
    }

    /// <summary>
    /// Returns which vehicle the player is using in a Enum.
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public static VTOLVehicles GetPlayersVehicleEnum()
    {
        if (PilotSaveManager.currentVehicle == null)
            return VTOLVehicles.None;

        string vehicleName = PilotSaveManager.currentVehicle.vehicleName;
        switch (vehicleName)
        {
            case "AV-42C":
                return VTOLVehicles.AV42C;
            case "F/A-26B":
                return VTOLVehicles.FA26B;
            case "F-45A":
                return VTOLVehicles.F45A;
            case "AH-94":
                return VTOLVehicles.AH94;
            default:
                return VTOLVehicles.None;
        }
    }



    /// <summary>
    /// Returns a list of mods which are currently loaded
    /// </summary>
    /// <returns></returns>
    public static List<Mod> GetUsersMods()
    {
        if (Loader.instance == null) return new List<Mod>();
        return Loader.instance.mods.Where(m => m.isLoaded && m.valid).ToList();
    }


    /// <summary>
    /// Returns an ordered string of mods which are currently loaded
    /// </summary>
    /// <returns></returns>
    public static string GetUsersOrderedMods()
    {
        List<Mod> mods = GetUsersMods();

        if (mods.Count == 0)
        {
            return "";
        }

        var sortedMods = mods.OrderBy(x => x.info.name);

        string loadedMods = "";
        foreach (Mod m in sortedMods)
        {
            loadedMods += m.info.name.ToLower() + ",";
        }

        return loadedMods.Remove(loadedMods.Length - 1);

    }

    /// <summary>
    /// Please don't use this, this is for the mod loader only.
    /// </summary>
    public void WaitForScenarioReload()
    {
        StartCoroutine(Wait());
    }

    private IEnumerator Wait()
    {
        while (!VTMapManager.fetch.scenarioReady)
        {
            yield return null;
        }
        if (MissionReloaded != null)
            MissionReloaded.Invoke();
    }
}