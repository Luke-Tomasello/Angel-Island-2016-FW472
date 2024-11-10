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
 * Created by Pixie
 *	CHANGELOG
 * 3/15/16, Adam
 *		Reverse changes of 2/8/08
 *		Turn IPException code back on. This is because the IPException logic is per IP whereas the MaxAccountsPerIP
 *		functionality is global.
 *	2/18/08, Adam
 *		We now allow 3 accounts per household - IPException logic no longer needed
 *	8/13/06 - Pix.
 *		Added null-checking in IPException checking.
 *	6/14/05 - Pix
 *		Initial Version.
 */

using Server.Commands;
using Server.Diagnostics;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Accounting
{
    /// <summary>
    /// Summary description for IPException.
    /// </summary>
    public class IPException
    {
        private static Hashtable m_IPException = new Hashtable();

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }

        static IPException()
        {
        }

        public static Hashtable Table
        {
            get
            {
                return m_IPException;
            }
        }

        public static int GetIPException(string ip)
        {
            int allowed = 1;
            try
            {
                if (m_IPException != null)
                {
                    object o = m_IPException[ip];
                    if (o != null)
                    {
                        allowed = (int)m_IPException[ip];
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            return allowed;
        }

        public static void AddException(string ip, int limit)
        {
            m_IPException[ip] = limit;
        }


        public static void Save(WorldSaveEventArgs e)
        {
            if (!Directory.Exists("Saves/Accounts"))
                Directory.CreateDirectory("Saves/Accounts");

            string filePath = Path.Combine("Saves/Accounts", "ipexception.xml");

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

                        xml.WriteStartElement("root");

                        xml.WriteAttributeString("count", m_IPException.Count.ToString());

                        foreach (string key in m_IPException.Keys)
                        {
                            xml.WriteStartElement("ipexception");
                            xml.WriteStartElement("ip");
                            xml.WriteString(key);
                            xml.WriteEndElement();
                            xml.WriteStartElement("number");
                            xml.WriteString(m_IPException[key].ToString());
                            xml.WriteEndElement();
                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement();

                        xml.Close();

                        bNotSaved = false;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    System.Console.WriteLine("Caught exception in IPException.Save: {0}", ex.Message);
                    System.Console.WriteLine(ex.StackTrace);
                    System.Console.WriteLine("Will attempt to recover three times.");
                }
            }
        } // end Save()

        public static void Load()
        {
            m_IPException = new Hashtable(32, 1.0f, CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);

            string filePath = Path.Combine("Saves/Accounts", "ipexception.xml");

            if (!File.Exists(filePath))
                return;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["root"];

            foreach (XmlElement ipexception in root.GetElementsByTagName("ipexception"))
            {
                try
                {
                    string ip = ipexception["ip"].InnerText;
                    int number = Int32.Parse(ipexception["number"].InnerText);
                    m_IPException[ip] = number;
                }
                catch
                {
                    Console.WriteLine("Warning (nonfatal!): IPException instance load failed");
                }
            }
        } //end Load()

    }
}
