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

/* Scripts/Commands/Time.cs
 * ChangeLog
 *	3/25/08 - Pix
 *		Changed to use new AdjustedDateTime utility class.
 *	12/06/05 - Pigpen
 *		Created.
 *		Time command works as follows. '[time' Displays Date then time.
 *	3/10/07 - Cyrun
 *		Edited message displayed to include "PST".
 */


using Server.Mobiles;
using System;

namespace Server.Commands
{
    public class TimeCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Time", AccessLevel.Player, new CommandEventHandler(Time_OnCommand));
        }

        public static void Time_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m is PlayerMobile)
            {
                //m.SendMessage("Server time is: {0} PST.", DateTime.UtcNow);

                //AdjustedDateTime ddt = new AdjustedDateTime(DateTime.UtcNow);
                DateTime ddt = AdjustedDateTime.ServerTime;
                m.SendMessage("Server time is: {0} {1}.", ddt.ToShortTimeString(), AdjustedDateTime.ServerTimezone);
            }
        }

    }
}
