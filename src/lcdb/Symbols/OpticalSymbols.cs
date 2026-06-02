using System;
using System.Collections.Generic;
using LitMath;
using lcdb;
using lcdb.Colors;

namespace lcdb.Symbols
{
    /// <summary>
    /// 光学符号库
    /// 提供标准光学符号的创建和管理
    /// </summary>
    public static class OpticalSymbols
    {
        #region 符号创建方法

        /// <summary>
        /// 创建透镜符号
        /// </summary>
        public static List<Entity> CreateLensSymbol(LensType type, Vector2 position, double diameter, double thickness)
        {
            var entities = new List<Entity>();

            switch (type)
            {
                case LensType.BiConvex:
                    entities.AddRange(CreateBiConvexLens(position, diameter, thickness));
                    break;
                
                case LensType.BiConcave:
                    entities.AddRange(CreateBiConcaveLens(position, diameter, thickness));
                    break;
                
                case LensType.PlanoConvex:
                    entities.AddRange(CreatePlanoConvexLens(position, diameter, thickness));
                    break;
                
                case LensType.PlanoConcave:
                    entities.AddRange(CreatePlanoConcaveLens(position, diameter, thickness));
                    break;
                
                case LensType.Meniscus:
                    entities.AddRange(CreateMeniscusLens(position, diameter, thickness));
                    break;
                
                default:
                    entities.AddRange(CreateGenericLens(position, diameter, thickness));
                    break;
            }

            return entities;
        }

        /// <summary>
        /// 创建反射镜符号
        /// </summary>
        public static List<Entity> CreateMirrorSymbol(MirrorType type, Vector2 position, double size, double angle = 0)
        {
            var entities = new List<Entity>();

            switch (type)
            {
                case MirrorType.Flat:
                    entities.AddRange(CreateFlatMirror(position, size, angle));
                    break;
                
                case MirrorType.Concave:
                    entities.AddRange(CreateConcaveMirror(position, size, angle));
                    break;
                
                case MirrorType.Convex:
                    entities.AddRange(CreateConvexMirror(position, size, angle));
                    break;
                
                case MirrorType.Dichroic:
                    entities.AddRange(CreateDichroicMirror(position, size, angle));
                    break;
            }

            return entities;
        }

        /// <summary>
        /// 创建棱镜符号
        /// </summary>
        public static List<Entity> CreatePrismSymbol(PrismType type, Vector2 position, double size)
        {
            var entities = new List<Entity>();

            switch (type)
            {
                case PrismType.RightAngle:
                    entities.AddRange(CreateRightAnglePrism(position, size));
                    break;
                
                case PrismType.Penta:
                    entities.AddRange(CreatePentaPrism(position, size));
                    break;
                
                case PrismType.Dove:
                    entities.AddRange(CreateDovePrism(position, size));
                    break;
                
                case PrismType.Wedge:
                    entities.AddRange(CreateWedgePrism(position, size));
                    break;
            }

            return entities;
        }

        /// <summary>
        /// 创建光轴符号
        /// </summary>
        public static Line CreateOpticalAxis(Vector2 start, Vector2 end)
        {
            var axis = new Line(start, end);
            axis.lineType = LineType.DashDot;
            axis.thickness = 0.25;
            axis.color = Color.FromRGB(255, 128, 0); // 橙色
            return axis;
        }

        /// <summary>
        /// 创建光线符号
        /// </summary>
        public static List<Entity> CreateRayPath(List<Vector2> points, bool showArrows = true)
        {
            var entities = new List<Entity>();

            // 创建光线路径
            for (int i = 0; i < points.Count - 1; i++)
            {
                var ray = new Line(points[i], points[i + 1]);
                ray.lineType = LineType.Solid;
                ray.thickness = 0.35;
                ray.color = Color.FromRGB(255, 0, 0); // 红色
                entities.Add(ray);
            }

            // 添加箭头
            if (showArrows && points.Count >= 2)
            {
                var lastSegment = points[points.Count - 1] - points[points.Count - 2];
                var arrowHead = CreateArrowHead(points[points.Count - 1], lastSegment.normalized, 2.0);
                entities.AddRange(arrowHead);
            }

            return entities;
        }

        /// <summary>
        /// 创建光阑符号
        /// </summary>
        public static List<Entity> CreateApertureSymbol(ApertureType type, Vector2 position, double diameter)
        {
            var entities = new List<Entity>();

            switch (type)
            {
                case ApertureType.Circular:
                    entities.Add(CreateCircularAperture(position, diameter));
                    break;
                
                case ApertureType.Iris:
                    entities.AddRange(CreateIrisAperture(position, diameter));
                    break;
                
                case ApertureType.Rectangular:
                    entities.AddRange(CreateRectangularAperture(position, diameter, diameter * 0.6));
                    break;
            }

            return entities;
        }

        /// <summary>
        /// 创建焦点符号
        /// </summary>
        public static List<Entity> CreateFocalPointSymbol(Vector2 position, double size = 3.0)
        {
            var entities = new List<Entity>();

            // 创建十字符号表示焦点
            var horizontal = new Line(
                position - new Vector2(size / 2, 0),
                position + new Vector2(size / 2, 0)
            );
            var vertical = new Line(
                position - new Vector2(0, size / 2),
                position + new Vector2(0, size / 2)
            );

            horizontal.thickness = 0.5;
            vertical.thickness = 0.5;
            horizontal.color = Color.FromRGB(255, 0, 0);
            vertical.color = Color.FromRGB(255, 0, 0);

            entities.Add(horizontal);
            entities.Add(vertical);

            // 添加"F"标记
            var text = new Text();
            text.Value = "F";
            text.Position = new Vector3(position.X + size, position.Y + size, 0);
            text.Height = 2.5;
            text.color = Color.ByLayer;
            entities.Add(text);

            return entities;
        }

        /// <summary>
        /// 创建光源符号
        /// </summary>
        public static List<Entity> CreateLightSourceSymbol(LightSourceType type, Vector2 position, double size)
        {
            var entities = new List<Entity>();

            switch (type)
            {
                case LightSourceType.Point:
                    entities.AddRange(CreatePointSource(position, size));
                    break;
                
                case LightSourceType.Collimated:
                    entities.AddRange(CreateCollimatedSource(position, size));
                    break;
                
                case LightSourceType.Laser:
                    entities.AddRange(CreateLaserSource(position, size));
                    break;
                
                case LightSourceType.LED:
                    entities.AddRange(CreateLEDSource(position, size));
                    break;
            }

            return entities;
        }

        #endregion

        #region 私有方法 - 透镜符号

        private static List<Entity> CreateBiConvexLens(Vector2 center, double diameter, double thickness)
        {
            var entities = new List<Entity>();
            double radius = diameter / 2;
            double sagita = thickness / 2;

            // 左侧弧
            var leftArc = CreateLensArc(center, radius, sagita, true);
            entities.Add(leftArc);

            // 右侧弧
            var rightArc = CreateLensArc(center, radius, sagita, false);
            entities.Add(rightArc);

            // 上下连接线
            var topLine = new Line(
                new Vector2(center.X - sagita, center.Y + radius),
                new Vector2(center.X + sagita, center.Y + radius)
            );
            var bottomLine = new Line(
                new Vector2(center.X - sagita, center.Y - radius),
                new Vector2(center.X + sagita, center.Y - radius)
            );

            topLine.thickness = 0.25;
            bottomLine.thickness = 0.25;
            entities.Add(topLine);
            entities.Add(bottomLine);

            return entities;
        }

        private static List<Entity> CreateBiConcaveLens(Vector2 center, double diameter, double thickness)
        {
            var entities = new List<Entity>();
            double radius = diameter / 2;
            double sagita = thickness / 4; // 凹透镜的矢高较小

            // 创建凹面弧线
            // 这里简化处理，实际应该计算准确的弧线
            var leftArc = CreateConcaveArc(center, radius, sagita, true);
            var rightArc = CreateConcaveArc(center, radius, sagita, false);
            
            entities.Add(leftArc);
            entities.Add(rightArc);

            // 上下边缘线
            var topLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y + radius),
                new Vector2(center.X + thickness / 2, center.Y + radius)
            );
            var bottomLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y - radius),
                new Vector2(center.X + thickness / 2, center.Y - radius)
            );

            entities.Add(topLine);
            entities.Add(bottomLine);

            return entities;
        }

        private static List<Entity> CreatePlanoConvexLens(Vector2 center, double diameter, double thickness)
        {
            var entities = new List<Entity>();
            double radius = diameter / 2;

            // 平面（左侧）
            var flatSide = new Line(
                new Vector2(center.X - thickness / 2, center.Y - radius),
                new Vector2(center.X - thickness / 2, center.Y + radius)
            );
            flatSide.thickness = 0.5;
            entities.Add(flatSide);

            // 凸面（右侧）
            var convexArc = CreateLensArc(center, radius, thickness / 2, false);
            entities.Add(convexArc);

            // 上下连接线
            var topLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y + radius),
                new Vector2(center.X + thickness / 2, center.Y + radius)
            );
            var bottomLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y - radius),
                new Vector2(center.X + thickness / 2, center.Y - radius)
            );

            entities.Add(topLine);
            entities.Add(bottomLine);

            return entities;
        }

        private static List<Entity> CreatePlanoConcaveLens(Vector2 center, double diameter, double thickness)
        {
            var entities = new List<Entity>();
            double radius = diameter / 2;
            double sagita = thickness * 0.3;

            // 凹面（右侧）
            var concaveArc = CreateConcaveArc(center, radius, sagita, false);
            entities.Add(concaveArc);

            // 平面（左侧）
            var leftLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y - radius),
                new Vector2(center.X - thickness / 2, center.Y + radius)
            );
            leftLine.lineWeight = LineWeight.LineWeight050;
            leftLine.color = Color.ByLayer;
            entities.Add(leftLine);

            // 上下边缘
            var topLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y + radius),
                concaveArc.startPoint
            );
            var bottomLine = new Line(
                new Vector2(center.X - thickness / 2, center.Y - radius),
                concaveArc.endPoint
            );
            topLine.lineWeight = LineWeight.LineWeight050;
            bottomLine.lineWeight = LineWeight.LineWeight050;
            topLine.color = Color.ByLayer;
            bottomLine.color = Color.ByLayer;
            entities.Add(topLine);
            entities.Add(bottomLine);

            return entities;
        }

        private static List<Entity> CreateMeniscusLens(Vector2 center, double diameter, double thickness)
        {
            var entities = new List<Entity>();
            double radius = diameter / 2;
            double sagita1 = thickness * 0.2;
            double sagita2 = thickness * 0.15;

            // 凸面（左侧）
            var convexArc = CreateLensArc(center, radius, sagita1, true);
            entities.Add(convexArc);

            // 凹面（右侧）
            var concaveArc = CreateConcaveArc(center, radius, sagita2, false);
            entities.Add(concaveArc);

            // 上下边缘
            var topLine = new Line(convexArc.endPoint, concaveArc.startPoint);
            var bottomLine = new Line(convexArc.startPoint, concaveArc.endPoint);
            topLine.lineWeight = LineWeight.LineWeight050;
            bottomLine.lineWeight = LineWeight.LineWeight050;
            topLine.color = Color.ByLayer;
            bottomLine.color = Color.ByLayer;
            entities.Add(topLine);
            entities.Add(bottomLine);

            return entities;
        }

        private static List<Entity> CreateGenericLens(Vector2 center, double diameter, double thickness)
        {
            // 创建通用透镜符号（简化的双凸透镜）
            return CreateBiConvexLens(center, diameter, thickness);
        }

        private static Arc CreateLensArc(Vector2 center, double aperture, double sagita, bool isLeft)
        {
            // 计算弧线参数
            double chordLength = 2 * aperture;
            double arcRadius = (sagita * sagita + aperture * aperture) / (2 * sagita);
            
            Vector2 arcCenter;
            double startAngle, endAngle;

            if (isLeft)
            {
                arcCenter = new Vector2(center.X - arcRadius + sagita, center.Y);
                startAngle = Math.Asin(aperture / arcRadius) * 180 / Math.PI;
                endAngle = -startAngle;
            }
            else
            {
                arcCenter = new Vector2(center.X + arcRadius - sagita, center.Y);
                startAngle = 180 - Math.Asin(aperture / arcRadius) * 180 / Math.PI;
                endAngle = 180 + Math.Asin(aperture / arcRadius) * 180 / Math.PI;
            }

            var arc = new Arc(arcCenter, arcRadius, startAngle, endAngle);
            arc.lineWeight = LineWeight.LineWeight050;
            arc.color = Color.ByLayer;

            return arc;
        }

        private static Arc CreateConcaveArc(Vector2 center, double aperture, double sagita, bool isLeft)
        {
            // 凹面弧线的创建逻辑
            // 这里简化处理，实际需要更精确的计算
            double arcRadius = aperture * 1.5;
            
            Vector2 arcCenter;
            double startAngle, endAngle;

            if (isLeft)
            {
                arcCenter = new Vector2(center.X + arcRadius - sagita, center.Y);
                startAngle = 180 + 20;
                endAngle = 180 - 20;
            }
            else
            {
                arcCenter = new Vector2(center.X - arcRadius + sagita, center.Y);
                startAngle = -20;
                endAngle = 20;
            }

            var arc = new Arc(arcCenter, arcRadius, startAngle, endAngle);
            arc.lineWeight = LineWeight.LineWeight050;
            arc.color = Color.ByLayer;

            return arc;
        }

        #endregion

        #region 私有方法 - 反射镜符号

        private static List<Entity> CreateFlatMirror(Vector2 position, double length, double angle)
        {
            var entities = new List<Entity>();

            // 反射面
            var mirror = new Line(
                position - new Vector2(length / 2, 0).Rotate(angle),
                position + new Vector2(length / 2, 0).Rotate(angle)
            );
            mirror.lineWeight = LineWeight.LineWeight070;
            mirror.color = Color.ByLayer;
            entities.Add(mirror);

            // 反射面背部标记
            var backOffset = new Vector2(0, -2).Rotate(angle);
            var back = new Line(
                position - new Vector2(length / 2, 0).Rotate(angle) + backOffset,
                position + new Vector2(length / 2, 0).Rotate(angle) + backOffset
            );
            back.thickness = 0.35;
            back.lineType = LineType.Dash;
            entities.Add(back);

            return entities;
        }

        private static List<Entity> CreateConcaveMirror(Vector2 center, double diameter, double angle)
        {
            var entities = new List<Entity>();

            // 创建凹面弧
            var arc = new Arc(center, diameter / 2, -30 + angle, 30 + angle);
            arc.lineWeight = LineWeight.LineWeight070;
            arc.color = Color.ByLayer;
            entities.Add(arc);

            // 背部标记
            var backArc = new Arc(center, diameter / 2 + 2, -30 + angle, 30 + angle);
            backArc.lineWeight = LineWeight.LineWeight035;
            backArc.lineType = LineType.Dash;
            entities.Add(backArc);

            return entities;
        }

        private static List<Entity> CreateConvexMirror(Vector2 center, double diameter, double angle)
        {
            var entities = new List<Entity>();

            // 创建凸面弧
            var arc = new Arc(center, diameter / 2, 150 + angle, 210 + angle);
            arc.lineWeight = LineWeight.LineWeight070;
            arc.color = Color.ByLayer;
            entities.Add(arc);

            // 背部标记
            var backArc = new Arc(center, diameter / 2 + 2, 150 + angle, 210 + angle);
            backArc.lineWeight = LineWeight.LineWeight035;
            backArc.lineType = LineType.Dash;
            entities.Add(backArc);

            return entities;
        }

        private static List<Entity> CreateDichroicMirror(Vector2 position, double length, double angle)
        {
            var entities = new List<Entity>();

            // 反射面（双线表示）
            var mirror1 = new Line(
                position - new Vector2(length / 2, 0).Rotate(angle),
                position + new Vector2(length / 2, 0).Rotate(angle)
            );
            mirror1.lineWeight = LineWeight.LineWeight070;
            mirror1.color = Color.ByLayer;
            entities.Add(mirror1);

            var offset = new Vector2(0, -1.5).Rotate(angle);
            var mirror2 = new Line(
                position - new Vector2(length / 2, 0).Rotate(angle) + offset,
                position + new Vector2(length / 2, 0).Rotate(angle) + offset
            );
            mirror2.lineWeight = LineWeight.LineWeight070;
            mirror2.color = Color.ByLayer;
            entities.Add(mirror2);

            // 添加标记表示二向色性
            var markOffset = new Vector2(0, 3).Rotate(angle);
            var mark = new Text();
            mark.Value = "DC";
            mark.Position = new Vector3(position.X + markOffset.X, position.Y + markOffset.Y, 0);
            mark.Height = 2.0;
            mark.alignment = TextAlignment.CenterMiddle;
            entities.Add(mark);

            return entities;
        }

        #endregion

        #region 私有方法 - 棱镜符号

        private static List<Entity> CreateRightAnglePrism(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建直角三角形
            var triangle = new Polyline();
            triangle.closed = true;
            triangle.AddVertexAt(0, position);
            triangle.AddVertexAt(1, position + new Vector2(size, 0));
            triangle.AddVertexAt(2, position + new Vector2(0, size));
            triangle.lineWeight = LineWeight.LineWeight070;
            triangle.color = Color.ByLayer;
            entities.Add(triangle);

            return entities;
        }

        private static List<Entity> CreatePentaPrism(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建五边形
            var pentagon = new Polyline();
            pentagon.closed = true;
            
            for (int i = 0; i < 5; i++)
            {
                double angle = i * 72 * Math.PI / 180;
                var vertex = position + new Vector2(
                    size * Math.Cos(angle),
                    size * Math.Sin(angle)
                );
                pentagon.AddVertexAt(i, vertex);
            }
            
            pentagon.lineWeight = LineWeight.LineWeight070;
            pentagon.color = Color.ByLayer;
            entities.Add(pentagon);

            return entities;
        }

        private static List<Entity> CreateDovePrism(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建梯形（道威棱镜的侧视图）
            var trapezoid = new Polyline();
            trapezoid.closed = true;
            trapezoid.AddVertexAt(0, position + new Vector2(-size * 0.7, -size * 0.3));
            trapezoid.AddVertexAt(1, position + new Vector2(size * 0.7, -size * 0.3));
            trapezoid.AddVertexAt(2, position + new Vector2(size * 0.5, size * 0.3));
            trapezoid.AddVertexAt(3, position + new Vector2(-size * 0.5, size * 0.3));
            trapezoid.lineWeight = LineWeight.LineWeight070;
            trapezoid.color = Color.ByLayer;
            entities.Add(trapezoid);

            return entities;
        }

        private static List<Entity> CreateWedgePrism(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建楔形
            var wedge = new Polyline();
            wedge.closed = true;
            wedge.AddVertexAt(0, position + new Vector2(-size / 2, -size / 2));
            wedge.AddVertexAt(1, position + new Vector2(size / 2, -size / 2));
            wedge.AddVertexAt(2, position + new Vector2(size / 2, size / 2));
            wedge.AddVertexAt(3, position + new Vector2(-size / 2, size / 3));
            wedge.lineWeight = LineWeight.LineWeight070;
            wedge.color = Color.ByLayer;
            entities.Add(wedge);

            return entities;
        }

        #endregion

        #region 私有方法 - 光阑和光源符号

        private static List<Entity> CreateRectangularAperture(Vector2 position, double width, double height)
        {
            var entities = new List<Entity>();

            // 创建矩形孔径
            var rect = new Polyline();
            rect.closed = true;
            rect.AddVertexAt(0, position + new Vector2(-width / 2, -height / 2));
            rect.AddVertexAt(1, position + new Vector2(width / 2, -height / 2));
            rect.AddVertexAt(2, position + new Vector2(width / 2, height / 2));
            rect.AddVertexAt(3, position + new Vector2(-width / 2, height / 2));
            rect.lineWeight = LineWeight.LineWeight050;
            rect.color = Color.ByLayer;
            entities.Add(rect);

            // 添加对角线表示阻挡
            var diag1 = new Line(
                position + new Vector2(-width / 2, -height / 2),
                position + new Vector2(width / 2, height / 2)
            );
            var diag2 = new Line(
                position + new Vector2(width / 2, -height / 2),
                position + new Vector2(-width / 2, height / 2)
            );
            diag1.lineWeight = LineWeight.LineWeight025;
            diag2.lineWeight = LineWeight.LineWeight025;
            diag1.color = Color.ByLayer;
            diag2.color = Color.ByLayer;
            entities.Add(diag1);
            entities.Add(diag2);

            return entities;
        }

        private static List<Entity> CreateCollimatedSource(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 创建平行光源符号
            double lineLength = size * 1.5;
            double spacing = size / 4;
            
            for (int i = -2; i <= 2; i++)
            {
                var line = new Line(
                    position + new Vector2(-lineLength / 2, i * spacing),
                    position + new Vector2(lineLength / 2, i * spacing)
                );
                line.lineWeight = LineWeight.LineWeight035;
                line.color = Color.ByLayer;
                entities.Add(line);

                // 添加箭头
                if (i % 2 == 0)
                {
                    var arrowTip = position + new Vector2(lineLength / 2, i * spacing);
                    entities.AddRange(CreateArrowHead(arrowTip, new Vector2(1, 0), size * 0.2));
                }
            }

            return entities;
        }

        private static List<Entity> CreateLaserSource(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // 激光器符号 - 矩形盒子
            var box = new Polyline();
            box.closed = true;
            box.AddVertexAt(0, position + new Vector2(-size, -size / 2));
            box.AddVertexAt(1, position + new Vector2(0, -size / 2));
            box.AddVertexAt(2, position + new Vector2(0, size / 2));
            box.AddVertexAt(3, position + new Vector2(-size, size / 2));
            box.lineWeight = LineWeight.LineWeight070;
            box.color = Color.ByLayer;
            entities.Add(box);

            // 输出光束
            var beam = new Line(
                position,
                position + new Vector2(size * 2, 0)
            );
            beam.lineWeight = LineWeight.LineWeight050;
            beam.color = Color.FromRGB(255, 0, 0);
            entities.Add(beam);

            // 添加"LASER"标记
            var text = new Text();
            text.Value = "LASER";
            text.Position = new Vector3(position.X - size / 2, position.Y, 0);
            text.Height = size * 0.3;
            text.alignment = TextAlignment.CenterMiddle;
            entities.Add(text);

            return entities;
        }

        private static List<Entity> CreateLEDSource(Vector2 position, double size)
        {
            var entities = new List<Entity>();

            // LED符号 - 半圆形
            var arc = new Arc(position, size / 2, 0, 180);
            arc.lineWeight = LineWeight.LineWeight070;
            arc.color = Color.ByLayer;
            entities.Add(arc);

            // 底部线
            var baseLine = new Line(
                position + new Vector2(-size / 2, 0),
                position + new Vector2(size / 2, 0)
            );
            baseLine.lineWeight = LineWeight.LineWeight070;
            baseLine.color = Color.ByLayer;
            entities.Add(baseLine);

            // 发光线
            for (int i = -1; i <= 1; i++)
            {
                double angle = 45 + i * 45;
                double rad = angle * Math.PI / 180;
                var ray = new Line(
                    position,
                    position + new Vector2(Math.Cos(rad), Math.Sin(rad)) * size
                );
                ray.lineWeight = LineWeight.LineWeight025;
                ray.color = Color.ByLayer;
                entities.Add(ray);

                // 箭头
                var arrowTip = position + new Vector2(Math.Cos(rad), Math.Sin(rad)) * size;
                entities.AddRange(CreateArrowHead(arrowTip, new Vector2(Math.Cos(rad), Math.Sin(rad)), size * 0.15));
            }

            return entities;
        }

        #endregion

        #region 私有方法 - 其他符号

        private static List<Entity> CreateArrowHead(Vector2 tip, Vector2 direction, double size)
        {
            var entities = new List<Entity>();

            var perpendicular = new Vector2(-direction.Y, direction.X);
            var leftWing = tip - direction * size + perpendicular * size * 0.3;
            var rightWing = tip - direction * size - perpendicular * size * 0.3;

            var leftLine = new Line(tip, leftWing);
            var rightLine = new Line(tip, rightWing);

            leftLine.lineWeight = LineWeight.LineWeight035;
            rightLine.lineWeight = LineWeight.LineWeight035;
            leftLine.color = Color.FromRGB(255, 0, 0);
            rightLine.color = Color.FromRGB(255, 0, 0);

            entities.Add(leftLine);
            entities.Add(rightLine);

            return entities;
        }

        private static Circle CreateCircularAperture(Vector2 center, double diameter)
        {
            var aperture = new Circle(center, diameter / 2);
            aperture.lineWeight = LineWeight.LineWeight050;
            aperture.color = Color.ByLayer;
            return aperture;
        }

        private static List<Entity> CreateIrisAperture(Vector2 center, double diameter)
        {
            var entities = new List<Entity>();

            // 外圆
            var outer = new Circle(center, diameter / 2);
            outer.lineWeight = LineWeight.LineWeight050;
            entities.Add(outer);

            // 内部叶片表示
            int bladeCount = 6;
            double bladeAngle = 360.0 / bladeCount;
            double innerRadius = diameter * 0.3;

            for (int i = 0; i < bladeCount; i++)
            {
                double angle = i * bladeAngle * Math.PI / 180;
                var bladeLine = new Line(
                    center + new Vector2(Math.Cos(angle), Math.Sin(angle)) * innerRadius,
                    center + new Vector2(Math.Cos(angle), Math.Sin(angle)) * diameter / 2
                );
                bladeLine.lineWeight = LineWeight.LineWeight025;
                entities.Add(bladeLine);
            }

            return entities;
        }

        private static List<Entity> CreatePointSource(Vector2 center, double size)
        {
            var entities = new List<Entity>();

            // 中心点
            var centerCircle = new Circle(center, size * 0.1);
            centerCircle.lineWeight = LineWeight.LineWeight050;
            entities.Add(centerCircle);

            // 放射线
            int rayCount = 8;
            for (int i = 0; i < rayCount; i++)
            {
                double angle = i * 360.0 / rayCount * Math.PI / 180;
                var ray = new Line(
                    center + new Vector2(Math.Cos(angle), Math.Sin(angle)) * size * 0.2,
                    center + new Vector2(Math.Cos(angle), Math.Sin(angle)) * size
                );
                ray.lineWeight = LineWeight.LineWeight025;
                entities.Add(ray);
            }

            return entities;
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 透镜类型
    /// </summary>
    public enum LensType
    {
        BiConvex,       // 双凸
        BiConcave,      // 双凹
        PlanoConvex,    // 平凸
        PlanoConcave,   // 平凹
        Meniscus,       // 弯月形
        Achromatic,     // 消色差
        Cylindrical,    // 柱面
        Aspheric        // 非球面
    }

    /// <summary>
    /// 反射镜类型
    /// </summary>
    public enum MirrorType
    {
        Flat,           // 平面镜
        Concave,        // 凹面镜
        Convex,         // 凸面镜
        Dichroic,       // 二向色镜
        Parabolic,      // 抛物面镜
        Elliptical      // 椭球面镜
    }

    /// <summary>
    /// 棱镜类型
    /// </summary>
    public enum PrismType
    {
        RightAngle,     // 直角棱镜
        Penta,          // 五角棱镜
        Dove,           // 道威棱镜
        Wedge,          // 楔形棱镜
        Roof,           // 屋脊棱镜
        Dispersive      // 色散棱镜
    }

    /// <summary>
    /// 光阑类型
    /// </summary>
    public enum ApertureType
    {
        Circular,       // 圆形光阑
        Iris,           // 可变光阑
        Rectangular,    // 矩形光阑
        Elliptical      // 椭圆光阑
    }

    /// <summary>
    /// 光源类型
    /// </summary>
    public enum LightSourceType
    {
        Point,          // 点光源
        Collimated,     // 平行光
        Laser,          // 激光
        LED,            // LED
        Fiber,          // 光纤
        Extended        // 扩展光源
    }

    #endregion

    #region 扩展方法

    public static class Vector2Extensions
    {
        public static Vector2 Rotate(this Vector2 v, double angleInDegrees)
        {
            double rad = angleInDegrees * Math.PI / 180;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new Vector2(
                v.X * cos - v.Y * sin,
                v.X * sin + v.Y * cos
            );
        }
    }

    #endregion
}