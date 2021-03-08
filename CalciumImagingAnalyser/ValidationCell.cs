//
//  ValidationCell.cs
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

namespace CalciumImagingAnalyser
{
	public class ValidationCell:BrainCell
	{
		int size;
		Color cellColour;

		protected new static Brush brush = new SolidBrush (Color.White);

		public new string Name { get { return "ManualROI" + number.ToString (); } }

		/// <summary>
		/// Initializes a new instance of the <see cref="CalciumImagingAnalyser.ValidationCell"/> class.
		/// </summary>
		/// <param name="x">The x coordinate of the cell.</param>
		/// <param name="y">The y coordinate of the cell.</param>
		/// <param name="number">The index to assign to this cell.</param>
		/// <param name="imageWidth">The width in pixels of the image to draw the ValidationCell on.</param>
		/// <param name="imageHeight">The height in pixels of the image to draw the ValidationCell on.</param>
		/// <param name="size">The diameter in pixels of the ValidationCell.</param>
		public ValidationCell (int x, int y, int number, int imageWidth, int imageHeight, int size):base(x,y,number)
		{
			cellColour = Color.DodgerBlue;//undetected colour

			pixels = new List<Point> ();
			edgePixels = new List<Point> ();
			this.size = size;
			pixels.Add (new Point (x, y));

			int s2 = size / 2;
			//add corresponding pixels, if within image bounds. Cells on the border will have only one pixel (small price to pay for less code).
			if (x > s2 && x < imageWidth - s2 && y > s2 && y < imageHeight - s2) {
				for (int xi = -s2; xi < s2; xi++) {
					for (int yi = -s2; yi < s2; yi++) {
						if (Math.Sqrt (xi * xi + yi * yi) <= s2) {
							pixels.Add (new Point (x +xi, y +yi));
							if (Math.Sqrt (xi * xi + yi * yi)+1 >= s2) {//within one pixel from border: this must be a border pixel
								edgePixels.Add (new Point (x + xi, y + yi));
							}
						}
					}
				}
				/*pixels.Add (new Point (x - 1, y - 1));
				pixels.Add (new Point (x - 1, y));
				pixels.Add (new Point (x - 1, y + 1));
				pixels.Add (new Point (x, y - 1));
				pixels.Add (new Point (x, y + 1));
				pixels.Add (new Point (x + 1, y - 1));
				pixels.Add (new Point (x + 1, y));
				pixels.Add (new Point (x + 1, y + 1));*/
			}
		}

		/// <summary>
		/// Colours the area occupied by the cell.
		/// </summary>
		/// <param name="bmp">The bitmap that will be coloured in (destructive).</param>
		public override void Colour(ref Bitmap bmp) {
			foreach (Point p in pixels) {
				//float mult = (0 + bmp.GetPixel (p.X, p.Y).R) / 255f;
				//Color c = Color.FromArgb ((int)(mult * cellColour.R), (int)(mult * cellColour.G), (int)(mult * cellColour.B));
				bmp.SetPixel (p.X, p.Y, cellColour);
			}
		}

		/// <summary>
		/// Sets the colour of the cell to whether it's got a matching detection.
		/// </summary>
		/// <param name="hasMatch">If set to <c>true</c>, it has a match.</param>
		public void SetDetectedState (bool hasMatch) {
			if (hasMatch) {
				cellColour = Color.Green;
			} else {
				cellColour = Color.OrangeRed;
			}
		}

		/// <summary>
		/// Checks whether this ValidationCell matches the given BrainCell
		/// </summary>
		/// <returns><c>true</c>, if the given BrainCell overlaps with this ValidationCell, <c>false</c> otherwise.</returns>
		/// <param name="c">The BrainCell to check for.</param>
		public bool MatchesCell (BrainCell c) {
			//check all the pixels, if it's a match, return true.
			foreach (Point p in pixels) {
				if (c.ContainsPosition (p.X, p.Y)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// "Lights up" the cell, used if the cell is selected.
		/// </summary>
		/// <param name="bmp">The bitmap that will be coloured in (destructive).</param>
		/// <param name="g">The Graphics object associated with the provided bitmap (for drawing the cell name)</param>
		public override void LightUp(ref Bitmap bmp, Graphics g) {
			foreach (Point p in pixels) {
				bmp.SetPixel (p.X, p.Y, Color.White);
			}
			g.DrawString (Name, font, brush, x - 1, y + 2);
		}

		public override string ToString ()
		{
			return string.Format ("{0} ({1},{2})",Name,x.ToString(),y.ToString());
		}
	}
}

