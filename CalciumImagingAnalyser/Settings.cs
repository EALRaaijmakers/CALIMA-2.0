//
//  Settings.cs
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
using System.Xml;

namespace CalciumImagingAnalyser
{
	public class Settings
	{
		string filename;
		bool isInitialized;

		bool fullscreen;
		bool showhelp;
		int xres;
		int yres;

		public bool Fullscreen { 
			get {
				if (isInitialized) {
					return fullscreen;
				} else {
					return false;
				}
			}
			set {
				fullscreen = value;
			}
		}
		public bool ShowHelp { 
			get {
				if (isInitialized) {
					return showhelp;
				} else {
					return false;
				}
			}
			set {
				showhelp = value;
			}
		}
		public int XResolution { 
			get {
				if (isInitialized) {
					return xres;
				} else {
					return 1024;
				}
			}
			set {
				xres = value;
			}
		}
		public int YResolution { 
			get {
				if (isInitialized) {
					return yres;
				} else {
					return 768;
				}
			}
			set {
				yres = value;
			}
		}

		public Settings (string filename)
		{
			this.filename = filename;
			isInitialized = false;
			try {
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load (filename);

				fullscreen = bool.Parse(xmlDoc.DocumentElement.GetElementsByTagName ("fullscreen") [0].InnerText);
				showhelp = bool.Parse(xmlDoc.DocumentElement.GetElementsByTagName ("showhelp") [0].InnerText);
				string[] resolution = xmlDoc.DocumentElement.GetElementsByTagName ("resolution") [0].InnerText.Split('x');
				xres = int.Parse (resolution [0]);
				yres = int.Parse (resolution [1]);
				isInitialized = true;
			} catch (Exception e) {
				Console.WriteLine ("Error loading settings.xml: " + e.Message);
			}
		}

		public void Save () {
			if (isInitialized) {
				string n = Environment.NewLine;
				string xml = "﻿<?xml version=\"1.0\" encoding=\"utf-8\"?>" + n +
					"<configuration>" + n +
					"\t<runtime>" + n +
					"\t\t<gcAllowVeryLargeObjects enabled=\"true\" />" + n +
					"\t</runtime>" + n +
					"\t<settings>" + n + "\t\t<fullscreen>" + fullscreen.ToString () + "</fullscreen>" + n + "\t\t<showhelp>" +
				            showhelp.ToString () + "</showhelp>" + n + "\t\t<resolution>" + xres.ToString () + "x" + yres.ToString () +
				            "</resolution>" + n + "\t</settings>" + n +
							"</configuration>";
				System.IO.File.WriteAllText (filename, xml);
			}
		}
	}
}

