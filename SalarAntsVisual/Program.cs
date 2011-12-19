using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SalarAntsVisual
{
	public static class Program
	{

		public static frmAntVisual FormAntVisual { get; set; }
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			FormAntVisual = new frmAntVisual();
			Application.Run(FormAntVisual);
		}

		public static void RunTheForm(frmAntVisual frm)
		{
			Application.EnableVisualStyles();
			FormAntVisual = frm;
			Application.Run(FormAntVisual);
		}
	}
}
