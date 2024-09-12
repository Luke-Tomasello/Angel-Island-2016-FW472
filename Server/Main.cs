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

/* Server\Main.cs
 * ChangeLog:
 *	8/30/2024, Adam
 *	    1. Add colorization to startup text
 *	    2. Check for bad or missing Ports.json
 */

using Server.Commands;
using Server.Misc;
using Server.Network;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Server
{
    public delegate void Slice();
    [CustomEnum(new string[] { "Pre-Alpha", "Alpha", "Pre-Beta", "Beta", "Production" })]
    public enum ReleasePhase
    {
        Pre_Alpha,
        Alpha,
        Pre_Beta,
        Beta,
        Production
    }
    public class Core
    {
        private static bool m_Crashed;
        private static Thread timerThread;
        private static string m_BaseDirectory;
        private static string m_DataDirectory;
        private static string m_ExePath;
        private static ArrayList m_DataDirectories = new ArrayList();
        private static Assembly m_Assembly;
        private static Process m_Process;
        private static Thread m_Thread;
        private static bool m_Service;
        private static bool m_Debug;
        private static bool m_Cache = true;
        private static bool m_HaltOnWarning;
        private static bool m_VBdotNET;
        private static MultiTextWriter m_MultiConOut;
        private static bool m_Quiet;

        #region Error Log Shortcuts
        public static class LoggerShortcuts
        {
            public static string Boot = "ShardBoot.log";
            public static void BootError(string text)
            {
                LogHelper logger = new LogHelper(Boot, false, true);
                logger.Log(text);
                logger.Finish();
                Utility.Monitor.WriteLine(text, ConsoleColor.Red);
            }
        }
        #endregion Error Log Shortcuts

        private static bool m_AOS;
        private static bool m_SE;
        private static bool m_ML;
        private static bool m_UOSP;                     // Siege
        private static bool m_UOTC;                     // Test Center
        private static bool m_UOAI;                     // Angel Island
        private static bool m_UOREN;                    // Renaissance
        private static bool m_UOMO;                     // Mortalis
        private static bool m_UOEV;                     // Event Shard
        private static int m_HITMISS;                   // special flag to allow test center players to view their hit/miss stats over time
        private static int m_DAMAGE;                    // special flag to allow test center players to view their damage dealt stats over time
        private static bool m_Building;                 // gives GMs access to certain world building commands during world construction
        private static bool m_Developer;                // developers machine, allows direct login to any server

        public static class RuleSets
        {

            #region Shards
            public static bool AngelIslandRules()
            { return m_UOAI; }
            public static bool TestCenterRules()
            { return m_UOTC; }
            public static bool MortalisRules()
            { return m_UOMO; }
            public static bool EventShardRules()
            { return m_UOEV; }
            public static bool SiegeRules()
            { return m_UOSP; }
            public static bool RenaissanceRules()
            { return m_UOREN; }
            public static bool LoginServerRules()
            {
                // not really right. In the AI 7, we have an explicit login server.
                //  Here, AI doubles as a shard and a login server.
#if GMN         // defined in GMN Core 3.0.csproj.user
                // never a login server on game-master.net
                return false;
#else
                return Core.UOAI && !Core.UOTC; 
#endif
            }
            public static bool StandardShardRules()
            {
                return SiegeRules() || MortalisRules() || RenaissanceRules();
            }
            #endregion Shards

            #region Rules
            public static bool TentAnnexation()
            {
                return (m_UOAI || m_UOSP) && CoreAI.TentAnnexation;
            }
            #endregion Rules
        }

        private static bool m_Profiling;
        private static DateTime m_ProfileStart;
        private static TimeSpan m_ProfileTime;

        private static bool m_Patching = true;          // default is to run the patcher at startup.
        public static bool Patching
        {
            get { return m_Patching; }
            set { m_Patching = value; }
        }

        private static MessagePump m_MessagePump;

        public static MessagePump MessagePump
        {
            get { return m_MessagePump; }
            set { m_MessagePump = value; }
        }

        public static Slice Slice;

        public static bool Profiling
        {
            get { return m_Profiling; }
            set
            {
                if (m_Profiling == value)
                    return;

                m_Profiling = value;

                if (m_ProfileStart > DateTime.MinValue)
                    m_ProfileTime += DateTime.UtcNow - m_ProfileStart;

                m_ProfileStart = (m_Profiling ? DateTime.UtcNow : DateTime.MinValue);
            }
        }

        public static TimeSpan ProfileTime
        {
            get
            {
                if (m_ProfileStart > DateTime.MinValue)
                    return m_ProfileTime + (DateTime.UtcNow - m_ProfileStart);

                return m_ProfileTime;
            }
        }

#if !DEBUG
		public static bool Debug { get { return m_Debug; } }
#else
        public static bool Debug { get { return true; } }
#endif

        public static bool Service { get { return m_Service; } }
        public static bool Developer { get { return m_Developer; } }                                    //Disallow direct logins to other servers if we are not a developer
        internal static bool HaltOnWarning { get { return m_HaltOnWarning; } }
        internal static bool VBdotNet { get { return m_VBdotNET; } }
        public static ArrayList DataDirectories { get { return m_DataDirectories; } }
        public static Assembly Assembly { get { return m_Assembly; } set { m_Assembly = value; } }
        public static Process Process { get { return m_Process; } }
        public static Thread Thread { get { return m_Thread; } }
        public static MultiTextWriter MultiConsoleOut { get { return m_MultiConOut; } }

        // import a non-encrypted world
        private static bool m_Import = false;
        public static bool Import { get { return m_Import; } }

        private static string m_Server;
        public static string Server { get { return m_Server; } }

        // perform a onetime upgrade from old non-locking boat holds to the new locking ones
        private static bool m_BoatHoldUpgrade = false;
        public static bool BoatHoldUpgrade { get { return m_BoatHoldUpgrade; } }

        public static readonly bool Is64Bit = (IntPtr.Size == 8);
        #region Time Management
        /* 
        * DateTime.Now and DateTime.UtcNow are based on actual system clock time.
        * The resolution is acceptable but large clock jumps are possible and cause issues.
        * GetTickCount and GetTickCount64 have poor resolution.
        * GetTickCount64 is unavailable on Windows XP and Windows Server 2003.
        * Stopwatch.GetTimestamp() (QueryPerformanceCounter) is high resolution, but
        * somewhat expensive to call because of its deference to DateTime.Now,
        * which is why Stopwatch has been used to verify HRT before calling GetTimestamp(),
        * enabling the usage of DateTime.UtcNow instead.
        */

        private static readonly bool _HighRes = Stopwatch.IsHighResolution;

        private static readonly double _HighFrequency = 1000.0 / Stopwatch.Frequency;
        private static readonly double _LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;

        private static bool _UseHRT;

        public static bool UsingHighResolutionTiming { get { return _UseHRT && _HighRes && !Unix; } }

        public static long TickCount { get { return (long)Ticks; } }

        public static double Ticks
        {
            get
            {
                if (_UseHRT && _HighRes && !Unix)
                {
                    return Stopwatch.GetTimestamp() * _HighFrequency;
                }

                return DateTime.UtcNow.Ticks * _LowFrequency;
            }
        }
        #endregion Time Management
        private static bool m_MultiProcessor;
        private static int m_ProcessorCount;

        public static bool MultiProcessor { get { return m_MultiProcessor; } }
        public static int ProcessorCount { get { return m_ProcessorCount; } }

        private static bool m_Unix;

        public static bool Unix { get { return m_Unix; } }

        public static string FindDataFile(string path)
        {
            if (m_DataDirectories.Count == 0)
                throw new InvalidOperationException("Attempted to FindDataFile before DataDirectories list has been filled.");

            string fullPath = null;

            for (int i = 0; i < m_DataDirectories.Count; ++i)
            {
                fullPath = Path.Combine((string)m_DataDirectories[i], path);

                if (File.Exists(fullPath))
                    break;

                fullPath = null;
            }

            return fullPath;
        }

        public static string FindDataFile(string format, params object[] args)
        {
            return FindDataFile(String.Format(format, args));
        }

        private static Expansion m_Expansion;
        public static Expansion Expansion
        {
            get { return m_Expansion; }
            set { m_Expansion = value; }
        }

        // Scenario 4: Plague of Despair
        // http://www.uoguide.com/List_of_BNN_Articles_(2002)
        public static DateTime PlagueOfDespair
        {
            get
            {
                // Enemies and Allies - April 11
                return new DateTime(2002, 4, 11);
            }
        }


        // http://www.uoguide.com/Savage_Empire
        // http://uo.stratics.com/database/view.php?db_content=hunters&id=176
        // Savage Empire was the title of an EA-run UO scenario, active from May to July of 2001.
        public static DateTime EraSAVE  // Savage Empire active from May to July of 2001. 
        {
            get
            {
                return new DateTime(2001, 5, 1);
            }
        }

        public static DateTime EraSA    // The Second Age (October 1, 1998) 
        {
            get
            {
                return new DateTime(1998, 10, 1);
            }
        }
        public static DateTime EraREN   // Renaissance (May 4, 2000)
        {
            get
            {
                return new DateTime(2000, 5, 4);
            }
        }
        public static DateTime EraTD    // Third Dawn (March 7, 2001)
        {
            get
            {
                return new DateTime(2001, 3, 7);
            }
        }
        public static DateTime EraLBR   // Lord Blackthorn's Revenge (February 24, 2002)
        {
            get
            {
                return new DateTime(2002, 2, 24);
            }
        }
        public static DateTime EraAOS   // Age of Shadows (February 11, 2003)
        {
            get
            {
                return new DateTime(2003, 2, 11);
            }
        }
        public static DateTime EraSE    // Samurai Empire (November 2, 2004) 
        {
            get
            {
                return new DateTime(2004, 11, 2);
            }
        }
        public static DateTime EraML    // Mondain's Legacy (August 30, 2005) 
        {
            get
            {
                return new DateTime(2005, 8, 30);
            }
        }

        public static DateTime EraABYSS // Stygian Abyss (September 8, 2009) 
        {
            get
            {
                return new DateTime(2009, 9, 9);
            }
        }

        public static DateTime LocalizationUO   // I think this was UO Third Dawn
        {
            get
            {
                return EraTD;
            }
        }

        /// <summary>
        /// Use this for deciding between beautiful old-school UO gumps and the new style gumps designed to hold variable length text.
        /// We believe it was UO Third Dawn that saw the massive Localization changes. With these changes came the ugly gumps to ensure vatiable
        /// length text would fit. 
        /// </summary>
        public static bool Localized    // I think this was UO Third Dawn
        {
            get
            {
                return PublishInfo.PublishDate >= LocalizationUO;
            }
        }

        /// <summary>
        /// Without naming a shard, describes whether this shard is attempting era accuracy.
        /// </summary>
        public static bool EraAccurate
        {
            get
            {   // add your Era Accurate shards here
                return UOSP;
            }
        }

        /// <summary>
        /// Inclusive
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>true if this publish is active</returns>
        public static bool CheckPublish(double from, double to)
        {   // Note: this test is inclusive. The 'to' date needs be the last valid publish for this test.
            // For example. If purple wisps were only valid from publish 5 - 7, then the CheckPublish(5,7) would return true
            //	The 'to' date should NOT be the publish in which something became invalid.
            return PublishInfo.Publish >= from && PublishInfo.Publish <= to;
        }

        /// <summary>
        /// Is the named publish active?
        /// </summary>
        /// <param name="pub"></param>
        /// <returns>true if this publish is active</returns>
        public static bool CheckPublish(double pub)
        {
            return CheckPublish(pub, pub);
        }

        public static bool OldEthics
        {
            get
            {
                return Core.UOSP && PublishInfo.Publish < 13.6;
            }
        }

        public static bool NewEthics
        {
            get
            {
                return !OldEthics;
            }
        }
        #region Damage Tracker
        public static int HITMISS
        {
            get
            {
                return m_HITMISS;
            }
            set
            {
                m_HITMISS = value;
            }
        }
        public static int DAMAGE
        {
            get
            {
                return m_DAMAGE;
            }
            set
            {
                m_DAMAGE = value;
            }
        }
        #endregion Damage tracker
        private static ReleasePhase m_releasePhase;
        public static ReleasePhase ReleasePhase
        {
            get { return m_releasePhase; }
            set { m_releasePhase = value; }
        }

        public static bool T2A
        {   // is T2A available to this shard?
            get
            {
                return Core.UOSP;
            }
        }

        public static bool OldStyleTinkerTrap
        {
            get
            {
                return PublishInfo.Publish < 4 || Core.UOAI || Core.UOREN;
            }
        }

        public static bool NewStyleTinkerTrap
        {
            get
            {
                return !Core.OldStyleTinkerTrap;
            }
        }

        public static bool Factions
        {
            get
            {   // add your factions enabled servers here
                return Core.UOSP && PublishInfo.Publish >= 8.0;
            }
        }

        public static bool Ethics
        {
            get
            {
                // Siege Perilous is a special ruleset shard that launched on July 15, 1999. 
                return Core.UOSP && PublishInfo.PublishDate >= new DateTime(1999, 7, 15);
            }
        }

        public static bool UOEV
        {
            get
            {
                return m_UOEV;
            }
        }

        public static bool UOTC
        {
            get
            {
                return m_UOTC;
            }
        }

        public static bool UOAI
        {
            get
            {
                return m_UOAI;
            }
        }

        /// <summary>
        /// Publish 5 UO Siege Perilous
        /// </summary>
        public static bool UOSP
        {
            get
            {
                return m_UOSP;
            }
        }

        public static bool UOREN
        {
            get
            {
                return m_UOREN;
            }
        }

        public static bool UOMO
        {
            get
            {
                return m_UOMO;
            }
        }

        public static bool Building
        {
            get
            {
                return m_Building;
            }
        }

        public static bool AOS
        {
            get
            {
                return m_AOS || m_SE;
            }
            set
            {
                m_AOS = value;
            }
        }

        public static bool SE
        {
            get
            {
                return m_SE;
            }
            set
            {
                m_SE = value;
            }
        }

        public static bool ML
        {
            get
            {
                return m_ML;
            }
            set
            {
                m_ML = value;
            }
        }

        public static string ExePath
        {
            get
            {
                if (m_ExePath == null)
                    m_ExePath = Process.GetCurrentProcess().MainModule.FileName.Replace("vshost.", "");

                return m_ExePath;
            }
        }
        public static string BaseDirectory
        {
            get
            {
                if (m_BaseDirectory == null)
                {
                    try
                    {
                        m_BaseDirectory = ExePath;

                        if (m_BaseDirectory.Length > 0)
                            m_BaseDirectory = Path.GetDirectoryName(m_BaseDirectory);
                    }
                    catch
                    {
                        m_BaseDirectory = "";
                    }
                }

                return m_BaseDirectory;
            }
        }
        public static string DataDirectory
        {
            get
            {
                if (m_DataDirectory == null)
                {
                    try
                    {
                        bool isDevelopmerMachine = false;
                        if (BaseDirectory.ToLower().Contains(@"\debug\") || BaseDirectory.ToLower().Contains(@"\release\"))
                            isDevelopmerMachine = true;

                        if (isDevelopmerMachine)
                            m_DataDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, "../../../", "Data"));
                        else
                            m_DataDirectory = Path.GetFullPath(Path.Combine(BaseDirectory, "Data"));
                    }
                    catch
                    {
                        // don't default this. we want the system to blow up if either the developer's or production machine is not setup correctly
                    }
                }

                return m_DataDirectory;
            }
        }
        private static string m_LogsDirectory;
        public static string LogsDirectory
        {
            get
            {
                if (m_LogsDirectory == null)
                {
                    try
                    {
                        return m_LogsDirectory = Path.Combine(BaseDirectory, "Logs");
                    }
                    catch
                    {
                        // don't default this. we want the system to blow up if either the developer's or production machine is not setup correctly
                    }
                }

                return m_LogsDirectory;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
            Console.WriteLine(e.ExceptionObject);

            if (e.IsTerminating)
            {
                m_Crashed = true;

                bool close = false;

                try
                {
                    CrashedEventArgs args = new CrashedEventArgs(e.ExceptionObject as Exception);

                    EventSink.InvokeCrashed(args);

                    close = args.Close;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                if (!close && !m_Service)
                {
                    try
                    {
                        for (int i = 0; i < m_MessagePump.Listeners.Length; i++)
                        {
                            m_MessagePump.Listeners[i].Dispose();
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    if (SocketPool.Created)
                        SocketPool.Destroy();

                    Console.WriteLine("This exception is fatal, press return to exit");
                    Console.ReadLine();
                }

                m_Closing = true;
            }
        }

        private enum ConsoleEventType
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private delegate bool ConsoleEventHandler(ConsoleEventType type);
        private static ConsoleEventHandler m_ConsoleEventHandler;

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventHandler callback, bool add);

        private static bool OnConsoleEvent(ConsoleEventType type)
        {
            if (World.Saving || (m_Service && type == ConsoleEventType.CTRL_LOGOFF_EVENT))
                return true;

            Kill();

            return true;
        }

        #region HIDE_CLOSEBOX
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);

        internal const uint SC_CLOSE = 0xF060;
        internal const uint MF_GRAYED = 0x00000001;
        internal const uint MF_BYCOMMAND = 0x00000000;
        #endregion HIDE_CLOSEBOX

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            HandleClosed();
        }

        private static bool m_Closing;
        public static bool Closing { get { return m_Closing; } }

        public static void Kill()
        {
            Kill(false);
        }

        public static void Kill(bool restart)
        {
            HandleClosed();

            if (restart)
                Process.Start(ExePath, Arguments);

            m_Process.Kill();
        }

        private static void HandleClosed()
        {
            if (m_Closing)
                return;

            m_Closing = true;

            Console.Write("Exiting...");

            if (!m_Crashed)
                EventSink.InvokeShutdown(new ShutdownEventArgs());

            if (SocketPool.Created)
                SocketPool.Destroy();

            Timer.TimerThread.Set();

            Console.WriteLine("done");
        }

        private static AutoResetEvent m_Signal = new AutoResetEvent(true);
        public static void Set() { m_Signal.Set(); }

        public static void Main(string[] args)
        {
#if !DEBUG
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            #region ESTABLISH CWD
            {   // in development, VS places the exe in something like

            }
            #endregion ESTABLISH CWD

            #region HIDE_CLOSEBOX
#if !DEBUG
			IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;
			IntPtr hSystemMenu = GetSystemMenu(hMenu, false);
			EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
			RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
#endif
            #endregion HIDE_CLOSEBOX

            #region ARG PARSING
            m_releasePhase = ReleasePhase.Production;
            Arguments = "";
            for (int i = 0; i < args.Length; ++i)
            {
                if (Insensitive.Equals(args[i], "-debug"))
                    m_Debug = true;
                else if (Insensitive.Equals(args[i], "-service"))
                    m_Service = true;
                else if (Insensitive.Equals(args[i], "-profile"))
                    Profiling = true;
                else if (Insensitive.Equals(args[i], "-nocache"))
                    m_Cache = false;
                else if (Insensitive.Equals(args[i], "-haltonwarning"))
                    m_HaltOnWarning = true;
                else if (Insensitive.Equals(args[i], "-vb"))
                    m_VBdotNET = true;
                else if (Insensitive.Equals(args[i], "-import"))
                    m_Import = true;
                else if (Insensitive.Equals(args[i], "-boatholdupgrade"))
                    m_BoatHoldUpgrade = true;
                else if (Insensitive.Equals(args[i], "-uotc"))
                    m_UOTC = true; //
                else if (Insensitive.Equals(args[i], "-uosp"))
                    m_UOSP = true;
                else if (Insensitive.Equals(args[i], "-uoren"))
                    m_UOREN = true;
                else if (Insensitive.Equals(args[i], "-uomo"))
                    m_UOMO = true;
                else if (Insensitive.Equals(args[i], "-uoai"))
                    m_UOAI = true;
                else if (Insensitive.Equals(args[i], "-uoev"))
                    m_UOEV = true;
                else if (Insensitive.Equals(args[i], "-build"))
                    m_Building = true;
                else if (Insensitive.Equals(args[i], "-nopatch"))
                    m_Patching = false;
                else if (Insensitive.Equals(args[i], "-developer"))
                    m_Developer = true;
                else if (Insensitive.Equals(args[i], "-beta"))
                    m_releasePhase = ReleasePhase.Beta;
                
                Arguments += args[i] + " ";
            }
            #endregion

            #region VERIFY ARGS
            int server_count = 0;
            if (m_UOAI == true) server_count++;
            if (m_UOSP == true) server_count++;
            if (m_UOREN == true) server_count++;
            if (m_UOMO == true) server_count++;
            if (server_count == 0)
            {
                Console.WriteLine("Core: No server specified, defaulting to Angel Island");
                m_UOAI = true;
            }
            if (server_count > 1)
            {
                Console.WriteLine("Core: Too many servers specified.");
                return;
            }
            #endregion

            #region LOG SETUP
            try
            {
                if (m_Service)
                {
                    if (!Directory.Exists("Logs"))
                        Directory.CreateDirectory("Logs");

                    Console.SetOut(m_MultiConOut = new MultiTextWriter(Console.Out, new FileLogger("Logs/Console.log")));
                }
                else
                {
                    Console.SetOut(m_MultiConOut = new MultiTextWriter(Console.Out));
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            #endregion

            m_Thread = Thread.CurrentThread;
            m_Process = Process.GetCurrentProcess();
            m_Assembly = Assembly.GetEntryAssembly();

            if (m_Thread != null)
                m_Thread.Name = "Core Thread";

            if (BaseDirectory.Length > 0)
                Directory.SetCurrentDirectory(BaseDirectory);

            bool state = false;
            
            #region Server Name
            if (Core.UOSP)
            {
                m_Server = "Siege Perilous";
            }
            else if (Core.UOMO)
            {
                m_Server = "Mortalis";
            }
            else if (Core.RuleSets.LoginServerRules())
            {
                m_Server = "Login Server";
            }
            else if (Core.UOREN)
            {
                m_Server = "Renaissance";
            }
            else if (Core.UOAI)
            {
                m_Server = "Angel Island";
            }
            else
                m_Server = "Unknown Configuration";
            #endregion Server Name

            Utility.Monitor.WriteLine("{0}{5} - [www.game-master.net] Version {1}.{2}.{3}, Build {4}",
                Utility.BuildColor(Utility.BuildRevision()), m_Server, Utility.BuildMajor(), Utility.BuildMinor(),
                Utility.BuildRevision(), Utility.BuildBuild(),
                Core.ReleasePhase < ReleasePhase.Production ? string.Format(" ({0})",
                Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)m_releasePhase]) : "");

            //if (RuleSets.UOSP_SVR)
            //    Utility.Monitor.WriteLine("[Siege II is {0}.]", ConsoleColorEnabled(Core.SiegeII_CFG), TextEnabled(Core.SiegeII_CFG));
            Utility.Monitor.WriteLine("[UO Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(DataPath.GetUOPath("Ultima Online")));
            Utility.Monitor.WriteLine("[Current Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(Directory.GetCurrentDirectory()));
            Utility.Monitor.WriteLine("[Base Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(BaseDirectory));
            Utility.Monitor.WriteLine("[Data Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(DataDirectory));
            //Utility.Monitor.WriteLine("[Shared Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(SharedDirectory));
            Utility.Monitor.WriteLine("[Game Time Zone: {0}]", ConsoleColorInformational(), AdjustedDateTime.GameTimezone);
            Utility.Monitor.WriteLine("[Server Time Zone: {0}]", ConsoleColorInformational(), AdjustedDateTime.ServerTimezone);
            
            #region ZLib
            bool zlib_loaded = false;
            try
            {
                zlib_loaded = (Compression.Compressor.Version != null);
            }
            catch (Exception ex)
            {
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing or cannot be loaded.", "zlib"));
            }

            state = zlib_loaded;
            Utility.Monitor.WriteLine("[ZLib version {0} ({1}) loaded.]", ConsoleColorInformational(), Compression.Compressor.Version, Compression.Compressor.GetType().Name);
            #endregion ZLib

            #region Email
            if (EmailCheck() == true)
                Utility.Monitor.WriteLine("[All of the required Email environment variables are set.]", ConsoleColorInformational());
            else
                Utility.Monitor.WriteLine("[Some or all of the required Email environment variables are not set.]", ConsoleColorWarning());
            #endregion Email

            #region Ports
            if (PortsCheck() == false)
            {
                Utility.Monitor.WriteLine("[Ports not configured. Using defaults.]", ConsoleColorWarning());
                if (DefaultPorts() == false)
                {
                    Utility.Monitor.WriteLine("Unable to find Ports.json. Press return to exit.", ConsoleColorWarning());
                    Console.ReadKey();
                    return;
                }
            }
            #endregion Ports

            #region BuildInfo 
            if (BuildInfoCheck() == true)
                Utility.Monitor.WriteLine($"[Reading build.info from {Utility.GetShortPath(BuildInfoDir, raw: true)}.]", ConsoleColorInformational());
            else
                Utility.Monitor.WriteLine($"[Reading build.info from default location {BuildInfoDir}.]", ConsoleColorWarning());
            #endregion BuildInfo

            #region GeoIP
            //if (GeoIPCheck() == true)
            //    Utility.Monitor.WriteLine("[Geo IP Configured.]", ConsoleColorInformational());
            //else
            //    Utility.Monitor.WriteLine("[Geo IP is not configured. See AccountHandler.cs for setup instructions.]", ConsoleColorWarning());
            #endregion GeoIP

            #region Boot Errors
            if (Directory.Exists(Path.Combine(Core.DataDirectory)) == false)
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing.", Core.DataDirectory));

            if (File.Exists(Path.Combine(Core.BuildInfoDir, "Core 3.info")) == false)
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing.", Path.Combine(Core.BuildInfoDir, "Core 6.info")));

            #region Check Login DB
#if false
            if (m_useLoginDB)
            {
                bool adError = false;
                bool ipeError = false;
                bool fwError = false;
                string adPath = AccountsDatabase.GetDatabasePath(ref adError);
                string ipePath = IPExceptionDatabase.GetDatabasePath(ref ipeError);
                string fwPath = FirewallDatabase.GetDatabasePath(ref fwError);
                if (adError || ipeError || fwError)
                {
                    if (adError)
                        Console.WriteLine($"Unable to create {adPath}");

                    if (ipeError)
                        Console.WriteLine($"Unable to create {ipePath}");

                    if (fwError)
                        Utility.Monitor.WriteLine($"Unable to create {fwPath}", ConsoleColor.Red);

                    Utility.Monitor.WriteLine($"Use the following environment variables to relocate the database(s)", ConsoleColor.Yellow);
                    Utility.Monitor.WriteLine("AI.IPEXCEPTIONDB, AI.FIREWALLDB, AI.LOGINDB", ConsoleColor.Yellow);

                    while (true)
                    {
                        Utility.Monitor.WriteLine("Insufficient privileges to create one or more databases.", ConsoleColor.Yellow);
                        Utility.Monitor.WriteLine("Press 'c' to continue without axillary database support, or 'q' to quit.", ConsoleColor.Yellow);

                        string input = Console.ReadLine().ToLower();
                        if (input.StartsWith("c"))
                        {
                            m_useLoginDB = false;
                            break;
                        }
                        else if (input.StartsWith("q"))
                        {
                            return;
                        }
                    }
                }
            }
#endif
            #endregion Check Login DB

            #endregion Boot Errors
#if DEBUG
            Utility.Monitor.WriteLine("[Debug Build Enabled]", ConsoleColorInformational());
#else
            Utility.Monitor.WriteLine("[Release Build Enabled]", ConsoleColorInformational());
#endif

#if DEBUG
            //  Turn off saves for DEBUG builds
            AutoSave.SavesEnabled = false;
#endif
            state = AutoSave.SavesEnabled;
            Utility.Monitor.WriteLine("[Saves are {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            //state = m_useLoginDB;
            //Utility.Monitor.WriteLine("[Using login database {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            Utility.Monitor.WriteLine("[Shard configuration is {0}.]", ConsoleColorInformational(), m_Server);
            state = true;//RuleSets.ResourcePoolRules();
            Utility.Monitor.WriteLine("[Resource Pool is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.m_UOTC;
            Utility.Monitor.WriteLine("[Test Center functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.RuleSets.LoginServerRules();
            Utility.Monitor.WriteLine("[Login Server functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = m_releasePhase == ReleasePhase.Beta;
            Utility.Monitor.WriteLine("[Beta functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = World.FreezeDryEnabled;
            Utility.Monitor.WriteLine("[Freeze dry system is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.m_UOEV;
            Utility.Monitor.WriteLine("[Event Shard functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            Utility.Monitor.WriteLine("[Publish {0} enabled ({1}).]", ConsoleColorInformational(), PublishInfo.Publish, PublishInfo.PublishDate);
            state = Core.Building;
            Utility.Monitor.WriteLine("[World building is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));

            // Disabling developer mode for now (for custom house building)
            // state = Core.Developer;
            //Utility.ConsoleOut("[Developer mode is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            
            state = Core.Factions;
            Utility.Monitor.WriteLine("[Factions are {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.T2A;
            Utility.Monitor.WriteLine("[T2A is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));

            if (Arguments.Length > 0)
                Console.WriteLine("Core: Running with arguments: {0}", Arguments);

            m_ProcessorCount = Environment.ProcessorCount;

            if (m_ProcessorCount > 1)
                m_MultiProcessor = true;

            if (m_MultiProcessor || Is64Bit)
                Console.WriteLine("Core: Optimizing for {0} {2}processor{1}", m_ProcessorCount, m_ProcessorCount == 1 ? "" : "s", Is64Bit ? "64-bit " : "");

            int platform = (int)Environment.OSVersion.Platform;
            if ((platform == 4) || (platform == 128))
            { // MS 4, MONO 128
                m_Unix = true;
                Console.WriteLine("Core: Unix environment detected");
            }
            else
            {
                m_ConsoleEventHandler = new ConsoleEventHandler(OnConsoleEvent);
                SetConsoleCtrlHandler(m_ConsoleEventHandler, true);
            }
#if false
            // we don't use the RunUO system for debugging scripts
            while (!ScriptCompiler.Compile(m_Debug))
            {
                if (m_Quiet) //abort and exit if compile scripts failed
                    return;

                Console.WriteLine("Scripts: One or more scripts failed to compile or no script files were found.");
                Console.WriteLine(" - Press return to exit, or R to try again.");

                string line = Console.ReadLine();
                if (line == null || line.ToLower() != "r")
                    return;
            }
#endif
            // adam: I believe the new startup logic is more robust as it attempts to prevents timers from firing 
            //  before the shard is fully up and alive.
            AIWorldBoot aiWorldBoot = new AIWorldBoot();
            aiWorldBoot.Configure();
            aiWorldBoot.WorldLoad();
            aiWorldBoot.Initialize();
            //aiWorldBoot.ObjectInitialize(); // not yet available in core 3

            // this timer (and output) simply proves timers created during Configure, WorldLoad, and Initialize will be
            // respected, and processed as planned. 
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { null });

            Region.Load();

            SocketPool.Create();

            Timer.TimerThread ttObj = new Timer.TimerThread();
            timerThread = new Thread(new ThreadStart(ttObj.TimerMain));
            timerThread.Name = "Timer Thread";

            MessagePump ms = m_MessagePump = new MessagePump();

            timerThread.Start();

            for (int i = 0; i < Map.AllMaps.Count; ++i)
                ((Map)Map.AllMaps[i]).Tiles.Force();

            NetState.Initialize();

            EventSink.InvokeServerStarted();

#if !DEBUG
			try
			{
#endif
            while (m_Signal.WaitOne())
            {
                Mobile.ProcessDeltaQueue();
                Item.ProcessDeltaQueue();

                Timer.Slice();
                m_MessagePump.Slice();

                NetState.FlushAll();
                NetState.ProcessDisposedQueue();

                if (Slice != null)
                    Slice();
            }
#if !DEBUG
			}
			catch (Exception e)
			{
				CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
			}
#endif

        }
        public static ConsoleColor ConsoleColorEnabled(bool enabled)
        {
            if (enabled)
                return ConsoleColor.Green;
            else
                return ConsoleColor.Yellow;
        }
        public static ConsoleColor ConsoleColorInformational()
        {
            return ConsoleColor.Green;
        }
        public static ConsoleColor ConsoleColorWarning()
        {
            return ConsoleColor.Red;
        }
        public static string TextEnabled(bool enabled)
        {
            if (enabled)
                return "Enabled";
            else
                return "Disabled";
        }
        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            Utility.Monitor.WriteLine("Timers initialized", ConsoleColor.Green);
        }
        public static string BuildInfoDir
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI.BuildInfoDir")))
                    return Environment.GetEnvironmentVariable("AI.BuildInfoDir");
                return "./";
            }
        }
        public static bool BuildInfoCheck()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI.BuildInfoDir")))
                return true;
            else
                return false;
        }
        public static bool DefaultPorts()
        {
            if (!File.Exists("Data/Ports.json"))
                return false;
            try
            {
                File.Copy("Data/Ports.json", "./Ports.json");
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool PortsCheck()
        {
            try
            {
                int port;
                port = SocketOptions.AngelIslandPort;
                port = SocketOptions.TestCenterPort;
                port = SocketOptions.SiegePerilousPort;
                port = SocketOptions.MortalisPort;
                port = SocketOptions.RenaissancePort;
                port = SocketOptions.EventShardPort;
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool EmailCheck()
        {
            string noreply_password = Environment.GetEnvironmentVariable("AI.NOREPLY.PASSWORD");                // = (your email password. For this I use an AppPassword generated by Google)
            string noreply_address = Environment.GetEnvironmentVariable("AI.NOREPLY.ADDRESS");                  // = example: noreply @game-master.net
            string email_host = Environment.GetEnvironmentVariable("AI.EMAIL.HOST");                            //  = example: smtp.gmail.com
            string email_user = Environment.GetEnvironmentVariable("AI.EMAIL.USER");                            //  = example: luke.tomasello@gmail.com
            string email_port = Environment.GetEnvironmentVariable("AI.EMAIL.HOST.PORT");                       //  = 587
            string email_accounting = Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING");                // = example: aiaccounting @game - master.net    // new account created etc.
            string email_announcements = Environment.GetEnvironmentVariable("AI.EMAIL.ANNOUNCEMENTS");          // = announcements@game-master.net				// announce shard wide events
            string email_devnotify = Environment.GetEnvironmentVariable("AI.EMAIL.DEVNOTIFY");                  // = devnotify@game-master.net					// sent on server crash
            string email_distlist_password = Environment.GetEnvironmentVariable("AI.EMAIL.DISTLIST.PASSWORD");  //  = (your email password.For this I use an AppPassword generated by Google) ;
            string email_shardowner = Environment.GetEnvironmentVariable("AI.EMAIL.SHARDOWNER");                // = shard owners email address, certain private information

            if (noreply_password == null || noreply_address == null || email_host == null || email_user == null || email_port == null ||
                email_accounting == null || email_announcements == null || email_devnotify == null || email_distlist_password == null || email_shardowner == null)
            {
                return false;
            }

            return true;
        }
        private static string m_arguments;
        public static string Arguments
        {
            get
            {
                return m_arguments;
            }

            set
            {
                m_arguments = value;
            }
        }

        private static int m_GlobalMaxUpdateRange = 24;

        public static int GlobalMaxUpdateRange
        {
            get { return m_GlobalMaxUpdateRange; }
            set { m_GlobalMaxUpdateRange = value; }
        }

        private static int m_ItemCount, m_MobileCount, m_SerializableObjectCount;

        public static int ScriptItems { get { return m_ItemCount; } }
        public static int ScriptMobiles { get { return m_MobileCount; } }
        public static int ScriptSerializableObjects { get { return m_SerializableObjectCount; } }

        public static void VerifySerialization()
        {
            m_ItemCount = 0;
            m_MobileCount = 0;

            VerifySerialization(Assembly.GetCallingAssembly());

            for (int a = 0; a < ScriptCompiler.Assemblies.Length; ++a)
                VerifySerialization(ScriptCompiler.Assemblies[a]);
        }

        private static void VerifySerialization(Assembly a)
        {
            if (a == null) return;

            Type[] ctorTypes = new Type[] { typeof(Serial) };

            foreach (Type t in a.GetTypes())
            {
                bool isItem = t.IsSubclassOf(typeof(Item));
                bool isSerializableObject = t.IsSubclassOf(typeof(SerializableObject));
                bool isMobile = t.IsSubclassOf(typeof(Mobile));
                if (isItem || isMobile || isSerializableObject)
                {
                    if (isItem)
                        ++m_ItemCount;
                    else if (isMobile)
                        ++m_MobileCount;
                    else
                        ++m_SerializableObjectCount;

                    bool warned = false;

                    try
                    {
                        if (isSerializableObject == false)
                            if (t.GetConstructor(ctorTypes) == null)
                            {
                                if (!warned)
                                    Console.WriteLine("Warning: {0}", t);

                                warned = true;
                                Console.WriteLine("       - No serialization constructor");
                            }

                        if (t.GetMethod("Serialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                        {
                            if (!warned)
                                Console.WriteLine("Warning: {0}", t);

                            warned = true;
                            Console.WriteLine("       - No Serialize() method");
                        }

                        if (t.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                        {
                            if (!warned)
                                Console.WriteLine("Warning: {0}", t);

                            warned = true;
                            Console.WriteLine("       - No Deserialize() method");
                        }

                        if (warned)
                            Console.WriteLine();
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        public class AIWorldBoot
        {
            Assembly m_scripts = null;
            ArrayList m_invoke = new ArrayList();
            Type[] m_types;
            public AIWorldBoot()
            {   // get the current assembly
                Type t = typeof(Core);
                m_scripts = t.Assembly;
                m_types = m_scripts.GetTypes();
            }

            public void Configure()
            {
                for (int i = 0; i < m_types.Length; ++i)
                {
                    MethodInfo m = m_types[i].GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);

                m_invoke.Clear();
            }

            public void WorldLoad()
            {
                World.Load();
            }

            // not yet implemented in core 3
            //public void ObjectInitialize()
            //{
            //    try
            //    {
            //        // 4/4/23, Adam: Add individual object initialization. Unlike Initialize, WorldLoaded provides initialization
            //        //  where the context of the object is known.
            //        Console.WriteLine("Initializing {0} items", World.Items.Count);
            //        foreach (Item item_dsr in World.Items.Values)
            //            if (item_dsr != null) item_dsr.WorldLoaded();
            //        Console.WriteLine("Initializing {0} Mobiles", World.Mobiles.Count);
            //        foreach (Mobile mobile_dsr in World.Mobiles.Values)
            //            if (mobile_dsr != null) mobile_dsr.WorldLoaded();
            //        Console.WriteLine("{0} objects initialized", World.Items.Count + World.Mobiles.Count);
            //    }
            //    catch {; }
            //}

            public void Initialize()
            {
                for (int i = 0; i < m_types.Length; ++i)
                {
                    MethodInfo m = m_types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);
            }
        }
    }

    public class FileLogger : TextWriter, IDisposable
    {
        private string m_FileName;
        private bool m_NewLine;
        public const string DateFormat = "[MMMM dd hh:mm:ss.f tt]: ";

        public string FileName { get { return m_FileName; } }

        public FileLogger(string file)
            : this(file, false)
        {
        }

        public FileLogger(string file, bool append)
        {
            m_FileName = file;
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine(">>>Logging started on {0}.", DateTime.UtcNow.ToString("f")); //f = Tuesday, April 10, 2001 3:51 PM 
            }
            m_NewLine = true;
        }

        public override void Write(char ch)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                {
                    writer.Write(DateTime.UtcNow.ToString(DateFormat));
                    m_NewLine = false;
                }
                writer.Write(ch);
            }
        }

        public override void Write(string str)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                {
                    writer.Write(DateTime.UtcNow.ToString(DateFormat));
                    m_NewLine = false;
                }
                writer.Write(str);
            }
        }

        public override void WriteLine(string line)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                    writer.Write(DateTime.UtcNow.ToString(DateFormat));
                writer.WriteLine(line);
                m_NewLine = true;
            }
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }

    public class MultiTextWriter : TextWriter
    {
        private ArrayList m_Streams;

        public MultiTextWriter(params TextWriter[] streams)
        {
            m_Streams = new ArrayList(streams);

            if (m_Streams.Count < 0)
                throw new ArgumentException("You must specify at least one stream.");
        }

        public void Add(TextWriter tw)
        {
            m_Streams.Add(tw);
        }

        public void Remove(TextWriter tw)
        {
            m_Streams.Remove(tw);
        }

        public override void Write(char ch)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).Write(ch);
        }

        public override void WriteLine(string line)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).WriteLine(line);
        }

        public override void WriteLine(string line, params object[] args)
        {
            WriteLine(String.Format(line, args));
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }
}
