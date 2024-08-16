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

using Server.ContextMenus;
using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class BaseHire : BaseCreature
    {
        private int m_Pay = 1;
        private bool m_IsHired;
        private int m_HoldGold = 0;
        private Timer m_PayTimer;

        public BaseHire(AIType AI)
            : base(AI, FightMode.Aggressor, 10, 1, 0.1, 4.0)
        {
        }

        public BaseHire()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.1, 4.0)
        {
        }

        public BaseHire(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version 

            writer.Write((bool)m_IsHired);
            writer.Write((int)m_HoldGold);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_IsHired = reader.ReadBool();
            m_HoldGold = reader.ReadInt();

            m_PayTimer = new PayTimer(this);
            m_PayTimer.Start();

        }

        private static Hashtable m_HireTable = new Hashtable();

        public static Hashtable HireTable
        {
            get { return m_HireTable; }
        }

        public override bool KeepsItemsOnDeath { get { return true; } }
        //private int m_GoldOnDeath = 0; 
        public override bool OnBeforeDeath()
        {
            // Stop the pay timer if its running 
            if (m_PayTimer != null)
                m_PayTimer.Stop();

            m_PayTimer = null;

            return base.OnBeforeDeath();

        }


        [CommandProperty(AccessLevel.Administrator)]
        public bool IsHired
        {
            get
            {
                return m_IsHired;
            }
            set
            {
                if (m_IsHired == value)
                    return;

                m_IsHired = value;
                Delta(MobileDelta.Noto);
                InvalidateProperties();
            }
        }

        #region [ GetOwner ]
        public virtual Mobile GetOwner()
        {
            if (!Controlled)
                return null;
            Mobile Owner = ControlMaster;

            m_IsHired = true;

            if (Owner == null)
                return null;

            if (Owner.Deleted || Owner.Map != this.Map || !Owner.InRange(Location, 30))
            {
                Say(1005653); // Hmmm.  I seem to have lost my master. 
                BaseHire.HireTable.Remove(Owner);
                SetControlMaster(null);
                return null;
            }

            return Owner;
        }
        #endregion

        #region [ AddHire ]
        public virtual bool AddHire(Mobile m)
        {
            Mobile owner = GetOwner();

            if (owner != null)
            {
                m.SendLocalizedMessage(1043283, owner.Name); // I am following ~1_NAME~. 
                return false;
            }

            if (SetControlMaster(m))
            {
                m_IsHired = true;
                return true;
            }

            return false;
        }
        #endregion

        #region [ Payday ]
        public virtual bool Payday(BaseHire m)
        {
            m_Pay = 0;
            m_Pay += (int)m.Skills[SkillName.Macing].Base + (int)m.Skills[SkillName.Swords].Base;
            m_Pay += (int)m.Skills[SkillName.Fencing].Base + (int)m.Skills[SkillName.Wrestling].Base;

            if (m_Pay < 50)
            {
                m_Pay = 6;
            }
            else if (m_Pay >= 50 && m_Pay < 65)
            {
                m_Pay = 7;
            }
            else if (m_Pay >= 65 && m_Pay < 80)
            {
                m_Pay = 8;
            }
            else
            {
                m_Pay = 9;
            }

            return true;
        }
        #endregion

        #region [ OnDragDrop ]
        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (m_Pay != 0)
            {
                // Is the creature already hired 
                if (Controlled == false)
                {
                    // Is the item the payment in gold 
                    if (item is Gold)
                    {
                        // Is the payment in gold sufficient 
                        if (item.Amount >= m_Pay)
                        {
                            // Check if this mobile already has a hire 
                            BaseHire hire = (BaseHire)m_HireTable[from];

                            if (hire != null && !hire.Deleted && hire.GetOwner() == from)
                            {
                                SayTo(from, "I see you have already hired someone else.");
                                return false;
                            }

                            // Try to add the hireling as a follower 
                            if (AddHire(from) == true)
                            {
                                SayTo(from, 1043258, string.Format("{0}", (int)item.Amount / m_Pay));//"I thank thee for paying me. I will work for thee for ~1_NUMBER~ days.", (int)item.Amount / m_Pay ); 
                                m_HireTable[from] = this;
                                m_HoldGold += item.Amount;
                                m_PayTimer = new PayTimer(this);
                                m_PayTimer.Start();
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                        {
                            this.SayHireCost();
                        }
                    }
                    else
                    {
                        SayTo(from, 1043268); // Tis crass of me, but I want gold 
                    }
                }
                else
                {
                    Say(1042495);// I have already been hired. 
                }
            }
            else
            {
                SayTo(from, 500200); // I have no need for that. 
            }

            return base.OnDragDrop(from, item);
        }
        #endregion


        #region [ OnSpeech ]
        internal void SayHireCost()
        {
            Say(1043256, string.Format("{0}", m_Pay)); // "I am available for hire for ~1_AMOUNT~ gold coins a day. If thou dost give me gold, I will work for thee." 
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.Mobile.InRange(this, 6))
            {
                int[] keywords = e.Keywords;
                string speech = e.Speech;

                // Check for a greeting or 'Hire' 
                if ((e.HasKeyword(0x003B) == true) || (e.HasKeyword(0x0162) == true))
                {
                    e.Handled = Payday(this);
                    this.SayHireCost();
                }
            }

            base.OnSpeech(e);
        }
        #endregion

        #region [ GetContextMenuEntries ]
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            Mobile Owner = GetOwner();

            if (Owner == null)
            {
                base.GetContextMenuEntries(from, list);
                list.Add(new HireEntry(from, this));
            }
            else
                base.GetContextMenuEntries(from, list);
        }
        #endregion

        #region [ Class PayTimer ]
        private class PayTimer : Timer
        {
            private BaseHire m_Hire;

            public PayTimer(BaseHire vend)
                : base(TimeSpan.FromMinutes(Clock.MinutesPerUODay), TimeSpan.FromMinutes(Clock.MinutesPerUODay))
            {
                m_Hire = vend;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                int m_Pay = m_Hire.m_Pay;
                if (m_Hire.m_HoldGold <= m_Pay)
                {
                    // Get the current owner, if any (updates HireTable) 
                    Mobile owner = m_Hire.GetOwner();

                    m_Hire.Say(503235); // I regret nothing!postal 
                    m_Hire.Delete();
                }
                else
                {
                    m_Hire.m_HoldGold -= m_Pay;
                }
            }
        }
        #endregion


        #region [ CheckGold ]
        public override bool CheckGold(Mobile from, Item dropped)
        {
            if (dropped is Gold)
                return OnGoldGiven(from, (Gold)dropped);

            return false;
        }

        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            return false;
        }
        #endregion

        #region [ CanBeRenamedBy ]
        public override bool CanBeRenamedBy(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }
            else
                return false;
        }
        #endregion


        #region [ Class HireEntry ]
        public class HireEntry : ContextMenuEntry
        {
            private Mobile m_Mobile;
            private BaseHire m_Hire;

            public HireEntry(Mobile from, BaseHire hire)
                : base(6120, 3)
            {
                m_Hire = hire;
                m_Mobile = from;
            }

            public override void OnClick()
            {
                m_Hire.Payday(m_Hire);
                m_Hire.SayHireCost();
            }
        }
        #endregion
    }
}
