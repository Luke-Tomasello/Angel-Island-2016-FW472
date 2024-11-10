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

/* Changelog :
 *	03/25/05, erlein
 *		Integrated with LogHelper class.		
 *	03/23/05, erlein
 *		Moved to /Scripts/Commands/FindItemByName.cs (for Find* command normalization).
 *		Changed namespace to Server.Commands.
 */

using Server.Diagnostics;

namespace Server.Commands
{
    public class FindItemByName
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItemByName", AccessLevel.Administrator, new CommandEventHandler(FindItemByName_OnCommand));
        }

        [Usage("FindItemByName <name>")]
        [Description("Finds an item by name.")]
        public static void FindItemByName_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                LogHelper Logger = new LogHelper("FindItemByName.log", e.Mobile, false);

                string name = e.GetString(0).ToLower();

                foreach (Item item in World.Items.Values)
                    if (item.Name != null && item.Name.ToLower().IndexOf(name) >= 0)
                        Logger.Log(LogType.Item, item);

                Logger.Finish();


            }
            else
            {
                e.Mobile.SendMessage("Format: FindItemByName <name>");
            }
        }
    }
}
