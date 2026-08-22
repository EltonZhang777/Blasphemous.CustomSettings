using Blasphemous.CustomSettings.Components;
using Gameplay.UI;
using Gameplay.UI.Others;
using Gameplay.UI.Others.MenuLogic;
using HarmonyLib;

namespace Blasphemous.CustomSettings.HarmonyPatches;

/// <summary>
/// Injects registered custom toggles into the GAME settings submenu exactly when that
/// submenu is shown. Deferring to ShowMenu ensures the menu is active and (after
/// OptionsWidget.Initialize's ResetMenus) laid out, so injected clones land in the
/// visible, in-use selection container.
/// </summary>
[HarmonyPatch(typeof(OptionsWidget))]
class OptionsWidget_ShowMenu_ToggleInjector_Patch
{
    [HarmonyPatch("ShowMenu")]
    [HarmonyPostfix]
    static void InjectTogglesWhenGameMenuShown(OptionsWidget.MENU menu)
    {
        if (menu == OptionsWidget.MENU.GAME)
        {
            SettingsMenuInjector.EnterGameMenu();
            SettingsMenuInjector.InjectAll();
        }
        else
        {
            SettingsMenuInjector.ExitGameMenu();
        }
    }
}

[HarmonyPatch(typeof(UIController), "HidePauseMenu")]
class UIController_HidePauseMenu_NavigationPatch
{
    [HarmonyPostfix]
    static void RestoreVerticalNavigation()
    {
        SettingsMenuInjector.ExitGameMenu();
    }
}

[HarmonyPatch(typeof(KeepFocus), "Update")]
class KeepFocus_Update_ManualNavigationPatch
{
    [HarmonyPrefix]
    static bool PreserveManualNavigationSelection()
    {
        // KeepFocus only needs to run when the selected object is outside the
        // custom ring. Once the controller selects a ring node, its whitelist
        // would otherwise immediately restore the previous vanilla selection.
        return !SettingsMenuInjector.ShouldBypassKeepFocus();
    }
}
