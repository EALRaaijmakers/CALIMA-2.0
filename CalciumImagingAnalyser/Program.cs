//
//  Program.cs
//
//  Author:
//       F.D.W. Radstake <>
//
//  Copyright (c) 2017 2017, Eindhoven University of Technology
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace CalciumImagingAnalyser
{
	public class Program
	{

		public static Settings settings;

		[STAThread]
		public static void Main (string[] args)
		{
			//first load settings
			//settings = new Settings("settings.xml");
			settings = new Settings ("CalciumImagingAnalyser.exe.config");
			Application.Run (new MainWindow ());
			string tmpdir = Directory.GetCurrentDirectory () + Path.DirectorySeparatorChar + "tmp";
			if (Directory.Exists (tmpdir)) {
				Directory.Delete (tmpdir, true);
			}
			settings.Save ();
		}
	}
}
