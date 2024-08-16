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

/* Scripts/Engines/ResourcePool/ResourcePool.cs
 * ChangeLog
 *	07/19/09, plasma
 *			Added exception handler to the loading of Debts.dat
 *  7/13/07, Adam
 *      rewrite Debt hashtable saving and loading to eliminage
 *      BinaryFileReader.End() as the means for detecting the EOF.
 *      http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=127647&SiteID=1
 *		Also add a 'PlayerMobile' filter to the mobile associated with debt.
 *			When I was debugging I found 127 mobiles where only 5 were PlayerMobile. 
 *			I'm assuming the original PlayerMobile was deleted and the serial reassigned to some other creature.
 *	06/2/06, Adam
 *		Give this console a special hue
 *  04/27/05 TK
 *		Made ResourceDatas sortable, using Name as the comparator
 *	04/23/05 Pix
 *		Added GetBunchType() for use in 'vendor buy'
 *  02/24/05 TK
 *		Fixed ResourcePool.Save so that it doesn't save Deleted mobiles
 *		Fixed auto-detection algorithm for configuration data
 *  02/19/05, Adam
 *		** HACK **
 *		Added null checks to stop startup crash
 *		Commented out 'auto change detection' as it was always firing
 *  02/11/05 TK
 *		Made sure all files are closed so that backups occur correctly.
 *  02/10/05 TK
 *		Added a player==null check to prevent crashes when paying off consignments from
 *		deleted players. Paid gold goes to BountySystem.BountyKeeper.LBFund
 *  06/02/05 TK
 *		Overhauled save implementation, hopefully for last time
 *  04/02/05 TK
 *		Changed write access levels to Administrator instead of GM
 *  03/02/05 TK
 *		Added DatFilePosition element to XML configuration to allow a bit more tolerance
 *		Moved ResourceLogger code to own file
 *		Cleaned up ResourcePool [props window
 *  02/02/05 TK
 *		Revamped ResourcePool save system to use XML configuration.
 *		Removed Resource setup in ResourcePool.Configure(), made it call Load
 *		Documentation to come, don't f*ck with the config file till then ;)
 *		Added ValidFailsafe flag to prevent auto-generation of valorite ingots etc
 *  01/31/05
 *		Various changes that I forgot (small ones, no worries ;)
 *		Cleaned up OnLoad failure notifications.
 *  01/28/05 Taran Kain
 *		Changed logging system, added RDRedirect class, probably some other stuff
 *	01/24/05 Taran Kain
 *		Removed PaymentCheck and placed in own file.
 *  01/23/05 Taran Kain
 *		Version 1.0
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Engines.ResourcePool
{
    ///	<summary>
    ///	ResourceConsignment
    ///	An entry in	the	database. Implements IComparable so	that it	can	be sorted by
    ///	ArrayList.Sort().
    ///	</summary>
    public class ResourceConsignment : IComparable
    {
        private double m_Price; // price to	pay	the	player when	some are sold
        private Mobile m_Seller; //	the	player to pay
        private Type m_Type;    // type	of resource	this is
        private int m_Amount; // how many of the resource..	4000 boards, 6 cloth, etc

        public double Price { get { return m_Price; } }
        public Mobile Seller { get { return m_Seller; } }
        public Type Type { get { return m_Type; } }
        public int Amount { get { return m_Amount; } }

        public ResourceConsignment(Type type, int amount, double price, Mobile seller)
        {
            m_Price = price;
            m_Seller = seller;
            m_Type = type;
            m_Amount = amount;
        }

        public ResourceConsignment(GenericReader reader)
        {
            Deserialize(reader);
        }

        public int CompareTo(object obj)
        {
            if (obj is ResourceConsignment)
            {
                ResourceConsignment rc = (ResourceConsignment)obj;
                return m_Amount.CompareTo(((ResourceConsignment)obj).Amount);
            }

            throw new ArgumentException("object	is not ResourceConsignment");
        }

        public int ModifyAmount(int delta)
        {
            if (-delta > m_Amount)
            {
                int overflow = m_Amount + delta;
                m_Amount = 0;
                return overflow;
            }

            m_Amount += delta;

            return 0;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            // version 0
            writer.Write((double)m_Price);
            writer.Write((Mobile)m_Seller);
            writer.Write((string)m_Type.FullName);
            writer.Write((int)m_Amount);
            writer.Write((string)"ResourceConsignment");
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Price = reader.ReadDouble();
                        m_Seller = reader.ReadMobile();
                        m_Type = Type.GetType(reader.ReadString());
                        m_Amount = reader.ReadInt();
                        if (reader.ReadString() != "ResourceConsignment")
                        {
                            Console.WriteLine();
                            Console.WriteLine("**WARNING**: Deserialization stream out of sync as of ResourceConsignment");
                        }

                        break;
                    }
                default:
                    {
                        Console.WriteLine();
                        Console.WriteLine("**WARNING**: ResourceConsignment save version unrecognized: {0}", version);
                        break;
                    }
            }
        }
    }

    [PropertyObject]
    public class ResourceData : IComparable
    {
        private ArrayList m_ConsignmentList;
        private double m_WholesalePrice, m_ResalePrice;
        private Type m_Type;
        private string m_Name;
        private string m_BunchName;
        private int m_ItemID;
        private int m_Hue;
        private double m_SmallestFirstFactor;
        private bool m_ValidFailsafe;

        // return read-only	copy, otherwise	ours might not be sorted
        public virtual ResourceConsignment[] ConsignmentList { get { return (ResourceConsignment[])m_ConsignmentList.ToArray(typeof(ResourceConsignment)); } }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public virtual double WholesalePrice
        {
            get { return m_WholesalePrice; }
            set
            {
                m_WholesalePrice = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double ResalePrice
        {
            get { return m_ResalePrice; }
            set
            {
                m_ResalePrice = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Type Type { get { return m_Type; } set { m_Type = value; } }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public virtual string Name { get { return m_Name; } }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public string BunchName { get { return m_BunchName; } }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ItemID { get { return m_ItemID; } }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int Hue { get { return m_Hue; } }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool ValidFailsafe
        {
            get { return m_ValidFailsafe; }
            set { m_ValidFailsafe = value; }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double SmallestFirstFactor
        {
            get { return m_SmallestFirstFactor; }
            set
            {
                if (value >= 0 && value <= 1)
                    m_SmallestFirstFactor = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public virtual int TotalCount
        {
            get
            {
                int total = 0;
                foreach (ResourceConsignment rc in m_ConsignmentList)
                    total += rc.Amount;
                return total;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public virtual double TotalInvested
        {
            get
            {
                double total = 0;
                foreach (ResourceConsignment rc in m_ConsignmentList)
                    total += rc.Amount * rc.Price;
                return total;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public double TotalValue
        {
            get
            {
                return TotalCount * m_ResalePrice;
            }
        }

        public ResourceData(Type type, string name, string bunchname, double wholesale, double resale, int itemid, int hue, bool validfailsafe)
        {
            m_ConsignmentList = new ArrayList();
            m_Type = type;
            m_Name = name;
            m_BunchName = bunchname + " of " + name + "s";
            m_WholesalePrice = wholesale;
            m_ResalePrice = resale;
            m_ItemID = itemid;
            m_Hue = hue;
            m_SmallestFirstFactor = 0.5;
            m_ValidFailsafe = validfailsafe;
        }

        public ResourceData()
        {
        }

        public ResourceData(XmlTextReader reader, GenericReader rcreader)
        {
            m_ConsignmentList = new ArrayList();
            Load(reader, rcreader);
        }

        public int CompareTo(object obj)
        {
            if (obj is ResourceData)
            {
                ResourceData rd = (ResourceData)obj;
                return this.Name.CompareTo(rd.Name); // use this.Name instead of m_Name!
                                                     // otherwise inheritance shits bricks
            }

            throw new ArgumentException("object	is not ResourceData");
        }

        public virtual double Add(Mobile seller, int amount, bool truncprice)
        {
            return Add(seller, amount, 0, truncprice);
        }

        public virtual double Add(Mobile seller, int amount, double saleprice, bool truncprice)
        {
            if (saleprice == 0)
                saleprice = m_WholesalePrice;

            if (truncprice)
                saleprice = Math.Floor(saleprice);

            foreach (ResourceConsignment rc in m_ConsignmentList)
            {
                if (rc.Seller == seller && rc.Price == saleprice)
                {
                    rc.ModifyAmount(amount);
                    m_ConsignmentList.Sort();
                    return saleprice;
                }
            }

            m_ConsignmentList.Add(new ResourceConsignment(m_Type, amount, saleprice, seller));
            m_ConsignmentList.Sort();
            return saleprice;
        }

        public ResourceConsignment[] Remove(int amount)
        {
            int newbie = (int)Math.Ceiling(amount * m_SmallestFirstFactor),
                communal = amount - newbie;
            ArrayList topay = new ArrayList();

            if (amount > TotalCount)
                return null;

            for (int i = 0; i < m_ConsignmentList.Count && newbie > 0; i++)
            {
                ResourceConsignment rc = (ResourceConsignment)m_ConsignmentList[i];

                int delta = rc.ModifyAmount(-newbie);

                if (delta == -newbie)
                    continue;

                topay.Add(new ResourceConsignment(rc.Type, newbie + delta,
                    rc.Price, rc.Seller));

                newbie = -delta;
            }

            for (int i = 0; i < m_ConsignmentList.Count && communal > 0; i++)
            {
                ResourceConsignment rc = (ResourceConsignment)m_ConsignmentList[i];
                int toremove = communal / (m_ConsignmentList.Count - i);
                if (toremove == 0)
                    toremove = 1;

                int delta = rc.ModifyAmount(-toremove);

                if (delta == -toremove)
                    continue;

                topay.Add(new ResourceConsignment(rc.Type, toremove + delta,
                    rc.Price, rc.Seller));

                communal -= (toremove + delta);
            }

            while (m_ConsignmentList.Count > 0 && ((ResourceConsignment)m_ConsignmentList[0]).Amount == 0)
                m_ConsignmentList.RemoveAt(0);

            return (ResourceConsignment[])topay.ToArray(typeof(ResourceConsignment));
        }

        public string DescribeInvestment(Mobile m)
        {
            int total = 0;
            double avgprice = 0;

            foreach (ResourceConsignment rc in m_ConsignmentList)
            {
                if (rc.Seller != m)
                    continue;

                avgprice = (avgprice * total) + (rc.Price * rc.Amount);
                total += rc.Amount;
                avgprice /= total;
            }

            if (total == 0)
                return "None";
            return total + " at avg " + avgprice.ToString("F") + "gp ea.";
        }

        public virtual void Save(XmlTextWriter writer, GenericWriter rcwriter)
        {
            writer.WriteStartElement("ResourceData");
            writer.WriteAttributeString("version", "0");

            // add new versions INSIDE try block
            // if WriteStartElement fails, we don't want WriteEndElement processed
            // but if it succeeds, WriteEndElement MUST be processed
            // any other code outside try block could exception before finally block
            // failing to do this and causing an exception will corrupt all RP data
            try
            {
                // version 0
                writer.WriteElementString("Name", m_Name);
                writer.WriteElementString("Type", m_Type.FullName);
                writer.WriteElementString("BunchName", m_BunchName);
                writer.WriteElementString("ResalePrice", m_ResalePrice.ToString("R"));
                writer.WriteElementString("WholesalePrice", m_WholesalePrice.ToString("R"));
                writer.WriteElementString("ItemID", m_ItemID.ToString());
                writer.WriteElementString("Hue", m_Hue.ToString());
                writer.WriteElementString("SmallestFirstFactor", m_SmallestFirstFactor.ToString("R"));
                writer.WriteElementString("ValidFailsafe", m_ValidFailsafe.ToString());

                //rcwriter.Write((string)m_Type.FullName);
                //rcwriter.Write((int)m_ConsignmentList.Count);
                foreach (ResourceConsignment rc in m_ConsignmentList)
                    rc.Serialize(rcwriter);
            }
            // ALWAYS LAST
            finally { writer.WriteEndElement(); }
        }

        private bool FindPosition(GenericReader rcreader)
        {
            ((BinaryFileReader)rcreader).Seek(0, SeekOrigin.Begin);
            if (rcreader.End())
                return false;

            int rcversion = rcreader.ReadInt();
            switch (rcversion)
            {
                case 0:
                    {
                        long tableposition = rcreader.ReadLong();
                        Console.WriteLine("Tableposition: {0}", tableposition);
                        ((BinaryFileReader)rcreader).Seek(tableposition, SeekOrigin.Begin);
                        while (!rcreader.End())
                        {
                            string typename = rcreader.ReadString();
                            long location = rcreader.ReadLong();
                            Console.WriteLine("{0}, {1}", typename, location);
                            if (typename == m_Type.FullName)
                            {
                                ((BinaryFileReader)rcreader).Seek(location, SeekOrigin.Begin);
                                return true;
                            }
                        }

                        break;
                    }
                default:
                    {
                        throw new Exception("ResourcePool error: Invalid consignments.dat save version");
                    }
            }

            return false;
        }

        public virtual void Load(XmlTextReader reader, GenericReader rcreader)
        {
            int version = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("ResourceData");

            switch (version)
            {
                case 0:
                    {
                        m_Name = reader.ReadElementString("Name");
                        m_Type = Type.GetType(reader.ReadElementString("Type"));
                        m_BunchName = reader.ReadElementString("BunchName");
                        m_ResalePrice = Double.Parse(reader.ReadElementString("ResalePrice"));
                        m_WholesalePrice = Double.Parse(reader.ReadElementString("WholesalePrice"));
                        m_ItemID = Int32.Parse(reader.ReadElementString("ItemID"));
                        m_Hue = Int32.Parse(reader.ReadElementString("Hue"));
                        m_SmallestFirstFactor = Double.Parse(reader.ReadElementString("SmallestFirstFactor"));
                        m_ValidFailsafe = bool.Parse(reader.ReadElementString("ValidFailsafe"));

                        if (m_Type == null || m_Type.GetInterface("ICommodity", false) == null)
                            throw new Exception(String.Format("Error: Type of resource '{0}' does not exist or does not implement ICommodity.", m_Name));

                        m_ConsignmentList = new ArrayList();

                        if (rcreader == null)// || !FindPosition(rcreader))
                            break;

                        ((BinaryFileReader)rcreader).Seek(0, SeekOrigin.Begin);
                        if (rcreader.End())
                            break;
                        int rcversion = rcreader.ReadInt();
                        switch (rcversion)
                        {
                            case 0:
                                {
                                    while (!rcreader.End())
                                    {
                                        ResourceConsignment rc = new ResourceConsignment(rcreader);
                                        if (rc.Type == m_Type)
                                            m_ConsignmentList.Add(rc);
                                    }

                                    break;
                                }
                            default:
                                throw new Exception("ResourcePool error: Invalid file version for Consignments.dat");
                        }
                        /*if (!rcreader.End() && rcreader.ReadString() == m_Type.FullName)
								{
									int rcversion = rcreader.ReadInt();
									switch (rcversion)
									{
										case 0:
										{
											count = rcreader.ReadInt();
											for (; count > 0; count--)
												m_ConsignmentList.Add(new ResourceConsignment(rcreader));
											break;
										}
										default:
										{
											throw new Exception("ResourcePool error: Invalid ResourceData entry save version in Consignments.dat");
										}
									}
								}*/

                        break;
                    }
                default:
                    {
                        throw new Exception("Error loading ResourceData: Invalid saveversion");
                    }
            }

            reader.ReadEndElement();
        }
    }

    [PropertyObject]
    public class RDRedirect : ResourceData
    {
        private ResourceData m_Redirect;
        private double m_AmountFactor;
        private double m_PriceFactor;
        private string m_Message;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double AmountFactor
        {
            get { return m_AmountFactor; }
            set { m_AmountFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double PriceFactor
        {
            get { return m_PriceFactor; }
            set { m_PriceFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public string Message
        {
            get { return m_Message; }
            set { m_Message = value; }
        }

        public override double WholesalePrice
        {
            get { return m_Redirect.WholesalePrice * m_PriceFactor * m_AmountFactor; }
        }

        public override string Name
        {
            get { return m_Redirect.Name; }
        }

        public override int TotalCount
        {
            get { return m_Redirect.TotalCount; }
        }

        public RDRedirect(Type mytype, Type t, double amtfact, double pricefact, string message)
        {
            this.Type = mytype;
            m_Redirect = (ResourceData)ResourcePool.Resources[t];
            m_AmountFactor = amtfact;
            m_PriceFactor = pricefact;
            m_Message = message;
        }

        public RDRedirect(XmlTextReader reader, GenericReader rcreader)
        {
            Load(reader, rcreader);
        }

        public override double Add(Mobile seller, int amount, bool truncprice)
        {
            seller.SendMessage(m_Message);
            return m_Redirect.Add(seller, (int)(amount * m_AmountFactor), m_Redirect.WholesalePrice * m_PriceFactor, truncprice);
        }

        public override double Add(Mobile seller, int amount, double price, bool truncprice)
        {
            seller.SendMessage(m_Message);
            return m_Redirect.Add(seller, (int)(amount * m_AmountFactor), price * m_PriceFactor, truncprice);
        }

        public override void Save(XmlTextWriter writer, GenericWriter rcwriter)
        {
            writer.WriteStartElement("RDRedirect");
            writer.WriteAttributeString("version", "0");

            // new versions go inside try block, see ResourceData.Save for why
            try
            {
                // version 0
                writer.WriteElementString("AmountFactor", m_AmountFactor.ToString("R"));
                writer.WriteElementString("PriceFactor", m_PriceFactor.ToString("R"));
                writer.WriteElementString("Redirect", m_Redirect.Type.FullName);
                writer.WriteElementString("Message", m_Message);
                writer.WriteElementString("Type", this.Type.FullName);
            }
            // always last
            finally { writer.WriteEndElement(); }
        }

        public override void Load(XmlTextReader reader, GenericReader rcreader)
        {
            int version = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("RDRedirect");

            switch (version)
            {
                case 0:
                    {
                        m_AmountFactor = Double.Parse(reader.ReadElementString("AmountFactor"));
                        m_PriceFactor = Double.Parse(reader.ReadElementString("PriceFactor"));
                        string redir = reader.ReadElementString("Redirect");
                        m_Redirect = (ResourceData)ResourcePool.Resources[Type.GetType(redir)];
                        m_Message = reader.ReadElementString("Message");

                        string t = reader.ReadElementString("Type");
                        this.Type = Type.GetType(t);

                        if (m_Redirect == null)
                            throw new Exception(String.Format("Error: Resource '{0}' did not exist at creation time of Redirector '{1}'", redir, t));

                        break;
                    }
                default:
                    {
                        throw new Exception("Error loading RDRedirect: Invalid save version");
                    }
            }

            reader.ReadEndElement();
        }

        public override ResourceConsignment[] ConsignmentList { get { return new ResourceConsignment[0]; } }
        public override double TotalInvested { get { return -1; } }
    }

    ///	<summary>
    ///	
    ///	</summary>
    public class ResourcePool : Item
    {
        private static Hashtable m_Resources;
        private static double m_PaymentFactor; //	the	player gets this cut of the sale
        private static double m_FailsafePriceHike; // price multiplier for when the vendor sells spawned resources
        private static Hashtable m_Debts;

        private static DateTime m_LastModified = DateTime.MinValue;

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceDataList ResourceDatas { get { return new ResourceDataList(); } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirectList RDRedirects { get { return new RDRedirectList(); } set { } }

        public static Hashtable Resources { get { return m_Resources; } }
        public static Hashtable Debts { get { return m_Debts; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public static double PaymentFactor
        {
            get { return m_PaymentFactor; }
            set { m_PaymentFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public static double FailsafePriceHike
        {
            get { return m_FailsafePriceHike; }
            set { m_FailsafePriceHike = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public static int LogLevel
        {
            get { return ResourceLogger.LogLevel; }
            set { ResourceLogger.LogLevel = value; }
        }

        public static Type GetBunchType(Type t)
        {
            if (t == typeof(Board))
            {
                return typeof(BunchBoard);
            }
            else if (t == typeof(Arrow))
            {
                return typeof(BunchArrow);
            }
            else if (t == typeof(Bolt))
            {
                return typeof(BunchBolt);
            }
            else if (t == typeof(Shaft))
            {
                return typeof(BunchShaft);
            }
            else if (t == typeof(Feather))
            {
                return typeof(BunchFeather);
            }
            else if (t == typeof(Cloth))
            {
                return typeof(BunchCloth);
            }
            else if (t == typeof(Leather))
            {
                return typeof(BunchLeather);
            }
            else if (t == typeof(SpinedLeather))
            {
                return typeof(BunchSpinedLeather);
            }
            else if (t == typeof(HornedLeather))
            {
                return typeof(BunchHornedLeather);
            }
            else if (t == typeof(BarbedLeather))
            {
                return typeof(BunchBarbedLeather);
            }
            else if (t == typeof(IronIngot))
            {
                return typeof(BunchIronIngot);
            }
            else if (t == typeof(DullCopperIngot))
            {
                return typeof(BunchDullCopperIngot);
            }
            else if (t == typeof(ShadowIronIngot))
            {
                return typeof(BunchShadowIronIngot);
            }
            else if (t == typeof(CopperIngot))
            {
                return typeof(BunchCopperIngot);
            }
            else if (t == typeof(BronzeIngot))
            {
                return typeof(BunchBronzeIngot);
            }
            else if (t == typeof(GoldIngot))
            {
                return typeof(BunchGoldIngot);
            }
            else if (t == typeof(AgapiteIngot))
            {
                return typeof(BunchAgapiteIngot);
            }
            else if (t == typeof(VeriteIngot))
            {
                return typeof(BunchVeriteIngot);
            }
            else if (t == typeof(ValoriteIngot))
            {
                return typeof(BunchValoriteIngot);
            }

            return null;
        }

        /*		[CommandProperty(AccessLevel.Administrator)]
				public static string ReloadConfig
				{
					get { return "Re-init RP database"; }
					set
					{
						Load(true);
						//BroadcastMessage( AccessLevel.Counselor, 0, "[ResourcePool]: Reloading XML configuration...");
					}
				}
		/*		[CommandProperty(AccessLevel.Administrator)]
				public static string ReloadAll
				{
					get { return "Reload ALL RP data"; }
					set
					{
						Load(false);
						BroadcastMessage( AccessLevel.Counselor, 0, "[ResourcePool]: Reloading database...");
					}
				}*/

        public static string GetName(Type type)
        {
            return ((ResourceData)(m_Resources[type])).Name;
        }

        public static string GetBunchName(Type type)
        {
            return ((ResourceData)(m_Resources[type])).BunchName;
        }

        public static double GetResalePrice(Type type)
        {
            return ((ResourceData)(m_Resources[type])).ResalePrice;
        }

        public static double GetWholesalePrice(Type type)
        {
            return ((ResourceData)(m_Resources[type])).WholesalePrice;
        }

        public static int GetItemID(Type type)
        {
            return ((ResourceData)(m_Resources[type])).ItemID;
        }

        public static int GetHue(Type type)
        {
            return ((ResourceData)(m_Resources[type])).Hue;
        }

        public static int GetTotalCount(Type type)
        {
            return ((ResourceData)(m_Resources[type])).TotalCount;
        }

        public static bool GetValidFailsafe(Type type)
        {
            return ((ResourceData)(m_Resources[type])).ValidFailsafe;
        }

        public static bool IsPooledResource(Type type)
        {
            if (Core.UOSP) return false;

            if (type == null)
                return false;

            return (m_Resources[type] != null);
        }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);

            m_Resources = new Hashtable();
            m_Debts = new Hashtable();
            m_PaymentFactor = 1;
            m_FailsafePriceHike = 1;

            Load(true); // this way the vendors won't f up when loading
        }

        public static bool SellOff(Type type, int amount, Serial vendorid, Mobile player)
        {
            if (((ResourceData)m_Resources[type]).TotalCount < amount)
            {
                return false;
            }

            ResourceTransaction rt = new ResourceTransaction(TransactionType.Purchase);
            rt.Amount = amount;
            rt.Price = ((ResourceData)m_Resources[type]).ResalePrice;
            rt.ResName = ((ResourceData)m_Resources[type]).Name;
            rt.VendorID = vendorid;

            ResourceConsignment[] topay = ((ResourceData)m_Resources[type]).Remove(amount);

            rt.NewAmount = ((ResourceData)m_Resources[type]).TotalCount;

            foreach (ResourceConsignment rc in topay)
            {
                if (rc.Seller == null || rc.Seller.Deleted)
                {
                    BountySystem.BountyKeeper.LBFund += (int)(rc.Amount * rc.Price * m_PaymentFactor);
                    continue;
                }

                // do this here instead of when writing check so player may know how much is waiting
                ResourceTransaction rtpay = new ResourceTransaction(TransactionType.Payment);
                rtpay.Amount = rc.Amount;
                rtpay.Price = rc.Price * m_PaymentFactor;
                rtpay.ResName = ((ResourceData)m_Resources[type]).Name;
                rtpay.VendorID = vendorid;
                ResourceLogger.Add(rtpay, rc.Seller);

                if (m_Debts[rc.Seller] == null)
                    m_Debts[rc.Seller] = (double)rc.Amount * rc.Price * m_PaymentFactor;
                else
                    m_Debts[rc.Seller] = (double)rc.Amount * rc.Price * m_PaymentFactor + (double)m_Debts[rc.Seller];
            }

            Mobile[] keys = new Mobile[m_Debts.Count];
            m_Debts.Keys.CopyTo(keys, 0);
            foreach (Mobile m in keys)
            {
                double payment = (double)m_Debts[m];
                if (payment > 1)
                {
                    Container bank = m.BankBox;

                    PaymentCheck check = (PaymentCheck)bank.FindItemByType(typeof(PaymentCheck), false);
                    if (check != null)
                    {
                        check.Worth += (int)payment;
                        m_Debts[m] = (double)m_Debts[m] - (int)payment;
                    }
                    else
                    {
                        check = new PaymentCheck((int)payment);
                        if (bank.Items.Count < 125)
                        {
                            check.SetLastMoved();
                            bank.DropItem(check);
                            m_Debts[m] = (double)m_Debts[m] - (int)payment;
                        }
                    }
                }
                if ((double)m_Debts[m] <= 0)
                    m_Debts.Remove(m);
            }

            ResourceLogger.Add(rt, player);

            return true;
        }

        public static double AddToPool(Mobile seller, Type type, int amount, bool truncateprice, Serial vendorid)
        {
            if (m_Resources[type] == null)
            {
                return -1;
            }

            ResourceTransaction rt = new ResourceTransaction(TransactionType.Sale);
            rt.Amount = amount;
            rt.ResName = ((ResourceData)m_Resources[type]).Name;
            rt.VendorID = vendorid;

            double ret = ((ResourceData)m_Resources[type]).Add(seller, amount, truncateprice);

            rt.NewAmount = ((ResourceData)m_Resources[type]).TotalCount;
            rt.Price = ret;

            ResourceLogger.Add(rt, seller);

            return ret;
        }

        public static void Save(WorldSaveEventArgs args)
        {
            Console.WriteLine("Resource Pool Saving...");

            if (!Directory.Exists("Saves/ResourcePool"))
                Directory.CreateDirectory("Saves/ResourcePool");

            XmlTextWriter writer = new XmlTextWriter("Saves/ResourcePool/config.xml", System.Text.Encoding.Default);
            writer.Formatting = Formatting.Indented;
            BinaryFileWriter rcwriter = new BinaryFileWriter(new FileStream("Saves/ResourcePool/Consignments.dat", FileMode.Create, FileAccess.Write), true);
            BinaryFileWriter dwriter = new BinaryFileWriter(new FileStream("Saves/ResourcePool/Debts.dat", FileMode.Create, FileAccess.Write), true);

            writer.WriteStartDocument(true);
            writer.WriteStartElement("ResourcePool");
            writer.WriteAttributeString("version", "1");

            try
            {
                // VERSION 1
                writer.WriteElementString("PaymentFactor", m_PaymentFactor.ToString("R"));
                writer.WriteElementString("FailsafePriceHike", m_FailsafePriceHike.ToString("R"));

                //Hashtable RDTable = new Hashtable();
                rcwriter.Write((int)0); // version
                                        //rcwriter.Write((long)0); // placeholder tableposition

                foreach (Type t in m_Resources.Keys)
                {
                    if (m_Resources[t] is RDRedirect || !(m_Resources[t] is ResourceData))
                        continue;

                    //RDTable[t.FullName] = rcwriter.Position;
                    //Console.WriteLine("{0} {1}", t.FullName, rcwriter.Position);
                    ((ResourceData)m_Resources[t]).Save(writer, rcwriter);
                }

                foreach (Type t in m_Resources.Keys)
                {
                    if (!(m_Resources[t] is RDRedirect))
                        continue;

                    //RDTable[t.FullName] = rcwriter.Position;
                    //Console.WriteLine("{0} {1}", t.FullName, rcwriter.Position);
                    ((RDRedirect)m_Resources[t]).Save(writer, rcwriter);
                }

                /*long pos = rcwriter.Position;
				foreach (string key in RDTable.Keys)
				{
					rcwriter.Write((string)key);
					rcwriter.Write((long)RDTable[key]);
					Console.WriteLine("Writing {0} {1}", key, (long)RDTable[key]);
				}
				rcwriter.UnderlyingStream.Seek(0, SeekOrigin.Begin);
				rcwriter.Write((int)0); // REAL version
				rcwriter.Write((long)pos); // REAL tableposition
				Console.WriteLine("Writing tableposition {0}", pos);*/

                // version 1 change
                //	we used to read until EOF, but there is a bug with that.
                //	so now we write the number of hashtable elements written
                // http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=127647&SiteID=1

                // count valid elements
                int count = 0;
                foreach (Mobile m in m_Debts.Keys)
                {   // mobile serials can get reassigned to non PlayerMobiles
                    if (m == null || m.Deleted || m is PlayerMobile == false)
                        continue;
                    count++;
                }

                // write count
                dwriter.Write(count);

                // write elements
                foreach (Mobile m in m_Debts.Keys)
                {
                    if (m == null || m.Deleted || m is PlayerMobile == false)
                        continue;
                    dwriter.Write((Mobile)m);
                    dwriter.Write((double)m_Debts[m]);
                }
            }

            finally { writer.WriteEndDocument(); }

            BinaryFileWriter rpw = new BinaryFileWriter(new FileStream("Saves/ResourcePool/ResourcePool.dat", FileMode.Create, FileAccess.Write), true);
            rpw.Write((int)0); // version
            rpw.Write((DateTime)m_LastModified);
            rpw.Close();

            writer.Close();
            rcwriter.Close();
            dwriter.Close();
        }

        public static void Load()
        {
            Load(false);
        }

        public static void Load(bool initonly)
        {
            if (initonly)
                Console.WriteLine("Initializing ResourcePool database...");
            else
                Console.WriteLine("Resource Pool Loading...");

            if (!Directory.Exists("Saves/ResourcePool"))
                Directory.CreateDirectory("Saves/ResourcePool");

            bool newconfig = false;
            FileStream rpfs = new FileStream("Saves/ResourcePool/ResourcePool.dat", FileMode.OpenOrCreate, FileAccess.Read);
            BinaryFileReader rpreader = new BinaryFileReader(new BinaryReader(rpfs));
            if (!rpreader.End())
            {
                int rpversion = rpreader.ReadInt();
                switch (rpversion)
                {
                    case 0:
                        {
                            m_LastModified = rpreader.ReadDateTime();
                            FileInfo fi = new FileInfo("Data/ResourcePool/config.xml");
                            if (fi.LastWriteTime != m_LastModified)
                            {
                                m_LastModified = fi.LastWriteTime;
                                newconfig = true;
                            }
                            else
                                newconfig = false;

                            break;
                        }
                    default:
                        throw new Exception("Error loading ResourcePool: Invalid ResourcePool.dat save version");
                }
            }
            else
            {
                m_LastModified = (new FileInfo("Data/ResourcePool/config.xml")).LastWriteTime;
                Console.WriteLine("Warning: Saves/ResourcePool/ResourcePool.dat not found.");
                newconfig = true;
            }

            if (newconfig)
                Console.WriteLine("New configuration detected! Reading from Data/...");
            XmlTextReader reader;
            reader = new XmlTextReader((newconfig ? "Data/ResourcePool/config.xml" : "Saves/ResourcePool/config.xml"));
            reader.WhitespaceHandling = WhitespaceHandling.None;

            FileStream rcfs = new FileStream("Saves/ResourcePool/Consignments.dat", FileMode.OpenOrCreate, FileAccess.Read);
            FileStream dfs = new FileStream("Saves/ResourcePool/Debts.dat", FileMode.OpenOrCreate, FileAccess.Read);
            BinaryFileReader rcreader = new BinaryFileReader(new BinaryReader(rcfs));
            BinaryFileReader dreader = new BinaryFileReader(new BinaryReader(dfs));

            try { reader.MoveToContent(); }
            catch
            {
                Console.WriteLine("Save xml data not found or invalid, reverting to defaults");
                reader = new XmlTextReader("Data/ResourcePool/config.xml");
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();
            }

            int version = (int)double.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("ResourcePool");

            switch (version)
            {
                case 0:
                    {
                        m_PaymentFactor = Double.Parse(reader.ReadElementString("PaymentFactor"));
                        m_FailsafePriceHike = Double.Parse(reader.ReadElementString("FailsafePriceHike"));

                        m_Resources = new Hashtable();
                        while (reader.LocalName == "ResourceData")
                        {
                            ResourceData rd = new ResourceData(reader, (initonly ? null : rcreader));
                            if (rd != null)
                                m_Resources[rd.Type] = rd;
                        }
                        while (reader.LocalName == "RDRedirect")
                        {
                            RDRedirect rd = new RDRedirect(reader, (initonly ? null : rcreader));
                            if (rd != null)
                                m_Resources[rd.Type] = rd;
                        }

                        m_Debts = new Hashtable();

                        if (initonly)
                            break;

                        while (!dreader.End())
                        {
                            try
                            {
                                Mobile m = dreader.ReadMobile();
                                double debt = dreader.ReadDouble();
                                if (m != null)
                                    m_Debts[m] = debt;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception occured attempting to desrialize from Debts.dat : " + ex.Message);
                                break;
                            }

                        }

                        break;
                    }

                case 1:
                    {
                        m_PaymentFactor = Double.Parse(reader.ReadElementString("PaymentFactor"));
                        m_FailsafePriceHike = Double.Parse(reader.ReadElementString("FailsafePriceHike"));

                        m_Resources = new Hashtable();
                        while (reader.LocalName == "ResourceData")
                        {
                            ResourceData rd = new ResourceData(reader, (initonly ? null : rcreader));
                            if (rd != null)
                                m_Resources[rd.Type] = rd;
                        }
                        while (reader.LocalName == "RDRedirect")
                        {
                            RDRedirect rd = new RDRedirect(reader, (initonly ? null : rcreader));
                            if (rd != null)
                                m_Resources[rd.Type] = rd;
                        }

                        m_Debts = new Hashtable();

                        if (initonly)
                            break;

                        // read count
                        int count = dreader.ReadInt();

                        for (int ix = 0; ix < count; ix++)
                        {
                            Mobile m = dreader.ReadMobile();
                            double debt = dreader.ReadDouble();
                            if (m != null)
                                m_Debts[m] = debt;
                        }

                        break;
                    }

                default:
                    {
                        throw new Exception("Invalid ResourcePool save version.");
                    }
            }

            reader.ReadEndElement();
            reader.Close();
            rcfs.Close();
            rpfs.Close();
            dfs.Close();
        }
        /******************* END STATIC STUFF; NOW FOR ITEM STUFF *************/

        [Constructable]
        public ResourcePool()
            : base(0x1F14)
        {
            Name = "Resource Pool";
            Weight = 1.0;
            Hue = 0x53;
        }

        public ResourcePool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}
