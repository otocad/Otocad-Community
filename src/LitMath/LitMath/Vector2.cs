using System;


namespace LitMath
{
    public struct Vector2
    {
        public double X;
        public double Y;
        public bool isvalid;

        #region Static Properties

        /// <summary>
        /// Unit X vector (1, 0).
        /// </summary>
        public static Vector2 UnitX
        {
            get { return new Vector2(1.0, 0.0); }
        }

        /// <summary>
        /// Unit Y vector (0, 1).
        /// </summary>
        public static Vector2 UnitY
        {
            get { return new Vector2(0.0, 1.0); }
        }

        #endregion

        public Vector2(double x = 0.0, double y = 0.0, bool isvalid = true)
        {
            this.X = x;
            this.Y = y;
            this.isvalid = isvalid;
        }
        public Vector2(netDxf.Vector2 v)
        {
            this.X = v.X;
            this.Y = v.Y;
            this.isvalid = true;
        }
        public Vector2(netDxf.Vector3 v)
        {
            this.X = v.X;
            this.Y = v.Y;            
            this.isvalid = true;
        }
        public void Set(double newX, double newY)
        {
            this.X = newX;
            this.Y = newY;
        }
        public netDxf.Vector2 ToDxfVector2()
        {
            return new netDxf.Vector2(X,Y);
        }
     
        public override string ToString()
        {
            // return string.Format("Vector2({0}, {1})", this.X, this.Y);
            return string.Format("{0:f3}, {1:f3}", this.X, this.Y);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2))
                return false;

            return Equals((Vector2)obj);
        }

        public bool Equals(Vector2 rhs)
        {
            return (Utils.IsEqual(X, rhs.X) && Utils.IsEqual(Y, rhs.Y));
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
        public double length
        {
            get
            {
                return Math.Sqrt((this.X * this.X) + (this.Y * this.Y));
            }
        }

        /// <summary>
        /// Gets the modulus (magnitude/length) of the vector.
        /// </summary>
        public double Modulus()
        {
            return Math.Sqrt((this.X * this.X) + (this.Y * this.Y));
        }
        public double lengthSqrd
        {
            get
            {
                return ((this.X * this.X) + (this.Y * this.Y));
            }
        }

        public void Normalize()
        {
            double length = this.length;
            if (length != 0.0)
            {
                this.X /= length;
                this.Y /= length;
            }
        }
        public Vector2 normalized
        {
            get
            {
                double length = this.length;
                if (length != 0.0)
                {
                    return new Vector2(this.X / length, this.Y / length);
                }
                return this;
            }
        }

        public static double Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double Cross(Vector2 a, Vector2 b)
        {
            return ((a.X * b.Y) - (a.Y * b.X));
        }

        ///// <summary>
        ///// Returns the unsigned angle in degrees between a and B.
        ///// The smaller of the two possible angles between the two vectors is used.
        ///// The result value range: [0, 180]
        ///// </summary>
        //public static double Angle(Vector2 a, Vector2 B)
        //{
        //    return Utils.RadianToDegree(AngleInRadian(a, B));
        //}


        /// <summary>
        /// Obtains the angle of a line defined by two points.
        /// </summary>
        /// <param name="u">A Vector2.</param>
        /// <param name="v">A Vector2.</param>
        /// <returns>Angle in radians.</returns>
        public static double Angle(Vector2 u, Vector2 v)
        {
            Vector2 dir = v - u;
            return Angle(dir);
        }

        /// <summary>
        /// Obtains the angle of a vector.
        /// </summary>
        /// <param name="u">A Vector2.</param>
        /// <returns>Angle in radians.</returns>
        public static double Angle(Vector2 u)
        {
            double angle = Math.Atan2(u.Y, u.X);
            if (angle < 0)
            {
                return Utils.TwoPI + angle;
            }

            return angle;
        }


        /// <summary>
        /// Returns the unsigned angle in radians between a and B.
        /// The smaller of the two possible angles between the two vectors is used.
        /// The result value range: [0, PI]
        /// </summary>
        public static double AngleInRadian(Vector2 a, Vector2 b)
        {
            double num = a.length * b.length;
            if (num == 0.0)
            {
                return 0.0;
            }
            double num2 = Dot(a, b) / num;
            return Math.Acos(Utils.Clamp(num2, -1.0, 1.0));
        }

        /// <summary>
        /// Returns the signed acute clockwise angle in degrees between from and to.
        /// The result value range: [-180, 180]
        /// </summary>
        public static double SignedAngle(Vector2 from, Vector2 to)
        {
            return Utils.RadianToDegree(SignedAngleInRadian(from, to));
        }

        /// <summary>
        /// Returns the signed acute clockwise angle in radians between from and to.
        /// The result value range: [-PI, PI]
        /// </summary>
        public static double SignedAngleInRadian(Vector2 from, Vector2 to)
        {
            double rad = AngleInRadian(from, to);
            if (Cross(from, to) < 0)
            {
                rad = -rad;
            }
            return rad;
        }

        public static double Distance(Vector2 a, Vector2 b)
        {
            Vector2 vector = b - a;
            return vector.length;
        }
        public double DistanceTo(Vector2 a)
        {
            Vector2 vector = this - a;
            return vector.length;
        }
        public static double AnglesDifference(double firstAngle, double secondAngle)
        {
            double difference = secondAngle - firstAngle;
            while (difference < -Utils.PI) difference += Utils.TwoPI;
            while (difference > Utils.PI) difference -= Utils.TwoPI;

            return difference;
        }


        // Evaluates if the points are clockwise.
        public static bool Clockwise(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-8;
        }

        public static Vector2 Polar(Vector2 u, double distance, double angle)
        {
            Vector2 dir = new Vector2(Math.Cos(angle), Math.Sin(angle));
            return u + dir * distance;
        }

        /// <summary>
        /// Returns the vector perpendicular to u (rotated 90° counter-clockwise).
        /// </summary>
        public static Vector2 Perpendicular(Vector2 u)
        {
            return new Vector2(-u.Y, u.X);
        }

        public static Vector2 Rotate(Vector2 v, double angle)
        {
            return RotateInRadian(v, Utils.DegreeToRadian(angle));
        }

        public static Vector2 Rotate(Vector2 point, Vector2 basePoint, double angle)
        {
            return RotateInRadian(point, basePoint, Utils.DegreeToRadian(angle));
        }

        public static Vector2 RotateInRadian(Vector2 v, double rad)
        {
            double x = v.X * Math.Cos(rad) - v.Y * Math.Sin(rad);
            double y = v.X * Math.Sin(rad) + v.Y * Math.Cos(rad);
            return new Vector2(x, y);
        }

        public static Vector2 StringToVector(string text)
        {
            bool isvalid = true;

            if (!string.IsNullOrEmpty(text) && text.IndexOf(",", StringComparison.Ordinal) >= 0)
            {
                string[] arr = text.Split(',');

                double x = 0;
                double y = 0;
                isvalid = double.TryParse(arr[0].Replace(".",","), out x);

                if(isvalid)
                    isvalid = double.TryParse(arr[1].Replace(".", ","), out y);

                return new Vector2(x, y, isvalid);
            }
            else
            {
                return new Vector2(0, 0, false);
            }
        }

        //public static Vector2 RotateInRadian(Vector2 point, Vector2 basePoint, double rad)
        //{
        //    double cos = Math.Cos(rad);
        //    double sin = Math.Sin(rad);
        //    double X = point.X * cos - point.Y * sin + basePoint.X * (1 - cos) + basePoint.Y * sin;
        //    double Y = point.X * sin + point.Y * cos + basePoint.Y * (1 - cos) + basePoint.X * sin;

        //    return new Vector2(X, Y);
        //}


        /// <summary>
        /// Rotates one point around another TM
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
        /// <returns>Rotated point</returns>
        public static Vector2 RotateInRadian(Vector2 pointToRotate, Vector2 centerPoint, double angleInRadians)
        {
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Vector2
            {
                X = (cosTheta * (pointToRotate.X - centerPoint.X) - sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (sinTheta * (pointToRotate.X - centerPoint.X) + cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        public static Vector2 PointOrthoMode(Vector2 last, Vector2 point, bool ortho)
        {
            if (ortho)
            {
                if (Math.Abs(point.X - last.X) > Math.Abs(point.Y - last.Y))
                    return new LitMath.Vector2(point.X, last.Y);
                else
                    return new LitMath.Vector2(last.X, point.Y);
            }
            else
            {
                return point;
            }


        }
        public static Vector2 Zero
        {
            get { return new Vector2(0.0, 0.0); }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2 operator -(Vector2 a)
        {
            return new Vector2(-a.X, -a.Y);
        }

        public static Vector2 operator *(Vector2 a, double d)
        {
            return new Vector2(a.X * d, a.Y * d);
        }

        public static Vector2 operator *(double d, Vector2 a)
        {
            return new Vector2(a.X * d, a.Y * d);
        }

        public static Vector2 operator /(Vector2 a, double d)
        {
            return new Vector2(a.X / d, a.Y / d);
        }

        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Vector2 lhs, Vector2 rhs)
        {
            return !(lhs == rhs);
        }
    }
}
