using RimWorld;
using Verse;

namespace DrillTurret;

public class Alert_DrillTurretNotDrilling : Alert
{
    public Alert_DrillTurretNotDrilling()
    {
        defaultLabel = "MCDT.IdleDrill".Translate();
        defaultExplanation = "MCDT.IdleDrillTT".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    public override AlertReport GetReport()
    {
        var maps = Find.Maps;
        foreach (var map in maps)
        {
            foreach (var building in map.listerBuildings.AllBuildingsColonistOfDef(Util_DrillTurret.drillTurretDef))
            {
                if (building is not Building_DrillTurret buildingDrillTurret)
                {
                    continue;
                }

                if (!buildingDrillTurret.targetPosition.IsValid)
                {
                    return AlertReport.CulpritIs(buildingDrillTurret);
                }
            }
        }

        return AlertReport.Inactive;
    }
}