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

/* Scripts/Mobiles/Vendors/SBInfo/SBMage.cs
 * ChangeLog
 *10/14/04, Froste
 *      Changed the amount argument to GenericBuyInfo from 999 to 20 for reagents, so the argument means something in GenericBuy.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/22/04, mith
 *		Modified so that Mages only sell up to level 3 scrolls.
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class SBMage : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBMage()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Type[] types = Loot.RegularScrollTypes;

                int circles = 3;

                for (int i = 0; i < circles * 8 && i < types.Length; ++i)
                {
                    int itemID = 0x1F2E + i;

                    if (i == 6)
                        itemID = 0x1F2D;
                    else if (i > 6)
                        --itemID;

                    Add(new GenericBuyInfo(types[i], 15 + ((i / 8) * 10), 20, itemID, 0));
                }

                Add(new GenericBuyInfo(typeof(BlackPearl), 5, 20, 0xF7A, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), 5, 20, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), 3, 20, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), 3, 20, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), 3, 20, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(SulfurousAsh), 3, 20, 0xF8C, 0));

                if (Core.AOS)
                {
                    Add(new GenericBuyInfo(typeof(BatWing), 3, 20, 0xF78, 0));
                    Add(new GenericBuyInfo(typeof(GraveDust), 3, 20, 0xF8F, 0));
                    Add(new GenericBuyInfo(typeof(DaemonBlood), 6, 20, 0xF7D, 0));
                    Add(new GenericBuyInfo(typeof(NoxCrystal), 6, 20, 0xF8E, 0));
                    Add(new GenericBuyInfo(typeof(PigIron), 5, 20, 0xF8A, 0));

                    Add(new GenericBuyInfo(typeof(NecromancerSpellbook), 115, 10, 0x2253, 0));
                }

                Add(new GenericBuyInfo("1041072", typeof(MagicWizardsHat), 11, 10, 0x1718, 0));

                // sold by players
                //Add( new GenericBuyInfo( "1041267", typeof( Runebook ), 2500, 10, 0xEFA, 0x461 ) );

                Add(new GenericBuyInfo(typeof(RecallRune), 15, 10, 0x1F14, 0));

                if (Core.UOAI || Core.UOAR || Core.UOMO)
                {
                    // the text "An Unmarked Moonstone" does not show. Figure out how Taran did the BBS Types (see GetBunchType in BaseVendor.cs)
                    Add(new GenericBuyInfo("An Unmarked Moonstone", typeof(Moonstone), 10, 100, 0xF8B, 0));
                    Add(new GenericBuyInfo(typeof(PowderOfTranslocation), 3, 100, 0x26B8, 0));
                    Add(new GenericBuyInfo(typeof(Stonebag), 1250, 10, 0xE76, 0));
                }

                Add(new GenericBuyInfo(typeof(Spellbook), 18, 10, 0xEFA, 0));

                Add(new GenericBuyInfo(typeof(ScribesPen), 8, 10, 0xFBF, 0));
                Add(new GenericBuyInfo(typeof(BlankScroll), 5, 20, 0x0E34, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Add(typeof(Runebook), 1250);
                    Add(typeof(BlackPearl), 3);
                    Add(typeof(Bloodmoss), 3);
                    Add(typeof(MandrakeRoot), 2);
                    Add(typeof(Garlic), 2);
                    Add(typeof(Ginseng), 2);
                    Add(typeof(Nightshade), 2);
                    Add(typeof(SpidersSilk), 2);
                    Add(typeof(SulfurousAsh), 2);
                    Add(typeof(RecallRune), 8);
                    Add(typeof(Spellbook), 9);
                    Add(typeof(BlankScroll), 3);
                }

                if (Core.AOS)
                {   // cash buyback
                    Add(typeof(BatWing), 2);
                    Add(typeof(GraveDust), 2);
                    Add(typeof(DaemonBlood), 3);
                    Add(typeof(NoxCrystal), 3);
                    Add(typeof(PigIron), 3);
                }

                if (!Core.UOAI && !Core.UOAR && !Core.UOSP && !Core.UOMO)
                {   // cash buyback
                    Type[] types = Loot.RegularScrollTypes;

                    for (int i = 0; i < types.Length; ++i)
                        Add(types[i], 6 + ((i / 8) * 5));
                }
            }
        }
    }
}
