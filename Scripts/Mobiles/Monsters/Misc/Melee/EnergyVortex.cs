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

/* Scripts/Mobiles/Monsters/Misc/Melee/Energyvortex.cs
 * ChangeLog
 *	2/9/11, Adam
 *		Add Llama vortices
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  4/27/05, Kit
 *		Adjusted dispell difficulty
 *  4/27/05, Kit
 *		Adapted to use new ev/bs logic
 *  7,17,04, Old Salty
 * 		Changed ActiveSpeed to make EV's a little slower.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/7/04, mith
 *		Increased Damage from 14-17 to 25-30.
 */

namespace Server.Mobiles
{
    [CorpseName("an energy vortex corpse")]
    public class EnergyVortex : BaseCreature
    {
        public override bool DeleteCorpseOnDeath { get { return Summoned; } }
        public override bool AlwaysMurderer { get { return true; } } // Or Llama vortices will appear gray.

        public override double DispelDifficulty { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? 56.0 : 80.0; } }
        public override double DispelFocus { get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules() ? 45.0 : 20; } }


        [Constructable]
        public EnergyVortex()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest | FightMode.Int, 6, 1, 0.199, 0.350)
        {
            Name = "an energy vortex";
            if (0.02 > Utility.RandomDouble()) // Tested on OSI, but is this right? Who knows.
            {
                // Llama vortex!
                Body = 0xDC;
                Hue = 0x76;
            }
            else
            {
                Body = 164;
            }

            SetStr(200);
            SetDex(200);
            SetInt(100);

            SetHits(70);
            SetStam(250);
            SetMana(0);

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.RenaissanceRules())
                SetDamage(25, 30);
            else
                SetDamage(14, 17);

            SetSkill(SkillName.MagicResist, 99.9);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 0;
            Karma = 0;

            VirtualArmor = 40;
            ControlSlots = 1;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override bool SpeedOverrideOK { get { return true; } }

        public override int GetAngerSound()
        {
            return 0x15;
        }

        public override int GetAttackSound()
        {
            return 0x28;
        }

        public EnergyVortex(Serial serial)
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

            if (BaseSoundID == 263)
                BaseSoundID = 0;
        }
    }
}
