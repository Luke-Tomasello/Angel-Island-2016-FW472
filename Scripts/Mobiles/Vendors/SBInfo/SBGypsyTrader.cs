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

/* Scripts/Mobiles/Vendors/SBInfo/SBGypsyTrader.cs
 * ChangeLog
 *	9/19/06, Pix
 *		Added TeleporterAddonDyeTub for sale for 15K
 *	9/13/06, Pix
 *		Removed old TeleporterDeed and replaced it with TeleporterAddonDeed
 *	3/29/06 Taran Kain
 *		Add NPCNameChangeDeed for 4000
 *	3/27/05, Kitaras
 *		Add the NpcTitleChangeDeed for sale for 1500
 *	3/15/05, Adam
 *		Add the OrcishBodyDeed for sale for 1500
 *	11/16/04, Froste
 *      Created from SBCarpenter.cs
 */

using Server.Items;
using System.Collections;


namespace Server.Mobiles
{
    public class SBGypsyTrader : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBGypsyTrader()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.UOAI || Core.UOREN || Core.UOMO)
                {
                    Add(new GenericBuyInfo("Gender Change Deed", typeof(GenderChangeDeed), 100000, 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Name Change Deed", typeof(NameChangeDeed), 100000, 20, 0x14F0, 0x0));
                    //Add(new GenericBuyInfo("Teleporter Deed", typeof(TeleporterDeed), 500000, 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Teleporter Addon Deed", typeof(TeleporterAddonDeed), 500000, 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Teleporter Dye Tub", typeof(TeleporterAddonDyeTub), 15000, 20, 0xFAB, 0x0));
                    Add(new GenericBuyInfo("Orcish Vendor Body Deed", typeof(OrcishBodyDeed), 1500, 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Vendor Title Change Deed", typeof(NpcTitleChangeDeed), 1500, 20, 0x14F0, 0x0));
                    Add(new GenericBuyInfo("Vendor Name Change Deed", typeof(NpcNameChangeDeed), 4000, 20, 0x14F0, 0x0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
            }
        }
    }
}
