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

using Server.Multis;
using System.Collections;

namespace Server.Mobiles
{
    public class SBShipwright : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBShipwright()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("1041205", typeof(SmallBoatDeed), 12500, 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041206", typeof(SmallDragonBoatDeed), 12500, 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041207", typeof(MediumBoatDeed), 14200, 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041208", typeof(MediumDragonBoatDeed), 14200, 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041209", typeof(LargeBoatDeed), 15900, 20, 0x14F2, 0));
                Add(new GenericBuyInfo("1041210", typeof(LargeDragonBoatDeed), 15900, 20, 0x14F2, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOREN && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(SmallBoatDeed), 6250);
                    Add(typeof(SmallDragonBoatDeed), 6250);
                    Add(typeof(MediumBoatDeed), 7100);
                    Add(typeof(MediumDragonBoatDeed), 7100);
                    Add(typeof(LargeBoatDeed), 7950);
                    Add(typeof(LargeDragonBoatDeed), 7950);
                }
            }
        }
    }
}
