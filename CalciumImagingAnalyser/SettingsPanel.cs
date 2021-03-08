//
//  SettingsPanel.cs
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CalciumImagingAnalyser
{
	public class SettingsPanel:GroupBox
	{
		MainWindow mainWindow;
		public CheckBox addCustomCellButton { get; private set; }

		public int Verbosity { get { return (int)verbosityVal.Value; } }

		NumericUpDown validationX,validationY,validationW,validationH;

		public Rectangle validationRectangle {
			get {
				return new Rectangle ((int)validationX.Value, (int)validationY.Value, (int)validationW.Value, (int)validationH.Value);
			}
		}

		public int ThreadCount {
			get {
				return (int)threadsCount.Value;
			}
		}

		Button loadButton,doValidateButton,detectButton,tutorialButton,detectOverlayButton;
		GroupBox filterParams;
		NumericUpDown filterB, filterC, filterCap, threadsCount, verbosityVal,manualROISize;
		GroupBox validationParams, detectionParams, actionParams;
		Label filterRectLabel, filterParLabel,imageResizeLabel,manualROISizeLabel;
		Label ly,lw,lh,lt,ld, lv;
		ComboBox convolutionType, filterPreset,imageResize;
		TextBox ffmpegCommand;
		CheckBox filterUseAverage;

		public SettingsPanel (MainWindow mainWindow)
		{
			this.mainWindow = mainWindow;

			imageResize = new ComboBox ();
			imageResize.Items.Add ("Don't resize");
			imageResize.Items.Add ("1.33x");
			imageResize.Items.Add ("1.66x");
			imageResize.Items.Add ("2x");
			imageResize.Items.Add ("4x");
			imageResize.Items.Add ("8x");
			imageResize.SelectedIndex = 0;
			imageResize.SelectedIndexChanged += delegate {
				switch (imageResize.SelectedIndex) {
				case 0:
					ffmpegCommand.Text = "-i \"filename\" output";
					break;
				case 1:
					ffmpegCommand.Text = "-i \"filename\" -vf \"scale=iw*.75:ih*.75\" output";
					break;
				case 2:
					ffmpegCommand.Text = "-i \"filename\" -vf \"scale=iw*.6:ih*.6\" output";
					break;
				case 3:
					ffmpegCommand.Text = "-i \"filename\" -vf \"scale=iw*.5:ih*.5\" output";
					break;
				case 4:
					ffmpegCommand.Text = "-i \"filename\" -vf \"scale=iw*.25:ih*.25\" output";
					break;
				case 5:
					ffmpegCommand.Text = "-i \"filename\" -vf \"scale=iw*.125:ih*.125\" output";
					break;
				}
			};
			Controls.Add (imageResize);

			imageResizeLabel = new Label ();
			imageResizeLabel.Text = "Downscale image: ";
			Controls.Add (imageResizeLabel);

			loadButton = new Button ();
			loadButton.Text = "Load File";
			loadButton.Click += delegate {
				mainWindow.imageMatrixCollection.LoadFile (ffmpegCommand.Text);
			};
			Controls.Add (loadButton);


			filterParams = new GroupBox ();
			filterParams.Text = "Filter Parameters";
			Controls.Add (filterParams);

			filterParLabel = new Label ();
			filterParLabel.Text = "Preset:";
			filterParams.Controls.Add (filterParLabel);

			filterPreset = new ComboBox ();
			filterPreset.Items.Add (new FilterPreset("4x zoom",2,1,2.5m,.001m));
			filterPreset.Items.Add (new FilterPreset("10x zoom",4,1,2.5m,.001m));
			filterPreset.Items.Add (new FilterPreset("20x zoom",8,1,4m,.001m));
			filterPreset.SelectedIndex = 1;
			filterPreset.SelectedIndexChanged += delegate {
				FilterPreset selected = (FilterPreset)filterPreset.SelectedItem;
				//filterA.Value = selected.A;
				filterB.Value = selected.B;
				filterC.Value = selected.C;
				filterCap.Value = selected.Cap;
			};
			filterParams.Controls.Add (filterPreset);

			filterUseAverage = new CheckBox ();
			filterUseAverage.Checked = true;
			filterUseAverage.Text = "Use frame avg.";
			filterParams.Controls.Add (filterUseAverage);

			/*
			filterA = new NumericUpDown ();
			filterA.DecimalPlaces = 1;
			filterA.Minimum = 1;
			filterA.Increment = .5m;
			filterA.Value = 4;
			filterParams.Controls.Add (filterA); */

			filterB = new NumericUpDown ();
			filterB.DecimalPlaces = 1;
			filterB.Minimum = 1;
			filterB.Increment = .5m;
			filterB.Value = 1;
			filterParams.Controls.Add (filterB);

			filterC = new NumericUpDown ();
			filterC.DecimalPlaces = 1;
			filterC.Minimum = 1;
			filterC.Increment = .5m;
			filterC.Value = 2.5m;
			filterParams.Controls.Add (filterC);

			filterCap = new NumericUpDown ();
			filterCap.DecimalPlaces = 4;
			filterCap.Minimum = 0;
			filterCap.Maximum = 1;
			filterCap.Increment = .0005m;
			filterCap.Value = .001m;
			filterParams.Controls.Add (filterCap);


			validationParams = new GroupBox ();
			validationParams.Text = "Validation";
			Controls.Add (validationParams);

			filterRectLabel = new Label ();
			filterRectLabel.Text = "Validation area: x:";
			validationParams.Controls.Add (filterRectLabel);

			validationX = new NumericUpDown ();
			validationX.Minimum = 0;
			validationX.Maximum = 1000;
			validationX.Value = 256;
			validationParams.Controls.Add (validationX);

			ly = new Label ();
			ly.Text = "y:";
			validationParams.Controls.Add (ly);

			validationY = new NumericUpDown ();
			validationY.Minimum = 0;
			validationY.Maximum = 1000;
			validationY.Value = 256;
			validationParams.Controls.Add (validationY);

			lw = new Label ();
			lw.Text = "   width:";
			validationParams.Controls.Add (lw);

			validationW = new NumericUpDown ();
			validationW.Minimum = 25;
			validationW.Maximum = 500;
			validationW.Value = 128;
			validationParams.Controls.Add (validationW);

			lh = new Label ();
			lh.Text = "height:";
			validationParams.Controls.Add (lh);

			validationH = new NumericUpDown ();
			validationH.Minimum = 25;
			validationH.Maximum = 500;
			validationH.Value = 128;
			validationParams.Controls.Add (validationH);

			doValidateButton = new Button ();
			doValidateButton.Text = "Validate cells";
			doValidateButton.Click += delegate {
				mainWindow.imageMatrixCollection.DoValidation(validationRectangle);
			};
			validationParams.Controls.Add (doValidateButton);


			detectionParams = new GroupBox ();
			detectionParams.Text = "Misc. settings";
			Controls.Add (detectionParams);

			lt = new Label ();
			lt.Text = "Nr. of threads:";
			detectionParams.Controls.Add (lt);

			threadsCount = new NumericUpDown ();
			threadsCount.Minimum = 0;
			threadsCount.Maximum = 32;
			detectionParams.Controls.Add (threadsCount);

			lv = new Label ();
			lv.Text = "Verbosity:";
			detectionParams.Controls.Add (lv);

			verbosityVal = new NumericUpDown ();
			verbosityVal.Minimum = 0;
			verbosityVal.Maximum = 2;
			verbosityVal.Value = 0;
			detectionParams.Controls.Add (verbosityVal);

			ld = new Label ();
			//ld.Text = "Detection type:";
			ld.Text = "Ffmpeg args:";
			detectionParams.Controls.Add (ld);

			ffmpegCommand = new TextBox ();
			ffmpegCommand.Text = "-i \"filename\" output";
			ffmpegCommand.Multiline = false;
			detectionParams.Controls.Add (ffmpegCommand);

			convolutionType = new ComboBox ();
			convolutionType.Items.Add (ConvolutionType.Threaded);
			convolutionType.Items.Add (ConvolutionType.Unthreaded);
			convolutionType.Items.Add (ConvolutionType.Benchmark);
			convolutionType.SelectedItem = ConvolutionType.Threaded;
			//detectionParams.Controls.Add (convolutionType);

			actionParams = new GroupBox ();
			actionParams.Text = "Actions";
			Controls.Add (actionParams);

			addCustomCellButton = new CheckBox ();
			addCustomCellButton.Text = "Manually add ROIs";
			addCustomCellButton.Appearance = Appearance.Button;
			addCustomCellButton.FlatStyle = FlatStyle.System;
			addCustomCellButton.TextAlign = ContentAlignment.MiddleCenter;
			actionParams.Controls.Add (addCustomCellButton);

			manualROISizeLabel = new Label ();
			manualROISizeLabel.Text = "Size:";
			actionParams.Controls.Add (manualROISizeLabel);

			manualROISize = new NumericUpDown ();
			manualROISize.DecimalPlaces = 0;
			manualROISize.Minimum = 2;
			manualROISize.Maximum = 100;
			manualROISize.Increment = 2m;
			manualROISize.Value = 16m;
			actionParams.Controls.Add (manualROISize);

			detectButton = new Button ();
			detectButton.Text = "Detect ROIs";
			detectButton.Click += delegate {
				mainWindow.imageMatrixCollection.DetectSingleFrame ((ConvolutionType)convolutionType.SelectedItem,
					(float)filterB.Value,(float)filterC.Value,(float)filterCap.Value,
					(int)threadsCount.Value, filterUseAverage.Checked);
			};
			actionParams.Controls.Add (detectButton);

			OpenFileDialog openImageDialog = new OpenFileDialog ();
			openImageDialog.DefaultExt = "png";
			openImageDialog.AddExtension = true;
			openImageDialog.Filter = "PNG Image|*.png";
			openImageDialog.InitialDirectory = Directory.GetCurrentDirectory ();

			detectOverlayButton = new Button ();
			detectOverlayButton.Text = "Load image overlay";
			detectOverlayButton.Click += delegate {
				DialogResult result = openImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = openImageDialog.FileName;
					try {
						if (mainWindow.imageMatrixCollection.images.Count == 0) {
							MainWindow.ShowMessage ("Error: Please load calcium imaging footage before loading an overlay.");
							return;
						}
						MainWindow.ShowMessage ("Loading cell overlay " + filename + "...");
						Bitmap overlay = new Bitmap (filename);
						if (mainWindow.imageMatrixCollection.images [0].width != overlay.Width || mainWindow.imageMatrixCollection.images [0].height != overlay.Height) {
							MainWindow.ShowMessage ("Warning: Overlay is not the same size as the calcium imaging footage. It should be " + 
							mainWindow.imageMatrixCollection.images [0].width.ToString() + "x" + mainWindow.imageMatrixCollection.images [0].height.ToString() +
								" pixels. Please change the size of the overlay using external tools.");
							if (MessageBox.Show ("Warning: Overlay is not the same size as the calcium imaging footage. It should be " +
								mainWindow.imageMatrixCollection.images [0].width.ToString () + "x" + mainWindow.imageMatrixCollection.images [0].height.ToString () +
								" pixels. It is recommended to change the size of the overlay using external tools.\nLoading this overlay may have unexpected results. Do you want to continue?", "Warning", MessageBoxButtons.YesNo,MessageBoxIcon.Warning) != DialogResult.Yes) {
								MainWindow.ShowMessage ("Loading overlay aborted.");
								return;
							}
						}

						mainWindow.imageMatrixCollection.DetectSingleFrame (new Bitmap(filename), (int)threadsCount.Value);
					} catch (IOException e) {
						MainWindow.ShowMessage("Error loading file: " + e.Message);
					}
				}
			};
			actionParams.Controls.Add (detectOverlayButton);

			tutorialButton = new Button ();
			//tutorialButton.Text = "Detection sweep";
			tutorialButton.Text = "Help";
			tutorialButton.Click += delegate {
				//mainWindow.imageMatrixCollection.DetectAllFrames ((ConvolutionType)convolutionType.SelectedItem,
				//	(float)filterA.Value,(float)filterB.Value,(float)filterC.Value,(float)filterCap.Value,
				//	(int)threadsCount.Value);
				mainWindow.ShowHelp ();
			};
			actionParams.Controls.Add (tutorialButton);

			// Set up the ToolTip text for the various controls.
			mainWindow.ToolTip.SetToolTip(convolutionType, "Set the convolution operation.\nSeperable is much faster with a complexity of O(2n), but might not work for some filter types.\nUnseperable has complexity O(n^2), and always works.\nUnless you've implemented your own filter, this should be kept at Seperable.");
			//mainWindow.ToolTip.SetToolTip(filterA, "Standard deviation of the Gaussian blurring filter. Increasing the value filters out the smallest cells.\nDecreasing it will detect more and smaller cells, but also has much more false positives.\nWarning: large values take long to compute!"); //blurring filter obsolete
			mainWindow.ToolTip.SetToolTip(filterB, "Standard deviation of the first Gaussian filter for the DOG filtering.\nTogether with the third parameter, controls how steep a lightness slope is needed to detect a cell.\nIncrease to filter out false positives. Warning: large values take long to compute!");
			mainWindow.ToolTip.SetToolTip(filterC, "Standard deviation of the second Gaussian filter for the DOG filtering. Together with the second parameter, controls how steep a lightness slope is needed to detect a cell.\nIncrease to make the detected areas larger. Lower this if adjacent cells get merged.\nWarning: large values take long to compute!");
			mainWindow.ToolTip.SetToolTip (filterCap, "Only detect ROIs with a value above this one. Keeps only the clearest, brightest cells. Can be used to remove false positives.");
			mainWindow.ToolTip.SetToolTip (threadsCount, "The number of threads used by the application.\nSet to 0 for automatic (recommended). On some (old) systems, this might cause problems. If so, set the value manually.\nIncreasing it speeds up the algorithm, though with a larger chance of problems or clogging up the processor.");
			mainWindow.ToolTip.SetToolTip (detectButton, "Run the ROI detection algorithm on the selected frame.");
			mainWindow.ToolTip.SetToolTip (detectOverlayButton, "Load a black-and-white image of detected ROIs.");
			mainWindow.ToolTip.SetToolTip (tutorialButton, "Show the tutorial.");
			mainWindow.ToolTip.SetToolTip (verbosityVal, "Determines how much info appears in the message window on the right.\nA higher value means more info.");
			mainWindow.ToolTip.SetToolTip (ffmpegCommand, "The ffmpeg command used to convert a video into an image sequence.\n\"filename\" gets changed into the chosen filename, and \"output\" will be the\noutput filename format as chosen by CalciumImagingAnalyser.");
			mainWindow.ToolTip.SetToolTip (filterUseAverage, "Check to run the cell detection algorithm on the average values across all frames, instead of on the currently shown frame.\nThis might improve results, at the cost of a longer calculation time.");
			mainWindow.ToolTip.SetToolTip (addCustomCellButton, "Manually add ROIs. Use this if the automated ROI detection does not detect some ROIs.");
			mainWindow.ToolTip.SetToolTip (manualROISize, "Diameter in pixels of the manually added ROIs.");
			mainWindow.ToolTip.SetToolTip (imageResize, "Downscaling the image reduces memory usage and shortens the execution time of the cell detection algorithm.\nIf you have any issues caused by large memory usage, such as crashes or slowdowns, downscaling should help.\nDownscaling can also improve the cell detection quality by reducing noise, although too much downscaling can reduce the quality.\nSet this BEFORE loading a video file (not used for image sequences).");
			mainWindow.ToolTip.SetToolTip (loadButton, "Load calcium imaging footage. If the video type isn't listed, please set \"Files of type\" to \"All files\" since\n" +
				"ffmpeg supports a wide range of video formats, not all of which are shown here.");

			//do the positioning
			Resize ();
		}

		public new void Resize () {
			Rectangle screenRectangle = mainWindow.DisplayRectangle;
			Left = screenRectangle.Left;
			//Width = screenRectangle.Width/2;
			Width = 1024 / 2;
			Text = "Settings";

			Rectangle windowRectangle = this.DisplayRectangle;

			imageResizeLabel.Left = windowRectangle.Left;
			imageResizeLabel.Width = TextRenderer.MeasureText (imageResizeLabel.Text, Utils.font).Width;
			imageResizeLabel.Height = loadButton.Height;
			imageResizeLabel.Top = windowRectangle.Top;

			imageResize.Left = imageResizeLabel.Right;
			imageResize.Width = windowRectangle.Width / 2 - imageResizeLabel.Width;
			imageResize.Top = imageResizeLabel.Top;
			imageResize.Height = loadButton.Height;

			loadButton.Left = imageResize.Right;
			loadButton.Top = windowRectangle.Top;
			loadButton.Width = windowRectangle.Width / 2;

			filterParams.Left = windowRectangle.Left;
			filterParams.Width = windowRectangle.Width/2;
			filterParams.Top = loadButton.Bottom;

			filterParLabel.Left = filterParams.DisplayRectangle.Left;
			filterParLabel.Width = TextRenderer.MeasureText (filterParLabel.Text, Utils.font).Width;
			filterParLabel.Height = filterPreset.Height;
			filterParLabel.Top = filterParams.DisplayRectangle.Top;

			filterPreset.Left = filterParLabel.Right;
			filterPreset.Top = filterParams.DisplayRectangle.Top;
			filterPreset.Width = 80;

			filterUseAverage.Left = filterPreset.Left + filterPreset.Width;
			filterUseAverage.Top = filterParams.DisplayRectangle.Top;
			filterUseAverage.Width = filterParams.DisplayRectangle.Width - filterUseAverage.Left;
			filterUseAverage.Height = filterPreset.Height;

			//filterA obsolete
			//filterA.Width = 48;
			//filterA.Left = filterParams.DisplayRectangle.Left;
			//filterA.Top = filterPreset.Bottom;

			filterB.Width = 48;
			filterB.Left = filterParams.DisplayRectangle.Left;//filterA.Right;
			filterB.Top = filterPreset.Bottom; //filterA.Top;

			filterC.Width = 48;
			filterC.Left = filterB.Right;
			filterC.Top = filterB.Top;

			filterCap.Width = 64;
			filterCap.Left = filterC.Right;
			filterCap.Top = filterC.Top;


			filterParams.Height = filterPreset.Height + filterB.Height + filterParams.Height - filterParams.DisplayRectangle.Height;

			validationParams.Top = filterParams.Bottom;
			validationParams.Left = filterParams.Left;
			validationParams.Width = filterParams.Width;

			filterRectLabel.Left = validationParams.DisplayRectangle.Left;
			filterRectLabel.Width = TextRenderer.MeasureText (filterRectLabel.Text, Utils.font).Width;
			filterRectLabel.Height = validationX.Height;
			filterRectLabel.Top = validationParams.DisplayRectangle.Top;

			validationX.Left = filterRectLabel.Right;
			validationX.Top = filterRectLabel.Top;
			validationX.Width = 55;

			ly.Left = validationX.Right;
			ly.Width = TextRenderer.MeasureText (ly.Text, Utils.font).Width;
			ly.Height = validationY.Height;
			ly.Top = validationX.Top;

			validationY.Left = ly.Right;
			validationY.Top = filterRectLabel.Top;
			validationY.Width = 55;

			lw.Left = filterRectLabel.Left;
			lw.Width = TextRenderer.MeasureText (lw.Text, Utils.font).Width;
			lw.Height = validationW.Height;
			lw.Top = validationX.Bottom;

			validationW.Left = lw.Right;
			validationW.Top = lw.Top;
			validationW.Width = 55;

			lh.Left = validationW.Right;
			lh.Width = TextRenderer.MeasureText (lh.Text, Utils.font).Width;
			lh.Height = validationH.Height;
			lh.Top = validationW.Top;

			validationH.Left = lh.Right;
			validationH.Top = lh.Top;
			validationH.Width = 55;


			doValidateButton.Left = validationParams.DisplayRectangle.Left;
			doValidateButton.Top = validationW.Bottom;
			doValidateButton.Width = validationParams.DisplayRectangle.Width;

			detectionParams.Top = filterParams.Top;
			detectionParams.Left = filterParams.Right;
			detectionParams.Width = windowRectangle.Width - filterParams.Width;

			lt.Top = detectionParams.DisplayRectangle.Top;
			lt.Width = TextRenderer.MeasureText (lt.Text, Utils.font).Width;
			lt.Height = threadsCount.Height;
			lt.Left = detectionParams.DisplayRectangle.Left;

			threadsCount.Left = lt.Right;
			threadsCount.Top = lt.Top;
			threadsCount.Width = 40;

			lv.Top = lt.Top;
			lv.Width = TextRenderer.MeasureText (lv.Text, Utils.font).Width;
			lv.Height = verbosityVal.Height;
			lv.Left = threadsCount.Right;

			verbosityVal.Left = lv.Right;
			verbosityVal.Top = lv.Top;
			verbosityVal.Width = 40;

			ld.Height = threadsCount.Height;
			ld.Top = threadsCount.Bottom;
			ld.Width = TextRenderer.MeasureText (ld.Text, Utils.font).Width;
			ld.Left = detectionParams.DisplayRectangle.Left;

			//ffmpegCommand.MaximumSize = new Size (100000, 10);
			ffmpegCommand.Left = ld.Right;
			ffmpegCommand.Top = threadsCount.Bottom;
			ffmpegCommand.Width = detectionParams.DisplayRectangle.Width - ld.Width;
			ffmpegCommand.Height = ld.Height;
			//ffmpegCommand.AutoSize = false;
			//ffmpegCommand.Size = new Size (detectionParams.DisplayRectangle.Width - ld.Width, 10);

			convolutionType.Left = ld.Right;
			convolutionType.Top = threadsCount.Bottom;

			detectionParams.Height = threadsCount.Height + convolutionType.Height + detectionParams.Height - detectionParams.DisplayRectangle.Height;

			actionParams.Left = detectionParams.Left;
			actionParams.Width = detectionParams.Width;
			actionParams.Top = detectionParams.Bottom;

			detectButton.Left = actionParams.DisplayRectangle.Left;
			detectButton.Top = actionParams.DisplayRectangle.Top;
			detectButton.Width = actionParams.DisplayRectangle.Width;

			addCustomCellButton.Left = actionParams.DisplayRectangle.Left;
			addCustomCellButton.Top = detectButton.Bottom;
			addCustomCellButton.Width = actionParams.DisplayRectangle.Width / 2;

			manualROISizeLabel.Left = addCustomCellButton.Right;
			manualROISizeLabel.Top = detectButton.Bottom;
			manualROISizeLabel.Width = TextRenderer.MeasureText (manualROISizeLabel.Text, Utils.font).Width;
			manualROISizeLabel.Height = addCustomCellButton.Height;

			manualROISize.Left = manualROISizeLabel.Right;
			manualROISize.Top = detectButton.Bottom;
			manualROISize.Width = actionParams.DisplayRectangle.Width - manualROISizeLabel.Right;
			manualROISize.Height = addCustomCellButton.Height;



			detectOverlayButton.Left = actionParams.DisplayRectangle.Left;
			detectOverlayButton.Top = addCustomCellButton.Bottom;
			detectOverlayButton.Width = actionParams.DisplayRectangle.Width / 2;

			tutorialButton.Left = detectOverlayButton.Right;
			tutorialButton.Top = addCustomCellButton.Bottom;
			tutorialButton.Width = actionParams.DisplayRectangle.Width-detectOverlayButton.Width;
			tutorialButton.Height = detectOverlayButton.Height;


			actionParams.Height = addCustomCellButton.Height + detectButton.Height + detectOverlayButton.Height + actionParams.Height - actionParams.DisplayRectangle.Height;

			int outerHeight = Height - this.DisplayRectangle.Height;
			validationParams.Height = addCustomCellButton.Height + validationH.Height + validationX.Height + validationParams.Height - validationParams.DisplayRectangle.Height;
			Height = Math.Max(actionParams.Bottom,validationParams.Bottom) - loadButton.Top + outerHeight;
		}

		public void SetTutorialControls (ref Control[] buttons, ref List<Control>[] controls) {
			buttons [1] = loadButton;
			buttons [2] = detectButton;
			controls [0].Add (tutorialButton);
			controls [1] = new List<Control> ();
			controls [1].Add (loadButton);
			controls [2] = new List<Control> ();
			controls [2].Add (detectButton);
			controls [2].Add (filterParams);
			controls [2].Add (addCustomCellButton);

		}

		public int ManualROISize {
			get {
				return (int)manualROISize.Value;
			}
		}
	}
}

