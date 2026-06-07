using System;
using System.Collections.Generic;

using lcdb.Colors;

namespace lcdb
{
    /// <summary>
    /// 图层
    /// </summary>
    public class Layer : DBTableRecord
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className
        {
            get { return "Layer"; }
        }

        /// <summary>
        /// 颜色
        /// </summary>
        private Color _color = Color.FromRGB(255, 255, 255);
        public Color color
        {
            get { return _color; }
            set
            {
                if (value.colorMethod == ColorMethod.ByColor)
                {
                    _color = value;
                }
                else
                {
                    throw new System.Exception("Layer set color exception.");
                }
            }
        }

        public System.Drawing.Color colorValue
        {
            get { return System.Drawing.Color.FromArgb(_color.r, _color.g, _color.b); }
        }

        /// <summary>
        /// 描述
        /// </summary>
        private string _description = string.Empty;
        public string description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// 线宽
        /// </summary>
        private LineWeight _lineWeight = LineWeight.ByLineWeightDefault;
        public LineWeight lineWeight
        {
            get { return _lineWeight; }
            set { _lineWeight = value; }
        }

        /// <summary>
        /// 线型
        /// </summary>
        private LineType _lineType = LineType.Solid; // LineType.ByLineTypeDefault;
        public LineType lineType
        {
            get { return _lineType; }
            set { _lineType = value; }
        }

        /// <summary>
        /// 锁定 — 该图层上的实体不可被选中/拖动 grip/编辑 (但仍渲染). 标准 CAD 概念.
        /// 用例: 图框完工后锁定, 防止误改; 参考底图锁定; 等.
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// 可见 — false 时该图层实体跳过渲染 (但仍存在于 db, 序列化保留).
        /// 标准 CAD "灯泡" 开关.
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 冻结 — true 时实体彻底不参与任何运算 (渲染/拾取/计算). 比 !IsVisible 更彻底,
        /// 用于大图层关掉后的性能优化. 当前 Avalonia 实现等同于 !IsVisible (无独立优化).
        /// </summary>
        public bool IsFrozen { get; set; } = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Layer(string name = "")
        {
            _name = name;
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Layer layer = base.Clone() as Layer;
            layer._color = _color;
            layer._lineWeight = _lineWeight;
            layer._lineType = _lineType;
            layer._description = _description;
            layer.IsLocked = IsLocked;
            layer.IsVisible = IsVisible;
            layer.IsFrozen = IsFrozen;
            return layer;
        }

        protected override DBObject CreateInstance()
        {
            return new Layer();
        }

        /// <summary>
        /// 写XML
        /// </summary>

        /// <summary>
        /// 读XML
        /// </summary>
    }
}
