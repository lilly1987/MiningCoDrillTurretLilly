using Verse;

namespace DrillTurretLilly;

public static class Util_DrillTurret
{
    public static ThingDef DrillTurretDef => ThingDef.Named("DrillTurretLilly");

    public static JobDef OperateDrillTurretJobDef => DefDatabase<JobDef>.GetNamed("OperateDrillTurretLilly");

    public static ResearchProjectDef ResearchDrillTurretEfficientDrillingDef =>
        ResearchProjectDef.Named("ResearchDrillTurretEfficientDrillingLilly");
}