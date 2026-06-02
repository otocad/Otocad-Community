using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

namespace lcdb
{
    /// <summary>
    /// 填充图案
    /// </summary>
    public class HatchPattern : ICloneable
    {
        #region 私有字段

        private string _name;
        private string _description;
        private List<HatchPatternLineDefinition> _lineDefinitions;
        private HatchStyle _style;
        private HatchFillType _fillType;
        private HatchType _type;
        private Vector2 _origin;
        private double _angle;
        private double _scale;

        #endregion

        #region 属性

        /// <summary>
        /// 图案名称（始终大写存储）
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = string.IsNullOrEmpty(value) ? string.Empty : value.ToUpper(); }
        }

        /// <summary>
        /// 图案描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value ?? string.Empty; }
        }

        /// <summary>
        /// 线条定义列表
        /// </summary>
        public List<HatchPatternLineDefinition> LineDefinitions
        {
            get { return _lineDefinitions; }
            set { _lineDefinitions = value ?? new List<HatchPatternLineDefinition>(); }
        }

        /// <summary>
        /// 填充样式
        /// </summary>
        public HatchStyle Style
        {
            get { return _style; }
            set { _style = value; }
        }

        /// <summary>
        /// 填充类型
        /// </summary>
        public HatchFillType FillType
        {
            get { return _fillType; }
            set { _fillType = value; }
        }

        /// <summary>
        /// 图案类型
        /// </summary>
        public HatchType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// 图案原点
        /// </summary>
        public Vector2 Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        /// <summary>
        /// 图案角度（弧度）
        /// </summary>
        public double Angle
        {
            get { return _angle; }
            set { _angle = value; }
        }

        /// <summary>
        /// 图案比例
        /// </summary>
        public double Scale
        {
            get { return _scale; }
            set { _scale = value > 0 ? value : 1.0; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public HatchPattern() : this("SOLID")
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">图案名称</param>
        public HatchPattern(string name) : this(name, null, string.Empty)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">图案名称</param>
        /// <param name="description">图案描述</param>
        public HatchPattern(string name, string description) : this(name, null, description)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">图案名称</param>
        /// <param name="lineDefinitions">线条定义</param>
        public HatchPattern(string name, IEnumerable<HatchPatternLineDefinition> lineDefinitions) 
            : this(name, lineDefinitions, string.Empty)
        {
        }

        /// <summary>
        /// 完整构造函数
        /// </summary>
        /// <param name="name">图案名称</param>
        /// <param name="lineDefinitions">线条定义</param>
        /// <param name="description">图案描述</param>
        public HatchPattern(string name, IEnumerable<HatchPatternLineDefinition> lineDefinitions, string description)
        {
            _name = string.IsNullOrEmpty(name) ? "SOLID" : name.ToUpper();
            _description = string.IsNullOrEmpty(description) ? _name : description;
            _lineDefinitions = lineDefinitions != null ? new List<HatchPatternLineDefinition>(lineDefinitions) : new List<HatchPatternLineDefinition>();
            _style = HatchStyle.Normal;
            _fillType = _name == "SOLID" ? HatchFillType.SolidFill : HatchFillType.PatternFill;
            _type = HatchType.UserDefined;
            _origin = Vector2.Zero;
            _angle = 0.0;
            _scale = 1.0;
        }

        #endregion

        #region 静态预定义图案

        /// <summary>
        /// 实体填充图案
        /// </summary>
        public static HatchPattern Solid
        {
            get
            {
                var pattern = new HatchPattern("SOLID");
                pattern._fillType = HatchFillType.SolidFill;
                pattern._type = HatchType.Predefined;
                return pattern;
            }
        }

        /// <summary>
        /// 角度填充图案
        /// </summary>
        public static HatchPattern AnglePattern
        {
            get
            {
                var lines = new List<HatchPatternLineDefinition>
                {
                    new HatchPatternLineDefinition(0, Vector2.Zero, new Vector2(0, 0.125)),
                    new HatchPatternLineDefinition(Math.PI / 2, Vector2.Zero, new Vector2(0.125, 0))
                };
                var pattern = new HatchPattern("ANGLE", lines, "Angle steel");
                pattern._type = HatchType.Predefined;
                return pattern;
            }
        }

        /// <summary>
        /// 方形填充图案
        /// </summary>
        public static HatchPattern Square
        {
            get
            {
                var lines = new List<HatchPatternLineDefinition>
                {
                    new HatchPatternLineDefinition(0, Vector2.Zero, new Vector2(0, 0.125)),
                    new HatchPatternLineDefinition(Math.PI / 2, Vector2.Zero, new Vector2(0.125, 0))
                };
                var pattern = new HatchPattern("SQUARE", lines, "Small aligned squares");
                pattern._type = HatchType.Predefined;
                return pattern;
            }
        }

        /// <summary>
        /// 线条填充图案
        /// </summary>
        public static HatchPattern Line
        {
            get
            {
                var lines = new List<HatchPatternLineDefinition>
                {
                    new HatchPatternLineDefinition(0, Vector2.Zero, new Vector2(0, 0.125))
                };
                var pattern = new HatchPattern("LINE", lines, "Parallel horizontal lines");
                pattern._type = HatchType.Predefined;
                return pattern;
            }
        }

        /// <summary>
        /// 蜂窝填充图案
        /// </summary>
        public static HatchPattern Honey
        {
            get
            {
                var lines = new List<HatchPatternLineDefinition>
                {
                    new HatchPatternLineDefinition(0, Vector2.Zero, new Vector2(0, 0.1875)),
                    new HatchPatternLineDefinition(Math.PI / 3, new Vector2(0, 0.1083), new Vector2(-0.1625, 0.281475)),
                    new HatchPatternLineDefinition(-Math.PI / 3, new Vector2(0.1625, 0.1083), new Vector2(-0.1625, -0.281475))
                };
                var pattern = new HatchPattern("HONEY", lines, "Honeycomb pattern");
                pattern._type = HatchType.Predefined;
                return pattern;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            var clonedLines = _lineDefinitions.Select(line => (HatchPatternLineDefinition)line.Clone()).ToList();
            var cloned = new HatchPattern(_name, clonedLines, _description)
            {
                _style = _style,
                _fillType = _fillType,
                _type = _type,
                _origin = _origin,
                _angle = _angle,
                _scale = _scale
            };
            return cloned;
        }

        /// <summary>
        /// 应用变换到图案
        /// </summary>
        /// <param name="translation">平移</param>
        /// <param name="rotation">旋转</param>
        /// <param name="scale">缩放</param>
        public void ApplyTransform(Vector2 translation, double rotation, double scale)
        {
            _origin += translation;
            _angle += rotation;
            _scale *= scale;

            // 变换所有线条定义
            foreach (var lineDef in _lineDefinitions)
            {
                lineDef.Angle += rotation;
                lineDef.Origin += translation;
                lineDef.Delta *= scale;
                
                // 缩放虚线图案
                for (int i = 0; i < lineDef.DashPattern.Count; i++)
                {
                    lineDef.DashPattern[i] *= scale;
                }
            }
        }

        /// <summary>
        /// 检查图案是否有效
        /// </summary>
        /// <returns>true表示有效</returns>
        public bool IsValid()
        {
            if (_fillType == HatchFillType.SolidFill)
                return true; // 实体填充总是有效的

            if (_lineDefinitions == null || _lineDefinitions.Count == 0)
                return false; // 图案填充必须有线条定义

            // 检查所有线条定义是否有效
            return _lineDefinitions.All(line => line.IsValidDashPattern());
        }

        /// <summary>
        /// 获取图案的边界框估算
        /// </summary>
        /// <param name="boundary">填充边界</param>
        /// <returns>图案覆盖的大致边界框</returns>
        public Bounding GetPatternBounds(Bounding boundary)
        {
            if (_fillType == HatchFillType.SolidFill)
                return boundary;

            // 对于图案填充，边界可能会略微扩展以确保完整覆盖
            double expansion = _scale * 0.1; // 10%的扩展
            return new Bounding(
                boundary.center,
                boundary.width + expansion,
                boundary.height + expansion
            );
        }

        /// <summary>
        /// 创建用户定义的简单图案
        /// </summary>
        /// <param name="angle">线条角度（弧度）</param>
        /// <param name="spacing">线条间距</param>
        /// <returns>用户定义的图案</returns>
        public static HatchPattern CreateUserDefined(double angle, double spacing)
        {
            var lines = new List<HatchPatternLineDefinition>
            {
                new HatchPatternLineDefinition(angle, Vector2.Zero, new Vector2(
                    -spacing * Math.Sin(angle),
                    spacing * Math.Cos(angle)
                ))
            };
            
            var pattern = new HatchPattern("USER", lines, "User defined pattern");
            pattern._type = HatchType.UserDefined;
            return pattern;
        }

        /// <summary>
        /// 创建交叉填充图案
        /// </summary>
        /// <param name="angle">第一组线条角度（弧度）</param>
        /// <param name="spacing">线条间距</param>
        /// <returns>交叉图案</returns>
        public static HatchPattern CreateCrossHatch(double angle, double spacing)
        {
            var lines = new List<HatchPatternLineDefinition>
            {
                new HatchPatternLineDefinition(angle, Vector2.Zero, new Vector2(
                    -spacing * Math.Sin(angle),
                    spacing * Math.Cos(angle)
                )),
                new HatchPatternLineDefinition(angle + Math.PI / 2, Vector2.Zero, new Vector2(
                    -spacing * Math.Sin(angle + Math.PI / 2),
                    spacing * Math.Cos(angle + Math.PI / 2)
                ))
            };
            
            var pattern = new HatchPattern("CROSS", lines, "Cross hatch pattern");
            pattern._type = HatchType.UserDefined;
            return pattern;
        }

        #endregion

        #region 重写方法

        public override string ToString()
        {
            return $"{_name} ({_fillType}, {_lineDefinitions.Count} lines)";
        }

        public override bool Equals(object obj)
        {
            if (obj is HatchPattern other)
            {
                return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _name?.GetHashCode() ?? 0;
        }

        #endregion
    }
}