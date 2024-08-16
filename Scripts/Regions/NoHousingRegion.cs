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

#if OLD_REGIONS


/* Scripts/Regions/NoHousingRegion.cs
 * CHANGELOG
 *	04/24/09, plasma
 *		Commented out all regions, replaced with DRDT
 *	?? unknown
 *		Added "Yew Orc Fort Small Area" as a no housing region
 *	3/11/04: Pixie
 *		Added Ocllo Island no housing region.
 *	3/6/05, Adam
 *		Add "Shame Entrance", "Ice Entrance", "Hythloth Entrance", 
 * 			"Destard Entrance", "Deceit Entrance"
 *	6/15/04, Pixie
 *		Added "Moongate Houseblockers" region to stop people
 *		from placing right next to a moongate.
 */

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
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Spells;

namespace Server.Regions
{
	public class NoHousingRegion : Region
	{
		public static void Initialize()
		{
			/* The first parameter is a boolean value:
			 *  - False: this uses 'stupid OSI' house placement checking: part of the house may be placed here provided that the center is not in the region
			 *  -  True: this uses 'smart RunUO' house placement checking: no part of the house may be in the region
			 */
																																 /*
			Region.AddRegion( new NoHousingRegion( false, "", "Britain Graveyard", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Wrong Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Covetous Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Despise Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Shame Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Ice Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Hythloth Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Destard Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Deceit Entrance", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Despise Passage", Map.Felucca ) );
			Region.AddRegion( new NoHousingRegion( false, "", "Jhelom Islands", Map.Felucca ) );

			Region.AddRegion( new NoHousingRegion( true, "", "Moongate Houseblockers", Map.Felucca ) );

			Region.AddRegion( new NoHousingRegion( true, "", "Ocllo Island", Map.Felucca ) );

			Region.AddRegion( new NoHousingRegion( false, "", "Yew Orc Fort Small Area", Map.Felucca ) );
																																	 */
//			Region.AddRegion( new NoHousingRegion( false, "", "Britain Graveyard", Map.Trammel ) );
//			Region.AddRegion( new NoHousingRegion( false, "", "Wrong Entrance", Map.Trammel ) );
//			Region.AddRegion( new NoHousingRegion( false, "", "Covetous Entrance", Map.Trammel ) );
//			Region.AddRegion( new NoHousingRegion( false, "", "Despise Entrance", Map.Trammel ) );
//			Region.AddRegion( new NoHousingRegion( false, "", "Despise Passage", Map.Trammel ) );
//			Region.AddRegion( new NoHousingRegion( false, "", "Jhelom Islands", Map.Trammel ) );
//			Region.AddRegion( new NoHousingRegion(  true, "", "Haven Island", Map.Trammel ) );
//
//			Region.AddRegion( new NoHousingRegion( false, "", "Crystal Cave Entrance", Map.Malas ) );
//			Region.AddRegion( new NoHousingRegion(  true, "", "Protected Island", Map.Malas ) );
		}

		private bool m_SmartChecking;

		public bool SmartChecking{ get{ return m_SmartChecking; } }

		public NoHousingRegion( bool smartChecking, string prefix, string name, Map map ) : base( prefix, name, map )
		{
			m_SmartChecking = smartChecking;
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return m_SmartChecking;
		}

		public override void OnEnter( Mobile m )
		{
		}

		public override void OnExit( Mobile m )
		{
		}
	}
}
#endif
