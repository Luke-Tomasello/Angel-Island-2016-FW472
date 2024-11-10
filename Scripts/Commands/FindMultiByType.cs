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

/* Scripts/Commands/FindMultiByType.cs
 * Changelog : 
 *	3/9/07, Adam
 *		first time checkin
 */

using Server.Diagnostics;
using Server.Multis;
using System;
using System.Collections;

namespace Server.Commands
{
    public class FindMultiByType
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindMultiByType", AccessLevel.Administrator, new CommandEventHandler(FindMultiByType_OnCommand));
        }

        [Usage("FindMultiByType <type>")]
        [Description("Finds a multi by type.")]
        public static void FindMultiByType_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length == 1)
                {
                    LogHelper Logger = new LogHelper("FindMultiByType.log", e.Mobile, false);

                    string name = e.GetString(0);

                    foreach (ArrayList list in Server.Multis.BaseHouse.Multis.Values)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            BaseHouse house = list[i] as BaseHouse;
                            // like Server.Multis.Tower
                            if (house.GetType().ToString().ToLower().IndexOf(name.ToLower()) >= 0)
                            {
                                Logger.Log(house);
                            }
                        }
                    }
                    Logger.Finish();
                }
                else
                {
                    e.Mobile.SendMessage("Format: FindMultiByType <type>");
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}
