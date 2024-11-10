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

/* Scripts/Engines/ClanSystem/ClanSystem.cs
 * CHANGELOG:
 *	9/25/2024, Adam
 *		Initial Version
 */

/* Clan System Explained
 *  Clans are a subcategory of Kin. Orc kin for instance can have any number of Clans. (Example: Blood Rock)
 *  Currently, only NPCs may be clan members.
 *  Flagging: When you as a player belong kin, and you attack that kin, your alignment is switched to Outcast which make you attackable
 *      to your own kin. Clans operate differently. When you attack a clan member, you are not set to Outcast, but rather added to a temporal
 *      list of clan aggressors. (5 minutes, just like Outcast.) The greater kin population will not attack you, but members of that clan will.
 *      Clan members auto-attack all other clans.  The greater kin population are indifferent to this.
 *  Clans: Clans are dynamic. They are not defined in the code anywhere. A GM/spawner/champ engine need only set the ClanAlignment string of the 
 *      kin aligned mobile. The same string in multiple mobiles will align these mobiles. The 'string', for example "red" or "blue" is never 
 *      used internally by the system. All of these clan strings are hashed to an integer, and this is how they are operated upon. 
 *      For example, the dictionary of clan aggressors is Keyed on the attacker mobile, and the Value is a list of clan IDs offended.
 *      IsEnemy will check this dictionary to see if the mobile in question has offended our clan. If so, you are an enemy.
 */

using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.ClanSystem
{
    public enum ClanAlignment
    {
        None,
        Healer,
        OutCast
    }
    public class ClanSystem
    {

        public static Memory ClanAggressionTable = new Memory();
        private static double MemoryTime = TimeSpan.FromMinutes(5).TotalSeconds;
        public static int SendClanMessage(string clan, string message, int hue = 0x482)
        {
            return SendClanMessage(Utility.StringToInt(clan), message, hue);
        }
        public static int SendClanMessage(int clan, string message, int hue = 0x482)
        {
            message = "Clan Message: " + message;
            int count = 0;
            foreach (KeyValuePair<Mobile, int> kvp in Mobile.ClanRegistry)
                if (kvp.Value == clan && kvp.Key is PlayerMobile pm && pm.NetState != null)
                {
                    pm.SendMessage(hue: hue, message);
                    count++;
                }

            return count;
        }
        public static void EstablishClanAggression(Mobile attacker, Mobile defender)
        {
            Memory.ObjectMemory om = ClanAggressionTable.Recall(attacker as object);
            if (om == null)
            {   // nope, don't remember him.
                //  We will remember him and the clan he attacked for 5 minutes
                ClanAggressionTable.Remember(attacker, new List<int>() { GetClanAlignment(defender) }, MemoryTime);
            }
            else
            {   // yes we remember him.
                //  We will refresh our memory and update the list of clan attacked
                System.Diagnostics.Debug.Assert(om.Context != null);
                List<int> clans = ((IEnumerable)om.Context).Cast<int>().ToList();
                if (!clans.Contains(GetClanAlignment(defender)))
                    clans.Add(GetClanAlignment(defender));
                // now refresh our memory of him
                ClanAggressionTable.Remember(attacker, clans, MemoryTime);
            }
        }
        public static int GetClanAlignment(Mobile m)
        {
            if (m == null)
                return (int)ClanAlignment.None;

            if (m is BaseCreature bc && bc.Controlled && bc.ControlMaster != null)
                return GetClanAlignment(bc.ControlMaster);

            if (string.IsNullOrEmpty(m.ClanAlignment))
                return (int)ClanAlignment.None;

            // remove spaces and lowercase, and convert to int - avoids typos
            return Utility.StringToInt(m.ClanAlignment);
        }
        public static string GetClanAlignmentAsString(Mobile m)
        {
            if (m == null)
                return string.Empty;

            if (m is BaseCreature bc && bc.Controlled && bc.ControlMaster != null)
                return bc.ControlMaster.ClanAlignment;

            if (string.IsNullOrEmpty(m.ClanAlignment))
                return string.Empty;

            return m.ClanAlignment;
        }
        public static bool IsClanAlignedNPC(Mobile m)
        {
            return m.Player == false && IsClanAligned(m);
        }
        public static bool IsClanAligned(Mobile m)
        { return GetClanAlignment(m) != (int)ClanAlignment.None; }

        private static List<int> Offenses(Mobile m)
        {
            // if it's a pet, assume the Offenses of your owner
            if (m is BaseCreature bc && bc.Controlled && bc.ControlMaster != null)
                m = bc.ControlMaster;

            List<int> list = new List<int>();
            Memory.ObjectMemory om = ClanAggressionTable.Recall(m as object);
            if (om != null)
            {   // yes we remember him.
                //  return the list of clans he has attacked
                System.Diagnostics.Debug.Assert(om.Context != null);
                List<int> clans = ((IEnumerable)om.Context).Cast<int>().ToList();
                list.AddRange(clans);
            }
            return list;
        }

        public static bool IsClanEnemy(Mobile m1, Mobile m2)
        {
            // no one is clan aligned
            if (!IsClanAligned(m1) && !IsClanAligned(m2))
                return false;

            int m1_clan = GetClanAlignment(m1);
            int m2_clan = GetClanAlignment(m2);
            List<int> m1_Offenses = Offenses(m1);
            List<int> m2_Offenses = Offenses(m2);

            // have you attacked me, or have I attacked you?
            if (m1_Offenses.Contains(m2_clan) || m2_Offenses.Contains(m1_clan))
                return true;

            // not an Clan enemy
            return false;
        }

        public static bool IsEnemy(Mobile m1, Mobile m2)
        {
            // A clan enemy is one that has attacked a clan member.
            //  This differs from attacking 'kin' as you don't go Outcast, but are instead opposing that clan (for 5 minutes)
            if (IsClanEnemy(m1, m2))
                return true;

            // can't be an Clan enemy if you're not aligned
            if (!IsClanAligned(m1) || !IsClanAligned(m2))
                return false;

            // must be an enemy if they are on different teams
            if (GetClanAlignment(m1) != GetClanAlignment(m2))
                return true;

            // not an Clan enemy
            return false;
        }

        public static bool IsFriend(Mobile m1, Mobile m2)
        {
            // can't be an Clan friend if you're not aligned
            if (!IsClanAligned(m1) || !IsClanAligned(m2))
                return false;

            // must be a friend if they are on the same teams
            if (GetClanAlignment(m1) == GetClanAlignment(m2))
                return true;

            // not an Clan friend
            return false;
        }
    }
}
