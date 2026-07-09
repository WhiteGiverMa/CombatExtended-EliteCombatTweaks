using CombatExtended;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace CEEliteCombatTweaks;

public static class CombatStatPatches
{
    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), "get_ShootingAccuracy")]
    public static class Patch_ShootingAccuracy
    {
        static void Postfix(Verb_LaunchProjectileCE __instance, ref float __result)
        {
            __result = CombatStatCurves.EffectiveWeaponHandling(CombatStatCurves.RawWeaponHandling(__instance));
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), "get_AimingAccuracy")]
    public static class Patch_AimingAccuracy
    {
        static void Postfix(Verb_LaunchProjectileCE __instance, ref float __result)
        {
            __result = CombatStatCurves.EffectiveAimingAccuracy(CombatStatCurves.RawAimingAccuracy(__instance));
        }
    }

    [HarmonyPatch(typeof(ShiftVecReport), "get_accuracyFactor")]
    public static class Patch_AccuracyFactor
    {
        static void Postfix(ref float __result)
        {
            __result = CombatStatCurves.NonNegativeFiniteOrZero(__result);
        }
    }

    [HarmonyPatch(typeof(ShiftVecReport), "get_visibilityShift")]
    public static class Patch_VisibilityShift
    {
        static void Postfix(ref float __result)
        {
            __result = CombatStatCurves.NonNegativeFiniteOrZero(__result);
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), nameof(Verb_LaunchProjectileCE.ShiftVecReportFor), typeof(LocalTargetInfo), typeof(IntVec3))]
    public static class Patch_ShiftVecReportFor_Local
    {
        static void Postfix(ShiftVecReport __result)
        {
            ApplyExtraAimingAccuracy(__result);
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), nameof(Verb_LaunchProjectileCE.ShiftVecReportFor), typeof(GlobalTargetInfo))]
    public static class Patch_ShiftVecReportFor_Global
    {
        static void Postfix(ShiftVecReport __result)
        {
            ApplyExtraAimingAccuracy(__result);
        }
    }

    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldown), typeof(Verb), typeof(Pawn))]
    public static class Patch_AdjustedCooldown
    {
        [HarmonyPriority(Priority.Low)]
        static void Postfix(Verb ownerVerb, Pawn attacker, ref float __result)
        {
            __result = CombatStatCurves.ApplyEliteRangedCooldown(__result, ownerVerb, attacker);
        }
    }

    private static void ApplyExtraAimingAccuracy(ShiftVecReport report)
    {
        if (report == null)
            return;

        float multiplier = CombatStatCurves.ExtraAimingSpreadMultiplier(report.aimingAccuracy);
        report.spreadDegrees *= multiplier;
        report.swayDegrees *= multiplier;
    }
}
