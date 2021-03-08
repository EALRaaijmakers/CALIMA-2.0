//
//  MessagePanel.cs
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
using System.Windows.Forms;
using System.Drawing;

namespace CalciumImagingAnalyser
{
	public class MessagePanel:GroupBox
	{

		ProgressBar progressBar;
		TextBox messageBox;
		MainWindow mainWindow;
		SettingsPanel settingsPanel;

		/// <summary>
		/// Initializes a new instance of the <see cref="CalciumImagingAnalyser.MessagePanel"/> class.
		/// </summary>
		/// <param name="mainWindow">Main window.</param>
		/// <param name="settingsPanel">Settings panel.</param>
		public MessagePanel (MainWindow mainWindow,SettingsPanel settingsPanel)
		{
			this.mainWindow = mainWindow;
			this.settingsPanel = settingsPanel;
			Text = "Info";

			progressBar = new ProgressBar ();
			progressBar.Style = ProgressBarStyle.Continuous;
			progressBar.ForeColor = Color.Green;
			Controls.Add (progressBar);

			messageBox = new TextBox ();
			messageBox.Multiline = true;
			messageBox.ScrollBars = ScrollBars.Vertical;
			Controls.Add (messageBox);

			//now set the size and position of all controls
			Resize ();
		}

		/// <summary>
		/// Resizes this instance.
		/// </summary>
		public new void Resize () {
			Rectangle screenRectangle = mainWindow.DisplayRectangle;
			Left = settingsPanel.Right;
			Width = screenRectangle.Width - settingsPanel.Width;
			Height = settingsPanel.Height;

			Rectangle windowRectangle = this.DisplayRectangle;

			progressBar.Height = 16;
			progressBar.Top = windowRectangle.Top;
			progressBar.Left = windowRectangle.Left;
			progressBar.Width = windowRectangle.Width;

			messageBox.Height = windowRectangle.Height - progressBar.Height;
			messageBox.Left = progressBar.Left;
			messageBox.Top = progressBar.Bottom;
			messageBox.Width = windowRectangle.Width;


		}

		public void ShowMessage(string message, bool newLine = true){
			if (newLine) {
				message += "\n";
			}
			messageBox.AppendText (message);
			messageBox.Invalidate();
			messageBox.Update();
			messageBox.Refresh();
			Application.DoEvents();
		}
		public void SetProgressBar(float progress){
			progressBar.Value = (int)(100f * progress);
			progressBar.Invalidate();
			progressBar.Update();
			progressBar.Refresh();
			Application.DoEvents();
		}
	}
}

