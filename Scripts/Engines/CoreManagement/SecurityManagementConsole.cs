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

/* Engines/CoreManagement/SecurityManagementConsole.cs
 * CHANGELOG
 * 
 *  1/18/07 Taran Kain
 *      Initial version.
 */

using Server.Misc;
using Server.Mobiles;
using System;

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    class SecurityManagementConsole : Item
    {
        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan MovementPacketThrottleThreshold
        {
            get
            {
                return PlayerMobile.FastwalkThreshold;
            }
            set
            {
                if (value >= TimeSpan.Zero)
                    PlayerMobile.FastwalkThreshold = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool MovementPacketThrottlingEnabled
        {
            get
            {
                return PlayerMobile.FastwalkPrevention;
            }
            set
            {
                PlayerMobile.FastwalkPrevention = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int PacketThrottleCountWarningThreshold
        {
            get
            {
                return PlayerMobile.ThrottleRunWarningThreshold;
            }
            set
            {
                PlayerMobile.ThrottleRunWarningThreshold = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static AccessLevel FastwalkAccessOverride
        {
            get
            {
                return PlayerMobile.FastWalkAccessOverride;
            }
            set
            {
                PlayerMobile.FastWalkAccessOverride = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int FwdMaxSteps
        {
            get
            {
                return Mobile.FwdMaxSteps;
            }
            set
            {
                if (value >= 0)
                    Mobile.FwdMaxSteps = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool FastWalkDetectionEnabled
        {
            get
            {
                return Mobile.FwdEnabled;
            }
            set
            {
                Mobile.FwdEnabled = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool FastWalkProtectionEnabled
        {
            get
            {
                return Fastwalk.ProtectionEnabled;
            }
            set
            {
                Fastwalk.ProtectionEnabled = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool FwdUOTDOverride
        {
            get
            {
                return Mobile.FwdUOTDOverride;
            }
            set
            {
                Mobile.FwdUOTDOverride = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static AccessLevel FwdAccessOverride
        {
            get
            {
                return Mobile.FwdAccessOverride;
            }
            set
            {
                Mobile.FwdAccessOverride = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int FwdWarningThreshold
        {
            get
            {
                return Fastwalk.WarningThreshold;
            }
            set
            {
                Fastwalk.WarningThreshold = value;
            }
        }

        public static void Configure()
        {
            SetDefaults();
        }

        public static void SetDefaults()
        {
            FastWalkDetectionEnabled = true;
            FastWalkProtectionEnabled = false;
            FwdAccessOverride = AccessLevel.GameMaster;
            FwdMaxSteps = 2;
            FwdUOTDOverride = false;
            FwdWarningThreshold = 5;

            MovementPacketThrottlingEnabled = true;
            MovementPacketThrottleThreshold = TimeSpan.FromSeconds(0.1);
            PacketThrottleCountWarningThreshold = 5;
            FastwalkAccessOverride = AccessLevel.GameMaster;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static string ResetDefaults
        {
            get
            {
                return "Enter \"yes\" to reset defaults.";
            }
            set
            {
                if (Insensitive.Equals(value, "yes"))
                    SetDefaults();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(FastWalkDetectionEnabled);
            writer.Write(FastWalkProtectionEnabled);
            writer.Write((int)FwdAccessOverride);
            writer.Write(FwdMaxSteps);
            writer.Write(FwdUOTDOverride);
            writer.Write(FwdWarningThreshold);

            writer.Write(MovementPacketThrottleThreshold);
            writer.Write(MovementPacketThrottlingEnabled);
            writer.Write(PacketThrottleCountWarningThreshold);
            writer.Write((int)FastwalkAccessOverride);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        FastWalkDetectionEnabled = reader.ReadBool();
                        FastWalkProtectionEnabled = reader.ReadBool();
                        FwdAccessOverride = (AccessLevel)reader.ReadInt();
                        FwdMaxSteps = reader.ReadInt();
                        FwdUOTDOverride = reader.ReadBool();
                        FwdWarningThreshold = reader.ReadInt();

                        MovementPacketThrottleThreshold = reader.ReadTimeSpan();
                        MovementPacketThrottlingEnabled = reader.ReadBool();
                        PacketThrottleCountWarningThreshold = reader.ReadInt();
                        FastwalkAccessOverride = (AccessLevel)reader.ReadInt();

                        break;
                    }
                default:
                    {
                        throw new Exception("Invalid Security Management Console save version.");
                    }
            }

            if (version == 0)
                Hue = Utility.RandomSpecialHue(GetType().FullName);
        }

        [Constructable]
        public SecurityManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = Utility.RandomSpecialHue(GetType().FullName);
            Name = "Security Management Console";
        }

        public SecurityManagementConsole(Serial s)
            : base(s)
        {
        }
    }
}
