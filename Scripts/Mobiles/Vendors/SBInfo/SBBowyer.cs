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

/* Scripts/Mobiles/SBInfo/SBBowyer.cs
 * Changelog
 *	01/23/05	Taran Kain
 *		Added arrows, bolts, shafts and feathers.
 */
using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBBowyer : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBBowyer()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {   // balanced buyback
                    Add(new GenericBuyInfo(typeof(Arrow)));
                    Add(new GenericBuyInfo(typeof(Bolt)));
                    Add(new GenericBuyInfo(typeof(Shaft)));
                    Add(new GenericBuyInfo(typeof(Feather)));
                }

                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.MortalisRules())
                {
                    Add(new GenericBuyInfo(typeof(FletcherTools), 20, 20, 0x1022, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {   // balanced buyback system
                    Add(typeof(Arrow));
                    Add(typeof(Bolt));
                    Add(typeof(Shaft));
                    Add(typeof(Feather));
                }

                if (!Core.RuleSets.SiegeRules() && !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(FletcherTools), 1);
                }
            }
        }
    }
}
