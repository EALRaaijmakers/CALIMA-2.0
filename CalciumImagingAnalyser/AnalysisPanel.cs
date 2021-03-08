//
//  AnalysisPanel.cs
//
//  Author:
//       F.D.W. Radstake <>
//	     E.A.L. Raaijmakers <>
//
//  Copyright (c) 2017-2021, Eindhoven University of Technology
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
using System.Collections.Generic;
using System.IO;

namespace CalciumImagingAnalyser
{
	public class AnalysisPanel:GroupBox
	{
		MainWindow mainWindow;
		public ListBox cellsListBox { get; private set; }
		PictureBox graphsBox;
		ImagePanel imagePanel;
		Button exportCSVButton, getDataButton, deleteCellsButton;
		public Button analysisStageButton { get; private set; }
		ComboBox analysisMode;

		NumericUpDown numNormWindowLength, numNormWindowPercentile;

		Label lblNormWindowLength, lblNormWindowPercentile;

		public Size graphsImageSize { get { return new Size (graphsBox.Width, graphsBox.Height); } }

		public AnalysisPanel (MainWindow mainWindow, ImagePanel imagePanel)
		{
			this.mainWindow = mainWindow;
			this.imagePanel = imagePanel;
			Text = "Analysis";

			Rectangle windowRectangle = this.DisplayRectangle;

			cellsListBox = new ListBox ();
			Controls.Add (cellsListBox);
			cellsListBox.SelectionMode = SelectionMode.MultiExtended;
			cellsListBox.SelectedIndexChanged += delegate {
				this.mainWindow.imageMatrixCollection.SelectCells ();
			};
			cellsListBox.KeyDown += CellsListBoxKeyDown;

			analysisMode = new ComboBox ();
			analysisMode.Items.Add (CellMeasuringMode.Average);
			analysisMode.Items.Add (CellMeasuringMode.Maximum);
			analysisMode.SelectedIndex = 0;
			Controls.Add (analysisMode);

			lblNormWindowLength = new Label();
			lblNormWindowLength.Text = "Window length:";
			Controls.Add(lblNormWindowLength);

			lblNormWindowPercentile = new Label();
			lblNormWindowPercentile.Text = "F0 percentile:";
			Controls.Add(lblNormWindowPercentile);

			numNormWindowLength = new NumericUpDown();
			numNormWindowLength.Minimum = 1;
			numNormWindowLength.Maximum = 1000000;
			numNormWindowLength.DecimalPlaces = 0;
			numNormWindowLength.Increment = 1;
			numNormWindowLength.Value = 10;
			Controls.Add(numNormWindowLength);

			numNormWindowPercentile = new NumericUpDown();
			numNormWindowPercentile.Minimum = 1;
			numNormWindowPercentile.Maximum = 100;
			numNormWindowPercentile.DecimalPlaces = 0;
			numNormWindowPercentile.Increment = 5;
			numNormWindowPercentile.Value = 10;
			Controls.Add(numNormWindowPercentile);

			getDataButton = new Button ();
			getDataButton.Text = "Record cell activity";
			getDataButton.Click += delegate {
				if (mainWindow.imageMatrixCollection.GetCellValues ((CellMeasuringMode)analysisMode.SelectedItem)){
					this.mainWindow.imageMatrixCollection.SelectCells ();//draw the graphs right away
					analysisStageButton.Enabled = true;
				}
			};
			Controls.Add (getDataButton);

			SaveFileDialog saveCSVDialog = new SaveFileDialog ();
			saveCSVDialog.DefaultExt = "csv";
			saveCSVDialog.AddExtension = true;
			saveCSVDialog.Filter = "Comma-Separated Values|*.csv";
			saveCSVDialog.InitialDirectory = Directory.GetCurrentDirectory ();

			exportCSVButton = new Button ();
			exportCSVButton.Text = "Export to .csv";
			exportCSVButton.Click += delegate {
				string data = mainWindow.imageMatrixCollection.GetCSVData(CellActivityContainer.CSVExportMode.Raw);
				if (data == null) {
					MainWindow.ShowMessage ("Error: please record cell activity first, and select at least one cell.");
				} else {
					DialogResult result = saveCSVDialog.ShowDialog(); // Show the dialog.
					if (result == DialogResult.OK) // Test result.
					{
						string filename = saveCSVDialog.FileName;
						try
						{
							System.IO.File.WriteAllText(filename,data);
							MainWindow.ShowMessage("Saved CSV data to " + filename + ".");						
						} catch (IOException e) {
							MainWindow.ShowMessage("Error saving file: " + e.Message);
						}
					}
				}
			};
			Controls.Add (exportCSVButton);

			deleteCellsButton = new Button ();
			deleteCellsButton.Text = "Delete selected ROIs";
			deleteCellsButton.Click += delegate {
				mainWindow.imageMatrixCollection.DeleteCells (cellsListBox.SelectedIndices);
			};
			Controls.Add (deleteCellsButton);

			analysisStageButton = new Button ();
			analysisStageButton.Text = "Go to Analysis stage";
			analysisStageButton.Enabled = false;
			analysisStageButton.Click += delegate {
				mainWindow.SetStageAnalysis ();
			};


			Controls.Add (analysisStageButton);

			graphsBox = new PictureBox ();
			graphsBox.Image = Image.FromFile ("graphs.png");
			Controls.Add (graphsBox);
			graphsBox.SizeMode = PictureBoxSizeMode.Zoom;

			// Set up the ToolTip text for the various controls.
			mainWindow.ToolTip.SetToolTip(analysisMode, "Average: takes the average pixel value of the cell. More susceptible to spatial differences in the cell.\n" +
				"Maximum: takes the brightest pixel value of the cell. More susceptible to noise and overexposure.");



			//resize all controls
			Resize ();
		}

		public void SetGraph (Bitmap bmp) {
			graphsBox.Image = bmp;
		}

		public void SetFocus() {
			cellsListBox.Focus ();
		}

		public new void Resize () {
			Rectangle screenRectangle = mainWindow.DisplayRectangle;
			Left = imagePanel.Right;
			Width = screenRectangle.Width - imagePanel.Width;
			Height = imagePanel.Height;
			Top = imagePanel.Top;

			Rectangle windowRectangle = this.DisplayRectangle;

			cellsListBox.Width = 128;
			cellsListBox.Left = windowRectangle.Right - cellsListBox.Width;
			cellsListBox.Height = windowRectangle.Height;
			cellsListBox.Top = windowRectangle.Top;

			analysisMode.Left = windowRectangle.Left;
			analysisMode.Width = windowRectangle.Width - cellsListBox.Width;
			analysisMode.Top = windowRectangle.Top;

			lblNormWindowLength.Left = analysisMode.Left;
			lblNormWindowLength.Top = analysisMode.Bottom + 10;
			lblNormWindowLength.Width = analysisMode.Width;
			lblNormWindowLength.Height = analysisMode.Height;

			numNormWindowLength.Left = analysisMode.Left;
			numNormWindowLength.Width = analysisMode.Width;
			numNormWindowLength.Top = lblNormWindowLength.Bottom;

			lblNormWindowPercentile.Left = analysisMode.Left;
			lblNormWindowPercentile.Width = analysisMode.Width;
			lblNormWindowPercentile.Top = numNormWindowLength.Bottom + 10;
			lblNormWindowPercentile.Height = analysisMode.Height;

			numNormWindowPercentile.Left = analysisMode.Left;
			numNormWindowPercentile.Width = analysisMode.Width;
			numNormWindowPercentile.Top = lblNormWindowPercentile.Bottom;

			getDataButton.Left = analysisMode.Left;
			getDataButton.Top = numNormWindowPercentile.Bottom + 10;
			getDataButton.Width = analysisMode.Width;

			analysisStageButton.Left = windowRectangle.Left;
			analysisStageButton.Top = windowRectangle.Bottom - analysisStageButton.Height;
			analysisStageButton.Width = windowRectangle.Width - cellsListBox.Width;

			exportCSVButton.Left = windowRectangle.Left;
			exportCSVButton.Top = analysisStageButton.Top - exportCSVButton.Height;
			exportCSVButton.Width = windowRectangle.Width - cellsListBox.Width;

			deleteCellsButton.Left = windowRectangle.Left;
			deleteCellsButton.Top = exportCSVButton.Top - deleteCellsButton.Height;
			deleteCellsButton.Width = windowRectangle.Width - cellsListBox.Width;

			graphsBox.Left = windowRectangle.Left;
			graphsBox.Top = getDataButton.Bottom;
			graphsBox.Height = windowRectangle.Height - exportCSVButton.Height - analysisMode.Height - getDataButton.Height - deleteCellsButton.Height;
			graphsBox.Width = analysisMode.Width;
		}

		public void CellsListBoxKeyDown(Object o, KeyEventArgs e) {
			if (e.KeyCode == Keys.Delete) {
				this.mainWindow.imageMatrixCollection.DeleteCell (cellsListBox.SelectedIndex);
			}
		}

		public void SetTutorialControls (ref Control[] buttons, ref List<Control>[] controls) {
			buttons [3] = getDataButton;
			buttons [4] = analysisStageButton;
			controls [3] = new List<Control> ();
			controls [3].Add (getDataButton);
			controls [3].Add (analysisMode);
			controls [4] = new List<Control> ();
			controls [4].Add (exportCSVButton);
			controls [4].Add (analysisStageButton);
		}

		public int GetNormWindowLength() {
			return (int)numNormWindowLength.Value;
		}

		public int GetNormWindowPercentile() {
			return (int)numNormWindowPercentile.Value;
		}
	}
}

