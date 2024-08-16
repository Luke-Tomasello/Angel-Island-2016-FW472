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

/* Items/Shields/OrderShield.cs
 * CHANGELOG:
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Guilds;

namespace Server.Items
{
    public class OrderShield : BaseShield
    {

        public override int InitMinHits { get { return 100; } }
        public override int InitMaxHits { get { return 125; } }

        public override int AosStrReq { get { return 95; } }

        public override int ArmorBase { get { return 30; } }

        [Constructable]
        public OrderShield()
            : base(0x1BC4)
        {
            if (!Core.AOS)
                LootType = LootType.Newbied;

            Weight = 7.0;
        }

        public OrderShield(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (Weight == 6.0)
                Weight = 7.0;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);//version
        }

        public override bool OnEquip(Mobile from)
        {
            return Validate(from) && base.OnEquip(from);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Validate(Parent as Mobile))
                base.OnSingleClick(from);
        }

        public bool Validate(Mobile m)
        {
            if (Core.AOS || m == null || !m.Player || m.AccessLevel != AccessLevel.Player)
                return true;

            Guild g = m.Guild as Guild;

            if (g == null || g.Type != GuildType.Order)
            {
                m.FixedEffect(0x3728, 10, 13);
                Delete();

                return false;
            }

            return true;
        }
    }
}
