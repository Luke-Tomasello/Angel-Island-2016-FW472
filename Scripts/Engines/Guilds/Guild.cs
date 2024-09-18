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

/* Engines/Guilds/Guild.cs
 * CHANGELOG:
 *  8/2/08, Pix
 *		When Name changes and this guild is in a township, call UpdateRegionName() for the townshipstone.
 *	4/25/08, Adam
 *		Add new NewPlayerGuild flag to indicate this guild may be selected from available guilds to auto-add players.
 *		Change from Guild.Peaceful to guild.NewPlayerGuild when deciding auto adding
 *  01/14/08 Pix
 *      Reverted last change.  Added two methods:
 *      GetVotesFor - gets the votes for a member (for display to that member)
 *      UpdateFealtiesFor - gets called when a member declares his fealty - if anyone is
 *          declared to that member, then they get their fealty changed to the new vote.
 *  01/14/08, Pix
 *      Changed guildmaster voting calculation - now votes follow fealty chain.
 *  01/04/08, Pix
 *      Added delay to Guild War Ring toggling.
 *  12/21/07, Pix
 *      Added no counting flag for guilds.
 *	12/4/07, Adam
 *		Add support for peaceful guilds (no notoriety)
 *  02/26/07, Adam
 *      Generate a "GuildDisband.log" log entry for all disbanded guilds. When you recieve
 *      a missing guildstone exception from the guild restoration deed, check here first.
 *  02/04/07, Kit
 *      Made RemoveMember() update guild feality of members in guild to point to themselves if previously
 *      pointing to member being removed.
 *	12/03/06, Pix
 *		Changed hue for old-client guild/ally chat so it's readable in the journal.
 *	12/02/06, Pix
 *		Made guild/ally chats work with lower version clients (like it used to).
 *	11/19/06, Pix
 *		Changes for fixing guild and ally chat colors.
 *  10/14/06, Rhiannon
 *		Added ResignMember(), which displays a confirmation gump if the member is the guildmaster.
 *  9/01/06, Taran Kain
 *		Moved guildmaster fealty checks out of Serialize() and into Heartbeat.
 *	03/24/06, Pix
 *		Added IOBAlignment to guild.
 *	01/03/06, Pix
 *		Added AlliedMessage()
 *  12/14/05, Kit
 *		Added check to RemoveAlly()
 *	7/11/05: Pix
 *		Added ListenToGuild_OnCommand
 *	7/5/05: Pix
 *		Added new GuildMessage function which takes a string.
 */

using Server.Commands;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Guilds
{
    [Flags]
    public enum GuildFlags
    {   // make sure all valuse default to zero so that most guilds won't have to serialize these flags
        None = 0x00000000,
        Peaceful = 0x00000001,          // cannot war, ally, or attack guildmates
        AnnounceNewMembers = 0x00000002,            // announce members to the New Guild
        IsNoCountingGuild = 0x00000004,         // honor guild
        NewPlayerGuild = 0x00000008,            // new player guild, usually only one
    }

    public class Guild : BaseGuild
    {
        #region IOB Alignment functionality

        private static int m_JoinDelay = 10;
        private IOBAlignment m_IOBAlignment;
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set
            {
                if (m_IOBAlignment != value)
                {
                    m_IOBAlignment = value;
                    m_TypeLastChange = DateTime.UtcNow;

                    InvalidateMemberProperties();
                }
            }
        }

        private DateTime m_LastIOBChangeTime;
        public DateTime LastIOBChangeTime
        {
            get { return m_LastIOBChangeTime; }
        }

        private bool CanJoinIOB()
        {
            if (m_LastIOBChangeTime + TimeSpan.FromDays(m_JoinDelay) > DateTime.UtcNow)
                return false;

            return true;
        }

        public void IOBKick()
        {
            string message = string.Format("Your guild has been kicked from the {0} by the consensus of other {0}.  You cannot rejoin for {1} days.",
                Engines.IOBSystem.IOBSystem.GetIOBName(IOBAlignment), m_JoinDelay);
            GuildMessage(message);

            m_LastIOBChangeTime = DateTime.UtcNow;
            m_IOBAlignment = IOBAlignment.None;
        }

        #endregion //IOB Functionality

        private ArrayList m_Listeners;

        private bool m_bGuildWarRing = false;
        public bool GuildWarRing
        {
            get
            {
                return m_bGuildWarRing;
            }
            set
            {
                m_bGuildWarRing = value;
            }
        }
        private DateTime m_dtGuildWarRingChangeTime;
        public DateTime GuildWarRingChangeTime
        {
            get
            {
                return m_dtGuildWarRingChangeTime;
            }
            set
            {
                m_dtGuildWarRingChangeTime = value;
            }
        }

        public static void Configure()
        {
            EventSink.CreateGuild += new CreateGuildHandler(EventSink_CreateGuild);

            Server.CommandSystem.Register("ListenToGuild", AccessLevel.GameMaster, new CommandEventHandler(ListenToGuild_OnCommand));
        }

        public static void ListenToGuild_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ListenToGuild_OnTarget));
            e.Mobile.SendMessage("Target a guilded player.");
        }

        public static void ListenToGuild_OnTarget(Mobile from, object obj)
        {
            try
            {
                if (obj is Mobile)
                {
                    Guild g = ((Mobile)obj).Guild as Guild;

                    if (g == null)
                    {
                        from.SendMessage("They are not in a guild.");
                    }
                    else if (g.m_Listeners.Contains(from))
                    {
                        g.m_Listeners.Remove(from);
                        from.SendMessage("You are no longer listening to the guild [" + g.Abbreviation + "].");
                    }
                    else
                    {
                        g.m_Listeners.Add(from);
                        from.SendMessage("You are now listening to the guild [" + g.Abbreviation + "].");
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }


        public static BaseGuild EventSink_CreateGuild(CreateGuildEventArgs args)
        {
            return (BaseGuild)(new Guild(args.Id));
        }

        public static bool NewGuildSystem { get { return Core.RuleSets.SERules(); } }

        private Mobile m_Leader;

        private string m_Name;
        private string m_Abbreviation;

        private ArrayList m_Allies;
        private ArrayList m_Enemies;

        private ArrayList m_Members;

        private Item m_Guildstone;
        private Item m_Teleporter;

        private Item m_TownshipStone;

        private string m_Charter;
        private string m_Website;

        private DateTime m_LastFealty;

        private GuildType m_Type;
        private DateTime m_TypeLastChange;

        private ArrayList m_AllyDeclarations, m_AllyInvitations;

        private ArrayList m_WarDeclarations, m_WarInvitations;
        private ArrayList m_Candidates, m_Accepted;

        private GuildFlags m_Flags;

        public Guild(Mobile leader, string name, string abbreviation)
            : base()
        {
            m_Leader = leader;

            m_Members = new ArrayList();
            m_Allies = new ArrayList();
            m_Enemies = new ArrayList();
            m_WarDeclarations = new ArrayList();
            m_WarInvitations = new ArrayList();
            m_AllyDeclarations = new ArrayList();
            m_AllyInvitations = new ArrayList();
            m_Candidates = new ArrayList();
            m_Accepted = new ArrayList();

            m_LastFealty = DateTime.UtcNow;

            m_Name = name;
            m_Abbreviation = abbreviation;

            m_TypeLastChange = DateTime.MinValue;

            AddMember(m_Leader);
            m_Listeners = new ArrayList();
        }

        public Guild(int id)
            : base(id)//serialization ctor
        {
            m_Listeners = new ArrayList();
        }

        public void AddMember(Mobile m)
        {
            if (!m_Members.Contains(m))
            {
                if (m.Guild != null && m.Guild != this)
                    ((Guild)m.Guild).RemoveMember(m);

                m_Members.Add(m);
                m.Guild = this;
                m.GuildFealty = m_Leader;
            }
        }

        public void ResignMember(Mobile m)
        {
            if (m == m_Leader)
            {
                m.SendGump(new Gumps.ConfirmResignGump(m));
            }
            else
            {
                RemoveMember(m);
            }
        }

        public void RemoveMember(Mobile m)
        {
            if (m_Members.Contains(m))
            {
                m_Members.Remove(m);
                m.Guild = null;

                m.SendLocalizedMessage(1018028); // You have been dismissed from your guild.

                if (m == m_Leader)
                {
                    CalculateGuildmaster();

                    if (m_Leader == null)
                        Disband();
                }

                if (m_Members.Count == 0)
                    Disband();

                //update guild feality
                foreach (Mobile n in m_Members)
                {
                    if (n == null || n.Deleted || n.Guild != this)
                        continue;

                    if (n.GuildFealty == m)
                        n.GuildFealty = n; //set guild fealty to self
                }
            }
        }

        public void AddAlly(Guild g)
        {
            if (!m_Allies.Contains(g))
            {
                m_Allies.Add(g);

                g.AddAlly(this);
            }
        }

        public void RemoveAlly(Guild g)
        {
            if (m_Allies != null && m_Allies.Contains(g))
            {
                m_Allies.Remove(g);

                g.RemoveAlly(this);
            }
        }

        public void AddEnemy(Guild g)
        {
            if (!m_Enemies.Contains(g))
            {
                m_Enemies.Add(g);

                g.AddEnemy(this);
            }
        }

        public void RemoveEnemy(Guild g)
        {
            if (m_Enemies != null && m_Enemies.Contains(g))
            {
                m_Enemies.Remove(g);

                g.RemoveEnemy(this);
            }
        }

        //public void AlliedMessage( string message )
        //{
        //	//Send to us
        //	this.GuildMessage(message);
        //	//Send to all our allies
        //	foreach( Guild alliedguild in this.Allies )
        //	{
        //		if( alliedguild != null )
        //		{
        //			alliedguild.GuildMessage(message);
        //		}
        //	}
        //}

        public void GuildMessage(int num, string format, params object[] args)
        {
            GuildMessage(num, String.Format(format, args));
        }

        public void GuildMessage(int num, string append)
        {
            for (int i = 0; i < m_Members.Count; ++i)
                ((Mobile)m_Members[i]).SendLocalizedMessage(num, true, append);
        }

        public void GuildMessage(string message)
        {
            for (int i = 0; i < m_Members.Count; ++i)
                ((Mobile)m_Members[i]).SendMessage(68, message);

            if (m_Listeners.Count > 0)
            {
                foreach (Mobile m in m_Listeners)
                {
                    if (m != null)
                    {
                        m.SendMessage("[[" + this.Abbreviation + "]]" + message);
                    }
                }
            }
        }

        public void AlliedChat(string message, Server.Mobiles.PlayerMobile pm)
        {
            AlliedChat(pm, 0x3B2, message);
        }

        static ClientVersion GA_CHAT_MIN_VERSION = new ClientVersion("4.0.10a");

        public void AlliedChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (int i = 0; i < m_Members.Count; i++)
            {
                Mobile m = m_Members[i] as Mobile;

                if (m != null)
                {
                    NetState state = m.NetState;

                    if (state != null)
                    {
                        if (state.Version >= GA_CHAT_MIN_VERSION)
                        {
                            if (p == null)
                            {
                                p = Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Alliance, hue, 3, from.Language, from.Name, text));
                            }

                            state.Send(p);
                        }
                        else
                        {
                            m.SendMessage(0x587, "[Alliance][" + from.Name + "]: " + text);
                        }
                    }
                }
            }

            Packet.Release(p);

            if (from.Guild == this)
            {
                //Then send to all allied members
                foreach (Guild alliedguild in this.Allies)
                {
                    if (alliedguild != null)
                    {
                        alliedguild.AlliedChat(from, hue, text);
                        //alliedguild.GuildChat(message);
                    }
                }
            }
        }

        public void GuildChat(string message, Server.Mobiles.PlayerMobile pm)
        {
            GuildChat(pm, 0x3B2, message);
        }
        public void GuildChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (int i = 0; i < m_Members.Count; i++)
            {
                Mobile m = m_Members[i] as Mobile;

                if (m != null)
                {
                    NetState state = m.NetState;

                    if (state != null)
                    {
                        if (state.Version >= GA_CHAT_MIN_VERSION)
                        {
                            if (p == null)
                            {
                                p = Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Guild, hue, 3, from.Language, from.Name, text));
                            }

                            state.Send(p);
                        }
                        else
                        {
                            m.SendMessage(0x1D8, "[Guild][" + from.Name + "]: " + text);
                        }
                    }
                }
            }

            Packet.Release(p);
        }


        public void Disband()
        {
            // was it already disbanded?
            if (BaseGuild.List.Contains(this.Id))
            {
                LogHelper Logger = new LogHelper("GuildDisband.log", false);
                string abbreviation = "(null)";
                string name = "(null)";
                Serial sx = 0x0;
                if (Abbreviation != null) abbreviation = Abbreviation;
                if (Name != null) name = Name;
                if (m_Guildstone != null) sx = m_Guildstone.Serial;
                Logger.Log(LogType.Text, String.Format("The Guild \"{0}\" [{1}] ({2}) was disbanded.", name, abbreviation, sx.ToString()));
                Logger.Finish();
            }

            m_Leader = null;

            BaseGuild.List.Remove(this.Id);

            foreach (Mobile m in m_Members)
            {
                m.SendLocalizedMessage(502131); // Your guild has disbanded.
                m.Guild = null;
            }

            m_Members.Clear();

            for (int i = m_Allies.Count - 1; i >= 0; --i)
                if (i < m_Allies.Count)
                    RemoveAlly((Guild)m_Allies[i]);

            for (int i = m_Enemies.Count - 1; i >= 0; --i)
                if (i < m_Enemies.Count)
                    RemoveEnemy((Guild)m_Enemies[i]);

            if (m_Guildstone != null)
            {
                m_Guildstone.Delete();
                m_Guildstone = null;
            }

            if (m_TownshipStone != null)
            {
                m_TownshipStone.Delete();
                m_TownshipStone = null;
            }
        }

        public void UpdateFealtiesFor(Mobile originalVote, Mobile newVote)
        {
            try
            {
                if (originalVote == null)
                {
                    return;
                }

                for (int i = 0; i < m_Members.Count; i++)
                {
                    Mobile member = m_Members[i] as Mobile;
                    if (member != null)
                    {
                        if (member.GuildFealty == originalVote)
                        {
                            member.GuildFealty = newVote;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }
        }

        public int GetVotesFor(Mobile member)
        {
            int count = 0;

            try
            {
                if (member == null)
                {
                    return 0;
                }

                for (int i = 0; i < m_Members.Count; i++)
                {
                    Mobile m = m_Members[i] as Mobile;
                    if (m != null)
                    {
                        if (m == member)
                        {
                            if (m.GuildFealty == null || m.GuildFealty == member)
                            {
                                count++;
                            }
                        }
                        else
                        {
                            if (m.GuildFealty == member)
                            {
                                count++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }

            return count;
        }

        private Mobile CalculateMemberVote(Mobile member)
        {
            if (member == null || member.Deleted || member.Guild != this)
            {
                return null;
            }

            Mobile candidate = member.GuildFealty;

            if (candidate == null || candidate.Deleted || candidate.Guild != this)
            {
                if (m_Leader != null && !m_Leader.Deleted && m_Leader.Guild == this)
                {
                    candidate = m_Leader;
                }
                else
                {
                    candidate.GuildFealty = member;
                    candidate = member;
                }
            }

            return candidate;
        }

        public void CalculateGuildmaster()
        {
            Hashtable votes = new Hashtable();

            for (int i = 0; m_Members != null && i < m_Members.Count; ++i)
            {
                Mobile v = CalculateMemberVote(m_Members[i] as Mobile);

                if (v == null)
                {
                    continue;
                }

                if (votes[v] == null)
                {
                    votes[v] = (int)1;
                }
                else
                {
                    votes[v] = (int)(votes[v]) + 1;
                }
            }

            Mobile winner = null;
            int highVotes = 0;

            foreach (DictionaryEntry de in votes)
            {
                Mobile m = (Mobile)de.Key;
                int val = (int)de.Value;

                if (winner == null || val > highVotes)
                {
                    winner = m;
                    highVotes = val;
                }
            }

            if (winner == null) //make sure we have a winner!
            {
                if (m_Leader == null || m_Leader.Guild != this)
                {
                    if (m_Members.Count > 0)
                    {
                        winner = m_Members[0] as Mobile;
                    }
                }
                else
                {
                    winner = m_Leader;
                }
            }

            if (m_Leader != winner && winner != null)
            {
                GuildMessage(1018015, winner.Name); // Guild Message: Guildmaster changed to:
            }

            m_Leader = winner;
            m_LastFealty = DateTime.UtcNow;
        }

        public GuildFlags Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        public bool GetFlag(GuildFlags flag)
        {
            return ((m_Flags & flag) != 0);
        }

        public void SetFlag(GuildFlags flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NewPlayerGuild
        {
            get { return GetFlag(GuildFlags.NewPlayerGuild); }
            set { SetFlag(GuildFlags.NewPlayerGuild, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Peaceful
        {
            get { return GetFlag(GuildFlags.Peaceful); }
            set { SetFlag(GuildFlags.Peaceful, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AnnounceNewMembers
        {
            get { return GetFlag(GuildFlags.AnnounceNewMembers); }
            set { SetFlag(GuildFlags.AnnounceNewMembers, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsNoCountingGuild
        {
            get { return GetFlag(GuildFlags.IsNoCountingGuild); }
            set { SetFlag(GuildFlags.IsNoCountingGuild, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Guildstone
        {
            get
            {
                return m_Guildstone;
            }
            set
            {
                m_Guildstone = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item TownshipStone
        {
            get
            {
                return m_TownshipStone;
            }
            set
            {
                m_TownshipStone = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Teleporter
        {
            get
            {
                return m_Teleporter;
            }
            set
            {
                m_Teleporter = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;

                try
                {
                    if (m_TownshipStone != null)
                    {
                        if (m_TownshipStone is TownshipStone)
                        {
                            ((TownshipStone)m_TownshipStone).UpdateRegionName();
                        }
                    }
                }
                catch (Exception omgwtfwouldthisbe)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(omgwtfwouldthisbe));
                }

                if (m_Guildstone != null)
                    m_Guildstone.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Website
        {
            get
            {
                return m_Website;
            }
            set
            {
                m_Website = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Abbreviation
        {
            get
            {
                return m_Abbreviation;
            }
            set
            {
                m_Abbreviation = value;

                InvalidateMemberProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Charter
        {
            get
            {
                return m_Charter;
            }
            set
            {
                m_Charter = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override GuildType Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;
                    m_TypeLastChange = DateTime.UtcNow;

                    InvalidateMemberProperties();
                }
            }
        }

        public void InvalidateMemberProperties()
        {
            if (m_Members != null)
            {
                for (int i = 0; i < m_Members.Count; i++)
                    ((Mobile)m_Members[i]).InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Leader
        {
            get
            {
                if (m_Leader == null || m_Leader.Deleted || m_Leader.Guild != this)
                    CalculateGuildmaster();

                return m_Leader;
            }
            set
            {
                m_Leader = value;
            }
        }

        public override bool Disbanded
        {
            get
            {
                return (m_Leader == null || m_Leader.Deleted);
            }
        }

        public ArrayList Allies
        {
            get
            {
                return m_Allies;
            }
        }

        public ArrayList Enemies
        {
            get
            {
                return m_Enemies;
            }
        }

        public ArrayList AllyDeclarations
        {
            get
            {
                return m_AllyDeclarations;
            }
        }

        public ArrayList AllyInvitations
        {
            get
            {
                return m_AllyInvitations;
            }
        }

        public ArrayList WarDeclarations
        {
            get
            {
                return m_WarDeclarations;
            }
        }

        public ArrayList WarInvitations
        {
            get
            {
                return m_WarInvitations;
            }
        }

        public ArrayList Candidates
        {
            get
            {
                return m_Candidates;
            }
        }

        public ArrayList Accepted
        {
            get
            {
                return m_Accepted;
            }
        }

        public ArrayList Members
        {
            get
            {
                return m_Members;
            }
        }

        public bool IsMember(Mobile m)
        {
            return m_Members.Contains(m);
        }

        public bool IsAlly(Guild g)
        {
            return m_Allies.Contains(g);
        }

        public bool IsEnemy(Guild g)
        {
            if (m_Type != GuildType.Regular
                 && g.m_Type != GuildType.Regular
                 && m_Type != g.m_Type)
                return true;

            if (this != g && this.GuildWarRing && g.GuildWarRing)
            {
                return true;
            }

            return m_Enemies.Contains(g);
        }

        public bool IsWar(Guild g)
        {
            return m_Enemies.Contains(g);
        }

        public override void OnDelete(Mobile mob)
        {
            RemoveMember(mob);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastFealty
        {
            get
            {
                return m_LastFealty;
            }
            set
            {
                m_LastFealty = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TypeLastChange
        {
            get
            {
                return m_TypeLastChange;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            writer.Write((int)8);//version

            //version 8 addition
            writer.Write((int)m_Flags);

            //version 7 addition
            writer.Write(m_bGuildWarRing);

            //version 6 addition
            writer.Write(m_TownshipStone);

            //version 5 additions
            writer.Write((int)m_IOBAlignment);
            //end version 5 additions

            writer.WriteGuildList(m_AllyDeclarations, true);
            writer.WriteGuildList(m_AllyInvitations, true);

            writer.Write(m_TypeLastChange);

            writer.Write((int)m_Type);

            writer.Write(m_LastFealty);

            writer.Write(m_Leader);
            writer.Write(m_Name);
            writer.Write(m_Abbreviation);

            writer.WriteGuildList(m_Allies, true);
            writer.WriteGuildList(m_Enemies, true);
            writer.WriteGuildList(m_WarDeclarations, true);
            writer.WriteGuildList(m_WarInvitations, true);

            writer.WriteMobileList(m_Members, true);
            writer.WriteMobileList(m_Candidates, true);
            writer.WriteMobileList(m_Accepted, true);

            writer.Write(m_Guildstone);
            writer.Write(m_Teleporter);

            writer.Write(m_Charter);
            writer.Write(m_Website);
        }

        public override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 8:
                    {
                        m_Flags = (GuildFlags)reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        m_bGuildWarRing = reader.ReadBool();
                        goto case 6;
                    }
                case 6:
                    {
                        m_TownshipStone = reader.ReadItem();
                        goto case 5;
                    }
                case 5:
                    {
                        m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        goto case 4;
                    }
                case 4:
                    {
                        m_AllyDeclarations = reader.ReadGuildList();
                        m_AllyInvitations = reader.ReadGuildList();

                        goto case 3;
                    }
                case 3:
                    {
                        m_TypeLastChange = reader.ReadDateTime();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Type = (GuildType)reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_LastFealty = reader.ReadDateTime();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Leader = reader.ReadMobile();
                        m_Name = reader.ReadString();
                        m_Abbreviation = reader.ReadString();

                        m_Allies = reader.ReadGuildList();
                        m_Enemies = reader.ReadGuildList();
                        m_WarDeclarations = reader.ReadGuildList();
                        m_WarInvitations = reader.ReadGuildList();

                        m_Members = reader.ReadMobileList();
                        m_Candidates = reader.ReadMobileList();
                        m_Accepted = reader.ReadMobileList();

                        m_Guildstone = reader.ReadItem();
                        m_Teleporter = reader.ReadItem();

                        m_Charter = reader.ReadString();
                        m_Website = reader.ReadString();

                        break;
                    }
            }

            if (m_AllyDeclarations == null)
                m_AllyDeclarations = new ArrayList();

            if (m_AllyInvitations == null)
                m_AllyInvitations = new ArrayList();

            if (m_Guildstone == null || m_Members.Count == 0)
                Disband();
        }
    }
}
