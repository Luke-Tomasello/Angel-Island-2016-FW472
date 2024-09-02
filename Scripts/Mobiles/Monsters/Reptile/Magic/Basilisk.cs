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

/* Scripts/Mobiles/Monsters/Reptile/Melee/Basilisk.cs
 * ChangeLog
 *	5/19/06, Adam
 *		Add override ControlDifficulty(). Only use dragon difficulty for controling
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	10/1/04, Adam
 *		Up the skill cap for this baby to 800
 *	9/30/04, Adam
 *		Scale HitPoison (poison strength) based on poisoning skill
 *	9/28/04, Adam
 *		Heal the mana of the master if he is not hidden (like AOS ShadowWisp Familiar)
 *	9/21/04, Adam
 *		increase int from 101-140 to 436-475
 *		Add special ability - hide when master hides, attack what master attacks.
 *		only exhibit special abilities if bonded
 *	9/17/04, Adam
 *		1. Created
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a basilisk corpse")]
    public class Basilisk : BaseCreature
    {
        private bool m_LastHidden = false;
        private OrderType m_OrderMode = OrderType.None;
        private DateTime m_NextFlare = DateTime.UtcNow + TimeSpan.FromSeconds(5.0 + (25.0 * Utility.RandomDouble()));

        [Constructable]
        public Basilisk()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a basilisk";
            Body = Utility.RandomList(60, 61);
            BaseSoundID = 362;
            SkillsCap = 8000;   // Adam: 800 cap {magic + poison + melee}

            SetStr(401, 430);
            SetDex(133, 152);
            SetInt(436, 475);

            SetHits(241, 258);

            SetDamage(11, 17);

            SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.EvalInt, 30.1, 40.0);
            SetSkill(SkillName.Magery, 30.1, 40.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 92.5);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 50;

            Hue = 0x7D6;
            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 98.9;
        }

        public override bool ReAcquireOnMovement { get { return true; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.UOAI || Core.UOAR ? false : PublishInfo.PublishDate >= Core.EraREN ? true : false; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        public override int TreasureMapLevel { get { return Core.UOAI || Core.UOAR ? 4 : 0; } }
        public override int Meat { get { return 10; } }
        public override int Hides { get { return 20; } }
        public override HideType HideType { get { return HideType.Barbed; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Fish; } }

        public override double ControlDifficulty()
        {   // only use dragon difficulty for controling
            return 93.9;
        }

        public override Poison HitPoison
        {
            get
            {
                // chance deadly
                if (Skills[SkillName.Poisoning].Base == 100.0)
                    return (0.8 >= Utility.RandomDouble() ? Poison.Greater : Poison.Deadly);
                if (Skills[SkillName.Poisoning].Base > 90.0)
                    return (0.9 >= Utility.RandomDouble() ? Poison.Greater : Poison.Deadly);
                // chance greater
                if (Skills[SkillName.Poisoning].Base > 80.0)
                    return (0.8 >= Utility.RandomDouble() ? Poison.Regular : Poison.Greater);
                if (Skills[SkillName.Poisoning].Base > 70.0)
                    return (0.9 >= Utility.RandomDouble() ? Poison.Regular : Poison.Greater);
                // chance regular
                if (Skills[SkillName.Poisoning].Base > 60.0)
                    return (0.8 >= Utility.RandomDouble() ? Poison.Lesser : Poison.Regular);
                if (Skills[SkillName.Poisoning].Base > 50.0)
                    return (0.9 >= Utility.RandomDouble() ? Poison.Lesser : Poison.Regular);
                // lesser
                return (Poison.Lesser);
            }
        }

        public override int GetAttackSound()
        {
            return 713;
        }

        public override int GetAngerSound()
        {
            return 718;
        }

        public override int GetDeathSound()
        {
            return 716;
        }

        public override int GetHurtSound()
        {
            return 721;
        }

        public override int GetIdleSound()
        {
            return 725;
        }

        public Basilisk(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.UOAI || Core.UOAR)
            {
                PackGold(400, 500);
                PackItem(new DeadlyPoisonPotion());
                PackMagicEquipment(1, 2, 0.20, 0.20);
                PackMagicEquipment(1, 2, 0.05, 0.05);

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.UOSP || Core.UOMO)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        public override void OnThink()
        {
            base.OnThink();

            // only exhibit special abilities if bonded
            if (IsBonded == false)
                return;

            // who's your daddy!
            Mobile master = ControlMaster;
            if (master == null)
                return;

            // exit guard mode
            if (ControlOrder == OrderType.Stop)
                m_OrderMode = OrderType.None;

            // enter Guard mode
            if (ControlOrder == OrderType.Guard)
                m_OrderMode = OrderType.Guard;

            // if we're not in the special 'guard' mode, return
            //	to normal creature behavior
            if (m_OrderMode != OrderType.Guard)
                return;

            if (m_LastHidden != master.Hidden)
                Hidden = m_LastHidden = master.Hidden;

            Mobile toAttack = null;

            if (!Hidden)
            {
                toAttack = master.Combatant;

                if (toAttack == this)
                    toAttack = master;
                else if (toAttack == null)
                    toAttack = this.Combatant;
            }

            if (Combatant != toAttack)
                Combatant = null;

            if (toAttack == null)
            {
                if (ControlTarget != master || ControlOrder != OrderType.Follow)
                {
                    ControlTarget = master;
                    ControlOrder = OrderType.Follow;
                }
            }
            else if (ControlTarget != toAttack || ControlOrder != OrderType.Attack)
            {
                ControlTarget = toAttack;
                ControlOrder = OrderType.Attack;
            }

            // Heal the mana of the master if he is not hidden
            if (master.Hidden == false && master.Mana < master.Int)
                DoShadowWisp();
        }

        public void DoShadowWisp()
        {
            if (DateTime.UtcNow < m_NextFlare)
                return;

            m_NextFlare = DateTime.UtcNow + TimeSpan.FromSeconds(5.0 + (25.0 * Utility.RandomDouble()));

            this.FixedEffect(0x37C4, 1, 12, 1109, 6);
            this.PlaySound(0x1D3);

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerCallback(Flare));
        }

        private void Flare()
        {
            Mobile caster = this.ControlMaster;

            if (caster == null)
                caster = this.SummonMaster;

            if (caster == null)
                return;

            caster.FixedEffect(0x37C4, 1, 12, 1109, 3); // At player
            caster.Mana += 1;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
