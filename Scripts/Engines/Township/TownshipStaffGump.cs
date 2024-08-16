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

/* Scripts/Engines/Township/TownshipStaffGump.cs
 * CHANGELOG
 * 10/25/08, Pix
 *		Gump text positioning.
 * 10/19/08, Pix
 *		Added more information, center of townships, whether the ts is extended.
 *		Made Go button go to stone, not township center (they used to be the same thing).
 * 10/17/08, Pix
 *		Added percentage house ownership to gump.
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	5/14/07, Pix
 *		Added WeeksAtThisLevel in detail.
 *	4/30/07: Pix
 *		Enhancements for usability.
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Township
{
    class TownshipStaffGump : Gump
    {
        private Mobile m_Mobile;
        private List<TownshipStone> m_List;
        private Sort m_Sort = Sort.DEFAULT;
        private int m_Page = 0;

        private enum Sort
        {
            DEFAULT,
            GUILD,
            ACTIVITY,
            SIZE,
            NULL
        }


        private void ConfigureGump()
        {
            AddPage(0);
            AddBackground(0, 0, 550, 430, 5054);
            AddBackground(10, 10, 530, 420, 3000);
        }

        public TownshipStaffGump(Mobile mobile)
            : this(mobile, Sort.DEFAULT, 0, null)
        {
        }

        private TownshipStaffGump(Mobile mobile, Sort sort, int page, List<TownshipStone> list)
            : this(mobile, sort, page, list, false, 0)
        {
        }

        private TownshipStaffGump(Mobile mobile, Sort sort, int page, List<TownshipStone> list, bool tsdetail, int index)
            : base(20, 30)
        {
            try
            {
                mobile.CloseGump(typeof(TownshipStaffGump));
                m_Mobile = mobile;
                m_List = list;
                m_Page = page;

                ConfigureGump();

                if (m_List == null)
                {
                    GenerateList();
                }

                if (tsdetail && index >= 0 && index < m_List.Count)
                {
                    //Show detail about township!
                    TownshipStone thisStone = m_List[index];

                    double dHousePercentageFull = 0.0;
                    double dHousePercentageWithAlliedIgnored = 0.0;
                    try
                    {
                        dHousePercentageFull = TownshipDeed.GetPercentageOfGuildedHousesInArea(thisStone.TownshipCenter, thisStone.Map, thisStone.Extended ? TownshipStone.EXTENDED_RADIUS : TownshipStone.INITIAL_RADIUS, thisStone.Guild, false);
                        dHousePercentageWithAlliedIgnored = TownshipDeed.GetPercentageOfGuildedHousesInArea(thisStone.TownshipCenter, thisStone.Map, thisStone.Extended ? TownshipStone.EXTENDED_RADIUS : TownshipStone.INITIAL_RADIUS, thisStone.Guild, true);
                    }
                    catch (Exception cpe)
                    {
                        Server.Commands.LogHelper.LogException(cpe);
                    }

                    AddHtml(20, 50, 300, 35, string.Format("{0} of {1}", TownshipStone.GetTownshipSizeDesc(thisStone.ActivityLevel), thisStone.GuildName), false, false);
                    AddHtml(20, 80, 300, 35, string.Format("Last Week Total: {0}", thisStone.LastActualWeekNumber), false, false);
                    AddHtml(20, 110, 300, 35, string.Format("Weeks at current level: {0}", thisStone.WeeksAtThisLevel), false, false);
                    AddHtml(20, 140, 300, 35, string.Format("Last 7 days: {0},{1},{2},{3},{4},{5},{6}", thisStone.Visitors0, thisStone.Visitors1, thisStone.Visitors2, thisStone.Visitors3, thisStone.Visitors4, thisStone.Visitors5, thisStone.Visitors6), false, false);
                    AddHtml(20, 170, 300, 35, string.Format("Current day index: {0}", thisStone.VisitorsIndex), false, false);
                    AddHtml(20, 200, 300, 35, string.Format("Stone Location: ({0}, {1}, {2})", thisStone.X, thisStone.Y, thisStone.Z), false, false);
                    AddHtml(20, 230, 300, 35, string.Format("House Ownership Percentage: all: {0:0.00} -- allies ignored: {1:0.00}", dHousePercentageFull, dHousePercentageWithAlliedIgnored), false, false);

                    AddButton(20, 390, 4005, 4007, 997, GumpButtonType.Reply, 0);
                    AddHtml(55, 390, 470, 30, "Back to Main Page", false, false); // EXIT
                }
                else
                {
                    SortList(sort);

                    ShowList(page);

                    int numPages = (m_List.Count / 10) + 1;
                    //need next/back buttons
                    if (page > 0)
                    {
                        AddButton(20, 390, 4005, 4007, 998, GumpButtonType.Reply, 0);
                        AddHtml(55, 390, 470, 30, "Previous", false, false); // EXIT
                    }
                    if (page + 1 < numPages)
                    {
                        AddButton(150, 390, 4005, 4007, 999, GumpButtonType.Reply, 0);
                        AddHtml(185, 390, 470, 30, "Next", false, false); // EXIT
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }
        }

        private void GenerateList()
        {
            m_List = TownshipStone.AllTownshipStones;
        }

        private void SortList(Sort sort)
        {
            if (sort != m_Sort)
            {
                //Sort it
                //not implemented

                sort = m_Sort;
            }
        }

        private void ShowList(int page)
        {
            try
            {
                int perpage = 13;
                int y = 40;

                AddHtml(50, y, 300, 25, "Guild", false, false);
                AddHtml(100, y, 300, 25, "Size", false, false);
                AddHtml(160, y, 300, 25, "Growth", false, false);
                AddHtml(220, y, 300, 25, "LW#", false, false);
                AddHtml(250, y, 300, 25, "ALCalc", false, false);
                AddHtml(330, y, 300, 25, "Center", false, false);
                y += 30;
                AddHtml(180, y, 300, 25, "<HR>", false, false);
                y += 20;

                int start = page * perpage;
                int end = start + perpage;
                for (int i = start; i < end && i < m_List.Count; i++)
                {
                    try
                    {
                        TownshipStone ts = m_List[i];

                        AddButton(20, y, 4005, 4007, i + 1000, GumpButtonType.Reply, 0);
                        AddHtml(60, y, 300, 25, ts.Guild.Abbreviation, false, false);
                        AddHtml(100, y, 300, 25, ts.Extended ? "(X):" : "" + TownshipStone.GetTownshipSizeDesc(ts.ActivityLevel), false, false);
                        AddHtml(160, y, 300, 25, TownshipStone.GetTownshipActivityDesc(ts.LastActivityLevel), false, false);
                        AddHtml(220, y, 300, 25, ts.LastActualWeekNumber.ToString(), false, false);
                        AddHtml(250, y, 200, 25, ts.ALLastCalculated.ToString("MM/dd"), false, false);
                        AddHtml(300, y, 200, 25, ts.TownshipCenter.ToString(), false, false);

                        AddButton(470, y, 4005, 4007, i + 5000, GumpButtonType.Reply, 0);
                        AddHtml(510, y, 20, 25, "Go", false, false);

                        y += 25;
                    }
                    catch (Exception ex)
                    {
                        Server.Commands.LogHelper.LogException(ex, "inner");
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            try
            {
                int button = info.ButtonID;

                int page = 0;

                if (button == 998)
                {
                    page = m_Page - 1;
                    if (page < 0) page = 0;
                    m_Mobile.SendGump(new TownshipStaffGump(m_Mobile, Sort.DEFAULT, page, m_List));
                }
                else if (button == 999)
                {
                    page = m_Page + 1;
                    m_Mobile.SendGump(new TownshipStaffGump(m_Mobile, Sort.DEFAULT, page, m_List));
                }
                else if (button == 997)
                {
                    m_Mobile.SendGump(new TownshipStaffGump(m_Mobile, Sort.DEFAULT, page, m_List));
                }
                else if (button >= 1000)
                {
                    if (button >= 5000)
                    {
                        int index = button - 5000;
                        bool moved = false;
                        if (index >= 0 && index < m_List.Count)
                        {
                            TownshipStone ts = m_List[index];
                            if (ts != null)
                            {
                                moved = true;
                                //m_Mobile.MoveToWorld(ts.TownshipCenter, ts.Map);
                                m_Mobile.MoveToWorld(ts.Location, ts.Map);
                            }
                        }
                        if (!moved)
                        {
                            m_Mobile.SendMessage("Error sending you to township stone");
                        }
                        m_Mobile.SendGump(new TownshipStaffGump(m_Mobile, Sort.DEFAULT, page, m_List));
                    }
                    else
                    {
                        m_Mobile.SendGump(new TownshipStaffGump(m_Mobile, Sort.DEFAULT, page, m_List, true, button - 1000));
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }
        }

    }
}
