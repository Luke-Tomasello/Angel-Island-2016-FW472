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

/* Scripts/Items/Containers/Container.cs
 * ChangeLog:
 *	7/11/10, Adam
 *		replace local copy of memory system with the common implementation in Utility.Memory
 *	5/23/10, Adam
 *		Add the notion of per-player lift memory.
 *		In Ransom chests, dungeon chests, and treasuremap chests we use this memory to halt/slow lift macros
 *	01/23/07, Pix
 *		In BaseContainer.OnItemLifted: needed to call the base class's OnItemLifted.
 *  01/07.07, Kit
 *      Reverted changes below
 *      Added virtual ManageLockDowns() to IBlockRazorSearch.
 *      override IsLockedDown set to call ManageLockdowns
 *  12/24/06, Adam
 *      In TryDropItem() reset the 'ready state' of IBlockRazorSearch.
 *      We do this to work around the funny way the razor executes OnDoubleClick() on item IDs
 *      that look like containers when dropped into a players backpack  
 *      Case: Drop into a players paperdoll-backpack
 *  12/22/06, Adam
 *      heavy commenting of the IBlockRazorSearch.
 *  12/21/06, Adam
 *      In OnDragDropInto() reset the 'ready state' of IBlockRazorSearch.
 *      We do this to work around the funny way razor executes OnDoubleClick() on item IDs
 *      that look like containers when dropped into a players backpack
 *      Case: Drop into a players open backpack
 *	10/19/06, Adam
 *		Record OnItemLifted() if the item is looted from a non-friend of the house
 *		(exploit audit trail)
 *  8/13/06, Rhiannon
 *		Changed OnDoubleClick to set range of 3 if container is inside a VendorBackpack.
 *  8/05/06, Taran Kain
 *		Modified BaseContainer.TryDropItem to re-use Container.TryDropItem code - was duplicated.
 *	6/7/05, erlein
 *		Added LOS check in BaseContainer to prevent access from behind walls.
 *	5/9/05, Adam
 *		Push the Deco flag down to the core Container level
 *	3/26/05, Adam
 *		Add SmallBasket
 *	2/28/05, Adam
 *		DefaultMaxWeight() added conditional (pm.AccessLevel > AccessLevel.Player) to
 *		allow players staff to carry unlimited weight.
 *	2/26/05, mith
 *		DefaultMaxWeight() Added conditional in case the Container is a player backpack (if Parent is Mobile)
 *			Since player backpacks are not movable, they had no weight limits.
 *	02/24/05, mith
 *		ClosedBarrel created. Weight set to 6 stones to make them stealable with moderate difficulty.
 *  02/17/05, mith
 *		CheckHold()	Override the Core.Container.CheckHold method, copied code from that method
 *			minus the CanStore checks. This enables players to drop items in public containers.
 *		MaxDefaultWeight() Added check if container is movable. That's how it worked in RC0.
 *			This changed in the core with 1.0.0, so we've modified it here instead.
 *		MaxDefaultWeight() put this property back in so that secures no longer have a max weight.
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *	9/24/04, Adam
 *		Push the deco support all the way down to BaseContainer
 *		Remove explicit support for Deco from WoodenBox
 *	9/21/04, Adam
 *		New version of WoodenBox (Version 1)
 *		WoodenBox now supports the new Deco attribute for decorative containers.
 *	9/1/04, Pixie
 *		Added TinkerTrapableAttribute so we can mark containers as tinkertrapable or not.
 *	6/24/04, Pix
 *		Fixed dropping items onto closed lockeddown containers in houses locking down
 *		the items dropped.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    public abstract class BaseContainer : Container
    {

        private Memory m_LiftMemory = new Memory();
        protected Memory LiftMemory { get { return m_LiftMemory; } }

        // record both non-house friends and staff looting items
        public override void OnItemLifted(Mobile from, Item item)
        {
            try
            {
                BaseHouse house = BaseHouse.FindHouseAt(item);
                if (house != null)
                {
                    bool bRecord = (house.IsFriend(from) == false || from.AccessLevel > AccessLevel.Player);
                    if (Movable == false && this.RootParent as Mobile == null && bRecord)
                    {
                        string text = String.Format("Looting: Non friend of house lifting item {0} from {1}.", item.Serial, this.Serial);
                        RecordCheater.TrackIt(from, text, true);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Error in Container.OnItemLifted(): " + e.Message);
                Console.WriteLine(e.StackTrace.ToString());
            }

            //REMEMBER TO CALL THE BASE FUNCTION!!!!!!!!!!
            base.OnItemLifted(from, item);
        }

        public override int DefaultMaxWeight
        {
            get
            {
                if (Parent is PlayerMobile)
                {
                    PlayerMobile pm = Parent as PlayerMobile;
                    if (pm != null && pm.AccessLevel > AccessLevel.Player)
                        return 0;
                    else
                        return base.DefaultMaxWeight;
                }

                if (IsSecure || !Movable)
                    return 0;

                return base.DefaultMaxWeight;
            }
        }

        public BaseContainer(Serial serial)
            : base(serial)
        {

        }

        public BaseContainer(int itemID)
            : base(itemID)
        {

        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (!BaseHouse.CheckAccessible(m, this))
                return false;

            return base.IsAccessibleTo(m);
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            // is in a house, check housing rules
            if (!BaseHouse.CheckHold(m, this, item, message, checkItems))
                return false;

            //return base.CheckHold( m, item, message, checkItems );
            int maxItems = this.MaxItems;

            if (checkItems && maxItems != 0 && (this.TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems)
            {
                if (message)
                    SendFullItemsMessage(m, item);

                return false;
            }
            else
            {
                int maxWeight = this.MaxWeight;

                if (maxWeight != 0 && (this.TotalWeight + plusWeight + item.TotalWeight + item.PileWeight) > maxWeight)
                {
                    if (message)
                        SendFullWeightMessage(m, item);

                    return false;
                }
            }
            object parent = this.Parent;

            while (parent != null)
            {
                if (parent is Container)
                    return ((Container)parent).CheckHold(m, item, message, checkItems, plusItems, plusWeight);
                else if (parent is Item)
                    parent = ((Item)parent).Parent;
                else
                    break;
            }

            return true;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        //This is called when an item is placed into a closed container
        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            // Adam: Disallow dropping on a closed Deco container
            if (base.Deco)
                return false;

            return base.TryDropItem(from, dropped, sendFullMessage);
        }


        //This is called when an item is placed into an open container
        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            // Adam: Disallow dropping on an open Deco container
            if (base.Deco)
                return false;

            if (!CheckHold(from, item, true, true))
                return false;

            //			BaseHouse house = BaseHouse.FindHouseAt( this );

            //			if ( house != null && house.IsLockedDown( this ) && !house.LockDown( from, item, false ) )
            //				return false;

            item.Location = new Point3D(p.X, p.Y, 0);
            AddItem(item);

            from.SendSound(GetDroppedSound(item), GetWorldLocation());

            return true;
        }

        // Override added to perform LOS check and prevent unauthorized viewiing
        // of locked down containers in houses
        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InLOS(this))
            {
                from.SendLocalizedMessage(500237); // Target can not be seen.
                return;
            }

            int range = 0;

            if (this.RootParent is VendorBackpack)
                range = 3;
            else
                range = 2;

            if (from.AccessLevel > AccessLevel.Player || from.InRange(this.GetWorldLocation(), range))
                Open(from);
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }

        public virtual void Open(Mobile from)
        {
            DisplayTo(from);
        }

        /* Note: base class insertion; we cannot serialize anything here */
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }

    public class StrongBackpack : Backpack
    {
        [Constructable]
        public StrongBackpack()
        {
            Layer = Layer.Backpack;
            Weight = 3.0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int MaxWeight { get { return 1600; } }

        public override bool CheckContentDisplay(Mobile from)
        {
            object root = this.RootParent;

            if (root is BaseCreature && ((BaseCreature)root).Controlled && ((BaseCreature)root).ControlMaster == from)
                return true;

            return base.CheckContentDisplay(from);
        }

        public StrongBackpack(Serial serial)
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

    public class Backpack : BaseContainer, IDyable
    {
        public override int DefaultGumpID { get { return 0x3C; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(44, 65, 142, 94); }
        }

        [Constructable]
        public Backpack()
            : base(0x9B2)
        {
            Layer = Layer.Backpack;
            Weight = 3.0;
        }

        public Backpack(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted) return false;

            Hue = sender.DyedHue;

            return true;
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

    public class Pouch : TrapableContainer
    {
        public override int DefaultGumpID { get { return 0x3C; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(44, 65, 142, 94); }
        }

        [Constructable]
        public Pouch()
            : base(0xE79)
        {
            Weight = 1.0;
        }

        public Pouch(Serial serial)
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

    public abstract class BaseBagBall : BaseContainer, IDyable
    {
        public override int DefaultGumpID { get { return 0x3D; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(29, 34, 108, 94); }
        }

        public BaseBagBall(int itemID)
            : base(itemID)
        {
            Weight = 1.0;
        }

        public BaseBagBall(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
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

    public class SmallBagBall : BaseBagBall
    {
        [Constructable]
        public SmallBagBall()
            : base(0x2256)
        {
        }

        public SmallBagBall(Serial serial)
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

    public class LargeBagBall : BaseBagBall
    {
        [Constructable]
        public LargeBagBall()
            : base(0x2257)
        {
        }

        public LargeBagBall(Serial serial)
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

    public class Bag : BaseContainer, IDyable
    {
        public override int DefaultGumpID { get { return 0x3D; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(29, 34, 108, 94); }
        }

        [Constructable]
        public Bag()
            : base(0xE76)
        {
            Weight = 2.0;
        }

        public Bag(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted) return false;

            Hue = sender.DyedHue;

            return true;
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

    public class Barrel : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        [Constructable]
        public Barrel()
            : base(0xE77)
        {
            Weight = 25.0;
        }

        public Barrel(Serial serial)
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
                Weight = 25.0;
        }
    }

    public class Keg : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        [Constructable]
        public Keg()
            : base(0xE7F)
        {
            Weight = 15.0;
        }

        public Keg(Serial serial)
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

    public class PicnicBasket : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3F; } }
        public override int DefaultDropSound { get { return 0x4F; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(19, 47, 163, 76); }
        }

        [Constructable]
        public PicnicBasket()
            : base(0xE7A)
        {
            Weight = 2.0; // Stratics doesn't know weight
        }

        public PicnicBasket(Serial serial)
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

    public class Basket : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x41; } }
        public override int DefaultDropSound { get { return 0x4F; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(35, 38, 110, 78); }
        }

        [Constructable]
        public Basket()
            : base(0x990)
        {
            Weight = 1.0; // Stratics doesn't know weight
        }

        public Basket(Serial serial)
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

    public class SmallBasket : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x41; } }
        public override int DefaultDropSound { get { return 0x4F; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(35, 38, 110, 78); }
        }

        [Constructable]
        public SmallBasket()
            : base(0x9B1)
        {
            Weight = 1.0; // Stratics doesn't know weight
        }

        public SmallBasket(Serial serial)
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

    [Furniture]
    [TinkerTrapable]
    [Flipable(0x9AA, 0xE7D)]
    public class WoodenBox : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x43; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(16, 51, 168, 73); }
        }

        [Constructable]
        public WoodenBox()
            : base(0x9AA)
        {
            Weight = 4.0;
        }

        public WoodenBox(Serial serial)
            : base(serial)
        {

        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);       // version

            writer.Write((bool)true);   // Not Used - available
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:                     // Not Used - available
                    bool dummy = reader.ReadBool();
                    goto case 0;
                case 0:
                    break;
            }
        }
    }

    [Furniture]
    [TinkerTrapable]
    [Flipable(0x9A9, 0xE7E)]
    public class SmallCrate : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        [Constructable]
        public SmallCrate()
            : base(0x9A9)
        {
            Weight = 2.0;
        }

        public SmallCrate(Serial serial)
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

            if (Weight == 4.0)
                Weight = 2.0;
        }
    }

    [Furniture]
    [TinkerTrapable]
    [Flipable(0xE3F, 0xE3E)]
    public class MediumCrate : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        [Constructable]
        public MediumCrate()
            : base(0xE3F)
        {
            Weight = 2.0;
        }

        public MediumCrate(Serial serial)
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

            if (Weight == 6.0)
                Weight = 2.0;
        }
    }

    [Furniture]
    [TinkerTrapable]
    [FlipableAttribute(0xe3c, 0xe3d)]
    public class LargeCrate : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        [Constructable]
        public LargeCrate()
            : base(0xE3C)
        {
            Weight = 1.0;
        }

        public LargeCrate(Serial serial)
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

            if (Weight == 8.0)
                Weight = 1.0;
        }
    }

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0x9A8, 0xE80)]
    public class MetalBox : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x4B; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(16, 51, 168, 73); }
        }

        [Constructable]
        public MetalBox()
            : base(0x9A8)
        {
            Weight = 3.0; // TODO: Real weight
        }

        public MetalBox(Serial serial)
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

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0x9AB, 0xE7C)]
    public class MetalChest : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x4A; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public MetalChest()
            : base(0x9AB)
        {
            Weight = 25.0; // TODO: Real weight
        }

        public MetalChest(Serial serial)
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

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0xE41, 0xE40)]
    public class MetalGoldenChest : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public MetalGoldenChest()
            : base(0xE41)
        {
            Weight = 25.0; // TODO: Real weight
        }

        public MetalGoldenChest(Serial serial)
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

    [Furniture]
    [TinkerTrapable]
    [Flipable(0xe43, 0xe42)]
    public class WoodenChest : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x49; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public WoodenChest()
            : base(0xe43)
        {
            Weight = 15.0; // TODO: Real weight
        }

        public WoodenChest(Serial serial)
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

    [Furniture]
    [Flipable(0x280D, 0x280E)]
    public class OrnateWoodenChest : LockableContainer
    {
        [Constructable]
        public OrnateWoodenChest()
            : base(0x280D)
        {
        }

        public OrnateWoodenChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version == 0 && Weight == 15)
                Weight = -1;
        }
    }

    public class ClosedBarrel : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        [Constructable]
        public ClosedBarrel()
            : base(0xFAE)
        {
            Weight = 6.0;
        }

        public ClosedBarrel(Serial serial)
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
                Weight = 6.0;
        }
    }
}
