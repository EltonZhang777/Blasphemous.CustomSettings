using Blasphemous.CustomSettings.Components;
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
            SettingsMenuInjector.InjectAll();
    }
}
