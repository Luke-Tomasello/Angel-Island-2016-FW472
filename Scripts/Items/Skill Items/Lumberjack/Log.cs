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

using System;
namespace Server.Items
{
    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class Log : Item, ICommodity
    {
        string ICommodity.Description
        {
            get
            {
                return String.Format(Amount == 1 ? "{0} log" : "{0} logs", Amount);
            }
        }

        [Constructable]
        public Log()
            : this(1)
        {
        }

        [Constructable]
        public Log(int amount)
            : base(0x1BDD)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;
        }

        public Log(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Log(amount), amount);
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
