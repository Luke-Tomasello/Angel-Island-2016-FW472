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

using Server.Factions;
using System;

namespace Server.Ethics.Evil
{
    public sealed class EvilEthic : Ethic
    {
        public EvilEthic()
        {
            m_Definition = new EthicDefinition(
                    0x455,
                    "Evil", "(Evil)",
                    "I am evil incarnate",
                    new Power[]
                    {
                        new UnholySense(),
                        new UnholyItem(),
                        new SummonFamiliar(),
                        new VileBlade(),
                        new Blight(),
                        new UnholyShield(),
                        new UnholySteed(),
                        new UnholyWord()
                    }
                );
        }

        public override bool IsEligible(Mobile mob)
        {
            if (Core.NewEthics)
            {   // must be part of a faction
                Faction fac = Faction.Find(mob);
                return (fac is Minax || fac is Shadowlords);
            }
            else
            {
                if ((mob.CreationTime + TimeSpan.FromHours(24)) > DateTime.Now)
                    return false;

                return true;
            }
        }
    }
}
