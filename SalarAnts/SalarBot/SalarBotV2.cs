#if DEBUG
#define DrawVisual
#endif

//#define UseAccV2

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SalarAnts.AStarV2;
using SalarAnts.Classes;
using SalarAnts.Defination;
using SalarAnts.Pack;
using SettlersEngine;
using System.Collections.Specialized;

namespace SalarAnts.SalarBot
{
    public class SalarBotV2
    {
        #region variables
        private GameState _state;
        private Bot _bot;
        private Random _random = null;
        private int _turn = 0;

        /// <summary>
        /// In whole match eny enemy found?
        /// </summary>
        private bool _enemySeen = false;

        /// <summary>
        /// the map used for search!!
        /// </summary>
        private AntAStarPathNode[,] _astarAntsMap = null;
        private AntAStarSolver<AntAStarPathNode, Object> _aStar = null;
        private readonly PlanManagerV2 planMan = new PlanManagerV2();
        private readonly Dictionary<AntHill, IList<Location>> _myHillsWays = new Dictionary<AntHill, IList<Location>>();
        Dictionary<AntHill, Location[]> _hillGuards = new Dictionary<AntHill, Location[]>();
        #endregion

        public SalarBotV2(IGameState state, Bot bot)
        {
            State = state;
            Bot = bot;
        }

        public IGameState State
        {
            get { return _state; }
            set
            {
                _state = (GameState)value;
                if (_random == null && _state != null)
                {
#if TestInput
                    unchecked
                    {
                        int val = (int)_state.PlayerSeed;
                        _random = new Random(val);
                    }
#else
                    _random = new Random();
#endif
                }
#if TestInput
                //if (_state != null)
                //{
                //    SalarAntsVisualRunner.VisualStart();
                //}
#endif
            }
        }
        public Bot Bot
        {
            get { return _bot; }
            set { _bot = value; }
        }

        #region Initializing methods
        public void EnsureAStar()
        {
            // -----------------------------------------------------------
            // Step: generate map for all of the uses
            if (_astarAntsMap == null)
                _astarAntsMap = AntAStarPathNode.GenerateMap(_state.Map, _state.Height, _state.Width);
            if (_aStar == null && _astarAntsMap != null)
                _aStar = new AntAStarSolver<AntAStarPathNode, Object>(_astarAntsMap);
        }
        #endregion

        public void DoTurn()
        {
            _turn++;

            // init plan man
            planMan.GameState = _state;

#if UseAccV2
            // influence map
            AttackPlannerV2.InitInfluenceMap(_state);
#endif

            // remove invalid plans
            planMan.InvalidatePlans();

            // checking enemy hills
            RemoveInvalidEnemyHills();

            // Thresholds = How far A* algorithm can go? ---------------
            var turnSearchLimit = ((_state.Width * _state.Height) / 2) / _state.MyAnts.Count;
            if (turnSearchLimit < 20)
                turnSearchLimit = 20; // minimum is 50
            var foodSearchLimit = turnSearchLimit + 10;

            // ---------------------------
            // first turn!
            if (_turn == 1)
            {
                // find a way out of my hills
                foreach (var myHill in _state.MyHills)
                {
                    var hillWays = CheckingAround.CheckAround(_state, myHill, 53, true);
                    _myHillsWays.Add(myHill, hillWays);
                }

                // plan hill guards
                PlanHillGuardsLocations();
            }
            else if (_turn == 30)
            {
                _myHillsWays.Clear();
                // find a way out of my hills
                foreach (var myHill in _state.MyHills)
                {
                    var hillWays = CheckingAround.CheckAround(_state, myHill, 20, true);
                    _myHillsWays.Add(myHill, hillWays);
                }
            }

            // remove my invalid hills/ they are destroyed!
            RemoveInvalidMyHills();


#if TestInput
            int jumpToTurn = 95;

            if (jumpToTurn > 0)
            {
                if (_turn < jumpToTurn)
                    return;
            }

            if (_turn >= 2)
            {
                _turn = _turn;
            }

#endif


#if DrawVisual
            // drawing hillways
            foreach (var hillsWay in _myHillsWays)
            {
                BotHelpers.OverlaySetFillColor(System.Drawing.Color.Yellow);
                foreach (var location in hillsWay.Value)
                {
                    BotHelpers.OverlayCircle(location.ColX, location.RowY, .1f, true);
                }
            }
#endif

            // ---------------------------
            // Checking for new-born ants
            foreach (var myHill in _state.MyHills)
            {
                IList<Location> newBorns = new List<Location>();
                //newBorns = CheckingAround.FindTargetsInRadius(
                //   _state, myHill, 8,
                //   _state.GetIsPassableSafe,
                //   (x) =>
                //   {
                //       var tile = _state[x];
                //       return tile == Tile.AntMine;
                //   });

                // is there any ant ON hill?
                if (_state[myHill] == Tile.AntMine)
                    newBorns.Add(myHill);

                // first try to send the ants to guarding position
                // only if any enemy has found
                if (_enemySeen)
                    PlanHillGuards(newBorns);

                foreach (Location newBornAnt in newBorns)
                {
                    if (planMan.HasPlan(newBornAnt))
                        continue;

                    // out of hill
                    PlanNewBornOutOfHill(myHill, newBornAnt, turnSearchLimit);
                }
            }

            // ---------------------------
            // Checking around for activity - all ants ,including planned and not planned!
            for (int myAntIndex = 0; myAntIndex < _state.MyAnts.Count; myAntIndex++)
            {
                // we are in hurry
                if (_state.TimeRemaining < 20)
                    break;

                var myAnt = _state.MyAnts[myAntIndex];

                // first, does my ant has plan?
                var myAntPlanType = planMan.GetPlanType(myAnt);

                // guards don't do anything!
                if (myAntPlanType == PlanManagerV2.PlanType.HillGuard)
                    continue;

                // is this ant got new order?
                bool antIsNotTouched = true;

                // finding targets in view radius
                var aroundTargets = CheckingAround.FindTargetsInRadius(
                    _state, myAnt,
                    // search radius
                    _state.FogOfView,
                    // is passable?
                    _state.GetIsPassableSafe,
                    // is taget?
                    x =>
                    {
                        var locTile = _state[x];
                        if (locTile == Tile.AntEnemy || locTile == Tile.AntMine || locTile == Tile.Food || locTile == Tile.HillEnemy)
                            return true;
                        return false;
                    });

                // any targets found?
                if (aroundTargets.Count == 0)
                {
                    // no target around this ant
                    if (myAntPlanType.HasValue == false)
                    {
                        // this ant is not planned and it is free!
                    }
                }
                else
                {
                    // try to group what is found around it

                    var aroundAntMine = new List<Location>();
                    var aroundAntEnemy = new List<Location>();
                    var aroundEnemyHills = new List<Location>();
                    var aroundFood = new List<Location>();

                    foreach (var aroundT in aroundTargets)
                    {
                        var locTile = _state[aroundT];

                        switch (locTile)
                        {
                            case Tile.AntMine:
                                aroundAntMine.Add(aroundT);
                                break;

                            case Tile.AntEnemy:
                                aroundAntEnemy.Add(aroundT);
                                break;

                            case Tile.HillEnemy:
                                aroundEnemyHills.Add(aroundT);
                                break;

                            case Tile.Food:
                                aroundFood.Add(aroundT);
                                break;
                        }

                        // sometime, enemy is on the hill!
                        // BUGFIXED: not working, enemy hill is not detected when enemy ant is on it!
                        if (locTile == Tile.AntEnemy && _state.EnemyHills.Any(x => x.EqualTo(aroundT)))
                        {
                            // this is hill too!
                            aroundEnemyHills.Add(aroundT);
                        }
                    }

                    // ENEMY ------------------------------------
                    // enemy is the first proirity
                    if (antIsNotTouched && aroundAntEnemy.Count > 0)
                    {
                        // enemy found 
                        _enemySeen = true;

                        // before attack is there a HILL closer than enemy?
                        var wentToHill = ActionMyAntMeetsHillAndEnemy(myAnt, aroundAntEnemy, aroundEnemyHills, foodSearchLimit);
                        if (wentToHill)
                        {
                            // this ant is not free for sure!
                            antIsNotTouched = false;
                        }
                        else
                        {

#if UseAccV2
                            AttackPlannerV2._state = _state;
                            AttackPlannerV2.planMan = planMan;

                            //var actionDone = AttackPlannerV2.ActionMyAntMeetsEnemy(myAnt, aroundAntMine, aroundAntEnemy);
#else

                            // Tada...
                            var actionDone = ActionMyAntMeetsEnemy(myAnt, aroundAntMine, aroundAntEnemy);
#endif 

                            if (actionDone)
                            {
                                // this ant is not free for sure!
                                antIsNotTouched = false;
                            }
                        }
                    }

                    // ENEMY HILL ------------------------------------
                    if (antIsNotTouched && aroundEnemyHills.Count > 0)
                    {
                        if (myAntPlanType.HasValue &&
                            (myAntPlanType == PlanManagerV2.PlanType.Hill ||
                             myAntPlanType == PlanManagerV2.PlanType.Move ||
                             myAntPlanType == PlanManagerV2.PlanType.Attack))
                        {
                            // do not send it for hill!
                            // ignore
                        }
                        else
                        {
                            // is this ant was going for food?
                            SearchPlanDetail hasFoodPlan = null;
                            if (myAntPlanType.HasValue && myAntPlanType == PlanManagerV2.PlanType.Food)
                            {
                                hasFoodPlan = planMan.GetPlanByAnt(myAnt, PlanManagerV2.PlanType.Food) as SearchPlanDetail;
                            }

                            foreach (Location hill in aroundEnemyHills)
                            {
                                var hillPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, myAnt, hill, turnSearchLimit);
                                if (hillPlan != null)
                                {
                                    bool usePlan = true;
                                    if (hasFoodPlan != null)
                                    {
                                        if (hasFoodPlan.IsPlanForGoal() && !hillPlan.IsPlanForGoal())
                                            usePlan = false;

                                        if (hasFoodPlan.LeftSteps < hillPlan.LeftSteps + 6)
                                            usePlan = false;

                                        if (usePlan)
                                        {
                                            // remove food plan for this ant, Hill plan is better
                                            planMan.RemovePlannedAnt(hasFoodPlan.Ant);
                                        }
                                    }

                                    if (usePlan)
                                    {
                                        // remove new-born plan
                                        planMan.RemovePlannedAnt(hillPlan.Ant);

                                        // --------
                                        planMan.AddPlan(hillPlan, PlanManagerV2.PlanType.Hill);

                                        // this ant is not free anymore
                                        antIsNotTouched = false;
                                    }
                                }

                                // stop this loop if the order is issued
                                if (antIsNotTouched == false)
                                    break;
                            }
                        }
                    }

                    // FOOD ------------------------------------
                    if (antIsNotTouched && aroundFood.Count > 0)
                    {
                        if (myAntPlanType.HasValue &&
                           (myAntPlanType == PlanManagerV2.PlanType.Hill ||
                            myAntPlanType == PlanManagerV2.PlanType.Move ||
                            //myAntPlanType == PlanManagerV2.PlanType.HillGuard || // let it eat food!
                            myAntPlanType == PlanManagerV2.PlanType.Attack))
                        {
                            // do not send it for food!
                            // ignore
                        }
                        else
                        {
                            // sort the foods according to distance to this ant
                            var aroundFoodSorted = aroundFood.OrderBy(x => _state.GetDistance(myAnt, x));

                            // all food for this ant!
                            foreach (var food in aroundFoodSorted)
                            {
                                // who is coming for this food?
                                var previousFoodPlan = planMan.GetPlanByDest(food, PlanManagerV2.PlanType.Food) as SearchPlanDetail;
                                if (previousFoodPlan != null)
                                {
                                    // are they same ant?
                                    if (previousFoodPlan.Ant.EqualTo(myAnt))
                                    {
                                        // oops! this is same ant!
                                    }
                                    else
                                    {
                                        // find a path to the food!
                                        var newFoodPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, myAnt, food, foodSearchLimit);
                                        if (newFoodPlan != null)
                                        {
                                            bool useNewPlan = false;

                                            if (!previousFoodPlan.IsPlanForGoal() && newFoodPlan.IsPlanForGoal())
                                                useNewPlan = true;

                                            if (previousFoodPlan.LeftSteps > newFoodPlan.LeftSteps + 3)
                                                useNewPlan = true;

                                            if (useNewPlan)
                                            {
                                                // new plan is better

                                                // remove previous plan, and its ant
                                                planMan.RemovePlannedAnt(previousFoodPlan.Ant);

                                                // remove new-born plan
                                                planMan.RemovePlannedAnt(newFoodPlan.Ant);

                                                // --------
                                                planMan.AddPlan(newFoodPlan, PlanManagerV2.PlanType.Food);

                                                // this ant is not free anymore
                                                antIsNotTouched = false;
                                            }
                                            else
                                            {
                                                // previous plan is better
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    bool useNewPlan = true;

                                    // find a path to the food
                                    var newFoodPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, myAnt, food, foodSearchLimit);
                                    if (newFoodPlan == null)
                                        useNewPlan = false;

                                    if (newFoodPlan != null && myAntPlanType == PlanManagerV2.PlanType.Food)
                                    {
                                        // this ant has already a plan!
                                        var previousAntFoodPlan = planMan.GetPlanByAnt(myAnt, PlanManagerV2.PlanType.Food) as SearchPlanDetail;
                                        if (previousAntFoodPlan != null)
                                        {
                                            if (previousAntFoodPlan.IsPlanForGoal() && !newFoodPlan.IsPlanForGoal())
                                                useNewPlan = false;

                                            if (previousAntFoodPlan.LeftSteps <= newFoodPlan.LeftSteps)
                                                useNewPlan = false;

                                            if (useNewPlan)
                                            {
                                                // remove previous
                                                planMan.RemovePlannedAnt(previousAntFoodPlan.Ant);
                                            }
                                        }
                                    }

                                    if (useNewPlan)
                                    {
                                        // remove new-born plan
                                        planMan.RemovePlannedAnt(newFoodPlan.Ant);

                                        // --------
                                        planMan.AddPlan(newFoodPlan, PlanManagerV2.PlanType.Food);

                                        // this ant is not free anymore
                                        antIsNotTouched = false;
                                    }
                                }

                                // stop this loop if the order is issued
                                if (antIsNotTouched == false)
                                    break;
                            }
                        }
                    }
                }

                // if this is not planned ant, it is free
                if (antIsNotTouched && myAntPlanType == null)
                {
                    // this is free ant!
                    planMan.AddFreeAnt(myAnt);
                }
            }

            // we are in hurry
            if (_state.TimeRemaining < 20)
            {
                // Issue the orders 
                planMan.IssuePlannedOrders(_aStar, turnSearchLimit);
                return;
            }

            // my free ants
            ICollection myFreeAnts = planMan.GetFreeAnts();

            // ---------------------
            // FREE ANTS - Go for enemy HILLS
            if (myFreeAnts.Count > 0 && _state.EnemyHills.Count > 0)
            {
                // calculating minimum possible ants can go for an enemy hill
                // min = (MyAnts /1.3) / EnemyHills
                int minNeeded = (int)((myFreeAnts.Count / 1.3) / _state.EnemyHills.Count);

                // the ants which are not free any more
                var notFreeAnts = new List<Location>();
                foreach (AntHill enemyHill in _state.EnemyHills)
                {
                    // we are in hurry
                    if (_state.TimeRemaining < 20)
                        break;

                    int needed = minNeeded;
                    notFreeAnts.Clear();

                    // how many ants are issue to hill before?
                    var plannedAntCount = planMan.GetPlanCountByDest(enemyHill, PlanManagerV2.PlanType.Hill);
                    needed = needed - plannedAntCount;

                    // for all the ants
                    foreach (Location myFreeAnt in myFreeAnts)
                    {
                        if (needed <= 0)
                            break;

                        // search a path for it!
                        var enemyHillPlan = AntAStarMain.GetFindPathPlan(
                            _state,
                            _aStar,
                            myFreeAnt,
                            enemyHill,
                            turnSearchLimit);

                        if (enemyHillPlan != null)
                        {
                            needed--;

                            // remove from free ants
                            notFreeAnts.Add(enemyHillPlan.Ant);

                            // not free anymore
                            planMan.RemovePlannedAnt(enemyHillPlan.Ant);

                            planMan.AddPlan(enemyHillPlan, PlanManagerV2.PlanType.Hill);
                        }
                    }
                    foreach (var notFreeAnt in notFreeAnts)
                    {
                        planMan.RemoveFreeAnt(notFreeAnt);
                    }
                }
            }



            // my free ants
            myFreeAnts = planMan.GetFreeAnts();

            // FREE ANTS - ALL FOODS ---------------------------
            if (myFreeAnts.Count > 0)
            {
                var antsWeight = new List<AntWeight>();
                foreach (var food in _state.FoodTiles)
                {
                    if (_state.TimeRemaining < 20)
                        break;

                    // is this food planned before?
                    if (planMan.GetPlanByDest(food, PlanManagerV2.PlanType.Food) != null)
                    {
                        continue;
                    }

                    // minimum distance
                    foreach (Location freeAnt in myFreeAnts)
                    {
                        var weight = _state.GetDistance(freeAnt, food);
                        antsWeight.Add(new AntWeight
                        {
                            Ant = freeAnt,
                            Weight = weight
                        });
                    }

                    // search
                    var closeAntToFood = antsWeight.MinValue(x => x.Weight);

                    // found?
                    if (closeAntToFood.Ant != null)
                    {
                        // search a path for it!
                        var foodPlan = AntAStarMain.GetFindPathPlan(
                            _state,
                            _aStar,
                            closeAntToFood.Ant,
                            food,
                            foodSearchLimit);

                        if (foodPlan != null)
                        {
                            // remove from free ants
                            planMan.RemoveFreeAnt(closeAntToFood.Ant);

                            // not free anymore
                            planMan.RemovePlannedAnt(closeAntToFood.Ant);

                            planMan.AddPlan(foodPlan, PlanManagerV2.PlanType.Food);
                        }
                    }
                }
            }

            // my free ants
            myFreeAnts = planMan.GetFreeAnts();

            // ---------------------
            // FREE ANTS - EXPLORE the MAP
            if (myFreeAnts.Count > 0)
            {
                // we are in hurry
                if (_state.TimeRemaining < 20)
                {     // Issue the orders 
                    planMan.IssuePlannedOrders(_aStar, turnSearchLimit);
                    return;
                }

                // reset the state of the vision
                _state.ClearVision();
                _state.SetVision();

                // 
                if (_state.Unvisiable.Count > 0)
                {
                    // zero them
                    _state.Unvisiable.ForEach(x => x.ZVal = -1);
                    var notFreeAnts = new List<Location>();

                    foreach (Location myFreeAnt in myFreeAnts)
                    {
                        // we are in hurry
                        if (_state.TimeRemaining < 20)
                            break;

                        // 
                        notFreeAnts.Clear();

                        var exploreTarget =
                            _state.Unvisiable.MinValue(x => (x.ZVal == -1) ? _state.GetDistanceDirect(x, myFreeAnt) : int.MaxValue);

                        if (exploreTarget != null)
                        {
                            // search a path for it!
                            var explorePlan = AntAStarMain.GetFindPathPlan(
                                _state,
                                _aStar,
                                myFreeAnt,
                                exploreTarget,
                                turnSearchLimit);

                            if (explorePlan != null)
                            {
                                // this location is taken!
                                exploreTarget.ZVal = 1;

                                // remove from free ants
                                notFreeAnts.Add(explorePlan.Ant);

                                // not free anymore
                                planMan.RemovePlannedAnt(explorePlan.Ant);

                                planMan.AddPlan(explorePlan, PlanManagerV2.PlanType.Explore);
                            }
                        }
                    }
                    foreach (var notFreeAnt in notFreeAnts)
                    {
                        planMan.RemoveFreeAnt(notFreeAnt);
                    }
                }
            }

            // drawing debug info
            DrawDebugOverlayInfo();

            // Issue the orders 
            planMan.IssuePlannedOrders(_aStar, turnSearchLimit);
        }

        #region strategy methods

        private bool ActionMyAntMeetsHillAndEnemy(Ant myAnt, IList<Location> aroundAntEnemy, IList<Location> aroundEnemyHills, int foodSearchLimit)
        {
            if (aroundEnemyHills.Count == 0)
                return false;
            var theHill = aroundEnemyHills[0];
            var closestEnemy = aroundAntEnemy.MinValue(x => _state.GetDistanceDirect(x, myAnt));
            if (closestEnemy == null)
            {
                // search a path for it!
                var enemyHillPlan = AntAStarMain.GetFindPathPlan(
                    _state,
                    _aStar,
                    myAnt,
                    theHill,
                    foodSearchLimit);

                if (enemyHillPlan != null)
                {
                    // not free anymore
                    planMan.RemovePlannedAnt(enemyHillPlan.Ant);

                    planMan.AddPlan(enemyHillPlan, PlanManagerV2.PlanType.Hill);

                    return true;
                }
            }
            else
            {
                var closestEnemyDistance = _state.GetDistanceDirect(myAnt, closestEnemy);
                var distanceToHill = _state.GetDistance(myAnt, theHill);

                // is hill closer?
                if (closestEnemyDistance > distanceToHill)
                {
                    // search a path for it!
                    var enemyHillPlan = AntAStarMain.GetFindPathPlan(
                        _state,
                        _aStar,
                        myAnt,
                        theHill,
                        foodSearchLimit);

                    if (enemyHillPlan != null)
                    {
                        // not free anymore
                        planMan.RemovePlannedAnt(enemyHillPlan.Ant);

                        planMan.AddPlan(enemyHillPlan, PlanManagerV2.PlanType.Hill);

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Weaken the ANT, BAAD
        /// </summary>
        private void ActionValidateAttacks_NoUse()
        {
            var listOfInAttack = new Dictionary<Location, List<Location>>();
            var lostOfOneStepToAttack = new Dictionary<Location, List<Location>>();
            var attackRadiusDanger = (_state.AttackRadius2 / 2) + 1;
            var attackRadiusClose = attackRadiusDanger + 1;
            var attackRadiusNeedAction = attackRadiusDanger + 2;

            foreach (var attackPlan in planMan.GetAttackPlans())
            {
                //var distance = _state.GetDistanceDirect(attackPlan.Enemy, attackPlan.Ant);
                var nextStepDistance = _state.GetDistanceDirect(attackPlan.Enemy, attackPlan.NextStep);

                if (nextStepDistance <= attackRadiusDanger)
                {
                    List<Location> inAttackList;
                    listOfInAttack.TryGetValue(attackPlan.Enemy, out inAttackList);
                    if (inAttackList != null)
                    {
                        inAttackList.Add(attackPlan.Ant);
                    }
                    else
                    {
                        inAttackList = new List<Location>();
                        inAttackList.Add(attackPlan.Ant);
                        listOfInAttack.Add(attackPlan.Enemy, inAttackList);
                    }
                }
                //else if (nextStepDistance == attackRadiusClose)
                //{
                //    List<Location> inCloseAttackList;
                //    lostOfOneStepToAttack.TryGetValue(attackPlan.Enemy, out inCloseAttackList);
                //    if (inCloseAttackList != null)
                //    {
                //        inCloseAttackList.Add(attackPlan.Ant);
                //    }
                //    else
                //    {
                //        inCloseAttackList = new List<Location>();
                //        inCloseAttackList.Add(attackPlan.Ant);
                //        lostOfOneStepToAttack.Add(attackPlan.Enemy, inCloseAttackList);
                //    }
                //}
            }

            foreach (var inAttack in listOfInAttack)
            {
                if (inAttack.Value.Count < 2)
                {
                    foreach (var myAnt in inAttack.Value)
                    {
                        planMan.RemovePlannedAnt(myAnt);
                        ActionEvadeFromEnemyDirectional(myAnt, attackRadiusNeedAction, attackRadiusDanger, inAttack.Key);
                    }
                }
            }
            //foreach (var onStep in lostOfOneStepToAttack)
            //{
            //    if (onStep.Value.Count > 2)
            //    {
            //        foreach (var myAnt in onStep.Value)
            //        {
            //            //attack then!
            //            // chooce passabe location
            //            var attackLocation = _state.GetDirectionLocationCloser(myAnt, onStep.Key, onStep.Key);

            //            // cancel all other plans
            //            // remeber, Attack plan is not reached here already! 
            //            planMan.RemovePlannedAnt(myAnt);

            //            // add new simple plan
            //            planMan.AddPlan(new AttackPlanDetail
            //            {
            //                NextStep = attackLocation,
            //                Ant = myAnt,
            //                Enemy = onStep.Key,
            //                LeaderAnt = myAnt
            //            }, PlanManagerV2.PlanType.Attack);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Managing attack/defence
        /// </summary>
        private bool ActionMyAntMeetsEnemy(Ant myAntMeetEnemy, IList<Location> aroundMyAntsInView, IList<Location> aroundEnemyAntsInView)
        {
            // distances of 
            var fogOfView = _state.FogOfView;
            var attackRadiusDanger = (_state.AttackRadius2 / 2) + 1;
            var attackRadiusClose = attackRadiusDanger + 1;
            var attackRadiusNeedAction = attackRadiusDanger + 2;
            var supportMyAntRadius = fogOfView;

            // in distance of attack requires action
            var aroundAntsInAttack = CheckingAround.FindTargetsInDirectRadius(
                _state, myAntMeetEnemy,
                // search radius
                attackRadiusNeedAction,
                // is passable?
                _state.GetIsPassableSafe,
                // is taget?
                x =>
                {
                    var locTile = _state[x];
                    if (locTile == Tile.AntEnemy || locTile == Tile.AntMine || locTile == Tile.HillEnemy)
                        return true;
                    return false;
                });




            //var aroundMyAntsInAttack = new List<Location>();
            //var aroundEnemyAntsInAttack = new List<Location>();
            //foreach (var aroundT in aroundAntsInAttack)
            //{
            //    var locTile = _state[aroundT];
            //    switch (locTile)
            //    {
            //        case Tile.AntMine:
            //            // add only if this ant doesn't have guard plan
            //            if (planMan.GetPlanType(aroundT) != PlanManagerV2.PlanType.HillGuard)
            //            {
            //                aroundMyAntsInAttack.Add(aroundT);
            //            }
            //            break;

            //        //case Tile.HillEnemy:
            //        case Tile.AntEnemy:

            //            // add only if this ant doesn't have plan
            //            if (planMan.GetPlanType(aroundT) != PlanManagerV2.PlanType.Hill)
            //            {
            //                aroundEnemyAntsInAttack.Add(aroundT);
            //            }
            //            break;
            //    }
            //}

            // simple calc!
            var aroundEnemyAntsInAttack = aroundEnemyAntsInView.Where(x => _state.GetDistanceDirect(x, myAntMeetEnemy) <= attackRadiusNeedAction);

            // no enemy found!, requires no action
            if (aroundEnemyAntsInAttack.Count == 0)
            {
                // do normal job
                return false;
            }

            // my ants the view
            var aroundMyAntsInAttack = aroundMyAntsInView.Where(x => _state.GetDistanceDirect(x, myAntMeetEnemy) <= attackRadiusNeedAction);

            bool attackDone = false;

            if (aroundMyAntsInAttack.Count + 1 > aroundEnemyAntsInAttack.Count)
            {
                // attack is required!
                // do attack
                attackDone = ActionAttack(myAntMeetEnemy, aroundMyAntsInView, aroundEnemyAntsInView, aroundMyAntsInAttack,
                             aroundEnemyAntsInAttack);
                if (attackDone) return true;
            }


            // does this ant has attack plan? then just cancel the rest!
            if (planMan.GetPlanType(myAntMeetEnemy) == PlanManagerV2.PlanType.Attack)
                return true;


            if (!attackDone || aroundEnemyAntsInAttack.Count >= aroundMyAntsInAttack.Count)
            {
                // evade calculated
                if (ActionEvadeFromPowerOfEnemyAnts(myAntMeetEnemy, aroundEnemyAntsInAttack, attackRadiusNeedAction))
                    return true;

                // Evade only
                if (ActionEvadeFromEnemyDirectional(myAntMeetEnemy, attackRadiusNeedAction, attackRadiusDanger, aroundEnemyAntsInAttack))
                    return true;
            }

            // we reach here, means we did nothing!
            return false;
        }

        /// <summary>
        /// Attack algorithms
        /// </summary>
        private bool ActionAttack(
            Ant myAntMeetEnemy,
            IList<Location> aroundMyAntsInView,
            IList<Location> aroundEnemyAntsInView,
            IList<Location> aroundMyAntsInAttack,
            IList<Location> aroundEnemyAntsInAttack)
        {
            var fogOfView = _state.FogOfView;
            var attackRadiusDanger = (_state.AttackRadius2 / 2) + 1;
            var attackRadiusClose = attackRadiusDanger + 1;
            var attackRadiusNeedAction = attackRadiusDanger + 2;
            var supportMyAntRadius = fogOfView;

            // all my ant
            var myAntsInvolvedAll = new List<Location>(aroundMyAntsInAttack);
            myAntsInvolvedAll.Add(myAntMeetEnemy);

            // close enemy to this ant!
            var closeEnemyToThis = aroundEnemyAntsInAttack.MinValue(x => _state.GetDistanceDirect(myAntMeetEnemy, x));
            if (closeEnemyToThis == null)
                return false;
            var closeEnemyToThisWeight = _state.GetDistanceDirect(myAntMeetEnemy, closeEnemyToThis);


            // my ant previous attack plan
            var myAntPreviousAttackPlan = planMan.GetPlanByAnt(myAntMeetEnemy, PlanManagerV2.PlanType.Attack) as AttackPlanDetail;
            if (myAntPreviousAttackPlan != null)
            {
                // checking differences
                if (closeEnemyToThisWeight > _state.GetDistanceDirect(myAntPreviousAttackPlan.LeaderAnt, closeEnemyToThis))
                {
                    // the previous plan is OK
                    return true;
                }
            }

            // remove helpers if they already have closer enemy!
            myAntsInvolvedAll.RemoveFromIList(x => aroundEnemyAntsInView.Any(z => _state.GetDistanceDirect(z, x) < closeEnemyToThisWeight));


            // attack points, by my closest ant to enemy
            var enemyCirclePoints = Common.CircleMidPoint(closeEnemyToThis.ColX, closeEnemyToThis.RowY,
                closeEnemyToThisWeight);

            // attack or wait?
            int myAntCountAround = 0;
            foreach (var ant in myAntsInvolvedAll)
            {
                if (enemyCirclePoints.Any(x => x.EqualTo(ant)))
                {
                    myAntCountAround++;
                }
                if (myAntCountAround >= 2)
                {
                    break;
                }
            }


            // do we have enough ants to attack?
            if (myAntCountAround >= 2)
            {
                const bool useV3sendingAcc = true;

                if (useV3sendingAcc)
                {
                    var plannedAttack = new List<KeyValuePair<Location, Location>>();

                    // send attack to them
                    foreach (var myAnt in myAntsInvolvedAll)
                    {
                        // chooce passabe location
                        var attackLocation = _state.GetDirectionLocationCloser(myAnt, closeEnemyToThis, closeEnemyToThis);

                        // attack loction
                        plannedAttack.Add(new KeyValuePair<Location, Location>(myAnt, attackLocation));
                    }

                    // simumlating the attack!
                    for (int i = plannedAttack.Count - 1; i >= 0; i--)
                    {
                        var myAntKeyVal = plannedAttack[i];
                        var dist = _state.GetDistanceDirect(myAntKeyVal.Key, closeEnemyToThis);

                        // are there enought helper?
                        var helperCount = plannedAttack.Count(x => dist >= _state.GetDistanceDirect(x.Value, closeEnemyToThis));
                        if (helperCount < 2)
                        {
                            // oop! this ant is goinf to die! just don't move it!
                            plannedAttack.RemoveAt(i);
                        }
                    }

                    // ok, now do we have enought ant for attack?
                    if (plannedAttack.Count >= 2)
                    {
                        for (int i = plannedAttack.Count - 1; i >= 0; i--)
                        {
                            var myAntKeyVal = plannedAttack[i];

                            // cancel all other plans
                            // remeber, Attack plan is not reached here already! 
                            planMan.RemovePlannedAnt(myAntKeyVal.Key);

                            // add new simple plan
                            planMan.AddPlan(new AttackPlanDetail
                            {
                                NextStep = myAntKeyVal.Value,
                                Ant = myAntKeyVal.Key,
                                Enemy = closeEnemyToThis,
                                LeaderAnt = myAntMeetEnemy
                            }, PlanManagerV2.PlanType.Attack);
                        }
                    }
                }
                else
                {
                    // V1 sending attack

                    // send attack to them
                    foreach (var myAnt in myAntsInvolvedAll)
                    {

                        // chooce passabe location
                        var attackLocation = _state.GetDirectionLocationCloser(myAnt, closeEnemyToThis, closeEnemyToThis);

                        // cancel all other plans
                        // remeber, Attack plan is not reached here already! 
                        planMan.RemovePlannedAnt(myAnt);

                        // add new simple plan
                        planMan.AddPlan(new AttackPlanDetail
                        {
                            NextStep = attackLocation,
                            Ant = myAnt,
                            Enemy = closeEnemyToThis,
                            LeaderAnt = myAntMeetEnemy
                        }, PlanManagerV2.PlanType.Attack);

                    }
                }

                

                return true;
            }
            else
            {
                // try to keep the distance
                ActionMoveAntsGroupToDistanceOfEnemy(enemyCirclePoints, myAntsInvolvedAll, closeEnemyToThis, myAntMeetEnemy);
                return true;
            }
        }


        /// <summary>
        /// Moves all ants around the enemy by attackRadius
        /// </summary>
        void ActionMoveAntsGroupToDistanceOfEnemy(IList<Location> enemyCirclePoints, IList<Location> myAntsInvolvedAll,
            Location closeEnemyToThis,
            Location myAntLeaderAnt)
        {
            var attackRadiusDanger = (_state.AttackRadius2 / 2) + 1;
            var attackRadiusClose = attackRadiusDanger + 1;
            var attackRadiusNeedAction = attackRadiusDanger + 2;

            // ordered by 
            var myAntsInvolved = myAntsInvolvedAll.OrderBy(x => _state.GetDistanceDirect(x, closeEnemyToThis));
            //var closeDistanceToEnemy = _state.GetDistanceDirect(myAntsInvolvedAll[0], closeEnemyToThis);// antsGroup[0].Weight;

            foreach (var myAnt in myAntsInvolved)
            {
                var minAttackPoint = enemyCirclePoints.MinValue(
                    x =>
                    {
                        // -1 means issue went there before!
                        if (!_state.GetIsUnoccupiedSafe(x) || x.ZVal == -1)
                            return x.ZVal = int.MaxValue;

                        return x.ZVal = _state.GetDistanceDirect(x, myAnt);
                    });

                // no luck!
                if (minAttackPoint == null)
                    continue;

                // is this ant close enough?
                if (_state.GetDistanceDirect(myAnt, closeEnemyToThis) <= _state.GetDistanceDirect(minAttackPoint, closeEnemyToThis))
                {
                    // DON'T MOVE
                    // The ant should not move, to be sure we cancel other plans for this ant
                    planMan.RemovePlannedAnt(myAnt);

                    // find miimum distance of my friends
                    var minDistance = myAntsInvolvedAll.MinValue(
                        x =>
                        {
                            if (x.EqualTo(myAnt))
                                return 100000;
                            var a = _state.GetDistanceDirect(x, myAnt);
                            var b = _state.GetDistanceDirect(x, closeEnemyToThis);
                            x.ZVal = a;
                            return a + b;
                        });

                    // my friends are soo far
                    if (minDistance != null && minDistance.ZVal >= 2)
                    {
                        // evade for now!
                        ActionEvadeFromEnemyDirectional(myAnt, attackRadiusNeedAction, attackRadiusDanger, closeEnemyToThis);
                    }

                    // we done with this ant
                    continue;
                }

                // moveToReadyForAttack
                //var myAnt = antWeight.Ant;
                Location moveToReadyForAttack = minAttackPoint;

                // make distance equal
                var directions = _state.GetDirections(myAnt, minAttackPoint) as IList<Direction>;
                if (directions.Count > 1)
                {
                    var dirLocs = new Location[directions.Count];
                    for (int i = 0; i < directions.Count; i++)
                    {
                        dirLocs[i] = _state.GetDestination(myAnt, directions[i]);
                        if (!_state.GetIsUnoccupiedSafe(dirLocs[i]))
                        {
                            dirLocs[i].ZVal = int.MaxValue;
                        }
                        else
                        {
                            dirLocs[i].ZVal = _state.GetDistanceDirect(myAnt, dirLocs[i]);
                        }
                    }

                    moveToReadyForAttack = dirLocs.MinValue(x => x.ZVal);
                    if (moveToReadyForAttack == null)
                    {
                        moveToReadyForAttack = minAttackPoint;
                    }
                }

                // not any more
                minAttackPoint.ZVal = -1;

                // cancel all other plans!!!
                planMan.RemovePlannedAnt(myAnt);

                // add new simple plan
                planMan.AddPlan(new AttackPlanDetail
                    {
                        NextStep = moveToReadyForAttack,
                        Ant = myAnt,
                        Enemy = closeEnemyToThis,
                        LeaderAnt = myAntLeaderAnt,
                    },
                    PlanManagerV2.PlanType.Attack);
            }
        }


        /// <summary>
        /// Evade from enemy in straigth direction
        /// </summary>
        private bool ActionEvadeFromEnemyDirectional(Location myAntMeetEnemy,
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

        /// <summary>
        /// Evade from enemy in straigth direction
        /// </summary>
        private bool ActionEvadeFromEnemyDirectional(Location myAntMeetEnemy, int attackRadiusNeedAction, int attackRadiusDanger,
                                                     IList<Location> aroundEnemyAntsInAttack)
        {
            // is evade needed!
            var closestEnemy = aroundEnemyAntsInAttack.MinValue(x => _state.GetDistanceDirect(myAntMeetEnemy, x));
            return ActionEvadeFromEnemyDirectional(myAntMeetEnemy, attackRadiusNeedAction, attackRadiusDanger, closestEnemy);
        }

        /// <summary>
        /// Which location is far from all of enemy
        /// </summary>
        private bool ActionEvadeFromPowerOfEnemyAnts(Ant myAntMeetEnemy, IList<Location> aroundEnemyAntsInAttack, int attackRadiusNeedAction)
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
        private static Location EvadeFromPowerOfAnts(Location thisAnt, int radiusForEnemy, IList<Location> enemyAnts)
        {
            // the initial value is the location of my ant!
            int finalX = thisAnt.ColX;
            int finalY = thisAnt.RowY;

            //foreach (Location myAnt in myAnts)
            //{
            //    // more than this distance does not affect
            //    if (Math.Abs(thisAnt.RowY - myAnt.RowY) > radiusForMyAnts)
            //        continue;

            //    // more than this distance does not affect
            //    if (Math.Abs(thisAnt.ColX - myAnt.ColX) > radiusForMyAnts)
            //        continue;

            //    // the diffecnce point
            //    var close = CloseAPointAgainAnotherDiff(myAnt, thisAnt);

            //    // diffence line
            //    var difX = Math.Abs(Math.Abs(myAnt.ColX) - Math.Abs(thisAnt.ColX));
            //    var difY = Math.Abs(Math.Abs(myAnt.RowY) - Math.Abs(thisAnt.RowY));
            //    var lenOfDiffLine = Math.Sqrt((difX * difX) + (difY * difY));

            //    // the power of the ant!
            //    var decreaseVal = (int)(radiusForMyAnts - lenOfDiffLine);

            //    // the location that has the power
            //    close = DecreaseValueFromLine(close.ColX, close.RowY, decreaseVal);

            //    // add to final
            //    finalX += close.ColX;
            //    finalY += close.RowY;
            //}

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

        /// <summary>
        /// Sending ants to hill guard position
        /// </summary>
        private void PlanHillGuards(IList<Location> newBornAnts)
        {
            // do we have enough ants?
            if (_state.MyAnts.Count < (_hillGuards.Count * 4) + 5)
                return;

            foreach (var hillGuard in _hillGuards)
                foreach (var location in hillGuard.Value)
                {
                    if (_state[location] != Tile.AntMine)
                    {
                        // move to guard position
                        var closestAnt = newBornAnts.MinValue(x => _state.GetDistance(x, location));
                        if (closestAnt != null)
                        {
                            var closestAntDistance = _state.GetDistance(closestAnt, location);

                            // we cannot send far ants to protect this!
                            if (closestAntDistance > 7)
                                continue;

                            // search a path for it!
                            var guardPLan = AntAStarMain.GetFindPathPlan(
                                _state,
                                _aStar,
                                closestAnt,
                                location,
                                30);

                            if (guardPLan != null)
                            {
                                planMan.RemovePlannedAnt(closestAnt);
                                newBornAnts.Remove(closestAnt);

                                // goes to guard
                                planMan.AddPlan(guardPLan, PlanManagerV2.PlanType.Food);


                                //// where to go to get close! it should be passabel
                                //var passableLocation = _state.GetDirectionLocationPassable(closestAnt, location);

                                //// add new simple plan
                                //planMan.AddPlan(new MovePlanDetail
                                //{
                                //    Ant = closestAnt,
                                //    Dest = passableLocation
                                //}, PlanManagerV2.PlanType.Move);
                            }




                        }
                    }
                    else
                    {
                        // just remove it from new borns
                        planMan.RemovePlannedAnt(location);
                        newBornAnts.Remove(location);

                        // don't move!
                        planMan.AddPlan(location, PlanManagerV2.PlanType.HillGuard);
                    }
                }
        }

        private void PlanHillGuardsLocations()
        {
            var locations = new Location[4];
            foreach (var myHill in _state.MyHills)
            {
                int count = 0;
                locations[0] = Common.CreateMirrorableLocation(_state, myHill.RowY - 1, myHill.ColX - 1);
                if (_state.GetIsUnoccupiedMirrored(locations[0]))
                    count++;
                else
                    locations[0] = null;

                locations[1] = Common.CreateMirrorableLocation(_state, myHill.RowY - 1, myHill.ColX + 1);
                if (_state.GetIsUnoccupiedMirrored(locations[1]))
                    count++;
                else
                    locations[1] = null;

                locations[2] = Common.CreateMirrorableLocation(_state, myHill.RowY + 1, myHill.ColX - 1);
                if (_state.GetIsUnoccupiedMirrored(locations[2]))
                    count++;
                else
                    locations[2] = null;

                locations[3] = Common.CreateMirrorableLocation(_state, myHill.RowY + 1, myHill.ColX + 1);
                if (_state.GetIsUnoccupiedMirrored(locations[3]))
                    count++;
                else
                    locations[3] = null;

                int guardId = count;
                if (count > 0)
                {
                    var guards = new Location[count];
                    foreach (var location in locations)
                    {
                        if (location != null)
                        {
                            guardId--;
                            guards[guardId] = location;
                        }
                    }

                    _hillGuards.Add(myHill, guards);
                }
            }
        }

        /// <summary>
        /// Sending new-born ant out of hill
        /// </summary>
        private void PlanNewBornOutOfHill(Location theHill, Location ant, int searchLimit)
        {
            var hill = _myHillsWays.FirstOrDefault(x => x.Key.EqualTo(theHill));
            if (hill.Key != null)
            {
                // count of out ways found!
                var count = hill.Value.Count;

                var tryCount = 0;
                Location destLoc = null;
                bool antIssued = false;

                while (tryCount < count)
                {
                    tryCount++;
                    var rnd = _random.Next(0, count - 1);
                    destLoc = hill.Value[rnd];

                    if (destLoc != null)
                    {
                        // NO!
                        var tempPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, ant, destLoc, searchLimit);

                        if (tempPlan != null)
                        {
                            // goes to plans!
                            planMan.AddPlan(tempPlan, PlanManagerV2.PlanType.NewBorn);


                            antIssued = true;
                            break;
                        }
                    }
                }

                // no luck with A* searching? try random move!
                if (antIssued == false)
                {
                    var rndMove = RandomMove(ant);
                    if (rndMove != null)
                    {
                        // just get out of it!
                        planMan.AddPlan(new SearchPlanDetail
                                            {
                                                Ant = ant,
                                                Goal = rndMove,
                                                LeftSteps = 0
                                            },
                                        PlanManagerV2.PlanType.NewBorn);
                    }
                }
            }
        }

        /// <summary>
        /// Chooce a random direction
        /// </summary>
        private Location RandomMove(Location ant)
        {
            var inNeighbors = new Location[4];
            var count = 0;
            Common.GetLocationNeighborsMirrored(_state, inNeighbors, ant);

            while (count < 10)
            {
                var nextMove = _random.Next(0, 3);
                var newLoc = inNeighbors[nextMove];

                if (_state.GetIsUnoccupiedMirrored(newLoc))
                {
                    return newLoc;
                }

                count++;
            }
            return null;
        }
        #endregion

        #region game stats methods
        private void RemoveInvalidMyHills()
        {
            // if my hill is visible and is not hill any more, means it is destroyed
            for (int i = _state.MyHills.Count - 1; i >= 0; i--)
            {
                var myHill = _state.MyHills[i];

                if (_state.GetIsVisible(myHill) &&
                _state[myHill] != Tile.HillMine && _state[myHill] != Tile.AntMine)
                {
                    // remove destroyed hill
                    _state.MyHills.RemoveAt(i);

                    // remove my hills ways
                    _myHillsWays.Remove(myHill);
                }
            }
        }

        private void RemoveInvalidEnemyHills()
        {
            // if enemy hill is visible and is not hill any more, means it is destroyed
            _state.EnemyHills.RemoveWhere(enemyHill =>
                _state.GetIsVisible(enemyHill) &&
                _state[enemyHill] != Tile.HillEnemy && _state[enemyHill] != Tile.AntEnemy);
        }

        void DrawDebugOverlayInfo()
        {
#if DrawVisual

            BotHelpers.OverlaySetFillColor(System.Drawing.Color.Red);
            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Black);
            // draw guards
            foreach (var guards in _hillGuards)
            {
                foreach (var l in guards.Value)
                {
                    BotHelpers.OverlayStar(l.ColX, l.RowY, .01f, 0.03f, 4, true);
                }
            }

            // Misc
            BotHelpers.OverlaySetFillColor(System.Drawing.Color.Red);
            foreach (var enemyHill in _state.EnemyHills)
            {
                BotHelpers.OverlayCircle(enemyHill.ColX, enemyHill.RowY, 0.2f, true);
            }

            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Black);
            foreach (var p in planMan.GetMovePlans())
            {
                BotHelpers.OverlayArrow(p.Ant.ColX, p.Ant.RowY, p.Dest.ColX, p.Dest.RowY);
            }

            BotHelpers.OverlaySetFillColor(System.Drawing.Color.WhiteSmoke);
            foreach (Location freeAnt in planMan.GetFreeAnts())
            {
                BotHelpers.OverlayCircle(freeAnt.ColX, freeAnt.RowY, 0.2f, true);
            }

            // foood
            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Green);
            foreach (var p in planMan.GetFoodPlans())
            {
                BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
            }

            BotHelpers.OverlaySetLineColor(System.Drawing.Color.LightGreen);
            foreach (var p in planMan.GetFoodPlans())
            {
                var next = p.PathCurrent;
                if (next != null && (next = next.Next) != null)
                    BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
            }

            // hill
            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Orange);
            foreach (var p in planMan.GetHillPlans())
            {
                BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
            }

            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Orange);
            foreach (var p in planMan.GetHillPlans())
            {
                var next = p.PathCurrent;
                if (next != null && (next = next.Next) != null)
                    BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
            }

            // new born!
            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Yellow);
            foreach (var p in planMan.GetNewBornPlans())
            {
                BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
            }

            BotHelpers.OverlaySetLineColor(System.Drawing.Color.WhiteSmoke);
            foreach (var p in planMan.GetNewBornPlans())
            {
                var next = p.PathCurrent;
                if (next != null && (next = next.Next) != null)
                    BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
            }

            // explore
            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Blue);
            foreach (var p in planMan.GetExplorePlans())
            {
                BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
            }

            BotHelpers.OverlaySetLineColor(System.Drawing.Color.CornflowerBlue);
            foreach (var p in planMan.GetExplorePlans())
            {
                var next = p.PathCurrent;
                if (next != null && (next = next.Next) != null)
                    BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
            }

            // attacks
            BotHelpers.OverlaySetLineColor(System.Drawing.Color.DarkRed);
            foreach (var p in planMan.GetAttackPlans())
            {
                BotHelpers.OverlayArrow(p.Ant.ColX, p.Ant.RowY, p.NextStep.ColX, p.NextStep.RowY);
            }

            BotHelpers.OverlaySetLineColor(System.Drawing.Color.Red);
            foreach (var p in planMan.GetAttackPlans())
            {
                BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.Enemy.ColX, p.Enemy.RowY);
            }

            BotHelpers.OverlaySetFillColor(System.Drawing.Color.Red);
            foreach (var p in planMan.GetAttackPlans())
            {
                BotHelpers.OverlayCircle(p.Enemy.ColX, p.Enemy.RowY, _state.GetDistanceDirect(p.Enemy, p.LeaderAnt), false);
            }

            // Misc
            BotHelpers.OverlaySetFillColor(System.Drawing.Color.OrangeRed);
            foreach (var p in planMan.GetAttackPlans())
            {
                if (p.LeaderAnt != null)
                    BotHelpers.OverlayCircle(p.LeaderAnt.ColX, p.LeaderAnt.RowY, 0.2f, true);
            }


#endif
        }
        #endregion

        #region common methods

        Location FarDistanceFromEnemy(Location myAnt, Location enemy, int attackRadius)
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

        //static Location CloseAPointAgainAnotherDiff(Location toMirror, Location other)
        //{
        //    int x = Math.Abs(toMirror.ColX - other.ColX);
        //    int y = Math.Abs(toMirror.RowY - other.RowY);

        //    if (toMirror.ColX > other.ColX)
        //    {
        //        x = x;
        //    }
        //    else if (toMirror.ColX < other.ColX)
        //    {
        //        x = -x;
        //    }
        //    else
        //    {
        //        x = 0;
        //    }

        //    if (toMirror.RowY > other.RowY)
        //    {
        //        y = y;
        //    }
        //    else if (toMirror.RowY < other.RowY)
        //    {
        //        y = -y;
        //    }
        //    else
        //    {
        //        y = 0;
        //    }
        //    return new Location(y, x);
        //}
        #endregion

    }
}
