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

/* Scripts/Accounting/Accounts.cs
 * CHANGELOG
 *  8/10/2024, Adam: Rework initial account creation 'access'.
 *      Now handled in AccountPrompt and CharacterCreation.
 *	3/4/10, Adam
 *		Fix GetDateTime to handle the frequent case of instring=="" instead of letting exception handling handle it. 
 *	3/4/10, Adam
 *		Fix GetInt32 to handle the frequent case of instring=="" instead of letting exception handling handle it.
 *  12/12/07, Adam
 *      we delay adding the IPAddress to the IPDatabase give us time to check for it in the WelcomeTimer
 *	12/6/07, Adam
 *		Add new IP Database functionality
 *			- hashtable to hold the addresses
 *			- add function to mask off last octet and add it to the database
 *			- lookup function to mask and check to see if that IP address is known
 *  6/11/05, Pix
 *		Added GetBool() for parsing ease.
 *	1/28/05, Pix
 *		Added try/catch around accounts.xml save
 *		Now will retry 3 times to save...
 */

using Server.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Accounting
{
    public class Accounts
    {
        private static List<Hashtable> m_Accounts = new List<Hashtable>();
        private static List<List<string>> m_IPDatabase = new List<List<string>>();
        public static List<List<string>> IPDatabase { get { return m_IPDatabase; } }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }

        public static void Initialize()
        {
            EventSink.Login += new LoginEventHandler(OnLogin);
            EventSink.Connected += new ConnectedEventHandler(OnConnected);
        }

        private static void OnLogin(LoginEventArgs e)
        {   // we delay adding the IPAddress to give us time to check for it in the WelcomeTimer
            if (e.Mobile != null && e.Mobile.NetState != null)
                new IPLogTimer(e.Mobile.NetState.Address).Start();
        }

        private static void OnConnected(ConnectedEventArgs e)
        {   // we delay adding the IPAddress to give us time to check for it in the WelcomeTimer
            if (e.Mobile != null && e.Mobile.NetState != null)
                new IPLogTimer(e.Mobile.NetState.Address).Start();
        }

        public static string GetIPMask(System.Net.IPAddress ax)
        {
            if (ax == null)
                return null;

            string left = null;
            Byte[] bytes = ax.GetAddressBytes();
            if (bytes.Length != 4)
            {
                Console.WriteLine("IPAddress error: {0}", ax.ToString());
                return null;
            }

            // get first 3 octets only to mask out the whole range covered by the 4th octet
            left += bytes[0].ToString() + ".";
            left += bytes[1].ToString() + ".";
            left += bytes[2].ToString();

            return left;
        }

        static Accounts()
        {
        }

        public static Hashtable Table
        {
            get
            {
                if (m_Accounts.Count == 0)
                    m_Accounts.Add(new Hashtable());
                return m_Accounts[0];
            }
        }

        public static Hashtable[] Database
        {
            get
            {
                return m_Accounts.ToArray();
            }
        }

        public static Account GetAccount(string username)
        {
            return m_Accounts[0][username] as Account;
        }

        public static Account AddAccount(string user, string pass)
        {
            Account a = new Account(user, pass);
            // 8/10/2024, Adam: Rework initial account creation 'access'.
            //  Now handled in AccountPrompt and CharacterCreation.
            //if (m_Accounts[0].Count == 0)
            //    a.AccessLevel = AccessLevel.Administrator;

            m_Accounts[0][a.Username] = a;

            return a;
        }

        public static int GetInt32(string intString, int defaultValue)
        {
            // adam: using exceptions to handle errors is bad-bad.
            //	the intString=="" case happens a lot!
            if (intString == null || intString.Length == 0)
                return defaultValue;

            try
            {
                return XmlConvert.ToInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static DateTime GetDateTime(string dateTimeString, DateTime defaultValue)
        {
            // adam: using exceptions to handle errors is bad-bad.
            //	the intString=="" case happens a lot!
            if (dateTimeString == null || dateTimeString.Length == 0)
                return defaultValue;

            try
            {
                return XmlConvert.ToDateTime(dateTimeString);
            }
            catch
            {
                try
                {
                    return DateTime.Parse(dateTimeString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static string GetAttribute(XmlElement node, string attributeName)
        {
            return GetAttribute(node, attributeName, null);
        }

        public static string GetAttribute(XmlElement node, string attributeName, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            XmlAttribute attr = node.Attributes[attributeName];

            if (attr == null)
                return defaultValue;

            return attr.Value;
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }

        public static bool GetBool(XmlElement node, bool defaultValue)
        {
            if (node == null)
                return defaultValue;

            string strVal = GetText(node, "xxx");
            if (strVal == "xxx") return defaultValue;
            else if (strVal == "true") return true;
            else if (strVal == "false") return false;
            else return defaultValue;
        }

        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Account information Saving...");

            if (!Directory.Exists("Saves/Accounts"))
                Directory.CreateDirectory("Saves/Accounts");

            string filePath = Path.Combine("Saves/Accounts", "accounts.xml");

            bool bNotSaved = true;
            int attempt = 0;
            while (bNotSaved && attempt < 3)
            {
                try
                {
                    attempt++;
                    using (StreamWriter op = new StreamWriter(filePath))
                    {
                        XmlTextWriter xml = new XmlTextWriter(op);

                        xml.Formatting = Formatting.Indented;
                        xml.IndentChar = '\t';
                        xml.Indentation = 1;

                        xml.WriteStartDocument(true);

                        xml.WriteStartElement("accounts");

                        xml.WriteAttributeString("count", m_Accounts[0].Count.ToString());

                        foreach (Account a in Accounts.Table.Values)
                            a.Save(xml);

                        xml.WriteEndElement();

                        xml.Close();

                        bNotSaved = false;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    System.Console.WriteLine("Caught exception in Accounts.Save: {0}", ex.Message);
                    System.Console.WriteLine(ex.StackTrace);
                    System.Console.WriteLine("Will attempt to recover three times.");
                }
            }
        }

        // used for determining player age
        public static bool IPLookup(System.Net.IPAddress ip)
        {
            if (ip == null || m_IPDatabase == null || m_IPDatabase.Count == 0)
                return false;

            string ipx = GetIPMask(ip);

            if (ipx == null)
                return false;

            foreach (List<string> sx in m_IPDatabase)
            {
                if (sx.Contains(ipx))
                    return true;
            }

            return false;
        }

        public static void Load()
        {
            string[] text;
            if (File.Exists(@"Accounts.txt"))
                text = System.IO.File.ReadAllLines(@"Accounts.txt");
            else
                text = new string[1] { "Saves/Accounts" };

            List<string> been_there = new List<string>();

            // parse the accounts pointer file 
            foreach (string line in text)
            {
                if (line == null || line.Trim().Length == 0 || line.Trim()[0] == ';')
                    continue;

                // we have a pathname!
                string filePath = Path.Combine(line, "accounts.xml");

                try
                {   // exclude duplicate entries. This happens because the 'defalult shard' listed in the accounts.txt file will have an explicit entry later in the file.
                    //	This mechanism allows us to have a common accounts.txt file for all shards.
                    if (been_there.Contains(Path.GetFullPath(filePath)) == true)
                        continue;   // duplicate path
                }
                catch { continue; } // bad path

                been_there.Add(Path.GetFullPath(filePath));

                LoadAccountDB(filePath);
            }
        }

        public static void LoadAccountDB(string filePath)
        {
            int accounts = 0, ips = 0;
            string[] tokens = filePath.Split(new Char[] { '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens != null && tokens.Length > 0)
                Console.Write("Account information Loading for {0}...", tokens[0]);
            else
                Console.Write("Account information Loading...");

            Hashtable accDatabase;
            accDatabase = new Hashtable(32, 1.0f, CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
            List<string> ipDatabase;
            ipDatabase = new List<string>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("done (no data)");
                return;
            }

            // add this account database to our list of account databases
            m_Accounts.Add(accDatabase);

            // add this ip database to our list of ip databases
            m_IPDatabase.Add(ipDatabase);

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["accounts"];

            foreach (XmlElement account in root.GetElementsByTagName("account"))
            {
                try
                {
                    // build the account database
                    Account acct = new Account(account);
                    accDatabase[acct.Username] = acct;
                    accounts++;

                    // build the IP database
                    foreach (System.Net.IPAddress ax in acct.LoginIPs)
                    {
                        ips++;
                        string ipx = GetIPMask(ax);
                        if (ipx != null && !ipDatabase.Contains(ipx))
                            ipDatabase.Add(ipx);
                    }
                }
                catch
                {
                    Console.WriteLine();
                    Console.WriteLine("Warning: Account instance load failed");
                }
            }

            Console.WriteLine("done ({0} accounts, {1}/{2} IP addresses)", accounts, ipDatabase.Count, ips);
        }
    }

    // this is called at logon/connect to record the user's IP address.
    //	we use the delay timer to give us time to check in WelcomeTimer if this is a new-user (i.e., this IP does not exist yet.)
    public class IPLogTimer : Timer
    {
        private System.Net.IPAddress m_IPAddress;

        public IPLogTimer(System.Net.IPAddress IPAddress)
            : base(TimeSpan.FromSeconds(30.0))
        {
            m_IPAddress = IPAddress;
        }

        protected override void OnTick()
        {
            try
            {   // add the recently logged in ip address to our default (this server) ip database
                string ipx = Accounts.GetIPMask(m_IPAddress);
                if (ipx != null && Accounts.IPDatabase.Count > 0)
                    if (!Accounts.IPDatabase[0].Contains(ipx))
                        Accounts.IPDatabase[0].Add(ipx);
                Stop();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
    }
}
