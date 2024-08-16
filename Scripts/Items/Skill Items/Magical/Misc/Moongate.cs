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

/* Scripts/Items/Skill Items/Magical/Misc/Moongate.cs
 *	ChangeLog:
 *	5/14/09, Plasma
 *		Added kinOnly filter
 *	1/7/09, Adam
 *		Remove LootType.Special/Internal from the NOT RestrictedItem() list
 *	7/26/07, Adam
 *		Add the DestinationOverride property.
 *			DestinationOverride lets us go places usually restricted by CheckTravel()
 *			useful for staff created gates.
 *	2/10/06, Adam
 *		1. added item.ItemID == 8270(DeathShroud) to EmptyBackpack exclusion check.
 *		Oddly, we dressing the ghost in an item.ItemID == 8270 and not a DeathShroud.
 *		2. make the Moongate.RestrictedItem() public so it can be reused in AITeleporters as it is the same item list
 * 2/3/06, Pix
 *		Added DeathShroud to EmptyBackpack exclusion check.
 *	1/30/06, Pix
 *		Added TravelRules system as well as the first set of restrictions:
 *		MortalsOnly, GhostsOnly, and EmptyBackpack
 * 1/4/06, Adam
 *		Reverse the 'drop holding' change of 01/03/06
 *		for now, this an allowed means of transportation of heavy objects
 *	01/03/06, Pix
 *		Gate user now drops what he's holding on cursor when he gets teleported.
 * 11/30/04, Pix
 *		Made it so criminals couldn't use moongates (either through doubleclicking
 *		or moving over).
*  6/5/04, Pix
*		Merged in 1.0RC0 code.
 *	5/26/04 smerX
 *		Added functionality for tamed pets
 *		Added another format for the CheckGate() method
 *	4/xx/04 smerX
 *		Removed gate gump
 */

using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells;
using System;

namespace Server.Items
{
    [Flags]
    public enum TravelRules
    {
        None = 0x00000000,
        GhostsOnly = 0x00000001,
        MortalsOnly = 0x00000002,
        EmptyBackpack = 0x00000004,
        DestinationOverride = 0x00000008,
        KinOnly = 0x00000010,
    }

    [DispellableFieldAttribute]
    public class Moongate : Item
    {
        private Point3D m_Target;
        private Map m_TargetMap;
        private bool m_bDispellable;

        #region  Special restrications
        private TravelRules m_SpecialAccess;

        public bool GetSpecialFlag(TravelRules flag)
        {
            return ((m_SpecialAccess & flag) != 0);
        }

        public void SetSpecialFlag(TravelRules flag, bool value)
        {
            if (value)
            {
                if (CheckIllegalFlagCombination(flag))
                {
                    return;
                }
            }

            if (value)
                m_SpecialAccess |= flag;
            else
                m_SpecialAccess &= ~flag;
        }

        private bool CheckIllegalFlagCombination(TravelRules flag)
        {
            TravelRules temp = m_SpecialAccess | flag;

            //can't have GhostOnly AND MortalsOnly
            if ((temp & TravelRules.GhostsOnly) != 0 && (temp & TravelRules.MortalsOnly) != 0)
            {
                return true;
            }

            //No illegal flag combinations, return false
            return false;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GhostsOnly
        {
            get { return GetSpecialFlag(TravelRules.GhostsOnly); }
            set { SetSpecialFlag(TravelRules.GhostsOnly, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool MortalsOnly
        {
            get { return GetSpecialFlag(TravelRules.MortalsOnly); }
            set { SetSpecialFlag(TravelRules.MortalsOnly, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool KinOnly
        {
            get { return GetSpecialFlag(TravelRules.KinOnly); }
            set { SetSpecialFlag(TravelRules.KinOnly, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EmptyBackpack
        {
            get { return GetSpecialFlag(TravelRules.EmptyBackpack); }
            set { SetSpecialFlag(TravelRules.EmptyBackpack, value); }
        }

        /* DestinationOverride
		 *	DestinationOverride lets us go places usually restricted by CheckTravel()
		 *	useful for staff created gates.
		 */
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestinationOverride
        {
            get { return GetSpecialFlag(TravelRules.DestinationOverride); }
            set { SetSpecialFlag(TravelRules.DestinationOverride, value); }
        }

        // items that are allowed to pass (also called for AITeleporters)
        public static bool RestrictedItem(Mobile m, Item item)
        {
            if (item == m.Backpack || item == m.BankBox ||
                item == m.Hair || item == m.Beard ||
                item is DeathShroud || item.ItemID == 8270  // 8270 == deathshroud
                )
                return false;
            else
                return true;
        }

        private bool CheckSpecialRestrictions(Mobile m)
        {
            //Always let staff pass.
            if (m.AccessLevel > AccessLevel.Player) return true;

            //Don't bother checking it if there's no special flags.
            if (m_SpecialAccess == TravelRules.None) return true;

            //Ghosts Only
            if (GhostsOnly)
            {
                if (m is PlayerMobile)
                {
                    if (m.Alive)
                    {
                        m.SendMessage("You are alive, you cannot pass.");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            //Mortals Only
            if (MortalsOnly)
            {
                if (m is PlayerMobile)
                {
                    if (((PlayerMobile)m).Mortal == false)
                    {
                        m.SendMessage("You are not mortal, you cannot pass.");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            //Empty Backpack
            if (EmptyBackpack)
            {
                if (m is PlayerMobile)
                {
                    if (m.Backpack != null && m.Backpack.Items.Count > 0)
                    {
                        m.SendMessage("You have items in your backpack, you cannot pass.");
                        return false;
                    }
                    if (m.Holding != null)
                    {
                        m.SendMessage("You are holding something, you cannot pass.");
                        return false;
                    }
                    if (m.Items != null && m.Items.Count > 0)
                    {
                        foreach (Item it in m.Items)
                        {
                            if (RestrictedItem(m, it) == true)
                            {
                                m.SendMessage("You have items on you, you cannot pass.");
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            //Kin only
            if (KinOnly)
            {
                if (!(m is PlayerMobile)) return false;
                if (((PlayerMobile)m).IOBRealAlignment == IOBAlignment.None)
                {
                    m.SendMessage("You are not Kin aligned, you cannot pass.");
                    return false;
                }
                return true;
            }

            return true;
        }
        #endregion


        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Target
        {
            get
            {
                return m_Target;
            }
            set
            {
                m_Target = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap
        {
            get
            {
                return m_TargetMap;
            }
            set
            {
                m_TargetMap = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Dispellable
        {
            get
            {
                return m_bDispellable;
            }
            set
            {
                m_bDispellable = value;
            }
        }

        public virtual bool ShowFeluccaWarning { get { return false; } }

        [Constructable]
        public Moongate()
            : this(Point3D.Zero, null)
        {
            m_bDispellable = true;
        }

        [Constructable]
        public Moongate(bool bDispellable)
            : this(Point3D.Zero, null)
        {
            m_bDispellable = bDispellable;
        }

        [Constructable]
        public Moongate(Point3D target, Map targetMap)
            : base(0xF6C)
        {
            Movable = false;
            Light = LightType.Circle300;

            m_Target = target;
            m_TargetMap = targetMap;
        }

        public Moongate(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player)
                return;

            if (from.InRange(GetWorldLocation(), 1))
            {
                if (from.Criminal)
                {
                    from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else if (from.CheckState(Mobile.ExpirationFlagID.EvilCrim))
                {
                    from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else
                {
                    CheckGate(from, 1);
                }
            }
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.Player)
            {
                if (m.Criminal)
                {
                    m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else if (m.CheckState(Mobile.ExpirationFlagID.EvilCrim))
                {
                    m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else
                {
                    CheckGate(m, 0, TimeSpan.FromSeconds(1.0));
                }
            }
            else if (m is BaseCreature && ((BaseCreature)m).Controlled)
            {
                CheckGate(m, 0, TimeSpan.Zero);
            }

            return true;
        }

        public virtual void CheckGate(Mobile m, int range)
        {
            new DelayTimer(m, this, range, TimeSpan.FromSeconds(1.0)).Start();
        }

        public virtual void CheckGate(Mobile m, int range, TimeSpan delay)
        {
            new DelayTimer(m, this, range, delay).Start();
        }

        public virtual void OnGateUsed(Mobile m)
        {
        }

        public virtual void UseGate(Mobile m)
        {
            if (CheckSpecialRestrictions(m) == false)
            {
                m.SendMessage("You cannot use this moongate.");
            }
            //else if ( m.Murderer && m_TargetMap != Map.Felucca )
            //{
            //m.SendLocalizedMessage( 1019004 ); // You are not allowed to travel there.
            //}
            else if (m.Spell != null)
            {
                m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
            }
            else if (m is BaseOverland && (m as BaseOverland).GateTravel == false)
            {   // overland mobs are afraid of magic and will not enter!
                BaseOverland bo = m as BaseOverland;
                bo.OnMoongate();
            }
            else if (m_TargetMap != null && m_TargetMap != Map.Internal)
            {
                bool jail;
                // DestinationOverride lets us go places usually restricted by CheckTravel()
                if (DestinationOverride || SpellHelper.CheckTravel(m_TargetMap, m_Target, TravelCheckType.GateTo, m, out jail))
                {
                    BaseCreature.TeleportPets(m, m_Target, m_TargetMap);
                    // Adam: for now, this an allowed means of transportation of heavy objects
                    // m.DropHolding();
                    m.MoveToWorld(m_Target, m_TargetMap);
                    m.PlaySound(0x1FE);
                    OnGateUsed(m);
                }
                else
                {
                    if (jail == true)
                    {
                        Point3D jailCell = new Point3D(5295, 1174, 0);
                        m.MoveToWorld(jailCell, m.Map);
                    }
                    else
                        m.SendMessage("This moongate does not seem to go anywhere.");
                }
            }
            else
            {
                m.SendMessage("This moongate does not seem to go anywhere.");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write(m_Target);
            writer.Write(m_TargetMap);

            // Version 1
            writer.Write(m_bDispellable);

            // Version 2
            writer.Write((int)m_SpecialAccess);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Target = reader.ReadPoint3D();
            m_TargetMap = reader.ReadMap();

            if (version >= 1)
                m_bDispellable = reader.ReadBool();

            if (version >= 2)
                m_SpecialAccess = (TravelRules)reader.ReadInt();
        }

        public virtual bool ValidateUse(Mobile from, bool message)
        {
            if (from.Deleted || this.Deleted)
                return false;

            if (from.Map != this.Map || !from.InRange(this, 1))
            {
                if (message)
                    from.SendLocalizedMessage(500446); // That is too far away.

                return false;
            }

            return true;
        }

        public virtual void BeginConfirmation(Mobile from)
        {
            // removed trammy confirm gump below
            bool bUseConfirmGump = false;

            if (bUseConfirmGump && IsInTown(from.Location, from.Map) && !IsInTown(m_Target, m_TargetMap))
            {
                from.Send(new PlaySound(0x20E, from.Location));
                from.CloseGump(typeof(MoongateConfirmGump));
                from.SendGump(new MoongateConfirmGump(from, this));
            }
            else
            {
                EndConfirmation(from);
            }
        }

        public virtual void EndConfirmation(Mobile from)
        {
            if (!ValidateUse(from, true))
                return;

            UseGate(from);
        }

        public virtual void DelayCallback(Mobile from, int range)
        {
            if (!ValidateUse(from, false) || !from.InRange(this, range))
                return;

            if (m_TargetMap != null)
                BeginConfirmation(from);
            else
                from.SendMessage("This moongate does not seem to go anywhere.");
        }

        public static bool IsInTown(Point3D p, Map map)
        {
            if (map == null)
                return false;

            GuardedRegion reg = Region.Find(p, map) as GuardedRegion;

            return (reg != null && reg.IsGuarded);
        }

        private class DelayTimer : Timer
        {
            private Mobile m_From;
            private Moongate m_Gate;
            private int m_Range;

            public DelayTimer(Mobile from, Moongate gate, int range, TimeSpan delay)
                : base(delay)
            {
                m_From = from;
                m_Gate = gate;
                m_Range = range;
            }

            protected override void OnTick()
            {
                m_Gate.DelayCallback(m_From, m_Range);
            }
        }
    }

    public class ConfirmationMoongate : Moongate
    {
        private int m_GumpWidth;
        private int m_GumpHeight;

        private int m_TitleColor;
        private int m_MessageColor;

        private int m_TitleNumber;
        private int m_MessageNumber;

        private string m_MessageString;

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpWidth
        {
            get { return m_GumpWidth; }
            set { m_GumpWidth = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpHeight
        {
            get { return m_GumpHeight; }
            set { m_GumpHeight = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TitleColor
        {
            get { return m_TitleColor; }
            set { m_TitleColor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageColor
        {
            get { return m_MessageColor; }
            set { m_MessageColor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TitleNumber
        {
            get { return m_TitleNumber; }
            set { m_TitleNumber = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get { return m_MessageNumber; }
            set { m_MessageNumber = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get { return m_MessageString; }
            set { m_MessageString = value; }
        }

        [Constructable]
        public ConfirmationMoongate()
            : this(Point3D.Zero, null)
        {
        }

        [Constructable]
        public ConfirmationMoongate(Point3D target, Map targetMap)
            : base(target, targetMap)
        {
        }

        public ConfirmationMoongate(Serial serial)
            : base(serial)
        {
        }

        public virtual void Warning_Callback(Mobile from, bool okay, object state)
        {
            if (okay)
                EndConfirmation(from);
        }

        public override void BeginConfirmation(Mobile from)
        {
            if (m_GumpWidth > 0 && m_GumpHeight > 0 && m_TitleNumber > 0 && (m_MessageNumber > 0 || m_MessageString != null))
            {
                from.CloseGump(typeof(WarningGump));
                from.SendGump(new WarningGump(m_TitleNumber, m_TitleColor, m_MessageString == null ? (object)m_MessageNumber : (object)m_MessageString, m_MessageColor, m_GumpWidth, m_GumpHeight, new WarningGumpCallback(Warning_Callback), from));
            }
            else
            {
                base.BeginConfirmation(from);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_GumpWidth);
            writer.WriteEncodedInt(m_GumpHeight);

            writer.WriteEncodedInt(m_TitleColor);
            writer.WriteEncodedInt(m_MessageColor);

            writer.WriteEncodedInt(m_TitleNumber);
            writer.WriteEncodedInt(m_MessageNumber);

            writer.Write(m_MessageString);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_GumpWidth = reader.ReadEncodedInt();
                        m_GumpHeight = reader.ReadEncodedInt();

                        m_TitleColor = reader.ReadEncodedInt();
                        m_MessageColor = reader.ReadEncodedInt();

                        m_TitleNumber = reader.ReadEncodedInt();
                        m_MessageNumber = reader.ReadEncodedInt();

                        m_MessageString = reader.ReadString();

                        break;
                    }
            }
        }
    }

    public class MoongateConfirmGump : Gump
    {
        private Mobile m_From;
        private Moongate m_Gate;

        public MoongateConfirmGump(Mobile from, Moongate gate)
            : base(Core.AOS ? 110 : 20, Core.AOS ? 100 : 30)
        {
            m_From = from;
            m_Gate = gate;

            if (Core.AOS)
            {
                Closable = false;

                AddPage(0);

                AddBackground(0, 0, 420, 280, 5054);

                AddImageTiled(10, 10, 400, 20, 2624);
                AddAlphaRegion(10, 10, 400, 20);

                AddHtmlLocalized(10, 10, 400, 20, 1062051, 30720, false, false); // Gate Warning

                AddImageTiled(10, 40, 400, 200, 2624);
                AddAlphaRegion(10, 40, 400, 200);

                if (from.Map != Map.Felucca && gate.TargetMap == Map.Felucca && gate.ShowFeluccaWarning)
                    AddHtmlLocalized(10, 40, 400, 200, 1062050, 32512, false, true); // This Gate goes to Felucca... Continue to enter the gate, Cancel to stay here
                else
                    AddHtmlLocalized(10, 40, 400, 200, 1062049, 32512, false, true); // Dost thou wish to step into the moongate? Continue to enter the gate, Cancel to stay here

                AddImageTiled(10, 250, 400, 20, 2624);
                AddAlphaRegion(10, 250, 400, 20);

                AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

                AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
            }
            else
            {
                AddPage(0);

                AddBackground(0, 0, 420, 400, 5054);
                AddBackground(10, 10, 400, 380, 3000);

                AddHtml(20, 40, 380, 60, @"Dost thou wish to step into the moongate? Continue to enter the gate, Cancel to stay here", false, false);

                AddHtmlLocalized(55, 110, 290, 20, 1011012, false, false); // CANCEL
                AddButton(20, 110, 4005, 4007, 0, GumpButtonType.Reply, 0);

                AddHtmlLocalized(55, 140, 290, 40, 1011011, false, false); // CONTINUE
                AddButton(20, 140, 4005, 4007, 1, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1)
                m_Gate.EndConfirmation(m_From);
        }
    }
}
