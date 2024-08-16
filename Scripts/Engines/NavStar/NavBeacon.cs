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

/* Scripts/Engines/NavStar/NavBeacon.cs
 * CHANGELOG
 * 11/30/05, Kit
 *		Set Movable to false, added On/Off active switch, set all path values default -1, changed to admin access only
 * 11/18/05, Kit
 * 	Initial Creation
 */

using Server.Engines;
using Server.Gumps;
using Server.Network;
using System;

namespace Server.Items
{

    public class NavBeacon : Item
    {


        private int[,] NavArray;
        private bool m_Running;

        [CommandProperty(AccessLevel.Administrator)]
        public bool Active
        {
            get { return m_Running; }
            set { m_Running = value; }
        }

        public class NavGump : Gump
        {
            private NavBeacon temp;

            public NavGump(NavBeacon nav)
                : base(50, 50)
            {
                temp = nav;
                Closable = true;
                Dragable = true;
                Resizable = false;

                AddPage(0);
                AddBackground(10, 10, 225, 425, 9270);
                AddAlphaRegion(7, 7, 235, 435);

                AddLabel(40, 15, 1152, "Routes");
                AddLabel(165, 15, 1152, "Weight");
                AddButton(91, 398, 247, 248, 1, GumpButtonType.Reply, 0);
                //Okay Button ->  # 1

                int itemsThisPage = 0;
                int nextPageNumber = 1;

                string[] ary = Enum.GetNames(typeof(NavDestinations));

                for (int i = 1; i < ary.Length; i++)
                {
                    if (ary[i] != null)
                    {
                        if (itemsThisPage >= 7 || itemsThisPage == 0)
                        {
                            itemsThisPage = 0;

                            if (nextPageNumber != 1)
                            {
                                AddButton(190, 399, 4005, 4007, 2, GumpButtonType.Page, nextPageNumber);
                                //Forward button -> #2
                            }

                            AddPage(nextPageNumber++);

                            if (nextPageNumber != 2)
                            {
                                AddButton(29, 399, 4014, 4016, 3, GumpButtonType.Page, nextPageNumber - 2);
                                //Back Button -> #3
                            }
                        }
                        AddTextEntry(180, 55 + (45 * itemsThisPage), 1152, 20, 0xFA5, (i + 500), nav.GetWeight(i).ToString());
                        AddLabel(40, 55 + (45 * itemsThisPage), 1152, ary[i]);

                        itemsThisPage++;
                    }
                }

            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                switch (info.ButtonID)
                {
                    case 1:
                        {
                            try
                            {
                                int X1 = 0;
                                Array t = Enum.GetValues(typeof(NavDestinations));
                                for (int i = 1; i < t.Length; i++)
                                {
                                    TextRelay x1 = info.GetTextEntry(i + 500);
                                    X1 = Convert.ToInt32(x1.Text);
                                    temp.SetWeight(i, X1);

                                }
                            }
                            catch
                            {
                                from.SendMessage("Invalid Formating");
                                from.SendGump(new NavGump(temp));
                                break;
                            }
                        }
                        break;
                }
            }
        }

        public int GetLenght()
        {
            return NavArray.Length;
        }

        public int GetWeight(int x)
        {
            return NavArray[x, 0];
        }

        public void SetWeight(int x, int value)
        {
            NavArray[x, 0] = value;
        }

        [Constructable]
        public NavBeacon()
            : base(0x1ECD)
        {
            Name = "NavBeacon";
            Weight = 0.0;
            Hue = 0x47E;
            Visible = false;
            Movable = false;
            Active = true;
            Array t = Enum.GetValues(typeof(NavDestinations));
            //init array and set all defaults to -1
            NavArray = new int[t.Length, 1];
            for (int i = 0; i < NavArray.Length; i++)
            {
                SetWeight(i, -1);
            }


        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Running)
                LabelTo(from, "[Active]");
            else
                LabelTo(from, "[Disabled]");
        }

        public override void OnDoubleClick(Mobile m)
        {

            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.CloseGump(typeof(NavGump));
                m.SendGump(new NavGump(this));
            }

        }


        public NavBeacon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);
            //rev1
            writer.Write(m_Running);

            WriteNavArray(writer, NavArray);


        }

        public static void WriteNavArray(GenericWriter writer, int[,] ba)
        {
            writer.Write(ba.Length);

            for (int i = 0; i < ba.Length; i++)
            {
                writer.Write(ba[i, 0]);
            }
            return;
        }

        public static int[,] ReadNavArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            int[,] newBA = new int[size, 1];

            for (int i = 0; i < size; i++)
            {
                newBA[i, 0] = reader.ReadInt();
            }

            return newBA;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_Running = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        NavArray = ReadNavArray(reader);
                        break;
                    }

            }
            if (version < 1)
            {
                m_Running = false;

            }
        }

    }

}
