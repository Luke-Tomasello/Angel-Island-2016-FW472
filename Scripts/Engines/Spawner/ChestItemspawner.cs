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

/* Scripts\Engines\Spawner\ChestItemspawner.cs
 * CHANGELOG
 *	3/1/11, Adam
 *		Add a ChanceToSpawn. This powerful fearure allows us to randomly spawn over time.
 *		Example: chanceToSpawn=0.1 and a spawn time of every hour will result in roughly one item spawned every 10 hours. Perfect!
 *	5/13/10, Adam
 *		Change LogType.Mobile to LogType.Item when logging lootpacks (was causing blank lines in the output log)
 *	3/22/10, Adam
 *		1) Restore the ability to spawn one random item from list. Keep the defalut that all items are spawned (needed for AI prison chests)
 *		2) Add the ability for spawn from a LootPack object
 *		3) add new ChestLootPackSpawner for spawning raw items usually constructed by staff (Differs from normal spawners that spawn TYPE items.)
 *  02/28/05, erlein
 *    Added logging of Count property change & deletion of ChestItemSpawner.
 *    Now logs all changes to these in /logs/spawnerchange.log
 *  4/23/04 Pulse
 * 	 Spawner no longer spawns a single random item each Spawn() attempt
 * 	   but instead spawns on of each item type listed in m_ItemsNames
 * 	 The limit of items in a chest is no longer m_Count items but is now
 *      m_Count of each item type listed in m_ItemsNames
 *  4/13/04 pixie
 *    Removed a couple unnecessary checks that were throwing warnings.
 *  4/11/04 pixie
 *    Initial Revision.
 *  4/06/04 Created by Pixie;
 */

using Server.Commands;
using Server.Engines;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Items
{
    public class ChestLootPackSpawner : ChestItemSpawner
    {
        private Item m_LootPack;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item LootPack
        {
            get { return m_LootPack; }
            set
            {   // delete the old one
                if (m_LootPack != null && m_LootPack.Deleted == false && m_LootPack != value)
                    m_LootPack.Delete();

                // remove from current container
                if (value != null && value.Parent != null && value.Parent is Container)
                    (value.Parent as Container).RemoveItem(value);

                // assign
                m_LootPack = value;

                // move to int item storage
                if (m_LootPack != null && m_LootPack.Deleted == false)
                    m_LootPack.MoveItemToIntStorage();

                // finally, add the loot pack to the list of items to be spawned
                this.ItemsName = new ArrayList();
                this.ItemsName.Add(m_LootPack.Serial.ToString());
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool RandomItem
        {
            get
            {
                return base.RandomItem;
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            // Remove LootPack
            if (this.LootPack != null && this.LootPack.Deleted == false)
                this.LootPack.Delete();

            // erl: Log the fact it's been deleted
            LogChange("Spawner deleted");
        }

        [Constructable]
        public ChestLootPackSpawner()
            : base()
        {
            // lootpack spawners don't understand how to spawn the spawn-list understood by normal spawners
            base.RandomItem = true;
        }

        public ChestLootPackSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            else
                from.SendMessage("You are not authorized to use this console.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version 
            writer.Write(m_LootPack);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_LootPack = reader.ReadItem();
                        goto case 0;
                    }
                    break;

                case 0:
                    {

                    }
                    break;
            }
        }
    }

    public class ChestItemSpawner : Item
    {
        private int m_Count;                    //how much items to spawn
        private TimeSpan m_MinDelay;            //min delay to respawn
        private TimeSpan m_MaxDelay;            //max delay to respawn
        private ArrayList m_ItemsName;          //list of item names
        private ArrayList m_Items;              //list of items spawned
        private DateTime m_End;                 //time to next respawn
        private InternalTimer m_Timer;          //internaltimer
        private bool m_Running;                 //active ? 
        private Container m_Container;          //container to spawn in
        private bool m_RandomItem;              //spawn all items in the list or only one?
        private double m_ChanceToSpawn = 1.0;   //chance of spawning an itrem. Default to always for backwards compat


        public ArrayList ItemsName
        {
            get
            {
                return m_ItemsName;
            }
            set
            {
                m_ItemsName = value;
                // If no itemname, no spawning 
                if (m_ItemsName.Count < 1)
                    Stop();

                InvalidateProperties();
            }
        }

        private Mobile m_LastProps; // erl: added to hold who last opened props on it
        public Mobile LastProps
        {
            get
            {
                return m_LastProps;
            }
            set
            {
                if (value is PlayerMobile)
                    m_LastProps = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ChanceToSpawn
        {
            get { return m_ChanceToSpawn; }
            set
            {
                if (m_ChanceToSpawn != value)
                    LogChange("ChanceToSpawn changed, " + m_ChanceToSpawn + " to " + value);
                m_ChanceToSpawn = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count
        {
            get
            {
                return m_Count;
            }
            set
            {
                if (m_Count != value)
                {
                    // erl: Log the change to Count
                    LogChange("Count changed, " + m_Count + " to " + value);
                }
                m_Count = value;
                InvalidateProperties();
            }
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

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool RandomItem
        {
            get
            {
                return m_RandomItem;
            }
            set
            {
                bool respawn = m_RandomItem != value;
                m_RandomItem = value;
                if (respawn == true)
                    Respawn();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MinDelay
        {
            get
            {
                return m_MinDelay;
            }
            set
            {
                m_MinDelay = value;
                InvalidateProperties();
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MaxDelay
        {
            get
            {
                return m_MaxDelay;
            }
            set
            {
                m_MaxDelay = value;
                InvalidateProperties();
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (m_Running)
                    return m_End - DateTime.UtcNow;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                Start();
                DoTimer(value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Container SpawnContainer
        {
            get
            {
                return m_Container;
            }
            set
            {
                m_Container = value;
            }
        }

        [Constructable]
        public ChestItemSpawner(int amount, int minDelay, int maxDelay, string itemName)
            : base(0x1f13)
        {
            ArrayList itemsName = new ArrayList();
            itemsName.Add(itemName.ToLower());
            InitSpawn(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay), itemsName);
        }


        [Constructable]
        public ChestItemSpawner(string itemName)
            : base(0x1f13)
        {
            ArrayList itemsName = new ArrayList();
            itemsName.Add(itemName.ToLower());
            InitSpawn(1, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(60), itemsName);
        }


        [Constructable]
        public ChestItemSpawner()
            : base(0x1f13)
        {
            ArrayList itemsName = new ArrayList();
            InitSpawn(1, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(60), itemsName);
        }


        public ChestItemSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, ArrayList itemsName)
            : base(0x1f13)
        {
            InitSpawn(amount, minDelay, maxDelay, itemsName);
        }


        public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, ArrayList itemsName)
        {
            Visible = false;
            Movable = true;
            m_Running = true;
            Name = "ChestItemSpawner";
            m_MinDelay = minDelay;
            m_MaxDelay = maxDelay;
            m_Count = amount;
            m_ItemsName = itemsName;
            m_Items = new ArrayList();              //create new list of items
            DoTimer(TimeSpan.FromSeconds(1));       //spawn in 1 sec 
        }


        public ChestItemSpawner(Serial serial)
            : base(serial)
        {
        }


        public override void OnDoubleClick(Mobile from)
        {
            ChestItemSpawnerGump g = new ChestItemSpawnerGump(this);
            from.SendGump(g);
        }


        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Running)
            {
                list.Add(1060742); // active 

                list.Add(1060656, m_Count.ToString()); // amount to make: ~1_val~ 
                list.Add(1060660, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_val~: ~2_val~ 
            }
            else
            {
                list.Add(1060743); // inactive 
            }
        }


        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Running)
                LabelTo(from, "[Running]");
            else
                LabelTo(from, "[Off]");
        }


        public void Start()
        {
            if (!m_Running)
            {
                if (m_ItemsName.Count > 0)
                {
                    m_Running = true;
                    DoTimer();
                }
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


        public void Defrag()
        {
            bool removed = false;

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (o is Item)
                {

                    Item item = (Item)o;

                    //if not in the original container or deleted -> delete from list 
                    if (item.Deleted)
                    {
                        m_Items.RemoveAt(i);
                        --i;
                        removed = true;
                    }
                    else
                    {

                        if (item.Parent is Container)
                        {
                            Container par = (Container)item.Parent;

                            if (this.m_Container != null)
                            {
                                Container cont = (Container)this.m_Container;
                                if (((Item)cont).Serial != ((Item)par).Serial)
                                {
                                    m_Items.RemoveAt(i);
                                    --i;
                                    removed = true;
                                }
                            }
                            else
                            {
                                m_Items.RemoveAt(i);
                                --i;
                                removed = true;

                            }
                        }
                        else
                        {
                            m_Items.RemoveAt(i);
                            --i;
                            removed = true;

                        }

                    }
                }
                else
                {
                    //should not be something else 
                    m_Items.RemoveAt(i);
                    --i;
                    removed = true;
                }
            }

            if (removed)
                InvalidateProperties();
        }


        public void OnTick()
        {
            DoTimer();
            Spawn();
        }

        public void Respawn()
        {
            try
            {
                RemoveItems();
                for (int i = 0; i < m_Count; i++)
                    Spawn();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }


        public void Spawn()
        {
            if (m_RandomItem == false)
            {
                // spawn one of each item type
                for (int i = 0; i < m_ItemsName.Count; i++)
                {
                    Spawn(i);
                }
            }
            else
            {
                //if there are item to spawn in list 
                if (m_ItemsName.Count > 0)
                    Spawn(Utility.Random(m_ItemsName.Count)); //spawn on of them index 
            }
        }


        public void Spawn(string itemName)
        {
            for (int i = 0; i < m_ItemsName.Count; i++)
            {
                if ((string)m_ItemsName[i] == itemName)
                {
                    Spawn(i);
                    break;
                }
            }
        }


        public void Spawn(int index)
        {

            if (m_ItemsName.Count == 0 || index >= m_ItemsName.Count || m_Container == null)
                return;

            Defrag();

            // random means spawn N random items from the list
            // Note RandomItem is ignored when the spawner type is ChestLootPackSpawner
            if (m_RandomItem == false && this is ChestLootPackSpawner == false)
            {   // See if we have reached the limit at for this type of item
                if (CountItems((string)m_ItemsName[index]) >= m_Count)
                    return;
            }
            // otherwise spawn N of everything in the list
            else
            {   // See if we have reached the limit at for this type of item
                if (m_Items.Count >= m_Count)
                    return;
            }

            if (m_ChanceToSpawn >= Utility.RandomDouble())
            {
                Type type = SpawnerType.GetType((string)m_ItemsName[index]);

                if (type != null)
                {
                    try
                    {
                        object o = Activator.CreateInstance(type);

                        if (o is Item)
                        {
                            if (m_Container != null)
                            {
                                Item item = (Item)o;
                                m_Items.Add(item);              //add it to the list 
                                InvalidateProperties();
                                Container cont = m_Container;   //spawn it in the container 
                                cont.DropItem(item);
                            }
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }

                Item lootPack = ChestItemSpawnerType.GetLootPack((string)m_ItemsName[index]);

                if (lootPack != null)
                {
                    try
                    {
                        ArrayList o = CreateItem(lootPack);

                        if (o != null && o.Count > 0)
                        {
                            if (m_Container != null)
                            {
                                for (int ix = 0; ix < o.Count; ix++)
                                {
                                    Item item = o[ix] as Item;
                                    m_Items.Add(item);              //add it to the list 
                                    InvalidateProperties();
                                    Container cont = m_Container;   //spawn it in the container 
                                    cont.DropItem(item);
                                }
                            }
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        // spawners can now spawn special lootpacks with per item drop rates.
        public ArrayList CreateItem(Item lootPack)
        {
            LogHelper Logger = null;
            ArrayList items = new ArrayList();
            try
            {
                if (lootPack == null || lootPack.Deleted == true)
                    return null;

                Logger = new LogHelper("LootPack.log", false, true);

                if (lootPack is Container)
                {
                    if ((lootPack as Container).Factory)
                    {   // only one item from factory has a chance at a drop
                        if ((lootPack as Container).DropRate >= Utility.RandomDouble())
                        {
                            if ((lootPack as Container).Items.Count > 0)
                            {
                                Item item = (lootPack as Container).Items[Utility.Random((lootPack as Container).Items.Count)] as Item;
                                Item temp = RareFactory.DupeItem(item);
                                if (temp != null)
                                {
                                    Logger.Log(LogType.Item, this, string.Format("Dropped lootpack item {0}", temp));
                                    temp.DropRate = 1.0;    // should not be set, but lets be safe
                                    items.Add(temp);
                                    return items;
                                }
                            }
                        }
                    }
                    else
                    {
                        // each item from a container has a chance at a drop
                        foreach (Item item in lootPack.Items)
                        {   // drop chance
                            if (item.DropRate >= Utility.RandomDouble())
                            {
                                Item temp = RareFactory.DupeItem(item);
                                if (temp != null)
                                {
                                    Logger.Log(LogType.Item, this, string.Format("Dropped lootpack item {0}", temp));
                                    temp.DropRate = 1.0;    // all this does is save the sizeof(double) for each item generated
                                    items.Add(temp);
                                }
                            }
                        }

                        return items;
                    }
                }
                else
                {
                    // drop chance
                    if (lootPack.DropRate >= Utility.RandomDouble())
                    {
                        Item temp = RareFactory.DupeItem(RareFactory.DupeItem(lootPack));
                        if (temp != null)
                        {
                            Logger.Log(LogType.Item, this, string.Format("Dropped lootpack item {0}", temp));
                            temp.DropRate = 1.0;    // all this does is save the sizeof(double) for each item generated
                            items.Add(temp);
                            return items;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
            finally
            {
                if (Logger != null)
                    Logger.Finish();
            }

            return items;
        }

        public void DoTimer()
        {
            if (!m_Running)
                return;

            int minSeconds = (int)m_MinDelay.TotalSeconds;
            int maxSeconds = (int)m_MaxDelay.TotalSeconds;

            TimeSpan delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }


        public void DoTimer(TimeSpan delay)
        {
            if (!m_Running)
                return;

            m_End = DateTime.UtcNow + delay;

            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }


        private class InternalTimer : Timer
        {
            private ChestItemSpawner m_Spawner;

            public InternalTimer(ChestItemSpawner spawner, TimeSpan delay)
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
        }


        public int CountItems(string itemName)
        {

            Defrag();

            int count = 0;

            for (int i = 0; i < m_Items.Count; ++i)
                if (Insensitive.Equals(itemName, m_Items[i].GetType().Name))
                    ++count;

            return count;
        }


        public void RemoveItems(string itemName)
        {
            //Console.WriteLine( "defrag from removeitems" ); 
            Defrag();

            itemName = itemName.ToLower();

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (Insensitive.Equals(itemName, o.GetType().Name))
                {
                    if (o is Item)
                        ((Item)o).Delete();

                }
            }

            InvalidateProperties();
        }

        public void RemoveItems()
        {

            Defrag();

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (o is Item)
                    ((Item)o).Delete();

            }

            InvalidateProperties();
        }

        // change logging!
        public void LogChange(string changemade)
        {
            if (changemade == "")
                return;

            string strAcc = "";
            if (m_LastProps is PlayerMobile)
                strAcc = m_LastProps.Account.ToString();
            else
                strAcc = "SYSTEM";

            LogHelper Logger = new LogHelper("spawnerchange.log", false, true);
            Logger.Log(LogType.Item, this, string.Format("{0}, made the following changes: {1}", strAcc, changemade));
            Logger.Finish();
        }

        public override void OnDelete()
        {
            // erl: Log the fact it's been deleted
            LogChange("Spawner deleted");

            base.OnDelete();
            try
            {
                RemoveItems();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            if (m_Timer != null)
                m_Timer.Stop();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version 

            // version 2
            writer.Write(m_ChanceToSpawn);

            // version 1
            writer.Write(m_RandomItem);

            // version 0
            writer.Write(m_Container);
            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            writer.Write(m_Count);
            writer.Write(m_Running);

            if (m_Running)
                writer.Write(m_End - DateTime.UtcNow);

            writer.Write(m_ItemsName.Count);

            for (int i = 0; i < m_ItemsName.Count; ++i)
                writer.Write((string)m_ItemsName[i]);

            writer.Write(m_Items.Count);

            for (int i = 0; i < m_Items.Count; ++i)
            {
                object o = m_Items[i];

                if (o is Item)
                    writer.Write((Item)o);
                else
                    writer.Write(Serial.MinusOne);
            }
        }

        private static WarnTimer m_WarnTimer;

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_ChanceToSpawn = reader.ReadDouble();
                        goto case 1;
                    }
                    break;

                case 1:
                    {
                        m_RandomItem = reader.ReadBool();
                        goto case 0;
                    }
                    break;

                case 0:
                    {
                        m_Container = reader.ReadItem() as Container;
                        m_MinDelay = reader.ReadTimeSpan();
                        m_MaxDelay = reader.ReadTimeSpan();
                        m_Count = reader.ReadInt();
                        m_Running = reader.ReadBool();

                        if (m_Running)
                        {
                            TimeSpan delay = reader.ReadTimeSpan();
                            DoTimer(delay);
                        }

                        int size = reader.ReadInt();

                        m_ItemsName = new ArrayList(size);

                        for (int i = 0; i < size; ++i)
                        {
                            string typeName = reader.ReadString();

                            m_ItemsName.Add(typeName);

                            if (ChestItemSpawnerType.GetType(typeName) == null && ChestItemSpawnerType.GetLootPack(typeName) == null)
                            {
                                if (m_WarnTimer == null)
                                    m_WarnTimer = new WarnTimer();

                                m_WarnTimer.Add(Location, Map, typeName);
                            }
                        }

                        int count = reader.ReadInt();

                        m_Items = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            IEntity e = World.FindEntity(reader.ReadInt());

                            if (e != null)
                                m_Items.Add(e);
                        }
                    }
                    break;
            }
        }

        private class WarnTimer : Timer
        {
            private ArrayList m_List;

            private class WarnEntry
            {
                public Point3D m_Point;
                public Map m_Map;
                public string m_Name;

                public WarnEntry(Point3D p, Map map, string name)
                {
                    m_Point = p;
                    m_Map = map;
                    m_Name = name;
                }
            }

            public WarnTimer()
                : base(TimeSpan.FromSeconds(1.0))
            {
                m_List = new ArrayList();
                Start();
            }

            public void Add(Point3D p, Map map, string name)
            {
                m_List.Add(new WarnEntry(p, map, name));
            }

            protected override void OnTick()
            {
                try
                {
                    Console.WriteLine("Warning(ChestItemspawner.cs:WarnTimer): {0} bad spawns detected, logged: 'badspawn.log'", m_List.Count);

                    LogHelper Logger = new LogHelper("badspawn.log", false, true);
                    if (Logger != null)
                    {// 3/23/2010 4.13.56 PM:1520	1755	22	Felucca	0x4000001B
                        foreach (WarnEntry e in m_List)
                            Logger.Log(LogType.Text, string.Format(" X:{0}, Y:{1}, Z:{2}, Map:{3}, Type Name:{4} (if it looks like a serial it's probably a loot pack)", e.m_Point.X, e.m_Point.Y, e.m_Point.Z, e.m_Map, e.m_Name));
                        Logger.Finish();
                    }

                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }
        }
    }
}
