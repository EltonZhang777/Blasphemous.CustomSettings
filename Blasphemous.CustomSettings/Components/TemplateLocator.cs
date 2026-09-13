using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Gameplay.UI.Others.MenuLogic;
using System.Collections;
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
        GameObject mappedTemplate = FindGameOptionTemplate(OptionsWidget.GAME_OPTIONS.ENABLEHOWTOPLAY);
        if (mappedTemplate != null)
            return mappedTemplate;

        // The GAME selection container was already located by MenuLocator (reflection optionsRoot / scene path)
        Transform selection = MenuLocator.FindOptionsSelection(VanillaMenuTarget.Game);
        if (selection == null)
        {
            ModLog.Error("Failed to locate vanilla toggle template: could not locate GAME menu selection");
            return null;
        }

        // Channel 1: find the option whose paired value object renders an Enabled/Disabled state.
        foreach (Transform child in selection)
        {
            if (FindToggleValueText(selection, child.gameObject) != null)
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

    internal static GameObject FindTemplate(OptionType type)
    {
        OptionsWidget.GAME_OPTIONS vanillaOption = type switch
        {
            OptionType.Toggle => OptionsWidget.GAME_OPTIONS.ENABLEHOWTOPLAY,
            OptionType.Arrow => OptionsWidget.GAME_OPTIONS.AUDIOLANGUAGE,
            OptionType.Text => OptionsWidget.GAME_OPTIONS.CONTROLSREMAP,
            _ => OptionsWidget.GAME_OPTIONS.ENABLEHOWTOPLAY
        };

        GameObject template = FindGameOptionTemplate(vanillaOption);
        if (template != null)
            return template;

        return type == OptionType.Toggle ? FindToggleTemplate() : null;
    }

    internal static Text FindDefaultValueText(Transform selection, GameObject toggleTemplate)
    {
        Text valueText = FindToggleValueText(selection, toggleTemplate);
        if (valueText != null)
            return valueText;

        return FindGameOptionValueText(OptionsWidget.GAME_OPTIONS.ENABLEHOWTOPLAY);
    }

    internal static void AlignGameOptionValueTexts(Transform selection)
    {
        IDictionary elements = FindGameElements();
        if (elements == null || selection == null)
            return;

        foreach (DictionaryEntry entry in elements)
        {
            if (!(entry.Key is OptionsWidget.GAME_OPTIONS)
                || !(entry.Value is SelectableOption selectable)
                || selectable.parent == null
                || selectable.highlightableText == null)
                continue;

            Transform row = selection.Find(selectable.parent.name);
            if (row == null)
                continue;

            Text title = row.GetComponentInChildren<Text>(true);
            if (title == null)
                continue;

            Transform value = selectable.highlightableText.transform;
            Transform valueRoot = value.parent != null
                && value.parent.name == selectable.parent.name
                ? value.parent
                : value;
            if (valueRoot == selection || valueRoot == selection.parent || valueRoot.IsChildOf(selection))
                continue;

            Vector3 position = valueRoot.position;
            position.y += title.transform.position.y - value.position.y;
            valueRoot.position = position;
        }
    }

    private static GameObject FindGameOptionTemplate(OptionsWidget.GAME_OPTIONS option)
    {
        IDictionary elements = FindGameElements();
        if (elements == null)
            return null;

        foreach (DictionaryEntry entry in elements)
        {
            if (!(entry.Key is OptionsWidget.GAME_OPTIONS key) || key != option
                || !(entry.Value is SelectableOption selectable))
                continue;

            if (selectable.parent != null)
            {
                Transform selection = MenuLocator.FindOptionsSelection(VanillaMenuTarget.Game);
                Transform selectionTemplate = selection?.Find(selectable.parent.name);
                GameObject template = selectionTemplate != null
                    ? selectionTemplate.gameObject
                    : selectable.parent;
                ModLog.Info($"Located vanilla {option} template: {template.name}");
                return template;
            }
        }

        return null;
    }

    private static Text FindGameOptionValueText(OptionsWidget.GAME_OPTIONS option)
    {
        IDictionary elements = FindGameElements();
        if (elements == null)
            return null;

        foreach (DictionaryEntry entry in elements)
        {
            if (!(entry.Key is OptionsWidget.GAME_OPTIONS key) || key != option
                || !(entry.Value is SelectableOption selectable))
                continue;

            return selectable.highlightableText;
        }

        return null;
    }

    private static IDictionary FindGameElements()
    {
        OptionsWidget widget = Object.FindObjectOfType<OptionsWidget>();
        return TraverseUtils.GetValue<IDictionary>(
            widget,
            "gameElements",
            TraverseUtils.TraverseAccessType.Field);
    }

    internal static Text FindToggleValueText(GameObject option)
    {
        if (option == null)
            return null;

        foreach (Text text in option.GetComponentsInChildren<Text>(true))
        {
            if (IsEnabledDisabledText(text))
                return text;
        }
        return null;
    }

    internal static Text FindToggleValueText(Transform selection, GameObject template)
    {
        Transform menuRoot = selection?.parent;
        if (menuRoot == null || template == null)
            return null;

        foreach (Text text in menuRoot.GetComponentsInChildren<Text>(true))
        {
            if (text.transform.IsChildOf(selection)
                || text.transform.parent == null
                || text.transform.parent.name != template.name)
                continue;
            if (IsEnabledDisabledText(text))
                return text;
        }
        return null;
    }

    private static bool IsEnabledDisabledText(Text text)
    {
        string value = text != null ? text.text ?? string.Empty : string.Empty;
        string upper = value.Trim().ToUpperInvariant();
        return upper == "ENABLED" || upper == "DISABLED";
    }
}
