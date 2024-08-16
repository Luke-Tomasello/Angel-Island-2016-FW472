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

/* ChangeLog:
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Commands;
using Server.Targeting;

namespace Server.Targets
{
    public class PickMoveTarget : Target
    {
        public PickMoveTarget()
            : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (!BaseCommand.IsAccessible(from, o))
            {
                from.SendMessage("That is not accessible.");
                return;
            }

            if (o is Item || o is Mobile)
                from.Target = new MoveTarget(o);
        }
    }
}
