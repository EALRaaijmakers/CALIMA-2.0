//
//  ImageMatrixCollection.cs
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
using System.IO;
using System.Drawing;
using System.Threading;
using System.Security.Cryptography;
using System.Linq;

namespace CalciumImagingAnalyser
{
	public class ImageMatrixCollection
	{
		private OpenFileDialog openFileDialog;
		private ListBox cellsListBox;
		private TrackBar frameTrackBar;
		private ImagePanel imagePanel;
		private AnalysisPanel analysisPanel;
		private MainWindow mainWindow;
		private int imagesLoaded;

		Bitmap backgroundImage;
		public List<BrainCell> detectedCells { get; private set; }
		public List<List<BrainCell>> cellsPerFrame { get; private set; }
		public List<ValidationCell> validationCells { get; private set; }
		public List<BrainCell> allCells { get { return cellActivityContainer.allCells; } }// private set {}; }
		public List<ImageMatrix> images { get; private set; }
		public CellActivityContainer cellActivityContainer {get; private set;}
		public int currentFrame { get; private set; }
		public Size footageSize { get; private set; }


		public ImageMatrixCollection (ImagePanel imagePanel, AnalysisPanel analysisPanel, TrackBar tb, MainWindow mainWindow)
		{
			this.imagePanel = imagePanel;
			this.analysisPanel = analysisPanel;
			this.mainWindow = mainWindow;
			cellsListBox = analysisPanel.cellsListBox;
			frameTrackBar = tb;
			images = new List<ImageMatrix> ();
			currentFrame = 0;

			detectedCells = new List<BrainCell> ();
			validationCells = new List<ValidationCell> ();
			cellsPerFrame = new List<List<BrainCell>> ();

			cellActivityContainer = new CellActivityContainer (this);

			openFileDialog = new OpenFileDialog ();
			openFileDialog.InitialDirectory = Directory.GetCurrentDirectory ();
			openFileDialog.Multiselect = true;
			openFileDialog.Filter = "Supported files (*.avi, *.wmv, *.mp4, *.png, *.jpg, *.bmp, *.gif, *.tiff, *.tif, *.mov)|*.png;*.avi;*.wmv;*.mp4;*.jpg;*.bmp;*.gif;*.tiff;*.tif;*.mov|All files (*.*)|*.*";

		}
		public void ChangeFrame () {
			if (frameTrackBar.Value - 1 < images.Count) {
				currentFrame = frameTrackBar.Value;
				backgroundImage = images [currentFrame].Bitmap;
				//Bitmap bmp = backgroundImage.Clone ();
				//foreach (BrainCell cell in detectedCells) {
				//	cell.Colour (ref backgroundImage);
				//}
				imagePanel.SetBaseImage(backgroundImage);
				//change the graph to show the new selected frame
				SelectCells ();
			}
		}
		public void DeleteCell (int index){
			//cellsListBox.Items.Remove (selected);
			try {
				BrainCell c = (BrainCell)cellsListBox.Items[index];
				if (index >= 0 && index < cellsListBox.Items.Count) {
					if (cellsListBox.Items[index] is ValidationCell) {
						validationCells.Remove((ValidationCell)c);
					} else {
						detectedCells.Remove(c);
					}
					allCells.Remove(c);
					cellsListBox.Items.Clear ();//to prevent a strange error from occuring when we do a removeAt(index) on the listbox, build the whole box again.
					ListBox.ObjectCollection cellsCollection = new ListBox.ObjectCollection (cellsListBox, allCells.ToArray ());
					cellsListBox.Items.AddRange (cellsCollection);
					if (index < cellsListBox.Items.Count) {
						cellsListBox.SelectedIndex = index;
					}
					MainWindow.ShowMessage ("Deleted cell " + c.ToString() + ".");
				}
			} catch (Exception ex) {
				MainWindow.ShowMessage ("Cannot remove cell at " + index + " " + ex.Message);
			}
		}
		public void DeleteCells (ListBox.SelectedIndexCollection indices)
		{
			//cellsListBox.Items.Remove (selected);
			try {
				List<BrainCell> cs = new List<BrainCell> (indices.Count);
				//(BrainCell)cellsListBox.Items [index];
				foreach (int i in indices) {
					if (i >= 0 && i < cellsListBox.Items.Count) {
						cs.Add ((BrainCell)cellsListBox.Items [i]);
					}
				}
				foreach (BrainCell c in cs) {
					if (c is ValidationCell) {
						validationCells.Remove ((ValidationCell)c);
					} else {
						detectedCells.Remove (c);
					}
					allCells.Remove (c);
					MainWindow.ShowMessage ("Deleted cell " + c.ToString () + ".");
				}
				cellsListBox.Items.Clear ();//to prevent a strange error from occuring when we do a removeAt(index) on the listbox, build the whole box again.
				ListBox.ObjectCollection cellsCollection = new ListBox.ObjectCollection (cellsListBox, allCells.ToArray ());
				cellsListBox.Items.AddRange (cellsCollection);
			} catch (Exception ex) {
				MainWindow.ShowMessage ("Error: cannot remove ROIs. Please try with a single ROI. " + ex.Message);
			}
		}

		public string GetCSVData (CellActivityContainer.CSVExportMode exportMode) {
			if (allCells.Count == 0 || images.Count == 0) {
				MainWindow.ShowMessage ("Error: first load an image and detect ROIs.");
				return null;
			} else {

				/*List<int> selectedCells = new List<int> ();
				foreach (int i in cellsListBox.SelectedIndices) {
					selectedCells.Add (i);
				}*/
				List<BrainCell> selectedBrainCells = new List<BrainCell> ();
				foreach (BrainCell b in cellsListBox.SelectedItems) {
					selectedBrainCells.Add (b);
				}
				return cellActivityContainer.GetCSVData (selectedBrainCells, exportMode);
			}
		}

		/// <summary>
		/// Sets all data in the CellActivityContainer
		/// </summary>
		/// <returns><c>true</c>, if cell values are set successfully, <c>false</c> otherwise.</returns>
		/// <param name="mode">The measuring mode.</param>
		public bool GetCellValues (CellMeasuringMode mode) {
			//System.Diagnostics.Stopwatch signaltimer = System.Diagnostics.Stopwatch.StartNew();
			if (allCells.Count == 0 || images.Count == 0) {
				MainWindow.ShowMessage ("Error: first load an image and detect ROIs.");
				return false;
			} else if (allCells.Count < 2 || images.Count < 2){
				MainWindow.ShowMessage ("Error: at least 2 frames needed to detect activity.");
				return false;
			} else {
				MainWindow.ShowMessage ("Getting activity values of all cells... ", false);
				List<BrainCell> cells = allCells;
				cellActivityContainer.FillContainer (images, cells, mode, mainWindow.analysisPanel.GetNormWindowLength(), mainWindow.analysisPanel.GetNormWindowPercentile());
				MainWindow.ShowMessage ("Done!");
				MainWindow.SetProgressBar (1f);
				//signaltimer.Stop();
				//MainWindow.ShowMessage(signaltimer.ElapsedMilliseconds.ToString());
				return true;
			}
		}

		public void SelectCells () {
			if (cellsListBox.SelectedIndex > allCells.Count) {
				MainWindow.ShowMessage("Error: selected index outside of bounds!");
			} else {
				if (cellsListBox.SelectedIndex < 0) {
					cellsListBox.ClearSelected ();
				}
				List<BrainCell> selectedBrainCells = new List<BrainCell> ();
				foreach (BrainCell b in cellsListBox.SelectedItems) {
					selectedBrainCells.Add (b);
				}
				imagePanel.UpdateSelectedCells (selectedBrainCells);
				analysisPanel.SetGraph (cellActivityContainer.GetGraph (selectedBrainCells, 
					analysisPanel.graphsImageSize.Width, analysisPanel.graphsImageSize.Height, GraphType.Activity, true, false, currentFrame));
				
			}
		}
		public void LoadFile (string ffmpegArgs) {
			//show the dialog
			openFileDialog.ValidateNames = false;
			openFileDialog.CheckFileExists = false;
			DialogResult result = openFileDialog.ShowDialog(mainWindow);
				if (result == DialogResult.OK) // Test result.
			{
				//System.Diagnostics.Stopwatch loadTimer = System.Diagnostics.Stopwatch.StartNew();

				//ensure there's no leftovers from previously loaded images.
				allCells.Clear ();
				detectedCells.Clear ();
				validationCells.Clear ();
				images.Clear ();
				cellActivityContainer.Reset ();
				analysisPanel.analysisStageButton.Enabled = false;
				analysisPanel.cellsListBox.Items.Clear ();
				string [] filename = openFileDialog.FileNames;
					if (filename [0].Substring(filename[0].Length-3) == "all") {
					filename = Directory.GetFiles (filename [0].Substring (0, filename [0].Length - 3));
				}
				if (filename.Length == 1) {
					string extension = filename [0].Substring (filename [0].Length - 3).ToLower ();
					if (extension == "png" || extension == "jpg" || extension == "iff" || extension == "tif" || extension == "tiff") {//single image
						MainWindow.ShowMessage("Warning: You only load a single frame. Any analysis beyond cell detection is disabled. Are you sure this is what you want?");
						LoadPNGArray (filename, 0f);
					} else {//movie
						string tmpdir = Directory.GetCurrentDirectory () + Path.DirectorySeparatorChar + "tmp";
						if (Directory.Exists (tmpdir)) {//if the temporary directory already exists, delete all files in it.
							Directory.Delete (tmpdir, true);
						}
						Directory.CreateDirectory (tmpdir);
						System.Diagnostics.Process pProcess = new System.Diagnostics.Process ();
						MainWindow.SetProgressBar (.05f);
						MainWindow.ShowMessage ("Converting " + filename[0] + "... ", false);
						//pProcess.StartInfo.FileName = @"C:\Users\Vitor\ConsoleApplication1.exe";
						switch (Environment.OSVersion.Platform) {
						case PlatformID.Unix:
							pProcess.StartInfo.FileName = "ffmpeg";
							break;
						case PlatformID.MacOSX:
							pProcess.StartInfo.FileName = "ffmpeg_mac" + Path.DirectorySeparatorChar + "ffmpeg";
							break;
						default://all windows versions
							pProcess.StartInfo.FileName = "ffmpeg_win" + Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar + "ffmpeg.exe";
							break;
						}
						try {//use a try-catch here, as we depend on external dependencies here, which might crash or the-Gods-may-know-what.
							//	pProcess.StartInfo.FileName = "ffmpeg";
							//replace the filename and output placeholders by the correct arguments
							pProcess.StartInfo.Arguments = ffmpegArgs.Replace("filename",filename[0]).Replace("output","\"" + tmpdir + Path.DirectorySeparatorChar + "%04d.png\"");
							//pProcess.StartInfo.Arguments = "-i \"" + filename[0] + "\" -r 1 \"" + tmpdir + Path.DirectorySeparatorChar + "%04d.png\""; //argument
							pProcess.StartInfo.UseShellExecute = false;
							pProcess.StartInfo.RedirectStandardOutput = true;
							if (MainWindow.Verbosity > 1) {//if very high verbosity, also display the ffmpeg output
								pProcess.OutputDataReceived += delegate(object sender, System.Diagnostics.DataReceivedEventArgs e) {
									MainWindow.ShowMessage (e.Data);
									Console.WriteLine (e.Data);
								};
							}
							pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
							pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
							pProcess.Start ();
							pProcess.WaitForExit ();
							pProcess.Dispose ();
						} catch (Exception e) {
							MainWindow.ShowMessage ("Error running ffmpeg: " + e.Message + "\nCheck your ffmpeg install, or download the newest version to the running directory.");
							return;
						}
						MainWindow.SetProgressBar (.25f);
						MainWindow.ShowMessage ("Done!");
						string[] tempfilenames = Directory.GetFiles (tmpdir);
						LoadPNGArray (tempfilenames, .25f);
					}
				} else {
					LoadPNGArray (filename, 0f);
				}
				//loadTimer.Stop();
				//MainWindow.ShowMessage(loadTimer.ElapsedMilliseconds.ToString());
			}
		}

		public void LoadPNGArray (string [] files, float startProcessBar) {
			if (files.Length > 0 ) {
				MainWindow.ShowMessage("Loading images " + Path.GetFileName(files[0]) + " to " + 
					Path.GetFileName(files[files.Length - 1]) + ".");
				Array.Sort (files);
				try
				{
					//check we can load this format
					bool retry = true;
					while (retry) {
						try {
							ImageMatrix im = new ImageMatrix (files[0]);
							retry = false;
						} catch (Exception e) {
							DialogResult dr = MessageBox.Show("Usually this is an image format issue. Please check your files " +
								"are in a supported (8-bit) format.\n\nError: " + e.Message,"Error loading images",
								MessageBoxButtons.AbortRetryIgnore,MessageBoxIcon.Error);
							switch (dr) {
							case DialogResult.Abort:
								return;
							case DialogResult.Retry:
								continue;
							case DialogResult.Ignore:
								retry = false;
								break;
							default://if it's something else for some reason
								retry = false;
								break;
							}
						}
					}

					//images.Clear();
					cellActivityContainer.Reset();
					images = new List<ImageMatrix> (files.Length);
					imagesLoaded = 0;
					frameTrackBar.Maximum = 1;
					//initialize array
					for (int c = 0; c < files.Length; c++) {
						images.Add(null);
					}


					float progressDiff = (1f - startProcessBar) / (float)files.Length;
					int threadN = mainWindow.ThreadCount;
					if (threadN == 0) {
						threadN = Environment.ProcessorCount;
					}
					int imgsPerThread = (files.Length / threadN) + 1;
					for(int n = 0; n < threadN; n++) {
						int minN = n * imgsPerThread;
						int maxN = (n+1)*imgsPerThread;
						if (maxN > files.Length) {
							maxN = files.Length;
						}
						LoadPNGThreadParameters pars = new LoadPNGThreadParameters();
						pars.filenames = files;
						pars.minN = minN;
						pars.maxN = maxN;
						Thread newThread = new Thread(LoadPNGThread);
						newThread.Start(pars);
					}

					int oldImagesLoaded = 0;
					//update UI every 50 ms;
					while (imagesLoaded < files.Length) {//wait for all threads to finish;
						Thread.Sleep (50);
						if (imagesLoaded > oldImagesLoaded) {
							//imagePanel.SetBaseImage(images[imagesLoaded - 1].Bitmap);
							if (MainWindow.Verbosity > 0) {
								MainWindow.ShowMessage ("Loaded " + (imagesLoaded- oldImagesLoaded).ToString () + " images.");
							}
							MainWindow.SetProgressBar(imagesLoaded*progressDiff + startProcessBar);
							oldImagesLoaded = imagesLoaded;
						}
					}

					// Contrast correction
					StretchContrast();
					

					//save the footage resolution
					footageSize = new Size(images[0].width,images[0].height);
					MainWindow.SetProgressBar(1f);
					MainWindow.ShowMessage ("Done!");
					frameTrackBar.Maximum = files.Length - 1;
					imagePanel.SetBaseImage(images[0].Bitmap);
					imagePanel.ResetZoom ();
				} catch (IOException e) {
					MainWindow.ShowMessage (e.Message);
				}
			} else {
				MainWindow.ShowMessage("Error: no frames extracted from video. Check your ffmpeg install.");
				MainWindow.SetProgressBar (1f);
				return;
			}

		}

		struct LoadPNGThreadParameters {
			public string[] filenames;
			public int minN;
			public int maxN;
		}

		void LoadPNGThread (object pars) {
			LoadPNGThreadParameters parameters = (LoadPNGThreadParameters)pars;
			for (int i = parameters.minN; i < parameters.maxN; i++) {
				ImageMatrix imageMatrix = new ImageMatrix (new Bitmap (parameters.filenames [i]));
				images[i] = imageMatrix;
				//Console.Write (" " + i.ToString () + " ");
				imagesLoaded++;
			}
		}


		/// <summary>
		/// Runs the cell detection algorithm on the selected frame, and displays the results.
		/// </summary>
		/// <param name="convolutionType">Convolution type.</param>
		/// <param name="par1">Filter parameter 1.</param> // par 1 obsolete
		/// <param name="par2">Filter parameter 2.</param>
		/// <param name="par3">Filter parameter 3.</param>
		/// <param name="par4">Filter parameter 4.</param>
		/// <param name="nThreads">The number of threads to use.</param>
		public void DetectAllFrames (ConvolutionType convolutionType, float par2, float par3, float par4, int nThreads, bool useAverage) {

			if (images.Count != 0) {//check if an image is loaded
				if (convolutionType == ConvolutionType.Benchmark) {
					MainWindow.ShowMessage ("Please run benchmark on a single frame.");
					return;
				}
				if (MainWindow.Verbosity > 0) {
					MainWindow.ShowMessage ("Creating filters (sigma={" + par2 + ";" + par3 + "})... ", false);
				}
				//Filter filter_blur = new Filter (FilterType.Gaussian, new float[]{ par1 });
				Filter filter_dog1 = new Filter (FilterType.Gaussian, new float[]{ par2 });
				Filter filter_dog2 = new Filter (FilterType.Gaussian, new float[]{ par3 });
				if (MainWindow.Verbosity > 0) {
					MainWindow.ShowMessage ("Done!");
				}

				//clear the old cell data
				cellsPerFrame.Clear ();
				//detect cells
				for (int n = 0; n < images.Count; n++) {
					if (MainWindow.Verbosity > 0) {
						MainWindow.ShowMessage ("Detecting cells on frame " + (n + 1).ToString () + " of " + images.Count.ToString () + "... ", false);
					}
					detectedCells = FindCells (convolutionType, filter_dog1, filter_dog2, par4, nThreads, n, useAverage);
					cellsPerFrame.Add (detectedCells);
					MainWindow.SetProgressBar ((n+1)/(float)images.Count);
					if (MainWindow.Verbosity > 0) {
						MainWindow.ShowMessage ("Done!");
					}
				}
				//make sure allCells still contains all validation cells.
				allCells.Clear ();
				allCells.AddRange (validationCells);
				allCells.AddRange (detectedCells);
				cellsListBox.Items.Clear ();
				ListBox.ObjectCollection cellsCollection = new ListBox.ObjectCollection (cellsListBox, allCells.ToArray ());
				cellsListBox.Items.AddRange (cellsCollection);
				MainWindow.ShowMessage (detectedCells.Count + " ROIs detected!");
				MainWindow.SetProgressBar (1f);
				imagePanel.UpdateImage ();

			} else {
				MainWindow.ShowMessage ("Error: no image loaded. Please load an image before detecting cells.");
			}
		}

		/// <summary>
		/// Runs the cell detection algorithm on the selected frame, and displays the results.
		/// </summary>
		/// <param name="convolutionType">Convolution type.</param>
		/// <param name="par1">Filter parameter 1.</param> //par 1 obsolete
		/// <param name="par2">Filter parameter 2.</param>
		/// <param name="par3">Filter parameter 3.</param>
		/// <param name="par4">Filter parameter 4.</param>
		/// <param name="nThreads">The number of threads to use.</param>
		public void DetectSingleFrame (ConvolutionType convolutionType, float par2, float par3, float par4, int nThreads, bool useAverage) {
			//System.Diagnostics.Stopwatch dogtimer = System.Diagnostics.Stopwatch.StartNew();
			if (images.Count != 0) {//check if an image is loaded
				MainWindow.ShowMessage ("Creating filters (sigma={" + par2 + ";" + par3 + "})... ", false);
				//Filter filter_blur = new Filter (FilterType.Gaussian, new float[]{ par1 });
				Filter filter_dog1 = new Filter (FilterType.Gaussian, new float[]{ par2 });
				Filter filter_dog2 = new Filter (FilterType.Gaussian, new float[]{ par3 });
				MainWindow.ShowMessage ("Done!");
				//detect cells
				detectedCells = FindCells (convolutionType, filter_dog1, filter_dog2, par4, nThreads, -1, useAverage);
				//clear all activity data and reset button to disabled
				cellActivityContainer.Reset ();
				analysisPanel.analysisStageButton.Enabled = false;
				//make sure allCells still contains all validation cells.
				allCells.Clear ();
				allCells.AddRange (validationCells);
				allCells.AddRange (detectedCells);
				cellsListBox.Items.Clear ();
				//ListBox.ObjectCollection cellsCollection = new ListBox.ObjectCollection (cellsListBox, allCells.ToArray ());
				cellsListBox.Items.AddRange(allCells.ToArray());
				//cellsListBox.Items.AddRange (cellsCollection);
				MainWindow.ShowMessage (detectedCells.Count + " ROIs detected!");
				MainWindow.SetProgressBar(1f);
				imagePanel.UpdateImage();

				//dogtimer.Stop();
				//MainWindow.ShowMessage(dogtimer.ElapsedMilliseconds.ToString());
			} else {
				MainWindow.ShowMessage ("Error: no image loaded. Please load an image before detecting cells.");
			}
		}

		/// <summary>
		/// Runs the cell detection algorithm on the selected frame, and displays the results.
		/// </summary>
		/// <param name="cellImage">A black-and-white image with cells in white and the background in black.</param>
		/// <param name="nThreads">The number of threads to use.</param>
		public void DetectSingleFrame (Bitmap cellImage, int nThreads) {
			if (images.Count != 0) {//check if an image is loaded
				MainWindow.ShowMessage ("Creating cell objects based on provided image...");
				//detect cells
				detectedCells = FindCells (cellImage, nThreads, -1);
				//clear all activity data and reset button to disabled
				cellActivityContainer.Reset ();
				analysisPanel.analysisStageButton.Enabled = false;
				//make sure allCells still contains all validation cells.
				allCells.Clear ();
				allCells.AddRange (validationCells);
				allCells.AddRange (detectedCells);
				cellsListBox.Items.Clear ();
				//ListBox.ObjectCollection cellsCollection = new ListBox.ObjectCollection (cellsListBox, allCells.ToArray ());
				cellsListBox.Items.AddRange(allCells.ToArray());
				//cellsListBox.Items.AddRange (cellsCollection);
				MainWindow.ShowMessage (detectedCells.Count + " ROIs detected!");
				MainWindow.SetProgressBar(1f);
				imagePanel.UpdateImage();
			} else {
				MainWindow.ShowMessage ("Error: no image loaded. Please load an image before detecting cells.");
			}
		}

		/// <summary>
		/// Runs the cell detection algorithm on the specified frame, and displays the results.
		/// </summary>
		/// <param name="convolutionType">Convolution type.</param>
		/// <param name="par1">Filter parameter 1.</param> //par1 obsolete
		/// <param name="par2">Filter parameter 2.</param>
		/// <param name="par3">Filter parameter 3.</param>
		/// <param name="par4">Filter parameter 4.</param>
		/// <param name="nThreads">The number of threads to use.</param>
		/// <param name="frame">The 0-based frame to run the algorithm on. -1 for currently displayed frame.</param>
		public List<BrainCell> FindCells (ConvolutionType convolutionType, 
			Filter filter_dog1, Filter filter_dog2, float treshold, int nThreads, int frame, bool useAverage) {
			//protect against wrong frame numbers
			if (frame == -1) {
				frame = currentFrame;
			}
			if (frame >= images.Count) {
				frame = images.Count - 1;
			}

			MainWindow.SetProgressBar (0f);
			ImageMatrix startImage;
			if (useAverage) {
				if (MainWindow.Verbosity > 1) {
					MainWindow.ShowMessage ("Calculating average values... ", false);
				}
				startImage = ImageMatrix.GetAverageMatrix (images);
				MainWindow.SetProgressBar (.1f);
				if (MainWindow.Verbosity > 1) {
					MainWindow.ShowMessage ("Done!\nStretching contrast... ", false);
				}
				//startImage = startImage.StretchContrast ();
			} else {
				if (MainWindow.Verbosity > 1) {
					MainWindow.ShowMessage ("Stretching contrast... ", false);
				}
				startImage = images[frame];//.StretchContrast ();
			}


			//extend the image to get rid of image artifacts around the borders
			startImage = startImage.ExtendImage (filter_dog2.width);
			if (MainWindow.Verbosity > 1) {
				MainWindow.SetProgressBar (.2f);
				//MainWindow.ShowMessage ("Done!\nBlurring image... ", false);
			}
			/*ImageMatrix blurred = startImage.Convolve (filter_blur, convolutionType,nThreads);
			if (MainWindow.Verbosity > 1) {
				MainWindow.ShowMessage ("Done!\nPerforming DOG filter... ", false);
				MainWindow.SetProgressBar (.5f);
			}*/
			ImageMatrix dog1 = startImage.Convolve (filter_dog1, convolutionType,nThreads);
			if (MainWindow.Verbosity > 1) {
				MainWindow.ShowMessage ("33% ", false);
				MainWindow.SetProgressBar (.7f);
			}
			ImageMatrix dog2 = startImage.Convolve (filter_dog2, convolutionType,nThreads);
			if (MainWindow.Verbosity > 1) {
				MainWindow.ShowMessage ("66% ", false);
				MainWindow.SetProgressBar (.9f);
			}
			ImageMatrix diff = dog1 - dog2;
			if (MainWindow.Verbosity > 1) {
				MainWindow.ShowMessage ("100%\nDetecting cells (treshold=" + treshold.ToString () + ")... ", false);
				MainWindow.SetProgressBar (.95f);
			}
			//shrink the previously detected image to the previous size
			diff = diff.ShrinkImage (filter_dog2.width);
			//detect cells
			List <BrainCell> temp = diff.DetectCells (treshold);
			if (MainWindow.Verbosity > 1) {
				MainWindow.ShowMessage (detectedCells.Count + " ROIs detected!");
				MainWindow.SetProgressBar (1f);
			}
			cellActivityContainer.Reset ();
			return temp;
		}

		/// <summary>
		/// Runs the cell detection algorithm on the specified frame, and displays the results.
		/// </summary>
		/// <param name="cellImage">A black-and-white image with cells in white and the background in black.</param>
		/// <param name="nThreads">The number of threads to use.</param>
		/// <param name="frame">The 0-based frame to run the algorithm on. -1 for currently displayed frame.</param>
		public List<BrainCell> FindCells (Bitmap cellImage, int nThreads, int frame) {
			//protect against wrong frame numbers
			if (frame == -1) {
				frame = currentFrame;
			}
			if (frame >= images.Count) {
				frame = images.Count - 1;
			}
			if (MainWindow.Verbosity > 1) {
				MainWindow.SetProgressBar (0f);
				MainWindow.ShowMessage ("Stretching contrast... ", false);
			}
			ImageMatrix cellImageMatrix = new ImageMatrix (cellImage);
			//detect cells
			List <BrainCell> temp = cellImageMatrix.DetectCells (.5f);
			if (MainWindow.Verbosity > 1) {
				MainWindow.ShowMessage (detectedCells.Count + " ROIs detected!");
				MainWindow.SetProgressBar (1f);
			}
			cellActivityContainer.Reset ();
			return temp;
		}


		/// <summary>
		/// Execute the validation algorithm.
		/// </summary>
		/// <param name="validationArea">The area to count positives/negatives in.</param>
		public void DoValidation (Rectangle validationArea) {
			if (detectedCells.Count == 0 || validationCells.Count == 0) {
				MainWindow.ShowMessage ("Please both manually add ValidationCells and automatically detect BrainCells to enable validation.");
			} else {
				Dictionary <BrainCell,ValidationCell> cellsDone = new Dictionary<BrainCell, ValidationCell>();
				List<BrainCell> cellsInRectangle = new List<BrainCell> ();
				//first only add cells within the area to the dictionary
				foreach (BrainCell c in detectedCells) {
					if (c.IsInRectangle (validationArea)) {
						cellsDone.Add (c, null);
						cellsInRectangle.Add (c);
					}
				}
				int TP = 0;//True Positives
				int FP = 0;
				int FN = 0;
				int mergedCells = 0;

				foreach (ValidationCell v in validationCells) {
					bool didDetect = false;
					foreach (BrainCell c in cellsInRectangle) {
						if (v.MatchesCell (c)) {
							v.SetDetectedState (true);
							didDetect = true;//no matter if the cell is already claimed, we did at least detect it.
							if (cellsDone [c] == null) {//claim the BrainCell for this ValidationCell.
								cellsDone[c] = v;
								TP ++;
							} else {//the cell was already claimed by another ValidationCell!
								mergedCells++;
							}
						}
					}
					if (!didDetect) {//ValidationCell couldn't find a matching BrainCell, so is a false negative.
						v.SetDetectedState (false);
						FN++;
					}
				}
				foreach (BrainCell c in cellsInRectangle) {
					if (cellsDone [c] == null) {//cell has not been claimed by a ValidationCell, so is a false positive.
						FP++;
					}
				}

				int P = validationCells.Count;//condition positives
				int P_pred = TP + FP;//prediction positives

				float TPR = 100f * TP / (float)P;
				float PPV = 100f * TP / (float)P_pred;
				MainWindow.ShowMessage ("Validation:\n   Condition Positives: " + P.ToString () + 
					"\n   True Positives: " + TP.ToString () + 
					"\n   False Positives: " + FP.ToString () + "\n   False Negatives: " +
					FN.ToString () + "\n   Merged cells: " + mergedCells.ToString () +
					"\n   Sensitivity (True Positive Rate): " + TPR.ToString () +
					"%\n   Positive Predictive Value: " + PPV.ToString () + "%");

				//and redraw the image with the coloured ValidationCells.
				imagePanel.UpdateImage ();
			}
		}

		/// <summary>
		/// Creates an image with the background in black and all cells in white.
		/// </summary>
		/// <returns>A Bitmap containing the cell overlay.</returns>
		public Bitmap GetCellOvelay () {
			Bitmap bmp = new Bitmap (images [0].width, images [0].height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			Graphics g = Graphics.FromImage (bmp);
			foreach (BrainCell c in allCells) {
				c.LightUp (ref bmp, Color.White, g);
			}
			return bmp;
		}

		/// <summary>
		/// Runs when the image is clicked.
		/// </summary>
		/// <param name="clickx">The mouse x coordinate.</param>
		/// <param name="clicky">The mouse y coordinate.</param>
		/// <param name="validating"><c>True</c> if we want to add a ValidationCell, <c>false</c> if we want to select a BrainCell.</param>
		public void ClickImage(int clickx, int clicky, bool validating){
			if (images.Count != 0) {//check if an image is loaded
				//first save the coordinates
				int x, y;
				imagePanel.GetClickedCoordinates (clickx, clicky, out x, out y);

				//check if we're trying to create a ValidationCell, or select a BrainCell
				if (validating) {
					ValidationCell v = new ValidationCell (x, y, validationCells.Count, images[currentFrame].width, images[currentFrame].height, MainWindow.settingsPanel.ManualROISize);
					allCells.Insert (validationCells.Count,v);
					cellsListBox.Items.Insert (validationCells.Count, v);
					cellActivityContainer.Reset();
					analysisPanel.SetGraph((Bitmap)Image.FromFile ("graphs.png"));
					validationCells.Add(v);
					//ensure the new cell shows up
					imagePanel.UpdateImage();
					MainWindow.ShowMessage ("Validation cell added at (" + x.ToString() + "," + y.ToString() + ").");
				} else {
					foreach (BrainCell c in detectedCells) {
						if (c.ContainsPosition (x, y)) {
							int index = cellsListBox.Items.IndexOf (c);
							cellsListBox.SetSelected (index, !cellsListBox.GetSelected (index));
							//cellsListBox.SelectedItem = c;
						}
					}
				}
			}
		}

		public void StretchContrast() {
			MainWindow.ShowMessage("Finding pixel value limits for " + images.Count.ToString() + " images.");
			MainWindow.SetProgressBar(0);

			float[] pixelValsMax = new float[images.Count];
			float[] pixelValsMin = new float[images.Count];

			imagesLoaded = 0;
			int oldImagesLoaded = 0;

			int threadN = mainWindow.ThreadCount;
			if (threadN == 0) {
				threadN = Environment.ProcessorCount;
			}
			int imgsPerThread = (images.Count / threadN) + 1;
			for (int n = 0; n < threadN; n++) {
				int minN = n * imgsPerThread;
				int maxN = (n + 1) * imgsPerThread;
				if (maxN > images.Count) {
					maxN = images.Count;
				}
				FindMinMaxThreadParameters pars = new FindMinMaxThreadParameters();
				pars.imageIndexFrom = minN;
				pars.imageIndexTo = maxN;
				pars.resultsMin = pixelValsMin;
				pars.resultsMax = pixelValsMax;
				Thread newThread = new Thread(FindMinMaxThread);
				newThread.Start(pars);
			}

			float progressDiff = (1f) / (float)images.Count;
			while (imagesLoaded < images.Count) {//wait for all threads to finish;
				Thread.Sleep(10);
				if (imagesLoaded > oldImagesLoaded) {
					if (MainWindow.Verbosity > 0) {
						MainWindow.ShowMessage("Found pixel value limits for " + (imagesLoaded - oldImagesLoaded).ToString() + " images.");
					}
					MainWindow.SetProgressBar(imagesLoaded * progressDiff + 0);
					oldImagesLoaded = imagesLoaded;
				}
			}


			float pixelValMin = pixelValsMin.Min();
			float pixelValMax = pixelValsMax.Max();



			MainWindow.ShowMessage("Correcting contrast for " + images.Count.ToString() + " images.");
			MainWindow.SetProgressBar(0);

			imagesLoaded = 0;
			oldImagesLoaded = 0;
			for (int n = 0; n < threadN; n++) {
				int minN = n * imgsPerThread;
				int maxN = (n + 1) * imgsPerThread;
				if (maxN > images.Count) {
					maxN = images.Count;
				}
				StretchContrastThreadParameters pars = new StretchContrastThreadParameters();
				pars.imageIndexFrom = minN;
				pars.imageIndexTo = maxN;
				pars.pixelValMin = pixelValMin;
				pars.pixelValMax = pixelValMax;
				Thread newThread = new Thread(StrecthContrastThread);
				newThread.Start(pars);
			}

			while (imagesLoaded < images.Count) {//wait for all threads to finish;
				Thread.Sleep(10);
				if (imagesLoaded > oldImagesLoaded) {
					if (MainWindow.Verbosity > 0) {
						MainWindow.ShowMessage("Correct contrast for " + (imagesLoaded - oldImagesLoaded).ToString() + " images.");
					}
					MainWindow.SetProgressBar(imagesLoaded * progressDiff + 0);
					oldImagesLoaded = imagesLoaded;
				}
			}
		}


		struct StretchContrastThreadParameters {
			public int imageIndexFrom;
			public int imageIndexTo;
			public float pixelValMin;
			public float pixelValMax;
		}

		void StrecthContrastThread(object pars) {
			StretchContrastThreadParameters parameters = (StretchContrastThreadParameters)pars;
			for (int i = parameters.imageIndexFrom; i < parameters.imageIndexTo; i++) {
				images[i] = images[i].StretchContrast(parameters.pixelValMin, parameters.pixelValMax);
				imagesLoaded++;
			}
		}

		struct FindMinMaxThreadParameters {
			public int imageIndexFrom;
			public int imageIndexTo;
			public float [] resultsMin;
			public float [] resultsMax;
		}

		void FindMinMaxThread(object pars) {
			FindMinMaxThreadParameters parameters = (FindMinMaxThreadParameters)pars;
			for(int i = parameters.imageIndexFrom; i < parameters.imageIndexTo; i++) {
				parameters.resultsMin[i] = images[i].Min;
				parameters.resultsMax[i] = images[i].Max;
				imagesLoaded++;
			}
		}
	}
}

