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

/* Server/Utility.cs
 * ChangeLog:
 *	7/20/10, adam
 *		Add a 'Release' callback for the Memory system.
 *		Note: The Memory system is passive, that is it doesn't use any timers for cleaning up old objects, it
 *			simply cleans them up the next time it's called before processing the request. The one exception
 *			to this rule is when you specify a 'Release' callback to be notified when an object expires.
 *			In this case a timer is created to insure your callback is timely.
 *	7/11/10, adam
 *		Update our memoy system to support the notion of a memory context, i.e., what you remember about the thing
 *		For instance, if you are remembering the Mobile 'adam', you may want to remember the last know location in the context.
 *	6/21/10, adam
 *		Add a simple memory system for timed remembering of items and mobiles.
 *		See BaseAI.cs & Container.cs for an example.
 *	5/27/10, adam
 *		Refactor RandomChance() to take a double insead of an int so you can pass facrtions like 10.4 would be a 10.4% chance
 * 3/21/10, adam
 *		Add function RescaleNumber to scale a number from an Old range to a new range. 
 *	5/11/08, Adam
 *		Added Profiler class
 *  3/25/08, Pix
 *		Added AdjustedDateTime class.
 *  5/8/07, Adam
 *      Add new data packing functions. e.g. Utility.GetUIntRight16()
 *  12/25/06, Adam
 *      Add an Elapsed() function to the TimeCheck class
 *  12/21/06, Adam
 *      Moved TimeCheck from Heartbeat.cs to Utility.cs
 *  12/19/06, Adam
 *      Add: GetHost(), IsHostPrivate(), IsHostPROD(), IsHostTC()
 *      These functions help to distinguish private servers from public.
 *      See: CrashGuard.cs
 *  3/28/06 Taran Kain
 *		Change IPAddress.Address (deprecated) references to use GetAddressBytes()
 *	3/18/06, Adam
 *		Move special dye tub colors here from the Harrower code
 * 		i.e., RandomSpecialHue()
 *	2/2/06, Adam
 *		Add DebugOut() functions.
 *		This functions only output if DEBUG is defined
 *	9/17/05, Adam
 *		add 'special' hues; i.e., RandomSpecialVioletHue() 
 *	7/26/05, Adam
 *		Massive AOS cleanout
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Server
{
    public class Utility
    {
        public static class Monitor
        {
            public static void Write(string text, ConsoleColor color)
            {
                System.Console.Out.Flush();
                PushColor(color);
                System.Console.Write(text);
                PopColor();
                System.Console.Out.Flush();
            }
            public static void WriteLine(string format, ConsoleColor color, params object[] args)
            {
                Monitor.WriteLine(string.Format(format, args), color);
            }
            public static void WriteLine(string text, ConsoleColor color)
            {
                System.Console.Out.Flush();
                PushColor(color);
                System.Console.WriteLine(text);
                //if (m_ConsoleOutEcho != null)
                //{
                //    EnsurePath(m_ConsoleOutEcho);
                //    File.AppendAllLines(m_ConsoleOutEcho, new string[] { text }, Encoding.UTF8);
                //}
                PopColor();
                System.Console.Out.Flush();
            }
        }
        public static class World
        {
            private static Rectangle2D[] m_BritWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 5120 - 32, 4096 - 32), new Rectangle2D(5136, 2320, 992, 1760) };
            private static Rectangle2D[] m_IlshWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 2304 - 32, 1600 - 32) };
            private static Rectangle2D[] m_TokunoWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 1448 - 32, 1448 - 32) };
            private static Rectangle2D[] m_SolenCaves = new Rectangle2D[] { new Rectangle2D(5632, 1775, 305, 266) };
            private static Rectangle2D[] m_Dungeons = new Rectangle2D[] { new Rectangle2D(new Point2D(5120, 3), new Point2D(6143, 2303)) };
            private static Rectangle2D[] m_DaggerIsland = new Rectangle2D[] { new Rectangle2D(3865, 175, 432, 584) };


            // Rectangle to define Angel Island boundaries.
            // used to define where boats can be placed, and where they can sail.
            private static Rectangle2D m_AIRect = new Rectangle2D(140, 690, 270, 180);
            public static Rectangle2D AIRect { get { return m_AIRect; } }
            public static Rectangle2D[] BritWrap { get { return m_BritWrap; } }
            public static Rectangle2D BritMainLandWrap { get { return m_BritWrap[0]; } }
            public static Rectangle2D LostLandsWrap { get { return m_BritWrap[1]; } }
            public static Rectangle2D SolenCaves { get { return m_SolenCaves[0]; } }
            public static Rectangle2D Dungeons { get { return m_Dungeons[0]; } }
            public static Rectangle2D DaggerIsland { get { return m_DaggerIsland[0]; } }

            public static bool IsValidLocation(Point3D p, Map map)
            {
                Rectangle2D[] wrap = GetWrapFor(map);

                for (int i = 0; i < wrap.Length; ++i)
                {
                    if (wrap[i].Contains(p))
                        return true;
                }

                return false;
            }
#if false
            public static bool InaccessibleMapLoc(Point3D p, Map map)
            {
                RecallRune marker = new RecallRune();
                marker.MoveToWorld(p, map);

                IPooledEnumerable eable;
                eable = map.GetItemsInRange(p, 0);
                bool found = false;
                foreach (Item item in eable)
                {
                    if (item == null || item.Deleted || item is not RecallRune)
                        continue;

                    // yeah, GetItemsInRange() does not respect exact Z, not sure why
                    if (item.Z != p.Z)
                        continue;

                    found = true;
                    break;
                }
                eable.Free();

                marker.Delete();

                return !found;
            }
#endif
            public static Rectangle2D[] GetWrapFor(Map map)
            {
                if (map == Map.Ilshenar)
                    return m_IlshWrap;
                else if (map == Map.Tokuno)
                    return m_TokunoWrap;
                else
                    return m_BritWrap;
            }

            public static bool AreaContains(Rectangle2D[] area, Point2D p)
            {
                for (int i = 0; i < area.Length; i++)
                {
                    if (area[i].Contains(p))
                        return true;
                }

                return false;
            }
        }
        public static List<Item> ConvertToList(ArrayList old_style)
        {
            List<Item> new_style = new List<Item>();

            foreach (object o in old_style)
                if (o != null && o is Item)
                    new_style.Add(o as Item);

            return new_style;
        }


        private static Rectangle2D[] m_BritWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 5120 - 32, 4096 - 32), new Rectangle2D(5136, 2320, 992, 1760) };
        public static Rectangle2D[] BritWrap { get { return m_BritWrap; } }

        /// <summary>
        /// Token
        /// Return a statistically unique integer token representing a string.
        /// The resulting token represents a case insensitive, space insensitive, and word order insensitive string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Int64</returns>
        public static Int64 Token(string str)
        {
            if (str == null)
                return 0;

            string[] tokens = str.ToLower().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Int64 hash = 0;
            for (int ix = 0; ix < tokens.Length; ix++)
                hash += tokens[ix].GetHashCode();

            return hash;
        }

        public static bool Token(string str1, string str2)
        {
            return Token(str1) == Token(str2);
        }

        // adam: scale a number from an Old range to a new range. 
        // in the example below we take the stat based damage from mind blast (1-45 hp) and scale it DamageRangeLow and DamageRangeHigh.
        //	where DamageRangeLow is some where around a lightning bolt and DamageRangeHigh was somewhere around an ebolt
        // damage = (int) (((double)damage / ((45.0 - 1.0) / (DamageRangeHigh - DamageRangeLow))) + DamageRangeLow);
        public static double RescaleNumber(double n, double oRangeMin, double oRangeMax, double nRangeMin, double nRangeMax)
        {
            try { return (((double)n / ((oRangeMax - oRangeMin) / (nRangeMax - nRangeMin))) + nRangeMin); }
            catch { return 0.0; }
        }

        // data packing functions
        public static uint GetUIntRight16(uint value)
        {
            return value & 0x0000FFFF;
        }

        public static void SetUIntRight16(ref uint value, uint bits)
        {
            value &= 0xFFFF0000;   // clear bottom 16
            bits &= 0x0000FFFF;    // clear top 16
            value |= bits;         // Done
        }

        public static uint GetUIntByte3(uint value)
        {
            return (value & 0x00FF0000) >> 16;
        }

        public static void SetUIntByte3(ref uint value, uint bits)
        {
            // set byte #3
            uint byte3 = bits << 16;    // move it into position
            value &= 0xFF00FFFF;        // clear 3rd byte in dest
            byte3 &= 0x00FF0000;        // clear all but 3rd byte - safety
            value |= byte3;             // Done
        }

        public static uint GetUIntByte4(uint value)
        {
            return (value & 0xFF000000) >> 24;
        }

        public static void SetUIntByte4(ref uint value, uint bits)
        {
            // set byte #4
            uint byte4 = bits << 24;    // move it into position
            value &= 0x00FFFFFF;        // clear 4th byte in dest
            byte4 &= 0xFF000000;        // clear all but 4th byte - safety
            value |= byte4;             // Done
        }

        /* How to use the Profiler class
			// Create a class like this
			public class  CPUProfiler
			{	// 
				public static void Start()
				{	// add as many of these as you want
					Region.InternalExitProfile.ResetTimer();		
				}
				public static void StopAndSaveSnapShot()
				{	// add as many of these as you want
					System.Console.WriteLine("Region.InternalExit: {0:00.00} seconds", Region.InternalExitProfile.Elapsed());
				}
			}
		 
			// then create a Utility.Profiler object in the file you want to monitor
			public static Utility.Profiler InternalExitProfile = new Utility.Profiler();

			// then bracket the code you want to monitor	
			public void InternalExit( Mobile m )
			{
				InternalExitProfile.Start();
				// the code you want to monitor
				InternalExitProfile.End();
			}
		 
			// finally make the calls to start and stop and print the profile info
			CPUProfiler.Start();							// ** PROFILER START ** 
			// call some process that you want to profile
			CPUProfiler.StopAndSaveSnapShot();				// ** PROFILER STOP ** 
		*/
        /*public class  Profiler
		{
			private double m_Elapsed;
			Utility.TimeCheck m_tc;
			public void ResetTimer() { m_Elapsed = 0.0; }
			public double Elapsed() { return m_Elapsed; }
			public void Start() { m_tc = new Utility.TimeCheck(); m_tc.Start(); }
			public void End() { m_tc.End(); m_Elapsed += m_tc.Elapsed(); }
		}*/

        public class TimeCheck
        {
            private DateTime m_startTime;
            private TimeSpan m_span;

            public TimeCheck()
            {
            }

            public void Start()
            {
                m_startTime = DateTime.Now;
            }

            public void End()
            {
                m_span = DateTime.Now - m_startTime;
            }

            public double Elapsed()
            {
                TimeSpan tx = DateTime.Now - m_startTime;
                return tx.TotalSeconds;
            }

            public string TimeTaken
            {
                get
                {
                    //return string.Format("{0:00}:{1:00}:{2:00.00}",
                    //	m_span.Hours, m_span.Minutes, m_span.Seconds);
                    return string.Format("{0:00.00} seconds", m_span.TotalSeconds);
                }
            }
        }

        public static void DebugOut(string text)
        {
#if DEBUG
            Console.WriteLine(text);
#endif
        }

        public static void DebugOut(string format, params object[] args)
        {
            DebugOut(String.Format(format, args));
        }

        private static Random m_Random = new Random();
        private static Encoding m_UTF8, m_UTF8WithEncoding;

        public static Encoding UTF8
        {
            get
            {
                if (m_UTF8 == null)
                    m_UTF8 = new UTF8Encoding(false, false);

                return m_UTF8;
            }
        }

        public static Encoding UTF8WithEncoding
        {
            get
            {
                if (m_UTF8WithEncoding == null)
                    m_UTF8WithEncoding = new UTF8Encoding(true, false);

                return m_UTF8WithEncoding;
            }
        }

        public static void Separate(StringBuilder sb, string value, string separator)
        {
            if (sb.Length > 0)
                sb.Append(separator);

            sb.Append(value);
        }

        public static bool IsValidIP(string text)
        {
            bool valid = true;

            IPMatch(text, IPAddress.None, ref valid);

            return valid;
        }

        public static bool IPMatch(string val, IPAddress ip)
        {
            bool valid = true;

            return IPMatch(val, ip, ref valid);
        }

        public static string FixHtml(string str)
        {
            bool hasOpen = (str.IndexOf('<') >= 0);
            bool hasClose = (str.IndexOf('>') >= 0);
            bool hasPound = (str.IndexOf('#') >= 0);

            if (!hasOpen && !hasClose && !hasPound)
                return str;

            StringBuilder sb = new StringBuilder(str);

            if (hasOpen)
                sb.Replace('<', '(');

            if (hasClose)
                sb.Replace('>', ')');

            if (hasPound)
                sb.Replace('#', '-');

            return sb.ToString();
        }

        public static bool IPMatch(string val, IPAddress ip, ref bool valid)
        {
            valid = true;

            string[] split = val.Split('.');

            for (int i = 0; i < 4; ++i)
            {
                int lowPart, highPart;

                if (i >= split.Length)
                {
                    lowPart = 0;
                    highPart = 255;
                }
                else
                {
                    string pattern = split[i];

                    if (pattern == "*")
                    {
                        lowPart = 0;
                        highPart = 255;
                    }
                    else
                    {
                        lowPart = 0;
                        highPart = 0;

                        bool highOnly = false;
                        int lowBase = 10;
                        int highBase = 10;

                        for (int j = 0; j < pattern.Length; ++j)
                        {
                            char c = (char)pattern[j];

                            if (c == '?')
                            {
                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += 0;
                                }

                                highPart *= highBase;
                                highPart += highBase - 1;
                            }
                            else if (c == '-')
                            {
                                highOnly = true;
                                highPart = 0;
                            }
                            else if (c == 'x' || c == 'X')
                            {
                                lowBase = 16;
                                highBase = 16;
                            }
                            else if (c >= '0' && c <= '9')
                            {
                                int offset = c - '0';

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'a' && c <= 'f')
                            {
                                int offset = 10 + (c - 'a');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'A' && c <= 'F')
                            {
                                int offset = 10 + (c - 'A');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else
                            {
                                valid = false;
                            }
                        }
                    }
                }

                int b = ip.GetAddressBytes()[i];

                if (b < lowPart || b > highPart)
                    return false;
            }

            return true;
        }

        public static bool IPMatchClassC(IPAddress ip1, IPAddress ip2)
        {
            for (int i = 0; i < 3; i++)
            {
                if (ip1.GetAddressBytes()[i] != ip2.GetAddressBytes()[i])
                    return false;
            }

            return true;
        }

        /*public static bool IPMatch( string val, IPAddress ip )
		{
			string[] split = val.Split( '.' );

			for ( int i = 0; i < split.Length; ++i )
			{
				int b = (byte)(ip.Address >> (i * 8));
				string s = split[i];

				if ( s == "*" )
					continue;

				if ( ToInt32( s ) != b )
					return false;
			}

			return true;
		}*/


        public static string GetHost()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch
            {
                return null;
            }
        }

        public static bool IsHostPrivate(string host)
        {
            try
            {   // are we on some random developer's computer?
                if (IsHostPROD(host) || IsHostTC(host))
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHostPROD(string host)
        {
            try
            {   // host name of our "prod" server
                if (host == "sls-dd4p11")
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHostTC(string host)
        {
            try
            {   // host name of our "Test Center" server
                if (host == "sls-ce9p3")
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static int InsensitiveCompare(string first, string second)
        {
            return Insensitive.Compare(first, second);
        }

        public static bool InsensitiveStartsWith(string first, string second)
        {
            return Insensitive.StartsWith(first, second);
        }

        public static bool ToBoolean(string value)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        public static double ToDouble(string value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.0;
            }
        }

        public static TimeSpan ToTimeSpan(string value)
        {
            try
            {
                return TimeSpan.Parse(value);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        public static int ToInt32(string value)
        {
            try
            {
                if (value.StartsWith("0x"))
                {
                    return Convert.ToInt32(value.Substring(2), 16);
                }
                else
                {
                    return Convert.ToInt32(value);
                }
            }
            catch
            {
                return 0;
            }
        }

        public static bool RandomChance(double percent)
        {
            return (percent / 100.0 >= Utility.RandomDouble());
        }

        //SMD: merged in for runuo2.0 networking stuff
        public static int GetAddressValue(IPAddress address)
        {
#pragma warning disable 618
            return (int)address.Address;
#pragma warning restore 618
        }

        public static string Intern(string str)
        {
            if (str == null)
                return null;
            else if (str.Length == 0)
                return String.Empty;

            return String.Intern(str);
        }

        public static void Intern(ref string str)
        {
            str = Intern(str);
        }

        private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

        public static IPAddress Intern(IPAddress ipAddress)
        {
            if (_ipAddressTable == null)
            {
                _ipAddressTable = new Dictionary<IPAddress, IPAddress>();
            }

            IPAddress interned;

            if (!_ipAddressTable.TryGetValue(ipAddress, out interned))
            {
                interned = ipAddress;
                _ipAddressTable[ipAddress] = interned;
            }

            return interned;
        }

        public static void Intern(ref IPAddress ipAddress)
        {
            ipAddress = Intern(ipAddress);
        }

        private static Stack<ConsoleColor> m_ConsoleColors = new Stack<ConsoleColor>();

        public static void PushColor(ConsoleColor color)
        {
            try
            {
                m_ConsoleColors.Push(System.Console.ForegroundColor);
                System.Console.ForegroundColor = color;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void PopColor()
        {
            try
            {
                System.Console.ForegroundColor = m_ConsoleColors.Pop();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        //SMD: end merge


        public static double RandomDouble()
        {
            return m_Random.NextDouble();
        }

        public static bool InRange(Point3D p1, Point3D p2, int range)
        {
            return (p1.m_X >= (p2.m_X - range))
                && (p1.m_X <= (p2.m_X + range))
                && (p1.m_Y >= (p2.m_Y - range))
                && (p1.m_Y <= (p2.m_Y + range));
        }

        public static bool InUpdateRange(Point3D p1, Point3D p2)
        {
            return (p1.m_X >= (p2.m_X - 18))
                && (p1.m_X <= (p2.m_X + 18))
                && (p1.m_Y >= (p2.m_Y - 18))
                && (p1.m_Y <= (p2.m_Y + 18));
        }

        public static bool InUpdateRange(Point2D p1, Point2D p2)
        {
            return (p1.m_X >= (p2.m_X - 18))
                && (p1.m_X <= (p2.m_X + 18))
                && (p1.m_Y >= (p2.m_Y - 18))
                && (p1.m_Y <= (p2.m_Y + 18));
        }

        public static bool InUpdateRange(IPoint2D p1, IPoint2D p2)
        {
            return (p1.X >= (p2.X - 18))
                && (p1.X <= (p2.X + 18))
                && (p1.Y >= (p2.Y - 18))
                && (p1.Y <= (p2.Y + 18));
        }

        public static Direction GetDirection(IPoint2D from, IPoint2D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);

            if (adx >= ady * 3)
            {
                if (dx > 0)
                    return Direction.East;
                else
                    return Direction.West;
            }
            else if (ady >= adx * 3)
            {
                if (dy > 0)
                    return Direction.South;
                else
                    return Direction.North;
            }
            else if (dx > 0)
            {
                if (dy > 0)
                    return Direction.Down;
                else
                    return Direction.Right;
            }
            else
            {
                if (dy > 0)
                    return Direction.Left;
                else
                    return Direction.Up;
            }
        }

        public static bool CanMobileFit(int z, Tile[] tiles)
        {
            int checkHeight = 15;
            int checkZ = z;

            for (int i = 0; i < tiles.Length; ++i)
            {
                Tile tile = tiles[i];

                if (((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))/* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*/ )
                {
                    return false;
                }
                else if (checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInContact(Tile check, Tile[] tiles)
        {
            int checkHeight = check.Height;
            int checkZ = check.Z;

            for (int i = 0; i < tiles.Length; ++i)
            {
                Tile tile = tiles[i];

                if (((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))/* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*/ )
                {
                    return true;
                }
                else if (checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z)
                {
                    return true;
                }
            }

            return false;
        }

        public static object GetArrayCap(Array array, int index)
        {
            return GetArrayCap(array, index, null);
        }

        public static object GetArrayCap(Array array, int index, object emptyValue)
        {
            if (array.Length > 0)
            {
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= array.Length)
                {
                    index = array.Length - 1;
                }

                return array.GetValue(index);
            }
            else
            {
                return emptyValue;
            }
        }

        //4d6+8 would be: Utility.Dice( 4, 6, 8 )
        public static int Dice(int numDice, int numSides, int bonus)
        {
            int total = 0;
            for (int i = 0; i < numDice; ++i)
                total += Random(numSides) + 1;
            total += bonus;
            return total;
        }

        public static int RandomList(params int[] list)
        {
            return list[m_Random.Next(list.Length)];
        }

        public static Item RandomList(params Item[] list)
        {
            return list[m_Random.Next(list.Length)];
        }

        public static bool RandomBool()
        {
            return (m_Random.Next(2) == 0);
        }

        public static int RandomMinMax(int min, int max)
        {
            if (min > max)
            {
                int copy = min;
                min = max;
                max = copy;
            }
            else if (min == max)
            {
                return min;
            }

            return min + m_Random.Next((max - min) + 1);
        }

        public static int Random(int from, int count)
        {
            if (count == 0)
            {
                return from;
            }
            else if (count > 0)
            {
                return from + m_Random.Next(count);
            }
            else
            {
                return from - m_Random.Next(-count);
            }
        }

        public static int Random(int count)
        {
            return m_Random.Next(count);
        }

        public static int RandomNondyedHue()
        {
            switch (Random(6))
            {
                case 0: return RandomPinkHue();
                case 1: return RandomBlueHue();
                case 2: return RandomGreenHue();
                case 3: return RandomOrangeHue();
                case 4: return RandomRedHue();
                case 5: return RandomYellowHue();
            }

            return 0;
        }

        public static int RandomPinkHue()
        {
            return Random(1201, 54);
        }

        public static int RandomBlueHue()
        {
            return Random(1301, 54);
        }

        public static int RandomGreenHue()
        {
            return Random(1401, 54);
        }

        public static int RandomOrangeHue()
        {
            return Random(1501, 54);
        }

        public static int RandomRedHue()
        {
            return Random(1601, 54);
        }

        public static int RandomYellowHue()
        {
            return Random(1701, 54);
        }

        public static int RandomNeutralHue()
        {
            return Random(1801, 108);
        }

        public static int RandomSpecialVioletHue()
        {
            return Utility.RandomList(1230, 1231, 1232, 1233, 1234, 1235);
        }

        public static int RandomSpecialTanHue()
        {
            return Utility.RandomList(1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508);
        }

        public static int RandomSpecialBrownHue()
        {
            return Utility.RandomList(2012, 2013, 2014, 2015, 2016, 2017);
        }

        public static int RandomSpecialDarkBlueHue()
        {
            return Utility.RandomList(1303, 1304, 1305, 1306, 1307, 1308);
        }

        public static int RandomSpecialForestGreenHue()
        {
            return Utility.RandomList(1420, 1421, 1422, 1423, 1424, 1425, 1426);
        }

        public static int RandomSpecialPinkHue()
        {
            return Utility.RandomList(1619, 1620, 1621, 1622, 1623, 1624, 1625, 1626);
        }

        public static int RandomSpecialRedHue()
        {
            return Utility.RandomList(1640, 1641, 1642, 1643, 1644);
        }

        public static int RandomSpecialOliveHue()
        {
            return Utility.RandomList(2001, 2002, 2003, 2004, 2005);
        }

        public static int RandomSnakeHue()
        {
            return Random(2001, 18);
        }

        public static int RandomBirdHue()
        {
            return Random(2101, 30);
        }

        public static int RandomSlimeHue()
        {
            return Random(2201, 24);
        }

        public static int RandomAnimalHue()
        {
            return Random(2301, 18);
        }

        // special dye tub colors
        public static int RandomSpecialHue()
        {
            switch (Utility.Random(8))
            {
                default:
                /* Violet */
                case 0: return Utility.RandomList(1230, 1231, 1232, 1233, 1234, 1235);
                /* Tan */
                case 1: return Utility.RandomList(1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508);
                /* Brown */
                case 2: return Utility.RandomList(2012, 2013, 2014, 2015, 2016, 2017);
                /* Dark Blue */
                case 3: return Utility.RandomList(1303, 1304, 1305, 1306, 1307, 1308);
                /* Forest Green */
                case 4: return Utility.RandomList(1420, 1421, 1422, 1423, 1424, 1425, 1426);
                /* Pink */
                case 5: return Utility.RandomList(1619, 1620, 1621, 1622, 1623, 1624, 1625, 1626);
                /* Red */
                case 6: return Utility.RandomList(1640, 1641, 1642, 1643, 1644);
                /* Olive */
                case 7: return Utility.RandomList(2001, 2002, 2003, 2004, 2005);
            }

        }

        public static int RandomMetalHue()
        {
            return Random(2401, 30);
        }

        public static int ClipDyedHue(int hue)
        {
            if (hue < 2)
                return 2;
            else if (hue > 1001)
                return 1001;
            else
                return hue;
        }

        public static int RandomDyedHue()
        {
            return Random(2, 1000);
        }

        public static int ClipSkinHue(int hue)
        {
            if (hue < 1002)
                return 1002;
            else if (hue > 1058)
                return 1058;
            else
                return hue;
        }

        public static int RandomSkinHue()
        {
            return Random(1002, 57) | 0x8000;
        }

        public static int ClipHairHue(int hue)
        {
            if (hue < 1102)
                return 1102;
            else if (hue > 1149)
                return 1149;
            else
                return hue;
        }

        public static int RandomHairHue()
        {
            return Random(1102, 48);
        }

        private static SkillName[] m_AllSkills = new SkillName[]
            {
                SkillName.Alchemy,
                SkillName.Anatomy,
                SkillName.AnimalLore,
                SkillName.ItemID,
                SkillName.ArmsLore,
                SkillName.Parry,
                SkillName.Begging,
                SkillName.Blacksmith,
                SkillName.Fletching,
                SkillName.Peacemaking,
                SkillName.Camping,
                SkillName.Carpentry,
                SkillName.Cartography,
                SkillName.Cooking,
                SkillName.DetectHidden,
                SkillName.Discordance,
                SkillName.EvalInt,
                SkillName.Healing,
                SkillName.Fishing,
                SkillName.Forensics,
                SkillName.Herding,
                SkillName.Hiding,
                SkillName.Provocation,
                SkillName.Inscribe,
                SkillName.Lockpicking,
                SkillName.Magery,
                SkillName.MagicResist,
                SkillName.Tactics,
                SkillName.Snooping,
                SkillName.Musicianship,
                SkillName.Poisoning,
                SkillName.Archery,
                SkillName.SpiritSpeak,
                SkillName.Stealing,
                SkillName.Tailoring,
                SkillName.AnimalTaming,
                SkillName.TasteID,
                SkillName.Tinkering,
                SkillName.Tracking,
                SkillName.Veterinary,
                SkillName.Swords,
                SkillName.Macing,
                SkillName.Fencing,
                SkillName.Wrestling,
                SkillName.Lumberjacking,
                SkillName.Mining,
                SkillName.Meditation,
                SkillName.Stealth,
                SkillName.RemoveTrap,
                SkillName.Necromancy,
                SkillName.Focus,
                SkillName.Chivalry,
                SkillName.Bushido,
                SkillName.Ninjitsu
            };

        private static SkillName[] m_CombatSkills = new SkillName[]
            {
                SkillName.Archery,
                SkillName.Swords,
                SkillName.Macing,
                SkillName.Fencing,
                SkillName.Wrestling
            };

        private static SkillName[] m_CraftSkills = new SkillName[]
            {
                SkillName.Alchemy,
                SkillName.Blacksmith,
                SkillName.Fletching,
                SkillName.Carpentry,
                SkillName.Cartography,
                SkillName.Cooking,
                SkillName.Inscribe,
                SkillName.Tailoring,
                SkillName.Tinkering
            };

        public static SkillName RandomSkill()
        {
            return m_AllSkills[Utility.Random(m_AllSkills.Length - (Core.SE ? 0 : Core.AOS ? 2 : 5))];
        }

        public static SkillName RandomCombatSkill()
        {
            return m_CombatSkills[Utility.Random(m_CombatSkills.Length)];
        }

        public static SkillName RandomCraftSkill()
        {
            return m_CraftSkills[Utility.Random(m_CraftSkills.Length)];
        }

        public static void FixPoints(ref Point3D top, ref Point3D bottom)
        {
            if (bottom.m_X < top.m_X)
            {
                int swap = top.m_X;
                top.m_X = bottom.m_X;
                bottom.m_X = swap;
            }

            if (bottom.m_Y < top.m_Y)
            {
                int swap = top.m_Y;
                top.m_Y = bottom.m_Y;
                bottom.m_Y = swap;
            }

            if (bottom.m_Z < top.m_Z)
            {
                int swap = top.m_Z;
                top.m_Z = bottom.m_Z;
                bottom.m_Z = swap;
            }
        }

        public static ArrayList BuildArrayList(IEnumerable enumerable)
        {
            IEnumerator e = enumerable.GetEnumerator();

            ArrayList list = new ArrayList();

            while (e.MoveNext())
            {
                list.Add(e.Current);
            }

            return list;
        }

        public static bool RangeCheck(IPoint2D p1, IPoint2D p2, int range)
        {
            return (p1.X >= (p2.X - range))
                && (p1.X <= (p2.X + range))
                && (p1.Y >= (p2.Y - range))
                && (p2.Y <= (p2.Y + range));
        }

        public static void FormatBuffer(TextWriter output, Stream input, int length)
        {
            output.WriteLine("        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F");
            output.WriteLine("       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --");

            int byteIndex = 0;

            int whole = length >> 4;
            int rem = length & 0xF;

            for (int i = 0; i < whole; ++i, byteIndex += 16)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(16);

                for (int j = 0; j < 16; ++j)
                {
                    int c = input.ReadByte();

                    bytes.Append(c.ToString("X2"));

                    if (j != 7)
                    {
                        bytes.Append(' ');
                    }
                    else
                    {
                        bytes.Append("  ");
                    }

                    if (c >= 0x20 && c < 0x80)
                    {
                        chars.Append((char)c);
                    }
                    else
                    {
                        chars.Append('.');
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }

            if (rem != 0)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(rem);

                for (int j = 0; j < 16; ++j)
                {
                    if (j < rem)
                    {
                        int c = input.ReadByte();

                        bytes.Append(c.ToString("X2"));

                        if (j != 7)
                        {
                            bytes.Append(' ');
                        }
                        else
                        {
                            bytes.Append("  ");
                        }

                        if (c >= 0x20 && c < 0x80)
                        {
                            chars.Append((char)c);
                        }
                        else
                        {
                            chars.Append('.');
                        }
                    }
                    else
                    {
                        bytes.Append("   ");
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }
        }

        public static bool NumberBetween(double num, int bound1, int bound2, double allowance)
        {
            if (bound1 > bound2)
            {
                int i = bound1;
                bound1 = bound2;
                bound2 = i;
            }

            return (num < bound2 + allowance && num > bound1 - allowance);
        }

        public static int RandomHair(bool female)   //Random hair doesn't include baldness
        {
            switch (Utility.Random(9))
            {
                case 0: return 0x203B;  //Short
                case 1: return 0x203C;  //Long
                case 2: return 0x203D;  //Pony Tail
                case 3: return 0x2044;  //Mohawk
                case 4: return 0x2045;  //Pageboy
                case 5: return 0x2047;  //Afro
                case 6: return 0x2049;  //Pig tails
                case 7: return 0x204A;  //Krisna
                default: return (female ? 0x2046 : 0x2048); //Buns or Receeding Hair
            }
        }

        public static int RandomFacialHair(bool female)
        {
            if (female)
                return 0;

            int rand = Utility.Random(7);

            return ((rand < 4) ? 0x203E : 0x2047) + rand;
        }

        public static void AssignRandomHair(Mobile m)
        {
            AssignRandomHair(m, true);
        }
        public static void AssignRandomHair(Mobile m, int hue)
        {
            m.HairItemID = RandomHair(m.Female);
            m.HairHue = hue;
        }
        public static void AssignRandomHair(Mobile m, bool randomHue)
        {
            m.HairItemID = RandomHair(m.Female);

            if (randomHue)
                m.HairHue = RandomHairHue();
        }

        public static void AssignRandomFacialHair(Mobile m)
        {
            AssignRandomFacialHair(m, true);
        }
        public static void AssignRandomFacialHair(Mobile m, int hue)
        {
            m.FacialHairHue = RandomFacialHair(m.Female);
            m.FacialHairHue = hue;
        }
        public static void AssignRandomFacialHair(Mobile m, bool randomHue)
        {
            m.FacialHairItemID = RandomFacialHair(m.Female);

            if (randomHue)
                m.FacialHairHue = RandomHairHue();
        }
    }

    #region Item and Mobile Memory
    /*	Item and Mobile Memory
	 *	The Memory system is passive, that is it doesn't use any timers for cleaning up old objects, it
	 *	simply cleans them up the next time it's called before processing the request. The one exception
	 *	to this rule is when you specify a 'Release' callback to be notified when an object expires.
	 *	In this case a timer is created to insure your callback is timely.
	 */
    public class Memory
    {
        public class ObjectMemory
        {
            private object m_context;
            public object Context                                   // return what we remember about this thing
            { get { return m_context; } set { m_context = value; } }
            private object m_object;
            public object Object { get { return m_object; } }       // return the thing we remember
            private double m_seconds;
            private DateTime m_Expiration;
            public DateTime RefreshTime { get { return DateTime.Now + TimeSpan.FromSeconds(m_seconds); } }
            public DateTime Expiration { get { return m_Expiration; } set { m_Expiration = value; } }
            public TimerStateCallback m_OnReleaseEventHandler;
            public TimerStateCallback OnReleaseEventHandler { get { return m_OnReleaseEventHandler; } }

            public ObjectMemory(object ox, double seconds)
                : this(ox, null, seconds)
            {
            }

            public ObjectMemory(object ox, object context, double seconds)
                : this(ox, null, null, seconds)
            {
            }

            public ObjectMemory(object ox, object context, TimerStateCallback ReleaseEventHandler, double seconds)
            {
                m_object = ox;                                  // the thing to remember
                m_seconds = seconds;                            // how long to remember
                m_context = context;                            // what we remember about this object
                m_OnReleaseEventHandler = ReleaseEventHandler;  // release callback
                m_Expiration = RefreshTime;                     // when to delete
            }
        };

        private Hashtable m_MemoryCache = new Hashtable();
        private bool m_Tidying;

        public void TidyMemory()
        {   // we can reenter when we are called back via a user callback.
            //	For instance if the usercallback calls Recal() or anyother function that calls Tidy, we will reenter
            if (m_Tidying == false)
            {
                m_Tidying = true;
                // first clreanup the LOS cache
                ArrayList cleanup = new ArrayList();
                foreach (DictionaryEntry de in m_MemoryCache)
                {   // list expired elements
                    if (de.Value == null) continue;
                    if (DateTime.Now > (de.Value as ObjectMemory).Expiration)
                        cleanup.Add(de.Key as object);
                }

                foreach (object ox in cleanup)
                {   // remove expired elements
                    if (ox == null) continue;
                    if (m_MemoryCache.Contains(ox))
                        Remove(ox);
                }
                m_Tidying = false;
            }
        }

        // you should be calling Forget() to remove objects.
        //	This is called internally
        public void Remove(object ox)
        {   // DO NOT CALL TIDY HERE
            if (m_MemoryCache.Contains(ox))
            {   // call user defined cleanup
                ObjectMemory om = m_MemoryCache[ox] as ObjectMemory;
                if (om.m_OnReleaseEventHandler != null)
                    om.m_OnReleaseEventHandler(om.Context);
                m_MemoryCache.Remove(ox);
            }
        }

        public int Count
        {
            get
            {
                TidyMemory();
                return m_MemoryCache.Count;
            }
        }

        public void Remember(object ox, double seconds)
        {   // preserve the previous context if there was one
            ObjectMemory om = Recall(ox);
            object temp = (om != null) ? om.Context : null;
            Remember(ox, temp, seconds);
        }

        public void Remember(object ox, object context, double seconds)
        {
            Remember(ox, context, null, seconds);
        }

        public void Remember(object ox, object context, TimerStateCallback releaseHandler, double seconds)
        {
            TidyMemory();
            if (ox == null) return;
            if (Recall(ox) != null)
            {   // we already know about this guy - just refresh
                Refresh(ox, context);
                return;
            }
            m_MemoryCache[ox] = new ObjectMemory(ox, context, releaseHandler, seconds);

            if (releaseHandler != null)
                Timer.DelayCall(TimeSpan.FromSeconds(seconds), new TimerStateCallback(Remove), ox);
        }

        public void Forget(object ox)
        {
            TidyMemory();
            if (ox == null) return;
            if (m_MemoryCache.Contains(ox))
                Remove(ox);
        }

        public bool Recall(Mobile mx)
        {
            return Recall(mx as object) != null;
        }

        public ObjectMemory Recall(object ox)
        {
            TidyMemory();
            if (ox == null) return null;
            if (m_MemoryCache.Contains(ox))
                return m_MemoryCache[ox] as ObjectMemory;
            return null;
        }

        public void Refresh(object ox, object context)
        {
            TidyMemory();
            if (ox == null) return;
            if (m_MemoryCache.Contains(ox))
            {
                (m_MemoryCache[ox] as ObjectMemory).Expiration = (m_MemoryCache[ox] as ObjectMemory).RefreshTime;
                (m_MemoryCache[ox] as ObjectMemory).Context = context;
            }
        }

        public void Refresh(object ox)
        {
            // preserve the previous context if there was one
            ObjectMemory om = Recall(ox);
            object temp = (om != null) ? om.Context : null;
            Refresh(ox, temp);
        }
    }
    #endregion Item and Mobile Memory

    public class AdjustedDateTime
    {
        private DateTime m_DateTime = DateTime.MinValue;
        private bool m_IsDuringDST = false;

        public AdjustedDateTime(DateTime datetime)
        {
            if (datetime == DateTime.MinValue ||
                datetime == DateTime.MaxValue)
            {
                m_DateTime = datetime;
            }
            else
            {
                m_DateTime = ConvertDateTimeToAdjustedTime(datetime, ref m_IsDuringDST);
            }
        }

        public bool IsDuringDST
        {
            get { return m_IsDuringDST; }
        }

        public DateTime Value
        {
            get { return m_DateTime; }
        }

        public string TZName
        {
            get
            {
                if (m_IsDuringDST)
                {
                    return TimeZone.CurrentTimeZone.DaylightName;
                }
                else
                {
                    return TimeZone.CurrentTimeZone.StandardName;
                }
            }
        }

        private static DateTime ConvertDateTimeToAdjustedTime(DateTime dt, ref bool isDuringDST)
        {
            DateTime returnTime = dt;

            try
            {
                bool bError = false;

                if (dt.IsDaylightSavingTime())
                {
                    isDuringDST = true;
                }
                else
                {
                    DateTime beginDST = new DateTime(dt.Year, 3, 1, 2, 0, 0);
                    bool foundfirstsunday = false;
                    bool foundsecondsunday = false;
                    while (!foundsecondsunday && !bError)
                    {
                        if (beginDST.Month != 3)
                        {
                            bError = true;
                        }
                        else
                        {
                            if (beginDST.DayOfWeek == 0)
                            {
                                if (!foundfirstsunday)
                                {
                                    foundfirstsunday = true;
                                    beginDST = beginDST.AddDays(1.0);
                                }
                                else
                                {
                                    foundsecondsunday = true;
                                }
                            }
                            else
                            {
                                beginDST = beginDST.AddDays(1.0);
                            }
                        }
                    }
                    DateTime endDST = new DateTime(dt.Year, 11, 1, 2, 0, 0);
                    foundfirstsunday = false;
                    while (!foundfirstsunday && !bError)
                    {
                        if (endDST.Month != 11)
                        {
                            bError = true;
                        }
                        else
                        {
                            if (endDST.DayOfWeek == 0)
                            {
                                foundfirstsunday = true;
                            }
                            else
                            {
                                endDST = endDST.AddDays(1.0);
                            }
                        }
                    }

                    if (!bError && beginDST <= dt && endDST > DateTime.Now)
                    {
                        returnTime = dt.AddHours(1.0);
                        isDuringDST = true;
                        //MessageBox.Show("yes: " + now.AddHours(1.0).ToLongTimeString());
                    }
                    else
                    {
                        //MessageBox.Show("no: " + now.ToLongTimeString());
                    }
                }
            }
            catch (Exception e)
            {
                //LogHelper.LogException(e); (not in server)
                System.Console.WriteLine("EXCEPTION IN AdjustedDateTime: " + e.Message);
            }

            return returnTime;
        }
    }
}
