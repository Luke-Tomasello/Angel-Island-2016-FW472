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

/* Scripts/Regions/GuardedRegion.cs
 * ChangeLog
 *	2/15/11, Adam
 *		Reds are now allowed in town UOMO
 *	2/12/11, Adam
 *		Issue the message to reds "Guards can no longer be called on you." if reds are allowed in town
 *	2/10/11, Adam
 *		Update IsGuardCandidate to allow for reds in town (UOSP)
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/21/10, Adam
 *		In CheckGuardCandidate() we now check to see if the mobile 'remembers' the criminal.
 *		The old code used to simply enumerate the nearby mobiles to see if one was in range. If one was in range, it is assumed they were seen.
 *		The new code checks to see if the mobile actiually saw the player .. this allows guard whacks at WBB for players letting loose spells 
 *		while hidden while at the same time allowing reds to stealth and hidden recall into town (the NPCs will not have seen them.)
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 3 loops updated.
 *	9/1/07, Adam
 *		change [SetGuarded from AccessLevel Administrator to Seer
 *  03/31/06 Taran Kain
 *		Changed GuardTimer, CallGuards to only display "Guards cannot be called on you" if mobile is not red
 *  04/23/05, erlein
 *    Changed ToggleGuarded command to Seer level access.
 *	04/19/05, Kit
 *		Added check to IsGuardCandidate( ) to not make hidden players canadites.
 *	10/28/04, Pix
 *		In CheckGuardCandidate() ruled out the case where a player can recall to a guardzone before
 *		explosion hits and the person that casts explosion gets guardwhacked.
 *	8/12/04, mith
 *		IsGuardCandidate(): Modified to that player vendors will not call guards.
 *  6/21/04, Old Salty
 *  	Added a little code to CallGuards to close the bankbox of a criminal when the guards come
 *  6/20/04, Old Salty
 * 		Fixed IsGuardCandidate so that guards react properly 
 * 
 *	6/10/04, mith
 *		Modified to work with the new non-insta-kill guards.
 */

using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Regions
{
    public class GuardedRegion : Region
    {
        private static object[] m_GuardParams = new object[1];
        private Type m_GuardType;

        public override bool IsGuarded { get { return false; } set {; } }
        public override bool IsSmartGuards { get { return false; } set {; } }

        public static void Initialize()
        {
            CommandSystem.Register("CheckGuarded", AccessLevel.GameMaster, new CommandEventHandler(CheckGuarded_OnCommand));
            CommandSystem.Register("SetGuarded", AccessLevel.Seer, new CommandEventHandler(SetGuarded_OnCommand));
            CommandSystem.Register("ToggleGuarded", AccessLevel.Seer, new CommandEventHandler(ToggleGuarded_OnCommand));
        }

        [Usage("CheckGuarded")]
        [Description("Returns a value indicating if the current region is guarded or not.")]
        private static void CheckGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            GuardedRegion reg = from.Region as GuardedRegion;

            if (reg == null)
                from.SendMessage("You are not in a guardable region.");
            else if (reg.IsGuarded == false)
                from.SendMessage("The guards in this region have been disabled.");
            else
                from.SendMessage("This region is actively guarded.");
        }

        [Usage("SetGuarded <true|false>")]
        [Description("Enables or disables guards for the current region.")]
        private static void SetGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Length == 1)
            {
                GuardedRegion reg = from.Region as GuardedRegion;

                if (reg == null)
                {
                    from.SendMessage("You are not in a guardable region.");
                }
                else
                {
                    reg.IsGuarded = e.GetBoolean(0);

                    if (reg.IsGuarded == false)
                        from.SendMessage("The guards in this region have been disabled.");
                    else
                        from.SendMessage("The guards in this region have been enabled.");
                }
            }
            else
            {
                from.SendMessage("Format: SetGuarded <true|false>");
            }
        }

        [Usage("ToggleGuarded")]
        [Description("Toggles the state of guards for the current region.")]
        private static void ToggleGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            GuardedRegion reg = from.Region as GuardedRegion;

            if (reg == null)
            {
                from.SendMessage("You are not in a guardable region.");
            }
            else
            {
                reg.IsGuarded = !reg.IsGuarded;

                if (reg.IsGuarded == false)
                    from.SendMessage("The guards in this region have been disabled.");
                else
                    from.SendMessage("The guards in this region have been enabled.");
            }
        }

        public virtual bool CheckVendorAccess(BaseVendor vendor, Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster || IsGuarded)
                return true;

            return (from.LongTermMurders < 5);
        }

        public GuardedRegion(string prefix, string name, Map map, Type guardType)
            : base(prefix, name, map)
        {
            m_GuardType = guardType;
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (IsGuarded && !s.OnCastInTown(this))
            {
                m.SendLocalizedMessage(500946); // You cannot cast this in town!
                return false;
            }

            return base.OnBeginSpellCast(m, s);
        }

        public override bool AllowHousing(Point3D p)
        {
            return false;
        }

        public void MakeGuards(Mobile focus)
        {
            // also generate guards for the criminals pets
            ArrayList pets = new ArrayList();
            foreach (Mobile m in World.Mobiles.Values)
            {
                BaseCreature bc = m as BaseCreature;
                if (bc != null && bc.Controlled && bc.ControlMaster == focus && bc.ControlOrder == OrderType.Attack)
                {   // okay, looks bad for this pet...
                    if (bc.Region == focus.Region && bc.Map == focus.Map)
                    {   // okay, that's good enough for me.
                        pets.Add(bc);
                    }
                }
                else if (bc != null && bc.Controlled && bc.ControlMaster == focus && bc.ControlOrder != OrderType.Attack)
                {
                    // it's possible to command the rest of your pets to 'all kill' after the guards have been called.
                    //	the protect from this case, we will reduce all your pets Loyalty to almost nothing.
                    bc.Loyalty = PetLoyalty.Confused;
                    focus.SendMessage(string.Format("{0} is greatly confused by what's going on.", bc.Name));
                }
            }

            for (int ix = 0; ix < pets.Count; ix++)
                MakeGuard(pets[ix] as Mobile);

            MakeGuard(focus);
        }

        public override void MakeGuard(Mobile focus)
        {
            if (focus == null || focus.Deleted || !focus.Alive)
                return;

            BaseGuard useGuard = null;

            IPooledEnumerable eable = focus.GetMobilesInRange(8);
            foreach (Mobile m in eable)
            {
                if (m is BaseGuard)
                {
                    BaseGuard g = (BaseGuard)m;

                    if (g.Focus == null) // idling
                    {
                        useGuard = g;
                        break;
                    }
                }
            }
            eable.Free();

            if (useGuard != null)
            {
                useGuard.Focus = focus;
            }
            else
            {
                m_GuardParams[0] = focus;

                Activator.CreateInstance(m_GuardType, m_GuardParams);
            }
        }

        public override void OnEnter(Mobile m)
        {
            if (IsGuarded == false)
                return;

            //m.SendLocalizedMessage( 500112 ); // You are now under the protection of the town guards.

            if (m.Murderer)
                CheckGuardCandidate(m);
        }

        public override void OnExit(Mobile m)
        {
            if (IsGuarded == false)
                return;

            //m.SendLocalizedMessage( 500113 ); // You have left the protection of the town guards.
        }

        public override void OnSpeech(SpeechEventArgs args)
        {
            if (IsGuarded == false)
                return;

            if (args.Mobile.Alive && args.HasKeyword(0x0007)) // *guards*
                CallGuards(args.Mobile.Location);
        }

        public override void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
            base.OnAggressed(aggressor, aggressed, criminal);

            if (IsGuarded && aggressor != aggressed && criminal)
                CheckGuardCandidate(aggressor);
        }

        public override void OnGotBenificialAction(Mobile helper, Mobile helped)
        {
            base.OnGotBenificialAction(helper, helped);

            if (IsGuarded == false)
                return;

            int noto = Notoriety.Compute(helper, helped);

            if (helper != helped && (noto == Notoriety.Criminal || noto == Notoriety.Murderer))
                CheckGuardCandidate(helper);
        }

        public override void OnCriminalAction(Mobile m, bool message)
        {
            base.OnCriminalAction(m, message);

            if (IsGuarded)
                CheckGuardCandidate(m);
        }

        private Hashtable m_GuardCandidates = new Hashtable();

        public void CheckGuardCandidate(Mobile m)
        {
            if (IsGuarded == false)
                return;

            if (IsGuardCandidate(m))
            {
                GuardTimer timer = (GuardTimer)m_GuardCandidates[m];

                if (timer == null)
                {
                    timer = new GuardTimer(m, m_GuardCandidates);
                    timer.Start();

                    m_GuardCandidates[m] = timer;
                    m.SendLocalizedMessage(502275); // Guards can now be called on you!

                    // okay, look for a nearby mobile that may have seen the crime .. no complaint, no investegation!
                    if (m.Map != null)
                    {
                        Mobile fakeCall = null;
                        double prio = 0.0;

                        IPooledEnumerable eable = m.GetMobilesInRange(8);
                        foreach (Mobile v in eable)
                        {   // make sure the towns person remembers the mobile in question
                            if (!v.Player && v.Body.IsHuman && v != m && !IsGuardCandidate(v) && v.Remembers(m))
                            {
                                //Pixie 10/28/04: checking whether v is in the region fixes the problem
                                // where player1 recalls to a guardzone before player2's explosion hits ...
                                if (this.Contains(v.Location))
                                {
                                    double dist = m.GetDistanceToSqrt(v);

                                    if (fakeCall == null || dist < prio)
                                    {
                                        fakeCall = v;
                                        prio = dist;
                                    }
                                }
                                else
                                {
                                    //System.Console.WriteLine( "Mobile ({0}) isn't in this region, so skip him!", v.Name );
                                }
                            }
                        }
                        eable.Free();

                        if (fakeCall != null)
                        {
                            fakeCall.Say(Utility.RandomList(1007037, 501603, 1013037, 1013038, 1013039, 1013041, 1013042, 1013043, 1013052));
                            MakeGuards(m);
                            m_GuardCandidates.Remove(m);
                            m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                        }
                    }
                }
                else
                {
                    timer.Stop();
                    timer.Start();
                }
            }
        }

        public void CallGuards(Point3D p)
        {
            if (IsGuarded == false)
                return;

            IPooledEnumerable eable = Map.GetMobilesInRange(p, 14);

            foreach (Mobile m in eable)
            {
                if (IsGuardCandidate(m) && ((m.Murderer && Mobiles.ContainsKey(m.Serial)) || m_GuardCandidates.Contains(m)))
                {
                    if (m.BankBox != null) // Old Salty - Added to close the bankbox of a criminal on GuardCall
                        m.BankBox.Close();

                    MakeGuards(m);
                    m_GuardCandidates.Remove(m);

                    // add your 'reds in town' shards here:
                    bool reds_in_town = Core.RuleSets.SiegeRules() || Core.RuleSets.MortalisRules();

                    if (reds_in_town || m.LongTermMurders < 5)
                        m.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                    break;
                }
            }

            eable.Free();
        }

        public bool IsGuardCandidate(Mobile m)
        {
            if (m is BaseGuard || m is PlayerVendor || !m.Alive || m.AccessLevel > AccessLevel.Player || m.Blessed || !IsGuarded)
                return false;

            IPooledEnumerable eable = m.GetMobilesInRange(10);
            foreach (Mobile check in eable)
            {
                BaseGuard guard = check as BaseGuard;
                if (guard != null && guard.Focus == m)
                {
                    eable.Free();
                    return false;
                }
            }
            eable.Free();

            // add your 'reds in town' shards here:
            bool reds_in_town = Core.RuleSets.SiegeRules() || Core.RuleSets.MortalisRules();

            return (reds_in_town ? false : m.Murderer) || m.Criminal;
        }

        private class GuardTimer : Timer
        {
            private Mobile m_Mobile;
            private Hashtable m_Table;

            public GuardTimer(Mobile m, Hashtable table)
                : base(TimeSpan.FromSeconds(15.0))
            {
                Priority = TimerPriority.TwoFiftyMS;

                m_Mobile = m;
                m_Table = table;
            }

            protected override void OnTick()
            {
                if (m_Table.Contains(m_Mobile))
                {
                    m_Table.Remove(m_Mobile);
                    if (m_Mobile.LongTermMurders < 5)
                        m_Mobile.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                }
            }
        }
    }
}
