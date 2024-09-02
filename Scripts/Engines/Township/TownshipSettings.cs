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

/* Scripts/Engines/Township/TownshipSettings.cs
 * CHANGELOG:
 * 11/16/08, Pix
 *		Added WallTeleporterDistance for wall placement.
 * 10/10/08, Pix
 *		Added CalculateFeesBasedOnServerActivity to be called when we calc server activity.
 *	3/20/07, Pix
 *		Added InitialFunds dial.
 *	Pix: 4/19/07
 *		Added all fees/charges and modifiers.
 */


using Server.Items;
using System;
using System.IO;

namespace Server.Township
{
    class TownshipSettings
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        public static DateTime m_sLastActivityLevelLimitCalculation = DateTime.MinValue;

        public static void CalculateFeesBasedOnServerActivity(int acctsAccessedInLastWeek)
        {
            if (m_sLastActivityLevelLimitCalculation == DateTime.MinValue)
            {
                //if we've just come up from a restart, then set the last calc to Now.
                m_sLastActivityLevelLimitCalculation = DateTime.UtcNow;
            }
            else if (m_sLastActivityLevelLimitCalculation + TimeSpan.FromHours(24.0) < DateTime.UtcNow)
            {
                double idealAILW = 1000.0; //hahahahahah ideal.  good joke, eh?

                double iPercentage = ((double)acctsAccessedInLastWeek) / idealAILW;

                // make sure that we're > 10% (meaning a base fee would cost 250 gold per day (at high activity)
                if (iPercentage < 0.1) iPercentage = 0.1;

                FeeAcctInfoCount = acctsAccessedInLastWeek;
                FeePercentageCalc = iPercentage;

                BaseFee = (int)(2500 * iPercentage);
                ExtendedFee = (int)(2500 * iPercentage);
                NoGateOutFee = (int)(1000 * iPercentage);
                NoGateInFee = (int)(1000 * iPercentage);
                NoRecallOutFee = (int)(1000 * iPercentage);
                NoRecallInFee = (int)(1000 * iPercentage);
                LawlessFee = (int)(2000 * iPercentage);
                LawAuthFee = (int)(5000 * iPercentage);
                NPCType1Fee = (int)(1000 * iPercentage);
                NPCType2Fee = (int)(2000 * iPercentage);
                NPCType3Fee = (int)(5000 * iPercentage);
            }
        }

        #region Save/Load

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("TownshipSettings Saving...");
                if (!Directory.Exists("Saves/AngelIsland"))
                    Directory.CreateDirectory("Saves/AngelIsland");

                string filePath = Path.Combine("Saves/AngelIsland", "Township.bin");

                GenericWriter bin;
                bin = new BinaryFileWriter(filePath, true);

                bin.Write(9); //version

                //v9 addition
                bin.Write(WallHitsDecay);

                //v8 addition
                bin.Write(AuctioneerPayoutPercentage);

                //v7 addition
                bin.Write(AuctioneerCharge);

                //v6 addition
                bin.Write(FightBrokerCharge);

                //v5 addition
                bin.Write(WallTeleporterDistance);

                //v4 addition
                bin.Write(InitialFunds);

                //v3 additions

                bin.Write(TSDeedCost);

                bin.Write(GuildHousePercentage);

                bin.Write(LLModifierNone);
                bin.Write(LLModifierLow);
                bin.Write(LLModifierMed);
                bin.Write(LLModifierHigh);
                bin.Write(LLModifierBoom);

                bin.Write(NPCModifierNone);
                bin.Write(NPCModifierLow);
                bin.Write(NPCModifierMed);
                bin.Write(NPCModifierHigh);
                bin.Write(NPCModifierBoom);

                bin.Write(BaseModifierNone);
                bin.Write(BaseModifierLow);
                bin.Write(BaseModifierMed);
                bin.Write(BaseModifierHigh);
                bin.Write(BaseModifierBoom);


                //begin v2 additions
                bin.Write(BaseFee);
                bin.Write(ExtendedFee);
                bin.Write(NoGateOutFee);
                bin.Write(NoGateInFee);
                bin.Write(NoRecallOutFee);
                bin.Write(NoRecallInFee);
                bin.Write(LawlessFee);
                bin.Write(LawAuthFee);
                bin.Write(NPCType1Fee);
                bin.Write(NPCType2Fee);
                bin.Write(NPCType3Fee);
                bin.Write(LawNormCharge);
                bin.Write(LawlessCharge);
                bin.Write(LawAuthCharge);
                bin.Write(ChangeTravelCharge);
                bin.Write(UpdateEnemyCharge);
                bin.Write(EmissaryCharge);
                bin.Write(EvocatorCharge);
                bin.Write(AlchemistCharge);
                bin.Write(AnimalTrainerCharge);
                bin.Write(BankerCharge);
                bin.Write(InnkeeperCharge);
                bin.Write(MageCharge);
                bin.Write(ProvisionerCharge);
                bin.Write(ArmsTrainerCharge);
                bin.Write(MageTrainerCharge);
                bin.Write(RogueTrainerCharge);
                bin.Write(LookoutCharge);
                bin.Write(TownCrierCharge);

                //v1 below
                bin.Write(Hue);
                bin.Write(NoneToLow);
                bin.Write(LowToMedium);
                bin.Write(MediumToHigh);
                bin.Write(HighToBooming);

                bin.Close();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void OnLoad()
        {
            try
            {
                Console.WriteLine("TownshipSettings Loading...");
                string filePath = Path.Combine("Saves/AngelIsland", "Township.bin");

                if (!File.Exists(filePath))
                    return;

                BinaryFileReader datreader = new BinaryFileReader(new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)));
                int version = datreader.ReadInt();

                switch (version)
                {
                    case 9:
                        WallHitsDecay = datreader.ReadInt();
                        goto case 8;
                    case 8:
                        AuctioneerPayoutPercentage = datreader.ReadDouble();
                        goto case 7;
                    case 7:
                        AuctioneerCharge = datreader.ReadInt();
                        goto case 6;
                    case 6:
                        FightBrokerCharge = datreader.ReadInt();
                        goto case 5;
                    case 5:
                        WallTeleporterDistance = datreader.ReadInt();
                        goto case 4;
                    case 4:
                        InitialFunds = datreader.ReadInt();
                        goto case 3;
                    case 3:
                        TSDeedCost = datreader.ReadInt();

                        GuildHousePercentage = datreader.ReadDouble();

                        LLModifierNone = datreader.ReadDouble();
                        LLModifierLow = datreader.ReadDouble();
                        LLModifierMed = datreader.ReadDouble();
                        LLModifierHigh = datreader.ReadDouble();
                        LLModifierBoom = datreader.ReadDouble();

                        NPCModifierNone = datreader.ReadDouble();
                        NPCModifierLow = datreader.ReadDouble();
                        NPCModifierMed = datreader.ReadDouble();
                        NPCModifierHigh = datreader.ReadDouble();
                        NPCModifierBoom = datreader.ReadDouble();

                        BaseModifierNone = datreader.ReadDouble();
                        BaseModifierLow = datreader.ReadDouble();
                        BaseModifierMed = datreader.ReadDouble();
                        BaseModifierHigh = datreader.ReadDouble();
                        BaseModifierBoom = datreader.ReadDouble();

                        goto case 2;
                    case 2:
                        BaseFee = datreader.ReadInt();
                        ExtendedFee = datreader.ReadInt();
                        NoGateOutFee = datreader.ReadInt();
                        NoGateInFee = datreader.ReadInt();
                        NoRecallOutFee = datreader.ReadInt();
                        NoRecallInFee = datreader.ReadInt();
                        LawlessFee = datreader.ReadInt();
                        LawAuthFee = datreader.ReadInt();
                        NPCType1Fee = datreader.ReadInt();
                        NPCType2Fee = datreader.ReadInt();
                        NPCType3Fee = datreader.ReadInt();
                        LawNormCharge = datreader.ReadInt();
                        LawlessCharge = datreader.ReadInt();
                        LawAuthCharge = datreader.ReadInt();
                        ChangeTravelCharge = datreader.ReadInt();
                        UpdateEnemyCharge = datreader.ReadInt();
                        EmissaryCharge = datreader.ReadInt();
                        EvocatorCharge = datreader.ReadInt();
                        AlchemistCharge = datreader.ReadInt();
                        AnimalTrainerCharge = datreader.ReadInt();
                        BankerCharge = datreader.ReadInt();
                        InnkeeperCharge = datreader.ReadInt();
                        MageCharge = datreader.ReadInt();
                        ProvisionerCharge = datreader.ReadInt();
                        ArmsTrainerCharge = datreader.ReadInt();
                        MageTrainerCharge = datreader.ReadInt();
                        RogueTrainerCharge = datreader.ReadInt();
                        LookoutCharge = datreader.ReadInt();
                        TownCrierCharge = datreader.ReadInt();
                        goto case 1;
                    case 1:
                        Hue = datreader.ReadInt();
                        NoneToLow = datreader.ReadInt();
                        LowToMedium = datreader.ReadInt();
                        MediumToHigh = datreader.ReadInt();
                        HighToBooming = datreader.ReadInt();
                        break;
                }

                datreader.Close();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            //try to initialize informational fee stuff - not crucial as this is informational
            // and gets set when the activity calcs are done
            try
            {
                FeePercentageCalc = (double)BaseFee / 2500.0;
                FeeAcctInfoCount = (int)(FeePercentageCalc * 1000.0);
            }
            catch { }
        }
        #endregion

        //PIX - added these for information - they don't get saved or anything
        public static double FeePercentageCalc = 1.0;
        public static int FeeAcctInfoCount = 1000;

        //The guts... with initial values before load

        public static int Hue = 0x333;

        //these numbers are: number of people who enter the town in a week's time - every char
        // being counted once per day.  So theoretically, if someone knew how this worked, one
        // account could keep the setting at 35 if it logged in each of its characters every
        // day and visited the town.
        public static int NoneToLow = 36;
        public static int LowToMedium = 72;
        public static int MediumToHigh = 144;
        public static int HighToBooming = 288;

        //Deed cost!
        public static int TSDeedCost = 250000;
        //InitialDeposit
        public static int InitialFunds = 125000;

        //daily fees
        public static int BaseFee = 2500;
        public static int ExtendedFee = 2500;
        public static int NoGateOutFee = 1000;
        public static int NoGateInFee = 1000;
        public static int NoRecallOutFee = 1000;
        public static int NoRecallInFee = 1000;
        public static int LawlessFee = 2000;
        public static int LawAuthFee = 5000;
        public static int NPCType1Fee = 1000;
        public static int NPCType2Fee = 2000;
        public static int NPCType3Fee = 5000;

        //charges
        public static int LawNormCharge = 5000;
        public static int LawlessCharge = 500000;
        public static int LawAuthCharge = 1000000;
        public static int ChangeTravelCharge = 25000;
        public static int UpdateEnemyCharge = 1000;
        //npc prices
        public static int EmissaryCharge = 100000;
        public static int EvocatorCharge = 100000;
        public static int AlchemistCharge = 100000;
        public static int AnimalTrainerCharge = 100000;
        public static int BankerCharge = 1000000;
        public static int InnkeeperCharge = 100000;
        public static int MageCharge = 100000;
        public static int ProvisionerCharge = 20000;
        public static int ArmsTrainerCharge = 20000;
        public static int MageTrainerCharge = 20000;
        public static int RogueTrainerCharge = 20000;
        public static int LookoutCharge = 50000;
        public static int FightBrokerCharge = 100000;
        public static int AuctioneerCharge = 250000;
        public static int TownCrierCharge = 5000000;

        //placement requirement numbers
        public static double GuildHousePercentage = 0.75;

        //AND FINALLY... modifiers
        public static double LLModifierNone = 10.0;
        public static double LLModifierLow = 5.0;
        public static double LLModifierMed = 2.5;
        public static double LLModifierHigh = 1.0;
        public static double LLModifierBoom = 0.5;

        public static double NPCModifierNone = 10.0;
        public static double NPCModifierLow = 6.0;
        public static double NPCModifierMed = 3.0;
        public static double NPCModifierHigh = 1.5;
        public static double NPCModifierBoom = 1.0;

        public static double BaseModifierNone = 5.0;
        public static double BaseModifierLow = 2.0;
        public static double BaseModifierMed = 1.5;
        public static double BaseModifierHigh = 1.0;
        public static double BaseModifierBoom = 0.75;


        //wall stuff
        public static int WallTeleporterDistance = 2;

        //auctioneer specials
        public static double AuctioneerPayoutPercentage = 1.0; //Initial setting is 100% is paid to the auction owner

        public static int WallHitsDecay = 0;

    }

    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class TownshipConsole : Item
    {
        [Constructable]
        public TownshipConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            base.Hue = Township.TownshipSettings.Hue;
            Name = "Township Settings Console";
        }

        public TownshipConsole(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
            {
                from.SendGump(new Gumps.PropertiesGump(from, this));
            }
        }

        //PIX - added these for information
        [CommandProperty(AccessLevel.GameMaster)]
        public double FeePercentageCalc
        {
            get { return TownshipSettings.FeePercentageCalc; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public double FeeAcctInfoCount
        {
            get { return TownshipSettings.FeeAcctInfoCount; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double AuctioneerPayoutPercentage
        {
            get { return TownshipSettings.AuctioneerPayoutPercentage; }
            set
            {
                if (value > 0.0 && value <= 1.0) //make sure payout is > 0 and <= 1
                {
                    TownshipSettings.AuctioneerPayoutPercentage = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallHitsDecay
        {
            get { return TownshipSettings.WallHitsDecay; }
            set
            {
                if (value >= 0)
                {
                    TownshipSettings.WallHitsDecay = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int WallTeleporterDistance
        {
            get { return TownshipSettings.WallTeleporterDistance; }
            set { TownshipSettings.WallTeleporterDistance = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TownshipHue
        {
            get
            {
                return TownshipSettings.Hue;
            }
            set
            {
                TownshipSettings.Hue = value;
                base.Hue = TownshipSettings.Hue;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_NoneToLow
        {
            get
            {
                return TownshipSettings.NoneToLow;
            }
            set
            {
                TownshipSettings.NoneToLow = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_LowToMedium
        {
            get
            {
                return TownshipSettings.LowToMedium;
            }
            set
            {
                TownshipSettings.LowToMedium = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_MediumToHigh
        {
            get
            {
                return TownshipSettings.MediumToHigh;
            }
            set
            {
                TownshipSettings.MediumToHigh = value;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AL_HighToBooming
        {
            get
            {
                return TownshipSettings.HighToBooming;
            }
            set
            {
                TownshipSettings.HighToBooming = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TSDeedCost
        {
            get
            {
                return TownshipSettings.TSDeedCost;
            }
            set
            {
                TownshipSettings.TSDeedCost = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int InitialFunds
        {
            get
            {
                return TownshipSettings.InitialFunds;
            }
            set
            {
                TownshipSettings.InitialFunds = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int BaseFee
        {
            get { return TownshipSettings.BaseFee; }
            set { TownshipSettings.BaseFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ExtendedFee
        {
            get { return TownshipSettings.ExtendedFee; }
            set { TownshipSettings.ExtendedFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoGateOutFee
        {
            get { return TownshipSettings.NoGateOutFee; }
            set { TownshipSettings.NoGateOutFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoGateInFee
        {
            get { return TownshipSettings.NoGateInFee; }
            set { TownshipSettings.NoGateInFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoRecallOutFee
        {
            get { return TownshipSettings.NoRecallOutFee; }
            set { TownshipSettings.NoRecallOutFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NoRecallInFee
        {
            get { return TownshipSettings.NoRecallInFee; }
            set { TownshipSettings.NoRecallInFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawlessFee
        {
            get { return TownshipSettings.LawlessFee; }
            set { TownshipSettings.LawlessFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawAuthFee
        {
            get { return TownshipSettings.LawAuthFee; }
            set { TownshipSettings.LawAuthFee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType1Fee
        {
            get { return TownshipSettings.NPCType1Fee; }
            set { TownshipSettings.NPCType1Fee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType2Fee
        {
            get { return TownshipSettings.NPCType2Fee; }
            set { TownshipSettings.NPCType2Fee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int NPCType3Fee
        {
            get { return TownshipSettings.NPCType3Fee; }
            set { TownshipSettings.NPCType3Fee = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawNormCharge
        {
            get { return TownshipSettings.LawNormCharge; }
            set { TownshipSettings.LawNormCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawlessCharge
        {
            get { return TownshipSettings.LawlessCharge; }
            set { TownshipSettings.LawlessCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LawAuthCharge
        {
            get { return TownshipSettings.LawAuthCharge; }
            set { TownshipSettings.LawAuthCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ChangeTravelCharge
        {
            get { return TownshipSettings.ChangeTravelCharge; }
            set { TownshipSettings.ChangeTravelCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int UpdateEnemyCharge
        {
            get { return TownshipSettings.UpdateEnemyCharge; }
            set { TownshipSettings.UpdateEnemyCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EmissaryCharge
        {
            get { return TownshipSettings.EmissaryCharge; }
            set { TownshipSettings.EmissaryCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EvocatorCharge
        {
            get { return TownshipSettings.EvocatorCharge; }
            set { TownshipSettings.EvocatorCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AlchemistCharge
        {
            get { return TownshipSettings.AlchemistCharge; }
            set { TownshipSettings.AlchemistCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AnimalTrainerCharge
        {
            get { return TownshipSettings.AnimalTrainerCharge; }
            set { TownshipSettings.AnimalTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int BankerCharge
        {
            get { return TownshipSettings.BankerCharge; }
            set { TownshipSettings.BankerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int InnkeeperCharge
        {
            get { return TownshipSettings.InnkeeperCharge; }
            set { TownshipSettings.InnkeeperCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MageCharge
        {
            get { return TownshipSettings.MageCharge; }
            set { TownshipSettings.MageCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ProvisionerCharge
        {
            get { return TownshipSettings.ProvisionerCharge; }
            set { TownshipSettings.ProvisionerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ArmsTrainerCharge
        {
            get { return TownshipSettings.ArmsTrainerCharge; }
            set { TownshipSettings.ArmsTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MageTrainerCharge
        {
            get { return TownshipSettings.MageTrainerCharge; }
            set { TownshipSettings.MageTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int RogueTrainerCharge
        {
            get { return TownshipSettings.RogueTrainerCharge; }
            set { TownshipSettings.RogueTrainerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int LookoutCharge
        {
            get { return TownshipSettings.LookoutCharge; }
            set { TownshipSettings.LookoutCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int FightBrokerCharge
        {
            get { return TownshipSettings.FightBrokerCharge; }
            set { TownshipSettings.FightBrokerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AuctioneerCharge
        {
            get { return TownshipSettings.AuctioneerCharge; }
            set { TownshipSettings.AuctioneerCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int TownCrierCharge
        {
            get { return TownshipSettings.TownCrierCharge; }
            set { TownshipSettings.TownCrierCharge = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double GuildHousePercentage
        {
            get { return TownshipSettings.GuildHousePercentage; }
            set { if (value <= 1.0 && value > 0.0) TownshipSettings.GuildHousePercentage = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierNone
        {
            get { return TownshipSettings.LLModifierNone; }
            set { if (value >= 0.0) TownshipSettings.LLModifierNone = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierLow
        {
            get { return TownshipSettings.LLModifierLow; }
            set { if (value >= 0.0) TownshipSettings.LLModifierLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierMed
        {
            get { return TownshipSettings.LLModifierMed; }
            set { if (value >= 0.0) TownshipSettings.LLModifierMed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierHigh
        {
            get { return TownshipSettings.LLModifierHigh; }
            set { if (value >= 0.0) TownshipSettings.LLModifierHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double LLModifierBoom
        {
            get { return TownshipSettings.LLModifierBoom; }
            set { if (value >= 0.0) TownshipSettings.LLModifierBoom = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierNone
        {
            get { return TownshipSettings.NPCModifierNone; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierNone = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierLow
        {
            get { return TownshipSettings.NPCModifierLow; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierMed
        {
            get { return TownshipSettings.NPCModifierMed; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierMed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierHigh
        {
            get { return TownshipSettings.NPCModifierHigh; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double NPCModifierBoom
        {
            get { return TownshipSettings.NPCModifierBoom; }
            set { if (value >= 0.0) TownshipSettings.NPCModifierBoom = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierNone
        {
            get { return TownshipSettings.BaseModifierNone; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierNone = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierLow
        {
            get { return TownshipSettings.BaseModifierLow; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierLow = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierMed
        {
            get { return TownshipSettings.BaseModifierMed; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierMed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierHigh
        {
            get { return TownshipSettings.BaseModifierHigh; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierHigh = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double BaseModifierBoom
        {
            get { return TownshipSettings.BaseModifierBoom; }
            set { if (value >= 0.0) TownshipSettings.BaseModifierBoom = value; }
        }

        #region Serialize/Deserialize - nothing to do
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
        #endregion
    }

}
