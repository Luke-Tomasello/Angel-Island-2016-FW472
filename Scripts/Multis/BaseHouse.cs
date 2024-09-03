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

/* Scripts/Multis/BaseHouse.cs
 * ChangeLog
 *  9/3/21, Adam
 *      Added "(locked down)" and "(secured)" labels.
 *  8/26/2024, Adam (TentBackpack)
 *      Disallow players from "I wish to release this" on tent backpacks.
 *	3/17/16, Adam
 *		Add the special tent logic to IsInside() - (see those comments 3/16/16, 2/27/10.)
 *		This is called directly from placing a vendorTrntalContract in a tent.
 *		It's probably called from a number of other places.
 *	3/16/16, Adam
 *		FindHouseAy() - Put back the change I put in on 2/27/10 (see those comments.)
 *		Why was it reverted? 
 *		Anyway, since tents have mcl lists widths and heights of 0, you cannot use the traditional IsInside logic.
 *	2/15/11, adam
 *		Fixup AccountCode for version < 21
 *	2/14/11, Adam
 *		UOMO: Add Inheritance mechanism that allows a new character on an account to Inherit the house previously owned 
 *			on that account
 *	11/15/10, Adam
 *		Stop announcing IDOCs on TC for Siege
 *	2/28/10, Adam
 *		Prevent the owner from Demolishing or Transfering a house of the ManagedDemolishion flag is set on the house.
 *		The ManagedDemolishion flag is set on the house when this house annexed one of more tents. 
 *	2/27/10, adam
 *		WRT FindHouseAt(): the width and height of MultiComponentList Components for tents are all 0, so we can't use the IsInside() check
 *		when checking to see of the point (item or mobile) inside or outside the tent, we there PASS the test as TRUE.
 *		This change came about while modifying HousePlacement to allow houses to annex tents.
 *	9/29/08, Adam
 *		Add location to IDOC listing
 *	7/20/08, Pix
 *		De-coupled township stones from houses.
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *	4/1/08, Adam
 *		Add a new 32 bit m_NPCData variable to hold the MaximumBarkeepCount in byte 4
 *		The other 3 bytes are available for other NPC data
 *  12/28/07 Taran Kain
 *      Added Addons to the list of things adjusted in OnLocationChange, OnMapChange
 *      Clarified variable names in OnLocationChange
 *  12/17/07, Adam
 *      Add the new [GetDeed command to get a house deed from a house sign
 *      (useful for the new static houses)
 *	11/29/07, Adam
 *		Limit IDOC email warnings to test center
 *	11/29/07, Adam
 *		Add an email warning to owners of houses which are about to go IDOC (2 day warning)
 *	9/2/07, Adam
 *		Add a auto-resume-decay system so that we can feeze for a set amount of time.
 *	8/27/07, Adam
 *		don't list staff owned houses to the console because it lags the game (probably) because console IO is so slow.
 *		(not a problem until we open the preview neighborhood)
 *	8/26/07, Adam
 *		Remove TC only code from CheckLockboxDecay that was preventing decay while we recovered 
 *		lost lockboxes.
 *	8/23/07, Adam
 *		Make SetLockdown() public so that [Nuke may access it to release containers
 *	7/29/07, Adam
 *		Change the Property method name from m_DecayTime to StructureDecayTime
 *		Update LockBoxCount property to recalc the true LockBoxCount
 *			In Serialize, use the LockBoxCount property to make sure we write the correct number
 *	7/27/07, Adam
 *		- Add SuppressRegion property to turn on region suppression 
 *		- Added 32bit flags (move all bools here)
 *  6/12/07, Adam
 *      Added AdminTransfer() to allow unilateral annexation of a house.
 *      See Also: [TakeOwnership
 *  06/08/07, plasma
 *      Commented redundant HouseFoundation check in IsInside()
 *  6/7/07, Adam
 *      When serializing containers, cancel the Freeze Timer after setting IsSecure = true, IsLockeddown = true
 *  5/19/07, Adam
 *      Add support for lockable private houses
 *  5/8/07, Adam
 *      Updates to make use of data packing functions. e.g. Utility.GetUIntRight16()
 *  5/7/07, Adam
 *      Fix public houses so that lockboxes are accessable by anyone
 *  5/6/07, Adam
 *      - Add support for purchasable lockboxes
 *      - Move house Decay timer logic from Heartbeat
 *      - allow public houses to have lockboxes
 *  4/4/07, Adam
 *      Add CleanHouse() on IDOC to remove gold, resources, stackables, etc..
 *      Add an exception for necro regs.
 *  3/6/07, Adam
 *      Add public RemoveDoor() method so we can 'chop' doors to destroy them.
 *  01/07/07, Kit
 *      Reverted change!
 *      Added call to fakecontainers ManageLockDowns() in house deserialization, 
 *      for reseting of a items IslockedDown status.
 *  12/29/06, Kit
 *      Changed Decay state to virtual.
 *  12/18/06, Kit
 *      Added access rights to librarys
 *  12/11/06, Kit
 *      Added in support for Library archives
 *  11/23/06, Rhiannon
 *      Removed code that supposedly banned all characters on the account of a player whose character was banned from a house.
 *	10/16/06, Adam
 *		Add global override for SecurePremises
 *			i.e., CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises)
 *	9/19/06, Pix
 *		Added check to make sure the container isn't already secured or locked down when trying to secure a container.
 *	8/7/06, Adam
 *		Add a public method to allow us to enum the multis
 *	8/25/06, Adam
 *		Undo change of 8/14/06(weaver), and comment out the *other* debug code.
 *	8/23/06, Pix
 *		Added check for sign == null in CheckDecay()
 *	8/22/06, Pix
 *		Excluded announcements of IDOC tents.
 *	8/19/06, weaver
 *		Removed console debug message re: decayability of houses checked.
 *	8/14/06, weaver
 *		Modified lockdown check to treat containers as single items.
 *	8/14/06, Pix
 *		Added more safety-checking to CheckAllHouseDecay.  Now doesn't assume there's an owner or a sign.
 *	8/03/06, weaver
 *		Removed Contains() check from FindPlayerVendor() as it was pointless (region already verified)
 *		and messing up tents.
 *	7/12/06, weaver
 *		Virtualized Refresh() and RefreshHouseOneDay() to allow overridable 
 *		Added public accessor for town crier IDOC thing decay handling.
 *	7/6/06, Adam
 *		- Change default of m_SecurePremises to false from true;
 *			We will leave the flag for GM access in case of another exploit
 *		- Add a set SetSecurity() function that will change the state of m_SecurePremises on all houses
 *	5/02/06, weaver
 *		Added GetAccountHouses to retrieve all houses on passed mobile's account.
 *	3/21/06, weaver
 *		Added check for trash barrels to exclude them from lockbox count.
 *	2/21/06, Pix
 *		Added SecurePremises flag.
 *	2/20/06, Adam
 *		Add method FindPlayer() to find players in a house.
 *		We no longer allow a house to be deeded when players are inside (on roof) 
 *		This was used as an exploit.
 *	2/11/06, Adam
 *		Clear the Town Crier in OnDelete()
 *  1/1/06, Adam
 *		Swap HouseCheck for LastHouseCheckMinutes in the IDOC announce logic
 *		I also rewrote LastHouseCheck in Heartbeat.cs to do the right thing
 *	12/27/05, Adam
 *		Add IDOC logger
 *	12/15/05, Adam
 *		Reverse the change of 6/24/04, (Pix)
 *		This code no longer applies as you cannot lock down containers
 *		that are not 'deco' in a public building. Furthermore, you cannot lock down
 *		items inside of a container.
 *  12/11/05, Kit
 *		Added EternalEmbers to accessability list.
 *	11/25/05, erlein
 *		Added function + calls from targets to handle command logging.
 *  10/30/05 Taran Kain
 *		Fixed minor bug with securing containers near the lockdown limit
 *	09/3/05, Adam
 *		a. Reformat console output for never decaying houses
 *		b. add new GlobalNeverDecay() function - World crisis mode :\
 *			(controlled from Core Management Console)
 *  08/28/05, Adam
 *		minor tweak in IDOC announcement: 
 *		Change "at" to "near" for sextant coordinates
 *  08/27/05, Taran Kain
 *		Changed IDOC announcement to vaguely describe location in sextant coordinates.
 *  08/25/05, Taran Kain
 *		Added IDOC Announcement system.
 *	8/16/05, Pix
 *		Fixed house refresh problem.
 *	8/4/05, Pix
 *		Change to house decay.
 *	7/30/05, Pix
 *		Outputs message to console to log never-decay houses.
 *	7/29/05, Pix
 *		Now DecayState() returns "This structure is in perfect condition." if house is set to not decay.
 *	7/05/05, Pix
 *		Now NeverDecay maintains the stored time for a house instead of setting it at HouseDecayDelay
 *	6/11/05, Pix
 *		Added SetBanLocation because teh BanLocation property doesn't function like I need.
 *	06/04/05, Pix
 *		Upped Friends List Max to 150 (from 50).
 *	05/06/05, Kit
 *		Added support for SeedBoxs.
 *	04/20/05, Pix
 *		Increased the max timed storable to 3X (90 days)
 *	02/23/05, Pixie
 *		Made other characters on same account count as owners of the house.
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *	11/09/04, Pixie
 *		Made it so you can't lock down items inside containers (to curb exploit).
 *	10/23/04, Darva
 *			Checked if house is public before doing ChangeLocks on transfer.
 *	9/24/04, Adam
 *		Make checks against BaseContainer now that we've pushed the deco support all the way down
 *		(Decorative containers)
 *	9/21/04, Adam
 *		Create mechanics for Decorative containers that do not count against lockboxes
 *			1. Add IsExceptionContainer(): Add all exceptions to this function
 *			2. SetLockdown( ... ) now checks against IsExceptionContainer() when calculating lockboxes counts
 *			3. LockDown( ... ) now checks against IsExceptionContainer() when deciding whether to lockdown
 *			4. Add HouseDecoTarget : Target. This mechanism is invoked from one of the commands to in HouseRegion
 *			to make a container decorative or not.
 *			5. make sure the container is not locked down before changing it's state
 *		See Also: HouseRegion.cs and various containers
 *	9/19/04, Adam
 *		Revert the privious change due to a bug.
 *	9/19/04, mith
 *		 SetLockdown(): Added functioanlity for Decorative Containers
 *	9/16/04, Pix
 *		Changed so House Decay uses the Heartbeat system.
 *	7/14/04, mith
 *		Ban(): added check to see if target is BaseGuard (in with the AccessLevel checking)
 *	7/4/04, Adam
 *		CheckAccessibility
 *			Change: Keys are FriendAccess, KeyRings are CoOwnerAccess
 *			Change: Containers in a Public House are AnyoneAccess
 *		CheckAccessible
 *			Comment out the code that provides locked-down containers CoOwnerAccess.
 *			Now that lockboxes are accessible by 'anyone', this would seem inappropriate.
 *	6/24/04, Pix
 *		KeyRing change - now they don't count as lockboxes.
 *		Lockbox contents decay fix.
 *	6/14/04, Pix
 *		Changes for House decay
 *	6/12/04, mith
 *		Modified roles of friends/co-owners with respect locking down items.
 *		Clear Access, Bans, Friends, and CoOwners arrays OnAfterDelete().
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/30/04, mith
 *		Modified banning rules so that Aggressors/Criminals can not ban from their house.
 *		Set Public() property to always be true; no private housing allowed.
 */

using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis.Deeds;
using Server.Network;
using Server.Regions;
using Server.SMTP;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Multis
{
    public abstract class BaseHouse : BaseMulti
    {
        public static bool NewVendorSystem { get { return Core.AOS; } } // Is new player vendor system enabled?

        #region ImplFlags
        [Flags]
        public enum ImplFlags
        {
            None = 0x00000000,
            SuppressRegion = 0x00000001,
            ManagedDemolishion = 0x00000002,
        }
        private ImplFlags m_Flags;

        public void SetFlag(ImplFlags flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        public bool GetFlag(ImplFlags flag)
        {
            return ((m_Flags & flag) != 0);
        }
        #endregion ImplFlags

        #region GetDeed

        public static void Initialize()
        {
            Server.CommandSystem.Register("GetDeed", AccessLevel.Administrator, new CommandEventHandler(OnGetDeed));
        }

        [Usage("GetDeed")]
        [Description("Gets the deed for this house.")]
        private static void OnGetDeed(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Get a deed for which house?.");
            e.Mobile.SendMessage("Please target the house sign.");
            e.Mobile.Target = new GetDeedTarget();
        }

        public class GetDeedTarget : Target
        {
            public GetDeedTarget()
                : base(8, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign && (target as HouseSign).Structure != null)
                {
                    HouseSign sign = target as HouseSign;
                    /*
					if (sign.Owner is StaticHouse == false)
					{
						from.SendMessage("You may only wipe regions on a StaticHouse.");
						return;
					}*/

                    BaseHouse bh = sign.Structure as BaseHouse;
                    if (bh != null)
                    {
                        HouseDeed hd = bh.GetDeed();
                        if (hd == null)
                        {
                            from.SendMessage("There is no deed for this house.");
                            return;
                        }
                        from.AddToBackpack(hd);
                        from.SendMessage("The deed has been added to your backpack.");
                        return;
                    }
                    from.SendMessage("Error getting house deed.");
                }
                else
                {
                    from.SendMessage("That is not a house sign.");
                }
            }
        }
        #endregion GetDeed

        public bool ManagedDemolishion
        {
            get
            {
                return GetFlag(ImplFlags.ManagedDemolishion);
            }
            set
            {
                SetFlag(ImplFlags.ManagedDemolishion, value);
            }
        }

        public bool SuppressRegion
        {
            get
            {
                return GetFlag(ImplFlags.SuppressRegion);
            }
            set
            {
                SetFlag(ImplFlags.SuppressRegion, value);
                UpdateRegionArea();
            }
        }

        private uint m_AccountCode;
        public uint AccountCode
        {
            get { return m_AccountCode; }
        }

        #region Decay Stuff
        private static bool HouseCollapsingEnabled = true;
        private static TimeSpan HouseDecayDelay = TimeSpan.FromDays(15.0);
        private static TimeSpan MaxHouseDecayTime = TimeSpan.FromDays(30.0 * 3); //virtual time bank max
        private static DateTime m_HouseDecayLast = DateTime.MinValue;   // not serialized
        private int m_LockboxDecayAccumulator = 0;                      // not serialized
        private TimeSpan m_RestartDecay = TimeSpan.Zero;                // serialized
        private DateTime m_RestartDecayDelta = DateTime.MinValue;       // mot serialized

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan RestartDecay
        {
            get
            {
                return m_RestartDecay;
            }
            set
            {
                m_RestartDecay = value;
            }
        }

        // town crier entry for telling the world our house is idoc
        private TownCrierEntry m_IDOC_Broadcast_TCE;

        // wea: added public accessor
        public TownCrierEntry IDOC_Broadcast_TCE
        {
            get
            {
                return m_IDOC_Broadcast_TCE;
            }

            set
            {
                m_IDOC_Broadcast_TCE = value;
            }
        }

        public bool m_NeverDecay;
        public DateTime StructureDecayTime
        {
            get
            {
                return DateTime.UtcNow + TimeSpan.FromMinutes(m_DecayMinutesStored);
            }
        }

        private double m_DecayMinutesStored;

        [CommandProperty(AccessLevel.GameMaster)]
        public double DecayMinutesStored
        {
            get
            {
                return m_DecayMinutesStored;
            }
            set
            {
                m_DecayMinutesStored = value;

                if (Core.UOAI || Core.UOAR)
                {
                    if (m_DecayMinutesStored > ONE_DAY_IN_MINUTES && m_IDOC_Broadcast_TCE != null)
                    {
                        GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                        m_IDOC_Broadcast_TCE = null;
                    }
                }
            }
        }

        public void RefreshNonDecayingHouse()
        {
            if (StructureDecayTime < DateTime.UtcNow)
            {
                Refresh();
            }
        }

        public virtual void Refresh()
        {
            if (Core.UOAI || Core.UOAR)
            {
                m_DecayMinutesStored = HouseDecayDelay.TotalMinutes;

                if (m_DecayMinutesStored > ONE_DAY_IN_MINUTES && m_IDOC_Broadcast_TCE != null)
                {
                    GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                    m_IDOC_Broadcast_TCE = null;
                }
            }
            else
            {
                m_DecayMinutesStored = HouseDecayDelay.TotalMinutes;
            }
        }

        private bool GlobalNeverDecay()
        {
            return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay);
        }

        public bool TownshipRestrictedRefresh
        {
            get
            {
                bool bReturn = false;
                try
                {
                    CustomRegion cr = TownshipRegion.FindDRDTRegion(this.Map, this.Location);
                    if (cr != null)
                    {
                        if (cr is TownshipRegion)
                        {
                            TownshipStone tstone = ((TownshipRegion)cr).TStone;
                            if (tstone != null)
                            {
                                if (this.Owner != null)
                                {
                                    if (tstone.Guild != null && tstone.Guild != this.Owner.Guild)
                                    {
                                        if (this.LastTraded > tstone.BuiltOn || this.BuiltOn > tstone.BuiltOn)
                                        {
                                            //if we're last traded after the township was placed,
                                            // and we're not of the township's guild
                                            // then we can't refresh the house.
                                            bReturn = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception tse)
                {
                    LogHelper.LogException(tse, "Pixie: In BaseHouse.RefreshOneDay() doing township exclusion");
                }


                return bReturn;
            }
        }

        private void CheckLockboxDecay()
        {
            m_LockboxDecayAccumulator += LastHouseCheckMinutes;
            if (m_LockboxDecayAccumulator > 60)
            {   // an hour has passed, consume a tax credit.
                m_LockboxDecayAccumulator = 0;
                // consume one credit per extra lockbox
                if (StorageTaxCredits > 0 && m_LockBoxCount > LockBoxFloor)
                {   // we loop to insure we never go below 0
                    for (int ix = 0; ix < m_LockBoxCount - LockBoxFloor; ix++)
                        if (StorageTaxCredits > 0)
                            StorageTaxCredits--;
                }
                else
                {   // need to decay a lockbox!
                    LogHelper Logger = null;
                    try
                    {
                        Container ToRelease = null;
                        // if we have *extra* storage, release one
                        if (m_LockDowns != null && m_LockBoxCount > LockBoxFloor)
                            foreach (Item ix in m_LockDowns)
                                if (ix is Container && IsLockedDown(ix) && IsExceptionContainer(ix) == false)
                                {
                                    ToRelease = ix as Container;
                                    break;
                                }

                        if (ToRelease != null)
                        {   // log it
                            Logger = new LogHelper("LockboxCleanup.log", false);
                            Logger.Log(LogType.Item, ToRelease, "Releasing container.");
                            // release it
                            SetLockdown(ToRelease, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }
                    finally
                    {
                        if (Logger != null)
                            Logger.Finish();
                    }

                }
            }
        }

        // See if we are to auto-resume a frozen decay
        public void CheckAutoResumeDecay()
        {
            if (m_RestartDecay > TimeSpan.Zero)             // is the system enabled?
            {
                if (m_RestartDecayDelta != DateTime.MinValue)
                {
                    TimeSpan temp = DateTime.UtcNow - m_RestartDecayDelta;
                    if (temp >= m_RestartDecay)
                    {
                        m_RestartDecay = TimeSpan.Zero;     // disable system
                        m_NeverDecay = false;               // resume decay
                    }
                    else
                        m_RestartDecay -= temp;             // we're this much closer
                }
                m_RestartDecayDelta = DateTime.UtcNow;
            }
        }

        //returns true if house decays
        public bool CheckDecay()
        {
            // first, see if we are to auto-resume a frozen decay
            CheckAutoResumeDecay();

            //if house is set to never decay, refresh it!
            // adam: also, don't consume tax credits if decay if frozen
            if (m_NeverDecay || GlobalNeverDecay())
            {
                RefreshNonDecayingHouse();
                return false;
            }

            // decay any lockboxes if needed
            CheckLockboxDecay();

            // if the owner annexed a tent to place this house, then Demolishion Managed, i.e., they cannot transfer or demolish
            //	before 7 days. Clear the ManagedDemolishion flag if 7 days have passed.
            if (GetFlag(BaseHouse.ImplFlags.ManagedDemolishion) == true)
            {
                DateTime ok_demo = BuiltOn + TimeSpan.FromDays(7.0);
                if (DateTime.UtcNow > ok_demo)
                    SetFlag(BaseHouse.ImplFlags.ManagedDemolishion, false);
            }

            // calc time to IDOC
            double oldminutes = m_DecayMinutesStored;
            m_DecayMinutesStored -= LastHouseCheckMinutes;

            // check if we should email an idoc warning (2 days before IDOC)
            if (oldminutes > ONE_DAY_IN_MINUTES * 2 && m_DecayMinutesStored <= ONE_DAY_IN_MINUTES * 2)
                EmailWarning();

            // check if we should broadcast idoc
            if (oldminutes > ONE_DAY_IN_MINUTES && m_DecayMinutesStored <= ONE_DAY_IN_MINUTES)
                LogDecay();

            if (m_DecayMinutesStored < 0)
            {
                if (HouseCollapsingEnabled)
                {   // remove checks, gold and other economy ruining items during idoc
                    CleanHouse();
                    Delete();
                }
                return true;
            }
            return false;
        }

        private void EmailWarning()
        {
            try
            {   // only on the production shard
                if (TestCenter.Enabled == false)
                    if (Owner != null && Owner.Account != null && Owner.Account as Accounting.Account != null)
                    {
                        Accounting.Account a = Owner.Account as Accounting.Account;
                        if (a.EmailAddress != null && a.EmailAddress.Length > 0 && SmtpDirect.CheckEmailAddy(a.EmailAddress, false) == true)
                        {
                            string subject = "Angel Island: Your house is in danger of collapsing";
                            string body = String.Format("\nThis message is to inform you that your house at {2} on the '{0}' account is in danger of collapsing (IDOC). If you do not return to refresh your house, it will fall on {1}.\n\nBest Regards,\n  The Angel Island Team\n\n", a.ToString(), DateTime.UtcNow + TimeSpan.FromMinutes(m_DecayMinutesStored), BanLocation);
                            Emailer mail = new Emailer();
                            mail.SendEmail(a.EmailAddress, subject, body, false);
                        }
                    }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private void LogDecay()
        {
            LogHelper Logger = new LogHelper("houseDecay.log", false);
            bool announced = false;
            if (this is Tent || this is SiegeTent)
            {
                //never announce tents
            }
            else if (Utility.RandomDouble() < CoreAI.IDOCBroadcastChance && (Core.UOAI || Core.UOAR))
            {
                string[] lines = new string[1];
                lines[0] = String.Format("Lord British has condemned the estate of {0} near {1}.", this.Owner.Name, DescribeLocation());
                m_IDOC_Broadcast_TCE = new TownCrierEntry(lines, TimeSpan.FromMinutes(m_DecayMinutesStored), Serial.MinusOne);
                GlobalTownCrierEntryList.Instance.AddEntry(m_IDOC_Broadcast_TCE);
                announced = true;
            }

            try
            {
                // log it
                string temp = string.Format(
                    "Owner:{0}, Account:{1}, Name:{2}, Serial:{3}, Location:{4}, BuiltOn:{5}, StructureDecayTime:{6}, Type:{7}, Announced:{8}",
                    this.m_Owner,
                    this.m_Owner.Account,
                    ((this.m_Sign != null) ? this.m_Sign.Name : "NO SIGN"),
                    this.Serial,
                    this.Location,
                    this.BuiltOn,
                    this.StructureDecayTime,
                    this.GetType(),
                    announced.ToString()
                    );
                Logger.Log(LogType.Text, temp);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Logger.Finish();
            }
        }

        /*
		public override void Delete()
		{
			// now delete player placed Sensory Consoles
			Item[] scs = FindAllItems(typeof(SensoryConsole));
			foreach (SensoryConsole sc in scs)
			{	// if player owned, delete it
				if (sc.Owner != null && sc.Owner.AccessLevel == AccessLevel.Player)
				{
					sc.Delete();
				}
			}

			base.Delete();
		}*/

        private void CleanHouse()
        {
            LogHelper Logger = new LogHelper("IDOCCleanup.log", false);
            try
            {
                ArrayList list = new ArrayList();

                // okay, process lockdowns and lockboxes
                if (m_LockDowns != null)
                    foreach (Item ix in m_LockDowns)
                    {
                        if (ix is Container == true)
                        {
                            ArrayList contents = (ix as Container).FindAllItems();
                            foreach (Item jx in contents)
                                if (IsIDOCManagedItem(jx))
                                    list.Add(jx);
                        }
                        else if (IsIDOCManagedItem(ix))
                            list.Add(ix);
                    }

                // now for secures
                if (m_Secures != null)
                    foreach (SecureInfo info in m_Secures)
                    {
                        if (info.Item is Container == true)
                        {
                            ArrayList contents = (info.Item as Container).FindAllItems();
                            foreach (Item jx in contents)
                                if (IsIDOCManagedItem(jx))
                                    list.Add(jx);
                        }
                        else if (IsIDOCManagedItem(info.Item))
                            list.Add(info.Item);
                    }

                // okay, now delete all IDOC managed items
                foreach (Item mx in list)
                {
                    if (mx == null || mx.Deleted == true)
                        continue;

                    // log it
                    LogIDOCManagedItem(Logger, mx);

                    // release it
                    if (IsLockedDown(mx))
                        SetLockdown(mx, false);
                    else if (IsSecure(mx))
                        ReleaseSecure(mx);

                    // delete it
                    mx.Delete();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Logger.Finish();
            }
        }

        // stuff you want to delete
        private bool IsIDOCManagedItem(Item ix)
        {
            if (ix == null || ix.Deleted == true)
                return false;

            // exceptions to the delete rules below
            if (IsIDOCManagedItemException(ix) == true)
                return false;

            if (ix.Stackable == true)
                return true;

            if (ix is CommodityDeed)
                return true;

            if (ix is BankCheck)
                return true;

            return false;
        }

        private void LogIDOCManagedItem(LogHelper Logger, Item ix)
        {
            // log all the items deleted at IDOC
            if (Logger == null)
                return;

            if (ix == null || ix.Deleted == true)
                return;

            if (ix.Stackable == true)
                Logger.Log(LogType.Item, ix, String.Format("Amount = {0}", ix.Amount));

            if (ix is CommodityDeed)
                Logger.Log(LogType.Item, ix, String.Format("Commodity = {0}, Amount = {1}", (ix as CommodityDeed).Commodity, (ix as CommodityDeed).CommodityAmount));

            if (ix is BankCheck)
                Logger.Log(LogType.Item, ix, String.Format("Amount = {0}", (ix as BankCheck).Worth));

            return;
        }

        private bool IsIDOCManagedItemException(Item ix)
        {
            if (ix == null || ix.Deleted == true)
                return false;

            // necro regs are an exception
            if (ix is BatWing || ix is GraveDust || ix is DaemonBlood || ix is NoxCrystal || ix is PigIron)
                return true;

            return false;
        }

        //This is called by the object that keeps track 
        //of steps.
        public const double ONE_DAY_IN_MINUTES = 60.0 * 24.0;
        public virtual void RefreshHouseOneDay()
        {
            if (TownshipRestrictedRefresh)
            {
                return;
            }

            if (m_DecayMinutesStored <= MaxHouseDecayTime.TotalMinutes)
            {
                if (m_DecayMinutesStored >= (MaxHouseDecayTime.TotalMinutes - ONE_DAY_IN_MINUTES))
                {
                    m_DecayMinutesStored = MaxHouseDecayTime.TotalMinutes;
                }
                else
                {
                    m_DecayMinutesStored += ONE_DAY_IN_MINUTES;
                }
            }

            if (m_DecayMinutesStored > ONE_DAY_IN_MINUTES && m_IDOC_Broadcast_TCE != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                m_IDOC_Broadcast_TCE = null;
            }

        }

        public virtual string DecayState()
        {
            TimeSpan decay = TimeSpan.FromMinutes(m_DecayMinutesStored);

            if (decay == BaseHouse.MaxHouseDecayTime || this.m_NeverDecay == true || GlobalNeverDecay() == true)
            {
                return "This structure is in perfect condition.";
            }
            else if (decay <= TimeSpan.FromDays(1.0)) //1 day
            {
                return "This structure is in danger of collapsing.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(12.0)) //3days
            {
                return "This structure is greatly worn.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(9.0)) //6 days
            {
                return "This structure is fairly worn.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(5.0)) //10 days
            {
                return "This structure is somewhat worn.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(1.0)) //14 days
            {
                return "This structure is slightly worn.";
            }
            else if (decay > HouseDecayDelay - TimeSpan.FromDays(1.0) &&
                decay < BaseHouse.MaxHouseDecayTime)
            {
                return "This structure is like new.";
            }
            else
            {
                return "This structure has problems.";
            }
        }

        // needed for decay check
        public static int LastHouseCheckMinutes
        {
            get
            {
                // see if it's been initialized yet
                if (m_HouseDecayLast == DateTime.MinValue)
                    return 0;

                // return the delta in minutes since last check
                TimeSpan sx = DateTime.UtcNow - m_HouseDecayLast;
                return (int)sx.TotalMinutes;
            }
        }

        public static int CheckAllHouseDecay()
        {
            int numberchecked = 0;
            try
            {
                foreach (ArrayList list in BaseHouse.m_Table.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        BaseHouse house = list[i] as BaseHouse;
                        if (house != null)
                        {
                            if (house.m_NeverDecay)
                            {
                                Point3D loc = house.Location;
                                if (house.Sign != null) loc = house.Sign.Location;
                                Mobile owner = house.Owner;

                                if (owner != null)
                                {   // don't list staff owned houses
                                    if (owner.AccessLevel == AccessLevel.Player)
                                        Console.WriteLine("House: (Never Decays) owner: " + owner + " location: " + loc);
                                }
                                else
                                {
                                    Console.WriteLine("House: (Never Decays) owner: NULL location: " + loc);
                                }
                            }

                            house.CheckDecay();
                            numberchecked++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Error in CheckAllHouseDecay(): " + e.Message);
                Console.WriteLine(e.StackTrace.ToString());
            }

            // Adam: record the 'last' check.
            m_HouseDecayLast = DateTime.UtcNow;

            return numberchecked;
        }

        #endregion //Decay stuff

        public static int SetSecurity(bool state)
        {
            int numberchecked = 0;
            try
            {
                foreach (ArrayList list in BaseHouse.m_Table.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        BaseHouse house = list[i] as BaseHouse;
                        if (house != null)
                        {
                            house.SecurePremises = state;
                        }
                        numberchecked++;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Error in BaseHouse.SetSecurity(): " + e.Message);
                Console.WriteLine(e.StackTrace.ToString());
            }
            return numberchecked;
        }

        public string DescribeLocation()
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            Point3D p = new Point3D(this.Location);
            p.X = (p.X + Utility.RandomMinMax(-70, 70));
            p.Y = (p.Y + Utility.RandomMinMax(-70, 70));

            bool valid = Sextant.Format(p, this.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = String.Format("{0}� {1}'{2}, {3}� {4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W");
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", p.X, p.Y);

            if (this.Map != null)
            {
                if (this.Region != this.Map.DefaultRegion && this.Region.ToString() != "")
                {
                    location += (" in " + this.Region);
                }
            }

            return location;
        }

        public const int MaxCoOwners = 15;
        public const int MaxFriends = 150;
        public const int MaxBans = 50;

        private bool m_Public;

        private bool m_SecurePremises = false;
        public bool SecurePremises
        {
            get
            {
                return m_SecurePremises || CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises);
            }
            set { m_SecurePremises = value; }
        }

        private HouseRegion m_Region;
        private HouseSign m_Sign;
        private TrashBarrel m_Trash;
        private ArrayList m_Doors;

        private Mobile m_Owner;

        private ArrayList m_Access;
        private ArrayList m_Bans;
        private ArrayList m_CoOwners;
        private ArrayList m_Friends;

        private ArrayList m_LockDowns;
        private ArrayList m_Secures;

        private ArrayList m_Addons;

        private int m_MaxLockDowns;
        private int m_MaxSecures;
        private int m_LockBoxCount;
        private int m_MaxLockBoxes;
        private int m_Price;
        private uint m_LockBoxData;

        private int m_Visits;

        private uint m_UpgradeCosts;

        private DateTime m_BuiltOn, m_LastTraded;

        private static Hashtable m_Table = new Hashtable();

        public virtual bool IsAosRules { get { return Core.AOS; } }

        public bool CanAddLockbox
        {
            get
            {
                return LockBoxCeling > m_MaxLockBoxes;
            }
        }

        public bool CanAddStorageCredits(ushort amount)
        {   // no more than 64K max credits
            return (ushort)StorageTaxCredits + amount < (ushort)64000;
        }

        public uint StorageTaxCredits
        {
            get { return Utility.GetUIntRight16(m_LockBoxData); }
            set { Utility.SetUIntRight16(ref m_LockBoxData, value); }
        }

        public uint LockBoxCeling
        {
            get { return Utility.GetUIntByte3(m_LockBoxData); }
            set { Utility.SetUIntByte3(ref m_LockBoxData, value); }
        }

        public uint LockBoxFloor
        {
            get { return Utility.GetUIntByte4(m_LockBoxData); }
            set { Utility.SetUIntByte4(ref m_LockBoxData, value); }
        }

        public uint UpgradeCosts
        {   // refundable upgrade costs when redeeding house
            get { return m_UpgradeCosts; }
            set { m_UpgradeCosts = value; }
        }

        public static Hashtable Multis
        {
            get
            {
                return m_Table;
            }
        }

        public virtual HousePlacementEntry GetAosEntry()
        {
            return HousePlacementEntry.Find(this);
        }

        public virtual int GetAosMaxSecures()
        {
            HousePlacementEntry hpe = GetAosEntry();

            if (hpe == null)
                return 0;

            return hpe.Storage;
        }

        public virtual int GetAosMaxLockdowns()
        {
            HousePlacementEntry hpe = GetAosEntry();

            if (hpe == null)
                return 0;

            return hpe.Lockdowns;
        }

        public virtual int GetAosCurSecures(out int fromSecures, out int fromVendors, out int fromLockdowns)
        {
            fromSecures = 0;
            fromVendors = 0;
            fromLockdowns = 0;

            ArrayList list = m_Secures;

            for (int i = 0; list != null && i < list.Count; ++i)
            {
                SecureInfo si = (SecureInfo)list[i];

                fromSecures += si.Item.TotalItems;
            }

            if (m_LockDowns != null)
                fromLockdowns += m_LockDowns.Count;

            foreach (Mobile mx in m_Region.Mobiles.Values)
                if (mx is PlayerVendor)
                    if (mx.Backpack != null)
                        fromVendors += mx.Backpack.TotalItems;

            return fromSecures + fromVendors + fromLockdowns;
        }

        public virtual bool CanPlaceNewVendor()
        {
            if (!IsAosRules)
                return true;

            return CheckAosLockdowns(10);
        }

        #region BARKEEP SYSTEM
        /* NPCData, currently we only use byte 4 for the max barkeep count
		 * bytes 1,2&3 are currently unused and available
		 */
        private uint m_NPCData;
        public uint MaximumBarkeepCount
        {
            get { return Utility.GetUIntByte4(m_NPCData); }
            set { Utility.SetUIntByte4(ref m_NPCData, value); }
        }

        public virtual bool CanPlaceNewBarkeep()
        {
            int avail = (int)MaximumBarkeepCount;

            foreach (Mobile mx in m_Region.Mobiles.Values)
            {
                if (avail <= 0)
                    break;

                if (mx is PlayerBarkeeper)
                    --avail;
            }

            return (avail > 0);
        }
        #endregion BARKEEP SYSTEM

        public virtual bool CheckAosLockdowns(int need)
        {
            return ((GetAosCurLockdowns() + need) <= GetAosMaxLockdowns());
        }

        public virtual bool CheckAosStorage(int need)
        {
            int fromSecures, fromVendors, fromLockdowns;

            return ((GetAosCurSecures(out fromSecures, out fromVendors, out fromLockdowns) + need) <= GetAosMaxSecures());
        }

        public static void Configure()
        {
            Item.LockedDownFlag = 1;
            Item.SecureFlag = 2;
        }

        public virtual int GetAosCurLockdowns()
        {
            int v = 0;

            if (m_LockDowns != null)
                v += m_LockDowns.Count;

            if (m_Secures != null)
                v += m_Secures.Count;

            foreach (Mobile mx in m_Region.Mobiles.Values)
                if (mx is PlayerVendor)
                    v += 10;

            return v;
        }

        public static bool CheckLockedDown(Item item)
        {
            BaseHouse house = FindHouseAt(item);

            return (house != null && house.IsLockedDown(item));
        }

        public static bool CheckSecured(Item item)
        {
            BaseHouse house = FindHouseAt(item);

            return (house != null && house.IsSecure(item));
        }

        public static bool CheckLockedDownOrSecured(Item item)
        {
            BaseHouse house = FindHouseAt(item);

            return (house != null && (house.IsSecure(item) || house.IsLockedDown(item)));
        }

        public static ArrayList GetHouses(Mobile m)
        {
            ArrayList list = new ArrayList();

            if (m != null)
            {
                ArrayList exists = (ArrayList)m_Table[m];

                if (exists != null)
                {
                    for (int i = 0; i < exists.Count; ++i)
                    {
                        BaseHouse house = exists[i] as BaseHouse;

                        if (house != null && !house.Deleted && house.Owner == m)
                            list.Add(house);
                    }
                }
            }

            return list;
        }

        // wea: added to retrieve all houses on mobile's account
        public static ArrayList GetAccountHouses(Mobile m)
        {
            ArrayList list = new ArrayList();

            Account a = m.Account as Account;

            if (a == null)
                return list;

            // loop characters
            for (int i = 0; i < 5; ++i)
            {
                if (a[i] != null)
                {
                    // loop houses
                    ArrayList exists = (ArrayList)m_Table[a[i]];
                    if (exists != null)
                        foreach (object o in exists)
                            list.Add(o);                    // add any found to master list
                }
            }

            return list;
        }

        public static bool CheckHold(Mobile m, Container cont, Item item, bool message, bool checkItems)
        {
            BaseHouse house = FindHouseAt(cont);

            if (house == null || !house.IsAosRules)
                return true;

            object root = cont.RootParent;

            if (root == null)
                root = cont;

            if (root is Item && house.IsSecure((Item)root) && !house.CheckAosStorage(1 + item.TotalItems))
            {
                if (message)
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.

                return false;
            }

            return true;
        }

        public static bool CheckAccessible(Mobile m, Item item)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true; // Staff can access anything

            BaseHouse house = FindHouseAt(item);

            if (house == null)
                return true;

            SecureAccessResult res = house.CheckSecureAccess(m, item);

            switch (res)
            {
                case SecureAccessResult.Insecure: break;
                case SecureAccessResult.Accessible: return true;
                case SecureAccessResult.Inaccessible: return false;
            }

            // adam: I don't believe we want this check.
            //	We want lockboxes to be free access to any and all players, 
            //	as this is how we achieve lootability. (which is the point.)
            //if ( house.IsLockedDown( item ) )
            //return house.IsCoOwner( m ) && (item is Container);

            return true;
        }

        public static BaseHouse FindHouseAt(Mobile m)
        {
            if (m == null || m.Deleted)
                return null;

            return FindHouseAt(m.Location, m.Map, 16);
        }

        public static BaseHouse FindHouseAt(Item item)
        {
            if (item == null || item.Deleted)
                return null;

            return FindHouseAt(item.GetWorldLocation(), item.Map, item.ItemData.Height);
        }

        public static BaseHouse FindHouseAt(Point3D loc, Map map, int height)
        {

            if (map == null || map == Map.Internal)
                return null;

            Sector sector = map.GetSector(loc);

            // Adam: the width and height of MultiComponentList Components for tents are all 0, so we can't use the IsInside() check
            //  when checking to see of the point (item or mobile) inside or outside the tent
            foreach (BaseMulti mult in sector.Multis.Values)
            {
                BaseHouse house = mult as BaseHouse;
                if (house != null)
                {
                    if ((house is Tent || house is SiegeTent) && house.Region != null)
                    {
                        if (house.Region.Contains(loc))
                            return house;
                    }
                    else if (house.IsInside(loc, height))
                    {
                        return house;
                    }
                }
            }

            return null;
        }

        public bool IsInside(Mobile m)
        {
            if (m == null || m.Deleted || m.Map != this.Map)
                return false;

            return IsInside(m.Location, 16);
        }

        public bool IsInside(Item item)
        {
            if (item == null || item.Deleted || item.Map != this.Map)
                return false;

            return IsInside(item.Location, item.ItemData.Height);
        }

        public bool CheckAccessibility(Item item, Mobile from)
        {
            SecureAccessResult res = CheckSecureAccess(from, item);

            switch (res)
            {
                case SecureAccessResult.Insecure: break;
                case SecureAccessResult.Accessible: return true;
                case SecureAccessResult.Inaccessible: return false;
            }

            if (!IsLockedDown(item))
                return true;
            else if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;
            else if (item is Runebook)
                return true;
            else if (item is SeedBox)
                return true;
            else if (item is Library)
                return true;
            else if (item is ISecurable)
                return HasSecureAccess(from, ((ISecurable)item).Level);

            else if (item is Key)
            {   // Adam: for complementary access, see KeyRing
                // from.Say("item is Key (IsFriend)");
                return IsFriend(from);
            }
            else if (item is KeyRing)
            {   // Adam: for complementary access, see Key
                //from.Say("item is Keyring (IsCoOwner)");
                return IsCoOwner(from);
            }
            else if ((item is Container) /*&& (m_Public == false)*/)
            {
                // Adam: lockboxes in ANY houses are accessable by anyone!
                //from.Say("item is Container (IsAnyOne)");
                return true;
            }

            else if (item is BaseLight)
                return IsFriend(from);
            else if (item is PotionKeg)
                return IsFriend(from);
            else if (item is BaseBoard)
                return true;
            else if (item is Dices)
                return true;
            else if (item is RecallRune)
                return true;
            else if (item is TreasureMap)
                return true;
            else if (item is Clock)
                return true;
            else if (item is BaseBook)
                return true;
            else if (item is BaseInstrument)
                return true;
            else if (item is Dyes || item is DyeTub)
                return true;
            else if (item is EternalEmbers)
                return true;

            return false;
        }

        public virtual bool IsInside(Point3D p, int height)
        {
            if (Deleted)
                return false;

            // Adam: tents are special - cannot account for height though. Is this ever a problem?
            if ((this is Tent || this is SiegeTent) && this.Region != null)
            {
                if (this.Region.Contains(p))
                    return true;
            }

            MultiComponentList mcl = Components;

            int x = p.X - (X + mcl.Min.X);
            int y = p.Y - (Y + mcl.Min.Y);

            if (x < 0 || x >= mcl.Width || y < 0 || y >= mcl.Height)
                return false;

            /* //pla: don't want this anymore
		   if ( this is HouseFoundation && y < (mcl.Height-1) )
			 return true;
		   */

            Tile[] tiles = mcl.Tiles[x][y];

            for (int j = 0; j < tiles.Length; ++j)
            {
                Tile tile = tiles[j];
                int id = tile.ID & 0x3FFF;
                ItemData data = TileData.ItemTable[id];

                // Slanted roofs do not count; they overhang blocking south and east sides of the multi
                if ((data.Flags & TileFlag.Roof) != 0)
                    continue;

                // Signs and signposts are not considered part of the multi
                if ((id >= 0xB95 && id <= 0xC0E) || (id >= 0xC43 && id <= 0xC44))
                    continue;

                int tileZ = tile.Z + this.Z;

                if (p.Z == tileZ || (p.Z + height) > tileZ)
                    return true;
            }

            return false;
        }

        public SecureAccessResult CheckSecureAccess(Mobile m, Item item)
        {
            if (m_Secures == null || !(item is Container))
                return SecureAccessResult.Insecure;

            for (int i = 0; i < m_Secures.Count; ++i)
            {
                SecureInfo info = (SecureInfo)m_Secures[i];

                if (info.Item == item)
                    return HasSecureAccess(m, info.Level) ? SecureAccessResult.Accessible : SecureAccessResult.Inaccessible;
            }

            return SecureAccessResult.Insecure;
        }

        public BaseHouse(int multiID, Mobile owner, int MaxLockDown, int MaxSecure, int MaxLockBox)
            : base(multiID | 0x4000)
        {
            //initialize decay
            m_DecayMinutesStored = HouseDecayDelay.TotalMinutes;

            m_BuiltOn = DateTime.UtcNow;
            m_LastTraded = DateTime.MinValue;

            m_Doors = new ArrayList();
            m_LockDowns = new ArrayList();
            m_Secures = new ArrayList();
            m_Addons = new ArrayList();

            m_CoOwners = new ArrayList();
            m_Friends = new ArrayList();
            m_Bans = new ArrayList();
            m_Access = new ArrayList();

            m_Region = new HouseRegion(this);

            m_Owner = owner;
            m_AccountCode = CalcAccountCode(owner);

            m_MaxLockDowns = MaxLockDown;
            m_MaxSecures = MaxSecure;
            m_MaxLockBoxes = MaxLockBox;            // current limit, can increase
            LockBoxCeling = (uint)MaxLockBox * 2;   // high limit
            LockBoxFloor = (uint)MaxLockBox;        // low limit
            m_LockBoxCount = 0;                     // no lock boses yet
            m_UpgradeCosts = 0;                     // no refundable upgrades yet
            MaximumBarkeepCount = 2;                // default for new houses

            UpdateRegionArea();

            if (owner != null)
            {
                ArrayList list = (ArrayList)m_Table[owner];

                if (list == null)
                    m_Table[owner] = list = new ArrayList();

                list.Add(this);
            }

            Movable = false;

            //Decay Stuff:
            m_NeverDecay = false;
            Refresh();
        }

        public BaseHouse(Serial serial)
            : base(serial)
        {
        }

        public override void OnMapChange()
        {
            // why is this here?
            //if (m_LockDowns == null)
            //    return;

            m_Region.Map = this.Map;

            if (m_Sign != null && !m_Sign.Deleted)
                m_Sign.Map = this.Map;

            if (m_Trash != null && !m_Trash.Deleted)
                m_Trash.Map = this.Map;

            if (m_Doors != null)
            {
                foreach (Item item in m_Doors)
                    item.Map = this.Map;
            }

            if (m_LockDowns != null)
            {
                foreach (Item item in m_LockDowns)
                    item.Map = this.Map;
            }

            if (m_Addons != null)
            {
                foreach (Item item in m_Addons)
                    item.Map = this.Map;
            }
        }

        public virtual void ChangeSignType(int itemID)
        {
            if (m_Sign != null)
                m_Sign.ItemID = itemID;
        }

        private static Rectangle2D[] m_AreaArray = new Rectangle2D[0];
        public virtual Rectangle2D[] Area { get { return m_AreaArray; } }

        public virtual void UpdateRegionArea()
        {
            Rectangle2D[] area = this.Area;
            ArrayList coords = new ArrayList(area.Length);

            for (int i = 0; i < area.Length; ++i)
                coords.Add(new Rectangle2D(X + area[i].Start.X, Y + area[i].Start.Y, area[i].Width, area[i].Height));

            m_Region.Coords = coords;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            // why was this here?
            //if (m_LockDowns == null)
            //    return;

            int dx = base.Location.X - oldLocation.X;
            int dy = base.Location.Y - oldLocation.Y;
            int dz = base.Location.Z - oldLocation.Z;

            if (m_Sign != null && !m_Sign.Deleted)
                m_Sign.Location = new Point3D(m_Sign.X + dx, m_Sign.Y + dy, m_Sign.Z + dz);

            if (m_Trash != null && !m_Trash.Deleted)
                m_Trash.Location = new Point3D(m_Trash.X + dx, m_Trash.Y + dy, m_Trash.Z + dz);

            UpdateRegionArea();

            m_Region.GoLocation = new Point3D(m_Region.GoLocation.X + dx, m_Region.GoLocation.Y + dy, m_Region.GoLocation.Z + dz);

            if (m_Doors != null)
            {
                foreach (Item item in m_Doors)
                {
                    if (!item.Deleted)
                        item.Location = new Point3D(item.X + dx, item.Y + dy, item.Z + dz);
                }
            }

            if (m_LockDowns != null)
            {
                foreach (Item item in m_LockDowns)
                {
                    if (!item.Deleted)
                        item.Location = new Point3D(item.X + dx, item.Y + dy, item.Z + dz);
                }
            }

            if (m_Addons != null)
            {
                foreach (Item item in m_Addons)
                {
                    if (!item.Deleted)
                        item.Location = new Point3D(item.X + dx, item.Y + dy, item.Z + dz);
                }
            }
        }

        public BaseDoor AddEastDoor(int x, int y, int z)
        {
            return AddEastDoor(true, x, y, z);
        }

        public BaseDoor AddEastDoor(bool wood, int x, int y, int z)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.SouthCW);

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddSouthDoor(int x, int y, int z)
        {
            return AddSouthDoor(true, x, y, z);
        }

        public BaseDoor AddSouthDoor(bool wood, int x, int y, int z)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.WestCW);

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddEastDoor(int x, int y, int z, uint k)
        {
            return AddEastDoor(true, x, y, z, k);
        }

        public BaseDoor AddEastDoor(bool wood, int x, int y, int z, uint k)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.SouthCW);

            door.Locked = true;
            door.KeyValue = k;

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddSouthDoor(int x, int y, int z, uint k)
        {
            return AddSouthDoor(true, x, y, z, k);
        }

        public BaseDoor AddSouthDoor(bool wood, int x, int y, int z, uint k)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.WestCW);

            door.Locked = true;
            door.KeyValue = k;

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor[] AddSouthDoors(int x, int y, int z, uint k)
        {
            return AddSouthDoors(true, x, y, z, k);
        }

        public BaseDoor[] AddSouthDoors(bool wood, int x, int y, int z, uint k)
        {
            BaseDoor westDoor = MakeDoor(wood, DoorFacing.WestCW);
            BaseDoor eastDoor = MakeDoor(wood, DoorFacing.EastCCW);

            westDoor.Locked = true;
            eastDoor.Locked = true;

            westDoor.KeyValue = k;
            eastDoor.KeyValue = k;

            // westDoor.Link = eastDoor;
            // eastDoor.Link = westDoor;

            AddDoor(westDoor, x, y, z);
            AddDoor(eastDoor, x + 1, y, z);

            return new BaseDoor[2] { westDoor, eastDoor };
        }

        public uint CreateKeys(Mobile m)
        {
            uint value = Key.RandomValue();

            if (!IsAosRules)
            {
                Key packKey = new Key(KeyType.Gold);
                Key bankKey = new Key(KeyType.Gold);

                packKey.KeyValue = value;
                bankKey.KeyValue = value;

                //packKey.LootType = LootType.Newbied;
                //bankKey.LootType = LootType.Newbied;

                BankBox box = m.BankBox;

                if (box == null || !box.TryDropItem(m, bankKey, false))
                    bankKey.Delete();

                m.AddToBackpack(packKey);
            }

            return value;
        }

        public BaseDoor[] AddSouthDoors(int x, int y, int z)
        {
            return AddSouthDoors(true, x, y, z, false);
        }

        public BaseDoor[] AddSouthDoors(bool wood, int x, int y, int z, bool inv)
        {
            BaseDoor westDoor = MakeDoor(wood, inv ? DoorFacing.WestCCW : DoorFacing.WestCW);
            BaseDoor eastDoor = MakeDoor(wood, inv ? DoorFacing.EastCW : DoorFacing.EastCCW);

            // westDoor.Link = eastDoor;
            // eastDoor.Link = westDoor;

            AddDoor(westDoor, x, y, z);
            AddDoor(eastDoor, x + 1, y, z);

            return new BaseDoor[2] { westDoor, eastDoor };
        }

        public BaseDoor MakeDoor(bool wood, DoorFacing facing)
        {
            if (wood)
                return new DarkWoodHouseDoor(facing);
            else
                return new MetalHouseDoor(facing);
        }

        public void AddDoor(BaseDoor door, int xoff, int yoff, int zoff)
        {
            door.MoveToWorld(new Point3D(xoff + this.X, yoff + this.Y, zoff + this.Z), this.Map);
            m_Doors.Add(door);
        }

        public void RemoveDoor(BaseDoor door)
        {
            if (m_Doors != null && m_Doors.Contains(door))
                m_Doors.Remove(door);
        }

        public void AddTrashBarrel(Mobile from)
        {
            for (int i = 0; m_Doors != null && i < m_Doors.Count; ++i)
            {
                BaseDoor door = m_Doors[i] as BaseDoor;
                Point3D p = door.Location;

                if (door.Open)
                    p = new Point3D(p.X - door.Offset.X, p.Y - door.Offset.Y, p.Z - door.Offset.Z);

                if ((from.Z + 16) >= p.Z && (p.Z + 16) >= from.Z)
                {
                    if (from.InRange(p, 1))
                    {
                        from.SendLocalizedMessage(502120); // You cannot place a trash barrel near a door or near steps.
                        return;
                    }
                }
            }

            if (m_Trash == null || m_Trash.Deleted)
            {
                m_Trash = new TrashBarrel();

                m_Trash.Movable = false;
                m_Trash.MoveToWorld(from.Location, from.Map);

                from.SendLocalizedMessage(502121); /* You have a new trash barrel.
													  * Three minutes after you put something in the barrel, the trash will be emptied.
													  * Be forewarned, this is permanent! */
            }
            else
            {
                m_Trash.MoveToWorld(from.Location, from.Map);
            }
        }

        public void SetSign(int xoff, int yoff, int zoff)
        {
            m_Sign = new HouseSign(this);
            m_Sign.MoveToWorld(new Point3D(this.X + xoff, this.Y + yoff, this.Z + zoff), this.Map);
        }

        // Adam: is this one of our exceptions to the lockbox container?
        private bool IsExceptionContainer(Item i)
        {
            if (i is BaseBoard) return true;    // BaseBoard doesn't count as lockboxes
            if (i is KeyRing) return true;      // keyrings shouldn't count as lockboxes
            if (i is BaseContainer)             // deco BaseContainers don't count as lockboxes
            {
                if (((BaseContainer)i).Deco == true || i is TrashBarrel) // wea: trashbarrels don't count as lockboxes
                    return true;
            }
            return false;
        }

        public void SetLockdown(Item i, bool locked)
        {
            SetLockdown(i, locked, false);
        }

        private void SetLockdown(Item i, bool locked, bool checkContains)
        {
            if (m_LockDowns == null)
                return;

            i.Movable = !locked;
            i.IsLockedDown = locked;

            if (locked)
            {
                if (!checkContains || !m_LockDowns.Contains(i))
                    if (i is Container && !IsExceptionContainer(i) /*&& !m_Public*/ )
                        m_LockBoxCount += 1;

                m_LockDowns.Add(i);
            }
            else
            {
                if (i is Container && !IsExceptionContainer(i) /*&& !m_Public*/ )
                    m_LockBoxCount -= 1;

                m_LockDowns.Remove(i);
            }

            if (!locked)
                i.SetLastMoved();


        }
        public bool LockDown(Mobile m, Item item)
        {
            return LockDown(m, item, true);
        }

        public bool LockDown(Mobile m, Item item, bool checkIsInside)
        {
            if (!IsFriend(m))
                return false;

            if (item.Movable && !IsSecure(item))
            {
                // wea: 14/Aug/2006 modified so containers are treated as a single item
                int amt;
                if (item is BaseContainer)
                    amt = 1;
                else
                    amt = 1 + item.TotalItems;

                Item rootItem = item.RootParent as Item;

                if (checkIsInside && item.RootParent is Mobile)
                {
                    m.SendLocalizedMessage(1005525);//That is not in your house
                }
                else if (checkIsInside && !IsInside(item.GetWorldLocation(), item.ItemData.Height))
                {
                    m.SendLocalizedMessage(1005525);//That is not in your house
                }
                else if (Ethics.Ethic.IsImbued(item))
                {
                    m.SendLocalizedMessage(1005377);//You cannot lock that down
                }
                else if (IsSecure(rootItem))
                {
                    m.SendLocalizedMessage(501737); // You need not lock down items in a secure container.
                }
                //Pix: In order to eliminate an exploit where players can create non-movable objects anywhere
                // in the world, we'll make then not be able to lock down items inside containers.
                // If the item is not in a container, then the rootItem will be null.
                else if (rootItem != null)
                {
                    m.SendMessage("You cannot lock down items inside containers.");
                }
                //else if ( rootItem != null && !IsLockedDown( rootItem ) )
                //{
                //	m.SendLocalizedMessage( 501736 ); // You must lockdown the container first!
                //}
                else if (IsAosRules ? (!CheckAosLockdowns(amt) || !CheckAosStorage(amt)) : (this.LockDownCount + amt) > m_MaxLockDowns)
                {
                    m.SendLocalizedMessage(1005379);//That would exceed the maximum lock down limit for this house
                }
                else if (item is Container && !IsExceptionContainer(item) && (m_LockBoxCount >= m_MaxLockBoxes /*|| m_Public*/))
                {
                    /*if ( m_Public )
						m.SendMessage( "Public houses may not have locked down containers." );
					else*/
                    m.SendMessage("The maximum number of LockBoxes has been reached : {0}", m_MaxLockBoxes.ToString());

                    return false;
                }
                else if (item is Container && !IsExceptionContainer(item) && StorageTaxCredits == 0 && m_LockBoxCount >= LockBoxFloor)
                {
                    m.SendMessage("You do not have enough stored tax credits to lock that down.");
                    return false;
                }
                else
                {
                    item.SendMessageTo(m, false, 0x3B2, "(locked down)");
                    SetLockdown(item, true);
                    return true;
                }
            }
            else if (m_LockDowns.IndexOf(item) != -1)
            {
                m.SendLocalizedMessage(1005526);//That is already locked down
                return true;
            }
            else
            {
                m.SendLocalizedMessage(1005377);//You cannot lock that down
            }

            return false;
        }

        private class TransferItem : Item
        {
            private BaseHouse m_House;

            public TransferItem(BaseHouse house)
                : base(0x14F0)
            {
                m_House = house;

                Hue = 0x480;
                Movable = false;
                Name = "a house transfer contract";
            }

            public override void GetProperties(ObjectPropertyList list)
            {
                base.GetProperties(list);

                string houseName, owner, location;

                Item sign = (m_House == null ? null : m_House.Sign);

                if (sign == null || sign.Name == null || sign.Name == "a house sign")
                    houseName = "nothing";
                else
                    houseName = sign.Name;

                Mobile houseOwner = (m_House == null ? null : m_House.Owner);

                if (houseOwner == null)
                    owner = "nobody";
                else
                    owner = houseOwner.Name;

                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                bool valid = m_House != null && Sextant.Format(m_House.Location, m_House.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

                if (valid)
                    location = String.Format("{0}� {1}'{2}, {3}� {4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W");
                else
                    location = "????";

                list.Add(1061112, Utility.FixHtml(houseName)); // House Name: ~1_val~
                list.Add(1061113, owner); // Owner: ~1_val~
                list.Add(1061114, location); // Location: ~1_val~
            }

            public TransferItem(Serial serial)
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

                Delete();
            }

            public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (!base.AllowSecureTrade(from, to, newOwner, accepted))
                    return false;
                else if (!accepted)
                    return true;

                if (Deleted || m_House == null || m_House.Deleted || !m_House.IsOwner(from) || !from.CheckAlive() || !to.CheckAlive())
                    return false;

                if (BaseHouse.HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                    return false;
                }

                TownshipRegion tr = TownshipRegion.GetTownshipAt(from);
                if (tr != null)
                {
                    if (tr.TStone != null)
                    {
                        if (to.Guild != tr.TStone.Guild)
                        {
                            string guildname = "";
                            try { guildname = tr.TStone.Guild.Abbreviation; }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                            from.SendMessage("You cannot tranfer a house within the {0} township to a person who is not a member of that guild.", guildname);
                            return false;
                        }
                    }
                }

                return m_House.CheckTransferPosition(from, to);
            }

            public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (Deleted || m_House == null || m_House.Deleted || !m_House.IsOwner(from) || !from.CheckAlive() || !to.CheckAlive())
                    return;

                Delete();

                if (!accepted)
                    return;

                from.SendLocalizedMessage(501338); // You have transferred ownership of the house.
                to.SendLocalizedMessage(501339); /* You are now the owner of this house.
													* The house's co-owner, friend, ban, and access lists have been cleared.
													* You should double-check the security settings on any doors and teleporters in the house.
													*/

                m_House.RemoveKeys(from);
                m_House.Owner = to;
                m_House.Bans.Clear();
                m_House.Friends.Clear();
                m_House.CoOwners.Clear();
                if (m_House.Public == false)
                    m_House.ChangeLocks(to);
            }
        }

        public bool CheckTransferPosition(Mobile from, Mobile to)
        {
            bool isValid = true;
            Item sign = m_Sign;
            Point3D p = (sign == null ? Point3D.Zero : sign.GetWorldLocation());

            if (from.Map != Map || to.Map != Map)
                isValid = false;
            else if (sign == null)
                isValid = false;
            else if (from.Map != sign.Map || to.Map != sign.Map)
                isValid = false;
            else if (IsInside(from))
                isValid = false;
            else if (IsInside(to))
                isValid = false;
            else if (!from.InRange(p, 2))
                isValid = false;
            else if (!to.InRange(p, 2))
                isValid = false;

            if (!isValid)
                from.SendLocalizedMessage(1062067); // In order to transfer the house, you and the recipient must both be outside the building and within two paces of the house sign.

            return isValid;
        }

        public void BeginConfirmTransfer(Mobile from, Mobile to)
        {
            if (Deleted || !from.CheckAlive() || !IsOwner(from))
                return;

            if (from == to)
            {
                from.SendLocalizedMessage(1005330); // You cannot transfer a house to yourself, silly.
            }
            else if (to.Player)
            {
                //SMD: township check addition
                bool bTownshipGuildCheckPassed = true;
                string guildname = "";

                try //safety
                {
                    TownshipRegion tr = TownshipRegion.GetTownshipAt(from);
                    if (tr != null)
                    {
                        if (tr.TStone != null)
                        {
                            if (to.Guild != tr.TStone.Guild)
                            {
                                try { guildname = tr.TStone.Guild.Abbreviation; }
                                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                                bTownshipGuildCheckPassed = false;
                            }
                        }
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                //SMD: end township check addition

                if (BaseHouse.HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                }
                //SMD: township check addition
                else if (!bTownshipGuildCheckPassed)
                {
                    from.SendMessage("You cannot tranfer a house within the {0} township to a person who is not a member of that guild.", guildname);
                }
                //SMD: end township check addition
                else if (CheckTransferPosition(from, to))
                {
                    from.SendLocalizedMessage(1005326); // Please wait while the other player verifies the transfer.
                    to.SendGump(new Gumps.HouseTransferGump(from, to, this));
                }
            }
            else
            {
                from.SendLocalizedMessage(501384); // Only a player can own a house!
            }
        }

        public void AdminTransfer(Mobile to)
        {
            Mobile from = Owner == null ? to : Owner;
            new TransferItem(this).OnSecureTrade(from, to, null, true);
        }

        public void EndConfirmTransfer(Mobile from, Mobile to)
        {
            if (Deleted || !from.CheckAlive() || !IsOwner(from))
                return;

            if (from == to)
            {
                from.SendLocalizedMessage(1005330); // You cannot transfer a house to yourself, silly.
            }
            else if (to.Player)
            {
                if (BaseHouse.HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                }
                else if (CheckTransferPosition(from, to))
                {
                    /*
					NetState fromState = from.NetState, toState = to.NetState;

					if ( fromState != null && toState != null )
					{
						Container c = fromState.TradeWith( toState );

						c.DropItem( new TransferItem( this ) );
					}
					*/
                    NetState fromState = from.NetState, toState = to.NetState;

                    if (fromState != null && toState != null)
                    {
                        if (from.HasTrade)
                        {
                            from.SendLocalizedMessage(1062071); // You cannot trade a house while you have other trades pending.
                        }
                        else if (to.HasTrade)
                        {
                            to.SendLocalizedMessage(1062071); // You cannot trade a house while you have other trades pending.
                        }
                        else
                        {
                            Container c = fromState.AddTrade(toState);

                            c.DropItem(new TransferItem(this));
                        }
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(501384); // Only a player can own a house!
            }
        }

        public void Release(Mobile m, Item item)
        {
            if (!IsFriend(m))
                return;

            if (item is TentBackpack)
            {
                m.SendLocalizedMessage(501722);//That isn't locked down...
            }
            else if (IsLockedDown(item))
            {
#if RunUO
                item.PublicOverheadMessage(Server.Network.MessageType.Label, 0x3B2, 501657);//[no longer locked down]
#else
                item.SendLocalizedMessageTo(m, 501657); // (no longer locked down)
#endif
                SetLockdown(item, false);
                //TidyItemList( m_LockDowns );
            }
            else if (IsSecure(item))
            {
                ReleaseSecure(m, item);
            }
            else
            {
                m.SendLocalizedMessage(501722);//That isn't locked down...
            }
        }

        public void AddSecure(Mobile m, Item item)
        {
            if (m_Secures == null || !IsCoOwner(m))
                return;

            if (!IsInside(item))
            {
                m.SendLocalizedMessage(1005525); // That is not in your house
            }
            else if (IsLockedDown(item))
            {
                m.SendLocalizedMessage(1010550); // This is already locked down and cannot be secured.
            }
            else if (!(item is Container))
            {
                LockDown(m, item);
            }
            else
            {
                SecureInfo info = null;

                for (int i = 0; info == null && i < m_Secures.Count; ++i)
                    if (((SecureInfo)m_Secures[i]).Item == item)
                        info = (SecureInfo)m_Secures[i];

                if (info != null)
                {
                    m.SendGump(new Gumps.SetSecureLevelGump(m_Owner, info));
                }
                else if (item.Parent != null)
                {
                    m.SendLocalizedMessage(1010423); // You cannot secure this, place it on the ground first.
                }
                else if (!item.Movable)
                {
                    m.SendLocalizedMessage(1010424); // You cannot secure this.
                }
                else if (!IsAosRules && SecureCount >= MaxSecures)
                {
                    // The maximum number of secure items has been reached : 
                    m.SendLocalizedMessage(1008142, true, MaxSecures.ToString());
                }
                else if (IsAosRules ? !CheckAosLockdowns(1) : ((LockDownCount + 125) > MaxLockDowns))
                {
                    m.SendLocalizedMessage(1005379); // That would exceed the maximum lock down limit for this house
                }
                else if (IsAosRules && !CheckAosStorage(item.TotalItems))
                {
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.
                }
                else
                {
                    item.SendMessageTo(m, false, 0x3B2, "(secured)");
                    info = new SecureInfo((Container)item, SecureLevel.CoOwners);

                    item.IsLockedDown = false;
                    item.IsSecure = true;

                    m_Secures.Add(info);
                    m_LockDowns.Remove(item);
                    item.Movable = false;

                    m.SendGump(new Gumps.SetSecureLevelGump(m_Owner, info));
                }
            }
        }

        public bool HasSecureAccess(Mobile m, SecureLevel level)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            switch (level)
            {
                case SecureLevel.Owner: return IsOwner(m);
                case SecureLevel.CoOwners: return IsCoOwner(m);
                case SecureLevel.Friends: return IsFriend(m);
                case SecureLevel.Anyone: return true;
            }

            return false;
        }

        public void ReleaseSecure(Mobile m, Item item)
        {
            if (m_Secures == null || !IsOwner(m) || item is StrongBox)
                return;

            for (int i = 0; i < m_Secures.Count; ++i)
            {
                SecureInfo info = (SecureInfo)m_Secures[i];

                if (info.Item == item && HasSecureAccess(m, info.Level))
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;
                    item.SetLastMoved();
                    item.PublicOverheadMessage(Server.Network.MessageType.Label, 0x3B2, 501656);//[no longer secure]
                    m_Secures.RemoveAt(i);
                    return;
                }
            }

            m.SendLocalizedMessage(501717);//This isn't secure...
        }

        public void ReleaseSecure(Item item)
        {
            if (m_Secures == null)
                return;

            for (int i = 0; i < m_Secures.Count; ++i)
            {
                SecureInfo info = (SecureInfo)m_Secures[i];

                if (info.Item == item)
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;
                    item.SetLastMoved();
                    m_Secures.RemoveAt(i);
                    return;
                }
            }
        }

        public override bool Decays
        {
            get
            {
                return false;
            }
        }

        public void AddStrongBox(Mobile from)
        {
            if (!IsCoOwner(from))
                return;

            if (from == Owner)
            {
                from.SendLocalizedMessage(502109); // Owners don't get a strong box
                return;
            }

            if (IsAosRules ? !CheckAosLockdowns(1) : ((LockDownCount + 1) > m_MaxLockDowns))
            {
                from.SendLocalizedMessage(1005379);//That would exceed the maximum lock down limit for this house
                return;
            }

            foreach (SecureInfo info in m_Secures)
            {
                Container c = info.Item;

                if (!c.Deleted && c is StrongBox && ((StrongBox)c).Owner == from)
                {
                    from.SendLocalizedMessage(502112);//You already have a strong box
                    return;
                }
            }

            for (int i = 0; m_Doors != null && i < m_Doors.Count; ++i)
            {
                BaseDoor door = m_Doors[i] as BaseDoor;
                Point3D p = door.Location;

                if (door.Open)
                    p = new Point3D(p.X - door.Offset.X, p.Y - door.Offset.Y, p.Z - door.Offset.Z);

                if ((from.Z + 16) >= p.Z && (p.Z + 16) >= from.Z)
                {
                    if (from.InRange(p, 1))
                    {
                        from.SendLocalizedMessage(502113); // You cannot place a strongbox near a door or near steps.
                        return;
                    }
                }
            }

            StrongBox sb = new StrongBox(from, this);
            sb.Movable = false;
            sb.IsLockedDown = false;
            sb.IsSecure = true;
            m_Secures.Add(new SecureInfo(sb, SecureLevel.CoOwners));
            sb.MoveToWorld(from.Location, from.Map);
        }

        public void Kick(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Friends == null)
                return;

            if (targ.AccessLevel > AccessLevel.Player && from.AccessLevel <= targ.AccessLevel)
            {
                from.SendLocalizedMessage(501346); // Uh oh...a bigger boot may be required!
            }
            else if (IsFriend(targ))
            {
                from.SendLocalizedMessage(501348); // You cannot eject a friend of the house!
            }
            else if (targ is PlayerVendor)
            {
                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
            }
            else if (!IsInside(targ))
            {
                from.SendLocalizedMessage(501352); // You may not eject someone who is not in your house!
            }
            else
            {
                targ.MoveToWorld(BanLocation, Map);

                from.SendLocalizedMessage(1042840, targ.Name); // ~1_PLAYER NAME~ has been ejected from this house.
                targ.SendLocalizedMessage(501341); /* You have been ejected from this house.
													  * If you persist in entering, you may be banned from the house.
													  */
            }
        }

        public void RemoveAccess(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Access == null)
                return;

            if (m_Access.Contains(targ))
            {
                m_Access.Remove(targ);

                if (!HasAccess(targ) && IsInside(targ))
                {
                    targ.Location = BanLocation;
                    targ.SendLocalizedMessage(1060734); // Your access to this house has been revoked.
                }

                from.SendLocalizedMessage(1050051); // The invitation has been revoked.
            }
        }

        public void RemoveBan(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || m_Bans == null)
                return;

            if (m_Bans.Contains(targ))
            {
                m_Bans.Remove(targ);

                from.SendLocalizedMessage(501297); // The ban is lifted.
            }
        }

        public bool IsTownshipNPC(Mobile m)
        {
            bool bReturn = false;

            if (m != null)
            {
                Type type = m.GetType();
                TownshipNPCAttribute[] attributearray = (TownshipNPCAttribute[])type.GetCustomAttributes(typeof(TownshipNPCAttribute), false);

                if (attributearray.Length > 0)
                {
                    bReturn = true;
                }
            }

            return bReturn;
        }

        public void Ban(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Bans == null)
                return;

            if ((targ.AccessLevel > AccessLevel.Player && from.AccessLevel <= targ.AccessLevel) || targ is BaseGuard)
            {
                from.SendLocalizedMessage(501354); // Uh oh...a bigger boot may be required.
            }
            else if (IsTownshipNPC(targ))
            {
                from.SendLocalizedMessage(1062040); // You cannot ban that.
            }
            else if (IsFriend(targ))
            {
                from.SendLocalizedMessage(501348); // You cannot eject a friend of the house!
            }
            else if (targ is PlayerVendor)
            {
                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
            }
            else if (m_Bans.Count >= MaxBans)
            {
                from.SendLocalizedMessage(501355); // The ban limit for this house has been reached!
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501356); // This person is already banned!
            }
            else if (!IsInside(targ))
            {
                from.SendLocalizedMessage(501352); // You may not eject someone who is not in your house!
            }
            else if (!Public && IsAosRules)
            {
                from.SendLocalizedMessage(1062521); // You cannot ban someone from a private house.  Revoke their access instead.
            }
            else if (targ is BaseCreature && ((BaseCreature)targ).NoHouseRestrictions)
            {
                from.SendLocalizedMessage(1062040); // You cannot ban that.
            }
            else if (from.Aggressed.Count > 0)
            {
                bool allowBan = true;
                for (int i = 0; i < from.Aggressed.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)from.Aggressed[i];
                    if (info.Defender == targ)
                    {
                        allowBan = false;
                        break;
                    }
                }

                if (!allowBan)
                    from.SendMessage("You cannot ban someone while in the heat of battle!");
                else
                {
                    m_Bans.Add(targ);

                    from.SendLocalizedMessage(1042839, targ.Name); // ~1_PLAYER_NAME~ has been banned from this house.
                    targ.SendLocalizedMessage(501340); // You have been banned from this house.

                    targ.MoveToWorld(BanLocation, Map);
                }

            }
            else if (from.Criminal)
            {
                from.SendMessage("You cannot ban from your house while flagged criminal!");
            }
            else
            {
                m_Bans.Add(targ);

                from.SendLocalizedMessage(1042839, targ.Name); // ~1_PLAYER_NAME~ has been banned from this house.
                targ.SendLocalizedMessage(501340); // You have been banned from this house.

                targ.Location = BanLocation;
                targ.Map = Map;
            }
        }

        public PlayerMobile FindPlayer()
        {
            if (m_Region == null)
                return null;

            foreach (Mobile mx in m_Region.Players.Values)
            {
                PlayerMobile pm = mx as PlayerMobile;
                if (pm != null && Contains(pm))
                    return pm;
            }

            return null;
        }

        public bool CanPlacePlayerVendorInThisTownshipHouse()
        {
            CustomRegion cr = TownshipRegion.FindDRDTRegion(this.Map, this.Location);
            if (cr != null)
            {
                TownshipRegion tr = cr as TownshipRegion;
                if (tr != null)
                {
                    TownshipStone ts = tr.TStone;
                    if (ts != null)
                    {
                        if (m_Region != null)
                        {
                            foreach (Mobile m in m_Region.Mobiles.Values)
                            {
                                if (m != null)
                                {
                                    if (TownshipHelper.IsRestrictedTownshipNPC(m))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public Mobile FindTownshipNPC()
        {
            if (m_Region == null)
                return null;

            //Console.WriteLine("Looping through mobiles in region... {0}", r);
            foreach (Mobile m in m_Region.Mobiles.Values)
            {
                if (m != null)
                {
                    Type type = m.GetType();
                    TownshipNPCAttribute[] attributearray = (TownshipNPCAttribute[])type.GetCustomAttributes(typeof(TownshipNPCAttribute), false);

                    if (attributearray.Length > 0)
                    {
                        return m;
                    }
                }
            }

            return null;
        }

        public PlayerVendor FindPlayerVendor()
        {
            if (m_Region == null)
                return null;

            //Console.WriteLine("Looping through mobiles in region... {0}", r);
            foreach (Mobile mx in m_Region.Mobiles.Values)
            {
                PlayerVendor pv = mx as PlayerVendor;
                //Console.WriteLine(list[i]);

                if (pv != null) // wea: removed Contains() check... we already know it's the right place
                    return pv;      // and this screws up tents
            }

            return null;
        }

        public void GrantAccess(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Access == null)
                return;

            if (HasAccess(targ))
            {
                from.SendLocalizedMessage(1060729); // That person already has access to this house.
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(1060712); // That is not a player.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501367); // This person is banned!  Unban them first.
            }
            else
            {
                m_Access.Add(targ);

                targ.SendLocalizedMessage(1060735); // You have been granted access to this house.
            }
        }

        public void AddCoOwner(Mobile from, Mobile targ)
        {
            if (!IsOwner(from) || m_CoOwners == null || m_Friends == null)
                return;

            if (IsOwner(targ))
            {
                from.SendLocalizedMessage(501360); // This person is already the house owner!
            }
            else if (m_Friends.Contains(targ))
            {
                from.SendLocalizedMessage(501361); // This person is a friend of the house. Remove them first.
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(501362); // That can't be a co-owner of the house.
            }
            //			else if ( HasAccountHouse( targ ) )
            //			{
            //				from.SendLocalizedMessage( 501364 ); // That person is already a house owner.
            //			}
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501367); // This person is banned!  Unban them first.
            }
            else if (m_CoOwners.Count >= MaxCoOwners)
            {
                from.SendLocalizedMessage(501368); // Your co-owner list is full!
            }
            else if (m_CoOwners.Contains(targ))
            {
                from.SendLocalizedMessage(501369); // This person is already on your co-owner list!
            }
            else
            {
                m_CoOwners.Add(targ);

                targ.Delta(MobileDelta.Noto);
                targ.SendLocalizedMessage(501343); // You have been made a co-owner of this house.
            }
        }

        public void RemoveCoOwner(Mobile from, Mobile targ)
        {
            if (!IsOwner(from) || m_CoOwners == null)
                return;

            if (m_CoOwners.Contains(targ))
            {
                m_CoOwners.Remove(targ);

                targ.Delta(MobileDelta.Noto);

                from.SendLocalizedMessage(501299); // Co-owner removed from list.
                targ.SendLocalizedMessage(501300); // You have been removed as a house co-owner.

                foreach (SecureInfo info in m_Secures)
                {
                    Container c = info.Item;

                    if (c is StrongBox && ((StrongBox)c).Owner == targ)
                    {
                        c.IsLockedDown = false;
                        c.IsSecure = false;
                        m_Secures.Remove(c);
                        c.Destroy();
                        break;
                    }
                }
            }
        }

        public void AddFriend(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || m_Friends == null || m_CoOwners == null)
                return;

            if (IsOwner(targ))
            {
                from.SendLocalizedMessage(501370); // This person is already an owner of the house!
            }
            else if (m_CoOwners.Contains(targ))
            {
                from.SendLocalizedMessage(501369); // This person is already on your co-owner list!
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(501371); // That can't be a friend of the house.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501374); // This person is banned!  Unban them first.
            }
            else if (m_Friends.Count >= MaxFriends)
            {
                from.SendLocalizedMessage(501375); // Your friends list is full!
            }
            else if (m_Friends.Contains(targ))
            {
                from.SendLocalizedMessage(501376); // This person is already on your friends list!
            }
            else
            {
                m_Friends.Add(targ);

                targ.Delta(MobileDelta.Noto);
                targ.SendLocalizedMessage(501337); // You have been made a friend of this house.
            }
        }

        public void RemoveFriend(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || m_Friends == null)
                return;

            if (m_Friends.Contains(targ))
            {
                m_Friends.Remove(targ);

                targ.Delta(MobileDelta.Noto);

                from.SendLocalizedMessage(501298); // Friend removed from list.
                targ.SendLocalizedMessage(1060751); // You are no longer a friend of this house.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)21); // version

            // version 21 - Adam
            // record a unique id that ties this house to an account to support inheritance
            writer.WriteUInt32(m_AccountCode);

            // version 20 - Adam
            writer.WriteUInt32(m_NPCData);

            // version 19 - Adam
            writer.Write(m_RestartDecay);

            //version 18 - Adam
            writer.WriteUInt32((System.UInt32)m_Flags);

            //version 17 - Adam
            writer.WriteUInt32(m_UpgradeCosts);

            //version 16 - Adam
            writer.WriteUInt32(m_LockBoxData);

            //version 15 - Pix
            writer.Write(m_SecurePremises);

            //version 14 - TK - store bool if IDOC Announcement is running
            writer.Write((bool)(m_IDOC_Broadcast_TCE != null));

            //version 13 - Pix. - store minutes instead of timespan
            writer.Write(m_DecayMinutesStored);

            //version 12 - Pix. - house decay variables
            //writer.WriteDeltaTime( StructureDecayTime );
            writer.Write(m_NeverDecay);
            //end version 12 additions

            writer.Write(m_MaxLockBoxes);

            // use the Property to insure we have an accurate count
            writer.Write(LockBoxCount);

            writer.Write((int)m_Visits);

            writer.Write((int)m_Price);

            writer.WriteMobileList(m_Access);

            writer.Write(m_BuiltOn);
            writer.Write(m_LastTraded);

            writer.WriteItemList(m_Addons, true);

            writer.Write(m_Secures.Count);

            for (int i = 0; i < m_Secures.Count; ++i)
                ((SecureInfo)m_Secures[i]).Serialize(writer);

            writer.Write(m_Public);

            writer.Write(BanLocation);

            writer.Write(m_Owner);

            // Version 5 no longer serializes region coords
            /*writer.Write( (int)m_Region.Coords.Count );
			foreach( Rectangle2D rect in m_Region.Coords )
			{
				writer.Write( rect );
			}*/

            writer.WriteMobileList(m_CoOwners, true);
            writer.WriteMobileList(m_Friends, true);
            writer.WriteMobileList(m_Bans, true);

            writer.Write(m_Sign);
            writer.Write(m_Trash);

            writer.WriteItemList(m_Doors, true);
            writer.WriteItemList(m_LockDowns, true);
            //writer.WriteItemList( m_Secures, true );

            writer.Write((int)m_MaxLockDowns);
            writer.Write((int)m_MaxSecures);

            /* -- Adam: This code no longer applies as you cannot lock down containers
			 * that are not 'deco' in a public building. Furthermore, you cannot lock down
			 * items inside of a container.
			// Items in locked down containers that aren't locked down themselves must decay!
			//6/24/04 - Pix: this functionality shouldn't happen for private houses with our
			//	lockbox concept.
			if( m_Public )  
			{
				for ( int i = 0; i < m_LockDowns.Count; ++i )
				{
					Item item = (Item)m_LockDowns[i];

					if ( item is Container && !(item is BaseBoard) )
					{
						Container cont = (Container)item;
						ArrayList children = cont.Items;

						for ( int j = 0; j < children.Count; ++j )
						{
							Item child = (Item)children[j];

							if ( child.Decays && !child.IsLockedDown && !child.IsSecure && (child.LastMoved + child.DecayTime) <= DateTime.UtcNow )
								Timer.DelayCall( TimeSpan.Zero, new TimerCallback( child.Delete ) );
						}
					}
				}
			}
			*/
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            int count;
            bool idocannc = false;

            m_Region = new HouseRegion(this);

            switch (version)
            {
                case 21:
                    {
                        m_AccountCode = reader.ReadUInt32();
                        goto case 20;
                    }
                case 20:
                    {
                        m_NPCData = reader.ReadUInt32();
                        goto case 19;
                    }
                case 19:
                    {
                        m_RestartDecay = reader.ReadTimeSpan();
                        goto case 18;
                    }
                case 18:
                    {
                        m_Flags = (ImplFlags)reader.ReadUInt32();
                        goto case 17;
                    }
                case 17:
                    {
                        m_UpgradeCosts = reader.ReadUInt32();
                        goto case 16;
                    }
                case 16:
                    {
                        m_LockBoxData = reader.ReadUInt32();
                        goto case 15;
                    }
                case 15:
                    {
                        m_SecurePremises = reader.ReadBool();
                        goto case 14;
                    }
                case 14:
                    {
                        idocannc = reader.ReadBool();
                        goto case 13;
                    }
                case 13:
                    {
                        m_DecayMinutesStored = reader.ReadDouble();
                        m_NeverDecay = reader.ReadBool();
                        goto case 11; //note, this isn't a mistake - we want to skip 12
                    }
                case 12:
                    {
                        DateTime tempDT = reader.ReadDeltaTime();
                        //StructureDecayTime = reader.ReadDeltaTime();
                        m_DecayMinutesStored = (tempDT - DateTime.UtcNow).TotalMinutes;

                        m_NeverDecay = reader.ReadBool();
                        goto case 11;
                    }
                case 11:
                    {
                        m_MaxLockBoxes = reader.ReadInt();
                        m_LockBoxCount = reader.ReadInt();

                        goto case 9;
                    }
                case 10: // just a signal for updates
                case 9:
                    {
                        m_Visits = reader.ReadInt();
                        goto case 8;
                    }
                case 8:
                    {
                        m_Price = reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        m_Access = reader.ReadMobileList();
                        goto case 6;
                    }
                case 6:
                    {
                        m_BuiltOn = reader.ReadDateTime();
                        m_LastTraded = reader.ReadDateTime();
                        goto case 5;
                    }
                case 5: // just removed fields
                case 4:
                    {
                        m_Addons = reader.ReadItemList();
                        goto case 3;
                    }
                case 3:
                    {
                        count = reader.ReadInt();
                        m_Secures = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            SecureInfo info = new SecureInfo(reader);

                            if (info.Item != null)
                            {
                                info.Item.IsSecure = true;
                                info.Item.CancelFreezeTimer();        // don't initiate for Deserialize
                                m_Secures.Add(info);
                            }
                        }

                        goto case 2;
                    }
                case 2:
                    {
                        m_Public = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Region.GoLocation = reader.ReadPoint3D();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 16)
                        {
                            LockBoxCeling = (uint)m_MaxLockBoxes * 2;   // high limit
                            LockBoxFloor = (uint)m_MaxLockBoxes;        // low limit
                        }

                        if (version < 12)
                        {
                            Refresh();
                            m_NeverDecay = false;
                        }

                        if (version < 4)
                            m_Addons = new ArrayList();

                        if (version < 7)
                            m_Access = new ArrayList();

                        if (version < 8)
                            m_Price = DefaultPrice;

                        m_Owner = reader.ReadMobile();

                        if (version < 5)
                        {
                            count = reader.ReadInt();

                            for (int i = 0; i < count; i++)
                                reader.ReadRect2D();
                        }

                        UpdateRegionArea();

                        Region.AddRegion(m_Region);

                        m_CoOwners = reader.ReadMobileList();
                        m_Friends = reader.ReadMobileList();
                        m_Bans = reader.ReadMobileList();

                        m_Sign = reader.ReadItem() as HouseSign;
                        m_Trash = reader.ReadItem() as TrashBarrel;

                        m_Doors = reader.ReadItemList();
                        m_LockDowns = reader.ReadItemList();

                        for (int i = 0; i < m_LockDowns.Count; ++i)
                        {
                            Item item = m_LockDowns[i] as Item;
                            if (item != null)
                            {
                                item.IsLockedDown = true;
                                item.CancelFreezeTimer();        // don't initiate for Deserialize
                            }
                        }

                        if (version < 3)
                        {
                            ArrayList items = reader.ReadItemList();
                            m_Secures = new ArrayList(items.Count);

                            for (int i = 0; i < items.Count; ++i)
                            {
                                Container c = items[i] as Container;

                                if (c != null)
                                {
                                    c.IsSecure = true;
                                    m_Secures.Add(new SecureInfo(c, SecureLevel.CoOwners));
                                }
                            }
                        }

                        m_MaxLockDowns = reader.ReadInt();
                        m_MaxSecures = reader.ReadInt();

                        if ((Map == null || Map == Map.Internal) && Location == Point3D.Zero)
                            Delete();

                        if (m_Owner != null)
                        {
                            ArrayList list = (ArrayList)m_Table[m_Owner];

                            if (list == null)
                                m_Table[m_Owner] = list = new ArrayList();

                            list.Add(this);
                        }
                        break;
                    }
            }

            // fixup the account code
            if (version < 21)
            {
                m_AccountCode = CalcAccountCode(m_Owner);
            }

            // patch m_NPCData to hold the default barkeep count
            if (version < 20)
                MaximumBarkeepCount = 2;

            if (version <= 1)
                ChangeSignType(0xBD2);//private house, plain brass sign

            if (version < 10)
            {
                /* NOTE: This can exceed the house lockdown limit. It must be this way, because
				 * we do not want players' items to decay without them knowing. Or not even
				 * having a chance to fix it themselves.
				 */

                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(FixLockdowns_Sandbox));
            }

            if (idocannc) // idoc announcement was running when we saved, re-create it
            {
                string[] lines = new string[1];
                lines[0] = String.Format("Lord British has condemned the estate of {0} near {1}.", this.Owner.Name, DescribeLocation());
                m_IDOC_Broadcast_TCE = new TownCrierEntry(lines, TimeSpan.FromMinutes(m_DecayMinutesStored), Serial.MinusOne);
                GlobalTownCrierEntryList.Instance.AddEntry(m_IDOC_Broadcast_TCE);
            }
        }

        private void FixLockdowns_Sandbox()
        {
            ArrayList lockDowns = new ArrayList();

            for (int i = 0; m_LockDowns != null && i < m_LockDowns.Count; ++i)
            {
                Item item = (Item)m_LockDowns[i];

                if (item is Container)
                    lockDowns.Add(item);
            }

            for (int i = 0; i < lockDowns.Count; ++i)
                SetLockdown((Item)lockDowns[i], true, true);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                // owner change gets a new account code
                m_AccountCode = CalcAccountCode(value);

                if (m_Owner != null)
                {
                    ArrayList list = (ArrayList)m_Table[m_Owner];

                    if (list == null)
                        m_Table[m_Owner] = list = new ArrayList();

                    list.Remove(this);
                    m_Owner.Delta(MobileDelta.Noto);
                }

                m_Owner = value;

                if (m_Owner != null)
                {
                    ArrayList list = (ArrayList)m_Table[m_Owner];

                    if (list == null)
                        m_Table[m_Owner] = list = new ArrayList();

                    list.Add(this);
                    m_Owner.Delta(MobileDelta.Noto);
                }

                if (m_Sign != null)
                    m_Sign.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Visits
        {
            get { return m_Visits; }
            set { m_Visits = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Public
        {
            get
            {
                return m_Public;
            }
            set
            {
                if (m_Public != value)
                {
                    m_Public = value;

                    if (!m_Public)//privatizing the house, change to brass sign
                        ChangeSignType(0xBD2);

                    if (m_Sign != null)
                        m_Sign.InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSecures
        {
            get { return m_MaxSecures; }
            set { m_MaxSecures = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockBoxes
        {
            get { return m_MaxLockBoxes; }
            set { m_MaxLockBoxes = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D BanLocation
        {
            get
            {
                return m_Region.GoLocation;
            }
            set
            {
                m_Region.GoLocation = new Point3D(m_Region.GoLocation.X + value.X, m_Region.GoLocation.Y + value.Y, m_Region.GoLocation.Z + value.Z);
            }
        }

        public void SetBanLocation(Point3D p)
        {
            m_Region.GoLocation = p;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockDowns
        {
            get
            {
                return m_MaxLockDowns;
            }
            set
            {
                m_MaxLockDowns = value;
            }
        }

        public Region Region { get { return m_Region; } }
        public ArrayList CoOwners { get { return m_CoOwners; } set { m_CoOwners = value; } }
        public ArrayList Friends { get { return m_Friends; } set { m_Friends = value; } }
        public ArrayList Access { get { return m_Access; } set { m_Access = value; } }
        public ArrayList Bans { get { return m_Bans; } set { m_Bans = value; } }
        public ArrayList Doors { get { return m_Doors; } set { m_Doors = value; } }

        public int LockDownCount
        {
            get
            {
                int count = 0;

                if (m_LockDowns != null)
                    count += m_LockDowns.Count;

                for (int i = 0; i < m_LockDowns.Count; ++i)
                {
                    Item item = (Item)m_LockDowns[i];
                    if (item is SeedBox)
                    {
                        SeedBox SeedBox2 = (SeedBox)item;
                        int seedcount = SeedBox2.SeedCount();
                        count += (seedcount / 5);
                    }
                    if (item is Library) //library count as themselves plus books inside
                        count += (item.TotalItems / 2);
                }

                if (m_Secures != null)
                {
                    for (int i = 0; i < m_Secures.Count; ++i)
                    {
                        SecureInfo info = (SecureInfo)m_Secures[i];

                        if (info.Item.Deleted)
                            continue;
                        else if (info.Item is StrongBox)
                            count += 1;
                        else
                            count += 125;
                    }
                }

                return count;
            }
        }

        public int SecureCount
        {
            get
            {
                int count = 0;

                if (m_Secures != null)
                {
                    for (int i = 0; i < m_Secures.Count; i++)
                    {
                        SecureInfo info = (SecureInfo)m_Secures[i];

                        if (info.Item.Deleted)
                            continue;
                        else if (!(info.Item is StrongBox))
                            count += 1;
                    }
                }

                return count;
            }
        }

        public int LockBoxCount
        {
            get
            {
                int tempCount = 0;
                foreach (Item ix in m_LockDowns)
                {
                    if (ix == null || ix.Deleted == true)
                        continue;

                    if (ix is Container && !IsExceptionContainer(ix))
                        tempCount += 1;
                }
                if (m_LockBoxCount != tempCount)
                {
                    LogHelper Logger = new LogHelper("PhantomLockboxCleanup.log", false);
                    Logger.Log(LogType.Item, this.Sign, String.Format("Adjusting LockBoxCount by {0}", m_LockBoxCount - tempCount));
                    m_LockBoxCount = tempCount;
                }
                return m_LockBoxCount;
            }
        }

        public ArrayList Addons { get { return m_Addons; } set { m_Addons = value; } }
        public ArrayList LockDowns { get { return m_LockDowns; } }
        public ArrayList Secures { get { return m_Secures; } }
        public HouseSign Sign { get { return m_Sign; } set { m_Sign = value; } }

        public DateTime BuiltOn
        {
            get { return m_BuiltOn; }
            set { m_BuiltOn = value; }
        }

        public DateTime LastTraded
        {
            get { return m_LastTraded; }
            set { m_LastTraded = value; }
        }

        public override void OnDelete()
        {
            //Township cleanup
            Mobile tsNPC = this.FindTownshipNPC();
            int tscount = 0; //just a safety measure - there should be only one Township NPC anyways
            while (tsNPC != null && tscount < 10)
            {
                tsNPC.Delete();
                tsNPC = this.FindTownshipNPC();
                tscount++;
            }

            //Pix: 7/13/2008 - Removing the requirement of a townshipstone to be in a house.
            //			TownshipStone tstone = this.FindTownshipStone();
            //			if (tstone != null)
            //			{
            //				tstone.Delete();
            //			}
            //END Township cleanup


            // stop announcing this house!!
            if (m_IDOC_Broadcast_TCE != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                m_IDOC_Broadcast_TCE = null;
            }

            new FixColumnTimer(this).Start();

            if (m_Region != null)
                Region.RemoveRegion(m_Region);

            base.OnDelete();
        }

        private class FixColumnTimer : Timer
        {
            private Map m_Map;
            private int m_StartX, m_StartY, m_EndX, m_EndY;

            public FixColumnTimer(BaseMulti multi)
                : base(TimeSpan.Zero)
            {
                m_Map = multi.Map;

                MultiComponentList mcl = multi.Components;

                m_StartX = multi.X + mcl.Min.X;
                m_StartY = multi.Y + mcl.Min.Y;
                m_EndX = multi.X + mcl.Max.X;
                m_EndY = multi.Y + mcl.Max.Y;
            }

            protected override void OnTick()
            {
                if (m_Map == null)
                    return;

                for (int x = m_StartX; x <= m_EndX; ++x)
                    for (int y = m_StartY; y <= m_EndY; ++y)
                        m_Map.FixColumn(x, y);
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Owner != null)
            {
                ArrayList list = (ArrayList)m_Table[m_Owner];

                if (list == null)
                    m_Table[m_Owner] = list = new ArrayList();

                list.Remove(this);
            }

            if (m_Access != null)
                for (int i = m_Access.Count - 1; i >= 0; i--)
                    m_Access.Remove(i);

            if (m_Bans != null)
                for (int i = m_Bans.Count - 1; i >= 0; i--)
                    m_Bans.Remove(i);

            if (m_CoOwners != null)
                for (int i = m_CoOwners.Count - 1; i >= 0; i--)
                    m_CoOwners.Remove(i);

            if (m_Friends != null)
                for (int i = m_Friends.Count - 1; i >= 0; i--)
                    m_Friends.Remove(i);


            Region.RemoveRegion(m_Region);

            if (m_Sign != null)
                m_Sign.Delete();

            if (m_Trash != null)
                m_Trash.Delete();

            if (m_Doors != null)
            {
                for (int i = 0; i < m_Doors.Count; ++i)
                {
                    Item item = (Item)m_Doors[i];

                    if (item != null)
                        item.Delete();
                }

                m_Doors.Clear();
            }

            if (m_LockDowns != null)
            {
                for (int i = 0; i < m_LockDowns.Count; ++i)
                {
                    Item item = (Item)m_LockDowns[i];

                    if (item != null)
                    {
                        item.IsLockedDown = false;
                        item.IsSecure = false;
                        item.Movable = true;
                        item.SetLastMoved();
                    }
                }

                m_LockDowns.Clear();
            }

            if (m_Secures != null)
            {
                for (int i = 0; i < m_Secures.Count; ++i)
                {
                    SecureInfo info = (SecureInfo)m_Secures[i];

                    if (info.Item is StrongBox)
                    {
                        info.Item.Destroy();
                    }
                    else
                    {
                        info.Item.IsLockedDown = false;
                        info.Item.IsSecure = false;
                        info.Item.Movable = true;
                        info.Item.SetLastMoved();
                    }
                }

                m_Secures.Clear();
            }

            if (m_Addons != null)
            {
                for (int i = 0; i < m_Addons.Count; ++i)
                {
                    Item item = (Item)m_Addons[i];

                    if (item != null)
                        item.Delete();
                }

                m_Addons.Clear();
            }
        }

        public static bool HasHouse(Mobile m)
        {
            if (m == null)
                return false;

            ArrayList list = (ArrayList)m_Table[m];

            if (list == null)
                return false;

            for (int i = 0; i < list.Count; ++i)
            {
                BaseHouse h = (BaseHouse)list[i];

                if (!h.Deleted)
                    return true;
            }

            return false;
        }

        public static bool HasAccountHouse(Mobile m)
        {
            Account a = m.Account as Account;

            if (a == null)
                return false;

            for (int i = 0; i < 5; ++i)
                if (a[i] != null && HasHouse(a[i]))
                    return true;

            return false;
        }

        public bool CheckInheritance(Mobile m)
        {
            Account a = m.Account as Account;

            // if they already have a house, than this can't be thiers
            //	PS. We will want to modify this if we ever allow multiple houses per account.
            if (a == null || HasAccountHouse(m))
                return false;

            // same account?
            if (CalcAccountCode(m) == AccountCode)
                return true;

            return false;
        }

        public UInt32 CalcAccountCode()
        {
            return CalcAccountCode(m_Owner);
        }

        public UInt32 CalcAccountCode(Mobile m)
        {
            if (m != null && m.Account != null)
            {
                Account a = m.Account as Account;
                if (a == null)
                    return 0;
                else
                    return (UInt32)a.Username.GetHashCode();
            }
            else
                return 0;
        }

        public bool CheckAccount(Mobile mobCheck, Mobile accCheck)
        {
            if (accCheck != null)
            {
                Account a = accCheck.Account as Account;

                if (a != null)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        if (a[i] == mobCheck)
                            return true;
                    }
                }
            }

            return false;
        }

        public bool IsOwner(Mobile m)
        {
            if (m == null)
                return false;

            if (m == m_Owner || m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            //return IsAosRules && CheckAccount( m, m_Owner );
            return CheckAccount(m, m_Owner);
        }

        public bool IsCoOwner(Mobile m)
        {
            if (m == null || m_CoOwners == null)
                return false;

            if (IsOwner(m) || m_CoOwners.Contains(m))
                return true;

            //return !IsAosRules && CheckAccount( m, m_Owner );
            return CheckAccount(m, m_Owner);
        }

        public void RemoveKeys(Mobile m)
        {
            if (m_Doors != null)
            {
                uint keyValue = 0;

                for (int i = 0; keyValue == 0 && i < m_Doors.Count; ++i)
                {
                    BaseDoor door = m_Doors[i] as BaseDoor;

                    if (door != null)
                        keyValue = door.KeyValue;
                }

                Key.RemoveKeys(m, keyValue);
            }
        }

        public void ChangeLocks(Mobile m)
        {
            uint keyValue = CreateKeys(m);

            if (m_Doors != null)
            {
                for (int i = 0; i < m_Doors.Count; ++i)
                {
                    BaseDoor door = m_Doors[i] as BaseDoor;

                    if (door != null)
                        door.KeyValue = keyValue;
                }
            }
        }

        public void RemoveLocks()
        {
            if (m_Doors != null)
            {
                for (int i = 0; i < m_Doors.Count; ++i)
                {
                    BaseDoor door = m_Doors[i] as BaseDoor;

                    if (door != null)
                    {
                        door.KeyValue = 0;
                        door.Locked = false;
                    }
                }
            }
        }

        public virtual HousePlacementEntry ConvertEntry { get { return null; } }
        public virtual int ConvertOffsetX { get { return 0; } }
        public virtual int ConvertOffsetY { get { return 0; } }
        public virtual int ConvertOffsetZ { get { return 0; } }

        public virtual int DefaultPrice { get { return 0; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Price { get { return m_Price; } set { m_Price = value; } }

        public virtual HouseDeed GetDeed()
        {
            return null;
        }

        public bool IsFriend(Mobile m)
        {
            if (m == null || m_Friends == null)
                return false;

            return (IsCoOwner(m) || m_Friends.Contains(m));
        }

        public bool IsBanned(Mobile m)
        {
            if (m == null || m == Owner || m.AccessLevel > AccessLevel.Player || m_Bans == null)
                return false;

            for (int i = 0; i < m_Bans.Count; ++i)
            {
                Mobile c = (Mobile)m_Bans[i];

                if (c == m)
                    return true;

                // The following section purports to ban all characters on a player's account from a house, 
                // but it doesn't work right, and anyway, we don't want to do that.
                //Account a = c.Account as Account;

                //if ( a == null )
                //    continue;

                //for ( int j = 0; j < 5; ++j )
                //{
                //    if ( a[i] == m )
                //        return true;
                //}
            }

            return false;
        }

        public bool HasAccess(Mobile m)
        {
            if (m == null)
                return false;

            if (m.AccessLevel > AccessLevel.Player || IsFriend(m) || (m_Access != null && m_Access.Contains(m)))
                return true;

            if (m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;

                if (bc.NoHouseRestrictions)
                    return true;

                if (bc.Controlled || bc.Summoned)
                {
                    m = bc.ControlMaster;

                    if (m == null)
                        m = bc.SummonMaster;

                    if (m == null)
                        return false;

                    if (m.AccessLevel > AccessLevel.Player || IsFriend(m) || (m_Access != null && m_Access.Contains(m)))
                        return true;
                }
            }

            return false;
        }

        public new bool IsLockedDown(Item check)
        {
            if (check == null)
                return false;

            if (m_LockDowns == null)
                return false;

            return m_LockDowns.Contains(check);
        }

        public new bool IsSecure(Item item)
        {
            if (item == null)
                return false;

            if (m_Secures == null)
                return false;

            bool contains = false;

            for (int i = 0; !contains && i < m_Secures.Count; ++i)
                contains = (((SecureInfo)m_Secures[i]).Item == item);

            return contains;
        }

        public Item[] FindAllItems(Type itemType)
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));
            List<Item> list = new List<Item>();
            foreach (Item item in eable)
                if (item.GetType() == itemType && Contains(item))
                    list.Add(item);

            eable.Free();
            return list.ToArray();
        }


        public virtual Guildstone FindGuildstone()
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (Item item in eable)
            {
                if (item is Guildstone && Contains(item))
                {
                    eable.Free();
                    return (Guildstone)item;
                }
            }

            eable.Free();
            return null;
        }

        public virtual TownshipStone FindTownshipStone()
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (Item item in eable)
            {
                if (item is TownshipStone && Contains(item))
                {
                    eable.Free();
                    return (TownshipStone)item;
                }
            }

            eable.Free();
            return null;
        }

        public void LogCommand(Mobile from, string command, object targeted)
        {
            CommandLogging.WriteLine(from, String.Format("{0} {1} ('{2}') used command '{3}' on '{4}'", from.AccessLevel, from, ((Account)from.Account).Username, command, targeted.ToString()));
        }
    }

    public enum SecureAccessResult
    {
        Insecure,
        Accessible,
        Inaccessible
    }

    public enum SecureLevel
    {
        Owner,      // Only the player who secured the item and the owner of the house has access to open/use the item.
        CoOwners,   // Only the Co-Owners and the owner of the house has access to open/use the item.
        Friends,    // Friends, Co-Owners, and the owner of the house has access to open/use the item.
        Anyone      // For a Public House, anyone can open/use the item. For a Private House, anyone who has access can open/use the item.
    }

    public class SecureInfo : ISecurable
    {
        private Container m_Item;
        private SecureLevel m_Level;

        public Container Item { get { return m_Item; } }
        public SecureLevel Level { get { return m_Level; } set { m_Level = value; } }

        public SecureInfo(Container item, SecureLevel level)
        {
            m_Item = item;
            m_Level = level;
        }

        public SecureInfo(GenericReader reader)
        {
            m_Item = reader.ReadItem() as Container;
            m_Level = (SecureLevel)reader.ReadByte();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(m_Item);
            writer.Write((byte)m_Level);
        }
    }

    public class LockdownTarget : Target
    {
        private bool m_Release;
        private BaseHouse m_House;

        public LockdownTarget(bool release, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Release = release;
            m_House = house;
        }

        protected override void OnTargetNotAccessible(Mobile from, object targeted)
        {
            OnTarget(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Item)
            {
                if (m_Release)
                {
                    m_House.Release(from, (Item)targeted);
                    m_House.LogCommand(from, "I wish to release this", targeted);
                }
                else
                {
                    m_House.LockDown(from, (Item)targeted);
                    m_House.LogCommand(from, "I wish to lock this down", targeted);
                }
            }
            else
            {
                from.SendLocalizedMessage(1005377);//You cannot lock that down
            }
        }
    }

    public class SecureTarget : Target
    {
        private bool m_Release;
        private BaseHouse m_House;

        public SecureTarget(bool release, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Release = release;
            m_House = house;
        }

        protected override void OnTargetNotAccessible(Mobile from, object targeted)
        {
            OnTarget(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsCoOwner(from))
                return;

            if (targeted is Item)
            {
                if (((Item)targeted).IsSecure)
                {
                    from.SendMessage("That container is already secure.");
                }
                else if (((Item)targeted).IsLockedDown)
                {
                    from.SendMessage("That container is already locked down.");
                }
                else
                {
                    if (m_Release)
                    {
                        m_House.ReleaseSecure(from, (Item)targeted);
                        m_House.LogCommand(from, "I wish to release this", targeted);
                    }
                    else
                    {
                        m_House.AddSecure(from, (Item)targeted);
                        m_House.LogCommand(from, "I wish to secure this", targeted);
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1010424);//You cannot secure this
            }
        }
    }

    public class HouseKickTarget : Target
    {
        private BaseHouse m_House;

        public HouseKickTarget(BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Mobile)
            {
                m_House.Kick(from, (Mobile)targeted);
            }
            else
            {
                from.SendLocalizedMessage(501347);//You cannot eject that from the house!
            }
        }
    }

    public class HouseBanTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Banning;

        public HouseBanTarget(bool ban, BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Banning = ban;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Mobile)
            {
                if (m_Banning)
                    m_House.Ban(from, (Mobile)targeted);
                else
                    m_House.RemoveBan(from, (Mobile)targeted);
            }
            else
            {
                from.SendLocalizedMessage(501347);//You cannot eject that from the house!
            }
        }
    }

    public class HouseDecoTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Deco;

        public HouseDecoTarget(bool deco, BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Deco = deco;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            // make sure the item isn't locked down already
            ArrayList LockDowns = m_House.LockDowns;
            if (LockDowns.IndexOf(targeted) != -1)
            {
                from.SendMessage("You must release this item first.");
                return;
            }

            if (targeted is BaseContainer)
            {
                if (((BaseContainer)targeted).TotalItems > 0)
                {
                    from.SendMessage("That must be empty to make it decorative.");
                    return;
                }

                if (((BaseContainer)targeted).TotalItems > 0)
                {
                    from.SendMessage("That must be empty to make it decorative.");
                    return;
                }

                if (((BaseContainer)targeted).Deco == true && m_Deco == true)
                {
                    from.SendMessage("That is already decorative.");
                    return;
                }

                if (((BaseContainer)targeted).Deco == false && m_Deco == false)
                {
                    from.SendMessage("That is already functional.");
                    return;
                }

                // change the state
                ((BaseContainer)targeted).Deco = m_Deco;

                if (m_Deco == true)
                {
                    from.SendMessage("That container is now decorative.");
                    m_House.LogCommand(from, "I wish to make this decorative", targeted);
                }
                else
                {
                    from.SendMessage("That container is now functional.");
                    m_House.LogCommand(from, "I wish to make this functional", targeted);
                }
            }
            else
            {
                from.SendMessage("You cannot make that decorative.");
            }
        }
    }

    public class HouseAccessTarget : Target
    {
        private BaseHouse m_House;

        public HouseAccessTarget(BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Mobile)
                m_House.GrantAccess(from, (Mobile)targeted);
            else
                from.SendLocalizedMessage(1060712); // That is not a player.
        }
    }

    public class CoOwnerTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Add;

        public CoOwnerTarget(bool add, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Add = add;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsOwner(from))
                return;

            if (targeted is Mobile)
            {
                if (m_Add)
                    m_House.AddCoOwner(from, (Mobile)targeted);
                else
                    m_House.RemoveCoOwner(from, (Mobile)targeted);
            }
            else if (targeted is CertificateOfIdentity)
            {
                CertificateOfIdentity coi = targeted as CertificateOfIdentity;
                if (coi.Mobile == null || coi.Mobile.Deleted)
                    from.SendMessage("That identity certificate does not represent a player.");
                else
                {
                    if (m_Add)
                        m_House.AddCoOwner(from, coi.Mobile);
                    else
                        m_House.RemoveCoOwner(from, coi.Mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501362);//That can't be a coowner
            }
        }
    }

    public class HouseFriendTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Add;

        public HouseFriendTarget(bool add, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Add = add;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsCoOwner(from))
                return;

            if (targeted is Mobile)
            {
                if (m_Add)
                    m_House.AddFriend(from, (Mobile)targeted);
                else
                    m_House.RemoveFriend(from, (Mobile)targeted);
            }
            else if (targeted is CertificateOfIdentity)
            {
                CertificateOfIdentity coi = targeted as CertificateOfIdentity;
                if (coi.Mobile == null || coi.Mobile.Deleted)
                    from.SendMessage("That identity certificate does not represent a player.");
                else
                {
                    if (m_Add)
                        m_House.AddFriend(from, coi.Mobile);
                    else
                        m_House.RemoveFriend(from, coi.Mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501371); // That can't be a friend
            }
        }
    }

    public class HouseOwnerTarget : Target
    {
        private BaseHouse m_House;

        public HouseOwnerTarget(BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile)
                m_House.BeginConfirmTransfer(from, (Mobile)targeted);
            else
                from.SendLocalizedMessage(501384); // Only a player can own a house!
        }
    }

    public class SetSecureLevelEntry : ContextMenuEntry
    {
        private Item m_Item;
        private ISecurable m_Securable;

        public SetSecureLevelEntry(Item item, ISecurable securable)
            : base(6203, 6)
        {
            m_Item = item;
            m_Securable = securable;
        }

        public static ISecurable GetSecurable(Mobile from, Item item)
        {
            BaseHouse house = BaseHouse.FindHouseAt(item);

            if (house == null || !house.IsOwner(from) || !house.IsAosRules)
                return null;

            ISecurable sec = null;

            if (item is ISecurable)
            {
                bool isOwned = house.Doors.Contains(item);

                if (!isOwned)
                    isOwned = (house is HouseFoundation && ((HouseFoundation)house).IsFixture(item));

                if (!isOwned)
                    isOwned = house.IsLockedDown(item);

                if (isOwned)
                    sec = (ISecurable)item;
            }
            else
            {
                ArrayList list = house.Secures;

                for (int i = 0; sec == null && list != null && i < list.Count; ++i)
                {
                    SecureInfo si = (SecureInfo)list[i];

                    if (si.Item == item)
                        sec = si;
                }
            }

            return sec;
        }

        public static void AddTo(Mobile from, Item item, ArrayList list)
        {
            ISecurable sec = GetSecurable(from, item);

            if (sec != null)
                list.Add(new SetSecureLevelEntry(item, sec));
        }

        public override void OnClick()
        {
            ISecurable sec = GetSecurable(Owner.From, m_Item);

            if (sec != null)
                Owner.From.SendGump(new SetSecureLevelGump(Owner.From, sec));
        }

    }
}
