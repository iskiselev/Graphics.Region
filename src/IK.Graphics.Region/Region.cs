/*
 * Ported from: http://geosoft.no/software/region/Region.java.html
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
using System.Collections.Generic;
using System.Diagnostics;

namespace IK.Graphics.Region
{
    /// <summary>
    /// <para>
    ///  A <see cref="Region"/> is simply an area, as the name implies, and is
    ///  implemented as a so called "y-x-banded" array of rectangles; Each Region
    ///  is made up of a certain number of rectangles sorted by y coordinate first,
    ///  and then by x coordinate.
    /// </para>
    /// <remarks>
    /// <para>
    ///  Furthermore, the rectangles are banded such that every rectangle with a
    ///  given upper-left y coordinate (y1) will have the same lower-right Y
    ///  coordinate (y2) and vice versa. If a rectangle has scanlines in a band,
    ///  it will span the entire vertical distance of the band. This means that
    ///  some areas that could be merged into a taller rectangle will be represented
    ///  as several shorter rectangles to account for shorter rectangles to its
    ///  left or right but within its "vertical scope".
    /// </para>
    /// <para>
    ///  An added constraint on the rectangles is that they must cover as much
    ///  horizontal area as possible. E.g. no two rectangles in a band are allowed
    ///  to touch. Whenever possible, bands will be merged together to cover a
    ///  greater vertical distance (and thus reduce the number of rectangles).
    ///  Two bands can be merged only if the bottom of one touches the top of the
    ///  other and they have rectangles in the same places (of the same width, of
    ///  course). This maintains the y-x-banding.
    /// </para>
    /// <para>
    ///  Region operations includes add (union), subtract, intersect, and
    ///  exclusive-or.
    /// </para>
    /// <para>
    ///  This class corresponds to Region.c of the X11 distribution and the
    ///  implemntation is based on it.
    /// </para>
    /// <para>
    ///  The <see cref="Region"/> is essentially equivalent to an AWT &lt;em&gt;Area&lt;/em&gt;
    ///  but with different back-end implementation. Becnhmarking proves it more
    ///  than 100 times faster than AWT Area for binary CAG operations,
    /// </para>
    /// <para>
    /// Thanks to:
    /// <list type="bullet">
    /// <item>
    /// Bryan Lin @ China Minmetals Corporation - for identifying
    /// synchronization errors when run on the MS WindowsXP platform.
    /// </item>
    /// <item>
    /// Maxim Butov @ Belhard - for identifying error in the
    /// isInside(Rect) method.
    /// </item>
    ///</list>
    /// </para>
    /// </remarks>
    /// </summary>
    public class Region
    {
        private enum Operation
        {
            Union,
            Intersection,
            Substraction,
        }

        private const int InitialSize = 40; // 10 rectangles

        // Temporary working area common for all regions for maximum performance
        private static readonly object SyncRoot = new object();
        private static int[] _gRectangles = new int[InitialSize];
        private static int _gNRectangles;

        private Box _extent;
        private int _nRectangles;
        private int[] _rectangles; // y0,y1,x0,x1,.....

        /// <summary>
        ///  Create an empty region. Corresponds to XCreateRegion of X11.
        /// </summary>
        public Region()
        {
            _extent = new Box(0, 0, 0, 0);
            _rectangles = new int[InitialSize];
            _nRectangles = 0;
        }

        /// <summary>
        /// Create a region constituting of a single rectangle as specified.
        /// </summary>
        /// <param name="rectangle">Rectangle to create region from.</param>
        public Region(RectJ rectangle)
        {
            Set(rectangle);
        }

        /// <summary>
        /// Create a region consisting of one rectangle as specified.
        /// </summary>
        /// <param name="x">X position of upper left corner of rectangle.</param>
        /// <param name="y">Y position of upper left corner of rectangle.</param>
        /// <param name="width">Width of rectangle.</param>
        /// <param name="height">Height of rectangle.</param>
        public Region(int x, int y, int width, int height) : this(new RectJ(x, y, width, height))
        {
        }

        /// <summary>
        /// Create a region consisting of one rectangle as specified.
        /// </summary>
        /// <param name="box">Box specification of rectangle to create region from.</param>
        public Region(Box box) : this(new RectJ(box))
        {
        }

        /// <summary>
        /// Create a region as a copy of the specified region.
        /// </summary>
        /// <param name="region">Region to copy.</param>
        public Region(Region region)
        {
            _extent = new Box();
            _rectangles = new int[region._nRectangles << 2];
            Set(region);
        }

        /// <summary>
        /// Clone this region.
        /// </summary>
        /// <returns>Clone of this region.</returns>
        public Region Clone()
        {
            return new Region(this);
        }

        private static void CheckMemory(Region region, int nRectangles)
        {
            int nEntries = nRectangles << 2;

            if (region == null)
            {
                if (_gRectangles.Length < nEntries)
                {
                    int newSize = nEntries*2;
                    int[] newArray = new int[newSize];
                    Array.Copy(_gRectangles, 0, newArray, 0, _gRectangles.Length);
                    _gRectangles = newArray;
                }
            }
            else
            {
                if (region._rectangles.Length < nEntries)
                {
                    int newSize = nEntries*2;
                    int[] newArray = new int[newSize];
                    Array.Copy(region._rectangles, 0, newArray, 0,
                        region._rectangles.Length);
                    region._rectangles = newArray;
                }
            }
        }

        /// <summary>
        /// Set the content of this region according to the specified region.
        /// </summary>
        /// <param name="region">Region to copy.</param>
        public void Set(Region region)
        {
            _extent.Copy(region._extent);

            CheckMemory(this, region._nRectangles);

            Array.Copy(region._rectangles, 0,
                _rectangles, 0, region._nRectangles << 2);

            _nRectangles = region._nRectangles;
        }

        /// <summary>
        /// Set the content of this region according to the specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle to set region according to.</param>
        public void Set(RectJ rectangle)
        {
            _rectangles = new int[InitialSize];

            if (rectangle.IsEmpty())
            {
                _extent = new Box();
                _nRectangles = 0;
            }
            else
            {
                _extent = new Box(rectangle);
                _rectangles[0] = _extent.Y1;
                _rectangles[1] = _extent.Y2;
                _rectangles[2] = _extent.X1;
                _rectangles[3] = _extent.X2;
                _nRectangles = 1;
            }
        }

        /// <summary>
        /// Clear the region.
        /// </summary>
        public void Clear()
        {
            _nRectangles = 0;
            _extent.Set(0, 0, 0, 0);
        }

        /// <summary>
        /// Return true if the region is empty. Corresponds to XEmptyRegion in X11.
        /// </summary>
        /// <returns> True if the region is empty, false otherwise.</returns>
        public bool IsEmpty()
        {
            return _nRectangles == 0;
        }

        /// <summary>
        /// Offset the entire region a specified distance.
        /// Corresponds to XOffsetRegion in X11.
        /// </summary>
        /// <param name="dx">Offset in x direction.</param>
        /// <param name="dy">Offset in y direction.</param>
        public void Offset(int dx, int dy)
        {
            for (int i = 0; i < _rectangles.Length; i += 4)
            {
                _rectangles[i + 0] += dy;
                _rectangles[i + 1] += dy;
                _rectangles[i + 2] += dx;
                _rectangles[i + 3] += dx;
            }

            _extent.Offset(dx, dy);
        }

        /// <summary>
        ///  Return true if the specified region intersect this region.
        /// </summary>
        /// <param name="region">Region to check against.</param>
        /// <returns>True if the region intersects this one, false otherwise.</returns>
        public bool IsIntersecting(Region region)
        {
            Region r = Clone();
            r.Intersect(region);
            return !r.IsEmpty();
        }

        /// <summary>
        /// Return true if the specified rectangle intersect this region.
        /// </summary>
        /// <param name="rectangle">Rectangle to check against.</param>
        /// <returns>True if the rectangle intersects this, false otherwise.</returns>
        public bool IsIntersecting(RectJ rectangle)
        {
            Region region = new Region(rectangle);
            return IsIntersecting(region);
        }

        /// <summary>
        /// Return true if the specified point is inside this region.
        /// This method corresponds to XPointInRegion in X11.
        /// </summary>
        /// <param name="x">X part of point to check.</param>
        /// <param name="y">Y part of point to check.</param>
        /// <returns>True if the point is inside the region, false otherwise.</returns>
        public bool IsInside(int x, int y)
        {
            if (IsEmpty())
                return false;

            if (!_extent.IsInside(x, y))
                return false;

            int rEnd = _nRectangles << 2;

            // Find correct band
            int i = 0;
            while (i < rEnd && _rectangles[i + 1] < y)
            {
                if (_rectangles[i] > y) return false; // Passed the band
                i += 4;
            }

            // Check each rectangle in the band
            while (i < rEnd && _rectangles[i] <= y)
            {
                if (x >= _rectangles[i + 2] && x < _rectangles[i + 3]) return true;
                i += 4;
            }

            return false;
        }

        /// <summary>
        /// Return true if the specified rectangle is inside this region.
        /// This method corresponds to XRectInRegion in X11.
        /// </summary>
        /// <param name="rectangle">Rectangle to check.</param>
        /// <returns>True if the rectangle is inside this region, false otherwise.</returns>
        public bool IsInside(RectJ rectangle)
        {
            // Trivial reject case 1 
            if (IsEmpty() || rectangle.IsEmpty())
                return false;

            // Trivial reject case 2
            if (!_extent.IsOverlapping(rectangle))
                return false;

            int x1 = rectangle.X;
            int x2 = rectangle.X + rectangle.Width;
            int y1 = rectangle.Y;
            int y2 = rectangle.Y + rectangle.Height;

            int rEnd = _nRectangles << 2;

            // Trivial reject case 3
            if (_rectangles[0] > y1) return false;

            // Loop to start band
            int i = 0;
            while (i < rEnd && _rectangles[i + 1] <= y1)
            {
                i += 4;
                if (_rectangles[i] > y1) return false;
            }

            while (i < rEnd)
            {
                int yTop = _rectangles[i];
                int yBottom = _rectangles[i + 1];

                // Find start rectangle within band
                while (i < rEnd && _rectangles[i + 3] <= x1)
                {
                    i += 4;
                    if (_rectangles[i] > yTop) return false; // Passed the band
                }

                if (i == rEnd) return false;

                // This rectangle must cover the entire rectangle horizontally
                if (x1 < _rectangles[i + 2] || x2 > _rectangles[i + 3]) return false;

                // See if we are done
                if (_rectangles[i + 1] >= y2) return true;

                // Move to next band
                i += 4;
                while (i < rEnd && _rectangles[i] == yTop)
                    i += 4;

                if (i == rEnd) return false;

                if (_rectangles[i] > yBottom) return false;
            }

            return false;
        }

        /// <summary>
        /// Return true if this region is inside of the specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle to check if this is inside of.</param>
        /// <returns>True if this region is inside the specified rectangle, false otherwise.</returns>
        public bool IsInsideOf(RectJ rectangle)
        {
            return Subtract(this, rectangle).IsEmpty();
        }

        /// <summary>
        /// Return the extent of the region.
        /// Correspond to XClipBox in X11.
        /// </summary>
        /// <returns>The extent of this region.</returns>
        public RectJ GetExtent()
        {
            return new RectJ(_extent);
        }

        /// <summary>
        /// Return the number of rectangles in the region. In case the number
        /// is getting very high, the application might choose to call collapse().
        /// </summary>
        /// <returns>Number of rectangles this region consists of.</returns>
        public int GetNRectangles()
        {
            return _nRectangles;
        }


        /// <summary>
        /// Collapse the region into its extent box. Useful if the region becomes
        /// very complex (number of rectangles is getting high) and the client
        /// accepts the (in general) coarser result region.
        /// </summary>
        public void Collapse()
        {
            _rectangles[0] = _extent.Y1;
            _rectangles[1] = _extent.Y2;
            _rectangles[2] = _extent.X1;
            _rectangles[3] = _extent.X2;
            _nRectangles = 1;
        }

        /// <summary>
        /// Perform a logical set operation between this and the specified
        /// region. Corresponds to miRegionOp in Region.c of X11.
        /// </summary>
        /// <param name="region">Region to combine with.</param>
        /// <param name="operationType">Combination operator.</param>
        private void Combine(Region region, Operation operationType)
        {
            // This is the only method (with sub methods) that utilize the
            // common working area gRectangles_. The lock ensures that only
            // one thread access this variable at any time.
            lock (SyncRoot)
            {
                int r1 = 0;
                int r2 = 0;
                int r1End = _nRectangles << 2;
                int r2End = region._nRectangles << 2;

                // Initialize the working region
                _gNRectangles = 0;

                int yTop = 0;
                int yBottom = _extent.Y1 < region._extent.Y1
                    ? _extent.Y1
                    : region._extent.Y1;

                int previousBand = 0;
                int currentBand;

                int r1BandEnd, r2BandEnd;
                int top, bottom;

                // Main loop
                do
                {
                    currentBand = _gNRectangles;

                    // Find end of the current r1 band
                    r1BandEnd = r1 + 4;
                    while (r1BandEnd != r1End &&
                           _rectangles[r1BandEnd] == _rectangles[r1])
                        r1BandEnd += 4;

                    // Find end of the current r2 band
                    r2BandEnd = r2 + 4;
                    while (r2BandEnd != r2End &&
                           region._rectangles[r2BandEnd] == region._rectangles[r2])
                        r2BandEnd += 4;

                    // First handle non-intersection band if any
                    if (_rectangles[r1] < region._rectangles[r2])
                    {
                        top = Math.Max(_rectangles[r1], yBottom);
                        bottom = Math.Min(_rectangles[r1 + 1], region._rectangles[r2]);

                        if (top != bottom)
                            NonOverlap1(_rectangles, r1, r1BandEnd, top, bottom, operationType);

                        yTop = region._rectangles[r2];
                    }
                    else if (region._rectangles[r2] < _rectangles[r1])
                    {
                        top = Math.Max(region._rectangles[r2], yBottom);
                        bottom = Math.Min(region._rectangles[r2 + 1], _rectangles[r1]);

                        if (top != bottom)
                            NonOverlap2(region._rectangles,
                                r2, r2BandEnd, top, bottom, operationType);

                        yTop = _rectangles[r1];
                    }
                    else
                        yTop = _rectangles[r1];

                    // Then coalesce if possible
                    if (_gNRectangles != currentBand)
                        previousBand = CoalesceBands(previousBand, currentBand);
                    currentBand = _gNRectangles;

                    // Check if this is an intersecting band
                    yBottom = Math.Min(_rectangles[r1 + 1], region._rectangles[r2 + 1]);
                    if (yBottom > yTop)
                        Overlap(_rectangles, r1, r1BandEnd,
                            region._rectangles, r2, r2BandEnd,
                            yTop, yBottom, operationType);

                    // Coalesce again
                    if (_gNRectangles != currentBand)
                        previousBand = CoalesceBands(previousBand, currentBand);

                    // If we're done with a band, skip forward in the region to the next band
                    if (_rectangles[r1 + 1] == yBottom) r1 = r1BandEnd;
                    if (region._rectangles[r2 + 1] == yBottom) r2 = r2BandEnd;

                } while (r1 != r1End && r2 != r2End);

                currentBand = _gNRectangles;

                //
                // Deal with whichever region still has rectangles left
                //
                if (r1 != r1End)
                {
                    do
                    {

                        r1BandEnd = r1;
                        while (r1BandEnd < r1End &&
                               _rectangles[r1BandEnd] == _rectangles[r1])
                            r1BandEnd += 4;

                        top = Math.Max(_rectangles[r1], yBottom);
                        bottom = _rectangles[r1 + 1];

                        NonOverlap1(_rectangles, r1, r1BandEnd, top, bottom, operationType);
                        r1 = r1BandEnd;

                    } while (r1 != r1End);
                }
                else if (r2 != r2End)
                {
                    do
                    {

                        r2BandEnd = r2;
                        while (r2BandEnd < r2End &&
                               region._rectangles[r2BandEnd] == region._rectangles[r2])
                            r2BandEnd += 4;

                        top = Math.Max(region._rectangles[r2], yBottom);
                        bottom = region._rectangles[r2 + 1];

                        NonOverlap2(region._rectangles, r2, r2BandEnd, top, bottom,
                            operationType);
                        r2 = r2BandEnd;

                    } while (r2 != r2End);
                }

                // Coalesce again
                if (currentBand != _gNRectangles)
                    CoalesceBands(previousBand, currentBand);

                // Copy the work region into this
                CheckMemory(this, _gNRectangles);
                Array.Copy(_gRectangles, 0, _rectangles, 0, _gNRectangles << 2);
                _nRectangles = _gNRectangles;

            }
        }

        private void NonOverlap1(
            int[] rectangles,
            int r,
            int rEnd,
            int yTop,
            int yBottom,
            Operation operationType
        )
        {
            int i = _gNRectangles << 2;

            if (operationType == Operation.Union ||
                operationType == Operation.Substraction)
            {
                while (r != rEnd)
                {
                    CheckMemory(null, _gNRectangles + 1);

                    _gRectangles[i] = yTop;
                    i++;
                    _gRectangles[i] = yBottom;
                    i++;
                    _gRectangles[i] = rectangles[r + 2];
                    i++;
                    _gRectangles[i] = rectangles[r + 3];
                    i++;
                    _gNRectangles++;
                    r += 4;
                }
            }
        }

        private void NonOverlap2(
            int[] rectangles,
            int r,
            int rEnd,
            int yTop,
            int yBottom,
            Operation operationType
        )
        {
            int i = _gNRectangles << 2;

            if (operationType == Operation.Union)
            {
                while (r != rEnd)
                {
                    CheckMemory(null, _gNRectangles + 1);
                    _gRectangles[i] = yTop;
                    i++;
                    _gRectangles[i] = yBottom;
                    i++;
                    _gRectangles[i] = rectangles[r + 2];
                    i++;
                    _gRectangles[i] = rectangles[r + 3];
                    i++;

                    _gNRectangles++;
                    r += 4;
                }
            }
        }

        private void Overlap(
            int[] rectangles1,
            int r1,
            int r1End,
            int[] rectangles2,
            int r2,
            int r2End,
            int yTop,
            int yBottom,
            Operation operationType)
        {
            int i = _gNRectangles << 2;

            //
            // UNION
            //
            if (operationType == Operation.Union)
            {
                while (r1 != r1End && r2 != r2End)
                {
                    if (rectangles1[r1 + 2] < rectangles2[r2 + 2])
                    {
                        if (_gNRectangles > 0 &&
                            _gRectangles[i - 4] == yTop &&
                            _gRectangles[i - 3] == yBottom &&
                            _gRectangles[i - 1] >= rectangles1[r1 + 2])
                        {
                            if (_gRectangles[i - 1] < rectangles1[r1 + 3])
                                _gRectangles[i - 1] = rectangles1[r1 + 3];
                        }
                        else
                        {
                            CheckMemory(null, _gNRectangles + 1);

                            _gRectangles[i] = yTop;
                            _gRectangles[i + 1] = yBottom;
                            _gRectangles[i + 2] = rectangles1[r1 + 2];
                            _gRectangles[i + 3] = rectangles1[r1 + 3];

                            i += 4;
                            _gNRectangles++;
                        }

                        r1 += 4;
                    }
                    else
                    {
                        if (_gNRectangles > 0 &&
                            _gRectangles[i - 4] == yTop &&
                            _gRectangles[i - 3] == yBottom &&
                            _gRectangles[i - 1] >= rectangles2[r2 + 2])
                        {
                            if (_gRectangles[i - 1] < rectangles2[r2 + 3])
                                _gRectangles[i - 1] = rectangles2[r2 + 3];
                        }
                        else
                        {
                            CheckMemory(null, _gNRectangles + 1);

                            _gRectangles[i] = yTop;
                            _gRectangles[i + 1] = yBottom;
                            _gRectangles[i + 2] = rectangles2[r2 + 2];
                            _gRectangles[i + 3] = rectangles2[r2 + 3];

                            i += 4;
                            _gNRectangles++;
                        }

                        r2 += 4;
                    }
                }

                if (r1 != r1End)
                {
                    do
                    {
                        if (_gNRectangles > 0 &&
                            _gRectangles[i - 4] == yTop &&
                            _gRectangles[i - 3] == yBottom &&
                            _gRectangles[i - 1] >= rectangles1[r1 + 2])
                        {
                            if (_gRectangles[i - 1] < rectangles1[r1 + 3])
                                _gRectangles[i - 1] = rectangles1[r1 + 3];
                        }
                        else
                        {
                            CheckMemory(null, _gNRectangles + 1);

                            _gRectangles[i] = yTop;
                            _gRectangles[i + 1] = yBottom;
                            _gRectangles[i + 2] = rectangles1[r1 + 2];
                            _gRectangles[i + 3] = rectangles1[r1 + 3];

                            i += 4;
                            _gNRectangles++;
                        }

                        r1 += 4;

                    } while (r1 != r1End);
                }
                else
                {
                    while (r2 != r2End)
                    {
                        if (_gNRectangles > 0 &&
                            _gRectangles[i - 4] == yTop &&
                            _gRectangles[i - 3] == yBottom &&
                            _gRectangles[i - 1] >= rectangles2[r2 + 2])
                        {
                            if (_gRectangles[i - 1] < rectangles2[r2 + 3])
                                _gRectangles[i - 1] = rectangles2[r2 + 3];
                        }
                        else
                        {
                            CheckMemory(null, _gNRectangles + 1);

                            _gRectangles[i] = yTop;
                            _gRectangles[i + 1] = yBottom;
                            _gRectangles[i + 2] = rectangles2[r2 + 2];
                            _gRectangles[i + 3] = rectangles2[r2 + 3];

                            i += 4;
                            _gNRectangles++;
                        }

                        r2 += 4;
                    }
                }
            }

            //
            // SUBTRACT
            //
            else if (operationType == Operation.Substraction)
            {
                int x1 = rectangles1[r1 + 2];

                while (r1 != r1End && r2 != r2End)
                {
                    if (rectangles2[r2 + 3] <= x1)
                        r2 += 4;
                    else if (rectangles2[r2 + 2] <= x1)
                    {
                        x1 = rectangles2[r2 + 3];
                        if (x1 >= rectangles1[r1 + 3])
                        {
                            r1 += 4;
                            if (r1 != r1End) x1 = rectangles1[r1 + 2];
                        }
                        else
                            r2 += 4;
                    }
                    else if (rectangles2[r2 + 2] < rectangles1[r1 + 3])
                    {
                        CheckMemory(null, _gNRectangles + 1);

                        _gRectangles[i + 0] = yTop;
                        _gRectangles[i + 1] = yBottom;
                        _gRectangles[i + 2] = x1;
                        _gRectangles[i + 3] = rectangles2[r2 + 2];

                        i += 4;
                        _gNRectangles++;

                        x1 = rectangles2[r2 + 3];
                        if (x1 >= rectangles1[r1 + 3])
                        {
                            r1 += 4;
                            if (r1 != r1End) x1 = rectangles1[r1 + 2];
                            else r2 += 4;
                        }
                    }
                    else
                    {
                        if (rectangles1[r1 + 3] > x1)
                        {
                            CheckMemory(null, _gNRectangles + 1);

                            _gRectangles[i + 0] = yTop;
                            _gRectangles[i + 1] = yBottom;
                            _gRectangles[i + 2] = x1;
                            _gRectangles[i + 3] = rectangles1[r1 + 3];

                            i += 4;
                            _gNRectangles++;
                        }

                        r1 += 4;
                        if (r1 != r1End) x1 = rectangles1[r1 + 2];
                    }
                }
                while (r1 != r1End)
                {
                    CheckMemory(null, _gNRectangles + 1);

                    _gRectangles[i + 0] = yTop;
                    _gRectangles[i + 1] = yBottom;
                    _gRectangles[i + 2] = x1;
                    _gRectangles[i + 3] = rectangles1[r1 + 3];

                    i += 4;
                    _gNRectangles++;

                    r1 += 4;
                    if (r1 != r1End) x1 = rectangles1[r1 + 2];
                }
            }

            //
            // INTERSECT
            //
            else if (operationType == Operation.Intersection)
            {
                while (r1 != r1End && r2 != r2End)
                {
                    int x1 = Math.Max(rectangles1[r1 + 2], rectangles2[r2 + 2]);
                    int x2 = Math.Min(rectangles1[r1 + 3], rectangles2[r2 + 3]);

                    if (x1 < x2)
                    {
                        CheckMemory(null, _gNRectangles + 1);

                        _gRectangles[i] = yTop;
                        _gRectangles[i + 1] = yBottom;
                        _gRectangles[i + 2] = x1;
                        _gRectangles[i + 3] = x2;

                        i += 4;
                        _gNRectangles++;
                    }

                    if (rectangles1[r1 + 3] < rectangles2[r2 + 3]) r1 += 4;
                    else if (rectangles2[r2 + 3] < rectangles1[r1 + 3]) r2 += 4;
                    else
                    {
                        r1 += 4;
                        r2 += 4;
                    }
                }
            }
        }

        /// <summary>
        /// Corresponds to miCoalesce in Region.c of X11.
        /// </summary>
        private int CoalesceBands(int previousBand, int currentBand)
        {
            int r1 = previousBand << 2;
            int r2 = currentBand << 2;
            int rEnd = _gNRectangles << 2;

            // Number of rectangles in prevoius band
            int nRectanglesInPreviousBand = currentBand - previousBand;

            // Number of rectangles in current band
            int nRectanglesInCurrentBand = 0;
            int r = r2;
            int y = _gRectangles[r2];
            while (r != rEnd && _gRectangles[r] == y)
            {
                nRectanglesInCurrentBand++;
                r += 4;
            }

            // If more than one band was added, we have to find the start
            // of the last band added so the next coalescing job can start
            // at the right place.
            if (r != rEnd)
            {
                rEnd -= 4;
                while (_gRectangles[rEnd - 4] == _gRectangles[rEnd])
                    rEnd -= 4;

                currentBand = rEnd >> 2 - _gNRectangles;
                rEnd = _gNRectangles << 2;
            }

            if (nRectanglesInCurrentBand == nRectanglesInPreviousBand &&
                nRectanglesInCurrentBand != 0)
            {

                // The bands may only be coalesced if the bottom of the previous
                // band matches the top of the current.
                if (_gRectangles[r1 + 1] == _gRectangles[r2])
                {

                    // Chek that the bands have boxes in the same places
                    do
                    {
                        if ((_gRectangles[r1 + 2] != _gRectangles[r2 + 2]) ||
                            (_gRectangles[r1 + 3] != _gRectangles[r2 + 3]))
                            return currentBand; // No coalescing

                        r1 += 4;
                        r2 += 4;

                        nRectanglesInPreviousBand--;
                    } while (nRectanglesInPreviousBand != 0);

                    //
                    // OK, the band can be coalesced
                    //

                    // Adjust number of rectangles and set pointers back to start
                    _gNRectangles -= nRectanglesInCurrentBand;
                    r1 -= nRectanglesInCurrentBand << 2;
                    r2 -= nRectanglesInCurrentBand << 2;

                    // Do the merge
                    do
                    {
                        _gRectangles[r1 + 1] = _gRectangles[r2 + 1];
                        r1 += 4;
                        r2 += 4;
                        nRectanglesInCurrentBand--;
                    } while (nRectanglesInCurrentBand != 0);

                    // If only one band was added we back up the current pointer
                    if (r2 == rEnd)
                        currentBand = previousBand;
                    else
                    {
                        do
                        {
                            _gRectangles[r1] = _gRectangles[r2];
                            r1++;
                            r2++;
                        } while (r2 != rEnd);
                    }
                }
            }

            return currentBand;
        }

        /// <summary>
        /// Update region extent based on rectangle values.
        /// </summary>
        private void UpdateExtent()
        {
            if (_nRectangles == 0)
                _extent.Set(0, 0, 0, 0);
            else
            {
                // Y values
                _extent.Y1 = _rectangles[0];
                _extent.Y2 = _rectangles[(_nRectangles << 2) - 3];

                // X values initialize
                _extent.X1 = _rectangles[2];
                _extent.X2 = _rectangles[3];

                // Scan all rectangles for extreme X values
                for (int i = 4; i < _nRectangles << 2; i += 4)
                {
                    if (_rectangles[i + 2] < _extent.X1) _extent.X1 = _rectangles[i + 2];
                    if (_rectangles[i + 3] > _extent.X2) _extent.X2 = _rectangles[i + 3];
                }
            }
        }

        /// <summary>
        /// Union this region with the specified region.
        /// Corresponds to XUnionRegion in X11.
        /// </summary>
        /// <param name="region">Region to union this with.</param>
        public void Union(Region region)
        {
            // Trivial case #1. Region is this or empty
            if (this == region || region.IsEmpty())
                return;

            // Trivial case #2. This is empty
            if (IsEmpty())
            {
                Set(region);
                return;
            }

            // Trivial case #3. This region covers the specified one
            if (_rectangles.Length == 1 && region._extent.IsInsideOf(_extent))
                return;

            // Trivial case #4. The specified region covers this one
            if (region._rectangles.Length == 1 &&
                _extent.IsInsideOf(region._extent))
            {
                Set(region);
                return;
            }

            // Ceneral case
            Combine(region, Operation.Union);

            // Update extent
            _extent.X1 = Math.Min(_extent.X1, region._extent.X1);
            _extent.Y1 = Math.Min(_extent.Y1, region._extent.Y1);
            _extent.X2 = Math.Max(_extent.X2, region._extent.X2);
            _extent.Y2 = Math.Max(_extent.Y2, region._extent.Y2);
        }

        /// <summary>
        /// Union this region with the specified rectangle.
        /// Corresponds to XUnionRectWithRegion in X11.
        /// </summary>
        /// <param name="rectangle">Rectangle to union this with.</param>
        public void Union(RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                return;

            Union(new Region(rectangle));
        }

        /// <summary>
        /// Create a new region as the union between two specified regions.
        /// </summary>
        /// <param name="r1">First region to union.</param>
        /// <param name="r2">Second region to union.</param>
        /// <returns>Union of the two specified regions.</returns>
        public static Region Union(Region r1, Region r2)
        {
            Region region = new Region(r1);
            region.Union(r2);
            return region;
        }

        /// <summary>
        /// Create a new region as the union between a region and a rectangle.
        /// </summary>
        /// <param name="region">Region to union.</param>
        /// <param name="rectangle">Rectangle to intersect with.</param>
        /// <returns>Union of the region and the rectangle.</returns>
        public static Region Union(Region region, RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                return new Region(region);
            else
                return Union(region, new Region(rectangle));
        }

        /// <summary>
        /// Leave this region as the intersection between this region and the specified region.
        /// Corresponds to XIntersectRegion in X11.
        /// </summary>
        /// <param name="region">Region to intersect this with.</param>
        public void Intersect(Region region)
        {
            // Trivial case which results in an empty region
            if (IsEmpty() || region.IsEmpty() ||
                !_extent.IsOverlapping(region._extent))
            {
                Clear();
                return;
            }

            // General case
            Combine(region, Operation.Intersection);

            // Update extent
            UpdateExtent();
        }

        /// <summary>
        /// Leave this region as the intersection between this region and the specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle to intersect this with.</param>
        public void Intersect(RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                Clear();
            else
                Intersect(new Region(rectangle));
        }

        /// <summary>
        /// Create a new region as the intersection between two specified regions.
        /// </summary>
        /// <param name="r1">First region to intersect.</param>
        /// <param name="r2">Second region to intersect.</param>
        /// <returns>Intersection between the two specified regions.</returns>
        public static Region Intersect(Region r1, Region r2)
        {
            Region region = new Region(r1);
            region.Intersect(r2);
            return region;
        }

        /// <summary>
        /// Create a new region as the intersection between a region and a rectangle.
        /// </summary>
        /// <param name="region">Region to intersect.</param>
        /// <param name="rectangle">Rectangle to intersect with.</param>
        /// <returns>Intersection between the region and the rectangle.</returns>
        public static Region Intersect(Region region, RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                return new Region();
            else
                return Intersect(region, new Region(rectangle));
        }

        /// <summary>
        /// Subtract the specified region from this region.
        /// Corresponds to XSubtractRegion in X11.
        /// </summary>
        /// <param name="region">Region to subtract from this region.</param>
        public void Subtract(Region region)
        {
            // Trivial check for non-op
            if (IsEmpty() || region.IsEmpty() ||
                !_extent.IsOverlapping(region._extent))
                return;

            // General case
            Combine(region, Operation.Substraction);

            // Update extent
            UpdateExtent();
        }

        /// <summary>
        /// Subtract the specified rectangle from this region.
        /// </summary>
        /// <param name="rectangle">Rectangle to subtract from this region.</param>
        public void Subtract(RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                return;

            Subtract(new Region(rectangle));
        }

        /// <summary>
        /// Create a new region as the subtraction of one region from another.
        /// </summary>
        /// <param name="r1">Region to subtract from.</param>
        /// <param name="r2">Region to subtract.</param>
        /// <returns>Subtraction of the two specified regions.</returns>
        public static Region Subtract(Region r1, Region r2)
        {
            Region region = new Region(r1);
            region.Subtract(r2);
            return region;
        }

        /// <summary>
        /// Create a new region as the subtraction of a rectangle from a region.
        /// </summary>
        /// <param name="region">Region to subtract from.</param>
        /// <param name="rectangle">Rectangle to subtract.</param>
        /// <returns>Subtraction of the two specified regions.</returns>
        public static Region Subtract(Region region, RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                return new Region(region);
            else
                return Subtract(region, new Region(rectangle));
        }

        /// <summary>
        /// Leave the exclusive-or between this and the specified region in
        /// this region. Corresponds to the XXorRegion in X11.
        /// </summary>
        /// <param name="region">Region to xor this region with.</param>
        public void Xor(Region region)
        {
            Region r = region.Clone();
            r.Subtract(this);
            Subtract(region);
            Union(r);
        }

        /// <summary>
        /// Leave the exclusive-or between this and the specified rectangle in this region.
        /// </summary>
        /// <param name="rectangle">Rectangle to xor this region with.</param>
        public void Xor(RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                Clear();
            else
                Xor(new Region(rectangle));
        }

        /// <summary>
        /// Do an exlusive-or operation between two regions and return the result.
        /// </summary>
        /// <param name="r1">First region to xor.</param>
        /// <param name="r2">Second region to xor.</param>
        /// <returns>Result of operation.</returns>
        public static Region Xor(Region r1, Region r2)
        {
            Region region = new Region(r1);
            region.Xor(r2);
            return region;
        }

        /// <summary>
        /// Do an exlusive-or operation between a regions and a rectangle
        /// and return the result.
        /// </summary>
        /// <param name="region">Region to xor.</param>
        /// <param name="rectangle">Rectangle to xor with.</param>
        /// <returns>Result of operation.</returns>
        public static Region Xor(Region region, RectJ rectangle)
        {
            if (rectangle.IsEmpty())
                return new Region();
            else
                return Xor(region, new Region(rectangle));
        }

        protected bool Equals(Region region)
        {
            if (_nRectangles != region._nRectangles) return false;
            else if (_nRectangles == 0) return true;
            else if (_extent.X1 != region._extent.X1) return false;
            else if (_extent.X2 != region._extent.X2) return false;
            else if (_extent.Y1 != region._extent.Y1) return false;
            else if (_extent.Y2 != region._extent.Y2) return false;
            else
            {
                for (int i = 0; i < _nRectangles << 2; i++)
                    if (_rectangles[i] != region._rectangles[i]) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Region) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_extent != null ? _extent.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ _nRectangles;

                if (_rectangles != null)
                {
                    foreach (var i in _rectangles)
                    {
                        hashCode = (hashCode * 397) ^ i;
                    }
                }
                return hashCode;
            }
        }

        public static bool operator ==(Region left, Region right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Region left, Region right)
        {
            return !Equals(left, right);
        }

        public IEnumerable<RectJ> GetRects()
        {
            var rectJList = new List<RectJ>();
            for (int index = 0; index < _nRectangles; ++index)
                rectJList.Add(new RectJ(new Box(_rectangles[index * 4 + 2], _rectangles[index * 4], _rectangles[index * 4 + 3], _rectangles[index * 4 + 1])));
            return rectJList;
        }

        // DEBUG
        private bool IsExtentCorrect()
        {
            int yMin = 0;
            int yMax = 0;
            int xMin = 0;
            int xMax = 0;

            if (_nRectangles > 0)
            {
                yMin = _rectangles[0];
                yMax = _rectangles[1];
                xMin = _rectangles[2];
                xMax = _rectangles[3];
                for (int i = 4; i < _nRectangles << 2; i += 4)
                {
                    if (_rectangles[i + 0] < yMin) yMin = _rectangles[i + 0];
                    if (_rectangles[i + 1] > yMax) yMax = _rectangles[i + 1];
                    if (_rectangles[i + 2] < xMin) xMin = _rectangles[i + 2];
                    if (_rectangles[i + 3] > xMax) xMax = _rectangles[i + 3];
                }
            }

            if (_extent.X1 != xMin)
            {
                Debug.WriteLine("Extent error x1");
                return false;
            }
            if (_extent.X2 != xMax)
            {
                Debug.WriteLine("Extent error x2");
                return false;
            }
            if (_extent.Y1 != yMin)
            {
                Debug.WriteLine("Extent error y1");
                return false;
            }
            if (_extent.Y2 != yMax)
            {
                Debug.WriteLine("Extent error y2");
                return false;
            }

            return true;
        }

// DEBUG
        private bool IsCoalesced()
        {
            if (_nRectangles < 2) return true;

            int rEnd = _nRectangles << 2;

            int thisBand = 0;
            while (thisBand != rEnd)
            {
                // Find start of next band
                int nextBand = thisBand;
                while (nextBand != rEnd &&
                       _rectangles[nextBand] == _rectangles[thisBand])
                    nextBand += 4;
                if (nextBand == rEnd) return true;

                // Now we have two consecutive bands. See if they touch.
                if (_rectangles[thisBand + 1] == _rectangles[nextBand + 1])
                {

                    // Check the x values
                    int thisY = _rectangles[thisBand];
                    int nextY = _rectangles[nextBand];
                    int i = thisBand;
                    int j = nextBand;

                    while (j != rEnd &&
                           _rectangles[i] == thisY && _rectangles[j] == nextY)
                    {
                        if (_rectangles[i + 2] != _rectangles[j + 2] ||
                            _rectangles[i + 3] != _rectangles[j + 3])
                            break;

                        i += 4;
                        j += 4;
                    }

                    if (_rectangles[i] != thisY && (_rectangles[j] != nextY || j == rEnd))
                        Debug.WriteLine("Coalesce error at Y=" + thisY);
                }

                thisBand = nextBand;
            }

            return true;
        }

// DEBUG
        private bool IsConsistent()
        {
            bool isExtentCorrect = IsExtentCorrect();
            if (!isExtentCorrect) return false;

            if (_nRectangles == 0) return true;

            for (int i = 0; i < _nRectangles; i += 4)
            {
                int y1 = _rectangles[i + 0];
                int y2 = _rectangles[i + 1];
                int x1 = _rectangles[i + 2];
                int x2 = _rectangles[i + 3];

                if (y2 <= y1)
                {
                    Debug.WriteLine("Rectangle error y2 > y1");
                    return false;
                }
                if (x2 <= x1)
                {
                    Debug.WriteLine("Rectangle error x2 > x1");
                    return false;
                }

                if (i + 4 < _nRectangles)
                {
                    int y1next = _rectangles[i + 4];
                    int y2next = _rectangles[i + 5];
                    int x1next = _rectangles[i + 6];
                    int x2next = _rectangles[i + 7];

                    if (y1next < y1)
                    {
                        Debug.WriteLine("Band alignment top error");
                        return false;
                    }

                    if (y1next == y1)
                    {
                        if (y2next != y2)
                        {
                            Debug.WriteLine("Band alignment bottom error");
                            return false;
                        }
                        if (x1next < x2)
                        {
                            Debug.WriteLine("X bands intersect error");
                            return false;
                        }
                        if (x1next == x2)
                        {
                            Debug.WriteLine("X bands touch error");
                            return false;
                        }
                    }
                }
            }

            if (!IsCoalesced())
                return false;

            return true;
        }

// DEBUG
        private void Print()
        {
            Debug.WriteLine("-------------------------------");
            Debug.WriteLine(_extent);
            Debug.WriteLine("nRectangles = " + _nRectangles);
            for (int i = 0; i < _nRectangles; i++)
            {
                var text = "y1=" + _rectangles[i*4 + 0] + ", ";
                text += "y2=" + _rectangles[i*4 + 1] + ", ";
                text += "x1=" + _rectangles[i*4 + 2] + ", ";
                text += "x2=" + _rectangles[i*4 + 3];
                Debug.WriteLine(text);
            }
        }

// DEBUG
        private void printRects()
        {
            var text = string.Empty;
            for (int i = 0; i < _nRectangles << 2; i++)
            {
                if (i%4 == 0 && i != 0) text += "  ";
                text += _rectangles[i];
                if ((i + 1)%4 != 0) text += ',';
            }
            Debug.WriteLine(text);
        }
    }
}