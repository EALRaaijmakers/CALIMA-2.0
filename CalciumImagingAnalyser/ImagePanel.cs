//
//  ImagePanel.cs
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
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace CalciumImagingAnalyser
{
	public class ImagePanel:GroupBox
	{
		private PictureBox imageBox;//{ get; private set; }
		public TrackBar frameTrackBar { get; private set; }
		MainWindow mainWindow;
		SettingsPanel settingsPanel;
		PictureBox zoomedImageBox;
		Label frameTrackBarLabel, frameTrackbarLabelText;
		TrackBar zoomLevel, brightnessLevel, contrastLevel, gammaLevel;
		CheckBox layerCellsButton, layerValidationButton, layerBaseButton, layerHeatmapButton;

		GroupBox settingsGroup,layerGroup,otherGroup;

		Button saveImageButton, saveOverlayButton;
		Button zoomImageButtonPlus,zoomImageButtonMinus;

		Label zoomLevelLabel, contrastLabel, brightnessLabel, gammaLabel;

		Panel imageContainer;

		Color backColor;

		Bitmap baseImage;
		ImageMatrixCollection imageMatrixCollection;

		List<BrainCell> selectedCells;

		Point zoomStart;

		float imageZoomLevel = 1f;

		public ImagePanel (MainWindow mainWindow,SettingsPanel settingsPanel)
		{
			this.mainWindow = mainWindow;
			this.settingsPanel = settingsPanel;
			selectedCells = new List<BrainCell> ();
			backColor = Color.Black;
			Text = "Frame data";

			settingsGroup = new GroupBox ();
			settingsGroup.Text = "Zoom";
			Controls.Add (settingsGroup);

			frameTrackbarLabelText = new Label ();
			frameTrackbarLabelText.TextAlign = ContentAlignment.TopCenter;
			frameTrackbarLabelText.Text = "Frame no.:";
			Controls.Add (frameTrackbarLabelText);

			frameTrackBarLabel = new Label ();
			frameTrackBarLabel.TextAlign = ContentAlignment.TopCenter;
			Font labelFont = new Font (Utils.font,FontStyle.Bold);
			frameTrackBarLabel.Font = labelFont;
			frameTrackBarLabel.Text = "0";
			Controls.Add (frameTrackBarLabel);

			frameTrackBar = new TrackBar ();
			frameTrackBar.Minimum = 0;
			frameTrackBar.Maximum = 1;
			frameTrackBar.TickStyle = TickStyle.Both;
			frameTrackBar.ValueChanged += delegate {
				//Rectangle r = frameTrackBar.thumb_pos;
				int v = frameTrackBar.Value ;
				frameTrackBarLabel.Text = v.ToString();
				this.mainWindow.imageMatrixCollection.ChangeFrame();
			};
			Controls.Add (frameTrackBar);


			imageContainer = new Panel ();
			imageContainer.AutoScroll = true;
			imageContainer.AutoSize = false;
			Controls.Add (imageContainer);

			imageBox = new PictureBox ();
			imageBox.SizeMode = PictureBoxSizeMode.Zoom;
			imageContainer.Controls.Add (imageBox);
			imageBox.Click += ImageClicked;
			imageBox.MouseDown += StartZoomDrag;
			imageBox.MouseUp += EndZoomDrag;
			imageBox.Image = Image.FromFile ("background.png");

			zoomImageButtonPlus = new Button ();
			zoomImageButtonPlus.Text = "+";
			zoomImageButtonPlus.Click += delegate {
				ZoomImage(1.5f);
			};
			//zoomImageButtonPlus.BackColor = Color.FromArgb (128, 255, 255, 255);
			Controls.Add (zoomImageButtonPlus);

			zoomImageButtonMinus = new Button ();
			zoomImageButtonMinus.Text = "-";
			zoomImageButtonMinus.Click += delegate {
				ZoomImage(1f/1.5f);
			};
			Controls.Add (zoomImageButtonMinus);

			zoomedImageBox = new PictureBox ();
			zoomedImageBox.BackColor = backColor;
			zoomedImageBox.SizeMode = PictureBoxSizeMode.StretchImage;
			settingsGroup.Controls.Add (zoomedImageBox);

			zoomLevelLabel = new Label ();
			//zoomLevelLabel.AutoSize = true;//to autosize it to the x10 content
			zoomLevelLabel.Text = "x10";
			//zoomLevelLabel.AutoSize = false;
			settingsGroup.Controls.Add (zoomLevelLabel);

			zoomLevel = new TrackBar ();
			zoomLevel.Minimum = 1;
			zoomLevel.Maximum = 10;
			zoomLevel.TickFrequency = 2;
			zoomLevel.TickStyle = TickStyle.BottomRight;
			zoomLevel.ValueChanged += delegate {
				zoomLevelLabel.Text = "x" + zoomLevel.Value.ToString();
			};
			zoomLevel.Value = 2;
			settingsGroup.Controls.Add (zoomLevel);

			layerGroup = new GroupBox ();
			layerGroup.Text = "Layers";
			Controls.Add (layerGroup);

			layerBaseButton = new CheckBox ();
			layerBaseButton.Text = "Base image";
			layerBaseButton.Checked = true;
			layerBaseButton.Enabled = false;
			layerGroup.Controls.Add (layerBaseButton);

			layerCellsButton = new CheckBox ();
			layerCellsButton.Text = "Cells";
			layerCellsButton.Checked = true;
			layerCellsButton.CheckedChanged += delegate {
				UpdateImage();
			};
			layerGroup.Controls.Add (layerCellsButton);

			layerValidationButton = new CheckBox ();
			layerValidationButton.Text = "Validation";
			layerValidationButton.Checked = false;
			layerValidationButton.CheckedChanged += delegate {
				UpdateImage();
			};
			layerGroup.Controls.Add (layerValidationButton);

			layerHeatmapButton = new CheckBox ();
			layerHeatmapButton.Text = "Heatmap";
			layerHeatmapButton.Checked = false;
			layerHeatmapButton.CheckedChanged += delegate {
				UpdateImage();
			};
			layerGroup.Controls.Add (layerHeatmapButton);

			otherGroup = new GroupBox ();
			otherGroup.Text = "Other";
			Controls.Add (otherGroup);

			SaveFileDialog saveImageDialog = new SaveFileDialog ();
			saveImageDialog.DefaultExt = "png";
			saveImageDialog.AddExtension = true;
			saveImageDialog.Filter = "PNG Image|*.png";
			saveImageDialog.InitialDirectory = Directory.GetCurrentDirectory ();

			saveImageButton = new Button ();
			saveImageButton.Text = "Save image";
			otherGroup.Controls.Add (saveImageButton);
			saveImageButton.Click += delegate {
				DialogResult result = saveImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = saveImageDialog.FileName;
					try
					{
						imageBox.Image.Save(filename);
						MainWindow.ShowMessage("Saved current image to " + filename + ".");						
					} catch (IOException e) {
						MainWindow.ShowMessage("Error saving file: " + e.Message);
					}
				}
			};

			saveOverlayButton = new Button ();
			saveOverlayButton.Text = "Save cell overlay";
			otherGroup.Controls.Add (saveOverlayButton);
			saveOverlayButton.Click += delegate {
				DialogResult result = saveImageDialog.ShowDialog(); // Show the dialog.
				if (result == DialogResult.OK) // Test result.
				{
					string filename = saveImageDialog.FileName;
					try
					{
						Bitmap bmp = imageMatrixCollection.GetCellOvelay();//.images[imageMatrixCollection.currentFrame].;
						bmp.Save(filename);
						MainWindow.ShowMessage("Saved current image to " + filename + ".");						
					} catch (IOException e) {
						MainWindow.ShowMessage("Error saving file: " + e.Message);
					}
				}
			};

			imageBox.MouseMove += UpdateZoomedImage;

			brightnessLevel = new TrackBar ();
			brightnessLevel.Minimum = -20;
			brightnessLevel.Maximum = 20;
			brightnessLevel.TickFrequency = 4;
			brightnessLevel.TickStyle = TickStyle.BottomRight;
			brightnessLevel.ValueChanged += delegate {
				UpdateImage();
			};
			brightnessLevel.Value = 0;
			otherGroup.Controls.Add (brightnessLevel);

			contrastLevel = new TrackBar ();
			contrastLevel.Minimum = -20;
			contrastLevel.Maximum = 20;
			contrastLevel.TickFrequency = 4;
			contrastLevel.TickStyle = TickStyle.BottomRight;
			contrastLevel.ValueChanged += delegate {
				UpdateImage();
			};
			contrastLevel.Value = 0;
			otherGroup.Controls.Add (contrastLevel);

			gammaLevel = new TrackBar ();
			gammaLevel.Minimum = 1;
			gammaLevel.Maximum = 40;
			gammaLevel.TickFrequency = 4;
			gammaLevel.TickStyle = TickStyle.BottomRight;
			gammaLevel.ValueChanged += delegate {
				UpdateImage();
			};
			gammaLevel.Value = 20;
			otherGroup.Controls.Add (gammaLevel);

			brightnessLabel = new Label ();
			brightnessLabel.Text = "Brightness";
			otherGroup.Controls.Add (brightnessLabel);

			contrastLabel = new Label ();
			contrastLabel.Text = "Contrast";
			otherGroup.Controls.Add (contrastLabel);

			gammaLabel = new Label ();
			gammaLabel.Text = "Gamma";
			otherGroup.Controls.Add (gammaLabel);

			mainWindow.ToolTip.SetToolTip (gammaLevel, "Set the gamma of the shown image.\nThis is purely for display purposes; this value is not used by the algorithm.");
			mainWindow.ToolTip.SetToolTip (contrastLevel, "Set the contrast of the shown image.\nThis is purely for display purposes; this value is not used by the algorithm.");
			mainWindow.ToolTip.SetToolTip (brightnessLevel, "Set the brightness of the shown image.\nThis is purely for display purposes; this value is not used by the algorithm.");
			mainWindow.ToolTip.SetToolTip (layerBaseButton, "Show the selected frame in the image box on the right.");
			mainWindow.ToolTip.SetToolTip (layerCellsButton, "Show detected ROIs in the image box on the right.");
			mainWindow.ToolTip.SetToolTip (layerHeatmapButton, "Show the difference in brightness between this frame and the previous.");
			mainWindow.ToolTip.SetToolTip (layerValidationButton, "Display the box within which the validation algorithm is run.");
			mainWindow.ToolTip.SetToolTip (saveImageButton, "Save the image as shown to the right.");
			mainWindow.ToolTip.SetToolTip (saveOverlayButton, "Save the detected ROIs as a black-and-white image, which can be loaded using the \"Load image overlay\" button.");

			//set the positions and sizes of all controls
			Resize ();
			ResetZoom ();
		}

		public new void Resize () {
			Rectangle screenRectangle = mainWindow.DisplayRectangle;
			Left = screenRectangle.Left;
			//Width = (screenRectangle.Width * 2)/3;
			Width = screenRectangle.Width - (1024 / 3);
			Height = screenRectangle.Height - settingsPanel.Height;
			Top = settingsPanel.Bottom;

			Rectangle windowRectangle = this.DisplayRectangle;

			settingsGroup.Left = windowRectangle.Left;
			settingsGroup.Top = windowRectangle.Top;
			settingsGroup.Width = 128;

			frameTrackbarLabelText.Left = settingsGroup.Right;
			int ftltw = TextRenderer.MeasureText ("Frame no.:", Utils.font).Width;
			frameTrackbarLabelText.Width = ftltw;

			frameTrackBarLabel.Left = settingsGroup.Right;
			frameTrackBarLabel.Width = ftltw;

			frameTrackBar.Height = 24;
			frameTrackBar.Top = windowRectangle.Bottom - frameTrackBar.Height;
			frameTrackBar.Left = frameTrackBarLabel.Right;
			frameTrackBar.Width = windowRectangle.Width - settingsGroup.Width - ftltw;

			frameTrackbarLabelText.Top = windowRectangle.Bottom - frameTrackBar.Height;
			frameTrackBarLabel.Top = frameTrackbarLabelText.Bottom;

			imageContainer.Width = frameTrackBar.Width;
			imageContainer.Top = windowRectangle.Top;
			imageContainer.Left = settingsGroup.Right;
			imageContainer.Height = frameTrackBar.Top - windowRectangle.Top;

			/*imageBox.Width = frameTrackBar.Width;
			imageBox.Top = windowRectangle.Top;
			imageBox.Left = settingsGroup.Right;
			imageBox.Height = frameTrackBar.Top - windowRectangle.Top;*/

			imageBox.Width = imageContainer.Width;
			imageBox.Height = imageContainer.Height;
			imageBox.Top = 0;
			imageBox.Left = 0;

			zoomImageButtonPlus.Width = 24;
			zoomImageButtonPlus.Height = 24;
			zoomImageButtonPlus.Left = settingsGroup.Right;
			zoomImageButtonPlus.Top = windowRectangle.Top;
			zoomImageButtonPlus.BringToFront ();

			zoomImageButtonMinus.Width = 24;
			zoomImageButtonMinus.Height = 24;
			zoomImageButtonMinus.Left = settingsGroup.Right;
			zoomImageButtonMinus.Top = zoomImageButtonPlus.Bottom;
			zoomImageButtonMinus.BringToFront ();

			zoomedImageBox.Width = settingsGroup.DisplayRectangle.Width;
			zoomedImageBox.Height = zoomedImageBox.Width;
			zoomedImageBox.Top = settingsGroup.DisplayRectangle.Top;
			zoomedImageBox.Left = settingsGroup.DisplayRectangle.Left;

			//Graphics measureGraphics = this.CreateGraphics();
			zoomLevelLabel.Top = zoomedImageBox.Bottom;
			zoomLevelLabel.Width = TextRenderer.MeasureText ("x10", Utils.font).Width + 4;
			zoomLevelLabel.Left = settingsGroup.DisplayRectangle.Right - zoomLevelLabel.Width;

			zoomLevel.Top = zoomedImageBox.Bottom;
			zoomLevel.Left = settingsGroup.DisplayRectangle.Left;
			zoomLevel.Width = zoomLevelLabel.Left - zoomLevel.Left;

			int borderHeight = settingsGroup.Height - settingsGroup.DisplayRectangle.Height;
			settingsGroup.Height = zoomLevel.Bottom - zoomedImageBox.Top + borderHeight;

			layerGroup.Top = settingsGroup.Bottom;
			layerGroup.Width = settingsGroup.Width;
			layerGroup.Left = settingsGroup.Left;

			layerBaseButton.Left = layerGroup.DisplayRectangle.Left;
			layerBaseButton.Top = layerGroup.DisplayRectangle.Top;

			layerCellsButton.Left = layerBaseButton.Left;
			layerCellsButton.Top = layerBaseButton.Bottom;

			layerValidationButton.Left = layerBaseButton.Left;
			layerValidationButton.Top = layerCellsButton.Bottom;

			layerHeatmapButton.Left = layerBaseButton.Left;
			layerHeatmapButton.Top = layerValidationButton.Bottom;

			layerGroup.Height = layerBaseButton.Height + layerCellsButton.Height + layerValidationButton.Height + + layerHeatmapButton.Height + layerGroup.Height - layerGroup.DisplayRectangle.Height;

			otherGroup.Top = layerGroup.Bottom;
			otherGroup.Width = settingsGroup.Width;
			otherGroup.Left = settingsGroup.Left;
			otherGroup.Height = windowRectangle.Bottom - layerGroup.Bottom;

			brightnessLabel.Left = otherGroup.DisplayRectangle.Left;
			brightnessLabel.Top = otherGroup.DisplayRectangle.Top;
			brightnessLabel.Width = otherGroup.DisplayRectangle.Width;

			brightnessLevel.Left = otherGroup.DisplayRectangle.Left;
			brightnessLevel.Top = brightnessLabel.Bottom;
			brightnessLevel.Width = otherGroup.DisplayRectangle.Width;

			contrastLabel.Left = otherGroup.DisplayRectangle.Left;
			contrastLabel.Top = brightnessLevel.Bottom;
			contrastLabel.Width = otherGroup.DisplayRectangle.Width;

			contrastLevel.Left = otherGroup.DisplayRectangle.Left;
			contrastLevel.Top = contrastLabel.Bottom;
			contrastLevel.Width = otherGroup.DisplayRectangle.Width;

			gammaLabel.Left = otherGroup.DisplayRectangle.Left;
			gammaLabel.Top = contrastLevel.Bottom;
			gammaLabel.Width = otherGroup.DisplayRectangle.Width;

			gammaLevel.Left = otherGroup.DisplayRectangle.Left;
			gammaLevel.Top = gammaLabel.Bottom;
			gammaLevel.Width = otherGroup.DisplayRectangle.Width;

			saveImageButton.Left = otherGroup.DisplayRectangle.Left;
			saveImageButton.Top = gammaLevel.Bottom;
			saveImageButton.Width = otherGroup.DisplayRectangle.Width;

			saveOverlayButton.Left = otherGroup.DisplayRectangle.Left;
			saveOverlayButton.Top = saveImageButton.Bottom;
			saveOverlayButton.Width = otherGroup.DisplayRectangle.Width;
		}


		public void SetImageMatrixCollection (ImageMatrixCollection imageMatrixCollection) {
			this.imageMatrixCollection = imageMatrixCollection;
		}

		public void ImageClicked (object o, EventArgs e) {
			MouseEventArgs me = (MouseEventArgs)e;
			int clickx = me.Location.X;
			int clicky = me.Location.Y;
			if (settingsPanel.addCustomCellButton.Checked) {//create 
				mainWindow.imageMatrixCollection.ClickImage (clickx, clicky, true);
			} else {
				mainWindow.imageMatrixCollection.ClickImage (clickx, clicky, false);
			}
			//set focus to the cell listbox so that it responds to a del key press.
			mainWindow.analysisPanel.SetFocus ();
		}

		public void SetBaseImage (Bitmap image){
			baseImage = image;
			//imageBox.Image = image;
			UpdateImage();
		}

		void StartZoomDrag(object sender, MouseEventArgs e){
			if (e.Button == MouseButtons.Right) {
				int clickx = e.Location.X;
				int clicky = e.Location.Y;
				zoomStart = new Point (clickx, clicky);
			}
		}

		void EndZoomDrag(object sender, MouseEventArgs e){
			if (e.Button == MouseButtons.Right) {
				if (zoomStart.X > 0 && zoomStart.Y > 0) {
					int clickx = e.Location.X;
					int clicky = e.Location.Y;
					int w = Math.Abs (clickx - zoomStart.X);
					int h = Math.Abs (clicky - zoomStart.Y);
					if (w > 0 && h > 0) {
						Rectangle r = new Rectangle (zoomStart, new Size (w, h));
						MainWindow.ShowMessage (r.ToString ());
					}
					zoomStart = new Point(-1,-1);
				}
			}
		}

		/// <summary>
		/// Display the shown image, including all cells and stuff.
		/// </summary>
		public void UpdateImage(){
			if (baseImage != null) {//if no image was loaded yet
				Bitmap temp;
				//heatmap layer overrides everything else
				if (layerHeatmapButton.Checked) {
					int nextFrame = (imageMatrixCollection.currentFrame + 1 < imageMatrixCollection.images.Count) ? 
						imageMatrixCollection.currentFrame + 1 : imageMatrixCollection.currentFrame;
					temp = imageMatrixCollection.images [imageMatrixCollection.currentFrame].GetHeatmap (
						                 imageMatrixCollection.images [nextFrame].Bitmap, 10000f);
				} else {
					temp = (Bitmap)baseImage.Clone ();
					Graphics g = null;
					try {
						if (brightnessLevel.Value == 0 && contrastLevel.Value == 0 && gammaLevel.Value != 20) {
							//create our graphics object
							g = Graphics.FromImage (temp);
						} else {

							g = Graphics.FromImage(temp);
							float contrast = contrastLevel.Value / 20f;
							float brightness = brightnessLevel.Value / 20f;
							float gamma = gammaLevel.Value / 20f;
							float add = 0f;
							float mult = 1f;
							if (brightness < 0f) {
								mult *= (1 + brightness);
								add = 0f;
								add = brightness;
							} else {
								add = brightness;
								//mult *= 1 - brightness;
							}
							mult *= (1f+contrast);
							if (contrast < 0) {
								add += -.5f * contrast;
							}
								/*factor = (259 * (contrast + 255)) / (255 * (259 - contrast))
								colour = GetPixelColour(x, y)
								newRed   = Truncate(factor * (Red(colour)   - 128) + 128)
								newGreen = Truncate(factor * (Green(colour) - 128) + 128)
								newBlue  = Truncate(factor * (Blue(colour)  - 128) + 128)
								PutPixelColour(x, y) = RGB(newRed, newGreen, newBlue)*/


							int cont = (int)(contrast * 255);
							int bright = (int)(brightness * 255);
							float factor = (259 * (cont + 255)) / (float)(255 * (259 - cont));
							//mult = (259 * (contrast + 255)) / (255* (259-contrast));
							//add = -.5f;
							ColorMap[] colorMap = new ColorMap[256];
							for (int i = 0; i < 256; i++) {
								colorMap[i] = new ColorMap();
								colorMap[i].OldColor = Color.FromArgb(i,i,i);
								//apply brightness and contrast
								int newColor = (int)(bright + factor * (i - 128) + 128);
								//apply gamma
								newColor = (int)(255 * Math.Pow((newColor/255f),1f/gamma));
									
								if (newColor > 255) {
									newColor = 255;
								}
								if (newColor < 0) {
									newColor = 0;
								}
								colorMap[i].NewColor = Color.FromArgb(newColor,newColor,newColor);
							}

							/*float[][] ptsArray ={
								new float[] {mult, 0, 0, 0, 0}, // scale red
								new float[] {0, mult, 0, 0, 0}, // scale green
								new float[] {0, 0, mult, 0, 0}, // scale blue
								new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
								new float[] {add, add, add, 0, 1}};*/

							ImageAttributes imageAttributes = new ImageAttributes();
							imageAttributes.SetRemapTable(colorMap);
							//imageAttributes.ClearColorMatrix();
							//imageAttributes.SetGamma(gammaLevel.Value + 10f,ColorAdjustType.Bitmap);
							//imageAttributes.SetGamma(10f);// y u no work?
							//imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
							g.DrawImage(temp, new Rectangle(0,0,temp.Width,temp.Height)
								,0,0,temp.Width,temp.Height,
								GraphicsUnit.Pixel, imageAttributes);
							
						}

						if (layerCellsButton.Checked) {
							//draw each and every cell
							foreach (BrainCell c in imageMatrixCollection.detectedCells) {
								c.Colour (ref temp);
							}
							//draw selected cells, if applicable
							foreach (BrainCell c in selectedCells) {
								if (!(c is ValidationCell)) {
									c.LightUp (ref temp, g);
								}
							}
							//draw each and every cell
							foreach (ValidationCell c in imageMatrixCollection.validationCells) {
								c.Colour (ref temp);
							}
						}
						if (layerValidationButton.Checked) {
							//draw validation square
							Graphics.FromImage (temp).DrawRectangle (new Pen (Color.DodgerBlue, 2), settingsPanel.validationRectangle);
							//draw selected cells, if applicable
							foreach (BrainCell c in selectedCells) {
								if (c is ValidationCell) {
									c.LightUp (ref temp, g);
								}
							}
						}
					} finally {
						if (g != null) {
							g.Dispose ();
						}
					}
				}
				imageBox.Image = temp;
				imageContainer.AutoScroll = true;
				//imageContainer.Width = 300;
				//imageBox.SizeMode = PictureBoxSizeMode.Normal;
				imageBox.SizeMode = PictureBoxSizeMode.Zoom;
				//ZoomImage (imageZoomLevel);
				//imageBox.Height = temp.Height;
				//imageBox.Width = temp.Width;
			}
		}

		public void UpdateSelectedCells (List<BrainCell> cells) {
			selectedCells = cells;
			UpdateImage ();
		}

		public void GetClickedCoordinates (int mousex, int mousey, out int x, out int y) {
			if (imageBox.Image != null) {
				float aspectRatio = imageBox.Image.Width / (float)imageBox.Image.Height;
				float pbAspectRatio = imageBox.Width / (float)imageBox.Height;
				float zoomFactor;
				if (pbAspectRatio > aspectRatio) { //picturebox wider than image, so vertical bars at horizontal edges.
					float imageWidth = imageBox.Height * aspectRatio;
					float barWidth = .5f * (imageBox.Width - imageWidth);//two bars
					zoomFactor = imageBox.Image.Height / (float)imageBox.Height;
					//clicky -= imageBox.Top;
					mousex -= (int)(barWidth);
				} else {//picturebox taller than image, so horizontal bars at vertical edges.
					float imageHeight = imageBox.Width / aspectRatio;
					float barHeight = .5f * (imageBox.Height - imageHeight);//two bars
					zoomFactor = imageBox.Image.Width / (float)imageBox.Width;
					//clickx -= imageBox.Left;
					mousey -= (int)(barHeight);
				}
				x = (int)(zoomFactor * mousex);
				y = (int)(zoomFactor * mousey);
			} else {
				x = -1;
				y = -1;
			}
		}

		private void ZoomImage (float zoomLevel) {
			int realTop = -imageContainer.DisplayRectangle.Top;
			int realBottom = realTop + imageContainer.Height;
			int centery = (realTop + realBottom) / 2;

			int realLeft = -imageContainer.DisplayRectangle.Left;
			int realRight = realLeft + imageContainer.Width;
			int centerx = (realLeft + realRight) / 2;

			float percentageFromTop = centery / (float)imageContainer.DisplayRectangle.Height;
			//float topPercentageFromTop = realTop / (float)imageContainer.DisplayRectangle.Height;

			float percentageFromLeft = centerx / (float)imageContainer.DisplayRectangle.Width;
			//float leftPercentageFromLeft = realLeft / (float)imageContainer.DisplayRectangle.Width;

			imageZoomLevel *= zoomLevel;
			if (imageZoomLevel > 10f) {
				imageZoomLevel = 10f;
			}
			float minZoom = Math.Min(imageContainer.Width / (float)imageBox.Image.Width,
				imageContainer.Height / (float)imageBox.Image.Height);
			if (imageZoomLevel < minZoom) {
				imageZoomLevel = minZoom;
			}
			//imageBox.Left = 0;
			//imageBox.Top = 0;
			int newWidth = (int)(imageZoomLevel * imageBox.Image.Width);
			int newHeight = (int)(imageZoomLevel * imageBox.Image.Height);
			if (newWidth > imageContainer.Width) {
				imageBox.Width = newWidth;
			} else {
				imageBox.Width = imageContainer.Width;
			}
			if (newHeight > imageContainer.Height) {
				imageBox.Height = newHeight;
			} else {
				imageBox.Height = imageContainer.Height;
			}

			//MainWindow.ShowMessage (percentageFromTop.ToString("0.00"));
			//int yOffset = (int)((percentageFromTop - topPercentageFromTop) * imageContainer.VerticalScroll.Maximum );
			int yOffset = (int)(.5f*imageContainer.ClientRectangle.Height) - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
			int scrollY = (int)(percentageFromTop * imageContainer.VerticalScroll.Maximum) - yOffset;// - (int)(0.5f * yOffset);

			int xOffset = (int)(.5f*imageContainer.ClientRectangle.Width) - System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
			int scrollX = (int)(percentageFromLeft * imageContainer.HorizontalScroll.Maximum) - xOffset;// - (int)(0.5f * yOffset);
	
			if (imageBox.Height <= imageContainer.Height) {
				imageContainer.VerticalScroll.Value = 0;
				imageBox.Top = 0;
			} else if (scrollY < imageContainer.VerticalScroll.Minimum) {
				imageContainer.VerticalScroll.Value = 0;
			} else if (scrollY + 2*yOffset > imageContainer.VerticalScroll.Maximum) {
				imageContainer.VerticalScroll.Value = imageBox.Height - imageContainer.ClientRectangle.Height + System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
			} else {
				imageContainer.VerticalScroll.Value = scrollY;
			}

			if (imageBox.Width <= imageContainer.Width) {
				imageContainer.HorizontalScroll.Value = 0;
				imageBox.Left = 0;
			} else if (scrollX < imageContainer.HorizontalScroll.Minimum) {
				imageContainer.HorizontalScroll.Value = 0;
			} else if (scrollX + 2*yOffset > imageContainer.HorizontalScroll.Maximum) {
				imageContainer.HorizontalScroll.Value = imageBox.Width - imageContainer.ClientRectangle.Width + System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
			} else {
				imageContainer.HorizontalScroll.Value = scrollX;
			}
			//imageContainer.VerticalScroll.Value = imageContainer.VerticalScroll.Maximum - imageContainer.Height;
			//MainWindow.ShowMessage (work.ToString());
//			imageContainer.VerticalScroll.Value += centery - realTop;
			///int topy = imageContainer.DisplayRectangle.Top;
		//	int offset = (centery - topy) / imageContainer.VerticalScroll.Maximum;
			//imageContainer.VerticalScroll.Value += offset;
		}

		public void ResetZoom () {
			imageZoomLevel = 0f;
			ZoomImage (1f);
			imageZoomLevel = 1f;
		}

		/// <summary>
		/// Updates the zoomed image. Adapted from https://www.codeproject.com/articles/21097/picturebox-zoom
		/// </summary>
		private void UpdateZoomedImage(object sender, MouseEventArgs e)
		{
			int mousex, mousey;
			GetClickedCoordinates (e.X, e.Y, out mousex, out mousey);
			// Calculate the width and height of the portion of the image we want
			// to show in the picZoom picturebox. This value changes when the zoom
			// factor is changed.
			int zoomWidth = zoomedImageBox.Width / zoomLevel.Value;
			int zoomHeight = zoomedImageBox.Height / zoomLevel.Value;

			// Calculate the horizontal and vertical midpoints for the crosshair
			// cursor and correct centering of the new image
			int halfWidth = zoomWidth / 2;
			int halfHeight = zoomHeight / 2;

			// Create a new temporary bitmap to fit inside the picZoom picturebox
			int imageWidth = zoomedImageBox.Width-1;//strangely it doesn't work if it's the same size as the image
			int imageHeight = zoomedImageBox.Height-1;

			int halfImageWidth = imageWidth / 2;
			int halfImageHeight = imageHeight / 2;

			Bitmap tempBitmap = new Bitmap(imageWidth, imageHeight, 
				PixelFormat.Format24bppRgb);

			// Create a temporary Graphics object to work on the bitmap
			Graphics bmGraphics = Graphics.FromImage(tempBitmap);

			// Clear the bitmap with the selected backcolor
			bmGraphics.Clear(backColor);

			// Set the interpolation mode
			bmGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			// Draw the portion of the main image onto the bitmap
			// The target rectangle is already known now.
			// Here the mouse position of the cursor on the main image is used to
			// cut out a portion of the main image.
			bmGraphics.DrawImage (imageBox.Image,
				new Rectangle (0, 0, imageWidth, imageHeight),
				new Rectangle (mousex - halfWidth, mousey - halfHeight, 
					zoomWidth, zoomHeight), GraphicsUnit.Pixel);

			// Draw the bitmap on the picZoom picturebox
			zoomedImageBox.Image = tempBitmap;

			// Draw a crosshair on the bitmap to simulate the cursor position
			bmGraphics.DrawLine(Pens.Black, halfImageWidth + 1, halfImageHeight - 4,
				halfImageWidth + 1, halfImageHeight - 1);
			bmGraphics.DrawLine(Pens.Black, halfImageWidth + 1, halfImageHeight + 6, 
				halfImageWidth + 1, halfImageHeight + 3);
			bmGraphics.DrawLine(Pens.Black, halfImageWidth - 4, halfImageHeight + 1, 
				halfImageWidth - 1, halfImageHeight + 1);
			bmGraphics.DrawLine(Pens.Black, halfImageWidth + 6, halfImageHeight + 1, 
				halfImageWidth + 3, halfImageHeight + 1);

			// Dispose of the Graphics object
			bmGraphics.Dispose();

			// Refresh the picZoom picturebox to reflect the changes
			zoomedImageBox.Refresh();
		}
	}
}

