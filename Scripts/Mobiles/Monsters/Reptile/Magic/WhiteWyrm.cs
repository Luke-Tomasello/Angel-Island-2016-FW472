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

/* Scripts/Mobiles/Monsters/Reptile/Magic/WhiteWyrm.cs
 * ChangeLog
 *	4/10/10, adam
 *		Add speed management MCi to tune dragon speeds.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	12/21/05, Adam
 *		10% at a pure White Wyrm
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/2/04
 *		Change chance to drop a magic item to 30% 
 *		add a 5% chance for a bonus drop at next intensity level
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a white wyrm corpse")]
    public class WhiteWyrm : BaseCreature
    {
        private static double WyrmActiveSpeed { get { return Server.Items.Consoles.DragonSpeedMCi.WyrmActiveSpeed; } }
        private static double WyrmPassiveSpeed { get { return Server.Items.Consoles.DragonSpeedMCi.WyrmPassiveSpeed; } }

        [Constructable]
        public WhiteWyrm()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, WyrmActiveSpeed, WyrmPassiveSpeed)
        {
            // Adam: 10% at a pure White Wyrm
            if (0.12 > Utility.RandomDouble())
            {
                Body = 59;
                Hue = 1153;
            }
            else
                Body = 49;

            Name = "a white wyrm";
            BaseSoundID = 362;

            SetStr(721, 760);
            SetDex(101, 130);
            SetInt(386, 425);

            SetHits(433, 456);

            SetDamage(17, 25);

            SetSkill(SkillName.EvalInt, 99.1, 100.0);
            SetSkill(SkillName.Magery, 99.1, 100.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 64;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 96.3;
        }

        public override void GenerateLoot()
        {
            if (Core.UOAI || Core.UOREN)
            {
                int gems = Utility.RandomMinMax(1, 5);

                for (int i = 0; i < gems; ++i)
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
                {   // http://web.archive.org/web/20020607002613/uo.stratics.com/hunters/whitewyrm.shtml
                    // 1800 to 2100 Gold, Gems, Level 4 Treasure Map, Magic Weapons and Armor, 7 or 10 White Scales, 19 Raw Meat, 20 Hides
                    if (Spawning)
                    {
                        PackGold(1800, 2100);
                    }
                    else
                    {
                        PackGem(Utility.Random(1, 5));
                        PackMagicEquipment(2, 3);
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.FilthyRich, 2);
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Gems, Utility.Random(1, 5));
                }
            }
        }

        public override int TreasureMapLevel { get { return Core.UOAI || Core.UOREN ? 4 : 4; } }
        public override int Meat { get { return Core.UOAI || Core.UOREN ? 20 : 19; } }
        public override int Hides { get { return Core.UOAI || Core.UOREN ? 40 : 20; } }
        public override HideType HideType { get { return HideType.Barbed; } }
        public override int Scales { get { return (Core.UOAI || Core.UOREN || PublishInfo.PublishDate < Core.PlagueOfDespair) ? 0 : Utility.RandomBool() ? 7 : 10; } }
        public override ScaleType ScaleType { get { return ScaleType.White; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Gold; } }

        public WhiteWyrm(Serial serial)
            : base(serial)
        {
        }

        public override bool OnBeforeDeath()
        {
            //			if( !IsBonded )
            //			{
            //				int gems = Utility.RandomMinMax( 1, 5 );
            //				for ( int i = 0; i < gems; ++i )
            //					PackGem();
            //
            //				PackGold( 800, 900 );
            //				PackMagicEquipment( 1, 3, 0.50, 0.50 );
            //				PackMagicEquipment( 1, 3, 0.15, 0.15 );
            //
            //				// Category 4 MID
            //				PackMagicItem( 2, 3, 0.10 );
            //				PackMagicItem( 2, 3, 0.05 );
            //				PackMagicItem( 2, 3, 0.02 );
            //			}

            return base.OnBeforeDeath();
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
