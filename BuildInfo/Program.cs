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

/* BuildInfo\Program.cs
 * ChangeLog
 *  8/29/2024, Adam
 *      1. first time check in
 *      Bump the major version to 3. This will be our new GitHub version
 *      
 *  Version History:
 *      10/29/22, Adam
 *      Minor 2:
 *          1. add BuildInfo.
 *          2. Colorize certain console I/O
 *          3. Bug fixes: tent backpacks, and decaying of township walls.
 *          4. Condition port numbers on whether or not we are running a GMN (game-master.net) server
 *          Note: This is source versioning, not framework versioning. We have both Frameword 4.7.2, and .NET 8
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace BuildInfo
{
    class Program
    {
        static int Main(string[] args)
        {
            // major version number
            const int major = 3;    // if you change this, update Version History above
            // minor version number
            const int minor = 2;    // if you change this, update Version History above

            Console.WriteLine("BuildInfo: Generating Build Information...");
            string buildInfoFile = Path.Combine(BuildInfoDir, "Core 3.info");
            bool okay = false;
            int buildNo = GetBuild();
            try
            {
                // this needs to go into the root folder of the project
                StreamWriter sw = new StreamWriter(buildInfoFile);
                //human readable build number
                sw.WriteLine((buildNo + 1).ToString());
                //human readable revision number
                sw.WriteLine(BuildRevision().ToString());
                //human readable major version number
                sw.WriteLine(major.ToString());
                //human readable minor version number
                sw.WriteLine(minor.ToString());
                sw.Close();
                okay = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }

            if (okay)
                Console.WriteLine("BuildInfo: Done.");
            else
                Console.WriteLine("BuildInfo: failed.");

            return 0;
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
        public static int GetBuild()
        {
            string buildInfoFile = Path.Combine(BuildInfoDir, "Core 3.info");
            try
            {
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build number
                string line = sr.ReadLine();
                sr.Close();
                int result = 0;
                int.TryParse(line, out result);
                return result;

            }
            catch
            {
                Console.WriteLine("Creating a new build_core3.info file");
            }
            return 0;
        }

        public static int BuildRevision()
        {   // revisions are the time representation noted below
            var now = DateTime.UtcNow;
            return now.Year + now.Month + now.Day + now.Hour + now.Minute + now.Second;
        }
    }
}
