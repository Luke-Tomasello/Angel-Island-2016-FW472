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
    public class SBMetalShields : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMetalShields()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Buckler), 66, 20, 0x1B73, 0));
                Add(new GenericBuyInfo(typeof(BronzeShield), 91, 20, 0x1B72, 0));
                Add(new GenericBuyInfo(typeof(MetalShield), 98, 20, 0x1B7B, 0));
                Add(new GenericBuyInfo(typeof(MetalKiteShield), 135, 20, 0x1B74, 0));
                Add(new GenericBuyInfo(typeof(HeaterShield), 185, 20, 0x1B76, 0));
                Add(new GenericBuyInfo(typeof(WoodenKiteShield), 121, 20, 0x1B78, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                /*				Add( typeof( Buckler ), 33 );
				 *				Add( typeof( BronzeShield ), 45 );
				 *				Add( typeof( MetalShield ), 49 );
				 *				Add( typeof( MetalKiteShield ), 67 );
				 *				Add( typeof( HeaterShield ), 87 );
				 *				Add( typeof( WoodenKiteShield ), 60 );
				 */
            }
        }
    }
}
