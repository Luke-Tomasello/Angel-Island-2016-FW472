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

using Server.Items;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class TasteID
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.TasteID].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(502807); // What would you like to taste?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    from.SendLocalizedMessage(502816); // You feel that such an action would be inappropriate
                }
                else if (targeted is Food)
                {
                    if (from.CheckTargetSkill(SkillName.TasteID, targeted, 0, 100))
                    {
                        Food targ = (Food)targeted;

                        if (targ.Poison != null)
                        {
                            from.SendLocalizedMessage(1038284); // It appears to have poison smeared on it
                        }
                        else
                        {
                            // No poison on the food
                            from.SendLocalizedMessage(502823); // You cannot discern anything about this substance
                        }
                    }
                    else
                    {
                        // Skill check failed
                        from.SendLocalizedMessage(502823); // You cannot discern anything about this substance
                    }
                }
                else
                {
                    // The target is not food. (Potion support in the next version)
                    from.SendLocalizedMessage(502820); // That's not something you can taste
                }
            }
        }
    }
}
