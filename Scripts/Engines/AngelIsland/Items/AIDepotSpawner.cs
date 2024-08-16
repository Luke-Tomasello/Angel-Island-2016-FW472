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

// Engines/AngelIsland/AIDepotSpawner.cs
// 4/30/04 Created by Pixie;

using System;
namespace Server.Items
{
    public class AIDepotSpawner : Item
    {
        private bool m_Running;         //active ? 
        private DateTime m_End;         //time to next respawn 
        private InternalTimer m_Timer;  //internaltimer 
        private Container m_BandageContainer;
        private Container m_GHPotionContainer;
        private Container m_ReagentContainer;

        [Constructable]
        public AIDepotSpawner()
            : base(0x1f13)
        {
            InitSpawn();
        }

        public AIDepotSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.CloseGump(typeof(Server.Gumps.PropertiesGump));
            from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Running)
                LabelTo(from, "[Running]");
            else
                LabelTo(from, "[Off]");
        }


        public void InitSpawn()
        {
            Visible = false;
            Movable = true;
            Name = "AI DepotSpawner";
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get
            {
                return m_Running;
            }
            set
            {
                if (value)
                    Start();
                else
                    Stop();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Container BandageContainer
        {
            get
            {
                return m_BandageContainer;
            }
            set
            {
                m_BandageContainer = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Container GHPotContainer
        {
            get
            {
                return m_GHPotionContainer;
            }
            set
            {
                m_GHPotionContainer = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Container ReagentContainer
        {
            get
            {
                return m_ReagentContainer;
            }
            set
            {
                m_ReagentContainer = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (m_Running)
                    return m_End - DateTime.Now;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                Start();
                DoTimer(value);
            }
        }

        public void Spawn()
        {
            int bandies = CoreAI.SpiritDepotBandies;
            int ghpots = CoreAI.SpiritDepotGHPots;
            int regs = CoreAI.SpiritDepotReagents;

            if (this.m_BandageContainer != null)
            {
                //clear of all existing bandaids
                Item[] contents = m_BandageContainer.FindItemsByType(typeof(Bandage), true);
                foreach (Item b in contents)
                {
                    b.Delete();
                }

                Item item = new Bandage(bandies);
                this.m_BandageContainer.DropItem(item);
            }
            if (this.m_GHPotionContainer != null)
            {
                //clear of all existing ghpots
                Item[] contents = m_GHPotionContainer.FindItemsByType(typeof(GreaterHealPotion), true);
                foreach (Item b in contents)
                {
                    b.Delete();
                }

                for (int i = 0; i < ghpots; i++)
                {
                    Item item = new GreaterHealPotion();
                    this.m_GHPotionContainer.DropItem(item);
                }
            }
            if (this.m_ReagentContainer != null)
            {
                //delete all reagents in container
                foreach (Type t in Loot.RegTypes)
                {
                    Item[] contents = m_ReagentContainer.FindItemsByType(t);
                    foreach (Item b in contents)
                    {
                        b.Delete();
                    }
                }

                int iTotal = regs;
                while (iTotal > 0)
                {
                    Item item = Loot.RandomReagent();
                    int count = Utility.RandomMinMax(1, 10);
                    if (count > iTotal) count = iTotal;
                    iTotal -= count;
                    item.Amount = count;
                    this.m_ReagentContainer.DropItem(item);
                }
            }
        }

        public void Start()
        {
            if (!m_Running)
            {
                m_Running = true;
                DoTimer();
            }
        }


        public void Stop()
        {
            if (m_Running)
            {
                m_Timer.Stop();
                m_Running = false;
            }
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version 

            writer.Write(m_BandageContainer);
            writer.Write(m_GHPotionContainer);
            writer.Write(m_ReagentContainer);
            writer.Write(m_Running);
            if (m_Running)
                writer.Write(m_End - DateTime.Now);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_BandageContainer = reader.ReadItem() as Container;
            m_GHPotionContainer = reader.ReadItem() as Container;
            m_ReagentContainer = reader.ReadItem() as Container;
            m_Running = reader.ReadBool();

            if (m_Running)
            {
                TimeSpan delay = reader.ReadTimeSpan();
                DoTimer(delay);
            }
        }

        public void OnTick()
        {
            DoTimer();
            Spawn();
        }

        public void DoTimer()
        {
            if (!m_Running)
                return;

            TimeSpan delay = TimeSpan.FromSeconds(CoreAI.SpiritDepotRespawnFreq);
            DoTimer(delay);
        }


        public void DoTimer(TimeSpan delay)
        {
            if (!m_Running)
                return;

            m_End = DateTime.Now + delay;

            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }

        private class InternalTimer : Timer
        {
            private AIDepotSpawner m_Spawner;

            public InternalTimer(AIDepotSpawner spawner, TimeSpan delay)
                : base(delay)
            {
                Priority = TimerPriority.OneSecond;
                m_Spawner = spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null)
                    if (!m_Spawner.Deleted)
                        m_Spawner.OnTick();
            }
        } //end of InternalTimer

    } //end of AIDepotSpawner


}
