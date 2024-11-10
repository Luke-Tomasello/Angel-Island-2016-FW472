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

using System;

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
            Hue = Utility.RandomSpecialHue(GetType().FullName);
            Name = "Teleport Management Console";
        }

        public TeleportConsole(Serial serial)
            : base(serial)
        {
        }

        public static double DefaultTeleDelay = 0.0;
        public static bool DefaultTeleGlobalDelay = false;
        public static int DefaultTeleTiles = 12;

        [CommandProperty(AccessLevel.Owner)]
        public bool TeleRunning
        {
            get
            {
                return CoreAI.TeleRunningEnabled;
            }
            set
            {
                CoreAI.TeleRunningEnabled = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public bool GlobalDelay
        {
            get
            {
                return CoreAI.TeleGlobalDelay;
            }
            set
            {
                CoreAI.TeleGlobalDelay = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public bool Reset
        {
            get
            {
                return false;
            }
            set
            {
                CoreAI.TeleDelay = DefaultTeleDelay;
                CoreAI.TeleGlobalDelay = DefaultTeleGlobalDelay;
                CoreAI.TeleTiles = DefaultTeleTiles;
                Server.Spells.Third.TeleportSpell.SpiritCohesion = new Memory();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public double TeleDelay
        {
            get
            {
                return TimeSpan.FromSeconds(CoreAI.TeleDelay).TotalMilliseconds;
            }
            set
            {
                CoreAI.TeleDelay = TimeSpan.FromMilliseconds(value).TotalSeconds;
            }
        }
        [CommandProperty(AccessLevel.Owner)]
        public int TeleTiles
        {
            get
            {
                return CoreAI.TeleTiles;
            }
            set
            {
                CoreAI.TeleTiles = value;
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel == AccessLevel.Owner)
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
