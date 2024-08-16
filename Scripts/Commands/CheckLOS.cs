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

/* Scripts/Commands/CheckLOS.cs
 * ChangeLog
 *	4/28/08, Adam
 *		First time checkin
 */


using Server.Misc;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class CheckLOSCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("CheckLOS", AccessLevel.Player, new CommandEventHandler(CheckLOS_OnCommand));
        }

        public static void CheckLOS_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel == AccessLevel.Player && TestCenter.Enabled == false)
            {   // Players can only test this on Test Center
                e.Mobile.SendMessage("Not available here.");
                return;
            }

            if (e.Mobile.AccessLevel > AccessLevel.Player)
            {   // you will not get good results if you test this with AccessLevel > Player
                e.Mobile.SendMessage("You should test this with AccessLevel.Player.");
                return;
            }

            try
            {
                e.Mobile.Target = new LOSTarget();
                e.Mobile.SendMessage("Check LOS to which object?");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

        }

        private class LOSTarget : Target
        {
            public LOSTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                from.SendMessage("You {0} see that.", from.Map.LineOfSight(from, targ) ? "can" : "cannot");
                return;
            }
        }

    }
}
