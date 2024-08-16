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

/* Scripts/Engines/ResourcePool/ResourcePool.cs
 * ChangeLog
 *  06/02/05 TK
 *		Fixed having Leather types in RDRedirects instead of Hides
 *  04/02/05 TK
 *		Added special leather and cloth redirect types
 *	03/02/05 Taran Kain
 *		Created
 */

using Server.Items;

namespace Server.Engines.ResourcePool
{
    [PropertyObject]
    public class ResourceDataList
    {
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Arrows { get { return (ResourceData)ResourcePool.Resources[typeof(Arrow)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Boards { get { return (ResourceData)ResourcePool.Resources[typeof(Board)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Bolts { get { return (ResourceData)ResourcePool.Resources[typeof(Bolt)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Feathers { get { return (ResourceData)ResourcePool.Resources[typeof(Feather)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Shafts { get { return (ResourceData)ResourcePool.Resources[typeof(Shaft)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Iron { get { return (ResourceData)ResourcePool.Resources[typeof(IronIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData DullCopper { get { return (ResourceData)ResourcePool.Resources[typeof(DullCopperIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData ShadowIron { get { return (ResourceData)ResourcePool.Resources[typeof(ShadowIronIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Copper { get { return (ResourceData)ResourcePool.Resources[typeof(CopperIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Bronze { get { return (ResourceData)ResourcePool.Resources[typeof(BronzeIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Gold { get { return (ResourceData)ResourcePool.Resources[typeof(GoldIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Agapite { get { return (ResourceData)ResourcePool.Resources[typeof(AgapiteIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Verite { get { return (ResourceData)ResourcePool.Resources[typeof(VeriteIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Valorite { get { return (ResourceData)ResourcePool.Resources[typeof(ValoriteIngot)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Cloth { get { return (ResourceData)ResourcePool.Resources[typeof(Cloth)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Leather { get { return (ResourceData)ResourcePool.Resources[typeof(Leather)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData HornedLeather { get { return (ResourceData)ResourcePool.Resources[typeof(HornedLeather)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData SpinedLeather { get { return (ResourceData)ResourcePool.Resources[typeof(SpinedLeather)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData BarbedLeather { get { return (ResourceData)ResourcePool.Resources[typeof(BarbedLeather)]; } set { } }

        public ResourceDataList()
        {
        }
    }

    [PropertyObject]
    public class RDRedirectList
    {
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect Logs { get { return (RDRedirect)ResourcePool.Resources[typeof(Log)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect BoltOfCloth { get { return (RDRedirect)ResourcePool.Resources[typeof(BoltOfCloth)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect UncutCloth { get { return (RDRedirect)ResourcePool.Resources[typeof(UncutCloth)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect Hides { get { return (RDRedirect)ResourcePool.Resources[typeof(Hides)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect IronOre { get { return (RDRedirect)ResourcePool.Resources[typeof(IronOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect DullCopperOre { get { return (RDRedirect)ResourcePool.Resources[typeof(DullCopperOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect ShadowIronOre { get { return (RDRedirect)ResourcePool.Resources[typeof(ShadowIronOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect CopperOre { get { return (RDRedirect)ResourcePool.Resources[typeof(CopperOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect BronzeOre { get { return (RDRedirect)ResourcePool.Resources[typeof(BronzeOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect GoldOre { get { return (RDRedirect)ResourcePool.Resources[typeof(GoldOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect AgapiteOre { get { return (RDRedirect)ResourcePool.Resources[typeof(AgapiteOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect VeriteOre { get { return (RDRedirect)ResourcePool.Resources[typeof(VeriteOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect ValoriteOre { get { return (RDRedirect)ResourcePool.Resources[typeof(ValoriteOre)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect Cotton { get { return (RDRedirect)ResourcePool.Resources[typeof(Cotton)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect SpoolThread { get { return (RDRedirect)ResourcePool.Resources[typeof(SpoolOfThread)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect Wool { get { return (RDRedirect)ResourcePool.Resources[typeof(Wool)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect DarkYarn { get { return (RDRedirect)ResourcePool.Resources[typeof(DarkYarn)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect LightYarn { get { return (RDRedirect)ResourcePool.Resources[typeof(LightYarn)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect LightYarnUnraveled { get { return (RDRedirect)ResourcePool.Resources[typeof(LightYarnUnraveled)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect SpinedHides { get { return (RDRedirect)ResourcePool.Resources[typeof(SpinedHides)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect HornedHides { get { return (RDRedirect)ResourcePool.Resources[typeof(HornedHides)]; } set { } }
        [CommandProperty(AccessLevel.Counselor)]
        public static RDRedirect BarbedHides { get { return (RDRedirect)ResourcePool.Resources[typeof(BarbedHides)]; } set { } }

        public RDRedirectList()
        {
        }
    }
}
