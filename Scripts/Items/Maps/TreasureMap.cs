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

/* Scripts/Items/Maps/TreasureMap.cs
 * ChangeLog
 *	2/3/11, Adam
 *		Make extended map locations based upon Core.AngelIsland
 *	9/1/10, Adam
 *		Make treasure maps ReadOnly 
 *	8/27/10, adam
 *		Use Utility.BritWrap rects from boat nav to test for valid treasure map locations.
 *		Turns out that the rects defined in Utility.BritWrap are the two halves of the world excluding T2A and the dungeons
 *		which is what we want.
 *	8/12/10, adam
 *		Add tax collector spawn
 *	1/20/06, Adam
 *		Add new OnNPCBeginDig() function so that NPC's can dig treasure.
 *	5/07/05, Adam
 *		Remove hue and replace the the text 
 *			"for somewhere in Felucca" with "for somewhere in Ocllo"
 *		if the map is in Ocllo
 *	5/07/05, Kit
 *		Hued maps to faction orange if for withen ocllo
 *	4/17/05, Kitaras	
 *		Fixed bug regarding level 1 and 2 chests being set to themed
 *	4/14/04, Adam
 *		1. Put back lost change to treasure map drop rate
 *		2. Convert LootChance to a property, and attach it to CoreAI.TreasureMapDrop
 *	4/07/05, Kitaras
 *		Fixed static access variable issue
 *	4/03/05, Kitaras	
 *		Added check to spawn only one level 5 mob on themed chests
 *	3/30/05, Kitaras
 *		Redesigned system to use new TreasureThemes.cs control system for spawn generation.
 *	3/1/05, mith
 *		OnDoubleClick(): modified difficulty check so that if base Carto skill is less than midPoint, players gets failure message.
 *  12/05/04, Jade
 *      Reverted the chance to drop t-map back to 1%
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Changed percentage chance for creature to drop t-map from 1% to 3.5%
 *
 *      8/30/04, Lego Eater
 *               changed the on singleclick so tmaps displayed properly (sorry for spelling)
 *
 *
 */

using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.IO;

namespace Server.Items
{
    public class TreasureMap : MapItem
    {

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
        }

        public static void OnLoad()
        {
            if (SpawnerCache.Spawners.Count == 0)
            {
                Console.Write("Loading spawner cache...");
                int count = SpawnerCache.LoadSpawnerCache();
                Console.WriteLine("done ({0} spawners loaded.)", count.ToString());
            }

            Console.Write("Loading treasure map locations...");
            int lcount = LoadLocations();
            Console.WriteLine("done ({0} locations loaded.)", lcount.ToString());
        }

        public override bool ReadOnly { get { return true; } set {; } }

        private int m_Level;
        private bool m_Completed;
        private Mobile m_Decoder;
        private Map m_Map;
        private Point2D m_ChestLocation;
        private bool m_Themed;
        private ChestThemeType m_type;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; InvalidateProperties(); } }

        //set theme type
        [CommandProperty(AccessLevel.GameMaster)]
        public ChestThemeType Theme { get { return m_type; } set { m_type = value; InvalidateProperties(); } }

        //set if map is themed or not
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Themed { get { return m_Themed; } set { m_Themed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed { get { return m_Completed; } set { m_Completed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Decoder { get { return m_Decoder; } set { m_Decoder = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map ChestMap { get { return m_Map; } set { m_Map = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation { get { return m_ChestLocation; } set { m_ChestLocation = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double LootChance { get { return CoreAI.TreasureMapDrop; } }

        private static Point2D[] m_Locations;
        public static Point2D GetRandomLocation()
        {
            if (m_Locations == null)
                LoadLocations();

            if (Core.UOAI)
            {
                if (m_Locations.Length > 0)
                {
                    for (int tries = 0; tries < 1000; tries++)
                    {   // Utility.BritWrap contains T2A and dungeons and is not compatible with treasure maps
                        Point2D px2 = m_Locations[Utility.Random(m_Locations.Length)];
                        Point3D px3 = Spawner.GetSpawnPosition(Map.Felucca, new Point3D(px2.X, px2.Y, 0), 45, false, null);
                        if (!(Utility.BritWrap[0].Contains(new Point2D(px3.X, px3.Y)) || Utility.BritWrap[1].Contains(new Point2D(px3.X, px3.Y))))
                            Console.WriteLine("Bad treasure map location detected, discarding.");
                        else
                            return new Point2D(px3.X, px3.Y);
                    }
                }

                return Point2D.Zero;
            }
            else
            {
                if (m_Locations.Length > 0)
                    return m_Locations[Utility.Random(m_Locations.Length)];

                return Point2D.Zero;
            }
        }

        private static int LoadLocations()
        {
            string filePath = Path.Combine(Core.BaseDirectory, "Data/treasure.cfg");

            ArrayList list = new ArrayList();

            // first load up the standard OSI locations (we will still randomize around this point)
            if (File.Exists(filePath))
            {
                using (StreamReader ip = new StreamReader(filePath))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        try
                        {
                            string[] split = line.Split(' ');

                            int x = Convert.ToInt32(split[0]), y = Convert.ToInt32(split[1]);

                            list.Add(new Point2D(x, y));
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }
                }
            }

            // now load up the spawner locations
            if (Core.UOAI)
            {
                foreach (Spawner sx in SpawnerCache.Spawners)
                {
                    Spawner random = sx;
                    Region region = Server.Region.Find(random.Location, Map.Felucca);

                    // Must be running
                    if (!random.Running)
                        continue;

                    if (region != null)
                    {   // No Towns
                        if (SpawnerCache.IsTown(region.Name))
                            continue;

                        // no green acres, inside houses, etc..
                        if (SpawnerCache.IsValidRegion(random.Location, region) == false)
                            continue;
                    }

                    list.Add(new Point2D(random.X, random.Y));
                }
            }

            m_Locations = (Point2D[])list.ToArray(typeof(Point2D));

            return list.Count;
        }

        //old constructer [add treasuremap <level> <map>
        [Constructable]
        public TreasureMap(int level, Map map)
            : this(level, map, false, ChestThemeType.None)
        {
        }

        //new constructor [add treasuremap <level> <map> <theme>
        [Constructable]
        public TreasureMap(int level, Map map, ChestThemeType type)
            : this(level, map, true, type)
        {
        }

        public void Setup()
        {
            m_ChestLocation = GetRandomLocation();

            Width = 300;
            Height = 300;

            int width = 600;
            int height = 600;

            int x1 = m_ChestLocation.X - Utility.RandomMinMax(width / 4, (width / 4) * 3);
            int y1 = m_ChestLocation.Y - Utility.RandomMinMax(height / 4, (height / 4) * 3);

            if (x1 < 0)
                x1 = 0;

            if (y1 < 0)
                y1 = 0;

            int x2 = x1 + width;
            int y2 = y1 + height;

            if (x2 >= 5120)
                x2 = 5119;

            if (y2 >= 4096)
                y2 = 4095;

            x1 = x2 - width;
            y1 = y2 - height;

            Bounds = new Rectangle2D(x1, y1, width, height);

            Protected = true;

            AddWorldPin(m_ChestLocation.X, m_ChestLocation.Y);
        }

        [Constructable]
        public TreasureMap(int level, Map map, bool themed, ChestThemeType type)
        {
            m_Level = level;
            m_Map = map;
            m_Themed = themed;
            m_type = type;

            Setup();
        }


        public TreasureMap(Serial serial)
            : base(serial)
        {
        }

        public void OnBeginDig(Mobile from)
        {
            if (m_Completed)
            {
                from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
            }
            else if (from != m_Decoder)
            {
                from.SendLocalizedMessage(503016); // Only the person who decoded this map may actually dig up the treasure.
            }
            else if (!from.CanBeginAction(typeof(TreasureMap)))
            {
                from.SendLocalizedMessage(503020); // You are already digging treasure.
            }
            else
            {
                from.SendLocalizedMessage(503033); // Where do you wish to dig?
                from.Target = new DigTarget(this);
            }
        }

        public void OnNPCBeginDig(Mobile from)
        {
            TreasureMap m_TMap = this;
            Point2D loc = m_TMap.m_ChestLocation;
            int x = loc.X, y = loc.Y;
            Map map = m_TMap.m_Map;
            int z = map.GetAverageZ(x, y);

            if (from.BeginAction(typeof(TreasureMap)))
            {
                new DigTimer(from, m_TMap, new Point3D(x, y, z - 14), map, z, m_TMap.m_type).Start();
            }
        }

        private class DigTarget : Target
        {
            private TreasureMap m_Map;

            public DigTarget(TreasureMap map)
                : base(6, true, TargetFlags.None)
            {
                m_Map = map;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Map.Deleted)
                    return;

                if (m_Map.m_Completed)
                {
                    from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
                }
                else if (from != m_Map.m_Decoder)
                {
                    from.SendLocalizedMessage(503016); // Only the person who decoded this map may actually dig up the treasure.
                }
                else if (!from.CanBeginAction(typeof(TreasureMap)))
                {
                    from.SendLocalizedMessage(503020); // You are already digging treasure.
                }
                else
                {
                    IPoint3D p = targeted as IPoint3D;

                    if (p is Item)
                        p = ((Item)p).GetWorldLocation();

                    int maxRange;
                    double skillValue = from.Skills[SkillName.Mining].Value;

                    if (skillValue >= 100.0)
                        maxRange = 4;
                    else if (skillValue >= 81.0)
                        maxRange = 3;
                    else if (skillValue >= 51.0)
                        maxRange = 2;
                    else
                        maxRange = 1;

                    Point2D loc = m_Map.m_ChestLocation;
                    int x = loc.X, y = loc.Y;
                    Map map = m_Map.m_Map;

                    if (map == from.Map && Utility.InRange(new Point3D(p), new Point3D(loc, 0), maxRange))
                    {
                        if (from.Location.X == loc.X && from.Location.Y == loc.Y)
                        {
                            from.SendLocalizedMessage(503030); // The chest can't be dug up because you are standing on top of it.
                        }
                        else if (map != null)
                        {
                            int z = map.GetAverageZ(x, y);

                            if (!map.CanFit(x, y, z, 16, CanFitFlags.checkBlocksFit | CanFitFlags.checkMobiles | CanFitFlags.requireSurface))
                            {
                                from.SendLocalizedMessage(503021); // You have found the treasure chest but something is keeping it from being dug up.
                            }
                            else if (from.BeginAction(typeof(TreasureMap)))
                            {
                                new DigTimer(from, m_Map, new Point3D(x, y, z - 14), map, z, m_Map.m_type).Start();
                            }
                            else
                            {
                                from.SendLocalizedMessage(503020); // You are already digging treasure.
                            }
                        }
                    }
                    else if (Utility.InRange(new Point3D(p), from.Location, 8)) // We're close, but not quite
                    {
                        from.SendLocalizedMessage(503032); // You dig and dig but no treasure seems to be here.
                    }
                    else
                    {
                        from.SendLocalizedMessage(503035); // You dig and dig but fail to find any treasure.
                    }
                }
            }

        }

        private class DigTimer : Timer
        {
            private Mobile m_From;
            private TreasureMap m_TreasureMap;
            private Map m_Map;
            private TreasureMapChest m_Chest;
            private int m_Count;
            private int m_Z;
            private DateTime m_NextSkillTime;
            private DateTime m_NextSpellTime;
            private DateTime m_NextActionTime;
            private DateTime m_LastMoveTime;
            private ChestThemeType type;
            private bool themed;
            public DigTimer(Mobile from, TreasureMap treasureMap, Point3D p, Map map, int z, ChestThemeType m_type)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {

                m_From = from;
                m_TreasureMap = treasureMap;
                m_Map = map;
                m_Z = z;
                type = m_type;
                themed = m_TreasureMap.m_Themed;

                if (themed == false) themed = TreasureTheme.GetIsThemed(m_TreasureMap.Level);
                m_TreasureMap.m_Themed = themed;

                if (themed == true && type == ChestThemeType.None)
                {
                    type = (ChestThemeType)TreasureTheme.GetThemeType(m_TreasureMap.Level);
                }

                m_TreasureMap.m_type = type;
                m_Chest = new TreasureMapChest(from, m_TreasureMap.m_Level, themed, type);
                m_Chest.MoveToWorld(p, map);

                m_NextSkillTime = from.NextSkillTime;
                m_NextSpellTime = from.NextSpellTime;
                m_NextActionTime = from.NextActionTime;
                m_LastMoveTime = from.LastMoveTime;
            }

            protected override void OnTick()
            {
                if (m_NextSkillTime != m_From.NextSkillTime || m_NextSpellTime != m_From.NextSpellTime || m_NextActionTime != m_From.NextActionTime)
                {
                    Stop();
                    m_From.EndAction(typeof(TreasureMap));
                    m_Chest.Delete();
                }
                else if (m_LastMoveTime != m_From.LastMoveTime)
                {
                    m_From.SendLocalizedMessage(503023); // You cannot move around while digging up treasure. You will need to start digging anew.

                    Stop();
                    m_From.EndAction(typeof(TreasureMap));
                    m_Chest.Delete();
                }
                /*else if ( !m_Map.CanFit( m_Chest.X, m_Chest.Y, m_Z, 16, true, true ) )
				{
					m_From.SendLocalizedMessage( 503024 ); // You stop digging because something is directly on top of the treasure chest.

					Stop();
					m_From.EndAction( typeof( TreasureMap ) );
					m_Chest.Delete();
				}*/
                else
                {
                    m_From.RevealingAction();

                    m_Count++;

                    m_Chest.Location = new Point3D(m_Chest.Location.X, m_Chest.Location.Y, m_Chest.Location.Z + 1);
                    m_From.Direction = m_From.GetDirectionTo(m_Chest.GetWorldLocation());

                    if (m_Count == 14)
                    {
                        Stop();
                        m_From.EndAction(typeof(TreasureMap));

                        m_TreasureMap.Completed = true;

                        // checks to see if the map is a themed map and if so gets the theme type based on level of map
                        // and sends appropriate theme message/warning

                        // checks to see if the map is a themed map and already has a theme set
                        // and sends appropriate theme message/warning
                        if (themed == true && type != ChestThemeType.None) m_From.SendMessage(TreasureTheme.GetThemeMessage(type));

                        if (m_TreasureMap.Level >= 2)
                        {
                            //generates 1 of the highest mobs for pirate or undead iob chests
                            TreasureTheme.Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, true, true);
                            //generates guardian spawn numbers based on if themed or not
                            for (int i = 0; i < TreasureTheme.GetGuardianSpawn(themed, type); ++i)
                            {
                                if (type == ChestThemeType.Undead || type == ChestThemeType.Pirate)
                                {
                                    //spawns rest of pirate or undead initial guardian spawn with out highest rank mobs appereing
                                    TreasureTheme.Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, true, false);
                                }
                                else
                                {
                                    //not pirate or undead chest spawn as per normal random guardians
                                    TreasureTheme.Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, false, false);
                                }
                            }

                            // 25% chance to spawn a a tax collector on a regular chest, 100% on themed chests
                            if (m_TreasureMap.Level > 3 && (Utility.RandomChance(25) || themed == true))
                            {
                                TaxCollector tc = new TaxCollector(m_Chest);
                                Point3D px = Spawner.GetSpawnPosition(m_Chest.Map, m_Chest.Location, 25, false, true, Spawner.SpawnFlags.SpawnFar, tc);
                                if (px != m_Chest.Location)
                                {   // got a good location
                                    tc.MoveToWorld(px, m_Chest.Map);
                                }

                                // if we get a tax collector, add a chance to get an additional rare. The chance is calc in the rare drop code
                                // this chest 'hides' another treasure
                                m_Chest.SetFlag(TreasureMapChest.iFlags.Hides, true);
                            }
                        }
                    }
                    else
                    {
                        if (m_From.Body.IsHuman && !m_From.Mounted)
                            m_From.Animate(11, 5, 1, true, false, 0);

                        new SoundTimer(m_From, 0x125 + (m_Count % 2)).Start();
                    }
                }
            }

            private class SoundTimer : Timer
            {
                private Mobile m_From;
                private int m_SoundID;

                public SoundTimer(Mobile from, int soundID)
                    : base(TimeSpan.FromSeconds(0.9))
                {
                    m_From = from;
                    m_SoundID = soundID;
                }

                protected override void OnTick()
                {
                    m_From.PlaySound(m_SoundID);
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!m_Completed && m_Decoder == null)
            {
                double midPoint = 0.0;

                switch (m_Level)
                {
                    case 1: midPoint = 27.0; break;
                    case 2: midPoint = 71.0; break;
                    case 3: midPoint = 81.0; break;
                    case 4: midPoint = 91.0; break;
                    case 5: midPoint = 100.0; break;
                }

                double minSkill = midPoint - 30.0;
                double maxSkill = midPoint + 30.0;

                if (from.Skills[SkillName.Cartography].Value < midPoint)
                {
                    from.SendLocalizedMessage(503013); // The map is too difficult to attempt to decode.
                    return;
                }

                if (from.CheckSkill(SkillName.Cartography, minSkill, maxSkill))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503019); // You successfully decode a treasure map!
                    Decoder = from;

                    from.PlaySound(0x249);
                    base.OnDoubleClick(from);

                    if (m_Level > 3)
                        UsageReport(from, string.Format("level {0} map decoded", m_Level));
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503018); // You fail to make anything of the map.
                }
            }
            else if (m_Completed)
            {
                from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
                base.OnDoubleClick(from);
            }
            else
            {
                from.SendLocalizedMessage(503017); // The treasure is marked by the red pin. Grab a shovel and go dig it up!
                base.OnDoubleClick(from);
            }
        }

        public static void UsageReport(Mobile m, string text)
        {
            // Tell staff that an a player is using this system
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TreasureMapUsageReport))
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator,
                0x482,
                String.Format("At location: {0}, {1} ", m.Location, text));
        }

        public override int LabelNumber { get { return (m_Decoder != null ? 1041516 + m_Level : 1041510 + m_Level); } }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(m_Map == Map.Felucca ? 1041502 : 1041503); // for somewhere in Felucca : for somewhere in Trammel

            if (m_Completed)
                list.Add(1041507, m_Decoder == null ? "someone" : m_Decoder.Name); // completed by ~1_val~
        }
        // changed this 
        // public override void OnSingleClick( Mobile from )
        //{
        //if ( m_Completed )
        //from.Send( new MessageLocalizedAffix( Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, String.Format( " completed by {0}", m_Decoder == null ? "someone" : m_Decoder.Name ), "" ) );
        //else if ( m_Decoder != null )
        //LabelTo( from, 1041516 + m_Level );
        //else
        //LabelTo( from, 1041522, String.Format( "#{0}\t \t#{1}", 1041510 + m_Level, m_Map == Map.Felucca ? 1041502 : 1041503 ) );
        //}
        //to this
        public override void OnSingleClick(Mobile from)
        {
            if (m_Completed)
                from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, String.Format(" completed by {0}", m_Decoder == null ? "someone" : m_Decoder.Name), ""));
            else if (m_Decoder != null)
            {   // non tattered
                // "an adeptly drawn treasure map";
                if (Core.UOAI || Core.UOREN || Core.UOMO || (Core.UOSP && PublishInfo.Publish >= 13))
                    LabelTo(from, 1041516 + m_Level);
                else
                    LabelTo(from, String.Format("a treasure map"));
            }
            else
            {   // tattered
                // "a tattered, adeptly drawn treasure map"
                if (Core.UOAI || Core.UOREN || Core.UOMO || (Core.UOSP && PublishInfo.Publish >= 13))
                {
                    LabelTo(from, 1041510 + m_Level);
                    LabelTo(from, m_Map == Map.Felucca ? 1041502 : 1041503);
                }
                else
                {
                    LabelTo(from, String.Format("a tattered treasure map", m_Level));
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write(m_Themed);
            writer.Write((int)m_type);

            writer.Write(m_Level);
            writer.Write(m_Completed);
            writer.Write(m_Decoder);
            writer.Write(m_Map);
            writer.Write(m_ChestLocation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Themed = reader.ReadBool();
                        m_type = (ChestThemeType)reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_Level = (int)reader.ReadInt();
                        m_Completed = reader.ReadBool();
                        m_Decoder = reader.ReadMobile();
                        m_Map = reader.ReadMap();
                        m_ChestLocation = reader.ReadPoint2D();

                        break;
                    }
            }
            if (version < 1)
            {
                m_Themed = false;
                m_type = ChestThemeType.None;
            }
        }
    }
}
