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

/* ChangeLog
 *  04/02/05 TK
 *		Added special leathers and redirects
 *  01/28/05 TK
 *		Added hides.
 *  01/23/05, Taran Kain
 *		Added leather.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *
 *  9/14/04, Lego Eater
 *              changed price of taxidermykit from 100k to 30k
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBTanner : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBTanner()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bag), 6, 20, 0xE76, 0));
                Add(new GenericBuyInfo(typeof(Pouch), 6, 20, 0xE79, 0));

                /* Shopkeeper NPCs do not sell any resources (Ingots, Cloth, etc.))
				 * http://www.uoguide.com/Siege_Perilous
				 */
                if (!Core.UOSP && !Core.UOAI && !Core.UOREN && !Core.UOMO)
                {
                    Add(new GenericBuyInfo(typeof(Leather), 6, 20, 0x1081, 0));
                }

                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback
                    Add(new GenericBuyInfo(typeof(Leather)));
                    Add(new GenericBuyInfo(typeof(SpinedLeather)));
                    Add(new GenericBuyInfo(typeof(HornedLeather)));
                    Add(new GenericBuyInfo(typeof(BarbedLeather)));
                }

                if (Core.UOAI || Core.UOREN || Core.UOMO)
                    Add(new GenericBuyInfo("1041279", typeof(TaxidermyKit), 30000, 20, 0x1EBA, 0));//changed price of taxidermykit from 100k to 30k Lego eater.
                else if (Core.UOSP && PublishInfo.Publish >= 11)
                    Add(new GenericBuyInfo("1041279", typeof(TaxidermyKit), 90000, 20, 0x1EBA, 0));

                Add(new GenericBuyInfo(typeof(SkinningKnife), 26, 20, 0xEC4, 0));
                Add(new GenericBuyInfo(typeof(LeatherLegs), 80, 20, 0x13CB, 0));
                Add(new GenericBuyInfo(typeof(LeatherShorts), 86, 20, 0x1C00, 0));
                Add(new GenericBuyInfo(typeof(LeatherSkirt), 87, 20, 0x1C08, 0));
                Add(new GenericBuyInfo(typeof(LeatherCap), 10, 20, 0x1DB9, 0));
                Add(new GenericBuyInfo(typeof(LeatherGloves), 60, 20, 0x13C6, 0));
                Add(new GenericBuyInfo(typeof(LeatherGorget), 74, 20, 0x13C7, 0));
                Add(new GenericBuyInfo(typeof(LeatherChest), 101, 20, 0x13CC, 0));
                Add(new GenericBuyInfo(typeof(LeatherBustierArms), 97, 20, 0x1C0A, 0));
                Add(new GenericBuyInfo(typeof(LeatherArms), 80, 20, 0x13CD, 0));
                Add(new GenericBuyInfo(typeof(StuddedLegs), 103, 20, 0x13DA, 0));
                Add(new GenericBuyInfo(typeof(StuddedGloves), 79, 20, 0x13D5, 0));
                Add(new GenericBuyInfo(typeof(StuddedGorget), 73, 20, 0x13D6, 0));
                Add(new GenericBuyInfo(typeof(StuddedChest), 128, 20, 0x13DB, 0));
                Add(new GenericBuyInfo(typeof(StuddedBustierArms), 120, 20, 0x1C0C, 0));
                Add(new GenericBuyInfo(typeof(StuddedArms), 87, 20, 0x13DC, 0));
                Add(new GenericBuyInfo(typeof(FemaleStuddedChest), 142, 20, 0x1C02, 0));
                Add(new GenericBuyInfo(typeof(FemalePlateChest), 245, 20, 0x1C04, 0));
                Add(new GenericBuyInfo(typeof(FemaleLeatherChest), 116, 20, 0x1C06, 0));
                Add(new GenericBuyInfo(typeof(Backpack), 15, 20, 0x9B2, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback system
                    Add(typeof(Leather));
                    Add(typeof(Hides));
                    Add(typeof(SpinedLeather));
                    Add(typeof(HornedLeather));
                    Add(typeof(BarbedLeather));
                    Add(typeof(SpinedHides));
                    Add(typeof(BarbedHides));
                    Add(typeof(HornedHides));
                }

                if (!Core.UOAI && !Core.UOREN && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(LeatherArms), 40);
                    Add(typeof(LeatherChest), 52);
                    Add(typeof(LeatherGloves), 30);
                    Add(typeof(LeatherGorget), 37);
                    Add(typeof(LeatherLegs), 40);
                    Add(typeof(LeatherCap), 5);

                    Add(typeof(StuddedArms), 43);
                    Add(typeof(StuddedChest), 64);
                    Add(typeof(StuddedGloves), 39);
                    Add(typeof(StuddedGorget), 36);
                    Add(typeof(StuddedLegs), 51);

                    Add(typeof(FemaleStuddedChest), 71);
                    Add(typeof(StuddedBustierArms), 60);

                    Add(typeof(FemaleLeatherChest), 58);
                    Add(typeof(LeatherBustierArms), 48);
                    Add(typeof(LeatherShorts), 43);
                    Add(typeof(LeatherSkirt), 43);
                }
            }
        }
    }
}
