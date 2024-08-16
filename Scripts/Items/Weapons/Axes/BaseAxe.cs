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

using Server.Engines.Harvest;
using System;
using System.Collections;

namespace Server.Items
{
    public abstract class BaseAxe : BaseMeleeWeapon, IUsesRemaining
    {
        public override int DefHitSound { get { return 0x232; } }
        public override int DefMissSound { get { return 0x23A; } }

        public override SkillName DefSkill { get { return SkillName.Swords; } }
        public override WeaponType DefType { get { return WeaponType.Axe; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Slash2H; } }

        public virtual HarvestSystem HarvestSystem { get { return Lumberjacking.System; } }

        private int m_UsesRemaining;
        private bool m_ShowUsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowUsesRemaining
        {
            get { return m_ShowUsesRemaining; }
            set { m_ShowUsesRemaining = value; InvalidateProperties(); }
        }

        public BaseAxe(int itemID)
            : base(itemID)
        {
            m_UsesRemaining = 150;
        }

        public BaseAxe(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (HarvestSystem == null)
                return;

            if (IsChildOf(from.Backpack) || Parent == from)
                HarvestSystem.BeginHarvesting(from, this);
            else
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (HarvestSystem != null)
                BaseHarvestTool.AddContextMenuEntries(from, this, list, HarvestSystem);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_ShowUsesRemaining);

            writer.Write((int)m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_ShowUsesRemaining = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_UsesRemaining = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (m_UsesRemaining < 1)
                            m_UsesRemaining = 150;

                        break;
                    }
            }
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
            // if (!Core.AOS && (attacker.Player || attacker.Body.IsHuman) && Layer == Layer.TwoHanded && (attacker.Skills[SkillName.Anatomy].Value / 400.0) >= Utility.RandomDouble())

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
                StatMod mod = defender.GetStatMod("Concussion");

                if (mod == null)
                {
                    defender.SendMessage("You receive a concussion blow!");
                    defender.AddStatMod(new StatMod(StatType.Int, "Concussion", -(defender.RawInt / 2), TimeSpan.FromSeconds(30.0)));

                    attacker.SendMessage("You deliver a concussion blow!");
                    attacker.PlaySound(0x308);
                }
            }
        }

    }
}
