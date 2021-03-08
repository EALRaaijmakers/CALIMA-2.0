//
//  HelpWindow.cs
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
using System.Collections.Generic;
using System.Drawing;

namespace CalciumImagingAnalyser
{
	public class HelpWindow:Form
	{
		Button nextButton;
		Button previousButton;
		Label textLabel;

		List<Control>[] activeControls;
		Control[] proceedButtons;
		string[] texts;

		int currentStage = -1;
		List<Control> activeControl;
		List<Color> activeControlColors;

		CheckBox showAtStartBox;

		//Plexiglass arrow;

		public HelpWindow (AnalysisPanel ap, AnalysisWindow aw, ImagePanel ip, SettingsPanel sp)
		{
			StartPosition = FormStartPosition.CenterScreen;

			previousButton = new Button ();
			previousButton.Text = "Previous";
			previousButton.Left = 0;
			previousButton.Top = DisplayRectangle.Height - previousButton.Height;
			previousButton.Click += Previous;
			Controls.Add (previousButton);

			nextButton = new Button ();
			nextButton.Text = "Next";
			nextButton.Left = DisplayRectangle.Width - nextButton.Width;
			nextButton.Top = DisplayRectangle.Height - nextButton.Height;
			nextButton.Click += Next;
			Controls.Add (nextButton);

			showAtStartBox = new CheckBox ();
			showAtStartBox.Checked = Program.settings.ShowHelp;
			showAtStartBox.Left = 0;
			showAtStartBox.Top = previousButton.Top - showAtStartBox.Height;
			showAtStartBox.Text = "Show at start";
			Controls.Add (showAtStartBox);
			showAtStartBox.CheckedChanged += delegate {
				Program.settings.ShowHelp = showAtStartBox.Checked;
			};

			textLabel = new Label ();
			textLabel.Width = DisplayRectangle.Width;
			textLabel.Height = DisplayRectangle.Height;
			Controls.Add (textLabel);
			textLabel.SendToBack ();

			activeControls = new List<Control>[9];
			activeControls [0] = new List<Control> ();
			activeControls [0].Add (nextButton);
			activeControls [7] = new List<Control> ();
			activeControls [7].Add (nextButton);


			proceedButtons = new Control[9];
			proceedButtons [0] = null;
			proceedButtons [7] = null;

			sp.SetTutorialControls (ref proceedButtons, ref activeControls);
			ap.SetTutorialControls (ref proceedButtons, ref activeControls);
			aw.SetTutorialControls (ref proceedButtons, ref activeControls);

			texts = new string[9];
			texts [0] = "Hello and welcome to CalciumImagingAnalyser. This software was created to be an easy to use application for the analysis" +
			" of calcium imaging footage.\n\nFor more information about CALIMA and the algorithms used, please read our paper:\nhttps://doi.org/10.1016/j.cmpb.2019.104991\n\nThis tutorial will guide you through the essential steps of loading and analysing the footage. " +
			"Feel free to end this tutorial at any time; you can reopen it from the \"Help\" button at the top left of the window.\n\n" +
				"Click the button labeled \"Next\" at the bottom right of this screen to proceed.";
			texts [1] = "Let's first load a file. CalciumImagingAnalyser can load image sequences natively (png, jpg, tiff, bmp, gif), and various " +
				"video formats using the ffmpeg redistributable included with this program (avi, mp4, wmv, ...)." +
				"Either select a single video file or multiple image files to start the " +
				"corresponding action." +
				"\n\nClick on the button marked \"Load\" to load calcium image footage. You can check the progress in the progressbar on the " +
				"top right.\n\nPress the \"Load File\" button and open a file to continue (this can take a while).";
			texts [2] = "Once your footage has loaded, you can scroll through " +
				"the frames using the slider below the large image. Once you've found a frame in which most cells are clearly visible, you can " +
				"press the \"Detect ROIs\" button. You can also use the average brightness across all frames; for this check the \"Use frame avg.\" " +
				"box. Pressing the \"Detect ROIs\" button will run the cell detection algorithm using the parameters in the \"Filter  Parameters\"" +
				" window on the top left.\n\nYou can hover your mouse over the filter parameters numboxes to get more info on what they do." +
				"\n\nIf cells aren't detected or there are lots of false positives, you can either change the filter parameters, run the " +
				"algorithm on another frame, or add custom cells using the \"Manually add ROIs\" button on the left, after which you can " +
				"add custom cells by clicking the image.\n\nPress the \"Detect ROIs\" button to continue.";
			texts [3] = "You can rerun the cell detection algorithm as often as you want - the new cells will just overwrite the old ones.\n\n" +
				"You can select ROIs by either clicking them in the image or selecting them in the listbox to the right. If you want, you " +
				"can delete the selected cell by pressing the delete button on your keyboard. If the algorithm has missed any cells, you can " +
				"add them using the \"Manually add ROIs\" button as described in the previous screen.\n\nOnce you're satisfied with the detected cells, " +
				"you can press the \"Record cell activity\" button to gather data about the cells. By selecting the corresponding value in the "+
				"box above it, the algorithm will either save the maximum value of all pixels belonging to a cell, or the average." +
				"\n\nPress the \"Record cell activity\" button to continue.";
			texts [4] = "Now we've gathered data on all cells, we can see graphs of their lightness by selecting cells. You can export these " +
				"graphs as a comma-separated file by pressing the \"Export to .csv\" button. By clicking the \"Go to Analysis stage\" button, "+
				"you can proceed to the next stage, in which more advanced functions are possible." + 
				"\n\nPress the \"Go to Analysis stage\" button to continue.";
			texts [5] = "On the Analysis screen you see several graphs. These give information about selected cells, including when pulses " +
				"are detected.\n\nOn the left you can select the minimum number of pulses a cell needs to show up in the graphs. If set " +
				"to 0, all cells are shown, if set to 1 all cells that pulse at least once are shown and so forth.\n\nPlay around with the value " +
				"and notice how the two maps on the right change. The displayed ROIs on the lower right map correspond to the cells in the listbox " +
				"on the left.";
			texts [6] = "To fill the graphs in the center, you first need to select the graphs you want.\n\nSelect one or more cells in the " +
				"listbox on the left";
			texts [7] = "On the left graph, detected pulses are marked red. Depending on how many neurons pulse on a certain frame, all " +
			"graphs are shaded green. The red area to the left shows the frames where spike detection may be less " +
			"accurate, depending on the pulse detection parameters.\n\nFor pulse detection, a z-score algorithm is used. The numbox on the " +
			"left determines the number of frames used for determining the standard deviation; next to it is the minimum z-score needed to " +
			"be considered a pulse; next to that is a measure of how much a pulse influences the standard deviation; finally on the right " +
			"shows the minimum value of the ROC graph to be considered a pulse. Hover over the boxes for more information on what effect a value has.";
			texts [8] = "This concludes the tutorial. If you have any more questions, you can alway contact us using the contact form on https://aethelraed.nl/calima. Also " +
				"check our paper (https://doi.org/10.1016/j.cmpb.2019.104991) for more information about the various algorithms used. " +
				"If you use CALIMA in your research, we would very much appreciate it if you cite our paper.";

			activeControl = new List<Control>();
			activeControlColors = new List<Color>();

			TopMost = true;

			Next (null, null);
		}

		public void Next (Object o, System.EventArgs e) {
			if (currentStage + 1 < activeControls.Length) {
				Clear ();
				currentStage++;
				this.Text = "Tutorial [" + (currentStage+1).ToString () + "/" + texts.Length.ToString () + "]";

				SetText(texts [currentStage]);

				//set the new controls
				if (activeControls != null) {
					activeControl = activeControls [currentStage];
					if (activeControl != null) {
						//save the new colors, and set them to red
						for (int i = 0; i < activeControl.Count; i++) {
							activeControlColors.Add (activeControl [i].BackColor);
							activeControl [i].BackColor = Color.Red;
						}
					}
					if (proceedButtons != null) {
						if (proceedButtons [currentStage] != null) {
							if (proceedButtons [currentStage] is Button) {
								((Button)proceedButtons [currentStage]).Click += Next;
							}
							if (proceedButtons [currentStage] is NumericUpDown) {
								((NumericUpDown)proceedButtons [currentStage]).ValueChanged += Next;
							}
							if (proceedButtons [currentStage] is ListBox) {
								//((ListBox)proceedButtons [currentStage]).SelectedIndexChanged += Next;
								((ListBox)proceedButtons [currentStage]).Click += Next; ;
							}
						}
					}
				}
				textLabel.Text = texts [currentStage];
			}
			if (currentStage == 0) {
				showAtStartBox.Visible = true;
			} else {
				showAtStartBox.Visible = false;
			}
		}

		void SetText (string text) {
			//TextRenderer.MeasureText (listLabel.Text, Utils.font).Width + 4;
			textLabel.Text = text;
			SizeF txtSize = textLabel.CreateGraphics ().MeasureString (text, textLabel.Font, textLabel.Width, new StringFormat (0));
			textLabel.Height = (int)txtSize.Height + 6;//round up + 5 px padding
			nextButton.Top = textLabel.Bottom;
			previousButton.Top = textLabel.Bottom;
			Size cs = this.ClientSize;
			cs.Height = nextButton.Bottom;
			this.ClientSize = cs;
		}

		public void Previous (Object o, System.EventArgs e) {
			if (currentStage > 0) {
				Clear ();
				currentStage -= 2;
				Next (o, e);
			}
		}

		protected override void OnClosing (System.ComponentModel.CancelEventArgs e)
		{
			Clear ();
			base.OnClosing (e);
		}

		void Clear () {
			if (currentStage >= 0) {
				if (proceedButtons != null) {
					if (proceedButtons [currentStage] != null) {
						if (proceedButtons [currentStage] is Button) {
							((Button)proceedButtons [currentStage]).Click -= Next;
						}
						if (proceedButtons [currentStage] is NumericUpDown) {
							((NumericUpDown)proceedButtons [currentStage]).ValueChanged -= Next;
						}
						if (proceedButtons [currentStage] is ListBox) {
							//((ListBox)proceedButtons [currentStage]).SelectedIndexChanged -= Next;
							((ListBox)proceedButtons [currentStage]).Click -= Next;
						}
					}
				}
			}
			//set the colors back to their default values
			if (activeControl != null) {
				for (int i = 0; i < activeControl.Count; i++) {
					activeControl [i].BackColor = activeControlColors [i];
				}
			}
		//	activeControlColors.Clear ();
		//	activeControl.Clear ();
		}
	}
}

