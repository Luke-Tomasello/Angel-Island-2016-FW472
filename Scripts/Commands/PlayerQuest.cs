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

/* Scripts/Commands/PlayerQuest.cs
 * ChangeLog:
 *  8/11/07, Adam
 *      Protect against targeting a backpack
 *      Add assert to alert staff if the player is missing a backpack.
 *	8/11/07, Pixie
 *		Safeguarded PlayerQuestTarget.OnTarget.
 *  9/08/06, Adam
 *		Created.
 */

using Server.Items;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class PlayerQuest
    {
        public static void Initialize()
        {
            Register();
        }

        public static void Register()
        {
            Server.CommandSystem.Register("Quest", AccessLevel.Player, new CommandEventHandler(PlayerQuest_OnCommand));
        }

        private class PlayerQuestTarget : Target
        {
            public PlayerQuestTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                try
                {
                    if (from == null)
                    {
                        return;
                    }

                    if (o == null)
                    {
                        from.SendMessage("Target does not exist.");
                        return;
                    }


                    if (o is BaseContainer == false)
                    {
                        from.SendMessage("That is not a container.");
                        return;
                    }

                    BaseContainer bc = o as BaseContainer;

                    if (Misc.Diagnostics.Assert(from.Backpack != null, "from.Backpack == null") == false)
                    {
                        from.SendMessage("You cannot use this deed without a backpack.");
                        return;
                    }

                    // mobile backpacks may not be used
                    if (bc == from.Backpack || bc.Parent is Mobile)
                    {
                        from.SendMessage("You may not use that container.");
                        return;
                    }

                    // must not be locked down
                    if (bc.IsLockedDown == true || bc.IsSecure == true)
                    {
                        from.SendMessage("That container is locked down.");
                        return;
                    }

                    // if it's in your bankbox, or it's in YOUR house, you can deed it
                    if ((bc.IsChildOf(from.BankBox) || CheckAccess(from)) == false)
                    {
                        from.SendMessage("The container must be in your bankbox, or a home you own.");
                        return;
                    }

                    // cannot be in another container
                    if (bc.RootParent is BaseContainer && bc.IsChildOf(from.BankBox) == false)
                    {
                        from.SendMessage("You must remove it from that container first.");
                        return;
                    }

                    // okay, done with target checking, now deed the container.
                    // place a special deed to reclaim the container in the players backpack
                    PlayerQuestDeed deed = new PlayerQuestDeed(bc);
                    if (from.Backpack.CheckHold(from, deed, true, false, 0, 0))
                    {
                        bc.PlayerQuest = true;                          // mark as special
                        bc.MoveToWorld(from.Location, Map.Internal);    // put it on the internal map
                        bc.SetLastMoved();                              // record the move (will use this in Heartbeat cleanup)
                                                                        //while (deed.Expires.Hours + deed.Expires.Minutes == 0)
                                                                        //Console.WriteLine("Waiting...");
                                                                        //int hours = deed.Expires.Hours;
                                                                        //int minutes = deed.Expires.Minutes;
                                                                        //string text = String.Format("{0} {1}, and {2} {3}",	hours, hours == 1 ? "hour" : "hours", minutes, minutes == 1 ? "minute" : "minutes");
                        from.Backpack.DropItem(deed);
                        from.SendMessage("A deed for the container has been placed in your backpack.");
                        //from.SendMessage( "This quest will expire in {0}.", text);
                    }
                    else
                    {
                        from.SendMessage("Your backpack is full and connot hold the deed.");
                        deed.Delete();
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                }
            }

            public bool CheckAccess(Mobile m)
            {   // Allow access if the player is owner of the house.
                BaseHouse house = BaseHouse.FindHouseAt(m);
                return (house != null && house.IsOwner(m));
            }
        }

        [Usage("PlayerQuest")]
        [Description("Allows a player to convert a container of items into a quest ticket.")]
        private static void PlayerQuest_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from.Alive == false)
            {
                e.Mobile.SendMessage("You are dead and cannot do that.");
                return;
            }

            from.SendMessage("Please target the container you would like to deed.");
            from.Target = new PlayerQuestTarget();
        }
    }
}
