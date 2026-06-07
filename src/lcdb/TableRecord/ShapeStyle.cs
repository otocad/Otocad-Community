using System;
using System.Collections.Generic;

namespace lcdb
{
    /// <summary>
    /// 形状样式表记录
    /// 定义了形状文件（SHX）的引用
    /// </summary>
    public class ShapeStyle : DBTableRecord
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "ShapeStyle"; }
        }

        /// <summary>
        /// SHX文件路径
        /// </summary>
        private string _fileName = string.Empty;

        /// <summary>
        /// 默认大小
        /// </summary>
        private double _size = 1.0;

        /// <summary>
        /// 默认宽度因子
        /// </summary>
        private double _widthFactor = 1.0;

        /// <summary>
        /// 默认倾斜角度
        /// </summary>
        private double _obliqueAngle = 0.0;

        /// <summary>
        /// 形状名称缓存（名称到编号的映射）
        /// </summary>
        private Dictionary<string, short> _shapeNames = new Dictionary<string, short>();

        /// <summary>
        /// 是否已加载形状定义
        /// </summary>
        private bool _shapesLoaded = false;

        /// <summary>
        /// 获取或设置SHX文件路径
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set 
            { 
                _fileName = value ?? string.Empty;
                _shapesLoaded = false;
                _shapeNames.Clear();
            }
        }

        /// <summary>
        /// 获取或设置默认大小
        /// </summary>
        public double Size
        {
            get { return _size; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Size must be greater than 0");
                _size = value;
            }
        }

        /// <summary>
        /// 获取或设置默认宽度因子
        /// </summary>
        public double WidthFactor
        {
            get { return _widthFactor; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("WidthFactor cannot be 0");
                _widthFactor = value;
            }
        }

        /// <summary>
        /// 获取或设置默认倾斜角度（弧度）
        /// </summary>
        public double ObliqueAngle
        {
            get { return _obliqueAngle; }
            set
            {
                double degrees = value * 180.0 / Math.PI;
                if (degrees < -85 || degrees > 85)
                    throw new ArgumentException("ObliqueAngle must be between -85 and 85 degrees");
                _obliqueAngle = value;
            }
        }

        /// <summary>
        /// 检查是否包含指定名称的形状
        /// </summary>
        public bool ContainsShapeName(string shapeName)
        {
            if (string.IsNullOrEmpty(shapeName))
                return false;

            LoadShapeNames();
            return _shapeNames.ContainsKey(shapeName.ToUpper());
        }

        /// <summary>
        /// 获取形状编号
        /// </summary>
        public short GetShapeNumber(string shapeName)
        {
            if (string.IsNullOrEmpty(shapeName))
                return 0;

            LoadShapeNames();
            
            string upperName = shapeName.ToUpper();
            if (_shapeNames.ContainsKey(upperName))
            {
                return _shapeNames[upperName];
            }
            
            return 0;
        }

        /// <summary>
        /// 获取形状名称
        /// </summary>
        public string GetShapeName(short shapeNumber)
        {
            LoadShapeNames();
            
            foreach (var kvp in _shapeNames)
            {
                if (kvp.Value == shapeNumber)
                {
                    return kvp.Key;
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// 获取所有形状名称
        /// </summary>
        public IEnumerable<string> GetShapeNames()
        {
            LoadShapeNames();
            return _shapeNames.Keys;
        }

        /// <summary>
        /// 加载形状名称（占位实现）
        /// </summary>
        private void LoadShapeNames()
        {
            if (_shapesLoaded)
                return;

            _shapesLoaded = true;

            // 注意：OtoCAD不支持读取SHX文件
            // 这里提供一些默认的形状名称作为示例
            if (_fileName.ToUpper().IndexOf("LTYPESHP.SHX", StringComparison.Ordinal) >= 0)
            {
                // 线型形状
                _shapeNames["BOX"] = 1;
                _shapeNames["CIRC1"] = 2;
                _shapeNames["TRACK1"] = 3;
                _shapeNames["ZIG"] = 4;
                _shapeNames["BAT"] = 5;
            }
            else if (_fileName.ToUpper().IndexOf("SIMPLEX.SHX", StringComparison.Ordinal) >= 0)
            {
                // 简单文字形状
                _shapeNames["DEGREE"] = 176;
                _shapeNames["PLUS-MINUS"] = 177;
                _shapeNames["DIAMETER"] = 216;
            }
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            ShapeStyle style = base.Clone() as ShapeStyle;
            style._fileName = _fileName;
            style._size = _size;
            style._widthFactor = _widthFactor;
            style._obliqueAngle = _obliqueAngle;
            style._shapeNames = new Dictionary<string, short>(_shapeNames);
            style._shapesLoaded = _shapesLoaded;
            return style;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new ShapeStyle();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>

        /// <summary>
        /// 创建默认的线型形状样式
        /// </summary>
        public static ShapeStyle CreateLinetypeShapeStyle()
        {
            ShapeStyle style = new ShapeStyle();
            style.name = "LTYPESHP";
            style.FileName = "LTYPESHP.SHX";
            return style;
        }
    }
}