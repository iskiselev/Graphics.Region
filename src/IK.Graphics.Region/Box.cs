/*
 * Ported from: http://geosoft.no/software/region/Box.java.html
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

namespace IK.Graphics.Region
{

    /// <summary>
    /// A rectangle defined by its upper left (included) and lower right (not included) corners.
    /// <remarks>
    /// <code>
    ///    1##############
    ///    ###############
    ///    ###############
    ///                   2
    /// </code>
    /// <para>
    /// This corresponds to a <see cref="RectJ"/> of 
    /// <see cref="RectJ.Width"/> = <see cref="X2"/> - <see cref="X1"/> and
    /// <see cref="RectJ.Height"/> = <see cref="Y2"/> - <see cref="Y1"/>.
    /// </para>
    /// <para>
    /// <see cref="RectJ"/> and <see cref="Box"/> represents the same concept, 
    /// but their different definition makes them suitable for use in different situations.
    /// </para>
    /// </remarks>
    /// </summary>
    public class Box
    {

        public int X1;
        public int X2;
        public int Y1;
        public int Y2;

        /// <summary>
        /// Create an empty box.
        /// </summary>
        public Box()
        {
            Set(0, 0, 0, 0);
        }



        /// <summary>
        /// Create a new box as a copy of the specified box.
        /// </summary>
        /// <param name="box">Box to copy.</param>
        public Box(Box box)
        {
            Set(box.X1, box.Y1, box.X2, box.Y2);
        }

        /// <summary>
        /// Create a new box with specified coordinates. The box includes
        /// the (x1,y1) as upper left corner. The lower right corner (x2,y2)
        /// is just outside the box.
        /// </summary>
        /// <param name="x1">X of upper left corner (inclusive).</param>
        /// <param name="y1">Y of upper left corner (inclusive).</param>
        /// <param name="x2">X of lower right corner (not inclusive).</param>
        /// <param name="y2">Y of lower right corner (not inclusive).</param>
        public Box(int x1, int y1, int x2, int y2)
        {
            Set(x1, y1, x2, y2);
        }

        /// <summary>
        /// Create a new box based on the specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle to copy.</param>
        public Box(RectJ rectangle)
        {
            X1 = rectangle.X;
            Y1 = rectangle.Y;
            X2 = rectangle.X + rectangle.Width;
            Y2 = rectangle.Y + rectangle.Height;
        }

        /// <summary>
        /// Copy the specified box.
        /// </summary>
        /// <param name="box">Box to copy.</param>
        public void Copy(Box box)
        {
            Set(box.X1, box.Y1, box.X2, box.Y2);
        }

        /// <summary>
        /// Clone this box.
        /// </summary>
        /// <returns>Clone of this box.</returns>
        public Box Clone()
        {
            return new Box(X1, Y1, X2, Y2);
        }


        /// <summary>
        /// Set the parameters of this box.
        /// </summary>
        /// <param name="x1">X coordinate of upper left corner of box.</param>
        /// <param name="y1">Y coordinate of upper left corner of box.</param>
        /// <param name="x2">X coordinate of lower right corner of box.</param>
        /// <param name="y2">Y coordinate of lower right corner of box.</param>
        public void Set(int x1, int y1, int x2, int y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        /// <summary>
        /// Check if the specified point is inside this box.
        /// </summary>
        /// <param name="x">X coordinate of point to check.</param>
        /// <param name="y">Y coordinate of point to check.</param>
        /// <returns>True if the point is inside this box, false otherwise.</returns>
        public bool IsInside(int x, int y)
        {
            return x >= X1 && x < X2 && y >= Y1 && y < Y2;
        }

        /// <summary>
        /// Return true if this box is inside the specified box.
        /// </summary>
        /// <param name="box">Box to check if this is inside of.</param>
        /// <returns>True if this box in inside the specified box, false otherwise.</returns>
        public bool IsInsideOf(Box box)
        {
            return X1 >= box.X1 && Y1 >= box.Y1 &&
                   X2 <= box.X2 && Y2 <= box.Y2;
        }

        /// <summary>
        /// Return true if this box overlaps the specified box.
        /// </summary>
        /// <param name="box">Box to check if this is inside of.</param>
        /// <returns>True if this box overlaps the specified box, false otherwise.</returns>
        public bool IsOverlapping(Box box)
        {
            return X2 > box.X1 && Y2 > box.Y1 &&
                   X1 < box.X2 && Y1 < box.Y2;
        }

        /// <summary>
        /// Return true if this box overlaps the specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectnagle to check if this is inside of.</param>
        /// <returns>True if this box overlaps the specified rectangle, false otherwise.</returns>
        public bool IsOverlapping(RectJ rectangle)
        {
            return X2 > rectangle.X && X1 < rectangle.X + rectangle.Width &&
                   Y2 > rectangle.Y && Y1 < rectangle.Y + rectangle.Height;
        }

        /// <summary>
        /// Offset this box a specified distance in x and y direction.
        /// </summary>
        /// <param name="dx">Offset in x direction.</param>
        /// <param name="dy">Offset in y direction.</param>
        public void Offset(int dx, int dy)
        {
            X1 += dx;
            Y1 += dy;
            X2 += dx;
            Y2 += dy;
        }

        /// <summary>
        /// Return a string representation of this box.
        /// </summary>
        /// <returns>String representation of this box.</returns>
        public override string ToString()
        {
            return "Box: " + "y1=" + Y1 + " y2=" + Y2 + " x1=" + X1 + " x2=" + X2;
        }

        protected bool Equals(Box other)
        {
            return X1 == other.X1 && X2 == other.X2 && Y1 == other.Y1 && Y2 == other.Y2;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Box) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X1;
                hashCode = (hashCode*397) ^ X2;
                hashCode = (hashCode*397) ^ Y1;
                hashCode = (hashCode*397) ^ Y2;
                return hashCode;
            }
        }

        public static bool operator ==(Box left, Box right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Box left, Box right)
        {
            return !Equals(left, right);
        }
    }
}