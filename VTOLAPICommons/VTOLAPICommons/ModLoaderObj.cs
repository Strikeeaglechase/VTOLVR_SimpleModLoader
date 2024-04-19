using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace VTOLAPICommons
{
    public class ModLoaderObj : MonoBehaviour
    {
        public static ModLoaderObj instance;
        public AssetBundle assetBundle;

        public InGameUIManager uiManager;

        private void Awake()
        {
            if (instance != null)
            {
                Logger.Log($"Two instances of ModLoaderObj, destroying {this}");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            StartCoroutine(LoadAssetBundle());
            StartCoroutine(LoadModsRoutine());
        }

        private IEnumerator LoadAssetBundle()
        {
            var path = Directory.GetCurrentDirectory() + "/SimpleModLoader/modloader.asset";
            Logger.Log($"Loading asset bundle from {path}");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            assetBundle = request.assetBundle;
            Logger.Log("Loaded asset bundle");
        }


        private IEnumerator LoadModsRoutine()
        {
            while (true)
            {
                Loader.instance.RunLoadModsRoutine();
                yield return new WaitForSeconds(1);
            }
        }


        public GameObject LoadAsset(string name)
        {
            return Instantiate(assetBundle.LoadAsset<GameObject>(name));
        }

        public GameObject LoadAsset(string name, Transform parent)
        {
            return Instantiate(assetBundle.LoadAsset<GameObject>(name), parent);
        }
    }
}
