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

using System;

namespace Server.Factions
{
    public interface IFactionItem
    {
        FactionItem FactionItemState { get; set; }
    }

    public class FactionItem
    {
        public static readonly TimeSpan ExpirationPeriod = TimeSpan.FromDays(21.0);

        private Item m_Item;
        private Faction m_Faction;
        private DateTime m_Expiration;

        public Item Item { get { return m_Item; } }
        public Faction Faction { get { return m_Faction; } }
        public DateTime Expiration { get { return m_Expiration; } }

        public bool HasExpired
        {
            get
            {
                if (m_Item == null || m_Item.Deleted)
                    return true;

                return (m_Expiration != DateTime.MinValue && DateTime.UtcNow >= m_Expiration);
            }
        }

        public void StartExpiration()
        {
            m_Expiration = DateTime.UtcNow + ExpirationPeriod;
        }

        public void Attach()
        {
            if (m_Item is IFactionItem)
                ((IFactionItem)m_Item).FactionItemState = this;

            if (m_Faction != null)
                m_Faction.State.FactionItems.Add(this);
        }

        public void Detach()
        {
            if (m_Item is IFactionItem)
                ((IFactionItem)m_Item).FactionItemState = null;

            if (m_Faction != null && m_Faction.State.FactionItems.Contains(this))
                m_Faction.State.FactionItems.Remove(this);
        }

        public FactionItem(Item item, Faction faction)
        {
            m_Item = item;
            m_Faction = faction;
        }

        public FactionItem(GenericReader reader, Faction faction)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        m_Item = reader.ReadItem();
                        m_Expiration = reader.ReadDateTime();
                        break;
                    }
            }

            m_Faction = faction;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt((int)0);

            writer.Write((Item)m_Item);
            writer.Write((DateTime)m_Expiration);
        }

        public static int GetMaxWearables(Mobile mob)
        {
            PlayerState pl = PlayerState.Find(mob);

            if (pl == null)
                return 0;

            if (pl.Faction.IsCommander(mob))
                return 9;

            return pl.Rank.MaxWearables;
        }

        public static FactionItem Find(Item item)
        {
            if (item is IFactionItem)
            {
                FactionItem state = ((IFactionItem)item).FactionItemState;

                if (state != null && state.HasExpired)
                {
                    state.Detach();
                    state = null;
                }

                return state;
            }

            return null;
        }

        public static Item Imbue(Item item, Faction faction, bool expire, int hue)
        {
            if (!(item is IFactionItem))
                return item;

            FactionItem state = Find(item);

            if (state == null)
            {
                state = new FactionItem(item, faction);
                state.Attach();
            }

            if (expire)
                state.StartExpiration();

            item.Hue = hue;
            return item;
        }
    }
}
