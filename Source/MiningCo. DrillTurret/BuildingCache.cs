using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
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

        public static Dictionary<Map, List<Building>> cached = new Dictionary<Map, List<Building>>();
        public static List<Building_DrillTurret> drills = new List<Building_DrillTurret>();
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

        [HarmonyPatch(typeof(Map), "FinalizeInit")]
        public static class Patch_MapFinalizeInit
        {
            [HarmonyPostfix]
            public static void OnMapInit(Map __instance)
            {
                MyLog.Warning($"Map FinalizeInit {__instance} {__instance.Tile}",print: DebugMode);
                var mineableBuildings = __instance.listerThings.AllThings
                    .OfType<Building>()
                    //.Where(b => b.def.mineable)
                    .ToList();

                BuildingCache.cached[__instance] = mineableBuildings;

                foreach (var b in mineableBuildings)
                {
                    actionSpawnSetup?.Invoke(b);
                }

                foreach(var b in building_DrillTurrets)
                {
                    MyLog.Warning($"Map sortedCells {b.sortedCells.Count} {b.sortedCells.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsMine {b.sortedCellsMine.Count} {b.sortedCellsMine.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsDeconstruct {b.sortedCellsDeconstruct.Count} {b.sortedCellsDeconstruct.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsMine1 {b.sortedCellsMine1.Count} {b.sortedCellsMine1.Min}", print: DebugMode);
                    MyLog.Warning($"Map sortedCellsDeconstruct1 {b.sortedCellsDeconstruct1.Count} {b.sortedCellsDeconstruct1.Min}", print: DebugMode);
                }
            }
        }

        [HarmonyPatch(typeof(Map), "FinalizeLoading")]
        public static class Patch_Map_FinalizeLoading
        {
            [HarmonyPostfix]
            public static void OnMapLoaded(Map __instance)
            {
                MyLog.Warning($"Map FinalizeLoading {__instance} {__instance.Tile}", print: DebugMode);
                var mineables = __instance.listerThings.AllThings
                    .OfType<Building>()
                    //.Where(b => b.def.mineable)
                    .ToList();

                BuildingCache.cached[__instance] = mineables;
            }
        }

        [HarmonyPatch(typeof(ThingSetMaker_Meteorite), "Generate", new System.Type[] { typeof(ThingSetMakerParams),typeof(List<Thing>) })]
        public static class Patch_MeteoriteGenerate
        {
            [HarmonyPostfix]
            public static void Patch(ThingSetMakerParams parms, List<Thing> outThings)
            {
                MyLog.Warning($"Meteorite Generated on tile {parms.tile}, things count: {outThings.Count}", print: DebugMode);

                MyLog.Warning($"parms.tile : {parms.tile == -1}", print: DebugMode);
                if (parms.tile == -1)
                {
                    //return;
                }

                Map map = Find.Maps.FirstOrDefault(m => m.Tile == parms.tile);
                MyLog.Warning($"map : {map}", print: DebugMode);
                if (map == null) 
                    return;

                var mineables = outThings
                    .OfType<Building>()
                    //.Where(b => b.def.mineable)
                    .ToList();

                if (mineables.Count == 0) return;

                if (!BuildingCache.cached.TryGetValue(map, out var list))
                    BuildingCache.cached[map] = list = new List<Building>();

                list.AddRange(mineables);
            }
        }

        [HarmonyPatch(typeof(Building), nameof(Thing.SpawnSetup))]// 됨
        public static class Patch_Building_SpawnSetup
        {
            // __instance.Map 는 항상 null
            //[HarmonyPrefix]
            [HarmonyPostfix]
            public static void Patch(Building __instance, Map map)//, bool respawningAfterLoad
            {
                if (map == null)
                    return;

                if (actionSpawnSetup != null && __instance.Position.InBounds(map))
                {
                    //MyLog.Warning($"SpawnSetup1 / {__instance} / {map} / {__instance.def.defName} / {__instance.def.mineable} / {__instance.def.building.IsDeconstructible}", print: DebugMode);
                    if ( (__instance.def.mineable || __instance.def.building.IsDeconstructible))
                        actionSpawnSetup(__instance);
                }

                // 작동 안됨?
                if (
                    //__instance.def.mineable &&
                    BuildingCache.cached.TryGetValue(map, out var list))
                {
                    MyLog.Warning($"SpawnSetup2 / {__instance} / {map} / {__instance.def.defName}", print: DebugMode);
                    list.Add(__instance);
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

                if (__instance.Map == null)
                    return;

                if (actionDeSpawn != null)
                    actionDeSpawn(__instance);

                if (__instance.def.mineable &&
                    BuildingCache.cached.TryGetValue(__instance.Map, out var list))
                {
                    list.Remove(__instance);
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
                    actionAddDesignation(newDes);
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
                    actionRemoveDesignation(des);
            }
        }

        //[HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
        public static class Patch_Thing_DeSpawn
        {
            [HarmonyPrefix]
            public static void Patch(Thing __instance)
            {
                    MyLog.Warning($"Thing DeSpawn {__instance} {__instance.Map} {__instance.def.defName}", print: DebugMode);

                if (__instance.Map == null || !(__instance is Building))
                    return;

                Building building = __instance as Building;
                if (
                    //building.def.mineable &&
                    BuildingCache.cached.TryGetValue(__instance.Map, out var list))
                {
                    list.Remove(building);
                }
            }
        }

    }
}
