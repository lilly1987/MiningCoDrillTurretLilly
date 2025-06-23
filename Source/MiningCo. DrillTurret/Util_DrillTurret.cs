using Verse;

namespace DrillTurret;

public static class Util_DrillTurret
{
    public static ThingDef DrillTurretDef => ThingDef.Named("DrillTurret");

    public static JobDef OperateDrillTurretJobDef => DefDatabase<JobDef>.GetNamed("OperateDrillTurret");

    public static ResearchProjectDef ResearchDrillTurretEfficientDrillingDef =>
        ResearchProjectDef.Named("ResearchDrillTurretEfficientDrilling");
}