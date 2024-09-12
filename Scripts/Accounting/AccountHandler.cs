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

/* Scripts/Accounting/AccountHandler.cs
 * ChangeLog:
 *  8/26/2024, Adam
 *      Console color Yellow for all client login/out etc. actions.
 * 8/10/2024, Adam: 
 *  Disable developer mode when releasing source to general public
 * 3/15/16, Adam
 *		Reverse changes of 2/8/08
 *		Turn IPException code back on. This is because the IPException logic is per IP whereas the MaxAccountsPerIP
 *			functionality is global.
 *	12/15/10, adam
 *		Add a new LoginServer variable to the core used in determining when we are a login server and when we are not.
 *		This saves us from trying to explicitly name all the servers we may be running (see AccountHandler.cs)
 *		History: I was getting complaints that auto creation on Test Center wasn't working. This was because we were incorrectly
 *		enumerating the non-primary (login) servers. This new varible should alleviate that problem.
 *  11/13/10, Adam
 *      Re allow AutoAccountCreation untill we enter ALPHA
 *	11/26/08, Adam
 *		During login, call IPLimiter.Notify() to tell other accounts on this IP why they cannot connect.
 *	11/25/08, Adam
 *		In Account logon and game logon call IPLimiter.IPStillTooHot() to see if the player is attempting a hot-swap of accounts.
 *		Account hot-swap prevention is only really effective if IPLimiter.MaxAddresses == 1
 *	2/18/08, Adam
 *		- We now allow 3 accounts per household - IPException logic no longer needed
 *		- Make MaxAccountsPerIP a console value (move to CoreAI)
 *  2/26/07, Adam
 *      Check for parameters > 1 passed to [password reset and handle with usage message
 *	11/06/06, Pix
 *		Removed AccountActivation stuff, because now it is handled without changing passwords
 *		purely with the profile gump.
 *	2/27/06, Pix
 *		Changes for IPLimiter.
 *	9/15/05, Adam
 *		convert MaxAccountsPerIP to a property so that it can be set
 *		in TestCenter.cs
 *	7/25/05, Pix
 *		Fixed to check ALL the IPs an account has logged in from, not just the first one.
 *	7/7/05, Pix
 *		Added Audit Email for Auto-account creation.
 * 6/28/05, Pix
 *	Reworked Password_OnCommand to use 'reset password' functionality gump.
 * 6/15/05, Pix
 *	Enabled AutoAccount, which now uses new IPException class.
 *  Removed functionality of [password command - now just directs player to [profile
 * 2/23/05, Pix
 *	Now if you delete a houseowner, the house will transfer to the first available 
 *	character on the account.  If you try to delete a houseowner and it is the last
 *	character on the account, the delete will fail.
 * 7/15/04, Pix
 *	Removed IP check from password changing with [password
 *	Logged IP when changing password.
 * 6/5/04, Pix
 * Merged in 1.0RC0 code.
 * 5/17/04, Pixie 
 *	Enabled password changing.
 *	Added use of new PasswordGump.
 *	User must enter current password.
 * 4/5/04 code changes by Pixie
 *	Changed the number of accounts per IP to 2 for ALPHA testing.
 * 4/24/04 code changes by adam
 *	Change AutoAccountCreation from true to false
 */


using Server.Accounting;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Server.Misc
{
    public class AccountHandler
    {
        private static bool AutoAccountCreation = true;
        private static bool RestrictDeletion = true;
        private static TimeSpan DeleteDelay = TimeSpan.FromDays(7.0);
        public static bool ProtectPasswords = true;
        private static AccessLevel m_LockdownLevel;

        public static AccessLevel LockdownLevel
        {
            get { return m_LockdownLevel; }
            set { m_LockdownLevel = value; }
        }

        public static int MaxAccountsPerIP
        {
            get { return CoreAI.MaxAccountsPerIP; }
            set { CoreAI.MaxAccountsPerIP = value; }
        }

        private static CityInfo[] StartingCities = new CityInfo[]
            {
                new CityInfo( "Yew",        "The Empath Abbey",         633,    858,    0  ),
                new CityInfo( "Minoc",      "The Barnacle",             2476,   413,    15 ),
                new CityInfo( "Britain",    "Sweet Dreams Inn",         1496,   1628,   10 ),
                new CityInfo( "Moonglow",   "The Scholars Inn",         4408,   1168,   0  ),
                new CityInfo( "Trinsic",    "The Traveler's Inn",       1845,   2745,   0  ),
                new CityInfo( "Magincia",   "The Great Horns Tavern",   3734,   2222,   20 ),
                new CityInfo( "Jhelom",     "The Mercenary Inn",        1374,   3826,   0  ),
                new CityInfo( "Skara Brae", "The Falconer's Inn",       618,    2234,   0  ),
                new CityInfo( "Vesper",     "The Ironwood Inn",         2771,   976,    0  ),
                new CityInfo( "Haven",      "Buckler's Hideaway",       3667,   2625,   0  )
            };

        private static bool PasswordCommandEnabled = true;

        public static void Initialize()
        {
            EventSink.DeleteRequest += new DeleteRequestEventHandler(EventSink_DeleteRequest);
            EventSink.AccountLogin += new AccountLoginEventHandler(EventSink_AccountLogin);
            EventSink.GameLogin += new GameLoginEventHandler(EventSink_GameLogin);

            if (PasswordCommandEnabled)
                Server.CommandSystem.Register("Password", AccessLevel.Player, new CommandEventHandler(Password_OnCommand));

            if (Core.AOS)
            {
                CityInfo haven = new CityInfo("Haven", "Uzeraan's Mansion", 3618, 2591, 0);
                StartingCities[StartingCities.Length - 1] = haven;
            }

            /* When we enter ALPHA we will lock the server down, but for now we want players to log in and help us identify
			 * those odd bits we need to fix.
			if (Core.UOSP) //Until Siege gets out of the closed phase, keep AutoAccountCreation false
			{
				AutoAccountCreation = false;
			}*/
        }

        [Usage("Password reset")]
        [Description("Brings up a gump for reset password.")]
        public static void Password_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            bool bSendUsage = false;

            if (e.Length == 0)
            {
                bSendUsage = true;
            }
            else if (e.Length == 1)
            {
                string keyword = e.GetString(0);

                if (keyword.Length > 0)
                {
                    if (keyword.ToLower() == "reset")
                    {
                        from.SendGump(new Server.Gumps.PasswordGump(from));
                    }
                    else
                    {
                        bSendUsage = true;
                    }
                }
                else
                {
                    bSendUsage = true;
                }
            }
            else
                bSendUsage = true;

            if (bSendUsage)
            {
                from.SendMessage("To change your password, use the {0}profile command.", Server.CommandSystem.CommandPrefix);
                from.SendMessage("To reset the password for a friend's account, use {0}password reset", Server.CommandSystem.CommandPrefix);
            }
        }

        private static void EventSink_DeleteRequest(DeleteRequestEventArgs e)
        {
            NetState state = e.State;
            int index = e.Index;

            Account acct = state.Account as Account;

            if (acct == null)
            {
                state.Dispose();
            }
            else if (index < 0 || index >= 5)
            {
                state.Send(new DeleteResult(DeleteResultType.BadRequest));
                state.Send(new CharacterListUpdate(acct));
            }
            else
            {
                Mobile m = acct[index];

                if (m == null)
                {
                    state.Send(new DeleteResult(DeleteResultType.CharNotExist));
                    state.Send(new CharacterListUpdate(acct));
                }
                else if (m.NetState != null)
                {
                    state.Send(new DeleteResult(DeleteResultType.CharBeingPlayed));
                    state.Send(new CharacterListUpdate(acct));
                }
                else if (RestrictDeletion && DateTime.UtcNow < (m.Created + DeleteDelay))
                {
                    state.Send(new DeleteResult(DeleteResultType.CharTooYoung));
                    state.Send(new CharacterListUpdate(acct));
                }
                else
                {
                    bool bDelete = true;

                    if (m is Server.Mobiles.PlayerMobile)
                    {
                        Server.Mobiles.PlayerMobile pm = (Server.Mobiles.PlayerMobile)m;
                        System.Collections.ArrayList houses = Multis.BaseHouse.GetHouses(pm);
                        if (houses.Count > 0)
                        {
                            if (acct.Count > 1)
                            {
                                Mobile newOwner = null;
                                //find a non-deleted, non-null character on the account
                                for (int i = 0; i < acct.Count; i++)
                                {
                                    if (index != i)
                                    {
                                        if (acct[i] != null)
                                        {
                                            if (!acct[i].Deleted)
                                            {
                                                newOwner = acct[i];
                                            }
                                        }
                                    }
                                }

                                if (newOwner == null) //sanity check, should never happen
                                {
                                    System.Console.WriteLine("Sanity check failed: newOwner == null!");
                                    bDelete = false;
                                    state.Send(new DeleteResult(DeleteResultType.BadRequest));
                                }
                                else
                                {
                                    for (int i = 0; i < houses.Count; ++i)
                                    {
                                        if (houses[i] is Server.Multis.BaseHouse)
                                        {
                                            Server.Multis.BaseHouse house = (Server.Multis.BaseHouse)houses[i];
                                            if (house != null)
                                            {
                                                if (house.Owner == m) //another sanity check - house.Owner should always be m at this point!
                                                {
                                                    //swap to new owner
                                                    house.Owner = newOwner;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //If account only has one character, then refuse to delete the houseowner
                                bDelete = false;
                                state.Send(new DeleteResult(DeleteResultType.BadRequest));
                            }
                        }
                    }

                    if (bDelete)
                    {
                        Utility.Monitor.WriteLine(string.Format("Client: {0}: Deleting character {1} (name:{3}) (0x{2:X})", state, index, m.Serial.Value, m.Name), ConsoleColor.Yellow);
                        m.Delete();
                    }

                    state.Send(new CharacterListUpdate(acct));
                }
            }
        }

        private static Account CreateAccount(NetState state, string un, string pw)
        {
            if (un.Length == 0 || pw.Length == 0)
                return null;

            bool isSafe = true;

            for (int i = 0; isSafe && i < un.Length; ++i)
                isSafe = (un[i] >= 0x20 && un[i] < 0x80);

            for (int i = 0; isSafe && i < pw.Length; ++i)
                isSafe = (pw[i] >= 0x20 && pw[i] < 0x80);

            if (!isSafe)
                return null;

            IPAddress ip = state.Address;

            string strIP = ip.ToString();
            int ipexception_numberallowed = 1;
            try
            {
                ipexception_numberallowed = Server.Accounting.IPException.GetIPException(strIP);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            int count = 0;

            foreach (Account a in Accounts.Table.Values)
            {
                if (a.LoginIPs.Length > 0)
                {
                    for (int i = 0; i < a.LoginIPs.Length; i++)
                    {
                        try
                        {
                            if (a.LoginIPs[i].Equals(ip))
                            {
                                ++count;
                            }
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }
                }
            }

            if (count >= MaxAccountsPerIP && count >= ipexception_numberallowed)
            {
                //Console.WriteLine("Login: {0}: Account '{1}' not created, ip already has {2} account{3}.", state, un, MaxAccountsPerIP, MaxAccountsPerIP == 1 ? "" : "s");
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Account '{1}' not created, ip already has {2} account{3}.", state, un, count, count == 1 ? "" : "s"), ConsoleColor.Yellow);
                return null;
            }

            //Audit email
            try
            {
                string regSubject = "Account Created Dynamically";
                string regBody = "Account created dynamically, auto-account.\n";
                regBody += "Info: \n";
                regBody += "\n";
                regBody += "Account: " + un + "\n";
                regBody += "IP: " + ip + "\n";
                regBody += "Password: " + pw + "\n";
                regBody += "\n";
                Emailer mail = new Emailer();
                mail.SendEmail(Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING"), regSubject, regBody, false);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            Utility.Monitor.WriteLine(string.Format("Login: {0}: Creating new account '{1}'", state, un), ConsoleColor.Yellow);
            return Accounts.AddAccount(un, pw);
        }

        public static void EventSink_AccountLogin(AccountLoginEventArgs e)
        {
            //Disallow direct logins to other servers if we are not a developer
            // 8/10/2024, Adam: Disable developer mode when releasing source to general public
            //if (!Core.LoginServer && !Core.Developer)
            //{
            //    Console.WriteLine("Login: You cannot login directly to this server without the -developer commandline switch");
            //    e.Accepted = false;
            //    e.RejectReason = ALRReason.Blocked;
            //    return;
            //}

            if (!IPLimiter.SocketBlock && !IPLimiter.Verify(e.State.Address))
            {
                e.Accepted = false;
                e.RejectReason = ALRReason.InUse;

                Console.WriteLine("Login: {0}: Past IP limit threshold", e.State);

                using (StreamWriter op = new StreamWriter("ipLimits.log", true))
                    op.WriteLine("{0}\tPast IP limit threshold\t{1}", e.State, DateTime.UtcNow);

                // tell other accounts on this IP what's going on
                IPLimiter.Notify(e.State.Address);
                return;
            }

            // before a new logon to this shard, make sure to read in all queued password changes from all shards
            Account.ReadAllPasswords();

            string un = e.Username;
            string pw = e.Password;

            e.Accepted = false;
            Account acct = Accounts.GetAccount(un);

            if (acct == null)
            {   // there are NO STAFF accounts with this username and either you have NO accounts, or you have a matching account name and password for another shard.
                if (AutoAccountCreation && !Account.CheckAllStaff(null, un, false) && (!Account.CheckAllAccounts(un) || (Account.CheckAllAccounts(un) && Account.CheckAllPasswords(un, pw))))
                {
                    e.State.Account = acct = CreateAccount(e.State, un, pw);
                    e.Accepted = acct == null ? false : acct.CheckAccess(e.State);

                    if (!e.Accepted)
                        e.RejectReason = ALRReason.BadComm;
                }
                else
                {
                    if (Account.CheckAllStaff(null, un, false))
                    {
                        Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password for staff account '{1}'", e.State, un), ConsoleColor.Yellow);
                        e.RejectReason = ALRReason.BadPass;
                    }
                    else if (Account.CheckAllAccounts(un))
                    {
                        Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password for '{1}'", e.State, un), ConsoleColor.Yellow);
                        e.RejectReason = ALRReason.BadPass;
                    }
                    else
                    {
                        Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid username '{1}'", e.State, un), ConsoleColor.Yellow);
                        e.RejectReason = ALRReason.Invalid;
                    }
                }
            }
            else if (IPLimiter.IPStillHot(acct, e.State.Address))
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Access denied for '{1}'. IP too hot", e.State, un), ConsoleColor.Yellow);
                e.RejectReason = ALRReason.InUse;
            }
            else if (!acct.HasAccess(e.State))
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Access denied for '{1}'", e.State, un), ConsoleColor.Yellow);
                e.RejectReason = (m_LockdownLevel > AccessLevel.Player ? ALRReason.BadComm : ALRReason.BadPass);
            }
            // You succeed login when your password matches some shard and no shards have a user with the same name with greater access 
            else if (!(Account.CheckAllPasswords(un, pw) && !Account.CheckAllStaff(acct, un, true)))
            {
                if (Account.CheckAllStaff(acct, un, true))
                    Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password or access level for staff account '{1}'", e.State, un), ConsoleColor.Yellow);
                else
                    Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password for '{1}'", e.State, un), ConsoleColor.Yellow);
                e.RejectReason = ALRReason.BadPass;
            }
            else if (acct.CheckBanned())
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Banned account '{1}'", e.State, un), ConsoleColor.Yellow);
                e.RejectReason = ALRReason.Blocked;
            }
            else
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Valid credentials for '{1}'", e.State, un), ConsoleColor.Yellow);
                e.State.Account = acct;
                e.Accepted = true;

                acct.LogAccess(e.State);
                acct.LastLogin = DateTime.UtcNow;
            }

            if (!e.Accepted)
                AccountAttackLimiter.RegisterInvalidAccess(e.State);
        }

        public static int CountOfLastIPsEqualing(IPAddress ip, out AccessLevel accessLevel, out List<Account> accts)
        {
            int count = 0;
            accessLevel = AccessLevel.Player; //assume player first
            accts = new List<Account>(); //initialize
            try
            {
                foreach (Account a in Accounts.Table.Values)
                {
                    if (a.LastGAMELogin != null && a.LastGAMELogin.Equals(ip))
                    {
                        accts.Add(a);
                        count++;

                        AccessLevel al = a.GetAccessLevel();
                        if (al > accessLevel)
                        {
                            accessLevel = al;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return count;
        }

        public static void EventSink_GameLogin(GameLoginEventArgs e)
        {
            if (!IPLimiter.SocketBlock && !IPLimiter.Verify(e.State.Address))
            {
                e.Accepted = false;

                Console.WriteLine("Login: {0}: Past IP limit threshold", e.State);

                using (StreamWriter op = new StreamWriter("ipLimits.log", true))
                    op.WriteLine("{0}\tPast IP limit threshold\t{1}", e.State, DateTime.UtcNow);

                // tell other accounts on this IP what's going on
                IPLimiter.Notify(e.State.Address);
                return;
            }

            // before a new logon to this shard, make sure to read in all queued password changes from all shards
            Account.ReadAllPasswords();

            string un = e.Username;
            string pw = e.Password;

            Account acct = Accounts.GetAccount(un);


            //PIX: Note - this won't work where the login server lives (in our case, AI), but it will work for
            // servers not the login server.  That's because the IPs get logged on those after this check.
            // On login servers, we'd have to move this code before the account access was logged.
            // Note also that we only check the last IP logged in from.  So if Person A has account a1 and
            // Person B has account b1, they can both play from IP address I1.  However, both won't be able
            // to access any other accounts.  If Person B then logs in from IP I2, another account can then 
            // be accessed from IP I1, but then account b1 won't be able to log in from IP I1.
            // The idea of this is that we only ever let two accounts at a time log in from a single IP
            // address.  If one (or more) accounts move to a different IP, then another account can login 
            // from the old IP.  This effectively stops any more than 2 accounts having a single IP as their
            // last accessed.
            #region IPBinderEnabled
            if (!Core.RuleSets.LoginServerRules() && CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IPBinderEnabled))    //Adam: disable via IPBinderEnabled for now until we can better understand the logon problems people are having
            {
                AccessLevel aal = AccessLevel.Player;
                List<Account> acctList = new List<Account>();
                int countIP = CountOfLastIPsEqualing(e.State.Address, out aal, out acctList);
                bool boot = false;

                if (acct != null
                    && acctList.Contains(acct)
                    )
                {
                    //if we've got a valid account and the account's last ip is the current ip, allow login
                }
                else
                {
                    //Here then we're in the case where it's either a new account
                    // or the accout has a different last-game-login-ip, so make sure that
                    // that IP doesn't already have 2+ accounts
                    if (countIP >= 2 && aal <= AccessLevel.Player)
                    {
                        boot = true; //allow only 2 for Players
                    }
                    else if (countIP >= 3 && aal < AccessLevel.Administrator)
                    {
                        boot = true; //allow 3 for staff
                    }
                    else if (countIP >= 3)
                    {
                        //allow unlimited for Admins or greater
                        Console.WriteLine("Allowing unlimited IP access for {0} from {1}", e.Username, e.State);
                    }
                }

                if (boot)
                {
                    e.Accepted = false;

                    string strAccts = "";
                    foreach (Account a1 in acctList)
                    {
                        strAccts += ("[" + a1.Username + "]");
                    }

                    Console.WriteLine("Login: {0}({1}): Past OFFLINE IP limit threshold.  Accts: {2} ", e.Username, e.State, strAccts);
                    using (StreamWriter sw1 = new StreamWriter("offlineIPLimits.log", true))
                    {
                        sw1.WriteLine("{0}:{1}\tPast OFFLINE IP limit threshold\t{2}\tAccounts:{3}", e.Username, e.State, DateTime.UtcNow, strAccts);

                        for (int i = 0; i < NetState.Instances.Count; ++i)
                        {
                            NetState compState = NetState.Instances[i];
                            if (e.State.Address.Equals(compState.Address) && compState.Mobile != null)
                                compState.Mobile.SendMessage(0x35, "You have exceeded the number of accounts authorized to connect from this address.");
                        }
                    }

                    return;
                }
            }
            #endregion

            //if we have a valid account on a non-primary shard, create it (assuming AutoAccountCreation is on)
            if (acct == null && !Core.RuleSets.LoginServerRules())
            {
                // there are NO STAFF accounts with this username and either you have NO accounts, or you have a matching account name and password for another shard.
                if (AutoAccountCreation && !Account.CheckAllStaff(null, un, false) && (!Account.CheckAllAccounts(un) || (Account.CheckAllAccounts(un) && Account.CheckAllPasswords(un, pw))))
                {
                    acct = CreateAccount(e.State, un, pw);
                }
                else
                {
                    if (Account.CheckAllStaff(null, un, false))
                        Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password for staff account '{1}'", e.State, un), ConsoleColor.Yellow);
                    else if (Account.CheckAllAccounts(un))
                        Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password for '{1}'", e.State, un), ConsoleColor.Yellow);
                    else
                        Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid username '{1}'", e.State, un), ConsoleColor.Yellow);
                }
            }

            if (acct == null)
            {
                e.Accepted = false;
            }
            else if (IPLimiter.IPStillHot(acct, e.State.Address))
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Access denied for '{1}'. IP too hot", e.State, un), ConsoleColor.Yellow);
                e.Accepted = false;
            }
            else if (!acct.HasAccess(e.State))
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Access denied for '{1}'", e.State, un), ConsoleColor.Yellow);
                e.Accepted = false;
            }
            // You succeed login when your password matches some shard and no shards have a user with the same name with greater access 
            else if (!(Account.CheckAllPasswords(un, pw) && !Account.CheckAllStaff(acct, un, true)))
            {
                if (Account.CheckAllStaff(acct, un, true))
                    Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password or access level for staff account '{1}'", e.State, un), ConsoleColor.Yellow);
                else
                    Utility.Monitor.WriteLine(string.Format("Login: {0}: Invalid password for '{1}'", e.State, un), ConsoleColor.Yellow);
                e.Accepted = false;
            }
            else if (acct.Banned)
            {
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Banned account '{1}'", e.State, un), ConsoleColor.Yellow);
                e.Accepted = false;
            }
            else
            {
                acct.LogAccess(e.State);
                acct.LogGAMELogin(e.State);
                Utility.Monitor.WriteLine(string.Format("Login: {0}: Account '{1}' at character list", e.State, un), ConsoleColor.Yellow);
                e.State.Account = acct;
                e.Accepted = true;
                e.CityInfo = StartingCities;
            }

            if (!e.Accepted)
                AccountAttackLimiter.RegisterInvalidAccess(e.State);
        }
    }
}
