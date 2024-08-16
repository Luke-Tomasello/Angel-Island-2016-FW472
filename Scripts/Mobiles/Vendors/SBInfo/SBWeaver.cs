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

/* Scripts/Mobiles/Vendors/SBInfo/SBWeaver.cs
 * ChangeLog
 *  02/04/05 TK
 *		Added new cloth redirects
 *  01/28/05 TK
 *		Added BoltOfCloth, UncutCloth
 *	01/23/05, Taran Kain
 *		Added cloth.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBWeaver : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBWeaver()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Dyes), 8, 20, 0xFA9, 0));
                Add(new GenericBuyInfo(typeof(DyeTub), 8, 20, 0xFAB, 0));

                /* Shopkeeper NPCs do not sell any resources (Ingots, Cloth, etc.))
				 * http://www.uoguide.com/Siege_Perilous
				 */
                if (!Core.UOSP && !Core.UOAI && !Core.UOAR)
                {
                    Add(new GenericBuyInfo(typeof(UncutCloth), 3, 20, 0x1761, 0));
                    Add(new GenericBuyInfo(typeof(UncutCloth), 3, 20, 0x1762, 0));
                    Add(new GenericBuyInfo(typeof(UncutCloth), 3, 20, 0x1763, 0));
                    Add(new GenericBuyInfo(typeof(UncutCloth), 3, 20, 0x1764, 0));

                    Add(new GenericBuyInfo(typeof(BoltOfCloth), 100, 20, 0xf9B, 0));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), 100, 20, 0xf9C, 0));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), 100, 20, 0xf96, 0));
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), 100, 20, 0xf97, 0));

                    Add(new GenericBuyInfo(typeof(DarkYarn), 18, 20, 0xE1D, 0));
                    Add(new GenericBuyInfo(typeof(LightYarn), 18, 20, 0xE1E, 0));
                    Add(new GenericBuyInfo(typeof(LightYarnUnraveled), 18, 20, 0xE1F, 0));
                }

                Add(new GenericBuyInfo(typeof(Scissors), 11, 20, 0xF9F, 0));


                if (Core.UOAI || Core.UOAR || Core.UOMO)
                {   // balanced buyback system
                    Add(new GenericBuyInfo(typeof(Cloth)));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.UOAI || Core.UOAR || Core.UOMO)
                {
                    // balanced buyback system
                    Add(typeof(Cloth));
                    Add(typeof(BoltOfCloth));
                    Add(typeof(UncutCloth));
                    Add(typeof(Cotton));
                    Add(typeof(DarkYarn));
                    Add(typeof(LightYarn));
                    Add(typeof(LightYarnUnraveled));
                    Add(typeof(Wool));
                }

                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Scissors), 6);
                    Add(typeof(Dyes), 4);
                    Add(typeof(DyeTub), 4);
                    Add(typeof(BoltOfCloth), 60);
                    Add(typeof(LightYarnUnraveled), 9);
                    Add(typeof(LightYarn), 9);
                    Add(typeof(DarkYarn), 9);
                }
            }
        }
    }
}
