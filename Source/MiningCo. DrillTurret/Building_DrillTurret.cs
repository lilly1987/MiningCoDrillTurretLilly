using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DrillTurret;

[StaticConstructorOnStartup]
internal class Building_DrillTurret : Building
{
    public enum MiningMode
    {
        Ores,
        Rocks,
        OresAndRocks
    }

    public const int updatePeriodInTicks = 30;

    public const int drillPeriodInTicks = 30;

    public static readonly Material turretTopOnTexture = MaterialPool.MatFrom("Things/Building/DrillTurret_On");

    public static Material turretTopOffTexture = MaterialPool.MatFrom("Things/Building/DrillTurret_Off");

    public static readonly Material laserBeamTexture =
        MaterialPool.MatFrom("Effects/DrillTurret_LaserBeam", ShaderDatabase.Transparent);

    public static Material targetLineTexture =
        MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 1f, 1f));

    public bool designatedOnly;

    public int drillDamageAmount;

    public int drillEfficiencyInPercent;

    public bool isManned;

    public Matrix4x4 laserBeamMatrix;

    public Vector3 laserBeamScale = new Vector3(1f, 1f, 1f);

    public Effecter laserDrillEffecter;

    public Sustainer laserDrillSoundSustainer = null;

    public MiningMode miningMode = MiningMode.OresAndRocks;

    public int nextDrillTick = 0;

    public int nextUpdateTick;

    public float operatorEfficiency;

    public CompPowerTrader powerComp;

    public IntVec3 targetPosition = IntVec3.Invalid;

    public Matrix4x4 turretTopMatrix;

    public float turretTopRotation;

    public Vector3 turretTopScale = new Vector3(4f, 1f, 4f);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        if (!respawningAfterLoad)
        {
            nextUpdateTick = Find.TickManager.TicksGame + Rand.RangeInclusive(0, 30);
            turretTopRotation = Rotation.AsAngle;
        }

        powerComp.powerStoppedAction = OnPoweredOff;
        turretTopMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, turretTopRotation.ToQuat(), turretTopScale);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        ResetTarget();
    }

    public void ResetTarget()
    {
        targetPosition = IntVec3.Invalid;
        StopLaserDrillEffecter();
        drillEfficiencyInPercent = 0;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref targetPosition, "targetPosition");
        Scribe_Values.Look(ref miningMode, "MiningMode");
        Scribe_Values.Look(ref turretTopRotation, "turretTopRotation");
        Scribe_Values.Look(ref designatedOnly, "designatedOnly");
    }

    public void SetOperatorEfficiency(float efficiency)
    {
        isManned = true;
        operatorEfficiency = efficiency;
    }

    public float ComputeDrillEfficiency()
    {
        var num = 0.25f;
        if (isManned)
        {
            isManned = false;
            num += 0.5f * operatorEfficiency;
        }

        var isFinished = Util_DrillTurret.researchDrillTurretEfficientDrillingDef.IsFinished;
        if (isFinished)
        {
            num += 0.25f;
        }

        return Mathf.Clamp01(num);
    }

    public override void Tick()
    {
        base.Tick();
        if (!powerComp.PowerOn)
        {
            return;
        }

        if (Find.TickManager.TicksGame >= nextUpdateTick)
        {
            nextUpdateTick = Find.TickManager.TicksGame + updatePeriodInTicks;
            if (targetPosition.IsValid)
            {
                if (!IsValidTargetAt(targetPosition))
                {
                    ResetTarget();
                }
            }

            if (!targetPosition.IsValid)
            {
                LookForNewTarget(out targetPosition);
            }

            var num = ComputeDrillEfficiency();
            drillEfficiencyInPercent = Mathf.RoundToInt(Mathf.Clamp(num * 100f, 0f, 100f));
            drillDamageAmount = Mathf.CeilToInt(Mathf.Lerp(0f, 100f, num));
            if (targetPosition.IsValid)
            {
                DrillRock();
            }
        }

        var isValid3 = targetPosition.IsValid;
        if (isValid3)
        {
            StartOrMaintainLaserDrillEffecter();
        }

        ComputeDrawingParameters();
    }

    public void OnPoweredOff()
    {
        ResetTarget();
    }

    public void LookForNewTarget(out IntVec3 newTargetPosition)
    {
        newTargetPosition = IntVec3.Invalid;
        foreach (var intVec in GenRadial.RadialCellsAround(Position, def.specialDisplayRadius, false).InRandomOrder())
        {
            if (!IsValidTargetAt(intVec))
            {
                continue;
            }

            newTargetPosition = intVec;
            break;
        }

        if (newTargetPosition.IsValid)
        {
            turretTopRotation = Mathf.Repeat((targetPosition.ToVector3Shifted() - this.TrueCenter()).AngleFlat(), 360f);
        }
    }

    public bool IsValidTargetAt(IntVec3 position)
    {
        if (!GenSight.LineOfSight(Position, position, Map, false))
        {
            return false;
        }

        if (designatedOnly && Map.designationManager.DesignationAt(position, DesignationDefOf.Mine) == null)
        {
            return false;
        }

        var edifice = position.GetEdifice(Map);
        if (edifice == null || !edifice.def.mineable)
        {
            return false;
        }

        if (edifice.def.building.isResourceRock)
        {
            return miningMode is (MiningMode.Ores or MiningMode.OresAndRocks);
        }

        return miningMode is (MiningMode.Rocks or MiningMode.OresAndRocks);
    }

    public bool IsValidTargetAtForGizmo(IntVec3 position)
    {
        if (!GenSight.LineOfSight(Position, position, Map, false))
        {
            return false;
        }

        var edifice = position.GetEdifice(Map);
        if (edifice == null || !edifice.def.mineable)
        {
            return false;
        }

        return edifice.def.building.isResourceRock || edifice.def.building.isNaturalRock;
    }

    public void DrillRock()
    {
        var edifice = targetPosition.GetEdifice(Map);
        if (edifice == null)
        {
            return;
        }

        if (edifice.HitPoints > drillDamageAmount)
        {
            edifice.TakeDamage(new DamageInfo(DamageDefOf.Mining, drillDamageAmount));
        }
        else
        {
            if (edifice.def.building.isResourceRock && edifice.def.building.mineableThing != null)
            {
                var num = edifice.def.building.mineableYield;
                if (!Util_DrillTurret.researchDrillTurretEfficientDrillingDef.IsFinished)
                {
                    num = Mathf.RoundToInt(num * 0.75f);
                }

                var thing = ThingMaker.MakeThing(edifice.def.building.mineableThing);
                thing.stackCount = num;
                GenSpawn.Spawn(thing, edifice.Position, Map);
                edifice.Destroy();
            }
            else
            {
                edifice.Destroy(DestroyMode.KillFinalize);
            }
        }

        if (!edifice.DestroyedOrNull())
        {
            return;
        }

        ResetTarget();
        LookForNewTarget(out targetPosition);
    }

    public void StopLaserDrillEffecter()
    {
        if (laserDrillEffecter == null)
        {
            return;
        }

        laserDrillEffecter.Cleanup();
        laserDrillEffecter = null;
    }

    public void StartOrMaintainLaserDrillEffecter()
    {
        if (laserDrillEffecter == null)
        {
            laserDrillEffecter = new Effecter(DefDatabase<EffecterDef>.GetNamed("LaserDrill"));
        }
        else
        {
            laserDrillEffecter.EffectTick(new TargetInfo(targetPosition, Map), new TargetInfo(Position, Map));
        }
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder(base.GetInspectString());
        stringBuilder.AppendLine();
        stringBuilder.Append($"Drill efficiency: {drillEfficiencyInPercent}%");
        return stringBuilder.ToString();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        IList<Gizmo> list = new List<Gizmo>();
        var num = 700000103;
        var command_Action = new Command_Action();
        switch (miningMode)
        {
            case MiningMode.Ores:
                command_Action.defaultLabel = "MCDT.OresOnly".Translate();
                command_Action.defaultDesc = "MCDT.OresOnlyTT".Translate();
                break;
            case MiningMode.Rocks:
                command_Action.defaultLabel = "MCDT.RocksOnly".Translate();
                command_Action.defaultDesc = "MCDT.RocksOnlyTT".Translate();
                break;
            case MiningMode.OresAndRocks:
                command_Action.defaultLabel = "MCDT.OresRocks".Translate();
                command_Action.defaultDesc = "MCDT.OresRocksTT".Translate();
                break;
        }

        command_Action.icon = ContentFinder<Texture2D>.Get("Ui/Commands/CommandButton_SwitchMode");
        command_Action.activateSound = SoundDef.Named("Click");
        command_Action.action = SwitchMiningMode;
        command_Action.groupKey = num + 1;
        list.Add(command_Action);
        list.Add(new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
            defaultLabel = "MCDT.SetTarget".Translate(),
            defaultDesc = "MCDT.SetTargetTT".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = SelectTarget,
            groupKey = num + 6
        });
        list.Add(new Command_Toggle
        {
            icon = ContentFinder<Texture2D>.Get("UI/Designators/Mine"),
            defaultLabel = "MCDT.DesignatedOnly".Translate(),
            defaultDesc = "MCDT.DesignatedOnlyTT".Translate(),
            activateSound = SoundDef.Named("Click"),
            isActive = () => designatedOnly,
            toggleAction = () =>
            {
                designatedOnly = !designatedOnly;
                LookForNewTarget(out targetPosition);
            }
        });
        var gizmos = base.GetGizmos();
        var result = gizmos != null ? gizmos.Concat(list) : list;

        return result;
    }

    public void SwitchMiningMode()
    {
        switch (miningMode)
        {
            case MiningMode.Ores:
                miningMode = MiningMode.Rocks;
                break;
            case MiningMode.Rocks:
                miningMode = MiningMode.OresAndRocks;
                break;
            case MiningMode.OresAndRocks:
                miningMode = MiningMode.Ores;
                break;
        }

        ResetTarget();
    }

    public void SelectTarget()
    {
        var targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            canTargetLocations = true,
            validator = targ =>
                IsValidTargetAtForGizmo(targ.Cell) && targ.Cell.InHorDistOf(Position, def.specialDisplayRadius)
        };
        Find.Targeter.BeginTargeting(targetingParameters, SetForcedTarget, null, null);
    }

    public void SetForcedTarget(LocalTargetInfo forcedTarget)
    {
        targetPosition = forcedTarget.Cell;
        if (Map.designationManager.DesignationAt(forcedTarget.Cell, DesignationDefOf.Mine) == null)
        {
            Map.designationManager.AddDesignation(new Designation(forcedTarget, DesignationDefOf.Mine));
        }

        turretTopRotation = Mathf.Repeat((targetPosition.ToVector3Shifted() - this.TrueCenter()).AngleFlat(), 360f);
        ComputeDrawingParameters();
    }

    public void ComputeDrawingParameters()
    {
        laserBeamScale.x = 0.2f + (0.8f * drillEfficiencyInPercent / 100f);
        var isValid = targetPosition.IsValid;
        if (isValid)
        {
            var a = targetPosition.ToVector3Shifted() - this.TrueCenter();
            a.y = 0f;
            laserBeamScale.z = a.magnitude - 0.8f;
            var b = a / 2f;
            laserBeamMatrix.SetTRS(Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Projectile.AltitudeFor()) + b,
                turretTopRotation.ToQuat(), laserBeamScale);
        }
        else
        {
            laserBeamScale.z = 1.5f;
            var b2 = new Vector3(0f, 0f, laserBeamScale.z / 2f).RotatedBy(turretTopRotation);
            laserBeamMatrix.SetTRS(Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Projectile.AltitudeFor()) + b2,
                turretTopRotation.ToQuat(), laserBeamScale);
        }
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        turretTopMatrix.SetTRS(
            Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Projectile.AltitudeFor()) + Altitudes.AltIncVect,
            turretTopRotation.ToQuat(), turretTopScale);
        Graphics.DrawMesh(MeshPool.plane10, turretTopMatrix, turretTopOnTexture, 0);
        var powerOn = powerComp.PowerOn;
        if (powerOn)
        {
            Graphics.DrawMesh(MeshPool.plane10, laserBeamMatrix, laserBeamTexture, 0);
        }
    }
}