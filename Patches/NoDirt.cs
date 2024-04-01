using System;
using HarmonyLib;
using BepInEx;
using SailwindModdingHelper;
using UnityEngine;

namespace CaptainDTweaks.Patches;

[HarmonyPatch(typeof(CleanableObject))]
internal static class NoDirt {
    [HarmonyPatch("ApplyDailyDirt")]
    public static bool Prefix(CleanableObject __instance) {
        if (Plugin.noDirt.Value) {
            __instance.CleanFully();
        }
        return !Plugin.noDirt.Value;
    }
}