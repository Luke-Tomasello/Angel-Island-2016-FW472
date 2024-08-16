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

/* Scripts/Engines/Spawner/TotalRespawnCommand.cs
 * ChangeLog
 *	12/30/04 - Pixie
 *		Changed to Admin only; Added broadcast message; Only spawn Running spawners
 *	12/29/04 - Pixie
 *		Initial Version!
 */


using System;
using System.Collections;

namespace Server.Commands
{
    public class TotalRespondCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("TotalRespawn", AccessLevel.Administrator, new CommandEventHandler(TotalRespawn_OnCommand));
        }

        public static void TotalRespawn_OnCommand(CommandEventArgs e)
        {
            DateTime begin = DateTime.Now;

            World.Broadcast(0x35, true, "The world is respawning, please wait.");

            ArrayList spawners = new ArrayList();
            foreach (Item item in World.Items.Values)
            {
                if (item is Server.Mobiles.Spawner)
                {
                    spawners.Add(item);
                }
            }

            foreach (Server.Mobiles.Spawner sp in spawners)
            {
                if (sp.Running)
                {
                    sp.Respawn();
                }
            }

            DateTime end = DateTime.Now;

            TimeSpan timeTaken = end - begin;
            World.Broadcast(0x35, true, "World spawn complete. The entire process took {0:00.00} seconds.", timeTaken.TotalSeconds);
            e.Mobile.SendMessage("Total Respawn of {0} spawners took {1:00.00} seconds", spawners.Count, timeTaken.TotalSeconds);
        }
    }
}
