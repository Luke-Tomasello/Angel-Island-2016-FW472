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

/* Scripts/Commands/Abstracted/Implementors/GlobalCommandImplementor.cs
 * CHANGELOG
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;
using System.Collections;

namespace Server.Commands
{
    public class GlobalCommandImplementor : BaseCommandImplementor
    {
        public GlobalCommandImplementor()
        {
            Accessors = new string[] { "Global" };
            SupportRequirement = CommandSupport.Global;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.Administrator;
            Usage = "Global <command> [condition]";
            Description = "Invokes the command on all appropriate objects in the world. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                ObjectConditional cond = ObjectConditional.Parse(from, ref args);

                bool items, mobiles;

                if (!CheckObjectTypes(command, cond, out items, out mobiles))
                    return;

                ArrayList list = new ArrayList();

                if (items)
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (cond.CheckCondition(item))
                            list.Add(item);
                    }
                }

                if (mobiles)
                {
                    foreach (Mobile mob in World.Mobiles.Values)
                    {
                        if (cond.CheckCondition(mob))
                            list.Add(mob);
                    }
                }

                obj = list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                from.SendMessage(ex.Message);
            }
        }
    }
}
