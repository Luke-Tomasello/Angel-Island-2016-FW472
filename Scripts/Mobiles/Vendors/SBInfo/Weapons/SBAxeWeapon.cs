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
    public class SBAxeWeapon : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBAxeWeapon()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(BattleAxe), 38, 20, 0xF47, 0));
                Add(new GenericBuyInfo(typeof(DoubleAxe), 32, 20, 0xF4B, 0));
                Add(new GenericBuyInfo(typeof(ExecutionersAxe), 38, 20, 0xF45, 0));
                Add(new GenericBuyInfo(typeof(LargeBattleAxe), 43, 20, 0x13FB, 0));
                Add(new GenericBuyInfo(typeof(Pickaxe), 32, 20, 0xE86, 0));
                Add(new GenericBuyInfo(typeof(TwoHandedAxe), 42, 20, 0x1443, 0));
                Add(new GenericBuyInfo(typeof(WarAxe), 38, 20, 0x13B0, 0));
                Add(new GenericBuyInfo(typeof(Axe), 48, 20, 0xF49, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                /*				Add( typeof( BattleAxe ), 36 );
				 *				Add( typeof( DoubleAxe ), 16 );
				 *				Add( typeof( ExecutionersAxe ), 33 );
				 *				Add( typeof( LargeBattleAxe ), 21 );
				 *				Add( typeof( Pickaxe ), 16 );
				 *				Add( typeof( TwoHandedAxe ), 21 );
				 *				Add( typeof( WarAxe ), 19 );
				 *				Add( typeof( Axe ), 24 );
				 */
            }
        }
    }
}
