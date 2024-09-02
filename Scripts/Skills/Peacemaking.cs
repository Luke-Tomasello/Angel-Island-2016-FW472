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

/* Scripts/Skills/PeaceMaking.cs
 * ChangeLog
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  2/1/08, Pix
 *      Protected RTT test from ever being called, even on TC, but left the code.
 *	8/31/07, Adam
 *		Change CheckTargetSkill() to check against a max skill of 100 instead of 120
 *	8/30/07, Adam
 *		Revert change below and move the logic to m_Instrument.GetDifficultyFor(creature)
 *	8/28/07, Adam
 *		Fail peace on Paragon creatures (with no message)
 *	8/26/07, Pix.
 *		Added RTT to peacemaking.
 *  1/26/07, Adam
 *      - new dynamic property system
 *      - Invoke the new OnPeace() which allows NPCs to speak out if someone tries to peace them
 *	1/5/06, weaver
 *		Made targetted peacing an aggressive action.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class Peacemaking
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Peacemaking].Callback = new SkillUseCallback(OnUse);
        }

        private static bool bUseRTT = false;

        public static TimeSpan OnUse(Mobile m)
        {
            PlayerMobile pm = m as PlayerMobile;

            if (bUseRTT && TestCenter.Enabled && pm != null)
            {
                pm.RTT("Please verify that you're at your computer.  Use of this skill will be disabled if too many failures occur.", false, 2, "Peacemaking");
            }

            if (bUseRTT && TestCenter.Enabled && pm != null && pm.RTTFailures >= 3)
            {
                pm.SendMessage("You cannot use this skill because you have failed the AFK check too many times in a row.");
                pm.SendMessage("After some time has elapsed, you will be tested again when you use the skill.");
                return TimeSpan.FromSeconds(5.0);
            }
            else
            {
                m.RevealingAction();

                BaseInstrument.PickInstrument(m, new InstrumentPickedCallback(OnPickedInstrument));

                return TimeSpan.FromSeconds(1.0); // Cannot use another skill for 1 second
            }
        }

        public static void OnPickedInstrument(Mobile from, BaseInstrument instrument)
        {
            from.RevealingAction();
            from.SendLocalizedMessage(1049525); // Whom do you wish to calm?
            from.Target = new InternalTarget(from, instrument);
            from.NextSkillTime = DateTime.UtcNow + TimeSpan.FromHours(6.0);
        }

        private class InternalTarget : Target
        {
            private BaseInstrument m_Instrument;
            private bool m_SetSkillTime = true;

            public InternalTarget(Mobile from, BaseInstrument instrument)
                : base(BaseInstrument.GetBardRange(from, SkillName.Peacemaking), false, TargetFlags.None)
            {
                m_Instrument = instrument;
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                    from.NextSkillTime = DateTime.UtcNow;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                from.RevealingAction();

                if (!(targeted is Mobile))
                {
                    from.SendLocalizedMessage(1049528); // You cannot calm that!
                }
                else
                {
                    m_SetSkillTime = false;
                    from.NextSkillTime = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);

                    if (targeted == from)
                    {
                        // Standard mode : reset combatants for everyone in the area

                        if (!BaseInstrument.CheckMusicianship(from))
                        {
                            from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else if (!from.CheckSkill(SkillName.Peacemaking, 0.0, 100.0))
                        {
                            from.SendLocalizedMessage(500613); // You attempt to calm everyone, but fail.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else
                        {
                            m_Instrument.PlayInstrumentWell(from);
                            m_Instrument.ConsumeUse(from);

                            Map map = from.Map;

                            if (map != null)
                            {
                                int range = BaseInstrument.GetBardRange(from, SkillName.Peacemaking);

                                bool calmed = false;

                                IPooledEnumerable eable = from.GetMobilesInRange(range);
                                foreach (Mobile m in eable)
                                {
                                    // execute the new Peace Action, and allow the NPC to speak out!
                                    if (m is BaseCreature)
                                        ((BaseCreature)m).OnPeace();

                                    if ((m is BaseCreature && ((BaseCreature)m).Uncalmable) || m == from || !from.CanBeHarmful(m, false))
                                        continue;

                                    calmed = true;

                                    m.SendLocalizedMessage(500616); // You hear lovely music, and forget to continue battling!
                                    m.Combatant = null;
                                    m.Warmode = false;

                                    if (m is BaseCreature && !((BaseCreature)m).BardPacified)
                                        ((BaseCreature)m).Pacify(from, DateTime.UtcNow + TimeSpan.FromSeconds(1.0));
                                }
                                eable.Free();

                                if (!calmed)
                                    from.SendLocalizedMessage(1049648); // You play hypnotic music, but there is nothing in range for you to calm.
                                else
                                    from.SendLocalizedMessage(500615); // You play your hypnotic music, stopping the battle.
                            }
                        }
                    }
                    else
                    {
                        // Target mode : pacify a single target for a longer duration
                        Mobile targ = (Mobile)targeted;

                        // wea: made this an aggressive action
                        from.DoHarmful(targ);

                        // execute the new Peace Action, and allow the NPC to speak out!
                        if (targ is BaseCreature)
                            ((BaseCreature)targ).OnPeace();

                        if (!from.CanBeHarmful(targ, false))
                        {
                            from.SendLocalizedMessage(1049528);
                            m_SetSkillTime = true;
                        }
                        else if (targ is BaseCreature && ((BaseCreature)targ).Uncalmable)
                        {
                            from.SendLocalizedMessage(1049526); // You have no chance of calming that creature.
                            m_SetSkillTime = true;
                        }
                        else if (targ is BaseCreature && ((BaseCreature)targ).BardPacified)
                        {
                            from.SendLocalizedMessage(1049527); // That creature is already being calmed.
                            m_SetSkillTime = true;
                        }
                        else if (!BaseInstrument.CheckMusicianship(from))
                        {
                            from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else
                        {
                            double diff = m_Instrument.GetDifficultyFor(targ) - 10.0;
                            double music = from.Skills[SkillName.Musicianship].Value;

                            if (music > 100.0)
                                diff -= (music - 100.0) * 0.5;

                            if (!from.CheckTargetSkill(SkillName.Peacemaking, targ, diff - 25.0, diff + 25.0))
                            {
                                from.SendLocalizedMessage(1049531); // You attempt to calm your target, but fail.
                                m_Instrument.PlayInstrumentBadly(from);
                                m_Instrument.ConsumeUse(from);
                            }
                            else
                            {
                                m_Instrument.PlayInstrumentWell(from);
                                m_Instrument.ConsumeUse(from);

                                if (targ is BaseCreature)
                                {
                                    BaseCreature bc = (BaseCreature)targ;
                                    from.SendLocalizedMessage(1049532); // You play hypnotic music, calming your target.

                                    targ.Combatant = null;
                                    targ.Warmode = false;

                                    double seconds = 100 - (diff / 1.5);

                                    if (seconds > 120)
                                        seconds = 120;
                                    else if (seconds < 10)
                                        seconds = 10;

                                    bc.Pacify(from, DateTime.UtcNow + TimeSpan.FromSeconds(seconds));

                                }
                                else
                                {
                                    from.SendLocalizedMessage(1049532); // You play hypnotic music, calming your target.

                                    targ.SendLocalizedMessage(500616); // You hear lovely music, and forget to continue battling!
                                    targ.Combatant = null;
                                    targ.Warmode = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
