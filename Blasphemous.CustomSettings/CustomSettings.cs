using Blasphemous.ModdingAPI;

namespace Blasphemous.CustomSettings;

public class CustomSettings : BlasMod
{
    internal CustomSettings() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    protected override void OnInitialize()
    {
        // Perform initialization here
    }
}
