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

/* ChangeLog 
 *  9/13/2024, Adam (GetBalance())
 *      Use GetBalance() instead of BankBox.TotalGold as GetBalance includes checks
 *	01/03/07, plasma
 *		Remove all duel challenge system code
 *  10/15/07, Pix
 *      Fixed expired AggressorInfos from preventing banking.
 *  9/1/07, Pix
 *      Fixed aggressor-banking.
 *	8/26/07, Pix
 *		Prevent duelers from accessing their bankbox.
 *	8/22/07, Adam
 *		Add new CashCheck() function to be called from Withdraw() if the resulting check would be less than 5000 gold.
 *	04/16/05, Kitaras
 * 		Added DepositUpTo() function, added ability to deposit from a player or a container
 *	10/16/04, Darva
 *		Added code to prevent players from opening their bank box for 2 minutes after
 * 		Any successful steal.
 *
*/

using Server.ContextMenus;
using Server.Items;
using Server.Network;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Banker : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.MerchantsGuild; } }

        [Constructable]
        public Banker()
            : base("the banker")
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBBanker());
        }

        public static int GetBalance(Mobile from)
        {
            Item[] gold, checks;

            return GetBalance(from, out gold, out checks);
        }

        public static int GetBalance(Mobile from, out Item[] gold, out Item[] checks)
        {
            int balance = 0;

            Container bank = from.BankBox;

            if (bank != null)
            {
                gold = bank.FindItemsByType(typeof(Gold));
                checks = bank.FindItemsByType(typeof(BankCheck));

                for (int i = 0; i < gold.Length; ++i)
                    balance += gold[i].Amount;

                for (int i = 0; i < checks.Length; ++i)
                    balance += ((BankCheck)checks[i]).Worth;
            }
            else
            {
                gold = checks = new Item[0];
            }

            return balance;
        }

        public static bool Withdraw(Mobile from, int amount)
        {
            Item[] gold, checks;
            int balance = GetBalance(from, out gold, out checks);

            if (balance < amount)
                return false;

            for (int i = 0; amount > 0 && i < gold.Length; ++i)
            {
                if (gold[i].Amount <= amount)
                {
                    amount -= gold[i].Amount;
                    gold[i].Delete();
                }
                else
                {
                    gold[i].Amount -= amount;
                    amount = 0;
                }
            }

            for (int i = 0; amount > 0 && i < checks.Length; ++i)
            {
                BankCheck check = (BankCheck)checks[i];

                if (check.Worth <= amount)
                {
                    amount -= check.Worth;
                    check.Delete();
                }
                else
                {
                    check.Worth -= amount;
                    amount = 0;

                    // cash this check - don't leave tiny checks in the users bank
                    if (check.Worth < 5000)
                        CashCheck(from, check);
                }
            }

            return true;
        }

        public static void CashCheck(Mobile from, BankCheck check)
        {
            BankBox box = from.BankBox;

            if (box != null)
            {
                int deposited = 0;
                int toAdd = check.Worth;

                check.Delete();
                Gold gold;

                while (toAdd > 60000)
                {
                    gold = new Gold(60000);

                    if (box.TryDropItem(from, gold, false))
                    {
                        toAdd -= 60000;
                        deposited += 60000;
                    }
                    else
                    {
                        gold.Delete();

                        from.AddToBackpack(new BankCheck(toAdd));
                        toAdd = 0;

                        break;
                    }
                }

                if (toAdd > 0)
                {
                    gold = new Gold(toAdd);

                    if (box.TryDropItem(from, gold, false))
                    {
                        deposited += toAdd;
                    }
                    else
                    {
                        gold.Delete();

                        from.AddToBackpack(new BankCheck(toAdd));
                    }
                }
            }
        }

        public static bool Deposit(Mobile from, int amount)
        {
            BankBox box = from.BankBox;

            Item item = (amount >= 1000 ? (Item)new BankCheck(amount) : (Item)new Gold(amount));

            if (box != null && box.TryDropItem(from, item, false))
                return true;

            item.Delete();
            return false;
        }

        public static int DepositUpTo(Mobile from, int amount)
        {
            BankBox box = from.BankBox;
            if (box == null)
                return 0;

            int amountLeft = amount;
            while (amountLeft > 0)
            {
                Item item;
                int amountGiven;

                if (amountLeft < 5000)
                {
                    item = new Gold(amountLeft);
                    amountGiven = amountLeft;
                }
                else if (amountLeft <= 1000000)
                {
                    item = new BankCheck(amountLeft);
                    amountGiven = amountLeft;
                }
                else
                {
                    item = new BankCheck(1000000);
                    amountGiven = 1000000;
                }

                if (box.TryDropItem(from, item, false))
                {
                    amountLeft -= amountGiven;
                }
                else
                {
                    item.Delete();
                    break;
                }
            }

            return amount - amountLeft;
        }

        public static void Deposit(Container cont, int amount)
        {
            while (amount > 0)
            {
                Item item;

                if (amount < 5000)
                {
                    item = new Gold(amount);
                    amount = 0;
                }
                else if (amount <= 1000000)
                {
                    item = new BankCheck(amount);
                    amount = 0;
                }
                else
                {
                    item = new BankCheck(1000000);
                    amount -= 1000000;
                }

                cont.DropItem(item);
            }
        }
        public Banker(Serial serial)
            : base(serial)
        {
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 12))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.Mobile.InRange(this.Location, 12))
            {
                for (int i = 0; i < e.Keywords.Length; ++i)
                {
                    int keyword = e.Keywords[i];

                    switch (keyword)
                    {
                        case 0x0000: // *withdraw*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500389); // I will not do business with a criminal!
                                    break;
                                }

                                string[] split = e.Speech.Split(' ');

                                if (split.Length >= 2)
                                {
                                    int amount;

                                    try
                                    {
                                        amount = Convert.ToInt32(split[1]);
                                    }
                                    catch
                                    {
                                        break;
                                    }

                                    if (amount > 5000)
                                    {
                                        this.Say(500381); // Thou canst not withdraw so much at one time!
                                    }
                                    else if (amount > 0)
                                    {
                                        BankBox box = e.Mobile.BankBox;

                                        if (box == null || !box.ConsumeTotal(typeof(Gold), amount))
                                        {
                                            this.Say(500384); // Ah, art thou trying to fool me? Thou hast not so much gold!
                                        }
                                        else
                                        {
                                            e.Mobile.AddToBackpack(new Gold(amount));

                                            this.Say(1010005); // Thou hast withdrawn gold from thy account.
                                        }
                                    }
                                }

                                break;
                            }
                        case 0x0001: // *balance*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500389); // I will not do business with a criminal!
                                    break;
                                }

                                BankBox box = e.Mobile.BankBox;

                                if (box != null)
                                {
                                    //this.Say(1042759, box.TotalGold.ToString()); // Thy current bank balance is ~1_AMOUNT~ gold.
                                    this.Say(1042759, GetBalance(e.Mobile).ToString()); // Thy current bank balance is ~1_AMOUNT~ gold.
                                }

                                break;
                            }
                        case 0x0002: // *bank*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500378); // Thou art a criminal and cannot access thy bank box.
                                    break;
                                }

                                if (e.Mobile.Player == true)
                                {
                                    if (e.Mobile is PlayerMobile)
                                    {
                                        PlayerMobile pm = e.Mobile as PlayerMobile;

                                        if (DateTime.UtcNow - pm.LastStoleAt < TimeSpan.FromMinutes(2))
                                        {
                                            this.Say(500378); // Thou art a criminal and cannot access thy bank box.
                                            break;
                                        }

                                        //check that we're not actively involved in a fight:
                                        bool bBreak = false;
                                        for (int a = 0; a < pm.Aggressed.Count; a++)
                                        {
                                            AggressorInfo info = (AggressorInfo)pm.Aggressed[a];
                                            if (!info.Expired)
                                            {
                                                if (info.Attacker == pm && info.Defender is PlayerMobile)
                                                {
                                                    this.Say("You seem to be busy to bank, come back when you're not fighting.");
                                                    bBreak = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (bBreak) break;

                                    }
                                }
                                BankBox box = e.Mobile.BankBox;

                                if (box != null)
                                    box.Open();

                                break;
                            }
                        case 0x0003: // *check*
                            {
                                e.Handled = true;

                                if (e.Mobile.Criminal)
                                {
                                    this.Say(500389); // I will not do business with a criminal!
                                    break;
                                }

                                string[] split = e.Speech.Split(' ');

                                if (split.Length >= 2)
                                {
                                    int amount;

                                    try
                                    {
                                        amount = Convert.ToInt32(split[1]);
                                    }
                                    catch
                                    {
                                        break;
                                    }

                                    if (amount < 5000)
                                    {
                                        this.Say(1010006); // We cannot create checks for such a paltry amount of gold!
                                    }
                                    else if (amount > 1000000)
                                    {
                                        this.Say(1010007); // Our policies prevent us from creating checks worth that much!
                                    }
                                    else
                                    {
                                        BankCheck check = new BankCheck(amount);

                                        BankBox box = e.Mobile.BankBox;

                                        if (box == null || !box.TryDropItem(e.Mobile, check, false))
                                        {
                                            this.Say(500386); // There's not enough room in your bankbox for the check!
                                            check.Delete();
                                        }
                                        else if (!box.ConsumeTotal(typeof(Gold), amount))
                                        {
                                            this.Say(500384); // Ah, art thou trying to fool me? Thou hast not so much gold!
                                            check.Delete();
                                        }
                                        else
                                        {
                                            this.Say(1042673, AffixType.Append, amount.ToString(), ""); // Into your bank box I have placed a check in the amount of:
                                        }
                                    }
                                }

                                break;
                            }
                    }
                }
            }

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            if (from.Alive)
                list.Add(new OpenBankEntry(from, this));

            base.AddCustomContextEntries(from, list);
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
