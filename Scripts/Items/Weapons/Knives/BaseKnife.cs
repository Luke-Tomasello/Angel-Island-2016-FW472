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

/* Scripts/Items/Weapons/Swords/BaseKnife.cs
 * ChangeLog :
 *	10/16/05, Pix
 *		Streamlined applied poison code.
 *	09/13/05, erlein
 *		Reverted poisoning rules, applied same system as archery for determining
 *		poison level achieved.
 *	09/12/05, erlein
 *		Changed OnHit() code to utilise new poisoning rules.
 */


using Server.Targets;

namespace Server.Items
{
    public abstract class BaseKnife : BaseMeleeWeapon
    {
        public override int DefHitSound { get { return 0x23B; } }
        public override int DefMissSound { get { return 0x238; } }

        public override SkillName DefSkill { get { return SkillName.Swords; } }
        public override WeaponType DefType { get { return WeaponType.Slashing; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Slash1H; } }

        public BaseKnife(int itemID)
            : base(itemID)
        {
        }

        public BaseKnife(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(1010018); // What do you want to use this item on?

            from.Target = new BladedItemTarget(this);
        }

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);

            if (!Core.AOS && Poison != null && PoisonCharges > 0)
            {
                --PoisonCharges;

                if (Utility.RandomDouble() >= 0.5) // 50% chance to poison
                {
                    defender.ApplyPoison(attacker, GetPoisonBasedOnSkillAndPoison(attacker, Poison));
                }
            }
        }
    }
}
