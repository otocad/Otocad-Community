using LitMath;
using System;

namespace lcdb
{
    /// <summary>
    /// 矩形包围框
    /// </summary>
    public struct Bounding
    {
        /// <summary>
        /// Center
        /// </summary>
        public LitMath.Vector2 center
        {
            get
            { 
                return new LitMath.Vector2(
                    (_left + _right) / 2.0,
                    (_bottom + _top) / 2.0); 
            }
        }

        /// <summary>
        /// Width
        /// </summary>
        private double _width;
        public double width
        {
            get { return _width; }
            //set { _width = value; }
        }

        /// <summary>
        /// Height
        /// </summary>
        private double _height;
        public double height
        {
            get { return _height; }
            //set { _height = value; }
        }

        /// <summary>
        /// Check if the bounding box is valid (has non-zero dimensions)
        /// </summary>
        public bool IsValid
        {
            get { return _width >= 0 && _height >= 0 && (_width > 0 || _height > 0); }
        }

        /// <summary>
        /// Left
        /// </summary>
        private double _left;
        public double left
        {
            get { return _left; }
        }

        /// <summary>
        /// Right
        /// </summary>
        private double _right;
        public double right
        {
            get { return _right; }
        }

        /// <summary>
        /// Top
        /// </summary>
        private double _top;
        public double top
        {
            get { return _top; }
        }

        /// <summary>
        /// Bottom
        /// </summary>
        private double _bottom;
   

        public double bottom
        {
            get { return _bottom; }
        }

        /// <summary>
        /// Minimum point (bottom-left corner)
        /// </summary>
        public LitMath.Vector2 minPoint
        {
            get { return new LitMath.Vector2(_left, _bottom); }
        }

        /// <summary>
        /// Maximum point (top-right corner)
        /// </summary>
        public LitMath.Vector2 maxPoint
        {
            get { return new LitMath.Vector2(_right, _top); }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Bounding(LitMath.Vector2 point1, LitMath.Vector2 point2)
        {
            if (point1.X < point2.X)
            {
                _left = point1.X;
                _right = point2.X;
            }
            else
            {
                _left = point2.X;
                _right = point1.X;
            }

            if (point1.Y < point2.Y)
            {
                _bottom = point1.Y;
                _top = point2.Y;
            }
            else
            {
                _bottom = point2.Y;
                _top = point1.Y;
            }

            _width = _right - _left;
            _height = _top - _bottom;
      
        }

        public Bounding(LitMath.Vector2 center, double width, double height)
        {
            _left = center.X - width / 2.0;
            _right = center.X + width / 2.0;
            _bottom = center.Y - height / 2.0;
            _top = center.Y + height / 2.0;
            _width = _right - _left;
            _height = _top - _bottom;
        }

        public Bounding(double centerX, double centerY, double width, double height)
        {
            _left = centerX - width / 2.0;
            _right = centerX + width / 2.0;
            _bottom = centerY - height / 2.0;
            _top = centerY + height / 2.0;
            _width = _right - _left;
            _height = _top - _bottom;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public Bounding(Bounding other)
        {
            _left = other._left;
            _right = other._right;
            _bottom = other._bottom;
            _top = other._top;
            _width = other._width;
            _height = other._height;
        }

        /// <summary>
        /// Check whether contains bounding
        /// </summary>
        public bool Contains(Bounding bounding)
        {
            return this.Contains(bounding.left, bounding.bottom)
                && this.Contains(bounding.right, bounding.top);
        }

        /// <summary>
        /// Check whether contains point
        /// </summary>
        public bool Contains(LitMath.Vector2 point)
        {
            return this.Contains(point.X, point.Y);
        }

        /// <summary>
        /// Check whether contains point: (X, Y)
        /// </summary>
        public bool Contains(double x, double y)
        {
            return x >= this.left
                && x <= this.right
                && y >= this.bottom
                && y <= this.top;
        }

        /// <summary>
        /// Check whether intersect with bounding
        /// </summary>
        public bool IntersectWith(Bounding bounding)
        {
            bool b1 = (bounding.left >= this.left && bounding.left <= this.right)
                || (bounding.right >= this.left && bounding.right <= this.right)
                || (bounding.left <= this.left && bounding.right >= this.right);

            if (b1)
            {
                bool b2 = (this.bottom >= bounding.bottom && this.bottom <= bounding.top)
                    || (this.top >= bounding.bottom && this.top <= bounding.top)
                    || (this.bottom <= bounding.bottom && this.top >= bounding.top);
                if (b2)
                {
                    return true;
                }
            }
            

            return false;
        }

        private bool ValueInRange(double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Union this bounding box with another
        /// </summary>
        public void Union(Bounding other)
        {
            _left = Math.Min(_left, other._left);
            _right = Math.Max(_right, other._right);
            _bottom = Math.Min(_bottom, other._bottom);
            _top = Math.Max(_top, other._top);
            _width = _right - _left;
            _height = _top - _bottom;
        }

        /// <summary>
        /// Static property for an empty (invalid) bounding box
        /// </summary>
        public static Bounding Empty
        {
            get { return new Bounding(new LitMath.Vector2(0, 0), new LitMath.Vector2(0, 0)); }
        }
    }
}
