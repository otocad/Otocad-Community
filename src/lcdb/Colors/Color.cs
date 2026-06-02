using System;

namespace lcdb.Colors
{
    public struct Color
    {
        public ColorMethod colorMethod { get; set; }

        public byte r { get; set; }

        public byte g { get; set; }

        public byte b { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        private Color(byte r, byte g, byte b)
        {
            colorMethod = ColorMethod.ByColor;
            this.r = r;
            this.g = g;
            this.b = b;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public Color(ColorMethod colorMethod, byte r, byte g, byte b)
        {
            this.colorMethod = colorMethod;
            this.r = r;
            this.g = g;
            this.b = b;
        }
        /// <summary>
        /// 构造函数 - 支持ColorMethod参数
        /// </summary>
        public Color(ColorMethod colorMethod) : this()
        {
            this.colorMethod = colorMethod;
            this.r = 255;
            this.g = 255;
            this.b = 255;
        }

        public string Name
        {
            get
            {
                switch (colorMethod)
                {
                    case ColorMethod.ByLayer:
                        return "ByLayer";

                    case ColorMethod.ByBlock:
                        return "ByBlock";

                    case ColorMethod.None:
                        return "None";

                    case ColorMethod.ByColor:
                    case ColorMethod.ByEntity:
                        return string.Format("{0},{1},{2}", r, g, b);

                    default:
                        return "";
                }
            }
        }

        public static Color FromRGB(byte r, byte g, byte b)
        {
            return new Color(r, g, b);
        }

        public static Color FromColor(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B);
        }

        public static Color ByLayer
        {
            get
            {
                Color color = new Color();
                color.colorMethod = ColorMethod.ByLayer;
                color.r = 255;
                color.g = 255;
                color.b = 255;
                return color;
            }
        }

        public static Color ByBlock
        {
            get
            {
                Color color = new Color();
                color.colorMethod = ColorMethod.ByBlock;
                color.r = 255;
                color.g = 255;
                color.b = 255;
                return color;
            }
        }

        public static Color ByEntity(byte r, byte g, byte b)
        {
            return new Color(ColorMethod.ByEntity, r, g, b);
        }

        public static Color Black
        {
            get
            {
                return new Color(0, 0, 0);
            }
        }

        public static Color White
        {
            get
            {
                return new Color(255, 255, 255);
            }
        }

        public static Color FromColorMethod(ColorMethod method, System.Drawing.Color color)
        {
            return new Color(method, color.R, color.G, color.B);
        }

        /// <summary>
        /// 转换为 System.Drawing.Color
        /// </summary>
        public System.Drawing.Color ToDrawingColor()
        {
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// 获取 ARGB 值
        /// </summary>
        public int ToArgb()
        {
            return System.Drawing.Color.FromArgb(r, g, b).ToArgb();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1},{2},{3}", colorMethod.ToString(), r, g, b);
        }

        internal static bool TryParse(string str, out Color result)
        {
            string[] arr = str.Split(':');
            if (arr.Length != 2)
            {
                result = Color.ByLayer;
                return false;
            }

            //
            ColorMethod colorMethod = (ColorMethod)Enum.Parse(typeof(lcdb.Colors.ColorMethod), arr[0], true);

            //
            string[] rgb = arr[1].Split(',');
            if (rgb.Length != 3)
            {
                result = Color.ByLayer;
                return false;
            }

            byte red = 0;
            byte green = 0;
            byte blue = 0;
            if (byte.TryParse(rgb[0], out red)
                && byte.TryParse(rgb[1], out green)
                && byte.TryParse(rgb[2], out blue))
            {
                result = new Color();
                result.colorMethod = colorMethod;
                result.r = red;
                result.g = green;
                result.b = blue;

                return true;
            }
            else
            {
                result = Color.ByLayer;
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Color))
                return false;

            return Equals((Color)obj);
        }

        public bool Equals(Color rhs)
        {
            if (colorMethod != rhs.colorMethod)
            {
                return false;
            }

            switch (colorMethod)
            {
                case ColorMethod.ByColor:
                case ColorMethod.ByEntity:
                    return r == rhs.r 
                        && g == rhs.g 
                        && b == rhs.b;

                case ColorMethod.ByBlock:
                case ColorMethod.ByLayer:
                case ColorMethod.None:
                default:
                    return true;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = colorMethod.GetHashCode();
                
                switch (colorMethod)
                {
                    case ColorMethod.ByColor:
                    case ColorMethod.ByEntity:
                        hash = hash * 31 + r.GetHashCode();
                        hash = hash * 31 + g.GetHashCode();
                        hash = hash * 31 + b.GetHashCode();
                        break;
                }
                
                return hash;
            }
        }

        public static bool operator ==(Color lhs, Color rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Color lhs, Color rhs)
        {
            return !(lhs == rhs);
        }
    }
}
