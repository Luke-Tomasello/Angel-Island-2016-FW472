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

/********************************************************
 * TO DO
 * Finish the old style display never finished by Pixie.
 * See BaseClothes for a completed version
 ********************************************************
 */

/* Scripts/Items/Jewels/BaseJewel.cs
 * CHANGE LOG
 * 3/8/2016, Adam
 *		o looks like several magic clothes/jewlery were never tested. Fixing them now:
 *		o essentially all the changes for this date in BaseClothing
 *		o Add expiration and region checks for special event jewelry
 * 5/2/08, Adam
 *		Update use of HideAttributes to be simpler.
 * 5/1/08, Adam
 *		Use HideAttributes filtering to hide the [Exceptional] tag.
 *	01/04/07, Pix
 *		Fixed stat-effect items.
 *	01/02/07, Pix
 *		Stat-effect magic items no longer stack.
 *  06/26/06, Kit
 *		Added region spell checks to all magic jewlery effects, follow region casting rules!!
 *	8/18/05, erlein
 *		Added code necessary to support maker's mark and exceptional chance.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	12/19/04, Adam
 *		1. In SetRandomMagicEffect() change NewMagicType to use explicit Utility.RandomList()
 *		2. In SetRandomMagicEffect() change NewLevel to use Utility.RandomMinMax(MinLevel, MaxLevel)
 *  8/9/04 - Pixie
 *		Explicitly cleaned up timers.
 *	7/25/04 smerX
 *		A new timer is initiated OnAdded
 *  7/6/04 - Pixie
 *		Added cunning, agility, strength, feeblemind, clumsy, weaken, curse, nightsight jewelry
 *	6/25/04 - Pixie
 *		Fixed jewelry so that they didn't spawn outside of the appropriate range
 *		(bracelets were spawning with teleport and rings/bracelets were spawning
 *		as unidentified magic items but when id'ed didn't have a property)
// 05/11/2004 - Pulse
//	Completed changes to implement magic jewelry.
//	changes include:
//		* several new properties: magic type, number of charges, and identified flag
//		* updated GetProperties and OnSingleClick to include magic properties
//		* JewelMagicEffect enumeration for various available spell types
//		* MagicEffectTimer class to implement spell timing effects and control charge usage
//		* All jewelry items can be made magic through the [props command for Game Master or higher access level
//		* SetMagic and SetRandomMagicEffect to allow setting an existing jewelry item to some
//			type of magic and level
//		* "Apply" routines for the various magic effects
//		* an AddStatBonus routine used by the Bless effect.
*/

using Server.Commands;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Sixth;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    /* Charged abilities have a limited amount of charges, and function as the spell of the same-name. 
		Armour, clothing and jewelry function automatically when worn. They may contain one of the following effects � 
		*Clumsiness 
		*Feeblemindedness 
		*Weakness 
		*Agility 
		*Cunning 
		*Strength 
		*Protection 
		*Curses 
		*Night Eyes 
		*Blessings 
		*Spell Reflection 
		*Invisibility 
		*Protection ((Armour only)) 
		*Teleportation ((Rings only)) 
	 * http://forums.uosecondage.com/viewtopic.php?f=9&t=4150
	 * "Night Eyes" supported here:
	 * http://uo.stratics.com/php-bin/show_content.php?content=31536
	 */
    public enum JewelMagicEffect
    {
        None = 0,
        MagicReflect,   //1
        Invisibility,   //2
        Bless,          //3
        Teleport,       //4
        Agility,        //5
        Cunning,        //6
        Strength,       //7
        NightSight,     //8
        Curse,          //9
        Clumsy,         //10
        Feeblemind,     //11
        Weakness,       //12
    }

    public enum GemType
    {
        None,
        StarSapphire,
        Emerald,
        Sapphire,
        Ruby,
        Citrine,
        Amethyst,
        Tourmaline,
        Amber,
        Diamond
    }

    // AI only
    public enum JewelQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseJewel : Item, ICraftable
    {
        //private AosAttributes m_AosAttributes;
        //private AosElementAttributes m_AosResistances;
        //private AosSkillBonuses m_AosSkillBonuses;
        private CraftResource m_Resource;
        private GemType m_GemType;
        private JewelMagicEffect m_MagicType;
        private int m_MagicCharges;
        private bool m_Identified;
        private IOBAlignment m_IOBAlignment;
        private Timer m_StatEffectTimer;
        private Timer m_InvisTimer;
        private Timer m_NightSightTimer;
        private Timer m_MagicReflectTimer;
        private Mobile m_Crafter;
        private JewelQuality m_Quality;
        private DateTime m_Expiration;
        private List<int> m_Regions = new List<int>();  // Create a list of allowed regions.

        /*
				[CommandProperty( AccessLevel.GameMaster )]
				public AosAttributes Attributes
				{
					get{ return m_AosAttributes; }
					set{}
				}

				[CommandProperty( AccessLevel.GameMaster )]
				public AosElementAttributes Resistances
				{
					get{ return m_AosResistances; }
					set{}
				}

				[CommandProperty( AccessLevel.GameMaster )]
				public AosSkillBonuses SkillBonuses
				{
					get{ return m_AosSkillBonuses; }
					set{}
				}
		*/
        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; Hue = CraftResources.GetHue(m_Resource); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public GemType GemType
        {
            get { return m_GemType; }
            set { m_GemType = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public JewelMagicEffect MagicType
        {
            get { return m_MagicType; }
            set { m_MagicType = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MagicCharges
        {
            get { return m_MagicCharges; }
            set { m_MagicCharges = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Identified
        {
            get { return m_Identified; }
            set { m_Identified = value; }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        /// <summary>
        /// Adam: add a list of region IDs in which this item may be used.
        /// Since lists of integers aren't really supported by the properties gump, we manipulate it as a string
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public string RegionLock
        {
            get
            {
                if (m_Regions != null && m_Regions.Count > 0)
                {
                    String sx = null;
                    foreach (int ix in m_Regions)
                        sx += String.Format("{0}, ", ix);
                    return sx.TrimEnd(new char[] { ' ', ',' });
                }
                else
                    return null;
            }
            set
            {
                List<int> temp = new List<int>();
                if (value != null)
                {
                    char[] delimiterChars = { ' ', ',' };
                    string[] words = value.Split(delimiterChars);
                    foreach (string sx in words)
                        if (sx.Length > 0)
                            temp.Add(Int32.Parse(sx));

                    //okay, we didn't crash, update our master.
                    m_Regions.Clear();
                    foreach (int jx in temp)
                        m_Regions.Add(jx);
                }
            }
        }

        public bool CheckRegion()
        {
            if (Parent is Mobile == false)
                return false;

            // if not specified, all regions match
            if (m_Regions.Count == 0)
                return true;

            Mobile from = Parent as Mobile;
            Map map = from.Map;
            bool got_it = false;

            if (map != null)
            {
                try
                {
                    ArrayList reglist = Region.FindAll(from.Location, map);

                    foreach (Region rx in reglist)
                    {
                        if (rx is Region)
                        {
                            if (rx is HouseRegion)
                            {   // don't allow use in a house even if the underlying region is a match
                                return false;
                            }
                            else if (rx is SensoryRegion)
                            {   // return the id
                                if (m_Regions.Contains(rx.UId))
                                    got_it = true;
                            }
                            else if (rx is CustomRegion)
                            {   // sure, why not
                                if (m_Regions.Contains(rx.UId))
                                    got_it = true;
                            }
                            else if (rx != map.DefaultRegion)
                            {   // sure, why not
                                if (m_Regions.Contains(rx.UId))
                                    got_it = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }

            return got_it;
        }

        [CommandProperty(AccessLevel.Seer)]
        public DateTime Expiration
        {
            get { return m_Expiration; }
            set { m_Expiration = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get
            {
                return m_Crafter;
            }
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public JewelQuality Quality
        {
            get { return m_Quality; }
            set { m_Quality = value; InvalidateProperties(); }
        }

        public virtual int BaseGemTypeNumber { get { return 0; } }

        public override int LabelNumber
        {
            get
            {
                if (m_GemType == GemType.None)
                    return base.LabelNumber;

                return BaseGemTypeNumber + (int)m_GemType - 1;
            }
        }

        public virtual int ArtifactRarity { get { return 0; } }

        public BaseJewel(int itemID, Layer layer)
            : base(itemID)
        {
            //m_AosAttributes = new AosAttributes( this );
            //m_AosResistances = new AosElementAttributes( this );
            //m_AosSkillBonuses = new AosSkillBonuses( this );
            m_Resource = CraftResource.Iron;
            m_GemType = GemType.None;
            m_MagicType = JewelMagicEffect.None;
            m_MagicCharges = 0;
            m_Identified = true;
            m_IOBAlignment = IOBAlignment.None;
            Layer = layer;
            m_InvisTimer = null;
            m_StatEffectTimer = null;
            m_NightSightTimer = null;
            m_MagicReflectTimer = null;
            m_Expiration = DateTime.MinValue;           // null
        }

        public override bool CanEquip(Mobile m)
        {
            if (!Ethics.Ethic.CheckEquip(m, this))
                return false;

            if (m.AccessLevel < AccessLevel.GameMaster)
            {
                #region Kin
                if ((m != null) && (m is PlayerMobile))
                {
                    PlayerMobile pm = (PlayerMobile)m;

                    if (Core.UOMO || Core.UOAI || Core.UOREN)
                        if (this.IOBAlignment != IOBAlignment.None)
                        {
                            if (pm.IOBEquipped == true)
                            {
                                pm.SendMessage("You cannot equip more than one item of brethren at a time.");
                                return false;
                            }
                            if (pm.IOBAlignment != this.IOBAlignment)
                            {
                                if (pm.IOBAlignment == IOBAlignment.None)
                                {
                                    pm.SendMessage("You cannot equip a kin item without your guild aligning itself to a kin.");
                                }
                                else if (pm.IOBAlignment == IOBAlignment.OutCast)
                                {
                                    pm.SendMessage("You cannot equip a kin item while you are outcast from your kin.");
                                }
                                else
                                {
                                    pm.SendMessage("You cannot equip items of another kin.");
                                }
                                return false;
                            }
                        }
                }
                #endregion
            }

            return base.CanEquip(m);
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);
            ApplyMagic(parent);
        }

        public void ApplyMagic(object parent)
        {
            if (parent is PlayerMobile)
            {
                PlayerMobile Wearer = (PlayerMobile)parent;

                // if charges > 0 apply the magic effect
                if (MagicCharges > 0)
                {
                    // apply magic effect to wearer if appropriate
                    switch (MagicType)
                    {   // skills/magic
                        case JewelMagicEffect.MagicReflect:
                            if (ApplyMagicReflectEffect(Wearer))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Invisibility:
                            if (ApplyInvisibilityEffect(Wearer))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.NightSight:
                            if (ApplyNightSight(Wearer))
                                MagicCharges--;
                            break;
                        // stats
                        case JewelMagicEffect.Bless:
                            if (ApplyStatEffect(Wearer, true, true, true, 10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Agility:
                            if (ApplyStatEffect(Wearer, false, true, false, 10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Cunning:
                            if (ApplyStatEffect(Wearer, false, false, true, 10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Strength:
                            if (ApplyStatEffect(Wearer, true, false, false, 10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Curse:
                            if (ApplyStatEffect(Wearer, true, true, true, -10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Clumsy:
                            if (ApplyStatEffect(Wearer, false, true, false, -10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Feeblemind:
                            if (ApplyStatEffect(Wearer, false, false, true, -10))
                                MagicCharges--;
                            break;
                        case JewelMagicEffect.Weakness:
                            if (ApplyStatEffect(Wearer, true, false, false, -10))
                                MagicCharges--;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void OnRemoved(object parent)
        {
            if (parent is PlayerMobile)
            {
                PlayerMobile Wearer = (PlayerMobile)parent;

                if (m_InvisTimer != null)
                {
                    if (m_InvisTimer.Running)
                    {
                        Wearer.RevealingAction();
                        m_InvisTimer.Stop();
                        m_InvisTimer = null;
                    }
                }

                if (m_NightSightTimer != null)
                {
                    if (m_NightSightTimer.Running)
                    {
                        Wearer.EndAction(typeof(LightCycle));
                        Wearer.LightLevel = 0;
                        m_NightSightTimer.Stop();
                        m_NightSightTimer = null;
                    }
                }

                if (m_MagicReflectTimer != null)
                {
                    if (m_MagicReflectTimer.Running)
                    {   // leave the player with the effect
                        Wearer.EndAction(typeof(DefensiveSpell));
                        m_MagicReflectTimer.Stop();
                        m_MagicReflectTimer = null;
                    }
                }

                if (m_StatEffectTimer != null)
                {
                    if (m_StatEffectTimer.Running)
                    {
                        string StrName = String.Format("[Magic] {0} Offset:item-{1}", StatType.Str, this.Serial);
                        string IntName = String.Format("[Magic] {0} Offset:item-{1}", StatType.Int, this.Serial);
                        string DexName = String.Format("[Magic] {0} Offset:item-{1}", StatType.Dex, this.Serial);

                        Wearer.RemoveStatMod(StrName);
                        Wearer.RemoveStatMod(IntName);
                        Wearer.RemoveStatMod(DexName);
                        Wearer.CheckStatTimers();
                        m_StatEffectTimer.Stop();
                        m_StatEffectTimer = null;
                    }
                }
            }

        }

        public bool ApplyMagicReflectEffect(PlayerMobile Wearer)
        {
            if (Wearer == null)
                return false;

            if (Wearer.MagicDamageAbsorb > 0)
            {
                Wearer.SendMessage("The magic of this item is already protecting you.");
                return false;
            }
            else if (Wearer.Region.OnBeginSpellCast(Wearer, new MagicReflectSpell(Wearer, null)) == false)
            {
                Wearer.SendMessage("The magic normally within this object seems absent.");
                return false;
            }
            else if (!Wearer.CanBeginAction(typeof(DefensiveSpell)))
            {
                Wearer.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                return false;
            }
            else
            {
                if (Wearer.BeginAction(typeof(DefensiveSpell)))
                {
                    int value = (int)((Utility.Random(51) + 50) + (Utility.Random(51) + 50)); // Random value of up to 100 for magery and up to 100 for scribing - lowest though is 50 magery/50 scribing equivalent strength
                    value = (int)(8 + (value / 200) * 7.0);//absorb from 8 to 15 "circles"

                    Wearer.MagicDamageAbsorb = value;

                    // no sound or animation - stealth
                    Wearer.SendMessage("You feel the magic of the item envelope you.");

                    m_MagicReflectTimer = new MagicEffectTimer(Wearer, this, TimeSpan.FromSeconds(5));
                    m_MagicReflectTimer.Start();
                    return true;
                }
                else
                {
                    Wearer.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                    return false;
                }
            }
        }

        public bool ApplyInvisibilityEffect(PlayerMobile Wearer)
        {
            Spell spell = new InvisibilitySpell(Wearer, null);

            if (Wearer == null)
                return false;

            if (Wearer.Hidden == true)
            {
                // player is already invisible...do nothing
                return false;
            }
            else if (Wearer.Region.OnBeginSpellCast(Wearer, spell) == false)
            {
                Wearer.SendMessage("The magic normally within this object seems absent.");
                return false;
            }
            else
            {
                // hide the player, set a timer to check for additional charges or reveal
                Wearer.Hidden = true;

                if (m_InvisTimer != null)
                {
                    m_InvisTimer.Stop();
                    m_InvisTimer = null;
                }

                m_InvisTimer = new MagicEffectTimer(Wearer, this, TimeSpan.FromSeconds(120));

                m_InvisTimer.Start();
                return true;
            }
        }

        public bool ApplyNightSight(PlayerMobile Wearer)
        {
            Spell spell = new NightSightSpell(Wearer, null);

            if (Wearer == null)
                return false;

            if (Wearer.Region.OnBeginSpellCast(Wearer, spell) == false)
            {
                Wearer.SendMessage("The magic normally within this object seems absent.");
                return false;
            }
            //Pix: this was borrowed from the NightSight spell...
            else if (Wearer.BeginAction(typeof(LightCycle)))
            {
                // Adam, we need to own this timer so that we can decrement the charges - duh!
                m_NightSightTimer = new MagicEffectTimer(Wearer, this, TimeSpan.FromMinutes(Utility.Random(15, 25)));
                m_NightSightTimer.Start();

                int level = 25;
                Wearer.LightLevel = level;

                // no sound or animation - stealth
                return true;
            }

            return false;
        }

        public bool ApplyStatEffect(PlayerMobile Wearer, bool bStr, bool bDex, bool bInt, int change)
        {
            Spell spell = null;

            if (Wearer == null)
                return false;

            // Try to apply bless to all stats
            int BlessOffset = change;
            bool AppliedStr = false;
            bool AppliedInt = false;
            bool AppliedDex = false;
            if (bStr)
            {
                if (BlessOffset > 0)
                {
                    spell = new StrengthSpell(Wearer, null);
                }
                else
                    spell = new WeakenSpell(Wearer, null);

                if (Wearer.Region.OnBeginSpellCast(Wearer, spell) == false)
                {
                    Wearer.SendMessage("The magic normally within this object seems absent.");
                    return false;
                }

                AppliedStr = AddStatBonus(Wearer, BlessOffset, StatType.Str, TimeSpan.Zero);
            }
            if (bInt)
            {
                if (BlessOffset > 0)
                {
                    spell = new CunningSpell(Wearer, null);
                }
                else
                    spell = new FeeblemindSpell(Wearer, null);

                if (Wearer.Region.OnBeginSpellCast(Wearer, spell) == false)
                {
                    Wearer.SendMessage("The magic normally within this object seems absent.");
                    return false;
                }
                AppliedInt = AddStatBonus(Wearer, BlessOffset, StatType.Int, TimeSpan.Zero);
            }

            if (bDex)
            {
                if (BlessOffset > 0)
                {
                    spell = new AgilitySpell(Wearer, null);
                }
                else
                    spell = new ClumsySpell(Wearer, null);

                if (Wearer.Region.OnBeginSpellCast(Wearer, spell) == false)
                {
                    Wearer.SendMessage("The magic normally within this object seems absent.");
                    return false;
                }
                AppliedDex = AddStatBonus(Wearer, BlessOffset, StatType.Dex, TimeSpan.Zero);
            }
            Wearer.CheckStatTimers();
            // If any stats were adjusted, start timer to remove the stats after effect expires
            // return that spell was successful
            if (AppliedStr || AppliedInt || AppliedDex) /* 7/25/04 smerX */
            {
                if (m_StatEffectTimer != null)
                {
                    m_StatEffectTimer.Stop();
                    m_StatEffectTimer = null;
                }

                m_StatEffectTimer = new MagicEffectTimer(Wearer, this, TimeSpan.FromSeconds(120));

                m_StatEffectTimer.Start();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddStatBonus(Mobile target, int offset, StatType type, TimeSpan duration)
        {
            if (target == null)
                return false;

            string name = String.Format("[Magic] {0} Offset:item-{1}", type, this.Serial);
            string itemtypename = String.Format("[Magic] {0} Offset:item-", type);

            StatMod mod = target.GetStatMod(name);

            for (int i = 0; i < target.StatMods.Count; i++)
            {
                StatMod sm = target.StatMods[i] as StatMod;
                if (sm != null)
                {
                    if (sm.Name.IndexOf(itemtypename) == 0)
                    {
                        //found this item statmod type already - don't apply
                        return false;
                    }
                }
            }

            // If they have a negative effect on them, replace the effect with
            // the negative effect plus the new positive effect
            if (mod != null && mod.Offset < 0)
            {
                target.AddStatMod(new StatMod(type, name, mod.Offset + offset, duration));
                return true;
            }
            // If they have no effect or the current effect is weaker than the new effect
            // Apply the new effect
            else if (mod == null || mod.Offset < offset)
            {
                target.AddStatMod(new StatMod(type, name, offset, duration));
                return true;
            }
            // They already have an effect equal to or greater than the new effect
            // do nothing.
            return false;
        }
#if (not_used)
		public void SetMagic(JewelMagicEffect Effect, int Charges)
		{
			// Only allow Teleport to be set on Rings
			if (Effect == JewelMagicEffect.Teleport)
			{
				if (this is BaseRing)
				{
					m_MagicType = Effect;
					m_MagicCharges = Charges;
					Identified = false;
				}
			}
			else
			{
				m_MagicType = Effect;
				m_MagicCharges = Charges;
				Identified = false;
			}
		}
#endif
        public void SetRandomMagicEffect(int MinLevel, int MaxLevel)
        {
            if (MinLevel < 1 || MaxLevel > 3)
                return;

            int NewMagicType;

            if (this is BaseRing)
            {
                NewMagicType = Utility.RandomList((int)JewelMagicEffect.MagicReflect,
                    (int)JewelMagicEffect.Invisibility, (int)JewelMagicEffect.Bless,
                    (int)JewelMagicEffect.Teleport, (int)JewelMagicEffect.Agility,
                    (int)JewelMagicEffect.Cunning, (int)JewelMagicEffect.Strength,
                    (int)JewelMagicEffect.NightSight);
            }
            else
            {
                // no teleporting for non-rings
                NewMagicType = Utility.RandomList((int)JewelMagicEffect.MagicReflect,
                    (int)JewelMagicEffect.Invisibility, (int)JewelMagicEffect.Bless,
                    /*(int)JewelMagicEffect.Teleport,*/ (int)JewelMagicEffect.Agility,
                    (int)JewelMagicEffect.Cunning, (int)JewelMagicEffect.Strength,
                    (int)JewelMagicEffect.NightSight);
            }

            m_MagicType = (JewelMagicEffect)NewMagicType;

            int NewLevel = Utility.RandomMinMax(MinLevel, MaxLevel);
            switch (NewLevel)
            {
                case 1:
                    m_MagicCharges = Utility.Random(1, 5);
                    break;
                case 2:
                    m_MagicCharges = Utility.Random(4, 11);
                    break;
                case 3:
                    m_MagicCharges = Utility.Random(9, 20);
                    break;
                default:
                    // should never happen
                    m_MagicCharges = 0;
                    break;
            }
            Identified = false;
        }

        public BaseJewel(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            /*
						m_AosSkillBonuses.GetProperties( list );

						int prop;

						if ( (prop = ArtifactRarity) > 0 )
							list.Add( 1061078, prop.ToString() ); // artifact rarity ~1_val~

						if ( (prop = m_AosAttributes.WeaponDamage) != 0 )
							list.Add( 1060401, prop.ToString() ); // damage increase ~1_val~%

						if ( (prop = m_AosAttributes.DefendChance) != 0 )
							list.Add( 1060408, prop.ToString() ); // defense chance increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusDex) != 0 )
							list.Add( 1060409, prop.ToString() ); // dexterity bonus ~1_val~

						if ( (prop = m_AosAttributes.EnhancePotions) != 0 )
							list.Add( 1060411, prop.ToString() ); // enhance potions ~1_val~%

						if ( (prop = m_AosAttributes.CastRecovery) != 0 )
							list.Add( 1060412, prop.ToString() ); // faster cast recovery ~1_val~

						if ( (prop = m_AosAttributes.CastSpeed) != 0 )
							list.Add( 1060413, prop.ToString() ); // faster casting ~1_val~

						if ( (prop = m_AosAttributes.AttackChance) != 0 )
							list.Add( 1060415, prop.ToString() ); // hit chance increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusHits) != 0 )
							list.Add( 1060431, prop.ToString() ); // hit point increase ~1_val~

						if ( (prop = m_AosAttributes.BonusInt) != 0 )
							list.Add( 1060432, prop.ToString() ); // intelligence bonus ~1_val~

						if ( (prop = m_AosAttributes.LowerManaCost) != 0 )
							list.Add( 1060433, prop.ToString() ); // lower mana cost ~1_val~%

						if ( (prop = m_AosAttributes.LowerRegCost) != 0 )
							list.Add( 1060434, prop.ToString() ); // lower reagent cost ~1_val~%

						if ( (prop = m_AosAttributes.Luck) != 0 )
							list.Add( 1060436, prop.ToString() ); // luck ~1_val~

						if ( (prop = m_AosAttributes.BonusMana) != 0 )
							list.Add( 1060439, prop.ToString() ); // mana increase ~1_val~

						if ( (prop = m_AosAttributes.RegenMana) != 0 )
							list.Add( 1060440, prop.ToString() ); // mana regeneration ~1_val~

						if ( (prop = m_AosAttributes.ReflectPhysical) != 0 )
							list.Add( 1060442, prop.ToString() ); // reflect physical damage ~1_val~%

						if ( (prop = m_AosAttributes.RegenStam) != 0 )
							list.Add( 1060443, prop.ToString() ); // stamina regeneration ~1_val~

						if ( (prop = m_AosAttributes.RegenHits) != 0 )
							list.Add( 1060444, prop.ToString() ); // hit point regeneration ~1_val~

						if ( (prop = m_AosAttributes.SpellChanneling) != 0 )
							list.Add( 1060482 ); // spell channeling

						if ( (prop = m_AosAttributes.SpellDamage) != 0 )
							list.Add( 1060483, prop.ToString() ); // spell damage increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusStam) != 0 )
							list.Add( 1060484, prop.ToString() ); // stamina increase ~1_val~

						if ( (prop = m_AosAttributes.BonusStr) != 0 )
							list.Add( 1060485, prop.ToString() ); // strength bonus ~1_val~

						if ( (prop = m_AosAttributes.WeaponSpeed) != 0 )
							list.Add( 1060486, prop.ToString() ); // swing speed increase ~1_val~%
			*/
            if (Identified == true && MagicCharges > 0)
            {
                string MagicName;
                switch (MagicType)
                {
                    case JewelMagicEffect.MagicReflect:
                        MagicName = "magic reflection";
                        break;
                    case JewelMagicEffect.Invisibility:
                        MagicName = "invisibility";
                        break;
                    case JewelMagicEffect.Bless:
                        MagicName = "bless";
                        break;
                    case JewelMagicEffect.Teleport:
                        MagicName = "teleport";
                        break;
                    case JewelMagicEffect.Agility:
                        MagicName = "agility";
                        break;
                    case JewelMagicEffect.Cunning:
                        MagicName = "cunning";
                        break;
                    case JewelMagicEffect.Strength:
                        MagicName = "strength";
                        break;
                    case JewelMagicEffect.NightSight:
                        MagicName = "night sight";
                        break;
                    case JewelMagicEffect.Curse:
                        MagicName = "curse";
                        break;
                    case JewelMagicEffect.Clumsy:
                        MagicName = "clumsy";
                        break;
                    case JewelMagicEffect.Feeblemind:
                        MagicName = "feeblemind";
                        break;
                    case JewelMagicEffect.Weakness:
                        MagicName = "weakness";
                        break;
                    default:
                        MagicName = "Unknown";
                        break;
                }
                string MagicProp = String.Format("{0} - charges:{1}", MagicName, MagicCharges);
                list.Add(MagicProp);
            }
            else if (Identified == false && MagicCharges > 0)
                list.Add("Unidentified");

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

            if (m_Quality == JewelQuality.Exceptional)
                list.Add(1060636); // exceptional
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.HideAttributes == true)
            {
                base.OnSingleClick(from);
                return;
            }

            ArrayList attrs = new ArrayList();

            if (DisplayLootType)
            {
                if (LootType == LootType.Blessed)
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                else if (LootType == LootType.Cursed)
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }

            if (Name != null || OldName == null) // only use the new ([X/Y/Z]) method on things we don't have OldNames for
            {
                if (m_Quality == JewelQuality.Exceptional)
                    attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

                if (Identified == false && MagicCharges > 0)
                    attrs.Add(new EquipInfoAttribute(1038000)); // unidentified
                else if (Identified == true && MagicCharges > 0)
                {
                    switch (MagicType)
                    {
                        case JewelMagicEffect.MagicReflect:
                            attrs.Add(new EquipInfoAttribute(1044416, m_MagicCharges)); // magic reflection
                            break;
                        case JewelMagicEffect.Invisibility:
                            attrs.Add(new EquipInfoAttribute(1044424, m_MagicCharges)); // invisibility
                            break;
                        case JewelMagicEffect.Bless:
                            attrs.Add(new EquipInfoAttribute(1044397, m_MagicCharges)); // bless
                            break;
                        case JewelMagicEffect.Teleport:
                            attrs.Add(new EquipInfoAttribute(1044402, m_MagicCharges)); // teleport
                            break;
                        case JewelMagicEffect.Agility:
                            attrs.Add(new EquipInfoAttribute(1044389, m_MagicCharges)); // agility
                            break;
                        case JewelMagicEffect.Cunning:
                            attrs.Add(new EquipInfoAttribute(1044390, m_MagicCharges)); // cunning
                            break;
                        case JewelMagicEffect.Strength:
                            attrs.Add(new EquipInfoAttribute(1044396, m_MagicCharges)); // strength
                            break;
                        case JewelMagicEffect.NightSight:
                            attrs.Add(new EquipInfoAttribute(1044387, m_MagicCharges)); // night sight
                            break;
                        case JewelMagicEffect.Curse:
                            attrs.Add(new EquipInfoAttribute(1044407, m_MagicCharges)); // curse
                            break;
                        case JewelMagicEffect.Clumsy:
                            attrs.Add(new EquipInfoAttribute(1044382, m_MagicCharges)); // clumsy
                            break;
                        case JewelMagicEffect.Feeblemind:
                            attrs.Add(new EquipInfoAttribute(1044384, m_MagicCharges)); // feeblemind
                            break;
                        case JewelMagicEffect.Weakness:
                            attrs.Add(new EquipInfoAttribute(1044388, m_MagicCharges)); // weaken
                            break;
                    }
                }
            }

            if (attrs.Count == 0 && Name != null && m_Crafter == null)
                return;

            int number;

            if (Name == null)
            {
                if (OldName == null)
                {
                    number = LabelNumber;
                }
                else
                {
                    // display old style

                    string oldname = OldName;
                    string article = OldArticle;

                    // TBD
                    OldOnSingleClick(from);
                    return;

                    //finally, add the article
                    oldname = article + " " + oldname;

                    this.LabelTo(from, oldname);
                    number = 1041000;
                }
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            if (Name != null || OldName == null)
            {
                EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));
                from.Send(new DisplayEquipmentInfo(this, eqInfo));
            }
            else
            {
                if (attrs.Count > 0)
                {
                    EquipmentInfo eqInfo = new EquipmentInfo(number, null, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));
                    from.Send(new DisplayEquipmentInfo(this, eqInfo));
                }
            }
        }

        #region OLD OnSingleClick
        // FOR TEST - comment-out WHEN DONE
        public void OldOnSingleClick(Mobile from)
        {
            if (this.HideAttributes == true)
            {
                base.OnSingleClick(from);
                return;
            }

            ArrayList attrs = new ArrayList();

            if (DisplayLootType)
            {
                if (LootType == LootType.Blessed)
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                else if (LootType == LootType.Cursed)
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }

            if (m_Quality == JewelQuality.Exceptional)
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (Identified == false && MagicCharges > 0)
                attrs.Add(new EquipInfoAttribute(1038000)); // unidentified
            else if (Identified == true && MagicCharges > 0)
            {
                switch (MagicType)
                {
                    case JewelMagicEffect.MagicReflect:
                        attrs.Add(new EquipInfoAttribute(1044416, m_MagicCharges)); // magic reflection
                        break;
                    case JewelMagicEffect.Invisibility:
                        attrs.Add(new EquipInfoAttribute(1044424, m_MagicCharges)); // invisibility
                        break;
                    case JewelMagicEffect.Bless:
                        attrs.Add(new EquipInfoAttribute(1044397, m_MagicCharges)); // bless
                        break;
                    case JewelMagicEffect.Teleport:
                        attrs.Add(new EquipInfoAttribute(1044402, m_MagicCharges)); // teleport
                        break;
                    case JewelMagicEffect.Agility:
                        attrs.Add(new EquipInfoAttribute(1044389, m_MagicCharges)); // agility
                        break;
                    case JewelMagicEffect.Cunning:
                        attrs.Add(new EquipInfoAttribute(1044390, m_MagicCharges)); // cunning
                        break;
                    case JewelMagicEffect.Strength:
                        attrs.Add(new EquipInfoAttribute(1044396, m_MagicCharges)); // strength
                        break;
                    case JewelMagicEffect.NightSight:
                        attrs.Add(new EquipInfoAttribute(1044387, m_MagicCharges)); // night sight
                        break;
                    case JewelMagicEffect.Curse:
                        attrs.Add(new EquipInfoAttribute(1044407, m_MagicCharges)); // curse
                        break;
                    case JewelMagicEffect.Clumsy:
                        attrs.Add(new EquipInfoAttribute(1044382, m_MagicCharges)); // clumsy
                        break;
                    case JewelMagicEffect.Feeblemind:
                        attrs.Add(new EquipInfoAttribute(1044384, m_MagicCharges)); // feeblemind
                        break;
                    case JewelMagicEffect.Weakness:
                        attrs.Add(new EquipInfoAttribute(1044388, m_MagicCharges)); // weaken
                        break;
                }
            }

            if (attrs.Count == 0 && Name != null && m_Crafter == null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }
        #endregion

        private class MagicEffectTimer : Timer
        {
            private PlayerMobile m_Wearer;
            private BaseJewel m_Jewel;

            public MagicEffectTimer(PlayerMobile Wearer, BaseJewel Jewel, TimeSpan Duration)
                : base(Duration)
            {
                m_Wearer = Wearer;
                m_Jewel = Jewel;
            }

            protected override void OnTick()
            {
                if (m_Wearer != null)
                {
                    if (m_Jewel != null)
                    {
                        // this item has exired - special event items
                        if (DateTime.UtcNow > m_Jewel.Expiration)
                        {   // wipe special properties
                            m_Jewel.Name = null;
                            m_Jewel.MagicCharges = 0;
                        }

                        // check region locks - special event items
                        if (m_Jewel.CheckRegion() == false && m_Jewel.MagicCharges > 0)
                        {
                            // do nothing but mark-time
                            Start();
                            return;
                        }

                        switch (m_Jewel.MagicType)
                        {
                            case JewelMagicEffect.NightSight:
                                if (m_Jewel.MagicCharges > 0)
                                {
                                    m_Jewel.MagicCharges--;
                                    Start();
                                }
                                else
                                {
                                    Stop();
                                    m_Wearer.EndAction(typeof(LightCycle));
                                    m_Wearer.LightLevel = 0;
                                }
                                break;

                            case JewelMagicEffect.Invisibility:
                                if (m_Jewel.MagicCharges > 0)
                                {
                                    m_Wearer.Hidden = true;
                                    m_Jewel.MagicCharges--;
                                    Start();
                                }
                                else
                                {
                                    Stop();
                                    m_Wearer.RevealingAction();
                                }
                                break;

                            case JewelMagicEffect.MagicReflect:
                                if (m_Jewel.MagicCharges > 0)
                                {   // uosecondage says you continue to eat charges every 5 seconds regardless if 
                                    //	the reflect has been taken down
                                    m_Jewel.MagicCharges--;
                                    Start();
                                    //System.Diagnostics.Debugger.Log(1, "", String.Format("Charges = {0}, Before refresh MagicDamageAbsorb = {1}\n", m_Jewel.MagicCharges, m_Wearer.MagicDamageAbsorb)); /*DEBUG*/
                                    // it's okay if MagicDamageAbsorb > 0 as we will refresh it 
                                    if (m_Wearer.CanBeginAction(typeof(DefensiveSpell)))
                                    {
                                        m_Wearer.BeginAction(typeof(DefensiveSpell));
                                        //System.Diagnostics.Debugger.Log(1, "", String.Format("BeginAction() ok, MagicDamageAbsorb = {0}\n", m_Wearer.MagicDamageAbsorb)); /*DEBUG*/
                                    }

                                    int value = (int)((Utility.Random(51) + 50) + (Utility.Random(51) + 50)); // Random value of up to 100 for magery and up to 100 for scribing - lowest though is 50 magery/50 scribing equivalent strength
                                    value = (int)(8 + (value / 200) * 7.0);//absorb from 8 to 15 "circles"
                                    m_Wearer.MagicDamageAbsorb = value;
                                    //System.Diagnostics.Debugger.Log(1, "", String.Format("After refresh, MagicDamageAbsorb = {0}\n", m_Wearer.MagicDamageAbsorb)); /*DEBUG*/
                                }
                                else
                                {
                                    //System.Diagnostics.Debugger.Log(1, "", String.Format("END ACTION\n")); /*DEBUG*/
                                    m_Wearer.EndAction(typeof(DefensiveSpell));
                                    Stop();
                                }
                                break;

                            case JewelMagicEffect.Bless:
                            case JewelMagicEffect.Agility:
                            case JewelMagicEffect.Clumsy:
                            case JewelMagicEffect.Cunning:
                            case JewelMagicEffect.Curse:
                            case JewelMagicEffect.Feeblemind:
                            case JewelMagicEffect.Strength:
                            case JewelMagicEffect.Weakness:
                                if (m_Jewel.MagicCharges > 0)
                                {
                                    m_Jewel.MagicCharges--;
                                    Start();
                                }
                                else
                                {
                                    Stop();
                                    string StrName = String.Format("[Magic] {0} Offset:item-{1}", StatType.Str, m_Jewel.Serial);
                                    string IntName = String.Format("[Magic] {0} Offset:item-{1}", StatType.Int, m_Jewel.Serial);
                                    string DexName = String.Format("[Magic] {0} Offset:item-{1}", StatType.Dex, m_Jewel.Serial);

                                    m_Wearer.RemoveStatMod(StrName);
                                    m_Wearer.RemoveStatMod(IntName);
                                    m_Wearer.RemoveStatMod(DexName);

                                    m_Wearer.CheckStatTimers();
                                }
                                break;
                            default:
                                Stop();
                                break;
                        }
                    }
                }
            }
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Ressources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            if (1 < craftItem.Ressources.Count)
            {
                resourceType = craftItem.Ressources.GetAt(1).ItemType;

                if (resourceType == typeof(StarSapphire))
                    GemType = GemType.StarSapphire;
                else if (resourceType == typeof(Emerald))
                    GemType = GemType.Emerald;
                else if (resourceType == typeof(Sapphire))
                    GemType = GemType.Sapphire;
                else if (resourceType == typeof(Ruby))
                    GemType = GemType.Ruby;
                else if (resourceType == typeof(Citrine))
                    GemType = GemType.Citrine;
                else if (resourceType == typeof(Amethyst))
                    GemType = GemType.Amethyst;
                else if (resourceType == typeof(Tourmaline))
                    GemType = GemType.Tourmaline;
                else if (resourceType == typeof(Amber))
                    GemType = GemType.Amber;
                else if (resourceType == typeof(Diamond))
                    GemType = GemType.Diamond;
            }

            if (Core.UOAI || Core.UOREN)
            {
                if (makersMark)
                    this.Crafter = from;

                this.Quality = (JewelQuality)quality;
            }

            return 1;
        }

#if old
		else if (item is BaseJewel)
					{
						BaseJewel jewel = (BaseJewel)item;

						Type resourceType = typeRes;
						endquality = quality;

						if (resourceType == null)
							resourceType = Ressources.GetAt(0).ItemType;

						jewel.Resource = CraftResources.GetFromType(resourceType);

						if (1 < Ressources.Count)
						{
							resourceType = Ressources.GetAt(1).ItemType;

							if (resourceType == typeof(StarSapphire))
								jewel.GemType = GemType.StarSapphire;
							else if (resourceType == typeof(Emerald))
								jewel.GemType = GemType.Emerald;
							else if (resourceType == typeof(Sapphire))
								jewel.GemType = GemType.Sapphire;
							else if (resourceType == typeof(Ruby))
								jewel.GemType = GemType.Ruby;
							else if (resourceType == typeof(Citrine))
								jewel.GemType = GemType.Citrine;
							else if (resourceType == typeof(Amethyst))
								jewel.GemType = GemType.Amethyst;
							else if (resourceType == typeof(Tourmaline))
								jewel.GemType = GemType.Tourmaline;
							else if (resourceType == typeof(Amber))
								jewel.GemType = GemType.Amber;
							else if (resourceType == typeof(Diamond))
								jewel.GemType = GemType.Diamond;
						}

						if (makersMark)
							jewel.Crafter = from;

						jewel.Quality = (JewelQuality)quality;
					}
#endif

        #endregion

        #region Save Flags
        [Flags]
        enum SaveFlags
        {
            None = 0x0,
            HasExpiration = 0x01,
            HasIOBAlignment = 0x02,
            HasRegionLock = 0x04,
        }

        private SaveFlags m_SaveFlags = SaveFlags.None;

        private void SetFlag(SaveFlags flag, bool value)
        {
            if (value)
                m_SaveFlags |= flag;
            else
                m_SaveFlags &= ~flag;
        }

        private bool GetFlag(SaveFlags flag)
        {
            return ((m_SaveFlags & flag) != 0);
        }

        private SaveFlags ReadSaveFlags(GenericReader reader, int version)
        {
            SaveFlags sf = SaveFlags.None;
            if (version >= 6)
                sf = (SaveFlags)reader.ReadInt();
            return sf;
        }

        private SaveFlags WriteSaveFlags(GenericWriter writer)
        {
            m_SaveFlags = SaveFlags.None;
            SetFlag(SaveFlags.HasExpiration, m_Expiration != DateTime.MinValue ? true : false);
            SetFlag(SaveFlags.HasIOBAlignment, m_IOBAlignment != IOBAlignment.None ? true : false);
            SetFlag(SaveFlags.HasRegionLock, m_Regions.Count > 0 ? true : false);
            writer.Write((int)m_SaveFlags);
            return m_SaveFlags;
        }
        #endregion Save Flags

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)6);                   // version
            m_SaveFlags = WriteSaveFlags(writer);   // always follows version

            // version 6
            if (GetFlag(SaveFlags.HasIOBAlignment))
                writer.Write((int)m_IOBAlignment);

            if (GetFlag(SaveFlags.HasExpiration))
                writer.WriteDeltaTime(m_Expiration);

            if (GetFlag(SaveFlags.HasRegionLock))
            {
                writer.Write((short)m_Regions.Count);
                foreach (int ix in m_Regions)
                    writer.Write(ix);
            }

            // earlier versions
            writer.Write((Mobile)m_Crafter);
            writer.Write((short)m_Quality);

            writer.Write((int)m_MagicType);
            writer.Write((int)m_MagicCharges);
            writer.Write((bool)m_Identified);

            writer.WriteEncodedInt((int)m_Resource);
            writer.WriteEncodedInt((int)m_GemType);

            // removed in version 4
            //m_AosAttributes.Serialize( writer );
            //m_AosResistances.Serialize( writer );
            //m_AosSkillBonuses.Serialize( writer );
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SaveFlags = ReadSaveFlags(reader, version);

            switch (version)
            {
                case 6:
                    {   // adam: add flags and support for special expiring event items
                        if (GetFlag(SaveFlags.HasIOBAlignment))
                            m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        else
                            m_IOBAlignment = IOBAlignment.None;

                        if (GetFlag(SaveFlags.HasExpiration))
                            m_Expiration = reader.ReadDeltaTime();
                        else
                            m_Expiration = DateTime.MinValue;

                        if (GetFlag(SaveFlags.HasRegionLock))
                        {
                            short count = reader.ReadShort();
                            for (int ix = 0; ix < count; ix++)
                                m_Regions.Add(reader.ReadInt());
                        }
                        goto case 5;
                    }
                case 5:
                    {
                        // erl: New "crafted by" and quality properties

                        m_Crafter = reader.ReadMobile();
                        m_Quality = (JewelQuality)reader.ReadShort();
                        goto case 4;
                    }
                case 4:
                    {
                        // remove AOS crap
                        // see case 1 below
                        goto case 3;
                    }
                case 3:
                    {
                        m_MagicType = (JewelMagicEffect)reader.ReadInt();
                        m_MagicCharges = reader.ReadInt();
                        m_Identified = reader.ReadBool();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Resource = (CraftResource)reader.ReadEncodedInt();
                        m_GemType = (GemType)reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        // pack these out of furture versions.
                        if (version < 4)
                        {
                            AosAttributes dmy_AosAttributes;
                            AosElementAttributes dmy_AosResistances;
                            AosSkillBonuses dmy_AosSkillBonuses;
                            dmy_AosAttributes = new AosAttributes(this, reader);
                            dmy_AosResistances = new AosElementAttributes(this, reader);
                            dmy_AosSkillBonuses = new AosSkillBonuses(this, reader);

                            if (Core.AOS && Parent is Mobile)
                                dmy_AosSkillBonuses.AddTo((Mobile)Parent);

                            int strBonus = dmy_AosAttributes.BonusStr;
                            int dexBonus = dmy_AosAttributes.BonusDex;
                            int intBonus = dmy_AosAttributes.BonusInt;

                            if (Parent is Mobile && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
                            {
                                Mobile m = (Mobile)Parent;

                                string modName = Serial.ToString();

                                if (strBonus != 0)
                                    m.AddStatMod(new StatMod(StatType.Str, modName + "Str", strBonus, TimeSpan.Zero));

                                if (dexBonus != 0)
                                    m.AddStatMod(new StatMod(StatType.Dex, modName + "Dex", dexBonus, TimeSpan.Zero));

                                if (intBonus != 0)
                                    m.AddStatMod(new StatMod(StatType.Int, modName + "Int", intBonus, TimeSpan.Zero));
                            }
                        }

                        if (Parent is Mobile)
                            ((Mobile)Parent).CheckStatTimers();

                        break;
                    }
                case 0:
                    {
                        // pack these out of furture versions.
                        if (version < 4)
                        {
                            AosAttributes dmy_AosAttributes;
                            AosElementAttributes dmy_AosResistances;
                            AosSkillBonuses dmy_AosSkillBonuses;
                            dmy_AosAttributes = new AosAttributes(this);
                            dmy_AosResistances = new AosElementAttributes(this);
                            dmy_AosSkillBonuses = new AosSkillBonuses(this);
                        }

                        break;
                    }
            }

            if (version < 2)
            {
                m_Resource = CraftResource.Iron;
                m_GemType = GemType.None;
            }

            if (version < 5)
            {
                m_Quality = JewelQuality.Regular;
            }
        }
    }
}
