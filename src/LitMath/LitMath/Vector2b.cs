using System;

namespace LitMath
{
    public struct Vector2b
    {
        public double X;
        public double Y;
        public double B;
        public bool isvalid;

        public Vector2b(double x = 0.0, double y = 0.0, double b = 0.0, bool isvalid = true)
        {
            this.X = x;
            this.Y = y;
            this.B = b;
            this.isvalid = isvalid;
        }

        public void Set(double newX, double newY, double newB)
        {
            this.X = newX;
            this.Y = newY;
            this.B = newB;
        }

        public override string ToString()
        {
            return string.Format("{0:f3}, {1:f3}, {2:f3}", this.X, this.Y, this.B);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2b))
                return false;

            return Equals((Vector2b)obj);
        }

        public bool Equals(Vector2b rhs)
        {
            return (Utils.IsEqual(X, rhs.X) && Utils.IsEqual(Y, rhs.Y) && Utils.IsEqual(B, rhs.B));
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ B.GetHashCode();
        }

        public double length
        {
            get
            {
                return Math.Sqrt((this.X * this.X) + (this.Y * this.Y));
            }
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

        public Vector2b normalized
        {
            get
            {
                double length = this.length;
                if (length != 0.0)
                {
                    return new Vector2b(this.X / length, this.Y / length);
                }
                return this;
            }
        }

        public static double Dot(Vector2b a, Vector2b b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double Cross(Vector2b a, Vector2b b)
        {
            return ((a.X * b.Y) - (a.Y * b.X));
        }

        /// <summary>
        /// Obtains the angle of a line defined by two points.
        /// </summary>
        /// <param name="u">A Vector2b.</param>
        /// <param name="v">A Vector2b.</param>
        /// <returns>Angle in radians.</returns>
        public static double Angle(Vector2b u, Vector2b v)
        {
            Vector2b dir = v - u;
            return Angle(dir);
        }

        /// <summary>
        /// Obtains the angle of a vector.
        /// </summary>
        /// <param name="u">A Vector2b.</param>
        /// <returns>Angle in radians.</returns>
        public static double Angle(Vector2b u)
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
        public static double AngleInRadian(Vector2b a, Vector2b b)
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
        public static double SignedAngle(Vector2b from, Vector2b to)
        {
            return Utils.RadianToDegree(SignedAngleInRadian(from, to));
        }

        /// <summary>
        /// Returns the signed acute clockwise angle in radians between from and to.
        /// The result value range: [-PI, PI]
        /// </summary>
        public static double SignedAngleInRadian(Vector2b from, Vector2b to)
        {
            double rad = AngleInRadian(from, to);
            if (Cross(from, to) < 0)
            {
                rad = -rad;
            }
            return rad;
        }

        public static double Distance(Vector2b a, Vector2b b)
        {
            Vector2b vector = b - a;
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
        public static bool Clockwise(Vector2b p1, Vector2b p2, Vector2b p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-8;
        }

        public static Vector2b Polar(Vector2b u, double distance, double angle)
        {
            Vector2b dir = new Vector2b(Math.Cos(angle), Math.Sin(angle));
            return u + dir * distance;
        }

        public static Vector2b Rotate(Vector2b v, double angle)
        {
            return RotateInRadian(v, Utils.DegreeToRadian(angle));
        }

        public static Vector2b Rotate(Vector2b point, Vector2 basePoint, double angle)
        {
            return RotateInRadian(point, basePoint, Utils.DegreeToRadian(angle));
        }

        public static Vector2b RotateInRadian(Vector2b v, double rad)
        {
            double x = v.X * Math.Cos(rad) - v.Y * Math.Sin(rad);
            double y = v.X * Math.Sin(rad) + v.Y * Math.Cos(rad);
            return new Vector2b(x, y);
        }

        public static Vector2b StringToVector(string text)
        {
            bool isvalid = true;

            if (!string.IsNullOrEmpty(text) && text.IndexOf(",", StringComparison.Ordinal) >= 0)
            {
                string[] arr = text.Split(',');

                double x = 0;
                double y = 0;
                double b = 0;
                isvalid = double.TryParse(arr[0].Replace(".", ","), out x);

                if (isvalid)
                    isvalid = double.TryParse(arr[1].Replace(".", ","), out y);

                if (isvalid)
                    isvalid = double.TryParse(arr[2].Replace(".", ","), out b);


                return new Vector2b(x, y, b, isvalid);
            }
            else
            {
                return new Vector2b(0, 0, 0, false);
            }
        }



        /// <summary>
        /// Rotates one point around another TM
        /// </summary>
        /// <param name="pointToRotate">The point to rotate.</param>
        /// <param name="centerPoint">The center point of rotation.</param>
        /// <param name="angleInDegrees">The rotation angle in degrees.</param>
        /// <returns>Rotated point</returns>
        public static Vector2b RotateInRadian(Vector2b pointToRotate, Vector2 centerPoint, double angleInRadians)
        {
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Vector2b
            {
                X = (cosTheta * (pointToRotate.X - centerPoint.X) - sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y = (sinTheta * (pointToRotate.X - centerPoint.X) + cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }


        public static Vector2b PointOrthoMode(Vector2b last, Vector2b point, bool ortho)
        {
            if (ortho)
            {
                if (Math.Abs(point.X - last.X) > Math.Abs(point.Y - last.Y))
                    return new LitMath.Vector2b(point.X, last.Y);
                else
                    return new LitMath.Vector2b(last.X, point.Y);
            }
            else
            {
                return point;
            }


        }

        public static Vector2b Zero
        {
            get { return new Vector2b(0.0, 0.0); }
        }

        public static Vector2b operator +(Vector2b a, Vector2b b)
        {
            return new Vector2b(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2b operator -(Vector2b a, Vector2b b)
        {
            return new Vector2b(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2b operator -(Vector2b a)
        {
            return new Vector2b(-a.X, -a.Y);
        }

        public static Vector2b operator *(Vector2b a, double d)
        {
            return new Vector2b(a.X * d, a.Y * d);
        }

        public static Vector2b operator *(double d, Vector2b a)
        {
            return new Vector2b(a.X * d, a.Y * d);
        }

        public static Vector2b operator /(Vector2b a, double d)
        {
            return new Vector2b(a.X / d, a.Y / d);
        }

        public static bool operator ==(Vector2b lhs, Vector2b rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Vector2b lhs, Vector2b rhs)
        {
            return !(lhs == rhs);
        }
    }
}
