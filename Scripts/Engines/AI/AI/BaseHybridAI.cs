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

/* Scripts/Engines/AI/AI/BaseHybridAI.cs
 * CHANGELOG
 *	7/11/10, adam
 *		o major reorganization of AI
 *			o push most smart-ai logic from the advanced magery classes down to baseAI so that we can use potions and bandages from 
 *				the new advanced melee class
 *	05/25/09, plasma
 *		- Added magery check in one key place to prevent mobs that use this AI with no magery to not stand there tying to cast.
 *		- Forced the reveal code to always "remember" their ConstantFocus mob
 *	1/10/09, Adam
 *		Total rewrite of 'reveal' implementation
 *	1/1/09, Adam
 *		- Now uses real potions and real bandages
 *		- Convert from CrosshealMageAI to new BaseHybridAI
 *		- Cross heals is now an option
 *		- Add new Serialization model for creature AI.
 *		- BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		- Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	12/31/08, Adam
 *		- Add Bandage support
 *		- Fix some horrible code where Catch was being used to set the default spell :\
 *		- Refactoring names and properties
 *		- more ugly code cleanup
 *		- Add healing for warriors
 *	12/28/098, Adam
 *		Redesign to work with the all new HybridAI (use PreferMagic() method in decision making)
 *	12/26/08, Adam
 *		Recast as CrosshealingMageAI from GolemControllerAI
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  7/15/07, Adam
 *		Bug fix: Creatures staying 'locked on' players outside of LOS (even while under attack)
 *		Redesign AI memory system to be simplier and more flexible (and fix above bug)
 *		The new system makes use of a memory 'heap' of previously Acquired Focus Mobs (AcquireFocusMob())
 *		Once acquired, they are added to the heap. At some future time if AcquireFocusMob() fails to 
 *		find anything, we call AcquireFocusMobFromMemory() to retrieve a Focus Mob From heap Memory.
 *		AcquireFocusMob() could probably have been extended to include the return of hidden mobs, but
 *		the fucntion is already fare too complex. the heap approach is simple and fits nicely within the system.
 *  10/31/06, Kit
 *		Fixed bug with trapped pouchs and them being stoled(cancel spell target if item doesnt exsist)
 *  08/30/06, Kit
 *		Reveal logic fix(prevent wasteful reveals)
 *  08/13/06, Kit
 *		Logic rewrite, various tweaks
 *  06/03/06, Kit
 *		Added in reveal/DH logic, added weaken blocking, added debug output to AI.
 *  12/10/05, Kit
 *		Various Tweaks, fixed cross healing, optimized dispel targeting, optimized detection of
 *		enemys that counter paralyze, now trap pouchs in wander mode, added in use of heal pots, and
 *		no longer casts poison on creatures with poison immunity.
 *  6/05/05, Kit
 *		Added refrwesh pots, various minor tweaks
 *  5/30/05, Kit
 *		Initial Creation of leaf node
 *		GolemContoller's act per normal HumanMages however crossheal
 *		any other golemcontrollers around if fighting, as well as use potions and trap pouchs
 */

using Server.Commands;
using Server.Items;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;
using System;
using System.Collections;
using CalcMoves = Server.Movement.Movement;

namespace Server.Mobiles
{
    public enum ThreatType
    {
        PureMage,
        TankMage,
        MedWarrior,
        EvalWarrior,
        PureWarrior,
        Unknown
    }

    public class BaseHybridAI : HumanMageAI
    {
        public BaseHybridAI(BaseCreature m)
            : base(m)
        {
            UsesPotions = true;
            UsesBandages = true;
        }

        public int GetStatMod(Mobile mob, StatType type)
        {
            StatMod mod = mob.GetStatMod(String.Format("[Magic] {0} Offset", type));

            if (mod == null)
                return 0;

            return (int)mod.Offset;
        }


        private ThreatType GetThreatType(Mobile m)
        {
            if (m == null)
                return ThreatType.Unknown;

            bool Magery = (m.Skills.Magery.Value >= 50);
            bool Eval = (m.Skills.EvalInt.Value >= 50);
            bool Med = (m.Skills.Meditation.Value >= 50);
            bool Weapon = (m.Skills.Swords.Value >= 50 || m.Skills.Macing.Value >= 50 || m.Skills.Fencing.Value >= 50
                || m.Skills.Archery.Value >= 50);
            bool Tactics = (m.Skills.Tactics.Value >= 50);

            bool HighInt = (m.Int >= 70);
            bool HighDex = (m.Dex >= 70);

            if (Magery && Eval && Med && !Weapon && HighInt)
                return ThreatType.PureMage;
            if (Magery && Eval && Med && Weapon && Tactics && HighInt)
                return ThreatType.TankMage;
            if (!Magery && Weapon && Tactics)
                return ThreatType.PureWarrior;
            if (Magery && Med && Weapon && Tactics && HighDex)
                return ThreatType.MedWarrior;
            if (Magery && Eval && Weapon && Tactics && HighDex)
                return ThreatType.EvalWarrior;

            return ThreatType.Unknown;

        }

        private static bool NeedGHeal(Mobile m)
        {
            return m.Hits < m.HitsMax - 30;
        }

        public Mobile FindHealTarget(bool activeOnly)
        {
            if (m_Mobile.Deleted || m_Mobile.Int < 75)
                return null;
            m_Mobile.DebugSay("Looking for cross heal target");
            Type MyType = m_Mobile.GetType();
            Type HealType;

            Map map = m_Mobile.Map;

            if (map != null && activeOnly)
            {
                double prio = 0.0;
                Mobile found = null;

                IPooledEnumerable eable = m_Mobile.GetMobilesInRange(m_Mobile.RangePerception);
                foreach (Mobile m in eable)
                {
                    HealType = m.GetType();

                    if (HealType != MyType || !m_Mobile.CanSee(m) || !(m is BaseCreature) || ((BaseCreature)m).Team != m_Mobile.Team ||
                        (m == m_Mobile) || m == m_Mobile.Combatant)
                        continue;

                    if (NeedGHeal(m))
                    {   // heal the nearest creature like me
                        double val = -m_Mobile.GetDistanceToSqrt(m);
                        if (found == null || val > prio)
                        {
                            prio = val;
                            found = m;
                        }
                    }
                }
                eable.Free();

                return found;
            }

            return null;
        }

        public override Spell DoDispel(Mobile toDispel)
        {

            Spell spell = null;

            if (spell == null)
            {
                if (!m_Mobile.DisallowAllMoves && m_Mobile.InRange(toDispel, 4) && !toDispel.Paralyzed)
                    spell = new TeleportSpell(m_Mobile, null);
                else if (Utility.Random(1) == 0 && !m_Mobile.InRange(toDispel, 3) && !toDispel.Paralyzed && !toDispel.Frozen)
                    spell = new ParalyzeSpell(m_Mobile, null);
                else
                {
                    if (toDispel is Daemon)
                        return new MassDispelSpell(m_Mobile, null);
                    else
                        return new DispelSpell(m_Mobile, null);
                }
            }

            return spell;
        }

        public override Spell ChooseSpell(Mobile c)
        {

            if (c is PlayerMobile && SmartAI && (c.Spell is MagicTrapSpell || c.Spell is MagicArrowSpell))
            {
                EnemyCountersPara = true;
            }

            if (c.Int > 70 && m_Mobile.MagicDamageAbsorb <= 0 && m_Mobile.Mana > 20 && m_Mobile.Hits > 60 && m_Mobile.CanBeginAction(typeof(DefensiveSpell)))
            {
                Spell temp = c.Spell as Spell;

                if (temp == null || (temp != null && temp.IsCasting && (int)temp.Circle <= (int)SpellCircle.Fourth))
                    return new MagicReflectSpell(m_Mobile, null);
            }

            if (c.Dex > 60 && m_Mobile.MeleeDamageAbsorb <= 0 && m_Mobile.Mana > 20 && m_Mobile.Hits > 30 && m_Mobile.CanBeginAction(typeof(DefensiveSpell)))
                return new ReactiveArmorSpell(m_Mobile, null);

            Spell spell = null;

            int healChance = (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits));

            switch (Utility.Random(1 + healChance))
            {
                default:
                case 0: // Heal ourself
                    {
                        if (UsesPotions && HealPotCount >= 1 && m_Mobile.Hits < (m_Mobile.HitsMax - 30))
                            DrinkHeal(m_Mobile);
                        else if (m_Mobile.Hits < (m_Mobile.HitsMax - 35) && m_Mobile.Hits >= 45)
                            spell = new GreaterHealSpell(m_Mobile, null);
                        else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                            spell = new HealSpell(m_Mobile, null);
                        break;
                    }

                case 1: // Set up a combo
                    {
                        //para them and med up until we have mana for a dump
                        if (m_Mobile.Mana < 85 && m_Mobile.Mana > 2)
                        {
                            RegainingMana = true;
                            //if there low on life and we have the mana try an finish them		
                            if (m_Mobile.Mana > 20 && c.Hits < 28)
                                spell = new EnergyBoltSpell(m_Mobile, null);

                            if (m_Mobile.Mana > 12 && c.Hits < 15)
                                spell = new LightningSpell(m_Mobile, null);

                            if (c.Paralyzed && !c.Poisoned)
                            {
                                if (c.Hits < 45 && m_Mobile.Mana > 40)
                                    spell = new ExplosionSpell(m_Mobile, null);

                                if (c.Hits < 30)
                                    spell = new EnergyBoltSpell(m_Mobile, null);

                                m_Mobile.DebugSay("I am going to meditate");

                                m_Mobile.UseSkill(SkillName.Meditation);
                            }
                            else if (!c.Poisoned && EnemyCountersPara == false && m_Mobile.Mana > 40)
                            {
                                spell = new ParalyzeSpell(m_Mobile, null);
                            }
                        }

                        if (m_Mobile.Mana > 85)
                        {
                            RegainingMana = false;
                            Combo = 0;

                        }

                        break;
                    }
            }

            return spell;
        }

        public override bool DoActionWander()
        {
            TrapPouch(m_Mobile);
            base.DoActionWander();
            return true;
        }

        public override bool DoActionCombat()
        {
            m_Mobile.DebugSay("doing DoActionCombat");
            Mobile c = m_Mobile.Combatant;
            m_Mobile.Warmode = true;

            // check to see if our attack priority has changed
            Mobile newTarget = null;
            if (PriorityTarget(c, out newTarget) == true)
            {
                m_Mobile.DebugSay("Higher priority target found switching targets");
                m_Mobile.Combatant = c = newTarget;
                m_Mobile.FocusMob = null;
            }

            if (m_Mobile.CanReveal)
            {
                if (c == null)
                {
                    //	If we can Recall() a player via FindHiddenTarget, make them our new Combatant.
                    //	Keep in mind, CombatTimer.OnTick() will set the Combatant to null if it sees that the mobile is hidden, 
                    //	for this reason, we make this check in DoActionGuard and again in DoActionCombat if the Combatant is null.
                    Mobile mx = FindHiddenTarget();
                    if (mx != null)
                    {
                        m_Mobile.DebugSay("C: Ah, I remembered {0}!", mx.Name);
                        c = m_Mobile.Combatant = mx;
                    }
                }

                // if we can reveal and our target just hid and we Recall them, lets try to reveal
                if (c != null && c.Hidden && ShortTermMemory.Recall(c) && c.Alive && !c.IsDeadBondedPet && m_Mobile.CanBeHarmful(c, false) && !m_Mobile.Controlled)
                {   // we will keep retrying the reveal
                    if (DoProcessReveal(c))
                        return true;
                    else
                        m_Mobile.DebugSay("Failed reveal attempt");
                }
            }

            if (c == null || c.Deleted || !c.Alive || c.IsDeadBondedPet || !m_Mobile.CanSee(c) || !m_Mobile.CanBeHarmful(c, false) || c.Map != m_Mobile.Map)
            {
                // Our combatant is deleted, dead, hidden, or we cannot hurt them
                // Try to find another combatant
                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                {
                    if (m_Mobile.Debug)
                        m_Mobile.DebugSay("Something happened to my combatant, so I am going to fight {0}", m_Mobile.FocusMob.Name);

                    m_Mobile.Combatant = c = m_Mobile.FocusMob;
                    m_Mobile.FocusMob = null;

                }
                else
                {
                    m_Mobile.DebugSay("Something happened to my combatant, and nothing is around. I am on guard.");
                    Action = ActionType.Guard;
                    return true;
                }

                if (!m_Mobile.InRange(c, m_Mobile.RangePerception))
                {
                    // They are somewhat far away, can we find something else?

                    if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                    {
                        m_Mobile.Combatant = m_Mobile.FocusMob;
                        m_Mobile.FocusMob = null;
                    }
                    else if (!m_Mobile.InRange(c, m_Mobile.RangePerception * 3))
                    {
                        m_Mobile.Combatant = null;
                    }

                    c = m_Mobile.Combatant;

                    if (c == null)
                    {
                        m_Mobile.DebugSay("My combatant has fled, so I am on guard");
                        Action = ActionType.Guard;
                        return true;
                    }
                }
            }

            if (!m_Mobile.InLOS(c))
            {
                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                {
                    m_Mobile.Combatant = c = m_Mobile.FocusMob;
                    m_Mobile.FocusMob = null;
                }
            }

            if (c != null)
            {
                int strMod = GetStatMod(c, StatType.Str);
                Mobile toDispel = null;

                //dont worry about creatures/pets useing these spells, only players.
                if (c is PlayerMobile && SmartAI && (c.Spell is MagicTrapSpell || c.Spell is MagicArrowSpell))
                {
                    EnemyCountersPara = true;
                }

                if (UsesPotions && RefreshPotCount >= 1 && m_Mobile.Stam < 20)
                {
                    DrinkRefresh(m_Mobile);
                }

                if (m_Mobile.Paralyzed)
                {
                    UseTrapPouch(m_Mobile);

                }

                TrapPouch(m_Mobile);

                if ((c.Paralyzed || c.Frozen) && PreferMagic() == true)
                {
                    if (m_Mobile.InRange(c, 3))
                        RunAround(c);
                }

                if (PreferMagic() == true)
                    if (SmartAI && !m_Mobile.StunReady && m_Mobile.Skills[SkillName.Wrestling].Value >= 80.0 && m_Mobile.Skills[SkillName.Anatomy].Value >= 80.0)
                        EventSink.InvokeStunRequest(new StunRequestEventArgs(m_Mobile));

                if (UsesBandages && (IsDamaged || IsPoisoned) && m_Mobile.Skills.Healing.Base > 20.0)
                {
                    TimeSpan ts = TimeUntilBandage;

                    if (ts == TimeSpan.MaxValue)
                        StartBandage(m_Mobile, m_Mobile);
                }

                if (IsPoisoned && UsesPotions && CurePotCount >= 1)
                    if (m_Mobile.Poison.Level >= 3 || Combo != -1 || m_Mobile.Mana < 30)
                        DrinkCure(m_Mobile);

                if (IsDamaged && UsesPotions && HealPotCount >= 1)
                    if (Utility.Random(0, 4 + (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits))) >= 3)
                        if (m_Mobile.Hits < (m_Mobile.HitsMax * .25) || m_Mobile.Mana < (m_Mobile.ManaMax * .25))
                            DrinkHeal(m_Mobile);

                if (m_Mobile.Skills[SkillName.Magery].Value >= 50.0 && m_Mobile.Spell == null && DateTime.Now >= m_Mobile.NextSpellTime)
                {
                    m_Mobile.DebugSay("Doing spell selection");
                    // We are ready to cast a spell
                    Spell spell = null;
                    toDispel = FindDispelTarget(true);

                    //woot weaken block up to lightening!
                    ISpell i = c.Spell;
                    if (i != null && i.IsCasting)
                    {
                        Spell s = (Spell)i;
                        if (m_Mobile.Hits <= 40 && (s.MaxDamage >= 12 || (s is PoisonSpell && !IsPoisoned && CurePotCount == 0)))
                        {
                            m_Mobile.DebugSay("Damage is {0}", s.MaxDamage);
                            spell = new WeakenSpell(m_Mobile, null);
                        }
                    }
                    // Top cast priority is cure - may override the previous assignment
                    else if (IsPoisoned)
                    {
                        spell = new CureSpell(m_Mobile, null);
                        int level = (m_Mobile.Poison.Level + 1);
                        if (level > 0 && (((m_Mobile.Skills[SkillName.Magery].Value / level) - 20) * 7.5) > 50)
                        {
                            spell = new CureSpell(m_Mobile, null);
                        }
                        else
                        {
                            spell = new ArchCureSpell(m_Mobile, null);
                        }
                    }
                    //were hurt they have atleast half life and were to low on mana to finish them start healing
                    else if (m_Mobile.Hits < 70 && c.Hits > 50 && m_Mobile.Mana < 30)
                    {
                        spell = new HealSpell(m_Mobile, null);
                    }
                    // Something dispellable is attacking us
                    else if (toDispel != null)
                    {
                        if (Utility.Random(0, 4 + (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits))) >= 3)
                        {
                            if (UsesPotions && HealPotCount >= 1 && m_Mobile.Hits < (m_Mobile.HitsMax - 20))
                                DrinkHeal(m_Mobile);
                            else if (m_Mobile.Hits < (m_Mobile.HitsMax - 20))
                                spell = new HealSpell(m_Mobile, null);
                        }

                        spell = DoDispel(toDispel);
                    }
                    // a friend needs healed
                    else if (CrossHeals && FindHealTarget(true) != null)
                    {
                        spell = new GreaterHealSpell(m_Mobile, null);
                    }
                    //target has reflect up hit is with ManaDrain till down
                    else if (c.MagicDamageAbsorb > 5)
                    {
                        spell = new ManaDrainSpell(m_Mobile, null);
                    }
                    // We are doing a spell combo
                    else if (Combo != -1)
                    {
                        spell = DoCombo(c);
                    }
                    //keep them weakened.
                    else if (m_Mobile.Mana >= 40 && strMod >= 0 && !c.Paralyzed)
                    {
                        spell = new WeakenSpell(m_Mobile, null);
                    }
                    else
                    {
                        spell = ChooseSpell(c);
                    }

                    if (spell != null && m_Mobile.InRange(c, 12))
                        spell.Cast();
                }


                if (SmartAI && toDispel != null)
                {
                    if (m_Mobile.InRange(toDispel, 8) && !toDispel.Paralyzed)
                        RunFrom(toDispel);
                }
                else if (HoldingWeapon() == true && PreferMagic() == false)
                {
                    m_Mobile.DebugSay("I will prefer my weapon over magic");
                    RunTo(c, CanRun);
                }
                else
                {
                    if (c is BaseCreature && ((BaseCreature)c).Controlled && !((BaseCreature)c).Summoned && !c.Paralyzed && m_Mobile.InRange(c, 6))
                    {
                        RunFrom(c);
                    }

                    if (c is BaseCreature && (((BaseCreature)c).Controlled && !((BaseCreature)c).Summoned) && !m_Mobile.InRange(c, 10))
                    {
                        RunTo(c, CanRun);
                    }

                    if (RegainingMana == false)
                    {
                        if (c is PlayerMobile || (c is BaseCreature && !((BaseCreature)c).Controlled && !((BaseCreature)c).Summoned))
                        {
                            RunTo(c, CanRun);
                        }
                    }
                    else
                    {
                        if (c is PlayerMobile || (c is BaseCreature && !((BaseCreature)c).Controlled && !((BaseCreature)c).Summoned))
                        {
                            if (m_Mobile.InRange(c, 4))
                                RunAround(c);

                            if (!m_Mobile.InRange(c, 6))
                                RunTo(c, CanRun);
                        }

                        m_Mobile.UseSkill(SkillName.Meditation);
                    }
                }

                return true;
            }
            return true;
        }

        public void RunAround(Mobile enemy)
        {
            ArrayList Directions = new ArrayList();

            int z;
            try
            {
                //make list of all non blocked adjacent tiles from our position
                for (int i = 0; i < 8; ++i)
                {
                    if (CalcMoves.CheckMovement(m_Mobile, m_Mobile.Map, m_Mobile.Location, (Direction)i, out z))
                    {
                        Directions.Add(i);
                    }
                }
                //filter list and make sure none of those tiles have enemys adjacent to them
                //if a enemy is adjacent remove it from list of tiles not blocked.
                if (Directions != null)
                {
                    for (int j = 0; j < Directions.Count; j++)
                    {
                        int TileX = m_Mobile.X;
                        int TileY = m_Mobile.Y;
                        int Dir = (int)Directions[j];
                        Direction d = (Direction)Dir;
                        //caculate are X/Y corridnate based on direction offset
                        Offset(d, ref TileX, ref TileY);

                        if (IsAdjacentToEnemy(TileX, TileY, enemy.X, enemy.Y))
                            Directions.RemoveAt(j);
                    }
                    Directions.TrimToSize();

                    if (Directions.Count > 0)
                    {
                        int Dir2 = (int)Directions[Utility.Random(Directions.Count)];
                        Run((Direction)Dir2 & Direction.Mask);
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("catch I: Send to Zen please: ");
                System.Console.WriteLine("Exception caught in HumanMageAI.RunAround: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }


        public void Offset(Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Mask)
            {
                case Direction.North: --y; break;
                case Direction.South: ++y; break;
                case Direction.West: --x; break;
                case Direction.East: ++x; break;
                case Direction.Right: ++x; --y; break;
                case Direction.Left: --x; ++y; break;
                case Direction.Down: ++x; ++y; break;
                case Direction.Up: --x; --y; break;
            }
        }

        public bool IsAdjacentToEnemy(int TileX, int TileY, int EnemyX, int EnemyY)
        {
            if (TileX == EnemyX && TileY == EnemyY)
            {
                return true;
            }

            for (int i = 0; i < 8; ++i)
            {
                int x = EnemyX;
                int y = EnemyY;

                switch ((Direction)i)
                {
                    case Direction.North: --y; break;
                    case Direction.South: ++y; break;
                    case Direction.West: --x; break;
                    case Direction.East: ++x; break;
                    case Direction.Right: ++x; --y; break;
                    case Direction.Left: --x; ++y; break;
                    case Direction.Down: ++x; ++y; break;
                    case Direction.Up: --x; --y; break;
                }
                if (x == TileX && y == TileY)
                {
                    return true;
                }

            }
            return false;
        }

        public override void ProcessTarget(Target targ)
        {
            bool isDispel = (targ is DispelSpell.InternalTarget);
            bool isParalyze = (targ is ParalyzeSpell.InternalTarget);
            bool isTeleport = (targ is TeleportSpell.InternalTarget);
            bool isCrossHeal = (targ is GreaterHealSpell.InternalTarget);
            bool isTrap = (targ is MagicTrapSpell.InternalTarget);
            bool teleportAway = false;
            bool isReveal = (targ is RevealSpell.InternalTarget || targ is DetectHidden.InternalTarget);

            Mobile toTarget = null;
            Mobile toHeal = null;

            if (isReveal)
            {
                targ.Invoke(m_Mobile, m_Mobile);
            }

            if (isTrap)
            {
                Pouch p = FindPouch(m_Mobile);
                if (p != null)
                {
                    targ.Invoke(m_Mobile, p);
                }
                else
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }

            if (isDispel)
            {
                toTarget = FindDispelTarget(false);

                if (toTarget != null && m_Mobile.InRange(toTarget, 10) && !toTarget.Paralyzed)
                    RunFrom(toTarget);
            }

            if (CrossHeals && isCrossHeal)
            {
                toHeal = FindHealTarget(true);

                if (toHeal != null && !m_Mobile.InRange(toHeal, 8))
                {

                    RunTo(toHeal, CanRun);
                }
            }

            else if (isParalyze || isTeleport)
            {
                toTarget = FindDispelTarget(true);

                if (toTarget == null)
                {
                    toTarget = m_Mobile.Combatant;

                    if (toTarget != null && !m_Mobile.InRange(toTarget, 8))
                        RunTo(toTarget, CanRun);
                }
                else if (m_Mobile.InRange(toTarget, 10))
                {
                    RunFrom(toTarget);
                    teleportAway = true;
                }
                else
                {
                    teleportAway = true;
                }
            }

            else
            {
                if (m_Mobile.ControlOrder == OrderType.Come && isTeleport)
                {
                    toTarget = m_Mobile.ControlMaster;
                }
                else
                {
                    toTarget = m_Mobile.Combatant;
                }

                if (toTarget != null)
                {

                    RunTo(toTarget, CanRun);
                }
            }

            if ((targ.Flags & TargetFlags.Harmful) != 0 && toTarget != null)
            {
                if ((m_Mobile.InRange(toTarget, 10)) && m_Mobile.CanSee(toTarget) && m_Mobile.InLOS(toTarget))
                {
                    targ.Invoke(m_Mobile, toTarget);
                }
                else if (isDispel)
                {
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
                }

            }

            else if ((targ.Flags & TargetFlags.Beneficial) != 0 && toHeal != null)
            {
                if ((m_Mobile.InRange(toHeal, 10)) && m_Mobile.CanSee(toHeal) && m_Mobile.InLOS(toHeal) && NeedGHeal(toHeal))
                {

                    targ.Invoke(m_Mobile, toHeal);
                }
                else
                {
                    Targeting.Target.Cancel(m_Mobile);
                }
            }

            else if ((targ.Flags & TargetFlags.Beneficial) != 0)
            {
                if (!isCrossHeal)
                {
                    targ.Invoke(m_Mobile, m_Mobile);
                }
                if ((isCrossHeal) && (NeedGHeal(m_Mobile)))
                {
                    targ.Invoke(m_Mobile, m_Mobile);
                }
                else
                {
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
                }

            }
            else if (isTeleport && toTarget != null)
            {
                Map map = m_Mobile.Map;

                if (map == null)
                {
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
                    return;
                }

                int px, py;

                if (teleportAway)
                {
                    int rx = m_Mobile.X - toTarget.X;
                    int ry = m_Mobile.Y - toTarget.Y;

                    double d = m_Mobile.GetDistanceToSqrt(toTarget);

                    px = toTarget.X + (int)(rx * (10 / d));
                    py = toTarget.Y + (int)(ry * (10 / d));
                }
                else
                {
                    px = toTarget.X;
                    py = toTarget.Y;
                }

                for (int i = 0; i < m_Offsets.Length; i += 2)
                {
                    int x = m_Offsets[i], y = m_Offsets[i + 1];

                    Point3D p = new Point3D(px + x, py + y, 0);

                    LandTarget lt = new LandTarget(p, map);

                    if ((targ.Range == -1 || m_Mobile.InRange(p, targ.Range)) && m_Mobile.InLOS(lt) && map.CanSpawnMobile(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    {
                        targ.Invoke(m_Mobile, lt);
                        return;
                    }
                }

                int teleRange = targ.Range;

                if (teleRange < 0)
                    teleRange = 12;

                for (int i = 0; i < 10; ++i)
                {
                    Point3D randomPoint = new Point3D(m_Mobile.X - teleRange + Utility.Random(teleRange * 2 + 1), m_Mobile.Y - teleRange + Utility.Random(teleRange * 2 + 1), 0);

                    LandTarget lt = new LandTarget(randomPoint, map);

                    if (m_Mobile.InLOS(lt) && map.CanSpawnMobile(lt.X, lt.Y, lt.Z) && !SpellHelper.CheckMulti(randomPoint, map))
                    {
                        targ.Invoke(m_Mobile, new LandTarget(randomPoint, map));
                        return;
                    }
                }

                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }
            else
            {
                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }
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
}
