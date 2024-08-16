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

/* Scripts/Mobiles/Vendors/SBInfo/SBBaker.cs
 * ChangeLog
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBBaker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBBaker()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BreadLoaf), 7, 10, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(CheeseWheel), 25, 10, 0x97E, 0));
                Add(new GenericBuyInfo(typeof(FrenchBread), 3, 20, 0x98C, 0));
                Add(new GenericBuyInfo(typeof(FriedEggs), 8, 10, 0x9B6, 0));
                Add(new GenericBuyInfo(typeof(Cake), 11, 10, 0x9E9, 0));
                Add(new GenericBuyInfo(typeof(Cookies), 6, 20, 0x160b, 0));
                Add(new GenericBuyInfo(typeof(Muffins), 5, 10, 0x9eb, 0));
                Add(new GenericBuyInfo(typeof(CheesePizza), 14, 10, 0x1040, 0));

                Add(new GenericBuyInfo(typeof(ApplePie), 10, 10, 0x1041, 0));

                Add(new GenericBuyInfo(typeof(PeachCobbler), 10, 10, 0x1041, 0));

                Add(new GenericBuyInfo(typeof(Quiche), 25, 10, 0x1041, 0));
                Add(new GenericBuyInfo(typeof(Dough), 8, 20, 0x103d, 0));
                Add(new GenericBuyInfo(typeof(JarHoney), 3, 20, 0x9ec, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Water, 11, 20, 0x1F9D, 0));
                Add(new GenericBuyInfo(typeof(SackFlour), 3, 20, 0x1039, 0));
                Add(new GenericBuyInfo(typeof(Eggs), 3, 20, 0x9B5, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(BreadLoaf), 3);
                    Add(typeof(CheeseWheel), 12);
                    Add(typeof(FrenchBread), 1);
                    Add(typeof(FriedEggs), 4);
                    Add(typeof(Cake), 5);
                    Add(typeof(Cookies), 3);
                    Add(typeof(Muffins), 2);
                    Add(typeof(CheesePizza), 7);
                    Add(typeof(ApplePie), 5);
                    Add(typeof(PeachCobbler), 5);
                    Add(typeof(Quiche), 12);
                    Add(typeof(Dough), 4);
                    Add(typeof(JarHoney), 1);
                    Add(typeof(Pitcher), 5);
                    Add(typeof(SackFlour), 1);
                    Add(typeof(Eggs), 1);
                }
            }
        }
    }
}
