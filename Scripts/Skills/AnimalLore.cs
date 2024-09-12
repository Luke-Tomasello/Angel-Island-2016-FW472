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

/* ./Scripts/Skills/AnimalLore.cs
 *	ChangeLog :
 *	03/28/07 Taran Kain
 *		Added custom pages section - nonfunctional, needs redesign
 *	12/08/06 Taran Kain
 *		Made the gump closable.
 *	12/07/06 Taran Kain
 *		Added skill and stat locks.
 *		Changed the way the gump handles pages.
 *		Changed "---" to only display when skill == 0.0 (prev showed when skill < 10.0)
 *		Added TC-only code to allow players to see all genes.
 *	11/20/06 Taran Kain
 *		Made the gumps play nice with new loyalty values.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/6/04 - Old Salty
 *		Altered necessary skill levels to match 100 max skill rather than 120.  I left in a chance to fail at 100.0 
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.SkillHandlers
{
    public class AnimalLore
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.AnimalLore].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(500328); // What animal should I look at?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.Alive)
                {
                    from.SendLocalizedMessage(500331); // The spirits of the dead are not the province of animal lore.
                }
                else if (targeted is BaseCreature)
                {
                    BaseCreature c = (BaseCreature)targeted;

                    if (!c.IsDeadPet)
                    {
                        if (c.Body.IsAnimal || c.Body.IsMonster || c.Body.IsSea)
                        {
                            if ((!c.Controlled || !c.Tamable) && from.Skills[SkillName.AnimalLore].Base < 80.0) //changed to 80 from 100 by Old Salty
                            {
                                from.SendLocalizedMessage(1049674); // At your skill level, you can only lore tamed creatures.
                            }
                            else if (!c.Tamable && from.Skills[SkillName.AnimalLore].Base < 90.0) //changed to 90 from 110 by Old Salty
                            {
                                from.SendLocalizedMessage(1049675); // At your skill level, you can only lore tamed or tameable creatures.
                            }
                            else if (!from.CheckTargetSkill(SkillName.AnimalLore, c, 0.0, 120.0)) //unchanged by Old Salty to allow failure at GM skill
                            {
                                from.SendLocalizedMessage(500334); // You can't think of anything you know offhand.
                            }
                            else
                            {
                                if (PublishInfo.Publish >= 16 || Core.UOAI || Core.UOREN)
                                {
                                    from.CloseGump(typeof(AnimalLoreGump));
                                    from.SendGump(new AnimalLoreGump(c, from));
                                }
                                else
                                {
                                    new AnimalLoreResponse(c, from);
                                }
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(500329); // That's not an animal!
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(500331); // The spirits of the dead are not the province of animal lore.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500329); // That's not an animal!
                }
            }
        }
    }

    public class AnimalLoreResponse
    {
        private Mobile m_User;
        private BaseCreature m_Target;
        public AnimalLoreResponse(BaseCreature c, Mobile user)
        {
            m_User = user;
            m_Target = c;

            /*
			 * Players will use the Animal Lore and then target any animal to obtain two random items of information about that animal, 
			 * in addition to still discovering the current Loyalty level of an animal (tamed pets, only). 
			 * Players may need to use Animal Lore on a given animal more than once to learn all the possible available information on that animal. 
			 */

            int last = -1;
            int pick = 0;
            for (int ix = 0; ix < 2; ix++)
            {   // make sure we don't pick the same rando number twice
                do { pick = Utility.Random(5); } while (pick == last);
                last = pick;

                switch (pick)
                {
                    case 0: // Information about its magical ability:
                        {
                            if (c.Skills.Magery.Base >= 0.0 && c.Skills.Magery.Base <= 10.9)
                                user.SendMessage("It lacks any true magical abilities");
                            else if (c.Skills.Magery.Base >= 11.0 && c.Skills.Magery.Base <= 25.9)
                                user.SendMessage("It has only minor magical abilities");
                            else if (c.Skills.Magery.Base >= 26.0 && c.Skills.Magery.Base <= 40.9)
                                user.SendMessage("It has some magical abilities");
                            else if (c.Skills.Magery.Base >= 41.0 && c.Skills.Magery.Base <= 60.9)
                                user.SendMessage("It has rather well developed magical abilities");
                            else if (c.Skills.Magery.Base >= 61.0 && c.Skills.Magery.Base <= 74.9)
                                user.SendMessage("It has strong magical abilities");
                            else if (c.Skills.Magery.Base >= 75.0 && c.Skills.Magery.Base <= 85.9)
                                user.SendMessage("It has extremely powerful magical abilities");
                            else if (c.Skills.Magery.Base >= 86.0 && c.Skills.Magery.Base <= 99.9)
                                user.SendMessage("It has nearly mastered the secrets of magic");
                            else if (c.Skills.Magery.Base >= 100.0)
                                user.SendMessage("It has mastered the secrets of magic");
                        }
                        break;
                    case 1: // Information about its training level, for tamable animals:
                        double Training_Level = (c.Skills.Anatomy.Base + c.Skills.Wrestling.Base + c.Skills.Tactics.Base + c.Skills.MagicResist.Base + c.Skills.Meditation.Base + c.Skills.Magery.Base + c.Skills.EvalInt.Base) / 7.0;
                        {
                            if (Training_Level >= 0.0 && Training_Level <= 10.9)
                                user.SendMessage("It has only just begun its combat training");
                            else if (Training_Level >= 11.0 && Training_Level <= 25.9)
                                user.SendMessage("It is somewhat trained in the art of war");
                            else if (Training_Level >= 26.0 && Training_Level <= 40.9)
                                user.SendMessage("It appears fairly trained in the ways of combat");
                            else if (Training_Level >= 41.0 && Training_Level <= 60.9)
                                user.SendMessage("It has excellent combat training");
                            else if (Training_Level >= 61.0 && Training_Level <= 74.9)
                                user.SendMessage("It has superior combat training");
                            else if (Training_Level >= 75.0 && Training_Level <= 85.9)
                                user.SendMessage("It has nearly learned all there is in the ways of combat");
                            else if (Training_Level >= 86.0 && Training_Level <= 99.9)
                                user.SendMessage("It has nearly mastered the art of war");
                            else if (Training_Level >= 100.0)
                                user.SendMessage("It has mastered the art of war");
                        }
                        break;
                    case 2: // Information about its past owners, for tamable animals:
                        {
                            if (c.Owners.Count == 0)
                                user.SendMessage("It seems to have never been tamed");
                            else if (c.Owners.Count == 1)
                                user.SendMessage("It appears to have known only one master in its life");
                            else if (c.Owners.Count == 2)
                                user.SendMessage("It seems to have known two masters in its life");
                            else if (c.Owners.Count == 3)
                                user.SendMessage("It appears annoyed at having known three masters");
                            else if (c.Owners.Count == 4)
                                user.SendMessage("It appears angry to have known four masters");
                            else if (c.Owners.Count == 5)
                                user.SendMessage("It appears infuriated to have known five masters");
                            else
                                user.SendMessage("It is weary of human companionship");
                        }
                        break;
                    case 3: // Information about its diet:
                        {
                            /*
							 * RUNO's notion of favorite food is not granular enough to model the OSI favorite food messages.
							 * For instance, OSI differentiates between "grass" and "hay", RUNUO does not.
							 * In order to display all messages, we will randomly select one from the GrainsAndHay family
							 * At some point in the future the FootTypes should be expanded.
							 * ALSO : goats eat leather (as well as many other things.) We will add a special case for goats to insure 
							 *	their very special message gets displayed.
							 * http://vboards.stratics.com/uo-tamer/38862-amazing-pet-colour-charts.html
							 * http://update.uo.com/design_411.html
							 */

                            if ((c.FavoriteFood & FoodType.GrainsAndHay) != 0)
                            {
                                if ((c.FavoriteFood & FoodType.Leather) != 0 && Utility.RandomBool())
                                    user.SendMessage("Strangely enough, this animal will eat leather");
                                else
                                {
                                    switch (Utility.Random(4))
                                    {
                                        case 0: user.SendMessage("You sense that it likes to eat grass"); break;
                                        case 1: user.SendMessage("You sense that it likes to eat hay"); break;
                                        case 2: user.SendMessage("This creature likes to eat grains"); break;
                                        case 3: user.SendMessage("This creature will eat various crops"); break;
                                    }
                                }
                            }
                            else
                            {
                                if ((c.FavoriteFood & FoodType.FruitsAndVegies) != 0)
                                    user.SendMessage("You sense that it would delight in fruit for a meal");
                                else if ((c.FavoriteFood & FoodType.Meat) != 0)
                                    user.SendMessage("This creature devours meat for its meals");
                                else if ((c.FavoriteFood & FoodType.Fish) != 0)
                                    user.SendMessage("This creature will eat fish");
                            }
                        }
                        break;
                    case 4: // Information about its natural resources (things you can get off of the animal):
                        {
                            // I simply prioritize feathers over meat and hides over meat.
                            // I guess that's the right approach.
                            if (c is Sheep)
                                user.SendMessage("You could use this creature for its wool");
                            else if (c is PackHorse || c is PackLlama)
                                user.SendMessage("It does well at carrying heavy loads");
                            else if (c.Feathers > 0)
                                user.SendMessage("This creature is sometimes used for its feathers");
                            else if (c.Hides > 0)
                                user.SendMessage("If this creature were dead, you could use its hides for leather");
                            else if (c.Meat > 0)
                                user.SendMessage("You could slaughter it for meat");
                            else if (c is Reaper || c is Bogling || c is BogThing || c is Corpser)
                                user.SendMessage("It is sometimes used for its wood");
                        }
                        break;
                }
            }
        }
    }

    public class AnimalLoreGump : Gump
    {
        private Mobile m_User;
        private BaseCreature m_Target;
        private int m_Page;

        private enum ButtonID
        {
            NextPage = 100,
            PrevPage = 102,
            StrLock = 1001,
            DexLock = 1002,
            IntLock = 1003,
            SkillLock = 2000
        }

        private static string FormatSkill(BaseCreature c, SkillName name)
        {
            Skill skill = c.Skills[name];

            if (skill.Base == 0)
                return "<div align=right>---</div>";

            return String.Format("<div align=right>{0:F1}</div>", skill.Base);
        }

        private static string FormatAttributes(int cur, int max)
        {
            if (max == 0)
                return "<div align=right>---</div>";

            return String.Format("<div align=right>{0}/{1}</div>", cur, max);
        }

        private static string FormatStat(int val)
        {
            if (val == 0)
                return "<div align=right>---</div>";

            return String.Format("<div align=right>{0}</div>", val);
        }

        private static string FormatElement(int val)
        {
            if (val <= 0)
                return "<div align=right>---</div>";

            return String.Format("<div align=right>{0}%</div>", val);
        }

        private const int LabelColor = 0x24E5;

        private const int NumStaticPages = 3;

        private int NumTotalPages
        {
            get
            {
                int genes = 0;
                foreach (PropertyInfo pi in m_Target.GetType().GetProperties())
                {
                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(pi, typeof(GeneAttribute), true);
                    if (attr == null)
                        continue;
                    if (m_User.AccessLevel < AccessLevel.Counselor && !Server.Misc.TestCenter.Enabled)
                    {
                        if (attr.Visibility == GeneVisibility.Invisible)
                            continue;
                        if (attr.Visibility == GeneVisibility.Tame && m_User != m_Target.ControlMaster)
                            continue;
                    }

                    genes++;
                }

                return NumStaticPages + (int)Math.Ceiling(genes / 9.0);
            }
        }

        public AnimalLoreGump(BaseCreature c, Mobile user)
            : this(c, user, 0)
        {
        }

        public AnimalLoreGump(BaseCreature c, Mobile user, int page)
            : base(250, 50)
        {
            m_User = user;
            m_Target = c;
            m_Page = page;
            if (m_Page < 0)
                m_Page = 0;
            if (m_Page >= NumTotalPages)
                m_Page = NumTotalPages - 1;

            AddPage(0);

            AddImage(100, 100, 2080);
            AddImage(118, 137, 2081);
            AddImage(118, 207, 2081);
            AddImage(118, 277, 2081);
            AddImage(118, 347, 2083);

            AddHtml(147, 108, 210, 18, String.Format("<center><i>{0}</i></center>", c.Name), false, false);

            AddButton(240, 77, 2093, 2093, 2, GumpButtonType.Reply, 0);

            AddImage(140, 138, 2091);
            AddImage(140, 335, 2091);

            AddPage(0);
            switch (m_Page)
            {
                case 0:
                    {
                        #region Attributes
                        AddImage(128, 152, 2086);
                        AddHtmlLocalized(147, 150, 160, 18, 1049593, 200, false, false); // Attributes

                        AddHtmlLocalized(153, 168, 160, 18, 1049578, LabelColor, false, false); // Hits
                        AddHtml(280, 168, 75, 18, FormatAttributes(c.Hits, c.HitsMax), false, false);

                        AddHtmlLocalized(153, 186, 160, 18, 1049579, LabelColor, false, false); // Stamina
                        AddHtml(280, 186, 75, 18, FormatAttributes(c.Stam, c.StamMax), false, false);

                        AddHtmlLocalized(153, 204, 160, 18, 1049580, LabelColor, false, false); // Mana
                        AddHtml(280, 204, 75, 18, FormatAttributes(c.Mana, c.ManaMax), false, false);

                        AddHtmlLocalized(153, 222, 160, 18, 1028335, LabelColor, false, false); // Strength
                        AddHtml(320, 222, 35, 18, FormatStat(c.Str), false, false);
                        AddStatLock(355, 222, c.StrLock, ButtonID.StrLock);

                        AddHtmlLocalized(153, 240, 160, 18, 3000113, LabelColor, false, false); // Dexterity
                        AddHtml(320, 240, 35, 18, FormatStat(c.Dex), false, false);
                        AddStatLock(355, 240, c.DexLock, ButtonID.DexLock);

                        AddHtmlLocalized(153, 258, 160, 18, 3000112, LabelColor, false, false); // Intelligence
                        AddHtml(320, 258, 35, 18, FormatStat(c.Int), false, false);
                        AddStatLock(355, 258, c.IntLock, ButtonID.IntLock);

                        AddImage(128, 278, 2086);
                        AddHtmlLocalized(147, 276, 160, 18, 3001016, 200, false, false); // Miscellaneous

                        AddHtmlLocalized(153, 294, 160, 18, 1049581, LabelColor, false, false); // Armor Rating
                        AddHtml(320, 294, 35, 18, FormatStat(c.VirtualArmor), false, false);

                        AddHtmlLocalized(153, 312, 160, 18, 3000120, LabelColor, false, false); // Gender
                        AddHtml(280, 312, 75, 18, String.Format("<div align=right>{0}</div>", c.Female ? "Female" : "Male"), false, false);

                        break;
                        #endregion
                    }
                case 1:
                    {
                        #region Skills
                        AddImage(128, 152, 2086);
                        AddHtmlLocalized(147, 150, 160, 18, 3001030, 200, false, false); // Combat Ratings

                        AddHtmlLocalized(153, 168, 160, 18, 1044103, LabelColor, false, false); // Wrestling
                        AddHtml(320, 168, 35, 18, FormatSkill(c, SkillName.Wrestling), false, false);
                        AddSkillLock(355, 168, c, SkillName.Wrestling, ButtonID.SkillLock + (int)SkillName.Wrestling);

                        AddHtmlLocalized(153, 186, 160, 18, 1044087, LabelColor, false, false); // Tactics
                        AddHtml(320, 186, 35, 18, FormatSkill(c, SkillName.Tactics), false, false);
                        AddSkillLock(355, 186, c, SkillName.Tactics, ButtonID.SkillLock + (int)SkillName.Tactics);

                        AddHtmlLocalized(153, 204, 160, 18, 1044086, LabelColor, false, false); // Magic Resistance
                        AddHtml(320, 204, 35, 18, FormatSkill(c, SkillName.MagicResist), false, false);
                        AddSkillLock(355, 204, c, SkillName.MagicResist, ButtonID.SkillLock + (int)SkillName.MagicResist);

                        AddHtmlLocalized(153, 222, 160, 18, 1044061, LabelColor, false, false); // Anatomy
                        AddHtml(320, 222, 35, 18, FormatSkill(c, SkillName.Anatomy), false, false);
                        AddSkillLock(355, 222, c, SkillName.Anatomy, ButtonID.SkillLock + (int)SkillName.Anatomy);

                        AddHtmlLocalized(153, 240, 160, 18, 1044090, LabelColor, false, false); // Poisoning
                        AddHtml(320, 240, 35, 18, FormatSkill(c, SkillName.Poisoning), false, false);
                        AddSkillLock(355, 240, c, SkillName.Poisoning, ButtonID.SkillLock + (int)SkillName.Poisoning);

                        AddImage(128, 260, 2086);
                        AddHtmlLocalized(147, 258, 160, 18, 3001032, 200, false, false); // Lore & Knowledge

                        AddHtmlLocalized(153, 276, 160, 18, 1044085, LabelColor, false, false); // Magery
                        AddHtml(320, 276, 35, 18, FormatSkill(c, SkillName.Magery), false, false);
                        AddSkillLock(355, 276, c, SkillName.Magery, ButtonID.SkillLock + (int)SkillName.Magery);

                        AddHtmlLocalized(153, 294, 160, 18, 1044076, LabelColor, false, false); // Evaluating Intelligence
                        AddHtml(320, 294, 35, 18, FormatSkill(c, SkillName.EvalInt), false, false);
                        AddSkillLock(355, 294, c, SkillName.EvalInt, ButtonID.SkillLock + (int)SkillName.EvalInt);

                        AddHtmlLocalized(153, 312, 160, 18, 1044106, LabelColor, false, false); // Meditation
                        AddHtml(320, 312, 35, 18, FormatSkill(c, SkillName.Meditation), false, false);
                        AddSkillLock(355, 312, c, SkillName.Meditation, ButtonID.SkillLock + (int)SkillName.Meditation);

                        break;
                        #endregion
                    }
                case 2:
                    {
                        #region Misc
                        AddImage(128, 152, 2086);
                        AddHtmlLocalized(147, 150, 160, 18, 1049563, 200, false, false); // Preferred Foods

                        int foodPref = 3000340;

                        if ((c.FavoriteFood & FoodType.FruitsAndVegies) != 0)
                            foodPref = 1049565; // Fruits and Vegetables
                        else if ((c.FavoriteFood & FoodType.GrainsAndHay) != 0)
                            foodPref = 1049566; // Grains and Hay
                        else if ((c.FavoriteFood & FoodType.Fish) != 0)
                            foodPref = 1049568; // Fish
                        else if ((c.FavoriteFood & FoodType.Meat) != 0)
                            foodPref = 1049564; // Meat

                        AddHtmlLocalized(153, 168, 160, 18, foodPref, LabelColor, false, false);

                        AddImage(128, 188, 2086);
                        AddHtmlLocalized(147, 186, 160, 18, 1049569, 200, false, false); // Pack Instincts

                        int packInstinct = 3000340;

                        if ((c.PackInstinct & PackInstinct.Canine) != 0)
                            packInstinct = 1049570; // Canine
                        else if ((c.PackInstinct & PackInstinct.Ostard) != 0)
                            packInstinct = 1049571; // Ostard
                        else if ((c.PackInstinct & PackInstinct.Feline) != 0)
                            packInstinct = 1049572; // Feline
                        else if ((c.PackInstinct & PackInstinct.Arachnid) != 0)
                            packInstinct = 1049573; // Arachnid
                        else if ((c.PackInstinct & PackInstinct.Daemon) != 0)
                            packInstinct = 1049574; // Daemon
                        else if ((c.PackInstinct & PackInstinct.Bear) != 0)
                            packInstinct = 1049575; // Bear
                        else if ((c.PackInstinct & PackInstinct.Equine) != 0)
                            packInstinct = 1049576; // Equine
                        else if ((c.PackInstinct & PackInstinct.Bull) != 0)
                            packInstinct = 1049577; // Bull

                        AddHtmlLocalized(153, 204, 160, 18, packInstinct, LabelColor, false, false);

                        AddImage(128, 224, 2086);
                        AddHtmlLocalized(147, 222, 160, 18, 1049594, 200, false, false); // Loyalty Rating

                        // loyalty redo
                        int loyaltyval = (int)c.Loyalty / 10;
                        if (loyaltyval < 0)
                            loyaltyval = 0;
                        if (loyaltyval > 11)
                            loyaltyval = 11;
                        AddHtmlLocalized(153, 240, 160, 18, (!c.Controlled || c.Loyalty == PetLoyalty.None) ? 1061643 : 1049594 + loyaltyval, LabelColor, false, false);

                        break;
                        #endregion
                    }
                default: // rest of the pages are filled with genes - be sure to adjust "pg" calc in here when adding pages
                    {
                        int nextpage = 3;

                        // idea for later - flesh out custom pages more, a string[] is hackish

                        //List<string[]> custompages = c.GetAnimalLorePages();
                        //if (custompages != null && page >= nextpage && page < (nextpage + custompages.Count))
                        //{
                        //    foreach (string[] s in custompages)
                        //    {
                        //        for (int i = 0; i < s.Length; i++)
                        //        {
                        //            AddHtml(153, 168 + 18 * i, 150, 18, s[i], false, false);
                        //        }
                        //    }

                        //    nextpage += custompages.Count;
                        //}

                        #region Genetics
                        if (page >= nextpage)
                        {
                            List<PropertyInfo> genes = new List<PropertyInfo>();

                            foreach (PropertyInfo pi in c.GetType().GetProperties())
                            {
                                GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(pi, typeof(GeneAttribute), true);
                                if (attr == null)
                                    continue;
                                if (m_User.AccessLevel < AccessLevel.Counselor && !Server.Misc.TestCenter.Enabled)
                                {
                                    if (attr.Visibility == GeneVisibility.Invisible)
                                        continue;
                                    if (attr.Visibility == GeneVisibility.Tame && m_User != c.ControlMaster)
                                        continue;
                                }

                                genes.Add(pi);
                            }

                            int pg = m_Page - nextpage;

                            AddImage(128, 152, 2086);
                            AddHtml(147, 150, 160, 18, "Genetics", false, false);

                            for (int i = 0; i < 9; i++)
                            {
                                if (pg * 9 + i >= genes.Count)
                                    break;

                                GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(genes[pg * 9 + i], typeof(GeneAttribute), true);
                                AddHtml(153, 168 + 18 * i, 120, 18, attr.Name, false, false);
                                AddHtml(240, 168 + 18 * i, 115, 18, String.Format("<div align=right>{0:G3}</div>", c.DescribeGene(genes[pg * 9 + i], attr)), false, false);
                            }
                        }
                        break;
                        #endregion
                    }
            }

            if (m_Page < NumTotalPages - 1)
                AddButton(340, 358, 5601, 5605, (int)ButtonID.NextPage, GumpButtonType.Reply, 0);
            if (m_Page > 0)
                AddButton(317, 358, 5603, 5607, (int)ButtonID.PrevPage, GumpButtonType.Reply, 0);
        }

        private void AddSkillLock(int x, int y, BaseCreature c, SkillName skill, ButtonID buttonID)
        {
            if (m_Target.ControlMaster != m_User && m_User.AccessLevel < AccessLevel.GameMaster)
                return; // no fooling around with wild/other people's critters!

            Skill sk = c.Skills[skill];

            if (sk != null)
            {
                int buttonID1, buttonID2;
                int xOffset, yOffset;

                switch (sk.Lock)
                {
                    default:
                    case SkillLock.Up: buttonID1 = 0x983; buttonID2 = 0x983; xOffset = 3; yOffset = 4; break;
                    case SkillLock.Down: buttonID1 = 0x985; buttonID2 = 0x985; xOffset = 3; yOffset = 4; break;
                    case SkillLock.Locked: buttonID1 = 0x82C; buttonID2 = 0x82C; xOffset = 2; yOffset = 2; break;
                }

                AddButton(x + xOffset, y + yOffset, buttonID1, buttonID2, (int)buttonID, GumpButtonType.Reply, 0);
            }
        }

        private void AddStatLock(int x, int y, StatLockType setting, ButtonID buttonID)
        {
            if (m_Target.ControlMaster != m_User && m_User.AccessLevel < AccessLevel.GameMaster)
                return; // no fooling around with wild/other people's critters!

            int buttonID1, buttonID2;
            int xOffset, yOffset;

            switch (setting)
            {
                default:
                case StatLockType.Up: buttonID1 = 0x983; buttonID2 = 0x983; xOffset = 3; yOffset = 4; break;
                case StatLockType.Down: buttonID1 = 0x985; buttonID2 = 0x985; xOffset = 3; yOffset = 4; break;
                case StatLockType.Locked: buttonID1 = 0x82C; buttonID2 = 0x82C; xOffset = 2; yOffset = 2; break;
            }

            AddButton(x + xOffset, y + yOffset, buttonID1, buttonID2, (int)buttonID, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            switch ((ButtonID)info.ButtonID)
            {
                case ButtonID.NextPage:
                    {
                        m_Page++;
                        break; // gump will be resent at end of OnResponse
                    }
                case ButtonID.PrevPage:
                    {
                        m_Page--;
                        break; // gump will be resent at end of OnResponse
                    }
                case ButtonID.StrLock:
                    {
                        switch (m_Target.StrLock)
                        {
                            case StatLockType.Down: m_Target.StrLock = StatLockType.Locked; break;
                            case StatLockType.Locked: m_Target.StrLock = StatLockType.Up; break;
                            case StatLockType.Up: m_Target.StrLock = StatLockType.Down; break;
                        }
                        break;
                    }
                case ButtonID.DexLock:
                    {
                        switch (m_Target.DexLock)
                        {
                            case StatLockType.Down: m_Target.DexLock = StatLockType.Locked; break;
                            case StatLockType.Locked: m_Target.DexLock = StatLockType.Up; break;
                            case StatLockType.Up: m_Target.DexLock = StatLockType.Down; break;
                        }
                        break;
                    }
                case ButtonID.IntLock:
                    {
                        switch (m_Target.IntLock)
                        {
                            case StatLockType.Down: m_Target.IntLock = StatLockType.Locked; break;
                            case StatLockType.Locked: m_Target.IntLock = StatLockType.Up; break;
                            case StatLockType.Up: m_Target.IntLock = StatLockType.Down; break;
                        }
                        break;
                    }
                default:
                    {
                        if (info.ButtonID >= (int)ButtonID.SkillLock)
                        {
                            int skill = info.ButtonID - (int)ButtonID.SkillLock;
                            Skill sk = null;

                            if (skill >= 0 && skill < m_Target.Skills.Length)
                                sk = m_Target.Skills[skill];

                            if (sk != null)
                            {
                                switch (sk.Lock)
                                {
                                    case SkillLock.Up: sk.SetLockNoRelay(SkillLock.Down); sk.Update(); break;
                                    case SkillLock.Down: sk.SetLockNoRelay(SkillLock.Locked); sk.Update(); break;
                                    case SkillLock.Locked: sk.SetLockNoRelay(SkillLock.Up); sk.Update(); break;
                                }
                            }
                        }
                        else
                            return;

                        break;
                    }
            }


            m_User.SendGump(new AnimalLoreGump(m_Target, m_User, m_Page));
        }
    }
}
