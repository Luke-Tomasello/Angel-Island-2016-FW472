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

/* Scripts/Engines/Pathing/PathFollower.cs
 * Changelog:
 *  06/02/06 Taran Kain
 *		Added logic to allow skipping SectorNode waypoints, to smooth out paths.
 *		Still could use a fresh look to clean things up.
 *  05/16/06 Adam
 *		Comment out console stuff :\
 *  05/15/06 Taran Kain
 *		Removed NavStar logic, replaced with SectorPathAlgorithm. Needs to be cleaned up.
 *  12/05/05 Kit
 *		Changes to support NavStar algorithem use vs FastAStar
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.PathAlgorithms.FastAStar;
using System;
using System.Collections;
using CalcMoves = Server.Movement.Movement;

namespace Server
{
    public class PathFollower
    {
        // Should we use pathfinding? 'false' for not
        private static bool Enabled = true;

        private Mobile m_From;
        private IPoint3D m_Goal;
        private MovementPath m_Path;
        private int m_Index;
        private Point3D m_Next, m_LastGoalLoc;
        private DateTime m_LastPathTime;
        private MoveMethod m_Mover;

        public MoveMethod Mover
        {
            get { return m_Mover; }
            set { m_Mover = value; }
        }

        public IPoint3D Goal
        {
            get { return m_Goal; }
        }

        public PathFollower(Mobile from, IPoint3D goal)
        {
            m_From = from;
            m_Goal = goal;
        }

        public MoveResult Move(Direction d)
        {
            if (m_Mover == null)
                return (m_From.Move(d) ? MoveResult.Success : MoveResult.Blocked);

            return m_Mover(d);
        }

        public Point3D GetGoalLocation(int i)
        {
            if (m_SectorPoints != null)
            {
                if (m_NextSectorPoint < 0 || m_NextSectorPoint >= m_SectorPoints.Length)
                {
                    m_SectorPoints = null;
                    return GetEndGoalLocation(i);
                }

                if (i < 0 || i + m_NextSectorPoint >= m_SectorPoints.Length)
                    return Point3D.Zero;

                return m_SectorPoints[m_NextSectorPoint + i];
            }
            else
                return GetEndGoalLocation(i);
        }

        public Point3D GetEndGoalLocation(int i)
        {
            if (i != 0)
                return Point3D.Zero;

            if (m_Goal is Item)
                return ((Item)m_Goal).GetWorldLocation();

            return new Point3D(m_Goal);
        }

        private static TimeSpan RepathDelay = TimeSpan.FromSeconds(2.0);

        public void Advance(ref Point3D p, int index)
        {
            if (m_Path != null && m_Path.Success)
            {
                Direction[] dirs = m_Path.Directions;

                if (index >= 0 && index < dirs.Length)
                {
                    int x = p.X, y = p.Y;

                    CalcMoves.Offset(dirs[index], ref x, ref y);

                    p.X = x;
                    p.Y = y;
                }
            }
        }

        public void ForceRepath()
        {
            m_Path = null;
            m_SectorPoints = null;
            m_LastPathTime = DateTime.UtcNow - RepathDelay;
        }

        private Point3D[] m_SectorPoints;
        private int m_NextSectorPoint;
        private Point3D m_LastEndGoalLoc;

        public bool CheckPath()
        {
            if (!Enabled)
                return false;

            bool repath = false;
            bool bigpath = false;

            Point3D goal = GetEndGoalLocation(0);

            // check if we need to use SectorPath, or if goal is close enough to use FastAStar
            if (!FastAStarAlgorithm.Instance.CheckCondition(m_From, m_From.Map, m_From.Location, goal) &&
                (m_SectorPoints == null || goal != m_LastEndGoalLoc) && (m_LastPathTime + RepathDelay) <= DateTime.UtcNow)
            {
                m_LastEndGoalLoc = goal;
                m_SectorPoints = Server.PathAlgorithms.Sector.SectorPathAlgorithm.FindWaypoints(m_From, m_From.Map, m_From.Location, goal);
                m_NextSectorPoint = 0;
            }

            goal = GetGoalLocation(0);

            if (bigpath || m_Path == null)
                repath = true;
            else if ((!m_Path.Success || goal != m_LastGoalLoc) && (m_LastPathTime + RepathDelay) <= DateTime.UtcNow)
                repath = true;
            else if (m_Path.Success && Check(m_From.Location, m_LastGoalLoc, 0))
                repath = true;

            if (!repath)
                return false;

            // go as far forward in the list of waypoints as we can reach
            // FastAStar paths will have only one stack entry
            Stack s = new Stack();
            int i = 1;
            do
            {
                s.Push(goal);
                goal = GetGoalLocation(i++);
            } while (goal != Point3D.Zero && FastAStarAlgorithm.Instance.CheckCondition(m_From, m_From.Map, m_From.Location, goal));

            // now find the farthest waypoint that we can path to, smooths and shortens pathing
            while (s.Count > 0)
            {
                goal = (Point3D)s.Pop();

                m_LastPathTime = DateTime.UtcNow;
                m_LastGoalLoc = goal;

                if ((m_Path = new MovementPath(m_From, goal)).Success)
                {
                    m_Index = 0;
                    m_Next = m_From.Location;
                    m_NextSectorPoint += s.Count;

                    Advance(ref m_Next, m_Index);
                    break;
                }
            }

            return true;
        }

        public bool Check(Point3D loc, Point3D goal, int range)
        {
            if (!Utility.InRange(loc, goal, range))
                return false;

            if (range <= 1 && Math.Abs(loc.Z - goal.Z) >= 16)
                return false;

            return true;
        }

        public bool Follow(bool run, int range)
        {
            Point3D goal = GetEndGoalLocation(0);
            Direction d;

            if (Check(m_From.Location, goal, range))
                return true;

            if (Check(m_From.Location, GetGoalLocation(0), 2))
                m_NextSectorPoint++;

            bool repathed = CheckPath();

            if (!Enabled || !m_Path.Success)
            {
                d = m_From.GetDirectionTo(goal);

                if (run)
                    d |= Direction.Running;

                m_From.SetDirection(d);
                Move(d);

                return Check(m_From.Location, goal, range);
            }

            d = m_From.GetDirectionTo(m_Next);

            if (run)
                d |= Direction.Running;

            m_From.SetDirection(d);

            MoveResult res = Move(d);

            if (res == MoveResult.Blocked)
            {
                if (repathed)
                    return false;

                m_Path = null;
                CheckPath();

                if (!m_Path.Success)
                {
                    d = m_From.GetDirectionTo(goal);

                    if (run)
                        d |= Direction.Running;

                    m_From.SetDirection(d);
                    Move(d);

                    return Check(m_From.Location, goal, range);
                }

                d = m_From.GetDirectionTo(m_Next);

                if (run)
                    d |= Direction.Running;

                m_From.SetDirection(d);

                res = Move(d);

                if (res == MoveResult.Blocked)
                    return false;
            }

            if (m_From.X == m_Next.X && m_From.Y == m_Next.Y)
            {
                if (m_From.Z == m_Next.Z)
                {
                    ++m_Index;
                    Advance(ref m_Next, m_Index);
                }
                else
                {
                    m_Path = null;
                }
            }

            return Check(m_From.Location, goal, range);
        }
    }
}
