//
//  BrainCell.cs
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

namespace CalciumImagingAnalyser
{
	public enum CellMeasuringMode {Average, Maximum}

	public class BrainCell
	{
		public int number { get; protected set; }
		public int x { get; protected set; }

		public int y { get; protected set; }

		public string Name { get { return "ROI" + number.ToString (); } }

		int minx, maxx,miny,maxy;

		protected static Font font = new Font ("Arial", 10f);
		protected static Brush brush = new SolidBrush (Color.Yellow);

		protected List<Point> pixels;
		protected List<Point> edgePixels;

		/// <summary>
		/// Initializes a new instance of the <see cref="CalciumImagingAnalyser.BrainCell"/> class.
		/// </summary>
		/// <param name="pixels">A list of pixels that are contained by this cell.</param>
		/// <param name="number">The index to assign to this cell.</param>
		public BrainCell (List <Point> pixels, int number, List<Point> edgePixels)
		{
			this.pixels = pixels;
			this.edgePixels = edgePixels;
			this.number = number;
			int totalx = 0;
			int totaly = 0;
			minx = int.MaxValue;
			maxx = 0;
			miny = int.MaxValue;
			maxy = 0;
			foreach (Point p in pixels) {
				totalx += p.X;
				totaly += p.Y;
				if (p.X < minx) {
					minx = p.X;
				}
				if (p.Y < miny) {
					miny = p.Y;
				}
				if (p.X > maxx) {
					maxx = p.X;
				}
				if (p.Y > maxy) {
					maxy = p.Y;
				}
			}
			x = (int)(totalx / (float)pixels.Count);
			y = (int)(totaly / (float)pixels.Count);
		}

		/// <summary>
		/// Constructor of the <see cref="CalciumImagingAnalyser.BrainCell"/> class for use by the ValidationCell inherited class.
		/// </summary>
		protected BrainCell (int x, int y, int number) {
			this.x = x;
			this.y = y;
			this.number = number;
		}

		/// <summary>
		/// Checks if a position falls with the area occupied by this cell.
		/// </summary>
		/// <returns><c>true</c>, if (x,y) falls within the area occupied by this cell, <c>false</c> otherwise.</returns>
		/// <param name="x">The x coordinate to check.</param>
		/// <param name="y">The y coordinate to check.</param>
		public bool ContainsPosition (int x, int y) {
			if (x > minx) {//first check if click happens within boundaries.
				if (x < maxx) {
					if (y > miny) {
						if (y < maxy) {
							foreach (Point p in pixels) {//if so, check all pixels for a match.
								if (x == p.X && y == p.Y) {
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// "Lights up" the cell and draws its name, used if the cell is selected.
		/// </summary>
		/// <param name="bmp">The bitmap that will be coloured in (destructive).</param>
		/// <param name="g">The Graphics object associated with the provided bitmap (for drawing the cell name)</param>
		public virtual void LightUp(ref Bitmap bmp, Graphics g) {
			foreach (Point p in pixels) {
				bmp.SetPixel (p.X, p.Y, Color.Yellow);
			}
			g.DrawString (Name, font, brush, minx, maxy);
		}

		/// <summary>
		/// "Lights up" the cell in the provided colour.
		/// </summary>
		/// <param name="colour">The Graphics object associated with the provided bitmap (for drawing the cell name)</param>
		/// <param name="bmp">The bitmap that will be coloured in (destructive).</param>
		public virtual void LightUp(ref Bitmap bmp, Color colour, Graphics g) {
			foreach (Point p in pixels) {
				bmp.SetPixel (p.X, p.Y, colour);
			}
		}

		/// <summary>
		/// Colours the area occupied by the cell.
		/// </summary>
		/// <param name="bmp">The bitmap that will be coloured in (destructive).</param>
		public virtual void Colour(ref Bitmap bmp) {
			foreach (Point p in pixels) {
				Color c = bmp.GetPixel (p.X, p.Y);
				bmp.SetPixel (p.X, p.Y, Color.FromArgb(c.R,0,0));
			}
		}

		public virtual void DrawEdge (ref Bitmap bmp, Color colour)
		{
			foreach (Point p in edgePixels) {
				bmp.SetPixel (p.X, p.Y, colour);
			}
		}
		public virtual void DrawEdge (Graphics gfx, Brush brush, float xScale, float yScale)
		{
			foreach (Point p in edgePixels) {
				gfx.FillRectangle (brush, xScale*p.X, yScale* p.Y, 1, 1);
			}
		}

		/// <summary>
		/// Gets the lightness of the cell at a particular frame. 
		/// </summary>
		/// <returns>The lightness value of the cell.</returns>
		/// <param name="bmp">A bitmap containing the frame data.</param>
		/// <param name="mode">The way to measure the lightness.</param>
		public float GetCellValue (Bitmap bmp, CellMeasuringMode mode) {
			switch (mode) {
			case CellMeasuringMode.Average:
				float total = 0f;
				foreach (Point p in pixels) {
					total += bmp.GetPixel (p.X, p.Y).R;//doesn't really matter which RGB value we take as we ensure it's grayscale on image loading
				}
				return total / (float)pixels.Count;
			case CellMeasuringMode.Maximum:
				float max = 0f;
				foreach (Point p in pixels) {
					Color c = bmp.GetPixel (p.X, p.Y);
					if (c.R > max) {
						max = c.R;
					}
				}
				return max;
			}
			return 0f;
		}

		/// <summary>
		/// Determines whether this instance is in the specified rectangle r.
		/// </summary>
		/// <returns><c>true</c> if any pixel occupied by this instance falls within r; otherwise, <c>false</c>.</returns>
		/// <param name="r">The red component.</param>
		public bool IsInRectangle (Rectangle r) {
			if (r.Right > minx) {//first check if click happens within boundaries.
				if (r.Left < maxx) {
					if (r.Bottom > miny) {
						if (r.Top < maxy) {
							foreach (Point p in pixels) {//if so, check all pixels for a match.
								if (p.X > r.Left && p.X < r.Right && p.Y > r.Top && p.Y < r.Bottom) {
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="CalciumImagingAnalyser.BrainCell"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="CalciumImagingAnalyser.BrainCell"/>.</returns>
		public override string ToString ()
		{
			return string.Format ("{0} ({1};{2})", Name, x, y);
		}

		/// <summary>
		/// Returns the number of pixels
		/// </summary>
		/// <returns>Returns the number of pixels</returns>
		public int numberofpixels() {
			return this.pixels.Count;
        }
	}
}

