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

using Server.Regions;

namespace Server.Factions
{
    public class StrongholdRegion : BaseRegion
    {
        private Faction m_Faction;

        public Faction Faction
        {
            get { return m_Faction; }
            set { m_Faction = value; }
        }

        public StrongholdRegion(Faction faction)
            : base(faction.Definition.FriendlyName, Faction.Facet, Region.DefaultPriority, faction.Definition.Stronghold.Area)
        {
            m_Faction = faction;

            Register();
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (!base.OnMoveInto(m, d, newLocation, oldLocation))
                return false;

            if (m.AccessLevel >= AccessLevel.Counselor || Contains(oldLocation))
                return true;

            return (Faction.Find(m, true, true) != null);
        }

        public override bool AllowHousing(/*Mobile from,*/ Point3D p)
        {
            return false;
        }
    }
}
