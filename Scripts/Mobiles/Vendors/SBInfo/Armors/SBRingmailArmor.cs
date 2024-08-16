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
    public class SBRingmailArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBRingmailArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(RingmailArms), 122, 20, 0x13EE, 0));
                Add(new GenericBuyInfo(typeof(RingmailChest), 218, 20, 0x13ec, 0));
                Add(new GenericBuyInfo(typeof(RingmailGloves), 122, 20, 0x13eb, 0));
                Add(new GenericBuyInfo(typeof(RingmailLegs), 147, 20, 0x13F0, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                /*				Add( typeof( RingmailArms ), 61 );
				 *				Add( typeof( RingmailChest ), 109 );
				 *				Add( typeof( RingmailGloves ), 61 );
				 *				Add( typeof( RingmailLegs ), 73 );
				 */
            }
        }
    }
}
