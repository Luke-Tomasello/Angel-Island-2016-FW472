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

//  /Scripts/Mobiles/Vendors/NPC/Weaver.cs
//	CHANGE LOG
//  03/29/2004 - Pulse
//		Removed ability for this NPC vendor to support Bulk Order Deeds
//		Weavers will no longer issue or accept these deeds.

using Server.Engines.BulkOrders;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Weaver : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.TailorsGuild; } }

        [Constructable]
        public Weaver()
            : base("the weaver")
        {
            SetSkill(SkillName.Tailoring, 65.0, 88.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBWeaver());
        }

        public override VendorShoeType ShoeType
        {
            get { return VendorShoeType.Sandals; }
        }

        #region Bulk Orders
        public override Item CreateBulkOrder(Mobile from, bool fromContextMenu)
        {
            PlayerMobile pm = from as PlayerMobile;

            if (pm != null && pm.NextTailorBulkOrder == TimeSpan.Zero && (fromContextMenu || 0.2 > Utility.RandomDouble()))
            {
                double theirSkill = pm.Skills[SkillName.Tailoring].Base;

                if (theirSkill >= 70.1)
                    pm.NextTailorBulkOrder = TimeSpan.FromHours(6.0);
                else if (theirSkill >= 50.1)
                    pm.NextTailorBulkOrder = TimeSpan.FromHours(2.0);
                else
                    pm.NextTailorBulkOrder = TimeSpan.FromHours(1.0);

                if (theirSkill >= 70.1 && ((theirSkill - 40.0) / 300.0) > Utility.RandomDouble())
                    return new LargeTailorBOD();

                return SmallTailorBOD.CreateRandomFor(from);
            }

            return null;
        }

        public override bool IsValidBulkOrder(Item item)
        {
            return (item is SmallTailorBOD || item is LargeTailorBOD);
        }

        public override bool SupportsBulkOrders(Mobile from)
        {
            // The following line allows this NPC to support the BOD system. 
            //return ( from is PlayerMobile && from.Skills[SkillName.Tailoring].Base > 0 );
            // return false from this function to disable BOD support.
            return false;
        }

        public override TimeSpan GetNextBulkOrder(Mobile from)
        {
            if (from is PlayerMobile)
                return ((PlayerMobile)from).NextTailorBulkOrder;

            return TimeSpan.Zero;
        }
        #endregion

        public Weaver(Serial serial)
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
