using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Gameplay.UI.Others.Buttons;
using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;
using Rewired.Integration.UnityUI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Consumes the registration table and injects custom option UI into the vanilla settings menu at scene load.
/// </summary>
internal static class SettingsMenuInjector
{
    private static readonly Dictionary<RewiredStandaloneInputModule, string> SuspendedVerticalAxes
        = new Dictionary<RewiredStandaloneInputModule, string>();
    private static readonly Dictionary<CustomEventInput, bool> SuspendedCustomInputs
        = new Dictionary<CustomEventInput, bool>();
    private static readonly Dictionary<EventsButton, Navigation> SuspendedVerticalNavigations
        = new Dictionary<EventsButton, Navigation>();

    private static ModNavigationController _navigationController;
    private static bool _gameMenuActive;
    private static string _manualVerticalAxis;
    private static float _manualRepeatDelay;
    private static float _manualInputActionsPerSecond = 10f;
    private static float _nextCustomInputScanTime;

    /// <summary>
    /// Makes the custom navigation controller the only vertical-navigation authority while
    /// the GAME submenu is active. Rewired still handles horizontal movement and submit/cancel.
    /// </summary>
    internal static void EnterGameMenu()
    {
        PruneSuspendedModules();
        _gameMenuActive = true;

        _nextCustomInputScanTime = 0f;
        SuspendCompetingCustomInputs();
        RewiredStandaloneInputModule[] rewiredModules = Object.FindObjectsOfType<RewiredStandaloneInputModule>();

        foreach (RewiredStandaloneInputModule module in rewiredModules)
        {
            if (module == null)
                continue;

            if (!SuspendedVerticalAxes.ContainsKey(module))
            {
                SuspendedVerticalAxes.Add(module, module.verticalAxis);
                if (string.IsNullOrEmpty(_manualVerticalAxis))
                {
                    _manualVerticalAxis = module.verticalAxis;
                    _manualRepeatDelay = module.repeatDelay;
                    _manualInputActionsPerSecond = module.inputActionsPerSecond;
                }
            }
            module.verticalAxis = string.Empty;
        }

        EnsureNavigationController();
        ModLogExtensions.DebugIfDebugBuild($"DIAG navigation input authority=ModNavigationController customEventInputSuspended={SuspendedCustomInputs.Count} rewiredModules={rewiredModules.Length} rewiredVerticalSuspended={SuspendedVerticalAxes.Count} axis={_manualVerticalAxis}");
    }

    /// <summary>
    /// Restores all input components after leaving the GAME submenu.
    /// </summary>
    internal static void ExitGameMenu()
    {
        _gameMenuActive = false;
        if (_navigationController != null)
            _navigationController.DisableNavigation();
        RestoreVerticalNavigations();
        RestoreVerticalAxes();
        RestoreCustomInputs();
        _manualVerticalAxis = null;
        _manualRepeatDelay = 0f;
        _manualInputActionsPerSecond = 10f;
        _nextCustomInputScanTime = 0f;
    }

    /// <summary>
    /// Keeps a late-enabled legacy CustomEventInput from becoming a second vertical authority.
    /// </summary>
    internal static void SuspendCompetingCustomInputs()
    {
        if (Time.unscaledTime < _nextCustomInputScanTime && SuspendedCustomInputs.Count > 0)
            return;
        _nextCustomInputScanTime = Time.unscaledTime + 0.25f;

        CustomEventInput[] customInputs = Object.FindObjectsOfType<CustomEventInput>();
        foreach (CustomEventInput input in customInputs)
        {
            if (input == null)
                continue;

            if (!SuspendedCustomInputs.ContainsKey(input))
                SuspendedCustomInputs.Add(input, input.enabled);
            input.enabled = false;
        }
    }

    /// <summary>
    /// Binds the complete GAME-menu ring to the single vertical navigation controller.
    /// </summary>
    internal static void ConfigureManualNavigation(IList<EventsButton> buttons)
    {
        ModNavigationController controller = EnsureNavigationController();
        if (controller == null)
            return;

        controller.Configure(
            buttons,
            _manualVerticalAxis,
            _manualRepeatDelay,
            _manualInputActionsPerSecond);
    }

    /// <summary>
    /// Makes one custom toggle the sole selected visual while clearing the vanilla GAME rows.
    /// The vanilla option state cannot represent a custom entry, so its stale selection must
    /// be cleared at the UI boundary when the custom entry receives focus.
    /// </summary>
    internal static void SelectCustomToggle(ModToggleOption selected)
    {
        if (selected == null)
            return;

        ClearVanillaGameSelection();

        Transform selection = selected.transform.parent;
        GameObject current = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;
        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG custom visual select owner={selected.name} current={(current != null ? current.name : "null")} parent={(selection != null ? selection.name : "null")} frame={Time.frameCount}");
        if (selection == null)
        {
            selected.SetSelected(true);
            return;
        }

        foreach (Transform child in selection)
        {
            ModToggleOption customToggle = child.GetComponent<ModToggleOption>();
            if (customToggle != null)
            {
                customToggle.SetSelected(customToggle == selected);
                continue;
            }

            ClearVanillaSelection(child.gameObject);
        }
    }

    private static void ClearVanillaGameSelection()
    {
        OptionsWidget widget = Object.FindObjectOfType<OptionsWidget>();
        var setOptionGameSelected = AccessTools.Method(
            typeof(OptionsWidget),
            "SetOptionGameSelected");
        if (widget == null || setOptionGameSelected == null)
            return;

        foreach (OptionsWidget.GAME_OPTIONS option in
                 System.Enum.GetValues(typeof(OptionsWidget.GAME_OPTIONS)))
        {
            setOptionGameSelected.Invoke(widget, new object[] { option, false });
        }
    }

    internal static bool ShouldBypassKeepFocus()
    {
        if (!_gameMenuActive || _navigationController == null || !_navigationController.enabled)
            return false;

        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null
            && _navigationController.IsNavigationTarget(eventSystem.currentSelectedGameObject);
    }

    internal static bool IsCustomSelectionActive()
    {
        if (!_gameMenuActive || _navigationController == null || !_navigationController.enabled)
            return false;

        EventSystem eventSystem = EventSystem.current;
        GameObject current = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        return current != null
            && _navigationController.IsNavigationTarget(current)
            && current.GetComponentInParent<ModToggleOption>() != null;
    }

    private static ModNavigationController EnsureNavigationController()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return null;

        if (_navigationController == null
            || _navigationController.gameObject != eventSystem.gameObject)
        {
            _navigationController = eventSystem.gameObject.GetOrElseAddComponent<ModNavigationController>();
        }

        return _navigationController;
    }

    private static void RestoreVerticalAxes()
    {
        foreach (KeyValuePair<RewiredStandaloneInputModule, string> pair in SuspendedVerticalAxes.ToList())
        {
            if (pair.Key != null)
                pair.Key.verticalAxis = pair.Value;
            SuspendedVerticalAxes.Remove(pair.Key);
        }
    }

    private static void RestoreCustomInputs()
    {
        foreach (KeyValuePair<CustomEventInput, bool> pair in SuspendedCustomInputs.ToList())
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
            SuspendedCustomInputs.Remove(pair.Key);
        }
    }

    private static void PruneSuspendedModules()
    {
        foreach (RewiredStandaloneInputModule module in SuspendedVerticalAxes.Keys.ToList())
        {
            if (module == null)
                SuspendedVerticalAxes.Remove(module);
        }

        foreach (CustomEventInput input in SuspendedCustomInputs.Keys.ToList())
        {
            if (input == null)
                SuspendedCustomInputs.Remove(input);
        }
    }

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
            ModToggleOption existingToggle = existing.GetComponent<ModToggleOption>();
            EventsButton existingButton = existing.GetComponentsInChildren<EventsButton>(true).FirstOrDefault();
            if (existingToggle != null)
                existingToggle.AttachButton(existingButton);
            ModLog.Info($"Reusing injected custom settings toggle `{option.Id}` in current game menu");
            return;
        }

        // A RuntimeUI reference can point to an object destroyed with the previous menu scene.
        // Clear it before creating the current scene's instance.
        option.RuntimeUI = null;

        // Diagnostic: where did we resolve the game selection to?
        ModLogExtensions.DebugIfDebugBuild($"DIAG selection.name=`{selection.name}` parent=`{(selection.parent != null ? selection.parent.name : "null")}` childCount={selection.childCount} active={selection.gameObject.activeInHierarchy}");

        // Clone the template option
        GameObject clone = Object.Instantiate(template, selection);
        clone.name = $"ModToggle {option.Id}";

        // Diagnostic: where did the clone land, and is it visible?
        ModLogExtensions.DebugIfDebugBuild($"DIAG injected clone.name=`{clone.name}` parent=`{(clone.transform.parent != null ? clone.transform.parent.name : "null")}` activeSelf={clone.activeSelf} activeInHierarchy={clone.activeInHierarchy} localPos={clone.transform.localPosition} selection.childCount={selection.childCount}");

        // Locate the template's value text (the vanilla highlightableText, which displays Enabled/Disabled)
        EventsButton button = clone.GetComponentsInChildren<EventsButton>(true).FirstOrDefault();
        MenuButton menuButton = button != null ? button.GetComponent<MenuButton>() : null;
        Text valueTemplate = TemplateLocator.FindToggleValueText(selection, template);
        Text valueText = null;
        GameObject valueClone = null;
        Vector3 valueOffset = Vector3.zero;
        if (valueTemplate != null)
        {
            Text titleTemplate = clone.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (titleTemplate != null)
                valueOffset = valueTemplate.transform.position - titleTemplate.transform.position;

            valueClone = Object.Instantiate(valueTemplate.gameObject);
            valueClone.name = $"ModToggle {option.Id} Value";
            valueClone.transform.SetParent(selection.parent, true);
            valueText = valueClone.GetComponent<Text>();
        }
        if (valueText == null)
            valueText = TemplateLocator.FindToggleValueText(clone);
        if (valueText == null && button != null)
            valueText = menuButton != null ? menuButton.buttonText : button.GetComponentInChildren<Text>(true);
        Text titleText = clone.GetComponentsInChildren<Text>(true)
            .FirstOrDefault(text => text != valueText);
        GameObject selectionObj = EnsureSelectionImage(clone, template);
        if (valueText == null)
        {
            AbortInjection(option, clone, $"Failed to inject `{option.Id}`: no toggle value text found in template");
            return;
        }
        if (titleText == null)
        {
            AbortInjection(option, clone, $"Failed to inject `{option.Id}`: no title text found in toggle template");
            return;
        }
        if (button == null)
        {
            AbortInjection(option, clone, $"Failed to inject option={option.Id}: no EventsButton found in toggle template");
            return;
        }
        if (selectionObj == null)
            ModLog.Warn($"Custom settings toggle `{option.Id}` has no `Img` selection image");

        // Wire up the toggle behaviour. The title is rendered on the left and the value on the right.
        option.RuntimeUI = clone;
        ModToggleOption toggle = clone.AddComponent<ModToggleOption>();
        toggle.Initialize(option, valueText, selectionObj, valueText, titleText);
        toggle.AttachButton(button);

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
        ModLogExtensions.DebugIfDebugBuild($"DIAG vlg.enabled={(vlg != null ? vlg.enabled : false)} controls({(controls != null ? controls.name : "null")}) pos={posOf(controls)} childCount={selection.childCount}");
        ModLogExtensions.DebugIfDebugBuild($"DIAG post-layout clone.name=`{clone.name}` localPos={clone.transform.localPosition} anchoredPos={(clone.transform as RectTransform)?.anchoredPosition} sibling={clone.transform.GetSiblingIndex()}/{selection.childCount} activeInHierarchy={clone.activeInHierarchy}");
        if (valueClone != null)
            valueClone.transform.position = titleText.transform.position + valueOffset;
        ModLog.Info($"Injected custom settings toggle `{option.Id}` into game menu");
    }

    private static void AbortInjection(SettingsOption option, GameObject clone, string message)
    {
        clone.transform.SetParent(null);
        Object.Destroy(clone);
        option.RuntimeUI = null;
        ModLog.Error(message);
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
        ModLogExtensions.DebugIfDebugBuild("[Diag] Linking Navigation...");
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
        var navigationButtons = new List<EventsButton>();
        for (int i = 0; i < selection.childCount; i++)
        {
            Transform child = selection.GetChild(i);
            ModToggleOption customToggle = child.GetComponent<ModToggleOption>();
            EventsButton button = child.GetComponentsInChildren<EventsButton>(true).FirstOrDefault();
            if (button != null)
                navigationButtons.Add(button);
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
        ModLogExtensions.DebugIfDebugBuild($"DIAG navigation components previous={(previous != null ? previous.name : "null")}:{(previous != null ? previous.GetComponentsInChildren<EventsButton>(true).Length : 0)} first={first.name}:{first.GetComponentsInChildren<EventsButton>(true).Length} customCount={customButtons.Count}");
        if (prevButton == null || firstButton == null || customButtons.Count == 0)
        {
            ModLog.Error($"Failed to link custom settings navigation: previousButton={(prevButton != null)} firstButton={(firstButton != null)} customCount={customButtons.Count}");
            return;
        }

        for (int i = 0; i < customButtons.Count; i++)
        {
            customButtons[i].gameObject.name = customButtons.Count == 1
                ? "ModToggle_NavText"
                : $"ModToggle_NavText_{i}";
        }

        SuspendVerticalNavigations(navigationButtons);

        ModLogExtensions.DebugIfDebugBuild($"DIAG manual navigation nodes={navigationButtons.Count}");
        ConfigureManualNavigation(navigationButtons);
    }

    private static void SuspendVerticalNavigations(IList<EventsButton> buttons)
    {
        if (buttons == null)
            return;

        foreach (EventsButton button in buttons)
        {
            if (button == null)
                continue;

            if (!SuspendedVerticalNavigations.ContainsKey(button))
                SuspendedVerticalNavigations.Add(button, button.navigation);

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = null;
            navigation.selectOnDown = null;
            button.navigation = navigation;
        }
    }

    private static void RestoreVerticalNavigations()
    {
        foreach (KeyValuePair<EventsButton, Navigation> pair in SuspendedVerticalNavigations.ToList())
        {
            if (pair.Key != null)
                pair.Key.navigation = pair.Value;
            SuspendedVerticalNavigations.Remove(pair.Key);
        }
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

    private static GameObject EnsureSelectionImage(GameObject clone, GameObject template)
    {
        GameObject image = FindChildByName(clone.transform, "Img");
        if (image != null)
            return image;

        GameObject templateImage = FindChildByName(template.transform, "Img");
        if (templateImage == null)
            return null;

        image = Object.Instantiate(templateImage, clone.transform);
        image.name = "Img";
        image.transform.SetSiblingIndex(Mathf.Min(
            templateImage.transform.GetSiblingIndex(),
            clone.transform.childCount - 1));
        return image;
    }

    private static void ClearVanillaSelection(GameObject option)
    {
        GameObject image = FindChildByName(option.transform, "Img");
        bool imageBefore = image != null && image.activeSelf;
        Text[] texts = option.GetComponentsInChildren<Text>(true);
        Text highlightableText = option.GetComponentInChildren<Text>(true);
        string textStateBefore = string.Join(
            "|",
            texts.Select(text => $"{text.name}:{text.color}").ToArray());

        MenuButton[] menuButtons = option.GetComponentsInChildren<MenuButton>(true);
        if (image != null)
            image.SetActive(false);
        if (highlightableText != null)
            highlightableText.color = ModToggleOption.NormalOptionColor;

        foreach (MenuButton menuButton in menuButtons)
            menuButton.OnDeselect(null);

        string textStateAfter = string.Join(
            "|",
            texts.Select(text => $"{text.name}:{text.color}").ToArray());
        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG vanilla visual option={option.name} img={imageBefore}->{(image != null ? image.activeSelf.ToString() : "none")} menuButtons={menuButtons.Length} textBefore={textStateBefore} textAfter={textStateAfter}");
    }
}
