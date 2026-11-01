using System.Reflection;
using UnityEngine.Events;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Pigeon.UI;

namespace MyMycoModRoot;

// Adds a "Mission List" button and window to the game's mission-selection screen.
// The game types used here are private/internal in places, so Harmony reflection is
// used to read the existing UI objects and mission data without modifying the game.
[HarmonyPatch(
    typeof(MissionSelectWindow), 
    "OnOpen"
)]
internal static class MissionSelectWindowPatch
{
    // Runs after the game's mission window has opened, when its controls and data
    // are available to clone.
    private static void Postfix(MissionSelectWindow __instance)
    {
        // Get the game's existing Replay button to use as the visual template.
        var replayField = AccessTools.Field(typeof(MissionSelectWindow), "replayButton");
        var replayButton = (GameObject)replayField.GetValue(__instance);

        if (replayButton == null)
            return;

        Transform parent = replayButton.transform.parent;
        // OnOpen can run more than once. Do not create duplicate controls.
        if (parent.Find("MyCustomButton") != null)
            return;
        
        // Clone the Replay button so the new button matches the game's styling.
        GameObject buttonObject = Object.Instantiate(replayButton, parent);
        buttonObject.name = "MyCustomButton";
        buttonObject.transform.SetSiblingIndex(replayButton.transform.GetSiblingIndex() + 1);
        buttonObject.SetActive(true);
        buttonObject.transform.localPosition = Vector3.zero;
        

        // Change the cloned button's label from "Replay" to "Mission List".
        var label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "Mission List";

        var defaultButton = buttonObject.GetComponent<DefaultButton>();
        if (defaultButton != null)
        {
            // Remove the copied game's click behavior and replace it with mine.
            ClearPersistentListeners(defaultButton.OnClickUp);
            defaultButton.OnClickUp.RemoveAllListeners();
            defaultButton.OnClickUp.AddListener(OnCustomButtonClicked);
        }

        
        // These diagnostics help compare the cloned button's layout with Replay.
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

    // Toggles the cloned mission-list window when its button is pressed.
    private static void OnCustomButtonClicked()
    {
        if (customWindow == null)
            return;

        Plugin.Logger.LogInfo("Custom button clicked");
        customWindow.SetActive(!customWindow.activeSelf);
    }
    
    private static GameObject customWindow;
    // Clones the game's legacy mission window and fills it with all active missions.
    private static void CreateCustomWindow(MissionSelectWindow missionWindow)
    {
        // Find the original window through reflection so the clone preserves
        // the game's layout, components, and visuals.
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

        // Locate the mission-button container inside the cloned window. The
        // hierarchy path is calculated from the original window and reused on
        // the clone.
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

    // Finds the equivalent child in a cloned UI hierarchy by matching its path.
    private static RectTransform FindClone(
        RectTransform original,
        GameObject originalWindow,
        GameObject clonedWindow)
    {
        string path = GetPath(original, originalWindow.transform);
        return clonedWindow.transform.Find(path) as RectTransform;
    }

    // Builds a child path relative to the supplied root transform.
    private static string GetPath(Transform child, Transform root)
    {
        Transform current = child;
        string path = current.name;
        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        Plugin.Logger.LogDebug("path : "+path);
        return path;
    }

    // Copies every currently active mission into the custom window. Existing
    // button objects are reused where possible; extra buttons are created only
    // when the container does not already have enough children.
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
        // Iterate through each planet and its active missions in the game's
        // existing order.
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
                    // Reuse a button already present in the cloned template.
                    button = container.GetChild(buttonIndex)
                        .GetComponent<MissionSelectButton>();
                }
                else
                {
                    // Expand the list if there are more missions than template
                    // buttons.
                    button = Object.Instantiate(prefab, container);
                }

                button.gameObject.SetActive(true);
                button.Setup(missionData, missionWindow, true);
                // Preserve the original mission button dimensions.
                ((RectTransform)button.transform).sizeDelta =
                    ((RectTransform)activeMission.Button.transform).sizeDelta;
                Plugin.Logger.LogInfo("sizeDelta : "+((RectTransform)activeMission.Button.transform).sizeDelta);
                buttonIndex++;
            }
        }

        // Hide unused buttons left over from the cloned template.
        for (int i = buttonIndex; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(false);
    }
    
    // UnityEvents copied from the original button can retain serialized
    // callbacks. Clear those callbacks before attaching the custom handler.
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