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

/* Scripts/Engines/Township/TownshipDeed.cs
 * CHANGELOG:
 * 7/23/08 Pix
 *		Guess I need more sleep still.  It'll only get worse. . . 
 * 7/23/08, Pix
 *		Added more null-checking in GetPercentageOfGuildedHousesInArea.
 *		Put in better logging of placement.
 * 7/23/08, Adam
 *		Don't assume that houses have an owner
 * 7/22/08, Pix
 *		Extended try/catch in GetPercentageOfGuildedHousesInArea method - need to track down what caused the nullreferenceexception
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	5/16/07, Pix
 *		Fixed overlap testing to include custom regions.
 *	4/22/07, Pix
 *		Now ignores Siege Tents in the ownership percentage check for placement.
 *  4/21/07, Adam
 *      Added time-to-place logging
 *	3/20/07, Pix
 *		Added InitialFunds dial.
 *	3/19/07, Pix
 *		Added confirmation gump.
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Guilds;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public class TownshipDeed : Item
    {
        [Constructable]
        public TownshipDeed()
            : base(0x14F0)
        {
            Weight = 12.0;
            LootType = LootType.Blessed;
            Name = "a township deed";
            this.Hue = Township.TownshipSettings.Hue;
        }

        public TownshipDeed(Serial serial)
            : base(serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            Place(from, new Point3D(from.Location.X, from.Location.Y, from.Location.Z), true);
            tc.End();
            LogHelper Logger = new LogHelper("TownshipPlacementTime.log", false);
            //from.SendMessage(String.Format("Stone placement took {0}", tc.TimeTaken));
            Logger.Log(LogType.Text, String.Format("Stone placement check at {0} took {1}", from.Location, tc.TimeTaken));
            Logger.Finish();
        }

        public void Place(Mobile from, Point3D location, bool bCheck)
        {
            Guild g = from.Guild as Guild; ;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); //This must be in your backpack
            }
            else if (g == null)
            {
                from.SendMessage("You must be a member of a guild to use this.");
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);
                Guildstone stone = null;

                if (house != null)
                {
                    stone = house.FindGuildstone();
                }
                else
                {
                    stone = g.Guildstone as Guildstone;
                }

                //if (house == null || stone == null || stone.Guild != from.Guild)
                if (stone == null || stone.Guild != from.Guild)
                {
                    //from.SendMessage("You must be in the house with your guild's guildstone to use this.");
                    from.SendMessage("You must be a member of a guild and not be in a building which houses another guildstone to use this.");
                }
                else
                {
                    bool bZoneOverlaps = false;
                    bZoneOverlaps = DoesTownshipRegionConflict(location, from.Map, TownshipStone.INITIAL_RADIUS, null);

                    if (bZoneOverlaps)
                    {
                        from.SendMessage("You can't create a township that conflicts with another township, guardzone, or other special area.");
                        return;
                    }

                    double guildPercentage = GetPercentageOfGuildedHousesInArea(location, from.Map, TownshipStone.INITIAL_RADIUS, from.Guild as Guild, true);

                    if (guildPercentage >= Township.TownshipSettings.GuildHousePercentage)
                    {
                        if (bCheck)
                        {
                            from.SendGump(new ConfirmPlacementGump(from, location, this));
                        }
                        else
                        {
                            TownshipStone ts = new TownshipStone(stone.Guild);
                            ts.GoldHeld = Township.TownshipSettings.InitialFunds; //initial gold :-)
                            ts.MoveToWorld(location, from.Map);
                            ts.CreateInitialArea(location);

                            from.SendMessage("The township has been created.");

                            this.Delete();
                        }
                    }
                    else
                    {
                        from.SendMessage("You can't create a township without owning most of the houses in the area.");
                    }

                }
            }
        }

        public static double GetPercentageOfGuildedHousesInArea(Point3D location, Map map, int radius, Guild guild, bool bIgnoreAlliedHouses)
        {
            double guildPercentage = 0.0;

            if (guild == null) return guildPercentage; //doublecheck this - return 0 if we're not guilded
            if (location == Point3D.Zero) return guildPercentage; //might as well check that we're not the default point

            try //safety
            {
                int x = location.X;
                int y = location.Y;
                int z = location.Z;

                int x_start = x - radius;
                int y_start = y - radius;
                int x_end = x + radius;
                int y_end = y + radius;

                Rectangle2D rect = new Rectangle2D(x_start, y_start, TownshipStone.INITIAL_RADIUS * 2, TownshipStone.INITIAL_RADIUS * 2);
                List<BaseHouse> houseList = TownshipDeed.GetHousesInRect(rect, map);

                int guildCount = 0;
                int allyCount = 0;
                int otherCount = 0;

                int siegetentCount = 0;

                int countedHouses = 0;

                foreach (BaseHouse h in houseList)
                {
                    if (h != null && h.Owner != null)
                    {
                        countedHouses++;

                        Guild houseGuild = h.Owner.Guild as Guilds.Guild;

                        if (h is SiegeTent)
                        {
                            siegetentCount++;
                        }
                        else
                        {
                            if (houseGuild == null)
                            {
                                otherCount++;
                            }
                            else if (houseGuild == guild)
                            {
                                guildCount++;
                            }
                            else if (guild.IsAlly(houseGuild))
                            {
                                allyCount++;
                            }
                            else
                            {
                                otherCount++;
                            }
                        }
                    }
                }

                guildPercentage = ((double)guildCount) / ((double)(countedHouses - allyCount - siegetentCount));
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            return guildPercentage;
        }

        //Utility function :O
        public static List<BaseHouse> GetHousesInRect(Rectangle2D rect, Map map)
        {
            List<BaseHouse> houseList = new List<BaseHouse>();

            int z = 0; //Pix: is this right?

            int x_start = rect.Start.X;//x - TownshipStone.INITIAL_RADIUS;
            int y_start = rect.Start.Y;// y - TownshipStone.INITIAL_RADIUS;
            int x_end = rect.End.X;// x + TownshipStone.INITIAL_RADIUS;
            int y_end = rect.End.Y;// x + TownshipStone.INITIAL_RADIUS;

            for (int i = x_start; i < x_end; i++)
            {
                for (int j = y_start; j < y_end; j++)
                {
                    Region r = Region.Find(new Point3D(i, j, z), map);
                    if (r != null)
                    {
                        if (r is HouseRegion)
                        {
                            BaseHouse h = ((HouseRegion)r).House;
                            if (!houseList.Contains(h))
                            {
                                houseList.Add(h);
                            }
                        }
                    }
                }
            }

            return houseList;
        }

        public static bool DoesTownshipRegionConflict(Point3D centerLocation, Map map, int radius, TownshipRegion ignoreThisRegion)
        {
            bool bReturn = false;

            int x = centerLocation.X;
            int y = centerLocation.Y;
            int z = centerLocation.Z;

            int x_start = x - radius;
            int y_start = y - radius;
            int x_end = x + radius;
            int y_end = y + radius;

            for (int i = x_start; i < x_end; i++)
            {
                for (int j = y_start; j < y_end; j++)
                {
                    Region r = Region.Find(new Point3D(i, j, z), map);

                    if (r is TownshipRegion ||
                        r is GuardedRegion ||
                        r is CustomRegion)
                    {
                        if (ignoreThisRegion != null && r == ignoreThisRegion)
                        {
                        }
                        else
                        {
                            bReturn = true;
                            break;
                        }
                    }
                }

                if (bReturn) break;
            }


            return bReturn;
        }

        private bool DoHousesBelongToGuildOrAllies(Point3D centerLocation, Map map, Guild guild)
        {
            bool bReturn = true;

            int x = centerLocation.X;
            int y = centerLocation.Y;
            int z = centerLocation.Z;

            int x_start = x - TownshipStone.INITIAL_RADIUS;
            int y_start = y - TownshipStone.INITIAL_RADIUS;
            int x_end = x + TownshipStone.INITIAL_RADIUS;
            int y_end = x + TownshipStone.INITIAL_RADIUS;

            ArrayList houselist = new ArrayList();

            for (int i = x_start; i < x_end; i++)
            {
                for (int j = y_start; j < y_end; j++)
                {
                    Region r = Region.Find(new Point3D(i, j, z), map);
                    if (r != null)
                    {
                        if (r is HouseRegion)
                        {
                            BaseHouse h = ((HouseRegion)r).House;
                            if (!houselist.Contains(h))
                            {
                                houselist.Add(h);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < houselist.Count; i++)
            {
                try
                {
                    BaseHouse h = (BaseHouse)houselist[i];

                    if (h.Owner == null) //safety-check... ownerless houses are bad, m'kay
                    {
                        if (h.Owner.Guild == null)
                        {
                            bReturn = false;
                            break;
                        }

                        if (h.Owner.Guild != guild)
                        {
                            if (!guild.Allies.Contains(h.Owner.Guild))
                            {
                                bReturn = false;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }

            return bReturn;
        }


        private class ConfirmPlacementGump : Gump
        {
            Mobile m_From;
            Point3D m_Location;
            TownshipDeed m_TSDeed;

            public ConfirmPlacementGump(Mobile from, Point3D location, TownshipDeed deed)
                : base(50, 50)
            {
                m_From = from;
                m_Location = location;
                m_TSDeed = deed;

                from.CloseGump(typeof(ConfirmPlacementGump));

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "The township can be placed here.  Continue?"), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info.ButtonID == 1)
                {
                    try
                    {
                        Utility.TimeCheck tc = new Utility.TimeCheck();
                        tc.Start();
                        m_TSDeed.Place(m_From, m_Location, false);
                        tc.End();
                        LogHelper Logger = new LogHelper("TownshipPlacementTime.log", false);
                        //from.SendMessage(String.Format("Stone placement took {0}", tc.TimeTaken));
                        Logger.Log(LogType.Text, String.Format("Stone placement ACTUAL at {0} took {1}", m_Location, tc.TimeTaken));
                        Logger.Finish();
                    }
                    catch (Exception ex)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

    }
}
