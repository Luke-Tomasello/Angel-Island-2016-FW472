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

/* Scripts/Commands/HouseGen.cs
 *  Changelog:
 *  12/28/07 Taran Kain
 *      Added doubled-tile filter and flag
 *	7/8/07, Adam
 *		Add foundation stripping. there is still a bug here when we drop the structure
 *		a full 7 Z (tiles at 0 get clipped)
 *  6/25/07, Adam
 *      Major changes, please SR/MR for full details
 *  6/22/07, Adam
 *      Add BasePrice calculation function
 *  06/02/2007, plasma
 *      Initial creation, modified version of AddonGen.cs
 * 
 */

using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Multis.StaticHousing;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Commands
{
    /// <summary>
    /// Based upon [AddonGen, this command creates house blueprints and saves them to the StaticHousing file
    /// </summary>
    public class HouseGenerator
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("HouseGen", AccessLevel.Administrator, new CommandEventHandler(OnHouseGen));
        }

        [Usage("HouseGen")]
        [Description("Brings up the house script generator gump.")]
        private static void OnHouseGen(CommandEventArgs e)
        {

            //
            // State object:
            // 0: Name                          // house name
            // 1: Description                   // houes description
            // 2: Raw Capture (false)           // if true, ignore BaseHouse data at location
            // 3: Remove Foundation				// BETA
            // 4: Use range (false)             // specify Z range
            // 5: Min Z (-128)
            // 6: Max Z (127)
            // 7: Patch Tiles (false)			// use a 'FixerAddon' to patch missing house tiles

            // because we must keep rubish 'test' captures from ending up on Production, we have TC and PROD loading different XML files.
            // on Test Center we load/modify StaticHousingTC.xml, on production we load (read only) StaticHousingProd.xml.
            // we limit this tool to Test Center to help enforce this rule.
            if (TestCenter.Enabled == false)
            {
                e.Mobile.SendMessage(0x22, "Error: You may only capture houses on Test Center.");
                return;
            }

            object[] state = new object[8];

            TimeSpan ts = DateTime.UtcNow - new DateTime(2007, 1, 1);
            state[0] = "HID" + ((uint)ts.TotalMinutes).ToString("X");
            state[1] = "two-story mud and straw hut";
            state[2] = false;
            state[3] = false;
            state[4] = false;
            state[5] = (int)sbyte.MinValue;
            state[6] = (int)sbyte.MaxValue;
            state[7] = false;

            // Send gump
            e.Mobile.SendGump(new InternalGump(e.Mobile, state));
        }

        class Unit
        {
            public int m_xOffset;
            public int m_yOffset;
            public int m_zOffset;
            public int m_id;
            public StaticHouseHelper.TileType m_flags;

            public Unit(int xOffset, int yOffset, int zOffset, int id, StaticHouseHelper.TileType flags)
            {
                m_xOffset = xOffset;
                m_yOffset = yOffset;
                m_zOffset = zOffset;
                m_id = id;
                m_flags = flags;
            }
        }

        private static void PickerCallback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            object[] args = state as object[];
            string name = args[0] as string;        // house name
            string description = args[1] as string; // house description
            bool items = false;             // we capture items separatly
            bool statics = true;            // we alwaye want statics when capturing a house
            bool land = false;              // we never land tiles when capturing a house
            bool raw = (bool)args[2];       // raw capture will ignore BaseHouse info
            bool no_foundation = (bool)args[3]; // remove the foundation
            bool range = (bool)args[4];     // specify Z
            bool patch = (bool)args[7];     // create a FixerAddon to patch missing tiles (expensive, don't use unless you must)
            BaseHouse houseInRect = null;   // when not doing raw capture, we extract data directly from the house

            // normalize bounding rect
            if (start.X > end.X)
            {
                int x = start.X;
                start.X = end.X;
                end.X = x;
            }
            if (start.Y > end.Y)
            {
                int y = start.Y;
                start.Y = end.Y;
                end.Y = y;
            }

        // if we have a house here and we're not in 'raw capture' mode, extract
        //  the actual multi rect instead of leaving it up to the user ;p
        switching_to_Raw_mode:
            if (raw == false)
            {   // check each tile looking for a house
                for (int ix = start.X; ix < end.X; ix++)
                {
                    for (int iy = start.Y; iy < end.Y; iy++)
                    {
                        Point3D point = new Point3D(ix, iy, 16);
                        houseInRect = BaseHouse.FindHouseAt(point, from.Map, 16);
                        if (houseInRect != null)
                        {   // get the *real* location/dimentions from the multi
                            from.SendMessage(0x40, "Progress: Found a house at this location, extracting info.");
                            MultiComponentList mcl = houseInRect.Components;
                            int x = houseInRect.X + mcl.Min.X;
                            int y = houseInRect.Y + mcl.Min.Y;
                            start.X = x;
                            end.X = start.X + mcl.Width;
                            start.Y = y;
                            end.Y = start.Y + mcl.Height;
                            // for houses with plots, use the plot dimentions
                            if (houseInRect is HouseFoundation)
                            {   // patch width based on plot size and not multi 
                                int MultiID = houseInRect.ItemID;
                                int width = 0; int height = 0;
                                StaticHouseHelper.GetFoundationSize(MultiID, out width, out height);
                                end.X = start.X + width;
                                end.Y = start.Y + height;
                            }
                            goto exit_house_info;
                        }
                    }
                }

                // if we can't find a house here, switch to raw capture mode
                if (houseInRect == null)
                {
                    from.SendMessage(0x40, "Info: No house at this location, switching to Raw mode.");
                    raw = true;
                    goto switching_to_Raw_mode;
                }
            }
            // we're in raw capture mode, help the user by snapping to the next valid plot size
            //	if the rect they selected is not a valid size
            else
            {
                if (!StaticHouseHelper.IsFoundationSizeValid(end.X - start.X, end.Y - start.Y))
                {
                    int tempWidth = end.X - start.X;
                    int tempHeight = end.Y - start.Y;
                    int ix = 0, iy = 0;
                    while (true)
                    {
                        if (StaticHouseHelper.IsFoundationSizeValid(tempWidth + ix, tempHeight))
                        {
                            end.X += ix;
                            from.SendMessage(0x40, String.Format("Info: Snapping to next leagal X plot size."));
                            goto exit_house_info;
                        }
                        else if (StaticHouseHelper.IsFoundationSizeValid(tempWidth, tempHeight + iy))
                        {
                            end.Y += iy;
                            from.SendMessage(0x40, String.Format("Info: Snapping to next leagal Y plot size."));
                            goto exit_house_info;
                        }

                        if (ix == 18 && iy == 18) break;    // we should exit before hitting this case
                        if (ix < 18) ix++;                  // next valid X
                        if (iy < 18) iy++;                  // next valid Y
                    }
                }
            }

        // we now have the 'perfect' rect for BaseHouse being captured.
        exit_house_info:

            // do we have a valid plot size?
            if (!StaticHouseHelper.IsFoundationSizeValid(end.X - start.X, end.Y - start.Y))
            {
                from.SendMessage(0x22, "Error: House size " + Convert.ToString(end.X - start.X) + "x" + Convert.ToString(end.Y - start.Y) + " is invalid!");
                from.SendGump(new InternalGump(from, (object[])state));
                return;
            }
            else
                from.SendMessage(0x40, String.Format("Info: Selected plot size {0}x{1}.", end.X - start.X, end.Y - start.Y));

            // calc price based on plot size
            // THIS is the portion an NPC architect will refund... not the tile(plot size) assessment
            //	added below.
            int BasePrice = StaticHouseHelper.GetBasePrice(end.X - start.X, end.Y - start.Y);

            //pla: Check if the house blueprint file already exists, and if not then create one
            if (!Directory.Exists("Data"))
                Directory.CreateDirectory("Data");

            if (!File.Exists(StaticHouseHelper.BlueprintDatabase))
                using (XmlTextWriter writer = new XmlTextWriter(StaticHouseHelper.BlueprintDatabase, System.Text.Encoding.Unicode))
                {
                    writer.WriteStartElement("StaticHousing");
                    writer.WriteEndElement();
                }

            //pla: Create document object for manipulation
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(StaticHouseHelper.BlueprintDatabase);
            if (xmlDoc == null)
                return;

            // version of this XML format
            //  do not confuse this version with the version of the house.
            XmlNode root = xmlDoc.FirstChild;
            double formatVersion = 1.0;
            XmlAttributeCollection attrColl = root.Attributes;
            XmlAttribute attr = null;
            if (attrColl != null)
                attr = (XmlAttribute)attrColl.GetNamedItem("Version");
            if (attr == null)
            {   // we don't have a version stamp, add one
                XmlNode vattr = xmlDoc.CreateNode(XmlNodeType.Attribute, "Version", "");
                vattr.Value = formatVersion.ToString();
                root.Attributes.SetNamedItem(vattr);
            }

            // a new house
            XmlElement newHouse = xmlDoc.CreateElement("HouseID");
            Rectangle2D bounds = new Rectangle2D(start, end);

            // house name
            XmlElement nameElement = xmlDoc.CreateElement("id");
            nameElement.InnerText = name;
            newHouse.AppendChild(nameElement);

            // house description
            //	a deed name is constructed as follows:
            //	"deed to a " + sh.Description
            //	an example name: "deed to a marble house with patio"
            XmlElement descriptionElement = xmlDoc.CreateElement("Description");
            if (description != null && description.Length > 0)
                descriptionElement.InnerText = description;
            else
                descriptionElement.InnerText = "(none)";
            newHouse.AppendChild(descriptionElement);

            // version of the house.
            //  do not confuse with the version of this XML format
            //  this is not the XML format of the house either, it's the construction version
            //	Displayed in the House Gump.
            double houseVersion = 1.0;
            XmlElement houseVersionElement = xmlDoc.CreateElement("Version");
            houseVersionElement.InnerText = houseVersion.ToString();
            newHouse.AppendChild(houseVersionElement);

            // Date/Time this version of this house was captured
            //	Displayed in the House Gump.
            DateTime CaptureDate = DateTime.UtcNow;
            XmlElement CaptureElement = xmlDoc.CreateElement("Capture");
            CaptureElement.InnerText = CaptureDate.ToString();
            newHouse.AppendChild(CaptureElement);

            // Record region info
            //	we will also capture the (HouseRegion) region info. We don't like the dumb
            //	complete-plot-is-the-region system introduced with Custom Housing, but prefer
            //	the old-school OSI bodle where the region is an array of well defined rects.
            //	we use the rect editing tools on copy1 of the Custom House, then recapture to 
            //	record the region info
            if (raw == false && houseInRect != null)
            {
                Region r = houseInRect.Region;
                if (!(r == null || r.Coords == null || r.Coords.Count == 0))
                {
                    ArrayList c = r.Coords;

                    XmlElement regionElement = xmlDoc.CreateElement("Region");
                    XmlElement rectElement = null;
                    for (int i = 0; i < c.Count; i++)
                    {
                        if (c[i] is Rectangle2D)
                        {

                            int width = ((Rectangle2D)(c[i])).Width;
                            int height = ((Rectangle2D)(c[i])).Height;
                            int x = ((Rectangle2D)(c[i])).Start.X - start.X;
                            int y = ((Rectangle2D)(c[i])).Start.Y - start.Y;
                            if (x < 0) x = 0;
                            if (y < 0) y = 0;

                            XmlElement CoordsElement = xmlDoc.CreateElement("Rectangle2D");

                            rectElement = xmlDoc.CreateElement("x");
                            rectElement.InnerText = x.ToString();
                            CoordsElement.AppendChild(rectElement);

                            rectElement = xmlDoc.CreateElement("y");
                            rectElement.InnerText = y.ToString();
                            CoordsElement.AppendChild(rectElement);

                            rectElement = xmlDoc.CreateElement("width");
                            rectElement.InnerText = width.ToString();
                            CoordsElement.AppendChild(rectElement);

                            rectElement = xmlDoc.CreateElement("height");
                            rectElement.InnerText = height.ToString();
                            CoordsElement.AppendChild(rectElement);

                            regionElement.AppendChild(CoordsElement);
                        }
                    }

                    newHouse.AppendChild(regionElement);
                }
            }

            sbyte min = sbyte.MinValue;
            sbyte max = sbyte.MaxValue;

            try { min = sbyte.Parse(args[5] as string); }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            try { max = sbyte.Parse(args[6] as string); }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            if (max < min)
            {
                sbyte temp = max;
                max = min;
                min = temp;
            }

            Hashtable tiles = new Hashtable();

            // (x == end.X || y == end.Y) will be steps or other deco outside the plot
            // see below where we output statics and set TileType 'flags'
            if (statics)
            {
                for (int x = start.X; x <= end.X; x++)
                {
                    for (int y = start.Y; y <= end.Y; y++)
                    {
                        ArrayList list = map.GetTilesAt(new Point2D(x, y), items, land, statics);

                        if (range)
                        {
                            ArrayList remove = new ArrayList();

                            foreach (Tile t in list)
                            {
                                if (t.Z < min || t.Z > max)
                                    remove.Add(t);
                            }

                            foreach (Tile t in remove)
                                list.Remove(t);
                        }

                        if (list != null && list.Count > 0)
                        {
                            tiles[new Point2D(x, y)] = list;
                        }
                    }
                }
            }

            // we increase the bounds by one here to match the way we scan for static above, that is: including end.X and end.Y
            //	the end.X and end.Y allows us to pick up things on the steps, and the house sign hanger
            Rectangle2D iBounds = new Rectangle2D(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height + 1);
            IPooledEnumerable en = map.GetItemsInBounds(iBounds);
            ArrayList target = new ArrayList();
            bool foundSign = false;

            // info pulled from captured house  
            DateTime BuiltOn = DateTime.MaxValue;
            string OriginalOwnerName = "(unknown)";
            string OriginalOwnerAccount = "(unknown)";
            Serial OriginalOwnerSerial = Serial.MinusOne;
            Point3D SignLocation = Point3D.Zero;    // not used
            int SignHangerGraphic = 0xB98;          // default
            int SignpostGraphic = 0x09;             // default

            try
            {
                // (x == end.X || y == end.Y) will be steps or other deco outside the plot
                // see below where we output statics and set TileType 'flags'
                foreach (object o in en)
                {

                    // remove all doors
                    if (o is BaseDoor)
                    {
                        from.SendMessage(0x40, "Progress: removing door.");
                        continue;
                    }

                    // Remove SignHanger from the outside of a Custom House
                    //	we look for it at a particular location
                    if (raw == false && houseInRect != null && houseInRect is HouseFoundation == true)
                        if (IsSignHanger(o) && (o as Item).Y == bounds.Y + bounds.Height)
                        {
                            from.SendMessage(0x40, "Progress: removing sign hanger.");
                            SignHangerGraphic = (o as Item).ItemID;
                            continue;
                        }

                    // Remove Signpost  from the outside of a Custom House
                    //	we look for it at a particular location
                    if (raw == false && houseInRect != null && houseInRect is HouseFoundation == true)
                        if (IsSignpost(o) && (o as Item).Y == bounds.Y + bounds.Height - 1)
                        {
                            from.SendMessage(0x40, "Progress: removing Signpost.");
                            SignpostGraphic = (o as Item).ItemID;
                            continue;
                        }

                    // any BaseHouse
                    if (o is HouseSign)
                    {   // suck the meaningful info from the house sign
                        from.SendMessage(0x40, "Progress: Processing house sign.");
                        HouseSign sign = o as HouseSign;
                        BaseHouse house = sign.Structure;
                        if (house != null)
                        {   // from house
                            BuiltOn = house.BuiltOn;
                        }
                        // from sign
                        OriginalOwnerName = sign.Owner.Name;
                        OriginalOwnerAccount = sign.Owner.Account.ToString();
                        OriginalOwnerSerial = sign.Owner.Serial;
                        SignLocation = sign.Location;
                        foundSign = true;
                        continue;
                    }

                    // GM built or OSI town static structure
                    if (o is StaticHouseSign)
                    {   // suck the meaningful info from the house sign
                        from.SendMessage(0x40, "Progress: Processing static house sign.");
                        StaticHouseSign sign = o as StaticHouseSign;
                        // from sign
                        BuiltOn = sign.BuiltOn;
                        OriginalOwnerName = sign.Owner.Name;
                        OriginalOwnerAccount = sign.Owner.Account.ToString();
                        OriginalOwnerSerial = sign.Owner.Serial;
                        SignLocation = sign.Location;
                        foundSign = true;
                        continue;
                    }

                    // outside the rect?
                    if (range && ((o as Static).Z < min || (o as Static).Z > max))
                        continue;

                    target.Add(o);
                }

                // on OSI houses the sign falls outside the multi-rect
                if (raw == false && houseInRect != null && foundSign == false)
                {
                    // suck the meaningful info from the house sign
                    from.SendMessage(0x40, "Progress: Processing house sign.");
                    HouseSign sign = houseInRect.Sign;
                    BaseHouse house = sign.Structure;
                    if (house != null)
                    {   // from house
                        BuiltOn = house.BuiltOn;
                    }
                    // from sign
                    OriginalOwnerName = sign.Owner.Name;
                    OriginalOwnerAccount = sign.Owner.Account.ToString();
                    OriginalOwnerSerial = sign.Owner.Serial;
                    SignLocation = sign.Location;
                    foundSign = true;
                }
            }
            catch (Exception er)
            {
                LogHelper.LogException(er);
                Console.WriteLine(er.ToString());
                from.SendMessage(0x40, "Info: The targeted items have been modified. Please retry.");
                return;
            }
            finally
            {
                en.Free();
            }

            // captured houses need a sign. for static houses, [add StaticHouseSign and set props
            // we also use the location if the sign as the location for the real sign during construction
            if (foundSign == false)
            {
                from.SendMessage(0x22, "Warning: No StaticHouseSign found for static house.");
                from.SendMessage(0x22, "Warning: [add StaticHouseSign and set props.");
                // don't fail.. assume the XML will be hand edited
            }

            if (target.Count == 0 && tiles.Keys.Count == 0)
            {
                from.SendMessage(0x22, "Error: No items have been selected.");
                return;
            }

            /* -- save the house builder / designed info --
			 * BuiltOn = sign.BuiltOn;
			 * OriginalOwnerName = sign.OriginalOwner.Name;
			 * OriginalOwnerAccount = sign.OriginalOwner.Account.ToString();
			 * OriginalOwnerSerial = sign.OriginalOwner.Serial;
			 * SignLocation = sign.Location;
			 */

            // Date/Time this version of this house was created
            //	Displayed in the House Gump as the revision date
            XmlElement BuiltOnElement = xmlDoc.CreateElement("BuiltOn");
            CaptureElement.InnerText = BuiltOn.ToString();
            newHouse.AppendChild(CaptureElement);

            // OriginalOwnerName
            //	Displayed in the House Gump.
            XmlElement OriginalOwnerNameElement = xmlDoc.CreateElement("OriginalOwnerName");
            OriginalOwnerNameElement.InnerText = OriginalOwnerName.ToString();
            newHouse.AppendChild(OriginalOwnerNameElement);

            // OriginalOwnerAccount
            XmlElement OriginalOwnerAccountElement = xmlDoc.CreateElement("OriginalOwnerAccount");
            OriginalOwnerAccountElement.InnerText = OriginalOwnerAccount.ToString();
            newHouse.AppendChild(OriginalOwnerAccountElement);

            // OriginalOwnerSerial
            //	Displayed in the House Gump as the 'designer's licence number'
            XmlElement OriginalOwnerSerialElement = xmlDoc.CreateElement("OriginalOwnerSerial");
            OriginalOwnerSerialElement.InnerText = OriginalOwnerSerial.ToString();
            newHouse.AppendChild(OriginalOwnerSerialElement);

            // SignLocation
            //	not used
            XmlElement SignLocationElement = xmlDoc.CreateElement("SignLocation");
            SignLocationElement.InnerText = "(unused)" + SignLocation.ToString();
            newHouse.AppendChild(SignLocationElement);

            // SignHangerGraphic
            XmlElement SignHangerGraphicElement = xmlDoc.CreateElement("SignHangerGraphic");
            SignHangerGraphicElement.InnerText = SignHangerGraphic.ToString();
            newHouse.AppendChild(SignHangerGraphicElement);

            // SignpostGraphic
            XmlElement SignpostGraphicElement = xmlDoc.CreateElement("SignpostGraphic");
            SignpostGraphicElement.InnerText = SignpostGraphic.ToString();
            newHouse.AppendChild(SignpostGraphicElement);

            // Get center
            Point3D center = new Point3D();
            center.Z = 127;

            int x1 = bounds.End.X;
            int y1 = bounds.End.Y;
            int x2 = bounds.Start.X;
            int y2 = bounds.Start.Y;

            // Get correct bounds
            foreach (object o in target)
            {
                Item item = o as Item;
                if (item == null)
                    continue;

                // don't factor these tiles as they are outside the bounding rect.
                //	(steps most likely)
                if (item.X >= end.X || item.Y >= end.Y)
                    continue;

                if (item.Z < center.Z)
                {
                    center.Z = item.Z;
                }

                x1 = Math.Min(x1, item.X);
                y1 = Math.Min(y1, item.Y);
                x2 = Math.Max(x2, item.X);
                y2 = Math.Max(y2, item.Y);
            }

            // Get correct bounds
            foreach (Point2D p in tiles.Keys)
            {
                ArrayList list = tiles[p] as ArrayList;

                // don't factor these tiles as they are outside the bounding rect.
                //	(steps most likely)
                if (p.X >= end.X || p.Y >= end.Y)
                    continue;

                foreach (Tile t in list)
                {
                    if (t.Z < center.Z)
                    {
                        center.Z = t.Z;
                    }
                }

                x1 = Math.Min(x1, p.X);
                y1 = Math.Min(y1, p.Y);
                x2 = Math.Max(x2, p.X);
                y2 = Math.Max(y2, p.Y);
            }

            center.X = x1 + ((x2 - x1) / 2);
            center.Y = y1 + ((y2 - y1) / 2);

            // width
            int PlotWidth = end.X - start.X;
            XmlElement widthElement = xmlDoc.CreateElement("Width");
            widthElement.InnerText = PlotWidth.ToString();
            newHouse.AppendChild(widthElement);

            // height
            int PlotHeight = end.Y - start.Y;
            XmlElement heightElement = xmlDoc.CreateElement("Height");
            heightElement.InnerText = PlotHeight.ToString();
            newHouse.AppendChild(heightElement);

            XmlElement multiElement = xmlDoc.CreateElement("Multi");
            XmlElement tempElement = null;
            ArrayList tileTable = new ArrayList();

            // Statics - add to master list
            foreach (Point2D p in tiles.Keys)
            {
                ArrayList list = tiles[p] as ArrayList;

                int xOffset = p.X - center.X;
                int yOffset = p.Y - center.Y;
                StaticHouseHelper.TileType flags = StaticHouseHelper.TileType.Normal;

                // mark these tiles as existing outside the bounding rect (steps most likely)
                if (p.X >= end.X || p.Y >= end.Y)
                    flags |= StaticHouseHelper.TileType.OutsideRect;

                foreach (Tile t in list)
                {
                    int zOffset = t.Z - center.Z;
                    int id = t.ID & 0x3FFF;

                    Unit unit = new Unit(xOffset, yOffset, zOffset, id, flags);
                    bool add = true;

                    foreach (Unit existing in tileTable)
                    {
                        if (existing.m_xOffset == unit.m_xOffset &&
                            existing.m_yOffset == unit.m_yOffset &&
                            existing.m_zOffset == unit.m_zOffset)
                        {
                            if (existing.m_id == unit.m_id)
                            {
                                add = false;
                                break;
                            }
                            else
                            {
                                if (patch == true)
                                    unit.m_flags |= StaticHouseHelper.TileType.Overlapped;
                            }
                        }
                    }

                    // only add if unique
                    if (add == true)
                        tileTable.Add(unit);
                    else
                        from.SendMessage(0x40, "Progress: Ignoring duplicate tile.");
                }
            }

            // Items - add to master list
            foreach (Object o in target)
            {
                Item item = o as Item;
                if (item == null)
                    continue;

                int xOffset = item.X - center.X;
                int yOffset = item.Y - center.Y;
                int zOffset = item.Z - center.Z;
                int id = item.ItemID;
                StaticHouseHelper.TileType flags = StaticHouseHelper.TileType.Normal;

                // aftermarket addons are 'patched' later
                if (item is AddonComponent || item is StaticHouseHelper.FixerAddon)
                    flags |= StaticHouseHelper.TileType.Patch;

                // mark these tiles as existing outside the bounding rect (steps most likely)
                if (item.X >= end.X || item.Y >= end.Y)
                    flags |= StaticHouseHelper.TileType.OutsideRect;

                Unit unit = new Unit(xOffset, yOffset, zOffset, id, flags);
                bool add = true;

                foreach (Unit existing in tileTable)
                {
                    if (existing.m_xOffset == unit.m_xOffset &&
                        existing.m_yOffset == unit.m_yOffset &&
                        existing.m_zOffset == unit.m_zOffset)
                    {
                        if (existing.m_id == unit.m_id)
                        {
                            if ((unit.m_flags & StaticHouseHelper.TileType.Patch) != 0)     // if the one we are trying the add is a patch..
                                existing.m_flags |= StaticHouseHelper.TileType.Patch;       // convert the existing one to the patch and don't add this one
                            add = false;
                            break;
                        }
                        else
                        {
                            if (patch == true)
                                unit.m_flags |= StaticHouseHelper.TileType.Overlapped;
                        }
                    }
                }

                // only add if unique
                if (add == true)
                    tileTable.Add(unit);
                else
                    from.SendMessage(0x40, "Progress: Ignoring duplicate tile.");
            }

            // Preprocess the list - pass I
            ArrayList removeList = new ArrayList();
            foreach (Unit unit in tileTable)
            {
                Item o = new Item(unit.m_id);

                // remove the foundation and fixup the 
                if (no_foundation == true)
                {
                    if (unit.m_zOffset == 0)
                    {
                        // remove foundation tiles
                        if (unit.m_xOffset == start.X - center.X ||
                            unit.m_yOffset == start.Y - center.Y ||
                            unit.m_xOffset == (end.X - center.X) - 1 ||
                            unit.m_yOffset == (end.Y - center.Y) - 1)
                            removeList.Add(unit);

                        // steps
                        if ((unit.m_flags & StaticHouseHelper.TileType.OutsideRect) != 0)
                            removeList.Add(unit);
                    }
                    // dirt tiles
                    else if (unit.m_zOffset == 7 && unit.m_id == 12788)
                        removeList.Add(unit);
                    else
                        // move all tiles down 7
                        //	bug: when we move down 7, tiles at 0 get clipped.
                        unit.m_zOffset -= 7;
                }

                // Remove SignHanger on outside of an OSI house
                if (raw == false && houseInRect != null && houseInRect is HouseFoundation == false)
                    if (IsSignHanger(o) && unit.m_yOffset * 2 == PlotHeight)
                    {
                        from.SendMessage(0x40, "Progress: removing sign hanger.");
                        removeList.Add(unit);
                    }

                // remove all doors
                if (o is BaseDoor)
                {
                    from.SendMessage(0x40, "Progress: removing door.");
                    removeList.Add(unit);
                }

            }

            // preprocess - pass II
            //	remove / process all things found in pass I
            foreach (Unit unit in removeList)
            {
                if (tileTable.Contains(unit))
                    tileTable.Remove(unit);
            }

            // house price
            //	price is a base price based on the plot size + a per tile cost (PTC).
            //	PTC is greater as the plot gets bigger .. this will encourage smaller houses, and 'tax' the big land hogs.
            //	the numbers were heuristically derived by looking at a very large and complex 18x18 and deciding that we wanted to add
            //	about 1,000,000 to the price, then dividing that by the number of tiles on this house (1130) and came up with a cost of approx
            //	885 per tile. We then use the logic 18x18 = 36 and 885/32 == 24 which gives us the base PTC. We then multiply
            //	24 * (width + height) to get the actual PTC for this house.
            //	so using this system above, a small 8x8 house has a PTC of 384 where a large 18x18 has a pertile cost of 864
            int price = BasePrice + (tileTable.Count * (24 * (PlotWidth + PlotWidth)));
            XmlElement priceElement = xmlDoc.CreateElement("Price");
            priceElement.InnerText = BasePrice.ToString();
            newHouse.AppendChild(priceElement);

            // Okay, write out all tile data
            foreach (Unit unit in tileTable)
            {
                XmlElement singleMultiElement = xmlDoc.CreateElement("Graphic");

                tempElement = xmlDoc.CreateElement("id");
                tempElement.InnerText = unit.m_id.ToString();
                singleMultiElement.AppendChild(tempElement);

                tempElement = xmlDoc.CreateElement("x");
                tempElement.InnerText = unit.m_xOffset.ToString();
                singleMultiElement.AppendChild(tempElement);

                tempElement = xmlDoc.CreateElement("y");
                tempElement.InnerText = unit.m_yOffset.ToString();
                singleMultiElement.AppendChild(tempElement);

                tempElement = xmlDoc.CreateElement("z");
                tempElement.InnerText = unit.m_zOffset.ToString();
                singleMultiElement.AppendChild(tempElement);

                tempElement = xmlDoc.CreateElement("flags");
                tempElement.InnerText = ((int)unit.m_flags).ToString();
                singleMultiElement.AppendChild(tempElement);

                multiElement.AppendChild(singleMultiElement);
            }

            // stats
            /*int total = tileTable.Count, deco = 0, patch = 0;
			foreach (Unit existing in tileTable)
			{
				//StaticHouseHelper.TileType flags = StaticHouseHelper.TileType.Normal;
				//StaticHouseHelper.TileType.OutsideRect;
				//StaticHouseHelper.TileType.Overlapped;
				if (existing.m_flags & StaticHouseHelper.TileType.OutsideRect != 0)
					deco++;

				if (existing.m_flags & StaticHouseHelper.TileType.Overlapped != 0)
					patch++;
			}
			from.SendMessage(String.Format("{0} total tiles of which {1} were outside the foundation and {2} were patch tiles.",total, deco, patch));*/

            newHouse.AppendChild(multiElement);
            xmlDoc["StaticHousing"].AppendChild(newHouse);
            xmlDoc.Save(StaticHouseHelper.BlueprintDatabase);
            from.SendMessage("Blueprint creation successful");

            // give the GM a deed to test with
            from.SendMessage("A deed has been placed in your backpack");
            from.AddToBackpack(new StaticDeed(name, description));
        }
        #region Gump

        private class InternalGump : Gump
        {
            private const int LabelHue = 0x480;
            private const int GreenHue = 0x40;
            private object[] m_State;

            public InternalGump(Mobile m, object[] state)
                : base(100, 50)
            {
                m.CloseGump(typeof(InternalGump));
                m_State = state;
                MakeGump();
            }

            private void MakeGump()
            {
                this.Closable = true;
                this.Disposable = true;
                this.Dragable = true;
                this.Resizable = false;
                this.AddPage(0);
                this.AddBackground(0, 0, 280, 225 + 25, 9270);
                this.AddAlphaRegion(10, 10, 260, 205);
                this.AddLabel(64, 15, GreenHue, @"StaticHouse Blueprint Generator");
                this.AddLabel(20, 40, LabelHue, @"House ID");
                this.AddImageTiled(95, 55, 165, 1, 9304);

                // Name: 0
                this.AddTextEntry(95, 35, 165, 20, LabelHue, 0, m_State[0] as string);

                this.AddLabel(20, 60, LabelHue, @"Description");
                this.AddImageTiled(95, 75, 165, 1, 9304);

                // Description: 1
                this.AddTextEntry(95, 55, 165, 20, LabelHue, 1, m_State[1] as string);

                // Items: Check 0
                this.AddCheck(20, 85, 2510, 2511, ((bool)m_State[2]), 0);
                this.AddLabel(40, 85, LabelHue, @"Raw Capture");

                // Remove foundation: Check 1
                this.AddCheck(20, 110, 2510, 2511, ((bool)m_State[3]), 1);
                this.AddLabel(40, 110, LabelHue, @"Remove Foundation");

                this.AddCheck(20, 135, 2510, 2511, ((bool)m_State[7]), 2);
                this.AddLabel(40, 135, LabelHue, @"Patch Tiles");

                int voffset = 25;
                // Range: Check 2
                this.AddCheck(20, 135 + voffset, 2510, 2511, ((bool)m_State[4]), 3);
                this.AddLabel(40, 135 + voffset, LabelHue, @"Specify Z Range");

                // Min Z: Text 2
                this.AddLabel(50, 160 + voffset, LabelHue, @"min.");
                this.AddImageTiled(85, 175 + voffset, 60, 1, 9304);
                this.AddTextEntry(85, 155 + voffset, 60, 20, LabelHue, 2, m_State[5].ToString());

                // Max Z: Text 3
                this.AddLabel(160, 160 + voffset, LabelHue, @"max.");
                this.AddImageTiled(200, 175 + voffset, 60, 1, 9304);
                this.AddTextEntry(200, 155 + voffset, 60, 20, LabelHue, 3, m_State[6].ToString());

                // Cancel: B0
                this.AddButton(20, 185 + voffset, 4020, 4021, 0, GumpButtonType.Reply, 0);
                this.AddLabel(55, 185 + voffset, LabelHue, @"Cancel");

                // Generate: B1
                this.AddButton(155, 185 + voffset, 4005, 4006, 1, GumpButtonType.Reply, 0);
                this.AddLabel(195, 185 + voffset, LabelHue, @"Generate");
            }

            public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
            {
                if (info.ButtonID == 0)
                    return;

                foreach (TextRelay text in info.TextEntries)
                {
                    switch (text.EntryID)
                    {
                        case 0: // Name, 0

                            m_State[0] = text.Text;

                            break;

                        case 1: // Description, 1

                            m_State[1] = text.Text;

                            break;

                        case 2: // Min Z, 5

                            m_State[5] = text.Text;

                            break;

                        case 3: // Max Z, 6

                            m_State[6] = text.Text;

                            break;
                    }
                }

                // Reset checks
                m_State[2] = false; // raw capture
                m_State[3] = false; // Remove foundation
                m_State[4] = false; // specify Z
                m_State[7] = false; // Patch Tiles

                foreach (int check in info.Switches)
                    switch (check)
                    {
                        case 0: m_State[2] = true; continue;
                        case 1: m_State[3] = true; continue;
                        case 3: m_State[4] = true; continue;
                        case 2: m_State[7] = true; continue;
                    }


                if (Verify(sender.Mobile, m_State))
                {
                    BoundingBoxPicker.Begin(sender.Mobile, new BoundingBoxCallback(HouseGenerator.PickerCallback), m_State);
                }
                else
                {
                    sender.Mobile.SendGump(new InternalGump(sender.Mobile, m_State));
                }
            }

            private static bool Verify(Mobile from, object[] state)
            {
                // error: name
                if (state[0] == null || (state[0] as string).Length == 0)
                {
                    from.SendMessage(0x22, "Error: You must specify a name for your blueprint.");
                    return false;
                }

                // warning: Description
                if (state[1] == null || (state[1] as string).Length == 0)
                {
                    from.SendMessage(0x22, "Warning: You should specify a description for this house.");
                    // continue, only a warning
                }

                //pla: Check if this name is already taken
                try
                {
                    if (StaticHouseHelper.IsBlueprintInFile(state[0] as string))
                    {
                        from.SendMessage(0x22, "Error: A blueprint by that name already exists.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    from.SendMessage(0x22, "Error: Unknown blueprint generation error.");
                    return false;
                }

                bool raw = (bool)state[2];
                bool foundation = (bool)state[3];
                bool range = (bool)state[4];
                bool patch = (bool)state[7];
                sbyte min = sbyte.MinValue;
                sbyte max = sbyte.MaxValue;
                bool fail = false;

                try
                {
                    min = sbyte.Parse(state[5] as string);
                }
                catch
                {
                    from.SendMessage(0x22, "Error: Bad min Z specified.");
                    fail = true;
                }

                try
                {
                    max = sbyte.Parse(state[6] as string);
                }
                catch
                {
                    from.SendMessage(0x22, "Error: Bad max Z specified.");
                    fail = true;
                }

                // error already reported
                if (range && fail)
                    return false;

                if (raw == true)
                    from.SendMessage(0x40, "Info: Raw capture will ignore house data.");

                return true;
            }
        }

        #endregion

        private static bool IsSignHanger(object o)
        {
            if (o is Item)
                switch ((o as Item).ItemID)
                {
                    case 2968:
                    case 2970:
                    case 2972:
                    case 2974:
                    case 2976:
                    case 2978:
                        return true;
                    default:
                        return false;
                }

            return false;
        }

        private static bool IsSignpost(object o)
        {
            if (o is Item)
                switch ((o as Item).ItemID)
                {
                    case 9:
                    case 29:
                    case 54:
                    case 90:
                    case 147:
                    case 169:
                    case 177:
                    case 204:
                    case 251:
                    case 257:
                    case 263:
                    case 298:
                    case 347:
                    case 424:
                    case 441:
                    case 466:
                        return true;
                    default:
                        return false;
                }

            return false;
        }
    }
}
