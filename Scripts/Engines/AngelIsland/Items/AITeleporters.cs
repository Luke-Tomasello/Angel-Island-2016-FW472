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

/* Scripts/Engines/AngelIsland/AITeleporters.cs
 * ChangeLog
 *	3/17/10, adam
 *		Add a KeepStartup parameter to the EmptyPackOnExit() function.
 *			if KeepStartup is true, you will keep a blessed stinger and your spellbook.
 *			this is never sent when exiting the prison, but instead set OnLogon() in PlayerMobile to reset your backpack
 *			see comments in PlayerMobile:OnLogon
 *	3/12/10, adam
 *		Add suppord for ShortCriminalCounts. Like murder counts, but not reduced if you escape
 *		ShortCriminalCounts decay at the same rate as short term murder counts in prison only
 *	3/10/10, adam
 *		Lots of cleanup.
 *		Add common function for cleaning the players backpack on exit
 *		add 1 in 5000 chance to keep your light housepass
 *		delete regs, weapons, etc. on exit
 *	07/27/08, weaver
 *		Correctly remove gumps from the NetState object on CloseGump() (integrated RunUO fix).
 *	3/8/06, Pix
 *		Now shuts down all AITeleportQueryGump on teleport.
 *	2/10/06, Adam
 *		Reuse the Moongate.RestrictedItem() check as it is the same item list
 *	10/11/05, Pix
 *		Added DropHolding code to AIEscapeExit (to make sure nobody sneaks out a blessed stinger)
 *	4/29/05, Adam
 *		In AIParoleExit()
 *		1. Allow prisoners to walk out if Server Wars are on
 *		2. Flip logic for not deleted you lighthouse passes unless it's going to let you leave.
 *	04/05/05, erlein
 *		Added the same check for actual dropping to force the items
 *		to land in the bank on teleport.
 *	04/05/05, erlein
 *		Added check for Mobile.Alive property in MoveItemsToBank.
 *	2/26/05, mith
 *		AIEscapeExit, modified to reduce escapee's counts by half without going below 0.
 * 1/27/05, Darva
 *		This time -really- check. :P
 * 1/25/05, Darva
 *		check to make sure bank can hold the items put in it, refuse transportation to the island if not.
 * 12/29/04, Pix
 *		Made sure we always move the player to Felucca (Changed from directly setting the location
 *		to calling MoveToWorld instead)
 * 10/12/04, Adam
 *		Add a comment to explain the count reduction logic.
 *		From the code it is not obvious that the intent is to prohibit the STC from dropping below 5
 *		This is as per design.
 * 10/06/04, Pix
 *		Fixed short term murder count reduction in AIEscapeExit.
 *	9/28/04, Pix
 *		Made anything held (on cursor) bounce back before we check the pack for LHPasses/etc.
 *	9/26/04, Pix
 *		AICaveEntrance and AIEscapeExit shouldn't be persistent when the world loads.
 *		If they exist when the world loads, we now delete them.
 *	9/24/04, Pix
 *		Changed so Stingers don't get unblessed via the AICaveEntrance, instead they get unblessed
 *		using the AIEscapeExit.
 *		Now armed stingers get unblessed too.
 *	9/2/04, Pix
 *		Changed lighthousepass deleting mechanism - now it uses (container).FindItemsByType to recursively
 *		search players' backpacks.
 *		Changed AIStingers to LootType.Regular on teleport with these teleporters.
 *	8/12/04, mith
 *		AIParoleExit.OnMoveOver(): Copied code from AICaveEntrance to delete Lighthouse Passes from players packs after they get paroled.
 *	6/10/04, mith
 *		Modified AICaveEntrance.OnMoveOver() to delete all passes from a players pack, not just the first one it finds.
 *	5/10/04, mith
 *		Modified CaveEntrance.OnMoveOver() to only check for LHPasses if player is AccessLevel.Player. 
 *		GMs and Admins may use the teleporter without having a LHPass.
 *	4/30/04, mith
 *		Fixed a bug where if player is teleported to AI while dead, ResetKillTime() call fails and they don't get the benefit of shorter count timers.
 *	4/27/04, mith
 *		Added ResurrectGump calls to AIEntrance.OnMoveOver. This should pop-up the gump after the player has teleported to AI.
 *		Modified the way we handle dead/alive players and their robe/shroud. Shroud is not deleted on a living player.
 *	4/20/04, mith
 *		Small change to the giving out of deathrobes on entrance due to a bug I found.
 *	4/19/04, mith
 *		Modified Serialize/Deserialize to work with older versions of teleporters.
 *		Teleporters will be updated to "version 3" on next server restart without having to be replaced.
 *	4/15/04, mith
 *		Streamlined the code, removed variables that weren't used and tried to make this generally more readable.
 *	4/13/04 changes by smerX
 *		Must posess AILHPass to use AICaveEntrance
 *	4/9/04 changes by mith
 *		Added code to ResetKillTime on entrance and exit, so that counts decay faster on AI.
 *	4/8/04 changes by smerX
 *		Added AICaveEntrance class.
 *	4/7/04, changes by mith
 *		AIEntrance.DoTeleport(), replaced call to TeleportPets with call to StablePets.
 *	4/1/04
 *		Modified formula for successful escape to give diminishing returns on count decrease.
 *	3/31/04
 *		Added code to take all of a player's equipment and put it in a bag in their bankbox if they use the entrance teleporter.
 *		Moved code to create aiStinger as well as a robe, into the OnMoveOver event (since we want to be able to return true/false if moving items fails)
 *		Added code to create empty spellbook on entrance.
 *	3/30/04
 *		Added ParoleExit and EscapeExit teleporters, renamed file
 *		Tweaked "access denied" error messages to be more descriptive (can't use 
 *		this when dead, can't use this with less than x number of counts, etc)
 *		Added message to escape teleporter to let escapee know new number of ST counts.
 *		Removed code to verify that the person using the entrance teleporter is a ghost. 
 *		Living/Dead stipulations for exit teleporters still apply.
 *	3/29/04 changes by mith
 *		Changed default destination to put user in Warden's office.
 *		Added code to generate dagger on use of teleporter rather than upon resurrection.
 *		Added code to set PlayerFlag.Inmate = true when player teleports into AI.
 *	3/27/04 changes by mith
 *		TODO: Something needs to be done with pets, either during teleport, or right after. Thinking of putting a
 *		special stablemaster next to the healer, that will res/stable pets, but not allow claiming.
 *		Moved the status checking out of StartTeleport and into OnMoveOver, which returns true if teleporter doesn't work.
 *	3/26/04 Created by mith
 *		Modification of the current Teleporter.cs file that allows us to check the user's kills and whether they are dead or not.
 *		Automagically set the location point and map to put the user inside the cellblock on AI.
 */
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Items
{
    public class AIEntrance : Item
    {
        private bool m_Active, m_SourceEffect, m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }

        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AIEntrance()
            : base(0x1BC3)
        {
            m_Active = true;

            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Entrance");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            //shut down all AITeleportQueryGumps
            m.CloseGumps(typeof(AITeleportQueryGump));

            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            Server.Mobiles.BaseCreature.StablePets(m);

            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            // This puts them in the Warden's office on Angel Island.
            Server.Point3D location = new Point3D(355, 836, 20);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            if (m.Player && m.AccessLevel == AccessLevel.Player)
            {
                ((PlayerMobile)m).Inmate = true;
            }
        }

        private bool MoveItemsToBank(Mobile m)
        {
            Backpack bag = new Backpack();
            ArrayList equip = new ArrayList(m.Items);

            if (m.Backpack != null)
            {
                // count clothing items
                int WornCount = 0;
                foreach (Item i in equip)
                {
                    if (Moongate.RestrictedItem(m, i) == false)
                        continue;   // not clothes
                    else
                        WornCount++;
                }

                // erl: added check for Mobile.Alive property... will not return false if mobile is not alive
                if (125 - m.BankBox.TotalItems - 1 - m.Backpack.TotalItems - WornCount < 0 && m.Alive)
                {
                    return false;
                }

                // Unequip any items being worn
                foreach (Item i in equip)
                {
                    if (Moongate.RestrictedItem(m, i) == false)
                        continue;
                    else
                        m.Backpack.DropItem(i);
                }

                // Get a count of all items in the player's backpack.
                ArrayList items = new ArrayList(m.Backpack.Items);

                // Drop our new bag in the player's bank
                m.BankBox.DropItem(bag);

                // Run through all items in player's pack, move them to the bag we just dropped in the bank
                foreach (Item i in items)
                {
                    // If there's room, drop the item in the bank, otherwise drop it on the ground
                    if (bag.TryDropItem(m, i, false) || !m.Alive)
                        bag.DropItem(i);
                    else
                        i.DropToWorld(m, m.Location);
                }
            }

            // Give them a Deathrobe, Stinger dagger, and a blank spell book
            if (m.Alive)
            {
                Item robe = new Server.Items.DeathRobe();
                if (!m.EquipItem(robe))
                    robe.Delete();
            }

            Item aiStinger = new Server.Items.AIStinger();
            if (!m.AddToBackpack(aiStinger))
                aiStinger.Delete();

            Item spellbook = new Server.Items.Spellbook();
            if (!m.AddToBackpack(spellbook))
                spellbook.Delete();
            return true;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                if (!m.Player)
                    return true;

                if (m.AccessLevel == AccessLevel.Player)
                {
                    if (m.ShortTermMurders < 5)
                    {
                        // We want to make sure we're teleporting a PK and not some random blubie.
                        m.SendMessage("You must have at least five short term murder counts to use this.");
                        return true;
                    }

                    Item held = m.Holding;
                    if (held != null)
                    {
                        held.ClearBounce();
                        if (m.Backpack != null)
                        {
                            m.Backpack.DropItem(held);
                        }
                    }
                    m.Holding = null;

                    // attempt to move items to bank.
                    bool result;
                    result = MoveItemsToBank(m);
                    if (result == false)
                    {
                        m.SendMessage("You carry to many possessions to enter Angel Island at this time.");
                        return true;
                    }
                    else
                    {
                        m.SendMessage("Your worldly possessions have been placed in your bank for safekeeping.");
                    }

                }

                StartTeleport(m);

                if (!m.Alive && m.NetState != null)
                {
                    m.CloseGump(typeof(ResurrectGump));
                    m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
                }

                return false;
            }

            return true;
        }

        public AIEntrance(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }

    public class AIParoleExit : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AIParoleExit()
            : base(0x1BC3)
        {
            m_Active = true;
            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Parole Exit");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            Server.Point3D location = new Point3D(818, 1087, 0);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            if (m.Player && m.AccessLevel == AccessLevel.Player)
            {
                AITeleportHelper.EmptyPackOnExit(m, true);
                ((PlayerMobile)m).Inmate = false;
                ((PlayerMobile)m).ResetKillTime();
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            // prisoners get out free during server wars.
            bool bServerWars = (Server.Misc.AutoSave.SavesEnabled == false && Server.Misc.AutoRestart.Restarting == true);

            if (m_Active)
            {
                if (!m.Player)
                    return true;

                if (m.AccessLevel == AccessLevel.Player)
                {
                    if (bServerWars == true)
                    {
                        m.SendMessage("During Server Wars all prisoners go free!");
                        return true;
                    }

                    // Make sure they've worked off their counts
                    if (m is PlayerMobile)
                        if ((m as PlayerMobile).ShortTermCriminalCounts > 0)
                        {
                            // We don't want people leaving on parole if they've not worked off their counts
                            m.SendMessage("You still have {0} criminal counts against you. You must have 0 to be paroled.", (m as PlayerMobile).ShortTermCriminalCounts);
                            return true;
                        }

                    // Make sure they've worked off their counts
                    if (m.ShortTermMurders > 4)
                    {
                        // We don't want people leaving on parole if they've not worked out of stat-loss
                        m.SendMessage("You must have less than 5 short term murder counts to be paroled.");
                        return true;
                    }

                    if (!m.Alive)
                    {
                        // and we don't want them leaving as a ghost either.
                        m.SendMessage("You must be alive to use this.");
                        return true;
                    }
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public AIParoleExit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }

    public class AIEscapeExit : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AIEscapeExit()
            : base(0x1BC3)
        {
            m_Active = true;

            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Escape Exit");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            Server.Point3D location = new Point3D(5671, 2391, 50);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            if (m.Player && m.AccessLevel == AccessLevel.Player)
            {
                AITeleportHelper.EmptyPackOnExit(m, false);

                ((PlayerMobile)m).Inmate = false;

                // Adam: The following code prohibits the STC from dropping below 5
                //	as per design.
                int oldSTC = ((PlayerMobile)m).ShortTermMurders;
                int newSTC = ((PlayerMobile)m).ShortTermMurders;

                if (((PlayerMobile)m).ShortTermMurders >= 10)
                    newSTC = ((PlayerMobile)m).ShortTermMurders / 2;
                else if (((PlayerMobile)m).ShortTermMurders > 5)
                    newSTC = 5;

                ((PlayerMobile)m).ShortTermMurders = newSTC;

                if (oldSTC - newSTC > 0)
                    ((PlayerMobile)m).SendMessage("Your short term murders have been reduced to {0}", ((PlayerMobile)m).ShortTermMurders);

                ((PlayerMobile)m).ResetKillTime();
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                if (!m.Player)
                    return true;

                if (!m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    // We want to make sure we don't have ghosties camping the escape exit.
                    m.SendMessage("You must be alive to use this.");
                    return true;
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public AIEscapeExit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }

            //Pix: the AIEscapeExit shouldn't be saved... so we should delete it if
            // it's there on world load.
            System.Console.WriteLine("Deleting AIEscapeExit on world load!");
            this.Delete();
        }
    }

    public class AICaveEntrance : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AICaveEntrance()
            : base(0x1BC3)
        {
            m_Active = true;
            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Spirit Spawn");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            Server.Point3D location = new Point3D(5750, 350, 5);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            // not sure if this is needed
            Item held = m.Holding;
            if (held != null)
            {
                held.ClearBounce();
                if (m.Backpack != null)
                {
                    m.Backpack.DropItem(held);
                }
            }
            m.Holding = null;

            // okay, delete the lighthouse passes
            Container backpack = m.Backpack;
            if (backpack != null)
            {
                Item[] lhpasses = backpack.FindItemsByType(typeof(AILHPass), true);
                if (lhpasses != null && lhpasses.Length > 0)
                {
                    for (int i = 0; i < lhpasses.Length; i++)
                    {   // 1 in 5000 chance to keep this puppy
                        if (lhpasses[i] is AILHPass && Utility.Random(5000) != 1)
                            lhpasses[i].Delete();
                    }
                }
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            bool HasPass = false;

            if (m_Active)
            {
                if (!m.Player)
                    return true;

                if (m.AccessLevel == AccessLevel.Player)
                {
                    if (!m.Alive)
                    {
                        // no ghosts are allowed into the escape route
                        m.SendMessage("You must be alive to use this.");
                        return true;
                    }

                    Container backpack = m.Backpack;
                    if (backpack != null)
                    {
                        Item[] lhpasses = backpack.FindItemsByType(typeof(AILHPass), true);
                        if (lhpasses != null && lhpasses.Length > 0)
                            HasPass = true;
                    }

                    if (!HasPass)
                    {
                        m.SendMessage("You require a lighthouse pass to go there");
                        return true;
                    }
                    else
                        // we delete it in DoTeleport
                        m.SendMessage("Your lighthouse pass disappears as you're teleported");
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public AICaveEntrance(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }

            //Pix: the AICaveEntrance shouldn't be saved... so we should delete it if
            // it's there on world load.
            System.Console.WriteLine("Deleting AICaveEntrance on world load!");
            this.Delete();
        }
    }

    public class AITeleportHelper
    {
        public static void EmptyPackOnExit(Mobile m, bool DeletePasses)
        {
            EmptyPackOnExit(m, DeletePasses, false);
        }

        public static void EmptyPackOnExit(Mobile m, bool DeletePasses, bool KeepStartup)
        {

            Container backpack = m.Backpack;
            if (backpack == null)
                return;

            // put whatever you are holding in you backpack
            // (the 'drag' kind of holding)
            Item held = m.Holding;
            if (held != null)
            {
                held.ClearBounce();
                if (m.Backpack != null)
                {
                    m.Backpack.DropItem(held);
                }
            }
            m.Holding = null;

            // put whatever you are holding in your backpack
            // (actually in your hand kind of holding)
            Item weapon = m.Weapon as Item;
            if (weapon != null && weapon.Parent == m && !(weapon is Fists))
                backpack.DropItem(weapon);

            ArrayList stuff = backpack.FindAllItems();
            if (stuff != null && stuff.Count > 0)
            {
                for (int ix = 0; ix < stuff.Count; ix++)
                {
                    Item item = stuff[ix] as Item;
                    // process items as follows
                    //	delete weapons (except stinger) and reagents
                    //	change stinger to regular loot
                    //	delete reagents
                    //	Oprional delete lighthouse passes - (they were handled on entrance to the escape cave)
                    //	delete spellbook
                    // we don't delete EVERYTHING because we may allow some rares to be found here in the future

                    if (item is BaseWeapon && item is AIStinger == false)
                        item.Delete();

                    if (item is AIStinger && KeepStartup == false)
                        item.LootType = LootType.Regular;

                    if (item is BaseReagent)
                        item.Delete();

                    if (item is AILHPass && DeletePasses)
                        item.Delete();

                    if (item is Spellbook && KeepStartup == false)
                        item.Delete();
                }
            }

            return;
        }
    }
}
