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

/* ./Scripts/Mobiles/Animals/Reptiles/Snake.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 3 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a snake corpse")]
    public class Snake : BaseCreature
    {
        [Constructable]
        public Snake()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a snake";
            Body = 52;
            Hue = Utility.RandomSnakeHue();
            BaseSoundID = 0xDB;

            SetStr(22, 34);
            SetDex(16, 25);
            SetInt(6, 10);

            SetHits(15, 19);
            SetMana(0);

            SetDamage(1, 4);

            SetSkill(SkillName.Poisoning, 50.1, 70.0);
            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 19.3, 34.0);
            SetSkill(SkillName.Wrestling, 19.3, 34.0);

            Fame = 300;
            Karma = -300;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 59.1;
        }

        public override Poison PoisonImmune { get { return Poison.Lesser; } }
        public override Poison HitPoison { get { return Poison.Lesser; } }

        public override int Meat { get { return 1; } }
        public override FoodType FavoriteFood { get { return FoodType.Eggs; } }

        public Snake(Serial serial)
            : base(serial)
        {
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
