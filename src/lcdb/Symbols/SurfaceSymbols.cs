using System;
using System.Collections.Generic;
using LitMath;
using lcdb;
using lcdb.Colors;
using lcdb.Annotation;

namespace lcdb.Symbols
{
    /// <summary>
    /// 表面符号库
    /// 提供光学表面质量、纹理和特性符号
    /// </summary>
    public static class SurfaceSymbols
    {
        #region 表面质量符号

        /// <summary>
        /// 创建表面粗糙度符号（基础方法）
        /// </summary>
        public static List<Entity> CreateRoughnessSymbol(Vector2 position, double ra, RoughnessStandard standard = RoughnessStandard.ISO)
        {
            var entities = new List<Entity>();

            // 基础三角形符号
            var triangle = CreateRoughnessTriangle(position, 5.0);
            entities.AddRange(triangle);

            // Ra值文本
            var raText = new Text();
            raText.Value = $"Ra {ra}";
            raText.Position = new Vector3(position.X + 7, position.Y, 0);
            raText.Height = 2.5;
            raText.alignment = TextAlignment.LeftMiddle;
            raText.color = Color.ByLayer;
            entities.Add(raText);

            // 标准标记
            if (standard == RoughnessStandard.ISO)
            {
                var standardText = new Text();
                standardText.Value = "ISO";
                standardText.Position = new Vector3(position.X + 7, position.Y - 3, 0);
                standardText.Height = 2.0;
                standardText.color = Color.ByLayer;
                entities.Add(standardText);
            }

            return entities;
        }

        /// <summary>
        /// 创建完整的表面粗糙度符号（支持所有ISO 1302标记类型）
        /// </summary>
        public static List<Entity> CreateCompleteRoughnessSymbol(
            Vector2 position, 
            RoughnessSymbolType symbolType,
            RoughnessParameters parameters,
            double symbolSize = 5.0)
        {
            var entities = new List<Entity>();

            // 添加空参数检查 - 如果参数为空，返回空列表
            if (parameters == null)
            {
                return entities;
            }

            // 1. 绘制基础符号
            switch (symbolType)
            {
                case RoughnessSymbolType.Basic:
                    // 基本符号（60°V形）
                    entities.AddRange(CreateBasicRoughnessSymbol(position, symbolSize));
                    break;

                case RoughnessSymbolType.MaterialRemovalRequired:
                    // 需要去除材料的符号（带横线的V形）
                    entities.AddRange(CreateMaterialRemovalSymbol(position, symbolSize));
                    break;

                case RoughnessSymbolType.MaterialRemovalProhibited:
                    // 禁止去除材料的符号（带圆圈的V形）
                    entities.AddRange(CreateMaterialRemovalProhibitedSymbol(position, symbolSize));
                    break;

                case RoughnessSymbolType.AllAroundProfile:
                    // 全周轮廓符号（带圆圈的V形，圆圈内有点）
                    entities.AddRange(CreateAllAroundProfileSymbol(position, symbolSize));
                    break;

                case RoughnessSymbolType.MachiningRequired:
                    // 需要加工的符号（带封闭横线的V形）
                    entities.AddRange(CreateMachiningRequiredSymbol(position, symbolSize));
                    break;
            }

            // 2. 添加粗糙度参数文本
            double textHeight = symbolSize * 0.4;
            double xOffset = symbolSize * 1.4;
            // yOffset变量暂时保留，可能用于未来的文本布局调整

            // a - 粗糙度值（上限）
            if (parameters.RoughnessValueUpper.HasValue)
            {
                var upperText = new Text();
                upperText.Value = FormatRoughnessValue(parameters.RoughnessValueUpper.Value, parameters.Parameter);
                upperText.Position = new Vector3(position.X + xOffset, position.Y + symbolSize * 0.3, 0);
                upperText.Height = textHeight;
                upperText.alignment = TextAlignment.LeftMiddle;
                upperText.color = Color.ByLayer;
                entities.Add(upperText);
            }

            // a - 粗糙度值（下限）
            if (parameters.RoughnessValueLower.HasValue)
            {
                var lowerText = new Text();
                lowerText.Value = FormatRoughnessValue(parameters.RoughnessValueLower.Value, parameters.Parameter);
                lowerText.Position = new Vector3(position.X + xOffset, position.Y - symbolSize * 0.3, 0);
                lowerText.Height = textHeight;
                lowerText.alignment = TextAlignment.LeftMiddle;
                lowerText.color = Color.ByLayer;
                entities.Add(lowerText);
            }

            // b - 加工方法
            if (!string.IsNullOrEmpty(parameters.MachiningMethod))
            {
                var methodText = new Text();
                methodText.Value = parameters.MachiningMethod;
                methodText.Position = new Vector3(position.X - symbolSize * 0.8, position.Y + symbolSize * 0.8, 0);
                methodText.Height = textHeight * 0.8;
                methodText.alignment = TextAlignment.CenterMiddle;
                methodText.color = Color.ByLayer;
                entities.Add(methodText);
            }

            // c - 取样长度
            if (parameters.SamplingLength.HasValue)
            {
                var samplingText = new Text();
                samplingText.Value = parameters.SamplingLength.Value.ToString();
                samplingText.Position = new Vector3(position.X - symbolSize * 0.3, position.Y - symbolSize * 0.5, 0);
                samplingText.Height = textHeight * 0.8;
                samplingText.alignment = TextAlignment.CenterMiddle;
                samplingText.color = Color.ByLayer;
                entities.Add(samplingText);
            }

            // d - 加工纹理方向
            if (parameters.LayDirection != LayDirection.None)
            {
                var laySymbol = CreateLayDirectionSymbol(
                    position + new Vector2(0, -symbolSize * 0.5), 
                    parameters.LayDirection, 
                    symbolSize * 0.3);
                entities.AddRange(laySymbol);
            }

            // e - 加工余量
            if (parameters.MachiningAllowance.HasValue)
            {
                var allowanceText = new Text();
                allowanceText.Value = parameters.MachiningAllowance.Value.ToString();
                allowanceText.Position = new Vector3(position.X - symbolSize * 1.2, position.Y, 0);
                allowanceText.Height = textHeight * 0.8;
                allowanceText.alignment = TextAlignment.RightMiddle;
                allowanceText.color = Color.ByLayer;
                entities.Add(allowanceText);
            }

            // f - 其他粗糙度参数值（如Rz、Rmax等）
            if (parameters.OtherRoughnessValues != null && parameters.OtherRoughnessValues.Count > 0)
            {
                double yPos = position.Y + symbolSize * 0.6;
                foreach (var otherValue in parameters.OtherRoughnessValues)
                {
                    var otherText = new Text();
                    otherText.Value = $"{otherValue.Key} {otherValue.Value}";
                    otherText.Position = new Vector3(position.X + xOffset, yPos, 0);
                    otherText.Height = textHeight * 0.8;
                    otherText.alignment = TextAlignment.LeftMiddle;
                    otherText.color = Color.ByLayer;
                    entities.Add(otherText);
                    yPos += textHeight;
                }
            }

            return entities;
        }

        /// <summary>
        /// 创建基本粗糙度符号（60°V形）
        /// </summary>
        private static List<Entity> CreateBasicRoughnessSymbol(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建V形符号的两条线段而不是polyline，以满足测试的需求
            // 左边线：从左下到顶部
            var leftLine = new Line(
                position + new Vector2(-size * 0.5, -size * 0.5),
                position + new Vector2(0, size * 0.5))
            {
                lineWeight = LineWeight.LineWeight035,
                color = Color.ByLayer
            };
            entities.Add(leftLine);

            // 右边线：从顶部到右下
            var rightLine = new Line(
                position + new Vector2(0, size * 0.5),
                position + new Vector2(size * 0.5, -size * 0.5))
            {
                lineWeight = LineWeight.LineWeight035,
                color = Color.ByLayer
            };
            entities.Add(rightLine);

            return entities;
        }

        /// <summary>
        /// 创建需要去除材料的粗糙度符号（带横线的V形）
        /// </summary>
        private static List<Entity> CreateMaterialRemovalSymbol(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // V形符号
            entities.AddRange(CreateBasicRoughnessSymbol(position, size));

            // 添加横线
            var line = new Line(
                position + new Vector2(-size * 0.7, size * 0.5),
                position + new Vector2(size * 0.2, size * 0.5)
            );
            line.lineWeight = LineWeight.LineWeight035;
            line.color = Color.ByLayer;
            entities.Add(line);

            return entities;
        }

        /// <summary>
        /// 创建禁止去除材料的粗糙度符号（带圆圈的V形）
        /// </summary>
        private static List<Entity> CreateMaterialRemovalProhibitedSymbol(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // V形符号
            entities.AddRange(CreateBasicRoughnessSymbol(position, size));

            // 添加圆圈
            var circle = new Circle(position + new Vector2(0, size * 0.5), size * 0.15);
            circle.lineWeight = LineWeight.LineWeight035;
            circle.color = Color.ByLayer;
            entities.Add(circle);

            return entities;
        }

        /// <summary>
        /// 创建全周轮廓粗糙度符号
        /// </summary>
        private static List<Entity> CreateAllAroundProfileSymbol(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // V形符号
            entities.AddRange(CreateBasicRoughnessSymbol(position, size));

            // 添加带中心点的圆圈（在符号上方，Y值较小）
            var circle = new Circle(position + new Vector2(0, -size * 0.5), size * 0.15);
            circle.lineWeight = LineWeight.LineWeight035;
            circle.color = Color.ByLayer;
            entities.Add(circle);

            // 中心点
            var centerPoint = new Circle(position + new Vector2(0, -size * 0.5), size * 0.02);
            centerPoint.lineWeight = LineWeight.LineWeight050;
            centerPoint.color = Color.ByLayer;
            entities.Add(centerPoint);

            return entities;
        }

        /// <summary>
        /// 创建需要机加工的粗糙度符号（带封闭横线的V形）
        /// </summary>
        private static List<Entity> CreateMachiningRequiredSymbol(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // V形符号
            entities.AddRange(CreateBasicRoughnessSymbol(position, size));

            // 添加封闭横线
            var line1 = new Line(
                position + new Vector2(-size * 0.7, size * 0.5),
                position + new Vector2(size * 0.7, size * 0.5)
            );
            line1.lineWeight = LineWeight.LineWeight035;
            line1.color = Color.ByLayer;
            entities.Add(line1);

            // 封闭短竖线
            var line2 = new Line(
                position + new Vector2(size * 0.7, size * 0.5),
                position + new Vector2(size * 0.7, size * 0.3)
            );
            line2.lineWeight = LineWeight.LineWeight035;
            line2.color = Color.ByLayer;
            entities.Add(line2);

            return entities;
        }

        /// <summary>
        /// 创建加工纹理方向符号
        /// </summary>
        private static List<Entity> CreateLayDirectionSymbol(Vector2 position, LayDirection direction, double size)
        {
            var entities = new List<Entity>();

            switch (direction)
            {
                case LayDirection.Parallel:
                    // 平行符号（=）
                    entities.Add(new Line(position + new Vector2(-size / 2, -size / 4), position + new Vector2(size / 2, -size / 4)));
                    entities.Add(new Line(position + new Vector2(-size / 2, size / 4), position + new Vector2(size / 2, size / 4)));
                    break;

                case LayDirection.Perpendicular:
                    // 垂直符号（⊥）
                    entities.Add(new Line(position + new Vector2(0, -size / 2), position + new Vector2(0, size / 2)));
                    entities.Add(new Line(position + new Vector2(-size / 2, size / 2), position + new Vector2(size / 2, size / 2)));
                    break;

                case LayDirection.Crossed:
                    // 交叉符号（X）
                    entities.Add(new Line(position + new Vector2(-size / 2, -size / 2), position + new Vector2(size / 2, size / 2)));
                    entities.Add(new Line(position + new Vector2(-size / 2, size / 2), position + new Vector2(size / 2, -size / 2)));
                    break;

                case LayDirection.Multidirectional:
                    // 多向符号（M）
                    var mText = new Text();
                    mText.Value = "M";
                    mText.Position = new Vector3(position.X, position.Y, 0);
                    mText.Height = size;
                    mText.alignment = TextAlignment.CenterMiddle;
                    entities.Add(mText);
                    break;

                case LayDirection.Circular:
                    // 圆形符号（C）
                    var cText = new Text();
                    cText.Value = "C";
                    cText.Position = new Vector3(position.X, position.Y, 0);
                    cText.Height = size;
                    cText.alignment = TextAlignment.CenterMiddle;
                    entities.Add(cText);
                    break;

                case LayDirection.Radial:
                    // 径向符号（R）
                    var rText = new Text();
                    rText.Value = "R";
                    rText.Position = new Vector3(position.X, position.Y, 0);
                    rText.Height = size;
                    rText.alignment = TextAlignment.CenterMiddle;
                    entities.Add(rText);
                    break;

                case LayDirection.Particulate:
                    // 颗粒状符号（P）
                    var pText = new Text();
                    pText.Value = "P";
                    pText.Position = new Vector3(position.X, position.Y, 0);
                    pText.Height = size;
                    pText.alignment = TextAlignment.CenterMiddle;
                    entities.Add(pText);
                    break;
            }

            foreach (var entity in entities)
            {
                entity.color = Color.ByLayer;
                if (entity is Line line)
                {
                    line.lineWeight = LineWeight.LineWeight025;
                }
            }

            return entities;
        }

        /// <summary>
        /// 格式化粗糙度值
        /// </summary>
        private static string FormatRoughnessValue(double value, RoughnessParameter parameter)
        {
            string paramStr = parameter switch
            {
                RoughnessParameter.Ra => "Ra",
                RoughnessParameter.Rz => "Rz",
                RoughnessParameter.Rmax => "Rmax",
                RoughnessParameter.Rq => "Rq",
                RoughnessParameter.Rt => "Rt",
                RoughnessParameter.Rp => "Rp",
                RoughnessParameter.Rv => "Rv",
                RoughnessParameter.Rzjis => "Rz(JIS)",
                RoughnessParameter.RSm => "RSm",
                RoughnessParameter.Rmr => "Rmr",
                _ => ""
            };

            return $"{paramStr} {value}";
        }

        /// <summary>
        /// 创建表面纹理符号
        /// </summary>
        public static List<Entity> CreateSurfaceTextureSymbol(Vector2 position, SurfaceTextureType type, string specification = "")
        {
            var entities = new List<Entity>();

            switch (type)
            {
                case SurfaceTextureType.Ground:
                    entities.AddRange(CreateGroundSurfaceSymbol(position));
                    break;
                
                case SurfaceTextureType.Polished:
                    entities.AddRange(CreatePolishedSurfaceSymbol(position));
                    break;
                
                case SurfaceTextureType.Lapped:
                    entities.AddRange(CreateLappedSurfaceSymbol(position));
                    break;
                
                case SurfaceTextureType.Machined:
                    entities.AddRange(CreateMachinedSurfaceSymbol(position));
                    break;
                
                case SurfaceTextureType.Diamond:
                    entities.AddRange(CreateDiamondTurnedSymbol(position));
                    break;
            }

            // 添加规格说明
            if (!string.IsNullOrEmpty(specification))
            {
                var specText = new Text();
                specText.Value = specification;
                specText.Position = new Vector3(position.X + 10, position.Y, 0);
                specText.Height = 2.0;
                specText.color = Color.ByLayer;
                entities.Add(specText);
            }

            return entities;
        }

        /// <summary>
        /// 创建表面缺陷符号（ISO 10110-7）
        /// </summary>
        public static List<Entity> CreateSurfaceDefectSymbol(Vector2 position, int scratchNumber, int digNumber)
        {
            var entities = new List<Entity>();

            // 创建标准符号框
            var frame = new Polyline();
            frame.closed = true;
            frame.AddVertexAt(0, position);
            frame.AddVertexAt(1, position + new Vector2(15, 0));
            frame.AddVertexAt(2, position + new Vector2(15, 8));
            frame.AddVertexAt(3, position + new Vector2(0, 8));
            frame.lineWeight = LineWeight.LineWeight035;
            entities.Add(frame);

            // 添加斜线分隔
            var separator = new Line(
                position + new Vector2(0, 0),
                position + new Vector2(15, 8)
            );
            separator.lineWeight = LineWeight.LineWeight025;
            entities.Add(separator);

            // 添加数值
            var scratchText = new Text();
            scratchText.Value = scratchNumber.ToString();
            scratchText.Position = new Vector3(position.X + 4, position.Y + 5, 0);
            scratchText.Height = 2.5;
            scratchText.alignment = TextAlignment.CenterMiddle;
            entities.Add(scratchText);

            var digText = new Text();
            digText.Value = digNumber.ToString();
            digText.Position = new Vector3(position.X + 11, position.Y + 3, 0);
            digText.Height = 2.5;
            digText.alignment = TextAlignment.CenterMiddle;
            entities.Add(digText);

            return entities;
        }

        /// <summary>
        /// 创建表面形状偏差符号
        /// </summary>
        public static List<Entity> CreateFormDeviationSymbol(Vector2 position, FormDeviationType type, double value, string unit = "λ")
        {
            var entities = new List<Entity>();

            // 基础符号
            switch (type)
            {
                case FormDeviationType.Power:
                    entities.AddRange(CreatePowerSymbol(position));
                    break;
                
                case FormDeviationType.Irregularity:
                    entities.AddRange(CreateIrregularitySymbol(position));
                    break;
                
                case FormDeviationType.Astigmatism:
                    entities.AddRange(CreateAstigmatismSymbol(position));
                    break;
                
                case FormDeviationType.Sphericity:
                    entities.AddRange(CreateSphericitySymbol(position));
                    break;
            }

            // 数值标注
            var valueText = new Text();
            valueText.Value = $"{value}{unit}";
            valueText.Position = new Vector3(position.X + 12, position.Y + 2, 0);
            valueText.Height = 2.5;
            valueText.alignment = TextAlignment.LeftMiddle;
            entities.Add(valueText);

            return entities;
        }

        /// <summary>
        /// 创建表面处理符号
        /// </summary>
        public static List<Entity> CreateSurfaceTreatmentSymbol(Vector2 position, SurfaceTreatment treatment)
        {
            var entities = new List<Entity>();

            // 基础矩形框
            var frame = new Polyline();
            frame.closed = true;
            frame.AddVertexAt(0, position);
            frame.AddVertexAt(1, position + new Vector2(20, 0));
            frame.AddVertexAt(2, position + new Vector2(20, 6));
            frame.AddVertexAt(3, position + new Vector2(0, 6));
            frame.lineWeight = LineWeight.LineWeight025;
            frame.lineType = LineType.Dash;
            entities.Add(frame);

            // 处理类型文本
            var text = new Text();
            text.Value = GetTreatmentText(treatment);
            text.Position = new Vector3(position.X + 10, position.Y + 3, 0);
            text.Height = 2.0;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        #endregion

        #region 镀膜符号

        /// <summary>
        /// 创建镀膜标记符号
        /// </summary>
        public static List<Entity> CreateCoatingSymbol(Vector2 position, CoatingType type, string specification = "")
        {
            var entities = new List<Entity>();

            // 根据镀膜类型创建不同的符号
            switch (type)
            {
                case CoatingType.AR:
                    entities.AddRange(CreateARCoatingSymbol(position));
                    break;
                
                case CoatingType.HR:
                    entities.AddRange(CreateHRCoatingSymbol(position));
                    break;
                
                case CoatingType.BS:
                    entities.AddRange(CreateBeamsplitterSymbol(position));
                    break;
                
                case CoatingType.Filter:
                    entities.AddRange(CreateFilterCoatingSymbol(position));
                    break;
                
                case CoatingType.Protective:
                    entities.AddRange(CreateProtectiveCoatingSymbol(position));
                    break;
            }

            // 添加规格说明
            if (!string.IsNullOrEmpty(specification))
            {
                var specText = new Text();
                specText.Value = specification;
                specText.Position = new Vector3(position.X, position.Y - 5, 0);
                specText.Height = 2.0;
                specText.alignment = TextAlignment.CenterMiddle;
                entities.Add(specText);
            }

            return entities;
        }

        /// <summary>
        /// 创建镀膜区域边界
        /// </summary>
        public static List<Entity> CreateCoatingBoundary(List<Vector2> boundaryPoints, CoatingType type)
        {
            var entities = new List<Entity>();

            // 创建边界线
            var boundary = new Polyline();
            boundary.closed = true;
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                boundary.AddVertexAt(i, boundaryPoints[i]);
            }
            boundary.lineWeight = LineWeight.LineWeight035;
            boundary.lineType = LineType.Dash;
            boundary.color = GetCoatingColor(type);
            entities.Add(boundary);

            // 在边界内添加填充图案
            // 这里简化处理，实际可能需要更复杂的填充
            
            return entities;
        }

        #endregion

        #region 特殊表面符号

        /// <summary>
        /// 创建非球面标记
        /// </summary>
        public static List<Entity> CreateAsphericSurfaceSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建"A"符号
            var circle = new Circle(position, 3.0);
            circle.lineWeight = LineWeight.LineWeight035;
            entities.Add(circle);

            var text = new Text();
            text.Value = "A";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = 3.0;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        /// <summary>
        /// 创建衍射表面符号
        /// </summary>
        public static List<Entity> CreateDiffractiveSurfaceSymbol(Vector2 position, int grooveCount)
        {
            var entities = new List<Entity>();

            // 创建光栅线
            double width = 15.0;
            double height = 10.0;
            int lineCount = 5;

            for (int i = 0; i < lineCount; i++)
            {
                double x = position.X + (i * width / (lineCount - 1));
                var line = new Line(
                    new Vector2(x, position.Y),
                    new Vector2(x, position.Y + height)
                );
                line.lineWeight = LineWeight.LineWeight025;
                entities.Add(line);
            }

            // 添加刻线数标注
            var grooveText = new Text();
            grooveText.Value = $"{grooveCount} l/mm";
            grooveText.Position = new Vector3(position.X + width / 2, position.Y - 3, 0);
            grooveText.Height = 2.0;
            grooveText.alignment = TextAlignment.CenterMiddle;
            entities.Add(grooveText);

            return entities;
        }

        /// <summary>
        /// 创建自由曲面标记
        /// </summary>
        public static List<Entity> CreateFreeformSurfaceSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建波浪线表示自由曲面
            var points = new List<Vector2>();
            int pointCount = 20;
            double width = 20.0;
            double amplitude = 3.0;

            for (int i = 0; i <= pointCount; i++)
            {
                double x = position.X + (i * width / pointCount);
                double y = position.Y + amplitude * Math.Sin(i * Math.PI / 5);
                points.Add(new Vector2(x, y));
            }

            var spline = new Polyline();
            for (int i = 0; i < points.Count; i++)
            {
                spline.AddVertexAt(i, points[i]);
            }
            spline.lineWeight = LineWeight.LineWeight050;
            entities.Add(spline);

            // 添加"FF"标记
            var text = new Text();
            text.Value = "FF";
            text.Position = new Vector3(position.X + width / 2, position.Y + 6, 0);
            text.Height = 2.5;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        #endregion

        #region 私有辅助方法

        private static List<Entity> CreateRoughnessTriangle(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建倒三角形
            var triangle = new Polyline();
            triangle.closed = false;
            triangle.AddVertexAt(0, position + new Vector2(0, size));
            triangle.AddVertexAt(1, position);
            triangle.AddVertexAt(2, position + new Vector2(size, size));
            triangle.lineWeight = LineWeight.LineWeight035;
            entities.Add(triangle);

            return entities;
        }

        private static List<Entity> CreateGroundSurfaceSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建研磨符号（交叉线）
            var size = 5.0;
            var line1 = new Line(
                position - new Vector2(size / 2, size / 2),
                position + new Vector2(size / 2, size / 2)
            );
            var line2 = new Line(
                position - new Vector2(size / 2, -size / 2),
                position + new Vector2(size / 2, -size / 2)
            );
            
            line1.lineWeight = LineWeight.LineWeight025;
            line2.lineWeight = LineWeight.LineWeight025;
            entities.Add(line1);
            entities.Add(line2);

            return entities;
        }

        private static List<Entity> CreatePolishedSurfaceSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建抛光符号（圆圈）
            var circle = new Circle(position, 2.5);
            circle.lineWeight = LineWeight.LineWeight025;
            entities.Add(circle);

            var innerCircle = new Circle(position, 1.0);
            innerCircle.lineWeight = LineWeight.LineWeight025;
            entities.Add(innerCircle);

            return entities;
        }

        private static List<Entity> CreateARCoatingSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // AR镀膜符号 - 绿色圆圈
            var circle = new Circle(position, 3.0);
            circle.lineWeight = LineWeight.LineWeight035;
            circle.color = Color.FromRGB(0, 255, 0);
            entities.Add(circle);

            var text = new Text();
            text.Value = "AR";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = 2.0;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static List<Entity> CreateHRCoatingSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // HR镀膜符号 - 红色方框
            var square = new Polyline();
            square.closed = true;
            square.AddVertexAt(0, position - new Vector2(3, 3));
            square.AddVertexAt(1, position + new Vector2(3, -3));
            square.AddVertexAt(2, position + new Vector2(3, 3));
            square.AddVertexAt(3, position + new Vector2(-3, 3));
            square.lineWeight = LineWeight.LineWeight035;
            square.color = Color.FromRGB(255, 0, 0);
            entities.Add(square);

            var text = new Text();
            text.Value = "HR";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = 2.0;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static List<Entity> CreateBeamsplitterSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // BS分束镜符号 - 黄色三角形
            var triangle = new Polyline();
            triangle.closed = true;
            triangle.AddVertexAt(0, position + new Vector2(0, -3));
            triangle.AddVertexAt(1, position + new Vector2(3, 3));
            triangle.AddVertexAt(2, position + new Vector2(-3, 3));
            triangle.lineWeight = LineWeight.LineWeight035;
            triangle.color = Color.FromRGB(255, 255, 0);
            entities.Add(triangle);

            var text = new Text();
            text.Value = "BS";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = 1.8;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static List<Entity> CreateFilterCoatingSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 滤光片符号 - 紫色菱形
            var diamond = new Polyline();
            diamond.closed = true;
            diamond.AddVertexAt(0, position + new Vector2(0, -3));
            diamond.AddVertexAt(1, position + new Vector2(3, 0));
            diamond.AddVertexAt(2, position + new Vector2(0, 3));
            diamond.AddVertexAt(3, position + new Vector2(-3, 0));
            diamond.lineWeight = LineWeight.LineWeight035;
            diamond.color = Color.FromRGB(255, 0, 255);
            entities.Add(diamond);

            var text = new Text();
            text.Value = "F";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = 2.0;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static List<Entity> CreateProtectiveCoatingSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 保护膜符号 - 灰色方框
            var square = new Polyline();
            square.closed = true;
            square.AddVertexAt(0, position - new Vector2(3, 3));
            square.AddVertexAt(1, position + new Vector2(3, -3));
            square.AddVertexAt(2, position + new Vector2(3, 3));
            square.AddVertexAt(3, position + new Vector2(-3, 3));
            square.lineWeight = LineWeight.LineWeight035;
            square.color = Color.FromRGB(128, 128, 128);
            entities.Add(square);

            var text = new Text();
            text.Value = "P";
            text.Position = new Vector3(position.X, position.Y, 0);
            text.Height = 2.0;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static Color GetCoatingColor(CoatingType type)
        {
            switch (type)
            {
                case CoatingType.AR:
                    return Color.FromRGB(0, 255, 0);
                case CoatingType.HR:
                    return Color.FromRGB(255, 0, 0);
                case CoatingType.BS:
                    return Color.FromRGB(255, 255, 0);
                case CoatingType.Filter:
                    return Color.FromRGB(255, 0, 255);
                case CoatingType.Protective:
                    return Color.FromRGB(128, 128, 128);
                default:
                    return Color.ByLayer;
            }
        }

        private static string GetTreatmentText(SurfaceTreatment treatment)
        {
            switch (treatment)
            {
                case SurfaceTreatment.Annealed:
                    return "退火";
                case SurfaceTreatment.Hardened:
                    return "硬化";
                case SurfaceTreatment.Cleaned:
                    return "清洁";
                case SurfaceTreatment.Protected:
                    return "保护";
                case SurfaceTreatment.Etched:
                    return "蚀刻";
                default:
                    return "";
            }
        }

        private static List<Entity> CreatePowerSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建功率偏差符号
            var symbol = new Text();
            symbol.Value = "P";
            symbol.Position = new Vector3(position.X, position.Y, 0);
            symbol.Height = 3.0;
            symbol.alignment = TextAlignment.CenterMiddle;
            entities.Add(symbol);

            return entities;
        }

        private static List<Entity> CreateIrregularitySymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建不规则度符号
            var symbol = new Text();
            symbol.Value = "I";
            symbol.Position = new Vector3(position.X, position.Y, 0);
            symbol.Height = 3.0;
            symbol.alignment = TextAlignment.CenterMiddle;
            entities.Add(symbol);

            return entities;
        }

        private static List<Entity> CreateAstigmatismSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建像散符号
            var symbol = new Text();
            symbol.Value = "A";
            symbol.Position = new Vector3(position.X, position.Y, 0);
            symbol.Height = 3.0;
            symbol.alignment = TextAlignment.CenterMiddle;
            entities.Add(symbol);

            return entities;
        }

        private static List<Entity> CreateSphericitySymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 创建球面度符号
            var circle = new Circle(position, 2.0);
            circle.lineWeight = LineWeight.LineWeight025;
            entities.Add(circle);

            return entities;
        }

        private static List<Entity> CreateLappedSurfaceSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 研磨抛光表面符号 - 使用波浪线
            double width = 20.0;
            double height = 5.0;
            int waveCount = 3;

            var polyline = new Polyline();
            for (int i = 0; i <= waveCount * 4; i++)
            {
                double x = position.X - width / 2 + (i * width / (waveCount * 4));
                double y = position.Y + height * Math.Sin(i * Math.PI / 2) / 2;
                polyline.AddVertexAt(i, new Vector2(x, y));
            }
            polyline.lineWeight = LineWeight.LineWeight035;
            polyline.color = Color.ByLayer;
            entities.Add(polyline);

            // 添加标记
            var text = new Text();
            text.Value = "LAP";
            text.Position = new Vector3(position.X, position.Y - 8, 0);
            text.Height = 2.5;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static List<Entity> CreateMachinedSurfaceSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 机加工表面符号 - 使用平行斜线
            double width = 15.0;
            double height = 10.0;
            int lineCount = 5;

            for (int i = 0; i < lineCount; i++)
            {
                double offset = i * width / lineCount;
                var line = new Line(
                    new Vector2(position.X - width / 2 + offset, position.Y - height / 2),
                    new Vector2(position.X - width / 2 + offset + 5, position.Y + height / 2)
                );
                line.lineWeight = LineWeight.LineWeight025;
                line.color = Color.ByLayer;
                entities.Add(line);
            }

            // 外框
            var frame = new Polyline();
            frame.closed = true;
            frame.AddVertexAt(0, position + new Vector2(-width / 2, -height / 2));
            frame.AddVertexAt(1, position + new Vector2(width / 2, -height / 2));
            frame.AddVertexAt(2, position + new Vector2(width / 2, height / 2));
            frame.AddVertexAt(3, position + new Vector2(-width / 2, height / 2));
            frame.lineWeight = LineWeight.LineWeight035;
            frame.color = Color.ByLayer;
            entities.Add(frame);

            return entities;
        }

        private static List<Entity> CreateDiamondTurnedSymbol(Vector2 position)
        {
            var entities = new List<Entity>();

            // 金刚石车削表面符号 - 使用同心圆弧
            double radius = 8.0;
            int arcCount = 3;

            for (int i = 0; i < arcCount; i++)
            {
                var arc = new Arc(
                    position,
                    radius - i * 2,
                    -30,
                    30
                );
                arc.lineWeight = LineWeight.LineWeight025;
                arc.color = Color.ByLayer;
                entities.Add(arc);
            }

            // 添加中心标记
            var centerMark = new Circle(position, 1.0);
            centerMark.lineWeight = LineWeight.LineWeight050;
            centerMark.color = Color.ByLayer;
            entities.Add(centerMark);

            // 添加文字标记
            var text = new Text();
            text.Value = "DT";
            text.Position = new Vector3(position.X, position.Y - 12, 0);
            text.Height = 2.5;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 粗糙度标准
    /// </summary>
    public enum RoughnessStandard
    {
        ISO,
        DIN,
        JIS,
        GB
    }

    /// <summary>
    /// 粗糙度符号类型（ISO 1302）
    /// </summary>
    public enum RoughnessSymbolType
    {
        Basic,                      // 基本符号（60°V形）
        MaterialRemovalRequired,    // 需要去除材料（带横线的V形）
        MaterialRemovalProhibited,  // 禁止去除材料（带圆圈的V形）
        AllAroundProfile,          // 全周轮廓（带中心点圆圈的V形）
        MachiningRequired          // 需要机加工（带封闭横线的V形）
    }

    /// <summary>
    /// 粗糙度参数类型
    /// </summary>
    public enum RoughnessParameter
    {
        Ra,     // 算术平均粗糙度
        Rz,     // 十点平均粗糙度
        Rmax,   // 最大高度
        Rq,     // 均方根粗糙度
        Rt,     // 总高度
        Rp,     // 最大峰高
        Rv,     // 最大谷深
        Rzjis,  // JIS十点平均粗糙度
        RSm,    // 平均间距
        Rmr     // 材料比率
    }

    /// <summary>
    /// 加工纹理方向（ISO 1302）
    /// </summary>
    public enum LayDirection
    {
        None,              // 无特定方向
        Parallel,          // 平行（=）
        Perpendicular,     // 垂直（⊥）
        Crossed,           // 交叉（X）
        Multidirectional,  // 多向（M）
        Circular,          // 圆形（C）
        Radial,            // 径向（R）
        Particulate        // 颗粒状（P）
    }

    /// <summary>
    /// 表面纹理类型
    /// </summary>
    public enum SurfaceTextureType
    {
        Ground,         // 研磨
        Polished,       // 抛光
        Lapped,         // 研磨抛光
        Machined,       // 机加工
        Diamond,        // 金刚石车削
        Molded,         // 模压
        Etched          // 蚀刻
    }

    /// <summary>
    /// 形状偏差类型
    /// </summary>
    public enum FormDeviationType
    {
        Power,          // 功率偏差
        Irregularity,   // 不规则度
        Astigmatism,    // 像散
        Sphericity,     // 球面度
        Cylindricity,   // 圆柱度
        Flatness        // 平面度
    }

    /// <summary>
    /// 表面处理类型
    /// </summary>
    public enum SurfaceTreatment
    {
        None,           // 无处理
        Annealed,       // 退火
        Hardened,       // 硬化
        Cleaned,        // 清洁
        Protected,      // 保护
        Etched,         // 蚀刻
        Coated          // 镀膜
    }

    #endregion

    #region 数据类

    /// <summary>
    /// 粗糙度参数集合
    /// </summary>
    public class RoughnessParameters
    {
        /// <summary>
        /// 粗糙度参数类型
        /// </summary>
        public RoughnessParameter Parameter { get; set; } = RoughnessParameter.Ra;

        /// <summary>
        /// 粗糙度值上限
        /// </summary>
        public double? RoughnessValueUpper { get; set; }

        /// <summary>
        /// 粗糙度值下限
        /// </summary>
        public double? RoughnessValueLower { get; set; }

        /// <summary>
        /// 加工方法
        /// </summary>
        public string MachiningMethod { get; set; }

        /// <summary>
        /// 取样长度
        /// </summary>
        public double? SamplingLength { get; set; }

        /// <summary>
        /// 加工纹理方向
        /// </summary>
        public LayDirection LayDirection { get; set; } = LayDirection.None;

        /// <summary>
        /// 加工余量
        /// </summary>
        public double? MachiningAllowance { get; set; }

        /// <summary>
        /// 其他粗糙度参数值（如Rz、Rmax等）
        /// </summary>
        public Dictionary<string, double> OtherRoughnessValues { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RoughnessParameters()
        {
            OtherRoughnessValues = new Dictionary<string, double>();
        }

        /// <summary>
        /// 快速创建Ra参数
        /// </summary>
        public static RoughnessParameters CreateRa(double value)
        {
            return new RoughnessParameters
            {
                Parameter = RoughnessParameter.Ra,
                RoughnessValueUpper = value
            };
        }

        /// <summary>
        /// 快速创建带上下限的参数
        /// </summary>
        public static RoughnessParameters CreateWithRange(RoughnessParameter param, double upper, double lower)
        {
            return new RoughnessParameters
            {
                Parameter = param,
                RoughnessValueUpper = upper,
                RoughnessValueLower = lower
            };
        }
    }

    #endregion
}