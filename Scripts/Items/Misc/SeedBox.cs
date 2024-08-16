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

/* Scripts/items/misc/SeedBox.cs
 * ChangeLog
 *  12/21/06, Adam
 *      Use IBlockRazorSearch interface handle uneanted OnDoubleClick messages sent from razor.
 *	3/30/06 Taran Kain
 *		Added drop sound when a seed is added to the box.
 *  3/27/06 Taran Kain
 *		Removed OnAmountChange(), added CheckHold call to OnDragDrop to ensure we follow container rules
 *	10/15/05, erlein
 *		Replaced weird character in SendMessage() string to accurately display "You cannot access this."
 *	10/10/05, Pix
 *		Changes for PlantHue.None mutant fixing.
 *	05/27/05, Adam
 *		Comment	out	debug output
 *	05/08/05, Kit
 *	Added in override to totalitems	to keep	item count working,	added in serlization for security level
 *	05/06/05, Kit
 *	Modified runuo release for
 *	Added support for solen	seeds, added checks	for	exceeding lock downs of	house
 */
using Custom.Gumps;
using Server.Engines.Plants;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{

    public class SeedBox : Item
    {

        private static int m_maxSeeds = 499; //maximum amount of seeds a Seed Box can hold
        private SecureLevel m_Level;
        private int itemcount;

        public int[,] m_counts = new int[18, 21];

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

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

        public override void UpdateTotals()
        {
            base.UpdateTotals();
            SetTotalItems(itemcount);
        }

        [Constructable]
        public SeedBox()
            : base(0x9A9)
        {
            Movable = true;
            Weight = 60.0;
            Hue = 0x1CE;
            Name = "Seed Box";
            m_Level = SecureLevel.CoOwners;
            itemcount = 0;
            TotalItems = itemcount;
        }

        //public override bool DisplaysContent{	get{ return	false; } }

        public override void OnDoubleClick(Mobile from)
        {
            //Console.Write(itemcount);

            if (!Movable && !CheckAccess(from))
            {
                from.SendMessage("You cannot access this.");
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
                from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else if (from is PlayerMobile)
                from.SendGump(new SeedBoxGump((PlayerMobile)from, this));
        }

        public bool CheckAccess(Mobile m)
        {
            if (!IsLockedDown || m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsAosRules && (house.Public ? house.IsBanned(m) : !house.HasAccess(m)))
                return false;

            return (house != null && house.HasSecureAccess(m, m_Level));
        }

        public SeedBox(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060662, "{0}\t{1}", "Seeds", SeedCount());
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_Level);

            writer.Write(itemcount);
            for (int i = 0; i < 18; i++)
                for (int j = 0; j < 21; j++)
                    writer.Write((int)m_counts[i, j]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt(); // version

            switch (version)
            {
                case 2:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        itemcount = reader.ReadInt();
                        SetTotalItems(itemcount);
                        for (int i = 0; i < 18; i++)
                            for (int j = 0; j < 21; j++)
                                m_counts[i, j] = reader.ReadInt();
                        break;
                    }

                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        itemcount = reader.ReadInt();
                        SetTotalItems(itemcount);
                        for (int i = 0; i < 18; i++)
                            for (int j = 0; j < 20; j++)
                                m_counts[i, j] = reader.ReadInt();
                        break;
                    }
            }
        }

        public static int GetSeedHue(int Hue)
        {
            for (int i = 0; i < 21; i++)
                if (m_Hues[i] == Hue)
                    return i;
            return 0;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!Movable && !CheckAccess(from))
            {
                from.SendMessage("You cannot access	this");
                return false;
            }

            if (!(dropped is Seed))
            {
                from.SendMessage("You can only	store seeds	in this	box.");
                return false;
            }
            BaseHouse house = BaseHouse.FindHouseAt(from);
            int lockdowns = 0;
            if (house != null)
                lockdowns = house.LockDownCount;
            int seeds = lockdowns + 1;

            if (house != null && !Movable && seeds > house.MaxLockDowns)
            {
                from.SendMessage("This	would exceed the houses	lockdown limit.");
                return false;
            }

            int seedcount = SeedCount();
            if (seedcount > m_maxSeeds)
            {
                from.SendMessage("This	seed box cannot	hold any more seeds.");
                return false;
            }


            Seed seed = (Seed)dropped;
            int type = ConvertType(seed.PlantType);
            int hue = ConvertHue(seed.PlantHue);

            m_counts[type, hue]++;
            itemcount = SeedCount() / 5;
            this.TotalItems = itemcount;

            if (Parent is Container)
            {
                // calling the full version with (-itemcount - 1) prevents double-counting the seedbox
                if (!((Container)Parent).CheckHold(from, this, true, true, (-TotalItems - 1), (int)(-TotalWeight - Weight)))
                {
                    m_counts[type, hue]--; // reverse the change to our state
                    itemcount = SeedCount() / 5;
                    TotalItems = itemcount;
                    return false;
                }
            }

            from.SendSound(((dropped.GetDropSound() != -1) ? dropped.GetDropSound() : 0x42), GetWorldLocation());

            dropped.Delete();
            InvalidateProperties();
            return true;
        }

        public static int ConvertHue(PlantHue hue)
        {
            //convert from distro plant	hue
            switch (hue)
            {
                case PlantHue.Black: return 0;   //Black = 0x455
                case PlantHue.White: return 1;   //White = 0x481,
                case PlantHue.Red: return 2;     //Red = 0x66D,
                case PlantHue.BrightRed: return 3;   //BrightRed = 0x21,
                case PlantHue.Green: return 4;   //Green = 0x59B,
                case PlantHue.BrightGreen: return 5;     //BrightGreen = 0x42,
                case PlantHue.Blue: return 6;    //Blue	= 0x53D,
                case PlantHue.BrightBlue: return 7;  //BrightBlue =	0x5,
                case PlantHue.Yellow: return 8;  //Yellow =	0x8A5,
                case PlantHue.BrightYellow: return 9;    //BrightYellow	= 0x38,
                case PlantHue.Orange: return 10;     //Orange =	0x46F,
                case PlantHue.BrightOrange: return 11;   //BrightOrange	= 0x2B,
                case PlantHue.Purple: return 12;     //Purple =	0xD,
                case PlantHue.BrightPurple: return 13;   //BrightPurple	= 0x10,
                case PlantHue.Magenta: return 14;    //RareMagenta = 0x486,
                case PlantHue.Pink: return 15;   //RarePink	= 0x48E,
                case PlantHue.FireRed: return 16;    //RareFireRed = 0x489
                case PlantHue.Aqua: return 17;   //RareAqua	= 0x495,
                case PlantHue.Plain: return 18;  //Plain = 0,
                case PlantHue.Solen: return 19;
                case PlantHue.None: return 20;
                default: return 18;  //Plain
            }
        }

        public static int ConvertType(PlantType type)
        {
            //convert from distro plant	type
            switch (type)
            {
                case PlantType.CampionFlowers: return 0; //	campion	flowers	1st
                case PlantType.Poppies: return 1; //	poppies
                case PlantType.Snowdrops: return 2; //	snowdrops
                case PlantType.Bulrushes: return 3; //	bulrushes
                case PlantType.Lilies: return 4; //	lilies
                case PlantType.PampasGrass: return 5; //	pampas grass
                case PlantType.Rushes: return 6; //	rushes
                case PlantType.ElephantEarPlant: return 7; //	elephant ear plant
                case PlantType.Fern: return 8; //	fern 1st
                case PlantType.PonytailPalm: return 9; //	ponytail palm
                case PlantType.SmallPalm: return 10; //	small palm
                case PlantType.CenturyPlant: return 11; //	century	plant
                case PlantType.WaterPlant: return 12; //	water plant
                case PlantType.SnakePlant: return 13; //	snake plant
                case PlantType.PricklyPearCactus: return 14; //	prickly	pear cactus
                case PlantType.BarrelCactus: return 15; //	barrel cactus
                case PlantType.TribarrelCactus: return 16; //	tribarrel cactus 1st
                case PlantType.Hedge: return 17; //solen seed hedge
                default: return 0;
            }
        }

        public int SeedCount()
        {
            int count = 0;

            for (int i = 0; i < 18; i++)
                for (int j = 0; j < 21; j++)
                    count += m_counts[i, j];

            return count;
        }
    }
}
