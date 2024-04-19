using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace VTOLAPICommons
{
    public class InGameUIManager
    {
        private bool hasUiBeenCreated = false;
        private ModLoaderObj modLoaderObj;

        private CampaignInfoUI modInfoUI;
        private RectTransform modInfoSelectionTf;
        private RectTransform settingsSelectionTf;
        private GameObject settingsCampaignListTemplate;
        private ScrollRect settingsScrollView;
        private ScrollRect settingsScrollBoxView;
        private GameObject s_Holder;
        private VRPointInteractableCanvas interactableCanvasScript;
        private GameObject campaignListTemplate;

        private VRKeyboard intKeyboard;
        private VRKeyboard floatKeyboard;
        private VRKeyboard stringKeyboard;

        private Text selectButton;
        private ScrollRect scrollView;

        private GameObject modsPage;
        private GameObject mainScreen;
        private GameObject settingsPage;

        private Dictionary<string, GameObject> settingsTemplates = new Dictionary<string, GameObject>();
        private Dictionary<string, KeyboardType> keyboardSettingTypes = new Dictionary<string, KeyboardType>();

        private int currentModUiIndex = 0;
        private float buttonHeight = 100;
        private int currentSelectedSetting = -1;
        private int selectedMod = -1;

        private List<Setting> modSettings = null;

        public enum Pages { MainMenu, Mods, Settings }
        public enum KeyboardType { DisableAll, Int, Float, String }


        public InGameUIManager(ModLoaderObj mlObj)
        {
            modLoaderObj = mlObj;
        }

        public void CreateUI()
        {
            if (!modLoaderObj.assetBundle) Logger.Log("Asset bundle is null!");

            Logger.Log("Creating UI for Ready Room");
            GameObject InteractableCanvas = GameObject.Find("InteractableCanvas");
            if (InteractableCanvas == null) Logger.Log("InteractableCanvas was null");

            interactableCanvasScript = InteractableCanvas.GetComponent<VRPointInteractableCanvas>();
            GameObject CampaignDisplay = GameObject.Find("CampaignSelector").transform.GetChild(0).GetChild(0).gameObject;

            if (CampaignDisplay == null) Logger.Log("CampaignDisplay was null");
            CampaignDisplay.SetActive(true);

            mainScreen = GameObject.Find("MainScreen");
            if (mainScreen == null) Logger.Log("Main Screen was null");

            Logger.Log("Spawning Keyboards");
            stringKeyboard = modLoaderObj.LoadAsset("StringKeyboard").GetComponent<VRKeyboard>();
            floatKeyboard = modLoaderObj.LoadAsset("FloatKeyboard").GetComponent<VRKeyboard>();
            intKeyboard = modLoaderObj.LoadAsset("IntKeyboard").GetComponent<VRKeyboard>();
            stringKeyboard.gameObject.SetActive(false);
            floatKeyboard.gameObject.SetActive(false);
            intKeyboard.gameObject.SetActive(false);

            Logger.Log("Creating Mods Button");
            GameObject SettingsButton = mainScreen.transform.GetChild(0).GetChild(0).GetChild(8).gameObject;
            GameObject modsButton = modLoaderObj.LoadAsset("ModsButton", SettingsButton.transform.parent);
            modsButton.transform.localPosition = new Vector3(-811, -412, 0);
            VRInteractable modsInteractable = modsButton.GetComponent<VRInteractable>();
            modsInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Mods); SetDefaultText(); });

            Logger.Log("Creating Mods Page");
            modsPage = modLoaderObj.LoadAsset("ModLoaderDisplay", CampaignDisplay.transform.parent);

            campaignListTemplate = modsPage.transform.GetChild(3).GetChild(0).GetChild(0).GetChild(1).gameObject;
            scrollView = modsPage.transform.GetChild(3).GetComponent<ScrollRect>();
            buttonHeight = ((RectTransform)campaignListTemplate.transform).rect.height;
            modInfoSelectionTf = (RectTransform)modsPage.transform.GetChild(3).GetChild(0).GetChild(0).GetChild(0).transform;
            modInfoUI = modsPage.transform.GetChild(5).GetComponentInChildren<CampaignInfoUI>();
            selectButton = modsPage.transform.GetChild(1).GetComponentInChildren<Text>();

            VRInteractable selectVRI = modsPage.transform.GetChild(1).GetComponent<VRInteractable>();
            if (selectVRI == null) Logger.Log("selectVRI is null");
            selectVRI.OnInteract.AddListener(LoadMod);
            VRInteractable backInteractable = modsPage.transform.GetChild(2).GetComponent<VRInteractable>();
            if (backInteractable == null) Logger.Log("backInteractable is null");
            backInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.MainMenu); });
            VRInteractable settingsInteractable = modsPage.transform.GetChild(4).GetComponent<VRInteractable>();
            settingsInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Settings); });

            currentModUiIndex = 0;
            foreach (var mod in Loader.instance.mods)
            {
                SetupNewModObjectUi(mod, true);
            }


            Logger.Log("Mod Settings");
            settingsPage = modLoaderObj.LoadAsset("ModSettings", CampaignDisplay.transform.parent);
            settingsSelectionTf = (RectTransform)settingsPage.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).transform;


            var s_BoolTemplate = modLoaderObj.LoadAsset("BoolTemplate");
            var s_StringTemplate = modLoaderObj.LoadAsset("StringTemplate");
            var s_IntTemplate = modLoaderObj.LoadAsset("NumberTemplate");
            var s_CustomLabel = modLoaderObj.LoadAsset("CustomLabel");
            var s_FloatTemplate = s_IntTemplate;
            s_Holder = modsPage.transform.GetChild(5).gameObject;

            settingsTemplates.Clear();
            keyboardSettingTypes.Clear();

            settingsTemplates.Add("StringSetting", s_StringTemplate);
            keyboardSettingTypes.Add("StringSetting", KeyboardType.String);

            settingsTemplates.Add("IntTemplate", s_StringTemplate);
            keyboardSettingTypes.Add("IntTemplate", KeyboardType.Int);

            settingsTemplates.Add("FloatSetting", s_FloatTemplate);
            keyboardSettingTypes.Add("FloatSetting", KeyboardType.Float);

            settingsTemplates.Add("CustomLabel", s_CustomLabel);
            settingsTemplates.Add("BoolSetting", s_BoolTemplate);


            Logger.Log("Setting up settings buttons");
            settingsCampaignListTemplate = settingsPage.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject;
            settingsCampaignListTemplate.SetActive(false);

            VRInteractable settingsBackInteractable = settingsPage.transform.GetChild(2).GetComponent<VRInteractable>();
            settingsBackInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Mods); });
            var settingsScrollBox = settingsPage.transform.GetChild(4).gameObject;
            settingsScrollBoxView = settingsScrollBox.GetComponent<ScrollRect>();
            settingsScrollView = settingsPage.transform.GetChild(1).GetComponent<ScrollRect>();

            Logger.Log("Finished clearing up");
            OpenPage(Pages.MainMenu);
            scrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + Loader.instance.mods.Count) * buttonHeight);
            scrollView.ClampVertical();
            interactableCanvasScript.RefreshInteractables();
            CampaignDisplay.SetActive(false);
            campaignListTemplate.SetActive(false);
            SetDefaultText();
            RecreateSettings();

            hasUiBeenCreated = true;
        }

        public void SetupNewModObjectUi(Mod mod, bool initialSetup = false)
        {
            if (!hasUiBeenCreated && !initialSetup) return; // If no UI and initial setup not done cannot setup new mod object UI
            if (mod.listObj != null)
            {
                Logger.Log($"Mod list object already exists for {mod}");
                return;
            }

            mod.listObj = GameObject.Instantiate(campaignListTemplate, scrollView.content);
            mod.listObj.transform.localPosition = new Vector3(0f, -currentModUiIndex * buttonHeight, 0f);
            mod.listObj.GetComponent<VRUIListItemTemplate>().Setup(mod.info.name, mod.id, OpenMod);
            mod.listObj.SetActive(true); //  Something silly preventing late-added mod items from being interactable
            Logger.Log($"Setup mod select UI for {mod}. Mod index: {currentModUiIndex}");
            currentModUiIndex++;
        }

        private void OpenPage(Pages page)
        {
            Logger.Log("Opening Page " + page.ToString());
            modsPage.SetActive(false);
            mainScreen.SetActive(false);
            settingsPage.SetActive(false);

            switch (page)
            {
                case Pages.MainMenu:
                    mainScreen.SetActive(true);
                    break;
                case Pages.Mods:
                    modsPage.SetActive(true);
                    break;
                case Pages.Settings:
                    settingsPage.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        private void OpenMod(int id)
        {
            var mod = Loader.GetMod(id);
            Logger.Log($"Opening mod by id {id}: {mod}");

            selectButton.text = mod.isLoaded ? "Loaded!" : "Load";
            scrollView.ViewContent((RectTransform)mod.listObj.transform);
            modInfoSelectionTf.position = mod.listObj.transform.position;
            modInfoSelectionTf.GetComponent<UnityEngine.UI.Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);
            modInfoUI.campaignName.text = mod.info.name;
            modInfoUI.campaignDescription.text = mod.info.description;

            selectedMod = id;
            // if (!string.IsNullOrWhiteSpace(mod.ImagePath))
            // {
            //     modInfoUI.campaignImage.color = Color.white;
            //     StartCoroutine(SetModPreviewImage(modInfoUI.campaignImage, _selectedMod.ImagePath));
            // }
            // else
            // {
            modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            // }
        }

        private void LoadMod()
        {
            var mod = Loader.GetMod(selectedMod);
            if (mod == null)
            {
                Logger.Log($"Unable to load mod as selected mod is: {selectedMod}");
                return;
            }
            if (mod.isLoaded) return; // Don't reload mod

            try
            {
                var loadResult = mod.Load();
                if (loadResult) selectButton.text = "Loaded!";
                else selectButton.text = "Error";
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to load mod: {e}");
                selectButton.text = "Error";

                if (e is ReflectionTypeLoadException loadException)
                {
                    Logger.Log($"Error was loader exception: {loadException.Message}");
                    foreach (var err in loadException.LoaderExceptions)
                    {
                        Logger.Log($"Loader exception: {err}");
                    }

                    foreach (var type in loadException.Types)
                    {
                        Logger.Log($"Type load: {type.Name}");
                    }
                }
            }
        }

        private void SetDefaultText()
        {
            Logger.Log("Setting up default text");
            modInfoUI.campaignName.text = "";
            modInfoUI.campaignDescription.text = "";
            modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            modInfoSelectionTf.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
            settingsSelectionTf.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
        }

        private void RecreateSettings()
        {
            if (modSettings == null) return;
            foreach (var setting in modSettings)
            {
                CreateSettingsMenu(setting, true);
            }
        }

        public void CreateSettingsMenu(Setting settings) => CreateSettingsMenu(settings, false);
        private void CreateSettingsMenu(Setting settings, bool recreating)
        {
            var mod = settings.Mod;
            mod.settingsObj = UnityEngine.Object.Instantiate(settingsCampaignListTemplate, settingsScrollView.content);
            mod.settingsObj.SetActive(true);
            mod.settingsObj.transform.localPosition = new Vector3(0f, -(settingsScrollView.content.childCount - 5) * buttonHeight, 0f);
            mod.settingsObj.GetComponent<VRUIListItemTemplate>().Setup(mod.info.name, mod.id, OpenSetting);

            mod.settingsHolderObj = new GameObject(mod.info.name, typeof(RectTransform));
            mod.settingsHolderObj.transform.SetParent(s_Holder.transform, false);

            foreach (var subSetting in settings.subSettings) RegisterSubVariable(mod, subSetting);

            mod.settingsHolderObj.SetActive(false);
            if (!recreating)
            {
                if (modSettings == null) modSettings = new List<Setting>();
                modSettings.Add(settings);
            }

            RefreshSettings();
        }

        private void RegisterSubVariable(Mod mod, Setting.SubSetting subSetting)
        {
            var settingsHolder = mod.settingsHolderObj;
            var type = subSetting.GetType().Name;
            var settingsGo = UnityEngine.Object.Instantiate(settingsTemplates[type], settingsHolder.transform, false);
            settingsGo.transform.GetChild(1).GetComponent<Text>().text = subSetting.settingName;

            if (type != "CustomLabel")
            {
                subSetting.text = settingsGo.transform.GetChild(2).GetComponent<Text>();
                subSetting.text.text = subSetting.value.ToString();

                settingsGo.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate
                {
                    OpenKeyboard(KeyboardType.Int, subSetting.value.ToString(), 32, subSetting.SetValue);
                });
            }
            else
            {
                settingsGo.GetComponentInChildren<Text>().text = subSetting.settingName;
            }

            Logger.Log($"Spawned setting of type {type} for {mod}");
            settingsGo.SetActive(true);
        }

        private void RefreshSettings()
        {
            settingsScrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + settingsScrollView.content.childCount) * buttonHeight);
            settingsScrollView.ClampVertical();
            settingsScrollBoxView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + 1) * 100);
            settingsScrollBoxView.ClampVertical();
            interactableCanvasScript.RefreshInteractables();
        }

        public void OpenKeyboard(KeyboardType keyboardType, string startingText, int maxChars, Action<string> onEntered, UnityAction onCancelled = null)
        {
            CloseKeyboards();
            switch (keyboardType)
            {
                case KeyboardType.Int:
                    intKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                case KeyboardType.Float:
                    floatKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                case KeyboardType.String:
                    stringKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                default:
                    break;
            }
        }

        public void CloseKeyboards()
        {
            stringKeyboard.gameObject.SetActive(false);
            intKeyboard.gameObject.SetActive(false);
            floatKeyboard.gameObject.SetActive(false);
        }

        public void OpenSetting(int id)
        {
            var mod = Loader.GetMod(id);
            if (mod == null)
            {
                Logger.Log($"Unable to open settings for mod by id {id} as it was not found");
                return;
            }

            Logger.Log($"Opening settings for mod {mod}");

            settingsScrollView.ViewContent((RectTransform)mod.settingsObj.transform);
            settingsSelectionTf.position = mod.settingsObj.transform.position;
            settingsSelectionTf.GetComponent<UnityEngine.UI.Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);

            if (currentSelectedSetting != -1)
            {
                //There already is something on the content of the settings scroll box.
                MoveBackToPool(currentSelectedSetting);
            }
            MoveToSettingsView(mod.id, mod.settingsHolderObj.transform);
            RefreshSettings();
        }

        private void MoveToSettingsView(int modId, Transform parent)
        {
            currentSelectedSetting = modId;
            //They need to be stored in a temp array so that we can move them all.
            Transform[] children = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                children[i] = parent.GetChild(i);
            }
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetParent(settingsScrollBoxView.content, false);
            }
        }

        private void MoveBackToPool(int id)
        {
            var mod = Loader.GetMod(id);

            Transform holder = s_Holder.transform.Find(mod.settingsHolderObj.name);
            if (holder == null)
            {
                //Couldn't find the holder for some reason in the pool.
                holder = new GameObject(mod.info.name, typeof(RectTransform)).transform;
                holder.SetParent(s_Holder.transform, false);
                mod.settingsHolderObj = holder.gameObject;
            }

            Transform[] itemsToMove = new Transform[settingsScrollBoxView.content.childCount];
            for (int i = 0; i < settingsScrollBoxView.content.childCount; i++)
            {
                itemsToMove[i] = settingsScrollBoxView.content.GetChild(i);
            }

            for (int i = 0; i < itemsToMove.Length; i++)
            {
                itemsToMove[i].SetParent(holder, false);
            }
        }
    }
}
