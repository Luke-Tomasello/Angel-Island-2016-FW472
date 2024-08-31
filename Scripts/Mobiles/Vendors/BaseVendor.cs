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

/* Scripts/Mobiles/Vendors/BaseVendor.cs 
 * Changelog
 *	3/12/11, adam
 *		Don't add loot to vendor backpack if server is siege
 *	05/25/09, plasma
 *		- Implemneted tax properly withouht floatinf point math
 *		- Excluded BBS from the tax.
 *	02/02/09, plasma
 *		Implement OnVendvorBuy hook for faction city regions
 *		Implement taxing of goods for faction city regions
 *  10/6/07, Adam
 *      all weapons and armor are exceptional during server wars
 *  8/16/07, Adam
 *    Virtualize the adding of loot InitLoot()
 *    If you override this method, be sure to add a backpack if needed.
 *  4/6/07, Adam
 *      additional Hardening to OnBuyItems() logic.
 *      Exception:
 *        System.NullReferenceException: Object reference not set to an instance of an object.
 *           at Server.Mobiles.BaseVendor.OnBuyItems(Mobile buyer, ArrayList list)
 *           at Server.Network.PacketHandlers.VendorBuyReply(NetState state, PacketReader pvSrc)
 *           at Server.Network.MessagePump.HandleReceive(NetState ns)
 *           at Server.Network.MessagePump.Slice()
 *           at Server.Core.Main(String[] args)
 *  07/18/06, Kit
 *		Added CanBeHarmful override to return false if invunerable is set to true.
 *		Fixs problem with explosion pots hitting vendors even if unable to deal damage.
 *  07/02/06, InitOutfit/Body overrides
 *	06/28/06, Adam
 *		Logic cleanup
 *	06/28/06, Adam
 *		- Make IsInvulnerable accessable by Admin
 *		- have m_Invulnerable default to true
 *  06/27/06, Kit
 *		Changed Invunerability to bool vs function override.
 *  05/19/06, Kit
 *		Added check for control chance of handleing a pet when purchaseing. 
 *		If buyer does not have 50% control or greater of pet tell them to bugger off.
 *	12/15/05, Pix
 *		Vendors now restock from 50-70 minutes (instead of 60).
 *		Added properties to see last vendor restock time and timespan until next restock.
 *	8/30/05, Pix
 *		Now only Admins can buy things w/o gold.
 *	4/20/05, Pix
 *		Fixed ResourcePool working with new client and new RunUO1.0 BaseVendor code.
 *	4/19/05, Pix
 *		Merged in RunUO 1.0 release code.
 *		Fixes 'vendor buy' showing just 'Deed'
 *  2/24/05 TK
 *		Any resource purchase of over 100 now gets commodity deed
 *		Commodity deeds will now list at the individual price of the resource, not
 *			price * amount (VendorSellList packet limitation - ushort price)
 *	2/7/05, Adam
 *		Leave previous try/catch, but relax the comment
 *	2/7/05, Adam
 *		Emergency patch to stop from crashing server - Catch the exception
 *		This is a stop-gap solution only.. we still need to find *why* this is throwing
 *		an exception (line 542)
 *  01/31/05 - TK
 *		Removed isBunch from IBuyItem interface
 *		Added a check to prevent players creating stacks > 60000
 *		Modified RP interface to work with failsafe resource generation
 *  01/28/05 - Taran Kain
 *		Changed slightly the ResourcePool interface
 *  01/23/05 - Taran Kain
 *		Added logic in VendorSell, VendorBuy, OnBuyItems, OnSellItems to support
 *		Resource Pool.
 *	11/3/04 - Pixie
 *		Put conditional code around the new vendor update packets so only clients
 *		version 4.0.5a and later get those packets.
 *	10/29/04 - Pixie
 *		Added try/catch around D6VendorPacket for safety.
 *	10/28/04 - Pixie
 *		Added new Packet for updating vendor descriptions that were broken
 *		with the 4.0.5b patch (I think that's the right number).
 *	10/18/04, Froste
 *      Commented out OnRestockReagents() as it is no longer in use
 *	10/12/04, Froste
 *      added line 581 to temporarily deal with empty buy lists
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Engines.BulkOrders;
using Server.Engines.ResourcePool;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public enum VendorShoeType
    {
        None,
        Shoes,
        Boots,
        Sandals,
        ThighBoots
    }

    public abstract class BaseVendor : BaseCreature, IVendor
    {
        private const int MaxSell = 500;

        protected abstract ArrayList SBInfos { get; }
        public ArrayList Inventory { get { return SBInfos; } }

        private ArrayList m_ArmorBuyInfo = new ArrayList();
        private ArrayList m_ArmorSellInfo = new ArrayList();

        private DateTime m_LastRestock;

        private bool m_Invulnerable = true;
        public override bool CanTeach { get { return Core.UOSP ? false : true; } }

        public override bool PlayerRangeSensitive { get { return true; } }

        public virtual bool IsActiveVendor { get { return true; } }
        public virtual bool IsActiveBuyer { get { return IsActiveVendor; } } // response to vendor SELL
        public virtual bool IsActiveSeller { get { return IsActiveVendor; } } // repsonse to vendor BUY

        public virtual NpcGuild NpcGuild { get { return NpcGuild.None; } }

        public override bool IsInvulnerable
        {
            get { return m_Invulnerable; }
            set
            {
                // June 2, 2001
                // http://martin.brenner.de/ultima/uo/news1.html
                // the (invulnerable) tag has been removed; invulnerable NPCs and players can now be identified by the yellow hue of their name
                // Adam: June 2, 2001 probably means Publish 12 which was July 24, 2001
                m_Invulnerable = value;
                if (m_Invulnerable && !Core.AOS && (Core.UOAI || Core.UOAR || Core.UOMO || PublishInfo.Publish >= 12))
                    NameHue = 0x35;
            }
        }

        public override bool ShowFameTitle { get { return false; } }

        public virtual bool IsValidBulkOrder(Item item)
        {
            return false;
        }

        public virtual Item CreateBulkOrder(Mobile from, bool fromContextMenu)
        {
            return null;
        }

        public virtual bool SupportsBulkOrders(Mobile from)
        {
            return false;
        }

        public virtual TimeSpan GetNextBulkOrder(Mobile from)
        {
            return TimeSpan.Zero;
        }

        #region Faction
        public virtual int GetPriceScalar()
        {
            Town town = Town.FromRegion(this.Region);

            if (town != null)
                return (100 + town.Tax);

            return 100;
        }

        public void UpdateBuyInfo()
        {
            int priceScalar = GetPriceScalar();

            IBuyItemInfo[] buyinfo = (IBuyItemInfo[])m_ArmorBuyInfo.ToArray(typeof(IBuyItemInfo));

            if (buyinfo != null)
            {
                foreach (IBuyItemInfo info in buyinfo)
                    info.PriceScalar = priceScalar;
            }
        }
        #endregion

        private class BulkOrderInfoEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private BaseVendor m_Vendor;

            public BulkOrderInfoEntry(Mobile from, BaseVendor vendor)
                : base(6152, 6)
            {
                m_From = from;
                m_Vendor = vendor;
            }

            public override void OnClick()
            {
                if (m_Vendor.SupportsBulkOrders(m_From))
                {
                    TimeSpan ts = m_Vendor.GetNextBulkOrder(m_From);

                    int totalSeconds = (int)ts.TotalSeconds;
                    int totalHours = (totalSeconds + 3599) / 3600;

                    if (totalHours == 0)
                    {
                        m_From.SendLocalizedMessage(1049038); // You can get an order now.

                        if (Core.AOS)
                        {
                            Item bulkOrder = m_Vendor.CreateBulkOrder(m_From, true);

                            if (bulkOrder is LargeBOD)
                                m_From.SendGump(new LargeBODAcceptGump(m_From, (LargeBOD)bulkOrder));
                            else if (bulkOrder is SmallBOD)
                                m_From.SendGump(new SmallBODAcceptGump(m_From, (SmallBOD)bulkOrder));
                        }
                    }
                    else
                    {
                        int oldSpeechHue = m_Vendor.SpeechHue;
                        m_Vendor.SpeechHue = 0x3B2;
                        m_Vendor.SayTo(m_From, 1049039, totalHours.ToString()); // An offer may be available in about ~1_hours~ hours.
                        m_Vendor.SpeechHue = oldSpeechHue;
                    }
                }
            }
        }

        public BaseVendor(string title)
            : base(AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2)
        {

            // Shopkeepers can fight (poorly) on Siege, make sure they have SOME skill
            //	we don't know what the correct skill here is...
            if (Core.UOSP && this.Skills != null && this.Skills.Wrestling != null)
                if (this.Skills.Wrestling.Value == 0)
                    SetSkill(SkillName.Wrestling, 36.0, 68.0);

            LoadSBInfo();

            /* Publish 4
			 * Any shopkeeper that is currently [invulnerable] will lose that status except for stablemasters.
			 */
            IsInvulnerable = (!Core.UOAI && !Core.UOAR && !Core.UOMO && PublishInfo.Publish < 8) ? false : true;

            this.Title = title;

            InitBody();
            InitOutfit();

            Container pack;
            //these packs MUST exist, or the client will crash when the packets are sent
            pack = new Backpack();
            pack.Layer = Layer.ShopBuy;
            pack.Movable = false;
            pack.Visible = false;
            AddItem(pack);

            pack = new Backpack();
            pack.Layer = Layer.ShopResale;
            pack.Movable = false;
            pack.Visible = false;
            AddItem(pack);

            m_LastRestock = DateTime.Now;
        }

        public BaseVendor(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastRestock
        {
            get
            {
                return m_LastRestock;
            }
            set
            {
                m_LastRestock = value;
                m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);
            }
        }

        private TimeSpan m_NextRestockVariant = TimeSpan.MinValue;

        [CommandProperty(AccessLevel.Counselor)]
        public virtual TimeSpan NextRestockVariant
        {
            get
            {
                if (m_NextRestockVariant == TimeSpan.MinValue)
                {
                    m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);
                }
                return m_NextRestockVariant;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public virtual TimeSpan RestockDelay
        {
            get
            {
                return (TimeSpan.FromHours(1.0) + NextRestockVariant);
            }
        }

        public Container BuyPack
        {
            get
            {
                Container pack = FindItemOnLayer(Layer.ShopBuy) as Container;

                if (pack == null)
                {
                    pack = new Backpack();
                    pack.Layer = Layer.ShopBuy;
                    pack.Visible = false;
                    AddItem(pack);
                }

                return pack;
            }
        }

        public abstract void InitSBInfo();

        protected void LoadSBInfo()
        {
            m_LastRestock = DateTime.Now;
            m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);

            InitSBInfo();

            m_ArmorBuyInfo.Clear();
            m_ArmorSellInfo.Clear();

            for (int i = 0; i < SBInfos.Count; i++)
            {
                SBInfo sbInfo = (SBInfo)SBInfos[i];
                m_ArmorBuyInfo.AddRange(sbInfo.BuyInfo);
                m_ArmorSellInfo.Add(sbInfo.SellInfo);
            }
        }

        public virtual bool GetGender()
        {
            return Utility.RandomBool();
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            SpeechHue = Utility.RandomDyedHue();
            Hue = Utility.RandomSkinHue();

            if (Female = GetGender())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness, bool ignoreOurDeadness)
        {
            if (IsInvulnerable)
                return false;

            return base.CanBeHarmful(target, message, ignoreOurBlessedness, ignoreOurDeadness);
        }
        public virtual int GetRandomHue()
        {
            switch (Utility.Random(5))
            {
                default:
                case 0: return Utility.RandomBlueHue();
                case 1: return Utility.RandomGreenHue();
                case 2: return Utility.RandomRedHue();
                case 3: return Utility.RandomYellowHue();
                case 4: return Utility.RandomNeutralHue();
            }
        }

        public virtual int GetShoeHue()
        {
            if (0.1 > Utility.RandomDouble())
                return 0;

            return Utility.RandomNeutralHue();
        }

        public virtual VendorShoeType ShoeType
        {
            get { return VendorShoeType.Shoes; }
        }

        public virtual int RandomBrightHue()
        {
            if (0.1 > Utility.RandomDouble())
                return Utility.RandomList(0x62, 0x71);

            return Utility.RandomList(0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59);
        }

        public virtual void CheckMorph()
        {
            if (CheckGargoyle())
                return;

            CheckNecromancer();
        }

        public virtual bool CheckGargoyle()
        {
            Map map = this.Map;

            if (map != Map.Ilshenar)
                return false;

            if (Region.Name != "Gargoyle City")
                return false;

            if (Body != 0x2F6 || (Hue & 0x8000) == 0)
                TurnToGargoyle();

            return true;
        }

        public virtual bool CheckNecromancer()
        {
            Map map = this.Map;

            if (map != Map.Malas)
                return false;

            if (Region.Name != "Umbra")
                return false;

            if (Hue != 0x83E8)
                TurnToNecromancer();

            return true;
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            CheckMorph();
        }

        protected override void OnMapChange(Map oldMap)
        {
            base.OnMapChange(oldMap);

            CheckMorph();
        }

        public virtual int GetRandomNecromancerHue()
        {
            switch (Utility.Random(20))
            {
                case 0: return 0;
                case 1: return 0x4E9;
                default: return Utility.RandomList(0x485, 0x497);
            }
        }

        public virtual void TurnToNecromancer()
        {
            ArrayList items = new ArrayList(this.Items);

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];

                if (item is Hair || item is Beard)
                    item.Hue = 0;
                else if (item is BaseClothing || item is BaseWeapon || item is BaseArmor || item is BaseTool)
                    item.Hue = GetRandomNecromancerHue();
            }

            Hue = 0x83E8;
        }

        public virtual void TurnToGargoyle()
        {
            ArrayList items = new ArrayList(this.Items);

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];

                if (item is BaseClothing || item is Hair || item is Beard)
                    item.Delete();
            }

            Body = 0x2F6;
            Hue = RandomBrightHue() | 0x8000;
            Name = NameList.RandomName("gargoyle vendor");

            CapitalizeTitle();
        }

        public virtual void CapitalizeTitle()
        {
            string title = this.Title;

            if (title == null)
                return;

            string[] split = title.Split(' ');

            for (int i = 0; i < split.Length; ++i)
            {
                if (Insensitive.Equals(split[i], "the"))
                    continue;

                if (split[i].Length > 1)
                    split[i] = Char.ToUpper(split[i][0]) + split[i].Substring(1);
                else if (split[i].Length > 0)
                    split[i] = Char.ToUpper(split[i][0]).ToString();
            }

            this.Title = String.Join(" ", split);
        }

        public virtual int GetHairHue()
        {
            return Utility.RandomHairHue();
        }

        /*
		private static int[] m_SandalHues = new int[]
			{
				0x489, 0x47F, 0x482,
				0x47E, 0x48F, 0x494,
				0x484, 0x497
			};

		private static Item CreateSandals(int type)
		{
			return new Sandals(m_SandalHues[Utility.Random(m_SandalHues.Length)]);
		}*/

        public override void InitOutfit()
        {
            WipeLayers();
            switch (Utility.Random(3))
            {
                case 0: AddItem(new FancyShirt(GetRandomHue())); break;
                case 1: AddItem(new Doublet(GetRandomHue())); break;
                case 2: AddItem(new Shirt(GetRandomHue())); break;
            }

            /* Publish 4
			 * Shopkeeper Changes
			 * NPC shopkeepers will no longer have colored sandals. Evil NPC Mages will carry these items.
			 * adam: I'm unsure of the hue, but sice this was likely the hue 'moved' to evil mage lords in publish 4, we will assume these are the hues used here
			 * http://forums.uosecondage.com/viewtopic.php?f=8&t=22266
			 * runuo.com/community/threads/evil-mage-hues.91540/
			 */
            int sandal_hue = (Core.UOAI || Core.UOAR || Core.UOMO || PublishInfo.Publish >= 4) ? GetShoeHue() : Utility.RandomBool() ? Utility.RandomRedHue() : Utility.RandomBlueHue();

            switch (ShoeType)
            {
                case VendorShoeType.Shoes: AddItem(new Shoes(GetShoeHue())); break;
                case VendorShoeType.Boots: AddItem(new Boots(GetShoeHue())); break;
                case VendorShoeType.Sandals: AddItem(new Sandals(sandal_hue)); break;
                case VendorShoeType.ThighBoots: AddItem(new ThighBoots(GetShoeHue())); break;
            }

            int hairHue = GetHairHue();

            if (Female)
            {
                switch (Utility.Random(6))
                {
                    case 0: AddItem(new ShortPants(GetRandomHue())); break;
                    case 1:
                    case 2: AddItem(new Kilt(GetRandomHue())); break;
                    case 3:
                    case 4:
                    case 5: AddItem(new Skirt(GetRandomHue())); break;
                }

                switch (Utility.Random(9))
                {
                    case 0: AddItem(new Afro(hairHue)); break;
                    case 1: AddItem(new KrisnaHair(hairHue)); break;
                    case 2: AddItem(new PageboyHair(hairHue)); break;
                    case 3: AddItem(new PonyTail(hairHue)); break;
                    case 4: AddItem(new ReceedingHair(hairHue)); break;
                    case 5: AddItem(new TwoPigTails(hairHue)); break;
                    case 6: AddItem(new ShortHair(hairHue)); break;
                    case 7: AddItem(new LongHair(hairHue)); break;
                    case 8: AddItem(new BunsHair(hairHue)); break;
                }
            }
            else
            {
                switch (Utility.Random(2))
                {
                    case 0: AddItem(new LongPants(GetRandomHue())); break;
                    case 1: AddItem(new ShortPants(GetRandomHue())); break;
                }

                switch (Utility.Random(8))
                {
                    case 0: AddItem(new Afro(hairHue)); break;
                    case 1: AddItem(new KrisnaHair(hairHue)); break;
                    case 2: AddItem(new PageboyHair(hairHue)); break;
                    case 3: AddItem(new PonyTail(hairHue)); break;
                    case 4: AddItem(new ReceedingHair(hairHue)); break;
                    case 5: AddItem(new TwoPigTails(hairHue)); break;
                    case 6: AddItem(new ShortHair(hairHue)); break;
                    case 7: AddItem(new LongHair(hairHue)); break;
                }

                switch (Utility.Random(5))
                {
                    case 0: AddItem(new LongBeard(hairHue)); break;
                    case 1: AddItem(new MediumLongBeard(hairHue)); break;
                    case 2: AddItem(new Vandyke(hairHue)); break;
                    case 3: AddItem(new Mustache(hairHue)); break;
                    case 4: AddItem(new Goatee(hairHue)); break;
                }
            }

            if (!Core.UOSP)
                InitLoot();
        }

        public override bool CheckPoisonImmunity(Mobile from, Poison poison)
        {
            // Publish 5
            // Shopkeepers may no longer be poisoned
            // http://www.uoguide.com/Publish_5
            if (!Core.UOAI && !Core.UOAR && !Core.UOMO && PublishInfo.Publish >= 5)
                return true;
            else
                return false;
        }

        public virtual void InitLoot()
        {
            PackGold(100, 200);
        }

        public virtual void Restock()
        {
            try
            {
                m_LastRestock = DateTime.Now;
                m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);

                IBuyItemInfo[] buyInfo = this.GetBuyInfo();

                foreach (IBuyItemInfo bii in buyInfo)
                    bii.OnRestock();
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Error with Restock.GetBuyInfo()");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }

        private static TimeSpan InventoryDecayTime = TimeSpan.FromHours(1.0);

        public virtual void VendorBuy(Mobile from)
        {
            try
            {
                // adam: add new sanity checks
                if (from == null || from.NetState == null)
                    return;

                if (!IsActiveSeller)
                    return;

                if (!from.CheckAlive())
                    return;

                if (!CheckVendorAccess(from))
                {
                    Say(501522); // I shall not treat with scum like thee!
                    return;
                }

                if (DateTime.Now - m_LastRestock > RestockDelay)
                    Restock();

                UpdateBuyInfo();

                int count = 0;
                ArrayList list;

                // Adam: Catch the exception
                IBuyItemInfo[] buyInfo = null;
                try
                {
                    buyInfo = this.GetBuyInfo();
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Error with GetBuyInfo() :: Please send output to Taran:");
                    System.Console.WriteLine(exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                    // if buyInfo is null, we can't continue
                    throw new ApplicationException("Error with GetBuyInfo()");
                }

                IShopSellInfo[] sellInfo = this.GetSellInfo();

                list = new ArrayList(buyInfo.Length);
                Container cont = this.BuyPack;

                ArrayList opls = new ArrayList();

                for (int idx = 0; idx < buyInfo.Length; idx++)
                {
                    IBuyItemInfo buyItem = (IBuyItemInfo)buyInfo[idx];

                    if (buyItem.Amount <= 0 || list.Count >= 250)
                        continue;

                    if (ResourcePool.IsPooledResource(buyItem.Type))
                    {
                        if (buyItem.Amount / 100 > 0)
                        {
                            Type bunchtype = ResourcePool.GetBunchType(buyItem.Type);
                            if (bunchtype != null)
                            {
                                int price = buyItem.BunchPrice;
                                int amount = (buyItem.Amount > 59900) ? 599 : buyItem.Amount / 100;
                                GenericBuyInfo gbi = new GenericBuyInfo(bunchtype, price, amount, buyItem.ItemID, 0);
                                IEntity disp = gbi.GetDisplayObject() as IEntity;
                                //plasma: Implement city tax here, but only if the pool is empty (this prevents tax being applied to BBS sales)
                                /*if (ResourcePool.GetTotalCount(buyItem.Type) == 0)
								{
									KinCityRegion kcr = KinCityRegion.GetKinCityAt(this);
									if (kcr != null && kcr.CityTaxRate > 0.0)
									{
										double tax = price * kcr.CityTaxRate;
										price += (int)Math.Floor(tax);
									}
								}*/
                                list.Add(new BuyItemState(gbi.Name, cont.Serial, disp == null ? (Serial)0x7FC0FFEE : disp.Serial, gbi.Price, gbi.Amount, gbi.ItemID, gbi.Hue));
                                count++;

                                opls.Add((disp as Item).PropertyList);
                            }
                        }

                        if (buyItem.Amount % 100 > 0)
                        {

                            GenericBuyInfo gbi = (GenericBuyInfo)buyItem;
                            IEntity disp = gbi.GetDisplayObject() as IEntity;

                            int price = buyItem.Price;
                            int amount = buyItem.Amount % 100;

                            //plasma: Implement city tax here, but only if the pool is empty (this prevents tax being applied to BBS sales)
                            /*if (ResourcePool.GetTotalCount(buyItem.Type) == 0)
							{
								KinCityRegion kcr = KinCityRegion.GetKinCityAt(this);
								if (kcr != null && kcr.CityTaxRate > 0.0)
								{
									double tax = price * kcr.CityTaxRate;
									price += (int)Math.Floor(tax);
								}
							}*/
                            list.Add(new BuyItemState(buyItem.Name, cont.Serial, disp == null ? (Serial)0x7FC0FFEE : disp.Serial, price, amount, buyItem.ItemID, buyItem.Hue));
                            count++;

                            if (disp is Item)
                                opls.Add((disp as Item).PropertyList);
                            else if (disp is Mobile)
                                opls.Add((disp as Mobile).PropertyList);
                        }

                    }
                    else
                    {
                        // NOTE: Only GBI supported; if you use another implementation of IBuyItemInfo, this will crash
                        GenericBuyInfo gbi = (GenericBuyInfo)buyItem;
                        IEntity disp = gbi.GetDisplayObject() as IEntity;
                        //plasma: Implement city tax here												
                        //KinCityRegion kcr = KinCityRegion.GetKinCityAt(this);
                        int price = gbi.Price;
                        /*if (kcr != null && kcr.CityTaxRate > 0.0)
						{
							double tax = buyItem.Price * kcr.CityTaxRate;
							price += (int)Math.Floor(tax);
						}*/
                        list.Add(new BuyItemState(buyItem.Name, cont.Serial, disp == null ? (Serial)0x7FC0FFEE : disp.Serial, price, buyItem.Amount, buyItem.ItemID, buyItem.Hue));
                        count++;

                        if (disp is Item)
                            opls.Add((disp as Item).PropertyList);
                        else if (disp is Mobile)
                            opls.Add((disp as Mobile).PropertyList);
                    }
                }

                ArrayList playerItems = cont.Items;

                for (int i = playerItems.Count - 1; i >= 0; --i)
                {
                    if (i >= playerItems.Count)
                        continue;

                    Item item = (Item)playerItems[i];

                    if ((item.LastMoved + InventoryDecayTime) <= DateTime.Now)
                        item.Delete();
                }

                for (int i = 0; i < playerItems.Count; ++i)
                {
                    Item item = (Item)playerItems[i];

                    int price = 0;
                    string name = null;

                    foreach (IShopSellInfo ssi in sellInfo)
                    {
                        if (ssi.IsSellable(item))
                        {
                            price = ssi.GetBuyPriceFor(item);
                            name = ssi.GetNameFor(item);
                            break;
                        }
                    }

                    if (name != null && list.Count < 250)
                    {

                        list.Add(new BuyItemState(name, cont.Serial, item.Serial, price, item.Amount, item.ItemID, item.Hue));
                        count++;

                        opls.Add(item.PropertyList);
                    }
                }

                //one (not all) of the packets uses a byte to describe number of items in the list.  Osi = dumb.
                //if ( list.Count > 255 )
                //	Console.WriteLine( "Vendor Warning: Vendor {0} has more than 255 buy items, may cause client errors!", this );

                if (list.Count > 0)
                {
                    list.Sort(new BuyItemStateComparer());

                    SendPacksTo(from);

                    if (from.NetState == null)
                        return;

                    if (from.NetState.IsPost6017)
                        from.Send(new VendorBuyContent6017(list));
                    else
                        from.Send(new VendorBuyContent(list));

                    from.Send(new VendorBuyList(this, list));
                    from.Send(new DisplayBuyList(this));
                    from.Send(new MobileStatusExtended(from));//make sure their gold amount is sent

                    for (int i = 0; i < opls.Count; ++i)
                        from.Send(opls[i] as Packet);

                    SayTo(from, 500186); // Greetings.  Have a look around.
                }
                else
                    SayTo(from, "I'm all out of stock. Please come back later."); // Added to deal with an empty buy list
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
        }

        public virtual void SendPacksTo(Mobile from)
        {
            Item pack = FindItemOnLayer(Layer.ShopBuy);

            if (pack == null)
            {
                pack = new Backpack();
                pack.Layer = Layer.ShopBuy;
                pack.Movable = false;
                pack.Visible = false;
                AddItem(pack);
            }

            from.Send(new EquipUpdate(pack));

            pack = FindItemOnLayer(Layer.ShopSell);

            if (pack != null)
                from.Send(new EquipUpdate(pack));

            pack = FindItemOnLayer(Layer.ShopResale);

            if (pack == null)
            {
                pack = new Backpack();
                pack.Layer = Layer.ShopResale;
                pack.Movable = false;
                pack.Visible = false;
                AddItem(pack);
            }

            from.Send(new EquipUpdate(pack));
        }

        public virtual void VendorSell(Mobile from)
        {
            if (!IsActiveBuyer)
                return;

            if (!from.CheckAlive())
                return;

            if (!CheckVendorAccess(from))
            {
                Say(501522); // I shall not treat with scum like thee!
                return;
            }

            Container pack = from.Backpack;

            if (pack != null)
            {
                IShopSellInfo[] info = GetSellInfo();

                Hashtable table = new Hashtable();

                foreach (IShopSellInfo ssi in info)
                {
                    Item[] items = pack.FindItemsByType(ssi.Types);

                    foreach (Item item in items)
                    {
                        if (item is Container && ((Container)item).Items.Count != 0)
                            continue;

                        if (item is CommodityDeed)
                        {
                            CommodityDeed cd = (CommodityDeed)item;
                            if (cd.Commodity == null)
                                continue;
                            if (ResourcePool.IsPooledResource(cd.Commodity.GetType()) && ssi.IsSellable(cd.Commodity))
                                table[item] = new SellItemState(item, (int)ResourcePool.GetWholesalePrice(cd.Commodity.GetType()), "Commodity Deed");
                        }
                        else if (item.IsStandardLoot() && item.Movable && ssi.IsSellable(item))
                        {
                            if (ResourcePool.IsPooledResource(item.GetType()))
                                table[item] = new SellItemState(item, (int)ResourcePool.GetWholesalePrice(item.GetType()), ResourcePool.GetName(item.GetType()));
                            else
                                table[item] = new SellItemState(item, ssi.GetSellPriceFor(item), ssi.GetNameFor(item));
                        }
                    }
                }

                if (table.Count > 0)
                {
                    SendPacksTo(from);

                    from.Send(new VendorSellList(this, table));
                }
                else
                {
                    Say(true, "You have nothing I would be interested in.");
                }
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            /*
			if (dropped is SmallBOD || dropped is LargeBOD)
			{
				if (!IsValidBulkOrder(dropped) || !SupportsBulkOrders(from))
				{
					SayTo(from, 1045130); // That order is for some other shopkeeper.
					return false;
				}
				else if ((dropped is SmallBOD && !((SmallBOD)dropped).Complete) || (dropped is LargeBOD && !((LargeBOD)dropped).Complete))
				{
					SayTo(from, 1045131); // You have not completed the order yet.
					return false;
				}

				Item reward;
				int gold, fame;

				if (dropped is SmallBOD)
					((SmallBOD)dropped).GetRewards(out reward, out gold, out fame);
				else
					((LargeBOD)dropped).GetRewards(out reward, out gold, out fame);

				from.SendSound(0x3D);

				SayTo(from, 1045132); // Thank you so much!  Here is a reward for your effort.

				if (reward != null)
					from.AddToBackpack(reward);

				if (gold > 1000)
					from.AddToBackpack(new BankCheck(gold));
				else if (gold > 0)
					from.AddToBackpack(new Gold(gold));

				Titles.AwardFame(from, fame, true);

				dropped.Delete();
				return true;
			}*/

            return base.OnDragDrop(from, dropped);
        }

        private GenericBuyInfo LookupDisplayObject(object obj)
        {
            return LookupDisplayObject(obj, true);
        }

        private GenericBuyInfo LookupDisplayObject(object obj, bool bReturnBunchItem)
        {
            try
            {
                IBuyItemInfo[] buyInfo = this.GetBuyInfo();

                for (int i = 0; i < buyInfo.Length; ++i)
                {
                    GenericBuyInfo gbi = buyInfo[i] as GenericBuyInfo;

                    if (gbi.GetDisplayObject() == obj)
                    {
                        return gbi;
                    }
                    else if (ResourcePool.GetBunchType(gbi.Type) == obj.GetType())
                    {
                        if (bReturnBunchItem)
                        {
                            int price = gbi.BunchPrice;
                            int amount = (gbi.Amount > 59900) ? 599 : gbi.Amount / 100;
                            gbi = new GenericBuyInfo(obj.GetType(), price, amount, gbi.ItemID, 0);
                        }

                        return gbi;
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Error with LookupDisplayObject.GetBuyInfo()");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

            return null;
        }

        bool ProcessSinglePurchase(BuyItemResponse buy, IBuyItemInfo bii, ArrayList validBuy, ref int controlSlots, ref bool fullPurchase, ref int totalCost)
        {
            int amount = buy.Amount;

            if (amount > bii.Amount)
                amount = bii.Amount;

            if (amount <= 0)
                return false;

            int slots = bii.ControlSlots * amount;

            if (controlSlots >= slots)
            {
                controlSlots -= slots;
            }
            else
            {
                fullPurchase = false;
                return false;
            }

            totalCost += bii.Price * amount;
            validBuy.Add(buy);
            return true;
        }

        private void ProcessValidPurchase(int amount, IBuyItemInfo bii, Mobile buyer, Container cont)
        {
            if (amount > bii.Amount)
                amount = bii.Amount;

            if (amount < 1)
                return;

            if (ResourcePool.IsPooledResource(bii.Type) && ResourcePool.GetTotalCount(bii.Type) > 0)
            {
                ResourcePool.SellOff(bii.Type, amount, this.Serial, buyer);
            }
            else
            {
                bii.Amount -= amount;
            }

            object o = bii.GetObject();

            if (o is Item)
            {
                Item item = (Item)o;

                // all weapons and armor are exceptional during server wars
                if (Server.Misc.TestCenter.Enabled == true)
                {
                    if (item is BaseArmor)
                    {
                        (item as BaseArmor).Quality = ArmorQuality.Exceptional;
                        (item as BaseArmor).Durability = ArmorDurabilityLevel.Fortified;
                        (item as BaseArmor).Identified = true;
                    }
                    if (item is BaseWeapon)
                    {
                        (item as BaseWeapon).Quality = WeaponQuality.Exceptional;
                        (item as BaseWeapon).DurabilityLevel = WeaponDurabilityLevel.Fortified;
                        (item as BaseWeapon).Identified = true;
                    }
                }

                // all items bought in a store (from an NPC) only yield 1 ingot when smelted
                (o as Item).StoreBought = true;

                if (item.Stackable)
                {
                    item.Amount = amount;

                    if (ResourcePool.IsPooledResource(item.GetType()) && item.Amount >= 100)
                        item = new CommodityDeed(item);

                    if (cont == null || !cont.TryDropItem(buyer, item, false))
                        item.MoveToWorld(buyer.Location, buyer.Map);
                }
                else
                {
                    item.Amount = 1;

                    if (cont == null || !cont.TryDropItem(buyer, item, false))
                        item.MoveToWorld(buyer.Location, buyer.Map);

                    for (int i = 1; i < amount; i++)
                    {
                        item = bii.GetObject() as Item;

                        if (item != null)
                        {
                            item.Amount = 1;

                            if (cont == null || !cont.TryDropItem(buyer, item, false))
                                item.MoveToWorld(buyer.Location, buyer.Map);
                        }
                    }
                }
            }
            else if (o is Mobile)
            {
                Mobile m = (Mobile)o;

                m.Direction = (Direction)Utility.Random(8);
                m.MoveToWorld(buyer.Location, buyer.Map);
                m.PlaySound(m.GetIdleSound());

                if (m is BaseCreature)
                    ((BaseCreature)m).SetControlMaster(buyer);

                for (int i = 1; i < amount; ++i)
                {
                    m = bii.GetObject() as Mobile;

                    if (m != null)
                    {
                        m.Direction = (Direction)Utility.Random(8);
                        m.MoveToWorld(buyer.Location, buyer.Map);

                        if (m is BaseCreature)
                            ((BaseCreature)m).SetControlMaster(buyer);
                    }
                }
            }
        }

        public virtual bool OnBuyItems(Mobile buyer, ArrayList list)
        {
            // adam: additional Hardening.
            try
            {
                // adam: add new sanity checks
                if (buyer == null || buyer.NetState == null || list == null)
                    return false;

                if (!IsActiveSeller)
                    return false;

                if (!buyer.CheckAlive())
                    return false;

                // added back for factions vendor checks
                if (!CheckVendorAccess(buyer))
                {
                    Say(501522); // I shall not treat with scum like thee!
                    return false;
                }

                UpdateBuyInfo();

                IBuyItemInfo[] buyInfo = this.GetBuyInfo();
                IShopSellInfo[] info = GetSellInfo();
                int totalCost = 0;
                ArrayList validBuy = new ArrayList(list.Count);
                Container cont;
                bool bought = false;
                bool fromBank = false;
                bool fullPurchase = true;
                int controlSlots = buyer.FollowersMax - buyer.Followers;
                int totalTax = 0;
                //KinCityRegion kcr = KinCityRegion.GetKinCityAt(this);

                foreach (BuyItemResponse buy in list)
                {
                    Serial ser = buy.Serial;
                    int amount = buy.Amount;

                    if (ser.IsItem)
                    {
                        Item item = World.FindItem(ser);

                        if (item == null)
                            continue;

                        GenericBuyInfo gbi = LookupDisplayObject(item);

                        if (gbi != null)
                        {
                            if (ProcessSinglePurchase(buy, gbi, validBuy, ref controlSlots, ref fullPurchase, ref totalCost))
                            {
                                //plasma: after processing the single purchase, apply tax to each item if this isn't a bbs sale
                                int price = gbi.Price;
                                //plasma: Implement city tax here, but only if the pool is empty (this prevents tax being applied to BBS sales)
                                if (!ResourcePool.IsPooledResource(gbi.Type) || (ResourcePool.IsPooledResource(gbi.Type) && ResourcePool.GetTotalCount(gbi.Type) == 0))
                                {
                                    /*if (kcr != null && kcr.CityTaxRate > 0.0)
									{
										double tax = gbi.Price * kcr.CityTaxRate;
										tax = Math.Floor(tax);
										tax = tax * buy.Amount;
										totalCost += (int)tax;
										totalTax += (int)tax;
									}*/
                                }
                            }

                        }
                        else if (item.RootParent == this)
                        {
                            if (amount > item.Amount)
                                amount = item.Amount;

                            if (amount <= 0)
                                continue;

                            foreach (IShopSellInfo ssi in info)
                            {
                                if (ssi.IsSellable(item))
                                {
                                    if (ssi.IsResellable(item))
                                    {
                                        totalCost += ssi.GetBuyPriceFor(item) * amount;
                                        validBuy.Add(buy);
                                        break;
                                    }
                                }
                            }

                        }
                    }
                    else if (ser.IsMobile)
                    {
                        Mobile mob = World.FindMobile(ser);

                        if (mob == null)
                            continue;


                        if (mob is BaseCreature)
                        {

                            double chance = ((BaseCreature)mob).GetControlChance(buyer);
                            if (chance <= 0.50) //require 50% control or better
                            {
                                SayTo(buyer, true, "You don't look like you would make a fitting owner for this fine animal.");
                                return false;
                            }

                        }

                        GenericBuyInfo gbi = LookupDisplayObject(mob);

                        if (gbi != null)
                        {
                            if (ProcessSinglePurchase(buy, gbi, validBuy, ref controlSlots, ref fullPurchase, ref totalCost))
                            {
                                //plasma: after processing the single purchase, apply tax to each item
                                int price = gbi.Price;
                                //plasma: Implement city tax here, but only if the pool is empty (this prevents tax being applied to BBS sales)
                                /*if (!ResourcePool.IsPooledResource(gbi.Type) || (ResourcePool.IsPooledResource(gbi.Type) && ResourcePool.GetTotalCount(gbi.Type) == 0))
								{
									if (kcr != null && kcr.CityTaxRate > 0.0)
									{
										double tax = gbi.Price * kcr.CityTaxRate;
										tax = Math.Floor(tax);
										tax = tax * gbi.Amount;
										totalCost += (int)tax;
										totalTax += (int)tax;
									}
								}*/
                            }
                        }
                    }
                }//foreach

                if (fullPurchase && validBuy.Count == 0)
                {
                    SayTo(buyer, 500190); // Thou hast bought nothing!
                }
                else if (validBuy.Count == 0)
                {
                    SayTo(buyer, 500187); // Your order cannot be fulfilled, please try again.
                }

                if (validBuy.Count == 0)
                {
                    return false;
                }

                bought = (buyer.AccessLevel >= AccessLevel.Administrator);//Pix: I decided to bump this up to Admin... staff shouldn't be buying things anyways

                cont = buyer.Backpack;
                if (!bought && cont != null)
                {
                    if (cont.ConsumeTotal(typeof(Gold), totalCost))
                    {
                        bought = true;
                    }
                    else if (totalCost < 2000)
                    {
                        SayTo(buyer, 500192);//Begging thy pardon, but thou casnt afford that.
                    }
                }

                if (!bought && totalCost >= 2000)
                {
                    cont = buyer.BankBox;
                    if (cont != null && cont.ConsumeTotal(typeof(Gold), totalCost))
                    {
                        bought = true;
                        fromBank = true;
                    }
                    else
                    {
                        SayTo(buyer, 500191); //Begging thy pardon, but thy bank account lacks these funds.
                    }
                }

                if (!bought)
                {
                    return false;
                }
                else
                {
                    buyer.PlaySound(0x32);
                }

                cont = buyer.Backpack;
                if (cont == null)
                {
                    cont = buyer.BankBox;
                }

                foreach (BuyItemResponse buy in validBuy)
                {
                    Serial ser = buy.Serial;
                    int amount = buy.Amount;

                    if (amount < 1)
                        continue;

                    if (ser.IsItem)
                    {
                        Item item = World.FindItem(ser);

                        if (item == null)
                            continue;

                        GenericBuyInfo gbi = LookupDisplayObject(item, false);

                        if (item.GetType() != gbi.Type)
                        {
                            if (ResourcePool.GetBunchType(gbi.Type) == item.GetType())
                            {
                                amount *= 100;
                            }
                        }

                        if (gbi != null)
                        {
                            ProcessValidPurchase(amount, gbi, buyer, cont);
                        }
                        else
                        {
                            if (amount > item.Amount)
                                amount = item.Amount;

                            foreach (IShopSellInfo ssi in info)
                            {
                                if (ssi.IsSellable(item))
                                {
                                    if (ssi.IsResellable(item))
                                    {
                                        Item buyItem;
                                        if (amount >= item.Amount)
                                        {
                                            buyItem = item;
                                        }
                                        else
                                        {
                                            buyItem = item.Dupe(amount);
                                            item.Amount -= amount;
                                        }

                                        if (cont == null || !cont.TryDropItem(buyer, buyItem, false))
                                            buyItem.MoveToWorld(buyer.Location, buyer.Map);

                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (ser.IsMobile)
                    {
                        Mobile mob = World.FindMobile(ser);

                        if (mob == null)
                            continue;

                        GenericBuyInfo gbi = LookupDisplayObject(mob);

                        if (gbi != null)
                            ProcessValidPurchase(amount, gbi, buyer, cont);
                    }

                    /*if ( ser >= 0 && ser <= buyInfo.Length )
					{
							IBuyItemInfo bii = buyInfo[ser];

					}
					else
					{
							Item item = World.FindItem( buy.Serial );

							if ( item == null )
									continue;

							if ( amount > item.Amount )
									amount = item.Amount;

							foreach( IShopSellInfo ssi in info )
							{
									if ( ssi.IsSellable( item ) )
									{
											if ( ssi.IsResellable( item ) )
											{
													Item buyItem;
													if ( amount >= item.Amount )
													{
															buyItem = item;
													}
													else
													{
															buyItem = item.Dupe( amount );
															item.Amount -= amount;
													}

													if ( cont == null || !cont.TryDropItem( buyer, buyItem, false ) )
															buyItem.MoveToWorld( buyer.Location, buyer.Map );

													break;
											}
									}
							}
					}*/
                }//foreach

                if (fullPurchase)
                {
                    if (buyer.AccessLevel >= AccessLevel.Administrator)//Pix: I decided to bump this up to Admin... staff shouldn't be buying things anyways
                        SayTo(buyer, true, "I would not presume to charge thee anything.  Here are the goods you requested.");
                    else if (fromBank)
                        SayTo(buyer, true, "The total of thy purchase is {0} gold, which has been withdrawn from your bank account.  My thanks for the patronage.", totalCost);
                    else
                        SayTo(buyer, true, "The total of thy purchase is {0} gold.  My thanks for the patronage.", totalCost);
                }
                else
                {
                    if (buyer.AccessLevel >= AccessLevel.Administrator)//Pix: I decided to bump this up to Admin... staff shouldn't be buying things anyways
                        SayTo(buyer, true, "I would not presume to charge thee anything.  Unfortunately, I could not sell you all the goods you requested.");
                    else if (fromBank)
                        SayTo(buyer, true, "The total of thy purchase is {0} gold, which has been withdrawn from your bank account.  My thanks for the patronage.  Unfortunately, I could not sell you all the goods you requested.", totalCost);
                    else
                        SayTo(buyer, true, "The total of thy purchase is {0} gold.  My thanks for the patronage.  Unfortunately, I could not sell you all the goods you requested.", totalCost);
                }
                //plasma:  Process the sale here if this is a faction region
                //if (kcr != null) kcr.OnVendorBuy(buyer, totalTax);
                return true;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return false;
        }

        public virtual bool CheckVendorAccess(Mobile from)
        {
            //GuardedRegion reg = (GuardedRegion)this.Region.GetRegion(typeof(GuardedRegion));
            GuardedRegion reg = this.Region as GuardedRegion;

            if (reg != null && !reg.CheckVendorAccess(this, from))
                return false;

            if (this.Region != from.Region)
            {
                //reg = (GuardedRegion)from.Region.GetRegion(typeof(GuardedRegion));
                reg = from.Region as GuardedRegion;

                if (reg != null && !reg.CheckVendorAccess(this, from))
                    return false;
            }

            return true;
        }

        public virtual bool OnSellItems(Mobile seller, ArrayList list)
        {
            // adam: additional Hardening.
            try
            {
                // adam: add new sanity checks
                if (seller == null || seller.NetState == null || list == null)
                    return false;

                if (!IsActiveBuyer)
                    return false;

                if (!seller.CheckAlive())
                    return false;

                // adam: added back to support factions
                if (!CheckVendorAccess(seller))
                {
                    Say(501522); // I shall not treat with scum like thee!
                    return false;
                }

                seller.PlaySound(0x32);

                IShopSellInfo[] info = GetSellInfo();
                IBuyItemInfo[] buyInfo = this.GetBuyInfo();
                int GiveGold = 0;
                int Sold = 0;
                Container cont;
                ArrayList delete = new ArrayList();
                ArrayList drop = new ArrayList();

                foreach (SellItemResponse resp in list)
                {
                    if (resp.Item.RootParent != seller || resp.Amount <= 0)
                        continue;

                    foreach (IShopSellInfo ssi in info)
                    {
                        if (ssi.IsSellable(resp.Item))
                        {
                            Sold++;
                            break;
                        }
                    }
                }

                if (Sold > MaxSell)
                {
                    SayTo(seller, true, "You may only sell {0} items at a time!", MaxSell);
                    return false;
                }
                else if (Sold == 0)
                {
                    return true;
                }

                foreach (SellItemResponse resp in list)
                {
                    if (resp.Item.RootParent != seller || resp.Amount <= 0)
                        continue;

                    foreach (IShopSellInfo ssi in info)
                    {
                        if (ssi.IsSellable(resp.Item))
                        {
                            int amount = resp.Amount;

                            if (amount > resp.Item.Amount)
                                amount = resp.Item.Amount;

                            if (ResourcePool.IsPooledResource(resp.Item.GetType()))
                            {
                                double price = ResourcePool.AddToPool(seller, resp.Item.GetType(), amount, true, this.Serial);
                                resp.Item.Consume(amount);
                                SayTo(seller, true, "Thank you. I will sell these for you at {0} gp each, minus my commission.", price);
                                if ((int)price != price)
                                    SayTo(seller, true, "I can give you a slightly better price if you sell in bulk with commodity deeds.");
                                break;
                            }
                            if (resp.Item is CommodityDeed && ResourcePool.IsPooledResource(((CommodityDeed)resp.Item).Commodity.GetType()))
                            {
                                double price = ResourcePool.AddToPool(seller, ((CommodityDeed)resp.Item).Commodity.GetType(), ((CommodityDeed)resp.Item).Commodity.Amount, false, this.Serial);
                                resp.Item.Delete();
                                SayTo(seller, true, "Thank you. I will sell these for you at {0} gp each, minus my commission.", price);
                                break;
                            }

                            if (ssi.IsResellable(resp.Item))
                            {
                                bool found = false;

                                foreach (IBuyItemInfo bii in buyInfo)
                                {
                                    if (bii.Restock(resp.Item, amount))
                                    {
                                        resp.Item.Consume(amount);
                                        found = true;

                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    cont = this.BuyPack;

                                    if (amount < resp.Item.Amount)
                                    {
                                        resp.Item.Amount -= amount;
                                        Item item = resp.Item.Dupe(amount);
                                        item.SetLastMoved();
                                        cont.DropItem(item);
                                    }
                                    else
                                    {
                                        resp.Item.SetLastMoved();
                                        cont.DropItem(resp.Item);
                                    }
                                }
                            }
                            else
                            {
                                if (amount < resp.Item.Amount)
                                    resp.Item.Amount -= amount;
                                else
                                    resp.Item.Delete();
                            }

                            GiveGold += ssi.GetSellPriceFor(resp.Item) * amount;
                            break;
                        }
                    }
                }

                if (GiveGold > 0)
                {
                    while (GiveGold > 60000)
                    {
                        seller.AddToBackpack(new Gold(60000));
                        GiveGold -= 60000;
                    }

                    seller.AddToBackpack(new Gold(GiveGold));

                    seller.PlaySound(0x0037);//Gold dropping sound

                    if (SupportsBulkOrders(seller))
                    {
                        Item bulkOrder = CreateBulkOrder(seller, false);

                        if (bulkOrder is LargeBOD)
                            seller.SendGump(new LargeBODAcceptGump(seller, (LargeBOD)bulkOrder));
                        else if (bulkOrder is SmallBOD)
                            seller.SendGump(new SmallBODAcceptGump(seller, (SmallBOD)bulkOrder));
                    }
                }
                //no cliloc for this?
                //SayTo( seller, true, "Thank you! I bought {0} item{1}. Here is your {2}gp.", Sold, (Sold > 1 ? "s" : ""), GiveGold );

                return true;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_Invulnerable);
            ArrayList sbInfos = this.SBInfos;

            for (int i = 0; sbInfos != null && i < sbInfos.Count; ++i)
            {
                SBInfo sbInfo = (SBInfo)sbInfos[i];
                ArrayList buyInfo = sbInfo.BuyInfo;

                for (int j = 0; buyInfo != null && j < buyInfo.Count; ++j)
                {
                    GenericBuyInfo gbi = (GenericBuyInfo)buyInfo[j];

                    int maxAmount = gbi.MaxAmount;
                    int doubled = 0;

                    switch (maxAmount)
                    {
                        case 40: doubled = 1; break;
                        case 80: doubled = 2; break;
                        case 160: doubled = 3; break;
                        case 320: doubled = 4; break;
                        case 640: doubled = 5; break;
                        case 999: doubled = 6; break;
                    }

                    if (doubled > 0)
                    {
                        writer.WriteEncodedInt(1 + ((j * sbInfos.Count) + i));
                        writer.WriteEncodedInt(doubled);
                    }
                }
            }

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            LoadSBInfo();

            ArrayList sbInfos = this.SBInfos;

            switch (version)
            {
                case 2:
                    {
                        m_Invulnerable = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        int index;

                        while ((index = reader.ReadEncodedInt()) > 0)
                        {
                            int doubled = reader.ReadEncodedInt();

                            if (sbInfos != null)
                            {
                                index -= 1;
                                int sbInfoIndex = index % sbInfos.Count;
                                int buyInfoIndex = index / sbInfos.Count;

                                if (sbInfoIndex >= 0 && sbInfoIndex < sbInfos.Count)
                                {
                                    SBInfo sbInfo = (SBInfo)sbInfos[sbInfoIndex];
                                    ArrayList buyInfo = sbInfo.BuyInfo;

                                    if (buyInfo != null && buyInfoIndex >= 0 && buyInfoIndex < buyInfo.Count)
                                    {
                                        GenericBuyInfo gbi = (GenericBuyInfo)buyInfo[buyInfoIndex];

                                        int amount = 20;

                                        switch (doubled)
                                        {
                                            case 1: amount = 40; break;
                                            case 2: amount = 80; break;
                                            case 3: amount = 160; break;
                                            case 4: amount = 320; break;
                                            case 5: amount = 640; break;
                                            case 6: amount = 999; break;
                                        }

                                        gbi.Amount = gbi.MaxAmount = amount;
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            NameHue = CalcInvulNameHue();

            CheckMorph();
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            if (from.Alive && IsActiveVendor)
            {
                if (IsActiveSeller)
                    list.Add(new VendorBuyEntry(from, this));

                if (IsActiveBuyer)
                    list.Add(new VendorSellEntry(from, this));

                if (SupportsBulkOrders(from))
                    list.Add(new BulkOrderInfoEntry(from, this));
            }

            base.AddCustomContextEntries(from, list);
        }

        public virtual IShopSellInfo[] GetSellInfo()
        {
            return (IShopSellInfo[])m_ArmorSellInfo.ToArray(typeof(IShopSellInfo));
        }

        public virtual IBuyItemInfo[] GetBuyInfo()
        {
            return (IBuyItemInfo[])m_ArmorBuyInfo.ToArray(typeof(IBuyItemInfo));
        }

        public override bool CanBeDamaged()
        {
            return !IsInvulnerable;
        }

        public override void OnDeath(Container c)
        {
            /*
			 * Publish 4
			 * Shopkeeper Changes
			 * If a shopkeeper is killed, a new shopkeeper will appear as soon as another player (other than the one that killed it) approaches.
			 * http://www.uoguide.com/Publish_4
			 */
            if (!Core.UOAI && !Core.UOAR && !Core.UOMO && PublishInfo.Publish >= 4)
            {
                if (Spawner != null && !Spawner.Deleted && Spawner.Running)
                {
                    foreach (AggressorInfo ai in this.Aggressors)
                    {
                        if (ai.Attacker.Player && ai.CanReportMurder && !ai.InitialAggressionInNoCountZone)
                        {   // no need to remember longer than the next spawn time
                            Spawner.BadGuys.Remember(ai.Attacker, Spawner.NextSpawn.TotalSeconds);
                        }
                    }

                }
            }

            /* Publish 4
			 * Shopkeeper Changes
			 * NPC shopkeepers will give a murder count when they die unless they are criminal or evil. The issue with murder counts from NPCs not decaying (as reported on Siege Perilous) will also be addressed.
			 * http://www.uoguide.com/Publish_4
			 */
            if (!Core.UOAI && !Core.UOAR && !Core.UOMO && PublishInfo.Publish >= 4)
            {
                foreach (AggressorInfo ai in this.Aggressors)
                {
                    if (ai.Attacker.Player && ai.CanReportMurder && !ai.InitialAggressionInNoCountZone)
                    {
                        Mobile killer = ai.Attacker;
                        if (killer != null && !killer.Deleted)
                        {
                            killer.LongTermMurders++;
                            killer.ShortTermMurders++;

                            if (killer is PlayerMobile)
                                ((PlayerMobile)killer).ResetKillTime();

                            killer.SendLocalizedMessage(1049067);//You have been reported for murder!

                            if (killer.LongTermMurders == 5)
                                killer.SendLocalizedMessage(502134);//You are now known as a murderer!
                            else if (SkillHandlers.Stealing.SuspendOnMurder && killer.LongTermMurders == 1 && killer is PlayerMobile && ((PlayerMobile)killer).NpcGuild == NpcGuild.ThievesGuild)
                                killer.SendLocalizedMessage(501562); // You have been suspended by the Thieves Guild.

                            killer.OnReportedForMurder(this);
                        }
                    }
                }
            }

            base.OnDeath(c);
        }

        public override void OnActionCombat()
        {
            if (this.AI == AIType.AI_Vendor)
            {
                // save our pissed-off state
                bool warmode = this.Warmode;
                Mobile combatant = this.Combatant;
                Mobile focusMob = this.FocusMob;

                if (this.Combatant != null)
                {   // close open inventory lists
                    this.Combatant.NetState.Send(new EndVendorBuy(this));
                }

                this.AI = AIType.AI_Melee;
                if (AIObject != null)
                {   // creating a new AI will reset a bunch of state, so we need to restore it
                    AIObject.Action = ActionType.Combat;
                    this.Warmode = warmode;
                    this.Combatant = combatant;
                    this.FocusMob = focusMob;
                    this.CurrentSpeed = this.ActiveSpeed;
                    this.FightMode = FightMode.All | FightMode.Closest;
                }
                this.DebugSay("Changing from AI_Vendor to AI_Melee");
            }

            base.OnActionCombat();
        }


        public override void OnActionFlee()
        {
            if (this.Combatant != null)
            {   // close open inventory lists
                this.Combatant.NetState.Send(new EndVendorBuy(this));
            }

            base.OnActionFlee();
        }

        /*public override void OnActionFlee()
		{
			base.OnActionFlee();
		}*/

        public override void OnActionWander()
        {
            if (this.AI == AIType.AI_Melee)
            {
                this.AI = AIType.AI_Vendor;         // just a vendor again
                this.FightMode = FightMode.None;    // revert to passive
                this.Aggressed.Clear();             // because we do not AcquireFocusMob in AI_Vendor AI, we fail to reset the Combatant 
                this.Aggressors.Clear();            //	in mobile.AggressiveAction(). Rather that handle this special case, we can force the issue
                                                    //	by simply clearing the vendor's agressors list
                this.DebugSay("Changing from AI_Melee to AI_Vendor");
            }

            base.OnActionWander();
        }
    }
}

namespace Server.ContextMenus
{
    public class VendorBuyEntry : ContextMenuEntry
    {
        private BaseVendor m_Vendor;

        public VendorBuyEntry(Mobile from, BaseVendor vendor)
            : base(6103, 8)
        {
            m_Vendor = vendor;
            Enabled = vendor.CheckVendorAccess(from);
        }

        public override void OnClick()
        {
            m_Vendor.VendorBuy(this.Owner.From);
        }
    }

    public class VendorSellEntry : ContextMenuEntry
    {
        private BaseVendor m_Vendor;

        public VendorSellEntry(Mobile from, BaseVendor vendor)
            : base(6104, 8)
        {
            m_Vendor = vendor;
            Enabled = vendor.CheckVendorAccess(from);
        }

        public override void OnClick()
        {
            m_Vendor.VendorSell(this.Owner.From);
        }
    }
}

namespace Server
{
    public interface IShopSellInfo
    {
        //get display name for an item
        string GetNameFor(Item item);

        //get price for an item which the player is selling
        int GetSellPriceFor(Item item);

        //get price for an item which the player is buying
        int GetBuyPriceFor(Item item);

        //can we sell this item to this vendor?
        bool IsSellable(Item item);

        //What do we sell?
        Type[] Types { get; }

        //does the vendor resell this item?
        bool IsResellable(Item item);
    }

    public interface IBuyItemInfo
    {
        //get a new instance of an object (we just bought it)
        object GetObject();

        int ControlSlots { get; }

        int PriceScalar { get; set; }

        //display price of the item
        int Price { get; }

        //display name of the item
        string Name { get; }

        //display hue
        int Hue { get; }

        //display id
        int ItemID { get; }

        //amount in stock
        int Amount { get; set; }

        //max amount in stock
        int MaxAmount { get; }

        int BunchPrice { get; }

        string BunchName { get; }

        Type Type { get; }

        //Attempt to restock with item, (return true if restock sucessful)
        bool Restock(Item item, int amount);

        //called when its time for the whole shop to restock
        void OnRestock();
        // void OnRestockReagents();
    }
}
