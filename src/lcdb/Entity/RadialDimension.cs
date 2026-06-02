using System;
using System.Collections.Generic;
using LitMath;


namespace lcdb
{
    /// <summary>
    /// 半径标注实体
    /// 原生OtoCAD实现，用于标注圆弧或圆的半径
    /// </summary>
    [Serializable]
    public class RadialDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "RadialDimension";

        #region 字段

        // 圆心点
        private Vector2 _centerPoint = new Vector2();
        
        // 圆弧上的定义点
        private Vector2 _chordPoint = new Vector2();
        
        // 引线长度
        private double _leaderLength = 0.0;

        #endregion

        #region 属性

        /// <summary>
        /// 圆心点
        /// </summary>
        public Vector2 centerPoint
        {
            get { return _centerPoint; }
            set 
            { 
                _centerPoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 圆弧上的定义点
        /// </summary>
        public Vector2 chordPoint
        {
            get { return _chordPoint; }
            set 
            { 
                _chordPoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 引线长度
        /// </summary>
        public double leaderLength
        {
            get { return _leaderLength; }
            set { _leaderLength = Math.Max(0, value); }
        }


        /// <summary>
        /// 中心点（便利属性）
        /// </summary>
        public Vector2 center
        {
            get { return _centerPoint; }
            set 
            { 
                _centerPoint = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 远弦点（便利属性，映射到文本位置）
        /// </summary>
        public Vector2 farChordPoint
        {
            get 
            { 
                // 如果文本位置未手动设置，计算默认位置
                if (!_textPositionManuallySet)
                {
                    Vector2 dir = (_chordPoint - _centerPoint).normalized;
                    return _chordPoint + dir * _leaderLength;
                }
                return _textReferencePoint;
            }
            set 
            { 
                _textReferencePoint = value;
                _textPositionManuallySet = true;
            }
        }

        /// <summary>
        /// 获取实际测量值（半径）
        /// </summary>
        public override double measurement
        {
            get
            {
                return (_chordPoint - _centerPoint).length;
            }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                var st = _style ?? DimensionStyle.Default;
                Vector2 direction = (_chordPoint - _centerPoint).normalized;
                List<Vector2> points = new List<Vector2> { _chordPoint };

                if (_leaderLength > 0)
                {
                    // 引线模式: 选择框只覆盖可见几何 (弧点→拐点→水平落脚→文字),
                    // 不含远处的曲率中心 — 否则透镜弧 (R 很大) 的选择框会被拉到球心, 巨大无比。
                    Vector2 knee = _chordPoint + direction * _leaderLength;
                    points.Add(knee);

                    string text = string.IsNullOrEmpty(userText) ? "R" + GetFormattedText() : GetFormattedText();
                    double textW = Math.Max(text.Length, 2) * st.TextHeight * 0.6;
                    double side = Math.Abs(direction.X) > 1e-6
                        ? Math.Sign(direction.X)
                        : (_chordPoint.X >= _centerPoint.X ? 1.0 : -1.0);

                    if (_textPositionManuallySet)
                    {
                        points.Add(new Vector2(_textReferencePoint.X, knee.Y));
                        points.Add(_textReferencePoint);
                        points.Add(_textReferencePoint + new Vector2(side * textW, 0));
                    }
                    else
                    {
                        Vector2 landingEnd = new Vector2(knee.X + side * (textW + st.DimensionLineGap * 2), knee.Y);
                        points.Add(landingEnd);
                        points.Add(landingEnd + new Vector2(side * (st.DimensionLineGap + textW), 0));
                    }
                    // 文字高度方向留余量
                    points.Add(new Vector2(knee.X, knee.Y + st.TextHeight));
                    points.Add(new Vector2(knee.X, knee.Y - st.TextHeight));
                }
                else
                {
                    // 半径线模式: 画 center→chord, 故选择框需含圆心
                    points.Add(_centerPoint);
                    if (_textPositionManuallySet) points.Add(_textReferencePoint);
                }

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (Vector2 point in points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                return new Bounding(new Vector2(minX, minY), new Vector2(maxX, maxY));
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建半径标注
        /// </summary>
        public RadialDimension() : base(DimensionType.Radial)
        {
        }

        /// <summary>
        /// 创建半径标注
        /// </summary>
        public RadialDimension(Vector2 center, Vector2 chordPt, double leaderLen = 0) 
            : base(DimensionType.Radial)
        {
            _centerPoint = center;
            _chordPoint = chordPt;
            _leaderLength = leaderLen;
            UpdateDefinitionPoint();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 生成标注图形
        /// </summary>
        public override void Generate()
        {
            _generatedEntities.Clear();

            if (_style == null)
                _style = DimensionStyle.Default;

            Vector2 direction = (_chordPoint - _centerPoint).normalized;
            double radius = measurement;

            // 引线模式 vs 半径线模式 (类似 ACAD R 标注): 有引线时不再画 center→chord 的半径线,
            // 引线直接从 chord 沿径向外延. 这样当 centerPoint 是真正的远端球心 (例如透镜),
            // 也不会画一条横穿物体的辅助线.
            string text = string.IsNullOrEmpty(userText) ? "R" + GetFormattedText() : GetFormattedText();

            // 落脚方向: 引线朝右 → 文字向右展开; 近竖直时按弧点相对圆心的左右决定。
            double side = Math.Abs(direction.X) > 1e-6
                ? Math.Sign(direction.X)
                : (_chordPoint.X >= _centerPoint.X ? 1.0 : -1.0);

            Vector2 leaderEnd = _chordPoint;   // = knee (径向引线末端 / 落脚线起点)
            if (_leaderLength > 0)
            {
                // 径向引线 chord → knee。手动文字时 knee 取径向射线在文字 Y 高度的交点,
                // 使落脚线既水平又与引线相接 (R 标注的标准 "拐点" 画法)。
                Vector2 knee = _chordPoint + direction * _leaderLength;
                if (_textPositionManuallySet && Math.Abs(direction.Y) > 1e-6)
                {
                    double t = (_textReferencePoint.Y - _chordPoint.Y) / direction.Y;
                    if (t > 0) knee = _chordPoint + direction * t;
                    side = _textReferencePoint.X >= knee.X ? 1.0 : -1.0;
                }
                leaderEnd = knee;
                _generatedEntities.Add(new Line(_chordPoint, knee) { color = this.color, layer = this.layer });

                // 落脚线: 永远水平 (与标注方向无关)。
                double textW = Math.Max(text.Length, 2) * _style.TextHeight * 0.6;   // 近似文字宽
                double landingLen = textW + _style.DimensionLineGap * 2;
                Vector2 landingEnd = _textPositionManuallySet
                    ? new Vector2(_textReferencePoint.X, knee.Y)
                    : new Vector2(knee.X + side * landingLen, knee.Y);
                _generatedEntities.Add(new Line(knee, landingEnd) { color = this.color, layer = this.layer });

                if (!_textPositionManuallySet)
                    _textReferencePoint = landingEnd + new Vector2(side * _style.DimensionLineGap, 0);
            }
            else
            {
                _generatedEntities.Add(new Line(_centerPoint, _chordPoint) { color = this.color, layer = this.layer });
                if (!_textPositionManuallySet)
                    _textReferencePoint = _chordPoint + direction * _style.DimensionLineGap;
            }

            // 箭头 (在弧点, 指向圆内)
            Solid arrow = CreateArrowhead(_chordPoint, -direction, _style.ArrowSize);
            _generatedEntities.Add(arrow);

            // 文本: 半径标注始终水平书写 (落脚线水平, 文字坐在其上)。
            Text dimText = CreateDimensionText(_textReferencePoint, text, _style.TextHeight);
            bool textOnRight = _textReferencePoint.X >= leaderEnd.X;
            dimText.angle = 0;
            dimText.alignment = textOnRight ? TextAlignment.LeftMiddle : TextAlignment.RightMiddle;
            _generatedEntities.Add(dimText);

            // 如果没有引线且文本离圆心较近，添加中心标记
            if (_leaderLength == 0 && radius > _style.ArrowSize * 2)
            {
                CreateCenterMark();
            }
        }

        /// <summary>
        /// 创建中心标记
        /// </summary>
        private void CreateCenterMark()
        {
            double markSize = _style.ArrowSize * 0.5;
            
            // 水平线
            Line hLine = new Line(
                _centerPoint - new Vector2(markSize, 0),
                _centerPoint + new Vector2(markSize, 0))
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(hLine);
            
            // 垂直线
            Line vLine = new Line(
                _centerPoint - new Vector2(0, markSize),
                _centerPoint + new Vector2(0, markSize))
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(vLine);
        }

        /// <summary>
        /// 更新标注
        /// </summary>
        protected override void CalculateReferencePoints()
        {
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new RadialDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            RadialDimension dimension = base.Clone() as RadialDimension;
            dimension._centerPoint = _centerPoint;
            dimension._chordPoint = _chordPoint;
            dimension._leaderLength = _leaderLength;
            return dimension;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, _centerPoint));
            gripPoints.Add(new GripPoint(GripPointType.End, _chordPoint));
            
            // 引线终点夹点
            if (_leaderLength > 0)
            {
                Vector2 direction = (_chordPoint - _centerPoint).normalized;
                Vector2 leaderEnd = _chordPoint + direction * _leaderLength;
                gripPoints.Add(new GripPoint(GripPointType.End, leaderEnd));
            }
            
            if (_textPositionManuallySet)
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, _textReferencePoint));
            }
            
            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 圆心 — 整体平移整个标注 (center+chord+text 同步), 半径 / 引线方向都不变
                    Vector2 delta = newPosition - _centerPoint;
                    _centerPoint = newPosition;
                    _chordPoint += delta;
                    if (_textPositionManuallySet) _textReferencePoint += delta;
                    break;
                case 1: // 圆弧点 — 绕 center 旋转 chord 到点击方向, 半径锁死
                    SetDimensionLinePosition(newPosition);
                    Update();
                    return;
                case 2: // 引线端 — 拖动绕圆心重新瞄准 (改标注方向, 半径锁定) + 调整引线长度
                    if (_leaderLength > 0)
                    {
                        double r = (_chordPoint - _centerPoint).length;
                        if (r > Utils.EPSILON && (newPosition - _centerPoint).length > Utils.EPSILON)
                        {
                            double ang = Vector2.Angle(_centerPoint, newPosition);
                            _chordPoint = Vector2.Polar(_centerPoint, r, ang);
                            Vector2 direction = (_chordPoint - _centerPoint).normalized;
                            _leaderLength = Math.Max(0, Vector2.Dot(newPosition - _chordPoint, direction));
                            _textPositionManuallySet = false;
                            UpdateDefinitionPoint();
                        }
                    }
                    break;
                case 3: // 文本位置 — 自由拖 (排版)
                    if (_textPositionManuallySet)
                    {
                        _textReferencePoint = newPosition;
                    }
                    break;
            }
            Update();
        }

        /// <summary>
        /// 获取捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Center, _centerPoint));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _chordPoint));
            
            // 引线终点
            if (_leaderLength > 0)
            {
                Vector2 direction = (_chordPoint - _centerPoint).normalized;
                Vector2 leaderEnd = _chordPoint + direction * _leaderLength;
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, leaderEnd));
            }
            
            return snapPoints;
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 子类特定的平移操作
        /// </summary>
        protected override void TranslateSpecific(Vector2 translation)
        {
            _centerPoint += translation;
            _chordPoint += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _centerPoint = Vector2.RotateInRadian(_centerPoint, center, angle);
            _chordPoint = Vector2.RotateInRadian(_chordPoint, center, angle);
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _centerPoint = transform * _centerPoint;
            _chordPoint = transform * _chordPoint;
            // 注意：变换可能改变尺度，需要重新计算引线长度
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新定义点
        /// </summary>
        private void UpdateDefinitionPoint()
        {
            _definitionPoint = _chordPoint;
        }

        /// <summary>
        /// 设置引线终点位置
        /// </summary>
        public void SetLeaderEndPoint(Vector2 endPoint)
        {
            Vector2 direction = (_chordPoint - _centerPoint).normalized;
            _leaderLength = Vector2.Dot(endPoint - _chordPoint, direction);
            _leaderLength = Math.Max(0, _leaderLength);
        }

        /// <summary>
        /// 按尺寸线上一点反推 chordPoint (绕圆心旋转到该点方向, 半径不变).
        /// 对接 netDxf.RadialDimension.SetDimensionLinePosition: Grip 拖拽时单点调用.
        /// </summary>
        public void SetDimensionLinePosition(Vector2 point)
        {
            double radius = (_chordPoint - _centerPoint).length;
            if (radius < Utils.EPSILON) return;

            double angle = Vector2.Angle(_centerPoint, point);
            _chordPoint = Vector2.Polar(_centerPoint, radius, angle);
            _textPositionManuallySet = false;
            UpdateDefinitionPoint();
        }

        #endregion
    }
}
