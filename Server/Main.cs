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

using Server.Network;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Server
{
    public delegate void Slice();

    public class Core
    {
        public static int ListeningPort = -1;   // default port override
        private static bool m_Crashed;
        private static Thread timerThread;
        private static string m_BaseDirectory;
        private static string m_ExePath;
        private static ArrayList m_DataDirectories = new ArrayList();
        private static Assembly m_Assembly;
        private static Process m_Process;
        private static Thread m_Thread;
        private static bool m_Service;
        private static bool m_Debug;
        private static bool m_Cache = true;
        private static bool m_HaltOnWarning;
        private static bool m_VBdotNET;
        private static MultiTextWriter m_MultiConOut;
        private static bool m_Quiet;

        private static bool m_AOS;
        private static bool m_SE;
        private static bool m_ML;
        private static bool m_UOSP;                     // Siege
        private static bool m_UOTC;                     // Test Center
        private static bool m_UOAI;                     // Angel Island
        private static bool m_UOAR;                     // AI Resurrection
        private static bool m_UOMO;                     // Mortalis
        private static bool m_UOEV;                     // Event Shard
        private static bool m_Building;                 // gives GMs access to certain world building commands during world construction
        private static double m_Publish = 5;            // publish
        private static bool m_Developer;                // developers machine, allows derect login to any server

        private static bool m_Profiling;
        private static DateTime m_ProfileStart;
        private static TimeSpan m_ProfileTime;

        private static MessagePump m_MessagePump;

        public static MessagePump MessagePump
        {
            get { return m_MessagePump; }
            set { m_MessagePump = value; }
        }

        public static Slice Slice;

        public static bool Profiling
        {
            get { return m_Profiling; }
            set
            {
                if (m_Profiling == value)
                    return;

                m_Profiling = value;

                if (m_ProfileStart > DateTime.MinValue)
                    m_ProfileTime += DateTime.Now - m_ProfileStart;

                m_ProfileStart = (m_Profiling ? DateTime.Now : DateTime.MinValue);
            }
        }

        public static TimeSpan ProfileTime
        {
            get
            {
                if (m_ProfileStart > DateTime.MinValue)
                    return m_ProfileTime + (DateTime.Now - m_ProfileStart);

                return m_ProfileTime;
            }
        }

#if !DEBUG
		public static bool Debug { get { return m_Debug; } }
#else
        public static bool Debug { get { return true; } }
#endif

        public static bool Service { get { return m_Service; } }
        public static bool Developer { get { return m_Developer; } }                                    //Disallow direct logins to other servers if we are not a developer
        internal static bool HaltOnWarning { get { return m_HaltOnWarning; } }
        internal static bool VBdotNet { get { return m_VBdotNET; } }
        public static ArrayList DataDirectories { get { return m_DataDirectories; } }
        public static Assembly Assembly { get { return m_Assembly; } set { m_Assembly = value; } }
        public static Process Process { get { return m_Process; } }
        public static Thread Thread { get { return m_Thread; } }
        public static MultiTextWriter MultiConsoleOut { get { return m_MultiConOut; } }

        // import a non-encrypted world
        private static bool m_Import = false;
        public static bool Import { get { return m_Import; } }

        private static string m_Server;
        public static string Server { get { return m_Server; } }

        // perform a onetime upgrade from old non-locking boat holds to the new locking ones
        private static bool m_BoatHoldUpgrade = false;
        public static bool BoatHoldUpgrade { get { return m_BoatHoldUpgrade; } }

        public static readonly bool Is64Bit = (IntPtr.Size == 8);

        private static bool m_MultiProcessor;
        private static int m_ProcessorCount;

        public static bool MultiProcessor { get { return m_MultiProcessor; } }
        public static int ProcessorCount { get { return m_ProcessorCount; } }

        private static bool m_Unix;

        public static bool Unix { get { return m_Unix; } }

        public static string FindDataFile(string path)
        {
            if (m_DataDirectories.Count == 0)
                throw new InvalidOperationException("Attempted to FindDataFile before DataDirectories list has been filled.");

            string fullPath = null;

            for (int i = 0; i < m_DataDirectories.Count; ++i)
            {
                fullPath = Path.Combine((string)m_DataDirectories[i], path);

                if (File.Exists(fullPath))
                    break;

                fullPath = null;
            }

            return fullPath;
        }

        public static string FindDataFile(string format, params object[] args)
        {
            return FindDataFile(String.Format(format, args));
        }

        private static Expansion m_Expansion;
        public static Expansion Expansion
        {
            get { return m_Expansion; }
            set { m_Expansion = value; }
        }

        // Scenario 4: Plague of Despair
        // http://www.uoguide.com/List_of_BNN_Articles_(2002)
        public static DateTime PlagueOfDespair
        {
            get
            {
                // Enemies and Allies - April 11
                return new DateTime(2002, 4, 11);
            }
        }


        // http://www.uoguide.com/Savage_Empire
        // http://uo.stratics.com/database/view.php?db_content=hunters&id=176
        // Savage Empire was the title of an EA-run UO scenario, active from May to July of 2001.
        public static DateTime EraSAVE  // Savage Empire active from May to July of 2001. 
        {
            get
            {
                return new DateTime(2001, 5, 1);
            }
        }

        public static DateTime EraSA    // The Second Age (October 1, 1998) 
        {
            get
            {
                return new DateTime(1998, 10, 1);
            }
        }
        public static DateTime EraREN   // Renaissance (May 4, 2000)
        {
            get
            {
                return new DateTime(2000, 5, 4);
            }
        }
        public static DateTime EraTD    // Third Dawn (March 7, 2001)
        {
            get
            {
                return new DateTime(2001, 3, 7);
            }
        }
        public static DateTime EraLBR   // Lord Blackthorn's Revenge (February 24, 2002)
        {
            get
            {
                return new DateTime(2002, 2, 24);
            }
        }
        public static DateTime EraAOS   // Age of Shadows (February 11, 2003)
        {
            get
            {
                return new DateTime(2003, 2, 11);
            }
        }
        public static DateTime EraSE    // Samurai Empire (November 2, 2004) 
        {
            get
            {
                return new DateTime(2004, 11, 2);
            }
        }
        public static DateTime EraML    // Mondain's Legacy (August 30, 2005) 
        {
            get
            {
                return new DateTime(2005, 8, 30);
            }
        }

        public static DateTime EraABYSS // Stygian Abyss (September 8, 2009) 
        {
            get
            {
                return new DateTime(2009, 9, 9);
            }
        }

        public static DateTime LocalizationUO   // I think this was UO Third Dawn
        {
            get
            {
                return EraTD;
            }
        }

        /// <summary>
        /// Use this for deciding between beautiful old-school UO gumps and the new style gumps designed to hold variable length text.
        /// We believe it was UO Third Dawn that saw the massive Localization changes. With these changes came the ugly gumps to ensure vatiable
        /// length text would fit. 
        /// </summary>
        public static bool Localized    // I think this was UO Third Dawn
        {
            get
            {
                return PublishDate >= LocalizationUO;
            }
        }

        /// <summary>
        /// Without naming a shard, describes whether this shard is attempting era accuracy.
        /// </summary>
        public static bool EraAccurate
        {
            get
            {   // add your Era Accurate shards here
                return UOSP;
            }
        }

        /*	http://www.uoguide.com/Publishes
		 * Publishes are changes to a Shard's programming to fix bugs or add content. 
		 * Publishes may or may not be announced by the development team, depending on the type. 
		 * A major game update will always be announced (i.e. Publish 25), however, publishes may also occur invisibly during a 
		 * shard's maintenance to fix exploits and bugs.
		 * Originally, major publishes were known simply by the date on which they were released, 
		 * but in the time leading up to the announcement of UO:R, The Six Month Plan was announced. This laid out the future goal 
		 * of releasing large, regularly scheduled publishes. Publishes began being numbered internally, and the practice caught 
		 * on until publishes began being publically known by their number. The first publish to be mentioned by its number 
		 * publically was Publish 10, and the first publish to be officially titled by its number was the massive Publish 15.
		 * 
		 * Implementation note:
		 * note on version dates.
		 * a publish number if 64.0.2 is folded to 64.02
		 */

        public static DateTime PublishDate
        {
            get
            {
                // 2010
                if (Core.Publish == 68.3) return new DateTime(2010, 10, 28);
                // Halloween 2010, 13th Anniversary, Message in a Bottle changes, bug fixes
                if (Core.Publish == 68.2) return new DateTime(2010, 10, 15);
                // Fishing quest and ship fixes
                if (Core.Publish == 68.1) return new DateTime(2010, 10, 14);
                // Bug fixes
                if (Core.Publish == 68) return new DateTime(2010, 10, 12);
                // High Seas booster release, level 7 treasure maps and smooth sailing animation introduction
                if (Core.Publish == 67) return new DateTime(2010, 8, 4);
                // Treasure Map location randomization and new loot, Lockpicking and Bard Masteries changes.
                if (Core.Publish == 66.2) return new DateTime(2010, 6, 22);
                // In the Shadow of Virtue live event begun, addition of Endless Decanter of Water
                if (Core.Publish == 66) return new DateTime(2010, 5, 19);
                // More Human-to-Gargoyle weapon conversions, Bard Mastery System introduced, Player Memorials introduced, additional Advanced Character Templates, Throwing skill changes, bug fixes
                if (Core.Publish == 65) return new DateTime(2010, 4, 5);
                // Mysticism revamp, Gargoyle Racial Abilities update, Titles customization, additional Artifacts
                if (Core.Publish == 64.02) return new DateTime(2010, 3, 4);
                // Server crash and Faction fixes.
                if (Core.Publish == 64.01) return new DateTime(2010, 2, 12);
                // Valentines 2010, Seed Trading Box and Sarah the Exotic Goods Trader removed.
                if (Core.Publish == 64) return new DateTime(2010, 2, 10);
                // Substantial Item Insurance changes, new Gardening resources, Stygian Abyss encounter buffs, Seed Trading Box introduced, Mysticism changes.
                if (Core.Publish == 63.00) return new DateTime(2010, 1, 12); // mysteriously out of order Publish number
                                                                             // Introduced skin-rehue NPC vendor, fixed vendor skill/stat exploit.
                                                                             // 2009
                if (Core.Publish == 63.2) return new DateTime(2009, 12, 18);
                // Bug fixes for Paroxymus Swamp Dragons, auto-rezzing at login, various character appearance issues
                if (Core.Publish == 63.1) return new DateTime(2009, 12, 18);
                // Bug fixes for chicken coops and body-changing forms being stuck.
                if (Core.Publish == 63) return new DateTime(2009, 12, 17);
                // Imbuing changes, Holiday 2009, rennovated global Chat system
                if (Core.Publish == 62.37) return new DateTime(2009, 12, 7);
                // Several significant changes to the Imbuing skill.
                if (Core.Publish == 62.3) return new DateTime(2009, 11, 18);
                // Thanksgiving 2009 mini-event, general bug fixes
                if (Core.Publish == 62.2) return new DateTime(2009, 10, 29);
                // Halloween 2009 bug fixes
                if (Core.Publish == 62) return new DateTime(2009, 10, 22);
                // Halloween 2009 content, general bug fixes
                if (Core.Publish == 61.1) return new DateTime(2009, 10, 15);
                // Bug fixes for Stygian Abyss
                if (Core.Publish == 61) return new DateTime(2009, 10, 7);
                // Several bug fixes for Stygian Abyss, new Veteran Rewards, 12th Anniversary Gifts
                if (Core.Publish == 60.1) return new DateTime(2009, 9, 18);
                // Several bug fixes for Stygian Abyss
                if (Core.Publish == 60) return new DateTime(2009, 9, 8);
                // Stygian Abyss expansion launch
                if (Core.Publish == 59) return new DateTime(2009, 7, 14);
                // Ghost cam fixes, Shadowlord events, bug fixes.
                if (Core.Publish == 58.8) return new DateTime(2009, 6, 19);
                // Treasures of Tokuno re-activated, Quiver of Rage to be fixed.
                if (Core.Publish == 58) return new DateTime(2009, 3, 19);
                // Trial account limitations, various Champion Spawn bug fixes, reverted the Bag of Sending nerf from Publish 48, reduced weight of gold and silver.
                // 2008
                if (Core.Publish == 57) return new DateTime(2008, 12, 18);
                // New stealables for thieves, Scroll of Transcendence drops at Champion Spawns, Holiday 2008 gifts, Lumber skill requirement changes for Carpentry and several bug fixes.
                if (Core.Publish == 56) return new DateTime(2008, 10, 29);
                // War of Shadows and Halloween 2008 event content, new Magincia quests, new stackables, Factions updates, miscellaneous fixes
                if (Core.Publish == 55) return new DateTime(2008, 9, 10);
                // Spellweaving changes, new Veteran Rewards, additional Seed types, end of Spring Cleaning 2008
                if (Core.Publish == 54) return new DateTime(2008, 7, 11);
                // Faction bug fixes, Spring Cleaning 2008: Phase III, miscellaneous bug fixes
                if (Core.Publish == 53) return new DateTime(2008, 6, 10);
                // House resizing, various item and creature bug fixes, Spring Cleaning 2008: Phase II, ramping up of current events, misc. bug fixes
                if (Core.Publish == 52) return new DateTime(2008, 5, 6);
                // Faction fixes/changes, house placement and IDoC changes, Spring Cleaning 2008: Phase I, commas in checks, misc. bug fixes
                if (Core.Publish == 51) return new DateTime(2008, 3, 27);
                // Pet ball changes, pet AI improvements, various bug fixes
                if (Core.Publish == 50) return new DateTime(2008, 2, 14);
                // Improved runic intensities and BoD chances, greater dragons, BoS and Salvage Bag fixes, disabled Halloween 2007, activated Valentine's Day 2008 activities
                if (Core.Publish == 49) return new DateTime(2008, 1, 23);
                // Changes to the character database
                // 2007
                if (Core.Publish == 48) return new DateTime(2007, 11, 27);
                // Salvage Bag, Doom Gauntlet changes, Faction changes, Blood Oath fixes, Bag of Sending nerf, various monster strength/loot buffs
                if (Core.Publish == 47) return new DateTime(2007, 9, 25);
                // 10th Anniversary legacy dungeon drop system, 10th Anniversary gifts
                if (Core.Publish == 46) return new DateTime(2007, 8, 10);
                // PvP balances/changes, KR crafting menu functionality revamps, new KR macro functionality, resource randomization, various bug fixes
                if (Core.Publish == 45) return new DateTime(2007, 5, 25);
                // Stat gain changes and other miscellaneous changes
                if (Core.Publish == 44) return new DateTime(2007, 4, 30);
                // Discontinuation of the 3d client, New Player Experience, destruction of Old Haven, emergence of New Haven, Arms Lore changes
                // 2006
                if (Core.Publish == 43) return new DateTime(2006, 10, 26);
                // Contained 9th Anniversary additions, Evasion balancing, and Stealth/Detect Hidden changes
                if (Core.Publish == 42) return new DateTime(2006, 9, 1);
                // Added new Veteran Rewards, capped regeneration properties, and nerfed hit leech properties
                if (Core.Publish == 41) return new DateTime(2006, 6, 30);
                // Added the 4 Personal Attendants
                if (Core.Publish == 40) return new DateTime(2006, 5, 18);
                // Added Buff Bar, Targeting System, and various PvP related changes
                if (Core.Publish == 39) return new DateTime(2006, 2, 16);
                // New player improvements, 8x8 removal, and Spring D�cor Collection items added
                // 2005
                if (Core.Publish == 38) return new DateTime(2005, 12, 16);
                // Name/Gender Change Tokens, Holiday 2005 gifts, stats capped individually at 150
                if (Core.Publish == 37) return new DateTime(2005, 11, 3);
                // Many, many, many bug fixes for Mondain's Legacy and other long standing bugs
                if (Core.Publish == 36) return new DateTime(2005, 9, 22);
                // 8th Age anniversary items added and various Mondain's Legacy bug fixes
                if (Core.Publish == 35) return new DateTime(2005, 8, 10);
                // Mondain's Legacy support and Gamemaster support tool updates
                if (Core.Publish == 34) return new DateTime(2005, 7, 28);
                // Mondain's Legacy support
                if (Core.Publish == 33) return new DateTime(2005, 6, 21);
                // Change to buying Advanced Character Tokens, Evil Home Decor support, and shuts off Treasures of Tokuno
                if (Core.Publish == 32) return new DateTime(2005, 5, 2);
                // Necromancy potions and Exorcism, Alliance/Guild chat, end of The Britain Invasion, Special Moves fixes
                if (Core.Publish == 31) return new DateTime(2005, 3, 17);
                // Treasures of Tokuno I turn in begins, promo tokens, soulstone fragments, magery summons fixes, craftable spellbook properties, various bug fixes
                if (Core.Publish == 30) return new DateTime(2005, 2, 9);
                // Treasures of Tokuno I begins, item fixes, craftable Necromancy Scrolls and magic spellbooks, Yamandon gas attack, bug fixes
                if (Core.Publish == 29) return new DateTime(2005, 1, 20);
                // Damage numbers, pet fixes, archery/bola/lightning strike fixes, slayer changes
                // 2004
                if (Core.Publish == 28) return new DateTime(2004, 11, 23);
                // SE fixes, instanced corpses, 160.0 bard difficulty cap, fishing fixes, fame changes, new marties
                if (Core.Publish == 27) return new DateTime(2004, 9, 14);
                // Paragon and Minor Artifact systems
                if (Core.Publish == 26) return new DateTime(2004, 8, 17);
                // Loot changes, bardable creature changes, necromancer form fixes
                if (Core.Publish == 25) return new DateTime(2004, 7, 13);
                // Bug fixes, PvP balance changes, Archery fixes
                if (Core.Publish == 24) return new DateTime(2004, 5, 13);
                // Housefighting balances, reds in Fel guard zones, Valor spam fix, overloading fixes
                if (Core.Publish == 23) return new DateTime(2004, 3, 25);
                // The Character Transfer system
                // 2003
                if (Core.Publish == 22) return new DateTime(2003, 12, 17);
                // Holiday 2003 gifts, Sacred Journey fixes, bonded pet fixes, assorted other bug fixes
                if (Core.Publish == 21) return new DateTime(2003, 11, 25);
                // Housing/Vendor fixes, NPC economics, Factions fixes, Bulk Order Deed lockdown fix, various other fixes
                if (Core.Publish == 20) return new DateTime(2003, 10, 6);
                // New Vendor system, Bulletin Boards, Housing Runestones, the death of in-game HTML and UBWS bows
                if (Core.Publish == 19) return new DateTime(2003, 7, 30);
                // Quick Self-looting, BoD books, special move fixes/changes, housing lockdown fixes, pet and faction tweaks
                if (Core.Publish == 18) return new DateTime(2003, 5, 28);
                // AoS launch gifts, new wearables, customized housing fixes, Paladin/Necromancer balances
                if (Core.Publish == 17) return new DateTime(2003, 2, 11);
                // AoS launch publish and miscellaneous related changes
                // 2002
                if (Core.Publish == 16) return new DateTime(2002, 7, 12);
                // Resource/Crafting changes, Felucca Champion Spawns and Powerscrolls, Barding/Taming changes, Felucca enhancements/changes, House Ownership changes, the GGS system, the Justice Virtue, Siege Perilous ruleset changes
                if (Core.Publish == 15) return new DateTime(2002, 1, 9);
                /* http://www.uoguide.com/List_of_BNN_Articles_(2002)#Scenario_4:_Plague_of_Despair
				 * Scenario 5: When Ants Attack
				 *	Workers - October 3
				 *	Scientific Discussion - September 26
				 *	Orcs and Bombs - September 19
				 *	Crazy Miggie - September 12
				 *	I Think, Therefore I Dig - September 5
				 * Scenario 4: Plague of Despair
				 *	Epilogue - May 30
				 *	Plague of Despair - May 16
				 *	Preparations - May 9
				 *	Symptoms - May 2
				 *	Seeds - April 25
				 *	The Casting - April 18
				 *		(Dragon Scale Armor was introduced into the game during the first week of Scenerio Four.)
				 *		(http://noctalis.com/dis/uo/n-smit3.shtml)
				 *	Enemies and Allies - April 11 
				 * Scenario 3: Blackthorn's Damnation
				 *	The Watcher - January 25
				 *	Downfall to Power - January 17
				 *	Change - January 10
				 *	Inferno - January 2
				 */
                // Housing/Lockdown fixes, NPC and hireling fixes, various skill gump fixes, Faction updates, miscellaneous localizations
                // 2001
                if (Core.Publish == 14) return new DateTime(2001, 11, 30);
                // New Player Experience, context sensitive menus, Blacksmithing BoD system, Animal Lore changes, Crafting overhaul, Factions updates
                if (Core.Publish == 13.6) return new DateTime(2001, 10, 25);
                // Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                if (Core.Publish == 13.5) return new DateTime(2001, 10, 11);
                // Commodity Deeds, Repair Contracts, Secure House Trades
                if (Core.Publish == 13) return new DateTime(2001, 8, 19);
                // Treasure map changes, tutorial/Haven changes, combat changes, with power hour changes and player owned barkeeps as later additions
                if (Core.Publish == 12) return new DateTime(2001, 7, 24);
                // Veteran Rewards, vendor changes, skill modification changes, GM rating tool, miscellaneous bug fixes and changes
                if (Core.Publish == 11) return new DateTime(2001, 3, 14);
                // The Ilshenar landmass, taxidermy kits, hair stylist NPCs, Item Identification changes, creatures vs. negative karma, vendor changes, various fixes and preparations for UO:TD
                if (Core.Publish == 10) return new DateTime(2001, 2, 1);
                // Henchman for Noble NPCs, disabling Hero/Evil, magic in towns, facet menus for public moongates, karma locking, faction fixes, spawn changes
                // 2000
                if (Core.Publish == 9) return new DateTime(2000, 12, 15);
                // T2A transport, Holiday gifts/tree activation, lockdown changes, default desktops, house add-on changes
                if (Core.Publish == 8) return new DateTime(2000, 12, 6);
                // The Factions System, stablemaster changes, monster movement changes
                if (Core.Publish == 7) return new DateTime(2000, 9, 3);
                // New Player Experience changes, lockdown/secure changes, comm. crystal changes, dungeon Khaldun, vendor customization
                if (Core.Publish == 6) return new DateTime(2000, 8, 1);
                // Looting rights changes, lockdown changes, stuck player options
                if (Core.Publish == 5) return new DateTime(2000, 4, 27);
                // Ultima Online: Renaissance, various updates and fixes for UO:R
                if (Core.Publish == 4) return new DateTime(2000, 3, 8);
                // Skill gain changes, Power Hour, sea serpents in fishing, bank checks, tinker traps, shopkeeper changes
                if (Core.Publish == 3) return new DateTime(2000, 2, 23);
                // Trade window changes, monsters trapped in houses, guild stone revamp, moonstones, secure pet/house trading, dex and healing
                if (Core.Publish == 2) return new DateTime(2000, 1, 24);
                // Escort and Taming changes, invalid house placement, land surveying tool, the death of precasting, Clean Up Britannia Phase III, item decay on boats
                // 1999
                if (Core.Publish == 1) return new DateTime(1999, 11, 23);
                // Co-owners, Maker's Mark, Perma-reds, Skill management, Clean Up Britannia Phase II, Bank box weight limit removal, Runebooks, Potion Kegs, other changes

                // Publish - September 22, 1999
                // Smelting, Unraveling, pet changes, Chaos/Order changes, armoire fix
                // UO Live Access Patch - August 25, 1999
                // Companion program, "Young" status, arm/disarm, last target, next target, TargetSelf macros, Ultima Messenger, various bug fixes
                // Publish - May 25, 1999
                // Difficulty-based Tinkering, "all follow me" etc., cut-up leather, boards from logs, other skill changes, dry-docking boats, various fixes
                // Publish - April 14, 1999
                // Targetting distance changes, trade window scam prevention, "I must consider my sins"
                // Publish - March 28, 1999
                // Long-term murder counts, Fishing resources, sunken treasure, craftable musical instruments, jewelcrafting, no more casting while hidden, new Stealth rules, ability to sell house deeds, house and boat optimizations
                // Publish - February 24, 1999
                // The Stealth and Remove Trap skills, changes to Detect Hidden and Forensic Evaluation, the Thieves Guild, new skill titles, tying Evaluating Intelligence to spell damage, dungeon treasure chests and dungeon traps, trash barrels, pet "orneriness," miscellaneous fixes and changes
                // Publish - February 2, 1999
                // Colored ore, granting karma, macing weapons destroying armor, Anatomy damage bonus, the Meditation skill, lockdown commands, blacksmith NPC guild, miscellaneous fixes
                // Publish - January 19, 1999
                // New Carpentry items, Fire Field banned from towns, Treasure Maps, Tailoring becomes difficulty-based, no more "a scroll," miscellaneous fixes
                // 1997 - 1998

                // not sure what the default should be, but assum it's 'new' therefore likely excluding stuff we are un sure about.
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Inclusive
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>true if this publish is active</returns>
        public static bool CheckPublish(double from, double to)
        {   // Note: this test is inclusive. The 'to' date needs be the last valid publish for this test.
            // For example. If purple wisps were only valid from publish 5 - 7, then the CheckPublish(5,7) would return true
            //	The 'to' date should NOT be the publish in which something became invalid.
            return Publish >= from && Publish <= to;
        }

        /// <summary>
        /// Is the named publish active?
        /// </summary>
        /// <param name="pub"></param>
        /// <returns>true if this publish is active</returns>
        public static bool CheckPublish(double pub)
        {
            return CheckPublish(pub, pub);
        }

        public static double Publish
        {
            get
            {
                return m_Publish;
            }
        }

        public static bool OldEthics
        {
            get
            {
                return Core.UOSP && Core.Publish < 13.6;
            }
        }

        public static bool NewEthics
        {
            get
            {
                return !OldEthics;
            }
        }

        public static bool LoginServer
        {
            get
            {   // not really right. In the AI 7, we have an explicit login server.
                //  Here, AI doubles as a shard and a login server.
                //  So if the ListeningPort has been reassigned, we can assume this is not a login server
                return Core.UOAI && !Core.UOTC && ListeningPort == -1;
            }
        }

        public static bool T2A
        {   // is T2A available to this shard?
            get
            {
                return Core.UOSP;
            }
        }

        public static bool OldStyleTinkerTrap
        {
            get
            {
                return Core.Publish < 4 || Core.UOAI || Core.UOAR;
            }
        }

        public static bool NewStyleTinkerTrap
        {
            get
            {
                return !Core.OldStyleTinkerTrap;
            }
        }

        public static bool Factions
        {
            get
            {   // add your factions enabled servers here
                return Core.UOSP && Publish >= 8.0;
            }
        }

        public static bool Ethics
        {
            get
            {
                // Siege Perilous is a special ruleset shard that launched on July 15, 1999. 
                return Core.UOSP && Core.PublishDate >= new DateTime(1999, 7, 15);
            }
        }

        public static bool UOEV
        {
            get
            {
                return m_UOEV;
            }
        }

        public static bool UOTC
        {
            get
            {
                return m_UOTC;
            }
        }

        public static bool UOAI
        {
            get
            {
                return m_UOAI;
            }
        }

        /// <summary>
        /// Publish 5 UO Siege Perilous
        /// </summary>
        public static bool UOSP
        {
            get
            {
                return m_UOSP;
            }
        }

        public static bool UOAR
        {
            get
            {
                return m_UOAR;
            }
        }

        public static bool UOMO
        {
            get
            {
                return m_UOMO;
            }
        }

        public static bool Building
        {
            get
            {
                return m_Building;
            }
        }

        public static bool AOS
        {
            get
            {
                return m_AOS || m_SE;
            }
            set
            {
                m_AOS = value;
            }
        }

        public static bool SE
        {
            get
            {
                return m_SE;
            }
            set
            {
                m_SE = value;
            }
        }

        public static bool ML
        {
            get
            {
                return m_ML;
            }
            set
            {
                m_ML = value;
            }
        }


        public static string ExePath
        {
            get
            {
                if (m_ExePath == null)
                    m_ExePath = Process.GetCurrentProcess().MainModule.FileName.Replace("vshost.", "");

                return m_ExePath;
            }
        }

        public static string BaseDirectory
        {
            get
            {
                if (m_BaseDirectory == null)
                {
                    try
                    {
                        m_BaseDirectory = ExePath;

                        if (m_BaseDirectory.Length > 0)
                            m_BaseDirectory = Path.GetDirectoryName(m_BaseDirectory);
                    }
                    catch
                    {
                        m_BaseDirectory = "";
                    }
                }

                return m_BaseDirectory;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
            Console.WriteLine(e.ExceptionObject);

            if (e.IsTerminating)
            {
                m_Crashed = true;

                bool close = false;

                try
                {
                    CrashedEventArgs args = new CrashedEventArgs(e.ExceptionObject as Exception);

                    EventSink.InvokeCrashed(args);

                    close = args.Close;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                if (!close && !m_Service)
                {
                    try
                    {
                        for (int i = 0; i < m_MessagePump.Listeners.Length; i++)
                        {
                            m_MessagePump.Listeners[i].Dispose();
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    if (SocketPool.Created)
                        SocketPool.Destroy();

                    Console.WriteLine("This exception is fatal, press return to exit");
                    Console.ReadLine();
                }

                m_Closing = true;
            }
        }

        private enum ConsoleEventType
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private delegate bool ConsoleEventHandler(ConsoleEventType type);
        private static ConsoleEventHandler m_ConsoleEventHandler;

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventHandler callback, bool add);

        private static bool OnConsoleEvent(ConsoleEventType type)
        {
            if (World.Saving || (m_Service && type == ConsoleEventType.CTRL_LOGOFF_EVENT))
                return true;

            Kill();

            return true;
        }

        #region HIDE_CLOSEBOX
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);

        internal const uint SC_CLOSE = 0xF060;
        internal const uint MF_GRAYED = 0x00000001;
        internal const uint MF_BYCOMMAND = 0x00000000;
        #endregion HIDE_CLOSEBOX

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            HandleClosed();
        }

        private static bool m_Closing;
        public static bool Closing { get { return m_Closing; } }

        public static void Kill()
        {
            Kill(false);
        }

        public static void Kill(bool restart)
        {
            HandleClosed();

            if (restart)
                Process.Start(ExePath, Arguments);

            m_Process.Kill();
        }

        private static void HandleClosed()
        {
            if (m_Closing)
                return;

            m_Closing = true;

            Console.Write("Exiting...");

            if (!m_Crashed)
                EventSink.InvokeShutdown(new ShutdownEventArgs());

            if (SocketPool.Created)
                SocketPool.Destroy();

            Timer.TimerThread.Set();

            Console.WriteLine("done");
        }

        private static AutoResetEvent m_Signal = new AutoResetEvent(true);
        public static void Set() { m_Signal.Set(); }

        public static void Main(string[] args)
        {
#if !DEBUG
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            #region ESTABLISH CWD
            {   // in development, VS places the exe in something like

            }
            #endregion ESTABLISH CWD

            #region HIDE_CLOSEBOX
#if !DEBUG
			IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;
			IntPtr hSystemMenu = GetSystemMenu(hMenu, false);
			EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
			RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
#endif
            #endregion HIDE_CLOSEBOX

            #region ARG PARSING
            Arguments = "";
            for (int i = 0; i < args.Length; ++i)
            {
                if (Insensitive.Equals(args[i], "-debug"))
                    m_Debug = true;
                else if (Insensitive.Equals(args[i], "-port"))
                    ListeningPort = int.Parse(args[i + 1]);
                //Server.SocketOptions.AngelIslandPort = int.Parse(args[i + 1]);
                else if (Insensitive.Equals(args[i], "-service"))
                    m_Service = true;
                else if (Insensitive.Equals(args[i], "-profile"))
                    Profiling = true;
                else if (Insensitive.Equals(args[i], "-nocache"))
                    m_Cache = false;
                else if (Insensitive.Equals(args[i], "-haltonwarning"))
                    m_HaltOnWarning = true;
                else if (Insensitive.Equals(args[i], "-vb"))
                    m_VBdotNET = true;
                else if (Insensitive.Equals(args[i], "-import"))
                    m_Import = true;
                else if (Insensitive.Equals(args[i], "-boatholdupgrade"))
                    m_BoatHoldUpgrade = true;
                else if (Insensitive.Equals(args[i], "-uotc"))
                    m_UOTC = true; //
                else if (Insensitive.Equals(args[i], "-uosp"))
                    m_UOSP = true;
                else if (Insensitive.Equals(args[i], "-uoar"))
                    m_UOAR = true;
                else if (Insensitive.Equals(args[i], "-uomo"))
                    m_UOMO = true;
                else if (Insensitive.Equals(args[i], "-uoai"))
                    m_UOAI = true;
                else if (Insensitive.Equals(args[i], "-uoev"))
                    m_UOEV = true;
                else if (Insensitive.Equals(args[i], "-build"))
                    m_Building = true;
                else if (Insensitive.Equals(args[i], "-developer"))
                    m_Developer = true;

                Arguments += args[i] + " ";
            }
            #endregion

            #region VERIFY ARGS
            int server_count = 0;
            if (m_UOAI == true) server_count++;
            if (m_UOSP == true) server_count++;
            if (m_UOAR == true) server_count++;
            if (m_UOMO == true) server_count++;
            if (server_count == 0)
            {
                Console.WriteLine("Core: No server specified, defaulting to Angel Island");
                m_UOAI = true;
            }
            if (server_count > 1)
            {
                Console.WriteLine("Core: Too many servers specified.");
                return;
            }
            #endregion

            #region LOG SETUP
            try
            {
                if (m_Service)
                {
                    if (!Directory.Exists("Logs"))
                        Directory.CreateDirectory("Logs");

                    Console.SetOut(m_MultiConOut = new MultiTextWriter(Console.Out, new FileLogger("Logs/Console.log")));
                }
                else
                {
                    Console.SetOut(m_MultiConOut = new MultiTextWriter(Console.Out));
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            #endregion

            m_Thread = Thread.CurrentThread;
            m_Process = Process.GetCurrentProcess();
            m_Assembly = Assembly.GetEntryAssembly();

            if (m_Thread != null)
                m_Thread.Name = "Core Thread";

            if (BaseDirectory.Length > 0)
                Directory.SetCurrentDirectory(BaseDirectory);

            Timer.TimerThread ttObj = new Timer.TimerThread();
            timerThread = new Thread(new ThreadStart(ttObj.TimerMain));
            timerThread.Name = "Timer Thread";

            Version ver = m_Assembly.GetName().Version;

            // Added to help future code support on forums, as a 'check' people can ask for to it see if they recompiled core or not
            if (Core.UOSP)
                m_Server = "Siege Perilous";
            else if (Core.UOMO)
                m_Server = "Mortalis";
            else if (Core.UOAR)
                m_Server = "AI Resurrection";
            else
                m_Server = "Angel Island";

            Console.WriteLine("{4} - [www.game-master.net] Version {0}.{1}.{3}, Build {2}", ver.Major, ver.Minor, ver.Revision, ver.Build, m_Server);
#if DEBUG
            Console.WriteLine("[Debug Build Enabled]");
#endif
            if (Core.BoatHoldUpgrade == true)
                Console.WriteLine("[Boat holds will be upgraded on this world load.]");

            Console.WriteLine("[Test Center functionality is turned {0}.]", Core.UOTC ? "on" : "off");

            Console.WriteLine("[Event Shard functionality is turned {0}.]", Core.UOEV ? "on" : "off");

            Console.WriteLine("[Publish {0} enabled.]", Publish);

            Console.WriteLine("[World building is turned {0}.]", Core.Building ? "on" : "off");

            Console.WriteLine("[Developer mode is turned {0}.]", Core.Developer ? "on" : "off");

            Console.WriteLine("[Factions are {0}.]", Core.Factions ? "enabled" : "disabled");

            Console.WriteLine("[T2A is {0}.]", Core.T2A ? "available" : "unavailable");

            if (Core.Import)
                Console.WriteLine("[Importing unencrypted database.]");

            if (Arguments.Length > 0)
                Console.WriteLine("Core: Running with arguments: {0}", Arguments);

            m_ProcessorCount = Environment.ProcessorCount;

            if (m_ProcessorCount > 1)
                m_MultiProcessor = true;

            if (m_MultiProcessor || Is64Bit)
                Console.WriteLine("Core: Optimizing for {0} {2}processor{1}", m_ProcessorCount, m_ProcessorCount == 1 ? "" : "s", Is64Bit ? "64-bit " : "");

            int platform = (int)Environment.OSVersion.Platform;
            if ((platform == 4) || (platform == 128))
            { // MS 4, MONO 128
                m_Unix = true;
                Console.WriteLine("Core: Unix environment detected");
            }
            else
            {
                m_ConsoleEventHandler = new ConsoleEventHandler(OnConsoleEvent);
                SetConsoleCtrlHandler(m_ConsoleEventHandler, true);
            }

            // we don't use the RunUO system for debugging scripts
            //while (!ScriptCompiler.Compile(m_Debug))
            //{
            //    if (m_Quiet) //abort and exit if compile scripts failed
            //        return;

            //    Console.WriteLine("Scripts: One or more scripts failed to compile or no script files were found.");
            //    Console.WriteLine(" - Press return to exit, or R to try again.");

            //    string line = Console.ReadLine();
            //    if (line == null || line.ToLower() != "r")
            //        return;
            //}

            // adam: I believe the new startup logic is more robust as it attempts to prevents timers from firing 
            //  before the shard is fully up and alive.
            AIWorldBoot aiWorldBoot = new AIWorldBoot();
            aiWorldBoot.Configure();
            aiWorldBoot.WorldLoad();
            aiWorldBoot.Initialize();
            //aiWorldBoot.ObjectInitialize(); // not yet available in core 3

            // this timer (and output) simply proves timers created during Configure, WorldLoad, and Initialize will be
            // respected, and processed as planned. 
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { null });

            Region.Load();

            SocketPool.Create();

            MessagePump ms = m_MessagePump = new MessagePump();

            timerThread.Start();

            for (int i = 0; i < Map.AllMaps.Count; ++i)
                ((Map)Map.AllMaps[i]).Tiles.Force();

            NetState.Initialize();

            EventSink.InvokeServerStarted();

#if !DEBUG
			try
			{
#endif
            while (m_Signal.WaitOne())
            {
                Mobile.ProcessDeltaQueue();
                Item.ProcessDeltaQueue();

                Timer.Slice();
                m_MessagePump.Slice();

                NetState.FlushAll();
                NetState.ProcessDisposedQueue();

                if (Slice != null)
                    Slice();
            }
#if !DEBUG
			}
			catch (Exception e)
			{
				CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
			}
#endif

        }

        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine("Timers initialized");
            Utility.PopColor();
        }

        private static string m_arguments;
        public static string Arguments
        {
            get
            {
                return m_arguments;
            }

            set
            {
                m_arguments = value;
            }
        }

        private static int m_GlobalMaxUpdateRange = 24;

        public static int GlobalMaxUpdateRange
        {
            get { return m_GlobalMaxUpdateRange; }
            set { m_GlobalMaxUpdateRange = value; }
        }

        private static int m_ItemCount, m_MobileCount, m_SerializableObjectCount;

        public static int ScriptItems { get { return m_ItemCount; } }
        public static int ScriptMobiles { get { return m_MobileCount; } }
        public static int ScriptSerializableObjects { get { return m_SerializableObjectCount; } }

        public static void VerifySerialization()
        {
            m_ItemCount = 0;
            m_MobileCount = 0;

            VerifySerialization(Assembly.GetCallingAssembly());

            for (int a = 0; a < ScriptCompiler.Assemblies.Length; ++a)
                VerifySerialization(ScriptCompiler.Assemblies[a]);
        }

        private static void VerifySerialization(Assembly a)
        {
            if (a == null) return;

            Type[] ctorTypes = new Type[] { typeof(Serial) };

            foreach (Type t in a.GetTypes())
            {
                bool isItem = t.IsSubclassOf(typeof(Item));
                bool isSerializableObject = t.IsSubclassOf(typeof(SerializableObject));
                bool isMobile = t.IsSubclassOf(typeof(Mobile));
                if (isItem || isMobile || isSerializableObject)
                {
                    if (isItem)
                        ++m_ItemCount;
                    else if (isMobile)
                        ++m_MobileCount;
                    else
                        ++m_SerializableObjectCount;

                    bool warned = false;

                    try
                    {
                        if (isSerializableObject == false)
                            if (t.GetConstructor(ctorTypes) == null)
                            {
                                if (!warned)
                                    Console.WriteLine("Warning: {0}", t);

                                warned = true;
                                Console.WriteLine("       - No serialization constructor");
                            }

                        if (t.GetMethod("Serialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                        {
                            if (!warned)
                                Console.WriteLine("Warning: {0}", t);

                            warned = true;
                            Console.WriteLine("       - No Serialize() method");
                        }

                        if (t.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                        {
                            if (!warned)
                                Console.WriteLine("Warning: {0}", t);

                            warned = true;
                            Console.WriteLine("       - No Deserialize() method");
                        }

                        if (warned)
                            Console.WriteLine();
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        public class AIWorldBoot
        {
            Assembly m_scripts = null;
            ArrayList m_invoke = new ArrayList();
            Type[] m_types;
            public AIWorldBoot()
            {   // get the current assembly
                Type t = typeof(Core);
                m_scripts = t.Assembly;
                m_types = m_scripts.GetTypes();
            }

            public void Configure()
            {
                for (int i = 0; i < m_types.Length; ++i)
                {
                    MethodInfo m = m_types[i].GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);

                m_invoke.Clear();
            }

            public void WorldLoad()
            {
                World.Load();
            }

            // not yet implemented in core 3
            //public void ObjectInitialize()
            //{
            //    try
            //    {
            //        // 4/4/23, Adam: Add individual object initialization. Unlike Initialize, WorldLoaded provides initialization
            //        //  where the context of the object is known.
            //        Console.WriteLine("Initializing {0} items", World.Items.Count);
            //        foreach (Item item_dsr in World.Items.Values)
            //            if (item_dsr != null) item_dsr.WorldLoaded();
            //        Console.WriteLine("Initializing {0} Mobiles", World.Mobiles.Count);
            //        foreach (Mobile mobile_dsr in World.Mobiles.Values)
            //            if (mobile_dsr != null) mobile_dsr.WorldLoaded();
            //        Console.WriteLine("{0} objects initialized", World.Items.Count + World.Mobiles.Count);
            //    }
            //    catch {; }
            //}

            public void Initialize()
            {
                for (int i = 0; i < m_types.Length; ++i)
                {
                    MethodInfo m = m_types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);
            }
        }
    }

    public class FileLogger : TextWriter, IDisposable
    {
        private string m_FileName;
        private bool m_NewLine;
        public const string DateFormat = "[MMMM dd hh:mm:ss.f tt]: ";

        public string FileName { get { return m_FileName; } }

        public FileLogger(string file)
            : this(file, false)
        {
        }

        public FileLogger(string file, bool append)
        {
            m_FileName = file;
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine(">>>Logging started on {0}.", DateTime.Now.ToString("f")); //f = Tuesday, April 10, 2001 3:51 PM 
            }
            m_NewLine = true;
        }

        public override void Write(char ch)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                {
                    writer.Write(DateTime.Now.ToString(DateFormat));
                    m_NewLine = false;
                }
                writer.Write(ch);
            }
        }

        public override void Write(string str)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                {
                    writer.Write(DateTime.Now.ToString(DateFormat));
                    m_NewLine = false;
                }
                writer.Write(str);
            }
        }

        public override void WriteLine(string line)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                    writer.Write(DateTime.Now.ToString(DateFormat));
                writer.WriteLine(line);
                m_NewLine = true;
            }
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }

    public class MultiTextWriter : TextWriter
    {
        private ArrayList m_Streams;

        public MultiTextWriter(params TextWriter[] streams)
        {
            m_Streams = new ArrayList(streams);

            if (m_Streams.Count < 0)
                throw new ArgumentException("You must specify at least one stream.");
        }

        public void Add(TextWriter tw)
        {
            m_Streams.Add(tw);
        }

        public void Remove(TextWriter tw)
        {
            m_Streams.Remove(tw);
        }

        public override void Write(char ch)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).Write(ch);
        }

        public override void WriteLine(string line)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).WriteLine(line);
        }

        public override void WriteLine(string line, params object[] args)
        {
            WriteLine(String.Format(line, args));
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }
}
