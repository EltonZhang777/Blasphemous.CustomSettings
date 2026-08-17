using Blasphemous.CustomSettings.Components;
using UnityEngine;
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