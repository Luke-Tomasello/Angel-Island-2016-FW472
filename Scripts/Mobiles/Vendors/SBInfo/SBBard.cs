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

/* Scripts/Mobiles/Vendors/SBInfo/SBBard.cs
 * ChangeLog
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBBard : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBBard()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(LapHarp), 30, (10), 0x0EB2, 0));
                Add(new GenericBuyInfo(typeof(Lute), 40, (10), 0x0EB3, 0));
                Add(new GenericBuyInfo(typeof(Drums), 50, (10), 0x0E9C, 0));
                Add(new GenericBuyInfo(typeof(Harp), 90, (10), 0x0EB1, 0));
                Add(new GenericBuyInfo(typeof(Tambourine), 60, (10), 0x0E9E, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(LapHarp), 15);
                    Add(typeof(Lute), 20);
                    Add(typeof(Drums), 25);
                    Add(typeof(Harp), 45);
                    Add(typeof(Tambourine), 30);
                }
            }
        }
    }
}
