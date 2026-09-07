using Blasphemous.CustomSettings.Components;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;

namespace Blasphemous.CustomSettings;

/// <summary>
/// The main mod class, providing the custom settings registration API to other mods
/// </summary>
public class CustomSettings : BlasMod
{
    internal CustomSettings() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    /// <summary>
    /// Registers example settings entries in DEBUG builds to verify the registration API surface
    /// </summary>
    protected override void OnRegisterServices(ModServiceProvider provider)
    {
#if DEBUG
        // Example registrations used to verify the registration API surface
        provider.RegisterCustomSettingsOption(
            new SettingsOption { Id = "debug_toggle", Title = "Debug Toggle", Type = OptionType.Toggle, DefaultValue = false },
            SettingsMenuTarget.Vanilla(VanillaMenuTarget.Game));
        provider.RegisterCustomSettingsTab(
            new SettingsTab { Id = "debug_tab", Title = "Debug Tab" },
            SettingsMenuTarget.Vanilla(VanillaMenuTarget.Game));
#endif
    }

    /// <summary>
    /// Logs registration counts on scene load. Actual injection is deferred to the
    /// ShowMenu(GAME) Harmony postfix so the menu is active and laid out.
    /// </summary>
    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        if (SceneHelper.MenuSceneLoaded)
        {
            SettingsMenuRegister.LogRegisteredContents();
        }
    }
}
