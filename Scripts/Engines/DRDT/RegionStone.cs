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

/* Scripts/Engines/DRDT/RegionStone.cs
 * CHANGELOG:
 *	10/13/10, Adam
 *		Begin adding the sensory system 
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	05/07/09, plasma
 *		Add explict FALSE AllowGate and AllowRecall flags for FeluccaDungeons.
 *	04/26/09, plasma
 *		Added "Moongates" to the IsGuardedRegion method
 *		Fixed copy/paste error, put "keep" on the end of "terra" :)
 *		Added new custom region priority, GreenAcres 0x1
 *	04/24/09, plasma
 *		Add support for reading / writing 3d rects
 *	04/23/09, plasma
 *		- Added the ability to load/upgrade from a region in the XML file									 
 *		- Refactored the clone prop into method so it can be called from elsewhere
 *		- Added cloning of 3D regions as well as 2D regions
 *		- Created new methods that will determine if a region being cloned should be guarded / dungeon stuff in the case
 *			it was loaded from XML and therefore just a base "Region"
 *	4/9/09, Adam
 *		Add BlockLooting rule for the region
 *	6/30/08, Adam
 *		Added a 'fixup' in Deserialize for the m_RestrictedSkills and m_RestrictedSpells BitArrays if the underlying Tables have changed.
 *	2/24/08, plasma
 *		Added factions Capture Area
 *	1/27/08, Adam
 *		- Add the following Region Priorities: MoongateHouseblockers, TownPriorityLow
 *		- Add and/or serialize the following variables: Map m_TargetMap, m_GoLocation, m_MinZ, m_MaxZ,
 *		- Use the Items SendMessage for sending status to staf
 *		- Convert IsDungeonRules from a bool to a bitflag
 *  12/03/07, plasma
 *    Finish off custom region cloning from both custom and normal regions 
 *	7/28/07, Adam
 *		- Add GhostBlindness property so that regions can override ghost blindness for special
 *		events.
 *		- Add support for Custom Regions that are a HousingRegion superset
 *	01/11/07, Pix
 *		Changes so we can subclass RegionControl.
 *  01/08/07, Kit
 *      Final exception handling protection.
 *  01/07/07, Kit
 *      Added specific protection into IsRestrictedSpell/Skill
 *	01/05/07 - Pix
 *		Added protection around IsRestrictedSpell() and IsRestrictedSkill().
 *		Added Exception logging to try/catches.
 * 11/06/06, Kit
 *		Added Enabled/Disabled bool for controller.
 * 10/30/06, Pix
 *		Added protection for crash relating to BitArray index.
 *  8/19/06, Kit
 *		Added NoExternalHarmful, if set to true, prevent any harmful attacks from out of region.
 *  7/29/06, Kit
 *		Added ArrayList RestrictedTypes for use of blocking equiping or use of any types specified.
 *  6/26/06, Kit
 *		Added AllowTravelSpellsInRegion flag and OverrideMaxFollowers flag and int for followers number change.
 *  6/25/06, Kit
 *		Added RestrictCreatureMagic flag and MagicMsgFailure string for overrideing default cant cast msg.
 *  6/24/06, Kit
 *		Added IsMagicIsolated flag to prevent the casting of any spells into the area from outside.
 *  5/02/06, Kit
 *		Added Music flag for playing of region music on enter/exit.
 *	04/30/06, weaver
 *		Added IsIsolated flag to control visibility of mobiles outside of the region to those within.
 *	05/03/05, Kit
 *		Added LogOutDelay for Inn's
 *		Added Inn Support
 *	05/02/05, Kit
 *		Added toggle for showing iob zone messages, as well as indivitual gate/recall travel disallows
 *		Added ISIOBStronghold flag
 *	04/30/05, Kit
 *		Added EnterArea() function call for entering regions via x/y vs mouse
 *		Added AllowTravel and initial IOB region support
 *	04/29/05, Kitaras
 *		Initial system
 */

using Server.Diagnostics;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    [Flags]
    public enum RegionFlag
    {
        None = 0x00000000,
        AllowHousing = 0x00000001,
        CanUseStuckMenu = 0x00000002,
        ShowEnterMessage = 0x00000004,
        ShowExitMessage = 0x00000008,
        CannotEnter = 0x00000010,
        CanUsePotions = 0x00000020,
        IsGuarded = 0x00000040,
        NoMurderCounts = 0x00000080,
        CanRessurect = 0x00000100,
        IOBArea = 0x00000200,
        AllowGate = 0x00000400,
        AllowRecall = 0x00000800,
        ShowIOBMessage = 0x00001000,
        IsIsolated = 0x00002000,                // wea: controls outside mobile visibility
        Music = 0x00004000,
        IsMagicIsolated = 0x00008000,
        RestrictCreatureMagic = 0x00010000,
        AllowTravelSpellsInRegion = 0x00020000,
        OverrideMaxFollowers = 0x00040000,
        NoExternalHarmful = 0x00080000,
        NoGhostBlindness = 0x00100000,          // adam: ghosts go blind in this region
        UseHouseRules = 0x00200000,             // adam: implement some house region systems
        UseDungeonRules = 0x00400000,           // adam: Convert IsDungeonRules to flag
        IsCaptureArea = 0x00800000,             // plasma: factions sigil capture region
        BlockLooting = 0x01000000,              // adam: disallow looting in this region
        UseJailRules = 0x02000000,              // adam: implement jail region systems
        UseGreenAcresRules = 0x04000000,        // adam: implement GreenAcres region systems
        UseAngelIslandRules = 0x08000000,       // adam: implement AngelIsland region systems
        IsSmartGuards = 0x10000000,             // adam: use smart guards against players bank griefing
    }

    public enum CustomRegionPriority
    {
        HighestPriority = 0x96,
        HousePriority = 0x96,
        HighPriority = 0x90,
        MediumPriority = 0x64,
        LowPriority = 0x60,
        InnPriority = 0x33,
        TownPriority = 0x32,
        TownPriorityLow = 0x31,         // Jhelom Islands
        MoongateHouseblockers = 0x28,
        GreenAcres = 0x1,
        LowestPriority = 0x0
    }

    public class RegionControl : Item
    {
        #region Flags

        public bool GetFlag(RegionFlag flag)
        {
            return ((m_Flags & flag) != 0);
        }

        public void SetFlag(RegionFlag flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        public RegionFlag Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        public enum RegionRuleSet
        {
            Custom,
            Dungeon,
            House,
            Jail,
            GreenAcres,
            AngelIsland
        }

        // adam: we no longer use regions for every different kind of area, but instead use the notion
        //	of 'rule sets'. 
        [CommandProperty(AccessLevel.GameMaster)]
        public RegionRuleSet RuleSet
        {
            get
            {   // show the rule that is currently set (may not be combined)
                RegionRuleSet rules = RegionRuleSet.Custom;
                if (GetFlag(RegionFlag.UseDungeonRules)) rules = RegionRuleSet.Dungeon;
                if (GetFlag(RegionFlag.UseHouseRules)) rules = RegionRuleSet.House;
                if (GetFlag(RegionFlag.UseJailRules)) rules = RegionRuleSet.Jail;
                if (GetFlag(RegionFlag.UseGreenAcresRules)) rules = RegionRuleSet.GreenAcres;
                if (GetFlag(RegionFlag.UseAngelIslandRules)) rules = RegionRuleSet.AngelIsland;
                return rules;
            }
            set
            {   // clear all but the one set
                SetFlag(RegionFlag.UseDungeonRules, value == RegionRuleSet.Dungeon);
                SetFlag(RegionFlag.UseHouseRules, value == RegionRuleSet.House);
                SetFlag(RegionFlag.UseJailRules, value == RegionRuleSet.Jail);
                SetFlag(RegionFlag.UseGreenAcresRules, value == RegionRuleSet.GreenAcres);
                SetFlag(RegionFlag.UseAngelIslandRules, value == RegionRuleSet.AngelIsland);
                UpdateRegion();
            }
        }

        // no command access - use RuleSet
        public bool UseDungeonRules
        {
            get { return GetFlag(RegionFlag.UseDungeonRules); }
            set { SetFlag(RegionFlag.UseDungeonRules, value); UpdateRegion(); }
        }

        // no command access - use RuleSet
        public bool UseHouseRules
        {
            get { return GetFlag(RegionFlag.UseHouseRules); }
            set { SetFlag(RegionFlag.UseHouseRules, value); UpdateRegion(); }
        }

        // no command access - use RuleSet
        public bool UseJailRules
        {
            get { return GetFlag(RegionFlag.UseJailRules); }
            set { SetFlag(RegionFlag.UseJailRules, value); UpdateRegion(); }
        }

        // no command access - use RuleSet
        public bool UseGreenAcresRules
        {
            get { return GetFlag(RegionFlag.UseGreenAcresRules); }
            set { SetFlag(RegionFlag.UseGreenAcresRules, value); UpdateRegion(); }
        }

        // no command access - use RuleSet
        public bool UseAngelIslandRules
        {
            get { return GetFlag(RegionFlag.UseAngelIslandRules); }
            set { SetFlag(RegionFlag.UseAngelIslandRules, value); UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsGuarded
        {
            get
            {
                return GetFlag(RegionFlag.IsGuarded);
            }
            set
            {
                SetFlag(RegionFlag.IsGuarded, value);
                UpdateRegion();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsSmartGuards
        {
            get
            {
                return GetFlag(RegionFlag.IsSmartGuards);
            }
            set
            {
                SetFlag(RegionFlag.IsSmartGuards, value);
                UpdateRegion();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowHousing
        {
            get { return GetFlag(RegionFlag.AllowHousing); }
            set { SetFlag(RegionFlag.AllowHousing, value); UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoRecallInto
        {
            get { return GetFlag(RegionFlag.AllowRecall); }
            set { SetFlag(RegionFlag.AllowRecall, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoGateInto
        {
            get { return GetFlag(RegionFlag.AllowGate); }
            set { SetFlag(RegionFlag.AllowGate, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CannotEnter
        {
            get { return GetFlag(RegionFlag.CannotEnter); }
            set { SetFlag(RegionFlag.CannotEnter, value); }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanUseStuckMenu
        {
            get { return GetFlag(RegionFlag.CanUseStuckMenu); }
            set { SetFlag(RegionFlag.CanUseStuckMenu, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanRessurect
        {
            get { return GetFlag(RegionFlag.CanRessurect); }
            set { SetFlag(RegionFlag.CanRessurect, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowEnterMessage
        {
            get { return GetFlag(RegionFlag.ShowEnterMessage); }
            set { SetFlag(RegionFlag.ShowEnterMessage, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowExitMessage
        {
            get { return GetFlag(RegionFlag.ShowExitMessage); }
            set { SetFlag(RegionFlag.ShowExitMessage, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoMurderZone
        {
            get { return GetFlag(RegionFlag.NoMurderCounts); }
            set { SetFlag(RegionFlag.NoMurderCounts, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanUsePotions
        {
            get { return GetFlag(RegionFlag.CanUsePotions); }
            set { SetFlag(RegionFlag.CanUsePotions, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IOBZone
        {
            get { return GetFlag(RegionFlag.IOBArea); }
            set { SetFlag(RegionFlag.IOBArea, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowIOBMsg
        {
            get { return GetFlag(RegionFlag.ShowIOBMessage); }
            set { SetFlag(RegionFlag.ShowIOBMessage, value); }
        }

        // wea: added IsIsolated flag to control outside mobile visibility
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsIsolated
        {
            get
            {
                return GetFlag(RegionFlag.IsIsolated);
            }
            set
            {
                SetFlag(RegionFlag.IsIsolated, value);
                UpdateRegion();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PlayMusic
        {
            get
            {
                return GetFlag(RegionFlag.Music);
            }
            set
            {
                SetFlag(RegionFlag.Music, value);
                UpdateRegion();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsMagicIsolated
        {
            get { return GetFlag(RegionFlag.IsMagicIsolated); }
            set { SetFlag(RegionFlag.IsMagicIsolated, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestrictCreatureMagic
        {
            get { return GetFlag(RegionFlag.RestrictCreatureMagic); }
            set { SetFlag(RegionFlag.RestrictCreatureMagic, value); UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowTravelSpellsInRegion
        {
            get { return GetFlag(RegionFlag.AllowTravelSpellsInRegion); }
            set { SetFlag(RegionFlag.AllowTravelSpellsInRegion, value); UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool OverrideMaxFollowers
        {
            get { return GetFlag(RegionFlag.OverrideMaxFollowers); }
            set { SetFlag(RegionFlag.OverrideMaxFollowers, value); UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoExternalHarmful
        {
            get { return GetFlag(RegionFlag.NoExternalHarmful); }
            set { SetFlag(RegionFlag.NoExternalHarmful, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GhostBlindness
        {
            get { return !GetFlag(RegionFlag.NoGhostBlindness); }
            set { SetFlag(RegionFlag.NoGhostBlindness, !value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CaptureArea
        {
            get { return GetFlag(RegionFlag.IsCaptureArea); }
            set { SetFlag(RegionFlag.IsCaptureArea, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BlockLooting
        {
            get { return GetFlag(RegionFlag.BlockLooting); }
            set { SetFlag(RegionFlag.BlockLooting, value); }
        }

        #endregion

        #region SPEECH COMMAND SYSTEM
        private enum TokenTypes
        {
            String,
            Char,
        }
        private enum Field
        {
            Name,
            Track,
        }
        // label database
        private Dictionary<Item, object[]> m_ItemDatabase = new Dictionary<Item, object[]>();
        // key value pairs : [keyword1, keyword2...] [action1, action2...]
        private Dictionary<string, object[]> m_KeywordDatabase = new Dictionary<string, object[]>();
        #endregion

        // version 13
        private Map m_TargetMap = Map.Felucca;
        private Point3D m_GoLocation;
        private int m_MinZ = sbyte.MinValue;
        private int m_MaxZ = sbyte.MaxValue + 1;

        private CustomRegion m_Region;
        private IOBAlignment m_IOBAlignment;
        private RegionFlag m_Flags;
        private BitArray m_RestrictedSpells;
        private BitArray m_RestrictedSkills;

        private string m_RegionName;
        private string m_RestrictedMagicMsg;
        private CustomRegionPriority m_Priority;

        private ArrayList m_Coords;
        private ArrayList m_InnBounds;  //INN corridantes

        private ArrayList m_Types;      //used for storeing types to not allow equiping of/use of.

        private int m_LightLevel;
        private int m_MaxFollowers;
        private MusicName m_Music = MusicName.Invalid;


        private TimeSpan m_PlayerLogoutDelay = TimeSpan.FromMinutes(5.0);
        private TimeSpan m_InnLogoutDelay = TimeSpan.Zero;

        public TimeSpan m_Delay = TimeSpan.FromMinutes(5.0); // time between iob enter kin messages
        public DateTime m_Msg;

        private bool m_Enabled = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D GoLocation
        {
            get { return m_GoLocation; }
            set { m_GoLocation = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap
        {
            get { return m_TargetMap; }
            set { m_TargetMap = value; UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MinZ
        {
            get { return m_MinZ; }
            set { m_MinZ = value; UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxZ
        {
            get { return m_MaxZ; }
            set { m_MaxZ = value; UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Enabled
        {
            get { return m_Enabled; }
            set
            {
                m_Enabled = value;

                if (value)
                    UpdateRegion();
                else
                {
                    if (m_Region != null)
                    {
                        Region.RemoveRegion(m_Region);
                    }
                }

                InvalidateProperties();
            }
        }

        private void DupeCustomBits(Region r)
        {
            //pla: replicate custom region
            if (r is CustomRegion)
            {
                RegionControl rc = (r as CustomRegion).GetRegionControler();

                if (rc != null)
                {
                    //copy all the flags and restricted stuff
                    this.m_Flags = rc.m_Flags;
                    this.m_RestrictedSpells = new BitArray(rc.m_RestrictedSpells);
                    this.m_RestrictedSkills = new BitArray(rc.m_RestrictedSkills);
                    this.m_RestrictedMagicMsg = rc.m_RestrictedMagicMsg;
                    this.m_InnLogoutDelay = rc.InnLogoutDelay;
                    this.m_IOBAlignment = rc.IOBAlign;
                    this.m_MaxFollowers = rc.MaxFollowerSlots;
                    this.m_Music = r.Music;
                    this.SetFlag(RegionFlag.Music, (r.Music != MusicName.Invalid));

                    if (rc.LightLevel > 0)
                        this.m_LightLevel = rc.LightLevel;

                    this.m_Types.Clear();
                    if (rc.m_Types != null && rc.m_Types.Count > 0)
                    {
                        ArrayList t = rc.m_Types;
                        for (int i = 0; i < rc.m_Types.Count; ++i)
                            if (t[i] is string)
                                this.m_Types.Add((string)t[i]);
                    }
                }
                else
                {
                    this.SendMessage(String.Format("Region controller for custom region '" + r.Name + "' could not be found.\n\rCustom properties and flags have not been cloned successfully"));
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string CloneFromRegion
        {
            get
            {
                return "Enter existing region name here";
            }

            set
            {
                Region r = Region.GetByName(value, Map);
                if (r != null)
                {
                    CloneFromRegionObject(r);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string LoadFromXML
        {
            get { return "Enter xml region name here"; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    //Load region from the xml
                    string output = string.Empty;
                    Region region = null;
                    region = Region.CreateRegionFromXML(ref output, value);
                    this.SendMessage(output);
                    if (region == null)
                    {
                        this.SendMessage("Loading failed");
                        return;
                    }
                    //upgrade it do drdt
                    CloneFromRegionObject(region, true);
                }
            }
        }

        #region loading / cloning

        public void CloneFromRegionObject(Region r)
        {
            CloneFromRegionObject(r, false);
        }

        public void CloneFromRegionObject(Region r, bool loadedFromXML)
        {
            if (r != null)
            {
                //Clear flags before copy
                m_Flags = RegionFlag.None;

                // if we have a custom region, get this special bits
                if (r is CustomRegion)
                    DupeCustomBits(r);

                if (r.Coords != null && r.Coords.Count > 0)
                {
                    m_Coords.Clear();
                    ArrayList c = r.Coords;

                    for (int i = 0; i < c.Count; i++)
                    {
                        if (c[i] is Rectangle2D)
                            m_Coords.Add((Rectangle2D)c[i]);
                        //plasma: add missing 3d rects lol!
                        if (c[i] is Rectangle3D)
                            m_Coords.Add((Rectangle3D)c[i]);
                    }
                }

                if (r.InnBounds != null && r.InnBounds.Count > 0)
                {
                    m_InnBounds.Clear();
                    ArrayList c = r.InnBounds;

                    for (int i = 0; i < c.Count; i++)
                    {
                        if (c[i] is Rectangle2D)
                            m_InnBounds.Add((Rectangle2D)c[i]);
                        if (c[i] is Rectangle3D)
                            m_InnBounds.Add((Rectangle3D)c[i]);
                    }
                }

                //default these to true for non custom regions
                this.SetFlag(RegionFlag.CanUsePotions, true);
                this.SetFlag(RegionFlag.CanRessurect, true);
                this.SetFlag(RegionFlag.CanUseStuckMenu, true);
                this.SetFlag(RegionFlag.UseHouseRules, r is HouseRegion);
                this.SetFlag(RegionFlag.AllowTravelSpellsInRegion, !IsFeluccaDungeon(r, loadedFromXML) && !(r.IsAngelIslandRules || r.Name.Equals("AngelIsland")));
                //set guards and murder zone
                this.SetFlag(RegionFlag.IsGuarded, IsGuardedRegion(r, loadedFromXML));
                this.SetFlag(RegionFlag.NoMurderCounts, r.IsNoMurderZone);
                //Non custom regions only have a static InnLogoutDelay
                this.m_InnLogoutDelay = Region.InnLogoutDelay;
                //Blank the restricted spells / skills
                this.m_RestrictedSpells = new BitArray(SpellRegistry.Types.Length);
                this.m_RestrictedSkills = new BitArray(SkillInfo.Table.Length);

                //Assign generic props						
                this.m_Priority = MapPriority(r.Priority);
                this.m_Music = r.Music;
                this.SetFlag(RegionFlag.Music, (r.Music != MusicName.Invalid));
                this.m_RegionName = r.Name;
                this.m_TargetMap = r.Map;

                // LightLevel - defualts to 0, use -1 for region to inherit and use light level/cycle across the world.            
                if (this.m_LightLevel <= 0)
                    this.m_LightLevel = -1;

                // Dungeon - Setting to True will set the regions light level to that of the dungon light level, has no other effects.
                this.SetFlag(RegionFlag.UseDungeonRules, IsFeluccaDungeon(r, loadedFromXML));
                this.m_GoLocation = r.GoLocation;
                this.m_MaxZ = r.MaxZ;
                this.m_MinZ = r.MinZ;

                //Plasma:  Felucca dungeons and AI need recall and gate OFF!
                if (IsFeluccaDungeon(r, loadedFromXML) || r.IsAngelIslandRules)
                {
                    NoGateInto = true;
                    NoRecallInto = true;
                }

                UpdateRegion();
                this.SendMessage(String.Format("Region cloned from '" + r.Name + "'."));
            }
            else
            {
                this.SendMessage(String.Format("No region by the name '" + r.Name + "' found."));
            }
        }

        private bool IsGuardedRegion(Region r, bool loadedFromXML)
        {
            if (r is GuardedRegion && (r as GuardedRegion).IsGuarded) return true;
            if (!loadedFromXML) return false;
            switch (r.Name) //plasma: note we only include the non-iob cities as these are never guarded or are now faction cities and thus have their guards controlled
            {
                case "Cove":
                case "Britain":
                case "Minoc":
                case "Trinsic":
                case "Skara Brae":
                case "Nujel'm":
                case "Moonglow":
                case "Magincia":
                case "Delucia":
                case "Papua":
                case "Moongates":
                    return true;
            }
            return false;
        }

        public static bool IsFeluccaDungeon(Region r)
        {
            bool a = IsFeluccaDungeon(r, true);
            bool b = IsFeluccaDungeon(r, false);
            return a | b;
        }

        private static bool IsFeluccaDungeon(Region r, bool loadedFromXML)
        {
            if (!loadedFromXML) return false;
            switch (r.Name)
            {
                case "Covetous":
                case "Despise":
                case "Destard":
                case "Hythloth":
                case "Shame":
                case "Wrong":
                case "Terathan Keep":
                case "Fire":
                case "Ice":
                case "Orc Cave":
                    return true;
            }
            return false;
        }
        #endregion

        private CustomRegionPriority MapPriority(int priority)
        {
            if (Enum.IsDefined(typeof(CustomRegionPriority), priority))
                return (CustomRegionPriority)priority;
            else
                return CustomRegionPriority.HighestPriority;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan InnLogoutDelay
        {
            get { return m_InnLogoutDelay; }
            set { m_InnLogoutDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan PlayerLogoutDelay
        {
            get { return m_PlayerLogoutDelay; }
            set { m_PlayerLogoutDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlign
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        public CustomRegion MyRegion
        {
            get { return m_Region; }
        }

        public BitArray RestrictedSpells
        {
            get { return m_RestrictedSpells; }
        }

        public BitArray RestrictedSkills
        {
            get { return m_RestrictedSkills; }
        }

        public ArrayList RestrictedTypes
        {
            get { return m_Types; }
            set { m_Types = value; }
        }

        public ArrayList Coords
        {
            get { return m_Coords; }
            set { m_Coords = value; UpdateRegion(); }
        }

        public ArrayList InnBounds
        {
            get { return m_InnBounds; }
            set { m_InnBounds = value; }
        }

        public bool IsInInn(Point3D p)
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

        [CommandProperty(AccessLevel.GameMaster)]
        public string RegionName
        {
            get { return m_RegionName; }
            set { m_RegionName = value; UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MagicMsgFailure
        {
            get { return m_RestrictedMagicMsg; }
            set { m_RestrictedMagicMsg = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LightLevel
        {
            get { return m_LightLevel; }
            set { m_LightLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxFollowerSlots
        {
            get { return m_MaxFollowers; }
            set { m_MaxFollowers = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CustomRegionPriority Priority
        {
            get { return m_Priority; }
            set { m_Priority = value; UpdateRegion(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MusicName MusicTrack
        {
            get { return m_Music; }
            set
            {
                // should this be pushed down to base region?
                if (MyRegion != null && PlayMusic == true && m_Region.Music != value)
                {
                    foreach (System.Collections.Generic.KeyValuePair<Serial, Mobile> kvp in MyRegion.Players)
                        if (kvp.Value != null && kvp.Value.NetState != null)
                            MyRegion.StopMusic(kvp.Value);

                    m_Region.Music = value;

                    foreach (System.Collections.Generic.KeyValuePair<Serial, Mobile> kvp in MyRegion.Players)
                        if (kvp.Value != null && kvp.Value.NetState != null)
                            MyRegion.PlayMusic(kvp.Value);
                }

                m_Music = value; UpdateRegion();
            }
        }

        [Constructable]
        public RegionControl()
            : base(3026)
        {
            Visible = false;
            Movable = false;
            Enabled = true;
            Name = "Region Controller";
            m_RegionName = "New Region";
            m_RestrictedMagicMsg = null;
            m_Priority = CustomRegionPriority.HighPriority;

            m_RestrictedSpells = new BitArray(SpellRegistry.Types.Length);
            m_RestrictedSkills = new BitArray(SkillInfo.Table.Length);

            m_Msg = DateTime.UtcNow + m_Delay;

            Coords = new ArrayList();
            InnBounds = new ArrayList();
            RestrictedTypes = new ArrayList();

            m_Music = MusicName.Invalid;
            UpdateRegion();
        }

        [Constructable]
        public RegionControl(Rectangle2D rect)
            : base(3026)
        {
            Coords = new ArrayList();
            InnBounds = new ArrayList();
            RestrictedTypes = new ArrayList();

            Coords.Add(rect);

            m_RestrictedSpells = new BitArray(SpellRegistry.Types.Length);
            m_RestrictedSkills = new BitArray(SkillInfo.Table.Length);

            Visible = false;
            Movable = false;
            Enabled = true;
            Name = "Region Controller";
            m_RegionName = "Custom Region";
            m_RestrictedMagicMsg = null;
            m_Priority = CustomRegionPriority.HighPriority;
            m_Music = MusicName.Invalid;
            UpdateRegion();
        }

        public RegionControl(Serial serial)
            : base(serial)
        {
        }


        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                m.CloseGump(typeof(RegionControlGump));
                m.SendGump(new RegionControlGump(this));
                m.SendMessage("Don't forget to props this object for more options!");

                m.CloseGump(typeof(RemoveAreaGump));
                m.SendGump(new RemoveAreaGump(this));

                m.CloseGump(typeof(RemoveInnGump));
                m.SendGump(new RemoveInnGump(this));
            }
        }

        public override void OnMapChange()
        {
            UpdateRegion();
            base.OnMapChange();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 15;
            writer.Write((int)version); // version

            // version 15
            #region SPEECH COMMAND SYSTEM
            writer.Write(m_KeywordDatabase.Count);
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is string)
                    {
                        writer.Write((int)TokenTypes.String);
                        writer.Write(o as string);
                    }
                    else if (o is Char)
                    {
                        writer.Write((int)TokenTypes.Char);
                        writer.Write((char)o);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }

            writer.Write(m_ItemDatabase.Count);
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is Field)
                    {
                        writer.Write((int)((Field)o));
                    }
                    else if (o is string)
                    {
                        writer.Write(o as string);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }
            #endregion

            //version 14
            WriteRect3DArray(writer, m_Coords); //just writes out the 3d ones in the array
            WriteRect3DArray(writer, m_InnBounds); //just writes out the 3d ones in the array

            // version 13
            writer.Write(m_TargetMap);
            writer.Write(m_GoLocation);
            writer.Write(m_MinZ);
            writer.Write(m_MaxZ);

            // version 12
            writer.Write(m_Enabled);

            writer.Write(m_Types.Count);

            for (int i = 0; i < m_Types.Count; ++i)
                writer.Write((string)m_Types[i]);

            writer.Write(m_MaxFollowers);
            writer.Write(m_RestrictedMagicMsg);
            writer.Write((int)m_Music);

            WriteRect2DArray(writer, m_InnBounds);
            writer.Write((TimeSpan)m_InnLogoutDelay);

            writer.Write((TimeSpan)m_PlayerLogoutDelay);

            // Adam: Removed in version 13
            //writer.Write(m_IsDungeon);
            writer.Write((int)m_IOBAlignment);

            writer.Write((int)m_LightLevel);

            WriteRect2DArray(writer, Coords);
            writer.Write((int)m_Priority);

            //writer.Write( m_Area );
            WriteBitArray(writer, m_RestrictedSpells);
            WriteBitArray(writer, m_RestrictedSkills);

            writer.Write((int)m_Flags);
            writer.Write(m_RegionName);
        }

        #region Serialization Helpers
        public static void WriteBitArray(GenericWriter writer, BitArray ba)
        {
            writer.Write(ba.Length);

            for (int i = 0; i < ba.Length; i++)
            {
                writer.Write(ba[i]);
            }
            return;
        }

        public static BitArray ReadBitArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            BitArray newBA = new BitArray(size);

            for (int i = 0; i < size; i++)
            {
                newBA[i] = reader.ReadBool();
            }

            return newBA;
        }

        public static void WriteRect2DArray(GenericWriter writer, ArrayList ary)
        {
            //create a temp list and clean up 
            ArrayList temp = new ArrayList(ary);
            for (int i = temp.Count - 1; i >= 0; --i)
            {
                if (!(temp[i] is Rectangle2D))
                {
                    temp.RemoveAt(i);
                }
            }

            writer.Write(temp.Count);

            for (int i = 0; i < temp.Count; i++)
            {
                writer.Write((Rectangle2D)temp[i]); //Rect2D
            }
            return;
        }

        public static ArrayList ReadRect2DArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            ArrayList newAry = new ArrayList();

            for (int i = 0; i < size; i++)
            {
                newAry.Add(reader.ReadRect2D());
            }

            return newAry;
        }


        public static void WriteRect3DArray(GenericWriter writer, ArrayList ary)
        {
            //create a temp list and clean up 
            ArrayList temp = new ArrayList(ary);
            for (int i = temp.Count - 1; i >= 0; --i)
            {
                if (!(temp[i] is Rectangle3D))
                {
                    temp.RemoveAt(i);
                }
            }
            writer.Write(temp.Count);

            for (int i = 0; i < temp.Count; i++)
            {
                //Write the two 3d points
                writer.Write(((Rectangle3D)temp[i]).Start);
                writer.Write(((Rectangle3D)temp[i]).End);
            }
            return;
        }

        public static ArrayList ReadRect3DArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            ArrayList newAry = new ArrayList();

            for (int i = 0; i < size; i++)
            {
                newAry.Add(new Rectangle3D(reader.ReadPoint3D(), reader.ReadPoint3D()));
            }

            return newAry;
        }

        #endregion
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 15:
                    {
                        #region SPEECH COMMAND SYSTEM
                        // read the keyword database
                        int kwdb_count = reader.ReadInt();
                        for (int ix = 0; ix < kwdb_count; ix++)
                        {
                            string key = reader.ReadString();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                switch ((TokenTypes)reader.ReadInt())
                                {
                                    case TokenTypes.Char:
                                        list.Add((Char)reader.ReadChar());
                                        continue;

                                    case TokenTypes.String:
                                        list.Add(reader.ReadString());
                                        continue;
                                }
                            }
                            m_KeywordDatabase.Add(key, list.ToArray());
                        }

                        // read the item database
                        int idb_count = reader.ReadInt();
                        for (int ix = 0; ix < idb_count; ix++)
                        {
                            Item key = reader.ReadItem();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                Field field = (Field)reader.ReadInt();
                                list.Add(field);
                                switch (field)
                                {
                                    case Field.Track:
                                        continue;

                                    case Field.Name:
                                        list.Add(reader.ReadString());
                                        jx++;
                                        continue;
                                }
                            }

                            m_ItemDatabase.Add(key, list.ToArray());
                        }
                        goto case 14;
                        #endregion
                    }
                case 14:
                    {
                        //version 14
                        m_Coords = ReadRect3DArray(reader);
                        m_InnBounds = ReadRect3DArray(reader);
                        goto case 13;
                    }
                case 13:
                    {
                        m_TargetMap = reader.ReadMap();
                        m_GoLocation = reader.ReadPoint3D();
                        m_MinZ = reader.ReadInt();
                        m_MaxZ = reader.ReadInt();
                        goto case 12;
                    }
                case 12:
                    {
                        m_Enabled = reader.ReadBool();
                        goto case 11;
                    }
                case 11:
                    {
                        int size = reader.ReadInt();
                        m_Types = new ArrayList(size);

                        for (int i = 0; i < size; ++i)
                        {
                            string typeName = reader.ReadString();
                            m_Types.Add(typeName);

                        }
                        goto case 10;
                    }

                case 10:
                    {
                        m_MaxFollowers = reader.ReadInt();
                        goto case 9;
                    }
                case 9:
                    {
                        m_RestrictedMagicMsg = reader.ReadString();
                        goto case 8;
                    }
                case 8:
                    {
                        m_Music = (MusicName)reader.ReadInt();
                        goto case 7;
                    }

                case 7:
                    {
                        if (m_InnBounds == null)
                        {
                            m_InnBounds = ReadRect2DArray(reader);
                        }
                        else
                        {
                            m_InnBounds.AddRange(ReadRect2DArray(reader));
                        }
                        m_InnLogoutDelay = reader.ReadTimeSpan();

                        goto case 6;
                    }
                case 6:
                    {
                        m_PlayerLogoutDelay = reader.ReadTimeSpan();
                        goto case 5;
                    }
                case 5:
                    {
                        if (version < 13)
                        {   // converted to a flag
                            bool m_IsDungeon = (bool)reader.ReadBool();
                            UseDungeonRules = m_IsDungeon;
                        }
                        goto case 4;
                    }
                case 4:
                    {
                        m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        goto case 3;
                    }

                case 3:
                    {
                        m_LightLevel = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        goto case 1;
                    }
                case 1:
                    {
                        if (Coords == null)
                        {
                            Coords = ReadRect2DArray(reader);
                        }
                        else
                        {
                            Coords.AddRange(ReadRect2DArray(reader));
                        }
                        m_Priority = (CustomRegionPriority)reader.ReadInt();

                        m_RestrictedSpells = ReadBitArray(reader);
                        m_RestrictedSkills = ReadBitArray(reader);

                        m_Flags = (RegionFlag)reader.ReadInt();

                        m_RegionName = reader.ReadString();
                        break;
                    }
                case 0:
                    {
                        Coords = new ArrayList();

                        Coords.Add(reader.ReadRect2D());
                        m_RestrictedSpells = ReadBitArray(reader);
                        m_RestrictedSkills = ReadBitArray(reader);

                        m_Flags = (RegionFlag)reader.ReadInt();

                        m_RegionName = reader.ReadString();
                        break;
                    }
            }
            if (version < 12)
            {
                m_Enabled = true;
            }

            if (version < 11)
            {
                m_Types = new ArrayList();
            }

            if (version < 8)
            {
                m_Music = MusicName.Invalid;
            }

            // fixup this table if Skills have been added or removed.
            if (SkillInfo.Table.Length != m_RestrictedSkills.Count)
            {
                BitArray temp = new BitArray(SkillInfo.Table.Length);
                int MaxIterations = Math.Min(temp.Length, m_RestrictedSkills.Count);
                for (int ix = 0; ix < MaxIterations; ix++)
                    temp[ix] = m_RestrictedSkills[ix];

                m_RestrictedSkills = temp;
            }

            // fixup this table if Spells have been added or removed.
            if (SpellRegistry.Types.Length != m_RestrictedSpells.Count)
            {
                BitArray temp = new BitArray(SpellRegistry.Types.Length);
                int MaxIterations = Math.Min(temp.Length, m_RestrictedSpells.Count);
                for (int ix = 0; ix < MaxIterations; ix++)
                    temp[ix] = m_RestrictedSpells[ix];

                m_RestrictedSkills = temp;
            }

            UpdateRegion();

        }

        public virtual CustomRegion CreateRegion(RegionControl rc, Map map)
        {
            return new CustomRegion(rc, map);
        }

        public void UpdateRegion()
        {
            if (Enabled)
            {
                if (Coords != null && Coords.Count != 0)
                {
                    if (m_Region == null)
                    {
                        //Pix: This change was needed so that classes derived from
                        // RegionControl could have a different CustomRegion (i.e. a different 
                        // class derived from CustomRegion)
                        //m_Region = new CustomRegion( this, this.Map );
                        m_Region = CreateRegion(this, this.TargetMap);
                    }

                    Region.RemoveRegion(m_Region);

                    m_Region.Coords = Coords;

                    m_Region.InnBounds = InnBounds;

                    m_Region.IsGuarded = (GetFlag(RegionFlag.IsGuarded));

                    m_Region.Name = m_RegionName;

                    m_Region.Priority = (int)m_Priority;

                    m_Region.Map = m_TargetMap;

                    m_Region.MinZ = m_MinZ;

                    m_Region.MaxZ = m_MaxZ;

                    m_Region.GoLocation = m_GoLocation;

                    m_Region.Music = m_Music;

                    Region.AddRegion(m_Region);
                }
            }

            return;
        }

        public static int GetRegistryNumber(ISpell s)
        {
            Type[] t = SpellRegistry.Types;

            if (t != null)
            {
                for (int i = 0; i < t.Length; i++)
                {
                    if (s.GetType() == t[i])
                        return i;
                }

            }

            return -1;
        }

        public bool IsRestrictedSpell(ISpell s, Mobile m)
        {
            bool bReturn = false;

            try
            {
                int regNum = GetRegistryNumber(s);
                bool Invalid = false;

                if (regNum < 0) //Happens with unregistered Spells
                    Invalid = true;

                if (regNum >= m_RestrictedSpells.Length)
                    Invalid = true;

                if (Invalid && m is PlayerMobile)
                {
                    try
                    {
                        Account a = m.Account as Account;
                        LogHelper Logger = new LogHelper("InvalidPacket.log", false, true);
                        Logger.Log(LogType.Text, "--------New Invalid Packet Entry---------");
                        Logger.Log(LogType.Text,
                                    string.Format("Spell Registered Number: {0} From Player: {1} Account: {2}, IP: {3}, Version: {4}",
                                                    regNum,
                                                    m.Name,
                                                    ((a != null) ? a.Username : ""),
                                                    ((m.NetState != null) ? m.NetState.ToString() : ""),
                                                    m.NetState.Version == null ? "(null)" :
                                                    m.NetState.Version.ToString()));
                        Logger.Finish();
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogException(e, "Caught error in Invalid packet handling.");
                    }
                    return false;
                }
                if (!Invalid)
                    bReturn = m_RestrictedSpells[regNum];
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

            return bReturn;
        }

        public bool IsRestrictedSkill(int skill, Mobile m)
        {
            bool bReturn = false;

            try
            {
                bool Invalid = false;

                if (skill < 0)
                    Invalid = true;

                if (skill >= m_RestrictedSkills.Length)
                    Invalid = true;

                if (Invalid && m is PlayerMobile)
                {
                    try
                    {
                        Account a = m.Account as Account;
                        LogHelper Logger = new LogHelper("InvalidPacket.log", false, true);
                        Logger.Log(LogType.Text, "--------New Invalid Packet Entry---------");
                        Logger.Log(LogType.Text,
                                     string.Format("Skill Registered Number: {0} From Player: {1} Account: {2}, IP: {3}, Version: {4}",
                                                 skill,
                                                 m.Name,
                                                 ((a != null) ? a.Username : "a is null"),
                                                    ((m.NetState != null) ? m.NetState.ToString() : ""),
                                                    m.NetState.Version == null ? "(null)" :
                                                    m.NetState.Version.ToString()));
                        Logger.Finish();
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogException(e, "Caught error in Invalid packet handling.");
                    }
                    return false;
                }
                if (!Invalid)
                    bReturn = m_RestrictedSkills[skill];
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

            return bReturn;
        }

        public void ChooseArea(Mobile m)
        {
            BoundingBoxPicker.Begin(m, new BoundingBoxCallback(CustomRegion_Callback), this);
        }

        public void ChooseInnArea(Mobile m)
        {
            BoundingBoxPicker.Begin(m, new BoundingBoxCallback(CustomInnRegion_Callback), this);
        }

        private static void CustomRegion_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            DoChooseArea(from, map, start, end, state, false);
        }

        private static void CustomInnRegion_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            DoChooseArea(from, map, start, end, state, true);
        }


        protected static void DoChooseArea(Mobile from, Map map, Point3D start, Point3D end, object control, bool inn)
        {
            RegionControl r = (RegionControl)control;
            if (inn == false)
            {
                Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
                r.m_Coords.Add(rect);
            }
            if (inn)
            {
                Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
                r.m_InnBounds.Add(rect);
            }

            r.UpdateRegion();
        }

        public void EnterArea(Mobile from, Map map, Rectangle2D rect, object control)
        {
            RegionControl r = (RegionControl)control;
            r.m_Coords.Add(rect);
            r.UpdateRegion();
        }

        public void EnterInnArea(Mobile from, Map map, Rectangle2D rect, object control)
        {
            RegionControl r = (RegionControl)control;
            r.m_InnBounds.Add(rect);
            r.UpdateRegion();
        }

        public override void OnDelete()
        {
            if (m_Region != null)
                Region.RemoveRegion(m_Region);

            base.OnDelete();
        }
    }
}
