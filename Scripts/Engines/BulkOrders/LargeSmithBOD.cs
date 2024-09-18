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
using Mat = Server.Engines.BulkOrders.BulkMaterialType;

namespace Server.Engines.BulkOrders
{
    [TypeAlias("Scripts.Engines.BulkOrders.LargeSmithBOD")]
    public class LargeSmithBOD : LargeBOD
    {
        public static double[] m_BlacksmithMaterialChances = new double[]
            {
                0.501953125, // None
				0.250000000, // Dull Copper
				0.125000000, // Shadow Iron
				0.062500000, // Copper
				0.031250000, // Bronze
				0.015625000, // Gold
				0.007812500, // Agapite
				0.003906250, // Verite
				0.001953125  // Valorite
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
                new int[][] // Ringmail (regular)
				{
                    new int[]{ 3000, 5000, 5000, 7500, 7500, 10000, 10000, 15000, 15000 },
                    new int[]{ 4500, 7500, 7500, 11250, 11500, 15000, 15000, 22500, 22500 },
                    new int[]{ 6000, 10000, 15000, 15000, 20000, 20000, 30000, 30000, 50000 }
                },
                new int[][] // Ringmail (exceptional)
				{
                    new int[]{ 5000, 10000, 10000, 15000, 15000, 25000, 25000, 50000, 50000 },
                    new int[]{ 7500, 15000, 15000, 22500, 22500, 37500, 37500, 75000, 75000 },
                    new int[]{ 10000, 20000, 30000, 30000, 50000, 50000, 100000, 100000, 200000 }
                },
                new int[][] // Chainmail (regular)
				{
                    new int[]{ 4000, 7500, 7500, 10000, 10000, 15000, 15000, 25000, 25000 },
                    new int[]{ 6000, 11250, 11250, 15000, 15000, 22500, 22500, 37500, 37500 },
                    new int[]{ 8000, 15000, 20000, 20000, 30000, 30000, 50000, 50000, 100000 }
                },
                new int[][] // Chainmail (exceptional)
				{
                    new int[]{ 7500, 15000, 15000, 25000, 25000, 50000, 50000, 100000, 100000 },
                    new int[]{ 11250, 22500, 22500, 37500, 37500, 75000, 75000, 150000, 150000 },
                    new int[]{ 15000, 30000, 50000, 50000, 100000, 100000, 200000, 200000, 200000 }
                },
                new int[][] // Platemail (regular)
				{
                    new int[]{ 5000, 10000, 10000, 15000, 15000, 25000, 25000, 50000, 50000 },
                    new int[]{ 7500, 15000, 15000, 22500, 22500, 37500, 37500, 75000, 75000 },
                    new int[]{ 10000, 20000, 30000, 30000, 50000, 50000, 100000, 100000, 200000 }
                },
                new int[][] // Platemail (exceptional)
				{
                    new int[]{ 10000, 25000, 25000, 50000, 50000, 100000, 100000, 100000, 100000 },
                    new int[]{ 15000, 37500, 37500, 75000, 75000, 150000, 150000, 150000, 150000 },
                    new int[]{ 20000, 50000, 100000, 100000, 200000, 200000, 200000, 200000, 200000 }
                }
            };

        public override int ComputeGold()
        {
            Type primaryType;

            if (Entries.Length > 0)
                primaryType = Entries[0].Details.Type;
            else
                return 0;

            bool isRingmail = (primaryType == typeof(RingmailGloves));
            bool isChainmail = (primaryType == typeof(ChainCoif));
            bool isPlatemail = (primaryType == typeof(PlateArms));

            int index;

            if (isPlatemail)
                index = 2;
            else if (isChainmail)
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

                    if (Material >= BulkMaterialType.DullCopper && Material <= BulkMaterialType.Valorite)
                        index = (1 + (int)(Material - BulkMaterialType.DullCopper));
                    else
                        index = 0;

                    if (index < list.Length)
                        return list[index];
                }
            }

            return 0;
        }

        [Constructable]
        public LargeSmithBOD()
        {
            LargeBulkEntry[] entries;
            bool useMaterials = true;

            switch (Utility.Random(3))
            {
                default:
                case 0: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeRing); break;
                case 1: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargePlate); break;
                case 2: entries = LargeBulkEntry.ConvertEntries(this, LargeBulkEntry.LargeChain); break;
            }

            int hue = 0x44E;
            int amountMax = Utility.RandomList(10, 15, 20, 20);
            bool reqExceptional = (0.825 > Utility.RandomDouble());

            BulkMaterialType material;

            if (useMaterials)
                material = GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances);
            else
                material = BulkMaterialType.None;

            this.Hue = hue;
            this.AmountMax = amountMax;
            this.Entries = entries;
            this.RequireExceptional = reqExceptional;
            this.Material = material;
        }

        public LargeSmithBOD(int amountMax, bool reqExceptional, BulkMaterialType mat, LargeBulkEntry[] entries)
        {
            this.Hue = 0x44E;
            this.AmountMax = amountMax;
            this.Entries = entries;
            this.RequireExceptional = reqExceptional;
            this.Material = mat;
        }

        public int High(Mat mat)
        {
            return 2 + (3 * (int)mat);
        }

        public int Low(Mat mat)
        {
            return 3 * (int)mat;
        }

        public bool Check(int val)
        {
            int ours = (AmountMax < 20 ? Low(Material) : High(Material));

            return (ours == val);
        }

        public bool Check(int min, int max)
        {
            int ours = (AmountMax < 20 ? Low(Material) : High(Material));

            return (ours >= min && ours <= max);
        }

        private bool Check(Mat val)
        {
            return (Material == val);
        }

        private bool Check(Mat start, Mat end)
        {
            return (Material >= start && Material <= end);
        }

        public override ArrayList ComputeRewards()
        {
            Type primaryType;

            if (Entries.Length > 0)
                primaryType = Entries[0].Details.Type;
            else
                return new ArrayList();

            bool glovesPlusThree = false, glovesPlusFive = false;
            bool gargoylesPickaxe = false, prospectorsTool = false;
            bool powderOfTemperament = false, coloredAnvil = false;
            bool ps5 = false, ps10 = false, ps15 = false, ps20 = false;
            bool rDull = false, rShadow = false, rCopper = false, rBronze = false;
            bool rGold = false, rAgapite = false, rVerite = false, rValorite = false;
            bool a10 = false, a15 = false, a30 = false, a60 = false;

            bool isRingmail = (primaryType == typeof(RingmailGloves));
            bool isChainmail = (primaryType == typeof(ChainCoif));
            bool isPlatemail = (primaryType == typeof(PlateArms));

            if (isRingmail)
            {
                if (RequireExceptional)
                {
                    gargoylesPickaxe = Check(Mat.None);
                    prospectorsTool = Check(Mat.None);
                    powderOfTemperament = Check(Mat.None);
                    coloredAnvil = Check(Low(Mat.DullCopper)) || Check(Low(Mat.ShadowIron));
                    ps5 = Check(Low(Mat.DullCopper));
                    ps10 = Check(High(Mat.DullCopper), Low(Mat.ShadowIron));
                    ps15 = Check(High(Mat.ShadowIron), Low(Mat.Bronze));
                    ps20 = Check(High(Mat.Bronze), Low(Mat.Gold));
                    rDull = Check(Low(Mat.DullCopper));
                    rShadow = Check(Low(Mat.DullCopper));
                    rCopper = Check(High(Mat.DullCopper), Low(Mat.ShadowIron));
                    rBronze = Check(High(Mat.ShadowIron), Low(Mat.Gold)) || Check(Low(Mat.Agapite));
                    rGold = Check(High(Mat.Agapite), Low(Mat.Valorite));
                    rAgapite = Check(High(Mat.Valorite));
                    a10 = Check(High(Mat.ShadowIron), Low(Mat.Bronze));
                    a15 = Check(Low(Mat.Gold), High(Mat.Agapite));
                    a30 = Check(High(Mat.Agapite), High(Mat.Valorite));
                }
                else
                {
                    glovesPlusThree = Check(Mat.None) || Check(Mat.ShadowIron, Mat.Copper);
                    glovesPlusFive = Check(Mat.Copper, Mat.Bronze);
                    gargoylesPickaxe = Check(Mat.None, Mat.ShadowIron);
                    prospectorsTool = Check(Mat.None, Mat.ShadowIron);
                    powderOfTemperament = Check(Mat.DullCopper, Mat.ShadowIron);
                    coloredAnvil = Check(Mat.Bronze, Mat.Agapite);
                    ps5 = Check(Mat.Bronze, Mat.Gold);
                    ps10 = Check(Mat.Verite);
                    ps15 = Check(Mat.Verite, Mat.Valorite);
                    ps20 = Check(Mat.Valorite);
                    rDull = Check(Mat.ShadowIron, Mat.Gold);
                    rShadow = Check(Mat.Copper, Mat.Gold);
                    rCopper = Check(Mat.Gold, Mat.Agapite);
                    rBronze = Check(Mat.Verite, Mat.Valorite);
                    rGold = Check(Mat.Valorite);
                    a10 = Check(Mat.Agapite, Mat.Verite);
                    a15 = Check(Mat.Valorite);
                }
            }
            else if (isChainmail)
            {
                if (RequireExceptional)
                {
                    glovesPlusThree = Check(Low(Mat.None));
                    glovesPlusFive = Check(Mat.None);
                    gargoylesPickaxe = Check(Low(Mat.None));
                    powderOfTemperament = Check(Low(Mat.None));
                    coloredAnvil = Check(Low(Mat.ShadowIron));
                    ps15 = Check(Low(Mat.DullCopper), Low(Mat.ShadowIron));
                    ps20 = Check(High(Mat.ShadowIron), High(Mat.Copper));
                    rDull = Check(Mat.None);
                    rShadow = Check(High(Mat.None));
                    rBronze = Check(Low(Mat.DullCopper), Low(Mat.Bronze));
                    rGold = Check(High(Mat.Bronze), Low(Mat.Agapite));
                    rAgapite = Check(High(Mat.Agapite), Low(Mat.Valorite));
                    rVerite = Check(High(Mat.Valorite));
                    a10 = Check(Low(Mat.DullCopper), Low(Mat.ShadowIron)) || Check(Low(Mat.Copper));
                    a15 = Check(High(Mat.ShadowIron), Low(Mat.Bronze)) || Check(Low(Mat.Gold));
                    a30 = Check(High(Mat.Bronze), Low(Mat.Agapite));
                    a60 = Check(High(Mat.Agapite), Low(Mat.Valorite));
                }
                else
                {
                    glovesPlusThree = Check(Mat.None, Mat.DullCopper);
                    glovesPlusFive = Check(Mat.DullCopper, Mat.ShadowIron);
                    gargoylesPickaxe = Check(Mat.None);
                    prospectorsTool = Check(Mat.None);
                    coloredAnvil = Check(Mat.ShadowIron, Mat.Copper);
                    ps5 = Check(Mat.ShadowIron, Mat.Copper);
                    ps10 = Check(Mat.Copper);
                    ps15 = Check(Mat.Gold, Mat.Agapite);
                    ps20 = Check(Mat.Agapite, Mat.Verite);
                    rDull = Check(Mat.DullCopper, Mat.Copper);
                    rShadow = Check(Mat.DullCopper, Mat.Copper);
                    rCopper = Check(Mat.Copper, Mat.Bronze);
                    rBronze = Check(Mat.Bronze, Mat.Verite);
                    rGold = Check(Mat.Valorite);
                    a10 = Check(Mat.Bronze, Mat.Agapite);
                    a15 = Check(Mat.Agapite, Mat.Valorite);
                }
            }
            else if (isPlatemail)
            {
                if (RequireExceptional)
                {
                    coloredAnvil = Check(Mat.None);
                    ps5 = Check(Low(Mat.None));
                    ps10 = Check(High(Mat.None));
                    ps20 = Check(Low(Mat.DullCopper), Low(Mat.ShadowIron));
                    rDull = Check(Low(Mat.None));
                    rShadow = Check(Low(Mat.None));
                    rCopper = Check(High(Mat.None));
                    rBronze = Check(Low(Mat.DullCopper), Low(Mat.ShadowIron));
                    rGold = Check(High(Mat.ShadowIron), Low(Mat.Bronze));
                    rAgapite = Check(High(Mat.Bronze), Low(Mat.Agapite));
                    rVerite = Check(High(Mat.Agapite), High(Mat.Valorite));
                    rValorite = Check(High(Mat.Agapite), High(Mat.Valorite));
                    a15 = Check(Low(Mat.DullCopper), Low(Mat.ShadowIron));
                    a30 = Check(High(Mat.ShadowIron), Low(Mat.Bronze));
                    a60 = Check(High(Mat.Bronze), Low(Mat.Agapite));
                }
                else
                {
                    gargoylesPickaxe = Check(Mat.None);
                    prospectorsTool = Check(Mat.None);
                    powderOfTemperament = Check(Mat.None);
                    coloredAnvil = Check(Mat.DullCopper, Mat.ShadowIron);
                    ps5 = Check(Mat.DullCopper);
                    ps10 = Check(Mat.DullCopper, Mat.ShadowIron);
                    ps15 = Check(Mat.ShadowIron, Mat.Bronze);
                    ps20 = Check(Mat.Bronze, Mat.Agapite);
                    rDull = Check(Mat.DullCopper);
                    rShadow = Check(Mat.DullCopper);
                    rCopper = Check(Mat.DullCopper, Mat.ShadowIron);
                    rBronze = Check(Mat.ShadowIron, Mat.Agapite);
                    rGold = Check(Mat.Agapite, Mat.Verite);
                    rAgapite = Check(Mat.Valorite);
                    a10 = Check(Mat.ShadowIron, Mat.Bronze);
                    a15 = Check(Mat.Bronze, Mat.Agapite);
                    a30 = Check(Mat.Agapite, Mat.Valorite);
                    a60 = Check(Mat.Valorite);
                }
            }

            ArrayList list = new ArrayList();

            if (glovesPlusThree)
                list.Add(new StuddedGlovesOfMining(3));

            if (glovesPlusFive)
                list.Add(new RingmailGlovesOfMining(5));

            if (gargoylesPickaxe)
                list.Add(new GargoylesPickaxe());

            if (prospectorsTool)
                list.Add(new ProspectorsTool());

            if (powderOfTemperament)
                list.Add(new PowderOfTemperament());

            if (coloredAnvil)
                list.Add(new ColoredAnvil());

            if (ps5)
                list.Add(new PowerScroll(SkillName.Blacksmith, 105));

            if (ps10)
                list.Add(new PowerScroll(SkillName.Blacksmith, 110));

            if (ps15)
                list.Add(new PowerScroll(SkillName.Blacksmith, 115));

            if (ps20)
                list.Add(new PowerScroll(SkillName.Blacksmith, 120));

            if (rDull)
                list.Add(new RunicHammer(CraftResource.DullCopper, Core.RuleSets.AOSRules() ? 50 : 50));

            if (rShadow)
                list.Add(new RunicHammer(CraftResource.ShadowIron, Core.RuleSets.AOSRules() ? 45 : 50));

            if (rCopper)
                list.Add(new RunicHammer(CraftResource.Copper, Core.RuleSets.AOSRules() ? 40 : 50));

            if (rBronze)
                list.Add(new RunicHammer(CraftResource.Bronze, Core.RuleSets.AOSRules() ? 35 : 50));

            if (rGold)
                list.Add(new RunicHammer(CraftResource.Gold, Core.RuleSets.AOSRules() ? 30 : 50));

            if (rAgapite)
                list.Add(new RunicHammer(CraftResource.Agapite, Core.RuleSets.AOSRules() ? 25 : 50));

            if (rVerite)
                list.Add(new RunicHammer(CraftResource.Verite, Core.RuleSets.AOSRules() ? 20 : 50));

            if (rValorite)
                list.Add(new RunicHammer(CraftResource.Valorite, Core.RuleSets.AOSRules() ? 15 : 50));

            if (a10)
                list.Add(new AncientSmithyHammer(10));

            if (a15)
                list.Add(new AncientSmithyHammer(15));

            if (a30)
                list.Add(new AncientSmithyHammer(30));

            if (a60)
                list.Add(new AncientSmithyHammer(60));

            return list;
        }

        public LargeSmithBOD(Serial serial)
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
