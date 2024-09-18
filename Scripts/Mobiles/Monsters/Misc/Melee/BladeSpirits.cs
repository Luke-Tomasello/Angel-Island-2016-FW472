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

/* Scripts/Mobiles/Monsters/Misc/Melee/BladeSpirits.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 * 4/27/05, Kit
 *	Adjusted dispell difficulty
 *  4/27/05, Kit
 *	Adapted to use new ev/bs logic
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a blade spirit corpse")]
    public class BladeSpirits : BaseCreature
    {
        public override bool DeleteCorpseOnDeath { get { return Core.RuleSets.AOSRules(); } }
        public override bool IsHouseSummonable { get { return true; } }

        public override double DispelDifficulty { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? 56.0 : 0; } }
        public override double DispelFocus { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? 45.0 : 20.0; } }

        [Constructable]
        public BladeSpirits()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest | FightMode.Dex, 10, 1, 0.2, 0.4)
        {
            Name = "a blade spirit";
            Body = 574;

            SetStr(150);
            SetDex(150);
            SetInt(100);

            SetHits(80);
            SetStam(250);
            SetMana(0);

            SetDamage(10, 14);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 0;
            Karma = 0;

            VirtualArmor = 40;
            ControlSlots = 1;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override int GetAngerSound()
        {
            return 0x23A;
        }

        public override int GetAttackSound()
        {
            return 0x3B8;
        }

        public override int GetHurtSound()
        {
            return 0x23A;
        }

        public BladeSpirits(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
