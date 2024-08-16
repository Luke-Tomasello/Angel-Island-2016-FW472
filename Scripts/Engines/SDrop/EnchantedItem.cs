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

/* Scripts/Engines/SDrop/EnchantedItem.cs
 * ChangeLog:
 *	09/04/07, plasma
 *		Fixed bug with magical value calculation for jewellery
 *	11/10/05, erlein
 *		Altered GenerateiProps to set armor protection level to ('protection offset' + 1)
 *		as opposed to 'protection offset'.
 *	8/16/05, erlein
 *		Altered item id requirement condition so only uses BaseJewel & BaseClothing.
 *	8/16/05, erlein
 *		Added condition to make sure only clothing has item id as application
 *		requirement.
 *	8/11/05, erlein
 *		- Stripped out ID labels, added override for OnSingleClick() to localize labelling
 *		- Added conditions for BaseClothing & BaseJewel
 *		- Adapted so tinkering/tailoring items require item ID instead of lore
 *		- Added deserialization conditions to handle old label types
 *		- Added magic item level calc function (virtual).
 *	8/01/05, erlein
 *		Added a check for Pitchfork and BlackStaff types on deserialization
 *		to resolve Blacksmithing requirement to Tinkering.
 *	7/20/05, erlein
 *		- Altered storage of skill requirements and changed ints to shorts to
 *		reduce server load.
 *		- Moved out and expanded upon code to generate label from class name, now
 *		utilizes Misc/ClassNameTranslator.cs, cross referencing xml file
 *		Data/ClassTranslator.xml
 *	7/14/05, Kit
 *		Removed space from / between propertys.
 *	7/13/05, erlein
 *		Added Slayer property to iProps.
 *	7/13/05, erlein
 *		Initial creation.
 */

using Server.Misc;
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{

    public class EnchantedItem : Item
    {
        public Type ItemType;
        public string sApproxLabel;
        public int iMajorSkill;
        public int[] iProps;

        protected bool m_Identified;

        public virtual double SuccessAdjust
        {
            get
            {
                return 0.5;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Identified
        {
            get
            {
                return m_Identified;
            }
            set
            {
                m_Identified = value;
            }
        }

        // Holds skill requirements for using this item

        private int[] m_SkillReq;
        public int[] SkillReq
        {
            get
            {
                return m_SkillReq;
            }

            set
            {
                m_SkillReq = value;
            }
        }

        public EnchantedItem(int baseimage)
            : base(baseimage)
        {
        }

        public EnchantedItem(Serial serial)
            : base(serial)
        {
        }

        public virtual double MagicLevel
        {
            get
            {
                // Return weighted level, based on properties stored

                double level = 0.0;

                if (ItemType.IsSubclassOf(typeof(BaseWeapon)) || ItemType.IsSubclassOf(typeof(BaseArmor)))
                {

                    // Scale a level out of 5 using magic values of
                    // durability, protection/damage and accuracy (if it exists)

                    level = 5.0 *
                    (

                    ((iProps[0] / 5.0) * 0.8) +
                    ((iProps[1] / 5.0) * 0.15) +
                    ((iProps[2] > 0 ? ((iProps[2] / 5.0) * 0.05) : 0))

                    );
                }
                else
                {

                    // Scale based on the number of charges
                    // assume 30 is as hard as it gets
                    //
                    // 10 charges = (10/30 * 5) = 1.66
                    // 20 charges = (20/30 * 5) = 3.3
                    // 40 charges = (40/40 * 5) = 5

                    //pla: change 30 to 30.0 to prevent rounding
                    level = 5.0 * (iProps[1] / (iProps[1] <= 30 ? 30.0 : iProps[1]));
                }

                return (level);
            }
        }

        public virtual void GenerateiProps(object miref)
        {
            // Item type

            ItemType = miref.GetType();

            // Approximate label

            ClassNameTranslator cnt = new ClassNameTranslator();
            sApproxLabel = cnt.TranslateClass(ItemType);

            // Magical properties

            iProps = new int[4];

            // Default to blacksmithing requirement for enhancement

            iMajorSkill = (int)SkillName.Blacksmith;

            if (miref is BaseArmor)
            {
                BaseArmor ba = (BaseArmor)miref;

                // Stored    : Protection / Durability

                iProps[0] = ba.GetProtOffset() + 1;
                iProps[1] = (int)ba.Durability;
                iProps[2] = 0;

                // Figure out what kind of material was used

                if (ba.MaterialType == ArmorMaterialType.Leather ||
                    ba.MaterialType == ArmorMaterialType.Studded ||
                    ba.MaterialType == ArmorMaterialType.Bone)
                {
                    iMajorSkill = (int)SkillName.Tailoring;
                }
                else if (miref is WoodenShield)
                {
                    iMajorSkill = (int)SkillName.Carpentry;
                }

            }
            else if (miref is BaseWeapon)
            {
                BaseWeapon bw = (BaseWeapon)miref;

                // Stored    : Damage / Durability / Accuracy / Silver

                iProps[0] = (int)bw.DamageLevel;
                iProps[1] = (int)bw.DurabilityLevel;
                iProps[2] = (int)bw.AccuracyLevel;
                iProps[3] = (int)bw.Slayer;

                // If it's a bow or a staff, a carpenter made it.
                // Blackstaffs are made by tinkers.

                if (miref is BaseRanged || (miref is BaseStaff && !(miref is BlackStaff)) || miref is Club)
                    iMajorSkill = (int)SkillName.Carpentry;
                else if (miref is BlackStaff || miref is Pitchfork)
                    iMajorSkill = (int)SkillName.Tinkering;

            }
            else if (miref is BaseClothing)
            {
                // Store :
                // [0] - int representing the type of charge
                // [1] - int representing the number of charges

                BaseClothing bc = (BaseClothing)miref;

                iProps[0] = (int)bc.MagicType;
                iProps[1] = (int)bc.MagicCharges;

                iMajorSkill = (int)SkillName.Tailoring;
            }
            else if (miref is BaseJewel)
            {
                // Store :
                // [0] - int representing the type of charge
                // [1] - int representing the number of charges

                BaseJewel bj = (BaseJewel)miref;

                iProps[0] = (int)bj.MagicType;
                iProps[1] = (int)bj.MagicCharges;

                iMajorSkill = (int)SkillName.Tinkering;
            }
            else
            {
                // We've been passed an object that cannot be handled...
                // don't drop this enchanted item!

                this.Delete();
                return;
            }

            // Skill requirements

            SkillReq = new int[52];

            // all enhancements require these

            SkillReq[(int)SkillName.Magery] = 80;

            if (ItemType.IsSubclassOf(typeof(BaseJewel)) ||
                ItemType.IsSubclassOf(typeof(BaseClothing)))
                SkillReq[(int)SkillName.ItemID] = 80;
            else
                SkillReq[(int)SkillName.ArmsLore] = 80;

            // base final skill on what kind of object we're dealing with

            SkillReq[iMajorSkill] = 90;
        }

        public override void OnSingleClick(Mobile from)
        {
            // If it's not identified, leave... name is handled in EnchantedScroll

            if (!m_Identified)
                return;

            ArrayList attrs = new ArrayList();

            if (ItemType.IsSubclassOf(typeof(BaseWeapon)))
            {
                // Send damage, durability, accuracy and slayer info

                if (iProps[3] != (int)SlayerName.None)
                    attrs.Add(new EquipInfoAttribute(1017383 + iProps[3]));

                if (iProps[1] != (int)WeaponDurabilityLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038000 + iProps[1]));

                if (iProps[0] != (int)WeaponDamageLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038015 + iProps[0]));

                if (iProps[2] != (int)WeaponAccuracyLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038010 + iProps[2]));

            }
            else if (ItemType.IsSubclassOf(typeof(BaseArmor)))
            {
                // Send protection and durability info

                if (iProps[1] != (int)ArmorDurabilityLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038000 + iProps[1]));

                if (iProps[0] > (int)ArmorProtectionLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038005 + iProps[0]));

            }
            else if (ItemType.IsSubclassOf(typeof(BaseClothing)))
            {
                // Send charges and charge type info

                MagicEffect MagicType = (MagicEffect)iProps[0];

                switch (MagicType)
                {
                    case MagicEffect.MagicReflect:
                        attrs.Add(new EquipInfoAttribute(1044416, iProps[1])); // magic reflection
                        break;
                    case MagicEffect.Invisibility:
                        attrs.Add(new EquipInfoAttribute(1044424, iProps[1])); // invisibility
                        break;
                    case MagicEffect.Bless:
                        attrs.Add(new EquipInfoAttribute(1044397, iProps[1])); // bless
                        break;
                    case MagicEffect.Agility:
                        attrs.Add(new EquipInfoAttribute(1044389, iProps[1])); // agility
                        break;
                    case MagicEffect.Cunning:
                        attrs.Add(new EquipInfoAttribute(1044390, iProps[1])); // cunning
                        break;
                    case MagicEffect.Strength:
                        attrs.Add(new EquipInfoAttribute(1044396, iProps[1])); // strength
                        break;
                    case MagicEffect.NightSight:
                        attrs.Add(new EquipInfoAttribute(1044387, iProps[1])); // night sight
                        break;
                    case MagicEffect.Curse:
                        attrs.Add(new EquipInfoAttribute(1044407, iProps[1])); // curse
                        break;
                    case MagicEffect.Clumsy:
                        attrs.Add(new EquipInfoAttribute(1044382, iProps[1])); // clumsy
                        break;
                    case MagicEffect.Feeblemind:
                        attrs.Add(new EquipInfoAttribute(1044384, iProps[1])); // feeblemind
                        break;
                    case MagicEffect.Weakness:
                        attrs.Add(new EquipInfoAttribute(1044388, iProps[1])); // weaken
                        break;
                }
            }
            else if (ItemType.IsSubclassOf(typeof(BaseJewel)))
            {
                // Send charges and charge type info

                JewelMagicEffect MagicType = (JewelMagicEffect)iProps[0];

                switch (MagicType)
                {
                    case JewelMagicEffect.MagicReflect:
                        attrs.Add(new EquipInfoAttribute(1044416, iProps[1])); // magic reflection
                        break;
                    case JewelMagicEffect.Invisibility:
                        attrs.Add(new EquipInfoAttribute(1044424, iProps[1])); // invisibility
                        break;
                    case JewelMagicEffect.Bless:
                        attrs.Add(new EquipInfoAttribute(1044397, iProps[1])); // bless
                        break;
                    case JewelMagicEffect.Teleport:
                        attrs.Add(new EquipInfoAttribute(1044402, iProps[1])); // teleport
                        break;
                    case JewelMagicEffect.Agility:
                        attrs.Add(new EquipInfoAttribute(1044389, iProps[1])); // agility
                        break;
                    case JewelMagicEffect.Cunning:
                        attrs.Add(new EquipInfoAttribute(1044390, iProps[1])); // cunning
                        break;
                    case JewelMagicEffect.Strength:
                        attrs.Add(new EquipInfoAttribute(1044396, iProps[1])); // strength
                        break;
                    case JewelMagicEffect.NightSight:
                        attrs.Add(new EquipInfoAttribute(1044387, iProps[1])); // night sight
                        break;
                    case JewelMagicEffect.Curse:
                        attrs.Add(new EquipInfoAttribute(1044407, iProps[1])); // curse
                        break;
                    case JewelMagicEffect.Clumsy:
                        attrs.Add(new EquipInfoAttribute(1044382, iProps[1])); // clumsy
                        break;
                    case JewelMagicEffect.Feeblemind:
                        attrs.Add(new EquipInfoAttribute(1044384, iProps[1])); // feeblemind
                        break;
                    case JewelMagicEffect.Weakness:
                        attrs.Add(new EquipInfoAttribute(1044388, iProps[1])); // weaken
                        break;
                }
            }
            else
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(1041000, null, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));
            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((short)1); // version

            // Version 0

            // is the item identified?
            writer.Write(m_Identified);

            // Type of item
            writer.Write(ItemType.Name);

            // iProps
            writer.Write((short)iProps[0]);
            writer.Write((short)iProps[1]);
            writer.Write((short)iProps[2]);
            writer.Write((short)iProps[3]);

            // ID label
            // erl: Don't write this out ::::writer.Write( IDLabel )::::

            // Approx label
            writer.Write(sApproxLabel);

            // Skill stuff
            // iMajorSkill
            writer.Write((short)iMajorSkill);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            short version = reader.ReadShort();

            switch (version)
            {
                case 1:
                    goto case 0;

                case 0:
                    m_Identified = reader.ReadBool();

                    string sType = reader.ReadString();
                    ItemType = ScriptCompiler.FindTypeByName(sType);

                    // iProps
                    iProps = new int[4];

                    iProps[0] = (int)reader.ReadShort();
                    iProps[1] = (int)reader.ReadShort();
                    iProps[2] = (int)reader.ReadShort();
                    iProps[3] = (int)reader.ReadShort();

                    if (version < 1)
                        reader.ReadString();                // ID label

                    // Approx label
                    sApproxLabel = reader.ReadString();

                    // Skill stuff
                    SkillReq = new int[52];

                    iMajorSkill = (int)reader.ReadShort();

                    if (version < 1)
                    {
                        // Add new lines to name property
                        if (m_Identified)
                            this.Name = string.Format("an enchanted {0} scroll\n\n", sApproxLabel);

                        // Fix any BlackStaffs or Pitchforks
                        if (iMajorSkill == (int)SkillName.Blacksmith && (ItemType == typeof(BlackStaff) || ItemType == typeof(Pitchfork)))
                            iMajorSkill = (int)SkillName.Tinkering;
                    }

                    SkillReq[(int)SkillName.Magery] = 80;
                    SkillReq[iMajorSkill] = 90;

                    // Determine skill required :
                    if (ItemType.IsSubclassOf(typeof(BaseJewel)) ||
                        ItemType.IsSubclassOf(typeof(BaseClothing)))
                        SkillReq[(int)SkillName.ItemID] = 80;
                    else
                        SkillReq[(int)SkillName.ArmsLore] = 80;

                    break;
            }
        }
    }
}
