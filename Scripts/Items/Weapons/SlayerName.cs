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

/* Scripts\Items\Weapons\SlayerName.cs
 * CHANGELOG
 * 2010.05.22 - Pix
 *      Added utility class SlayerLabel for 'naming' the slayer enums.
 */

namespace Server.Items
{
    public enum SlayerName
    {
        None,
        Silver,
        OrcSlaying,
        TrollSlaughter,
        OgreTrashing,
        Repond,
        DragonSlaying,
        Terathan,
        SnakesBane,
        LizardmanSlaughter,
        ReptilianDeath,
        DaemonDismissal,
        GargoylesFoe,
        BalronDamnation,
        Exorcism,
        Ophidian,
        SpidersDeath,
        ScorpionsBane,
        ArachnidDoom,
        FlameDousing,
        WaterDissipation,
        Vacuum,
        ElementalHealth,
        EarthShatter,
        BloodDrinking,
        SummerWind,
        ElementalBan // Bane?
    }

    public class SlayerLabel
    {
        public static string GetSlayerLabel(SlayerName name)
        {
            switch (name)
            {
                case SlayerName.None:
                    return "";
                    break;
                case SlayerName.Silver:
                    return "Silver";
                    break;
                case SlayerName.OrcSlaying:
                    return "Orc Slaying";
                    break;
                case SlayerName.TrollSlaughter:
                    return "Troll Slaughter";
                    break;
                case SlayerName.OgreTrashing:
                    return "Ogre Thrashing";
                    break;
                case SlayerName.Repond:
                    return "Repond";
                    break;
                case SlayerName.DragonSlaying:
                    return "Dragon Slaying";
                    break;
                case SlayerName.Terathan:
                    return "Terathan";
                    break;
                case SlayerName.SnakesBane:
                    return "Snakes Bane";
                    break;
                case SlayerName.LizardmanSlaughter:
                    return "Lizardman Slaughter";
                    break;
                case SlayerName.ReptilianDeath:
                    return "Reptillian Death";
                    break;
                case SlayerName.DaemonDismissal:
                    return "Daemon Dismissal";
                    break;
                case SlayerName.GargoylesFoe:
                    return "Gargoles' Foe";
                    break;
                case SlayerName.BalronDamnation:
                    return "Balron Damnation";
                    break;
                case SlayerName.Exorcism:
                    return "Exorcism";
                    break;
                case SlayerName.Ophidian:
                    return "Ophidian";
                    break;
                case SlayerName.SpidersDeath:
                    return "Spiders' Death";
                    break;
                case SlayerName.ScorpionsBane:
                    return "Scorpions' Bane";
                    break;
                case SlayerName.ArachnidDoom:
                    return "Arachnid Doom";
                    break;
                case SlayerName.FlameDousing:
                    return "Flame Dousing";
                    break;
                case SlayerName.WaterDissipation:
                    return "Water Dissipation";
                    break;
                case SlayerName.Vacuum:
                    return "Vacuum";
                    break;
                case SlayerName.ElementalHealth:
                    return "Elemental Health";
                    break;
                case SlayerName.EarthShatter:
                    return "Earth Shatter";
                    break;
                case SlayerName.BloodDrinking:
                    return "Blood Drinking";
                    break;
                case SlayerName.SummerWind:
                    return "Summer Wind";
                    break;
                case SlayerName.ElementalBan:
                    return "Elemental Bane";
                    break;
                default:
                    return "unknown slaying";
                    break;
            }

        }
    }
}
