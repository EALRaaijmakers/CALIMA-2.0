//
//  Utils.cs
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

namespace CalciumImagingAnalyser
{
	public static class Utils
	{
		public static Font font;
		public static Pen lightGreyPen, darkGreyPen, redPen, lightRedPen, greenPen, lightGreenPen;
		public static SolidBrush greenBrush,yellowBrush;
		static Color[] colorscale = new Color[256];

		static Utils () {
			try {
				Bitmap cols = (Bitmap)Bitmap.FromFile ("colormap.png");
				for (int i = 0; i < 256; i++) {
					colorscale [i] = cols.GetPixel (i, 0);
				}
				font = new Font ("Arial", 10f);
				lightGreyPen = new Pen (Color.LightGray);
				darkGreyPen = new Pen (Color.DarkGray);
				redPen = new Pen (Color.Red);
				lightRedPen = new Pen (Color.LightPink);
				greenPen = new Pen (Color.Green);
				lightGreenPen = new Pen (Color.LightGreen);
				greenBrush = new SolidBrush (Color.FromArgb(96,255,96));
				yellowBrush = new SolidBrush (Color.FromArgb (255, 255, 0));
			} catch (Exception e) {
				MainWindow.ShowMessage ("Error loading colormap.png: " + e.Message);
			}
		}

		/// <summary>
		/// Convert HSV to RGB.
		/// Based upon http://ilab.usc.edu/wiki/index.php/HSV_And_H2SV_Color_Space#HSV_Transformation_C_.2F_C.2B.2B_Code_2
		/// Copyright Chris Hulbert: http://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/
		/// </summary>
		/// <param name="h">Hue, 0-360.</param>
		/// <param name="S">Saturation, 0-1.</param>
		/// <param name="V">Value, 0-1.</param>
		/// <param name="r">The red component.</param>
		/// <param name="g">The green component.</param>
		/// <param name="b">The blue component.</param>
		public static void HsvToRgb(float h, float S, float V, out int r, out int g, out int b)
		{
			// ######################################################################
			// T. Nathan Mundhenk
			// mundhenk@usc.edu
			// C/C++ Macro HSV to RGB

			float H = h;
			while (H < 0) { H += 360; };
			while (H >= 360) { H -= 360; };
			float R, G, B;
			if (V <= 0)
			{ R = G = B = 0; }
			else if (S <= 0)
			{
				R = G = B = V;
			}
			else
			{
				float hf = H / 60.0f;
				int i = (int)Math.Floor(hf);
				float f = hf - i;
				float pv = V * (1 - S);
				float qv = V * (1 - S * f);
				float tv = V * (1 - S * (1 - f));
				switch (i)
				{
				case 0:// Red is the dominant color
					R = V;
					G = tv;
					B = pv;
					break;
				case 1:// Green is the dominant color
					R = qv;
					G = V;
					B = pv;
					break;
				case 2:
					R = pv;
					G = V;
					B = tv;
					break;
				case 3:// Blue is the dominant color
					R = pv;
					G = qv;
					B = V;
					break;
				case 4:
					R = tv;
					G = pv;
					B = V;
					break;
				case 5:// Red is the dominant color
					R = V;
					G = pv;
					B = qv;
					break;
				case 6:// Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.
					R = V;
					G = tv;
					B = pv;
					break;
				case -1:
					R = V;
					G = pv;
					B = qv;
					break;// The color is not defined, we should throw an error.
				default:
					//LFATAL("i Value error in Pixel conversion, Value is %d", i);
					R = G = B = V; // Just pretend its black/white
					break;
				}
			}
			r = Clamp((int)(R * 255.0));
			g = Clamp((int)(G * 255.0));
			b = Clamp((int)(B * 255.0));
		}

		public static Color GetColorScale(byte num) {
			return colorscale [num];
		}

		/// <summary>
		/// Clamp a value to 0-255
		/// </summary>
		public static int Clamp(int i)
		{
			if (i < 0) return 0;
			if (i > 255) return 255;
			return i;
		}

		/// <summary>
		/// Gets the average value.
		/// </summary>
		/// <returns>The average value of all values in the given array.</returns>
		/// <param name="nums">An array of values.</param>
		public static float GetAverage (int[] nums) {
			float ans = 0;
			for (int i = 0; i < nums.Length; i++) {
				ans += nums [i];
			}
			return ans / (float)nums.Length;
		}

		/// <summary>
		/// Gets the average value.
		/// </summary>
		/// <returns>The average value of all values in the given array.</returns>
		/// <param name="nums">An array of values.</param>
		public static float GetAverage (float[] nums) {
			float ans = 0;
			for (int i = 0; i < nums.Length; i++) {
				ans += nums [i];
			}
			return ans;
		}


		/// <summary>
		/// Gets the average value.
		/// </summary>
		/// <returns>The average value of all values in the given array.</returns>
		/// <param name="nums">An array of values.</param>
		public static void GetMinMaxAverage (int[] nums, out float min, out float max, out float avg) {
			float ans = 0;
			min = float.MaxValue;
			max = float.MinValue;
			for (int i = 0; i < nums.Length; i++) {
				if (nums[i] > max) {
					max = nums[i];
				}
				if (nums[i] < min) {
					min = nums[i];
				}
				ans += nums [i];
			}
			avg = ans / (float)nums.Length;
		}

		/// <summary>
		/// Gets the maximum value.
		/// </summary>
		/// <returns>The maximum value of all values in the given array.</returns>
		/// <param name="nums">An array of values.</param>
		public static float GetMax (float[] nums) {
			float ans = float.MinValue;
			for (int i = 0; i < nums.Length; i++) {
				if (nums [i] > ans) {
					ans = nums [i];
				}
			}
			return ans;
		}

		/// <summary>
		/// Gets the distance between two points.
		/// </summary>
		/// <returns>The distance between points 1 and 2.</returns>
		/// <param name="x1">The x coordinate of the first point.</param>
		/// <param name="y1">The y coordinate of the first point.</param>
		/// <param name="x2">The x coordinate of the second point.</param>
		/// <param name="y2">The y coordinate of the second point.</param>
		public static float GetDistance (int x1, int y1, int x2, int y2) {
			float a = x2 - x1;
			float b = y2 - y1;
			return (float)Math.Sqrt (a * a + b * b);
		}

		public static PointF GetCircleLineIntersection (float x1, float y1, float x2, float y2, float radius) {
			float w = x2 - x1;
			float h = y2 - y1;
			//convert to polar coordinates and apply offset
			double angle = Math.Atan2 (h, w);
			double length = Math.Sqrt (w * w + h * h) - radius;
			//and back to cartesian
			float x = x1 + (float)(length * Math.Cos(angle));
			float y = y1 + (float)(length * Math.Sin(angle));
			return new PointF (x, y);
		}

		/// <summary>
		/// Gets the cell peaks.
		/// </summary>
		/// <returns>The cell peaks.</returns>
		/// <param name="data">Data.</param>
		/// <param name="lag">Lag.</param>
		/// <param name="threshold">Threshold.</param>
		/// <param name="influence">Influence.</param>
		/// <param name="peakTreshold">Peak treshold.</param>
		public static int[] GetCellPeaks(float[] data, int lag, float threshold, float influence) {

			int[] signals = new int[data.Length];
			//float influence = 0;
			//http://stackoverflow.com/questions/22583391/peak-signal-detection-in-realtime-timeseries-data
			// Initialise filtered series
			float[] filteredY = new float[data.Length];
			for (int i = 0; i < lag; i++) {
				filteredY[i] = data[i];
			}
			// Initialise filters
			float[] avgFilter = new float[data.Length];
			float[] stdFilter = new float[data.Length];
			float minSD = 1 /(10*threshold); //Prevent divisions by 0 and desensitizes the z-score for extremely low SD.

			//initialize first frames with the first calculable mean and std
			float firstMean = Mean(data, 0, lag);
			float firstStd = Std(data, 0, lag, avgFilter[lag]);
			if (firstStd< minSD) { firstStd = minSD;}

			for (int i = 0; i <= lag; i++) {
				avgFilter[i] = firstMean;
				stdFilter[i] = firstStd;
			}
			//avgFilter [lag] = Mean (data, 0, lag);
			//stdFilter [lag] = Std (data, 0, lag, avgFilter[lag]);
			// Loop over all datapoints y(lag+2),...,y(t)
			// First start with the frames up to lag
			// Start at position 1, as obviously the first frame cannot be detected as a peak (nothing to compare it with)
			for (int i = 1; i <= lag; i++) {
				// If new value is a specified number of deviations away
				if ((data[i] - avgFilter[i - 1]) > (threshold * stdFilter[i - 1]) ) {
					signals[i] = 1;
				} else {
					// No signal
					signals[i] = 0;
				}
			}
			// Now for the other frames (similar except that the mean and SD values now need to be calculated to take into account the peak filtering).
			for (int i = lag + 1; i < data.Length; i++) {
				// If new value is a specified number of deviations away
				if ((data[i] - avgFilter[i - 1]) > (threshold * stdFilter[i - 1])) {
					signals[i] = 1;
					// Make influence lower
					filteredY[i] = influence * data[i] + (1 - influence) * filteredY[i - 1];
				} else {
					// No signal
					signals[i] = 0;
					//we don't want the peaks to spoil our SD, so after a peak use low-pass filtered versions of the signals to calc SD and mean. 
					if (-data[i] - 2*avgFilter[i - 1] > threshold * stdFilter[i - 1]) {
						filteredY[i] = influence * data[i] + (1 - influence) * filteredY[i - 1];
					} else {
						filteredY[i] = data[i];
					}
				}
				// Adjust the filters
				avgFilter[i] = Mean(filteredY, i - lag - 1, lag);
				stdFilter[i] = Std(filteredY, i - lag - 1, lag);
				if (stdFilter[i]< minSD) {stdFilter[i] = minSD;}
			}
			return signals;
		}
		/*
		int[] signals = new int[data.Length];
		// Initialise filtered series
		// Loop over all datapoints y(lag+2),...,y(t)
		// First start with the frames up to lag
		// Start at position 1, as obviously the first frame cannot be detected as a peak (nothing to compare it with)
		for (int i = 1; i < data.Length; i++) {
			// If new value is a specified number of deviations away
			if (data[i] > threshold) {
				signals[i] = 1;
			} else {
				// No signal
				signals[i] = 0;
			}
		}
		return signals;*/


		public static float GetXCorrelation (float[] data1, float[] data2, int offset, bool useMean){
			if (data1 == null || data2 == null) {
				MainWindow.ShowMessage ("Error: first calculate peaks/slopes.");
				return 0f;
			}

			float ans = 0;

			int length = data1.Length - Math.Abs (offset);// - 1;
			//int start = Math.Abs(offset) / 2;

			int offseta, offsetb;

			float mean1 = Mean (data1, 0, data1.Length);
			float length1 = Norm2 (data1, mean1);
			//first test for the special case of an xcorr with itself
			if (data1 == data2 && offset == 0) {
				//the result is always 1 UNLESS the array only has 0's, in which case it's 0 (see below)
				if (length1 == 0) {
					return 0f;
				} else {
					return 1f;
				}
			}
			float mean2 = Mean (data2, 0, data2.Length);
			float length2 = Norm2 (data2, mean2);
			//Test if there's no norm2 0 arrays, which can only happen if all elements equal 0.
			//We can immediately return 0 if so, as the zero's will cancel out any non-zero elements in the other array.
			if (length1 == 0 || length2 == 0) {
				return 0f;
			}

			/*if (useMean) {
				for (int i = 0; i < data1.Length; i++) {
					data1 [i] = (data1 [i] - mean1) / length1;
					data2 [i] = (data2 [i] - mean2) / length2;	
				}
			} else {
				for (int i = 0; i < data1.Length; i++) {
					data1 [i] = data1 [i] / length1;
					data2 [i] = data2 [i] / length2;			
				}
			}*/

			if (offset < 0) {
				offseta = - offset;
				offsetb = 0;
			} else {
				offseta = 0;
				offsetb = offset;
			}

			/*for (int f = 0; f < length; f++) {
				ans += (data1 [f + offseta]) * (data2 [f + offsetb]);
			}*/


			if (useMean) {
				for (int f = 0; f < length; f++) {
					ans += ((data1 [f + offseta] - mean1) / length1) * ((data2 [f + offsetb] - mean2) / length2);
				}
			} else {
				for (int f = 0; f < length; f++) {
					ans += (data1 [f + offseta] / length1) * (data2 [f + offsetb] / length2);
				}
			}



			if (float.IsNaN(ans) || float.IsInfinity(ans))
				return 1f;

			return ans;
		}

		/// <summary>
		/// Calculates the L2-norm of the given array.
		/// </summary>
		public static float Norm2(float[] data) {
			float ans = 0;
			for (int i = 0; i < data.Length; i++) {
				ans += data [i] * data [i];
			}
			return (float)Math.Sqrt (ans);
		}
		/// <summary>
		/// Calculates the L2-norm of the given array.
		/// </summary>
		public static float Norm2(float[] data, float subtractor) {
			float ans = 0;
			float x;
			for (int i = 0; i < data.Length; i++) {
				x = data [i] - subtractor;
				ans += x * x;
			}
			return (float)Math.Sqrt (ans);
		}

		public static float Mean (float[] data, int offset, int length) {
			float ans = 0;
			for (int i = offset; i < offset + length; i ++ ) {
				ans += data [i];
			}
			return ans / (float)length;
		}
		public static float Std (float[] data, int offset, int length) {
			float mean = Mean (data, offset, length);
			return Std (data, offset, length, mean);
		}
		public static float Std (float[] data, int offset, int length, float mean) {
			float ans = 0;
			for (int i = offset; i < offset + length; i ++ ) {
				float a = data [i] - mean;
				ans += a*a;
			}
			return (float)Math.Sqrt (ans / (float)length);
		}
			

		/// <summary>
		/// Sets the <paramref name="max"/> parameter to the maximum of <paramref name="max"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="max">A reference to the value containing the maximum value.</param>
		/// <param name="value">The value to check for.</param>
		public static void SetMax(ref float max, float value) {
			if (value > max) {
				max = value;
			}
		}

		/// <summary>
		/// Sets the <paramref name="min"/> parameter to the minimum of <paramref name="min"/> and <paramref name="value"/>.
		/// </summary>
		/// <param name="min">A reference to the value containing the minimum value.</param>
		/// <param name="value">The value to check for.</param>
		public static void SetMin(ref float min, float value) {
			if (value < min) {
				min = value;
			}
		}


		/*private static float GetSimilarity (int offset, float[] dataset1, float[] dataset2, SimilarityMeasure similarityMeasure) {
			if (dataset1 == null || dataset2 == null) {
				MainWindow.ShowMessage ("Error: first calculate peaks/slopes.");
				return 0f;
			}

			float ans = 0, a = 0, b = 0;

			int length = dataset1.Length - Math.Abs (offset);// - 1;
			//int start = Math.Abs(offset) / 2;

			int offseta, offsetb;

			if (offset < 0) {
				offseta = -offset;
				offsetb = 0;
			} else {
				offseta = 0;
				offsetb = offset;
			}
			//int offseta = -offset / 2;
			//int offsetb = offset / 2;

			switch (similarityMeasure) {

			case SimilarityMeasure.Euclidian:
				for (int f = 0; f < length; f++) {
					a = dataset1 [f + offseta] - dataset2 [f + offsetb];
					ans += a * a;
				}
				ans = (float)Math.Sqrt(ans);
				break;

			case SimilarityMeasure.Intersection:
				for (int f = 0; f < length; f++) {
					ans += Math.Min(dataset1 [f + offseta], dataset2 [f + offsetb]);
				}
				break;

			case SimilarityMeasure.Canberra:
				for (int f = 0; f < length; f++) {
					a = Math.Abs(dataset1 [f + offseta] - dataset2 [f + offsetb]);
					b = dataset1 [f + offseta] + dataset2 [f + offsetb];
					ans += a / b;
				}
				break;

			case SimilarityMeasure.Motyka:
				for (int f = 0; f < length; f++) {
					a += Math.Min (dataset1 [f + offseta], dataset2 [f + offsetb]);
					b += dataset1 [f + offseta] + dataset2 [f + offsetb];
				}
				ans = a / b;
				break;

			case SimilarityMeasure.HarmonicMean:
				for (int f = 0; f < length; f++) {
					a = dataset1 [f + offseta] * dataset2 [f + offsetb];
					b = dataset1 [f + offseta] + dataset2 [f + offsetb];
					ans += a / b;
				}
				ans = 2f * ans;
				break;

			case SimilarityMeasure.SquaredChord:
				for (int f = 0; f < length; f++) {
					ans += (float)Math.Sqrt(dataset1 [f + offseta] * dataset2 [f + offsetb]);
				}
				ans = 2f * ans - 1f;
				break;

			case SimilarityMeasure.InnerProduct:
				for (int f = 0; f < length; f++) {
					a += dataset1 [f + offseta];
					b += dataset2 [f + offsetb];
					ans += dataset1 [f + offseta] * dataset2 [f + offsetb];
				}
				//ans = (2f * ans - 1f) / Math.Max (a, b);
				ans /= Math.Max (a, b);
				break;

			case SimilarityMeasure.Cosine:
				for (int f = 0; f < length; f++) {
					ans += dataset1 [f + offseta] * dataset2 [f + offsetb];
					a += dataset1 [f + offseta] * dataset2 [f + offseta];
					b += dataset1 [f + offsetb] * dataset2 [f + offsetb];
				}
				ans = ans / (float)(Math.Sqrt(a)*Math.Sqrt(b));
				break;
			}
			return ans;

		}*/
	}
}

