using System;
using HarmonyLib;
using BepInEx;
using SailwindModdingHelper;
using UnityEngine;

namespace CaptainDTweaks.Patches;

[HarmonyPatch(typeof(CleanableObject))]
internal static class NoDirt {
    [HarmonyPatch("ApplyDailyDirt")]
    public static bool Prefix(CleanableObject __instance, SaveableObject ___saveable, Texture2D ___dirtCoat) {
        if (!___saveable || ___saveable.extraSetting || !GameState.playing)
        {
            MasterPainter.instance.ApplyCoat(__instance, ___dirtCoat, 0.02f * Plugin.dirtReduction.Value);
        }
        return false;
    }
}