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

namespace Server.Misc
{
    public class RenameRequests
    {
        public static void Initialize()
        {
            EventSink.RenameRequest += new RenameRequestEventHandler(EventSink_RenameRequest);
        }

        private static void EventSink_RenameRequest(RenameRequestEventArgs e)
        {
            Mobile from = e.From;
            Mobile targ = e.Target;
            string name = e.Name;

            if (from.CanSee(targ) && from.InRange(targ, 12) && targ.CanBeRenamedBy(from))
            {
                name = name.Trim();

                if (NameVerification.Validate(name, 1, 16, true, false, true, 0, NameVerification.Empty))
                    targ.Name = name;
                else
                    from.SendMessage("That name is unacceptable.");
            }
        }
    }
}
