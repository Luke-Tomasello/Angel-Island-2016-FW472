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

/* Scripts/Engines/DRDT/CustomRegion.cs
 * CHANGELOG:
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	05/14/09, plasma
 *	add base OnSpeech() call in as was preventing players calling the guards!
 *	4/9/09, Adam
 *		Add missing FindDRDTRegion(Item from)
 *	6/26/08, weaver
 *		Don't make sparklies for anyone of higher access when moving into an isolated region...
 *	6/24/08, weaver
 *		Optimized isolation checks (added extra playermobile checks).
 *	6/23/08, weaver
 *		Fixed isolated regions so that all mobiles entering a region are made visible.
 *		Added sparklies to disappearing mobiles.
 *		Added sound effect on enter/exit of isolated region when mobiles disappear or appear.
 *	1/27/08, Adam
 *		Replace Dungeon prop with the IsDungeonRules property that uses 'flags'
 *	07/28/07, Adam
 *		Add support for Custom Regions that are a HousingRegion superset
 *	02/03/07, Pix
 *		Fixed FindDRDTRegion to just check on Point.Zero - the other checks didn't work.
 *		Also, fixed order of one combined null-checking.
 *		(Future use) Changed protection level of m_Controller to protected (from private) so subclasses could access.
 * 02/02/07, Kit
 *      Additional sanity checking to FindDRDTRegion still throwing exceptions here and there.
 *      Added deleted and point3d/map bounds checks
 *      Made FindDRDTRegion(Mobile) and FindDRDTRegion(Item) use common FindDRDTRegion(Map, Point3d)
 * 12/31/06, Kit
 *      Additional sanity checking to FindDRDTRegion still throwing exceptions here and there.
 *      Updated IsrestrictedSpell/Skill checks to pass mobile for invalid packet range logging.
 * 10/30/06, Pix
 *		Added protection for crash relating to BitArray index.
 *  9/02/06, Kit
 *		Added additional checking to FINDDRDTRegion(Item).
 *  8/27/06, Kit
 *		Added additional null checks to FindDRDTRegion for incase of sector retrival fail or invalid location.
 *  8/19/06, Kit
 *		Changed OnEnter/Exit Isolation functionality to handle the hiding of items/multis.
 *  7/29/06, Kit
 *		Added EquipItem region override that checks RestrictedTypes list. Added RestrictedType list
 *		checking to OnDoubleClick().
 *  6/26/06, Kit
 *		Addec checks to OnEnter/OnExit so that if OverrideMaxFollowers is set players entering
 *		will have there MaxFollowers rateing adjusted. 
 *  6/25/06, Kit
 *		Added checks for RestrictCreatureMagic and for useing non-generic Magic fail msg if
 *		MagicFailureMsg is set.
 *  6/24/06, Kit
 *		Added overload FindDRDTRegion that now accepts a point3d location.
 *	6/15/06, Pix
 *		Overrode new IsNoMurderZone property so that the server.exe could tell whether the region is a NoMurderZone.
 *	6/11/06, Pix
 *		Added warning on enter if the area is a No Count Zone
 *  5/02/06, Kit
 *		Added Check to OnEnter/OnExit to play/stop music if enabled and music track.
 *	30/4/06, weaver
 *		Added OnEnter() and OnExit() code to refresh region isolated mobile visibility (and invisibility...)
 *	2/3/06, Pix
 *		Enter message uses RegionName instead of Name. :D
 *	2/3/06, Pix
 *		Now the IOB attack message uses the region controller's name property when 
 *		announcing attackers.
 *	10/06/05, Pix
 *		Removed extraneous 's' on IOB message.
 *	10/04/05, Pix
 *		Changed OnEnter for to use new GetIOBName() function.
 *	9/20/05, Pix
 *		Fixed the enter messages for the Good IOB
 *	7/29/05, Pix
 *		Fixed grammar mistake: orc's vs orcs
 *  05/05/05, Kit
 *	Added fix for iob msgs sending for None alignment
 *  05/03/05, Kit
 *	Added FindDRDTRegion() to return any drdt's regions at the mobiles position even if in another higher priority region.
 *	Added GetLogOutDelay for dealing with inns'
 *  05/02/05, Kit
 *	Added toggle for IOB zone messages
 *  04/30/05, Kit
 *	Added IOB Support and kin messages when opposeing kin enter a opposeing iob zone.
 *  04/29/05, Kitaras
 *	 Initial system
 */
using Server.Commands;
using Server.Engines.IOBSystem;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells.Fourth;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using System;
using System.Collections;

namespace Server.Regions
{
    public class CustomRegion : GuardedRegion
    {
        protected RegionControl m_Controller;

        new public static void Initialize()
        {
            EventSink.Login += new LoginEventHandler(OnLogin);

        }

        public static void OnLogin(LoginEventArgs e)
        {
            // HousingRegion processing
            CustomRegion cr = CustomRegion.FindDRDTRegion(e.Mobile);
            if (cr != null)
            {
                RegionControl rc = cr.GetRegionControler();
                if (rc != null)
                {
                    if (rc.UseHouseRules == true)
                    {
                        BaseHouse house = BaseHouse.FindHouseAt(e.Mobile);
                        if (house != null && !house.Public && !house.IsFriend(e.Mobile))
                            e.Mobile.Location = house.BanLocation;
                    }
                }
            }

            // other OnLogin processing
        }

        public CustomRegion(RegionControl m, Map map)
            : base("", "Custom Region", map, typeof(WarriorGuard))
        {
            LoadFromXml = false;

            m_Controller = m;
        }

        public RegionControl GetRegionControler()
        {
            return m_Controller;
        }

        public static CustomRegion FindDRDTRegion(Mobile from)
        {
            if (from == null || from.Deleted)
                return null;

            return FindDRDTRegion(from.Map, from.Location);
        }

        public static CustomRegion FindDRDTRegion(Mobile from, Point3D loc)
        {
            if (from == null || from.Deleted)
                return null;

            return FindDRDTRegion(from.Map, loc);
        }

        public static CustomRegion FindDRDTRegion(Item from)
        {
            if (from == null || from.Deleted)
                return null;

            return FindDRDTRegion(from.Map, from.Location);
        }

        public static CustomRegion FindDRDTRegion(Item from, Point3D loc)
        {
            if (from == null || from.Deleted)
                return null;

            return FindDRDTRegion(from.Map, loc);
        }

        public static CustomRegion FindDRDTRegion(Map map, Point3D loc)
        {
            Point3D p = loc;

            if (p == Point3D.Zero)
                return null;

            if (map == Map.Internal || map == null)
                return null;

            Sector sector = map.GetSector(p);

            if (sector == null || sector.Owner == null || sector == sector.Owner.InvalidSector)
                return null;

            if (sector.Regions == null) //new check 2/2/07
                return null;

            ArrayList list = sector.Regions;

            if (list == null || list.Count == 0)
                return null;

            ArrayList list2 = new ArrayList();

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is Region) //new check 2/2/07 
                {
                    Region region = (Region)list[i];

                    if (region == null)
                        continue;

                    if (region.Contains(p))
                        list2.Add(region);
                }

            }
            foreach (Region reg in list2)
            {
                if (reg == null)
                    continue;

                CustomRegion test = null;

                if (reg is CustomRegion)
                {
                    test = reg as CustomRegion;
                    if (test != null)
                        return test;
                }

            }
            //no custom region found
            return null;
        }

        public override bool IsDungeonRules { get { return m_Controller != null && m_Controller.UseDungeonRules; } }
        public override bool IsJailRules { get { return m_Controller != null && m_Controller.UseJailRules; } }
        public override bool IsHouseRules { get { return m_Controller != null && m_Controller.UseHouseRules; } }
        public override bool IsGreenAcresRules { get { return m_Controller != null && m_Controller.UseGreenAcresRules; } }
        public override bool IsAngelIslandRules { get { return m_Controller != null && m_Controller.UseAngelIslandRules; } }
        public override bool IsGuarded { get { return m_Controller.GetFlag(RegionFlag.IsGuarded); } set { m_Controller.SetFlag(RegionFlag.IsGuarded, value); } }
        public override bool IsSmartGuards { get { return m_Controller.GetFlag(RegionFlag.IsSmartGuards); } set { m_Controller.SetFlag(RegionFlag.IsSmartGuards, value); } }

        public override bool IsNoMurderZone
        {
            get
            {
                if (m_Controller == null)
                {
                    return base.IsNoMurderZone;
                }
                else
                {
                    return m_Controller.NoMurderZone;
                }
            }
        }

        public override bool AllowHousing(Point3D p)
        {
            if (IsAngelIslandRules)
                return false;
            if (IsGreenAcresRules)
            {   // we allow players to place houses on Green Acres on Test Center
                if (Server.Misc.TestCenter.Enabled == true)
                    return true;
                else
                    return false;
            }
            if (IsJailRules)
                return false;

            return m_Controller.AllowHousing;
        }

        public override bool AllowBenificial(Mobile from, Mobile target)
        {
            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You may not do that in jail.");
                return false;
            }

            return base.AllowBenificial(from, target); ;
        }

        public override bool AllowHarmful(Mobile from, Mobile target)
        {
            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You may not do that in jail.");
                return false;
            }

            return base.AllowHarmful(from, target);
        }

        public override bool CanUseStuckMenu(Mobile m)
        {
            if (IsAngelIslandRules)
                return false;
            if (!m_Controller.CanUseStuckMenu)
                m.SendMessage("You cannot use the Stuck menu here.");
            return m_Controller.CanUseStuckMenu;
        }

        public override bool OnResurrect(Mobile m)
        {
            if (m.AccessLevel == AccessLevel.Player)
            {
                if (m_Controller.CanRessurect == false)
                    m.SendMessage("You cannot ressurect here.");
                return m_Controller.CanRessurect;
            }
            else
            {
                if (m_Controller.CanRessurect == false)
                    m.SendMessage("Resurrection is not allowed here, but your godly powers allow it.");
                return true;
            }
        }

        public override bool OnBeginSpellCast(Mobile from, ISpell s)
        {
            if (IsAngelIslandRules && (s is TeleportSpell || s is RecallSpell || s is GateTravelSpell || s is MarkSpell) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You cannot cast that spell here.");
                return false;
            }
            if (IsGreenAcresRules && (s is GateTravelSpell || s is RecallSpell || s is MarkSpell) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You cannot cast that spell here.");
                return false;
            }
            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(502629); // You cannot cast spells here.
                return false;
            }
            if (from.AccessLevel == AccessLevel.Player)
            {
                bool restricted = m_Controller.IsRestrictedSpell(s, from);
                if (restricted && ((from is BaseCreature && m_Controller.RestrictCreatureMagic) || from is PlayerMobile))
                {
                    if (m_Controller.MagicMsgFailure == null)
                        from.SendMessage("You cannot cast that spell here.");
                    else
                        from.SendMessage(m_Controller.MagicMsgFailure);

                    return false;
                }

            }

            return base.OnBeginSpellCast(from, s);
        }

        public override bool OnSkillUse(Mobile m, int skill)
        {
            if (IsJailRules && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You may not use skills in jail.");
                return false;
            }
            else
            {
                bool bReturn = true;
                try
                {
                    if (m.AccessLevel == AccessLevel.Player)
                    {
                        bool restricted = m_Controller.IsRestrictedSkill(skill, m);
                        if (restricted)
                        {
                            m.SendMessage("You cannot use that skill here.");
                            return false;
                        }
                    }
                    bReturn = base.OnSkillUse(m, skill);
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Caught error in CustomRegion.OnSkillUse({0}, {1})", m.Name, skill);
                    Console.WriteLine("Error is: {0}", e.Message);
                    Console.WriteLine(e.StackTrace.ToString());
                }
                return bReturn;
            }
        }

        public override bool OnCombatantChange(Mobile from, Mobile Old, Mobile New)
        {
            if (IsJailRules)
            {
                return from.AccessLevel > AccessLevel.Player;
            }
            else
                return base.OnCombatantChange(from, Old, New);
        }

        public override void OnExit(Mobile m)
        {
            if (IsGreenAcresRules)
            {
                return;
            }

            Region left = null;
            PlayerMobile pm = null;

            if (m is PlayerMobile)
            {
                pm = (PlayerMobile)m;
                left = pm.Region;
            }

            // wea: If we're leaving an isolated region, we need
            // to send remove packets to all isolated playermobiles 
            // within

            if (m_Controller.IsIsolated)
            {
                // Send fresh state info if we're a player leaving an isolated region
                if (pm != null)
                {
                    if (m.NetState != null)
                    {
                        m.SendEverything();
                    }
                }

                Packet p = null;
                Packet particles = null;
                IPooledEnumerable eable = m.GetMobilesInRange(Core.GlobalMaxUpdateRange);
                int revealedmobiles = 0;

                foreach (Mobile mir in eable)
                {
                    // If they're in the region we're leaving, send a remove packet

                    if (mir is PlayerMobile)
                    {
                        if (mir.Region == this && !mir.CanSee(m)) // CanSee includes the isolation check
                        {
                            p = m.RemovePacket; // make us disappear to them

                            if (mir.NetState != null)
                                mir.Send(p);
                        }
                    }

                    // They're going to become visible to us as a result of
                    // the SendEverything() call above, so as long as they're not us and
                    // are in our new region, send sparklies at their location 

                    // Also, send a sound at the end if there's at least one revealed

                    if (mir != m && mir.Region != this && m.CanSee(mir) && m.AccessLevel == AccessLevel.Player)
                    {
                        // Create localized sparklies 
                        particles = new LocationParticleEffect(EffectItem.Create(mir.Location, mir.Map, EffectItem.DefaultDuration), 0x376A, 10, 10, 0, 0, 2023, 0);

                        if (m.NetState != null)
                        {
                            m.Send(particles);
                            revealedmobiles++;
                        }
                    }
                }

                // If at least one was revealed, play a sound too
                if (revealedmobiles > 0)
                {
                    if (m.NetState != null)
                    {
                        m.PlaySound(0x3C4);
                    }
                }
            }

            //if were moving into a house dont play the exit msg
            if (pm != null && pm.Region is HouseRegion)
                return;

            //if (m_Controller.PlayMusic)
            //StopMusic(m);

            if (m_Controller.ShowExitMessage)
                m.SendMessage("You have left {0}", this.Name);

            if (m_Controller.OverrideMaxFollowers)
                m.FollowersMax = 5; //return to default;

            base.OnExit(m);

        }

        public override void OnEnter(Mobile m)
        {
            if (IsGreenAcresRules)
            {
                return;
            }

            Region left = null;
            PlayerMobile pm = null;

            if (m is PlayerMobile)
            {
                pm = (PlayerMobile)m;
                left = pm.LastRegionIn;
            }
            // wea: If this is an isolated region, we're going to send sparklies where
            // mobiles will disappear (this happens as part of an IsIsolatedFrom() check,
            // not explicit packet removal here) + a sound effect if we had to do this
            // 
            // also send an incoming packet to all players within the region.
            // ____
            // ||
            if (m_Controller.IsIsolated)
            {
                if (m.Map != null)
                {
                    int invissedmobiles = 0;

                    IPooledEnumerable eable = m.GetMobilesInRange(Core.GlobalMaxUpdateRange);

                    foreach (Mobile mir in eable)
                    {
                        // Regardless of whether this is mobile or playermobile,
                        // we need to send an incoming packet to each of the mobiles
                        // in the region

                        if (mir.Region == m.Region)
                        {
                            if (mir is PlayerMobile)
                            {
                                // We've just walked into this mobile's region, so send incoming packet
                                // if they're a playermobile

                                if (Utility.InUpdateRange(m.Location, mir.Location) && mir.CanSee(m))
                                {
                                    // Send incoming packet to player if they're online
                                    if (mir.NetState != null)
                                    {
                                        mir.NetState.Send(new MobileIncoming(mir, m));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // They're in a different region, so localise sparklies
                            // to us if we're a player mobile
                            if (pm != null && mir.AccessLevel <= pm.AccessLevel)
                            {
                                Packet particles = new LocationParticleEffect(EffectItem.Create(mir.Location, mir.Map, EffectItem.DefaultDuration), 0x376A, 10, 10, 0, 0, 2023, 0);

                                if (pm.NetState != null && particles != null)
                                {
                                    pm.Send(particles);
                                    invissedmobiles++;
                                }
                            }
                        }
                    }

                    if (invissedmobiles > 0)
                    {
                        // Play a sound effect to go with it
                        if (pm.NetState != null)
                        {
                            pm.PlaySound(0x3C4);
                        }
                    }

                    if (pm != null)
                    {
                        m.ClearScreen();
                        m.SendEverything();
                    }
                    eable.Free();
                }
            }
            // ||
            // ____

            // if were leaving a house and entering the region(already in it) dont play the enter msg
            if (pm != null && pm.LastRegionIn is HouseRegion)
            {
                return;
            }

            if (m_Controller.ShowEnterMessage)
            {
                m.SendMessage("You have entered {0}", this.Name);

                if (m_Controller.NoMurderZone)
                {
                    m.SendMessage("This is a lawless area; you are freely attackable here.");
                }
            }

            if (m_Controller.OverrideMaxFollowers)
                m.FollowersMax = m_Controller.MaxFollowerSlots;

            //if (m_Controller.PlayMusic)
            //PlayMusic(m);

            PlayerMobile IOBenemy = null;

            if (m is PlayerMobile)
            {
                IOBenemy = (PlayerMobile)m;
            }

            //if is a iob zone/region and a iob aligned mobile with a differnt alignment then the zone enters
            //find all players of the zones alignment and send them a message
            //plasma: refactored the send message code into its own method within KinSystem
            if (DateTime.UtcNow >= m_Controller.m_Msg && m_Controller.IOBZone && m_Controller.ShowIOBMsg && IOBenemy != null && IOBenemy.IOBAlignment != IOBAlignment.None && IOBenemy.IOBAlignment != m_Controller.IOBAlign && m.AccessLevel == AccessLevel.Player)  //we dont want it announceing staff with iob kinship
            {
                if (m_Controller.RegionName != null && m_Controller.RegionName.Length > 0)
                {
                    IOBSystem.SendKinMessage(m_Controller.IOBAlign, string.Format("Come quickly, the {0} are attacking {1}!",
                        IOBSystem.GetIOBName(IOBenemy.IOBRealAlignment),
                        m_Controller.RegionName));
                }
                else
                {
                    IOBSystem.SendKinMessage(m_Controller.IOBAlign, string.Format("Come quickly, the {0} are attacking your stronghold!",
                        IOBSystem.GetIOBName(IOBenemy.IOBRealAlignment)));
                }
                m_Controller.m_Msg = DateTime.UtcNow + m_Controller.m_Delay;
            }
            /*else if (DateTime.UtcNow >= m_Controller.m_Msg && this is Engines.IOBSystem.KinCityRegion && IOBenemy != null && IOBenemy.IOBAlignment != IOBAlignment.None && IOBenemy.IOBAlignment != m_Controller.IOBAlign && m.AccessLevel == AccessLevel.Player)  //we dont want it announceing staff with iob kinship
			{
				KinCityRegion r = KinCityRegion.GetKinCityAt(this.m_Controller);
				if (r != null)
				{
					KinCityData cd = KinCityManager.GetCityData(r.KCStone.City);
					if (cd != null && cd.ControlingKin != IOBAlignment.None)
					{
						Engines.IOBSystem.KinSystem.SendKinMessage(cd.ControlingKin, string.Format("Come quickly, the {0} are attacking the City of {1}!",
							IOBSystem.GetIOBName(IOBenemy.IOBRealAlignment), cd.City.ToString()));
						m_Controller.m_Msg = DateTime.UtcNow + m_Controller.m_Delay;
					}
				}
			}*/

            base.OnEnter(m);
        }

        public override bool SendInaccessibleMessage(Item item, Mobile from)
        {
            if (IsHouseRules)
            {
                if (item is Container)
                    item.SendLocalizedMessageTo(from, 501647); // That is secure.
                else
                    item.SendLocalizedMessageTo(from, 1061637); // You are not allowed to access this.

                return true;
            }
            else
                return base.SendInaccessibleMessage(item, from);
        }

        public override bool CheckAccessibility(Item item, Mobile from)
        {
            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);
                if (house == null)
                    return base.CheckAccessibility(item, from);

                return house.CheckAccessibility(item, from);
            }
            else
                return base.CheckAccessibility(item, from);
        }

        private bool m_Recursion;

        // Use OnLocationChanged instead of OnEnter because it can be that we enter a house region even though we're not actually inside the house
        public override void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);
                if (house != null)
                {
                    if (m_Recursion)
                        return;

                    m_Recursion = true;

                    if (m is BaseCreature && ((BaseCreature)m).NoHouseRestrictions)
                    {
                    }
                    else if (m is BaseCreature && ((BaseCreature)m).IsHouseSummonable && (BaseCreature.Summoning || house.IsInside(oldLocation, 16)))
                    {
                    }
                    else if ((house.Public || !house.IsAosRules) && house.IsBanned(m) && house.IsInside(m))
                    {
                        m.Location = house.BanLocation;
                        m.SendLocalizedMessage(501284); // You may not enter.
                    }
                    //Adam: no AOS rules here
                    /*else if ( house.IsAosRules && !house.Public && !house.HasAccess( m ) && house.IsInside( m ) )
					{
						m.Location = house.BanLocation;
						m.SendLocalizedMessage( 501284 ); // You may not enter.
					}*/
                    else if (house is HouseFoundation)
                    {
                        HouseFoundation foundation = (HouseFoundation)house;

                        if (foundation.Customizer != null && foundation.Customizer != m && house.IsInside(m) && m.AccessLevel < AccessLevel.GameMaster)
                            m.Location = house.BanLocation;
                    }

                    m_Recursion = false;
                }
            }
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (IsAngelIslandRules)
            {
                // If someone attempting to enter the region is not an Inmate, we deny them entry.
                // The only way in is via the teleporter, which sets the Inmate flag true.
                if (m.Player)
                    if (!((PlayerMobile)m).Inmate && m.AccessLevel == AccessLevel.Player)
                        return false;

                return true;
            }

            // HousingRegion control
            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);
                if (house != null)
                {
                    if (m is BaseCreature && ((BaseCreature)m).NoHouseRestrictions)
                    {
                    }
                    else if (m is BaseCreature && ((BaseCreature)m).IsHouseSummonable && (BaseCreature.Summoning || house.IsInside(oldLocation, 16)))
                    {
                    }
                    else if ((house.Public || !house.IsAosRules) && house.IsBanned(m) && house.IsInside(newLocation, 16))
                    {
                        m.Location = house.BanLocation;
                        m.SendLocalizedMessage(501284); // You may not enter.
                        return false;
                    }
                    //Adam: no AOS rules here
                    /*else if ( house.IsAosRules && !house.Public && !house.HasAccess( from ) && house.IsInside( newLocation, 16 ) )
					{
						from.SendLocalizedMessage( 501284 ); // You may not enter.
						return false;
					}*/
                    else if (house is HouseFoundation)
                    {
                        HouseFoundation foundation = (HouseFoundation)house;

                        if (foundation.Customizer != null && foundation.Customizer != m && house.IsInside(newLocation, 16) && m.AccessLevel < AccessLevel.GameMaster)
                            return false;
                    }
                }
            }

            // controller control
            if (m_Controller.CannotEnter && !this.Contains(oldLocation))
            {
                m.SendMessage("You cannot enter this area.");
                return false;
            }

            return true;
        }

        public override void OnMobileAdd(Mobile m)
        {
            if (IsAngelIslandRules)
            {
                if (m.Player)
                    if (m.ShortTermMurders < 5 && m.AccessLevel == AccessLevel.Player)
                        if (!((PlayerMobile)m).Inmate && m.ShortTermMurders > 0)
                            m.Location = new Point3D(373, 870, 0);
            }
        }

        public override bool OnDecay(Item item)
        {
            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(item);
                if (house != null)
                {
                    if ((house.IsLockedDown(item) || house.IsSecure(item)) && house.IsInside(item))
                        return false;
                    else
                        return base.OnDecay(item);
                }
            }

            return base.OnDecay(item);
        }

        private static TimeSpan CombatHeatDelay = TimeSpan.FromSeconds(30.0);

        public override TimeSpan GetLogoutDelay(Mobile m)
        {
            // prefer house region settings if we are a housing region
            if (IsHouseRules == true)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);
                if (house != null)
                {
                    if (house.IsFriend(m) && house.IsInside(m))
                    {
                        for (int i = 0; i < m.Aggressed.Count; ++i)
                        {
                            AggressorInfo info = (AggressorInfo)m.Aggressed[i];

                            if (info.Defender.Player && (DateTime.UtcNow - info.LastCombatTime) < CombatHeatDelay)
                                return base.GetLogoutDelay(m);
                        }

                        return TimeSpan.Zero;
                    }
                    else
                    {
                        try
                        {
                            Mobile tsnpc = house.FindTownshipNPC();
                            if (tsnpc != null && tsnpc is TSInnKeeper)
                            {
                                if (house.IsInside(m))
                                {
                                    TownshipRegion tr = TownshipRegion.GetTownshipAt(m);
                                    if (tr != null)
                                    {
                                        if (tr.TStone != null && !tr.TStone.IsEnemy(m as PlayerMobile))
                                        {
                                            for (int i = 0; i < m.Aggressed.Count; ++i)
                                            {
                                                AggressorInfo info = (AggressorInfo)m.Aggressed[i];

                                                if (info.Defender.Player && (DateTime.UtcNow - info.LastCombatTime) < CombatHeatDelay)
                                                    return base.GetLogoutDelay(m);
                                            }

                                            return TimeSpan.Zero;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Server.Commands.LogHelper.LogException(e);
                        }
                    }

                    return base.GetLogoutDelay(m);
                }
            }

            // if we are not a house (or there is no house at this location), use region controller settings
            if (m.Aggressors.Count == 0 && m.Aggressed.Count == 0 && IsInInn(m.Location))
                return m_Controller.InnLogoutDelay;
            else
                return m_Controller.PlayerLogoutDelay;
        }

        // emulate house region if there is a house here
        public override void OnSpeech(SpeechEventArgs e)
        {
            //plasma: add base call in as was preventing players calling the guards!
            base.OnSpeech(e);

            #region REGION HOUSE RULES/COMMANDS
            if (IsHouseRules == true)
            {
                BaseHouse house = BaseHouse.FindHouseAt(e.Mobile);
                if (house != null)
                {
                    Mobile from = e.Mobile;

                    if (!from.Alive || !house.IsInside(from))
                        return;

                    bool isOwner = house.IsOwner(from);
                    bool isCoOwner = isOwner || house.IsCoOwner(from);
                    bool isFriend = isCoOwner || house.IsFriend(from);

                    if (!isFriend)
                        return;

                    if (e.HasKeyword(0x33)) // remove thyself
                    {
                        if (isFriend)
                        {
                            from.SendLocalizedMessage(501326); // Target the individual to eject from this house.
                            from.Target = new HouseKickTarget(house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.Speech.ToLower() == "i wish to make this decorative" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())) // i wish to make this decorative
                    {
                        if (!isFriend)
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                        else
                        {
                            from.SendMessage("Make what decorative?"); // 
                            from.Target = new HouseDecoTarget(true, house);
                        }
                    }
                    else if (e.Speech.ToLower() == "i wish to make this functional" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())) // i wish to make this functional
                    {
                        if (!isFriend)
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                        else
                        {
                            from.SendMessage("Make what functional?"); // 
                            from.Target = new HouseDecoTarget(false, house);
                        }
                    }
                    else if (e.HasKeyword(0x34)) // I ban thee
                    {
                        if (!isFriend)
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                        //Adam: no AOS rules here
                        /*else if ( !house.Public && house.IsAosRules )
						{
							from.SendLocalizedMessage( 1062521 ); // You cannot ban someone from a private house.  Revoke their access instead.
						}*/
                        else
                        {
                            from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                            from.Target = new HouseBanTarget(true, house);
                        }
                    }
                    else if (e.HasKeyword(0x23)) // I wish to lock this down
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(502097); // Lock what down?
                            from.Target = new LockdownTarget(false, house);
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x24)) // I wish to release this
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(502100); // Choose the item you wish to release
                            from.Target = new LockdownTarget(true, house);
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x25)) // I wish to secure this
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(502103); // Choose the item you wish to secure
                            from.Target = new SecureTarget(false, house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x26)) // I wish to unsecure this
                    {
                        if (isOwner)
                        {
                            from.SendLocalizedMessage(502106); // Choose the item you wish to unsecure
                            from.Target = new SecureTarget(true, house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x27)) // I wish to place a strong box
                    {
                        if (isOwner)
                        {
                            from.SendLocalizedMessage(502109); // Owners do not get a strongbox of their own.
                        }
                        else if (isFriend)
                        {
                            house.AddStrongBox(from);
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }

                    /* weaver: disallowed trash barrel placement by command
					 * 
					else if ( e.HasKeyword( 0x28 ) ) // I wish to place a trash barrel
					{
						if ( isCoOwner )
						{
							house.AddTrashBarrel( from );
						}
						else if ( isFriend )
						{
							from.SendLocalizedMessage( 1010587 ); // You are not a co-owner of this house.
						}
						else
						{
							from.SendLocalizedMessage( 502094 ); // You must be in your house to do this.
						}
					}
					*/

                    else if (e.Speech.ToLower() == "i wish to place my guild stone") // I wish to place a guild stone
                    {
                        if (isCoOwner)
                        {
                            // ask the playermobile to deal with this item request
                            Item item = from.RequestItem(typeof(Server.Items.Guildstone));
                            if (item == null)
                                from.SendMessage("You do not seem to have one of those.");
                            else
                            {   // ask the player mobile to place this guild stone
                                from.ProcessItem(item);
                            }
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                }
            }
            #endregion

        }

        public override bool EquipItem(Mobile m, Item item)
        {
            if (m_Controller.RestrictedTypes != null)
            {
                Type t = item.GetType();

                if (m_Controller.RestrictedTypes.Contains(t.Name))
                {
                    m.SendMessage("You cannot equip that.");
                    return false;
                }
            }
            return base.EquipItem(m, item);
        }

        public override bool OnDoubleClick(Mobile m, object o)
        {
            if (o is BasePotion && !m_Controller.CanUsePotions)
            {
                m.SendMessage("You cannot drink potions here.");
                return false;
            }

            if (m_Controller.RestrictedTypes != null)
            {
                Type t = o.GetType();
                if (m_Controller.RestrictedTypes.Contains(t.Name))
                {
                    m.SendMessage("You cannot use that.");
                    return false;
                }
            }

            if (IsHouseRules == true)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);
                if (house != null)
                {
                    if (o is Container)
                    {
                        Container c = (Container)o;

                        SecureAccessResult res = house.CheckSecureAccess(m, c);

                        switch (res)
                        {
                            case SecureAccessResult.Insecure: break;
                            case SecureAccessResult.Accessible: return true;
                            case SecureAccessResult.Inaccessible: c.SendLocalizedMessageTo(m, 1010563); return false;
                        }
                    }
                }
            }

            return base.OnDoubleClick(m, o);
        }

        public override bool OnSingleClick(Mobile from, object o)
        {
            if (IsHouseRules == true)
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);
                if (house != null)
                {
                    if (o is Item)
                    {
                        Item item = (Item)o;

                        if (house.IsLockedDown(item))
                            item.LabelTo(from, 501643); // [locked down]
                        else if (house.IsSecure(item))
                            item.LabelTo(from, 501644); // [locked down & secure]
                    }

                    return true;
                }
            }

            return base.OnSingleClick(from, o);
        }

        public override bool OnDeath(Mobile m)
        {
            if (m_Controller.NoMurderZone)
            {
                foreach (AggressorInfo ai in m.Aggressors)
                {
                    ai.CanReportMurder = false;
                }
            }

            return base.OnDeath(m);
        }

        public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            if (IsJailRules)
                global = LightCycle.JailLevel;
            else if (IsDungeonRules)
                global = LightCycle.DungeonLevel;

            if (m_Controller.LightLevel >= 0)
                global = m_Controller.LightLevel;
            else
                base.AlterLightLevel(m, ref global, ref personal);
        }


    }
}
