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

/* Items/Deeds/BuildingUpgradeContracts.cs
 * ChangeLog:
 *	05/8/07, Adam
 *      first time checkin
 */

using Server.Diagnostics;
using Server.Commands;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Items
{
    public abstract class BaseUpgradeContract : Item
    {
        private uint m_LockdownData;

        public BaseUpgradeContract()
            : base(0x14F0)
        {
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public BaseUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public uint Lockdowns
        {
            get { return Utility.GetUIntRight16(m_LockdownData); }
            set { Utility.SetUIntRight16(ref m_LockdownData, value); }
        }

        public uint Secures
        {
            get { return Utility.GetUIntByte3(m_LockdownData); }
            set { Utility.SetUIntByte3(ref m_LockdownData, value); }
        }

        public uint LockBoxes
        {
            get { return Utility.GetUIntByte4(m_LockdownData); }
            set { Utility.SetUIntByte4(ref m_LockdownData, value); }
        }

        public abstract uint Price { get; }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteInt32((int)0);       // version

            writer.WriteUInt32(m_LockdownData); // version 0

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt32();
            switch (version)
            {
                case 0:
                    {
                        m_LockdownData = reader.ReadUInt32();
                        break;
                    }
            }
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
                from.SendMessage("Please target the house sign of the house to apply upgrade to.");
                from.Target = new UpgradeContractTarget(this); // Call our target
            }
        }
    }

    public class UpgradeContractTarget : Target
    {
        private BaseUpgradeContract m_Deed;

        public UpgradeContractTarget(BaseUpgradeContract deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        bool UpgradeCheck(Mobile from, BaseHouse house)
        {
            // extra taxable lockbox purchases
            bool Purchased = house.MaxLockBoxes > house.LockBoxFloor;
            bool Placed = house.LockBoxCount > house.LockBoxFloor;

            // see if the upgrade is really an *upgrade*
            //  handle the special case where the user has added taxable lockbox storage below
            if (house.MaxLockDowns >= m_Deed.Lockdowns || house.MaxSecures >= m_Deed.Secures || house.LockBoxFloor >= m_Deed.LockBoxes)
            {
                from.SendMessage("You may not add-to or downgrade your existing storage.");
                return false;
            }

            // have the purchased but not placed taxable storage?
            if (Purchased)
            {
                from.SendMessage("You have purchased taxable lockbox storage.");
                from.SendMessage("Your storage will be upgraded.");
            }

            return true;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is HouseSign && (target as HouseSign).Structure != null)
            {
                HouseSign sign = target as HouseSign;
                LogHelper Logger = null;

                try
                {
                    if (sign.Structure.IsFriend(from) == false)
                    {
                        from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        return;
                    }
                    else if (UpgradeCheck(from, (target as HouseSign).Structure) == false)
                    {
                        // filters out any oddball cases and askes the user to correct it
                    }
                    else
                    {
                        BaseHouse house = (target as HouseSign).Structure;
                        Logger = new LogHelper("StorageUpgrade.log", false);
                        Logger.Log(LogType.Item, house, String.Format("Upgraded with: {0}", m_Deed.ToString()));
                        house.MaxLockDowns = (int)m_Deed.Lockdowns;
                        house.MaxSecures = (int)m_Deed.Secures;
                        // give the deeds lockboxes PLUS any taxable lockboxes thay may have purchased.
                        house.MaxLockBoxes = (int)m_Deed.LockBoxes + (int)(house.MaxLockBoxes - house.LockBoxFloor);
                        house.LockBoxFloor = m_Deed.LockBoxes;
                        house.LockBoxCeling = m_Deed.LockBoxes * 2;
                        house.UpgradeCosts += m_Deed.Price;
                        from.SendMessage(String.Format("Upgrade complete with: {0} lockdowns, {1} secures, and {2} lockboxes.", house.MaxLockDowns, house.MaxSecures, house.MaxLockBoxes));
                        m_Deed.Delete();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
                finally
                {
                    if (Logger != null)
                        Logger.Finish();
                }
            }
            else
            {
                from.SendMessage("That is not a house sign.");
            }
        }
    }

    /*
	 * Class 1     500     3     3     82,562   - modest 
	 * Class 2     900     6     4     195,750  - moderate 
	 * Class 3     1300    9     5     498,900  - premium 
	 * Class 4     1950    14    7     767,100  - extravagant 
	 */
    public class ModestUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public ModestUpgradeContract()
        {
            Name = "a modest storage upgrade contract";
            Lockdowns = 500;
            Secures = 3;
            LockBoxes = 3;
        }

        public ModestUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 82562; } }

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

    }

    public class ModerateUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public ModerateUpgradeContract()
        {
            Name = "a moderate storage upgrade contract";
            Lockdowns = 900;
            Secures = 6;
            LockBoxes = 4;
        }

        public ModerateUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 195750; } }

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

    }

    public class PremiumUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public PremiumUpgradeContract()
        {
            Name = "a premium storage upgrade contract";
            Lockdowns = 1300;
            Secures = 9;
            LockBoxes = 5;
        }

        public PremiumUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 498900; } }

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

    }

    public class ExtravagantUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public ExtravagantUpgradeContract()
        {
            Name = "an extravagant storage upgrade contract";
            Lockdowns = 1950;
            Secures = 14;
            LockBoxes = 7;
        }

        public ExtravagantUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 767100; } }

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

    }
}
