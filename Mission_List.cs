using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Pigeon.UI;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Rect = Pigeon.UI.Rect;

namespace MyMycoModRoot;

[HarmonyPatch(
    typeof(MissionSelectWindow),
    "OnOpen"
)]
internal static class MissionList
{
    private static readonly string[] BiomeOrder =
    {
        "GravityFarm",
        "City",
        "Desert",
        "IceFloes",
        "Tundra",
        "Sewer",
        "Moon"
    };

    private static readonly string[] MissionTypeOrder =
    {
        "abomrush",

        "planet_def",
        "rampage",
        "sat_salvage",
        "cleanup_detail",
        "sax_transport",
        "prized_pos",
        "pop_ctrl",

        "ge_amalgamation",
        "freeroam",

        "incursion",
        "ouroboros",
        "ouroblitz",
        "oxybreach_e",
    };

    private static ScrollArea _customScrollArea;

    private static void Postfix(MissionSelectWindow __instance)
    {
        var replayButtonField = AccessTools.Field(typeof(MissionSelectWindow), "replayButton");
        var replayButton = (GameObject)replayButtonField.GetValue(__instance);

        Plugin.Logger.LogInfo("replayButtonField : " + replayButtonField);
        Plugin.Logger.LogInfo("replayButtonField.Name : " + replayButtonField.Name);
        Plugin.Logger.LogInfo("replayButton : " + replayButton);
        Plugin.Logger.LogInfo("replayButton.name : " + replayButton.name);

        if (replayButton == null)
        {
            Plugin.Logger.LogError("Could not find replayButton.");
            return;
        }

        // Clone the Replay button so the new button matches the game's styling.
        GameObject missionListButton = Object.Instantiate(replayButton, replayButton.transform.parent);
        missionListButton.name = "MyCustomButton";
        missionListButton.GetComponentInChildren<TextMeshProUGUI>(true).text = "Mission List";
        missionListButton.transform.SetSiblingIndex(replayButton.transform.GetSiblingIndex() + 2);
        missionListButton.SetActive(true);
        missionListButton.transform.localPosition = Vector3.zero;

        var defaultButton = missionListButton.GetComponent<DefaultButton>();
        if (defaultButton != null)
        {
            ClearPersistentListeners(defaultButton.OnClickUp);
            // not needed ? :
            // defaultButton.OnClickUp.RemoveAllListeners();
            defaultButton.OnClickUp.AddListener(() => OnCustomButtonClicked(__instance));
        }

        if (_customWindow == null) CreateCustomWindow(__instance);
    }

    // Toggles the cloned mission-list window when its button is pressed.
    private static void OnCustomButtonClicked(MissionSelectWindow instance)
    {
        if (_customWindow == null) return;
        instance.legacyWindow.SetActive(false);
        instance.replayWindow.SetActive(false);

        _customWindow.SetActive(!_customWindow.activeSelf);
    }

    internal static void CloseCustomWindow()
    {
        if (_customWindow != null) _customWindow.SetActive(false);
    }


    private static GameObject _customWindow;

    private static void CreateCustomWindow(MissionSelectWindow missionWindow)
    {
        // Find the original window through reflection so the clone preserves
        // the game's layout, components, and visuals.
        var legacyWindowField = AccessTools.Field(typeof(MissionSelectWindow), "legacyWindow");
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

        _customWindow = Object.Instantiate(
            legacyWindow,
            legacyWindow.transform.parent
        );
        _customWindow.name = "MyCustomWindow";
        _customWindow.GetComponentInChildren<TextMeshProUGUI>(true).text = "Mission List";
        _customWindow.SetActive(Configs.IsOpenByDefault.Value);

        // Locate the mission-button container inside the cloned window.
        // The hierarchy path is calculated from the original window and
        // reused on the clone.
        var originalContainerField = AccessTools.Field(typeof(MissionSelectWindow), "legacyMissionContainer");
        var originalContainer = (RectTransform)originalContainerField.GetValue(missionWindow);
        var customContainer = FindClone(originalContainer, legacyWindow, _customWindow);
        customContainer.name = "CustomBKG";
        customContainer.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();


        var originalScrollArea = missionWindow.GetComponentInChildren<ScrollArea>(true);
        if (originalScrollArea == null) Plugin.Logger.LogError("No originalScrollArea.");


        _customScrollArea = Object.Instantiate(originalScrollArea, _customWindow.transform);
        _customScrollArea.SetName("_customScrollArea");
        var newRectTransform = customContainer.GetComponent<RectTransform>();
        _customScrollArea.GetComponent<RectTransform>().anchoredPosition = newRectTransform.anchoredPosition;
        _customScrollArea.GetComponent<RectTransform>().anchorMax = newRectTransform.anchorMax;
        _customScrollArea.GetComponent<RectTransform>().anchorMin = newRectTransform.anchorMin;
        _customScrollArea.GetComponent<RectTransform>().offsetMax = newRectTransform.offsetMax;
        _customScrollArea.GetComponent<RectTransform>().offsetMin = newRectTransform.offsetMin;
        _customScrollArea.GetComponent<RectTransform>().pivot = newRectTransform.pivot * 2;
        _customScrollArea.GetComponent<RectTransform>().sizeDelta = newRectTransform.sizeDelta;


        // changes the "margin" of the mission buttons in the window
        // 76.0f is too much. couldn't care enough to test decimals
        customContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>().spacing = new Vector2(75.0f, 75.0f);


        var scrollContent = _customScrollArea.transform.Find("ScrollContent") as RectTransform;

        if (scrollContent == null)
        {
            Plugin.Logger.LogError("Could not find ScrollContent.");
            return;
        }

        var sourceLayout = customContainer.GetComponent<GridLayoutGroup>();
        var targetLayout = scrollContent.GetComponent<GridLayoutGroup>();

        if (targetLayout == null) targetLayout = scrollContent.gameObject.AddComponent<GridLayoutGroup>();

        targetLayout.padding = new RectOffset(
            sourceLayout.padding.left,
            sourceLayout.padding.right,
            sourceLayout.padding.top,
            sourceLayout.padding.bottom
        );
        targetLayout.cellSize = sourceLayout.cellSize;
        targetLayout.spacing = sourceLayout.spacing;
        targetLayout.startCorner = sourceLayout.startCorner;
        targetLayout.startAxis = sourceLayout.startAxis;
        targetLayout.childAlignment = sourceLayout.childAlignment;
        targetLayout.constraint = sourceLayout.constraint;
        targetLayout.constraintCount = sourceLayout.constraintCount;

        // _customScrollArea.ItemContainer = scrollContent;


        // LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        PopulateCustomWindow(missionWindow, scrollContent);
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

        Plugin.Logger.LogInfo("path : " + path);
        return path;
    }


    private static void PopulateCustomWindow(
        MissionSelectWindow missionWindow,
        RectTransform container)
    {
        var prefabField = AccessTools.Field(typeof(MissionSelectWindow), "missionButtonPrefab");
        var prefab = (MissionSelectButton)prefabField.GetValue(missionWindow);

        var missions = new List<MissionData>();
        foreach (MissionSelectWindow.PlanetData planet in missionWindow.planets)
        {
            for (int missionIndex = 0;
                 missionIndex < planet.activeMissions.TriangleCount;
                 missionIndex++)
            {
                MissionSelectWindow.ActiveMission activeMission = planet.activeMissions.GetData(missionIndex);
                if (activeMission.Button == null ||
                    !activeMission.Button.gameObject.activeSelf)
                    continue;
                
                if (!Configs.Freeroam.Value && activeMission.Button.mission.Mission._missionName == "freeroam") continue;
                if (!Configs.Incursion.Value && activeMission.Button.mission.Mission._missionName == "incursion") continue;
                if (!Configs.Ouroboros.Value && activeMission.Button.mission.Mission._missionName == "ouroboros") continue;
                if (!Configs.Ouroblitz.Value && activeMission.Button.mission.Mission._missionName == "ouroblitz") continue;
                if (!Configs.Oxythane.Value && activeMission.Button.mission.Mission._missionName == "oxybreach_e") continue;
                
                // Plugin.Logger.LogInfo("region.GetName() : "+activeMission.Button.Mission.Region.GetName()); // Moon (this)
                Plugin.Logger.LogInfo("Mission._missionName : " + activeMission.Button.mission.Mission._missionName); //sax_transport (this)
                
                missions.Add(activeMission.Button.Mission);
            }
        }

        missions = Configs.SortBy.Value switch
        {
            Configs.SortMethod.Biome => missions
                .OrderBy(GetBiomeOrder)
                .ThenByDescending(mission => mission.GetModifierCount())
                .ThenBy(GetMissionTypeOrder)
                .ToList(),
            Configs.SortMethod.ModifierCount => missions
                .OrderByDescending(mission => mission.GetModifierCount())
                .ThenBy(GetMissionTypeOrder)
                .ThenBy(GetBiomeOrder)
                .ToList(),
            Configs.SortMethod.MissionType => missions
                .OrderBy(GetMissionTypeOrder)
                .ThenByDescending(mission => mission.GetModifierCount())
                .ThenBy(GetBiomeOrder)
                .ToList(),
            _ => missions
        };

        int buttonIndex = 0;
        foreach (MissionData missionData in missions)
        {
            MissionSelectButton button;
            if (buttonIndex < container.childCount)
            {
                // Reuses a button that already exists
                button = container.GetChild(buttonIndex).GetComponent<MissionSelectButton>();
            }
            else
            {
                // creates new objects
                button = Object.Instantiate(prefab, container);
            }

            button.gameObject.SetActive(true);
            button.Setup(missionData, missionWindow, true);

            if (Configs.BiomeIconsColor.Value)
                button.transform.Find("Outline").GetComponent<UnityEngine.UI.Image>().color = missionData.Region.Color;

            buttonIndex++;
        }

        // Hide unused buttons left over from the cloned template.
        for (int i = buttonIndex; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(false);
    }

    private static int GetBiomeOrder(MissionData mission)
    {
        string biomeName = mission.Region.GetName();
        for (int index = 0; index < BiomeOrder.Length; index++)
        {
            if (string.Equals(BiomeOrder[index], biomeName, System.StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return BiomeOrder.Length;
    }

    private static int GetMissionTypeOrder(MissionData mission)
    {
        string typeName = mission.Mission._missionName;
        // Plugin.Logger.LogInfo("typeName : " + typeName);
        for (int index = 0; index < MissionTypeOrder.Length; index++)
        {
            if (string.Equals(
                    MissionTypeOrder[index],
                    typeName,
                    System.StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return BiomeOrder.Length;
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

        // AccessTools.Method(
        //     typeof(UnityEventBase),
        //     "DirtyPersistentCalls"
        // ).Invoke(unityEvent, null);
    }


    [HarmonyPatch(
        typeof(MissionSelectWindow),
        "ToggleLegacyWindow"
    )]
    [HarmonyPostfix]
    private static void ToggleLegacyWindowPostfix()
    {
        CloseCustomWindow();
    }

    [HarmonyPatch(
        typeof(MissionSelectWindow),
        "ToggleReplayWindow"
    )]
    [HarmonyPostfix]
    private static void ToggleReplayWindowPostfix()
    {
        CloseCustomWindow();
    }
}