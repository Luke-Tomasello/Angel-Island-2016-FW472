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

/* Scripts/Engines/Crafting/DefAlchemy.cs
 * ChangeLog:
 *	10/15/05, erlein
 *		Re-worked special dye handling to accommodate new dye tub based craft model.
 *	10/15/05, erlein
 *		Added special dyes.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Items;
using System;

namespace Server.Engines.Craft
{
    public class DefAlchemy : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Alchemy; }
        }

        public override int GumpTitleNumber
        {
            get { return 1044001; } // <CENTER>ALCHEMY MENU</CENTER>
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefAlchemy();

                return m_CraftSystem;
            }
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.0; // 0%
        }

        private DefAlchemy()
            : base(1, 1, 1.25)// base( 1, 1, 3.1 )
        {
        }

        public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.

            return 0;
        }

        public override void PlayCraftEffect(Mobile from)
        {
            from.PlaySound(0x242);
        }

        public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            // erl: handle special dyes differently

            if (item.ItemType == typeof(SpecialDyeTub) || item.ItemType == typeof(SpecialDye))
            {
                if (failed)
                    from.SendMessage("You fail to mix the dye correctly.");

                return 0;
            }

            if (failed)
            {
                from.AddToBackpack(new Bottle());

                return 500287; // You fail to create a useful potion.
            }
            else
            {
                from.PlaySound(0x240); // Sound of a filling bottle

                if (quality == -1)
                    return 1048136; // You create the potion and pour it into a keg.
                else
                    return 500279; // You pour the potion into a bottle...
            }
        }

        public override void InitCraftList()
        {
            int index = -1;

            // Refresh Potion
            index = AddCraft(typeof(RefreshPotion), 1044530, 1044538, -25, 25.0, typeof(BlackPearl), 1044353, 1, 1044361);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(TotalRefreshPotion), 1044530, 1044539, 25.0, 75.0, typeof(BlackPearl), 1044353, 5, 1044361);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Agility Potion
            index = AddCraft(typeof(AgilityPotion), 1044531, 1044540, 15.0, 65.0, typeof(Bloodmoss), 1044354, 1, 1044362);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterAgilityPotion), 1044531, 1044541, 35.0, 85.0, typeof(Bloodmoss), 1044354, 3, 1044362);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Nightsight Potion
            index = AddCraft(typeof(NightSightPotion), 1044532, 1044542, -25.0, 25.0, typeof(SpidersSilk), 1044360, 1, 1044368);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Heal Potion
            index = AddCraft(typeof(LesserHealPotion), 1044533, 1044543, -25.0, 25.0, typeof(Ginseng), 1044356, 1, 1044364);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(HealPotion), 1044533, 1044544, 15.0, 65.0, typeof(Ginseng), 1044356, 3, 1044364);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterHealPotion), 1044533, 1044545, 55.0, 105.0, typeof(Ginseng), 1044356, 7, 1044364);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Strength Potion
            index = AddCraft(typeof(StrengthPotion), 1044534, 1044546, 25.0, 75.0, typeof(MandrakeRoot), 1044357, 2, 1044365);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterStrengthPotion), 1044534, 1044547, 45.0, 95.0, typeof(MandrakeRoot), 1044357, 5, 1044365);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Poison Potion
            index = AddCraft(typeof(LesserPoisonPotion), 1044535, 1044548, -5.0, 45.0, typeof(Nightshade), 1044358, 1, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(PoisonPotion), 1044535, 1044549, 15.0, 65.0, typeof(Nightshade), 1044358, 2, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterPoisonPotion), 1044535, 1044550, 55.0, 105.0, typeof(Nightshade), 1044358, 4, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(DeadlyPoisonPotion), 1044535, 1044551, 90.0, 140.0, typeof(Nightshade), 1044358, 8, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Cure Potion
            index = AddCraft(typeof(LesserCurePotion), 1044536, 1044552, -10.0, 40.0, typeof(Garlic), 1044355, 1, 1044363);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(CurePotion), 1044536, 1044553, 25.0, 75.0, typeof(Garlic), 1044355, 3, 1044363);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterCurePotion), 1044536, 1044554, 65.0, 115.0, typeof(Garlic), 1044355, 6, 1044363);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Explosion Potion
            index = AddCraft(typeof(LesserExplosionPotion), 1044537, 1044555, 5.0, 55.0, typeof(SulfurousAsh), 1044359, 3, 1044367);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(ExplosionPotion), 1044537, 1044556, 35.0, 85.0, typeof(SulfurousAsh), 1044359, 5, 1044367);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterExplosionPotion), 1044537, 1044557, 65.0, 115.0, typeof(SulfurousAsh), 1044359, 10, 1044367);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // erl: special dyes!
            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Violet", 80.0, 113.33, typeof(SulfurousAsh), "Sulfurous Ash", 10, 1044367);
            AddRes(index, typeof(BlackPearl), "Black Pearl", 10, 1044361);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Tan", 80.0, 113.33, typeof(Ginseng), "Ginseng", 20, 1044364);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Brown", 80.0, 113.33, typeof(MandrakeRoot), "Mandrake Root", 20);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Dark Blue", 80.0, 113.33, typeof(BlackPearl), "Black Pearl", 20, 1044361);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Forest Green", 80.0, 113.33, typeof(BlackPearl), "Black Pearl", 10, 1044361);
            AddRes(index, typeof(Nightshade), "Nightshade", 10, 1044366);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Pink", 80.0, 113.33, typeof(Bloodmoss), "Blood Moss", 10, 1044362);
            AddRes(index, typeof(Garlic), "Garlic", 10, 1044363);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Red", 80.0, 113.33, typeof(Bloodmoss), "Blood Moss", 20, 1044362);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDyeTub), "Mix Dye", "Olive", 80.0, 113.33, typeof(Garlic), "Garlic", 10, 1044363);
            AddRes(index, typeof(Nightshade), "Nightshade", 10, 1044366);
            SetNeedWater(index, true);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDye), "Mix Dye", "> Lighten the mix", 80.0, 100.00, typeof(SulfurousAsh), "Sulfurous Ash", 2, 1044367);
            AddRes(index, typeof(SpecialDyeTub), "Special Dye Tub", 1);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

            index = AddCraft(typeof(SpecialDye), "Mix Dye", "> Darken the mix", 80.0, 100.00, typeof(BlackPearl), "Black Pearl", 2, 1044361);
            AddRes(index, typeof(SpecialDyeTub), "Special Dye Tub", 1);
            AddSkill(index, SkillName.Tailoring, 80.0, 113.33);
        }
    }
}
