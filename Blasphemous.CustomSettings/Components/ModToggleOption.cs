using Blasphemous.CustomSettings.Components;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Gameplay.UI.Others.Buttons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Runtime behaviour for a cloned custom settings option.
/// Owns the current value, renders it on the option text, handles selection highlight,
/// and fires the owner option's <c>OnChange</c>/<c>OnClose</c> callbacks.
/// </summary>
internal class ModToggleOption : MonoBehaviour
{
    internal static readonly Color NormalOptionColor = new Color32(0x86, 0x76, 0x66, 0xff);
    internal static readonly Color HighlightedOptionColor = new Color32(0xfe, 0xd3, 0x11, 0xff);

    private SettingsOption _owner;
    private Text _valueText;
    private GameObject _selection;
    private Text _highlightableText;
    private Text _titleText;
    private EventsButton _button;

    private bool _toggleValue;
    private int _intValue;
    private bool _selected;
    private GameObject _lastSelected;

    /// <summary>
    /// Initializes this option from its owner and the template's visual parts.
    /// </summary>
    internal void Initialize(SettingsOption owner, Text valueText, GameObject selection, Text highlightableText, Text titleText)
    {
        _owner = owner;
        _valueText = valueText;
        _selection = selection;
        _highlightableText = highlightableText;
        _titleText = titleText;
        if (owner.Type == OptionType.Toggle)
        {
            _toggleValue = owner.DefaultValue is bool b && b;
            owner.CurrentValue = _toggleValue;
        }
        else if (owner.Type == OptionType.Arrow)
        {
            _intValue = NormalizeArrowValue(owner.DefaultValue is int value ? value : 0, owner.Choices);
            owner.CurrentValue = _intValue;
        }
        else if (owner.DefaultValue is int value)
        {
            _intValue = value;
            owner.CurrentValue = _intValue;
        }
        else
        {
            owner.CurrentValue = null;
        }

        IsSelected = false;
        _titleText?.text = owner.Title ?? string.Empty;
        UpdateValueText();
    }

    /// <summary>
    /// Currently selected state (true when the cursor/selection rests on this option)
    /// </summary>
    internal bool IsSelected
    {
        get => _selected;
        set
        {
            _selected = value;
            RenderSelection();
        }
    }

    /// <summary>
    /// Changes the current value in response to a left/right input.
    /// </summary>
    internal void ChangeValue(bool left)
    {
        if (_owner == null)
            return;

        switch (_owner.Type)
        {
            case OptionType.Toggle:
                _toggleValue = !_toggleValue;
                _owner.CurrentValue = _toggleValue;
                break;
            case OptionType.Arrow:
                if (_owner.Choices == null || _owner.Choices.Count == 0)
                    return;
                _intValue = NormalizeArrowValue(_intValue + (left ? -1 : 1), _owner.Choices);
                _owner.CurrentValue = _intValue;
                break;
            case OptionType.Text:
                if (!(_owner.DefaultValue is int))
                    return;
                _intValue += left ? -1 : 1;
                _owner.CurrentValue = _intValue;
                break;
        }

        UpdateValueText();
        _owner.OnChange?.Invoke(_owner.CurrentValue);
    }

    /// <summary>
    /// Handles submit/click. Toggles change on submit; text action options invoke their callback without a value.
    /// </summary>
    internal void Activate()
    {
        if (_owner == null)
            return;

        if (_owner.Type == OptionType.Text && !(_owner.DefaultValue is int))
        {
            _owner.OnChange?.Invoke(null);
            return;
        }

        if (_owner.Type == OptionType.Toggle)
            ChangeValue(false);
    }

    /// <summary>
    /// Fires the owner's close callback (called when the settings menu/tab closes)
    /// </summary>
    internal void NotifyClose() => _owner.OnClose?.Invoke();

    /// <summary>
    /// Disables callbacks copied from the vanilla template and binds this option's click action.
    /// </summary>
    internal void AttachButton(EventsButton button)
    {
        if (button == null)
            return;
        if (_button == button)
            return;

        _button?.onClick.RemoveListener(Activate);
        _button = button;

        int selectedPersistentListeners = DisablePersistentListeners(button.onSelected);
        int clickedPersistentListeners = DisablePersistentListeners(button.onClick);
        int selectActionPersistentListeners = 0;
        MenuButton menuButton = button.GetComponent<MenuButton>();
        if (menuButton != null)
        {
            selectActionPersistentListeners = DisablePersistentListeners(menuButton.OnSelectAction);
            menuButton.textColorDefault = NormalOptionColor;
            menuButton.textColorHighlighted = HighlightedOptionColor;
        }
        button.onClick.AddListener(Activate);

        ModToggleSelectionRelay relay = button.GetComponent<ModToggleSelectionRelay>() ?? button.gameObject.AddComponent<ModToggleSelectionRelay>();
        relay.Bind(this);

        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            SetSelected(true);
        }

        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG sanitized option button={button.name} type={_owner?.Type} selectedPersistent={selectedPersistentListeners} clickedPersistent={clickedPersistentListeners} selectActionPersistent={selectActionPersistentListeners}");
    }

    private static int DisablePersistentListeners(UnityEventBase unityEvent)
    {
        if (unityEvent == null)
            return 0;

        int count = unityEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            unityEvent.SetPersistentListenerState(i, UnityEventCallState.Off);
        return count;
    }

    /// <summary>
    /// Updates both the runtime component and the public registration state.
    /// </summary>
    internal void SetSelected(bool selected)
    {
        IsSelected = selected;
        _owner?.IsSelected = selected;
    }

    private void Update()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        GameObject current = eventSystem.currentSelectedGameObject;
        if (current == _lastSelected)
            return;

        _lastSelected = current;
        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG selected changed to \"{(current != null ? current.name : "null")}\"");
    }

    private void UpdateValueText()
    {
        if (_valueText == null || _owner == null)
            return;

        switch (_owner.Type)
        {
            case OptionType.Toggle:
                _valueText.text = _toggleValue ? "ENABLED" : "DISABLED";
                break;
            case OptionType.Arrow:
                _valueText.text = _owner.Choices != null
                    && _intValue >= 0
                    && _intValue < _owner.Choices.Count
                    ? _owner.Choices[_intValue] ?? string.Empty
                    : string.Empty;
                break;
            case OptionType.Text:
                _valueText.text = _owner.DefaultValue is int ? _intValue.ToString() : string.Empty;
                break;
        }
    }

    private static int NormalizeArrowValue(int value, IList<string> choices)
    {
        if (choices == null || choices.Count == 0)
            return 0;

        value %= choices.Count;
        return value < 0 ? value + choices.Count : value;
    }

    private void RenderSelection()
    {
        _selection?.SetActive(_selected);

        Color color = _selected ? HighlightedOptionColor : NormalOptionColor;
        _highlightableText?.color = color;
        _titleText?.color = color;
    }
}

/// <summary>
/// Relays Unity EventSystem selection callbacks from the EventsButton child to its option root.
/// </summary>
internal sealed class ModToggleSelectionRelay : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private ModToggleOption _owner;

    internal void Bind(ModToggleOption owner)
    {
        _owner = owner;
    }

    public void OnSelect(BaseEventData eventData)
    {
        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG relay select button={gameObject.name} current={(EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : "null")} frame={Time.frameCount}");
        if (_owner != null)
            SettingsMenuInjector.SelectCustomOption(_owner);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ModLogExtensions.DebugIfDebugBuild($"[CustomSettings] DIAG relay deselect button={gameObject.name} current={(EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : "null")} frame={Time.frameCount}");
        _owner?.SetSelected(false);
    }
}
