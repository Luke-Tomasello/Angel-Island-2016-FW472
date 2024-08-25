using System;
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

/*
 * Scripts/Items/Weapons/SpearsAndForks/BaseSpear.cs
 * ChangeLog
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    public abstract class BaseSpear : BaseMeleeWeapon
    {
        public override int DefHitSound { get { return 0x23C; } }
        public override int DefMissSound { get { return 0x238; } }

        public override SkillName DefSkill { get { return SkillName.Fencing; } }
        public override WeaponType DefType { get { return WeaponType.Piercing; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Pierce2H; } }

        public BaseSpear(int itemID)
            : base(itemID)
        {
        }

        public BaseSpear(Serial serial)
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

            /* Publish 5
			 * Two-handed Weapons
			 * Any melee weapon that requires two hands to wield will gain a special attack. The type of special attack will depend on the type of weapon used. These special attacks will only work against player characters, not against monsters or animals.
			 * Mace Weapon: Crushing blow, a hit for double damage. Only applies to true maces, not staves.
			 * Sword Weapon: Concussion blow, victim�s intelligence is halved for 30 seconds. Note the effects of a concussion blow are not cumulative, once a target is the victim of a concussion blow, they cannot be hit in that manner again for 30 seconds.
			 * Fencing Weapon: Paralyzing blow, victim is paralyzed for 4 seconds. Once paralyzed, the victim cannot fight back (s/he wont auto-defend) or cast spells, however s/he can still use potions and bandages. The paralysis will not break by any means, even if the victim takes damage. Once paralyzed, the victim cannot be paralyzed again with another special attack until the paralysis wears off.
			 * Upon a successful hit, there will be a small chance to inflict one of the special attacks. The base chance to inflict one of the special attacks is 20%. A high intelligence will give a small bonus towards the chance to execute a special attack up to a total chance of 30%.
			 */

            // old runuo test.. I don't think it's right.
            // if (!Core.AOS && Layer == Layer.TwoHanded && (attacker.Skills[SkillName.Anatomy].Value / 400.0) >= Utility.RandomDouble())

            // these publishes don't have random special moves
            if (Core.UOAI || Core.UOAR || Core.UOMO || Core.AOS || Core.Publish >= 18)
                return;

            // humanoids can use the moves I guess, but only on players.
            if (!((attacker.Player || attacker.Body.IsHuman) && defender.Player && Layer == Layer.TwoHanded))
                return;

            // calc the chance: anat bonus max 20% + int bonue max 10% = max total bonus 30%
            double chance = (attacker.Skills[SkillName.Anatomy].Value / 500.0) + (attacker.Int / 1000.0);
            if (chance >= Utility.RandomDouble())
            {
                defender.SendMessage("You receive a paralyzing blow!"); // Is this not localized?
                defender.Freeze(TimeSpan.FromSeconds(2.0));

                attacker.SendMessage("You deliver a paralyzing blow!"); // Is this not localized?
                attacker.PlaySound(0x11C);
            }

            // adam: why is this not enabled?
            /*
			if (!Core.AOS && Poison != null && PoisonCharges > 0)
			{
				--PoisonCharges;

				if (Utility.RandomDouble() >= 0.5) // 50% chance to poison
					defender.ApplyPoison(attacker, Poison);
			}*/
        }
    }
}
