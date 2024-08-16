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

/* Scripts/Engines/ResourcePool/AccountBook.cs
 * ChangeLog
 *  04/27/05 TK
 *		Made resources sorted by name so it's easier to find them in book.
 *  02/07/05 TK
 *		Made accountbooks un-copyable.
 *  06/02/05 TK
 *		Removed a few lingering debug WriteLine's
 *	03/02/05 Taran Kain
 *		Created.
 * 
 */

using Server.Engines.ResourcePool;
using System.Collections;

namespace Server.Items
{
    class AccountBook : BaseBook
    {
        //		public override bool Writable { get { return false; } }

        [Constructable]
        public AccountBook()
            : base(0xFF1, "title", "author", 0, false)
        {
            Copyable = false;
        }

        public AccountBook(Serial ser)
            : base(ser)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "an accounting book");
        }

        public override void OnDoubleClick(Mobile from)
        {
            Title = "Accounts";
            Author = from.Name;

            ClearPages();
            ArrayList al = new ArrayList(ResourcePool.Resources.Values);
            al.Sort();
            for (int i = 0; i < al.Count; i++)
            {
                if (al[i] is RDRedirect)
                    continue;
                ResourceData rd = al[i] as ResourceData;

                string inv = rd.DescribeInvestment(from);

                AddLine(rd.Name + ":");
                AddLine(inv);

                if (Pages[Pages.Length - 1].Lines.Length < 8)
                    AddLine("");
            }

            string ttemp = ResourceLogger.GetHistory(from);
            string[] history = ttemp.Split(new char[] { '\n' });

            string line;
            foreach (string trans in history)
            {
                if (trans == "")
                    continue;
                string[] tlines = trans.Split(new char[] { ' ' });
                line = "";
                foreach (string t in tlines)
                {
                    if ((line.Length + t.Length + 1) <= 20)
                        line += t + " ";
                    else
                    {
                        AddLine(line);
                        line = t + " ";
                    }
                }
                AddLine(line);
                AddLine("");
            }

            base.OnDoubleClick(from);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            Copyable = false;
        }
    }
}
