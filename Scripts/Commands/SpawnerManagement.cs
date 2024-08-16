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

/* Scripts\Engines\Spawner\SpawnerCapture.cs
 * CHANGELOG:
 *	3/3/11, adam
 *		Add the ability to name a spawned object which acts as the filter.
 *		For instance, "[savespawners " will save all spawners in the world, but "[savespawners cottonplant" will save all spawners that 
 *		spawn cotton plants. 
 *		Note: we now save these XML files in the data directory in order to recover certain spawn configurations.
 *	2/26/11, Adam
 *		Initial Version
 *		I commented out Kit's versions of the spawner management routines since he failed to take into account that the serial numbers 
 *		would collide on restore. 
 *		An XML version is also far more flexible since it also allows human hand editing
 */

using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Commands
{
    public class SpawnerCapture
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("SaveSpawners", AccessLevel.Administrator, new CommandEventHandler(SaveSpawners_OnCommand));
            Server.CommandSystem.Register("RestoreSpawners", AccessLevel.Administrator, new CommandEventHandler(RestoreSpawners_OnCommand));
            Server.CommandSystem.Register("FixupSpawners", AccessLevel.Administrator, new CommandEventHandler(FixupSpawners_OnCommand));
        }

        [Usage("SaveSpawners [object]")]
        [Description("Saves all world spawners, or spawners containing 'object'.")]
        public static void SaveSpawners_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Begin spawner save.");
                int count = SaveSpawnersWorker(e);
                e.Mobile.SendMessage("Spawner save complete with {0} items processed.", count);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static int SaveSpawnersWorker(CommandEventArgs e)
        {
            List<Spawner> list = new List<Spawner>();
            foreach (Item ix in World.Items.Values)
            {
                Spawner sx = ix as Spawner;

                if (sx == null || sx.Deleted)
                    continue;

                // now filter for type, that is spawners that contain this object.
                //	for instance "cottonplant"
                if (e.Arguments.Length > 0)
                    //if (sx.ObjectNames != null && sx.ObjectNames.Contains(e.Arguments[0]) == false)
                    if (sx.ObjectTypes.Contains(SpawnerType.GetType(e.Arguments[0])) == false)
                        continue;

                list.Add(sx);
            }

            int write_count = 0;

            if (write_count++ % 100 == 0)
                Console.WriteLine("Saving spawner {0} of {1}", write_count, list.Count);

            if (!Directory.Exists("Saves/Capture"))
                Directory.CreateDirectory("Saves/Capture");

            string filePath = Path.Combine("Saves/Capture", "spawners.xml");

            using (StreamWriter op = new StreamWriter(filePath))
            {
                XmlTextWriter xml = new XmlTextWriter(op);

                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);
                xml.WriteStartElement("SPAWNERS");

                // just to be complete..
                xml.WriteAttributeString("Count", list.Count.ToString());

                // Now write each entry
                foreach (Spawner spawner in list)
                    Save(spawner, xml);

                //and end doc
                xml.WriteEndElement();
                xml.Close();
            }

            return list.Count;
        }

        public static void Save(Spawner spawner, XmlTextWriter xml)
        {
            try
            {
                xml.WriteStartElement("Spawner_Properties");

                ////
                // basic item properties 

                // location
                xml.WriteStartElement("Location");
                xml.WriteString(spawner.Location.ToString());
                xml.WriteEndElement();

                // map
                xml.WriteStartElement("Map");
                xml.WriteString(spawner.Map.Name);
                xml.WriteEndElement();

                ////
                // spawner properties

                // spawner version
                xml.WriteStartElement("Version");
                xml.WriteString(((int)11).ToString());
                xml.WriteEndElement();

                // NavDest
                xml.WriteStartElement("NavDest");
                xml.WriteString(spawner.NavPoint.ToString());
                xml.WriteEndElement();

                // MobDirection
                xml.WriteStartElement("MobDirection");
                xml.WriteString(((int)spawner.MobileDirection).ToString());
                xml.WriteEndElement();

                // WayPoint - unsupported
                xml.WriteStartElement("WayPoint");
                xml.WriteString("0");
                xml.WriteEndElement();

                // Group
                xml.WriteStartElement("Group");
                xml.WriteString(spawner.Group ? "1" : "0");
                xml.WriteEndElement();

                // MinDelay
                xml.WriteStartElement("MinDelay");
                xml.WriteString(spawner.MinDelay.TotalSeconds.ToString());
                xml.WriteEndElement();

                // MaxDelay
                xml.WriteStartElement("MaxDelay");
                xml.WriteString(spawner.MaxDelay.TotalSeconds.ToString());
                xml.WriteEndElement();

                // Count
                xml.WriteStartElement("Count");
                xml.WriteString(spawner.Count.ToString());
                xml.WriteEndElement();

                // Team
                xml.WriteStartElement("Team");
                xml.WriteString(spawner.Team.ToString());
                xml.WriteEndElement();

                // HomeRange
                xml.WriteStartElement("HomeRange");
                xml.WriteString(spawner.HomeRange.ToString());
                xml.WriteEndElement();


                // how many creatures?
                xml.WriteStartElement("CreatureCount");
                xml.WriteString(spawner.ObjectNames.Count.ToString());
                xml.WriteEndElement();

                // the creatures
                for (int i = 0; i < spawner.ObjectNames.Count; i++)
                {
                    xml.WriteStartElement("Line" + i);
                    xml.WriteString(spawner.ObjectNames[i] as string);
                    xml.WriteEndElement();
                }

                // Running
                xml.WriteStartElement("Running");
                xml.WriteString(spawner.Running ? "1" : "0");
                xml.WriteEndElement();

                // NextSpawn
                if (spawner.Running)
                {
                    xml.WriteStartElement("NextSpawn");
                    xml.WriteString(spawner.NextSpawn.TotalSeconds.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }
            catch
            {
                Console.WriteLine("Error saving a spawner entry!");
            }
        }

        [Usage("RestoreSpawners")]
        [Description("Restores all world spawners.")]
        public static void RestoreSpawners_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Begin restore save.");
                int count = RestoreSpawnersWorker(e);
                e.Mobile.SendMessage("Spawner restore complete with {0} items processed.", count);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static int RestoreSpawnersWorker(CommandEventArgs e)
        {
            string filePath = Path.Combine("Saves/Capture", "spawners.xml");

            if (!File.Exists(filePath))
                return 0;

            int count = 0;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);
                XmlElement root = doc["SPAWNERS"];
                int read_count = 0;
                count = XmlUtility.GetInt32(root.GetAttribute("Count"), 0);

                foreach (XmlElement entry in root.GetElementsByTagName("Spawner_Properties"))
                {
                    try
                    {
                        if (read_count++ % 100 == 0)
                            Console.WriteLine("Restoring spawner {0} of {1}", read_count, count);

                        // load in entry!
                        Spawner spawner = Load(entry);
                    }
                    catch
                    {
                        Console.WriteLine("Warning: A Spawner entry load failed");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Exception caught loading spawners.xml");
                Console.WriteLine(ex.StackTrace);

            }

            return count;
        }

        private static Spawner Load(XmlNode xml)
        {
            Spawner spawner = new Spawner();
            try
            {
                // "Location"
                string location = XmlUtility.GetText(xml["Location"], "(0, 0, 0)");
                spawner.Location = Point3D.Parse(location);

                // "Map"
                string map = XmlUtility.GetText(xml["Map"], "xxx");
                spawner.Map = Map.Parse(map);

                // "Version"
                string version = XmlUtility.GetText(xml["Version"], "0");

                // "NavDest"
                string navDest = XmlUtility.GetText(xml["NavDest"], "0");
                spawner.NavPoint = (Server.Engines.NavDestinations)XmlUtility.GetInt32(navDest, 0);

                // "MobDirection"
                string MobDirection = XmlUtility.GetText(xml["MobDirection"], "0");
                spawner.MobileDirection = (Direction)XmlUtility.GetInt32(MobDirection, 0);

                // "WayPoint" (unsupported)
                string WayPoint = XmlUtility.GetText(xml["WayPoint"], "0");
                //spawner.WayPoint = 

                // "Group"
                string Group = XmlUtility.GetText(xml["Group"], "0");
                spawner.Group = XmlUtility.GetInt32(Group, 0) == 1 ? true : false;

                // "MinDelay"
                string MinDelay = XmlUtility.GetText(xml["MinDelay"], "0");
                spawner.MinDelay = TimeSpan.FromSeconds((double)XmlUtility.GetInt32(MinDelay, 0));

                // "MaxDelay"
                string MaxDelay = XmlUtility.GetText(xml["MaxDelay"], "0");
                spawner.MaxDelay = TimeSpan.FromSeconds((double)XmlUtility.GetInt32(MaxDelay, 0));

                // might as well fix this since I'm here
                if (spawner.MinDelay > spawner.MaxDelay)
                {
                    Console.WriteLine("Swapping poorly formed spawner min/max for {0}", spawner.Serial.ToString());
                    TimeSpan temp = spawner.MinDelay;
                    spawner.MinDelay = spawner.MaxDelay;
                    spawner.MaxDelay = temp;
                }

                // "Count"
                string Count = XmlUtility.GetText(xml["Count"], "0");
                spawner.Count = XmlUtility.GetInt32(Count, 0);

                // "Team"
                string Team = XmlUtility.GetText(xml["Team"], "0");
                spawner.Team = XmlUtility.GetInt32(Team, 0);

                // "HomeRange"
                string HomeRange = XmlUtility.GetText(xml["HomeRange"], "0");
                spawner.HomeRange = XmlUtility.GetInt32(HomeRange, 0);

                // "CreatureCount"
                int CreatureCount = XmlUtility.GetInt32(XmlUtility.GetText(xml["CreatureCount"], "0"), 0);
                spawner.ObjectNames = new ArrayList();

                // the creatures
                for (int i = 0; i < CreatureCount; i++)
                    spawner.ObjectNames.Add(XmlUtility.GetText(xml["Line" + i], "?"));

                // "Running"
                string Running = XmlUtility.GetText(xml["Running"], "0");
                spawner.Running = XmlUtility.GetInt32(Running, 0) == 1 ? true : false;

                // "NextSpawn"
                if (spawner.Running)
                {
                    string NextSpawn = XmlUtility.GetText(xml["NextSpawn"], "0");
                    spawner.NextSpawn = TimeSpan.FromSeconds((double)XmlUtility.GetInt32(NextSpawn, 0));
                }

                return spawner;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception caught loading spawners.xml");
                Console.WriteLine(e.StackTrace);

            }

            return null;
        }

        [Usage("FixupSpawners")]
        [Description("Normalizes Min/Max values for all world spawners.")]
        public static void FixupSpawners_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Begin spawner repair.");
                int count = FixupSpawnersWorker(e);
                e.Mobile.SendMessage("Spawner repair complete with {0} items repaired.", count);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static int FixupSpawnersWorker(CommandEventArgs e)
        {
            int count = 0;

            List<Spawner> list = new List<Spawner>();
            foreach (Item ix in World.Items.Values)
            {
                Spawner sx = ix as Spawner;

                if (sx == null || sx.Deleted)
                    continue;

                list.Add(sx);
            }

            int processed_count = 0;
            foreach (Spawner sx in list)
            {
                if (sx == null || sx.Deleted)
                    continue;

                if (processed_count++ % 100 == 0)
                    Console.WriteLine("Processing spawner {0} of {1}", processed_count, list.Count);

                if (sx.MinDelay > sx.MaxDelay)
                {
                    count++;
                    Console.WriteLine("Swapping poorly formed spawner min/max for {0}", sx.Serial.ToString());
                    TimeSpan temp = sx.MinDelay;
                    sx.MinDelay = sx.MaxDelay;
                    sx.MaxDelay = temp;
                }
            }

            return count;
        }
    }
}

#if not_used
/* Scripts/Commands/SpawnerManagement.cs
 * CHANGELOG:
 *  6/29/06, Kit
 *		updated save function to work with spawner templates, do not save mobile/item template data.
 *	6/25/06: Kit
 *		Initial Version
 */

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Prompts;
using Server.Targeting;
using Server.Misc;
using Server.Multis;

namespace Server.Commands
{

	public class SaveSpawners
	{
		public static void Usage(Mobile to)
		{
			to.SendMessage("Usage: SaveSpawners[<FileName> <X1> <Y1> <X2> <Y2>");
		}

		public static void Initialize()
		{
			Server.CommandSystem.Register("SaveSpawners", AccessLevel.Administrator, new CommandEventHandler(SaveSpawners_OnCommand));
		}

		public static void CopyProperties(Item dest, Item src)
		{
			PropertyInfo[] props = src.GetType().GetProperties();

			for (int i = 0; i < props.Length; i++)
			{
				try
				{
					if (props[i].CanRead && props[i].CanWrite)
					{
						props[i].SetValue(dest, props[i].GetValue(src, null), null);
					}
				}
				catch
				{

				}
			}
		}

		[Usage("SaveSpawners")]
		[Description("Saves All spawners in designated X/Y to specificed file name.")]
		public static void SaveSpawners_OnCommand(CommandEventArgs e)
		{

			if (e.Arguments.Length == 5)
			{
				int count = 0;
				int x1, y1, x2, y2;
				string FileName = e.Arguments[0].ToString();
				try
				{
					x1 = Int32.Parse(e.Arguments[1]);
					y1 = Int32.Parse(e.Arguments[2]);
					x2 = Int32.Parse(e.Arguments[3]);
					y2 = Int32.Parse(e.Arguments[4]);
				}
				catch
				{
					Usage(e.Mobile);
					return;
				}
				//adjust rect				
				if (x1 > x2)
				{
					int x3 = x1;
					x1 = x2;
					x2 = x3;
				}
				if (y1 > y2)
				{
					int y3 = y1;
					y1 = y2;
					y2 = y3;
				}
				string itemIdxPath = Path.Combine("Saves/Spawners/", FileName + ".idx");
				string itemBinPath = Path.Combine("Saves/Spawners/", FileName + ".bin");

				try
				{
					ArrayList list = new ArrayList();
					foreach (Item item in Server.World.Items.Values)
					{
						if (item is Spawner)
						{
							if (item.X >= x1 && item.Y >= y1 && item.X < x2 && item.Y < y2 && item.Map == e.Mobile.Map)
								list.Add(item);
						}
					}

					if (list.Count > 0)
					{
						try
						{
							string folder = Path.GetDirectoryName(itemIdxPath);

							if (!Directory.Exists(folder))
							{
								Directory.CreateDirectory(folder);
							}

						}
						catch
						{
							e.Mobile.SendMessage("An error occured while trying to create Spawner folder.");
						}

						count = list.Count;
						GenericWriter idx;
						GenericWriter bin;

						idx = new BinaryFileWriter(itemIdxPath, false);
						bin = new BinaryFileWriter(itemBinPath, true);

						idx.Write((int)list.Count);

						for (int i = 0; i < list.Count; ++i)
						{
							long start = bin.Position;
							Spawner temp = new Spawner();
							CopyProperties(temp, (Spawner)list[i]);

							idx.Write((long)start);
							//dont save template data as we cant load it back properly
							temp.TemplateItem = null;
							temp.TemplateMobile = null;
							temp.CreaturesName = ((Spawner)list[i]).CreaturesName;
							temp.Serialize(bin);

							idx.Write((int)(bin.Position - start));
							temp.Delete();
						}
						idx.Close();
						bin.Close();
					}
				}
				catch (Exception ex)
				{
					LogHelper.LogException(ex);
					System.Console.WriteLine("Exception Caught in SaveSpawner code: " + ex.Message);
					System.Console.WriteLine(ex.StackTrace);
				}

				e.Mobile.SendMessage("{0} Spawners Saved.", count);
			}
			else
			{
				Usage(e.Mobile);
			}
		}

	}

	public class WipeSpawners
	{
		public static void Usage(Mobile to)
		{
			to.SendMessage("Usage: WipeSpawners[<X1> <Y1> <X2> <Y2>");
		}

		public static void Initialize()
		{
			Server.CommandSystem.Register("WipeSpawners", AccessLevel.Administrator, new CommandEventHandler(WipeSpawners_OnCommand));
		}

		[Usage("WipeSpawners")]
		[Description("Wipes All spawners in designated X/Y area.")]
		public static void WipeSpawners_OnCommand(CommandEventArgs e)
		{
			if (e.Arguments.Length == 4)
			{
				int count = 0;
				int x1, y1, x2, y2;
				try
				{
					x1 = Int32.Parse(e.Arguments[1]);
					y1 = Int32.Parse(e.Arguments[2]);
					x2 = Int32.Parse(e.Arguments[3]);
					y2 = Int32.Parse(e.Arguments[4]);
				}
				catch
				{
					Usage(e.Mobile);
					return;
				}
				//adjust rect				
				if (x1 > x2)
				{
					int x3 = x1;
					x1 = x2;
					x2 = x3;
				}
				if (y1 > y2)
				{
					int y3 = y1;
					y1 = y2;
					y2 = y3;
				}

				try
				{
					ArrayList list = new ArrayList();
					foreach (Item item in Server.World.Items.Values)
					{
						if (item is Spawner)
						{
							if (item.X >= x1 && item.Y >= y1 && item.X < x2 && item.Y < y2 && item.Map == e.Mobile.Map)
								list.Add(item);
						}
					}

					if (list.Count > 0)
					{

						count = list.Count;
						for (int i = 0; i < list.Count; ++i)
						{
							((Item)list[i]).Delete();
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.LogException(ex);
					System.Console.WriteLine("Exception Caught in WipeSpawner code: " + ex.Message);
					System.Console.WriteLine(ex.StackTrace);
				}

				e.Mobile.SendMessage("{0} Spawners Deleted.", count);
			}
			else
			{
				Usage(e.Mobile);
			}
		}

	}

	public class ActivateSpawners
	{
		public static void Usage(Mobile to)
		{
			to.SendMessage("Usage: ActivateSpawners[<X1> <Y1> <X2> <Y2>");
		}

		public static void Initialize()
		{
			Server.CommandSystem.Register("ActivateSpawners", AccessLevel.Administrator, new CommandEventHandler(ActivateSpawners_OnCommand));
		}

		[Usage("ActivateSpawners")]
		[Description("Activates All spawners in designated X/Y area.")]
		public static void ActivateSpawners_OnCommand(CommandEventArgs e)
		{
			if (e.Arguments.Length == 4)
			{
				int count = 0;
				int x1, y1, x2, y2;
				try
				{
					x1 = Int32.Parse(e.Arguments[0]);
					y1 = Int32.Parse(e.Arguments[1]);
					x2 = Int32.Parse(e.Arguments[2]);
					y2 = Int32.Parse(e.Arguments[3]);
				}
				catch
				{
					Usage(e.Mobile);
					return;
				}
				//adjust rect				
				if (x1 > x2)
				{
					int x3 = x1;
					x1 = x2;
					x2 = x3;
				}
				if (y1 > y2)
				{
					int y3 = y1;
					y1 = y2;
					y2 = y3;
				}

				try
				{
					ArrayList list = new ArrayList();
					foreach (Item item in Server.World.Items.Values)
					{
						if (item is Spawner)
						{
							if (item.X >= x1 && item.Y >= y1 && item.X < x2 && item.Y < y2 && item.Map == e.Mobile.Map)
								list.Add(item);
						}
					}

					if (list.Count > 0)
					{

						count = list.Count;
						for (int i = 0; i < list.Count; ++i)
						{
							((Spawner)list[i]).Running = true;
							((Spawner)list[i]).Respawn();
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.LogException(ex);
					System.Console.WriteLine("Exception Caught in ActivateSpawner code: " + ex.Message);
					System.Console.WriteLine(ex.StackTrace);
				}

				e.Mobile.SendMessage("{0} Spawners Activated.", count);
			}
			else
			{
				Usage(e.Mobile);
			}
		}

	}

	public class DeactivateSpawners
	{
		public static void Usage(Mobile to)
		{
			to.SendMessage("Usage: DeactivateSpawners[<X1> <Y1> <X2> <Y2>");
		}

		public static void Initialize()
		{
			Server.CommandSystem.Register("DeactivateSpawners", AccessLevel.Administrator, new CommandEventHandler(DeactivateSpawners_OnCommand));
		}

		[Usage("DeactivateSpawners")]
		[Description("Deactivates All spawners in designated X/Y area.")]
		public static void DeactivateSpawners_OnCommand(CommandEventArgs e)
		{
			if (e.Arguments.Length == 4)
			{
				int count = 0;
				int x1, y1, x2, y2;
				try
				{
					x1 = Int32.Parse(e.Arguments[0]);
					y1 = Int32.Parse(e.Arguments[1]);
					x2 = Int32.Parse(e.Arguments[2]);
					y2 = Int32.Parse(e.Arguments[3]);
				}
				catch
				{
					Usage(e.Mobile);
					return;
				}
				//adjust rect				
				if (x1 > x2)
				{
					int x3 = x1;
					x1 = x2;
					x2 = x3;
				}
				if (y1 > y2)
				{
					int y3 = y1;
					y1 = y2;
					y2 = y3;
				}

				try
				{
					ArrayList list = new ArrayList();
					foreach (Item item in Server.World.Items.Values)
					{
						if (item is Spawner)
						{
							if (item.X >= x1 && item.Y >= y1 && item.X < x2 && item.Y < y2 && item.Map == e.Mobile.Map)
								list.Add(item);
						}
					}

					if (list.Count > 0)
					{

						count = list.Count;
						for (int i = 0; i < list.Count; ++i)
						{
							((Spawner)list[i]).Running = false;
							((Spawner)list[i]).RemoveCreatures();
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.LogException(ex);
					System.Console.WriteLine("Exception Caught in DeactivateSpawner code: " + ex.Message);
					System.Console.WriteLine(ex.StackTrace);
				}

				e.Mobile.SendMessage("{0} Spawners Deactivated.", count);
			}
			else
			{
				Usage(e.Mobile);
			}
		}

	}

	public class LoadSpawners
	{
		private interface IEntityEntry
		{
			long Position { get; }
			int Length { get; }
			object Object { get; }
		}

		private sealed class ItemEntry : IEntityEntry
		{
			private Item m_Item;
			private long m_Position;
			private int m_Length;

			public object Object
			{
				get
				{
					return m_Item;
				}
			}

			public long Position
			{
				get
				{
					return m_Position;
				}
			}

			public int Length
			{
				get
				{
					return m_Length;
				}
			}

			public ItemEntry(Item item, long pos, int length)
			{
				m_Item = item;
				m_Position = pos;
				m_Length = length;
			}
		}

		public static void Initialize()
		{
			Server.CommandSystem.Register("LoadSpawners", AccessLevel.Administrator, new CommandEventHandler(LoadSpawners_OnCommand));
		}

		[Usage("LoadSpawners")]
		[Description("Loads All spawners in from saved spawner file.")]
		public static void LoadSpawners_OnCommand(CommandEventArgs e)
		{
			int count = 0;
			int itemCount = 0;
			Hashtable m_Items;
			if (e.Arguments.Length == 1)
			{
				string FileName = e.Arguments[0].ToString();
				string itemIdxPath = Path.Combine("Saves/Spawners/", FileName + ".idx");
				string itemBinPath = Path.Combine("Saves/Spawners/", FileName + ".bin");

				try
				{
					ArrayList items = new ArrayList();
					if (File.Exists(itemIdxPath))
					{
						using (FileStream idx = new FileStream(itemIdxPath, FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							BinaryReader idxReader = new BinaryReader(idx);

							itemCount = idxReader.ReadInt32();
							count = itemCount;

							m_Items = new Hashtable(itemCount);

							for (int i = 0; i < itemCount; ++i)
							{
								long pos = idxReader.ReadInt64();
								int length = idxReader.ReadInt32();

								Item item = null;

								try
								{
									item = new Spawner();
								}
								catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

								if (item != null)
								{
									items.Add(new ItemEntry(item, pos, length));
									World.AddItem(item);

								}
							}

							idxReader.Close();
						}


					}
					else
					{
						e.Mobile.SendMessage("File Not Found {0}.idx", FileName);
					}

					if (File.Exists(itemBinPath))
					{
						using (FileStream bin = new FileStream(itemBinPath, FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							BinaryFileReader reader = new BinaryFileReader(new BinaryReader(bin));

							for (int i = 0; i < items.Count; ++i)
							{
								ItemEntry entry = (ItemEntry)items[i];
								Item item = (Item)entry.Object;

								if (item != null)
								{
									reader.Seek(entry.Position, SeekOrigin.Begin);

									try
									{
										item.Deserialize(reader);

										if (reader.Position != (entry.Position + entry.Length))
											throw new Exception(String.Format("***** Bad serialize on {0} *****", item.GetType()));

										item.MoveToWorld(item.Location, item.Map);
									}
									catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
								}
							}

							reader.Close();
						}

					}
					else
					{
						e.Mobile.SendMessage("File Not Found {0}.bin", FileName);
					}

				}
				catch (Exception ex)
				{
					LogHelper.LogException(ex);
					System.Console.WriteLine("Exception Caught in LoadSpawner code: " + ex.Message);
					System.Console.WriteLine(ex.StackTrace);
				}

				e.Mobile.SendMessage("{0} Spawners Loaded.", count);
			}
			else
			{
				e.Mobile.SendMessage("[Usage <FileName>");
			}
		}
	}
}
#endif
