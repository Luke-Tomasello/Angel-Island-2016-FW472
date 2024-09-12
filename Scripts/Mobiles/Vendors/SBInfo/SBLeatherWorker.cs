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

/* Scripts/Mobiles/Vendors/SBInfo/SBLeatherWorker.cs
 * ChangeLog
 *  04/02/05 TK
 *		Added special leather, hides
 *  01/28/05 TK
 *		Added hides
 *	01/23/05, Taran Kain
 *		Added leather.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBLeatherWorker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBLeatherWorker()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {

                /* Shopkeeper NPCs do not sell any resources (Ingots, Cloth, etc.))
				 * http://www.uoguide.com/Siege_Perilous
				 */
                if (!Core.UOSP && !Core.UOAI && !Core.UOREN && !Core.UOMO)
                    Add(new GenericBuyInfo(typeof(Hides), 4, 999, 0x1078, 0));

                if (!Core.UOAI && !Core.UOREN && !Core.UOMO)
                    // only sell these on some servers
                    Add(new GenericBuyInfo(typeof(ThighBoots), 56, 10, 0x1711, 0));

                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback support
                    Add(new GenericBuyInfo(typeof(Leather)));
                    Add(new GenericBuyInfo(typeof(SpinedLeather)));
                    Add(new GenericBuyInfo(typeof(HornedLeather)));
                    Add(new GenericBuyInfo(typeof(BarbedLeather)));
                }
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
                    Add(typeof(ThighBoots), 28);
                }
            }
        }
    }
}
