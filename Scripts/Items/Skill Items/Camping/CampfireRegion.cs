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

/* Items/Skill Items/Camping/CampfireRegion.cs
 * CHANGELOG:
 *	10/22/04 - Pix
 *		Changed camping to not use campfireregion.
 *		#if'd out class so it won't be used anymore.
 *	9/11/04, Pixie
 *		Removed the override of the Contains() method - it was causing all sorts of grief.
 *		Changed GetLogoutDelay to require a bedroll and waiting for the camp to be secure.
 *	7/25/04, Pixie
 *		Added all the other override functions so that the campingregion calls all the functions for the region
 *		the campingregion was created in.
 *	7/15/04, Pixie
 *		Made it so spells and lightlevels use behavior for the region that the campfire is created in
 *	5/10/04, Pixie
 *		Initial working revision
 */

#if false

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
using Server;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections;


namespace Server.Regions
{
	public class CampfireRegion : Region
	{
		private Campfire m_Campfire;
		private Region m_ExistingRegion;

		public Region SurroundingRegion
		{
			get
			{
				return m_ExistingRegion;
			}
		}

		public CampfireRegion( Campfire campfire, Region existingRegion ) : base( "", "", campfire.Map )
		{
			Priority = Region.HousePriority;
			LoadFromXml = false;
			m_Campfire = campfire;
			m_ExistingRegion = existingRegion;
		}

		public override TimeSpan GetLogoutDelay( Mobile m )
		{
			if ( m == m_Campfire.Camper )
			{
				if( m_Campfire.CampSecure )
				{
					if( m_Campfire.OwnerUsedBedroll )
					{
						m_Campfire.OwnerUsedBedroll = false;
						return TimeSpan.Zero;
					}
				}
			}

			
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.GetLogoutDelay( m );
			}
			else
			{
				return base.GetLogoutDelay( m );
			}
		}

		public override void OnEnter( Mobile m )
		{
			if ( m == m_Campfire.Camper )
				m_Campfire.RestartSecureTimer();
		}

		public override void OnExit( Mobile m )
		{
			if ( m == m_Campfire.Camper )
				m_Campfire.StopSecureTimer();
		}

		public Campfire Campfire
		{
			get
			{
				return m_Campfire;
			}
		}

		public override bool OnBeginSpellCast( Mobile m, ISpell s )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnBeginSpellCast(m,s);
			}
			else
			{
				return base.OnBeginSpellCast(m,s);
			}
		}

		public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.AlterLightLevel(m,ref global, ref personal);
			}
			else
			{
				base.AlterLightLevel(m,ref global, ref personal);
			}
		}

		public override bool AllowBenificial( Mobile from, Mobile target )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.AllowBenificial(from, target);
			}
			else
			{
				return base.AllowBenificial(from, target);
			}
		}

		public override bool AllowHarmful( Mobile from, Mobile target )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.AllowHarmful(from, target);
			}
			else
			{
				return base.AllowHarmful(from, target);
			}
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.AllowHousing(from, p);
			}
			else
			{
				return base.AllowHousing(from, p);
			}
		}

		public override bool AllowSpawn()
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.AllowSpawn();
			}
			else
			{
				return base.AllowSpawn();
			}
		}

		public override bool CanUseStuckMenu( Mobile m )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.CanUseStuckMenu(m);
			}
			else
			{
				return base.CanUseStuckMenu(m);
			}
		}

		public override bool CheckAccessibility( Item item, Mobile from )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.CheckAccessibility(item, from);
			}
			else
			{
				return base.CheckAccessibility(item, from);
			}
		}

/*
 * Pix: 9/11/04: This method should NOT be overridded.  It's only for if
 * the region contains the point - the base class Contains() does this fine.
		public override bool Contains( Point3D p )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.Contains(p);
			}
			else
			{
				return base.Contains(p);
			}
		}
*/

		public override bool IsInInn( Point3D p )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.IsInInn(p);
			}
			else
			{
				return base.IsInInn(p);
			}
		}

		public override void MakeGuard( Mobile focus )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.MakeGuard(focus);
			}
			else
			{
				base.MakeGuard(focus);
			}
		}

		public override void OnAggressed( Mobile aggressor, Mobile aggressed, bool criminal )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnAggressed(aggressor, aggressed, criminal);
			}
			else
			{
				base.OnAggressed(aggressor, aggressed, criminal);
			}
		}

		public override void OnBenificialAction( Mobile helper, Mobile target )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnBenificialAction(helper, target);
			}
			else
			{
				base.OnBenificialAction(helper, target);
			}
		}
		public override bool OnCombatantChange( Mobile m, Mobile Old, Mobile New )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnCombatantChange(m,Old,New);
			}
			else
			{
				return base.OnCombatantChange(m,Old,New);
			}
		}

		public override void OnCriminalAction( Mobile m, bool message )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnCriminalAction(m,message);
			}
			else
			{
				base.OnCriminalAction(m,message);
			}
		}

		public override bool OnDamage( Mobile m, ref int Damage )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnDamage(m,ref Damage);
			}
			else
			{
				return base.OnDamage(m,ref Damage);
			}
		}

		public override bool OnDeath( Mobile m )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnDeath(m);
			}
			else
			{
				return base.OnDeath(m);
			}
		}

		public override bool OnDecay( Item item )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnDecay(item);
			}
			else
			{
				return base.OnDecay(item);
			}
		}

		public override void OnDidHarmful( Mobile harmer, Mobile harmed )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnDidHarmful(harmer, harmed);
			}
			else
			{
				base.OnDidHarmful(harmer, harmed);
			}
		}

		public override bool OnDoubleClick( Mobile m, object o )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnDoubleClick(m,o);
			}
			else
			{
				return base.OnDoubleClick(m,o);
			}
		}

		public override void OnGotBenificialAction( Mobile helper, Mobile target )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnGotBenificialAction(helper, target);
			}
			else
			{
				base.OnGotBenificialAction(helper, target);
			}
		}

		public override void OnGotHarmful( Mobile harmer, Mobile harmed )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnGotHarmful(harmer, harmed);
			}
			else
			{
				base.OnGotHarmful(harmer, harmed);
			}
		}

		public override bool OnHeal( Mobile m, ref int Heal )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnHeal(m,ref Heal);
			}
			else
			{
				return base.OnHeal(m,ref Heal);
			}
		}

		public override bool OnResurrect( Mobile m )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnResurrect(m);
			}
			else
			{
				return base.OnResurrect(m);
			}
		}

		public override bool OnSingleClick( Mobile m, object o )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnSingleClick(m,o);
			}
			else
			{
				return base.OnSingleClick(m,o);
			}
		}

		public override bool OnSkillUse( Mobile m, int Skill )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnSkillUse(m,Skill);
			}
			else
			{
				return base.OnSkillUse(m,Skill);
			}
		}

		public override void OnSpeech( SpeechEventArgs args )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnSpeech(args);
			}
			else
			{
				base.OnSpeech(args);
			}
		}

		public override void OnSpellCast( Mobile m, ISpell s )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.OnSpellCast(m,s);
			}
			else
			{
				base.OnSpellCast(m,s);
			}
		}

		public override bool OnTarget( Mobile m, Target t, object o )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.OnTarget(m,t,o);
			}
			else
			{
				return base.OnTarget(m,t,o);
			}
		}

		public override bool SendInaccessibleMessage( Item item, Mobile from )
		{
			if( m_ExistingRegion != null )
			{
				return m_ExistingRegion.SendInaccessibleMessage(item, from);
			}
			else
			{
				return base.SendInaccessibleMessage(item, from);
			}
		}

		public override void SpellDamageScalar( Mobile caster, Mobile target, ref double damage )
		{
			if( m_ExistingRegion != null )
			{
				m_ExistingRegion.SpellDamageScalar(caster, target, ref damage);
			}
			else
			{
				base.SpellDamageScalar(caster, target, ref damage);
			}
		}

	}
}

#endif
