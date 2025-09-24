using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Lilly
{
    
    public class DrillTurretMod : Mod
    {
        public static DrillTurretMod self;
        public static DrillTurretSettings settings;

        public DrillTurretMod(ModContentPack content) : base(content)
        {
            self = this;
            settings = GetSettings<DrillTurretSettings>();// 주의. MainSettings의 patch가 먼저 실행됨      
        }

        Vector2 scrollPosition;
        string tmp;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            var rect = new Rect(0, 0, inRect.width - 16, 1000);

            Widgets.BeginScrollView(inRect, ref scrollPosition, rect);

            Listing_Standard listing = new Listing_Standard();

            listing.Begin(rect);

            listing.GapLine();

            // ---------

            listing.CheckboxLabeled($"Debug", ref DrillTurretSettings.onDebug);

            listing.CheckboxLabeled($"시야제한 적용", ref DrillTurretSettings.onSight);

            listing.Label("채굴 데미지 배율".Translate(), tipSignal: ".".Translate());
            tmp = DrillTurretSettings.DamageMultiple.ToString();
            listing.TextFieldNumeric(ref DrillTurretSettings.DamageMultiple, ref tmp);

            // ---------

            listing.GapLine();

            listing.End();

            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
        {
            return "Drill Turret Mod Lilly".Translate();
        }

    }


    public class DrillTurretSettings : ModSettings
    {
        public static bool onDebug = true;
        public static bool onSight = false;
        public static float DamageMultiple = 100f;
        //public static bool ASAITog = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref onDebug, "onDebug", false);
            Scribe_Values.Look(ref onSight, "onSight", false);
            Scribe_Values.Look(ref DamageMultiple, "DamageMultiple", 100f);
            //Scribe_Values.Look(ref ASAITog, "ASAITog", true);
        }
    }
}
