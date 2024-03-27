using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Oculus;

namespace CaptainDTweaks.Patches;

[HarmonyPatch(typeof(OVRPlayerController))]
internal static class SprintPatch {
    [HarmonyPatch("UpdateMovement")]
    [HarmonyPostfix]
    public static void Patch(ref bool ___running, ref CharacterController ___Controller) {
        var control = ___Controller;
        if (Plugin.holdSprint.Value) {
            if (control.isGrounded) {
                ___running = GameInput.GetKey(InputName.Run);
            } else {
                if (GameInput.GetKeyDown(InputName.Run)) {
                    ___running = !___running;
                }
            }
        }
    }

}
