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

/* Scripts\Items\Skill Items\Tailor Items\Dyetubs\PotionKegDyeTub.cs
 * CHANGELOG:
 *	4/30/05 - Pix
 *		Assigned Name property so that it will show up properly in a vendor's list.
 *	9/29/04 - Pixie
 *		Initial Version
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class PotionKegDyeTub : DyeTub
    {
        public override bool AllowDyables { get { return false; } }
        public override bool AllowRunebooks { get { return false; } }

        private int m_Uses;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get { return m_Uses; }
            set { m_Uses = value; }
        }

        [Constructable]
        public PotionKegDyeTub()
        {
            m_Uses = 10;
            Name = "potion keg dye tub";
        }

        public override void OnSingleClick(Mobile from)
        {
            this.LabelTo(from, "potion keg dye tub");
            this.LabelTo(from, string.Format("{0} uses remaining", this.m_Uses));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage("Target the potion keg to paint.");
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public PotionKegDyeTub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Uses);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Uses = reader.ReadInt();
                        break;
                    }
            }
        }


        private class InternalTarget : Target
        {
            private PotionKegDyeTub m_Tub;

            public InternalTarget(PotionKegDyeTub tub)
                : base(1, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is PotionKeg)
                {
                    Item item = (Item)targeted;

                    if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                    {
                        from.SendLocalizedMessage(500446); // That is too far away.
                    }
                    else
                    {
                        bool okay = (item.IsChildOf(from.Backpack));

                        if (!okay)
                        {
                            if (item.Parent == null)
                            {
                                BaseHouse house = BaseHouse.FindHouseAt(item);

                                if (house == null || !house.IsLockedDown(item))
                                    from.SendMessage("The potion keg must be locked down to paint it.");
                                else if (!house.IsCoOwner(from))
                                    from.SendLocalizedMessage(501023); // You must be the owner to use this item.
                                else
                                    okay = true;
                            }
                            else
                            {
                                from.SendMessage("The potion keg must be in your backpack to be painted.");
                            }
                        }

                        if (okay)
                        {
                            m_Tub.m_Uses--;
                            item.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);

                            if (m_Tub.m_Uses <= 0)
                            {
                                m_Tub.Delete();
                            }
                        }
                    }
                }
                else
                {
                    from.SendMessage("That is not a potion keg.");
                }
            }
        }


    }
}
