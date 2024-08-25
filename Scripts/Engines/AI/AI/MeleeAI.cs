using System;
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

/* Scripts/Engines/AI/AI/MeleeAI.cs
 * CHANGELOG
 *	7/11/10, adam
 *		o major reorganization of AI
 *			o push most smart-ai logic from the advanced magery classes down to baseAI so that we can use potions and bandages from 
 *				the new advanced melee class
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *  04/22/06, Kit
 *		Fixed glitchy running where creatures look like their on crack or rather
 *		animation flip flops in attack graphic forever while moving.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    public class MeleeAI : BaseAI
    {
        public MeleeAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            m_Mobile.DebugSay("I have no combatant");

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                if (m_Mobile.Debug)
                    m_Mobile.DebugSay("I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
            }
            else
            {
                base.DoActionWander();
            }

            return true;
        }

        public override bool DoActionCombat()
        {
            Mobile combatant = m_Mobile.Combatant;

            if (combatant == null || combatant.Deleted || combatant.Map != m_Mobile.Map || !combatant.Alive || combatant.IsDeadBondedPet)
            {
                m_Mobile.DebugSay("My combatant is gone, so my guard is up");

                Action = ActionType.Guard;

                return true;
            }

            /*if ( !m_Mobile.InLOS( combatant ) )
			{
				if ( AcquireFocusMob( m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true ) )
				{
					m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
					m_Mobile.FocusMob = null;
				}
			}*/

            if (MoveTo(combatant, true, m_Mobile.RangeFight))
            {
                //m_Mobile.Direction = m_Mobile.GetDirectionTo( combatant );
            }
            else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                if (m_Mobile.Debug)
                    m_Mobile.DebugSay("My move is blocked, so I am going to attack {0}", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;

                return true;
            }
            else if (m_Mobile.GetDistanceToSqrt(combatant) > m_Mobile.RangePerception + 1)
            {
                if (m_Mobile.Debug)
                    m_Mobile.DebugSay("I cannot find {0}, so my guard is up", combatant.Name);

                Action = ActionType.Guard;

                return true;
            }
            else
            {
                if (m_Mobile.Debug)
                    m_Mobile.DebugSay("I should be closer to {0}", combatant.Name);
            }

            if (!m_Mobile.Controlled && !m_Mobile.Summoned)
            {
                if (m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100)
                {
                    // We are low on health, should we flee?

                    bool flee = false;

                    if (m_Mobile.Hits < combatant.Hits)
                    {
                        // We are more hurt than them

                        int diff = combatant.Hits - m_Mobile.Hits;

                        flee = (Utility.Random(0, 100) < (10 + diff)); // (10 + diff)% chance to flee
                    }
                    else
                    {
                        flee = Utility.Random(0, 100) < 10; // 10% chance to flee
                    }

                    if (flee)
                    {
                        if (m_Mobile.Debug)
                            m_Mobile.DebugSay("I am going to flee from {0}", combatant.Name);

                        Action = ActionType.Flee;
                    }
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                if (m_Mobile.Debug)
                    m_Mobile.DebugSay("I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
            }
            else
            {
                base.DoActionGuard();
            }

            return true;
        }

        public override bool DoActionFlee()
        {
            if (m_Mobile.Hits > m_Mobile.HitsMax / 2)
            {
                m_Mobile.DebugSay("I am stronger now, so I will continue fighting");
                Action = ActionType.Combat;
            }
            else
            {
                m_Mobile.FocusMob = m_Mobile.Combatant;
                base.DoActionFlee();
            }

            return true;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}
