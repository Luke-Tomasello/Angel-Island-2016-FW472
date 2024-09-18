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

/* Scripts/Mobiles/Vendors/SBInfo/SBInnKeeper.cs
 * ChangeLog
 *	4/2/08, Adam
 *		- Add "work permit for a barkeep"
 *		- test out random pricing (without buyback this should be ok)
 *	1/16/08, Adam
 *		Add "a vendor renegotiation contract" to the list of contracts
 *  04/19/05, Kit
 *	Added vendor rental contract
 *  11/12/04, Jade
 *      Changed spelling to make housesitter one word.
 *  11/07/2004, Jade
 *      Added new House Sitter deeds to the inventory of items for sale.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBInnKeeper : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBInnKeeper()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Ale, 7, 20, 0x99F, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Wine, 7, 20, 0x9C7, 0));
                Add(new BeverageBuyInfo(typeof(BeverageBottle), BeverageType.Liquor, 7, 20, 0x99B, 0));
                Add(new BeverageBuyInfo(typeof(Jug), BeverageType.Cider, 13, 20, 0x9C8, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Milk, 7, 20, 0x9F0, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Ale, 11, 20, 0x1F95, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Cider, 11, 20, 0x1F97, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Liquor, 11, 20, 0x1F99, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Wine, 11, 20, 0x1F9B, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Water, 11, 20, 0x1F9D, 0));
                Add(new GenericBuyInfo(typeof(BreadLoaf), 7, 10, 0x103B, 0));
                Add(new GenericBuyInfo(typeof(CheeseWheel), 25, 10, 0x97E, 0));
                Add(new GenericBuyInfo(typeof(CookedBird), 17, 20, 0x9B7, 0));
                Add(new GenericBuyInfo(typeof(LambLeg), 8, 20, 0x160A, 0));
                Add(new GenericBuyInfo(typeof(ChickenLeg), 6, 20, 0x1608, 0));
                Add(new GenericBuyInfo(typeof(Ribs), 12, 20, 0x9F2, 0));
                // TODO: Bowl of *, tomato soup, baked pie
                Add(new GenericBuyInfo(typeof(Peach), 3, 20, 0x9D2, 0));
                Add(new GenericBuyInfo(typeof(Pear), 3, 20, 0x994, 0));
                Add(new GenericBuyInfo(typeof(Grapes), 3, 20, 0x9D1, 0));
                Add(new GenericBuyInfo(typeof(Apple), 3, 20, 0x9D0, 0));
                Add(new GenericBuyInfo(typeof(Banana), 2, 20, 0x171F, 0));
                Add(new GenericBuyInfo(typeof(Torch), 7, 20, 0xF6B, 0));
                Add(new GenericBuyInfo(typeof(Candle), 6, 20, 0xA28, 0));
                Add(new GenericBuyInfo(typeof(Beeswax), 1, 20, 0x1422, 0));
                // TODO: Beeswax
                Add(new GenericBuyInfo(typeof(Backpack), 15, 20, 0x9B2, 0));
                Add(new GenericBuyInfo("1016450", typeof(Chessboard), 2, 20, 0xFA6, 0));
                Add(new GenericBuyInfo("1016449", typeof(CheckerBoard), 2, 20, 0xFA6, 0));
                Add(new GenericBuyInfo(typeof(Backgammon), 2, 20, 0xE1C, 0));
                Add(new GenericBuyInfo(typeof(Dices), 2, 20, 0xFA7, 0));

                Add(new GenericBuyInfo("1041243", typeof(ContractOfEmployment), 1025, 20, 0x14F0, 0));

                // Publish 13 - August 19, 2001
                // Treasure map changes, tutorial/Haven changes, combat changes, with power hour changes and player owned barkeeps as later additions
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules() || (Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13))
                    Add(new GenericBuyInfo("a barkeep contract", typeof(BarkeepContract), 6250, 20, 0x14F0, 0));

                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {
                    // Jade: Add new House Sitter deeds
                    Add(new GenericBuyInfo("a housesitter contract", typeof(HouseSitterDeed), 2500, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("vendor rental contract", typeof(VendorRentalContract), 1025, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("a vendor renegotiation contract", typeof(VendorRenegotiationContract), 1200, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("work permit for a barkeep", typeof(BarkeepWorkPermit), Utility.RandomMinMax(50000, 100000), 20, 0x14F0, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.SiegeRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(BeverageBottle), 3);
                    Add(typeof(Jug), 6);
                    Add(typeof(Pitcher), 5);
                    Add(typeof(GlassMug), 1);
                    Add(typeof(BreadLoaf), 3);
                    Add(typeof(CheeseWheel), 12);
                    Add(typeof(Ribs), 6);
                    Add(typeof(Peach), 1);
                    Add(typeof(Pear), 1);
                    Add(typeof(Grapes), 1);
                    Add(typeof(Apple), 1);
                    Add(typeof(Banana), 1);
                    Add(typeof(Torch), 3);
                    Add(typeof(Candle), 3);
                    Add(typeof(Chessboard), 1);
                    Add(typeof(CheckerBoard), 1);
                    Add(typeof(Backgammon), 1);
                    Add(typeof(Dices), 1);
                    Add(typeof(ContractOfEmployment), 512);
                    Add(typeof(Beeswax), 1);
                }
            }
        }
    }
}
