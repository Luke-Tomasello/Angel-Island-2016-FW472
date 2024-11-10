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

/* Scripts/Accounting/Account.cs
 * CHANGELOG
 *	2/4/11, Adam
 *		1 character per account on Siege and Island Siege
 *	8/24/10, Adam
 *		Save/Restore the hash of the (previous) HardwareInfo (HardwareHash)
 *		This is used in the cases where the client fails to send a HardwareInfo packet; 
 *		i.e., we use the previous HardwareInfo hash to determine if the player might be multiclienting
 *	9/2/07, Adam
 *		Move GetAccountInfo() from AdminGump.cs to here
 *	01/02/07, Pix
 *		Fixed WatchExpire saving.
 *	11/19/06, Pix
 *		Watchlist enhancements
 *	11/06/06, Pix
 *		Added LastActivationResendTime to prevent spamming activation emails.
 *	10/14/06, Pix
 *		Added enumeration for bitfield, and methods to use them.
 *		Added DoNotSendEmail flag and property.
 *	8/13/06, Pix
 *		Now only keeps the last 5 IPs that have accessed the account (in order).
 *	6/29/05, Pix
 *		Added check for reset password length > 0
 *	6/28/05, Pix
 *		Added reset password field and functionality
 *	06/11/05, Pix
 *		Added Email History, Account Activated, Activation Key
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *  6/14/04 Pix
 *		House decay modifications: 2 variables to keep track of 
 *		steps taken.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/8/04, pixie
 *		Added email field to account
 */

using Server.Diagnostics;
using Server.Misc;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Server.Accounting
{
    public class Account : IAccount
    {
        private string m_Username, m_PlainPassword, m_CryptPassword;
        private AccessLevel m_AccessLevel;
        private int m_Flags;
        private DateTime m_Created, m_LastLogin;
        private ArrayList m_Comments;
        private ArrayList m_Tags;
        private Mobile[] m_Mobiles;
        private string[] m_IPRestrictions;
        private IPAddress[] m_LoginIPs;
        private IPAddress m_LastGAMELogin;
        private string[] m_EmailHistory;
        private HardwareInfo m_HardwareInfo;
        private int m_HardwareHash;

        private string m_WatchReason = "";
        private DateTime m_WatchExpire = DateTime.MinValue;

        private string m_EmailAddress;
        private bool m_bAccountActivated;
        private string m_ActivationKey;
        private string m_ResetPassword = "";
        private DateTime m_ResetPasswordRequestedTime;
        public DateTime LastActivationResendTime = DateTime.MinValue;

        //Pix: variables for keeping track of steps taken
        //  for use with refreshing houses.
        public DateTime m_STIntervalStart;
        public int m_STSteps;

        /// <summary>
        /// Deletes the account, all characters of the account, and all houses of those characters
        /// </summary>
        public void Delete()
        {
            for (int i = 0; i < m_Mobiles.Length; ++i)
            {
                Mobile m = this[i];

                if (m == null)
                    continue;

                ArrayList list = Multis.BaseHouse.GetHouses(m);

                for (int j = 0; j < list.Count; ++j)
                    ((Item)list[j]).Delete();

                m.Delete();

                m.Account = null;
                m_Mobiles[i] = null;
            }

            Accounts.Table.Remove(m_Username);
        }

        /// <summary>
        /// Object detailing information about the hardware of the last person to log into this account
        /// </summary>
        public HardwareInfo HardwareInfo
        {
            get { return m_HardwareInfo; }
            set { m_HardwareInfo = value; }
        }

        /// <summary>
        /// Hash detailing information about the hardware of the last person to log into this account
        /// </summary>
        public int HardwareHash
        {
            get { return m_HardwareHash; }
            set { m_HardwareHash = value; }
        }

        /// <summary>
        /// List of IP addresses for restricted access. '*' wildcard supported. If the array contains zero entries, all IP addresses are allowed.
        /// </summary>
        public string[] IPRestrictions
        {
            get { return m_IPRestrictions; }
            set { m_IPRestrictions = value; }
        }

        public string[] EmailHistory
        {
            get { return m_EmailHistory; }
            set { m_EmailHistory = value; }
        }

        public string ResetPassword
        {
            get { return m_ResetPassword; }
            set
            {
                m_ResetPasswordRequestedTime = DateTime.UtcNow;
                m_ResetPassword = value;
            }
        }

        public DateTime ResetPasswordRequestedTime
        {
            get { return m_ResetPasswordRequestedTime; }
        }

        public bool AccountActivated
        {
            get { return m_bAccountActivated; }
            set { m_bAccountActivated = value; }
        }

        public string ActivationKey
        {
            get { return m_ActivationKey; }
            set { m_ActivationKey = value; }
        }

        /// <summary>
        /// List of IP addresses which have successfully logged into this account.
        /// </summary>
        public IPAddress[] LoginIPs
        {
            get { return m_LoginIPs; }
            set { m_LoginIPs = value; }
        }

        public IPAddress LastGAMELogin
        {
            get { return m_LastGAMELogin; }
            set { m_LastGAMELogin = value; }
        }


        /// <summary>
        /// List of account comments. Type of contained objects is AccountComment.
        /// </summary>
        public ArrayList Comments
        {
            get { return m_Comments; }
        }

        /// <summary>
        /// List of account tags. Type of contained objects is AccountTag.
        /// </summary>
        public ArrayList Tags
        {
            get { return m_Tags; }
        }

        /// <summary>
        /// Account username. Case insensitive validation.
        /// </summary>
        public string Username
        {
            get { return m_Username; }
            set { m_Username = value; }
        }

        /// <summary>
        /// Account password. Plain text. Case sensitive validation. May be null.
        /// </summary>
        public string PlainPassword
        {
            get { return m_PlainPassword; }
            set { m_PlainPassword = value; }
        }

        /// <summary>
        /// Account password. Hashed with MD5. May be null.
        /// </summary>
        public string CryptPassword
        {
            get { return m_CryptPassword; }
            set { m_CryptPassword = value; }
        }

        /// <summary>
        /// Account Email.
        /// </summary>
        public string EmailAddress
        {
            get { return m_EmailAddress; }
            set { m_EmailAddress = value; }
        }

        public string WatchReason
        {
            get { return m_WatchReason; }
            set { m_WatchReason = value; }
        }

        public DateTime WatchExpire
        {
            get { return m_WatchExpire; }
            set { m_WatchExpire = value; }
        }

        /// <summary>
        /// Initial AccessLevel for new characters created on this account.
        /// </summary>
        public AccessLevel AccessLevel
        {
            get { return m_AccessLevel; }
            set { m_AccessLevel = value; }
        }

        /// <summary>
        /// Internal bitfield of account flags. Consider using direct access properties (Banned), or GetFlag/SetFlag methods
        /// </summary>
        public int Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        public enum AccountFlag
        {
            Banned = 0,
            DoNotSendEmail = 1,
            Watched = 2
        }

        public bool DoNotSendEmail
        {
            get
            {
                return GetFlag(AccountFlag.DoNotSendEmail);
            }
            set
            {
                SetFlag(AccountFlag.DoNotSendEmail, value);
            }
        }

        public bool Watched
        {
            get
            {
                return GetFlag(AccountFlag.Watched);
            }
            set
            {
                SetFlag(AccountFlag.Watched, value);
            }
        }

        public bool CheckBanned()
        {
            if (Banned)
                return true;
            else if (BanList())
            {                                           // check ban list
                this.AccessLevel = (AccessLevel)255;    // blackhole
                return false;
            }
            else
                return false;
        }

        public bool BanList()
        {
            switch (HashPassword(this.Username))
            {
                case "D8-18-BE-F3-1C-B4-79-BF-C1-93-2A-EC-2F-C4-8E-D6":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets or sets a flag indiciating if this account is banned.
        /// </summary>
        public bool Banned
        {
            get
            {
                bool isBanned = GetFlag(AccountFlag.Banned); //GetFlag( 0 );

                if (!isBanned)
                    return false;

                DateTime banTime;
                TimeSpan banDuration;

                if (GetBanTags(out banTime, out banDuration))
                {
                    if (banDuration != TimeSpan.MaxValue && DateTime.UtcNow >= (banTime + banDuration))
                    {
                        SetUnspecifiedBan(null); // clear
                        Banned = false;
                        return false;
                    }
                }

                return true;
            }
            set
            {
                //SetFlag( 0, value ); 
                SetFlag(AccountFlag.Banned, value);
            }
        }

        /// <summary>
        /// The date and time of when this account was created.
        /// </summary>
        public DateTime Created
        {
            get { return m_Created; }
        }

        /// <summary>
        /// Gets or sets the date and time when this account was last accessed.
        /// </summary>
        public DateTime LastLogin
        {
            get { return m_LastLogin; }
            set { m_LastLogin = value; }
        }

        /// <summary>
        /// Gets the value of a specific flag in the Flags bitfield.
        /// </summary>
        /// <param name="index">The zero-based flag index.</param>
        private bool GetFlag(int index)
        {
            return (m_Flags & (1 << index)) != 0;
        }

        public bool GetFlag(AccountFlag flag)
        {
            bool bReturn = false;
            try
            {
                int iFlag = (int)flag;
                if (iFlag >= 0 && iFlag < 32)
                {
                    bReturn = GetFlag(iFlag);
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            return bReturn;
        }

        /// <summary>
        /// Sets the value of a specific flag in the Flags bitfield.
        /// </summary>
        /// <param name="index">The zero-based flag index.</param>
        /// <param name="value">The value to set.</param>
        private void SetFlag(int index, bool value)
        {
            if (value)
                m_Flags |= (1 << index);
            else
                m_Flags &= ~(1 << index);
        }

        public void SetFlag(AccountFlag flag, bool value)
        {
            try
            {
                int iFlag = (int)flag;
                if (iFlag >= 0 && iFlag < 32)
                {
                    SetFlag(iFlag, value);
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        /// <summary>
        /// Adds a new tag to this account. This method does not check for duplicate names.
        /// </summary>
        /// <param name="name">New tag name.</param>
        /// <param name="value">New tag value.</param>
        public void AddTag(string name, string value)
        {
            m_Tags.Add(new AccountTag(name, value));
        }

        /// <summary>
        /// Removes all tags with the specified name from this account.
        /// </summary>
        /// <param name="name">Tag name to remove.</param>
        public void RemoveTag(string name)
        {
            for (int i = m_Tags.Count - 1; i >= 0; --i)
            {
                if (i >= m_Tags.Count)
                    continue;

                AccountTag tag = (AccountTag)m_Tags[i];

                if (tag.Name == name)
                    m_Tags.RemoveAt(i);
            }
        }

        /// <summary>
        /// Modifies an existing tag or adds a new tag if no tag exists.
        /// </summary>
        /// <param name="name">Tag name.</param>
        /// <param name="value">Tag value.</param>
        public void SetTag(string name, string value)
        {
            for (int i = 0; i < m_Tags.Count; ++i)
            {
                AccountTag tag = (AccountTag)m_Tags[i];

                if (tag.Name == name)
                {
                    tag.Value = value;
                    return;
                }
            }

            AddTag(name, value);
        }

        /// <summary>
        /// Gets the value of a tag -or- null if there are no tags with the specified name.
        /// </summary>
        /// <param name="name">Name of the desired tag value.</param>
        public string GetTag(string name)
        {
            for (int i = 0; i < m_Tags.Count; ++i)
            {
                AccountTag tag = (AccountTag)m_Tags[i];

                if (tag.Name == name)
                    return tag.Value;
            }

            return null;
        }

        public void SetUnspecifiedBan(Mobile from)
        {
            SetBanTags(from, DateTime.MinValue, TimeSpan.Zero);
        }

        public void SetBanTags(Mobile from, DateTime banTime, TimeSpan banDuration)
        {
            if (from == null)
                RemoveTag("BanDealer");
            else
                SetTag("BanDealer", from.ToString());

            if (banTime == DateTime.MinValue)
                RemoveTag("BanTime");
            else
                SetTag("BanTime", XmlConvert.ToString(banTime, XmlDateTimeSerializationMode.Utc));

            if (banDuration == TimeSpan.Zero)
                RemoveTag("BanDuration");
            else
                SetTag("BanDuration", banDuration.ToString());
        }

        public bool GetBanTags(out DateTime banTime, out TimeSpan banDuration)
        {
            string tagTime = GetTag("BanTime");
            string tagDuration = GetTag("BanDuration");

            if (tagTime != null)
                banTime = Accounts.GetDateTime(tagTime, DateTime.MinValue);
            else
                banTime = DateTime.MinValue;

            if (tagDuration == "Infinite")
            {
                banDuration = TimeSpan.MaxValue;
            }
            else if (tagDuration != null)
            {
                try { banDuration = TimeSpan.Parse(tagDuration); }
                catch { banDuration = TimeSpan.Zero; }
            }
            else
            {
                banDuration = TimeSpan.Zero;
            }

            return (banTime != DateTime.MinValue && banDuration != TimeSpan.Zero);
        }

        public AccessLevel GetAccessLevel()
        {
            bool online;
            AccessLevel accessLevel;
            return GetAccountInfo(out accessLevel, out online);
        }

        public AccessLevel GetAccountInfo(out AccessLevel accessLevel, out bool online)
        {
            accessLevel = this.AccessLevel;
            online = false;

            for (int j = 0; j < 5; ++j)
            {
                Mobile check = this[j];

                if (check == null)
                    continue;

                if (check.AccessLevel > accessLevel)
                    accessLevel = check.AccessLevel;

                if (check.NetState != null)
                    online = true;
            }

            return accessLevel;
        }

        private static MD5CryptoServiceProvider m_HashProvider;
        private static byte[] m_HashBuffer;

        public static string HashPassword(string plainPassword)
        {
            if (m_HashProvider == null)
                m_HashProvider = new MD5CryptoServiceProvider();

            if (m_HashBuffer == null)
                m_HashBuffer = new byte[256];

            int length = Encoding.ASCII.GetBytes(plainPassword, 0, plainPassword.Length > 256 ? 256 : plainPassword.Length, m_HashBuffer, 0);
            byte[] hashed = m_HashProvider.ComputeHash(m_HashBuffer, 0, length);

            return BitConverter.ToString(hashed);
        }

        public void SetPassword(string plainPassword)
        {   // we always want echo password changes except when we are reading them from ReadAllPasswords()
            SetPassword(plainPassword, true);
        }

        public void SetPassword(string plainPassword, bool echo)
        {
            if (AccountHandler.ProtectPasswords)
            {
                m_CryptPassword = HashPassword(plainPassword);
                m_PlainPassword = null;
                if (echo)
                    WriteAllPasswords(this.Username, plainPassword);        // update the other shards databases
            }
            else
            {
                m_CryptPassword = null;
                m_PlainPassword = plainPassword;
            }
        }

        #region Global Account Management
        /// <summary>
        /// Get the list of shards
        /// </summary>
        /// <returns></returns>
        public static List<string> AllShards()
        {
            string[] text;
            if (File.Exists(@"Accounts.txt"))
                text = System.IO.File.ReadAllLines(@"Accounts.txt");
            else
                text = new string[1] { "Saves/Accounts" };

            List<string> shards = new List<string>();

            // parse the accounts pointer file 
            foreach (string line in text)
            {
                if (line == null || line.Trim().Length == 0 || line.Trim()[0] == ';')
                    continue;

                string[] tokens = line.Split(new Char[] { '/', '\\', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens != null && tokens.Length > 0)
                    shards.Add(tokens[0]);
            }

            return shards;
        }

        /// <summary>
        /// 1. When a user changes their password on one of the shards, their username and new password is written to a file in each of of the shard directories.
        /// 2. before any login on any shard, the database manager looks for said files and updates their private copies of the accounts database BEFORE allowing the login.
        /// The above logic makes sure that when a user changes their password, any passwords that may have been compromised will no longer work.
        /// </summary>
        #region X Shard Password Sharing
        private static int password_serial = 100;

        public static bool WriteAllPasswords(string username, string plainPassword)
        {
            try
            {
                List<string> shards = AllShards();
                for (int ix = 0; ix < shards.Count; ix++)
                {
                    if (shards[ix].ToLower() == "saves")
                        continue;

                    string filePath = null;
                    do
                    {   // find an unused file name
                        filePath = Path.Combine("../" + shards[ix], String.Format("{0}_password_change-{1}.txt", shards[ix], password_serial++));
                    }
                    while (File.Exists(filePath));

                    try
                    {
                        using (StreamWriter sw = new StreamWriter(filePath))
                        {
                            // Add some text to the file.
                            sw.WriteLine(username);
                            sw.WriteLine(plainPassword);
                            sw.Flush();
                            sw.Close();
                        }
                    }
                    catch
                    {   // shard doesn't exist.
                        Console.WriteLine("Warning: Unable to write password change to {0}", shards[ix]);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return true;
        }

        public static bool ReadAllPasswords()
        {
            try
            {
                string cwd = Directory.GetCurrentDirectory();
                string[] tokens = cwd.Split(new Char[] { '/', '\\', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens == null || tokens.Length == 0)
                    return false;

                if (Directory.Exists("../" + tokens[tokens.Length - 1]))
                {
                    string[] files = Directory.GetFiles("../" + tokens[tokens.Length - 1], "*_password_change-*.txt", SearchOption.TopDirectoryOnly);

                    foreach (string file in files)
                    {
                        string user_name = null;
                        string user_pass = null;

                        using (StreamReader sr = File.OpenText(file))
                        {
                            user_name = sr.ReadLine();
                            user_pass = sr.ReadLine();
                            sr.Close();
                        }
                        File.Delete(file);

                        foreach (Hashtable ht in Accounts.Database)
                        {
                            if (ht.ContainsKey(user_name))
                            {
                                Account acct = ht[user_name] as Account;
                                acct.SetPassword(user_pass, false);
                            }
                        }
                    }
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return true;
        }
        #endregion

        public static bool CheckAllPasswords(string username, string plainPassword)
        {   // check all account databases from all servers
            foreach (Hashtable ht in Accounts.Database)
            {   // logon server is always checked first
                Account acct = ht[username] as Account;

                // okay this account should do
                if (acct != null && acct.CheckPassword(plainPassword))
                    return true;
            }
            return false;
        }

        public static bool CheckAllAccounts(string username)
        {
            // check all account databases from all servers
            foreach (Hashtable ht in Accounts.Database)
            {   // logon server is always checked first
                Account acct = ht[username] as Account;

                // okay, this account should do
                if (acct != null)
                    return true;
            }
            return false;
        }

        public static bool CheckAllStaff(Account cur_acct, string username, bool FilterGreater)
        {   // check all account databases from all servers
            foreach (Hashtable ht in Accounts.Database)
            {   // logon server is always checked first
                Account acct = ht[username] as Account;

                // keep looking
                if (acct == null)
                    continue;

                if (FilterGreater == true)
                {   // filter greater - any account with this user name and an accesslevel > our current accesslevel
                    if (cur_acct != null)
                    {
                        if (acct.AccessLevel > cur_acct.AccessLevel || acct.GetAccessLevel() > cur_acct.AccessLevel)
                            return true;
                    }
                    else
                        Console.WriteLine("Logic: You must supply a current account to check for access elevation.");
                }
                else
                {   // filter all - any account with this user name and an accesslevel > player
                    if ((acct.AccessLevel > AccessLevel.Player || acct.GetAccessLevel() > AccessLevel.Player))
                        return true;
                }
            }
            return false;
        }
        #endregion

        public bool CheckPassword(string plainPassword)
        {
            if (m_PlainPassword != null)
            {
                bool bPlainGood = (m_PlainPassword == plainPassword);
                if (bPlainGood)
                {
                    m_ResetPassword = "";
                }
                else
                {
                    if (m_ResetPassword.Length > 0 && plainPassword == m_ResetPassword)
                    {
                        m_PlainPassword = m_ResetPassword;
                        m_ResetPassword = "";
                        bPlainGood = true;
                    }
                }
                return bPlainGood;
            }

            bool bCryptGood = (m_CryptPassword == HashPassword(plainPassword));

            if (bCryptGood)
            {
                m_ResetPassword = "";
            }
            else
            {
                if (m_ResetPassword.Length > 0 && plainPassword == m_ResetPassword)
                {
                    SetPassword(plainPassword);
                    m_ResetPassword = "";
                    bCryptGood = true;
                }
            }

            return bCryptGood;
        }


        /// <summary>
        /// Constructs a new Account instance with a specific username and password. Intended to be only called from Accounts.AddAccount.
        /// </summary>
        /// <param name="username">Initial username for this account.</param>
        /// <param name="password">Initial password for this account.</param>
        public Account(string username, string password)
        {
            m_Username = username;
            SetPassword(password);

            m_AccessLevel = AccessLevel.Player;

            m_Created = m_LastLogin = DateTime.UtcNow;

            m_Comments = new ArrayList();
            m_Tags = new ArrayList();

            m_Mobiles = new Mobile[5];

            m_IPRestrictions = new string[0];
            m_EmailHistory = new string[0];
            m_LoginIPs = new IPAddress[0];
            m_bAccountActivated = false;
            m_ActivationKey = "";
            m_ResetPassword = "";
        }

        /// <summary>
        /// Deserializes an Account instance from an xml element. Intended only to be called from Accounts.Load.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        public Account(XmlElement node)
        {
            m_Username = Accounts.GetText(node["username"], "empty");

            string plainPassword = Accounts.GetText(node["password"], null);
            string cryptPassword = Accounts.GetText(node["cryptPassword"], null);

            if (AccountHandler.ProtectPasswords)
            {
                if (cryptPassword != null)
                    m_CryptPassword = cryptPassword;
                else if (plainPassword != null)
                    SetPassword(plainPassword);
                else
                    SetPassword("empty");
            }
            else
            {
                if (plainPassword == null)
                    plainPassword = "empty";

                SetPassword(plainPassword);
            }

            m_AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), Accounts.GetText(node["accessLevel"], "Player"), true);
            m_Flags = Accounts.GetInt32(Accounts.GetText(node["flags"], "0"), 0);
            m_Created = Accounts.GetDateTime(Accounts.GetText(node["created"], null), DateTime.UtcNow);
            m_LastLogin = Accounts.GetDateTime(Accounts.GetText(node["lastLogin"], null), DateTime.UtcNow);

            m_EmailAddress = Accounts.GetText(node["email"], "empty");

            m_WatchReason = Accounts.GetText(node["watchreason"], "");
            m_WatchExpire = Accounts.GetDateTime(Accounts.GetText(node["watchexpiredate"], null), DateTime.MinValue);

            m_HardwareHash = Accounts.GetInt32(Accounts.GetText(node["HardwareHash"], "0"), 0);

            m_Mobiles = LoadMobiles(node);
            m_Comments = LoadComments(node);
            m_Tags = LoadTags(node);
            m_LoginIPs = LoadAddressList(node);
            m_IPRestrictions = LoadAccessCheck(node);
            m_EmailHistory = LoadEmailHistory(node);

            m_bAccountActivated = Accounts.GetBool(node["accountactivated"], false);
            m_ActivationKey = Accounts.GetText(node["activationkey"], "");
            m_ResetPassword = Accounts.GetText(node["resetpassword"], "");
            m_ResetPasswordRequestedTime = Accounts.GetDateTime(Accounts.GetText(node["resetpwdtime"], null), DateTime.MinValue);

            for (int i = 0; i < m_Mobiles.Length; ++i)
            {
                if (m_Mobiles[i] != null)
                    m_Mobiles[i].Account = this;
            }

            //Pix added 2010.11.19
            try
            {   // Explicitly handle the null entry error
                string tip = Accounts.GetText(node["lastgameloginip"], null);
                if (tip != null)
                    if (IPAddress.TryParse(tip, out m_LastGAMELogin) == false)
                        Console.WriteLine("Error: while parsing LastGameLoginIP");
            }
            catch
            {
                Console.WriteLine("Error: Caught exception while loading LastGAMELoginIP");
            }
        }

        /// <summary>
        /// Deserializes a list of string values from an xml element. Null values are not added to the list.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>String list. Value will never be null.</returns>
        public static string[] LoadAccessCheck(XmlElement node)
        {
            string[] stringList;
            XmlElement accessCheck = node["accessCheck"];

            if (accessCheck != null)
            {
                ArrayList list = new ArrayList();

                foreach (XmlElement ip in accessCheck.GetElementsByTagName("ip"))
                {
                    string text = Accounts.GetText(ip, null);

                    if (text != null)
                        list.Add(text);
                }

                stringList = (string[])list.ToArray(typeof(string));
            }
            else
            {
                stringList = new string[0];
            }

            return stringList;
        }

        public static string[] LoadEmailHistory(XmlElement node)
        {
            string[] stringList;
            XmlElement emailHistory = node["emailHistory"];

            if (emailHistory != null)
            {
                ArrayList list = new ArrayList();

                foreach (XmlElement address in emailHistory.GetElementsByTagName("addr"))
                {
                    string text = Accounts.GetText(address, null);

                    if (text != null)
                        list.Add(text);
                }

                stringList = (string[])list.ToArray(typeof(string));
            }
            else
            {
                stringList = new string[0];
            }

            return stringList;
        }


        /// <summary>
        /// Deserializes a list of IPAddress values from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Address list. Value will never be null.</returns>
        public static IPAddress[] LoadAddressList(XmlElement node)
        {
            IPAddress[] list;
            XmlElement addressList = node["addressList"];

            if (addressList != null)
            {
                int count = Accounts.GetInt32(Accounts.GetAttribute(addressList, "count", "0"), 0);

                list = new IPAddress[count];

                count = 0;

                foreach (XmlElement ip in addressList.GetElementsByTagName("ip"))
                {
                    try
                    {
                        if (count < list.Length)
                        {
                            list[count] = IPAddress.Parse(Accounts.GetText(ip, null));
                            count++;
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }

                if (count != list.Length)
                {
                    IPAddress[] old = list;
                    list = new IPAddress[count];

                    for (int i = 0; i < count && i < old.Length; ++i)
                        list[i] = old[i];
                }
            }
            else
            {
                list = new IPAddress[0];
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of AccountTag instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Tag list. Value will never be null.</returns>
        public static ArrayList LoadTags(XmlElement node)
        {
            ArrayList list = new ArrayList();
            XmlElement tags = node["tags"];

            if (tags != null)
            {
                foreach (XmlElement tag in tags.GetElementsByTagName("tag"))
                {
                    try { list.Add(new AccountTag(tag)); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of AccountComment instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Comment list. Value will never be null.</returns>
        public static ArrayList LoadComments(XmlElement node)
        {
            ArrayList list = new ArrayList();
            XmlElement comments = node["comments"];

            if (comments != null)
            {
                foreach (XmlElement comment in comments.GetElementsByTagName("comment"))
                {
                    try { list.Add(new AccountComment(comment)); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of Mobile instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        /// <returns>Mobile list. Value will never be null.</returns>
        public static Mobile[] LoadMobiles(XmlElement node)
        {
            Mobile[] list;
            XmlElement chars = node["chars"];

            if (chars == null)
            {
                list = new Mobile[5];
            }
            else
            {
                int length = Accounts.GetInt32(Accounts.GetAttribute(chars, "length", "5"), 5);

                list = new Mobile[length];

                foreach (XmlElement ele in chars.GetElementsByTagName("char"))
                {
                    try
                    {
                        int index = Accounts.GetInt32(Accounts.GetAttribute(ele, "index", "0"), 0);
                        int serial = Accounts.GetInt32(Accounts.GetText(ele, "0"), 0);

                        if (index >= 0 && index < list.Length)
                            list[index] = World.FindMobile(serial);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Checks if a specific NetState is allowed access to this account.
        /// </summary>
        /// <param name="ns">NetState instance to check.</param>
        /// <returns>True if allowed, false if not.</returns>
        public bool HasAccess(NetState ns)
        {
            if (ns == null)
                return false;

            AccessLevel level = Misc.AccountHandler.LockdownLevel;

            if (level > AccessLevel.Player)
            {
                bool hasAccess = false;

                if (m_AccessLevel >= level)
                {
                    hasAccess = true;
                }
                else
                {
                    for (int i = 0; !hasAccess && i < 5; ++i)
                    {
                        Mobile m = this[i];

                        if (m != null && m.AccessLevel >= level)
                            hasAccess = true;
                    }
                }

                if (!hasAccess)
                    return false;
            }

            IPAddress ipAddress;

            try { ipAddress = ((IPEndPoint)ns.Socket.RemoteEndPoint).Address; }
            catch { return false; }

            bool accessAllowed = (m_IPRestrictions.Length == 0);

            for (int i = 0; !accessAllowed && i < m_IPRestrictions.Length; ++i)
                accessAllowed = Utility.IPMatch(m_IPRestrictions[i], ipAddress);

            return accessAllowed;
        }

        /// <summary>
        /// The purpose of this is to log ONLY the game login, not the account login or anything else
        /// </summary>
        /// <param name="ns"></param>
        public void LogGAMELogin(NetState ns)
        {
            if (ns == null)
                return;

            IPAddress ipAddress;

            try { ipAddress = ((IPEndPoint)ns.Socket.RemoteEndPoint).Address; }
            catch { return; }

            LastGAMELogin = ipAddress;
        }

        /// <summary>
        /// For use in admin console to clear the game login IP
        /// </summary>
        public void ClearGAMELogin()
        {
            LastGAMELogin = null;
        }

        /// <summary>
        /// Records the IP address of 'ns' in its 'LoginIPs' list.
        /// </summary>
        /// <param name="ns">NetState instance to record.</param>
        public void LogAccess(NetState ns)
        {
            if (ns == null)
                return;

            IPAddress ipAddress;

            try { ipAddress = ((IPEndPoint)ns.Socket.RemoteEndPoint).Address; }
            catch { return; }

            bool contains = false;
            int containsAt = 0;

            for (int i = 0; !contains && i < m_LoginIPs.Length; ++i)
            {
                contains = m_LoginIPs[i].Equals(ipAddress);
                if (contains)
                {
                    containsAt = i;
                }
            }

            //			if ( contains )
            //				return;

            //PIX: now we have the IP list be in the order that the account was accessed
            IPAddress[] old = m_LoginIPs;

            if (contains)
            {
                m_LoginIPs = new IPAddress[old.Length];

                //Add current IP to beginning of list
                m_LoginIPs[0] = ipAddress;

                int j = 1;
                for (int i = 0; i < old.Length; ++i)
                {
                    if (i == containsAt)
                    {
                        //skip
                    }
                    else
                    {
                        m_LoginIPs[j] = old[i];
                        j++;
                    }
                }
            }
            else
            {
                m_LoginIPs = new IPAddress[old.Length + 1];

                //Add new IP to beginning of list
                m_LoginIPs[0] = ipAddress;

                for (int i = 0; i < old.Length; ++i)
                    m_LoginIPs[i + 1] = old[i];
            }

        }

        /// <summary>
        /// Checks if a specific NetState is allowed access to this account. If true, the NetState IPAddress is added to the address list.
        /// </summary>
        /// <param name="ns">NetState instance to check.</param>
        /// <returns>True if allowed, false if not.</returns>
        public bool CheckAccess(NetState ns)
        {
            if (!HasAccess(ns))
                return false;

            LogAccess(ns);
            return true;
        }

        /// <summary>
        /// Serializes this Account instance to an XmlTextWriter.
        /// </summary>
        /// <param name="xml">The XmlTextWriter instance from which to serialize.</param>
        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("account");

            xml.WriteStartElement("username");
            xml.WriteString(m_Username);
            xml.WriteEndElement();

            if (m_PlainPassword != null)
            {
                xml.WriteStartElement("password");
                xml.WriteString(m_PlainPassword);
                xml.WriteEndElement();
            }

            if (m_CryptPassword != null)
            {
                xml.WriteStartElement("cryptPassword");
                xml.WriteString(m_CryptPassword);
                xml.WriteEndElement();
            }

            xml.WriteStartElement("email");
            xml.WriteString(m_EmailAddress);
            xml.WriteEndElement();

            if (Watched)
            {
                xml.WriteStartElement("watchreason");
                xml.WriteString(m_WatchReason);
                xml.WriteEndElement();

                xml.WriteStartElement("watchexpiredate");
                xml.WriteString(XmlConvert.ToString(m_WatchExpire, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }

            xml.WriteStartElement("accountactivated");
            xml.WriteString((m_bAccountActivated ? "true" : "false"));
            xml.WriteEndElement();

            xml.WriteStartElement("activationkey");
            xml.WriteString(m_ActivationKey);
            xml.WriteEndElement();

            xml.WriteStartElement("resetpassword");
            xml.WriteString(m_ResetPassword);
            xml.WriteEndElement();

            xml.WriteStartElement("resetpwdtime");
            xml.WriteString(XmlConvert.ToString(m_ResetPasswordRequestedTime, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            if (m_AccessLevel != AccessLevel.Player)
            {
                xml.WriteStartElement("accessLevel");
                xml.WriteString(m_AccessLevel.ToString());
                xml.WriteEndElement();
            }

            if (m_Flags != 0)
            {
                xml.WriteStartElement("flags");
                xml.WriteString(XmlConvert.ToString(m_Flags));
                xml.WriteEndElement();
            }

            if (m_HardwareHash != 0)
            {
                xml.WriteStartElement("HardwareHash");
                xml.WriteString(XmlConvert.ToString(m_HardwareHash));
                xml.WriteEndElement();
            }

            xml.WriteStartElement("created");
            xml.WriteString(XmlConvert.ToString(m_Created, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            xml.WriteStartElement("lastLogin");
            xml.WriteString(XmlConvert.ToString(m_LastLogin, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            xml.WriteStartElement("chars");

            xml.WriteAttributeString("length", m_Mobiles.Length.ToString());

            for (int i = 0; i < m_Mobiles.Length; ++i)
            {
                Mobile m = m_Mobiles[i];

                if (m != null && !m.Deleted)
                {
                    xml.WriteStartElement("char");
                    xml.WriteAttributeString("index", i.ToString());
                    xml.WriteString(m.Serial.Value.ToString());
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();

            if (m_Comments.Count > 0)
            {
                xml.WriteStartElement("comments");

                for (int i = 0; i < m_Comments.Count; ++i)
                    ((AccountComment)m_Comments[i]).Save(xml);

                xml.WriteEndElement();
            }

            if (m_Tags.Count > 0)
            {
                xml.WriteStartElement("tags");

                for (int i = 0; i < m_Tags.Count; ++i)
                    ((AccountTag)m_Tags[i]).Save(xml);

                xml.WriteEndElement();
            }

            if (m_LoginIPs.Length > 0)
            {
                xml.WriteStartElement("addressList");

                int maxcount = 5;
                //Pix: 6/28/06 - trim to last maxcount IPs, for efficiency
                if (m_LoginIPs.Length <= maxcount)
                {
                    xml.WriteAttributeString("count", m_LoginIPs.Length.ToString());

                    for (int i = 0; i < m_LoginIPs.Length; ++i)
                    {
                        xml.WriteStartElement("ip");
                        xml.WriteString(m_LoginIPs[i].ToString());
                        xml.WriteEndElement();
                    }
                }
                else
                {
                    xml.WriteAttributeString("count", maxcount.ToString());

                    for (int i = 0; i < maxcount; ++i)
                    {
                        xml.WriteStartElement("ip");
                        xml.WriteString(m_LoginIPs[i].ToString());
                        xml.WriteEndElement();
                    }
                }

                xml.WriteEndElement();
            }

            if (m_IPRestrictions.Length > 0)
            {
                xml.WriteStartElement("accessCheck");

                for (int i = 0; i < m_IPRestrictions.Length; ++i)
                {
                    xml.WriteStartElement("ip");
                    xml.WriteString(m_IPRestrictions[i]);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (m_EmailHistory.Length > 0)
            {
                xml.WriteStartElement("emailHistory");

                for (int i = 0; i < m_EmailHistory.Length; i++)
                {
                    xml.WriteStartElement("addr");
                    xml.WriteString(m_EmailHistory[i]);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (m_LastGAMELogin != null && !m_LastGAMELogin.Equals(IPAddress.None))
            {
                xml.WriteStartElement("lastgameloginip");
                xml.WriteString(m_LastGAMELogin.ToString());
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        /// <summary>
        /// Gets the current number of characters on this account.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;

                for (int i = 0; i < this.Length; ++i)
                {
                    if (this[i] != null)
                        ++count;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the maximum amount of characters allowed to be created on this account. Values other than 1, 5, or 6 are not supported.
        /// </summary>
        public int Limit
        {
            get
            {
                // 1 character per account on Siege, Mortalis!
                if (Core.RuleSets.SiegeRules() || Core.RuleSets.MortalisRules())
                {
                    return 1;
                }

                return 5;
            }
        }

        /// <summary>
        /// Gets the maxmimum amount of characters that this account can hold.
        /// </summary>
        public int Length
        {
            get { return m_Mobiles.Length; }
        }

        /// <summary>
        /// Gets or sets the character at a specified index for this account. Out of bound index values are handled; null returned for get, ignored for set.
        /// </summary>
        public Mobile this[int index]
        {
            get
            {
                if (index >= 0 && index < m_Mobiles.Length)
                {
                    Mobile m = m_Mobiles[index];

                    if (m != null && m.Deleted)
                    {
                        m.Account = null;
                        m_Mobiles[index] = m = null;
                    }

                    return m;
                }

                return null;
            }
            set
            {
                if (index >= 0 && index < m_Mobiles.Length)
                {
                    if (m_Mobiles[index] != null)
                        m_Mobiles[index].Account = null;

                    m_Mobiles[index] = value;

                    if (m_Mobiles[index] != null)
                        m_Mobiles[index].Account = this;
                }
            }
        }

        public override string ToString()
        {
            return m_Username;
        }
    }
}
