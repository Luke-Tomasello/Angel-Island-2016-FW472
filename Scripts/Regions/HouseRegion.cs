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

/* Scripts/Regions/HouseRegion.cs
 * ChangeLog
 *	8/11/10, Adam
 *		return baseRegion.IsHouseRules true
 *  5/1`0/07, Adam
 *      - Remove AOS 'enter' and 'ban' rules 
 *	4/30/07, Pix
 *		Fixed township innkeepers allowing enemies of the town to instalog.
 *  7/29/06, Kit
 *		Cleanup DRDT region checks, add in EquipItem region/drdt call.
 *	3/20/06, weaver
 *		Disabled "I wish to place a trash barrel" command.
 *	2/10/06, Adam
 *		Add a new backdoor command to extract a guildstone from a player (now carried on their person)
 *		"I wish to place my guild stone"
 *		This command is superseded by the "Guild Restoration Deed"
 *	05/03/05, Kit
 *		Added checks into Houseing region for DRDT regions below it and if so use DRDT rules.
 *	12/17/04, Adam
 *		Undo Mith's changes of 6/12/04 wrt lockdowns and friends
 *	9/23/04, Adam
 *		Make Speech lower before checking against the command string. 
 *		Example: e.Speech.ToLower() == "i wish to make this functional"
 *	9/21/04, Adam
 *		Create mechanics for Decorative containers that do not count against lockboxes
 *			Add two new commands:
 *				"i wish to make this decorative"
 *				"i wish to make this functional"
 *			These new commands convert a container to and from decorative. 
 *			Decorative containers do not count towards a houses lockbox count.
 *		See Also: BaseHouse.cs and various containers
 *	6/12/04, mith
 *		Made changes so that friends can lock down and release items.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/13/04, mith
 *		Modified OnMoveInto() and OnLocationChanged() to allow GMs to enter a house that's being customized
 */

using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using System;

namespace Server.Regions
{
    public class HouseRegion : Region
    {
        public override bool IsHouseRules { get { return true; } }

        private BaseHouse m_House;
        public static void Initialize()
        {
            EventSink.Login += new LoginEventHandler(OnLogin);

        }

        public static void OnLogin(LoginEventArgs e)
        {
            BaseHouse house = BaseHouse.FindHouseAt(e.Mobile);

            if (house != null && !house.Public && !house.IsFriend(e.Mobile))
                e.Mobile.Location = house.BanLocation;
        }

        public HouseRegion(BaseHouse house)
            : base("", "", house.Map)
        {
            Priority = Region.HousePriority;
            LoadFromXml = false;
            m_House = house;

        }

        public override bool SendInaccessibleMessage(Item item, Mobile from)
        {
            if (item is Container)
                item.SendLocalizedMessageTo(from, 501647); // That is secure.
            else
                item.SendLocalizedMessageTo(from, 1061637); // You are not allowed to access this.

            return true;
        }

        public override bool CheckAccessibility(Item item, Mobile from)
        {
            return m_House.CheckAccessibility(item, from);
        }

        private bool m_Recursion;

        // Use OnLocationChanged instead of OnEnter because it can be that we enter a house region even though we're not actually inside the house
        public override void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
            if (m_Recursion)
                return;

            m_Recursion = true;

            if (m is BaseCreature && ((BaseCreature)m).NoHouseRestrictions)
            {
            }
            else if (m is BaseCreature && ((BaseCreature)m).IsHouseSummonable && (BaseCreature.Summoning || m_House.IsInside(oldLocation, 16)))
            {
            }
            else if ((m_House.Public || !m_House.IsAosRules) && m_House.IsBanned(m) && m_House.IsInside(m))
            {
                m.Location = m_House.BanLocation;
                m.SendLocalizedMessage(501284); // You may not enter.
            }
            //Adam: no AOS rules here
            /*else if ( m_House.IsAosRules && !m_House.Public && !m_House.HasAccess( m ) && m_House.IsInside( m ) )
			{
				m.Location = m_House.BanLocation;
				m.SendLocalizedMessage( 501284 ); // You may not enter.
			}*/
            else if (m_House is HouseFoundation)
            {
                HouseFoundation foundation = (HouseFoundation)m_House;

                if (foundation.Customizer != null && foundation.Customizer != m && m_House.IsInside(m) && m.AccessLevel < AccessLevel.GameMaster)
                    m.Location = m_House.BanLocation;
            }

            m_Recursion = false;
        }

        public override bool OnMoveInto(Mobile from, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (from is BaseCreature && ((BaseCreature)from).NoHouseRestrictions)
            {
            }
            else if (from is BaseCreature && ((BaseCreature)from).IsHouseSummonable && (BaseCreature.Summoning || m_House.IsInside(oldLocation, 16)))
            {
            }
            else if ((m_House.Public || !m_House.IsAosRules) && m_House.IsBanned(from) && m_House.IsInside(newLocation, 16))
            {
                from.Location = m_House.BanLocation;
                from.SendLocalizedMessage(501284); // You may not enter.
                return false;
            }
            //Adam: no AOS rules here
            /*else if ( m_House.IsAosRules && !m_House.Public && !m_House.HasAccess( from ) && m_House.IsInside( newLocation, 16 ) )
			{
				from.SendLocalizedMessage( 501284 ); // You may not enter.
				return false;
			}*/
            else if (m_House is HouseFoundation)
            {
                HouseFoundation foundation = (HouseFoundation)m_House;

                if (foundation.Customizer != null && foundation.Customizer != from && m_House.IsInside(newLocation, 16) && from.AccessLevel < AccessLevel.GameMaster)
                    return false;
            }

            return true;
        }

        public override bool OnDecay(Item item)
        {
            if ((m_House.IsLockedDown(item) || m_House.IsSecure(item)) && m_House.IsInside(item))
                return false;
            else
                return base.OnDecay(item);
        }

        private static TimeSpan CombatHeatDelay = TimeSpan.FromSeconds(30.0);

        public override TimeSpan GetLogoutDelay(Mobile m)
        {
            if (m_House.IsFriend(m) && m_House.IsInside(m))
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
                    Mobile tsnpc = m_House.FindTownshipNPC();
                    if (tsnpc != null && tsnpc is TSInnKeeper)
                    {
                        if (m_House.IsInside(m))
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

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!from.Alive || !m_House.IsInside(from))
                return;

            bool isOwner = m_House.IsOwner(from);
            bool isCoOwner = isOwner || m_House.IsCoOwner(from);
            bool isFriend = isCoOwner || m_House.IsFriend(from);

            if (!isFriend)
                return;

            if (e.HasKeyword(0x33)) // remove thyself
            {
                if (isFriend)
                {
                    from.SendLocalizedMessage(501326); // Target the individual to eject from this house.
                    from.Target = new HouseKickTarget(m_House);
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
                    from.Target = new HouseDecoTarget(true, m_House);
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
                    from.Target = new HouseDecoTarget(false, m_House);
                }
            }
            else if (e.HasKeyword(0x34)) // I ban thee
            {
                if (!isFriend)
                {
                    from.SendLocalizedMessage(502094); // You must be in your house to do this.
                }
                //Adam: no AOS rules here
                /*else if ( !m_House.Public && m_House.IsAosRules )
				{
					from.SendLocalizedMessage( 1062521 ); // You cannot ban someone from a private house.  Revoke their access instead.
				}*/
                else
                {
                    from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                    from.Target = new HouseBanTarget(true, m_House);
                }
            }
            else if (e.HasKeyword(0x23)) // I wish to lock this down
            {
                if (isCoOwner)
                {
                    from.SendLocalizedMessage(502097); // Lock what down?
                    from.Target = new LockdownTarget(false, m_House);
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
                    from.Target = new LockdownTarget(true, m_House);
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
                    from.Target = new SecureTarget(false, m_House);
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
                    from.Target = new SecureTarget(true, m_House);
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
                    m_House.AddStrongBox(from);
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
					m_House.AddTrashBarrel( from );
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

        public override bool EquipItem(Mobile m, Item item)
        {
            try
            {
                //check if there is a DRDT region below the houses, and if so use its rules
                RegionControl regstone = null;
                CustomRegion inHouse = null;
                if (m != null)
                    inHouse = CustomRegion.FindDRDTRegion(m);
                if (inHouse != null)
                    regstone = inHouse.GetRegionControler();

                if (regstone != null)
                    return inHouse.EquipItem(m, item);
            }
            catch (NullReferenceException e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("{0} Caught exception.", e);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            return true;
        }

        public override bool OnDoubleClick(Mobile from, object o)
        {
            try
            {
                //check if there is a DRDT region below the houses, and if so use its rules
                RegionControl regstone = null;
                CustomRegion inHouse = null;
                if (from != null)
                    inHouse = CustomRegion.FindDRDTRegion(from);
                if (inHouse != null)
                    regstone = inHouse.GetRegionControler();

                if (regstone != null)
                    return inHouse.OnDoubleClick(from, o);
            }
            catch (NullReferenceException e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("{0} Caught exception.", e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            if (o is Container)
            {
                Container c = (Container)o;

                SecureAccessResult res = m_House.CheckSecureAccess(from, c);

                switch (res)
                {
                    case SecureAccessResult.Insecure: break;
                    case SecureAccessResult.Accessible: return true;
                    case SecureAccessResult.Inaccessible: c.SendLocalizedMessageTo(from, 1010563); return false;
                }
            }

            return true;
        }

        public override bool OnSingleClick(Mobile from, object o)
        {
            if (o is Item)
            {
                Item item = (Item)o;

                if (m_House.IsLockedDown(item))
                    item.LabelTo(from, 501643); // [locked down]
                else if (m_House.IsSecure(item))
                    item.LabelTo(from, 501644); // [locked down & secure]
            }

            return true;
        }

        public override bool OnSkillUse(Mobile m, int skill)
        {
            try
            {
                //check if there is a DRDT region below the houses, and if so use its rules
                RegionControl regstone = null;
                CustomRegion inHouse = null;
                if (m != null)
                    inHouse = CustomRegion.FindDRDTRegion(m);
                if (inHouse != null)
                    regstone = inHouse.GetRegionControler();

                if (m != null && regstone != null)
                {
                    return inHouse.OnSkillUse(m, skill);
                }
            }
            catch (NullReferenceException e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("{0} Caught exception.", e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return base.OnSkillUse(m, skill);
        }

        public override bool OnBeginSpellCast(Mobile from, ISpell s)
        {
            try
            {
                //check if there is a DRDT region below the houses, and if so use its rules
                RegionControl regstone = null;
                CustomRegion inHouse = null;
                inHouse = CustomRegion.FindDRDTRegion(from);
                if (inHouse != null)
                    regstone = inHouse.GetRegionControler();

                if (regstone != null)
                {
                    return inHouse.OnBeginSpellCast(from, s);

                }
            }
            catch (NullReferenceException e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("{0} Caught exception.", e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return base.OnBeginSpellCast(from, s);
        }

        public override bool OnDeath(Mobile m)
        {
            try
            {
                //check if there is a DRDT region below the houses, and if so use its rules
                RegionControl regstone = null;
                CustomRegion inHouse = null;
                inHouse = CustomRegion.FindDRDTRegion(m);
                if (inHouse != null)
                    regstone = inHouse.GetRegionControler();

                if (regstone != null && regstone.NoMurderZone)
                {
                    foreach (AggressorInfo ai in m.Aggressors)
                    {
                        ai.CanReportMurder = false;
                    }
                }
            }
            catch (NullReferenceException e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("{0} Caught exception.", e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            return base.OnDeath(m);
        }

        public BaseHouse House
        {
            get
            {
                return m_House;
            }
        }
    }
}
