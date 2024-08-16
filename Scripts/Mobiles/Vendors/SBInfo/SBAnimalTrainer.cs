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

/* Scripts/Mobiles/Vendors/SBInfo/AnimalTrainer.cs
 * CHANGELOG:
 *	5/6/05: Pix
 *		Updated "ItemID" field for mobiles that the animal trainer sells
 */

using System.Collections;

namespace Server.Mobiles
{
    public class SBAnimalTrainer : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBAnimalTrainer()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new AnimalBuyInfo(1, typeof(Eagle), 402, 10, 5, 0));
                Add(new AnimalBuyInfo(1, typeof(Cat), 138, 10, 201, 0));
                Add(new AnimalBuyInfo(1, typeof(Horse), 602, 10, 204, 0));
                Add(new AnimalBuyInfo(1, typeof(Rabbit), 78, 10, 205, 0));
                Add(new AnimalBuyInfo(1, typeof(BrownBear), 855, 10, 167, 0));
                Add(new AnimalBuyInfo(1, typeof(GrizzlyBear), 1767, 10, 212, 0));
                Add(new AnimalBuyInfo(1, typeof(Panther), 1271, 10, 214, 0));
                Add(new AnimalBuyInfo(1, typeof(Dog), 181, 10, 217, 0));
                Add(new AnimalBuyInfo(1, typeof(TimberWolf), 768, 10, 225, 0));
                Add(new AnimalBuyInfo(1, typeof(PackHorse), 606, 10, 291, 0));
                Add(new AnimalBuyInfo(1, typeof(PackLlama), 491, 10, 292, 0));
                Add(new AnimalBuyInfo(1, typeof(Rat), 107, 10, 238, 0));
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
