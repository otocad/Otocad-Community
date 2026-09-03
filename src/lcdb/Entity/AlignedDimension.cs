using System;
using System.Collections.Generic;
using LitMath;


namespace lcdb
{
    /// <summary>
    /// 对齐标注实体
    /// 原生OtoCAD实现，标注线平行于两个参考点的连线
    /// </summary>
    [Serializable]
    public class AlignedDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "AlignedDimension";

        #region 字段

        // 第一个参考点（起点）
        private Vector2 _firstReferencePoint = new Vector2();
        
        // 第二个参考点（终点）
        private Vector2 _secondReferencePoint = new Vector2();
        
        // 尺寸线位置
        private Vector2 _dimLinePosition = new Vector2();

        #endregion

        #region 属性

        /// <summary>
        /// 第一个参考点
        /// </summary>
        public Vector2 firstReferencePoint
        {
            get { return _firstReferencePoint; }
            set 
            { 
                _firstReferencePoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 第二个参考点
        /// </summary>
        public Vector2 secondReferencePoint
        {
            get { return _secondReferencePoint; }
            set 
            { 
                _secondReferencePoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 尺寸线位置
        /// </summary>
        public Vector2 dimLinePosition
        {
            get { return _dimLinePosition; }
            set { _dimLinePosition = value; }
        }
        
        /// <summary>
        /// 第一个点（便利属性）
        /// </summary>
        public Vector2 firstPoint
        {
            get { return _firstReferencePoint; }
            set 
            { 
                _firstReferencePoint = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 第二个点（便利属性）
        /// </summary>
        public Vector2 secondPoint
        {
            get { return _secondReferencePoint; }
            set 
            { 
                _secondReferencePoint = value;
                UpdateDefinitionPoint();
            }
        }
        
        /// <summary>
        /// 尺寸线（便利属性）
        /// </summary>
        public Vector2 dimensionLine
        {
            get { return _dimLinePosition; }
            set { _dimLinePosition = value; }
        }

        /// <summary>
        /// 获取实际测量值
        /// </summary>
        public override double measurement
        {
            get
            {
                // 对齐标注测量两点之间的直线距离
                return (_secondReferencePoint - _firstReferencePoint).length;
            }
        }

        /// <summary>
        /// 获取对齐角度（弧度）
        /// </summary>
        public double alignmentAngle
        {
            get
            {
                Vector2 dir = _secondReferencePoint - _firstReferencePoint;
                return Math.Atan2(dir.Y, dir.X);
            }
        }

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                List<Vector2> points = new List<Vector2>();
                points.Add(_firstReferencePoint);
                points.Add(_secondReferencePoint);
                points.Add(_dimLinePosition);
                
                // 添加文本位置
                if (_textReferencePoint != null)
                {
                    points.Add(_textReferencePoint);
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
        /// 创建对齐标注
        /// </summary>
        public AlignedDimension() : base(DimensionType.Aligned)
        {
        }

        /// <summary>
        /// 创建对齐标注
        /// </summary>
        public AlignedDimension(Vector2 firstPoint, Vector2 secondPoint, Vector2 dimLinePos) 
            : base(DimensionType.Aligned)
        {
            _firstReferencePoint = firstPoint;
            _secondReferencePoint = secondPoint;
            _dimLinePosition = dimLinePos;
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

            // 计算标注线的方向（平行于两参考点连线）
            Vector2 dimDir = (_secondReferencePoint - _firstReferencePoint).normalized;
            Vector2 extDir = new Vector2(-dimDir.Y, dimDir.X); // 垂直于标注方向

            // 计算尺寸线位置到两参考点连线的距离
            double distance = PointToLineDistance(_dimLinePosition, _firstReferencePoint, _secondReferencePoint);
            
            // 确定尺寸线在哪一侧
            Vector2 toPos = _dimLinePosition - _firstReferencePoint;
            double side = Vector2.Dot(toPos, extDir);
            if (side < 0)
            {
                extDir = -extDir;
                distance = -distance;
            }

            // 计算尺寸线的端点
            Vector2 dimStart = _firstReferencePoint + extDir * distance;
            Vector2 dimEnd = _secondReferencePoint + extDir * distance;

            // 创建尺寸线
            Line dimLine = new Line(dimStart, dimEnd)
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(dimLine);

            // 创建尺寸界线1
            if (_style.ShowExtensionLine1)
            {
                Vector2 ext1Start = _firstReferencePoint + extDir * _style.ExtensionLineOffset;
                Vector2 ext1End = dimStart + extDir * _style.ExtensionLineExtend;
                
                Line extLine1 = new Line(ext1Start, ext1End)
                {
                    color = this.color,
                    layer = this.layer
                };
                _generatedEntities.Add(extLine1);
            }

            // 创建尺寸界线2
            if (_style.ShowExtensionLine2)
            {
                Vector2 ext2Start = _secondReferencePoint + extDir * _style.ExtensionLineOffset;
                Vector2 ext2End = dimEnd + extDir * _style.ExtensionLineExtend;
                
                Line extLine2 = new Line(ext2Start, ext2End)
                {
                    color = this.color,
                    layer = this.layer
                };
                _generatedEntities.Add(extLine2);
            }

            // 创建箭头
            Solid arrow1 = CreateArrowhead(dimStart, dimDir, _style.ArrowSize);
            Solid arrow2 = CreateArrowhead(dimEnd, -dimDir, _style.ArrowSize);
            _generatedEntities.Add(arrow1);
            _generatedEntities.Add(arrow2);

            // 计算文本位置
            if (!_textPositionManuallySet)
            {
                _textReferencePoint = (dimStart + dimEnd) * 0.5 + extDir * _style.DimensionLineGap;
            }

            // 创建文本
            string text = GetFormattedText();
            Text dimText = CreateDimensionText(_textReferencePoint, text, _style.TextHeight);
            
            // 设置文本旋转
            if (_style.TextInsideHorizontal)
            {
                dimText.angle = 0; // 文本保持水平
            }
            else
            {
                // 文本跟随标注线方向，但确保文本总是正向的
                double angle = alignmentAngle;
                if (angle > Math.PI / 2)
                {
                    angle -= Math.PI;
                }
                else if (angle < -Math.PI / 2)
                {
                    angle += Math.PI;
                }
                dimText.angle = angle;
            }
            
            dimText.alignment = TextAlignment.CenterBottom;
            _generatedEntities.Add(dimText);
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
            return new AlignedDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            AlignedDimension dimension = base.Clone() as AlignedDimension;
            dimension._firstReferencePoint = _firstReferencePoint;
            dimension._secondReferencePoint = _secondReferencePoint;
            dimension._dimLinePosition = _dimLinePosition;
            return dimension;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.End, _firstReferencePoint));
            gripPoints.Add(new GripPoint(GripPointType.End, _secondReferencePoint));
            gripPoints.Add(new GripPoint(GripPointType.Mid, _dimLinePosition));

            // 文字夹点恒在 (即使未手动定位), 让用户随时单独拖文字
            gripPoints.Add(new GripPoint(GripPointType.Center, _textReferencePoint));

            return gripPoints;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0:
                    _firstReferencePoint = newPosition;
                    break;
                case 1:
                    _secondReferencePoint = newPosition;
                    break;
                case 2:
                {
                    // 尺寸线: 手动文字按尺寸线位移同步平移 → 保持相对位置跟随
                    Vector2 oldDim = _dimLinePosition;
                    _dimLinePosition = newPosition;
                    if (_textPositionManuallySet) _textReferencePoint += _dimLinePosition - oldDim;
                    break;
                }
                case 3: // 文字 — 自由拖 (排版); 转手动, 之后随尺寸线跟随
                    _textReferencePoint = newPosition;
                    _textPositionManuallySet = true;
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
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _firstReferencePoint));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _secondReferencePoint));
            
            // 添加尺寸线中点
            Vector2 dimMid = (_firstReferencePoint + _secondReferencePoint) * 0.5;
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Mid, dimMid));
            
            return snapPoints;
        }

        #endregion

        #region 重写基类方法

        /// <summary>
        /// 子类特定的平移操作
        /// </summary>
        protected override void TranslateSpecific(Vector2 translation)
        {
            _firstReferencePoint += translation;
            _secondReferencePoint += translation;
            _dimLinePosition += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _firstReferencePoint = Vector2.RotateInRadian(_firstReferencePoint, center, angle);
            _secondReferencePoint = Vector2.RotateInRadian(_secondReferencePoint, center, angle);
            _dimLinePosition = Vector2.RotateInRadian(_dimLinePosition, center, angle);
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _firstReferencePoint = transform * _firstReferencePoint;
            _secondReferencePoint = transform * _secondReferencePoint;
            _dimLinePosition = transform * _dimLinePosition;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新定义点
        /// </summary>
        private void UpdateDefinitionPoint()
        {
            _definitionPoint = (_firstReferencePoint + _secondReferencePoint) * 0.5;
        }

        /// <summary>
        /// 计算点到直线的距离
        /// </summary>
        private double PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 lineVec = lineEnd - lineStart;
            Vector2 pointVec = point - lineStart;
            
            double lineLength = lineVec.length;
            if (lineLength < 1e-10)
                return pointVec.length;
                
            double t = Vector2.Dot(pointVec, lineVec) / (lineLength * lineLength);
            t = Math.Max(0, Math.Min(1, t));
            
            Vector2 projection = lineStart + lineVec * t;
            return (point - projection).length;
        }

        /// <summary>
        /// 设置标注线位置
        /// </summary>
        public void SetDimensionLinePosition(Vector2 position)
        {
            _dimLinePosition = position;
        }

        #endregion
    }
}