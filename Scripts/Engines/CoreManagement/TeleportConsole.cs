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

/* Scripts\Engines\CoreManagement\TeleportConsole.cs
 * ChangeLog
 *  9/10/2024, Adam
 *		Management console for Teleport Spell distance and delay
 */


namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class TeleportConsole : Item
    {
        [Constructable]
        public TeleportConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x534;
            Name = "Teleport Management Console";
        }

        public TeleportConsole(Serial serial)
            : base(serial)
        {
        }

        public static double DefaultTeleDelay = 0.0;
        public static int DefaultTeleTiles = 12;

        [CommandProperty(AccessLevel.Player)]
        public bool Reset
        {
            get
            {
                return false;
            }
            set
            {
                if (Core.RuleSets.TestCenterRules())
                {
                    CoreAI.TeleDelay = DefaultTeleDelay;
                    CoreAI.TeleTiles = DefaultTeleTiles;
                    Server.Spells.Third.TeleportSpell.SpiritCohesion = new Memory();
                    InvalidateProperties();
                }
                else
                    SendMessage("This can only be used on Test Center");
            }
        }

        [CommandProperty(AccessLevel.Player)]
        public double TeleDelay
        {
            get
            {
                return TimeSpan.FromSeconds(CoreAI.TeleDelay).TotalMilliseconds;
            }
            set
            {
                if (Core.RuleSets.TestCenterRules())
                    CoreAI.TeleDelay = TimeSpan.FromMilliseconds(value).TotalSeconds;
                else
                    SendMessage("This can only be used on Test Center");
            }
        }
        [CommandProperty(AccessLevel.Player)]
        public int TeleTiles
        {
            get
            {
                return CoreAI.TeleTiles;
            }
            set
            {
                if (Core.RuleSets.TestCenterRules())
                    CoreAI.TeleTiles = value;
                else
                    SendMessage("This can only be used on Test Center");
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
