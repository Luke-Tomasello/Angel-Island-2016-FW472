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


/* Scripts/Regions/Felucca/Dungeon.cs
 * ChangeLog
 *	04/24/09, plasma
 *		Commented out all regions, replaced with DRDT
 *	9/21/05, Adam
 *		Remove Wind and Deceit as they are controlled by DRDT
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/11/04, mith
 *		Moved Wind from Town.cs to Dungeon.cs, this removes guards and prevents recall, mark, and gate.
 */

using System;
using Server;
using Server.SMTP;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Net.Mime;
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

namespace Server.Regions
{
	public class FeluccaDungeon : Region
	{
		public static void Initialize()
		{
			/*
			Region.AddRegion( new FeluccaDungeon( "Covetous" ) );
			Region.AddRegion( new FeluccaDungeon( "Despise" ) );
			Region.AddRegion( new FeluccaDungeon( "Destard" ) );
			Region.AddRegion( new FeluccaDungeon( "Hythloth" ) );
			Region.AddRegion( new FeluccaDungeon( "Shame" ) );
			Region.AddRegion( new FeluccaDungeon( "Wrong" ) );
			Region.AddRegion( new FeluccaDungeon( "Terathan Keep" ) );
			Region.AddRegion( new FeluccaDungeon( "Fire" ) );
			Region.AddRegion( new FeluccaDungeon( "Ice" ) );
			Region.AddRegion( new FeluccaDungeon( "Orc Cave" ) );
			*/
			// Controlled by DRDT
			//Region.AddRegion( new FeluccaDungeon( "Wind" ) );
			//Region.AddRegion( new FeluccaDungeon( "Deceit" ) );
		}

		public FeluccaDungeon( string name ) : base( "the dungeon", name, Map.Felucca )
		{
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}

		public override void OnEnter( Mobile m )
		{
			//base.OnEnter( m ); // You have entered the dungeon {0}
		}

		public override void OnExit( Mobile m )
		{
			//base.OnExit( m );
		}

		public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
		{
			global = LightCycle.DungeonLevel;
		}

		/*RunUO 1.0RC0 had this commented out*/
		/**/public override bool OnBeginSpellCast( Mobile m, ISpell s )
		{
			if ( s is GateTravelSpell || s is RecallSpell || s is MarkSpell )
			{
				m.SendMessage( "You cannot cast that spell here." );
				return false;
			}
			else
			{
				return base.OnBeginSpellCast( m, s );
			}
		}/**/
	}
}
#endif
