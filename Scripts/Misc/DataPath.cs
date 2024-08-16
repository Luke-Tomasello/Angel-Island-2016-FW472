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


using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Misc
{
    public class DataPath
    {
        /* If you have not installed Ultima Online,
		 * or wish the server to use a seperate set of datafiles,
		 * change the 'CustomPath' value, example:
		 * 
		 * private const string CustomPath = @"C:\Program Files\Ultima Online";
		 */
        private static string CustomPath = null;

        /* The following is a list of files which a required for proper execution:
		 * 
		 * Multi.idx
		 * Multi.mul
		 * VerData.mul
		 * TileData.mul
		 * Map*.mul
		 * StaIdx*.mul
		 * Statics*.mul
		 * MapDif*.mul
		 * MapDifL*.mul
		 * StaDif*.mul
		 * StaDifL*.mul
		 * StaDifI*.mul
		 */

        public static void Configure()
        {
            string pathReg = GetExePath("Ultima Online");
            string pathTD = GetExePath("Ultima Online Third Dawn");

            if (CustomPath != null)
                Core.DataDirectories.Add(CustomPath);

            if (pathReg != null)
                Core.DataDirectories.Add(pathReg);

            if (pathTD != null)
                Core.DataDirectories.Add(pathTD);

            if (Core.DataDirectories.Count == 0)
            {
                Console.WriteLine("Enter the Ultima Online directory:");
                Console.Write("> ");

                Core.DataDirectories.Add(Console.ReadLine());
            }
        }

        private static string GetExePath(string subName)
        {
            try
            {
                if (Core.Is64Bit)
                {
                    IntPtr key;
                    int error;
                    // adam: 8/21/22, first try the "Angel Island" key
                    error = WOW6432Node.RegOpenKeyEx(WOW6432Node.HKEY_LOCAL_MACHINE, String.Format(@"SOFTWARE\Angel Island\{0}\1.0", subName),
                        0, WOW6432Node.KEY_READ | WOW6432Node.KEY_WOW64_32KEY, out key);

                    // adam: 8/21/22, next try the standard "Origin Worlds Online" key
                    if (error != 0)
                        error = WOW6432Node.RegOpenKeyEx(WOW6432Node.HKEY_LOCAL_MACHINE, String.Format(@"SOFTWARE\Origin Worlds Online\{0}\1.0", subName),
                        0, WOW6432Node.KEY_READ | WOW6432Node.KEY_WOW64_32KEY, out key);

                    if (error != 0)
                        return null;
                    try
                    {
                        string v = WOW6432Node.RegQueryValue(key, "ExePath") as String;

                        if (v == null || v.Length <= 0)
                            return null;

                        // fix up the string, remove trailing \0
                        if (v.IndexOf("\0") != -1)
                            v = v.Trim('\0');

                        if (!File.Exists(v))
                            return null;

                        v = Path.GetDirectoryName(v);

                        if (v == null)
                            return null;

                        return v;
                    }
                    finally
                    {
                        WOW6432Node.RegCloseKey(key);
                    }
                    return null;
                }
                else
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format(@"SOFTWARE\Origin Worlds Online\{0}\1.0", subName)))
                    {
                        if (key == null)
                            return null;

                        string v = key.GetValue("ExePath") as string;

                        if (v == null || v.Length <= 0)
                            return null;

                        if (!File.Exists(v))
                            return null;

                        v = Path.GetDirectoryName(v);

                        if (v == null)
                            return null;

                        return v;
                    }

                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    public static class WOW6432Node
    {
        [DllImport("Advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(IntPtr hKey, [In] string lpSubKey, int ulOptions, int samDesired, out IntPtr phkResult);
        [DllImport("Advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode)]
        public static extern int RegQueryValueEx(IntPtr hKey, [In] string lpValueName, IntPtr lpReserved, out int lpType, [Out] byte[] lpData, ref int lpcbData);
        [DllImport("advapi32.dll")]
        public static extern int RegCloseKey(IntPtr hKey);

        static public readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(-2147483648);
        static public readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);
        static public readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);
        static public readonly IntPtr HKEY_USERS = new IntPtr(-2147483645);
        static public readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(-2147483644);
        static public readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(-2147483643);
        static public readonly IntPtr HKEY_DYN_DATA = new IntPtr(-2147483642);

        public const int KEY_READ = 0x20019;
        public const int KEY_WRITE = 0x20006;
        public const int KEY_QUERY_VALUE = 0x0001;
        public const int KEY_SET_VALUE = 0x0002;
        public const int KEY_WOW64_64KEY = 0x0100;
        public const int KEY_WOW64_32KEY = 0x0200;

        public const int REG_NONE = 0;
        public const int REG_SZ = 1;
        public const int REG_EXPAND_SZ = 2;
        public const int REG_BINARY = 3;
        public const int REG_DWORD = 4;
        public const int REG_DWORD_BIG_ENDIAN = 5;
        public const int REG_LINK = 6;
        public const int REG_MULTI_SZ = 7;
        public const int REG_RESOURCE_LIST = 8;
        public const int REG_FULL_RESOURCE_DESCRIPTOR = 9;
        public const int REG_RESOURCE_REQUIREMENTS_LIST = 10;
        public const int REG_QWORD = 11;

        public static object RegQueryValue(IntPtr key, string value)
        {
            return RegQueryValue(key, value, null);
        }

        public static object RegQueryValue(IntPtr key, string value, object defaultValue)
        {
            int error, type = 0, dataLength = 0xfde8;
            int returnLength = dataLength;
            byte[] data = new byte[dataLength];
            while ((error = RegQueryValueEx(key, value, IntPtr.Zero, out type, data, ref returnLength)) == 0xea)
            {
                dataLength *= 2;
                returnLength = dataLength;
                data = new byte[dataLength];
            }
            if (error == 2)
                return defaultValue; // value doesn't exist
            if (error != 0)
                throw new Win32Exception(error);

            switch (type)
            {
                case REG_NONE:
                case REG_BINARY:
                    return data;
                case REG_DWORD:
                    return (((data[0] | (data[1] << 8)) | (data[2] << 16)) | (data[3] << 24));
                case REG_DWORD_BIG_ENDIAN:
                    return (((data[3] | (data[2] << 8)) | (data[1] << 16)) | (data[0] << 24));
                case REG_QWORD:
                    {
                        uint numLow = (uint)(((data[0] | (data[1] << 8)) | (data[2] << 16)) | (data[3] << 24));
                        uint numHigh = (uint)(((data[4] | (data[5] << 8)) | (data[6] << 16)) | (data[7] << 24));
                        return (long)(((ulong)numHigh << 32) | (ulong)numLow);
                    }
                case REG_SZ:
                    return Encoding.Unicode.GetString(data, 0, returnLength);
                case REG_EXPAND_SZ:
                    return Environment.ExpandEnvironmentVariables(Encoding.Unicode.GetString(data, 0, returnLength));
                case REG_MULTI_SZ:
                    {
                        var strings = new List<string>();
                        string packed = Encoding.Unicode.GetString(data, 0, returnLength);
                        int start = 0;
                        int end = packed.IndexOf('\0', start);
                        while (end > start)
                        {
                            strings.Add(packed.Substring(start, end - start));
                            start = end + 1;
                            end = packed.IndexOf('\0', start);
                        }
                        return strings.ToArray();
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
