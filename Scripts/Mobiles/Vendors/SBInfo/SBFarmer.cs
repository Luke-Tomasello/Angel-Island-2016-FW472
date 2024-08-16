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

/* Scripts/Mobiles/Vendors/SBInfo/SBFarmer.cs
 * ChangeLog
 *	5/12/05, Adam
 *		Add PlantBowl
 *		Add SeedBox
 *  3/30/05, Jade
 *      Restore carrots, pumpkins, squash, gouards, melons that were commented out
 *  3/26/05, Jade
 *      comment out carrots for easter event.
 *	10/30/04, Adam
 *		comment out squashes and gourds for halloween event: will turn back on after
 *	10/29/04, Adam
 *		comment out pumpkins for halloween event: will turn back on after
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBFarmer : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBFarmer()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Eggs), 3, 20, 0x9B5, 0));
                Add(new GenericBuyInfo(typeof(Apple), 3, 20, 0x9D0, 0));
                Add(new GenericBuyInfo(typeof(Grapes), 3, 20, 0x9D1, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Milk, 7, 20, 0x9AD, 0));
                Add(new GenericBuyInfo(typeof(Watermelon), 7, 20, 0xC5C, 0));
                Add(new GenericBuyInfo(typeof(YellowGourd), 3, 20, 0xC64, 0));
                Add(new GenericBuyInfo(typeof(GreenGourd), 3, 20, 0xC66, 0));
                Add(new GenericBuyInfo(typeof(Pumpkin), 11, 20, 0xC6A, 0));
                Add(new GenericBuyInfo(typeof(Onion), 3, 20, 0xC6D, 0));
                Add(new GenericBuyInfo(typeof(Lettuce), 5, 20, 0xC70, 0));
                Add(new GenericBuyInfo(typeof(Squash), 3, 20, 0xC72, 0));
                Add(new GenericBuyInfo(typeof(HoneydewMelon), 7, 20, 0xC74, 0));
                Add(new GenericBuyInfo(typeof(Carrot), 3, 20, 0xC78, 0));
                Add(new GenericBuyInfo(typeof(Cantaloupe), 6, 20, 0xC79, 0));
                Add(new GenericBuyInfo(typeof(Cabbage), 5, 20, 0xC7B, 0));
                //Add( new GenericBuyInfo( typeof( EarOfCorn ), 3, 20, XXXXXX, 0 ) );
                //Add( new GenericBuyInfo( typeof( Turnip ), 6, 20, XXXXXX, 0 ) );
                //Add( new GenericBuyInfo( typeof( SheafOfHay ), 2, 20, XXXXXX, 0 ) );
                Add(new GenericBuyInfo(typeof(Lemon), 3, 20, 0x1728, 0));
                Add(new GenericBuyInfo(typeof(Lime), 3, 20, 0x172A, 0));
                Add(new GenericBuyInfo(typeof(Peach), 3, 20, 0x9D2, 0));
                Add(new GenericBuyInfo(typeof(Pear), 3, 20, 0x994, 0));
                Add(new GenericBuyInfo("1060834", typeof(Engines.Plants.PlantBowl), 2, 20, 0x15FD, 0));

                if (Core.UOAI || Core.UOAR || Core.UOMO)
                    Add(new GenericBuyInfo(typeof(SeedBox), 10000, 20, 0x9A9, 0x1CE));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Pitcher), 5);
                    Add(typeof(Eggs), 1);
                    Add(typeof(Apple), 1);
                    Add(typeof(Grapes), 1);
                    Add(typeof(Watermelon), 3);
                    Add(typeof(YellowGourd), 1);
                    Add(typeof(GreenGourd), 1);
                    Add(typeof(Pumpkin), 5);
                    Add(typeof(Onion), 1);
                    Add(typeof(Lettuce), 2);
                    Add(typeof(Squash), 1);
                    Add(typeof(Carrot), 1);
                    Add(typeof(HoneydewMelon), 3);
                    Add(typeof(Cantaloupe), 3);
                    Add(typeof(Cabbage), 2);
                    Add(typeof(Lemon), 1);
                    Add(typeof(Lime), 1);
                    Add(typeof(Peach), 1);
                    Add(typeof(Pear), 1);
                }
            }
        }
    }
}
