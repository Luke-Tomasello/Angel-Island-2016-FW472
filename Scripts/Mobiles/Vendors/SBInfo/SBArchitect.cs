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

/* Scripts/Mobiles/Vendors/SBInfo/SBArchitect.cs
 * ChangeLog
 *  9/29/07, plasma
 *    Added DarkWoodGateHouseDoorDeed, LightWoodHouseDoorDeed, LightWoodGateHouseDoorDeed, 
 *    StrongWoodHouseDoorDeed, SmallIronGateHouseDoorDeed, SecretLightStoneHouseDoorDeed,
 *    SecretLightWoodHouseDoorDeed, SecretDarkWoodHouseDoorDeed and RattanHouseDoorDeed to the buy list     
 *  5/6/07, Adam
 *      Add StorageTaxCredits, and LockboxBuildingPermit deeds to the shopping list
 *	2/27/07, Pix
 *		Added CellHouseDoorDeed
 *  12/05/06 Taran Kain
 *      Added IronGateHouseDoorDeed.
 *	9/05/06 Taran Kain
 *		Added MetalHouseDoorDeed, DarkWoodHouseDoorDeed, SecretStoneHouseDoorDeed to vendor sell list.
 *  9/26/04, Jade
 *      Added SurveyTool to the vendor inventory.
 *	4/29/04, mith
 *		removed Core.AOS check so that Architects will sell house placement tools even if AOS is disabled.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBArchitect : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBArchitect()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if ((Core.UOAI || Core.UOAR || Core.UOMO) || (Core.UOSP && PublishInfo.Publish >= 11))
                    Add(new GenericBuyInfo("1041280", typeof(InteriorDecorator), 10000, 20, 0xFC1, 0));

                // I'm guessing this was in all publishes of Siege?
                Add(new GenericBuyInfo("Survey Tool", typeof(SurveyTool), 5000, 20, 0x14F6, 0));

                if (Core.UOAI || Core.UOAR || Core.UOMO)
                {
                    Add(new GenericBuyInfo(typeof(MetalHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(DarkWoodHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(DarkWoodGateHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(LightWoodHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(LightWoodGateHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(StrongWoodHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(IronGateHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SmallIronGateHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(CellHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(RattanHouseDoorDeed), 25000, 20, 0x14F0, 0));
                    //secret doors              
                    Add(new GenericBuyInfo(typeof(SecretStoneHouseDoorDeed), 28000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SecretLightStoneHouseDoorDeed), 28000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SecretLightWoodHouseDoorDeed), 28000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo(typeof(SecretDarkWoodHouseDoorDeed), 28000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("30 day tax credit: Lockbox", typeof(StorageTaxCredits), 30000, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("A building permit: Lockbox", typeof(LockboxBuildingPermit), 15000, 20, 0x14F0, 0));
                }

                if (Core.AOS)
                    Add(new GenericBuyInfo("1060651", typeof(HousePlacementTool), 601, 20, 0x14F6, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(InteriorDecorator), 5000);
                }

                if (Core.AOS)
                {   // cash buyback
                    Add(typeof(HousePlacementTool), 301);
                }
            }
        }
    }
}
