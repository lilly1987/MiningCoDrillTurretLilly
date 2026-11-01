using Mlie;
using UnityEngine;
using Verse;

namespace DrillTurret;

public class DrillTurretMod : Mod
{
    public static DrillTurretModSettings Settings;
    private static string currentVersion;
    private string drillDamagePercentBuffer = string.Empty;
    private string fixedDrillDamageBuffer = string.Empty;
    private string drillIntervalTicksBuffer = string.Empty;
    private string targetSearchIntervalTicksBuffer = string.Empty;

    public DrillTurretMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<DrillTurretModSettings>();
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("MCDT.EnableIdlePowerDraw".Translate(), ref Settings.EnableIdlePowerDraw,
            "MCDT.EnableIdlePowerDrawTT".Translate());
        listing.CheckboxLabeled("MCDT.AllowUnmannedDrilling".Translate(), ref Settings.AllowUnmannedDrilling,
            "MCDT.AllowUnmannedDrillingTT".Translate());
        listing.CheckboxLabeled("MCDT.UnlimitedRange".Translate(), ref Settings.UnlimitedRange,
            "MCDT.UnlimitedRangeTT".Translate());
        listing.CheckboxLabeled("MCDT.IgnoreWalls".Translate(), ref Settings.IgnoreWalls,
            "MCDT.IgnoreWallsTT".Translate());
        listing.CheckboxLabeled("MCDT.PrioritizeDesignatedTargets".Translate(),
            ref Settings.PrioritizeDesignatedTargets, "MCDT.PrioritizeDesignatedTargetsTT".Translate());

        listing.Gap();
        listing.CheckboxLabeled("MCDT.UseFixedDrillDamage".Translate(), ref Settings.UseFixedDrillDamage,
            "MCDT.UseFixedDrillDamageTT".Translate());
        if (Settings.UseFixedDrillDamage)
        {
            listing.Label("MCDT.FixedDrillDamage".Translate(Settings.FixedDrillDamage));
            var fixedDamageSlider = (float)Settings.FixedDrillDamage;
            fixedDamageSlider = listing.Slider(fixedDamageSlider, 1f, 1000f);
            Settings.FixedDrillDamage = Mathf.RoundToInt(fixedDamageSlider);
            listing.TextFieldNumeric(ref Settings.FixedDrillDamage, ref fixedDrillDamageBuffer, 80f);
            Settings.FixedDrillDamage = Mathf.Clamp(Settings.FixedDrillDamage, 1, 1000);
        }
        else
        {
            listing.Label("MCDT.DrillDamagePercent".Translate(Settings.DrillDamagePercent));
            var damagePercentSlider = (float)Settings.DrillDamagePercent;
            damagePercentSlider = listing.Slider(damagePercentSlider, 10f, 500f);
            Settings.DrillDamagePercent = Mathf.RoundToInt(damagePercentSlider);
            listing.TextFieldNumeric(ref Settings.DrillDamagePercent, ref drillDamagePercentBuffer, 80f);
            Settings.DrillDamagePercent = Mathf.Clamp(Settings.DrillDamagePercent, 10, 500);
        }

        listing.Gap();
        listing.Label("MCDT.DrillIntervalTicks".Translate(Settings.DrillIntervalTicks));
        var drillIntervalSlider = (float)Settings.DrillIntervalTicks;
        drillIntervalSlider = listing.Slider(drillIntervalSlider, 1f, 600f);
        Settings.DrillIntervalTicks = Mathf.RoundToInt(drillIntervalSlider);
        listing.TextFieldNumeric(ref Settings.DrillIntervalTicks, ref drillIntervalTicksBuffer, 80f);
        Settings.DrillIntervalTicks = Mathf.Clamp(Settings.DrillIntervalTicks, 1, 600);

        listing.Label("MCDT.TargetSearchIntervalTicks".Translate(Settings.TargetSearchIntervalTicks));
        var searchIntervalSlider = (float)Settings.TargetSearchIntervalTicks;
        searchIntervalSlider = listing.Slider(searchIntervalSlider, 1f, 600f);
        Settings.TargetSearchIntervalTicks = Mathf.RoundToInt(searchIntervalSlider);
        listing.TextFieldNumeric(ref Settings.TargetSearchIntervalTicks, ref targetSearchIntervalTicksBuffer, 80f);
        Settings.TargetSearchIntervalTicks = Mathf.Clamp(Settings.TargetSearchIntervalTicks, 1, 600);

        if (currentVersion != null)
        {
            listing.Gap();
            GUI.contentColor = Color.gray;
            listing.Label("MCDT.CurrentModVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listing.End();
        base.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "MiningCo. Drill turret";
    }
}