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

/* Scripts/Mobiles/Vendors/NPC/RealEstateBroker.cs
 *  Changelog:
 *	9/1/07, Adam
 *		Make ComputePriceFor(HouseDeed deed) static so we can access it publicly
 *	8/11/07, Adam
 *		- Replace 10000000 with PriceError constant
 *		- add assert for invalid price
 *	08/06/2007, plasma
 *		- Initial changelog creation
 *		- Allow StaticDeeds to be sold back!
 */

using Server.Items;
using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class RealEstateBroker : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public RealEstateBroker()
            : base("the real estate broker")
        {
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.Alive && from.InRange(this, 3))
                return true;

            return base.HandlesOnSpeech(from);
        }

        private DateTime m_NextCheckPack;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (DateTime.UtcNow > m_NextCheckPack && InRange(m, 4) && !InRange(oldLocation, 4) && m.Player)
            {
                Container pack = m.Backpack;

                if (pack != null)
                {
                    m_NextCheckPack = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);

                    Item deed = pack.FindItemByType(typeof(HouseDeed), false);

                    if (deed != null)
                    {
                        // If you have a deed, I can appraise it or buy it from you...
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500605, m.NetState);

                        // Simply hand me a deed to sell it.
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500606, m.NetState);
                    }
                }
            }

            base.OnMovement(m, oldLocation);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.Mobile.Alive && e.HasKeyword(0x38)) // *appraise*
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 500608); // Which deed would you like appraised?
                e.Mobile.BeginTarget(12, false, TargetFlags.None, new TargetCallback(Appraise_OnTarget));
                e.Handled = true;
            }

            base.OnSpeech(e);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is HouseDeed)
            {
                HouseDeed deed = (HouseDeed)dropped;
                int price = ComputePriceFor(deed);

                if (price > 0)
                {
                    if (Banker.Deposit(from, price))
                    {
                        // For the deed I have placed gold in your bankbox : 
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1008000, AffixType.Append, price.ToString(), "");

                        deed.Delete();
                        return true;
                    }
                    else
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 500390); // Your bank box is full.
                        return false;
                    }
                }
                else
                {
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 500607); // I'm not interested in that.
                    return false;
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        public void Appraise_OnTarget(Mobile from, object obj)
        {
            if (obj is HouseDeed)
            {
                HouseDeed deed = (HouseDeed)obj;
                int price = ComputePriceFor(deed);

                if (price > 0)
                {
                    // I will pay you gold for this deed : 
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 1008001, AffixType.Append, price.ToString(), "");

                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 500610); // Simply hand me the deed if you wish to sell it.
                }
                else
                {
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 500607); // I'm not interested in that.
                }
            }
            else
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 500609); // I can't appraise things I know nothing about...
            }
        }

        public static int ComputePriceFor(HouseDeed deed)
        {
            int price = 0;

            /*if (deed is SmallBrickHouseDeed || deed is StonePlasterHouseDeed || deed is FieldStoneHouseDeed || deed is SmallBrickHouseDeed || deed is WoodHouseDeed || deed is WoodPlasterHouseDeed || deed is ThatchedRoofCottageDeed)
			else if (deed is BrickHouseDeed)
				price = 144500;
			else if (deed is TwoStoryWoodPlasterHouseDeed || deed is TwoStoryStonePlasterHouseDeed)
				price = 192400;
			else if (deed is TowerDeed)
				price = 433200;
			else if (deed is KeepDeed)
				price = 665200;
			else if (deed is CastleDeed)
				price = 1022800;
			else if (deed is LargePatioDeed)
				price = 152800;
			else if (deed is LargeMarbleDeed)
				price = 192800;
			else if (deed is SmallTowerDeed)
				price = 88500;
			else if (deed is LogCabinDeed)
				price = 97800;
			else if (deed is SandstonePatioDeed)
				price = 90900;
			else if (deed is VillaDeed)
				price = 136500;
			else if (deed is StoneWorkshopDeed)
				price = 60600;
			else if (deed is MarbleWorkshopDeed)
				price = 60300;
			else if (deed is StaticDeed) //pla: Allow our static houses to be sold back based on price in blueprint
			{
				price = StaticHouseHelper.GetPrice(((StaticDeed)deed).HouseID);

				//check for the failsafe price and if so set to 0 - dont want someone getting 8 million back!
				if (price == (int)StaticHouseHelper.Error.PriceError)
					price = 0;

				// track the error
				Misc.Diagnostics.Assert(price != 0, "price == 0");
			} */

            if (deed is HouseDeed)
                price = deed.Price;

            //check for the failsafe price and if so set to 0 - dont want someone getting 8 million back!
            if (price == (int)StaticHouseHelper.Error.PriceError)
                price = 0;

            // track the error
            Misc.Diagnostics.Assert(price != 0, "price == 0");

            // don't allow repurchase (or sale) of customs on siege. Before we can buy them back, we need to markup the sale price and that's not been done 
            // on the player vendors in the special	housing area. (so they will make 300% profit!)
            if (Core.RuleSets.SiegeRules() && !Misc.Diagnostics.Assert(deed is StaticDeed == false, "deed is StaticDeed for sale on siege!"))
                price = 0;

            // Publish 11
            // o) NPC real estate brokers will now buy house deeds back at 20% below the base price of the deed, or the original
            //	price paid when the deed was purchased from a vendor (whichever is lower).
            // o) House deed prices when reselling to real estate agents raised to the correct level on Siege Perilous
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.RenaissanceRules() && !Core.RuleSets.MortalisRules() && PublishInfo.Publish < 11)
                return GenericBuyInfo.ComputeSiegeMarkup(price); // 100%

            return AOS.Scale(price, 80); // refunds 80% of the purchase price
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBRealEstateBroker());
        }

        public RealEstateBroker(Serial serial)
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
        }
    }
}
