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

/* Mobiles/Vendors/QuestGiver.cs
 * CHANGELOG:
 *	11/1/10, Adam
 *		If the remembered item is deleted (or freeze dried) it's serialized as zero which is normal
 *		item bahavior. However, we were then trying to add a null ietm to the databasse and blowing things up.
 *		I added code to skil deleted items, but we should probably come up with a better way to handle this.
 *	1/30/10, Adam
 *		initial creation
 */

using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Server.Mobiles
{
    public class QuestGiver : BaseVendor
    {
        private Mobile m_Owner;
        private Mobile m_From;

        // key value pairs
        // [keyword1, keyword2...] [action1, action2...]
        private Dictionary<string, object[]> m_KeywordDatabase = new Dictionary<string, object[]>();

        //Misc vendor stuff.
        protected ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool CanBeDamaged() { return false; }
        public override bool ShowFameTitle { get { return false; } }
        public override bool DisallowAllMoves { get { return true; } }
        public override bool ClickTitle { get { return true; } }
        public override bool CanTeach { get { return false; } }
        public override void InitSBInfo() { }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Owner
        {
            get { return (PlayerMobile)m_Owner; }
            set { m_Owner = value; }
        }

        [Constructable]
        public QuestGiver()
            : this(null)
        {
        }

        public QuestGiver(Mobile owner)
            : base("the quest giver")
        {
            m_Owner = owner;
            IsInvulnerable = true;
            CantWalk = true;
            InitStats(75, 75, 75);
            EmoteHue = Utility.RandomYellowHue();
        }

        public QuestGiver(Serial serial)
            : base(serial)
        {
        }

        private enum TokenTypes
        {
            String,
            Char,
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2);    //version

            // version 2
            // write the Alias database
            List<string[]> AliasDatabase = new List<string[]>();
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                string[] found = new string[0];
                if (FindKeywordAliases(kvp.Key, out found))
                {   // we sort it so that all alias look alike
                    Array.Sort(found);
                    // does the database already contain this alias list?
                    bool Contains = false;
                    for (int ty = 0; ty < AliasDatabase.Count; ty++)
                    {
                        if (AliasDatabase[ty].Length == found.Length)
                        {
                            for (int uo = 0; uo < AliasDatabase[ty].Length; uo++)
                                if (AliasDatabase[ty][uo] == found[uo])
                                {   // if the last one matches, we have a matching set
                                    if (uo + 1 == AliasDatabase[ty].Length)
                                        Contains = true;
                                }
                                else
                                    break;
                        }
                    }
                    if (Contains == false)
                        AliasDatabase.Add(found);
                }
            }

            // number of aliased actions
            writer.Write(AliasDatabase.Count);
            for (int ii = 0; ii < AliasDatabase.Count; ii++)
            {
                // number of aliases    
                writer.Write(AliasDatabase[ii].Length);
                for (int oo = 0; oo < AliasDatabase[ii].Length; oo++)
                {   // write the aliases
                    writer.Write(AliasDatabase[ii][oo]);
                }
            }

            // version 1
            writer.Write(m_memory);
            writer.Write(m_distance);

            writer.Write(m_KeywordDatabase.Count);
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is string)
                    {
                        writer.Write((int)TokenTypes.String);
                        writer.Write(o as string);
                    }
                    else if (o is Char)
                    {
                        writer.Write((int)TokenTypes.Char);
                        writer.Write((char)o);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }

            writer.Write(m_ItemDatabase.Count);
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is Field)
                    {
                        writer.Write((int)((Field)o));
                    }
                    else if (o is string)
                    {
                        writer.Write(o as string);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }

            //version 0:
            writer.Write(m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            // introduced in version 2 to fixup the aliases post-load
            List<string[]> AliasDatabase = new List<string[]>();

            switch (version)
            {
                case 2:
                    {
                        // read the alias database
                        int ad_size = reader.ReadInt();
                        for (int ii = 0; ii < ad_size; ii++)
                        {   // number of aliases in this array
                            int netls = reader.ReadInt();
                            List<string> temp = new List<string>();
                            for (int uu = 0; uu < netls; uu++)
                                temp.Add(reader.ReadString());
                            AliasDatabase.Add(temp.ToArray());
                        }
                        goto case 1;
                    }
                case 1:
                    {
                        m_memory = reader.ReadDouble();
                        m_distance = reader.ReadInt();

                        // read the keyword database
                        int kwdb_count = reader.ReadInt();
                        for (int ix = 0; ix < kwdb_count; ix++)
                        {
                            string key = reader.ReadString();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                switch ((TokenTypes)reader.ReadInt())
                                {
                                    case TokenTypes.Char:
                                        list.Add((Char)reader.ReadChar());
                                        continue;

                                    case TokenTypes.String:
                                        list.Add(reader.ReadString());
                                        continue;
                                }
                            }
                            m_KeywordDatabase.Add(key, list.ToArray());
                        }

                        // read the item database
                        int idb_count = reader.ReadInt();
                        for (int ix = 0; ix < idb_count; ix++)
                        {
                            Item key = reader.ReadItem();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                Field field = (Field)reader.ReadInt();
                                list.Add(field);
                                switch (field)
                                {
                                    case Field.Track:
                                        continue;

                                    case Field.Name:
                                        list.Add(reader.ReadString());
                                        jx++;
                                        continue;
                                }
                            }

                            if (key != null)
                                m_ItemDatabase.Add(key, list.ToArray());
                            else
                            {   // the key has been deleted.
                                // would should probably add 'status' strings to the QG so that he can tell
                                //	the owner what happened. For now just delete the item, i.e., don't add it.
                            }
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            // okay, patch the keyword database aliases
            if (AliasDatabase.Count > 0)
            {
                for (int gg = 0; gg < AliasDatabase.Count; gg++)
                {
                    // grab the shared action from the first key
                    object[] shared_action = m_KeywordDatabase[AliasDatabase[gg][0]];
                    for (int yy = 1; yy < AliasDatabase[gg].Length; yy++)
                    {   // patch 'em!
                        m_KeywordDatabase[AliasDatabase[gg][yy]] = shared_action;
                    }
                }
            }

            NameHue = CalcInvulNameHue();
        }

        // how long we remember players in minutes
        private double m_memory = 30;               // default: 30 minutes

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Memory
        {
            get { return TimeSpan.FromMinutes(m_memory); }
            set { m_memory = value.TotalMinutes; }
        }

        // how far until we talk to a player
        private int m_distance = 6;                     // default: 6 tiles

        [CommandProperty(AccessLevel.GameMaster)]
        public int Distance
        {
            get { return m_distance; }
            set { m_distance = value; }
        }

        private Memory m_PlayerMemory = new Memory();       // memory used to remember is a saw a player in the area
        public override void OnSeePlayer(Mobile m)
        {
            // yeah
            if (m is PlayerMobile == false)
                return;

            // sanity
            if (m.Deleted || m.Hidden || !m.Alive || m.AccessLevel > this.AccessLevel || !this.CanSee(m))
                return;

            if (m_PlayerMemory.Recall(m) == false && this.GetDistanceToSqrt(m) <= m_distance)
            {   // we havn't seen this player yet
                m_PlayerMemory.Remember(m, m_memory * 60);                  // remember him for this long
                bool found = m_KeywordDatabase.ContainsKey("onenter");      // is OnEnter defined?
                if (found)                                                  // if so execute!
                    OnSpeech(new SpeechEventArgs(m, "onenter", MessageType.Regular, SpeechHue, new int[0], true));
            }
        }

        private DateTime m_lastLook = DateTime.MinValue;
        public override void OnThink()
        {
            base.OnThink();

            // look around every 2 seconds
            if (DateTime.UtcNow > m_lastLook && AIObject != null)
            {   // remember players in the area
                AIObject.LookAround(RangePerception);
                m_lastLook = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
            }
        }

        public override bool IsOwner(Mobile m)
        {
            return (m == m_Owner || m.AccessLevel >= AccessLevel.GameMaster);
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();
            SpeechHue = 0x3B2;

            NameHue = CalcInvulNameHue();

            if (this.Female = Utility.RandomBool())
            {
                this.Body = 0x191;
                this.Name = NameList.RandomName("female");
            }
            else
            {
                this.Body = 0x190;
                this.Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Item item = new FancyShirt(Utility.RandomNeutralHue());
            item.Layer = Layer.InnerTorso;
            AddItem(item);
            AddItem(new LongPants(Utility.RandomNeutralHue()));
            AddItem(new BodySash(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new Cloak(Utility.RandomNeutralHue()));

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            Container pack = new Backpack();
            pack.Movable = false;
            AddItem(pack);
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            bool result = false;
            if (base.CheckNonlocalDrop(from, item, target))
                result = true;
            else if (IsOwner(from))
                result = true;

            if (result == true)
            {
                // We must wait until the item is added
                Timer.DelayCall(TimeSpan.Zero, new TimerStateCallback(NonLocalDropCallback), new object[] { from, item });
            }

            return result;
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            if (base.CheckNonlocalLift(from, item))
                return true;
            else if (IsOwner(from))
                return true;

            return false;
        }

        private string GetTrackedItemLabel(Item dropped)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                if (kvp.Key == dropped && GetField(kvp.Value, Field.Track) != null)
                    return GetField(kvp.Value, Field.Name) as string;
            }

            return null;
        }

        private bool TrackedItem(Item dropped)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                if (kvp.Key == dropped && GetField(kvp.Value, Field.Track) != null)
                    return true;
            }

            return false;
        }

        private enum Disposition
        {
            Keep,
            Return,
            Delete
        }

        private Disposition m_disposition = Disposition.Keep;

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            // onreceive handling: is OnReceive defined? Is this a tracked item?
            string found;
            if (FindKeyPhrase(new object[] { "onreceive", GetTrackedItemLabel(dropped) }, 0, out found) && TrackedItem(dropped))
            {   // do onreceive handling
                // since we call OnSpeech() with Internal==true for privilege reasons, 
                //	we must track who was really talking. i.e., non owner players cannot say "onreceive"
                m_From = from;                          // who we are talking to
                m_disposition = Disposition.Return;     // default disposition
                OnSpeech(new SpeechEventArgs(from, "onreceive" + " " + GetTrackedItemLabel(dropped), MessageType.Regular, SpeechHue, new int[0], true));
                switch (m_disposition)
                {
                    case Disposition.Keep:
                        if (this.Backpack != null && this.Backpack.TryDropItem(from, dropped, false))
                        {   // item placed in my backpack
                            return true;
                        }
                        else
                        {
                            SayTo(from, 503211); // I can't carry any more.
                            return false;
                        }
                    case Disposition.Delete:
                        dropped.Delete();
                        return true;

                    case Disposition.Return:
                        return false;
                }
            }

            /// stocking the NPC
            if (IsOwner(from))
            {
                if (this.Backpack != null && this.Backpack.TryDropItem(from, dropped, false))
                {
                    OnItemGiven(from, dropped);
                    return true;
                }
                else
                {
                    SayTo(from, 503211); // I can't carry any more.
                    return false;
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        enum Field
        {
            Name,
            Track,
        }

        private object GetField(object[] tokens, Field field)
        {
            for (int ix = 0; ix < tokens.Length; ix++)
            {
                if (tokens[ix] is Field && (Field)tokens[ix] == field)
                {
                    switch (field)
                    {
                        case Field.Name:
                            int name_index = ix + 1;
                            if (name_index <= tokens.Length && tokens[name_index] is string)
                                return tokens[name_index];
                            else
                                return null;
                            break;
                        case Field.Track:
                            return tokens[ix];
                            break;
                    }
                }
            }

            return null;
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (from.GetDistanceToSqrt(this) <= 4);
        }

        private object[] RemoveHandle(object[] tokens, string handle)
        {
            if (handle.ToLower() == (tokens[0] as string).ToLower())
            {
                List<object> list = new List<object>();
                for (int ix = 1; ix < tokens.Length; ix++)
                    list.Add(tokens[ix]);

                return list.ToArray();
            }

            return tokens;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            try
            {
                Mobile from = e.Mobile;
                m_From = from;

                if (e.Handled)
                    return;

                #region standard processing
                BaseHouse house = BaseHouse.FindHouseAt(this);
                bool bIsHouseOwner = false;
                if (house != null)
                {
                    bIsHouseOwner = house.IsCoOwner(from);
                }
                if (e.HasKeyword(0x3F) || (e.HasKeyword(0x174))) // status
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        SayTo(from, "I'm not sure.");
                        e.Handled = true;
                    }
                    else
                    {
                        SayTo(from, "I have nothing to say to you.");
                    }
                }
                else if (e.HasKeyword(0x40) || (e.HasKeyword(0x175))) // dismiss
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        Dismiss(from);
                        e.Handled = true;
                    }
                }
                else if (e.HasKeyword(0x41) || (e.HasKeyword(0x176))) // cycle
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        this.Direction = this.GetDirectionTo(from);
                        e.Handled = true;
                    }
                }
                #endregion

                // compile speech into an array of strings and delimiters
                object[] tokens = Compile(e.Speech);

                // remove the optional handle, i.e., the quest giver's name
                // Distinguishes between zones and Quest Giver NPC�s and TEST CENTER �set� commands
                tokens = RemoveHandle(tokens, Name);

                #region COMMANDS (remember, clear)
                if (e.Handled == false && (tokens[0] as string).ToLower() == "remember")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;

                        // okay, process the special OnReceive keyword by prompting to target an item
                        e.Mobile.Target = new OnReceiveTarget(this);
                        e.Mobile.SendMessage("Target the item to remember.");
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "title")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        Title = null;
                        from.SendMessage("Title cleared.");
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "labels")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        int count = m_ItemDatabase.Count;
                        m_ItemDatabase.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} items cleared.", count), from.NetState);
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "keywords")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        int count = m_KeywordDatabase.Count;
                        m_KeywordDatabase.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} keywords cleared.", count), from.NetState);
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "reset")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        int count = m_ItemDatabase.Count;
                        m_ItemDatabase.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} items cleared.", count), from.NetState);
                        count = m_KeywordDatabase.Count;
                        m_KeywordDatabase.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} keywords cleared.", count), from.NetState);
                    }
                }
                #endregion

                #region GET & SET
                // process owner programming commands GET & SET
                if (e.Handled == false && (IsOwner(from) || bIsHouseOwner))
                {
                    if (tokens[0] is string && (tokens[0] as string).ToLower() == "get")
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 1 && tokens[1] is string)
                        {
                            e.Handled = true;
                            switch ((tokens[1] as string).ToLower())
                            {
                                case "distance":
                                    from.SendMessage("distance is set to {0} tiles.", m_distance);
                                    break;

                                case "memory":
                                    from.SendMessage("Memory set to {0} minutes.", m_memory);
                                    break;
                            }
                        }
                    }

                    if (tokens[0] is string && (tokens[0] as string).ToLower() == "set")
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 2 && tokens[1] is string && tokens[2] is string)
                        {
                            e.Handled = true;
                            switch ((tokens[1] as string).ToLower())
                            {
                                case "name":
                                    {
                                        // Pattern match for invalid characters
                                        Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");
                                        string text = e.Speech.Substring(e.Speech.IndexOf(tokens[2] as string, 0, StringComparison.CurrentCultureIgnoreCase)).Trim();
                                        if (InvalidPatt.IsMatch(text))
                                        {
                                            // Invalid chars
                                            from.SendMessage("You may only use numbers, letters, apostrophes, hyphens and spaces.");
                                        }
                                        else if (!Misc.NameVerification.Validate(text, 2, 16, true, true, true, 1, Misc.NameVerification.SpaceDashPeriodQuote))
                                        {
                                            // Invalid for some other reason
                                            from.SendMessage("That name is not allowed here.");
                                        }
                                        else if (true)
                                        {
                                            Name = text;
                                            from.SendMessage("Set.");
                                        }
                                        else
                                            from.SendMessage("Usage: set name <name string>");
                                    }
                                    break;

                                case "title":
                                    {
                                        // Pattern match for invalid characters
                                        Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");
                                        string text = e.Speech.Substring(e.Speech.IndexOf(tokens[2] as string, 0, StringComparison.CurrentCultureIgnoreCase)).Trim();
                                        if (InvalidPatt.IsMatch(text))
                                        {
                                            // Invalid chars
                                            from.SendMessage("You may only use numbers, letters, apostrophes, hyphens and spaces.");
                                        }
                                        else if (!Misc.NameVerification.Validate(text, 2, 16, true, true, true, 1, Misc.NameVerification.SpaceDashPeriodQuote))
                                        {
                                            // Invalid for some other reason
                                            from.SendMessage("That title is not allowed here.");
                                        }
                                        else if (true)
                                        {
                                            Title = text;
                                            from.SendMessage("Set.");
                                        }
                                        else
                                            from.SendMessage("Usage: set title <name string>");
                                    }
                                    break;

                                case "distance":
                                    {
                                        int result;
                                        // max 12 tiles
                                        if (int.TryParse(tokens[2] as string, out result) && result >= 0 && result < 12)
                                        {
                                            m_distance = result;
                                            from.SendMessage("Set.");
                                        }
                                        else
                                            from.SendMessage("Usage: set distance <number>");
                                    }
                                    break;

                                case "memory":
                                    {
                                        double result;
                                        // max 72 hours
                                        if (double.TryParse(tokens[2] as string, out result) && result > 0 && result < TimeSpan.FromHours(72).TotalMinutes)
                                        {
                                            m_memory = result;
                                            m_PlayerMemory = new Memory();
                                            from.SendMessage("Set.");
                                        }
                                        else
                                            from.SendMessage("Usage: set memory <number>");
                                    }
                                    break;
                            }
                        }
                    }
                }
                #endregion

                #region owner programming commands (keywords)
                // process owner programming commands
                if (e.Handled == false && (IsOwner(from) || bIsHouseOwner))
                {
                    // we now have a compiled list of strings and tokens
                    // find out what the user is constructing
                    //
                    // if the user is inserting a keyword, make sure it doesn't already exist and insert it
                    // if the user is adding verbs, append to named keyword

                    bool understood = true;

                    // if the user is inserting a keyword, make sure it doesn't already exist and insert it
                    if (tokens[0] is string && ((tokens[0] as string).ToLower() == "keyword" || (tokens[0] as string).ToLower() == "add"))
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 1 && tokens[1] is string)
                        {
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeKeywords(tokens, 1, false, out good, out bad);

                            if (!ck)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are already defined: {0}", bad), from.NetState);
                            else if ((tokens[1] as string).ToLower() == "onreceive")
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: Add OnReceive.<item>"), from.NetState);
                            else if (m_KeywordDatabase.Count >= 256)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Your keyword database is full."), from.NetState);
                            else
                            {
                                // okay, we don't have the keyword(s) yet, so lets add them (with a null action)
                                // remove the first token 'keywords'

                                object[][] chunks = SplitArray(tokens, '|');
                                string keyword = chunks[0][1] as string;

                                // shared placeholder for all of these keywords and aliases
                                object[] actions = new object[0];

                                object[] kwords = chunks[0];
                                for (int ix = 1; ix < kwords.Length; ix++)
                                {   // skip delimiters
                                    if (kwords[ix] is string)
                                        m_KeywordDatabase[(kwords[ix] as string).ToLower()] = actions;
                                }

                                // OPTIONAL: extract the actions
                                for (int ix = 1; ix < chunks.Length; ix++)
                                {
                                    object[] action = chunks[ix];
                                    // these are the actions
                                    OnSpeech(new SpeechEventArgs(from, "action " + keyword + " " + MakeString(action, 0), MessageType.Regular, SpeechHue, new int[0]));
                                }

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
                            }
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' keyword1[, keyword2, keyword3].", tokens[0] as string), from.NetState);
                    }
                    else if (tokens[0] is string && ((tokens[0] as string).ToLower() == "action" || (tokens[0] as string).ToLower() == "verb"))
                    {
                        if (tokens.Length > 2 && tokens[1] is string && tokens[2] is string)
                        {
                            e.Handled = true;
                            if (m_KeywordDatabase.ContainsKey((tokens[1] as string).ToLower()))
                            {   // we have all the named keywords
                                // locate the verb
                                if (CheckVerb((tokens[2] as string).ToLower()))
                                {   // append the verb list to any existing verb list
                                    object[] verbList = m_KeywordDatabase[(tokens[1] as string).ToLower()]; // the action for keyword - never null
                                    object[] action;                                // the new action

                                    if (MakeString(tokens, 0).Length + MakeString(verbList, 0).Length > 768)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Action too long for this keyword."), from.NetState);
                                    else
                                    {
                                        // oldAction.Length + 1 for the new action delimiter + the new action length - 2
                                        // We remove the first two tokens: 'action' 'keyword'. then append to the action for this keyword
                                        bool delimiter = verbList.Length > 0;
                                        action = new object[verbList.Length + (delimiter ? 1 : 0) + tokens.Length - 2];
                                        Array.Copy(verbList, 0, action, 0, verbList.Length);
                                        if (delimiter) action[verbList.Length] = '|' as object;
                                        Array.Copy(tokens, 2, action, verbList.Length + (delimiter ? 1 : 0), tokens.Length - 2);

                                        // okay. Now we have a new action. We want to associate it with all keywords that share the same action
                                        List<string> tomod = new List<string>();
                                        foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
                                        {
                                            if (kvp.Value == verbList)
                                            {   // this keyword has one of the shared actions
                                                tomod.Add(kvp.Key);
                                            }
                                        }
                                        foreach (string sx in tomod)
                                            m_KeywordDatabase[sx] = action;

                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
                                    }
                                }
                                else
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not know the verb {0}.", tokens[2] as string), from.NetState);
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not have the keyword(s) {0}.", tokens[1] as string), from.NetState);
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' 'keyword' 'verb' text.", tokens[0] as string), from.NetState);
                    }
                    else if (tokens.Length == 1 && tokens[0] is string && (tokens[0] as string).ToLower() == "list")
                    {   // list everything
                        OnSpeech(new SpeechEventArgs(from, "list keywords", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                        OnSpeech(new SpeechEventArgs(from, "list labels", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "labels")
                    {   // list the labels

                        if (m_ItemDatabase.Count > 0)
                        {
                            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                            {
                                if (kvp.Key == null)
                                    continue;

                                string where = GetItemLocation(kvp.Key);
                                if (kvp.Key.RootParent == this)
                                    where = "in my backpack";
                                else if (World.FindItem(kvp.Key.Serial) == null)
                                    where = "location unknown";

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} [{1}]", GetField(kvp.Value, Field.Name), where), from.NetState);
                            }
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no labels defined."), from.NetState);
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "keywords")
                    {   // list the keywords specified
                        if (tokens.Length > 2 && tokens[2] is string)
                        {   // loop over all of the keywords and delete it if it exists.
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeKeywords(tokens, 2, true, out good, out bad);

                            if (!ck)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are not defined: {0}", bad), from.NetState);

                            string[] good_array, bad_array;
                            ComputeKeywords(tokens, 2, true, out good_array, out bad_array);

                            List<string> remember = new List<string>();
                            foreach (string sx in good_array)
                            {
                                if (remember.Contains(sx) == false)
                                {   // add this keyword and aliases to the 'remember' array
                                    string aliases;                             // display list of keyword and aliases
                                    FindKeywordAliases(sx, out aliases);        // find 'em
                                    string[] aliases_array;                     // list of keyword and aliases
                                    FindKeywordAliases(sx, out aliases_array);  // find 'em
                                    foreach (string ux in aliases_array)        // remember that we have processed 'em
                                        remember.Add(ux);

                                    // tell the user what we are listing (all aliases are deleted with the keyword)
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keywords: {0}", aliases), from.NetState);

                                    if (m_KeywordDatabase[aliases_array[0]].Length == 0)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions: {0}.", "<null>"), from.NetState);
                                    else
                                    {
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions:"), from.NetState);
                                        object[][] actions = SplitArray(m_KeywordDatabase[aliases_array[0]], '|');
                                        foreach (object[] action in actions)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}", MakeString(action, 0)), from.NetState);
                                    }
                                }
                            }
                        }
                        else
                        {   // list all keywords and associated actions
                            if (m_KeywordDatabase.Count > 0)
                            {
                                List<string> good_list = new List<string>();
                                foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
                                    good_list.Add(kvp.Key);

                                string[] good_array = good_list.ToArray();
                                List<string> remember = new List<string>();
                                foreach (string sx in good_array)
                                {
                                    if (remember.Contains(sx) == false)
                                    {   // add this keyword and aliases to the 'remember' array
                                        string aliases;                             // display list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases);        // find 'em
                                        string[] aliases_array;                     // list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases_array);  // find 'em
                                        foreach (string ux in aliases_array)        // remember that we have processed 'em
                                            remember.Add(ux);

                                        // tell the user what we are listing (all aliases are deleted with the keyword)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keywords: {0}", aliases), from.NetState);

                                        if (m_KeywordDatabase[aliases_array[0]].Length == 0)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions: {0}.", "<null>"), from.NetState);
                                        else
                                        {
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions:"), from.NetState);
                                            object[][] actions = SplitArray(m_KeywordDatabase[aliases_array[0]], '|');
                                            foreach (object[] action in actions)
                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}", MakeString(action, 0)), from.NetState);
                                        }
                                    }
                                }
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no keywords defined."), from.NetState);
                        }
                    }
                    else if (tokens.Length >= 3 && tokens[0] is string && (tokens[0] as string).ToLower() == "delete")
                    {
                        if ((tokens[1] as string).ToLower() == "label")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the labels and delete it if it exists.
                                e.Handled = true;

                                List<Item> list = new List<Item>();
                                foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                                {
                                    if ((GetField(kvp.Value, Field.Name) as string).ToLower() == (tokens[2] as string).ToLower())
                                        list.Add(kvp.Key);
                                }
                                foreach (Item ix in list)
                                    m_ItemDatabase.Remove(ix);

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} '{1}' labels cleared.", list.Count, tokens[2] as string), from.NetState);
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' label <label>.", tokens[0] as string), from.NetState);
                        }
                        else if ((tokens[1] as string).ToLower() == "keyword")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the keywords and delete it if it exists.
                                e.Handled = true;

                                string good, bad;
                                bool ck = ComputeKeywords(tokens, 2, true, out good, out bad);

                                if (!ck)
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are not defined: {0}", bad), from.NetState);

                                string[] good_array, bad_array;
                                ComputeKeywords(tokens, 2, true, out good_array, out bad_array);

                                List<string> remember = new List<string>();
                                foreach (string sx in good_array)
                                {
                                    if (remember.Contains(sx) == false)
                                    {   // add this keyword and aliases to the 'remember' array
                                        string aliases;                             // display list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases);        // find 'em
                                        string[] aliases_array;                     // list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases_array);  // find 'em
                                        foreach (string ux in aliases_array)        // remember that we have processed 'em
                                            remember.Add(ux);

                                        // tell the user what we are deleting (all aliases are deleted with the keyword)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keywords have been deleted: {0}", aliases), from.NetState);

                                        // now remove each keyword and alias
                                        foreach (string dx in aliases_array)
                                            m_KeywordDatabase.Remove(dx);
                                    }
                                }
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' keyword1[, keyword2, keyword3].", tokens[0] as string), from.NetState);
                        }
                    }

                    if (understood == false)
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I'm sorry. I do not understand."), from.NetState);
                }
                #endregion

                // anyone talking - process keywords
                if (e.Handled == false)
                {
                    string match;
                    if (FindKeyPhrase(tokens, 0, out match) || FindKeyword(tokens, 0, out match) && m_KeywordDatabase[match].Length > 0)
                    {
                        e.Handled = true;
                        // execute the verb for this keyword

                        // do not allow standard players to access internal commands like 'OnEnter'
                        //	When e.Internal is true, it's the NPC dispatching the keyword and will be allowed
                        string akw;
                        if (AdminKeyword(tokens, 0, out akw) && !(IsOwner(from) || bIsHouseOwner || e.Internal))
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I'm sorry. You do not have access the {0} command.", match), from.NetState);
                        else
                        {
                            // begin execute 
                            object[][] actions = SplitArray(m_KeywordDatabase[match], '|');
                            int depth = 0;
                            try { ExecuteActions(from, actions, ref depth); }
                            catch
                            {
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Excessive recursion detected for keyword ({0}).", match), from.NetState);
                            }
                        }
                        // end execute
                    }
                    // else we don't recognize what was said, so we will simply ignore the player.
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        protected string DirectionMacros(string text)
        {
            switch (text.ToLower())
            {
                case "northeast":
                case "ne":
                    return "right";
                case "east":
                case "e":
                    return "east";
                case "southeast":
                case "se":
                    return "down";
                case "south":
                case "s":
                    return "south";
                case "southwest":
                case "sw":
                    return "left";
                case "west":
                case "w":
                    return "west";
                case "northwest":
                case "nw":
                    return "up";
                case "north":
                case "n":
                    return "north";
                default:
                    return text;
            }
        }

        object[] TokenList(object[] list, int offset)
        {
            object[] out_array = new object[list.Length - offset];
            Array.Copy(list, offset, out_array, 0, out_array.Length);
            return out_array;
        }

        string[] TokenList(object[] list)
        {
            List<string> strlist = new List<string>();
            for (int ix = 0; ix < list.Length; ix++)
                if (list[ix] is string)
                    strlist.Add(list[ix] as string);
            return strlist.ToArray();
        }

        string[] KeywordList(string[] list)
        {
            List<string> strlist = new List<string>();
            for (int ix = 0; ix < list.Length; ix++)
                if (m_KeywordDatabase.ContainsKey(list[ix]))
                    strlist.Add(list[ix] as string);
            return strlist.ToArray();
        }

        protected void ExecuteActions(Mobile from, object[][] actions, ref int depth)
        {
            if (depth++ > 8)
            {   // prevent user-defined recursive patterns that would otherwise crash the server
                throw new ApplicationException("Excessive recursion detected");
            }

            // execute all actions.
            //	certain failures like syntax will abort the execution. other minor failures will allow us to continue.
            for (int ix = 0; ix < actions.Length; ix++)
            {
                object[] action = actions[ix];
                bool done = false;
                if (done == false)
                {
                    switch ((action[0] as string).ToLower())
                    {
                        // see if the user already has thie thing, and if so, branch
                        case "has":
                            {   // Has <item_label> (branch to) <keyword>
                                if (action.Length == 3 && action[1] is string && action[2] is string)
                                {
                                    Memory.ObjectMemory om = m_PlayerMemory.Recall(from as object);
                                    // look to see if the player was given one of these yet
                                    if (om != null && om.Context != null)
                                    {
                                        if ((om.Context as List<string>).Contains((action[1] as string).ToLower()))
                                        {
                                            if (m_KeywordDatabase.ContainsKey((action[2] as string).ToLower()))
                                            {   // branch to this label and execute
                                                object[][] branch = SplitArray(m_KeywordDatabase[(action[2] as string).ToLower()], '|');
                                                ExecuteActions(from, branch, ref depth);
                                                return; // we're done now.
                                            }
                                            else
                                                from.SendMessage(string.Format("While executing 'has' label {0} not found.", (action[2] as string).ToLower()));
                                        }
                                    }
                                }
                                else
                                    from.SendMessage(string.Format("Usage: has <label> <keyword>."));

                            }
                            break;

                        case "random":
                            {
                                string[] keys = KeywordList(TokenList(TokenList(action, 1)));
                                if (keys.Length > 0)
                                {
                                    actions[ix] = m_KeywordDatabase[keys[Utility.Random(keys.Length)]];
                                    ix--;
                                    continue;
                                }
                            }
                            break;

                        // foreach item [in direction] do
                        case "foreach":
                            {
                                // extract the direction to look if any
                                Direction dir = Direction.Down;
                                bool have_direction = false;
                                if (action.Length >= 3)
                                {
                                    // replace things like southeast with 'down' which is the UO enum value
                                    string test = DirectionMacros(action[1] as string);
                                    foreach (string sx in Enum.GetNames(typeof(Direction)))
                                    {
                                        if (test == sx.ToLower())
                                        {
                                            have_direction = true;
                                            dir = (Direction)Enum.Parse(typeof(Direction), test, true);
                                            break;
                                        }
                                    }
                                }

                                // check to see that we have enough arguments
                                if (action.Length < 2 || (action.Length < 3 && have_direction))
                                {
                                    from.SendMessage(string.Format("Usage: foreach [direction] verb <arguments>."));
                                    // no more actions if we fail with a syntax error
                                    done = true;
                                    break;
                                }

                                // build a new action by removing the setup parameters foreach & [direction]
                                List<object> list = new List<object>();
                                for (int jx = 0; jx < action.Length; jx++)
                                {
                                    object node = action[jx];
                                    if (jx == 0 && node as string == "foreach")
                                        continue;
                                    if (jx == 1 && have_direction)
                                        continue;

                                    list.Add(node);
                                }

                                // for each item, and if we have a direction: each item in that direction from us
                                foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                                {
                                    // if a direction was specified, only list items in that direction
                                    if (have_direction && from.GetDirectionTo(kvp.Key.Location) != dir)
                                        continue;

                                    // make a temp copy we can modify
                                    object[] temp = list.ToArray();

                                    // expand macros
                                    for (int ux = 0; ux < temp.Length; ux++)
                                        if (temp[ux] is string && (temp[ux] as string).Contains("%item%"))
                                            temp[ux] = GetField(kvp.Value, Field.Name);

                                    // build the new macro-expanded action and execute
                                    List<object[]> table = new List<object[]>();
                                    table.Add(temp);
                                    ExecuteActions(from, table.ToArray(), ref depth);
                                }
                            }
                            break;
                        case "keep":
                            m_disposition = Disposition.Keep;
                            break;
                        case "return":
                            m_disposition = Disposition.Return;
                            break;
                        case "delete":
                            m_disposition = Disposition.Delete;
                            break;
                        case "emote":
                            Emote("*" + MakeString(action, 1) + "*");
                            break;
                        case "sayto":
                            SayTo(m_From, MakeString(action, 1));
                            break;
                        case "say":
                            Say(MakeString(action, 1));
                            break;
                        case "give":
                            {
                                bool known = false;
                                bool given = false;
                                string name = null;
                                foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                                {
                                    if (action[1] is string)
                                    {
                                        name = GetField(kvp.Value, Field.Name) as string;
                                        if (name.ToLower() == (action[1] as string).ToLower())
                                        {   // I had one at one time
                                            known = true;
                                            if (kvp.Key != null && kvp.Key.RootParent == this)
                                            {   // I have one now
                                                if (Backpack != null && from.Backpack != null)
                                                {
                                                    Backpack.RemoveItem(kvp.Key);
                                                    if (!from.Backpack.TryDropItem(from, kvp.Key, false))
                                                    {
                                                        this.SayTo(from, 503204);                       // You do not have room in your backpack for this.
                                                        kvp.Key.MoveToWorld(from.Location, from.Map);
                                                    }
                                                    given = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (given == false)
                                {
                                    if (known == false)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I know nothing about a {0}.", action[1]), from.NetState);
                                    else
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I'm sorry. I no longer have a {0}.", action[1]), from.NetState);

                                    // no more actions if we fail the give
                                    done = true;
                                    break;
                                }
                                else
                                {
                                    // refresh our memory of this player
                                    m_PlayerMemory.Remember(from, m_memory * 60);

                                    // extract the context object
                                    Memory.ObjectMemory om = m_PlayerMemory.Recall(from as object);
                                    if (om.Context == null)
                                        om.Context = new List<string>();

                                    // now remember what was given to this player
                                    (om.Context as List<string>).Add(name.ToLower());
                                }

                            }
                            break;
                        default:
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} is an unknown verb.", action[0]), from.NetState);
                            break;
                    }
                }
            }
        }

        private bool CheckVerb(string verb)
        {
            switch (verb)
            {
                case "has":
                case "random":
                case "foreach":
                case "keep":
                case "return":
                case "delete":
                case "emote":
                case "sayto":
                case "say":
                case "give":
                    return true;
                default:
                    return false;
            }
        }

        protected object[][] SplitArray(object[] tokens, Char splitChar)
        {
            List<object[]> list = new List<object[]>();
            List<object> objects = new List<object>();
            foreach (object o in tokens)
            {
                if (o is Char && (Char)o == splitChar)
                {
                    list.Add(objects.ToArray());
                    objects.Clear();
                    continue;
                }

                objects.Add(o);
            }

            if (objects.Count > 0)
                list.Add(objects.ToArray());

            return list.ToArray();
        }

        protected string MakeString(object[] tokens, int offset)
        {
            string temp = "";
            ExpansionStatus result;
            string match = null;
            if ((result = ExpandMacros(ref tokens, offset, ref match)) == ExpansionStatus.Okay)
                for (int ix = 0; ix < tokens.Length; ix++)
                {
                    if (temp.Length > 0 && tokens[ix] is string)
                        temp += ' ';

                    if (tokens[ix] is string)
                        temp += tokens[ix] as string;
                    else
                        temp += (Char)tokens[ix];       // add in punction like a comma (which was turned to a Char for parsing reasons
                                                        // it's now returned
                }
            else
            {
                if (result == ExpansionStatus.Unknown)                              // I never had one of these
                    temp = string.Format("I know nothing about a {0}.", match);
                else if (result == ExpansionStatus.HaveAll)                         // i've not given any of these out
                    temp = string.Format("The {0} is in my backpack.", match);
                else if (result == ExpansionStatus.BadField)                        // i have the item, but I don't know the field
                    temp = string.Format("Bad field used for: {0}.", match);
                else
                    temp = string.Format("I'm sorry. I'm at a loss looking for a {0}.", match);
            }


            return temp;
        }

        private string GetItemLocation(Item item)
        {
            Point3D px = item.GetWorldLocation();
            Map map = item.Map;
            return GetLocation(px, map);
        }

        private string GetLocation(Point3D px, Map map)
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            bool valid = Sextant.Format(px, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", px.X, px.Y);

            if (map != null)
            {
                Region reg = Region.Find(px, map);

                if (reg != map.DefaultRegion && reg.Name != null && reg.Name.Length > 0)
                {
                    location += (" in " + reg);
                }
            }

            return location;
        }

        protected enum ExpansionStatus
        {
            Okay,               // okay
            Unknown,            // I never knew of this item
            NoMore,             // I have no more
            HaveAll,            // I have all that there are
            Deleted,            // deleted or freeze dried
            BadField,           // I know the item, but there was a problem with the field
        }

        protected ExpansionStatus ExpandMacros(ref object[] tokens, int offset, ref string match)
        {
            List<object> list = new List<object>();
            for (int ix = offset; ix < tokens.Length; ix++)
            {
                string temp = tokens[ix] as string;

                if (temp == null)
                {   // a delimiter like '|'
                    list.Add(tokens[ix]);
                    continue;
                }

                temp = temp.Replace("%%", "\r");                                                // escape double '%' as a literal
                int name_start = temp.IndexOf('%');                                             // locate start of name
                int name_end = temp.LastIndexOf('%');                                           // locate end of name
                int field_start = temp.IndexOf('.');                                            // locate start of field
                int tail = temp.LastIndexOfAny(new char[] { '.', ',', '!', '?' });              // any tail delimiters?
                int field_end = (field_start == tail || tail < 0) ? temp.Length - 1 : tail - 1; //	end of field

                // %sword%.location
                if (name_start < name_end && field_start < field_end && name_start >= 0 && field_start >= 0)
                {   // okay, looks like a macro
                    // extract the name(sword) and field(location)
                    string name = temp.Substring(name_start + 1, (name_end - name_start) - 1);      // extract the name
                    string field = temp.Substring(field_start + 1, (field_end - field_start));      // extract the field
                    temp = temp.Replace(string.Format("%{0}%.{1}", name, field), "{0}");            // format string preserveing head and tail characters
                    temp = temp.Replace("\r", "%");                                                 // unescape literal '%'

                    // tell the user what we think we are dealing with
                    match = name;

                    if (name == "npc")
                    {
                        if (field == "location")
                        {
                            list.Add(string.Format(temp, GetLocation(this.Location, this.Map)));
                        }
                        else if (field == "name")
                        {
                            list.Add(string.Format(temp, this.Name));
                        }
                        else
                            return ExpansionStatus.BadField;
                    }
                    else if (name == "pc")
                    {
                        if (field == "location")
                        {
                            list.Add(string.Format(temp, GetLocation(m_From.Location, m_From.Map)));
                        }
                        else if (field == "name")
                        {
                            list.Add(string.Format(temp, m_From.Name));
                        }
                        else
                            return ExpansionStatus.BadField;
                    }
                    else
                    {
                        // did we ever know of this item?
                        if (Lookup(name) == null)
                            return ExpansionStatus.Unknown;

                        // okay, we know of this item. See if we have given any out
                        Item item;

                        // look for an entry in our database that does not exist in our inventory
                        if ((item = Lookup(name, false)) == null)
                            // we've not given any out
                            return ExpansionStatus.HaveAll;

                        if (field == "location")
                        {
                            list.Add(string.Format(temp, GetItemLocation(item)));

                            // warn the player that they are being tracked
                            if (item.RootParent is PlayerMobile)
                            {
                                PlayerMobile pm = item.RootParent as PlayerMobile;
                                if (pm.Map != Map.Internal)
                                {
                                    string realName = item.Name;
                                    if (item.Name == null || item.Name.Length == 0)
                                        realName = item.ItemData.Name;

                                    pm.SendMessage("The {0} ({1}) you carry is being used to track you!", name, realName);
                                }
                            }
                        }
                        else
                            return ExpansionStatus.BadField;
                    }
                }
                else
                    list.Add(tokens[ix]);
            }

            tokens = list.ToArray();
            return ExpansionStatus.Okay;
        }

        private Item Lookup(string name, bool exists)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                if (GetField(kvp.Value, Field.Track) != null && GetField(kvp.Value, Field.Name) as string == name)
                {   // deleted of freeze dried.
                    if (kvp.Key.Deleted)
                        continue;

                    // return it if it's the state (exists) we want
                    if (kvp.Key.RootParent == this == exists)
                        return kvp.Key;
                }

            return null;
        }

        private Item Lookup(string name)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                if (GetField(kvp.Value, Field.Track) != null && GetField(kvp.Value, Field.Name) as string == name)
                    return kvp.Key;

            return null;
        }

        protected bool AdminKeyword(object[] tokens, int offset, out string found)
        {
            found = "";
            if (tokens.Length > offset && tokens[offset] is string)
            {
                // look at our current set of keywords skipping to offest (ignore 'keywords' etc.)
                for (int mx = offset; mx < tokens.Length; mx++)
                {
                    string s = tokens[mx] as string;
                    if (s == "onenter" || s == "onreceive")
                    {
                        // found it
                        found = s;
                        return true;
                    }
                }
            }

            // null unit
            return false;
        }

        // build a string suitable for display
        protected bool FindKeywordAliases(string keyword, out string found)
        {
            found = "";
            string[] found_array;

            bool result = FindKeywordAliases(keyword, out found_array);

            if (result)
                foreach (string sx in found_array)
                {
                    if (found.Length > 0)
                        found += ", ";
                    found += sx;
                }

            // we found keyword + N aliases
            return result;
        }

        protected bool FindKeywordAliases(string keyword, out string[] found)
        {
            found = new string[0];

            if (m_KeywordDatabase.ContainsKey(keyword) == false)
                return false;

            List<string> list = new List<string>();
            list.Add(keyword);
            object[] actions = m_KeywordDatabase[keyword];

            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
                if (kvp.Key != keyword && kvp.Value == actions)
                    list.Add(kvp.Key);

            found = list.ToArray();

            // we found keyword + N aliases
            return found.Length > 0;
        }

        protected bool FindKeyPhrase(object[] tokens, int offset, out string found)
        {
            found = null;

            // check each keyword entry for a special dotted keyword phrase
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {   // does it even look like a key phrase?
                if (kvp.Key.IndexOf('.') != -1)
                {   // we may have something
                    string[] temp = kvp.Key.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length <= 1)
                        continue;           // must be at least two keywords
                    for (int ix = 0; ix < temp.Length; ix++)
                    {
                        if (MatchPhraseKey(tokens, offset, temp[ix]))
                        {
                            if (ix == temp.Length - 1)
                            {
                                found = kvp.Key;    // the match
                                return true;        // we have matched all terms of this key phrase
                            }
                        }
                        else
                            break;          // oops, no match
                    }
                }
            }

            // not found
            return false;
        }

        protected bool MatchPhraseKey(object[] tokens, int offset, string match)
        {
            if (tokens.Length > offset && tokens[offset] is string)
            {
                for (int ix = offset; ix < tokens.Length; ix++)
                {
                    if (tokens[ix] is string)
                    {
                        // remove punctuation from token as "hello!" should match the keyword "hello"
                        string kword = tokens[ix] as string;
                        int ndx;
                        char[] delims = new char[] { '.', ',', '!' };
                        while ((ndx = kword.IndexOfAny(delims)) != -1)
                            kword = kword.Remove(ndx, 1);

                        // okay, we have a clean keyword, look it up
                        if (kword == match)
                            return true;
                    }
                    if (tokens[ix] is Char && (Char)tokens[ix] == '|')
                    {   // we're done .. this ('|') starts the action
                        return false;
                    }
                }
            }

            // not found
            return false;
        }

        protected bool FindKeyword(object[] tokens, int offset, out string found)
        {
            found = null;
            if (tokens.Length > offset && tokens[offset] is string)
            {
                for (int ix = offset; ix < tokens.Length; ix++)
                {
                    if (tokens[ix] is string)
                    {
                        // remove punctuation from token as "hello!" should match the keyword "hello"
                        string kword = tokens[ix] as string;
                        int ndx;
                        char[] delims = new char[] { '.', ',', '!' };
                        while ((ndx = kword.IndexOfAny(delims)) != -1)
                            kword = kword.Remove(ndx, 1);

                        // okay, we have a clean keyword, look it up
                        if (m_KeywordDatabase.ContainsKey(kword))
                        {
                            found = kword;
                            return true;
                        }
                    }
                    if (tokens[ix] is Char && (Char)tokens[ix] == '|')
                    {   // we're done .. this ('|') starts the action
                        return false;
                    }
                }
            }

            // not found
            return false;
        }

        protected bool ComputeKeywords(object[] tokens, int offset, bool defined, out string good, out string bad)
        {
            good = "";
            bad = "";
            string[] good_array;
            string[] bad_array;
            bool result = ComputeKeywords(tokens, offset, defined, out good_array, out bad_array);

            foreach (string gx in good_array)
            {
                if (good.Length > 0)
                    good += ", ";
                good += gx;
            }

            foreach (string bx in bad_array)
            {
                if (bad.Length > 0)
                    bad += ", ";
                bad += bx;
            }

            // true if all good (nothing bad)
            return result;
        }

        protected bool ComputeKeywords(object[] tokens, int offset, bool defined, out string[] good, out string[] bad)
        {
            good = new string[0];
            bad = new string[0];

            List<string> good_list = new List<string>();
            List<string> bad_list = new List<string>();

            if (tokens.Length > offset)
            {
                for (int ix = offset; ix < tokens.Length; ix++)
                {
                    // check for end of keywords
                    if (tokens[ix] is Char && (Char)tokens[ix] == '|')
                        break;

                    // probably a comma
                    if (tokens[ix] is string == false)
                        continue;

                    // do we know about this keyword?
                    if (m_KeywordDatabase.ContainsKey(tokens[ix] as string))
                    {   // the key word is known
                        if (defined == true)
                            good_list.Add(tokens[ix] as string);    // known and should be known
                        else
                            bad_list.Add(tokens[ix] as string);     // known and should not be known
                    }
                    else
                    {
                        if (defined == true)
                            bad_list.Add(tokens[ix] as string);     // not known and should be known
                        else
                            good_list.Add(tokens[ix] as string);    // not known and should not be known
                    }
                }
            }

            good = good_list.ToArray();
            bad = bad_list.ToArray();

            // true if all good (nothing bad)
            return !(bad.Length > 0);
        }

        protected object[] Compile(string input)
        {
            // compile the string into an array of objects
            List<object> list = new List<object>();
            string current = "";
            foreach (Char ch in input)
            {
                switch (ch)
                {
                    case '|':
                    case ',':
                    case ' ':
                        // add the current string.
                        if (current.Length > 0)
                        {
                            list.Add(current);
                            current = "";
                        }

                        // ignore white
                        if (ch == ' ')
                            continue;

                        // add the delimiter
                        list.Add(ch);
                        continue;

                    default:
                        current += ch;
                        continue;
                }
            }

            // add the tail string
            if (current.Length > 0)
            {
                list.Add(current);
                current = "";
            }

            return list.ToArray();
        }

        // arraylist to get all items in vendor backpack used for destroying vendors
        protected ArrayList GetItems()
        {
            ArrayList list = new ArrayList();

            foreach (Item item in this.Items)
            {
                if (item.Movable && item != this.Backpack)
                    list.Add(item);
            }

            if (this.Backpack != null)
            {
                list.AddRange(this.Backpack.Items);
            }

            return list;
        }

        public virtual void Destroy(bool toBackpack)
        {
            Item shoes = this.FindItemOnLayer(Layer.Shoes);

            if (shoes is Sandals)
                shoes.Hue = 0;

            ArrayList list = GetItems();

            // don't drop stuff owned by an administrator
            if (StaffOwned == false)
                if (list.Count > 0) // if you have items
                {
                    if (toBackpack && this.Map != Map.Internal) // Move to backpack
                    {
                        Container backpack = new Backpack();

                        foreach (Item item in list)
                        {
                            if (item.Movable != false) // only drop items which are moveable
                                backpack.DropItem(item);
                        }

                        backpack.MoveToWorld(this.Location, this.Map);
                    }
                }

            Delete();
        }

        public void Dismiss(Mobile from)
        {
            Container pack = this.Backpack;

            if (pack != null && pack.Items.Count > 0)
            {
                SayTo(from, 503229); // Thou canst replace me until thy removest all the item from my stock.
                return;
            }

            Destroy(pack != null);
        }

        private void NonLocalDropCallback(object state)
        {
            object[] aState = (object[])state;

            Mobile from = (Mobile)aState[0];
            Item item = (Item)aState[1];

            OnItemGiven(from, item);
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            if (IsOwner(from) && from.InRange(this, 3) && from.InLOS(this))
                return true;

            return base.AllowEquipFrom(from);
        }

        private void OnItemGiven(Mobile from, Item item)
        {   // see if it already has a name
            if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Name) != null)
            {
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Okay.", from.NetState);
                return;
            }
            from.Prompt = new LabelItemPrompt(this, item);
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "What would you like to name this item?", from.NetState);
        }

        // label database
        private Dictionary<Item, object[]> m_ItemDatabase = new Dictionary<Item, object[]>();
        public void LabelItem(Mobile from, Item item, string text)
        {
            if (m_ItemDatabase.Count >= 256)
            {   // hard stop
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Your label database is full."), from.NetState);
                return;
            }

            // does it already have a label?
            if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Name) != null)
                return;

            if (text == null || text.Length == 0)
            {
                if (item.Name != null && item.Name.Length > 0)
                    m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, item.Name);
                else if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Name) != null)
                    m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, GetField(m_ItemDatabase[item], Field.Name));
                else
                    m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, item.ItemData.Name);
            }
            else
                m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, text);

            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
        }

        private object[] AppendField(object[] tokens, Field field, object value)
        {
            List<object> list = new List<object>();

            int skip = 0;
            switch (field)
            {   // skilling fields is how to update an existing field. I.e., we never copy over the old field
                case Field.Name: skip = 1; break;
                case Field.Track: skip = 0; break;
            }

            if (tokens != null)
                for (int ix = 0; ix < tokens.Length; ix++)
                {
                    if (tokens[ix] is Field && (Field)tokens[ix] == field)
                    {
                        ix += skip;
                        continue;
                    }
                    list.Add(tokens[ix]);
                }

            list.Add(field);
            if (value != null)
                list.Add(value);

            return list.ToArray();
        }

        public bool TrackItem(Mobile from, Item item)
        {
            if (m_ItemDatabase.Count >= 256)
            {   // hard stop
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Your label database is full."), from.NetState);
                return false;
            }

            if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Track) != null)
            {   // we're already tracking this item
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I am already tracking this item."), from.NetState);
                return true;
            }

            // add the 'tracking' field
            m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Track, null);

            if (GetField(m_ItemDatabase[item], Field.Name) == null)
                OnItemGiven(from, item);
            else
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
            return true;
        }

        public class LabelItemPrompt : Prompt
        {
            private QuestGiver m_QuestGiver;
            private Item m_item;

            public LabelItemPrompt(QuestGiver questGiver, Item item)
            {
                m_QuestGiver = questGiver;
                m_item = item;
            }

            public override void OnCancel(Mobile from)
            {
                OnResponse(from, "");
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 50)
                    text = text.Substring(0, 50);

                m_QuestGiver.LabelItem(from, m_item, text);
            }
        }

        public class OnReceiveTarget : Target
        {
            private QuestGiver m_QuestGiver;

            public OnReceiveTarget(QuestGiver questGiver)
                : base(15, false, TargetFlags.None)
            {
                m_QuestGiver = questGiver;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Item)
                    m_QuestGiver.TrackItem(from, targ as Item);
                else
                    m_QuestGiver.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("That is not a valid item."), from.NetState);
                return;
            }
        }
    }
}

namespace Server.Items
{
    public class QuestGiverContract : Item
    {
        [Constructable]
        public QuestGiverContract()
            : base(0x14F0)
        {
            Name = "a quest giver contract";
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public QuestGiverContract(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); //version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.SendLocalizedMessage(503248); // Your godly powers allow you to place this vendor whereever you wish.

                Mobile v = new QuestGiver(from);
                v.Direction = from.Direction & Direction.Mask;
                v.MoveToWorld(from.Location, from.Map);
                this.Delete();
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house == null || !house.IsOwner(from))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You are not the full owner of this house.");
                }
                else if (!CanPlaceNewQuestGiver(house.Region))
                {
                    from.SendMessage("You may not add any more quest givers to this house.");
                }
                else
                {
                    Mobile v = new QuestGiver(from);
                    v.Direction = from.Direction & Direction.Mask;
                    v.MoveToWorld(from.Location, from.Map);
                    this.Delete();
                }
            }
        }

        public bool CanPlaceNewQuestGiver(Region region)
        {
            if (region == null)
                return false;

            // 7 quest givers
            // keyword database is 256 rows if 768 characters
            // so a single user can control 1,376,256 bytes of memory
            int avail = 7;
            foreach (Mobile mx in region.Mobiles.Values)
            {
                if (avail <= 0)
                    break;

                if (mx is QuestGiver)
                    --avail;
            }

            return (avail > 0);
        }
    }
}
