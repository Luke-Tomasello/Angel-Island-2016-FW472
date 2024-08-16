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

/* Scripts/Engines/Township/TownshipStone.cs
 * CHANGELOG:
 *	05/06/09, plasma
 *		Added new security method that asserts the same rules for button creation
 *		Prevents exploits as its possible with razor to respond to any gump with any ID.
 * 11/26/08, Pix
 *		Added house distance check and teleporter distance check to moving township stone.
 *	8/3/08, Pix
 *		Change for CanExtend() call - now returns a reason.
 *		Also fixed placement of staff-only button.
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	12/11/07, Pix
 *		Since allies can now access stone, make sure they're restricted to just
 *		view access (non-guildmaster and non-admin) even if they're co-owned to the house.
 *	Pix: 4/30/07,
 *		Resticted Update Enemy list button.
 *		Added activity indicator.
 *	Pix: 4/19/07
 *		Added dials for all fees/charges and modifiers.
 */

using Server.Guilds;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Gumps
{
    public class TownshipGump : Gump
    {
        private enum Buttons
        {
            EXIT,
            MAINPAGE,
            WORLDSTAFF,
            ADDGOLD,
            PURCHASENPCPAGE,
            MANAGENPCPAGE,
            LAWLEVELPAGE,
            TRAVELPAGE,
            LASTFEESPAGE,
            DAILYFEEPAGE,
            LASTDEPOSITSPAGE,
            DELETETOWNSHIPPAGE,
            BUYEMISSARY,
            BUYEVOCATOR,
            BUYBANKER,
            BUYINNKEEPER,
            BUYMAGE,
            BUYPROVISIONER,
            BUYALCHEMIST,
            BUYANIMALTRAINER,
            BUYMAGETRAINER,
            BUYARMSTRAINER,
            BUYROGUETRAINER,
            BUYTOWNCRIER,
            BUYLOOKOUT,
            BUYFIGHTBROKER,
            BUYAUCTIONEER,
            DISMISSNPC,
            LL_STANDARD,
            LL_LAWLESS,
            LL_AUTHORITY,
            DELETETOWNSHIP,
            TOGGLERECALLIN,
            TOGGLERECALLOUT,
            TOGGLEGATEIN,
            TOGGLEGATEOUT,
            ENEMYLIST,
            SYNCENEMYLIST,
            MOVESTONE,
            TOGGLEINHOUSEONLYNPC,
            NULL
        }

        private enum TextFields
        {
            GOLDAMOUNT,
            NULL
        }

        private enum Pages
        {
            MAIN,
            BUYNPC,
            MANAGENPCS,
            LAWLEVEL,
            TRAVEL,
            LASTFEES,
            DAILYFEES,
            LASTDEPOSITS,
            DELETETOWNSHIPPAGE,
            ENEMYLISTPAGE,
            NULL
        }

        //		private enum Switches
        //		{
        //			BANKER,
        //			MAGE, 
        //			PROVISIONER,
        //			ALCHEMIST,
        //			ANIMALTRAINER,
        //			MAGETRAINER,
        //			ARMSTRAINER,
        //			ROGUETRAINER,
        //			NULL
        //		}

        private Mobile m_Mobile;
        private TownshipStone m_Stone;
        private bool m_AdminAccess = false;
        private bool m_GuildMasterAccess = false;

        private void ConfigureGump()
        {
            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 430, 5054);
            //AddBackground(10, 10, 530, 420, 3000);
            AddBackground(10, 10, 530, 360, 3000);
            AddBackground(10, 380, 530, 40, 3000);

            AddHtml(20, 15, 400, 35, "Township Controller - " + m_Stone.GuildName, false, false);
        }

        public TownshipGump(Mobile mobile, TownshipStone stone)
            : this(mobile, stone, Pages.MAIN)
        {
        }

        private TownshipGump(Mobile mobile, TownshipStone stone, TownshipGump.Pages page)
            : base(20, 30)
        {
            mobile.CloseGump(typeof(TownshipGump));
            m_Mobile = mobile;
            m_Stone = stone;

            BaseHouse house = BaseHouse.FindHouseAt(stone);
            if (house != null && stone.Guild == mobile.Guild)
            {
                if (house.IsCoOwner(mobile))
                {
                    m_AdminAccess = true;
                }

                if (stone != null && stone.Guild != null && stone.Guild.Leader == m_Mobile)
                {
                    m_AdminAccess = true;
                    m_GuildMasterAccess = true;
                }
            }

            //If the stone isn't in a house, then the only member with Admin/Guildmaster access
            // will be the guildmaster.
            if (house == null && stone.Guild == mobile.Guild)
            {
                if (stone.Guild.Leader == m_Mobile)
                {
                    m_AdminAccess = true;
                    m_GuildMasterAccess = true;
                }
            }

            if (mobile.AccessLevel >= AccessLevel.Administrator)
            {
                m_AdminAccess = true;
                m_GuildMasterAccess = true;
            }

            ConfigureGump();

            switch (page)
            {
                case Pages.MAIN:
                    MainPage();
                    break;
                case Pages.BUYNPC:
                    BuyNPCPage();
                    break;
                case Pages.MANAGENPCS:
                    ManageNPCPage();
                    break;
                case Pages.LAWLEVEL:
                    ManageLawLevelPage();
                    break;
                case Pages.TRAVEL:
                    TravelPage();
                    break;
                case Pages.LASTFEES:
                    LastFeesPage();
                    break;
                case Pages.DAILYFEES:
                    DailyFeesPage();
                    break;
                case Pages.LASTDEPOSITS:
                    LastDepositsPage();
                    break;
                case Pages.DELETETOWNSHIPPAGE:
                    DeleteTownshipPage();
                    break;
                case Pages.ENEMYLISTPAGE:
                    EnemyListPage();
                    break;
                default:
                    break;
            }

            if (page == Pages.MAIN)
            {
                if (m_Mobile.AccessLevel >= AccessLevel.Administrator)
                {
                    AddButton(150, 365, 4005, 4007, (int)Buttons.WORLDSTAFF, GumpButtonType.Reply, 0);
                    AddHtml(185, 365, 470, 30, "World Staff region control", false, false);
                }
            }
            else
            {
                AddButton(150, 390, 4005, 4007, (int)Buttons.MAINPAGE, GumpButtonType.Reply, 0);
                AddHtml(185, 390, 470, 30, "Back to Main Page", false, false);
            }

            AddButton(20, 390, 4005, 4007, (int)Buttons.EXIT, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 390, 470, 30, 1011441, false, false); // EXIT
        }

        private void MainPage()
        {
            int y = 50;

            //AddHtml(20, y, 200, 35, "Activity Level: " + m_Stone.ActivityLevel.ToString(), false, false);
            //LastActivityLevel
            AddHtml(20, y, 200, 35, GetSizeString(), false, false);
            AddHtml(300, y, 200, 25, "Established Law: " + m_Stone.LawLevel.ToString(), false, false);

            y += 20;
            if (m_Stone.Extended == false)
            {
                string extendstring = "unknown";
                CanExtendResult cer = m_Stone.CanExtendRegion(null);
                switch (cer)
                {
                    case CanExtendResult.CanExtend:
                        extendstring = "can extend";
                        break;
                    case CanExtendResult.ConflictingRegion:
                        extendstring = "can't extend - conflicting area";
                        break;
                    case CanExtendResult.HousingPercentage:
                        extendstring = "can't extend - insufficient house ownership";
                        break;
                }
                string original = "Physical Size: Original (" + extendstring + ")";
                AddHtml(20, y, 500, 35, original, false, false);
            }
            else
            {
                AddHtml(20, y, 200, 35, "Physical Size: Extended", false, false);
            }

            //Pix: I'm not sure that we should show them how the visitors increase or the thresholds for
            // town growth, so stop showing weekly visitors
            // And township NPC count is silly to have since they can see the NPCs on the dismiss page
            //			y += 20;
            //			AddHtml(20, y, 200, 35, "Weekly visitors: " + m_Stone.WeeklyVisitors, false, false);
            //			AddHtml(200, y, 200, 35, "Township NPCS: " + m_Stone.TownshipNPCCount, false, false);

            y += 40;
            AddHtml(20, y, 200, 35, "NPC Charge per RL Day: " + m_Stone.NPCChargePerRLDay, false, false);
            y += 20;
            string chargestring = string.Format("Total Charge per RL Day: {0} (funds for {1} days held)", m_Stone.TotalFeePerDay, m_Stone.RLDaysLeftInFund.ToString("0.00"));
            AddHtml(20, y, 400, 35, chargestring, false, false);

            y += 20;
            AddButtonLabeled(20, y, (int)Buttons.LASTFEESPAGE, "Last charges record");
            AddButtonLabeled(200, y, (int)Buttons.DAILYFEEPAGE, "Last daily charges breakdown");

            y += 20;
            AddButtonLabeled(20, y, (int)Buttons.LASTDEPOSITSPAGE, "Last deposits record");

            y += 30;
            AddHtml(20, y, 180, 35, "Gold Held: " + m_Stone.GoldHeld, false, false);
            AddButtonLabeled(270, y, (int)Buttons.ADDGOLD, "Add Gold to Fund");

            y += 30;
            AddButtonLabeled(20, y, (int)Buttons.ENEMYLIST, "Enemy List");

            if (this.m_AdminAccess)
            {
                y += 30;
                if (m_Stone.ActivityLevel != Server.Township.ActivityLevel.NONE)
                {
                    AddButtonLabeled(20, y, (int)Buttons.PURCHASENPCPAGE, "Buy Township NPC");
                }
                if (m_Stone.TownshipNPCCount > 0)
                {
                    AddButtonLabeled(200, y, (int)Buttons.MANAGENPCPAGE, "Manage Township NPCs");
                }

                y += 30;
                AddButtonLabeled(20, y, (int)Buttons.LAWLEVELPAGE, "Change Law Level");

                y += 30;
                AddButtonLabeled(20, y, (int)Buttons.TRAVELPAGE, "Change Travel Rules");

                if (this.m_GuildMasterAccess)
                {
                    y += 30;
                    AddButtonLabeled(400, 390, (int)Buttons.DELETETOWNSHIPPAGE, "Delete Township");
                    AddButtonLabeled(200, 390, (int)Buttons.MOVESTONE, "Move Stone");
                }
            }
        }

        private string GetSizeString()
        {
            StringBuilder sizeSB = new StringBuilder();
            sizeSB.Append("Size: ");
            sizeSB.Append(TownshipStone.GetTownshipSizeDesc(m_Stone.ActivityLevel));

            /*			int diff = (int)m_Stone.ActivityLevel - (int)m_Stone.LastActivityLevel;

						if (diff < 0)
						{
							sizeSB.Append(" (");
							for (int i = Math.Abs(diff); i > 0; i--)
							{
								sizeSB.Append("-");
							}
							sizeSB.Append(")");
						}
						else if (diff > 0)
						{
							sizeSB.Append(" (");
							for (int i = Math.Abs(diff); i > 0; i--)
							{
								sizeSB.Append("+");
							}
							sizeSB.Append(")");
						}
						else
						{
							sizeSB.Append(" (=)");
						}
			 */
            int lastSize = (int)m_Stone.LastActivityLevel;
            if (lastSize > 0)
            {
                sizeSB.Append("  Activity: ");
                for (int i = 0; i < lastSize; i++)
                {
                    sizeSB.Append("*");
                }
            }

            return sizeSB.ToString();
        }

        private const int irid = 210;
        private const int aid = 211;
        private void BuyNPCPage()
        {
            int y = 50;

            AddHtml(20, y, 500, 35, "Township NPCs cost the initial amount listed, plus a daily charge depending on the type of NPC.", false, false);
            y += 35;
            AddHtml(20, y, 500, 35, "As your activity level increases, you will have more NPCs to purchase.", false, false);
            y += 25;

            AddHtml(20, y, 200, 35, "Purchase NPC:", false, false);

            int firstColumn = 20;
            int secondColumn = 300;

            y += 20;
            if (m_Stone.ActivityLevel >= Township.ActivityLevel.MEDIUM)
            {
                AddButtonLabeled(firstColumn, y, (int)Buttons.BUYEMISSARY, "Buy Emissary (" + Township.TownshipSettings.EmissaryCharge + ")");
                AddButtonLabeled(secondColumn, y, (int)Buttons.BUYEVOCATOR, "Buy Evocator (" + Township.TownshipSettings.EvocatorCharge + ")");
                y += 20;
            }
            AddButtonLabeled(firstColumn, y, (int)Buttons.BUYPROVISIONER, "Buy Provisioner (" + Township.TownshipSettings.ProvisionerCharge + ")");
            AddButtonLabeled(secondColumn, y, (int)Buttons.BUYINNKEEPER, "Buy Innkeeper (" + Township.TownshipSettings.InnkeeperCharge + ")");
            y += 20;
            if (m_Stone.ActivityLevel >= Township.ActivityLevel.HIGH)
            {
                AddButtonLabeled(firstColumn, y, (int)Buttons.BUYBANKER, "Buy Banker (" + Township.TownshipSettings.BankerCharge + ")");
                AddButtonLabeled(secondColumn, y, (int)Buttons.BUYANIMALTRAINER, "Buy Animal Trainer (" + Township.TownshipSettings.AnimalTrainerCharge + ")");
                y += 20;
            }
            if (m_Stone.ActivityLevel >= Township.ActivityLevel.MEDIUM)
            {
                AddButtonLabeled(firstColumn, y, (int)Buttons.BUYALCHEMIST, "Buy Alchemist Shopkeeper (" + Township.TownshipSettings.AlchemistCharge + ")");
                AddButtonLabeled(secondColumn, y, (int)Buttons.BUYMAGE, "Buy Mage Shopkeeper (" + Township.TownshipSettings.MageCharge + ")");
                y += 20;
            }
            AddButtonLabeled(firstColumn, y, (int)Buttons.BUYMAGETRAINER, "Buy Mage Trainer (" + Township.TownshipSettings.MageTrainerCharge + ")");
            AddButtonLabeled(secondColumn, y, (int)Buttons.BUYARMSTRAINER, "Buy Arms Trainer (" + Township.TownshipSettings.ArmsTrainerCharge + ")");
            y += 20;
            AddButtonLabeled(firstColumn, y, (int)Buttons.BUYROGUETRAINER, "Buy Rogue Trainer (" + Township.TownshipSettings.RogueTrainerCharge + ")");
            AddButtonLabeled(secondColumn, y, (int)Buttons.BUYLOOKOUT, "Buy Lookout (" + Township.TownshipSettings.LookoutCharge + ")");
            y += 20;
            AddButtonLabeled(firstColumn, y, (int)Buttons.BUYFIGHTBROKER, "Buy Fight Broker (" + Township.TownshipSettings.FightBrokerCharge + ")");
            AddButtonLabeled(secondColumn, y, (int)Buttons.BUYAUCTIONEER, "Buy Auctioneer (" + Township.TownshipSettings.AuctioneerCharge + ")");
            y += 20;

            if (!m_Stone.Extended && m_Stone.CanExtendRegion(null) == CanExtendResult.CanExtend && m_Stone.ActivityLevel >= Township.ActivityLevel.MEDIUM)
            {
                AddButtonLabeled(firstColumn, y, (int)Buttons.BUYTOWNCRIER, "Buy Town Crier (" + Township.TownshipSettings.TownCrierCharge + ")");
                y += 20;
            }

        }

        private void ManageNPCPage()
        {
            int y = 50;

            AddHtml(20, y, 500, 35, "Township NPCs cost the initial amount listed, plus a daily charge.", false, false);
            y += 25;

            AddHtml(20, y, 200, 35, "Manage NPC:", false, false);
            y += 30;

            string npcMessage = string.Format("Toggle NPC Interaction (Currently {0})",
                m_Stone.OutsideNPCInterctionAllowed ? "Outside Houses Allowed" : "Inside Houses Only");
            AddButtonLabeled(20, y, (int)Buttons.TOGGLEINHOUSEONLYNPC, npcMessage, 400);
            y += 30;

            List<Mobile> npcs = m_Stone.TownshipMobiles;

            int i = 0;
            foreach (Mobile m in npcs)
            {
                if (m != null)
                {
                    y += 20;
                    string text = string.Format("{0} -- {1}", m.Name, m.Title);
                    AddButtonLabeled(20, y, (int)Buttons.DISMISSNPC + 10000 + i, "Dismiss :: " + text, 400);
                    i++;
                }
            }
        }

        public void ManageLawLevelPage()
        {
            int y = 50;

            AddHtml(20, y, 400, 35, "Lawlevels cost the initial charge to change the lawlevel, plus a daily charge based on the activity in the town.", false, false);

            y += 50;

            string current = "Standard";
            if (m_Stone.LawLevel == Server.Township.LawLevel.AUTHORITY)
            {
                current = "Grant of Authority";
            }
            else if (m_Stone.LawLevel == Server.Township.LawLevel.LAWLESS)
            {
                current = "Lawless";
            }

            AddHtml(20, y, 400, 35, "Current Law Level: " + current, false, false);

            y += 30;

            if (m_Stone.LawLevel != Server.Township.LawLevel.NONE)
            {
                AddButtonLabeled(20, y, (int)Buttons.LL_STANDARD, "Change to Standard (" + Township.TownshipSettings.LawNormCharge + ")", 400);
                y += 30;
            }
            if (m_Stone.LawLevel != Server.Township.LawLevel.LAWLESS)
            {
                AddButtonLabeled(20, y, (int)Buttons.LL_LAWLESS, "Change to Lawless (" + Township.TownshipSettings.LawlessCharge + ")", 400);
                y += 30;
            }
            if (m_Stone.LawLevel != Server.Township.LawLevel.AUTHORITY)
            {
                if (m_Stone.HasEmissaryNPC)
                {
                    AddButtonLabeled(20, y, (int)Buttons.LL_AUTHORITY, "Change to Grant of Authority (" + Township.TownshipSettings.LawAuthCharge + ")", 400);
                    y += 30;
                }
                else
                {
                    AddHtml(20, y, 400, 35, "You must purchase and place an Emissary before purchasing a Grant of Authority", false, false);
                    y += 30;
                }
            }

            y += 20;
            AddHtml(20, y, 400, 35, "Daily fees based on current activity level:", false, false);
            y += 30;
            AddHtml(20, y, 400, 35, "Standard: " + TownshipStone.CalculateLawlevelFee(Township.LawLevel.NONE, m_Stone.LastActivityLevel), false, false);
            y += 30;
            AddHtml(20, y, 400, 35, "Lawless: " + TownshipStone.CalculateLawlevelFee(Township.LawLevel.LAWLESS, m_Stone.LastActivityLevel), false, false);
            y += 30;
            AddHtml(20, y, 400, 35, "Grant of Authority: " + TownshipStone.CalculateLawlevelFee(Township.LawLevel.AUTHORITY, m_Stone.LastActivityLevel), false, false);

        }

        public void TravelPage()
        {
            int y = 50;
            AddHtml(20, y, 400, 35, "Travel Restrictions Settings", false, false);
            y += 30;
            AddHtml(20, y, 400, 35, "Changing the travel settings costs a one-time fee, and anything other than normal settings cost a daily fee.", false, false);

            bool hasEvocator = m_Stone.HasEvocatorNPC;
            y += 50;
            //current settings:
            if (hasEvocator)
            {
                AddHtml(20, y, 400, 35, "Changing each option costs " + Township.TownshipSettings.ChangeTravelCharge + " gold.", false, false);
                y += 30;

                AddHtml(20, y, 400, 35, "Recall In: " + (m_Stone.NoRecallInto ? "Disabled" : "Enabled"), false, false);
                if (m_Stone.LastToggleRecallIn < DateTime.Now.AddMinutes(-30.0))
                {
                    AddButtonLabeled(300, y, (int)Buttons.TOGGLERECALLIN, (m_Stone.NoRecallInto ? "Enable" : "Disable"));
                }
                else
                {
                    AddHtml(240, y, 300, 35, "Can only be changed once per 30 minutes", false, false);
                }
                y += 30;
                AddHtml(20, y, 400, 35, "Recall Out: " + (m_Stone.NoRecallOut ? "Disabled" : "Enabled"), false, false);
                if (m_Stone.LastToggleRecallOut < DateTime.Now.AddMinutes(-30.0))
                {
                    AddButtonLabeled(300, y, (int)Buttons.TOGGLERECALLOUT, (m_Stone.NoRecallOut ? "Enable" : "Disable"));
                }
                else
                {
                    AddHtml(240, y, 300, 35, "Can only be changed once per 30 minutes", false, false);
                }
                y += 30;
                AddHtml(20, y, 400, 35, "Gate Travel In: " + (m_Stone.NoGateInto ? "Disabled" : "Enabled"), false, false);
                if (m_Stone.LastToggleGateIn < DateTime.Now.AddMinutes(-30.0))
                {
                    AddButtonLabeled(300, y, (int)Buttons.TOGGLEGATEIN, (m_Stone.NoGateInto ? "Enable" : "Disable"));
                }
                else
                {
                    AddHtml(240, y, 300, 35, "Can only be changed once per 30 minutes", false, false);
                }
                y += 30;
                AddHtml(20, y, 400, 35, "Gate Travel Out: " + (m_Stone.NoGateOut ? "Disabled" : "Enabled"), false, false);
                if (m_Stone.LastToggleGateOut < DateTime.Now.AddMinutes(-30.0))
                {
                    AddButtonLabeled(300, y, (int)Buttons.TOGGLEGATEOUT, (m_Stone.NoGateOut ? "Enable" : "Disable"));
                }
                else
                {
                    AddHtml(240, y, 300, 35, "Can only be changed once per 30 minutes", false, false);
                }
                y += 30;
            }
            else
            {
                m_Stone.NoRecallInto = false;
                m_Stone.NoRecallOut = false;
                m_Stone.NoGateInto = false;
                m_Stone.NoGateOut = false;

                AddHtml(20, y, 400, 35, "You must purchase and place an Evocator before being able to set travel spell limitations.", false, false);
                y += 50;
                AddHtml(20, y, 400, 35, "Recall In: Enabled", false, false);
                y += 30;
                AddHtml(20, y, 400, 35, "Recall Out: Enabled", false, false);
                y += 30;
                AddHtml(20, y, 400, 35, "Gate Travel In: Enabled", false, false);
                y += 30;
                AddHtml(20, y, 400, 35, "Gate Travel Out: Enabled", false, false);
                y += 30;
            }

        }

        public void LastDepositsPage()
        {
            int y = 50;
            AddHtml(20, y, 400, 35, "Record of last deposits", false, false);

            y += 50;

            AddHtml(20, y, 400, 400, m_Stone.LastDepositHTML, false, false);
        }
        public void LastFeesPage()
        {
            int y = 50;
            AddHtml(20, y, 400, 35, "Record of last fees charged", false, false);

            y += 50;

            AddHtml(20, y, 400, 400, m_Stone.LastFeesHTML, false, false);
        }
        public void DailyFeesPage()
        {
            int y = 50;
            AddHtml(20, y, 400, 35, "Last daily fee breakdown", false, false);

            y += 50;

            AddHtml(20, y, 400, 400, m_Stone.DailyFeesHTML, false, false);
        }

        public void DeleteTownshipPage()
        {
            int y = 50;

            AddHtml(20, y, 400, 35, "Delete Township??", false, false);

            y += 100;
            AddButtonLabeled(50, y, (int)Buttons.DELETETOWNSHIP, "YES, delete it forever");
        }

        public void EnemyListPage()
        {
            int y = 50;
            AddHtml(20, y, 200, 35, "Enemy List", false, false);
            if (m_AdminAccess)
            {
                AddButtonLabeled(225, y, (int)Buttons.SYNCENEMYLIST, "Update enemy list (" + Township.TownshipSettings.UpdateEnemyCharge + " gold)");
            }

            y += 30;
            int perpage = 9;

            int numberofpages = (m_Stone.Enemies.Count / perpage) + 1;

            for (int i = 0; i < m_Stone.Enemies.Count; ++i)
            {
                if ((i % perpage) == 0)
                {
                    if (i != 0)
                    {
                        AddButton(300, 350, 4005, 4007, 0, GumpButtonType.Page, (i / perpage) + 1);
                        AddHtmlLocalized(335, 350, 300, 35, 1011066, false, false); // Next page
                    }

                    int pagenumber = (i / perpage) + 1;
                    AddPage(pagenumber);

                    bool notlastpage = (pagenumber < numberofpages);

                    if (notlastpage)
                    {
                        AddButton(150, 390, 4005, 4007, (int)Buttons.MAINPAGE, GumpButtonType.Reply, 0);
                        AddHtml(185, 390, 470, 30, "Back to Main Page", false, false);

                        AddButton(20, 390, 4005, 4007, (int)Buttons.EXIT, GumpButtonType.Reply, 0);
                        AddHtmlLocalized(55, 390, 470, 30, 1011441, false, false); // EXIT
                    }

                    if (i != 0)
                    {
                        AddButton(20, 350, 4014, 4016, 0, GumpButtonType.Page, (i / perpage));
                        AddHtmlLocalized(55, 350, 300, 35, 1011067, false, false); // Previous page
                    }
                }

                Mobile m = (Mobile)m_Stone.Enemies[i];
                AddLabel(20, y + ((i % perpage) * 30), 0, m.Name);
            }

        }

        #region Utility gump functions
        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }
        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            //AddHtml(x + 35, y, 240, 20, Color(text, LabelColor), false, false);
            AddHtml(x + 35, y, 240, 20, text, false, false);
        }
        public void AddButtonLabeled(int x, int y, int buttonID, string text, int textwidth)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            //AddHtml(x + 35, y, 240, 20, Color(text, LabelColor), false, false);
            AddHtml(x + 35, y, textwidth, 20, text, false, false);
        }
        #endregion


        public static bool BadMember(Mobile m, Guild g)
        {
            return false;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (BadMember(m_Mobile, m_Stone.Guild))
                return;

            int vendornumber = 0;
            int button = info.ButtonID;

            if (!ValidateResponseSecurity(sender.Mobile, info.ButtonID))
                return;

            if (button > 10000)
            {
                vendornumber = button - (int)Buttons.DISMISSNPC - 10000;
                button = (int)Buttons.DISMISSNPC;
            }

            switch (button)
            {
                case (int)Buttons.EXIT:
                    break;
                case (int)Buttons.MAINPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.MAIN));
                    break;
                case (int)Buttons.WORLDSTAFF:
                    m_Stone.AccessBaseController(m_Mobile);
                    break;
                case (int)Buttons.PURCHASENPCPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.BUYNPC));
                    break;
                case (int)Buttons.MANAGENPCPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.MANAGENPCS));
                    break;
                case (int)Buttons.LAWLEVELPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.LAWLEVEL));
                    break;
                case (int)Buttons.TRAVELPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.TRAVEL));
                    break;
                case (int)Buttons.LASTFEESPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.LASTFEES));
                    break;
                case (int)Buttons.DAILYFEEPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.DAILYFEES));
                    break;
                case (int)Buttons.LASTDEPOSITSPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.LASTDEPOSITS));
                    break;
                case (int)Buttons.DELETETOWNSHIPPAGE:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.DELETETOWNSHIPPAGE));
                    break;
                case (int)Buttons.DELETETOWNSHIP:
                    m_Stone.Delete();
                    m_Mobile.SendMessage("Your township has been deleted");
                    break;
                case (int)Buttons.MOVESTONE:
                    if (this.m_GuildMasterAccess)
                    {
                        m_Mobile.SendMessage("Target the spot within your township where you want your stone to reside.");
                        m_Mobile.Target = new MoveTownshipStoneTarget(m_Mobile, m_Stone);
                    }
                    break;
                case (int)Buttons.ADDGOLD:
                    m_Mobile.SendMessage("Target the gold or bank check to add funds to the township.");
                    m_Mobile.Target = new AddTownshipGoldTarget(m_Mobile, m_Stone);
                    break;
                case (int)Buttons.TOGGLERECALLIN:
                    if (m_Stone.LastToggleRecallIn < DateTime.Now.AddMinutes(-30.0))
                    {
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.ChangeTravelCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed Recall In: " + Township.TownshipSettings.ChangeTravelCharge);
                            m_Stone.GoldHeld -= Township.TownshipSettings.ChangeTravelCharge;
                            m_Stone.NoRecallInto = !m_Stone.NoRecallInto;
                            m_Stone.LastToggleRecallIn = DateTime.Now;
                            m_Mobile.SendMessage("Recall in is now " + (m_Stone.NoRecallInto ? "disabled." : "enabled."));
                        }
                        else
                        {
                            m_Mobile.SendMessage("You don't have enough in your township fund to change that.");
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("You can only change this once per 30 minutes.");
                    }
                    //resend gump
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    break;
                case (int)Buttons.TOGGLERECALLOUT:
                    if (m_Stone.LastToggleRecallOut < DateTime.Now.AddMinutes(-30.0))
                    {
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.ChangeTravelCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed Recall Out: " + Township.TownshipSettings.ChangeTravelCharge);
                            m_Stone.GoldHeld -= Township.TownshipSettings.ChangeTravelCharge;
                            m_Stone.NoRecallOut = !m_Stone.NoRecallOut;
                            m_Stone.LastToggleRecallOut = DateTime.Now;
                            m_Mobile.SendMessage("Recall out is now " + (m_Stone.NoRecallOut ? "disabled." : "enabled."));
                        }
                        else
                        {
                            m_Mobile.SendMessage("You don't have enough in your township fund to change that.");
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("You can only change this once per 30 minutes.");
                    }
                    //resend gump
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    break;
                case (int)Buttons.TOGGLEGATEIN:
                    if (m_Stone.LastToggleGateIn < DateTime.Now.AddMinutes(-30.0))
                    {
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.ChangeTravelCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed GateTravel In: " + Township.TownshipSettings.ChangeTravelCharge);
                            m_Stone.GoldHeld -= Township.TownshipSettings.ChangeTravelCharge;
                            m_Stone.NoGateInto = !m_Stone.NoGateInto;
                            m_Stone.LastToggleGateIn = DateTime.Now;
                            m_Mobile.SendMessage("Gate Travel in is now " + (m_Stone.NoGateInto ? "disabled." : "enabled."));
                        }
                        else
                        {
                            m_Mobile.SendMessage("You don't have enough in your township fund to change that.");
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("You can only change this once per 30 minutes.");
                    }
                    //resend gump
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    break;
                case (int)Buttons.TOGGLEGATEOUT:
                    if (m_Stone.LastToggleGateOut < DateTime.Now.AddMinutes(-30.0))
                    {
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.ChangeTravelCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed GateTravel Out: " + Township.TownshipSettings.ChangeTravelCharge);
                            m_Stone.GoldHeld -= Township.TownshipSettings.ChangeTravelCharge;
                            m_Stone.NoGateOut = !m_Stone.NoGateOut;
                            m_Stone.LastToggleGateOut = DateTime.Now;
                            m_Mobile.SendMessage("Gate Travel out is now " + (m_Stone.NoGateOut ? "disabled." : "enabled."));
                        }
                        else
                        {
                            m_Mobile.SendMessage("You don't have enough in your township fund to change that.");
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("You can only change this once per 30 minutes.");
                    }
                    //resend gump
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    break;
                case (int)Buttons.BUYEMISSARY:
                case (int)Buttons.BUYEVOCATOR:
                case (int)Buttons.BUYALCHEMIST:
                case (int)Buttons.BUYANIMALTRAINER:
                case (int)Buttons.BUYARMSTRAINER:
                case (int)Buttons.BUYBANKER:
                case (int)Buttons.BUYINNKEEPER:
                case (int)Buttons.BUYMAGE:
                case (int)Buttons.BUYMAGETRAINER:
                case (int)Buttons.BUYPROVISIONER:
                case (int)Buttons.BUYROGUETRAINER:
                case (int)Buttons.BUYTOWNCRIER:
                case (int)Buttons.BUYLOOKOUT:
                case (int)Buttons.BUYFIGHTBROKER:
                case (int)Buttons.BUYAUCTIONEER:
                    BuyNPCDeed(info.ButtonID);
                    //resend gump
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    break;
                case (int)Buttons.DISMISSNPC:
                    if (vendornumber >= 0 && vendornumber < m_Stone.TownshipMobiles.Count)
                    {
                        bool isSpecial = false;
                        if (m_Stone.TownshipMobiles[vendornumber] is Mobiles.TSEmissary ||
                            m_Stone.TownshipMobiles[vendornumber] is Mobiles.TSEvocator ||
                            m_Stone.TownshipMobiles[vendornumber] is Mobiles.TSTownCrier
                            )
                        {
                            isSpecial = true;
                        }

                        m_Stone.TownshipMobiles[vendornumber].Delete();
                        m_Mobile.SendMessage("The vendor has been dismissed.");

                        //Note: the Check for special npc settings has to be done AFTER the delete
                        if (isSpecial)
                        {
                            //If we're deleting a special NPC, make sure the settings on the
                            // region are set correctly for not having the npc.
                            m_Stone.CheckSpecialNPCRequiredSettings();
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("There was an error dismissing that vendor.");
                    }
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    break;
                //
                // LAW LEVEL CHANGES:
                //
                case (int)Buttons.LL_STANDARD:
                    if (m_Stone.LawLevel != Server.Township.LawLevel.NONE)
                    {
                        //check for funds
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.LawNormCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed Lawlevel to Standard: " + Township.TownshipSettings.LawNormCharge);
                            m_Stone.LawLevel = Server.Township.LawLevel.NONE;
                            m_Stone.GoldHeld -= Township.TownshipSettings.LawNormCharge;
                            m_Mobile.SendMessage("Your town now uses standard LB laws.");
                            m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                        }
                        else
                        {
                            m_Mobile.SendMessage("Your need " + Township.TownshipSettings.LawNormCharge + " gold in your township fund to change to standard law level.");
                            m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("Your law level is already set to standard.");
                        m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    }
                    break;
                case (int)Buttons.LL_AUTHORITY:
                    if (m_Stone.LawLevel != Server.Township.LawLevel.AUTHORITY)
                    {
                        //check for funds
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.LawAuthCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed Lawlevel to Grant of Authority: " + Township.TownshipSettings.LawAuthCharge);
                            m_Stone.LawLevel = Server.Township.LawLevel.AUTHORITY;
                            m_Stone.GoldHeld -= Township.TownshipSettings.LawAuthCharge;
                            m_Mobile.SendMessage("Your town now has a grant of authority from Lord British.");
                            m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                        }
                        else
                        {
                            m_Mobile.SendMessage("Your need " + Township.TownshipSettings.LawAuthCharge + " gold in your township fund to change to a grant of authority.");
                            m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("Your law level is already set to a grant of authority.");
                        m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    }
                    break;
                case (int)Buttons.LL_LAWLESS:
                    if (m_Stone.LawLevel != Server.Township.LawLevel.LAWLESS)
                    {
                        //check for funds
                        if (m_Stone.GoldHeld >= Township.TownshipSettings.LawlessCharge)
                        {
                            m_Stone.AddFeeRecord(m_Mobile.Name + " changed Lawlevel to Lawless: " + Township.TownshipSettings.LawlessCharge);
                            m_Stone.LawLevel = Server.Township.LawLevel.LAWLESS;
                            m_Stone.GoldHeld -= Township.TownshipSettings.LawlessCharge;
                            m_Mobile.SendMessage("Your township is now lawless.");
                            m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));

                            //Check for emissary npc - if it exists, delete it
                            if (m_Stone.HasEmissaryNPC)
                            {
                                m_Stone.RemoveEmissary();
                            }
                        }
                        else
                        {
                            m_Mobile.SendMessage("Your need " + Township.TownshipSettings.LawlessCharge + " gold in your township fund to change to lawless.");
                            m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("Your law level is already set to lawless.");
                        m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone));
                    }
                    break;
                //
                //
                //
                case (int)Buttons.ENEMYLIST:
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.ENEMYLISTPAGE));
                    break;
                case (int)Buttons.SYNCENEMYLIST:
                    if (m_Stone.GoldHeld >= Township.TownshipSettings.UpdateEnemyCharge)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " updated enemy list: " + Township.TownshipSettings.UpdateEnemyCharge);
                        m_Stone.GoldHeld -= Township.TownshipSettings.UpdateEnemyCharge;
                        m_Stone.SyncEnemies();
                        m_Mobile.SendMessage("The enemy list has been updated from the ban lists of the guild and ally houses in the township.");
                    }
                    else
                    {
                        m_Mobile.SendMessage("There are insufficient funds to update the enemy list, you need 1000 gold in the township's fund.");
                    }
                    m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.ENEMYLISTPAGE));
                    break;
                case (int)Buttons.TOGGLEINHOUSEONLYNPC:
                    m_Stone.OutsideNPCInterctionAllowed = !m_Stone.OutsideNPCInterctionAllowed;

                    if (m_Stone.OutsideNPCInterctionAllowed)
                    {
                        m_Mobile.SendMessage("You have set township NPCs to interact outside of houses.");
                        m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.MANAGENPCS));
                    }
                    else
                    {
                        m_Mobile.SendMessage("Township NPCs now only interact inside houses.");
                        m_Mobile.SendGump(new TownshipGump(m_Mobile, m_Stone, Pages.MANAGENPCS));
                    }
                    break;
            }
        }


        private void BuyNPCDeed(int buttonid)
        {
            Item deed = null;
            bool bPurchased = false;
            int cost = 100000;

            switch (buttonid)
            {
                case (int)Buttons.BUYEMISSARY:
                    cost = Township.TownshipSettings.EmissaryCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Emissary: " + cost);
                        deed = new TSEmissaryDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYEVOCATOR:
                    cost = Township.TownshipSettings.EvocatorCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Evocator: " + cost);
                        deed = new TSEvocatorDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYALCHEMIST:
                    cost = Township.TownshipSettings.AlchemistCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Alchemist: " + cost);
                        deed = new TSAlchemistDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYANIMALTRAINER:
                    cost = Township.TownshipSettings.AnimalTrainerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Animal Trainer: " + cost);
                        deed = new TSAnimalTrainerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYBANKER:
                    cost = Township.TownshipSettings.BankerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Banker: " + cost);
                        deed = new TSBankerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYINNKEEPER:
                    cost = Township.TownshipSettings.InnkeeperCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought InnKeeper: " + cost);
                        deed = new TSInnkeeperDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYMAGE:
                    cost = Township.TownshipSettings.MageCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Mage: " + cost);
                        deed = new TSMageDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYPROVISIONER:
                    cost = Township.TownshipSettings.ProvisionerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Provisioner: " + cost);
                        deed = new TSProvisionerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                //Trainers
                case (int)Buttons.BUYARMSTRAINER:
                    cost = Township.TownshipSettings.ArmsTrainerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Arms Trainer: " + cost);
                        deed = new TSArmsTrainerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYMAGETRAINER:
                    cost = Township.TownshipSettings.MageTrainerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Mage Trainer: " + cost);
                        deed = new TSMageTrainerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYROGUETRAINER:
                    cost = Township.TownshipSettings.RogueTrainerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Rogue Trainer: " + cost);
                        deed = new TSRogueTrainerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                //lookout
                case (int)Buttons.BUYLOOKOUT:
                    cost = Township.TownshipSettings.LookoutCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Lookout: " + cost);
                        deed = new TSLookoutDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                //towncrier
                case (int)Buttons.BUYTOWNCRIER:
                    cost = Township.TownshipSettings.TownCrierCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Town Crier: " + cost);
                        deed = new TSTownCrierDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYFIGHTBROKER:
                    cost = Township.TownshipSettings.FightBrokerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought FightBroker: " + cost);
                        deed = new TSFightbrokerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;
                case (int)Buttons.BUYAUCTIONEER:
                    cost = Township.TownshipSettings.AuctioneerCharge;
                    if (m_Stone.GoldHeld >= cost)
                    {
                        m_Stone.AddFeeRecord(m_Mobile.Name + " bought Auctioneer: " + cost);
                        deed = new TSAuctioneerDeed(m_Stone.Guild);
                        bPurchased = true;
                    }
                    break;

            }

            if (bPurchased)
            {
                if (deed != null)
                {
                    if (m_Mobile.Backpack != null)
                    {
                        m_Mobile.Backpack.AddItem(deed);
                        m_Stone.GoldHeld -= cost;
                        m_Mobile.SendMessage("The deed has been placed in your backpack and {0} gold has been removed from the Township's fund.", cost);
                    }
                    else
                    {
                        m_Mobile.SendMessage("Something is wrong with your backpack.");
                    }
                }
                else
                {
                    m_Mobile.SendMessage("There was a problem with the deed.");
                }
            }
            else
            {
                m_Mobile.SendMessage("You do not have the funds in the township to purchase this.");
            }
        }


        #region validation method

        /// <summary>
        /// Plasma: Need this to assert all the rules for displaying buttons, as gump response exploit allows any ID to be sent.
        /// </summary>
        /// <param name="buttonID"></param>
        /// <returns></returns>
        private bool ValidateResponseSecurity(Mobile from, int buttonID)
        {
            //Admins can do wtf they like, even with exploits!
            if (from.AccessLevel == AccessLevel.Administrator) return true;

            switch ((Buttons)buttonID)
            {
                //No security required
                case Buttons.EXIT:
                case Buttons.MAINPAGE:
                case Buttons.ADDGOLD:
                case Buttons.LASTFEESPAGE:
                case Buttons.DAILYFEEPAGE:
                case Buttons.LASTDEPOSITSPAGE:
                case Buttons.ENEMYLIST:
                case Buttons.SYNCENEMYLIST:
                    return true;

                //Admin pages
                case Buttons.MANAGENPCPAGE:
                case Buttons.PURCHASENPCPAGE:
                case Buttons.LAWLEVELPAGE:
                case Buttons.TRAVELPAGE:
                case Buttons.DELETETOWNSHIPPAGE:
                case Buttons.DISMISSNPC:
                    return (m_AdminAccess);
                case Buttons.DELETETOWNSHIP:
                case Buttons.MOVESTONE:
                    return (m_AdminAccess && m_GuildMasterAccess);

                //NPC Options
                case Buttons.TOGGLEINHOUSEONLYNPC:
                    return m_AdminAccess;

                //NPC Purchase				
                //Any activity level
                case Buttons.BUYINNKEEPER:
                case Buttons.BUYPROVISIONER:
                case Buttons.BUYMAGETRAINER:
                case Buttons.BUYARMSTRAINER:
                case Buttons.BUYROGUETRAINER:
                case Buttons.BUYLOOKOUT:
                case Buttons.BUYFIGHTBROKER:
                case Buttons.BUYAUCTIONEER:
                    return (m_AdminAccess);
                // >= Medium
                case Buttons.BUYEMISSARY:
                case Buttons.BUYEVOCATOR:
                case Buttons.BUYMAGE:
                case Buttons.BUYALCHEMIST:
                    return (m_AdminAccess && m_Stone.ActivityLevel >= Township.ActivityLevel.MEDIUM);
                // High
                case Buttons.BUYBANKER:
                case Buttons.BUYANIMALTRAINER:
                    return (m_AdminAccess && m_Stone.ActivityLevel >= Township.ActivityLevel.HIGH);
                //Special case
                case Buttons.BUYTOWNCRIER:
                    return (m_AdminAccess && !m_Stone.Extended && m_Stone.CanExtendRegion(null) == CanExtendResult.CanExtend && m_Stone.ActivityLevel >= Township.ActivityLevel.MEDIUM);

                case Buttons.LL_STANDARD:
                case Buttons.LL_LAWLESS:
                    return (m_AdminAccess);
                case Buttons.LL_AUTHORITY:
                    return (m_AdminAccess && m_Stone.HasEmissaryNPC);

                //Travel
                case Buttons.TOGGLERECALLIN:
                case Buttons.TOGGLERECALLOUT:
                case Buttons.TOGGLEGATEIN:
                case Buttons.TOGGLEGATEOUT:
                    return (m_AdminAccess && m_Stone.HasEvocatorNPC);

                default:
                    break;
            }
            return true;
        }

        #endregion

        private class MoveTownshipStoneTarget : Target
        {
            private Mobile m_Mobile;
            private TownshipStone m_Stone;

            public MoveTownshipStoneTarget(Mobile mobile, TownshipStone stone)
                : base(8, true, TargetFlags.None)
            {
                m_Mobile = mobile;
                m_Stone = stone;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Point3D targetPoint = Point3D.Zero;

                if (targeted is Item)
                {
                    targetPoint = ((Item)targeted).Location;
                }
                else if (targeted is Mobile)
                {
                    targetPoint = ((Mobile)targeted).Location;
                }
                else if (targeted is StaticTarget)
                {
                    targetPoint = ((StaticTarget)targeted).Location;
                }
                else if (targeted is LandTarget)
                {
                    targetPoint = ((LandTarget)targeted).Location;
                }

                if (targetPoint != Point3D.Zero)
                {
                    CustomRegion cr = CustomRegion.FindDRDTRegion(from.Map, targetPoint);
                    if (cr is TownshipRegion)
                    {
                        TownshipRegion tsr = cr as TownshipRegion;
                        if (tsr != null)
                        {
                            if (tsr.TStone != null && this.m_Stone != null && tsr.TStone == this.m_Stone)
                            {
                                bool bCanMove = true;
                                //Make sure this isn't in someone's house who's not in the township's guild:
                                //11/26/08 addition - and that we're not right next to a house that's not in the guild.
                                for (int i = -1; i <= 1 && bCanMove; i++)
                                {
                                    for (int j = -1; j <= 1 && bCanMove; j++)
                                    {
                                        Point3D testTargetPoint = new Point3D(targetPoint.X + i, targetPoint.Y + j, targetPoint.Z);
                                        BaseHouse house = BaseHouse.FindHouseAt(testTargetPoint, from.Map, 20);
                                        if (house != null)
                                        {
                                            if (house.Owner.Guild != m_Stone.Guild)
                                            {
                                                bCanMove = false;
                                                m_Mobile.SendMessage("You can only move your township stone to or by a house that your guild owns.");
                                            }
                                        }
                                    }
                                }

                                IPooledEnumerable itemlist = from.Map.GetItemsInRange(targetPoint, 2);
                                foreach (Item item in itemlist)
                                {
                                    if (item is Teleporter)
                                    {
                                        bCanMove = false;
                                        m_Mobile.SendMessage("You can't move your township stone to that spot.");
                                        break;
                                    }
                                }
                                itemlist.Free();

                                if (bCanMove)
                                {
                                    m_Stone.MoveToWorld(targetPoint);
                                    m_Mobile.SendMessage("You have successfully moved your stone.");
                                }
                            }
                            else
                            {
                                m_Mobile.SendMessage("Mismatching Region Stone and Township Stone.  You may only place your township stone within your township.");
                            }
                        }
                        else
                        {
                            m_Mobile.SendMessage("Cannot determine township region.  You may only place your township stone within your township.");
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("Cannot find township region.  You may only place your township stone within your township.");
                    }
                }
                else
                {
                    m_Mobile.SendMessage("Cannot determine point from target, please retry.");
                }
            }
        }

        private class AddTownshipGoldTarget : Target
        {
            private Mobile m_Mobile;
            private TownshipStone m_Stone;

            public AddTownshipGoldTarget(Mobile mobile, TownshipStone stone)
                : base(11, false, TargetFlags.None)
            {
                m_Mobile = mobile;
                m_Stone = stone;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Gold || targeted is BankCheck)
                {
                    Item item_targeted = targeted as Item;
                    if (item_targeted.RootParent == null ||
                        item_targeted.RootParent == from)
                    {
                        if (item_targeted is Gold)
                        {
                            Gold gold = item_targeted as Gold;
                            int amount = gold.Amount;

                            if (amount + m_Stone.GoldHeld < TownshipStone.MAXGOLDHELD)
                            {
                                m_Stone.GoldHeld += amount;
                                gold.Delete();
                                from.SendMessage("You have deposited {0} into your township fund", amount);
                                m_Stone.AddDepositRecord(string.Format("{0}: {1} deposited {2} gold.", DateTime.Now.ToShortDateString(), from.Name, amount));
                            }
                            else
                            {
                                int difference = TownshipStone.MAXGOLDHELD - m_Stone.GoldHeld;
                                m_Stone.GoldHeld = TownshipStone.MAXGOLDHELD;
                                gold.Amount -= difference;
                                from.SendMessage("You have deposited {0} of {1} into your township fund", difference, amount);
                                m_Stone.AddDepositRecord(string.Format("{0}: {1} deposited {2} gold.", DateTime.Now.ToShortDateString(), from.Name, difference));
                            }
                        }
                        else if (item_targeted is BankCheck)
                        {
                            BankCheck bankcheck = item_targeted as BankCheck;
                            int amount = bankcheck.Worth;

                            if (amount + m_Stone.GoldHeld < TownshipStone.MAXGOLDHELD)
                            {
                                m_Stone.GoldHeld += amount;
                                bankcheck.Delete();
                                from.SendMessage("You have deposited {0} into your township fund", amount);
                                m_Stone.AddDepositRecord(string.Format("{0}: {1} deposited a check worth {2} gold.", DateTime.Now.ToShortDateString(), from.Name, amount));
                            }
                            else
                            {
                                from.SendMessage("That check contains more than the township's fund can hold.");
                            }
                        }
                    }
                    else
                    {
                        from.SendMessage("You must have that in your backpack or on the ground to add it to your township's funds.");
                    }
                }
                else
                {
                    from.SendMessage("You can't add that to the township's fund!");
                }

                from.SendGump(new TownshipGump(m_Mobile, m_Stone));
            }
        }


    }
}
