using System;
using System.Collections.Generic;
using System.ComponentModel;
using LitMath;
using OtoCAD;
using lcdb;
using lcdb.Colors;

namespace lcdb.Standards
{
    /// <summary>
    /// 标题栏实体类
    /// 符合ISO 10110和GB/T 13323-2009标准的标题栏
    /// </summary>
    public class TitleBlock : Entity, ICloneable
    {
        #region 私有字段

        private double _width = 180.0;
        private double _height = 56.0;
        private Vector2 _position = new Vector2(0, 0);
        private TitleBlockContent _content = new TitleBlockContent();
        private TitleBlockLayout _layout = TitleBlockLayout.ISO_Standard;

        #endregion

        #region 属性

        /// <summary>
        /// 标题栏宽度
        /// </summary>
        [Category("标题栏尺寸")]
        [DisplayName("宽度")]
        [Description("标题栏宽度 (mm)")]
        public double Width
        {
            get => _width;
            set => _width = Math.Max(value, 100.0);
        }

        /// <summary>
        /// 标题栏高度
        /// </summary>
        [Category("标题栏尺寸")]
        [DisplayName("高度")]
        [Description("标题栏高度 (mm)")]
        public double Height
        {
            get => _height;
            set => _height = Math.Max(value, 40.0);
        }

        /// <summary>
        /// 标题栏位置
        /// </summary>
        [Category("标题栏位置")]
        [DisplayName("位置")]
        [Description("标题栏左下角位置")]
        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// 标题栏内容
        /// </summary>
        [Category("标题栏内容")]
        [DisplayName("内容")]
        [Description("标题栏显示的内容信息")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TitleBlockContent Content
        {
            get => _content;
            set => _content = value ?? new TitleBlockContent();
        }

        /// <summary>
        /// 标题栏布局
        /// </summary>
        [Category("标题栏样式")]
        [DisplayName("布局样式")]
        [Description("选择标题栏布局样式")]
        public TitleBlockLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

        #endregion

        #region 构造函数

        public TitleBlock()
        {
            Initialize();
        }

        public TitleBlock(Database database) : base()
        {
            // Database will be set when added to a table
            Initialize();
        }

        private void Initialize()
        {
            _content = new TitleBlockContent();
            _layout = TitleBlockLayout.ISO_Standard;
            // TitleBlock属于Frame SuperLayer
            EditMode = EntityEditMode.Frame;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 生成标题栏实体
        /// </summary>
        public List<Entity> GenerateTitleBlockEntities(DrawingStandard standard = null)
        {
            var entities = new List<Entity>();

            switch (_layout)
            {
                case TitleBlockLayout.ISO_Standard:
                    entities.AddRange(GenerateISOLayout(standard));
                    break;
                case TitleBlockLayout.GB_Standard:
                    entities.AddRange(GenerateGBLayout(standard));
                    break;
                case TitleBlockLayout.Custom:
                    entities.AddRange(GenerateCustomLayout(standard));
                    break;
                default:
                    entities.AddRange(GenerateISOLayout(standard));
                    break;
            }

            return entities;
        }

        /// <summary>
        /// 直接绘制标题栏到画布
        /// </summary>
        public void DrawDirect(IGraphicsDraw gd, DrawingStandard standard = null)
        {
            switch (_layout)
            {
                case TitleBlockLayout.ISO_Standard:
                    DrawISOLayoutDirect(gd, standard);
                    break;
                case TitleBlockLayout.GB_Standard:
                    DrawGBLayoutDirect(gd, standard);
                    break;
                case TitleBlockLayout.Custom:
                    DrawCustomLayoutDirect(gd, standard);
                    break;
                default:
                    DrawISOLayoutDirect(gd, standard);
                    break;
            }
        }

        #endregion

        #region Entity 基类实现

        protected override DBObject CreateInstance()
        {
            return new TitleBlock();
        }

        public override object Clone()
        {
            var clone = new TitleBlock();
            clone._width = this._width;
            clone._height = this._height;
            clone._position = this._position;
            clone._content = this._content?.Clone() as TitleBlockContent;
            clone._layout = this._layout;
            return clone;
        }

        public override Bounding bounding
        {
            get
            {
                return new Bounding(
                    _position,
                    _position + new Vector2(_width, _height)
                );
            }
        }

        public override void Draw(IGraphicsDraw gd)
        {
            var entities = GenerateTitleBlockEntities();
            foreach (var entity in entities)
            {
                entity.Draw(gd);
            }
        }

        public override void Translate(Vector2 translation)
        {
            _position += translation;
        }

        public override void Rotate(Vector2 center, double angle)
        {
            _position = Vector2.RotateInRadian(_position, center, angle);
        }

        public override void TransformBy(Matrix3 transform)
        {
            _position = transform * _position;
        }

        protected void _Translate(Vector2 translation)
        {
            _position += translation;
        }

        protected void _Rotate(Vector2 anchor, double angle)
        {
            // 标题栏通常不旋转
        }

        protected void _Mirror(Vector2 anchor, Vector2 direction)
        {
            // 标题栏通常不镜像
        }

        protected void _Scale(Vector2 anchor, double factor)
        {
            _width *= factor;
            _height *= factor;
            var offset = _position - anchor;
            _position = anchor + offset * factor;
        }

        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            var snapPoints = new List<ObjectSnapPoint>();
            // 添加标题栏四个角点
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _position));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _position + new Vector2(_width, 0)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _position + new Vector2(_width, _height)));
            snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _position + new Vector2(0, _height)));
            return snapPoints;
        }

        public override List<GripPoint> GetGripPoints()
        {
            var gripPoints = new List<GripPoint>();
            // 标题栏通常不需要夹点编辑
            return gripPoints;
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            // 标题栏不支持夹点编辑
        }

        #endregion

        #region 私有方法 - 直接绘制布局

        private void DrawISOLayoutDirect(IGraphicsDraw gd, DrawingStandard standard)
        {
            // 绘制外框
            gd.DrawLine(_position, _position + new Vector2(_width, 0));
            gd.DrawLine(_position + new Vector2(_width, 0), _position + new Vector2(_width, _height));
            gd.DrawLine(_position + new Vector2(_width, _height), _position + new Vector2(0, _height));
            gd.DrawLine(_position + new Vector2(0, _height), _position);

            // ISO标准布局分区
            double[] horizontalDivisions = { 0, 60, 120, 180 };
            double[] verticalDivisions = { 0, 14, 28, 42, 56 };

            // 绘制垂直分割线
            for (int i = 1; i < horizontalDivisions.Length - 1; i++)
            {
                gd.DrawLine(
                    _position + new Vector2(horizontalDivisions[i], 0),
                    _position + new Vector2(horizontalDivisions[i], _height)
                );
            }

            // 绘制水平分割线
            for (int i = 1; i < verticalDivisions.Length - 1; i++)
            {
                gd.DrawLine(
                    _position + new Vector2(0, verticalDivisions[i]),
                    _position + new Vector2(_width, verticalDivisions[i])
                );
            }

            // 绘制ISO内容
            DrawISOContentDirect(gd);
        }

        private void DrawGBLayoutDirect(IGraphicsDraw gd, DrawingStandard standard)
        {
            // 绘制外框
            gd.DrawLine(_position, _position + new Vector2(_width, 0));
            gd.DrawLine(_position + new Vector2(_width, 0), _position + new Vector2(_width, _height));
            gd.DrawLine(_position + new Vector2(_width, _height), _position + new Vector2(0, _height));
            gd.DrawLine(_position + new Vector2(0, _height), _position);

            // GB标准布局（八个分区）
            double[] horizontalDivisions = { 0, 30, 60, 90, 120, 150, 180 };
            double[] verticalDivisions = { 0, 8, 16, 24, 32, 40, 48, 56 };

            // 绘制垂直分割线
            for (int i = 1; i < horizontalDivisions.Length - 1; i++)
            {
                gd.DrawLine(
                    _position + new Vector2(horizontalDivisions[i], 0),
                    _position + new Vector2(horizontalDivisions[i], _height)
                );
            }

            // 绘制水平分割线（部分）
            gd.DrawLine(
                _position + new Vector2(0, 8),
                _position + new Vector2(_width, 8)
            );
            gd.DrawLine(
                _position + new Vector2(0, 40),
                _position + new Vector2(120, 40)
            );

            // 绘制GB内容
            DrawGBContentDirect(gd);
        }

        private void DrawCustomLayoutDirect(IGraphicsDraw gd, DrawingStandard standard)
        {
            // 自定义布局，暂时使用ISO布局
            DrawISOLayoutDirect(gd, standard);
        }

        private void DrawISOContentDirect(IGraphicsDraw gd)
        {
            var largeTextHeight = 5.0;
            var normalTextHeight = 3.5;
            var smallTextHeight = 2.5;

            // 公司名称（顶部中央）
            gd.DrawText(
                _position + new Vector2(_width / 2, _height - 7),
                _content.CompanyName ?? "",
                largeTextHeight,
                "宋体",
                lcdb.TextAlignment.CenterMiddle,
                0
            );

            // 图纸名称（中央大字）
            gd.DrawText(
                _position + new Vector2(90, 35),
                _content.DrawingTitle ?? "",
                largeTextHeight,
                "宋体",
                lcdb.TextAlignment.CenterMiddle,
                0
            );

            // 左列标签和内容
            var leftLabels = new[] { "图号", "材料", "比例", "日期" };
            var leftValues = new[] { 
                _content.DrawingNumber ?? "", 
                _content.Material ?? "", 
                _content.Scale ?? "", 
                _content.Date ?? "" 
            };

            for (int i = 0; i < leftLabels.Length; i++)
            {
                var y = 7 + i * 7;
                gd.DrawText(
                    _position + new Vector2(5, y),
                    leftLabels[i] + ":",
                    smallTextHeight,
                    "宋体",
                    lcdb.TextAlignment.LeftMiddle,
                    0
                );
                gd.DrawText(
                    _position + new Vector2(35, y),
                    leftValues[i],
                    normalTextHeight,
                    "宋体",
                    lcdb.TextAlignment.LeftMiddle,
                    0
                );
            }

            // 右列人员信息
            var rightLabels = new[] { "设计", "校对", "审核", "批准" };
            var rightValues = new[] { 
                _content.Designer ?? "", 
                _content.Checker ?? "", 
                _content.Reviewer ?? "", 
                _content.Approver ?? "" 
            };

            for (int i = 0; i < rightLabels.Length; i++)
            {
                var y = 7 + i * 7;
                gd.DrawText(
                    _position + new Vector2(125, y),
                    rightLabels[i] + ":",
                    smallTextHeight,
                    "宋体",
                    lcdb.TextAlignment.LeftMiddle,
                    0
                );
                gd.DrawText(
                    _position + new Vector2(145, y),
                    rightValues[i],
                    normalTextHeight,
                    "宋体",
                    lcdb.TextAlignment.LeftMiddle,
                    0
                );
            }
        }

        private void DrawGBContentDirect(IGraphicsDraw gd)
        {
            var largeTextHeight = 5.0;
            var normalTextHeight = 3.5;
            var smallTextHeight = 2.5;

            // 单位名称（顶部）
            gd.DrawText(
                _position + new Vector2(90, _height - 4),
                _content.CompanyName ?? "",
                largeTextHeight,
                "宋体",
                lcdb.TextAlignment.CenterMiddle,
                0
            );

            // 图样名称（中央）
            gd.DrawText(
                _position + new Vector2(90, 24),
                _content.DrawingTitle ?? "",
                largeTextHeight,
                "宋体",
                lcdb.TextAlignment.CenterMiddle,
                0
            );

            // 图样代号
            gd.DrawText(
                _position + new Vector2(5, 4),
                "图样代号",
                smallTextHeight,
                "宋体",
                lcdb.TextAlignment.LeftMiddle,
                0
            );
            gd.DrawText(
                _position + new Vector2(60, 4),
                _content.DrawingNumber ?? "",
                normalTextHeight,
                "宋体",
                lcdb.TextAlignment.CenterMiddle,
                0
            );

            // 标记、数量、材料、比例
            var labels = new[] { "标记", "数量", "材料", "比例" };
            var values = new[] { 
                _content.PartMark ?? "", 
                _content.Quantity ?? "", 
                _content.Material ?? "", 
                _content.Scale ?? "" 
            };
            var xPositions = new[] { 15, 45, 75, 105 };

            for (int i = 0; i < labels.Length; i++)
            {
                gd.DrawText(
                    _position + new Vector2(xPositions[i], 44),
                    labels[i],
                    smallTextHeight,
                    "宋体",
                    lcdb.TextAlignment.CenterMiddle,
                    0
                );
                gd.DrawText(
                    _position + new Vector2(xPositions[i], 36),
                    values[i],
                    normalTextHeight,
                    "宋体",
                    lcdb.TextAlignment.CenterMiddle,
                    0
                );
            }

            // 人员签名区
            var signLabels = new[] { "设计", "校对", "工艺", "审核", "批准" };
            var signValues = new[] { 
                _content.Designer ?? "", 
                _content.Checker ?? "", 
                _content.TechReviewer ?? "",
                _content.Reviewer ?? "", 
                _content.Approver ?? "" 
            };
            var signX = new[] { 135, 135, 135, 165, 165 };
            var signY = new[] { 36, 28, 20, 28, 20 };

            for (int i = 0; i < signLabels.Length; i++)
            {
                gd.DrawText(
                    _position + new Vector2(signX[i], signY[i] + 4),
                    signLabels[i],
                    smallTextHeight,
                    "宋体",
                    lcdb.TextAlignment.CenterMiddle,
                    0
                );
                gd.DrawText(
                    _position + new Vector2(signX[i], signY[i]),
                    signValues[i],
                    smallTextHeight,
                    "宋体",
                    lcdb.TextAlignment.CenterMiddle,
                    0
                );
            }

            // 日期
            gd.DrawText(
                _position + new Vector2(150, 4),
                _content.Date ?? "",
                normalTextHeight,
                "宋体",
                lcdb.TextAlignment.CenterMiddle,
                0
            );
        }

        #endregion

        #region 私有方法 - 生成布局

        private List<Entity> GenerateISOLayout(DrawingStandard standard)
        {
            var entities = new List<Entity>();
            var lineStyle = standard?.GetLineStyle("Hidden");
            var lineWeight = lineStyle?.LineWeight ?? LineWeight.LineWeight035;

            // 外框
            var outerRect = new Polyline();
            outerRect.closed = true;
            outerRect.AddVertexAt(0, _position);
            outerRect.AddVertexAt(1, _position + new Vector2(_width, 0));
            outerRect.AddVertexAt(2, _position + new Vector2(_width, _height));
            outerRect.AddVertexAt(3, _position + new Vector2(0, _height));
            outerRect.lineWeight = lineWeight;
            outerRect.color = Color.ByLayer;
            outerRect.EditMode = EntityEditMode.Frame;  // TitleBlock生成的实体属于Frame SuperLayer
            entities.Add(outerRect);

            // ISO标准布局分区
            double[] horizontalDivisions = { 0, 60, 120, 180 };
            double[] verticalDivisions = { 0, 14, 28, 42, 56 };

            // 垂直分割线
            for (int i = 1; i < horizontalDivisions.Length - 1; i++)
            {
                var line = new Line(
                    _position + new Vector2(horizontalDivisions[i], 0),
                    _position + new Vector2(horizontalDivisions[i], _height)
                );
                line.lineWeight = lineWeight;
                line.color = Color.ByLayer;
                line.EditMode = EntityEditMode.Frame;  // TitleBlock生成的实体属于Frame SuperLayer
                entities.Add(line);
            }

            // 水平分割线
            for (int i = 1; i < verticalDivisions.Length - 1; i++)
            {
                var line = new Line(
                    _position + new Vector2(0, verticalDivisions[i]),
                    _position + new Vector2(_width, verticalDivisions[i])
                );
                line.lineWeight = lineWeight;
                line.color = Color.ByLayer;
                line.EditMode = EntityEditMode.Frame;  // TitleBlock生成的实体属于Frame SuperLayer
                entities.Add(line);
            }

            // 添加内容
            entities.AddRange(AddISOContent());

            return entities;
        }

        private List<Entity> GenerateGBLayout(DrawingStandard standard)
        {
            var entities = new List<Entity>();
            var lineStyle = standard?.GetLineStyle("Hidden");
            var lineWeight = lineStyle?.LineWeight ?? LineWeight.LineWeight035;

            // 外框
            var outerRect = new Polyline();
            outerRect.closed = true;
            outerRect.AddVertexAt(0, _position);
            outerRect.AddVertexAt(1, _position + new Vector2(_width, 0));
            outerRect.AddVertexAt(2, _position + new Vector2(_width, _height));
            outerRect.AddVertexAt(3, _position + new Vector2(0, _height));
            outerRect.lineWeight = lineWeight;
            outerRect.color = Color.ByLayer;
            outerRect.EditMode = EntityEditMode.Frame;  // TitleBlock生成的实体属于Frame SuperLayer
            entities.Add(outerRect);

            // GB标准布局（7个分区）
            double[] horizontalDivisions = { 0, 30, 60, 90, 120, 150, 180 };
            double[] verticalDivisions = { 0, 8, 16, 24, 32, 40, 48, 56 };

            // 垂直分割线
            for (int i = 1; i < horizontalDivisions.Length - 1; i++)
            {
                var line = new Line(
                    _position + new Vector2(horizontalDivisions[i], 0),
                    _position + new Vector2(horizontalDivisions[i], _height)
                );
                line.lineWeight = lineWeight;
                line.color = Color.ByLayer;
                line.EditMode = EntityEditMode.Frame;  // TitleBlock生成的实体属于Frame SuperLayer
                entities.Add(line);
            }

            // 水平分割线（部分）
            var horizontalLine1 = new Line(
                _position + new Vector2(0, 8),
                _position + new Vector2(_width, 8)
            );
            horizontalLine1.lineWeight = lineWeight;
            horizontalLine1.color = Color.ByLayer;
            entities.Add(horizontalLine1);

            var horizontalLine2 = new Line(
                _position + new Vector2(0, 40),
                _position + new Vector2(120, 40)
            );
            horizontalLine2.lineWeight = lineWeight;
            horizontalLine2.color = Color.ByLayer;
            entities.Add(horizontalLine2);

            // 添加内容
            entities.AddRange(AddGBContent());

            return entities;
        }

        private List<Entity> GenerateCustomLayout(DrawingStandard standard)
        {
            // 自定义布局，暂时使用ISO布局
            return GenerateISOLayout(standard);
        }

        private List<Entity> AddISOContent()
        {
            var entities = new List<Entity>();
            var largeTextHeight = 5.0;
            var normalTextHeight = 3.5;
            var smallTextHeight = 2.5;

            // 公司名称（顶部中央）
            entities.Add(CreateText(
                _content.CompanyName,
                _position + new Vector2(_width / 2, _height - 7),
                largeTextHeight,
                TextAlignment.CenterMiddle
            ));

            // 图纸名称（中央大字）
            entities.Add(CreateText(
                _content.DrawingTitle,
                _position + new Vector2(90, 35),
                largeTextHeight,
                TextAlignment.CenterMiddle
            ));

            // 左列标签和内容
            var leftLabels = new[] { "图号", "材料", "比例", "日期" };
            var leftValues = new[] { 
                _content.DrawingNumber, 
                _content.Material, 
                _content.Scale, 
                _content.Date 
            };

            for (int i = 0; i < leftLabels.Length; i++)
            {
                var y = 7 + i * 7;
                entities.Add(CreateText(
                    leftLabels[i] + ":",
                    _position + new Vector2(5, y),
                    smallTextHeight,
                    TextAlignment.LeftMiddle
                ));
                entities.Add(CreateText(
                    leftValues[i],
                    _position + new Vector2(35, y),
                    normalTextHeight,
                    TextAlignment.LeftMiddle
                ));
            }

            // 右列人员信息
            var rightLabels = new[] { "设计", "校对", "审核", "批准" };
            var rightValues = new[] { 
                _content.Designer, 
                _content.Checker, 
                _content.Reviewer, 
                _content.Approver 
            };

            for (int i = 0; i < rightLabels.Length; i++)
            {
                var y = 7 + i * 7;
                entities.Add(CreateText(
                    rightLabels[i] + ":",
                    _position + new Vector2(125, y),
                    smallTextHeight,
                    TextAlignment.LeftMiddle
                ));
                entities.Add(CreateText(
                    rightValues[i],
                    _position + new Vector2(145, y),
                    normalTextHeight,
                    TextAlignment.LeftMiddle
                ));
            }

            return entities;
        }

        private List<Entity> AddGBContent()
        {
            var entities = new List<Entity>();
            var largeTextHeight = 5.0;
            var normalTextHeight = 3.5;
            var smallTextHeight = 2.5;

            // 单位名称（顶部）
            entities.Add(CreateText(
                _content.CompanyName,
                _position + new Vector2(90, _height - 4),
                largeTextHeight,
                TextAlignment.CenterMiddle
            ));

            // 图样名称（中央）
            entities.Add(CreateText(
                _content.DrawingTitle,
                _position + new Vector2(90, 24),
                largeTextHeight,
                TextAlignment.CenterMiddle
            ));

            // 图样代号
            entities.Add(CreateText(
                "图样代号",
                _position + new Vector2(5, 4),
                smallTextHeight,
                TextAlignment.LeftMiddle
            ));
            entities.Add(CreateText(
                _content.DrawingNumber,
                _position + new Vector2(60, 4),
                normalTextHeight,
                TextAlignment.CenterMiddle
            ));

            // 标记、数量、材料、比例
            var labels = new[] { "标记", "数量", "材料", "比例" };
            var values = new[] { 
                _content.PartMark, 
                _content.Quantity, 
                _content.Material, 
                _content.Scale 
            };
            var xPositions = new[] { 15, 45, 75, 105 };

            for (int i = 0; i < labels.Length; i++)
            {
                entities.Add(CreateText(
                    labels[i],
                    _position + new Vector2(xPositions[i], 44),
                    smallTextHeight,
                    TextAlignment.CenterMiddle
                ));
                entities.Add(CreateText(
                    values[i],
                    _position + new Vector2(xPositions[i], 36),
                    normalTextHeight,
                    TextAlignment.CenterMiddle
                ));
            }

            // 人员签名区
            var signLabels = new[] { "设计", "校对", "工艺", "审核", "批准" };
            var signValues = new[] { 
                _content.Designer, 
                _content.Checker, 
                _content.TechReviewer,
                _content.Reviewer, 
                _content.Approver 
            };
            var signX = new[] { 135, 135, 135, 165, 165 };
            var signY = new[] { 36, 28, 20, 28, 20 };

            for (int i = 0; i < signLabels.Length; i++)
            {
                entities.Add(CreateText(
                    signLabels[i],
                    _position + new Vector2(signX[i], signY[i] + 4),
                    smallTextHeight,
                    TextAlignment.CenterMiddle
                ));
                entities.Add(CreateText(
                    signValues[i],
                    _position + new Vector2(signX[i], signY[i]),
                    smallTextHeight,
                    TextAlignment.CenterMiddle
                ));
            }

            // 日期
            entities.Add(CreateText(
                _content.Date,
                _position + new Vector2(150, 4),
                normalTextHeight,
                TextAlignment.CenterMiddle
            ));

            return entities;
        }

        private Text CreateText(string value, Vector2 position, double height, TextAlignment alignment)
        {
            var text = new Text();
            text.Value = value ?? "";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = height;
            text.alignment = alignment;
            text.color = Color.ByLayer;
            // TitleBlock生成的实体属于Frame SuperLayer
            text.EditMode = EntityEditMode.Frame;
            return text;
        }

        #endregion
    }

    /// <summary>
    /// 标题栏布局类型
    /// </summary>
    public enum TitleBlockLayout
    {
        [Description("ISO标准布局")]
        ISO_Standard,
        
        [Description("GB国标布局")]
        GB_Standard,
        
        [Description("自定义布局")]
        Custom
    }

    /// <summary>
    /// 标题栏内容
    /// </summary>
    public class TitleBlockContent : ICloneable
    {
        [Category("基本信息")]
        [DisplayName("公司名称")]
        [Description("公司或组织名称")]
        public string CompanyName { get; set; } = "光学设计有限公司";

        [Category("基本信息")]
        [DisplayName("图纸标题")]
        [Description("图纸标题或零件名称")]
        public string DrawingTitle { get; set; } = "单透镜";

        [Category("基本信息")]
        [DisplayName("图号")]
        [Description("图纸编号")]
        public string DrawingNumber { get; set; } = "OPT-001";

        [Category("基本信息")]
        [DisplayName("材料")]
        [Description("材料规格")]
        public string Material { get; set; } = "H-K9L";

        [Category("基本信息")]
        [DisplayName("比例")]
        [Description("图纸比例")]
        public string Scale { get; set; } = "1:1";

        [Category("基本信息")]
        [DisplayName("数量")]
        [Description("零件数量")]
        public string Quantity { get; set; } = "1";

        [Category("基本信息")]
        [DisplayName("零件标记")]
        [Description("零件标记或代号")]
        public string PartMark { get; set; } = "";

        [Category("签名信息")]
        [DisplayName("设计")]
        [Description("设计人员姓名")]
        public string Designer { get; set; } = "";

        [Category("签名信息")]
        [DisplayName("校对")]
        [Description("校对人员姓名")]
        public string Checker { get; set; } = "";

        [Category("签名信息")]
        [DisplayName("工艺")]
        [Description("工艺审核人员姓名")]
        public string TechReviewer { get; set; } = "";

        [Category("签名信息")]
        [DisplayName("审核")]
        [Description("审核人员姓名")]
        public string Reviewer { get; set; } = "";

        [Category("签名信息")]
        [DisplayName("批准")]
        [Description("批准人员姓名")]
        public string Approver { get; set; } = "";

        [Category("签名信息")]
        [DisplayName("日期")]
        [Description("创建或修改日期")]
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

        [Category("版本信息")]
        [DisplayName("版本")]
        [Description("图纸版本号")]
        public string Version { get; set; } = "A";

        [Category("版本信息")]
        [DisplayName("修订记录")]
        [Description("修订历史记录")]
        public string RevisionHistory { get; set; } = "";

        public object Clone()
        {
            return new TitleBlockContent
            {
                CompanyName = this.CompanyName,
                DrawingTitle = this.DrawingTitle,
                DrawingNumber = this.DrawingNumber,
                Material = this.Material,
                Scale = this.Scale,
                Quantity = this.Quantity,
                PartMark = this.PartMark,
                Designer = this.Designer,
                Checker = this.Checker,
                TechReviewer = this.TechReviewer,
                Reviewer = this.Reviewer,
                Approver = this.Approver,
                Date = this.Date,
                Version = this.Version,
                RevisionHistory = this.RevisionHistory
            };
        }

        public override string ToString()
        {
            return $"{DrawingTitle} - {DrawingNumber}";
        }
    }
}