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

/* Scripts/Mobiles/Vendors/SBInfo/SBImporter.cs
 * ChangeLog
 *	29/06/09, plasma
 *		Increased bulk regs +1gp to offset factions
 *  10/18/04, Froste
 *      Added a MinValue param equal to the amount param
 *	10/18/04, Adam
 *		Reduce bulk prices a tad, and increase quantities
 *  10/11/04, Froste
 *      Modified version of SBMage.cs
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
    public class SBImporter : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBImporter()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bloodmoss), 9, 500, 500, 999, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(Garlic), 6, 500, 500, 999, 0xF84, 0));
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
