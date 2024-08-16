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

using Server.Network;
using System.Collections;
using System.Collections.Generic;

namespace Server
{
    public class Sector
    {
        private int m_X, m_Y;
        private Map m_Owner;
        private Dictionary<Serial, object> m_Mobiles;
        private Dictionary<Serial, object> m_Items;
        private Dictionary<Serial, object> m_Clients;
        private Dictionary<Serial, object> m_Multis;
        private ArrayList m_Regions;        // sorted, so it can't be a HashTable
        private Dictionary<Serial, object> m_Players;
        private bool m_Active;

        private static ArrayList m_DefaultList = ArrayList.ReadOnly(new ArrayList(0));
        private static Dictionary<Serial, object> m_DefaultTable = new Dictionary<Serial, object>(0);

        //public static ArrayList EmptyList
        //{
        //get { return m_DefaultList; }
        //}

        public Sector(int x, int y, Map owner)
        {
            m_X = x;
            m_Y = y;
            m_Owner = owner;
            m_Active = false;
        }

        public void OnClientChange(NetState oldState, NetState newState)
        {
            if (m_Clients != null && oldState != null && oldState.Mobile != null)
                m_Clients.Remove(oldState.Mobile.Serial);

            if (newState != null && newState.Mobile != null)
            {
                if (m_Clients == null)
                    m_Clients = new Dictionary<Serial, object>(4);

                if (m_Clients.ContainsKey(newState.Mobile.Serial) == false)
                    m_Clients.Add(newState.Mobile.Serial, newState);
            }
        }

        public void OnEnter(Item item)
        {
            if (m_Items == null)
                m_Items = new Dictionary<Serial, object>();

            if (m_Items.ContainsKey(item.Serial) == false)
                m_Items.Add(item.Serial, item);
        }

        public void OnLeave(Item item)
        {
            if (m_Items != null)
                m_Items.Remove(item.Serial);
        }

        public void OnEnter(Mobile m)
        {
            if (m_Mobiles == null)
                m_Mobiles = new Dictionary<Serial, object>(4);

            if (m_Mobiles.ContainsKey(m.Serial) == false)
                m_Mobiles.Add(m.Serial, m);

            if (m.NetState != null)
            {
                if (m_Clients == null)
                    m_Clients = new Dictionary<Serial, object>(4);

                if (m_Clients.ContainsKey(m.Serial) == false)
                    m_Clients.Add(m.Serial, m.NetState);
            }

            if (m.Player)
            {
                if (m_Players == null)
                    m_Players = new Dictionary<Serial, object>(4);

                if (m_Players.ContainsKey(m.Serial) == false)
                    m_Players.Add(m.Serial, m);

                if (m_Players.Count == 1)//first player
                    Owner.ActivateSectors(m_X, m_Y);
            }
        }

        public void OnLeave(Mobile m)
        {
            if (m_Mobiles != null)
                m_Mobiles.Remove(m.Serial);

            if (m_Clients != null && m.NetState != null)
                m_Clients.Remove(m.Serial);

            if (m.Player)
            {
                if (m_Players != null)
                    m_Players.Remove(m.Serial);

                if (m_Players == null || m_Players.Count == 0)
                    Owner.DeactivateSectors(m_X, m_Y);
            }
        }

        public void OnEnter(Region r)
        {
            if (m_Regions == null || !m_Regions.Contains(r))
            {
                if (m_Regions == null)
                    m_Regions = new ArrayList();

                m_Regions.Add(r);
                m_Regions.Sort();

                if (m_Mobiles != null)
                    foreach (Mobile m in m_Mobiles.Values)
                        m.ForceRegionReEnter(true);

                /*if (m_Mobiles != null && m_Mobiles.Count > 0)
				{
					ArrayList list = new ArrayList(m_Mobiles.Values);

					for (int i = 0; i < list.Count; ++i)
						((Mobile)list[i]).ForceRegionReEnter(true);
				}*/
            }
        }

        public void OnLeave(Region r)
        {
            if (m_Regions != null)
                m_Regions.Remove(r);
        }

        public void OnMultiEnter(Item item)
        {
            if (m_Multis == null)
                m_Multis = new Dictionary<Serial, object>(4);

            if (m_Multis.ContainsKey(item.Serial) == false)
                m_Multis.Add(item.Serial, item);
        }

        public void OnMultiLeave(Item item)
        {
            if (m_Multis != null)
                m_Multis.Remove(item.Serial);
        }

        public void Activate()
        {
            if (!Active && m_Owner != Map.Internal)//only activate if its the first player in
            {
                if (m_Items != null)
                    foreach (Item item in m_Items.Values)
                        item.OnSectorActivate();

                //for (int i = 0; m_Items != null && i < m_Items.Count; i++)
                //((Item)m_Items.Values[i]).OnSectorActivate();


                if (m_Mobiles != null)
                    foreach (Mobile m in m_Mobiles.Values)
                    {
                        if (m.Player == false)
                            m.OnSectorActivate();
                    }

                /*for (int i = 0; m_Mobiles != null && i < m_Mobiles.Count; i++)
				{
					Mobile m = (Mobile)m_Mobiles.Values[i];

					if (!m.Player)
						m.OnSectorActivate();
				}*/

                m_Active = true;
            }
        }

        public void Deactivate()
        {
            if (Active && (m_Players == null || m_Players.Count == 0))//only deactivate if there's really no more players here
            {

                if (m_Items != null)
                    foreach (Item item in m_Items.Values)
                        item.OnSectorDeactivate();

                //for (int i = 0; m_Items != null && i < m_Items.Count; i++)
                //((Item)m_Items[i]).OnSectorDeactivate();

                if (m_Mobiles != null)
                    foreach (Mobile m in m_Mobiles.Values)
                        m.OnSectorDeactivate();

                //for (int i = 0; m_Mobiles != null && i < m_Mobiles.Count; i++)
                //((Mobile)m_Mobiles.Values[i]).OnSectorDeactivate();

                m_Active = false;
            }
        }

        public ArrayList Regions
        {
            get
            {
                if (m_Regions == null)
                    return m_DefaultList;

                return m_Regions;
            }
        }

        public Dictionary<Serial, object> Multis
        {
            get
            {
                if (m_Multis == null)
                    return m_DefaultTable;

                return m_Multis;
            }
        }

        public Dictionary<Serial, object> Mobiles
        {
            get
            {
                if (m_Mobiles == null)
                    return m_DefaultTable;

                return m_Mobiles;
            }
        }

        public Dictionary<Serial, object> Items
        {
            get
            {
                if (m_Items == null)
                    return m_DefaultTable;

                return m_Items;
            }
        }

        public Dictionary<Serial, object> Clients
        {
            get
            {
                if (m_Clients == null)
                    return m_DefaultTable;

                return m_Clients;
            }
        }

        public Dictionary<Serial, object> Players
        {
            get
            {
                if (m_Players == null)
                    return m_DefaultTable;

                return m_Players;
            }
        }

        public bool Active
        {
            get
            {
                return (m_Active && m_Owner != Map.Internal);
            }
        }

        public Map Owner
        {
            get
            {
                return m_Owner;
            }
        }

        public int X
        {
            get
            {
                return m_X;
            }
        }

        public int Y
        {
            get
            {
                return m_Y;
            }
        }
    }
}
