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

/* Scripts/Mobiles/Vendors/SBInfo/SBCook.cs
 * ChangeLog
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBCook : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBCook()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(CheeseWheel), 25, 20, 0x97E, 0));
                Add(new GenericBuyInfo("1044567", typeof(Skillet), 3, 20, 0x97F, 0));
                Add(new GenericBuyInfo(typeof(CookedBird), 17, 20, 0x9B7, 0));
                Add(new GenericBuyInfo(typeof(RoastPig), 106, 20, 0x9BB, 0));
                Add(new GenericBuyInfo(typeof(Cake), 11, 20, 0x9E9, 0));
                // TODO: Muffin @ 3gp
                Add(new GenericBuyInfo(typeof(JarHoney), 3, 20, 0x9EC, 0));
                Add(new GenericBuyInfo(typeof(SackFlour), 3, 20, 0x1039, 0));
                Add(new GenericBuyInfo(typeof(BreadLoaf), 7, 20, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(FlourSifter), 2, 20, 0x103E, 0));
                //Add( new GenericBuyInfo( typeof( BakedPie ), 7, 20, 0x1041, 0 ) );
                Add(new GenericBuyInfo(typeof(RollingPin), 2, 20, 0x1043, 0));
                // TODO: Bowl of carrots/corn/lettuce/peas/potatoes/stew/tomato soup @ 3gp
                // TODO: Pewter bowl @ 2gp
                Add(new GenericBuyInfo(typeof(ChickenLeg), 6, 20, 0x1608, 0));
                Add(new GenericBuyInfo(typeof(LambLeg), 8, 20, 0x1609, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(CheeseWheel), 12);
                    Add(typeof(CookedBird), 8);
                    Add(typeof(RoastPig), 53);
                    Add(typeof(Cake), 5);
                    Add(typeof(JarHoney), 1);
                    Add(typeof(SackFlour), 1);
                    Add(typeof(BreadLoaf), 3);
                    Add(typeof(ChickenLeg), 3);
                    Add(typeof(LambLeg), 4);
                    Add(typeof(Skillet), 1);
                    Add(typeof(FlourSifter), 1);
                    Add(typeof(RollingPin), 1);
                }
            }
        }
    }
}
