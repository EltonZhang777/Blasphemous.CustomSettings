using Blasphemous.ModdingAPI;
using System.Collections.Generic;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// A custom settings tab (sub-page) that can be registered and opened from a settings menu
/// </summary>
public sealed class SettingsTab
{
    /// <summary>
    /// Unique id of this tab (used to prevent duplicate registrations and to log it)
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display title of this tab
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Options initially contained in this tab (can also be appended later)
    /// </summary>
    public IList<SettingsOption> Options { get; set; }

    /// <summary>
    /// The mod that registered this tab
    /// </summary>
    public BlasMod OwnerMod { get; internal set; }
}
