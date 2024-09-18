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

/* Scripts/Engines/ChampionSpawn/ChampEngine.cs
 * ChangeLog:
 *	8/12/10, Adam
 *		Replace GetSpawnLocation() implementation with a shared version if Spawner.cs
 *	6/23/10, Adam
 *		return LongTermMurders to Kills .. got changed in a global search and replace
 *	04/27/09, plasma
 *		Virtualised GetSpawnLocation, made IsNearPlayer protected.
 *	04/07/09, plasma
 *		Made PrepMob protected/virtual. Added RangeScale.
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *	07/27/2007, plasma
 *		- Commented out mobile factory property as now replaced with gump
 *		- Launch new mobile factory gump on doubleclick if Seer+
 *  4/2/07, Adam
 *      Add DebugDump() function to dump state when we get one of the following exceptions:
 *      Value cannot be null.
 *      Parameter name: type
 *      at System.Activator.CreateInstance(Type type, Boolean nonPublic)
 *      at Server.Engines.ChampionSpawn.ChampEngine.Spawn(Type[] types)
 *  3/17/07, Adam
 *      Add MobileFactory system. 
 *          - Allow the adding of MobileFactory (spawners) instead of raw creatures.
 *            This allows us to spawn custom creatures
 *          - Add a lvl_Error to the command properties so we can see if we are missing a factory
 *          - 
 *  02/11/2007, plasma
 *      Changed serialise to check for valid graphics, and if not to 
 *      reverse the graphics bool.
 *  01/13/2007, plasma
 *      Added NextSpawn property and ensured on deserialisation that the
 *      Delay is carried over rather than restarted.
 *  12/28/2006, plasma
 *      Virtualised RestartDelay property for ChampAngelIsland
 *	11/07/2006, plasma
 *		Fixed arraylist bug
 *	11/06/2006, plasma
 *		Serial cleanup
 *	11/01/2006, plasma
 *		Added ebb and flow system for smooth level transition
 *		Fixed null ref bug with free list
 *	10/29/2006, plasma
 *		Added WipeMonsters() into AdvanceLevel()
 *	10/29/2006, plasma
 *		Removed line that was causing navdest to not work
 *		Added serialisation for navdest :)				
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Engines.ChampionSpawn
{
    // Plasma: Core champion spawn class.  
    public abstract class ChampEngine : Item
    {
        // Members
        #region members

        private ChampSliceTimer m_Slice;                                // Slice timer  
        private ChampRestartTimer m_Restart;                        // Restart timer	
        private DateTime m_End;                                                 // Holds next respawn time
        protected ChampLevelData.SpawnTypes m_Type;         // Which spawn type this is ( cold blood etc )
        protected ArrayList m_Monsters;                                 // Mobile container
        protected ArrayList m_FreeMonsters;                         // Mobile container for old spawn
        protected int m_LevelCounter;                                       // Current level
        protected DateTime m_ExpireTime;                                // Level down datetime
        protected DateTime m_SpawnTime;                             // Respawn datetime
        protected TimeSpan m_RestartDelay;                          // Restart delay
        protected bool m_bGraphics;                                         // Altar & Skulls on/off
        protected bool m_bRestart;                                          // Restart timer on/off
        protected bool m_bActive;                                               // Champ running..?
        protected ChampGraphics m_Graphics;                         // Graphics object
        protected int m_Kills;                                                  // Kill counter
        protected NavDestinations m_NavDest;                        // Allow mobs to navigate!		
        protected double m_LevelScale;                                  // Virtually scales the maxmobs and maxrange
        public ArrayList SpawnLevels;                                       // Contains all the level data objects

        public enum LevelErrors
        {
            None,               // no error on this level
            No_Factory,         // no factory to spawn mob
            No_Location         // can't find a good spawn location
        };
        private LevelErrors m_LevelError = LevelErrors.None;

        #endregion

        #region command properties

        /*
    //hold object for templated spawner use.
    Item m_MobileFactory = null;
    [CommandProperty(AccessLevel.Seer)]
    public Item MobileFactory
    {
        get 
        {   // the current factory item
            foreach (Item ix in Items)
            {
                if (ix is Spawner)
                {   // just get the first spawner
                    m_MobileFactory = ix;
                    break;
                }
            }
            return m_MobileFactory; 
        }
        set 
        {
            if (value == null)
            {   // kill the current factory
                if (m_MobileFactory != null)
                    if (Items.Contains(m_MobileFactory) == true)
                    {
                        this.RemoveItem(m_MobileFactory);
                        m_MobileFactory.Delete();
                    }
            }
            else if (value is Spawner)
            {   // add a new factory spawner
                if (Items.Contains(value) == false)
                {   // make sure it's off
                    (value as Spawner).Running = false;
                    this.AddItem(value);
                }
            }
            else
                // do nothing 
                value = m_MobileFactory;

            m_MobileFactory = value; 
        }
    }
		*/

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampLevelData.SpawnTypes SpawnType                  // SpawnType.  Changing this will also restart the champ if active.
        {
            get { return (m_Type); }
            set
            {
                m_Type = value;
                StopSlice();                // stop slice timer and create new spawn type
                SpawnLevels = ChampLevelData.CreateSpawn(m_Type);
                // begin
                if (m_bActive)
                    StartSpawn();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Graphics
        {
            get { return m_bGraphics; }
            set
            {
                if (m_bGraphics == value)
                    return;

                if (value)
                {
                    // Switch gfx on
                    m_bGraphics = true;
                    m_Graphics = new ChampGraphics(this);
                    m_Graphics.UpdateLocation();
                }
                else
                {
                    // switch em off!. and delete. and stuff.
                    if (m_Graphics != null)
                        m_Graphics.Delete();

                    m_bGraphics = false;
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_bActive; }
            set
            {
                if (value == m_bActive)
                    return;

                // set active bool and call overridable activate code
                m_bActive = value;
                Activate();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestartTimer
        {
            get { return m_bRestart; }
            set { m_bRestart = value; }
        }

        //pla 12/28/06:
        //virtualised for ChampAngelIsland
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan RestartDelay
        {
            get { return m_RestartDelay; }
            set
            {
                m_RestartDelay = value;
                if (m_Restart != null)
                    if (m_Restart.Running)
                        DoTimer(value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get { return m_LevelCounter; }
            set
            {
                if (value < SpawnLevels.Count)
                {
                    // set new level....
                    // wipe monsters first !
                    WipeMonsters();
                    m_Kills = 0;
                    m_LevelCounter = value;
                    // reset level down time
                    m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills
        {
            // squirrels
            get { return m_Kills; }
            set
            {
                if (value <= Lvl_MaxKills && value >= 0)
                    m_Kills = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (!m_bActive && m_Restart != null)
                    return m_End - DateTime.UtcNow;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                if (m_bActive)
                    DoTimer(value);
            }
        }


        /// <summary>
        /// Gets or sets the range scale.
        /// </summary>
        /// <value>The range scale.</value>
        [CommandProperty(AccessLevel.GameMaster)]
        public double LevelScale
        {
            get { return m_LevelScale; }
            set { m_LevelScale = value; }
        }

        ////
        //Current level properties

        [CommandProperty(AccessLevel.GameMaster)]
        public LevelErrors Lvl_LevelError           // level error - Any errors while spawning creatures?
        {
            get { return m_LevelError; }
            set { m_LevelError = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxKills         // Max Kills - How many kills are needed to level up
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxKills + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxKills * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxKills = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Lvl_FactoryMobs         // Factory Mobs - Are these mobiles to be factory created?
        {
            get { return (((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags & SpawnFlags.FactoryMobile) != 0; }
            set
            {

                if (value)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags |= SpawnFlags.FactoryMobile;
                else
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags &= ~SpawnFlags.FactoryMobile;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxRange         // Max Range : Max distance mobs will spawn 
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxRange + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxRange * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxRange = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxSpawn     // Max Spawn : amount of mobs that will span in one respawn()
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxSpawn + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxSpawn * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxSpawn = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxMobs          // Max Mobs : Amount of mobs allowed onscreen at once
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxMobs + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxMobs * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxMobs = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Lvl_SpawnDelay      // SpawnDelay :  Delay inbetween respawn()
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_SpawnDelay; }
            set
            {
                ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_SpawnDelay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Lvl_ExpireDelay     // ExpireDelay :  Delay before level down ...
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_ExpireDelay; }
            set { ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_ExpireDelay = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Lvl_Monsters      // Monster array!  have this as a comma seperated list
        {
            get
            {
                string temp = "";
                foreach (string s in ((ChampLevelData)SpawnLevels[m_LevelCounter]).Monsters)
                    temp = temp + s + ",";

                temp = temp.Substring(0, temp.Length - 1);
                return temp;
            }
            set
            {
                // create new props!  (if this goes wrong exceptions are caught)
                string[] temp = value.Split(',');
                ((ChampLevelData)SpawnLevels[m_LevelCounter]).Monsters = new string[temp.GetLength(0)];
                for (int i = 0; i < temp.GetLength(0); ++i)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).Monsters[i] = temp[i].Trim();
            }

        }

        // non command props		
        public NavDestinations NavPoint
        {
            get { return m_NavDest; }
            set { m_NavDest = value; }
        }

        // 
        public bool IsFinalLevel
        {
            // useful!
            get { return (m_LevelCounter == SpawnLevels.Count - 1 ? true : false); }
        }

        #endregion

        //Constructors
        #region constructors


        public ChampEngine()
            : base(0xBD2)
        {
            // default constructor
            // assgin initial values..
            Visible = false;
            Movable = false;
            m_ExpireTime = DateTime.UtcNow;
            m_SpawnTime = DateTime.UtcNow;
            m_RestartDelay = TimeSpan.FromMinutes(5);
            m_Monsters = new ArrayList();
            m_FreeMonsters = new ArrayList();
            SpawnLevels = new ArrayList();
            m_bRestart = false;

            //load default spawn just so there's no nulls on the [props
            SpawnLevels = ChampLevelData.CreateSpawn(ChampLevelData.SpawnTypes.Abyss);
        }

        public ChampEngine(Serial serial)
            : base(serial)
        {
        }

        #endregion

        //Serialize
        #region serialize

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3);           // version

            // version 3
            writer.Write((double)m_LevelScale);  // errors for this level

            // version 2
            writer.Write((int)m_LevelError);  // errors for this level

            // version 1
            writer.WriteDeltaTime(m_End);       //Next spawn time

            // first up is the spawn data       // version 0
            writer.Write((int)m_Type);
            writer.Write((int)SpawnLevels.Count);
            foreach (ChampLevelData data in SpawnLevels)
                data.Serialize(writer);

            //now the monsters + misc data
            writer.WriteMobileList(m_Monsters);
            writer.WriteMobileList(m_FreeMonsters);
            writer.Write((int)m_LevelCounter);
            writer.Write((int)m_Kills);
            writer.Write((DateTime)m_ExpireTime);
            //  nav dest
            writer.Write((int)m_NavDest);

            // the bools
            writer.Write((bool)m_bActive);

            //pla: 02/11/07
            //Check here incase the altar or platform has been deleted.
            //in which case we just set the grpahics to false and they can be
            //switched back on on reload.  Prevents the champ from crashing on load
            //if the gfx were accidentally deleted through [delete
            /////////////////////////////////////////////////////////////////////
            if (m_bGraphics && !m_Graphics.IsHealthy())
            {
                //call delete code as these save on world save anyway
                m_Graphics.Delete();
                m_bGraphics = false;
            }

            writer.Write((bool)m_bGraphics);

            // NavDest

            // the altar and platform
            if (m_bGraphics)
            {
                m_Graphics.Serialize(writer);
            }
            writer.Write((bool)m_bRestart);
            writer.Write(m_RestartDelay);
            // And finally if the restart timer is currently on or not, and the delay value.
            writer.Write(m_Restart != null);

        }

        public override void Deserialize(GenericReader reader)
        {
            TimeSpan ts = TimeSpan.Zero;

            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_LevelScale = reader.ReadDouble();
                        goto case 2;
                    }
                case 2:
                    {
                        m_LevelError = (LevelErrors)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        ts = reader.ReadDeltaTime() - DateTime.UtcNow;
                        goto case 0;
                    }
                case 0:
                    {

                        // read it all back in									
                        m_Type = ((ChampLevelData.SpawnTypes)reader.ReadInt());

                        int a = reader.ReadInt();
                        SpawnLevels = new ArrayList();

                        // create new level array through deserialise constructors
                        for (int i = 0; i < a; ++i)
                            SpawnLevels.Add(new ChampLevelData(reader));

                        m_Monsters = reader.ReadMobileList();
                        m_FreeMonsters = reader.ReadMobileList();
                        m_LevelCounter = reader.ReadInt();
                        m_Kills = reader.ReadInt();
                        m_ExpireTime = reader.ReadDateTime();
                        m_NavDest = (NavDestinations)reader.ReadInt();

                        // the bools
                        m_bActive = reader.ReadBool();
                        m_bGraphics = reader.ReadBool();

                        // if graphics were on remake them thru deserialise constructor
                        if (m_bGraphics)
                            m_Graphics = new ChampGraphics(this, reader);

                        // and the restart...
                        m_bRestart = reader.ReadBool();
                        m_RestartDelay = reader.ReadTimeSpan();

                        if (reader.ReadBool() && !m_bActive && m_bRestart)
                        {
                            // in this case the champ is activley in restart mode, so create new timer
                            //pla: 13/01/07
                            //changed so we don't lose time on restart
                            if (ts == TimeSpan.Zero)
                                DoTimer(m_RestartDelay);
                            else
                                DoTimer(ts);
                        }
                        else if (m_bActive)
                        {
                            // if spawn was active then start the wheels turning...
                            StartSlice();
                        }

                        break;
                    }

            }

        }

        #endregion

        //Methods
        #region methods

        // Big switch! This has the effect of restarting a spawn too.
        protected virtual void StartSpawn()
        {
            m_bActive = true;
            m_Kills = 0;
            m_LevelCounter = 0;
            WipeMonsters();
            StartSlice();
        }

        // Slice timer on / off		
        protected void StartSlice()
        {
            if (Deleted)
                return;

            // if there's a restart timer on, kill it.
            if (m_Restart != null)
            {
                m_Restart.Stop();
                m_Restart = null;
            }

            if (m_Slice != null)
                m_Slice.Stop();

            m_Slice = new ChampSliceTimer(this);
            m_Slice.Start();

            // reset level expire delay
            m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
        }

        protected void StopSlice()
        {
            if (Deleted)
                return;

            if (m_Slice != null)
            {
                m_Slice.Stop();
                m_Slice = null;
            }
        }

        // this is called from the Active prop 
        protected virtual void Activate()
        {
            // for base champ we just want to start spawn if active
            if (m_bActive)
                StartSpawn();
        }

        // This needs to be public so the restart timer can call it!
        public virtual void Restart()
        {
            if (Deleted)
                return;

            m_Restart = null;
            StartSpawn();
        }

        // Core stuff, slice, advancelevel, respawn, expire

        //OnSlice needs to be public so the slice timer can access it
        public virtual void OnSlice()
        {
            ArrayList ClearList = new ArrayList();
            // this is the champ heartbeat. 
            if (!m_bActive || Deleted)
                return;

            // couple of null checks just in case!
            if (m_Monsters == null)
                m_Monsters = new ArrayList();

            if (m_FreeMonsters == null)
                m_FreeMonsters = new ArrayList();

            if (m_LevelCounter <= SpawnLevels.Count)
            {
                // Now clear out any dead mobs from the mob list
                for (int i = 0; i < m_Monsters.Count; ++i)
                {
                    if (((Mobile)m_Monsters[i]).Deleted)
                    {
                        // increase kills !
                        ++m_Kills;
                        //add to clear list!
                        ClearList.Add(m_Monsters[i]);
                    }
                }
                // Now remove those guys from the original list
                for (int i = 0; i < ClearList.Count; i++)
                    m_Monsters.Remove(ClearList[i]);

                ClearList.Clear();

                // Now clear out any dead mobs from the free list, don't add these to the kill count though
                for (int i = 0; i < m_FreeMonsters.Count; ++i)
                    if (((Mobile)m_FreeMonsters[i]).Deleted)
                        ClearList.Add(m_FreeMonsters[i]);

                // Now remove those guys from the original list
                for (int i = 0; i < ClearList.Count; i++)
                    m_FreeMonsters.Remove(ClearList[i]);

                //calculate percentage killed against max for this level
                double n = m_Kills / (double)Lvl_MaxKills;
                int percentage = (int)(n * 100);

                // level up if > 90%
                if (percentage > 90)
                {
                    AdvanceLevel();
                }
                else
                {
                    // level down if the time's up!
                    if (DateTime.UtcNow >= m_ExpireTime)
                        Expire();

                    // Call spawn top-up function
                    Respawn();
                }

                // Update altar/skulls if they're on
                if (m_bGraphics)
                    if (m_Graphics != null)
                        m_Graphics.Update();

            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ClearMonsters
        {
            get { return false; }
            set { if (value) WipeMonsters(); }
        }

        public void WipeMonsters()
        {
            // Delete all mosters, clear arraylists..
            if (m_Monsters != null)
            {
                foreach (Mobile m in m_Monsters)
                    m.Delete();

                m_Monsters.Clear();
            }

            if (m_FreeMonsters != null)
            {
                foreach (Mobile m in m_FreeMonsters)
                    m.Delete();

                m_FreeMonsters.Clear();
            }
        }

        protected bool CompareLevel(int offset)
        {

            try //just in case!
            {
                // create compare strings
                string current = "";
                foreach (string s in ((ChampLevelData)SpawnLevels[m_LevelCounter]).Monsters)
                    current = current + s;

                string target = "";
                foreach (string s in ((ChampLevelData)SpawnLevels[m_LevelCounter + offset]).Monsters)
                    target = target + s;

                //return true if the strings match
                if (current == target)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;  // must have been out of range
            }
        }

        protected virtual void AdvanceLevel()
        {
            //reset kills etc and level up
            m_Kills = 0;
            if (!IsFinalLevel)
            {
                // Check if the next level is the same as the current level's monster array				
                if (CompareLevel(1) == false)
                {
                    // anything left in the mob list gets moved into the free list
                    //first we let anything in the free list go
                    m_FreeMonsters.Clear();
                    // shift all mobs into the free list
                    foreach (Mobile m in m_Monsters)
                    {
                        if (m_FreeMonsters.Count < ((ChampLevelData)SpawnLevels[m_LevelCounter + 1]).m_MaxMobs)
                            m_FreeMonsters.Add(m);
                        else
                            break;
                    }
                    // Clear original list if anything's left
                    m_Monsters.Clear();

                }
                ++m_LevelCounter;
                //reset expire time
                m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
                m_SpawnTime = DateTime.UtcNow;
            }
            else
            {
                //Last level (champ) is over
                StopSlice();
                m_LevelCounter = 0;
                m_bActive = false;

                // update altar
                if (m_bGraphics)
                    m_Graphics.Update();

                // Start restart timer !
                if (m_bRestart)
                {
                    //pla: 13/01/07.
                    //changed to use DoTimer
                    DoTimer(m_RestartDelay);
                }

            }
        }

        protected virtual void Respawn()
        {
            if (!m_bActive || Deleted)
                return;

            if (DateTime.UtcNow < m_SpawnTime)
                return;

            // Check to see if we can spawn more monsters				
            int amount_spawned = 0;

            while (m_Monsters.Count + m_FreeMonsters.Count < Lvl_MaxMobs && amount_spawned < Lvl_MaxSpawn)
            {
                Mobile m = Spawn();

                if (m == null)
                    return;

                // Increase vars and place into the big wide world!  (old code!)
                ++amount_spawned;
                m_Monsters.Add(m);
                m.MoveToWorld(GetSpawnLocation(m), Map);
                PrepMob(m);
            }
            m_SpawnTime = DateTime.UtcNow + Lvl_SpawnDelay;

            // if free list has monsters in it, we convert them one a second 
            // preferably away from the players
            Mobile victim = null;
            bool random = false;
            if (m_FreeMonsters.Count > 0)
            {
                // try and find a mob that can't be seen by a player
                for (int i = 0; i < m_FreeMonsters.Count; ++i)
                {
                    Mobile m = (Mobile)m_FreeMonsters[i];
                    random = false;
                    IPooledEnumerable eable = m.GetMobilesInRange(15);
                    foreach (Mobile t in eable)
                    {
                        if (t is PlayerMobile)
                        {
                            // found a player. no good!.	
                            random = true;
                            break;
                        }
                    }
                    eable.Free();
                    if (!random)
                    {
                        // this mob will do!
                        victim = m;
                        m_FreeMonsters.RemoveAt(i);
                        break;
                    }
                }

                // if we couldn't find one out of sight, pick one at random
                if (random)
                {
                    Random r = new Random();
                    int i = r.Next(m_FreeMonsters.Count);
                    victim = (Mobile)m_FreeMonsters[i];
                    m_FreeMonsters.RemoveAt(i);
                }

                Mobile n = Spawn();
                if (n == null)
                    return;

                m_Monsters.Add(n);

                // we cannot reuse the location of a victem if they have different water/land domains
                bool sameType = (n.CanSwim == victim.CanSwim && n.CantWalk == victim.CantWalk);

                // perform rangecheck to see if we can just replace this mob with one from new level
                if (!random && sameType && victim.GetDistanceToSqrt(Location) <= Lvl_MaxRange)
                    // they are within spawn range, so replace with new mob in same location													
                    n.MoveToWorld(victim.Location, Map);
                else    // spawn somewhere randomly
                    n.MoveToWorld(GetSpawnLocation(n), Map);

                //delete old mob
                victim.Delete();

                // setup new mob
                PrepMob(n);
            }
        }

        protected virtual void Expire()
        {
            // Level down time - you just can't get the players these days !
            if (m_LevelCounter < SpawnLevels.Count)
            {
                double f = ((double)Kills) / ((double)Lvl_MaxKills);
                if (f * 100 < 20)
                {
                    // They didn't even get 20% !!, go back a level.....
                    if (Level > 0)
                    {
                        // if previous level is the same as the current, just decrease level counter
                        if (CompareLevel(-1) == true)
                            --m_LevelCounter;
                        else    //otherwise wipe mobs when leveling down
                            --Level;
                    }
                    Kills = 0;
                    InvalidateProperties();
                }
                else
                {
                    Kills = 0;
                }
                m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
            }
        }

        protected virtual void PrepMob(Mobile m)
        {
            BaseCreature bc = m as BaseCreature;
            if (bc != null)
            {
                bc.Tamable = false;
                bc.Home = m_NavDest != NavDestinations.None ? new Point3D() : Location;
                bc.RangeHome = Lvl_MaxRange;
                bc.NavDestination = m_NavDest;

                //if we have a navdestination as soon as we spawn start on it
                if (bc.NavDestination != NavDestinations.None)
                    bc.AIObject.Think();

            }
        }


        //  pla: 13/01/2007
        /// <summary>
        /// stops and restarts the restart timer with a new delay
        /// </summary>
        /// <param name="delay"></param>
        public void DoTimer(TimeSpan delay)
        {
            if (m_bActive)  //cant have a restart if the champ is on
                return;

            m_End = DateTime.UtcNow + delay;

            if (m_Restart != null)
                m_Restart.Stop();

            m_Restart = new ChampRestartTimer(this, delay);
            m_Restart.Start();
        }

        protected Mobile Factory(Type type)
        {
            Mobile mx = null;
            // look for a factory that knows how to spawn one of these
            foreach (Item ix in Items)
            {
                if (ix is Spawner)
                    if ((mx = (ix as Spawner).CreateRaw(type.Name) as Mobile) != null)
                        break;
            }

            // Experimental, property-based, error reporting.
            //  cannot find a factory to manufacture this creature
            if (mx == null) Lvl_LevelError = LevelErrors.No_Factory;

            return mx;
        }

        private void DebugDump()
        {
            LogHelper logger = new LogHelper("champSpawner.log", false);
            try
            {
                logger.Log(LogType.Text, String.Format("this = {0}", this));
                logger.Log(LogType.Text, String.Format("m_LevelCounter = {0}", m_LevelCounter));
                logger.Log(LogType.Text, String.Format("SpawnLevels.Count = {0}", SpawnLevels.Count));
                logger.Log(LogType.Text, String.Format("((ChampLevelData)SpawnLevels [m_LevelCounter]).Monsters.Length = {0}", ((ChampLevelData)SpawnLevels[m_LevelCounter]).Monsters.Length));
                //logger.Log(LogType.Text, "X = {0}", X);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            finally
            {
                logger.Finish();
            }
        }

        // Mob spawn functions - Extracted from old code
        protected Mobile Spawn()
        {
            return Spawn(((ChampLevelData)SpawnLevels[m_LevelCounter]).GetRandomType());
        }
        protected Mobile Spawn(params Type[] types)
        {
            try
            {
                if ((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags & SpawnFlags.FactoryMobile) != 0)
                    return Factory(types[Utility.Random(types.Length)]);
                else
                    return Activator.CreateInstance(types[Utility.Random(types.Length)]) as Mobile;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                DebugDump();
                return null;
            }
        }

        protected virtual Point3D GetSpawnLocation(Mobile m)
        {
            Spawner.SpawnFlags sflags = (((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags & SpawnFlags.SpawnFar) > 0 ? Spawner.SpawnFlags.SpawnFar : Spawner.SpawnFlags.None;
            return Spawner.GetSpawnPosition(Map, Location, Lvl_MaxRange, false, true, sflags, m);
            /*
			//Map map = Map;

			CanFitFlags flags = CanFitFlags.requireSurface;
			if (m != null && m.CanSwim == true) flags |= CanFitFlags.canSwim;
			if (m != null && m.CantWalk == true) flags |= CanFitFlags.cantWalk;

			if (Map == null)
				return Location;

			// Try 10 times to find a spawnable location not near a player.
			for (int i = 0; i < 10; i++)
			{
				int x;
				int y;
				if ((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags & SpawnFlags.SpawnFar) != 0)
				{
					x = (int)((double)Location.X + Spawner.RandomFar() * (double)Lvl_MaxRange);
					y = (int)((double)Location.Y + Spawner.RandomFar() * (double)Lvl_MaxRange);
				}
				else
				{
					x = Location.X + (Utility.Random((Lvl_MaxRange * 2) + 1) - Lvl_MaxRange);
					y = Location.Y + (Utility.Random((Lvl_MaxRange * 2) + 1) - Lvl_MaxRange);
				}
				int z = Map.GetAverageZ(x, y);

				if (Map.CanSpawnMobile(new Point2D(x, y), this.Z, flags) && !NearPlayer(new Point3D(x, y, this.Z)))
					return new Point3D(x, y, this.Z);
				if (Map.CanSpawnMobile(new Point2D(x, y), z, flags) && !NearPlayer(new Point3D(x, y, z)))
					return new Point3D(x, y, z);
			}

			// Try 10 more times to find a any spawnable location.
			for (int i = 0; i < 10; i++)
			{
				int x;
				int y;
				if ((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags & SpawnFlags.SpawnFar) != 0)
				{
					x = (int)((double)Location.X + Spawner.RandomFar() * (double)Lvl_MaxRange);
					y = (int)((double)Location.Y + Spawner.RandomFar() * (double)Lvl_MaxRange);
				}
				else
				{
					x = Location.X + (Utility.Random((Lvl_MaxRange * 2) + 1) - Lvl_MaxRange);
					y = Location.Y + (Utility.Random((Lvl_MaxRange * 2) + 1) - Lvl_MaxRange);
				}
				int z = Map.GetAverageZ(x, y);

				if (Map.CanSpawnMobile(new Point2D(x, y), this.Z, flags))
					return new Point3D(x, y, this.Z);
				if (Map.CanSpawnMobile(new Point2D(x, y), z, flags))
					return new Point3D(x, y, z);

			}

			// Experimental, property-based, error reporting.
			//  cannot find a valid location to spawn this creature
			Lvl_LevelError = LevelErrors.No_Location;

			return Location;
			 */
        }

        protected bool NearPlayer(Point3D px)
        {
            IPooledEnumerable eable = Map.GetMobilesInRange(px, 15);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile && m.AccessLevel <= AccessLevel.Player)
                {
                    eable.Free();
                    return true;
                }
            }
            eable.Free();

            return false;
        }

        // public overrides from Item
        public override void OnLocationChange(Point3D oldLoc)
        {
            base.OnLocationChange(oldLoc);

            if ((Deleted || !m_bGraphics))
                return;

            // Update grpahics
            if (m_Graphics != null)
                m_Graphics.UpdateLocation();
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (Deleted || !m_bGraphics)
                return;

            // update grphics
            if (m_Graphics != null)
                m_Graphics.UpdateLocation();
        }

        public override void OnAfterDelete()
        {
            // cleanup			
            Graphics = false;
            WipeMonsters();
            base.OnAfterDelete();
        }

        public override void OnSingleClick(Mobile from)
        {
            // display information about current spawn
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                if (m_bActive)
                    LabelTo(from, "{0} (Active; Level: {1} / {2}; Kills: {3}/{4})", m_Type, Level, SpawnLevels.Count - 1, m_Kills, Lvl_MaxKills);
                else
                {
                    LabelTo(from, "{0} (Inactive; Levels : {1}) ", m_Type, SpawnLevels.Count - 1);
                    if (m_Restart != null)
                        if (m_Restart.Running)
                            LabelTo(from, "Restart timer active...!");
                }

            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                from.SendGump(new PropertiesGump(from, this));
            if (from.AccessLevel >= AccessLevel.Seer)
                from.SendGump(new ChampMobileFactoryGump(this));
        }
        #endregion


        // Slice and Restart timer classes for the ChampEngine.
        private class ChampRestartTimer : Timer
        {
            private ChampEngine m_Spawn;

            public ChampRestartTimer(ChampEngine spawn, TimeSpan delay)
                : base(delay)
            {
                m_Spawn = spawn;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                // call restart code
                m_Spawn.Restart();
            }
        }

        private class ChampSliceTimer : Timer
        {
            private ChampEngine m_Spawn;

            public ChampSliceTimer(ChampEngine spawn)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Spawn = spawn;
                // update spawn every second
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                // pump the pump
                m_Spawn.OnSlice();
            }
        }
    } // class
}
