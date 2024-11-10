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

/* Changelog 
 * 1/07/05, Darva
 *		First checkin.
 *
 */
using Server.Mobiles;
using Server.Targeting;
namespace Server.Commands
{
    public class SpawnerCmd
    {
        public static void Initialize()
        {
            Register();
        }

        public static void Register()
        {
            Server.CommandSystem.Register("Spawner", AccessLevel.GameMaster, new CommandEventHandler(Spawner_OnCommand));
        }

        [Usage("Spawner")]
        [Description("Moves you to the spawner of the targeted creature, if any.")]
        private static void Spawner_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new SpawnerTarget();
        }

        private class SpawnerTarget : Target
        {
            public SpawnerTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is BaseCreature)
                {
                    if (((BaseCreature)o).Spawner != null)
                    {
                        BaseCreature bc = (BaseCreature)o;
                        from.MoveToWorld(bc.Spawner.Location, bc.Spawner.Map);
                    }
                    else
                    {
                        from.SendMessage("That mobile is homeless");
                    }
                }
                else if (o is Item)
                {
                    if (((Item)o).SpawnerLocation != Point3D.Zero && (o as Item).SpawnerMap != null)
                    {
                        from.MoveToWorld((o as Item).SpawnerLocation, (o as Item).SpawnerMap);
                    }
                    else
                    {
                        from.SendMessage("That item is not from a spawner");
                    }
                }
                else
                {
                    from.SendMessage("Why would that have a spawner?");
                }
            }
        }
    }
}
