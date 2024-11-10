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

/* Scripts\Engines\CoreManagement\AIConsole.cs
 * ChangeLog
 *  10/8/2024, Adam
 *		Management console for AI knobs
 *		First time check in
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class AIConsole : Item
    {
        [Constructable]
        public AIConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = Utility.RandomSpecialHue(GetType().FullName);
            Name = "AI Management Console";
        }

        public AIConsole(Serial serial)
            : base(serial)
        {
        }

        private static bool m_PathTooComplex = false;
        [CommandProperty(AccessLevel.Owner)]
        public static bool UsePathTooComplex
        {
            get
            {
                return m_PathTooComplex;
            }
            set
            {
                m_PathTooComplex = value;
            }
        }

        private static bool m_AllGuardBug = false;
        [CommandProperty(AccessLevel.Owner)]
        public static bool AllGuardBugFix
        {
            get
            {
                return m_AllGuardBug;
            }
            set
            {
                m_AllGuardBug = value;
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

            writer.Write(m_PathTooComplex);
            writer.Write(m_AllGuardBug);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch(version) 
            { 
                case 1:
                    {
                        m_PathTooComplex = reader.ReadBool();
                        m_AllGuardBug = reader.ReadBool();
                        break;
                    }
            }

            if (version == 1)
                Hue = Utility.RandomSpecialHue(GetType().FullName);
        }
    }
}
