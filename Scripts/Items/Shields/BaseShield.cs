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

/* Scripts/items/sheilds/baseshield.cs
 * CHANGELOG
 *	6/10/10, Adam
 *		Port OnHit logic to that of RunUO 2.0
 *			primary effect is the damage and frequency of damage done to the shield.
 *  04/30/05, Kit
 *		Added meditation allowance none to allow passive mana regen with sheilds equiped
 */

using Server.Network;

namespace Server.Items
{
    public class BaseShield : BaseArmor
    {
        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override ArmorMeditationAllowance DefMedAllowance { get { return ArmorMeditationAllowance.All; } }

        public BaseShield(int itemID)
            : base(itemID)
        {
        }

        public BaseShield(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override double ArmorRating
        {
            get
            {
                Mobile m = this.Parent as Mobile;
                double ar = base.ArmorRating;

                if (m != null)
                    return ((m.Skills[SkillName.Parry].Value * ar) / 200.0) + 1.0;
                else
                    return ar;
            }
        }

        public override int OnHit(BaseWeapon weapon, int damage)
        {
            Mobile owner = this.Parent as Mobile;
            if (owner == null)
                return damage;

            double ar = this.ArmorRating;
            double chance = (owner.Skills[SkillName.Parry].Value - (ar * 2.0)) / 100.0;

            if (chance < 0.01)
                chance = 0.01;
            /*
			FORMULA: Displayed AR = ((Parrying Skill * Base AR of Shield) � 200) + 1 

			FORMULA: % Chance of Blocking = parry skill - (shieldAR * 2)

			FORMULA: Melee Damage Absorbed = (AR of Shield) / 2 | Archery Damage Absorbed = AR of Shield 
			*/
            if (owner.CheckSkill(SkillName.Parry, chance))
            {
                if (weapon.Skill == SkillName.Archery)
                    damage -= (int)ar;
                else
                    damage -= (int)(ar / 2.0);

                if (damage < 0)
                    damage = 0;

                owner.FixedEffect(0x37B9, 10, 16);

                if (25 > Utility.Random(100)) // 25% chance to lower durability
                {
                    if (Core.AOS /*&& ArmorAttributes.SelfRepair > Utility.Random(10)*/)
                    {
                        HitPoints += 2;
                    }
                    else
                    {
                        int wear = Utility.Random(2);

                        if (wear > 0 && MaxHitPoints > 0)
                        {
                            if (HitPoints >= wear)
                            {
                                HitPoints -= wear;
                                wear = 0;
                            }
                            else
                            {
                                wear -= HitPoints;
                                HitPoints = 0;
                            }

                            if (wear > 0)
                            {
                                if (MaxHitPoints > wear)
                                {
                                    MaxHitPoints -= wear;

                                    if (Parent is Mobile)
                                        ((Mobile)Parent).LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061121); // Your equipment is severely damaged.
                                }
                                else
                                {
                                    Delete();
                                }
                            }
                        }
                    }
                }
            }

            return damage;
        }
    }
}
