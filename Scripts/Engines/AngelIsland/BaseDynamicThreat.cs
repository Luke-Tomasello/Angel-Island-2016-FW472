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

/* Scripts/Engines/AngelIsland/AILevelSystem/Mobiles/BaseDynamicThreat.cs
 * ChangeLog
 *	3/10/10, Adam
 *		we don't want out mobs to stop healing when the players leave the sector (a trick for killing monsters)
 *		So, if we need to heal, don't deactivate when all players leave the sector
 *		PlayerRangeSensitive { get { return Hits == HitsMax; } }
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	7/15/07, Adam
 *		- Base threat evaluation on OnDamage() and the Aggressor.Count
 *			Basically, all players that attack the mob will increase it's STR
 *		- Update mob's STR and not hits. Updating hits 'heals' the creature, and we don't want that
 *	6/15/06, Adam
 *		normalize all spirit spawn dynamic threat code into this common base class
 */

using System;

namespace Server.Mobiles
{
    public class BaseDynamicThreat : BaseCreature
    {
        private bool m_bThreatKnown = false;
        private DateTime m_DecayStats = DateTime.MinValue;
        private int m_BaseVirtualArmor;
        private int m_BaseHits;
        private int m_threatLevel;

        public BaseDynamicThreat(AIType ai, FightMode mode, int iRangePerception, int iRangeFight, double dActiveSpeed, double dPassiveSpeed)
            : base(ai, mode, iRangePerception, iRangeFight, dActiveSpeed, dPassiveSpeed)
        {
        }

        public BaseDynamicThreat(Serial serial)
            : base(serial)
        {
        }

        public virtual void InitStats(int iHits, int iVirtualArmor)
        {
        }

        public int BaseVirtualArmor
        {
            get { return m_BaseVirtualArmor; }
            set { m_BaseVirtualArmor = value; }
        }

        public int BaseHits
        {
            get { return m_BaseHits; }
            set { m_BaseHits = value; }
        }

        // Adam; we don't want out mobs to stop healing when the players leave the sector (a trick for killing monsters)
        public override bool PlayerRangeSensitive { get { return Hits == HitsMax; } }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (Aggressors.Count > m_threatLevel)
            {
                m_threatLevel = Aggressors.Count;
                if (m_bThreatKnown == false)
                {
                    this.DebugSay("Determining Threat");

                    int players = 0;
                    double SkillFactor = 0.0;
                    DetermineThreat(out players, out SkillFactor);
                    if (players > 0)
                    {
                        m_bThreatKnown = true;
                        this.DebugSay("Threat Known!");
                        this.DebugSay(SkillFactor.ToString());

                        ///////////////////////////////////////////////////////////
                        // manage FightMode.
                        //	If there 3 or more players, we switch into a very agressive mode
                        //	where we focus on the weakest player :P
                        switch (players)
                        {
                            case 0:
                            case 1:
                                if ((FightMode & FightMode.Closest) == 0)
                                {
                                    this.DebugSay("Switching to FightMode.Closest");
                                    FightMode = FightMode.All | FightMode.Closest;
                                }
                                break;
                            case 2:
                                if ((FightMode & FightMode.Strongest) == 0)
                                {
                                    this.DebugSay("Switching to FightMode.Strongest");
                                    FightMode = FightMode.All | FightMode.Strongest;
                                }
                                break;
                            case 3:
                            case 4:
                            default:
                                if ((FightMode & FightMode.Weakest) == 0)
                                {
                                    this.DebugSay("Switching to FightMode.Weakest");
                                    FightMode = FightMode.All | FightMode.Weakest;
                                }
                                break;
                        }

                        // HIT = base + 30% per player
                        InitStats(BaseHits + (int)((BaseHits * 0.3) * players), BaseVirtualArmor);
                    }
                }
            }
            base.OnDamage(amount, from, willKill);
        }

        // Adam: manage the threat
        public void DetermineThreat(out int players, out double SkillFactor)
        {
            //this.DebugSay( "Determining Threat" );
            int count = 0;
            double skills = 0.0;
            IPooledEnumerable eable = this.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m != null && m is PlayerMobile && m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    double temp = 0.0;
                    temp += m.Skills[SkillName.Archery].Base;
                    temp += m.Skills[SkillName.EvalInt].Base;
                    temp += m.Skills[SkillName.Magery].Base;
                    temp += m.Skills[SkillName.MagicResist].Base;
                    temp += m.Skills[SkillName.Tactics].Base;
                    temp += m.Skills[SkillName.Swords].Base;
                    temp += m.Skills[SkillName.Macing].Base;
                    temp += m.Skills[SkillName.Fencing].Base;
                    temp += m.Skills[SkillName.Wrestling].Base;
                    temp += m.Skills[SkillName.Lumberjacking].Base;
                    temp += m.Skills[SkillName.Meditation].Base;
                    temp += m.Skills[SkillName.Anatomy].Base;
                    //temp += m.Skills[SkillName.Parry].Base;
                    //temp += m.Skills[SkillName.Peacemaking].Base;
                    //temp += m.Skills[SkillName.Discordance].Base;
                    //temp += m.Skills[SkillName.Provocation].Base;

                    count++;
                    skills += temp * .001;
                    this.DebugSay(m.Name);
                    this.DebugSay(skills.ToString());
                }
            }
            eable.Free();

            SkillFactor = skills;
            players = count;
            return;
        }

        // Decay any old knowledge of combatants
        // called every 15 minutes.
        public override bool CheckWork()
        {
            // if we are full str
            if (this.Hits == this.HitsMax)
                // if 4 hours have passed without incident
                if (DateTime.UtcNow - m_DecayStats > TimeSpan.FromHours(4.0))
                {
                    m_bThreatKnown = false;
                    m_DecayStats = DateTime.UtcNow;
                    m_threatLevel = 0;
                }

            // always return false to indicate no further work needs to be done
            return false;
        }

        public override void OnActionCombat()
        {   // refresh dynamic stats the decay timer
            m_DecayStats = DateTime.UtcNow;
            base.OnActionCombat();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_threatLevel);

            // version 0
            writer.Write(m_bThreatKnown);
            writer.Write(m_DecayStats);
            writer.Write(m_BaseVirtualArmor);
            writer.Write(m_BaseHits);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_threatLevel = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_bThreatKnown = reader.ReadBool();
                        m_DecayStats = reader.ReadDateTime();
                        m_BaseVirtualArmor = reader.ReadInt();
                        m_BaseHits = reader.ReadInt();
                        break;
                    }
            }
        }
    }
}
