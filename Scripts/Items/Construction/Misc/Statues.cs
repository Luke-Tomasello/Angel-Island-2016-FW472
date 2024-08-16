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

/* Items/Construction/Misc/Stautes.cs
 * ChangeLog:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    public class StatueSouth : Item
    {
        [Constructable]
        public StatueSouth()
            : base(0x139A)
        {
            Weight = 10;
        }

        public StatueSouth(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatueSouth2 : Item
    {
        [Constructable]
        public StatueSouth2()
            : base(0x1227)
        {
            Weight = 10;
        }

        public StatueSouth2(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatueNorth : Item
    {
        [Constructable]
        public StatueNorth()
            : base(0x139B)
        {
            Weight = 10;
        }

        public StatueNorth(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatueWest : Item
    {
        [Constructable]
        public StatueWest()
            : base(0x1226)
        {
            Weight = 10;
        }

        public StatueWest(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatueEast : Item
    {
        [Constructable]
        public StatueEast()
            : base(0x139C)
        {
            Weight = 10;
        }

        public StatueEast(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatueEast2 : Item
    {
        [Constructable]
        public StatueEast2()
            : base(0x1224)
        {
            Weight = 10;
        }

        public StatueEast2(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatueSouthEast : Item
    {
        [Constructable]
        public StatueSouthEast()
            : base(0x1225)
        {
            Weight = 10;
        }

        public StatueSouthEast(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class BustSouth : Item
    {
        [Constructable]
        public BustSouth()
            : base(0x12CB)
        {
            Weight = 10;
        }

        public BustSouth(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class BustEast : Item
    {
        [Constructable]
        public BustEast()
            : base(0x12CA)
        {
            Weight = 10;
        }

        public BustEast(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatuePegasus : Item
    {
        [Constructable]
        public StatuePegasus()
            : base(0x139D)
        {
            Weight = 10;
        }

        public StatuePegasus(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StatuePegasus2 : Item
    {
        [Constructable]
        public StatuePegasus2()
            : base(0x1228)
        {
            Weight = 10;
        }

        public StatuePegasus2(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
