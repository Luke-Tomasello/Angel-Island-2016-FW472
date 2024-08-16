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
    public class SBKnifeWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBKnifeWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(ButcherKnife), 21, 20, 0x13F6, 0));
                Add(new GenericBuyInfo(typeof(Cleaver), 24, 20, 0xEC3, 0));
                Add(new GenericBuyInfo(typeof(Dagger), 33, 20, 0xF52, 0));
                Add(new GenericBuyInfo(typeof(SkinningKnife), 26, 20, 0xEC4, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                /*				Add( typeof( ButcherKnife ), 10 );
				 *				Add( typeof( Cleaver ), 12 );
				 *				Add( typeof( Dagger ), 21 );
				 *				Add( typeof( SkinningKnife ), 13 );
				 */
            }
        }
    }
}
