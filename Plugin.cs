using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using CaptainDTweaks.Patches;
using HarmonyLib;

namespace CaptainDTweaks;


[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("com.app24.sailwindmoddinghelper", ">=2.0.0")]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "com.captaind54321.tweaks";
    public const string PLUGIN_NAME = "CaptainD's Tweaks";
    public const string PLUGIN_VERSION = "0.1.0";

    internal static ConfigEntry<bool> foodOverflow;

    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        foodOverflow = Config.Bind("Settings","Food Overflow", true, "Makes eating food that would put your hunger above 100% not waste the excess, and instead \"overflow\" your hunger value, and then prevent eating until hunger is below 100% again");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(),PLUGIN_GUID);
    }
}
