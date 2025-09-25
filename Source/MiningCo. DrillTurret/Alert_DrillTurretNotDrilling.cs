using RimWorld;
using Verse;

namespace Lilly.DrillTurret;

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
            foreach (var building in map.listerBuildings.AllBuildingsColonistOfDef(Util_DrillTurret.DrillTurretDef))
            {
                if (building is not Building_DrillTurret buildingDrillTurret)
                {
                    continue;
                }

                if (!buildingDrillTurret.TargetPosition.IsValid)
                {
                    return AlertReport.CulpritIs(buildingDrillTurret);
                }
            }
        }

        return AlertReport.Inactive;
    }
}