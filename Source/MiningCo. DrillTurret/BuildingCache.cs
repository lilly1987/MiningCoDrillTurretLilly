using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;
using static UnityEngine.UI.Image;

namespace Lilly
{    public class DistanceComparer : IComparer<IntVec3>
    {
        private readonly IntVec3 origin;

        public DistanceComparer(IntVec3 origin)
        {
            this.origin = origin;
        }

        public int Compare(IntVec3 a, IntVec3 b)
        {
            int distA = a.DistanceToSquared(origin);
            int distB = b.DistanceToSquared(origin);

            // 거리 같으면 위치로 정렬 (중복 방지)
            if (distA == distB)
            {
                if (a.x != b.x) return a.x.CompareTo(b.x);
                if (a.z != b.z) return a.z.CompareTo(b.z);
                return a.y.CompareTo(b.y);
            }

            return distA.CompareTo(distB);
        }
    }

    [StaticConstructorOnStartup]
    public static class BuildingCache
    {
        public static string harmonyId = "Lilly.DrillCache";
        public static Harmony harmony;

        public static bool DebugMode=false;

        public static List<Building> buildings = new List<Building>();
        public static List<Designation> designations = new List<Designation>();

        public static Action<Building> actionSpawnSetup;
        public static Action<Building> actionDeSpawn;
        public static Action<Designation> actionAddDesignation;
        public static Action<Designation> actionRemoveDesignation;

        public static List<Building_DrillTurret>  building_DrillTurrets=new List<Building_DrillTurret>();

        static BuildingCache()
        {
            try
            {
                harmony = new Harmony(harmonyId);
                harmony.PatchAll();
                MyLog.Warning($"{harmonyId}/Patch/<color=#00FF00FF>Succ</color>");
            }
            catch (System.Exception e)
            {
                MyLog.Error($"{harmonyId}/Patch/Fail");
                MyLog.Error(e.ToString());
                MyLog.Error($"{harmonyId}/Patch/Fail");
            }

        }

        public static void DoSettingsWindowContents(Rect inRect, Listing_Standard listing)
        {
            listing.GapLine();
            //listing.CheckboxLabeled($"DrillCache 패치".Translate(), ref onPatch, tooltip.Translate());
            listing.CheckboxLabeled($"DrillCache Debug", ref DebugMode, ".");
        }

        public static void ExposeData()
        {
            //Scribe_Values.Look(ref onPatch, "onPatch", true);
            Scribe_Values.Look(ref DebugMode, "DebugMode", false);
        }

        // Map.FinalizeInit
        // Map.FinalizeLoading 로딩때만
        // MapComponentUtility.MapGenerated
        // MapGenerator.GenerateMap
        // Building.SpawnSetup
        // 순으로 완료됨

        private static void NewMethod(List<Building> mineableBuildings)
        {
            foreach (var b in mineableBuildings)
            {
                actionSpawnSetup?.Invoke(b);
            }

            if (DebugMode)
                foreach (var b in building_DrillTurrets)
                {
                    MyLog.Warning($"Map sortedCells {b.sortedCells.Count} {b.sortedCells.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsMine {b.sortedCellsMine.Count} {b.sortedCellsMine.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsDeconstruct {b.sortedCellsDeconstruct.Count} {b.sortedCellsDeconstruct.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsMine1 {b.sortedCellsMine1.Count} {b.sortedCellsMine1.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsDeconstruct1 {b.sortedCellsDeconstruct1.Count} {b.sortedCellsDeconstruct1.Min}", print: DebugMode);
                }
        }

        [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]// 됨
        public static class Patch_Building_SpawnSetup
        {
            // __instance.Map 는 항상 null
            [HarmonyPostfix]
            public static void SpawnSetup(Building __instance, Map map, bool respawningAfterLoad)//
            {
                MyLog.Warning($"Building.SpawnSetup : {map} / {respawningAfterLoad}", print: DebugMode);
                
                if ( (__instance.def.mineable || __instance.def.building.IsDeconstructible))
                {
                    //MyLog.Warning($"Building.SpawnSetup ok : {map} / {respawningAfterLoad}", print: DebugMode);
                    actionSpawnSetup?.Invoke(__instance);
                    //MyLog.Warning($"Building.SpawnSetup ok2 : {map} / {respawningAfterLoad}", print: DebugMode);
                    buildings.Add(__instance);
                    //MyLog.Warning($"Building.SpawnSetup ok3 : {map} / {respawningAfterLoad}", print: DebugMode);
                }
            }
        }

        [HarmonyPatch(typeof(Building), nameof(Thing.DeSpawn))]
        public static class Patch_Building_DeSpawn
        {
            [HarmonyPrefix]
            public static void Patch(Building __instance)
            {
                //MyLog.Warning($"DeSpawn {__instance} {__instance.Map} {__instance.def.defName}", print: DebugMode);
                if ((__instance.def.mineable || __instance.def.building.IsDeconstructible))
                {
                    actionDeSpawn?.Invoke(__instance);
                    buildings.Remove(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.AddDesignation))]
        public static class Patch_DesignationManager_AddDesignation
        {
            [HarmonyPostfix]
            public static void Patch(DesignationManager __instance, Designation newDes)
            {
                //MyLog.Warning($"AddDesignation/{__instance}/{newDes}/{newDes.def}", print: DebugMode);
                if (actionAddDesignation != null
                    && (newDes.def == DesignationDefOf.Mine
                    || newDes.def == DesignationDefOf.Deconstruct && newDes.target.HasThing))
                {
                    actionAddDesignation?.Invoke(newDes);
                    designations.Add(newDes);
                }
            }
        }

        [HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.RemoveDesignation))]
        public static class Patch_DesignationManager_RemoveDesignation
        {
            [HarmonyPrefix]
            public static void Patch(DesignationManager __instance, Designation des)
            {
                //MyLog.Warning($"RemoveDesignation/{__instance}/{des}/{des.def}", print: DebugMode);
                if (actionRemoveDesignation != null
                    && (des.def == DesignationDefOf.Mine
                    || des.def == DesignationDefOf.Deconstruct && des.target.HasThing))
                {
                    actionRemoveDesignation?.Invoke(des);
                    designations.Add(des);
                }
            }
        }

    }
}
