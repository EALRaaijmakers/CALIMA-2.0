//
//  ImageMatrix.cs
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
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace CalciumImagingAnalyser
{
	public enum ConvolutionType { Threaded, Unthreaded, Benchmark };

	public class ImageMatrix {
		private const int maxStack = 4000;
		public int width { get; private set; }
		public int height { get; private set; }
		public float [,] data { get; private set; }

		public float Max {
			get {
				float max = float.MinValue;
				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						if (data [x, y] > max) {
							max = data [x, y];
						}
					}
				}
				return max;
			}
		}
		public float Min {
			get {
				float min = float.MaxValue;
				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						if (data [x, y] < min) {
							min = data [x, y];
						}
					}
				}
				return min;
			}
		}

		public Bitmap Bitmap { get; private set; }

		#region Operators
		public static ImageMatrix operator + (ImageMatrix im1, ImageMatrix im2) {
			if (im1.width != im2.width || im1.height != im2.height) {
				MainWindow.ShowMessage ("Error: images not of the same size. Aborting.");
				return null;
			}
			float [,] newdata = new float [im1.width, im1.height];
			for (int x = 0; x < im1.width; x++) {
				for (int y = 0; y < im1.height; y++) {
					newdata [x, y] = im1.data [x, y] + im2.data [x, y];
				}
			}
			return new ImageMatrix (newdata);
		}
		public static ImageMatrix operator - (ImageMatrix im1, ImageMatrix im2) {
			if (im1.width != im2.width || im1.height != im2.height) {
				MainWindow.ShowMessage ("Error: images not of the same size. Aborting.");
				return null;
			}
			float [,] newdata = new float [im1.width, im1.height];
			for (int x = 0; x < im1.width; x++) {
				for (int y = 0; y < im1.height; y++) {
					newdata [x, y] = im1.data [x, y] - im2.data [x, y];
				}
			}
			return new ImageMatrix (newdata);
		}

		#endregion



		public ImageMatrix (string filename)
		{
			FillMatrix (new Bitmap (filename));
			CreateBitmap ();
		}

		public ImageMatrix (Bitmap bmp)
		{
			FillMatrix (bmp);
			CreateBitmap ();
		}

		public ImageMatrix (float [,] data) {
			width = data.GetLength (0);
			height = data.GetLength (1);
			this.data = data;
			//	Clamp ();//ensure there's no pixel with lower value than 0
			CreateBitmap ();
		}

		/*public void Clamp (){
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					if (data [x, y] < 0f)
						data [x, y] = 0f;
					if (data [x, y] > 1f)
						data [x, y] = 1f;
				}
			}
		}*/

		private void CreateBitmap () {
			Bitmap = new Bitmap (width, height);
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					float val = data [x, y];
					Color c;
					//we cannot assign pixels with a negative value
					if (val < 0) {
						c = Color.Black;
					} else {
						int cval = (int)(255 * val);
						c = Color.FromArgb (cval, cval, cval);//convert back to RGB 0..255
					}
					Bitmap.SetPixel (x, y, c);
				}
			}
		}

		public Bitmap GetHeatmap (Bitmap other, float factor) {
			Bitmap heatmap = new Bitmap (width, height);
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					//get the difference between frames. Doesn't matter if we take the R G or B channels as it's greyscale.
					//int diff = Math.Abs(other.GetPixel (x, y).R - Bitmap.GetPixel (x, y).R);//0-255
					float diff = Math.Max (0f, (other.GetPixel (x, y).R - Bitmap.GetPixel (x, y).R) / 255f);//0-1
					diff *= diff;
					//hue goes from 240 (blue) to 0 (red)
					float hue = 240f - (factor / 255f) * 255f * diff;
					if (hue < 0f) {
						hue = 0f;
					}
					int r, g, b;
					Utils.HsvToRgb (hue, 1f, 1f, out r, out g, out b);
					heatmap.SetPixel (x, y, Color.FromArgb (r, g, b));
				}
			}
			return heatmap;
		}

		public Bitmap OverlayBitmap (Bitmap baseMap, float mid) {
			if (width != baseMap.Width || height != baseMap.Height) {
				MainWindow.ShowMessage ("Error: ImageMatrix not of the same height as Bitmap. Aborting.");
				return null;
			}
			Bitmap bmp = new Bitmap (width, height);
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					Color c;
					if (data [x, y] < mid) {
						c = baseMap.GetPixel (x, y);
					} else {
						Color ct = baseMap.GetPixel (x, y);
						c = Color.FromArgb ((ct.R + ct.G + ct.B) / 3, 0, 0);
					}
					bmp.SetPixel (x, y, c);
				}
			}
			return bmp;
		}

		public void ToBW (float mid) {
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					if (data [x, y] < mid)
						data [x, y] = 0f;
					else
						data [x, y] = 1f;
				}
			}
		}

		public ImageMatrix StretchContrast () {
			return StretchContrast (Min, Max);
		}

		public ImageMatrix StretchContrast (float min, float max) {
			//img_edit = (img - minval)*(255/(maxval-minval));
			float [,] newdata = new float [width, height];
			float factor = 1f / (max - min);
			if (float.IsInfinity (factor) || float.IsNaN (factor)) {
				factor = 1f;
			}
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					newdata [x, y] = (data [x, y] - min) * factor;
				}
			}
			return new ImageMatrix (newdata);
		}

		private void FillMatrix (Bitmap bmp) {
			width = bmp.Width;
			height = bmp.Height;
			data = new float [width, height];

			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					Color c = bmp.GetPixel (x, y);
					data [x, y] = (c.R + c.G + c.B) / (3f * 255f);//convert RGB to greyscale 0..1

				}
			}
		}

		public ImageMatrix Convolve (Filter filter, ConvolutionType type, int threads) {
			//http://www.songho.ca/dsp/convolution/convolution.html#cpp_conv2d
			// find center position of kernel (half of kernel size)
			//http://www.songho.ca/dsp/convolution/convolution.html#cpp_conv2d

			switch (type) {
			case ConvolutionType.Threaded:
				if (threads == 1) {
					return ConvolveSeparable (filter);
				} else {
					if (threads == 0) {
						return ConvolveSeparableThreaded (filter, Environment.ProcessorCount);
					} else {
						return ConvolveSeparableThreaded (filter, threads);
					}
				}
			case ConvolutionType.Unthreaded:
				return ConvolveSeparable (filter);
			//return ConvolveUnseperable (filter);
			case ConvolutionType.Benchmark:
				DateTime startTime = DateTime.Now;
				ConvolveSeparable (filter);
				ConvolveSeparable (filter);
				ConvolveSeparable (filter);
				ConvolveSeparable (filter);
				ConvolveSeparable (filter);

				DateTime timesept = DateTime.Now;
				ConvolveSeparableThreaded (filter, Environment.ProcessorCount);
				ConvolveSeparableThreaded (filter, Environment.ProcessorCount);
				ConvolveSeparableThreaded (filter, Environment.ProcessorCount);
				ConvolveSeparableThreaded (filter, Environment.ProcessorCount);
				ConvolveSeparableThreaded (filter, Environment.ProcessorCount);

				DateTime timesepu = DateTime.Now;
				ConvolveUnseperable (filter);
				ConvolveUnseperable (filter);
				ConvolveUnseperable (filter);
				ConvolveUnseperable (filter);
				ConvolveUnseperable (filter);

				TimeSpan diff = startTime - timesept;
				TimeSpan diff2 = timesept - timesepu;
				TimeSpan diff3 = timesepu - DateTime.Now;
				MainWindow.ShowMessage ("Seperable, threaded (" + Environment.ProcessorCount + " threads): " + diff2.TotalSeconds / 5d + "s (5-point average)");
				MainWindow.ShowMessage ("Seperable, unthreaded: " + diff.TotalSeconds / 5d + "s (5-point average)");
				MainWindow.ShowMessage ("Unseperable, unthreaded: " + diff3.TotalSeconds / 5d + "s (5-point average)");
				return this;
			}
			return null;
		}

		public List<BrainCell> DetectCells (float treshold) {
			List<BrainCell> allCells = new List<BrainCell> ();
			bool [,] hasChecked = new bool [width, height];//defaults to false, which is exactly what we desire

			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					if (!hasChecked [x, y]) {
						if (data [x, y] > treshold) {

							List<Point> points = new List<Point> ();
							List<Point> edgePoints = new List<Point> ();
							int stack = 1;//avoid a stack overlow by keeping track of the number of recursions.
							DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stack, x, y, treshold);

							allCells.Add (new BrainCell (points, allCells.Count, edgePoints));
						}
						hasChecked [x, y] = true;
					}
				}
			}
			return allCells;
		}
		/// <summary>
		/// Detects the single cell.
		/// </summary>
		/// <returns><c>true</c>, if this pixel belongs to an ROI, <c>false</c> otherwise.</returns>
		private bool DetectSingleCell (ref bool [,] hasChecked, ref List<Point> points, ref List<Point> edgePoints, ref int stackN, int x, int y, float threshold) {
			//System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack ();
			if (x < 0 || x >= width || y < 0 || y >= height) {//out of bounds
				return false;
			}
			if (hasChecked [x, y]) {//already been here. Simply return whether it was an ROI
				return data [x, y] >= threshold;
			}
			hasChecked [x, y] = true;
			if (data [x, y] < threshold) {//not part of the ROI
				return false;
			}
			points.Add (new Point (x, y));
			if (stackN++ > maxStack) {
				if (stackN < maxStack + 10) {//first overflow
					MainWindow.ShowMessage ("Error: cell too large (>" + maxStack + "pixels). Splitting in two to prevent stack overflow.");
					stackN = maxStack + 10;
				}
				return false;
			}
			//if not surrounded on all sides by other ROI pixels, it must be an edge.
			//separate bool and binary AND because misplaced optimisation in the compiler refuses to run all functions otherwise.
			bool surrounded = DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x - 1, y - 1, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x - 1, y, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x - 1, y + 1, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x, y - 1, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x, y + 1, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x + 1, y, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x + 1, y - 1, threshold) &
				DetectSingleCell (ref hasChecked, ref points, ref edgePoints, ref stackN, x + 1, y + 1, threshold);
			if (!surrounded) {
				edgePoints.Add (new Point (x, y));
			}
			return true;
			/*if (x > 0) {
				if (!hasChecked [x-1, y] && data [x-1, y] > threshold) {
					DetectSingleCell (ref hasChecked, ref points, ref stackN, x - 1, y, threshold);
				}
			}
			if (x < width - 1) {
				if (!hasChecked [x+1, y] && data [x+1, y] > threshold) {
					DetectSingleCell (ref hasChecked, ref points, ref stackN, x + 1, y, threshold);
				}
			}
			if (y > 0) {
				if (!hasChecked [x, y-1] && data [x, y-1] > threshold) {
					DetectSingleCell (ref hasChecked, ref points, ref stackN, x, y-1, threshold);
				}
			}
			if (y < height - 1) {
				if (!hasChecked [x, y+1] && data [x, y+1] > threshold) {
					DetectSingleCell (ref hasChecked, ref points, ref stackN, x, y+1, threshold);
				}
			}*/
		}

		private ImageMatrix ConvolveSeparableThreaded (Filter filter, int nThreads) {
			int cols = width;
			int rows = height;
			int kCols = filter.width;
			int kRows = filter.height;

			//int nThreads = 8;
			int rowdiff = rows / nThreads;
			int coldiff = cols / nThreads;
			int [] [] iters = new int [nThreads] [];

			int i;
			for (i = 0; i < nThreads; i++) {
				iters [i] = new int [4];
				iters [i] [0] = i * rowdiff;
				iters [i] [1] = (i + 1) * rowdiff;
				iters [i] [2] = i * coldiff;
				iters [i] [3] = (i + 1) * coldiff;
			}
			iters [iters.Length - 1] [1] = rows;
			iters [iters.Length - 1] [3] = cols;//make sure the final threads always do all leftover pixels.

			t_filter = filter;
			t_finished = 0;
			t_intermediateData = new float [width, height];
			t_newData = new float [width, height];

			for (i = 0; i < nThreads; i++) {//start threads
				Thread newThread = new Thread (ConvolveThreadRows);
				newThread.Start (iters [i]);
			}

			int t_iter = 0;
			while (t_finished < nThreads) {//wait for all threads to finish;
				Thread.Sleep (10);
				if (t_iter++ > 1000) {//10s
					if (MessageBox.Show ("Calculating convolution is taking longer than expected. Continue?", "Warning", MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning) != DialogResult.Yes) {
						t_finished = nThreads;
						MessageBox.Show ("Operation did not complete successfully. Please rerun.", "Warning", MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					}
					t_iter = 0;
				}
			}

			t_finished = 0;

			for (i = 0; i < nThreads; i++) {//start threads
				Thread newThread = new Thread (ConvolveThreadCols);
				newThread.Start (iters [i]);
			}

			while (t_finished < nThreads) {//wait for all threads to finish;
				Thread.Sleep (5);
			}

			//return new ImageMatrix (intermediateData);
			return new ImageMatrix (t_newData);
		}

		Filter t_filter;
		int t_finished;
		float [,] t_intermediateData;
		float [,] t_newData;

		private void ConvolveThreadRows (object args) {
			int [] offsets = (int [])args;
			int rowstart = offsets [0];
			int rowend = offsets [1];
			int rows = height;
			int cols = width;
			int kCols = t_filter.width;
			int kRows = t_filter.height;
			int kCenterX = kCols / 2;

			for (int iy = rowstart; iy < rowend; iy++) {
				for (int ix = kCenterX; ix < cols - kCenterX; ix++) {
					t_intermediateData [ix, iy] = 0;                       // set to zero before sum
					for (int j = 0; j < kCols; j++) {
						t_intermediateData [ix, iy] += data [ix - j + kCenterX, iy] * t_filter.xKernel [j];    // convolve: multiply and accumulate
					}
				}
			}
			t_finished++;
		}
		private void ConvolveThreadCols (object args) {
			int [] offsets = (int [])args;
			int colstart = offsets [2];
			int colend = offsets [3];
			int rows = height;
			int cols = width;
			int kCols = t_filter.width;
			int kRows = t_filter.height;
			int kCenterY = kRows / 2;

			for (int ix = colstart; ix < colend; ix++) {
				for (int iy = kCenterY; iy < rows - kCenterY; iy++) {
					t_newData [ix, iy] = 0;                       // set to zero before sum
					for (int j = 0; j < kRows; j++) {
						t_newData [ix, iy] += t_intermediateData [ix, iy - j + kCenterY] * t_filter.yKernel [j];    // convolve: multiply and accumulate
																													//newData [ix,iy] += data [ix,iy - j] * filter.yKernel[j];   
					}
				}
			}
			t_finished++;
		}



		private ImageMatrix ConvolveSeparable (Filter filter) {
			int cols = width;
			int rows = height;
			int kCols = filter.width;
			int kRows = filter.height;
			int kCenterX = kCols / 2;
			int kCenterY = kRows / 2;

			float [,] intermediateData = new float [cols, rows];
			float [,] newData = new float [cols, rows];

			for (int iy = 0; iy < rows; iy++) {
				for (int ix = kCenterX; ix < cols - kCenterX; ix++) {
					intermediateData [ix, iy] = 0;                       // set to zero before sum
					for (int j = 0; j < kCols; j++) {
						intermediateData [ix, iy] += data [ix - j + kCenterX, iy] * filter.xKernel [j];    // convolve: multiply and accumulate
					}
				}
			}
			//return new ImageMatrix (intermediateData);
			for (int ix = 0; ix < cols; ix++) {
				for (int iy = kCenterY; iy < rows - kCenterY; iy++) {
					newData [ix, iy] = 0;                       // set to zero before sum
					for (int j = 0; j < kRows; j++) {
						newData [ix, iy] += intermediateData [ix, iy - j + kCenterY] * filter.yKernel [j];    // convolve: multiply and accumulate
																											  //newData [ix,iy] += data [ix,iy - j] * filter.yKernel[j];   
					}
				}
			}
			return new ImageMatrix (newData);
		}

		private ImageMatrix ConvolveUnseperable (Filter filter) {
			int cols = width;
			int rows = height;
			int kCols = filter.width;
			int kRows = filter.height;
			int kCenterX = kCols / 2;
			int kCenterY = kRows / 2;

			float [,] newData = new float [width, height];

			for (int i = kCenterY; i < rows - kCenterY; ++i)              // rows
			{
				for (int j = kCenterX; j < cols - kCenterX; ++j)          // columns
				{
					float val = 0;
					for (int m = 0; m < kRows; ++m)     // kernel rows
					{
						int mm = kRows - 1 - m;      // row index of flipped kernel

						for (int n = 0; n < kCols; ++n) // kernel columns
						{
							int nn = kCols - 1 - n;  // column index of flipped kernel

							// index of input signal, used for checking boundary
							int ii = i + (m - kCenterY);
							int jj = j + (n - kCenterX);

							// ignore input samples which are out of bound
							//if( ii >= 0 && ii < rows && jj >= 0 && jj < cols )
							val += data [jj, ii] * filter [mm, nn];
						}
					}
					newData [j, i] = val;
				}
			}
			return new ImageMatrix (newData);
		}
		public override string ToString ()
		{
			return string.Format ("[ImageMatrix: width={0}, height={1}]", width, height);
		}

		public ImageMatrix ExtendImage (int borderSize) {
			int newWidth = width + borderSize * 2;
			int newHeight = height + borderSize * 2;
			float [,] newData = new float [newWidth, newHeight];

			//first copy pixels
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					newData [x + borderSize, y + borderSize] = data [x, y];
				}
			}
			//then extend top and bottom
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < borderSize; y++) {
					newData [x + borderSize, y] = data [x, 0];
					newData [x + borderSize, newHeight - y - 1] = data [x, height - 1];
				}
			}
			//then extend left and right
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < borderSize; x++) {
					newData [x, y + borderSize] = data [0, y];
					newData [newWidth - x - 1, y + borderSize] = data [width - 1, y];
				}
			}
			//then extend corners
			for (int y = 0; y < borderSize; y++) {
				for (int x = 0; x < borderSize; x++) {
					newData [x, y] = data [0, 0];
					newData [newWidth - x - 1, y] = data [width - 1, 0];
					newData [newWidth - x - 1, newHeight - y - 1] = data [width - 1, height - 1];
					newData [x, newHeight - y - 1] = data [0, height - 1];
				}
			}

			return new ImageMatrix (newData);
		}
		public ImageMatrix ShrinkImage (int borderSize) {
			int newWidth = width - borderSize * 2;
			int newHeight = height - borderSize * 2;
			float [,] newData = new float [newWidth, newHeight];

			//copy pixels
			for (int x = 0; x < newWidth; x++) {
				for (int y = 0; y < newHeight; y++) {
					newData [x, y] = data [x + borderSize, y + borderSize];
				}
			}

			return new ImageMatrix (newData);
		}
		public static ImageMatrix GetAverageMatrix (List<ImageMatrix> images)
		{
			float [,] newData = new float [images[0].width, images[0].height];
			for (int i = 0; i < images.Count; i++) {
				for (int x = 0; x < images [0].width; x++) {
					for (int y = 0; y < images [0].height; y++) {
						newData [x, y] += images [i].data [x, y];
					}
				}
			}
			for (int x = 0; x < images [0].width; x++) {
				for (int y = 0; y < images [0].height; y++) {
					newData [x, y] /= (float)images.Count;
				}
			}
			return new ImageMatrix (newData);
		}

	}
}

