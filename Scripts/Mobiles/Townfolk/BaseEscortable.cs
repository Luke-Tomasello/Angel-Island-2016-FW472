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

/* Scripts/Mobiles/Townfolk/BaseEscortable.cs
 * ChangeLog
 *	2/16/11, adam
 *		don't allow profitable farming of blue townsfolk from a region which is usually guarded.
 *		Note: murders already only get 1/3 of creatures loot, so this is a double whammy for them
 *  2/27/06, Adam
 *      Log when an Escortable NPC is Abandoned.
 *      This is important for addressing player complaints regarding the high cost 'Treasure Hunter' NPCs
 *	06/22/06, Adam
 *		Remove the "RETURNING 2 MINUTES" text from the console output
 *	05/18/06, Adam
 *		- rewrite to elimnate named locations and replace with Point locations.
 *			This change lets us set a distination independent of the region name.
 *		- double fame for unguarded locations
 *	04/27/06, weaver
 *		- Added a virtual TimeSpan AbandonDelay to control delay before escort
 *		abandons its owner.
 *		- Altered abandon logic to use this new function instead of fixed 2 minute time.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *	1/27/06, Adam
 *		add GetMaster(). Like GetEscorter but without all the kooky side-effects :\
 *		Used in: AddCustomContextEntries()
 *		Note: GetEscorter() was doing all sorts of stuff including set ControlOrder
 *	1/20/06, Adam
 *		1. More virtuals to over ride text and behaviors
 *		2. unfortunately the BaseEscortable class relies on GetDestination() being
 *		called from OnThink() to reload the destination info from the saved string
 *		after a WorldLoad  ... lame!
 *		We should find a better way to handle this and eliminate this hack
 *	1/16/06, Adam
 *		Also add a virtual ArrivedSpeak() so we can have a custom "Arrived" message
 *	1/15/06, Adam
 *		Add support for coordinate based escorts instead of just town/dungeons.
 *	1/13/06, Adam
 *		Virtualize the distribution of loot so that it can be over ridden in the derived class.
 *		Extend the system to give items as well as gold: ProvideLoot(), GiveLoot( Item item )
 *	6/27/04, Pix
 *		lowered gold in their backpack
 *	6/27/04, Pix
 *		Further tweaks to escorts:
 *		Escort Delay: 10 minutes
 *		gold to town: 100-300
 *		gold to non-town: 200-400
 *  6/23/04, Pix
 *		Halved gold if going to town.
 *  6/22/04, Old Salty
 * 		Increased time between escorts from 5 to 20 minutes.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/24/04, mith
 *		Commented Compassion gain on escort complete, since virtues are now disabled.
 *	4/24/04, adam
 *		Commented out "bool gainedPath = false;"
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Items;
using Server.Regions;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class BaseEscortable : BaseOverland
    {
        // wea: added to control delay before abandoning
        public virtual TimeSpan AbandonDelay
        {
            get
            {
                //Console.WriteLine("RETURNING 2 MINUTES");
                return TimeSpan.FromMinutes(2.0);
            }
        }

        private Region m_Destination;
        private string m_DestinationString;

        private DateTime m_DeleteTime;
        private Timer m_DeleteTimer;

        public override bool Commandable { get { return false; } } // Our master cannot boss us around!

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Destination
        {
            get { return m_Destination == null ? null : m_Destination.Name; }
            set { m_DestinationString = value; m_Destination = Find(value); }
        }

        private static string[] m_TownNames = new string[]
            {

                new Point3D(2275,1210,0).ToString(),	// "Cove" 
				new Point3D(1495,1629,10).ToString(),	// "Britain"
				new Point3D(1383,3815,0).ToString(),	// "Jhelom"

				new Point3D(2466,544,0).ToString(),		// "Minoc"
				new Point3D(3650,2653,0).ToString(),	// "Ocllo"
				new Point3D(1867,2780,0).ToString(),	// "Trinsic"

				new Point3D(2892,685,0).ToString(),		// "Vesper"
				new Point3D(635,860,0).ToString(),		// "Yew"

				new Point3D(632,2233,0).ToString(),		// "Skara Brae"
				new Point3D(3732,1279,0).ToString(),	// "Nujel'm"

				new Point3D(4442,1172,0).ToString(),	// "Moonglow" 
				new Point3D(3714,2220,20).ToString(),	// "Magincia"

				// new places!!

				new Point3D(4530,1378,23).ToString(),	// "Britannia Royal Zoo"
			};

        [Constructable]
        public BaseEscortable()
        {

        }

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
            AddItem(new FancyShirt(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            PackGold(50, 125);
        }

        public override bool OnBeforeDeath()
        {
            bool obd = base.OnBeforeDeath();

            // don't allow profitable farming of blue townsfolk from a region which is usually guarded.
            //	Note: murders already only get 1/3 of creatures loot, so this is a double whammy for them
            if (obd && Core.UOMO)
                if (!(this.Spawner != null && Region.Find(this.Spawner.Location, this.Spawner.Map) as Regions.GuardedRegion != null && Region.Find(this.Spawner.Location, this.Spawner.Map).IsGuarded))
                {
                    // first find out how much gold this creature is dropping
                    int MobGold = this.GetGold();

                    // reds get 1/3 of usual gold
                    int NewGold = MobGold / 3;

                    // first delete all dropped gold
                    Container pack = this.Backpack;
                    if (pack != null)
                    {
                        // how much gold is on the creature?
                        Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                        foreach (Item g in golds)
                        {
                            pack.RemoveItem(g);
                            g.Delete();
                        }

                        this.PackGold(NewGold);
                    }
                }

            return obd;
        }

        public virtual bool SayDestinationTo(Mobile m)
        {
            Region dest = GetDestination();

            if (dest == null || !m.Alive)
                return false;

            Mobile escorter = GetEscorter();

            if (escorter == null)
            {
                DestinationSpeak(dest.Name);
                return true;
            }
            else if (escorter == m)
            {
                LeadOnSpeak(dest.Name);
                return true;
            }

            return false;
        }

        private static Hashtable m_EscortTable = new Hashtable();

        public static Hashtable EscortTable
        {
            get { return m_EscortTable; }
        }

        protected virtual TimeSpan EscortDelay { get { return TimeSpan.FromMinutes(10.0); } }

        public virtual bool AcceptEscorter(Mobile m)
        {
            Region dest = GetDestination();

            if (dest == null)
                return false;

            Mobile escorter = GetEscorter();

            if (escorter != null || !m.Alive)
                return false;

            BaseEscortable escortable = (BaseEscortable)m_EscortTable[m];

            if (escortable != null && !escortable.Deleted && escortable.GetEscorter() == m)
            {
                Say("I see you already have an escort.");
                return false;
            }
            else if (m is PlayerMobile && (((PlayerMobile)m).AccessLevel == AccessLevel.Player) && (((PlayerMobile)m).LastEscortTime + EscortDelay) >= DateTime.Now)
            {
                int minutes = (int)Math.Ceiling(((((PlayerMobile)m).LastEscortTime + EscortDelay) - DateTime.Now).TotalMinutes);

                Say("You must rest {0} minute{1} before we set out on this journey.", minutes, minutes == 1 ? "" : "s");
                return false;
            }
            else if (SetControlMaster(m))
            {
                m_LastSeenEscorter = DateTime.Now;

                if (m is PlayerMobile)
                    ((PlayerMobile)m).LastEscortTime = DateTime.Now;

                LeadOnSpeak(dest.Name);
                m_EscortTable[m] = this;
                StartFollow();
                return true;
            }

            return false;
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 3))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Region dest = GetDestination();

            if (dest != null && !e.Handled && e.Mobile.InRange(this.Location, 3))
            {
                if (e.HasKeyword(0x1D)) // *destination*
                    e.Handled = SayDestinationTo(e.Mobile);
                else if (e.HasKeyword(0x1E)) // *i will take thee*
                    e.Handled = AcceptEscorter(e.Mobile);
            }
        }

        public override void OnAfterDelete()
        {
            if (m_DeleteTimer != null)
                m_DeleteTimer.Stop();

            m_DeleteTimer = null;

            base.OnAfterDelete();
        }

        public override void OnThink()
        {
            // unfortunately the BaseEscortable class relies on GetDestination() being
            //	called from OnThink() to reload the destination info from the saved string
            //	after a WorldLoad  ... lame!
            //	We should find a better way to handle this and eliminate this hack
            GetDestination(); /* hack */

            base.OnThink();
            CheckAtDestination();
        }

        protected override bool OnMove(Direction d)
        {
            if (!base.OnMove(d))
                return false;

            CheckAtDestination();

            return true;
        }

        public virtual void StartFollow()
        {
            StartFollow(GetEscorter());
        }

        public virtual void StartFollow(Mobile escorter)
        {
            if (escorter == null)
                return;

            ActiveSpeed = 0.1;
            PassiveSpeed = 0.2;

            ControlOrder = OrderType.Follow;
            ControlTarget = escorter;

            CurrentSpeed = 0.1;
        }

        public virtual void StopFollow()
        {
            ActiveSpeed = 0.2;
            PassiveSpeed = 1.0;

            ControlOrder = OrderType.None;
            ControlTarget = null;

            CurrentSpeed = 1.0;
        }

        private DateTime m_LastSeenEscorter;

        // Like GetEscorter but without all the kooky side-effects :\
        public Mobile GetMaster()
        {
            if (!Controlled)
                return null;

            Mobile master = ControlMaster;

            if (master == null)
                return null;

            if (master.Deleted || master.Map != this.Map || !master.InRange(Location, 30) || !master.Alive)
                return null;

            return master;
        }

        public virtual Mobile GetEscorter()
        {
            if (!Controlled)
                return null;

            Mobile master = ControlMaster;

            if (master == null)
                return null;

            if (master.Deleted || master.Map != this.Map || !master.InRange(Location, 30) || !master.Alive)
            {
                StopFollow();

                TimeSpan lastSeenDelay = DateTime.Now - m_LastSeenEscorter;

                if (lastSeenDelay >= AbandonDelay)
                {
                    master.SendLocalizedMessage(1042473); // You have lost the person you were escorting.
                    Say(1005653); // Hmmm.  I seem to have lost my master.

                    SetControlMaster(null);
                    m_EscortTable.Remove(master);

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(Delete));
                    LogAbandon(master);
                    return null;
                }
                else
                {
                    ControlOrder = OrderType.Stay;
                    return master;
                }
            }

            if (ControlOrder != OrderType.Follow)
                StartFollow(master);

            m_LastSeenEscorter = DateTime.Now;
            return master;
        }

        private void LogAbandon(Mobile master)
        {
            LogHelper Logger = new LogHelper("EscortableAbandoned.log", false);
            Logger.Log(LogType.Text, "The player:");
            Logger.Log(LogType.Mobile, master);
            Logger.Log(LogType.Text, "Has abandoned the Escortable NPC:");
            Logger.Log(LogType.Mobile, this);
            Logger.Finish();
        }

        public virtual void BeginDelete()
        {
            if (m_DeleteTimer != null)
                m_DeleteTimer.Stop();

            m_DeleteTime = DateTime.Now + TimeSpan.FromMinutes(3.0);

            m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - DateTime.Now);
            m_DeleteTimer.Start();
        }

        protected bool IsSafe(Region dest)
        {
            // is it a guarded region?
            if (dest is GuardedRegion && ((GuardedRegion)dest).IsGuarded)
                // yes it is, and it is actively guarded
                return true;

            return false;
        }

        public void GiveLoot(Item item)
        {   // adam: default is to try to pack an enchanted scroll
            GiveLoot(item, true);
        }

        public void GiveLoot(Item item, bool EScrollChance)
        {
            Mobile escorter = GetEscorter();

            if (escorter == null)
                return;

            // wea: check for chance to drop enchanted scroll instead
            if (EScrollChance && Server.Engines.SDrop.SDropTest(item, CoreAI.EScrollChance))
            {
                // Drop a scroll instead of the item
                EnchantedScroll escroll = Loot.GenEScroll((object)item);

                // Delete the original item
                item.Delete();

                // Re-reference item to escroll and continue
                item = (Item)escroll;
            }

            // sanity
            if (item == null)
            {
                Console.WriteLine("Warning: Null item generated in BaseEscortable.PackItem");
                return;
            }

            Container cont = escorter.Backpack;

            if (cont == null)
                cont = escorter.BankBox;

            if (cont == null || !cont.TryDropItem(escorter, item, false))
                item.MoveToWorld(escorter.Location, escorter.Map);
        }

        public virtual void ProvideLoot(Mobile escorter)
        {
            if (escorter == null)
                return;

            Gold gold = new Gold(200, 400);

            //lower gold if we're going to town
            Region dest = GetDestination();
            if (dest != null && IsSafe(dest))
            {
                //Say("This is a Safe location");
                gold.Amount -= 100;
                Misc.Titles.AwardFame(escorter, 10, true);

            }
            else
            {   // more gold, more fame
                //Say("This is NOT a Safe location");
                Misc.Titles.AwardFame(escorter, 20, true);
            }

            GiveLoot(gold);
        }

        public virtual void ArrivedSpeak(string name)
        {
            // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
            Say(1042809, name);
        }

        public virtual void LeadOnSpeak(string name)
        {
            Say("Lead on! Payment will be made when we arrive in {0}.", name);
        }

        public virtual void DestinationSpeak(string name)
        {
            Say("I am looking to go to {0}, will you take me?", name);
        }

        public virtual bool CheckAtDestination()
        {
            Region dest = GetDestination();

            if (dest == null)
                return false;

            Mobile escorter = GetEscorter();

            if (escorter == null)
                return false;

            if (There(Location))
            {
                // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
                ArrivedSpeak(escorter.Name);

                ProvideLoot(escorter);      // Give the player their reward for this escort	
                Reset();                    // not going anywhere
                OnEscortComplete();         // ask the Town Crier to stop
                Cleanup();                  // start the delete timer

                return true;
            }

            return false;
        }

        public BaseEscortable(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            Region dest = GetDestination();

            writer.Write(dest != null);

            if (dest != null)
                writer.Write(dest.Name);

            writer.Write(m_DeleteTimer != null);

            if (m_DeleteTimer != null)
                writer.WriteDeltaTime(m_DeleteTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (reader.ReadBool())
                m_DestinationString = reader.ReadString(); // NOTE: We cannot EDI.Find here, regions have not yet been loaded :-(

            if (reader.ReadBool())
            {
                m_DeleteTime = reader.ReadDeltaTime();
                m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - DateTime.Now);
                m_DeleteTimer.Start();
            }
        }

        public override bool CanBeRenamedBy(Mobile from)
        {
            return (from.AccessLevel >= AccessLevel.GameMaster);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            Region dest = GetDestination();

            if (dest != null && from.Alive)
            {
                Mobile escorter = GetMaster();

                if (escorter == null || escorter == from)
                    list.Add(new AskDestinationEntry(this, from));

                if (escorter == null)
                    list.Add(new AcceptEscortEntry(this, from));
                else if (escorter == from)
                    list.Add(new AbandonEscortEntry(this, from));
            }

            base.AddCustomContextEntries(from, list);
        }

        public virtual string[] GetPossibleDestinations()
        {
            return m_TownNames;
        }

        public bool SystemInitialized()
        {
            if (Map.Felucca.Regions.Count == 0 || Map == null || Map == Map.Internal || Location == Point3D.Zero)
                return false; // Not yet fully initialized

            return true;
        }

        public virtual Region PickRandomDestination()
        {
            Point3D point;
            return PickRandomDestination(out point);
        }

        public virtual Region PickRandomDestination(out Point3D point)
        {
            point = new Point3D();

            if (SystemInitialized() == false)
                return null; // Not yet fully initialized

            // sanity
            if (GetPossibleDestinations() == null || GetPossibleDestinations().Length == 0)
                return null;

            string[] px = GetPossibleDestinations();

            object[,] places = new object[px.Length, 2];

            for (int ix = 0; ix < px.Length; ix++)
            {
                places[ix, 0] = px[ix];
                places[ix, 1] = Utility.RandomDouble();
            }

            // Bubble sort method.
            object[,] holder = new object[1, 2];
            for (int x = 0; x < px.Length; x++)
                for (int y = 0; y < px.Length - 1; y++)
                    if ((double)places[y, 1] > (double)places[y + 1, 1])
                    {
                        // holder = places[y + 1];
                        holder[0, 0] = places[y + 1, 0];
                        holder[0, 1] = places[y + 1, 1];

                        // places[y + 1] = places[y];
                        places[y + 1, 0] = places[y, 0];
                        places[y + 1, 1] = places[y, 1];

                        // places[y] = holder;
                        places[y, 0] = holder[0, 0];
                        places[y, 1] = holder[0, 1];
                    }

            for (int jx = 0; jx < px.Length; jx++)
            {
                Region reg = Find(places[jx, 0] as string);
                // keep trying if we pick a spot where we are
                if (reg == null
                    || reg.Contains(this.Location)
                    || reg.Name == null
                    || reg.Name == ""
                    || reg.Name == "DynRegion")
                    continue;

                point = Point3D.Parse(places[jx, 0] as string);
                return reg;
            }

            return null;

            // Adam: remove this old implementation as it can loop forever if the array passed is bad.

            /*if (SystemInitialized() == false)
				return null; // Not yet fully initialized

			string[] possible = GetPossibleDestinations();
			string picked = null;
			Region test = null;

			while (picked == null && possible != null)
			{
				picked = possible[Utility.Random(possible.Length)];
				test = Find(picked);

				// keep trying if we pick a spot where we are
				if (test == null
					|| test.Contains(this.Location)
					|| test.Name == null
					|| test.Name == ""
					|| test.Name == "DynRegion")
					picked = null;
			}

			return test;*/
        }

        public Region GetDestination()
        {
            if (SystemInitialized() == false)
                return null; // Not yet fully initialized

            if (m_Destination == null && m_DeleteTimer == null)
                m_Destination = PickRandomDestination();
            else
                return m_Destination;

            if (m_Destination != null)
            {
                m_DestinationString = m_Destination.Name;
                return m_Destination;
            }

            return (m_Destination = null);
        }

        public class DeleteTimer : Timer
        {
            private Mobile m_Mobile;

            public DeleteTimer(Mobile m, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Mobile.Delete();
            }
        }

        public virtual bool There(Point3D p)
        {
            if (m_Destination == null)
                return false;
            return m_Destination.Contains(p);
        }

        public void Reset()
        {
            // not going anywhere
            m_Destination = null;
            m_DestinationString = null;
            Mobile escorter = GetEscorter();
            if (escorter != null)
                m_EscortTable.Remove(escorter);
        }

        public virtual void Cleanup()
        {
            StopFollow();
            SetControlMaster(null);
            BeginDelete();
        }

        public Region Find(string name)
        {
            // add it if it is valid 
            Point3D location;
            Region reg = null;
            try { location = Point3D.Parse(name); }
            catch { location = Point3D.Zero; }
            if (location != Point3D.Zero)
            {   // add it then fall through
                // custom regions
                reg = Region.Find(location, Map.Felucca);
            }

            return reg;
        }

        public string DescribeLocation(Map map, Point3D p)
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            //Point3D p = Point3D.Parse(Name);
            Region rx = Region.Find(p, map);

            bool valid = Sextant.Format(p, rx.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", p.X, p.Y);

            if (rx.Map != null)
            {
                //if ( mob.Region != mob.Map.DefaultRegion && mob.Region.ToString() != "" )
                if (rx != rx.Map.DefaultRegion && rx.ToString() != "" && rx.ToString() != "DynRegion")
                {
                    location += (" in " + rx);
                }
            }

            return location;
        }
    }

    public class AskDestinationEntry : ContextMenuEntry
    {
        private BaseEscortable m_Mobile;
        private Mobile m_From;

        public AskDestinationEntry(BaseEscortable m, Mobile from)
            : base(6100, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.SayDestinationTo(m_From);
        }
    }

    public class AcceptEscortEntry : ContextMenuEntry
    {
        private BaseEscortable m_Mobile;
        private Mobile m_From;

        public AcceptEscortEntry(BaseEscortable m, Mobile from)
            : base(6101, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.AcceptEscorter(m_From);
        }
    }

    public class AbandonEscortEntry : ContextMenuEntry
    {
        private BaseEscortable m_Mobile;
        private Mobile m_From;

        public AbandonEscortEntry(BaseEscortable m, Mobile from)
            : base(6102, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.Delete(); // OSI just seems to delete instantly
        }
    }
}
