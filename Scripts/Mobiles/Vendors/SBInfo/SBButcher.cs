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

/* Scripts/Mobiles/Vendors/SBInfo/SBButcher.cs
 * ChangeLog
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBButcher : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBButcher()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(RawRibs), 7, 20, 0x9F1, 0));
                Add(new GenericBuyInfo(typeof(RawLambLeg), 5, 20, 0x1609, 0));
                Add(new GenericBuyInfo(typeof(RawChickenLeg), 2, 20, 0x1607, 0));
                Add(new GenericBuyInfo(typeof(RawBird), 3, 20, 0x9B9, 0));
                Add(new GenericBuyInfo(typeof(Bacon), 3, 20, 0x979, 0));
                Add(new GenericBuyInfo(typeof(Sausage), 17, 20, 0x9C0, 0));
                Add(new GenericBuyInfo(typeof(Ham), 20, 20, 0x9C9, 0));
                Add(new GenericBuyInfo(typeof(ButcherKnife), 21, 20, 0x13F6, 0));
                Add(new GenericBuyInfo(typeof(Cleaver), 24, 20, 0xEC3, 0));
                Add(new GenericBuyInfo(typeof(SkinningKnife), 26, 20, 0xEC4, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.SiegeRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(RawRibs), 3);
                    Add(typeof(RawLambLeg), 2);
                    Add(typeof(RawChickenLeg), 1);
                    Add(typeof(RawBird), 1);
                    Add(typeof(Bacon), 1);
                    Add(typeof(Sausage), 8);
                    Add(typeof(Ham), 10);
                    Add(typeof(ButcherKnife), 13);
                    Add(typeof(Cleaver), 12);
                    Add(typeof(SkinningKnife), 10);
                }
            }
        }
    }
}
