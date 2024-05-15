using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using SailwindModdingHelper;

namespace CaptainDTweaks.Fixes;

[HarmonyPatch(typeof(ShipItemCompass))]
internal static class CompassFix {
    [HarmonyPatch("OnLoad")]
    [HarmonyPostfix]
    internal static void FixRotation(ref ShipItemCompass __instance) {
        if(__instance.gameObject.name.Contains("82 compass M")) {
            __instance.inventoryRotation = 180;
            __instance.inventoryRotationX = 270;
        }
    }
}