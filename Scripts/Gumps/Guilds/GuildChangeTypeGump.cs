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

/* Scripts/Gumps/Guilds/GuildChangeTypeGump.cs
 * ChangeLog:
 *	7/23/06, Pix
 *		Now disallows kin alignment change when GuildKinChangeDisabled featurebit is set.
 *	4/28/06, Pix
 *		Changes for Kin alignment by guild.
 *  12/14/05, Kit
 *		Added check to prevent special type guilds changeing to 
 *		a differnt special type if allied with opposeing type guilds.
 */
using Server.Guilds;
using Server.Network;
using System;
using System.Collections;

namespace Server.Gumps
{
    public class GuildChangeTypeGump : Gump
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        private string TranslateIOBName(IOBAlignment iob)
        {
            return Engines.IOBSystem.IOBSystem.GetIOBName(iob);
        }

        private bool CanChangeKin()
        {
            return !CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.GuildKinChangeDisabled);
        }

        public GuildChangeTypeGump(Mobile from, Guild guild)
            : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 400, 5054);
            AddBackground(10, 10, 530, 380, 3000);

            AddHtmlLocalized(20, 15, 510, 30, 1013062, false, false); // <center>Change Guild Type Menu</center>

            //SEPARATOR
            AddHtml(20, 75, 300, 300, "Order/Chaos Alignment - Currently " + m_Guild.Type.ToString(), false, false);

            AddButton(20, 110, 4005, 4007, 1, GumpButtonType.Reply, 0);
            //AddHtmlLocalized( 195, 85, 300, 30, 1013063, false, false ); // Standard guild
            AddHtml(55, 110, 300, 30, "Standard", false, false); // Standard guild

            AddButton(180, 110, 4005, 4007, 2, GumpButtonType.Reply, 0);
            //AddItem( 50, 123, 7109 );
            AddHtml(215, 110, 300, 300, "Order", false, false); // Order guild

            AddButton(340, 110, 4005, 4007, 3, GumpButtonType.Reply, 0);
            //AddItem( 45, 160, 7107 );
            AddHtml(375, 110, 300, 300, "Chaos", false, false); // Chaos guild


            //SEPARATOR
            AddHtml(20, 180, 300, 300, "Kin Alignment - Currently " + TranslateIOBName(m_Guild.IOBAlignment), false, false);

            if (CanChangeKin())
            {
                //Unaligned
                AddButton(50, 210, 4005, 4007, 12, GumpButtonType.Reply, 0);
                AddHtml(85, 210, 300, 300, TranslateIOBName(IOBAlignment.None), false, false);
                //Brigand
                AddButton(50, 235, 4005, 4007, 5, GumpButtonType.Reply, 0);
                AddHtml(85, 235, 300, 300, TranslateIOBName(IOBAlignment.Brigand), false, false);
                //Council
                AddButton(50, 260, 4005, 4007, 6, GumpButtonType.Reply, 0);
                AddHtml(85, 260, 300, 300, TranslateIOBName(IOBAlignment.Council), false, false);
                //Good
                AddButton(50, 285, 4005, 4007, 7, GumpButtonType.Reply, 0);
                AddHtml(85, 285, 300, 300, TranslateIOBName(IOBAlignment.Good), false, false);
                //Orcish
                AddButton(300, 210, 4005, 4007, 8, GumpButtonType.Reply, 0);
                AddHtml(335, 210, 300, 300, TranslateIOBName(IOBAlignment.Orcish), false, false);
                //Pirate
                AddButton(300, 235, 4005, 4007, 9, GumpButtonType.Reply, 0);
                AddHtml(335, 235, 300, 300, TranslateIOBName(IOBAlignment.Pirate), false, false);
                //Savage
                AddButton(300, 260, 4005, 4007, 10, GumpButtonType.Reply, 0);
                AddHtml(335, 260, 300, 300, TranslateIOBName(IOBAlignment.Savage), false, false);
                //Undead
                AddButton(300, 285, 4005, 4007, 11, GumpButtonType.Reply, 0);
                AddHtml(335, 285, 300, 300, TranslateIOBName(IOBAlignment.Undead), false, false);
            }
            else
            {
                AddHtml(85, 210, 300, 300, "Kin Alignment change is currently disabled.", false, false);
            }


            //CANCEL!
            AddButton(300, 360, 4005, 4007, 4, GumpButtonType.Reply, 0);
            AddHtmlLocalized(335, 360, 150, 30, 1011012, false, false); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            GuildType newType = m_Guild.Type; //GuildType.Regular;
            IOBAlignment newIOBType = m_Guild.IOBAlignment; //IOBAlignment.None;

            switch (info.ButtonID)
            {
                default: break;//return; // Close
                case 1: newType = GuildType.Regular; break;
                case 2: newType = GuildType.Order; break;
                case 3: newType = GuildType.Chaos; break;
                case 5: newIOBType = IOBAlignment.Brigand; break;
                case 6: newIOBType = IOBAlignment.Council; break;
                case 7: newIOBType = IOBAlignment.Good; break;
                case 8: newIOBType = IOBAlignment.Orcish; break;
                case 9: newIOBType = IOBAlignment.Pirate; break;
                case 10: newIOBType = IOBAlignment.Savage; break;
                case 11: newIOBType = IOBAlignment.Undead; break;
                case 12: newIOBType = IOBAlignment.None; break;
            }

            if (m_Guild.IOBAlignment != newIOBType && CanChangeKin() == false)
            {
                m_Mobile.SendMessage("Guild Kin Alignment change is currently disabled.");
                return;
            }

            if ((m_Guild.Type != newType || m_Guild.IOBAlignment != newIOBType)
                && m_Guild.TypeLastChange.AddDays(7) > DateTime.UtcNow)
            {
                m_Mobile.SendMessage("You can only change your alignment once every 7 days.");
            }
            else
            {
                if (m_Guild.Type != newType)
                {
                    //Changing Order/Chaos

                    //Check that Order/Chaos guilds aren't allied
                    if (newType != GuildType.Regular) //we only care if there changeing to a differnt special type
                    {
                        ArrayList Allies = m_Guild.Allies;

                        for (int i = 0; i < Allies.Count; ++i)
                        {
                            Guild g = (Guild)Allies[i];

                            if (g.Type != GuildType.Regular && g.Type != newType)
                            {
                                m_Mobile.SendMessage("Break any alliances with opposing guild types first");
                                return;
                            }
                        }
                    }

                    //Change the Guild!
                    m_Guild.Type = newType;
                    m_Guild.GuildMessage(1018022, newType.ToString()); // Guild Message: Your guild type has changed:
                }
                else if (m_Guild.IOBAlignment != newIOBType)
                {
                    //Changing KIN

                    //Check that different IOB types aren't allied
                    if (newIOBType != IOBAlignment.None)
                    {
                        ArrayList Allies = m_Guild.Allies;

                        for (int i = 0; i < Allies.Count; ++i)
                        {
                            Guild g = (Guild)Allies[i];

                            if (g.IOBAlignment != IOBAlignment.None && g.IOBAlignment != newIOBType)
                            {
                                m_Mobile.SendMessage("Break any alliances with opposing guild types first");
                                return;
                            }
                        }
                    }

                    //Change the Guild!
                    m_Guild.IOBAlignment = newIOBType;
                    if (m_Guild.IOBAlignment != IOBAlignment.None)
                    {
                        m_Guild.GuildMessage("Your guild is now allied with the " + this.TranslateIOBName(newIOBType));
                    }
                    else
                    {
                        m_Guild.GuildMessage("Your guild has broken its kin alignment, it is now unaligned.");
                    }
                }
                else
                {
                    m_Mobile.SendMessage("You have not changed your alignment.");
                }
            }


            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }
    }
}
