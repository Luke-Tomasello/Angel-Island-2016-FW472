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

/* Items/Containers/DungeonTreasureChest.cs
 * ChangeLog:
 *	7/14/10, Adam
 *		o revert multi-piles of gold until we can balance the lift-risk
 *		o soften the pirates magic ability (based on level) so that he's got less of a chance at casting reveal
 *		o soften the pirates detect hidden skill (based on level)
 *	7/11/10, Adam
 *		o swap out local copy of 'lift memory' and replace it with the shared version in Utilities
 *		o Split gold into level*2 piles to increase the chance of getting revealed
 *	5/23/10, Adam
 *		In CheckLift(), thwart lift macros by checking the per-player 'lift memory'
 *	3/5/09, Adam
 *		MonsterStatuette drops cut in 1/2
 *		skin cream drops cut by 3/4
 *	11/12/08, Adam
 *		- Set the Detect Hidden of the chest Guardian to match the players hiding so that they have a fighting chance
 *		- Thwart �fast lifting� in CheckLift: �You thrust your hand into the chest but come up empty handed.�
 *		- Remove IOB checks from the CheckThief() and CheckGuardian() functions
 *		  - CheckThief() now checks the IOB alignment in the calling logic
 *		  - CheckGuardian() does not care if you are aligned or not
 *  11/11/08. Adam
 *      - Fix a bug in CheckGuardian() that was preventing any chance of being caught
 *      - turn on Reveal and Run for the Guardian (Uses memory)
 *      - Switch fight mode to Aggressor mode for Guardians
 *	11/10/08, Adam
 *		Reduce the drop level of level 5 chests about 50%
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *	12/8/07, Pix
 *		Moved check up in PackMagicItem() so we don't create the item if we don't need it
 *			(and thus it's not left on the internal map)
 *  2/28/07, Adam
 *      Allow either explosion OR poison for the trap
 *  2/1/07, Adam
 *      - repair reveal logic for OnItemLifted to be CheckGuardian% OR normal reveal%, not both.
 *      - Reduce the chance for the guardian to catch you by a small amount based on the number of monsters around you.
 *  1/30/07, Adam
 *      Add the new SkinHueCreme for level 4&5 chests
 *  1/28/07, Adam
 *      Have the Guardian recall away (decay) when the chest decays
 *  1/26/07, Adam
 *      - new dynamic property system
 *      - Add new Dynamic Properties for identification and speach text
 *	1/25/07, Adam
 *      Set the new BaseCreature.LifespanMinutes to change the lifespan and refresh scope
 *	1/25/07, Adam
 *		- add a new short lived, non-aligned pirate to guard this pirate booty
 *		- 30% of the time, for each item lifted, you will incur the wrath of the guardian
 *      - the guardian's stats is proportioned to the chest level
 *	1/9/06, Adam
 *		It's no longer required that an aligned player be wearing their IOB to get caught stealing from their kin.
 *		Detail: Removed the requirement for pm.IOBEquipped in CheckThief()
 *	4/24/05, Adam
 *		I adjusted the low-end of the gold generation to greater than high-end of the previous chest level.
 *		(level 5 gold will always be greater than level 4.)
 *		MIN is now 75% of max
 *	4/17/04, Adam
 *		Cleanup monster statue drop
 *	03/28/04, Kitaras
 *		Added Check to CheckThief to prevent controled pets with iob alignment
 *		from setting off "you have been noticed stealing from you kin"
 *	12/05/04, Adam
 *		Crank down chest loot MIN so as to decrease daily take home
 *  12/05/04, Jade
 *      Changed chance to drop a t-map to 1%
 *	11/20/04, Adam
 *		1. add level 0 (trainer chests)
 *		2. Add CheckThief() method to OnItemLifted() to see if you are stealing from your kin!
 *	11/10/04, Adam
 *		change treasure map chest loot to (level * 1000) / 3 MAX
 *	10/14/04, Adam
 *		Increase difficulty:
 *		change TrapSensitivity = 1.0 to TrapSensitivity = 1.5
 *			This will for example make a 10% chance to set off the trap 15% given:
 *			100 trap power and 20 disarm skill
 *		we want to reveal the looter about level * 3% of the time (per item looted)
 *		for chest levels 1-5, this works out to: 3%, 6%, 9%, 12%, 15%
 *  8/9/04, Pixie
 *		Changed the damage done when the trap is tripped on a disarm failure.
 *  8/8/04, Pixie
 *		Added functionality for tripping the trap if you fail to disarm it.
 *	8/3/04, Adam
 *		we want to reveal the looter about level * 2.5% of the time (per item looted)
 *		for chest levels 1-5, this works out to: 2.5%, 5%, 7.5%, 10%, 12.5%
 *	7/11/05, Adam
 *		1. Decrease tmap drops from 20% to 5%
 *		2. Decrease statue drops from 5%, 6%, and 7% ==> 3%, 4%, and 5%
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/3/04, Adam - Magic Item Drop (MID)
 *		1. Set MID to level number * 70% chance to drop.
 *		2. Scale MID Intensity based on chest level.
 *		3. Bonus drop: level number * 7% chance to drop
 *			(level 3 intensity item.)
 *	7/3/04, Adam
 *		1. Change monster statue drop from 20% to max 7%
 *		level 3 = 5%, level 4 = 6%, level 5 = 7%
 *	6/29/04, Adam
 *		1. Changed to drop scrolls appropriate for the level.
 *		Added PackScroll procedures
 *		2. give a 2.5% chance to reveal the looter (per item removed)
 *		3. Add the "You have been revealed!" message
 *		4. Only show the message if the looter is hidden
 *	6/27/04, Adam
 *		Massive cleanup: remove weapons and armor, Add tmaps, monster statues
 *			and magic jewelry, and magic clothing, ...
 *	6/25/04, Adam
 *		Copy from TreasureMapChest and update to be correct levels for dungeons
 *		(should be a subclass)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/19/04, pixie
 *		Modifies so the trap resets when it is tripped.
 *		Now the only way to access the items inside is by removing the
 *		trap with the Remove Trap skill.
 *	4/30/04, mith
 *		modified the chances for getting high end weapons/armor based on treasure chest level.
 *   4/27/2004, pixie
 *     Changed so telekinesis doesn't trip the trap
 */

using Server.Commands;
using Server.Engines.PartySystem;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fourth;
using System;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute(0xE41, 0xE40)]
    public class DungeonTreasureChest : LockableContainer
    {
        // 23 unique statues 
        static MonsterStatuetteType[] level3 = new MonsterStatuetteType[]
        {
            MonsterStatuetteType.Crocodile,
            MonsterStatuetteType.Daemon,
            MonsterStatuetteType.Dragon,
            MonsterStatuetteType.EarthElemental,
            MonsterStatuetteType.Ettin,
            MonsterStatuetteType.Gargoyle,
            MonsterStatuetteType.Gorilla
        };

        static MonsterStatuetteType[] level4 = new MonsterStatuetteType[]
        {
            MonsterStatuetteType.Lich,
            MonsterStatuetteType.Lizardman,
            MonsterStatuetteType.Ogre,
            MonsterStatuetteType.Orc,
            MonsterStatuetteType.Ratman,
            MonsterStatuetteType.Skeleton,
            MonsterStatuetteType.Troll
        };

        static MonsterStatuetteType[] level5 = new MonsterStatuetteType[]
        {
            MonsterStatuetteType.Cow,
            MonsterStatuetteType.Zombie,
            MonsterStatuetteType.Llama,
            MonsterStatuetteType.Ophidian,
            MonsterStatuetteType.Reaper,
            MonsterStatuetteType.Mongbat,
            MonsterStatuetteType.Gazer,
            MonsterStatuetteType.FireElemental,
            MonsterStatuetteType.Wolf
        };

        static MonsterStatuetteType[][] m_monsters = new MonsterStatuetteType[][]
            {
                level3, level4, level5
            };

        //TrapSensitivity modifies the chance to trip the trap
        // when someone fails to disarm it.
        private double TrapSensitivity = 1.5;
        private int m_Level;
        private DateTime m_DeleteTime;
        private Timer m_Timer;
        private Mobile m_Owner;
        private Mobile m_Guardian;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Guardian { get { return m_Guardian; } set { m_Guardian = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DeleteTime { get { return m_DeleteTime; } }

        [Constructable]
        public DungeonTreasureChest(int level)
            : this(null, level)
        {
        }

        public DungeonTreasureChest(Mobile owner, int level)
            : base(0xE41)
        {
            m_Owner = owner;
            m_Level = level;

            // adam: usual decay time is 3 hours
            //	See also: ExecuteTrap() where decay starts
            m_DeleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(3.0 * 60.0);

            m_Timer = new DeleteTimer(this, m_DeleteTime);
            m_Timer.Start();

            Fill(this, level);
        }

        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        public static void Fill(LockableContainer cont, int level)
        {
            cont.Movable = false;
            cont.TrapType = Utility.RandomBool() ? TrapType.PoisonTrap : TrapType.ExplosionTrap;
            cont.TrapPower = level * 25;
            cont.TrapLevel = level;
            cont.Locked = true;

            switch (level)
            {
                // Adam: add level 0 (trainer chests)
                case 0: cont.RequiredSkill = Utility.RandomMinMax(30, 37); break;
                case 1: cont.RequiredSkill = 36; break;
                case 2: cont.RequiredSkill = 76; break;
                case 3: cont.RequiredSkill = 84; break;
                case 4: cont.RequiredSkill = 92; break;
                case 5: cont.RequiredSkill = 100; break;
            }

            cont.LockLevel = cont.RequiredSkill - 10;
            cont.MaxLockLevel = cont.RequiredSkill + 40;

            // adam: change treasure map chest loot MIN-MAX so as to decrease daily take home
            if (level != 0)
            {
                int amount = Utility.RandomMinMax(
                        (int)(((double)((level * 1000) / 3)) * .75), // min is 75% of MAX
                        (level * 1000) / 3);

                //int piles = level * 2;	// cool way but the players whined
                int piles = 1;              // sissy way

                // make several piles
                for (int ix = 0; ix < piles; ix++)
                    cont.DropItem(new Gold(Utility.RandomMinMax(amount / piles - 10, amount / piles + 10)));
            }


            // skin tone creme for level 4 & 5 chests
            if (Utility.RandomDouble() < 0.05 && level > 3)
            {
                cont.DropItem(new SkinHueCreme());
            }

            // adam: scrolls * 3 and not 5
            for (int i = 0; i < level * 3; ++i)
            {
                int minCircle = level;
                int maxCircle = (level + 3);
                PackScroll(cont, minCircle, maxCircle);
            }

            // plus "level chances" for magic jewelry & clothing
            switch (level)
            {
                case 0: // Adam: trainer chest
                case 1: // none
                    break;
                case 2:
                    PackMagicItem(cont, 1, 1, 0.05);
                    break;
                case 3:
                    PackMagicItem(cont, 1, 2, 0.10);
                    PackMagicItem(cont, 1, 2, 0.05);
                    break;
                case 4:
                    PackMagicItem(cont, 2, 3, 0.10);
                    PackMagicItem(cont, 2, 3, 0.05);
                    PackMagicItem(cont, 2, 3, 0.02);
                    break;
                case 5:
                    PackMagicItem(cont, 3, 3, 0.10);
                    PackMagicItem(cont, 3, 3, 0.05);
                    PackMagicItem(cont, 3, 3, 0.02);
                    break;
            }

            // TreasureMap( int level, Map map
            //	5% chance to get a treasure map
            //  Changed chance for tmap to 1%
            if (level != 0)
                if (Utility.RandomDouble() < 0.01)
                {
                    int mlevel = level;

                    //	20% chance to get a treasure map one level better than the level of this chest
                    if (Utility.RandomDouble() < 0.20)
                        mlevel += (level < 5) ? 1 : 0;  // bump up the map level by one

                    TreasureMap map = new TreasureMap(mlevel, Map.Felucca);
                    cont.DropItem(map);             // drop it baby!
                }

            // if You're doing a level 3, 4, or 5 chest you have a 1.5%, 2%, or 2.5% chance to get a monster statue
            double chance = 0.00 + (((double)level) * 0.005);
            if ((level > 3) && (Utility.RandomDouble() < chance))
            {
                int ndx = level - 3;
                MonsterStatuette mx =
                    new MonsterStatuette(m_monsters[ndx][Utility.Random(m_monsters[ndx].Length)]);
                mx.LootType = LootType.Regular;                 // not blessed
                cont.DropItem(mx);                          // drop it baby!
            }

            TreasureMapChest.PackRegs(cont, level * 10);
            TreasureMapChest.PackGems(cont, level * 5);

        }

        public static void PackScroll(LockableContainer cont, int minCircle, int maxCircle)
        {
            PackScroll(cont, Utility.RandomMinMax(minCircle, maxCircle));
        }

        public static void PackScroll(LockableContainer cont, int circle)
        {
            int min = (circle - 1) * 8;

            cont.DropItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

        public static void PackMagicItem(LockableContainer cont, int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            Item item = Loot.RandomClothingOrJewelry();

            if (item == null)
                return;

            if (item is BaseClothing)
                ((BaseClothing)item).SetRandomMagicEffect(minLevel, maxLevel);
            else if (item is BaseJewel)
                ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            cont.DropItem(item);
        }

        private ArrayList m_Lifted = new ArrayList();

        private bool CheckLoot(Mobile m, bool criminalAction)
        {
            if (m_Owner == null || m == m_Owner)
                return true;

            Party p = Party.Get(m_Owner);

            if (p != null && p.Contains(m))
                return true;

            Map map = this.Map;

            if (map != null && (map.Rules & MapRules.HarmfulRestrictions) == 0)
            {
                if (criminalAction)
                    m.CriminalAction(true);
                else
                    m.SendLocalizedMessage(1010630); // Taking someone else's treasure is a criminal offense!

                return true;
            }

            m.SendLocalizedMessage(1010631); // You did not discover this chest!
            return false;
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            return CheckLoot(from, item != this) && base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {   // Thwart lift macros
            if (LiftMemory.Recall(from))
            {   // throttle
                from.SendMessage("You thrust your hand into the chest but come up empty handed.");
                reject = LRReason.Inspecific;
                return false;
            }
            else
                LiftMemory.Remember(from, 1.8);

            return CheckLoot(from, true) && base.CheckLift(from, item, ref reject);
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            bool notYetLifted = !m_Lifted.Contains(item);
            BaseCreature witness = null;

            if (notYetLifted)
            {
                // Adam: Insure IOB wearers are not stealing from their kin
                if (Engines.IOBSystem.IOBSystem.IsIOBAligned(from) && CheckThief(from, out witness))
                {
                    from.SendMessage("You have been discovered stealing from your kin!");
                    if (from.Hidden)
                        from.RevealingAction();

                    // attack kin to make them come after you
                    from.DoHarmful(witness);
                }
                else if (Utility.RandomDouble() < 0.30 && CheckGuardian(from, out witness))
                {
                    from.SendMessage("You have been discovered stealing pirate booty!");
                    if (from.Hidden)
                        from.RevealingAction();

                    // attack kin to make them come after you
                    from.DoHarmful(witness);
                }
                // adam: we want to reveal the looter about level * 3% of the time (per item looted)
                // for chest levels 1-5, this works out to: 3%, 6%, 9%, 12%, 15%
                else if ((from.Hidden) && (Utility.RandomDouble() < (0.025 * m_Level)))
                {
                    from.SendMessage("You have been revealed!");
                    from.RevealingAction();
                }
            }

            base.OnItemLifted(from, item);
        }

        public bool CheckThief(Mobile from, out BaseCreature witness)
        {
            witness = null;

            if (from == null || !(from is PlayerMobile))
                return false;

            PlayerMobile pm = (PlayerMobile)from;

            IPooledEnumerable eable = pm.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m == null || !(m is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)m;
                if (bc.Controlled == true)
                    continue;

                if (pm.IOBAlignment == bc.IOBAlignment)
                {
                    witness = bc;
                    eable.Free();
                    return true;
                }
            }
            eable.Free();

            return false;
        }

        /*
		 * Check to see if there is a guardian nearby, and if so, provide a good chance
		 * that he will catch you stealing booty. The chance to 'catch you' is based on the number
		 * of monsters near you. Each nearby monster will decrease the chance to catch you by 10%.
		 * This is not much as you start at 100% and it would take 3+ monsters to even give you a small chance at 
		 * slipping past the guardian 'on this lift' <-- Remember, it's a per lift check
		 */
        private bool CheckGuardian(Mobile from, out BaseCreature witness)
        {
            witness = null;

            if (from == null || !(from is PlayerMobile))
                return false;

            PlayerMobile pm = (PlayerMobile)from;

            // start with a 100% chance to get attacked by pirate
            double chance = 1.0;
            IPooledEnumerable eable = pm.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m == null || !(m is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)m;
                if (bc.Controlled == true || bc.Summoned == true)
                    continue;

                if (from.CanSee(bc) == false)
                    continue;

                // if it carries the Guardian Use property, it's a Guardian
                if (Property.FindUse(bc, Use.IsGuardian))
                    witness = bc;
                else
                    // reduce chance by 10% for each nearby mobile that is not a guardian
                    chance *= .9;
            }
            eable.Free();

            // see if user gets a pass
            if (Utility.RandomDouble() > chance)
                witness = null;

            return witness == null ? false : true;
        }

        private static object[] m_Arguments = new object[1];

        private static void AddItems(Container cont, int[] amounts, Type[] types)
        {
            for (int i = 0; i < amounts.Length && i < types.Length; ++i)
            {
                if (amounts[i] > 0)
                {
                    try
                    {
                        m_Arguments[0] = amounts[i];
                        Item item = (Item)Activator.CreateInstance(types[i], m_Arguments);

                        cont.DropItem(item);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        public DungeonTreasureChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 3
            writer.Write(m_Guardian);

            // version 1
            writer.Write(m_Owner);

            // version 2
            writer.Write((int)m_Level);
            writer.WriteDeltaTime(m_DeleteTime);
            writer.WriteItemList(m_Lifted, true);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Guardian = reader.ReadMobile();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Owner = reader.ReadMobile();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Level = reader.ReadInt();
                        m_DeleteTime = reader.ReadDeltaTime();
                        m_Lifted = reader.ReadItemList();

                        m_Timer = new DeleteTimer(this, m_DeleteTime);
                        m_Timer.Start();

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;

            base.OnAfterDelete();
        }

        private class DeleteTimer : Timer
        {
            private Item m_Item;

            public DeleteTimer(Item item, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_Item = item;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                DeleteGuardian(m_Item as DungeonTreasureChest);
                m_Item.Delete();
            }
        }

        public static void DeleteGuardian(DungeonTreasureChest tc)
        {
            if (tc == null || (tc as DungeonTreasureChest).Guardian == null || (tc as DungeonTreasureChest).Guardian.Alive == false || (tc as DungeonTreasureChest).Guardian.Deleted == true)
                return;

            if ((tc as DungeonTreasureChest).Guardian as BaseCreature == null)
                return;

            // say something!
            switch (Utility.Random(4))
            {
                case 0:
                    (tc as DungeonTreasureChest).Guardian.Say("Thar be nothing left for me here.");
                    break;
                case 1:
                    (tc as DungeonTreasureChest).Guardian.Say("I done me best!");
                    break;
                case 2:
                    (tc as DungeonTreasureChest).Guardian.Say("Arr, me work be done here.");
                    break;
                case 3:
                    (tc as DungeonTreasureChest).Guardian.Say("Arr, I got to get back to me ale!");
                    break;
            }

            // Frozen while casting
            (tc as DungeonTreasureChest).Guardian.CantWalk = true;

            // fake recall
            new NpcRecallSpell((tc as DungeonTreasureChest).Guardian, null, new Point3D(0, 0, 0)).Cast();

            // delete him
            DateTime DeleteTime = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
            new DeleteGuardianTimer((tc as DungeonTreasureChest).Guardian, DeleteTime).Start();
        }

        private class DeleteGuardianTimer : Timer
        {
            private Mobile m_mob;

            public DeleteGuardianTimer(Mobile m, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_mob = m;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                if (m_mob == null || m_mob.Deleted == true || m_mob.Alive == false)
                    return;

                m_mob.Delete();
            }
        }

        public override void OnTelekinesis(Mobile from)
        {
            //Do nothing, telekinesis doesn't work on a TMap.
        }

        public override bool OnFailDisarm(Mobile from)
        {
            bool bExploded = false;

            double rtskill = from.Skills[SkillName.RemoveTrap].Value;

            double chance = (TrapPower - rtskill) / 800;

            //make sure there's some chance to trip
            if (chance <= 0) chance = .005; //minimum of 1/200 trip
            if (chance >= 1) chance = .995;
            chance *= TrapSensitivity;

            //debug message only available to non-Player level
            if (from.AccessLevel > AccessLevel.Player)
            {
                from.SendMessage("Chance to trip trap: " + chance);
            }

            if (Utility.RandomDouble() < chance)
            { //trap is tripped, effect disarmer
                ExecuteTrap(from);
                from.RevealingAction();
                bExploded = true;
            }

            return bExploded;
        }

        public override bool ExecuteTrap(Mobile from)
        {
            //In order to REQUIRE the remove trap skill for
            //Treasure Map Chests, make sure that the trap resets immediately
            //after the trap is tripped.
            TrapType originaltrap = TrapType;

            bool bReturn = base.ExecuteTrap(from);

            //reset trap!
            TrapType = originaltrap;

            // adam: reset decay timer when the trap is messed with
            if (m_Timer != null)
            {
                m_Timer.Stop();

                m_Timer = null;

                // adam: once the trap has been tripped, it decays in 15 minutes
                m_DeleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(1 * 15.0);
                from.SendMessage("The chest begins to decay.");

                m_Timer = new DeleteTimer(this, m_DeleteTime);
                m_Timer.Start();
            }

            return bReturn;
        }

        public override void LockPick(Mobile from)
        {
            base.LockPick(from);

            if (m_Level >= 3)
                m_Guardian = SpawnGuardian("Pirate", m_Level, from.Skills[SkillName.Hiding].Value);

            if (m_Guardian != null)
                m_Guardian.AggressiveAction(from);
        }

        public Mobile SpawnGuardian(string name, int level, double PlayersHidingSkill)
        {
            Type type = ScriptCompiler.FindTypeByName(name);
            BaseCreature c = null;

            if (type != null)
            {
                try
                {
                    object o = Activator.CreateInstance(type);

                    if (o is BaseCreature)
                    {
                        c = o as BaseCreature;

                        // decay time of a chest once it's opened
                        c.Lifespan = TimeSpan.FromMinutes(15);

                        // reset the alignment
                        c.IOBAlignment = IOBAlignment.None;

                        // Can chase you and can reveal you if you be hiding!
                        c.CanRun = true;
                        c.CanReveal = true;

                        // stats based on chest level
                        double factor = 1.0;
                        if (level == 3)
                            factor = .3;
                        if (level == 4)
                            factor = .5;
                        if (level == 5)
                            factor = 1.0;

                        c.SetMana((int)(c.ManaMax * factor));
                        c.SetStr((int)(c.RawStr * factor));
                        c.SetDex((int)(c.RawDex * factor));
                        c.SetInt((int)(c.RawInt * factor));
                        c.SetHits((int)(((c.HitsMax / 100.0) * 60.0) * factor));

                        // these guys can reveal - set the Detect Hidden to match the players hiding so that they have a fighting chance
                        c.SetSkill(SkillName.DetectHidden, PlayersHidingSkill * factor);

                        // nerf their magery so that they 
                        // Sixth	20	52.1	100
                        c.SetSkill(SkillName.Magery, 52.1 * factor);

                        // only attack aggressors
                        c.FightMode = FightMode.Aggressor;

                        // maybe 6 tiles? Keep him near by
                        c.RangeHome = 6;

                        // the chest is the home of the guardian
                        c.Home = this.Location;

                        // we are not bardable
                        c.BardImmune = true;

                        // make them a guardian
                        c.AddItem(new Property(Use.IsGuardian, null));

                        // give them shite speak if they are calmed
                        c.AddItem(new Quip("Arr, but that be a pretty tune .. can you play me another?"));
                        c.AddItem(new Quip("Thar be no time for singing and dancin' now matey."));
                        c.AddItem(new Quip("That be a downright lovely tune ye be playing thar."));
                        c.AddItem(new Quip("Har! Me thinks a cutlass would be a better choice!"));

                        // show them
                        Point3D loc = (GetSpawnPosition(c.RangeHome));
                        c.MoveToWorld(loc, this.Map);

                        // teleport
                        Effects.SendLocationParticles(EffectItem.Create(c.Location, c.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
                        Effects.PlaySound(c.Location, c.Map, 0x1FE);

                        Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(ShiteTalk_Callback), c);

                    }

                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Exception caught in Spawner.Refresh: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }

            return c as Mobile;
        }

        public virtual void ShiteTalk_Callback(object state)
        {
            Mobile guardian = state as Mobile;

            if (guardian == null || guardian.Alive == false || guardian.Deleted == true)
                return;

            // shite talking
            switch (Utility.Random(3))
            {
                case 0: guardian.Say("Arr. Ye best be steppin' away from that thar chest matey."); break;
                case 1: guardian.Say("Avast Ye, Scallywag! I be watching over that thar booty."); break;
                case 2: guardian.Say("I know ye be 'round here somewhere!"); break;
            }
        }

        private Point3D GetSpawnPosition(int HomeRange)
        {
            Map map = Map;

            if (map == null)
                return Location;

            // Try 10 times to find a Spawnable location.
            for (int i = 0; i < 10; i++)
            {
                int x = Location.X + (Utility.Random((HomeRange * 2) + 1) - HomeRange);
                int y = Location.Y + (Utility.Random((HomeRange * 2) + 1) - HomeRange);
                int z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnMobile(new Point2D(x, y), this.Z))
                    return new Point3D(x, y, this.Z);
                else if (Map.CanSpawnMobile(new Point2D(x, y), z))
                    return new Point3D(x, y, z);
            }

            return this.Location;
        }
    }
}
