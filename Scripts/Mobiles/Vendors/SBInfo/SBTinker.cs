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

/* Scripts/Mobiles/Vendors/SBInfo/SBTinker.cs
 * ChangeLog
 *  5/23/07, Adam
 *      Add the DoorRekeyingContract. This Contract allows the player to rekey a single door.
 *      (minor gold sink)
 *  09/08/05, erlein
 *     Added EtchingKit, EtchingBook.
 *  01/28/05 TK
 *		Added ore colors, logs.
 *  01/23/05, Taran Kain
 *		Added boards, all nine ingots.
 *	01/18/05, Pigpen
 *		Added hatchet to list of items for sale.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBTinker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBTinker()
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
                    Add(new GenericBuyInfo("metal etching", typeof(EtchingBook), 10625, 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("metal etching kit", typeof(EtchingKit), 250, 20, 0x1EB8, 0));
                    Add(new GenericBuyInfo(typeof(DoorRekeyingContract), 520, 20, 0x14F0, 0));  // minor gold sink
                }

                Add(new GenericBuyInfo(typeof(Drums), 50, 20, 0x0E9C, 0));
                Add(new GenericBuyInfo(typeof(Tambourine), 60, 20, 0x0E9E, 0));
                Add(new GenericBuyInfo(typeof(LapHarp), 30, 20, 0x0EB2, 0));
                Add(new GenericBuyInfo(typeof(Lute), 40, 20, 0x0EB3, 0));

                Add(new GenericBuyInfo(typeof(Shovel), 12, 20, 0xF39, 0));
                Add(new GenericBuyInfo(typeof(SewingKit), 3, 20, 0xF9D, 0));
                Add(new GenericBuyInfo(typeof(Scissors), 13, 20, 0xF9F, 0));
                Add(new GenericBuyInfo(typeof(Tongs), 3, 20, 0xFBB, 0));
                Add(new GenericBuyInfo(typeof(Key), 3, 20, 0x100E, 0));

                Add(new GenericBuyInfo(typeof(DovetailSaw), 14, 20, 0x1028, 0));
                Add(new GenericBuyInfo(typeof(MouldingPlane), 13, 20, 0x102C, 0));
                Add(new GenericBuyInfo(typeof(Nails), 3, 20, 0x102E, 0));
                Add(new GenericBuyInfo(typeof(JointingPlane), 13, 20, 0x1030, 0));
                Add(new GenericBuyInfo(typeof(SmoothingPlane), 12, 20, 0x1032, 0));
                Add(new GenericBuyInfo(typeof(Saw), 18, 20, 0x1034, 0));

                Add(new GenericBuyInfo(typeof(Clock), 22, 20, 0x104B, 0));
                Add(new GenericBuyInfo(typeof(ClockParts), 3, 20, 0x104F, 0));
                Add(new GenericBuyInfo(typeof(AxleGears), 3, 20, 0x1051, 0));
                Add(new GenericBuyInfo(typeof(Gears), 2, 20, 0x1053, 0));
                Add(new GenericBuyInfo(typeof(Hinge), 2, 20, 0x1055, 0));
                Add(new GenericBuyInfo(typeof(Sextant), 25, 20, 0x1057, 0));
                Add(new GenericBuyInfo(typeof(SextantParts), 5, 20, 0x1059, 0));
                Add(new GenericBuyInfo(typeof(Axle), 2, 20, 0x105B, 0));
                Add(new GenericBuyInfo(typeof(Springs), 3, 20, 0x105D, 0));

                Add(new GenericBuyInfo(typeof(DrawKnife), 12, 20, 0x10E4, 0));
                Add(new GenericBuyInfo(typeof(Froe), 12, 20, 0x10E5, 0));
                Add(new GenericBuyInfo(typeof(Inshave), 12, 20, 0x10E6, 0));
                Add(new GenericBuyInfo(typeof(Scorp), 12, 20, 0x10E7, 0));

                Add(new GenericBuyInfo(typeof(Lockpick), 12, 20, 0x14FC, 0));
                Add(new GenericBuyInfo(typeof(TinkerTools), 30, 20, 0x1EB8, 0));

                Add(new GenericBuyInfo(typeof(Pickaxe), 32, 20, 0xE86, 0));
                // TODO: Sledgehammer
                Add(new GenericBuyInfo(typeof(Hammer), 28, 20, 0x102A, 0));
                Add(new GenericBuyInfo(typeof(SmithHammer), 4, 20, 0x13E3, 0));
                Add(new GenericBuyInfo(typeof(Hatchet), 22, 20, 0xF43, 0));  //added 1/18/05 by pigpen
                Add(new GenericBuyInfo(typeof(ButcherKnife), 21, 20, 0x13F6, 0));

                /* Shopkeeper NPCs do not sell any resources (Ingots, Cloth, etc.))
				 * http://www.uoguide.com/Siege_Perilous
				 */
                if (!Core.UOSP && !Core.UOAI && !Core.UOREN && !Core.UOMO)
                {
                    Add(new GenericBuyInfo(typeof(Board), 3, 20, 0x1BD7, 0));
                    Add(new GenericBuyInfo(typeof(IronIngot), 5, 16, 0x1BF2, 0));
                }

                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback system
                    Add(new GenericBuyInfo(typeof(Board)));
                    Add(new GenericBuyInfo(typeof(IronIngot)));
                    Add(new GenericBuyInfo(typeof(DullCopperIngot)));
                    Add(new GenericBuyInfo(typeof(ShadowIronIngot)));
                    Add(new GenericBuyInfo(typeof(CopperIngot)));
                    Add(new GenericBuyInfo(typeof(BronzeIngot)));
                    Add(new GenericBuyInfo(typeof(GoldIngot)));
                    Add(new GenericBuyInfo(typeof(AgapiteIngot)));
                    Add(new GenericBuyInfo(typeof(VeriteIngot)));
                    Add(new GenericBuyInfo(typeof(ValoriteIngot)));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {
                    // balanced buyback system
                    Add(typeof(Board));
                    Add(typeof(Log));
                    Add(typeof(IronIngot));
                    Add(typeof(DullCopperIngot));
                    Add(typeof(ShadowIronIngot));
                    Add(typeof(CopperIngot));
                    Add(typeof(BronzeIngot));
                    Add(typeof(GoldIngot));
                    Add(typeof(AgapiteIngot));
                    Add(typeof(VeriteIngot));
                    Add(typeof(ValoriteIngot));
                    Add(typeof(IronOre));
                    Add(typeof(DullCopperOre));
                    Add(typeof(ShadowIronOre));
                    Add(typeof(CopperOre));
                    Add(typeof(BronzeOre));
                    Add(typeof(GoldOre));
                    Add(typeof(AgapiteOre));
                    Add(typeof(VeriteOre));
                    Add(typeof(ValoriteOre));
                }

                if (!Core.UOAI && !Core.UOREN && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Drums), 25);
                    Add(typeof(Tambourine), 30);
                    Add(typeof(LapHarp), 15);
                    Add(typeof(Lute), 20);

                    Add(typeof(Shovel), 6);
                    Add(typeof(SewingKit), 1);
                    Add(typeof(Scissors), 6);
                    Add(typeof(Tongs), 1);
                    Add(typeof(Key), 1);

                    Add(typeof(DovetailSaw), 7);
                    Add(typeof(MouldingPlane), 6);
                    Add(typeof(Nails), 1);
                    Add(typeof(JointingPlane), 6);
                    Add(typeof(SmoothingPlane), 6);
                    Add(typeof(Saw), 9);

                    Add(typeof(Clock), 11);
                    Add(typeof(ClockParts), 1);
                    Add(typeof(AxleGears), 1);
                    Add(typeof(Gears), 1);
                    Add(typeof(Hinge), 1);
                    Add(typeof(Sextant), 12);
                    Add(typeof(SextantParts), 2);
                    Add(typeof(Axle), 1);
                    Add(typeof(Springs), 1);

                    Add(typeof(DrawKnife), 6);
                    Add(typeof(Froe), 6);
                    Add(typeof(Inshave), 6);
                    Add(typeof(Scorp), 6);

                    Add(typeof(Lockpick), 6);
                    Add(typeof(TinkerTools), 15);

                    Add(typeof(Pickaxe), 16);
                    Add(typeof(Hammer), 14);
                    Add(typeof(SmithHammer), 2);
                    Add(typeof(ButcherKnife), 10);
                }
            }
        }
    }
}
