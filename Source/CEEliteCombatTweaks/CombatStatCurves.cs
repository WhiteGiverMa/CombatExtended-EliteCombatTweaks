using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace CEEliteCombatTweaks;

public static class CombatStatCurves
{
    private const float HandlingSoftCap = 4.5f;
    private const float HandlingRecoilSafeCeiling = 4.99f;
    private const float AimingSoftCap = 1.5f;
    private const float EliteCooldownMaxReduction = 0.75f;
    private const float EliteCooldownScoreScale = 0.65f;

    public static float RawWeaponHandling(Verb_LaunchProjectileCE verb)
    {
        Thing shooter = verb.Shooter ?? verb.Caster;
        if (shooter == null)
            return HandlingSoftCap;

        // Mirrors CE's private CasterShootingAccuracyValue(Thing): pawns use ShootingAccuracyPawn,
        // turrets and other casters use ShootingAccuracyTurret.
        return shooter is Pawn
            ? shooter.GetStatValue(StatDefOf.ShootingAccuracyPawn)
            : shooter.GetStatValue(StatDefOf.ShootingAccuracyTurret);
    }

    public static float RawAimingAccuracy(Verb_LaunchProjectileCE verb)
    {
        Thing shooter = verb.Shooter ?? verb.Caster;
        return shooter?.GetStatValue(CE_StatDefOf.AimingAccuracy) ?? 1f;
    }

    public static float EffectiveWeaponHandling(float raw)
    {
        raw = FiniteOr(raw, HandlingSoftCap);
        if (raw <= HandlingSoftCap)
            return Mathf.Max(0f, raw);

        float excess = raw - HandlingSoftCap;
        float headroom = HandlingRecoilSafeCeiling - HandlingSoftCap;
        return HandlingSoftCap + headroom * (1f - Mathf.Exp(-excess / 4f));
    }

    public static float EffectiveAimingAccuracy(float raw)
    {
        raw = FiniteOr(raw, 1f);
        if (raw <= AimingSoftCap)
            return Mathf.Max(0f, raw);

        float excess = raw - AimingSoftCap;
        return AimingSoftCap + Mathf.Log(1f + excess) * 0.25f;
    }

    public static float ExtraAimingSpreadMultiplier(float effectiveAimingAccuracy)
    {
        effectiveAimingAccuracy = FiniteOr(effectiveAimingAccuracy, 1f);
        if (effectiveAimingAccuracy <= AimingSoftCap)
            return 1f;

        float excess = effectiveAimingAccuracy - AimingSoftCap;
        return 1f / Mathf.Sqrt(1f + excess * 0.75f);
    }

    public static float ApplyEliteRangedCooldown(float currentCooldown, Verb ownerVerb, Pawn attacker)
    {
        currentCooldown = NonNegativeFiniteOrZero(currentCooldown);
        if (ownerVerb == null || ownerVerb.verbProps == null || ownerVerb.verbProps.IsMeleeAttack || ownerVerb is not Verb_LaunchProjectileCE)
            return currentCooldown;

        float multiplier = EliteCooldownMultiplier(attacker);
        float minimumCooldown = BaseRangedCooldown(ownerVerb) * 0.01f;
        return Mathf.Max(minimumCooldown, currentCooldown * multiplier);
    }

    public static float EliteCooldownMultiplier(Pawn pawn)
    {
        if (pawn?.health?.capacities == null)
            return 1f;

        float score =
            CapacityExcess(pawn, PawnCapacityDefOf.Manipulation) * 0.55f
            + CapacityExcess(pawn, PawnCapacityDefOf.Moving) * 0.30f
            + CapacityExcess(pawn, PawnCapacityDefOf.Breathing) * 0.15f;
        score = NonNegativeFiniteOrZero(score);

        return 1f - EliteCooldownMaxReduction * (1f - Mathf.Exp(-score / EliteCooldownScoreScale));
    }

    public static float NonNegativeFiniteOrZero(float value)
    {
        if (float.IsNaN(value) || float.IsNegativeInfinity(value) || value < 0f)
            return 0f;

        return float.IsPositiveInfinity(value) ? float.MaxValue : value;
    }

    private static float FiniteOr(float value, float fallback)
    {
        if (float.IsNaN(value))
            return fallback;

        if (float.IsPositiveInfinity(value))
            return float.MaxValue;

        if (float.IsNegativeInfinity(value))
            return -float.MaxValue;

        return value;
    }

    private static float CapacityExcess(Pawn pawn, PawnCapacityDef capacity)
    {
        return Mathf.Max(0f, FiniteOr(pawn.health.capacities.GetLevel(capacity), 1f) - 1f);
    }

    private static float BaseRangedCooldown(Verb ownerVerb)
    {
        if (ownerVerb.tool != null)
            return NonNegativeFiniteOrZero(ownerVerb.tool.AdjustedCooldown(ownerVerb.EquipmentSource));

        if (ownerVerb.EquipmentSource != null)
            return NonNegativeFiniteOrZero(ownerVerb.EquipmentSource.GetStatValue(StatDefOf.RangedWeapon_Cooldown));

        return NonNegativeFiniteOrZero(ownerVerb.verbProps.defaultCooldownTime);
    }
}
