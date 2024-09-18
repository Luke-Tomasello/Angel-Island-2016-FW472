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

/* Items/Misc/KeyRing.cs
 * ChangeLog:
 *  9/11/2024, Adam
 *      1. Add OnAfterDelete() override to delete keys
 *      2. Set/Clear IsIntMapStorage when adding/removing keys
 *	3/16/11,
 *		move the keys from the 'container' (old implementation) to the key list.
 *		we do this because when Razor sees an item.Items.Count > 0, it assumes it's a container and tries to inventory it
 * 		this inventory action causes the double click action which invokes the target cursor (ugly)
 *  5/19/07, Adam
 *      Add support for lockable private houses
 *	11/05/04, Darva
 *			Fixed locking public houses error message.
 *	10/29/04 - Pix
 *		Made keys with keyvalue 0 (blank) not lock/unlock doors when they're on keyrings.
 *    10/23/04, Darva
 *			Added code to prevent locking public houses, but allow currently locked public
 *			houses to be unlocked.
 *	9/4/04, mith
 *		OnDragDrop(): Copied Else block from Spellbook, to prevent people dropping things on book to have it bounce back to original location.
 *	8/26/04, Pix
 *		Made it so keys and keyrings must be in your pack to use.
 *	6/24/04, Pix
 *		KeyRing change - contained keys don't decay (set to non-movable when put on keyring,
 *		and movable when taken off).
 *		Also - GM+ can view the contents of a keyring.
 *	5/18/2004
 *		Added handling of (un)locking/(dis)abling of tinker made traps
 *	5/02/2004, pixie
 *		Changed to be a container...
 *		Now you can doubleclick the keyring, target the keyring, and it'll dump all the keys
 *		into your pack akin to OSI.
 *   4/26/2004, pixie
 *     Initial Version
 */

#define current

using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{
    /// <summary>
    /// Summary description for KeyRing.
    /// </summary>
    public class KeyRing : BaseContainer
    {
        private const int ZEROKEY_ITEMID = 0x1011;
        private const int ONEKEY_ITEMID = 0x1769;
        private const int THREEKEY_ITEMID = 0x176A;
        private const int MANYKEY_ITEMID = 0x176B;
        private const int MAX_KEYS = 20;

        private List<Key> m_Keys = new List<Key>();
        public List<Key> Keys { get { return m_Keys; } }

        [Constructable]
        public KeyRing()
            : base(0x1011)
        {
            Weight = 1;
        }

        public KeyRing(Serial serial)
            : base(serial)
        {
        }
        public override void OnAfterDelete()
        {
            foreach (Key key in Keys)
                key.Delete();
        }
        public bool IsKeyOnRing(uint keyid)
        {
            bool bReturn = false;

            foreach (Key i in m_Keys)
            {
                if (i is Key)
                {
                    Key k = i;
                    if (keyid == k.KeyValue)
                    {
                        bReturn = true;
                        break;
                    }
                }
            }

            return bReturn;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxRange
        {
            get { return 3; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count
        {
            get { return m_Keys.Count; }
        }

        public void UpdateItemID()
        {
            if (Count == 0)
                ItemID = ZEROKEY_ITEMID;
            else if (Count == 1 || Count == 2)
                ItemID = ONEKEY_ITEMID;
            else if (Count == 3 || Count == 4)
                ItemID = THREEKEY_ITEMID;
            else if (Count > 4)
                ItemID = MANYKEY_ITEMID;
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {

            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
                return false;
            }

            bool bReturn = false;
            Key key = item as Key;

            if (key == null || key.KeyValue == 0)
            {
                from.SendLocalizedMessage(501689); // Only non-blank keys can be put on a keyring.
                return false;
            }
            else if (Count < MAX_KEYS)
            {
                key.MoveItemToIntStorage();
                m_Keys.Add(key);
                UpdateItemID();
                from.SendLocalizedMessage(501691); // You put the key on the keyring.
                bReturn = true;
            }
            else
            {
                from.SendLocalizedMessage(1008138); // This keyring is full.
                bReturn = false;
            }

            return bReturn;
        }

        public override void OnDoubleClick(Mobile from)
        {

            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            Target t;

            if (Count > 0)
            {
                t = new RingUnlockTarget(this);
                from.SendLocalizedMessage(501680); // What do you want to unlock?
                from.Target = t;
            }
            else
            {
                from.SendMessage("The keyring contains no keys");
            }
        }

        public override bool DisplaysContent { get { return false; } }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            string descr = "";
            if (Count == 1)
            {
                descr = string.Format("{0} key", Count);
            }
            else if (Count > 1)
            {
                descr = string.Format("{0} keys", Count);
            }
            else
            {
                descr = "Empty";
            }

            this.LabelTo(from, descr);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string descr = "";
            if (Count == 1)
            {
                descr = string.Format("{0} key", Count);
            }
            else if (Count > 1)
            {
                descr = string.Format("{0} keys", Count);
            }
            else
            {
                descr = "Empty";
            }

            if (descr != null)
                list.Add(descr);
        }

        public override bool CanFreezeDry { get { return true; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version of new keyring

            // version 1
            writer.WriteItemList<Key>(m_Keys);

            // version 0 (obsolete in version 1)
            // writer.Write((int)m_MaxRange);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Keys = reader.ReadStrongItemList<Key>();
                        // no goto 0 here as the reading of m_MaxRange is now obsolete
                        break;
                    }

                case 0:
                    {
                        int m_MaxRange = reader.ReadInt();  // obsolete in version 1

                        // move the keys from the 'container' (old implementation) to the key list.
                        //	we do this because when Razor sees an item.Items.Count > 0, it assumes it's a container and tries to inventory it
                        //	this inventory action causes the double click action which invokes the target cursor (ugly)
                        if (this.Items != null)
                        {
                            Item[] keys = this.FindItemsByType(typeof(Key));
                            foreach (Key key in keys)
                            {
                                key.Movable = true;         // old implementation had these movable = false
                                this.RemoveItem(key);
                                this.m_Keys.Add(key);
                            }
                        }

                        break;
                    }
            }
        }


        private class RingUnlockTarget : Target
        {
            private KeyRing m_KeyRing;

            public RingUnlockTarget(KeyRing keyring)
                : base(keyring.MaxRange, false, TargetFlags.None)
            {
                m_KeyRing = keyring;
                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {

                if (m_KeyRing.Deleted || !m_KeyRing.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    return;
                }

                int number;

                if (targeted == m_KeyRing)
                {
                    number = -1;
                    //remove keys from keyring
                    List<Key> list = new List<Key>(m_KeyRing.Keys);
                    foreach (Key i in list)
                    {
                        if (i is Key)
                        {
                            if (from is PlayerMobile)
                            {
                                Container b = ((PlayerMobile)from).Backpack;
                                if (b != null)
                                {
                                    if (m_KeyRing.Keys.Contains(i))
                                        m_KeyRing.Keys.Remove(i);
                                    i.IsIntMapStorage = false;
                                    b.DropItem(i);
                                }
                            }
                        }
                    }
                    m_KeyRing.UpdateItemID();
                    from.SendMessage("You remove all the keys.");
                }
                else if (targeted is ILockable)
                {
                    number = -1;
                    ILockable o = (ILockable)targeted;

                    if (m_KeyRing.IsKeyOnRing(o.KeyValue) && o.KeyValue != 0)
                    {
                        if (o is BaseDoor && !((BaseDoor)o).UseLocks())
                        {
                            //number = 501668;	// This key doesn't seem to unlock that.
                            number = 1008140;   // You do not have a key for that.
                        }
                        else
                        {

                            #region PUBLIC HOUSE (disabled)
                            /*if (o is BaseHouseDoor)
							{
								BaseHouse home;
								home = ((BaseHouseDoor)o).FindHouse();
								if (home.Public == true)
								{
									if (o.Locked != true)
									from.SendMessage("You cannot lock a public house.");
									o.Locked = false;
									return;
								}
							}*/
                            #endregion

                            o.Locked = !o.Locked;

                            if (o is LockableContainer)
                            {
                                LockableContainer cont = (LockableContainer)o;

                                if (PublishInfo.Publish < 4 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
                                {   // old-school traps (< publish 4)
                                    if (cont.TrapEnabled)
                                    {
                                        from.SendMessage("You leave the trap enabled.");
                                    }
                                    else
                                    {   // only give a message if trapped (even if it's disabled.)
                                        if (cont.TrapType != TrapType.None)
                                            from.SendMessage("You leave the trap disabled.");
                                    }
                                }
                                else
                                {   // new-style traps (>= publish 4)
                                    if (cont.TrapType != TrapType.None)
                                    {
                                        if (cont.Locked)
                                        {
                                            if (Core.NewStyleTinkerTrap)    // last person to lock trap is 'owner'
                                                cont.Owner = from;

                                            cont.TrapEnabled = true;
                                            (o as LockableContainer).SendLocalizedMessageTo(from, 501673); // You re-enable the trap.
                                        }
                                        else
                                        {
                                            cont.TrapEnabled = false;
                                            (o as LockableContainer).SendLocalizedMessageTo(from, 501672); // You disable the trap temporarily.  Lock it again to re-enable it.
                                        }
                                    }
                                }

                                if (cont.LockLevel == -255)
                                    cont.LockLevel = cont.RequiredSkill - 10;
                            }

                            if (targeted is Item)
                            {
                                Item item = (Item)targeted;

                                if (o.Locked)
                                    item.SendLocalizedMessageTo(from, 1048000);
                                else
                                    item.SendLocalizedMessageTo(from, 1048001);
                            }
                        }
                    }
                    else
                    {
                        //number = 501668;	// This key doesn't seem to unlock that.
                        number = 1008140;   // You do not have a key for that.
                    }
                }
                else
                {
                    number = 501666; // You can't unlock that!
                }

                if (number != -1)
                {
                    from.SendLocalizedMessage(number);
                }
            }
        }//end RingUnlockTarget

    }
}
