using System;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CEEliteCombatTweaks;

public class CEEliteCombatTweaksMod : Mod
{
    public CEEliteCombatTweaksMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("WhiteGiverMa.CEEliteCombatTweaks");
        harmony.PatchAll();
        Log.Message("[CE Elite Combat Tweaks] Initialized");
        LongEventHandler.ExecuteWhenFinished(ValidateStatPatches);
    }

    private static void ValidateStatPatches()
    {
        bool aimingDelayCurve = Math.Abs(StatDefOf.AimingDelayFactor.postProcessCurve.Evaluate(0.01f) - 0.01f) < 0.001f;
        bool weaponHandlingUncapped = StatDefOf.ShootingAccuracyPawn.maxValue > 1000f;
        bool aimingAccuracyUncapped = CE_StatDefOf.AimingAccuracy.maxValue > 1000f;

        if (aimingDelayCurve && weaponHandlingUncapped && aimingAccuracyUncapped)
        {
            Log.Message("[CE Elite Combat Tweaks] Stat XML patches active: AimingDelayFactor min=1%, weapon handling uncapped, aiming accuracy uncapped.");
            return;
        }

        Log.Warning(
            "[CE Elite Combat Tweaks] Stat XML patch validation failed. "
            + $"AimingDelayFactor@0.01={StatDefOf.AimingDelayFactor.postProcessCurve.Evaluate(0.01f):0.###}, "
            + $"ShootingAccuracyPawn.maxValue={StatDefOf.ShootingAccuracyPawn.maxValue:0.###}, "
            + $"AimingAccuracy.maxValue={CE_StatDefOf.AimingAccuracy.maxValue:0.###}. "
            + "Load order or CE stat definitions may have changed.");
    }
}
