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

/* Scripts/Mobiles/Monsters/Reptile/Magic/Dragon.cs
 * ChangeLog
 *	5/12/10, adam
 *		update AttackOrderHack to account for ScaredOfScaryThings
 *	4/10/10, adam
 *		Add speed management MCi to tune dragon speeds.
 *	9/1/07, Adam
 *		move helper code to the end of the file
 *		remove the 'force to dragonai' from deserialize.
 *			(because we should be able to dynamically change it. See Paragons.)
 *	7/3/07, Adam
 *		- remove OnControlMasterChanged() as it was continually resetting the hatch date everytime someone put
 *			their pet in the stables or took them out.
 *		- add a BreedingParticipant flag
 *		- deprecate using m_hatchDate as a flag, prefer BreedingParticipant flag.
 *		-Add BreedingParticipant(ion) checks for both males and females when the mood turns to procreation
 *  07/01/07 Taran Kain
 *      Fixed phantom stabled-deleted log warning.
 *  06/25/07 Taran Kain
 *      Added TC timers to play nice with prod values.
 *  06/24/07 Taran Kain
 *      Removed TC time/chance values.
 *      Fixed chicken sounds, changed bodies to depend on gender
 *  6/19/07, Adam
 *      - Added a TestCenter override for the 'in fire dungeon' check.
 *      on TC drags can breed anywhere (don't want testers getting PKed)
 *      - merged versions 3&4 as there was a versioning error.
 *	06/18/07 Taran Kain
 *		BREEDING!!!
 *	03/28/07 Taran Kain
 *		Added names to genes
 *  1/08/07 Taran Kain
 *      Updated old dragons to new #'s, add in some safety measures
 *  1/08/07 Taran Kain
 *      Changed *Max values
 *      Removed RawStr, RawInt, RawDex from genetics, now set (semi-)normally
 *      Fixed tiny bug with HitsMaxDiff not updating Hits correctly
 *      Added in dragon-specific SkillCheck logic for custom stat cap
 *  11/28/06 Taran Kain
 *      Changed HitsMaxDiff to be more in-line with previous values.
 *	11/20/06 Taran Kain
 *		Added genetics, allowed breeding logic, overrode several properties.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/2/04
 *		Change chance to drop a magic item to 30% 
 *		add a 5% chance for a bonus drop at next intensity level
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Commands;
using Server.Items;
using Server.Misc;
using Server.Multis;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a dragon corpse")]
    public class Dragon : BaseCreature
    {
        private DateTime m_Hatchdate = DateTime.MinValue;

        [CommandProperty(AccessLevel.Counselor)]
        public DateTime Hatchdate
        {
            get
            {
                return m_Hatchdate;
            }
        }

        // force an opt-out of the breeding program. 
        //	** Currently called by [nuke to patch dragons.
        //	Can be removed later
        public void BreedingRedaction()
        {
            BreedingParticipant = false;
        }

        // should be moved into base creature if/when we enable breeding across the board.
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool BreedingParticipant
        {
            get
            {
                return GetFlag(CreatureFlags.BreedingParticipant);
            }

            set
            {
                SetFlag(CreatureFlags.BreedingParticipant, value);
                if (value == false)
                    m_Hatchdate = DateTime.MinValue;
                else
                    m_Hatchdate = DateTime.Now;
            }
        }

        public enum DragonMaturity
        {
            Egg = 0,
            Infant,
            Child,
            Youth,
            Adult,
            Ancient
        }

        private DragonMaturity m_Maturity;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DragonMaturity Maturity
        {
            get
            {
                return m_Maturity;
            }
            set
            {
                m_Maturity = value;
            }
        }

        public override void OnThink()
        {
            if ((Controlled || BreedingParticipant == true) &&          // wild, non-bred dragons don't age
                Maturity != DragonMaturity.Egg &&
                m_CheckedBody + TimeSpan.FromMinutes(30.0) < DateTime.Now)
            {
                CheckGrow();
            }

            base.OnThink();
        }

        public void OnHatch()
        {
            // enroll in breeding system, sets m_HatchDate
            BreedingParticipant = true;

            if (Backpack == null)
            {
                Backpack pack = new Backpack();
                pack.Movable = false;
                AddItem(pack);
            }
            Backpack.Items.Clear();

            SetSkill(SkillName.EvalInt, 10.1, 20.0);
            SetSkill(SkillName.Magery, 15.1, 25.0);
            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 20.1, 30.0);
            SetSkill(SkillName.Wrestling, 25.1, 40.0);

            // must manually call CheckGrow because OnThink will not call when we're in Egg stage
            // don't simply bypass and set to Child because many other things are set alongside
            CheckGrow();
        }

        private DateTime m_CheckedBody = DateTime.MinValue;
        private DateTime m_LastGrowth = DateTime.MinValue;
        private double m_GainFactor;

        public void CheckGrow()
        {
            // sanity check
            if (BreedingParticipant == false)
                return;

            m_CheckedBody = DateTime.Now;

            if (Maturity == DragonMaturity.Ancient)
                return; // no work to do

            double weeks = (DateTime.Now - Hatchdate).TotalDays / 7;
            if (TestCenter.Enabled)
                weeks = (DateTime.Now - Hatchdate).TotalHours / 7;
            double stats = ((double)RawStr / StrMax + (double)RawInt / IntMax + (double)RawDex / DexMax) / StatCapFactor;

            // initialize with safe values
            DragonMaturity mat = DragonMaturity.Adult;
            int body = 0x3B;
            double gainfactor = 1.0;
            double statmult = 1.0; // THIS PERMANENTLY AFFECTS STATS - IF IN DOUBT, LEAVE ALONE
            int hue = 0;

            if (m_Maturity == DragonMaturity.Egg && m_Hatchdate <= DateTime.Now)
            {
                body = 0xCE;
                mat = DragonMaturity.Infant;
                gainfactor = 1.0;
                statmult = 0.4;
                hue = Female ? 1053 : 1138;
            }
            else if (m_Maturity == DragonMaturity.Infant && weeks * stats >= 1)
            {
                body = 0x31F;
                mat = DragonMaturity.Child;
                gainfactor = 1.5;
                statmult = 1.0;
                hue = Female ? 1053 : 1138;
            }
            else if (m_Maturity == DragonMaturity.Child && weeks * stats >= 3)
            {
                body = Female ? 0x3C : 0x3D;
                mat = DragonMaturity.Youth;
                gainfactor = 5.0;
                statmult = 1.0;
                hue = 0;
            }
            else if (m_Maturity == DragonMaturity.Youth && weeks * stats >= 5)
            {
                body = Female ? 0xC : 0x3B;
                mat = DragonMaturity.Adult;
                gainfactor = 1.0;
                statmult = 1.0;
                hue = 0;
            }
            else if (m_Maturity == DragonMaturity.Adult && weeks / stats >= 50)
            {
                body = 0x2E;
                mat = DragonMaturity.Ancient;
                gainfactor = 0.0;
                statmult = 0.6;
                hue = 0;
            }
            else
                return; // no change needed

            //if (IsStabled || Maturity == DragonMaturity.Egg)
            //{
            m_LastGrowth = DateTime.Now;

            Body = body;
            m_Maturity = mat;
            m_GainFactor = gainfactor;
            Hue = hue;

            RawStr = (int)((double)RawStr * statmult);
            RawInt = (int)((double)RawInt * statmult);
            RawDex = (int)((double)RawDex * statmult);
            //}
            //else
            //    PublicOverheadMessage(Server.Network.MessageType.Regular, 0, true, "*your pet seems tired*");
        }

        protected override double GainChance(Skill skill, double chance, bool success)
        {
            return base.GainChance(skill, chance, success) * m_GainFactor;
        }

        protected override double StatGainChance(Skill skill, Stat stat)
        {
            return base.StatGainChance(skill, stat) * m_GainFactor;
        }

        protected override void AttackOrderHack(Mobile aggressor)
        {
            if (AIObject is DragonAI && ((DragonAI)AIObject).BreedingState != DragonAI.BreedState.FightingCompetition)
            {
                // don't attack Scary creatures if we are ScaredOfScaryThings
                if (aggressor is BaseCreature && (((BaseCreature)aggressor).IsScaryToPets && ((BaseCreature)aggressor).IsScaryCondition()) && this.IsScaredOfScaryThings)
                    return;

                ControlTarget = aggressor;
                ControlOrder = OrderType.Attack;
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is SulfurousAsh && from == ControlMaster)
            {
                Animate(17, 5, 1, true, false, 0);
                SayTo(from, Name + " gets a deep, fiery gleam in " + (Female ? "her" : "his") + " eye.");
                Items.Add(new TimedProperty(Server.Items.Use.SABonus, null, TimeSpan.FromMinutes(5.0)));

                dropped.Delete();
                return true;
            }

            if (dropped is BlackPearl && from == ControlMaster)
            {
                Animate(17, 5, 1, true, false, 0);
                SayTo(from, Name + " straightens up and seems more vibrant.");
                Items.Add(new TimedProperty(Server.Items.Use.BPBonus, null, TimeSpan.FromMinutes(5.0)));

                dropped.Delete();
                return true;
            }

            return base.OnDragDrop(from, dropped);
        }

        public override bool CheckBreedWith(BaseCreature male)
        {
            if (!(male is Dragon))
                return false;

            if (!(AIObject is DragonAI))
                return false;

            if (Maturity != DragonMaturity.Adult)
                return false;

            if ((AIObject as DragonAI).CheckMateAccept(male as Dragon))
                return base.CheckBreedWith(male);

            return false;
        }

        public virtual void BeginMate(Dragon mate)
        {
            if (AIObject is DragonAI)
                (AIObject as DragonAI).BeginMate(mate);
        }

        public virtual void EndMate(bool success)
        {
            if (AIObject is DragonAI)
                (AIObject as DragonAI).EndMate(success);
        }

        public override string DescribeGene(System.Reflection.PropertyInfo prop, GeneAttribute attr)
        {
            if (attr != null)
            {
                switch (attr.Name)
                {
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
                    case "Scales":
                        {
                            if (VirtualArmor < 54)
                                return "Flimsy";
                            if (VirtualArmor < 58)
                                return "Durable";
                            if (VirtualArmor < 62)
                                return "Rugged";
                            if (VirtualArmor < 66)
                                return "Hard";

                            return "Plated";
                        }
                    case "Claw Size":
                        {
                            switch (DamageMax)
                            {
                                case 20:
                                    return "Undeveloped";
                                case 21:
                                    return "Small";
                                case 22:
                                    return "Ample";
                                case 23:
                                    return "Large";
                                case 24:
                                    return "Frightening";
                            }
                            break;
                        }
                    case "Claw Accuracy":
                        {
                            if (DamageMin < 14)
                                return "Unwieldy";
                            if (DamageMin < 16)
                                return "Able";
                            if (DamageMin < 18)
                                return "Deft";

                            return "Precise";
                        }
                    case "Physique":
                        {
                            if (HitsMaxDiff < -10)
                                return "Frail";
                            if (HitsMaxDiff < -5)
                                return "Spindly";
                            if (HitsMaxDiff < 0)
                                return "Slight";
                            if (HitsMaxDiff < 5)
                                return "Lithe";
                            if (HitsMaxDiff < 10)
                                return "Sturdy";

                            return "Tough";
                        }
                    case "Hides":
                        {
                            if (Hides < 15)
                                return "Delicate";
                            if (Hides < 18)
                                return "Supple";
                            if (Hides < 22)
                                return "Thick";
                            if (Hides < 25)
                                return "Heavy";

                            return "Mountainous";
                        }
                    case "Meat":
                        {
                            if (Meat < 16)
                                return "Frail";
                            if (Meat < 20)
                                return "Lean";
                            if (Meat < 24)
                                return "Brawny";

                            return "Colossal";
                        }
                    default:
                        return base.DescribeGene(prop, attr);
                }
            }

            return "Error";
        }

        private int m_IntMax, m_StrMax, m_DexMax, m_HitsMaxDiff;
        private double m_StatCapFactor;

        [Gene("Versatility", .05, .05, 1.975, 2.025, 1.95, 2.05, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public double StatCapFactor
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

        // 796, 825
        [Gene("StrMax", 1145, 1295, 995, 1445)]
        [CommandProperty(AccessLevel.GameMaster)]
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

        // 436, 475
        [Gene("IntMax", 640, 736, 545, 831)]
        [CommandProperty(AccessLevel.GameMaster)]
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

        // 86, 105
        [Gene("DexMax", 133, 159, 108, 184)]
        [CommandProperty(AccessLevel.GameMaster)]
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

        private static double DragonActiveSpeed { get { return Server.Items.Consoles.DragonSpeedMCi.DragonActiveSpeed; } }
        private static double DragonPassiveSpeed { get { return Server.Items.Consoles.DragonSpeedMCi.DragonPassiveSpeed; } }

        [Constructable]
        public Dragon()
            : base(AIType.AI_Dragon, FightMode.All | FightMode.Closest, 10, 1, DragonActiveSpeed, DragonPassiveSpeed)
        {
            Name = "a dragon";
            Body = Female ? 12 : 59;
            BaseSoundID = 362;

            SetStr(675 + StrMax / 100, 754 + StrMax / 100);
            SetInt(370 + IntMax / 100, 434 + IntMax / 100);
            SetDex(73 + DexMax / 100, 96 + DexMax / 100);

            SetSkill(SkillName.EvalInt, 30.1, 40.0);
            SetSkill(SkillName.Magery, 30.1, 40.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 92.5);

            Fame = 15000;
            Karma = -15000;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 93.9;

            m_Maturity = DragonMaturity.Adult;
            m_GainFactor = 1.0;
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

        public override bool BreedingEnabled
        {
            get
            {
                return true;
            }
        }

        public override void ValidateGenes()
        {
            base.ValidateGenes();

            if (DamageMin > DamageMax)
            {
                int t = DamageMin;
                DamageMin = DamageMax;
                DamageMax = t;
            }
        }

        [Gene("Scales", .05, .05, 58, 62, 50, 70, GeneVisibility.Wild)]
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

        [Gene("Claw Accuracy", .05, .05, 15, 17, 12, 20, GeneVisibility.Tame)]
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

        [Gene("Claw Size", .05, .05, 21, 23, 20, 24, GeneVisibility.Tame)]
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

        [Gene("Physique", .05, .05, -10, 10, -15, 15, GeneVisibility.Tame)]
        [CommandProperty(AccessLevel.GameMaster)]
        public int HitsMaxDiff
        {
            get
            {
                return m_HitsMaxDiff;
            }
            set
            {
                m_HitsMaxDiff = value;
                if (Hits > HitsMax)
                    Hits = HitsMax;
                Delta(MobileDelta.Hits);
            }
        }

        public override int HitsMax
        {
            get
            {
                // do not use base.HitsMax here - many dragons have a non-zero HitsMaxSeed from before breeding!
                // this formula is quite intentional.
                return base.Str - HitsMaxDiff;
            }
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
                                                                // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.UOAI || Core.UOAR ? false : Core.PublishDate >= Core.EraREN ? true : false; } }
        public override int TreasureMapLevel { get { return Core.UOAI || Core.UOAR ? 4 : 0; } }
        public override int Scales { get { return (Core.UOAI || Core.UOAR || Core.PublishDate < Core.PlagueOfDespair) ? 0 : 7; } }
        public override ScaleType ScaleType { get { return (Body == 12 ? ScaleType.Yellow : ScaleType.Red); } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }

        private int m_Meat, m_Hides;

        [Gene("Meat", .05, .05, 18, 22, 12, 28, GeneVisibility.Wild)]
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

        [Gene("Hides", .05, .05, 18, 22, 12, 28, GeneVisibility.Wild)]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hides
        {
            get
            {
                return m_Hides;
            }
            set
            {
                m_Hides = value;
            }
        }

        public override HideType HideType { get { return HideType.Barbed; } }

        public Dragon(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.UOAI || Core.UOAR)
            {
                if (m_LastGrowth != DateTime.MinValue)
                    return;

                for (int i = 0; i < 8; ++i)
                    PackGem();

                PackGold(800, 900);
                PackMagicEquipment(1, 3, 0.50, 0.50);
                PackMagicEquipment(1, 3, 0.15, 0.15);

                // Category 4 MID
                PackMagicItem(2, 3, 0.10);
                PackMagicItem(2, 3, 0.05);
                PackMagicItem(2, 3, 0.02);
            }
            else
            {
                if (Core.UOSP || Core.UOMO)
                {   // http://web.archive.org/web/20020213035855/uo.stratics.com/hunters/dragon.shtml
                    // 1200 to 1400 Gold, Gems, Magic items, 19 Raw Ribs (carved), 20 Hides (carved)

                    if (Spawning)
                    {
                        PackGold(1200, 1400);
                    }
                    else
                    {
                        PackGem(1, .9);
                        PackGem(1, .5);
                        PackMagicStuff(1, 2, 0.40);
                        PackMagicStuff(1, 2, 0.05);
                    }
                }
                else
                {
                    AddLoot(LootPack.FilthyRich, 2);
                    AddLoot(LootPack.Gems, 8);
                }
            }
        }

        // not serialized
        private byte m_KukuiNutCount = 0;
        public override bool CheckFeed(Mobile from, Item dropped)
        {
            //special kukui nut opt-in for breeding system.
            if (!IsDeadPet && Controlled && ControlMaster == from && dropped is KukuiNut)
            {
                if (BreedingParticipant == false)
                {
                    // sanity
                    if (m_Hatchdate != DateTime.MinValue)
                    {
                        try { throw new ApplicationException("Dragon.CheckFeed(): m_Hatchdate != DateTime.MinValue"); }
                        catch (Exception ex) { LogHelper.LogException(ex); }
                    }

                    switch (++m_KukuiNutCount)
                    {
                        case 1:
                            from.SendMessage("The magical properties of 3 kukui nuts will make your dragon fertile.");
                            break;

                        case 2:
                            from.SendMessage("Fertile dragons may be bred, but they also grow older and will become weak in old age.");
                            break;

                        case 3:
                            from.SendMessage("Your dragon is now fertile.");
                            break;
                    }

                    // do it
                    if (m_KukuiNutCount == 3)
                    {
                        // Enroll as a breeding system participant, sets m_HatchDate
                        BreedingParticipant = true;

                        // Animate
                        FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);
                        if (Body.IsAnimal)
                            Animate(3, 5, 1, true, false, 0);
                        else if (Body.IsMonster)
                            Animate(17, 5, 1, true, false, 0);

                        // log those that optin so there can be no fibbiing later when their pet gets old
                        LogHelper logger = new LogHelper("DragonBreedingOptIn.log", false);
                        logger.Log(LogType.Mobile, this.ControlMaster, "I am opting in to the breeding system.");
                        logger.Log(LogType.Mobile, this, "My master has opted in to the breeding system.");
                        logger.Finish();
                    }

                    dropped.Delete();
                    return true;
                }
                else
                {
                    // Animate
                    if (Body.IsAnimal)
                        Animate(3, 5, 1, true, false, 0);
                    else if (Body.IsMonster)
                        Animate(17, 5, 1, true, false, 0);

                    from.SendMessage("The magical properties appear to be wasted on this creature.");
                    dropped.Delete();
                    return true;
                }
            }
            else
                return base.CheckFeed(from, dropped);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)3);

            // version 3
            writer.Write((int)m_Maturity);
            writer.Write(m_LastGrowth);
            writer.Write(m_GainFactor);
            writer.Write(m_Hatchdate);
            writer.Write(m_CheckedBody);

            // version 2
            // do nothing - one-time logic update placeholder

            // version 1
            writer.Write(m_IntMax);
            writer.Write(m_DexMax);
            writer.Write(m_StrMax);
            writer.Write(m_StatCapFactor);
            writer.Write(m_Hides);
            writer.Write(m_Meat);
            writer.Write(m_HitsMaxDiff);

            // version 0
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_Maturity = (DragonMaturity)reader.ReadInt();
                        m_LastGrowth = reader.ReadDateTime();
                        m_GainFactor = reader.ReadDouble();
                        m_Hatchdate = reader.ReadDateTime();
                        m_CheckedBody = reader.ReadDateTime();

                        goto case 2;
                    }
                case 2:
                    {
                        goto case 1;
                    }
                case 1:
                    {
                        m_IntMax = reader.ReadInt();
                        m_DexMax = reader.ReadInt();
                        m_StrMax = reader.ReadInt();
                        m_StatCapFactor = reader.ReadDouble();
                        m_Hides = reader.ReadInt();
                        m_Meat = reader.ReadInt();
                        m_HitsMaxDiff = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    break;
            }

            if (version < 3)
            {
                m_Maturity = DragonMaturity.Adult;
                m_GainFactor = 1.0;
            }
            if (version < 2)
            {
                StatCapFactor = Utility.RandomDouble() * 0.05 + 1.975;
                StrMax = (int)(Utility.RandomDouble() * 150) + 1145;
                IntMax = (int)(Utility.RandomDouble() * 96) + 640;
                DexMax = (int)(Utility.RandomDouble() * 26) + 133;
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

                Console.WriteLine("Warning: Dragon.ValidateStatCap didn't change any stats when it should have. Tell Taran Kain.");
                Console.WriteLine("Stats: {0} {1} {2}", RawStr, RawInt, RawDex);
                return;
            }

            // damn do i love recursion, it makes my life soooo so easy
            // re-call ourselves, as the differing stat values makes 1 str point worth less than 1 dex point
            // thus, we may actually have to go through several steps of dropping stats to validate statcap
            ValidateStatCap(increased);
        }
    }

    public class DragonAI : MageAI
    {
        public enum BreedState
        {
            None,
            Approaching,
            FightingCompetition,
            MovingIn,
            Mating
        }

        private Dragon m_MateTarget;
        private DateTime m_BecameIdle;
        private DateTime m_NextMateAttempt;
        private DateTime m_LastMate;
        private DateTime m_BeganMateAttempt;
        private DateTime m_BeganTheNasty;
        private int m_FightDistance;
        private List<Dragon> m_Ignore;
        private BreedState m_BreedingState;

        private static TimeSpan MateIdleDelay = TimeSpan.FromMinutes(2.0);
        private static TimeSpan MaleMateDelay = TimeSpan.FromHours(2.0); // FromHours 2
        private static TimeSpan FemaleMateDelay = TimeSpan.FromHours(6.0); // FromHours 6
        private static TimeSpan MateDuration = TimeSpan.FromSeconds(10.0);
        private static TimeSpan MateAttemptTimout = TimeSpan.FromMinutes(5.0); // if it's taking longer than 5 min, give up
        private const int MinMateAttemptDelay = 20;
        private const int MaxMateAttemptDelay = 45;
        private const double m_MateAttemptChance = 0.1;
        private const double m_MateAcceptChance = 0.6;
        private const double m_MateSuccessChance = 0.25;
        private const double StillBornChance = 0.05;
        private const double MateHealthThreshold = 0.8;

        private double MateAttemptChance
        {
            get
            {
                if (TestCenter.Enabled)
                    return 1.0;
                if (Property.FindUse(m_Mobile, Use.SABonus))
                    return m_MateAttemptChance * 1.25;
                return m_MateAttemptChance;
            }
        }

        private double MateAcceptChance
        {
            get
            {
                if (TestCenter.Enabled)
                    return 1.0;
                if (Property.FindUse(m_Mobile, Use.SABonus))
                    return m_MateAcceptChance * 1.25;
                return m_MateAcceptChance;
            }
        }

        private double MateSuccessChance
        {
            get
            {
                if (TestCenter.Enabled)
                    return 1.0;
                if (Property.FindUse(m_Mobile, Use.BPBonus))
                    return m_MateSuccessChance * 1.25;
                return m_MateSuccessChance;
            }
        }

        public static void Initialize()
        {
            if (TestCenter.Enabled)
            {
                MateIdleDelay = TimeSpan.FromMinutes(5.0);
                FemaleMateDelay = TimeSpan.FromMinutes(10.0);
            }
        }

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

        public bool InFireDungeon(Mobile m)
        {
            if (m == null || m.Deleted == true)
                return false;

            // on TC drags can breed anywhere (don't want testers getting PKed)
            if (TestCenter.Enabled == true)
                return true;

            return m.Region.Name.ToLower() == "fire";
        }

        public DragonAI(BaseCreature m)
            : base(m)
        {
            m_MateTarget = null;
            m_BecameIdle = DateTime.Now;
            m_NextMateAttempt = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(MinMateAttemptDelay, MaxMateAttemptDelay));
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
            Dragon mob = m_Mobile as Dragon;
            if (mob == null)
                return false; // big problems!

            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BreedingEnabled) && mob.BreedingParticipant)
            {

                if (!m_Mobile.Female &&                                                     // males do the finding
                    m_Mobile.Alive &&                                                       // ...
                    mob.Maturity == Dragon.DragonMaturity.Adult &&                          // must be an adult
                    (double)m_Mobile.Hits / m_Mobile.HitsMax >= MateHealthThreshold &&      // must be minimum health
                    m_BecameIdle + MateIdleDelay < DateTime.Now &&                          // wait after starting to wander
                    m_LastMate + MaleMateDelay < DateTime.Now &&                            // wait after last mating - we gotta recoup!
                    m_NextMateAttempt < DateTime.Now &&                                     // gotta give it a while between looking
                    InFireDungeon(m_Mobile))                                                // gotta be in fire dungeon
                {
                    FindMate();
                    m_NextMateAttempt += TimeSpan.FromSeconds(Utility.RandomMinMax(MinMateAttemptDelay, MaxMateAttemptDelay));

                    if (m_MateTarget != null && Utility.RandomDouble() < MateAttemptChance)
                    {
                        m_Mobile.DebugSay("Found a mate I like! Trying to mate...");

                        BreedingState = BreedState.Approaching;
                        Action = ActionType.Interact;
                        m_BeganMateAttempt = DateTime.Now;
                        m_Mobile.PlaySound(Utility.RandomList(364, 365, 362));
                    }

                    return true;
                }
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
                    m_Mobile.PlaySound(Utility.RandomList(362, 364, 365, 705, 711, 712, 714, 715, 718));

                return true;
            }

            // must be AFTER female case - females don't ever get m_BeganMateAttempt set!
            // funky check - basically we can exceed timeout by Random * Patience seconds
            // or, if we're already actually doing the nasty, we're not gonna give up

            /*	DEBUG VERSION -- so we can see what's going on
			double seconds = ((m_BeganMateAttempt + MateAttemptTimout) - DateTime.Now).TotalSeconds;
			double patience = Utility.RandomDouble() * m_Mobile.Patience;
			if (this.m_Mobile.ControlMaster != null && this.m_Mobile.ControlMaster.AccessLevel > AccessLevel.Player && this.m_Mobile.ControlMaster.NetState != null)
				this.m_Mobile.SayTo(this.m_Mobile.ControlMaster, String.Format("seconds {0} > patience {1}", seconds, patience));
			if (BreedingState != BreedState.Mating && seconds > patience)
			 */

            // original version - always times out on Patience
            //if (BreedingState != BreedState.Mating && 
            //((m_BeganMateAttempt + MateAttemptTimout) - DateTime.Now).TotalSeconds > Utility.RandomDouble() * m_Mobile.Patience)

            // adam: new temp version until taran cn have a look .. here I'm simply removing Patience
            //	but keeping the basic timeout logic
            if (BreedingState != BreedState.Mating && DateTime.Now > (m_BeganMateAttempt + MateAttemptTimout))
            {
                m_Mobile.DebugSay("F it. This broad's too much work.");

                if (m_MateTarget != null)
                {
                    Console.WriteLine("DEBUG: EndMating on the female during timeout. Tell Taran Kain.");
                    m_MateTarget.EndMate(false);
                }
                EndMate(false);

                Action = ActionType.Wander;

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
                            m_Ignore = new List<Dragon>();
                            BreedingState = BreedState.FightingCompetition;
                        }

                        break;
                    }
                case BreedState.FightingCompetition:
                    {
                        // depending on temper, fight all other male dragons near target
                        if (m_FightDistance > -1)
                        {
                            IPooledEnumerable eable = m_Mobile.Map.GetMobilesInRange(m_MateTarget.Location, m_FightDistance);
                            foreach (Mobile m in eable)
                            {
                                Dragon c = m as Dragon;

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
                                m_MateTarget.BeginMate(m_Mobile as Dragon);

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
                        if (!m_MateTarget.CheckBreedWith(m_Mobile) ||       // does she STILL like us?
                            m_MateTarget.Location != m_Mobile.Location ||   // did she leave?
                            !InFireDungeon(m_Mobile))                       // sanity check
                        {
                            // crap, she doesn't
                            m_MateTarget.EndMate(false); // important that this goes first, as EndMateWith nullifies m_MateTarget
                            EndMate(false);

                            m_Mobile.DebugSay("Shit, she don't like me anymore.");

                            break;
                        }

                        if (m_BeganTheNasty + MateDuration < DateTime.Now)
                        {
                            // patience affects chance to successfully procreate
                            if (Utility.RandomDouble() < (MateSuccessChance + (m_Mobile.Patience - 10) / 500.0))
                            {
                                m_Mobile.DebugSay("Smokin a cig...");

                                m_Mobile.PlaySound(Utility.RandomList(362, 364, 365, 705, 711, 712, 714, 715, 718));

                                Dragon child = m_MateTarget.BreedWith(m_Mobile) as Dragon;
                                DragonEgg egg;

                                if (Utility.RandomDouble() < StillBornChance)
                                    egg = new DragonEgg(null);
                                else
                                    egg = new DragonEgg(child);

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

                                m_Mobile.PlaySound(366);
                            }

                            return true;
                        }
                        else
                        {
                            m_Mobile.DebugSay("Get down tonight...");

                            if (Utility.RandomDouble() < .3)
                                m_Mobile.PlaySound(Utility.RandomList(362, 364, 365, 705, 711, 712, 714, 715, 718));

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
                m_BecameIdle = DateTime.Now;

            base.OnActionChanged();
        }

        public override void OnCurrentOrderChanged()
        {
            if (m_Mobile.ControlOrder == OrderType.None)
            {
                m_BecameIdle = DateTime.Now; // action must = wander and order must = none, setting this in both ensures max wait
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

            // find the closest female dragon
            foreach (Mobile m in eable)
            {
                Dragon c = m as Dragon;
                double d = m_Mobile.GetDistanceToSqrt(m);
                if (c != null && c.Female && c.BreedingParticipant && d < dist)
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

        public bool CheckMateAccept(Dragon male)
        {
            if (m_Mobile.Deleted || !m_Mobile.Alive)
                return false; // we've got bigger shit to worry about

            if (!m_Mobile.Female || male == null || male.Female)
                return false; // dragons only swing one way

            if (!m_Mobile.Controlled)
                return false; // only tame drags mate

            if (!InFireDungeon(m_Mobile))
                return false; // only mate in fire dungeon

            if ((double)m_Mobile.Hits / m_Mobile.HitsMax < MateHealthThreshold)
                return false; // must be minimum health                   

            if ((Action != ActionType.Wander && Action != ActionType.Interact) ||
                m_Mobile.ControlOrder != OrderType.None || m_BecameIdle + MateIdleDelay > DateTime.Now)
                return false; // we are busy

            if (m_MateTarget == male)
                return true; // we're already mating with them, they're ok

            if (m_MateTarget != null && m_MateTarget != male)
                return false; // we're already mating with someone else

            if (m_LastMate + FemaleMateDelay > DateTime.Now)
                return false; // haven't waited long enough yet - need to recoup!

            // male's wisdom can up chances up to .10, our patience can up chances up to .05, our temper can lower chances up to .15
            double score = Utility.RandomDouble() - male.Wisdom / 1000.0 - m_Mobile.Patience / 2000.0 + m_Mobile.Temper * 3.0 / 2000.0;
            if (score < MateAcceptChance)
                return true;
            else
                return false;
        }

        public void BeginMate(Dragon mate)
        {
            // mark us as gettin down
            Action = ActionType.Interact; // important that this happens BEFORE setting BeganTheNasty - OnActionChanged will overwrite it otherwise
            m_BeganTheNasty = DateTime.Now;
            m_MateTarget = mate;
        }

        public void EndMate(bool success)
        {
            if (success)
            {
                m_LastMate = DateTime.Now;
            }

            m_MateTarget = null;
            m_NextMateAttempt = DateTime.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(MinMateAttemptDelay, MaxMateAttemptDelay));
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

    public class CookedDragonEgg : Food
    {
        [Constructable]
        public CookedDragonEgg()
            : this(1)
        {
        }

        [Constructable]
        public CookedDragonEgg(int amt)
            : base(amt, 4963)
        {
            Weight = 8;
            FillFactor = 20;
            Name = "cooked dragon egg";
        }

        public CookedDragonEgg(Serial s)
            : base(s)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new CookedDragonEgg(), amount);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool Eat(Mobile from)
        {
            if (base.Eat(from))
            {
                from.AddStatMod(new StatMod(StatType.All, "CookedDragonEgg", 5.0, TimeSpan.FromMinutes(2.0)));
                from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                from.PlaySound(0x1EA);

                return true;
            }

            return false;
        }
    }

    public class DragonEgg : CookableFood
    {
        private enum HatchState
        {
            Rustle,
            Crack,
            Beak,
            Split,
            Hatch
        }

        private static List<DragonEgg> m_Eggs;
        private static Timer m_Tick;

        public static void Configure()
        {
            m_Eggs = new List<DragonEgg>();

            m_Tick = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMinutes(5.0), new TimerCallback(OnTick));
            m_Tick.Priority = TimerPriority.OneMinute;
            m_Tick.Start();
        }

        private static void OnTick()
        {
            // kinda complicated at first - simple in concept
            // remove any eggs from list IF: egg is null, egg is deleted, or egg.HatchTick() returns false
            m_Eggs.RemoveAll(new Predicate<DragonEgg>(delegate (DragonEgg egg) { return (egg == null || egg.Deleted || !egg.HatchTick()); }));
        }

        private Dragon m_Chick;
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

        public DragonEgg(Dragon child)
            : base(4963, 80)
        {
            Name = "dragon egg";
            Weight = 8;
            Hue = 1053;

            m_Birthdate = DateTime.Now + TimeSpan.FromDays(5.0 + 2 * Utility.RandomDouble());
            if (TestCenter.Enabled)
                m_Birthdate = DateTime.Now + TimeSpan.FromMinutes(20.0 + 5 * Utility.RandomDouble());
            m_Chick = child;
            m_Hatch = null;
            m_Health = 5;

            if (m_Chick != null)
            {
                m_Chick.SpawnerTempMob = true;
                m_Chick.Maturity = Dragon.DragonMaturity.Egg;
                m_Eggs.Add(this);
            }
        }

        public override Food Cook()
        {
            return new CookedDragonEgg();
        }

        private bool HatchTick()
        {
            if (m_Chick == null || m_Chick.Deleted)
                return false;

            TimeSpan ts = m_Birthdate - DateTime.Now;

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
                m_Chick.SpawnerTempMob = false;
                m_Chick.Delete();
                m_Birthdate = DateTime.MinValue;

                return false; // remove us from tick list
            }

            return true;
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
                Console.WriteLine("{0} passed to DragonEgg.Hatch(). Tell Taran Kain.", ob);
                return;
            }

            if (Deleted || m_Chick == null || m_Chick.Deleted)
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
                            PublicOverheadMessage(Server.Network.MessageType.Regular, 0, true, "You can see a claw!");
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
                        m_Chick.OnHatch();
                        m_Chick.MoveToWorld(GetWorldLocation(), Map);
                        m_Chick.PlaySound(Utility.RandomList(111, 113, 114, 115));
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
                    m_Chick.SpawnerTempMob = false; // chick will get auto-cleaned up if it's still internal

                BaseHouse bh;
                if (IsLockedDown && (bh = BaseHouse.FindHouseAt(this)) != null)
                    bh.LockDowns.Remove(this);
            }
        }

        public DragonEgg(Serial s)
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
                        m_Chick = reader.ReadMobile() as Dragon;
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
}
