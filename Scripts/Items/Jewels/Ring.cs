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

/* Scripts/Items/Jewels/Ring.cs
 * CHANGELOG:
 * 3/11/2016, Adam,
 *	-- teleport ring --
 *	o remove explicit clearing of magic type when charges reaches zero. None of the other magic items do this.
 *	o cleanup the region check fo teleport ring
 * 06/26/06, Kit
 *	Added msg to fail of teleport items because of region setting "The magic normally within this object seems absent."
 * 9/23/05, Adam
 *	Add SpellHelper and Region checks so that teleport rings follow the same rules
 *	as the spell.	
 * 05/11/2004 - Pulse
 * 	Added OnDoubleClick routine with supporting InternalTarget class and Target routine 
 * 	to support the teleport spell if the ring is a teleport ring with charges  
 */

using Server.Mobiles;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public abstract class BaseRing : BaseJewel
    {
        public override int BaseGemTypeNumber { get { return 1044176; } } // star sapphire ring

        public BaseRing(int itemID)
            : base(itemID, Layer.Ring)
        {
        }

        public BaseRing(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            Container pack = from.Backpack;
            if (pack != null)
            {
                if (!IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    return;
                }

                if (MagicType == JewelMagicEffect.Teleport && MagicCharges > 0)
                {
                    if (from is PlayerMobile)
                    {
                        from.Target = new InternalTarget(this);
                    }
                }
                else
                {
                    from.SendMessage("This item is not used in that way.");
                }
            }
        }

        public void Target(Mobile from, IPoint3D p)
        {
            IPoint3D orig = p;
            Map map = from.Map;

            SpellHelper.GetSurfaceTop(ref p);

            if (Server.Misc.WeightOverloading.IsOverloaded(from))
            {   // Thou art too encumbered to move.
                from.SendLocalizedMessage(502359, "", 0x22);
            }
            else if (from.Region.OnBeginSpellCast(from, SpellRegistry.NewSpell(21, from, null)) == false)
            {   // check to region to see if the teleport SPELL is allowed.
                from.SendMessage("The magic normally within this object seems absent.");
            }   // if it is not, then the teleport ring should not be allowed.
            else if (!SpellHelper.CheckTravel(from, TravelCheckType.TeleportFrom))
            {   // make teleport rings follow the same rules as the spell
            }
            else if (!SpellHelper.CheckTravel(from, map, new Point3D(p), TravelCheckType.TeleportTo))
            {   // make teleport rings follow the same rules as the spell
            }
            else if (map == null || !map.CanSpawnMobile(p.X, p.Y, p.Z))
            {
                from.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (SpellHelper.CheckMulti(new Point3D(p), map))
            {
                from.SendLocalizedMessage(501942); // That location is blocked.
            }
            else
            {
                SpellHelper.Turn(from, orig);

                Mobile m = from;

                Point3D frompoint = m.Location;
                Point3D topoint = new Point3D(p);

                m.Location = topoint;
                m.ProcessDelta();

                Effects.SendLocationParticles(EffectItem.Create(frompoint, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                Effects.SendLocationParticles(EffectItem.Create(topoint, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

                m.PlaySound(0x1FE);
                MagicCharges--;
                m.RevealingAction();
            }
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

        public class InternalTarget : Target
        {
            private BaseRing m_Owner;

            public InternalTarget(BaseRing owner)
                : base(12, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                IPoint3D p = o as IPoint3D;

                if (p != null)
                    m_Owner.Target(from, p);
            }
        }
    }

    public class GoldRing : BaseRing
    {
        [Constructable]
        public GoldRing()
            : base(0x108a)
        {
            Weight = 0.1;
        }

        public GoldRing(Serial serial)
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

    public class SilverRing : BaseRing
    {
        [Constructable]
        public SilverRing()
            : base(0x1F09)
        {
            Weight = 0.1;
        }

        public SilverRing(Serial serial)
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
