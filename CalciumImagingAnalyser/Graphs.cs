//
//  Graphs.cs
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
using csDelaunay;
using System.Collections.Generic;

namespace CalciumImagingAnalyser
{
	public static class Graphs
	{
		/// <summary>
		/// Gets the Bitmap containing a cross-correlation map of all cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="brainCells">The cells to draw graphs for.</param>
		/// <param name="lag">The lag to calculate the cross-correlation for.</param>
		public static Bitmap GetVoronoiMap (int imagewidth, int imageheight, BrainCell mainCell, List<BrainCell> allCells, float[] set1, float[][] set2, int lag, Font font) {
			List<Vector2f> points = new List<Vector2f> ();
			List<Color> colours = new List<Color> ();
			for (int x = 0; x < allCells.Count; x++) {
				float s = 0;
				s = Utils.GetXCorrelation (set1, set2[x], lag, true);
				if (allCells [x] == mainCell) {
					colours.Add (Color.Black);
				} else {
					colours.Add (Utils.GetColorScale ((byte)((s + 1f) * 255f / 2f)));
				}

				points.Add (new Vector2f (allCells [x].x, allCells [x].y));
			}

			return GetVoronoi (points, colours, imagewidth, imageheight);
		}

		/*
		 * //the old one from CellActivityContainer.cs
		/// <summary>
		/// Gets the Bitmap containing a cross-correlation map of all cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="brainCells">The cells to draw graphs for.</param>
		/// <param name="lag">The lag to calculate the cross-correlation for.</param>
		public Bitmap GetVoronoiMap (int imagewidth, int imageheight, BrainCell mainCell, int lag, SimilaritySource similaritySource) {
			if (mainCell == null) {
				Bitmap bmpa = new Bitmap (256, 256);
				Graphics ga = Graphics.FromImage(bmpa);
				ga.DrawString ("Select a cell", font, new SolidBrush (Color.Red), 0f, 0f);
				ga.Dispose ();
				return bmpa;
			}

			Bitmap bmp = new Bitmap (imagewidth, imageheight);

			//first draw a white background
			Graphics g = Graphics.FromImage(bmp);
			g.Clear (Color.White);

			if (!isFilled) {
				g.Dispose ();
				return bmp;
			}

			float[] set1 = null;

			switch (similaritySource) {
			case SimilaritySource.Peak:
				set1 = cellPeak[cellIndices[mainCell]];
				break;
			case SimilaritySource.RateOfChange:
				set1 = cellROCNormalised[cellIndices[mainCell]];
				break;
			case SimilaritySource.Activity:						
				set1 = cellActivity [cellIndices[mainCell]];
				break;
			}
			float[] set2 = null;
			List<Vector2f> points = new List<Vector2f> ();
			List<Color> colours = new List<Color> ();
			for (int x = 0; x< allCells.Count; x++) {
				float s = 0;
				switch (similaritySource) {
				case SimilaritySource.Peak:
					set2 = cellPeak[x];
					//s = Utils.GetXCorrelation (set1, set2, lag, true);
					break;
				case SimilaritySource.RateOfChange:
					set2 = cellROCNormalised[x];
					//s = Utils.GetXCorrelation (set1, set2, lag, false);
					break;
				case SimilaritySource.Activity:			
					set2 = cellActivity[x];
					break;
				}
				s = Utils.GetXCorrelation (set1, set2, lag, true);
				//float[] set1 = cellPeak[x];
				//float[] set2 = cellPeak[y];
				//float[] set1 = cellActivity[x];
				//float[] set2 = cellActivity[y];
				//int r,gg,b;
				//Utils.HsvToRgb(240f - (1f+s)*120f,1f,1f,out r, out gg, out b);
				//Color c = Color.FromArgb(r,gg,b);
				if (float.IsNaN (s)) {
					Console.WriteLine ("NAN!!!");
				}
				if (allCells [x] == mainCell) {
					colours.Add (Color.Black);
				} else {
					colours.Add (Utils.GetColorScale ((byte)((s + 1f) * 255f / 2f)));
				}

				points.Add (new Vector2f (allCells [x].x, allCells [x].y));


				//	bmp.SetPixel(x,y,c);
				//	bmp.SetPixel(y,x,c);
			}

			Voronoi voronoi = new Voronoi(points,new Rectf(0,0,imagewidth,imageheight));
			Dictionary<Vector2f,Site> sites = voronoi.SitesIndexedByLocation;
			List<Site> allSites = voronoi.AllSites;
			//edges = voronoi.Edges;

			for (int i = 0; i < allSites.Count; i++) {
				Vector2f loc = new Vector2f (allCells [i].x, allCells [i].y);
				Site s = sites [loc];
				//s=allSites[i]
				List<Vector2f> p = s.Region (new Rectf (0f, 0f, imagewidth, imageheight));
				PointF[] points2 = new PointF[p.Count];
				for (int j = 0; j < p.Count; j++) {
					points2 [j] = new PointF (p [j].x, p [j].y);
				}
				g.FillPolygon (new SolidBrush (colours[i]), points2);
			}

			// Dispose of the Graphics object
			g.Dispose ();
			return bmp;
		}
		*/


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
		public static void DrawGraphOnBitmap (float[] data, string title, int width, int height, int yoffset, Graphics g, Color c, int frame, bool commonScale, bool showPeaks) {
			int nFrames = data.Length;
			float xScale = width / (float)nFrames;

			//we only need to draw the bottom line as the top one is already drawn by the previous cell
			g.DrawLine (Utils.lightGreyPen, 0, yoffset + height, width, yoffset + height);
			//next draw the name of the cell.
			g.DrawString (title, Utils.font, new SolidBrush(c), 0, yoffset);

			Pen p = new Pen (c);
			//now for each frame, draw a line from the current value to the next
			for (int f = 0; f < nFrames - 1; f++) {
				int x1 = (int)(f * xScale); 
				//int y1 = yoffset + height - (int)(yScale * (cellActivity [cell, f] - min));
				int y1 = yoffset + height - (int)(data [f]);
				int x2 = (int)((f + 1) * xScale); 
				//int y2 = yoffset + height - (int)(yScale * (cellActivity [cell, f + 1] - min));
				int y2 = yoffset + height - (int)(data [f + 1]);
				g.DrawLine (p, x1, y1, x2, y2);
			}
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
		public static void DrawSpikeTrainOnBitmap (int[] cellPeakFrames, int width, int height, int yoffset, Graphics g, Color c, int frame, bool commonScale, bool showPeaks) {
			int nFrames = cellPeakFrames.Length;
			float xScale = width / (float)nFrames;

			foreach (int x in cellPeakFrames) {
				g.FillRectangle (Utils.yellowBrush, x * xScale, yoffset, Math.Max (1f, xScale), height);
			}
		}

		public static void DrawGlobalSpikeTrainOnBitmap (int[][] cellPeakFrames, Graphics g, int nDrawnCells, int width, int height, int lag, int cellHeight) {
			int nCells = cellPeakFrames.Length;
			if (nCells == 0) {
				DrawErrorImage (g,"Error: no cells detected");
				return;
			}
			int nFrames = cellPeakFrames [0].Length;
			float xScale = width / (float)nFrames;
			SolidBrush greenTransBrush = new SolidBrush (Color.FromArgb (255 / (nDrawnCells + 1), 0, 255, 0));
			SolidBrush transRed = new SolidBrush(Color.FromArgb(64,255,0,0));
			//first draw the area without frames
			g.FillRectangle(transRed,0,0, lag * xScale, nDrawnCells * cellHeight);
			//then draw the peak frames
			//foreach (BrainCell c in brainCells) {
			for (int i = 0; i < cellPeakFrames.Length; i++) {
				foreach (int x in cellPeakFrames[i]) {
					//g.DrawLine (lightGreenPen, x * xScale, 0, x * xScale, brainCells.Count * cellHeight);
					g.FillRectangle (greenTransBrush, x * xScale, 0, Math.Max (1f,xScale), nDrawnCells * cellHeight);
				}
			}
		}


		/// <summary>
		/// Gets the Bitmap containing a heatmap of all cells.
		/// </summary>
		/// <returns>A Bitmap with graphs drawn on it.</returns>
		/// <param name="imagewidth">The width of the returned Bitmap.</param>
		/// <param name="imageheight">The height of the returned Bitmap.</param>
		/// <param name="similaritySource">The data to use.</param>
		public static Bitmap GetHeatMap (int imagewidth, int imageheight, float[][] source, List<BrainCell> allCells) {
			int nCells = source.Length;
			if (nCells == 0) {
				return GetErrorImage (imagewidth, imageheight, "Error: no cells detected");
			}
			int nFrames = source [0].Length;
			float[] act = new float[nCells];

			float max = float.MinValue;
			float min = float.MaxValue;

			for (int i = 0; i < nCells; i++) {
				act[i] = 0f;

				for (int j = 1; j < nFrames; j++) {
					act[i] += Math.Abs(source [i][j] - source [i][j-1]);
				}
				Utils.SetMax (ref max,act[i]);
				Utils.SetMin (ref min,act[i]);
			}

			float scale = max-min;
			for (int i = 0; i < nCells; i++) {
				act [i] -= min;
				act [i] /= scale;
			}

			List<Vector2f> points = new List<Vector2f> (nCells);
			List<Color> colours = new List<Color> ();

			for (int i = 0; i < nCells; i++) {
				colours.Add (Utils.GetColorScale ((byte)(255 * act [i])));
				points.Add (new Vector2f (allCells [i].x, allCells [i].y));
			}

			return GetVoronoi (points, colours, imagewidth, imageheight);
		}

		public static Bitmap GetErrorImage (int width, int height, string message, Font font = null) {
			if (font == null) {
				font = Utils.font;
			}
			Bitmap bmp = new Bitmap (width, height);
			Graphics g = Graphics.FromImage(bmp);
			DrawErrorImage (g, message, font);
			g.Dispose ();
			return bmp;
		}
		private static void DrawErrorImage (Graphics g, string message, Font font = null) {
			g.DrawString (message, font, new SolidBrush (Color.Red), 0f, 0f);
		}

		private static Bitmap GetVoronoi (List<Vector2f> points, List<Color> colours, int width, int height) {

			Voronoi voronoi = new Voronoi(points,new Rectf(0,0,width, height));
			Dictionary<Vector2f,Site> sites = voronoi.SitesIndexedByLocation;
			List<Site> allSites = voronoi.AllSites;
			//edges = voronoi.Edges;
			if (points.Count != allSites.Count) {
				return GetErrorImage (width, height, "Error: Voronoi sites don't match positions");
			}

			Bitmap bmp = new Bitmap (width, height);

			//first draw a white background
			Graphics g = Graphics.FromImage(bmp);
			g.Clear (Color.White);

			for (int i = 0; i < points.Count; i++) {
				Site s = sites [points[i]];
				List<Vector2f> p = s.Region (new Rectf (0f, 0f, width, height));
				PointF[] points2 = new PointF[p.Count];
				for (int j = 0; j < p.Count; j++) {
					points2 [j] = new PointF (p [j].x, p [j].y);
				}
				g.FillPolygon (new SolidBrush (colours[i]), points2);
			}

			// Dispose of the Graphics object
			g.Dispose ();

			return bmp;
		}

	}
}

