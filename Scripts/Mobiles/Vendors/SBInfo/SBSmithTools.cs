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

/* Scripts/Mobiles/Vendors/SBInfo/SBSmithTools.cs
 * ChangeLog
 *  01/28/05 TK
 *		Added ores.
 *  01/23/05, Taran Kain
 *		Added all nine ingot colors
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBSmithTools : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBSmithTools()
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
                if (!Core.UOSP)
                    Add(new GenericBuyInfo(typeof(IronIngot), 5, 16, 0x1BF2, 0));

                Add(new GenericBuyInfo(typeof(Tongs), 13, 14, 0xFBB, 0));
                Add(new GenericBuyInfo(typeof(SmithHammer), 4, 16, 0x13E3, 0));

                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback system
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
                {   // balanced buyback system
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
                    Add(typeof(Tongs), 7);
                    Add(typeof(IronIngot), 4);
                }
            }
        }
    }
}
