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

/* Scripts/Commands/FindGuild.cs
 * Changelog
 *	06/14/06, Adam
 *		Add the account name to the display
 *	05/17/06, Kit
 *		Initial creation.
 */
using Server.Guilds;
using Server.Items;
using Server.Mobiles;

namespace Server.Commands
{
    public class FindGuild
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindGuild", AccessLevel.GameMaster, new CommandEventHandler(FindGuild_OnCommand));
        }

        [Usage("FindGuild <abbrevation>")]
        [Description("Finds a guild by abbrevation.")]
        public static void FindGuild_OnCommand(CommandEventArgs e)
        {
            Guild temp = null;
            PlayerMobile ptemp = null;

            if (e.Length == 1)
            {
                string name = e.GetString(0).ToLower();

                foreach (Item n in World.Items.Values)
                {
                    if (n is Guildstone && n != null)
                    {
                        if (((Guildstone)n).Guild != null)
                            temp = ((Guildstone)n).Guild;

                        if (temp.Abbreviation.ToLower() == name)
                        {
                            if (n.Parent != null && n.Parent is PlayerMobile)
                            {
                                ptemp = (PlayerMobile)n.Parent;
                                e.Mobile.SendMessage("Guild Stone Found on Mobile {2}:{0}, {1}", ptemp.Name, ptemp.Location, ptemp.Account);
                            }
                            else
                            {
                                e.Mobile.SendMessage("Guild Stone {1} Found at: {0} ({2})", n.Location, n.Serial, n.Map);
                            }
                            return;
                        }
                    }
                }
                e.Mobile.SendMessage("Guild Stone not found in world");
            }
            else
            {
                e.Mobile.SendMessage("Format: FindGuild <abbreviation>");
            }
        }
    }
}
