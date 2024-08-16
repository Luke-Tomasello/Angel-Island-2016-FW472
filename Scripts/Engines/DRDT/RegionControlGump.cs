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

/* Scripts/Engines/DRDT/RegionControlGump.cs
 *  05/02/05, Kit
 *	 Added Inn Support and Gumps.
 *  04/30/05, Kitaras
 *	 Added gump to enter regions via x/y 
 */

using Server.Items;
using Server.Network;
using System;

namespace Server.Gumps
{

    public class AreaGump : Gump
    {
        int X1, X2, Y1, Y2;
        RegionControl r;
        Rectangle2D newarea;

        public AreaGump(RegionControl b)
            : base(25, 300)
        {

            r = (RegionControl)b;
            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(23, 32, 412, 256, 9270);
            AddAlphaRegion(19, 29, 418, 263);

            AddLabel(116, 50, 1152, "Enter Corridanates for new region");

            AddTextEntry(140, 77 + 25, 1152, 20, 0xFA5, 0, "0");
            AddTextEntry(140, 77 + 50, 1152, 20, 0xFA5, 1, "0");
            AddTextEntry(140, 77 + 75, 1152, 20, 0xFA5, 2, "0");
            AddTextEntry(140, 77 + 100, 1152, 20, 0xFA5, 3, "0");

            AddLabel(105, 77 + 25, 1152, "X1:");
            AddLabel(105, 77 + 50, 1152, "Y1:");
            AddLabel(105, 77 + 75, 1152, "X2:");
            AddLabel(105, 77 + 100, 1152, "Y2:");

            AddButton(105, 77 + 125, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(140, 77 + 125, 90, 80, 1046362, false, false); // Yes
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        try
                        {
                            TextRelay x1 = info.GetTextEntry(0);
                            X1 = Convert.ToInt32(x1.Text);
                            TextRelay y1 = info.GetTextEntry(1);
                            Y1 = Convert.ToInt32(y1.Text);
                            TextRelay x2 = info.GetTextEntry(2);
                            X2 = Convert.ToInt32(x2.Text);
                            TextRelay y2 = info.GetTextEntry(3);
                            Y2 = Convert.ToInt32(y2.Text);
                        }
                        catch
                        {
                            from.SendMessage("Invalid Formating");
                            from.SendGump(new AreaGump(r));
                            break;
                        }
                        //if input okay create area
                        newarea = new Rectangle2D(X1, Y1, X2 - X1, Y2 - Y1);
                        r.EnterArea(from, from.Map, newarea, r);
                        break;
                    }


            }
        }

    }

    public class InnGump : Gump
    {
        int X1, X2, Y1, Y2;
        RegionControl r;
        Rectangle2D newarea;

        public InnGump(RegionControl b)
            : base(25, 300)
        {

            r = (RegionControl)b;
            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(23, 32, 412, 256, 9270);
            AddAlphaRegion(19, 29, 418, 263);

            AddLabel(116, 50, 1152, "Enter Corridanates for new Inn");

            AddTextEntry(140, 77 + 25, 1152, 20, 0xFA5, 0, "0");
            AddTextEntry(140, 77 + 50, 1152, 20, 0xFA5, 1, "0");
            AddTextEntry(140, 77 + 75, 1152, 20, 0xFA5, 2, "0");
            AddTextEntry(140, 77 + 100, 1152, 20, 0xFA5, 3, "0");

            AddLabel(105, 77 + 25, 1152, "X1:");
            AddLabel(105, 77 + 50, 1152, "Y1:");
            AddLabel(105, 77 + 75, 1152, "X2:");
            AddLabel(105, 77 + 100, 1152, "Y2:");

            AddButton(105, 77 + 125, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(140, 77 + 125, 90, 80, 1046362, false, false); // Yes
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        try
                        {
                            TextRelay x1 = info.GetTextEntry(0);
                            X1 = Convert.ToInt32(x1.Text);
                            TextRelay y1 = info.GetTextEntry(1);
                            Y1 = Convert.ToInt32(y1.Text);
                            TextRelay x2 = info.GetTextEntry(2);
                            X2 = Convert.ToInt32(x2.Text);
                            TextRelay y2 = info.GetTextEntry(3);
                            Y2 = Convert.ToInt32(y2.Text);
                        }
                        catch
                        {
                            from.SendMessage("Invalid Formating");
                            from.SendGump(new InnGump(r));
                            break;
                        }
                        //if input okay create area
                        newarea = new Rectangle2D(X1, Y1, X2 - X1, Y2 - Y1);
                        r.EnterInnArea(from, from.Map, newarea, r);
                        break;
                    }


            }
        }

    }
    public class RegionControlGump : Gump
    {
        RegionControl m_Controller;
        public RegionControlGump(RegionControl r)
            : base(25, 25)
        {
            m_Controller = r;

            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(23, 32, 590, 270, 9270);
            AddAlphaRegion(19, 29, 590, 280);

            AddButton(55, 46, 5569, 5570, (int)Buttons.SpellButton, GumpButtonType.Reply, 0);
            AddButton(345, 46, 5581, 5582, (int)Buttons.SkillButton, GumpButtonType.Reply, 0);
            AddButton(50, 128, 7006, 7006, (int)Buttons.AreaButton, GumpButtonType.Reply, 0);
            AddButton(50, 205, 7006, 7006, (int)Buttons.EnterAreaButton, GumpButtonType.Reply, 0);
            AddButton(340, 128, 7006, 7006, (int)Buttons.AreaInnButton, GumpButtonType.Reply, 0);
            AddButton(340, 205, 7006, 7006, (int)Buttons.EnterInnAreaButton, GumpButtonType.Reply, 0);

            AddLabel(152, 70, 1152, "Edit Restricted Spells");
            AddLabel(452, 70, 1152, "Edit Restricted Skills");
            AddLabel(152, 157, 1152, "Add Region Area");
            AddLabel(152, 235, 1152, "Enter New Region X/Y");
            AddLabel(452, 157, 1152, "Add Inn Area");
            AddLabel(452, 235, 1152, "Enter New Inn X/Y");
            AddImage(353, 54, 3953);
            AddImage(353, 180, 3955);

        }

        public enum Buttons
        {
            SpellButton = 1,
            SkillButton,
            AreaButton,
            EnterAreaButton,
            AreaInnButton,
            EnterInnAreaButton
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Controller == null || m_Controller.Deleted)
                return;

            Mobile m = sender.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        //m_Controller.SendRestrictGump( m, RestrictType.Spells );
                        m.CloseGump(typeof(SpellRestrictGump));
                        m.SendGump(new SpellRestrictGump(m_Controller.RestrictedSpells));

                        m.CloseGump(typeof(RegionControlGump));
                        m.SendGump(new RegionControlGump(m_Controller));
                        break;
                    }
                case 2:
                    {
                        //m_Controller.SendRestrictGump( m, RestrictType.Skills );

                        m.CloseGump(typeof(SkillRestrictGump));
                        m.SendGump(new SkillRestrictGump(m_Controller.RestrictedSkills));

                        m.CloseGump(typeof(RegionControlGump));
                        m.SendGump(new RegionControlGump(m_Controller));
                        break;
                    }
                case 3:
                    {
                        m.CloseGump(typeof(RegionControlGump));
                        m.SendGump(new RegionControlGump(m_Controller));

                        m.CloseGump(typeof(RemoveAreaGump));

                        m.SendGump(new RemoveAreaGump(m_Controller));
                        m.CloseGump(typeof(RemoveInnGump));

                        m.SendGump(new RemoveInnGump(m_Controller));

                        m_Controller.ChooseArea(m);
                        break;
                    }
                case 4:
                    {
                        m.CloseGump(typeof(RegionControlGump));
                        m.SendGump(new RegionControlGump(m_Controller));

                        m.CloseGump(typeof(RemoveAreaGump));
                        m.SendGump(new RemoveAreaGump(m_Controller));

                        m.CloseGump(typeof(RemoveInnGump));

                        m.SendGump(new RemoveInnGump(m_Controller));

                        m.SendGump(new AreaGump(m_Controller));
                        break;
                    }
                case 5:
                    {
                        m.CloseGump(typeof(RegionControlGump));
                        m.SendGump(new RegionControlGump(m_Controller));

                        m.CloseGump(typeof(RemoveAreaGump));

                        m.SendGump(new RemoveAreaGump(m_Controller));

                        m.CloseGump(typeof(RemoveInnGump));

                        m.SendGump(new RemoveInnGump(m_Controller));

                        m_Controller.ChooseInnArea(m);
                        break;
                    }
                case 6:
                    {
                        m.CloseGump(typeof(RegionControlGump));
                        m.SendGump(new RegionControlGump(m_Controller));

                        m.CloseGump(typeof(RemoveAreaGump));
                        m.SendGump(new RemoveAreaGump(m_Controller));

                        m.CloseGump(typeof(RemoveInnGump));

                        m.SendGump(new RemoveInnGump(m_Controller));

                        m.SendGump(new InnGump(m_Controller));
                        break;
                    }
            }
        }
    }
}
