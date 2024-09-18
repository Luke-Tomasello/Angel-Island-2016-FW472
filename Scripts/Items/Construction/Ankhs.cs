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

/* Scripts\Items\Construction\Ankhs.cs
 * CHANGELOG
 *	2/9/11, Adam
 *		Reject Law and Mortality choices are now conditioned on Angel Island
 * 06/29/06, Kit
 *		removed tithing gump/context menu junk
 * 11/17/05, Pigpen
 *		Added new commands: "I reject the law of this land" & "me nub follow human rules". These commands will
 *		set the players LongTermMurders (long term murder counts) to 5 giving them murderder status. Set regions around each 
 *		respective shrine for the mortality and law rejection systems to ensure that each command is being used only 
 *		at the correct shrine.
 * 08/29/05 TK
 *		Changed wording in PermadeathConfirmationGump to not sound like continuing will delete character immediately
 * 08/28/05 TK
 *		Added a Permadeath confirmation gump, changed keyword to "I choose a life of mortality"
 *		Disallowed dead players from opting in
 * 08/27/05 Taran Kain
 *		Added command "Life is only as sweet as death is painful" to opt the player into Permadeath mode.
 */

using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Items
{
    public class Ankhs
    {
        public const int ResurrectRange = 2;
        public const int TitheRange = 2;
        public const int LockRange = 2;
        public const int ShrineRange = 3; //Changed from PermadeathRange to be more generic for all shrines.

        public static void GetContextMenuEntries(Mobile from, Item item, ArrayList list)
        {
            if (from is PlayerMobile)
                list.Add(new LockKarmaEntry((PlayerMobile)from));

            list.Add(new ResurrectEntry(from, item));

        }

        public static void Resurrect(Mobile m, Item item)
        {
            if (m.Alive)
                return;

            bool nearChaos = m.GetDistanceToSqrt(new Point3D(1456, 844, 5)) <= 2;
            bool evilAligned = (m is PlayerMobile && (m as PlayerMobile).EthicPlayer != null && (m as PlayerMobile).EthicPlayer.Ethic == Ethics.Ethic.Evil);

            if (m is PlayerMobile && ((PlayerMobile)m).Mortal && m.AccessLevel == AccessLevel.Player)
                m.SendMessage("Thy soul was too closely intertwined with thy flesh - thou'rt unable to incorporate a new body.");
            else if (!m.InRange(item.GetWorldLocation(), ResurrectRange))
                m.SendLocalizedMessage(500446); // That is too far away.
            else if (m.Map != null && m.Map.CanFit(m.Location, 16, CanFitFlags.requireSurface))
            {
                if (evilAligned)
                {   // ethics system
                    if (nearChaos == false)
                        m.SendLocalizedMessage(502590); // "Thy deeds are those of a scoundrel; thou shalt not be resurrected here.";
                    else
                        m.SendGump(new ResurrectGump(m, ResurrectMessage.ChaosShrine));
                }
                else
                    m.SendGump(new ResurrectGump(m, ResurrectMessage.VirtueShrine));
            }
            else
                m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
        }

        public static void Permadeath(PlayerMobile pm, Item item)
        {
            if (pm == null)
                return;

            if (pm.Location.X >= 3352 && pm.Location.Y >= 285 && pm.Location.X <= 3357 && pm.Location.Y <= 292) //Added in a check to make sure issuer of Mortality command is next to the Sacrafice Shrine.
            {
                TimeSpan age = DateTime.UtcNow - pm.Created;
                if (age < TimeSpan.FromDays(7.0))
                {
                    pm.SendMessage("Thou'rt too young to swear thy beliefs on thy soul.");
                    return;
                }
                if (!pm.Alive)
                {
                    pm.SendMessage("Thou art unable to pledge thyself to mortality whilst dead.");
                    return;
                }
                if (pm.Mortal)
                {
                    pm.SendMessage("Thou hast already pledged thy beliefs! Shouldst thine flesh extinguish its light, thy soul will die as well.");
                    return;
                }
                else
                {
                    pm.SendGump(new PermadeathConfirmationGump());
                }
            }
        }

        public static void RejectLaw(PlayerMobile pm, Item item) //Added 11/17/05 - Pigpen, Function to check location for RejectLaw commands, and check for proper number of LongTerms before usage.
        {
            if (pm == null)
                return;

            if (pm.Location.X >= 1456 && pm.Location.Y >= 842 && pm.Location.X <= 1461 && pm.Location.Y <= 846)
            {
                if (pm.Murderer)
                {
                    pm.SendMessage("You are already a murderer.");
                    return;
                }
                else if (pm.Location.X >= 1456 && pm.Location.Y >= 842 && pm.Location.X <= 1461 && pm.Location.Y <= 846)
                {
                    pm.SendGump(new RejectLawGump());
                }
            }
        }

        private class ResurrectEntry : ContextMenuEntry
        {
            private Mobile m_Mobile;
            private Item m_Item;

            public ResurrectEntry(Mobile mobile, Item item)
                : base(6195, ResurrectRange)
            {
                m_Mobile = mobile;
                m_Item = item;

                Enabled = !m_Mobile.Alive;
            }

            public override void OnClick()
            {
                Resurrect(m_Mobile, m_Item);
            }
        }

        private class LockKarmaEntry : ContextMenuEntry
        {
            private PlayerMobile m_Mobile;

            public LockKarmaEntry(PlayerMobile mobile)
                : base(mobile.KarmaLocked ? 6197 : 6196, LockRange)
            {
                m_Mobile = mobile;
            }

            public override void OnClick()
            {
                m_Mobile.KarmaLocked = !m_Mobile.KarmaLocked;

                if (m_Mobile.KarmaLocked)
                    m_Mobile.SendLocalizedMessage(1060192); // Your karma has been locked. Your karma can no longer be raised.
                else
                    m_Mobile.SendLocalizedMessage(1060191); // Your karma has been unlocked. Your karma can be raised again.
            }
        }

        private class PermadeathConfirmationGump : Gump
        {
            public PermadeathConfirmationGump()
                : base(150, 50)
            {
                AddPage(0);

                AddBackground(0, 0, 400, 350, 2600);

                AddHtml(0, 20, 400, 35, "<center>Mortality Confirmation</center>", false, false);

                AddHtml(50, 55, 300, 140, "By pledging your beliefs as such on life and death at this shrine, your body and soul will be permanently joined as one. When your mortal flesh dies, your spirit will be removed from this world forever. Do you wish to continue?<br>CONTINUE - When you die, your character will be deleted.<br>CANCEL - Your character will die normally and become a ghost.", true, true);

                AddButton(200, 227, 4005, 4007, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(235, 230, 110, 35, 1011012, false, false); // CANCEL

                AddButton(65, 227, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(100, 230, 110, 35, 1011011, false, false); // CONTINUE				
            }

            public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
            {
                PlayerMobile pm = sender.Mobile as PlayerMobile;
                if (pm == null)
                    return;

                pm.CloseGump(typeof(PermadeathConfirmationGump));

                if (info.ButtonID == 1) // continue
                {
                    pm.Mortal = true;
                    pm.SendMessage("Thy soul and thy flesh are now eternally bound as one. When thy mortal body dies, so shall thy spirit.");

                    if (Utility.RandomBool())
                        pm.PlaySound(41); // play some thunder
                    else
                        pm.PlaySound(0x215); // play summ critter sound

                    pm.FixedParticles(0x375A, 9, 40, 5027, EffectLayer.Waist); // get some sparkle around them
                }
                else
                {
                    pm.SendMessage("You choose not to become mortal.");
                }
            }
        }

        private class RejectLawGump : Gump //Copy if Permadeath Gump, changed to fit RejectLaw commands.
        {
            public RejectLawGump()
                : base(150, 50)
            {
                AddPage(0);

                AddBackground(0, 0, 400, 350, 2600);

                AddHtml(0, 20, 400, 35, "<center><italic><bold>I reject the law of this land.</bold></italic></center>", false, false);

                AddHtml(50, 55, 300, 140, "By renouncing the laws of this land you take upon yourself the status of a murderer. Lord British's guards will dispatch you on site, as will most of the law abiding citizens of this land. Do you wish to continue?<br>CONTINUE - I renounce the laws of this land.<br>CANCEL - On second thought, maybe this isn't right for me.", true, true);

                AddButton(200, 227, 4005, 4007, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(235, 230, 110, 35, 1011012, false, false); // CANCEL

                AddButton(65, 227, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(100, 230, 110, 35, 1011011, false, false); // CONTINUE				
            }

            public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
            {
                PlayerMobile pm = sender.Mobile as PlayerMobile;
                if (pm == null)
                    return;

                pm.CloseGump(typeof(RejectLawGump));

                if (info.ButtonID == 1) // continue
                {
                    pm.LongTermMurders = 5;
                    pm.SendMessage("You have rejected the laws of this land. Take heed, as you are now known as a Murderer.");

                    if (Utility.RandomBool())
                        pm.PlaySound(41); // play some thunder
                    else
                        pm.PlaySound(0x215); // play summ critter sound

                    pm.FixedParticles(0x375A, 9, 40, 5027, EffectLayer.Waist); // get some sparkle around them
                }
                else
                {
                    pm.SendMessage("You decide against rejecting the laws of this land.");
                }
            }
        }
    }

    public class AnkhWest : Item
    {
        private InternalItem m_Item;

        [Constructable]
        public AnkhWest()
            : this(false)
        {
        }

        [Constructable]
        public AnkhWest(bool bloodied)
            : base(bloodied ? 0x1D98 : 0x3)
        {
            Movable = false;

            m_Item = new InternalItem(bloodied, this);
        }

        public AnkhWest(Serial serial)
            : base(serial)
        {
        }

        public override bool HandlesOnMovement { get { return true; } } // Tell the core that we implement OnMovement

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                Ankhs.Resurrect(m, this);
        }

        public override bool HandlesOnSpeech { get { return true; } } // tell the core that we implement OnSpeech

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Speech.ToLower() == "i choose a life of mortality" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !e.Handled && Utility.InRange(Location, e.Mobile.Location, Ankhs.ShrineRange))
            {
                Ankhs.Permadeath(e.Mobile as PlayerMobile, this);
                return;
            }
            else if (e.Speech.ToLower() == "me nub follow human rules" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !e.Handled && Utility.InRange(Location, e.Mobile.Location, Ankhs.ShrineRange))//New command for rejecting law of this land to set Long Term Counts to 5.
            {
                Ankhs.RejectLaw(e.Mobile as PlayerMobile, this);
                return;
            }
            else if (e.Speech.ToLower() == "i reject the law of this land" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !e.Handled && Utility.InRange(Location, e.Mobile.Location, Ankhs.ShrineRange))//New command for rejecting law of this land to set Long Term Counts to 5.
            {
                Ankhs.RejectLaw(e.Mobile as PlayerMobile, this);
                return;
            }

        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            Ankhs.GetContextMenuEntries(from, this, list);
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set { base.Hue = value; if (m_Item.Hue != value) m_Item.Hue = value; }
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            Ankhs.Resurrect(m, this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
                m_Item.Location = new Point3D(X, Y + 1, Z);
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
                m_Item.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Item != null)
                m_Item.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Item);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Item = reader.ReadItem() as InternalItem;
        }

        private class InternalItem : Item
        {
            private AnkhWest m_Item;

            public InternalItem(bool bloodied, AnkhWest item)
                : base(bloodied ? 0x1D97 : 0x2)
            {
                Movable = false;

                m_Item = item;
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                    m_Item.Location = new Point3D(X, Y - 1, Z);
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                    m_Item.Map = Map;
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Item != null)
                    m_Item.Delete();
            }

            public override bool HandlesOnMovement { get { return true; } } // Tell the core that we implement OnMovement

            public override void OnMovement(Mobile m, Point3D oldLocation)
            {
                if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                    Ankhs.Resurrect(m, this);
            }

            public override void GetContextMenuEntries(Mobile from, ArrayList list)
            {
                base.GetContextMenuEntries(from, list);
                Ankhs.GetContextMenuEntries(from, this, list);
            }

            [Hue, CommandProperty(AccessLevel.GameMaster)]
            public override int Hue
            {
                get { return base.Hue; }
                set { base.Hue = value; if (m_Item.Hue != value) m_Item.Hue = value; }
            }

            public override void OnDoubleClickDead(Mobile m)
            {
                Ankhs.Resurrect(m, this);
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Item = reader.ReadItem() as AnkhWest;
            }
        }
    }

    [TypeAlias("Server.Items.AnkhEast")]
    public class AnkhNorth : Item
    {
        private InternalItem m_Item;

        [Constructable]
        public AnkhNorth()
            : this(false)
        {
        }

        [Constructable]
        public AnkhNorth(bool bloodied)
            : base(bloodied ? 0x1E5D : 0x4)
        {
            Movable = false;

            m_Item = new InternalItem(bloodied, this);
        }

        public AnkhNorth(Serial serial)
            : base(serial)
        {
        }

        public override bool HandlesOnSpeech { get { return true; } } // tell the core that we implement OnSpeech

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Speech.ToLower() == "i choose a life of mortality" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !e.Handled && Utility.InRange(Location, e.Mobile.Location, Ankhs.ShrineRange))
            {
                Ankhs.Permadeath(e.Mobile as PlayerMobile, this);
                return;
            }
            else if (e.Speech.ToLower() == "me nub follow human rules" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !e.Handled && Utility.InRange(Location, e.Mobile.Location, Ankhs.ShrineRange))//New command for rejecting law of this land to set Long Term Counts to 5.
            {
                Ankhs.RejectLaw(e.Mobile as PlayerMobile, this);
                return;
            }
            else if (e.Speech.ToLower() == "i reject the law of this land" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules()) && !e.Handled && Utility.InRange(Location, e.Mobile.Location, Ankhs.ShrineRange))//New command for rejecting law of this land to set Long Term Counts to 5.
            {
                Ankhs.RejectLaw(e.Mobile as PlayerMobile, this);
                return;
            }
        }

        public override bool HandlesOnMovement { get { return true; } } // Tell the core that we implement OnMovement

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                Ankhs.Resurrect(m, this);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            Ankhs.GetContextMenuEntries(from, this, list);
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set { base.Hue = value; if (m_Item.Hue != value) m_Item.Hue = value; }
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            Ankhs.Resurrect(m, this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
                m_Item.Location = new Point3D(X + 1, Y, Z);
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
                m_Item.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Item != null)
                m_Item.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Item);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Item = reader.ReadItem() as InternalItem;
        }

        [TypeAlias("Server.Items.AnkhEast+InternalItem")]
        private class InternalItem : Item
        {
            private AnkhNorth m_Item;

            public InternalItem(bool bloodied, AnkhNorth item)
                : base(bloodied ? 0x1E5C : 0x5)
            {
                Movable = false;

                m_Item = item;
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                    m_Item.Location = new Point3D(X - 1, Y, Z);
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                    m_Item.Map = Map;
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Item != null)
                    m_Item.Delete();
            }

            public override bool HandlesOnMovement { get { return true; } } // Tell the core that we implement OnMovement

            public override void OnMovement(Mobile m, Point3D oldLocation)
            {
                if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                    Ankhs.Resurrect(m, this);
            }

            public override void GetContextMenuEntries(Mobile from, ArrayList list)
            {
                base.GetContextMenuEntries(from, list);
                Ankhs.GetContextMenuEntries(from, this, list);
            }

            [Hue, CommandProperty(AccessLevel.GameMaster)]
            public override int Hue
            {
                get { return base.Hue; }
                set { base.Hue = value; if (m_Item.Hue != value) m_Item.Hue = value; }
            }

            public override void OnDoubleClickDead(Mobile m)
            {
                Ankhs.Resurrect(m, this);
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Item = reader.ReadItem() as AnkhNorth;
            }
        }
    }
}
