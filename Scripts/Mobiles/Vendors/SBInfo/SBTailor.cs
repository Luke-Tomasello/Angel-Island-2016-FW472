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

/* Scripts/Mobiles/Vendors/SBInfo/SBTailor.cs
 * ChangeLog
 *	4/2/8, Pix
 *		Added Leather Embroidery Book/Floss
 *  10/15/05, erlein
 *     Added color swatches + fixed embroidery book text.
 *  2/10/05, erlein
 *     Removed finished clothing pieces.
 *  09/08/05, erlein
 *     Added embroidery floss + book.
 *  04/02/05 TK
 *		Added new cloth redirects, special leather types and hides
 *  01/28/05 TK
 *		Added BoltOfCloth, UncutCloth, Hides
 *  01/23/05, Taran Kain
 *		Added cloth and leather. Removed Bolt of Cloth (not handled by ResourcePool)
 *	4/24/04, mith
 *		Commented all items from SellList so that NPCs don't buy from players.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class SBTailor : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBTailor()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {

                Add(new GenericBuyInfo(typeof(Scissors), 13, 20, 0xF9F, 0));
                Add(new GenericBuyInfo(typeof(SewingKit), 3, 20, 0xF9D, 0));
                Add(new GenericBuyInfo(typeof(Dyes), 8, 20, 0xFA9, 0));
                Add(new GenericBuyInfo(typeof(DyeTub), 9, 20, 0xFAB, 0));

                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {   // AI specials
                    Add(new GenericBuyInfo("embroidering", typeof(EmbroideryBook), 10625, 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("leather embroidering", typeof(LeatherEmbroideryBook), 25000, 20, 0xFF4, 0));
                    Add(new GenericBuyInfo("embroidery floss", typeof(Embroidery), 250, 20, 0xFA0, 0));
                    Add(new GenericBuyInfo("heavy embroidery floss", typeof(LeatherEmbroideryFloss), 1000, 20, 0x1420, 0));
                    Add(new GenericBuyInfo("color swatch", typeof(ColorSwatch), 250, 20, 0x175D, 0));
                }

                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
                {
                    // erl : removed for new clothing wear system to involve tailors directly in clothing
                    Add(new GenericBuyInfo(typeof(FancyShirt), 55, 20, 0x1EFD, 0));
                    Add(new GenericBuyInfo(typeof(Shirt), 37, 20, 0x1517, 0));

                    Add(new GenericBuyInfo(typeof(ShortPants), 24, 20, 0x152E, 0));
                    Add(new GenericBuyInfo(typeof(LongPants), 34, 20, 0x1539, 0));

                    Add(new GenericBuyInfo(typeof(Cloak), 43, 20, 0x1515, 0));
                    Add(new GenericBuyInfo(typeof(FancyDress), 25, 20, 0x1EFF, 0));
                    Add(new GenericBuyInfo(typeof(Robe), 68, 20, 0x1F03, 0));
                    Add(new GenericBuyInfo(typeof(PlainDress), 56, 20, 0x1F01, 0));

                    Add(new GenericBuyInfo(typeof(Skirt), 33, 20, 0x1516, 0));
                    Add(new GenericBuyInfo(typeof(Kilt), 31, 20, 0x1537, 0));

                    Add(new GenericBuyInfo(typeof(FullApron), 26, 20, 0x153d, 0));
                    Add(new GenericBuyInfo(typeof(HalfApron), 26, 20, 0x153b, 0));

                    Add(new GenericBuyInfo(typeof(Doublet), 25, 20, 0x1F7B, 0));
                    Add(new GenericBuyInfo(typeof(Tunic), 33, 20, 0x1FA1, 0));
                    Add(new GenericBuyInfo(typeof(JesterSuit), 51, 20, 0x1F9F, 0));

                    Add(new GenericBuyInfo(typeof(JesterHat), 31, 20, 0x171C, 0));
                    Add(new GenericBuyInfo(typeof(FloppyHat), 25, 20, 0x1713, 0));
                    Add(new GenericBuyInfo(typeof(WideBrimHat), 26, 20, 0x1714, 0));
                    Add(new GenericBuyInfo(typeof(Cap), 27, 20, 0x1715, 0));
                    Add(new GenericBuyInfo(typeof(SkullCap), 12, 20, 0x1544, 0));
                    Add(new GenericBuyInfo(typeof(Bandana), 14, 20, 0x1540, 0));
                    Add(new GenericBuyInfo(typeof(TallStrawHat), 26, 20, 0x1716, 0));
                    Add(new GenericBuyInfo(typeof(StrawHat), 25, 20, 0x1717, 0));
                    Add(new GenericBuyInfo(typeof(WizardsHat), 30, 20, 0x1718, 0));
                    Add(new GenericBuyInfo(typeof(Bonnet), 26, 20, 0x1719, 0));
                    Add(new GenericBuyInfo(typeof(FeatheredHat), 27, 20, 0x171A, 0));
                    Add(new GenericBuyInfo(typeof(TricorneHat), 26, 20, 0x171B, 0));
                }

                /* Shopkeeper NPCs do not sell any resources (Ingots, Cloth, etc.))
				 * http://www.uoguide.com/Siege_Perilous
				 */
                if (!Core.RuleSets.SiegeRules() && !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules())
                {   // no selling these resources on AI
                    Add(new GenericBuyInfo(typeof(BoltOfCloth), 100, 20, 0xf95, Utility.RandomDyedHue()));

                    Add(new GenericBuyInfo(typeof(Cloth), 2, 20, 0x1766, Utility.RandomDyedHue()));
                    Add(new GenericBuyInfo(typeof(UncutCloth), 2, 20, 0x1767, Utility.RandomDyedHue()));

                    Add(new GenericBuyInfo(typeof(Cotton), 102, 20, 0xDF9, 0));
                    Add(new GenericBuyInfo(typeof(Wool), 62, 20, 0xDF8, 0));
                    Add(new GenericBuyInfo(typeof(Flax), 102, 20, 0x1A9C, 0));
                    Add(new GenericBuyInfo(typeof(SpoolOfThread), 18, 20, 0xFA0, 0));
                }

                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {   // balanced buyback system
                    //Add( new GenericBuyInfo( typeof( Flax ) ) );
                    Add(new GenericBuyInfo(typeof(Cloth)));
                    Add(new GenericBuyInfo(typeof(Leather)));
                    Add(new GenericBuyInfo(typeof(SpinedLeather)));
                    Add(new GenericBuyInfo(typeof(HornedLeather)));
                    Add(new GenericBuyInfo(typeof(BarbedLeather)));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules())
                {   // balanced buyback system 
                    Add(typeof(Cloth));
                    Add(typeof(Leather));
                    Add(typeof(BoltOfCloth));
                    Add(typeof(UncutCloth));
                    Add(typeof(Hides));
                    Add(typeof(Cotton));
                    Add(typeof(Wool));
                    Add(typeof(SpoolOfThread));
                    Add(typeof(SpinedLeather));
                    Add(typeof(HornedLeather));
                    Add(typeof(BarbedLeather));
                    Add(typeof(SpinedHides));
                    Add(typeof(BarbedHides));
                    Add(typeof(HornedHides));
                }

                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.SiegeRules() && !Core.RuleSets.MortalisRules())
                {   // cash buyback
                    Add(typeof(Scissors), 6);
                    Add(typeof(SewingKit), 1);
                    Add(typeof(Dyes), 4);
                    Add(typeof(DyeTub), 4);

                    Add(typeof(BoltOfCloth), 60);

                    Add(typeof(FancyShirt), 27);
                    Add(typeof(Shirt), 18);

                    Add(typeof(ShortPants), 12);
                    Add(typeof(LongPants), 17);

                    Add(typeof(Cloak), 21);
                    Add(typeof(FancyDress), 12);
                    Add(typeof(Robe), 34);
                    Add(typeof(PlainDress), 28);

                    Add(typeof(Skirt), 16);
                    Add(typeof(Kilt), 15);

                    Add(typeof(Doublet), 12);
                    Add(typeof(Tunic), 16);
                    Add(typeof(JesterSuit), 25);

                    Add(typeof(FullApron), 13);
                    Add(typeof(HalfApron), 13);

                    Add(typeof(JesterHat), 15);
                    Add(typeof(FloppyHat), 12);
                    Add(typeof(WideBrimHat), 13);
                    Add(typeof(Cap), 13);
                    Add(typeof(SkullCap), 6);
                    Add(typeof(Bandana), 6);
                    Add(typeof(TallStrawHat), 13);
                    Add(typeof(StrawHat), 12);
                    Add(typeof(WizardsHat), 15);
                    Add(typeof(Bonnet), 13);
                    Add(typeof(FeatheredHat), 13);
                    Add(typeof(TricorneHat), 13);

                    Add(typeof(SpoolOfThread), 9);

                    Add(typeof(Flax), 51);
                    Add(typeof(Cotton), 51);
                    Add(typeof(Wool), 31);
                }
            }
        }
    }
}
