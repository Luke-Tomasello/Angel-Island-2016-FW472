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

/* Scripts/Engines/ChampionSpawn/ChampLevelData.cs
 *	ChangeLog:
 *	6/26/08, Adam
 *		increase timeouts for the Vampire levels since this is a champ for warriors
 *	5/31/08, Adam
 *		Add SpawnTypes.Vampire
 *  4/4/07, Adam
 *      Change "BongMagi" to "BoneMagi" (lol)
 *  3/16/07, Adam
 *      Add new SpawnTypes.Pirate
 *	11/01/2006, plasms
 *		Decreased big champ MaxSpawn to 1/4
 *	10/29/2006, plasma
 *		 Increased AI Guard spawn range from 6 to 12
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/
using System;
using System.Collections;

namespace Server.Engines.ChampionSpawn
{
    [Flags]
    public enum SpawnFlags
    {
        None = 0x0,
        FactoryMobile = 0x01,
        SpawnFar = 0x02,     // spawn to the outer rim of the range
        SpawnNear = 0x04,     // spawn nearer the spawner
    }

    // Pla: Core champion spawn class.  
    public class ChampLevelData
    {
        // Enum of spawn types to provide nice [props 
        public enum SpawnTypes
        {
            Abyss,
            Arachnid,
            ColdBlood,
            ForestLord,
            FrozenHost,
            UnholyTerror,
            VerminHorde,
            Mini_Deceit,
            Mini_Destard,
            Mini_Hythloth,
            Mini_Ice,
            Mini_Wind,
            Mini_Wrong,
            AI_Escape,
            AI_Guard,
            QuickClanOrcWar,
            ClanOrcWar,
            Pirate,
            Bob,
            Vampire,
            KinCity
        }

        // Members
        public string[] Monsters;               // Monster array
        public int m_MaxKills;                  // max kills
        public int m_MaxMobs;                   // max mobiles at once
        public int m_MaxSpawn;                  // spawn amount
        public int m_MaxRange;                  // max range from centre
        public TimeSpan m_SpawnDelay;           // spawn delay
        public TimeSpan m_ExpireDelay;          // level down delay
        public SpawnFlags m_Flags;          // is this mobile created by a factory?

        // Properties

        // Constructors
        public ChampLevelData(int max_kills, int max_mobs, int max_spawn, int max_range, TimeSpan spawn_delay, TimeSpan expire_delay, SpawnFlags flags, String[] monsters)
        {
            //assign values
            Monsters = monsters;
            m_MaxKills = max_kills;
            m_MaxMobs = max_mobs;
            m_MaxSpawn = max_spawn;
            m_MaxRange = max_range;
            m_SpawnDelay = spawn_delay;
            m_ExpireDelay = expire_delay;
            m_Flags = flags;
        }

        // this is called from the engine's deserialize to create a new set of 
        // levels based upon the serialized data
        public ChampLevelData(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Flags = (SpawnFlags)reader.ReadInt();
                        goto case 0;    // skip case 1
                    }
                case 1:
                    {
                        bool unused = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        // read in a seriliased level !
                        Monsters = new string[reader.ReadInt()];
                        for (int i = 0; i < Monsters.Length; ++i)
                            Monsters[i] = reader.ReadString();

                        m_MaxKills = reader.ReadInt();
                        m_MaxMobs = reader.ReadInt();
                        m_MaxSpawn = reader.ReadInt();
                        m_MaxRange = reader.ReadInt();
                        m_SpawnDelay = reader.ReadTimeSpan();
                        m_ExpireDelay = reader.ReadTimeSpan();
                        break;
                    }

            }

        }

        public void Serialize(GenericWriter writer)
        {
            // serialize level data
            writer.Write((int)2);               // version number

            // version 2
            writer.Write((int)m_Flags);             // spawn preferences

            // version 1
            //writer.Write(m_FactoryMobile);          // is this mobile created by a factory?

            writer.Write((int)Monsters.Length); // write amount of levels
            for (int i = 0; i < Monsters.Length; ++i)   // write monster array
                writer.Write((string)Monsters[i]);

            writer.Write(m_MaxKills);                   // write level data
            writer.Write(m_MaxMobs);
            writer.Write(m_MaxSpawn);
            writer.Write(m_MaxRange);
            writer.Write(m_SpawnDelay);
            writer.Write(m_ExpireDelay);

        }

        public Type GetRandomType()
        {
            // Select a monster at random from the array			
            return ScriptCompiler.FindTypeByName((string)Monsters[Utility.Random(Monsters.Length)]);
        }

        // Static spawn generation funciton.
        // Create your spawns here!
        public static ArrayList CreateSpawn(SpawnTypes type)
        {
            ArrayList temp = new ArrayList();
            switch (type)
            {
                // Big champs first.
                // To emulate the original big champs exactly, we need 16 levels of spawn.
                // This is because the original champs were actually 16 levels, one for each red skull.
                // The spawns have 4 "big" levels split into 16 sub levels with this distribution:
                // Level 1 : 5 levels
                // Level 2 : 4 levels
                // Level 3 : 4 levels
                // Level 4 : 3 Levels					
                #region big champs
                case SpawnTypes.Abyss:
                    {
                        ///////// Abyss //////////////////////////////		   kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------					
                        for (int i = 1; i <= 5; ++i)    // Level " 1 "
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Mongbat", "Imp" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2 "
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Gargoyle", "Harpy" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FireGargoyle", "StoneGargoyle" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Daemon", "Succubus" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Semidar" }));
                        break;
                    }
                case SpawnTypes.Arachnid:
                    {
                        ///////// Arachnid //////////////////////////			kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Scorpion", "GiantSpider" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "TerathanDrone", "TerathanWarrior" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "DreadSpider", "TerathanMatriarch" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PoisonElemental", "TerathanAvenger" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Mephitis" }));
                        break;
                    }
                case SpawnTypes.ColdBlood:
                    {
                        ////////// Cold Blood ///////////////////////		kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Lizardman", "Snake" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "LavaLizard", "OphidianWarrior" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Drake", "OphidianArchmage" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Dragon", "OphidianKnight" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Rikktor" }));
                        break;
                    }
                case SpawnTypes.ForestLord:
                    {
                        ///////// Forest Lord //////////////////////     kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Pixie", "ShadowWisp" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Kirin", "Wisp" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Centaur", "Unicorn" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "EtherealWarrior", "SerpentineDragon" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "LordOaks" }));
                        break;
                    }
                case SpawnTypes.FrozenHost:
                    {
                        ///////// Frozen Host /////////////////////       kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FrostOoze", "FrostSpider" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FrostTroll", "IceSerpent" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "SnowElemental", "FrostNymph" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "IceFiend", "WhiteWyrm" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Azothu" }));
                        break;
                    }
                case SpawnTypes.UnholyTerror:
                    {
                        ///////// UnholyTerror ////////////////////    kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level "1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Bogle", "Ghoul", "Shade", "Spectre", "Wraith" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "BoneMagi", "Mummy", "SkeletalMage" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "BoneKnight", "BoneMagiLord", "SkeletalKnight" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "BoneKnightLord", "RottingCorpse" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Neira" }));
                        break;
                    }
                case SpawnTypes.VerminHorde:
                    {
                        ///////// Vermin Horde ///////////////////    kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "GiantRat", "Slime" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Ratman", "DireWolf" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "HellHound", "RatmanMage" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "RatmanArcher", "SilverSerpent" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Barracoon" }));

                        break;
                    }

                /*
				 * Increase range to 75
				 * Reduce the number of mobs spawned at once by 1/2 when Pirates are involved.
				 */
                case SpawnTypes.Pirate:
                    {
                        ///////// Pirate at sea //////////////////    kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.SpawnFar, new string[] { "WaterElemental", "SeaSerpent" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.SpawnFar, new string[] { "SeaSerpent", "Kraken" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25 / 2, 6, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.FactoryMobile, new string[] { "PirateDeckHand", "PirateWench" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17 / 2, 4, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.FactoryMobile, new string[] { "PirateWench", "Pirate" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.FactoryMobile, new string[] { "PirateChamp" }));

                        break;
                    }

                case SpawnTypes.Bob:
                    {
                        ///////// Bobs in Jhelom /////////
                        // kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.FactoryMobile, new string[] { "Gypsy" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.FactoryMobile, new string[] { "Brigand", "NightMare" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25 / 2, 6, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.FactoryMobile, new string[] { "Brigand", "EvilMageLord" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17 / 2, 4, 75, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.FactoryMobile, new string[] { "GolemController" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.FactoryMobile, new string[] { "TheOneBob" }));

                        break;
                    }

                case SpawnTypes.Vampire:
                    {
                        ///////// Vampires in the dead forset of Ilshenar /////////
                        // kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(40, 40, 10, 20, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "VampireBat" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(38, 38, 9, 20, TimeSpan.Zero, TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "VampireBat", "WalkingDead" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(25, 25 / 2, 6, 20, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "WalkingDead", "WalkingDead" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(17, 17 / 2, 4, 20, TimeSpan.Zero, TimeSpan.FromMinutes(50), SpawnFlags.None, new string[] { "Vampire" }));

                        //Champion!
                        temp.Add(new ChampLevelData(1, 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(60), SpawnFlags.None, new string[] { "VladDracula" }));

                        break;
                    }

                #endregion

                // Now for the mini champs.
                // These are just 3 levels with the fourth spawning a single mob, like a  champ
                #region mini champs
                case SpawnTypes.Mini_Deceit:
                    {
                        ///////// Decei! ///////////////////////////////    kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  --------------------------------------------------------------------------------------------------------------------------------------------------------------
                        temp.Add(new ChampLevelData(9, 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Skeleton" }));
                        temp.Add(new ChampLevelData(15, 4, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "SkeletalKnight", "BoneKnight" }));
                        temp.Add(new ChampLevelData(16, 2, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "SkeletalMage", "BoneMagi" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "BoneDemon", "SkeletalDragon" }));
                        break;
                    }
                case SpawnTypes.Mini_Destard:
                    {
                        ///////// Destard ////////////////////////////    kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ----------------------------------------------------------------------------------------------------------------------------------------------------------------
                        temp.Add(new ChampLevelData(9, 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Lizardman", "OphidianWarrior" }));
                        temp.Add(new ChampLevelData(15, 4, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "Drake", "Wyvern" }));
                        temp.Add(new ChampLevelData(16, 2, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Dragon", "OphidianKnight" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "AncientWyrm" }));
                        break;
                    }
                case SpawnTypes.Mini_Hythloth:
                    {
                        ///////// Hyloth ////////////////////////////      kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  --------------------------------------------------------------------------------------------------------------------------------------------------------------
                        temp.Add(new ChampLevelData(9, 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Imp", "HellHound" }));
                        temp.Add(new ChampLevelData(15, 4, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "Gargoyle", "ChaosDaemon" }));
                        temp.Add(new ChampLevelData(16, 2, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Succubus", "Daemon" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "Balron" }));
                        break;
                    }
                case SpawnTypes.Mini_Ice:
                    {
                        ///////// Ice !  /////////////////////////////////      kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ---------------------------------------------------------------------------------------------------------------------------------------------------------------
                        temp.Add(new ChampLevelData(9, 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FrostOoze", "FrostSpider" }));
                        temp.Add(new ChampLevelData(15, 4, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "IceElemental", "FrostSpider" }));
                        temp.Add(new ChampLevelData(16, 2, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "IceFiend", "FrostNymph" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "ArcticOgreLord" }));
                        break;
                    }
                case SpawnTypes.Mini_Wind:              // Wind has a different expire delay for level 4
                    {
                        ///////// Wind ////////////////////////////////      kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ---------------------------------------------------------------------------------------------------------------------------------------------------------------
                        temp.Add(new ChampLevelData(9, 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "EvilMage" }));
                        temp.Add(new ChampLevelData(15, 4, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "Lich", "EvilMageLord" }));
                        temp.Add(new ChampLevelData(16, 2, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "LichLord", "CouncilMember" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "AncientLich" }));
                        break;
                    }
                case SpawnTypes.Mini_Wrong:
                    {
                        ///////// Wrong ///////////////////////////////      kills, Mobs, Spawn, Range, Spawn Delay, Level Expire Delay, Monster Array
                        ///////////////////////////////////////////////////  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        temp.Add(new ChampLevelData(9, 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Zombie", "HeadlessOne", "Slime" }));
                        temp.Add(new ChampLevelData(15, 4, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "GoreFiend", "FleshGolem" }));
                        temp.Add(new ChampLevelData(16, 2, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "BloodElemental", "RottingCorpse" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "FleshRenderer" }));
                        break;
                    }
                #endregion

                // Angel Island prison system spawns 			
                // These guys take some of thir spawn level settings
                // from statics in CoreAI
                #region AI Level system
                case SpawnTypes.AI_Escape:
                    {//															k														m  s  r
                        temp.Add(new ChampLevelData(CoreAI.SpiritFirstWaveNumber, 5, 5, 18, TimeSpan.Zero, TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "Spirit" }));
                        temp.Add(new ChampLevelData(CoreAI.SpiritSecondWaveNumber, 5, 5, 18, TimeSpan.Zero, TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "VengefulSpirit" }));
                        temp.Add(new ChampLevelData(CoreAI.SpiritThirdWaveNumber, 5, 5, 18, TimeSpan.Zero, TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "Soul" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 18, TimeSpan.Zero, TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "AngelofJustice" }));
                        break;
                    }
                case SpawnTypes.AI_Guard:
                    {   //														k  m s  r
                        temp.Add(new ChampLevelData(5, 5, 5, 12, TimeSpan.Zero, TimeSpan.FromMinutes(CoreAI.GuardSpawnExpireDelay), SpawnFlags.None, new string[] { "AIPostGuard" }));
                        temp.Add(new ChampLevelData(1, 1, 1, 12, TimeSpan.Zero, TimeSpan.FromMinutes(CoreAI.GuardSpawnExpireDelay), SpawnFlags.None, new string[] { "AIGuardCaptain" }));
                        break;
                    }

                #endregion

                #region Kin

                //Kin city consists purely of golem controllers.  
                case SpawnTypes.KinCity:
                    {
                        temp.Add(new ChampLevelData(40, 40, 5, 40, TimeSpan.Zero, TimeSpan.FromHours(1), SpawnFlags.None, new string[] { "GolemController" }));
                        temp.Add(new ChampLevelData(40, 40, 5, 40, TimeSpan.Zero, TimeSpan.FromHours(1), SpawnFlags.None, new string[] { "GolemController" }));
                        break;
                    }

                #endregion

                #region Custom Spawner
                case SpawnTypes.QuickClanOrcWar:
                    {
                        temp.Add(new ChampLevelData(max_kills: 02, max_mobs: 02, max_spawn: 1, max_range: 2, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 02, max_mobs: 02, max_spawn: 1, max_range: 2, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 02, max_mobs: 02, max_spawn: 1, max_range: 2, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 02, max_mobs: 02, max_spawn: 1, max_range: 2, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 02, max_mobs: 02, max_spawn: 1, max_range: 2, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        // boss
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 2, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrcBoss" }));
                        break;
                    }
                case SpawnTypes.ClanOrcWar:
                    {   // guardians
                        temp.Add(new ChampLevelData(max_kills: 02, max_mobs: 10, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        // main levels
                        temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 30, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 30, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 20, max_mobs: 25, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 15, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrc" }));
                        // boss
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 4, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), flags: SpawnFlags.None, monsters: new string[] { "ClanOrcBoss" }));
                        break;
                    }
            }
            #endregion

            return temp;
        }

    }

}
