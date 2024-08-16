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
using System.Collections;

namespace Server.Mobiles
{
    public interface IMount
    {
        Mobile Rider { get; set; }
    }

    public interface IMountItem
    {
        IMount Mount { get; }
    }
}

namespace Server
{
    public interface IVendor
    {
        bool OnBuyItems(Mobile from, ArrayList list);
        bool OnSellItems(Mobile from, ArrayList list);

        DateTime LastRestock { get; set; }
        TimeSpan RestockDelay { get; }
        void Restock();
    }

    public interface IPoint2D
    {
        int X { get; }
        int Y { get; }
    }

    public interface IPoint3D : IPoint2D
    {
        int Z { get; }
    }

    public interface ICarvable
    {
        void Carve(Mobile from, Item item);
    }

    public interface IWeapon
    {
        int MaxRange { get; }
        TimeSpan OnSwing(Mobile attacker, Mobile defender);
        void GetStatusDamage(Mobile from, out int min, out int max);
    }

    public interface IHued
    {
        int HuedItemID { get; }
    }

    public interface ISpell
    {
        bool IsCasting { get; }
        void OnCasterHurt();
        void OnCasterKilled();
        void OnConnectionChanged();
        bool OnCasterMoving(Direction d);
        bool OnCasterEquiping(Item item);
        bool OnCasterUsingObject(object o);
        bool OnCastInTown(Region r);
    }

    public interface IParty
    {
        void OnStamChanged(Mobile m);
        void OnManaChanged(Mobile m);
        void OnStatsQuery(Mobile beholder, Mobile beheld);
    }
}
