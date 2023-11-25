using System;
using UnityEngine;
using IMLLoader;
public class Dep_VTOLMOD : MonoBehaviour
{
    private Mod mod = null;

    public string ModFolder
    {
        get
        {
            if (mod != null) return mod.containingFolder;
            else return string.Empty;
        }
    }

    public virtual void ModLoaded()
    {
        Log("Loaded!");
    }

    public void Log(object message)
    {
        if (mod == null) Debug.Log(gameObject.name + ": " + message);
        else Debug.Log(mod.info.name + ": " + message);
    }

    public void LogWarning(object message)
    {
        if (mod == null) Debug.LogWarning(gameObject.name + ": " + message);
        else Debug.LogWarning(mod.info.name + ": " + message);
    }

    public void LogError(object message)
    {
        if (mod == null) Debug.LogError(gameObject.name + ": " + message);
        else Debug.LogError(mod.info.name + ": " + message);
    }

    public void SetModInfo(Mod thisMod)
    {
        mod = thisMod;
    }
}
