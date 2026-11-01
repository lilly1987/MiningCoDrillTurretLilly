using Verse;
using UnityEngine;

namespace DrillTurret;

public class DrillTurretModSettings : ModSettings
{
    public bool EnableIdlePowerDraw;

    public bool AllowUnmannedDrilling = true;

    public bool UnlimitedRange;

    public bool IgnoreWalls;

    public bool PrioritizeDesignatedTargets = true;

    public bool UseFixedDrillDamage;

    public int DrillDamagePercent = 100;

    public int FixedDrillDamage = 100;

    public int DrillIntervalTicks = 30;

    public int TargetSearchIntervalTicks = 30;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref EnableIdlePowerDraw, "EnableIdlePowerDraw");
        Scribe_Values.Look(ref AllowUnmannedDrilling, "AllowUnmannedDrilling", true);
        Scribe_Values.Look(ref UnlimitedRange, "UnlimitedRange");
        Scribe_Values.Look(ref IgnoreWalls, "IgnoreWalls");
        Scribe_Values.Look(ref PrioritizeDesignatedTargets, "PrioritizeDesignatedTargets", true);
        Scribe_Values.Look(ref UseFixedDrillDamage, "UseFixedDrillDamage");
        Scribe_Values.Look(ref DrillDamagePercent, "DrillDamagePercent", 100);
        Scribe_Values.Look(ref FixedDrillDamage, "FixedDrillDamage", 100);
        Scribe_Values.Look(ref DrillIntervalTicks, "DrillIntervalTicks", 30);
        Scribe_Values.Look(ref TargetSearchIntervalTicks, "TargetSearchIntervalTicks", 30);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            DrillDamagePercent = Mathf.Clamp(DrillDamagePercent, 10, 500);
            FixedDrillDamage = Mathf.Clamp(FixedDrillDamage, 1, 1000);
            DrillIntervalTicks = Mathf.Clamp(DrillIntervalTicks, 1, 600);
            TargetSearchIntervalTicks = Mathf.Clamp(TargetSearchIntervalTicks, 1, 600);
        }
    }
}