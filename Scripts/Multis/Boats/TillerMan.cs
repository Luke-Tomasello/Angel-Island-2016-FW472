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

/* Scripts/Multis/Boats/Tillerman.cs
 * ChangeLog
 *	8/25/10, Adam
 *		Add support for map based navigation. (Give tillerman a map.)
 *	3/16/10, Adam
 *		Have the tillerman give us a real message if we get to close to Angel Island
 *		"Ar, I'll not go any nearer to that Angel Island."
 *		We do this by adding another Say() method that displayes our string
 *  7/15/04, Pix
 *		Removed some stuff that always threw compile-time warnings.
 *	6/29/04, Pixie
 *		Added IsClosestPlayer() utility function for boats to determine if
 *		someone can command the boat.
 *	5/19/04, mith
 *		Modified tillerman to give boat's decay state when single-clicked.
 */

using Server.Multis;
using Server.Network;
using System.Collections;

namespace Server.Items
{
    public class TillerMan : Item
    {
        private BaseBoat m_Boat;

        public TillerMan(BaseBoat boat)
            : base(0x3E4E)
        {
            m_Boat = boat;
            Movable = false;
        }

        public TillerMan(Serial serial)
            : base(serial)
        {
        }

        public void SetFacing(Direction dir)
        {
            switch (dir)
            {
                case Direction.South: ItemID = 0x3E4B; break;
                case Direction.North: ItemID = 0x3E4E; break;
                case Direction.West: ItemID = 0x3E50; break;
                case Direction.East: ItemID = 0x3E53; break;
            }
        }

        public void Say(int number)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, number);
        }

        public void Say(int number, string args)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, number, args);
        }

        public void Say(bool ascii, string text)
        {   // make him say what we want
            PublicOverheadMessage(0, 0x3B2, ascii, text);
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Boat != null && m_Boat.ShipName != null)
                list.Add(1042884, m_Boat.ShipName); // the tiller man of the ~1_SHIP_NAME~
            else
                base.AddNameProperty(list);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Boat != null && m_Boat.ShipName != null)
                LabelTo(from, 1042884, m_Boat.ShipName); // the tiller man of the ~1_SHIP_NAME~
            else
                base.OnSingleClick(from);

            LabelTo(from, m_Boat.DecayState());     // This structure is...
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Boat != null && m_Boat.Contains(from))
                m_Boat.BeginRename(from);
            else if (m_Boat != null)
                m_Boat.BeginDryDock(from);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is MapItem && m_Boat != null && m_Boat.CanCommand(from) && m_Boat.Contains(from))
            {
                m_Boat.AssociateMap((MapItem)dropped);
            }

            return false;
        }

        public override void OnAfterDelete()
        {
            if (m_Boat != null)
                m_Boat.Delete();
        }

        public bool IsClosestPlayer(Mobile m)
        {
            bool bClosest = true;

            ArrayList mobileList = new ArrayList();

            int maxrange = 10;
            IPooledEnumerable eable =
                Map.GetObjectsInBounds(
                    new Rectangle2D(X - maxrange / 2,
                                     Y - maxrange / 2,
                                     maxrange,
                                     maxrange));
            foreach (object o in eable)
            {
                if (o is Server.Mobiles.PlayerMobile)
                {
                    mobileList.Add(o);
                }
            }
            eable.Free();

            if (mobileList.Count > 0)
            {
                double shortestDistance = 10;
                foreach (Server.Mobiles.PlayerMobile pm in mobileList)
                {
                    double dist = pm.GetDistanceToSqrt(this);
                    if (dist < shortestDistance)
                    {
                        shortestDistance = dist;
                    }
                }

                if (m.GetDistanceToSqrt(this) > shortestDistance)
                {
                    bClosest = false;
                }
            }

            return bClosest;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);//version

            writer.Write(m_Boat);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Boat = reader.ReadItem() as BaseBoat;

                        if (m_Boat == null)
                            Delete();

                        break;
                    }
            }
        }
    }
}
