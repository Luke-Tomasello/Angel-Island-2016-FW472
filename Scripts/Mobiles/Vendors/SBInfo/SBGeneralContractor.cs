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

/* Scripts/Mobiles/Vendors/SBInfo/SBGeneralContractor.cs
 * ChangeLog
 *  5/8/07, Adam
 *      First time checkin
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBGeneralContractor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBGeneralContractor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {
                    Add(new GenericBuyInfo(typeof(BookofUpgradeContracts), 100, 20, 0xFF0, 0));
                    Add(new GenericBuyInfo(typeof(ModestUpgradeContract), 82562, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(ModerateUpgradeContract), 195750, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(PremiumUpgradeContract), 498900, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(ExtravagantUpgradeContract), 767100, 20, 0x14F0, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.SiegeRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(InteriorDecorator), 5000);
                }

                if (Core.RuleSets.AOSRules())
                {   // cash buyback (AOS)
                    Add(typeof(HousePlacementTool), 301);
                }
            }
        }
    }
}
