using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Pigeon.Movement;
using Pigeon.UI;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

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

        "sat_salvage",
        "planet_def",
        "prized_pos",
        "rampage",
        "cleanup_detail",
        "sax_transport",
        "pop_ctrl",

        "freeroam",
        "incursion",
        "ouroboros",
        "ouroblitz",
        "oxybreach_e",

        "perfectfit",
        "containment",
        "exp_alpha",
        "oxybreach",
        "intro",
    };

    private static ScrollArea _customScrollArea;
    private static GameObject _customButton;
    private static GameObject _customWindow;
    private static GameObject _customScrollBar;
    private static readonly HashSet<MissionSelectButton> OtButtons = new();


    private static void Postfix(MissionSelectWindow __instance)
    {
        var replayButtonField = AccessTools.Field(typeof(MissionSelectWindow), "replayButton");
        var replayButton = (GameObject)replayButtonField.GetValue(__instance);

        if (replayButton == null)
        {
            Plugin.Logger.LogError("Could not find replayButton.");
            return;
        }

        if (!replayButton.activeSelf) // only for lvl 1 people
        {
            Plugin.Logger.LogError("replayButton.activeSelf == false.");
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

    // instantiates the new custom window by cloning LegacyWindow.
    // also instantiates _customScrollArea from the first(?) missioninfowindow's scrollbar.
    private static void CreateCustomWindow(MissionSelectWindow missionWindow)
    {
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

        _customWindow = Object.Instantiate(legacyWindow, legacyWindow.transform.parent);
        _customWindow.name = "_customWindow";
        _customWindow.GetComponentInChildren<TextMeshProUGUI>(true).text = "Mission List";
        _customWindow.SetActive(false);

        WireCloseButton(_customWindow);


        // - custom container ------------------------------------------------------------------------------------------
        // is basically only used for the background color i think
        // Locate the mission-button container inside the cloned window.
        // The hierarchy path is calculated from the original window and
        // reused on the clone.
        // idk
        var originalContainerField = AccessTools.Field(typeof(MissionSelectWindow), "legacyMissionContainer");
        var originalContainer = (RectTransform)originalContainerField.GetValue(missionWindow);
        var _customContainer = FindClone(originalContainer, legacyWindow, _customWindow);
        _customContainer.name = "_customContainer"; // was CustomBKG


        // - ScrollArea ------------------------------------------------------------------------------------------------
        ScrollArea originalScrollArea = missionWindow.GetComponentInChildren<ScrollArea>(true);
        if (originalScrollArea == null)
        {
            Plugin.Logger.LogError("No originalScrollArea.");
            return;
        }

        _customScrollArea = Object.Instantiate(originalScrollArea, _customWindow.transform);
        _customScrollArea.SetName("_customScrollArea");
        var oldRectTransform = _customContainer.GetComponent<RectTransform>();
        _customScrollArea.GetComponent<RectTransform>().anchoredPosition = oldRectTransform.anchoredPosition;
        _customScrollArea.GetComponent<RectTransform>().anchorMax = oldRectTransform.anchorMax;
        _customScrollArea.GetComponent<RectTransform>().anchorMin = oldRectTransform.anchorMin;
        _customScrollArea.GetComponent<RectTransform>().offsetMax = oldRectTransform.offsetMax;
        _customScrollArea.GetComponent<RectTransform>().offsetMin = oldRectTransform.offsetMin;
        _customScrollArea.GetComponent<RectTransform>().pivot = oldRectTransform.pivot * 2;
        _customScrollArea.GetComponent<RectTransform>().sizeDelta = oldRectTransform.sizeDelta;
        // The clone keeps a serialized onScroll call into the ORIGINAL
        // MissionInfoWindow.OnModifierScroll. While it exists, ScrollArea.Scroll()
        // invokes it and returns without changing the _customScrollArea, so the
        // mouse wheel does nothing in this window.
        ClearPersistentListeners(_customScrollArea.onScroll);


        // - ScrollBar -------------------------------------------------------------------------------------------------
        // TODO make originalScrollBar be an actual ScrollBar so i can make my code cleaner
        var originalScrollBar = originalScrollArea.transform.parent.Find("Scroll R");
        // this should never happen(?)
        if (originalScrollBar == null)
        {
            Plugin.Logger.LogError(
                $"Could not find 'Scroll R' beside " +
                $"'{GetPath(originalScrollArea.transform, missionWindow.transform)}' " +
                "in the original mission info window.");
            DestroyCustomWindow();
            return;
        }

        _customScrollBar = Object.Instantiate(originalScrollBar.gameObject, _customWindow.transform, false);
        _customScrollBar.name = "_customScrollBar";
        // The game hides 'Scroll R' when the info list fits; the clone copies that.
        _customScrollBar.SetActive(true);
        ScrollBar customScrollBarComp = _customScrollBar
            .transform.Find("ScrollRect/ScrollBar")
            ?.GetComponent<ScrollBar>();
        // ^ could be instead : ScrollBar customScrollBarComp = _customScrollBar.transform.GetChild(0).GetChild(0).GetComponent<ScrollBar>();
        var scrollBarField = AccessTools.Field(typeof(ScrollArea), "scrollBar");
        if (customScrollBarComp == null || scrollBarField == null)
        {
            // Do not continue: the ScrollArea would drive the original window's bar.
            Plugin.Logger.LogError(
                "Could not wire the cloned 'Scroll R/ScrollRect/ScrollBar' " +
                "to the custom ScrollArea.");
            DestroyCustomWindow();
            return;
        }

        // changes the custom scrollbar's appearance
        _customScrollBar.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 775);
        _customScrollBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2, 20);

        // Same problem as onScroll: dragging would move the original reward list.
        ClearPersistentListeners(customScrollBarComp.OnDrag);
        scrollBarField.SetValue(_customScrollArea, customScrollBarComp);
        // Plugin.Logger.LogInfo(
        //     $"Cloned 'Scroll R/ScrollRect/ScrollBar' under " +
        //     $"'{_customWindow.name}' and assigned it to the ScrollArea.");


        // - scrollContent ---------------------------------------------------------------------------------------------
        var scrollContent = _customScrollArea.transform.Find("ScrollContent") as RectTransform;
        if (scrollContent == null)
        {
            Plugin.Logger.LogError("Could not find ScrollContent.");
            return;
        }

        _customScrollArea.ItemContainer = scrollContent;
        var originalLayout = _customContainer.GetComponent<GridLayoutGroup>();
        var newLayout = scrollContent.GetComponent<GridLayoutGroup>();
        if (newLayout == null) newLayout = scrollContent.gameObject.AddComponent<GridLayoutGroup>();
        newLayout.padding = originalLayout.padding;
        newLayout.cellSize = originalLayout.cellSize;
        // changes the "margin" of the mission buttons in the window
        // 76.0f is too much. too lazy to test decimals
        newLayout.spacing = new Vector2(75.0f, 75.0f);


        // -------------------------------------------------------------------------------------------------------------
        // "opens and closes" replay and legacy so that their missions are created and can be put inside my custom window
        missionWindow.ToggleReplayWindow();
        missionWindow.ToggleReplayWindow();
        missionWindow.ToggleLegacyWindow();
        missionWindow.ToggleLegacyWindow();

        PopulateCustomWindow(missionWindow, scrollContent);
        if (Configs.IsOpenByDefault.Value) OpenCustomWindow();
    }


    // Toggles the cloned mission-list window when its button is pressed.
    private static void OnCustomButtonClicked(MissionSelectWindow instance)
    {
        if (_customWindow == null) return;
        instance.legacyWindow.SetActive(false);
        instance.replayWindow.SetActive(false);

        if (_customWindow.activeSelf) CloseCustomWindow();
        else
        {
            PopulateCustomWindow(
                instance,
                _customScrollArea.transform.Find("ScrollContent") as RectTransform
            );
            OpenCustomWindow();
        }
    }

    private static void CloseCustomWindow()
    {
        if (_customWindow != null) _customWindow.SetActive(false);
    }

    private static void OpenCustomWindow()
    {
        if (_customWindow == null) return;
        _customWindow.SetActive(true);
        RefreshCustomScroll();
        PlayerLook.Instance.DotTransition.Enable(
            (RectTransform)_customWindow.transform,
            Global.UIColor
        );
        _customScrollBar.GetComponent<BevelRectOutline>().color = Global.UIColor;
        _customScrollBar.transform.GetChild(0).GetChild(0).GetComponent<BevelRect>().color = Global.UIColor;
    }


    // Must run AFTER the window is active: the layout rebuild is a no-op on an
    // inactive hierarchy, and ScrollArea.Awake (which hooks the bar to the
    // content) only runs on first activation.
    private static void RefreshCustomScroll()
    {
        if (_customScrollArea == null) return;
        var content = _customScrollArea.ItemContainer;
        var bar = _customScrollArea.ScrollBar;
        if (content == null || bar == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        // Wheel step per notch; the cloned bar's serialized speed is meant for
        // the info window and would jump from one end to the other.
        _customScrollArea.UpdateScrollSpeed();
        float maxScroll = _customScrollArea.GetMaxScrollPos(null);
        if (_customScrollBar != null) _customScrollBar.SetActive(maxScroll > 0f);
        bar.SetScrollY(1f); // 1 = top of the list

        // Plugin.Logger.LogInfo(
        //     $"scroll: persistent={_customScrollArea.onScroll.GetPersistentEventCount()} " +
        //     $"max={maxScroll} contentH={content.rect.height} " +
        //     $"areaH={((RectTransform)_customScrollArea.transform).rect.height} " +
        //     $"speed={bar.ScrollSpeed}");
    }

    private static void DestroyCustomWindow()
    {
        if (_customWindow != null) Object.Destroy(_customWindow);
        _customWindow = null;
        _customScrollArea = null;
        _customScrollBar = null;
    }

    private static void WireCloseButton(GameObject window)
    {
        var defaultCloseButton = window.GetComponentInChildren<DefaultButton>();
        ClearPersistentListeners(defaultCloseButton.OnClickUp);
        defaultCloseButton.OnClickUp.AddListener(CloseCustomWindow);
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

        // Plugin.Logger.LogInfo("path : " + path);
        return path;
    }


    private static void PopulateCustomWindow(MissionSelectWindow missionWindow, RectTransform container)
    {
        var prefabField = AccessTools.Field(typeof(MissionSelectWindow), "missionButtonPrefab");
        var prefab = (MissionSelectButton)prefabField.GetValue(missionWindow);

        var missions = new List<MissionData>();
        foreach (var planet in missionWindow.planets)
        {
            // if the player hasnt unlocked x11b, skip the missions
            if (planet.planetReference.IsLocked()) continue;
            foreach (var activeMission in planet.activeMissions.Data)
            {
                if (activeMission.Button == null ||
                    !activeMission.Button.gameObject.activeSelf)
                    continue;

                var missionData = activeMission.Button.Mission;

                if (!FilterMission(missionData.Mission, activeMission.Source)) continue;

                missions.Add(missionData);
            }
        }

        // replay window's missions
        var replayContainerField = AccessTools.Field(
            typeof(MissionSelectWindow),
            "replayMissionContainer"
        );
        var replayContainer = replayContainerField?.GetValue(missionWindow) as Transform;
        if (replayContainer == null)
            Plugin.Logger.LogError("Could not find MissionSelectWindow.replayMissionContainer.");
        else
        {
            foreach (var replayButton in replayContainer.GetComponentsInChildren<MissionSelectButton>(true))
            {
                if (!replayButton.gameObject.activeSelf) continue;

                var missionData = replayButton.Mission;
                if (!FilterMission(missionData.Mission, replayButton) ||
                    missions.Contains(missionData))
                    continue;


                // stupid goofy aah if statement
                // NOT add ge_amalgamation , and NOT add groundzero if Configs.WeekliesEnum.HideAll , and NOT add groundzero if Configs.WeekliesEnum.Vanilla AND completedweekly is true, and ALWAYS add groundzero if Configs.WeekliesEnum.HideNone
                bool isGroundzero = missionData.Mission._missionName == "groundzero";
                bool includeGroundzero =
                    Configs.WeekliesVisibility.Value == Configs.WeekliesEnum.HideNone ||
                    (Configs.WeekliesVisibility.Value == Configs.WeekliesEnum.Vanilla &&
                     !missionData.Mission.HasCompletedThisWeek());
                if (missionData.Mission._missionName != "ge_amalgamation" &&
                    (isGroundzero ? includeGroundzero : Configs.ReplayMissions.Value))
                    missions.Add(missionData);
            }
        }

        // legacy window's missions
        if (Configs.LegacyMissions.Value)
        {
            var legacyContainerField = AccessTools.Field(
                typeof(MissionSelectWindow),
                "legacyMissionContainer"
            );
            var legacyContainer = legacyContainerField?.GetValue(missionWindow) as Transform;
            if (legacyContainer == null)
                Plugin.Logger.LogError("Could not find MissionSelectWindow.legacyMissionContainer.");
            else
            {
                foreach (var legacyButton in legacyContainer.GetComponentsInChildren<MissionSelectButton>(true))
                {
                    if (!legacyButton.gameObject.activeSelf) continue;

                    var missionData = legacyButton.Mission;
                    if (!FilterMission(missionData.Mission, legacyButton) ||
                        missions.Contains(missionData))
                        continue;

                    missions.Add(missionData);
                }
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

    private static bool FilterMission(Mission mission, object source)
    {
        // Plugin.Logger.LogInfo("region.GetName() : " + missionData.Region.GetName()); // Moon (this)
        // Plugin.Logger.LogInfo("Mission._missionName : " +
        //                       mission._missionName); //sax_transport
        // Plugin.Logger.LogInfo("Mission.completedweekly : " +
        //                       mission
        //                           .HasCompletedThisWeek());
        // Plugin.Logger.LogInfo(
        //     mission ==
        //     Mission.GetMissionData(Global.Instance.OvertimeAssignment).Mission);
        // Plugin.Logger.LogInfo(
        //     $"mission data container: {missionData.Container?.GetType().Name ?? "null"}; " +
        //     $"mission source: {activeMission.Source?.GetType().Name ?? "null"}; " +
        //     $"is overtime assignment: " +
        //     $"{activeMission.Source is MissionSelectButton && OtButtons.Contains(activeMission.Source)}");
        // var data = activeMission.Button.Mission;
        // var otData = Mission.GetMissionData(Global.Instance.OvertimeAssignment);
        // Plugin.Logger.LogInfo(
        //     $"same data: {ReferenceEquals(data, otData)}, " +
        //     $"same mission: {ReferenceEquals(data.Mission, otData.Mission)}");


        if (!Configs.Freeroam.Value && mission._missionName == "freeroam") return false;
        if (!Configs.Incursion.Value && mission._missionName == "incursion") return false;
        if (!Configs.Ouroboros.Value && mission._missionName == "ouroboros") return false;
        if (!Configs.Ouroblitz.Value && mission._missionName == "ouroblitz") return false;
        if (!Configs.OxybreachE.Value && mission._missionName == "oxybreach_e") return false;
        if (Configs.WeekliesVisibility.Value == Configs.WeekliesEnum.HideAll)
        {
            if (mission.HasCompletedThisWeek())
            {
                // OT
                if (source is MissionSelectButton && OtButtons.Contains(source)) return false;
                // Amalgam...
                if (mission.ResetWeekly) return false;
            }
        }

        return true;
    }

    private static int GetBiomeOrder(MissionData mission)
    {
        for (int index = 0; index < BiomeOrder.Length; index++)
        {
            if (string.Equals(
                    BiomeOrder[index],
                    mission.Region.GetName(),
                    System.StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return BiomeOrder.Length;
    }

    private static int GetMissionTypeOrder(MissionData mission)
    {
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
        if (_customWindow == null || !_customWindow.activeSelf) return false;
        CloseCustomWindow();
        return true;
    }

    internal static void TrackOtButton(MissionContainer missionContainer, MissionSelectButton button)
    {
        if (missionContainer == Global.Instance.OvertimeAssignment) OtButtons.Add(button);
    }
}

// tf is that
[HarmonyPatch]
internal static class OtTrackingPatches
{
    [HarmonyPatch(typeof(MissionSelectWindow), "SetupMission")]
    [HarmonyPostfix]
    private static void SetupMissionPostfix(
        MissionContainer __0,
        MissionSelectButton __2)
    {
        MissionList.TrackOtButton(__0, __2);
    }
}