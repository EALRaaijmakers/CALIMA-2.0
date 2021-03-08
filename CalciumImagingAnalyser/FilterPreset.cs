//
//  FilterPreset.cs
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
	public class FilterPreset
	{
		public decimal A { get; private set; }
		public decimal B { get; private set; }
		public decimal C { get; private set; }
		public decimal Cap { get; private set; }

		private string Name;

		public FilterPreset (string Name, decimal A, decimal B, decimal C, decimal Cap)
		{
			this.A = A;
			this.B = B;
			this.C = C;
			this.Cap = Cap;
			this.Name = Name;
		}

		public override string ToString ()
		{
			return Name;
		}
	}
}

