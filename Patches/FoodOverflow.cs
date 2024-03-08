using System;
using System.Net.NetworkInformation;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SailwindModdingHelper;
using UnityEngine;

namespace CaptainDTweaks.Patches;

internal class NeedsInfo {
    public float food;
    public float water;

    public NeedsInfo(float food, float water) { // cap values at 200, and set to -1 if less than 100 to indicate no action needed;
        this.food = food > 100f ? Math.Min(food,200f):-1f;
        this.water = water > 100f ? Math.Min(water,200f):-1f;
    }
}

[HarmonyPatch(typeof(PlayerNeeds))]
internal static class PlayerNeedsPatch {
    [HarmonyPatch("LateUpdate")]
    public static void Prefix(ref NeedsInfo __state) {
        if (!Plugin.foodOverflow.Value) return;
        // Grab food & water values from base code before they are modified
        __state = new NeedsInfo(PlayerNeeds.food,PlayerNeeds.water);
        PlayerNeeds.food = Math.Min(PlayerNeeds.food,100f);
        PlayerNeeds.water = Math.Min(PlayerNeeds.water,100f);
    }
    [HarmonyPatch("LateUpdate")]
    public static void Postfix(ref NeedsInfo __state) {
        if (!Plugin.foodOverflow.Value) return;
        // the real method did all the math of how much food/water was used this tick, we just need to apply that to the value saved in __state
        if (__state.food > 0f) {
            float deltaFood = 100f-PlayerNeeds.food;
            //Plugin.logger.LogInfo($"Food overflowed, old value {__state.food}, delta {deltaFood}");
            PlayerNeeds.food = __state.food - deltaFood;
        }
        if (__state.water > 0f) {
            float deltaWater = 100f-PlayerNeeds.water;
            //Plugin.logger.LogInfo($"Water overflowed, old value {__state.water}, delta {deltaWater}");
            PlayerNeeds.water = __state.water - deltaWater;
        }
    }
}
[HarmonyPatch(typeof(ShipItemFood))]
internal static class EatingPatch {
    [HarmonyPatch("OnAltHeld")]
    public static bool Prefix() {
        if (!Plugin.foodOverflow.Value) return true;
        return PlayerNeeds.food < 100f; // return false and skip the base method if we're overflowing
    }
}
[HarmonyPatch(typeof(ShipItemBottle))]
internal static class DrinkingPatch {
    [HarmonyPatch("OnAltHeld")]
    public static bool Prefix() {
        if (!Plugin.foodOverflow.Value) return true;
        return PlayerNeeds.water < 100f; // return false and skip the base method if we're overflowing
    }
}

[HarmonyPatch(typeof(PlayerNeedsUI))]
internal static class NeedsUIPatch {
    [HarmonyPatch("UpdateBars")]
    public static void Prefix(ref NeedsInfo __state) {
        __state = new NeedsInfo(PlayerNeeds.food,PlayerNeeds.water); // store the real food/water values in __state
        // and cap them at 100
        PlayerNeeds.food = Math.Min(PlayerNeeds.food,100f);
        PlayerNeeds.water = Math.Min(PlayerNeeds.water,100f);
    }
    
    [HarmonyPatch("UpdateBars")]
    public static void Postfix(ref NeedsInfo __state) {
        // if the values are overflowing, restore them from __state
        if (__state.food > 0f) {
            PlayerNeeds.food = __state.food;
        }
        if (__state.water > 0f) {
            PlayerNeeds.water = __state.water;
        }
    }
}