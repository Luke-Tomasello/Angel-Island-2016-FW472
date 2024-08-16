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

/* Scripts/Mobiles/Vendors/SBInfo/SBMagiSupplier.cs
 * ChangeLog
 *	2/10/05, Adam
 *		Restore Moonstones
 *	2/6/05, Adam
 *		Remove Moonstones temporarily
 *  1/23/05, Froste
 *      Modified version of SBOrcMerchant.cs
 *  10/18/04, Froste
 *      Added a MinValue param equal to the amount param
 *	10/18/04, Adam
 *		Reduce bulk prices a tad, and increase quantities
 *  10/17/04, Froste
 *      Modified version of SBImporter.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/22/04, mith
 *		Modified so that Mages only sell up to level 3 scrolls.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMagiSupplier : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMagiSupplier()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo("Arms of the Bone Magi", typeof(BoneMagiArms), 360, 20, 0x144E, 0));
                Add(new GenericBuyInfo("Armor of the Bone Magi", typeof(BoneMagiArmor), 500, 20, 0x144F, 0));
                Add(new GenericBuyInfo("Gloves of the Bone Magi", typeof(BoneMagiGloves), 270, 20, 0x1450, 0));
                Add(new GenericBuyInfo("Legs of the Bone Magi", typeof(BoneMagiLegs), 360, 20, 0x1452, 0));
                Add(new GenericBuyInfo("Helm of the Bone Magi", typeof(BoneMagiHelm), 45, 20, 0x1451, 0));
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
