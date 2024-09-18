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

/* Engines/AngelIsland/CoreAI.cs
 * ChangeLog
 *  9/10/2024, Adam
 *      Add StaminaDrainByWeight. Set in CoreManagementConsole, used in WeightOverloading
 *	3/5/16, Adam
 *		Add the featurebit PlayerAccountWipe which executes in the usual account cleanup in CrobTasks.cs
 *		The bit is reset after all all accounts are wiped. Houses are also cleaned up with the wipe.
 *		This bit is accessed via the core management console and is owner access only.
 *	8/20/11, Adam
 *		Add a switch to turn on/off that account blocker when users are switching between accounts and IP addresses.
 *		I think this is preventing legitimate play so I'm turning it off until I understand the system better.
 *	8/8/10, Adam
 *		Add: OldFlee switch
 *			BaseAI seems to have a bug where they clear FocusMob in OnActionChanged().Flee
 *	6/12/10, Adam
 *		Add a switch for controling the algo used for calculating armor absorb	(ArmorAbsorbClassic)
 *	3/13/10, Adam
 *		Add PetNeedsLOS
 *		in DoOrderAttack() for pets: give up if target is not in LOS (addresses the gate pet to give counts exploit)
 *	3/4/10, Adam
 *		Fix GetDouble to handle the frequent case of instring=="" instead of letting exception handling handle it.
 *	11/25/08, Adam
 *		Make MaxAddresses a console value
 *			in IPLimiter; controls how many of the same IP address can be concurrently logged in
 *	2/18/08, Adam
 *		Make MaxAccountsPerIP a console value
 *	1/19/08, Adam
 *		Add GracePeriod, ConnectionFloor, and Commission values for Player Vendor Management
 *  01/04/08, Pix
 *      Added GWRChangeDelayMinutes setting.
 *  12/9/07, Adam
 *      Added NewPlayerGuild feature bit.
 *	8/26/07 - Pix
 *		Added NewPlayerStartingArea feature bit.
 *	8/1/07, Pix
 *		Added RazorNegotiateFeaturesEnabled and RazorNegotiateWarnAndKick feature bits.
 *  4/3/07, Adam
 *      Add a BreedingEnabled bit
 *      Add a RTTNotifyEnabled bit
 *	1/08/07 Taran Kain
 *		Changed GSGG lookups to reflect new location in PlayerMobile
 *	01/02/07, Pix
 *		Added RangedCorrosionModifier.
 *		Added RangedCorrosion featurebit.
 *  08/12/06, Plasma
 *      Changed AI champ restart values to 1 min
 *	10/16/06, Adam
 *		Add flag to disable tower placement
 *	10/16/06, Adam
 *		Add global override for SecurePremises
 *			i.e., CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises)
 *	8/24/06, Adam
 *		Added TCAcctCleanupEnable to allow disabling of auto account cleanup
 *  8/19/06, Rhiannon
 *		Added PlayAccessLevel to allow dynamic control of the Play command.
 *	8/5/06, weaver
 *		Added feature bit to control disabling of help stuck command.
 *	7/23/06, Pix
 *		Added GuildKinChangeDisabled featurebit
 *  3/26/06, Pix
 *		Added IOBJoinEnabled;
 *	1/29/06, Adam
 *		TCAcctCleanupDays; trim all non-staff account and not logged in for N days - heartbeat
 *	12/28/05, Adam
 *		Add CommDeedBatch to set the number of containers processed per pass
 *	12/14/05 Pix
 *		Added DebugItemDecayOutput
 * 	12/01/05 Pix
 *		Added WorldSaveFrequency to CoreAI
 *	10/02/05, erlein
 *		Added ConfusionBaseDelay for adjustment of tamed creature confusion upon paralysis.
 *	9/20/05, Adam
 *		a. Add flag for OpposePlayers. This flag modifies the bahavior of IsOpposition()
 *		in BaseCreature such that aligned PLAYERS appear as enimies to NPCs of a different alignment.
 *		b. add flag for OpposePlayersPets
 *	9/13/05, erlein
 *		Added MeleePoisonSkillFactor bool to control poison skill factoring in
 *		OnHit() equations of melee weapons.
 *	9/03/05, Adam
 *		Add Global FreezeHouseDecay variable - World crisis mode :\
 *	09/02,05, erlein
 *		Added ReaggressIgnoreChance and xml save/load for IDOCBroadcastChance
 *  08/25/05, Taran Kain
 *		Added IDOCBroadcastChance
 *	07/13/05, erlein
 *		Added EScrollChance, EScrollSuccess.
 *	07/06/05, erlein
 *		Added CohesionLowerDelay, CohesionBaseDelay and CohesionFactor
 *		to control new res delay.
 *  06/03/05 Taran Kain
 *		Added TownCrierWordMinuteCost
 *	6/3/05, Adam
 *		Add in ExplosionPotionThreshold to control the tossers
 *		health requirement
 *	4/30/05, Pix
 *		Removed ExplosionPotionAlchemyReduction
 *	4/26/05, Pix
 *		Made explode pots targetting method toggleable based on CoreAI/ConsoleManagement setting.
 *	4/23/05, Pix
 *		Added ExplosionPotionAlchemyReduction
 *	04/18/05, Pix
 *		Added offline short term murder decay (only if it's turned on).
 *		Added potential exploding of carried explosion potions.
 *	4/18/05, Adam
 *		Add TempDouble and TempInt for testing ingame settings
 *	4/14/05, Adam
 *		Add CoreAI.TreasureMapDrop to dynamically adjust the treasuremap drop.
 *	4/8/05, Adam
 *		Add variables for the following CoreAI properties
 *		SpiritDepotTRPots, SpiritFirstWaveVirtualArmor, SpiritSecondWaveVirtualArmor,
 *		SpiritThirdWaveVirtualArmor, SpiritBossVirtualArmor
 *	3/31/05, Pix
 *		Added IsDynamicFeatureSet() utility function.
 *	3/31/05, Adam
 *		Add the new global flag for IOB'ness IOBShardWide
 *	3/7/05, Adam
 *		Add the global InmateRecallExploitCheck flag (see recall.cs)
 *	2/4/05, Adam
 *		Added PowderOfTranslocationAvail drop percentage
 *		The meaning of this file has just changed to mean: core Angel Island golbal properties.
 *		Previously this file was really only the Angel Island Prison.
 *	6/16/04, Pixie
 *		Added GSGG factor.
 *	4/29/04, mith
 *		Modified SpawnFreq variables, replaced with Restart/Expire variables (these are what the AILevelSystem uses)
 *		Added variable for CaveEntrance timer.
 *		Integrated all variables into applicable AI objects.
 *	4/13/04, mith
 *		Removed armor drops, added scroll drops.
 *	4/12/04 mith
 *		Modified initial values for Stinger Min/Max HP
 *		Added variables for Stinger Min/Max Damage
 *		Modified reg drops and starting stats for guards.
 *	4/12/04 pixie
 *		Initial Revision.
 *	4/12/04 Created by Pixie;
 */

using System;
using System.IO;
using System.Xml;

namespace Server
{

    public class CoreAI
    {
        /// <summary>
        /// This enum is used by at least teleporters and spawners to limit the world size.
        /// When the global world size changes, certain spawners and teleporters will enable/disable.
        /// </summary>
        [Flags]
        public enum WorldSize
        {
            None = 0x00,
            Small = 0x01,
            Medium = 0x02,
            Large = 0x04,
            Full = 0x08,
        }

        public static WorldSize CurrentWorldSize = WorldSize.Small | WorldSize.Medium | WorldSize.Large | WorldSize.Full;
        public static int StingerMinHP = 50;
        public static int StingerMaxHP = 75;
        public static int StingerMinDamage = 8;
        public static int StingerMaxDamage = 12;
        //		public static int CellGuardSpawnFreq = 20; //seconds?
        public static int CellGuardStrength = 200;
        public static int CellGuardSkillLevel = 75;
        public static int CellGuardNumRegDrop = 5;
        public static int GuardSpawnRestartDelay = 1;   //minutes
        public static int GuardSpawnExpireDelay = 10;   //minutes
                                                        //		public static int PostGuardSpawnFreq = 20;
        public static int PostGuardStrength = 200;
        public static int PostGuardSkillLevel = 85;
        public static int PostGuardNumRegDrop = 5;
        public static int PostGuardNumBandiesDrop = 2;
        public static int PostGuardNumGHPotDrop = 2;
        public static int CaptainGuardStrength = 600;
        public static int CaptainGuardSkillLevel = 100;
        public static int CaptainGuardWeapDrop = 2;
        public static int CaptainGuardNumRegDrop = 10;
        public static int CaptainGuardNumBandiesDrop = 10;
        public static int CaptainGuardGHPotsDrop = 4;
        public static int CaptainGuardScrollDrop = 3;
        public static int CaptainGuardNumLighthousePasses = 4;
        public static int CavePortalAvailability = 120; //seconds
                                                        //		public static int CaptainGuardLeatherSets = 1;
                                                        //		public static int CaptainGuardRingSets = 1;
                                                        //		public static int SpiritRespawnFreq = 15;
        public static int SpiritRestartDelay = 1;   //minutes
        public static int SpiritExpireDelay = 7;    //minutes
        public static int SpiritPortalAvailablity = 60; //seconds
        public static int SpiritFirstWaveNumber = 5;
        public static int SpiritFirstWaveHP = 25;
        public static int SpiritSecondWaveNumber = 5;
        public static int SpiritSecondWaveHP = 100;
        public static int SpiritThirdWaveNumber = 5;
        public static int SpiritThirdWaveHP = 200;
        public static int SpiritBossHP = 1000;
        public static int SpiritDepotGHPots = 10;
        public static int SpiritDepotBandies = 100;
        public static int SpiritDepotReagents = 10;
        public static int SpiritDepotRespawnFreq = 300;
        public static double PowderOfTranslocationAvail = 0.001;    // drop rate in percent
        public static int DynamicFeatures = 0;
        public static int SpiritDepotTRPots = 10;

        // AIP spirit spawn virtual armor
        public static int SpiritFirstWaveVirtualArmor = 100;
        public static int SpiritSecondWaveVirtualArmor = 30;
        public static int SpiritThirdWaveVirtualArmor = 50;
        public static int SpiritBossVirtualArmor = 34;

        // treasure map drop rate - usually something like 3.5% chance to appear as loot
        public static double TreasureMapDrop = 0.035;   // drop rate in percent

        // use these to tune the system, then replace with consts
        public static double TempDouble = 0.0;
        public static int TempInt = 0;

        // System accounts. Used for automated tasks that require a mobile
        public static int SystemAccount = -1;           // Persistent System Account
        public static int AdminAccount = -1;            // Persistent Admin Account

        // purple potion explosion factors
        public static int ExplosionPotionSensitivityLevel = 100; //minimum damage before potion check happens
        public static double ExplosionPotionChance = 0.0; //percentage chance that potion will go off
        public static double ExplosionPotionThreshold = 0.95; // health of caster must be this %

        // offline count decays
        public static int OfflineShortsDecayHours = 24;
        public static int OfflineShortsDecay = 0;

        // Explosion Potion Target Method
        public enum EPTM { MobileBased, PointBased };
        public static EPTM ExplosionPotionTargetMethod = EPTM.MobileBased;

        // Town Crier cost
        public static int TownCrierWordMinuteCost = 50;

        // Spirit cohesion controls
        public static int CohesionBaseDelay = 0;
        public static int CohesionLowerDelay = 0;
        public static int CohesionFactor = 0;

        // Enchanted scroll drop & success chance adjusters
        public static double EScrollChance = 0.75;
        public static double EScrollSuccess = 1.0;

        // Chance to broadcast newly IDOC houses over TCCS
        public static double IDOCBroadcastChance = 0.3;

        // Chance for creatures to ignore re-aggressive actions when not aggressing
        // already
        public static double ReaggressIgnoreChance = 0.0;

        // Base delay for pet confusion upon paralysis
        public static int ConfusionBaseDelay = 10;

        public static int WorldSaveFrequency = 30;

        public static bool DebugItemDecayOutput = false;
        public static bool TentAnnexation = true;


        // trim all non-staff account and not logged in for N days - heartbeat
        public static int TCAcctCleanupDays = 30;

        /// <summary>
        /// StandingDelay: denotes the minimum time (in seconds) an archer must stand still
        /// before being able to fire.e
        /// </summary>
        public static double StandingDelay = 0.5;

        // Control access level of Play command
        public static AccessLevel PlayAccessLevel = AccessLevel.Player;

        // enable account cleanup - heartbeat
        public static bool TCAcctCleanupEnable = true;

        // ranged corrosion addition reduction
        public static int RangedCorrosionModifier = 0;

        // Guild War Ring change setting delay
        public static int GWRChangeDelayMinutes = 0;

        // Player Vendor knobs
        public static int GracePeriod = 60;     // 60 minute grace period to move items without restock charge
        public static int ConnectionFloor = 50; // do not factor connections below this floor
        public static double Commission = .07;  // comission player vendors charge

        public static int MaxAccountsPerIP = 1;
        public static int MaxAddresses = 10;    // in IPLimiter; controls how many of the same IP address can be concurrently logged in

        public static double SlayerWeaponDropRate = 0.05;

        public static double TeleDelay = 0.0;
        public static int TeleTiles = 12;

        public enum FeatureBits
        {
            InmateRecallExploitCheck = 0x01,
            IOBShardWide = 0x02,
            FreezeHouseDecay = 0x04,
            MeleePoisonSkillFactor = 0x08,
            OpposePlayers = 0x10,
            OpposePlayersPets = 0x20,
            IOBJoinEnabled = 0x40,
            GuildKinChangeDisabled = 0x80,
            HelpStuckDisabled = 0x100,
            SecurePremises = 0x200,
            TowerAllowed = 0x400,
            RangedCorrosion = 0x800,
            BreedingEnabled = 0x1000,
            RTTNotifyEnabled = 0x2000,
            RazorNegotiateFeaturesEnabled = 0x4000,
            RazorNegotiateWarnAndKick = 0x8000,
            NewPlayerStartingArea = 0x10000,
            NewPlayerGuild = 0x20000,
            PetNeedsLOS = 0x40000,              // in DoOrderAttack() for pets: give up if target is not in LOS (addresses the gate pet to give counts exploit)
            LogStableCharges = 0x80000,
            ArmorAbsorbClassic = 0x100000,      // use the scaled armor AR to calculate how much damage is absorbed
            SpiritSpeakUsageReport = 0x200000,  // send admins system messages regarding SpirtHeal, and SlayerStrike 
            OldFlee = 0x400000,                 // BaseAI seems to have a bug where they clear FocusMob in OnActionChanged().Flee
            TreasureMapUsageReport = 0x800000,  // send admins system messages regarding treasure map decoded 
            SeaGypsyUsageReport = 0x1000000,    // send admins system messages sea gypsy escorts 
            IPBinderEnabled = 0x2000000,        // This is pixie's code to block account access if some combination of account/ip actions are taken
            PlayerAccountWipe = 0x4000000,      // Set this bit to wipe all Player access accounts. Owner access only, auto resets after wipe. Houses and all possessions go too
            StaminaDrainByWeight = 0x8000000,   // Set this bit to enable stamina drain by weight
        };

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);

            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoadCliloc);
        }

        public static bool IsDynamicFeatureSet(FeatureBits fb)
        {
            if ((DynamicFeatures & (int)fb) > 0) return true;
            else return false;
        }

        public static int SetDynamicFeature(FeatureBits fb)
        {
            DynamicFeatures |= (int)fb;
            return DynamicFeatures;
        }

        public static int ClearDynamicFeature(FeatureBits fb)
        {
            DynamicFeatures &= ~((int)fb);
            return DynamicFeatures;
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Console.WriteLine("CoreAI Saving...");
            if (!Directory.Exists("Saves/AngelIsland"))
                Directory.CreateDirectory("Saves/AngelIsland");

            string filePath = Path.Combine("Saves/AngelIsland", "CoreAI.xml");

            using (StreamWriter op = new StreamWriter(filePath))
            {
                XmlTextWriter xml = new XmlTextWriter(op);

                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);

                xml.WriteStartElement("CoreAI");
                
                xml.WriteStartElement("TeleDelay");
                xml.WriteString(TeleDelay.ToString());
                xml.WriteEndElement();
                xml.WriteStartElement("TeleTiles");
                xml.WriteString(TeleTiles.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StingerMinHP");
                xml.WriteString(StingerMinHP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StingerMaxHP");
                xml.WriteString(StingerMaxHP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StingerMinDamage");
                xml.WriteString(StingerMinDamage.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StingerMaxDamage");
                xml.WriteString(StingerMaxDamage.ToString());
                xml.WriteEndElement();

                //				xml.WriteStartElement( "CellGuardSpawnFreq" );
                //				xml.WriteString( CellGuardSpawnFreq.ToString() );
                //				xml.WriteEndElement();

                xml.WriteStartElement("CellGuardStrength");
                xml.WriteString(CellGuardStrength.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CellGuardSkillLevel");
                xml.WriteString(CellGuardSkillLevel.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CellGuardNumRegDrop");
                xml.WriteString(CellGuardNumRegDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("GuardSpawnRestartDelay");
                xml.WriteString(GuardSpawnRestartDelay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("GuardSpawnExpireDelay");
                xml.WriteString(GuardSpawnExpireDelay.ToString());
                xml.WriteEndElement();

                //				xml.WriteStartElement( "PostGuardSpawnFreq" );
                //				xml.WriteString( PostGuardSpawnFreq.ToString() );
                //				xml.WriteEndElement();

                xml.WriteStartElement("PostGuardStrength");
                xml.WriteString(PostGuardStrength.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("PostGuardSkillLevel");
                xml.WriteString(PostGuardSkillLevel.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("PostGuardNumRegDrop");
                xml.WriteString(PostGuardNumRegDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("PostGuardNumBandiesDrop");
                xml.WriteString(PostGuardNumBandiesDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("PostGuardNumGHPotDrop");
                xml.WriteString(PostGuardNumGHPotDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardStrength");
                xml.WriteString(CaptainGuardStrength.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardSkillLevel");
                xml.WriteString(CaptainGuardSkillLevel.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardWeapDrop");
                xml.WriteString(CaptainGuardWeapDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardNumRegDrop");
                xml.WriteString(CaptainGuardNumRegDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardNumBandiesDrop");
                xml.WriteString(CaptainGuardNumBandiesDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardGHPotsDrop");
                xml.WriteString(CaptainGuardGHPotsDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardScrollDrop");
                xml.WriteString(CaptainGuardScrollDrop.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CaptainGuardNumLighthousePasses");
                xml.WriteString(CaptainGuardNumLighthousePasses.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CavePortalAvailability");
                xml.WriteString(CavePortalAvailability.ToString());
                xml.WriteEndElement();

                //				xml.WriteStartElement( "CaptainGuardLeatherSets" );
                //				xml.WriteString( CaptainGuardLeatherSets.ToString() );
                //				xml.WriteEndElement();
                //
                //				xml.WriteStartElement( "CaptainGuardRingSets" );
                //				xml.WriteString( CaptainGuardRingSets.ToString() );
                //				xml.WriteEndElement();

                //				xml.WriteStartElement( "SpiritRespawnFreq" );
                //				xml.WriteString( SpiritRespawnFreq.ToString() );
                //				xml.WriteEndElement();

                xml.WriteStartElement("SpiritRestartDelay");
                xml.WriteString(SpiritRestartDelay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritExpireDelay");
                xml.WriteString(SpiritExpireDelay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritPortalAvailablity");
                xml.WriteString(SpiritPortalAvailablity.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritFirstWaveNumber");
                xml.WriteString(SpiritFirstWaveNumber.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritFirstWaveHP");
                xml.WriteString(SpiritFirstWaveHP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritSecondWaveNumber");
                xml.WriteString(SpiritSecondWaveNumber.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritSecondWaveHP");
                xml.WriteString(SpiritSecondWaveHP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritThirdWaveNumber");
                xml.WriteString(SpiritThirdWaveNumber.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritThirdWaveHP");
                xml.WriteString(SpiritThirdWaveHP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritBossHP");
                xml.WriteString(SpiritBossHP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritDepotGHPots");
                xml.WriteString(SpiritDepotGHPots.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritDepotBandies");
                xml.WriteString(SpiritDepotBandies.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritDepotReagents");
                xml.WriteString(SpiritDepotReagents.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritDepotRespawnFreq");
                xml.WriteString(SpiritDepotRespawnFreq.ToString());
                xml.WriteEndElement();

                // GSGG values
                xml.WriteStartElement("GSGGTime");
                xml.WriteString(Server.Mobiles.PlayerMobile.GSGG.ToString());
                xml.WriteEndElement();

                // PowderOfTranslocation availability
                xml.WriteStartElement("PowderOfTranslocationAvail");
                xml.WriteString(PowderOfTranslocationAvail.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("DynamicFeatures");
                xml.WriteString(DynamicFeatures.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritDepotTRPots");
                xml.WriteString(SpiritDepotTRPots.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritFirstWaveVirtualArmor");
                xml.WriteString(SpiritFirstWaveVirtualArmor.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritSecondWaveVirtualArmor");
                xml.WriteString(SpiritSecondWaveVirtualArmor.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritThirdWaveVirtualArmor");
                xml.WriteString(SpiritThirdWaveVirtualArmor.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("SpiritBossVirtualArmor");
                xml.WriteString(SpiritBossVirtualArmor.ToString());
                xml.WriteEndElement();

                // Treasure Map Drop Rate
                xml.WriteStartElement("TreasureMapDrop");
                xml.WriteString(TreasureMapDrop.ToString());
                xml.WriteEndElement();

                // temp var used for system tuning
                xml.WriteStartElement("TempDouble");
                xml.WriteString(TempDouble.ToString());
                xml.WriteEndElement();

                // temp var used for system tuning
                xml.WriteStartElement("TempInt");
                xml.WriteString(TempInt.ToString());
                xml.WriteEndElement();

                // System accounts. Used for automated tasks that require a mobile
                xml.WriteStartElement("SystemAccount");
                xml.WriteString(SystemAccount.ToString());
                xml.WriteEndElement();
                xml.WriteStartElement("AdminAccount");
                xml.WriteString(AdminAccount.ToString());
                xml.WriteEndElement();

                // purple potion explosion factors
                xml.WriteStartElement("ExplPotSensitivity");
                xml.WriteString(ExplosionPotionSensitivityLevel.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ExplPotChance");
                xml.WriteString(ExplosionPotionChance.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("OfflineShortsHours");
                xml.WriteString(OfflineShortsDecayHours.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("OfflineShortsDecay");
                xml.WriteString(OfflineShortsDecay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ExplosionPotionTargetMethod");
                xml.WriteString(((int)ExplosionPotionTargetMethod).ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ExplosionPotionThreshold");
                xml.WriteString(ExplosionPotionThreshold.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("TownCrierWordMinuteCost");
                xml.WriteString(TownCrierWordMinuteCost.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CohesionBaseDelay");
                xml.WriteString(CohesionBaseDelay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CohesionLowerDelay");
                xml.WriteString(CohesionLowerDelay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CohesionFactor");
                xml.WriteString(CohesionFactor.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("EScrollChance");
                xml.WriteString(EScrollChance.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("EScrollSuccess");
                xml.WriteString(EScrollSuccess.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("IDOCBroadcastChance");
                xml.WriteString(IDOCBroadcastChance.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ReaggressIgnoreChance");
                xml.WriteString(ReaggressIgnoreChance.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ConfusionBaseDelay");
                xml.WriteString(ReaggressIgnoreChance.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("WorldSaveFrequency");
                xml.WriteString(WorldSaveFrequency.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("DebugItemDecayOutput");
                xml.WriteString(DebugItemDecayOutput ? "1" : "0");
                xml.WriteEndElement();

                xml.WriteStartElement("TentAnnexation");
                xml.WriteString(TentAnnexation ? "1" : "0");
                xml.WriteEndElement();
                
                xml.WriteStartElement("TCAcctCleanupDays");
                xml.WriteString(TCAcctCleanupDays.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("StandingDelay");
                xml.WriteString(StandingDelay.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("PlayAccessLevel");
                xml.WriteString(((int)PlayAccessLevel).ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("TCAcctCleanupEnable");
                xml.WriteString(TCAcctCleanupEnable ? "1" : "0");
                xml.WriteEndElement();

                xml.WriteStartElement("RangedCorrosionModifier");
                xml.WriteString(RangedCorrosionModifier.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("GWRChangeDelayMinutes");
                xml.WriteString(GWRChangeDelayMinutes.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("GracePeriod");
                xml.WriteString(GracePeriod.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("ConnectionFloor");
                xml.WriteString(ConnectionFloor.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("MaxAccountsPerIP");
                xml.WriteString(MaxAccountsPerIP.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("MaxAddresses");
                xml.WriteString(MaxAddresses.ToString());
                xml.WriteEndElement();

                // Slayer Weapon drop rate
                xml.WriteStartElement("SlayerWeaponDropRate");
                xml.WriteString(SlayerWeaponDropRate.ToString());
                xml.WriteEndElement();

                xml.WriteStartElement("CurrentWorldSize");
                xml.WriteString(((int)CurrentWorldSize).ToString());
                xml.WriteEndElement();

                xml.WriteEndElement();
                xml.Close();
            }

        }

        public static void OnLoadCliloc()
        {
            Console.WriteLine("Cliloc Loading... ");
            string temp = Server.Text.Cliloc.Lookup[500000];
        }

        public static void OnLoad()
        {
            Console.WriteLine("CoreAI Loading...");
            string filePath = Path.Combine("Saves/AngelIsland", "CoreAI.xml");

            if (!File.Exists(filePath))
                return;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["CoreAI"];

            TeleDelay = GetDouble(GetText(root["TeleDelay"], TeleDelay.ToString()), TeleDelay);
            TeleTiles = GetValue(root["TeleTiles"], TeleTiles);

            StingerMinHP = GetValue(root["StingerMinHP"], StingerMinHP);
            StingerMaxHP = GetValue(root["StingerMaxHP"], StingerMaxHP);
            StingerMinDamage = GetValue(root["StingerMinDamage"], StingerMinDamage);
            StingerMaxDamage = GetValue(root["StingerMaxDamage"], StingerMaxDamage);
            //			CellGuardSpawnFreq = GetValue( root["CellGuardSpawnFreq"], CellGuardSpawnFreq );
            CellGuardStrength = GetValue(root["CellGuardStrength"], CellGuardStrength);
            CellGuardSkillLevel = GetValue(root["CellGuardSkillLevel"], CellGuardSkillLevel);
            CellGuardNumRegDrop = GetValue(root["CellGuardNumRegDrop"], CellGuardNumRegDrop);
            GuardSpawnRestartDelay = GetValue(root["GuardSpawnRestartDelay"], GuardSpawnRestartDelay);
            GuardSpawnExpireDelay = GetValue(root["GuardSpawnExpireDelay"], GuardSpawnExpireDelay);
            //			PostGuardSpawnFreq = GetValue( root["PostGuardSpawnFreq"], PostGuardSpawnFreq );
            PostGuardStrength = GetValue(root["PostGuardStrength"], PostGuardStrength);
            PostGuardSkillLevel = GetValue(root["PostGuardSkillLevel"], PostGuardSkillLevel);
            PostGuardNumRegDrop = GetValue(root["PostGuardNumRegDrop"], PostGuardNumRegDrop);
            PostGuardNumBandiesDrop = GetValue(root["PostGuardNumBandiesDrop"], PostGuardNumBandiesDrop);
            PostGuardNumGHPotDrop = GetValue(root["PostGuardNumGHPotDrop"], PostGuardNumGHPotDrop);
            CaptainGuardStrength = GetValue(root["CaptainGuardStrength"], CaptainGuardStrength);
            CaptainGuardSkillLevel = GetValue(root["CaptainGuardSkillLevel"], CaptainGuardSkillLevel);
            CaptainGuardWeapDrop = GetValue(root["CaptainGuardWeapDrop"], CaptainGuardWeapDrop);
            CaptainGuardNumRegDrop = GetValue(root["CaptainGuardNumRegDrop"], CaptainGuardNumRegDrop);
            CaptainGuardNumBandiesDrop = GetValue(root["CaptainGuardNumBandiesDrop"], CaptainGuardNumBandiesDrop);
            CaptainGuardGHPotsDrop = GetValue(root["CaptainGuardGHPotsDrop"], CaptainGuardGHPotsDrop);
            CaptainGuardScrollDrop = GetValue(root["CaptainGuardScrollDrop"], CaptainGuardScrollDrop);
            CaptainGuardNumLighthousePasses = GetValue(root["CaptainGuardNumLighthousePasses"], CaptainGuardNumLighthousePasses);
            CavePortalAvailability = GetValue(root["CavePortalAvailability"], CavePortalAvailability);
            //			CaptainGuardLeatherSets = GetValue( root["CaptainGuardLeatherSets"], CaptainGuardLeatherSets );
            //			CaptainGuardRingSets = GetValue( root["CaptainGuardRingSets"], CaptainGuardRingSets );
            //			SpiritRespawnFreq = GetValue( root["SpiritRespawnFreq"], SpiritRespawnFreq );
            SpiritRestartDelay = GetValue(root["SpiritRestartDelay"], SpiritRestartDelay);
            SpiritExpireDelay = GetValue(root["SpiritExpireDelay"], SpiritExpireDelay);
            SpiritPortalAvailablity = GetValue(root["SpiritPortalAvailablity"], SpiritPortalAvailablity);
            SpiritFirstWaveNumber = GetValue(root["SpiritFirstWaveNumber"], SpiritFirstWaveNumber);
            SpiritFirstWaveHP = GetValue(root["SpiritFirstWaveHP"], SpiritFirstWaveHP);
            SpiritSecondWaveNumber = GetValue(root["SpiritSecondWaveNumber"], SpiritSecondWaveNumber);
            SpiritSecondWaveHP = GetValue(root["SpiritSecondWaveHP"], SpiritSecondWaveHP);
            SpiritThirdWaveNumber = GetValue(root["SpiritThirdWaveNumber"], SpiritThirdWaveNumber);
            SpiritThirdWaveHP = GetValue(root["SpiritThirdWaveHP"], SpiritThirdWaveHP);
            SpiritBossHP = GetValue(root["SpiritBossHP"], SpiritBossHP);
            SpiritDepotGHPots = GetValue(root["SpiritDepotGHPots"], SpiritDepotGHPots);
            SpiritDepotBandies = GetValue(root["SpiritDepotBandies"], SpiritDepotBandies);
            SpiritDepotReagents = GetValue(root["SpiritDepotReagents"], SpiritDepotReagents);
            SpiritDepotRespawnFreq = GetValue(root["SpiritDepotRespawnFreq"], SpiritDepotRespawnFreq);

            // GSGG values
            Server.Mobiles.PlayerMobile.GSGG = GetDouble(GetText(root["GSGGTime"], "0.0"), 0.0);

            // PowderOfTranslocation availability
            PowderOfTranslocationAvail = GetDouble(GetText(root["PowderOfTranslocationAvail"], ""), PowderOfTranslocationAvail);

            DynamicFeatures = GetValue(root["DynamicFeatures"], DynamicFeatures);

            // more supply depot supplies
            SpiritDepotTRPots = GetValue(root["SpiritDepotTRPots"], SpiritDepotTRPots);

            // AIP spirit spawn virtual armor
            SpiritFirstWaveVirtualArmor = GetValue(root["SpiritFirstWaveVirtualArmor"], SpiritFirstWaveVirtualArmor);
            SpiritSecondWaveVirtualArmor = GetValue(root["SpiritSecondWaveVirtualArmor"], SpiritSecondWaveVirtualArmor);
            SpiritThirdWaveVirtualArmor = GetValue(root["SpiritThirdWaveVirtualArmor"], SpiritThirdWaveVirtualArmor);
            SpiritBossVirtualArmor = GetValue(root["SpiritBossVirtualArmor"], SpiritBossVirtualArmor);

            // Treasure Map Drop Rate
            TreasureMapDrop = GetDouble(GetText(root["TreasureMapDrop"], ""), TreasureMapDrop);

            // temp vars used for system tuning
            TempDouble = GetDouble(GetText(root["TempDouble"], ""), TempDouble);
            TempInt = GetValue(root["TempInt"], TempInt);

            // System accounts. Used for automated tasks that require a mobile
            SystemAccount = GetValue(root["SystemAccount"], SystemAccount);
            AdminAccount = GetValue(root["AdminAccount"], SystemAccount);

            // purple potion explosion factors
            ExplosionPotionSensitivityLevel = GetValue(root["ExplPotSensitivity"], ExplosionPotionSensitivityLevel);
            ExplosionPotionChance = GetDouble(GetText(root["ExplPotChance"], ""), ExplosionPotionChance);

            // murder count vars
            OfflineShortsDecayHours = GetValue(root["OfflineShortsHours"], OfflineShortsDecayHours);
            OfflineShortsDecay = GetValue(root["OfflineShortsDecay"], 0);

            ExplosionPotionTargetMethod = (EPTM)GetValue(root["ExplosionPotionTargetMethod"], (int)ExplosionPotionTargetMethod);

            ExplosionPotionThreshold = GetDouble(GetText(root["ExplosionPotionThreshold"], ""), ExplosionPotionThreshold);

            // town crier cost
            TownCrierWordMinuteCost = GetValue(root["TownCrierWordMinuteCost"], TownCrierWordMinuteCost);

            // spirit cohesion controls
            CohesionBaseDelay = GetValue(root["CohesionBaseDelay"], CohesionBaseDelay);
            CohesionLowerDelay = GetValue(root["CohesionLowerDelay"], CohesionLowerDelay);
            CohesionFactor = GetValue(root["CohesionFactor"], CohesionFactor);

            // enchanted scroll drop chance
            EScrollChance = GetDouble(GetText(root["EScrollChance"], ""), EScrollChance);

            // enchanted scroll success chance adjuster
            EScrollChance = GetDouble(GetText(root["EScrollSuccess"], ""), EScrollSuccess);

            // chance to broadcast newly IDOC houses over TCCS
            IDOCBroadcastChance = GetDouble(GetText(root["IDOCBroadcastChance"], ""), IDOCBroadcastChance);

            // chance for creatures to ignore re-aggression when not aggressing already
            ReaggressIgnoreChance = GetDouble(GetText(root["ReaggressIgnoreChance"], ""), ReaggressIgnoreChance);

            // base period of confusion for tamed creatures upon paralysis
            ReaggressIgnoreChance = GetDouble(GetText(root["ConfusionBaseDelay"], ""), ConfusionBaseDelay);

            WorldSaveFrequency = GetInt32(GetText(root["WorldSaveFrequency"], "30"), 30);

            DebugItemDecayOutput = (GetInt32(GetText(root["DebugItemDecayOutput"], "0"), 0) != 0);

            TentAnnexation = (GetInt32(GetText(root["TentAnnexation"], TentAnnexation ? "1" : "0"), 0) != 0);

            TCAcctCleanupDays = GetInt32(GetText(root["TCAcctCleanupDays"], "30"), 30);

            StandingDelay = GetDouble(GetText(root["StandingDelay"], ""), StandingDelay);

            PlayAccessLevel = (AccessLevel)GetValue(root["PlayAccessLevel"], (int)PlayAccessLevel);

            TCAcctCleanupEnable = (GetInt32(GetText(root["TCAcctCleanupEnable"], "0"), 0) != 0);

            RangedCorrosionModifier = GetInt32(GetText(root["RangedCorrosionModifier"], "0"), 0);

            GWRChangeDelayMinutes = GetInt32(GetText(root["GWRChangeDelayMinutes"], "0"), 0);

            GracePeriod = GetInt32(GetText(root["GracePeriod"], ""), GracePeriod);
            ConnectionFloor = GetInt32(GetText(root["ConnectionFloor"], ""), ConnectionFloor);
            Commission = GetDouble(GetText(root["Commission"], ""), Commission);

            MaxAccountsPerIP = GetInt32(GetText(root["MaxAccountsPerIP"], MaxAccountsPerIP.ToString()), 0);

            MaxAddresses = GetInt32(GetText(root["MaxAddresses"], MaxAddresses.ToString()), 0);

            SlayerWeaponDropRate = GetDouble(GetText(root["SlayerWeaponDropRate"], ""), SlayerWeaponDropRate);

            CurrentWorldSize = (WorldSize)GetInt32(GetText(root["CurrentWorldSize"], ""), (int)CurrentWorldSize);
        }

        public static int GetValue(XmlElement node, int defaultValue)
        {
            return GetInt32(GetText(node, defaultValue.ToString()), defaultValue);
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }

        public static int GetInt32(string intString, int defaultValue)
        {
            try
            {
                return XmlConvert.ToInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static double GetDouble(string dblString, double defaultValue)
        {

            // adam: using exceptions to handle errors is bad-bad.
            //	the intString=="" case happens a lot!
            if (dblString == null || dblString.Length == 0)
                return defaultValue;

            try
            {
                return XmlConvert.ToDouble(dblString);
            }
            catch
            {
                try
                {
                    return Convert.ToDouble(dblString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

    }

}
