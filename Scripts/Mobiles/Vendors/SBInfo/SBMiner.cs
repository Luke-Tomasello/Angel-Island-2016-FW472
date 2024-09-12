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

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMiner : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMiner()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bag), 6, 20, 0xE76, 0));
                Add(new GenericBuyInfo(typeof(Candle), 6, 10, 0xA28, 0));
                Add(new GenericBuyInfo(typeof(Torch), 8, 10, 0xF6B, 0));
                Add(new GenericBuyInfo(typeof(Lantern), 2, 10, 0xA25, 0));
                //Add( new GenericBuyInfo( typeof( OilFlask ), 8, 10, 0x####, 0 ) );
                Add(new GenericBuyInfo(typeof(Pickaxe), 25, 10, 0xE86, 0));
                Add(new GenericBuyInfo(typeof(Shovel), 12, 10, 0xF39, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {   // balanced buyback system

                }

                if (!Core.UOAI && !Core.UOREN && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Pickaxe), 12);
                    Add(typeof(Shovel), 6);
                    Add(typeof(Lantern), 1);
                    //Add( typeof( OilFlask ), 4 );
                    Add(typeof(Torch), 3);
                    Add(typeof(Bag), 3);
                    Add(typeof(Candle), 3);
                }
            }
        }
    }
}
