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

/* Server/Timer.cs
 * Changelog:
 *	12/23/08, Adam
 *		Add missing timer synchronization from RunUO 2.0
 *  7/5/08, Adam
 *      Add fall-through exception handling around OnTick()
 *  6/14/07, Adam
 *      Sometimes on server restart players are not able to logon. I suspect that the timer queue is kicking off 
 *      before the server is fully up and causing the problem. I've added an initial (one time) 5 second sleep to the 
 *      timer queuing logic to ensure we are alive before beginning to process messages.
 *      - exit the timer queue processing if we exceed DateTime.UtcNow < breakTime time
 *  2/26/07, Adam
 *      I found we were running timers AFTER Stop() is called
 *      This is apparently supported behavior, and actually makes some sense if you 
 *      stop and think about it. However, there are times when you want Stop() to mean
 *      STOP. For this, we've added timer.Flush() to eat next the queued event (only.)
 *      See: Spawner.cs for an example and explanation.
 *  05/31/06, Adam
 *		Make debug message conditioned on DEBUG so we don't spam the console on prod
 *  05/30/06, Adam
 *		Send console output when we are execting a timer AFTER Stop()
 *		We need to look into this odd behavior.
 *		Search key: "if (t.m_Running == false)"
 */

using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace Server
{
    public enum TimerPriority
    {
        EveryTick,
        TenMS,
        TwentyFiveMS,
        FiftyMS,
        TwoFiftyMS,
        OneSecond,
        FiveSeconds,
        OneMinute
    }

    public delegate void TimerCallback();
    public delegate void TimerStateCallback(object state);

    public class TimerProfile
    {
        private int m_Created;
        private int m_Started;
        private int m_Stopped;
        private int m_Ticked;
        private TimeSpan m_TotalProcTime;
        private TimeSpan m_PeakProcTime;

        [CommandProperty(AccessLevel.Administrator)]
        public int Created
        {
            get { return m_Created; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int Started
        {
            get { return m_Started; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int Stopped
        {
            get { return m_Stopped; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int Ticked
        {
            get { return m_Ticked; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan TotalProcTime
        {
            get { return m_TotalProcTime; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan PeakProcTime
        {
            get { return m_PeakProcTime; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan AverageProcTime
        {
            get
            {
                if (m_Ticked == 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromTicks(m_TotalProcTime.Ticks / m_Ticked);
            }
        }

        public void RegCreation()
        {
            ++m_Created;
        }

        public void RegStart()
        {
            ++m_Started;
        }

        public void RegStopped()
        {
            ++m_Stopped;
        }

        public void RegTicked(TimeSpan procTime)
        {
            ++m_Ticked;
            m_TotalProcTime += procTime;

            if (procTime > m_PeakProcTime)
                m_PeakProcTime = procTime;
        }

        public TimerProfile()
        {
        }
    }

    public class Timer
    {
        private DateTime m_Next;
        private TimeSpan m_Delay;
        private TimeSpan m_Interval;
        private bool m_Running;
        private int m_Index, m_Count;
        private TimerPriority m_Priority;
        private ArrayList m_List;
        private bool m_Eat = false;

        public bool Eat
        {
            get { return m_Eat; }
            set { m_Eat = value; }
        }

        public bool Flush()
        {
            if (m_Queued == false)
                return false;

            // eat the next tick in the queue
            return m_Eat = true;
        }

        public DateTime NextTick { get { return m_Next; } }

        private static string FormatDelegate(Delegate callback)
        {
            if (callback == null)
                return "null";

            return String.Format("{0}.{1}", callback.Method.DeclaringType.FullName, callback.Method.Name);
        }

        public static void DumpInfo(TextWriter tw)
        {
            TimerThread.DumpInfo(tw);
        }

        public TimerPriority Priority
        {
            get
            {
                return m_Priority;
            }
            set
            {
                if (m_Priority != value)
                {
                    m_Priority = value;

                    if (m_Running)
                        TimerThread.PriorityChange(this, (int)m_Priority);
                }
            }
        }

        public TimeSpan Delay
        {
            get
            {
                return m_Delay;
            }
            set
            {
                m_Delay = value;
            }
        }

        public TimeSpan Interval
        {
            get
            {
                return m_Interval;
            }
            set
            {
                m_Interval = value;
            }
        }

        public bool Running
        {
            get
            {
                return m_Running;
            }
            set
            {
                if (value)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
        }

        private static Hashtable m_Profiles = new Hashtable();

        public static Hashtable Profiles { get { return m_Profiles; } }

        public TimerProfile GetProfile()
        {
            if (!Core.Profiling)
                return null;

            string name = ToString();

            if (name == null)
                name = "null";

            TimerProfile prof = (TimerProfile)m_Profiles[name];

            if (prof == null)
                m_Profiles[name] = prof = new TimerProfile();

            return prof;
        }

        public class TimerThread
        {
            private static Queue m_ChangeQueue = Queue.Synchronized(new Queue());

            private static DateTime[] m_NextPriorities = new DateTime[8];
            private static TimeSpan[] m_PriorityDelays = new TimeSpan[8]
            {
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds( 10.0 ),
                TimeSpan.FromMilliseconds( 25.0 ),
                TimeSpan.FromMilliseconds( 50.0 ),
                TimeSpan.FromMilliseconds( 250.0 ),
                TimeSpan.FromSeconds( 1.0 ),
                TimeSpan.FromSeconds( 5.0 ),
                TimeSpan.FromMinutes( 1.0 )
            };

            private static ArrayList[] m_Timers = new ArrayList[8]
            {
                new ArrayList(),
                new ArrayList(),
                new ArrayList(),
                new ArrayList(),
                new ArrayList(),
                new ArrayList(),
                new ArrayList(),
                new ArrayList()
            };

            public static void DumpInfo(TextWriter tw)
            {
                for (int i = 0; i < 8; ++i)
                {
                    tw.WriteLine("Priority: {0}", (TimerPriority)i);
                    tw.WriteLine();

                    Hashtable hash = new Hashtable();

                    for (int j = 0; j < m_Timers[i].Count; ++j)
                    {
                        Timer t = (Timer)m_Timers[i][j];
                        string key = t.ToString();

                        ArrayList list = (ArrayList)hash[key];

                        if (list == null)
                            hash[key] = list = new ArrayList();

                        list.Add(t);
                    }

                    foreach (DictionaryEntry de in hash)
                    {
                        string key = (string)de.Key;
                        ArrayList list = (ArrayList)de.Value;

                        tw.WriteLine("Type: {0}; Count: {1}; Percent: {2}%", key, list.Count, (int)(100 * (list.Count / (double)m_Timers[i].Count)));
                    }

                    tw.WriteLine();
                    tw.WriteLine();
                }
            }

            private class TimerChangeEntry
            {
                public Timer m_Timer;
                public int m_NewIndex;
                public bool m_IsAdd;

                private TimerChangeEntry(Timer t, int newIndex, bool isAdd)
                {
                    m_Timer = t;
                    m_NewIndex = newIndex;
                    m_IsAdd = isAdd;
                }

                public void Free()
                {
                    //m_InstancePool.Enqueue( this );
                }

                private static Queue m_InstancePool = new Queue();

                public static TimerChangeEntry GetInstance(Timer t, int newIndex, bool isAdd)
                {
                    TimerChangeEntry e;

                    if (m_InstancePool.Count > 0)
                    {
                        e = (TimerChangeEntry)m_InstancePool.Dequeue();

                        if (e == null)
                            e = new TimerChangeEntry(t, newIndex, isAdd);
                        else
                        {
                            e.m_Timer = t;
                            e.m_NewIndex = newIndex;
                            e.m_IsAdd = isAdd;
                        }
                    }
                    else
                    {
                        e = new TimerChangeEntry(t, newIndex, isAdd);
                    }

                    return e;
                }
            }

            public TimerThread()
            {
            }

            public static void Change(Timer t, int newIndex, bool isAdd)
            {
                m_ChangeQueue.Enqueue(TimerChangeEntry.GetInstance(t, newIndex, isAdd));
            }

            public static void AddTimer(Timer t)
            {
                Change(t, (int)t.Priority, true);
            }

            public static void PriorityChange(Timer t, int newPrio)
            {
                Change(t, newPrio, false);
            }

            public static void RemoveTimer(Timer t)
            {
                Change(t, -1, false);
            }

            private static void ProcessChangeQueue()
            {
                while (m_ChangeQueue.Count > 0)
                {
                    TimerChangeEntry tce = (TimerChangeEntry)m_ChangeQueue.Dequeue();
                    Timer timer = tce.m_Timer;
                    int newIndex = tce.m_NewIndex;

                    if (timer.m_List != null)
                        timer.m_List.Remove(timer);

                    if (tce.m_IsAdd)
                    {
                        timer.m_Next = DateTime.UtcNow + timer.m_Delay;
                        timer.m_Index = 0;
                    }

                    if (newIndex >= 0)
                    {
                        timer.m_List = m_Timers[newIndex];
                        timer.m_List.Add(timer);
                    }
                    else
                    {
                        timer.m_List = null;
                    }

                    tce.Free();
                }
            }

            private static AutoResetEvent m_Signal = new AutoResetEvent(false);
            public static void Set() { m_Signal.Set(); }

            public void TimerMain()
            {
                DateTime now;
                int i, j;
                bool loaded;

                while (!Core.Closing)
                {
                    ProcessChangeQueue();

                    loaded = false;

                    for (i = 0; i < m_Timers.Length; i++)
                    {
                        now = DateTime.UtcNow;
                        if (now < m_NextPriorities[i])
                            break;

                        m_NextPriorities[i] = now + m_PriorityDelays[i];

                        for (j = 0; j < m_Timers[i].Count; j++)
                        {
                            Timer t = (Timer)m_Timers[i][j];

                            if (!t.m_Queued && now > t.m_Next)
                            {
                                t.m_Queued = true;

                                lock (m_Queue)
                                    m_Queue.Enqueue(t);

                                loaded = true;

                                if (t.m_Count != 0 && (++t.m_Index >= t.m_Count))
                                {
                                    t.Stop();
                                }
                                else
                                {
                                    t.m_Next = now + t.m_Interval;
                                }
                            }
                        }
                    }

                    if (loaded)
                        Core.Set();

                    m_Signal.WaitOne(10, false);

                }
            }
        }

        private static Queue m_Queue = new Queue();
        private static int m_BreakCount = 20000;
        private static TimeSpan m_BreakTime = TimeSpan.FromMilliseconds(250);

        public static int BreakCount { get { return m_BreakCount; } set { m_BreakCount = value; } }

        public static Queue Queue
        {
            get
            {
                return m_Queue;
            }
        }

        private static int m_QueueCountAtSlice;

        public static int QueueCountAtSlice
        {
            get { return m_QueueCountAtSlice; }
        }

        private bool m_Queued;

        public static void Slice()
        {
            lock (m_Queue)
            {
                m_QueueCountAtSlice = m_Queue.Count;

                int index = 0;
                DateTime start = DateTime.MinValue;
                DateTime breakTime = DateTime.UtcNow + m_BreakTime;

                while (index < m_BreakCount && DateTime.UtcNow < breakTime && m_Queue.Count != 0)
                {
                    Timer t = (Timer)m_Queue.Dequeue();
                    TimerProfile prof = t.GetProfile();

                    if (prof != null)
                        start = DateTime.UtcNow;

                    // Adam: See comments at top of file regarding timer.Flush()
                    if (t.Eat == true)
                        t.Eat = false;
                    else
                        try { t.OnTick(); }
                        catch (Exception e)
                        {
                            // Log an exception
                            EventSink.InvokeLogException(new LogExceptionEventArgs(e));
                        }

                    t.m_Queued = false;
                    ++index;

                    if (prof != null)
                        prof.RegTicked(DateTime.UtcNow - start);

                }//while !empty

                if (index >= m_BreakCount)
                    Console.WriteLine("Timer.Slice: index >= m_BreakCount");

                if (DateTime.UtcNow >= breakTime)
                    Console.WriteLine("Timer.Slice: DateTime.UtcNow >= breakTime");
            }
        }

        public Timer(TimeSpan delay)
            : this(delay, TimeSpan.Zero, 1)
        {
        }

        public Timer(TimeSpan delay, TimeSpan interval)
            : this(delay, interval, 0)
        {
        }

        public virtual bool DefRegCreation
        {
            get { return true; }
        }

        public virtual void RegCreation()
        {
            TimerProfile prof = GetProfile();

            if (prof != null)
                prof.RegCreation();
        }

        public Timer(TimeSpan delay, TimeSpan interval, int count)
        {
            m_Delay = delay;
            m_Interval = interval;
            m_Count = count;

            if (DefRegCreation)
                RegCreation();
        }

        public override string ToString()
        {
            return GetType().FullName;
        }

        public static TimerPriority ComputePriority(TimeSpan ts)
        {
            if (ts >= TimeSpan.FromMinutes(1.0))
                return TimerPriority.FiveSeconds;

            if (ts >= TimeSpan.FromSeconds(10.0))
                return TimerPriority.OneSecond;

            if (ts >= TimeSpan.FromSeconds(5.0))
                return TimerPriority.TwoFiftyMS;

            if (ts >= TimeSpan.FromSeconds(2.5))
                return TimerPriority.FiftyMS;

            if (ts >= TimeSpan.FromSeconds(1.0))
                return TimerPriority.TwentyFiveMS;

            if (ts >= TimeSpan.FromSeconds(0.5))
                return TimerPriority.TenMS;

            return TimerPriority.EveryTick;
        }

        public static Timer DelayCall(TimeSpan delay, TimerCallback callback)
        {
            return DelayCall(delay, TimeSpan.Zero, 1, callback);
        }

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, TimerCallback callback)
        {
            return DelayCall(delay, interval, 0, callback);
        }

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, int count, TimerCallback callback)
        {
            Timer t = new DelayCallTimer(delay, interval, count, callback);

            if (count == 1)
                t.Priority = ComputePriority(delay);
            else
                t.Priority = ComputePriority(interval);

            t.Start();

            return t;
        }

        public static Timer DelayCall(TimeSpan delay, TimerStateCallback callback, object state)
        {
            return DelayCall(delay, TimeSpan.Zero, 1, callback, state);
        }

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, TimerStateCallback callback, object state)
        {
            return DelayCall(delay, interval, 0, callback, state);
        }

        public static Timer DelayCall(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback callback, object state)
        {
            Timer t = new DelayStateCallTimer(delay, interval, count, callback, state);

            if (count == 1)
                t.Priority = ComputePriority(delay);
            else
                t.Priority = ComputePriority(interval);

            t.Start();

            return t;
        }

        private class DelayCallTimer : Timer
        {
            private TimerCallback m_Callback;

            public TimerCallback Callback { get { return m_Callback; } }

            public override bool DefRegCreation { get { return false; } }

            public DelayCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerCallback callback)
                : base(delay, interval, count)
            {
                m_Callback = callback;
                RegCreation();
            }

            protected override void OnTick()
            {
                if (m_Callback != null)
                    m_Callback();
            }

            public override string ToString()
            {
                return String.Format("DelayCallTimer[{0}]", FormatDelegate(m_Callback));
            }
        }

        private class DelayStateCallTimer : Timer
        {
            private TimerStateCallback m_Callback;
            private object m_State;

            public TimerStateCallback Callback { get { return m_Callback; } }

            public override bool DefRegCreation { get { return false; } }

            public DelayStateCallTimer(TimeSpan delay, TimeSpan interval, int count, TimerStateCallback callback, object state)
                : base(delay, interval, count)
            {
                m_Callback = callback;
                m_State = state;

                RegCreation();
            }

            protected override void OnTick()
            {
                if (m_Callback != null)
                    m_Callback(m_State);
            }

            public override string ToString()
            {
                return String.Format("DelayStateCall[{0}]", FormatDelegate(m_Callback));
            }
        }

        public void Start()
        {
            if (!m_Running)
            {
                m_Running = true;
                TimerThread.AddTimer(this);

                TimerProfile prof = GetProfile();

                if (prof != null)
                    prof.RegStart();
            }
        }

        public void Stop()
        {
            if (m_Running)
            {
                m_Running = false;
                TimerThread.RemoveTimer(this);

                TimerProfile prof = GetProfile();

                if (prof != null)
                    prof.RegStopped();
            }
        }

        protected virtual void OnTick()
        {
        }
    }
}
