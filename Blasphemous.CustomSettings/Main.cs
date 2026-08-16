using BepInEx;

namespace Blasphemous.CustomSettings;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "3.0.1")]
internal class Main : BaseUnityPlugin
{
    public static CustomSettings CustomSettings { get; private set; }

    private void Start()
    {
        CustomSettings = new CustomSettings();
    }
}
