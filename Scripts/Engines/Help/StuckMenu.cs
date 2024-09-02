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

/* Scripts/Engines/Help/StuckMenu.cs
 * Changelog:
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	3/18/07, Pix
 *		Fixed logic for all cases for help stuck.
 *	3/2/07, Pix
 *		Implemented random selection of 3 towns and 6 shrines for help stuck.
 *	3/1/07, Pix
 *		Tweaks to last night's change.
 *	2/28/07, Pix
 *		Modifications to the help-stuck system.
 *  08/03/06 Taran Kain
 *		Re-added OnTick() reentry blocking, after further investigation. Added explanatory comment.
 *	07/30/06 Taran Kain
 *		Removed OnTick() reentry blocking, could prevent it from running at all
 *	05/30/06, Adam
 *		- Make ValidUseLocation() succeed for staff
 *		- Also add comments and some cleanup.
 *		- Block OnTick() reentry (Open separate SR)
 *  05/01/06 Taran Kain
 *		Consolidated validation checks into ValidUseLocation() to help checking at more than one point in the process.
 *  04/11/06, Kit
 *		Made check IsInSecondAgeArea public for use with testing at help menu to prvent helpstuck in lostlands.
 *	06/04/05, Pix
 *		Force mobile to drop whatever they're holding on HelpStuck teleport.
 *  03/14/05, Lego
 *           added Oc'Nivelle to help stuck menu
 *  11/09/04, Lego
 *           fixed problem with delucia teleporting
 *  9/26/04, Pigpen
 *		Added Buc's Den to Old Lands Locations and added Location in South East T2A to T2A Locations
 *	9/17/04, Pigpen
 *		Changed Cove teleport location, fixing problem of stuck players teleporting into wall of new Rug Shop.
 *  7/12/04, Pix
 *		Fixed problem with help-stuck option where if the person instalogs while
 *		waiting for teleport timer, they get teleported to trammel.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Menus.Questions
{
    public class StuckMenu : Gump
    {
        public const int MAXHELPSTUCKALIVEWEIGHT = 101;

        private static Point3D[] m_Locations = new Point3D[]
            {
                new Point3D( 1522, 1757, 28 ), //0: Britain
				new Point3D( 2005, 2754, 30 ), //1: Trinsic
				new Point3D( 2973,  891,  0 ), //2: Vesper
				new Point3D( 2498,  392,  0 ), //3: Minoc
				new Point3D(  490, 1166,  0 ), //4: Yew
				new Point3D( 2249, 1192,  0 ), //5: Cove
				new Point3D( 2716, 2182,  0 ), //6: Buccaneer's Den
                new Point3D(  825, 1072,  0 ), //7: Oc'Nivelle
				new Point3D( 5720, 3109, -1 ), //8: Papua
				new Point3D( 5216, 4033, 37 ), //9: Delucia
				new Point3D( 5884, 3596, 1 ) //10: South East Lost Lands

				//Added for AutoMove option:
				,
                new Point3D( 1458, 843, 7 ), //11: chaos shrine
				new Point3D( 1858, 873, -1 ), //12: compassion shrine
				new Point3D( 1728, 3528, 3 ), //13: honor shrine
				new Point3D( 1301, 635, 16 ), //14: justice shrine
				new Point3D( 3355, 292, 4 ), //15: sacrifice shrine
				new Point3D( 1600, 2490, 12 ), //16: spirituality shrine
			};

        public static bool IsInSecondAgeArea(Mobile m)
        {
            // Must be redone with a specific external support
            // in order to consider dungeons too

            return (m.X >= 5120 && m.Y >= 2304);
        }

        private Mobile m_Mobile, m_Sender;
        private bool m_MarkUse;

        private bool m_bAdditionalChecks = false;

        private Timer m_Timer;

        public StuckMenu(Mobile beholder, Mobile beheld, bool markUse)
            : this(beholder, beheld, markUse, false)
        {
        }

        public StuckMenu(Mobile beholder, Mobile beheld, bool markUse, bool bAdditionalChecks)
            : base(150, 50)
        {
            m_Sender = beholder;
            m_Mobile = beheld;
            m_MarkUse = markUse;

            m_bAdditionalChecks = bAdditionalChecks;

            Closable = false;
            Dragable = false;

            AddPage(0);

            AddBackground(0, 0, 280, 400, 2600);

            AddHtmlLocalized(50, 25, 170, 40, 1011027, false, false); //Chose a town:

            if (!IsInSecondAgeArea(beheld))
            {
                AddButton(50, 60, 208, 209, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 60, 335, 40, 1011028, false, false); // Britain

                AddButton(50, 95, 208, 209, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 95, 335, 40, 1011029, false, false); // Trinsic

                AddButton(50, 130, 208, 209, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 130, 335, 40, 1011030, false, false); // Vesper

                AddButton(50, 165, 208, 209, 4, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 165, 335, 40, 1011031, false, false); // Minoc

                AddButton(50, 200, 208, 209, 5, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 200, 335, 40, 1011032, false, false); // Yew

                AddButton(50, 235, 208, 209, 6, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 235, 335, 40, 1011033, false, false); // Cove

                AddButton(50, 270, 208, 209, 7, GumpButtonType.Reply, 0);
                AddHtml(75, 270, 335, 40, "Buccaneer's Den", false, false); // Buccaneer's Den

                AddButton(50, 305, 208, 209, 8, GumpButtonType.Reply, 0);
                AddHtml(75, 305, 335, 40, "Oc'Nivelle", false, false); //Oc'Nivelle
            }
            else
            {

                AddButton(50, 60, 208, 209, 9, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 60, 335, 40, 1011057, false, false); // Papua

                AddButton(50, 95, 208, 209, 10, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 95, 335, 40, 1011058, false, false); // Delucia

                AddButton(50, 120, 208, 209, 11, GumpButtonType.Reply, 0);
                AddHtml(75, 130, 335, 40, "South East Lost Lands", false, false); // South East Lost Lands

            }

            AddButton(55, 340, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(90, 340, 320, 40, 1011012, false, false); // CANCEL
        }

        public void AutoSelect()
        {
            if (m_Mobile != null)
            {
                //pick one of:
                //0,3,7,11,12,13,14,15,16
                int[] choices = new int[] { 0, 3, 7, 11, 12, 13, 14, 15, 16 };

                if (m_Mobile.Criminal || m_Mobile.ShortTermMurders >= 5)
                {
                    //don't send murderers or criminals to guarded town
                    choices = new int[] { 7, 11, 12, 13, 14, 15, 16 };
                }

                int index = Utility.Random(choices.Length);
                if (index < 0 || index >= choices.Length) index = 0;

                Teleport(choices[index]);
            }
        }

        public void BeginClose()
        {
            StopClose();

            m_Timer = new CloseTimer(m_Mobile);
            m_Timer.Start();

            m_Mobile.Frozen = true;
        }

        public void StopClose()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Mobile.Frozen = false;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            StopClose();

            if (info.ButtonID == 0)
            {
                if (m_Mobile == m_Sender)
                    m_Mobile.SendLocalizedMessage(1010588); // You choose not to go to any city.
            }
            else if (!IsInSecondAgeArea(m_Mobile))
            {
                if (info.ButtonID >= 1 && info.ButtonID <= 8)
                    Teleport(info.ButtonID - 1);
            }
            else if (info.ButtonID == 9 || info.ButtonID == 10 || info.ButtonID == 11)
            {
                Teleport(info.ButtonID - 1);
            }
        }

        private void Teleport(int index)
        {
            if (m_MarkUse)
            {
                m_Mobile.SendLocalizedMessage(1010589); // You will be teleported within the next two minutes.

                new TeleportTimer(m_Sender, m_Mobile, m_Locations[index], TimeSpan.FromSeconds(10.0 + (Utility.RandomDouble() * 110.0)), m_bAdditionalChecks).Start();
            }
            else
            {
                new TeleportTimer(m_Sender, m_Mobile, m_Locations[index], TimeSpan.Zero, m_bAdditionalChecks).Start();
            }
        }

        public static bool CheckCombat(Mobile m)
        {
            for (int i = 0; i < m.Aggressed.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)m.Aggressed[i];

                if (DateTime.UtcNow - info.LastCombatTime < TimeSpan.FromSeconds(30.0))
                    return true;
            }

            return false;
        }

        public static bool ValidUseLocation(Mobile Sender, Mobile from)
        {
            // sanity test
            PlayerMobile pm = from as PlayerMobile;

            // must be a player
            if (pm == null)
                return false;

            // staff can always use the options
            if (Sender != null && Sender.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            // they are in jail
            if (from.Region != null && from.Region.IsJailRules)
            {
                from.SendLocalizedMessage(1041530, "", 0x35); // You'll need a better jailbreak plan then that!
                return false;
            }

            // they are in prison
            if (pm.Inmate == true)
            {
                from.SendLocalizedMessage(1041530, "", 0x35); // You'll need a better jailbreak plan then that!
                return false;
            }

            // t2a (we wish to trap them there)
            if (IsInSecondAgeArea(from))
            {
                return false;
            }

            // normal case
            if (from.CanUseStuckMenu() && from.Region.CanUseStuckMenu(from) && !CheckCombat(from) && !from.Frozen && !from.Criminal)
            {
                return true;
            }

            // false if not listed here
            return false;
        }

        private class CloseTimer : Timer
        {
            private Mobile m_Mobile;
            private DateTime m_End;

            public CloseTimer(Mobile m)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_End = DateTime.UtcNow + TimeSpan.FromMinutes(3.0);
            }

            protected override void OnTick()
            {
                if (m_Mobile.NetState == null || DateTime.UtcNow > m_End)
                {
                    m_Mobile.Frozen = false;
                    m_Mobile.CloseGump(typeof(StuckMenu));

                    Stop();
                }
                else
                {
                    m_Mobile.Frozen = true;
                }
            }
        }

        public class TeleportTimer : Timer
        {
            private Mobile m_Mobile, m_Sender;
            private Point3D m_Location;
            private DateTime m_End;
            private DateTime m_NextMessage;
            private bool m_bAdditionalChecks = false;

            public TeleportTimer(Mobile s, Mobile m, Point3D loc, TimeSpan delay, bool bAdditionalChecks)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                Priority = TimerPriority.TwoFiftyMS;

                m_Sender = s;
                m_Mobile = m;
                m_Location = loc;
                m_End = DateTime.UtcNow + delay;
                m_NextMessage = DateTime.UtcNow + TimeSpan.FromSeconds(5.0);
                m_bAdditionalChecks = bAdditionalChecks;
            }

            protected override void OnTick()
            {
                if (!Running)
                    return; // this is ok because the underlying Timer is set up to be never-ending; TimerMain() will never
                            // call Stop() on us. THIS IS NOT THE GENERAL CASE! Normally this sort of check is BAD.

                if (DateTime.UtcNow > m_End)
                {
                    m_Mobile.Frozen = false;

                    if (StuckMenu.ValidUseLocation(m_Sender, m_Mobile))
                    {
                        //if mobile is already logged out, leave it where it is!
                        if (m_Mobile.Map != Map.Internal)
                        {
                            //Force mobile to drop whatever they're holding.
                            m_Mobile.DropHolding();

                            bool bMoveMe = true;
                            if (m_bAdditionalChecks)
                            {
                                bool bGood = false;
                                if (m_Mobile.Alive == false) //dead: always allow to transport
                                {
                                    bGood = true;
                                }
                                else if (m_Mobile.Region.IsDungeonRules) //alive and in dungeon
                                {
                                    m_Mobile.Kill();
                                    bGood = true;
                                }
                                else if (m_Mobile.TotalWeight < StuckMenu.MAXHELPSTUCKALIVEWEIGHT) //alive and out of dungeon and not over weight limit
                                {
                                    bGood = true;
                                }
                                else // alive, out of dungeon, over weight limit
                                {
                                    m_Mobile.SendMessage("You are too encumbered to be moved, drop most of your stuff and help-stuck again.");
                                }

                                if (bGood)
                                {
                                    //all good - proceed

                                    //check his pets in range:
                                    try
                                    {
                                        IPooledEnumerable eable = m_Mobile.GetMobilesInRange(3);
                                        foreach (Mobile m in eable)
                                        {
                                            if (m is BaseCreature)
                                            {
                                                BaseCreature pet = (BaseCreature)m;

                                                if (pet.Controlled && pet.ControlMaster == m_Mobile)
                                                {
                                                    if (pet.ControlOrder == OrderType.Guard || pet.ControlOrder == OrderType.Follow || pet.ControlOrder == OrderType.Come)
                                                    {
                                                        if (pet is PackHorse || pet is PackLlama || pet is Beetle || pet is HordeMinion)
                                                        {
                                                            if (pet.Backpack != null && pet.Backpack.Items.Count > 0)
                                                            {
                                                                m_Mobile.SendMessage("You cannot be transported because you have a pack animal that is not empty!");
                                                                bMoveMe = false;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        eable.Free();
                                    }
                                    catch (Exception checkexception)
                                    {
                                        Server.Commands.LogHelper.LogException(checkexception);
                                    }

                                }
                                else
                                {
                                    bMoveMe = false;
                                }
                            }

                            if (bMoveMe)
                            {
                                Mobiles.BaseCreature.TeleportPets(m_Mobile, m_Location, Map.Felucca);
                                m_Mobile.MoveToWorld(m_Location, Map.Felucca);
                                m_Mobile.UsedStuckMenu();
                            }
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("You are not in a valid location to use auto-help-stuck. You may use help-stuck again to ask for GM intervention.");
                    }

                    Stop();
                }
                else
                {
                    m_Mobile.Frozen = true;
                }
            }
        }
    }
}
