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

/* Scripts/Misc/WelcomeTimer.cs
 *  ChangeLog	
 *	3/3/11, Adam
 *		Parameterize the welcome message based upon shard conf parameters (Event, Test Center, etc.)
 *	2/9/11, Adam
 *		Promote Mortalis to a real shard and give it a better welcome message
 *	02/3/11, Adam
 *		Add better Siege Wipe warning
 *		Add a scary message for our Island Siege secret shard AKA UO Mortalis
 *	02/23/10, Adam
 *		remove faction messages, added "GMN service since" notice
 *	06/29/09, plasma
 *		Added factions message
 *	4/25/08, Adam
 *		Change from Guild.Peaceful to guild.NewPlayerGuild when deciding auto adding
 *	1/20/08, Adam
 *		more sanity checking
 *	1/17/08, Adam
 *		Add sanity checking in OnTick()
 *	1/4/08, Adam
 *		- unconditional add to New guild (if their IP is unknown to us)
 *		- change New member titles from Day Month to Month Day
 *  12/12/07, Adam
 *      Cleanup code in Accounting.Accounts.IPLookup() test
 *  12/9/07, Adam
 *      Added NewPlayerGuild feature bit.
 *  12/6/07, Adam
 *      - Call new gump to auto add players to the New Guild (a peaceful guild)
 *		- Add call to Accounting.Accounts.IPLookup() to see if this a known IP address
 *      - log all players added to the NEW guild to PlayerAddedToNEWGuild.log
 *      - add exception handling
 *	8/26/07 - Pix
 *		Added WelcomeGump if NewPlayerStartingArea feature bit is enabled.
 *  09/14/05 Taran Kain
 *		Add TC message back in with a check for functionality, change it around to make it accurate with what we put in the bank.
 *	6/19/04, Adam
 *		1. Comment out TestCenter message
 *		2. add nowmal welcome message
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Guilds;
using Server.Items;
using System;

namespace Server.Misc
{
    /// <summary>
    /// This timer spouts some welcome messages to a user at a set interval. It is used on character creation.
    /// </summary>
    public class WelcomeTimer : Timer
    {
        private Mobile m_Mobile;
        private int m_State, m_Count;
        private static Guildstone m_Stone;
        private static string[] m_Messages;

        public static void Initialize()
        {
            if (TestCenter.Enabled)
            {
                m_Messages = new string[]
                {	// Test Center
					String.Format("Welcome to {0} Test Center{1}",
                        Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? "Angel Island" : Core.RuleSets.SiegeRules() ? "Siege Perilous" : Core.RuleSets.MortalisRules() ? "Mortalis" : "Unknown Configuration",
                        Core.RuleSets.EventShardRules() ? " Event Shard." : "."),
                    "GMN core version 3.0, online since March 2004.",
                    "You are able to customize your character's stats and skills at anytime to anything you wish.  To see the commands to do this just say 'help'.",
                    "You will find bank checks worth nearly 1.5 million gold in your bank!",
                    "A spellbook and a bag of reagents has been placed into your bank box.",
                    "Various tools have been placed into your bank.",
                    "Various raw materials like ingots, logs, feathers, hides, bottles, etc, have been placed into your bank.",
                    "Nine unmarked recall runes have been placed into your bank box.",
                    "A keg of each potion has been placed into your bank box.",
                    "Two of each level of treasure map have been placed in your bank box.",
                    "You will find 60,000 gold pieces deposited into your bank box.  Spend it as you see fit and enjoy yourself!",
                };
            }
            else
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
                    m_Messages = new string[]
                    {	// Angel Island
						String.Format("Welcome to Angel Island{0}", Core.RuleSets.EventShardRules() ? " Event Shard." : "."),
                        "GMN core version 3.0, online since March 2004.",
                        "Please see www.game-master.net for game-play information."
                    };
                else if (Core.RuleSets.SiegeRules())
                    m_Messages = new string[]
                    {	// Siege Perilous
						String.Format("Welcome to Siege Perilous{0}", Core.RuleSets.EventShardRules() ? " Event Shard." : "."),
                        "Version: PRE ALPHA",
                        "This Server will be wiped.",
                        "GMN core version 3.0, online since March 2004.",
                        "Please see www.game-master.net for game-play information."
                    };
                else if (Core.RuleSets.MortalisRules())
                    m_Messages = new string[]
                    {	// Mortalis
						String.Format("Welcome to Mortalis{0}", Core.RuleSets.EventShardRules() ? " Event Shard." : "."),
                        "You are mortal here, and if you die you will need to reenter this world anew.",
                        "There is no resurrection, there are no second chances.",
                        "You have been warned.",
                        "Life is only as sweet as death is painful."
                    };
                else if (Core.RuleSets.RenaissanceRules())
                    m_Messages = new string[]
                    {	// AI Resurrection
						String.Format("Welcome to AI Resurrection{0}", Core.RuleSets.EventShardRules() ? " Event Shard." : "."),
                        "GMN core version 3.0, online since March 2004.",
                        "This shard is a resurrection of the AI 1.0 world dated Feburary 23, 2009.",
                        "Please see www.game-master.net for game-play information."
                    };
            }
        }

        public WelcomeTimer(Mobile m)
            : this(m, m_Messages.Length)
        {
        }

        public WelcomeTimer(Mobile m, int count)
            : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
        {
            m_Mobile = m;
            m_Count = count;
            m_State = 0;
        }

        protected override void OnTick()
        {
            try
            {
                // sanity
                if (m_Mobile == null || m_Mobile.Deleted == true || m_Mobile.NetState == null || Running == false)
                {
                    Stop();
                    return;
                }

                // Let new players join the NEW guild
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerGuild) && m_State == 0)
                    NewPlayerGuild(m_Mobile);

                // print welcome messages
                if (m_State < m_Count)
                    m_Mobile.SendMessage(0x35, m_Messages[m_State]);

                // stop the timer
                if (m_State == m_Count)
                    Stop();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {   // make sure we keep marching forward no matter what
                m_State++;
            }
        }

        private Guildstone FindGuild(string abv)
        {
            // cache the stone
            if (m_Stone != null && m_Stone is Guildstone && m_Stone.Deleted == false)
                return m_Stone;
            else
                m_Stone = null;

            Guild guild = null;
            string name = abv.ToLower();
            foreach (Item n in World.Items.Values)
            {
                if (n is Guildstone && n != null)
                {
                    if (((Guildstone)n).Guild != null)
                        guild = ((Guildstone)n).Guild;

                    if (guild.Abbreviation != null && guild.NewPlayerGuild == true && guild.Abbreviation.ToLower() == name)
                    {   // cache the guildstone
                        m_Stone = (Guildstone)guild.Guildstone;
                        return m_Stone;
                    }
                }
            }

            return null;
        }

        private void NewPlayerGuild(Mobile from)
        {   // sanity
            if (from == null || from.Deleted == true || from.NetState == null)
                return;

            Accounting.Account a = from.Account as Accounting.Account;
            if (a != null && a.AccessLevel == AccessLevel.Player)
            {
                // 30 days young
                TimeSpan delta = DateTime.UtcNow - a.Created;
                if (delta.TotalDays <= 30 && Accounting.Accounts.IPLookup(from.NetState.Address) == false)
                {   // unconditional add
                    // from.SendGump(new JoinNEWGuildGump(from));
                    Guildstone stone = FindGuild("new");
                    if (stone != null && stone.Guild != null)
                    {   // log it
                        LogHelper logger = new LogHelper("PlayerAddedToNEWGuild.log", false, true);
                        logger.Log(LogType.Mobile, from);
                        logger.Finish();
                        // do it
                        stone.Guild.AddMember(from);
                        from.DisplayGuildTitle = true;
                        DateTime tx = DateTime.UtcNow.AddDays(14);
                        string title = String.Format("{0}/{1}", tx.Month, tx.Day);
                        from.GuildTitle = title;
                        from.GuildFealty = stone.Guild.Leader != null ? stone.Guild.Leader : from;
                        stone.Guild.GuildMessage(String.Format("{0} has just joined {1}.", from.Name, stone.Guild.Abbreviation == null ? "your guild" : stone.Guild.Abbreviation));
                    }
                    else
                        from.SendMessage("We're sorry, but the new player guild is temporarily unavailable.");
                }

            }
        }
    }
}
