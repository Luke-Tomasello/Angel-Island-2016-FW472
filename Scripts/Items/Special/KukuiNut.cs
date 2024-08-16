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

/* Scripts\Items\Special\KukuiNut.cs
 * Changelog:
 *	05/09/06, Kit
 *		Initial creation limited one time item for reverting a pet from bonded back to unbonded.
 */

namespace Server.Items
{
    public class KukuiNut : Item
    {

        [Constructable]
        public KukuiNut()
            : base(0xF8B)
        {
            Hue = 541;
            Stackable = false;
            Weight = 1.0;
            LootType = LootType.Regular;
            Name = "a kukui nut";

        }

        public KukuiNut(Serial serial)
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
