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

/* Scripts\Misc\HardwareInfo.cs
 * CHANGELOG
 *	8/24/10, Adam
 *		Add a HardwareInfo HASH function
 *		Save the hardware hash with the account
 *		Accounts will now use the hardware hash when the client fails to send a real hardware info packet
 */

using Server.Accounting;
using Server.Commands;
using Server.Network;
using Server.Targeting;
using System;

namespace Server
{
    public class HardwareInfo
    {
        private int m_InstanceID;
        private int m_OSMajor, m_OSMinor, m_OSRevision;
        private int m_CpuManufacturer, m_CpuFamily, m_CpuModel, m_CpuClockSpeed, m_CpuQuantity;
        private int m_PhysicalMemory;
        private int m_ScreenWidth, m_ScreenHeight, m_ScreenDepth;
        private int m_DXMajor, m_DXMinor;
        private int m_VCVendorID, m_VCDeviceID, m_VCMemory;
        private int m_Distribution, m_ClientsRunning, m_ClientsInstalled, m_PartialInstalled;
        private string m_VCDescription;
        private string m_Language;
        private string m_Unknown;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuModel { get { return m_CpuModel; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuClockSpeed { get { return m_CpuClockSpeed; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuQuantity { get { return m_CpuQuantity; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSMajor { get { return m_OSMajor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSMinor { get { return m_OSMinor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSRevision { get { return m_OSRevision; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int InstanceID { get { return m_InstanceID; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenWidth { get { return m_ScreenWidth; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenHeight { get { return m_ScreenHeight; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenDepth { get { return m_ScreenDepth; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PhysicalMemory { get { return m_PhysicalMemory; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuManufacturer { get { return m_CpuManufacturer; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuFamily { get { return m_CpuFamily; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCVendorID { get { return m_VCVendorID; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCDeviceID { get { return m_VCDeviceID; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCMemory { get { return m_VCMemory; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DXMajor { get { return m_DXMajor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DXMinor { get { return m_DXMinor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string VCDescription { get { return m_VCDescription; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Language { get { return m_Language; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Distribution { get { return m_Distribution; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsRunning { get { return m_ClientsRunning; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsInstalled { get { return m_ClientsInstalled; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PartialInstalled { get { return m_PartialInstalled; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Unknown { get { return m_Unknown; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string HashCode { get { return string.Format("{0:X}", GetHashCode()); } }

        public static void Initialize()
        {
            PacketHandlers.Register(0xD9, 0x10C, false, new OnPacketReceive(OnReceive));
            CommandSystem.Register("HWInfo", AccessLevel.GameMaster, new CommandEventHandler(HWInfo_OnCommand));
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile != null && e.Mobile.Account != null && e.Mobile.NetState != null)
                try
                {
                    LogHelper Logger = new LogHelper("HardwareInfo.log", false, true);
                    Account acct = e.Mobile.Account as Account;
                    string hi = "{null}";
                    if (acct.HardwareInfo != null)
                    {
                        hi = string.Format("{{{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {12}, {22}, {23}, {24}}}",
                            acct.HardwareInfo.m_InstanceID,
                            acct.HardwareInfo.m_OSMajor,
                            acct.HardwareInfo.m_OSMinor,
                            acct.HardwareInfo.m_OSRevision,
                            acct.HardwareInfo.m_CpuManufacturer,
                            acct.HardwareInfo.m_CpuFamily,
                            acct.HardwareInfo.m_CpuModel,
                            acct.HardwareInfo.m_CpuClockSpeed,
                            acct.HardwareInfo.m_CpuQuantity,
                            acct.HardwareInfo.m_PhysicalMemory,
                            acct.HardwareInfo.m_ScreenWidth,
                            acct.HardwareInfo.m_ScreenHeight,
                            acct.HardwareInfo.m_ScreenDepth,
                            acct.HardwareInfo.m_DXMajor,
                            acct.HardwareInfo.m_DXMinor,
                            acct.HardwareInfo.m_VCDescription,
                            acct.HardwareInfo.m_VCVendorID,
                            acct.HardwareInfo.m_VCDeviceID,
                            acct.HardwareInfo.m_VCMemory,
                            acct.HardwareInfo.m_Distribution,
                            acct.HardwareInfo.m_ClientsRunning,
                            acct.HardwareInfo.m_ClientsInstalled,
                            acct.HardwareInfo.m_PartialInstalled,
                            acct.HardwareInfo.m_Language,
                            acct.HardwareInfo.m_Unknown
                        );
                    }

                    Logger.Log(LogType.Mobile, e.Mobile,
                        string.Format("Current HardwareInfo={0}(hash={1}), Previous hash={2}", hi, acct.HardwareInfo != null ? string.Format("{0:X}", acct.HardwareInfo.GetHashCode()) : "null", string.Format("{0:X}", acct.HardwareHash)));
                    Logger.Finish();
                }
                catch (Exception ex) { LogHelper.LogException(ex); }
        }

        [Usage("HWInfo")]
        [Description("Displays information about a targeted player's hardware.")]
        public static void HWInfo_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(HWInfo_OnTarget));
            e.Mobile.SendMessage("Target a player to view their hardware information.");
        }

        public static void HWInfo_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile && ((Mobile)obj).Player)
            {
                Mobile m = (Mobile)obj;
                Account acct = m.Account as Account;

                if (acct != null)
                {
                    HardwareInfo hwInfo = acct.HardwareInfo;

                    if (hwInfo != null)
                        CommandLogging.WriteLine(from, "{0} {1} viewing hardware info of {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(m));

                    if (hwInfo != null)
                        from.SendGump(new Gumps.PropertiesGump(from, hwInfo));
                    else
                    {
                        from.SendMessage("No hardware information for that account was found.");
                        from.SendMessage("Previous hardware info hash code {0:X}.", acct.HardwareHash);
                    }
                }
                else
                {
                    from.SendMessage("No account has been attached to that player.");
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(HWInfo_OnTarget));
                from.SendMessage("That is not a player. Try again.");
            }
        }

        public override int GetHashCode()
        {   // create a hash code that represents this clients computer
            string temp = "";

            //temp += this.m_InstanceID.ToString();			// eliminate stuff we don't understand (might change?)
            temp += this.m_OSMajor.ToString();
            temp += this.m_OSMinor.ToString();
            temp += this.m_OSRevision.ToString();
            temp += this.m_CpuManufacturer.ToString();
            temp += this.m_CpuFamily.ToString();
            temp += this.m_CpuModel.ToString();
            temp += this.m_CpuClockSpeed.ToString();
            temp += this.m_CpuQuantity.ToString();
            temp += this.m_PhysicalMemory.ToString();
            temp += this.m_ScreenWidth.ToString();
            temp += this.m_ScreenHeight.ToString();
            temp += this.m_ScreenDepth.ToString();
            temp += this.m_DXMajor.ToString();
            temp += this.m_DXMinor.ToString();
            temp += this.m_VCDescription.ToString();
            temp += this.m_VCVendorID.ToString();
            temp += this.m_VCDeviceID.ToString();
            temp += this.m_VCMemory.ToString();
            temp += this.m_Distribution.ToString();
            temp += this.m_ClientsRunning.ToString();
            temp += this.m_ClientsInstalled.ToString();
            temp += this.m_PartialInstalled.ToString();
            temp += this.m_Language.ToString();
            //temp += this.m_Unknown.ToString();		// eliminate stuff we don't understand (might change?)

            return temp.GetHashCode();
        }

        public static void OnReceive(NetState state, PacketReader pvSrc)
        {
            pvSrc.ReadByte(); // 1: <4.0.1a, 2>=4.0.1a

            HardwareInfo info = new HardwareInfo();

            info.m_InstanceID = pvSrc.ReadInt32();
            info.m_OSMajor = pvSrc.ReadInt32();
            info.m_OSMinor = pvSrc.ReadInt32();
            info.m_OSRevision = pvSrc.ReadInt32();
            info.m_CpuManufacturer = pvSrc.ReadByte();
            info.m_CpuFamily = pvSrc.ReadInt32();
            info.m_CpuModel = pvSrc.ReadInt32();
            info.m_CpuClockSpeed = pvSrc.ReadInt32();
            info.m_CpuQuantity = pvSrc.ReadByte();
            info.m_PhysicalMemory = pvSrc.ReadInt32();
            info.m_ScreenWidth = pvSrc.ReadInt32();
            info.m_ScreenHeight = pvSrc.ReadInt32();
            info.m_ScreenDepth = pvSrc.ReadInt32();
            info.m_DXMajor = pvSrc.ReadInt16();
            info.m_DXMinor = pvSrc.ReadInt16();
            info.m_VCDescription = pvSrc.ReadUnicodeStringLESafe(64);
            info.m_VCVendorID = pvSrc.ReadInt32();
            info.m_VCDeviceID = pvSrc.ReadInt32();
            info.m_VCMemory = pvSrc.ReadInt32();
            info.m_Distribution = pvSrc.ReadByte();
            info.m_ClientsRunning = pvSrc.ReadByte();
            info.m_ClientsInstalled = pvSrc.ReadByte();
            info.m_PartialInstalled = pvSrc.ReadByte();
            info.m_Language = pvSrc.ReadUnicodeStringLESafe(4);
            info.m_Unknown = pvSrc.ReadStringSafe(64);

            Account acct = state.Account as Account;

            if (acct != null)
            {
                acct.HardwareInfo = info;
                acct.HardwareHash = info.GetHashCode();     // serialized - used again when no hardwareinfo is sent
            }
            else
                Console.WriteLine("HardwareInfo lost");
        }
    }
}
