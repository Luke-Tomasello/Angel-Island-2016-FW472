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

/* Scripts/Items/SkillItems/CarpenterItems/TaxidermyKit.cs
 * CHANGELOG
 *	04/07/05, Kitaras	
 *		Added overload methods to accept strings for deed and trophy name vs cliloc's, added Zvalue adjusment
 */

using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    [FlipableAttribute(0x1EBA, 0x1EBB)]
    public class TaxidermyKit : Item
    {
        public override int LabelNumber { get { return 1041279; } } // a taxidermy kit

        [Constructable]
        public TaxidermyKit()
            : base(0x1EBA)
        {
            Weight = 1.0;
        }

        public TaxidermyKit(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.Skills[SkillName.Carpentry].Base < 90.0)
            {
                from.SendLocalizedMessage(1042594); // You do not understand how to use this.
            }
            else
            {
                from.SendLocalizedMessage(1042595); // Target the corpse to make a trophy out of.
                from.Target = new CorpseTarget(this);
            }
        }

        private static object[,] m_Table = new object[,]
            {
                { typeof( BrownBear ),      0x1E60,     1041093, 1041107 },
                { typeof( GreatHart ),      0x1E61,     1041095, 1041109 },
                { typeof( BigFish ),        0x1E62,     1041096, 1041110 },
                { typeof( Gorilla ),        0x1E63,     1041091, 1041105 },
                { typeof( Orc ),            0x1E64,     1041090, 1041104 },
                { typeof( PolarBear ),      0x1E65,     1041094, 1041108 },
                { typeof( Troll ),          0x1E66,     1041092, 1041106 }
            };

        private class CorpseTarget : Target
        {
            private TaxidermyKit m_Kit;

            public CorpseTarget(TaxidermyKit kit)
                : base(3, false, TargetFlags.None)
            {
                m_Kit = kit;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Kit.Deleted)
                    return;

                if (!(targeted is Corpse) && !(targeted is BigFish))
                {
                    from.SendLocalizedMessage(1042600); // That is not a corpse!
                }
                else if (targeted is Corpse && ((Corpse)targeted).VisitedByTaxidermist)
                {
                    from.SendLocalizedMessage(1042596); // That corpse seems to have been visited by a taxidermist already.
                }
                else if (!m_Kit.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else if (from.Skills[SkillName.Carpentry].Base < 90.0)
                {
                    from.SendLocalizedMessage(1042603); // You would not understand how to use the kit.
                }
                else
                {
                    object obj = targeted;

                    if (obj is Corpse)
                        obj = ((Corpse)obj).Owner;

                    for (int i = 0; obj != null && i < m_Table.GetLength(0); ++i)
                    {
                        if (m_Table[i, 0] == obj.GetType())
                        {
                            Container pack = from.Backpack;

                            if (pack != null && pack.ConsumeTotal(typeof(Board), 10))
                            {
                                from.SendLocalizedMessage(1042278); // You review the corpse and find it worthy of a trophy.
                                from.SendLocalizedMessage(1042602); // You use your kit up making the trophy.

                                from.AddToBackpack(new TrophyDeed((int)m_Table[i, 1] + 7, (int)m_Table[i, 1], (int)m_Table[i, 2], (int)m_Table[i, 3]));

                                if (targeted is Corpse)
                                    ((Corpse)targeted).VisitedByTaxidermist = true;
                                else if (targeted is BigFish)
                                    ((BigFish)targeted).Consume();

                                m_Kit.Delete();
                                return;
                            }
                            else
                            {
                                from.SendLocalizedMessage(1042598); // You do not have enough boards.
                                return;
                            }
                        }
                    }

                    from.SendLocalizedMessage(1042599); // That does not look like something you want hanging on a wall.
                }
            }
        }
    }

    public class TrophyAddon : Item
    {
        private int m_WestID;
        private int m_NorthID;
        private int m_DeedNumber;
        private int m_AddonNumber;
        private string m_Deed;
        private string m_Addon;
        private int Zvalue; //new variable to allow adjustments of zvalues

        [CommandProperty(AccessLevel.GameMaster)]
        public int WestID { get { return m_WestID; } set { m_WestID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NorthID { get { return m_NorthID; } set { m_NorthID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeedNumber { get { return m_DeedNumber; } set { m_DeedNumber = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ZAdjustment { get { return Zvalue; } set { Zvalue = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AddonNumber { get { return m_AddonNumber; } set { m_AddonNumber = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DeedName { get { return m_Deed; } set { m_Deed = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string AddonName { get { return m_Addon; } set { m_Addon = value; InvalidateProperties(); } }

        public override int LabelNumber { get { return m_AddonNumber; } }

        //new constructer for string based names vs clilocs
        [Constructable]
        public TrophyAddon(Mobile from, int itemID, int westID, int northID, string deedNumber, string addonNumber, int z)
            : base(itemID)
        {
            m_WestID = westID;
            m_NorthID = northID;
            m_Deed = deedNumber;
            Name = addonNumber;
            m_Addon = addonNumber;
            Movable = false;
            Zvalue = z;

            MoveToWorld(new Point3D(from.X, from.Y, from.Z + Zvalue), from.Map);
        }

        [Constructable]
        public TrophyAddon(Mobile from, int itemID, int westID, int northID, int deedNumber, int addonNumber)
            : base(itemID)
        {
            m_WestID = westID;
            m_NorthID = northID;
            m_DeedNumber = deedNumber;
            m_AddonNumber = addonNumber;
            Zvalue = 0;
            Movable = false;

            MoveToWorld(from.Location, from.Map);
        }

        public TrophyAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write((string)m_Deed);
            writer.Write((string)m_Addon);
            writer.Write((int)Zvalue);

            writer.Write((int)m_WestID);
            writer.Write((int)m_NorthID);
            writer.Write((int)m_DeedNumber);
            writer.Write((int)m_AddonNumber);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Deed = reader.ReadString();
                        m_Addon = reader.ReadString();
                        Zvalue = reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_WestID = reader.ReadInt();
                        m_NorthID = reader.ReadInt();
                        m_DeedNumber = reader.ReadInt();
                        m_AddonNumber = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    if (m_DeedNumber != 0)
                    {
                        from.AddToBackpack(new TrophyDeed(m_WestID, m_NorthID, m_DeedNumber, m_AddonNumber));
                        Delete();
                    }

                    if (m_DeedNumber == 0)
                    {
                        from.AddToBackpack(new TrophyDeed(m_WestID, m_NorthID, m_Deed, m_Addon, Zvalue));
                        Delete();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }
    }

    [Flipable(0x14F0, 0x14EF)]
    public class TrophyDeed : Item
    {
        private int m_WestID;
        private int m_NorthID;
        private int m_DeedNumber;
        private int m_AddonNumber;
        private string m_Deed;
        private string m_Addon;
        private int Zvalue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int WestID { get { return m_WestID; } set { m_WestID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NorthID { get { return m_NorthID; } set { m_NorthID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DeedNumber { get { return m_DeedNumber; } set { m_DeedNumber = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ZAdjustment { get { return Zvalue; } set { Zvalue = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AddonNumber { get { return m_AddonNumber; } set { m_AddonNumber = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DeedName { get { return m_Deed; } set { m_Deed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string AddonName { get { return m_Addon; } set { m_Addon = value; } }

        public override int LabelNumber { get { return m_DeedNumber; } }

        [Constructable]
        public TrophyDeed(int westID, int northID, string deedNumber, string addonNumber, int z)
            : base(0x14F0)
        {
            m_WestID = westID;
            m_NorthID = northID;
            m_Deed = deedNumber;
            Name = deedNumber;
            m_Addon = addonNumber;
            Zvalue = z;
        }

        [Constructable]
        public TrophyDeed(int westID, int northID, int deedNumber, int addonNumber)
            : base(0x14F0)
        {
            m_WestID = westID;
            m_NorthID = northID;
            m_DeedNumber = deedNumber;
            m_AddonNumber = addonNumber;
            Zvalue = 0;
        }

        public TrophyDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write((string)m_Deed);
            writer.Write((string)m_Addon);
            writer.Write((int)Zvalue);

            writer.Write((int)m_WestID);
            writer.Write((int)m_NorthID);
            writer.Write((int)m_DeedNumber);
            writer.Write((int)m_AddonNumber);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Deed = reader.ReadString();
                        m_Addon = reader.ReadString();
                        Zvalue = reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_WestID = reader.ReadInt();
                        m_NorthID = reader.ReadInt();
                        m_DeedNumber = reader.ReadInt();
                        m_AddonNumber = reader.ReadInt();
                        break;
                    }
            }
        }

        public static bool IsWall(int x, int y, int z, Map map)
        {
            if (map == null)
                return false;

            Tile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                Tile t = tiles[i];
                ItemData id = TileData.ItemTable[t.ID & 0x3FFF];

                if ((id.Flags & TileFlag.Wall) != 0 && (z + 16) > t.Z && (t.Z + t.Height) > z)
                    return true;
            }

            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house != null && house.IsCoOwner(from))
                {
                    bool northWall = IsWall(from.X, from.Y - 1, from.Z, from.Map);
                    bool westWall = IsWall(from.X - 1, from.Y, from.Z, from.Map);

                    if (northWall && westWall)
                    {
                        switch (from.Direction & Direction.Mask)
                        {
                            case Direction.North:
                            case Direction.South: northWall = true; westWall = false; break;

                            case Direction.East:
                            case Direction.West: northWall = false; westWall = true; break;

                            default: from.SendMessage("Turn to face the wall on which to hang this trophy."); return;
                        }
                    }

                    int itemID = 0;

                    if (northWall)
                        itemID = m_NorthID;
                    else if (westWall)
                        itemID = m_WestID;
                    else
                        from.SendLocalizedMessage(1042626); // The trophy must be placed next to a wall.

                    if (itemID > 0)
                    {
                        if (m_DeedNumber != 0)
                        {
                            house.Addons.Add(new TrophyAddon(from, itemID, m_WestID, m_NorthID, m_DeedNumber, m_AddonNumber));
                            Delete();
                        }

                        if (m_DeedNumber == 0)
                        {
                            house.Addons.Add(new TrophyAddon(from, itemID, m_WestID, m_NorthID, m_Deed, m_Addon, Zvalue));
                            Delete();
                        }


                    }
                }
                else
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}
