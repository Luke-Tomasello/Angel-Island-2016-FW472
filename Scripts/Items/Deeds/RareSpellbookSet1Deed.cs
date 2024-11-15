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

/* Items/Deeds/RareSpellbookSet1Deed.cs
 * ChangeLog:
 *	4/1/10, Adam
 *		Remove LootType = LootType.Blessed; from deserialize
 *		This was preventing the items from dropping via lootpacks
 *	5/14/09, Adam
 *		initial creation
 */

namespace Server.Items
{
    public class RareSpellbookSet1Deed : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public RareSpellbookSet1Deed()
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = Utility.RandomList(2207, 2425, 2213, 2419);
            Name = "a rare spellbook deed";
            LootType = LootType.Regular;
        }

        public RareSpellbookSet1Deed(Serial serial)
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

            // must be regular to work with LootPacks
            if (LootType != LootType.Regular)
                LootType = LootType.Regular;
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                this.Delete();
                from.SendMessage("A rare spellbook has been placed in your backpack.");
                Spellbook book = new Spellbook();   // new spell book
                book.Hue = Hue;                     // take the hue of this deed.
                book.Name = "magical spellbook";    // an interesting name
                from.AddToBackpack(book);           // stash it
            }
        }
    }
}
