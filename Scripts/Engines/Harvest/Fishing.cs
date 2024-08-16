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

/* Scripts/Engines/Harvest/Fishing.cs
 * ChangeLog
 *	9/26/10, adam
 *		Increase leveled sos loot drop to (level * 8) so that a level 1 sos is 8% chance at a rare and level 5 is a 40% chance
 *		Also increase weapon drop a bit
 *	9/23/10, Adam
 *		Add new fishing system and bonus loot
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/16/04, mith
 *		Modified FinishHarvesting() to increase the range. 
 *			This allows for fishing while moving slow forward. The range is reset after FinishHarvesting() is done.
 */

using Server.Engines.Quests;
using Server.Engines.Quests.Collector;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines.Harvest
{
    public class Fishing : HarvestSystem
    {
        private static Fishing m_System;

        public static Fishing System
        {
            get
            {
                if (m_System == null)
                    m_System = new Fishing();

                return m_System;
            }
        }

        private HarvestDefinition m_Definition;

        public HarvestDefinition Definition
        {
            get { return m_Definition; }
        }

        private Fishing()
        {
            HarvestResource[] res;
            HarvestVein[] veins;

            #region Fishing
            HarvestDefinition fish = new HarvestDefinition();

            // Resource banks are every 8x8 tiles
            fish.BankWidth = 8;
            fish.BankHeight = 8;

            // Every bank holds from 5 to 15 fish
            fish.MinTotal = 5;
            fish.MaxTotal = 15;

            // A resource bank will respawn its content every 10 to 20 minutes
            fish.MinRespawn = TimeSpan.FromMinutes(10.0);
            fish.MaxRespawn = TimeSpan.FromMinutes(20.0);

            // Skill checking is done on the Fishing skill
            fish.Skill = SkillName.Fishing;

            // Set the list of harvestable tiles
            fish.Tiles = m_WaterTiles;
            fish.RangedTiles = true;

            // Players must be within 4 tiles to harvest
            fish.MaxRange = 4;

            // One fish per harvest action
            fish.ConsumedPerHarvest = 1;
            fish.ConsumedPerFeluccaHarvest = 1;

            // The fishing
            fish.EffectActions = new int[] { 12 };
            fish.EffectSounds = new int[0];
            fish.EffectCounts = new int[] { 1 };
            fish.EffectDelay = TimeSpan.Zero;
            fish.EffectSoundDelay = TimeSpan.FromSeconds(8.0);

            fish.NoResourcesMessage = 503172; // The fish don't seem to be biting here.
            fish.FailMessage = 503171; // You fish a while, but fail to catch anything.
            fish.TimedOutOfRangeMessage = 500976; // You need to be closer to the water to fish!
            fish.OutOfRangeMessage = 500976; // You need to be closer to the water to fish!
            fish.PackFullMessage = 503176; // You do not have room in your backpack for a fish.
            fish.ToolBrokeMessage = 503174; // You broke your fishing pole.

            res = new HarvestResource[]
                {
                    new HarvestResource( 00.0, 00.0, 100.0, 1043297, typeof( Fish ) )
                };

            veins = new HarvestVein[]
                {
                    new HarvestVein( 100.0, 0.0, res[0], null )
                };

            fish.Resources = res;
            fish.Veins = veins;

            m_Definition = fish;
            Definitions.Add(fish);
            #endregion
        }

        public override void OnConcurrentHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            from.SendLocalizedMessage(500972); // You are already fishing.
        }

        private class MutateEntry
        {
            public double m_ReqSkill, m_MinSkill, m_MaxSkill;
            public bool m_DeepWater;
            public Type[] m_Types;

            public MutateEntry(double reqSkill, double minSkill, double maxSkill, bool deepWater, params Type[] types)
            {
                m_ReqSkill = reqSkill;
                m_MinSkill = minSkill;
                m_MaxSkill = maxSkill;
                m_DeepWater = deepWater;
                m_Types = types;
            }
        }

        private static MutateEntry[] m_MutateTable = new MutateEntry[]
            {
                new MutateEntry(  80.0,  80.0,  4080.0,  true, typeof( SpecialFishingNet ) ),
                new MutateEntry(  80.0,  80.0,  4080.0,  true, typeof( BigFish ) ),
                new MutateEntry(  90.0,  80.0,  4080.0,  true, typeof( TreasureMap ) ),
                new MutateEntry( 100.0,  80.0,  4080.0,  true, typeof( MessageInABottle ) ),
                new MutateEntry(   0.0, 125.0, -2375.0, false, typeof( PrizedFish ), typeof( WondrousFish ), typeof( TrulyRareFish ), typeof( PeculiarFish ) ),
                new MutateEntry(   0.0, 105.0,  -420.0, false, typeof( Boots ), typeof( Shoes ), typeof( Sandals ), typeof( ThighBoots ) ),
                new MutateEntry(   0.0, 200.0,  -200.0, false, new Type[1]{ null } )
            };

        public override bool SpecialHarvest(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
        {
            PlayerMobile player = from as PlayerMobile;

            if (player != null)
            {
                QuestSystem qs = player.Quest;

                if (qs is CollectorQuest)
                {
                    QuestObjective obj = qs.FindObjective(typeof(FishPearlsObjective));

                    if (obj != null && !obj.Completed)
                    {
                        if (Utility.RandomDouble() < 0.5)
                        {
                            player.SendLocalizedMessage(1055086, "", 0x59); // You pull a shellfish out of the water, and find a rainbow pearl inside of it.

                            obj.CurProgress++;
                        }
                        else
                        {
                            player.SendLocalizedMessage(1055087, "", 0x2C); // You pull a shellfish out of the water, but it doesn't have a rainbow pearl.
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public override Type MutateType(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            bool deepWater = SpecialFishingNet.FullValidation(map, loc.X, loc.Y);

            double skillBase = from.Skills[SkillName.Fishing].Base;
            double skillValue = from.Skills[SkillName.Fishing].Value;

            for (int i = 0; i < m_MutateTable.Length; ++i)
            {
                MutateEntry entry = m_MutateTable[i];

                // no deepwater check if you are in a fishing hotspot
                if (GoodFishingHere(from, new Point2D(loc)) == false)
                    if (!deepWater && entry.m_DeepWater)
                        continue;

                if (skillBase >= entry.m_ReqSkill)
                {
                    double chance = (skillValue - entry.m_MinSkill) / (entry.m_MaxSkill - entry.m_MinSkill);

                    // check for sea quest bonus
                    if (GoodFishingHere(from, new Point2D(loc)))
                        chance *= GoodFishingBonus(from, new Point2D(loc));

                    if (chance > Utility.RandomDouble())
                        return entry.m_Types[Utility.Random(entry.m_Types.Length)];
                }
            }

            return type;
        }

        public override object[] ObjectArgs(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            if (GoodFishingHere(from, new Point2D(loc)))
            {
                return new object[] { typeof(MessageInABottle), GoodFishingLevel(from, new Point2D(loc)) };
            }
            return null;
        }

        private bool GoodFishingHere(Mobile from, Point2D loc)
        {
            if (from != null)
            {
                Multis.BaseBoat boat = Multis.BaseBoat.FindBoatAt(from);
                if (boat != null)
                    if (boat.Fishing != null && boat.Fishing.Running && boat.Fishing.GoodFishingHere(loc))
                        return true;
            }

            return false;
        }

        private bool GoodFishingNear(Mobile from, Point2D loc)
        {
            if (from != null)
            {
                Multis.BaseBoat boat = Multis.BaseBoat.FindBoatAt(from);
                if (boat != null)
                    if (boat.Fishing != null && boat.Fishing.Running && boat.Fishing.GoodFishingNear(loc))
                        return true;
            }

            return false;
        }

        private bool GoodFishingAdvice(Mobile from, string text)
        {
            if (from != null)
            {
                Multis.BaseBoat boat = Multis.BaseBoat.FindBoatAt(from);
                if (boat != null)
                    boat.TillerMan.Say(false, text);
            }

            return false;
        }

        private double GoodFishingBonus(Mobile from, Point2D loc)
        {
            if (from != null)
            {
                Multis.BaseBoat boat = Multis.BaseBoat.FindBoatAt(from);
                if (boat != null)
                    if (boat.Fishing != null && boat.Fishing.Running && boat.Fishing.GoodFishingHere(loc))
                        switch (boat.Fishing.Level)
                        {
                            case 1:
                                return 2.0;
                            case 2:
                                return 3.0;
                            case 3:
                                return 4.0;
                            case 4:
                                return 5.0;
                            case 5:
                                return 6.0;
                        }

            }

            return 1.0;
        }

        private int GoodFishingLevel(Mobile from, Point2D loc)
        {
            if (from != null)
            {
                Multis.BaseBoat boat = Multis.BaseBoat.FindBoatAt(from);
                if (boat != null)
                    if (boat.Fishing != null && boat.Fishing.Running && boat.Fishing.GoodFishingHere(loc))
                        return boat.Fishing.Level;
            }

            return 0;
        }

        private static Map SafeMap(Map map)
        {
            if (map == null || map == Map.Internal)
                return Map.Trammel;

            return map;
        }

        public override bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            Container pack = from.Backpack;

            if (pack != null)
            {
                Item[] messages = pack.FindItemsByType(typeof(SOS));

                for (int i = 0; i < messages.Length; ++i)
                {
                    SOS sos = (SOS)messages[i];

                    if (from.Map == sos.TargetMap && from.InRange(sos.TargetLocation, 60))
                        return true;
                }
            }

            bool result = base.CheckResources(from, tool, def, map, loc, timed);

            if (result == false)    // no fish in this bank
                if (GoodFishingHere(from, new Point2D(loc)) == false && GoodFishingNear(from, new Point2D(loc)) == true)    // too far away and no fish
                    GoodFishingAdvice(from, "Ar, why ye be fishing over there?");
                else
                    if (GoodFishingHere(from, new Point2D(loc)) == true)        // right spot, but no fish
                    switch (Utility.Random(4))
                    {
                        case 0:
                            GoodFishingAdvice(from, "Maybe try fishing fore and aft?"); // try another spot
                            break;
                        case 1:
                            GoodFishingAdvice(from, "Have you tried port side?");       // try another spot
                            break;
                        case 2:
                            GoodFishingAdvice(from, "Have you tried the starboard side?");  // try another spot
                            break;
                        case 3:
                            GoodFishingAdvice(from, "Ar, thar's got to be fish here somewhere!");   // try another spot
                            break;
                    }

            return result;
        }

        public override Item Construct(Type type, Mobile from, object[] args)
        {
            if (type == typeof(TreasureMap))
                return new TreasureMap(1, from.Map == Map.Felucca ? Map.Felucca : Map.Trammel);
            else if (type == typeof(MessageInABottle))
            {   // build a level N mib based on the good fishing map level
                if (args != null && args.Length == 2 && args[0] == typeof(MessageInABottle) && args[1] is int)
                    return new MessageInABottle(SafeMap(from.Map), (int)args[1]);
                return new MessageInABottle(SafeMap(from.Map));
            }

            Container pack = from.Backpack;

            if (pack != null)
            {
                Item[] messages = pack.FindItemsByType(typeof(SOS));

                for (int i = 0; i < messages.Length; ++i)
                {
                    SOS sos = (SOS)messages[i];

                    if (from.Map == sos.TargetMap && from.InRange(sos.TargetLocation, 60))
                    {
                        Item preLoot = null;

                        switch (Utility.Random(7))
                        {
                            case 0: // Body parts
                                {
                                    int[] list = new int[]
                                    {
                                        0x1CDD, 0x1CE5, // arm
										0x1CE0, 0x1CE8, // torso
										0x1CE1, 0x1CE9, // head
										0x1CE2, 0x1CEC // leg
									};

                                    preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                    break;
                                }
                            case 1: // Bone parts
                                {
                                    int[] list = new int[]
                                    {
                                        0x1AE0, 0x1AE1, 0x1AE2, 0x1AE3, 0x1AE4, // skulls
										0x1B09, 0x1B0A, 0x1B0B, 0x1B0C, 0x1B0D, 0x1B0E, 0x1B0F, 0x1B10, // bone piles
										0x1B15, 0x1B16 // pelvis bones
									};

                                    preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                    break;
                                }
                            case 2: // Paintings and portraits
                                {
                                    preLoot = new ShipwreckedItem(Utility.Random(0xE9F, 10));
                                    break;
                                }
                            case 3: // Pillows
                                {
                                    preLoot = new ShipwreckedItem(Utility.Random(0x13A4, 11));
                                    break;
                                }
                            case 4: // Shells
                                {
                                    preLoot = new ShipwreckedItem(Utility.Random(0xFC4, 9));
                                    break;
                                }
                            case 5: // Misc
                                {
                                    int[] list = new int[]
                                    {
                                        0x1EB5, // unfinished barrel
										0xA2A, // stool
										0xC1F, // broken clock
										0x1047, 0x1048, // globe
										0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4 // barrel staves
									};

                                    preLoot = new ShipwreckedItem(Utility.RandomList(list));
                                    break;
                                }
                        }

                        if (preLoot != null)
                            return preLoot;

                        Container chest = null;

                        if (sos.Level >= 4 && Utility.RandomChance(10))
                            chest = new MetalGoldenChest();
                        else
                            chest = new WoodenChest();

                        // regular sos's have a chance at a 1, 2 or 3. high level sos's are high level chests
                        int level = (sos.Level >= 4) ? sos.Level : Utility.RandomMinMax(1, 3);
                        TreasureMapChest.Fill((chest as LockableContainer), level);

                        // add 50% more gold since we are decreasing the chance to get rares
                        Item[] golds = chest.FindItemsByType(typeof(Gold));
                        if (golds != null && golds.Length > 0)
                        {
                            int total = 0;
                            for (int tx = 0; tx < golds.Length; tx++)
                                total += (golds[tx] as Gold).Amount;

                            // add 50% more gold
                            chest.DropItem(new Gold(total / 2));

                            // add some cursed gold
                            chest.DropItem(new CursedGold(total / 8));
                        }

                        // adjust high end loot by removing high end weapons and armor
                        if (sos.Level >= 4)
                        {
                            // trim the high end weps and armor so as not to to give away all the highend weapons usually reserved for Treasure Map hunters
                            Item[] lootz = chest.FindItemsByType(new Type[] { typeof(EnchantedScroll), typeof(BaseArmor), typeof(BaseWeapon) });
                            int LootzKeep = level == 4 ? Utility.RandomList(2) + 2 : Utility.RandomList(3) + 2;

                            if (lootz != null && lootz.Length > LootzKeep)
                            {
                                // remove some items as we're dropping too much for the sea chest
                                int toDel = lootz.Length - LootzKeep;
                                for (int ox = 0; ox < toDel; ox++)
                                {
                                    Item dx = lootz[ox];
                                    chest.RemoveItem(dx);
                                    dx.Delete();
                                }
                            }
                        }

                        // TODO: Are there chances on this? All MIB's I've done had nets..
                        chest.DropItem(new SpecialFishingNet());

                        // add Good Fishing bonus lootz
                        if (Utility.RandomChance(sos.Level * 8))
                        {
                            switch (sos.Level)
                            {
                                case 1:
                                    if (Utility.RandomChance(10))
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x0DBB), // 3515 (0x0DBB) Seaweed
											new AddonDeed(0x0C2E), // 3118 (0x0C2E) debris
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    else
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x154D),		// 5453 (0x054D) water barrel � empty or filled we don't have them on ai yet.
											new AddonDeed(0x0DC9),		// 3529 (0x0DC9) fishing net � unhued and oddly shaped 
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    break;
                                case 2:
                                    if (Utility.RandomChance(10))
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x1E9A), // 7834 (0x1E9A) hook
											new AddonDeed(0x1E9D), // 7837 (0x1E9D) pulleys
											new AddonDeed(0x1E9E), // 7838 (0x1E9E) Pulley
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    else
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x1EA0), // 7840 (0x1EA0) Rope
											new AddonDeed(0x1EA9,Direction.South), // 7849 (0x1EA9) Winch � south
											new AddonDeed(0x1EAC,Direction.East), // 7852 (0x1EAC) Winch � east
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    break;
                                case 3:
                                    if (Utility.RandomChance(10))
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x0FCD,Direction.South), // 4045 (0x0FCD) string of shells � south
											new AddonDeed(0x0FCE,Direction.South), // 4046 (0x0FCE) string of shells � south
											new AddonDeed(0x0FCF,Direction.South), // 4047 (0x0FCF) � string of shells � south
											new AddonDeed(0x0FD0,Direction.South), // 4048 (0x0FD0) � string of shells � south
											new AddonDeed(0x0FD1,Direction.East), // 4049 (0x0FD1) � string of shells � east
											new AddonDeed(0x0FD2,Direction.East), // 4050 (0x0FD2) � string of shells � east
											new AddonDeed(0x0FD3,Direction.East), // 4051 (0x0FD3) � string of shells � east
											new AddonDeed(0x0FD4,Direction.East), // 4052 (0x0FD4) � string of shells � east
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    else
                                    {
                                        switch (Utility.Random(4))
                                        {
                                            case 0:
                                                // 4099 (0x1003) � Spittoon
                                                chest.DropItem(new ShipwreckedItem(0x1003));
                                                break;
                                            case 1:
                                                // 4091 (0x0FFB) � Skull mug 1
                                                chest.DropItem(new ShipwreckedItem(0x0FFB));
                                                break;
                                            case 2:
                                                // 4092 (0x0FFC) � Skull mug 2
                                                chest.DropItem(new ShipwreckedItem(0x0FFC));
                                                break;
                                            case 3:
                                                // 3700 (0x0E74) Cannon Balls
                                                chest.DropItem(new AddonDeed(0x0E74));
                                                break;
                                        }
                                    }
                                    break;
                                case 4:
                                    if (Utility.RandomChance(10))
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x0C2C), // 3116 (0x0C2C) Ruined Painting
											new AddonDeed(0x0C18), // 3096 (0x0C18) Covered chair - (server birth on osi)
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    else
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new AddonDeed(0x1EA3,Direction.South),	// 7843 (0x1EA3) net � south
											new AddonDeed(0x1EA4,Direction.East),	// 7844 (0x1EA4) net � east
											new AddonDeed(0x1EA5,Direction.South),	// 7845 (0x1EA5) net � south
											new AddonDeed(0x1EA6,Direction.East),	// 7846 (0x1EA6) net � east
										};
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    break;
                                case 5:
                                    if (Utility.RandomChance(10))
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new DarkFlowerTapestrySouthDeed(),
                                            new DarkFlowerTapestryEastDeed(),
                                            new LightTapestrySouthDeed(),
                                            new LightTapestryEastDeed(),
                                        };
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    else
                                    {
                                        Item[] list = new Item[]
                                        {
                                            new DarkTapestrySouthDeed(),
                                            new DarkTapestryEastDeed(),
                                            new LightFlowerTapestrySouthDeed(),
                                            new LightFlowerTapestryEastDeed(),
                                        };
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    break;
                            }
                        }

                        // 1 in 1000 chance at the actual item (rare)
                        if (Utility.RandomChance(.1))
                        {
                            switch (sos.Level)
                            {
                                case 1:
                                    if (Utility.RandomChance(10))
                                    {
                                        int[] list = new int[]
                                    {
                                        0x0DBB, // 3515 (0x0DBB) Seaweed
										0x0C2E, // 3118 (0x0C2E) debris
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    else
                                    {
                                        int[] list = new int[]
                                    {
                                        0x154D,		// 5453 (0x054D) water barrel � empty or filled we don't have them on ai yet.
										0x0DC9,		// 3529 (0x0DC9) fishing net � unhued and oddly shaped 
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    break;
                                case 2:
                                    if (Utility.RandomChance(10))
                                    {
                                        int[] list = new int[]
                                    {
                                        0x1E9A, // 7834 (0x1E9A) hook
										0x1E9D, // 7837 (0x1E9D) pulleys
										0x1E9E, // 7838 (0x1E9E) Pulley
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    else
                                    {
                                        int[] list = new int[]
                                    {
                                        0x1EA0, // 7840 (0x1EA0) Rope
										0x1EA9, // 7849 (0x1EA9) Winch � south
										0x1EAC, // 7852 (0x1EAC) Winch � east
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    break;
                                case 3:
                                    if (Utility.RandomChance(10))
                                    {
                                        int[] list = new int[]
                                    {
                                        0x0FCD, // 4045 (0x0FCD) string of shells � south
										0x0FCE, // 4046 (0x0FCE) string of shells � south
										0x0FCF, // 4047 (0x0FCF) � string of shells � south
										0x0FD0, // 4048 (0x0FD0) � string of shells � south
										0x0FD1, // 4049 (0x0FD1) � string of shells � east
										0x0FD2, // 4050 (0x0FD2) � string of shells � east
										0x0FD3, // 4051 (0x0FD3) � string of shells � east
										0x0FD4, // 4052 (0x0FD4) � string of shells � east
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    else
                                    {
                                        int[] list = new int[]
                                    {
                                        0x1003, // 4099 (0x1003) � Spittoon
										0x0FFB, // 4091 (0x0FFB) � Skull mug 1
										0x0FFC, // 4092 (0x0FFC) � Skull mug 2
										0x0E74, // 3700 (0x0E74) Cannon Balls
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    break;
                                case 4:
                                    if (Utility.RandomChance(10))
                                    {
                                        int[] list = new int[]
                                    {
                                        0x0C2C, // 3116 (0x0C2C) Ruined Painting
										0x0C18, // 3096 (0x0C18) Covered chair - (server birth on osi)
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    else
                                    {
                                        int[] list = new int[]
                                    {
                                        0x1EA3, // 7843 (0x1EA3) net � south
										0x1EA4, // 7844 (0x1EA4) net � east
										0x1EA5, // 7845 (0x1EA5) net � south
										0x1EA6, // 7846 (0x1EA6) net � east
									};
                                        chest.DropItem(new ShipwreckedItem(Utility.RandomList(list)));
                                    }
                                    break;
                                case 5: // same frequent drop
                                    if (Utility.RandomChance(10))
                                    {
                                        Item[] list = new Item[]
                                    {
                                        new DarkFlowerTapestrySouthDeed(),
                                        new DarkFlowerTapestryEastDeed(),
                                        new LightTapestrySouthDeed(),
                                        new LightTapestryEastDeed(),
                                    };
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    else
                                    {
                                        Item[] list = new Item[]
                                    {
                                        new DarkTapestrySouthDeed(),
                                        new DarkTapestryEastDeed(),
                                        new LightFlowerTapestrySouthDeed(),
                                        new LightFlowerTapestryEastDeed(),
                                    };
                                        chest.DropItem(Utility.RandomList(list));
                                    }
                                    break;
                            }
                        }

                        (chest as LockableContainer).Movable = true;
                        (chest as LockableContainer).Locked = false;
                        (chest as LockableContainer).TrapType = TrapType.None;
                        (chest as LockableContainer).TrapPower = 0;
                        (chest as LockableContainer).TrapLevel = 0;

                        sos.Delete();

                        return chest;
                    }
                }
            }

            return base.Construct(type, from, args);
        }

        public override bool Give(Mobile m, Item item, bool placeAtFeet)
        {
            // chests at levels 4&5 are on a kraken
            if (item is TreasureMap || item is MessageInABottle || item is SpecialFishingNet)
            {
                BaseCreature serp;

                // mibs at levels 4&5 are on a kraken
                if ((item is MessageInABottle) && (item as MessageInABottle).Level >= 4)
                    serp = new Kraken();
                else
                {
                    if (0.25 > Utility.RandomDouble())
                        serp = new DeepSeaSerpent();
                    else
                        serp = new SeaSerpent();
                }

                int x = m.X, y = m.Y;

                Map map = m.Map;

                for (int i = 0; map != null && i < 20; ++i)
                {
                    int tx = m.X - 10 + Utility.Random(21);
                    int ty = m.Y - 10 + Utility.Random(21);

                    Tile t = map.Tiles.GetLandTile(tx, ty);

                    if (t.Z == -5 && ((t.ID >= 0xA8 && t.ID <= 0xAB) || (t.ID >= 0x136 && t.ID <= 0x137)) && !Spells.SpellHelper.CheckMulti(new Point3D(tx, ty, -5), map))
                    {
                        x = tx;
                        y = ty;
                        break;
                    }
                }

                serp.MoveToWorld(new Point3D(x, y, -5), map);

                serp.Home = serp.Location;
                serp.RangeHome = 10;

                serp.PackItem(item);

                m.SendLocalizedMessage(503170); // Uh oh! That doesn't look like a fish!

                return true; // we don't want to give the item to the player, it's on the serpent
            }

            if (item is BigFish || item is WoodenChest || item is MetalGoldenChest)
                placeAtFeet = true;

            return base.Give(m, item, placeAtFeet);
        }

        public override void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            if (item is BigFish)
            {
                from.SendLocalizedMessage(1042635); // Your fishing pole bends as you pull a big fish from the depths!
            }
            else if (item is WoodenChest || item is MetalGoldenChest)
            {
                from.SendLocalizedMessage(503175); // You pull up a heavy chest from the depths of the ocean!
            }
            else
            {
                int number;
                string name;

                if (item is BaseMagicFish)
                {
                    number = 1008124;
                    name = "a mess of small fish";
                }
                else if (item is Fish)
                {
                    number = 1008124;
                    name = "a fish";
                }
                else if (item is BaseShoes)
                {
                    number = 1008124;
                    name = item.ItemData.Name;
                }
                else if (item is TreasureMap)
                {
                    number = 1008125;
                    name = "a sodden piece of parchment";
                }
                else if (item is MessageInABottle)
                {
                    number = 1008125;
                    name = "a bottle, with a message in it";
                }
                else if (item is SpecialFishingNet)
                {
                    number = 1008125;
                    name = "a special fishing net"; // TODO: this is just a guess--what should it really be named?
                }
                else
                {
                    number = 1043297;

                    if ((item.ItemData.Flags & TileFlag.ArticleA) != 0)
                        name = "a " + item.ItemData.Name;
                    else if ((item.ItemData.Flags & TileFlag.ArticleAn) != 0)
                        name = "an " + item.ItemData.Name;
                    else
                        name = item.ItemData.Name;
                }

                if (number == 1043297)
                    from.SendLocalizedMessage(number, name);
                else
                    from.SendLocalizedMessage(number, true, name);
            }
        }

        public override void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            base.OnHarvestStarted(from, tool, def, toHarvest);

            int tileID;
            Map map;
            Point3D loc;

            if (GetHarvestDetails(from, tool, toHarvest, out tileID, out map, out loc))
                Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(Splash_Callback), new object[] { loc, map });
        }

        // no fish bite
        public override void OnHarvestFailed(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            // too far away and no fish bite
            if (GoodFishingHere(from, new Point2D(loc)) == false && GoodFishingNear(from, new Point2D(loc)) == true)
                GoodFishingAdvice(from, "Ar, why ye be fishing over there?");
            else // right spot, but no bite
                if (GoodFishingHere(from, new Point2D(loc)) == true && Utility.RandomChance(25))
            {
                switch (Utility.Random(3))
                {
                    case 0: GoodFishingAdvice(from, "Ar, ye be in the right spot there skipper."); break;
                    case 1: GoodFishingAdvice(from, "Methinks that be a good spot."); break;
                    case 2: GoodFishingAdvice(from, "Ar, too bad skipper. Try again."); break;
                }

            }
        }

        private void Splash_Callback(object state)
        {
            object[] args = (object[])state;
            Point3D loc = (Point3D)args[0];
            Map map = (Map)args[1];

            Effects.SendLocationEffect(loc, map, 0x352D, 16, 4);
            Effects.PlaySound(loc, map, 0x364);
        }

        public override object GetLock(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            return this;
        }

        public override bool BeginHarvesting(Mobile from, Item tool)
        {
            if (!base.BeginHarvesting(from, tool))
                return false;

            from.SendLocalizedMessage(500974); // What water do you want to fish in?
            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool)
        {
            if (!base.CheckHarvest(from, tool))
                return false;

            if (from.Mounted)
            {
                from.SendLocalizedMessage(500971); // You can't fish while riding!
                return false;
            }

            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            if (!base.CheckHarvest(from, tool, def, toHarvest))
                return false;

            if (from.Mounted)
            {
                from.SendLocalizedMessage(500971); // You can't fish while riding!
                return false;
            }

            return true;
        }

        public override void FinishHarvesting(Mobile from, Item tool, HarvestDefinition def, object toHarvest, object locked)
        {
            int tmpRangeHolder = def.MaxRange;
            def.MaxRange += 3;
            base.FinishHarvesting(from, tool, def, toHarvest, locked);
            def.MaxRange = tmpRangeHolder;
        }

        private static int[] m_WaterTiles = new int[]
            {
                0x00A8, 0x00AB,
                0x0136, 0x0137,
                0x5797, 0x579C,
                0x746E, 0x7485,
                0x7490, 0x74AB,
                0x74B5, 0x75D5
            };
    }
}
