using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts.AStarV2;
using SalarAnts.Defination;
using SalarAnts.Pack;
using SettlersEngine;

namespace SalarAnts.SalarBot
{
    public class PlanManagerV2
    {
        private enum IssuedOrderState
        {
            PlanFinished,
            Success,
            Blocked,
            IssuedBefore,
            Failed
        };

        public enum PlanType
        {
            None,
            Attack,
            Food,
            Hill,
            NewBorn,
            Move,
            Explore,
            HillGuard
        }

        private IGameState _state;
        public IGameState GameState
        {
            get { return _state; }
            set { _state = value; }
        }

        private readonly List<AttackPlanDetail> _attackPlans;
        private readonly List<SearchPlanDetail> _foodPlans;
        private readonly List<SearchPlanDetail> _hillPlans;
        private readonly List<MovePlanDetail> _movePlans;
        private readonly List<SearchPlanDetail> _newBornPlans;
        private readonly Hashtable _myFreeAnts;
        private readonly List<SearchPlanDetail> _explorePlans;
        private readonly Hashtable _plannedAnts;
        private readonly List<Location> _thisTurnDoneOrders = new List<Location>();
        private readonly List<Location> _hillGuards = new List<Location>();

        public PlanManagerV2()
        {
            //GameState = gameState;
            _foodPlans = new List<SearchPlanDetail>();
            _hillPlans = new List<SearchPlanDetail>();
            _movePlans = new List<MovePlanDetail>();
            _attackPlans = new List<AttackPlanDetail>();
            _newBornPlans = new List<SearchPlanDetail>();
            _myFreeAnts = new Hashtable();
            _explorePlans = new List<SearchPlanDetail>();
            _plannedAnts = new Hashtable();
            _hillGuards = new List<Location>();
        }

        public void AddPlan(SearchPlanDetail plan, PlanType planType)
        {
            switch (planType)
            {
                case PlanType.Food:
                    InternalAddPlannedAnt(plan.Ant, planType);
                    _foodPlans.Add(plan);
                    break;

                case PlanType.Hill:
                    InternalAddPlannedAnt(plan.Ant, planType);
                    _hillPlans.Add(plan);
                    break;

                case PlanType.NewBorn:
                    InternalAddPlannedAnt(plan.Ant, planType);
                    _newBornPlans.Add(plan);
                    break;

                case PlanType.Explore:
                    InternalAddPlannedAnt(plan.Ant, planType);
                    _explorePlans.Add(plan);
                    break;
                case PlanType.None:
                case PlanType.Move:
                case PlanType.Attack:
                default:
                    throw new ArgumentOutOfRangeException("planType");
            }
        }
        public void AddPlan(Location ant, PlanType planType)
        {
            switch (planType)
            {
                case PlanType.HillGuard:
                    _hillGuards.Add(ant);
                    InternalAddPlannedAnt(ant, planType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("planType");
            }
        }

        public void AddPlan(AttackPlanDetail plan, PlanType planType)
        {
            switch (planType)
            {
                case PlanType.Attack:
                    _attackPlans.Add(plan);
                    InternalAddPlannedAnt(plan.Ant, planType);
                    break;

                case PlanType.Move:
                case PlanType.Food:
                case PlanType.Hill:
                case PlanType.NewBorn:
                case PlanType.Explore:
                case PlanType.None:
                default:
                    throw new ArgumentOutOfRangeException("planType");
            }
        }
        public void AddPlan(MovePlanDetail plan, PlanType planType)
        {
            switch (planType)
            {
                case PlanType.Move:
                    _movePlans.Add(plan);
                    InternalAddPlannedAnt(plan.Ant, planType);
                    break;

                case PlanType.Attack:
                case PlanType.Food:
                case PlanType.Hill:
                case PlanType.NewBorn:
                case PlanType.Explore:
                case PlanType.None:
                default:
                    throw new ArgumentOutOfRangeException("planType");
            }
        }

        public void RemovePlannedAnt(Location ant)
        {
            var planType = (PlanType?)_plannedAnts[ant.HashCode()];
            if (!planType.HasValue)
                return;

            bool remove = true;
            switch (planType.Value)
            {
                case PlanType.Attack:
                    _attackPlans.RemoveFirst(x => x.Ant.EqualTo(ant));
                    break;

                case PlanType.Food:
                    _foodPlans.RemoveFirst(x => x.Ant.EqualTo(ant));
                    break;

                case PlanType.Hill:
                    _hillPlans.RemoveFirst(x => x.Ant.EqualTo(ant));
                    break;

                case PlanType.NewBorn:
                    _newBornPlans.RemoveFirst(x => x.Ant.EqualTo(ant));
                    break;

                case PlanType.Move:
                    _movePlans.RemoveFirst(x => x.Ant.EqualTo(ant));
                    break;

                case PlanType.Explore:
                    _explorePlans.RemoveFirst(x => x.Ant.EqualTo(ant));
                    break;

                case PlanType.HillGuard:
                    _hillGuards.RemoveFirst(x => x.EqualTo(ant));
                    break;

                case PlanType.None:
                default:
                    remove = false;
                    break;
            }
            if (remove)
                InternalRemovePlannedAnt(ant);
        }

        public void AddFreeAnt(Location ant)
        {
            _myFreeAnts.Add(ant.HashCode(), ant);
        }

        public void RemoveFreeAnt(Location ant)
        {
            _myFreeAnts.Remove(ant.HashCode());
        }

        /// <summary>
        /// Internal use
        /// </summary>
        /// <param name="ant"></param>
        private void InternalRemovePlannedAnt(Location ant)
        {
            _plannedAnts.Remove(ant.HashCode());
        }

        private void InternalAddPlannedAnt(Location ant, PlanType planType)
        {
            _plannedAnts.Add(ant.HashCode(), planType);
        }

        public bool HasPlan(Location ant)
        {
            return _plannedAnts.ContainsKey(ant.HashCode());
        }

        public PlanType? GetPlanType(Location ant)
        {
            return _plannedAnts[ant.HashCode()] as PlanType?;
        }

        /// <summary>
        /// Detects invalid plans, renew the planned ants list
        /// </summary>
        public void InvalidatePlans()
        {
            // remove free ants
            _myFreeAnts.Clear();

            // remove all plans!
            _plannedAnts.Clear();

            // this plans occurs once
            _movePlans.Clear();
            _myFreeAnts.Clear();

            // attack plans occurs once
            _attackPlans.Clear();

            _hillGuards.Clear();


            // explore plans ----------------
            for (int i = _explorePlans.Count - 1; i >= 0; i--)
            {
                var explorer = _explorePlans[i];

                if (explorer.PathCurrent != null)
                {
                    if (explorer.PathCurrent.Value.X != explorer.Ant.ColX || explorer.PathCurrent.Value.Y != explorer.Ant.RowY)
                    {
                        _explorePlans.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // no path current? ..invalid
                    _explorePlans.RemoveAt(i);
                    continue;
                }

                // not available deleted
                if (!GameState.MyAnts.Exists(x => x.EqualTo(explorer.Ant)))
                {
                    _explorePlans.RemoveAt(i);
                    continue;
                }

                // ok this plans is valid, goes to list
                InternalAddPlannedAnt(explorer.Ant, PlanType.Explore);
            }

            // new-born-ant plans-----------------
            for (int i = _newBornPlans.Count - 1; i >= 0; i--)
            {
                var newBorn = _newBornPlans[i];

                if (newBorn.PathCurrent != null)
                {
                    if (newBorn.PathCurrent.Value.X != newBorn.Ant.ColX || newBorn.PathCurrent.Value.Y != newBorn.Ant.RowY)
                    {
                        _newBornPlans.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // no path current? ..invalid
                    _newBornPlans.RemoveAt(i);
                    continue;
                }

                // ok this plans is valid, goes to list
                InternalAddPlannedAnt(newBorn.Ant, PlanType.NewBorn);
            }

            // hill plans ---------------------
            for (int i = _hillPlans.Count - 1; i >= 0; i--)
            {
                var hill = _hillPlans[i];

                if (hill.PathCurrent != null)
                {
                    if (hill.PathCurrent.Value.X != hill.Ant.ColX || hill.PathCurrent.Value.Y != hill.Ant.RowY)
                    {
                        _hillPlans.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // no path current? ..invalid
                    _hillPlans.RemoveAt(i);
                    continue;
                }

                // not available deleted
                if (!GameState.MyAnts.Exists(x => x.EqualTo(hill.Ant)))
                {
                    _hillPlans.RemoveAt(i);
                    continue;
                }

                try
                {
                    if (GameState[hill.Ant] != Tile.AntMine)
                    {
                        _hillPlans.RemoveAt(i);
                        continue;
                    }
                }
                catch (Exception)
                {
                    _hillPlans.RemoveAt(i);
                    continue;
                }

                try
                {
                    var dest = GameState[hill.GetFinalMoveLoc()];
                    if (dest != Tile.HillEnemy)
                    {
                        _hillPlans.RemoveAt(i);
                        continue;
                    }
                }
                catch (Exception)
                {
                    _hillPlans.RemoveAt(i);
                    continue;
                }

                // ok this plans is valid, goes to list
                InternalAddPlannedAnt(hill.Ant, PlanType.Hill);
            }

            // Food plans -----------------------------
            for (int i = _foodPlans.Count - 1; i >= 0; i--)
            {
                var food = _foodPlans[i];

                if (food.PathCurrent != null)
                {
                    if (food.PathCurrent.Value.X != food.Ant.ColX || food.PathCurrent.Value.Y != food.Ant.RowY)
                    {
                        _foodPlans.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // no path current? ..invalid
                    _foodPlans.RemoveAt(i);
                    continue;
                }

                // not available deleted
                if (!GameState.MyAnts.Exists(x => x.EqualTo(food.Ant)))
                {
                    _foodPlans.RemoveAt(i);
                    continue;
                }

                try
                {
                    if (GameState[food.Ant] != Tile.AntMine)
                    {
                        _foodPlans.RemoveAt(i);
                        continue;
                    }
                }
                catch (Exception)
                {
                    _foodPlans.RemoveAt(i);
                    continue;
                }

                try
                {
                    var dest = GameState[food.GetFinalMoveLoc()];
                    if (dest != Tile.Food)
                    {
                        _foodPlans.RemoveAt(i);
                        continue;
                    }
                }
                catch (Exception)
                {
                    _foodPlans.RemoveAt(i);
                    continue;
                }

                // ok this plans is valid, goes to list
                InternalAddPlannedAnt(food.Ant, PlanType.Food);
            }
        }

        public ICollection GetFreeAnts()
        {
            return _myFreeAnts.Values;
        }

        public List<AttackPlanDetail> GetAttackPlans()
        {
            return _attackPlans;
        }
        public List<SearchPlanDetail> GetFoodPlans()
        {
            return _foodPlans;
        }
        public List<SearchPlanDetail> GetHillPlans()
        {
            return _hillPlans;
        }
        public List<MovePlanDetail> GetMovePlans()
        {
            return _movePlans;
        }
        public List<SearchPlanDetail> GetNewBornPlans()
        {
            return _newBornPlans;
        }
        //public List<Location> MyFreeAnts;
        public List<SearchPlanDetail> GetExplorePlans()
        {
            return _explorePlans;
        }
        public List<Location> GetHillGuards()
        {
            return _hillGuards;
        }

        public BasePlan GetPlanByAnt(Location ant, PlanType plan)
        {
            switch (plan)
            {
                case PlanType.Move:
                    return _movePlans.FirstOrDefaultFast(x => x.Ant.EqualTo(ant));

                case PlanType.Attack:
                    return _attackPlans.FirstOrDefaultFast(x => x.Ant.EqualTo(ant));

                case PlanType.Food:
                    return _foodPlans.FirstOrDefaultFast(x => x.Ant.EqualTo(ant));

                case PlanType.Hill:
                    return _hillPlans.FirstOrDefaultFast(x => x.Ant.EqualTo(ant));

                case PlanType.NewBorn:
                    return _newBornPlans.FirstOrDefaultFast(x => x.Ant.EqualTo(ant));

                case PlanType.Explore:
                    return _explorePlans.FirstOrDefaultFast(x => x.Ant.EqualTo(ant));

                case PlanType.HillGuard:
                    return null;
            }
            return null;
        }
        public BasePlan GetPlanByDest(Location dest, PlanType plan)
        {
            switch (plan)
            {
                case PlanType.Move:
                    return _movePlans.FirstOrDefaultFast(x => x.Dest.EqualTo(dest));

                case PlanType.Attack:
                    return _attackPlans.FirstOrDefaultFast(x => x.NextStep.EqualTo(dest));

                case PlanType.Food:
                    return _foodPlans.FirstOrDefaultFast(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.Hill:
                    return _hillPlans.FirstOrDefaultFast(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.NewBorn:
                    return _newBornPlans.FirstOrDefaultFast(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.Explore:
                    return _explorePlans.FirstOrDefaultFast(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.HillGuard:
                    return null;
            }
            return null;
        }
        public int GetPlanCountByDest(Location dest, PlanType plan)
        {
            switch (plan)
            {
                case PlanType.Move:
                    return _movePlans.Count(x => x.Dest.EqualTo(dest));

                case PlanType.Attack:
                    return _attackPlans.Count(x => x.NextStep.EqualTo(dest));

                case PlanType.Food:
                    return _foodPlans.Count(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.Hill:
                    return _hillPlans.Count(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.NewBorn:
                    return _newBornPlans.Count(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.Explore:
                    return _explorePlans.Count(x => x.GetFinalMoveLoc().EqualTo(dest));

                case PlanType.HillGuard:
                    return _hillGuards.Count(x => x.EqualTo(dest));
            }
            return 0;
        }

        bool HasPlanDest(Location dest, params PlanType[] plans)
        {
            foreach (var plan in plans)
            {
                switch (plan)
                {
                    case PlanType.Move:
                        if (_movePlans.Exists(x => x.Dest.EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.Attack:
                        if (_attackPlans.Exists(x => x.NextStep.EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.Food:
                        if (_foodPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.Hill:
                        if (_hillPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.NewBorn:
                        if (_newBornPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;
                    case PlanType.Explore:
                        if (_explorePlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;
                }
            }
            return false;
        }

        #region issuing orders

        IList<MovePlanDetail> SmartMovePlan(List<MovePlanDetail> movePlanDetails)
        {
            var list = new List<MovePlanDetail>();
            var tempMovePlan = new List<MovePlanDetail>(movePlanDetails);

            do
            {
                bool added = false;

                // adding move plans that doesn't blocked by others
                for (int i = tempMovePlan.Count - 1; i >= 0; i--)
                {
                    var plan = tempMovePlan[i];

                    // the ants that does not have conflicts go first
                    if (tempMovePlan.Any(x => (!x.Ant.EqualTo(plan.Ant)) && !x.Dest.EqualTo(plan.Dest)))
                    {
                        list.Add(plan);
                        tempMovePlan.RemoveAt(i);
                        added = true;
                    }
                }

                // adding move plans that blocked by previous plans
                for (int i = tempMovePlan.Count - 1; i >= 0; i--)
                {
                    var plan = tempMovePlan[i];

                    // the ants that does not have conflicts go first
                    if (tempMovePlan.Any(x => x.Ant.EqualTo(plan.Dest)))
                    {
                        list.Add(plan);
                        tempMovePlan.RemoveAt(i);
                        added = true;
                    }
                }


                if (!added)
                    break;
            }
            while (true);

            foreach (var planDetail in tempMovePlan)
            {
                list.Add(planDetail);
            }
            return list;
        }

        public void IssuePlannedOrders(AntAStarSolver<AntAStarPathNode, Object> aStar, int searchLimit)
        {
            // remove previous done orders
            _thisTurnDoneOrders.Clear();

            var areWeInHurry = (_state.TimeRemaining < 20);

            // ------------------------------------------------
            // move plan is VERY important
            foreach (MovePlanDetail plan in SmartMovePlan(_movePlans))
            {
                // send orders
                IssueOrderMovePlan(plan);
            }

            // ------------------------------------------------
            // attack plan
            for (int i = _attackPlans.Count - 1; i >= 0; i--)
            //for (int i = 0; i < planMan.AttackPlans.Count; i++)
            {
                var plan = _attackPlans[i];
                var state = IssueOrderAttckPlan(plan, true);

                if (state == IssuedOrderState.PlanFinished ||
                    state == IssuedOrderState.Failed ||
                    state == IssuedOrderState.Blocked)
                {
                    _attackPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);
                }
            }

            // HILL------------------------------------------------
            for (int i = _hillPlans.Count - 1; i >= 0; i--)
            {
                SearchPlanDetail plan = _hillPlans[i];

                //#if TestInput
                //                if (VISUALizerCondition)
                //                {
                //                    if (planDetail.Path != null)
                //                        SalarAntsVisualRunner.AntMarkLocation(planDetail.Path, i);
                //                }
                //#endif

                var state = IssueOrderPlanDetail(plan, true);

                // is plan failed?
                if (state == IssuedOrderState.PlanFinished)
                {
                    _hillPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);

                    // continue the plan, if it was not finished before!
                    if (plan.IsPlanForGoal() == false && areWeInHurry == false)
                    {
                        var newPlan = AntAStarMain.GetFindPathPlan(
                            _state,
                            aStar,
                            plan.Ant,
                            plan.Goal,
                            searchLimit);

                        if (newPlan != null)
                            // new plan to hill!
                            AddPlan(newPlan, PlanType.Hill);
                    }
                }
                else if (state == IssuedOrderState.Failed)
                {
                    _hillPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);
                }
                else if (state == IssuedOrderState.Blocked)
                {
                    _hillPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);

                    if (areWeInHurry == false)
                    {
                        //if (plan.Ant != null && plan.Goal != null)
                        //{
                        //    // try to find a new plan for it!
                        //    var newPlan = AntAStarMain.GetFindPathPlan(
                        //        _state,
                        //        _aStar,
                        //        plan.Ant,
                        //        plan.Goal,
                        //        foodSearchLimit);

                        //    if (newPlan != null)
                        //        // new plan to hill!
                        //        AddPlan(newPlan);
                        //}
                    }
                }
            }

            // FOOD ------------------------------------------------
            for (int i = _foodPlans.Count - 1; i >= 0; i--)
            //for (int i = 0; i < planMan.FoodPlans.Count; i++)
            {
                SearchPlanDetail plan = _foodPlans[i];

                //#if TestInput
                //                if (VISUALizerCondition)
                //                {
                //                    if (planDetail.Path != null)
                //                        SalarAntsVisualRunner.AntMarkLocation(planDetail.Path, i);
                //                }
                //#endif

                var state = IssueOrderPlanDetail(plan, true);

                // is plan failed?
                if (state == IssuedOrderState.PlanFinished)
                {
                    _foodPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);

                    // continue the plan, if it was not finished before!
                    if (plan.IsPlanForGoal() == false && areWeInHurry == false)
                    {
                        var newPlan = AntAStarMain.GetFindPathPlan(
                            _state,
                            aStar,
                            plan.Ant,
                            plan.Goal,
                            searchLimit);

                        if (newPlan != null)
                            // new plan to food!
                            AddPlan(newPlan, PlanType.Food);
                    }
                }
                else if (state == IssuedOrderState.Failed)
                {
                    _foodPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);
                }
                else if (state == IssuedOrderState.Blocked)
                {
                    _foodPlans.RemoveAt(i);
                    InternalRemovePlannedAnt(plan.Ant);

                    if (areWeInHurry == false)
                    {
                        //if (plan.Ant != null && plan.Goal != null)
                        //{
                        //    // try to find a new plan for it!
                        //    var newPlan = AntAStarMain.GetFindPathPlan(
                        //        _state,
                        //        _aStar,
                        //        plan.Ant,
                        //        plan.Goal,
                        //        foodSearchLimit);

                        //    if (newPlan != null)
                        //        // new plan to food!
                        //        planMan.FoodPlans.Add(newPlan);
                        //}
                    }
                }
            }

            // ------------------------------------------------
            // new borns
            for (int index = _newBornPlans.Count - 1; index >= 0; index--)
            {
                SearchPlanDetail plan = _newBornPlans[index];

                var state = IssueOrderPlanDetail(plan, true);

                // is plan failed?
                if (state == IssuedOrderState.PlanFinished ||
                    state == IssuedOrderState.Failed
                    //state == IssuedOrderState.Blocked
                    )
                {
                    _newBornPlans.RemoveAt(index);
                    InternalRemovePlannedAnt(plan.Ant);
                }
                else if (state == IssuedOrderState.Blocked)
                {
                    _newBornPlans.RemoveAt(index);
                    InternalRemovePlannedAnt(plan.Ant);

                    //if (plan.Ant != null && plan.Goal != null)
                    //{
                    //    // try to find a new plan for it!
                    //    var newPlan = AntAStarMain.GetFindPathPlan(
                    //        _state,
                    //        _aStar,
                    //        plan.Ant,
                    //        plan.Goal,
                    //        foodSearchLimit);

                    //    if (newPlan != null)
                    //        // new plan to borned!
                    //        planMan.NewBornPlans.Add(newPlan);
                    //}

                }
            }

            // ------------------------------------------------
            // map explores
            for (int index = _explorePlans.Count - 1; index >= 0; index--)
            {
                SearchPlanDetail plan = _explorePlans[index];

                var state = IssueOrderPlanDetail(plan, true);

                if (state == IssuedOrderState.Blocked ||
                    state == IssuedOrderState.IssuedBefore)
                {
                    if (plan.Blocked >= 2)
                    {
                        _explorePlans.RemoveAt(index);
                        InternalRemovePlannedAnt(plan.Ant);
                    }
                    else
                    {
                        // blocked
                        plan.Blocked++;
                    }
                }
                else if (state == IssuedOrderState.PlanFinished ||
                    state == IssuedOrderState.Failed)
                //state == IssuedOrderState.Blocked)
                {
                    _explorePlans.RemoveAt(index);
                    InternalRemovePlannedAnt(plan.Ant);
                }
                else
                {
                    plan.Blocked = 0;
                }
            }
        }

        private IssuedOrderState IssueOrderMovePlan(MovePlanDetail plan, bool validateNextStep = true)
        {
            var directions = _state.GetDirections(plan.Ant, plan.Dest) as IList<Direction>;
            if (directions.Count > 0)
            {
                Location newLoc = _state.GetDestination(plan.Ant, directions[0]);
                if (validateNextStep)
                {
                    if (!_state.GetIsPassableSafe(newLoc))
                    {
                        return IssuedOrderState.Blocked;
                    }
                    if (!_state.GetIsUnoccupiedSafe(newLoc))
                    {
                        return IssuedOrderState.IssuedBefore;
                    }
                }

                // check if the new location is ordered?
                if (_thisTurnDoneOrders.Exists(x => x.EqualTo(newLoc)))
                //if (_thisTurnDoneOrders.Contains(newLoc))
                {
                    // not this turn!
                    // wait for next turn
                    return IssuedOrderState.IssuedBefore;
                }


                // first issue order
                Bot.IssueOrderStatic(plan.Ant, directions[0]);

                // add to orders
                _thisTurnDoneOrders.Add(newLoc);

                // save old location of ant
                var antOldLocation = plan.Ant;

                // change direction of my ant!
                plan.Ant = plan.Ant.CloneToDirection(directions[0]);

                // the new location of ant
                var antNewLocation = plan.Ant;

                // update map! for next moves this ant should be assumed in new location
                _state.UpdateMap(antOldLocation.RowY, antOldLocation.ColX, Tile.Land);
                _state.UpdateMap(antNewLocation.RowY, antNewLocation.ColX, Tile.AntMine);

                // done!
                return IssuedOrderState.Success;
            }
            return IssuedOrderState.Failed;
        }


        private IssuedOrderState IssueOrderAttckPlan(AttackPlanDetail plan, bool validateNextStep = false)
        {
            var directions = _state.GetDirections(plan.Ant, plan.NextStep) as IList<Direction>;
            if (directions.Count > 0)
            {
                Location newLoc = _state.GetDestination(plan.Ant, directions[0]);
                if (validateNextStep)
                {
                    if (!_state.GetIsPassableSafe(newLoc))
                    {
                        return IssuedOrderState.Blocked;
                    }
                    if (!_state.GetIsUnoccupiedSafe(newLoc))
                    {
                        return IssuedOrderState.IssuedBefore;
                    }
                }

                // check if the new location is ordered?
                if (_thisTurnDoneOrders.Exists(x => x.EqualTo(newLoc)))
                //if (_thisTurnDoneOrders.Contains(newLoc))
                {
                    // not this turn!
                    // wait for next turn
                    return IssuedOrderState.IssuedBefore;
                }


                // first issue order
                Bot.IssueOrderStatic(plan.Ant, directions[0]);

                // add to orders
                _thisTurnDoneOrders.Add(newLoc);

                // save old location of ant
                var antOldLocation = plan.Ant;
                
                // change direction of my ant!
                plan.Ant = plan.Ant.CloneToDirection(directions[0]);

                // the new location of ant
                var antNewLocation = plan.Ant;

                // BUG: this causes some issue!!!! Causes ghost ants!
                // bug is fixed!
                // update map! for next moves this ant should be assumed in new location
                _state.UpdateMap(antOldLocation.RowY, antOldLocation.ColX, Tile.Land);
                _state.UpdateMap(antNewLocation.RowY, antNewLocation.ColX, Tile.AntMine);


                // done!
                return IssuedOrderState.Success;
            }
            return IssuedOrderState.Failed;
        }


        private IssuedOrderState IssueOrderPlanDetail(SearchPlanDetail searchPlanDetail, bool validateNextStep)
        {
            // fist update next step!
            if (searchPlanDetail.PathCurrent != null)
            {
                var current = searchPlanDetail.PathCurrent;
                var nextStep = searchPlanDetail.PathCurrent.Next;
                searchPlanDetail.PathCurrent = nextStep;
                var last = searchPlanDetail.Path.Last;

                if (nextStep != null)
                {
                    // issue to next step!
                    var directions = _state.GetDirections(searchPlanDetail.Ant, nextStep.Value.ToLocation()) as IList<Direction>;
                    if (directions.Count > 0)
                    {
                        Location newLoc = _state.GetDestination(searchPlanDetail.Ant, directions[0]);
                        if (validateNextStep)
                        {
                            if (!_state.GetIsPassableSafe(newLoc))
                            {
                                return IssuedOrderState.Blocked;
                            }
                            if (!_state.GetIsUnoccupiedSafe(newLoc))
                            {
                                return IssuedOrderState.IssuedBefore;
                            }
                        }

                        // check if the new location is ordered?
                        if (_thisTurnDoneOrders.Exists(x => x.EqualTo(newLoc)))
                        //if (_thisTurnDoneOrders.Contains(newLoc))
                        {
                            // not this turn!
                            // wait for next turn
                            return IssuedOrderState.IssuedBefore;
                        }


                        searchPlanDetail.LeftSteps--;

                        // first issue order
                        Bot.IssueOrderStatic(searchPlanDetail.Ant, directions[0]);

                        // add to orders
                        _thisTurnDoneOrders.Add(newLoc);

                        // save old location of ant
                        var antOldLocation = searchPlanDetail.Ant;

                        // change direction of my ant!
                        searchPlanDetail.Ant = searchPlanDetail.Ant.CloneToDirection(directions[0]);

                        // the new location of ant
                        var antNewLocation = searchPlanDetail.Ant;

                        // update map! for next moves this ant should be assumed in new location
                        _state.UpdateMap(antOldLocation.RowY, antOldLocation.ColX, Tile.Land);
                        _state.UpdateMap(antNewLocation.RowY, antNewLocation.ColX, Tile.AntMine);


                        // is goal reached?
                        if (searchPlanDetail.Ant.ColX == last.Value.X && searchPlanDetail.Ant.RowY == last.Value.Y)
                        {
                            // the goal is reached!
                            return IssuedOrderState.PlanFinished;
                        }

                        return IssuedOrderState.Success;
                    }
                    return IssuedOrderState.Failed;
                }
                else
                {
                    // AStar sucked here!
                    searchPlanDetail.PathCurrent = null;
                    searchPlanDetail.Path = null;

                    // directly issue order!
                    var directions = _state.GetDirections(searchPlanDetail.Ant, searchPlanDetail.GetFinalMoveLoc()) as IList<Direction>;
                    if (directions.Count > 0)
                    {
                        Location newLoc = _state.GetDestination(searchPlanDetail.Ant, directions[0]);
                        if (validateNextStep)
                        {
                            if (!_state.GetIsPassableSafe(newLoc))
                            {
                                return IssuedOrderState.Blocked;
                            }
                            if (!_state.GetIsUnoccupiedSafe(newLoc))
                            {
                                return IssuedOrderState.IssuedBefore;
                            }
                        }

                        // check if the new location is ordered?
                        if (_thisTurnDoneOrders.Exists(x => x.EqualTo(newLoc)))
                        //if (_thisTurnDoneOrders.Contains(newLoc))
                        {
                            // not this turn!
                            // wait for next turn
                            return IssuedOrderState.IssuedBefore;
                        }

                        searchPlanDetail.LeftSteps--;

                        // first issue order
                        Bot.IssueOrderStatic(searchPlanDetail.Ant, directions[0]);

                        // add to orders
                        _thisTurnDoneOrders.Add(newLoc);

                        // change direction of my ant!
                        searchPlanDetail.Ant = searchPlanDetail.Ant.CloneToDirection(directions[0]);

                        // this plan has no goal!!
                        return IssuedOrderState.Success;
                    }
                    return IssuedOrderState.Failed;
                }
            }
            else
            {
                // directly issue order!
                var directions = _state.GetDirections(searchPlanDetail.Ant, searchPlanDetail.GetFinalMoveLoc()) as IList<Direction>;
                if (directions.Count > 0)
                {
                    Location newLoc = _state.GetDestination(searchPlanDetail.Ant, directions[0]);
                    if (validateNextStep)
                    {
                        if (!_state.GetIsPassableSafe(newLoc))
                        {
                            return IssuedOrderState.Blocked;
                        }
                        if (!_state.GetIsUnoccupiedSafe(newLoc))
                        {
                            return IssuedOrderState.IssuedBefore;
                        }
                    }

                    // check if the new location is ordered?
                    if (_thisTurnDoneOrders.Exists(x => x.EqualTo(newLoc)))
                    //if (_thisTurnDoneOrders.Contains(newLoc))
                    {
                        // not this turn!
                        // wait for next turn
                        return IssuedOrderState.IssuedBefore;
                    }

                    searchPlanDetail.LeftSteps--;

                    // first issue order
                    Bot.IssueOrderStatic(searchPlanDetail.Ant, directions[0]);

                    // add to orders
                    _thisTurnDoneOrders.Add(newLoc);

                    // change direction of my ant!
                    searchPlanDetail.Ant = searchPlanDetail.Ant.CloneToDirection(directions[0]);

                    // this plan has no goal!!
                    return IssuedOrderState.Success;
                }
                return IssuedOrderState.Failed;
            }
        }
        #endregion


        void RemoveInvalidPlans_old()
        {
            _movePlans.Clear();
            _myFreeAnts.Clear();
            //NewBornPlans.Clear();

            for (int i = _explorePlans.Count - 1; i >= 0; i--)
            {
                var explorer = _explorePlans[i];

                if (explorer.PathCurrent != null)
                    if (explorer.PathCurrent.Value.X != explorer.Ant.ColX || explorer.PathCurrent.Value.Y != explorer.Ant.RowY)
                    {
                        _explorePlans.RemoveAt(i);
                        continue;
                    }

                // not available deleted
                if (!GameState.MyAnts.Exists(x => x.EqualTo(explorer.Ant)))
                {
                    _explorePlans.RemoveAt(i);
                    continue;
                }

            }

            for (int i = _newBornPlans.Count - 1; i >= 0; i--)
            {
                var newBorn = _newBornPlans[i];

                if (newBorn.PathCurrent != null)
                    if (newBorn.PathCurrent.Value.X != newBorn.Ant.ColX || newBorn.PathCurrent.Value.Y != newBorn.Ant.RowY)
                    {
                        _newBornPlans.RemoveAt(i);
                        continue;
                    }
            }

            for (int i = _foodPlans.Count - 1; i >= 0; i--)
            {
                var food = _foodPlans[i];

                if (food.PathCurrent != null)
                    if (food.PathCurrent.Value.X != food.Ant.ColX || food.PathCurrent.Value.Y != food.Ant.RowY)
                    {
                        _foodPlans.RemoveAt(i);
                        continue;
                    }

                // not available deleted
                if (!GameState.MyAnts.Exists(x => x.EqualTo(food.Ant)))
                {
                    _foodPlans.RemoveAt(i);
                    continue;
                }

                try
                {
                    if (GameState[food.Ant] != Tile.AntMine)
                    {
                        _foodPlans.RemoveAt(i);
                        continue;
                    }
                }
                catch (Exception)
                {
                    _foodPlans.RemoveAt(i);
                    continue;
                }

                try
                {
                    var dest = GameState[food.GetFinalMoveLoc()];
                    if (dest != Tile.Food && dest != Tile.HillEnemy)
                    {
                        _foodPlans.RemoveAt(i);
                        continue;
                    }
                }
                catch (Exception)
                {
                    _foodPlans.RemoveAt(i);
                    continue;
                }
            }

            foreach (var deadTile in GameState.DeadTiles)
            {
                var antIndex = _foodPlans.FirstIndexOf(x => x.Ant.EqualTo(deadTile));
                if (antIndex >= 0)
                    _foodPlans.RemoveAt(antIndex);
                _attackPlans.RemoveFirst(x => x.Ant.EqualTo(deadTile));
                _attackPlans.RemoveFirst(x => x.Enemy.EqualTo(deadTile));
            }
        }


        /// <summary>
        /// Index of ant in food plan
        /// </summary>
        int GetFoodPlanIndexAnt(Location ant)
        {
            return _attackPlans.FirstIndexOf(x => x.Ant.EqualTo(ant));
        }

        //void AddFreeAntChecked(Location ant)
        //{
        //    // check if it has already a plan
        //    if (!HasPlanAnt(ant,
        //                 PlanType.Move,
        //                 PlanType.Attack,
        //                 PlanType.Food,
        //                 PlanType.Hill,
        //                 PlanType.Explore))
        //    {
        //        _myFreeAnts.Add(ant);
        //    }
        //}

        //void AddFreeAntChecked(Location ant, params PlanType[] plans)
        //{
        //    // check if it has already a plan
        //    if (!HasPlanAnt(ant, plans))
        //    {
        //        _myFreeAnts.Add(ant);
        //    }
        //}

        bool HasPlanDest_old(Location dest, params PlanType[] plans)
        {
            foreach (var plan in plans)
            {
                switch (plan)
                {
                    case PlanType.Move:
                        if (_movePlans.Exists(x => x.Dest.EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.Attack:
                        if (_attackPlans.Exists(x => x.NextStep.EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.Food:
                        if (_foodPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.Hill:
                        if (_foodPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;

                    case PlanType.NewBorn:
                        if (_newBornPlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;
                    case PlanType.Explore:
                        if (_explorePlans.Exists(x => x.GetFinalMoveLoc().EqualTo(dest)))
                            return true;
                        break;
                }
            }
            return false;
        }

        bool HasPlanAnt(Location ant, params PlanType[] plans)
        {
            foreach (var plan in plans)
            {
                switch (plan)
                {
                    case PlanType.Move:
                        if (_movePlans.Exists(x => x.Ant.EqualTo(ant)))
                            return true;
                        break;

                    case PlanType.Attack:
                        if (_attackPlans.Exists(x => x.Ant.EqualTo(ant)))
                            return true;
                        break;

                    case PlanType.Food:
                        if (_foodPlans.Exists(x => x.Ant.EqualTo(ant)))
                            return true;
                        break;

                    case PlanType.Hill:
                        if (_foodPlans.Exists(x => x.Ant.EqualTo(ant)))
                            return true;
                        break;

                    case PlanType.NewBorn:
                        if (_newBornPlans.Exists(x => x.Ant.EqualTo(ant)))
                            return true;
                        break;

                    case PlanType.Explore:
                        if (_explorePlans.Exists(x => x.Ant.EqualTo(ant)))
                            return true;
                        break;
                }
            }
            return false;
        }

        bool HasPlanAnt(Location ant)
        {
            bool food = _foodPlans.Exists(x => x.Ant.EqualTo(ant));
            if (food) return true;

            bool move = _movePlans.Exists(x => x.Ant.EqualTo(ant));
            if (move) return true;

            bool attack = _attackPlans.Exists(x => x.Ant.EqualTo(ant));
            if (attack) return true;

            var born = _newBornPlans.Exists(x => x.Ant.EqualTo(ant));
            if (born) return true;

            var exp = _explorePlans.Exists(x => x.Ant.EqualTo(ant));
            if (exp) return true;

            // has plan
            return false;
        }

        //public void ClearPlans()
        //{
        //    AttackPlans.Clear();
        //    FoodPlans.Clear();
        //    MovePlans.Clear();
        //    //NewBornPlans.Clear();
        //    MyFreeAnts.Clear();
        //}

        void CancelOtherPlansForAnt(Location ant)
        {
            _foodPlans.RemoveAll(x => x.Ant.EqualTo(ant));
            _attackPlans.RemoveAll(x => x.Ant.EqualTo(ant));
            _movePlans.RemoveAll(x => x.Ant.EqualTo(ant));
            _newBornPlans.RemoveAll(x => x.Ant.EqualTo(ant));
            _explorePlans.RemoveAll(x => x.Ant.EqualTo(ant));
        }

        void CancelPlans(Location ant, params PlanType[] plans)
        {
            foreach (var plan in plans)
            {
                switch (plan)
                {
                    case PlanType.Move:
                        _movePlans.RemoveAll(x => x.Ant.EqualTo(ant));
                        break;

                    case PlanType.Attack:
                        _attackPlans.RemoveAll(x => x.Ant.EqualTo(ant));
                        break;

                    case PlanType.Food:
                        _foodPlans.RemoveAll(x => x.Ant.EqualTo(ant));
                        break;

                    case PlanType.Hill:
                        _foodPlans.RemoveAll(x => x.Ant.EqualTo(ant));
                        break;

                    case PlanType.NewBorn:
                        _newBornPlans.RemoveAll(x => x.Ant.EqualTo(ant));
                        break;
                    case PlanType.Explore:
                        _explorePlans.RemoveAll(x => x.Ant.EqualTo(ant));
                        break;
                }
            }
        }
    }
}
