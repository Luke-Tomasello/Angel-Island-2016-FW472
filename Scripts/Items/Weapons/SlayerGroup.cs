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

/* Scripts/Items/Weapons/SlayerGroup.cs
 * ChangeLog
 *	7/19/10, Adam
 *		Add Vampires to undead slayer group
 *  6/21/06, Kit	
 *		Added BoneMagiLord/Bone Knight Lord to silver slayer grouping.
 *	3/5/05, Adam
 *		Inserted some debug code to highlight the fact that the the first 'Entry'
 *		in CompileEntries( SlayerGroup[] groups ) is NULL. I'm not sure this is a bug, but 
 *		stumled upon it while fixing 'plain' instruments in BaseRunicTool.cs (I believe the
 *		two are unrelated.)
 *  11/19/04, Froste
 *      Added WraithRiderWarrior and WraithRiderMage to the "silver" slayer list
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    public class SlayerGroup
    {
        private static SlayerEntry[] m_TotalEntries;
        private static SlayerGroup[] m_Groups;

        public static SlayerEntry[] TotalEntries
        {
            get { return m_TotalEntries; }
        }

        public static SlayerGroup[] Groups
        {
            get { return m_Groups; }
        }

        public static SlayerEntry GetEntryByName(SlayerName name)
        {
            int v = (int)name;

            if (v >= 0 && v < m_TotalEntries.Length)
                return m_TotalEntries[v];

            return null;
        }

        public static SlayerName GetLootSlayerType(Type type)
        {
            for (int i = 0; i < m_Groups.Length; ++i)
            {
                SlayerGroup group = m_Groups[i];
                Type[] foundOn = group.FoundOn;

                bool inGroup = false;

                for (int j = 0; foundOn != null && !inGroup && j < foundOn.Length; ++j)
                    inGroup = (foundOn[j] == type);

                if (inGroup)
                {
                    int index = Utility.Random(1 + group.Entries.Length);

                    if (index == 0)
                        return group.m_Super.Name;

                    return group.Entries[index - 1].Name;
                }
            }

            return SlayerName.Silver;
        }

        static SlayerGroup()
        {
            SlayerGroup humanoid = new SlayerGroup();
            SlayerGroup undead = new SlayerGroup();
            SlayerGroup elemental = new SlayerGroup();
            SlayerGroup abyss = new SlayerGroup();
            SlayerGroup arachnid = new SlayerGroup();
            SlayerGroup reptilian = new SlayerGroup();

            humanoid.Opposition = undead;
            humanoid.FoundOn = new Type[] { typeof(BoneKnight), typeof(Lich), typeof(LichLord) };
            humanoid.Super = new SlayerEntry(SlayerName.Repond, typeof(Ogre), typeof(OgreLord), typeof(ArcticOgreLord), typeof(Orc), typeof(OrcishMage), typeof(OrcishLord), typeof(Troll), typeof(Cyclops), typeof(Titan), typeof(OrcBrute), typeof(OrcBomber), typeof(OrcCaptain));
            humanoid.Entries = new SlayerEntry[]
                {
                    new SlayerEntry( SlayerName.OgreTrashing, typeof( Ogre ), typeof( OgreLord ), typeof( ArcticOgreLord ) ),
                    new SlayerEntry( SlayerName.OrcSlaying, typeof( Orc ), typeof( OrcishMage ), typeof( OrcishLord ), typeof( OrcBrute ), typeof( OrcBomber ), typeof( OrcCaptain ) ),
                    new SlayerEntry( SlayerName.TrollSlaughter, typeof( Troll ) )
                };

            undead.Opposition = humanoid;
            undead.Super = new SlayerEntry(SlayerName.Silver,
                typeof(Vampire), typeof(VampireBat), typeof(VladDracula), typeof(WalkingDead),
                typeof(BoneKnightLord), typeof(BoneMagiLord), typeof(AncientLich), typeof(Bogle),
                typeof(BoneMagi), typeof(Lich), typeof(LichLord), typeof(Shade), typeof(Spectre),
                typeof(Wraith), typeof(BoneKnight), typeof(Ghoul), typeof(Mummy), typeof(SkeletalKnight),
                typeof(Skeleton), typeof(Zombie), typeof(WraithRiderWarrior), typeof(WraithRiderMage));
            undead.Entries = new SlayerEntry[0];

            elemental.Opposition = abyss;
            elemental.FoundOn = new Type[] { typeof(Balron), typeof(Daemon) };
            elemental.Super = new SlayerEntry(SlayerName.ElementalBan, typeof(BloodElemental), typeof(EarthElemental), typeof(AgapiteElemental), typeof(BronzeElemental), typeof(CopperElemental), typeof(DullCopperElemental), typeof(GoldenElemental), typeof(ShadowIronElemental), typeof(ValoriteElemental), typeof(VeriteElemental), typeof(PoisonElemental), typeof(FireElemental), typeof(SnowElemental), typeof(AirElemental), typeof(WaterElemental));
            elemental.Entries = new SlayerEntry[]
                {
                    new SlayerEntry( SlayerName.BloodDrinking, typeof( BloodElemental ) ),
                    new SlayerEntry( SlayerName.EarthShatter, typeof( EarthElemental ) ),
                    new SlayerEntry( SlayerName.ElementalHealth, typeof( PoisonElemental ) ),
                    new SlayerEntry( SlayerName.FlameDousing, typeof( FireElemental ) ),
                    new SlayerEntry( SlayerName.SummerWind, typeof( SnowElemental ) ),
                    new SlayerEntry( SlayerName.Vacuum, typeof( AirElemental ) ),
                    new SlayerEntry( SlayerName.WaterDissipation, typeof( WaterElemental ) )
                };

            abyss.Opposition = elemental;
            abyss.FoundOn = new Type[] { typeof(BloodElemental) };
            abyss.Super = new SlayerEntry(SlayerName.Exorcism, typeof(AbysmalHorror), typeof(Balron), typeof(BoneDemon), typeof(ChaosDaemon), typeof(Daemon), typeof(DemonKnight), typeof(Devourer), typeof(Gargoyle), typeof(FireGargoyle), typeof(Gibberling), typeof(HordeMinion), typeof(IceFiend), typeof(Imp), typeof(Impaler), typeof(Ravager), typeof(StoneGargoyle));

            abyss.Entries = new SlayerEntry[]
                {
                    new SlayerEntry( SlayerName.DaemonDismissal, typeof( AbysmalHorror ), typeof( Balron ), typeof( BoneDemon ), typeof( ChaosDaemon ), typeof( Daemon ), typeof( DemonKnight ), typeof( Devourer ), typeof( Gibberling ), typeof( HordeMinion ), typeof( IceFiend ), typeof( Imp ), typeof( Impaler ), typeof( Ravager ) ),
                    new SlayerEntry( SlayerName.GargoylesFoe, typeof( FireGargoyle ), typeof( Gargoyle ), typeof( StoneGargoyle ) ),
                    new SlayerEntry( SlayerName.BalronDamnation, typeof( Balron ) )
                };

            /*abyss.Super = new SlayerEntry( SlayerName.Exorcism, typeof( Daemon ), typeof( SummonedDaemon ), typeof( Gargoyle ), typeof( StoneGargoyle ), typeof( FireGargoyle ) ); // No balron?
			abyss.Entries = new SlayerEntry[]
				{
					new SlayerEntry( SlayerName.DaemonDismissal, typeof( Daemon ), typeof( SummonedDaemon ) ),
					new SlayerEntry( SlayerName.GargoylesFoe, typeof( Gargoyle ), typeof( StoneGargoyle ), typeof( FireGargoyle ) ),
					new SlayerEntry( SlayerName.BalronDamnation, typeof( Balron ) )
				};*/

            arachnid.Opposition = reptilian;
            arachnid.FoundOn = new Type[] { typeof(AncientWyrm), typeof(Dragon), typeof(OphidianMatriarch), typeof(ShadowWyrm) };
            arachnid.Super = new SlayerEntry(SlayerName.ArachnidDoom, typeof(DreadSpider), typeof(FrostSpider), typeof(GiantBlackWidow), typeof(Mephitis), typeof(Scorpion), typeof(TerathanDrone), typeof(TerathanMatriarch), typeof(TerathanWarrior));
            arachnid.Entries = new SlayerEntry[]
                {
                    new SlayerEntry( SlayerName.ScorpionsBane, typeof( Scorpion ) ),
                    new SlayerEntry( SlayerName.SpidersDeath, typeof( DreadSpider ), typeof( FrostSpider ), typeof( GiantBlackWidow ), typeof( GiantSpider ) ),
                    new SlayerEntry( SlayerName.Terathan, typeof( TerathanAvenger ), typeof( TerathanDrone ), typeof( TerathanMatriarch ), typeof( TerathanWarrior ) )
                };

            reptilian.Opposition = arachnid;
            reptilian.FoundOn = new Type[] { typeof(TerathanAvenger), typeof(TerathanMatriarch) };
            reptilian.Super = new SlayerEntry(SlayerName.ReptilianDeath, typeof(AncientWyrm), typeof(Dragon), typeof(Drake), typeof(GiantIceWorm), typeof(IceSerpent), typeof(GiantSerpent), typeof(IceSnake), typeof(LavaSerpent), typeof(LavaSnake), typeof(Lizardman), typeof(OphidianArchmage), typeof(OphidianKnight), typeof(OphidianMage), typeof(OphidianMatriarch), typeof(OphidianWarrior), typeof(SerpentineDragon), typeof(ShadowWyrm), typeof(SilverSerpent), typeof(SkeletalDragon), typeof(Snake), typeof(SwampDragon), typeof(WhiteWyrm), typeof(Wyvern));
            reptilian.Entries = new SlayerEntry[]
                {
                    new SlayerEntry( SlayerName.DragonSlaying, typeof( AncientWyrm ), typeof( Dragon ), typeof( Drake ), typeof( SerpentineDragon ), typeof( ShadowWyrm ), typeof( SkeletalDragon ), typeof( SwampDragon ), typeof( WhiteWyrm ), typeof( Wyvern ) ),
                    new SlayerEntry( SlayerName.LizardmanSlaughter, typeof( Lizardman ) ),
                    new SlayerEntry( SlayerName.Ophidian, typeof( OphidianArchmage ), typeof( OphidianKnight ), typeof( OphidianMage ), typeof( OphidianMatriarch ), typeof( OphidianWarrior ) ),
                    new SlayerEntry( SlayerName.SnakesBane, typeof( IceSerpent ), typeof( GiantIceWorm ), typeof( GiantSerpent ), typeof( IceSnake ), typeof( LavaSerpent ), typeof( LavaSnake ), typeof( SilverSerpent ), typeof( Snake ) )
                };

            m_Groups = new SlayerGroup[]
                {
                    humanoid,
                    undead,
                    elemental,
                    abyss,
                    arachnid,
                    reptilian
                };

            m_TotalEntries = CompileEntries(m_Groups);
        }

        private static SlayerEntry[] CompileEntries(SlayerGroup[] groups)
        {
            SlayerEntry[] entries = new SlayerEntry[27];

            for (int i = 0; i < groups.Length; ++i)
            {
                SlayerGroup g = groups[i];

                g.Super.Group = g;

                entries[(int)g.Super.Name] = g.Super;

                for (int j = 0; j < g.Entries.Length; ++j)
                {
                    g.Entries[j].Group = g;
                    entries[(int)g.Entries[j].Name] = g.Entries[j];
                }
            }
            /* ** DELETE ** 
					// Adam: test
					{
						Console.WriteLine("count" + entries.Length.ToString());
						for (int ix=0; ix < entries.Length; ix++)
						{
							//Console.WriteLine("entries:" + entries[ix].ToString());
							if ((entries[ix] as SlayerEntry) != null)
							{
								Console.Write("entries: "); 
								Console.WriteLine(entries[ix].Group.Super.Name.ToString());
							}
							else
							{
								Console.Write("entries: "); 
								Console.WriteLine("bogus");
							}
						}
					}
			*/

            return entries;
        }

        private SlayerGroup m_Opposition;
        private SlayerEntry m_Super;
        private SlayerEntry[] m_Entries;
        private Type[] m_FoundOn;

        public SlayerGroup Opposition { get { return m_Opposition; } set { m_Opposition = value; } }
        public SlayerEntry Super { get { return m_Super; } set { m_Super = value; } }
        public SlayerEntry[] Entries { get { return m_Entries; } set { m_Entries = value; } }
        public Type[] FoundOn { get { return m_FoundOn; } set { m_FoundOn = value; } }

        public SlayerGroup()
        {
        }
    }
}
