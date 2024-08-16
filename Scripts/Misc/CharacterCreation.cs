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

/* Scripts/Misc/CharacterCreation.cs
 * ChangeLog
 *  8/10/2024, Adam
 *      1. first 'admin account' created is made is Owner
 *          We need to set AccessLevelInternal to achieve this
 *      2. Add management console for admins
 *	3/6/11, Adam
 *		Add faction silver
 *	2/3/11, Adam
 *		Make characters for Core.UOIS mortal
 *	4/4/08, Adam
 *		Make several static functions public so we can build 'potion bags' for the potion stone
 *	2/26/08, Adam
 *		Remove setting of stat and skill caps from FillBankBox
 *  1/4/08, Adam
 *      Remove some old commented code (New Guild stuff)
 *	12/11/07, Pix
 *		Changed the new new char spawn spot to 5731, 953, 0 in Trammel.
 *	10/8/07, Adam
 *		- Return to spawning new players at WBB due to our very low pop.
 *		- We need to get pixie's new player starting area deco'ed before we can use it.
 *	8/26/07 - Pix
 *		Added new player starting area (dependent on NewPlayerStartingArea feature bit)
 *	8/20/06, Pix
 *		Re-enabled town selection on character creation.
 *		Left random spawn point if Britain is selected.
 *		Removed Oc'Nivelle rune.
 *	07/22/06, weaver
 *		Added newbie (regular) tent bag on character creation.
 *		Reformatted invalidly formatted changelog entries.
 *	05/30/06, Adam
 *		no more blessed reagents
 *	04/24/06, Kit
 *		Added random new character position spawn logic.
 *	09/14/05 Taran Kain
 *		Uncommented call to FillBankbox, added check for TC functionality
 *	9/12/05 - Pix
 *		Safeguarded against Paladin/Necro/etc creation.
 *	6/22/04, Old Salty
 * 		Added starting loot of TinkerTools for tinkers.
 *	6/19/04, Adam
 *		1. add the starting gold of 204 Pieces
 *		2. Add Bedroll for campers
 *		3. Add a SpyGlass to new players starting loot. 
 *		4. Comment out calls to FillBankbox
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/23/04, Pixie
 *		Switched it so all characters start at West Brit Bank.
 *	5/20/04, pixie
 *		Switched it so when user can choose which city to start in
 *		(assuming he/she chooses the "Advanced" character instead of smith/warrior/mage)
 *	5/3/04, mith
 *		Added rune to Oc'Nivelle in the AddBackpack() method.
 *		Added 60,000 of each leather to bankbox.
 *	4/13/04, mith
 *		Streamlined the creation of bankbox items. Fixed bug where they weren't newbied.
 *	3/30/04
 *		commented out lines 143 and 269 to remove fletcher tools
 *	3/28/04, Sambo
 *		added lines 55-342 added various items
 */

using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Misc
{
    public class CharacterCreation
    {
        public static void Initialize()
        {
            // Register our event handler
            EventSink.CharacterCreated += new CharacterCreatedEventHandler(EventSink_CharacterCreated);
        }

        private static void AddBackpack(Mobile m)
        {
            Container pack = m.Backpack;

            if (pack == null)
            {
                pack = new Backpack();
                pack.Movable = false;

                m.AddItem(pack);
            }

            if (Core.UOAI || Core.UOAR)
            {
                PackItem(new RedBook("a book", m.Name, 20, true));
                PackItem(new Gold(204));    // Adam: add the magic starting gold of 204
                PackItem(new Dagger());
                PackItem(new Candle());
                PackItem(new Spyglass());   // Adam: The spyglass is how you opperate a moongate. seems like a reasonable starting item

                // [NEW] Tower
                //RecallRune ocRune = new RecallRune();
                //ocRune.Target =	new Point3D( 1688, 1422, 0 ); 
                //ocRune.TargetMap = Map.Felucca;
                //ocRune.Description = "[NEW] Tower";
                //ocRune.Marked = true;
                //PackItem( ocRune );

                // add a book describing new
                //PackItem(NewBook());

                // wea: added newbie tent
                TentBag newbtent = new TentBag();
                PackItem(newbtent);
            }
            else if (Core.UOSP || Core.UOMO)
            {
                //daggers are newbie, everything else isn't
                Dagger dagger = new Dagger();
                PackItem(dagger);
                dagger.LootType = LootType.Newbied;

                PackItem(new Gold(100));
                PackItem(new IronIngot(25));
                PackItem(new Shovel());

                BagOfReagents regs = new BagOfReagents(10);
                PackItem(regs);

                //ADD back of 10 reagents
                PackItem(new Katana());
            }
        }

        public static Item MakeNewbie(Item item) //started editing here
        {
            item.LootType = LootType.Newbied;

            return item;
        }

        public static void PlaceItemIn(Container parent, int x, int y, Item item)
        {
            parent.AddItem(item);
            item.Location = new Point3D(x, y, 0);
        }

        public static Item MakePotionKeg(PotionEffect type, int hue)
        {
            PotionKeg keg = new PotionKeg();

            keg.Held = 100;
            keg.Type = type;
            keg.Hue = hue;

            return MakeNewbie(keg);
        }

        // adam: not called for production servers
        private static void FillBankbox(Mobile m)
        {
            BankBox bank = m.BankBox;

            if (bank == null)
                return;

            Container cont;

            // Begin box of money
            cont = new WoodenBox();
            cont.ItemID = 0xE7D;
            cont.Hue = 0x489;

            PlaceItemIn(cont, 16, 51, new BankCheck(1000000)); //edited by sam
            PlaceItemIn(cont, 28, 51, new BankCheck(250000)); //edited by sam
            PlaceItemIn(cont, 40, 51, new BankCheck(125000)); //edited by sam
            PlaceItemIn(cont, 52, 51, new BankCheck(75000)); //edited by sam
            PlaceItemIn(cont, 64, 51, new BankCheck(32500)); //edited by sam

            if (Core.Factions)
                PlaceItemIn(cont, 16, 115, new Factions.Silver(9000));

            PlaceItemIn(cont, 34, 115, MakeNewbie(new Gold(60000)));

            PlaceItemIn(bank, 18, 169, cont);
            // End box of money


            // Begin bag of potion kegs
            cont = new Backpack();
            cont.Name = "Various Potion Kegs";

            PlaceItemIn(cont, 45, 149, MakePotionKeg(PotionEffect.CureGreater, 0x2D));
            PlaceItemIn(cont, 69, 149, MakePotionKeg(PotionEffect.HealGreater, 0x499));
            PlaceItemIn(cont, 93, 149, MakePotionKeg(PotionEffect.PoisonDeadly, 0x46));
            PlaceItemIn(cont, 117, 149, MakePotionKeg(PotionEffect.RefreshTotal, 0x21));
            PlaceItemIn(cont, 141, 149, MakePotionKeg(PotionEffect.ExplosionGreater, 0x74));

            PlaceItemIn(cont, 93, 82, MakeNewbie(new Bottle(1000)));

            PlaceItemIn(bank, 53, 169, cont);
            // End bag of potion kegs


            // Begin bag of tools
            cont = new Bag();
            cont.Name = "Tool Bag";

            PlaceItemIn(cont, 30, 35, MakeNewbie(new TinkerTools(60000)));
            PlaceItemIn(cont, 90, 35, MakeNewbie(new DovetailSaw(60000)));
            PlaceItemIn(cont, 30, 68, MakeNewbie(new Scissors()));
            PlaceItemIn(cont, 45, 68, MakeNewbie(new MortarPestle(60000)));
            PlaceItemIn(cont, 75, 68, MakeNewbie(new ScribesPen(60000)));
            PlaceItemIn(cont, 90, 68, MakeNewbie(new SmithHammer(60000)));
            PlaceItemIn(cont, 30, 118, MakeNewbie(new TwoHandedAxe()));
            PlaceItemIn(cont, 90, 118, MakeNewbie(new SewingKit(60000)));

            PlaceItemIn(bank, 118, 169, cont);
            // End bag of tools


            // Begin bag of archery ammo
            cont = new Bag();
            cont.Name = "Bag Of Archery Ammo";

            PlaceItemIn(cont, 48, 76, MakeNewbie(new Arrow(60000)));
            PlaceItemIn(cont, 72, 76, MakeNewbie(new Bolt(60000)));

            PlaceItemIn(bank, 118, 124, cont);
            // End bag of archery ammo


            // Begin bag of treasure maps
            cont = new Bag();
            cont.Name = "Bag Of Treasure Maps";

            PlaceItemIn(cont, 30, 35, MakeNewbie(new TreasureMap(1, Map.Felucca)));
            PlaceItemIn(cont, 45, 35, MakeNewbie(new TreasureMap(2, Map.Felucca)));
            PlaceItemIn(cont, 60, 35, MakeNewbie(new TreasureMap(3, Map.Felucca)));
            PlaceItemIn(cont, 75, 35, MakeNewbie(new TreasureMap(4, Map.Felucca)));
            PlaceItemIn(cont, 90, 35, MakeNewbie(new TreasureMap(5, Map.Felucca)));

            PlaceItemIn(cont, 30, 50, MakeNewbie(new TreasureMap(1, Map.Felucca)));
            PlaceItemIn(cont, 45, 50, MakeNewbie(new TreasureMap(2, Map.Felucca)));
            PlaceItemIn(cont, 60, 50, MakeNewbie(new TreasureMap(3, Map.Felucca)));
            PlaceItemIn(cont, 75, 50, MakeNewbie(new TreasureMap(4, Map.Felucca)));
            PlaceItemIn(cont, 90, 50, MakeNewbie(new TreasureMap(5, Map.Felucca)));

            PlaceItemIn(cont, 55, 100, MakeNewbie(new Lockpick(60000)));
            PlaceItemIn(cont, 60, 100, MakeNewbie(new Pickaxe()));

            PlaceItemIn(bank, 98, 124, cont);
            // End bag of treasure maps


            // Begin bag of raw materials
            cont = new Bag();
            cont.Hue = 0x835;
            cont.Name = "Raw Materials Bag";

            PlaceItemIn(cont, 30, 35, MakeNewbie(new DullCopperIngot(60000)));
            PlaceItemIn(cont, 37, 35, MakeNewbie(new ShadowIronIngot(60000)));
            PlaceItemIn(cont, 44, 35, MakeNewbie(new CopperIngot(60000)));
            PlaceItemIn(cont, 51, 35, MakeNewbie(new BronzeIngot(60000)));
            PlaceItemIn(cont, 58, 35, MakeNewbie(new GoldIngot(60000)));
            PlaceItemIn(cont, 65, 35, MakeNewbie(new AgapiteIngot(60000)));
            PlaceItemIn(cont, 72, 35, MakeNewbie(new VeriteIngot(60000)));
            PlaceItemIn(cont, 79, 35, MakeNewbie(new ValoriteIngot(60000)));
            PlaceItemIn(cont, 86, 35, MakeNewbie(new IronIngot(60000)));

            PlaceItemIn(cont, 29, 55, MakeNewbie(new Leather(60000)));
            PlaceItemIn(cont, 44, 55, MakeNewbie(new SpinedLeather(60000)));
            PlaceItemIn(cont, 59, 55, MakeNewbie(new HornedLeather(60000)));
            PlaceItemIn(cont, 74, 55, MakeNewbie(new BarbedLeather(60000)));
            PlaceItemIn(cont, 35, 100, MakeNewbie(new Cloth(60000)));
            PlaceItemIn(cont, 67, 89, MakeNewbie(new Board(60000)));
            PlaceItemIn(cont, 88, 91, MakeNewbie(new BlankScroll(60000)));

            PlaceItemIn(bank, 98, 169, cont);
            // End bag of raw materials


            // Begin bag of spell casting stuff
            cont = new Backpack();
            cont.Hue = 0x480;
            cont.Name = "Spell Casting Stuff";

            PlaceItemIn(cont, 45, 105, new Spellbook(UInt64.MaxValue));

            Runebook runebook = new Runebook(10);
            runebook.CurCharges = runebook.MaxCharges;
            PlaceItemIn(cont, 105, 105, runebook);

            Item toHue = new BagOfReagents(65000);
            toHue.Hue = 0x2D;
            PlaceItemIn(cont, 45, 150, toHue);

            for (int i = 0; i < 9; ++i)
                PlaceItemIn(cont, 45 + (i * 10), 75, MakeNewbie(new RecallRune()));

            PlaceItemIn(bank, 78, 169, cont);
            // End bag of spell casting stuff
        }

        private static void AddShirt(Mobile m, int shirtHue)
        {
            int hue = Utility.ClipDyedHue(shirtHue & 0x3FFF);

            switch (Utility.Random(3))
            {
                case 0: EquipItem(new Shirt(hue), true); break;
                case 1: EquipItem(new FancyShirt(hue), true); break;
                case 2: EquipItem(new Doublet(hue), true); break;
            }
        }

        private static void AddPants(Mobile m, int pantsHue)
        {
            int hue = Utility.ClipDyedHue(pantsHue & 0x3FFF);

            if (m.Female)
            {
                switch (Utility.Random(2))
                {
                    case 0: EquipItem(new Skirt(hue), true); break;
                    case 1: EquipItem(new Kilt(hue), true); break;
                }
            }
            else
            {
                switch (Utility.Random(2))
                {
                    case 0: EquipItem(new LongPants(hue), true); break;
                    case 1: EquipItem(new ShortPants(hue), true); break;
                }
            }
        }

        private static void AddShoes(Mobile m)
        {
            EquipItem(new Shoes(Utility.RandomYellowHue()), true);
        }

        private static void AddHair(Mobile m, int itemID, int hue)
        {
            Item item;

            switch (itemID & 0x3FFF)
            {
                case 0x2044: item = new Mohawk(hue); break;
                case 0x2045: item = new PageboyHair(hue); break;
                case 0x2046: item = new BunsHair(hue); break;
                case 0x2047: item = new Afro(hue); break;
                case 0x2048: item = new ReceedingHair(hue); break;
                case 0x2049: item = new TwoPigTails(hue); break;
                case 0x204A: item = new KrisnaHair(hue); break;
                case 0x203B: item = new ShortHair(hue); break;
                case 0x203C: item = new LongHair(hue); break;
                case 0x203D: item = new PonyTail(hue); break;
                default: return;
            }

            m.AddItem(item);
        }

        private static void AddBeard(Mobile m, int itemID, int hue)
        {
            if (m.Female)
                return;

            Item item;

            switch (itemID & 0x3FFF)
            {
                case 0x203E: item = new LongBeard(hue); break;
                case 0x203F: item = new ShortBeard(hue); break;
                case 0x2040: item = new Goatee(hue); break;
                case 0x2041: item = new Mustache(hue); break;
                case 0x204B: item = new MediumShortBeard(hue); break;
                case 0x204C: item = new MediumLongBeard(hue); break;
                case 0x204D: item = new Vandyke(hue); break;
                default: return;
            }

            m.AddItem(item);
        }

        private static Mobile CreateMobile(Account a)
        {
            for (int i = 0; i < 5; ++i)
                if (a[i] == null)
                    return (a[i] = new PlayerMobile());

            return null;
        }

        private static void EventSink_CharacterCreated(CharacterCreatedEventArgs args)
        {
            Mobile newChar = CreateMobile(args.Account as Account);

            if (newChar == null)
            {
                Console.WriteLine("Login: {0}: Character creation failed, account full", args.State);
                return;
            }

            args.Mobile = newChar;
            m_Mobile = newChar;

            newChar.Player = true;
            newChar.AccessLevelInternal = ((Account)args.Account).AccessLevel;
            newChar.Female = args.Female;
            newChar.Body = newChar.Female ? 0x191 : 0x190;
            newChar.Hue = Utility.ClipSkinHue(args.Hue & 0x3FFF) | 0x8000;
            newChar.Hunger = 20;

            // Adam: Mortalis
            if (Core.UOMO && newChar is PlayerMobile && newChar.AccessLevel == AccessLevel.Player)
            {   // UO Mortalis is a dangerous place
                (newChar as PlayerMobile).Mortal = true;
                (newChar as PlayerMobile).Hidden = true;
            }

            if (newChar is PlayerMobile)
                ((PlayerMobile)newChar).Profession = args.Profession;

            //Pix: default to warrior if chosen is paladin, necro, etc.
            if (((PlayerMobile)newChar).Profession > 3)
                ((PlayerMobile)newChar).Profession = 1;

            SetName(newChar, args.Name);

            AddBackpack(newChar);

            SetStats(newChar, args.Str, args.Dex, args.Int);
            SetSkills(newChar, args.Skills, args.Profession);

            AddHair(newChar, args.HairID, Utility.ClipHairHue(args.HairHue & 0x3FFF));
            AddBeard(newChar, args.BeardID, Utility.ClipHairHue(args.BeardHue & 0x3FFF));

            // 8/10/2024, Adam: add management console for admins
            if (newChar.AccessLevel >= AccessLevel.Administrator)
            {
                PackItem(new CoreManagementConsole());
            }

            if (!Core.AOS || (args.Profession != 4 && args.Profession != 5))
            {
                AddShirt(newChar, args.ShirtHue);
                AddPants(newChar, args.PantsHue);
                AddShoes(newChar);
            }

            if (TestCenter.Enabled)
                FillBankbox(newChar);

            CityInfo city;

            if (Core.UOAI || Core.UOAR)
            {
                /*
				 * Our numbers have been so low lately (< 50), it's once again important
				 * to concentrate playerss so that they do not log into what seems to be an empty shard.
				 * We can stem the griefing by:
				 * 1. Changing the look of starting players (remove noob look)
				 * 2. Have a wider entry area
				 * 3. Have them 'Recall in' so they look like they've been here for a while
				 * 4. Give them a 1 minute 'young' status?
				 */

                //Comment out the following line to let the player choose where to start.
                Map spawnMap = Map.Felucca;

                //Comment out the following line to have them always start at Brit Inn
                //CityInfo city = new CityInfo( "Britain", "Sweet Dreams Inn", 1496, 1628, 10 );

                // this NewPlayerStartingArea probably needs to be based on IP address and account age etc.
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerStartingArea))
                {
                    city = new CityInfo("New Player Starting Area", "Starting Area", 5731, 953, 0);
                    spawnMap = Map.Trammel;
                }
                else
                {
                    //if( city.City == "Britain" )
                    {
                        Point3D p = NewCharacterSpawnLocation();
                        city = new CityInfo("Britain", "West Brit Bank", p.X, p.Y, p.Z);
                    }
                }

                newChar.MoveToWorld(city.Location, spawnMap);
            }
            else
            {
                city = args.City;
                Map spawnMap = Map.Felucca;
                newChar.MoveToWorld(city.Location, spawnMap);
            }

            Console.WriteLine("Login: {0}: New character being created (account={1})", args.State, ((Account)args.Account).Username);
            Console.WriteLine(" - Character: {0} (serial={1})", newChar.Name, newChar.Serial);
            Console.WriteLine(" - Started: {0} {1}", city.City, city.Location);

            new WelcomeTimer(newChar).Start();
        }

        private static Point3D NewCharacterSpawnLocation()
        {
            Map map = Map.Felucca;

            for (int i = 0; i < 20; ++i)
            {
                int x = Utility.RandomMinMax(1417, 1442);
                int y = Utility.RandomMinMax(1693, 1700);
                if (map.CanSpawnMobile(x, y, 0))
                {
                    return new Point3D(x, y, 0);
                }
                else
                {
                    int z = map.GetAverageZ(x, y);

                    if (map.CanSpawnMobile(x, y, z))
                    {
                        return new Point3D(x, y, z);
                    }
                }
            }
            //this shouldnt happen but if for some reason all spawn 20 random checks blocked, return default spawn location
            return new Point3D(1420, 1698, 0);
        }


        private static void FixStats(ref int str, ref int dex, ref int intel)
        {
            int vStr = str - 10;
            int vDex = dex - 10;
            int vInt = intel - 10;

            if (vStr < 0)
                vStr = 0;

            if (vDex < 0)
                vDex = 0;

            if (vInt < 0)
                vInt = 0;

            int total = vStr + vDex + vInt;

            if (total == 0 || total == 50)
                return;

            double scalar = 50 / (double)total;

            vStr = (int)(vStr * scalar);
            vDex = (int)(vDex * scalar);
            vInt = (int)(vInt * scalar);

            FixStat(ref vStr, (vStr + vDex + vInt) - 50);
            FixStat(ref vDex, (vStr + vDex + vInt) - 50);
            FixStat(ref vInt, (vStr + vDex + vInt) - 50);

            str = vStr + 10;
            dex = vDex + 10;
            intel = vInt + 10;
        }

        private static void FixStat(ref int stat, int diff)
        {
            stat += diff;

            if (stat < 0)
                stat = 0;
            else if (stat > 50)
                stat = 50;
        }

        private static void SetStats(Mobile m, int str, int dex, int intel)
        {
            FixStats(ref str, ref dex, ref intel);

            if (str < 10 || str > 60 || dex < 10 || dex > 60 || intel < 10 || intel > 60 || (str + dex + intel) != 80)
            {
                str = 10;
                dex = 10;
                intel = 10;
            }

            m.InitStats(str, dex, intel);
        }

        private static void SetName(Mobile m, string name)
        {
            name = name.Trim();

            if (!NameVerification.Validate(name, 2, 16, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                name = "Generic Player";

            m.Name = name;
        }

        private static bool ValidSkills(SkillNameValue[] skills)
        {
            int total = 0;

            for (int i = 0; i < skills.Length; ++i)
            {
                if (skills[i].Value < 0 || skills[i].Value > 50)
                    return false;

                total += skills[i].Value;

                for (int j = i + 1; j < skills.Length; ++j)
                {
                    if (skills[j].Value > 0 && skills[j].Name == skills[i].Name)
                        return false;
                }
            }

            return (total == 100);
        }

        private static Mobile m_Mobile;

        private static void SetSkills(Mobile m, SkillNameValue[] skills, int prof)
        {
            switch (prof)
            {
                case 1: // Warrior
                    {
                        skills = new SkillNameValue[]
                        {
                            new SkillNameValue( SkillName.Anatomy, 30 ),
                            new SkillNameValue( SkillName.Healing, 45 ),
                            new SkillNameValue( SkillName.Swords, 35 ),
                            new SkillNameValue( SkillName.Tactics, 50 )
                        };

                        break;
                    }
                case 2: // Magician
                    {
                        skills = new SkillNameValue[]
                        {
                            new SkillNameValue( SkillName.EvalInt, 30 ),
                            new SkillNameValue( SkillName.Wrestling, 30 ),
                            new SkillNameValue( SkillName.Magery, 50 ),
                            new SkillNameValue( SkillName.Meditation, 50 )
                        };

                        break;
                    }
                case 3: // Blacksmith
                    {
                        skills = new SkillNameValue[]
                        {
                            new SkillNameValue( SkillName.Mining, 30 ),
                            new SkillNameValue( SkillName.ArmsLore, 30 ),
                            new SkillNameValue( SkillName.Blacksmith, 50 ),
                            new SkillNameValue( SkillName.Tinkering, 50 )
                        };

                        break;
                    }
                //				case 4: // Necromancer
                //				{
                //					if ( !Core.AOS )
                //						goto default;
                //
                //					skills = new SkillNameValue[]
                //						{
                //							new SkillNameValue( SkillName.Necromancy, 50 ),
                //							new SkillNameValue( SkillName.Focus, 30 ),
                //							new SkillNameValue( SkillName.SpiritSpeak, 30 ),
                //							new SkillNameValue( SkillName.Swords, 30 ),
                //							new SkillNameValue( SkillName.Tactics, 20 )
                //						};
                //
                //					break;
                //				}
                //				case 5: // Paladin
                //				{
                //					if ( !Core.AOS )
                //						goto default;
                //
                //					skills = new SkillNameValue[]
                //						{
                //							new SkillNameValue( SkillName.Chivalry, 51 ),
                //							new SkillNameValue( SkillName.Swords, 49 ),
                //							new SkillNameValue( SkillName.Focus, 30 ),
                //							new SkillNameValue( SkillName.Tactics, 30 )
                //						};
                //
                //					break;
                //				}
                default:
                    {
                        if (!ValidSkills(skills))
                            return;

                        break;
                    }
            }

            bool addSkillItems = true;

            switch (prof)
            {
                case 1: // Warrior
                    {
                        EquipItem(new LeatherChest());
                        break;
                    }
                    //				case 4: // Necromancer
                    //				{
                    //					Container regs = new BagOfNecroReagents( 50 );
                    //
                    //					if ( !Core.AOS )
                    //					{
                    //						foreach ( Item item in regs.Items )
                    //							item.LootType = LootType.Newbied;
                    //					}
                    //
                    //					PackItem( regs );
                    //
                    //					regs.LootType = LootType.Regular;
                    //
                    //					EquipItem( new BoneHarvester() );
                    //					EquipItem( new BoneHelm() );
                    //
                    //					EquipItem( NecroHue( new LeatherChest() ) );
                    //					EquipItem( NecroHue( new LeatherArms() ) );
                    //					EquipItem( NecroHue( new LeatherGloves() ) );
                    //					EquipItem( NecroHue( new LeatherGorget() ) );
                    //					EquipItem( NecroHue( new LeatherLegs() ) );
                    //					EquipItem( NecroHue( new Skirt() ) );
                    //					EquipItem( new Sandals( 0x8FD ) );
                    //
                    //					Spellbook book = new NecromancerSpellbook( (ulong)0x8981 ); // animate dead, evil omen, pain spike, summon familiar, wraith form
                    //
                    //					PackItem( book );
                    //
                    //					book.LootType = LootType.Blessed;
                    //
                    //					addSkillItems = false;
                    //
                    //					break;
                    //				}
                    //				case 5: // Paladin
                    //				{
                    //					EquipItem( new Broadsword() );
                    //					EquipItem( new Helmet() );
                    //					EquipItem( new PlateGorget() );
                    //					EquipItem( new RingmailArms() );
                    //					EquipItem( new RingmailChest() );
                    //					EquipItem( new RingmailLegs() );
                    //					EquipItem( new ThighBoots( 0x748 ) );
                    //					EquipItem( new Cloak( 0xCF ) );
                    //					EquipItem( new BodySash( 0xCF ) );
                    //
                    //					Spellbook book = new BookOfChivalry( (ulong)0x3FF );
                    //
                    //					PackItem( book );
                    //
                    //					book.LootType = LootType.Blessed;
                    //
                    //					break;
                    //				}
            }

            for (int i = 0; i < skills.Length; ++i)
            {
                SkillNameValue snv = skills[i];

                if (snv.Value > 0 && snv.Name != SkillName.Stealth && snv.Name != SkillName.RemoveTrap)
                {
                    Skill skill = m.Skills[snv.Name];

                    if (skill != null)
                    {
                        skill.BaseFixedPoint = snv.Value * 10;

                        if (addSkillItems)
                            AddSkillItems(snv.Name);
                    }
                }
            }
        }

        private static void EquipItem(Item item)
        {
            EquipItem(item, false);
        }

        private static void EquipItem(Item item, bool mustEquip)
        {
            if (!Core.AOS)
                item.LootType = LootType.Newbied;

            if (m_Mobile != null && m_Mobile.EquipItem(item))
                return;

            Container pack = m_Mobile.Backpack;

            if (!mustEquip && pack != null)
                pack.DropItem(item);
            else
                item.Delete();
        }

        private static void PackItem(Item item)
        {
            if (!Core.AOS)
                item.LootType = LootType.Newbied;

            if (Core.UOSP || Core.UOMO)
            {
                item.LootType = LootType.Regular;
            }

            Container pack = m_Mobile.Backpack;

            if (pack != null)
                pack.DropItem(item);
            else
                item.Delete();
        }

        private static void PackInstrument()
        {
            switch (Utility.Random(6))
            {
                case 0: PackItem(new Drums()); break;
                case 1: PackItem(new Harp()); break;
                case 2: PackItem(new LapHarp()); break;
                case 3: PackItem(new Lute()); break;
                case 4: PackItem(new Tambourine()); break;
                case 5: PackItem(new TambourineTassel()); break;
            }
        }

        private static void PackScroll(int circle)
        {
            switch (Utility.Random(8) * (circle * 8))
            {
                case 0: PackItem(new ClumsyScroll()); break;
                case 1: PackItem(new CreateFoodScroll()); break;
                case 2: PackItem(new FeeblemindScroll()); break;
                case 3: PackItem(new HealScroll()); break;
                case 4: PackItem(new MagicArrowScroll()); break;
                case 5: PackItem(new NightSightScroll()); break;
                case 6: PackItem(new ReactiveArmorScroll()); break;
                case 7: PackItem(new WeakenScroll()); break;
                case 8: PackItem(new AgilityScroll()); break;
                case 9: PackItem(new CunningScroll()); break;
                case 10: PackItem(new CureScroll()); break;
                case 11: PackItem(new HarmScroll()); break;
                case 12: PackItem(new MagicTrapScroll()); break;
                case 13: PackItem(new MagicUnTrapScroll()); break;
                case 14: PackItem(new ProtectionScroll()); break;
                case 15: PackItem(new StrengthScroll()); break;
                case 16: PackItem(new BlessScroll()); break;
                case 17: PackItem(new FireballScroll()); break;
                case 18: PackItem(new MagicLockScroll()); break;
                case 19: PackItem(new PoisonScroll()); break;
                case 20: PackItem(new TelekinisisScroll()); break;
                case 21: PackItem(new TeleportScroll()); break;
                case 22: PackItem(new UnlockScroll()); break;
                case 23: PackItem(new WallOfStoneScroll()); break;
            }
        }

        private static Item NecroHue(Item item)
        {
            item.Hue = 0x2C3;

            return item;
        }

        private static void AddSkillItems(SkillName skill)
        {
            switch (skill)
            {
                case SkillName.Alchemy:
                    {
                        PackItem(new Bottle(4));
                        PackItem(new MortarPestle());
                        EquipItem(new Robe(Utility.RandomPinkHue()));
                        break;
                    }
                case SkillName.Anatomy:
                    {
                        PackItem(new Bandage(3));
                        EquipItem(new Robe(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.AnimalLore:
                    {
                        EquipItem(new ShepherdsCrook());
                        EquipItem(new Robe(Utility.RandomBlueHue()));
                        break;
                    }
                case SkillName.Archery:
                    {
                        PackItem(new Arrow(25));
                        EquipItem(new Bow());
                        break;
                    }
                case SkillName.ArmsLore:
                    {
                        switch (Utility.Random(3))
                        {
                            case 0: EquipItem(new Kryss()); break;
                            case 1: EquipItem(new Katana()); break;
                            case 2: EquipItem(new Club()); break;
                        }

                        break;
                    }
                case SkillName.Begging:
                    {
                        EquipItem(new GnarledStaff());
                        break;
                    }
                case SkillName.Blacksmith:
                    {
                        PackItem(new Tongs());
                        PackItem(new Pickaxe());
                        PackItem(new Pickaxe());
                        PackItem(new IronIngot(50));
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Fletching:
                    {
                        PackItem(new Board(14));
                        PackItem(new Feather(5));
                        PackItem(new Shaft(5));
                        break;
                    }
                case SkillName.Camping:
                    {
                        // Adam: Add the new Bedroll for campers
                        PackItem(new Bedroll(1));
                        PackItem(new Kindling(5));
                        break;
                    }
                case SkillName.Carpentry:
                    {
                        PackItem(new Board(10));
                        PackItem(new Saw());
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Cartography:
                    {
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new BlankMap());
                        PackItem(new Sextant());
                        break;
                    }
                case SkillName.Cooking:
                    {
                        PackItem(new Kindling(2));
                        PackItem(new RawLambLeg());
                        PackItem(new RawChickenLeg());
                        PackItem(new RawFishSteak());
                        PackItem(new SackFlour());
                        PackItem(new Pitcher(BeverageType.Water));
                        break;
                    }
                case SkillName.DetectHidden:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Discordance:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Fencing:
                    {
                        EquipItem(new Kryss());
                        break;
                    }
                case SkillName.Fishing:
                    {
                        EquipItem(new FishingPole());
                        EquipItem(new FloppyHat(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Healing:
                    {
                        PackItem(new Bandage(50));
                        PackItem(new Scissors());
                        break;
                    }
                case SkillName.Herding:
                    {
                        EquipItem(new ShepherdsCrook());
                        break;
                    }
                case SkillName.Hiding:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Inscribe:
                    {
                        PackItem(new BlankScroll(2));
                        PackItem(new BlueBook());
                        break;
                    }
                case SkillName.ItemID:
                    {
                        EquipItem(new GnarledStaff());
                        break;
                    }
                case SkillName.Lockpicking:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.Lumberjacking:
                    {
                        EquipItem(new Hatchet());
                        break;
                    }
                case SkillName.Macing:
                    {
                        EquipItem(new Club());
                        break;
                    }
                case SkillName.Magery:
                    {
                        BagOfReagents regs = new BagOfReagents(30);

                        // Adam: no more blessed reagents
                        //if ( !Core.AOS )
                        //{
                        //	foreach ( Item item in regs.Items )
                        //		item.LootType = LootType.Newbied;
                        //}

                        PackItem(regs);

                        regs.LootType = LootType.Regular;

                        PackScroll(0);
                        PackScroll(1);
                        PackScroll(2);

                        Spellbook book = new Spellbook((ulong)0x382A8C38);

                        EquipItem(book);

                        book.LootType = LootType.Blessed;

                        EquipItem(new Robe(Utility.RandomBlueHue()));
                        EquipItem(new WizardsHat());

                        break;
                    }
                case SkillName.Mining:
                    {
                        PackItem(new Pickaxe());
                        break;
                    }
                case SkillName.Musicianship:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Parry:
                    {
                        EquipItem(new WoodenShield());
                        break;
                    }
                case SkillName.Peacemaking:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Poisoning:
                    {
                        PackItem(new LesserPoisonPotion());
                        PackItem(new LesserPoisonPotion());
                        break;
                    }
                case SkillName.Provocation:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Snooping:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.SpiritSpeak:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Stealing:
                    {
                        PackItem(new Lockpick(20));
                        break;
                    }
                case SkillName.Swords:
                    {
                        EquipItem(new Katana());
                        break;
                    }
                case SkillName.Tactics:
                    {
                        EquipItem(new Katana());
                        break;
                    }
                case SkillName.Tailoring:
                    {
                        PackItem(new BoltOfCloth());
                        PackItem(new SewingKit());
                        break;
                    }
                case SkillName.Tinkering:
                    {
                        PackItem(new TinkerTools());
                        break;
                    }
                case SkillName.Tracking:
                    {
                        if (m_Mobile != null)
                        {
                            Item shoes = m_Mobile.FindItemOnLayer(Layer.Shoes);

                            if (shoes != null)
                                shoes.Delete();
                        }

                        EquipItem(new Boots(Utility.RandomYellowHue()));
                        EquipItem(new SkinningKnife());
                        break;
                    }
                case SkillName.Veterinary:
                    {
                        PackItem(new Bandage(5));
                        PackItem(new Scissors());
                        break;
                    }
                case SkillName.Wrestling:
                    {
                        EquipItem(new LeatherGloves());
                        break;
                    }
            }
        }
    }
}
