﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Duality.Resources;
using Duality.Plugins.Tilemaps;


namespace Duality.Editor.Plugins.Tilemaps
{
	public class SourcePaletteTilesetView : TilesetView
	{
		private Rectangle  selectedArea           = Rectangle.Empty;
		private Grid<Tile> selectedTiles          = new Grid<Tile>();
		private Point      actionBeginTilePos     = Point.Empty;
		private bool       isUserSelecting        = false;

		public event EventHandler SelectedAreaChanged = null;
		public event EventHandler SelectedAreaEditingFinished = null;


		public Rectangle SelectedArea
		{
			get { return this.selectedArea; }
			set
			{
				if (this.selectedArea != value)
				{
					Rectangle croppedArea = new Rectangle(
						Math.Max(value.X, 0),
						Math.Max(value.Y, 0),
						Math.Min(value.Width, this.TileCount.X - Math.Max(value.X, 0)),
						Math.Min(value.Height, this.TileCount.Y - Math.Max(value.Y, 0)));

					this.selectedArea = croppedArea;
					this.selectedTiles.ResizeClear(croppedArea.Width, croppedArea.Height);
					for (int y = 0; y < croppedArea.Height; y++)
					{
						for (int x = 0; x < croppedArea.Width; x++)
						{
							this.selectedTiles[x, y] = new Tile { Index = this.GetTileIndex(croppedArea.X + x, croppedArea.Y + y) };
						}
					}

					this.Invalidate();
					this.RaiseSelectedAreaChanged();
				}
			}
		}
		public IReadOnlyGrid<Tile> SelectedTiles
		{
			get { return this.selectedTiles; }
		}


		protected override void OnTilesetChanged()
		{
			base.OnTilesetChanged();
			this.SelectedArea = Rectangle.Empty;
			this.RaiseSelectedAreaEditingFinished();
		}
		protected override void OnPaintTiles(PaintEventArgs e)
		{
			Tileset tileset = this.TargetTileset.Res;
			Color highlightColor = Color.White;
			Color highlightBorderColor = Color.Black;
			Region regularClip = e.Graphics.Clip;
			Region selectionClip = regularClip.Clone();

			// Fill the selection background
			if (this.Enabled && !this.selectedArea.IsEmpty)
			{
				int startIndex = this.GetTileIndex(this.selectedArea.X, this.selectedArea.Y);
				Rectangle rect = this.GetDrawingTileRect(startIndex, this.selectedArea.Width, this.selectedArea.Height, -1);
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(32, highlightBorderColor)), rect);
				rect.Inflate(1, 1);
				selectionClip.Exclude(rect);
				e.Graphics.Clip = selectionClip;
			}

			// Draw hovered tile background
			if (this.Enabled && this.HoveredTileIndex != -1)
			{
				Rectangle rect = this.GetDrawingTileRect(this.HoveredTileIndex, 1, 1, -1);
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(32, highlightBorderColor)), rect);
			}

			// Paint the tile layer itself
			e.Graphics.Clip = regularClip;
			base.OnPaintTiles(e);
			e.Graphics.Clip = selectionClip;

			// Draw hovered tile foreground
			if (this.Enabled && this.HoveredTileIndex != -1)
			{
				Rectangle rect = this.GetDrawingTileRect(this.HoveredTileIndex, 1, 1, -1);

				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(32, highlightBorderColor)), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(highlightBorderColor), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(highlightColor), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(highlightBorderColor), rect);
			}

			// Draw selection indicators
			if (this.Enabled && !this.selectedArea.IsEmpty)
			{
				e.Graphics.Clip = regularClip;

				int startIndex = this.GetTileIndex(this.selectedArea.X, this.selectedArea.Y);
				Rectangle rect = this.GetDrawingTileRect(startIndex, this.selectedArea.Width, this.selectedArea.Height, 0);
				Point startPos = this.GetTileIndexLocation(startIndex);

				// Draw the selected tile area border
				e.Graphics.DrawRectangle(new Pen(highlightBorderColor), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(highlightColor), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(highlightColor), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(highlightBorderColor), rect);
				rect.Inflate(1, 1);

				// Draw the outer shadow of the selected tile area
				e.Graphics.DrawRectangle(new Pen(Color.FromArgb(128, highlightBorderColor)), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(Color.FromArgb(64, highlightBorderColor)), rect);
				rect.Inflate(1, 1);
				e.Graphics.DrawRectangle(new Pen(Color.FromArgb(32, highlightBorderColor)), rect);
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left)
			{
				int tileIndex = this.PickTileIndexAt(e.X, e.Y);
				if (tileIndex != -1)
				{
					this.actionBeginTilePos = this.GetTilePos(tileIndex);
					this.isUserSelecting = true;
					this.SelectedArea = new Rectangle(this.actionBeginTilePos.X, this.actionBeginTilePos.Y, 1, 1);
					this.HoveredTileIndex = -1;
				}
			}
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (!this.isUserSelecting)
			{
				this.SelectedArea = Rectangle.Empty;
			}
			this.actionBeginTilePos = Point.Empty;
			this.isUserSelecting = false;
			this.RaiseSelectedAreaEditingFinished();
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			int tileIndex = this.PickTileIndexAt(e.X, e.Y);
			if (this.isUserSelecting)
			{
				if (tileIndex != -1)
				{
					Point tilePos = this.GetTilePos(tileIndex);
					Point selectionTopLeft = new Point(
						Math.Min(this.actionBeginTilePos.X, tilePos.X), 
						Math.Min(this.actionBeginTilePos.Y, tilePos.Y));
					Point selectionBottomRight = new Point(
						Math.Max(this.actionBeginTilePos.X, tilePos.X), 
						Math.Max(this.actionBeginTilePos.Y, tilePos.Y));
					this.SelectedArea = new Rectangle(
						selectionTopLeft.X,
						selectionTopLeft.Y,
						selectionBottomRight.X - selectionTopLeft.X + 1,
						selectionBottomRight.Y - selectionTopLeft.Y + 1);
				}
			}
			else
			{
				base.OnMouseMove(e);
			}
		}
		
		private Rectangle GetDrawingTileRect(int tileIndex, int tileW, int tileH, int offset)
		{
			Point startPos = this.GetTileIndexLocation(tileIndex);
			return new Rectangle(
				startPos.X - 1 - offset, 
				startPos.Y - 1 - offset,
				tileW * (this.TileSize.Width + this.Spacing.Width) + offset * 2, 
				tileH * (this.TileSize.Height + this.Spacing.Height) + offset * 2);
		}

		private void RaiseSelectedAreaEditingFinished()
		{
			if (this.SelectedAreaEditingFinished != null)
				this.SelectedAreaEditingFinished(this, EventArgs.Empty);
		}
		private void RaiseSelectedAreaChanged()
		{
			if (this.SelectedAreaChanged != null)
				this.SelectedAreaChanged(this, EventArgs.Empty);
		}
	}
}