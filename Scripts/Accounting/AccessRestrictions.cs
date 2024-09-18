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

/* Scripts/Accounting/AccountHandler.cs
 * ChangeLog:
 *	2/27/06 - Pix.
 *		Added.
 */

using Server.Misc;
using System;
using System.IO;
using System.Net;

namespace Server
{
    public class AccessRestrictions
    {
        public static void Initialize()
        {
            EventSink.SocketConnect += new SocketConnectEventHandler(EventSink_SocketConnect);
        }

        private static void EventSink_SocketConnect(SocketConnectEventArgs e)
        {
            try
            {
                IPAddress ip = ((IPEndPoint)e.Socket.RemoteEndPoint).Address;

                if (Firewall.IsBlocked(ip))
                {
                    Utility.Monitor.WriteLine("Client: {0}: Firewall blocked connection attempt.", ConsoleColor.Red, ip);
                    e.AllowConnection = false;
                    return;
                }
                else if (IPLimiter.SocketBlock && !IPLimiter.Verify(ip))
                {
                   Utility.Monitor.WriteLine("Client: {0}: Past IP limit threshold", ConsoleColor.Red, ip);

                    using (StreamWriter op = new StreamWriter("ipLimits.log", true))
                        op.WriteLine("{0}\tPast IP limit threshold\t{1}", ip, DateTime.UtcNow);

                    e.AllowConnection = false;
                    return;
                }
            }
            catch
            {
                e.AllowConnection = false;
            }
        }
    }
}
