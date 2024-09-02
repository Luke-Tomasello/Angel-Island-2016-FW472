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

/* Scripts/Engines/Township/TownshipStone.cs
 * CHANGELOG:
 *  8/26/2024, Adam (Delete())
 *      1. BaseFortificationWall now saves a reference to the TownshipStone to which it belongs.
 *      2. BaseFortificationWall is now ITownshipItem
 *      We use this to cleanup all ITownshipItems when the stone is Deleted().
 * 7/13/10, Pix
 *      Change so all allies can access stone - instead of allies who are friends of the house that the stone is in.
 * 5/8/10, adam
 *		Add TownshipMember(Mobile m) to determine if some random mobile is a township member
 * 2010-05-02, SMD
 *      ensure lightlevel is set to -1 - this makes the township follow normal lightcycle rules
 * 8/2/08, Pix
 *		Added UpdateRegionName() function for when a guild name changes.
 * 8/2/08, Pix
 *		Added CanExtendResult enum and return this from CanExtend() function (instead of a bool).
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	07/13/08, Pix
 *		Added TownshipCenter variable/property to enable possible movement of the stone.
 *	01/18/08, Pix
 *		Allies that are kin-interferers will no longer be considered enemies.
 *	12/11/07, Pix
 *		Allies that are friends of the house can now access the township stone.
 *	5/14/07, Pix
 *		On delete of township stone, make sure to remove the townshipstone from the global list
 *	5/14/07, Pix
 *		Fixed growth (after none->low, it always took 1 week longer than wanted)
 *		Added accessor to WeeksAtThisLevel
 *	Pix: 5/3/07, Pix
 *		Fixed potential re-setting of travel rules on restart.
 *	Pix: 4/30/07,
 *		Changed enter message under 'normal' counting rules.
 *		Added property for ALLastCalculated.
 *	Pix: 4/22/07,
 *		Tweaked enter message to make it consistent with the exit message.
 *	Pix: 3/18/07
 *		Fixed NoGateOut charge.
 *	Pix: 4/19/07
 *		Added dials for all fees/charges and modifiers.
 */

using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Server.Items
{
    public enum CanExtendResult
    {
        CanExtend,
        ConflictingRegion,
        HousingPercentage,
        Unknown
    }

    public class TownshipStone : Server.Items.RegionControl
    {
        #region Statics and Constants
        public const int MAXGOLDHELD = 10000000; //max gold the fund can have

        public const int INITIAL_RADIUS = 50;
        public const int EXTENDED_RADIUS = 75;

        public static List<TownshipStone> AllTownshipStones = new List<TownshipStone>();
        #endregion

        private Guild m_guild = null;
        private bool m_showTownshipMessages = true;
        private DateTime m_BuiltOn;
        private int m_goldHeld = 0;

        private bool m_bExtended = false;
        private string m_strLastFees = "";
        private List<string> m_FeeRecord = new List<string>();
        private List<string> m_DepositRecord = new List<string>();

        private ArrayList m_Enemies;

        private Point3D m_TownshipCenter;

        private bool m_bOutsideNPCInterctionAllowed;

        //Note: these 4 datetimes don't need to be serialized
        private DateTime m_LastToggleRecallOut = DateTime.MinValue;
        private DateTime m_LastToggleRecallIn = DateTime.MinValue;
        private DateTime m_LastToggleGateOut = DateTime.MinValue;
        private DateTime m_LastToggleGateIn = DateTime.MinValue;

        private Township.LawLevel m_LawLevel = Server.Township.LawLevel.NONE;

        private int[] m_uniqueVisitors = new int[7];
        private int m_visitorsIndex;

        private ArrayList m_todaysVisitors = new ArrayList();
        private DateTime m_lastEnter = DateTime.MinValue;

        private Township.ActivityLevel m_ActivityLevel; //this is the 'size' that the town has grown to
        private Township.ActivityLevel m_LastActualActivityLevel; //this is the actual activity of the town last week (what we base NPC charges on)
        private int m_LastActualActivityWeekTotal = 0;
        private int m_WeeksAtThisLevel = 0;
        private DateTime m_ALLastCalculated;

        public DateTime ALLastCalculated
        {
            get { return m_ALLastCalculated; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Point3D TownshipCenter
        {
            get { return m_TownshipCenter; }
            set { m_TownshipCenter = value; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int WeeksAtThisLevel
        {
            get { return m_WeeksAtThisLevel; }
            set { m_WeeksAtThisLevel = value; }
        }

        #region Constructors and Setup
        public TownshipStone()
            : base()
        {
            m_Enemies = new ArrayList();

            this.Name = "a township stone";
            Setup();

            //initial values
            this.AllowTravelSpellsInRegion = true;
            this.NoGateInto = false;
            this.NoRecallInto = false;

            m_BuiltOn = DateTime.UtcNow;
            AllTownshipStones.Add(this);
        }

        //Note: this should be the only real creation point for the stone.
        public TownshipStone(Guild g)
            : this()
        {
            m_guild = g;
            m_guild.TownshipStone = this;
            this.RegionName = "the township of " + g.Name;
            Setup();

            //initial values
            this.AllowTravelSpellsInRegion = true;
            this.NoGateInto = false;
            this.NoRecallInto = false;

            m_BuiltOn = DateTime.UtcNow;
            //note: AllTownshipStones.Add(this) is handled by above constructor

            SetGuildedHousesNotGrandfathered();
        }

        public TownshipStone(Serial serial)
            : base(serial)
        {
            m_Enemies = new ArrayList();

            //safety
            Setup();

            AllTownshipStones.Add(this);
        }

        private void Setup()
        {
            this.Movable = false;
            this.IsGuarded = false;
            this.Visible = true;
            this.ItemID = 0xED4;
            this.Hue = Township.TownshipSettings.Hue;
            this.CanRessurect = true;
            this.CanUsePotions = true;
            this.CanUseStuckMenu = true;
            this.AllowHousing = true;

            this.LightLevel = -1; //ensure that we follow normal lightcycle rules
        }

        // return the township associated with this player mobile
        public static TownshipStone TownshipMember(Mobile m)
        {
            foreach (TownshipStone ts in AllTownshipStones)
            {
                if (ts != null && ts.Deleted == false)
                {
                    if (ts.Guild != null)
                    {
                        if (ts.Guild.IsMember(m))
                            return ts;
                    }
                }
            }

            return null;
        }

        private void SetGuildedHousesNotGrandfathered()
        {
            List<BaseHouse> bhl = this.HousesInRegion;
            foreach (BaseHouse house in bhl)
            {
                if (house.Owner != null && house.Owner.Guild != null
                    && house.Owner.Guild == this.Guild)
                {
                    house.LastTraded = DateTime.UtcNow.AddSeconds(10.0);
                }
            }
        }

        #endregion

        public bool IsEnemy(PlayerMobile pm)
        {
            if (pm == null)
            {
                return false;
            }

            if (pm.Guild != null)
            {
                if (Guild.IsAlly(pm.Guild as Guild)) //This is to rule out allied outcasts/kin interferers as enemies
                {
                }
                else
                {
                    if (Guild.IsEnemy(pm.Guild as Guild))
                    {
                        return true;
                    }

                    if (Guild.IOBAlignment != IOBAlignment.None &&
                        pm.IOBAlignment != IOBAlignment.None &&
                        Guild.IOBAlignment != pm.IOBAlignment)
                    {
                        return true;
                    }
                }
            }

            if (m_Enemies.Contains(pm))
            {
                return true;
            }

            return false;
        }

        public ArrayList Enemies
        {
            get { return m_Enemies; }
        }

        public void AddEnemy(PlayerMobile pm)
        {
            if (m_Enemies.Contains(pm) == false)
            {
                m_Enemies.Add(pm);
            }
        }

        public int SyncEnemies()
        {
            int iReturn = 0;
            try
            {
                m_Enemies.Clear();
                List<BaseHouse> houseList = HousesInRegion;

                foreach (BaseHouse house in houseList)
                {
                    if (house.Owner.Guild == this.Guild ||
                        this.Guild.IsAlly(house.Owner.Guild as Guild))
                    {
                        foreach (Mobile m in house.Bans)
                        {
                            if (m != null && m is PlayerMobile)
                            {
                                AddEnemy((PlayerMobile)m);
                            }
                        }
                    }
                }

                iReturn = m_Enemies.Count;
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e);
            }

            return iReturn;
        }

        public void AddFeeRecord(string fee)
        {
            while (m_FeeRecord.Count >= 10)
            {
                m_FeeRecord.RemoveAt(0);
            }

            m_FeeRecord.Add(fee);
        }

        public void AddDepositRecord(string dep)
        {
            while (m_DepositRecord.Count >= 10)
            {
                m_DepositRecord.RemoveAt(0);
            }
            m_DepositRecord.Add(dep);
        }

        public string LastFeesHTML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Latest Fees:<br>");
                for (int i = 0; i < m_FeeRecord.Count; i++)
                {
                    sb.Append("<br>");
                    sb.Append(m_FeeRecord[i]);
                }
                return sb.ToString();
            }
        }
        public string LastDepositHTML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Latest Deposits:<br>");
                for (int i = 0; i < m_DepositRecord.Count; i++)
                {
                    sb.Append("<br>");
                    sb.Append(m_DepositRecord[i]);
                }
                return sb.ToString();
            }
        }
        public string DailyFeesHTML
        {
            get
            {
                return m_strLastFees;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool OutsideNPCInterctionAllowed
        {
            get { return m_bOutsideNPCInterctionAllowed; }
            set { m_bOutsideNPCInterctionAllowed = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Extended
        {
            get { return m_bExtended; }
        }

        public void ExtendRegion()
        {
            Coords.Clear();
            CreateExtendedArea();
            m_bExtended = true;
        }

        public void ReduceRegion()
        {
            Coords.Clear();
            CreateInitialArea(this.m_TownshipCenter);
            m_bExtended = false;
        }

        public CanExtendResult CanExtendRegion(Mobile from) //note: from can be null by design
        {
            bool bConflicts = TownshipDeed.DoesTownshipRegionConflict(this.TownshipCenter, this.Map, EXTENDED_RADIUS, this.MyRegion as TownshipRegion);
            bool bHouseOwnershipCheck = false;

            double percentage = TownshipDeed.GetPercentageOfGuildedHousesInArea(this.TownshipCenter, this.Map, EXTENDED_RADIUS, this.Guild, true);
            if (percentage >= Township.TownshipSettings.GuildHousePercentage)
            {
                bHouseOwnershipCheck = true;
            }

            if (from != null)
            {
                if (bConflicts)
                {
                    from.SendMessage("Extended area would conflict with another township or a region controlled by Lord British.");
                }

                if (bHouseOwnershipCheck == false)
                {
                    from.SendMessage("Your guild wouldn't own enough of the houses in the extended area.");
                }
            }

            CanExtendResult result = CanExtendResult.CanExtend;

            if (bConflicts) result = CanExtendResult.ConflictingRegion;
            else if (bHouseOwnershipCheck == false) result = CanExtendResult.HousingPercentage;

            return result;
        }

        public override void Delete()
        {
            try
            {
                if (this.Guild != null)
                {
                    this.Guild.TownshipStone = null;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            //First delete all the township vendors.
            while (this.TownshipMobiles.Count > 0)
            {
                this.TownshipMobiles[0].Delete();
            }

            //delete me from the global list of township stones
            AllTownshipStones.Remove(this);

            // remove all ITownshipItems
            foreach (Item item in World.Items.Values)
                if (item is ITownshipItem ts && ts.Stone == this)
                    item.Delete();

            base.Delete();
        }

        //returns number of townships removed
        public static int DoAllTownshipCharges()
        {
            int numberoftownships = AllTownshipStones.Count;
            for (int i = AllTownshipStones.Count - 1; i >= 0; i--)
            {
                AllTownshipStones[i].DoDailyTownshipCharges();
            }

            return numberoftownships - AllTownshipStones.Count;
        }
        public static string GetTownshipSizeDesc(Township.ActivityLevel al)
        {
            switch (al)
            {
                case Server.Township.ActivityLevel.NONE:
                    return "locality";
                case Server.Township.ActivityLevel.LOW:
                    return "Hamlet";
                case Server.Township.ActivityLevel.MEDIUM:
                    return "Village";
                case Server.Township.ActivityLevel.HIGH:
                    return "Township";
                case Server.Township.ActivityLevel.BOOMING:
                    return "City";
            }
            return "ERROR";
        }
        public static string GetTownshipActivityDesc(Township.ActivityLevel al)
        {
            switch (al)
            {
                case Server.Township.ActivityLevel.NONE:
                    return "Stagnant";
                case Server.Township.ActivityLevel.LOW:
                    return "Declining";
                case Server.Township.ActivityLevel.MEDIUM:
                    return "Stable";
                case Server.Township.ActivityLevel.HIGH:
                    return "Growing";
                case Server.Township.ActivityLevel.BOOMING:
                    return "Booming";
            }
            return "ERROR";
        }

        public void UpdateRegionName()
        {
            if (this.GuildName != null && this.GuildName.Length > 0)
            {
                this.RegionName = "the township of " + this.GuildName;
            }
        }

        public bool DoDailyTownshipCharges()
        {
            bool bReturn = true;

            int feeForToday = GetTotalFeePerDay(true);

            if (feeForToday <= this.GoldHeld)
            {
                AddFeeRecord("Daily charge: " + feeForToday);
                this.GoldHeld -= feeForToday;
            }
            else
            {
                string message = string.Format("Township {0} is being deleted: daily charge: {1} funds: {2}",
                    this.GuildName, TotalFeePerDay, GoldHeld);
                Server.Commands.LogHelper log = new Server.Commands.LogHelper("township.log", false);
                log.Log(message);
                log.Finish();
                this.Delete();
            }


            //finally, after we've done the daily charges, make sure that
            // we update our activity level if needed
            if (IsActivityLevelCalcNeeded())
            {
                CalculateActivityLevel();
            }

            return bReturn;
        }

        public double RLDaysLeftInFund
        {
            get
            {
                double days = (double)this.GoldHeld / (double)this.TotalFeePerDay;
                return days;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int GoldHeld
        {
            get { return m_goldHeld; }
            set { m_goldHeld = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public DateTime BuiltOn
        {
            get { return m_BuiltOn; }
            set { m_BuiltOn = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastToggleRecallOut
        {
            get { return m_LastToggleRecallOut; }
            set { m_LastToggleRecallOut = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastToggleRecallIn
        {
            get { return m_LastToggleRecallIn; }
            set { m_LastToggleRecallIn = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastToggleGateOut
        {
            get { return m_LastToggleGateOut; }
            set { m_LastToggleGateOut = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastToggleGateIn
        {
            get { return m_LastToggleGateIn; }
            set { m_LastToggleGateIn = value; }
        }

        public int GetTotalFeePerDay(bool recordFees)
        {
            //Make sure special NPC requirements are met.
            CheckSpecialNPCRequiredSettings();

            StringBuilder feeListing = new StringBuilder();

            double basefee_activitymodifier = 1; //assume no modifier
                                                 //switch (this.ActivityLevel)
            switch (this.m_LastActualActivityLevel)
            {
                case Server.Township.ActivityLevel.NONE:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierNone;
                    break;
                case Server.Township.ActivityLevel.LOW:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierLow;
                    break;
                case Server.Township.ActivityLevel.MEDIUM:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierMed;
                    break;
                case Server.Township.ActivityLevel.HIGH:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierHigh;
                    break;
                case Server.Township.ActivityLevel.BOOMING:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierBoom;
                    break;
            }

            int charge = (int)(Township.TownshipSettings.BaseFee * basefee_activitymodifier);

            if (recordFees)
            {
                feeListing.Append("<br>Base Fee: ");
                feeListing.Append(charge);
            }

            // NPC Charges
            charge += NPCChargePerRLDay;
            if (recordFees)
            {
                feeListing.Append("<br>NPC Charges: ");
                feeListing.Append(NPCChargePerRLDay);
            }

            //travel charges
            if (this.NoGateOut)
            {
                charge += Township.TownshipSettings.NoGateOutFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Gate Out: ");
                    feeListing.Append(Township.TownshipSettings.NoGateOutFee);
                }
            }
            if (this.NoGateInto)
            {
                charge += Township.TownshipSettings.NoGateInFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Gate In: ");
                    feeListing.Append(Township.TownshipSettings.NoGateInFee);
                }
            }
            if (this.NoRecallOut)
            {
                charge += Township.TownshipSettings.NoRecallOutFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Recall Out: ");
                    feeListing.Append(Township.TownshipSettings.NoRecallOutFee);
                }
            }
            if (this.NoRecallInto)
            {
                charge += Township.TownshipSettings.NoRecallInFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Recall In: ");
                    feeListing.Append(Township.TownshipSettings.NoRecallInFee);
                }
            }

            //lawlevel charges
            if (LawLevel != Server.Township.LawLevel.NONE)
            {
                charge += CalculateLawlevelFee(LawLevel, this.m_LastActualActivityLevel);//ActivityLevel);
                if (recordFees)
                {
                    feeListing.Append("<br>Lawlevel Charge: ");
                    feeListing.Append(CalculateLawlevelFee(LawLevel, this.m_LastActualActivityLevel));//ActivityLevel));
                }
            }

            //entended region charge
            if (this.Extended)
            {
                charge += (int)(Township.TownshipSettings.ExtendedFee * basefee_activitymodifier);
                if (recordFees)
                {
                    feeListing.Append("<br>Extended Area: ");
                    feeListing.Append((int)(Township.TownshipSettings.ExtendedFee * basefee_activitymodifier));
                }
            }

            //other features

            if (recordFees)
            {
                feeListing.Append("<br><br>Total: ");
                feeListing.Append(charge);

                m_strLastFees = feeListing.ToString();
            }

            return charge;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalFeePerDay
        {
            get
            {
                return GetTotalFeePerDay(false);
            }
        }

        public static int CalculateLawlevelFee(Township.LawLevel ll, Township.ActivityLevel al)
        {
            double modifier = 1.0;
            switch (al)
            {
                case Server.Township.ActivityLevel.NONE:
                    modifier = Township.TownshipSettings.LLModifierNone;
                    break;
                case Server.Township.ActivityLevel.LOW:
                    modifier = Township.TownshipSettings.LLModifierLow;
                    break;
                case Server.Township.ActivityLevel.MEDIUM:
                    modifier = Township.TownshipSettings.LLModifierMed;
                    break;
                case Server.Township.ActivityLevel.HIGH:
                    modifier = Township.TownshipSettings.LLModifierHigh;
                    break;
                case Server.Township.ActivityLevel.BOOMING:
                    modifier = Township.TownshipSettings.LLModifierBoom;
                    break;
            }

            int result = 0;
            switch (ll)
            {
                case Server.Township.LawLevel.NONE:
                    result = 0;
                    break;
                case Server.Township.LawLevel.LAWLESS:
                    result = (int)(Township.TownshipSettings.LawlessFee * modifier);
                    break;
                case Server.Township.LawLevel.AUTHORITY:
                    result = (int)(Township.TownshipSettings.LawAuthFee * modifier);
                    break;
            }

            return result;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NPCChargePerRLDay
        {
            get
            {
                //base cost
                double cost = 0;
                //additional cost for special tsnpcs
                foreach (Mobile m in this.TownshipMobiles)
                {
                    if (m is TSBanker ||
                        m is TSAnimalTrainer)
                    {
                        cost += Township.TownshipSettings.NPCType3Fee;
                    }
                    else if (m is TSAlchemist ||
                             m is TSMage ||
                             m is TSAuctioneer)
                    {
                        cost += Township.TownshipSettings.NPCType2Fee;
                    }
                    else
                    {
                        cost += Township.TownshipSettings.NPCType1Fee;
                    }
                }

                //switch (this.ActivityLevel)
                switch (this.m_LastActualActivityLevel)
                {
                    case Server.Township.ActivityLevel.NONE:
                        cost = cost * Township.TownshipSettings.NPCModifierNone;
                        break;
                    case Server.Township.ActivityLevel.LOW:
                        cost = cost * Township.TownshipSettings.NPCModifierLow;
                        break;
                    case Server.Township.ActivityLevel.MEDIUM:
                        cost = cost * Township.TownshipSettings.NPCModifierMed;
                        break;
                    case Server.Township.ActivityLevel.HIGH:
                        cost = cost * Township.TownshipSettings.NPCModifierHigh;
                        break;
                    case Server.Township.ActivityLevel.BOOMING:
                        cost = cost * Township.TownshipSettings.NPCModifierBoom;
                        break;
                }

                return (int)cost;
            }
        }

        private bool IsActivityLevelCalcNeeded()
        {
            bool bReturn = false;

            //Pix: because heartbeat jobs aren't exact, we want to add a little 'flex room' for them,
            // change from 7.0 days (10080 minutes) to 10070 minutes, giving a 10-minute time buffer
            bReturn = (m_ALLastCalculated + TimeSpan.FromMinutes(10070) < DateTime.UtcNow);

            if (Server.Misc.TestCenter.Enabled)
            {
                bReturn = (m_ALLastCalculated + TimeSpan.FromDays(0.5) < DateTime.UtcNow);
            }

            return bReturn;
        }

        private void CalculateActivityLevel()
        {
            int pastSevenDaysVisitors = this.WeeklyVisitors;

            switch (m_ActivityLevel)
            {
                case Server.Township.ActivityLevel.NONE:
                    if (pastSevenDaysVisitors > Township.TownshipSettings.NoneToLow)
                    {
                        //Should bump up to low after one week, so bump it.
                        m_ActivityLevel = Server.Township.ActivityLevel.LOW;
                    }
                    break;
                case Server.Township.ActivityLevel.LOW:
                    if (pastSevenDaysVisitors > Township.TownshipSettings.LowToMedium)
                    {
                        if (m_WeeksAtThisLevel >= 1)
                        {
                            m_ActivityLevel = Server.Township.ActivityLevel.MEDIUM;
                            m_WeeksAtThisLevel = 0;
                        }
                        else
                        {
                            m_WeeksAtThisLevel++;
                        }
                    }
                    else
                    {
                        // didn't meet the activity requirements this week, set counter to 0
                        m_WeeksAtThisLevel = 0;
                    }
                    break;
                case Server.Township.ActivityLevel.MEDIUM:
                    if (pastSevenDaysVisitors > Township.TownshipSettings.MediumToHigh)
                    {
                        if (m_WeeksAtThisLevel >= 2)
                        {
                            m_ActivityLevel = Server.Township.ActivityLevel.HIGH;
                            m_WeeksAtThisLevel = 0;
                        }
                        else
                        {
                            m_WeeksAtThisLevel++;
                        }
                    }
                    else
                    {
                        // didn't meet the activity requirements this week, set counter to 0
                        m_WeeksAtThisLevel = 0;
                    }
                    break;
                case Server.Township.ActivityLevel.HIGH:
                    if (pastSevenDaysVisitors > Township.TownshipSettings.HighToBooming)
                    {
                        if (m_WeeksAtThisLevel >= 3)
                        {
                            m_ActivityLevel = Server.Township.ActivityLevel.BOOMING;
                            m_WeeksAtThisLevel = 0;
                        }
                        else
                        {
                            m_WeeksAtThisLevel++;
                        }
                    }
                    else
                    {
                        // didn't meet the activity requirements this week, set counter to 0
                        m_WeeksAtThisLevel = 0;
                    }
                    break;
                case Server.Township.ActivityLevel.BOOMING:
                    //already biggest, do nothing
                    break;
            }

            m_LastActualActivityLevel = Server.Township.ActivityLevel.NONE;
            if (pastSevenDaysVisitors > Township.TownshipSettings.HighToBooming)
                m_LastActualActivityLevel = Server.Township.ActivityLevel.BOOMING;
            else if (pastSevenDaysVisitors > Township.TownshipSettings.MediumToHigh)
                m_LastActualActivityLevel = Server.Township.ActivityLevel.HIGH;
            else if (pastSevenDaysVisitors > Township.TownshipSettings.LowToMedium)
                m_LastActualActivityLevel = Server.Township.ActivityLevel.MEDIUM;
            else if (pastSevenDaysVisitors > Township.TownshipSettings.NoneToLow)
                m_LastActualActivityLevel = Server.Township.ActivityLevel.LOW;

            m_LastActualActivityWeekTotal = pastSevenDaysVisitors;
            m_ALLastCalculated = DateTime.UtcNow;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Township.ActivityLevel LastActivityLevel
        {
            get
            {
                return m_LastActualActivityLevel;
            }
            set
            {
                m_LastActualActivityLevel = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int LastActualWeekNumber
        {
            get
            {
                return m_LastActualActivityWeekTotal;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Township.ActivityLevel ActivityLevel
        {
            get
            {
                //Calculate Activity Level every week.
                if (IsActivityLevelCalcNeeded())
                {
                    CalculateActivityLevel();
                    return m_ActivityLevel;
                }
                else
                {
                    return this.m_ActivityLevel;
                }
            }
            set
            {
                m_ActivityLevel = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Township.LawLevel LawLevel
        {
            get
            {
                return m_LawLevel;
            }
            set
            {
                m_LawLevel = value;
            }
        }

        #region TRSorter class
        private class TRSorter : System.Collections.IComparer
        {
            public TRSorter()
            {
            }

            public int Compare(object x, object y)
            {
                if (x is TownshipRegion && y is TownshipRegion)
                {
                    return ((TownshipRegion)x).TStone.WeeklyVisitors - ((TownshipRegion)y).TStone.WeeklyVisitors;
                }
                else
                {
                    throw new ArgumentException("x and y need to both be TownshipRegions");
                }
            }
        }
        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowTownshipMessages
        {
            get { return m_showTownshipMessages; }
            set { m_showTownshipMessages = value; }
        }

        private List<BaseHouse> HousesInRegion
        {
            get
            {
                List<BaseHouse> houseList = new List<BaseHouse>();

                if (this.MyRegion != null &&
                    this.MyRegion.Coords != null &&
                    this.MyRegion.Coords.Count > 0 &&
                    this.MyRegion.Coords[0] != null)
                {
                    Rectangle2D rect = (Rectangle2D)this.MyRegion.Coords[0];

                    if (true)//rect != null)
                    {
                        Map map = this.Map;
                        int z = this.Z;

                        int x_start = rect.Start.X;//x - TownshipStone.INITIAL_RADIUS;
                        int y_start = rect.Start.Y;// y - TownshipStone.INITIAL_RADIUS;
                        int x_end = rect.End.X;// x + TownshipStone.INITIAL_RADIUS;
                        int y_end = rect.End.Y;// x + TownshipStone.INITIAL_RADIUS;

                        for (int i = x_start; i < x_end; i++)
                        {
                            for (int j = y_start; j < y_end; j++)
                            {
                                Region r = Region.Find(new Point3D(i, j, z), map);
                                if (r != null)
                                {
                                    if (r is HouseRegion)
                                    {
                                        BaseHouse h = ((HouseRegion)r).House;
                                        if (!houseList.Contains(h))
                                        {
                                            houseList.Add(h);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return houseList;
            }
        }

        public List<Mobile> TownshipMobiles
        {
            get
            {
                List<Mobile> mobiles = new List<Mobile>();

                foreach (Mobile m in this.MyRegion.Mobiles.Values)
                {
                    Type type = m.GetType();

                    TownshipNPCAttribute[] attributearray = (TownshipNPCAttribute[])type.GetCustomAttributes(typeof(TownshipNPCAttribute), false);

                    if (attributearray.Length > 0)
                    {
                        mobiles.Add(m);
                    }
                }

                //SO!  Search for all houses in the area and check the NPCs in there as well.
                List<BaseHouse> bhList = HousesInRegion;
                foreach (BaseHouse house in bhList)
                {
                    foreach (Mobile m in house.Region.Mobiles.Values)
                    {
                        Type type = m.GetType();

                        TownshipNPCAttribute[] attributearray = (TownshipNPCAttribute[])type.GetCustomAttributes(typeof(TownshipNPCAttribute), false);

                        if (attributearray.Length > 0)
                        {
                            mobiles.Add(m);
                        }
                    }
                }

                mobiles.Sort(new NPCComparer());
                return mobiles;
            }
        }

        public void CheckSpecialNPCRequiredSettings()
        {
            if (!HasEmissaryNPC)
            {
                if (LawLevel == Server.Township.LawLevel.AUTHORITY)
                {
                    LawLevel = Server.Township.LawLevel.NONE;
                }
            }

            if (!HasEvocatorNPC)
            {
                this.NoRecallInto = false;
                this.NoRecallOut = false;
                this.NoGateInto = false;
                this.NoGateOut = false;
            }

            if (!HasTownCrierNPC && m_bExtended)
            {
                ReduceRegion();
            }
        }

        public void RemoveEmissary()
        {
            try
            {
                List<Mobile> TSMList = TownshipMobiles;
                for (int i = TSMList.Count - 1; i >= 0; i--)
                {
                    if (TSMList[i] is TSEmissary)
                    {
                        TSMList[i].Delete();
                    }
                }
            }
            catch (Exception e)
            {
                Server.Commands.LogHelper.LogException(e, "Pix: problem with RemoveEmissary()");
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasEmissaryNPC
        {
            get
            {
                List<Mobile> TSMList = TownshipMobiles;
                for (int i = 0; i < TSMList.Count; i++)
                {
                    if (TSMList[i] is TSEmissary)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasEvocatorNPC
        {
            get
            {
                List<Mobile> TSMList = TownshipMobiles;
                for (int i = 0; i < TSMList.Count; i++)
                {
                    if (TSMList[i] is TSEvocator)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasTownCrierNPC
        {
            get
            {
                List<Mobile> TSMList = TownshipMobiles;
                for (int i = 0; i < TSMList.Count; i++)
                {
                    if (TSMList[i] is TSTownCrier)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TownshipNPCCount
        {
            get
            {
                return TownshipMobiles.Count;
            }
        }

        public bool CanBuildHouseInTownship(PlayerMobile pm)
        {
            if (pm.Guild == this.Guild)
            {
                return true;
            }

            return false;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild
        {
            get { return this.m_guild; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item GuildStone
        {
            get
            {
                if (m_guild == null)
                {
                    return null;
                }

                return this.m_guild.Guildstone;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildName
        {
            get
            {
                if (m_guild == null)
                {
                    return "";
                }

                return m_guild.Name;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RegionType
        {
            get
            {
                if (this.Coords.Count > 0)
                {
                    return this.Coords[0].GetType().ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NoRecallOut
        {
            get
            {
                return this.RestrictedSpells[31];
            }
            set
            {
                this.RestrictedSpells[31] = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NoGateOut
        {
            get
            {
                return this.RestrictedSpells[51];
            }
            set
            {
                this.RestrictedSpells[51] = value;
            }
        }

        public override CustomRegion CreateRegion(RegionControl rc, Map map)
        {
            return new Server.Regions.TownshipRegion(rc, map);
        }

        public void CreateInitialArea(Point3D centerLocation)
        {
            DoChooseArea(null, this.Map, new Point3D(centerLocation.X - INITIAL_RADIUS, centerLocation.Y - INITIAL_RADIUS, centerLocation.Z), new Point3D(centerLocation.X + INITIAL_RADIUS, centerLocation.Y + INITIAL_RADIUS, centerLocation.Z), this, false);
            this.TownshipCenter = centerLocation;
        }

        public void CreateExtendedArea()
        {
            DoChooseArea(null, this.Map, new Point3D(this.TownshipCenter.X - EXTENDED_RADIUS, this.TownshipCenter.Y - EXTENDED_RADIUS, this.TownshipCenter.Z), new Point3D(this.TownshipCenter.X + EXTENDED_RADIUS, this.TownshipCenter.Y + EXTENDED_RADIUS, this.TownshipCenter.Z), this, false);
        }

        public void AccessBaseController(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Administrator)
            {
                base.OnDoubleClick(from);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (this.Guild == null || this.Guild.Disbanded || this.GuildStone == null || this.GuildStone.Deleted)
            {
                from.SendMessage("The guild no longer exists, so the township is gone!");
                this.Delete();
            }
            else if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(this);
                bool bFriendOfHouse = false;
                if (house != null)
                {
                    bFriendOfHouse = house.IsFriend(from);
                }

                //if we're GM+ OR we're the guildmember, open the control gump
                //Pix: changed so all allies can access stone.
                if (from.AccessLevel >= AccessLevel.GameMaster
                    || (from.Guild != null && from.Guild == this.Guild)
                    //					|| (this.Guild.IsAlly(from.Guild as Server.Guilds.Guild) && bFriendOfHouse)
                    || this.Guild.IsAlly(from.Guild as Server.Guilds.Guild)
                    )
                {
                    from.SendGump(new TownshipGump(from, this));
                }
                else
                {
                    from.SendMessage("You can't access this.");
                }
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public int WeeklyVisitors
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 7; i++)
                {
                    count += m_uniqueVisitors[i];
                }
                return count;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int DailyVisitors
        {
            get { return m_uniqueVisitors[m_visitorsIndex]; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int VisitorsIndex
        {
            get { return m_visitorsIndex; }
        }

        #region Visitors CommandProperties for Admin setting - BLECH!!!

        [CommandProperty(AccessLevel.Administrator)]
        public bool ForceCalculateAL
        {
            get { return false; }
            set
            {
                if (value)
                {
                    CalculateActivityLevel();
                }
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors0
        {
            get { return m_uniqueVisitors[0]; }
            set { m_uniqueVisitors[0] = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors1
        {
            get { return m_uniqueVisitors[1]; }
            set { m_uniqueVisitors[1] = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors2
        {
            get { return m_uniqueVisitors[2]; }
            set { m_uniqueVisitors[2] = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors3
        {
            get { return m_uniqueVisitors[3]; }
            set { m_uniqueVisitors[3] = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors4
        {
            get { return m_uniqueVisitors[4]; }
            set { m_uniqueVisitors[4] = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors5
        {
            get { return m_uniqueVisitors[5]; }
            set { m_uniqueVisitors[5] = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int Visitors6
        {
            get { return m_uniqueVisitors[6]; }
            set { m_uniqueVisitors[6] = value; }
        }
        #endregion

        public bool MurderZone
        {
            get
            {
                return true;
            }
        }

        public bool IsMobileCountable(Mobile attacker)
        {
            bool bReturn = true;

            switch (LawLevel)
            {
                case Township.LawLevel.LAWLESS:
                    bReturn = false;
                    break;
                case Township.LawLevel.AUTHORITY:
                    if (m_guild.IsMember(attacker) || m_guild.IsAlly(attacker.Guild as Server.Guilds.Guild))
                    {
                        bReturn = false;
                    }
                    else
                    {
                        bReturn = true;
                    }
                    break;
                default:
                case Township.LawLevel.NONE:
                    bReturn = true;
                    break;
            }

            return bReturn;
        }

        public void OnEnter(Mobile m)
        {
            bool bShowEnterMessage = m_showTownshipMessages;

            PlayerMobile pm = m as PlayerMobile;

            if (pm != null && pm.LastRegionIn is HouseRegion)
            {
                bShowEnterMessage = false;
            }


            if (m is PlayerMobile && m.AccessLevel == AccessLevel.Player) //only count players, not mobs or staff
            {
                if (m_lastEnter != DateTime.MinValue && m_lastEnter.Day < DateTime.UtcNow.Day)
                {
                    //enter next day
                    m_visitorsIndex++;
                    if (m_visitorsIndex > 6) m_visitorsIndex = 0;
                    //reset new day
                    m_todaysVisitors.Clear();
                    m_uniqueVisitors[m_visitorsIndex] = 0;
                }

                m_lastEnter = DateTime.UtcNow;

                if (!m_todaysVisitors.Contains(m))
                {
                    m_todaysVisitors.Add(m);
                    m_uniqueVisitors[m_visitorsIndex]++;
                }
            }

            if (bShowEnterMessage)
            {
                string ALName = TownshipStone.GetTownshipSizeDesc(ActivityLevel);
                StringBuilder enterMessage = new StringBuilder();
                enterMessage.Append("You have entered the ");
                enterMessage.Append(ALName);
                enterMessage.Append(" of the ");
                enterMessage.Append(GuildName);
                enterMessage.Append(".  ");

                switch (LawLevel)
                {
                    case Township.LawLevel.AUTHORITY:
                        enterMessage.Append("The ");
                        enterMessage.Append(GuildName);
                        enterMessage.Append(" has received a Grant of Authority by Lord British to enforce ");
                        enterMessage.Append("the law within this ");
                        enterMessage.Append(ALName);
                        enterMessage.Append(".");
                        enterMessage.Append("  The following guilds are authorized to protect this town: ");
                        try
                        {
                            enterMessage.Append(this.Guild.Abbreviation);
                            foreach (object o in this.Guild.Allies)
                            {
                                if (o is Guild)
                                {
                                    enterMessage.Append(", " + ((Guild)o).Abbreviation);
                                }
                            }
                        }
                        catch { }
                        break;
                    case Township.LawLevel.LAWLESS:
                        enterMessage.Append("Beware!  There are no laws being enforced within this ");
                        enterMessage.Append(ALName);
                        enterMessage.Append("!");
                        break;
                    case Township.LawLevel.NONE:
                    default:
                        //enterMessage.Append("Lord British enforces the laws within this ");
                        enterMessage.Append("Lord British will make note of any murders that are reported within this ");
                        enterMessage.Append(ALName);
                        enterMessage.Append(".");
                        break;
                }

                m.SendMessage(enterMessage.ToString());
            }
        }


        public void OnExit(Mobile m)
        {
            if (m_showTownshipMessages)
            {
                PlayerMobile pm = null;

                if (m is PlayerMobile)
                {
                    pm = (PlayerMobile)m;

                    if (pm != null && pm.Region is HouseRegion) //Nested regions are shitty, don't display exit message when entering a house
                        return;

                    string ALName = "locality";
                    ALName = TownshipStone.GetTownshipSizeDesc(ActivityLevel);
                    m.SendMessage("You have left the {0} of the {1}.", ALName, this.GuildName);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5); // version

            //version 5 additions
            writer.Write(m_bOutsideNPCInterctionAllowed);

            //version 4 additions:
            writer.Write(m_TownshipCenter);

            //version 3 additions:

            writer.Write(m_WeeksAtThisLevel);

            //version 2 additions

            writer.Write(m_LastActualActivityWeekTotal);

            //version 1 below:
            writer.Write((int)m_ActivityLevel);
            writer.Write((int)m_LastActualActivityLevel);
            writer.WriteDeltaTime(this.m_ALLastCalculated);

            writer.Write((int)m_LawLevel);

            writer.Write(this.m_goldHeld);
            writer.Write(m_DepositRecord.Count);
            for (int i = 0; i < m_DepositRecord.Count; i++)
            {
                writer.Write(m_DepositRecord[i]);
            }

            writer.Write(m_FeeRecord.Count);
            for (int i = 0; i < m_FeeRecord.Count; i++)
            {
                writer.Write(m_FeeRecord[i]);
            }
            writer.Write(m_strLastFees);

            writer.WriteMobileList(m_Enemies, true);

            writer.Write(this.m_visitorsIndex);
            for (int i = 0; i < 7; i++)
            {
                writer.Write(m_uniqueVisitors[i]);
            }
            writer.WriteDeltaTime(this.m_lastEnter);
            writer.WriteMobileList(this.m_todaysVisitors);

            writer.Write(m_bExtended);
            writer.Write(m_BuiltOn);
            writer.Write(this.m_guild);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    m_bOutsideNPCInterctionAllowed = reader.ReadBool();
                    goto case 4;
                case 4:
                    m_TownshipCenter = reader.ReadPoint3D();
                    goto case 3;
                case 3:
                    m_WeeksAtThisLevel = reader.ReadInt();
                    goto case 2;
                case 2:
                    m_LastActualActivityWeekTotal = reader.ReadInt();
                    goto case 1;
                case 1:
                    m_ActivityLevel = (Township.ActivityLevel)reader.ReadInt();
                    m_LastActualActivityLevel = (Township.ActivityLevel)reader.ReadInt();
                    m_ALLastCalculated = reader.ReadDeltaTime();

                    m_LawLevel = (Township.LawLevel)reader.ReadInt();

                    m_goldHeld = reader.ReadInt();
                    int iDRCount = reader.ReadInt();
                    for (int i = 0; i < iDRCount; i++)
                    {
                        AddDepositRecord(reader.ReadString());
                    }

                    int iFRCount = reader.ReadInt();
                    for (int i = 0; i < iFRCount; i++)
                    {
                        AddFeeRecord(reader.ReadString());
                    }
                    m_strLastFees = reader.ReadString();

                    m_Enemies = reader.ReadMobileList();

                    m_visitorsIndex = reader.ReadInt();
                    for (int i = 0; i < 7; i++)
                    {
                        m_uniqueVisitors[i] = reader.ReadInt();
                    }
                    m_lastEnter = reader.ReadDeltaTime();
                    m_todaysVisitors = reader.ReadMobileList();

                    m_bExtended = reader.ReadBool();
                    m_BuiltOn = reader.ReadDateTime(); ;
                    this.m_guild = reader.ReadGuild() as Guild;
                    break;
            }

            if (version < 4)
            {
                m_TownshipCenter = this.Location;
            }

            //SMD 2010-05-02 - ensure lightlevel is set to -1 - this makes the township follow normal lightcycle rules
            LightLevel = -1;
        }

        #region NPCComparer class
        private class NPCComparer : IComparer<Mobile>
        {
            public int Compare(Mobile x, Mobile y)
            {
                if (x == null)
                {
                    if (y == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    if (y == null)
                    {
                        return 1;
                    }
                    else
                    {
                        int retval = x.Name.CompareTo(y.Name);

                        return retval;
                    }
                }
            }
        }
        #endregion
    }
}
