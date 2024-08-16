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

/* Items/SkillItems/Magical/Potions/Cure Potions/GreaterCurePotion.cs
 * CHANGELOG:
 *  11/07/05, Kit
 *		Restored former cure levels.
 *	10/16/05, Pix
 *		Tweaked poison cure levels
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    public class GreaterCurePotion : BaseCurePotion
    {
        private static CureLevelInfo[] m_OldLevelInfo = new CureLevelInfo[]
            {
                new CureLevelInfo( Poison.Lesser,  1.00 ), // 100% chance to cure lesser poison
				new CureLevelInfo( Poison.Regular, 1.00 ), // 100% chance to cure regular poison
				new CureLevelInfo( Poison.Greater, 1.00 ), // 100% chance to cure greater poison
				new CureLevelInfo( Poison.Deadly,  0.75 ), //  75% chance to cure deadly poison
				//new CureLevelInfo( Poison.Deadly,  0.85 ), //  85% chance to cure deadly poison
				new CureLevelInfo( Poison.Lethal,  0.10 )  //  25% chance to cure lethal poison
			};

        public override CureLevelInfo[] LevelInfo { get { return m_OldLevelInfo; } }

        [Constructable]
        public GreaterCurePotion()
            : base(PotionEffect.CureGreater)
        {
        }

        public GreaterCurePotion(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
