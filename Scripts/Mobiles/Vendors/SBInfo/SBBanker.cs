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

/* Scripts/Mobiles/Vendors/SBInfo/SBBanker.cs
 * ChangeLog
 *	04/19/05, Kit
 *		Added  VendorRentalContract
 *	05/02/05 TK
 *		Added Account Book
 * */


using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBBanker : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBBanker()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.UOAI || Core.UOAR || Core.UOMO || (Core.UOSP && Core.Publish >= 13.5))
                    Add(new GenericBuyInfo("1047016", typeof(CommodityDeed), 5, 20, 0x14F0, 0x47));

                if (Core.UOAI || Core.UOAR || Core.UOMO)
                {
                    Add(new GenericBuyInfo("1041243", typeof(ContractOfEmployment), 1025, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("account book", typeof(AccountBook), 150, 10, 0xFF1, 0));
                    Add(new GenericBuyInfo("vendor rental contract", typeof(VendorRentalContract), 1025, 20, 0x14F0, 0));
                    Add(new GenericBuyInfo("certificate of identity", typeof(CertificateOfIdentity), 180, 20, 0x14F0, 0));
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
