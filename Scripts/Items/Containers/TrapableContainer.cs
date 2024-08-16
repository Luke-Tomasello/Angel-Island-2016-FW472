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

/* Scripts/Items/Containers/TrapableContainer.cs
 * CHANGELOG:
 *	3/28/10, adam
 *		Added an auto-reset mechanism to LocableContainers for resetting the trap and lock after a timeout period.
 *		Note: because of the way trapped containers are untrapped via RemoveTrap (power and traptype are cleared)
 *			the autoreset doesn't kick in until the Locked value is set to false.
 *	2/2/07, Pix
 *		Changed animations for explosion and poison traps to be at acceptable z levels.
 *	3/4/06, Pix
 *		Now staff never trip traps.
 *	5/9/05, Adam
 *		Push the Deco flag down to the Container level
 *		Pack old property from serialization routines.
 *	10/30/04, Darva
 *		Fixed CanDetonate
 *	10/25/04, Pix
 *		Reversed the change to m_Enabled made on 10/19.
 *		Also, now serialize/deserialize the m_Enabled flag.
 *	10/23/04, Darva
 *			Added CanDetonate check, which currently stops the trap from going
 *			off if it's on a vendor.
 *    10/19/04, Darva
 *			Set m_Enabled to false after trap goes off.
 *	9/25/04, Adam
 *		Create Version 3 of TrapableContainers that support the Deco attribute.
 *			Most/many containers are derived from TrapableContainer, so they will all get 
 *			the benefits for free.
 *	9/1/04, Pixie
 *		Added TinkerTrapableAttribute so we can mark containers as tinkertrapable or not.
 *  8/8/04, Pixie
 *		Added functionality for tripping the trap if you fail to disarm it.
 *	5/22/2004, Pixie
 *		made tinker traps one-use only
 *	5/22/2004, Pixie
 *		Tweaked poison trap levels up (now GM tinkers always make lethal poison traps)
 *		Changed so tinker-made traps don't get disabled when they're tripped.
 *		Changed sound effects to the right ones for dart/poison traps
 *  5/18/2004, Pixie
 *		Fixed re-enabling of tinker traps, added values to
 *		serialize/deserialize
 *	5/18/2004, Pixie
 *		Added Handling of tinker traps, added dart and poison traps
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TinkerTrapableAttribute : System.Attribute
    {
        public TinkerTrapableAttribute()
        {
        }
    }

    public enum TrapType
    {
        None,
        MagicTrap,
        ExplosionTrap,
        DartTrap,
        PoisonTrap
    }

    public abstract class TrapableContainer : BaseContainer, ITelekinesisable
    {
        private TrapType m_TrapType;
        private int m_TrapPower;
        private int m_TrapLevel;
        private bool m_Enabled;
        private TrapType m_OldTrapType;
        private int m_OldTrapPower;
        private Mobile m_Trapper = null;        // tinker that will take the murder count (< publish 4)
        private Mobile m_Owner = null;          // last person to lock the chest (>= publish 4)
        private IOBAlignment m_IOBAlignment;    // kin-only access

        /// <summary>
        /// Tinker that trapped this box somewhere other than on the floor of his house or boat deck (< publish 4)
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Trapper
        {
            get
            {
                return m_Trapper;
            }
            set
            {
                m_Trapper = value;
            }
        }

        /// <summary>
        /// Last person to lock the chest (>= publish 4)
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TrapType TrapType
        {
            get
            {
                return m_TrapType;
            }
            set
            {
                m_TrapType = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrapPower
        {
            get
            {
                return m_TrapPower;
            }
            set
            {
                m_TrapPower = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrapLevel
        {
            get
            {
                return m_TrapLevel;
            }
            set
            {
                m_TrapLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        public virtual bool TrapOnOpen { get { return true; } }

        // auto reset traps will call this to store the current settings for restoration later
        public void RememberTrap()
        {
            // rememebr the last trap power for auto-reset functionality
            m_OldTrapPower = m_TrapPower;
            m_OldTrapType = m_TrapType;
        }

        // okay, reset the trap based on stored settings.
        public void ResetTrap()
        {   // don't turn on the enabled flag since disarm trap only changes power and type :\
            m_TrapPower = m_OldTrapPower;
            m_TrapType = m_OldTrapType;
        }

        public TrapableContainer(int itemID)
            : base(itemID)
        {
            m_Enabled = true;
            //m_TinkerMade = false;
        }

        public TrapableContainer(Serial serial)
            : base(serial)
        {
            m_Enabled = true;
            //m_TinkerMade = false;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TrapEnabled
        {
            get
            {
                return m_Enabled && m_TrapType != TrapType.None;
            }
            set
            {
                m_Enabled = value;
            }
        }

        /*[CommandProperty(AccessLevel.GameMaster)]
		public bool TinkerMadeTrap
		{
			get { return m_TinkerMade; }
			set { m_TinkerMade = value; }
		}*/

        private bool CanDetonate(Mobile from)
        {
            //Added to check if trap is allowed to detonate.
            // from is not used currently, but might be necessary
            // in the future.
            object oParent;
            oParent = this.Parent;
            while (oParent != null)
            {
                if (oParent is PlayerVendor)
                    return false;
                if (oParent is BaseContainer)
                    oParent = ((BaseContainer)oParent).Parent;
                else
                    return true;
            }
            return true;
        }

        private void SendMessageTo(Mobile to, int number, int hue)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new Network.MessageLocalized(Serial, ItemID, Network.MessageType.Regular, hue, 3, number, "", ""));
        }

        private void SendMessageTo(Mobile to, string text, int hue)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new Network.UnicodeMessage(Serial, ItemID, Network.MessageType.Regular, hue, 3, "ENU", "", text));
        }

        public virtual bool ExecuteTrap(Mobile from)
        {
            return ExecuteTrap(from, false);
        }

        public bool ExecuteTrap(Mobile from, bool bAutoReset)
        {
            Point3D loc = this.GetWorldLocation();
            Map facet = this.Map;

            if (m_TrapType != TrapType.None && m_Enabled && CanDetonate(from))
            {
                if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    SendMessageTo(from, "That is trapped, but you open it with your godly powers.", 0x3B2);
                    return false;
                }

                switch (m_TrapType)
                {
                    case TrapType.ExplosionTrap:
                        {
                            SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

                            if (from.InRange(loc, 3))
                            {
                                int damage;

                                // RunUO says 10, 30, but stratics says 5, 15
                                // http://replay.waybackmachine.org/20020402172114/http://uo.stratics.com/content/guides/tinkertraps/trapessay.shtml
                                if (m_TrapLevel > 0)
                                    damage = Utility.RandomMinMax(5, 15) * m_TrapLevel;
                                else
                                    damage = m_TrapPower;

                                if (m_Trapper != null && !m_Trapper.Deleted)
                                    from.Aggressors.Add(AggressorInfo.Create(m_Trapper, from, true));

                                AOS.Damage(from, damage, 0, 100, 0, 0, 0);

                                // Your skin blisters from the heat!
                                from.LocalOverheadMessage(Network.MessageType.Regular, 0x2A, 503000);
                            }

                            Effects.SendLocationEffect(loc, facet, 0x36BD, 15, 10);
                            Effects.PlaySound(loc, facet, 0x307);

                            break;
                        }
                    case TrapType.MagicTrap:
                        {
                            if (from.InRange(loc, 1))
                                from.Damage(m_TrapPower);
                            //AOS.Damage( from, m_TrapPower, 0, 100, 0, 0, 0 );

                            Effects.PlaySound(loc, Map, 0x307);

                            Effects.SendLocationEffect(new Point3D(loc.X - 1, loc.Y, loc.Z), Map, 0x36BD, 15);
                            Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y, loc.Z), Map, 0x36BD, 15);

                            Effects.SendLocationEffect(new Point3D(loc.X, loc.Y - 1, loc.Z), Map, 0x36BD, 15);
                            Effects.SendLocationEffect(new Point3D(loc.X, loc.Y + 1, loc.Z), Map, 0x36BD, 15);

                            Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y + 1, loc.Z + 11), Map, 0x36BD, 15);

                            break;
                        }
                    case TrapType.DartTrap:
                        {
                            SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

                            if (from.InRange(loc, 3))
                            {
                                int damage;

                                if (m_TrapLevel > 0)
                                    damage = Utility.RandomMinMax(5, 15) * m_TrapLevel;
                                else
                                    damage = m_TrapPower;

                                if (m_Trapper != null && !m_Trapper.Deleted)
                                    from.Aggressors.Add(AggressorInfo.Create(m_Trapper, from, true));

                                AOS.Damage(from, damage, 100, 0, 0, 0, 0);

                                // A dart imbeds itself in your flesh!
                                from.LocalOverheadMessage(Network.MessageType.Regular, 0x62, 502998);
                            }

                            Effects.PlaySound(loc, facet, 0x223);

                            break;
                        }
                    case TrapType.PoisonTrap:
                        {
                            SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

                            if (from.InRange(loc, 3))
                            {
                                Poison poison;

                                if (m_Trapper != null && !m_Trapper.Deleted)
                                    from.Aggressors.Add(AggressorInfo.Create(m_Trapper, from, true));

                                if (m_TrapLevel > 0)
                                {
                                    poison = Poison.GetPoison(Math.Max(0, Math.Min(4, m_TrapLevel - 1)));
                                }
                                else
                                {
                                    AOS.Damage(from, m_TrapPower, 0, 0, 0, 100, 0);
                                    poison = Poison.Greater;
                                }

                                from.ApplyPoison(from, poison);

                                // You are enveloped in a noxious green cloud!
                                from.LocalOverheadMessage(Network.MessageType.Regular, 0x44, 503004);
                            }

                            Effects.SendLocationEffect(loc, facet, 0x113A, 10, 20);
                            Effects.PlaySound(loc, facet, 0x231);

                            break;
                        }
                }

                // new style tinker traps remain trapped (auto reset)
                if (!bAutoReset)
                    m_TrapType = TrapType.None;

                return true;
            }

            return false;
        }

        //Virtual function so that we can have child classes
        // implement different things based on how that class
        // wants to behave.
        // returns false if nothing happens, otherwise return true
        public virtual bool OnFailDisarm(Mobile from)
        {
            //base class does nothing
            return false;
        }

        public virtual void OnTelekinesis(Mobile from)
        {
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
            Effects.PlaySound(Location, Map, 0x1F5);

            // adam: this checks makes sure that if this is a kin chest, it can only be accessed by those kin
            if (CheckKin(from) == false)
            {
                SendMessageTo(from, "Your alignment prevents you from accessing this container.", 0x3B2);
                return;
            }

            if (!this.TrapOnOpen || !ExecuteTrap(from))
                base.DisplayTo(from);
        }

        public override void Open(Mobile from)
        {
            // adam: this checks makes sure that if this is a kin chest, it can only be accessed by those kin
            if (CheckKin(from) == false)
            {
                SendMessageTo(from, "Your alignment prevents you from accessing this container.", 0x3B2);
                return;
            }

            // new style tinker traps allow the owner to simply open the chest
            if ((from == this.Owner && Core.NewStyleTinkerTrap) || !this.TrapOnOpen || !ExecuteTrap(from))
                base.Open(from);
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (CheckKin(m) == false)
            {
                SendMessageTo(m, "Your alignment prevents you from storing anything here.", 0x3B2);
                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public bool CheckKin(Mobile m)
        {
            // this checks makes sure that if this is a kin chest, it can only be accessed by those kin
            if (m_IOBAlignment != IOBAlignment.None && m is PlayerMobile && (m as PlayerMobile).IOBAlignment != m_IOBAlignment && (m as PlayerMobile).AccessLevel == AccessLevel.Player)
                return false;

            return true;
        }

#if old
		public override void OnDoubleClick(Mobile from)
		{
			if (from.AccessLevel > AccessLevel.Player || from.InRange(this.GetWorldLocation(), 2))
			{
				if (!ExecuteTrap(from))
					base.OnDoubleClick(from);
			}
			else
			{
				from.SendLocalizedMessage(500446); // That is too far away.
			}
		}
#endif

        #region Save Flags
        [Flags]
        enum SaveFlags
        {
            None = 0x0,
            HasOBAlignment = 0x01,
        }

        private SaveFlags m_SaveFlags = SaveFlags.None;

        private void SetFlag(SaveFlags flag, bool value)
        {
            if (value)
                m_SaveFlags |= flag;
            else
                m_SaveFlags &= ~flag;
        }

        private bool GetFlag(SaveFlags flag)
        {
            return ((m_SaveFlags & flag) != 0);
        }

        private SaveFlags ReadSaveFlags(GenericReader reader, int version)
        {
            SaveFlags sf = SaveFlags.None;
            if (version >= 10)
                sf = (SaveFlags)reader.ReadInt();
            return sf;
        }

        private SaveFlags WriteSaveFlags(GenericWriter writer)
        {
            m_SaveFlags = SaveFlags.None;
            SetFlag(SaveFlags.HasOBAlignment, m_IOBAlignment != IOBAlignment.None ? true : false);
            writer.Write((int)m_SaveFlags);
            return m_SaveFlags;
        }
        #endregion Save Flags

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)10);                  // version
            m_SaveFlags = WriteSaveFlags(writer);   // always follows version

            /* begin normal serialization here */

            // version 10
            if (GetFlag(SaveFlags.HasOBAlignment))
                writer.Write((int)m_IOBAlignment);  // kin-only chest

            // version 9
            writer.Write(m_Owner);                  // last person to lock chest (>= publish 4)

            // version 8
            writer.Write(m_Trapper);                // tinker (< publish 4)

            // version 7
            writer.Write((int)m_TrapLevel);

            // version 6
            writer.Write((int)m_OldTrapPower);

            // version 5
            writer.Write((bool)m_Enabled);
            //writer.Write( (bool) false );		// removed in version 5 
            //writer.Write((bool)false);		// m_TinkerMade (obsolete) removed in version 7
            writer.Write((int)m_OldTrapType);
            writer.Write((int)m_TrapPower);
            writer.Write((int)m_TrapType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();                 // version
            m_SaveFlags = ReadSaveFlags(reader, version);   // always follows version

            /* begin normal Deserialize here */

            switch (version)
            {
                case 10:
                    {
                        if (GetFlag(SaveFlags.HasOBAlignment))
                            m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        goto case 9;
                    }
                case 9:
                    {
                        m_Owner = reader.ReadMobile();
                        goto case 8;
                    }
                case 8:
                    {
                        m_Trapper = reader.ReadMobile();
                        goto case 7;
                    }
                case 7:
                    {
                        m_TrapLevel = reader.ReadInt();
                        goto case 6;
                    }
                case 6:
                    {
                        m_OldTrapPower = reader.ReadInt();
                        goto case 5;
                    }
                case 5:
                    {
                        goto case 4;
                    }
                case 4:
                    {
                        m_Enabled = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        if (version < 5)
                            reader.ReadBool();  // deco field
                        goto case 2;
                    }
                case 2:
                    {
                        if (version <= 7)
                            reader.ReadBool();  // m_TinkerMade

                        m_OldTrapType = (TrapType)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_TrapPower = reader.ReadInt();
                        if (version < 6)
                            m_OldTrapPower = m_TrapPower;
                        goto case 0;
                    }

                case 0:
                    {
                        m_TrapType = (TrapType)reader.ReadInt();

                        break;
                    }
            }

            if (version < 7)
            {   // I guess this is reasonable
                // Example: a level 5 trap is TrapPower 5*25, and the TrapLevel is 5
                //	therfore a reasonable m_TrapLevel is TrapPower / 25
                m_TrapLevel = TrapPower / 25;
            }
        }
    }
}
