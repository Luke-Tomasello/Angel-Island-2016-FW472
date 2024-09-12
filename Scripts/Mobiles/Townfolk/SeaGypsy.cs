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

/* Scripts/Mobiles/Townfolk/SeaGypsy.cs
 * ChangeLog
 *	8/26/10, Adam
 *		first time checkin of the seagoing escort
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class SeaGypsy : BaseEscortable
    {
        public override bool GateTravel { get { return false; } }   // you must take me via boat

        private static string[] m_Docks = new string[]
            {
                new Point3D(2736, 2166, 0).ToString(),		// "Buccaneers Den"
				new Point3D(1492, 3696, -3).ToString(),		// "Jhelom"
				new Point3D(3675, 2259, 20).ToString(),		// "Magincia"
				new Point3D(4406, 1045, -2).ToString(),		// "Moonglow"
				new Point3D(3803, 1279, 5).ToString(),		// "Nujel'm"
				new Point3D(3650, 2653, 0).ToString(),		// "Ocllo"
				new Point3D(716, 2233, -3).ToString(),		// "Skara Brae" East
				new Point3D(639, 2236, -3).ToString(),		// "Skara Brae" West
				new Point3D(2071, 2855, -3).ToString(),		// "Trinsic"
				new Point3D(3013, 828, -3).ToString(),		// "Vesper"
			};

        public override string[] GetPossibleDestinations()
        {
            return m_Docks;
        }

        [Constructable]
        public SeaGypsy()
        {
            Title = "the sea gypsy";
        }

        // public override bool ClickTitle{ get{ return false; } } // Do not display 'the seeker of adventure' when single-clicking
        public override void InitBody()
        {
            SetStr(90, 100);
            SetDex(90, 100);
            SetInt(15, 25);

            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            WipeLayers();
            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new SkullCap(Utility.RandomNeutralHue()));

            switch (Utility.Random(2))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new LongHair(Utility.RandomHairHue())); break;
            }

            PackGold(26);
        }

        public override void ProvideLoot(Mobile escorter)
        {
            if (escorter == null)
                return;

            //fame is based on whether it is a safe location or not 
            Region dest = GetDestination();
            if (dest != null && IsSafe(dest))
                Misc.Titles.AwardFame(escorter, 10, true);
            else
                Misc.Titles.AwardFame(escorter, 20, true);

            // create a fishing map
            Item item = GenMap();

            GiveLoot(item);
        }

        public static SeaChart GenMap()
        {
            int chance = Utility.Random(100);
            if (chance >= 0 && chance <= 1)         // 2 in 100
                return new SeaChart(5);
            else if (chance >= 2 && chance <= 5)    // 4 in 100
                return new SeaChart(4);
            else if (chance >= 6 && chance <= 13)   // 8 in 100
                return new SeaChart(3);
            else if (chance >= 14 && chance <= 30)  // 16 in 100
                return new SeaChart(2);
            else
                return new SeaChart(1);
        }

        public override void LeadOnSpeak(string name)
        {
            Say("Let's go. I have given you a map to the docks in {0}.", name);
        }

        public override bool AcceptEscorter(Mobile m)
        {
            if (base.AcceptEscorter(m) == true)
            {   // hand over the map to the dock
                SeaChart docks = new SeaChart(4, new Point2D(m_point)); // create a map to the docks.
                docks.Level = 0;                                        // kill the level so that there are no rewards.
                docks.Pins.Clear();                                     // we will add our own pins
                int mapX, mapY;
                docks.ConvertToMap(m_point.X, m_point.Y, out mapX, out mapY);
                docks.Pins.Add(new Point2D(mapX, mapY));
                docks.Name = string.Format("{0} docks", Destination);
                base.GiveLoot(docks, false);
                SeaGypsy.UsageReport(m, string.Format("is taking an escort to {0}", docks.Name));
                return true;
            }

            return false;
        }

        // no delay on sea gypsies
        protected override TimeSpan EscortDelay { get { return TimeSpan.FromMinutes(0.0); } }

        public override void ArrivedSpeak(string name)
        {
            // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
            //Say(1042809, name);

            Say("We have arrived! I thank thee!");
            Say("I'm sorry, but I all I have is this old map to a secret fishing spot.");
            Say("I hope that will be enough.");
        }


        private Point3D m_point;
        public override Region PickRandomDestination()
        {
            return PickRandomDestination(out m_point);
        }

        public override bool There(Point3D p)
        {
            return
                base.There(p) &&                                // we are in the town
                Multis.BaseBoat.FindBoatAt(this) == null && // and no longer on the boat
                this.GetDistanceToSqrt(m_point) <= 100.0 &&     // and we are within 100 tiles of the point on the docks
                true;
        }

        public override void GenerateLoot()
        {
            if (Core.UOAI || Core.UOREN)
            {
                switch (Utility.Random(4))
                {
                    case 0: PackItem(new Drums()); break;
                    case 1: PackItem(new Harp()); break;
                    case 2: PackItem(new Lute()); break;
                    case 3: PackItem(new Tambourine()); break;
                }
            }
            else
            {
                if (Core.UOSP || Core.UOMO)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        public static void UsageReport(Mobile m, string text)
        {
            // Tell staff that an a player is using this system
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SeaGypsyUsageReport))
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator,
                0x482,
                String.Format("{0}, {1} ", m.Name, text));
        }

        public SeaGypsy(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_point);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_point = reader.ReadPoint3D();
                    goto default;

                default:
                    break;
            }
        }
    }
}
