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

/* Scripts/Engines/SDrop/SDrop.cs
 * ChangeLog:
 *	3/3/09, Adam
 *		Change it so that Guarding armor or better has a chance at being an SDrop.
 *		This is because the roomer is that Guarding or better armor is as good as GM.
 *  11/9/08, Adam
 *      Replace old MaxHits and Hits with MaxHitPoints and HitPoints (RunUO 2.0 compatibility)
 *  12/21/06, Kit
 *      Added in check for requireing clothing to enhance to be exceptional.
 *	7/10/05, erlein
 *		Made application of sdrop scroll de-newbify any clothing.	
 *	8/18/05, erlein
 *		Altered so requires 10 or more charges for anything sdrop'd other than
 *		invis, bless & teleport... which now require 6 charges or more.
 *	8/18/05, erlein
 *		Removed extra condition to handle rings and bracelets
 *		(now we have these as craftable, so no longer an issue).
 *		Added exceptional requirement for enchantment of jewellery items.
 *	8/11/05, erlein
 *		- Linked magic item level function in place of formula.
 *		- Added necessary code to handle BaseJewel and BaseClothing types.
 *	8/1/05, erlein
 *		Added a check for orc helms, set to not drop as they are non craftable.
 *		Removed separate condition for BaseShield on exception item check
 *		and tidied into one if() statement.
 *	7/14/05, Kit
 &		Added exceptional item quality check for basearmor/baseweapon/baseshield.
 *	7/13/05, erlein
 *		Added Slayer property to scroll application method.
 *		Fixed ArmorProtectionLevel transfer.
 *		Removed MaterialType limitation to metal in SDropTest().
 *		Changed mentions of "enhance" to "enchant" in all messages.
 *	7/13/05, erlein
 *		Initial creation.
 */

using Server.Items;
using System;

namespace Server.Engines
{
    // Handles drop instance - all calcs, the actual enhancement and
    // any future engine type functionality performed here

    public class SDrop
    {
        // Test item vs. chance

        public static bool SDropTest(Item item, double chance)
        {
            bool TrySDrop = false;

            if (item is OrcHelm)
                return false;

            if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                // adam: Medwin says that armor > guarding is as good as GM, so we want to reduce thos by requiring
                //	that they become SDrops (Sdrops involve a GM)
                if (armor.ProtectionLevel >= ArmorProtectionLevel.Guarding)
                    TrySDrop = true;

                // Make sure level 3 armor or higher
                //if( armor.GetProtOffset() >= 3 )
                //TrySDrop = true;
            }
            else if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                // Make sure level 3 weapon or higher
                if (((int)weapon.DamageLevel) >= 3)
                    TrySDrop = true;
            }
            else if (item is BaseClothing)
            {
                BaseClothing bc = (BaseClothing)item;
                int iMagicEffect = (int)bc.MagicType;

                // Make sure it's got at least 6 charges
                // for invis & bless, 10 for the others

                if ((bc.MagicCharges >= 6 &&
                    (iMagicEffect == 2 || iMagicEffect == 3)
                    ) || bc.MagicCharges >= 10)
                    TrySDrop = true;

            }
            else if (item is BaseJewel)
            {
                BaseJewel bj = (BaseJewel)item;
                int iMagicEffect = (int)bj.MagicType;

                // Make sure it's got at least 6 charges
                // for invis, bless, teleport, 10 for the others

                if ((bj.MagicCharges >= 6 &&
                    (iMagicEffect == 2 || iMagicEffect == 3 || iMagicEffect == 4)
                    ) || bj.MagicCharges >= 10)
                    TrySDrop = true;
            }

            if (TrySDrop && Utility.RandomDouble() < chance)
                return true;

            return false;
        }

        // Person performing the enhancement
        private Mobile m_Enhancer;
        public Mobile Enhancer
        {
            get
            {
                return m_Enhancer;
            }

            set
            {
                m_Enhancer = value;
            }
        }

        // The item which is being used to attempt the enhancement
        private EnchantedItem m_EItem;
        public EnchantedItem EItem
        {
            get
            {
                return m_EItem;
            }
            set
            {
                m_EItem = value;
            }
        }

        // Item attempting to be enhanced
        private Item m_TargetItem;
        public Item TargetItem
        {
            get
            {
                return m_TargetItem;
            }

            set
            {
                m_TargetItem = value;
            }
        }

        // SDrop construct
        public SDrop(EnchantedItem eitem, Item target, Mobile enhancer)
        {
            m_TargetItem = target;
            m_EItem = eitem;
            m_Enhancer = enhancer;
        }

        // Determines whether person can perform enhancement, sends appropriate messages to person if they cannot
        public bool CanEnhance()
        {
            Type TargetType = TargetItem.GetType();

            // 1) Make sure the item type is correct
            if (EItem.ItemType != TargetType)
            {
                m_Enhancer.SendMessage("This scroll cannot be used with that kind of item.");
                return false;
            }

            // 1) Check that the item is PlayerCrafted
            if (!TargetItem.PlayerCrafted)
            {
                m_Enhancer.SendMessage("The item you are enchanting must have been hand crafted.");
                return false;
            }

            if ((TargetItem is BaseWeapon && ((BaseWeapon)TargetItem).Quality != WeaponQuality.Exceptional) ||
                    (TargetItem is BaseArmor && ((BaseArmor)TargetItem).Quality != ArmorQuality.Exceptional) ||
                    (TargetItem is BaseJewel && ((BaseJewel)TargetItem).Quality != JewelQuality.Exceptional) ||
                    (TargetItem is BaseClothing && ((BaseClothing)TargetItem).Quality != ClothingQuality.Exceptional)
                )
            {
                m_Enhancer.SendMessage("You feel the quality of this item is not worthy of enchantment.");
                return false;
            }

            // 2) Require skills

            string sError = "";
            string sPassed = "";

            for (int i = 0; i < 52; i++)
            {
                if (m_Enhancer.Skills[i].Value < m_EItem.SkillReq[i])
                {
                    sError += (sError != "" ? ", " : "") + (string.Format("{0} {1}", m_EItem.SkillReq[i], m_Enhancer.Skills[i].Name));
                }
                else if (m_EItem.SkillReq[i] > 0)
                    sPassed += (sPassed != "" ? ", " : "") + m_Enhancer.Skills[i].Name;

            }

            if (sError != "")
            {
                m_Enhancer.SendMessage("You do not have the skills necessary to perform this enchantment. It requires {0}{1}", sError, (sPassed != "" ? " You do, however, have sufficient " + sPassed : ""));
                return false;
            }

            // 3) Make sure item we're enhancing is brand new
            int iHits = 0, iMaxHits = 0;

            if (TargetItem is BaseWeapon)
            {
                iHits = ((BaseWeapon)TargetItem).HitPoints;
                iMaxHits = ((BaseWeapon)TargetItem).MaxHitPoints;
            }
            else if (TargetItem is BaseArmor)
            {
                iHits = ((BaseArmor)TargetItem).HitPoints;
                iMaxHits = ((BaseArmor)TargetItem).MaxHitPoints;
            }

            if (iHits < iMaxHits)
            {
                m_Enhancer.SendMessage("The item you are trying to enchant appears to be damaged...");
                return false;
            }

            return true;
        }

        // Returns calculated chance of enhancement, based on item, mobile and enchanted item
        // 0.6 = 60% chance of success
        // 1.0 = 100% chance of success
        //
        public double EnhanceChance()
        {
            /*	Create scaling based on iProps :

				Factor for 3xGM @ vanq/invul for 60% success (0.6)

				100 = 0.2
				100 = 0.2
				100 = 0.2

				Multiply each skill by x / (level * 100) ?
				where x is determined by CommandConsole, defaulting to 1

				level 2 :
				100% success

				level 3 :
				99% success

				level 4 :
				75% success

				level 5 :
				60% success

				If it was set to 1.1, for example, level 5 would then be 66%

			*/

            double chance = 0.0;
            double adjuster = m_EItem.SuccessAdjust;
            double itemlevel = m_EItem.MagicLevel;

            // Get the top 3 skills and adjust the item level to produce chance

            int[] SortedSkills = m_EItem.SkillReq;

            int[] OriginPos = new int[52];
            for (int i = 0; i < 52; i++)
                OriginPos[i] = i;

            for (int i = 0; i < 52; i++)
            {
                for (int ib = 0; ib < 51; ib++)
                {
                    if (SortedSkills[i] > SortedSkills[ib])
                    {
                        int temp = SortedSkills[i];

                        SortedSkills[i] = SortedSkills[ib];
                        SortedSkills[ib] = temp;

                        int ortemp = OriginPos[i];

                        OriginPos[i] = OriginPos[ib];
                        OriginPos[ib] = ortemp;
                    }
                }
            }

            chance += (m_Enhancer.Skills[OriginPos[0]].Value * adjuster / (itemlevel * 100));
            chance += (m_Enhancer.Skills[OriginPos[1]].Value * adjuster / (itemlevel * 100));
            chance += (m_Enhancer.Skills[OriginPos[2]].Value * adjuster / (itemlevel * 100));

            return chance;
        }

        // Performs the enhancement

        public void DoEnhance()
        {
            // Enhance TargetItem with EItem properties... we know it can be done, this
            // function is the final stage

            if (TargetItem is BaseArmor)
            {
                BaseArmor ba = (BaseArmor)TargetItem;

                // ProtectionLevel, Durability

                ba.ProtectionLevel = (ArmorProtectionLevel)(EItem.iProps[0]);
                ba.Durability = (ArmorDurabilityLevel)EItem.iProps[1];
                ba.Identified = true;
                ba.Quality = ArmorQuality.Regular;
            }
            else if (TargetItem is BaseWeapon)
            {
                BaseWeapon bw = (BaseWeapon)TargetItem;

                // DamageLevel / Durability / Accuracy Level

                bw.DamageLevel = (WeaponDamageLevel)EItem.iProps[0];
                bw.DurabilityLevel = (WeaponDurabilityLevel)EItem.iProps[1];
                bw.AccuracyLevel = (WeaponAccuracyLevel)EItem.iProps[2];
                bw.Slayer = (SlayerName)EItem.iProps[3];
                bw.Identified = true;
                bw.Quality = WeaponQuality.Regular;
            }
            else if (TargetItem is BaseClothing)
            {
                BaseClothing bc = (BaseClothing)TargetItem;

                // MagicEffect / MagicCharges

                bc.MagicType = (MagicEffect)EItem.iProps[0];
                bc.MagicCharges = EItem.iProps[1];

                // It's probably newbified from the initial craft, we want these to drop

                bc.LootType = LootType.Regular;
            }
            else if (TargetItem is BaseJewel)
            {
                BaseJewel bj = (BaseJewel)TargetItem;

                // JewelMagicEffect / MagicCharges

                bj.MagicType = (JewelMagicEffect)EItem.iProps[0];
                bj.MagicCharges = EItem.iProps[1];
            }

            // Done with the scroll!
            ((Item)EItem).Delete();
        }

        // Destroys both the scroll and the item

        public void DoFailure()
        {
            ((Item)TargetItem).Delete();
            ((Item)EItem).Delete();
        }
    }
}
