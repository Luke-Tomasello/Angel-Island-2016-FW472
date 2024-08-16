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

/* Items/Deeds/StorageTaxCredits.cs
 * ChangeLog:
 *	05/5/07, Adam
 *      first time checkin
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class StorageTaxCredits : Item // Create the item class which is derived from the base item class
    {
        private ushort m_Credits;
        public ushort Credits
        {
            get
            {
                return m_Credits;
            }
        }

        [Constructable]
        public StorageTaxCredits()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "tax credits: storage";
            LootType = LootType.Regular;

            // 30 credits: Cost is 1K each and decays at 1 per day
            m_Credits = 30 * 24;
        }

        public StorageTaxCredits(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteInt32((int)0); // version

            writer.Write(m_Credits);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt32();

            m_Credits = reader.ReadUShort();
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Backpack == null || !IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Please target the house sign of the house to apply credits to.");
                from.Target = new StorageTaxCreditsTarget(this); // Call our target
            }
        }
    }

    public class StorageTaxCreditsTarget : Target
    {
        private StorageTaxCredits m_Deed;

        public StorageTaxCreditsTarget(StorageTaxCredits deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is HouseSign && (target as HouseSign).Structure != null)
            {
                HouseSign sign = target as HouseSign;

                if (sign.Structure.IsFriend(from) == false)
                {
                    from.SendLocalizedMessage(502094); // You must be in your house to do this.
                    return;
                }

                if (sign.Structure.CanAddStorageCredits(m_Deed.Credits) == false)
                {
                    from.SendMessage("That house cannot hold more credits.");
                    return;
                }

                sign.Structure.StorageTaxCredits += (uint)m_Deed.Credits;
                from.SendMessage("Your total storage credits are {0}.", sign.Structure.StorageTaxCredits);
                m_Deed.Delete(); // Delete the deed                
            }
            else
            {
                from.SendMessage("That is not a house sign.");
            }
        }
    }
}
