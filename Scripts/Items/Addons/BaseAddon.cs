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

/* Scripts/Items/Addons/BaseAddon.cs
 * ChangeLog
 *  10/10/2024, Adam (redeeding)
 *      In attempt to keep addon value high, AI doesn't allow redeeding of addons.
 *      However, in this patch, we allow the addon to be redeeded for the first 30 minutes after placement
 *  3/7/07, Adam
 *      Make OnChop() virtual 
 *  9/03/06 Taran Kain
 *		Added BlocksDoors property.
 *  9/01/06 Taran Kain
 *		Virtualized CouldFit, added OnPlaced hook
 *	06/06/06, Adam
 *		Make AddComponent virtual to allow the override in derived classes.
 *		The immediate need is to make the components being added invisible.
 *	05/12/05, erlein
 *		Changed logic for access level vs addon placement checking to avoid
 *		GM and greater placed addons which are not tied to house they were placed
 *		within.
 *	9/18/04, Adam
 *		Check the new blocking flag in CouldFit() to see something would block the door.
 *		See new function BlockingObject() in BaseAddonDeed.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */
//comented out line 77-103 added lines 73-76 for addon deletetion instead of redeeding

using Server.Multis;
using System;
using System.Collections;

namespace Server.Items
{
    public enum AddonFitResult
    {
        Valid,
        Blocked,
        NotInHouse,
        DoorsNotClosed,
        DoorTooClose
    }

    public abstract class BaseAddon : Item, IChopable
    {
        private ArrayList m_Components;

        public virtual void AddComponent(AddonComponent c, int x, int y, int z)
        {
            if (Deleted)
                return;

            m_Components.Add(c);

            c.Addon = this;
            c.Offset = new Point3D(x, y, z);
            c.MoveToWorld(new Point3D(X + x, Y + y, Z + z), Map);
        }

        public BaseAddon()
            : base(1)
        {
            Movable = false;
            Visible = false;

            m_Components = new ArrayList();
        }

        public virtual bool RetainDeedHue { get { return false; } }

        public virtual bool BlocksDoors { get { return true; } }

        public virtual void OnChop(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsOwner(from) && house.Addons.Contains(this))
            {   // chopping withing the first 30 minutes will allow the addon to be redeeded.
                if (Age > TimeSpan.FromMinutes(30))
                {
                    Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                    from.SendLocalizedMessage(500461); // You destroy the item.

                    Delete();

                    house.Addons.Remove(this);
                }
                else
                {   // redeed

                    Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                    from.SendMessage("You redeed the item.");

                    int hue = 0;

                    if (RetainDeedHue)
                    {
                        for (int i = 0; hue == 0 && i < m_Components.Count; ++i)
                        {
                            AddonComponent c = (AddonComponent)m_Components[i];

                            if (c.Hue != 0)
                                hue = c.Hue;
                        }
                    }

                    Delete();

                    house.Addons.Remove(this);

                    BaseAddonDeed deed = Deed;

                    if (deed != null)
                    {
                        if (RetainDeedHue)
                            deed.Hue = hue;

                        from.AddToBackpack(deed);
                    }
                }
            }
        }

        public virtual BaseAddonDeed Deed { get { return null; } }

        public ArrayList Components
        {
            get
            {
                return m_Components;
            }
        }

        public BaseAddon(Serial serial)
            : base(serial)
        {
        }

        public virtual AddonFitResult CouldFit(bool blocking, IPoint3D p, Map map, Mobile from, ref ArrayList houseList)
        {
            if (Deleted)
                return AddonFitResult.Blocked;

            ArrayList houses = new ArrayList();

            foreach (AddonComponent c in m_Components)
            {
                Point3D p3D = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);
                CanFitFlags flags = CanFitFlags.checkMobiles;
                if (c.Z == 0) flags |= CanFitFlags.requireSurface;
                if (!map.CanFit(p3D.X, p3D.Y, p3D.Z, c.ItemData.Height, flags))
                    return AddonFitResult.Blocked;
                else if (!CheckHouse(from, p3D, map, c.ItemData.Height, houses))
                    return AddonFitResult.NotInHouse;
            }


            foreach (BaseHouse house in houses)
            {
                ArrayList doors = house.Doors;

                for (int i = 0; i < doors.Count; ++i)
                {
                    BaseDoor door = doors[i] as BaseDoor;

                    if (door != null && door.Open)
                        return AddonFitResult.DoorsNotClosed;

                    Point3D doorLoc = door.GetWorldLocation();
                    int doorHeight = door.ItemData.CalcHeight;

                    foreach (AddonComponent c in m_Components)
                    {
                        Point3D addonLoc = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);
                        int addonHeight = c.ItemData.CalcHeight;

                        if (Utility.InRange(doorLoc, addonLoc, 1) && (addonLoc.Z == doorLoc.Z || ((addonLoc.Z + addonHeight) > doorLoc.Z && (doorLoc.Z + doorHeight) > addonLoc.Z)))
                            if (blocking == true)
                                return AddonFitResult.DoorTooClose;
                    }
                }
            }

            houseList = houses;
            return AddonFitResult.Valid;
        }

        public bool CheckHouse(Mobile from, Point3D p, Map map, int height, ArrayList list)
        {
            // erl: this is what's stopping addon cancellation - it's also
            // likely to mess up a few other things (ie. by not adding
            // the addon into the house you're placing within)
            //
            /*
			if ( from.AccessLevel >= AccessLevel.GameMaster )
				return true;
			*/

            BaseHouse house = BaseHouse.FindHouseAt(p, map, height);

            // erl: if house==null but mob >=GM, return true anyway without
            // adding into list
            if (from.AccessLevel >= AccessLevel.GameMaster && house == null)
                return true;

            // erl: if mob<GM and not owner or house is null (which wouldn't have got to
            // *unless* their access level < GM anyway), return false
            if (house == null || (!house.IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster))
                return false;

            // erl: otherwise, we have a house that needs a new addon added to its list,
            // irrespective of mob access level
            if (!list.Contains(house))
                list.Add(house);

            return true;
        }

        public virtual void OnComponentLoaded(AddonComponent c)
        {
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (Deleted)
                return;

            foreach (AddonComponent c in m_Components)
                c.Location = new Point3D(X + c.Offset.X, Y + c.Offset.Y, Z + c.Offset.Z);
        }

        public override void OnMapChange()
        {
            if (Deleted)
                return;

            foreach (AddonComponent c in m_Components)
                c.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            foreach (AddonComponent c in m_Components)
                c.Delete();
        }

        public virtual void OnPlaced(Mobile placer, BaseHouse house)
        {
        }

        public virtual bool ShareHue { get { return true; } }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                if (base.Hue != value)
                {
                    base.Hue = value;

                    if (!Deleted && this.ShareHue && m_Components != null)
                    {
                        foreach (AddonComponent c in m_Components)
                            c.Hue = value;
                    }
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteItemList(m_Components);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Components = reader.ReadItemList();
                        break;
                    }
            }
        }
    }

    public class Addon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new AddonDeed(m_itemID); } }
        int m_itemID;

        public Addon(int itemID)
        {
            m_itemID = itemID;
            AddComponent(new AddonComponent(m_itemID, true), 0, 0, 0);
        }

        public Addon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_itemID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_itemID = reader.ReadInt();
                        break;
                    }
            }

        }
    }
}
