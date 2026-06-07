using System;
using System.Collections.Generic;
using LitMath;

namespace lcdb
{
    /// <summary>
    /// 填充图案线定义
    /// </summary>
    public class HatchPatternLineDefinition : ICloneable
    {
        #region 私有字段

        private double _angle;
        private Vector2 _origin;
        private Vector2 _delta;
        private List<double> _dashPattern;

        #endregion

        #region 属性

        /// <summary>
        /// 线条角度（弧度）
        /// </summary>
        public double Angle
        {
            get { return _angle; }
            set { _angle = value; }
        }

        /// <summary>
        /// 起始点
        /// </summary>
        public Vector2 Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        /// <summary>
        /// 增量向量（线条间距和偏移）
        /// </summary>
        public Vector2 Delta
        {
            get { return _delta; }
            set { _delta = value; }
        }

        /// <summary>
        /// 虚线图案（线段和间隙的长度序列）
        /// </summary>
        public List<double> DashPattern
        {
            get { return _dashPattern; }
            set { _dashPattern = value ?? new List<double>(); }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public HatchPatternLineDefinition()
        {
            _angle = 0.0;
            _origin = Vector2.Zero;
            _delta = Vector2.Zero;
            _dashPattern = new List<double>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="angle">角度（弧度）</param>
        /// <param name="origin">起始点</param>
        /// <param name="delta">增量向量</param>
        public HatchPatternLineDefinition(double angle, Vector2 origin, Vector2 delta)
        {
            _angle = angle;
            _origin = origin;
            _delta = delta;
            _dashPattern = new List<double>();
        }

        /// <summary>
        /// 完整构造函数
        /// </summary>
        /// <param name="angle">角度（弧度）</param>
        /// <param name="origin">起始点</param>
        /// <param name="delta">增量向量</param>
        /// <param name="dashPattern">虚线图案</param>
        public HatchPatternLineDefinition(double angle, Vector2 origin, Vector2 delta, IEnumerable<double> dashPattern)
        {
            _angle = angle;
            _origin = origin;
            _delta = delta;
            _dashPattern = dashPattern != null ? new List<double>(dashPattern) : new List<double>();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            return new HatchPatternLineDefinition(_angle, _origin, _delta, _dashPattern);
        }

        /// <summary>
        /// 计算在指定偏移处的线条起始点
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <returns>线条起始点</returns>
        public Vector2 GetLineStartPoint(int offset)
        {
            return _origin + _delta * offset;
        }

        /// <summary>
        /// 获取线条方向向量
        /// </summary>
        /// <returns>单位方向向量</returns>
        public Vector2 GetDirection()
        {
            return new Vector2(Math.Cos(_angle), Math.Sin(_angle));
        }

        /// <summary>
        /// 检查虚线图案是否有效
        /// </summary>
        /// <returns>true表示有效</returns>
        public bool IsValidDashPattern()
        {
            if (_dashPattern == null || _dashPattern.Count == 0)
                return true; // 实线

            // 检查图案总长度是否大于0
            double totalLength = 0;
            foreach (double length in _dashPattern)
            {
                totalLength += Math.Abs(length);
            }

            return totalLength > 0;
        }

        /// <summary>
        /// 获取虚线图案的总长度
        /// </summary>
        /// <returns>图案总长度</returns>
        public double GetPatternLength()
        {
            if (_dashPattern == null || _dashPattern.Count == 0)
                return 0;

            double totalLength = 0;
            foreach (double length in _dashPattern)
            {
                totalLength += Math.Abs(length);
            }

            return totalLength;
        }

        #endregion

        #region 重写方法

        public override string ToString()
        {
            return $"Angle={_angle:F3}, Origin=({_origin.X:F2},{_origin.Y:F2}), Delta=({_delta.X:F2},{_delta.Y:F2}), Dashes={_dashPattern.Count}";
        }

        #endregion
    }
}