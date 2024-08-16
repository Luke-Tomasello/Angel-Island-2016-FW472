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

/* Scripts/Mobiles/Vendors/SBInfo/SBMapmaker.cs
 * ChangeLog
 *  10/14/04, Froste
 *      Changed the amount argument to GenericBuyInfo from 999 to 20 for MapmakersPen, so the argument means something in GenericBuy.cs
 *  9/24/04, Jade
 *      Changed BlankScroll price from 12gp to 5gp to be uniform with other scroll-selling npcs.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMapmaker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMapmaker()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                for (int i = 0; i < PresetMapEntry.Table.Length; ++i)
                    Add(new PresetMapBuyInfo(PresetMapEntry.Table[i], Utility.RandomMinMax(7, 10), 20));

                Add(new GenericBuyInfo(typeof(BlankScroll), 5, 20, 0x0E34, 0));
                Add(new GenericBuyInfo(typeof(MapmakersPen), 8, 20, 0x0FBF, 0));
                Add(new GenericBuyInfo(typeof(BlankMap), 5, 40, 0x14EC, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(BlankScroll), 6);
                    Add(typeof(MapmakersPen), 4);
                    Add(typeof(BlankMap), 2);
                }
            }
        }
    }
}
