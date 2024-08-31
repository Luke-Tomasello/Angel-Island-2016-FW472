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

/* Scripts/Mobiles/Vendors/GenericBuy.cs
 * ChangeLog
 *	2/13/11, Adam
 *		UOSP: houses are 10x the price
 *		http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
 *	6/11/07, Pix
 *		Made the args parameter to GenericBuyInfo constructors actually get hooked up!
 *	4/23/05, Pix
 *		Fixed internalmobiles cleanup stopping animal vendors from working.
 *		Now it re-creates the mobs if they were deleted by [internalmobiles
 *	4/19/05, Pix
 *		Merged in RunUO 1.0 release code.
 *		Fixes 'vendor buy' showing just 'Deed'
 *	2/7/05, Adam
 *		Leave previous try/catch, but relax the comment and debug message
 *  07/02/05 TK
 *		Added in null sanity check and error message in main GenericBuy constructor
 *	02/07/05, Adam
 *		More patches to stop server crashes...
 *  05/02/05 TK
 *		Made "I can give you a better price with comm deed" message only show if the
 *		price actually will be better with a commodity deed
 *  02/02/05 TK
 *		Put in check for ValidFailsafe, to prevent autogeneration of things like valorite
 *  01/31/05 TK
 *		Reworked RP logic to allow normal spawning of resources if there are 0 in system
 *	01/23/05, Taran Kain
 *		Added logic to support Resource Pool.
 *  10/18/04, Froste
 *      Reworked OnRestock in order to remove OnRestockReagents
 *      Added a parameter MinValue that can be set in GenericBuyInfo
 *      If MinValue or MAxValue are not specified, they will default to 20 and 100 respectively
 *  10/13/04, Froste
 *      Added a parameter MaxValue that can be set in GenericBuyInfo
 *      Reworked OnRestockReagents() routine to allow for differing MaxValues
 *	4/29/04
 *		Modified OnRestock() routine to never retsock more than 20 of any item.
 *		Created OnRestockReagents() routine, called by NPCs that need to restock regs (since they will restock up to 100 of each item)
 *
 *  
 *      
 */

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class GenericBuyInfo : IBuyItemInfo
    {
        private class DisplayCache : Container
        {
            private static DisplayCache m_Cache;

            public static DisplayCache Cache
            {
                get
                {
                    if (m_Cache == null || m_Cache.Deleted)
                        m_Cache = new DisplayCache();

                    return m_Cache;
                }
            }

            private Hashtable m_Table;
            private ArrayList m_Mobiles;

            public DisplayCache()
                : base(0)
            {
                m_Table = new Hashtable();
                m_Mobiles = new ArrayList();
            }

            public object Lookup(Type key)
            {
                return m_Table[key];
            }

            public void Store(Type key, object obj, bool cache)
            {
                if (cache)
                    m_Table[key] = obj;

                if (obj is Item)
                    AddItem((Item)obj);
                else if (obj is Mobile)
                    m_Mobiles.Add(obj);
            }

            public DisplayCache(Serial serial)
                : base(serial)
            {
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                for (int i = 0; i < m_Mobiles.Count; ++i)
                    ((Mobile)m_Mobiles[i]).Delete();

                m_Mobiles.Clear();

                for (int i = Items.Count - 1; i >= 0; --i)
                {
                    if (i < Items.Count)
                        ((Item)Items[i]).Delete();
                }

                if (m_Cache == this)
                    m_Cache = null;
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.WriteMobileList(m_Mobiles, true);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Mobiles = reader.ReadMobileList();

                for (int i = 0; i < m_Mobiles.Count; ++i)
                    ((Mobile)m_Mobiles[i]).Delete();

                m_Mobiles.Clear();

                for (int i = Items.Count - 1; i >= 0; --i)
                {
                    if (i < Items.Count)
                        ((Item)Items[i]).Delete();
                }

                if (m_Cache == null)
                    m_Cache = this;
                else
                    Delete();

                m_Table = new Hashtable();
            }
        }

        private Type m_Type;
        private string m_Name;
        private int m_Price;
        private int m_MaxAmount, m_MinAmount, m_Amount, m_RestockAmount;
        private int m_ItemID;
        private int m_Hue;
        private object[] m_Args;
        private object m_DisplayObject;

        public virtual int ControlSlots { get { return 0; } }

        public virtual bool CanCacheDisplay { get { return false; } } //return ( m_Args == null || m_Args.Length == 0 ); } 

        private bool IsDeleted(object obj)
        {
            if (obj is Item)
                return (obj as Item).Deleted;
            else if (obj is Mobile)
                return (obj as Mobile).Deleted;

            return false;
        }

        public object GetDisplayObject()
        {
            if (m_DisplayObject != null && !IsDeleted(m_DisplayObject))
                return m_DisplayObject;

            bool canCache = this.CanCacheDisplay;

            if (canCache)
                m_DisplayObject = DisplayCache.Cache.Lookup(m_Type);

            //Pix: we need to test for BOTH null and if it's deleted here!
            if (m_DisplayObject == null || IsDeleted(m_DisplayObject))
                m_DisplayObject = GetObject();

            DisplayCache.Cache.Store(m_Type, m_DisplayObject, canCache);

            return m_DisplayObject;
        }

        public Type Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public string Name
        {
            get
            {
                if (!ResourcePool.IsPooledResource(m_Type))
                    return m_Name;

                return ResourcePool.GetName(m_Type);
            }
            set { m_Name = value; }
        }

        public int DefaultPrice { get { return m_PriceScalar; } }

        private int m_PriceScalar;

        public int PriceScalar
        {
            get { return m_PriceScalar; }
            set { m_PriceScalar = value; }
        }

        public int Price
        {
            get
            {
                if (ResourcePool.IsPooledResource(m_Type))
                {
                    if (ResourcePool.GetTotalCount(m_Type) > 0)
                        return (int)Math.Ceiling(ResourcePool.GetResalePrice(m_Type));
                    else
                        return (int)Math.Ceiling(ResourcePool.GetResalePrice(m_Type) * ResourcePool.FailsafePriceHike);
                }

                int base_price = m_Price;

                // adjust for inflation (siege)
                // Question(11) on the boards .. what's the formula?
                if (!Core.UOAI && !Core.UOAR && !Core.UOMO && PublishInfo.Publish < 11)
                {
                    double delta = (double)(m_RestockAmount - m_Amount);
                    double percent = ((delta / m_RestockAmount) * 100.00) / 100;
                    if (percent >= .3)                                              // stock is down 30%
                        base_price += (int)((double)base_price * (percent / 4.0));  // about a 12% markup
                }

                if (m_PriceScalar != 0)
                {
                    if (base_price > 5000000)
                    {
                        long price = base_price;

                        price *= m_PriceScalar;
                        price += 50;
                        price /= 100;

                        if (price > int.MaxValue)
                            price = int.MaxValue;

                        return (int)price;
                    }

                    return (((base_price * m_PriceScalar) + 50) / 100);
                }

                return base_price;
            }
            set { m_Price = value; }
        }

        public int ItemID
        {
            get
            {
                if (!ResourcePool.IsPooledResource(m_Type))
                    return m_ItemID;

                return ResourcePool.GetItemID(m_Type);
            }
            set { m_ItemID = value; }
        }

        public int Hue
        {
            get
            {
                if (!ResourcePool.IsPooledResource(m_Type))
                    return m_Hue;

                return ResourcePool.GetHue(m_Type);
            }
            set { m_Hue = value; }
        }

        public int Amount
        {
            get
            {
                // Adam: Sanity - Why is this NULL?
                if (m_Type == null)
                {
                    System.Console.WriteLine("Error with GetBuyInfo.Amount");
                    System.Console.WriteLine("In Amount.get(): (m_Type == null)");
                    return 0;
                }
                if (ResourcePool.IsPooledResource(m_Type) && (ResourcePool.Resources[m_Type] is RDRedirect))
                    return 0;
                if (ResourcePool.IsPooledResource(m_Type) && ResourcePool.GetTotalCount(m_Type) > 0)
                    return ResourcePool.GetTotalCount(m_Type);
                else if (ResourcePool.IsPooledResource(m_Type) && !ResourcePool.GetValidFailsafe(m_Type))
                    return 0;
                else
                    return m_Amount;
            }
            // don't worry about resourcepool here, BaseVendor calls SellOff()
            set { if (value < 0) value = 0; m_Amount = value; }
        }

        public int BunchPrice
        {
            get
            {
                if (ResourcePool.IsPooledResource(m_Type))
                {
                    if (ResourcePool.GetTotalCount(m_Type) > 0)
                        return (int)ResourcePool.GetResalePrice(m_Type) * 100;
                    else
                        return (int)(ResourcePool.GetResalePrice(m_Type) * ResourcePool.FailsafePriceHike * 100);
                }

                return -1;
            }
        }

        public string BunchName
        {
            get
            {
                if (ResourcePool.IsPooledResource(m_Type))
                    return ResourcePool.GetBunchName(m_Type);

                return null;
            }
        }

        public int MinAmount
        {
            get { return m_MinAmount; }
            set { m_MinAmount = value; }
        }

        public int MaxAmount
        {
            get { return m_MaxAmount; }
            set { m_MaxAmount = value; }
        }

        public object[] Args
        {
            get { return m_Args; }
            set { m_Args = value; }
        }

        public GenericBuyInfo(Type type, int price, int amount, int min_amount, int max_amount, int itemID, int hue)
            : this(null, type, price, amount, min_amount, max_amount, itemID, hue, null)
        {
            MaxAmount = max_amount;
            MinAmount = min_amount;
        }

        public GenericBuyInfo(Type type, int price, int amount, int itemID, int hue)
            : this(null, type, price, amount, 0, 0, itemID, hue, null)
        {
        }

        public GenericBuyInfo(string name, Type type, int price, int amount, int itemID, int hue)
            : this(name, type, price, amount, 0, 0, itemID, hue, null)
        {
        }

        public GenericBuyInfo(Type type, int price, int amount, int itemID, int hue, object[] args)
            : this(null, type, price, amount, 0, 0, itemID, hue, args)
        {
        }

        public GenericBuyInfo(string name, Type type, int price, int amount, int min_amount, int max_amount, int itemID, int hue, object[] args)
        {
            //amount = 20;

            m_Type = type;
            if (m_Type == null)
            {
                Console.WriteLine();
                Console.WriteLine("***WARNING***: GenericBuy constructor passed null for m_Type! BAD!");
                Console.WriteLine("Item ID of offending object: {0:X}", itemID);
                Console.WriteLine("Search all files for that ID (and possibly decimal version!) and look for any that pass null to this constructor.");
                Console.WriteLine("Setting type to recall rune...");
                Console.WriteLine();
                m_Type = typeof(Server.Items.RecallRune);
            }
            m_Price = price;

            if (Core.UOSP)
                m_Price = GenericBuyInfo.ComputeSiegeMarkup(m_Price);

            m_Amount = amount;
            m_ItemID = itemID;
            m_Hue = hue;

            m_Args = args;

            if (max_amount == 0)
                m_MaxAmount = 100;
            else
                m_MaxAmount = max_amount;

            if (min_amount == 0)
                m_MinAmount = 20;
            else
                m_MinAmount = min_amount;

            m_RestockAmount = amount;

            if (name == null)
                m_Name = (1020000 + (itemID & 0x3FFF)).ToString();
            else
                m_Name = name;

        }

        public GenericBuyInfo(Type type)
        {
            if (!Core.UOSP)
            {
                if (!ResourcePool.IsPooledResource(type))
                    throw new Exception(type.FullName + " is not a pooled resource.");
            }
            m_Type = type; // will load all props dynamically from ResourcePool

            m_MinAmount = 20;
            m_MaxAmount = 100;
            m_Amount = m_RestockAmount = 20;
        }

        //get a new instance of an object (we just bought it)
        public virtual object GetObject()
        {
            if (m_Args == null || m_Args.Length == 0)
                return Activator.CreateInstance(m_Type);

            return Activator.CreateInstance(m_Type, m_Args);
            //return (Item)Activator.CreateInstance( m_Type );
        }

        //Attempt to restock with item, (return true if restock sucessful)
        public bool Restock(Item item, int amount)
        {
            return false;
            /*if ( item.GetType() == m_Type )
			{
				if ( item is BaseWeapon )
				{
					BaseWeapon weapon = (BaseWeapon)item;

					if ( weapon.Quality == WeaponQuality.Low || weapon.Quality == WeaponQuality.Exceptional || (int)weapon.DurabilityLevel > 0 || (int)weapon.DamageLevel > 0 || (int)weapon.AccuracyLevel > 0 )
						return false;
				}

				if ( item is BaseArmor )
				{
					BaseArmor armor = (BaseArmor)item;

					if ( armor.Quality == ArmorQuality.Low || armor.Quality == ArmorQuality.Exceptional || (int)armor.Durability > 0 || (int)armor.ProtectionLevel > 0 )
						return false;
				}

				m_Amount += amount;

				return true;
			}
			else
			{
				return false;
			}*/
        }

        public void OnRestock()
        {
            if (m_Amount <= 0)  // Do we half the stock or double it?
            {
                m_RestockAmount *= 2;
                m_Amount = m_RestockAmount;
            }
            else
                m_RestockAmount /= 2;

            m_Amount = m_RestockAmount;

            if (m_Amount < m_MinAmount) // never below minimum nor above maximum
                m_Amount = m_MinAmount;
            else if (m_Amount > m_MaxAmount)
                m_Amount = m_MaxAmount;

            m_RestockAmount = m_Amount; //update restock_amount
        }

        public static int ComputeSiegeMarkup(int price)
        {
            int total = 0;
            if (Core.UOSP)
            {
                // houses are 10x the price for siege from 13.6 on, just * 3 before that
                // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                // See also Real estate Broker buy back
                if (PublishInfo.Publish >= 13.6)
                    total = price * 10;
                else
                    total = price * 3;
            }

            return total;
        }
    }
}
