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

/* Scripts/Mobiles/Vendors/SBInfo/SBCarpenter.cs
 * ChangeLog
 *  12/25/06, adam
 *      Added the library bookcase
 *  12/22/06, adam
 *      Added the library bookcase, but left it comment out; waiting on the ability to name the library bookcase
 *  08/03/06, Rhiannon
 *		Added display cases
 *	03/20/06, weaver
 *		Added trash barrels.
 *	03/10/05, erlein
 *	    Changed to lower case letters.
 *	03/10/05, erlein
 *		Added Square Graver & Wood Engraving book.
 *  01/28/05
 *		Added Logs
 *  01/23/05, Taran Kain
 *		Added boards.
 *  11/18/04, Jade
 *      Re-enabled the fountain deeds.
 *  11/17/04, Jade
 *      Commented out the fountain deeds, because they are bugged.
 *  11/16/04, Jade
 *      Added new fountain deeds to the inventory.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBCarpenter : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBCarpenter()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {
                    // Jade: Add new fountain deeds (currently commented out because they are bugged)
                    // Adam: waiting on the ability to name the library bookcase
                    Add(new GenericBuyInfo("a library bookcase", typeof(Library), 12204, 20, 0xA97, 0));
                    Add(new GenericBuyInfo("wood engraving", typeof(WoodEngravingBook), 10625, 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("a stone fountain deed", typeof(StoneFountainDeed), 175000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a sandstone fountain deed", typeof(SandstoneFountainDeed), 175000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("square graver", typeof(SquareGraver), 250, 20, 0x10E7, 0));
                    Add(new GenericBuyInfo("trash barrel", typeof(TrashBarrel), 300, 20, 0xE77, 0));
                    Add(new GenericBuyInfo("a tiny display case (east) deed", typeof(DisplayCaseTinyEastAddonDeed), 15000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a tiny display case (south) deed", typeof(DisplayCaseTinySouthAddonDeed), 15000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a small display case (east) deed", typeof(DisplayCaseSmallEastAddonDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a small display case (south) deed", typeof(DisplayCaseSmallSouthAddonDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a narrow display case (east) deed", typeof(DisplayCaseNarrowEastAddonDeed), 50000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a narrow display case (south) deed", typeof(DisplayCaseNarrowSouthAddonDeed), 50000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a medium display case (east) deed", typeof(DisplayCaseMediumEastAddonDeed), 60000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a medium display case (south) deed", typeof(DisplayCaseMediumSouthAddonDeed), 60000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a square display case deed", typeof(DisplayCaseSquareAddonDeed), 75000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a large display case (east) deed", typeof(DisplayCaseLargeEastAddonDeed), 100000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a large display case (south) deed", typeof(DisplayCaseLargeSouthAddonDeed), 100000, 20, 0x14F0, 0));
                }

                Add(new GenericBuyInfo(typeof(Lute), 21, 20, 0xEB3, 0));
                Add(new GenericBuyInfo(typeof(LapHarp), 21, 20, 0xEB2, 0));
                Add(new GenericBuyInfo(typeof(Tambourine), 21, 20, 0xE9D, 0));
                Add(new GenericBuyInfo(typeof(Drums), 21, 20, 0xE9C, 0));
                Add(new GenericBuyInfo(typeof(JointingPlane), 11, 20, 0x1030, 0));
                Add(new GenericBuyInfo(typeof(SmoothingPlane), 10, 20, 0x1032, 0));
                Add(new GenericBuyInfo(typeof(MouldingPlane), 11, 20, 0x102C, 0));
                Add(new GenericBuyInfo(typeof(Hammer), 16, 20, 0x102A, 0));
                Add(new GenericBuyInfo(typeof(Saw), 15, 20, 0x1034, 0));
                Add(new GenericBuyInfo(typeof(DovetailSaw), 12, 20, 0x1028, 0));
                Add(new GenericBuyInfo(typeof(Inshave), 10, 20, 0x10E6, 0));
                Add(new GenericBuyInfo(typeof(Scorp), 10, 20, 0x10E7, 0));
                Add(new GenericBuyInfo(typeof(Froe), 10, 20, 0x10E5, 0));
                Add(new GenericBuyInfo(typeof(DrawKnife), 10, 20, 0x10E4, 0));

                /* Shopkeeper NPCs do not sell any resources (Ingots, Cloth, etc.))
				 * http://www.uoguide.com/Siege_Perilous
				 */
                if (!Core.UOSP)
                    Add(new GenericBuyInfo(typeof(Board)));

                Add(new GenericBuyInfo(typeof(Axle), 2, 20, 0x105B, 0));
                Add(new GenericBuyInfo(typeof(Nails), 3, 20, 0x102E, 0));

                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {
                    Add(new GenericBuyInfo(typeof(Server.Township.Sledgehammer), Utility.Random(230, 260), 20, 0x1439, 0));
                    Add(new GenericBuyInfo(typeof(Server.Township.DemolitionAx), Utility.Random(230, 260), 20, 0x13FB, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback system
                    Add(typeof(Board));
                    Add(typeof(Log));
                }

                if (!Core.UOAI && !Core.UOREN && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(WoodenBox), 7);
                    Add(typeof(SmallCrate), 5);
                    Add(typeof(MediumCrate), 6);
                    Add(typeof(LargeCrate), 7);
                    Add(typeof(WoodenChest), 15);

                    Add(typeof(LargeTable), 10);
                    Add(typeof(Nightstand), 7);
                    Add(typeof(YewWoodTable), 10);

                    Add(typeof(Throne), 24);
                    Add(typeof(WoodenThrone), 6);
                    Add(typeof(Stool), 6);
                    Add(typeof(FootStool), 6);

                    Add(typeof(FancyWoodenChairCushion), 12);
                    Add(typeof(WoodenChairCushion), 10);
                    Add(typeof(WoodenChair), 8);
                    Add(typeof(BambooChair), 6);
                    Add(typeof(WoodenBench), 6);

                    Add(typeof(Saw), 9);
                    Add(typeof(Scorp), 6);
                    Add(typeof(SmoothingPlane), 6);
                    Add(typeof(DrawKnife), 6);
                    Add(typeof(Froe), 6);
                    Add(typeof(Hammer), 14);
                    Add(typeof(Inshave), 6);
                    Add(typeof(JointingPlane), 6);
                    Add(typeof(MouldingPlane), 6);
                    Add(typeof(DovetailSaw), 7);
                    Add(typeof(Board), 1);
                    Add(typeof(Axle), 1);
                    Add(typeof(WoodenShield), 31);
                    Add(typeof(BlackStaff), 24);
                    Add(typeof(GnarledStaff), 12);
                    Add(typeof(QuarterStaff), 15);
                    Add(typeof(ShepherdsCrook), 12);
                    Add(typeof(Club), 13);
                    Add(typeof(Lute), 10);
                    Add(typeof(LapHarp), 10);
                    Add(typeof(Tambourine), 10);
                    Add(typeof(Drums), 10);
                }

            }
        }
    }
}
