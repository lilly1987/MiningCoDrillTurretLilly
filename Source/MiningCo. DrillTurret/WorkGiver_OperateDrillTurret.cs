using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DrillTurret;

public class WorkGiver_OperateDrillTurret : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Util_DrillTurret.drillTurretDef);

    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(Util_DrillTurret.drillTurretDef);
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(Util_DrillTurret.drillTurretDef).Count == 0;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t.Faction != pawn.Faction)
        {
            return false;
        }

        if (t is not Building building)
        {
            return false;
        }

        if (building.IsForbidden(pawn))
        {
            return false;
        }

        if (!pawn.CanReserve(building, 1, -1, null, forced))
        {
            return false;
        }

        return !building.IsBurning() && ((Building_DrillTurret)building).targetPosition.IsValid;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(Util_DrillTurret.operateDrillTurretJobDef, t, 1500, true);
    }
}