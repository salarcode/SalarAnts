#define DrawVisual

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SalarAnts.AStarV2;
using SalarAnts.Classes;
using SalarAnts.Defination;
using SalarAnts.Pack;
using SettlersEngine;

namespace SalarAnts.SalarBot
{
	public class SalarBotV1
	{
		private enum IssuedOrderState
		{
			PlanFinished,
			Success,
			Blocked,
			IssuedBefore,
			Failed
		};

		private GameState _state;
		private Bot _bot;
		private int _turn = 1;
		private readonly Dictionary<AntHill, IList<Location>> _hillsWays = new Dictionary<AntHill, IList<Location>>();
		//private List<SearchPlanDetail> _plansAttack = new List<SearchPlanDetail>();
		//private List<SearchPlanDetail> _plansFood = new List<SearchPlanDetail>();
		//private List<SearchPlanDetail> _plansNewBorn = new List<SearchPlanDetail>();
		private readonly PlanManagerV1 planMan = new PlanManagerV1();
		private Random _random = null;
		private readonly List<Location> _thisTurnDoneOrders = new List<Location>();

		// the map used for search!!
		private AntAStarPathNode[,] _astarAntsMap = null;
		private AntAStarSolver<AntAStarPathNode, Object> _aStar = null;

		public SalarBotV1(IGameState state, Bot bot)
		{
			State = state;
			Bot = bot;
		}

		public void EnsureAStar()
		{
			// -----------------------------------------------------------
			// Step: generate map for all of the uses
			if (_astarAntsMap == null)
				_astarAntsMap = AntAStarPathNode.GenerateMap(_state.Map, _state.Height, _state.Width);
			if (_aStar == null && _astarAntsMap != null)
				_aStar = new AntAStarSolver<AntAStarPathNode, Object>(_astarAntsMap);
		}

		public IGameState State
		{
			get { return _state; }
			set
			{
				_state = (GameState)value;
				if (_random == null && _state != null)
				{
					unchecked
					{
						int val = (int)_state.PlayerSeed;
						_random = new Random(val);
					}
				}
#if TestInput_NOT
				if (_state != null)
				{
					SalarAntsVisualRunner.VisualStart();
				}
#endif
			}
		}

		public Bot Bot
		{
			get { return _bot; }
			set { _bot = value; }
		}

		public void DoTurn()
		{
			try
			{
				// clear plan manager
				planMan.GameState = _state;

				// clean the orders
				_thisTurnDoneOrders.Clear();

				// update plans!
				planMan.RemoveInvalidPlans();
				RemoveInvalidEnemyHills();

				// Thresholds -----------------------------------------------------------
				var thisTurnThreshold = ((_state.Width * _state.Height) / 2) / (_state.MyAnts.Count);
				if (thisTurnThreshold < 50)
					thisTurnThreshold = 50; // minimum is 20
				var farTurnThreshold = (int)(thisTurnThreshold + (thisTurnThreshold / 1.2));
				var foodTurnThreshold = (int)(thisTurnThreshold + (thisTurnThreshold / 1.1));
				var enemyTurnThreshold = (int)(thisTurnThreshold - (thisTurnThreshold / 1.5));
				if (enemyTurnThreshold > 10)
					enemyTurnThreshold = 10;
				else if (enemyTurnThreshold < 3)
					enemyTurnThreshold = 3;


				// ---------------------------
				// first turn!
				if (_turn == 1)
				{
					foreach (var myHill in _state.MyHills)
					{
						var hillWays = CheckingAround.CheckAround(_state, myHill, 53, false);
						_hillsWays.Add(myHill, hillWays);
					}
				}

#if TestInput_NOT
				bool drawHills = false;
				bool VISUALizerCondition = false;
				VISUALizerCondition = _turn >= 2;

				//if (_turn > 5)
				//{
				//    System.Windows.Forms.Application.Exit();
				//    return;
				//}

				if (drawHills && _turn == 1)
				{
					SalarAntsVisualRunner.UpdateVisualInit(_state);

					foreach (var hillsWay in _hillsWays.First().Value)
					{
						SalarAntsVisualRunner.MarkLocation(hillsWay.ColX, hillsWay.RowY, 1);
					}
				}
#endif

#if  TestInputVisual
				if (true)
				{
					if (_turn == 1)
					{
						var thisAnt = new Location(10, 10);
						var myAnts = new List<Location>
						             	{
						             		new Location(14, 11),
						             		new Location(10, 12),
						             		new Location(15, 12)
						             	};
						var enemyAnts = new List<Location>
						                	{
						                		new Location(11, 9)
						                	};
						foreach (Location m in myAnts)
						{
							_state.AddAnt(m.RowY, m.ColX, 0);
						}
						foreach (Location m in enemyAnts)
						{
							_state.AddAnt(m.RowY, m.ColX, 1);
						}

						//var done = Attack(thisAnt, myAnts, enemyAnts);
						SalarAntsVisualRunner.UpdateVisualInit(_state);



						//myAnts.Add(thisAnt);
						//SalarAntsVisualRunner.AntSetAnts(myAnts, enemyAnts);
						//SalarAntsVisualRunner.AntMarkLocation(thisAnt.ColX, thisAnt.RowY, 6);
						//foreach (AttackPlanDetail attackPlan in planMan.AttackPlans)
						//{
						//    SalarAntsVisualRunner.AntMarkLocation(attackPlan.NextStep.ColX, attackPlan.NextStep.RowY, 1);
						//}

						var circle = Common.CircleMidPoint(enemyAnts[0].ColX, enemyAnts[0].RowY, (_state.AttackRadius2 + 2) / 2);
						foreach (Location location in circle)
						{
							SalarAntsVisualRunner.MarkLocation(location.ColX, location.RowY, 3);
						}


						//return;
					}
					else
					{
						SalarAntsVisualRunner.UpdateVisualInit(_state);

					}

					Thread.Sleep(1000);
				}
#endif


				// -----------------------------------
				// all of not planned ants!-----------
				//var freeAnts = new List<Location>();

				// ---------------------------
				// Checking for new born ants
				foreach (var myHill in _state.MyHills)
				{
					IList<Location> locations = new List<Location>();
					//locations = CheckingAround.FindTargetsInRadius(
					//   _state, myHill, 3,
					//   _state.GetIsPassableSafe,
					//   x =>
					//   {
					//       var tile = _state[x];
					//       return tile == Tile.AntMine;
					//   });

					// is there any ant ON hill?
					if (_state[myHill] == Tile.AntMine)
						locations.Add(myHill);

					foreach (Location newBornAnt in locations)
					{
						if (planMan.HasPlanAnt(newBornAnt))
							continue;

						// out of hill
						SendNewBornOutOfHill(myHill, newBornAnt, foodTurnThreshold);
					}
				}

#if TestInput_NOT
				if (VISUALizerCondition)
				{
					SalarAntsVisualRunner.UpdateVisualInit(_state);
				}
#endif

				// ---------------------------
				// Checking around for activity
				// adll ants!
				foreach (var myAnt in _state.MyAnts)
				{
					if (_state.TimeRemaining < 20) break;

					//// ignore this ant if it has special plans
					//if (planMan.HasPlanAnt(myAnt,
					//    PlanManagerV1.PlanType.Attack,
					//    PlanManagerV1.PlanType.Move))
					//{
					//    continue;
					//}

					// is there food plan for this ant
					//var planFoodForThisAntIndex = planMan.GetFoodPlanIndexAnt(myAnt);

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

					if (aroundTargets.Count == 0)
					{
						// add free ant to list if it is not planned before!
						// Move, and Attack are checked before!
						planMan.AddFreeAntChecked(
							myAnt,
							//PlanManagerV1.PlanType.Move,
							PlanManagerV1.PlanType.Attack,
							PlanManagerV1.PlanType.Food,
							PlanManagerV1.PlanType.Hill);
					}
					else
					{
						bool thisAntIsFree = true;
						//if (planFoodForThisAntIndex != -1)
						//    thisAntIsFree = false;

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
						}

						// ENEMY ------------------------------------
						// enemy is in first
						if (thisAntIsFree && aroundAntEnemy.Count > 0)
						{
							var searchAntRadius = (_state.AttackRadius2 / 2) + 3;

							var aroundAnts = CheckingAround.FindTargetsInRadius(
								_state, myAnt,
								// search radius
								searchAntRadius,
								// is passable?
								_state.GetIsPassableSafe,
								// is taget?
								x =>
								{
									var locTile = _state[x];
									if (locTile == Tile.AntEnemy || locTile == Tile.AntMine)
										return true;
									return false;
								});

							var antAroundMine = new List<Location>();
							var antAroundEnemy = new List<Location>();
							foreach (var aroundT in aroundAnts)
							{
								var locTile = _state[aroundT];
								switch (locTile)
								{
									case Tile.AntMine:
										antAroundMine.Add(aroundT);
										break;

									case Tile.AntEnemy:
										antAroundEnemy.Add(aroundT);
										break;
								}
							}

							// ++++++++++++++++++++++++++++++++++++++
							// ++++++++++++++++++++++++++++++++++++++
							// ++++++++++++++++++++++++++++++++++++++
							// ++++++++++++++++++++++++++++++++++++++
							// ++++++++++++++++++++++++++++++++++++++
							// Only IF ENEMY-ANTS > MY-ANTS 

							// more enemy this any is not free
							if (antAroundEnemy.Count > antAroundMine.Count)
							{
								// cancel plans
								thisAntIsFree = false;

								planMan.CancelPlans(myAnt,
									PlanManagerV1.PlanType.Food,
									PlanManagerV1.PlanType.Explore,
									PlanManagerV1.PlanType.NewBorn);
							}

							// try to send attack
							bool attackIssued = false;// Attack(myAnt, antAroundMine, antAroundEnemy);

							if (attackIssued)
							{
								thisAntIsFree = false;
							}
							else if (antAroundEnemy.Count > antAroundMine.Count)
							{
								// ++++++++++++++++++++++++++++++++++++++
								// DEFENCE

								var attackOrDefLocation = AttackOrDefence(myAnt, searchAntRadius, antAroundMine, antAroundEnemy);

								// succeeed?
								if (attackOrDefLocation != null &&
									// no action?
									!attackOrDefLocation.EqualTo(myAnt))
								{
									Location nextLocation = attackOrDefLocation;
									var directions = _state.GetDirections(myAnt, attackOrDefLocation) as IList<Direction>;
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

										nextLocation = dirLocs.MinValue(x => x.ZVal);
										if (nextLocation == null)
										{
											nextLocation = attackOrDefLocation;
										}
									}

									// no any more
									thisAntIsFree = false;

									// cancel all other plans!!!
									planMan.CancelOtherPlansForAnt(myAnt);

									// add new simple plan
									planMan.MovePlans.Add(new MovePlanDetail
															{
																Ant = myAnt,
																Dest = nextLocation
															});
								}
								else
								{
									//------------------------------------------------------
									// Evade for now only
									//------------------------------------------------------

									var enemyWightList = new List<AntWeight>();
									foreach (var antEnemy in aroundAntEnemy)
									{
										enemyWightList.Add(new AntWeight()
															{
																Ant = myAnt,
																Weight = _state.GetDistanceDirect(myAnt, antEnemy),
															});
									}

									// is evade needed!
									var closest = enemyWightList.MinValue(x => x.Weight);
									if (closest.Ant != null)
									{
										// is close enough?
										if (closest.Weight < (_state.AttackRadius2 / 2) + 1)
										{
											// this evades!!
											var evadeLocation = FarDistanceToLocation(myAnt, closest.Ant);
											if (evadeLocation != null)
											{
												// no any more
												thisAntIsFree = false;

												// cancel all other plans!!!
												planMan.CancelOtherPlansForAnt(myAnt);

												// add new simple plan
												planMan.MovePlans.Add(new MovePlanDetail
																		{
																			Ant = myAnt,
																			Dest = evadeLocation
																		});
											}
										}
									}
								}
							}
						}

						// ENEMY HILL ------------------------------------
						if (thisAntIsFree && aroundEnemyHills.Count > 0)
						{
							foreach (Location hill in aroundEnemyHills)
							{
								if (!planMan.HasPlanDest(hill, PlanManagerV1.PlanType.Hill))
								{
									var hillPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, myAnt, hill, foodTurnThreshold);

									if (hillPlan != null)
									{
										// remove other plans for that
										planMan.CancelOtherPlansForAnt(hillPlan.Ant);

										// --------
										planMan.FoodPlans.Add(hillPlan);

										// this ant is not free anymore
										thisAntIsFree = false;
									}
								}

								// stop this loop if the order is issued
								if (thisAntIsFree == false)
									break;
							}
						}

						// FOOD ------------------------------------
						if (thisAntIsFree && aroundFood.Count > 0)
						{
							foreach (var food in aroundFood)
							{
								var previousFoodPlan = planMan.FoodPlans.FirstOrDefault(x => x.GetFinalMoveLoc().EqualTo(food));
								if (previousFoodPlan != null)
								{
									var newFoodPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, myAnt, food, foodTurnThreshold);
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

											// --------
											planMan.FoodPlans.Add(newFoodPlan);

											// this ant is not free anymore
											thisAntIsFree = false;
										}
										else
										{
											// previous plan is better
										}
									}
								}
								else
								{
									// no previous plan!
									// find a path to the food
									var foodPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, myAnt, food, foodTurnThreshold);

									if (foodPlan != null)
									{
										// --------
										planMan.FoodPlans.Add(foodPlan);

										// this ant is not free anymore
										thisAntIsFree = false;
									}
								}

								// stop this loop if the order is issued
								if (thisAntIsFree == false)
									break;
							}
						}

						// MY ANTS ----------------------------
						if (thisAntIsFree && aroundAntMine.Count > 0)
						{
							// 
						}

						// FREE --------------------------------------
						// is this ant still free!???
						if (thisAntIsFree)
						{
							// add free ant to list if it is not planned before!
							// Move, and Attack are checked before!
							planMan.AddFreeAntChecked(
								myAnt,
								//PlanManagerV1.PlanType.Move,
								//PlanManagerV1.PlanType.Attack,
								PlanManagerV1.PlanType.Food,
								PlanManagerV1.PlanType.Hill);
						}
						else
						{
							// not free anymore
							// make sure it is not in new born list, remove it!
							planMan.CancelPlans(myAnt,
								PlanManagerV1.PlanType.NewBorn,
								PlanManagerV1.PlanType.Explore);
						}
					}
				}

				if (_state.TimeRemaining < 20)
				{
					IssuePlannedOrders(foodTurnThreshold);
					return;
				}

				// --------------------------
				//var antsTempPlan = new List<SearchPlanDetail>();
				var antsWeight = new List<AntWeight>();

				// ---------------------------
				// going to eat food!!
				if (planMan.MyFreeAnts.Count > 0)
					foreach (var food in _state.FoodTiles)
					{
						if (_state.TimeRemaining < 20)
							break;

						// is this food planned before?
						if (planMan.HasPlanDest(food, PlanManagerV1.PlanType.Food))
						{
							continue;
						}

						antsWeight.Clear();

						// the found index
						int freeAntIndex;

						// finding closest free ant to this food!
						for (freeAntIndex = 0; freeAntIndex < planMan.MyFreeAnts.Count; freeAntIndex++)
						{
							var myAnt = planMan.MyFreeAnts[freeAntIndex];

							var weight = _state.GetDistance(myAnt, food);
							antsWeight.Add(new AntWeight
											{
												Ant = myAnt,
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
								foodTurnThreshold);

							if (foodPlan != null)
							{
								planMan.FoodPlans.Add(foodPlan);

								// remove from free ants
								planMan.MyFreeAnts.Remove(closeAntToFood.Ant);

								// not free anymore
								// make sure it is not in new born list, remove it!
								//planMan.NewBornPlans.RemoveAll(x => x.Ant.EqualTo(closeAntToFood.Ant));

								planMan.CancelPlans(
									closeAntToFood.Ant,
									PlanManagerV1.PlanType.Explore,
									PlanManagerV1.PlanType.NewBorn);
							}
						}
					}



				// ---------------------
				// Go for enemy HILLS
				if (planMan.MyFreeAnts.Count > 0 && _state.EnemyHills.Count > 0)
				{
					foreach (AntHill enemyHill in _state.EnemyHills)
					{
						int issued = 0;

						for (int freeAntIndex = planMan.MyFreeAnts.Count - 1; freeAntIndex >= 0; freeAntIndex--)
						{
							if (issued >= 2)
								break;
							issued++;
							Location myFreeAnt = planMan.MyFreeAnts[freeAntIndex];


							// search a path for it!
							var enemyHillPlan = AntAStarMain.GetFindPathPlan(
								_state,
								_aStar,
								myFreeAnt,
								enemyHill,
								foodTurnThreshold);

							if (enemyHillPlan != null)
							{
								planMan.FoodPlans.Add(enemyHillPlan);

								// remove from free ants
								planMan.MyFreeAnts.RemoveAt(freeAntIndex);

								// not free anymore
								planMan.CancelPlans(myFreeAnt, PlanManagerV1.PlanType.NewBorn);
							}
						}
					}
				}

				// ---------------------
				// EXPLORE the MAP
				if (planMan.MyFreeAnts.Count > 0)
				{
					_state.ClearVision();
					_state.SetVision();

					// 
					if (_state.Unvisiable.Count > 0)
					{
						// zero them
						_state.Unvisiable.ForEach(x => x.ZVal = -1);

						for (int freeAntIndex = planMan.MyFreeAnts.Count - 1; freeAntIndex >= 0; freeAntIndex--)
						{
							Location myFreeAnt = planMan.MyFreeAnts[freeAntIndex];

							// only not planned ant!!
							if (planMan.HasPlanAnt(myFreeAnt))
								continue;

							// how much try to find a unvisible place!?
							int unvisibleIndex = 0;

							// closest unvisible

							SearchPlanDetail explorePlan = null;
							int tryCount = 0;


							// random selection!
							//unvisibleIndex = _random.Next(0, _state.Unvisiable.Count - 1);
							var exploreTarget =
								_state.Unvisiable.MinValue(x => (x.ZVal == -1) ? _state.GetDistanceDirect(x, myFreeAnt) : int.MaxValue);
							//_state.Unvisiable[unvisibleIndex];

							if (exploreTarget != null)
							{
								exploreTarget.ZVal = 1;

								// search a path for it!
								explorePlan = AntAStarMain.GetFindPathPlan(
									_state,
									_aStar,
									myFreeAnt,
									exploreTarget,
									foodTurnThreshold);

								if (explorePlan != null)
								{
									planMan.ExplorePlans.Add(explorePlan);

									// remove from free ants
									planMan.MyFreeAnts.RemoveAt(freeAntIndex);

									// not free anymore
									planMan.CancelPlans(myFreeAnt, PlanManagerV1.PlanType.NewBorn);

									// not free anymore
									// make sure it is not in new born list, remove it!
									//planMan.CancelPlans(myFreeAnt, PlanManagerV1.PlanType.NewBorn);

								}
							}
						}
					}

				}

				// ---------------------
				// issue the orders planned!

				// using debug information
				DrawDebugInfo();

				// issue order
				IssuePlannedOrders(foodTurnThreshold);

#if TestInput_NOT
				SalarAntsVisualRunner.UpdateVisualInit(_state);
				SalarAntsVisualRunner.DrawPlans(planMan);
#endif

			}
			finally
			{
				_turn++;
			}

		}

		void DrawDebugInfo()
		{
#if DEBUG
			BotHelpers.OverlaySetLineColor(System.Drawing.Color.HotPink);
			foreach (var p in planMan.MovePlans)
			{
				BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.Dest.ColX, p.Dest.RowY);
			}

			BotHelpers.OverlaySetFillColor(System.Drawing.Color.Red);
			foreach (var freeAnt in planMan.MyFreeAnts)
			{
				BotHelpers.OverlayCircle(freeAnt.ColX, freeAnt.RowY, 0.2f, true);
			}

			BotHelpers.OverlaySetLineColor(System.Drawing.Color.Green);
			foreach (var p in planMan.FoodPlans)
			{
				BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
			}

			BotHelpers.OverlaySetLineColor(System.Drawing.Color.LightGreen);
			foreach (var p in planMan.FoodPlans)
			{
				var next = p.PathCurrent;
				if (next != null && (next = next.Next) != null)
					BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
			}

			// new born!
			BotHelpers.OverlaySetLineColor(System.Drawing.Color.Yellow);
			foreach (var p in planMan.NewBornPlans)
			{
				BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
			}

			BotHelpers.OverlaySetLineColor(System.Drawing.Color.WhiteSmoke);
			foreach (var p in planMan.NewBornPlans)
			{
				var next = p.PathCurrent;
				if (next != null && (next = next.Next) != null)
					BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
			}

			// explore
			BotHelpers.OverlaySetLineColor(System.Drawing.Color.Blue);
			foreach (var p in planMan.ExplorePlans)
			{
				BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.GetFinalMoveLoc().ColX, p.GetFinalMoveLoc().RowY);
			}

			BotHelpers.OverlaySetLineColor(System.Drawing.Color.CornflowerBlue);
			foreach (var p in planMan.ExplorePlans)
			{
				var next = p.PathCurrent;
				if (next != null && (next = next.Next) != null)
					BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, next.Value.X, next.Value.Y);
			}

			BotHelpers.OverlaySetLineColor(System.Drawing.Color.DarkRed);
			foreach (var p in planMan.AttackPlans)
			{
				BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.NextStep.ColX, p.NextStep.RowY);
			}

			BotHelpers.OverlaySetLineColor(System.Drawing.Color.Red);
			foreach (var p in planMan.AttackPlans)
			{
				BotHelpers.OverlayLine(p.Ant.ColX, p.Ant.RowY, p.Enemy.ColX, p.Enemy.RowY);
			}


#endif
		}

		/// <summary>
		/// A location next to my ant, which its distance to enemy is same as my ant
		/// </summary>
		Location NearLocationToEnemyNextToSrc(Location theAntSrc, Location shouldMoveAnt, Location theEnemy,
												  bool checkIfLocationIsNoOccupied = true)
		{
			var neighborNodes = new Location[4];
			Common.GetLocationNeighborsMirrored(_state, neighborNodes, theAntSrc);
			var theAntSrcDistanceToEnemy = _state.GetDistanceDirect(theAntSrc, theEnemy);

			var neighborNode = neighborNodes.MinValue(
				x =>
				{
					if (checkIfLocationIsNoOccupied && !_state.GetIsUnoccupiedSafe(x))
						return int.MaxValue;

					var thisToEnemy = _state.GetDistanceDirect(x, theEnemy);
					if (theAntSrcDistanceToEnemy != thisToEnemy)
					{
						thisToEnemy++;
						//return int.MaxValue;
					}

					var shouldMoveToEnemy = _state.GetDistanceDirect(x, shouldMoveAnt);
					return thisToEnemy + shouldMoveToEnemy;
				});
			return neighborNode;

		}


		/// <summary>
		/// Moves all ants around the enemy by attackRadius
		/// </summary>
		void MakeAntsGroupEqualToEnemy(IList<Location> enemyCirclePoints, IList<AntWeight> antsGroup, Location enemy)
		{
			// ordered by 
			antsGroup = antsGroup.OrderBy(x => x.Weight).ToList();
			var closeDistanceToEnemy = _state.GetDistance(antsGroup[0].Ant, enemy);// antsGroup[0].Weight;

			foreach (AntWeight antWeight in antsGroup)
			{
				// they are close enough
				if (_state.GetDistance(antWeight.Ant, enemy) < closeDistanceToEnemy)
					continue;

				var minAttackPoint = enemyCirclePoints.MinValue(
					x =>
					{
						// -1 means issue went there before!
						if (!_state.GetIsUnoccupiedSafe(x) || x.ZVal == -1)
							return x.ZVal = int.MaxValue;

						return x.ZVal = _state.GetDistanceDirect(x, antWeight.Ant);
					});

				// no luck!
				if (minAttackPoint == null)
					continue;

				// moveToReadyForAttack
				var myAnt = antWeight.Ant;
				Location moveToReadyForAttack = minAttackPoint;

				// make distance equal
				var directions = _state.GetDirections(antWeight.Ant, minAttackPoint) as IList<Direction>;
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
				planMan.CancelOtherPlansForAnt(myAnt);

				// add new simple plan
				planMan.AttackPlans.Add(new AttackPlanDetail
				{
					NextStep = moveToReadyForAttack,
					Ant = myAnt,
					Enemy = enemy,
					//LeaderAnt = null,
				});
			}
		}

		bool Attack(Location thisAnt, IList<Location> myAnts, IList<Location> enemyAnts)
		{
			if (thisAnt == null)
				return false;
			if (enemyAnts == null || enemyAnts.Count == 0)
				return false;

			if (myAnts == null || myAnts.Count == 0)
				return false;

			// steps to dead
			var attackRadius = (_state.AttackRadius2 / 2) + 1;
			var attackAntSupportRange = (_state.AttackRadius2 / 2) + 2;

			// close enemt to this ant!
			var closeEnemyToThis = enemyAnts.MinValue(x => (x.ZVal = _state.GetDistanceDirect(thisAnt, x)));
			var closeEnemyToThisWeight = closeEnemyToThis.ZVal;


			var myAntCloseToThislist = new List<AntWeight>();
			foreach (var ant in myAnts)
			{
				var weightToThis = _state.GetDistanceDirect(thisAnt, ant);
				var weightToEnemy = _state.GetDistanceDirect(closeEnemyToThis, ant);

				if (weightToThis <= attackAntSupportRange)// && weightToEnemy <= attackRadius)
				{
					// grouping ants in kind of random groups!!
					if (!planMan.HasPlanAnt(ant, PlanManagerV1.PlanType.Attack))
						myAntCloseToThislist.Add(new AntWeight()
						{
							Ant = ant,
							Weight = weightToEnemy,
						});
				}
			}

			if (myAntCloseToThislist.Count == 0)
			{
				// just evade!
				return false;
			}

			// this ant is also included!
			myAntCloseToThislist.Add(new AntWeight()
			{
				Ant = thisAnt,
				Weight = closeEnemyToThisWeight
			});

			var enemyCloseAntToThisList = new List<AntWeight>();
			foreach (var antEnemy in enemyAnts)
			{
				// closest my ant to this enemy
				var weightToThis = myAntCloseToThislist.Min(x => _state.GetDistanceDirect(antEnemy, x.Ant));
				var weightToEnemy = _state.GetDistanceDirect(closeEnemyToThis, antEnemy);

				if (weightToThis < attackAntSupportRange && weightToEnemy <= attackRadius)
				{
					enemyCloseAntToThisList.Add(new AntWeight()
					{
						Ant = antEnemy,
						Weight = weightToThis,
					});
				}
			}

			// send attack?
			if (myAntCloseToThislist.Count > enemyCloseAntToThisList.Count)
			{
				// attack points
				var enemyCirclePoints = Common.CircleMidPoint(closeEnemyToThis.ColX, closeEnemyToThis.RowY, attackRadius);

				// attack or wait?
				int myAntCountAround = 0;
				foreach (AntWeight antWeight in myAntCloseToThislist)
				{
					if (enemyCirclePoints.Any(x => x.EqualTo(antWeight.Ant)))
					{
						myAntCountAround++;
					}
					if (myAntCountAround >= 2)
					{
						break;
					}
				}


				if (myAntCountAround >= 2)
				{
					// send attack to them
					// just send all my ants to enemy, make distance equal first
					foreach (AntWeight myAntInfo in myAntCloseToThislist)
					{
						var myAnt = myAntInfo.Ant;
						var directions = _state.GetDirections(myAntInfo.Ant, closeEnemyToThis) as IList<Direction>;

						var attackOrDefLocation = closeEnemyToThis;
						Location nextLocation = attackOrDefLocation;

						// the close failed, try other ways!
						if (!_state.GetIsUnoccupiedSafe(nextLocation))
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

							nextLocation = dirLocs.MinValue(x => x.ZVal);
							if (nextLocation == null)
							{
								nextLocation = attackOrDefLocation;
							}
						}

						// no any more
						//thisAntIsFree = false;

						// cancel all other plans!!!
						planMan.CancelOtherPlansForAnt(myAnt);

						// add new simple plan
						planMan.AttackPlans.Add(new AttackPlanDetail
						{
							NextStep = nextLocation,
							Ant = myAnt,
							Enemy = closeEnemyToThis,
							//LeaderAnt = thisAnt
						});
					}
				}
				else
				{
					// first add all ant to list
					if (myAnts.Count > myAntCloseToThislist.Count)
					{
						foreach (var a in myAnts)
						{
							if (!myAntCloseToThislist.Exists(x => x.Ant.EqualTo(a)))
							{
								myAntCloseToThislist.Add(new AntWeight
								{
									Ant = a,
									Weight = _state.GetDistanceDirect(a, closeEnemyToThis)
								});
							}
						}
					}


					MakeAntsGroupEqualToEnemy(enemyCirclePoints, myAntCloseToThislist, closeEnemyToThis);
				}

				// done!
				return true;
			}
			else
			{
				// evade
				return false;
			}
			return false;
		}

		/// <summary>
		///  Attack or defence again enemies
		/// </summary>
		/// <param name="thisAnt">The ants goinf to be cheched!</param>
		/// <param name="distanceFactorRadius">Distance of check, closer ants are stronger</param>
		internal static Location AttackOrDefence(Location thisAnt, int distanceFactorRadius, IList<Location> myAnts, IList<Location> enemyAnts)
		{
			// the initial value is the location of my ant!
			int finalX = thisAnt.ColX;
			int finalY = thisAnt.RowY;

			foreach (Location myAnt in myAnts)
			{
				// more than this distance does not affect
				if (Math.Abs(thisAnt.RowY - myAnt.RowY) > distanceFactorRadius)
					continue;

				// more than this distance does not affect
				if (Math.Abs(thisAnt.ColX - myAnt.ColX) > distanceFactorRadius)
					continue;

				// the diffecnce point
				var close = CloseAPointAgainAnotherDiff(myAnt, thisAnt);

				// diffence line
				var difX = Math.Abs(Math.Abs(myAnt.ColX) - Math.Abs(thisAnt.ColX));
				var difY = Math.Abs(Math.Abs(myAnt.RowY) - Math.Abs(thisAnt.RowY));
				var lenOfDiffLine = Math.Sqrt((difX * difX) + (difY * difY));

				// the power of the ant!
				var decreaseVal = (int)(distanceFactorRadius - lenOfDiffLine);

				// the location that has the power
				close = DecreaseValueFromLine(close.ColX, close.RowY, decreaseVal);

				// add to final
				finalX += close.ColX;
				finalY += close.RowY;
			}

			foreach (Location enemyAnt in enemyAnts)
			{
				// more than this distance does not affect
				if (Math.Abs(thisAnt.ColX - enemyAnt.ColX) > distanceFactorRadius)
					continue;

				// more than this distance does not affect
				if (Math.Abs(thisAnt.RowY - enemyAnt.RowY) > distanceFactorRadius)
					continue;

				// the mirror point
				var mirrored = MirrorAPointAgainAnotherDiff(enemyAnt, thisAnt);


				// diffence line
				var difX = Math.Abs(Math.Abs(enemyAnt.ColX) - Math.Abs(thisAnt.ColX));
				var difY = Math.Abs(Math.Abs(enemyAnt.RowY) - Math.Abs(thisAnt.RowY));
				var lenOfDiffLine = Math.Sqrt((difX * difX) + (difY * difY));

				// the power of the ant!
				var decreaseVal = (int)(distanceFactorRadius - lenOfDiffLine);

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

		static Location IncreaseLengthOfLine(int x1, int y1, int incVal)
		{
			var t1 = Math.Sqrt((x1 * x1) + (y1 * y1));
			var t2 = t1 + incVal;

			// teta
			var teta = Math.Asin(x1 / t1);

			var x2 = (int)(t2 * Math.Sin(teta));
			var y2 = (int)(t2 * Math.Cos(teta));

			return new Location(y2, x2);
		}

		void RemoveInvalidEnemyHills()
		{
			_state.EnemyHills.RemoveWhere(enemyHill =>
				_state.GetIsVisible(enemyHill) &&
				_state[enemyHill] != Tile.HillEnemy);
		}


		private void IssuePlannedOrders(int foodTurnThreshold)
		{
			var areWeInHurry = (_state.TimeRemaining < 20);

			// ------------------------------------------------
			// move plan is VERY important
			foreach (MovePlanDetail plan in planMan.MovePlans)
			{
				// send orders
				IssueOrderMovePlan(plan);
			}

			// ------------------------------------------------
			// attack plan
			for (int i = planMan.AttackPlans.Count - 1; i >= 0; i--)
			//for (int i = 0; i < planMan.AttackPlans.Count; i++)
			{
				var plan = planMan.AttackPlans[i];
				var state = IssueOrderAttckPlan(plan, true);

				if (state == IssuedOrderState.PlanFinished ||
					state == IssuedOrderState.Failed ||
					state == IssuedOrderState.Blocked)
				{
					planMan.AttackPlans.RemoveAt(i);
				}
			}

			// ------------------------------------------------
			for (int i = planMan.FoodPlans.Count - 1; i >= 0; i--)
			//for (int i = 0; i < planMan.FoodPlans.Count; i++)
			{
				SearchPlanDetail plan = planMan.FoodPlans[i];

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
					planMan.FoodPlans.RemoveAt(i);

					// continue the plan, if it was not finished before!
					if (plan.IsPlanForGoal() == false && areWeInHurry == false)
					{
						var newPlan = AntAStarMain.GetFindPathPlan(
							_state,
							_aStar,
							plan.Ant,
							plan.Goal,
							foodTurnThreshold);

						if (newPlan != null)
							// new plan to food!
							planMan.FoodPlans.Add(newPlan);
					}
				}
				else if (state == IssuedOrderState.Failed)
				{
					planMan.FoodPlans.RemoveAt(i);
				}
				else if (state == IssuedOrderState.Blocked)
				{
					planMan.FoodPlans.RemoveAt(i);

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
						//        foodTurnThreshold);

						//    if (newPlan != null)
						//        // new plan to food!
						//        planMan.FoodPlans.Add(newPlan);
						//}
					}
				}
			}

			// ------------------------------------------------
			// new borns
			for (int index = planMan.NewBornPlans.Count - 1; index >= 0; index--)
			{
				SearchPlanDetail plan = planMan.NewBornPlans[index];

				var state = IssueOrderPlanDetail(plan, true);

				// is plan failed?
				if (state == IssuedOrderState.PlanFinished ||
					state == IssuedOrderState.Failed
					//state == IssuedOrderState.Blocked
					)
				{
					planMan.NewBornPlans.RemoveAt(index);
				}
				else if (state == IssuedOrderState.Blocked)
				{
					planMan.NewBornPlans.RemoveAt(index);

					//if (plan.Ant != null && plan.Goal != null)
					//{
					//    // try to find a new plan for it!
					//    var newPlan = AntAStarMain.GetFindPathPlan(
					//        _state,
					//        _aStar,
					//        plan.Ant,
					//        plan.Goal,
					//        foodTurnThreshold);

					//    if (newPlan != null)
					//        // new plan to borned!
					//        planMan.NewBornPlans.Add(newPlan);
					//}

				}
			}

			// ------------------------------------------------
			// map explores
			for (int index = planMan.ExplorePlans.Count - 1; index >= 0; index--)
			{
				SearchPlanDetail plan = planMan.ExplorePlans[index];

				var state = IssueOrderPlanDetail(plan, true);

				// is plan failed?
				if (state == IssuedOrderState.PlanFinished ||
					state == IssuedOrderState.Failed ||
					state == IssuedOrderState.Blocked
					)
				{
					planMan.ExplorePlans.RemoveAt(index);
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
				Bot.IssueOrder(plan.Ant, directions[0]);

				// add to orders
				_thisTurnDoneOrders.Add(newLoc);

				// change direction of my ant!
				plan.Ant = plan.Ant.CloneToDirection(directions[0]);

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
				Bot.IssueOrder(plan.Ant, directions[0]);

				// add to orders
				_thisTurnDoneOrders.Add(newLoc);

				// change direction of my ant!
				plan.Ant = plan.Ant.CloneToDirection(directions[0]);

				// done!
				return IssuedOrderState.Success;
			}
			return IssuedOrderState.Failed;
		}

		public void SendNewBornOutOfHill(Location theHill, Location ant, int turnThreshold)
		{
			var hill = _hillsWays.FirstOrDefault(x => x.Key.EqualTo(theHill));
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
						var tempPlan = AntAStarMain.GetFindPathPlan(_state, _aStar, ant, destLoc, turnThreshold);

						if (tempPlan != null)
						{
							// goes to plans!
							planMan.NewBornPlans.Add(tempPlan);


							antIssued = true;
							break;
						}
					}
				}
				if (antIssued == false && destLoc != null)
				{
					var rndMove = RandomMove(ant);
					if (rndMove != null)
					{
						// just get out of it!
						planMan.NewBornPlans.Add(new SearchPlanDetail()
													{
														Ant = ant,
														Goal = rndMove,
														LeftSteps = 0
													});
					}
				}
			}
		}

		static Location CloseAPointAgainAnotherDiff(Location toMirror, Location other)
		{
			int x = Math.Abs(toMirror.ColX - other.ColX);
			int y = Math.Abs(toMirror.RowY - other.RowY);

			if (toMirror.ColX > other.ColX)
			{
				x = x;
			}
			else if (toMirror.ColX < other.ColX)
			{
				x = -x;
			}
			else
			{
				x = 0;
			}

			if (toMirror.RowY > other.RowY)
			{
				y = y;
			}
			else if (toMirror.RowY < other.RowY)
			{
				y = -y;
			}
			else
			{
				y = 0;
			}
			return new Location(y, x);
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
				x = x;
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
				y = y;
			}
			else
			{
				y = 0;
			}
			return new Location(y, x);
		}

		static Location MirrorAPointAgainAnother(Location toMirror, Location other)
		{
			int x = Math.Abs(toMirror.ColX - other.ColX);
			int y = Math.Abs(toMirror.RowY - other.RowY);

			if (toMirror.ColX > other.ColX)
			{
				x = other.ColX - x;
			}
			else if (toMirror.ColX < other.ColX)
			{
				x = other.ColX + x;
			}
			else
			{
				x = other.ColX;
			}

			if (toMirror.RowY > other.RowY)
			{
				y = other.RowY - y;
			}
			else if (toMirror.RowY < other.RowY)
			{
				y = other.RowY + y;
			}
			else
			{
				y = toMirror.RowY;
			}
			return new Location(y, x);
		}

		Location FarDistanceToLocation(Location srcLoc, Location targetLoc)
		{
			var neighborNodes = new Location[4];
			Common.GetLocationNeighborsMirrored(_state, neighborNodes, srcLoc);
			//neighborNodes[0] = (new Location(srcLoc.RowY, srcLoc.ColX - 1));
			//neighborNodes[1] = (new Location(srcLoc.RowY, srcLoc.ColX + 1));
			//neighborNodes[2] = (new Location(srcLoc.RowY + 1, srcLoc.ColX));
			//neighborNodes[3] = (new Location(srcLoc.RowY - 1, srcLoc.ColX));

			var antsWeight = new AntWeight[4];

			for (int i = 0; i < neighborNodes.Length; i++)
			{
				var nNode = neighborNodes[i];
				antsWeight[i].Ant = nNode;
				if (_state.GetIsPassableSafe(nNode))
				{
					antsWeight[i].Weight = _state.GetDistance(nNode.RowY, nNode.ColX, targetLoc.RowY, targetLoc.ColX);
				}
				else
				{
					antsWeight[i].Weight = -1;
				}
			}

			//var farLocations = antsWeight.OrderByDescending(x => x.Weight);
			var farLocation = antsWeight.MaxValue(x => x.Weight);
			var sourceDistance = _state.GetDistance(srcLoc, targetLoc);
			if (farLocation.Ant != null)
			{
				if (sourceDistance > farLocation.Weight)
				{
					// if it is far enought for attack!
					if (farLocation.Weight > (_state.AttackRadius2 + 2) / 2)
					{
						// it is ok!
						return farLocation.Ant;
					}
					else
					{
						// oh no! don't go closer!!!
						return null;
					}
				}

				return farLocation.Ant;
			}

			// will never reach here!
			// default!
			return null;
		}

		private Location RandomMove(Location ant)
		{
			var inNeighbors = new Location[4];
			var count = 0;
			Common.GetLocationNeighborsMirrored(_state, inNeighbors, ant);

			while (count < 10)
			{
				var nextMove = _random.Next(0, 3);
				var newLoc = inNeighbors[nextMove];

				if (_state.GetIsUnoccupied(newLoc))
				{
					return newLoc;
				}

				count++;
			}
			return null;
		}

		private void IssueOrderToLocation(Location ant, Location target)
		{
			if (ant == null || target == null)
			{
				return;
			}

			// check if this order is already done?
			if (_thisTurnDoneOrders.Exists(x => x.EqualTo(target)))
			//if (_thisTurnDoneOrders.Contains(target))
			{
				// not this dude!
				return;
			}
			var directions = _state.GetDirections(ant, target);
			IssueOrderToDirection(ant, directions);
		}

		private void IssueOrderToDirection(Location ant, ICollection<Direction> directions)
		{
			// try all the directions
			foreach (Direction direction in directions)
			{
				// GetDestination will wrap around the map properly
				// and give us a new location
				Location newLoc = _state.GetDestination(ant, direction);

				// check if this order is already done?
				if (_thisTurnDoneOrders.Exists(x => x.EqualTo(newLoc)))
				//if (_thisTurnDoneOrders.Contains(newLoc))
				{
					// not this dude!
					continue;
				}

				// GetIsPassable returns true if the location is land
				if (_state.GetIsUnoccupied(newLoc))
				{
					Bot.IssueOrder(ant, direction);

					// add to done orders!
					_thisTurnDoneOrders.Add(newLoc);

					// stop now, don't give 1 and multiple orders
					break;
				}
			}
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
						Bot.IssueOrder(searchPlanDetail.Ant, directions[0]);

						// add to orders
						_thisTurnDoneOrders.Add(newLoc);

						// change direction of my ant!
						searchPlanDetail.Ant = searchPlanDetail.Ant.CloneToDirection(directions[0]);

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
						Bot.IssueOrder(searchPlanDetail.Ant, directions[0]);

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
					Bot.IssueOrder(searchPlanDetail.Ant, directions[0]);

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
	}
}
