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

/* Scripts/Items/Addons/BaseAddonDeed.cs
 * ChangeLog
 *  9/03/06 Taran Kain
 *		Changed targeting process, removed BlockingDoors().
 *  9/01/06 Taran Kain
 *		Added call to new BaseAddon.OnPlaced() hook
 *	5/17/05, erlein
 *		Altered BlockingObject() to perform single test instance instead of one per addon type
 *		that doesn't block.
 *	9/19/04, mith
 *		InternalTarget.OnTarget(): Added call to ConfirmAddonPlacementGump to allow user to cancel placement without losing original deed.
 *	9/18/04, Adam
 *		Add the new function BlockingObject() to determine if something would block the door.
 * 		Pass the result of BlockingObject() to addon.CouldFit().
 *  8/5/04, Adam
 * 		Changed item to LootType.Regular from LootType.Newbied.
 */

using Server.Gumps;
using Server.Targeting;
using System.Collections;

namespace Server.Items
{
    [Flipable(0x14F0, 0x14EF)]
    public abstract class BaseAddonDeed : Item
    {
        public abstract BaseAddon Addon { get; }

        public BaseAddonDeed()
            : base(0x14F0)
        {
            Weight = 1.0;

            if (!Core.RuleSets.AOSRules())
                LootType = LootType.Regular;
        }

        public BaseAddonDeed(Serial serial)
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

            if (Weight == 0.0)
                Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.SendMessage("Target where thy wouldst build thy addon, or target thyself to build it where thou'rt standing.");
                from.Target = new InternalTarget(this);
            }
            else
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }

        public void Place(IPoint3D p, Map map, Mobile from)
        {
            if (p == null || map == null || this.Deleted)
                return;

            if (IsChildOf(from.Backpack))
            {
                BaseAddon addon = Addon; // this creates an instance, don't use Addon (capital A) more than once!

                Server.Spells.SpellHelper.GetSurfaceTop(ref p);

                ArrayList houses = null;

                AddonFitResult res = addon.CouldFit(addon.BlocksDoors, p, map, from, ref houses);

                if (res == AddonFitResult.Valid)
                    addon.MoveToWorld(new Point3D(p), map);
                else if (res == AddonFitResult.Blocked)
                    from.SendLocalizedMessage(500269); // You cannot build that there.
                else if (res == AddonFitResult.NotInHouse)
                    from.SendLocalizedMessage(500274); // You can only place this in a house that you own!
                else if (res == AddonFitResult.DoorsNotClosed)
                    from.SendMessage("You must close all house doors before placing this.");
                else if (res == AddonFitResult.DoorTooClose)
                    from.SendLocalizedMessage(500271); // You cannot build near the door.

                if (res == AddonFitResult.Valid)
                {
                    Delete();

                    if (houses != null)
                    {
                        foreach (Server.Multis.BaseHouse h in houses)
                        {
                            h.Addons.Add(addon);
                            addon.OnPlaced(from, h);
                        }

                        from.SendGump(new ConfirmAddonPlacementGump(from, addon));
                    }
                }
                else
                {
                    addon.Delete();
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        private class InternalTarget : Target
        {
            private BaseAddonDeed m_Deed;
            private bool m_SecondTime;
            private Point3D m_Point;

            public InternalTarget(BaseAddonDeed deed)
                : this(deed, false, Point3D.Zero)
            {
            }

            private InternalTarget(BaseAddonDeed deed, bool secondtime, Point3D point)
                : base(-1, true, TargetFlags.None)
            {
                m_Deed = deed;
                m_SecondTime = secondtime;
                m_Point = point;

                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == targeted)
                {
                    if (m_SecondTime)
                    {
                        m_Deed.Place(m_Point as IPoint3D, from.Map, from);

                        return;
                    }
                    else
                    {
                        from.Target = new InternalTarget(m_Deed, true, from.Location);
                        from.SendMessage("Now, walk thyself away from this place and target thyself again.");

                        return;
                    }
                }

                m_Deed.Place(targeted as IPoint3D, from.Map, from);
            }
        }
    }

    public class AddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new Addon(m_itemID); } }
        int m_itemID;

        [Constructable]
        public AddonDeed(int itemID, Direction dir)
            : base()
        {
            m_itemID = itemID;
            ItemData id = TileData.ItemTable[m_itemID & 0x3FFF];
            Name = string.Format("{0} ({1})", id.Name, dir.ToString().ToLower());
        }

        [Constructable]
        public AddonDeed(int itemID)
            : base()
        {
            m_itemID = itemID;
            ItemData id = TileData.ItemTable[m_itemID & 0x3FFF];
            Name = string.Format("{0}", id.Name);
        }

        public AddonDeed(Serial serial)
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
