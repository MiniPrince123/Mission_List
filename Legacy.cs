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
        Debug.Log("Custom button clicked");
        Debug.Log(customWindow.GetName());
        Debug.Log(customWindow.GetComponentInChildren<TextMeshProUGUI>(true).text);

        if (customWindow == null)
            return;
        customWindow.SetActive(!customWindow.activeSelf);
    }
    
    private static GameObject customWindow;
    private static void CreateCustomWindow(MissionSelectWindow missionWindow)
    {
        var replayWindowField =
            AccessTools.Field(typeof(MissionSelectWindow), "replayWindow");
        
        if (replayWindowField == null)
        {
            Plugin.Logger.LogError("Could not find MissionSelectWindow.replayWindow.");
            return;
        }

        var replayWindow =
            (GameObject)replayWindowField.GetValue(missionWindow);
        
        if (replayWindow == null)
        {
            Plugin.Logger.LogError("MissionSelectWindow.replayWindow is null.");
            return;
        }

        customWindow = Object.Instantiate(
            replayWindow,
            replayWindow.transform.parent
        );

        customWindow.name = "MyCustomWindow";
        customWindow.GetComponentInChildren<TextMeshProUGUI>(true).text = "Mission List";
        customWindow.SetActive(false);
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