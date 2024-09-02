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

/* Items/Body Parts/Head.cs
 * CHANGELOG:
 *	3/6/0, Adam,
 *		Add a notion of FriendlyFire so that we can track when a player was killed by a shared account
 *		(so we can deny a bounty)
 *  01/20/06 Taran Kain
 *		Changed cast in loading PlayerMobile to make sure we're not creating an invalid cast
 *	5/16/04, Pixie
 *		Head now contains information for Bounty system.
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    public class Head : Item, ICarvable
    {
        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(15.0);
            }
        }

        private PlayerMobile m_Player;
        private DateTime m_Created;
        private bool m_FriendlyFire = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FriendlyFire { get { return m_FriendlyFire; } }

        [Constructable]
        public Head()
            : this(null)
        {
            m_Player = null;
            m_Created = DateTime.UtcNow;
        }

        [Constructable]
        public Head(string name)
            : base(0x1DA0)
        {
            Name = name;
            Weight = 1.0;
            m_Player = null;
            m_Created = DateTime.UtcNow;
        }

        public Head(string name, PlayerMobile m)
            : base(0x1DA0)
        {
            Name = name;
            Weight = 2.0;
            m_Player = m;
            m_FriendlyFire = (m != null && m.Corpse != null) ? (m.Corpse as Corpse).FriendlyFire : false;   // were we killed by a shared account?
            m_Created = DateTime.UtcNow;
        }

        public Head(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPlayerHead
        {
            get
            {
                if (m_Player == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Player
        {
            get { return m_Player; }
            set
            {
                m_Player = value;
                Name = String.Format("the head of {0}", m_Player.Name);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Time
        {
            get { return m_Created; }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_FriendlyFire);

            // version 1
            if (m_Player != null)
            {
                writer.Write((int)1);
                writer.Write(m_Player);
            }
            else
            {
                writer.Write((int)0);
            }
            writer.Write(m_Created);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_FriendlyFire = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        int iContainsMobile = reader.ReadInt();
                        if (iContainsMobile == 1)
                        {
                            // don't want to use C-style hard cast here, as ReadMobile can return a valid Mobile that's not a PlayerMobile
                            // as cast will just make it null, in that case
                            // Note from adam: It's also possible that it's a PM that's loaded, but a *different* PM.. don't know how to handle this atm. Not critical.
                            m_Player = reader.ReadMobile() as PlayerMobile;
                        }
                        m_Created = reader.ReadDateTime();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        #region ICarvable Members

        void ICarvable.Carve(Mobile from, Item item)
        {
            Point3D loc = this.Location;
            if (this.ParentContainer != null)
            {
                if (this.ParentMobile != null)
                {
                    if (this.ParentMobile != from)
                    {
                        from.SendMessage("You can't carve that there");
                        return;
                    }

                    loc = this.ParentMobile.Location;
                }
                else
                {
                    loc = this.ParentContainer.Location;
                    if (!from.InRange(loc, 1))
                    {
                        from.SendMessage("That is too far away.");
                        return;
                    }
                }
            }

            //add blood
            Blood blood = new Blood(Utility.Random(0x122A, 5), Utility.Random(15 * 60, 5 * 60));
            blood.MoveToWorld(loc, Map);
            //add brain			//add skull
            if (Player == null)
            {
                if (this.ParentContainer == null)
                {
                    new Brain().MoveToWorld(loc, Map);
                    new Skull().MoveToWorld(loc, Map);
                }
                else
                {
                    this.ParentContainer.DropItem(new Brain());
                    this.ParentContainer.DropItem(new Skull());
                }
            }
            else
            {
                if (this.ParentContainer == null)
                {
                    new Brain(Player.Name).MoveToWorld(loc, Map);
                    new Skull(Player.Name).MoveToWorld(loc, Map);
                }
                else
                {
                    this.ParentContainer.DropItem(new Brain(Player.Name));
                    this.ParentContainer.DropItem(new Skull(Player.Name));
                }
            }

            this.Delete();
        }

        #endregion
    }
}
