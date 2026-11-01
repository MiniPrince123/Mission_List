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
        customWindow.GetComponentInChildren<TextMeshProUGUI>(true).text = "Mission List";
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
        var planetsField =
            AccessTools.Field(typeof(MissionSelectWindow), "planets");
        var planets =
            (MissionSelectWindow.PlanetData[])planetsField.GetValue(missionWindow);

        int buttonIndex = 0;
        for (int planetIndex = 0; planetIndex < planets.Length; planetIndex++)
        {
            MissionSelectWindow.PlanetData planet = planets[planetIndex];
            for (int missionIndex = 0;
                 missionIndex < planet.activeMissions.TriangleCount;
                 missionIndex++)
            {
                MissionSelectWindow.ActiveMission activeMission =
                    planet.activeMissions.GetData(missionIndex);
                if (activeMission.Button == null ||
                    !activeMission.Button.gameObject.activeSelf)
                    continue;

                MissionData missionData = activeMission.Button.Mission;

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
                button.Setup(missionData, missionWindow, true);
                ((RectTransform)button.transform).sizeDelta =
                    ((RectTransform)activeMission.Button.transform).sizeDelta;
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