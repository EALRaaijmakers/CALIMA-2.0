//
//  CellActivityContainer.cs
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using csDelaunay;

namespace CalciumImagingAnalyser {
	/// <summary>
	/// CellActivityContainer contains all information about the cell activity, and does calculations on them such as drawing maps.
	/// </summary>
	public class CellActivityContainer {
		public enum ROIDisplayMode { Hide, Outline, Dot };
		public enum CSVExportMode { Raw, ROC, FF0, Peak };
		private ImageMatrixCollection imageMatrixCollection;
		private float[][] cellActivity;
		private float[][] celldFF0;
		private float[][] celldFF0Normalised;
		private float[,] cellPositiveROCAreaNormalised;
		private float[][] cellPeak;
		private int[][] cellPeakFrames;
		private float min, max;
		private float minROC, maxROC;
		private float yscale;
		private float yscaleROC;
		private float[] cellMaxima;
		private float[] cellMinima;
		private float[] frameAverage;
		private float[] cellAverage;
		private float[] cellMaxROC;
		private int lag;
		public List<BrainCell> allCells { get; set; }

		//		public Dictionary<BrainCell,float> cellMaxDADt {get; private set;}

		private Dictionary<BrainCell, int> cellIndices;

		private Pen lightGreyPen, darkGreyPen, redPen, lightRedPen, greenPen, lightGreenPen;
		private SolidBrush greenBrush, redBrush;
		private Font font;

		private int nCells, nFrames;

		private bool isFilled = false;

		public CellActivityContainer(ImageMatrixCollection imageMatrixCollection) {
			this.imageMatrixCollection = imageMatrixCollection;
			lightGreyPen = new Pen(Color.LightGray);
			darkGreyPen = new Pen(Color.DarkGray);
			redPen = new Pen(Color.Red);
			lightRedPen = new Pen(Color.LightPink);
			greenPen = new Pen(Color.Green);
			lightGreenPen = new Pen(Color.LightGreen);
			greenBrush = new SolidBrush(Color.FromArgb(96, 255, 96));
			redBrush = new SolidBrush(Color.FromArgb(255, 0, 0));
			font = new Font("Arial", 10f);
			allCells = new List<BrainCell>();
		}

		public void Reset() {
			isFilled = false;
			cellPeakFrames = null;
			cellActivity = null;
			nCells = 0;
			nFrames = 0;
		}

		public int[] GetPeakFrames(int cellIndex) {
			return cellPeakFrames[cellIndex];
		}

		public bool HasOverlap(int index, int[] peakFrames) {
			foreach (int i in peakFrames) {
				foreach (int j in cellPeakFrames[index]) {
					if (i == j) {
						return true;
					}
				}
			}
			return false;
		}

		public float MaxCellActivity(BrainCell cell) {
			return cellMaxROC[cellIndices[cell]];
		}

		public void FillContainer(List<ImageMatrix> images, List<BrainCell> allCells, CellMeasuringMode colourMode, int normWindowLength, int normWindowPercentile) {

			this.allCells = allCells;
			nCells = allCells.Count;
			nFrames = images.Count;
			cellActivity = new float[nCells][];//, nFrames];
			celldFF0 = new float[nCells][];//nFrames];
			celldFF0Normalised = new float[nCells][];//, nFrames];
			cellPositiveROCAreaNormalised = new float[nCells, nFrames];
			frameAverage = new float[nFrames];
			//			cellMaxDADt = new Dictionary<BrainCell, float> ();
			cellMaxima = new float[nCells];
			cellMinima = new float[nCells];
			cellMaxROC = new float[nCells];
			cellIndices = new Dictionary<BrainCell, int>();
			cellAverage = new float[nCells];
			min = float.MaxValue;
			max = float.MinValue;
			minROC = float.MaxValue;
			maxROC = float.MinValue;
			MainWindow.SetProgressBar(0);

			yscale = 0;
			yscaleROC = 0;

			int SlidingFilterSize = normWindowLength;//MainWindow.GetNormWindowLength();

			//Estimate background activity Fmin
			//create a vector of a certain size
			int numofsmallvals = (int)Math.Ceiling((float)0.01 * images[0].height * images[0].width);
			float[] Fminvector = new float[numofsmallvals];
			float Fminvectormax = float.MaxValue;
			int Fminmaxindex = 0;
			for (int j = 0; j < numofsmallvals; j++) {
				Fminvector[j] = float.MaxValue;
			}

			for (int h = 0; h < images[0].height; h++) {
				for (int w = 0; w < images[0].width; w++) {
					if (images[0].data[w, h] < Fminvectormax) {
						Fminvector[Fminmaxindex] = images[0].data[w, h];
						Fminvectormax = Fminvector.Max();
						//find index
						for (int p = 0; p < numofsmallvals; p++)
							if (Fminvector[p] == Fminvectormax) { Fminmaxindex = p; }
					}

				}
			}

			float Fmin = Utils.Mean(Fminvector, 0, numofsmallvals);

			//now set the activity.
			for (int c = 0; c < nCells; c++) {
				float cMin = float.MaxValue;
				float cMax = float.MinValue;
				float cMinROC = float.MaxValue;
				float cMaxROC = float.MinValue;
				//float dadtMax = float.MinValue;
				float normSignalMax = float.MinValue;

				//float previousVal = allCells [c].GetCellValue (images [0].Bitmap,colourMode);
				celldFF0[c] = new float[nFrames];
				celldFF0Normalised[c] = new float[nFrames];
				cellActivity[c] = new float[nFrames];
				cellIndices.Add(allCells[c], c);

				float[] SignalWindow = new float[images.Count];
				float slidingF0 = 0;

				for (int im = 0; im < images.Count; im++) {
					SignalWindow[im] = allCells[c].GetCellValue(images[im].Bitmap, colourMode);
				}

				for (int im = 0; im < images.Count; im++) {

					//Delta F/F0 normalization

					List<float> WindowArray = new List<float>(new float[SlidingFilterSize]); // 
					int minvalnumber = (int)Math.Ceiling(SlidingFilterSize * ((float)normWindowPercentile / 100));// 
					float[] MinValArray = new float[minvalnumber]; // 

					//write values in the current window to WindowArray list
					for (int filterI = 0; filterI < SlidingFilterSize; filterI++) {
						WindowArray[filterI] = SignalWindow[((im < SlidingFilterSize) ? filterI : im - filterI)];
					}
					WindowArray.Sort();

					for (int arraycnt = 0; arraycnt < minvalnumber; arraycnt++) {
						slidingF0 += WindowArray[arraycnt];
					}
					slidingF0 = slidingF0 / (float)minvalnumber;

					float val = allCells[c].GetCellValue(images[im].Bitmap, colourMode);
					if (val == 0) { val = float.Epsilon; }  //Prevent bugs for val = 0;
															//calculate rate of change
															//float dadt = val; //val - previousVal;

					//previousVal = val;
					//write raw data to containers
					cellActivity[c][im] = val;
					//cellROC [c][im] = dadt;
					float normSignal;
					if ((val >= slidingF0) && (slidingF0 > Fmin)) {
						normSignal = (val - slidingF0) / (slidingF0 - Fmin);
					} //else if (val < slidingF0) { normSignal = float.Epsilon;} 
					else { normSignal = float.Epsilon; }
					celldFF0[c][im] = normSignal;

					//calculate minimum and maximum values
					//Utils.SetMax(ref dadtMax, Math.Abs(dadt));
					Utils.SetMax(ref normSignalMax, Math.Abs(normSignal));
					//previousVal = val;
					//set absolute minimum and maximum values
					Utils.SetMin(ref min, val);
					Utils.SetMax(ref max, val);
					//Utils.SetMax(ref maxROC, dadt);
					//Utils.SetMin(ref minROC, dadt);
					Utils.SetMax(ref maxROC, normSignal);
					Utils.SetMin(ref minROC, normSignal);
					//set per-cell minimum and maximum values
					Utils.SetMin(ref cMin, val);
					Utils.SetMax(ref cMax, val);
					//Utils.SetMax(ref cMaxROC, dadt);
					//Utils.SetMin(ref cMinROC, dadt);
					Utils.SetMax(ref cMaxROC, normSignal);
					Utils.SetMin(ref cMinROC, normSignal);

					frameAverage[im] += val;
					cellAverage[c] += val;
				}
				cellAverage[c] /= (float)images.Count;
				//cMin and cMax now contain the min and max values of this cell. If it's the largest difference, save it to yscale
				Utils.SetMax(ref yscale, cMax - cMin);
				if (cMaxROC > Math.Abs(cMinROC)) {
					Utils.SetMax(ref yscaleROC, 2f * cMaxROC);
				} else {
					Utils.SetMax(ref yscaleROC, 2f * Math.Abs(cMinROC));
				}
				if (cMax - cMin > yscale) {
					yscale = cMax - cMin;
				}
				cellMaxROC[c] = Math.Max(cMaxROC, Math.Abs(cMinROC));

				//		cellMaxDADt.Add (allCells [c], dadtMax);
				cellMinima[c] = cMin;
				cellMaxima[c] = cMax;
			}
			//calculate normalised ROC
			float totalPositiveROC = 0f;
			for (int c = 0; c < nCells; c++) {
				for (int im = 0; im < images.Count; im++) {
					celldFF0Normalised[c][im] = celldFF0[c][im] / cellMaxROC[c];
					float a = Math.Max(0f, celldFF0[c][im]);
					cellPositiveROCAreaNormalised[c, im] = a;
					totalPositiveROC += a;
				}
			}
			for (int c = 0; c < nCells; c++) {
				for (int im = 0; im < images.Count; im++) {
					cellPositiveROCAreaNormalised[c, im] /= totalPositiveROC;
				}
			}

			isFilled = true;
		}

		//Calculate which frames contain spikes
		//Calculate which frames contain spikes
		public void CalculatePulseFrames(int lag, float treshold, float influence, int multiPeak) {
			cellPeakFrames = new int[nCells][];
			cellPeak = new float[nCells][];
			if (lag > nFrames) {
				MainWindow.ShowMessage("Error: too few frames to calculate connections for lag=" + lag.ToString() + ".");
				//initialize the vectors to avoid errors
				for (int c = 0; c < nCells; c++) {
					cellPeakFrames[c] = new int[nFrames];
					cellPeak[c] = new float[nFrames];
				}
				return;
			}

			cellPeakFrames = new int[nCells][];
			cellPeak = new float[nCells][];

			this.lag = lag;

			//peakTreshold *= 255f;//full range of the colour

			for (int c = 0; c < nCells; c++) {
				float[] celldff0 = new float[nFrames];
				cellPeak[c] = new float[nFrames];
				for (int f = 0; f < nFrames; f++) {
					celldff0[f] = celldFF0[c][f];
				}
				//
				int[] peaks = Utils.GetCellPeaks(celldff0, lag, treshold, influence);
				List<int> fpeaks = new List<int>();
				if (multiPeak == 0) { //detect first frame of activity > Zth
					if (peaks[0] > 0) {
						fpeaks.Add(0);
						cellPeak[c][0] = 1f;
					} else {
						cellPeak[c][0] = 0f;
					}
					for (int f = 1; f < nFrames; f++) {
						//avoid long pulses (>1 frames) being seen as several pulses
						if (peaks[f] > 0 && peaks[f - 1] <= 0) {
							fpeaks.Add(f);
							cellPeak[c][f] = 1f;
						} else {
							cellPeak[c][f] = 0f;
						}
					}
				} else if (multiPeak == 1) { //detect all frames of activity > Zth
					for (int f = 0; f < nFrames; f++) {
						cellPeak[c][f] = peaks[f];
						if (peaks[f] > 0) {
							fpeaks.Add(f);
						}
					}
				} else { //detect peak of activity within block
					int f = 0;
					while (f < nFrames) { //step through frames

						if (peaks[f] > 0) {
							float maxpeak = 0;
							int framemaxpeak = 0;
							while (peaks[f] > 0) {
								if (celldff0[f] > maxpeak) {
									maxpeak = celldff0[f];
									framemaxpeak = f;
								}
								cellPeak[c][f] = 0f;
								f++;
								if (f >= nFrames) { break; }
							}
							fpeaks.Add(framemaxpeak);
							cellPeak[c][framemaxpeak] = 1f;
						} else { f++; }

					}
				}

				cellPeakFrames[c] = fpeaks.ToArray();
			}
		}

		public string GetConnectionData(int imagewidth, int imageheight, List<BrainCell> brainCells, float maxDistance, int maxDt, float threshold) {
			float distanceTreshold = maxDistance * (float)Math.Sqrt(imagewidth * imagewidth + imageheight * imageheight);
			string ans = "Cell1, Cell2, ROI1 X Coordinate, ROI1 Y Coordinate, ROI2 X Coordinate, ROI2 Y Coordinate, Connection" + Environment.NewLine;

			for (int c = 0; c < brainCells.Count; c++) {
				//we only need to show one arrow for each pair
				for (int d = c + 1; d < brainCells.Count; d++) {
					BrainCell cell1 = brainCells[c];
					BrainCell cell2 = brainCells[d];
					int c1ind = cellIndices[cell1];
					int c2ind = cellIndices[cell2];

					//first calculate distance condition
					if (Utils.GetDistance(cell1.x, cell1.y, cell2.x, cell2.y) < distanceTreshold) {
						//calculate similarity between the two cells for various dt
						float[] similarities = new float[2 * maxDt + 1];
						for (int f = -maxDt; f <= maxDt; f++) {
							//float s = GetSimilarity(f, cellPeak[c1ind], cellPeak[c2ind],SimilarityMeasure.InnerProduct);
							float s = Utils.GetXCorrelation(cellPeak[c1ind], cellPeak[c2ind], f, true);
							similarities[f + maxDt] = s;
						}

						//no need to compute further if not similar
						float max = Utils.GetMax(similarities);
						if (max <= threshold) {
							continue;
						}

						//add caps depending on direction of communication
						string dir1 = "-";
						for (int f = -maxDt; f < 0; f++) {
							if (similarities[f + maxDt] == max) {
								dir1 = "<";
							}
						}
						string dir2 = "-";
						for (int f = 1; f <= maxDt; f++) {
							if (similarities[f + maxDt] == max) {
								dir2 = ">";
							}
						}
						ans += brainCells[c].Name + "," + brainCells[d].Name + "," + brainCells[c].x + ";" + brainCells[c].y +
							"," + brainCells[d].x + ";" + brainCells[d].y + ",";

						ans += dir1 + dir2;

						ans += Environment.NewLine;
					}
				}
			}
			return ans;
		}

		public string GetBinaryAdjacencyData(int imagewidth, int imageheight, List<BrainCell> brainCells, float maxDistance, int maxDt, float threshold) {
			float distanceTreshold = maxDistance * (float)Math.Sqrt(imagewidth * imagewidth + imageheight * imageheight);
			string ans = "";

			for (int c = 0; c < brainCells.Count; c++) {
				ans += "," + brainCells[c].Name;
			}
			ans += Environment.NewLine;
			for (int c = 0; c < brainCells.Count; c++) {
				BrainCell cell1 = brainCells[c];
				int c1ind = cellIndices[cell1];
				//row header
				ans += cell1.Name;
				for (int d = 0; d < brainCells.Count; d++) {
					BrainCell cell2 = brainCells[d];
					int c2ind = cellIndices[cell2];

					if (c == d) {
						ans += ",0";
						continue;
					}

					//first calculate distance condition
					if (Utils.GetDistance(cell1.x, cell1.y, cell2.x, cell2.y) < distanceTreshold) {
						//calculate similarity between the two cells for various dt
						float[] similarities = new float[2 * maxDt + 1];
						for (int f = -maxDt; f <= maxDt; f++) {
							//float s = GetSimilarity(f, cellPeak[c1ind], cellPeak[c2ind],SimilarityMeasure.InnerProduct);
							float s = Utils.GetXCorrelation(cellPeak[c1ind], cellPeak[c2ind], f, true);
							similarities[f + maxDt] = s;
						}
						float max = Utils.GetMax(similarities);
						if (max <= threshold) {
							ans += ",0";
						} else {
							ans += ",1";
						}
					} else {
						ans += ",0";
					}
				}
				ans += Environment.NewLine;
			}
			return ans;
		}


		public string GetXCorrData(List<BrainCell> brainCells, SimilaritySource similaritySource) {
			string ans = "";

			for (int c = 0; c < brainCells.Count; c++) {
				ans += "," + brainCells[c].Name;
			}
			ans += Environment.NewLine;
			float[][] source = null;
			switch (similaritySource) {
				case SimilaritySource.Peak:
					source = cellPeak;
					break;
				//case SimilaritySource.RateOfChange:
				//source = cellROCNormalised;
				//break;
				case SimilaritySource.Activity:
					source = cellActivity;
					break;
			}

			for (int c = 0; c < brainCells.Count; c++) {
				BrainCell cell1 = brainCells[c];
				int c1ind = cellIndices[cell1];
				//row header
				ans += cell1.Name;
				for (int d = 0; d < brainCells.Count; d++) {
					BrainCell cell2 = brainCells[d];
					int c2ind = cellIndices[cell2];

					float[] set1 = source[c1ind], set2 = source[c2ind];
					float s = Utils.GetXCorrelation(set1, set2, 0, true);
					ans += "," + s.ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				ans += Environment.NewLine;
			}
			return ans;
		}


		/// <summary>
		/// Gets a string containing comma-separated values for all selected cells and frames.
		/// </summary>
		/// <returns>The CSV data.</returns>
		/// <param name="brainCells">The brain cells to save.</param>
		public string GetCSVData(List<BrainCell> brainCells, CSVExportMode exportMode) {
			if (!isFilled) {
				MainWindow.ShowMessage("Error: please record cell activity first.");
				return null;
			}
			if (cellActivity == null) {
				MainWindow.ShowMessage("Error: please record cell activity first.");
				return null;
			} else {
				string data = "Frame";
				for (int f = 0; f < brainCells.Count; f++) {
					data += "," + brainCells[f].Name;
				}
				data += Environment.NewLine;

				switch (exportMode) {
					case CSVExportMode.Raw:
						for (int frame = 0; frame < nFrames; frame++) {
							data += frame.ToString();
							for (int c = 0; c < brainCells.Count; c++) {
								data += "," + cellActivity[cellIndices[brainCells[c]]][frame].ToString(System.Globalization.CultureInfo.InvariantCulture);
							}
							data += Environment.NewLine;
						}
						break;
					case CSVExportMode.Peak:
						if (cellPeak == null) {
							MainWindow.ShowMessage("Error: please run peak detection first.");
							return "";
						}
						for (int frame = 0; frame < nFrames; frame++) {
							data += frame.ToString();
							for (int c = 0; c < brainCells.Count; c++) {
								data += "," + ((int)cellPeak[cellIndices[brainCells[c]]][frame]).ToString();
							}
							data += Environment.NewLine;
						}
						break;
					case CSVExportMode.FF0:
						for (int frame = 0; frame < nFrames; frame++) {
							data += frame.ToString();
							for (int c = 0; c < brainCells.Count; c++) {
								float f0 = cellAverage[cellIndices[brainCells[c]]];
								if (Math.Abs(f0) <= float.Epsilon) {
									f0 = 1;//don't crash on pure black HERE2 celldFF0[c][im] = normSignal
								}
								data += "," + (celldFF0[cellIndices[brainCells[c]]][frame] / f0).ToString(System.Globalization.CultureInfo.InvariantCulture);
							}
							data += Environment.NewLine;
						}
						break;
				}
				return data;
			}
		}

		#region Graphs

		/// <summary>
		/// Gets the Bitmap containing graphs of all provided cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="cells">The indexes of the cells to draw graphs for.</param>
		/// <param name="brainCells">The cells to draw graphs for.</param>
		/// <param name="width">The desired width of the bitmap.</param>
		/// <param name="height">The desired height of the bitmap.</param>
		public Bitmap GetGraph(List<BrainCell> brainCells, int width, int height, GraphType type, bool commonScale, bool showPeaks, int frame = -1) {
			const int cellHeight = 64;
			int xAxisHeight = System.Windows.Forms.TextRenderer.MeasureText("0", font).Height;
			if (height == int.MaxValue) {//make room for all cells
				height = brainCells.Count * cellHeight + xAxisHeight;
			}
			Bitmap bmp = new Bitmap(width, height);

			//first draw a white background
			Graphics g = Graphics.FromImage(bmp);
			g.Clear(Color.White);

			if (!isFilled) {
				g.Dispose();
				return bmp;
			}

			//calculate vertical line frames
			int vLineFrames = nFrames / 7;
			float xScale = width / (float)nFrames;
			float frameWidth = Math.Max(1f, xScale);//ensure a width of at least 1px, as otherwise it may not display

			//calculate the number of cells to draw (in case it doesn't fit)
			int nDrawnCells = brainCells.Count;
			if (brainCells.Count * cellHeight > height) {
				//keep a 10px margin for the x-axis label
				nDrawnCells = (height - 10) / cellHeight;
			}

			//draw the frame numbers
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Near;
			g.DrawString("0", font, new SolidBrush(Color.Gray), 0, nDrawnCells * cellHeight, sf);
			sf.Alignment = StringAlignment.Far;
			g.DrawString(nFrames.ToString(), font, new SolidBrush(Color.Gray), width, nDrawnCells * cellHeight, sf);
			sf.Alignment = StringAlignment.Center;

			//draw the vertical lines
			for (int v = 1; v < 8; v++) {
				int x = (int)(v * vLineFrames * xScale);
				g.DrawLine(lightGreyPen, x, 0, x, nDrawnCells * cellHeight);
			}
			//then draw the x axis every 2 frames
			for (int v = 2; v < 8; v += 2) {
				int x = (int)(v * vLineFrames * xScale);
				g.DrawString((v * vLineFrames).ToString(), font, new SolidBrush(Color.Gray), x, nDrawnCells * cellHeight, sf);
			}

			//if available, draw the active frames
			if (showPeaks) {
				if (cellPeakFrames != null) {
					SolidBrush greenTransBrush = new SolidBrush(Color.FromArgb(255 / (nDrawnCells + 1), 0, 255, 0));
					//red brush to show the first frames that are less accurate for peak detection
					SolidBrush transYellow = new SolidBrush(Color.FromArgb(64, 255, 255, 0));
					//first draw the area without frames
					g.FillRectangle(transYellow, 0, 0, lag * xScale, nDrawnCells * cellHeight);
					//then draw the peak frames
					foreach (BrainCell c in brainCells) {
						foreach (int x in cellPeakFrames[cellIndices[c]]) {
							//g.DrawLine (lightGreenPen, x * xScale, 0, x * xScale, brainCells.Count * cellHeight);
							g.FillRectangle(greenTransBrush, x * xScale, 0, frameWidth, nDrawnCells * cellHeight);
						}
					}
				}
			}

			//if we have to draw the current frame, do so
			if (frame != -1) {
				//The line is one pixel to the right of where we want it, so correct that.
				g.DrawLine(darkGreyPen, frame * xScale - 1, 0, frame * xScale - 1, nDrawnCells * cellHeight);
			}

			//then draw all the graphs
			for (int c = 0; c < nDrawnCells; c++) {
				string name;
				//ensure the different name for ValidationCells gets used.
				if (brainCells[c] is ValidationCell) {
					name = ((ValidationCell)brainCells[c]).Name;
				} else {
					name = brainCells[c].Name;
				}
				switch (type) {
					case GraphType.Activity:
						//Graphs.DrawGraphOnBitmap
						DrawGraphOnBitmap(brainCells[c], name, width, cellHeight, c * cellHeight, g, Color.Blue, frame, commonScale, showPeaks);
						break;
						//case GraphType.RateOfChange:
						//DrawROCGraphOnBitmap (brainCells [c], name, width, cellHeight, c * cellHeight, g, Color.Blue, frame, commonScale, showPeaks);
						//break;
				}
			}

			// Dispose of the Graphics object
			g.Dispose();
			return bmp;
		}

		public Bitmap GetConnectionMap(int width, int height, int imagewidth, int imageheight, List<BrainCell> cells, List<BrainCell> selectedCells, float maxDistance, int maxDt, float threshold, bool showLabels, ROIDisplayMode roiDisplayMode, Image background = null) {
			float ratio = width / (float)height;
			float imageratio = imagewidth / (float)imageheight;

			if (ratio > imageratio) {//target image is wider
				width = (int)(height * imageratio);
			} else {//target image is taller
				height = (int)(width / imageratio);
			}

			Bitmap bmp = new Bitmap(width, height);

			//first draw a white background
			Graphics gfx = Graphics.FromImage(bmp);
			gfx.Clear(Color.White);

			if (!isFilled || cellPeakFrames == null) {
				gfx.Dispose();
				return bmp;
			}

			if (background != null) {
				gfx.DrawImage(background, new RectangleF(0f, 0f, width, height));
			}

			//calculate distance treshold = [0..1] times the diagonal length
			float distanceTreshold = maxDistance * (float)Math.Sqrt(imagewidth * imagewidth + imageheight * imageheight);

			float xScale = width / (float)imagewidth;
			float yScale = height / (float)imageheight;

			const float cellSize = 10f;

			//draw lines between connected cells
			for (int c = 0; c < cells.Count; c++) {
				//we only need to show one arrow for each pair
				for (int d = c + 1; d < cells.Count; d++) {
					BrainCell cell1 = cells[c];
					BrainCell cell2 = cells[d];
					int c1ind = cellIndices[cell1];
					int c2ind = cellIndices[cell2];

					//first calculate distance condition
					if (Utils.GetDistance(cell1.x, cell1.y, cell2.x, cell2.y) < distanceTreshold) {
						//calculate similarity between the two cells for various dt
						float[] similarities = new float[2 * maxDt + 1];
						for (int f = -maxDt; f <= maxDt; f++) {
							//float s = GetSimilarity(f, cellPeak[c1ind], cellPeak[c2ind],SimilarityMeasure.InnerProduct);
							float s = Utils.GetXCorrelation(cellPeak[c1ind], cellPeak[c2ind], f, true);
							similarities[f + maxDt] = s;
						}

						//no need to compute further if not similar
						float max = Utils.GetMax(similarities);
						if (max <= 0) {
							continue;
						}

						int val = (byte)(max * 255f);
						Pen arrowPen = new Pen(Color.FromArgb(val, 255, 0, 0));//new Pen (Color.FromArgb (255,255,0,0));
						arrowPen.Width = 2f;// val;

						//add caps depending on direction of communication
						for (int f = -maxDt; f < 0; f++) {
							if (similarities[f + maxDt] == max) {
								arrowPen.CustomEndCap = new AdjustableArrowCap(2, 2, true);
							}
						}
						for (int f = 1; f <= maxDt; f++) {
							if (similarities[f + maxDt] == max) {
								arrowPen.CustomStartCap = new AdjustableArrowCap(2, 2, true);
							}
						}

						if (Utils.GetMax(similarities) > threshold) {
							PointF a = Utils.GetCircleLineIntersection(cell1.x * xScale, cell1.y * yScale, cell2.x * xScale, cell2.y * yScale, cellSize / 2f);
							PointF b = Utils.GetCircleLineIntersection(cell2.x * xScale, cell2.y * yScale, cell1.x * xScale, cell1.y * yScale, cellSize / 2f);
							gfx.DrawLine(arrowPen, a, b);
							//		gfx.DrawLine (arrowPen, cell1.x * xScale, cell1.y * yScale, cell2.x * xScale, cell2.y * yScale);
						}
					}
				}
			}

			SolidBrush cellBrush = new SolidBrush(Color.FromArgb(192, 0, 0, 255));
			SolidBrush selectedCellBrush = new SolidBrush(Color.FromArgb(192, 255, 32, 0));

			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			sf.LineAlignment = StringAlignment.Far;

			//if wanted, draw cell names
			if (showLabels) {
				foreach (BrainCell c in cells) {
					float x = c.x * xScale;
					float y = c.y * yScale - cellSize / 2f;
					if (c is ValidationCell) {
						gfx.DrawString(((ValidationCell)c).Name, font, cellBrush, x, y, sf);
					} else {
						gfx.DrawString(c.Name, font, cellBrush, x, y, sf);
					}
					c.DrawEdge(gfx, cellBrush, xScale, yScale);
				}
				foreach (BrainCell c in selectedCells) {
					float x = c.x * xScale;
					float y = c.y * yScale - cellSize / 2f;
					if (c is ValidationCell) {
						gfx.DrawString(((ValidationCell)c).Name, font, selectedCellBrush, x, y, sf);
					} else {
						gfx.DrawString(c.Name, font, selectedCellBrush, x, y, sf);
					}
					c.DrawEdge(gfx, selectedCellBrush, xScale, yScale);
				}
			}

			switch (roiDisplayMode) {
				case ROIDisplayMode.Dot:
					//draw cells
					foreach (BrainCell c in cells) {
						float x = c.x * xScale;
						float y = c.y * yScale;
						gfx.FillEllipse(cellBrush, x - cellSize / 2f, y - cellSize / 2f, cellSize, cellSize);
					}
					//draw the selected cells
					foreach (BrainCell c in selectedCells) {
						float x = c.x * xScale;
						float y = c.y * yScale;
						gfx.FillEllipse(selectedCellBrush, x - cellSize / 2f, y - cellSize / 2f, cellSize, cellSize);
					}
					break;
				case ROIDisplayMode.Outline:
					//draw cells
					foreach (BrainCell c in cells) {
						c.DrawEdge(gfx, cellBrush, xScale, yScale);
					}
					//draw the selected cells
					foreach (BrainCell c in selectedCells) {
						c.DrawEdge(gfx, selectedCellBrush, xScale, yScale);
					}
					break;
				case ROIDisplayMode.Hide:
					break;
			}

			// Dispose of the Graphics object
			gfx.Dispose();
			return bmp;
		}

		/// <summary>
		/// Gets a spatial-temporal activity map.
		/// </summary>
		/// <returns>A Bitmap containing the map.</returns>
		/// <param name="width">The width of the PictureBox container.</param>
		/// <param name="height">The height of the PictureBox container.</param>
		/// <param name="imagewidth">The width of the original image.</param>
		/// <param name="imageheight">The height of the original image.</param>
		/// <param name="background">An image to show on the background, null for black.</param>
		public Bitmap GetActivityMap(int width, int height, int imagewidth, int imageheight, float maxSize, bool showLabels,
			bool showLegend, ActivityDisplayMode mode, Image background = null) {
			float ratio = width / (float)height;
			float imageratio = imagewidth / (float)imageheight;

			if (ratio > imageratio) {//target image is wider
				width = (int)(height * imageratio);
			} else {//target image is taller
				height = (int)(width / imageratio);
			}

			Bitmap bmp = new Bitmap(width, height);

			//first draw a white background
			Graphics gfx = Graphics.FromImage(bmp);
			gfx.Clear(Color.Black);

			if (background != null) {
				gfx.DrawImage(background, new RectangleF(0f, 0f, width, height));
			}

			if (!isFilled || cellPeakFrames == null) {
				gfx.Dispose();
				return bmp;
			}
			float maxWidth = (float)Math.Sqrt(width * width + height * height) * maxSize;

			float xScale = width / (float)imagewidth;
			float yScale = height / (float)imageheight;

			int[] nPulse = new int[allCells.Count];

			float maxN = int.MinValue;

			for (int c = 0; c < allCells.Count; c++) {
				int p = cellPeakFrames[c].Length;
				//make sure two adjacent "pulses" are seen as one
				//			for (int i = 0; i < 10; i++) {
				//
				//			}
				nPulse[c] = p;
				if (p > maxN) {
					maxN = p;
				}
			}

			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			sf.LineAlignment = StringAlignment.Far;
			if (mode == ActivityDisplayMode.MinMaxAverage) {
				foreach (BrainCell c in allCells) {
					int ci = cellIndices[c];

					//no need to draw if cell not active
					if (nPulse[ci] > 0) {
						float radius = maxWidth * (nPulse[ci] / maxN) / 2f;

						float x = c.x * xScale - radius;
						float y = c.y * yScale - radius;

						float min, max, avg;
						Utils.GetMinMaxAverage(cellPeakFrames[ci], out min, out max, out avg);

						int ra, ga, ba,
							rb, gb, bb,
							rc, gc, bc;
						Utils.HsvToRgb(min * 240f / (float)nFrames, 1f, 1f, out ra, out ga, out ba);
						Utils.HsvToRgb(max * 240f / (float)nFrames, 1f, 1f, out rb, out gb, out bb);
						Utils.HsvToRgb(avg * 240f / (float)nFrames, 1f, 1f, out rc, out gc, out bc);

						SolidBrush brushmin = new SolidBrush(Color.FromArgb(128, ra, ga, ba));
						SolidBrush brushmax = new SolidBrush(Color.FromArgb(128, rb, gb, bb));
						SolidBrush brushavg = new SolidBrush(Color.FromArgb(128, rc, gc, bc));

						gfx.FillPie(brushmin, x, y, 2f * radius, 2f * radius, 270, 90);
						gfx.FillPie(brushmax, x, y, 2f * radius, 2f * radius, 180, 90);
						gfx.FillPie(brushavg, x, y, 2f * radius, 2f * radius, 0, 180);

						//gfx.FillEllipse (brush, x, y, 2f * radius, 2f * radius);
						if (showLabels) {
							if (c is ValidationCell) {
								gfx.DrawString(((ValidationCell)c).Name, font, brushavg, x, y, sf);
							} else {
								gfx.DrawString(c.Name, font, brushavg, x + radius, y, sf);
							}
						}
					}
				}
			} else {
				foreach (BrainCell c in allCells) {
					int ci = cellIndices[c];

					//no need to draw if cell not active
					if (nPulse[ci] > 0) {
						float radius = maxWidth * (nPulse[ci] / maxN) / 2f;

						float x = c.x * xScale - radius;
						float y = c.y * yScale - radius;

						float averageVal = Utils.GetAverage(cellPeakFrames[ci]);

						int r, g, b;
						Utils.HsvToRgb(averageVal * 240f / (float)nFrames, 1f, 1f, out r, out g, out b);

						SolidBrush brush = new SolidBrush(Color.FromArgb(128, r, g, b));

						gfx.FillEllipse(brush, x, y, 2f * radius, 2f * radius);
						if (showLabels) {
							if (c is ValidationCell) {
								gfx.DrawString(((ValidationCell)c).Name, font, brush, x, y, sf);
							} else {
								gfx.DrawString(c.Name, font, brush, x + radius, y, sf);
							}
						}
					}
				}
			}

			//draw legend
			if (showLegend) {
				//the color gradient
				for (int x = 0; x < 128; x++) {
					int r, g, b, xpos;
					Utils.HsvToRgb(x * 240f / 128f, 1f, 1f, out r, out g, out b);
					using (Pen p = new Pen(Color.FromArgb(r, g, b))) {
						xpos = width - 128 - 10 + x;
						gfx.DrawLine(p, xpos, height - 20 - font.Height, xpos, height - 10 - font.Height);
					}
				}
				using (SolidBrush legendBrush = new SolidBrush(Color.Red)) {
					using (Pen p = new Pen(Color.Red)) {
						float lineheight = font.GetHeight(gfx);
						float y = height - 20 - lineheight * 3;//the 10px gradient, plus 3 lines of text
						float x1 = width - 10;
						float x2 = x1 - maxWidth;
						float x3 = x2 - 10;
						if (maxN == 0) { maxN = 1000; }
						float x4 = x3 - maxWidth * (1f / maxN);

						gfx.DrawLine(p, x1, y, x2, y);
						gfx.DrawLine(p, x1, y, x1, y - 4);
						gfx.DrawLine(p, x2, y, x2, y - 4);

						gfx.DrawLine(p, x3, y, x4, y);
						gfx.DrawLine(p, x3, y, x3, y - 4);
						gfx.DrawLine(p, x4, y, x4, y - 4);

						sf.Alignment = StringAlignment.Near;
						sf.LineAlignment = StringAlignment.Near;
						gfx.DrawString("0", font, legendBrush, width - 128 - 10, height - 10 - lineheight, sf);
						sf.Alignment = StringAlignment.Center;
						gfx.DrawString("1", font, legendBrush, x3 - (x3 - x4) / 2, y, sf);
						gfx.DrawString(maxN.ToString(), font, legendBrush, x1 - (x1 - x2) / 2, y, sf);
						sf.Alignment = StringAlignment.Far;
						gfx.DrawString(nFrames.ToString(), font, legendBrush, width - 10, height - 10 - lineheight, sf);
						sf.LineAlignment = StringAlignment.Far;
						//-2 offset as the far line alignment doesn't seem as perfect as it should be...
						gfx.DrawString("Peak frame no.", font, legendBrush, width - 10, height - 20 - lineheight - 2, sf);
						gfx.DrawString("Number of peaks", font, legendBrush, width - 10, y - 5 - 2, sf);
					}
				}
			}

			// Dispose of the Graphics object
			gfx.Dispose();
			return bmp;
		}

		/// <summary>
		/// Returns a map with the correlation for each combination of selected BrainCells
		/// </summary>
		/// <returns>The correlation map.</returns>
		/// <param name="brainCells">A list of BrainCells to draw the correlation map for.</param>
		/// <param name="width">The width of the target image.</param>
		/// <param name="height">The height of the targt image.</param>
		/// <param name="similaritySource">The data to calculate the correlation from.</param>
		public Bitmap GetCorrelationSquareGraph(List<BrainCell> brainCells, int width, int height, SimilaritySource similaritySource, int lag) {
			//if either width or height is 0, return full resolution
			if (width == 0 || height == 0) {
				return GetXCorrMap(brainCells, lag, similaritySource);
			}
			int s = Math.Min(width, height);
			Bitmap tmp = new Bitmap(width, height);
			Graphics tg = Graphics.FromImage(tmp);
			tg.InterpolationMode = InterpolationMode.NearestNeighbor;
			tg.DrawImage(GetXCorrMap(brainCells, lag, similaritySource), 0, height / 2 - s / 2, s, s);
			tg.Dispose();
			return tmp;
		}


		/// <summary>
		/// Returns a voronoi map of the correlation of each cell with the first selected cell.
		/// </summary>
		/// <returns>A voronoi map of the correlation.</returns>
		/// <param name="brainCells">A list of BrainCells to draw the correlation map for.</param>
		/// <param name="similaritySource">The data to calculate the correlation from.</param>
		public Bitmap GetCorrelationVoronoiGraph(List<BrainCell> brainCells, SimilaritySource similaritySource, int lag) {
			if (brainCells.Count > 0) {
				float[] set1 = null;
				float[][] set2 = null;
				switch (similaritySource) {
					case SimilaritySource.Peak:
						set1 = cellPeak[cellIndices[brainCells[0]]];
						set2 = cellPeak;
						break;
					//case SimilaritySource.RateOfChange:
					//set1 = cellROCNormalised[cellIndices[brainCells [0]]];
					//set2 = cellROCNormalised;
					//break;
					case SimilaritySource.Activity:
						set1 = cellActivity[cellIndices[brainCells[0]]];
						set2 = cellActivity;
						break;
				}
				return Graphs.GetVoronoiMap(imageMatrixCollection.footageSize.Width, imageMatrixCollection.footageSize.Height, brainCells[0],
					allCells, set1, set2, lag, font);
			} else {
				Bitmap bmpa = new Bitmap(256, 256);
				Graphics ga = Graphics.FromImage(bmpa);
				ga.DrawString("Select a cell", font, new SolidBrush(Color.Red), 0f, 0f);
				ga.Dispose();
				return bmpa;
			}
		}



		/*
		 * The old one
		 * /// <summary>
		/// Returns a voronoi map of the correlation of each cell with the first selected cell.
		/// </summary>
		/// <returns>A voronoi map of the correlation.</returns>
		/// <param name="brainCells">A list of BrainCells to draw the correlation map for.</param>
		/// <param name="similaritySource">The data to calculate the correlation from.</param>
		public Bitmap GetCorrelationVoronoiGraph2 (List<BrainCell> brainCells, SimilaritySource similaritySource, int lag) {
			if (brainCells.Count > 0) {
				return GetVoronoiMap (imageMatrixCollection.footageSize.Width, imageMatrixCollection.footageSize.Height, brainCells [0], lag, similaritySource);
			} else {
				return GetVoronoiMap (imageMatrixCollection.footageSize.Width, imageMatrixCollection.footageSize.Height, null, lag, similaritySource);
			}
		}*/

		/// <summary>
		/// Gets the Bitmap containing bar graphs of all provided cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="cells">The indexes of the cells to draw graphs for.</param>
		/// <param name="brainCells">The cells to draw graphs for.</param>
		/// <param name="width">The desired width of the bitmap.</param>
		/// <param name="height">The desired height of the bitmap.</param>
		public Bitmap GetCorrelationBarGraph(List<BrainCell> brainCells, int width, int height, SimilarityMeasure similarityMeasure, SimilaritySource similaritySource) {
			Bitmap bmp = new Bitmap(width, height);

			//first draw a white background
			Graphics g = Graphics.FromImage(bmp);
			g.Clear(Color.White);

			//if we don't have data yet for some reason, return an empty bitmap
			if (!isFilled) {
				g.Dispose();
				return bmp;
			}

			//then draw all the graphs
			int o = 0;
			for (int c = 0; c < brainCells.Count; c++) {
				//we don't need to show the graph if the cells are the same
				for (int d = c + 1; d < brainCells.Count; d++) {
					//don't draw anything if it doesn't fit
					if ((o + 1) * 64 > height - font.Height) {
						break;
					}
					string name1, name2;
					//ensure the different name for ValidationCells gets used.
					if (brainCells[c] is ValidationCell) {
						name1 = ((ValidationCell)brainCells[c]).Name;
					} else {
						name1 = brainCells[c].Name;
					}
					if (brainCells[d] is ValidationCell) {
						name2 = ((ValidationCell)brainCells[d]).Name;
					} else {
						name2 = brainCells[d].Name;
					}
					//draw the similarity graph
					int cell1 = cellIndices[brainCells[c]];
					int cell2 = cellIndices[brainCells[d]];
					float[] set1 = null, set2 = null;

					switch (similaritySource) {
						//case SimilaritySource.RateOfChange:
						//set1 = cellROCNormalised[cell1];
						//set2 = cellROCNormalised[cell2];
						//break;
						case SimilaritySource.Peak:
							set1 = cellPeak[cell1];
							set2 = cellPeak[cell2];
							break;
						case SimilaritySource.Activity:
							set1 = cellActivity[cell1];
							set2 = cellActivity[cell2];
							break;
					}
					DrawSimilarityGraphOnBitmap(set1, set2, name1 + ", " + name2, width, 64, o++ * 64, g, Color.Blue, similaritySource);
				}
			}

			//draw the frame numbers
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			int xoffset = (int)(width / (11f * 2f));
			g.DrawString("-4", font, new SolidBrush(Color.Gray), 1 * width / 11 + xoffset, o * 64, sf);
			g.DrawString("-2", font, new SolidBrush(Color.Gray), 3 * width / 11 + xoffset, o * 64, sf);
			g.DrawString("0", font, new SolidBrush(Color.Gray), 5 * width / 11 + xoffset, o * 64, sf);
			g.DrawString("2", font, new SolidBrush(Color.Gray), 7 * width / 11 + xoffset, o * 64, sf);
			g.DrawString("4", font, new SolidBrush(Color.Gray), 9 * width / 11 + xoffset, o * 64, sf);

			// Dispose of the Graphics object
			g.Dispose();
			return bmp;
		}


		/// <summary>
		/// Gets the Bitmap containing a cross-correlation map of all cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="brainCells">The cells to draw graphs for.</param>
		/// <param name="lag">The lag to calculate the cross-correlation for.</param>
		public Bitmap GetXCorrMap(List<BrainCell> brainCells, int lag, SimilaritySource similaritySource) {
			if (brainCells.Count < 2) {
				Bitmap bmpa = new Bitmap(256, 256);
				Graphics ga = Graphics.FromImage(bmpa);
				ga.DrawString("Select at least 2 cells", font, new SolidBrush(Color.Red), 0f, 0f);
				ga.Dispose();
				return bmpa;
			}

			Bitmap bmp = new Bitmap(brainCells.Count, brainCells.Count);

			//first draw a white background
			Graphics g = Graphics.FromImage(bmp);
			g.Clear(Color.White);

			if (!isFilled) {
				g.Dispose();
				return bmp;
			}
			/*if (brainCells.Count > 5) {
				g.DrawString ("Select < 6 neurons to calculate similarity.", font, new SolidBrush (Color.Blue), 0, 0);
				MainWindow.ShowMessage ("Warning: select at most 6 braincells to calculate similarity.");
				g.Dispose ();
				return bmp;
			}*/

			for (int x = 0; x < brainCells.Count; x++) {
				int cell1 = cellIndices[brainCells[x]];
				for (int y = x; y < brainCells.Count; y++) {
					int cell2 = cellIndices[brainCells[y]];
					//bool useMean;
					float[] set1 = null, set2 = null;
					switch (similaritySource) {
						case SimilaritySource.Peak:
							set1 = cellPeak[cell1];
							set2 = cellPeak[cell2];
							//useMean = true;
							break;
						//case SimilaritySource.RateOfChange:
						//set1 = cellROCNormalised [cell1];
						//set2 = cellROCNormalised [cell2];
						//in the case of the ROC we can just use 0 as the mean
						//useMean = false;
						//break;
						case SimilaritySource.Activity:
							set1 = cellActivity[cell1];
							set2 = cellActivity[cell2];
							break;
					}
					float s = Utils.GetXCorrelation(set1, set2, lag, true);
					//float[] set1 = cellPeak[x];
					//float[] set2 = cellPeak[y];
					//float[] set1 = cellActivity[x];
					//float[] set2 = cellActivity[y];
					//int r,gg,b;
					//Utils.HsvToRgb(240f - (1f+s)*120f,1f,1f,out r, out gg, out b);
					//Color c = Color.FromArgb(r,gg,b);
					Color c = Utils.GetColorScale((byte)((s + 1f) * 255f / 2f));
					bmp.SetPixel(x, y, c);
					bmp.SetPixel(y, x, c);
				}
			}
			// Dispose of the Graphics object
			g.Dispose();
			return bmp;
		}



		/// <summary>
		/// Gets the Bitmap containing a heatmap of all cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="imagewidth">The width of the returned Bitmap.</param>
		/// <param name="imageheight">The height of the returned Bitmap.</param>
		/// <param name="similaritySource">The data to use.</param>
		public Bitmap GetHeatMap(int imagewidth, int imageheight, SimilaritySource similaritySource) {
			float[][] source;
			switch (similaritySource) {
				case SimilaritySource.Activity:
					source = cellActivity;
					break;
				case SimilaritySource.Peak:
					source = cellPeak;
					break;
				default:
					source = celldFF0;
					break;
			}

			return Graphs.GetHeatMap(imagewidth, imageheight, source, allCells);
		}



		/// <summary>
		/// Draws the activity graph for the selected cell on the provided bitmap.
		/// </summary>
		/// <param name="cell">The cell to draw a graph of.</param>
		/// <param name="text">The unique name of the cell.</param>
		/// <param name="width">The width of the corresponding bitmap.</param>
		/// <param name="height">The height of the corresponding bitmap.</param>
		/// <param name="yoffset">The y coordinates to start drawing from.</param>
		/// <param name="g">The Graphics object created from the corresponding bitmap.</param>
		/// <param name="c">The colour to draw the graph in.</param>
		private void DrawGraphOnBitmap (BrainCell brainCell, string text, int width, int height, int yoffset, Graphics g, Color c, int frame, bool commonScale, bool showPeaks) {
			int cell = cellIndices [brainCell];
			float xScale = width / (float)nFrames;
			float frameWidth = Math.Max (1f, xScale);//ensure width of 1px, as otherwise the peak may not be shown.
			//float yScale = height / (max - min);
			float yScale = height / yscale;
			if (commonScale) {
				yScale = height / yscale;
			} else {
				yScale = height / (cellMaxima[cell] - cellMinima[cell]);
			}

			//if available, draw the active frames
			if (showPeaks) {
				if (cellPeakFrames != null) {
					foreach (int x in cellPeakFrames[cell]) {
						//g.FillRectangle (greenBrush, x * xScale, yoffset, xScale, height);
						g.FillRectangle (redBrush, x * xScale, yoffset, frameWidth, height);
					}
				}
			}

			//we only need to draw the bottom line as the top one is already drawn by the previous cell
			g.DrawLine (lightGreyPen, 0, yoffset + height, width, yoffset + height);
			//next draw the name of the cell.
			g.DrawString (text, font, new SolidBrush(c), 0, yoffset);

			Pen p = new Pen (c);
			//now for each frame, draw a line from the current value to the next
			for (int f = 0; f < nFrames - 1; f++) {
				int x1 = (int)(f * xScale); 
				//int y1 = yoffset + height - (int)(yScale * (cellActivity [cell, f] - min));
				int y1 = yoffset + height - (int)(yScale * (cellActivity [cell][f] - cellMinima[cell]));
				int x2 = (int)((f + 1) * xScale); 
				//int y2 = yoffset + height - (int)(yScale * (cellActivity [cell, f + 1] - min));
				int y2 = yoffset + height - (int)(yScale * (cellActivity [cell][f + 1] - cellMinima[cell]));
				g.DrawLine (p, x1, y1, x2, y2);

			}
		}

		
		/// <summary>
		/// Draws the Similarity graph for the selected cells on the provided bitmap, for the given offsets.
		/// </summary>
		/// <param name="cell1">The cell to draw a graph of.</param>
		/// <param name="cell2">The cell to draw a graph of.</param>
		/// <param name="text">The unique name of the cell.</param>
		/// <param name="width">The width of the corresponding bitmap.</param>
		/// <param name="height">The height of the corresponding bitmap.</param>
		/// <param name="yoffset">The y coordinates to start drawing from.</param>
		/// <param name="g">The Graphics object created from the corresponding bitmap.</param>
		/// <param name="c">The colour to draw the graph in.</param>
		/// <param name="similarityMeasure">How to measure the similarity.</param>
		private void DrawSimilarityGraphOnBitmap (float[] set1, float[] set2, string text, int width, int height, int yoffset, Graphics g, Color c, SimilaritySource similaritySource) {
			float[] similarities = new float[11];
			for (int f = 0; f < 11; f++) {
				//float s = GetSimilarity (cell1, cell2, f - 5, cellPositiveROCAreaNormalised, similarityMeasure);
				float s = Utils.GetXCorrelation(set1,set2,f-5,true);
				//float s = GetSimilarity (f - 5, set1, set2, similarityMeasure);
				similarities [f] = s;
			}

			float xScale = width / 11f;
			float yScale;
			float min;
			if (similaritySource == SimilaritySource.Peak) {//scale goes from 0 to 1
				yScale = height;
				min = 0f;
			} else {//scale goes from -1 to 1
				yScale = height / 2f;
				min = -1f;
			}
			int columnwidth =(int)( width / 11f);

			//we only need to draw the bottom line as the top one is already drawn by the previous cell
			g.DrawLine (lightGreyPen, 0, yoffset + height, width, yoffset + height);
			//and the center line at y=0 of course, if it's the ROC or Activity (the peak one cannot be less than 0)
			if (similaritySource != SimilaritySource.Peak) {
				g.DrawLine (lightGreyPen, 0, yoffset + yScale, width, yoffset + yScale);
			}
			//and the vertical lines
			g.DrawLine (lightGreyPen, 1 * width / 11 + columnwidth / 2, yoffset, 1 * width / 11 + columnwidth / 2, yoffset + height);
			g.DrawLine (lightGreyPen, 3 * width / 11 + columnwidth / 2, yoffset, 3 * width / 11 + columnwidth / 2, yoffset + height);
			g.DrawLine (lightGreyPen, 5 * width / 11 + columnwidth / 2, yoffset, 5 * width / 11 + columnwidth / 2, yoffset + height);
			g.DrawLine (lightGreyPen, 7 * width / 11 + columnwidth / 2, yoffset, 7 * width / 11 + columnwidth / 2, yoffset + height);
			g.DrawLine (lightGreyPen, 9 * width / 11 + columnwidth / 2, yoffset, 9 * width / 11 + columnwidth / 2, yoffset + height);

			Pen p = new Pen (c);
			Brush b = new SolidBrush (Color.LightBlue);
			//now for each frame, draw a line from the current value to the next
			if (similaritySource == SimilaritySource.Peak) {//scale goes from 0 to 1{
				for (int f = 0; f < 11; f++) {
					int x1 = (int)(f * xScale) + 1; 
					int y1 = yoffset - (int)(yScale * (similarities [f] - min)) + height;
					g.FillRectangle (b, x1, y1, columnwidth - 2, yoffset - y1 + height);
					g.DrawRectangle (p, x1, y1, columnwidth - 2, yoffset - y1 + height);
				}
			} else {//scale goes from -1 to 1
				for (int f = 0; f < 11; f++) {
					int x1 = (int)(f * xScale) + 1; 
					int h = (int)(yScale * (similarities [f]));
					//set the coordinates according to the height 
					if (h < 0) {
						g.FillRectangle (b, x1, yoffset + yScale, columnwidth - 2, -h);
						g.DrawRectangle (p, x1, yoffset + yScale, columnwidth - 2, -h);
					} else {
						g.FillRectangle (b, x1, yoffset + yScale - h, columnwidth - 2, h);
						g.DrawRectangle (p, x1, yoffset + yScale - h, columnwidth - 2, h);
					}
				}
			}

			//next draw the name of the cell.
			g.DrawString (text, font, new SolidBrush(c), 0, yoffset);
		}

		#endregion

		public int GetCellIndex(BrainCell c) {
			return cellIndices [c];
		}


		public int GetCellPeakFrameCount (BrainCell c) {
			return cellPeakFrames [cellIndices [c]].Length;
		}	
	}
}

