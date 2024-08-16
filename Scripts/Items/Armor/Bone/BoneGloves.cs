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

/* Scripts/Items/Armor/Bone/BoneGloves.cs
 * ChangeLog:
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 15 lines removed.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *  1/23/05, Froste
 *  Add Bone Magi variant of this Bone piece. Meddable, AR like leather, Exceptional.
 *	9/10/04, Pigpen
 *	Add Unholy Bone variant of this Bone piece.
 */

using Server.Network;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute(0x1450, 0x1455)]
    public class BoneGloves : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        public override int AosStrReq { get { return 55; } }
        public override int OldStrReq { get { return 40; } }

        public override int OldDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 30; } }
        public override int RevertArmorBase { get { return 2; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public BoneGloves()
            : base(0x1450)
        {
            Weight = 2.0;
        }

        public BoneGloves(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 2.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class UnholyBoneGloves : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        public override int AosStrReq { get { return 55; } }
        public override int OldStrReq { get { return 40; } }

        public override int OldDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 30; } }
        public override int RevertArmorBase { get { return 2; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public UnholyBoneGloves()
            : base(0x1450)
        {
            Weight = 2.0;
            Hue = 1109;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            Durability = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Unholy Bone Gloves";
        }

        public UnholyBoneGloves(Serial serial)
            : base(serial)
        {
        }

        // Special version that DOES NOT show armor attributes and tags
        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));

        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 2.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class BoneMagiGloves : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        public override int AosStrReq { get { return 55; } }
        public override int OldStrReq { get { return 40; } }

        // public override int OldDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 13; } }
        public override int RevertArmorBase { get { return 2; } }

        public override ArmorMeditationAllowance DefMedAllowance { get { return ArmorMeditationAllowance.All; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public BoneMagiGloves()
            : base(0x1450)
        {
            Weight = 2.0;
            Quality = ArmorQuality.Exceptional;
            Name = "Gloves of the Bone Magi";
        }

        public BoneMagiGloves(Serial serial)
            : base(serial)
        {
        }

        // Special version that DOES NOT show armor attributes and tags
        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));

        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 2.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
