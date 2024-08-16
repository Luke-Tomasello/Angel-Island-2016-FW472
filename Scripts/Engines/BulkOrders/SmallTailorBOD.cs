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

namespace Server.Engines.BulkOrders
{
    public class SmallTailorBOD : SmallBOD
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

        private enum Mat
        {
            Cloth,
            Plain,
            Spined,
            Horned,
            Barbed
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

        public override ArrayList ComputeRewards()
        {
            if (Type == null)
                return new ArrayList();

            bool cloth1 = false, cloth2 = false;
            bool cloth3 = false, cloth4 = false;
            bool cloth5 = false, sandals = false;
            bool ps5 = false, ps10 = false;
            bool smallHides = false, mediumHides = false;

            Mat mat;

            switch (Material)
            {
                default: mat = (Type.IsSubclassOf(typeof(BaseArmor)) || Type.IsSubclassOf(typeof(BaseShoes))) ? Mat.Plain : Mat.Cloth; break;
                case BulkMaterialType.Spined: mat = Mat.Spined; break;
                case BulkMaterialType.Horned: mat = Mat.Horned; break;
                case BulkMaterialType.Barbed: mat = Mat.Barbed; break;
            }

            if (Core.AOS)
            {
                if (RequireExceptional)
                {
                    if (AmountMax >= 20)
                    {
                        cloth4 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth5 = (mat >= Mat.Spined && mat <= Mat.Horned);
                        sandals = (mat >= Mat.Cloth && mat <= Mat.Horned);
                        ps5 = (mat >= Mat.Spined && mat <= Mat.Horned);
                        ps10 = (mat == Mat.Barbed);
                        smallHides = (mat == Mat.Barbed);
                        mediumHides = (mat == Mat.Barbed);
                    }
                    else
                    {
                        cloth3 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth4 = (mat == Mat.Spined);
                        cloth5 = (mat >= Mat.Horned && mat <= Mat.Barbed);
                        sandals = (mat >= Mat.Spined && mat <= Mat.Barbed);
                        ps5 = (mat >= Mat.Horned && mat <= Mat.Barbed);
                    }
                }
                else
                {
                    if (AmountMax >= 20)
                    {
                        cloth2 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth3 = (mat == Mat.Spined);
                        cloth4 = (mat == Mat.Horned);
                        cloth5 = (mat == Mat.Barbed);
                        sandals = (mat >= Mat.Horned && mat <= Mat.Barbed);
                        ps5 = (mat == Mat.Barbed);
                    }
                    else
                    {
                        cloth1 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth2 = (mat == Mat.Spined);
                        cloth3 = (mat == Mat.Horned);
                        cloth4 = (mat == Mat.Barbed);
                        sandals = (mat == Mat.Barbed);
                    }
                }
            }
            else
            {
                if (RequireExceptional)
                {
                    if (AmountMax >= 20)
                    {
                        cloth4 = (mat >= Mat.Cloth && mat <= Mat.Horned);
                        cloth5 = (mat == Mat.Barbed);
                        sandals = (mat >= Mat.Spined && mat <= Mat.Barbed);
                        ps5 = (mat == Mat.Barbed);
                    }
                    else
                    {
                        cloth3 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth4 = (mat >= Mat.Spined && mat <= Mat.Barbed);
                        sandals = (mat >= Mat.Horned && mat <= Mat.Barbed);
                    }
                }
                else
                {
                    if (AmountMax >= 20)
                    {
                        cloth2 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth3 = (mat == Mat.Spined);
                        cloth4 = (mat >= Mat.Horned && mat <= Mat.Barbed);
                        sandals = (mat == Mat.Barbed);
                    }
                    else
                    {
                        cloth1 = (mat >= Mat.Cloth && mat <= Mat.Plain);
                        cloth2 = (mat == Mat.Spined);
                        cloth3 = (mat == Mat.Horned);
                        cloth4 = (mat == Mat.Barbed);
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

            return list;
        }

        public static SmallTailorBOD CreateRandomFor(Mobile m)
        {
            SmallBulkEntry[] entries;
            bool useMaterials;

            if (useMaterials = Utility.RandomBool())
                entries = SmallBulkEntry.TailorLeather;
            else
                entries = SmallBulkEntry.TailorCloth;

            if (entries.Length > 0)
            {
                double theirSkill = m.Skills[SkillName.Tailoring].Base;
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
                        BulkMaterialType check = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);
                        double skillReq = 0.0;

                        switch (check)
                        {
                            case BulkMaterialType.DullCopper: skillReq = 65.0; break;
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

                CraftSystem system = DefTailoring.CraftSystem;

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
                    return new SmallTailorBOD(entry, material, amountMax, reqExceptional);
            }

            return null;
        }

        private SmallTailorBOD(SmallBulkEntry entry, BulkMaterialType material, int amountMax, bool reqExceptional)
        {
            this.Hue = 0x483;
            this.AmountMax = amountMax;
            this.Type = entry.Type;
            this.Number = entry.Number;
            this.Graphic = entry.Graphic;
            this.RequireExceptional = reqExceptional;
            this.Material = material;
        }

        [Constructable]
        public SmallTailorBOD()
        {
            SmallBulkEntry[] entries;
            bool useMaterials;

            if (useMaterials = Utility.RandomBool())
                entries = SmallBulkEntry.TailorLeather;
            else
                entries = SmallBulkEntry.TailorCloth;

            if (entries.Length > 0)
            {
                int hue = 0x483;
                int amountMax = Utility.RandomList(10, 15, 20);

                BulkMaterialType material;

                if (useMaterials)
                    material = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);
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

        public SmallTailorBOD(int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional, BulkMaterialType mat)
        {
            this.Hue = 0x483;
            this.AmountMax = amountMax;
            this.AmountCur = amountCur;
            this.Type = type;
            this.Number = number;
            this.Graphic = graphic;
            this.RequireExceptional = reqExceptional;
            this.Material = mat;
        }

        public SmallTailorBOD(Serial serial)
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
