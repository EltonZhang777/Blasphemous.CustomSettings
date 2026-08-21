using Blasphemous.CustomSettings.Components;
using Gameplay.UI.Others.Buttons;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Runtime behaviour for a cloned custom toggle option.
/// Owns the current value, renders it on the option text, handles selection highlight,
/// and fires the owner option's <c>OnChange</c>/<c>OnClose</c> callbacks.
/// </summary>
internal class ModToggleOption : MonoBehaviour
{
    private SettingsOption _owner;
    private Text _valueText;
    private GameObject _selection;
    private Text _highlightableText;

    private bool _value;
    private bool _selected;

    /// <summary>
    /// Initializes this toggle from its owner option and the template's visual parts.
    /// </summary>
    internal void Initialize(SettingsOption owner, Text valueText, GameObject selection, Text highlightableText)
    {
        _owner = owner;
        _valueText = valueText;
        _selection = selection;
        _highlightableText = highlightableText;
        _value = owner.DefaultValue is bool b && b;

        owner.CurrentValue = _value;
        IsSelected = false;
        UpdateValueText();
    }

    /// <summary>
    /// Currently selected state (true when the cursor/selection rests on this option)
    /// </summary>
    internal bool IsSelected
    {
        get => _selected;
        set { _selected = value; RenderSelection(); }
    }

    /// <summary>
    /// Toggles the current value, fires the owner's <c>OnChange</c>.
    /// </summary>
    internal void ToggleValue()
    {
        _value = !_value;
        _owner.CurrentValue = _value;
        UpdateValueText();
        _owner.OnChange?.Invoke(_value);
    }

    /// <summary>
    /// Fires the owner's close callback (called when the settings menu/tab closes)
    /// </summary>
    internal void NotifyClose() => _owner.OnClose?.Invoke();

    // Navigation ring nodes. Some vanilla code may reset these navigation fields at runtime, so we
    // re-assert the ring every frame from Update() to keep the injected option reachable.
    private EventsButton _upButton;
    private EventsButton _downButton;
    private EventsButton _thisButton;
    private UnityEngine.GameObject _lastSelected;

    // The game's EventSystem sometimes skips a dynamically injected Selectable even when its
    // navigation data is valid. Keep a small, targeted fallback for the custom ring only.
    private Player _rewired;
    private GameObject _lastNavigationSelected;
    private float _lastVerticalAxis;

    private const int MenuVerticalAxis = 49;
    private const float NavigationAxisThreshold = 0.3f;

    /// <summary>
    /// Attaches this option to its two neighbours in the navigation ring.
    /// These are re-asserted every frame in <see cref="Update"/>.
    /// </summary>
    internal void AttachNavigation(EventsButton up, EventsButton down, EventsButton self)
    {
        _upButton = up;
        _downButton = down;
        _thisButton = self;
        _lastNavigationSelected = null;
        _lastVerticalAxis = 0f;
        AssertNavigationRing();

        // Diagnostic: is the injected toggle registered with EventSystem as a selectable?
        bool registered = false;
        var all = Selectable.allSelectables;
        for (int i = 0; i < all.Count; i++)
        {
            if (ReferenceEquals(all[i], self))
            {
                registered = true;
                break;
            }
        }
        UnityEngine.Debug.Log($"[CustomSettings] DIAG selectable-registered clone={self.name}:{registered} allCount={all.Count}");
    }

    private void Update()
    {
        AssertNavigationRing();

        // Diagnostic: log EventSystem current-selection changes to determine how navigation is driven.
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null)
            return;
        UnityEngine.GameObject current = es.currentSelectedGameObject;
        if (ReferenceEquals(current, _lastSelected))
            return;
        _lastSelected = current;
        UnityEngine.Debug.Log($"[CustomSettings] DIAG selected changed to `{(current != null ? current.name : "null")}`");
    }

    private void LateUpdate()
    {
        TryManualNavigation();
    }

    private void AssertNavigationRing()
    {
        if (_upButton == null || _downButton == null || _thisButton == null)
            return;

        var upNav = _upButton.navigation;
        upNav.mode = Navigation.Mode.Explicit;
        upNav.selectOnDown = _thisButton;
        _upButton.navigation = upNav;

        var selfNav = _thisButton.navigation;
        selfNav.mode = Navigation.Mode.Explicit;
        selfNav.selectOnUp = _upButton;
        selfNav.selectOnDown = _downButton;
        _thisButton.navigation = selfNav;

        var downNav = _downButton.navigation;
        downNav.mode = Navigation.Mode.Explicit;
        downNav.selectOnUp = _thisButton;
        _downButton.navigation = downNav;
    }

    private void TryManualNavigation()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || _upButton == null || _downButton == null || _thisButton == null)
            return;

        if (!ReInput.isReady)
            return;

        if (_rewired == null)
            _rewired = ReInput.players.GetPlayer(0);
        if (_rewired == null)
            return;

        float verticalAxis = _rewired.GetAxisRaw(MenuVerticalAxis);
        bool movedDown = verticalAxis < -NavigationAxisThreshold
            && _lastVerticalAxis >= -NavigationAxisThreshold;
        bool movedUp = verticalAxis > NavigationAxisThreshold
            && _lastVerticalAxis <= NavigationAxisThreshold;

        GameObject current = eventSystem.currentSelectedGameObject;
        if (_lastNavigationSelected == null)
        {
            _lastNavigationSelected = current;
            _lastVerticalAxis = verticalAxis;
            return;
        }

        EventsButton target = null;
        if (movedDown)
        {
            if (ReferenceEquals(_lastNavigationSelected, _thisButton.gameObject)
                || ReferenceEquals(_lastNavigationSelected, _upButton.gameObject))
            {
                target = ReferenceEquals(_lastNavigationSelected, _thisButton.gameObject)
                    ? _downButton
                    : _thisButton;
            }
        }
        else if (movedUp)
        {
            if (ReferenceEquals(_lastNavigationSelected, _thisButton.gameObject)
                || ReferenceEquals(_lastNavigationSelected, _downButton.gameObject))
            {
                target = ReferenceEquals(_lastNavigationSelected, _thisButton.gameObject)
                    ? _upButton
                    : _thisButton;
            }
        }

        if (target != null && target.IsActive() && target.IsInteractable())
        {
            eventSystem.SetSelectedGameObject(target.gameObject);
            UnityEngine.Debug.Log($"[CustomSettings] DIAG manual navigation direction={(movedDown ? "Down" : "Up")} from=`{(_lastNavigationSelected != null ? _lastNavigationSelected.name : "null")}` to=`{target.name}` selected=`{(eventSystem.currentSelectedGameObject != null ? eventSystem.currentSelectedGameObject.name : "null")}`");
        }

        _lastNavigationSelected = eventSystem.currentSelectedGameObject;
        _lastVerticalAxis = verticalAxis;
    }

    private void UpdateValueText()
    {
        if (_valueText != null)
            _valueText.text = _value ? "ENABLED" : "DISABLED";
    }

    private void RenderSelection()
    {
        // Mirror vanilla SetOptionGameSelected: selection transform active + highlightable text color
        if (_selection != null)
            _selection.SetActive(_selected);
        if (_highlightableText != null)
            _highlightableText.color = _selected
                ? new Color(0.80784315f, 0.84705883f, 0.49803922f) // optionHighligterColor
                : new Color(0.972549f, 0.89411765f, 0.78039217f);  // optionNormalColor
    }
}
