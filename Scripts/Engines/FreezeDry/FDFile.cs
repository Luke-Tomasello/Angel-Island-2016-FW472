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

/* Scripts/Commands/FDFile.cs
 * Changelog
 *  02/26/06 Taran Kain
 *		Added an UpdateTotals call for each item loaded.
 *	02/24/06 Taran Kain
 *		Initial version.
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Server.Commands
{
    /// <summary>
    /// Summary description for FDFile.
    /// </summary>
    public class FDFile
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("DumpCont", AccessLevel.Administrator, new CommandEventHandler(On_FDFile));
            Server.CommandSystem.Register("LoadCont", AccessLevel.Administrator, new CommandEventHandler(On_RHFile));
            Server.CommandSystem.Register("ReserveSerials", AccessLevel.Administrator, new CommandEventHandler(On_Reserve));
            Server.CommandSystem.Register("FreeSerials", AccessLevel.Administrator, new CommandEventHandler(On_Free));
            Server.CommandSystem.Register("ReassignSerials", AccessLevel.Administrator, new CommandEventHandler(On_Reassign));
        }

        public static void On_Reserve(CommandEventArgs e)
        {
            uint start, end;
            try
            {
                start = uint.Parse(e.Arguments[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                end = uint.Parse(e.Arguments[1].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

                if (start < 0x60000000 || end >= 0x70000000 || start > end)
                    throw new Exception();
            }
            catch
            {
                e.Mobile.SendMessage("Usage: [ReserveSerials <start> <end>");
                e.Mobile.SendMessage("This command will reserve serials from <start> to <end> (inclusive). <start> and <end> must be in hex format. <start> must be equal to or above 0x60000000 and <end> must be below 0x70000000.");
                return;
            }

            for (int i = (int)start; i <= end; i++)
            {
                if (World.IsReserved(i) || World.FindItem(i) != null)
                {
                    e.Mobile.SendMessage("Failure: Serial # {0:X} is in use.", i);
                    return;
                }
            }

            for (int i = (int)start; i <= end; i++)
                World.ReserveSerial(i);

            e.Mobile.SendMessage("Serial range 0x{0:X} - 0x{1:X} successfully reserved.", start, end);
        }

        public static void On_Free(CommandEventArgs e)
        {
            uint start, end;
            try
            {
                start = uint.Parse(e.Arguments[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                end = uint.Parse(e.Arguments[1].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

                if (start < 0x60000000 || end >= 0x70000000 || start > end)
                    throw new Exception();
            }
            catch
            {
                e.Mobile.SendMessage("Usage: [FreeSerials <start> <end>");
                e.Mobile.SendMessage("This command will free serials from <start> to <end> (inclusive).  <start> and <end> must be in hex format. <start> must be equal to or above 0x60000000 and <end> must be below 0x70000000.");
                return;
            }

            for (int i = (int)start; i <= end; i++)
                World.FreeSerial(i);

            e.Mobile.SendMessage("Serial range 0x{0:X} - 0x{1:X} successfully freed.", start, end);
        }

        public static void On_Reassign(CommandEventArgs e)
        {
            uint start;
            try
            {
                start = uint.Parse(e.Arguments[0].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                if (start < 0x60000000 || start >= 0x70000000)
                    throw new Exception();
            }
            catch
            {
                e.Mobile.SendMessage("Usage: [ReassignSerials <start>");
                e.Mobile.SendMessage("This command will reassign serial numbers to the object selected and its children.  <start> must be in hex format. <start> must be equal to or above 0x60000000 and below 0x70000000.");
                return;
            }

            e.Mobile.SendMessage("Choose an item to assign a new serial number to.");
            e.Mobile.Target = new ReassignTarget(start);
        }

        private class ReassignTarget : Target
        {
            private uint m_Start;

            public ReassignTarget(uint start)
                : base(8, false, TargetFlags.None)
            {
                m_Start = start;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item reassign = targeted as Item;
                if (reassign == null)
                {
                    from.SendMessage("You must target an item to reassign.");
                    return;
                }

                ArrayList items = new ArrayList();
                items.Add(reassign);
                items.AddRange(reassign.GetDeepItems());

                if (m_Start + items.Count >= 0x70000000)
                {
                    from.SendMessage("There are too many items in that. Assigned serial numbers would overflow beyond 0x70000000.");
                    return;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    if (!World.IsReserved((int)(m_Start + i)))
                    {
                        from.SendMessage("Not all of the serial numbers that would be reassigned are reserved for reassignment. Failed at 0x{0:X}.", m_Start + i);
                        return;
                    }
                }

                for (int i = 0; i < items.Count; i++)
                {
                    Item item = items[i] as Item;
                    item.ReassignSerial((int)m_Start);
                    World.FreeSerial((int)m_Start);
                    m_Start++;
                }

                from.SendMessage("Successfully reassigned serials to {0} items. Next free reserved serial is 0x{1:X}.", items.Count, m_Start);
            }
        }


        public static void On_FDFile(CommandEventArgs e)
        {
            if (e.Arguments.Length != 1)
            {
                e.Mobile.SendMessage("Usage: [DumpCont <filename>");
                return;
            }

            e.Mobile.SendMessage("Choose the container to dump to {0}.", e.Arguments[0]);
            e.Mobile.Target = new FDFileTarget(e.Arguments[0]);
        }

        public static void On_RHFile(CommandEventArgs e)
        {
            if (e.Arguments.Length != 1)
            {
                e.Mobile.SendMessage("Usage: [LoadCont <filename>");
                return;
            }

            try
            {
                int loaded = 0;
                int count;
                LogHelper log = new LogHelper(e.Arguments[0] + " LoadCont.log");
                log.Log(LogType.Text, String.Format("Reload process initiated by {0}, with {1} as backup data.", e.Mobile, e.Arguments[0]));

                using (FileStream idxfs = new FileStream(e.Arguments[0] + ".idx", FileMode.Open, FileAccess.Read))
                {
                    using (FileStream binfs = new FileStream(e.Arguments[0] + ".bin", FileMode.Open, FileAccess.Read))
                    {
                        GenericReader bin = new BinaryFileReader(new BinaryReader(binfs));
                        GenericReader idx = new BinaryFileReader(new BinaryReader(idxfs));

                        count = idx.ReadInt();
                        if (count == -1)
                            log.Log(LogType.Text, "No item data to reload."); // do nothing
                        else
                        {
                            ArrayList items = new ArrayList(count);
                            log.Log(LogType.Text, String.Format("Attempting to reload {0} items.", count));

                            Type[] ctortypes = new Type[] { typeof(Serial) };
                            object[] ctorargs = new object[1];

                            for (int i = 0; i < count; i++)
                            {
                                string type = idx.ReadString();
                                Serial serial = (Serial)idx.ReadInt();
                                long position = idx.ReadLong();
                                int length = idx.ReadInt();

                                Type t = ScriptCompiler.FindTypeByFullName(type);
                                if (t == null)
                                {
                                    Console.WriteLine("Warning: Tried to load nonexistent type {0}. Ignoring item.", type);
                                    log.Log(String.Format("Warning: Tried to load nonexistent type {0}. Ignoring item.", type));
                                    continue;
                                }

                                ConstructorInfo ctor = t.GetConstructor(ctortypes);
                                if (ctor == null)
                                {
                                    Console.WriteLine("Warning: Tried to load type {0} which has no serialization constructor. Ignoring item.", type);
                                    log.Log(String.Format("Warning: Tried to load type {0} which has no serialization constructor. Ignoring item.", type));
                                    continue;
                                }

                                Item item = null;
                                try
                                {
                                    if (World.FindItem(serial) != null)
                                    {
                                        log.Log(LogType.Item, World.FindItem(serial), "Serial already in use!! Loading of saved item failed.");
                                    }
                                    else if (!World.IsReserved(serial))
                                    {
                                        log.Log(String.Format("Serial {0} is not reserved!! Loading of saved item failed.", serial));
                                    }
                                    else
                                    {
                                        ctorargs[0] = serial;
                                        item = (Item)(ctor.Invoke(ctorargs));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.LogException(ex);
                                    Console.WriteLine("An exception occurred while trying to invoke {0}'s serialization constructor.", t.FullName);
                                    Console.WriteLine(ex.ToString());
                                    log.Log(String.Format("An exception occurred while trying to invoke {0}'s serialization constructor.", t.FullName));
                                    log.Log(ex.ToString());
                                }

                                if (item != null)
                                {
                                    World.FreeSerial(serial);

                                    World.AddItem(item);
                                    items.Add(new object[] { item, position, length });
                                    log.Log(String.Format("Successfully created item {0}", item));
                                }
                            }

                            for (int i = 0; i < items.Count; i++)
                            {
                                object[] entry = (object[])items[i];
                                Item item = entry[0] as Item;
                                long position = (long)entry[1];
                                int length = (int)entry[2];

                                if (item != null)
                                {
                                    bin.Seek(position, SeekOrigin.Begin);

                                    try
                                    {
                                        item.Deserialize(bin);

                                        // take care of parent hierarchy
                                        object p = item.Parent;
                                        if (p is Item)
                                        {
                                            ((Item)p).RemoveItem(item);
                                            item.Parent = null;
                                            ((Item)p).AddItem(item);
                                        }
                                        else if (p is Mobile)
                                        {
                                            ((Mobile)p).RemoveItem(item);
                                            item.Parent = null;
                                            ((Mobile)p).AddItem(item);
                                        }
                                        else
                                        {
                                            item.Delta(ItemDelta.Update);
                                        }

                                        item.ClearProperties();

                                        object rp = item.RootParent;
                                        if (rp is Item)
                                            ((Item)rp).UpdateTotals();
                                        else if (rp is Mobile)
                                            ((Mobile)rp).UpdateTotals();
                                        else
                                            item.UpdateTotals();

                                        if (bin.Position != (position + length))
                                            throw new Exception(String.Format("Bad serialize on {0}", item));

                                        log.Log(LogType.Item, item, "Successfully loaded.");
                                        loaded++;
                                    }
                                    catch (Exception ex)
                                    {
                                        LogHelper.LogException(ex);
                                        Console.WriteLine("Caught exception while deserializing {0}:", item);
                                        Console.WriteLine(ex.ToString());
                                        Console.WriteLine("Deleting item.");
                                        log.Log(String.Format("Caught exception while deserializing {0}:", item));
                                        log.Log(ex.ToString());
                                        log.Log("Deleting item.");
                                        item.Delete();
                                    }
                                }
                            }

                        }
                        idx.Close();
                        bin.Close();
                    }
                }

                Console.WriteLine("Attempted to load {0} items: {1} loaded, {2} failed.", count, loaded, count - loaded);
                log.Log(String.Format("Attempted to load {0} items: {1} loaded, {2} failed.", count, loaded, count - loaded));
                e.Mobile.SendMessage("Attempted to load {0} items: {1} loaded, {2} failed.", count, loaded, count - loaded);
                log.Finish();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine(ex.ToString());
                e.Mobile.SendMessage("Exception: {0}", ex.Message);
            }
        }

        private class FDFileTarget : Target
        {
            private string m_Filename;

            public FDFileTarget(string filename)
                : base(8, false, TargetFlags.None)
            {
                m_Filename = filename;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is Container))
                {
                    from.SendMessage("Only containers can be dumped.");
                    return;
                }

                Container cont = (Container)targeted;

                try
                {
                    using (FileStream idxfs = new FileStream(m_Filename + ".idx", FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (FileStream binfs = new FileStream(m_Filename + ".bin", FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            GenericWriter idx = new BinaryFileWriter(idxfs, true);
                            GenericWriter bin = new BinaryFileWriter(binfs, true);

                            ArrayList items = new ArrayList();
                            items.Add(cont);
                            items.AddRange(cont.GetDeepItems());

                            idx.Write((int)items.Count);
                            foreach (Item item in items)
                            {
                                long start = bin.Position;

                                idx.Write(item.GetType().FullName); // <--- DIFFERENT FROM WORLD SAVE FORMAT!
                                idx.Write((int)item.Serial);
                                idx.Write((long)start);

                                item.Serialize(bin);

                                idx.Write((int)(bin.Position - start));
                            }


                            idx.Close();
                            bin.Close();
                        }
                    }

                    from.SendMessage("Container successfully dumped to {0}.", m_Filename);
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine(e.ToString());
                    from.SendMessage("Exception: {0}", e.Message);
                }
            }
        }
    }
}
