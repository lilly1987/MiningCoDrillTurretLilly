using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;
using static UnityEngine.UI.Image;

namespace Lilly.DrillTurret
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

        public static Dictionary<Map, List<Building>> dbuildings = new Dictionary<Map, List<Building>>();
        public static Dictionary<Map, List<Designation>> ddesignations = new Dictionary<Map, List<Designation>>();
        public static List<Building> buildings = new List<Building>();
        public static List<Designation> designations = new List<Designation>();

        public static Action<Building> actionSpawnSetup;
        public static Action<Building> actionDeSpawn;
        public static Action<Designation> actionAddDesignation;
        public static Action<Designation> actionRemoveDesignation;

        public static List<Building_DrillTurret>  building_DrillTurrets=new List<Building_DrillTurret>();

        static BuildingCache()
        {
            var parentType = typeof(BuildingCache);

            // 서브 클래스 중 HarmonyPatch가 붙은 것만 필터링
            var nestedPatchTypes = parentType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Any());

            harmony = new Harmony(harmonyId);
            foreach (var patchType in nestedPatchTypes)
            {
                try
                {
                    //harmony.PatchAll();
                    harmony.CreateClassProcessor(patchType).Patch();
                    MyLog.Message($"{harmonyId}/{patchType.Name}/Patch/<color=#00FF00FF>Succ</color>");
                }
                catch (System.Exception e)
                {
                    MyLog.Error($"{harmonyId}/Patch/Fail");
                    MyLog.Error(e.ToString());
                    MyLog.Error($"{harmonyId}/Patch/Fail");
                }
            }

        }

        //public static void DoSettingsWindowContents(Rect inRect, Listing_Standard listing)
        //{
        //    listing.GapLine();
        //    //listing.CheckboxLabeled($"DrillCache 패치".Translate(), ref onPatch, tooltip.Translate());
        //    listing.CheckboxLabeled($"DrillCache Debug", ref DebugMode, ".");
        //}

        //public static void ExposeData()
        //{
        //    //Scribe_Values.Look(ref onPatch, "onPatch", true);
        //    Scribe_Values.Look(ref DebugMode, "DebugMode", false);
        //}

        // Map.FinalizeInit
        // Map.FinalizeLoading 로딩때만
        // MapComponentUtility.MapGenerated
        // MapGenerator.GenerateMap
        // Building.SpawnSetup
        // 순으로 완료됨

        // Dispose
        // AddMap
        // Building.SpawnSetup
        // 맵을 이동할때마다 Building_DrillTurret 다시 생성됨


        [HarmonyPatch(typeof(Game), nameof(Game.AddMap))]// 됨
        public static class Patch_MapGenerator_GenerateMap
        {
            // __instance.Map 는 항상 null
            [HarmonyPrefix]
            public static void AddMap(Map map)//
            {
                MyLog.Message($"Game.AddMap : {map}", print: DebugMode);

                if (!dbuildings.TryGetValue(map, out var list1))
                {
                    list1 = new List<Building>();
                    dbuildings[map] = list1;
                }
                if (!ddesignations.TryGetValue(map, out var list2))
                {
                    list2 = new List<Designation>();
                    ddesignations[map] = list2;
                }
            }
        }

        [HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]// Dispose 다음에 AddMap
        public static class Patch_Map_FinalizeLoading
        {
            // __instance.Map 는 항상 null
            [HarmonyPrefix]
            public static void FinalizeLoading(Map __instance)//
            {
                MyLog.Message($"Map.FinalizeLoading : {__instance}", print: DebugMode);

                if (!dbuildings.TryGetValue(__instance, out var list1))
                {
                    list1 = new List<Building>();
                    dbuildings[__instance] = list1;
                }
                if (!ddesignations.TryGetValue(__instance, out var list2))
                {
                    list2 = new List<Designation>();
                    ddesignations[__instance] = list2;
                }
            }
        }

        [HarmonyPatch(typeof(Map), nameof(Map.Dispose))]// Dispose 다음에 AddMap
        public static class Patch_Map_Dispose
        {
            // __instance.Map 는 항상 null
            [HarmonyPrefix]
            public static void Dispose(Map __instance)//
            {
                MyLog.Message($"Map.Dispose : {__instance}", print: DebugMode);

                dbuildings.Remove(__instance);
                ddesignations.Remove(__instance);
            }
        }


        //[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateMap)
        //    , new System.Type[] { typeof(IntVec3), typeof(MapParent), typeof(MapGeneratorDef), 
        //        typeof(IEnumerable<GenStepWithParams>), typeof(Action<Map>), typeof(bool), typeof(bool)  }
        //    )]// 됨
        //public static class Patch_MapGenerator_GenerateMap
        //{
        //    // __instance.Map 는 항상 null
        //    [HarmonyPrefix]
        //    public static void GenerateMap(Map __result)//
        //    {
        //        MyLog.Warning($"MapGenerator.GenerateMap : {__result}", print: DebugMode);

        //    }
        //}

        [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]// 됨
        public static class Patch_Building_SpawnSetup
        {
            // __instance.Map 는 항상 null
            [HarmonyPostfix]
            public static void SpawnSetup(Building __instance, Map map, bool respawningAfterLoad)//
            {
                
                if ( (__instance.def.mineable || __instance.def.building.IsDeconstructible))
                {
                    MyLog.Message($"Building.SpawnSetup : {map} / {respawningAfterLoad}", print: DebugMode);
                    actionSpawnSetup?.Invoke(__instance);
                    buildings.Add(__instance);
                    if (dbuildings.TryGetValue(map, out var list1))
                    {
                        list1.Add(__instance);
                    }
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
                //MyLog.Warning($"Building.DeSpawn ", print: DebugMode);
                if ((__instance.def.mineable || __instance.def.building.IsDeconstructible))
                {
                    actionDeSpawn?.Invoke(__instance);
                    buildings.Remove(__instance);
                    if (dbuildings.TryGetValue(__instance.Map, out var list1))
                    {
                        list1.Remove(__instance);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.AddDesignation))]
        public static class Patch_DesignationManager_AddDesignation
        {
            [HarmonyPostfix]
            public static void Patch(DesignationManager __instance, Designation newDes)
            {
                MyLog.Message($"AddDesignation/{__instance}/{__instance.map}/{newDes}/{newDes.def}", print: DebugMode);
                if (actionAddDesignation != null
                    && (newDes.def == DesignationDefOf.Mine
                    || newDes.def == DesignationDefOf.Deconstruct && newDes.target.HasThing))
                {
                    actionAddDesignation?.Invoke(newDes);
                    designations.Add(newDes);
                    if (ddesignations.TryGetValue(__instance.map, out var list2))
                    {
                        list2.Add(newDes);
                    }
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
                    designations.Remove(des);
                    if (ddesignations.TryGetValue(__instance.map, out var list2))
                    {
                        list2.Remove(des);
                    }
                }
            }
        }

    }
}
