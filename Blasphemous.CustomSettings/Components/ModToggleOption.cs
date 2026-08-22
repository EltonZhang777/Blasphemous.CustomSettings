using Blasphemous.CustomSettings.Components;
using Gameplay.UI.Others.Buttons;
using UnityEngine;
using UnityEngine.Events;
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
    private EventsButton _button;

    private bool _value;
    private bool _selected;
    private GameObject _lastSelected;

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
        set
        {
            _selected = value;
            RenderSelection();
        }
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

    /// <summary>
    /// Disables callbacks copied from the vanilla template and binds this toggle's click action.
    /// </summary>
    internal void AttachButton(EventsButton button)
    {
        if (button == null)
            return;
        if (_button == button)
            return;

        if (_button != null)
            _button.onClick.RemoveListener(ToggleValue);
        _button = button;

        int selectedPersistentListeners = DisablePersistentListeners(button.onSelected);
        int clickedPersistentListeners = DisablePersistentListeners(button.onClick);
        int selectActionPersistentListeners = 0;
        MenuButton menuButton = button.GetComponent<MenuButton>();
        if (menuButton != null)
            selectActionPersistentListeners = DisablePersistentListeners(menuButton.OnSelectAction);
        button.onClick.AddListener(ToggleValue);

        ModToggleSelectionRelay relay = button.GetComponent<ModToggleSelectionRelay>();
        if (relay == null)
            relay = button.gameObject.AddComponent<ModToggleSelectionRelay>();
        relay.Bind(this);

        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            SetSelected(true);
        }

        Debug.Log($"[CustomSettings] DIAG sanitized toggle button={button.name} selectedPersistent={selectedPersistentListeners} clickedPersistent={clickedPersistentListeners} selectActionPersistent={selectActionPersistentListeners}");
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
        if (_owner != null)
            _owner.IsSelected = selected;
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
        Debug.Log($"[CustomSettings] DIAG selected changed to \"{(current != null ? current.name : "null")}\"");
    }

    private void UpdateValueText()
    {
        if (_valueText != null)
            _valueText.text = _value ? "ENABLED" : "DISABLED";
    }

    private void RenderSelection()
    {
        if (_selection != null)
            _selection.SetActive(_selected);
        if (_highlightableText != null)
            _highlightableText.color = _selected
                ? new Color(0.80784315f, 0.84705883f, 0.49803922f)
                : new Color(0.972549f, 0.89411765f, 0.78039217f);
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
        Debug.Log($"[CustomSettings] DIAG relay select button={gameObject.name} current={(EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : "null")} frame={Time.frameCount}");
        if (_owner != null)
            _owner.SetSelected(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log($"[CustomSettings] DIAG relay deselect button={gameObject.name} current={(EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ? EventSystem.current.currentSelectedGameObject.name : "null")} frame={Time.frameCount}");
        if (_owner != null)
            _owner.SetSelected(false);
    }
}
