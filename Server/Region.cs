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

/* Server/Region.cs
 * CHANGELOG:
 *  9/20/2024, Adam (MatchingRegions)
 *      MatchingRegions verifies all regions match between the player and the NPC (banker for instance.)
 *      A good example is standing in Wind Park, and yelling over the cave wall there to town to bank.
 *          this.Say("I will not do business with an exploiter!");
 *  9/10/2024, Adam
 *      Add FindByName()
 *	2/21/11, Adam
 *		Renamed from base region to region to better match the RunUO file structure.
 *		Please see the file Scripts/Regions/BaseRegion.cs for additional comments.
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/21/10, Adam
 *		Added IsGuarded to list of IS attributes
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 * 4/10/10, Adam
 *		Added new region overridables
 *		KeepsItemsOnDeath
 *			Yep, players keep all their loot
 *		OnAfterDeath
 *			Oppertunity to Modify 'ghost', like set frozen which doesn't work in OnDeath since that is cleared after the region
 *			method is called.
 *	04/24/09, plasma
 *		- Prevent all XML regions loading
 *		- Created a new method that returns a List<Region> cache using the xml data
 *			Have taken care to avoid AddRegion() and RemoveRegion() being called due to region props!
 *		- Some overloads to allow a single region to be found and returned
 *	7/7/08, Adam
 *		Replace exception logic for Map.Parse() with normal error handling. (No longer throws an exception.)
 *	5/11/08, Adam
 *		Performance: Convert Regions to use HashTables instead of ArrayLists
 *	10/10/2006, Pix
 *		Added static IsInNoMurderZone for nested regions
 *  3/25/07, Adam
 *      Add FindAll() function to locate all regions at a given point.
 *  7/29/06, Kit
 *		Added virtual EquipItem call.
 *	6/15/06, Pix
 *		Added virtual IsNoMurderZone property.
 */

using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Server
{
    public enum MusicName
    {
        Invalid = -1,
        OldUlt01 = 0,
        Create1,
        DragFlit,
        OldUlt02,
        OldUlt03,
        OldUlt04,
        OldUlt05,
        OldUlt06,
        Stones2,
        Britain1,
        Britain2,
        Bucsden,
        Jhelom,
        LBCastle,
        Linelle,
        Magincia,
        Minoc,
        Ocllo,
        Samlethe,
        Serpents,
        Skarabra,
        Trinsic,
        Vesper,
        Wind,
        Yew,
        Cave01,
        Dungeon9,
        Forest_a,
        InTown01,
        Jungle_a,
        Mountn_a,
        Plains_a,
        Sailing,
        Swamp_a,
        Tavern01,
        Tavern02,
        Tavern03,
        Tavern04,
        Combat1,
        Combat2,
        Combat3,
        Approach,
        Death,
        Victory,
        BTCastle,
        Nujelm,
        Dungeon2,
        Cove,
        Moonglow,
        Zento,
        TokunoDungeon
    }

    public class Region : IComparable
    {
        private int m_Priority;
        private ArrayList m_Coords;     // Rectangle2D & Rectangle3D
        private ArrayList m_InnBounds;  // Rectangle2D & Rectangle3D
        private Map m_Map;
        private string m_Name;
        private Region m_Parent;        // added for RunUO compatibility, but never set
        private string m_Prefix;
        private Point3D m_GoLoc;
        private int m_UId;
        private bool m_Load;
        private Dictionary<Serial, Mobile> m_Players;
        private Dictionary<Serial, Mobile> m_Mobiles;
        private MusicName m_Music = MusicName.Invalid;

        public int CompareTo(object o)
        {
            if (!(o is Region))
                return 0;

            Region r = (Region)o;

            int a = r.m_Priority;
            int b = m_Priority;

            if (a < b)
                return -1;
            else if (a > b)
                return 1;
            else
                return 0;

            /*if ( o is Region )
			{
				return ((Region)o).Priority.CompareTo( Priority );
			} 
			else
				return 0;*/
        }

        /*private Region( string prefix, string name, Map map, int uid ) : this(prefix,name,map)
		{
			m_UId = uid | 0x40000000;
		}*/

        public Region(string prefix, string name, Map map)
        {
            m_Prefix = prefix;
            m_Name = name;
            m_Map = map;

            m_Priority = Region.LowestPriority;
            m_GoLoc = Point3D.Zero;

            m_Players = new Dictionary<Serial, Mobile>(2);  // probably not many players 
            m_Mobiles = new Dictionary<Serial, Mobile>();   // probably a reasonable number of mobiles

            m_Load = true;

            m_UId = m_RegionUID++;
        }

        public virtual bool IsNoMurderZone
        {
            get
            {
                return false;
            }
        }

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write((int)0);//version
        }

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();
        }

        public virtual void MakeGuard(Mobile focus)
        {
        }

        public virtual Type GetResource(Type type)
        {
            return type;
        }

        public virtual bool KeepsItemsOnDeath()
        {   // special 'keep loot' area like the CTF region
            return false;
        }

        public virtual bool CanUseStuckMenu(Mobile m)
        {
            return true;
        }

        public virtual void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
        }

        public virtual void OnDidHarmful(Mobile harmer, Mobile harmed)
        {
        }

        public virtual void OnGotHarmful(Mobile harmer, Mobile harmed)
        {
        }

        public virtual void OnPlayerAdd(Mobile m)
        {
        }

        public virtual void OnPlayerRemove(Mobile m)
        {
        }

        public virtual void OnMobileAdd(Mobile m)
        {
        }

        public virtual void OnMobileRemove(Mobile m)
        {
        }

        public virtual bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            return true;
        }

        public virtual void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
        }

        public virtual void PlayMusic(Mobile m)
        {
            if (m_Music != MusicName.Invalid && m.NetState != null)
                m.Send(Network.PlayMusic.GetInstance(m_Music));
        }

        public virtual void StopMusic(Mobile m)
        {
            if (m_Music != MusicName.Invalid && m.NetState != null)
                m.Send(Network.PlayMusic.InvalidInstance);
        }

        public void InternalEnter(Mobile m)
        {
            if (m.Player && !m_Players.ContainsKey(m.Serial))
            {
                m_Players[m.Serial] = m;

                m.CheckLightLevels(false);

                //m.Send( new Network.GlobalLightLevel( (int)LightLevel( m, Map.GlobalLight ) ) );//update the light level
                //m.Send( new Network.PersonalLightLevel( m ) );

                OnPlayerAdd(m);
            }

            if (!m_Mobiles.ContainsKey(m.Serial))
            {
                m_Mobiles[m.Serial] = m;

                OnMobileAdd(m);
            }

            OnEnter(m);
            PlayMusic(m);
        }

        public virtual void OnEnter(Mobile m)
        {
            string s = ToString();
            if (s != "")
                m.SendMessage("You have entered {0}", this);
        }

        public void InternalExit(Mobile m)
        {
            // Adam's new fast version!
            if (m.Player && m_Players.ContainsKey(m.Serial))
            {
                m_Players.Remove(m.Serial);
                OnPlayerRemove(m);
            }

            if (m_Mobiles.ContainsKey(m.Serial))
            {
                m_Mobiles.Remove(m.Serial);
                OnMobileRemove(m);
            }

            OnExit(m);
            StopMusic(m);
        }

        public virtual void OnExit(Mobile m)
        {
            string s = ToString();
            if (s != "")
                m.SendMessage("You have left {0}", this);
        }

        public virtual bool OnTarget(Mobile m, Target t, object o)
        {
            return true;
        }

        public virtual bool OnCombatantChange(Mobile m, Mobile Old, Mobile New)
        {
            return true;
        }

        public virtual bool AllowHousing(Point3D p)
        {
            return true;
        }

        public virtual bool SendInaccessibleMessage(Item item, Mobile from)
        {
            return false;
        }

        public virtual bool CheckAccessibility(Item item, Mobile from)
        {
            return true;
        }

        public virtual bool OnDecay(Item item)
        {
            return true;
        }

        public virtual bool AllowHarmful(Mobile from, Mobile target)
        {
            if (Mobile.AllowHarmfulHandler != null)
                return Mobile.AllowHarmfulHandler(from, target);

            return true;

            /*if ( (Map.Rules & MapRules.HarmfulRestrictions) != 0 )
				return false;
			else
				return true;*/
        }

        public virtual void OnCriminalAction(Mobile m, bool message)
        {
            if (message)
                m.SendLocalizedMessage(1005040); // You've committed a criminal act!!
        }

        public virtual bool AllowBenificial(Mobile from, Mobile target)
        {
            if (Mobile.AllowBeneficialHandler != null)
                return Mobile.AllowBeneficialHandler(from, target);

            return true;

            /*if ( (Map.Rules & MapRules.BeneficialRestrictions) != 0 )
			{
				int n = Notoriety.Compute( from, target );

				if (n == Notoriety.Criminal || n == Notoriety.Murderer)
				{
					return false;
				}
				else if ( target.Guild != null && target.Guild.Type != Guilds.GuildType.Regular )//disallow Chaos/order for healing each other or being healed by blues
				{
					if ( from.Guild == null || from.Guild.Type != target.Guild.Type )
						return false;
				}
			}
			return true;*/
        }

        public virtual void OnBenificialAction(Mobile helper, Mobile target)
        {
        }

        public virtual void OnGotBenificialAction(Mobile helper, Mobile target)
        {
        }

        public virtual bool IsInInn(Point3D p)
        {
            if (m_InnBounds == null)
                return false;

            for (int i = 0; i < m_InnBounds.Count; ++i)
            {
                Rectangle2D rect = (Rectangle2D)m_InnBounds[i];

                if (rect.Contains(p))
                    return true;
            }

            return false;
        }

        private static TimeSpan m_InnLogoutDelay = TimeSpan.Zero;
        private static TimeSpan m_GMLogoutDelay = TimeSpan.FromSeconds(10.0);
        private static TimeSpan m_DefaultLogoutDelay = TimeSpan.FromMinutes(5.0);

        public static readonly int DefaultPriority = 50;

        public static TimeSpan InnLogoutDelay
        {
            get { return m_InnLogoutDelay; }
            set { m_InnLogoutDelay = value; }
        }

        public static TimeSpan GMLogoutDelay
        {
            get { return m_GMLogoutDelay; }
            set { m_GMLogoutDelay = value; }
        }

        public static TimeSpan DefaultLogoutDelay
        {
            get { return m_DefaultLogoutDelay; }
            set { m_DefaultLogoutDelay = value; }
        }

        public virtual TimeSpan GetLogoutDelay(Mobile m)
        {
            if (m.Aggressors.Count == 0 && m.Aggressed.Count == 0 && IsInInn(m.Location))
                return m_InnLogoutDelay;
            else if (m.AccessLevel >= AccessLevel.GameMaster)
                return m_GMLogoutDelay;
            else
                return m_DefaultLogoutDelay;
        }

        // adam: we no longer use regions for every different kind of area, but instead use the notion
        //	of 'rule sets'. 
        public virtual bool IsDungeonRules { get { return false; } }
        public virtual bool IsJailRules { get { return false; } }
        public virtual bool IsHouseRules { get { return false; } }
        public virtual bool IsGreenAcresRules { get { return false; } }
        public virtual bool IsAngelIslandRules { get { return false; } }
        public virtual bool IsGuarded { get { return false; } set {; } }
        public virtual bool IsSmartGuards { get { return false; } set {; } }

        public virtual void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
        }

        /*public virtual double LightLevel( Mobile m, double level )
		{
			return level;
		}*/

        public virtual void SpellDamageScalar(Mobile caster, Mobile target, ref double damage)
        {
        }

        public virtual void OnSpeech(SpeechEventArgs args)
        {
        }

        public virtual bool AllowSpawn()
        {
            return true;
        }

        public virtual bool OnSkillUse(Mobile m, int Skill)
        {
            return true;
        }

        public virtual bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            return true;
        }

        public virtual void OnSpellCast(Mobile m, ISpell s)
        {
        }

        public virtual bool EquipItem(Mobile m, Item item)
        {
            return true;
        }

        public virtual bool OnResurrect(Mobile m)
        {
            return true;
        }

        public virtual bool OnDeath(Mobile m)
        {
            return true;
        }

        public virtual void OnAfterDeath(Mobile m)
        {
        }

        public virtual bool OnDamage(Mobile m, ref int Damage)
        {
            return true;
        }

        public virtual bool OnHeal(Mobile m, ref int Heal)
        {
            return true;
        }

        public virtual bool OnDoubleClick(Mobile m, object o)
        {
            return true;
        }

        public virtual bool OnSingleClick(Mobile m, object o)
        {
            return true;
        }

        //Should this region be loaded from the xml?
        public bool LoadFromXml
        {
            get
            {
                return m_Load;
            }
            set
            {
                m_Load = value;
            }
        }

        //does this region save?
        public virtual bool Saves
        {
            get
            {
                return false;
            }
        }

        public Dictionary<Serial, Mobile> Mobiles
        {
            get
            {
                return m_Mobiles;
            }
        }

        public Dictionary<Serial, Mobile> Players
        {
            get
            {
                return m_Players;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        public string Prefix
        {
            get
            {
                return m_Prefix;
            }
            set
            {
                m_Prefix = value;
            }
        }

        public MusicName Music
        {
            get { return m_Music; }
            set { m_Music = value; }
        }

        public Point3D GoLocation
        {
            get
            {
                return m_GoLoc;
            }
            set
            {
                m_GoLoc = value;
            }
        }

        public Map Map
        {
            get
            {
                return m_Map;
            }
            set
            {
                RemoveRegion(this);
                m_Map = value;
                AddRegion(this);
            }
        }

        public ArrayList InnBounds
        {
            get { return m_InnBounds; }
            set { m_InnBounds = value; }
        }

        private ArrayList m_LoadCoords;

        public ArrayList Coords
        {
            get
            {
                return m_Coords;
            }
            set
            {
                if (m_Coords != value)
                {
                    RemoveRegion(this);
                    m_Coords = value;
                    AddRegion(this);
                }
            }
        }

        public int Priority
        {
            get
            {
                return m_Priority;
            }
            set
            {
                if (value != m_Priority)
                {
                    m_Priority = value;

                    /*if ( m_Priority < Region.LowestPriority ) m_Priority = Region.LowestPriority;
					else if ( m_Priority > Region.HighestPriority ) m_Priority = Region.HighestPriority;*/

                    m_Map.Regions.Sort();
                }
            }
        }

        public int UId
        {
            get
            {
                return m_UId;
            }
        }

        private int m_MinZ = short.MinValue;
        private int m_MaxZ = short.MaxValue + 1;

        public int MinZ { get { return m_MinZ; } set { RemoveRegion(this); m_MinZ = value; AddRegion(this); } }
        public int MaxZ { get { return m_MaxZ; } set { RemoveRegion(this); m_MaxZ = value; AddRegion(this); } }

        public bool Contains(Point3D p)
        {//possibly use a binary search instead, to increase speed?

            if (m_Coords == null)
                return false;

            for (int i = 0; i < m_Coords.Count; ++i)
            {
                object obj = m_Coords[i];

                if (obj is Rectangle3D)
                {
                    Rectangle3D r3d = (Rectangle3D)obj;

                    if (r3d.Contains(p))
                        return true;
                }
                else if (obj is Rectangle2D)
                {
                    Rectangle2D r2d = (Rectangle2D)obj;

                    if (r2d.Contains(p) && p.m_Z >= m_MinZ && p.m_Z <= m_MaxZ)
                        return true;
                }
            }

            return false;
        }

        public bool IsChildOf(Region region)
        {
            if (region == null)
                return false;

            Region p = m_Parent;

            while (p != null)
            {
                if (p == region)
                    return true;

                p = p.m_Parent;
            }

            return false;
        }

        public Region GetRegion(Type regionType)
        {
            if (regionType == null)
                return null;

            Region r = this;

            do
            {
                if (regionType.IsAssignableFrom(r.GetType()))
                    return r;

                r = r.m_Parent;
            }
            while (r != null);

            return null;
        }

        public Region GetRegion(string regionName)
        {
            if (regionName == null)
                return null;

            Region r = this;

            do
            {
                if (r.m_Name == regionName)
                    return r;

                r = r.m_Parent;
            }
            while (r != null);

            return null;
        }

        public bool IsPartOf(Region region)
        {
            if (this == region)
                return true;

            return IsChildOf(region);
        }

        public bool IsPartOf(Type regionType)
        {
            return (GetRegion(regionType) != null);
        }

        public bool IsPartOf(string regionName)
        {
            return (GetRegion(regionName) != null);
        }

        public override string ToString()
        {
            if (Prefix != "")
                return string.Format("{0} {1}", Prefix, Name);
            else
                return Name;
        }

        public static bool IsNull(Region r)
        {
            return Object.ReferenceEquals(r, null);
        }

        //high priorities first (high priority is less than low priority)
        public static bool operator <(Region l, Region r)
        {
            if (IsNull(l) && IsNull(r))
                return false;
            else if (IsNull(l))
                return true;
            else if (IsNull(r))
                return false;

            return l.Priority > r.Priority;
        }

        public static bool operator >(Region l, Region r)
        {
            if (IsNull(l) && IsNull(r))
                return false;
            else if (IsNull(l))
                return false;
            else if (IsNull(r))
                return true;

            return l.Priority < r.Priority;
        }

        public static bool operator ==(Region l, Region r)
        {
            if (IsNull(l))
                return IsNull(r);
            else if (IsNull(r))
                return false;

            return l.UId == r.UId;
        }

        public static bool operator !=(Region l, Region r)
        {
            if (IsNull(l))
                return !IsNull(r);
            else if (IsNull(r))
                return true;

            return l.UId != r.UId;
        }

        public override bool Equals(object o)
        {
            if (!(o is Region) || o == null)
                return false;
            else
                return ((Region)o) == this;
        }

        public override int GetHashCode()
        {
            return m_UId;
        }


        public const int LowestPriority = 0;
        public const int HighestPriority = 150;

        public const int TownPriority = 50;
        public const int HousePriority = HighestPriority;
        public const int InnPriority = TownPriority + 1;

        private static int m_RegionUID = 1;//use to give each region a unique identifier number (to check for equality)

        private static bool m_SupressXmlWarnings;

        public static bool SupressXmlWarnings
        {
            get { return m_SupressXmlWarnings; }
            set { m_SupressXmlWarnings = value; }
        }

        public static Region GetByName(string name, Map map)
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                Region r = (Region)m_Regions[i];
                if (r.Map == map && r.Name == name)
                    return r;
            }

            return null;
        }

        public static object ParseRectangle(XmlElement rect, bool supports3d)
        {
            int x1, y1, x2, y2;

            if (rect.HasAttribute("x") && rect.HasAttribute("y") && rect.HasAttribute("width") && rect.HasAttribute("height"))
            {
                x1 = int.Parse(rect.GetAttribute("x"));
                y1 = int.Parse(rect.GetAttribute("y"));
                x2 = x1 + int.Parse(rect.GetAttribute("width"));
                y2 = y1 + int.Parse(rect.GetAttribute("height"));
            }
            else if (rect.HasAttribute("x1") && rect.HasAttribute("y1") && rect.HasAttribute("x2") && rect.HasAttribute("y2"))
            {
                x1 = int.Parse(rect.GetAttribute("x1"));
                y1 = int.Parse(rect.GetAttribute("y1"));
                x2 = int.Parse(rect.GetAttribute("x2"));
                y2 = int.Parse(rect.GetAttribute("y2"));
            }
            else
            {
                throw new ArgumentException("Wrong attributes specified.");
            }

            if (supports3d && (rect.HasAttribute("zmin") || rect.HasAttribute("zmax")))
            {
                int zmin = short.MinValue;
                int zmax = short.MaxValue;

                if (rect.HasAttribute("zmin"))
                    zmin = int.Parse(rect.GetAttribute("zmin"));

                if (rect.HasAttribute("zmax"))
                    zmax = int.Parse(rect.GetAttribute("zmax"));

                return new Rectangle3D(x1, y1, zmin, x2 - x1, y2 - y1, zmax - zmin + 1);
            }

            return new Rectangle2D(x1, y1, x2 - x1, y2 - y1);
        }

        public static Region CreateRegionFromXML(ref string outputText, string regionName)
        {
            List<Region> regions = CreateCacheFromXML(ref outputText, regionName);
            if (regions.Count == 1)
            {
                return regions[0];
            }
            return null;
        }

        public static List<Region> CreateCacheFromXML(ref string outputText)
        {
            return CreateCacheFromXML(ref outputText, string.Empty);
        }

        /// <summary>
        /// Plasma: This method will create a cache of ALL the regions in the XML, not just the ones that have been loaded itno m_Regions through Initialize()
        /// This code still respects the LoadFromXML and all other previous logic.
        /// </summary>
        /// <param name="outputText"></param>
        /// <param name="regionToFind"></param>
        /// <returns></returns>
        public static List<Region> CreateCacheFromXML(ref string outputText, string regionToFind)
        {
            List<Region> loadedRegions = new List<Region>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (!System.IO.File.Exists("Data/Regions.xml"))
            {
                sb.Append(Environment.NewLine).Append("Error: Data/Regions.xml does not exist");
                return loadedRegions;
            }

            Console.Write("Regions: Loading...");

            XmlDocument doc = new XmlDocument();
            doc.Load("Data/Regions.xml");

            XmlElement root = doc["ServerRegions"];
            foreach (XmlElement facet in root.GetElementsByTagName("Facet"))
            {
                bool exit = false;
                string facetName = facet.GetAttribute("name");
                Map map = Map.Parse(facetName);

                if (map == null || map == Map.Internal)
                {
                    if (!m_SupressXmlWarnings)
                        sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Invalid facet name '{0}'", facetName);
                    continue;
                }

                foreach (XmlElement reg in facet.GetElementsByTagName("region"))
                {

                    string name = reg.GetAttribute("name");
                    if (name == null || name.Length <= 0)
                        continue;

                    Region r = new Region(string.Empty, name, map);

                    if (string.IsNullOrEmpty(regionToFind) || name == regionToFind)
                    {
                        loadedRegions.Add(r);

                        if (!r.LoadFromXml)
                        {
                            if (!m_SupressXmlWarnings)
                                sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Region '{0}' has an XML entry, but is set not to LoadFromXml.", name);
                            continue;
                        }

                        try
                        {
                            r.Priority = int.Parse(reg.GetAttribute("priority"));
                        }
                        catch
                        {
                            if (!m_SupressXmlWarnings)
                                sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Could not parse priority for region '{0}' (assuming TownPriority)", r.Name);
                            r.Priority = TownPriority;
                        }

                        XmlElement el;

                        el = reg["go"];
                        if (el != null)
                        {
                            try
                            {
                                r.GoLocation = Point3D.Parse(el.GetAttribute("location"));
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Could not parse go location for region '{0}'", r.Name);
                            }
                        }

                        el = reg["music"];
                        if (el != null)
                        {
                            try
                            {
                                r.Music = (MusicName)Enum.Parse(typeof(MusicName), el.GetAttribute("name"), true);
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Could not parse music for region '{0}'", r.Name);
                            }
                        }

                        el = reg["zrange"];
                        if (el != null)
                        {
                            string s = el.GetAttribute("min");
                            if (s != null && s != "")
                            {
                                try
                                {
                                    r.m_MinZ = int.Parse(s);
                                }
                                catch
                                {
                                    if (!m_SupressXmlWarnings)
                                        sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Could not parse zrange:min for region '{0}'", r.Name);
                                }
                            }

                            s = el.GetAttribute("max");
                            if (s != null && s != "")
                            {
                                try
                                {
                                    r.m_MaxZ = int.Parse(s);
                                }
                                catch
                                {
                                    if (!m_SupressXmlWarnings)
                                        sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Could not parse zrange:max for region '{0}'", r.Name);
                                }
                            }
                        }

                        foreach (XmlElement rect in reg.GetElementsByTagName("rect"))
                        {
                            try
                            {
                                if (r.m_LoadCoords == null)
                                    r.m_LoadCoords = new ArrayList(1);

                                r.m_LoadCoords.Add(ParseRectangle(rect, true));
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Error parsing rect for region '{0}'", r.Name);
                                continue;
                            }
                        }

                        foreach (XmlElement rect in reg.GetElementsByTagName("inn"))
                        {
                            try
                            {
                                if (r.InnBounds == null)
                                    r.InnBounds = new ArrayList(1);

                                r.InnBounds.Add(ParseRectangle(rect, false));
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat("Regions.xml: Error parsing inn for region '{0}'", r.Name);
                                continue;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(regionToFind) && name == regionToFind)
                    {
                        exit = true;
                    }
                }

                if (exit) break;
            }

            //ArrayList copy = new ArrayList(m_Regions);

            int i;
            for (i = 0; i < loadedRegions.Count; ++i)
            {
                Region region = loadedRegions[i];

                if (!region.LoadFromXml && region.m_Coords == null)
                {
                    region.Coords = new ArrayList();
                }
                else if (region.LoadFromXml)
                {
                    if (region.m_LoadCoords == null)
                        region.m_LoadCoords = new ArrayList();

                    region.m_Coords = region.m_LoadCoords;

                    //if ( !m_SupressXmlWarnings )
                    //	sb.Append(Environment.NewLine).AppendFormat( "Warning: Region '{0}' did not contain any coords in Regions.xml (map={0})", region, region.Map.Name );
                }
            }

            sb.Append(Environment.NewLine).Append("done");
            outputText = sb.ToString();
            return loadedRegions;
        }

        public static void Load()
        {
            return; //Plasma : We no longer load from XML as all the regions are DRDT
            /*
if (!System.IO.File.Exists("Data/Regions.xml"))
{
Console.WriteLine("Error: Data/Regions.xml does not exist");
return;
}

Console.Write("Regions: Loading...");

XmlDocument doc = new XmlDocument();
doc.Load("Data/Regions.xml");

XmlElement root = doc["ServerRegions"];
foreach (XmlElement facet in root.GetElementsByTagName("Facet"))
{
string facetName = facet.GetAttribute("name");
Map map = Map.Parse(facetName);

if (map == null || map == Map.Internal)
{
   if (!m_SupressXmlWarnings)
	   Console.WriteLine("Regions.xml: Invalid facet name '{0}'", facetName);
   continue;
}

foreach (XmlElement reg in facet.GetElementsByTagName("region"))
{
   string name = reg.GetAttribute("name");
   if (name == null || name.Length <= 0)
	   continue;

   Region r = GetByName(name, map);
   if (r == null)
   {
	   //if ( !m_SupressXmlWarnings )
	   //	Console.WriteLine( "Regions.xml: Region '{0}' not defined.", name );
	   continue;
   }
   else if (!r.LoadFromXml)
   {
	   if (!m_SupressXmlWarnings)
		   Console.WriteLine("Regions.xml: Region '{0}' has an XML entry, but is set not to LoadFromXml.", name);
	   continue;
   }

   try
   {
	   r.Priority = int.Parse(reg.GetAttribute("priority"));
   }
   catch
   {
	   if (!m_SupressXmlWarnings)
		   Console.WriteLine("Regions.xml: Could not parse priority for region '{0}' (assuming TownPriority)", r.Name);
	   r.Priority = TownPriority;
   }

   XmlElement el;

   el = reg["go"];
   if (el != null)
   {
	   try
	   {
		   r.GoLocation = Point3D.Parse(el.GetAttribute("location"));
	   }
	   catch
	   {
		   if (!m_SupressXmlWarnings)
			   Console.WriteLine("Regions.xml: Could not parse go location for region '{0}'", r.Name);
	   }
   }

   el = reg["music"];
   if (el != null)
   {
	   try
	   {
		   r.Music = (MusicName)Enum.Parse(typeof(MusicName), el.GetAttribute("name"), true);
	   }
	   catch
	   {
		   if (!m_SupressXmlWarnings)
			   Console.WriteLine("Regions.xml: Could not parse music for region '{0}'", r.Name);
	   }
   }

   el = reg["zrange"];
   if (el != null)
   {
	   string s = el.GetAttribute("min");
	   if (s != null && s != "")
	   {
		   try
		   {
			   r.MinZ = int.Parse(s);
		   }
		   catch
		   {
			   if (!m_SupressXmlWarnings)
				   Console.WriteLine("Regions.xml: Could not parse zrange:min for region '{0}'", r.Name);
		   }
	   }

	   s = el.GetAttribute("max");
	   if (s != null && s != "")
	   {
		   try
		   {
			   r.MaxZ = int.Parse(s);
		   }
		   catch
		   {
			   if (!m_SupressXmlWarnings)
				   Console.WriteLine("Regions.xml: Could not parse zrange:max for region '{0}'", r.Name);
		   }
	   }
   }

   foreach (XmlElement rect in reg.GetElementsByTagName("rect"))
   {
	   try
	   {
		   if (r.m_LoadCoords == null)
			   r.m_LoadCoords = new ArrayList(1);

		   r.m_LoadCoords.Add(ParseRectangle(rect, true));
	   }
	   catch
	   {
		   if (!m_SupressXmlWarnings)
			   Console.WriteLine("Regions.xml: Error parsing rect for region '{0}'", r.Name);
		   continue;
	   }
   }

   foreach (XmlElement rect in reg.GetElementsByTagName("inn"))
   {
	   try
	   {
		   if (r.InnBounds == null)
			   r.InnBounds = new ArrayList(1);

		   r.InnBounds.Add(ParseRectangle(rect, false));
	   }
	   catch
	   {
		   if (!m_SupressXmlWarnings)
			   Console.WriteLine("Regions.xml: Error parsing inn for region '{0}'", r.Name);
		   continue;
	   }
   }
}
}

ArrayList copy = new ArrayList(m_Regions);

int i;
for (i = 0; i < copy.Count; ++i)
{
Region region = (Region)copy[i];
if (!region.LoadFromXml && region.m_Coords == null)
{
   region.Coords = new ArrayList();
}
else if (region.LoadFromXml)
{
   if (region.m_LoadCoords == null)
	   region.m_LoadCoords = new ArrayList();

   region.Coords = region.m_LoadCoords;

   //if ( !m_SupressXmlWarnings )
   //	Console.WriteLine( "Warning: Region '{0}' did not contain any coords in Regions.xml (map={0})", region, region.Map.Name );
}
}

for (i = 0; i < Map.AllMaps.Count; ++i)
((Map)Map.AllMaps[i]).Regions.Sort();

ArrayList list = new ArrayList(World.Mobiles.Values);

foreach (Mobile m in list)
m.ForceRegionReEnter(true);

Console.WriteLine("done");
			 * */
        }

        /*public static void Load()
		{
			if ( !System.IO.File.Exists( "Data/Regions.xml" ) )
			{
				Console.WriteLine( "Error: Data/Regions.xml does not exist" );
				return;
			}

			int i;
			xmlRecord cur;
			Hashtable data = new Hashtable();
			Map CurMap = Map.Felucca;
			XmlTextReader xml = new XmlTextReader( "Data/Regions.xml" );
			xml.WhitespaceHandling = WhitespaceHandling.None;

			cur = new xmlRecord();
			cur.Name = "";
			cur.music = MusicName.Invalid;
			cur.goloc = Point3D.Zero;
			cur.Coords = null;
			cur.Map = Map.Felucca;
			cur.zMin = short.MinValue;
			cur.zMax = short.MaxValue;

			Console.Write( "Regions: Loading..." );

			while ( xml.Read() )
			{
				if ( xml.NodeType == XmlNodeType.Element )
				{
					if ( xml.Name.ToLower() == "serverregions" )//seek to <ServerRegions>
						break;
				}
			}

			while ( xml.Read() )
			{
				if ( xml.NodeType == XmlNodeType.Element )
				{
					switch ( xml.Name.ToLower() )
					{
						case "facet":
							xml.MoveToContent();
							try
							{
								CurMap = Map.Parse( xml["name"].ToLower() );
							}
							catch
							{
								CurMap = null;
							}

							if ( CurMap == null && !m_SupressXmlWarnings )
								Console.WriteLine( "Warning: Unknown Facet name \"{0}\" while reading regions.xml (Line {1})", xml["name"], xml.LineNumber );

							break;

						case "region":
							if ( CurMap == null )
								break;

							xml.MoveToContent();
							cur = new xmlRecord();
							cur.goloc = Point3D.Zero;
							cur.music = MusicName.Invalid;

							cur.Name = xml["name"];

							if ( cur.Name == "" && !m_SupressXmlWarnings )
								Console.WriteLine( "Warning: Region has no name in regions.xml (Line {0})", xml.LineNumber );

							cur.Map = CurMap;
							cur.Coords = new ArrayList();
							cur.innBounds = new ArrayList();
							cur.zMin = short.MinValue;
							cur.zMax = short.MaxValue;

							if ( xml["priority"] != "" )
							{
								try
								{
									cur.priority = Convert.ToInt32( xml["priority"] );
								}
								catch
								{
									if ( !m_SupressXmlWarnings )
										Console.WriteLine( "Warning: Unable to convert \"{0}\" to priority in regions.xml (Line {1})", xml["priority"], xml.LineNumber );
								}
							} 
							else
							{
								cur.priority = Region.LowestPriority;
							}
							break;

						case "go":
							try
							{
								xml.MoveToContent();
								cur.goloc = Point3D.Parse( xml["location"] ); 
							}
							catch
							{
								if ( !m_SupressXmlWarnings )
									Console.WriteLine( "Warning: Unable to convert \"{0}\" to Point3D in regions.xml (Line {1})", xml["location"], xml.LineNumber );
							}
							break;
						case "music":
						{
							try
							{
								xml.MoveToContent();
								cur.music = (MusicName)Enum.Parse( typeof( MusicName ), xml["name"], true );
							}
							catch
							{
								if ( !m_SupressXmlWarnings )
									Console.WriteLine( "Warning: Unable to conert \"{0}\" to MusicName in regions.xml (Line {1})", xml["name"], xml.LineNumber );
							}
							break;
						}
						case "zrange":
						{
							try
							{
								xml.MoveToContent();

								string min = xml["min"], max = xml["max"];

								if ( min != null && min.Length > 0 )
								{
									try
									{
										cur.zMin = int.Parse( min );
									}
									catch
									{
										if ( !m_SupressXmlWarnings )
											Console.WriteLine( "Warning: Unable to conert \"{0}\" to Integer in regions.xml (Line {1})", min, xml.LineNumber );
									}
								}

								if ( max != null && max.Length > 0 )
								{
									try
									{
										cur.zMax = int.Parse( max );
									}
									catch
									{
										if ( !m_SupressXmlWarnings )
											Console.WriteLine( "Warning: Unable to conert \"{0}\" to Integer in regions.xml (Line {1})", max, xml.LineNumber );
									}
								}
							}
							catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
							break;
						}
						case "rect":
							xml.MoveToContent();
							int x1=0,y1=0,x2=0,y2=0;
							try
							{
								string vx1 = xml["x1"];
								string vx2 = xml["x2"];
								string vy1 = xml["y1"];
								string vy2 = xml["y2"];

								if ( vx1 == null || vx1 == "" )
								{
									string x = xml["x"];
									string y = xml["y"];
									string w = xml["width"];
									string h = xml["height"];

									x1 = Convert.ToInt32( x );
									y1 = Convert.ToInt32( y );
									x2 = x1 + Convert.ToInt32( w );
									y2 = y1 + Convert.ToInt32( h );
								}
								else
								{
									x1 = Convert.ToInt32( vx1 );
									x2 = Convert.ToInt32( vx2 );
									y1 = Convert.ToInt32( vy1 );
									y2 = Convert.ToInt32( vy2 );
								}

								string zmin = xml["zmin"];
								string zmax = xml["zmax"];

								if ( (zmin == null || zmin == "") && (zmax == null || zmax == "") )
								{
									cur.Coords.Add( new Rectangle2D( x1, y1, x2-x1, y2-y1 ) ); 
								}
								else
								{
									int vzmin = short.MinValue;
									int vzmax = short.MaxValue;

									if ( zmin != null && zmin != "" )
										vzmin = Convert.ToInt32( zmin );

									if ( zmax != null && zmax != "" )
										vzmax = Convert.ToInt32( zmax );

									cur.Coords.Add( new Rectangle3D( x1, y1, vzmin, x2-x1, y2-y1, vzmax - vzmin + 1 ) );
								}
							}
							catch
							{
								if ( !m_SupressXmlWarnings )
									Console.WriteLine( "Warning: Could not convert to rect numbers in regions.xml (Line {0})", xml.LineNumber );
							}
							break;
						case "inn":
							xml.MoveToContent();
							try
							{
								cur.innBounds.Add( new Rectangle2D( Convert.ToInt32( xml["x"] ), Convert.ToInt32( xml["y"] ), Convert.ToInt32( xml["width"] ), Convert.ToInt32( xml["height"] ) ) );
							}
							catch
							{
								if ( !m_SupressXmlWarnings )
									Console.WriteLine("Warning: Could not convert inn numbers in regions.xml (Line {0})", xml.LineNumber);
							}
							break;
						default:
						{
							if ( !m_SupressXmlWarnings )
								Console.WriteLine( "Unknown xml Element \"{0}\" in regions.xml (Line {1})", xml.Name, xml.LineNumber );

							break;
						}
					}
				}
				else if ( xml.NodeType == XmlNodeType.EndElement )
				{
					switch( xml.Name.ToLower() )
					{
						case "region":
							if ( cur.Map == null )
								break;

							ArrayList dataEntries = (ArrayList)data[cur.Name];

							if ( dataEntries == null )
								data[cur.Name] = dataEntries = new ArrayList();

							dataEntries.Add( cur );
							break;
						case "serverregions":
							xml.Close();//stop parsing
							break;
						case "facet":
							CurMap = null;
							break;
						default:
						{
							if ( !m_SupressXmlWarnings )
								Console.WriteLine( "Unknown xml [End]Element \"{0}\" in regions.xml (Line {1})", xml.Name, xml.LineNumber );

							break;
						}
					}
				} 
				else if ( xml.NodeType != XmlNodeType.Comment )
				{
					if ( !m_SupressXmlWarnings )
						Console.WriteLine( "Unknown xml node ({0}) in regions.xml, Line {1}", xml.NodeType, xml.LineNumber );
				}
			}

			xml.Close();

			Queue q = new Queue();

			for ( i = 0; i < m_Regions.Count; ++i )
			{
				Region region = (Region)m_Regions[i];

				if ( !region.LoadFromXml )
					continue;

				ArrayList dataEntries = (ArrayList)data[region.Name];

				bool contains = false;

				for ( int j = 0; !contains && dataEntries != null && j < dataEntries.Count; ++j )
				{
					cur = (xmlRecord)dataEntries[j];

					if ( cur.Map != region.Map )
						continue;

					contains = true;

					region.GoLocation = cur.goloc;
					region.InnBounds = cur.innBounds;
					region.Music = cur.music;
					region.m_MinZ = cur.zMin;
					region.m_MaxZ = cur.zMax;

					if ( cur.priority != Region.LowestPriority )
						region.m_Priority = cur.priority;

					q.Enqueue( region );
					q.Enqueue( cur.Coords );
				}

				if ( !contains )
				{
					if ( region.m_Coords == null )
					{
						q.Enqueue( region );
						q.Enqueue( new ArrayList() );
					}

					if ( !m_SupressXmlWarnings )
						Console.WriteLine( "Warning: Region '{0}' did not contain an entry in regions.xml", region );
				}
			}

			while ( q.Count > 0 )
				((Region)q.Dequeue()).Coords = (ArrayList)q.Dequeue();

			for ( i = 0; i < Map.AllMaps.Count; ++i )
				((Map)Map.AllMaps[i]).Regions.Sort();

			ArrayList list = new ArrayList( World.Mobiles.Values );

			foreach ( Mobile m in list )
				m.ForceRegionReEnter( true );

			Console.WriteLine( "done" );
		}*/

        public bool IsDefault { get { return (this == m_Map.DefaultRegion); } }

        public void Unregister()
        {
            if (m_Coords == null || m_Map == null)
                return;

            for (int i = 0; i < m_Coords.Count; ++i)
            {
                object obj = m_Coords[i];

                Point2D start, end;

                if (obj is Rectangle2D)
                {
                    Rectangle2D r2d = (Rectangle2D)obj;

                    start = m_Map.Bound(r2d.Start);
                    end = m_Map.Bound(r2d.End);
                }
                else if (obj is Rectangle3D)
                {
                    Rectangle3D r3d = (Rectangle3D)obj;

                    start = m_Map.Bound(new Point2D(r3d.Start));
                    end = m_Map.Bound(new Point2D(r3d.End));
                }
                else
                {
                    continue;
                }

                Sector startSector = m_Map.GetSector(start);
                Sector endSector = m_Map.GetSector(end);

                for (int x = startSector.X; x <= endSector.X; ++x)
                    for (int y = startSector.Y; y <= endSector.Y; ++y)
                        m_Map.GetRealSector(x, y).OnLeave(this);
            }
        }

        public void Register()
        {
            if (m_Coords == null || m_Map == null)
                return;

            for (int i = 0; i < m_Coords.Count; ++i)
            {
                object obj = m_Coords[i];

                Point2D start, end;

                if (obj is Rectangle2D)
                {
                    Rectangle2D r2d = (Rectangle2D)obj;

                    start = m_Map.Bound(r2d.Start);
                    end = m_Map.Bound(r2d.End);
                }
                else if (obj is Rectangle3D)
                {
                    Rectangle3D r3d = (Rectangle3D)obj;

                    start = m_Map.Bound(new Point2D(r3d.Start));
                    end = m_Map.Bound(new Point2D(r3d.End));
                }
                else
                {
                    continue;
                }

                Sector startSector = m_Map.GetSector(start);
                Sector endSector = m_Map.GetSector(end);

                for (int x = startSector.X; x <= endSector.X; ++x)
                    for (int y = startSector.Y; y <= endSector.Y; ++y)
                        m_Map.GetRealSector(x, y).OnEnter(this);
            }
        }

        public static void AddRegion(Region region)
        {
            //Plasma: add in ref check to prevent duplicate regions being added! lol.
            //PS - yes, yes duplicates do get added ;p
            foreach (Region r in Regions)
                if (ReferenceEquals(r, region)) return;

            m_Regions.Add(region);

            region.Register();
            region.Map.Regions.Add(region);
            region.Map.Regions.Sort();
        }

        public static void RemoveRegion(Region region)
        {
            m_Regions.Remove(region);

            region.Unregister();
            region.Map.Regions.Remove(region);

            ArrayList list = new ArrayList(region.Mobiles.Values);

            for (int i = 0; i < list.Count; ++i)
                ((Mobile)list[i]).ForceRegionReEnter(false);
        }

        private static ArrayList m_Regions = new ArrayList();

        public static ArrayList Regions { get { return m_Regions; } }

        public static Region FindByUId(int uid)
        {
            for (int i = 0; i < m_Regions.Count; ++i)
            {
                Region region = (Region)m_Regions[i];

                if (region.UId == uid)
                    return region;
            }

            return null;
        }

        // the sector.Regions list is sorted on priority, so the first found is the highest priority
        public static Region Find(Point3D p, Map map)
        {
            return Find(p, map, null);
        }

        public static Region Find(Point3D p, Map map, Region ignore)
        {
            if (map == null)
                return Map.Internal.DefaultRegion;

            Sector sector = map.GetSector(p);
            ArrayList list = sector.Regions;

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is Region)
                {
                    // allow the caller to specify a region to ignore
                    if (ignore != null && list[i] as Region == ignore)
                        continue;

                    Region region = list[i] as Region;
                    if (region.Contains(p))
                        return region;
                }
            }

            return map.DefaultRegion;
        }
        public static Region FindByName(string name, Map map)
        {
            if (map != null && map != Map.Internal)
                foreach (Region rx in Regions)
                    if (rx != null && rx.Map == map && rx.Name.ToLower() == name.ToLower())
                        return rx;

            return null;
        }
        public virtual bool IsMobileCountable(Mobile aggressor)
        {
            return true;
        }

        #region Nested Region Resolver Static Functions

        public static bool IsInitialAggressionNotCountable(Mobile aggressor, Mobile defender)
        {
            bool bReturn = false;

            //if defender is in no murder zone, then not countable
            if (IsPointInNoMurderZone(defender.Location, defender.Map))
            {
                bReturn = true;
            }
            else if (!IsMobileCountableAtPoint(aggressor, aggressor.Location, aggressor.Map))
            {
                bReturn = true;
            }

            return bReturn;
        }

        private static bool IsMobileCountableAtPoint(Mobile m, Point3D p, Map map)
        {
            if (map == null)
                return true;

            Sector sector = map.GetSector(p);
            ArrayList list = sector.Regions;

            bool bReturn = true;

            for (int i = 0; i < list.Count; ++i)
            {
                Region region = (Region)list[i];

                if (region.Contains(p))
                {
                    if (region.IsMobileCountable(m) == false)
                    {
                        bReturn = false;
                    }
                }
            }

            return bReturn;
        }

        private static bool IsPointInNoMurderZone(Point3D p, Map map)
        {
            if (map == null)
                return false;

            Sector sector = map.GetSector(p);
            ArrayList list = sector.Regions;

            bool bReturn = false;

            for (int i = 0; i < list.Count; ++i)
            {
                Region region = (Region)list[i];

                if (region.Contains(p))
                {
                    if (region.IsNoMurderZone)
                    {
                        bReturn = true;
                    }
                }
            }

            return bReturn;
        }

        #endregion


        public static bool IsHighestPriority(Region r, Point3D p, Map map)
        {
            return r == GetHighestPriority(p, map);
        }

        public static Region GetHighestPriority(Point3D p, Map map)
        {   // will always contain at least the default region
            ArrayList list = FindAll(p, map);
            Region hpregion = map.DefaultRegion;
            for (int ix = 0; ix < list.Count; ix++)
            {
                if (list[ix] as Region > hpregion)
                    hpregion = list[ix] as Region;
            }

            return hpregion;
        }
        public static bool MatchingRegions(Mobile m1, Mobile m2)
        {   // all regions much match
            List<int> ids1 = new List<int>();
            foreach (Region rx in Region.FindAll(m1.Location, m1.Map))
                if (rx != null)
                    ids1.Add(rx.UId);

            List<int> ids2 = new List<int>();
            foreach (Region rx in Region.FindAll(m2.Location, m2.Map))
                if (rx != null)
                    ids2.Add(rx.UId);

            return ids1.Except(ids2).ToList().Count == 0;
        }
        public static ArrayList FindAll(Point3D p, Map map)
        {
            ArrayList all = new ArrayList();

            if (map == null)
            {
                all.Add(Map.Internal.DefaultRegion);
                return all;
            }

            Sector sector = map.GetSector(p);
            ArrayList list = sector.Regions;

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is Region)
                {
                    Region region = list[i] as Region;
                    if (region.Contains(p))
                        all.Add(region);
                }
            }

            all.Add(map.DefaultRegion);
            return all;
        }

    }
}
