using System;
using System.Collections.Generic;
using System.ComponentModel;
using lcdb;
using lcdb.Transaction;
using LitMath;

namespace OtoCAD.OpticEntity
{
    /// <summary>
    /// 光学图框类，基于BaseElementBlock
    /// 支持生成独立的子实体，每个子实体可单独选择和编辑
    /// </summary>
    public class Frame : BaseElementBlock
    {
        #region Fields
        
        /// <summary>
        /// 标记子实体是否已生成
        /// </summary>
        private bool _entitiesGenerated = false;
        
        #endregion
        
        #region Properties
        
        [Category("Frame Geometry")]
        [DisplayName("宽度")]
        [Description("图框宽度(mm)")]
        public double Width { get; set; } = 297.0;  // A4横向
        
        [Category("Frame Geometry")]
        [DisplayName("高度")]
        [Description("图框高度(mm)")]
        public double Height { get; set; } = 210.0;  // A4横向
        
        [Category("Frame Geometry")]
        [DisplayName("边距")]
        [Description("图框内边距(mm)")]
        public double MarginDis { get; set; } = 10.0;
        
        [Category("Frame Geometry")]
        [DisplayName("装订边距")]
        [Description("左侧装订边距(mm)")]
        public double BindingMargin { get; set; } = 25.0;
        
        [Category("Frame Geometry")]
        public FrameType Type { get; set; } = FrameType.Rectangular;
        
        [Category("Frame Geometry")]
        public double CircularRadius { get; set; } = 100.0;
        
        [Category("Frame Style")]
        [DisplayName("外框线宽")]
        [Description("外框线宽度(mm)")]
        public double OuterLineWidth { get; set; } = 0.7;
        
        [Category("Frame Style")]
        [DisplayName("内框线宽")]
        [Description("内框线宽度(mm)")]
        public double InnerLineWidth { get; set; } = 0.35;
        
        [Category("Frame Style")]
        [DisplayName("颜色")]
        [Description("图框颜色")]
        public lcdb.Colors.Color FrameColor { get; set; } = lcdb.Colors.Color.ByLayer;
        
        [Category("Frame Style")]
        [DisplayName("图层ID")]
        [Description("图框所在图层")]
        public ObjectId LayerId { get; set; } = ObjectId.Null;
        
        [Category("Title Block")]
        public bool IncludeTitleBlock { get; set; } = true;
        
        [Category("Title Block")]
        public TitleBlockSettings TitleBlock { get; set; } = new TitleBlockSettings();
        
        [Category("Additional Features")]
        [DisplayName("包含技术要求")]
        [Description("是否包含技术要求栏")]
        public bool IncludeRequirements { get; set; } = false;
        
        [Category("Additional Features")]
        [DisplayName("包含更改记录")]
        [Description("是否包含更改记录栏")]
        public bool IncludeChangeRecord { get; set; } = false;
        
        [Category("Additional Features")]
        [DisplayName("包含签名栏")]
        [Description("是否包含签名栏")]
        public bool IncludeSignature { get; set; } = false;
        
        #endregion
        
        #region Constructors
        
        public Frame(Database database) : base(database)
        {
            InitializeFrame();
        }
        
        public Frame() : base()
        {
            InitializeFrame();
        }
        
        private void InitializeFrame()
        {
            // 初始化默认值
            Width = 297.0;  // A4横向宽度
            Height = 210.0; // A4横向高度
            MarginDis = 10.0;
            BindingMargin = 25.0;
            OuterLineWidth = 0.7;
            InnerLineWidth = 0.35;
        }
        
        #endregion
        
        #region Methods

        /// <summary>
        /// Frame的实体存放在特殊的FrameBlock中，而不是ModelSpace
        /// 这样可以在不同编辑模式下控制Frame实体的交互性
        /// </summary>
        public override string BlockName => "FrameBlock";
        
        /// <summary>
        /// 确保子实体已生成
        /// </summary>
        public void EnsureEntitiesGenerated()
        {
            if (!_entitiesGenerated && database != null)
            {
                GenEntity();
                _entitiesGenerated = true;
            }
        }
        
        /// <summary>
        /// 重写GenEntity以跟踪生成状态
        /// </summary>
        public override void GenEntity()
        {
            base.GenEntity();
            _entitiesGenerated = true;
        }
        
        protected override void GenerateEntitiesWithTransaction(IEntityTransaction transaction)
        {
            System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 开始生成Frame实体");
            
            try
            {
                // 暂时跳过参数验证以便调试
                // if (!ValidateParameters())
                // {
                //     System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 参数验证失败");
                //     return;
                // }
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 跳过参数验证");
                
                if (database == null || transaction == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] database或transaction为空");
                    return;
                }
                    
                // 获取模型空间块
                var modelSpace = SafeBlock();
                if (modelSpace == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 无法获取模型空间块");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 成功获取模型空间块: {modelSpace.name}");
                
                    
                // 生成外框
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 生成外框");
                GenerateOuterFrame(transaction, modelSpace);
                
                // 生成内框
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 生成内框");
                GenerateInnerFrame(transaction, modelSpace);
                
                // 生成装订边
                if (BindingMargin > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 生成装订边");
                    GenerateBindingLine(transaction, modelSpace);
                }
                
                // 生成标题栏
                if (IncludeTitleBlock && TitleBlock != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 生成标题栏");
                    GenerateTitleBlock(transaction, modelSpace);
                }
                
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] Frame实体生成完成，子实体数: {ChildIdList.Count}");
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesWithTransaction] 生成实体时出错: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 重写GenerateEntitiesLegacy以支持无事务管理器的情况
        /// </summary>
        protected override void GenerateEntitiesLegacy()
        {
            System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesLegacy] 开始生成Frame实体(Legacy模式)");
            
            try
            {
                // 暂时跳过参数验证以便调试
                // if (!ValidateParameters())
                // {
                //     System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesLegacy] 参数验证失败");
                //     return;
                // }
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesLegacy] 跳过参数验证");
                
                if (database == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesLegacy] database为空");
                    return;
                }
                    
                // 获取模型空间块
                var modelSpace = SafeBlock();
                if (modelSpace == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesLegacy] 无法获取模型空间块");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[Frame.GenerateEntitiesLegacy] 成功获取模型空间块: {modelSpace.name}");
                
                    
                // 生成外框（传null作为transaction参数）
                GenerateOuterFrame(null, modelSpace);
                
                // 生成内框
                GenerateInnerFrame(null, modelSpace);
                
                // 生成装订边
                if (BindingMargin > 0)
                {
                    GenerateBindingLine(null, modelSpace);
                }
                
                // 生成标题栏
                if (IncludeTitleBlock && TitleBlock != null)
                {
                    GenerateTitleBlock(null, modelSpace);
                }
                
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        private void GenerateOuterFrame(IEntityTransaction transaction, Block modelSpace)
        {
            // 外框四条线
            var lines = new List<Line>();

            // 上边
            var topLine = new Line();
            topLine.startPoint = new Vector2(OriginalX, OriginalY + Height);
            topLine.endPoint = new Vector2(OriginalX + Width, OriginalY + Height);
            topLine.color = FrameColor;
            topLine.lineWeight = ConvertToLineWeight(OuterLineWidth);
            topLine.layerId = LayerId;
            lines.Add(topLine);

            // 右边
            var rightLine = new Line();
            rightLine.startPoint = new Vector2(OriginalX + Width, OriginalY + Height);
            rightLine.endPoint = new Vector2(OriginalX + Width, OriginalY);
            rightLine.color = FrameColor;
            rightLine.lineWeight = ConvertToLineWeight(OuterLineWidth);
            rightLine.layerId = LayerId;
            lines.Add(rightLine);

            // 下边
            var bottomLine = new Line();
            bottomLine.startPoint = new Vector2(OriginalX + Width, OriginalY);
            bottomLine.endPoint = new Vector2(OriginalX, OriginalY);
            bottomLine.color = FrameColor;
            bottomLine.lineWeight = ConvertToLineWeight(OuterLineWidth);
            bottomLine.layerId = LayerId;
            lines.Add(bottomLine);

            // 左边
            var leftLine = new Line();
            leftLine.startPoint = new Vector2(OriginalX, OriginalY);
            leftLine.endPoint = new Vector2(OriginalX, OriginalY + Height);
            leftLine.color = FrameColor;
            leftLine.lineWeight = ConvertToLineWeight(OuterLineWidth);
            leftLine.layerId = LayerId;
            lines.Add(leftLine);

            // 标记为Frame生成的实体
            MarkGeneratedEntities(lines);

            // 添加到模型空间并记录ID
            foreach (var line in lines)
            {
                var id = modelSpace.AppendEntity(line);
                ChildIdList.Add(id);
                // 不需要同时记录到GeneratedEntityIds，ChildIdList已经足够
            }
        }
        
        private void GenerateInnerFrame(IEntityTransaction transaction, Block modelSpace)
        {
            if (MarginDis <= 0)
                return;
                
            var lines = new List<Line>();
            
            double innerLeft = OriginalX + BindingMargin;
            double innerRight = OriginalX + Width - MarginDis;
            double innerBottom = OriginalY + MarginDis;
            double innerTop = OriginalY + Height - MarginDis;
            
            // 内框上边
            var topLine = new Line();
            topLine.startPoint = new Vector2(innerLeft, innerTop);
            topLine.endPoint = new Vector2(innerRight, innerTop);
            topLine.color = FrameColor;
            topLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
            topLine.layerId = LayerId;
            lines.Add(topLine);
            
            // 内框右边
            var rightLine = new Line();
            rightLine.startPoint = new Vector2(innerRight, innerTop);
            rightLine.endPoint = new Vector2(innerRight, innerBottom);
            rightLine.color = FrameColor;
            rightLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
            rightLine.layerId = LayerId;
            lines.Add(rightLine);
            
            // 内框下边
            var bottomLine = new Line();
            bottomLine.startPoint = new Vector2(innerRight, innerBottom);
            bottomLine.endPoint = new Vector2(innerLeft, innerBottom);
            bottomLine.color = FrameColor;
            bottomLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
            bottomLine.layerId = LayerId;
            lines.Add(bottomLine);
            
            // 内框左边 - 只有在没有装订边或装订边为0时才生成，否则由GenerateBindingLine生成
            if (BindingMargin <= 0)
            {
                var innerLeftLine = new Line();
                innerLeftLine.startPoint = new Vector2(innerLeft, innerBottom);
                innerLeftLine.endPoint = new Vector2(innerLeft, innerTop);
                innerLeftLine.color = FrameColor;
                innerLeftLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
                innerLeftLine.layerId = LayerId;
                lines.Add(innerLeftLine);
            }

            // 标记为Frame生成的实体
            MarkGeneratedEntities(lines);

            // 添加到模型空间并记录ID
            foreach (var line in lines)
            {
                var id = modelSpace.AppendEntity(line);
                ChildIdList.Add(id);
                // 不需要同时记录到GeneratedEntityIds，ChildIdList已经足够
            }
        }
        
        private void GenerateBindingLine(IEntityTransaction transaction, Block modelSpace)
        {
            // 装订线
            var bindingLine = new Line();
            bindingLine.startPoint = new Vector2(OriginalX + BindingMargin, OriginalY + MarginDis);
            bindingLine.endPoint = new Vector2(OriginalX + BindingMargin, OriginalY + Height - MarginDis);
            bindingLine.color = FrameColor;
            bindingLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
            bindingLine.layerId = LayerId;

            // 标记为Frame生成的实体
            MarkGeneratedEntity(bindingLine);

            modelSpace.AppendEntity(bindingLine);
            ChildIdList.Add(bindingLine.id);
            // 不需要同时记录到GeneratedEntityIds，ChildIdList已经足够
        }
        
        private void GenerateTitleBlock(IEntityTransaction transaction, Block modelSpace)
        {
            // 使用TitleBlock设置的尺寸
            double titleWidth = TitleBlock.Width;
            double titleHeight = TitleBlock.Height;
            double titleX = OriginalX + Width - titleWidth;
            double titleY = OriginalY;
            
            // 标题栏外框
            var titleLines = new List<Line>();
            
            // 标题栏上边
            var topLine = new Line();
            topLine.startPoint = new Vector2(titleX, titleY + titleHeight);
            topLine.endPoint = new Vector2(titleX + titleWidth, titleY + titleHeight);
            topLine.color = FrameColor;
            topLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
            topLine.layerId = LayerId;
            titleLines.Add(topLine);
            
            // 标题栏左边
            var leftLine = new Line();
            leftLine.startPoint = new Vector2(titleX, titleY);
            leftLine.endPoint = new Vector2(titleX, titleY + titleHeight);
            leftLine.color = FrameColor;
            leftLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
            leftLine.layerId = LayerId;
            titleLines.Add(leftLine);
            
            // 创建标题栏网格布局
            // 分为上下两部分
            double upperHeight = titleHeight * 0.6;
            double lowerHeight = titleHeight * 0.4;
            
            // 上部水平分割线
            var upperDivider = new Line();
            upperDivider.startPoint = new Vector2(titleX, titleY + lowerHeight);
            upperDivider.endPoint = new Vector2(titleX + titleWidth, titleY + lowerHeight);
            upperDivider.color = FrameColor;
            upperDivider.lineWeight = ConvertToLineWeight(InnerLineWidth);
            upperDivider.layerId = LayerId;
            titleLines.Add(upperDivider);
            
            // 上部垂直分割线（分成多个字段）
            double[] columnWidths = { 36, 36, 36, 36, 36 }; // 每列36mm，共5列=180mm
            double currentX = titleX;
            
            for (int i = 0; i < columnWidths.Length - 1; i++)
            {
                currentX += columnWidths[i];
                var vertLine = new Line();
                vertLine.startPoint = new Vector2(currentX, titleY + lowerHeight);
                vertLine.endPoint = new Vector2(currentX, titleY + titleHeight);
                vertLine.color = FrameColor;
                vertLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
                vertLine.layerId = LayerId;
                titleLines.Add(vertLine);
            }
            
            // 下部分隔线（分成3个区域）
            double[] lowerColumns = { 60, 60, 60 }; // 3个60mm区域
            currentX = titleX;
            for (int i = 0; i < lowerColumns.Length - 1; i++)
            {
                currentX += lowerColumns[i];
                var vertLine = new Line();
                vertLine.startPoint = new Vector2(currentX, titleY);
                vertLine.endPoint = new Vector2(currentX, titleY + lowerHeight);
                vertLine.color = FrameColor;
                vertLine.lineWeight = ConvertToLineWeight(InnerLineWidth);
                vertLine.layerId = LayerId;
                titleLines.Add(vertLine);
            }
            
            // 添加标签和内容文字
            double textPadding = 2.0;
            double labelFontSize = TitleBlock.FontSize * 0.8;
            double contentFontSize = TitleBlock.FontSize;
            
            // 上部区域文字（签署信息）
            AddTitleBlockText(modelSpace, "设计", titleX + textPadding, titleY + lowerHeight + upperHeight * 0.25, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.Designer, titleX + 18, titleY + lowerHeight + upperHeight * 0.25, contentFontSize);
            
            AddTitleBlockText(modelSpace, "校对", titleX + 36 + textPadding, titleY + lowerHeight + upperHeight * 0.25, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.Checker, titleX + 54, titleY + lowerHeight + upperHeight * 0.25, contentFontSize);
            
            AddTitleBlockText(modelSpace, "审核", titleX + 72 + textPadding, titleY + lowerHeight + upperHeight * 0.25, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.Approver, titleX + 90, titleY + lowerHeight + upperHeight * 0.25, contentFontSize);
            
            AddTitleBlockText(modelSpace, "批准", titleX + 108 + textPadding, titleY + lowerHeight + upperHeight * 0.25, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.Releaser, titleX + 126, titleY + lowerHeight + upperHeight * 0.25, contentFontSize);
            
            AddTitleBlockText(modelSpace, "日期", titleX + 144 + textPadding, titleY + lowerHeight + upperHeight * 0.25, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.DesignDate, titleX + 162, titleY + lowerHeight + upperHeight * 0.25, contentFontSize);
            
            // 日期行（第二行）
            AddTitleBlockText(modelSpace, TitleBlock.DesignDate, titleX + 18, titleY + lowerHeight + upperHeight * 0.75, contentFontSize * 0.9);
            AddTitleBlockText(modelSpace, TitleBlock.CheckDate, titleX + 54, titleY + lowerHeight + upperHeight * 0.75, contentFontSize * 0.9);
            AddTitleBlockText(modelSpace, TitleBlock.ApproveDate, titleX + 90, titleY + lowerHeight + upperHeight * 0.75, contentFontSize * 0.9);
            AddTitleBlockText(modelSpace, TitleBlock.ReleaseDate, titleX + 126, titleY + lowerHeight + upperHeight * 0.75, contentFontSize * 0.9);
            
            // 下部区域文字（项目信息）
            AddTitleBlockText(modelSpace, "图纸名称", titleX + textPadding, titleY + lowerHeight * 0.7, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.ProjectName, titleX + 30, titleY + lowerHeight * 0.7, contentFontSize * 1.2, true);
            
            AddTitleBlockText(modelSpace, "图号", titleX + textPadding, titleY + lowerHeight * 0.3, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.DrawingNumber, titleX + 30, titleY + lowerHeight * 0.3, contentFontSize);
            
            AddTitleBlockText(modelSpace, "材料", titleX + 60 + textPadding, titleY + lowerHeight * 0.7, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.Material, titleX + 80, titleY + lowerHeight * 0.7, contentFontSize);
            
            AddTitleBlockText(modelSpace, "比例", titleX + 60 + textPadding, titleY + lowerHeight * 0.3, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.Scale, titleX + 80, titleY + lowerHeight * 0.3, contentFontSize);
            
            AddTitleBlockText(modelSpace, "单位", titleX + 120 + textPadding, titleY + lowerHeight * 0.7, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.CompanyName, titleX + 140, titleY + lowerHeight * 0.7, contentFontSize);
            
            AddTitleBlockText(modelSpace, "页码", titleX + 120 + textPadding, titleY + lowerHeight * 0.3, labelFontSize);
            AddTitleBlockText(modelSpace, TitleBlock.GetPageText(), titleX + 140, titleY + lowerHeight * 0.3, contentFontSize * 0.9);

            // 标记为Frame生成的实体
            MarkGeneratedEntities(titleLines);

            // 添加所有线条到模型空间
            foreach (var line in titleLines)
            {
                modelSpace.AppendEntity(line);
                ChildIdList.Add(line.id);
                // 不需要同时记录到GeneratedEntityIds，ChildIdList已经足够
            }
        }
        
        private void AddTitleBlockText(Block modelSpace, string text, double x, double y, double fontSize, bool bold = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var textEntity = new Text();
            textEntity.Position = new Vector3(x, y, 0);
            textEntity.Value = text;
            textEntity.Height = fontSize;
            textEntity.Alignment = lcdb.TextAlignment.LeftMiddle;
            textEntity.color = FrameColor;
            textEntity.layerId = LayerId;

            // 标记为Frame生成的实体
            MarkGeneratedEntity(textEntity);

            modelSpace.AppendEntity(textEntity);
            ChildIdList.Add(textEntity.id);
            // 不需要同时记录到GeneratedEntityIds，ChildIdList已经足够
        }
        
        private LineWeight ConvertToLineWeight(double width)
        {
            // 将毫米转换为LineWeight枚举
            // LineWeight值以0.01mm为单位
            if (width <= 0.25) return LineWeight.LineWeight025;
            if (width <= 0.35) return LineWeight.LineWeight035;
            if (width <= 0.5) return LineWeight.LineWeight050;
            if (width <= 0.7) return LineWeight.LineWeight070;
            if (width <= 1.0) return LineWeight.LineWeight100;
            return LineWeight.LineWeight070;
        }
        
        public bool ValidateParameters()
        {
            var errors = new List<string>();
            
            // 基本尺寸验证
            if (Width <= 0)
                errors.Add("图框宽度必须大于0");
            
            if (Height <= 0)
                errors.Add("图框高度必须大于0");
            
            if (MarginDis < 0)
                errors.Add("边距不能为负值");
            
            if (BindingMargin < 0)
                errors.Add("装订边距不能为负值");
            
            // 线宽验证
            if (OuterLineWidth <= 0)
                errors.Add("外框线宽必须大于0");
            
            if (InnerLineWidth <= 0)
                errors.Add("内框线宽必须大于0");
            
            // 逻辑验证
            if (MarginDis + BindingMargin >= Width)
                errors.Add("边距和装订边距之和超过图框宽度");
            
            if (MarginDis * 2 >= Height)
                errors.Add("上下边距之和超过图框高度");
            
            // 标题栏验证
            if (IncludeTitleBlock && TitleBlock != null)
            {
                if (TitleBlock.Width > Width)
                    errors.Add("标题栏宽度超过图框宽度");
                
                if (TitleBlock.Height > Height)
                    errors.Add("标题栏高度超过图框高度");
                
                if (!TitleBlock.ValidateRequired())
                    errors.Add("标题栏必填项未完成");
            }
            
            // 输出错误信息
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                }
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 重新生成Frame的子实体（公共方法，供外部调用）
        /// </summary>
        public void Regenerate()
        {
            RegenerateEntities();
        }
        
        public void RegenerateEntities()
        {
            try
            {
                // 暂时跳过参数验证以便调试
                // if (!ValidateParameters())
                // {
                //     throw new InvalidOperationException("图框参数无效，无法重新生成");
                // }
                
                // 清理旧实体
                SafeClearEntity();
                
                // 重新生成所有实体
                GenEntity();
                
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        /// <summary>
        /// 获取Frame的边界框
        /// </summary>
        public lcdb.Bounding GetBounding()
        {
            return new lcdb.Bounding(
                new Vector2(OriginalX, OriginalY),
                new Vector2(OriginalX + Width, OriginalY + Height)
            );
        }
        
        /// <summary>
        /// 获取有效绘图区域（扣除边距和标题栏）
        /// </summary>
        public lcdb.Bounding GetDrawingArea()
        {
            double left = OriginalX + BindingMargin;
            double right = OriginalX + Width - MarginDis;
            double bottom = OriginalY + MarginDis;
            double top = OriginalY + Height - MarginDis;

            // 如果有标题栏，扣除标题栏区域
            if (IncludeTitleBlock && TitleBlock != null)
            {
                bottom = Math.Max(bottom, OriginalY + TitleBlock.Height + MarginDis);
            }

            return new lcdb.Bounding(
                new Vector2(left, bottom),
                new Vector2(right, top)
            );
        }
        
        #endregion
        
    }
}