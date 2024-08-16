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

/* Scripts/Accounting/IPLimiter.cs
 * CHANGELOG:
 *	4/16/09, Adam
 *		Add an assert() to make sure neither account is null in IPStillHot()
 *	11/26/08, Adam
 *		- don't count staff accounts
 *			note: because this check comes before the acct for ourAddress is known, staff can only exceed these limits if they login 
 * 			Player Accounts AFTER Staff Accounts.
 * 		- tell the other accounts on this IP that they are not authorized to connect another account to this IP
 *	11/25/08, Adam
 *		- Make MaxAddresses a core command console value
 *		- Add hot-swap protection (meaningful only when MaxAddresses == 1)
 *	2/27/06, Pix
 *		Changes for Verify().
 */


using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;

namespace Server.Misc
{
    public class IPLimiter
    {
        public static void Initialize()
        {   // start a timer (hashtable with time value) when a player disconnects
            EventSink.Disconnected += new DisconnectedEventHandler(EventSink_Disconnected);
        }

        public class WatchDog
        {
            private DateTime m_DateTime;
            public DateTime Limit { get { return m_DateTime; } }
            private Server.Accounting.Account m_Account;
            public Server.Accounting.Account Account { get { return m_Account; } }
            public WatchDog(Mobile m)
            {
                m_DateTime = DateTime.Now + m.GetLogoutDelay();
                m_Account = m.Account as Server.Accounting.Account;
            }
        }

        private static Dictionary<IPAddress, WatchDog> m_IPMRU = new Dictionary<IPAddress, WatchDog>();
        public Dictionary<IPAddress, WatchDog> IPMRU { get { return m_IPMRU; } }

        /*
		 * When a player disconnects, put their IP and the time at which they will time-out into a hashtable. 
		 *	if someone tries to login, and we are at our MAX allowable IPs, then force them to wait until the other account/character timesout.
		 *	NOTE: This only prevents day-to-day hotswaps if MaxAddresses == 1
		 */
        private static void EventSink_Disconnected(DisconnectedEventArgs e)
        {
            try
            {
                if (e.Mobile != null && e.Mobile.Account != null)
                {
                    if ((e.Mobile.Account as Server.Accounting.Account).LoginIPs != null)
                    {
                        if ((e.Mobile.Account as Server.Accounting.Account).LoginIPs[0] != null)
                        {   // delete an old record
                            if (m_IPMRU.ContainsKey((e.Mobile.Account as Server.Accounting.Account).LoginIPs[0]))
                                m_IPMRU.Remove((e.Mobile.Account as Server.Accounting.Account).LoginIPs[0]);
                            // create a new record
                            m_IPMRU[(e.Mobile.Account as Server.Accounting.Account).LoginIPs[0]] = new WatchDog(e.Mobile);
                        }
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static bool Enabled = true;
        public static bool SocketBlock = false; // true to block at connection, false to block at login request
        public static int MaxAddresses { get { return CoreAI.MaxAddresses; } set { CoreAI.MaxAddresses = value; } }

        // tell the other accounts that they are not authorized to connect another account
        public static void Notify(IPAddress ourAddress)
        {
            if (Enabled)
                for (int i = 0; i < NetState.Instances.Count; ++i)
                {
                    NetState compState = NetState.Instances[i];
                    if (ourAddress.Equals(compState.Address) && compState.Mobile != null)
                        compState.Mobile.SendMessage(0x35, String.Format("You are not authorized to connect more than {0} account{1} to this IP address.", MaxAddresses, MaxAddresses == 1 ? "" : "s"));
                }
        }

        // hot-swap prevention
        public static bool IPStillHot(Server.Accounting.Account acct, IPAddress ourAddress)
        {
            if (!Enabled)
                return false;

            int count = 0;
            for (int i = 0; i < NetState.Instances.Count; ++i)
            {
                NetState compState = NetState.Instances[i];
                if (ourAddress.Equals(compState.Address))
                    ++count;
            }

            // force this login to wait until the other client timesout (the same client is not restricted.)
            if (MaxAddresses == count && m_IPMRU.ContainsKey(ourAddress) && m_IPMRU[ourAddress].Account != acct)
            {
                // if one of these accounts is null, then this test delived a false positive whereby prohibiting the player from logging in
                Diagnostics.Assert(m_IPMRU[ourAddress].Account != null && acct != null, "Account null in IPStillHot()");

                if (DateTime.Now < m_IPMRU[ourAddress].Limit)
                    return true;
            }

            return false;
        }

        public static bool Verify(IPAddress ourAddress)
        {
            if (!Enabled)
                return true;

            // see if there is another logged in account with this IP address
            List<NetState> netStates = NetState.Instances;
            int count = 0;
            for (int i = 0; i < netStates.Count; ++i)
            {
                NetState compState = netStates[i];

                // don't count staff accounts
                //	note: because this check comes before the acct for ourAddress is known, staff can only exceed these limits if they login 
                //	Player Accounts AFTER Staff Accounts.
                if (compState.Mobile != null && compState.Mobile.Account != null)
                {
                    Server.Accounting.Account acct = compState.Mobile.Account as Server.Accounting.Account;
                    if (acct.GetAccessLevel() > AccessLevel.Player)
                        continue;
                }

                // add up matching accounts (connections to this IP address)
                if (ourAddress.Equals(compState.Address))
                {
                    ++count;

                    if (count > MaxAddresses)
                        return false;
                }
            }

            return true;
        }
    }
}
