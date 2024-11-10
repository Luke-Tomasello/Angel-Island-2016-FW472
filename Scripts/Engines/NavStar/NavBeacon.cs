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

/* Scripts/Engines/NavStar/NavBeacon.cs
 * CHANGELOG
 * 3/26/2024, Adam,
 *   Complete rewrite of the NavStar system. Smaller (works,) and easier to understand
 * 9/12/21, Adam
 *  Add objective to beacon properties so that we can determine direction to travel without hard-coding it in the code.
 *      That is, there will be one beacon tagged as the 'objective', all beacons will lead in that direction.
 * 11/30/05, Kit
 *		Set Movable to false, added On/Off active switch, set all path values default -1, changed to admin access only
 * 11/18/05, Kit
 * 	Initial Creation
 */

using Server.Engines;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Items
{
    [Aliases("Server.Items.NavBeacon")]
    public class NavigationBeacon : Item
    {
        private static Dictionary<string, List<NavigationBeacon>> m_registry = new Dictionary<string, List<NavigationBeacon>>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, List<NavigationBeacon>> Registry { get { return m_registry; } }

        #region FindObjective
        #region TODO?
        public NavigationBeacon FindObjective()
        {
            return FindObjective(this);
        }
        public NavigationBeacon FindObjective(NavigationBeacon beacon)
        {
            foreach (KeyValuePair<string, List<NavigationBeacon>> kvp in Registry)
            {
                if (kvp.Value.Contains(beacon))
                {
                    foreach (NavigationBeacon check in kvp.Value)
                    {
                        //TODO
                        //if (check.Objective == true)
                        //return check;
                    }
                }
            }

            return null;
        }
        #endregion TODO?
        public static NavigationBeacon FindObjective(Mobile m, string dest)
        {
            if (!string.IsNullOrEmpty(dest) && Registry.ContainsKey(dest))
                foreach (NavigationBeacon beacon in Registry[dest])
                    // TODO
                    ;// if (beacon.Objective == true)
                     //return beacon;

            return null;
        }

        #endregion FindObjective

        #region Props
        private byte m_LinkCount;   // for debugging. Tell us how many mobs are using us
        [CommandProperty(AccessLevel.Seer, AccessLevel.Seer)]
        public byte LinkCount { get { return m_LinkCount; } set { m_LinkCount = value; } }

        [CommandProperty(AccessLevel.Seer)]
        public bool Active
        {
            get { return base.IsRunning; }
            set { base.IsRunning = value; }
        }
        private string m_Journey;
        [CommandProperty(AccessLevel.Seer)]
        public string Journey { get { return m_Journey; } set { m_Journey = value; } }
        private int m_RingID;
        [CommandProperty(AccessLevel.Seer)]
        public int Ring { get { return m_RingID; } set { m_RingID = value; } }
        #endregion Props

        [Constructable]
        public NavigationBeacon(string journey, int beaconID)
            : base(0x1ECD)
        {
            Name = "NavBeacon";
            Weight = 0.0;
            Hue = 0x47E;
            Visible = false;
            Movable = false;
            Active = true;

            m_Journey = journey;
            m_RingID = beaconID;

            RegisterBeacon();
        }
        public override Item Dupe(int amount)
        {
            Item new_item = new NavigationBeacon(m_Journey, m_RingID);
            return base.Dupe(new_item, amount);
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, string.Format("{0}: Ring {1}", m_Journey, m_RingID));
            if (Debug)
                LabelTo(from, string.Format("{0} links here", m_LinkCount));
            if (Active)
                LabelTo(from, "[Active]");
            else
                LabelTo(from, "[Disabled]");
        }
        [CommandProperty(AccessLevel.Administrator)]
        public override bool Debug
        {
            get
            {
                return base.Debug;
            }
            set
            {
                base.Debug = value;
            }
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public override void OnDelete()
        {
            UnregisterBeacon();
            base.OnDelete();
        }

        public NavigationBeacon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5);

            // version 5, remove m_objective

            // version 4
            // use base Item for running state

            // version 3
            writer.Write(m_Journey);
            writer.Write(m_RingID);

            // version 2 - obsolete in version 5
            //writer.Write(m_objective);

            // version 1 - obsolete in version 4
            //writer.Write(m_Running);

            //WriteNavArray(writer, NavArray);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 5:
                case 4:
                case 3:
                    {
                        m_Journey = reader.ReadString();
                        m_RingID = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        if (version < 5)
                            /*m_objective = */
                            reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 4)
                            /*m_Running = */
                            reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                        {
                            ///*NavArray = */ReadNavArray(reader);
                            int size = reader.ReadInt();
                            int[,] newBA = new int[size, 1];

                            for (int i = 0; i < size; i++)
                            {
                                newBA[i, 0] = reader.ReadInt();
                            }
                        }
                        break;
                    }

            }

            // register our beacons for easy (and quick) access
            if (version > 2)
                RegisterBeacon();
        }
        public void RegisterBeacon()
        {
            //System.Diagnostics.Debug.Assert(m_Journey != null);

            if (m_Journey == null)
            {
                this.Delete();
                Utility.Monitor.WriteLine("{0} has no Journey. Deleting...", ConsoleColor.Red, this);
                return;
            }

            if (Registry.ContainsKey(m_Journey))
            {
                if (!Registry[m_Journey].Contains(this))
                    Registry[m_Journey].Add(this);
            }
            else
            {
                Registry.Add(m_Journey, new List<NavigationBeacon>());
                Registry[m_Journey].Add(this);
            }
        }
        public void UnregisterBeacon()
        {
            if (m_Journey == null)
                return;

            if (Registry.ContainsKey(m_Journey))
            {
                if (Registry[m_Journey].Contains(this))
                    Registry[m_Journey].Remove(this);

                if (Registry[m_Journey].Count == 0)
                    Registry.Remove(m_Journey);
            }
        }
        #region TOOLS
        public static void Initialize()
        {   // easy, walk around the drop beacons at your feet
            CommandSystem.Register("DropBeacon", AccessLevel.Owner, new CommandEventHandler(DropBeacon_OnCommand));
            // renumber beacons
            CommandSystem.Register("RenumberBeacons", AccessLevel.Owner, new CommandEventHandler(RenumberBeacons_OnCommand));
            //
            CommandSystem.Register("DropBeaconMarker", AccessLevel.Owner, new CommandEventHandler(DropBeaconMarker_OnCommand));
            //
            CommandSystem.Register("ProcessBeaconMarkers", AccessLevel.Owner, new CommandEventHandler(ProcessBeaconMarkers_OnCommand));
            //
            CommandSystem.Register("GoToBeacon", AccessLevel.Owner, new CommandEventHandler(GoToBeacon_OnCommand));
            //
            CommandSystem.Register("FindBeacon", AccessLevel.Owner, new CommandEventHandler(FindBeacon_OnCommand));
            //
            CommandSystem.Register("WipeBeaconMarkers", AccessLevel.Owner, new CommandEventHandler(WipeBeaconMarkers_OnCommand));
            //
            CommandSystem.Register("WipeBeacons", AccessLevel.Owner, new CommandEventHandler(WipeBeacons_OnCommand));

            // save beacons on test server, load on production
            CommandSystem.Register("SaveBeacons", AccessLevel.Owner, new CommandEventHandler(SaveBeacons_OnCommand));
            CommandSystem.Register("LoadBeacons", AccessLevel.Owner, new CommandEventHandler(LoadBeacons_OnCommand));
        }
        [Usage("RenumberBeacons")]
        [Description("Renumbers all beacons with consistent Ring numbers.")]
        public static void RenumberBeacons_OnCommand(CommandEventArgs e)
        {   // doesn't work due to beacons correctly having duplicate ring numbers
            try
            {
                Dictionary<string, List<NavigationBeacon>> table = new Dictionary<string, List<NavigationBeacon>>(StringComparer.OrdinalIgnoreCase);
                foreach(Item item in World.Items.Values)
                    if (item is NavigationBeacon nb)
                    {
                        if (table.ContainsKey(nb.Journey))
                        {
                            table[nb.Journey].Add(nb);
                        }
                        else
                        {
                            table.Add(nb.Journey, new List<NavigationBeacon>() { nb });
                        }
                    }
                foreach (var kvp in table)
                {
                    List<NavigationBeacon> list = new List<NavigationBeacon>(kvp.Value);
                    list = list.OrderBy(x => x.Ring).ToList();
                    ;
                    ;
                    ;
                    ;
                }
                ;
                ;
            }
            catch
            {
                e.Mobile.SendMessage("?");
            }
        }
        [Usage("DropBeacon <journey name> <ring number>")]
        [Description("Drops a beacon at your feet for the specified Journey with the specified Ring number.")]
        public static void DropBeacon_OnCommand(CommandEventArgs e)
        {
            try
            {
                new NavigationBeacon(e.GetString(0), e.GetInt32(1)).MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            }
            catch
            {
                e.Mobile.SendMessage("Usage: DropBeacon <journey name> <ring number>");
            }
        }
        [Usage("WipeBeaconMarkers")]
        [Description("Deleted all beacon markers.")]
        public static void WipeBeaconMarkers_OnCommand(CommandEventArgs e)
        {
            try
            {
                List<BeaconMarker> list = new List<BeaconMarker>();
                foreach (Item item in World.Items.Values)
                    if (item is BeaconMarker bm)
                        list.Add(bm);

                foreach (BeaconMarker bm in list)
                    bm.Delete();
            }
            catch
            {
                e.Mobile.SendMessage("?");
            }
        }
        [Usage("WipeBeacons")]
        [Description("Deleted all beacons.")]
        public static void WipeBeacons_OnCommand(CommandEventArgs e)
        {
            try
            {
                List<NavigationBeacon> list = new List<NavigationBeacon>();
                foreach (Item item in World.Items.Values)
                    if (item is NavigationBeacon nb)
                        list.Add(nb);

                foreach (NavigationBeacon bm in list)
                    bm.Delete();

                e.Mobile.SendMessage("{0} Navigation Beacons deleted", list.Count);
            }
            catch
            {
                e.Mobile.SendMessage("?");
            }
        }
        [Usage("ProcessBeaconMarkers <journey name> <-forward|-backward>")]
        [Description("Generates a set of NavBeacons for each beacon marker, Ring step 10.")]
        public static void ProcessBeaconMarkers_OnCommand(CommandEventArgs e)
        {
            try
            {
                List<BeaconMarker> list = new List<BeaconMarker>();
                foreach (Item item in World.Items.Values)
                    if (item is BeaconMarker bm)
                        list.Add(bm);

                if (e.GetString(1) == "-forward")
                    list = list.OrderBy(x => x.Serial).ToList();
                else if (e.GetString(1) == "-backward")
                    list = list.OrderByDescending(x => x.Serial).ToList();
                else
                    throw new ApplicationException();
                int ring = 100;

                foreach (BeaconMarker bm in list)
                {
                    for (int ix = 0; ix < 4; ix++)
                    {
                        NavigationBeacon beacon = new NavigationBeacon(e.GetString(0), ring);
                        Point3D px = Spawner.GetSpawnPosition(bm.Map, bm.Location, homeRange: 4, bm);
                        beacon.MoveToWorld(px, bm.Map);
                    }

                    ring += 100;
                }
            }
            catch
            {
                e.Mobile.SendMessage("Usage: ProcessBeaconMarkers <journey name> <-forward|-backward>");
            }
        }
        [Usage("GoToBeacon <journey name> <-first|-last>")]
        [Description("Jumps you to the first or last beacon based on Ring.")]
        public static void GoToBeacon_OnCommand(CommandEventArgs e)
        {
            try
            {
                List<NavigationBeacon> list = new List<NavigationBeacon>();
                foreach (Item item in World.Items.Values)
                    if (item is NavigationBeacon nb && nb.Journey.Equals(e.GetString(0), StringComparison.OrdinalIgnoreCase))
                        list.Add(nb);

                if (e.GetString(1) == "-first")
                    list = list.OrderBy(x => x.Ring).ToList();
                else if (e.GetString(1) == "-last")
                    list = list.OrderByDescending(x => x.Ring).ToList();
                else
                    throw new ApplicationException();
                
                if (list.Count == 0)
                    e.Mobile.SendMessage("No beacons with journey name {0}", e.GetString(0));
                else
                {
                    e.Mobile.MoveToWorld(list[0].Location, list[0].Map);
                    e.Mobile.SendMessage("{0} Ring: {1}", e.GetString(0), list[0].Ring);
                }
            }
            catch
            {
                e.Mobile.SendMessage("Usage: GoToBeacon <journey name> <-first|-last>");
            }
        }
        [Usage("FindBeacon <journey name> <Ring>")]
        [Description("Load your jump list with beacons matching Journey and Ring.")]
        public static void FindBeacon_OnCommand(CommandEventArgs e)
        {
            try
            {
                List<NavigationBeacon> list = new List<NavigationBeacon>();
                foreach (Item item in World.Items.Values)
                    if (item is NavigationBeacon nb && 
                        nb.Journey.Equals(e.GetString(0), StringComparison.OrdinalIgnoreCase) &&
                        nb.Ring == e.GetInt32(1))
                        list.Add(nb);

                PlayerMobile pm = e.Mobile as PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new System.Collections.ArrayList(list);

                if (list.Count == 0)
                    e.Mobile.SendMessage("No beacons with journey name {0} and Ring {1}", e.GetString(0), e.GetInt32(1));
                else
                    pm.SendMessage("your jumplist has been loaded with {0} items.", list.Count);
            }
            catch
            {
                e.Mobile.SendMessage("Usage: FindBeacon <journey name> <Ring>");
            }
        }
        class BeaconMarker : Item
        {
            [Constructable]
            public BeaconMarker()
            : base(0x1F14)
            {
                Movable = false;
            }

            public BeaconMarker(Serial serial)
                : base(serial)
            {
            }
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                int version = 1;
                writer.Write(version);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);
                int version = reader.ReadInt();
            }
        }
        [Usage("DropBeaconMarker")]
        [Description("Drops a beacon marker at your feet.")]
        public static void DropBeaconMarker_OnCommand(CommandEventArgs e)
        {
            try
            {
                new BeaconMarker().MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            }
            catch
            {
                e.Mobile.SendMessage("?");
            }
        }

        [Usage("SaveBeacons")]
        [Description("Saves all beacons.")]
        public static void SaveBeacons_OnCommand(CommandEventArgs e)
        {
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Beacons.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                List<NavigationBeacon> list = new List<NavigationBeacon>();
                foreach (Item item in World.Items.Values)
                    if (item is NavigationBeacon nb && nb.Journey.Contains("=>"))
                        list.Add(nb);

                writer.Write(list.Count);

                foreach (NavigationBeacon nb in list)
                {
                    writer.Write(nb.Map);
                    writer.Write(nb.Location);
                    writer.Write(nb.Journey);
                    writer.Write(nb.Ring);
                }

                writer.Close();

                e.Mobile.SendMessage("{0} beacons saved", list.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Beacons.bin");
                Console.WriteLine(ex.ToString());
            }
        }

        [Usage("LoadBeacons")]
        [Description("Loads all beacons.")]
        public static void LoadBeacons_OnCommand(CommandEventArgs e)
        {
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Beacons.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();
                int count = 0;
                switch (version)
                {
                    case 1:
                        {
                            count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Map map = reader.ReadMap();
                                Point3D px = reader.ReadPoint3D();
                                string journey = reader.ReadString();
                                int ring = reader.ReadInt();

                                NavigationBeacon nb = new NavigationBeacon(journey, ring);
                                nb.MoveToWorld(px, map);
                            }
                            break;
                        }
                    default:
                        {
                            reader.Close();
                            throw new Exception("Invalid Beacons.bin savefile version.");
                        }
                }

                reader.Close();

                e.Mobile.SendMessage("{0} beacons loaded", count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Beacons.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion TOOLS
    }

}
