/*
 * Ported from: http://geosoft.no/software/region/Rect.java.html
 * 
 * (C) 2004 - Geotechnical Software Services
 * 
 * This code is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public 
 * License as published by the Free Software Foundation; either 
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public 
 * License along with this program; if not, write to the Free 
 * Software Foundation, Inc., 59 Temple Place - Suite 330, Boston, 
 * MA  02111-1307, USA.
 */

using System;

namespace IK.Graphics.Region
{

    /// <summary>
    /// A integer based rectangle. 
    /// <remarks>
    /// <see cref="RectJ"/> and <see cref="Box"/> represents the same concept, 
    /// but their different definition makes them suitable for use in different situations.
    /// </remarks>
    /// </summary>
    public class RectJ
    {
        public int X;
        public int Y;
        public int Height;
        public int Width;

        /// <summary>
        /// Create a rectangle.
        /// </summary>
        /// <param name="x">X coordinate of upper left corner.</param>
        /// <param name="y">Y coordinate of upper left corner.</param>
        /// <param name="width">Width of rectangle.</param>
        /// <param name="height">Height of rectangle.</param>
        public RectJ(int x, int y, int width, int height)
        {
            Set(x, y, width, height);
        }

        /// <summary>
        /// Create a default rectangle.
        /// </summary>
        public RectJ() : this(0, 0, 0, 0)
        {
        }

        /// <summary>
        /// Create a rectangle as a copy of the specified rectangle.
        /// </summary>
        public RectJ(RectJ rectangle) : this(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height)
        {
        }

        /// <summary>
        /// Create a rectnagle based on specified box.
        /// </summary>
        /// <param name="box">Box to create rectangle from.</param>
        public RectJ(Box box): this(box.X1, box.Y1, box.X2 - box.X1, box.Y2 - box.Y1)
        {
        }

        /// <summary>
        /// Copy the specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle to copy.</param>
        public void Copy(RectJ rectangle)
        {
            X = rectangle.X;
            Y = rectangle.Y;
            Width = rectangle.Width;
            Height = rectangle.Height;
        }

        /// <summary>
        /// Clone this rectangle
        /// </summary>
        /// <returns> Clone of this rectangle.</returns>
        public RectJ Clone()
        {
            return new RectJ(X, Y, Width, Height);
        }

        /// <summary>
        /// Return true if this rectangle is empty.
        /// </summary>
        /// <returns>True if this rectangle is empty, false otherwise.</returns>
        public bool IsEmpty()
        {
            return Width <= 0 || Height <= 0;
        }

        /// <summary>
        /// Expand this rectangle the specified amount in each direction.
        /// </summary>
        /// <param name="dx">Amount to expand to left and right.</param>
        /// <param name="dy">Amount to expand on top and bottom.</param>
        public void Expand(int dx, int dy)
        {
            X -= dx;
            Y -= dy;
            Width += dx + dx;
            Height += dy + dy;
        }

        /// <summary>
        /// Set the parameters for this rectangle.
        /// </summary>
        /// <param name="x">X coordinate of upper left corner.</param>
        /// <param name="y">Y coordinate of upper left corner.</param>
        /// <param name="width">Width of rectangle.</param>
        /// <param name="height">Height of rectangle.</param>
        public void Set(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Set this rectangle as extent of specified polyline.
        /// </summary>
        /// <param name="xArray">X coordinates of polyline.</param>
        /// <param name="yArray">Y coordinates of polyline.</param>
        public void Set(int[] xArray, int[] yArray)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;

            int minY = int.MaxValue;
            int maxY = int.MinValue;

            for (int i = 0; i < xArray.Length; i++)
            {
                if (xArray[i] < minX) minX = xArray[i];
                if (xArray[i] > maxX) maxX = xArray[i];

                if (yArray[i] < minY) minY = yArray[i];
                if (yArray[i] > maxY) maxY = yArray[i];
            }

            X = minX;
            Y = minY;

            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
        }

        /// <summary>
        /// Return X coordinate of center of this rectangle.
        /// </summary>
        /// <returns>X coordinate of center of this rectangle.</returns>
        public int GetCenterX()
        {
            return X + (int) Math.Floor(Width/2.0);
        }

        /// <summary>
        /// Return Y coordinate of center of this rectangle.
        /// </summary>
        /// <returns>Y coordinate of center of this rectangle.</returns>
        public int GetCenterY()
        {
            return Y + (int) Math.Floor(Height/2.0);
        }

        /// <summary>
        /// Return a string representation of this rectangle.
        /// </summary>
        /// <returns>String representation of this rectangle.</returns>
        public override string ToString()
        {
            return "Rectangle: X= " + X + " Y=" + Y +
                              " Width=" + Width + " Height=" + Height;
        }

        protected bool Equals(RectJ other)
        {
            return X == other.X && Y == other.Y && Height == other.Height && Width == other.Width;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RectJ) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode*397) ^ Y;
                hashCode = (hashCode*397) ^ Height;
                hashCode = (hashCode*397) ^ Width;
                return hashCode;
            }
        }

        public static bool operator ==(RectJ left, RectJ right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RectJ left, RectJ right)
        {
            return !Equals(left, right);
        }
    }
}