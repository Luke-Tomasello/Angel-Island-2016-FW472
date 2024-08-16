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

/* Scripts/Commands/FindItemByID.cs
 * ChangeLog
 *  3//26/07, Adam
 *      Convert to find an item by ItemID
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *		Reformatted so readable (functionality left unchanged).
 *	03/23/05, erlein
 *		Moved to /Scripts/Commands/FindItemByID.cs (for Find* command normalization).
 *		Changed namespace to Server.Commands.
 *	9/15/04, Adam
 *		Added header and copyright
 */

using System;
namespace Server.Commands
{

    public class FindItemByID
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItemByID", AccessLevel.GameMaster, new CommandEventHandler(FindItemByID_OnCommand));
        }

        [Usage("FindItemByID <ItemID>")]
        [Description("Finds an item by graphic ID.")]
        public static void FindItemByID_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length == 1)
                {
                    //erl: LogHelper class handles generic logging functionality
                    LogHelper Logger = new LogHelper("FindItemByID.log", e.Mobile, false);

                    int ItemId = 0;
                    string sx = e.GetString(0).ToLower();

                    try
                    {
                        if (sx.StartsWith("0x"))
                        {   // assume hex
                            sx = sx.Substring(2);
                            ItemId = int.Parse(sx, System.Globalization.NumberStyles.AllowHexSpecifier);
                        }
                        else
                        {   // assume decimal
                            ItemId = int.Parse(sx);
                        }
                    }
                    catch
                    {
                        e.Mobile.SendMessage("Format: FindItemByID <ItemID>");
                        return;
                    }

                    foreach (Item ix in World.Items.Values)
                    {
                        if (ix is Item)
                            if (ix.ItemID == ItemId)
                            {
                                Logger.Log(LogType.Item, ix);
                            }
                    }
                    Logger.Finish();
                }
                else
                    e.Mobile.SendMessage("Format: FindItemByID <ItemID>");
            }
            catch (Exception err)
            {

                e.Mobile.SendMessage("Exception: " + err.Message);
            }

            e.Mobile.SendMessage("Done.");
        }
    }
}
