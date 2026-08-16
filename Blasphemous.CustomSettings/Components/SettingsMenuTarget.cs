using UnityEngine;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// A vanilla settings submenu that can be used as a mount target
/// </summary>
public enum VanillaMenuTarget
{
    /// <summary> The vanilla GAME settings submenu </summary>
    Game,

    /// <summary> The vanilla VIDEO settings submenu </summary>
    Video,

    /// <summary> The vanilla AUDIO settings submenu </summary>
    Audio,

    /// <summary> The vanilla ACCESSIBILITY settings submenu </summary>
    Accessibility,

    /// <summary> The vanilla Extras menu (main menu "extra content") </summary>
    Extras
}

/// <summary>
/// Where a registered option/tab gets mounted: either a vanilla settings submenu, or an arbitrary transform (e.g. inside another mod's tab, for nesting)
/// </summary>
public sealed class SettingsMenuTarget
{
    /// <summary>
    /// The vanilla submenu this target points at, when it was created via <see cref="Vanilla(VanillaMenuTarget)"/>
    /// </summary>
    public VanillaMenuTarget? VanillaMenu { get; }

    /// <summary>
    /// The custom transform this target points at, when it was created via <see cref="Custom(Transform)"/>
    /// </summary>
    public Transform CustomTransform { get; }

    private SettingsMenuTarget(VanillaMenuTarget? vanillaMenu, Transform customTransform)
    {
        VanillaMenu = vanillaMenu;
        CustomTransform = customTransform;
    }

    /// <summary>
    /// Creates a target pointing at a vanilla settings submenu
    /// </summary>
    public static SettingsMenuTarget Vanilla(VanillaMenuTarget menu) => new(menu, null);

    /// <summary>
    /// Creates a target pointing at an arbitrary transform (for nested mounting)
    /// </summary>
    public static SettingsMenuTarget Custom(Transform transform) => new(null, transform);
}
