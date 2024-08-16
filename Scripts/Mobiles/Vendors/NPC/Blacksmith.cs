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

/*	Scripts/Mobiles/Vendors/NPC/Blacksmith.cs
 *	CHANGELOG
 * 	03/29/2004 - Pulse
 * 		Removed ability for this NPC vendor to support Bulk Order Deeds
 * 		Blacksmiths will no longer issue or accept these deeds.
 */

using Server.Engines.BulkOrders;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Blacksmith : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.BlacksmithsGuild; } }

        [Constructable]
        public Blacksmith()
            : base("the blacksmith")
        {
            SetSkill(SkillName.ArmsLore, 36.0, 68.0);
            SetSkill(SkillName.Blacksmith, 65.0, 88.0);
            SetSkill(SkillName.Fencing, 60.0, 83.0);
            SetSkill(SkillName.Macing, 61.0, 93.0);
            SetSkill(SkillName.Swords, 60.0, 83.0);
            SetSkill(SkillName.Tactics, 60.0, 83.0);
            SetSkill(SkillName.Parry, 61.0, 93.0);
        }

        public override void InitSBInfo()
        {
            /*m_SBInfos.Add(new SBAxeWeapon());
			m_SBInfos.Add(new SBKnifeWeapon());
			m_SBInfos.Add(new SBMaceWeapon());
			m_SBInfos.Add(new SBSmithTools());
			m_SBInfos.Add(new SBPoleArmWeapon());
			m_SBInfos.Add(new SBSpearForkWeapon());
			m_SBInfos.Add(new SBSwordWeapon());

			m_SBInfos.Add(new SBMetalShields());

			m_SBInfos.Add(new SBHelmetArmor());
			m_SBInfos.Add(new SBPlateArmor());
			m_SBInfos.Add(new SBChainmailArmor());
			m_SBInfos.Add(new SBRingmailArmor());
			m_SBInfos.Add(new SBStuddedArmor());
			m_SBInfos.Add(new SBLeatherArmor());*/

            m_SBInfos.Add(new SBBlacksmith());
        }

        public override VendorShoeType ShoeType
        {
            get { return VendorShoeType.None; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = (Utility.RandomBool() ? null : new Server.Items.RingmailChest());

            if (item != null && !EquipItem(item))
            {
                item.Delete();
                item = null;
            }

            if (item == null)
                AddItem(new Server.Items.FullApron());

            AddItem(new Server.Items.Bascinet());
            AddItem(new Server.Items.SmithHammer());
        }

        #region Bulk Orders
        public override Item CreateBulkOrder(Mobile from, bool fromContextMenu)
        {
            PlayerMobile pm = from as PlayerMobile;

            if (pm != null && pm.NextSmithBulkOrder == TimeSpan.Zero && (fromContextMenu || 0.2 > Utility.RandomDouble()))
            {
                double theirSkill = pm.Skills[SkillName.Blacksmith].Base;

                if (theirSkill >= 70.1)
                    pm.NextSmithBulkOrder = TimeSpan.FromHours(6.0);
                else if (theirSkill >= 50.1)
                    pm.NextSmithBulkOrder = TimeSpan.FromHours(2.0);
                else
                    pm.NextSmithBulkOrder = TimeSpan.FromHours(1.0);

                if (theirSkill >= 70.1 && ((theirSkill - 40.0) / 300.0) > Utility.RandomDouble())
                    return new LargeSmithBOD();

                return SmallSmithBOD.CreateRandomFor(from);
            }

            return null;
        }

        public override bool IsValidBulkOrder(Item item)
        {
            return (item is SmallSmithBOD || item is LargeSmithBOD);
        }

        public override bool SupportsBulkOrders(Mobile from)
        {
            // The following line allows this NPC to support the BOD system. 
            //return ( from is PlayerMobile && from.Skills[SkillName.Blacksmith].Base > 0 );
            // return false from this function to disable BOD support.
            return false;
        }

        public override TimeSpan GetNextBulkOrder(Mobile from)
        {
            if (from is PlayerMobile)
                return ((PlayerMobile)from).NextSmithBulkOrder;

            return TimeSpan.Zero;
        }
        #endregion

        public Blacksmith(Serial serial)
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
