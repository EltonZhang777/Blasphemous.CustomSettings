using Blasphemous.ModdingAPI;
using System;
using System.Collections.Generic;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Type of a custom settings option control
/// </summary>
public enum OptionType
{
    /// <summary> A boolean on/off switch </summary>
    Toggle,

    /// <summary> A cycling selection between multiple choices </summary>
    Arrow,

    /// <summary> A clickable entry (runs an action, or edits a numeric value) </summary>
    Text
}

/// <summary>
/// A settings option that can be registered into a settings menu
/// </summary>
public sealed class SettingsOption
{
    /// <summary>
    /// Unique id of this option (used to prevent duplicate registrations and to log it)
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display title of this option
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Control type of this option
    /// </summary>
    public OptionType Type { get; set; }

    /// <summary>
    /// Choices for Arrow options (the selected index is the value)
    /// </summary>
    public IList<string> Choices { get; set; }

    /// <summary>
    /// Default value (Toggle: bool / Arrow: int / Text numeric: int)
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// Called whenever the value changes (payload is the new value, e.g. boxed bool? or int?)
    /// </summary>
    public Action<object> OnChange { get; set; }

    /// <summary>
    /// Called when the menu/tab containing this option closes
    /// </summary>
    public Action OnClose { get; set; }

    /// <summary>
    /// The mod that registered this option
    /// </summary>
    public BlasMod OwnerMod { get; internal set; }
}
