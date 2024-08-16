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

/* Items/Deeds/LockboxBuildingPermit.cs
 * ChangeLog:
 *	05/5/07, Adam
 *      first time checkin
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class LockboxBuildingPermit : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public LockboxBuildingPermit()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "building permit: lockbox";
            LootType = LootType.Regular;
        }

        public LockboxBuildingPermit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteInt32((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt32();
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
                from.SendMessage("Please target the house sign of the house to build on.");
                from.Target = new LockboxBuildingPermitTarget(this); // Call our target
            }
        }
    }

    public class LockboxBuildingPermitTarget : Target
    {
        private LockboxBuildingPermit m_Deed;

        public LockboxBuildingPermitTarget(LockboxBuildingPermit deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is HouseSign && (target as HouseSign).Structure != null)
            {
                HouseSign sign = target as HouseSign;

                if (sign.Structure.IsOwner(from) == false)
                {
                    from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                    return;
                }

                if (sign.Structure.CanAddLockbox == false)
                {
                    from.SendMessage("That house cannot hold more lockboxes.");
                    return;
                }

                // 5 free credits: decays at 1 per day
                ushort freeCredits = 5 * 24;
                if (sign.Structure.CanAddStorageCredits(freeCredits) == true)
                    sign.Structure.StorageTaxCredits += freeCredits;

                // add the lockbox
                sign.Structure.MaxLockBoxes++;
                from.SendMessage("This house now allows {0} lockboxes.", sign.Structure.MaxLockBoxes);
                m_Deed.Delete(); // Delete the deed                
            }
            else
            {
                from.SendMessage("That is not a house sign.");
            }
        }
    }
}
