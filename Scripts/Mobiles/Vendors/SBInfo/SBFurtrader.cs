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

/* Scripts/Mobiles/Vendors/SBInfo/SBFurTrader.cs
 * ChangeLog
 *  04/02/05 TK
 *		Added special leather types
 *	01/28/05 TK
 *		Added Leather, Hides
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBFurtrader : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBFurtrader()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.UOAI || Core.UOAR || Core.UOMO)
                {   // balanced buyback system
                    Add(new GenericBuyInfo(typeof(Leather)));
                    Add(new GenericBuyInfo(typeof(SpinedLeather)));
                    Add(new GenericBuyInfo(typeof(HornedLeather)));
                    Add(new GenericBuyInfo(typeof(BarbedLeather)));
                }

                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(new GenericBuyInfo(typeof(Hides), 3, 40, 0x1079, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.UOAI || Core.UOAR || Core.UOMO)
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

                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Hides), 2);
                }
            }
        }
    }
}
