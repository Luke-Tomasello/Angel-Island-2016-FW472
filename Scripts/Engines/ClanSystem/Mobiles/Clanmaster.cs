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

/* Scripts\Engines\ClanSystem\Mobiles\Clanmaster.cs
 * ChangeLog
 *  10/7/2024, Adam
 *      Register this clanmaster on creation
 *	10/5/2024, Adam
 *      Initial version
 *      This Clanmaster holds the mini-champ spawner and will activate/configure it based upon commands
 *          from a player that is clan aligned.
 *          I would like to update this so that he only listens to some ranking clan menber.
 */

using Server.Engines.ChampionSpawn;
using Server.Engines.ClanSystem;
using Server.Items;
using Server.Misc;
using static Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Server.Diagnostics;
using static Server.Mobiles.Spawner;
namespace Server.Mobiles
{
    [NoSort]
    [CorpseName("corpse of the clanmaster")]
    public class Clanmaster : BaseCreature
    {
        public static void Initialize()
        {
            EventSink.ChampInfoEvent += new ChampInfoEventHandler(ChampInfo_EventSink);
        }
        #region Memory
        private Memory Intruder = new Memory();
        private Memory Alert = new Memory();
        private Memory Aggression = new Memory();
        private Memory Surrendered = new Memory();
        private Memory Command = new Memory();
        private Memory Equip = new Memory();
        public void OnSeeMobile(Mobile m)
        {
            if (ClanSystem.IsEnemy(this, m))
            {
                if (Intruder.Recall(ClanSystem.GetClanAlignment(m)) != null)
                { /* we've already seen this clan in our area, do nothing */ }
                else
                {
                    string text = string.Format("Our enemy {0} [{1}] was last seen at {2}", m.Name,
                        Utility.SentenceCamel(ClanSystem.GetClanAlignmentAsString(m)),
                        Utility.SextantLocationString(m));
                    ClanSystem.SendClanMessage(this.ClanAlignment, text);
                    Intruder.Remember(ClanSystem.GetClanAlignment(m), TimeSpan.FromMinutes(5).TotalSeconds);
                }

                if (Aggression.Recall(ClanSystem.GetClanAlignment(m)) != null)
                { /* we've already know this clan is attacking us, do nothing */ }
                else
                {
                    if (IsAttackingUs(m))
                    {
                        string text = string.Format("{0} [{1}] at {2} is attacking us!", m.Name,
                            Utility.SentenceCamel(ClanSystem.GetClanAlignmentAsString(m)),
                            Utility.SextantLocationString(m));
                        ClanSystem.SendClanMessage(this.ClanAlignment, text, hue: 0x22);
                        Aggression.Remember(ClanSystem.GetClanAlignment(m), TimeSpan.FromMinutes(5).TotalSeconds);
                    }
                }
            }
        }
        private bool IsAttackingUs(Mobile m)
        {
            foreach (AggressorInfo info in m.Aggressed)
                if (ClanSystem.GetClanAlignment(info.Defender) == ClanSystem.GetClanAlignment(this))
                    return true;

            return false;
        }
        private DateTime m_lastLook = DateTime.MinValue;
        private DateTime m_lastLevelDownCheck = DateTime.MinValue;
        private DateTime m_lastLevelExpiryCheck = DateTime.MinValue;
        public override void OnThink()
        {
            if (m_OurSpawner != null)
            {
                // if we are actively engaged, don't expire the level
                if (DateTime.UtcNow > m_lastLevelExpiryCheck)
                {
                    if (Commander != null)                              // We've got a commander
                        if (m_OurSpawner.Level > 0)                     // we've got defenders or deployments
                            if (AtWarWithClan(m_OurSpawner).Count > 0)  // we've got an enemy
                                m_OurSpawner.RefreshLevelExpiry();

                    //m_lastLevelExpiryCheck = DateTime.UtcNow + m_OurSpawner.Lvl_ExpireDelay / 2;
                    // TimeSpan halfTimeSpan = new TimeSpan(originalTimeSpan.Ticks / 2);
                    m_lastLevelExpiryCheck = DateTime.UtcNow + new TimeSpan(m_OurSpawner.Lvl_ExpireDelay.Ticks / 2);
                }

                // If not actively fighting anyone reset nav destination
                if (DateTime.UtcNow > m_lastLevelDownCheck)
                {   
                    if (m_OurSpawner.Level > 0)                         // we've got defenders or deployments
                        if (AtWarWithClan(m_OurSpawner).Count == 0)     // we've got no enemy
                        {   // return to 'guardian only' mode
                            m_OurSpawner.NavDestination = null;
                        }

                    m_lastLevelDownCheck = DateTime.UtcNow + TimeSpan.FromMinutes(1.0);
                }

                // look around every 2 seconds
                if (DateTime.UtcNow > m_lastLook)
                {
                    List<Mobile> mobiles = new List<Mobile>();
                    IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, this.RangePerception);
                    foreach (Mobile m in eable)
                        if (this.CanSee(m))
                            mobiles.Add(m);
                    eable.Free();

                    Utility.Shuffle(mobiles);

                    foreach (Mobile t in mobiles)
                        OnSeeMobile(t);

                    m_lastLook = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
                }
            }

            base.OnThink();
        }
        #endregion Memory
        #region props
        [CommandProperty(AccessLevel.Administrator)]
        public ChampMini Controller
        {
            get { return m_OurSpawner; }
            set { m_OurSpawner = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public Mobile Commander
        {
            get
            {
                Mobile commander = PlayerCommander;
                if (commander != null && commander.NetState == null)
                {
                    RelinquishCommand();
                    return null;
                }
                return commander;
            }
            set
            {
                if (PlayerCommander != null)
                    RelinquishCommand();
                if (value != null)
                    PlayerCommander = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public string Level
        { get { return m_OurSpawner != null ? Wave(m_OurSpawner) : (-1).ToString(); } }
        [CommandProperty(AccessLevel.Administrator)]
        public string LevelExpire
        {
            get
            {
                if (m_OurSpawner != null)
                {
                    return (DateTime.UtcNow >= m_OurSpawner.ExpireTime) ? "Expired"
                        : (m_OurSpawner.ExpireTime - DateTime.UtcNow).ToString();
                }
                else
                    return "Error";
            }
        }


        [CommandProperty(AccessLevel.Administrator)]
        public string AtWarWith
        {
            get
            {
                if (m_OurSpawner != null)
                {
                    List<Clanmaster> list = AtWarWithClan(m_OurSpawner);
                    if (list.Count == 0)
                        return "None";

                    var newList = list.Select(item => item.ClanAlignment).ToList();
                    return string.Join(", ", newList);
                }
                else
                    return "Error";
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int AllMonsters { get { return m_OurSpawner != null ? m_OurSpawner.AllMonsters.Count : 0; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int Monsters { get { return m_OurSpawner != null ? m_OurSpawner.Monsters.Count : 0; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int FreeMonsters { get { return m_OurSpawner != null ? m_OurSpawner.FreeMonsters.Count : 0; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int AbandonedMonsters { get { return m_OurSpawner != null ? m_OurSpawner.AbandonedMonsters.Count : 0; } }

        private string m_LastOrder = string.Empty;
        [CommandProperty(AccessLevel.Administrator)]
        public string Order
        { get { return m_LastOrder; } }

        #endregion props
        public static Dictionary<Clanmaster, ChampMini> Instances = new Dictionary<Clanmaster, ChampMini>();
        private ChampMini m_OurSpawner = null;
        public ChampMini WaveSpawner { get { return m_OurSpawner; } }
        public List<ChampMini> EnemySpawners = new List<ChampMini>();
        public ChampMini ChampSpawner { get { return m_OurSpawner; } }
        //public ChampMini EnemyChampSpawner { get { return m_EnemySpawner; } }
        private bool m_Invulnerable = true;
        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Orc; } }
        public override bool IsInvulnerable
        {
            get { return m_Invulnerable; }
            set
            {
                // June 2, 2001
                // http://martin.brenner.de/ultima/uo/news1.html
                // the (invulnerable) tag has been removed; invulnerable NPCs and players can now be identified by the yellow hue of their name
                // Adam: June 2, 2001 probably means Publish 12 which was July 24, 2001
                m_Invulnerable = value;
                if (m_Invulnerable && !Core.RuleSets.AOSRules() && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 12))
                    NameHue = 0x35;
            }
        }
        public override void OnAfterSpawn()
        {
            if (this.WaveSpawner != null && this.WaveSpawner.Equipment != null)
                Equipment = (Backpack)Utility.DupeDeepSimple(this.WaveSpawner.Equipment);
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public override string ClanAlignment
        {
            get { return base.ClanAlignment; }
            set
            {
                if (m_OurSpawner != null)
                    m_OurSpawner.ClanAlignment = value;
                base.ClanAlignment = value;
            }
        }
        private Backpack m_Equipment;                               // used to dress/equip the mobile
        [CommandProperty(AccessLevel.GameMaster)]
        public Backpack Equipment
        {
            get { return m_Equipment; }
            set
            {
                if (m_Equipment != null)
                    m_Equipment.Delete();

                m_Equipment = value;

                if (m_Equipment != null)
                {
                    m_Equipment.MoveItemToIntStorage();
                    // now pass off to our spawner so that it may use it to dress the mobiles
                    if (m_OurSpawner != null)
                        m_OurSpawner.Equipment = (Backpack)Utility.DupeDeepSimple(m_Equipment);
                }
            }
        }
        [Constructable]
        public Clanmaster()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.None;
            Title = "the clanmaster";
            IsInvulnerable = true;

            SetStr(111, 145);
            SetDex(101, 135);
            SetInt(86, 110);

            SetHits(67, 87);

            SetDamage(5, 15);

            SetSkill(SkillName.Fencing, 70.1, 95.0);
            SetSkill(SkillName.Macing, 70.1, 95.0);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Swords, 70.1, 95.0);
            SetSkill(SkillName.Parry, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 85.1, 100.0);

            InitBody();
            InitOutfit();

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 34;
        }
        public override bool PlayerRangeSensitive { get { return false; } }
        public override bool CanRummageCorpses { get { return false; } }
        public override int Meat { get { return 1; } }

        public override void InitBody()
        {
            Name = NameList.RandomName("orc");
            Body = 0x190;   // male human
        }
        public override void InitOutfit()
        {
            WipeLayers();
            Hue = Utility.RandomBool() ? 0x841C : 0x83F1;

            AddItem(new RingmailChest());

            AddItem(new OrcHelm());

            switch (Utility.Random(7))
            {
                case 0: AddItem(new Longsword()); break;
                case 1: AddItem(new Cutlass()); break;
                case 2: AddItem(new Broadsword()); break;
                case 3: AddItem(new Axe()); break;
                case 4: AddItem(new Club()); break;
                case 5: AddItem(new Dagger()); break;
                case 6: AddItem(new Spear()); break;
            }

            // now, add his champ spawner
            m_OurSpawner = new ChampMini();
            m_OurSpawner.SpawnType = ChampLevelData.SpawnTypes.ClanOrcWar;
            m_OurSpawner.SpawnerFlags = SpawnerFlags.ClearPath;
            m_OurSpawner.Visible = false;
            PackItem(m_OurSpawner);
            Item backpack = FindItemOnLayer(Layer.Backpack);
            if (backpack != null)
                backpack.Hue = 0x01;

            Instances.Add(this, m_OurSpawner);
        }
        public override void OnDelete()
        {
            Instances.Remove(this);
            if (Equipment != null)
                Equipment.Delete();
            base.OnDelete();
        }
        public override void AddItem(Item item)
        {
            if (item == null || item.Deleted)
                return;

            item.Movable = false;

            base.AddItem(item);
        }
        public Clanmaster(Serial serial)
            : base(serial)
        {

        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (ClanSystem.IsClanAligned(this))
                return true;

            return base.HandlesOnSpeech(from);
        }

        private static List<string> Commands = new List<string>() { "attack", "defend", "defenders attack", "all attack", "recall troops", "surrender", "status", "stand down", "relinquish", "lose", "wavedown", "expiretime", "totalkills", "quickclanwar", "equip" };

        public override void OnSpeech(SpeechEventArgs e)
        {
            try
            {
                Mobile from = e.Mobile;
                // an enemy trying to talk with us will get trouble if: 1) they use our name, or 2) they try to issue a command
                if (ClanSystem.IsEnemy(this, from) && (e.WasNamed(this) || Commands.Any(word => e.Speech.Contains(word, StringComparison.OrdinalIgnoreCase))))
                {
                    e.Handled = true;
#if !DEBUG
                    object o = Activator.CreateInstance(typeof(ClanOrc));
                    if (o is ClanOrc co)
                    {
                        co.ClanAlignment = this.ClanAlignment;
                        co.Combatant = from;
                        if (GetEquipment(this) != null)
                            Utility.EquipMobile(co, GetEquipment(this));
                        co.MoveToWorld(Utility.NearMobileLocation(from), this.Map);
                    }
#endif
                    SayTo(from, "You are an enemy, and I will not deal with you!");

                    // Drop the commander if he's turned enemy 
                    if (PlayerCommander == from)
                        Command = new Memory();
                    return;
                }
                else if (e.WasNamed(this) && ClanSystem.IsFriend(this, from) && Commands.Any(word => e.Speech.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    e.Handled = true;
                    #region Administrative Commands
                    // Quick Clan War (fast)
                    if (RemoveMyName(e.Speech).Equals("quickclanwar", StringComparison.OrdinalIgnoreCase) && e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    {
                        foreach (var kvp in Clanmaster.Instances)
                            if (kvp.Key.ChampSpawner != null)
                            {
                                Clanmaster cm = kvp.Key;
                                ChampMini engine = kvp.Value;
                                engine.Active = false;
                                engine.ClearMonsters = true;
                                engine.SpawnType = ChampLevelData.SpawnTypes.QuickClanOrcWar;

                                cm.EnemySpawners.Clear();
                                cm.MoveToWorld(from.Location, from.Map);
                                cm.Home = from.Location;
                                cm.CantWalk = true;
                            }

                        Say("Ok {0}.", from.Female ? "mam" : "sir");

                        // don't refresh for admin access
                    }
                    // total kills
                    else if (RemoveMyName(e.Speech).Equals("totalkills", StringComparison.OrdinalIgnoreCase) && e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    {
                        Say("{0} total kills {1}.", m_OurSpawner.TotalKills, from.Female ? "mam" : "sir");

                        // don't refresh for admin access
                    }
                    // when will this wave expire
                    else if (RemoveMyName(e.Speech).Equals("expiretime", StringComparison.OrdinalIgnoreCase) && e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    {
                        if (m_OurSpawner.Active == false)
                        {
                            Say("Troops inactive {0}.", from.Female ? "mam" : "sir");
                        }
                        else
                        {
                            DateTime expireTime = m_OurSpawner.ExpireTime;
                            Say("Level will expire in {0} {1}.", (expireTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss"), from.Female ? "mam" : "sir");
                        }

                        // don't refresh for admin access
                    }
                    else if (RemoveMyName(e.Speech).Equals("lose", StringComparison.OrdinalIgnoreCase) && e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    {
                        if (m_OurSpawner.Active == false)
                        {
                            Say("Troops inactive {0}.", from.Female ? "mam" : "sir");
                        }
                        else
                        {
                            m_OurSpawner.Active = false;
                            m_OurSpawner.WipeMonsters(include_abandoned: true);
                            // must come after Active change
                            EventSink.InvokeChampInfoEvent(new ChampInfoEventArgs(m_OurSpawner, ChampInfoEventArgs.ChampInfo.ChampComplete));
                            Say("Ok {0}.", from.Female ? "mam" : "sir");
                        }

                        // don't refresh for admin access
                    }
                    else if (RemoveMyName(e.Speech).StartsWith("wavedown", StringComparison.OrdinalIgnoreCase) && e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    {
                        string minutes = RemoveMyName(e.Speech).Replace("wavedown", "", StringComparison.OrdinalIgnoreCase).Trim();

                        if (!string.IsNullOrEmpty(minutes))
                        {
                            // reset the default expiry delay with 5 minutes for testing
                            for (int ix = 0; ix < m_OurSpawner.SpawnLevels.Count; ix++)
                                ((ChampLevelData)m_OurSpawner.SpawnLevels[ix]).m_ExpireDelay = TimeSpan.FromMinutes(int.Parse(minutes));

                            m_lastLevelExpiryCheck = DateTime.UtcNow;   // our local level checker
                            m_OurSpawner.RefreshLevelExpiry();          // force the engine to update it's global checker
                            Say("Ok {0}. Wavedown set to {1} minutes.", from.Female ? "mam" : "sir", int.Parse(minutes));
                        }
                        else
                            Say("Usage: wavedown <minutes> {0}.", from.Female ? "mam" : "sir");
                        // don't refresh for admin access
                    }
                    #endregion Administrative Commands
                    #region Clan Member Commands
                    else if (RemoveMyName(e.Speech).Equals("equip", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Equip.Recall(from))
                            Say("You're starting to annoy me {0}.", from.Female ? "mam" : "sir");
                        else if (m_OurSpawner.Equipment == null)
                            Say("We have no equipment to provide you with {0}.", from.Female ? "mam" : "sir");
                        else if (AtWarWithClan(m_OurSpawner).Count == 0)
                            Say("We are not at war {0}.", from.Female ? "mam" : "sir");
                        else if (Utility.CountLayers(from) > 0)
                            Say("I am not your mother {0}. You're going to have to undress yourself.", from.Female ? "mam" : "sir");
                        else
                        {
                            DressPlayer(from, m_OurSpawner.Equipment, Item.ItemBoolTable.DeleteOnLift);
                            Equip.Remember(from, TimeSpan.FromMinutes(5).TotalSeconds);
                        }
                        // don't refresh for member access
                    }
                    #endregion Clan Member Commands
                    #region Clan Commander Commands
                    else
                    {
                        if (PlayerCommander == null || PlayerCommander.NetState == null)
                        {
                            PlayerCommander = from;
                            Say("I am now taking orders from {0}.", PlayerCommander.Name);
                            ClanSystem.SendClanMessage(ClanAlignment, string.Format("{0} is now taking orders from {1}.", ClanAlignment, PlayerCommander.Name));
                        }
                        if (from != PlayerCommander)
                        {
                            Say("I only take orders from {0}.", PlayerCommander.Name);
                        }
                        else if (!PlayerCommander.Alive)
                        {
                            Say("I am sorry, I do not understand OooOOo.");
                        }
                        else if (Surrendered.Recall(true) != null)
                        {
                            DateTime dt = (DateTime)Surrendered.Recall(true).Context;
                            TimeSpan delta = DateTime.UtcNow - dt;
                            Say("You surrendered and must wait {0} before you may reenter the battle.", delta.ToString(@"mm\:ss"));
                            return;
                        }
                        else
                        {   // process orders

                            // stash the last order given for diagnostics
                            m_LastOrder = RemoveMyName(e.Speech);

                            // status
                            if (e.Speech.Contains("status", StringComparison.OrdinalIgnoreCase))
                            {   // our stats
                                if (ClanReady && m_OurSpawner.Active == false)
                                {
                                    Say("{0} are ready {1}.", base.ClanAlignment, from.Female ? "mam" : "sir");
                                    RefreshCommanderLoyalty();
                                }
                                else if (ClanReady && m_OurSpawner.Active == true && ClanQuiescent(m_OurSpawner))
                                {
                                    Say("Guardian troops stand ready {0}.", from.Female ? "mam" : "sir");
                                    RefreshCommanderLoyalty();
                                }
                                else if (ClanReady && m_OurSpawner.Active == true)
                                {   // we're on the move!
                                    Say("Our {0} wave deployed {1}. We have suffered {2} {3}.", Wave(m_OurSpawner), from.Female ? "mam" : "sir", Casualties(m_OurSpawner), Casualties(m_OurSpawner) == "1" ? "casualty" : "casualties");
                                    RefreshCommanderLoyalty();
                                }
                                // enemy stats
                                foreach (ChampMini ci in EnemySpawners)
                                {
                                    if (EnemyClanReady(ci) && ci.Active == false)
                                    {
                                        Say("{0} are ready {1}.", ci.ClanAlignment, from.Female ? "mam" : "sir");
                                        RefreshCommanderLoyalty();
                                    }
                                    else if (EnemyClanReady(ci) && ci.Active == true && ClanQuiescent(ci))
                                    {
                                        Say("{0} guardian troops stand ready {1}.", ci.ClanAlignment, from.Female ? "mam" : "sir");
                                        RefreshCommanderLoyalty();
                                    }
                                    else if (EnemyClanReady(ci) && ci.Active == true)
                                    {   // they're on the move!
                                        Say("{0} {1} wave deployed {2}. They have suffered {3} {4}.",
                                            ci.ClanAlignment,
                                            Wave(ci), from.Female ? "mam" : "sir", Casualties(ci),
                                            Casualties(ci) == "1" ? "casualty" : "casualties");
                                        RefreshCommanderLoyalty();
                                    }
                                }
                            }
                            else if (e.Speech.Contains("relinquish", StringComparison.OrdinalIgnoreCase))
                            {
                                Say("You have been relieved of duty {0}.", PlayerCommander.Name);
                                ClanSystem.SendClanMessage(ClanAlignment, string.Format("{0} has relinquished command of {1}.", PlayerCommander.Name, ClanAlignment));
                                RelinquishCommand();
                            }
                            // attack
                            else if (RemoveMyName(e.Speech).StartsWith("attack ", StringComparison.OrdinalIgnoreCase))
                            {
                                string clan = RemoveMyName(e.Speech).Replace("attack ", "", StringComparison.OrdinalIgnoreCase).Trim();
                                Clanmaster EnemyClanmaster = GetEnemyClanmaster(clan);
                                ChampMini es = GetEnemySpawner(clan);
                                if (!ClanRegistry.ContainsValue(Utility.StringToInt(clan)))
                                {
                                    Say("The {0} does not exist.", clan);
                                }
                                else if (EnemyClanmaster == null || es == null)
                                {
                                    Say("The {0} does not have a Clanmaster.", clan);
                                }
                                else if (Utility.StringToInt(ClanAlignment) == Utility.StringToInt(clan))    // attack self
                                {
                                    Say("We will not attack ourselves {0}.", from.Female ? "mam" : "sir");
                                }
                                else
                                {
                                    clan = FixClanName(clan);

                                    // register the war
                                    RegisterWar(EnemyClanmaster);

                                    // deploy!
                                    string old_destination = m_OurSpawner.NavDestination;
                                    m_OurSpawner.NavDestination = MakePath(ClanAlignment, clan);
                                    if (m_OurSpawner.NavDestination == null)    // bad path
                                    {
                                        Say("We do not know how to get to {0} {1}.", clan, from.Female ? "mam" : "sir");
                                    }
                                    else if (m_OurSpawner.Active == false)
                                    {
                                        m_OurSpawner.Active = true;
                                        Say("Troops deployed {0}.", from.Female ? "mam" : "sir");
                                        ClanSystem.SendClanMessage(ClanAlignment, string.Format("Our {0} wave being deployed.", Wave(m_OurSpawner)));
                                    }
                                    else
                                    {
                                        if (m_OurSpawner.Level == 0)
                                        {   // reassign the navigation of the initial guardians around the fort
                                            int count = m_OurSpawner.ReassignNavigation(MakePath(ClanAlignment, clan), homeRange: true);
                                            Say("{0} troops redeployed {1}.", count, from.Female ? "mam" : "sir");
                                        }
                                        else
                                            Say("Troops redeployed {0}.", from.Female ? "mam" : "sir");

                                        if (Utility.StringToInt(m_OurSpawner.NavDestination) != Utility.StringToInt(old_destination))
                                            ClanSystem.SendClanMessage(ClanAlignment, string.Format("Redeploying troops to {0}.", clan));
                                    }
                                }

                                RefreshCommanderLoyalty();
                            }
                            // defenders attack
                            else if (RemoveMyName(e.Speech).StartsWith("defenders attack ", StringComparison.OrdinalIgnoreCase))
                            {
                                string clan = RemoveMyName(e.Speech).Replace("defenders attack ", "", StringComparison.OrdinalIgnoreCase).Trim();
                                Clanmaster EnemyClanmaster = GetEnemyClanmaster(clan);
                                ChampMini es = GetEnemySpawner(clan);
                                if (!ClanRegistry.ContainsValue(Utility.StringToInt(clan)))
                                {
                                    Say("The {0} does not exist.", clan);
                                }
                                else if (EnemyClanmaster == null || es == null)
                                {
                                    Say("The {0} does not have a Clanmaster.", clan);
                                }
                                else if (Utility.StringToInt(ClanAlignment) == Utility.StringToInt(clan))    // attack self
                                {
                                    Say("We will not attack ourselves {0}.", from.Female ? "mam" : "sir");
                                }
                                else
                                {
                                    clan = FixClanName(clan);

                                    // register the war
                                    RegisterWar(EnemyClanmaster);

                                    // future spawn will attack this clan
                                    m_OurSpawner.NavDestination = MakePath(ClanAlignment, clan);
                                    if (m_OurSpawner.NavDestination == null)    // bad path
                                    {
                                        Say("We do not know how to get to {0} {1}.", clan, from.Female ? "mam" : "sir");
                                    }
                                    else if (m_OurSpawner.Active == false)
                                    {
                                        m_OurSpawner.Active = true;
                                        Say("Troops deployed {0}.", from.Female ? "mam" : "sir");
                                        ClanSystem.SendClanMessage(ClanAlignment, string.Format("Deploying troops to {0}.", clan));
                                    }
                                    else
                                    {   // reassign the navigation of the troops without a nav point 'around the fort'
                                        int count = m_OurSpawner.ReassignNavigation(MakePath(ClanAlignment, clan), homeRange: true);
                                        Say("{0} troops redeployed {1}.", count, from.Female ? "mam" : "sir");
                                        ClanSystem.SendClanMessage(ClanAlignment, string.Format("Redeploying {0} troops to {1}.", count, clan));
                                    }
                                }

                                RefreshCommanderLoyalty();
                            }
                            // all attack
                            else if (RemoveMyName(e.Speech).StartsWith("all attack ", StringComparison.OrdinalIgnoreCase))
                            {
                                string clan = RemoveMyName(e.Speech).Replace("all attack ", "", StringComparison.OrdinalIgnoreCase).Trim();
                                Clanmaster EnemyClanmaster = GetEnemyClanmaster(clan);
                                ChampMini es = GetEnemySpawner(clan);
                                if (!ClanRegistry.ContainsValue(Utility.StringToInt(clan)))
                                {
                                    Say("The {0} does not exist.", clan);
                                }
                                else if (EnemyClanmaster == null || es == null)
                                {
                                    Say("The {0} does not have a Clanmaster.", clan);
                                }
                                else if (Utility.StringToInt(ClanAlignment) == Utility.StringToInt(clan))    // attack self
                                {
                                    Say("We will not attack ourselves {0}.", from.Female ? "mam" : "sir");
                                }
                                else
                                {
                                    clan = FixClanName(clan);

                                    // register the war
                                    RegisterWar(EnemyClanmaster);

                                    // future spawn will attack this clan
                                    m_OurSpawner.NavDestination = MakePath(ClanAlignment, clan);
                                    if (m_OurSpawner.NavDestination == null)    // bad path
                                    {
                                        Say("We do not know how to get to {0} {1}.", clan, from.Female ? "mam" : "sir");
                                    }
                                    else if (m_OurSpawner.Active == false)
                                    {
                                        m_OurSpawner.Active = true;
                                        Say("Troops deployed {0}.", from.Female ? "mam" : "sir");
                                        ClanSystem.SendClanMessage(ClanAlignment, string.Format("Deploying troops to {0}.", clan));
                                    }
                                    else
                                    {   // reassign the navigation of all troops 
                                        int count = m_OurSpawner.ReassignNavigation(MakePath(ClanAlignment, clan), homeRange: false, force: true);
                                        Say("{0} troops redeployed {1}.", count, from.Female ? "mam" : "sir");
                                        ClanSystem.SendClanMessage(ClanAlignment, string.Format("Redeploying {0} troops to {1}.", count, clan));
                                    }
                                }

                                RefreshCommanderLoyalty();
                            }
                            // defend
                            else if (RemoveMyName(e.Speech).Equals("defend", StringComparison.OrdinalIgnoreCase))
                            {
                                m_OurSpawner.NavDestination = null;
                                if (m_OurSpawner.Active == false)
                                {
                                    m_OurSpawner.Active = true;
                                    Say("All troops guarding the fort {0}.", from.Female ? "mam" : "sir");
                                    ClanSystem.SendClanMessage(ClanAlignment, "All troops guarding the fort.");
                                }
                                else
                                {
                                    Say("Newly arriving troops will guard the fort {0}.", from.Female ? "mam" : "sir");
                                    ClanSystem.SendClanMessage(ClanAlignment, "Newly arriving troops will guard the fort.");
                                }

                                RefreshCommanderLoyalty();
                            }
                            // surrender
                            else if (RemoveMyName(e.Speech).Equals("surrender", StringComparison.OrdinalIgnoreCase))
                            {
                                if (m_OurSpawner.Active == false)
                                {
                                    Say("Troops inactive {0}.", from.Female ? "mam" : "sir");
                                }
                                else if (m_OurSpawner.IsFinalLevel)
                                {
                                    Say("It is too late for that {0}. Our champion is already en route.", from.Female ? "mam" : "sir");
                                }
                                else
                                {
                                    m_OurSpawner.Active = false;
                                    ResetChamp(m_OurSpawner);
                                    NotifyEnemies(m_OurSpawner, string.Format("{0} have surrendered", ClanAlignment));
                                    EndWar(m_OurSpawner);
                                    Say("Troops standing down {0}.", from.Female ? "mam" : "sir");
                                    ClanSystem.SendClanMessage(ClanAlignment, "We have surrendered. Troops standing down.");
#if !DEBUG
                                Surrendered.Remember(true, DateTime.UtcNow + TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10).TotalSeconds);
#endif
                                }

                                RefreshCommanderLoyalty();
                            }
                            // stand down
                            else if (RemoveMyName(e.Speech).Equals("stand down", StringComparison.OrdinalIgnoreCase))
                            {
                                if (m_OurSpawner.Active == false)
                                {
                                    Say("Troops inactive {0}.", from.Female ? "mam" : "sir");
                                }
                                else if (AtWarWithClan(m_OurSpawner).Count != 0)
                                {
                                    Say("Unable to stand down while we are still at war {0}.", from.Female ? "mam" : "sir");
                                }
                                else
                                {
                                    ResetChamp(m_OurSpawner);
                                    EndWar(m_OurSpawner);
                                    Say("Troops standing down {0}.", from.Female ? "mam" : "sir");
                                    ClanSystem.SendClanMessage(ClanAlignment, "Troops standing down.");
#if !DEBUG
                                Surrendered.Remember(true, DateTime.UtcNow + TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10).TotalSeconds);
#endif
                                }

                                RefreshCommanderLoyalty();
                            }
                            // recall troops
                            else if (RemoveMyName(e.Speech).Equals("recall troops", StringComparison.OrdinalIgnoreCase))
                            {
                                if (m_OurSpawner.Active == false)
                                {
                                    Say("Troops inactive {0}.", from.Female ? "mam" : "sir");
                                }
                                else
                                {
                                    // reassign the navigation of the troops to null, bring them home
                                    m_OurSpawner.ClearNavigation();
                                    int count = m_OurSpawner.BringHome(this);
                                    Say("{0} troops recalled {1}.", count, from.Female ? "mam" : "sir");
                                    ClanSystem.SendClanMessage(ClanAlignment, string.Format("{0} troops recalled to fort.", count));
                                }

                                RefreshCommanderLoyalty();
                            }
                        }
                    }
                    #endregion Clan Commander Commands
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            base.OnSpeech(e);
        }
        public static Backpack GetEquipment(BaseCreature bc)
        {
            if (bc != null)
                if (bc.Spawner != null)
                    return bc.Spawner.Equipment;
                else if (bc.Engine != null)
                    return bc.Engine.Equipment;

            return null;
        }
        public void CheckWar(BaseCreature soldier)
        {
            /* Here is where can do interesting things like auto-start a war. For now, if we have a Commander
             *  we just notify.
             */

            // if we have a commander, just let him (and the clan) know
            if (PlayerCommander != null)
            {
                List<int> seen = new List<int>();
                List<Mobile> list = AggressingMobiles(soldier); // guaranteed to have an alignment
                foreach (Mobile m in list)
                    if (!AtWarWithClan(m))  // we're not at war with this clam, alert our troops
                    {
                        if (Alert.Recall(ClanSystem.GetClanAlignment(m)) != null)
                            continue; // we've already know this clan is attacking us, do nothing
                        else if (!seen.Contains(ClanSystem.GetClanAlignment(m)))
                        {   // notify the clan we are being attacked
                            string text = string.Format("{0} was killed by [{1}] at {2}.", soldier.Name,
                                Utility.SentenceCamel(ClanSystem.GetClanAlignmentAsString(m)),
                                Utility.SextantLocationString(m));
                            ClanSystem.SendClanMessage(this.ClanAlignment, text, hue: 0x22);
                            Alert.Remember(ClanSystem.GetClanAlignment(m), TimeSpan.FromMinutes(5).TotalSeconds);
                            seen.Add(ClanSystem.GetClanAlignment(m));
                        }
                    }
            }
            else// FU! We won't take the shit unanswered!
            {
                ;// maybe we will do something here like auto-start a war
                ;
                ;
                ;
            }
        }
        private List<Mobile> AggressingMobiles(BaseCreature soldier)
        {
            List<Mobile> list = new List<Mobile>();
            foreach (var info in soldier.Aggressors)
            {
                if (info.Attacker.ClanAlignment != null)
                    list.Add(info.Attacker);
            }
            return list;
        }
        private List<ChampMini> AggressingClans(BaseCreature soldier)
        {
            List<ChampMini> list = new List<ChampMini>();
            foreach (var info in soldier.Aggressors)
            {
                if (info.Attacker.ClanAlignment != null)
                    list.Add(FindEngine(info.Attacker.ClanAlignment));
            }
            return list;
        }
        private ChampMini FindEngine(string clanAlignment)
        {
            int clan = Utility.StringToInt(clanAlignment);
            foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                if (Utility.StringToInt(kvp.Key.ClanAlignment) == clan)
                    return kvp.Value;
            return null;
        }
        public static Clanmaster GetEnemyClanmaster(string clan_name)
        {
            if (clan_name != null)
            {
                foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                    if (kvp.Key.ClanAlignment.Equals(clan_name, StringComparison.OrdinalIgnoreCase))
                        return kvp.Key;
            }
            return null;
        }
        private static string FixClanName(string clan_name)
        {
            if (clan_name != null)
                foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                    if (kvp.Key.ClanAlignment.Equals(clan_name, StringComparison.OrdinalIgnoreCase))
                        return kvp.Key.ClanAlignment;
            return null;
        }
        private static string MakePath(string clan_source, string clan_dest)
        {
            clan_source = FixClanName(clan_source);
            clan_dest = FixClanName(clan_dest);
            string path = clan_source + " => " + clan_dest;

            System.Diagnostics.Debug.Assert(ValidPath(path));

            if (ValidPath(path)) // check individual components
                return path;        // standard format

            return null;
        }
        private static bool ValidPath(string path)
        {
            if (NavigationBeacon.Registry.ContainsKey(path))
                return true;
            return false;
        }
        private static ChampMini GetEnemySpawner(string clan_name)
        {
            if (clan_name != null)
            {
                foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                    if (kvp.Key.ClanAlignment.Equals(clan_name, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
            }
            return null;
        }
        private string RemoveMyName(string s)
        {
            return s.Replace(this.Name + " ", "", StringComparison.OrdinalIgnoreCase).Trim();
        }
        private static string Casualties(ChampMini ci)
        {
            return ci.TotalKills.ToString();
        }
        private static string Wave(ChampMini ci)
        {
            switch (ci.Level)
            {
                case 0:
                    return "1st";
                case 1:
                    return "2nd";
                case 2:
                    return "3rd";
                case 3:
                    return "4th";
                default:
                    return (ci.Level + 1).ToString() + "th";
            }
        }
        private bool ClanReady
        {
            get { return m_OurSpawner != null; }
        }
        private bool EnemyClanReady(ChampMini ci)
        {
            return ci != null;
        }
        private static bool ClanQuiescent(ChampMini ci)
        {
            return ci != null && ci.Level == 0 && ci.NavDestination == null;
        }
        public Mobile PlayerCommander
        {
            get { return Command.RecallFirst() as Mobile; }
            set { Command.Remember(value, value, TimeSpan.FromHours(1).TotalSeconds); }
        }
        private void RefreshCommanderLoyalty()
        {   // every time the commander issues a command, their 1 hr timer gets refreshed
            if (Command.RecallFirst() is Mobile commander)
                Command.Remember(commander, commander, TimeSpan.FromHours(1).TotalSeconds);
        }
        public void RelinquishCommand()
        {
            Command.Forget(PlayerCommander);
        }
        #region End Game
        private static List<Clanmaster> AtWarWithClan(ChampMini clan, bool active = false)
        {
            List<Clanmaster> list = new List<Clanmaster>();
            if (clan != null)
                foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                    if (kvp.Value != clan && kvp.Key.EnemySpawners.Contains(clan))
                        if (active == false || !ClanQuiescent(kvp.Value))
                            list.Add(kvp.Key);

            return list;
        }
        private bool AtWarWithClan(Mobile m)
        {
            if (m is BaseCreature bc && !string.IsNullOrEmpty(bc.ClanAlignment))
                foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                    if (Utility.StringToInt(kvp.Key.ClanAlignment) == Utility.StringToInt(bc.ClanAlignment))
                        if (EnemySpawners.Contains(kvp.Value))
                            return true;

            return false;
        }
        public static void ChampInfo_EventSink(ChampInfoEventArgs e)
        {
            if (e.Engine is ChampMini cm && cm.RootParent is Clanmaster)
                ChampStatus(cm, e.Info);
        }
        public static void Despawn(ChampMini engine)
        {
            int have = engine.AllMonsters.Count;
            int want = engine.Lvl_MaxMobs;
            List<BaseCreature> keep = new List<BaseCreature>();

            // keep the mobs currently Engaged With a Player
            foreach (object o in engine.AllMonsters)
                if (o is BaseCreature bc)
                    if (EngagedWithPlayer(bc))
                        keep.Add(bc);

            // now, keep all the others that are in sight of a player
            foreach (object o in engine.AllMonsters)
                if (o is BaseCreature bc)
                    if (CanSeePlayer(bc))
                        keep.Add(bc);

            keep = keep.Take(want).ToList();
            ;

            if (keep.Count < want)
            {
                foreach (object o in engine.AllMonsters)
                    if (o is BaseCreature bc && !keep.Contains(bc))
                    {
                        keep.Add(bc);
                        if (keep.Count == want)
                            break;
                    }
                    ;
            }
            else;// all good, we have what we want

            // cleanup lists 
            foreach (object o in engine.AllMonsters)
                if (o is BaseCreature bc && !keep.Contains(bc))
                    bc.Delete();

            engine.Monsters.Clear();
            engine.FreeMonsters.Clear();
            engine.AbandonedMonsters.Clear();
            engine.Monsters.AddRange(keep);
        }
        private static bool CanSeePlayer(BaseCreature m)
        {
            if (Spawner.NearPlayer(m.Map, m.Location))
                return true;
            return false;
        }
        private static bool EngagedWithPlayer(BaseCreature m)
        {
            List<AggressorInfo> table = m.Aggressors;
            if (table.Count > 0)
            {
                for (int i = 0; i < table.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)table[i];
                    if (!info.Expired)
                        if ((info.Attacker.Player && m.GetDistanceToSqrt(m) < 12) || (info.Defender.Player && m.GetDistanceToSqrt(m) < 12))
                            return true;
                }
            }

            table = m.Aggressed;
            if (table.Count > 0)
            {
                for (int i = 0; i < table.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)table[i];
                    if (!info.Expired)
                        if ((info.Attacker.Player && m.GetDistanceToSqrt(m) < 12) || (info.Defender.Player && m.GetDistanceToSqrt(m) < 12))
                            return true;
                }
            }
            return false;
        }
        public static void ChampStatus(ChampMini engine, ChampInfoEventArgs.ChampInfo info)
        {
            try
            {
                if (info.HasFlag(ChampInfoEventArgs.ChampInfo.Activated))
                { 
                }
                else if (info.HasFlag(ChampInfoEventArgs.ChampInfo.Deactivated))
                {
                    engine.NavDestination = null;
                }
                else if (info.HasFlag(ChampInfoEventArgs.ChampInfo.LevelDown))
                {
                    /* Background: When a champion spawn "levels down", it means the players are doing poorly as they are unable to 
                     *  advance the spawn. However, when a clan spawner (ChampMini) levels down, it means that the clan in question
                     *  is surviving, standing firm, or holding their own i.e.,
                     *      (the enemy clan is unable to defeat the wave before the timeout.)
                     * Our Clan Wars MiniChamp spawner has both WipeOnLevelUP and WipeOnLevelDown disabled.
                     *  We do this so we can fine tune despawn. For instance, we never want to despawn troops on a level-up because
                     *  it's 'jarring'. We therefore require players to eliminate or deal with the delta of kills needed and mobiles spawned.
                     *  WipeOnLevelDown is a bit more complex. We want to despawn on level down, but we try and do it in
                     *      a somewhat stealthy fashion. I.e., not in front of anyone. Also prioritize preserving mobs in
                     *      active combat with a player
                     */

                    // first inform those that are "holding their own"
                    Clanmaster clanmaster = engine.RootParent as Clanmaster;
                    if (clanmaster != null)
                        if (info.HasFlag(ChampInfoEventArgs.ChampInfo.LevelCounter))    // decrease level counter    
                            ClanSystem.SendClanMessage(clanmaster.ClanAlignment, string.Format("We are holding our own. Resuming {0} wave.", Wave(clanmaster.ChampSpawner)));
                        else if (info.HasFlag(ChampInfoEventArgs.ChampInfo.Level))      // wipe mobs when leveling down
                            ClanSystem.SendClanMessage(clanmaster.ClanAlignment, string.Format("We are holding our own. Some troops dismissed."));
                        else
                            ClanSystem.SendClanMessage(clanmaster.ClanAlignment, string.Format("We are holding our own."));

                    // cleanup superflous mobiles (we may want to consider ChampInfo.LevelCounter vs ChampInfo.Level
                    Despawn(engine);

                    // now inform those clans that are having trouble with this clan
                    if (clanmaster != null)
                    {
                        List<Clanmaster> stillFighting = AtWarWithClan(engine);
                        foreach (Clanmaster cm in stillFighting)
                        {
                            string message = string.Format("We are having some trouble putting down {0}.", clanmaster.ClanAlignment);
                            ClanSystem.SendClanMessage(cm.ClanAlignment, message);
                        }
                    }
                }
                else if (info.HasFlag(ChampInfoEventArgs.ChampInfo.LevelUp))
                {
                    Clanmaster clanmaster = engine.RootParent as Clanmaster;
                    if (clanmaster != null)
                        ClanSystem.SendClanMessage(clanmaster.ClanAlignment, string.Format("Our {0} wave being deployed.", Wave(clanmaster.ChampSpawner)));
                }
                else if (info.HasFlag(ChampInfoEventArgs.ChampInfo.ChampComplete))
                {
                    ChampMini loser = engine;
                    ChampMini winner = null;
                    bool oneOnOne = IsOneOnOne(loser, ref winner);  // we can only have winners and losers when it's one-on-one
                    bool nonWar = IsNonWar(loser);                  // no war, just players doing what they do    
                    Clanmaster clanmaster = null;
                    Mobile commander = null;
                    string message = null;
                    string local = null;
                    if (oneOnOne)                                   // clearly a one-on-one battle. We have a winner and loser
                    {
                        /*
                         * Loser first
                         */
                        clanmaster = GetClanMaster(loser);
                        commander = clanmaster.PlayerCommander;
                        message = null;
                        local = null;
                        if (commander == null)
                            message = string.Format("We lost by forfeit. Our commander is nowhere to be found.");
                        else
                        {
                            message = string.Format("We lost.");
                            local = string.Format("We lost {0}.", commander.Female ? "mam" : "sir");
                        }
                        ClanSystem.SendClanMessage(clanmaster.ClanAlignment, message);
                        if (commander != null)
                            clanmaster.Say(local);

                        ResetChamp(loser);

                        // cleanup enemy lists
                        EndWar(loser);

                        /*
                         * Winner 
                         */
                        commander = GetClanMaster(winner).PlayerCommander;
                        message = null;
                        local = null;
                        
                        message = string.Format("We won!");
                        if (commander != null)
                            local = string.Format("We won {0}!", commander.Female ? "mam" : "sir");

                        if (GetClanMaster(winner).ChampSpawner != null)
                            ResetChamp(GetClanMaster(winner).ChampSpawner);

                        // cleanup enemy lists
                        if (GetClanMaster(winner).ChampSpawner != null)
                            EndWar(GetClanMaster(winner).ChampSpawner);

                        ClanSystem.SendClanMessage(GetClanMaster(winner).ClanAlignment, message);
                        if (commander != null)
                            GetClanMaster(winner).Say(local);

                        return;
                    }
                    else if (nonWar)                                // Not a war, either the loser was taken out by players,
                    {                                               //  or other NPCs (possibly enemy, but not at war.)
                        clanmaster = GetClanMaster(loser);
                        commander = clanmaster.PlayerCommander;
                        message = string.Format("We have been defeated!");
                        if (commander != null)
                            local = string.Format("We have been defeated {0}!", commander.Female ? "mam" : "sir");

                        ClanSystem.SendClanMessage(clanmaster.ClanAlignment, message);
                        if (commander != null)
                            clanmaster.Say(local);
                        return;
                    }
                    else                                            // A multiway war, more than 2 warring parties.
                    {                                               // for now, we handle it the same as the nonWar case
                        clanmaster = GetClanMaster(loser);
                        commander = clanmaster.PlayerCommander;
                        message = string.Format("We have been defeated!");
                        if (commander != null)
                            local = string.Format("We have been defeated {0}!", commander.Female ? "mam" : "sir");

                        ClanSystem.SendClanMessage(clanmaster.ClanAlignment, message);
                        if (commander != null)
                            clanmaster.Say(local);

                        // now notify all enemies this clan has been defeated
                        message = string.Format("{0} has been defeated!", clanmaster.ClanAlignment);
                        foreach (Clanmaster cm in AtWarWithClan(loser))
                            ClanSystem.SendClanMessage(cm.ClanAlignment, message);

                        ResetChamp(loser);

                        // cleanup enemy lists
                        EndWar(loser);

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        private static Clanmaster GetClanMaster(ChampMini engine)
        {
            return engine != null ? engine.RootParent as Clanmaster : null;
        }
        /*
            (stillFighting[0].EnemySpawners[0].RootParent as Clanmaster).ClanAlignment
            "Darkfang Horde"
            (stillFighting[0].EnemySpawners[1].RootParent as Clanmaster).ClanAlignment
            "Bloodskull Orks"
            (loser.RootParent as Clanmaster).ClanAlignment
            "Bloodskull Orks"
         */
        private static bool IsNonWar(ChampMini engine)
        {
            return AtWarWithClan(engine).Count == 0;
        }
        private static bool IsOneOnOne(ChampMini loser, ref ChampMini winner)
        {
            /* 
             * if we are only fighting them and they are only fighting us, then it's a one-on-one
             */

            List<Clanmaster> stillFighting = AtWarWithClan(loser);
            
            if (stillFighting.Count != 1)
                return false;                   // too many attacking us to be one-on-one

            if (stillFighting[0].EnemySpawners.Count != 1)
                return false;                   // they are at war with more than us

            Clanmaster cm = (loser.RootParent as Clanmaster);
            if (cm != null)
            {   
                if (!cm.EnemySpawners.Contains(stillFighting[0].ChampSpawner))
                    return false;               // we are at war with them

                if (!stillFighting[0].EnemySpawners.Contains(loser))
                    return false;               // they are at war with us

                winner = stillFighting[0].ChampSpawner;

                return true;                    // it's a one-on-one baby!
            }

            return false;
        }
        public static void ResetChamp(ChampMini engine)
        {
            if (engine != null)
            {
                bool active = engine.Active;
                bool restartTimerEnabled = engine.RestartTimer;
                TimeSpan restartTimerDelay = engine.RestartDelay;
                engine.Active = false;
                engine.ClearMonsters = true;
                engine.ClearAbandonedMonsters = true;
                engine.ReloadTables = true;
                engine.TotalKills = 0;
                engine.NavDestination = null;
                engine.RestartDelay = restartTimerDelay; 
                engine.RestartTimer = restartTimerEnabled;
                engine.Active = active;
            }
        }
        private void RegisterWar(Clanmaster enemy)
        {
            // we war them
            if (!EnemySpawners.Contains(enemy.ChampSpawner))
                EnemySpawners.Add(enemy.ChampSpawner);
            // they war us
            if (!enemy.EnemySpawners.Contains(ChampSpawner))
                enemy.EnemySpawners.Add(ChampSpawner);
        }
        private static void EndWar(ChampMini engine)
        {
            // the clan is removed from the list of all battling clans
            List<Clanmaster> stillFighting = AtWarWithClan(engine);
            foreach (Clanmaster cm in stillFighting)
                if (cm.EnemySpawners.Contains(engine))
                    cm.EnemySpawners.Remove(engine);

            // loser is no longer fighting anyone
            Clanmaster clanmaster = engine.RootParent as Clanmaster;
            if (clanmaster != null)
                clanmaster.EnemySpawners.Clear();
        }
        private static void NotifyEnemies(ChampMini speaker, string text)
        {
            List<Clanmaster> stillFighting = AtWarWithClan(speaker);
            foreach (Clanmaster cm in stillFighting)
                ClanSystem.SendClanMessage(cm.ClanAlignment, text);
        }
        #endregion End Game
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version;
            writer.Write(version = 2);

            // version 2
            writer.Write(PlayerCommander);
            writer.Write(m_LastOrder);

            // version 1
            writer.WriteItemList<ChampMini>(EnemySpawners);

            // version 0
            writer.Write(m_Invulnerable);
            writer.Write(m_OurSpawner);
            // obsolete in version 1
            //writer.Write(m_EnemySpawner);
            writer.Write(m_Equipment);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch(version)
            {
                case 2:
                    {
                        PlayerCommander = reader.ReadMobile();
                        m_LastOrder = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        EnemySpawners = reader.ReadItemList<ChampMini>();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Invulnerable = reader.ReadBool();
                        Instances.Add(this, m_OurSpawner = (ChampMini)reader.ReadItem());
                        if (version == 0)
                            /*m_EnemySpawner = (ChampMini)*/ EnemySpawners.Add(reader.ReadItem() as ChampMini);
                        m_Equipment = (Backpack)reader.ReadItem();
                        break;
                    }
            }

            /* Initialize */
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            if (m_OurSpawner is ChampMini)
            {
                // make sure our champ engine has the proper 'champ' selected
                if (m_OurSpawner.SpawnType != ChampLevelData.SpawnTypes.ClanOrcWar)
                    m_OurSpawner.SpawnType = ChampLevelData.SpawnTypes.ClanOrcWar;

                // don't wipe spawn on level up
                m_OurSpawner.WipeOnLevelUp = false;

                // don't wipe spawn on level down
                m_OurSpawner.WipeOnLevelDown = false;
            }
        }
    }
}
