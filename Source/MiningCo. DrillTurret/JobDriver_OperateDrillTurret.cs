using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DrillTurretLilly;

public class JobDriver_OperateDrillTurret : JobDriver
{
    private const TargetIndex DrillTurretIndex = TargetIndex.A;

    public override bool TryMakePreToilReservations(bool errorOnFail)
    {
        return pawn.Reserve(TargetThingA, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOn(() => ((Building_DrillTurret)TargetThingA).TargetPosition == IntVec3.Invalid);
        yield return Toils_Goto.GotoCell(DrillTurretIndex, PathEndMode.InteractionCell);
        var operateDrillTurretToil = new Toil
        {
            tickAction = delegate
            {
                var actor = GetActor();
                var operatorEfficiency = actor.skills.GetSkill(SkillDefOf.Mining).Level / 20f;
                ((Building_DrillTurret)TargetThingA).SetOperatorEfficiency(operatorEfficiency);
                GetActor().skills.Learn(SkillDefOf.Mining, 0.11f);
            },
            defaultCompleteMode = ToilCompleteMode.Never
        };
        yield return operateDrillTurretToil;
        yield return Toils_Reserve.Release(DrillTurretIndex);
    }
}