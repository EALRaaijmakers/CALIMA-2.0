//
//  AnalysisWindow.cs
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
using System.IO;

namespace CalciumImagingAnalyser
{
	public class AnalysisWindow:Form
	{
		MainWindow mainWindow;
		ListBox cellsListBox;
		ImageMatrixCollection imageMatrixCollection;

		PictureBox graphsBox, graphSimilarity, mapActivity, mapConnections;

		NumericUpDown pulseThreshold, settingLag, settingTreshold, settingInfluence, connectionDistance,
			connectionDt, connectionTreshold, activityMaxSize, xCorrDelay, frameWidth, frameRate;
		Button detectionStageButton, exportCSVButton, showstatsButton, saveActivityImageButton, saveConnectionImageButton,
			saveCorrelationImageButton, selectAllButton, saveGraphsImageButton, selectSimilarPulsetimeCellsButton;

		GroupBox settingsGroup, connectionGroup, activityGroup, correlationGroup;

		Label graph1Label, graph2Label, listLabel, xCorrDelayLabel, frameWidthLabel, frameRateLabel, frameWidthDescLabel, frameRateDescLabel;

		ComboBox similarityMode;
		ComboBox similaritySource;
		ComboBox detectionMode;
		ComboBox activityMode;
		ComboBox cellDrawMode;

		SaveFileDialog saveCSVDialog;

		CheckBox showPeaks, showConnectionBackground, showActivityBackground, showActivityLabels, showConnectionLabels,
			showActivityLegend;

		List<BrainCell> shownCells, selectedCells;
		ToolTip ToolTip;

		public AnalysisWindow (MainWindow mainWindow, ImageMatrixCollection imageMatrixCollection)
		{
			this.mainWindow = mainWindow;
			this.imageMatrixCollection = imageMatrixCollection;
			this.Location = mainWindow.Location;
			//this.WindowState = mainWindow.WindowState;
			//this.Size = mainWindow.Size;
			this.Width = Program.settings.XResolution;
			this.Height = Program.settings.YResolution;
			this.Text = "CalciumImagingAnalyser";
			if (Program.settings.Fullscreen) {
				this.WindowState = FormWindowState.Maximized;
			} else {
				this.WindowState = FormWindowState.Normal;
			}
			this.Icon = mainWindow.Icon;
			this.Text = "CalciumImagingAnalyser";
			selectedCells = new List<BrainCell> ();


			pulseThreshold = new NumericUpDown ();
			pulseThreshold.Minimum = 0m;
			pulseThreshold.Maximum = 50m;
			pulseThreshold.DecimalPlaces = 0;
			pulseThreshold.Increment = 1m;
			pulseThreshold.Value = 1m;
			pulseThreshold.ValueChanged += delegate {
				ShowGraphs ();
			};
			Controls.Add (pulseThreshold);

			selectAllButton = new Button ();
			selectAllButton.Text = "Select all shown";
			selectAllButton.Click += delegate {
				cellsListBox.SelectedIndexChanged -= CellsSelectedEvent;
				for (int i = 0; i < cellsListBox.Items.Count; i++){
					cellsListBox.SetSelected (i, true);
				}
				cellsListBox.SelectedIndexChanged += CellsSelectedEvent;
				UpdateSelectedCells();
				ShowGraphs ();
			};
			Controls.Add (selectAllButton);

			selectSimilarPulsetimeCellsButton = new Button ();
			selectSimilarPulsetimeCellsButton.Text = "Select simultaneous pulses";
			selectSimilarPulsetimeCellsButton.Click += delegate {
				if (cellsListBox.SelectedItems.Count > 0) {
					int selectedCell = imageMatrixCollection.cellActivityContainer.GetCellIndex((BrainCell)cellsListBox.SelectedItems[0]);

					int[] peakFrames = imageMatrixCollection.cellActivityContainer.GetPeakFrames(selectedCell);

					List<BrainCell> similarCells = new List<BrainCell> ();
					for (int i = 0; i < imageMatrixCollection.allCells.Count; i++) {
						if (imageMatrixCollection.cellActivityContainer.HasOverlap(i,peakFrames)) {
							similarCells.Add(imageMatrixCollection.allCells[i]);
						}
					}

					//disable cell selection
					cellsListBox.SelectedIndexChanged -= CellsSelectedEvent;
					for (int i = 0; i < cellsListBox.Items.Count; i++) {
						if (similarCells.Contains((BrainCell)cellsListBox.Items[i])) {
							cellsListBox.SetSelected(i,true);
						}
					}
					//re-enable cell selection
					cellsListBox.SelectedIndexChanged += CellsSelectedEvent;
					UpdateSelectedCells ();
					ShowGraphs ();
				}
			};
			Controls.Add (selectSimilarPulsetimeCellsButton);
			
			SaveFileDialog saveImageDialog = new SaveFileDialog ();
			saveImageDialog.DefaultExt = "png";
			saveImageDialog.AddExtension = true;
			saveImageDialog.Filter = "PNG Image|*.png";
			saveImageDialog.InitialDirectory = Directory.GetCurrentDirectory ();

			saveCSVDialog = new SaveFileDialog ();
			saveCSVDialog.DefaultExt = "csv";
			saveCSVDialog.AddExtension = true;
			saveCSVDialog.Filter = "Comma-Separated Values|*.csv";
			saveCSVDialog.InitialDirectory = Directory.GetCurrentDirectory ();

			listLabel = new Label ();
			listLabel.Text = "Min. spikes";
			Controls.Add (listLabel);

			exportCSVButton = new Button ();
			exportCSVButton.Text = "Export to .csv";
			exportCSVButton.Click += delegate {
				ExportCSV();
			};
			Controls.Add (exportCSVButton);

			showstatsButton = new Button();
			showstatsButton.Text = "Show Stats";
			showstatsButton.Click += delegate {
				ShowStats();
			};
			Controls.Add(showstatsButton);

			detectionStageButton = new Button ();
			detectionStageButton.Text = "Go to detection stage";
			detectionStageButton.Click += delegate {
				mainWindow.SetStageDetection ();
			};
			Controls.Add (detectionStageButton);

			cellsListBox = new ListBox ();
			cellsListBox.SelectionMode = SelectionMode.MultiExtended;
			cellsListBox.SelectedIndexChanged += CellsSelectedEvent;
			Controls.Add (cellsListBox);

			settingsGroup = new GroupBox ();
			settingsGroup.Text = "Spike detection";
			Controls.Add (settingsGroup);

			settingLag = new NumericUpDown ();
			settingLag.Minimum = 1m;
			settingLag.Maximum = 20m;
			settingLag.DecimalPlaces = 0;
			settingLag.Increment = 1m;
			settingLag.Value = 10m;
			settingLag.ValueChanged += delegate {
				ShowGraphs ();
			};
			settingsGroup.Controls.Add (settingLag);

			settingTreshold = new NumericUpDown ();
			settingTreshold.Minimum = 0m;
			settingTreshold.Maximum = 50m;
			settingTreshold.DecimalPlaces = 1;
			settingTreshold.Increment = 1m;
			settingTreshold.Value = 3m;
			settingTreshold.ValueChanged += delegate {
				ShowGraphs ();
			};
			settingsGroup.Controls.Add (settingTreshold);

			settingInfluence = new NumericUpDown ();
			settingInfluence.Minimum = 0m;
			settingInfluence.Maximum = 1m;
			settingInfluence.DecimalPlaces = 1;
			settingInfluence.Increment = 0.1m;
			settingInfluence.Value = 0;//
			settingInfluence.ValueChanged += delegate {
				ShowGraphs ();
			};
			settingsGroup.Controls.Add (settingInfluence);

			/*settingPeakTreshold = new NumericUpDown ();
			settingPeakTreshold.Minimum = 0m;
			settingPeakTreshold.Maximum = 1m;
			settingPeakTreshold.DecimalPlaces = 4;
			settingPeakTreshold.Increment = .005m;
			settingPeakTreshold.Value = .025m;
			settingPeakTreshold.ValueChanged += delegate {
				ShowGraphs ();
			};
			settingsGroup.Controls.Add (settingPeakTreshold); */

			showPeaks = new CheckBox ();
			showPeaks.Checked = true;
			showPeaks.Text = "Show spikes";
			showPeaks.CheckedChanged += delegate {
				ShowGraphs ();
			};
			settingsGroup.Controls.Add (showPeaks);


			detectionMode = new ComboBox ();
			detectionMode.Items.Add (PeakMode.Slope);
			detectionMode.Items.Add (PeakMode.Block);
			detectionMode.Items.Add(PeakMode.Peak);
			detectionMode.SelectedIndex = 0;
			detectionMode.SelectedIndexChanged += delegate {
				ShowGraphs();
			};
			settingsGroup.Controls.Add (detectionMode);

			saveGraphsImageButton = new Button ();
			saveGraphsImageButton.Text = "Save image";
			saveGraphsImageButton.Click += delegate {
				DialogResult result = saveImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = saveImageDialog.FileName;
					try
					{
						Image img1 = imageMatrixCollection.cellActivityContainer.GetGraph (selectedCells, graphsBox.Width,
							int.MaxValue, GraphType.Activity, true,showPeaks.Checked);
						//Image img2 = imageMatrixCollection.cellActivityContainer.GetGraph (selectedCells, graphROC.Width,
						//	int.MaxValue, GraphType.RateOfChange, true,showPeaks.Checked);
						Bitmap bitmap = new Bitmap(img1.Width, img1.Height);
						using (Graphics g = Graphics.FromImage(bitmap))
						{
							g.DrawImage(img1, 0, 0);
							//g.DrawImage(img2, img1.Width, 0);
						}
						bitmap.Save(filename);
					} catch (IOException e) {
						MainWindow.ShowMessage("Error saving file: " + e.Message);
					}
				}
			};
			settingsGroup.Controls.Add (saveGraphsImageButton);

			graphsBox = new PictureBox ();
			graphsBox.Image = Image.FromFile ("graphs.png");
			Controls.Add (graphsBox);
			graphsBox.SizeMode = PictureBoxSizeMode.Zoom;

			/*graphROC = new PictureBox ();
			graphROC.Image = Image.FromFile ("graphs.png");
			Controls.Add (graphROC);
			graphROC.SizeMode = PictureBoxSizeMode.Zoom;*/

			graphSimilarity = new PictureBox ();
			graphSimilarity.Image = Image.FromFile ("graphs.png");
			Controls.Add (graphSimilarity);
			graphSimilarity.SizeMode = PictureBoxSizeMode.Zoom;

			activityGroup = new GroupBox ();
			activityGroup.Text = "Spatial-temporal map";
			Controls.Add (activityGroup);

			activityMode = new ComboBox ();
			activityMode.Items.Add (ActivityDisplayMode.Average);
			activityMode.Items.Add (ActivityDisplayMode.MinMaxAverage);
			activityMode.SelectedIndex = 0;
			activityMode.SelectedIndexChanged += delegate {
				ShowGraphs();
			};
			activityGroup.Controls.Add (activityMode);


			activityMaxSize = new NumericUpDown ();
			activityMaxSize.Minimum = 0m;
			activityMaxSize.Maximum = .25m;
			activityMaxSize.DecimalPlaces = 3;
			activityMaxSize.Increment = .01m;
			activityMaxSize.Value = .1m;
			activityMaxSize.ValueChanged += delegate {
				ShowGraphs ();
			};
			activityGroup.Controls.Add (activityMaxSize);

			showActivityBackground = new CheckBox ();
			showActivityBackground.Text = "Show background image";
			showActivityBackground.Checked = false;
			showActivityBackground.CheckedChanged += delegate {
				ShowGraphs ();
			};
			activityGroup.Controls.Add (showActivityBackground);

			showActivityLabels = new CheckBox ();
			showActivityLabels.Text = "Show cell labels";
			showActivityLabels.Checked = false;
			showActivityLabels.CheckedChanged += delegate {
				ShowGraphs ();
			};
			activityGroup.Controls.Add (showActivityLabels);

			showActivityLegend = new CheckBox ();
			showActivityLegend.Text = "Show legend";
			showActivityLegend.Checked = true;
			showActivityLegend.CheckedChanged += delegate {
				ShowGraphs ();
			};
			activityGroup.Controls.Add (showActivityLegend);

			saveActivityImageButton = new Button ();
			saveActivityImageButton.Text = "Save image";
			saveActivityImageButton.Click += delegate {
				DialogResult result = saveImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = saveImageDialog.FileName;
					try
					{
						Image img;
						int w = imageMatrixCollection.images [0].width;
						int h = imageMatrixCollection.images [0].height;
						if (showActivityBackground.Checked) {
							img = imageMatrixCollection.cellActivityContainer.GetActivityMap (w, h, w, h, (float)activityMaxSize.Value, showActivityLabels.Checked,
								showActivityLegend.Checked,(ActivityDisplayMode)activityMode.SelectedItem, imageMatrixCollection.images[0].Bitmap);
						} else {
							img = imageMatrixCollection.cellActivityContainer.GetActivityMap (w, h, w, h, (float)activityMaxSize.Value, showActivityLabels.Checked,
								showActivityLegend.Checked,(ActivityDisplayMode)activityMode.SelectedItem);
						}
						img.Save(filename);
					} catch (IOException e) {
						MainWindow.ShowMessage("Error saving file: " + e.Message);
					}
				}
			};
			activityGroup.Controls.Add (saveActivityImageButton);


			mapActivity = new PictureBox ();
			mapActivity.Image = Image.FromFile ("graphs.png");
			mapActivity.SizeMode = PictureBoxSizeMode.Zoom;
			activityGroup.Controls.Add (mapActivity);

			connectionGroup = new GroupBox ();
			connectionGroup.Text = "Inter-neuronal correlations";
			Controls.Add (connectionGroup);

			//test1 = new GroupBox ();
			//test1.Text = "Test 1";
			//Controls.Add (test1);

			//test2 = new GroupBox ();
			//test2.Text = "Test 1";
			//Controls.Add (test2);

			connectionDistance = new NumericUpDown ();
			connectionDistance.Minimum = 0m;
			connectionDistance.Maximum = 1m;
			connectionDistance.DecimalPlaces = 2;
			connectionDistance.Increment = .01m;
			connectionDistance.Value = .1m;
			connectionDistance.ValueChanged += delegate {
				SetFrameData ();
				ShowConnectionMap ();
			};
			connectionGroup.Controls.Add (connectionDistance);

			connectionDt = new NumericUpDown ();
			connectionDt.Minimum = 0m;
			connectionDt.Maximum = 100m;
			connectionDt.DecimalPlaces = 0;
			connectionDt.Increment = 1m;
			connectionDt.Value = 1m;
			connectionDt.ValueChanged += delegate {
				SetFrameData ();
				ShowConnectionMap ();
			};
			connectionGroup.Controls.Add (connectionDt);

			connectionTreshold = new NumericUpDown ();
			connectionTreshold.Minimum = 0m;
			connectionTreshold.Maximum = 1m;
			connectionTreshold.DecimalPlaces = 2;
			connectionTreshold.Increment = .1m;
			connectionTreshold.Value = .33m;
			connectionTreshold.ValueChanged += delegate {
				ShowConnectionMap ();
			};
			connectionGroup.Controls.Add (connectionTreshold);

			frameWidth = new NumericUpDown ();
			frameWidth.Minimum = .001m;
			frameWidth.Maximum = 1000000m;
			frameWidth.DecimalPlaces = 3;
			frameWidth.Increment = 250m;
			frameWidth.Value = 1000m;
			frameWidth.ValueChanged += delegate {
				SetFrameData ();
			};
			connectionGroup.Controls.Add (frameWidth);

			frameRate = new NumericUpDown ();
			frameRate.Minimum = .01m;
			frameRate.Maximum = 10000m;
			frameRate.DecimalPlaces = 2;
			frameRate.Increment = 1m;
			frameRate.Value = 24m;
			frameRate.ValueChanged += delegate {
				SetFrameData ();
			};
			connectionGroup.Controls.Add (frameRate);

			frameWidthDescLabel = new Label ();
			frameWidthDescLabel.Text = "Footage width (µm)";
			connectionGroup.Controls.Add (frameWidthDescLabel);

			frameWidthLabel = new Label ();
			connectionGroup.Controls.Add (frameWidthLabel);

			frameRateDescLabel = new Label ();
			frameRateDescLabel.Text = "Footage speed (FPS)";
			connectionGroup.Controls.Add (frameRateDescLabel);

			frameRateLabel = new Label ();
			connectionGroup.Controls.Add (frameRateLabel);

			showConnectionBackground = new CheckBox ();
			showConnectionBackground.Text = "Show background image";
			showConnectionBackground.Checked = true;
			showConnectionBackground.CheckedChanged += delegate {
				ShowConnectionMap ();
			};
			connectionGroup.Controls.Add (showConnectionBackground);

			showConnectionLabels = new CheckBox ();
			showConnectionLabels.Text = "Show cell labels";
			showConnectionLabels.Checked = true;
			showConnectionLabels.CheckedChanged += delegate {
				ShowConnectionMap ();
			};
			connectionGroup.Controls.Add (showConnectionLabels);

			cellDrawMode = new ComboBox ();
			cellDrawMode.Items.Add ("Hide");
			cellDrawMode.Items.Add ("Dot");
			cellDrawMode.Items.Add ("Outline");
			cellDrawMode.SelectedIndex = 2;
			cellDrawMode.SelectedIndexChanged += delegate {
				ShowConnectionMap ();
			};
			connectionGroup.Controls.Add (cellDrawMode);

			saveConnectionImageButton = new Button ();
			saveConnectionImageButton.Text = "Save image";
			saveConnectionImageButton.Click += delegate {
				DialogResult result = saveImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = saveImageDialog.FileName;
					try
					{
						Image img;
						int w = imageMatrixCollection.images [0].width;
						int h = imageMatrixCollection.images [0].height;
						CellActivityContainer.ROIDisplayMode roiDisplayMode=CellActivityContainer.ROIDisplayMode.Hide;
						switch (cellDrawMode.SelectedIndex) {
						case 0:
							roiDisplayMode = CellActivityContainer.ROIDisplayMode.Hide;
							break;
						case 1:
							roiDisplayMode = CellActivityContainer.ROIDisplayMode.Dot;
							break;
						case 2:
							roiDisplayMode = CellActivityContainer.ROIDisplayMode.Outline;
							break;
						}
						if (showConnectionBackground.Checked) {
							img = imageMatrixCollection.cellActivityContainer.GetConnectionMap (w, h, w, h, shownCells, selectedCells,
								(float)connectionDistance.Value, (int)connectionDt.Value, (float)connectionTreshold.Value,
								showConnectionLabels.Checked, roiDisplayMode, imageMatrixCollection.images [0].Bitmap);
						} else {
							img = imageMatrixCollection.cellActivityContainer.GetConnectionMap (w, h, w, h, shownCells, selectedCells,
								(float)connectionDistance.Value, (int)connectionDt.Value, (float)connectionTreshold.Value,
								showConnectionLabels.Checked, roiDisplayMode);
						}
						img.Save(filename);
					} catch (IOException e) {
						MainWindow.ShowMessage("Error saving file: " + e.Message);
					}
				}
			};
			connectionGroup.Controls.Add (saveConnectionImageButton);

			SaveFileDialog saveConnectionDialog = new SaveFileDialog ();
			saveConnectionDialog.DefaultExt = "csv";
			saveConnectionDialog.AddExtension = true;
			saveConnectionDialog.Filter = "CSV data|*.csv";
			saveConnectionDialog.InitialDirectory = Directory.GetCurrentDirectory ();


			mapConnections = new PictureBox ();
			mapConnections.Image = Image.FromFile ("graphs.png");
			mapConnections.SizeMode = PictureBoxSizeMode.Zoom;
			connectionGroup.Controls.Add (mapConnections);

			graph1Label = new Label ();
			graph1Label.Text = "Delta F/F0";
			Controls.Add (graph1Label);

			/*graph2Label = new Label ();
			graph2Label.Text = "Rate of change";
			Controls.Add (graph2Label);*/

			correlationGroup = new GroupBox ();
			correlationGroup.Text = "Cross-Correlation";
			Controls.Add (correlationGroup);

			similarityMode = new ComboBox ();
			similarityMode.Items.Add (SimilarityMeasure.BarGraphXCorr);
			similarityMode.Items.Add (SimilarityMeasure.SquareMapXCorr);
			similarityMode.Items.Add (SimilarityMeasure.SingleCellXCorr);
			similarityMode.Items.Add (SimilarityMeasure.Heatmap);
			similarityMode.SelectedIndex = 0;
			similarityMode.SelectedIndexChanged += delegate {
				ShowGraphs();
			};
			correlationGroup.Controls.Add (similarityMode);

			similaritySource = new ComboBox ();
			similaritySource.Items.Add (SimilaritySource.Peak);
			//similaritySource.Items.Add (SimilaritySource.RateOfChange);
			similaritySource.Items.Add (SimilaritySource.Activity);
			similaritySource.SelectedIndex = 0;
			similaritySource.SelectedIndexChanged += delegate {
				ShowGraphs();
			};
			correlationGroup.Controls.Add (similaritySource);

			xCorrDelayLabel = new Label ();
			xCorrDelayLabel.Text = "Lag: ";
			correlationGroup.Controls.Add (xCorrDelayLabel);

			xCorrDelay = new NumericUpDown ();
			xCorrDelay.DecimalPlaces = 0;
			xCorrDelay.Minimum = -100;
			xCorrDelay.Maximum = 100;
			xCorrDelay.Value = 0;
			xCorrDelay.ValueChanged += delegate {
				ShowGraphs ();
			};
			correlationGroup.Controls.Add (xCorrDelay);

			saveCorrelationImageButton = new Button ();
			saveCorrelationImageButton.Text = "Save image";
			saveCorrelationImageButton.Click += delegate {
				DialogResult result = saveImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = saveImageDialog.FileName;
					try
					{
						//we can just save the image shown, UNLESS it's the square map, which is downscaled.
						if ((SimilarityMeasure)similarityMode.SelectedItem == SimilarityMeasure.SquareMapXCorr) {
							CellActivityContainer cac = imageMatrixCollection.cellActivityContainer;
							cac.GetCorrelationSquareGraph(selectedCells, 0, 0, (SimilaritySource)similaritySource.SelectedItem, (int)xCorrDelay.Value).Save(filename);
						} else {
							graphSimilarity.Image.Save(filename);
						}
					} catch (IOException e) {
						MainWindow.ShowMessage("Error saving file: " + e.Message);
					}
				}
			};
			correlationGroup.Controls.Add (saveCorrelationImageButton);




			shownCells = imageMatrixCollection.allCells;
			ShowCells ();

			//first load the tooltip for use in the various panels
			ToolTip = new ToolTip();
			// Set up the delays for the ToolTip.
			ToolTip.AutoPopDelay = 10000;
			ToolTip.InitialDelay = 500;
			ToolTip.ReshowDelay = 250;
			ToolTip.ToolTipIcon = ToolTipIcon.Info;
			// Force the ToolTip text to be displayed whether or not the form is active.
			ToolTip.ShowAlways = true;
			ToolTip.SetToolTip (pulseThreshold, "Only show ROIs with at least this number of spikes in the list below.");
			//ToolTip.SetToolTip (settingPeakTreshold, "Peaks will only be detected if the rate of change is more than this value.");
			ToolTip.SetToolTip (settingLag, "The lag for the Z-score algorithm. The standard deviation is calculated for this number of frames prior to the frame under consideration.\n" +
				"Spike detection will generally be more accurate with higher values, unless spikes occur shortly after one another.\n" +
				"Larger values will also increase the number of frames at the start where spike detection can be inaccurate (denoted by the area lightly shaded yellow in the graphs).");
			ToolTip.SetToolTip (settingTreshold, "The Z-score needed for a peak detection. This is a measure of how much above noise levels a value needs to be before it's detected as a spike.\n" +
				"Increasing this value will reduce false positives, at the cost of more false negatives.");
			ToolTip.SetToolTip (settingInfluence, "The influence of spikes on the sliding mean and sd. A higher influence can lead to less peaks being detected as such.");
			ToolTip.SetToolTip (connectionDistance, "Max. distance between cells to still consider a possible connection.\nIn fraction of the image diagonal. True size shown to the right, if the Footage width has been set.");
			ToolTip.SetToolTip (connectionDt, "The max. offset in frames to still consider, i.e. a connection between ROIs will only be detected if both within this number of frames apart.\n" +
				"The corresponding time delay is shown to the right, if the Footage speed has been set.");
			ToolTip.SetToolTip (connectionTreshold, "The minimum similarity between peaks (exclusive).\n" +
				"A connection between ROIs will only be detected if they have at least this ratio of peaks in common.");
			ToolTip.SetToolTip (activityMaxSize, "The size of the largest circles, in times the diagonal.");
			ToolTip.SetToolTip (xCorrDelay, "The lag to calculate the cross-correlation for.");
			ToolTip.SetToolTip (similaritySource, "The data used for calculations. Activity is the plain activity graph and Peak is an array with the elements being 1 for each peak frame and 0 otherwise.");
			ToolTip.SetToolTip (detectionMode, "How peaks are detected. Slope will only detect the first frame of a peak. Block will detect all frames with the Z-score surpassing the threshold. Peak will detect the frame with the highest value of adjacent frames surpassing the threshold.");
			ToolTip.SetToolTip (frameRate, "The framerate of the loaded footage, in frames per second. Note: changing this won't have any effect on connection detection, it only helps by providing you with real-world values in the boxes below.");
			ToolTip.SetToolTip (frameWidth, "The real-world width of the footage, in µm. Note: changing this won't have any effect on connection detection, it only helps by providing you with real-world values in the boxes below.");
			ToolTip.SetToolTip (graphsBox, "Blue: brightness of this ROI. Red: spikes detected for this ROI. Green: spikes detected for all selected ROIs (greener=more spikes).\n" +
				"Spike detection can be less accurate in the lightly shaded yellow area on the left (depending on the lag settings).");
			//ToolTip.SetToolTip (graphROC, "Blue: Rate of change in brightness of this ROI. Red: spikes detected for this ROI. Green: spikes detected for all selected ROIs (greener=more spikes).\n" +
			//	"Spike detection can be less accurate in the lightly shaded yellow area on the left (depending on the lag settings).");
			ToolTip.SetToolTip (graphSimilarity, "Use the controls above to show several graph types denoting the correlation between ROIs.");
			ToolTip.SetToolTip (showConnectionBackground, "Display the first frame at the background if checked, use a white background otherwise.");
			ToolTip.SetToolTip (showConnectionLabels, "Display the names of the ROIs next to the ROIs for easier identification.");
			ToolTip.SetToolTip (exportCSVButton, "Export various data as .csv files. Can be used to save both raw cell brightness data and processed data such as connections between ROIs.");
			ToolTip.SetToolTip (showstatsButton, "Show culture statistics, such as the number of (active) ROIs, spikes per activev ROI, average ROI size etc.");
			ToolTip.SetToolTip (saveActivityImageButton, "Save the spatio-temporal map to an image file.");
			ToolTip.SetToolTip (saveConnectionImageButton, "Save the connection map to an image file.");
			ToolTip.SetToolTip (saveCorrelationImageButton, "Save the image shown below to an image file.");
			ToolTip.SetToolTip (selectAllButton, "Select all ROIs shown in the list below. Only ROIs with at least the number of spikes listed above will be selected.");
			ToolTip.SetToolTip (saveGraphsImageButton, "Save the activity and rate of change graphs for all selected ROIs.");
			ToolTip.SetToolTip (selectSimilarPulsetimeCellsButton, "Select all ROIs that spike simultaneously with the selected ROI at least once.\n" +
				"Takes the topmost as reference if more than one ROI is selected.");
			ToolTip.SetToolTip (cellsListBox, "Select ROIs to show them in the graphs to the right.");
			ToolTip.SetToolTip (mapActivity, "Spatio-temporal map of the footage; the number of detected spikes per ROI is denoted by the ROI size (larger=more spikes).\n" +
				"The average time of the detected spikes is denoted by its colour (red=earlier, blue=later).");
			ToolTip.SetToolTip (mapConnections, "Connection map. Only ROIs shown in the list on the left of the screen are shown; selected ROIs are red/purple.\n" +
				"If the direction of communication could be established (by a time difference between spikes), the connection is shown as an arrow, otherwise as a line.");
			ToolTip.SetToolTip (showActivityBackground, "Display the first frame at the background if checked, use a black background otherwise.");
			ToolTip.SetToolTip (showActivityLabels, "Display the names of the ROIs next to the ROIs for easier identification.");
			ToolTip.SetToolTip (showActivityLegend, "Display a legend for the spatio-temporal map.");
			ToolTip.SetToolTip (showPeaks, "Display detected spikes in the graphs below.");
			ToolTip.SetToolTip (similarityMode, "Select the type of graph to show below.");
			ToolTip.SetToolTip (similaritySource, "Select the source of the data to display.\n" +
				"Peak: the detected spikes. Activity: the delta F/F0 trace of each ROI.");
			ToolTip.SetToolTip (activityMode, "Average: colour the ROIs by the average spike time (red=earlier, blue=later).\n" +
				"MinMaxAverage: colour ROIs by, in clockwise order starting at the top, the earliest spiking time, the average spiking time, and the latest spiking time.");
			ToolTip.SetToolTip (cellDrawMode, "Hide: don't show ROI's (only show connections). Dot: display ROIs by a dot. Outline: draws the outline of each ROI.");


			Resize += delegate {
				ResizeWindow ();
			};

			ResizeWindow ();
		}

		void UpdateSelectedCells () {
			selectedCells.Clear ();
			foreach (object o in cellsListBox.SelectedItems) {
				selectedCells.Add ((BrainCell)o);
			}
		}

		void ExportCSV ()
		{
			if (selectedCells.Count == 0) {
				MessageBox.Show ("Please select the ROIs you want to export from the list on the left.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			Form saveCSVDataForm = new Form ();
			saveCSVDataForm.Text = "Select data to export";
			saveCSVDataForm.TopMost = true;
			saveCSVDataForm.FormBorderStyle = FormBorderStyle.FixedDialog;

			GroupBox saveCSVDataForm_a = new GroupBox ();
			saveCSVDataForm_a.Text = "Calcium Imaging data";
			saveCSVDataForm_a.Width = 384;
			saveCSVDataForm.Controls.Add (saveCSVDataForm_a);

			Label saveCSVDataForm_a_l = new Label ();
			saveCSVDataForm_a_l.Text = "Data to export:";
			saveCSVDataForm_a_l.Width = TextRenderer.MeasureText (saveCSVDataForm_a_l.Text, Utils.font).Width + 4;
			saveCSVDataForm_a_l.Left = saveCSVDataForm_a.DisplayRectangle.Left;
			saveCSVDataForm_a_l.Top = saveCSVDataForm_a.DisplayRectangle.Top;
			saveCSVDataForm_a.Controls.Add (saveCSVDataForm_a_l);

			ComboBox saveCSVDataForm_a_mode = new ComboBox ();
			saveCSVDataForm_a_mode.Items.Add ("ROI Brightness");
			saveCSVDataForm_a_mode.Items.Add ("F/F0");
			//saveCSVDataForm_a_mode.Items.Add ("Brightness rate of change");
			saveCSVDataForm_a_mode.Items.Add ("Detected spikes");
			saveCSVDataForm_a_mode.SelectedIndex = 0;
			saveCSVDataForm_a_mode.Left = saveCSVDataForm_a_l.Right;
			saveCSVDataForm_a_mode.Top = saveCSVDataForm_a_l.Top;
			saveCSVDataForm_a_mode.Width = (2*(saveCSVDataForm_a.DisplayRectangle.Width - saveCSVDataForm_a_l.Width)) / 3;
			saveCSVDataForm_a.Controls.Add (saveCSVDataForm_a_mode);

			Button saveCSVDataForm_a_Save = new Button ();
			saveCSVDataForm_a_Save.Text = "Export";
			saveCSVDataForm_a_Save.Click += delegate {
				string data = null;

				switch (saveCSVDataForm_a_mode.SelectedIndex) {
				case 0:
					data = imageMatrixCollection.cellActivityContainer.GetCSVData (selectedCells, CellActivityContainer.CSVExportMode.Raw);
					break;
				case 1:
					data = imageMatrixCollection.cellActivityContainer.GetCSVData (selectedCells, CellActivityContainer.CSVExportMode.FF0);
					break;
				case 2:
					data = imageMatrixCollection.cellActivityContainer.GetCSVData (selectedCells, CellActivityContainer.CSVExportMode.Peak);
					break;
				}



				if (data == null) {
					MessageBox.Show ("Please select the cells you want to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				} else {
					DialogResult result = saveCSVDialog.ShowDialog (); // Show the dialog.
					if (result == DialogResult.OK) // Test result.
					{
						string filename = saveCSVDialog.FileName;
						try {
							System.IO.File.WriteAllText (filename, data);
							MainWindow.ShowMessage ("Saved CSV data to " + filename + ".");
						} catch (IOException e) {
							MainWindow.ShowMessage ("Error saving file: " + e.Message);
						}
						saveCSVDataForm.Close ();
					}
				}
			};
			saveCSVDataForm_a_Save.Left = saveCSVDataForm_a_mode.Right;
			saveCSVDataForm_a_Save.Top = saveCSVDataForm_a_mode.Top;
			saveCSVDataForm_a_Save.Width = saveCSVDataForm_a.DisplayRectangle.Width - saveCSVDataForm_a_l.Width - saveCSVDataForm_a_mode.Width;
			saveCSVDataForm_a.Controls.Add (saveCSVDataForm_a_Save);

			saveCSVDataForm_a.Height = saveCSVDataForm_a_Save.Height + saveCSVDataForm_a.Height - saveCSVDataForm_a.DisplayRectangle.Height;


			GroupBox saveCSVDataForm_c = new GroupBox ();
			saveCSVDataForm_c.Text = "Connection data";
			saveCSVDataForm_c.Width = 384;
			saveCSVDataForm_c.Top = saveCSVDataForm_a.Bottom;
			saveCSVDataForm.Controls.Add (saveCSVDataForm_c);

			Label saveCSVDataForm_c_l = new Label ();
			saveCSVDataForm_c_l.Text = "Data to export:";
			saveCSVDataForm_c_l.Width = TextRenderer.MeasureText (saveCSVDataForm_c_l.Text, Utils.font).Width + 4;
			saveCSVDataForm_c_l.Left = saveCSVDataForm_c.DisplayRectangle.Left;
			saveCSVDataForm_c_l.Top = saveCSVDataForm_c.DisplayRectangle.Top;
			saveCSVDataForm_c.Controls.Add (saveCSVDataForm_c_l);

			ComboBox saveCSVDataForm_c_mode = new ComboBox ();
			saveCSVDataForm_c_mode.Items.Add ("Connections");
			saveCSVDataForm_c_mode.Items.Add ("Binary adjacency");
			saveCSVDataForm_c_mode.Items.Add ("Correlation (peak)");
			saveCSVDataForm_c_mode.Items.Add ("Correlation (brightness)");
			saveCSVDataForm_c_mode.Items.Add ("Correlation (rate of change)");
			saveCSVDataForm_c_mode.SelectedIndex = 0;
			saveCSVDataForm_c_mode.Left = saveCSVDataForm_a_l.Right;
			saveCSVDataForm_c_mode.Top = saveCSVDataForm_a_l.Top;
			saveCSVDataForm_c_mode.Width = (2*(saveCSVDataForm_c.DisplayRectangle.Width - saveCSVDataForm_c_l.Width)) / 3;
			saveCSVDataForm_c.Controls.Add (saveCSVDataForm_c_mode);


			Button saveCSVDataForm_c_Save = new Button ();
			saveCSVDataForm_c_Save.Text = "Export";
			saveCSVDataForm_c_Save.Click += delegate {
				string data = null;

				int w = imageMatrixCollection.images [0].width;
				int h = imageMatrixCollection.images [0].height;
				switch (saveCSVDataForm_c_mode.SelectedIndex) {
				case 0:
					data = imageMatrixCollection.cellActivityContainer.GetConnectionData (w, h, selectedCells,
						(float)connectionDistance.Value, (int)connectionDt.Value, (float)connectionTreshold.Value);
					break;
				case 1:
					data = imageMatrixCollection.cellActivityContainer.GetBinaryAdjacencyData (w, h, selectedCells,
						(float)connectionDistance.Value, (int)connectionDt.Value, (float)connectionTreshold.Value);
					break;
				case 2:
					data = imageMatrixCollection.cellActivityContainer.GetXCorrData (selectedCells,SimilaritySource.Peak);
					break;
				case 3:
					data = imageMatrixCollection.cellActivityContainer.GetXCorrData (selectedCells, SimilaritySource.Activity);
					break;
				//case 4:
					//data = imageMatrixCollection.cellActivityContainer.GetXCorrData (selectedCells, SimilaritySource.RateOfChange);
					break;
				}

				if (data == null) {
					MessageBox.Show ("Please select the cells you want to save.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				} else {
					DialogResult result = saveCSVDialog.ShowDialog (); // Show the dialog.
					if (result == DialogResult.OK) // Test result.
					{
						string filename = saveCSVDialog.FileName;
						try {
							System.IO.File.WriteAllText (filename, data);
							MainWindow.ShowMessage ("Saved CSV data to " + filename + ".");
						} catch (IOException e) {
							MainWindow.ShowMessage ("Error saving file: " + e.Message);
						}
						saveCSVDataForm.Close ();
					}
				}
			};
			saveCSVDataForm_c_Save.Left = saveCSVDataForm_c_mode.Right;
			saveCSVDataForm_c_Save.Top = saveCSVDataForm_c_mode.Top;
			saveCSVDataForm_c_Save.Width = saveCSVDataForm_c.DisplayRectangle.Width - saveCSVDataForm_c_l.Width - saveCSVDataForm_c_mode.Width;
			saveCSVDataForm_c.Controls.Add (saveCSVDataForm_c_Save);

			saveCSVDataForm_c.Height = saveCSVDataForm_c_Save.Height + saveCSVDataForm_c.Height - saveCSVDataForm_c.DisplayRectangle.Height;


			saveCSVDataForm.Width = saveCSVDataForm_a.Width + saveCSVDataForm.Width - saveCSVDataForm.DisplayRectangle.Width;
			Label saveCSVDataForm_l = new Label ();
			saveCSVDataForm_l.Width = saveCSVDataForm.Width;
			saveCSVDataForm_l.Top = saveCSVDataForm_c.Bottom;
			saveCSVDataForm_l.Text = "Exporting data can take a while.";
			saveCSVDataForm.Controls.Add (saveCSVDataForm_l);
			saveCSVDataForm.Height = saveCSVDataForm_a.Height + saveCSVDataForm_c.Height + saveCSVDataForm.Height + saveCSVDataForm_l.Height - saveCSVDataForm.DisplayRectangle.Height;
			saveCSVDataForm.Show ();
		}

		void ShowStats() {

			List<int> roisizevector = new List<int>(imageMatrixCollection.detectedCells.Count);
			// ROI size stats
			int NOcells = imageMatrixCollection.detectedCells.Count;
			float roisizemean = 0;
			float roisizesd = 0;
			float sqpixelsize = (float)frameWidth.Value / imageMatrixCollection.footageSize.Width;
			sqpixelsize = sqpixelsize * sqpixelsize;

			for (int j=0;j< NOcells; j++) {
				roisizevector.Add(imageMatrixCollection.detectedCells[j].numberofpixels());
				roisizemean = roisizemean + (float)imageMatrixCollection.detectedCells[j].numberofpixels();
			}
			roisizemean = roisizemean / (float)NOcells;

			for (int j = 0; j < NOcells; j++) {
				roisizesd = roisizesd + (float) Math.Pow(((float) imageMatrixCollection.detectedCells[j].numberofpixels()- roisizemean),2);
			}

			roisizevector.Sort();
			roisizemean = roisizemean;

			roisizesd = roisizesd / ((float) NOcells-1);
			roisizesd = (float) Math.Sqrt(roisizesd);
			roisizesd = roisizesd;

			// ROI activity stats
			int activerois = 0;
			int raterois = 0;
			for (int j = 0; j < NOcells; j++) {
				if (imageMatrixCollection.cellActivityContainer.GetPeakFrames(j).Length > 0) {  // ROI is active
					activerois = activerois + 1;
				}
				if (imageMatrixCollection.cellActivityContainer.GetPeakFrames(j).Length > 1) {  // ROI is active
					raterois = raterois + 1;
				}
			}
			List<int> roiactivityvector = new List<int>(activerois);
			List<float> roiratevector = new List<float>(raterois);
			float framerate = float.Parse(frameRate.Text);
			float activitymean = 0;
			float activitysd = 0;
			float ratemean = 0;
			float ratesd = 0;

			for (int j = 0; j < NOcells; j++) {
				int[] peakframestemp = imageMatrixCollection.cellActivityContainer.GetPeakFrames(j);
				if (peakframestemp.Length > 0) {  // ROI is active
					roiactivityvector.Add(peakframestemp.Length);
					activitymean = activitymean + (float)peakframestemp.Length;
				}
				if (peakframestemp.Length > 1) { //more than 1 peak -> rate
					int[] peakframes = peakframestemp;
					float firstframe = (float) peakframes[0];
					float lastframe = (float) peakframes[peakframestemp.Length-1];
					roiratevector.Add(((float) peakframestemp.Length-1)/((lastframe-firstframe)/(framerate)));
				}
			}
			activitymean = activitymean / activerois;
			roiactivityvector.Sort();
			roiratevector.Sort();

			if (activerois > 1) {
				for (int j = 0; j < activerois; j++) {
					activitysd = activitysd + (float)Math.Pow((roiactivityvector[j] - activitymean), 2);
				}
				activitysd = (float)Math.Sqrt(activitysd / ((float)activerois - 1));
			} else { activitymean = 0; activitysd = 0; activerois = 1; roiactivityvector.Add((int)1);}

			// rate
			for (int j = 0; j < roiratevector.Count; j++) {
				ratemean = ratemean + roiratevector[j];
			}
			ratemean = ratemean / roiratevector.Count;
			for (int j = 0; j < roiratevector.Count; j++) {
				ratesd = ratesd+(float)Math.Pow(roiratevector[j] - ratemean,2);
			}
			ratesd = (float)Math.Sqrt(ratesd / (roiratevector.Count - 1));

			MessageBox.Show(string.Format("Footage stats " + Environment.NewLine +
				"Number of frames \t " + imageMatrixCollection.images.Count + Environment.NewLine +
				"Footage width \t " + frameWidth.Text + " [\u00b5m]" + Environment.NewLine +
				"Frame rate \t " + frameRate.Text + " [FPS]" + Environment.NewLine +
				"---------------------------------------------" + Environment.NewLine +
				"ROI stats \t " + Environment.NewLine +
				"Number of ROIs \t " + NOcells + Environment.NewLine +
				"ROI size [pixels] \t mean \t " + roisizemean + "\u00b1 " + roisizesd + Environment.NewLine +
				" \t\t median \t " + roisizevector[(int)Math.Round((NOcells - 1) / 2.0)] + Environment.NewLine +
				" \t\t 5-95% \t " + roisizevector[(int)Math.Round(0.05 * (NOcells - 1))] +
				"-" + roisizevector[(int)Math.Round(0.95 * (NOcells - 1))] + Environment.NewLine +
				"ROI size [\u00b5m^2] \t mean \t " + roisizemean * sqpixelsize + "\u00b1 " + roisizesd * sqpixelsize + Environment.NewLine +
				" \t\t median \t " + sqpixelsize * (float) roisizevector[(int) Math.Round((NOcells-1)/ 2.0)] + Environment.NewLine +
				" \t\t 5-95% \t " + sqpixelsize * (float) roisizevector[(int)Math.Round(0.05*(NOcells-1))] +
				"-" + sqpixelsize * (float) roisizevector[(int)Math.Round(0.95 * (NOcells-1))] + Environment.NewLine +
				"---------------------------------------------" + Environment.NewLine +
				"Activity stats \t " + Environment.NewLine +
				"Active ROIs (aROI) \t " + activerois + Environment.NewLine +
				"Peaks per aROI \t mean \t " + activitymean + "\u00b1 " + activitysd + Environment.NewLine +
				" \t\t median \t " + roiactivityvector[(int)Math.Round((activerois-1) / 2.0)] + Environment.NewLine +
				" \t\t 5-95% \t " + roiactivityvector[(int)Math.Round((activerois-1)*0.05)] + "-" + roiactivityvector[(int)Math.Round((activerois-1) * 0.95)]+ Environment.NewLine +
				"Peak rates [Hz] \t mean \t " + ratemean + "\u00b1 " + ratesd + Environment.NewLine +
				" \t\t median \t " + roiratevector[(int)Math.Round((roiratevector.Count - 1) / 2.0)] + Environment.NewLine +
				" \t\t 5-95% \t " + roiratevector[(int)Math.Round((roiratevector.Count - 1) * 0.05)] + "-" + roiratevector[(int)Math.Round((roiratevector.Count - 1) * 0.95)]
				, "Stats", MessageBoxButtons.OK, MessageBoxIcon.Information));
		}

		void ShowCells () {
			//temporarily block cell selection
			cellsListBox.SelectedIndexChanged -= CellsSelectedEvent;
			cellsListBox.Items.Clear ();
			//ListBox.ObjectCollection cellsCollection = new ListBox.ObjectCollection (cellsListBox, shownCells.ToArray ());
			//cellsListBox.Items.AddRange (cellsCollection);
			cellsListBox.Items.AddRange(shownCells.ToArray());
			foreach (BrainCell c in selectedCells) {
				int index = shownCells.IndexOf (c);
				if (index >= 0) {
					cellsListBox.SetSelected (index, true);
				}
			}
			//re-enable cell selection
			cellsListBox.SelectedIndexChanged += CellsSelectedEvent;
			UpdateSelectedCells ();
		}

		public void ShowWindow () {
			cellsListBox.ClearSelected ();
			settingLag.Maximum = imageMatrixCollection.images.Count-1;
			Show ();
			SetFrameData ();
			ShowGraphs ();
		}

		void CellsSelectedEvent (Object o, EventArgs e) {
			UpdateSelectedCells ();
			ShowGraphs ();
		}

		void UpdateGraphs () {
			List<BrainCell> newCells = new List<BrainCell> ();
			int threshold = (int)pulseThreshold.Value;
			int multiPeak;
			if ((PeakMode)detectionMode.SelectedItem == PeakMode.Slope) {
				multiPeak = 0;
			} else if ((PeakMode)detectionMode.SelectedItem == PeakMode.Block) {
				multiPeak = 1;
			} else { 
				multiPeak = 2; 
			}

			imageMatrixCollection.cellActivityContainer.CalculatePulseFrames ((int)settingLag.Value,(float)settingTreshold.Value,(float)settingInfluence.Value, multiPeak);

			foreach (BrainCell c in imageMatrixCollection.allCells) {
				if (imageMatrixCollection.cellActivityContainer.GetCellPeakFrameCount(c) >= threshold) {
					newCells.Add (c);
				}
			}
			shownCells = newCells;

			ShowCells ();
		}

		void ShowGraphs () {
			if (cellsListBox.SelectedIndex < 0) {
				cellsListBox.ClearSelected ();
			}
			int multiPeak;
			if ((PeakMode)detectionMode.SelectedItem == PeakMode.Slope) {
				multiPeak = 0;
			} else if ((PeakMode)detectionMode.SelectedItem == PeakMode.Block) {
				multiPeak = 1;
			} else { 
				multiPeak = 2; 
			}

			imageMatrixCollection.cellActivityContainer.CalculatePulseFrames ((int)settingLag.Value,(float)settingTreshold.Value,(float)settingInfluence.Value, multiPeak);

			CellActivityContainer cac = imageMatrixCollection.cellActivityContainer;
			graphsBox.Image = cac.GetGraph (selectedCells, graphsBox.Width, graphsBox.Height, GraphType.Activity, 
				true,showPeaks.Checked);
			//graphROC.Image = cac.GetGraph (selectedCells, graphROC.Width, graphROC.Height, GraphType.RateOfChange, 
			//	true,showPeaks.Checked);
			switch ((SimilarityMeasure)similarityMode.SelectedItem) {
			case SimilarityMeasure.BarGraphXCorr:
				graphSimilarity.Image = cac.GetCorrelationBarGraph (selectedCells, graphSimilarity.Width, graphSimilarity.Height, 
					(SimilarityMeasure)similarityMode.SelectedItem,	(SimilaritySource)similaritySource.SelectedItem);
				break;
			case SimilarityMeasure.SquareMapXCorr:
				graphSimilarity.Image = cac.GetCorrelationSquareGraph (selectedCells, graphSimilarity.Width, graphSimilarity.Height,
					(SimilaritySource)similaritySource.SelectedItem, (int)xCorrDelay.Value);
				break;
			case SimilarityMeasure.SingleCellXCorr:
				graphSimilarity.Image = cac.GetCorrelationVoronoiGraph (selectedCells, (SimilaritySource)similaritySource.SelectedItem, (int)xCorrDelay.Value);
				break;
			case SimilarityMeasure.Heatmap:
				graphSimilarity.Image = cac.GetHeatMap (imageMatrixCollection.footageSize.Width, imageMatrixCollection.footageSize.Height, (SimilaritySource)similaritySource.SelectedItem);
				break;
			}

			UpdateGraphs ();
			if (showActivityBackground.Checked) {
				mapActivity.Image = cac.GetActivityMap (mapActivity.Width, mapActivity.Height, imageMatrixCollection.images [0].width,
					imageMatrixCollection.images [0].height, (float)activityMaxSize.Value, showActivityLabels.Checked, showActivityLegend.Checked, 
					(ActivityDisplayMode)activityMode.SelectedItem, imageMatrixCollection.images[0].Bitmap);
			} else {
				mapActivity.Image = cac.GetActivityMap (mapActivity.Width, mapActivity.Height, imageMatrixCollection.images [0].width,
					imageMatrixCollection.images [0].height, (float)activityMaxSize.Value, showActivityLabels.Checked, showActivityLegend.Checked,
					(ActivityDisplayMode)activityMode.SelectedItem);
			}
			ShowConnectionMap ();
		}

		void ShowConnectionMap ()
		{
			CellActivityContainer.ROIDisplayMode roiDisplayMode = CellActivityContainer.ROIDisplayMode.Hide;
			switch (cellDrawMode.SelectedIndex) {
			case 0:
				roiDisplayMode = CellActivityContainer.ROIDisplayMode.Hide;
				break;
			case 1:
				roiDisplayMode = CellActivityContainer.ROIDisplayMode.Dot;
				break;
			case 2:
				roiDisplayMode = CellActivityContainer.ROIDisplayMode.Outline;
				break;
			}
			if (showConnectionBackground.Checked) {
				mapConnections.Image = imageMatrixCollection.cellActivityContainer.GetConnectionMap (mapConnections.Width, mapConnections.Height,
					imageMatrixCollection.images [0].width, imageMatrixCollection.images [0].height, shownCells,selectedCells,
					(float)connectionDistance.Value, (int)connectionDt.Value, (float)connectionTreshold.Value,
					showConnectionLabels.Checked, roiDisplayMode, imageMatrixCollection.images [0].Bitmap);
			} else {
				mapConnections.Image = imageMatrixCollection.cellActivityContainer.GetConnectionMap (mapConnections.Width, mapConnections.Height,
					imageMatrixCollection.images [0].width, imageMatrixCollection.images [0].height, shownCells,selectedCells,
					(float)connectionDistance.Value, (int)connectionDt.Value, (float)connectionTreshold.Value, showConnectionLabels.Checked, roiDisplayMode);
			}
		}

		void RemoveIdle () {
			List<BrainCell> newCells = new List<BrainCell> ();
			int threshold = (int)pulseThreshold.Value;
			int multiPeak;
			if ((PeakMode)detectionMode.SelectedItem == PeakMode.Slope) {
				multiPeak = 0;
			} else if ((PeakMode)detectionMode.SelectedItem == PeakMode.Block) {
				multiPeak = 1;
			} else { 
				multiPeak = 2; 
			}

			imageMatrixCollection.cellActivityContainer.CalculatePulseFrames ((int)settingLag.Value,(float)settingTreshold.Value,(float)settingInfluence.Value, multiPeak);
			
			foreach (BrainCell c in imageMatrixCollection.allCells) {
				if (imageMatrixCollection.cellActivityContainer.GetCellPeakFrameCount(c) >= threshold) {
					newCells.Add (c);
				}
			}
			shownCells = newCells;

			ShowCells ();
		}

		void SetFrameData () {
			float fps = (float)frameRate.Value;
			float fw  = (float)frameWidth.Value;
			float spf = (float)(connectionDt.Value+1) / fps;//seconds per frame
			float cd = fw * (float)connectionDistance.Value;
			double w = imageMatrixCollection.images [0].width;
			double h = imageMatrixCollection.images [0].height;
			float diag = (float)(Math.Sqrt (w * w + h * h) / w);//ratio between diagonal and width

			if (spf > .5) {
				frameRateLabel.Text = "(<" + spf.ToString ("0.00") + "s)";
			} else {
				frameRateLabel.Text = "(<" + (1000f*spf).ToString ("0") + "ms)";
			}
			if (cd > 1000f) {
				frameWidthLabel.Text = "(<" + (diag * cd / 1000f).ToString ("0") + "mm)";
			} else if (cd > 1f) {
				frameWidthLabel.Text = "(<" + (diag * cd).ToString ("0") + "µm)";
			} else {
				frameWidthLabel.Text = "(<" + (diag * 1000f * cd).ToString ("0") + "nm)";
			}
		}

		public void ResizeWindow () {
			detectionStageButton.Top = 0;
			detectionStageButton.Left = 0;
			detectionStageButton.Width = 128;

			exportCSVButton.Top = detectionStageButton.Bottom;
			exportCSVButton.Left = 0;
			exportCSVButton.Width = 128;

			showstatsButton.Top = exportCSVButton.Bottom;
			showstatsButton.Left = 0;
			showstatsButton.Width = 128;

			listLabel.Top = showstatsButton.Bottom;
			listLabel.Width = TextRenderer.MeasureText (listLabel.Text, Utils.font).Width + 4;
			listLabel.Left = 0;

			pulseThreshold.Top = showstatsButton.Bottom;
			pulseThreshold.Left = listLabel.Right;
			pulseThreshold.Width = 128 - listLabel.Width;

			selectAllButton.Top = pulseThreshold.Bottom;
			selectAllButton.Left = 0;
			selectAllButton.Width = 128;

			selectSimilarPulsetimeCellsButton.Top = selectAllButton.Bottom;
			selectSimilarPulsetimeCellsButton.Left = 0;
			selectSimilarPulsetimeCellsButton.Width = 128;

			cellsListBox.Left = 0;
			cellsListBox.Top = selectSimilarPulsetimeCellsButton.Bottom;
			cellsListBox.Width = 128;
			cellsListBox.Height = DisplayRectangle.Height - selectSimilarPulsetimeCellsButton.Bottom;

			settingsGroup.Left = cellsListBox.Right;
			settingsGroup.Top = 0;
			settingsGroup.Height = settingsGroup.Height - settingsGroup.DisplayRectangle.Height + settingLag.Height + showPeaks.Height;
			settingsGroup.Width = 256 * 2;

			settingLag.Left = settingsGroup.DisplayRectangle.Left;
			settingLag.Top = settingsGroup.DisplayRectangle.Top;
			settingLag.Width = settingsGroup.DisplayRectangle.Width / 4;

			settingTreshold.Left = settingLag.Right;
			settingTreshold.Top = settingLag.Top;
			settingTreshold.Width = settingsGroup.DisplayRectangle.Width / 4;

			settingInfluence.Left = settingTreshold.Right;
			settingInfluence.Top = settingLag.Top;
			settingInfluence.Width = settingsGroup.DisplayRectangle.Width / 4;

			//settingPeakTreshold.Left = settingInfluence.Right;
			//settingPeakTreshold.Top = settingLag.Top;
			//settingPeakTreshold.Width = settingsGroup.DisplayRectangle.Width / 4;

			showPeaks.Left = settingsGroup.DisplayRectangle.Left;
			showPeaks.Top = settingLag.Bottom;

			detectionMode.Left = showPeaks.Right;
			detectionMode.Top = showPeaks.Top;

			saveGraphsImageButton.Left = detectionMode.Right;
			saveGraphsImageButton.Top = showPeaks.Top;

			graph1Label.Left = cellsListBox.Right;
			graph1Label.Top = settingsGroup.Bottom;

			graphsBox.Left = cellsListBox.Right;
			graphsBox.Top = graph1Label.Bottom;
			graphsBox.Height = DisplayRectangle.Height - settingsGroup.Height - graph1Label.Height;
			graphsBox.Width = 512;//256;

			/*graph2Label.Left = graphsBox.Right;
			graph2Label.Top = settingsGroup.Bottom;

			graphROC.Left = graphsBox.Right;
			graphROC.Top = graph2Label.Bottom;
			graphROC.Height = DisplayRectangle.Height - settingsGroup.Height - graph2Label.Height;
			graphROC.Width = 256; */

			correlationGroup.Left = graphsBox.Right;
			correlationGroup.Top = 0;
			correlationGroup.Width = 256;
			correlationGroup.Height = correlationGroup.Height + similarityMode.Height + similaritySource.Height + xCorrDelay.Height + saveCorrelationImageButton.Height - correlationGroup.DisplayRectangle.Height;

			similarityMode.Left = correlationGroup.DisplayRectangle.Left;
			similarityMode.Top = correlationGroup.DisplayRectangle.Top;
			similarityMode.Width = correlationGroup.DisplayRectangle.Width;

			similaritySource.Left = correlationGroup.DisplayRectangle.Left;
			similaritySource.Top = similarityMode.Bottom;
			similaritySource.Width = correlationGroup.DisplayRectangle.Width;

			xCorrDelayLabel.Left = correlationGroup.DisplayRectangle.Left;
			xCorrDelayLabel.Width = TextRenderer.MeasureText (xCorrDelayLabel.Text, Utils.font).Width;
			xCorrDelayLabel.Top = similaritySource.Bottom;

			xCorrDelay.Left = xCorrDelayLabel.Right;
			xCorrDelay.Top = xCorrDelayLabel.Top;

			saveCorrelationImageButton.Left = correlationGroup.DisplayRectangle.Left;
			saveCorrelationImageButton.Top = xCorrDelay.Bottom;
			saveCorrelationImageButton.Width = correlationGroup.DisplayRectangle.Width;

			graphSimilarity.Left = graphsBox.Right;
			graphSimilarity.Top = correlationGroup.Bottom;
			graphSimilarity.Height = DisplayRectangle.Height - correlationGroup.Bottom;
			graphSimilarity.Width = 256;

			activityGroup.Top = 0;
			activityGroup.Left = graphSimilarity.Right;
			activityGroup.Width = DisplayRectangle.Width - graphSimilarity.Right;
			activityGroup.Height = DisplayRectangle.Height / 2;

			activityMode.Top = activityGroup.DisplayRectangle.Top;
			activityMode.Left = activityGroup.DisplayRectangle.Left;
			activityMode.Width = 64;

			activityMaxSize.Top = activityGroup.DisplayRectangle.Top;
			activityMaxSize.Left = activityMode.Left;
			activityMaxSize.Width = 64;

			showActivityBackground.Top = activityGroup.DisplayRectangle.Top;
			showActivityBackground.Left = activityMaxSize.Right;

			showActivityLabels.Top = showActivityBackground.Top;
			showActivityLabels.Left = showActivityBackground.Right;

			showActivityLegend.Top = showActivityBackground.Top;
			showActivityLegend.Left = showActivityLabels.Right;

			saveActivityImageButton.Top = showActivityBackground.Top;
			saveActivityImageButton.Left = showActivityLegend.Right;

			mapActivity.Left = activityGroup.DisplayRectangle.Left;
			mapActivity.Top = showActivityBackground.Bottom;
			mapActivity.Height = activityGroup.DisplayRectangle.Bottom - showActivityBackground.Bottom;
			mapActivity.Width = activityGroup.DisplayRectangle.Width;

			connectionGroup.Top = activityGroup.Bottom;
			connectionGroup.Left = graphSimilarity.Right;
			connectionGroup.Width = DisplayRectangle.Width - connectionGroup.Left;
			connectionGroup.Height = DisplayRectangle.Height - connectionGroup.Top;

			frameRateDescLabel.Top = connectionGroup.DisplayRectangle.Top;
			frameRateDescLabel.Left = connectionGroup.DisplayRectangle.Left;
			frameRateDescLabel.Width = TextRenderer.MeasureText (frameRateDescLabel.Text, Utils.font).Width;

			frameRate.Top = connectionGroup.DisplayRectangle.Top;
			frameRate.Left = frameRateDescLabel.Right;
			frameRate.Width = 64;

			frameWidthDescLabel.Top = connectionGroup.DisplayRectangle.Top;
			frameWidthDescLabel.Left = frameRate.Right;
			frameWidthDescLabel.Width = TextRenderer.MeasureText (frameWidthDescLabel.Text, Utils.font).Width;

			frameWidth.Top = connectionGroup.DisplayRectangle.Top;
			frameWidth.Left = frameWidthDescLabel.Right;
			frameWidth.Width = 64;

			showConnectionBackground.Top = connectionGroup.DisplayRectangle.Top;
			showConnectionBackground.Left = frameWidth.Right;

			showConnectionLabels.Top = showConnectionBackground.Top;
			showConnectionLabels.Left = showConnectionBackground.Right;

			connectionDistance.Top = frameRate.Bottom;
			connectionDistance.Left = connectionGroup.DisplayRectangle.Left;
			connectionDistance.Width = 48;

			frameWidthLabel.Left = connectionDistance.Right;
			frameWidthLabel.Top = frameRate.Bottom;
			frameWidthLabel.Width = 64;

			connectionDt.Top = frameRate.Bottom;
			connectionDt.Left = frameWidthLabel.Right;
			connectionDt.Width = 48;

			frameRateLabel.Left = connectionDt.Right;
			frameRateLabel.Top = frameRate.Bottom;
			frameRateLabel.Width = 64;

			connectionTreshold.Top = frameRate.Bottom;
			connectionTreshold.Left = frameRateLabel.Right;
			connectionTreshold.Width = 64;

			cellDrawMode.Top = frameRate.Bottom;
			cellDrawMode.Left = connectionTreshold.Right;

			saveConnectionImageButton.Top = frameRate.Bottom;
			saveConnectionImageButton.Left = cellDrawMode.Right;

			mapConnections.Left = connectionGroup.DisplayRectangle.Left;
			mapConnections.Top = frameRateLabel.Bottom;
			mapConnections.Height = connectionGroup.DisplayRectangle.Bottom - frameRateLabel.Bottom;
			mapConnections.Width = connectionGroup.DisplayRectangle.Width;
		}

		protected override void OnClosed (EventArgs e)
		{
			base.OnClosed (e);
			mainWindow.Quit();
		}

		public void SetTutorialControls (ref Control[] buttons, ref List<Control>[] controls) {
			buttons [5] = pulseThreshold;
			buttons [6] = cellsListBox;
			controls [5] = new List<Control> ();
			controls [5].Add (pulseThreshold);
			controls [6] = new List<Control> ();
			controls [6].Add (cellsListBox);
			controls [7] = new List<Control> ();
			controls [7].Add (settingsGroup);
		}
	}
}

