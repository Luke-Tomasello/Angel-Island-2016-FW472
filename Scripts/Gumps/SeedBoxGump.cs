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

/* Scripts/Gumps/SeedBoxGump.cs
 * ChangeLog
 *	1/24/06, Adam
 *		Fix misspelling of "Prickly Pear"
 *	10/10/05, Pix
 *		Fixed Hedges and PlantHue.None.  Added "Other Mutation" display for PlantHue.None
 *  05/20/05, Kit
 *		Fixed issue with black seed color and hedge plant type both having button ID 18.
 *	05/11/05, Kit
 *	Added check for when removing seeds to update boxs item count.
 *	05/06/05, Kit
 *	Modified runuo release
 *	Added support for solen seeds, and hues.
 */

using Server;
using Server.Engines.Plants;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Custom.Gumps
{
    public class SeedBoxGump : Gump
    {
        private Mobile m_Owner;
        private SeedBox m_box;
        private int m_id;

        private static int[] m_ItemIds = new int[]
            {
                0xC83, 0xC86, 0xC88, 0xC94,
                0xC8B, 0xCA5, 0xCA7, 0xC97,
                0xC9F, 0xCA6, 0xC9C, 0xD31,
                0xD04, 0xCA9, 0xD2C, 0xD26,
                0xD27,3215
            };

        private static string[] m_Strings = new string[]
        {
            "Campion Flowers", "Poppies", "Snowdrops", "Bulrushes",
            "Lilies", "Pampas Grass", "Rushes", "Elephant Ear Plant",
            "Fern", "Ponytail Palm", "Small Palm", "Century Plant",
            "Water Plant", "Snake Plant", "Prickly Pear Cactus", "Barrel Cactus",
            "Tribarrel Cactus", "Hedge"
        };

        private static int[] m_Hues = new int[]
            {
                0x455, 0x481, 0x66D, 0x21,
                0x59B, 0x42, 0x53D, 0x5,
                0x8A5, 0x38, 0x46F, 0x2B,
                0xD, 0x10,
                0x486, 0x48E, 0x489, 0x495,
                0x0, 0x1,
                0xFFFF
            };

        private static int[] m_Buttons = new int[]
            {
                38, 19, 26, 27,
                21, 24, 22, 25,
                20, 23, 28, 30,
                29, 31,
                32, 33, 34, 35,
                36, 37,
                39
            };

        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
            }
        }

        public SeedBoxGump(Mobile owner, SeedBox box)
            : this(owner, box, 0)
        { }

        public SeedBoxGump(Mobile owner, SeedBox box, int PlantId)
            : base(10, 10)
        {
            owner.CloseGump(typeof(SeedBoxGump));

            int gumpX = 0;
            int gumpY = 0;
            //bool initialState = false;

            m_Owner = owner;
            m_box = box;
            m_id = PlantId;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            gumpX = 0;
            gumpY = 0;

            AddBackground(24, 24, 644, 445, 9200);

            AddBackground(40, 62, 204, 392, 83);
            AddImageTiled(48, 71, 190, 354, 1416);
            AddAlphaRegion(48, 71, 190, 374);

            AddBackground(255, 62, 394, 392, 83);
            AddImageTiled(263, 71, 380, 354, 1416);
            AddAlphaRegion(263, 71, 380, 374);

            AddLabel(103, 35, 1152, "Seed Types");
            AddLabel(423, 36, 1152, "Seed Colors");
            AddImage(290, 81, 3203);
            AddImage(82, 39, 216);
            AddImage(184, 39, 216);
            AddImage(394, 39, 216);
            AddImage(510, 39, 216);

            gumpX = 55; //20
            gumpY = 77; //30
            for (int i = 0; i < 18; i++)
            {
                int count = 0;
                for (int j = 0; j < 21; j++)
                    count += m_box.m_counts[i, j];

                if (count > 0)
                {
                    if (i == m_id)
                        AddButton(gumpX, gumpY, 4006, 4007, i + 1, GumpButtonType.Reply, 0);
                    else
                        AddButton(gumpX, gumpY, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
                }
                this.AddLabel(gumpX + 30, gumpY, (count > 0 ? 1152 : 808), m_Strings[i]);

                gumpY += 20;
            }

            int but1 = 270;
            int lab1 = but1 + 40;
            int but2 = but1 + 170;
            int lab2 = but2 + 40;
            gumpY = 77;

            #region Plain
            //*****PLAIN*****
            if (m_box.m_counts[m_id, 18] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 36, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 355, m_box.m_counts[m_id, 18].ToString() + " Plain");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Plain");
            gumpY += 40;
            #endregion
            #region Rares
            //*****MAGENTA****
            if (m_box.m_counts[m_id, 14] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 32, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 316, m_box.m_counts[m_id, 14].ToString() + " Rare Magenta");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Rare Magenta");
            gumpY += 20;
            //*****PINK*****
            if (m_box.m_counts[m_id, 15] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 33, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 30, m_box.m_counts[m_id, 15].ToString() + " Rare Pink");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Rare Pink");
            gumpY += 20;
            //*****AQUA****
            if (m_box.m_counts[m_id, 17] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 35, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 86, m_box.m_counts[m_id, 17].ToString() + " Rare Aqua");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Rare Aqua");
            gumpY += 20;
            //*****FIRE RED*****
            if (m_box.m_counts[m_id, 16] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 34, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 43, m_box.m_counts[m_id, 16].ToString() + " Rare Fire Red");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Rare Fire Red");
            gumpY += 40;
            #endregion
            #region StandardColors
            //*****RED*****
            if (m_box.m_counts[m_id, 2] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 26, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 36, m_box.m_counts[m_id, 2].ToString() + " Red");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Red");
            //*****BRIGHT RED*****
            if (m_box.m_counts[m_id, 3] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 27, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 38, m_box.m_counts[m_id, 3].ToString() + " Bright Red");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Bright Red");
            gumpY += 20;
            //*****ORANGE****
            if (m_box.m_counts[m_id, 10] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 28, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 41, m_box.m_counts[m_id, 10].ToString() + " Orange");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Orange");
            //*****BRIGHT ORANGE*****
            if (m_box.m_counts[m_id, 11] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 30, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 42, m_box.m_counts[m_id, 11].ToString() + " Bright Orange");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Bright Orange");
            gumpY += 20;
            //*****YELLOW****
            if (m_box.m_counts[m_id, 8] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 20, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 251, m_box.m_counts[m_id, 8].ToString() + " Yellow");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Yellow");
            //*****BRIGHT YELLOW*****
            if (m_box.m_counts[m_id, 9] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 23, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 153, m_box.m_counts[m_id, 9].ToString() + " Bright Yellow");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Bright Yellow");
            gumpY += 20;
            //*****GREEN****
            if (m_box.m_counts[m_id, 4] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 21, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 61, m_box.m_counts[m_id, 4].ToString() + " Green");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Green");
            //*****BRIGHT GREEN*****
            if (m_box.m_counts[m_id, 5] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 24, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 163, m_box.m_counts[m_id, 5].ToString() + " Bright Green");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Bright Green");
            gumpY += 20;
            //*****BLUE****
            if (m_box.m_counts[m_id, 6] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 22, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 102, m_box.m_counts[m_id, 6].ToString() + " Blue");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Blue");
            //*****BRIGHT BLUE*****
            if (m_box.m_counts[m_id, 7] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 25, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 104, m_box.m_counts[m_id, 7].ToString() + " Bright Blue");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Bright Blue");
            gumpY += 20;
            //*****PURPLE****
            if (m_box.m_counts[m_id, 12] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 29, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 17, m_box.m_counts[m_id, 12].ToString() + " Purple");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Purple");
            //*****BRIGHT PURPLE*****
            if (m_box.m_counts[m_id, 13] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 31, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 19, m_box.m_counts[m_id, 13].ToString() + " Bright Purple");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Bright Purple");
            gumpY += 40;
            #endregion
            #region Mutations
            //*****BLACK****
            if (m_box.m_counts[m_id, 0] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 38, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 0, m_box.m_counts[m_id, 0].ToString() + " Black Mutation");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Black Mutation");
            //*****WHITE*****
            if (m_box.m_counts[m_id, 1] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 19, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 2100, m_box.m_counts[m_id, 1].ToString() + " White Mutation");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 White Mutation");

            gumpY += 20;
            //*****Solen*****
            if (m_box.m_counts[m_id, 19] > 0)
            {
                AddButton(but1, gumpY, 0xFA5, 0xFA6, 37, GumpButtonType.Reply, 0);
                AddLabel(lab1, gumpY + 3, 61, m_box.m_counts[m_id, 19].ToString() + " Solen");
            }
            else
                AddLabel(lab1, gumpY + 3, 808, "0 Solen");
            //*****NONE***** //PlantHue.None == mutant (like Hedges)
            if (m_box.m_counts[m_id, 20] > 0)
            {
                AddButton(but2, gumpY, 0xFA5, 0xFA6, 39, GumpButtonType.Reply, 0);
                AddLabel(lab2, gumpY + 3, 61, m_box.m_counts[m_id, 20].ToString() + " Other Mutation");
            }
            else
                AddLabel(lab2, gumpY + 3, 808, "0 Other Mutation");

            gumpY += 20;
            AddLabel(lab2, gumpY + 3, 2100, m_box.SeedCount().ToString() + " Seeds Held");


            gumpY += 40;
            #endregion

            //plant name
            AddLabel(but1 + 20, 77, 1152, m_Strings[m_id]);

            //plant picture
            AddItem(lab1, 117, m_ItemIds[m_id]);

        }

        public static int GetButton(int ButtonID)
        {
            for (int i = 0; i < 21; i++)
                if (m_Buttons[i] == ButtonID)
                    return i;
            return -1;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (info.ButtonID >= 1 && info.ButtonID <= 18)
            {
                from.SendGump(new SeedBoxGump(from, m_box, info.ButtonID - 1));
                return;
            }

            if (info.ButtonID >= 19)
            {
                int bid = GetButton(info.ButtonID);
                if (bid != -1)
                {
                    int hue = m_Hues[bid];

                    if (m_box.m_counts[m_id, bid] > 0)
                    {
                        from.AddToBackpack(new Seed(ConvertType(m_id - 8), ConvertHue(hue), true));
                        m_box.m_counts[m_id, bid]--;
                        m_box.TotalItems = m_box.SeedCount() / 5;
                    }
                    else
                        from.SendMessage("You do not have any of those seeds!");
                    m_box.InvalidateProperties();
                    from.SendGump(new SeedBoxGump(from, m_box, m_id));
                }
            }
        }

        public static PlantHue ConvertHue(int hue)
        {
            //convert to distro plant hue
            switch (hue)
            {
                case 0x66D: return PlantHue.Red;            //Red = 0x66D,
                case 0x21: return PlantHue.BrightRed;     //BrightRed = 0x21,
                case 0x53D: return PlantHue.Blue;          //Blue = 0x53D,
                case 0x5: return PlantHue.BrightBlue;    //BrightBlue = 0x5,
                case 0x8A5: return PlantHue.Yellow;        //Yellow = 0x8A5,
                case 0x38: return PlantHue.BrightYellow;  //BrightYellow = 0x38,
                case 0: return PlantHue.Plain;         //Plain = 0,
                case 0xD: return PlantHue.Purple;        //Purple = 0xD,
                case 0x10: return PlantHue.BrightPurple;  //BrightPurple = 0x10,
                case 0x46F: return PlantHue.Orange;        //Orange = 0x46F,
                case 0x2B: return PlantHue.BrightOrange;  //BrightOrange = 0x2B,
                case 0x59B: return PlantHue.Green;         //Green = 0x59B,
                case 0x42: return PlantHue.BrightGreen;   //BrightGreen = 0x42,
                case 0x455: return PlantHue.Black;         //Black = 0x455,
                case 0x481: return PlantHue.White;         //White = 0x481,
                case 0x486: return PlantHue.Magenta;       //RareMagenta = 0x486,
                case 0x495: return PlantHue.Aqua;          //RareAqua = 0x495,
                case 0x48E: return PlantHue.Pink;          //RarePink = 0x48E,
                case 0x489: return PlantHue.FireRed;       //RareFireRed = 0x489
                case 0x1: return PlantHue.Solen;
                case 0xFFFF: return PlantHue.None;          //None, mutant (e.g. Hedge)
                default: return PlantHue.Plain;
            }
        }

        public static PlantType ConvertType(int type)
        {
            //convert to distro plant type
            switch (type + 8)
            {
                case 0: return PlantType.CampionFlowers;     // campion flowers 1st
                case 1: return PlantType.Poppies;            // poppies
                case 2: return PlantType.Snowdrops;          // snowdrops
                case 3: return PlantType.Bulrushes;          // bulrushes
                case 4: return PlantType.Lilies;             // lilies
                case 5: return PlantType.PampasGrass;        // pampas grass
                case 6: return PlantType.Rushes;             // rushes
                case 7: return PlantType.ElephantEarPlant;   // elephant ear plant
                case 8: return PlantType.Fern;               // fern 1st
                case 9: return PlantType.PonytailPalm;       // ponytail palm
                case 10: return PlantType.SmallPalm;         // small palm
                case 11: return PlantType.CenturyPlant;      // century plant
                case 12: return PlantType.WaterPlant;        // water plant
                case 13: return PlantType.SnakePlant;        // snake plant
                case 14: return PlantType.PricklyPearCactus; // prickly pear cactus
                case 15: return PlantType.BarrelCactus;      // barrel cactus
                case 16: return PlantType.TribarrelCactus;   // tribarrel cactus 1st
                case 17: return PlantType.Hedge;
                default: return PlantType.CampionFlowers;
            }
        }
    }
}
