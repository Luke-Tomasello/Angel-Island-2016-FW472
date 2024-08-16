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

/* Scripts\Engines\ChampionSpawn\Champs\BaseChampion.cs
 * ChangeLog:
 *	3/15/08, Adam
 *		Move DistributedLoot() to Scripts/Misc/ChampLoot.cs to be used as a shared facility.
 *			i.e., new ChampLootPack(this).DistributedLoot()
 *  3/19/07, Adam
 *      Pacakge up loot generation and move it into BaseCreature.cs
 *      We want to be able to designate any creature as a champ via our Mobile Factory
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  1/11/07, Adam
 *      Check the the skull type is not None before dropping it.
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  8/16/05, Rhiannon
 *		Added constructors to allow champions to have individual min and max speeds.
 *	12/6/05, Adam
 *		Cleanup:
 *			1. get rid of funky OnDeath() override
 *			2. Check OnDeath() for creature: (this is AdamsCat || this is CraZyLucY)
 *				don't drop skull
 *	12/23/04, Adam
 *		Remove the check to see if we're in felucca before dropping champ skull.
 *		Search string: c.DropItem( new ChampionSkull( SkullType ) );
 *	12/15/04, Adam
 *		While we're in Shame III, we want to hard code the gold drop location - OnBeforeDeath()
 *			to return to normal operation, see the comments in OnBeforeDeath()
 *	10/17/04, Adam
 *		Increase gold drop from: Gold( 400, 600 )
 *			to: Gold( 800, 1200 )
 *		Old drop was like ~10K, now it will be ~30K
 *		(25 piles * 1200 = 30K gold)
 *	7/1/04, Adam
 *		Adam's Cat uses base champ code, but doesn't want the ChampionSkull drop
 * 		Add new OnDeath() method that skips the skull drop
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Removed Justice rewards now that virtues are disabled.
 *		Modified the amount of items given. Modified weapon rewards to also reward with armor.
 *	3/23/04 code changes by mith:
 *		OnBeforeDeath() - replaced GivePowerScrolls with GiveMagicItems
 *		GiveMagicItems() - new function to award players with magic items upon death of champion
 *		CreateWeapon()/CreateArmor() - called by GiveMagicItems to create random item to be awarded to player
 *	3/17/04 code changes by mith:
 *		OnBeforeDeath() - Decreased radius of gold drop
 *		GoodiesTimer.OnClick() - Decreased amount of random gold dropped
 */

using Server.Engines.ChampionSpawn;
using Server.Items;

namespace Server.Mobiles
{
    public abstract class BaseChampion : BaseCreature
    {
        public BaseChampion(AIType aiType)
            : this(aiType, FightMode.All | FightMode.Closest)
        {
        }

        public BaseChampion(AIType aiType, FightMode mode)
            : base(aiType, mode, 18, 1, 0.1, 0.2)
        {
        }

        public BaseChampion(AIType aiType, double dActiveSpeed, double dPassiveSpeed)
            : base(aiType, FightMode.All | FightMode.Closest, 18, 1, dActiveSpeed, dPassiveSpeed)
        {
        }

        public BaseChampion(AIType aiType, FightMode mode, double dActiveSpeed, double dPassiveSpeed)
            : base(aiType, mode, 18, 1, dActiveSpeed, dPassiveSpeed)
        {
        }

        public BaseChampion(Serial serial)
            : base(serial)
        {
        }

        public abstract ChampionSkullType SkullType { get; }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override bool OnBeforeDeath()
        {
            return base.OnBeforeDeath();
        }

        public override void OnDeath(Container c)
        {
            // adam's cat, crazy lucy, and Azothu don't have skulls
            if (SkullType != ChampionSkullType.None)
                c.DropItem(new ChampionSkull(SkullType));

            base.OnDeath(c);
        }

        public override void DistributedLoot()
        {
            // Adam: While we're in Shame III, we want to hard code the gold drop location.
            //	To return to normal operation, delete the next two lines + the if() block
            int X = base.X;
            int Y = base.Y;
            if (this.Map != null)
            {
                Region reg = this.Region;

                if (reg != this.Map.DefaultRegion)
                {
                    if (reg.Name == "Shame")
                    {
                        X = 5609;
                        Y = 193;
                    }
                }
            }

            // distribute loot
            new ChampLootPack(this, X, Y).DistributedLoot();
        }
    }
}
