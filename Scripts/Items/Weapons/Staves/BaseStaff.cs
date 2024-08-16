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

namespace Server.Items
{
    public abstract class BaseStaff : BaseMeleeWeapon
    {
        public override int DefHitSound { get { return 0x233; } }
        public override int DefMissSound { get { return 0x239; } }

        public override SkillName DefSkill { get { return SkillName.Macing; } }
        public override WeaponType DefType { get { return WeaponType.Staff; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Bash2H; } }

        public BaseStaff(int itemID)
            : base(itemID)
        {
        }

        public BaseStaff(Serial serial)
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

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);

            defender.Stam -= Utility.Random(2, 4); // 3-5 points of stamina loss
        }
    }
}
