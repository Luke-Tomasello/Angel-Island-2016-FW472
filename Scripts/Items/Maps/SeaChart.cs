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

/* Scripts\Items\Maps\SeaChart.cs
 * ChangeLog:
 * 8/9/2024, Adam (BinaryFormatter)
 *  Microsoft obsoleted BinaryFormatter in .NET 5 due to security concerns.
 *  Therefore, in order to port this project, BinaryFormatter had to be replaced.
 *      Unfortunately, the internal format for BinaryFormatter is unknown and Microsoft's 
 *      migration is ugly and cumbersome. I instead chose incompatibility with 
 *      framework 4.7.2 versions of "Data/seatreasure.bin" and will generate an error if 
 *      one of these old versions are read. 
 *      Administrators can generate a fresh set of map points with [GenMapPoints
 * 9/12/10, Adam
 *		Created.
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{
    public class SeaChart : MapItem
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
        }

        public static void OnLoad()
        {
            Console.Write("Loading sea treasure map locations...");
            int lcount = LoadLocations();
            Console.WriteLine("done ({0} locations loaded.)", lcount.ToString());
        }

        // takes hours, do not run on production
        public static void Initialize()
        {
            Server.CommandSystem.Register("GenMapPoints", AccessLevel.Owner, new CommandEventHandler(GenMapPoints));
            Server.CommandSystem.Register("CheckPoint", AccessLevel.Owner, new CommandEventHandler(CheckPoint));
            Server.CommandSystem.Register("CheckMapPoints", AccessLevel.Owner, new CommandEventHandler(CheckMapPoints));
            Server.CommandSystem.Register("LoadMapPoints", AccessLevel.Owner, new CommandEventHandler(LoadMapPoints));
        }

        private int m_level = 0;                                            // player crafted is level 0
                                                                            // 5 rows of 100 columns - each cell contains an array of 6 Points
        private static Point2D[,][] m_Locations = new Point2D[5, 500][];    // treasure locations

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D FirstPin
        {
            get
            {
                if (Pins != null && Pins.Count > 0 && Pins[0] is Point2D)
                {
                    int worldX, worldY;
                    this.ConvertToWorld(((Point2D)Pins[0]).X, ((Point2D)Pins[0]).Y, out worldX, out worldY);
                    return new Point2D(worldX, worldY);
                }
                else
                    return Point2D.Zero;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_level; } set { m_level = value; InvalidateProperties(); } }

        public static Point2D GetRandomLocation(int level)
        {
            Point2D[] temp = m_Locations[level - 1, Utility.Random(m_Locations.GetUpperBound(1) + 1)];
            if (temp != null)
            {   // the first point is the map focus point (center of the map)
                return temp[0];
            }

            return Point2D.Zero;
        }

        public static Point2D[] GetPinLocations(int level, Point2D point)
        {
            for (int ix = 0; ix <= m_Locations.GetUpperBound(1); ix++)
            {
                Point2D[] temp = m_Locations[level - 1, ix];// get array at this location
                if (temp != null)                           // do we have an array here?
                    if (temp[0] == point)                   // does this table match?
                        return temp;                        // return the pins
            }

            return null;
        }

        private class MapNode
        {
            private Point2D[] m_pins = new Point2D[6];
            public Point2D[] Pins { get { return m_pins; } }
            private int m_level;
            public int Level { get { return m_level; } }
            public MapNode(int level, Point2D[] pins)
            {
                m_level = level;
                m_pins = pins;
            }
        }

        private static int LoadLocations()
        {
            string filePath = Path.Combine(Core.BaseDirectory, "Data/seatreasure.bin");

            // load up our compiled tables of locations
            if (File.Exists(filePath))
            {
                try
                {
                    int rowx, coly;
                    // read it back
#if true

                    BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)));
                    for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                        for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                        {
                            m_Locations[rowx, coly] = new Point2D[6];
                            //m_Locations[rowx, coly] = (Point2D[])bin.Deserialize(stream);
                            for (int ix = 0; ix < 6; ix++)
                                m_Locations[rowx, coly][ix] = reader.ReadPoint2D();
                        }
                    reader.Close();

                    // check for a corrupted bin file
                    int point_failures = 0;
                    for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                        for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                        {
                            for (int ix = 0; ix < 6; ix++)
                            {
                                point_failures = 0;
                                Point2D test = m_Locations[rowx, coly][ix];
                                for (int ji = 0; ji < Utility.BritWrap.Length; ji++)
                                    // are we inside the one of the brit rects?
                                    if (Utility.BritWrap[ji].Contains(test) == false)
                                    {   // bad point - not in any of the brit rects
                                        if (++point_failures == Utility.BritWrap.Length)
                                            throw new ApplicationException("seatreasure.bin is corrupted and needs to be regenerated.");
                                    }
                            }
                        }
                    ;
#else
                    using (Stream stream = File.Open(filePath, FileMode.Open))
                    {
                        BinaryFormatter bin = new BinaryFormatter();

                        for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                            for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                            {
                                m_Locations[rowx, coly] = new Point2D[6];
                                m_Locations[rowx, coly] = (Point2D[])bin.Deserialize(stream);
                            }
                    }
#endif
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
            else
                Console.WriteLine("No treasure map locations loaded. Cannot find Data/seatreasure.bin");

            return m_Locations.Length;
        }

        [Constructable]
        public SeaChart()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public override void CraftInit(Mobile from)
        {
            double skillValue = from.Skills[SkillName.Cartography].Value;
            int dist = 64 + (int)(skillValue * 10);

            if (dist < 200)
                dist = 200;

            int size = 24 + (int)(skillValue * 3.3);

            if (size < 200)
                size = 200;
            else if (size > 400)
                size = 400;

            SetDisplay(from.X - dist, from.Y - dist, from.X + dist, from.Y + dist, size, size);
        }

        #region System Constructable
        [Constructable]
        public SeaChart(int level)
            : this(level, SeaChart.GetRandomLocation(level))
        {
        }

        [Constructable]
        public SeaChart(int level, Point2D location)
        {
            m_level = level;

            double skillValue;                                          // = from.Skills[SkillName.Cartography].Value;

            switch (level)
            {
                case 5:
                    skillValue = 100;
                    Name = "a legendary fishing spot";
                    break;
                case 4:
                    skillValue = 90;
                    Name = "a fabled fishing spot";
                    break;
                case 3:
                    skillValue = 50;
                    Name = "a good fishing spot";
                    break;
                case 2:
                    skillValue = 30;
                    Name = "a decent fishing spot";
                    break;
                case 1:
                default:
                    skillValue = 20;
                    Name = "a fair fishing spot";
                    break;
            }

            int dist = 64 + (int)(skillValue * 10);

            if (dist < 200)
                dist = 200;

            int size = 24 + (int)(skillValue * 3.3);

            if (size < 200)
                size = 200;
            else if (size > 400)
                size = 400;

            SetDisplay(location.X - dist, location.Y - dist, location.X + dist, location.Y + dist, size, size);

            Point2D[] pins = SeaChart.GetPinLocations(level, location);

            // place level + 1 pins
            if (pins != null)
                for (int pinNum = 0; pinNum < pins.Length && pinNum <= level; pinNum++)
                {
                    int mapX, mapY;
                    ConvertToMap(pins[pinNum].X, pins[pinNum].Y, out mapX, out mapY);
                    AddPin(mapX, mapY);
                }
        }
        #endregion

        #region GenMapPoints
        /// <summary>
        /// GenMapPoints generates 5 sets of pin locations, one for each level of map.
        /// PROCESS: (Bruteforce)
        /// 1. enum all felucca tiles looking for 'wet' tiles
        /// 2. for each wet tile, keep the ones that are within 64 tiles of land
        /// 3. Save this as the 'focus point' of the map AND as pin 1
        /// 4. From this pin (the now current pin) look for a wet tile--in a random direction--upto 50 tiles away; 
        ///		prefer far to near. validate against minimum map bounds (200x200)
        /// 5. Protect against reusing an existing pin lication
        /// 6. This pin now becomes the current pin, save it, and repeat until we have 6 pins total
        /// 7. Create map a record with a level indicator. level 1 is closest to land, level 5 is farthest from land, etc.
        /// 8. Add record to a sorted list where the sort key is a randon number assuring the list is random (smooth distribution)
        /// 9. * This process takes hours and will generate about 1.9 million records. *
        /// 10. fill 5 tables of 500 elements with the 6 map points where each table represents a level (Each level gets 500 locations.)
        /// 11. serialize these 2500 map points to Data/seatreasure.bin. This table will be reloaded on server up.
        /// </summary>
        /// <param name="e"></param>
        [Usage("GenMapPoints")]
        [Description("Generates a list of interesting points out in the water. TAKES HOURS!")]
        public static void GenMapPoints(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Begin generation.");
                Dolphin fish = new Dolphin();
                SeaChart tempMap = null;
                double maxd = 0; double mind = 64; int count = 0; int failedPin = 0;
                int pinNdx = 0; int pinCollisions = 0; int notInRect = 0; int specialExclusions = 0; int badMap = 0;
                int wetTileFailure = 0; int badMapBounds = 0; int failMap = 0; int noLand = 0; int minDist = 0;
                SortedList<double, MapNode> list = new SortedList<double, MapNode>();
                for (int ji = 0; ji < Utility.BritWrap.Length; ji++)
                {
                    for (int mx = Utility.BritWrap[ji].Start.X; mx <= (Utility.BritWrap[ji].Start.X + Utility.BritWrap[ji].Width - 1); mx++)
                    {
                        for (int my = Utility.BritWrap[ji].Start.Y; my <= (Utility.BritWrap[ji].Start.Y + Utility.BritWrap[ji].Height - 1); my++)
                        {
                            // are we inside the brit rect?
                            if (Utility.BritWrap[ji].Contains(new Point2D(mx, my)) == false)
                            {   // bad point - not in rect
                                notInRect++;
                                continue;
                            }

                            // wet tile rect sanity check - make sure we are surrounded by water (not a puddle)
                            if (PuddleCheck(Utility.BritWrap[ji], new Point2D(mx, my)) == false)
                            {
                                wetTileFailure++;
                                continue;
                            }

                            // exclude special areas (eg. angel island prison, dungeons)
                            if (ValidRegion(mx, my) == false)
                            {
                                specialExclusions++;
                                continue;
                            }

                            // okay, we have what looks like a good map center and first pin
                            // we must have land within 64 tiles
                            Point3D wetLoc = new Point3D(mx, my, Map.Felucca.GetAverageZ(mx, my));
                            Point3D dryLoc = Spawner.GetSpawnPosition(Map.Felucca, wetLoc, 64, null);
                            if (wetLoc == dryLoc)
                            {   // spawner failed to find a good location
                                noLand++;
                                continue;
                            }

                            // okay, we have a focus point
                            //	now place the remaining N pins with validation

                            // dist2land to old point
                            double dist2land = GetDistance(new Point2D(wetLoc), new Point2D(dryLoc));

                            // level of this map
                            int level = (dist2land < 21) ? 1 : (dist2land < 31) ? 2 : (dist2land < 41) ? 3 : (dist2land < 51) ? 4 : (dist2land < 91) ? 5 : 0;

                            // create a temp map to validate pin locations
                            #region TEMP MAP CREATION & VALIDATION
                            if (tempMap != null)
                            {
                                tempMap.Delete();
                                tempMap = null;
                            }
                            // new level-sized map
                            tempMap = new SeaChart(level, new Point2D(wetLoc));

                            // make sure there is no w/h failure. I think this happens when we are at the map edge
                            if (tempMap.Bounds.Width <= 0 || tempMap.Height <= 0)
                            {
                                badMap++;
                                continue;
                            }
                            // even though our wet-point is in the brit rect, the bounds may not be. Exclude those maps here
                            if (!Utility.BritWrap[ji].Contains(tempMap.Bounds.Start) || !Utility.BritWrap[ji].Contains(tempMap.Bounds.End))
                            {
                                badMapBounds++;
                                continue;
                            }
                            #endregion TEMP MAP CREATION & VALIDATION

                            #region ADD PINS
                            // okay, place the start pin
                            Point2D[] pins = new Point2D[6];
                            pinNdx = 0;
                            pins[pinNdx++] = new Point2D(wetLoc.X, wetLoc.Y);

                            Point3D newPinLoc;              // new pin location
                            Point3D lastPinLoc = wetLoc;    // last pin location

                            // okay, try to place 6 pins
                            int good = 1;                   // how many pins have we placed?
                            for (int hx = 1; hx < 6; hx++)  // 6 pins
                            {   // 100 tries to place a pin
                                for (int retry = 0; retry < 100; retry++)
                                {
                                    // get a new pin location 
                                    newPinLoc = Spawner.GetSpawnPosition(Map.Felucca, lastPinLoc, 200, fish, Spawner.SpawnerFlags.SpawnFar);
                                    if (newPinLoc == lastPinLoc)
                                        continue;

                                    // did we exit the brit rect?
                                    if (Utility.BritWrap[ji].Contains(newPinLoc) == false)
                                    {   // bad point - not in rect
                                        notInRect++;
                                        continue;
                                    }

                                    // wet tile rect sanity check - make sure we are surrounded by water (not a puddle)
                                    if (PuddleCheck(Utility.BritWrap[ji], new Point2D(newPinLoc)) == false)
                                    {
                                        wetTileFailure++;
                                        continue;
                                    }

                                    // is this pin in an illegal region?
                                    if (ValidRegion(newPinLoc.X, newPinLoc.Y) == false)
                                    {
                                        specialExclusions++;
                                        continue;
                                    }

                                    // validate that we don't exceed the map bounds
                                    if (ValidBounds(tempMap, newPinLoc.X, newPinLoc.Y) == false)
                                    {
                                        badMap++;
                                        continue;
                                    }

                                    // validate a minmum distance
                                    if (ValidDistance(150, new Point2D(lastPinLoc), new Point2D(newPinLoc)) == false)
                                    {
                                        minDist++;
                                        continue;
                                    }

                                    if (ValidUnique(pins, pinNdx, new Point2D(newPinLoc.X, newPinLoc.Y)) == false)
                                    {
                                        pinCollisions++;
                                        continue;
                                    }

                                    // okay, looks to be a reasonable pin location
                                    pins[pinNdx++] = new Point2D(newPinLoc.X, newPinLoc.Y);
                                    lastPinLoc = newPinLoc; // make the next pin relative to this pin
                                    good++;
                                    break;
                                }
                            }

                            if (good < 6)
                            {   // skip this location as we can't place sufficient pins
                                failedPin++;
                                continue;
                            }
                            #endregion ADD PINS


                            // Create an actual map and run sanity checks on it.
                            //	for some unknown reason, we're still getting like 10% bad maps
                            #region FINAL CHECK
                            for (int rm = 0; rm < pins.Length; rm++)
                            {
                                tempMap.AddWorldPin(pins[rm].X, pins[rm].Y);
                            }
                            bool[] failTest = new bool[10];
                            if (CheckMap(tempMap, failTest) == false)
                            {
                                failMap++;
                                tempMap.ClearPins();
                                continue;
                            }
                            tempMap.ClearPins();
                            #endregion FINAL CHECK

                            // maps processed
                            count++;

                            // give a little feedback since this process is so slow
                            if (count % 1000 == 0)
                                Console.Write("{0},", count);

                            // add to sorted list
                            double key = Utility.RandomDouble();
                            bool retrykey = false;
                            while (list.ContainsKey(key))
                            {   // feedback for key retries
                                Console.Write(".");
                                key = Utility.RandomDouble();
                                retrykey = true;
                            }
                            list.Add(key, new MapNode(level, pins));

                            if (retrykey == true)
                                Console.Write(",");
                        }
                    }
                }

                // no longer need our test fish
                fish.Delete();

                Console.WriteLine();
                Console.WriteLine("Fill arrays");

                int[] indexes = new int[m_Locations.GetUpperBound(0) + 1];  // column index
                foreach (KeyValuePair<double, MapNode> kvp in list)
                {   // only store the first 500 (m_Locations.GetUpperBound(1)+1) for each level
                    if (indexes[kvp.Value.Level - 1] > m_Locations.GetUpperBound(1))
                        continue;

                    m_Locations[kvp.Value.Level - 1, indexes[kvp.Value.Level - 1]] =
                        new Point2D[6] {
                        kvp.Value.Pins[0], kvp.Value.Pins[1], kvp.Value.Pins[2],
                        kvp.Value.Pins[3], kvp.Value.Pins[4], kvp.Value.Pins[5] };

                    indexes[kvp.Value.Level - 1]++;
                }

                Console.WriteLine("Write the data");
                try
                {
                    int rowx, coly;
                    string filePath = Path.Combine(Core.BaseDirectory, "Data/seatreasure.bin");
#if true
                    BinaryFileWriter writer = new BinaryFileWriter(filePath, prefixStr: true);
                    for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                        for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                            for (int ix = 0; ix < 6; ix++)
                                writer.Write(m_Locations[rowx, coly][ix]);
                    writer.Close();
#else
                    using (Stream stream = File.Open(filePath, FileMode.Create))
                    {
                        BinaryFormatter bin = new BinaryFormatter();
                        for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                            for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                                bin.Serialize(stream, m_Locations[rowx, coly]);
                    }
#endif
                    // read it back
#if true
                    BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)));
                    for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                        for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                        {
                            m_Locations[rowx, coly] = new Point2D[6];
                            //m_Locations[rowx, coly] = (Point2D[])bin.Deserialize(stream);
                            for (int ix = 0; ix < 6; ix++)
                                m_Locations[rowx, coly][ix] = reader.ReadPoint2D();
                        }
                    reader.Close();
#else
                    using (Stream stream = File.Open(filePath, FileMode.Open))
                    {
                        BinaryFormatter bin = new BinaryFormatter();

                        for (rowx = 0; rowx <= m_Locations.GetUpperBound(0); rowx++)
                            for (coly = 0; coly <= m_Locations.GetUpperBound(1); coly++)
                            {
                                m_Locations[rowx, coly] = new Point2D[6];
                                m_Locations[rowx, coly] = (Point2D[])bin.Deserialize(stream);
                            }
                    }
#endif
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }

                e.Mobile.SendMessage("Generation complete with {0} maps processed.", count);
                e.Mobile.SendMessage("{0} map locations were skipped for bad pin placement.", failedPin);
                e.Mobile.SendMessage("{0} pin locations were skipped because of pin collisions.", pinCollisions);
                e.Mobile.SendMessage("{0} points were skipped: outside rect.", notInRect);
                e.Mobile.SendMessage("{0} points were skipped: special area.", specialExclusions);
                e.Mobile.SendMessage("{0} maps were skipped: bad width/height.", badMap);
                e.Mobile.SendMessage("{0} pins were skipped: not wet.", wetTileFailure);
                e.Mobile.SendMessage("{0} pins were skipped: min distance.", minDist);
                e.Mobile.SendMessage("{0} maps were skipped: bad map bounds.", badMapBounds);
                e.Mobile.SendMessage("{0} maps were skipped: failed map test.", failMap);
                e.Mobile.SendMessage("{0} maps were skipped: failed dist to land test.", noLand);
                e.Mobile.SendMessage("{0} total maps output.", count - failedPin);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        [Usage("LoadMapPoints")]
        [Description("Loads the list of interesting points out in the water.")]
        public static void LoadMapPoints(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Loading sea treasure map locations...");
            int lcount = LoadLocations();
            e.Mobile.SendMessage("done. ({0} locations loaded.)", lcount.ToString());
        }

        #region MAPGEN TOOLS
        [Usage("CheckPoint")]
        [Description("Checks to see which world rect a given point is in")]
        public static void CheckPoint(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Begin generation.");
                Point2D px = Point2D.Parse(e.ArgString);
                if (Utility.BritWrap[0].Contains(px))
                    e.Mobile.SendMessage("Rect[0] contains point {0}", px);
                else if (Utility.BritWrap[1].Contains(px))
                    e.Mobile.SendMessage("Rect[1] contains point {0}", px);
                else
                    e.Mobile.SendMessage("No rect contains point {0}", px);
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Rect format error. Format is (XXX, YYY)");
            }
        }

        [Usage("CheckMapPoints")]
        [Description("Validate map points")]
        public static void CheckMapPoints(CommandEventArgs e)
        {
            try
            {
                if (e.ArgString != null && e.ArgString.Length > 0)
                {
                    Point2D px = Point2D.Parse(e.ArgString);
                    SeaChart chart = new SeaChart(1, px);

                    chart.AddWorldPin(px.X, px.Y);

                    bool[] failTest = new bool[10];
                    if (CheckMap(chart, failTest) == false)
                        e.Mobile.SendMessage("Failed map test");
                    else
                        e.Mobile.SendMessage("Passed map test");

                    for (int ix = m_Locations.GetLowerBound(0); ix <= m_Locations.GetUpperBound(0); ix++)
                        for (int jx = m_Locations.GetLowerBound(1); jx <= m_Locations.GetUpperBound(1); jx++)
                        {
                            for (int mx = 0; mx < m_Locations[ix, jx].Length; mx++)
                                if ((Point2D)m_Locations[ix, jx][mx] == px)
                                {
                                    e.Mobile.SendMessage("Database contains the point {0}", px);

                                    ValidDistance(150,
                                        (Point2D)m_Locations[ix, jx][mx],
                                        (Point2D)m_Locations[ix, jx][mx + 1]);

                                    return;
                                }

                        }

                    e.Mobile.SendMessage("Database does not contain the point {0}", px);
                }
                else
                {
                    e.Mobile.Target = new MapTarget();
                    e.Mobile.SendMessage("Check which map?");
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Usage: [checkMapPoints {0}", new Point2D(123, 456));
                LogHelper.LogException(ex);
            }
        }

        private class MapTarget : Target
        {
            public MapTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is SeaChart)
                {
                    bool failure = false;
                    SeaChart map = (targ as SeaChart);
                    for (int ix = 0; map.Pins != null && ix < map.Pins.Count; ix++)
                    {
                        bool[] FailTest = new bool[10];

                        int WorldX = 0, WorldY = 0;
                        map.ConvertToWorld(((Point2D)map.Pins[ix]).X, ((Point2D)map.Pins[ix]).Y,
                            out WorldX, out WorldY);

                        if (!Utility.BritWrap[0].Contains(new Point2D(WorldX, WorldY)) && !Utility.BritWrap[1].Contains(new Point2D(WorldX, WorldY)))
                        {
                            failure = true;
                            FailTest[0] = true;
                        }

                        if (!ValidBounds(map, WorldX, WorldY))
                        {
                            failure = true;
                            FailTest[1] = true;
                        }

                        if (!ValidWater(WorldX, WorldY))
                        {
                            failure = true;
                            FailTest[2] = true;
                        }

                        if (failure == true)
                        {
                            from.SendMessage("-- Pin {0} {1}", ix + 1, new Point2D(WorldX, WorldY));

                            if (FailTest[0] == true)
                                from.SendMessage("    No rects contain point {0}", (Point2D)map.Pins[ix]);

                            if (FailTest[1] == true)
                                from.SendMessage("    This point fails the validity check");

                            if (FailTest[2] == true)
                                from.SendMessage("    This point fails the wet tile check");

                            from.SendMessage("    Failed: map width({0})X({1}), map height({2})Y({3})",
                                map.Width, ((Point2D)map.Pins[ix]).X,
                                map.Height, ((Point2D)map.Pins[ix]).Y);

                            from.SendMessage("-- failed");
                        }
                    }

                    if (failure == false)
                        from.SendMessage("Success");
                }
                else
                    from.SendMessage("The is not a SeaChart.");
                return;
            }
        }

        public static double GetDistance(Point2D lastPinLoc, Point2D newPinLoc)
        {
            // distance to last point
            int xDelta2 = lastPinLoc.X - newPinLoc.X;
            int yDelta2 = lastPinLoc.Y - newPinLoc.Y;
            double dist2pin = Math.Sqrt((xDelta2 * xDelta2) + (yDelta2 * yDelta2));
            return dist2pin;
        }

        public static bool ValidDistance(double mindist, Point2D lastPinLoc, Point2D newPinLoc)
        {
            // distance to last point
            int xDelta2 = lastPinLoc.X - newPinLoc.X;
            int yDelta2 = lastPinLoc.Y - newPinLoc.Y;
            double dist2pin = Math.Sqrt((xDelta2 * xDelta2) + (yDelta2 * yDelta2));

            // validate a minmum distance
            if (dist2pin >= mindist)
                return true;

            return false;
        }

        public static bool ValidUnique(Point2D[] pins, int pinNdx, Point2D pin)
        {
            // check previous pin locations
            for (int gx = 0; gx < pinNdx; gx++)
                if (pins[gx] == pin)
                    return false;

            return true;
        }

        public static bool CheckMap(SeaChart map, bool[] FailTest)
        {
            if (map == null || map.Pins == null || map.Pins.Count == 0)
                return false;

            bool failure = false;
            for (int ix = 0; map.Pins != null && ix < map.Pins.Count; ix++)
            {
                int WorldX = 0, WorldY = 0;
                map.ConvertToWorld(((Point2D)map.Pins[ix]).X, ((Point2D)map.Pins[ix]).Y,
                    out WorldX, out WorldY);

                if (!Utility.BritWrap[0].Contains(new Point2D(WorldX, WorldY)) && !Utility.BritWrap[1].Contains(new Point2D(WorldX, WorldY)))
                {
                    failure = true;
                    FailTest[0] = true;
                }

                if (!ValidBounds(map, WorldX, WorldY))
                {
                    failure = true;
                    FailTest[1] = true;
                }

                if (!ValidWater(WorldX, WorldY))
                {
                    failure = true;
                    FailTest[2] = true;
                }
            }

            return !failure;
        }

        #endregion MAPGEN TOOLS

        private static bool PuddleCheck(Rectangle2D BritWrap, Point2D newPinLoc)
        {
            // wet tile rect sanity check - make sure we are surrounded by water (not a puddle)
            Rectangle2D wetRect = new Rectangle2D(newPinLoc.X - 5, newPinLoc.Y - 5, 10, 10);
            for (int wx = wetRect.Start.X; wx <= (wetRect.Start.X + wetRect.Width - 1); wx++)
            {
                for (int wy = wetRect.Start.Y; wy <= (wetRect.Start.Y + wetRect.Height - 1); wy++)
                {
                    if (BritWrap.Contains(new Point2D(wx, wy)) == false)
                        continue;

                    if (!ValidWater(wx, wy))
                        return false;
                }

            }

            return true;
        }

        private static bool ValidRegion(Rectangle2D r2d)
        {
            for (int wx = r2d.Start.X; wx <= (r2d.Start.X + r2d.Width - 1); wx++)
            {
                for (int wy = r2d.Start.Y; wy <= (r2d.Start.Y + r2d.Height - 1); wy++)
                {
                    if (ValidRegion(wx, wy) == false)
                        return false;
                }
            }

            return true;
        }

        private static bool ValidRegion(int mx, int my)
        {
            // exclude special areas (eg. angel island prison)
            System.Collections.ArrayList regions = Region.FindAll(new Point3D(mx, my, 0), Map.Felucca);
            if (regions != null)
            {
                for (int bx = 0; bx < regions.Count; bx++)
                {
                    Region rt = regions[bx] as Region;
                    if (rt.IsAngelIslandRules || rt.IsDungeonRules)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool ValidWater(int WorldX, int WorldY)
        {
            CanFitFlags flags = CanFitFlags.requireSurface | CanFitFlags.canSwim | CanFitFlags.cantWalk;
            int z = Map.Felucca.GetAverageZ(WorldX, WorldY);
            if (Map.Felucca.CanSpawnMobile(new Point2D(WorldX, WorldY), z, flags))
                return true;
            else
                return false;
        }


        private static bool ValidBounds(SeaChart smallMap, int x, int y)
        {
            try
            {
                int MapX = 0, MapY = 0;
                smallMap.ConvertToMap(x, y, out MapX, out MapY);

                int saveX = MapX, saveY = MapY;
                smallMap.Validate(ref MapX, ref MapY);

                // it's a good point if validate didn't try to correct it
                return (saveX == MapX && saveY == MapY);
            }
            catch
            {   // Some maps come up with a bounds width of zero, and ConvertToMap bombs
                return false;
            }
        }
        #endregion

        public override int LabelNumber { get { return 1015232; } } // sea chart

        public SeaChart(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_level == 0)
                base.OnSingleClick(from);
            else
            {
                double pins = (Pins != null) ? Pins.Count : 0;
                double done = 0;
                for (int ix = 0; ix < (int)pins; ix++)
                    if (m_PinComplete[ix] == true)
                        done++;

                double complete = (done / pins) * 100;

                if (complete == 0.0)
                    LabelTo(from, "{0}", Name);
                else if (complete == 100.0)
                    LabelTo(from, "{0} [completed]", Name);
                else
                    LabelTo(from, "{0} [{1:F0}% complete]", Name, complete);
            }
        }

        public override bool ReadOnly { get { return m_level > 0; } }

        private bool[] m_PinComplete = new bool[6];
        public bool[] PinComplete { get { return m_PinComplete; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            // version 1
            writer.Write(m_level);

            if (m_level > 0)
                for (int ix = 0; ix < 6; ix++)
                    writer.Write(m_PinComplete[ix]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    m_level = reader.ReadInt();
                    if (m_level > 0)
                        for (int ix = 0; ix < 6; ix++)
                            m_PinComplete[ix] = reader.ReadBool();
                    goto case 0;

                case 0:
                    break;
            }
        }
    }
}
