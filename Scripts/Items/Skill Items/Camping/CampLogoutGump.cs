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

/* Items/Skill Items/Camping/CampLogoutGump.cs
 * CHANGELOG:
 *	2/22/11, Adam
 *		This is now obsolete as I've updated the camping system to Run 2.0
 *	8/14/06, weaver
 *		Added null check when handling the bedroll passed (for tent cases).
 *	10/28/04 - Pix
 *		Logout confirmation times out now after 20 seconds.
 *		Moving from the location where you doubleclicked the open bedroll now stops
 *		you from instaloggging.
 *	10/22/04 - Pix
 *		Changed camping to not use campfireregion.
 *	5/10/04, Pixie
 *		Initial working revision
 */

#if obsolete

using System;
using Server;
using System;
using Server;
using System;
using Server;
using Server.SMTP;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Net.Mime;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Misc;
using Server.Scripts.Commands;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using Server.Network;
using DiagELog = System.Diagnostics.EventLog;
using Server.Accounting;
using Server.Guilds;
using System.Net;
using System.Net.Sockets;
using Server.Targeting;
using Server.ContextMenus;
using Server.Gumps;
using Server.HuePickers;
using Server.Menus;
using Server.Prompts;
using System.Xml;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom.Compiler;
using System.Security.Cryptography;
using Server.Commands;
using CPA = Server.CommandPropertyAttribute;
using Server.Regions;
using Server.Spells;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Multis;
using Server.Engines.BulkOrders;
using Server.Engines.RewardSystem;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Targets;
using Server.BountySystem;
using Server.Scripts.Gumps;
using Server.Spells.Seventh;
using Server.Spells.Fourth;
using Server.Engines.Plants;
using Server.Engines.CronScheduler;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Server.Multis.Deeds;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Engines.Quests;
using Server.Spells.Fifth;
using Server.Spells.Sixth;
using Server.Engines.Harvest;
using Server.Engines.Quests.Hag;
using Server.Diagnostics;
using CV = Server.ClientVersion;
using Server.Spells.Third;
using Server.Engines;
using Server.Engines.ChampionSpawn;
using Server.Engines.IOBSystem;
using Mat = Server.Engines.BulkOrders.BulkMaterialType;
using Server.Engines.Craft;
using Server.Engines.Quests.Collector;
using System.Data.Odbc;
using CalcMoves = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;
using Server.PathAlgorithms;
using Server.PathAlgorithms.FastAStar;
using Server.PathAlgorithms.NavStar;
using Server.PathAlgorithms.SlowAStar;
using Server.Engines.ResourcePool;
using System.Drawing;
using System.Drawing.Imaging;
using Server.Township;
using AMA = Server.Items.ArmorMeditationAllowance;
using AMT = Server.Items.ArmorMaterialType;
using Server.Spells.First;
using Server.Spells.Second;
using System.Runtime.Serialization.Formatters.Binary;
using Haven = Server.Engines.Quests.Haven;
using Necro = Server.Engines.Quests.Necro;
using Custom.Gumps;
using ZLR.VM;
using Server.Multis.StaticHousing;
using Server.PathAlgorithms.Sector;
using Server.SkillHandlers;
using Server.Spells.NPC;
using System.Reflection.Emit;
using ZLR.IFF;
using ZLR.VM.Debugging;
using Server.Engines.Quests.Doom;
using Server.Engines.OldSchoolCraft;
using Server.Spells.Ninjitsu;
using Server.Text;
using Server.Ethics;
using Server.Factions.AI;
using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Items;

namespace Server.Gumps
{
	public class CampLogoutGump : Gump
	{
		private Mobile m_From;
		private UnrolledBedroll m_Bedroll;
		private Point3D m_Location;

		public CampLogoutGump(Mobile from, UnrolledBedroll bedroll)
			: base(150, 200)
		{
			m_From = from;
			m_Bedroll = bedroll;
			m_Location = from.Location;

			m_From.CloseGump(typeof(CampLogoutGump));

			AddPage(0);

			AddBackground(0, 0, 400, 200, 5054);

			AddHtml(130, 10, 200, 25, "Logging Out Via Camping", false, false);

			AddHtmlLocalized(40, 40, 320, 100, 1011016, 0, true, true);

			AddHtmlLocalized(70, 175, 140, 25, 1011011, false, false); // CONTINUE
			AddButton(40, 175, 4005, 4007, 2, GumpButtonType.Reply, 0);

			AddHtmlLocalized(160, 175, 140, 25, 1011012, false, false); // CANCEL
			AddButton(130, 175, 4005, 4007, 1, GumpButtonType.Reply, 0);

			new CampLogoutGumpTimeoutTimer(from).Start();
		}

		public override void OnResponse(NetState state, RelayInfo info)
		{
			if (info.ButtonID == 2)
			{
				// wea: 14/Aug/2006 added null check to bedroll (for instances when we log out in tents)
				if (m_Bedroll != null)
				{
					m_Bedroll.Delete();
					state.Mobile.AddToBackpack(new Bedroll());
				}

				if (m_Location != m_From.Location)
				{
					m_From.SendMessage("Moving from the location of the bedroll prohibits logging out via camping.");
				}
				else if (m_From.Criminal)
				{
					m_From.SendMessage("You are criminal, so you cannot logout via camping.");
				}
				else
				{
					//state.Dispose();
					state.Send(new LogoutAck());

					//manually logout: set logout location and logout map and move to internal map
					m_From.LogoutLocation = m_From.Location;
					m_From.LogoutMap = m_From.Map;
					m_From.Map = Map.Internal;
				}
			}
		}

		private class CampLogoutGumpTimeoutTimer : Timer
		{
			private Mobile m_Player;

			public CampLogoutGumpTimeoutTimer(Mobile m)
				: base(TimeSpan.FromSeconds(20.0))
			{
				m_Player = m;
			}

			protected override void OnTick()
			{
				m_Player.CloseGump(typeof(CampLogoutGump));
				Stop();
			}
		}

	}
}

#endif
