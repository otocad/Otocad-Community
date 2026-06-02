using System;
using System.Collections.Generic;
using LitMath;
using OtoCAD.OpticEntity;

namespace lcdb
{
    /// <summary>
    /// 线性标注实体
    /// 原生OtoCAD实现，支持水平、垂直和旋转标注
    /// </summary>
    [Serializable]
    public class LinearDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "LinearDimension";

        #region 字段

        // 第一个参考点（起点）
        private Vector2 _firstReferencePoint = new Vector2();
        
        // 第二个参考点（终点）
        private Vector2 _secondReferencePoint = new Vector2();
        
        // 尺寸线位置 (派生缓存: 由 _offset + 参考点 + _rotation 算出, 非独立存储)
        private Vector2 _dimLinePosition = new Vector2();

        // 标注旋转角度（弧度）
        private double _rotation = 0.0;

        // ── netDxf 风格: 尺寸线由"距离" _offset 驱动 (参考线中点→尺寸线), 而非绝对坐标 ──
        // 这样移动参考点时尺寸线自动跟随; 文字再按到尺寸线的相对距离跟随尺寸线 (见 SyncDerivedGeometry)。
        private double _offset = 0.0;        // 源真值: 参考线中点到尺寸线的有符号距离
        private Vector2 _lastDimMid;         // 上一次尺寸线中点, 用于让手动文字按位移跟随
        private bool _dimMidInit = false;

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
        /// 尺寸线偏移量（参考线中点到尺寸线的有符号距离）— netDxf 风格的源真值, 驱动尺寸线位置。
        /// </summary>
        public double offset
        {
            get { return _offset; }
            set { UpdateDimensionLinePosition(value); }
        }

        /// <summary>
        /// 标注旋转角度（弧度）
        /// </summary>
        public double rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
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
        /// 是否为重要标注
        /// </summary>
        public bool IsImportant { get; set; } = true;

        /// <summary>
        /// 获取实际测量值
        /// </summary>
        public override double measurement
        {
            get
            {
                // 根据旋转角度计算投影长度
                Vector2 diff = _secondReferencePoint - _firstReferencePoint;
                
                if (Math.Abs(_rotation) < 1e-10)
                {
                    // 水平标注
                    return Math.Abs(diff.X);
                }
                else if (Math.Abs(_rotation - Math.PI / 2) < 1e-10)
                {
                    // 垂直标注
                    return Math.Abs(diff.Y);
                }
                else
                {
                    // 旋转标注 - 在旋转方向上的投影
                    Vector2 rotDir = new Vector2(Math.Cos(_rotation), Math.Sin(_rotation));
                    return Math.Abs(Vector2.Dot(diff, rotDir));
                }
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
        /// 创建线性标注（默认构造函数）
        /// </summary>
        public LinearDimension() 
            : this(Vector2.Zero, Vector2.UnitX, 0.1, 0.0)
        {
        }

        /// <summary>
        /// 创建线性标注（使用Line引用）
        /// </summary>
        /// <param name="referenceLine">参考线</param>
        /// <param name="offset">从参考线到尺寸线的距离</param>
        /// <param name="rotation">尺寸线的旋转角度（弧度）</param>
        public LinearDimension(Line referenceLine, double offset, double rotation)
            : this(referenceLine, offset, rotation, null)
        {
        }

        /// <summary>
        /// 创建线性标注（使用Line引用和样式）
        /// </summary>
        /// <param name="referenceLine">参考线</param>
        /// <param name="offset">从参考线到尺寸线的距离</param>
        /// <param name="rotation">尺寸线的旋转角度（弧度）</param>
        /// <param name="style">标注样式</param>
        public LinearDimension(Line referenceLine, double offset, double rotation, DimensionStyle style)
            : base(DimensionType.Linear)
        {
            if (referenceLine == null)
                throw new ArgumentNullException(nameof(referenceLine));
            
            _firstReferencePoint = referenceLine.startPoint;
            _secondReferencePoint = referenceLine.endPoint;
            
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "偏移值必须大于或等于零。");
            
            _rotation = rotation;
            _style = style ?? DimensionStyle.Default;
            
            // 根据偏移量计算尺寸线位置
            UpdateDimensionLinePosition(offset);
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 创建线性标注（使用两个点）
        /// </summary>
        /// <param name="firstPoint">第一个参考点</param>
        /// <param name="secondPoint">第二个参考点</param>
        /// <param name="offset">从参考线中点到尺寸线的距离</param>
        /// <param name="rotation">尺寸线的旋转角度（弧度）</param>
        public LinearDimension(Vector2 firstPoint, Vector2 secondPoint, double offset, double rotation)
            : this(firstPoint, secondPoint, offset, rotation, null)
        {
        }

        /// <summary>
        /// 创建线性标注（使用两个点和样式）
        /// </summary>
        /// <param name="firstPoint">第一个参考点</param>
        /// <param name="secondPoint">第二个参考点</param>
        /// <param name="offset">从参考线中点到尺寸线的距离</param>
        /// <param name="rotation">尺寸线的旋转角度（弧度）</param>
        /// <param name="style">标注样式</param>
        public LinearDimension(Vector2 firstPoint, Vector2 secondPoint, double offset, double rotation, DimensionStyle style)
            : base(DimensionType.Linear)
        {
            _firstReferencePoint = firstPoint;
            _secondReferencePoint = secondPoint;
            
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "偏移值必须大于或等于零。");
            
            _rotation = rotation;
            _style = style ?? DimensionStyle.Default;
            
            // 根据偏移量计算尺寸线位置
            UpdateDimensionLinePosition(offset);
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 创建线性标注 - 指定两个点、偏移量、角度和来源
        /// </summary>
        /// <param name="firstPoint">第一个参考点</param>
        /// <param name="secondPoint">第二个参考点</param>
        /// <param name="offset">从参考线中点到尺寸线的距离</param>
        /// <param name="rotation">尺寸线的旋转角度（弧度）</param>
        /// <param name="source">实体来源</param>
        /// <param name="parentId">父组件ID</param>
        public LinearDimension(Vector2 firstPoint, Vector2 secondPoint, double offset, double rotation, EntitySource source, ObjectId? parentId = null)
            : this(firstPoint, secondPoint, offset, rotation)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        /// <summary>
        /// 创建线性标注（兼容旧版本）
        /// </summary>
        [Obsolete("请使用基于偏移量的构造函数")]
        public LinearDimension(Vector2 firstPoint, Vector2 secondPoint, Vector2 dimLinePos) 
            : base(DimensionType.Linear)
        {
            _firstReferencePoint = firstPoint;
            _secondReferencePoint = secondPoint;
            _dimLinePosition = dimLinePos;
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 创建线性标注（兼容旧版本，带旋转角度）
        /// </summary>
        [Obsolete("请使用基于偏移量的构造函数")]
        public LinearDimension(Vector2 firstPoint, Vector2 secondPoint, Vector2 dimLinePos, double rotation) 
            : this(firstPoint, secondPoint, dimLinePos)
        {
            _rotation = rotation;
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

            // netDxf 风格: 尺寸线由 _offset 派生 (跟随参考点), 手动文字按尺寸线位移跟随
            SyncDerivedGeometry();

            // 计算标注线的端点
            Vector2 dimDir = new Vector2(Math.Cos(_rotation), Math.Sin(_rotation));
            Vector2 extDir = new Vector2(-dimDir.Y, dimDir.X); // 垂直于标注方向

            // 计算第一和第二参考点在尺寸线上的投影
            Vector2 dimStart = ProjectPointOnLine(_firstReferencePoint, _dimLinePosition, dimDir);
            Vector2 dimEnd = ProjectPointOnLine(_secondReferencePoint, _dimLinePosition, dimDir);
            
            // 确定延伸线的正确方向，避免交叉
            // 计算参考点中点到尺寸线的向量，确保延伸线指向正确方向
            Vector2 refMidPoint = (_firstReferencePoint + _secondReferencePoint) * 0.5;
            Vector2 toDimLine = _dimLinePosition - refMidPoint;
            double dotProduct = Vector2.Dot(toDimLine, extDir);
            
            // 如果点积为负，说明extDir指向错误方向，需要反向
            if (dotProduct < 0)
            {
                extDir = -extDir;
            }

            // 确保dimStart在dimEnd左边（或下边），同时跟踪对应的参考点
            Vector2 refPoint1 = _firstReferencePoint;
            Vector2 refPoint2 = _secondReferencePoint;
            
            if (Vector2.Dot(dimEnd - dimStart, dimDir) < 0)
            {
                Vector2 temp = dimStart;
                dimStart = dimEnd;
                dimEnd = temp;
                
                // 同时交换对应的参考点
                temp = refPoint1;
                refPoint1 = refPoint2;
                refPoint2 = temp;
            }

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
                Vector2 ext1Start = refPoint1 + extDir * _style.ExtensionLineOffset;
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
                Vector2 ext2Start = refPoint2 + extDir * _style.ExtensionLineOffset;
                Vector2 ext2End = dimEnd + extDir * _style.ExtensionLineExtend;
                
                Line extLine2 = new Line(ext2Start, ext2End)
                {
                    color = this.color,
                    layer = this.layer
                };
                _generatedEntities.Add(extLine2);
            }

            // 创建箭头
            Solid arrow1 = CreateArrowhead(dimStart, dimEnd - dimStart, _style.ArrowSize);
            Solid arrow2 = CreateArrowhead(dimEnd, dimStart - dimEnd, _style.ArrowSize);
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
            Vector2 textAnchor = ProjectPointOnLine(_textReferencePoint, _dimLinePosition, dimDir);
            
            // 设置文本旋转
            if (_style.TextInsideHorizontal)
            {
                dimText.Rotation = 0; // 文本保持水平
            }
            else
            {
                dimText.Rotation = _rotation; // 文本跟随标注线方向
            }

            if (_style.TextInsideHorizontal)
            {
                double textSide = Vector2.Dot(_textReferencePoint - textAnchor, extDir);
                if (Math.Abs(extDir.X) >= Math.Abs(extDir.Y))
                {
                    dimText.alignment = textSide >= 0 ? TextAlignment.LeftMiddle : TextAlignment.RightMiddle;
                }
                else
                {
                    dimText.alignment = textSide >= 0 ? TextAlignment.CenterBottom : TextAlignment.CenterTop;
                }
            }
            else
            {
                dimText.alignment = TextAlignment.CenterBottom;
            }
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
            return new LinearDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            LinearDimension dimension = base.Clone() as LinearDimension;
            dimension._firstReferencePoint = _firstReferencePoint;
            dimension._secondReferencePoint = _secondReferencePoint;
            dimension._dimLinePosition = _dimLinePosition;
            dimension._rotation = _rotation;
            dimension._offset = _offset;       // 源真值随克隆保留
            dimension._dimMidInit = false;     // _lastDimMid 在新实例首次 Generate 时重新初始化, 避免误判位移
            dimension.IsImportant = IsImportant;
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

            // 文字夹点恒在 (即使未手动定位), 让用户随时单独拖文字; 拖后转手动并保持相对尺寸线跟随。
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
                case 0: // 第一参考点 — 自由拖 (允许调整测量区间端点)
                    _firstReferencePoint = newPosition;
                    break;
                case 1: // 第二参考点 — 自由拖
                    _secondReferencePoint = newPosition;
                    break;
                case 2: // 尺寸线中点 — 反推 offset (必要时翻 rotation); 文字跟随由 SyncDerivedGeometry 统一处理
                    SetDimensionLinePosition(newPosition);
                    Update();
                    return;
                case 3: // 文字 — 自由拖 (排版); 转手动, 之后由 SyncDerivedGeometry 保持相对尺寸线跟随
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
            // 同步 _lastDimMid: 文字已由基类 Translate 平移, 避免下次 Generate 再按位移多移一次
            if (_dimMidInit) _lastDimMid += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _firstReferencePoint = Vector2.RotateInRadian(_firstReferencePoint, center, angle);
            _secondReferencePoint = Vector2.RotateInRadian(_secondReferencePoint, center, angle);
            _dimLinePosition = Vector2.RotateInRadian(_dimLinePosition, center, angle);
            if (_dimMidInit) _lastDimMid = Vector2.RotateInRadian(_lastDimMid, center, angle);
            _rotation += angle;
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _firstReferencePoint = transform * _firstReferencePoint;
            _secondReferencePoint = transform * _secondReferencePoint;
            _dimLinePosition = transform * _dimLinePosition;
            if (_dimMidInit) _lastDimMid = transform * _lastDimMid;
            // TODO: 从变换矩阵中提取旋转角度
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
        /// 将点投影到直线上
        /// </summary>
        private Vector2 ProjectPointOnLine(Vector2 point, Vector2 linePoint, Vector2 lineDir)
        {
            Vector2 v = point - linePoint;
            double t = Vector2.Dot(v, lineDir);
            return linePoint + lineDir * t;
        }

        /// <summary>
        /// 根据偏移量更新尺寸线位置
        /// </summary>
        private void UpdateDimensionLinePosition(double offset)
        {
            _offset = offset;   // 源真值; _dimLinePosition 仅作即时缓存 (Generate 时由 SyncDerivedGeometry 重算)
            Vector2 midRef = (_firstReferencePoint + _secondReferencePoint) * 0.5;
            Vector2 dimDir = new Vector2(Math.Cos(_rotation), Math.Sin(_rotation));
            Vector2 perpDir = new Vector2(-dimDir.Y, dimDir.X);

            _dimLinePosition = midRef + perpDir * offset;
        }

        /// <summary>
        /// netDxf 风格的派生几何同步: 尺寸线位置由 _offset(距离) + 参考点 + 旋转 算出 → 移动参考点时尺寸线跟随;
        /// 手动定位的文字按尺寸线中点的位移量同步平移 → 保持到尺寸线的相对距离, 自动跟随。
        /// 每次 Generate 前调用; 无变化时位移为 0, 不漂移。
        /// </summary>
        private void SyncDerivedGeometry()
        {
            Vector2 midRef = (_firstReferencePoint + _secondReferencePoint) * 0.5;
            Vector2 dimDir = new Vector2(Math.Cos(_rotation), Math.Sin(_rotation));
            Vector2 perpDir = new Vector2(-dimDir.Y, dimDir.X);
            Vector2 newDimMid = midRef + perpDir * _offset;

            if (!_dimMidInit)
            {
                _lastDimMid = newDimMid;
                _dimMidInit = true;
            }
            else if (_textPositionManuallySet)
            {
                Vector2 delta = newDimMid - _lastDimMid;
                if (delta.length > 1e-9) _textReferencePoint += delta;
            }

            _dimLinePosition = newDimMid;
            _lastDimMid = newDimMid;
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 设置为水平标注
        /// </summary>
        public void SetHorizontal()
        {
            _rotation = 0.0;
        }

        /// <summary>
        /// 设置为垂直标注
        /// </summary>
        public void SetVertical()
        {
            _rotation = Math.PI / 2;
        }

        /// <summary>
        /// 按尺寸线上一点反推 offset (和必要时的 rotation 翻转).
        /// 对接 netDxf.LinearDimension.SetDimensionLinePosition: Grip 拖拽 dimLine 时单点调用即可.
        /// </summary>
        public void SetDimensionLinePosition(Vector2 point)
        {
            Vector2 midRef = (_firstReferencePoint + _secondReferencePoint) * 0.5;
            Vector2 dimDir = new Vector2(Math.Cos(_rotation), Math.Sin(_rotation));
            Vector2 pointDir = point - _firstReferencePoint;

            // cross < 0 表示点落在尺寸线反向, 翻转 rotation 让 offset 始终 ≥ 0
            double cross = Vector2.Cross(dimDir, pointDir);
            if (cross < 0)
            {
                _rotation += Math.PI;
                dimDir = -dimDir;
            }

            double off = Utils.PointLineDistance(midRef, point, dimDir);
            UpdateDimensionLinePosition(off);
            UpdateDefinitionPoint();
        }

        #endregion
    }
}
