using System;
using System.Net.NetworkInformation;
using HarmonyLib;
using SailwindModdingHelper;
using UnityEngine;

namespace CaptainDTweaks.Patches;

[HarmonyPatch(typeof(PlayerNeeds))]
internal static class PlayerNeedsPatch {
    [HarmonyPatch("LateUpdate")]
    public static void Prefix(ref NeedsInfo __state) {
        if (!Plugin.foodOverflow.Value) return;
        // Grab food & water values from base code before they are modified
        __state = new NeedsInfo(PlayerNeeds.food,PlayerNeeds.water);
    }
    [HarmonyPatch("LateUpdate")]
    public static void Postfix(ref NeedsInfo __state) {
        if (!Plugin.foodOverflow.Value) return;
        // the real method did all the math of how much food/water was used this tick, we just need to apply that to the value saved in __state
        if (__state.food < 0f) {
            PlayerNeeds.food = __state.food - (100-PlayerNeeds.food);
        }
        if (__state.water < 0f) {
            PlayerNeeds.water = __state.water - (100-PlayerNeeds.water);
        }
    }

    internal class NeedsInfo {
        public float food;
        public float water;

        public NeedsInfo(float food, float water) { // cap values at 200, and set to -1 if less than 100 to indicate no action needed;
            this.food = food > 100f ? Math.Min(food,200):-1f;
            this.water = water > 100f ? Math.Min(water,200):-1f;
        }
    }
}
[HarmonyPatch(typeof(ShipItemFood))]
internal static class EatingPatch {
    [HarmonyPatch("OnAltHeld")]
    public static bool Prefix() {
        if (!Plugin.foodOverflow.Value) return true;
        return PlayerNeeds.food < 100f;
    }
}
[HarmonyPatch(typeof(ShipItemBottle))]
internal static class DrinkingPatch {
    [HarmonyPatch("OnAltHeld")]
    public static bool Prefix() {
        if (!Plugin.foodOverflow.Value) return true;
        return PlayerNeeds.water < 100f;
    }
}

[HarmonyPatch(typeof(PlayerNeedsUI))]
internal static class UIPatch {
    [HarmonyPatch("UpdateBars")]
    public static void Postfix(ref Transform ___foodBar, ref Transform ___waterBar) {
        if (!Plugin.foodOverflow.Value) return;
        float t = Time.deltaTime * 2f;
        if (Time.timeScale == 0f)
        {
            t = 1f;
        }
        Vector3 localScale = ___waterBar.localScale;
        localScale.x = Mathf.Lerp(localScale.x, Math.Min(PlayerNeeds.water,100) * 0.01f + 0.01f, t);
        ___waterBar.localScale = localScale;
        localScale = ___foodBar.localScale;
        localScale.x = Mathf.Lerp(localScale.x, Math.Min(PlayerNeeds.food,100) * 0.01f + 0.01f, t);
        ___foodBar.localScale = localScale;
    }
}