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

namespace Server.Ethics.Hero
{
    public sealed class HeroEthic : Ethic
    {
        public HeroEthic()
        {
            m_Definition = new EthicDefinition(
                    0x482,
                    "Hero", "(Hero)",
                    "I will defend the virtues",
                    new Power[]
                    {
                        new HolySense(),
                        new HolyItem(),
                        new SummonFamiliar(),
                        new HolyBlade(),
                        new Bless(),
                        new HolyShield(),
                        new HolySteed(),
                        new HolyWord()
                    }
                );
        }

        public override bool IsEligible(Mobile mob)
        {
            // don't put murderer checks and such here because the RunUO impl calls this in IsEnemy and if this check fails
            //	you will be removed from the Ethic which is NOT what we want.

            if (Core.NewEthics)
            {   // must be part of a faction
                Faction fac = Faction.Find(mob);
                return (fac is TrueBritannians || fac is CouncilOfMages);
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
