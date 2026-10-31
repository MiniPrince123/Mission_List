using System.Reflection;
using UnityEngine.Events;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Pigeon.UI;

namespace MyMycoModRoot;

[HarmonyPatch(
    typeof(MissionSelectWindow), 
    "OnOpen"
)]
internal static class MissionSelectWindowPatch
{
    private static void Postfix(MissionSelectWindow __instance)
    {
        var replayField = AccessTools.Field(typeof(MissionSelectWindow), "replayButton");
        var replayButton = (GameObject)replayField.GetValue(__instance);

        if (replayButton == null)
            return;

        Transform parent = replayButton.transform.parent;
        if (parent.Find("MyCustomButton") != null)
            return;
        
        GameObject buttonObject = Object.Instantiate(replayButton, parent);
        buttonObject.name = "MyCustomButton";
        buttonObject.transform.SetSiblingIndex(replayButton.transform.GetSiblingIndex() + 1);
        buttonObject.SetActive(true);
        buttonObject.transform.localPosition = Vector3.zero;
        

        var label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "Mission List";

        var defaultButton = buttonObject.GetComponent<DefaultButton>();
        if (defaultButton != null)
        {
            ClearPersistentListeners(defaultButton.OnClickUp);
            defaultButton.OnClickUp.RemoveAllListeners();
            defaultButton.OnClickUp.AddListener(OnCustomButtonClicked);
        }

        
        
        RectTransform replayRect = replayButton.GetComponent<RectTransform>();
        RectTransform customRect = buttonObject.GetComponent<RectTransform>();
        Plugin.Logger.LogInfo(
            $"Replay button position: {replayButton.transform.position}; \n" +
            $"replay dimensions: {replayRect.rect.size} \n" +
            $"custom button position: {buttonObject.transform.position}; \n" +
            $"custom dimensions: {customRect.rect.size} \n"
        );

        CreateCustomWindow(__instance);
    }

    private static void OnCustomButtonClicked()
    {
        if (customWindow == null)
            return;

        Plugin.Logger.LogInfo("Custom button clicked");
        customWindow.SetActive(!customWindow.activeSelf);
    }
    
    private static GameObject customWindow;
    private static void CreateCustomWindow(MissionSelectWindow missionWindow)
    {
        var legacyWindowField =
            AccessTools.Field(typeof(MissionSelectWindow), "legacyWindow");

        if (legacyWindowField == null)
        {
            Plugin.Logger.LogError("Could not find MissionSelectWindow.legacyWindow.");
            return;
        }

        var legacyWindow = (GameObject)legacyWindowField.GetValue(missionWindow);

        if (legacyWindow == null)
        {
            Plugin.Logger.LogError("MissionSelectWindow.legacyWindow is null.");
            return;
        }

        customWindow = Object.Instantiate(
            legacyWindow,
            legacyWindow.transform.parent
        );

        customWindow.name = "MyCustomWindow";
        customWindow.SetActive(false);

        // ---------------------
        
        
        
        var originalContainerField =
            AccessTools.Field(typeof(MissionSelectWindow), "legacyMissionContainer");
        var originalContainer =
            (RectTransform)originalContainerField.GetValue(missionWindow);
        var customContainer = FindClone(originalContainer, legacyWindow, customWindow);

        if (customContainer == null)
        {
            Plugin.Logger.LogError("Could not find the custom mission container.");
            return;
        }

        PopulateCustomWindow(missionWindow, customContainer);
    }

    private static RectTransform FindClone(
        RectTransform original,
        GameObject originalWindow,
        GameObject clonedWindow)
    {
        string path = GetPath(original, originalWindow.transform);
        return clonedWindow.transform.Find(path) as RectTransform;
    }

    private static string GetPath(Transform child, Transform root)
    {
        Transform current = child;
        string path = current.name;
        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private static void PopulateCustomWindow(
        MissionSelectWindow missionWindow,
        RectTransform container)
    {
        var prefabField =
            AccessTools.Field(typeof(MissionSelectWindow), "missionButtonPrefab");
        var prefab = (MissionSelectButton)prefabField.GetValue(missionWindow);

        int buttonIndex = 0;
        for (int regionIndex = 0;
             regionIndex < Global.Instance.Regions.Length;
             regionIndex++)
        {
            WorldRegion region = Global.Instance.Regions[regionIndex];
            if (region == null ||
                region.LegacyScenes == null ||
                region.LegacyScenes.Length == 0)
                continue;

            for (int missionIndex = 0;
                 missionIndex < Global.Instance.Missions.Length;
                 missionIndex++)
            {
                Mission mission = Global.Instance.Missions[missionIndex];
                if (!Mission.IsValidMission(mission, region.Flags))
                    continue;

                int seed = Global.MissionSelectSeed +
                           (mission.ID.GetHashCode() + 3173) * 5390921 +
                           regionIndex;
                SceneData overriddenScene;
                SceneData scene = mission.OverrideScene(
                    out overriddenScene,
                    region,
                    seed)
                    ? overriddenScene
                    : region.LegacyScenes[0];

                MissionSelectButton button;
                if (buttonIndex < container.childCount)
                {
                    button = container.GetChild(buttonIndex)
                        .GetComponent<MissionSelectButton>();
                }
                else
                {
                    button = Object.Instantiate(prefab, container);
                }

                button.gameObject.SetActive(true);
                button.Setup(
                    new MissionData(
                        seed,
                        mission,
                        region,
                        scene,
                        null,
                        mission.GetAdditionalData(),
                        false),
                    missionWindow,
                    true);
                buttonIndex++;
            }
        }

        for (int i = buttonIndex; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(false);
    }
    
    private static void ClearPersistentListeners(UnityEventBase unityEvent)
    {
        var persistentCalls = AccessTools.Field(
            typeof(UnityEventBase),
            "m_PersistentCalls"
        ).GetValue(unityEvent);

        AccessTools.Method(
            persistentCalls.GetType(),
            "Clear"
        ).Invoke(persistentCalls, null);

        AccessTools.Method(
            typeof(UnityEventBase),
            "DirtyPersistentCalls"
        ).Invoke(unityEvent, null);
    }
}


// for (int i = 0; i < Global.Instance.Regions.Length; ++i)
// {
//     WorldRegion region = Global.Instance.Regions[i];
//     Planet planet = region.Planet;
//
//     this.SetupMissions(
//         this.GetPlanetData(planet),
//         region,
//         ref missionBag,
//         ref missionBagCount,
//         ref foundSelectedMission);
// }