using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CaptainDTweaks.Patches;
using HarmonyLib;

namespace CaptainDTweaks;


[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("com.app24.sailwindmoddinghelper", "2.0.2")]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "com.captaind54321.tweaks";
    public const string PLUGIN_NAME = "CaptainD's Tweaks";
    public const string PLUGIN_VERSION = "0.1.0";


    internal static Plugin instance;
    internal static ManualLogSource logger;
    internal static ConfigEntry<bool> foodOverflow;
    internal static ConfigEntry<float> dirtReduction;
    internal static ConfigEntry<bool> supplyDemand;

    internal static Harmony harmony;

    private void Awake()
    {
        instance = this;
        logger = Logger;
        // Plugin startup logic
        //logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!"); 
        foodOverflow = Config.Bind("Settings","Food Overflow", true, "Makes eating food that would put your hunger above 100% not waste the excess, and instead \"overflow\" your hunger value, and then prevent eating until hunger is below 100% again");
        dirtReduction = Config.Bind("Settings","Dirt Reduction", 1.0f, new ConfigDescription("Reduces how fast dirt accumulates on boats; 100% = vanilla behavior, 0% = no dirt accumulation", new AcceptableValueRange<float>(0f,1f)));
        supplyDemand = Config.Bind("Settings","Display Demand",true,"Makes the trade book display islands' supply or demand for commodities, as well as price.");
        harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(),PLUGIN_GUID);
    }
    private void OnDestroy() {
        logger.LogInfo($"Destroying and unpatching {PLUGIN_GUID}");
        harmony.UnpatchSelf();
    }
}
