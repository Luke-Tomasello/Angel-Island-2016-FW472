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

/* Scripts\Items\Misc\Silver.cs
 * ChangeLog
 *  3/10/11, Adam
 *      ifdef'ed out. Conflicts with engines\faction\silver
 *  12/5/08, Adam
 *		- Added Dupe() override to ensure Silver is properly duplicated (needed for stacking)
 *		- first time checkin
 */

#if old
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

namespace Server.Items
{
	public class Silver : Item
	{
		[Constructable]
		public Silver()
			: this(1)
		{
		}

		[Constructable]
		public Silver(int amountFrom, int amountTo)
			: this(Utility.RandomMinMax(amountFrom, amountTo))
		{
		}

		[Constructable]
		public Silver(int amount)
			: base(0xEF0)
		{
			Stackable = true;
			Weight = 0.02;
			Amount = amount;
		}

		public Silver(Serial serial)
			: base(serial)
		{
		}

		public override int GetDropSound()
		{
			if (Amount <= 1)
				return 0x2E4;
			else if (Amount <= 5)
				return 0x2E5;
			else
				return 0x2E6;
		}

		public override Item Dupe(int amount)
		{
			return base.Dupe(new Silver(amount), amount);
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
		}
	}
}
#endif
