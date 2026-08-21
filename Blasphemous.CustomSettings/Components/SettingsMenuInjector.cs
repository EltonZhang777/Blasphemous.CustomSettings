using Blasphemous.ModdingAPI;
using Gameplay.UI.Others.Buttons;
using System.Collections.Generic;
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
        List<SettingsOption> options = SettingsMenuRegister.RegisteredOptions
            .Where(x => x.Type == OptionType.Toggle)
            .ToList();
        if (options.Count == 0)
            return;

        Transform selection = MenuLocator.FindOptionsSelection(VanillaMenuTarget.Game);
        if (selection == null)
            return;

        GameObject template = TemplateLocator.FindToggleTemplate();
        if (template == null)
            return;

        foreach (SettingsOption option in options)
        {
            InjectToggleIntoGame(option, selection, template);
        }

        // Reconcile the complete ring on every GAME menu open. This also repairs the ring after
        // OptionsWidget or a scene transition has restored the vanilla navigation links.
        LinkNavigation(selection);
    }

    private static void InjectToggleIntoGame(SettingsOption option, Transform selection, GameObject template)
    {
        GameObject existing = FindRuntimeClone(option, selection);
        if (existing != null)
        {
            option.RuntimeUI = existing;
            ModLog.Info($"Reusing injected custom settings toggle `{option.Id}` in current game menu");
            return;
        }

        // A RuntimeUI reference can point to an object destroyed with the previous menu scene.
        // Clear it before creating the current scene's instance.
        option.RuntimeUI = null;

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

        // The Selection container's VerticalLayoutGroup is disabled at runtime (it is an editor-time
        // helper); the vanilla options are laid out by absolute coordinates. Re-pack the list by
        // temporarily enabling the layout group so it spaces every child (vanilla + injected clones)
        // automatically, then restore the runtime-disabled state.
        VerticalLayoutGroup vlg = selection.GetComponent<VerticalLayoutGroup>();
        if (vlg != null && !vlg.enabled)
        {
            vlg.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(selection as RectTransform);
            vlg.enabled = false;
        }
        else if (vlg != null)
        {
            // Already enabled — just rebuild so the new clone is spaced in.
            LayoutRebuilder.ForceRebuildLayoutImmediate(selection as RectTransform);
        }

        // Diagnostic: inspect the layout setup and where the clone ends up.
        Transform controls = selection.childCount >= 4 ? selection.GetChild(3) : null;
        string posOf(Transform t) => t == null ? "n/a" : $"anchored={t.GetComponent<RectTransform>()?.anchoredPosition} local={t.localPosition}";
        ModLog.Info($"DIAG vlg.enabled={(vlg != null ? vlg.enabled : false)} controls({(controls != null ? controls.name : "null")}) pos={posOf(controls)} childCount={selection.childCount}");
        ModLog.Info($"DIAG post-layout clone.name=`{clone.name}` localPos={clone.transform.localPosition} anchoredPos={(clone.transform as RectTransform)?.anchoredPosition} sibling={clone.transform.GetSiblingIndex()}/{selection.childCount} activeInHierarchy={clone.activeInHierarchy}");
        ModLog.Info($"Injected custom settings toggle `{option.Id}` into game menu");
    }

    private static GameObject FindRuntimeClone(SettingsOption option, Transform selection)
    {
        GameObject runtime = option.RuntimeUI;
        if (runtime != null && runtime.transform.parent == selection)
            return runtime;

        GameObject namedClone = FindChildByName(selection, $"ModToggle {option.Id}");
        return namedClone != null && namedClone.GetComponent<ModToggleOption>() != null
            ? namedClone
            : null;
    }

    /// <summary>
    /// Links the cloned option into the existing menu navigation. The vanilla GAME menu is a ring:
    /// the first option's selectOnUp points at the last, and the last's selectOnDown points at the first.
    /// We insert the clone into that ring so up/down both pass through it.
    /// Navigation uses the EventsButton living on each option's "XXXText" child, not the option root.
    /// </summary>
    private static void LinkNavigation(Transform selection)
    {
        ModLog.Info("[Diag] Linking Navigation...");
        if (selection.childCount < 3)
        {
            ModLog.Error($"Failed to link custom settings navigation: selection has {selection.childCount} child(ren)");
            return;
        }

        Transform first = selection.GetChild(0);
        EventsButton firstButton = first.GetComponentsInChildren<EventsButton>(true).FirstOrDefault();

        Transform previous = null;
        var customToggles = new List<ModToggleOption>();
        var customButtons = new List<EventsButton>();
        for (int i = 0; i < selection.childCount; i++)
        {
            Transform child = selection.GetChild(i);
            ModToggleOption customToggle = child.GetComponent<ModToggleOption>();
            EventsButton button = child.GetComponentsInChildren<EventsButton>(true).FirstOrDefault();
            if (customToggle != null)
            {
                if (button == null)
                {
                    ModLog.Error($"Failed to link custom settings navigation: custom child `{child.name}` has no EventsButton");
                    return;
                }
                customToggles.Add(customToggle);
                customButtons.Add(button);
            }
            else if (button != null)
            {
                // The last vanilla child is the tail of the original GAME menu ring.
                previous = child;
            }
        }

        EventsButton prevButton = previous != null
            ? previous.GetComponentsInChildren<EventsButton>(true).FirstOrDefault()
            : null;
        ModLog.Info($"DIAG navigation components previous={(previous != null ? previous.name : "null")}:{(previous != null ? previous.GetComponentsInChildren<EventsButton>(true).Length : 0)} first={first.name}:{first.GetComponentsInChildren<EventsButton>(true).Length} customCount={customButtons.Count}");
        if (prevButton == null || firstButton == null || customButtons.Count == 0)
        {
            ModLog.Error($"Failed to link custom settings navigation: previousButton={(prevButton != null)} firstButton={(firstButton != null)} customCount={customButtons.Count}");
            return;
        }

        EventsButton previousButton = prevButton;
        for (int i = 0; i < customButtons.Count; i++)
        {
            EventsButton customButton = customButtons[i];
            customButton.gameObject.name = customButtons.Count == 1
                ? "ModToggle_NavText"
                : $"ModToggle_NavText_{i}";

            SetDown(previousButton, customButton);
            SetUp(customButton, previousButton);
            previousButton = customButton;
        }
        SetDown(previousButton, firstButton);
        SetUp(firstButton, previousButton);

        // Diagnostic: navigation/interaction state of the clone and its neighbours.
        string navDesc(EventsButton b) => b == null ? "null" : $"mode={b.navigation.mode} interactable={b.interactable} isActive={b.IsActive()} up={(b.navigation.selectOnUp != null ? b.navigation.selectOnUp.name : "null")} down={(b.navigation.selectOnDown != null ? b.navigation.selectOnDown.name : "null")}";
        EventsButton firstCustom = customButtons[0];
        ModLog.Info($"DIAG nav prev({prevButton.name}) [{navDesc(prevButton)}] first({firstButton.name}) [{navDesc(firstButton)}] clone({firstCustom.name}) [{navDesc(firstCustom)}]");

        for (int i = 0; i < customButtons.Count; i++)
        {
            EventsButton up = i == 0 ? prevButton : customButtons[i - 1];
            EventsButton down = i == customButtons.Count - 1 ? firstButton : customButtons[i + 1];
            customToggles[i].AttachNavigation(up, down, customButtons[i]);
        }
    }

    private static void SetDown(EventsButton source, EventsButton target)
    {
        Navigation navigation = source.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnDown = target;
        source.navigation = navigation;
    }

    private static void SetUp(EventsButton source, EventsButton target)
    {
        Navigation navigation = source.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnUp = target;
        source.navigation = navigation;
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
