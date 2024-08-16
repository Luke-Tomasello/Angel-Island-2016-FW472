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

using Server.Engines.Craft;
using Server.Items;
using System;
using System.Collections;
using Mat = Server.Engines.BulkOrders.BulkMaterialType;

namespace Server.Engines.BulkOrders
{
    [TypeAlias("Scripts.Engines.BulkOrders.SmallSmithBOD")]
    public class SmallSmithBOD : SmallBOD
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

        public override int ComputeFame()
        {
            int bonus = 0;

            if (RequireExceptional)
                bonus += 20;

            if (Material >= BulkMaterialType.DullCopper && Material <= BulkMaterialType.Valorite)
                bonus += 20 * (1 + (int)(Material - BulkMaterialType.DullCopper));
            else if (Material >= BulkMaterialType.Spined && Material <= BulkMaterialType.Barbed)
                bonus += 40 * (1 + (int)(Material - BulkMaterialType.Spined));

            return 10 + Utility.Random(bonus);
        }

        public override int ComputeGold()
        {
            int bonus = 0;

            if (RequireExceptional)
                bonus += 500;

            if (Material >= BulkMaterialType.DullCopper && Material <= BulkMaterialType.Valorite)
                bonus += 250 * (1 + (int)(Material - BulkMaterialType.DullCopper));
            else if (Material >= BulkMaterialType.Spined && Material <= BulkMaterialType.Barbed)
                bonus += 500 * (1 + (int)(Material - BulkMaterialType.Spined));

            return 750 + Utility.Random(bonus);
        }

        public override ArrayList ComputeRewards()
        {
            if (Type == null)
                return new ArrayList();

            bool sturdyTool = false;
            bool glovesPlusThree = false, glovesPlusFive = false;
            bool gargoylesPickaxe = false, prospectorsTool = false;
            bool powderOfTemperament = false, coloredAnvil = false;
            bool ps5 = false, ps10 = false, ps15 = false, ps20 = false;
            bool rDull = false, rShadow = false, rCopper = false, rBronze = false;
            bool a10 = false, a15 = false;

            if (Type.IsSubclassOf(typeof(BaseWeapon)))
            {
                if (RequireExceptional)
                {
                    glovesPlusThree = true;
                    gargoylesPickaxe = true;
                    prospectorsTool = true;
                }
                else
                {
                    sturdyTool = true;
                    prospectorsTool = true;
                }
            }
            else
            {
                if (RequireExceptional)
                {
                    glovesPlusThree = Check(Mat.None) || Check(High(Mat.ShadowIron), Low(Mat.Copper));
                    glovesPlusFive = Check(High(Mat.ShadowIron), Low(Mat.Bronze));
                    gargoylesPickaxe = Check(Low(Mat.None), Low(Mat.ShadowIron));
                    prospectorsTool = Check(Low(Mat.None), High(Mat.ShadowIron));
                    powderOfTemperament = Check(Low(Mat.DullCopper), High(Mat.ShadowIron));
                    coloredAnvil = Check(High(Mat.Bronze), Low(Mat.Agapite));
                    ps5 = Check(High(Mat.Bronze), Low(Mat.Gold));
                    ps10 = Check(High(Mat.Gold), Low(Mat.Agapite));
                    ps15 = Check(High(Mat.Agapite), Low(Mat.Valorite));
                    ps20 = Check(High(Mat.Valorite));
                    rDull = Check(High(Mat.ShadowIron), Low(Mat.Gold));
                    rShadow = Check(High(Mat.Copper), Low(Mat.Gold));
                    rCopper = Check(High(Mat.Gold), Low(Mat.Agapite));
                    rBronze = Check(High(Mat.Agapite), High(Mat.Valorite));
                    a10 = Check(High(Mat.Agapite), High(Mat.Valorite));
                    a15 = Check(High(Mat.Valorite));
                }
                else
                {
                    sturdyTool = Check(Mat.None);
                    glovesPlusThree = Check(Mat.DullCopper, Mat.Bronze) || Check(Mat.Agapite, Mat.Verite);
                    glovesPlusFive = Check(Mat.Agapite, Mat.Valorite);
                    gargoylesPickaxe = Check(Mat.DullCopper, Mat.Agapite);
                    prospectorsTool = Check(Mat.DullCopper, Mat.Agapite);
                    powderOfTemperament = Check(Mat.ShadowIron, Mat.Agapite);
                    ps5 = Check(Mat.Valorite);
                    rDull = Check(Mat.Agapite, Mat.Valorite);
                    rShadow = Check(Mat.Verite, Mat.Valorite);
                }
            }

            ArrayList list = new ArrayList();

            if (sturdyTool)
            {
                if (Utility.RandomBool())
                    list.Add(new SturdyShovel());
                else
                    list.Add(new SturdyPickaxe());
            }

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
                list.Add(new RunicHammer(CraftResource.DullCopper, Core.AOS ? 50 : 50));

            if (rShadow)
                list.Add(new RunicHammer(CraftResource.ShadowIron, Core.AOS ? 45 : 50));

            if (rCopper)
                list.Add(new RunicHammer(CraftResource.Copper, Core.AOS ? 40 : 50));

            if (rBronze)
                list.Add(new RunicHammer(CraftResource.Bronze, Core.AOS ? 35 : 50));

            if (a10)
                list.Add(new AncientSmithyHammer(10));

            if (a15)
                list.Add(new AncientSmithyHammer(15));

            return list;
        }

        public static SmallSmithBOD CreateRandomFor(Mobile m)
        {
            SmallBulkEntry[] entries;
            bool useMaterials;

            if (useMaterials = Utility.RandomBool())
                entries = SmallBulkEntry.BlacksmithArmor;
            else
                entries = SmallBulkEntry.BlacksmithWeapons;

            if (entries.Length > 0)
            {
                double theirSkill = m.Skills[SkillName.Blacksmith].Base;
                int amountMax;

                if (theirSkill >= 70.1)
                    amountMax = Utility.RandomList(10, 15, 20, 20);
                else if (theirSkill >= 50.1)
                    amountMax = Utility.RandomList(10, 15, 15, 20);
                else
                    amountMax = Utility.RandomList(10, 10, 15, 20);

                BulkMaterialType material = BulkMaterialType.None;

                if (useMaterials && theirSkill >= 70.1)
                {
                    for (int i = 0; i < 20; ++i)
                    {
                        BulkMaterialType check = GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances);
                        double skillReq = 0.0;

                        switch (check)
                        {
                            case BulkMaterialType.DullCopper: skillReq = 65.0; break;
                            case BulkMaterialType.ShadowIron: skillReq = 70.0; break;
                            case BulkMaterialType.Copper: skillReq = 75.0; break;
                            case BulkMaterialType.Bronze: skillReq = 80.0; break;
                            case BulkMaterialType.Gold: skillReq = 85.0; break;
                            case BulkMaterialType.Agapite: skillReq = 90.0; break;
                            case BulkMaterialType.Verite: skillReq = 95.0; break;
                            case BulkMaterialType.Valorite: skillReq = 100.0; break;
                            case BulkMaterialType.Spined: skillReq = 65.0; break;
                            case BulkMaterialType.Horned: skillReq = 80.0; break;
                            case BulkMaterialType.Barbed: skillReq = 99.0; break;
                        }

                        if (theirSkill >= skillReq)
                        {
                            material = check;
                            break;
                        }
                    }
                }

                double excChance = 0.0;

                if (theirSkill >= 70.1)
                    excChance = (theirSkill + 80.0) / 200.0;

                bool reqExceptional = (excChance > Utility.RandomDouble());

                SmallBulkEntry entry = null;

                CraftSystem system = DefBlacksmithy.CraftSystem;

                for (int i = 0; i < 150; ++i)
                {
                    SmallBulkEntry check = entries[Utility.Random(entries.Length)];

                    CraftItem item = system.CraftItems.SearchFor(check.Type);

                    if (item != null)
                    {
                        bool allRequiredSkills = true;
                        double chance = item.GetSuccessChance(m, null, system, false, ref allRequiredSkills);

                        if (allRequiredSkills && chance >= 0.0)
                        {
                            if (reqExceptional)
                                chance = item.GetExceptionalChance(system, chance, m);

                            if (chance > 0.0)
                            {
                                entry = check;
                                break;
                            }
                        }
                    }
                }

                if (entry != null)
                    return new SmallSmithBOD(entry, material, amountMax, reqExceptional);
            }

            return null;
        }

        private SmallSmithBOD(SmallBulkEntry entry, BulkMaterialType material, int amountMax, bool reqExceptional)
        {
            this.Hue = 0x44E;
            this.AmountMax = amountMax;
            this.Type = entry.Type;
            this.Number = entry.Number;
            this.Graphic = entry.Graphic;
            this.RequireExceptional = reqExceptional;
            this.Material = material;
        }

        [Constructable]
        public SmallSmithBOD()
        {
            SmallBulkEntry[] entries;
            bool useMaterials;

            if (useMaterials = Utility.RandomBool())
                entries = SmallBulkEntry.BlacksmithArmor;
            else
                entries = SmallBulkEntry.BlacksmithWeapons;

            if (entries.Length > 0)
            {
                int hue = 0x44E;
                int amountMax = Utility.RandomList(10, 15, 20);

                BulkMaterialType material;

                if (useMaterials)
                    material = GetRandomMaterial(BulkMaterialType.DullCopper, m_BlacksmithMaterialChances);
                else
                    material = BulkMaterialType.None;

                bool reqExceptional = Utility.RandomBool() || (material == BulkMaterialType.None);

                SmallBulkEntry entry = entries[Utility.Random(entries.Length)];

                this.Hue = hue;
                this.AmountMax = amountMax;
                this.Type = entry.Type;
                this.Number = entry.Number;
                this.Graphic = entry.Graphic;
                this.RequireExceptional = reqExceptional;
                this.Material = material;
            }
        }

        public SmallSmithBOD(int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional, BulkMaterialType mat)
        {
            this.Hue = 0x44E;
            this.AmountMax = amountMax;
            this.AmountCur = amountCur;
            this.Type = type;
            this.Number = number;
            this.Graphic = graphic;
            this.RequireExceptional = reqExceptional;
            this.Material = mat;
        }

        public SmallSmithBOD(Serial serial)
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
