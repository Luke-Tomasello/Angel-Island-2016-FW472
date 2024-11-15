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
    public class StoneOvenSouthAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new StoneOvenSouthDeed(); } }

        [Constructable]
        public StoneOvenSouthAddon()
        {
            AddComponent(new AddonComponent(0x931), -1, 0, 0);
            AddComponent(new AddonComponent(0x930), 0, 0, 0);
        }

        public StoneOvenSouthAddon(Serial serial)
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

    public class StoneOvenSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new StoneOvenSouthAddon(); } }
        public override int LabelNumber { get { return 1044346; } } // stone oven (south)

        [Constructable]
        public StoneOvenSouthDeed()
        {
        }

        public StoneOvenSouthDeed(Serial serial)
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
