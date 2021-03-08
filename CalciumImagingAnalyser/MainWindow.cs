//
//  MainWindow.cs
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
	public class MainWindow:Form
	{
		AnalysisWindow analysisWindow;

		ImagePanel imagePanel;
		public AnalysisPanel analysisPanel;
		public static SettingsPanel settingsPanel;
		static MessagePanel messageWindow;
		public static int Verbosity { get { return settingsPanel.Verbosity; } }
		public ToolTip ToolTip { get; private set; }

		public ImageMatrixCollection imageMatrixCollection { get; private set; }

		public static HelpWindow helpWindow { get; private set; }

		public MainWindow (){
			this.Icon = new Icon("icon_cc0.ico",128,128);
			this.Width = Program.settings.XResolution;
			this.Height = Program.settings.YResolution;
			this.Text = "CalciumImagingAnalyser";
			if (Program.settings.Fullscreen) {
				this.WindowState = FormWindowState.Maximized;
			} else {
				this.WindowState = FormWindowState.Normal;
			}

			//first load the tooltip for use in the various panels
			ToolTip = new ToolTip();
			// Set up the delays for the ToolTip.
			ToolTip.AutoPopDelay = 10000;
			ToolTip.InitialDelay = 500;
			ToolTip.ReshowDelay = 250;
			ToolTip.ToolTipIcon = ToolTipIcon.Info;
			// Force the ToolTip text to be displayed whether or not the form is active.
			ToolTip.ShowAlways = true;


			settingsPanel = new SettingsPanel (this);
			Controls.Add (settingsPanel);

			messageWindow = new MessagePanel (this,settingsPanel);
			Controls.Add (messageWindow);

			imagePanel = new ImagePanel (this, settingsPanel);
			Controls.Add (imagePanel);

			analysisPanel = new AnalysisPanel (this, imagePanel);
			Controls.Add (analysisPanel);

			imageMatrixCollection = new ImageMatrixCollection (imagePanel, analysisPanel, imagePanel.frameTrackBar, this);
			imagePanel.SetImageMatrixCollection (imageMatrixCollection);

			analysisWindow = new AnalysisWindow (this, imageMatrixCollection);

			Resize += delegate {
				settingsPanel.Resize();
				messageWindow.Resize ();
				imagePanel.Resize ();
				analysisPanel.Resize ();
				Program.settings.XResolution = this.Size.Width;
				Program.settings.YResolution = this.Size.Height;
				if (this.WindowState == FormWindowState.Maximized) {
					Program.settings.Fullscreen = true;
				} else {
					Program.settings.Fullscreen = false;
				}
			};

			//show the help only if wanted
			if (Program.settings.ShowHelp) {
				ShowHelp ();
			}
		}


		public void ShowHelp () {
			helpWindow = new HelpWindow (analysisPanel,analysisWindow,imagePanel,settingsPanel);
			helpWindow.Show ();
			helpWindow.BringToFront ();
		}

		public static void ShowMessage(string message, bool newLine = true){
			messageWindow.ShowMessage (message, newLine);
			if (newLine)
				Console.WriteLine (message);
			else
				Console.Write (message);
		}

		public static void SetProgressBar (float progress) {
			messageWindow.SetProgressBar (progress);
		}


		public void SetStageAnalysis () {
			analysisWindow.ShowWindow ();
			this.Hide ();
		}

		public void SetStageDetection () {
			this.Show ();
			analysisWindow.Hide ();
		}

		public void Quit () {
			Application.Exit ();
		}

		public int ThreadCount {
			get {
				return settingsPanel.ThreadCount;
			}
		}
	}
}

