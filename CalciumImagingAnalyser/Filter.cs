//
//  Filter.cs
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

namespace CalciumImagingAnalyser
{
	public enum FilterType {Gaussian, Ones, Gabor};
	public class Filter
	{
		public int width { get; private set; }
		public int height { get; private set; }
		private float [,] data { get; set; }

		public float[] xKernel { get; private set; }
		public float[] yKernel { get; private set; }

		public float this[int i, int j]{
			get { return data [i, j]; }
		}

		public Filter (FilterType type, float[] args)
		{
			switch (type) {
			case FilterType.Gaussian:
				double sigma = args [0];//All Math functions use doubles
				int size = 6 * ((int)Math.Ceiling(sigma)) - 1;//in practice it's effectively 0 more than 3 standard deviations away (http://homepages.inf.ed.ac.uk/rbf/HIPR2/gsmooth.htm)
				width = size;
				height = size;
				data = new float[size, size];
				for (int x = 0; x < size; x++) {
					double a = x - size / 2d - .5d;//All Math functions use doubles
					for (int y = 0; y < size; y++) {
						double b = y - size / 2d - .5d;
						data [x, y] = (float)((1d / (2d * Math.PI * sigma * sigma)) *
						Math.Exp (-(a * a + b * b) / (2d * sigma * sigma)));
					}
				}

				xKernel = new float[size];
				for (int x = 0; x < size; x++) {
					double a = x - size / 2d + .5d;//All Math functions use doubles
					xKernel [x] = (float)(1d / (sigma * Math.Sqrt (2d * Math.PI)) * Math.Exp (-a * a / (2d * sigma * sigma)));
				}
				yKernel = xKernel;
				break;
			case FilterType.Gabor:
				throw new NotImplementedException ();
			case FilterType.Ones:
				size = (int)args [0];//All Math functions use doubles
				data = new float[size, size];
				for (int x = 0; x < size; x++) {
					for (int y = 0; y < size; y++) {
						data [x, y] = 1f;
					}
				}

				xKernel = new float[size];
				for (int x = 0; x < size; x++) {
					xKernel [x] = 1f;
				}
				yKernel = xKernel;
				break;
			}
		}
	}
}

