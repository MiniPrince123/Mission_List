using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Pigeon.Movement;
using Pigeon.UI;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
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
        "groundzero",
        "abomrush",
        "ge_amalgamation",

        "planet_def",
        "rampage",
        "sat_salvage",
        "cleanup_detail",
        "sax_transport",
        "prized_pos",
        "pop_ctrl",

        "freeroam",
        "incursion",
        "ouroboros",
        "ouroblitz",
        "oxybreach_e",
    };

    private static ScrollArea _customScrollArea;
    private static GameObject _customButton;
    private static GameObject _customWindow;
    private static readonly HashSet<MissionSelectButton> OtButtons = new();
    

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
        if (_customButton == null)
        {
            _customButton = Object.Instantiate(replayButton, replayButton.transform.parent);
            _customButton.name = "MyCustomButton";
            _customButton.GetComponentInChildren<TextMeshProUGUI>(true).text = "Mission List";
            _customButton.transform.SetSiblingIndex(replayButton.transform.GetSiblingIndex() + 2);
            _customButton.transform.localPosition = Vector3.zero;

            var defaultButton = _customButton.GetComponent<DefaultButton>();
            if (defaultButton != null)
            {
                ClearPersistentListeners(defaultButton.OnClickUp);
                defaultButton.OnClickUp.AddListener(() => OnCustomButtonClicked(__instance));
            }


            CreateCustomWindow(__instance);
        }
    }

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
        _customWindow.SetActive(false);

        WireCloseButton(_customWindow);

        // Locate the mission-button container inside the cloned window.
        // The hierarchy path is calculated from the original window and
        // reused on the clone.
        var originalContainerField = AccessTools.Field(typeof(MissionSelectWindow), "legacyMissionContainer");
        var originalContainer = (RectTransform)originalContainerField.GetValue(missionWindow);
        var customContainer = FindClone(originalContainer, legacyWindow, _customWindow);
        customContainer.name = "CustomBKG";
        // customContainer.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();


        ScrollArea originalScrollArea = missionWindow.GetComponentInChildren<ScrollArea>(true);
        if (originalScrollArea == null)
        {
            Plugin.Logger.LogError("No originalScrollArea.");
            return;
        }

        ScrollBar originalScrollBar = originalScrollArea.ScrollBar;
        Plugin.Logger.LogInfo("SCROLLAREA PARENT : " + originalScrollArea.transform.parent.GetName());
        Plugin.Logger.LogInfo(
            $"Original ScrollArea '{originalScrollArea.name}': ScrollBar is null = " +
            $"{originalScrollBar == null}; value = {originalScrollBar}; " +
            $"type = {originalScrollBar?.GetType().FullName ?? "null"}");

        _customScrollArea = Object.Instantiate(originalScrollArea, _customWindow.transform);
        _customScrollArea.SetName("_customScrollArea");
        _customScrollArea.Scroll(new Vector2(0, -1000));
        

        var originalScrollR = originalScrollArea.transform.parent.Find("Scroll R");
        if (originalScrollR == null)
        {
            Plugin.Logger.LogError(
                $"Could not find 'Scroll R' beside " +
                $"'{GetPath(originalScrollArea.transform, missionWindow.transform)}' " +
                "in the original mission info window.");
        }
        else
        {
            var customScrollR = Object.Instantiate(
                originalScrollR.gameObject,
                _customWindow.transform,
                false);
            customScrollR.name = "Scroll R";
            var customScrollBar = customScrollR
                .transform.Find("ScrollRect/ScrollBar")
                ?.GetComponent<ScrollBar>();
            if (customScrollBar == null)
            {
                Plugin.Logger.LogError(
                    "Cloned 'Scroll R' but could not find its " +
                    "'ScrollRect/ScrollBar' component.");
            }
            else
            {
                var scrollBarField = AccessTools.Field(typeof(ScrollArea), "scrollBar");
                if (scrollBarField == null)
                {
                    Plugin.Logger.LogError("Could not find ScrollArea.scrollBar.");
                    return;
                }

                scrollBarField.SetValue(_customScrollArea, customScrollBar);
                Plugin.Logger.LogInfo(
                    $"Cloned 'Scroll R/ScrollRect/ScrollBar' under " +
                    $"'{_customWindow.name}' and assigned it to the ScrollArea.");
            }
        }

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

        _customScrollArea.ItemContainer = scrollContent;

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

        // LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        PopulateCustomWindow(missionWindow, scrollContent);

        if (Configs.IsOpenByDefault.Value)
            OpenCustomWindow();
    }


    // Toggles the cloned mission-list window when its button is pressed.
    private static void OnCustomButtonClicked(MissionSelectWindow instance)
    {
        if (_customWindow == null) return;
        instance.legacyWindow.SetActive(false);
        instance.replayWindow.SetActive(false);

        if (_customWindow.activeSelf)
            CloseCustomWindow();
        else
        {
            PopulateCustomWindow(
                instance,
                _customScrollArea.transform.Find("ScrollContent") as RectTransform
            );
            OpenCustomWindow();
        }
    }

    internal static void CloseCustomWindow()
    {
        if (_customWindow != null) _customWindow.SetActive(false);
    }

    private static void OpenCustomWindow()
    {
        if (_customWindow == null) return;
        _customWindow.SetActive(true);
        PlayerLook.Instance.DotTransition.Enable(
            (RectTransform)_customWindow.transform,
            Global.UIColor
        );
    }


    private static void WireCloseButton(GameObject window)
    {
        var defaultCloseButton = window.GetComponentInChildren<DefaultButton>();
        ClearPersistentListeners(defaultCloseButton.OnClickUp);
        defaultCloseButton.OnClickUp.AddListener(CloseCustomWindow);

        // foreach (var defaultButton in window.GetComponentsInChildren<DefaultButton>(true))
        // {
        //     Plugin.Logger.LogInfo("foreach1 : "+defaultButton);
        //     if (!IsCloseButton(defaultButton.gameObject))
        //         continue;
        //
        //     ClearPersistentListeners(defaultButton.OnClickUp);
        //     defaultButton.OnClickUp.AddListener(CloseCustomWindow);
        //     return;
        // }

        // foreach (var button in window.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        // {
        //     Plugin.Logger.LogInfo("foreach2 : "+button);
        //     if (!IsCloseButton(button.gameObject))
        //         continue;
        //
        //     button.onClick.RemoveAllListeners();
        //     button.onClick.AddListener(CloseCustomWindow);
        //     return;
        // }
        //
        // Plugin.Logger.LogWarning("Could not find the close button in the custom mission window.");
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
        // todo add replay and legacy missions
        foreach (var planet in missionWindow.planets)
        {
            foreach (var activeMission in planet.activeMissions.Data)
            {
                if (activeMission.Button == null ||
                    !activeMission.Button.gameObject.activeSelf)
                    continue;

                var missionData = activeMission.Button.Mission;
                // Plugin.Logger.LogInfo("region.GetName() : " + missionData.Region.GetName()); // Moon (this)
                Plugin.Logger.LogInfo("Mission._missionName : " +
                                      activeMission.Button.mission.Mission._missionName); //sax_transport (this)
                Plugin.Logger.LogInfo("Mission.completedweekly : " +
                                      activeMission.Button.mission.Mission.HasCompletedThisWeek()); //sax_transport (this)
                Plugin.Logger.LogInfo(
                    activeMission.Button.mission.Mission ==
                    Mission.GetMissionData(Global.Instance.OvertimeAssignment).Mission); //sax_transport (this)

                Plugin.Logger.LogInfo(
                    $"mission data container: {missionData.Container?.GetType().Name ?? "null"}; " +
                    $"mission source: {activeMission.Source?.GetType().Name ?? "null"}; " +
                    $"is overtime assignment: " +
                    $"{activeMission.Source is MissionSelectButton sourceButton && OtButtons.Contains(sourceButton)}");

                var data = activeMission.Button.Mission;
                var otData = Mission.GetMissionData(Global.Instance.OvertimeAssignment);
                Plugin.Logger.LogInfo(
                    $"same data: {ReferenceEquals(data, otData)}, " +
                    $"same mission: {ReferenceEquals(data.Mission, otData.Mission)}");

                if (!FilterMission(missionData, activeMission.Source)) continue;

                missions.Add(missionData);
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
            button.GetComponent<Image>().maskable = true;
            button.Setup(missionData, missionWindow, true);
            foreach (var image in button.GetComponentsInChildren<Image>(true))
                image.maskable = true;

            if (Configs.BiomeIconsColor.Value)
                button.transform.Find("Outline").GetComponent<Image>().color = missionData.Region.Color;

            buttonIndex++;
        }

        // Hide unused buttons left over from the cloned template.
        for (int i = buttonIndex; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(false);
    }

    private static bool FilterMission(
        MissionData missionData,
        object source)
    {
        var mission = missionData.Mission;

        if (!Configs.Freeroam.Value && mission._missionName == "freeroam") return false;
        if (!Configs.Incursion.Value && mission._missionName == "incursion") return false;
        if (!Configs.Ouroboros.Value && mission._missionName == "ouroboros") return false;
        if (!Configs.Ouroblitz.Value && mission._missionName == "ouroblitz") return false;
        if (!Configs.Oxythane.Value && mission._missionName == "oxybreach_e") return false;
        // if (Configs.WeekliesVisibility.Value == Configs.WeekliesEnum.HideAll &&
        //     mission.HasCompletedThisWeek()) return false;
        // if (Configs.WeekliesVisibility.Value == Configs.WeekliesEnum.HideAll &&
        //     mission.ResetWeekly &&
        //     mission.HasCompletedThisWeek())
        //     return false;
        if (Configs.WeekliesVisibility.Value == Configs.WeekliesEnum.HideAll &&
            source is MissionSelectButton sourceButton &&
            OtButtons.Contains(sourceButton))
            return false;


        return true;
    }

    private static int GetBiomeOrder(MissionData mission)
    {
        for (int index = 0; index < BiomeOrder.Length; index++)
        {
            if (string.Equals(
                    BiomeOrder[index],
                    mission.Region.GetName(),
                    System.StringComparison.OrdinalIgnoreCase)
               ) return index;
        }
        return BiomeOrder.Length;
    }

    private static int GetMissionTypeOrder(MissionData mission)
    {
        // Plugin.Logger.LogInfo("typeName : " + typeName);
        for (int index = 0; index < MissionTypeOrder.Length; index++)
        {
            if (string.Equals(
                    MissionTypeOrder[index],
                    mission.Mission._missionName,
                    System.StringComparison.OrdinalIgnoreCase)
                ) return index;
        }
        return MissionTypeOrder.Length;
    }


    // UnityEvents copied from the original button can retain serialized
    // callbacks. Clear those callbacks before attaching the custom handler.
    // no clue
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


    [HarmonyPatch(typeof(MissionSelectWindow), "ToggleLegacyWindow")]
    [HarmonyPrefix]
    private static void ToggleLegacyWindowPrefix()
    {
        CloseCustomWindow();
    }

    [HarmonyPatch(typeof(MissionSelectWindow), "ToggleReplayWindow")]
    [HarmonyPostfix]
    private static void ToggleReplayWindowPostfix()
    {
        CloseCustomWindow();
    }

    [HarmonyPatch(typeof(MissionSelectWindow), "OnEscape")]
    [HarmonyTranspiler]
    // no fuckin clue
    private static IEnumerable<CodeInstruction> OnEscapeTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var codes = instructions.ToList();
        var replayWindowField = AccessTools.Field(
            typeof(MissionSelectWindow),
            "replayWindow"
        );
        if (replayWindowField == null)
        {
            Plugin.Logger.LogError("Could not find MissionSelectWindow.replayWindow for OnEscape transpiler.");
            return codes;
        }

        for (int index = 1; index < codes.Count; index++)
        {
            if (codes[index].opcode != OpCodes.Ldfld ||
                !Equals(codes[index].operand, replayWindowField) ||
                codes[index - 1].opcode != OpCodes.Ldarg_0)
                continue;

            var continueLabel = generator.DefineLabel();
            var replayWindowLoad = codes[index - 1];
            var originalLabels = replayWindowLoad.labels.ToArray();
            replayWindowLoad.labels.Clear();

            var injected = new List<CodeInstruction>
            {
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(MissionList),
                        nameof(HandleEscapeKey)
                    )
                ),
                new CodeInstruction(OpCodes.Brfalse, continueLabel),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret)
            };
            injected[0].labels.AddRange(originalLabels);
            replayWindowLoad.labels.Add(continueLabel);
            codes.InsertRange(index - 1, injected);
            return codes;
        }

        Plugin.Logger.LogError("Could not find the replayWindow check in MissionSelectWindow.OnEscape.");
        return codes;
    }

    private static bool HandleEscapeKey()
    {
        if (_customWindow == null || !_customWindow.activeSelf)
            return false;

        CloseCustomWindow();
        return true;
    }
    
    internal static void ClearOTButtons()
    {
        OtButtons.Clear();
    }
    
    
    internal static void TrackOtButton(
        MissionContainer missionContainer,
        MissionSelectButton button)
    {
        if (missionContainer == Global.Instance.OvertimeAssignment)
            OtButtons.Add(button);
        else
            OtButtons.Remove(button);
    }
}

[HarmonyPatch]
internal static class OtTrackingPatches
{
    [HarmonyPatch(typeof(MissionSelectWindow), "OnOpen")]
    [HarmonyPrefix]
    private static void OnOpenPrefix()
    {
        MissionList.ClearOTButtons();
    }

    [HarmonyPatch(typeof(MissionSelectWindow), "SetupMission")]
    [HarmonyPostfix]
    private static void SetupMissionPostfix(
        MissionContainer __0,
        MissionSelectButton __2)
    {
        MissionList.TrackOtButton(__0, __2);
    }
}