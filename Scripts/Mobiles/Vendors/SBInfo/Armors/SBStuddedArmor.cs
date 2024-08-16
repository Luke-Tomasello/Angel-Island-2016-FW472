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
    public class SBStuddedArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBStuddedArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(StuddedArms), 87, 20, 0x13DC, 0));
                Add(new GenericBuyInfo(typeof(StuddedChest), 128, 20, 0x13DB, 0));
                Add(new GenericBuyInfo(typeof(StuddedGloves), 79, 20, 0x13D5, 0));
                Add(new GenericBuyInfo(typeof(StuddedGorget), 73, 20, 0x13D6, 0));
                Add(new GenericBuyInfo(typeof(StuddedLegs), 103, 20, 0x13DA, 0));
                Add(new GenericBuyInfo(typeof(FemaleStuddedChest), 142, 20, 0x1C02, 0));
                Add(new GenericBuyInfo(typeof(StuddedBustierArms), 120, 20, 0x1c0c, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                /*				Add( typeof( StuddedArms ), 43 );
				 *				Add( typeof( StuddedChest ), 64 );
				 *				Add( typeof( StuddedGloves ), 39 );
				 *				Add( typeof( StuddedGorget ), 36 );
				 *				Add( typeof( StuddedLegs ), 51 );
				 *				Add( typeof( FemaleStuddedChest ), 71 );
				 *				Add( typeof( StuddedBustierArms ), 60 );
				 */
            }
        }
    }
}
