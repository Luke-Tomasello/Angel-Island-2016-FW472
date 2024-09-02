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

/* Scripts/Multis/Boats/BaseBoat.cs
 * ChangeLog
 *	3/16/11, Adam
 *		Add support for keyrings to HasKey()
 *	2/25/11, Adam
 *		Fix exception with Multis.BaseBoat.FindBoatAt(IPoint2D loc, Map map)
 *		The problem was incorrect enumeration of the sector multis
 *	2/3/11, Adam
 *		Have m_AIRect checks based upon Core.AngelIsland
 *  1/16/11, Pix
 *      Changes for Siege.  Closest player onboard (if nobody holding key) commands.
 *	8/25/10, Adam
 *		Add support for map based navigation. (Give tillerman a map.)
 *	3/16/10, Adam
 *		Have the tillerman give us a real message if we get to close to Angel Island
 *		"Ar, I'll not go any nearer to that Angel Island."
 *  07/02/07, plasma
 *      - Changed IsOnDeck to actually return true only for real deck tiles.
 *        This does not include the mast, ship, etc.  Fixes problem with 
 *        occasional weird tile flags using old method which was crap anyway.
 *      - Changed FindBoatAt so it doesn't call IsOnDeck...
 *        it shouldn't so this as it is to return true for any part of the boat
 *  4/03/07, Adam
 *      Replace redundant code in OnDeck and FindBoatAt with calls down to baseMulti
 *  03/13/07, plasma
 *      - Added CorpseHasKey( Mobile m ) and changed OnSpeech() logic,
 *          So there now HAS to be a key present to command the TillerMan.
 *          The key can be on a corpse and the ghost can command if the key remains.
 *      - Changed dry dock process to ignore dead mobiles on deck in CheckDryDock and kick them using new Stranded code instead!
 *      - Tweaks to some support functions
 *  03/12/07, plasma
 *      Added the following support methods for naval system:
 *          - FindBoatAt( Mobile m )
 *          - FindBoatAt( Item item )
 *          - FindBoatAt( Point3D loc, Map map, int height )
 *          - IsOnDeck( Mobile m )
 *          - IsOnDeck( Item i )
 *          - IsOnDeck( Point3D loc, int height )
 *          - GetMobilesOnDeck()
 *          - FindSpawnLoactionOnDeck()
 *          - DropFitResult( Point3D loc, Map map, int height )
 *  11/17/06, Adam
 *      Fix unreachable code error
 *	5/19/05, erlein
 *		- Time of decay on staff boats is ignored in CheckDecay() function +
 *		 reset to now when non staff boat
 *		- Disallowed naming of staff boats by non staff
 *	5/18/05, erlein
 *		Added a new bool to determine if this is a GM or > controlled boat.
 *	6/29/04, Pixie
 *		Fixed who can command the tillerman.
 *	6/9/04, Pixie
 *		Made ship keys regular loottype, not newbied.
 *	5/19/04, mith
 *		Added DecayState string, calculates state of decay and returns applicable string:
 *			"This structure is...
 *				like new."
 *				slightly worn."
 *				somewhat worn."
 *				fairly worn."
 *				greatly worn."
 *				in danger of collapsing."
 *		Modified DecayTime from 9 days to 7.
 *	5/16/04, mith
 *		Modified CanFit() so that arrows/bolts don't block boat movement, however, when they are run over
 *			they are considered to be "on the boat" and must be moved before it can be drydocked.
 *	3/26/04 changes by mith
 *		Decreased the size of m_AIRect to coincide with decreased region size.
 *		Boats are blocked up to 10 tiles out from the region boundary.
 *	3/21/04 changes by mith
 *		BaseBoat class variables: added m_AIRect to define Angel Island's perimeter.
 *		IsValidLocation(): Added check to prevent people within Angel Island permieter from placing a boat.
 *		Move(): Added check after new coordinates are calculated, to determine if next move puts
 *			us within the Angel Island perimeter. If so, movement is cancelled and message given.
 */

using Server.Items;
using Server.Misc;
using Server.Targeting;
using System;
using System.Collections;


namespace Server.Multis
{
    public enum BoatOrder
    {
        Move,
        Course,
        Single
    }

    public abstract class BaseBoat : BaseMulti
    {
        private static Rectangle2D[] m_IlshWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 2304 - 32, 1600 - 32) };
        private static Rectangle2D[] m_TokunoWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 1448 - 32, 1448 - 32) };

        // Rectangle to define Angel Island boundaries.
        // used to define where boats can be placed, and where they can sail.
        private static Rectangle2D m_AIRect = new Rectangle2D(140, 690, 270, 180);

        private static TimeSpan BoatDecayDelay = TimeSpan.FromDays(7.0);

        private Hold m_Hold;
        private TillerMan m_TillerMan;
        private Mobile m_Owner;

        private Direction m_Facing;

        private Direction m_Moving;
        private int m_Speed;

        private bool m_Anchored;
        private string m_ShipName;

        private BoatOrder m_Order;

        private MapItem m_MapItem;
        private int m_NextNavPoint;

        private Plank m_PPlank, m_SPlank;

        private DateTime m_DecayTime;

        private Timer m_TurnTimer;
        private Timer m_MoveTimer;

        private bool m_StaffBoat;

        private GoodFishingTimer m_GoodFishingTimer = null;

        public GoodFishingTimer Fishing
        {
            get
            {
                return m_GoodFishingTimer;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GoodFishing
        {
            get
            {
                return m_GoodFishingTimer != null && m_GoodFishingTimer.Running == true;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StaffBoat
        {
            get
            {
                return m_StaffBoat;
            }
            set
            {

                if (!value)
                    m_DecayTime = DateTime.UtcNow;

                m_StaffBoat = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Hold Hold { get { return m_Hold; } set { m_Hold = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TillerMan TillerMan { get { return m_TillerMan; } set { m_TillerMan = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Plank PPlank { get { return m_PPlank; } set { m_PPlank = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Plank SPlank { get { return m_SPlank; } set { m_SPlank = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Facing { get { return m_Facing; } set { SetFacing(value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Moving { get { return m_Moving; } set { m_Moving = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsMoving { get { return (m_MoveTimer != null); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Speed { get { return m_Speed; } set { m_Speed = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Anchored { get { return m_Anchored; } set { m_Anchored = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShipName { get { return m_ShipName; } set { m_ShipName = value; if (m_TillerMan != null) m_TillerMan.InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BoatOrder Order { get { return m_Order; } set { m_Order = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public MapItem MapItem { get { return m_MapItem; } set { m_MapItem = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NextNavPoint { get { return m_NextNavPoint; } set { m_NextNavPoint = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeOfDecay { get { return m_DecayTime; } set { m_DecayTime = value; } }

        public virtual int NorthID { get { return 0; } }
        public virtual int EastID { get { return 0; } }
        public virtual int SouthID { get { return 0; } }
        public virtual int WestID { get { return 0; } }

        public virtual int HoldDistance { get { return 0; } }
        public virtual int TillerManDistance { get { return 0; } }
        public virtual Point2D StarboardOffset { get { return Point2D.Zero; } }
        public virtual Point2D PortOffset { get { return Point2D.Zero; } }
        public virtual Point3D MarkOffset { get { return Point3D.Zero; } }

        public virtual BaseDockedBoat DockedBoat { get { return null; } }

        public BaseBoat()
            : base(0x4000)
        {
            m_DecayTime = DateTime.UtcNow + BoatDecayDelay;

            m_TillerMan = new TillerMan(this);
            m_Hold = new Hold(this);

            m_PPlank = new Plank(this, PlankSide.Port, 0);
            m_SPlank = new Plank(this, PlankSide.Starboard, 0);

            m_PPlank.MoveToWorld(new Point3D(X + PortOffset.X, Y + PortOffset.Y, Z), Map);
            m_SPlank.MoveToWorld(new Point3D(X + StarboardOffset.X, Y + StarboardOffset.Y, Z), Map);

            Facing = Direction.North;

            Movable = false;
            m_StaffBoat = false;
        }

        public BaseBoat(Serial serial)
            : base(serial)
        {
        }

        public Point3D GetRotatedLocation(int x, int y)
        {
            Point3D p = new Point3D(X + x, Y + y, Z);

            return Rotate(p, (int)m_Facing / 2);
        }

        public void UpdateComponents()
        {
            if (m_PPlank != null)
            {
                m_PPlank.MoveToWorld(GetRotatedLocation(PortOffset.X, PortOffset.Y), Map);
                m_PPlank.SetFacing(m_Facing);
            }

            if (m_SPlank != null)
            {
                m_SPlank.MoveToWorld(GetRotatedLocation(StarboardOffset.X, StarboardOffset.Y), Map);
                m_SPlank.SetFacing(m_Facing);
            }

            int xOffset = 0, yOffset = 0;
            Movement.Movement.Offset(m_Facing, ref xOffset, ref yOffset);

            if (m_TillerMan != null)
            {
                m_TillerMan.Location = new Point3D(X + (xOffset * TillerManDistance) + (m_Facing == Direction.North ? 1 : 0), Y + (yOffset * TillerManDistance), m_TillerMan.Z);
                m_TillerMan.SetFacing(m_Facing);
            }

            if (m_Hold != null)
            {
                m_Hold.Location = new Point3D(X + (xOffset * HoldDistance), Y + (yOffset * HoldDistance), m_Hold.Z);
                m_Hold.SetFacing(m_Facing);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4);

            // version 4
            writer.Write((Item)m_MapItem);
            writer.Write((int)m_NextNavPoint);

            // version 3
            writer.Write(m_StaffBoat);
            writer.Write((int)m_Facing);

            writer.WriteDeltaTime(m_DecayTime);

            writer.Write(m_Owner);
            writer.Write(m_PPlank);
            writer.Write(m_SPlank);
            writer.Write(m_TillerMan);
            writer.Write(m_Hold);
            writer.Write(m_Anchored);
            writer.Write(m_ShipName);

            CheckDecay();
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_MapItem = (MapItem)reader.ReadItem();
                        m_NextNavPoint = reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                    {
                        m_StaffBoat = reader.ReadBool();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Facing = (Direction)reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_DecayTime = reader.ReadDeltaTime();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                        {
                            m_StaffBoat = false;
                        }

                        if (version < 2)
                        {
                            if (ItemID == NorthID)
                                m_Facing = Direction.North;
                            else if (ItemID == SouthID)
                                m_Facing = Direction.South;
                            else if (ItemID == EastID)
                                m_Facing = Direction.East;
                            else if (ItemID == WestID)
                                m_Facing = Direction.West;
                        }

                        m_Owner = reader.ReadMobile();
                        m_PPlank = reader.ReadItem() as Plank;
                        m_SPlank = reader.ReadItem() as Plank;
                        m_TillerMan = reader.ReadItem() as TillerMan;
                        m_Hold = reader.ReadItem() as Hold;
                        m_Anchored = reader.ReadBool();
                        m_ShipName = reader.ReadString();

                        if (version < 1)
                            Refresh();

                        break;
                    }
            }
        }

        public void RemoveKeys(Mobile m)
        {
            uint keyValue = 0;

            if (m_PPlank != null)
                keyValue = m_PPlank.KeyValue;

            if (keyValue == 0 && m_SPlank != null)
                keyValue = m_SPlank.KeyValue;

            Key.RemoveKeys(m, keyValue);
        }

        public uint CreateKeys(Mobile m)
        {
            uint value = Key.RandomValue();

            Key packKey = new Key(KeyType.Gold, value, this);
            Key bankKey = new Key(KeyType.Gold, value, this);

            packKey.MaxRange = 10;
            bankKey.MaxRange = 10;

            //packKey.LootType = LootType.Newbied;
            //bankKey.LootType = LootType.Newbied;

            BankBox box = m.BankBox;

            if (box == null || !box.TryDropItem(m, bankKey, false))
                bankKey.Delete();
            else
                m.SendLocalizedMessage(502484); // A ship's key is now in my safety deposit box.

            if (m.AddToBackpack(packKey))
                m.SendLocalizedMessage(502485); // A ship's key is now in my backpack.
            else
                m.SendLocalizedMessage(502483); // A ship's key is now at my feet.

            return value;
        }

        public override void OnAfterDelete()
        {
            if (m_TillerMan != null)
                m_TillerMan.Delete();

            if (m_Hold != null)
                m_Hold.Delete();

            if (m_PPlank != null)
                m_PPlank.Delete();

            if (m_SPlank != null)
                m_SPlank.Delete();

            if (m_TurnTimer != null)
                m_TurnTimer.Stop();

            if (m_MoveTimer != null)
                m_MoveTimer.Stop();
        }

        public override void OnLocationChange(Point3D old)
        {
            if (m_TillerMan != null)
                m_TillerMan.Location = new Point3D(X + (m_TillerMan.X - old.X), Y + (m_TillerMan.Y - old.Y), Z + (m_TillerMan.Z - old.Z));

            if (m_Hold != null)
                m_Hold.Location = new Point3D(X + (m_Hold.X - old.X), Y + (m_Hold.Y - old.Y), Z + (m_Hold.Z - old.Z));

            if (m_PPlank != null)
                m_PPlank.Location = new Point3D(X + (m_PPlank.X - old.X), Y + (m_PPlank.Y - old.Y), Z + (m_PPlank.Z - old.Z));

            if (m_SPlank != null)
                m_SPlank.Location = new Point3D(X + (m_SPlank.X - old.X), Y + (m_SPlank.Y - old.Y), Z + (m_SPlank.Z - old.Z));
        }

        public override void OnMapChange()
        {
            if (m_TillerMan != null)
                m_TillerMan.Map = Map;

            if (m_Hold != null)
                m_Hold.Map = Map;

            if (m_PPlank != null)
                m_PPlank.Map = Map;

            if (m_SPlank != null)
                m_SPlank.Map = Map;
        }

        public bool HasKey(Mobile m)
        {
            bool hasKey = false;

            Container pack = m.Backpack;

            if (pack != null)
            {
                {
                    Item[] items = pack.FindItemsByType(typeof(Key));

                    for (int i = 0; !hasKey && i < items.Length; ++i)
                    {
                        Key key = items[i] as Key;

                        if (key != null && ((m_SPlank != null && key.KeyValue == m_SPlank.KeyValue) || (m_PPlank != null && key.KeyValue == m_PPlank.KeyValue)))
                            hasKey = true;
                    }
                }

                if (hasKey == false)
                {
                    Item[] items = pack.FindItemsByType(typeof(KeyRing));

                    for (int i = 0; !hasKey && i < items.Length; ++i)
                    {
                        KeyRing keyring = items[i] as KeyRing;

                        if (keyring != null && keyring.IsKeyOnRing(m_SPlank.KeyValue) || keyring.IsKeyOnRing(m_PPlank.KeyValue))
                            hasKey = true;
                    }
                }
            }

            return hasKey;
        }


        private bool NobodyOnboardHasKey()
        {
            bool nokey = true;

            MultiComponentList mcl = Components;

            IPooledEnumerable eable = Map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (object o in eable)
            {
                if (o is Mobile && Contains((Mobile)o))
                {
                    if (HasKey((Mobile)o))
                    {
                        nokey = false;
                        break;
                    }
                }
            }

            eable.Free();

            return nokey;
        }


        // 03/12/07, plasma
        //  Added Naval Battle support functions:
        //---------------------------------------------------------------------------

        /// <summary>
        /// Checks if a given mobile is on the deck of this boat
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool IsOnDeck(Mobile m)
        {
            if (Deleted || m == null || m.Deleted || m.Map != this.Map)
                return false;
            return IsOnDeck(m.Location);
        }

        /// <summary>
        /// Checks if a given item is on the deck of this boat
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsOnDeck(Item item)
        {
            if (Deleted || item == null || item.Deleted || item.Map != this.Map)
                return false;
            return IsOnDeck(item.Location);
        }

        /// <summary>
        /// Checks if a given location is on the deck of this boat
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool IsOnDeck(Point3D loc)
        {
            if (Deleted)
                return false;

            ArrayList list = Map.GetTilesAt(new Point2D(loc.X, loc.Y), false, false, true);

            foreach (Tile t in list)
            {
                StaticTarget st = new StaticTarget(loc, t.ID & 0x3FFF);
                if (st == null) continue;
                if (st.Name == "deck")
                    return Contains(loc);
            }
            //also check for hold item as it has no deck tile underneath it
            IPooledEnumerable eable = Map.GetObjectsInRange(loc, 0);
            foreach (object o in eable)
                if (o is Hold)
                    return Contains(loc); //call this incase its on another boat

            return false;

        }

        /// <summary>
        /// Returns the boat at a Mobile's location
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static BaseBoat FindBoatAt(Mobile m)
        {
            if (m == null || m.Deleted)
                return null;
            return FindBoatAt(m.Location, m.Map, 16);
        }

        /// <summary>
        /// Returns the boat at an Item's location
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static BaseBoat FindBoatAt(Item item)
        {
            if (item == null || item.Deleted)
                return null;
            return FindBoatAt(item.GetWorldLocation(), item.Map, item.ItemData.Height);
        }

        /// <summary>
        /// Returns the boat at a given location, or null
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="map"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static BaseBoat FindBoatAt(Point3D loc, Map map, int height)
        {
            if (map == null || map == Map.Internal)
                return null;

            // call basemulti to locate all multis at this location
            ArrayList arr = FindAll(loc, map);

            foreach (BaseMulti mx in arr)
            {
                BaseBoat boat = mx as BaseBoat;
                //Check if we found a valid boat and the location is within it
                if (boat != null && !boat.Deleted && boat.Contains(loc))
                    return boat;
            }

            return null;
        }

        public static BaseBoat FindBoatAt(IPoint2D loc, Map map)
        {
            Sector sector = map.GetSector(loc);
            if (sector != null && sector.Multis != null)
            {
                foreach (object o in sector.Multis.Values)
                {
                    BaseBoat boat = o as BaseBoat;
                    if (boat != null && boat.Contains(loc.X, loc.Y))
                        return boat;
                }
            }
            return null;
        }

        /// <summary>
        /// Retuns ArrayList of Moblies currently on the deck
        /// </summary>
        /// <returns></returns>
        public ArrayList GetMobilesOnDeck()
        {
            //Results array
            ArrayList results = new ArrayList();

            //Grab copy of the multi component list
            MultiComponentList mcl = Components;
            if (mcl == null)
                return results;

            //Grab all objects within bounds
            IPooledEnumerable eable = Map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            //Check if object is a Mobile and on deck
            foreach (object o in eable)
                if (o is Mobile && IsOnDeck((Mobile)o))
                    results.Add((Mobile)o);

            //Free up data
            eable.Free();
            eable = null;

            //Return ArrayList
            return results;
        }

        /// <summary>
        /// Returns a random spawn point on the deck!
        /// </summary>
        /// <returns></returns>
        public Point3D FindSpawnLocationOnDeck()
        {
            //Note: there will always be at least one location to spawn upon the deck,
            //Unless the players have used the overload bug to block the tillerman,
            //and every other tile on the ship (inlcuding where the players are standing)
            //In which case they don't stand much chance anyway! (The tillerman won't respond if he is blocked)

            //Set initial default return value 
            Point3D result = new Point3D();
            if (Deleted || Map == null || Map == Map.Internal)
                return result;

            //Grab copy of the multi component list
            MultiComponentList mcl = Components;
            if (mcl == null)
                return result;

            //Setup array of possible locations
            ArrayList locations = new ArrayList();

            //Add all possible spawn locations to array
            for (int x = 0; x < mcl.Width; x++)
                for (int y = 0; y < mcl.Height; y++)
                    locations.Add(new Point3D(X + mcl.Min.X + x, Y + mcl.Min.Y + y, Z + 3));//Z+3 brings us onto the deck

            //Now pick randomly & remove until a spawn point is found
            while (locations.Count > 0)
            {
                int rnd = Utility.Random(locations.Count - 1);
                if (Map.CanSpawnMobile((Point3D)locations[rnd], Server.CanFitFlags.requireSurface | Server.CanFitFlags.ignoreDeadMobiles))
                    return (Point3D)locations[rnd];
                else
                    locations.RemoveAt(rnd);
            }

            return result;
        }

        /// <summary>
        /// Returns false if specified location is next to the TillerMan.
        /// Used to prevent dropping items.
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        public static bool DropFitResult(Point3D loc, Map map, int height)
        {
            if (map == null || map == Map.Internal)
                return true;

            //Establish if there is a boat here
            BaseBoat b = FindBoatAt(loc, map, height);
            if (b == null)
                //Didn't even find a boat. Return true
                return true;

            //Calculate if this spot is next to the TillerMan
            //Evidently the Boat's direction is always set to North regardless. 
            //So here we just check all the four possible locations..
            if ((loc.X == b.TillerMan.X && loc.Y == b.TillerMan.Y + 1)          //East
                || (loc.X == b.TillerMan.X - 1 && loc.Y == b.TillerMan.Y)       //South
                || (loc.X == b.TillerMan.X - 1 && loc.Y == b.TillerMan.Y - 1)    //West
                || (loc.X == b.TillerMan.X + 1 && loc.Y == b.TillerMan.Y))      //North
                return false;

            //Didn't find a adjacent TillerMan spot - return true
            return true;
        }
        /// <summary>
        /// Checks if the mobile's corpse(s) on deck have the boat key
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool CorpseHasKey(Mobile m)
        {  //-- Pla
            bool CorpseHasKey = false;

            if (m == null)
                return false;

            //Grab copy of the multi component list
            MultiComponentList mcl = Components;
            if (mcl == null)
                return false;

            //Grab all mobiles within mcl bounds
            IPooledEnumerable eable = Map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));
            foreach (Object o in eable)
            {
                if (!(o is Corpse))
                    continue;

                Corpse c = (Corpse)o;
                //blah
                if (c == null || m_PPlank == null || m_SPlank == null)
                    continue;

                //Iterate through keys and see if we have a match
                Item[] items = c.FindItemsByType(typeof(Key));
                for (int i = 0; i < items.Length; ++i)
                {
                    Key key = items[i] as Key;
                    if (key != null && (key.KeyValue == m_SPlank.KeyValue || key.KeyValue == m_PPlank.KeyValue) && c.Owner == m)
                    {
                        CorpseHasKey = true;
                        break;
                    }
                }
            }

            eable.Free();
            eable = null;  //mark for GC

            return CorpseHasKey;
        }
        //End naval
        //---------------------------------------------------------------------------


        public bool CanCommand(Mobile m)
        {
            //erl: new property to limit vessel command to GM+
            if (StaffBoat)
                if (m.AccessLevel >= AccessLevel.GameMaster)
                    return true;
                else
                    return false;

            //pla: Commander must be on this boat to command it, dead or alive!
            // - this prevents the ship next to you trying to respond to your commands (most annoying)
            if (FindBoatAt(m) != this)
                return false;

            //pla: If the TillerMan is blocked, no one commands!
            IPooledEnumerable eable = TillerMan.GetItemsInRange(1);
            foreach (Object o in eable)
            {
                if (o is Item && ((Item)o).Movable)
                {
                    if (Utility.RandomBool())
                        this.TillerMan.PublicOverheadMessage(0, 0x3B2, false, "Arr! Clear the deck mateys!");
                    else
                        this.TillerMan.PublicOverheadMessage(0, 0x3B2, false, "Arr! I be needin' more room!");

                    eable.Free();
                    eable = null;   //mark for the gc
                    return false;
                }
            }
            eable.Free();
            eable = null; //mark for the gc            

            //2010.11.26 - separate out into AI && UOSP for ease of coding/understanding
            if (Core.UOSP || Core.UOMO)
            {
                if (HasKey(m)) //if the mobile has the key, they can command!
                {
                    return true;
                }
                else
                {
                    if (NobodyOnboardHasKey()) //if nobody has key, then the closest player on board commands
                    {
                        if (this.TillerMan.IsClosestPlayer(m))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                //Keyholders can always command
                if (HasKey(m))
                {
                    return true;
                }
                else if (CorpseHasKey(m))
                {
                    //pla: Only a key holder or a corpse of him with the key can command
                    return true;
                }
                else
                {
                    if (NobodyOnboardHasKey())
                    {
                        if (this.TillerMan.IsClosestPlayer(m))
                        {
                            //pla: Changed to display RP message and disallow command
                            switch (Utility.Random(3))
                            {
                                case 0:
                                    {
                                        this.TillerMan.PublicOverheadMessage(0, 0x3B2, false, "Ar! Ye not be my Cap'n!");
                                        break;
                                    }
                                case 1:
                                    {
                                        this.TillerMan.PublicOverheadMessage(0, 0x3B2, false, "Ar! I not be taking orders from ye, matey!");
                                        break;
                                    }
                                case 2:
                                    {
                                        this.TillerMan.PublicOverheadMessage(0, 0x3B2, false, "Ar! I not be listenin' to ye, scurvy deckhand!");
                                        break;
                                    }
                            }

                            return false;
                        }
                    }
                }
            }

            return false;
        }

        public Point3D GetMarkedLocation()
        {
            Point3D p = new Point3D(X + MarkOffset.X, Y + MarkOffset.Y, Z + MarkOffset.Z);

            return Rotate(p, (int)m_Facing / 2);
        }

        public bool CheckKey(uint keyValue)
        {
            if (m_SPlank != null && m_SPlank.KeyValue == keyValue)
                return true;

            if (m_PPlank != null && m_PPlank.KeyValue == keyValue)
                return true;

            return false;
        }

        private static TimeSpan SlowInterval = TimeSpan.FromSeconds(0.75);
        private static TimeSpan FastInterval = TimeSpan.FromSeconds(0.75);

        private static int SlowSpeed = 1;
        private static int FastSpeed = 3;

        private static TimeSpan SlowDriftInterval = TimeSpan.FromSeconds(1.50);
        private static TimeSpan FastDriftInterval = TimeSpan.FromSeconds(0.75);

        private static int SlowDriftSpeed = 1;
        private static int FastDriftSpeed = 1;

        private static Direction Forward = Direction.North;
        private static Direction ForwardLeft = Direction.Up;
        private static Direction ForwardRight = Direction.Right;
        private static Direction Backward = Direction.South;
        private static Direction BackwardLeft = Direction.Left;
        private static Direction BackwardRight = Direction.Down;
        private static Direction Left = Direction.West;
        private static Direction Right = Direction.East;
        private static Direction Port = Left;
        private static Direction Starboard = Right;

        private bool m_Decaying;

        public void Refresh()
        {
            if (!StaffBoat)
                m_DecayTime = DateTime.UtcNow + BoatDecayDelay;
        }

        public string DecayState()
        {
            TimeSpan decay = m_DecayTime - DateTime.UtcNow;

            if (decay <= TimeSpan.FromHours(24.0))
                return "This structure is in danger of collapsing.";
            else if (decay <= TimeSpan.FromHours(60.0))
                return "This structure is greatly worn.";
            else if (decay <= TimeSpan.FromHours(96.0))
                return "This structure is fairly worn.";
            else if (decay <= TimeSpan.FromHours(132.0))
                return "This structure is somewhat worn.";
            else if (decay <= TimeSpan.FromHours(167.0))
                return "This structure is slightly worn.";
            else if (decay <= TimeSpan.FromHours(168.0))
                return "This structure is like new.";
            else
                return "";
        }

        private class DecayTimer : Timer
        {
            private BaseBoat m_Boat;
            private int m_Count;

            public DecayTimer(BaseBoat boat)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(5.0))
            {
                m_Boat = boat;
            }

            protected override void OnTick()
            {
                if (m_Count == 5)
                {
                    m_Boat.Delete();
                    Stop();
                }
                else
                {
                    m_Boat.Location = new Point3D(m_Boat.X, m_Boat.Y, m_Boat.Z - 1);

                    if (m_Boat.TillerMan != null)
                        m_Boat.TillerMan.Say(1007168 + m_Count);

                    ++m_Count;
                }
            }
        }

        public bool CheckDecay()
        {
            if (m_StaffBoat)
                return false;

            if (m_Decaying)
                return true;

            if (!IsMoving && DateTime.UtcNow >= m_DecayTime)
            {
                new DecayTimer(this).Start();

                m_Decaying = true;

                return true;
            }

            return false;
        }

        public bool LowerAnchor(bool message)
        {
            if (CheckDecay())
                return false;

            if (m_Anchored)
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501445); // Ar, the anchor was already dropped sir.

                return false;
            }

            StopMove(false);

            m_Anchored = true;

            if (message && m_TillerMan != null)
                m_TillerMan.Say(501444); // Ar, anchor dropped sir.

            return true;
        }

        public bool RaiseAnchor(bool message)
        {
            if (CheckDecay())
                return false;

            if (!m_Anchored)
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501447); // Ar, the anchor has not been dropped sir.

                return false;
            }

            m_Anchored = false;

            if (message && m_TillerMan != null)
                m_TillerMan.Say(501446); // Ar, anchor raised sir.

            return true;
        }

        public bool StartMove(Direction dir, bool fast)
        {
            if (CheckDecay())
                return false;

            bool drift = (dir != Forward && dir != ForwardLeft && dir != ForwardRight);
            TimeSpan interval = (fast ? (drift ? FastDriftInterval : FastInterval) : (drift ? SlowDriftInterval : SlowInterval));
            int speed = (fast ? (drift ? FastDriftSpeed : FastSpeed) : (drift ? SlowDriftSpeed : SlowSpeed));

            if (StartMove(dir, speed, interval, false, true))
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(501429); // Aye aye sir.

                return true;
            }

            return false;
        }

        public bool OneMove(Direction dir)
        {
            if (CheckDecay())
                return false;

            bool drift = (dir != Forward);
            TimeSpan interval = drift ? FastDriftInterval : FastInterval;
            int speed = drift ? FastDriftSpeed : FastSpeed;

            if (StartMove(dir, speed, interval, true, true))
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(501429); // Aye aye sir.

                return true;
            }

            return false;
        }

        public void BeginRename(Mobile from)
        {
            if (CheckDecay())
                return;

            if (from.AccessLevel < AccessLevel.GameMaster && from != m_Owner ||
                (from.AccessLevel < AccessLevel.GameMaster && StaffBoat))
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(Utility.Random(1042876, 4)); // Arr, don't do that! | Arr, leave me alone! | Arr, watch what thour'rt doing, matey! | Arr! Do that again and I�ll throw ye overhead!

                return;
            }

            if (m_TillerMan != null)
                m_TillerMan.Say(502580); // What dost thou wish to name thy ship?

            from.Prompt = new RenameBoatPrompt(this);

        }

        public void EndRename(Mobile from, string newName)
        {
            if (Deleted || CheckDecay())
                return;

            if (from.AccessLevel < AccessLevel.GameMaster && from != m_Owner)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(1042880); // Arr! Only the owner of the ship may change its name!

                return;
            }
            else if (!from.Alive)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(502582); // You appear to be dead.

                return;
            }

            newName = newName.Trim();

            if (newName.Length == 0)
                newName = null;

            Rename(newName);
        }

        public enum DryDockResult { Valid, Dead, NoKey, NotAnchored, Mobiles, Items, Hold, Decaying }

        public DryDockResult CheckDryDock(Mobile from)
        {
            if (CheckDecay())
                return DryDockResult.Decaying;

            if (!from.Alive)
                return DryDockResult.Dead;

            bool hasKey = false;

            Container pack = from.Backpack;

            if (pack != null)
            {
                Item[] items = pack.FindItemsByType(typeof(Key));

                for (int i = 0; !hasKey && i < items.Length; ++i)
                {
                    Key key = items[i] as Key;

                    if (key != null && ((m_SPlank != null && key.KeyValue == m_SPlank.KeyValue) || (m_PPlank != null && key.KeyValue == m_PPlank.KeyValue)))
                        hasKey = true;
                }
            }

            if (!hasKey)
                return DryDockResult.NoKey;

            if (!m_Anchored)
                return DryDockResult.NotAnchored;

            if (m_Hold != null && m_Hold.Items.Count > 0)
                return DryDockResult.Hold;

            Map map = Map;

            if (map == null || map == Map.Internal)
                return DryDockResult.Items;

            MultiComponentList mcl = Components;

            IPooledEnumerable eable = map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (object o in eable)
            {
                if (o == this || o == m_Hold || o == m_SPlank || o == m_PPlank || o == m_TillerMan)
                    continue;

                if (o is Item && Contains((Item)o))
                {
                    eable.Free();
                    return DryDockResult.Items;
                }
                //pla: modified this to ignore dead mobiles. These are now booted on Dry Dock for Naval battles.
                else if (o is Mobile && Contains((Mobile)o) && ((Mobile)o).AccessLevel <= AccessLevel.Player && ((Mobile)o).Alive && !((Mobile)o).IsDeadBondedPet)
                {
                    eable.Free();
                    return DryDockResult.Mobiles;
                }
            }

            eable.Free();
            return DryDockResult.Valid;
        }

        public void BeginDryDock(Mobile from)
        {
            if (CheckDecay())
                return;

            DryDockResult result = CheckDryDock(from);

            if (result == DryDockResult.Dead)
                from.SendLocalizedMessage(502493); // You appear to be dead.
            else if (result == DryDockResult.NoKey)
                from.SendLocalizedMessage(502494); // You must have a key to the ship to dock the boat.
            else if (result == DryDockResult.NotAnchored)
                from.SendLocalizedMessage(1010570); // You must lower the anchor to dock the boat.
            else if (result == DryDockResult.Mobiles)
                from.SendLocalizedMessage(502495); // You cannot dock the ship with beings on board!
            else if (result == DryDockResult.Items)
                from.SendLocalizedMessage(502496); // You cannot dock the ship with a cluttered deck.
            else if (result == DryDockResult.Hold)
                from.SendLocalizedMessage(502497); // Make sure your hold is empty, and try again!
            else if (result == DryDockResult.Valid)
                from.SendGump(new ConfirmDryDockGump(from, this));
        }

        public void EndDryDock(Mobile from)
        {
            if (Deleted || CheckDecay())
                return;

            DryDockResult result = CheckDryDock(from);

            if (result == DryDockResult.Dead)
                from.SendLocalizedMessage(502493); // You appear to be dead.
            else if (result == DryDockResult.NoKey)
                from.SendLocalizedMessage(502494); // You must have a key to the ship to dock the boat.
            else if (result == DryDockResult.NotAnchored)
                from.SendLocalizedMessage(1010570); // You must lower the anchor to dock the boat.
            else if (result == DryDockResult.Mobiles)
                from.SendLocalizedMessage(502495); // You cannot dock the ship with beings on board!
            else if (result == DryDockResult.Items)
                from.SendLocalizedMessage(502496); // You cannot dock the ship with a cluttered deck.
            else if (result == DryDockResult.Hold)
                from.SendLocalizedMessage(502497); // Make sure your hold is empty, and try again!

            if (result != DryDockResult.Valid)
                return;

            //pla: Boot any dead mobiles from the deck!
            foreach (Mobile m in GetMobilesOnDeck())
                Strandedness.ProcessStranded(m, false);

            BaseDockedBoat boat = DockedBoat;
            if (boat == null)
                return;

            RemoveKeys(from);

            from.AddToBackpack(boat);
            Delete();
        }

        public void SetName(SpeechEventArgs e)
        {
            if (CheckDecay())
                return;

            if (e.Mobile.AccessLevel < AccessLevel.GameMaster && e.Mobile != m_Owner)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(1042880); // Arr! Only the owner of the ship may change its name!

                return;
            }
            else if (!e.Mobile.Alive)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(502582); // You appear to be dead.

                return;
            }

            if (e.Speech.Length > 8)
            {
                string newName = e.Speech.Substring(8).Trim();

                if (newName.Length == 0)
                    newName = null;

                Rename(newName);
            }
        }

        public void Rename(string newName)
        {
            if (CheckDecay())
                return;

            if (newName != null && newName.Length > 40)
                newName = newName.Substring(0, 40);

            if (m_ShipName == newName)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(502531); // Yes, sir.

                return;
            }

            ShipName = newName;

            if (m_TillerMan != null && m_ShipName != null)
                m_TillerMan.Say(1042885, m_ShipName); // This ship is now called the ~1_NEW_SHIP_NAME~.
            else if (m_TillerMan != null)
                m_TillerMan.Say(502534); // This ship now has no name.
        }

        public void RemoveName(Mobile m)
        {
            if (CheckDecay())
                return;

            if (m.AccessLevel < AccessLevel.GameMaster && m != m_Owner)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(1042880); // Arr! Only the owner of the ship may change its name!

                return;
            }
            else if (!m.Alive)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(502582); // You appear to be dead.

                return;
            }

            if (m_ShipName == null)
            {
                if (m_TillerMan != null)
                    m_TillerMan.Say(502526); // Ar, this ship has no name.

                return;
            }

            ShipName = null;

            if (m_TillerMan != null)
                m_TillerMan.Say(502534); // This ship now has no name.
        }

        public void GiveName(Mobile m)
        {
            if (m_TillerMan == null || CheckDecay())
                return;

            if (m_ShipName == null)
                m_TillerMan.Say(502526); // Ar, this ship has no name.
            else
                m_TillerMan.Say(1042881, m_ShipName); // This is the ~1_BOAT_NAME~.
        }

        public void GiveNavPoint()
        {
            if (TillerMan == null || CheckDecay())
                return;

            if (NextNavPoint < 0)
                TillerMan.Say(1042882); // I have no current nav point.
            else
                TillerMan.Say(1042883, (NextNavPoint + 1).ToString()); // My current destination navpoint is nav ~1_NAV_POINT_NUM~.
        }

        public void AssociateMap(MapItem map)
        {
            if (CheckDecay())
                return;

            if (map is BlankMap)
            {
                if (TillerMan != null)
                    TillerMan.Say(502575); // Ar, that is not a map, tis but a blank piece of paper!
            }
            else if (map.Pins.Count == 0)
            {
                if (TillerMan != null)
                    TillerMan.Say(502576); // Arrrr, this map has no course on it!
            }
            else if (map is SeaChart && (map as SeaChart).Level > 0)
            {
                StopMove(false);

                MapItem = map;
                NextNavPoint = -1;

                if (TillerMan != null)
                {
                    switch (Utility.Random(3))
                    {
                        case 0: m_TillerMan.Say(false, "Ar, a secret!"); break;
                        case 1: m_TillerMan.Say(false, "Ho ho, I can keep a secret!"); break;
                        case 2: m_TillerMan.Say(false, "*studies map*"); break;
                    }
                    m_TillerMan.Say(false, "Ar, what be yer command cap'n?");
                }
            }
            else
            {
                StopMove(false);

                MapItem = map;
                NextNavPoint = -1;

                if (TillerMan != null)
                    TillerMan.Say(502577); // A map!
            }
        }

        public bool StartCourse(string navPoint, bool single, bool message)
        {
            int number = -1;

            int start = -1;
            for (int i = 0; i < navPoint.Length; i++)
            {
                if (Char.IsDigit(navPoint[i]))
                {
                    start = i;
                    break;
                }
            }

            if (start != -1)
            {
                string sNumber = navPoint.Substring(start);

                try
                {
                    number = Convert.ToInt32(sNumber);
                }
                catch
                {
                    number = -1;
                }

                if (number != -1)
                {
                    number--;

                    if (MapItem == null || number < 0 || number >= MapItem.Pins.Count)
                    {
                        number = -1;
                    }
                }
            }

            if (number == -1)
            {
                if (message && TillerMan != null)
                    TillerMan.Say(1042551); // I don't see that navpoint, sir.

                return false;
            }

            NextNavPoint = number;
            return StartCourse(single, message);
        }

        public bool StartCourse(bool single, bool message)
        {
            if (CheckDecay())
                return false;

            if (Anchored)
            {
                if (message && TillerMan != null)
                    TillerMan.Say(501419); // Ar, the anchor is down sir!

                return false;
            }
            else if (MapItem == null || MapItem.Deleted)
            {
                if (message && TillerMan != null)
                    TillerMan.Say(502513); // I have seen no map, sir.

                return false;
            }
            else if (this.Map != MapItem.Map || !this.Contains(MapItem.GetWorldLocation()))
            {
                if (message && TillerMan != null)
                    TillerMan.Say(502514); // The map is too far away from me, sir.

                return false;
            }
            else if ((this.Map != Map.Trammel && this.Map != Map.Felucca) || NextNavPoint < 0 || NextNavPoint >= MapItem.Pins.Count)
            {
                if (message && TillerMan != null)
                    TillerMan.Say(1042551); // I don't see that navpoint, sir.

                return false;
            }

            Speed = FastSpeed;
            Order = single ? BoatOrder.Single : BoatOrder.Course;

            if (m_MoveTimer != null)
                m_MoveTimer.Stop();

            m_MoveTimer = new MoveTimer(this, FastInterval, false);
            m_MoveTimer.Start();

            if (message && TillerMan != null)
                TillerMan.Say(501429); // Aye aye sir.

            return true;
        }

        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (CheckDecay())
                return;

            Mobile from = e.Mobile;

            if (CanCommand(from) && Contains(from))
            {
                for (int i = 0; i < e.Keywords.Length; ++i)
                {
                    int keyword = e.Keywords[i];

                    if (keyword >= 0x42 && keyword <= 0x6B)
                    {
                        switch (keyword)
                        {
                            case 0x42: SetName(e); break;
                            case 0x43: RemoveName(e.Mobile); break;
                            case 0x44: GiveName(e.Mobile); break;
                            case 0x45: StartMove(Forward, true); break;
                            case 0x46: StartMove(Backward, true); break;
                            case 0x47: StartMove(Left, true); break;
                            case 0x48: StartMove(Right, true); break;
                            case 0x4B: StartMove(ForwardLeft, true); break;
                            case 0x4C: StartMove(ForwardRight, true); break;
                            case 0x4D: StartMove(BackwardLeft, true); break;
                            case 0x4E: StartMove(BackwardRight, true); break;
                            case 0x4F: StopMove(true); break;
                            case 0x50: StartMove(Left, false); break;
                            case 0x51: StartMove(Right, false); break;
                            case 0x52: StartMove(Forward, false); break;
                            case 0x53: StartMove(Backward, false); break;
                            case 0x54: StartMove(ForwardLeft, false); break;
                            case 0x55: StartMove(ForwardRight, false); break;
                            case 0x56: StartMove(BackwardRight, false); break;
                            case 0x57: StartMove(BackwardLeft, false); break;
                            case 0x58: OneMove(Left); break;
                            case 0x59: OneMove(Right); break;
                            case 0x5A: OneMove(Forward); break;
                            case 0x5B: OneMove(Backward); break;
                            case 0x5C: OneMove(ForwardLeft); break;
                            case 0x5D: OneMove(ForwardRight); break;
                            case 0x5E: OneMove(BackwardRight); break;
                            case 0x5F: OneMove(BackwardLeft); break;
                            case 0x49:
                            case 0x65: StartTurn(2, true); break; // turn right
                            case 0x4A:
                            case 0x66: StartTurn(-2, true); break; // turn left
                            case 0x67: StartTurn(-4, true); break; // turn around, come about
                            case 0x68: StartMove(Forward, true); break;
                            case 0x69: StopMove(true); break;
                            case 0x6A: LowerAnchor(true); break;
                            case 0x6B: RaiseAnchor(true); break;
                            case 0x60: GiveNavPoint(); break; // nav
                            case 0x61: NextNavPoint = 0; StartCourse(false, true); break; // start
                            case 0x62: StartCourse(false, true); break; // continue
                            case 0x63: StartCourse(e.Speech, false, true); break; // goto*
                            case 0x64: StartCourse(e.Speech, true, true); break; // single*
                        }

                        break;
                    }
                }
            }
        }

        public bool StartTurn(int offset, bool message)
        {
            if (CheckDecay())
                return false;

            if (m_Anchored)
            {
                if (message)
                    m_TillerMan.Say(501419); // Ar, the anchor is down sir!

                return false;
            }
            else
            {
                if (m_MoveTimer != null && this.Order != BoatOrder.Move)
                {
                    m_MoveTimer.Stop();
                    m_MoveTimer = null;
                }

                if (m_TurnTimer != null)
                    m_TurnTimer.Stop();

                m_TurnTimer = new TurnTimer(this, offset);
                m_TurnTimer.Start();

                if (message && TillerMan != null)
                    TillerMan.Say(501429); // Aye aye sir.

                return true;
            }
        }

        public bool Turn(int offset, bool message)
        {
            if (m_TurnTimer != null)
            {
                m_TurnTimer.Stop();
                m_TurnTimer = null;
            }

            if (CheckDecay())
                return false;

            if (m_Anchored)
            {
                if (message)
                    m_TillerMan.Say(501419); // Ar, the anchor is down sir!

                return false;
            }
            else if (SetFacing((Direction)(((int)m_Facing + offset) & 0x7)))
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501429); // Aye aye sir.

                return true;
            }
            else
            {
                if (message)
                    m_TillerMan.Say(501423); // Ar, can't turn sir.

                return false;
            }
        }

        private class TurnTimer : Timer
        {
            private BaseBoat m_Boat;
            private int m_Offset;

            public TurnTimer(BaseBoat boat, int offset)
                : base(TimeSpan.FromSeconds(0.5))
            {
                m_Boat = boat;
                m_Offset = offset;

                Priority = TimerPriority.TenMS;
            }

            protected override void OnTick()
            {
                if (!m_Boat.Deleted)
                    m_Boat.Turn(m_Offset, true);
            }
        }

        public bool StartMove(Direction dir, int speed, TimeSpan interval, bool single, bool message)
        {
            if (CheckDecay())
                return false;

            if (m_Anchored)
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501419); // Ar, the anchor is down sir!

                return false;
            }

            m_Moving = dir;
            m_Speed = speed;
            m_Order = BoatOrder.Move;

            if (m_MoveTimer != null)
                m_MoveTimer.Stop();

            m_MoveTimer = new MoveTimer(this, interval, single);
            m_MoveTimer.Start();

            return true;
        }

        public bool StopMove(bool message)
        {
            if (CheckDecay())
                return false;

            if (m_MoveTimer == null)
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501443); // Er, the ship is not moving sir.

                return false;
            }

            m_Moving = Direction.North;
            m_Speed = 0;
            m_MoveTimer.Stop();
            m_MoveTimer = null;

            if (message && m_TillerMan != null)
                m_TillerMan.Say(501429); // Aye aye sir.

            return true;
        }

        public bool CanFit(Point3D p, Map map, int itemID)
        {
            if (map == null || map == Map.Internal || Deleted || CheckDecay())
                return false;

            MultiComponentList oldComponents = MultiData.GetComponents(ItemID);
            MultiComponentList newComponents = MultiData.GetComponents(itemID);

            for (int x = 0; x < newComponents.Width; ++x)
            {
                for (int y = 0; y < newComponents.Height; ++y)
                {
                    int tx = p.X + newComponents.Min.X + x;
                    int ty = p.Y + newComponents.Min.Y + y;

                    if (newComponents.Tiles[x][y].Length == 0 || Contains(tx, ty))
                        continue;

                    Tile landTile = map.Tiles.GetLandTile(tx, ty);
                    Tile[] tiles = map.Tiles.GetStaticTiles(tx, ty, true);

                    bool hasWater = false;

                    if (landTile.Z == p.Z && ((landTile.ID >= 168 && landTile.ID <= 171) || (landTile.ID >= 310 && landTile.ID <= 311)))
                        hasWater = true;

                    int z = p.Z;

                    int landZ = 0, landAvg = 0, landTop = 0;

                    map.GetAverageZ(tx, ty, ref landZ, ref landAvg, ref landTop);

                    //if ( !landTile.Ignored && top > landZ && landTop > z )
                    //	return false;

                    for (int i = 0; i < tiles.Length; ++i)
                    {
                        Tile tile = tiles[i];
                        bool isWater = (tile.ID >= 0x5796 && tile.ID <= 0x57B2);

                        if (tile.Z == p.Z && isWater)
                            hasWater = true;
                        else if (tile.Z >= p.Z && !isWater)
                            return false;
                    }

                    if (!hasWater)
                        return false;
                }
            }

            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(p.X + newComponents.Min.X, p.Y + newComponents.Min.Y, newComponents.Width, newComponents.Height));

            foreach (Item item in eable)
            {
                if (item.ItemID >= 0x4000 || item.Z < p.Z || !item.Visible)
                    continue;

                int x = item.X - p.X + newComponents.Min.X;
                int y = item.Y - p.Y + newComponents.Min.Y;

                if (item is Arrow || item is Bolt)
                    continue;

                if (x >= 0 && x < newComponents.Width && y >= 0 && y < newComponents.Height && newComponents.Tiles[x][y].Length == 0)
                    continue;
                else if (Contains(item))
                    continue;

                eable.Free();
                return false;
            }

            eable.Free();

            return true;
        }

        public Point3D Rotate(Point3D p, int count)
        {
            int rx = p.X - Location.X;
            int ry = p.Y - Location.Y;

            for (int i = 0; i < count; ++i)
            {
                int temp = rx;
                rx = -ry;
                ry = temp;
            }

            return new Point3D(Location.X + rx, Location.Y + ry, p.Z);
        }

        public override bool Contains(int x, int y)
        {
            if (base.Contains(x, y))
                return true;

            if (m_TillerMan != null && x == m_TillerMan.X && y == m_TillerMan.Y)
                return true;

            if (m_Hold != null && x == m_Hold.X && y == m_Hold.Y)
                return true;

            if (m_PPlank != null && x == m_PPlank.X && y == m_PPlank.Y)
                return true;

            if (m_SPlank != null && x == m_SPlank.X && y == m_SPlank.Y)
                return true;

            return false;
        }

        public static bool IsValidLocation(Point3D p, Map map)
        {
            // this is our rectangle surrounding Angel Island
            // this check prevents people from placing boats anywhere in the AngelIsland region.
            if ((Core.UOAI || Core.UOAR) && m_AIRect.Contains(p))
                return false;

            Rectangle2D[] wrap = GetWrapFor(map);

            for (int i = 0; i < wrap.Length; ++i)
            {
                if (wrap[i].Contains(p))
                    return true;
            }

            return false;
        }

        public static Rectangle2D[] GetWrapFor(Map m)
        {
            if (m == Map.Ilshenar)
                return m_IlshWrap;
            else if (m == Map.Tokuno)
                return m_TokunoWrap;
            else
                return Utility.BritWrap;
        }
        // for the generic version, see Utils.World.GetWrapFor()
        public static Rectangle2D[] GetWrapFor(Point3D loc, Map map)
        {
            return Utility.World.GetWrapFor(map);
        }
        public Direction GetMovementFor(int x, int y, out int maxSpeed)
        {
            int dx = x - this.X;
            int dy = y - this.Y;

            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);

            Direction dir = Utility.GetDirection(this, new Point2D(x, y));
            int iDir = (int)dir;

            // Compute the maximum distance we can travel without going too far away
            if (iDir % 2 == 0) // North, East, South and West
                maxSpeed = Math.Abs(adx - ady);
            else // Right, Down, Left and Up
                maxSpeed = Math.Min(adx, ady);

            return (Direction)((iDir - (int)Facing) & 0x7);
        }

        private void ProcessSeaChart()
        {
            if (MapItem == null || MapItem.Deleted)
                return;

            if (MapItem is SeaChart == false)
                return;

            if ((MapItem as SeaChart).Level == 0)
                return;

            SeaChart chart = MapItem as SeaChart;

            if (NextNavPoint < 0 || NextNavPoint > chart.Pins.Count)
                return;

            // if we have not been to this pin before, mark it as visited and start the good fishing timer.
            if (chart.PinComplete[NextNavPoint] == false)
            {
                chart.PinComplete[NextNavPoint] = true;
                if (m_GoodFishingTimer != null)
                    m_GoodFishingTimer.Stop();

                int worldX, worldY;
                chart.ConvertToWorld(((Point2D)chart.Pins[NextNavPoint]).X, ((Point2D)chart.Pins[NextNavPoint]).Y, out worldX, out worldY);
                m_GoodFishingTimer = new GoodFishingTimer(chart.Level, new Point2D(worldX, worldY));
                m_GoodFishingTimer.Start();
                m_TillerMan.Say(false, "Ar, looks to be good fishing about here sir.");
            }
            else
            {   // if the user moves the boat, then comes back to this point....
                int worldX, worldY;
                chart.ConvertToWorld(((Point2D)chart.Pins[NextNavPoint]).X, ((Point2D)chart.Pins[NextNavPoint]).Y, out worldX, out worldY);
                if (m_GoodFishingTimer != null && m_GoodFishingTimer.Running && m_GoodFishingTimer.HotSpot == new Point2D(worldX, worldY))
                    m_TillerMan.Say(false, "Ar, looks to be good fishing about here sir.");
                else
                    m_TillerMan.Say(false, "Ar, looks to be fished out.");
            }
        }

        public bool DoMovement(bool message)
        {
            Direction dir;
            int speed;

            if (this.Order == BoatOrder.Move)
            {
                dir = this.Moving;
                speed = this.Speed;
            }
            else if (MapItem == null || MapItem.Deleted)
            {
                if (message && TillerMan != null)
                    TillerMan.Say(502513); // I have seen no map, sir.

                return false;
            }
            else if (this.Map != MapItem.Map || !this.Contains(MapItem.GetWorldLocation()))
            {
                if (message && TillerMan != null)
                    TillerMan.Say(502514); // The map is too far away from me, sir.

                return false;
            }
            else if ((this.Map != Map.Trammel && this.Map != Map.Felucca) || NextNavPoint < 0 || NextNavPoint >= MapItem.Pins.Count)
            {
                if (message && TillerMan != null)
                    TillerMan.Say(1042551); // I don't see that navpoint, sir.

                return false;
            }
            else
            {
                Point2D dest = (Point2D)MapItem.Pins[NextNavPoint];

                int x, y;
                MapItem.ConvertToWorld(dest.X, dest.Y, out x, out y);

                int maxSpeed;
                dir = GetMovementFor(x, y, out maxSpeed);

                if (maxSpeed == 0)
                {
                    if (message && this.Order == BoatOrder.Single && TillerMan != null)
                    {
                        TillerMan.Say(1042874, (NextNavPoint + 1).ToString());  // We have arrived at nav point ~1_POINT_NUM~ , sir.
                        ProcessSeaChart();                                      // if the player hs using a sea chart, start fishing timer
                    }

                    if (NextNavPoint + 1 < MapItem.Pins.Count)
                    {
                        NextNavPoint++;

                        if (this.Order == BoatOrder.Course)
                        {
                            if (message && TillerMan != null)
                                TillerMan.Say(1042875, (NextNavPoint + 1).ToString()); // Heading to nav point ~1_POINT_NUM~, sir.

                            return true;
                        }

                        return false;
                    }
                    else
                    {
                        NextNavPoint = -1;

                        if (message && this.Order == BoatOrder.Course && TillerMan != null)
                            TillerMan.Say(502515); // The course is completed, sir.

                        return false;
                    }
                }

                if (dir == Left || dir == BackwardLeft || dir == Backward)
                    return Turn(-2, true);
                else if (dir == Right || dir == BackwardRight)
                    return Turn(2, true);

                speed = Math.Min(this.Speed, maxSpeed);
            }

            return Move(dir, speed, true);
        }

        public bool Move(Direction dir, int speed, bool message)
        {
            Map map = Map;

            if (map == null || Deleted || CheckDecay())
                return false;

            if (m_Anchored)
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(501419); // Ar, the anchor is down sir!

                return false;
            }

            int rx = 0, ry = 0;
            Movement.Movement.Offset((Direction)(((int)m_Facing + (int)dir) & 0x7), ref rx, ref ry);

            for (int i = 1; i <= speed; ++i)
            {
                if (!CanFit(new Point3D(X + (i * rx), Y + (i * ry), Z), Map, ItemID))
                {
                    if (message && m_TillerMan != null)
                        m_TillerMan.Say(501424); // Ar, we've stopped sir.

                    if (i == 1)
                        return false;

                    speed = i - 1;
                    break;
                }
            }

            int xOffset = speed * rx;
            int yOffset = speed * ry;

            int newX = X + xOffset;
            int newY = Y + yOffset;

            // Figure out what our new location will be as a 2D point.
            // If that point puts us within the Angel Island perimeter, cancel the movement.
            // Have tillerman tell us we've stopped.
            // Adam: have the tillerman tell us about Angel Island
            if ((Core.UOAI || Core.UOAR) && m_AIRect.Contains(new Point2D(newX, newY)))
            {
                if (message && m_TillerMan != null)
                    m_TillerMan.Say(false, "Ar, I'll not go any nearer to that Angel Island.");

                return false;
            }

            Rectangle2D[] wrap = (map == Map.Ilshenar ? m_IlshWrap : Utility.BritWrap);

            for (int i = 0; i < wrap.Length; ++i)
            {
                Rectangle2D rect = wrap[i];

                if (rect.Contains(new Point2D(X, Y)) && !rect.Contains(new Point2D(newX, newY)))
                {
                    if (newX < rect.X)
                        newX = rect.X + rect.Width - 1;
                    else if (newX >= rect.X + rect.Width)
                        newX = rect.X;

                    if (newY < rect.Y)
                        newY = rect.Y + rect.Height - 1;
                    else if (newY >= rect.Y + rect.Height)
                        newY = rect.Y;

                    for (int j = 1; j <= speed; ++j)
                    {
                        if (!CanFit(new Point3D(newX + (j * rx), newY + (j * ry), Z), Map, ItemID))
                        {
                            if (message && m_TillerMan != null)
                                m_TillerMan.Say(501424); // Ar, we've stopped sir.

                            return false;
                        }
                    }

                    xOffset = newX - X;
                    yOffset = newY - Y;
                }
            }

            MultiComponentList mcl = Components;

            ArrayList toMove = new ArrayList();

            IPooledEnumerable eable = map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (object o in eable)
            {
                if (o != this && o != m_TillerMan && o != m_Hold && o != m_SPlank && o != m_PPlank)
                    toMove.Add(o);
            }

            eable.Free();

            for (int i = 0; i < toMove.Count; ++i)
            {
                object o = toMove[i];

                if (o is Item)
                {
                    Item item = (Item)o;

                    if (Contains(item) && item.Visible && item.Z >= Z)
                        item.Location = new Point3D(item.X + xOffset, item.Y + yOffset, item.Z);
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    if (Contains(m))
                        m.Location = new Point3D(m.X + xOffset, m.Y + yOffset, m.Z);
                }
            }

            Location = new Point3D(X + xOffset, Y + yOffset, Z);

            return true;
        }

        public void Teleport(int xOffset, int yOffset, int zOffset)
        {
            MultiComponentList mcl = Components;

            ArrayList toMove = new ArrayList();

            IPooledEnumerable eable = this.Map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (object o in eable)
            {
                if (o != this && !(o is TillerMan || o is Hold || o is Plank))
                    toMove.Add(o);
            }

            eable.Free();

            for (int i = 0; i < toMove.Count; ++i)
            {
                object o = toMove[i];

                if (o is Item)
                {
                    Item item = (Item)o;

                    if (Contains(item) && item.Visible && item.Z >= Z)
                        item.Location = new Point3D(item.X + xOffset, item.Y + yOffset, item.Z + zOffset);
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    if (Contains(m))
                        m.Location = new Point3D(m.X + xOffset, m.Y + yOffset, m.Z + zOffset);
                }
            }

            Location = new Point3D(X + xOffset, Y + yOffset, Z + zOffset);
        }

        public bool SetFacing(Direction facing)
        {
            if (CheckDecay())
                return false;

            if (Map != null && Map != Map.Internal)
            {
                switch (facing)
                {
                    case Direction.North: if (!CanFit(Location, Map, NorthID)) return false; break;
                    case Direction.East: if (!CanFit(Location, Map, EastID)) return false; break;
                    case Direction.South: if (!CanFit(Location, Map, SouthID)) return false; break;
                    case Direction.West: if (!CanFit(Location, Map, WestID)) return false; break;
                }
            }

            Direction old = m_Facing;

            m_Facing = facing;

            if (m_TillerMan != null)
                m_TillerMan.SetFacing(facing);

            if (m_Hold != null)
                m_Hold.SetFacing(facing);

            if (m_PPlank != null)
                m_PPlank.SetFacing(facing);

            if (m_SPlank != null)
                m_SPlank.SetFacing(facing);

            MultiComponentList mcl = Components;

            ArrayList toMove = new ArrayList();

            toMove.Add(m_PPlank);
            toMove.Add(m_SPlank);

            IPooledEnumerable eable = Map.GetObjectsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (object o in eable)
            {
                if (o is Item)
                {
                    Item item = (Item)o;

                    if (item != m_TillerMan && item != m_Hold && item != m_PPlank && item != m_SPlank && item != this && Contains(item) && item.Visible && item.Z >= Z)
                        toMove.Add(item);
                }
                else if (o is Mobile && Contains((Mobile)o))
                {
                    toMove.Add(o);

                    ((Mobile)o).Direction = (Direction)((int)((Mobile)o).Direction - (int)old + (int)facing);
                }
            }

            eable.Free();

            int xOffset = 0, yOffset = 0;
            Movement.Movement.Offset(facing, ref xOffset, ref yOffset);

            if (m_TillerMan != null)
                m_TillerMan.Location = new Point3D(X + (xOffset * TillerManDistance) + (facing == Direction.North ? 1 : 0), Y + (yOffset * TillerManDistance), m_TillerMan.Z);

            if (m_Hold != null)
                m_Hold.Location = new Point3D(X + (xOffset * HoldDistance), Y + (yOffset * HoldDistance), m_Hold.Z);

            int count = (int)(m_Facing - old) & 0x7;
            count /= 2;

            for (int i = 0; i < toMove.Count; ++i)
            {
                object o = toMove[i];

                if (o is Item)
                    ((Item)o).Location = Rotate(((Item)o).Location, count);
                else if (o is Mobile)
                    ((Mobile)o).Location = Rotate(((Mobile)o).Location, count);
            }

            switch (facing)
            {
                case Direction.North: ItemID = NorthID; break;
                case Direction.East: ItemID = EastID; break;
                case Direction.South: ItemID = SouthID; break;
                case Direction.West: ItemID = WestID; break;
            }

            RefreshComponents();

            return true;
        }

        private class MoveTimer : Timer
        {
            private BaseBoat m_Boat;

            public MoveTimer(BaseBoat boat, TimeSpan interval, bool single)
                : base(interval, interval, single ? 1 : 0)
            {
                m_Boat = boat;
                Priority = TimerPriority.TwentyFiveMS;
            }

            protected override void OnTick()
            {
                if (!m_Boat.DoMovement(true))
                    m_Boat.StopMove(false);
            }
        }

        public class GoodFishingTimer : Timer
        {
            private int m_level;
            public int Level { get { return m_level; } }
            private Point2D m_hotSpot;
            public Point2D HotSpot { get { return m_hotSpot; } }

            public GoodFishingTimer(int level, Point2D hotSpot)
                : base(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1), 15)
            {
                m_level = level;
                m_hotSpot = hotSpot;
                Priority = TimerPriority.FiveSeconds;
            }

            public bool GoodFishingHere(Point2D point)
            {
                int xDelta = m_hotSpot.X - point.X;
                int yDelta = m_hotSpot.Y - point.Y;

                // within 4 tiles
                return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta)) < 4;
            }

            public bool GoodFishingNear(Point2D point)
            {
                int xDelta = m_hotSpot.X - point.X;
                int yDelta = m_hotSpot.Y - point.Y;

                // within 10 tiles
                return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta)) < 10;
            }
        }

        public static void UpdateAllComponents()
        {
            ArrayList list = new ArrayList();

            foreach (Item item in World.Items.Values)
            {
                if (item is BaseBoat)
                    list.Add(item);
            }

            foreach (BaseBoat boat in list)
                boat.UpdateComponents();
        }

        public static void Initialize()
        {
            new UpdateAllTimer().Start();
            EventSink.WorldSave += new WorldSaveEventHandler(EventSink_WorldSave);
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            new UpdateAllTimer().Start();
        }

        public class UpdateAllTimer : Timer
        {
            public UpdateAllTimer()
                : base(TimeSpan.FromSeconds(1.0))
            {
            }

            protected override void OnTick()
            {
                UpdateAllComponents();
            }
        }
    }
}
