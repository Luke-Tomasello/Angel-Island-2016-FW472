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

/* Scripts/Mobiles/Monsters/AOS/VampireBat.cs
 * ChangeLog
 *	6/28/08, Adam
 *		if a silver weapon, do 150% damage
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  9/21/04, Jade
 *      Increased gold drop from (20, 60) to (100, 150)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a vampire bat corpse")]
    public class VampireBat : BaseCreature
    {
        [Constructable]
        public VampireBat()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a vampire bat";
            Body = 317;
            BaseSoundID = 0x270;

            SetStr(91, 110);
            SetDex(91, 115);
            SetInt(26, 50);

            SetHits(55, 66);

            SetDamage(7, 9);

            SetSkill(SkillName.MagicResist, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 30.1, 55.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 14;
        }

        public override int GetIdleSound()
        {
            return 0x29B;
        }

        public VampireBat(Serial serial)
            : base(serial)
        {
        }

        // just add the vampirebat to the table of undead
        /*public override void CheckWeaponImmunity(BaseWeapon wep, int damagein, out int damage)
		{

			// if a silver weapon, do 150% damage
			if (wep.Slayer == SlayerName.Silver || wep.HolyBlade == true)
				damage = (int)(damagein * 1.5);
			// otherwise do only 25% damage
			else
				damage = (int)(damagein * .25);
		}*/

        public override void GenerateLoot()
        {
            PackGold(100, 150);
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
