using Blasphemous.ModdingAPI;
using Gameplay.UI.Others.Buttons;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Consumes the registration table and injects custom option UI into the vanilla settings menu at scene load.
/// </summary>
internal static class SettingsMenuInjector
{
    /// <summary>
    /// Injects all registered custom options into the vanilla settings menus (called on level load when the menu is present).
    /// Currently injects Toggle options into the GAME submenu (issue #3 scope).
    /// </summary>
    internal static void InjectAll()
    {
        foreach (SettingsOption option in SettingsMenuRegister.RegisteredOptions.Where(x => x.Type == OptionType.Toggle).ToList())
        {
            InjectToggleIntoGame(option);
        }
    }

    private static void InjectToggleIntoGame(SettingsOption option)
    {
        // Already injected into this scene
        if (option.RuntimeUI != null)
            return;

        Transform selection = MenuLocator.FindOptionsSelection(VanillaMenuTarget.Game);
        GameObject template = TemplateLocator.FindToggleTemplate();
        if (selection == null || template == null)
        {
            ModLog.Error($"Skipping injection of `{option.Id}`: could not locate game menu or toggle template");
            return;
        }

        // Diagnostic: where did we resolve the game selection to?
        ModLog.Info($"DIAG selection.name=`{selection.name}` parent=`{(selection.parent != null ? selection.parent.name : "null")}` childCount={selection.childCount} active={selection.gameObject.activeInHierarchy}");

        // Clone the template option
        GameObject clone = Object.Instantiate(template, selection);
        clone.name = $"ModToggle {option.Id}";
        option.RuntimeUI = clone;

        // Diagnostic: where did the clone land, and is it visible?
        ModLog.Info($"DIAG injected clone.name=`{clone.name}` parent=`{(clone.transform.parent != null ? clone.transform.parent.name : "null")}` activeSelf={clone.activeSelf} activeInHierarchy={clone.activeInHierarchy} localPos={clone.transform.localPosition} selection.childCount={selection.childCount}");

        // Locate the template's value text (the vanilla highlightableText, which displays Enabled/Disabled)
        Text valueText = clone.GetComponentInChildren<Text>(true);
        GameObject selectionObj = FindChildByName(clone.transform, "Selection");
        if (valueText == null)
        {
            ModLog.Error($"Failed to inject `{option.Id}`: no text found in toggle template");
            return;
        }

        // Wire up the toggle behaviour. The value text renders the Enabled/Disabled state; the
        // registration title is kept as the cloned object's name and reported in the log, but the
        // vanilla template's own title label is left untouched (the exact title-text layout of the
        // vanilla option is not yet verified in the scene, so we avoid overwriting the value text).
        ModToggleOption toggle = clone.AddComponent<ModToggleOption>();
        toggle.Initialize(option, valueText, selectionObj, valueText);

        LinkNavigation(selection, clone);
        ModLog.Info($"Injected custom settings toggle `{option.Id}` into game menu");
    }

    /// <summary>
    /// Links the cloned option into the existing menu navigation: down from the previous last option, up from this one.
    /// </summary>
    private static void LinkNavigation(Transform selection, GameObject clone)
    {
        // The clone is appended as the last child; the option just above it is the previous last child
        if (selection.childCount < 2)
            return;

        Transform previous = selection.GetChild(selection.childCount - 2);
        EventsButton prevButton = previous.GetComponent<EventsButton>();
        EventsButton thisButton = clone.GetComponent<EventsButton>();

        if (prevButton == null || thisButton == null)
            return;

        var prevNav = prevButton.navigation;
        prevNav.selectOnDown = thisButton;
        prevButton.navigation = prevNav;

        var thisNav = thisButton.navigation;
        thisNav.selectOnUp = prevButton;
        thisButton.navigation = thisNav;
    }

    private static GameObject FindChildByName(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
                return child.gameObject;
        }
        return null;
    }
}