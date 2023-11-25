using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class VTOLMOD : MonoBehaviour
{
    // private Mod mod = null;
    public string containingFolder;
    
    public string ModFolder
    {
        get
        {
            return containingFolder;
        }
    }

    public virtual void ModLoaded()
    {
        Log("Loaded!");
    }

    public void Log(object message)
    {
        Debug.Log(gameObject.name + ": " + message);
    }

    public void LogWarning(object message)
    {
        Debug.LogWarning(gameObject.name + ": " + message);
    }

    public void LogError(object message)
    {
        Debug.LogError(gameObject.name + ": " + message);
    }
}
