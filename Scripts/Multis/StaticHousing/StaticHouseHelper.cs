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

/* Scripts/Multis/StaticHousing/StaticHouseHelper.cs
 *  Changelog:
 *  9/16/2024, Adam
 *      Since we're not building houses on TC ATM, lets allow players to play with prod houses
 *  9/15/2024, Adam (GetDescription)
 *      Add GetDescription()
 *	12/28/07 Taran Kain
 *		Added Doubled list and flag for doubled-up tiles
 *		Added FixerAddon object to add them back into a house after being filtered out
 *	8/11/07, Adam
 *		Replace 10000000 with PriceError constant
 *	8/2/07, Adam
 *		Add calls to new Assert() function to track down the Exceptions we are seeing:
 *			Log start : 8/1/2007 1:36:43 AM
 *			Botched Static Housing xml.
 *			Object reference not set to an instance of an object.
 *			at Server.Multis.StaticHousing.StaticHouseHelper.TransferData(StaticHouse sh)
 *		I'll remove these calls later, and my hunch is that house.Region is null.
 *	6/25/07, Adam
 *      - Major changes, please SR/MR for full details
 *		- Add new nodes to the recognized XML node list
 *  6/22/07, Adam
 *      Add BasePrice calculation function (based on plot size only)
 *  6/11/07, Pix
 *      Added GetAllStaticHouseDescriptions() and StaticHouseDescription class to help
 *      the architect get the deeds/etc for all the available houses.
 *  06/08/2007, plasma
 *      Initial creation
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Server.Multis.StaticHousing
{
    /// <summary>
    /// Hosts functions to read/write/lookup house blueprints
    /// </summary>
    public class StaticHouseHelper
    {
        public enum Error
        {
            PriceError = 10000000,   // 10 million if error
        }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);

        }

        [Usage("ReloadBlueprintCache")]
        [Description("Reload the blueprint cache.")]
        private static void OnReloadBlueprintCache(CommandEventArgs e)
        {
            m_BlueprintList.Clear();
            OnLoad();
        }

        public static void OnLoad()
        {
            Console.WriteLine("Structure blueprints Loading...");

            try
            {
                XmlDocument doc = OpenDoc();
                if (doc != null)
                {
                    foreach (XmlElement element in doc["StaticHousing"])
                    {
                        if (element.Name == "HouseID")
                        {
                            try
                            {
                                if (!AppendBlueprint(CreateBlueprint(element)))
                                {
                                    try { throw new ApplicationException("OnLoad() : AppendBlueprint() : failed"); }
                                    catch (Exception e)
                                    {
                                        LogHelper.LogException(e, "Possible bad element in static house xml: element[id].");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                LogHelper.LogException(e, "Possible bad element in static house xml: element[id].");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e, "Botched Static Housing xml.");
            }
        }

        [Flags]
        public enum TileType
        {
            Normal = 0x00,
            OutsideRect = 0x01,     // used for tiles outside the house bounding box/plot; such as steps
            Overlapped = 0x02,      // used for tiles that are at the same x/y/z (will sometimes fail to display)
            Patch = 0x04,       // used for tiles that fail to display and should be added back in as an perma-addon
        }

        public static void Initialize()
        {
            Server.Commands.TargetCommands.Register(new ToAddon());
            Server.CommandSystem.Register("WipeRegions", AccessLevel.Administrator, new CommandEventHandler(OnWipeRegions));
            Server.CommandSystem.Register("AddRegion", AccessLevel.Administrator, new CommandEventHandler(OnAddRegion));
            Server.CommandSystem.Register("DumpRegions", AccessLevel.Administrator, new CommandEventHandler(OnDumpRegions));
            Server.CommandSystem.Register("ReloadBlueprintCache", AccessLevel.Administrator, new CommandEventHandler(OnReloadBlueprintCache));
        }

        public class ToAddon : BaseCommand
        {
            private int m_found;
            private int m_processed;

            public ToAddon()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Area | CommandSupport.Single;
                Commands = new string[] { "ToAddon" };
                ObjectTypes = ObjectTypes.Items;

                Usage = "ToAddon";
                Description = "Converts the targeted item(s) to an item of type AddonComponent";
            }

            public override void Begin(CommandEventArgs e)
            {
                m_found = 0;
                m_processed = 0;
            }

            public override void End(CommandEventArgs e)
            {
                AddResponse(String.Format("{0} items processed, {1} objects found.", m_processed, m_found));
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                try
                {
                    Item item = obj as Item;
                    m_found++;
                    if (item != null)
                    {
                        m_processed++;
                        AddonComponent ac = new AddonComponent(item.ItemID);
                        ac.MoveToWorld(item.Location, item.Map);
                        item.Delete();
                    }

                    AddResponse(String.Format("done."));
                }
                catch (Exception exe)
                {
                    LogHelper.LogException(exe);
                    e.Mobile.SendMessage(exe.Message);
                }
            }
        }

        [Usage("WipeRegion")]
        [Description("Wipes all rectangles that make up this house's HousingRegion.")]
        private static void OnWipeRegions(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Wipe regions of which house?");
            e.Mobile.SendMessage("Please target the house sign.");
            e.Mobile.Target = new WipeRegionTarget();
        }

        public class WipeRegionTarget : Target
        {

            public WipeRegionTarget()
                : base(8, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign && (target as HouseSign).Structure != null)
                {
                    HouseSign sign = target as HouseSign;

                    if (sign.Structure is StaticHouse == false)
                    {
                        from.SendMessage("You may only wipe regions on a StaticHouse.");
                        return;
                    }

                    StaticHouse sh = sign.Structure as StaticHouse;
                    sh.AreaList.Clear();
                    sh.UpdateRegionArea();

                    from.SendMessage("All regions for thie StaticHouse have been wiped.");
                }
                else
                {
                    from.SendMessage("That is not a house sign.");
                }
            }
        }

        [Usage("AddRegion")]
        [Description("Adds a rectangle to this house's HousingRegion.")]
        private static void OnAddRegion(CommandEventArgs e)
        {
            BaseHouse bh = BaseHouse.FindHouseAt(e.Mobile);
            if (bh == null)
            {
                e.Mobile.SendMessage("You must be standing in the house you are defining regions for.");
                return;
            }
            if (bh is StaticHouse == false)
            {
                e.Mobile.SendMessage("You may only define regions for static houses.");
                return;
            }
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(AddRegionCallback), bh);
        }

        private static void AddRegionCallback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            if (state is StaticHouse == false)
                return;

            StaticHouse sh = state as StaticHouse;

            // normalize bounding rect
            if (start.X > end.X)
            {
                int ix = start.X;
                start.X = end.X;
                end.X = ix;
            }
            if (start.Y > end.Y)
            {
                int iy = start.Y;
                start.Y = end.Y;
                end.Y = iy;
            }

            int x = 0;
            int y = 0;

            /*MultiComponentList mcl = sh.Components;
			int x = sh.X + mcl.Min.X;
			int y = sh.Y + mcl.Min.Y;*/

            x = start.X - sh.X;
            y = start.Y - sh.Y;

            Rectangle2D temp = new Rectangle2D(start, end);
            Rectangle2D rectangle = new Rectangle2D(x, y, temp.Width, temp.Height);

            sh.AreaList.Add(rectangle);
            sh.UpdateRegionArea();
        }

        [Usage("DumpRegions")]
        [Description("Dumps the regions for this house.")]
        private static void OnDumpRegions(CommandEventArgs e)
        {
            BaseHouse bh = BaseHouse.FindHouseAt(e.Mobile);
            if (bh == null)
            {
                e.Mobile.SendMessage("You must be standing in the house you wish to dump regions for.");
                return;
            }
            if (bh is StaticHouse == false)
            {
                e.Mobile.SendMessage("You may only dump regions for static houses.");
                return;
            }

            StaticHouse sh = bh as StaticHouse;
            if (sh.AreaList.Count == 0)
                e.Mobile.SendMessage("There are no regions for this house.");
            else
            {
                e.Mobile.SendMessage("--- Area ---");

                foreach (Rectangle2D rx in sh.AreaList)
                    e.Mobile.SendMessage(rx.ToString());

                e.Mobile.SendMessage("--- Regions ---");

                Region r = sh.Region;
                if (r == null || r.Coords == null || r.Coords.Count == 0)
                    return;

                ArrayList c = r.Coords;

                for (int i = 0; i < c.Count; i++)
                {
                    if (c[i] is Rectangle2D)
                        e.Mobile.SendMessage(((Rectangle2D)c[i]).ToString());
                }
            }
        }

        /// <summary>
        /// Data structure for holding a house blueprint
        /// </summary>
        private class HouseBlueprint
        {
            private string m_ID;
            private int m_Width;
            private int m_Height;
            private ArrayList m_Multis;             //Arraylist of MultiStructs
            private ArrayList m_Deco;               //Arraylist of deco tiles (steps)
            private ArrayList m_PatchTiles;            //Arraylist of doubled-up tiles (for FixerAddon)
            private ArrayList m_Region;             //Arraylist of rects that makup the HousingRegion
            private string m_Description;
            private int m_Price;
            private string m_OriginalOwnerName;
            private double m_Version;
            private DateTime m_Capture;
            private string m_OriginalOwnerAccount;
            private Serial m_OriginalOwnerSerial;
            //private string m_SignLocation;		// not used
            private int m_SignHangerGraphic;
            private int m_SignpostGraphic;

            public double Version
            {
                get { return m_Version; }
                set { m_Version = value; }
            }
            public DateTime Capture
            {
                get { return m_Capture; }
                set { m_Capture = value; }
            }
            public string OriginalOwnerAccount
            {
                get { return m_OriginalOwnerAccount; }
                set { m_OriginalOwnerAccount = value; }
            }
            public Serial OriginalOwnerSerial
            {
                get { return m_OriginalOwnerSerial; }
                set { m_OriginalOwnerSerial = value; }
            }
            // not used
            //public string m_SignLocation;
            public ArrayList Region
            {
                get { return m_Region; }
                set { if (value != null) m_Region = value; }
            }
            public int SignHangerGraphic
            {
                get { return m_SignHangerGraphic; }
                set { m_SignHangerGraphic = value; }
            }
            public int SignpostGraphic
            {
                get { return m_SignpostGraphic; }
                set { m_SignpostGraphic = value; }
            }
            public string ID
            {
                get { return m_ID; }
                set { if (!string.IsNullOrEmpty(value)) m_ID = value; }
            }
            public int Width
            {
                get { return m_Width; }
                set { m_Width = value; }
            }
            public int Height
            {
                get { return m_Height; }
                set { m_Height = value; }
            }
            public ArrayList Multis
            {
                get { return m_Multis; }
                set { if (value != null) m_Multis = value; }
            }

            public ArrayList Deco
            {
                get { return m_Deco; }
                set { if (value != null) m_Deco = value; }
            }

            public ArrayList PatchTiles
            {
                get { return m_PatchTiles; }
                set { if (value != null) m_PatchTiles = value; }
            }

            public string Description
            {
                get { return m_Description; }
                set { m_Description = value; }
            }

            public int Price
            {
                get { return m_Price; }
                set { m_Price = value; }
            }

            public string OriginalOwnerName
            {
                get { return m_OriginalOwnerName; }
                set { m_OriginalOwnerName = value; }
            }

            public HouseBlueprint()
            {
                m_Multis = new ArrayList();
                m_Deco = new ArrayList();
                m_Region = new ArrayList();
                m_PatchTiles = new ArrayList();
            }

        }

        /// <summary>
        /// Data structure for the multis
        /// </summary>
        public struct MultiStruct
        {
            public short id;
            public short x;
            public short y;
            public short z;

            public MultiStruct(short ID, short X, short Y, short Z)
            {
                id = ID; x = X; y = Y; z = Z;
            }
        }

        private static Hashtable m_BlueprintList = new Hashtable();
        private static Hashtable m_FoundationLookup = new Hashtable();
        private static string m_BlueprintFilename = BlueprintDatabase;

        /// <summary>
        /// Creates a new blueprint object fron the passed xml node
        /// </summary>
        /// <param name="multis"></param>
        /// <returns></returns>
        private static HouseBlueprint CreateBlueprint(XmlElement blueprint)
        {
            if (blueprint == null)
                return null;

            HouseBlueprint b = new HouseBlueprint();
            foreach (XmlElement e in blueprint)
            {
                switch (e.Name.ToLower())
                {
                    case "id":
                        {
                            b.ID = e.InnerText;
                            break;
                        }
                    case "width":
                        {
                            b.Width = int.Parse(e.InnerText);
                            break;
                        }
                    case "height":
                        {
                            b.Height = int.Parse(e.InnerText);
                            break;
                        }
                    case "multi":
                        {
                            foreach (XmlElement multi in e)
                            {
                                try
                                {
                                    MultiStruct s = new MultiStruct();
                                    s.id = short.Parse(multi["id"].InnerText);
                                    s.x = short.Parse(multi["x"].InnerText);
                                    s.y = short.Parse(multi["y"].InnerText);
                                    s.z = short.Parse(multi["z"].InnerText);

                                    // flags == TileType.OutsideRect means the component exists outside 
                                    //	the bounding rect of the house (plot) (steps probably)
                                    TileType flags = (TileType)int.Parse(multi["flags"].InnerText);
                                    if (flags == TileType.Normal)
                                        b.Multis.Add(s);
                                    else if (((flags & TileType.Overlapped) != 0) || ((flags & TileType.Patch) != 0)) // must come before OutsideRect
                                        b.PatchTiles.Add(s);
                                    else if ((flags & TileType.OutsideRect) != 0)
                                        b.Deco.Add(s);
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.LogException(ex);
                                }
                            }
                            break;
                        }

                    // informational nodes
                    case "description":
                        {
                            b.Description = e.InnerText;
                            break;
                        }

                    case "price":
                        {   // fail safe pricing
                            int price;
                            if (int.TryParse(e.InnerText, out price) == false)
                                b.Price = (int)Error.PriceError;
                            else
                                b.Price = price;
                            break;
                        }

                    case "originalownername":
                        {
                            b.OriginalOwnerName = e.InnerText;
                            break;
                        }

                    case "version":
                        {
                            double version;
                            if (double.TryParse(e.InnerText, out version) == false)
                                b.Version = 1.0;
                            else
                                b.Version = version;
                            break;
                        }

                    case "capture":
                        {
                            DateTime capture;
                            if (DateTime.TryParse(e.InnerText, out capture) == false)
                                b.Capture = DateTime.MinValue;
                            else
                                b.Capture = capture;
                            break;
                        }

                    case "originalowneraccount":
                        {
                            b.OriginalOwnerAccount = e.InnerText;
                            break;
                        }

                    case "originalownerserial":
                        {
                            Int32 temp;
                            if (System.Int32.TryParse(e.InnerText.Remove(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out temp) == false)
                                b.OriginalOwnerSerial = Serial.MinusOne;
                            else
                                b.OriginalOwnerSerial = (Serial)temp;
                            break;
                        }

                    case "signlocation":
                        {   // not used - but we keep the case label to avoid the console warning
                            break;
                        }

                    case "region":
                        {
                            foreach (XmlElement rect in e)
                            {
                                try
                                {
                                    int OffsetX = short.Parse(rect["x"].InnerText);
                                    int OffsetY = short.Parse(rect["y"].InnerText);
                                    int width = short.Parse(rect["width"].InnerText);
                                    int height = short.Parse(rect["height"].InnerText);
                                    Rectangle2D rectangle = new Rectangle2D(OffsetX, OffsetY, width, height);
                                    b.Region.Add(rectangle);
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.LogException(ex);
                                }
                            }
                            break;
                        }

                    case "signhangergraphic":
                        {
                            int signhangergraphic;
                            if (int.TryParse(e.InnerText, out signhangergraphic) == false)
                                b.SignHangerGraphic = 0xB98;
                            else
                                b.SignHangerGraphic = signhangergraphic;
                            break;
                        }

                    case "signpostgraphic":
                        {
                            int signpostgraphic;
                            if (int.TryParse(e.InnerText, out signpostgraphic) == false)
                                b.SignpostGraphic = 0x09;
                            else
                                b.SignpostGraphic = signpostgraphic;
                            break;
                        }

                    default:
                        {
                            Console.WriteLine("Unrecognized XML node name \"{0}\"", e.Name.ToLower());
                            break;
                        }
                }

            }
            return b;
        }

        // because we must keep rubish 'test' captures from ending up on Production, we have TC and PROD loading different XML files.
        // on Test Center we load/modify StaticHousingTC.xml, on production we load (read only) StaticHousingProd.xml.
        // we limit our [housegen tool to Test Center to help enforce this rule.
        public static string BlueprintDatabase
        {
            get
            {
                // we cannot rely on TestCenter.Enabled being set during Configure() 
                if (Core.RuleSets.TestCenterRules() == true)
                    //return "data\\StaticHousingTC.xml";
                    // since we're not building houses on TC ATM, lets allow players to play with prod houses
                    return "data\\StaticHousingProd.xml";
                else
                    return "data\\StaticHousingProd.xml";
            }
        }


        public static void TransferData(StaticHouse sh)
        {
            // now transfer over the data from the original blueprint house
            try
            {
                Misc.Diagnostics.Assert(sh != null, "sh == null");
                if (!IsBlueprintLoaded(sh.HouseBlueprintID))
                    if (!LoadBlueprint(sh.HouseBlueprintID))
                    {
                        try { throw new ApplicationException(); }
                        catch (Exception ex) { LogHelper.LogException(ex, "Can't find/load blueprint from hashtable/xml."); }
                        return;
                    }

                Misc.Diagnostics.Assert(m_BlueprintList[sh.HouseBlueprintID] is HouseBlueprint, "m_BlueprintList[sh.HouseBlueprintID] is HouseBlueprint == false");
                HouseBlueprint house = m_BlueprintList[sh.HouseBlueprintID] as HouseBlueprint;
                if (house == null)
                {
                    try { throw new ApplicationException(); }
                    catch (Exception ex) { LogHelper.LogException(ex, "Can't find/load blueprint from hashtable."); }
                    return;
                }

                // copy the data
                sh.DefaultPrice = house.Price;
                sh.BlueprintVersion = house.Version;
                sh.Description = house.Description;
                sh.OriginalOwnerName = house.OriginalOwnerName;
                sh.OriginalOwnerAccount = house.OriginalOwnerAccount;
                sh.OriginalOwnerSerial = house.OriginalOwnerSerial;
                sh.SignHanger.ItemID = house.SignHangerGraphic;
                sh.Signpost.ItemID = house.SignpostGraphic;
                sh.SignpostGraphic = house.SignpostGraphic;
                sh.RevisionDate = house.Capture;

                Misc.Diagnostics.Assert(house.Region != null, "house.Region == null");
                if (house.Region != null && house.Region.Count > 0)
                {
                    Misc.Diagnostics.Assert(sh.Components != null, "sh.Components == null");
                    foreach (Rectangle2D rect in house.Region)
                    {   // fixup offsets
                        MultiComponentList mcl = sh.Components;
                        int x = rect.X + sh.X + mcl.Min.X;
                        int y = rect.Y + sh.Y + mcl.Min.Y;
                        sh.AreaList.Add(new Rectangle2D(x, y, rect.Width, rect.Height));
                    }
                    sh.UpdateRegionArea();
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e, "Botched Static Housing xml.");
            }

        }

        /// <summary>
        /// Populates the foundation id lookup hashtable
        /// </summary>
        private static void CreateFoundationTable()
        {
            // ugliness!
            m_FoundationLookup.Add("7x7", 0x13EC); m_FoundationLookup.Add("11x15", 0x1424);
            m_FoundationLookup.Add("7x8", 0x13ED); m_FoundationLookup.Add("11x16", 0x1425);
            m_FoundationLookup.Add("7x9", 0x13EE); m_FoundationLookup.Add("12x14", 0x142F);
            m_FoundationLookup.Add("7x10", 0x13EF); m_FoundationLookup.Add("12x15", 0x1430);
            m_FoundationLookup.Add("7x11", 0x13F0); m_FoundationLookup.Add("12x16", 0x1431);
            m_FoundationLookup.Add("7x12", 0x13F1); m_FoundationLookup.Add("12x17", 0x1432);
            m_FoundationLookup.Add("8x7", 0x13F8); m_FoundationLookup.Add("13x14", 0x143B);
            m_FoundationLookup.Add("8x8", 0x13F9); m_FoundationLookup.Add("13x15", 0x143C);
            m_FoundationLookup.Add("8x9", 0x13FA); m_FoundationLookup.Add("13x16", 0x143D);
            m_FoundationLookup.Add("8x10", 0x13FB); m_FoundationLookup.Add("13x17", 0x143E);
            m_FoundationLookup.Add("8x11", 0x13FC); m_FoundationLookup.Add("13x18", 0x143F);
            m_FoundationLookup.Add("8x12", 0x13FD); m_FoundationLookup.Add("14x9", 0x1442);
            m_FoundationLookup.Add("8x13", 0x13FE); m_FoundationLookup.Add("14x10", 0x1443);
            m_FoundationLookup.Add("9x7", 0x1404); m_FoundationLookup.Add("14x11", 0x1444);
            m_FoundationLookup.Add("9x8", 0x1405); m_FoundationLookup.Add("14x12", 0x1445);
            m_FoundationLookup.Add("9x9", 0x1406); m_FoundationLookup.Add("14x13", 0x1446);
            m_FoundationLookup.Add("9x10", 0x1407); m_FoundationLookup.Add("14x14", 0x1447);
            m_FoundationLookup.Add("9x11", 0x1408); m_FoundationLookup.Add("14x15", 0x1448);
            m_FoundationLookup.Add("9x12", 0x1409); m_FoundationLookup.Add("14x16", 0x1449);
            m_FoundationLookup.Add("9x13", 0x140A); m_FoundationLookup.Add("14x17", 0x144A);
            m_FoundationLookup.Add("10x7", 0x1410); m_FoundationLookup.Add("14x18", 0x144B);
            m_FoundationLookup.Add("10x8", 0x1411); m_FoundationLookup.Add("15x10", 0x144F);
            m_FoundationLookup.Add("10x9", 0x1412); m_FoundationLookup.Add("15x11", 0x1450);
            m_FoundationLookup.Add("10x10", 0x1413); m_FoundationLookup.Add("15x12", 0x1451);
            m_FoundationLookup.Add("10x11", 0x1414); m_FoundationLookup.Add("15x13", 0x1452);
            m_FoundationLookup.Add("10x12", 0x1415); m_FoundationLookup.Add("15x14", 0x1453);
            m_FoundationLookup.Add("10x13", 0x1416); m_FoundationLookup.Add("15x15", 0x1454);
            m_FoundationLookup.Add("11x7", 0x141C); m_FoundationLookup.Add("15x16", 0x1455);
            m_FoundationLookup.Add("11x8", 0x141D); m_FoundationLookup.Add("15x17", 0x1456);
            m_FoundationLookup.Add("11x9", 0x141E); m_FoundationLookup.Add("15x18", 0x1457);
            m_FoundationLookup.Add("11x10", 0x141F); m_FoundationLookup.Add("16x11", 0x145C);
            m_FoundationLookup.Add("11x11", 0x1420); m_FoundationLookup.Add("16x12", 0x145D);
            m_FoundationLookup.Add("11x12", 0x1421); m_FoundationLookup.Add("16x13", 0x145E);
            m_FoundationLookup.Add("11x13", 0x1422); m_FoundationLookup.Add("16x14", 0x145F);
            m_FoundationLookup.Add("12x7", 0x1428); m_FoundationLookup.Add("16x15", 0x1460);
            m_FoundationLookup.Add("12x8", 0x1429); m_FoundationLookup.Add("16x16", 0x1461);
            m_FoundationLookup.Add("12x9", 0x142A); m_FoundationLookup.Add("16x17", 0x1462);
            m_FoundationLookup.Add("12x10", 0x142B); m_FoundationLookup.Add("16x18", 0x1463);
            m_FoundationLookup.Add("12x11", 0x142C); m_FoundationLookup.Add("17x12", 0x1469);
            m_FoundationLookup.Add("12x12", 0x142D); m_FoundationLookup.Add("17x13", 0x146A);
            m_FoundationLookup.Add("12x13", 0x142E); m_FoundationLookup.Add("17x14", 0x146B);
            m_FoundationLookup.Add("13x8", 0x1435); m_FoundationLookup.Add("17x15", 0x146C);
            m_FoundationLookup.Add("13x9", 0x1436); m_FoundationLookup.Add("17x16", 0x146D);
            m_FoundationLookup.Add("13x10", 0x1437); m_FoundationLookup.Add("17x17", 0x146E);
            m_FoundationLookup.Add("13x11", 0x1438); m_FoundationLookup.Add("17x18", 0x146F);
            m_FoundationLookup.Add("13x12", 0x1439); m_FoundationLookup.Add("18x13", 0x1476);
            m_FoundationLookup.Add("13x13", 0x143A); m_FoundationLookup.Add("18x14", 0x1477);
            m_FoundationLookup.Add("9x14", 0x140B); m_FoundationLookup.Add("18x15", 0x1478);
            m_FoundationLookup.Add("10x14", 0x1417); m_FoundationLookup.Add("18x16", 0x1479);
            m_FoundationLookup.Add("10x15", 0x1418); m_FoundationLookup.Add("18x17", 0x147A);
            m_FoundationLookup.Add("11x14", 0x1423); m_FoundationLookup.Add("18x18", 0x147B);

        }

        /// <summary>
        /// Determines if a blueprint is already loaded in the hashtable or within the xml file.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsBlueprintLoaded(string id)
        {
            if (id != null && m_BlueprintList[id] != null)
                return true; // already exists

            return false;
        }

        /// <summary>
        /// Determines if a blueprint id already exists in the data file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsBlueprintInFile(string houseID)
        {
            XmlDocument doc = OpenDoc();
            if (doc == null)
                return true;

            //try to find the blueprint
            foreach (XmlElement element in doc["StaticHousing"])
                if (element.Name == "HouseID" && element["id"].InnerText == houseID)
                    return true;

            return false;
        }

        public static List<StaticHouseDescription> GetAllStaticHouseDescriptions()
        {
            List<StaticHouseDescription> returnList = new List<StaticHouseDescription>();

            try
            {
                foreach (DictionaryEntry de in m_BlueprintList)
                {
                    HouseBlueprint hb = de.Value as HouseBlueprint;
                    if (hb != null)
                    {
                        try
                        {
                            string id = hb.ID;              // element["id"].InnerText;
                            string desc = hb.Description;   // element["Description"].InnerText;
                            desc += " by ";
                            desc += hb.OriginalOwnerName;   // element["OriginalOwnerName"].InnerText;
                            int price = hb.Price;
                            StaticHouseDescription shd = new StaticHouseDescription(id, desc, price);
                            returnList.Add(shd);
                        }
                        catch (Exception e)
                        {
                            LogHelper.LogException(e, "Possible bad element in static house xml: element[id].");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e, "Botched Static Housing xml.");
            }

            return returnList;
        }

        /// <summary>
        /// Checks to see if a foudnation size exists
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static bool IsFoundationSizeValid(int width, int height)
        {
            bool result = false;

            string key = width.ToString() + "x" + height.ToString();

            if (m_FoundationLookup == null || m_FoundationLookup.Count == 0)
                CreateFoundationTable();

            if (m_FoundationLookup.ContainsKey(key))
                return true;

            return result;
        }

        public static int GetBasePrice(int width, int height)
        {   // http://i6.photobucket.com/albums/y236/squired/Custom-House-Pricing.jpg
            int Plot_Depth = height;
            int Plot_Width = width;
            return ((Plot_Depth - 7) * 9200 + 2500) * (Plot_Width - 7) + (Plot_Depth - 7) * 2500 + 65000;
        }

        /// <summary>
        /// Appends a new blueprint hashtable entry. Return value indicates success or lack thereof.
        /// </summary>
        /// <param name="blueprint"></param>
        /// <returns></returns>
        private static bool AppendBlueprint(HouseBlueprint blueprint)
        {
            if (blueprint == null)
                return false;

            //add the new entry under its ID
            try
            {
                m_BlueprintList.Add(blueprint.ID, blueprint);
                return true;
            }
            catch (ArgumentNullException ex)
            {
                LogHelper.LogException(ex, "Null argument caught during blueprint hashtable append");
                Console.WriteLine("Null argument caught during blueprint hashtable append");
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "Exception caught during blueprint hashtable append");
                Console.WriteLine("Exception caught during blueprint hashtable append");
                return false;
            }

        }

        public static ArrayList GetPatchTiles(string BlueprintID)
        {
            if (!StaticHouseHelper.IsBlueprintLoaded(BlueprintID))
                if (!StaticHouseHelper.LoadBlueprint(BlueprintID))
                    return null;

            //grab blueprint
            HouseBlueprint house = m_BlueprintList[BlueprintID] as HouseBlueprint;
            if (house == null)
                return null;

            return house.PatchTiles;
        }

        /// <summary>
        /// Loads a blueprint from the data file in the hashtable
        /// </summary>
        /// <param name="houseID"></param>
        /// <returns></returns>
        public static bool LoadBlueprint(string houseID)
        {
            if (houseID == null)
                return false;

            //try to create the blueprint
            XmlDocument doc = OpenDoc();
            foreach (XmlElement element in doc["StaticHousing"])
            {
                if (element.Name == "HouseID" && element["id"].InnerText == houseID)
                {
                    if (!AppendBlueprint(CreateBlueprint(element)))
                        return false;

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to open the data file
        /// </summary>
        /// <returns></returns>
        private static XmlDocument OpenDoc()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(m_BlueprintFilename);
                return doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in xml house blueprint file load " + ex.Message);
                return null;
            }

        }
        /// <summary>
        /// Retruns the multi id for the foundation of this house size
        /// </summary>
        /// <param name="houseID"></param>
        /// <returns></returns>
        public static int GetFoundationID(string houseID)
        {

            if (!IsBlueprintLoaded(houseID))
                if (!LoadBlueprint(houseID))
                    return 0;

            HouseBlueprint house = m_BlueprintList[houseID] as HouseBlueprint;
            if (house == null)
            {
                return 0;
            }
            if (m_FoundationLookup == null || m_FoundationLookup.Count == 0)
                CreateFoundationTable();

            //build key from width and height 
            string key = house.Width.ToString() + "x" + house.Height.ToString();
            if (m_FoundationLookup.ContainsKey(key))
                return (int)m_FoundationLookup[key];
            else
                return 0;

        }

        public static int GetPrice(string houseID)
        {

            if (!IsBlueprintLoaded(houseID))
                if (!LoadBlueprint(houseID))
                    // failsafe pricing
                    return (int)Error.PriceError;

            HouseBlueprint house = m_BlueprintList[houseID] as HouseBlueprint;
            if (house == null)
                return (int)Error.PriceError;
            else
                return house.Price;
        }

        public static string GetDescription(string houseID)
        {

            if (!IsBlueprintLoaded(houseID))
                if (!LoadBlueprint(houseID))
                    return null;

            HouseBlueprint house = m_BlueprintList[houseID] as HouseBlueprint;
            if (house == null)
                return null;
            else
                return house.Description;
        }

        public static bool GetFoundationSize(int MultiID, out int width, out int height)
        {
            if (m_FoundationLookup == null || m_FoundationLookup.Count == 0)
                CreateFoundationTable();

            width = height = 0;
            foreach (DictionaryEntry de in m_FoundationLookup)
                if (MultiID == ((int)de.Value | 0x4000))
                {   // looka like "18x18"
                    string dim = de.Key as string;
                    string[] split = dim.Split(new Char[] { 'x' });
                    try
                    {
                        width = int.Parse(split[0]);
                        height = int.Parse(split[1]);
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Creates and returns a FixerAddon object for this house blueprint.
        /// </summary>
        public static FixerAddon BuildFixerAddon(string houseID)
        {
            if (!IsBlueprintLoaded(houseID))
                if (!LoadBlueprint(houseID))
                    return null;

            //grab blueprint
            HouseBlueprint house = m_BlueprintList[houseID] as HouseBlueprint;
            if (house == null)
                return null;

            return new FixerAddon(house.PatchTiles);
        }

        /// <summary>
        /// Returns component list from the hashtable
        /// </summary>
        /// <param name="houseID"></param>
        /// <returns></returns>
        public static MultiComponentList GetComponents(string houseID)
        {

            MultiComponentList mcl = MultiComponentList.Empty;
            if (!IsBlueprintLoaded(houseID))
                if (!LoadBlueprint(houseID))
                    return mcl;

            //grab blueprint
            HouseBlueprint house = m_BlueprintList[houseID] as HouseBlueprint;
            if (house == null)
                return mcl;

            //now the fun tile processing and mcl setup      
            MultiTileEntry[] allTiles = mcl.List = new MultiTileEntry[house.Multis.Count + house.Deco.Count];

            // normal house tiles
            int i = 0;
            foreach (MultiStruct multiData in house.Multis)
            {
                allTiles[i].m_ItemID = multiData.id;
                allTiles[i].m_OffsetX = multiData.x;
                allTiles[i].m_OffsetY = multiData.y;
                allTiles[i].m_OffsetZ = multiData.z;
                ++i;
            }

            // deco items on the plot, like steps (outside bounding rect)
            foreach (MultiStruct multiData in house.Deco)
            {
                allTiles[i].m_ItemID = multiData.id;
                allTiles[i].m_OffsetX = multiData.x;
                allTiles[i].m_OffsetY = multiData.y;
                allTiles[i].m_OffsetZ = multiData.z;
                ++i;
            }

            mcl.Center = new Point2D(-mcl.Min.X, -mcl.Min.Y);
            mcl.Width = (mcl.Max.X - mcl.Min.X) + 1;
            mcl.Height = (mcl.Max.Y - mcl.Min.Y) + 1;

            TileList[][] tiles = new TileList[mcl.Width][];
            mcl.Tiles = new Tile[mcl.Width][][];

            for (int x = 0; x < mcl.Width; ++x)
            {
                tiles[x] = new TileList[mcl.Height];
                mcl.Tiles[x] = new Tile[mcl.Height][];

                for (int y = 0; y < mcl.Height; ++y)
                    tiles[x][y] = new TileList();
            }

            if (i == 0)
            {
                int xOffset = allTiles[i].m_OffsetX + mcl.Center.X;
                int yOffset = allTiles[i].m_OffsetY + mcl.Center.Y;

                tiles[xOffset][yOffset].Add((short)((allTiles[i].m_ItemID & 0x3FFF) | 0x4000), (sbyte)allTiles[i].m_OffsetZ);
            }

            for (int x = 0; x < mcl.Width; ++x)
                for (int y = 0; y < mcl.Height; ++y)
                    mcl.Tiles[x][y] = tiles[x][y].ToArray();

            return mcl;

        }

        public sealed class FixerAddon : BaseAddon
        {
            public override bool ShareHue
            {
                get
                {
                    return false;
                }
            }

            public FixerAddon(ArrayList tiles)
            {
                foreach (MultiStruct ms in tiles)
                {
                    AddComponent(new AddonComponent((int)ms.id), ms.x, ms.y, ms.z);
                }
            }

            public FixerAddon(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }

            public override void OnChop(Mobile from)
            {
                from.SendMessage("You cannot chop that.");
            }
        }
    }


    public class StaticHouseDescription
    {
        public string ID;
        public string Description;
        public int Price;

        public StaticHouseDescription(string id, string desc, int price)
        {
            ID = id;
            Description = desc;
            Price = price;
        }
    }


}
