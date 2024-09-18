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

/* Scripts/Mobiles/Guards/PatrolGuard.cs
 * Changelog
 * 6/21/10, Adam
 *		rewrite such that PatrolGuard is now based upon on WarriorGuard
 * 04/19/05, Kit
 *		Added check to onmovement not to attack player initially if they are hidden.
 *		updated bank closeing code to not have guard change direction of player(was causeing them to be paralyzed) 
 * 9/30/04, Pigpen
 * 		Fixed an issues where this guard would try to chase a hidden >player char if that char has more that 5 counts. Spamming Reveal etc.
 * 8/7/04, Old Salty
 * 		Patrol Guards now check to see if the region is guarded before attacking reds on sight.
 * 7/26/04, Old Salty
 * 		Added a few lines (97-100) to make the criminal turn, closing the bankbox, when the guard attacks.
 * 6/22/04, Old Salty
 * 		PatrolGuards now deal extra damage to NPC's like their *poof*guard counterparts.
 * 		PatrolGuards now respond more specifically to speech.
 * 6/21/04, Old Salty
 * 		Modified the search/reveal code so that guards can reveal players.
 * created 6/10/04 by mith
 *		These are guards designed for patroling banks and other town areas without going *poof*
 */

namespace Server.Mobiles
{
    public class PatrolGuard : WarriorGuard
    {
        [Constructable]
        public PatrolGuard()
            : this(null)
        {
        }

        public PatrolGuard(Mobile target)
            : base(target)
        {
        }

        public PatrolGuard(Serial serial)
            : base(serial)
        {
        }

        // does this guard auto 'poof' when en no longer needed?
        public override bool PoofingGuard { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if (base.Version > 0)
            {
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {   // no work
                            break;
                        }
                }
            }
        }
    }
}
