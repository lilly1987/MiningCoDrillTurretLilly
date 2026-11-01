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
            sliderWithNumericField(listing, ref Settings.FixedDrillDamage, ref fixedDrillDamageBuffer, 1, 1000);
        }
        else
        {
            listing.Label("MCDT.DrillDamagePercent".Translate(Settings.DrillDamagePercent));
            sliderWithNumericField(listing, ref Settings.DrillDamagePercent, ref drillDamagePercentBuffer, 10, 500);
        }

        listing.Gap();
        listing.Label("MCDT.DrillIntervalTicks".Translate(Settings.DrillIntervalTicks));
        sliderWithNumericField(listing, ref Settings.DrillIntervalTicks, ref drillIntervalTicksBuffer, 1, 600);

        listing.Label("MCDT.TargetSearchIntervalTicks".Translate(Settings.TargetSearchIntervalTicks));
        sliderWithNumericField(listing, ref Settings.TargetSearchIntervalTicks, ref targetSearchIntervalTicksBuffer, 1,
            600);

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

    private static void sliderWithNumericField(Listing_Standard listing, ref int value, ref string buffer, int min,
        int max)
    {
        var rowRect = listing.GetRect(30f);
        var fieldWidth = Mathf.Min(90f, rowRect.width * 0.35f);
        var sliderWidth = Mathf.Max(0f, rowRect.width - fieldWidth - 10f);
        var sliderRect = new Rect(rowRect.x, rowRect.y, sliderWidth, rowRect.height);
        var fieldRect = new Rect(sliderRect.xMax + 10f, rowRect.y, fieldWidth, rowRect.height);

        var previousValue = value;
        var sliderValue = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, value, min, max));
        if (sliderValue != previousValue)
        {
            value = sliderValue;
            buffer = value.ToString();
        }

        Widgets.TextFieldNumeric(fieldRect, ref value, ref buffer, min, max);
        value = Mathf.Clamp(value, min, max);
    }

    public override string SettingsCategory()
    {
        return "MiningCo. Drill turret";
    }
}