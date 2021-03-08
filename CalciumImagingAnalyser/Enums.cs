//
//  Enums.cs
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
	public enum GraphType {Activity};
	public enum SimilaritySource {Peak, Activity};
	//public enum SimilarityMeasure {Euclidian, Intersection, Canberra, Motyka, HarmonicMean, SquaredChord, InnerProduct, Cosine	};
	public enum SimilarityMeasure {SingleCellXCorr, SquareMapXCorr, BarGraphXCorr, Heatmap};
	public enum PeakMode {Block, Slope, Peak};
	public enum ActivityDisplayMode {Average,MinMaxAverage};
}

