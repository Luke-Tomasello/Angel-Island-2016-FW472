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

namespace Server.Items
{
    [Flipable(0x1053, 0x1054)]
    public class Gears : Item
    {
        [Constructable]
        public Gears()
            : this(1)
        {
        }

        [Constructable]
        public Gears(int amount)
            : base(0x1053)
        {
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public Gears(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Gears(), amount);
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
