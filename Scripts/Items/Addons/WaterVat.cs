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
    public class WaterVatEast : BaseAddon
    {
        [Constructable]
        public WaterVatEast()
        {
            AddComponent(new AddonComponent(0x1558), 0, 0, 0);
            AddComponent(new AddonComponent(0x14DE), -1, 1, 0);
            AddComponent(new AddonComponent(0x1552), 0, 1, 0);
            AddComponent(new AddonComponent(0x14DF), 1, -1, 0);
            AddComponent(new AddonComponent(0x1554), 1, 0, 0);
            AddComponent(new AddonComponent(0x1559), 1, 1, 0);
            AddComponent(new AddonComponent(0x1550), 1, 3, 0);
            AddComponent(new AddonComponent(0x1555), 3, 1, 0);
            AddComponent(new AddonComponent(0x14D7), 2, 2, 0);

            // Blockers
            AddComponent(new AddonComponent(0x21A4), 2, -1, 0);
            AddComponent(new AddonComponent(0x21A4), 3, 0, 0);
            AddComponent(new AddonComponent(0x21A4), -1, 2, 0);
            AddComponent(new AddonComponent(0x21A4), 0, 3, 0);
        }

        public WaterVatEast(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class WaterVatSouth : BaseAddon
    {
        [Constructable]
        public WaterVatSouth()
        {
            AddComponent(new AddonComponent(0x1558), 0, 0, 0);
            AddComponent(new AddonComponent(0x14DE), -1, 1, 0);
            AddComponent(new AddonComponent(0x1552), 0, 1, 0);
            AddComponent(new AddonComponent(0x14DF), 1, -1, 0);
            AddComponent(new AddonComponent(0x1554), 1, 0, 0);
            AddComponent(new AddonComponent(0x1559), 1, 1, 0);
            AddComponent(new AddonComponent(0x1551), 1, 3, 0);
            AddComponent(new AddonComponent(0x1556), 3, 1, 0);
            AddComponent(new AddonComponent(0x14D7), 2, 2, 0);

            // Blockers
            AddComponent(new AddonComponent(0x21A4), 2, -1, 0);
            AddComponent(new AddonComponent(0x21A4), 3, 0, 0);
            AddComponent(new AddonComponent(0x21A4), -1, 2, 0);
            AddComponent(new AddonComponent(0x21A4), 0, 3, 0);
        }

        public WaterVatSouth(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}
