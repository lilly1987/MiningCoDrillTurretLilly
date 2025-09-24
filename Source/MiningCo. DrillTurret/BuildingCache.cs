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
{
    [StaticConstructorOnStartup]
    public static class BuildingCache
    {
        public static string harmonyId = "Lilly.DrillCache";
        public static Harmony harmony;

        public static bool DebugMode=false;

        public static Dictionary<Map, List<Building>> cached = new Dictionary<Map, List<Building>>();

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
            [HarmonyPrefix]
            public static void Patch(Building __instance, Map map)//, bool respawningAfterLoad
            {
                if (map == null)
                    return;

                if (
                    //__instance.def.mineable &&
                    BuildingCache.cached.TryGetValue(map, out var list))
                {
                    MyLog.Warning($"SpawnSetup / {__instance} / {map} / {__instance.def.defName}", print: DebugMode);
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
                    MyLog.Warning($"DeSpawn {__instance} {__instance.Map} {__instance.def.defName}", print: DebugMode);

                if (__instance.Map == null)
                    return;

                if (__instance.def.mineable &&
                    BuildingCache.cached.TryGetValue(__instance.Map, out var list))
                {
                    list.Remove(__instance);
                }
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
