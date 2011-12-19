using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts.Pack;
using SettlersEngine;

namespace SalarAnts.Defination
{
	public class SearchPlanDetail : BasePlan
	{
 		/// <summary>
		/// Goal location
		/// </summary>
		public Location Goal { get; set; }

		/// <summary>
		/// Goal type
		/// </summary>
		public Tile GoalType { get; set; }

		/// <summary>
		/// Temporary found destination
		/// </summary>
		public Location TempDest { get; set; }

		/// <summary>
		/// Temporary found type
		/// </summary>
		public Tile? TempDestType { get; set; }

		/// <summary>
		/// Wight of plan, can be Temp or Goal
		/// </summary>
		public int LeftSteps { get; set; }

        /// <summary>
        /// Number of times blocked this plan
        /// </summary>
        public int Blocked { get; set; }

		public Location GetFinalMoveLoc()
		{
			return TempDest ?? Goal;
		}

		public bool IsPlanForGoal()
		{
			if (TempDest != null)
			{
				if (!TempDest.EqualTo(Goal))
				{
					return false;
				}
			}
			return true;
		}

		public LinkedList<AntAStarPathNode> Path { get; set; }
		public LinkedListNode<AntAStarPathNode> PathCurrent { get; set; }

		public override string ToString()
		{
			return string.Format("Ant: {0}, Goal: {1}, {2}, Dest: {3}, {4}, Steps: {5}", Ant, Goal, GoalType, TempDest, TempDestType, LeftSteps);
		}

#if DEBUG

        public string ToPathString()
        {
            var sb = new StringBuilder();
            var node = PathCurrent;
            while (node != null)
            {
                sb.AppendLine(node.Value.ToString());
                node = node.Next;
            }
            return sb.ToString();
        }

#endif
	}

	public class PlanAntDetailEqualityComparer : IEqualityComparer<SearchPlanDetail>
	{

		public bool Equals(SearchPlanDetail x, SearchPlanDetail y)
		{
			return x.Ant.EqualTo(y.Ant);
		}

		public int GetHashCode(SearchPlanDetail obj)
		{
			return obj.Ant.GetHashCode();
		}
	}

}
