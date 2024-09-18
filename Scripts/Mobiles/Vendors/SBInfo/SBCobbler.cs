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

/* Scripts/Mobiles/Vendors/SBInfo/SBCobbler.cs
 * ChangeLog
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBCobbler : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBCobbler()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Shoes), 23, 20, 0x170f, 0));
                Add(new GenericBuyInfo(typeof(Boots), 38, 20, 0x170b, 0));
                Add(new GenericBuyInfo(typeof(ThighBoots), 56, 20, 0x1711, 0));
                Add(new GenericBuyInfo(typeof(Sandals), 18, 20, 0x170d, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.SiegeRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(Shoes), 11);
                    Add(typeof(Boots), 19);
                    Add(typeof(ThighBoots), 28);
                    Add(typeof(Sandals), 9);
                }
            }
        }
    }
}
