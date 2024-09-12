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

/* Server\Patcher.cs
 * CHANGELOG:
 *	    First time checkin
 *	    Runs patches on shard launch
 */

using Server.Commands;
using Server.Items;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public static class Patcher
    {
        static int m_PatchID = 0;

        // executes after world load but BEFORE EventSink.ServerStarted
        public static void Initialize()
        {
            if (Core.Patching == false)
            {
                Utility.Monitor.WriteLine("Patching disabled with the -nopatch startup switch", ConsoleColor.Red);
                return;
            }

            // executes after world load AFTER EventSink.Initialize
            EventSink.ServerStarted += new ServerStartedEventHandler(EventSink_ServerStarted);

            /*  adam: This is the startup patcher.
             *  You'll usually put onetime patching logic here.
             */
            string pathName = Path.Combine(Core.DataDirectory, "Patches", "ConsoleHistory.log");
            Utility.EnsurePath(pathName);
            Utility.Monitor.ConsoleOutEcho = pathName;
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            try
            {
                int patches = 0;

                patches += PatchV1(m_PatchID++);

                //patches += PatchV2(m_PatchID++);

                if (patches == 0)
                    EchoOut(string.Format("No Initialize patching required."), ConsoleColor.Magenta);
            }
            #region Finally
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Utility.Monitor.ConsoleOutEcho = null;
            }

            tc.End();
            System.Console.WriteLine("Patcher1 completed in {0}", tc.TimeTaken);
            #endregion Finally
        }

        #region PatchV1
        private static int PatchV1(int patchid)
        {
            int patches = 0;
            PatchIndex bits = PatchIndex.PatchV1;
            if (!Patched(bits) && AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                #region Keys on the Ocean
                if (AngelIsland())
                {
                    foreach (Item item in World.Items.Values)
                        if (item is KeyRing kr)
                            if (kr.Keys.Count > 0)
                                foreach (Key key in kr.Keys)
                                {
                                    key.MoveItemToIntStorage();
                                    patched++;
                                }
                }
                #endregion Keys on the Ocean

                #endregion End Implementation
                EchoOut(string.Format("{1} patched with {0} objects updated", patched, GetPatchName(bits)), ConsoleColor.Magenta);
                PatchComplete(bits, patchid);
                patches = patched;
            }

            return patches;
        }
        #endregion PatchV1

        #region PatchV2
        private static int PatchV2(int patchid)
        {
            int patches = 0;
            PatchIndex bits = PatchIndex.PatchV2;
            if (!Patched(bits) && AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;

                #region Begin Implementation

                if (AngelIsland())
                {
                    Console.WriteLine("Hello World II");
                }

                #endregion End Implementation
                EchoOut(string.Format("{1} patched with {0} objects updated", patched, GetPatchName(bits)), ConsoleColor.Magenta);
                PatchComplete(bits, patchid);
                patches = patched;
            }

            return patches;
        }
        #endregion PatchV2

        private static int FeluccaMap00Check(int patchid)
        {
            int patched = 0;
            //PatchIndex bits = PatchIndex.None;
            //if (AllShards())
            //{//TODO:
            //    EchoOut("Run Always: Checking for mobiles & items at map 0,0...", ConsoleColor.Yellow);
            //    //adam: issue a warning if we find junk at map location 0 0
            //    #region Relocating / Deleting objects at Felucca 0,0
            //    IPooledEnumerable eable = Map.Felucca.GetObjectsInRange(new Point3D(0, 0, 0), 1);
            //    List<object> toMove = new List<object>();
            //    foreach (object obj in eable)
            //    {
            //        Point3D spawnerLoc = Point3D.Zero;
            //        if (obj is Mobile m) { spawnerLoc = m.SpawnerLocation; }
            //        if (obj is Item i) { spawnerLoc = i.Spawner != null ? i.Spawner.Location : Point3D.Zero; }
            //        EchoOut(string.Format("Warning: {0} found at {1} with a spawner located at {2}", obj, new Point3D(0, 0, 0), spawnerLoc == Point3D.Zero ? "-none-" : spawnerLoc), ConsoleColor.Yellow);
            //        if (obj is Mobile mWrongMap && mWrongMap.Map == Map.Felucca && mWrongMap.IsIntMapStorage)
            //        {
            //            EchoOut(string.Format("Note: IsIntMapStorage (template) mobile {0} found on wrong map. Relocating...", obj), ConsoleColor.Yellow);
            //            toMove.Add(obj);
            //            patched++;
            //        }
            //        else if (obj is Item iWrongMap && iWrongMap.Map == Map.Felucca && iWrongMap.IsIntMapStorage)
            //        {
            //            EchoOut(string.Format("Note: IsIntMapStorage (template) item {0} found on wrong map. Relocating...", obj), ConsoleColor.Yellow);
            //            toMove.Add(obj);
            //            patched++;
            //        }
            //        else
            //        {
            //            EchoOut(string.Format("Deleting and Logging: {0}", obj), ConsoleColor.Yellow);
            //            string output = string.Format("Deleting FeluccaMap object {0} from map 0,0 with a spawner located at {1}", obj, spawnerLoc == Point3D.Zero ? "-none-" : spawnerLoc);
            //            LogHelper logger = new LogHelper("FeluccaMap00Delete.log", false);
            //            logger.Log(LogType.Text, output);
            //            logger.Log(LogType.Mixed, obj);
            //            logger.Finish();
            //            patched++;
            //        }
            //    }
            //    eable.Free();
            //    foreach (object obj in toMove)
            //    {
            //        if (obj is Mobile mWrongMap && mWrongMap.Map == Map.Felucca && mWrongMap.IsIntMapStorage)
            //        {
            //            mWrongMap.Map = Map.Internal;
            //            patched++;
            //        }
            //        else if (obj is Item iWrongMap && iWrongMap.Map == Map.Felucca && iWrongMap.IsIntMapStorage)
            //        {
            //            iWrongMap.Map = Map.Internal;
            //            patched++;
            //        }
            //    }
            //    #endregion Relocating / Deleting objects at Felucca 0,0
            //    EchoOut(string.Format("{0} objects handled.", patched), ConsoleColor.Yellow);
            //    PatchComplete(bits, patchid);
            //}
            return patched;
        }
        public static void EventSink_ServerStarted()
        {
            Utility.Monitor.ConsoleOutEcho = Path.Combine(Core.LogsDirectory, "ConsoleHistory.log");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            try
            {
                int patches = 0;
                if (patches == 0)
                    EchoOut(string.Format("No special 'ServerStarted' patching required."), ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Utility.Monitor.ConsoleOutEcho = null;
            }

            tc.End();
            System.Console.WriteLine("Patcher2 completed in {0}", tc.TimeTaken);

        }

        #region Utility functions
        private static bool Patched(PatchIndex bits)
        {
            // Have we been patched?
            int need = (int)bits + 1;
            return PatchDatabase.Count >= need && PatchDatabase[(int)bits];
        }
        private static bool TestCenter(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.TestCenterRules()); else return Core.RuleSets.TestCenterRules(); }
        private static bool EventShard(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.EventShardRules()); else return Core.RuleSets.EventShardRules(); }
        private static bool Siege(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.SiegeRules()); else return Core.RuleSets.SiegeRules(); }
        private static bool AngelIsland(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.AngelIslandRules()); else return Core.RuleSets.AngelIslandRules(); }
        private static bool Mortalis(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.MortalisRules()); else return Core.RuleSets.MortalisRules(); }
        private static bool Renaissance(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.RenaissanceRules()); else return Core.RuleSets.RenaissanceRules(); }
        private static bool LoginServer(bool quiet = false) { if (!quiet) return Patching(Core.RuleSets.LoginServerRules()); else return Core.RuleSets.LoginServerRules(); }
        private static bool TestCenterShards(bool quiet = false)
        {
            if (Core.RuleSets.TestCenterRules())
                return TestCenter(quiet);
            else
                return false;
        }
        private static bool EventShards(bool quiet = false)
        {
            if (Core.RuleSets.EventShardRules())
                return EventShard(quiet);
            else
                return false;
        }
        private static bool SiegeStyleShards(bool quiet = false)
        {
            if (Core.RuleSets.SiegeRules())
                return Siege(quiet);
            else if (Core.RuleSets.MortalisRules())
                return Mortalis(quiet);
            else
                return false;
        }
        private static bool StandardShards(bool quiet = false)
        {
            if (Core.RuleSets.SiegeRules())
                return Siege(quiet);
            else if (Core.RuleSets.MortalisRules())
                return Mortalis(quiet);
            if (Core.RuleSets.RenaissanceRules())
                return Renaissance(quiet);
            else
                return false;
        }
        private static bool AllShards() { EchoOut("Patching all shards.", ConsoleColor.Green); return true; }
        private static void LogPatch(PatchIndex bits)
        {
            // record the patch
            using (StreamWriter sw = File.AppendText(m_pathName))
            {
                sw.WriteLine(string.Format("{0}: patching...",
                    Enum.GetName(typeof(PatchIndex), bits)));
            }
        }
        private static void PatchComplete(PatchIndex bits, int patchid, bool timeUnknown = false)
        {
            // PatchIndex.None is a special flag that runs each and every server restart. Usually 
            //  to cleanup map (0,0) and other common errors
            
            // Ensure size
            while (PatchDatabase.Count < (int)PatchIndex.__last)
                PatchDatabase.Add(false);

            PatchDatabase[(int)bits] = true;    // patched

            string l1 = string.Format(string.Format("Patch {0} complete. {1}", patchid,
                timeUnknown ? DateTime.MinValue : AdjustedDateTime.GameTime));
            string l2 = string.Format("===========================");
            EchoOut(l1, ConsoleColor.Green, echo: false);
            EchoOut(l2, ConsoleColor.Green, echo: false);
            using (StreamWriter sw = File.AppendText(m_pathName))
            {
                sw.WriteLine(string.Format("{0}: Complete on {1}",
                    GetPatchName(bits),
                    timeUnknown ? DateTime.MinValue : AdjustedDateTime.GameTime));
                sw.WriteLine(l2);
            }
        }
        private static string GetPatchName(PatchIndex bits)
        {
            return Enum.GetName(typeof(PatchIndex), bits);
        }
        private static bool Patching(bool shards)
        {
            // format for the screen
            {
                bool wasmuted = m_muted;
                if (wasmuted) Unmute();
                if (shards)
                {
                    if (Core.RuleSets.StandardShardRules())
                        EchoOut("Patching Siege, Mortalis, and Renaissance.", ConsoleColor.Green);
                    else if (Core.RuleSets.AngelIslandRules())
                        EchoOut("Patching Angel Island.", ConsoleColor.Green);
                    else
                        ;
                }
                if (wasmuted) Mute();
            }
            return shards;
        }
        private static void EchoOut(string text, ConsoleColor color, bool echo = true)
        {
            Utility.Monitor.WriteLine(text, color);
            if (echo)
                using (StreamWriter sw = File.AppendText(m_pathName))
                {
                    sw.WriteLine(string.Format("{0}", text));
                }
        }
        private static void ErrorOut(PatchIndex bits, string text, ConsoleColor color, object o = null)
        {
            Utility.Monitor.ErrorOut(text, color, o);
            using (StreamWriter sw = File.AppendText(m_pathName))
            {
                sw.WriteLine(string.Format("{0}: {1} {2}", Enum.GetName(typeof(PatchIndex), bits),
                    text, o));
            }
        }
        private static bool m_muted = false;
        private static void Mute()
        {
            m_muted = true;
            Console.SetOut(StreamWriter.Null);
        }
        private static void Unmute()
        {
            m_muted = false;
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }
        private static string m_pathName = Path.Combine(Core.LogsDirectory, "PatchHistory.log");
        #endregion Utility functions

        #region Patch Database
        public enum PatchIndex : int
        {
            PatchV1,
            PatchV2,
            __last  // defines the size of the table
        }
        private static List<bool> PatchDatabase = new List<bool>((int)PatchIndex.__last);
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/Patches.bin"))
                return;

            Console.WriteLine("Patches Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/Patches.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                PatchDatabase.Add(reader.ReadBool());
                            }
                            break;
                        }
                    default:
                        {
                            reader.Close();
                            throw new Exception("Invalid Patches.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/Patches.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Patches Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/Patches.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(PatchDatabase.Count);
                            foreach (bool state in PatchDatabase)
                                writer.Write(state);

                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/Patches.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
        #endregion Patch Database
    }
}
