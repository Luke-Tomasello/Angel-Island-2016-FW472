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

/* Scripts/Commands/Jail.cs
 * CHANGELOG
 *	3/25/09, Adam
 *		Add command version of Jail command to be called by PJUM ReportAsMacroer (JailPlayer)
 *		When auto stabeling, 
 *			ignore pets over stable limit (you're going to jail .. too bad)
 *			ignore pets that have something in their pack (you're going to jail .. too bad)
 *  1/12/08, Adam
 *      Stable pets if the jailed player has any
 *	6/14/06, Adam
 *		If the player is offline when they are jailed, set their map to internal
 *	5/30/06, Pix
 *		Changed LogoutLocation to jail when player is not online.
 *  04/11/06, Kit
 *		Added [JailTroublemaker command addition.
 *  11/06/05 Taran Kain
 *		Changed jail sentence to 12 + 12*prev infractions (jail or macroer)
 *  11/06/05 Taran Kain
 *		Changed jail sentence from 24 hours to 24hr + 24 * prev [jail sentences + 4 * [macroer convictions
 *	10/18/05, erlein
 *		Added logging of jailed players.
 *	09/12/05, Adam
 *		Make access Counselor
 *		Counselors can already 'pull' players to jail, and this is more of a 
 *		problem as they must then let them out, or tell the rest of staff to
 *		do so. Using [jail automates the exit process.
 *	09/01/05 Taran Kain
 *		First version. Jails a player and tags their account.
 */

using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Commands
{
    public class Jail
    {
        public Jail()
        {
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("Jail", AccessLevel.Counselor, new CommandEventHandler(Jail_OnCommand));
        }

        public static Point3D GetCell(int cell)
        {
            switch (cell)
            {
                case 1: return new Point3D(5276, 1164, 0);
                case 2: return new Point3D(5286, 1164, 0);
                case 3: return new Point3D(5296, 1164, 0);
                case 4: return new Point3D(5306, 1164, 0);
                case 5: return new Point3D(5276, 1174, 0);
                case 6: return new Point3D(5286, 1174, 0);
                case 7: return new Point3D(5296, 1174, 0);
                case 8: return new Point3D(5306, 1174, 0);
                case 9: return new Point3D(5283, 1184, 0);
                case 10: return new Point3D(5304, 1184, 0);
            }

            return Point3D.Zero;
        }

        public static void Jail_OnCommand(CommandEventArgs e)
        {

            switch (e.Arguments.Length)
            {
                case 0:
                    {
                        e.Mobile.SendMessage("Target the player to jail.");
                        e.Mobile.Target = new JailTarget(3, "", false);
                        break;
                    }
                case 1:
                    {
                        if (e.Arguments[0] == "-troublemaker")
                        {
                            e.Mobile.SendMessage("Target the player to jail.");
                            e.Mobile.Target = new JailTarget(3, "were disrupting a staff event", true);
                            break;
                        }
                        else
                        {
                            if (e.Arguments[0] == "-h" || e.Arguments[0] == "-cell")
                            {
                                Usage(e.Mobile);
                                return;
                            }

                            e.Mobile.SendMessage("Target the player to jail.");
                            e.Mobile.Target = new JailTarget(3, e.Arguments[0], false);
                            break;
                        }


                    }

                case 2:
                    {
                        if (e.Arguments[0] != "-cell")
                        {
                            Usage(e.Mobile);
                            return;
                        }

                        int cell;
                        try
                        {
                            cell = Int32.Parse(e.Arguments[1]);
                            if (cell < 1 || cell > 10)
                            {
                                e.Mobile.SendMessage("Cells range from 1 to 10.");
                                return;
                            }
                        }
                        catch
                        {
                            Usage(e.Mobile);
                            return;
                        }

                        e.Mobile.SendMessage("Target the player to jail.");
                        e.Mobile.Target = new JailTarget(cell, "", false);
                        break;
                    }
                case 3:
                    {
                        if (e.Arguments[0] != "-cell")
                        {
                            Usage(e.Mobile);
                            return;
                        }

                        int cell;
                        try
                        {
                            cell = Int32.Parse(e.Arguments[1]);
                            if (cell < 1 || cell > 10)
                            {
                                e.Mobile.SendMessage("Cells range from 1 to 10.");
                                return;
                            }
                        }
                        catch
                        {
                            Usage(e.Mobile);
                            return;
                        }

                        e.Mobile.SendMessage("Target the player to jail.");
                        e.Mobile.Target = new JailTarget(cell, e.Arguments[2], false);
                        break;
                    }

                default:
                    {
                        Usage(e.Mobile);
                        return;
                    }
            }
        }

        public static void Usage(Mobile to)
        {
            to.SendMessage("Usage: [jail [-troublemaker] [-cell <num>] [\"Tag Message\"]");
        }

        public class JailPlayer
        {
            private int m_Cell;
            private string m_Comment;
            private bool Trouble;
            private PlayerMobile m_Player;

            public JailPlayer(PlayerMobile pm, int cell, string comment, bool troublemaker)
            {
                m_Cell = cell;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";
                Trouble = troublemaker;
                m_Player = pm;
            }

            public void GoToJail()
            {
                if (m_Player == null || m_Player.Deleted)
                {
                    return;
                }

                Account acct = m_Player.Account as Account;

                int sentence = 12; // 12 hour minimum sentence

                if (Trouble == true)
                {
                    sentence = 2;// two hour sentance for troublemakets
                }
                else
                {
                    foreach (AccountComment comm in acct.Comments)
                    {
                        if (comm.Content.IndexOf("Jailed for ") != -1 && comm.Content.IndexOf("Tag count: ") != -1)
                            sentence += 12; // 12 hours for each previous [jail'ing
                        else if (comm.Content.IndexOf(" : reported using the [macroer command") != -1)
                            sentence += 12; // 12 hours for every time they were caught resource botting
                    }
                }

                // stable the players pets
                StablePets(m_Player);

                // move to jail
                Point3D destPoint = Jail.GetCell(m_Cell);
                m_Player.MoveToWorld(destPoint, Map.Felucca);

                // handle jailing of logged out players
                if (m_Player.NetState == null)
                {
                    m_Player.LogoutLocation = destPoint;
                    m_Player.Map = Map.Internal;
                }

                JailExitGate.AddInmate(m_Player, sentence);
                m_Player.SendMessage("You have been jailed for {0} hours.", sentence);

                LogHelper Logger = new LogHelper("jail.log", false, true);
                Logger.Log(LogType.Mobile, m_Player, string.Format("{0}:{1}:{2}:{3}", "SYSTEM", m_Cell, m_Comment, sentence));
                Logger.Finish();

                if (Trouble == true)
                    acct.Comments.Add(new AccountComment("SYSTEM", DateTime.Now + "\nTag count: " + (acct.Comments.Count + 1) + "\nJailedTroubleMaker for " + sentence + " hours. Reason: " + m_Comment));
                else
                    acct.Comments.Add(new AccountComment("SYSTEM", DateTime.Now + "\nTag count: " + (acct.Comments.Count + 1) + "\nJailed for " + sentence + " hours. Reason: " + m_Comment));
            }

            private void StablePets(PlayerMobile master)
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
                        AnimalTrainer.StablePet(master, pet, true);
                    }
                }
            }
        }

        private class JailTarget : Target
        {
            private int m_Cell;
            private string m_Comment;
            private bool Trouble;

            public JailTarget(int cell, string comment, bool troublemaker)
                : base(15, false, TargetFlags.None)
            {
                m_Cell = cell;
                m_Comment = comment;
                if (m_Comment == null || m_Comment == "")
                    m_Comment = "None";
                Trouble = troublemaker;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                PlayerMobile pm = targ as PlayerMobile;
                if (pm == null)
                {
                    from.SendMessage("Only players can be sent to jail.");
                    return;
                }

                Account acct = pm.Account as Account;

                int sentence = 12; // 12 hour minimum sentence

                if (Trouble == true)
                {
                    sentence = 2;// two hour sentance for troublemakets
                }
                else
                {
                    foreach (AccountComment comm in acct.Comments)
                    {
                        if (comm.Content.IndexOf("Jailed for ") != -1 && comm.Content.IndexOf("Tag count: ") != -1)
                            sentence += 12; // 12 hours for each previous [jail'ing
                        else if (comm.Content.IndexOf(" : reported using the [macroer command") != -1)
                            sentence += 12; // 12 hours for every time they were caught resource botting
                    }
                }

                // stable the players pets
                StablePets(from, pm);

                // handle jailing of logged out players
                Point3D destPoint = Jail.GetCell(m_Cell);
                pm.MoveToWorld(destPoint, Map.Felucca);
                if (pm.NetState == null)
                {
                    pm.LogoutLocation = destPoint;
                    pm.Map = Map.Internal;
                }

                JailExitGate.AddInmate(pm, sentence);
                pm.SendMessage("You have been jailed for {0} hours.", sentence);
                from.SendMessage("{0} has been jailed for {1} hours.", pm.Name, sentence);

                LogHelper Logger = new LogHelper("jail.log", false, true);

                Logger.Log(LogType.Mobile, pm, string.Format("{0}:{1}:{2}:{3}",
                                                from.Name,
                                                m_Cell,
                                                m_Comment,
                                                sentence));
                Logger.Finish();

                Commands.CommandLogging.WriteLine(from, "{0} jailed {1}(Username: {2}) into cell {3} for {5} hours with reason: {4}.",
                    from.Name, pm.Name, acct.Username, m_Cell, m_Comment, sentence);
                if (Trouble == true)
                    acct.Comments.Add(new AccountComment(from.Name, DateTime.Now + "\nTag count: " + (acct.Comments.Count + 1) + "\nJailedTroubleMaker for " + sentence + " hours. Reason: " + m_Comment));
                else
                    acct.Comments.Add(new AccountComment(from.Name, DateTime.Now + "\nTag count: " + (acct.Comments.Count + 1) + "\nJailed for " + sentence + " hours. Reason: " + m_Comment));
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
                        AnimalTrainer.StablePet(master, pet, true);
                    }

                    from.SendMessage("{0} pets have been stabled", pets.Count);
                }
            }
        }
    }
}
