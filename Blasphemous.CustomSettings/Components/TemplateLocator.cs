using Blasphemous.ModdingAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Locates the vanilla option objects used as templates for cloning custom option UI.
/// </summary>
internal static class TemplateLocator
{
    /// <summary>
    /// Finds the vanilla toggle option object ("Enable How To Play") to clone as the toggle template.
    /// Locates it inside the GAME menu's <c>Selection</c> container: the toggle is the option whose
    /// value text renders "ENABLED"/"DISABLED". Falls back to the 3rd child (vanilla layout order:
    /// AUDIO LANGUAGE, TEXT LANGUAGE, ENABLE HOW TO PLAY, CONTROLS REMAP) when no value-text match is found.
    /// </summary>
    internal static GameObject FindToggleTemplate()
    {
        // The GAME selection container was already located by MenuLocator (reflection optionsRoot / scene path)
        Transform selection = MenuLocator.FindOptionsSelection(VanillaMenuTarget.Game);
        if (selection == null)
        {
            ModLog.Error("Failed to locate vanilla toggle template: could not locate GAME menu selection");
            return null;
        }

        // Channel 1: find the option whose text renders an Enabled/Disabled state
        foreach (Transform child in selection)
        {
            if (IsEnabledDisabledText(child))
            {
                ModLog.Info($"Located vanilla toggle template: {child.name}");
                return child.gameObject;
            }
        }

        // Channel 2: fall back to the 3rd child (vanilla order: Audio Language, Text Language, Enable How To Play, ...)
        if (selection.childCount >= 3)
        {
            Transform fallback = selection.GetChild(2);
            ModLog.Warn($"No enabled/disabled text found; falling back to template: {fallback.name}");
            return fallback.gameObject;
        }

        ModLog.Error("Failed to locate vanilla toggle template: GAME menu selection has no matching option");
        return null;
    }

    private static bool IsEnabledDisabledText(Transform option)
    {
        foreach (Text text in option.GetComponentsInChildren<Text>(true))
        {
            string value = text.text ?? string.Empty;
            string upper = value.Trim().ToUpperInvariant();
            if (upper == "ENABLED" || upper == "DISABLED")
                return true;
        }
        return false;
    }
}