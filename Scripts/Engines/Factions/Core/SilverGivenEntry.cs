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
    public class SilverGivenEntry
    {
        public static readonly TimeSpan ExpirePeriod = TimeSpan.FromHours(3.0);

        private Mobile m_GivenTo;
        private DateTime m_TimeOfGift;

        public Mobile GivenTo { get { return m_GivenTo; } }
        public DateTime TimeOfGift { get { return m_TimeOfGift; } }

        public bool IsExpired { get { return (m_TimeOfGift + ExpirePeriod) < DateTime.Now; } }

        public SilverGivenEntry(Mobile givenTo)
        {
            m_GivenTo = givenTo;
            m_TimeOfGift = DateTime.Now;
        }
    }
}
