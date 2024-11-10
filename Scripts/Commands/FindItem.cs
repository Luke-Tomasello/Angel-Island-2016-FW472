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

/* Scripts/Commands/FindItem.cs
 * Changelog
 *  12/24/05, Kit
 *		changed to use logtype.ItemSerial which outputs serial item as well as item details
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *	03/23/05, erlein
 *		Moved to /Scripts/Commands/FindItem.cs (for Find* command normalization)
 *	03/22/05, erlein
 *	    Fixed location, changed output format, moved speed output to
 *		client from console window, made all matches case insensitive.
 *	03/22/05, erlein
 *		Altered so reflects type before iteration of instances.
 *		Added regex match to speed up string matching.
 *	03/16/05, erlein
 *		Initial creation.
 */

using Server.Diagnostics;
using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Server.Commands
{
    public class FindItem
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindItem", AccessLevel.Administrator, new CommandEventHandler(FindItem_OnCommand));
        }

        [Usage("FindItem <property> <value>")]
        [Description("Finds all items with property matching value.")]
        public static void FindItem_OnCommand(CommandEventArgs e)
        {
            if (e.Length > 1)
            {

                LogHelper Logger = new LogHelper("finditem.log", e.Mobile, false);

                // Extract property & value from command parameters

                string sProp = e.GetString(0);
                string sVal = "";

                if (e.Length > 2)
                {

                    sVal = e.GetString(1);

                    // Concatenate the strings
                    for (int argi = 2; argi < e.Length; argi++)
                        sVal += " " + e.GetString(argi);
                }
                else
                    sVal = e.GetString(1);

                Regex PattMatch = new Regex("= \"*" + sVal, RegexOptions.IgnoreCase);

                // Loop through assemblies and add type if has property

                Type[] types;
                Assembly[] asms = ScriptCompiler.Assemblies;

                ArrayList MatchTypes = new ArrayList();

                for (int i = 0; i < asms.Length; ++i)
                {
                    types = ScriptCompiler.GetTypeCache(asms[i]).Types;

                    foreach (Type t in types)
                    {

                        if (typeof(Item).IsAssignableFrom(t))
                        {

                            // Reflect type
                            PropertyInfo[] allProps = t.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                            foreach (PropertyInfo prop in allProps)
                                if (prop.Name.ToLower() == sProp.ToLower())
                                    MatchTypes.Add(t);
                        }
                    }
                }

                // Loop items and check vs. types

                foreach (Item item in World.Items.Values)
                {
                    Type t = item.GetType();
                    bool match = false;

                    foreach (Type MatchType in MatchTypes)
                    {
                        if (t == MatchType)
                        {
                            match = true;
                            break;
                        }
                    }

                    if (match == false)
                        continue;

                    // Reflect instance of type (matched)

                    if (PattMatch.IsMatch(Properties.GetValue(e.Mobile, item, sProp)))
                        Logger.Log(LogType.ItemSerial, item);

                }

                Logger.Finish();
            }
            else
            {
                // Badly formatted
                e.Mobile.SendMessage("Format: FindItem <property> <value>");
            }
        }
    }
}
