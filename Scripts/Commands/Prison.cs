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

/* Scripts/Commands/Prison.cs
 * CHANGELOG
 *	3/12/10, Adam
 *		Clone from Jail command.
 */

using Server.Diagnostics;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Commands
{
    public class Prison
    {
        public Prison()
        {
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("Prison", AccessLevel.Counselor, new CommandEventHandler(Prison_OnCommand));
        }

        // warden's office
        public static Point3D Location { get { return new Point3D(354, 836, 20); } }

        public static void Prison_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the player to imprison.");
            e.Mobile.Target = new PrisonTarget(Location, e.ArgString);
        }

        public static void Usage(Mobile to)
        {
            to.SendMessage("Usage: [Prison [\"Tag Message\"]");
        }

        public class PrisonPlayer
        {
            private Point3D m_Location;
            private string m_Comment;
            private PlayerMobile m_Player;
            private PlayerMobile m_Staff;

            public PrisonPlayer(PlayerMobile from, PlayerMobile pm, Point3D location, string comment)
            {
                m_Location = location;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";
                m_Player = pm;
                m_Staff = from;
            }

            public void GoToPrison()
            {
                try
                {
                    if (m_Player == null || m_Player.Deleted)
                    {
                        return;
                    }

                    Account acct = m_Player.Account as Account;

                    // stable the players pets
                    StablePets(m_Staff, m_Player);

                    // drop holding
                    Item held = m_Player.Holding;
                    if (held != null)
                    {
                        held.ClearBounce();
                        if (m_Player.Backpack != null)
                        {
                            m_Player.Backpack.DropItem(held);
                        }
                    }
                    m_Player.Holding = null;

                    // move their items to the bank, overload if needed
                    Backpack bag = new Backpack();
                    ArrayList equip = new ArrayList(m_Player.Items);

                    if (m_Player.Backpack != null)
                    {
                        // count clothing items
                        int WornCount = 0;
                        foreach (Item i in equip)
                        {
                            if (Moongate.RestrictedItem(m_Player, i) == false)
                                continue;   // not clothes
                            else
                                WornCount++;
                        }

                        // Unequip any items being worn
                        foreach (Item i in equip)
                        {
                            if (Moongate.RestrictedItem(m_Player, i) == false)
                                continue;
                            else
                                m_Player.Backpack.DropItem(i);
                        }

                        // Get a count of all items in the player's backpack.
                        ArrayList items = new ArrayList(m_Player.Backpack.Items);

                        // Drop our new bag in the player's bank
                        m_Player.BankBox.DropItem(bag);

                        // Run through all items in player's pack, move them to the bag we just dropped in the bank
                        foreach (Item i in items)
                        {
                            m_Player.Backpack.RemoveItem(i);
                            bag.DropItem(i);
                        }
                    }

                    // handle imprisoning of logged out players
                    m_Player.MoveToWorld(m_Location, Map.Felucca);
                    if (m_Player.NetState == null)
                    {
                        m_Player.LogoutLocation = m_Location;
                        m_Player.Map = Map.Internal;
                    }

                    // make them an inmate
                    m_Player.Inmate = true;

                    // Give them a Deathrobe, Stinger dagger, and a blank spell book
                    if (m_Player.Alive)
                    {
                        Item robe = new Server.Items.DeathRobe();
                        if (!m_Player.EquipItem(robe))
                            robe.Delete();
                    }

                    Item aiStinger = new Server.Items.AIStinger();
                    if (!m_Player.AddToBackpack(aiStinger))
                        aiStinger.Delete();

                    Item spellbook = new Server.Items.Spellbook();
                    if (!m_Player.AddToBackpack(spellbook))
                        spellbook.Delete();

                    m_Player.ShortTermCriminalCounts += 3;                      // how long you will stay
                    m_Player.LongTermCriminalCounts++;                          // how many times you've been to prison

                    if (!m_Player.Alive && m_Player.NetState != null)
                    {
                        m_Player.CloseGump(typeof(Server.Gumps.ResurrectGump));
                        m_Player.SendGump(new Server.Gumps.ResurrectGump(m_Player, Server.Gumps.ResurrectMessage.Healer));
                    }

                    int sentence = (int)m_Player.ShortTermCriminalCounts * 4; // decay time in prison is 4 hours per count
                    m_Player.SendMessage("You have been imprisoned for {0} hours.", sentence);
                    m_Staff.SendMessage("{0} has been imprisoned for {1} hours.", m_Player.Name, sentence);

                    LogHelper Logger = new LogHelper("Prison.log", false, true);

                    Logger.Log(LogType.Mobile, m_Player, string.Format("{0}:{1}:{2}:{3}",
                                                    m_Staff.Name,
                                                    m_Staff.Location,
                                                    m_Comment,
                                                    sentence));
                    Logger.Finish();

                    Commands.CommandLogging.WriteLine(m_Staff, "{0} imprisoned {1}(Username: {2}) for {4} hours with reason: {3}.",
                        m_Staff.Name, m_Player.Name, acct.Username, m_Comment, sentence);
                    acct.Comments.Add(new AccountComment(m_Staff.Name, DateTime.UtcNow + "\nTag count: " + (acct.Comments.Count + 1) + "\nImprisoned for " + sentence + " hours. Reason: " + m_Comment));
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }

            private void StablePets(Mobile from, PlayerMobile master)
            {
                ArrayList pets = new ArrayList();

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseCreature && (m as BaseCreature).IOBFollower == false)
                    {
                        BaseCreature bc = (BaseCreature)m;

                        if (bc.Controlled && bc.ControlMaster == master)
                            pets.Add(bc);
                    }
                }

                if (pets.Count > 0)
                {
                    for (int i = 0; i < pets.Count; ++i)
                    {
                        BaseCreature pet = pets[i] as BaseCreature;

                        if ((pet is PackLlama || pet is PackHorse || pet is Beetle) && (pet.Backpack != null && pet.Backpack.Items.Count > 0))
                        {
                            continue; // You need to unload your pet.
                        }
                        if (master.Stabled.Count >= BaseCreature.GetMaxStabled(master))
                        {
                            continue; // You have too many pets in the stables!
                        }

                        // stable pet and charge
                        AnimalTrainer.StablePet(from, pet, true);
                    }

                    if (from != null)
                        from.SendMessage("{0} pets have been stabled", pets.Count);
                }
            }
        }

        private class PrisonTarget : Target
        {
            private Point3D m_Location;
            private string m_Comment;

            public PrisonTarget(Point3D location, string comment)
                : base(15, false, TargetFlags.None)
            {
                m_Location = location;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                PlayerMobile pm = targ as PlayerMobile;
                if (pm == null)
                {
                    from.SendMessage("Only players can be sent to Prison.");
                    return;
                }

                PrisonPlayer prison = new PrisonPlayer(from as PlayerMobile, pm, m_Location, m_Comment);
                prison.GoToPrison();
            }
        }
    }
}
