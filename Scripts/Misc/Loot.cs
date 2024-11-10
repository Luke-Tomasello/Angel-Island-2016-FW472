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

/* Scripts/Misc/Loot.cs
 * ChangeLog
 *  9/20/2024, Adam (RandomWeapon et al.)
 *      The family of RandomWeapon generation does not include ranged weapons. We therefore include them here:
 *      From: return Construct(m_WeaponTypes)
 *      To: return Construct(m_RangedWeaponTypes, m_WeaponTypes)
 *      Note: A quick scan shows only the UnknownRogueSkeleton dropping ranged weapons.
 *	3/2/11, Adam
 *		Turn on LibraryBookTypes
 *	5/18/10, Adam
 *		Add/repair the following baby rare factory items light
 *		0x0A07, // Wall torch (facing east) - LightType.WestBig
 * 		0x0A0C, // Wall torch (facing south) - LightType.NorthBig
 * 		0x09FE, // Wall sconce (facing east) - LightType.WestBig
 * 		0x0A02, // Wall sconce (facing south) - LightType.NorthBig
 * 		0x0B26, // Tall candlabra (unlit 0xA29) - LightType.Circle225;
 *	5/10/10, Adam
 *		Add a baby rare factory.
 *		Most creatures now have a small chance to drop something cool!
 *	9/18/05, Adam
 *		a. Add new "Britannian Militia" IOBs
 *		b. make IOB's Scissorable = false;
 *	7/13/05, erlein
 *		Added GenEScroll to handle enchanted scroll drops.
 *	4/26/05, Adam
 *		Added the HammerPick to weapon types
 *	4/20/05, Kit
 *		Added Daggers to weapon types
 * 	4/12/05, Adam
 *		Fix: Don't hue bows or staves.
 * 	4/11/05, Adam
 *		Move the hueing code to Loot.ImbueWeaponOrArmor
 *		reduce Shadow chance (ugly) and replace with Copper and Bronze
 *		Don't hue bows or staves.
 *	3/29/05, Adam
 *		Tweaks to ImbueWeaponOrArmor() table values.
 *	3/28/05, Adam
 *		Add new method ImbueWeaponOrArmor() to Imbue weapons and armor with
 *		magical properties.
 *	12/28/05, Adam
 *		Add in old style 'orc colored' masks
 *  1/15/05,Froste
 *      Set Dyable to false for IOB Items and added "pirate boots" for IOBAlignment.Pirate
 *  12/21/04, Froste
 *      Added "blood drenched sash" for Council IOBAlignment, added "a pirate skullcap" for IOBAlignment.Pirate
 *  11/16/04, Froste
 *      Added "Savage Mask" for IOBAlignment.Savage
 *  11/10/04/Froste,
 *      Implemented new random IOB drop system and added "sandals of the walking dead"
 *	7/6/04, Adam
 *		turn back on Magic Item Drops
 *	7/6/04, Adam
 *		turn off RandomClothingOrJewelry()
 *		map RandomClothingOrJewelry() to RandomGem()
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;

namespace Server
{
    public class Loot
    {
        #region Rares
        public static Item RareFactoryItem(double chance)
        {
            int[] rares = new int[]
            {
                0x1502, // Plough
				0x1A9A, // Flax � looks lovely stacked on a pitcher.
				0x1BD9, // Stack of boards
				0x1BDE, // Logs
				0x0FF3, // Small open book
				0x0F41, // Stacked arrows
				0x1BFD, // Stacked crossbow bolts
				0x0A07, // Wall torch (facing east) - LightType.WestBig
				0x0A0C, // Wall torch (facing south) - LightType.NorthBig
				0x09FE, // Wall sconce (facing east) - LightType.WestBig
				0x0A02, // Wall sconce (facing south) - LightType.NorthBig
				0x0B26, // Tall candlabra (unlit 0xA29) - LightType.Circle225;
				0x0B47, // Large vase
				0x0B48, // Vase
				0x11FC, // Bamboo stool
				0x0A92, // Folded sheet � (east)  making these dyeable would rock! (use deathrobe - dyable/not scissorable)
				0x0A93, // Folded sheet � (south) making these dyeable would rock! (use deathrobe - dyable/not scissorable)
				0x09F3, // Pan 
				0x09E1, // Pot 1
				0x09E0, // Pot 2
				0x108B, // Beads � buyable at a jeweler on OSI, not released on AI.
				0x166D, // Shackles
				0x1A0D, // Chains with bones. (not a whole skeleton like given away at an event.)
				0x1230, // Guilotine
			};

            Item temp = null;
            if (chance >= Utility.RandomDouble())
            {
                int ItemId = rares[Utility.Random(rares.Length)];

                switch (ItemId)
                {
                    case 0x0A92:    // Folded sheet � (east)
                    case 0x0A93:    // Folded sheet � (south)
                        {   // make it dyable - don't scissor it!!
                            temp = new DeathRobe();
                            temp.ItemID = ItemId;
                            temp.LootType = LootType.Regular;
                        }
                        break;

                    case 0x0A07: // Wall torch (facing east)
                    case 0x09FE: // Wall sconce (facing east)
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.WestBig;
                        }
                        break;

                    case 0x0A0C: // Wall torch (facing south)
                    case 0x0A02: // Wall sconce (facing south)
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.NorthBig;
                        }
                        break;

                    case 0x0B26: // Tall candlabra
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                            temp.Light = LightType.Circle225;
                        }
                        break;

                    default:
                        {
                            temp = new Item(ItemId);
                            temp.Weight = 1;
                        }
                        break;
                }
            }
            return temp;
        }
        #endregion Rares

        #region List definitions
        private static Type[] m_SEWeaponTypes = new Type[]
            {
//				typeof( Bokuto ),				typeof( Daisho ),				typeof( Kama ),
//				typeof( Lajatang ),				typeof( NoDachi ),				typeof( Nunchaku ),
//				typeof( Sai ),					typeof( Tekagi ),				typeof( Tessen ),
//				typeof( Tetsubo ),				typeof( Wakizashi )
			};

        public static Type[] SEWeaponTypes { get { return m_SEWeaponTypes; } }

        private static Type[] m_AosWeaponTypes = new Type[]
            {
                typeof( Scythe ),               typeof( BoneHarvester ),        typeof( Scepter ),
                typeof( BladedStaff ),          typeof( Pike ),                 typeof( DoubleBladedStaff ),
                typeof( Lance ),                typeof( CrescentBlade )
            };

        public static Type[] AosWeaponTypes { get { return m_AosWeaponTypes; } }

        private static Type[] m_WeaponTypes = new Type[]
            {
                typeof( Axe ),                  typeof( BattleAxe ),            typeof( DoubleAxe ),
                typeof( ExecutionersAxe ),      typeof( Hatchet ),              typeof( LargeBattleAxe ),
                typeof( TwoHandedAxe ),         typeof( WarAxe ),               typeof( Club ),
                typeof( Mace ),                 typeof( Maul ),                 typeof( WarHammer ),
                typeof( WarMace ),              typeof( Bardiche ),             typeof( Halberd ),
                typeof( Spear ),                typeof( ShortSpear ),           typeof( Pitchfork ),
                typeof( WarFork ),              typeof( BlackStaff ),           typeof( GnarledStaff ),
                typeof( QuarterStaff ),         typeof( Broadsword ),           typeof( Cutlass ),
                typeof( Katana ),               typeof( Kryss ),                typeof( Longsword ),
                typeof( Scimitar ),             typeof( VikingSword ),          typeof( Pickaxe ),
                typeof( HammerPick ),           typeof( ButcherKnife ),         typeof( Cleaver ),
                typeof( Dagger ),               typeof( SkinningKnife ),        typeof( ShepherdsCrook )
            };

        public static Type[] WeaponTypes { get { return m_WeaponTypes; } }

        private static Type[] m_SERangedWeaponTypes = new Type[]
            {
//				typeof( Yumi )
			};

        public static Type[] SERangedWeaponTypes { get { return m_SERangedWeaponTypes; } }

        private static Type[] m_AosRangedWeaponTypes = new Type[]
            {
                typeof( CompositeBow ),         typeof( RepeatingCrossbow )
            };

        public static Type[] AosRangedWeaponTypes { get { return m_AosRangedWeaponTypes; } }

        private static Type[] m_RangedWeaponTypes = new Type[]
            {
                typeof( Bow ),                  typeof( Crossbow ),             typeof( HeavyCrossbow )
            };

        public static Type[] RangedWeaponTypes { get { return m_RangedWeaponTypes; } }

        private static Type[] m_SEArmorTypes = new Type[]
            {
//				typeof( ChainHatsuburi ),		typeof( LeatherDo ),			typeof( LeatherHaidate ),
//				typeof( LeatherHiroSode ),		typeof( LeatherJingasa ),		typeof( LeatherMempo ),
//				typeof( LeatherNinjaHood ),		typeof( LeatherNinjaJacket ),	typeof( LeatherNinjaMitts ),
//				typeof( LeatherNinjaPants ),	typeof( LeatherSuneate ),		typeof( DecorativePlateKabuto ),
//				typeof( HeavyPlateJingasa ),	typeof( LightPlateJingasa ),	typeof( PlateBattleKabuto ),
//				typeof( PlateDo ),				typeof( PlateHaidate ),			typeof( PlateHatsuburi ),
//				typeof( PlateHiroSode ),		typeof( PlateMempo ),			typeof( PlateSuneate ),
//				typeof( SmallPlateJingasa ),	typeof( StandardPlateKabuto ),	typeof( StuddedDo ),
//				typeof( StuddedHaidate ),		typeof( StuddedHiroSode ),		typeof( StuddedMempo ),
//				typeof( StuddedSuneate )
			};

        public static Type[] SEArmorTypes { get { return m_SEArmorTypes; } }

        private static Type[] m_ArmorTypes = new Type[]
            {
                typeof( BoneArms ),             typeof( BoneChest ),            typeof( BoneGloves ),
                typeof( BoneLegs ),             typeof( BoneHelm ),             typeof( ChainChest ),
                typeof( ChainLegs ),            typeof( ChainCoif ),            typeof( Bascinet ),
                typeof( CloseHelm ),            typeof( Helmet ),               typeof( NorseHelm ),
                typeof( OrcHelm ),              typeof( FemaleLeatherChest ),   typeof( LeatherArms ),
                typeof( LeatherBustierArms ),   typeof( LeatherChest ),         typeof( LeatherGloves ),
                typeof( LeatherGorget ),        typeof( LeatherLegs ),          typeof( LeatherShorts ),
                typeof( LeatherSkirt ),         typeof( LeatherCap ),           typeof( FemalePlateChest ),
                typeof( PlateArms ),            typeof( PlateChest ),           typeof( PlateGloves ),
                typeof( PlateGorget ),          typeof( PlateHelm ),            typeof( PlateLegs ),
                typeof( RingmailArms ),         typeof( RingmailChest ),        typeof( RingmailGloves ),
                typeof( RingmailLegs ),         typeof( FemaleStuddedChest ),   typeof( StuddedArms ),
                typeof( StuddedBustierArms ),   typeof( StuddedChest ),         typeof( StuddedGloves ),
                typeof( StuddedGorget ),        typeof( StuddedLegs )
            };

        public static Type[] ArmorTypes { get { return m_ArmorTypes; } }

        private static Type[] m_AosShieldTypes = new Type[]
            {
                typeof( ChaosShield ),          typeof( OrderShield )
            };

        public static Type[] AosShieldTypes { get { return m_AosShieldTypes; } }

        private static Type[] m_ShieldTypes = new Type[]
            {
                typeof( BronzeShield ),         typeof( Buckler ),              typeof( HeaterShield ),
                typeof( MetalShield ),          typeof( MetalKiteShield ),      typeof( WoodenKiteShield ),
                typeof( WoodenShield )
            };

        public static Type[] ShieldTypes { get { return m_ShieldTypes; } }

        private static Type[] m_GemTypes = new Type[]
            {
                typeof( Amber ),                typeof( Amethyst ),             typeof( Citrine ),
                typeof( Diamond ),              typeof( Emerald ),              typeof( Ruby ),
                typeof( Sapphire ),             typeof( StarSapphire ),         typeof( Tourmaline )
            };

        public static Type[] GemTypes { get { return m_GemTypes; } }

        private static Type[] m_JewelryTypes = new Type[]
            {
                typeof( GoldRing ),             typeof( GoldBracelet ),
                typeof( SilverRing ),           typeof( SilverBracelet )
            };

        public static Type[] JewelryTypes { get { return m_JewelryTypes; } }

        private static Type[] m_RegTypes = new Type[]
            {
                typeof( BlackPearl ),           typeof( Bloodmoss ),            typeof( Garlic ),
                typeof( Ginseng ),              typeof( MandrakeRoot ),         typeof( Nightshade ),
                typeof( SulfurousAsh ),         typeof( SpidersSilk )
            };

        public static Type[] RegTypes { get { return m_RegTypes; } }

        private static Type[] m_NecroRegTypes = new Type[]
            {
                typeof( BatWing ),              typeof( GraveDust ),            typeof( DaemonBlood ),
                typeof( NoxCrystal ),           typeof( PigIron )
            };

        public static Type[] NecroRegTypes { get { return m_NecroRegTypes; } }

        private static Type[] m_PotionTypes = new Type[]
            {
                typeof( AgilityPotion ),        typeof( StrengthPotion ),       typeof( RefreshPotion ),
                typeof( LesserCurePotion ),     typeof( LesserHealPotion ),     typeof( LesserPoisonPotion )
            };

        public static Type[] PotionTypes { get { return m_PotionTypes; } }

        private static Type[] m_SEInstrumentTypes = new Type[]
            {
//				typeof( BambooFlute )
			};

        public static Type[] SEInstrumentTypes { get { return m_SEInstrumentTypes; } }

        private static Type[] m_InstrumentTypes = new Type[]
            {
                typeof( Drums ),                typeof( Harp ),                 typeof( LapHarp ),
                typeof( Lute ),                 typeof( Tambourine ),           typeof( TambourineTassel )
            };

        public static Type[] InstrumentTypes { get { return m_InstrumentTypes; } }

        private static Type[] m_StatueTypes = new Type[]
        {
            typeof( StatueSouth ),          typeof( StatueSouth2 ),         typeof( StatueNorth ),
            typeof( StatueWest ),           typeof( StatueEast ),           typeof( StatueEast2 ),
            typeof( StatueSouthEast ),      typeof( BustSouth ),            typeof( BustEast )
        };

        public static Type[] StatueTypes { get { return m_StatueTypes; } }

        private static Type[] m_RegularScrollTypes = new Type[]
            {
                typeof( ClumsyScroll ),         typeof( CreateFoodScroll ),     typeof( FeeblemindScroll ),     typeof( HealScroll ),
                typeof( MagicArrowScroll ),     typeof( NightSightScroll ),     typeof( ReactiveArmorScroll ),  typeof( WeakenScroll ),
                typeof( AgilityScroll ),        typeof( CunningScroll ),        typeof( CureScroll ),           typeof( HarmScroll ),
                typeof( MagicTrapScroll ),      typeof( MagicUnTrapScroll ),    typeof( ProtectionScroll ),     typeof( StrengthScroll ),
                typeof( BlessScroll ),          typeof( FireballScroll ),       typeof( MagicLockScroll ),      typeof( PoisonScroll ),
                typeof( TelekinisisScroll ),    typeof( TeleportScroll ),       typeof( UnlockScroll ),         typeof( WallOfStoneScroll ),
                typeof( ArchCureScroll ),       typeof( ArchProtectionScroll ), typeof( CurseScroll ),          typeof( FireFieldScroll ),
                typeof( GreaterHealScroll ),    typeof( LightningScroll ),      typeof( ManaDrainScroll ),      typeof( RecallScroll ),
                typeof( BladeSpiritsScroll ),   typeof( DispelFieldScroll ),    typeof( IncognitoScroll ),      typeof( MagicReflectScroll ),
                typeof( MindBlastScroll ),      typeof( ParalyzeScroll ),       typeof( PoisonFieldScroll ),    typeof( SummonCreatureScroll ),
                typeof( DispelScroll ),         typeof( EnergyBoltScroll ),     typeof( ExplosionScroll ),      typeof( InvisibilityScroll ),
                typeof( MarkScroll ),           typeof( MassCurseScroll ),      typeof( ParalyzeFieldScroll ),  typeof( RevealScroll ),
                typeof( ChainLightningScroll ), typeof( EnergyFieldScroll ),    typeof( FlamestrikeScroll ),    typeof( GateTravelScroll ),
                typeof( ManaVampireScroll ),    typeof( MassDispelScroll ),     typeof( MeteorSwarmScroll ),    typeof( PolymorphScroll ),
                typeof( EarthquakeScroll ),     typeof( EnergyVortexScroll ),   typeof( ResurrectionScroll ),   typeof( SummonAirElementalScroll ),
                typeof( SummonDaemonScroll ),   typeof( SummonEarthElementalScroll ),   typeof( SummonFireElementalScroll ),    typeof( SummonWaterElementalScroll )
            };

        private static Type[] m_NecromancyScrollTypes = new Type[]
            {
                typeof( AnimateDeadScroll ),        typeof( BloodOathScroll ),      typeof( CorpseSkinScroll ), typeof( CurseWeaponScroll ),
                typeof( EvilOmenScroll ),           typeof( HorrificBeastScroll ),  typeof( LichFormScroll ),   typeof( MindRotScroll ),
                typeof( PainSpikeScroll ),          typeof( PoisonStrikeScroll ),   typeof( StrangleScroll ),   typeof( SummonFamiliarScroll ),
                typeof( VampiricEmbraceScroll ),    typeof( VengefulSpiritScroll ), typeof( WitherScroll ),     typeof( WraithFormScroll )
            };

        private static Type[] m_SENecromancyScrollTypes = new Type[]
        {
            typeof( AnimateDeadScroll ),        typeof( BloodOathScroll ),      typeof( CorpseSkinScroll ), typeof( CurseWeaponScroll ),
            typeof( EvilOmenScroll ),           typeof( HorrificBeastScroll ),  typeof( LichFormScroll ),   typeof( MindRotScroll ),
            typeof( PainSpikeScroll ),          typeof( PoisonStrikeScroll ),   typeof( StrangleScroll ),   typeof( SummonFamiliarScroll ),
            typeof( VampiricEmbraceScroll ),    typeof( VengefulSpiritScroll ), typeof( WitherScroll ),     typeof( WraithFormScroll ),
//			typeof( ExorcismScroll )
		};

        private static Type[] m_PaladinScrollTypes = new Type[0];

        public static Type[] RegularScrollTypes { get { return m_RegularScrollTypes; } }
        public static Type[] NecromancyScrollTypes { get { return m_NecromancyScrollTypes; } }
        public static Type[] SENecromancyScrollTypes { get { return m_SENecromancyScrollTypes; } }
        public static Type[] PaladinScrollTypes { get { return m_PaladinScrollTypes; } }

        private static Type[] m_GrimmochJournalTypes = new Type[]
        {
//			typeof( GrimmochJournal1 ),		typeof( GrimmochJournal2 ),		typeof( GrimmochJournal3 ),
//			typeof( GrimmochJournal6 ),		typeof( GrimmochJournal7 ),		typeof( GrimmochJournal11 ),
//			typeof( GrimmochJournal14 ),	typeof( GrimmochJournal17 ),	typeof( GrimmochJournal23 )
		};

        public static Type[] GrimmochJournalTypes { get { return m_GrimmochJournalTypes; } }

        private static Type[] m_LysanderNotebookTypes = new Type[]
        {
//			typeof( LysanderNotebook1 ),		typeof( LysanderNotebook2 ),		typeof( LysanderNotebook3 ),
//			typeof( LysanderNotebook7 ),		typeof( LysanderNotebook8 ),		typeof( LysanderNotebook11 )
		};

        public static Type[] LysanderNotebookTypes { get { return m_LysanderNotebookTypes; } }

        private static Type[] m_TavarasJournalTypes = new Type[]
        {
//			typeof( TavarasJournal1 ),		typeof( TavarasJournal2 ),		typeof( TavarasJournal3 ),
//			typeof( TavarasJournal6 ),		typeof( TavarasJournal7 ),		typeof( TavarasJournal8 ),
//			typeof( TavarasJournal9 ),		typeof( TavarasJournal11 ),		typeof( TavarasJournal14 ),
//			typeof( TavarasJournal16 ),		typeof( TavarasJournal16b ),	typeof( TavarasJournal17 ),
//			typeof( TavarasJournal19 )
		};

        public static Type[] TavarasJournalTypes { get { return m_TavarasJournalTypes; } }


        private static Type[] m_WandTypes = new Type[]
            {
                typeof( ClumsyWand ),               typeof( FeebleWand ),           typeof( FireballWand ),
                typeof( GreaterHealWand ),          typeof( HarmWand ),             typeof( HealWand ),
                typeof( IDWand ),                   typeof( LightningWand ),        typeof( MagicArrowWand ),
                typeof( ManaDrainWand ),            typeof( WeaknessWand )

            };
        public static Type[] WandTypes { get { return m_WandTypes; } }

        private static Type[] m_SEClothingTypes = new Type[]
            {
//				typeof( ClothNinjaJacket ),		typeof( FemaleKimono ),			typeof( Hakama ),
//				typeof( HakamaShita ),			typeof( JinBaori ),				typeof( Kamishimo ),
//				typeof( MaleKimono ),			typeof( NinjaTabi ),			typeof( Obi ),
//				typeof( SamuraiTabi ),			typeof( TattsukeHakama ),		typeof( Waraji )
			};

        public static Type[] SEClothingTypes { get { return m_SEClothingTypes; } }

        private static Type[] m_AosClothingTypes = new Type[]
            {
                typeof( FurSarong ),            typeof( FurCape ),              typeof( FlowerGarland ),
                typeof( GildedDress ),          typeof( FurBoots ),             typeof( FormalShirt ),
        };

        public static Type[] AosClothingTypes { get { return m_AosClothingTypes; } }

        private static Type[] m_ClothingTypes = new Type[]
            {
                typeof( Cloak ),
                typeof( Bonnet ),               typeof( Cap ),                  typeof( FeatheredHat ),
                typeof( FloppyHat ),            typeof( JesterHat ),            typeof( Surcoat ),
                typeof( SkullCap ),             typeof( StrawHat ),             typeof( TallStrawHat ),
                typeof( TricorneHat ),          typeof( WideBrimHat ),          typeof( WizardsHat ),
                typeof( BodySash ),             typeof( Doublet ),              typeof( Boots ),
                typeof( FullApron ),            typeof( JesterSuit ),           typeof( Sandals ),
                typeof( Tunic ),                typeof( Shoes ),                typeof( Shirt ),
                typeof( Kilt ),                 typeof( Skirt ),                typeof( FancyShirt ),
                typeof( FancyDress ),           typeof( ThighBoots ),           typeof( LongPants ),
                typeof( PlainDress ),           typeof( Robe ),                 typeof( ShortPants ),
                typeof( HalfApron )
            };
        public static Type[] ClothingTypes { get { return m_ClothingTypes; } }

        private static Type[] m_SEHatTypes = new Type[]
            {
//				typeof( ClothNinjaHood ),		typeof( Kasa )
			};

        public static Type[] SEHatTypes { get { return m_SEHatTypes; } }

        private static Type[] m_AosHatTypes = new Type[]
            {
                typeof( FlowerGarland ),    typeof( BearMask ),     typeof( DeerMask )	//Are Bear& Deer mask inside the Pre-AoS loottables too?
			};

        public static Type[] AosHatTypes { get { return m_AosHatTypes; } }

        private static Type[] m_HatTypes = new Type[]
            {
                typeof( SkullCap ),         typeof( Bandana ),      typeof( FloppyHat ),
                typeof( Cap ),              typeof( WideBrimHat ),  typeof( StrawHat ),
                typeof( TallStrawHat ),     typeof( WizardsHat ),   typeof( Bonnet ),
                typeof( FeatheredHat ),     typeof( TricorneHat ),  typeof( JesterHat )
            };

        public static Type[] HatTypes { get { return m_HatTypes; } }

        private static Type[] m_LibraryBookTypes = new Type[]
            {
                typeof( GrammarOfOrcish ),      typeof( CallToAnarchy ),                typeof( ArmsAndWeaponsPrimer ),
                typeof( SongOfSamlethe ),       typeof( TaleOfThreeTribes ),            typeof( GuideToGuilds ),
                typeof( BirdsOfBritannia ),     typeof( BritannianFlora ),              typeof( ChildrenTalesVol2 ),
                typeof( TalesOfVesperVol1 ),    typeof( DeceitDungeonOfHorror ),        typeof( DimensionalTravel ),
                typeof( EthicalHedonism ),      typeof( MyStory ),                      typeof( DiversityOfOurLand ),
                typeof( QuestOfVirtues ),       typeof( RegardingLlamas ),              typeof( TalkingToWisps ),
                typeof( TamingDragons ),        typeof( BoldStranger ),                 typeof( BurningOfTrinsic ),
                typeof( TheFight ),             typeof( LifeOfATravellingMinstrel ),    typeof( MajorTradeAssociation ),
                typeof( RankingsOfTrades ),     typeof( WildGirlOfTheForest ),          typeof( TreatiseOnAlchemy ),
                typeof( VirtueBook )
            };

        public static Type[] LibraryBookTypes { get { return m_LibraryBookTypes; } }
        #endregion

        #region Accessors

        public static BaseWeapon RandomRangedWeapon()
        {
            return RandomRangedWeapon(false);
        }

        public static BaseWeapon RandomRangedWeapon(bool inTokuno)
        {
            if (Core.RuleSets.SERules() && inTokuno)
                return Construct(m_SERangedWeaponTypes, m_AosRangedWeaponTypes, m_RangedWeaponTypes) as BaseWeapon;

            if (Core.RuleSets.AOSRules())
                return Construct(m_AosRangedWeaponTypes, m_RangedWeaponTypes) as BaseWeapon;

            return Construct(m_RangedWeaponTypes) as BaseWeapon;
        }

        public static BaseWeapon RandomWeapon()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes) as BaseWeapon;

            return Construct(m_RangedWeaponTypes/*Adam: See Change Log*/, m_WeaponTypes) as BaseWeapon;
        }

        public static Item RandomWeaponOrJewelry()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_JewelryTypes);

            return Construct(m_RangedWeaponTypes/*Adam: See Change Log*/, m_WeaponTypes, m_JewelryTypes);
        }

        public static BaseJewel RandomJewelry()
        {
            return Construct(m_JewelryTypes) as BaseJewel;
        }

        public static BaseArmor RandomArmor()
        {
            return Construct(m_ArmorTypes) as BaseArmor;
        }

        public static BaseShield RandomShield()
        {
            return Construct(m_ShieldTypes) as BaseShield;
        }

        public static BaseArmor RandomArmorOrShield()
        {
            return Construct(m_ArmorTypes, m_ShieldTypes) as BaseArmor;
        }

        public static Item RandomArmorOrShieldOrJewelry()
        {
            return Construct(m_ArmorTypes, m_ShieldTypes, m_JewelryTypes);
        }

        public static Item RandomArmorOrShieldOrWeapon()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_ArmorTypes, m_ShieldTypes);

            return Construct(m_RangedWeaponTypes/*Adam: See Change Log*/, m_WeaponTypes, m_ArmorTypes, m_ShieldTypes);
        }

        public static Item RandomArmorOrWeapon()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_ArmorTypes);

            return Construct(m_RangedWeaponTypes/*Adam: See Change Log*/, m_WeaponTypes, m_ArmorTypes);
        }

        public static Item RandomArmorOrShieldOrWeaponOrJewelry()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_AosWeaponTypes, m_WeaponTypes, m_ArmorTypes, m_ShieldTypes, m_JewelryTypes);

            return Construct(m_RangedWeaponTypes/*Adam: See Change Log*/, m_WeaponTypes, m_ArmorTypes, m_ShieldTypes, m_JewelryTypes);
        }

        public static Item RandomClothingOrJewelry()
        {
            return Construct(m_JewelryTypes, m_ClothingTypes);
        }

        public static Item RandomGem()
        {
            return Construct(m_GemTypes);
        }

        public static Item RandomReagent()
        {
            return Construct(m_RegTypes);
        }

        public static Item RandomNecromancyReagent()
        {
            return Construct(m_NecroRegTypes);
        }

        public static Item RandomPossibleReagent()
        {
            if (Core.RuleSets.AOSRules())
                return Construct(m_RegTypes, m_NecroRegTypes);

            return Construct(m_RegTypes);
        }

        public static Item RandomPotion()
        {
            return Construct(m_PotionTypes);
        }

        public static BaseInstrument RandomInstrument()
        {
            return Construct(m_InstrumentTypes) as BaseInstrument;
        }

        public static Item RandomStatue()
        {
            return Construct(m_StatueTypes);
        }

        // Adam: used for treasure chests / mini-bosses
        public static Item ImbueWeaponOrArmor(Item item, int level, double upgrade, bool hueing)
        {
            if (item == null)
                return item;

            // Adam: decrease the chances to get the max level attribute for this level
            // Create an unevenly weighted VISUAL table for chance resolution

            // start treasure chests
            int[] level1 = new int[] { 0,0,0,0,0,		// regular
										  1,1,1 };      // Ruin,	Defense

            int[] level2 = new int[] { 0,0,0,			// regular
										  1,1,1,1,1,1,	// Ruin,	Defense
										  2,2,2 };      // Might,	Guarding

            int[] level3 = new int[] { 0,0,0,			// regular
										  1,1,1,			// Ruin,	Defense
										  2,2,2,2,2,2,	// Might,	Guarding
										  3,3,3 };      // Force,	Hardening

            int[] level4 = new int[] { 1,1,1,1,1,		// Ruin,	Defense
										  2,2,2,2,2,2,	// Might,	Guarding
										  3,3,3,3,3,		// Force,	Hardening
										  4,4 };            // Power,	Fortification

            int[] level5 = new int[] { 1,1,1,1,		// Ruin,	Defense
										  2,2,2,2,2,		// Might,	Guarding
										  3,3,3,3,3,3,	// Force,	Hardening
										  4,4,4,4,4,		// Power,	Fortification
										  5,5 };            // Vanq,

            // mini boss loot table
            int[] level6 = new int[] { 3,3,3,3,		// force
										  4,4,4,4,		// Power
										  5,5 };            // Vanq,	Invulnerability

            int[][] chance = new int[][]
            {
                level1, level2, level3, level4, level5, level6
            };

            // level is 1-table size
            if (level < 1 || level > chance.Length)
                return item;

            // add a chance at a item upgrade
            if ((level < chance.Length) && (upgrade > Utility.RandomDouble()))
                level++;

            return ImbueWeaponOrArmor(item, level, chance, hueing);
        }

        public static Item ImbueWeaponOrArmor(Item item, int level, int[][] chance, bool hueing)
        {
            if (item == null)
                return item;

            //are differnt resource metal types and weight table
            int[] color = new int[]
            {
                9,					// Valorite,	1 in 36 chance
				8,8,				// Verite,		2 in 36 chance
				7,7,7,				// Agapite,		3 in 36 chance
				6,6,6,6,			// Gold,		4 in 36 chance
				5,5,5,5,5,			// Bronze,		5 in 36 chance
				4,4,4,4,4,4,		// Copper,		6 in 36 chance
				5,5,5,4,4,3,3,		// ShadowIron,	(Shadow is ugly, add copper and bronze)
				2,2,2,2,2,2,2,2		// DullCopper,	8 in 36 chance
			};

            if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                // add a 5% chance at a slayer weapon
                if (0.05 > Utility.RandomDouble())
                    weapon.Slayer = SlayerName.Silver;

                // find an appropriate weapon attribute for this level
                int DamageLevel;
                int ndx = level - 1;
                DamageLevel = chance[ndx][Utility.Random(chance[ndx].Length)];
                // Console.WriteLine(((int)DamageLevel).ToString());

                weapon.DamageLevel = (WeaponDamageLevel)DamageLevel;
                weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(6);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(6);

                // hue it baby!
                if (hueing == true)
                {
                    weapon.Resource = (CraftResource)color[Utility.Random(color.Length)];

                    // hueing rules: don't hue bows or staves
                    if ((weapon is BaseRanged) || (weapon is BaseStaff))
                        weapon.Hue = 0;
                }
            }
            else if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                // find an appropriate armor attribute for this level
                int ProtectionLevel;
                int ndx = level - 1;
                ProtectionLevel = chance[ndx][Utility.Random(chance[ndx].Length)];
                // Console.WriteLine(((int)ProtectionLevel).ToString());

                armor.ProtectionLevel = (ArmorProtectionLevel)ProtectionLevel;
                armor.Durability = (ArmorDurabilityLevel)Utility.Random(6);

                // hue it baby!
                if (hueing == true)
                {
                    armor.Resource = (CraftResource)color[Utility.Random(color.Length)];
                }
            }

            return item;
        }

        public static Item RandomIOB()
        {
            switch (Utility.Random(7))
            {
                case 0: // Undead - GUL
                    {
                        if (Utility.RandomBool())
                            return new BloodDrenchedBandana();
                        else
                        {
                            BodySash sash = new BodySash();
                            sash.Hue = 0x66C;
                            sash.IOBAlignment = IOBAlignment.Council;
                            sash.Name = "blood drenched sash";
                            sash.Dyable = false;
                            sash.Scissorable = false;
                            return sash;
                        }
                    }
                case 1: // Undead - UND
                    {
                        Sandals sandals = new Sandals();
                        if (Utility.RandomBool())
                            sandals.Hue = 0x66C;
                        else
                            sandals.Hue = 0x1;
                        sandals.IOBAlignment = IOBAlignment.Undead;
                        sandals.Name = "sandals of the walking dead";
                        sandals.Dyable = false;
                        sandals.Scissorable = false;
                        return sandals;


                    }
                case 2: // Orcish
                    {
                        if (Utility.RandomBool())
                        {   // green mask (brute color)
                            if (Utility.RandomBool())
                                return new OrcishKinMask();
                            else
                            {   // old style mask (orc colored)
                                OrcishKinMask mask = new OrcishKinMask();
                                mask.Hue = 0;
                                return mask;
                            }

                        }
                        else
                        {
                            return new OrcishKinHelm();

                        }
                    }
                case 3: //Savage
                    {
                        if (Utility.RandomBool())
                        {
                            if (Utility.RandomBool())
                            {
                                BearMask mask = new BearMask();
                                mask.IOBAlignment = IOBAlignment.Savage;
                                mask.Name = "bear mask of savage kin";
                                mask.Dyable = false;
                                return mask;
                            }
                            else
                            {
                                DeerMask mask = new DeerMask();
                                mask.IOBAlignment = IOBAlignment.Savage;
                                mask.Name = "deer mask of savage kin";
                                mask.Dyable = false;
                                return mask;
                            }
                        }
                        else
                        {
                            SavageMask mask = new SavageMask();
                            mask.IOBAlignment = IOBAlignment.Savage;
                            mask.Name = "tribal mask of savage kin";
                            mask.Dyable = false;
                            return mask;
                        }
                    }
                case 4: // Pirates
                    {
                        if (Utility.RandomBool())
                        {
                            if (Utility.RandomBool())
                            {
                                SkullCap skullcap = new SkullCap();
                                skullcap.IOBAlignment = IOBAlignment.Pirate;
                                skullcap.Name = "a pirate skullcap";
                                skullcap.Hue = 0x66C;
                                skullcap.Dyable = false;
                                skullcap.Scissorable = false;
                                return skullcap;
                            }
                            else
                            {
                                Boots boots = new Boots();
                                boots.IOBAlignment = IOBAlignment.Pirate;
                                boots.Name = "pirate kin boots";
                                boots.Hue = 0x66c;
                                boots.Dyable = false;
                                boots.Scissorable = false;
                                return boots;
                            }
                        }
                        else
                        {
                            return new PirateHat();
                        }

                    }
                case 5: // Brigands
                    {
                        if (Utility.RandomBool())
                        {
                            return new BrigandKinBandana();
                        }
                        else
                        {
                            return new BrigandKinBoots();
                        }
                    }
                case 6: // Good
                    {
                        switch (Utility.Random(4))
                        {
                            case 0:
                                Boots boots = new Boots(0x5E4);
                                boots.IOBAlignment = IOBAlignment.Good;
                                boots.Name = "Britannian Militia";
                                boots.Dyable = false;
                                return boots;
                            case 1:
                                Cloak cloak = new Cloak(Utility.RandomSpecialVioletHue());
                                cloak.IOBAlignment = IOBAlignment.Good;
                                cloak.Name = "Britannian Militia";
                                cloak.Dyable = false;
                                cloak.Scissorable = false;
                                return cloak;
                            case 2:
                                Surcoat surcoat = new Surcoat(Utility.RandomSpecialVioletHue());
                                surcoat.IOBAlignment = IOBAlignment.Good;
                                surcoat.Name = "Britannian Militia";
                                surcoat.Dyable = false;
                                surcoat.Scissorable = false;
                                return surcoat;
                            case 3:
                                BodySash bodySash = new BodySash(Utility.RandomSpecialRedHue());
                                bodySash.IOBAlignment = IOBAlignment.Good;
                                bodySash.Name = "Britannian Militia";
                                bodySash.Dyable = false;
                                bodySash.Scissorable = false;
                                return bodySash;
                        }
                        break;
                    }
            }
            return null;

        }

        public static SpellScroll RandomScroll(int minIndex, int maxIndex, SpellbookType type)
        {
            Type[] types;

            switch (type)
            {
                default:
                case SpellbookType.Regular: types = m_RegularScrollTypes; break;
                case SpellbookType.Necromancer: types = m_NecromancyScrollTypes; break;
                case SpellbookType.Paladin: types = m_PaladinScrollTypes; break;
            }

            return Construct(types, Utility.RandomMinMax(minIndex, maxIndex)) as SpellScroll;
        }

        public static EnchantedScroll GenEScroll(object MagicItem)
        {
            // Define arguments for instancing
            int ibaseimage = 8800 + Utility.Random(0, 15);
            object[] args = { MagicItem, ibaseimage };

            return ((EnchantedScroll)Activator.CreateInstance(typeof(EnchantedScroll), args));
        }


        #endregion

        #region Construction methods
        public static Item Construct(Type type)
        {
            try
            {
                return Activator.CreateInstance(type) as Item;
            }
            catch
            {
                return null;
            }
        }

        public static Item Construct(Type[] types)
        {
            if (types.Length > 0)
                return Construct(types, Utility.Random(types.Length));

            return null;
        }

        public static Item Construct(Type[] types, int index)
        {
            if (index >= 0 && index < types.Length)
                return Construct(types[index]);

            return null;
        }

        public static Item Construct(params Type[][] types)
        {
            int totalLength = 0;

            for (int i = 0; i < types.Length; ++i)
                totalLength += types[i].Length;

            if (totalLength > 0)
            {
                int index = Utility.Random(totalLength);

                for (int i = 0; i < types.Length; ++i)
                {
                    if (index >= 0 && index < types[i].Length)
                        return Construct(types[i][index]);

                    index -= types[i].Length;
                }
            }

            return null;
        }
        #endregion
    }
}
