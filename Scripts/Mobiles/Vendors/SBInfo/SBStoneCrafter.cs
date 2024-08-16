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

/* Scripts/Mobiles/Vendors/SBInfo/SBStoneCrafter.cs
 * ChangeLog
 *	4/10/05, Kitaras	
 *		Added in stone graver item.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBStoneCrafter : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBStoneCrafter()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("Making Valuables With Stonecrafting", typeof(MasonryBook), 10625, 10, 0xFBE, 0));
                Add(new GenericBuyInfo("Mining For Quality Stone", typeof(StoneMiningBook), 10625, 10, 0xFBE, 0));
                Add(new GenericBuyInfo("1044515", typeof(MalletAndChisel), 3, 50, 0x12B3, 0));

                if (Core.UOAI || Core.UOAR || Core.UOMO)
                    Add(new GenericBuyInfo("Stone Graver", typeof(StoneGraver), 350, 20, 4135, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(MasonryBook), 5000);
                    Add(typeof(StoneMiningBook), 5000);
                    Add(typeof(MalletAndChisel), 1);
                }
            }
        }
    }
}
