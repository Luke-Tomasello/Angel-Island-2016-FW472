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

using Server.Items;
using System;
using System.Collections;

namespace Server.Engines.BulkOrders
{
    public class LargeTailorBOD : LargeBOD
    {
        public static double[] m_TailoringMaterialChances = new double[]
            {
                0.857421875, // None
				0.125000000, // Spined
				0.015625000, // Horned
				0.001953125  // Barbed
			};

        public override int ComputeFame()
        {
            int bonus = 0;

            if (RequireExceptional)
                bonus += 50;

            if (Material >= BulkMaterialType.DullCopper && Material <= BulkMaterialType.Valorite)
                bonus += 50 * (1 + (int)(Material - BulkMaterialType.DullCopper));
            else if (Material >= BulkMaterialType.Spined && Material <= BulkMaterialType.Barbed)
                bonus += 100 * (1 + (int)(Material - BulkMaterialType.Spined));

            return 100 + Utility.Random(bonus);
        }

        private static int[][][] m_GoldTable = new int[][][]
            {
                new int[][] // 4-part (regular)
				{
                    new int[]{ 4000, 4000, 4000, 5000, 5000 },
                    new int[]{ 6000, 6000, 6000, 7500, 7500 },
                    new int[]{ 8000, 8000, 10000, 10000, 15000 }
                },
                new int[][] // 4-part (exceptional)
				{
                    new int[]{ 5000, 5000, 5000, 7500, 7500 },
                    new int[]{ 7500, 7500, 7500, 11250, 11250 },
                    new int[]{ 10000, 10000, 15000, 15000, 20000 }
                },
                new int[][] // 5-part (regular)
				{
                    new int[]{ 5000, 5000, 5000, 7500, 7500 },
                    new int[]{ 7500, 7500, 7500, 11250, 11250 },
                    new int[]{ 10000, 10000, 15000, 15000, 20000 }
                },
                new int[][] // 5-part (exceptional)
				{
                    new int[]{ 7500, 7500, 7500, 10000, 10000 },
                    new int[]{ 11250, 11250, 11250, 15000, 15000 },
                    new int[]{ 15000, 15000, 20000, 20000, 30000 }
                },
                new int[][] // 6-part (regular)
				{
                    new int[]{ 7500, 7500, 7500, 10000, 10000 },
                    new int[]{ 11250, 11250, 11250, 15000, 15000 },
                    new int[]{ 15000, 15000, 20000, 20000, 30000 }
                },
                new int[][] // 6-part (exceptional)
				{
                    new int[]{ 10000, 10000, 10000, 15000, 15000 },
                    new int[]{ 15000, 15000, 15000, 22500, 22500 },
                    new int[]{ 20000, 20000, 30000, 30000, 30000 }
                }
            };

        public override int ComputeGold()
        {
            Type primaryType;

            if (Entries.Length > 0)
                primaryType = Entries[0].Details.Type;
            else
                return 0;

            int parts = Entries.Length;

            int index;

            if (parts >= 6)
                index = 2;
            else if (parts == 5)
                index = 1;
            else
                index = 0;

            index *= 2;

            if (RequireExceptional)
                ++index;

            if (index < m_GoldTable.Length)
            {
                int[][] table = m_GoldTable[index];

                if (AmountMax >= 20)
                    index = 2;
                else if (AmountMax >= 15)
                    index = 1;
                else
                    index = 0;

                if (index < table.Length)
                {
                    int[] list = table[index];

                    switch (Material)
                    {
                        default: index = (primaryType.IsSubclassOf(typeof(BaseArmor)) || primaryType.IsSubclassOf(typeof(BaseShoes))) ? 1 : 0; break;
                        case BulkMaterialType.Spined: index = 2; break;
                        case BulkMaterialType.Horned: index = 3; break;
                        case BulkMaterialType.Barbed: index = 4; break;
                    }

                    if (index < list.Length)
                        return list[index];
                }
            }

            return 0;
        }

        private enum Mat
        {
            Cloth,
            Plain,
            Spined,
            Horned,
            Barbed
        }

        [Constructable]
        public LargeTailorBOD()
        {
            LargeBulkEntry[] entries;
            bool useMaterials = false;

            switch (Utility.Random(14))
            {
                default:
                case 0: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Farmer); break;
                case 1: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.FemaleLeatherSet); useMaterials = true; break;
                case 2: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.FisherGirl); break;
                case 3: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Gypsy); break;
                case 4: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.HatSet); break;
                case 5: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Jester); break;
                case 6: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Lady); break;
                case 7: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.MaleLeatherSet); useMaterials = true; break;
                case 8: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Pirate); break;
                case 9: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.ShoeSet); break;
                case 10: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.StuddedSet); useMaterials = true; break;
                case 11: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.TownCrier); break;
                case 12: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.Wizard); break;
                case 13: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.BoneSet); useMaterials = true; break;
            }

            int hue = 0x483;
            int amountMax = Utility.RandomList(10, 15, 20, 20);
            bool reqExceptional = (0.825 > Utility.RandomDouble());

            BulkMaterialType material;

            if (useMaterials)
                material = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);
            else
                material = BulkMaterialType.None;

            this.Hue = hue;
            this.AmountMax = amountMax;
            this.Entries = entries;
            this.RequireExceptional = reqExceptional;
            this.Material = material;
        }

        public LargeTailorBOD(int amountMax, bool reqExceptional, BulkMaterialType mat, LargeBulkEntry[] entries)
        {
            this.Hue = 0x483;
            this.AmountMax = amountMax;
            this.Entries = entries;
            this.RequireExceptional = reqExceptional;
            this.Material = mat;
        }

        public Item MakeCloth(int hue1, int hue2, int hue3, int hue4)
        {
            int hue;

            switch (Utility.Random(4))
            {
                default:
                case 0: hue = hue1 + 1; break;
                case 1: hue = hue2 + 1; break;
                case 2: hue = hue3 + 1; break;
                case 3: hue = hue4 + 1; break;
            }

            UncutCloth v = new UncutCloth(100);

            v.Hue = hue;

            return v;
        }

        private Mat m_Material;

        private int High(Mat mat)
        {
            return 2 + (3 * (int)mat);
        }

        private int Low(Mat mat)
        {
            return 3 * (int)mat;
        }

        private bool Check(int val)
        {
            int ours = (AmountMax < 20 ? Low(m_Material) : High(m_Material));

            return (ours == val);
        }

        private bool Check(int min, int max)
        {
            int ours = (AmountMax < 20 ? Low(m_Material) : High(m_Material));

            return (ours >= min && ours <= max);
        }

        private bool Check(Mat val)
        {
            return (m_Material == val);
        }

        private bool Check(Mat start, Mat end)
        {
            return (m_Material >= start && m_Material <= end);
        }

        public override ArrayList ComputeRewards()
        {
            Type primaryType;

            if (Entries.Length > 0)
                primaryType = Entries[0].Details.Type;
            else
                return new ArrayList();

            bool cloth1 = false, cloth2 = false;
            bool cloth3 = false, cloth4 = false;
            bool cloth5 = false, sandals = false;
            bool ps5 = false, ps10 = false, ps15 = false, ps20 = false;
            bool smallHides = false, mediumHides = false;
            bool lightTapestry = false, darkTapestry = false;
            bool brownBearRug = false, polarBearRug = false;
            bool clothingBlessDeed = false;
            bool rSpined = false, rHorned = false, rBarbed = false;

            Mat mat;

            switch (Material)
            {
                default: mat = (primaryType.IsSubclassOf(typeof(BaseArmor)) || primaryType.IsSubclassOf(typeof(BaseShoes))) ? Mat.Plain : Mat.Cloth; break;
                case BulkMaterialType.Spined: mat = Mat.Spined; break;
                case BulkMaterialType.Horned: mat = Mat.Horned; break;
                case BulkMaterialType.Barbed: mat = Mat.Barbed; break;
            }

            m_Material = mat;

            int parts = Entries.Length;

            if (Core.AOS)
            {
                if (parts <= 4)
                {
                    if (RequireExceptional)
                    {
                        ps15 = lightTapestry = darkTapestry = Check(Low(Mat.Cloth)) || Check(Low(Mat.Plain));
                        ps20 = brownBearRug = polarBearRug = Check(High(Mat.Cloth)) || Check(High(Mat.Plain), Low(Mat.Spined));
                        clothingBlessDeed = Check(High(Mat.Spined), Low(Mat.Barbed));
                        rSpined = Check(High(Mat.Barbed));
                    }
                    else
                    {
                        ps10 = smallHides = mediumHides = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps15 = lightTapestry = darkTapestry = Check(High(Mat.Spined), Low(Mat.Barbed));
                        ps20 = brownBearRug = polarBearRug = Check(High(Mat.Barbed));
                    }
                }
                else if (parts == 5)
                {
                    if (RequireExceptional)
                    {
                        clothingBlessDeed = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        rSpined = Check(High(Mat.Spined), Low(Mat.Horned));
                        rHorned = Check(High(Mat.Horned), Low(Mat.Barbed));
                        rBarbed = Check(High(Mat.Barbed));
                    }
                    else
                    {
                        ps15 = lightTapestry = darkTapestry = Check(Low(Mat.Cloth)) || Check(Low(Mat.Plain));
                        ps20 = brownBearRug = polarBearRug = Check(High(Mat.Cloth)) || Check(High(Mat.Plain), Low(Mat.Spined));
                        clothingBlessDeed = Check(High(Mat.Spined), Low(Mat.Barbed));
                        rSpined = Check(High(Mat.Barbed));
                    }
                }
                else if (parts >= 6)
                {
                    if (RequireExceptional)
                    {
                        rSpined = Check(Low(Mat.Cloth), Low(Mat.Plain));
                        rHorned = Check(High(Mat.Plain), Low(Mat.Spined));
                        rBarbed = Check(High(Mat.Spined), High(Mat.Barbed));
                    }
                    else
                    {
                        clothingBlessDeed = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        rSpined = Check(High(Mat.Spined), Low(Mat.Horned));
                        rHorned = Check(High(Mat.Horned), Low(Mat.Barbed));
                        rBarbed = Check(High(Mat.Barbed));
                    }
                }
            }
            else
            {
                if (parts <= 4)
                {
                    if (RequireExceptional)
                    {
                        cloth5 = Check(Low(Mat.Cloth), Low(Mat.Barbed));
                        sandals = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps5 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps10 = Check(High(Mat.Spined), Low(Mat.Barbed));
                        ps15 = Check(High(Mat.Barbed));
                        smallHides = Check(High(Mat.Spined), Low(Mat.Barbed));
                        mediumHides = Check(High(Mat.Spined), Low(Mat.Barbed));
                        lightTapestry = Check(High(Mat.Barbed));
                        darkTapestry = Check(High(Mat.Barbed));
                    }
                    else
                    {
                        cloth4 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        cloth5 = Check(High(Mat.Spined), High(Mat.Barbed));
                        sandals = Check(Low(Mat.Cloth), Low(Mat.Barbed));
                        ps5 = Check(High(Mat.Spined), Low(Mat.Barbed));
                        ps10 = Check(High(Mat.Barbed));
                        smallHides = Check(High(Mat.Barbed));
                        mediumHides = Check(High(Mat.Barbed));
                    }
                }
                else if (parts == 5)
                {
                    if (RequireExceptional)
                    {
                        cloth5 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps10 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps15 = Check(High(Mat.Spined), Low(Mat.Barbed));
                        ps20 = Check(High(Mat.Barbed));
                        smallHides = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        mediumHides = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        lightTapestry = Check(High(Mat.Spined), Low(Mat.Barbed));
                        darkTapestry = Check(High(Mat.Spined), Low(Mat.Barbed));
                        brownBearRug = Check(High(Mat.Barbed));
                        polarBearRug = Check(High(Mat.Barbed));
                    }
                    else
                    {
                        cloth5 = Check(Low(Mat.Cloth), Low(Mat.Barbed));
                        sandals = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps5 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps10 = Check(High(Mat.Spined), Low(Mat.Barbed));
                        ps15 = Check(High(Mat.Barbed));
                        smallHides = Check(High(Mat.Spined), Low(Mat.Barbed));
                        mediumHides = Check(High(Mat.Spined), Low(Mat.Barbed));
                        lightTapestry = Check(High(Mat.Barbed));
                        darkTapestry = Check(High(Mat.Barbed));
                    }
                }
                else if (parts >= 6)
                {
                    if (RequireExceptional)
                    {
                        ps15 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps20 = Check(High(Mat.Spined), Low(Mat.Barbed));
                        lightTapestry = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        darkTapestry = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        brownBearRug = Check(High(Mat.Spined), Low(Mat.Barbed));
                        polarBearRug = Check(High(Mat.Spined), Low(Mat.Barbed));
                        clothingBlessDeed = Check(High(Mat.Barbed));
                    }
                    else
                    {
                        cloth5 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps10 = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        ps15 = Check(High(Mat.Spined), Low(Mat.Barbed));
                        ps20 = Check(High(Mat.Barbed));
                        smallHides = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        mediumHides = Check(Low(Mat.Cloth), Low(Mat.Spined));
                        lightTapestry = Check(High(Mat.Spined), Low(Mat.Barbed));
                        darkTapestry = Check(High(Mat.Spined), Low(Mat.Barbed));
                        brownBearRug = Check(High(Mat.Barbed));
                        polarBearRug = Check(High(Mat.Barbed));
                    }
                }
            }

            ArrayList list = new ArrayList();

            if (cloth1)
                list.Add(MakeCloth(0x482, 0x48B, 0x487, 0x489));

            if (cloth2)
                list.Add(MakeCloth(0x494, 0x48A, 0x485, 0x484));

            if (cloth3)
                list.Add(MakeCloth(0x48C, 0x48F, 0x48D, 0x490));

            if (cloth4)
                list.Add(MakeCloth(0x48E, 0x493, 0x483, 0x496));

            if (cloth5)
                list.Add(MakeCloth(0x488, 0x47E, 0x481, 0x47D));

            if (sandals)
                list.Add(new Sandals(Utility.RandomList(0x489, 0x47F, 0x482, 0x47E, 0x48F, 0x494, 0x484, 0x497)));

            if (ps5)
                list.Add(new PowerScroll(SkillName.Tailoring, 105));

            if (ps10)
                list.Add(new PowerScroll(SkillName.Tailoring, 110));

            if (ps15)
                list.Add(new PowerScroll(SkillName.Tailoring, 115));

            if (ps20)
                list.Add(new PowerScroll(SkillName.Tailoring, 120));

            if (smallHides)
            {
                if (Utility.RandomBool())
                    list.Add(new SmallStretchedHideEastDeed());
                else
                    list.Add(new SmallStretchedHideSouthDeed());
            }

            if (mediumHides)
            {
                if (Utility.RandomBool())
                    list.Add(new MediumStretchedHideEastDeed());
                else
                    list.Add(new MediumStretchedHideSouthDeed());
            }

            if (lightTapestry)
            {
                if (Utility.RandomBool())
                    list.Add(new LightFlowerTapestryEastDeed());
                else
                    list.Add(new LightFlowerTapestrySouthDeed());
            }

            if (darkTapestry)
            {
                if (Utility.RandomBool())
                    list.Add(new DarkFlowerTapestryEastDeed());
                else
                    list.Add(new DarkFlowerTapestrySouthDeed());
            }

            if (brownBearRug)
            {
                if (Utility.RandomBool())
                    list.Add(new BrownBearRugEastDeed());
                else
                    list.Add(new BrownBearRugSouthDeed());
            }

            if (polarBearRug)
            {
                if (Utility.RandomBool())
                    list.Add(new PolarBearRugEastDeed());
                else
                    list.Add(new PolarBearRugSouthDeed());
            }

            if (clothingBlessDeed)
                list.Add(new ClothingBlessDeed());

            if (rSpined)
                list.Add(new RunicSewingKit(CraftResource.SpinedLeather, 45));

            if (rHorned)
                list.Add(new RunicSewingKit(CraftResource.HornedLeather, 30));

            if (rBarbed)
                list.Add(new RunicSewingKit(CraftResource.BarbedLeather, 15));

            return list;
        }

        public LargeTailorBOD(Serial serial)
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
