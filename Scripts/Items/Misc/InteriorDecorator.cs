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

/* Scripts/Items/Misc/InteriorDecorator.cs
 * ChangeLog
 *	9/15/10, Adam
 *		Add the ability the move AddonComponents where AddonComponent.Deco flag is true
 *  10/15/06, Rhiannon
 *		Made BaseHouseDoors turnable.
 *		Added security warning when BaseHouseDoors are turned.
 *		Deco tool now requires the owner to turn doors.
 *  9/01/06 Taran Kain
 *		Added special case in checks to allow turning BaseHouseDoorComponents
 *  7/17/04, Adam
 *		1. Change loot from Blessed to Regular
 */

using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public enum DecorateCommand
    {
        None,
        Turn,
        Up,
        Down
    }

    public class InteriorDecorator : Item
    {
        private DecorateCommand m_Command;

        [CommandProperty(AccessLevel.GameMaster)]
        public DecorateCommand Command { get { return m_Command; } set { m_Command = value; InvalidateProperties(); } }

        [Constructable]
        public InteriorDecorator()
            : base(0xFC1)
        {
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public override int LabelNumber { get { return 1041280; } } // an interior decorator

        public InteriorDecorator(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Command != DecorateCommand.None)
                list.Add(1018322 + (int)m_Command); // Turn/Up/Down
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
            if (!CheckUse(this, from))
                return;

            if (m_Command == DecorateCommand.None)
                from.SendGump(new InternalGump(this));
            else
                from.Target = new InternalTarget(this);
        }

        public static bool InHouse(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(from);

            return (house != null && house.IsCoOwner(from));
        }

        public static bool CheckUse(InteriorDecorator tool, Mobile from)
        {
            /*if ( tool.Deleted || !tool.IsChildOf( from.Backpack ) )
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			else*/
            if (!InHouse(from))
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
            else
                return true;

            return false;
        }

        private class InternalGump : Gump
        {
            private InteriorDecorator m_Decorator;

            public InternalGump(InteriorDecorator decorator)
                : base(150, 50)
            {
                m_Decorator = decorator;

                AddBackground(0, 0, 200, 200, 2600);

                AddButton(50, 45, 2152, 2154, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(90, 50, 70, 40, 1018323, false, false); // Turn

                AddButton(50, 95, 2152, 2154, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(90, 100, 70, 40, 1018324, false, false); // Up

                AddButton(50, 145, 2152, 2154, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(90, 150, 70, 40, 1018325, false, false); // Down
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                DecorateCommand command = DecorateCommand.None;

                switch (info.ButtonID)
                {
                    case 1: command = DecorateCommand.Turn; break;
                    case 2: command = DecorateCommand.Up; break;
                    case 3: command = DecorateCommand.Down; break;
                }

                if (command != DecorateCommand.None)
                {
                    m_Decorator.Command = command;
                    sender.Mobile.Target = new InternalTarget(m_Decorator);
                }
            }
        }

        private class InternalTarget : Target
        {
            private InteriorDecorator m_Decorator;

            public InternalTarget(InteriorDecorator decorator)
                : base(-1, false, TargetFlags.None)
            {
                CheckLOS = false;

                m_Decorator = decorator;
            }

            protected override void OnTargetNotAccessible(Mobile from, object targeted)
            {
                OnTarget(from, targeted);
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted == m_Decorator)
                {
                    m_Decorator.Command = DecorateCommand.None;
                    from.SendGump(new InternalGump(m_Decorator));
                }
                else if (targeted is Item && InteriorDecorator.CheckUse(m_Decorator, from))
                {
                    BaseHouse house = BaseHouse.FindHouseAt(from);
                    Item item = (Item)targeted;

                    if (house == null || !house.IsCoOwner(from))
                    {
                        from.SendLocalizedMessage(502092); // You must be in your house to do this.
                    }
                    else if (item.Parent != null || !house.IsInside(item))
                    {
                        from.SendLocalizedMessage(1042270); // That is not in your house.
                    }
                    else if (!house.IsLockedDown(item) && !house.IsSecure(item) && !(item is BaseHouseDoor) && !(item is AddonComponent && (item as AddonComponent).Deco))
                    {
                        from.SendLocalizedMessage(1042271); // That is not locked down.
                    }
                    else if ((item is BaseHouseDoor) && !house.IsOwner(from))
                    {
                        from.SendMessage("Only the owner can turn doors.");
                    }
                    else if (item is BaseHouseDoor && m_Decorator.Command != DecorateCommand.Turn)
                    {
                        from.SendMessage("That can only be rotated.");
                    }
                    else if (item.TotalWeight + item.PileWeight > 100)
                    {
                        from.SendLocalizedMessage(1042272); // That is too heavy.
                    }
                    else
                    {
                        switch (m_Decorator.Command)
                        {
                            case DecorateCommand.Up: Up(item, from); break;
                            case DecorateCommand.Down: Down(item, from); break;
                            case DecorateCommand.Turn: Turn(item, from); break;
                        }
                    }
                }
            }

            private static void Turn(Item item, Mobile from)
            {
                FlipableAttribute[] attributes = (FlipableAttribute[])item.GetType().GetCustomAttributes(typeof(FlipableAttribute), false);

                if (attributes.Length > 0)
                {
                    if (item is BaseHouseDoor) // If the front door is turned on a private house, it can lock people in instead of out.
                        from.SendMessage("Turning house doors will affect how they lock. Please double-check your house security.");
                    attributes[0].Flip(item);
                }
                else
                    from.SendLocalizedMessage(1042273); // You cannot turn that.
            }

            private static void Up(Item item, Mobile from)
            {
                int floorZ = GetFloorZ(item);

                if (floorZ > int.MinValue && item.Z < (floorZ + 15)) // Confirmed : no height checks here
                    item.Location = new Point3D(item.Location, item.Z + 1);
                else
                    from.SendLocalizedMessage(1042274); // You cannot raise it up any higher.
            }

            private static void Down(Item item, Mobile from)
            {
                int floorZ = GetFloorZ(item);

                if (floorZ > int.MinValue && item.Z > GetFloorZ(item))
                    item.Location = new Point3D(item.Location, item.Z - 1);
                else
                    from.SendLocalizedMessage(1042275); // You cannot lower it down any further.
            }

            private static int GetFloorZ(Item item)
            {
                Map map = item.Map;

                if (map == null)
                    return int.MinValue;

                Tile[] tiles = map.Tiles.GetStaticTiles(item.X, item.Y, true);

                int z = int.MinValue;

                for (int i = 0; i < tiles.Length; ++i)
                {
                    Tile tile = tiles[i];
                    ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                    int top = tile.Z; // Confirmed : no height checks here

                    if (id.Surface && !id.Impassable && top > z && top <= item.Z)
                        z = top;
                }

                return z;
            }
        }
    }
}
