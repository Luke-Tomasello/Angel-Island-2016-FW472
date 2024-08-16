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

/* Misc/GoldTracker.cs
 * Created by Pixie.
 * 
 * ChangeLog:
 *  9/2/07, Adam
 *		- Check for character access > Player (not just account access > Player.)
 *			This is because your account may be AccessLevel.Player, your you have a Seer character.
 *			This is infact the case with most of our staff .. there are good reasons for this btw.
 *		- Eliminate tent deeds from being counted, we do however continue to search tents for gold etc.
 *  9/1/07, Pix
 *      Fixed console timing output.
 *  9/1/07, Pix
 *      - Fixed -ib
 *      - Fixed -ss
 *      - Added placed houses worth
 *      - Included storage upgrade deeds/worth
 *	9/1/07, Adam
 *		- Add null check for the vendor.owner field (may be null)
 *		- Add "please wait" broadcast message
 *  8/31/07 - Pix
 *      Now counts locked down bankchecks (oversight), house deeds (+value), and has optional arguments.
 *	7/8/06 - Pix
 *		Changed the way goldtracker works.  Added sorting.
 *	7/7/06 - Pix
 *		Added PlayerVendor totals.
 *	6/06/05 - Pix
 *		Now also counts lockboxes too in count of gold.
 * 4/22/04 - pixie
 *    Initial Version
 */

using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Server.Misc
{
    public class GoldTracker : Timer
    {
        private static bool bInProcess = false;
        private const double SAVE_FREQUENCY = 10.0; //in minutes, with a 5-minute threshold.
        private const string OUTPUT_DIRECTORY = "web"; //directory to place file
        private const string OUTPUT_FILE = "gold.html"; //file for output

        public static void Initialize()
        {
            //uncomment the following line if you want to have it on a timer
            //new GoldTracker().Start();
            CommandSystem.Register("GoldTracker", AccessLevel.Administrator, new CommandEventHandler(GoldTracker_OnCommand));
        }

        public GoldTracker()
            : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(10.0))
        {
            Priority = TimerPriority.OneMinute;
        }

        private static string Encode(string input)
        {
            StringBuilder sb = new StringBuilder(input);

            sb.Replace("&", "&amp;");
            sb.Replace("<", "&lt;");
            sb.Replace(">", "&gt;");
            sb.Replace("\"", "&quot;");
            sb.Replace("'", "&apos;");

            return sb.ToString();
        }

        protected override void OnTick()
        {
            OnCalculateAndWrite(null);
        }

        [Usage("GoldTracker")]
        [Description("Tracks the gold in the world.")]
        private static void GoldTracker_OnCommand(CommandEventArgs e)
        {

            Server.World.Broadcast(0x35, true, "Performing routine maintenance, please wait.");
            DateTime startTime = DateTime.Now;

            if (e.Arguments.Length == 0)
            {
                OnCalculateAndWrite(e.Mobile); //this does 100
            }
            else
            {
                int number = 100;
                //-ct:xxx // use this amount
                //-sn // supress names 
                //-ss // supress staff 
                //-ib // indicate banned 
                bool bSupressNames = false;
                bool bSupressStaff = false;
                bool bIndicateBanned = false;

                for (int i = 0; i < e.Arguments.Length; i++)
                {
                    try
                    {
                        string arg = e.Arguments[i];

                        if (arg.StartsWith("-ct"))
                        {
                            string num = arg.Substring(4);
                            number = Int32.Parse(num);
                        }
                        else if (arg.StartsWith("-sn"))
                        {
                            bSupressNames = true;
                        }
                        else if (arg.StartsWith("-ss"))
                        {
                            bSupressStaff = true;
                        }
                        else if (arg.StartsWith("-ib"))
                        {
                            bIndicateBanned = true;
                        }
                        else
                        {
                            e.Mobile.SendMessage("Ignoring unrecognized switch: " + arg);
                        }
                    }
                    catch (Exception argEx)
                    {
                        e.Mobile.SendMessage("Exception (" + argEx.Message + ") when parsing arg: " + e.Arguments[i]);
                    }
                }

                if (number <= 0)
                {
                    number = Server.Accounting.Accounts.Table.Count;
                }

                OnCalculateAndWrite(e.Mobile, number, bSupressNames, bSupressStaff, bIndicateBanned);
            }

            DateTime endTime = DateTime.Now;
            Server.World.Broadcast(0x35, true, "Routine maintenance complete. The entire process took {0:F1} seconds.", (endTime - startTime).TotalSeconds);
        }

        private static void SM(Mobile m, string message)
        {
            if (m != null && message != null && message.Length > 0)
            {
                m.SendMessage(message);
            }
        }

        private static void OnCalculateAndWrite(Mobile cmdmob)
        {
            OnCalculateAndWrite(cmdmob, 100);
        }

        private static void OnCalculateAndWrite(Mobile cmdmob, int maxtoprint)
        {
            OnCalculateAndWrite(cmdmob, maxtoprint, false, false, false);
        }

        private static void OnCalculateAndWrite(Mobile cmdmob, int maxtoprint, bool bSupressNames, bool bSupressStaff, bool bIndicateBanned)
        {
            if (bInProcess == true)
            {
                System.Console.WriteLine("Got GoldTracker call when already processing.");
                return;
            }

            bInProcess = true;

            DateTime startDateTime = DateTime.Now;

            if (!Directory.Exists(OUTPUT_DIRECTORY))
                Directory.CreateDirectory(OUTPUT_DIRECTORY);

            try
            {
                string initmsg = string.Format("Tracking Gold...outputing top {0}", maxtoprint);
                System.Console.WriteLine(initmsg);
                SM(cmdmob, initmsg);

#if false
				using ( StreamWriter op = new StreamWriter( OUTPUT_DIRECTORY + "/" + OUTPUT_FILE ) )
				{
					op.WriteLine("<HTML><BODY>");
					op.WriteLine("<TABLE BORDER=1 WIDTH=90%><TR>");
					op.WriteLine("<TD>Account</TD><TD>Character</TD><TD>Total Worth</TD><TD>Backpack Gold</TD><TD>Bank Gold</TD><TD>House Gold</TD><TD>Number of Houses</TD><TD>Vendor Gold</TD>");
					op.WriteLine("</TR>");

					foreach ( Account acct in Accounts.Table.Values )
					{
						string acctname = acct.Username;
						//System.Console.WriteLine(acctname);
						
						//iterate through characters:
						for ( int i = 0; i < 5; ++i )
						{
							if ( acct[i] != null )
							{
								PlayerMobile player = (PlayerMobile)acct[i];

								op.WriteLine("<TR><TD><B>" + acctname + "</B></TD>");
								op.WriteLine("<TD>" + player.Name + "</TD>");

								Container backpack = player.Backpack;
								BankBox bank = player.BankBox;
								int backpackgold = 0;
								int bankgold = 0;
							
								//BACKPACK
								backpackgold = GetGoldInContainer(backpack);

								//BANK
								bankgold = GetGoldInContainer(bank);

								//house?
								int housetotal = 0;
								ArrayList list = Multis.BaseHouse.GetHouses( player );
								for ( int j = 0; j < list.Count; ++j )
								{
									if( list[j] is BaseHouse )
									{
										BaseHouse house = (BaseHouse)list[j];
										housetotal += GetGoldInHouse(house);
									}
								}

								//vendors
								int vendortotal = 0;
								ArrayList vendors = GetVendors( player );
								for( int j=0; j<vendors.Count; j++ )
								{
									if( vendors[j] is PlayerVendor )
									{
										PlayerVendor pv = vendors[j] as PlayerVendor;
										vendortotal += pv.HoldGold;
									}
								}

								op.WriteLine("<TD>" + (backpackgold+bankgold+housetotal+vendortotal) + "</TD>");

								op.WriteLine("<TD>" + backpackgold + "</TD>");
								op.WriteLine("<TD>" + bankgold + "</TD>");
								op.WriteLine("<TD>" + housetotal + "</TD>");
								op.WriteLine("<TD>" + list.Count + "</TD>");
								op.WriteLine("<TD>" + vendortotal + " (" + vendors.Count + " vendors)"  + "</TD>");

								op.WriteLine("</TR>");

							}
						}
					}

					op.WriteLine("</BODY></HTML>");
					op.Flush();
					op.Close();
				}
#else
                Hashtable table = new Hashtable(Accounts.Table.Values.Count);

                System.Console.WriteLine("GT: building hashtable");
                SM(cmdmob, "GT: building hashtable");
                foreach (Account acct in Accounts.Table.Values)
                {
                    if (acct == null)
                        continue;

                    if ((bSupressStaff && acct.GetAccessLevel() > AccessLevel.Player) == false)
                        table.Add(acct, new GoldTotaller());
                }

                System.Console.WriteLine("GT: looping through accounts");
                SM(cmdmob, "GT: looping through accounts");
                foreach (Account acct in Accounts.Table.Values)
                {
                    if (acct == null)
                        continue;

                    if ((bSupressStaff && acct.GetAccessLevel() > AccessLevel.Player) == false)
                    {
                        //iterate through characters:
                        for (int i = 0; i < 5; ++i)
                        {
                            if (acct[i] != null)
                            {
                                PlayerMobile player = (PlayerMobile)acct[i];
                                Container backpack = player.Backpack;
                                BankBox bank = player.BankBox;
                                int backpackgold = 0;
                                int bankgold = 0;

                                int housedeedcount = 0;
                                int housedeedworth = 0;

                                int housesworth = 0;

                                //BACKPACK
                                backpackgold = GetGoldInContainer(backpack);
                                housedeedworth = GetHouseDeedsInContainer(backpack, ref housedeedcount);

                                //BANK
                                bankgold = GetGoldInContainer(bank);
                                housedeedworth += GetHouseDeedsInContainer(bank, ref housedeedcount);

                                //house?
                                int housetotal = 0;
                                ArrayList list = Multis.BaseHouse.GetHouses(player);
                                for (int j = 0; j < list.Count; ++j)
                                {
                                    if (list[j] is BaseHouse)
                                    {
                                        BaseHouse house = (BaseHouse)list[j];
                                        housesworth += (int)house.UpgradeCosts;
                                        HouseDeed hd = house.GetDeed();
                                        if (hd != null && hd is SiegeTentBag == false && hd is TentBag == false)
                                        {
                                            housesworth += RealEstateBroker.ComputePriceFor(hd);
                                            hd.Delete();
                                        }

                                        housetotal += GetGoldInHouse(house);
                                        housedeedworth += GetHouseDeedWorthInHouse(house, ref housedeedcount);
                                    }
                                }

                                ((GoldTotaller)table[acct]).BackpackGold += backpackgold;
                                ((GoldTotaller)table[acct]).BankGold += bankgold;
                                ((GoldTotaller)table[acct]).HouseGold += housetotal;
                                ((GoldTotaller)table[acct]).HouseCount += list.Count;
                                ((GoldTotaller)table[acct]).CharacterCount += 1;
                                ((GoldTotaller)table[acct]).HouseDeedCount += housedeedcount;
                                ((GoldTotaller)table[acct]).HouseDeedWorth += housedeedworth;
                                ((GoldTotaller)table[acct]).HousesWorth += housesworth;
                            }
                        }
                    }
                }

                System.Console.WriteLine("GT: Searching mobiles for PlayerVendors");
                SM(cmdmob, "GT: Searching mobiles for PlayerVendors");
                foreach (Mobile wm in World.Mobiles.Values)
                {
                    if (wm == null)
                        continue;

                    if (wm is PlayerVendor)
                    {
                        PlayerVendor pv = wm as PlayerVendor;
                        if (pv != null && pv.Owner != null)
                        {
                            Account thisacct = pv.Owner.Account as Account;
                            if (thisacct != null)
                            {
                                if ((bSupressStaff && thisacct.GetAccessLevel() > AccessLevel.Player) == false)
                                {
                                    ((GoldTotaller)table[thisacct]).VendorCount += 1;
                                    ((GoldTotaller)table[thisacct]).VendorGold += pv.HoldGold;
                                }
                            }
                        }
                    }
                }

                System.Console.WriteLine("GT: sorting");
                SM(cmdmob, "GT: sorting");
                ArrayList keys = new ArrayList(table.Keys);
                keys.Sort(new GTComparer(table));

                System.Console.WriteLine("GT: Outputting html");
                SM(cmdmob, "GT: Outputting html");

                using (StreamWriter op = new StreamWriter(OUTPUT_DIRECTORY + "/" + OUTPUT_FILE))
                {
                    op.WriteLine("<HTML><BODY>");
                    op.WriteLine("<TABLE BORDER=1 WIDTH=90%><TR>");
                    if (bSupressNames)
                    {
                        op.WriteLine("<TD>=(char count)</TD>");
                    }
                    else
                    {
                        op.WriteLine("<TD>Account (char count)</TD>");
                    }
                    op.WriteLine("<TD>Total Worth</TD>");
                    op.WriteLine("<TD>Backpack Gold</TD>");
                    op.WriteLine("<TD>Bank Gold</TD>");
                    op.WriteLine("<TD>House Gold (house count)</TD>");
                    op.WriteLine("<TD>Vendor Gold (vendor count)</TD>");
                    op.WriteLine("<TD>Returnable Deed Worth (deed count)<br>(Included house and storage upgrade deeds</TD>");
                    op.WriteLine("<TD>House(s) Worth</TD>");
                    op.WriteLine("</TR>");

                    for (int i = 0; i < keys.Count && i < maxtoprint; i++)
                    {
                        GoldTotaller gt = table[keys[i]] as GoldTotaller;
                        if (gt != null)
                        {
                            string bannedstr = "";
                            if (bIndicateBanned)
                            {
                                if (((Account)keys[i]).Banned)
                                {
                                    bannedstr = " **banned** ";
                                }
                            }
                            //System.Console.WriteLine( "Acct: {0}, total: {1}", ((Account)keys[i]).Username, gt.TotalGold );
                            if (bSupressNames)
                            {
                                op.WriteLine("<TR><TD>(" + gt.CharacterCount + ")" + bannedstr + "</TD>"); //Account
                            }
                            else
                            {
                                op.WriteLine("<TR><TD><B>" + ((Account)keys[i]).Username + "</B>(" + gt.CharacterCount + ")" + bannedstr + "</TD>"); //Account
                            }
                            op.WriteLine("<TD>" + gt.TotalGold + "</TD>"); //Total Worth
                            op.WriteLine("<TD>" + gt.BackpackGold + "</TD>"); //Backpack
                            op.WriteLine("<TD>" + gt.BankGold + "</TD>"); //bank
                            op.WriteLine("<TD>" + gt.HouseGold + " (" + gt.HouseCount + ")</TD>"); //houses
                            op.WriteLine("<TD>" + gt.VendorGold + " (" + gt.VendorCount + ")</TD>"); //vendors
                            op.WriteLine("<TD>" + gt.HouseDeedWorth + " (" + gt.HouseDeedCount + ")</TD>"); //housedeeds
                            op.WriteLine("<TD>" + gt.HousesWorth + "</TD>"); //housedeeds

                            op.WriteLine("</TR>");
                        }
                    }

                    op.WriteLine("</TABLE></BODY></HTML>");
                    op.Flush();
                    op.Close();
                }

#endif
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error in GoldTracker: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            finally
            {
                DateTime endDateTime = DateTime.Now;
                TimeSpan time = endDateTime - startDateTime;
                string endmsg = "finished in " + ((double)time.TotalMilliseconds / (double)1000) + " seconds.";
                System.Console.WriteLine(endmsg);
                SM(cmdmob, endmsg);
                bInProcess = false;
            }
        }

        private class GTComparer : System.Collections.IComparer
        {
            private Hashtable m_Table = null;

            public GTComparer(Hashtable ht)
            {
                m_Table = ht;
            }

            public int Compare(object x, object y)
            {
                if (x == y) return 0;

                GoldTotaller xg = m_Table[x] as GoldTotaller;
                GoldTotaller yg = m_Table[y] as GoldTotaller;

                if (xg == null || yg == null)
                {
                    return 0;
                }
                else
                {
                    if (xg.TotalGold == yg.TotalGold)
                    {
                        if (xg.BankGold > yg.BankGold)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else if (xg.TotalGold > yg.TotalGold)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

        }

        private class GoldTotaller
        {
            public int CharacterCount = 0;

            public int BackpackGold = 0;
            public int BankGold = 0;

            public int HouseGold = 0;
            public int HouseCount = 0;

            public int VendorGold = 0;
            public int VendorCount = 0;

            public int HouseDeedCount = 0;
            public int HouseDeedWorth = 0;

            public int HousesWorth = 0;

            public int TotalGold
            {
                get { return BackpackGold + BankGold + HouseGold + VendorGold + HouseDeedWorth + HousesWorth; }
            }

            public GoldTotaller()
            {
            }
        }

        private static ArrayList GetVendors(Mobile m)
        {
            ArrayList list = new ArrayList();

            foreach (Mobile wm in World.Mobiles.Values)
            {
                if (wm is PlayerVendor)
                {
                    PlayerVendor pv = wm as PlayerVendor;
                    if (pv != null)
                    {
                        if (pv.Owner == m)
                        {
                            list.Add(pv);
                        }
                    }
                }
            }

            return list;
        }

        private static int GetHouseDeedsInContainer(Container c, ref int count)
        {
            int iGold = 0;

            Item[] deeds = c.FindItemsByType(typeof(Server.Multis.Deeds.HouseDeed), true);
            foreach (Item i in deeds)
            {
                Server.Multis.Deeds.HouseDeed housedeed = i as Server.Multis.Deeds.HouseDeed;
                // don't count tents as they cannot be redeemed for cash
                if (housedeed != null && housedeed is SiegeTentBag == false && housedeed is TentBag == false)
                {
                    count++;
                    iGold += RealEstateBroker.ComputePriceFor(housedeed);
                }
            }

            Item[] BUCdeeds = c.FindItemsByType(typeof(BaseUpgradeContract), true);
            foreach (Item i in BUCdeeds)
            {
                BaseUpgradeContract budeed = i as BaseUpgradeContract;
                if (budeed != null)
                {
                    count++;
                    iGold += (int)budeed.Price;
                }
            }


            return iGold;
        }

        private static int GetGoldInContainer(Container c)
        {
            int iGold = 0;

            Item[] golds = c.FindItemsByType(typeof(Gold), true);
            foreach (Item g in golds)
            {
                iGold += g.Amount;
            }
            Item[] checks = c.FindItemsByType(typeof(BankCheck), true);
            foreach (Item i in checks)
            {
                BankCheck bc = (BankCheck)i;
                iGold += bc.Worth;
            }

            return iGold;
        }

        private static int GetHouseDeedWorthInHouse(BaseHouse h, ref int count)
        {
            int iGold = 0;

            ArrayList lockdowns = h.LockDowns;
            foreach (Item i in lockdowns)
            {
                if (i is Container)
                {
                    iGold += GetHouseDeedsInContainer((Container)i, ref count);
                }
                else if (i is HouseDeed && i is SiegeTentBag == false && i is TentBag == false)
                {
                    count++;
                    iGold += RealEstateBroker.ComputePriceFor((HouseDeed)i);
                }
                else if (i is BaseUpgradeContract)
                {
                    count++;
                    iGold += (int)((BaseUpgradeContract)i).Price;
                }
            }
            ArrayList secures = h.Secures;
            foreach (SecureInfo si in secures)
            {
                Container c = (Container)si.Item;
                iGold += GetHouseDeedsInContainer(c, ref count);
            }

            return iGold;
        }


        private static int GetGoldInHouse(BaseHouse h)
        {
            int iGold = 0;

            ArrayList lockdowns = h.LockDowns;
            foreach (Item i in lockdowns)
            {
                if (i is Container)
                {
                    iGold += GetGoldInContainer((Container)i);
                }
                else if (i is Gold)
                {
                    Gold gold = (Gold)i;
                    iGold += gold.Amount;
                }
                else if (i is BankCheck)
                {
                    BankCheck check = (BankCheck)i;
                    iGold += check.Worth;
                }
            }
            ArrayList secures = h.Secures;
            foreach (SecureInfo si in secures)
            {
                Container c = (Container)si.Item;
                iGold += GetGoldInContainer(c);
            }

            return iGold;
        }
    }
}
