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

/* Scripts\Items\Special\Holiday\Snowflake.cs
 * Changelog:
 *	12/11/05, Adam
 *		Changed LootType.Blessed to LootType.Regular
 */

namespace Server.Items
{
    public class BlueSnowflake : Item
    {
        [Constructable]
        public BlueSnowflake()
            : base(0x232E)
        {
            Weight = 1.0;
            //Adam: Unbless these
            LootType = LootType.Regular;
        }

        public BlueSnowflake(Serial serial)
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

    public class WhiteSnowflake : Item
    {
        [Constructable]
        public WhiteSnowflake()
            : base(0x232F)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public WhiteSnowflake(Serial serial)
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
