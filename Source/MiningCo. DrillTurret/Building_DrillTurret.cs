using HarmonyLib;
using Lilly;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace Lilly;

[StaticConstructorOnStartup]
public class Building_DrillTurret : Building
{
    public const int UpdatePeriodInTicks = 30;

    public const int DrillPeriodInTicks = 30;

    public static readonly Material turretTopOnTexture = MaterialPool.MatFrom("Things/Building/DrillTurret_OnLilly");

    public static Material TurretTopOffTexture = MaterialPool.MatFrom("Things/Building/DrillTurret_OffLilly");

    public static readonly Material laserBeamTexture =
        MaterialPool.MatFrom("Effects/DrillTurret_LaserBeamLilly", ShaderDatabase.Transparent);

    public static Material TargetLineTexture =
        MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 1f, 1f));

    public readonly Vector3 turretTopScale = new(4f, 1f, 4f);

    public bool designatedOnly;

    public int drillDamageAmount;

    public int drillEfficiencyInPercent;

    public bool isManned;

    public Matrix4x4 laserBeamMatrix;

    public Vector3 laserBeamScale = new(1f, 1f, 1f);

    public Effecter laserDrillEffecter;

    public Sustainer LaserDrillSoundSustainer = null;

    public MiningMode miningMode = MiningMode.All;

    public int NextDrillTick = 0;

    public int nextUpdateTick;

    public float operatorEfficiency;

    public CompPowerTrader powerComp;

    public IntVec3 TargetPosition = IntVec3.Invalid;

    public Matrix4x4 turretTopMatrix;

    public float turretTopRotation;

    public SortedSet<IntVec3> sortedCells;
    public SortedSet<IntVec3> sortedCellsMine;
    public SortedSet<IntVec3> sortedCellsDeconstruct;
    public SortedSet<IntVec3> sortedCellsMine1;
    public SortedSet<IntVec3> sortedCellsDeconstruct1;

    //  이거 다음에 FinalizeInit, FinalizeLoading 순으로 실행
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

        BuildingCache.building_DrillTurrets.Add(this);
        sortedCells = new SortedSet<IntVec3>(new DistanceComparer(Position));
        sortedCellsMine = new SortedSet<IntVec3>(new DistanceComparer(Position));
        sortedCellsDeconstruct = new SortedSet<IntVec3>(new DistanceComparer(Position));
        sortedCellsMine1 = new SortedSet<IntVec3>(new DistanceComparer(Position));
        sortedCellsDeconstruct1 = new SortedSet<IntVec3>(new DistanceComparer(Position));
        BuildingCache.actionSpawnSetup += GetSpawnSetup;
        BuildingCache.actionDeSpawn += GetDeSpawn;
        BuildingCache.actionAddDesignation += AddDesignation;
        BuildingCache.actionRemoveDesignation += RemoveDesignation;
        MyLog.Warning($"SpawnSetup", print: DrillTurretSettings.onDebug);
    }

    private void RemoveDesignation(Designation designation)
    {
        if (designation.def == DesignationDefOf.Mine)
        {
            sortedCellsMine1.Remove(designation.target.Cell);
        }
        else if (designation.def == DesignationDefOf.Deconstruct && designation.target.HasThing)
        {
            sortedCellsDeconstruct1.Remove(designation.target.Thing.Position);
        }
    }

    private void AddDesignation(Designation designation)
    {
        if (designation.def == DesignationDefOf.Mine)
        {
            sortedCellsMine1.Add(designation.target.Cell);
        }
        else if (designation.def == DesignationDefOf.Deconstruct && designation.target.HasThing)
        {
            sortedCellsDeconstruct1.Add(designation.target.Thing.Position);
        }
    }

    private void GetDeSpawn(Building building)
    {
        sortedCells.Remove(building.Position);
        if (building.def.mineable)
            sortedCellsMine.Remove(building.Position);
        if (building.def.building.IsDeconstructible)
            sortedCellsDeconstruct.Remove(building.Position);
    }

    private void GetSpawnSetup(Building building)
    {
        sortedCells.Add(building.Position);
        if (building.def.mineable)
            sortedCellsMine.Add(building.Position);
        if (building.def.building.IsDeconstructible)
            sortedCellsDeconstruct.Add(building.Position);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        BuildingCache.actionSpawnSetup -= GetSpawnSetup;
        BuildingCache.actionDeSpawn -= GetDeSpawn;
        BuildingCache.actionAddDesignation -= AddDesignation;
        BuildingCache.actionRemoveDesignation -= RemoveDesignation;
        base.DeSpawn(mode);
        resetTarget();
    }

    public void resetTarget()
    {
        TargetPosition = IntVec3.Invalid;
        stopLaserDrillEffecter();
        drillEfficiencyInPercent = 0;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref TargetPosition, "targetPosition");
        Scribe_Values.Look(ref miningMode, "MiningMode");
        Scribe_Values.Look(ref turretTopRotation, "turretTopRotation");
        Scribe_Values.Look(ref designatedOnly, "designatedOnly");
    }

    public void SetOperatorEfficiency(float efficiency)
    {
        isManned = true;
        operatorEfficiency = efficiency;
    }

    public float computeDrillEfficiency()
    {
        var num = 0.25f;
        if (isManned)
        {
            isManned = false;
            num += 0.5f * operatorEfficiency;
        }

        var isFinished = Util_DrillTurret.ResearchDrillTurretEfficientDrillingDef.IsFinished;
        if (isFinished)
        {
            num += 0.25f;
        }

        return Mathf.Clamp01(num);
    }

    protected override void Tick()
    {
        base.Tick();

        if (!powerComp.PowerOn)
            return;

        if (Find.TickManager.TicksGame >= nextUpdateTick)
        {
            nextUpdateTick = Find.TickManager.TicksGame + UpdatePeriodInTicks;

            if (!TargetPosition.IsValid)
                lookForNewTarget(out TargetPosition);

            if (TargetPosition.IsValid && isValidTargetAt(TargetPosition))
            {
                if (targetDesignationDef == DesignationDefOf.Mine)
                {
                    var num = computeDrillEfficiency();
                    drillEfficiencyInPercent = Mathf.RoundToInt(Mathf.Clamp(num * 100f, 0f, 100f));
                    drillDamageAmount = (int)(Mathf.CeilToInt(Mathf.Lerp(0f, 100f, num) * DrillTurretSettings.DamageMultiple));
                    drillRock();
                }
                else if (targetDesignationDef == DesignationDefOf.Deconstruct)
                {
                    var edifice = TargetPosition.GetEdifice(Map);
                    if (edifice != null)
                    {
                        edifice.Destroy(DestroyMode.Deconstruct);
                        resetTarget();
                        lookForNewTarget(out TargetPosition);
                    }
                    else
                        resetTarget();
                }
                else
                {
                    resetTarget();
                }
                startOrMaintainLaserDrillEffecter();
            }
            else
                resetTarget();
        }

    }

    public void OnPoweredOff()
    {
        resetTarget();
    }

    public DesignationDef targetDesignationDef;

    public void lookForNewTarget(out IntVec3 newTargetPosition)
    {
        newTargetPosition = IntVec3.Invalid;

        if (!newTargetPosition.IsValid && miningMode is MiningMode.Deconstruct or MiningMode.All)
        {
            if (NewMethod(ref newTargetPosition, sortedCellsDeconstruct1)) return;
        }

        if (!newTargetPosition.IsValid && miningMode is MiningMode.Ores or MiningMode.Rocks or MiningMode.All)
        {
            if (NewMethod(ref newTargetPosition, sortedCellsMine1)) return;            
        }

        if (!newTargetPosition.IsValid && !designatedOnly && miningMode is MiningMode.Ores or MiningMode.Rocks or MiningMode.All)
        {
            if (NewMethod(ref newTargetPosition, sortedCellsMine)) return;
        }

        if (!newTargetPosition.IsValid && !designatedOnly && miningMode is MiningMode.Deconstruct or MiningMode.All)
        {
            if (NewMethod(ref newTargetPosition, sortedCellsDeconstruct)) return;
        }

        if (!newTargetPosition.IsValid && !designatedOnly)
        {
            if (NewMethod(ref newTargetPosition, sortedCells)) return;
        }

    }

    private bool NewMethod(ref IntVec3 newTargetPosition,SortedSet<IntVec3>  intVec3s)
    {
        foreach (var cell in intVec3s)
        {
            if (isValidTargetAt(cell))
            {
                newTargetPosition = cell;
                if (newTargetPosition.IsValid)
                {
                    turretTopRotation = Mathf.Repeat((TargetPosition.ToVector3Shifted() - this.TrueCenter()).AngleFlat(), 360f);
                }
                return true;
            }
        }
        return false;
    }



    public bool isValidTargetAt(IntVec3 position)
    {
        if (DrillTurretSettings.onSight && !GenSight.LineOfSight(Position, position, Map, false))
        {
            return false;
        }

        if (!position.InBounds(Map)) { 
            MyLog.Error($"{position} 위치 벗어남. {Map.info.Size}"); 
            return false; // 또는 무시, 로그 출력 등
        }

        var edifice = position.GetEdifice(Map);
        if (edifice == null)
        {
            return false;
        }

        if (designatedOnly
            && Map.designationManager.DesignationAt(position, DesignationDefOf.Mine) == null
            && Map.designationManager.DesignationOn(edifice, DesignationDefOf.Deconstruct) == null
            )
        {
            return false;
        }

        if (edifice.def.mineable)
        {
            targetDesignationDef = DesignationDefOf.Mine;
            if (edifice.def.building.isResourceRock)
            {
                return miningMode is MiningMode.Ores or MiningMode.All;
            }

            return miningMode is MiningMode.Rocks or MiningMode.All;
        }
        else if (edifice.def.building.IsDeconstructible)
        {
            targetDesignationDef = DesignationDefOf.Deconstruct;
            if (Map.designationManager.DesignationOn(edifice, DesignationDefOf.Deconstruct) != null)
            {
                return miningMode is MiningMode.Deconstruct or MiningMode.All;
            }
            else if (edifice.Faction != null && edifice.Faction.HostileTo(Faction.OfPlayer))
            {
                return miningMode is MiningMode.Deconstruct or MiningMode.All;                
            }           
        }
        return false;
    }

    public bool isValidTargetAtForGizmo(IntVec3 position)
    {
        if (!GenSight.LineOfSight(Position, position, Map, false))
        {
            return false;
        }

        if (!position.InBounds(Map))
        {
            MyLog.Error($"{position} 위치 벗어남. {Map.info.Size}");
            return false; // 또는 무시, 로그 출력 등
        }

        var edifice = position.GetEdifice(Map);
        if (edifice == null || !edifice.def.mineable)
        {
            return false;
        }

        return edifice.def.building.isResourceRock || edifice.def.building.isNaturalRock;
    }

    public void drillRock()
    {
        if (!TargetPosition.InBounds(Map))
        {
            MyLog.Error($"{TargetPosition} 위치 벗어남. {Map.info.Size}");
            resetTarget();
            return ; // 또는 무시, 로그 출력 등
        }

        var edifice = TargetPosition.GetEdifice(Map);
        if (edifice == null)
        {
            resetTarget();
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
                if (!Util_DrillTurret.ResearchDrillTurretEfficientDrillingDef.IsFinished)
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

        resetTarget();
        lookForNewTarget(out TargetPosition);
    }

    public void stopLaserDrillEffecter()
    {
        if (laserDrillEffecter == null)
        {
            return;
        }

        laserDrillEffecter.Cleanup();
        laserDrillEffecter = null;
    }

    private int nextEffectTick = 0;
    private const int EffectTickInterval = 5; // 예: 10틱마다 호출


    public void startOrMaintainLaserDrillEffecter()
    {
        if (Find.TickManager.TicksGame < nextEffectTick)
            return;

        nextEffectTick = Find.TickManager.TicksGame + EffectTickInterval;

        if (laserDrillEffecter == null)
        {
            laserDrillEffecter = new Effecter(DefDatabase<EffecterDef>.GetNamed("LaserDrillLilly"));
        }
        else
        {
            laserDrillEffecter.EffectTick(new TargetInfo(TargetPosition, Map), new TargetInfo(Position, Map));
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
        var commandAction = new Command_Action();
        switch (miningMode)
        {
            case MiningMode.Ores:
                commandAction.defaultLabel = "MCDT.OresOnly".Translate();
                commandAction.defaultDesc = "MCDT.OresOnlyTT".Translate();
                break;
            case MiningMode.Rocks:
                commandAction.defaultLabel = "MCDT.RocksOnly".Translate();
                commandAction.defaultDesc = "MCDT.RocksOnlyTT".Translate();
                break;
            case MiningMode.All:
                commandAction.defaultLabel = "MCDT.All".Translate();
                commandAction.defaultDesc = "MCDT.AllTT".Translate();
                break;
            case MiningMode.Deconstruct:
                commandAction.defaultLabel = "MCDT.Deconstruct".Translate();
                commandAction.defaultDesc = "MCDT.DeconstructTT".Translate();
                break;
        }

        commandAction.icon = ContentFinder<Texture2D>.Get("Ui/Commands/CommandButton_SwitchMode");
        commandAction.activateSound = SoundDef.Named("Click");
        commandAction.action = switchMiningMode;
        commandAction.groupKey = num + 1;
        list.Add(commandAction);
        list.Add(new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
            defaultLabel = "MCDT.SetTarget".Translate(),
            defaultDesc = "MCDT.SetTargetTT".Translate(),
            activateSound = SoundDef.Named("Click"),
            action = selectTarget,
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
                lookForNewTarget(out TargetPosition);
            }
        });
        var gizmos = base.GetGizmos();
        var result = gizmos != null ? gizmos.Concat(list) : list;

        return result;
    }

    public void switchMiningMode()
    {
        switch (miningMode)
        {
            case MiningMode.Ores:
                miningMode = MiningMode.Rocks;
                break;
            case MiningMode.Rocks:
                miningMode = MiningMode.All;
                break;
            case MiningMode.All:
                miningMode = MiningMode.Deconstruct;
                break;
            case MiningMode.Deconstruct:
                miningMode = MiningMode.Ores;
                break;
        }

        resetTarget();
    }

    public void selectTarget()
    {
        var targetingParameters = new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            canTargetLocations = true,
            validator = targ =>
                isValidTargetAtForGizmo(targ.Cell) && targ.Cell.InHorDistOf(Position, def.specialDisplayRadius)
        };
        Find.Targeter.BeginTargeting(targetingParameters, setForcedTarget, null, null);
    }

    public void setForcedTarget(LocalTargetInfo forcedTarget)
    {
        TargetPosition = forcedTarget.Cell;
        if (Map.designationManager.DesignationAt(forcedTarget.Cell, DesignationDefOf.Mine) == null)
        {
            Map.designationManager.AddDesignation(new Designation(forcedTarget, DesignationDefOf.Mine));
        }

        turretTopRotation = Mathf.Repeat((TargetPosition.ToVector3Shifted() - this.TrueCenter()).AngleFlat(), 360f);
        computeDrawingParameters();
    }

    public void computeDrawingParameters()
    {
        laserBeamScale.x = 0.2f + (0.8f * drillEfficiencyInPercent / 100f);
        var isValid = TargetPosition.IsValid;
        if (isValid)
        {
            var a = TargetPosition.ToVector3Shifted() - this.TrueCenter();
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
        var powerOn = powerComp.PowerOn;
        if (powerOn)
        {
            Graphics.DrawMesh(MeshPool.plane10, turretTopMatrix, turretTopOnTexture, 0);
            Graphics.DrawMesh(MeshPool.plane10, laserBeamMatrix, laserBeamTexture, 0);
        }
        else
        {
            Graphics.DrawMesh(MeshPool.plane10, turretTopMatrix, TurretTopOffTexture, 0);
        }

        //if (TargetPosition.IsValid)
        //    startOrMaintainLaserDrillEffecter();

        computeDrawingParameters();
    }

    public enum MiningMode
    {
        Ores,
        Rocks,
        All,
        Deconstruct
    }
}