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

/* Scripts\Mobiles\Special\ChristmasElf.cs
 * ChangeLog
 * 12/20/07, Adam
 *      Replace the Christmas tree with a roast pig so that we can get the trees our well before christmas
 * 12/18/06 Adam
 *      Update gifts to have "Christmas <year>" tag
 * 12/03/06 Taran Kain
 *      Set Female to true - no tranny elves!
 * 12/17/05, Adam
 *		This method fails if the bank is full!
 *		--> box.TryDropItem( from, Giftbox, false );
 *		this one won't 
 *		--> box.AddItem( Giftbox );
 * 12/13/05, Kit
 *		Added cookies to be dropped as well
 * 12/11/05 Adam
 *		Add the 'elf look'
 * 12/11/05 Kit,
 *		Added light source
 * 12/11/05 Kit, 
 *		Initial Creation
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class ChristmasElf : BaseCreature
    {
        private string message = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string MsgToSay { get { return message; } set { message = value; InvalidateProperties(); } }

        public override bool ClickTitle { get { return true; } }

        [Constructable]
        public ChristmasElf()
            : base(AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2)
        {
            SpeechHue = Utility.RandomDyedHue();
            Female = true;
            Body = 0x191;
            Title = "the elf";

            NameHue = CalcInvulNameHue();

            Name = NameList.RandomName("female");
            Blessed = true;

            // the "elf look"
            AddItem(new LightSource());
            AddItem(new Candle());
            Hue = 366;                              // pale green
            AddItem(new ShortHair());               // seems elf like
            Item Skirt = new LeatherSkirt();        // she's sexy!
            Skirt.Hue = 33;                         // devil in the red dress!
            AddItem(Skirt);
            Item Bustier = new LeatherBustierArms();
            Bustier.Hue = 33;
            AddItem(Bustier);
            Item Sandals = new Sandals();
            Sandals.Hue = 33;
            AddItem(Sandals);

            SetHits(100);
        }
        public ChristmasElf(Serial serial)
            : base(serial)
        {
        }
        public override bool CanBeDamaged()
        {
            return true;
        }
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            BankBox box = from.BankBox;
            if (box == null)
                return false;

            if (dropped is HolidayDeed)
            {
                if (message != null)
                    SayTo(from, message);
                Item random = null;
                int randomitem = Utility.RandomMinMax(0, 3);

                if (randomitem == 0)
                {
                    random = new Snowman();
                    random.Name = "snowman";
                }

                if (randomitem == 1)
                    random = new SnowPile();

                if (randomitem == 2)
                    random = new EternalEmbers();

                if (randomitem == 3)
                {
                    if (Utility.RandomBool())
                        random = new RedPoinsettia();
                    else
                        random = new WhitePoinsettia();

                    random.Name = "poinsettia";
                }

                Item roast = new RoastPig();
                roast.Name = "roast pig";

                Item candle = new CandleLong();
                candle.Name = "candle";
                candle.Hue = Utility.RandomList(0, 3343, 72, 7, 1274, 53);

                Item cookie = new Cookies();
                cookie.Name = "Christmas cookies";

                GiftBox Giftbox = new GiftBox();

                string year = DateTime.Now.Year.ToString();
                string Signature = "Christmas " + year;

                Giftbox.Name = Signature;

                roast.Name += " - " + Signature;
                Giftbox.DropItem(roast);
                candle.Name += " - " + Signature;
                Giftbox.DropItem(candle);
                random.Name += " - " + Signature;
                Giftbox.DropItem(random);
                cookie.Name += " - " + Signature;
                Giftbox.DropItem(cookie);

                //drop it all to bank
                // Adam: This method fails if the bank is full!
                // box.TryDropItem( from, Giftbox, false );
                // this one won't 
                box.AddItem(Giftbox);

                //delete deed.
                dropped.Delete();
                return true;
            }
            else //if not a holiday deed dont accept anything
                return false;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(message);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            MsgToSay = reader.ReadString();

            NameHue = CalcInvulNameHue();
        }
    }
}
