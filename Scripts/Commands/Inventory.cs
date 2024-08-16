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

/* Scripts/Commands/Inventory.cs
 * CHANGELOG:
 *	8/28/07, Adam
 *		Remove default game-screen display.
 *		Change format of type specification on the commandline
 *		Add a verbose switch if game-screen display is desired
 *	8/27/07, Adam
 *		Redesign name acqustion logic
 *		change sorting to be based on the name
 *		add serial number to output
 *	8/23/07, Adam
 *		- Add support for Static items
 *		- fix formatting for display output
 *	08/23/07, plasma
 *		Remove ToTitleCase call
 *  08/13/07, plasma
 *		Fix previous change (whoops) plus add new GetDescription() method to InvItem
 *  08/06/07, plasma
 *		Add m_description field, populated with name from ItemData when the item has no core Name prop set, or type.name == "Item"
 *		Logic to display description if exists + change duplicate item check to include new field.
 *	11/10/05, erlein
 *		Added additional type to hold enchanted scroll's hidden type
 *		and code to display this on callback
 *	11/04/05, erlein
 *		Altered handling of enchanted scrolls to include their magic
 *		properties (guarding, vanq, etc.)
 *	04/22/05, erlein
 *        - Adapated to use Amount property for count if object being
 *        inventoried is an item
 *        - Added containers to the items inventoried
 *        - Fixed deep nesting problem with container types
 *	03/28/05, erlein
 *		Added "Slayer" column to handle "Silver"
 *	03/28/05, erlein
 *		- Changed method of type checking so uses 'is' rather
 *		than an exact one via a string match.
 *        - Moved "protection level" of magic armour to Damage column and
 *		renamed header accordingly.
 *		- Added exceptional quality check for player crafted items.
 *	03/26/05, erlein
 *		Added header output and modified so recognises + creates
 *		inventory for magic properties of armor as well as weapon.
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *	03/23/05, erlein
 *		Initial creation.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Commands
{
    public class Inventory
    {
        private static ArrayList m_Inv;
        private static Type m_ItemType;
        public static Mobile m_from;
        private static bool m_verbose = false;

        // Each InvItem instance holds a distinct type/damage/dura/acc/qual

        public class InvItem : IComparable
        {
            public Serial m_serial;
            public Type m_type;
            public int m_count;
            public string m_damage;
            public string m_accuracy;
            public string m_durability;
            public string m_quality;
            public string m_slayer;
            public string m_description;

            public InvItem(Type type)
            {
                m_count = 1;
                m_type = type;
            }

            public Int32 CompareTo(Object obj)
            {
                InvItem tmpObj = (InvItem)obj;
                return (this.m_description.CompareTo(tmpObj.m_description));
            }

            public string GetDescription()
            {
                return m_description;
            }

        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("Inventory", AccessLevel.Administrator, new CommandEventHandler(Inventory_OnCommand));
        }

        [Usage("Inventory [<type=>] [<-v>]")]
        [Description("Finds all items within bounding box.")]
        public static void Inventory_OnCommand(CommandEventArgs e)
        {
            m_Inv = new ArrayList();
            m_from = e.Mobile;
            m_ItemType = null;
            m_verbose = false;

            // process commandline switches
            foreach (string sx in e.Arguments)
            {
                if (sx == null)
                    continue;

                if (sx.StartsWith("type="))
                {
                    string typeName = sx.Substring(5);
                    // We have an argument, so try to convert to a type
                    m_ItemType = ScriptCompiler.FindTypeByName(typeName);
                    if (m_ItemType == null)
                    {
                        // Invalid
                        e.Mobile.SendMessage(String.Format("No type with the name '{0}' was found.", typeName));
                        return;
                    }
                }
                else if (sx == "-v")
                    m_verbose = true;
            }

            // Request a callback from a bounding box to establish 2D rect
            // for use with command

            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(InvBox_Callback), 0x01);
        }

        private static void InvBox_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {

            LogHelper Logger = new LogHelper("inventory.log", true);

            Logger.Log(LogType.Text, string.Format("{0}\t{1,-25}\t{7,-25}\t{2,-25}\t{3,-20}\t{4,-20}\t{5,-20}\t{6,-20}",
                "Qty ---",
                "Item ------------",
                "Damage / Protection --",
                "Durability -----",
                "Accuracy -----",
                "Exceptional",
                "Slayer ----",
                "Serial ----"));

            // Create rec and retrieve items within from bounding box callback
            // result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            IPooledEnumerable eable = map.GetItemsInBounds(rect);

            // Loop through and add objects returned
            foreach (object obj in eable)
            {

                if (m_ItemType == null || obj is BaseContainer)
                    AddInv(obj);
                else
                {
                    Type ot = obj.GetType();

                    if (ot.IsSubclassOf(m_ItemType) || ot == m_ItemType)
                        AddInv(obj);
                }
            }

            eable.Free();

            m_Inv.Sort();   // Sort results

            // Loop and log
            foreach (InvItem ir in m_Inv)
            {
                // ir.m_description += String.Format(" ({0})", it.Serial.ToString());
                string output = string.Format("{0}\t{1,-25}\t{7,-25}\t{2,-25}\t{3,-20}\t{4,-20}\t{5,-20}\t{6,-20}",
                    ir.m_count + ",",
                    (ir.GetDescription()) + ",",
                    (ir.m_damage != null ? ir.m_damage : "N/A") + ",",
                    (ir.m_durability != null ? ir.m_durability : "N/A") + ",",
                    (ir.m_accuracy != null ? ir.m_accuracy : "N/A") + ",",
                    (ir.m_quality != null ? ir.m_quality : "N/A") + ",",
                    (ir.m_slayer != null ? ir.m_slayer : "N/A") + ",",
                    ir.m_serial.ToString()
                    );

                Logger.Log(LogType.Text, output);

                if (m_verbose)
                {
                    output = string.Format("{0}{1}{7}{2}{3}{4}{5}{6}",
                        ir.m_count + ",",
                        (ir.GetDescription()) + ",",
                        (ir.m_damage != null ? ir.m_damage : "N/A") + ",",
                        (ir.m_durability != null ? ir.m_durability : "N/A") + ",",
                        (ir.m_accuracy != null ? ir.m_accuracy : "N/A") + ",",
                        (ir.m_quality != null ? ir.m_quality : "N/A") + ",",
                        (ir.m_slayer != null ? ir.m_slayer : "N/A") + ",",
                        ir.m_serial.ToString()
                        );

                    from.SendMessage(output);
                }
            }

            Logger.Count--; // count-1 for header
            Logger.Finish();
        }

        private static void AddInv(object o)
        {

            // Handle contained objects (iterative ;)

            if (o is BaseContainer)
            {
                foreach (Item item in ((BaseContainer)o).Items)
                {
                    if (m_ItemType == null)
                    {
                        AddInv(item);
                        continue;
                    }

                    Type it = item.GetType();

                    if (it.IsSubclassOf(m_ItemType) || it == m_ItemType || item is BaseContainer)
                        AddInv(item);
                }

                // Do we want to inventory this container, or return?
                Type ct = o.GetType();

                if (!(m_ItemType == null) && !ct.IsSubclassOf(m_ItemType) && ct != m_ItemType)
                    return;
            }

            // Handle this object

            InvItem ir = new InvItem(o.GetType());

            // Determine and set inv item properties

            if (o is BaseWeapon)
            {
                BaseWeapon bw = (BaseWeapon)o;

                ir.m_accuracy = bw.AccuracyLevel.ToString();
                ir.m_damage = bw.DamageLevel.ToString();
                ir.m_durability = bw.DurabilityLevel.ToString();
                ir.m_slayer = bw.Slayer.ToString();

            }
            else if (o is BaseArmor)
            {
                BaseArmor ba = (BaseArmor)o;

                ir.m_durability = ba.Durability.ToString();
                ir.m_damage = ba.ProtectionLevel.ToString();

            }
            else if (o is EnchantedScroll)
            {
                EnchantedItem ei = (EnchantedItem)o;

                // ProtectionLevel, Durability

                if (ei.ItemType.IsSubclassOf(typeof(BaseArmor)))
                {
                    ir.m_durability = ((ArmorDurabilityLevel)ei.iProps[1]).ToString();
                    ir.m_damage = ((ArmorProtectionLevel)(ei.iProps[0])).ToString();
                }
                else if (ei.ItemType.IsSubclassOf(typeof(BaseWeapon)))
                {
                    ir.m_accuracy = ((WeaponAccuracyLevel)ei.iProps[2]).ToString();
                    ir.m_damage = ((WeaponDamageLevel)ei.iProps[0]).ToString();
                    ir.m_durability = ((WeaponDurabilityLevel)ei.iProps[1]).ToString();
                    ir.m_slayer = ((SlayerName)ei.iProps[3]).ToString();
                }
            }

            if (o is Item)
            {

                Item it = (Item)o;

                if (it.PlayerCrafted == true)
                {
                    // It's playercrafted, so check for 'Quality' property
                    string strVal = Properties.GetValue(m_from, o, "Quality");

                    if (strVal == "Quality = Exceptional")
                        ir.m_quality = "Exceptional";
                }

                if (it.Amount > 0)
                    ir.m_count = it.Amount;

                ir.m_serial = it.Serial;

                // adam: Find the best name we can
                if (o is EnchantedScroll)
                {
                    EnchantedItem ei = (EnchantedItem)o;
                    ir.m_description = ei.ItemType.Name + ".scroll";
                }
                else
                {
                    if (valid(it.Name))
                        ir.m_description = it.Name;
                    else if (valid(it.ItemData.Name) && (it.GetType().Name == null || it.GetType().Name == "Item" || it.GetType().Name == "Static"))
                        ir.m_description = it.ItemData.Name;
                    else
                        ir.m_description = it.GetType().Name;
                }
            }

            // Make sure there are no others like this

            foreach (InvItem ii in m_Inv)
            {

                if (ii.m_type == ir.m_type &&
                    ii.m_accuracy == ir.m_accuracy &&
                    ii.m_damage == ir.m_damage &&
                    ii.m_quality == ir.m_quality &&
                    ii.m_durability == ir.m_durability &&
                    ii.m_slayer == ir.m_slayer &&
                    ii.m_description == ir.m_description) //pla: include new field in this check
                {

                    // It exists, so increment and return
                    ii.m_count += ir.m_count;

                    return;
                }
            }

            // It doesn't exist, so add it
            m_Inv.Add(ir);
        }

        private static bool valid(string sx)
        {
            if (sx == null || sx == "") return false; else return true;
        }
    }
}
