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

/* Script/Misc/AutoSave.cs
 * CHANGELOG:
 *  9/10/2024, Adam (Backup)
 *      New Backup strategy. Now matches what we do in Core 6.
 *      Basically, all saves are saved to a time-stamped folder. 
 *      We will use an external daemon for cleaning up old versions.
 *	5/15/10, Adam
 *		Automatically create a 1 backup each day of the form: Archive-17May10
 *	8/22/07, Adam
 *		We split the backup and save into separate catch blocks because we still want to save even if we cannot backup
 *	9/1/06, Adam
 *		Update the previous fix so that Saves are only skipped when they are the hourly 'passive' saves.
 *		Maintenance restarts, and other proactive Saves will now go through as normal.
 *	8/13/06, Adam
 *		Add a check to Save() to see if "ntbackup.exe" is running. If so, do not do a backup at this time.
 *		We added this because we will see a 24 minute backup when the Shadow Copy process is running
 *			when we would normally see a 12 second save.
 *		We also print a console message to note the fact.
 * 	12/01/05 Pix
 *		Added WorldSaveFrequency to CoreAI -- and logic to change dynamically.
 *	9/12/05, Adam
 *		Remove time-stamp from directory name so we can backup the "Most Recent"
 *			without trouble.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/2/04 smerX
 *		Changed m_Delay to 30 minutes
*/

using Server.Diagnostics;
using Server.Commands;
using System;
using System.IO;

namespace Server.Misc
{
    public class AutoSave : Timer
    {
        private static TimeSpan m_Delay = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency);
        private static TimeSpan m_Warning = TimeSpan.Zero;
        //private static TimeSpan m_Warning = TimeSpan.FromSeconds( 15.0 );

        public static void Initialize()
        {
            new AutoSave().Start();
            CommandSystem.Register("SetSaves", AccessLevel.Administrator, new CommandEventHandler(SetSaves_OnCommand));

            //System.Console.WriteLine("WORLD SAVE FREQUENCY IS {0}", CoreAI.WorldSaveFrequency );
        }

        private static bool m_SavesEnabled = true;

        public static bool SavesEnabled
        {
            get { return m_SavesEnabled; }
            set { m_SavesEnabled = value; }
        }

        [Usage("SetSaves <true | false>")]
        [Description("Enables or disables automatic shard saving.")]
        public static void SetSaves_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                m_SavesEnabled = e.GetBoolean(0);
                e.Mobile.SendMessage("Saves have been {0}.", m_SavesEnabled ? "enabled" : "disabled");
            }
            else
            {
                e.Mobile.SendMessage("Format: SetSaves <true | false>");
            }
        }

        public AutoSave()
            : base(m_Delay - m_Warning, m_Delay)
        {
            Priority = TimerPriority.OneMinute;
        }

        private static int m_iLastSaveFrequency = CoreAI.WorldSaveFrequency;
        protected override void OnTick()
        {
            if (m_iLastSaveFrequency != CoreAI.WorldSaveFrequency)
            {
                this.Stop();
                m_Delay = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency);
                System.Console.WriteLine("Changing worldsave frequency from {0} to {1}", m_iLastSaveFrequency, CoreAI.WorldSaveFrequency);
                new AutoSave().Start();
            }
            m_iLastSaveFrequency = CoreAI.WorldSaveFrequency;

            // don't do a save if he server is backing up
            bool ServerBusy = false;
            try
            {
                ServerBusy = System.Diagnostics.Process.GetProcessesByName("ntbackup").Length > 0;
                if (ServerBusy)
                    Console.WriteLine("World save skipped, server backup in progress.");
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine(e.ToString());
            }

            // final check to see if we should save
            if (!m_SavesEnabled || AutoRestart.Restarting || ServerBusy)
                return;

            if (m_Warning == TimeSpan.Zero)
            {
                Save();
            }
            else
            {
                int s = (int)m_Warning.TotalSeconds;
                int m = s / 60;
                s %= 60;

                if (m > 0 && s > 0)
                    World.Broadcast(0x35, true, "The world will save in {0} minute{1} and {2} second{3}.", m, m != 1 ? "s" : "", s, s != 1 ? "s" : "");
                else if (m > 0)
                    World.Broadcast(0x35, true, "The world will save in {0} minute{1}.", m, m != 1 ? "s" : "");
                else
                    World.Broadcast(0x35, true, "The world will save in {0} second{1}.", s, s != 1 ? "s" : "");

                Timer.DelayCall(m_Warning, new TimerCallback(Save));
            }
        }

        public static void Save()
        {
            if (AutoRestart.Restarting)
                return;

            // we split the backup and save into separate catch blocks because we still want to save even if we cannot backup
            try
            {
                Backup();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

            try
            {
                World.Save();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

        }
#if true
        public static void Backup()
        {
            string filename = Utility.GameTimeFileStamp();  // 'Game Time' Format: "August 31 2023, 06 11 34 PM"
            //if (Server.Misc.AutoSave.FinalSave)             // final save before we go down
            //    filename += "(final)";                      // add a special tag to indicate this is the last save before the patch

            Backup(filename);
        }
        private static void Backup(string filename)
        {
            Console.WriteLine("Backing up...");

            string root = Path.Combine(Core.BaseDirectory, "Backups\\Automatic");
            string saves = Path.Combine(Core.BaseDirectory, "Saves");

            //m_fileManagerDatabase.Enqueue(filename);        // save/load these for periodic cleanup
            Console.WriteLine("File Manager: Enqueueing {0}", filename);
            if (Directory.Exists(saves))
            {
                try { Directory.Move(saves, FormatDirectory(root, filename, "")); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("We'll copy instead.");
                    try { DirectoryCopy(saves, FormatDirectory(root, filename, ""), true); }
                    catch { Console.WriteLine("Cannot copy Saves directory. Giving up."); }
                }
            }
            else
                Console.WriteLine("{0} does not exist!", saves);

            // 1/18/22, Adam: disabling this.. Our Google backup (server-side) allows the files to get deleted, but not the directories.
            //  kinda ugly, need to find a better way. For now I'll just delete old backups manually
#if false
            if (false)
                while (m_fileManagerDatabase.Count > CoreAI.BackupCount)
                {
                    string[] existing = Directory.GetDirectories(root);
                    DirectoryInfo dir;
                    string toDelete = m_fileManagerDatabase.Dequeue();
                    dir = Match(existing, toDelete);
                    Console.WriteLine("File Manager: Dequeuing {0}", toDelete);
                    if (dir != null && Directory.Exists(dir.FullName))
                    {
                        Console.WriteLine("File Manager: Deleting {0}", dir.Name);
                        // asynchronous file delete
                        //Task.Factory.StartNew(path => Directory.Delete((string)path, true), dir.FullName);
                        try { dir.Delete(true); }
                        catch { Console.WriteLine("Cannot delete backup directory {0}. Giving up.", dir.Name); }
                    }
                }
#endif
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private static DirectoryInfo Match(string[] paths, string match)
        {
            for (int i = 0; i < paths.Length; ++i)
            {
                DirectoryInfo info = new DirectoryInfo(paths[i]);

                if (info.Name.StartsWith(match))
                    return info;
            }

            return null;
        }

        private static string FormatDirectory(string root, string name, string timeStamp)
        {
            return Path.Combine(root, string.Format("{0}", name));
        }

#else
        private static string[] m_Backups = new string[]
            {
                "Third Backup",
                "Second Backup",
                "Most Recent"
            };

        public enum BakNames
        {
            THIRD,
            SECOND,
            MOST
        }

        // adam: we want to save one folder per day as our daily backup
        private static DateTime LastDailyArchive = DateTime.MinValue;

        private static void Backup()
        {
            if (m_Backups.Length == 0)
                return;

            string root = Path.Combine(Core.BaseDirectory, "Backups\\Automatic");

            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            string[] existing = Directory.GetDirectories(root);

            DirectoryInfo dir;

            // either nuke or archive the 3rd Backup			
            dir = Match(existing, m_Backups[(int)BakNames.THIRD]);
            if (dir != null && DateTime.UtcNow.Day != LastDailyArchive.Day)
            {   // last archive
                LastDailyArchive = DateTime.UtcNow;

                // Archive-Thu Aug 17 00
                // Archive-17Aug00
                string name = string.Format("Archive-{0}", DateTime.UtcNow.ToString("dMMMyy"));

                // rename 3rd to archive
                if (Directory.Exists(FormatDirectory(root, name, "")) == false)
                {
                    try { dir.MoveTo(FormatDirectory(root, name, "")); }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to move to {0}", FormatDirectory(root, name, "")); Console.WriteLine(e.ToString());
                        try { dir.Delete(true); }
                        catch (Exception ex) { Console.WriteLine("Failed to delete {0}", dir.FullName); Console.WriteLine(ex.ToString()); }
                    }
                }
                else if (dir != null)
                {
                    // delete the 3rd most recent
                    try { dir.Delete(true); }
                    catch (Exception e) { Console.WriteLine("Failed to delete {0}", dir.FullName); Console.WriteLine(e.ToString()); }
                }
            }
            else if (dir != null)
            {
                // delete the 3rd most recent
                try { dir.Delete(true); }
                catch (Exception e) { Console.WriteLine("Failed to delete {0}", dir.FullName); Console.WriteLine(e.ToString()); }
            }

            // rename 2nd to 3rd
            dir = Match(existing, m_Backups[(int)BakNames.SECOND]);
            if (dir != null)
            {
                try { dir.MoveTo(FormatDirectory(root, m_Backups[(int)BakNames.THIRD], "")); }
                catch (Exception e) { Console.WriteLine("Failed to move to {0}", FormatDirectory(root, m_Backups[(int)BakNames.THIRD], "")); Console.WriteLine(e.ToString()); }
            }

            // rename most recent to 2nd
            dir = Match(existing, m_Backups[(int)BakNames.MOST]);
            if (dir != null)
            {
                try { dir.MoveTo(FormatDirectory(root, m_Backups[(int)BakNames.SECOND], "")); }
                catch (Exception e) { Console.WriteLine("Failed to move to {0}", FormatDirectory(root, m_Backups[(int)BakNames.SECOND], "")); Console.WriteLine(e.ToString()); }
            }

            // move saves to most recent
            string saves = Path.Combine(Core.BaseDirectory, "Saves");
            if (Directory.Exists(saves))
                Directory.Move(saves, FormatDirectory(root, m_Backups[(int)BakNames.MOST], ""));
            else
                Console.WriteLine("{0} does not exist!", saves);
        }

        private static DirectoryInfo Match(string[] paths, string match)
        {
            for (int i = 0; i < paths.Length; ++i)
            {
                DirectoryInfo info = new DirectoryInfo(paths[i]);

                if (info.Name.StartsWith(match))
                    return info;
            }

            return null;
        }

        private static string FormatDirectory(string root, string name, string timeStamp)
        {
            return Path.Combine(root, String.Format("{0}", name));
        }

        private static string FindTimeStamp(string input)
        {
            int start = input.IndexOf('(');

            if (start >= 0)
            {
                int end = input.IndexOf(')', ++start);

                if (end >= start)
                    return input.Substring(start, end - start);
            }

            return null;
        }

        private static string GetTimeStamp()
        {
            DateTime now = DateTime.UtcNow;

            return String.Format("{0}-{1}-{2} {3}-{4:D2}-{5:D2}",
                    now.Day,
                    now.Month,
                    now.Year,
                    now.Hour,
                    now.Minute,
                    now.Second
                );
        }
#endif
    }
}
