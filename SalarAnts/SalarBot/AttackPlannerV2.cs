using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts.Classes;
using SalarAnts.Defination;
using SalarAnts.Pack;

namespace SalarAnts.SalarBot
{
    /// <summary>
    /// Influence map based on matrix
    /// </summary>
    public class AttackPlannerV2
    {
        public static GameState _state;
        public static PlanManagerV2 planMan;

        const int InfDanger = -1;
        const int InfAnt = -5;
        const int MapInflunceLength = 9;

        //  0= no influnce, 1..10=influnce of ant, -1= danger to influnce, -5= enemy ant 
        static int influenceMapRows;
        static int influenceMapCols;
        static int[,] enemyInfluenceMap;
        static int[,] myInfluenceMap;
        static int[,] antInfluenceMatrix =
            {
                { 0, 0, 0,-1,-1,-1, 0, 0, 0},
                { 0, 0,-1, 1, 1, 1,-1, 0, 0},
                { 0,-1, 1, 1, 1, 1, 1,-1, 0},
                {-1, 1, 1, 1, 1, 1, 1, 1,-1},
                {-1, 1, 1, 1, 1, 1, 1, 1,-1},
                {-1, 1, 1, 1, 1, 1, 1, 1,-1},
                { 0,-1, 1, 1, 1, 1, 1,-1, 0},
                { 0, 0,-1, 1, 1, 1,-1, 0, 0},
                { 0, 0, 0,-1,-1,-1, 0, 0, 0},
            };
        //static int[,] antInfluenceMatrix =
        //    {
        //        { 0, 0, 1, 1, 1, 0, 0},
        //        {0, 1, 1, 1, 1, 1, 0},
        //        {1, 1, 1, 1, 1, 1, 1},
        //        {1, 1, 1, 1, 1, 1, 1},
        //        {1, 1, 1, 1, 1, 1, 1},
        //        {0, 1, 1, 1, 1, 1, 0},
        //        {0, 0, 1, 1, 1, 0, 0}
        //    };

        #region influnced map
        public static void InitInfluenceMap(GameState state)
        {
            influenceMapRows = state.Height;
            influenceMapCols = state.Width;

            if (enemyInfluenceMap == null)
                enemyInfluenceMap = new int[influenceMapCols, influenceMapRows];
            else
                ResetInfluenceMap(state, enemyInfluenceMap);

            if (myInfluenceMap == null)
                myInfluenceMap = new int[influenceMapCols, influenceMapRows];
            else
                ResetInfluenceMap(state, myInfluenceMap);

        }

        static void ResetInfluenceMap(GameState state, int[,] map)
        {
            for (int col = 0; col < influenceMapCols; col++)
                for (int row = 0; row < influenceMapRows; row++)
                {
                    map[col, row] = 0;
                }
        }

        static void ApplyAntToInfluenceMap(int[,] map, Location ant)
        {
            // already applied?
            if (map[ant.ColX, ant.RowY] == InfAnt)
                return;

            // this is ant cell
            map[ant.ColX, ant.RowY] = InfAnt;

            // => 9/2 = 4
            var infLen = MapInflunceLength / 2;

            int colIndex = -1;
            int rowIndex = -1;
            for (int col = ant.ColX - infLen; col <= ant.ColX + infLen; col++)
            {
                colIndex++;
                rowIndex = -1;
                for (int row = ant.RowY - infLen; row <= ant.RowY + infLen; row++)
                {
                    rowIndex++;

                    var rowY = row;
                    var colX = col;

                    if (rowY < 0)
                        rowY = influenceMapRows - Math.Abs(rowY);
                    else if (rowY >= influenceMapRows)
                        rowY = rowY - influenceMapRows;

                    if (colX < 0)
                        colX = influenceMapCols - Math.Abs(colX);
                    else if (colX >= influenceMapCols)
                        colX = colX - influenceMapCols;

                    var mapVal = map[colX, rowY];
                    if (mapVal == InfAnt)
                    {
                        // this is ant
                        continue;
                    }
                    else if (mapVal == InfDanger)
                    {
                        // this in danger cell, just replace it!
                        map[colX, rowY] = antInfluenceMatrix[colIndex, rowIndex];
                    }
                    else
                    {
                        var val = antInfluenceMatrix[colIndex, rowIndex];

                        // in danger set
                        if (val == InfDanger && mapVal == 0)
                        {
                            map[colX, rowY] = InfDanger;
                        }
                        else if (val == InfDanger)
                        {
                            // no change, since this is just danger
                        }
                        else
                        {
                            // add value
                            map[colX, rowY] = mapVal + val;
                        }
                    }
                }
            }
        }

        static int GetEnemyInfluence(Location ant)
        {
            return enemyInfluenceMap[ant.ColX, ant.RowY];
        }
        static int GetMyInfluence(Location ant)
        {
            return myInfluenceMap[ant.ColX, ant.RowY];
        }
        #endregion

        /// <summary>
        /// The attack or defence algorithm based on the influnced map
        /// </summary>
        public static bool ActionMyAntMeetsEnemy(
            Ant myAntMeetEnemy,
            IList<Location> aroundMyAntsInView,
            IList<Location> aroundEnemyAntsInView)
        {
            // distances of 
            var fogOfView = _state.FogOfView;
            var attackRadiusDanger = (_state.AttackRadius2 / 2) + 1;
            var attackRadiusClose = attackRadiusDanger + 1;
            var attackRadiusNeedAction = attackRadiusDanger + 2;
            var supportMyAntRadius = fogOfView;

            // apply the influences
            aroundEnemyAntsInView.ForEachAction(x => ApplyAntToInfluenceMap(enemyInfluenceMap, x));
            aroundMyAntsInView.ForEachAction(x => ApplyAntToInfluenceMap(myInfluenceMap, x));

            // my ant is in which influence
            var myAntInfluence = GetEnemyInfluence(myAntMeetEnemy);

            var bestEnemyAnt = aroundEnemyAntsInView.MaxValue(x =>
            {
                var inf = GetMyInfluence(x);
                if (inf == 0) return 1000000;
                return inf;
            });
            if (bestEnemyAnt == null)
                return false;

            var bestEnemyAntInf = GetMyInfluence(bestEnemyAnt);
            var bestEnemyAntDist = _state.GetDistanceDirect(bestEnemyAnt, myAntMeetEnemy);

            // around enemy!
            var myAntsInDangerDistance = aroundMyAntsInView.Where(x => _state.GetDistanceDirect(x, bestEnemyAnt) <= attackRadiusNeedAction);


            var myAntsCloseToEnemy = myAntsInDangerDistance.Where(x =>
            {
                var dist = _state.GetDistanceDirect(x, myAntMeetEnemy);
                if (dist <= bestEnemyAntDist)
                {
                    return true;
                }
                return false;
            });

            // they can attack!
            if (myAntsCloseToEnemy.Count >= 2)
            {
                // we have enough ant to attack

                foreach (var myAnt in myAntsCloseToEnemy)
                {
                    // chooce passabe location
                    var attackLocation = _state.GetDirectionLocationCloser(myAnt, bestEnemyAnt, bestEnemyAnt);

                    // cancel all other plans
                    // remeber, Attack plan is not reached here already! 
                    planMan.RemovePlannedAnt(myAnt);

                    // add new simple plan
                    planMan.AddPlan(new AttackPlanDetail
                    {
                        NextStep = attackLocation,
                        Ant = myAnt,
                        Enemy = bestEnemyAnt,
                        LeaderAnt = myAntMeetEnemy
                    }, PlanManagerV2.PlanType.Attack);
                }
                return true;

            }
            else
            {
                if (myAntInfluence == 0)
                {
                    //// chooce passabe location
                    //var attackLocation = _state.GetDirectionLocationCloser(myAntMeetEnemy, bestEnemyAnt, bestEnemyAnt);
                    //var attackLocInf = GetEnemyInfluence(attackLocation);

                    //if (attackLocInf == InfDanger || attackLocInf > 0)
                    //{
                    //    // HOLD!
                    //    // danger close!
                    //    return true;
                    //}

                    //// cancel all other plans
                    //// remeber, Attack plan is not reached here already! 
                    //planMan.RemovePlannedAnt(myAntMeetEnemy);

                    //// add new simple plan
                    //planMan.AddPlan(new AttackPlanDetail
                    //{
                    //    NextStep = attackLocation,
                    //    Ant = myAntMeetEnemy,
                    //    Enemy = bestEnemyAnt,
                    //    LeaderAnt = myAntMeetEnemy
                    //}, PlanManagerV2.PlanType.Attack);
                    return true;

                }
                else
                {
                    // evade!

                    // evade calculated
                    if (ActionEvadeFromPowerOfEnemyAnts(myAntMeetEnemy, aroundEnemyAntsInView, attackRadiusNeedAction))
                        return true;

                    // Evade only
                    if (ActionEvadeFromEnemyDirectional(myAntMeetEnemy, attackRadiusNeedAction, attackRadiusDanger, bestEnemyAnt))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The attack or defence algorithm based on the influnced map
        /// </summary>
        public static bool ActionMyAntMeetsEnemy__OOLD(
             Ant myAntMeetEnemy,
             IList<Location> aroundMyAntsInView,
             IList<Location> aroundEnemyAntsInView)
        {
            // distances of 
            var fogOfView = _state.FogOfView;
            var attackRadiusDanger = (_state.AttackRadius2 / 2) + 1;
            var attackRadiusClose = attackRadiusDanger + 1;
            var attackRadiusNeedAction = attackRadiusDanger + 2;
            var supportMyAntRadius = fogOfView;

            // apply the enemy to the influence map
            aroundEnemyAntsInView.ForEachAction(x => ApplyAntToInfluenceMap(enemyInfluenceMap, x));

            // closest enemy!
            var closestEnemyToMine = aroundEnemyAntsInView.MinValue(x => _state.GetDistanceDirect(x, myAntMeetEnemy));
            if (closestEnemyToMine == null)
                return false;
            var closestEnemyToMineDistance = _state.GetDistanceDirect(closestEnemyToMine, myAntMeetEnemy);

            // my ants around the enemy ant
            var myAntAroundCloseEnemy = CheckingAround.FindTargetsInDirectRadius(
                _state, closestEnemyToMine,
                // search radius
                attackRadiusClose,
                // is passable?
                _state.GetIsPassableSafe,
                // is taget?
                x =>
                {
                    var locTile = _state[x];
                    if (locTile == Tile.AntMine)
                        return true;
                    return false;
                });

            // my ant is in which influence
            var myAntInfluence = GetEnemyInfluence(myAntMeetEnemy);

            // danger factor!
            if (myAntInfluence >= 1)
            {
                // in danger!
                if (myAntAroundCloseEnemy.Count == 0)
                {
                    // evade!

                    // evade calculated
                    if (ActionEvadeFromPowerOfEnemyAnts(myAntMeetEnemy, aroundEnemyAntsInView, attackRadiusNeedAction))
                        return true;

                    // Evade only
                    if (ActionEvadeFromEnemyDirectional(myAntMeetEnemy, attackRadiusNeedAction, attackRadiusDanger, closestEnemyToMine))
                        return true;
                    return false;
                }
                else
                {
                    int involvedInAttack = 0;
                    int antAttackDone = 0;
                    foreach (var ant in myAntAroundCloseEnemy)
                    {
                        // more in danger?
                        if (myAntInfluence <= GetEnemyInfluence(ant))
                        {
                            involvedInAttack++;
                            continue;
                        }
                        if (planMan.GetPlanType(ant) != null)
                        {
                            var accPlan = planMan.GetPlanByAnt(ant, PlanManagerV2.PlanType.Attack) as AttackPlanDetail;
                            if (accPlan == null)
                                continue;
                            // checking next step influence
                            if (myAntInfluence <= GetEnemyInfluence(accPlan.NextStep))
                            {
                                antAttackDone++;
                            }
                        }
                    }


                    if ((involvedInAttack + antAttackDone) < 2)
                    {
                        // oops! not enough ant for attack!

                        // evade calculated
                        if (ActionEvadeFromPowerOfEnemyAnts(myAntMeetEnemy, myAntAroundCloseEnemy, attackRadiusNeedAction))
                            return true;

                        // Evade only
                        if (ActionEvadeFromEnemyDirectional(myAntMeetEnemy, attackRadiusNeedAction, attackRadiusDanger, closestEnemyToMine))
                            return true;
                        return false;
                    }
                    else
                    {
                        if (involvedInAttack < 2)
                        {
                            // HOLD!
                            // some ant are evading!
                            return true;
                        }
                        else
                        {
                            // SEND MINE TO ATTACK!
                            // chooce passabe location
                            var attackLocation = _state.GetDirectionLocationCloser(myAntMeetEnemy, closestEnemyToMine, closestEnemyToMine);
                            var attackLocInf = GetEnemyInfluence(attackLocation);

                            // only if not much influnced
                            //if (attackLocInf < myAntInfluence)
                            {

                                // cancel all other plans
                                // remeber, Attack plan is not reached here already! 
                                planMan.RemovePlannedAnt(myAntMeetEnemy);

                                // add new simple plan
                                planMan.AddPlan(new AttackPlanDetail
                                {
                                    NextStep = attackLocation,
                                    Ant = myAntMeetEnemy,
                                    Enemy = closestEnemyToMine,
                                    LeaderAnt = myAntMeetEnemy
                                }, PlanManagerV2.PlanType.Attack);
                            }
                            return true;
                        }
                    }
                }
            }
            else if (myAntInfluence == InfDanger)
            {
                // In danger! be careful

                // in danger!
                if (myAntAroundCloseEnemy.Count == 0)
                {
                    // HOLD!
                    return true;
                }
                else
                {
                    int involvedInAttack = 0;
                    foreach (var ant in myAntAroundCloseEnemy)
                    {
                        // more in danger?
                        if (myAntInfluence <= GetEnemyInfluence(ant))
                        {
                            involvedInAttack++;
                        }
                    }

                    if (involvedInAttack < 2)
                    {
                        // oops! not enough ant for attack!
                        // HOLD!
                        return true;
                    }
                    else
                    {
                        // SEND MINE TO ATTACK!
                        // chooce passabe location
                        var attackLocation = _state.GetDirectionLocationCloser(myAntMeetEnemy, closestEnemyToMine, closestEnemyToMine);
                        var attackLocInf = GetEnemyInfluence(attackLocation);

                        // only if not much influnced
                        //if (attackLocInf < myAntInfluence)
                        {

                            // cancel all other plans
                            // remeber, Attack plan is not reached here already! 
                            planMan.RemovePlannedAnt(myAntMeetEnemy);

                            // add new simple plan
                            planMan.AddPlan(new AttackPlanDetail
                            {
                                NextStep = attackLocation,
                                Ant = myAntMeetEnemy,
                                Enemy = closestEnemyToMine,
                                LeaderAnt = myAntMeetEnemy
                            }, PlanManagerV2.PlanType.Attack);
                        }
                        return true;
                    }
                }
            }
            else if (myAntInfluence == 0)
            {
                // GO Closer to the enemy!
                // chooce passabe location
                var attackLocation = _state.GetDirectionLocationCloser(myAntMeetEnemy, closestEnemyToMine, closestEnemyToMine);
                var attackLocInf = GetEnemyInfluence(attackLocation);

                if (attackLocInf == InfDanger || attackLocInf > 0)
                {
                    // HOLD!
                    // danger close!
                    return true;
                }

                // cancel all other plans
                // remeber, Attack plan is not reached here already! 
                planMan.RemovePlannedAnt(myAntMeetEnemy);

                // add new simple plan
                planMan.AddPlan(new AttackPlanDetail
                {
                    NextStep = attackLocation,
                    Ant = myAntMeetEnemy,
                    Enemy = closestEnemyToMine,
                    LeaderAnt = myAntMeetEnemy
                }, PlanManagerV2.PlanType.Attack);
                return true;
            }
            return false;
        }






        /// <summary>
        /// Which location is far from all of enemy
        /// </summary>
        static bool ActionEvadeFromPowerOfEnemyAnts(Ant myAntMeetEnemy, IList<Location> aroundEnemyAntsInAttack, int attackRadiusNeedAction)
        {
            // evade is required!
            var evadeLocation = EvadeFromPowerOfAnts(myAntMeetEnemy, attackRadiusNeedAction, aroundEnemyAntsInAttack);

            if (evadeLocation != null && !evadeLocation.EqualTo(myAntMeetEnemy))
            {
                evadeLocation = _state.GetDirectionLocationCloser(myAntMeetEnemy, evadeLocation, evadeLocation);

                // cancel all other plans!!!
                planMan.RemoveFreeAnt(myAntMeetEnemy);
                planMan.RemovePlannedAnt(myAntMeetEnemy);

                // add new simple plan
                planMan.AddPlan(new MovePlanDetail
                {
                    Ant = myAntMeetEnemy,
                    Dest = evadeLocation
                }, PlanManagerV2.PlanType.Move);

                return true;
            }
            return false;
        }

        /// <summary>
        ///  Attack or defence again enemies
        /// </summary>
        /// <param name="thisAnt">The ants goinf to be cheched!</param>
        static Location EvadeFromPowerOfAnts(Location thisAnt, int radiusForEnemy, IList<Location> enemyAnts)
        {
            // the initial value is the location of my ant!
            int finalX = thisAnt.ColX;
            int finalY = thisAnt.RowY;

            foreach (Location enemyAnt in enemyAnts)
            {
                // more than this distance does not affect
                if (Math.Abs(thisAnt.ColX - enemyAnt.ColX) > radiusForEnemy)
                    continue;

                // more than this distance does not affect
                if (Math.Abs(thisAnt.RowY - enemyAnt.RowY) > radiusForEnemy)
                    continue;

                // the mirror point
                var mirrored = MirrorAPointAgainAnotherDiff(enemyAnt, thisAnt);


                // diffence line
                var difX = Math.Abs(Math.Abs(enemyAnt.ColX) - Math.Abs(thisAnt.ColX));
                var difY = Math.Abs(Math.Abs(enemyAnt.RowY) - Math.Abs(thisAnt.RowY));
                var lenOfDiffLine = Math.Sqrt((difX * difX) + (difY * difY));

                // the power of the ant!
                var decreaseVal = (int)(radiusForEnemy - lenOfDiffLine);

                // the location that has the power
                var close = DecreaseValueFromLine(mirrored.ColX, mirrored.RowY, decreaseVal);

                // add to finale result!
                finalX += close.ColX;
                finalY += close.RowY;
            }

            // now we have the location of where we should go!

            return new Location(finalY, finalX);
        }
        static Location DecreaseValueFromLine(int x1, int y1, int decreaseVal)
        {
            var t1 = Math.Sqrt((x1 * x1) + (y1 * y1));
            var t2 = Math.Abs(t1 - decreaseVal);

            // teta
            var tetaX = Math.Asin(x1 / t1);
            var tetaY = Math.Acos(y1 / t1);

            var x2 = (int)(t2 * Math.Sin(tetaX));
            var y2 = (int)(t2 * Math.Cos(tetaY));

            return new Location(y2, x2);
        }


        static Location MirrorAPointAgainAnotherDiff(Location toMirror, Location other)
        {
            int x = Math.Abs(toMirror.ColX - other.ColX);
            int y = Math.Abs(toMirror.RowY - other.RowY);

            if (toMirror.ColX > other.ColX)
            {
                x = -x;
            }
            else if (toMirror.ColX < other.ColX)
            {
                //x = x;
            }
            else
            {
                x = 0;
            }

            if (toMirror.RowY > other.RowY)
            {
                y = -y;
            }
            else if (toMirror.RowY < other.RowY)
            {
                //y = y;
            }
            else
            {
                y = 0;
            }
            return new Location(y, x);
        }


        /// <summary>
        /// Evade from enemy in straigth direction
        /// </summary>
        static bool ActionEvadeFromEnemyDirectional(Location myAntMeetEnemy,
                                                    int attackRadiusNeedAction,
                                                    int attackRadiusDanger,
                                                    Location closestEnemy)
        {
            if (closestEnemy != null)
            {
                // distance to close enemy
                var evadeDistance = _state.GetDistanceDirect(myAntMeetEnemy, closestEnemy);

                // is close enough?
                if (evadeDistance <= attackRadiusNeedAction)
                {
                    // this evades!!
                    var evadeLocation = FarDistanceFromEnemy(myAntMeetEnemy, closestEnemy, attackRadiusDanger);
                    if (evadeLocation != null)
                    {
                        evadeLocation = _state.GetDirectionLocationPassable(myAntMeetEnemy, evadeLocation);

                        // cancel all other plans!!!
                        planMan.RemoveFreeAnt(myAntMeetEnemy);
                        planMan.RemovePlannedAnt(myAntMeetEnemy);

                        // add new simple plan
                        planMan.AddPlan(new MovePlanDetail
                        {
                            Ant = myAntMeetEnemy,
                            Dest = evadeLocation
                        }, PlanManagerV2.PlanType.Move);

                        return true;
                    }
                }
            }
            return false;
        }

        static Location FarDistanceFromEnemy(Location myAnt, Location enemy, int attackRadius)
        {
            var neighborNodes = new Location[4];
            Common.GetLocationNeighborsMirrored(_state, neighborNodes, myAnt);

            // find far location
            var farLocation = neighborNodes.MaxValue(
                        x => _state.GetIsUnoccupiedSafe(x)
                        ? (x.ZVal = _state.GetDistanceDirect(x, enemy))
                        : -1);

            var sourceDistance = _state.GetDistanceDirect(myAnt, enemy);
            if (farLocation != null)
            {
                // warning! the distance is closer!!
                if (sourceDistance > farLocation.ZVal)
                {
                    // Oh! don't go closer
                    if (farLocation.ZVal >= attackRadius)
                    {
                        return null;
                    }
                }
            }

            // the far location, whether it is found or it is null
            return farLocation;
        }

    }
}
