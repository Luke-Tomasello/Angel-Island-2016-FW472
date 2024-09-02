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

/* Scripts/Skills/Poisoning.cs
 * CHANGELOG
 *  11/23/06, plasma
 *      Modified to only apply skill delay OnTick()
 *  7/31/06, Kit	
 *		Limit max beverage size to 3 if poisoned.
 *  7/30/06, Kit
 *		Made beverages poisonable as designed.
 *	9/2/04, Pix
 *		Made it so AIStingers are not poisonable.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  05/23/2004 - Pulse
 *		Added a failure message when failing to create a poison soaked rag.  
 *		Message is "You fail to apply a sufficient dose of poison to soak the rag."
 *  04/17/2004 - Pulse
 *		Added ability to poison a single piece of cloth
 *		When a piece of cloth is poisoned, the cloth is deleted
 *		and a PoisonCloth item is placed in the poisoner's backpack for use
 *		with ranged weapons.  During the poisoning process, the delay variable for
 *		using poison with ranged weapons is set, so changes to the delay must be made there.
 *		Also changed the message displayed when an item is poisoned to reflect the
 *		level of poison used and the object the poison was applied to.
 */

using Server.Items;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class Poisoning
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Poisoning].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTargetPoison();

            m.SendLocalizedMessage(502137); // Select the poison you wish to use

            return TimeSpan.FromSeconds(1.0); // pla: 1 second default delay - 10 seconds is applied OnTarget()
        }

        private class InternalTargetPoison : Target
        {
            public InternalTargetPoison()
                : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BasePoisonPotion)
                {
                    from.SendLocalizedMessage(502142); // To what do you wish to apply the poison?
                    from.Target = new InternalTarget((BasePoisonPotion)targeted);
                }
                else // Not a Poison Potion
                {
                    from.SendLocalizedMessage(502139); // That is not a poison potion.
                }

            }

            private class InternalTarget : Target
            {
                private BasePoisonPotion m_Potion;

                public InternalTarget(BasePoisonPotion potion)
                    : base(2, false, TargetFlags.None)
                {
                    m_Potion = potion;
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    if (m_Potion.Deleted)
                        return;

                    bool startTimer = false;

                    if (targeted is Food || targeted is BaseBeverage)
                    {
                        startTimer = true;
                    }
                    else if (targeted is BaseWeapon)
                    {
                        BaseWeapon weapon = (BaseWeapon)targeted;

                        if (Core.AOS)
                        {
                            startTimer = (weapon.PrimaryAbility == WeaponAbility.InfectiousStrike || weapon.SecondaryAbility == WeaponAbility.InfectiousStrike);
                        }
                        else if (weapon.Layer == Layer.OneHanded)
                        {
                            // Only Bladed or Piercing weapon can be poisoned
                            startTimer = (weapon.Type == WeaponType.Slashing || weapon.Type == WeaponType.Piercing);

                            //special case for stinger - can't be poisoned.
                            if (weapon is AIStinger)
                            {
                                startTimer = false;
                            }
                        }
                    }
                    else if (!Core.UOSP && targeted is Cloth)
                    {
                        // only a single piece of cloth may be poisoned
                        if (((Cloth)targeted).Amount > 1)
                            from.SendMessage("You can only poison a single cloth at a time.");
                        else
                            startTimer = true;
                    }

                    if (startTimer)
                    {
                        new InternalTimer(from, (Item)targeted, m_Potion).Start();

                        from.PlaySound(0x4F);
                        m_Potion.Delete();

                        from.AddToBackpack(new Bottle());
                    }
                    else // Target can't be poisoned
                    {
                        if (!(targeted is Cloth))
                        {
                            if (Core.AOS)
                                from.SendLocalizedMessage(1060204); // You cannot poison that! You can only poison infectious weapons, food or drink.
                            else
                                from.SendMessage("You cannot poison that! You can only poison bladed or piercing weapons, food, drink, or cloth.");
                            // Because you can now poison cloth, I had to use an unlocalized message to reflect this
                            //from.SendLocalizedMessage( 502145 ); // You cannot poison that! You can only poison bladed or piercing weapons, food or drink.
                        }
                    }

                }

                private class InternalTimer : Timer
                {
                    private Mobile m_From;
                    private Item m_Target;
                    private Poison m_Poison;
                    private double m_MinSkill, m_MaxSkill;

                    public InternalTimer(Mobile from, Item target, BasePoisonPotion potion)
                        : base(TimeSpan.FromSeconds(0.0))
                    {
                        m_From = from;
                        m_Target = target;
                        m_Poison = potion.Poison;
                        m_MinSkill = potion.MinPoisoningSkill;
                        m_MaxSkill = potion.MaxPoisoningSkill;
                        Priority = TimerPriority.TwoFiftyMS;
                    }

                    protected override void OnTick()
                    {
                        string DisplayName = "rag";
                        if (m_From.CheckTargetSkill(SkillName.Poisoning, m_Target, m_MinSkill, m_MaxSkill))
                        {
                            if (m_Target is Food)
                            {
                                ((Food)m_Target).Poison = m_Poison;
                                DisplayName = "food item";
                            }
                            else if (m_Target is BaseBeverage)
                            {
                                ((BaseBeverage)m_Target).Poison = m_Poison;
                                if (((BaseBeverage)m_Target).Quantity > 3)
                                {
                                    m_From.SendMessage("You pour out some of the contents to make room for the poison.");
                                    ((BaseBeverage)m_Target).Quantity = 3;
                                }
                                DisplayName = "beverage item";
                            }
                            else if (m_Target is BaseWeapon)
                            {
                                ((BaseWeapon)m_Target).Poison = m_Poison;
                                ((BaseWeapon)m_Target).PoisonCharges = 18 - (m_Poison.Level * 2);
                                DisplayName = ((BaseWeapon)m_Target).ItemData.Name;
                            }
                            else if (!Core.UOSP && m_Target is Cloth)
                            {
                                // Only create the poison rag if the cloth is a single piece
                                if (((Cloth)m_Target).Amount == 1)
                                {
                                    // delete the cloth
                                    ((Cloth)m_Target).Delete();

                                    // Create the poison soaked rag (PoisonCloth type)
                                    PoisonCloth PCloth = new PoisonCloth();

                                    // Standard weapon poisoning parameters
                                    PCloth.Poison = m_Poison;
                                    PCloth.PoisonCharges = 18 - (m_Poison.Level * 2);
                                    // This is where to adjust the additional delay for time it 
                                    // takes to poison an arrow before it can be fired
                                    PCloth.Delay = 0;

                                    // Add the poison soaked rag to players backpack
                                    m_From.AddToBackpack(PCloth);
                                }
                            }
                            switch (m_Poison.Level)
                            {
                                case 0:
                                    m_From.SendMessage("You apply the lesser poison to the " + DisplayName);
                                    break;
                                case 1:
                                    m_From.SendMessage("You apply the poison to the " + DisplayName);
                                    break;
                                case 2:
                                    m_From.SendMessage("You apply the greater poison to the " + DisplayName);
                                    break;
                                case 3:
                                    m_From.SendMessage("You apply the deadly poison to the " + DisplayName);
                                    break;
                                case 4:
                                    m_From.SendMessage("You apply the lethal poison to the " + DisplayName);
                                    break;
                                default:
                                    m_From.SendMessage("You apply the poison to the " + DisplayName);
                                    break;
                            }
                            // Removed localized poisoning message and replaced with a 
                            // more specific message based on the poison used
                            //m_From.SendLocalizedMessage( 1010517 ); // You apply the poison

                            Misc.Titles.AwardKarma(m_From, -20, true);
                        }
                        else // Failed
                        {
                            // 5% of chance of getting poisoned if failed
                            if (m_From.Skills[SkillName.Poisoning].Base < 80.0 && Utility.Random(20) == 0)
                            {
                                m_From.SendLocalizedMessage(502148); // You make a grave mistake while applying the poison.
                                m_From.ApplyPoison(m_From, m_Poison);
                            }
                            else
                            {
                                if (m_Target is Food || m_Target is BaseBeverage)
                                {
                                    m_From.SendLocalizedMessage(1010518); // You fail to apply a sufficient dose of poison
                                }
                                else if (m_Target is BaseWeapon)
                                {
                                    BaseWeapon weapon = (BaseWeapon)m_Target;

                                    if (weapon.Type == WeaponType.Slashing)
                                    {
                                        m_From.SendLocalizedMessage(1010516); // You fail to apply a sufficient dose of poison on the blade
                                    }
                                    else
                                    {
                                        m_From.SendLocalizedMessage(1010518); // You fail to apply a sufficient dose of poison
                                    }
                                }
                                else if (m_Target is Cloth)
                                {
                                    m_From.SendMessage("You fail to apply a sufficient dose of poison to soak the rag.");
                                }
                            }
                        }
                        //pla: set 10 second delay before next skill use
                        m_From.NextSkillTime = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);
                    }
                }
            }
        }
    }
}
