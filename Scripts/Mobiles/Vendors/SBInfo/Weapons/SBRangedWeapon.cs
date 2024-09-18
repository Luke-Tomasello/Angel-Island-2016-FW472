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

/* ChangeLog
 *  05/02/05 TK
 *		Removed arrow, bolt, shaft, feather from list - they're covered in Bowyer
 *		Bowyer was selling arrows twice
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBRangedWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBRangedWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Crossbow), 55, 20, 0xF50, 0));
                Add(new GenericBuyInfo(typeof(HeavyCrossbow), 55, 20, 0x13FD, 0));
                if (Core.RuleSets.AOSRules())
                {
                    Add(new GenericBuyInfo(typeof(RepeatingCrossbow), 46, 20, 0x26C3, 0));
                    Add(new GenericBuyInfo(typeof(CompositeBow), 45, 20, 0x26C2, 0));
                }
                Add(new GenericBuyInfo(typeof(Bolt), 2, Utility.Random(30, 60), 0x1BFB, 0));
                Add(new GenericBuyInfo(typeof(Bow), 40, 20, 0x13B2, 0));
                Add(new GenericBuyInfo(typeof(Arrow), 2, Utility.Random(30, 60), 0xF3F, 0));
                Add(new GenericBuyInfo(typeof(Feather), 2, Utility.Random(30, 60), 0x1BD1, 0));
                Add(new GenericBuyInfo(typeof(Shaft), 3, Utility.Random(30, 60), 0x1BD4, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.SiegeRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(Bolt), 1);
                    Add(typeof(Arrow), 1);
                    Add(typeof(Shaft), 1);
                    Add(typeof(Feather), 1);

                    Add(typeof(HeavyCrossbow), 27);
                    Add(typeof(Bow), 17);
                    Add(typeof(Crossbow), 25);

                    if (Core.RuleSets.AOSRules())
                    {
                        Add(typeof(CompositeBow), 23);
                        Add(typeof(RepeatingCrossbow), 22);
                    }
                }
            }
        }
    }
}
