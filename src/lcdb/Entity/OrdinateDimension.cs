using System;
using System.Collections.Generic;
using LitMath;


namespace lcdb
{
    /// <summary>
    /// 坐标标注实体
    /// 原生OtoCAD实现，用于标注点相对于基准点的X或Y坐标
    /// </summary>
    [Serializable]
    public class OrdinateDimension : DimensionBase
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override string className => "OrdinateDimension";

        #region 字段

        // 定义点（要标注坐标的点）
        private Vector2 _featurePoint = new Vector2();
        
        // 引线终点
        private Vector2 _leaderEndPoint = new Vector2();
        
        // 基准点（原点）
        private Vector2 _origin = new Vector2();
        
        // 坐标类型（X或Y）
        private OrdinateDimensionType _ordinateType = OrdinateDimensionType.XCoordinate;
        
        // 是否使用X轴
        private bool _useXAxis = true;

        #endregion

        #region 属性

        /// <summary>
        /// 特征点（要标注坐标的点）
        /// </summary>
        public Vector2 featurePoint
        {
            get { return _featurePoint; }
            set 
            { 
                _featurePoint = value;
                UpdateDefinitionPoint();
            }
        }

        /// <summary>
        /// 引线终点
        /// </summary>
        public Vector2 leaderEndPoint
        {
            get { return _leaderEndPoint; }
            set 
            { 
                _leaderEndPoint = value;
                DetermineOrdinateType();
            }
        }

        /// <summary>
        /// 基准点（原点）
        /// </summary>
        public Vector2 origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        /// <summary>
        /// 坐标类型
        /// </summary>
        public OrdinateDimensionType ordinateType
        {
            get { return _ordinateType; }
            set 
            { 
                _ordinateType = value;
                _useXAxis = (_ordinateType == OrdinateDimensionType.XCoordinate);
            }
        }

        /// <summary>
        /// 是否使用X轴
        /// </summary>
        public bool useXAxis
        {
            get { return _useXAxis; }
            set 
            { 
                _useXAxis = value;
                _ordinateType = _useXAxis ? OrdinateDimensionType.XCoordinate : OrdinateDimensionType.YCoordinate;
            }
        }

        /// <summary>
        /// 获取实际测量值
        /// </summary>
        public override double measurement
        {
            get
            {
                if (_useXAxis)
                {
                    return _featurePoint.X - _origin.X;
                }
                else
                {
                    return _featurePoint.Y - _origin.Y;
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
                points.Add(_featurePoint);
                points.Add(_leaderEndPoint);
                
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
        /// 创建坐标标注
        /// </summary>
        public OrdinateDimension() : base(DimensionType.Ordinate)
        {
        }

        /// <summary>
        /// 创建坐标标注
        /// </summary>
        /// <param name="featurePt">特征点</param>
        /// <param name="leaderEndPt">引线终点</param>
        /// <param name="useX">是否标注X坐标</param>
        public OrdinateDimension(Vector2 featurePt, Vector2 leaderEndPt, bool useX = true) 
            : base(DimensionType.Ordinate)
        {
            _featurePoint = featurePt;
            _leaderEndPoint = leaderEndPt;
            _useXAxis = useX;
            _ordinateType = useX ? OrdinateDimensionType.XCoordinate : OrdinateDimensionType.YCoordinate;
            UpdateDefinitionPoint();
        }

        /// <summary>
        /// 创建坐标标注（指定原点）
        /// </summary>
        /// <param name="origin">原点</param>
        /// <param name="featurePt">特征点</param>
        /// <param name="leaderEndPt">引线终点</param>
        /// <param name="useX">是否标注X坐标</param>
        public OrdinateDimension(Vector2 origin, Vector2 featurePt, Vector2 leaderEndPt, bool useX = true) 
            : base(DimensionType.Ordinate)
        {
            _origin = origin;
            _featurePoint = featurePt;
            _leaderEndPoint = leaderEndPt;
            _useXAxis = useX;
            _ordinateType = useX ? OrdinateDimensionType.XCoordinate : OrdinateDimensionType.YCoordinate;
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

            // 计算引线路径
            Vector2 doglegStart, doglegEnd;
            CalculateDoglegPoints(out doglegStart, out doglegEnd);

            // 创建引线
            // 第一段：从特征点到折弯点
            if ((_featurePoint - doglegStart).length > 1e-10)
            {
                Line line1 = new Line(_featurePoint, doglegStart)
                {
                    color = this.color,
                    layer = this.layer
                };
                _generatedEntities.Add(line1);
            }

            // 第二段：从折弯点到终点
            if ((doglegEnd - doglegStart).length > 1e-10)
            {
                Line line2 = new Line(doglegStart, doglegEnd)
                {
                    color = this.color,
                    layer = this.layer
                };
                _generatedEntities.Add(line2);
            }

            // 计算文本位置
            if (!_textPositionManuallySet)
            {
                // 文本放在引线终点附近
                Vector2 textDir = (doglegEnd - doglegStart).normalized;
                _textReferencePoint = doglegEnd + textDir * _style.DimensionLineGap;
            }

            // 创建文本
            string text = GetOrdinateFormattedText();
            Text dimText = CreateDimensionText(_textReferencePoint, text, _style.TextHeight);
            
            // 文本水平对齐
            dimText.angle = 0;
            
            // 根据引线方向决定文本对齐
            Vector2 leaderDir = doglegEnd - doglegStart;
            if (Math.Abs(leaderDir.X) > Math.Abs(leaderDir.Y))
            {
                // 水平引线
                dimText.alignment = leaderDir.X > 0 ? TextAlignment.LeftMiddle : TextAlignment.RightMiddle;
            }
            else
            {
                // 垂直引线
                dimText.alignment = leaderDir.Y > 0 ? TextAlignment.CenterBottom : TextAlignment.CenterTop;
            }
            
            _generatedEntities.Add(dimText);

            // 在特征点处创建一个小标记
            double markSize = _style.ArrowSize * 0.3;
            Line markH = new Line(
                _featurePoint - new Vector2(markSize, 0),
                _featurePoint + new Vector2(markSize, 0))
            {
                color = this.color,
                layer = this.layer
            };
            Line markV = new Line(
                _featurePoint - new Vector2(0, markSize),
                _featurePoint + new Vector2(0, markSize))
            {
                color = this.color,
                layer = this.layer
            };
            _generatedEntities.Add(markH);
            _generatedEntities.Add(markV);
        }

        /// <summary>
        /// 计算折弯点
        /// </summary>
        private void CalculateDoglegPoints(out Vector2 doglegStart, out Vector2 doglegEnd)
        {
            // 坐标标注的引线通常有一个折弯
            // 根据标注类型确定折弯方向
            
            if (_useXAxis)
            {
                // X坐标标注：引线主要是垂直的
                double deltaX = _leaderEndPoint.X - _featurePoint.X;
                double deltaY = _leaderEndPoint.Y - _featurePoint.Y;
                
                if (Math.Abs(deltaY) > _style.TextHeight * 2)
                {
                    // 需要折弯
                    doglegStart = new Vector2(_featurePoint.X, _leaderEndPoint.Y);
                    doglegEnd = _leaderEndPoint;
                }
                else
                {
                    // 直接连接
                    doglegStart = _featurePoint;
                    doglegEnd = _leaderEndPoint;
                }
            }
            else
            {
                // Y坐标标注：引线主要是水平的
                double deltaX = _leaderEndPoint.X - _featurePoint.X;
                double deltaY = _leaderEndPoint.Y - _featurePoint.Y;
                
                if (Math.Abs(deltaX) > _style.TextHeight * 2)
                {
                    // 需要折弯
                    doglegStart = new Vector2(_leaderEndPoint.X, _featurePoint.Y);
                    doglegEnd = _leaderEndPoint;
                }
                else
                {
                    // 直接连接
                    doglegStart = _featurePoint;
                    doglegEnd = _leaderEndPoint;
                }
            }
        }

        /// <summary>
        /// 根据引线终点位置自动确定坐标类型
        /// </summary>
        private void DetermineOrdinateType()
        {
            // 根据引线的主要方向确定是X还是Y坐标
            double deltaX = Math.Abs(_leaderEndPoint.X - _featurePoint.X);
            double deltaY = Math.Abs(_leaderEndPoint.Y - _featurePoint.Y);
            
            if (deltaY > deltaX)
            {
                // 垂直引线，标注X坐标
                _useXAxis = true;
                _ordinateType = OrdinateDimensionType.XCoordinate;
            }
            else
            {
                // 水平引线，标注Y坐标
                _useXAxis = false;
                _ordinateType = OrdinateDimensionType.YCoordinate;
            }
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
            return new OrdinateDimension();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            OrdinateDimension dimension = base.Clone() as OrdinateDimension;
            dimension._featurePoint = _featurePoint;
            dimension._leaderEndPoint = _leaderEndPoint;
            dimension._origin = _origin;
            dimension._ordinateType = _ordinateType;
            dimension._useXAxis = _useXAxis;
            return dimension;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, _featurePoint));
            gripPoints.Add(new GripPoint(GripPointType.End, _leaderEndPoint));
            
            // 折弯点夹点
            Vector2 doglegStart, doglegEnd;
            CalculateDoglegPoints(out doglegStart, out doglegEnd);
            if ((doglegStart - _featurePoint).length > 1e-10 && 
                (doglegEnd - doglegStart).length > 1e-10)
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, doglegStart));
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
                case 0: // 特征点
                    _featurePoint = newPosition;
                    break;
                case 1: // 引线终点
                    _leaderEndPoint = newPosition;
                    DetermineOrdinateType();
                    break;
                case 2: // 折弯点
                    // 调整引线终点以保持折弯
                    if (_useXAxis)
                    {
                        _leaderEndPoint = new Vector2(newPosition.X, _leaderEndPoint.Y);
                    }
                    else
                    {
                        _leaderEndPoint = new Vector2(_leaderEndPoint.X, newPosition.Y);
                    }
                    break;
                case 3: // 文本位置
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
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Node, _featurePoint));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _leaderEndPoint));
            
            Vector2 doglegStart, doglegEnd;
            CalculateDoglegPoints(out doglegStart, out doglegEnd);
            if ((doglegStart - _featurePoint).length > 1e-10)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, doglegStart));
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
            _featurePoint += translation;
            _leaderEndPoint += translation;
            _origin += translation;
        }

        /// <summary>
        /// 子类特定的旋转操作
        /// </summary>
        protected override void RotateSpecific(Vector2 center, double angle)
        {
            _featurePoint = Vector2.RotateInRadian(_featurePoint, center, angle);
            _leaderEndPoint = Vector2.RotateInRadian(_leaderEndPoint, center, angle);
            _origin = Vector2.RotateInRadian(_origin, center, angle);
            // 旋转后可能需要重新确定坐标类型
            DetermineOrdinateType();
        }

        /// <summary>
        /// 子类特定的变换操作
        /// </summary>
        protected override void TransformBySpecific(Matrix3 transform)
        {
            _featurePoint = transform * _featurePoint;
            _leaderEndPoint = transform * _leaderEndPoint;
            _origin = transform * _origin;
            // 变换后可能需要重新确定坐标类型
            DetermineOrdinateType();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新定义点
        /// </summary>
        private void UpdateDefinitionPoint()
        {
            _definitionPoint = _featurePoint;
        }

        /// <summary>
        /// 获取格式化的文本
        /// </summary>
        protected string GetOrdinateFormattedText()
        {
            if (!string.IsNullOrEmpty(_userText))
            {
                // 替换<>为实际测量值
                return _userText.Replace("<>", FormatMeasurement(measurement));
            }
            
            // 添加坐标前缀
            string prefix = _useXAxis ? "X " : "Y ";
            return prefix + FormatMeasurement(measurement);
        }

        #endregion
    }

    /// <summary>
    /// 坐标标注类型
    /// </summary>
    public enum OrdinateDimensionType
    {
        /// <summary>
        /// X坐标
        /// </summary>
        XCoordinate,
        
        /// <summary>
        /// Y坐标
        /// </summary>
        YCoordinate
    }
}