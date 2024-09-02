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

/* Scripts\Mobiles\Vendors\NPC\AnimalTrainer.cs
 * ChangeLog
 * 5/10/10, adam
 *		Stable Fees: Redesign stable fees as follows:
 *		(1) up basic stabling fee from 30 gold per week (.357 gold per UO day) to 84 gold per week (1 gold per UO day)
 *		(2) Actually charge the above amount once per UO day
 *		(3) Allow virtually unlimited *additional* stable slots for GM herding (up to 256) for 10 gp per UO day
 *		(4) Allow #3 above at a 50% discount (5gp per UO day) if (A) you belong to a township, and (B) your township has a stable master
 *		(5) if the gold cannot be collected automatically, create a tab for the player and require payment when ANY pets are claimed.
 * 12/15/04, Pix
 *		Stopped bretheren from being stabled.
 * 10/7/04, Pix
 *		Let dead pets be stabled.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/10/04 changes by mith
 *		When we put a GetMaxStabled function in BaseCreature for AI Auto-Stabling, the GetMaxStabled code here was overriden.
 *		Removed the GetMaxStabled function from here, as it was just a copy of what's in BaseCreature.
 */

using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class AnimalTrainer : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public AnimalTrainer()
            : base("the animal trainer")
        {
            /* Publish 4
			 * Any shopkeeper that is currently [invulnerable] will lose that status except for stablemasters.
			 */
            IsInvulnerable = true;

            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAnimalTrainer());
        }

        public override VendorShoeType ShoeType
        {
            get { return Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots; }
        }

        public override int GetShoeHue()
        {
            return 0;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(Utility.RandomBool() ? (Item)new QuarterStaff() : (Item)new ShepherdsCrook());
        }

        private class StableEntry : ContextMenuEntry
        {
            private AnimalTrainer m_Trainer;
            private Mobile m_From;

            public StableEntry(AnimalTrainer trainer, Mobile from)
                : base(6126, 12)
            {
                m_Trainer = trainer;
                m_From = from;
            }

            public override void OnClick()
            {
                m_Trainer.BeginStable(m_From);
            }
        }

        /*private class ClaimListGump : Gump
		{
			private AnimalTrainer m_Trainer;
			private Mobile m_From;
			private ArrayList m_List;

			public ClaimListGump( AnimalTrainer trainer, Mobile from, ArrayList list ) : base( 50, 50 )
			{
				m_Trainer = trainer;
				m_From = from;
				m_List = list;

				from.CloseGump( typeof( ClaimListGump ) );

				AddPage( 0 );

				AddBackground( 0, 0, 325, 50 + (list.Count * 20), 9250 );
				AddAlphaRegion( 5, 5, 315, 40 + (list.Count * 20) );

				AddHtml( 15, 15, 275, 20, "<BASEFONT COLOR=#FFFFFF>Select a pet to retrieve from the stables:</BASEFONT>", false, false );

				for ( int i = 0; i < list.Count; ++i )
				{
					BaseCreature pet = list[i] as BaseCreature;

					if ( pet == null || pet.Deleted )
						continue;

					AddButton( 15, 39 + (i * 20), 10006, 10006, i + 1, GumpButtonType.Reply, 0 );
					AddHtml( 32, 35 + (i * 20), 275, 18, String.Format( "<BASEFONT COLOR=#C0C0EE>{0}</BASEFONT>", pet.Name ), false, false );
				}
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				int index = info.ButtonID - 1;

				if ( index >= 0 && index < m_List.Count )
					m_Trainer.EndClaimList( m_From, m_List[index] as BaseCreature );
			}
		}*/

        private class ClaimAllEntry : ContextMenuEntry
        {
            private AnimalTrainer m_Trainer;
            private Mobile m_From;

            public ClaimAllEntry(AnimalTrainer trainer, Mobile from)
                : base(6127, 12)
            {
                m_Trainer = trainer;
                m_From = from;
            }

            public override void OnClick()
            {
                m_Trainer.Claim(m_From);
            }
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            if (from.Alive)
            {
                list.Add(new StableEntry(this, from));

                if (from.Stabled.Count > 0)
                    list.Add(new ClaimAllEntry(this, from));
            }

            base.AddCustomContextEntries(from, list);
        }

        private class StableTarget : Target
        {
            private AnimalTrainer m_Trainer;

            public StableTarget(AnimalTrainer trainer)
                : base(12, false, TargetFlags.None)
            {
                m_Trainer = trainer;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseCreature)
                    m_Trainer.EndStable(from, (BaseCreature)targeted);
                else if (targeted == from)
                    m_Trainer.SayTo(from, 502672); // HA HA HA! Sorry, I am not an inn.
                else
                    m_Trainer.SayTo(from, 1048053); // You can't stable that!
            }
        }

        public void BeginClaimList(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            ArrayList list = new ArrayList();

            for (int i = 0; i < from.Stabled.Count; ++i)
            {
                BaseCreature pet = from.Stabled[i] as BaseCreature;

                if (pet == null)
                {
                    from.Stabled.RemoveAt(i);
                    --i;
                    continue;
                }

                if (pet.Deleted)
                {
                    pet.IsStabled = false;
                    pet.LastStableChargeTime = DateTime.MinValue;   // When set to MinValue, we don't serialize
                    from.Stabled.RemoveAt(i);
                    --i;
                    continue;
                }

                // tell the player they need to pay the back stable fees
                if (pet.GetFlag(CreatureFlags.StableHold))
                {
                    // charge the player back stable fees
                    if ((from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), pet.StableBackFees) == true))
                    {
                        Server.Commands.LogHelper Logger = new Server.Commands.LogHelper("PetHoldFromStables.log", false, true);
                        Logger.Log(string.Format("{0} gold was taken from {2}'s bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name, from));
                        Logger.Finish();

                        SayTo(from, "{0} gold was taken from your bank cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        pet.StableBackFees = 0;
                        pet.SetFlag(CreatureFlags.StableHold, false);
                        goto add_pet;
                    }
                    else
                    {
                        SayTo(from, "You will need {0} gold in your bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        continue;
                    }
                }

            add_pet:
                list.Add(pet);
            }

            if (list.Count > 0)
                from.SendGump(new ClaimListGump(this, from, list));
            else
                SayTo(from, 502671); // But I have no animals stabled with me at the moment!
        }

        public void EndClaimList(Mobile from, BaseCreature pet)
        {
            if (pet == null || pet.Deleted || pet.GetFlag(CreatureFlags.StableHold) || from.Map != this.Map || !from.InRange(this, 14) || !from.Stabled.Contains(pet) || !from.CheckAlive())
                return;

            if ((from.Followers + pet.ControlSlots) <= from.FollowersMax)
            {
                pet.SetControlMaster(from);

                if (pet.Summoned)
                    pet.SummonMaster = from;

                pet.ControlTarget = from;
                pet.ControlOrder = OrderType.Follow;

                pet.MoveToWorld(from.Location, from.Map);

                pet.IsStabled = false;
                from.Stabled.Remove(pet);

                pet.LastStableChargeTime = DateTime.MinValue;   // When set to MinValue, we don't serialize

                SayTo(from, 1042559); // Here you go... and good day to you!
            }
            else
            {
                SayTo(from, 1049612, pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
            }
        }

        /*
		 * Non-Township Stable Bonus:
		 * Tamer w/ Herding = Unlimited stable slots
		 * Cost = 10gp per pet, per UO day for pets over normal slot count.
		 * Township Stable Bonus/Incentive:
		 * Tamer w/ Herding = Unlimited stable slots
		 * Cost = 5gp per pet, per UO day for pets over normal slot count.
		 */
        public static int UODayChargePerPet(Mobile from)
        {
            // base rate increased from OSI 30 gold per real week to 84 gold per real week
            if (from.Stabled.Count < GetMaxEconomyStabled(from))
                return 1;   // 1 gp per UO day

            // township discount?
            if (TownshipStone.TownshipMember(from) != null)
            {
                TownshipStone ts = TownshipStone.TownshipMember(from);
                foreach (Mobile m in ts.TownshipMobiles)
                    if (m is TSAnimalTrainer)
                        return 5;   // 5 gp per UO day
            }

            return 10;  // 10 gp per UO day
        }

        public static int UODayChargePerPet(Mobile from, int slot)
        {
            // base rate increased from OSI 30 gold per real week to 84 gold per real week
            if (slot < GetMaxEconomyStabled(from))
                return 1;   // 1 gp per UO day

            // township discount?
            if (TownshipStone.TownshipMember(from) != null)
            {
                TownshipStone ts = TownshipStone.TownshipMember(from);
                foreach (Mobile m in ts.TownshipMobiles)
                    if (m is TSAnimalTrainer)
                        return 5;   // 5 gp per UO day
            }

            return 10;  // 10 gp per UO day
        }

        public void BeginStable(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            if (from.Stabled.Count >= GetMaxStabled(from))
            {
                SayTo(from, 1042565); // You have too many pets in the stables!
            }
            else
            {
                /* I charge 30 gold per pet for a real week's stable time.
				 * I will withdraw it from thy bank account.
				 * Which animal wouldst thou like to stable here? */
                //SayTo(from, 1042558);	

                // adam: actually charge the players now.
                SayTo(from,
                    string.Format("I charge {0} gold per pet for a real day's stable time. I will withdraw it from thy bank account. Which animal wouldst thou like to stable here?", UODayChargePerPet(from) * 12)
                    );

                from.Target = new StableTarget(this);
            }
        }

        public void EndStable(Mobile from, BaseCreature pet)
        {
            if (Deleted || !from.CheckAlive())
                return;

            if (!pet.Controlled || pet.ControlMaster != from)
            {
                SayTo(from, 1042562); // You do not own that pet!
            }
            //Pix: 10/7/2004 - allow dead pets to be stabled.
            //else if ( pet.IsDeadPet )
            //{
            //	SayTo( from, 1049668 ); // Living pets only, please.
            //}
            else if (pet.Summoned)
            {
                SayTo(from, 502673); // I can not stable summoned creatures.
            }
            else if (pet.IOBFollower) // Don't stable IOB Bretheren
            {
                SayTo(from, "You can't stable your bretheren!");
            }
            else if (pet.Body.IsHuman)
            {
                SayTo(from, 502672); // HA HA HA! Sorry, I am not an inn.
            }
            else if ((pet is PackLlama || pet is PackHorse || pet is Beetle) && (pet.Backpack != null && pet.Backpack.Items.Count > 0))
            {
                SayTo(from, 1042563); // You need to unload your pet.
            }
            else if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
            {
                SayTo(from, 1042564); // I'm sorry.  Your pet seems to be busy.
            }
            else if (from.Stabled.Count >= GetMaxStabled(from))
            {
                SayTo(from, 1042565); // You have too many pets in the stables!
            }
            else
            {   // stable pet and charge/
                if (StablePet(from, pet, false))
                    SayTo(from, 502679); // Very well, thy pet is stabled. Thou mayst recover it by saying 'claim' to me. In one real world week, I shall sell it off if it is not claimed!
                else
                    SayTo(from, 502677); // But thou hast not the funds in thy bank account!
            }
        }

        public static bool StablePet(Mobile from, BaseCreature pet, bool bForceStable)
        {
            bool bHasGold = from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), UODayChargePerPet(from));

            if (pet is IMount)
                ((IMount)pet).Rider = null; // make sure it's dismounted

            // First try to get the gold, if not and force is set, stable anyway and tell the player they will loose their pet soon.
            // Force is set when we are sending a player to prison or jail and they don't really have any choice
            if (bHasGold || bForceStable)
            {
                pet.ControlTarget = null;
                pet.ControlOrder = OrderType.Stay;
                pet.Internalize();

                pet.SetControlMaster(null);
                pet.SummonMaster = null;

                pet.IsStabled = true;
                from.Stabled.Add(pet);

                pet.LastStableChargeTime = DateTime.UtcNow;

                // they are probably being jailed, let them slide for this FIRST payment only
                if (bHasGold == false && bForceStable == true)
                {
                    pet.SetFlag(CreatureFlags.StableHold, true);
                    pet.StableBackFees = UODayChargePerPet(from);
                    from.SendMessage("Thou hast not the funds in thy bank account to stable this pet.");
                    from.SendMessage("The stable master will keep a runing account for {0} of owed fees.", pet.Name);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Claim(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            bool claimed = false;
            int stabled = 0;

            for (int i = 0; i < from.Stabled.Count; ++i)
            {
                BaseCreature pet = from.Stabled[i] as BaseCreature;

                if (pet == null)
                {
                    from.Stabled.RemoveAt(i);
                    --i;
                    continue;
                }

                if (pet.Deleted)
                {
                    pet.IsStabled = false;
                    pet.LastStableChargeTime = DateTime.MinValue;   // When set to MinValue, we don't serialize
                    from.Stabled.RemoveAt(i);
                    --i;
                    continue;
                }

                // tell the player they need to pay the back stable fees
                if (pet.GetFlag(CreatureFlags.StableHold))
                {
                    // charge the player back stable fees
                    if ((from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), pet.StableBackFees) == true))
                    {
                        Server.Commands.LogHelper Logger = new Server.Commands.LogHelper("PetHoldFromStables.log", false, true);
                        Logger.Log(string.Format("{0} gold was taken from {2}'s bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name, from));
                        Logger.Finish();

                        SayTo(from, "{0} gold was taken from your bank cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        pet.StableBackFees = 0;
                        pet.SetFlag(CreatureFlags.StableHold, false);
                        goto add_pet;
                    }
                    else
                    {
                        SayTo(from, "You will need {0} gold in your bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        continue;
                    }
                }

            add_pet:
                ++stabled;

                if ((from.Followers + pet.ControlSlots) <= from.FollowersMax)
                {
                    pet.SetControlMaster(from);

                    if (pet.Summoned)
                        pet.SummonMaster = from;

                    pet.ControlTarget = from;
                    pet.ControlOrder = OrderType.Follow;

                    pet.MoveToWorld(from.Location, from.Map);

                    pet.IsStabled = false;
                    from.Stabled.RemoveAt(i);

                    pet.LastStableChargeTime = DateTime.MinValue;   // When set to MinValue, we don't serialize
                    --i;

                    claimed = true;
                }
                else
                {
                    SayTo(from, 1049612, pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
                }
            }

            if (claimed)
                SayTo(from, 1042559); // Here you go... and good day to you!
            else if (stabled == 0)
                SayTo(from, 502671); // But I have no animals stabled with me at the moment!
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.HasKeyword(0x0008))
            {
                e.Handled = true;
                BeginStable(e.Mobile);
            }
            else if (!e.Handled && e.HasKeyword(0x0009))
            {
                e.Handled = true;

                if (!Insensitive.Equals(e.Speech, "claim"))
                    BeginClaimList(e.Mobile);
                else
                    Claim(e.Mobile);
            }
            else
            {
                base.OnSpeech(e);
            }
        }

        public AnimalTrainer(Serial serial)
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
