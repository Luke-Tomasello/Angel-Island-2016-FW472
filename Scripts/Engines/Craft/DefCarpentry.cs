/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Engines/Crafting/DefCarpentry.cs
 * CHANGELOG:
 *	01/02/07, Pix
 *		Changed category for bows to "Ranged Weapons"
 *	01/02/07, Pix
 *		Added SealedBow, SealedCrossbow, SealedHeavyCrossbow
 *	07/30/05, erlein
 *		Randomized whether is fullbookcase, fullbookcase2 or fullbookcase3.
 *	07/25/05, erlein
 *		Made full bookcases craftable.
 *	01/15/05, Pigpen
 *		Added code needed to make clubs craftable.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
// 04,april,2004 edited by Sam edited lines 269-287 for wood working changes 09,may,2004 changed xbow and crossbow back to original values lines 286 & 287
 */

using Server.Items;
using System;

namespace Server.Engines.Craft
{
    public class DefCarpentry : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Carpentry; }
        }

        public override int GumpTitleNumber
        {
            get { return 1044004; } // <CENTER>CARPENTRY MENU</CENTER>
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefCarpentry();

                return m_CraftSystem;
            }
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            // erl: Added for fullbookcases
            if (item.ItemType == typeof(FullBookcase) ||
                item.ItemType == typeof(FullBookcase2) ||
                item.ItemType == typeof(FullBookcase3))
                return 0.3;

            return 0.5; // 50%
        }

        private DefCarpentry()
            : base(1, 1, 1.25)// base( 1, 1, 3.0 )
        {
        }

        public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.

            return 0;
        }

        public override void PlayCraftEffect(Mobile from)
        {
            // no animation
            //if ( from.Body.Type == BodyType.Human && !from.Mounted )
            //	from.Animate( 9, 5, 1, true, false, 0 );

            from.PlaySound(0x23D);
        }

        public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            if (failed)
            {
                if (lostMaterial)
                    return 1044043; // You failed to create the item, and some of your materials are lost.
                else
                    return 1044157; // You failed to create the item, but no materials were lost.
            }
            else
            {
                if (quality == 0)
                    return 502785; // You were barely able to make this item.  It's quality is below average.
                else if (makersMark && quality == 2)
                    return 1044156; // You create an exceptional quality item and affix your maker's mark.
                else if (quality == 2)
                    return 1044155; // You create an exceptional quality item.
                else
                    return 1044154; // You create the item.
            }
        }

        public override void InitCraftList()
        {
            int index = -1;

            // Other Items
            index =
            AddCraft(typeof(Board), 1044294, 1027127, 0.0, 0.0, typeof(Log), 1044466, 1, 1044465);
            SetUseAllRes(index, true);

            AddCraft(typeof(BarrelStaves), 1044294, 1027857, 00.0, 25.0, typeof(Log), 1044041, 5, 1044351);
            AddCraft(typeof(BarrelLid), 1044294, 1027608, 11.0, 36.0, typeof(Log), 1044041, 4, 1044351);
            AddCraft(typeof(ShortMusicStand), 1044294, 1044313, 78.9, 103.9, typeof(Log), 1044041, 15, 1044351);
            AddCraft(typeof(TallMusicStand), 1044294, 1044315, 81.5, 106.5, typeof(Log), 1044041, 20, 1044351);
            AddCraft(typeof(Easle), 1044294, 1044317, 86.8, 111.8, typeof(Log), 1044041, 20, 1044351);

            // Furniture
            AddCraft(typeof(FootStool), 1044291, 1022910, 11.0, 36.0, typeof(Log), 1044041, 9, 1044351);
            AddCraft(typeof(Stool), 1044291, 1022602, 11.0, 36.0, typeof(Log), 1044041, 9, 1044351);
            AddCraft(typeof(BambooChair), 1044291, 1044300, 21.0, 46.0, typeof(Log), 1044041, 13, 1044351);
            AddCraft(typeof(WoodenChair), 1044291, 1044301, 21.0, 46.0, typeof(Log), 1044041, 13, 1044351);
            AddCraft(typeof(FancyWoodenChairCushion), 1044291, 1044302, 42.1, 67.1, typeof(Log), 1044041, 15, 1044351);
            AddCraft(typeof(WoodenChairCushion), 1044291, 1044303, 42.1, 67.1, typeof(Log), 1044041, 13, 1044351);
            AddCraft(typeof(WoodenBench), 1044291, 1022860, 52.6, 77.6, typeof(Log), 1044041, 17, 1044351);
            AddCraft(typeof(WoodenThrone), 1044291, 1044304, 52.6, 77.6, typeof(Log), 1044041, 17, 1044351);
            AddCraft(typeof(Throne), 1044291, 1044305, 73.6, 98.6, typeof(Log), 1044041, 19, 1044351);
            AddCraft(typeof(Nightstand), 1044291, 1044306, 42.1, 67.1, typeof(Log), 1044041, 17, 1044351);
            AddCraft(typeof(WritingTable), 1044291, 1022890, 63.1, 88.1, typeof(Log), 1044041, 17, 1044351);
            AddCraft(typeof(YewWoodTable), 1044291, 1044307, 63.1, 88.1, typeof(Log), 1044041, 23, 1044351);
            AddCraft(typeof(LargeTable), 1044291, 1044308, 84.2, 109.2, typeof(Log), 1044041, 27, 1044351);

            // Containers
            AddCraft(typeof(WoodenBox), 1044292, 1023709, 21.0, 46.0, typeof(Log), 1044041, 10, 1044351);
            AddCraft(typeof(SmallCrate), 1044292, 1044309, 10.0, 35.0, typeof(Log), 1044041, 8, 1044351);
            AddCraft(typeof(MediumCrate), 1044292, 1044310, 31.0, 56.0, typeof(Log), 1044041, 15, 1044351);
            AddCraft(typeof(LargeCrate), 1044292, 1044311, 47.3, 72.3, typeof(Log), 1044041, 18, 1044351);
            AddCraft(typeof(WoodenChest), 1044292, 1023650, 73.6, 98.6, typeof(Log), 1044041, 20, 1044351);
            AddCraft(typeof(EmptyBookcase), 1044292, 1022718, 31.5, 56.5, typeof(Log), 1044041, 25, 1044351);
            AddCraft(typeof(FancyArmoire), 1044292, 1044312, 84.2, 109.2, typeof(Log), 1044041, 35, 1044351);
            AddCraft(typeof(Armoire), 1044292, 1022643, 84.2, 109.2, typeof(Log), 1044041, 35, 1044351);
            index = AddCraft(typeof(Keg), 1044292, 1023711, 57.8, 82.8, typeof(BarrelStaves), 1044288, 3, 1044253);
            AddRes(index, typeof(BarrelHoops), 1044289, 1, 1044253);
            AddRes(index, typeof(BarrelLid), 1044251, 1, 1044253);

            // Full bookcases

            Type tBookcase;
            double dDetermineCase = Utility.RandomDouble();

            if (dDetermineCase <= 0.3)
                tBookcase = typeof(FullBookcase);
            else if (dDetermineCase <= 0.6)
                tBookcase = typeof(FullBookcase2);
            else
                tBookcase = typeof(FullBookcase3);

            index = AddCraft(tBookcase, 1044292, 1022711, 90.0, 107.5, typeof(Log), "Boards or Logs", 25);
            AddSkill(index, SkillName.Inscribe, 90.0, 107.5);

            AddRes(index, typeof(ScribesPen), "Scribe's Pen", 1);
            AddRes(index, typeof(RedBook), "Red Books", 2);

            if (Utility.RandomBool())
                AddRes(index, typeof(TanBook), "Tan Books", 2);
            else
                AddRes(index, typeof(BrownBook), "Brown Books", 2);

            // Staves and Shields
            AddCraft(typeof(ShepherdsCrook), 1044295, 1023713, 78.9, 103.9, typeof(Log), 1044041, 7, 1044351);
            AddCraft(typeof(Club), 1044295, 1025044, 73.6, 98.6, typeof(Log), 1044041, 8, 1044351); // Pigpen, Club is now craftable.
            AddCraft(typeof(QuarterStaff), 1044295, 1023721, 73.6, 98.6, typeof(Log), 1044041, 6, 1044351);
            AddCraft(typeof(GnarledStaff), 1044295, 1025112, 78.9, 103.9, typeof(Log), 1044041, 7, 1044351);
            AddCraft(typeof(WoodenShield), 1044295, 1027034, 52.6, 77.6, typeof(Log), 1044041, 9, 1044351);
            index = AddCraft(typeof(FishingPole), 1044295, 1023519, 68.4, 93.4, typeof(Log), 1044041, 5, 1044351);
            AddSkill(index, SkillName.Tailoring, 40.0, 45.0);
            AddRes(index, typeof(Cloth), 1044286, 5, 1044287);

            // Instruments
            index = AddCraft(typeof(LapHarp), 1044293, 1023762, 63.1, 88.1, typeof(Log), 1044041, 20, 1044351);
            AddSkill(index, SkillName.Musicianship, 45.0, 50.0);
            AddRes(index, typeof(Cloth), 1044286, 10, 1044287);

            index = AddCraft(typeof(Harp), 1044293, 1023761, 78.9, 103.9, typeof(Log), 1044041, 35, 1044351);
            AddSkill(index, SkillName.Musicianship, 45.0, 50.0);
            AddRes(index, typeof(Cloth), 1044286, 15, 1044287);

            index = AddCraft(typeof(Drums), 1044293, 1023740, 57.8, 82.8, typeof(Log), 1044041, 20, 1044351);
            AddSkill(index, SkillName.Musicianship, 45.0, 50.0);
            AddRes(index, typeof(Cloth), 1044286, 10, 1044287);

            index = AddCraft(typeof(Lute), 1044293, 1023763, 68.4, 93.4, typeof(Log), 1044041, 25, 1044351);
            AddSkill(index, SkillName.Musicianship, 45.0, 50.0);
            AddRes(index, typeof(Cloth), 1044286, 10, 1044287);

            index = AddCraft(typeof(Tambourine), 1044293, 1023741, 57.8, 82.8, typeof(Log), 1044041, 15, 1044351);
            AddSkill(index, SkillName.Musicianship, 45.0, 50.0);
            AddRes(index, typeof(Cloth), 1044286, 10, 1044287);

            index = AddCraft(typeof(TambourineTassel), 1044293, 1044320, 57.8, 82.8, typeof(Log), 1044041, 15, 1044351);
            AddSkill(index, SkillName.Musicianship, 45.0, 50.0);
            AddRes(index, typeof(Cloth), 1044286, 15, 1044287);

            // Misc
            index = AddCraft(typeof(SmallBedSouthDeed), 1044290, 1044321, 94.7, 113.1, typeof(Log), 1044041, 100, 1044351);
            AddSkill(index, SkillName.Tailoring, 75.0, 80.0);
            AddRes(index, typeof(Cloth), 1044286, 100, 1044287);
            index = AddCraft(typeof(SmallBedEastDeed), 1044290, 1044322, 94.7, 113.1, typeof(Log), 1044041, 100, 1044351);
            AddSkill(index, SkillName.Tailoring, 75.0, 80.0);
            AddRes(index, typeof(Cloth), 1044286, 100, 1044287);
            index = AddCraft(typeof(LargeBedSouthDeed), 1044290, 1044323, 94.7, 113.1, typeof(Log), 1044041, 150, 1044351);
            AddSkill(index, SkillName.Tailoring, 75.0, 80.0);
            AddRes(index, typeof(Cloth), 1044286, 150, 1044287);
            index = AddCraft(typeof(LargeBedEastDeed), 1044290, 1044324, 94.7, 113.1, typeof(Log), 1044041, 150, 1044351);
            AddSkill(index, SkillName.Tailoring, 75.0, 80.0);
            AddRes(index, typeof(Cloth), 1044286, 150, 1044287);
            index = AddCraft(typeof(PentagramDeed), 1044290, 1044328, 100.0, 125.0, typeof(Log), 1044041, 100, 1044351);
            AddSkill(index, SkillName.Magery, 75.0, 80.0);
            AddRes(index, typeof(IronIngot), 1044036, 40, 1044037);
            index = AddCraft(typeof(AbbatoirDeed), 1044290, 1044329, 100.0, 125.0, typeof(Log), 1044041, 100, 1044351);
            AddSkill(index, SkillName.Magery, 50.0, 55.0);
            AddRes(index, typeof(IronIngot), 1044036, 40, 1044037);

            if (Core.AOS)
            {
                AddCraft(typeof(PlayerBBEast), 1044290, 1062420, 85.0, 110.0, typeof(Log), 1044041, 50, 1044351);
                AddCraft(typeof(PlayerBBSouth), 1044290, 1062421, 85.0, 110.0, typeof(Log), 1044041, 50, 1044351);
            }

            // Blacksmithy
            index = AddCraft(typeof(SmallForgeDeed), 1044296, 1044330, 73.6, 98.6, typeof(Log), 1044041, 5, 1044351);
            AddSkill(index, SkillName.Blacksmith, 75.0, 80.0);
            AddRes(index, typeof(IronIngot), 1044036, 75, 1044037);
            index = AddCraft(typeof(LargeForgeEastDeed), 1044296, 1044331, 78.9, 103.9, typeof(Log), 1044041, 5, 1044351);
            AddSkill(index, SkillName.Blacksmith, 80.0, 85.0);
            AddRes(index, typeof(IronIngot), 1044036, 100, 1044037);
            index = AddCraft(typeof(LargeForgeSouthDeed), 1044296, 1044332, 78.9, 103.9, typeof(Log), 1044041, 5, 1044351);
            AddSkill(index, SkillName.Blacksmith, 80.0, 85.0);
            AddRes(index, typeof(IronIngot), 1044036, 100, 1044037);
            index = AddCraft(typeof(AnvilEastDeed), 1044296, 1044333, 73.6, 98.6, typeof(Log), 1044041, 5, 1044351);
            AddSkill(index, SkillName.Blacksmith, 75.0, 80.0);
            AddRes(index, typeof(IronIngot), 1044036, 150, 1044037);
            index = AddCraft(typeof(AnvilSouthDeed), 1044296, 1044334, 73.6, 98.6, typeof(Log), 1044041, 5, 1044351);
            AddSkill(index, SkillName.Blacksmith, 75.0, 80.0);
            AddRes(index, typeof(IronIngot), 1044036, 150, 1044037);

            // Training
            index = AddCraft(typeof(TrainingDummyEastDeed), 1044297, 1044335, 68.4, 93.4, typeof(Log), 1044041, 55, 1044351);
            AddSkill(index, SkillName.Tailoring, 50.0, 55.0);
            AddRes(index, typeof(Cloth), 1044286, 60, 1044287);
            index = AddCraft(typeof(TrainingDummySouthDeed), 1044297, 1044336, 68.4, 93.4, typeof(Log), 1044041, 55, 1044351);
            AddSkill(index, SkillName.Tailoring, 50.0, 55.0);
            AddRes(index, typeof(Cloth), 1044286, 60, 1044287);
            index = AddCraft(typeof(PickpocketDipEastDeed), 1044297, 1044337, 73.6, 98.6, typeof(Log), 1044041, 65, 1044351);
            AddSkill(index, SkillName.Tailoring, 50.0, 55.0);
            AddRes(index, typeof(Cloth), 1044286, 60, 1044287);
            index = AddCraft(typeof(PickpocketDipSouthDeed), 1044297, 1044338, 73.6, 98.6, typeof(Log), 1044041, 65, 1044351);
            AddSkill(index, SkillName.Tailoring, 50.0, 55.0);
            AddRes(index, typeof(Cloth), 1044286, 60, 1044287);

            // Tailoring
            index = AddCraft(typeof(Dressform), 1044298, 1044339, 63.1, 88.1, typeof(Log), 1044041, 25, 1044351);
            AddSkill(index, SkillName.Tailoring, 65.0, 70.0);
            AddRes(index, typeof(Cloth), 1044286, 10, 1044287);
            index = AddCraft(typeof(SpinningwheelEastDeed), 1044298, 1044341, 73.6, 98.6, typeof(Log), 1044041, 75, 1044351);
            AddSkill(index, SkillName.Tailoring, 65.0, 70.0);
            AddRes(index, typeof(Cloth), 1044286, 25, 1044287);
            index = AddCraft(typeof(SpinningwheelSouthDeed), 1044298, 1044342, 73.6, 98.6, typeof(Log), 1044041, 75, 1044351);
            AddSkill(index, SkillName.Tailoring, 65.0, 70.0);
            AddRes(index, typeof(Cloth), 1044286, 25, 1044287);
            index = AddCraft(typeof(LoomEastDeed), 1044298, 1044343, 84.2, 109.2, typeof(Log), 1044041, 85, 1044351);
            AddSkill(index, SkillName.Tailoring, 65.0, 70.0);
            AddRes(index, typeof(Cloth), 1044286, 25, 1044287);
            index = AddCraft(typeof(LoomSouthDeed), 1044298, 1044344, 84.2, 109.2, typeof(Log), 1044041, 85, 1044351);
            AddSkill(index, SkillName.Tailoring, 65.0, 70.0);
            AddRes(index, typeof(Cloth), 1044286, 25, 1044287);

            // Cooking
            index = AddCraft(typeof(StoneOvenEastDeed), 1044299, 1044345, 68.4, 93.4, typeof(Log), 1044041, 85, 1044351);
            AddSkill(index, SkillName.Tinkering, 50.0, 55.0);
            AddRes(index, typeof(IronIngot), 1044036, 125, 1044037);
            index = AddCraft(typeof(StoneOvenSouthDeed), 1044299, 1044346, 68.4, 93.4, typeof(Log), 1044041, 85, 1044351);
            AddSkill(index, SkillName.Tinkering, 50.0, 55.0);
            AddRes(index, typeof(IronIngot), 1044036, 125, 1044037);
            index = AddCraft(typeof(FlourMillEastDeed), 1044299, 1044347, 94.7, 119.7, typeof(Log), 1044041, 100, 1044351);
            AddSkill(index, SkillName.Tinkering, 50.0, 55.0);
            AddRes(index, typeof(IronIngot), 1044036, 50, 1044037);
            index = AddCraft(typeof(FlourMillSouthDeed), 1044299, 1044348, 94.7, 119.7, typeof(Log), 1044041, 100, 1044351);
            AddSkill(index, SkillName.Tinkering, 50.0, 55.0);
            AddRes(index, typeof(IronIngot), 1044036, 50, 1044037);
            AddCraft(typeof(WaterTroughEastDeed), 1044299, 1044349, 94.7, 119.7, typeof(Log), 1044041, 150, 1044351);
            AddCraft(typeof(WaterTroughSouthDeed), 1044299, 1044350, 94.7, 119.7, typeof(Log), 1044041, 150, 1044351);

            if (Core.UOAI || Core.UOREN)
            {
                // Materials // changed by sam for woodworking
                AddCraft(typeof(Kindling), 1044457, 1023553, 0.0, 00.0, typeof(Log), 1044041, 1, 1044351); // changed by sam for woodworking

                index = AddCraft(typeof(Shaft), 1044457, 1027124, 0.0, 40.0, typeof(Log), 1044041, 1, 1044351); // changed by sam for woodworking
                SetUseAllRes(index, true);

                // Ammunition // changed by sam for woodworking
                index = AddCraft(typeof(Arrow), 1044565, 1023903, 0.0, 40.0, typeof(Shaft), 1044560, 1, 1044561); // changed by sam for woodworking
                AddRes(index, typeof(Feather), 1044562, 1, 1044563); // changed by sam for woodworking
                SetUseAllRes(index, true);

                index = AddCraft(typeof(Bolt), 1044565, 1027163, 0.0, 40.0, typeof(Shaft), 1044560, 1, 1044561); // changed by sam for woodworking
                AddRes(index, typeof(Feather), 1044562, 1, 1044563); // changed by sam for woodworking
                SetUseAllRes(index, true);

                // Weapons // changed by sam for woodworking reedited changes
                AddCraft(typeof(Bow), "Ranged Weapons", "bow", 30.0, 70.0, typeof(Log), "log or board", 7, 1044351); // changed by sam for woodworking
                AddCraft(typeof(Crossbow), "Ranged Weapons", "crossbow", 60.0, 100.0, typeof(Log), "log or board", 7, 1044351); // changed by sam for woodworking
                AddCraft(typeof(HeavyCrossbow), "Ranged Weapons", "heavy crossbow", 80.0, 120.0, typeof(Log), "log or board", 10, 1044351); // changed by sam for woodworking
            }

            //sealed ranged:
            if (Core.UOAI || Core.UOREN)
            {
                index = AddCraft(typeof(SealedBow), "Ranged Weapons", "sealed bow", 30.0, 70.0, typeof(Log), "log or board", 7, 1044351);
                AddRes(index, typeof(Beeswax), "beeswax", 1);
                index = AddCraft(typeof(SealedCrossbow), "Ranged Weapons", "sealed crossbow", 60.0, 100.0, typeof(Log), "log or board", 7, 1044351);
                AddRes(index, typeof(Beeswax), "beeswax", 1);
                index = AddCraft(typeof(SealedHeavyCrossbow), "Ranged Weapons", "sealed heavy crossbow", 80.0, 120.0, typeof(Log), "log or board", 10, 1044351);
                AddRes(index, typeof(Beeswax), "beeswax", 1);
            }


            MarkOption = true;
            Repair = Core.AOS;
        }
    }
}
