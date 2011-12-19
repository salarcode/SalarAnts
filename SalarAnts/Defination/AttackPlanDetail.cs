using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SalarAnts.Pack;

namespace SalarAnts.Defination
{
	public class AttackPlanDetail : BasePlan
	{
 		/// <summary>
		/// The next step should this ant go
		/// </summary>
		public Location NextStep { get; set; }

		/// <summary>
		/// The ant we support
		/// </summary>
		public Location LeaderAnt { get; set; }

		/// <summary>
		/// the enemy
		/// </summary>
		public Location Enemy { get; set; }
	}
}
