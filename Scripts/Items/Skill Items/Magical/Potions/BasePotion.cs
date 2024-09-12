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

/* 
 * ChangeLog
 *	5/26/05, Kit
 *		Added check to only create empty bottles in backpack for Players
 */
using Server.Engines.Craft;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public enum PotionEffect
    {
        Nightsight,
        CureLesser,
        Cure,
        CureGreater,
        Agility,
        AgilityGreater,
        Strength,
        StrengthGreater,
        PoisonLesser,
        Poison,
        PoisonGreater,
        PoisonDeadly,
        Refresh,
        RefreshTotal,
        HealLesser,
        Heal,
        HealGreater,
        ExplosionLesser,
        Explosion,
        ExplosionGreater
    }

    public abstract class BasePotion : Item, ICraftable
    {
        private PotionEffect m_PotionEffect;

        public PotionEffect PotionEffect
        {
            get
            {
                return m_PotionEffect;
            }
            set
            {
                m_PotionEffect = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber { get { return 1041314 + (int)m_PotionEffect; } }

        public BasePotion(int itemID, PotionEffect effect)
            : base(itemID)
        {
            m_PotionEffect = effect;

            Stackable = false;
            Weight = 1.0;
        }

        public BasePotion(Serial serial)
            : base(serial)
        {
        }

        public virtual bool RequireFreeHand { get { return true; } }

        public static bool HasFreeHand(Mobile m)
        {
            Item handOne = m.FindItemOnLayer(Layer.OneHanded);
            Item handTwo = m.FindItemOnLayer(Layer.TwoHanded);

            if (handTwo is BaseWeapon)
                handOne = handTwo;

            return (handOne == null || handTwo == null);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
                return;

            if (from.InRange(this.GetWorldLocation(), 1))
            {
                if (!RequireFreeHand || HasFreeHand(from))
                    Drink(from);
                else
                    from.SendLocalizedMessage(502172); // You must have a free hand to drink a potion.
            }
            else
            {
                from.SendLocalizedMessage(502138); // That is too far away for you to use
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_PotionEffect);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_PotionEffect = (PotionEffect)reader.ReadInt();
                        break;
                    }
            }
        }

        public abstract void Drink(Mobile from);

        public static void PlayDrinkEffect(Mobile m)
        {
            m.RevealingAction();

            m.PlaySound(0x2D6);

            if (m is PlayerMobile)
                m.AddToBackpack(new Bottle());

            if (m.Body.IsHuman /*&& !m.Mounted*/ )
                m.Animate(34, 5, 1, true, false, 0);
        }

        public static TimeSpan Scale(Mobile m, TimeSpan v)
        {
            if (!Core.AOS)
                return v;

            double scalar = 1.0 + (0.01 * AosAttributes.GetValue(m, AosAttribute.EnhancePotions));

            return TimeSpan.FromSeconds(v.TotalSeconds * scalar);
        }

        public static double Scale(Mobile m, double v)
        {
            if (!Core.AOS)
                return v;

            double scalar = 1.0 + (0.01 * AosAttributes.GetValue(m, AosAttribute.EnhancePotions));

            return v * scalar;
        }

        public static int Scale(Mobile m, int v)
        {
            if (!Core.AOS)
                return v;

            return AOS.Scale(v, 100 + AosAttributes.GetValue(m, AosAttribute.EnhancePotions));
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            if (craftSystem is DefAlchemy)
            {
                Container pack = from.Backpack;

                // Publish 15
                // You will now be able to make potions directly into a potion keg.
                if (pack != null && (Core.UOAI || Core.UOREN || Core.UOMO || PublishInfo.Publish >= 15))
                {
                    List<PotionKeg> kegs = pack.FindItemsByType<PotionKeg>();

                    for (int i = 0; i < kegs.Count; ++i)
                    {
                        PotionKeg keg = kegs[i];

                        if (keg == null)
                            continue;

                        if (keg.Held <= 0 || keg.Held >= 100)
                            continue;

                        if (keg.Type != PotionEffect)
                            continue;

                        ++keg.Held;

                        Consume();
                        from.AddToBackpack(new Bottle());

                        return -1; // signal placed in keg
                    }
                }
            }

            return 1;
        }

#if old
		if (craftSystem is DefAlchemy && item is BasePotion)
					{
						BasePotion pot = (BasePotion)item;

						Container pack = from.Backpack;

						if (pack != null)
						{
							Item[] kegs = pack.FindItemsByType(typeof(PotionKeg), true);

							for (int i = 0; i < kegs.Length; ++i)
							{
								PotionKeg keg = kegs[i] as PotionKeg;

								if (keg == null)
									continue;

								if (keg.Held <= 0 || keg.Held >= 100)
									continue;

								if (keg.Type != pot.PotionEffect)
									continue;

								++keg.Held;
								item.Delete();
								item = new Bottle();

								endquality = -1; // signal placed in keg

								break;
							}
						}
					}
#endif

        #endregion
    }
}
