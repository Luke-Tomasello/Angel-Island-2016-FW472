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

/* Scripts/Mobiles/Animals/Birds/Chicken.cs
 *	ChangeLog :
 *	5/12/10, Adam
 *		update AttackOrderHack to account for ScaredOfScaryThings
 *	06/18/07 Taran Kain
 *		Fixed proxy aggression with new abstraction
 *  6/13/07, Adam
 *      Emergency try/catch in HatchTick to stop this exception.
 *      We'll look deeper tomorrow.
 *      Exception: 
 *          System.NullReferenceException: Object reference not set to an instance of an object. 
 *          at Server.Mobiles.BaseCreature.Delete() 
 *          at Server.Mobiles.ChickenEgg.HatchTick() 
 *          at Server.Mobiles.ChickenEgg.<OnTick>b__0(ChickenEgg egg) 
 *  04/22/07 Taran Kain
 *      Fix disappearing eggs, hatching messages
 *  04/16/07 Taran Kain
 *      Fixed HitsMax to never return 0
 *  04/09/07 Taran Kain
 *      Bugfixes: Fixed ClientsInRange check to use GetWorldLocation, fixed house lockdown/egg deletion issue.
 *  04/05/07 Taran Kain
 *      Fixed crash in ChickenEgg
 *  04/04/07 Taran Kain
 *      Redesigned ChickenAI to be better encapsulated
 *      Added SpecialHue flag for reward chickens
 *      Made only tame chickens mate
 *  4/3/07, Adam
 *      Add a BreedingEnabled bit check
 *  03/29/07 Taran Kain
 *      Bugfixes
 *      Changed ChickenAI.DoActionInteract from a wannabe state machine to a real state machine
 *  03/28/07 Taran Kain
 *      Added in Genes to Chicken
 *      Created ChickenAI
 *      Created ChickenEgg
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
*/

using Server.Commands;
using Server.Items;
using Server.Multis;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Mobiles
{
    public class ChickenEgg : Eggs
    {
        private enum HatchState
        {
            Rustle,
            Crack,
            Beak,
            Split,
            Hatch
        }

        private static List<ChickenEgg> m_Eggs;
        private static Timer m_Tick;

        public static void Configure()
        {
            m_Eggs = new List<ChickenEgg>();
            m_Tick = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMinutes(5.0), new TimerCallback(OnTick));
            m_Tick.Priority = TimerPriority.OneMinute;
            m_Tick.Start();
        }

        private static void OnTick()
        {
            // kinda complicated at first - simple in concept
            // remove any eggs from list IF: egg is null, egg is deleted, or egg.HatchTick() returns false
            try
            {
                m_Eggs.RemoveAll(new Predicate<ChickenEgg>(delegate (ChickenEgg egg) { return (egg == null || egg.Deleted || !egg.HatchTick()); }));
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        private Chicken m_Chick;
        private DateTime m_Birthdate;
        private Timer m_Hatch;
        private int m_Health;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Birthdate
        {
            get
            {
                return m_Birthdate;
            }
            set
            {
                m_Birthdate = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Health
        {
            get
            {
                return m_Health;
            }
            set
            {
                m_Health = value;

                if (m_Health > 24)
                    m_Health = 24;
            }
        }

        public ChickenEgg(Chicken child)
            : base()
        {
            m_Birthdate = DateTime.UtcNow + TimeSpan.FromDays(2.0 + Utility.RandomDouble());
            m_Chick = child;
            m_Hatch = null;
            m_Health = 5;

            if (m_Chick != null)
            {
                m_Chick.SpawnerTempMob = true;
                m_Eggs.Add(this);
            }
        }

        private bool HatchTick()
        {
            try
            {
                if (m_Chick == null || m_Chick.Deleted)
                    return false;

                TimeSpan ts = m_Birthdate - DateTime.UtcNow;

                if (ts < TimeSpan.Zero && Health > 0)
                {
                    IPooledEnumerable eable = Map.GetClientsInRange(GetWorldLocation());
                    int clients = 0;
                    foreach (Server.Network.NetState ns in eable)
                        clients++;
                    eable.Free();

                    // wait until someone's around to actually begin hatching
                    if (clients > 0)
                    {
                        if (m_Hatch != null)
                            m_Hatch.Stop();
                        m_Hatch = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), new TimerStateCallback(Hatch), HatchState.Rustle);
                        m_Hatch.Start();

                        return false; // we're done! remove us from the list
                    }
                }

                if (Utility.RandomDouble() < (ts.TotalHours * ts.TotalHours / 324)) // (ts.TotalHours / 18) ^ 2
                {
                    if (m_Hatch != null)
                        m_Hatch.Stop();
                    m_Hatch = Timer.DelayCall(TimeSpan.FromMinutes(Utility.RandomDouble() * 4), new TimerCallback(Rustle));
                    m_Hatch.Start();
                }

                // adjust health accordingly
                if (RootParent is Mobile || IsLockedDown)
                    Health++;
                else
                    Health--;

                if (Health < -5 && ts.TotalDays < -2)
                {
                    // we die
                    m_Chick.Delete();
                    m_Birthdate = DateTime.MinValue;

                    return false; // remove us from tick list
                }

                return true;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                return false; // remove us from tick list
            }
        }

        private void Rustle()
        {
            PublicOverheadMessage(Server.Network.MessageType.Regular, 0, true, "*rustles*");
            Effects.PlaySound(Location, Map, Utility.RandomList(826, 827));
        }

        private void Hatch(object ob)
        {
            HatchState state = HatchState.Rustle;
            try
            {
                state = (HatchState)ob;
            }
            catch
            {
                m_Hatch.Stop();
                Console.WriteLine("{0} passed to ChickenEgg.Hatch(). Tell Taran Kain.", ob);
                return;
            }

            if (m_Chick == null || m_Chick.Deleted)
            {
                m_Hatch.Stop();
                return;
            }

            switch (state)
            {
                case HatchState.Rustle:
                    {
                        if (Utility.RandomDouble() < .4)
                            Rustle();

                        if (Utility.RandomDouble() < .05)
                        {
                            m_Hatch.Stop();
                            m_Hatch = Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble() + 2), TimeSpan.FromSeconds(2),
                                new TimerStateCallback(Hatch), HatchState.Crack);
                            m_Hatch.Start();
                        }

                        break;
                    }
                case HatchState.Crack:
                    {
                        if (Utility.RandomDouble() < .7)
                        {
                            Effects.PlaySound(Location, Map, Utility.RandomList(828, 829));
                            PublicOverheadMessage(Server.Network.MessageType.Regular, 0, true, "You notice some cracks!");
                        }

                        if (Utility.RandomDouble() < .2)
                        {
                            m_Hatch.Stop();
                            m_Hatch = Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble() + 4), TimeSpan.FromSeconds(1),
                                new TimerStateCallback(Hatch), HatchState.Beak);
                            m_Hatch.Start();
                        }

                        break;
                    }
                case HatchState.Beak:
                    {
                        if (Utility.RandomDouble() < .4)
                        {
                            Effects.PlaySound(Location, Map, Utility.RandomList(828, 829));
                            PublicOverheadMessage(Server.Network.MessageType.Regular, 0, true, "You can see a beak!");
                        }

                        if (Utility.RandomDouble() < .1)
                        {
                            m_Hatch.Stop();
                            m_Hatch = Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble() + 3), new TimerStateCallback(Hatch), HatchState.Split);
                            m_Hatch.Start();
                        }

                        break;
                    }
                case HatchState.Split:
                    {
                        Effects.PlaySound(Location, Map, Utility.RandomList(308, 309));
                        PublicOverheadMessage(Server.Network.MessageType.Regular, 0, true, "The egg splits open!");

                        m_Hatch.Stop();
                        m_Hatch = Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble() * 0.5), new TimerStateCallback(Hatch), HatchState.Hatch);
                        m_Hatch.Start();

                        break;
                    }
                case HatchState.Hatch:
                    {
                        m_Chick.SpawnerTempMob = false;
                        m_Chick.MoveToWorld(GetWorldLocation(), Map);
                        m_Chick.PlaySound(Utility.RandomList(111, 113, 114, 115));
                        new Eggshells().MoveToWorld(GetWorldLocation(), Map);
                        Delete();

                        break;
                    }
            }
        }

        public override void OnDelete()
        {
            m_Eggs.Remove(this);

            if (!IsFreezeDrying)
            {
                if (m_Chick != null)
                    m_Chick.SpawnerTempMob = false; // chick will get auto-cleaned up

                BaseHouse bh;
                if (IsLockedDown && (bh = BaseHouse.FindHouseAt(this)) != null)
                    bh.LockDowns.Remove(this);
            }
        }

        public ChickenEgg(Serial s)
            : base(s)
        {
            m_Eggs.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            // version 0
            writer.Write(m_Birthdate);
            writer.Write(m_Chick);
            writer.Write(m_Health);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Birthdate = reader.ReadDateTime();
                        m_Chick = reader.ReadMobile() as Chicken;
                        m_Health = reader.ReadInt();

                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool InList
        {
            get
            {
                return m_Eggs.Contains(this);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ValidChick
        {
            get
            {
                return (m_Chick != null && !m_Chick.Deleted);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsInRange
        {
            get
            {
                IPooledEnumerable eable = Map.GetClientsInRange(Location);
                int clients = 0;
                foreach (Server.Network.NetState ns in eable)
                    clients++;
                eable.Free();

                return clients;
            }
        }
    }

    public class ChickenAI : AnimalAI
    {
        public enum BreedState
        {
            None,
            Approaching,
            FightingCompetition,
            MovingIn,
            Mating
        }

        private Chicken m_MateTarget;
        private DateTime m_BecameIdle;
        private DateTime m_NextMateAttempt;
        private DateTime m_LastMate;
        private DateTime m_BeganTheNasty;
        private int m_FightDistance;
        private List<Chicken> m_Ignore;
        private BreedState m_BreedingState;

        private static TimeSpan MateIdleDelay = TimeSpan.FromMinutes(2.0);
        private static TimeSpan MaleMateDelay = TimeSpan.FromHours(2.0);
        private static TimeSpan FemaleMateDelay = TimeSpan.FromHours(6.0);
        private static TimeSpan MateDuration = TimeSpan.FromSeconds(10.0);
        private const int MinMateAttemptDelay = 20;
        private const int MaxMateAttemptDelay = 45;
        private const double MateAttemptChance = 0.1;
        private const double MateAcceptChance = 0.6;
        private const double MateSuccessChance = 0.25;
        private const double StillBornChance = 0.05;
        private const double MateHealthThreshold = 0.8;

        public BreedState BreedingState
        {
            get
            {
                return m_BreedingState;
            }
            set
            {
                m_BreedingState = value;

                if (m_BreedingState != BreedState.FightingCompetition)
                    m_Ignore = null;
                if (m_BreedingState == BreedState.None)
                    m_MateTarget = null;
            }
        }

        public ChickenAI(BaseCreature m)
            : base(m)
        {
            m_MateTarget = null;
            m_BecameIdle = DateTime.UtcNow;
            m_NextMateAttempt = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(MinMateAttemptDelay, MaxMateAttemptDelay));
            m_LastMate = DateTime.MinValue;
            m_BeganTheNasty = DateTime.MaxValue;
            m_Ignore = null;
        }

        public override bool DoOrderNone()
        {
            return Think();
        }

        public override bool DoActionWander()
        {
            // try breeding?
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BreedingEnabled))
                if (!m_Mobile.Female &&                                                     // males do the finding
                    (double)m_Mobile.Hits / m_Mobile.HitsMax > MateHealthThreshold &&       // must be minimum health
                    m_BecameIdle + MateIdleDelay < DateTime.UtcNow &&                          // wait after starting to wander
                    m_LastMate + MaleMateDelay < DateTime.UtcNow &&                                // wait after last mating - we gotta recoup!
                    m_NextMateAttempt < DateTime.UtcNow)                                       // gotta give it a while between looking
                {
                    FindMate();
                    m_NextMateAttempt += TimeSpan.FromSeconds(Utility.RandomMinMax(MinMateAttemptDelay, MaxMateAttemptDelay));

                    if (m_MateTarget != null && Utility.RandomDouble() < MateAttemptChance)
                    {
                        m_Mobile.DebugSay("Found a mate I like! Trying to mate...");

                        BreedingState = BreedState.Approaching;
                        Action = ActionType.Interact;
                        m_Mobile.PlaySound(Utility.RandomList(111, 113, 114, 115));
                    }

                    return true;
                }

            return base.DoActionWander();
        }

        public override bool DoActionInteract()
        {
            m_Mobile.DebugSay("Interacting..");

            if (!m_Mobile.Controlled)
            {
                m_Mobile.DebugSay("I'm wild now... for some reason, I don't wanna bone anymore. Hm.");

                if (m_MateTarget != null)
                    m_MateTarget.EndMate(false);
                EndMate(false);

                Action = ActionType.Wander;

                return true;
            }

            if (m_Mobile.Deleted || !m_Mobile.Alive)
            {
                m_Mobile.DebugSay("Oh shit, I've got bigger things to worry about now.");

                if (m_MateTarget != null)
                    m_MateTarget.EndMate(false);
                EndMate(false);

                Action = ActionType.Wander;

                return true;
            }

            if (m_MateTarget == null)
            {
                m_Mobile.DebugSay("My mate target is gone! Going back to wander...");

                EndMate(false);
                Action = ActionType.Wander;

                return true;
            }

            if (m_Mobile.Female)
            {
                // stand still and get mated with - occasionally make sounds
                if (Utility.RandomDouble() < .3)
                    m_Mobile.PlaySound(Utility.RandomList(111, 113, 114, 115));

                return true;
            }

            switch (BreedingState)
            {
                case BreedState.Approaching:
                    {
                        // if we are not at most one tile away from mate target, get one tile away
                        if (WalkMobileRange(m_MateTarget, 1, false, 0, 1))
                        {
                            m_FightDistance = -1 + (int)Math.Round((m_Mobile.Temper + Utility.RandomMinMax(-10, 10)) / 35.0);
                            m_Ignore = new List<Chicken>();
                            BreedingState = BreedState.FightingCompetition;
                        }

                        break;
                    }
                case BreedState.FightingCompetition:
                    {
                        // depending on temper, fight all other male chickens near target
                        if (m_FightDistance > -1)
                        {
                            IPooledEnumerable eable = m_Mobile.Map.GetMobilesInRange(m_MateTarget.Location, m_FightDistance);
                            foreach (Mobile m in eable)
                            {
                                Chicken c = m as Chicken;

                                // wisdom, temper and target's damagemin/max affect chance to attack
                                if (c != null && !c.Female && c != m_Mobile && !m_Ignore.Contains(c) &&
                                    (m_Mobile.Temper + Utility.RandomMinMax(-10, 10)) > (m_Mobile.Wisdom + c.DamageMax + c.DamageMin))
                                {
                                    m_Mobile.Combatant = c;
                                    m_Mobile.DebugSay("Get away from my woman!");
                                    Action = ActionType.Combat;
                                    return true;
                                }
                                else
                                    m_Ignore.Add(c);
                            }
                            eable.Free();
                        }

                        // if we got here, then we're done fighting away competition
                        m_Ignore = null;
                        BreedingState = BreedState.MovingIn;

                        break;
                    }
                case BreedState.MovingIn:
                    {
                        // if we are not same loc as target, get same loc
                        m_Mobile.DebugSay("Gettin in close...");
                        if (WalkMobileRange(m_MateTarget, 1, false, 0, 0))
                        {
                            if (m_MateTarget.CheckBreedWith(m_Mobile)) // does she like us?
                            {
                                BeginMate(m_MateTarget);
                                m_MateTarget.BeginMate(m_Mobile as Chicken);

                                BreedingState = BreedState.Mating;
                            }
                            else // shit! rejected!
                            {
                                m_Mobile.DebugSay("Shit! Rejected!");

                                // do NOT endmate on woman! she might be mating with someone else.
                                EndMate(false);
                            }
                        }

                        break;
                    }
                case BreedState.Mating:
                    {
                        if (!m_MateTarget.CheckBreedWith(m_Mobile)) // does she STILL like us?
                        {
                            // crap, she doesn't
                            m_MateTarget.EndMate(false); // important that this goes first, as EndMateWith nullifies m_MateTarget
                            EndMate(false);

                            m_Mobile.DebugSay("Shit, she don't like me anymore.");

                            break;
                        }

                        if (m_BeganTheNasty + MateDuration < DateTime.UtcNow)
                        {
                            // patience affects chance to successfully procreate
                            if (Utility.RandomDouble() < (MateSuccessChance + (m_Mobile.Patience - 10) / 500.0))
                            {
                                m_Mobile.DebugSay("Smokin a cig...");

                                m_Mobile.PlaySound(Utility.RandomList(111, 113, 114, 115));

                                Chicken child = m_MateTarget.BreedWith(m_Mobile) as Chicken;
                                ChickenEgg egg;

                                if (Utility.RandomDouble() < StillBornChance)
                                    egg = new ChickenEgg(null);
                                else
                                    egg = new ChickenEgg(child);

                                egg.MoveToWorld(m_Mobile.Location, m_Mobile.Map);

                                m_MateTarget.EndMate(true);
                                EndMate(true);
                            }
                            else
                            {
                                m_Mobile.DebugSay("Crap, 'sploded early.");

                                if (m_MateTarget != null)
                                    m_MateTarget.EndMate(false);
                                EndMate(false);

                                m_Mobile.PlaySound(112);
                            }

                            return true;
                        }
                        else
                        {
                            m_Mobile.DebugSay("Get down tonight...");

                            if (Utility.RandomDouble() < .3)
                                m_Mobile.PlaySound(Utility.RandomList(111, 113, 114, 115));

                            return true;
                        }
                    }
                case BreedState.None:
                    {
                        m_Mobile.DebugSay("I'm not supposed to be breeding. Going back to wander..");

                        Action = ActionType.Wander;

                        break;
                    }
                default:
                    {
                        Console.WriteLine("{0} had an invalid BreedingState (= {1}). Reverting to none.", this, BreedingState);
                        BreedingState = BreedState.None;

                        break;
                    }
            }

            return true;
        }

        public override bool DoActionCombat()
        {
            // run this logic only for males, and only when trying to mate
            if (!m_Mobile.Female && m_MateTarget != null && BreedingState == BreedState.FightingCompetition)
            {
                if (m_Mobile.Combatant == null ||
                    m_Mobile.Deleted ||
                    m_Mobile.Combatant.Map != m_Mobile.Map ||
                    m_Mobile.Combatant.GetDistanceToSqrt(m_MateTarget) > m_FightDistance)
                {
                    m_Mobile.Combatant = null;
                    m_Mobile.DebugSay("They're far enough away, going back to the bedroom");
                    Action = ActionType.Interact;

                    return true;
                }
            }
            else if (BreedingState != BreedState.None)// going into combat mode during anything but fighting away competition -> forget about the mate
            {
                if (m_MateTarget != null)
                    m_MateTarget.EndMate(false);
                EndMate(false);
            }

            return base.DoActionCombat();
        }

        public override void OnActionChanged()
        {
            m_BeganTheNasty = DateTime.MaxValue;

            if (Action == ActionType.Wander)
                m_BecameIdle = DateTime.UtcNow;

            base.OnActionChanged();
        }

        public override void OnCurrentOrderChanged()
        {
            if (m_Mobile.ControlOrder == OrderType.None)
            {
                m_BecameIdle = DateTime.UtcNow; // action must = wander and order must = none, setting this in both ensures max wait
            }

            if (m_MateTarget != null)
                m_MateTarget.EndMate(false);
            EndMate(false);

            base.OnCurrentOrderChanged();
        }

        protected virtual void FindMate()
        {
            if (m_Mobile.Female)
                return;

            m_Mobile.DebugSay("Love shack.. In the looove shack...");
            IPooledEnumerable eable = m_Mobile.Map.GetMobilesInRange(m_Mobile.Location);
            m_MateTarget = null;
            double dist = Double.MaxValue;

            // find the closest female chicken
            foreach (Mobile m in eable)
            {
                Chicken c = m as Chicken;
                double d = m_Mobile.GetDistanceToSqrt(m);
                if (c != null && c.Female && d < dist)
                {
                    m_MateTarget = c;
                    dist = d;
                }
            }
            eable.Free();

            if (m_MateTarget != null)
                m_Mobile.DebugSay("A woman!");
            else
                m_Mobile.DebugSay("Man, it's a sausage fest around here...");
        }

        public bool CheckMateAccept(Chicken male)
        {
            if (m_Mobile.Deleted || !m_Mobile.Alive)
                return false; // we've got bigger shit to worry about

            if (!m_Mobile.Female || male == null || male.Female)
                return false; // chickens only swing one way

            if (!m_Mobile.Controlled)
                return false; // only tame chickens mate

            if ((Action != ActionType.Wander && Action != ActionType.Interact) ||
                m_Mobile.ControlOrder != OrderType.None || m_BecameIdle + MateIdleDelay > DateTime.UtcNow)
                return false; // we are busy

            if (m_MateTarget == male)
                return true; // we're already mating with them, they're ok

            if (m_MateTarget != null && m_MateTarget != male)
                return false; // we're already mating with someone else

            if (m_LastMate + FemaleMateDelay > DateTime.UtcNow)
                return false; // haven't waited long enough yet - need to recoup!

            // male's wisdom can up chances up to .10, our patience can up chances up to .05, our temper can lower chances up to .15
            double score = Utility.RandomDouble() - male.Wisdom / 1000.0 - m_Mobile.Patience / 2000.0 + m_Mobile.Temper * 3.0 / 2000.0;
            if (score < MateAcceptChance)
                return true;
            else
                return false;
        }

        public void BeginMate(Chicken mate)
        {
            // mark us as gettin down
            Action = ActionType.Interact; // important that this happens BEFORE setting BeganTheNasty - OnActionChanged will overwrite it otherwise
            m_BeganTheNasty = DateTime.UtcNow;
            m_MateTarget = mate;
        }

        public void EndMate(bool success)
        {
            if (success)
            {
                m_LastMate = DateTime.UtcNow;
            }

            m_MateTarget = null;
            m_NextMateAttempt = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(MinMateAttemptDelay, MaxMateAttemptDelay));
            m_BeganTheNasty = DateTime.MaxValue;
            BreedingState = BreedState.None;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }

    [CorpseName("a chicken corpse")]
    public class Chicken : BaseCreature
    {
        private int m_StrMax, m_IntMax, m_DexMax;
        private double m_StatCapFactor;
        private double m_HitsMaxFactor;
        private int m_Meat, m_Feathers;
        private bool m_SpecialHue;

        protected override void AttackOrderHack(Mobile aggressor)
        {
            if (AIObject is ChickenAI && ((ChickenAI)AIObject).BreedingState != ChickenAI.BreedState.FightingCompetition)
            {
                // don't attack Scary creatures if we are ScaredOfScaryThings
                if (aggressor is BaseCreature && (((BaseCreature)aggressor).IsScaryToPets && ((BaseCreature)aggressor).IsScaryCondition()) && this.IsScaredOfScaryThings)
                    return;

                ControlTarget = aggressor;
                ControlOrder = OrderType.Attack;
            }
        }

        [Constructable]
        public Chicken()
            : base(AIType.AI_Chicken, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a chicken";
            Body = 0xD0;
            BaseSoundID = 0x6E;

            SetStr(StrMax / 6 - 1, StrMax / 6 + 1);
            SetDex(DexMax / 2 - 2, DexMax / 2 + 2);
            SetInt(IntMax / 4 - 1, IntMax / 4 + 1);

            SetSkill(SkillName.MagicResist, 4.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override bool CheckBreedWith(BaseCreature male)
        {
            if (!(male is Chicken))
                return false;

            if (!(AIObject is ChickenAI))
                return false;

            if ((AIObject as ChickenAI).CheckMateAccept(male as Chicken))
                return base.CheckBreedWith(male);

            return false;
        }

        public virtual void BeginMate(Chicken mate)
        {
            if (AIObject is ChickenAI)
                (AIObject as ChickenAI).BeginMate(mate);
        }

        public virtual void EndMate(bool success)
        {
            if (AIObject is ChickenAI)
                (AIObject as ChickenAI).EndMate(success);
        }

        public override string DescribeGene(PropertyInfo prop, GeneAttribute attr)
        {
            switch (attr.Name)
            {
                case "Meat":
                    {
                        switch (Meat)
                        {
                            case 1: return "Frail";
                            case 2: return "Lean";
                            case 3: return "Brawny";
                            case 4: return "Colossal";
                        }
                        break;
                    }
                case "Feathers":
                    {
                        if (Feathers < 22)
                            return "Nearly Bald";
                        if (Feathers < 24)
                            return "Thin";
                        if (Feathers < 26)
                            return "Healthy";
                        if (Feathers < 28)
                            return "Thick";
                        else
                            return "Extremely Thick";
                    }
                case "Physique":
                    {
                        if (HitsMaxFactor < .85)
                            return "Frail";
                        if (HitsMaxFactor < .90)
                            return "Spindly";
                        if (HitsMaxFactor < .95)
                            return "Slight";
                        if (HitsMaxFactor < 1.00)
                            return "Lithe";
                        if (HitsMaxFactor < 1.05)
                            return "Sturdy";
                        else
                            return "Tough";
                    }
                case "Versatility":
                    {
                        if (StatCapFactor < 1.97)
                            return "Limited";
                        if (StatCapFactor < 1.99)
                            return "Reserved";
                        if (StatCapFactor < 2.01)
                            return "Able";
                        if (StatCapFactor < 2.03)
                            return "Versatile";
                        else
                            return "Dynamic";
                    }
                case "Talon Accuracy":
                    {
                        switch (DamageMin)
                        {
                            case 1:
                                return "Unwieldy";
                            case 2:
                                return "Deft";
                            case 3:
                                return "Precise";
                        }
                        break;
                    }
                case "Talon Size":
                    {
                        switch (DamageMax)
                        {
                            case 1:
                                return "Undeveloped";
                            case 2:
                                return "Small";
                            case 3:
                                return "Ample";
                            case 4:
                                return "Large";
                            case 5:
                                return "Frightening";
                        }
                        break;
                    }
                case "Feather Armor":
                    {
                        switch (VirtualArmor)
                        {
                            case 1:
                                return "Flimsy";
                            case 2:
                                return "Delicate";
                            case 3:
                                return "Durable";
                            case 4:
                                return "Rugged";
                            case 5:
                                return "Hard";
                            case 6:
                                return "Solid";
                        }
                        break;
                    }
                default:
                    return base.DescribeGene(prop, attr);
            }

            return "Error";
        }

        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override FoodType FavoriteFood { get { return FoodType.GrainsAndHay; } }

        public override bool BreedingEnabled
        {
            get
            {
                return true;
            }
        }

        [Gene("Meat", .33, .33, 1, 2, 1, 4, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Meat
        {
            get
            {
                return m_Meat;
            }
            set
            {
                m_Meat = value;
            }
        }

        [Gene("Feathers", .25, .25, 23, 27, 20, 30, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Feathers
        {
            get
            {
                return m_Feathers;
            }
            set
            {
                m_Feathers = value;
            }
        }

        [Gene("StrMax", 20, 30, 10, 100)]
        public override int StrMax
        {
            get
            {
                return m_StrMax;
            }
            set
            {
                m_StrMax = value;
            }
        }

        [Gene("IntMax", 15, 20, 10, 30)]
        public override int IntMax
        {
            get
            {
                return m_IntMax;
            }
            set
            {
                m_IntMax = value;
            }
        }

        [Gene("DexMax", 25, 35, 20, 120)]
        public override int DexMax
        {
            get
            {
                return m_DexMax;
            }
            set
            {
                m_DexMax = value;
            }
        }

        [Gene("Physique", .05, .05, .85, 1.0, .80, 1.10, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double HitsMaxFactor
        {
            get
            {
                return m_HitsMaxFactor;
            }
            set
            {
                m_HitsMaxFactor = value;
            }
        }

        [Gene("Versatility", .05, .05, 1.975, 2.025, 1.95, 2.05, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double StatCapFactor
        {
            get
            {
                return m_StatCapFactor;
            }
            set
            {
                m_StatCapFactor = value;
            }
        }

        [Gene("Talon Accuracy", .05, .05, 1, 1, 1, 3, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DamageMin
        {
            get
            {
                return base.DamageMin;
            }
            set
            {
                base.DamageMin = value;
            }
        }

        [Gene("Talon Size", .05, .05, 1, 1, 1, 5, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int DamageMax
        {
            get
            {
                return base.DamageMax;
            }
            set
            {
                base.DamageMax = value;
            }
        }

        [Gene("Feather Armor", .05, .05, 1, 3, 1, 6, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int VirtualArmor
        {
            get
            {
                return base.VirtualArmor;
            }
            set
            {
                base.VirtualArmor = value;
            }
        }

        [Gene("Special Hue", 0, 0, 0, 0, 0, 1, GeneVisibility.Invisible, 0.0)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public double SpecialHue
        {
            get
            {
                if (m_SpecialHue)
                    return 1.0;
                else
                    return 0.0;
            }
            set
            {
                if (value == 0.0)
                    m_SpecialHue = false;
                else
                {
                    m_SpecialHue = true;
                    if (Utility.RandomDouble() < 0.05)
                        Hue = 642; // 5% of the time we get a brown chicken
                }
            }
        }

        public Chicken(Serial serial)
            : base(serial)
        {
            m_HitsMaxFactor = 65000; // hackish, but make sure HitsMax has a value never attainable before loading Hits
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2);

            // version 2
            writer.Write((bool)m_SpecialHue);

            // version 1
            writer.Write((int)m_StrMax);
            writer.Write((int)m_IntMax);
            writer.Write((int)m_DexMax);
            writer.Write((double)m_StatCapFactor);
            writer.Write((double)m_HitsMaxFactor);
            writer.Write((int)m_Meat);
            writer.Write((int)m_Feathers);

            // version 0
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_SpecialHue = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_StrMax = reader.ReadInt();
                        m_IntMax = reader.ReadInt();
                        m_DexMax = reader.ReadInt();
                        m_StatCapFactor = reader.ReadDouble();
                        m_HitsMaxFactor = reader.ReadDouble();
                        m_Meat = reader.ReadInt();
                        m_Feathers = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            // do this AFTER reading in all other data, since we might have changed some of the limits
            if (version < 1)
                InitializeGenes();
        }

        public override void ValidateGenes()
        {
            if (DamageMin > DamageMax)
            {
                int t = DamageMin;
                DamageMin = DamageMax;
                DamageMax = t;
            }

            base.ValidateGenes();
        }

        public override int StatCap
        {
            get
            {
                return -1;
            }
            set
            {
            }
        }

        public override int HitsMax
        {
            get
            {
                return Math.Max((int)Math.Round(Str * HitsMaxFactor), 1); // max to just double-check, Round should make sure it's never 0
            }
        }

        public override void ValidateStatCap(Stat increased)
        {
            double rawStatTotalFactor = (RawStr / (double)StrMax) +
                                        (RawDex / (double)DexMax) +
                                        (RawInt / (double)IntMax);
            if (rawStatTotalFactor <= StatCapFactor)
                return; // no work to do

            int ts = RawStr, ti = RawInt, td = RawDex;
            switch (increased)
            {
                case Stat.Str:
                    {
                        if (CanLower(Stat.Dex) && ((RawDex / (double)DexMax) > (RawInt / (double)IntMax) || !CanLower(Stat.Int)))
                            RawDex--;
                        else if (CanLower(Stat.Int))
                            RawInt--;
                        else
                            RawStr--;

                        break;
                    }
                case Stat.Dex:
                    {
                        if (CanLower(Stat.Str) && ((RawStr / (double)StrMax) > (RawInt / (double)IntMax) || !CanLower(Stat.Int)))
                            RawStr--;
                        else if (CanLower(Stat.Int))
                            RawInt--;
                        else
                            RawDex--;

                        break;
                    }
                case Stat.Int:
                    {
                        if (CanLower(Stat.Dex) && ((RawDex / (double)DexMax) > (RawStr / (double)StrMax) || !CanLower(Stat.Str)))
                            RawDex--;
                        else if (CanLower(Stat.Str))
                            RawStr--;
                        else
                            RawInt--;

                        break;
                    }
                default:
                    {
                        return;
                    }
            }
            if ((ts + td + ti) <= (RawStr + RawInt + RawDex))
            {
                // EXTREMELY important - break recursion here, as no change occurred. Str/Int/Max have setter checks to make sure doesn't go <10
                // if this tries to go all the way down to that, we WILL lock server, cause stack overflow exception and NOT restart!

                Console.WriteLine("Warning: Chicken.ValidateStatCap didn't change any stats when it should have. Tell Taran Kain.");
                Console.WriteLine("Stats: {0} {1} {2}", RawStr, RawInt, RawDex);
                return;
            }

            // damn do i love recursion, it makes my life soooo so easy
            // re-call ourselves, as the differing stat values makes 1 str point worth less than 1 dex point
            // thus, we may actually have to go through several steps of dropping stats to validate statcap
            ValidateStatCap(increased);
        }
    }
}
