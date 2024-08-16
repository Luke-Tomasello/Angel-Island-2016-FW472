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
    public class SBPlateArmor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBPlateArmor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(PlateArms), 181, 20, 0x1410, 0));
                Add(new GenericBuyInfo(typeof(PlateChest), 273, 20, 0x1415, 0));
                Add(new GenericBuyInfo(typeof(PlateGloves), 145, 20, 0x1414, 0));
                Add(new GenericBuyInfo(typeof(PlateGorget), 124, 20, 0x1413, 0));
                Add(new GenericBuyInfo(typeof(PlateLegs), 218, 20, 0x1411, 0));

                Add(new GenericBuyInfo(typeof(PlateHelm), 170, 20, 0x1412, 0));
                Add(new GenericBuyInfo(typeof(FemalePlateChest), 245, 20, 0x1C04, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                /*				Add( typeof( PlateArms ), 90 );
				 *				Add( typeof( PlateChest ), 136 );
				 *				Add( typeof( PlateGloves ), 72 );
				 *				Add( typeof( PlateGorget ), 70 );
				 *				Add( typeof( PlateLegs ), 109 );
				 *
				 *				Add( typeof( PlateHelm ), 85 );
				 *				Add( typeof( FemalePlateChest ), 122 );
				 */
            }
        }
    }
}
