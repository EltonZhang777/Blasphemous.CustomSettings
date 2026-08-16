using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Registration handler for custom settings options and tabs
/// </summary>
public static class SettingsMenuRegister
{
    internal static readonly List<SettingsOption> registeredOptions = new();
    internal static readonly List<SettingsTab> registeredTabs = new();

    /// <summary>
    /// All registered custom settings options
    /// </summary>
    public static IEnumerable<SettingsOption> RegisteredOptions => registeredOptions;

    /// <summary>
    /// All registered custom settings tabs
    /// </summary>
    public static IEnumerable<SettingsTab> RegisteredTabs => registeredTabs;

    internal static int TotalOptions => registeredOptions.Count;
    internal static int TotalTabs => registeredTabs.Count;

    internal static SettingsOption OptionAtId(string id) => registeredOptions.FirstOrDefault(x => x.Id == id);

    internal static SettingsTab TabAtId(string id) => registeredTabs.FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Registers a custom settings option to be injected into the given mount target
    /// </summary>
    public static void RegisterCustomSettingsOption(
        this ModServiceProvider provider,
        SettingsOption option,
        SettingsMenuTarget target)
    {
        // prevents null registration
        if (provider == null || option == null || target == null)
        {
            ModLog.Error("Failed to register custom settings option: null provider, option or target");
            return;
        }

        // prevents registering without an id
        if (string.IsNullOrEmpty(option.Id))
        {
            ModLog.Error("Failed to register custom settings option: option has no id");
            return;
        }

        // prevents repeated registering
        if (registeredOptions.Any(x => x.Id == option.Id))
        {
            ModLog.Error($"Failed to register custom settings option: duplicate id `{option.Id}`");
            return;
        }

        option.OwnerMod = provider.RegisteringMod;
        registeredOptions.Add(option);
        ModLog.Info($"Registered custom settings option: {option.Id}");
    }

    /// <summary>
    /// Registers a custom settings tab (sub-page), returning the registered tab
    /// </summary>
    public static SettingsTab RegisterCustomSettingsTab(
        this ModServiceProvider provider,
        SettingsTab tab,
        SettingsMenuTarget parent = null)
    {
        // prevents null registration
        if (provider == null || tab == null)
        {
            ModLog.Error("Failed to register custom settings tab: null provider or tab");
            return null;
        }

        // prevents registering without an id
        if (string.IsNullOrEmpty(tab.Id))
        {
            ModLog.Error("Failed to register custom settings tab: tab has no id");
            return null;
        }

        // prevents repeated registering
        if (registeredTabs.Any(x => x.Id == tab.Id))
        {
            ModLog.Error($"Failed to register custom settings tab: duplicate id `{tab.Id}`");
            return null;
        }

        tab.OwnerMod = provider.RegisteringMod;
        registeredTabs.Add(tab);
        ModLog.Info($"Registered custom settings tab: {tab.Id}");
        return tab;
    }

    /// <summary>
    /// Logs the current contents of the registration table (called on scene load)
    /// </summary>
    internal static void LogRegisteredContents()
    {
        ModLog.Info($"Settings menu registrations: {TotalOptions} options, {TotalTabs} tabs");
    }
}
