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

/* Scripts/Commands/Nuke.cs
 * CHANGELOG:
 *  9/12/2024, Adam
 *      Add MazeGenerator for the teleport maze in Hyloth
 *	8/5/2024, Adam
 *		Initial Version
 */

using Server.Diagnostics;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using Server.Engines.ChampionSpawn;
using System.Linq;

namespace Server.Commands
{
    public class Nuke
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Nuke", AccessLevel.Administrator, new CommandEventHandler(Nuke_OnCommand));
        }

        [Usage("Nuke [KeysOnTheOcean|PostMessage|LogElementals|StablePets|KillAES]")]
        [Description("Does whatever. Usually a one-time patch.")]
        private static void Nuke_OnCommand(CommandEventArgs e)
        {

            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage("Usage: Nuke <command> [arg|arg|etc.]");
                return;
            }
            try
            {
                switch (e.Arguments[0].ToLower())
                {
                    default:
                        {
                            e.Mobile.SendMessage("Usage: Nuke [KeysOnTheOcean|PostMessage|LogElementals|StablePets]");
                            e.Mobile.SendMessage(string.Format("Nuke does not implement the command: {0}", e.Arguments[0]));
                        }
                        return;

                    case "dupeplayers":
                        DupePlayers(e);
                        break;

                    case "prepworldfordistribution":
                        PrepWorldForDistribution(e);
                        break;

                    #region Build Player
                    case "makemurderer":
                        MakeMurderer(e);
                        break;

                    case "buildwarrior":
                        BuildWarrior(e);
                        break;

                    case "buildmage":
                        BuildMage(e);
                        break;

                    case "buildadam":
                        BuildAdam(e);
                        break;

                    //case "buildmobile":
                    //    BuildMobile(e);
                    //    break;
                    #endregion Build Player

                    case "buildmaze":
                        BuildMaze(e);
                        break;

                    case "patchbeacons":
                        PatchBeacons(e);
                        break;

                    case "loadbeacons":
                        LoadBeacons(e);
                        break;

                    case "dupedeepsimple":
                        DupeDeepSimple(e);
                        break;

                    case "deleteclanorcs":
                        DeleteClanOrcs(e);
                        break;

                    case "countclanorcs":
                        CountClanOrcs(e);
                        break;

                    case "resetclanorcs":
                        ResetClanOrcs(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        private static void ResetClanOrcs(CommandEventArgs e)
        {
            foreach (KeyValuePair<Clanmaster, ChampMini> kvp in Clanmaster.Instances)
                if (kvp.Key.Controller != null)
                {
                    kvp.Key.EnemySpawners.Clear();
                    Clanmaster.ResetChamp(kvp.Key.Controller);
                }
        }
        private static void CountClanOrcs(CommandEventArgs e)
        {
            List<string> list = new List<string>();
            foreach (Mobile m in World.Mobiles.Values)
                if (m is ClanOrc co && !co.Deleted)
                    list.Add(co.ClanAlignment);

            string clan = e.ArgString.Replace(e.GetString(0), "", StringComparison.OrdinalIgnoreCase).Trim();
            if (!string.IsNullOrEmpty(clan))
            {
                Clanmaster cm = Clanmaster.GetEnemyClanmaster(clan);
                if (cm == null)
                {
                    e.Mobile.SendMessage("There is no {0} clan", clan);
                    return;
                }
                list.RemoveAll(c => !c.Equals(clan, StringComparison.OrdinalIgnoreCase));

                e.Mobile.SendMessage("There are {0} {1} Clan Orc(s)", list.Count, clan);
                return;
            }

            e.Mobile.SendMessage("There are {0} Clan Orc(s)", list.Count);
        }
        private static void DeleteClanOrcs(CommandEventArgs e)
        {
            List<ClanOrc> list = new List<ClanOrc>();
            foreach (Mobile m in World.Mobiles.Values)
                if (m is ClanOrc co)
                    list.Add(co);

            foreach (ClanOrc co in list)
                co.Delete();
        }

        #region Dupe Deep Simple

        private static void DupeDeepSimple(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the container you would like to dupe...");
            e.Mobile.Target = new DupeTarget(e.Mobile);
        }
        public class DupeTarget : Target
        {
            private Mobile m_From;
            public DupeTarget(Mobile from)
                : base(17, true, TargetFlags.None)
            {
                m_From = from;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Container cont)
                {
                    Container new_cont = Utility.DupeDeepSimple(cont);
                    if (new_cont != null)
                    {
                        from.Backpack.AddItem(new_cont);
                        from.SendMessage("Done.");
                    }
                    else
                        from.SendMessage("Failed to create deep copy.");
                }
                else
                {
                    from.SendMessage("That is not a container.");
                    return;
                }
            }
        }

        #endregion Dupe Deep Simple

        #region Patch Beacons
        private static void PatchBeacons(CommandEventArgs e)
        {
            foreach (KeyValuePair<string, List<NavigationBeacon>> list in NavigationBeacon.Registry)
                foreach (NavigationBeacon nb in list.Value)
                    if (nb.Journey == "Rockbiter Clan")
                        nb.Journey = "Bloodskull Orks => Rockbiter Clan";
                    else if (nb.Journey == "Bloodskull Orks")
                        nb.Journey = "Rockbiter Clan => Bloodskull Orks";
        }
        #endregion Patch Beacons
        #region Load Beacons
        private static void LoadBeacons(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            pm.JumpIndex = 0;
            pm.JumpList = new System.Collections.ArrayList();
            List<NavigationBeacon> beacon_list = new List<NavigationBeacon>();
            foreach (KeyValuePair<string, List<NavigationBeacon>> list in NavigationBeacon.Registry)
                foreach (NavigationBeacon nb in list.Value)
                    if (nb.Journey == e.GetString(1))
                        beacon_list.Add(nb);

            var sortedItems =  beacon_list.OrderBy(item => item.Ring).ToList();
            pm.JumpList.AddRange(sortedItems);
        }
        #endregion Load Beacons
        #region Maze Builder

        private static void BuildMaze(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the item you would like to dupe for maze construction...");
            e.Mobile.Target = new MazeTarget(e.Mobile.Location, width: 41, height: 7);
        }
        public class MazeTarget : Target
        {
            private Point3D m_Origin;
            private int m_Width, m_Height;
            public MazeTarget(Point3D px, int width, int height)
                : base(17, true, TargetFlags.None)
            {
                m_Origin = px;
                m_Width = width;
                m_Height = height;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                int id = 0;
                string name = string.Empty;
                if (target is Item item)
                {
                    MazeGenerator maze = new MazeGenerator(m_Width, m_Height);
                    maze.DisplayMaze(item, m_Origin);
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }
            }
        }
        class MazeGenerator
        {
            private int width, height;
            private int[,] maze;
            private Random rand = new Random();

            // Directions for movement: Up, Down, Left, Right
            private int[] dx = { 0, 0, -1, 1 };
            private int[] dy = { -1, 1, 0, 0 };

            public MazeGenerator(int width, int height)
            {
                this.width = width;
                this.height = height;
                maze = new int[height, width];
                GenerateMaze(0, 0);
            }

            private void GenerateMaze(int x, int y)
            {
                maze[y, x] = 1; // Mark the cell as part of the maze (visited)

                // Create a randomized direction order
                int[] directions = { 0, 1, 2, 3 };
                Shuffle(directions);

                // Iterate over all possible directions in a random order
                foreach (int direction in directions)
                {
                    int nx = x + dx[direction] * 2; // Calculate the new x position
                    int ny = y + dy[direction] * 2; // Calculate the new y position

                    // Check if the new position is within bounds and not visited
                    if (nx >= 0 && ny >= 0 && nx < width && ny < height && maze[ny, nx] == 0)
                    {
                        maze[y + dy[direction], x + dx[direction]] = 1; // Carve the wall between
                        GenerateMaze(nx, ny); // Recursively carve the next cell
                    }
                }
            }

            // Shuffle an array (Fisher-Yates shuffle)
            private void Shuffle(int[] array)
            {
                for (int i = array.Length - 1; i > 0; i--)
                {
                    int j = rand.Next(i + 1);
                    int temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }

            // Display the maze in the console
            public void DisplayMaze(Item item, Point3D origin)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Console.Write(maze[y, x] == 1 ? "  " : "██"); // Empty space or wall
                        if (maze[y, x] != 1)
                        {
                            Item dest = (Item)Activator.CreateInstance(item.GetType());
                            Container parent = SaveParent(item);
                            Spawner.CopyProperties(dest, item);
                            RestoreParent(item, parent);
                            Point3D point = new Point3D(origin.X + x, origin.Y + y, origin.Z);
                            dest.MoveToWorld(point, item.Map);
                        }
                    }
                    Console.WriteLine();
                }
            }
            private Container SaveParent(Item item)
            {
                if (item.Parent is Container cont)
                {
                    cont.RemoveItem(item);
                    return cont;
                }
                return null;
            }
            private void RestoreParent(Item item, Container cont)
            {
                if (cont != null)
                    cont.AddItem(item);
            }
        }
        #endregion Maze Builder
        #region Build Player
        private static void BuildAdam(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildAdamTarget(); // Call our target
        }
        public class BuildAdamTarget : Target
        {
            public BuildAdamTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    // all skills 100
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 30000;
                    player.Mana = player.RawInt = 30000;
                    player.Hits = player.RawStr = 30000;

                    player.Karma = 30000;
                    player.Fame = 30000;
                    from.SendMessage("Adam built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        private static void BuildMage(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildMageTarget(); // Call our target
        }
        private static void NormalizePlayer(Mobile m)
        {
            if (!m.Alive)
                m.Resurrect();
            //m.Blessed = false;
            //m.AccessLevel = AccessLevel.Player;
            m.BodyValue = 400;

            if (m.Poison != null)
                m.CurePoison(m);

            if (m is PlayerMobile pm)
                pm.Mortal = false;
        }
        public class BuildMageTarget : Target
        {
            public BuildMageTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    NormalizePlayer(player);

                    // first clear all the skills
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 0.0;

                    // build us a mage
                    skills[SkillName.Swords].Base = 100.0;
                    skills[SkillName.Anatomy].Base = 100.0;
                    skills[SkillName.Tactics].Base = 100.0;
                    skills[SkillName.Healing].Base = 100.0;
                    skills[SkillName.Magery].Base = 100.0;
                    skills[SkillName.MagicResist].Base = 100.0;
                    skills[SkillName.Meditation].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 35;
                    player.Mana = player.RawInt = 100;
                    player.Hits = player.RawStr = 90;

                    from.SendMessage("Mage built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        private static void BuildWarrior(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildWarriorTarget(); // Call our target
        }
        public class BuildWarriorTarget : Target
        {
            public BuildWarriorTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    NormalizePlayer(player);

                    // first clear all the skills
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 0.0;

                    // build us a warrior
                    skills[SkillName.Swords].Base = 100.0;
                    skills[SkillName.Anatomy].Base = 100.0;
                    skills[SkillName.Tactics].Base = 100.0;
                    skills[SkillName.Healing].Base = 100.0;
                    skills[SkillName.Magery].Base = 100.0;
                    skills[SkillName.MagicResist].Base = 100.0;
                    skills[SkillName.Meditation].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 100;
                    player.Mana = player.RawInt = 25;
                    player.Hits = player.RawStr = 100;

                    from.SendMessage("Warrior built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #region Make Murderer / Innocent
        private static void MakeMurderer(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to make/unmake a murderer...");
            e.Mobile.Target = new MakeMurdererTarget(); // Call our target
        }
        public class MakeMurdererTarget : Target
        {
            public MakeMurdererTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    if (player.ShortTermMurders + player.LongTermMurders >= 5)
                    {
                        player.ShortTermMurders = player.LongTermMurders = 0;
                        from.SendMessage("Murder counts cleared.");
                    }
                    else
                    {
                        player.ShortTermMurders = player.LongTermMurders = 5;
                        from.SendMessage("Murder counts set.");
                    }

                    from.SendMessage("Done.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Make Murderer / Innocent
        #endregion Build Player
        #region Prep World For Distribution
        private static void PrepWorldForDistribution(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel < AccessLevel.Owner)
            {
                e.Mobile.SendMessage("Not authorized.");
                Console.WriteLine("PrepWorldForDistribution: Not authorized.", ConsoleColor.Red);
                return;
            }

            int house_count = 0;
            int guild_count = 0;
            int township_count = 0;
            foreach (Item item in World.Items.Values)
                if (item is Multis.BaseHouse bh)
                {
                    bh.Owner = null;
                    //bh.IsStaffOwned = true;
                    if (bh.Sign != null)
                        bh.Sign.FreezeDecay = true;
                    else
                    {
                        // probably a tent, we don't care
                        //System.Diagnostics.Debug.Assert(false);
                    }
                    house_count++;

                }
                else if (item is Guildstone gs)
                {
                    //gs.IsStaffOwned = true;
                    guild_count++;
                }
                else if (item is Server.Factions.TownStone ts)
                {
                    //ts.IsStaffOwned = true;
                    township_count++;
                }

            Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(BlammoTick),
                new object[] { house_count, guild_count, township_count });
        }
        private static void BlammoTick(object state)
        {
            object[] aState = (object[])state;
            int house_count = (int)aState[0];
            int guild_count = (int)aState[1];
            int township_count = (int)aState[2];

            // delete accounts 
            int account_count = DeleteAccounts(null, include_owner: true);

            Console.WriteLine("{0} Accounts deleted.", account_count);
            Console.WriteLine("{0} houses preserved.", house_count);
            Console.WriteLine("{0} guilds preserved.", guild_count);
            Console.WriteLine("{0} townships preserved.", township_count);

            World.Save();
        }
        public static int DeleteAccounts(CommandEventArgs e, bool include_owner = false)
        {
            List<Account> clients = new List<Account>();
            foreach (Account check in Accounts.Table.Values)
            {
                if (check == null)
                    continue;

                if (check.AccessLevel == AccessLevel.Owner && include_owner == false)
                    continue;

                clients.Add(check);
            }

            // kick everyone
            foreach (NetState ns in NetState.Instances)
                if (ns != null && ns.Running)
                    if (ns.Account != null && (ns.Account as Accounting.Account).AccessLevel == AccessLevel.Owner)
                    {
                        if (include_owner)
                            ns.Dispose();
                        else
                            continue;
                    }
                    else
                        ns.Dispose();

            foreach (Account check in clients)
            {
                check.Delete();
            }

            return clients.Count;
        }
        #endregion Prep World For Distribution
        #region 20 Year Tribute to the Players!
        public class PlayerDesc
        {
            public string Layers;
            public string Name;
            public string Title;
            public int Hue;
            public int Body;
            public bool Female;
            public int Fame;
            public int Karma;
            public bool Murderer;
            public DateTime Created;    // (PlayerToCopy.Account as Accounting.Account).Created.ToString()
            public DateTime LastLogin;  // (PlayerToCopy.Account as Accounting.Account).LastLogin.ToString()
            // guild
            public string GuildAlignment;
            public string GuildName;
            public string GuildAbbr;

            public PlayerDesc(
                string layers,
                string name,
                string title,
                int hue,
                int body,
                bool female,
                int fame,
                int karma,
                bool murderer,
                DateTime created,
                DateTime last_login,
                string guild_alignment,
                string guild_name,
                string guild_abbr
                )
            {
                Layers = layers;
                Name = name;
                Title = title;
                Hue = hue;
                Body = body;
                Female = female;
                Fame = fame;
                Karma = karma;
                Murderer = murderer;
                Created = created;
                LastLogin = last_login;
                // guild
                GuildAlignment = guild_alignment;
                GuildName = guild_name;
                GuildAbbr = guild_abbr;
            }
        }
        public static void DupePlayers(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel < AccessLevel.Owner)
            {
                e.Mobile.SendMessage("Not authorized.");
                Console.WriteLine("DupePlayers: Not authorized.", ConsoleColor.Red);
                return;
            }
            int created = 0;
            List<PlayerDesc> list = new List<PlayerDesc>();
            foreach (Mobile m in World.Mobiles.Values)
                if (m is PlayerMobile PlayerToCopy)
                {   // copy
                    string layers = CopyLayers(PlayerToCopy);
                    string Name = PlayerToCopy.Name;
                    string Title = PlayerToCopy.Title;
                    int Hue = PlayerToCopy.Hue;
                    int Body = (PlayerToCopy.Female) ? 401 : 400;   // get the correct body
                    bool Female = PlayerToCopy.Female;              // get the correct sex
                    int Fame = PlayerToCopy.Fame;
                    int Karma = PlayerToCopy.Karma;
                    bool Murderer = PlayerToCopy.LongTermMurders >= 5;
                    DateTime Created = (PlayerToCopy.Account != null) ? (PlayerToCopy.Account as Accounting.Account).Created : DateTime.MinValue;
                    DateTime LastLogin = (PlayerToCopy.Account != null) ? (PlayerToCopy.Account as Accounting.Account).LastLogin : DateTime.MinValue;
                    string GuildAlignment = null;
                    string GuildName = null;
                    string GuildAbbr = null;
                    if (PlayerToCopy.Guild != null)
                    {
                        GuildAlignment = ((Server.Guilds.Guild)PlayerToCopy.Guild).IOBAlignment.ToString();
                        GuildName = PlayerToCopy.Guild.Name;
                        GuildAbbr = PlayerToCopy.Guild.Abbreviation;
                    }

                    if (string.IsNullOrEmpty(Title))
                        Title = "[Angel Island " + Created.Year.ToString() + ']';
                    else
                        ;

                    list.Add(new PlayerDesc(
                        layers,
                        Name,
                        Title,
                        Hue,
                        Body,
                        Female,
                        Fame,
                        Karma,
                        Murderer,
                        Created,
                        LastLogin,
                        GuildAlignment,
                        GuildName,
                        GuildAbbr
                        ));
                }

            string filename = "AI 3.0.bin";
            WritePlayers(filename, list);

            e.Mobile.SendMessage("{list.Count} players duped to {filename}.");

            // test restore
            List<PlayerDesc> list2 = ReadPlayers(filename);

            int restored = 0;
            foreach (PlayerDesc p in list2)
            {
                restored++;
                try
                {
                    Gypsy m_to = new Gypsy();
                    Utility.WipeLayers(m_to);
                    m_to.IsInvulnerable = true;
                    m_to.Name = p.Name;
                    m_to.Title = p.Title;
                    m_to.Hue = p.Hue;
                    m_to.Body = p.Body;
                    m_to.Female = p.Female;
                    m_to.Fame = p.Fame;
                    m_to.Karma = p.Karma;
                    // murderer
                    // created
                    // last login
                    // guild alignment
                    // guild name
                    // build abbr
                    RestoreLayers(m_to, p.Layers);
                    m_to.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
                }
                catch
                {
                    ;
                }
            }
        }
        private static void WritePlayers(string shard, List<PlayerDesc> list)
        {
            Console.WriteLine("Saving {0}...", shard);
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(shard, true);
                int version = 0;
                writer.Write(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.Write(list.Count);
                            foreach (var pd in list)
                            {   // string layers, string name, string title, int hue, int body, bool female
                                writer.Write(pd.Layers);
                                writer.Write(pd.Name);
                                writer.Write(pd.Title);
                                writer.Write(pd.Hue);
                                writer.Write(pd.Body);
                                writer.Write(pd.Female);
                                writer.Write(pd.Fame);
                                writer.Write(pd.Karma);
                                writer.Write(pd.Murderer);
                                writer.Write(pd.Created);
                                writer.Write(pd.LastLogin);
                                writer.Write(pd.GuildAlignment);
                                writer.Write(pd.GuildName);
                                writer.Write(pd.GuildAbbr);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing {0}", shard);
                Console.WriteLine(ex.ToString());
            }
        }
        public static List<PlayerDesc> ReadPlayers(string shard)
        {
            List<PlayerDesc> PlayerDescriptions = new List<PlayerDesc>();
            Console.WriteLine("Loading {0}...", shard);
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(shard, FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                PlayerDescriptions.Add(
                                    new PlayerDesc(
                                        reader.ReadString(),    // layers
                                        reader.ReadString(),    // name
                                        reader.ReadString(),    // title
                                        reader.ReadInt(),       // hue
                                        reader.ReadInt(),       // body
                                        reader.ReadBool(),      // female
                                        reader.ReadInt(),       // fame
                                        reader.ReadInt(),       // karma
                                        reader.ReadBool(),      // murderer
                                        reader.ReadDateTime(),  // created
                                        reader.ReadDateTime(),  // last login
                                        reader.ReadString(),    // guild alignment
                                        reader.ReadString(),    // guild name
                                        reader.ReadString()     // guild abbr
                                        )
                                    );
                            break;
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading {0}", ConsoleColor.Red, shard);
            }

            return PlayerDescriptions;
        }
        public static void RestoreLayers(Mobile dest, string layers)
        {
            string[] table = layers.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string sr in table)
            {
                string s = sr.Trim();
                string[] attrs = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (attrs.Length != 3)
                    continue;

                string item_type = attrs[0].Trim();
                int hue = int.Parse(attrs[1].Trim());
                Layer layer = (Layer)Enum.Parse(typeof(Layer), attrs[2].Trim());

                Item item = null;
                try
                {
                    if (ScriptCompiler.FindTypeByFullName(item_type) == typeof(RingmailGlovesOfMining))
                        item = new RingmailGlovesOfMining(100);
                    else if (ScriptCompiler.FindTypeByFullName(item_type) == typeof(StuddedGlovesOfMining))
                        item = new StuddedGlovesOfMining(100);
                    else
                        item = (Item)Activator.CreateInstance(ScriptCompiler.FindTypeByFullName(item_type));
                }
                catch
                {
                    ;
                }
                if (item != null)
                {
                    item.Hue = hue;
                    dest.AddItem(item);
                }
            }

        }
        public static string CopyLayers(Mobile src)
        {
            string result = string.Empty;
            try
            {
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        string item = items[i].GetType().FullName;
                        string hue = items[i].Hue.ToString();
                        string layer = items[i].Layer.ToString();
                        result += string.Format($"{item}, {hue}, {layer}; ");
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Send to Zen please: ");
                System.Console.WriteLine("Exception caught in Spawner.CopyLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

            return result;
        }
        #endregion 20 Year Tribute to the Players!
    }
}
