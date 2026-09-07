using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using Gameplay.UI.Others.MenuLogic;
using System.Collections;
using UnityEngine;

namespace Blasphemous.CustomSettings.Components;

/// <summary>
/// Locates the vanilla settings menu structure at runtime.
/// Uses two channels: reflection into <c>OptionsWidget.optionsRoot</c>, and a scene-path lookup.
/// </summary>
internal static class MenuLocator
{
    private const string SelectionChildName = "Selection";

    /// <summary>
    /// Finds the options container (the "Selection" child) for the given vanilla menu target.
    /// Returns null when the menu is not present in the current scene or the structure differs.
    /// </summary>
    internal static Transform FindOptionsSelection(VanillaMenuTarget target)
    {
        // Channel 1: reflection into OptionsWidget.optionsRoot
        Transform root = FindViaOptionsRoot(target);
        if (root == null)
        {
            // Channel 2: scene-path lookup (mirrors vanilla structure Game UI/Content/UI_PAUSE/UI_OPTIONS/...)
            string path = OptionsPath(target);
            if (path != null)
                root = GameObject.Find(path)?.transform;
        }

        if (root == null)
        {
            ModLog.Error($"Failed to locate vanilla settings menu `{target}` — menu not present in current scene");
            return null;
        }

        // The options live under the "Selection" child of each submenu root
        Transform selection = root.Find(SelectionChildName);
        if (selection == null)
        {
            ModLog.Error($"Failed to locate `{SelectionChildName}` under settings menu `{target}`");
            return null;
        }

        ModLog.Info($"Located settings menu `{target}` selection: {selection.name}");
        return selection;
    }

    /// <summary>
    /// Reads <c>OptionsWidget.optionsRoot</c> by reflection and maps a <see cref="VanillaMenuTarget"/> to its menu enum.
    /// </summary>
    private static Transform FindViaOptionsRoot(VanillaMenuTarget target)
    {
        OptionsWidget widget = UnityEngine.Object.FindObjectOfType<OptionsWidget>();
        if (widget == null)
            return null;

        IDictionary dict = TraverseUtils.GetValue<IDictionary>(
            widget,
            "optionsRoot",
            TraverseUtils.TraverseAccessType.Field);
        if (dict == null)
            return null;

        string menuName = MenuEnumName(target);
        foreach (DictionaryEntry entry in dict)
        {
            if (entry.Key.ToString() == menuName)
            {
                Transform root = entry.Value as Transform;
                if (root != null)
                    ModLog.Info($"Located settings menu `{target}` via optionsRoot reflection: {root.name}");
                return root;
            }
        }
        return null;
    }

    /// <summary>
    /// Maps a <see cref="VanillaMenuTarget"/> to the vanilla <c>OptionsWidget.MENU</c> enum member name.
    /// </summary>
    private static string MenuEnumName(VanillaMenuTarget target) => target switch
    {
        VanillaMenuTarget.Game => "GAME",
        VanillaMenuTarget.Video => "VIDEO",
        VanillaMenuTarget.Audio => "AUDIO",
        VanillaMenuTarget.Accessibility => "ACCESSIBILITY",
        VanillaMenuTarget.Extras => "OPTIONS", // fallback: extras handled elsewhere
        _ => null
    };

    /// <summary>
    /// Best-effort scene path for a vanilla settings submenu. Mirrors the GenericElements scene hierarchy.
    /// Note: "Options_Main" is the Options top-level menu (holds the Game/Video/Audio entry buttons);
    /// each submenu lives under its own "Options_&lt;name&gt;" root.
    /// </summary>
    private static string OptionsPath(VanillaMenuTarget target) => target switch
    {
        VanillaMenuTarget.Game => "Game UI/Content/UI_PAUSE/UI_OPTIONS/Background/Options_Game",
        VanillaMenuTarget.Video => "Game UI/Content/UI_PAUSE/UI_OPTIONS/Background/Options_Video",
        VanillaMenuTarget.Audio => "Game UI/Content/UI_PAUSE/UI_OPTIONS/Background/Options_Audio",
        VanillaMenuTarget.Accessibility => "Game UI/Content/UI_PAUSE/UI_OPTIONS/Background/Options_Accessibility",
        _ => null
    };
}
