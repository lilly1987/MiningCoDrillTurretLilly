using Verse;

namespace DrillTurret;

public static class Util_DrillTurret
{
    public static ThingDef drillTurretDef => ThingDef.Named("DrillTurret");

    public static JobDef operateDrillTurretJobDef => DefDatabase<JobDef>.GetNamed("OperateDrillTurret");

    public static ResearchProjectDef researchDrillTurretEfficientDrillingDef =>
        ResearchProjectDef.Named("ResearchDrillTurretEfficientDrilling");
}