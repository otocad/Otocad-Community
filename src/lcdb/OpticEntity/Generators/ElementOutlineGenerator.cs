using System;
using System.Collections.Generic;
using lcdb;
using LitMath;

namespace OtoCAD.OpticEntity.Generators
{
    /// <summary>
    /// Element轮廓Entity生成器
    /// 负责从Element数据生成轮廓相关的Entity
    /// </summary>
    public static class ElementOutlineGenerator
    {
        /// <summary>
        /// 生成LensOutlineEntity组合实体
        /// </summary>
        /// <param name="element">Element数据</param>
        /// <returns>组合的轮廓实体</returns>
        public static LensOutlineEntity GenerateOutlineEntity(Element element)
        {
            var outlineEntity = new LensOutlineEntity
            {
                ParentElement = element,
                SelectAsGroup = true,
                IsVisible = true
            };

            // 生成所有子实体并添加到组合实体中
            var childEntities = GenerateOutlineChildrenFromElement(element);

            foreach (var entity in childEntities)
            {
                outlineEntity.AddChild(entity);
            }

            return outlineEntity;
        }

        /// <summary>
        /// 生成Element的轮廓Entity列表（向后兼容）
        /// </summary>
        /// <param name="element">Element数据</param>
        /// <returns>轮廓Entity列表</returns>
        public static List<Entity> GenerateOutline(Element element)
        {
            return GenerateOutlineChildren(element);
        }

        /// <summary>
        /// 生成轮廓子实体列表（新方法用于Element）
        /// </summary>
        private static List<Entity> GenerateOutlineChildrenFromElement(Element element)
        {
            var entities = new List<Entity>();

            if (element == null)
                return entities;

            var originX = element.OriginalX;
            var originY = element.OriginalY;
            var thickness = element.Thickness;
            var semiDiameter = element.SemiDiameter;
            var radius1 = element.Surface1?.Radius ?? 0.0;
            var radius2 = element.Surface2?.Radius ?? 0.0;

            // 1. 生成Surface1的弧线
            if (Math.Abs(radius1) > 1e-10)
            {
                var arc1 = GenerateSurfaceArcFromParams(radius1, semiDiameter, originX, originY);
                if (arc1 != null)
                    entities.Add(arc1);
            }
            else
            {
                // 平面：生成垂直线
                entities.Add(new Line(
                    new Vector2(originX, originY - semiDiameter),
                    new Vector2(originX, originY + semiDiameter)
                ));
            }

            // 2. 生成Surface2的弧线
            if (Math.Abs(radius2) > 1e-10)
            {
                var arc2 = GenerateSurfaceArcFromParams(radius2, semiDiameter, originX + thickness, originY);
                if (arc2 != null)
                    entities.Add(arc2);
            }
            else
            {
                // 平面：生成垂直线
                entities.Add(new Line(
                    new Vector2(originX + thickness, originY - semiDiameter),
                    new Vector2(originX + thickness, originY + semiDiameter)
                ));
            }

            // 3. 生成连接线（顶部和底部）
            var sag1 = CalculateOutlineSag(radius1, semiDiameter);
            var sag2 = CalculateOutlineSag(radius2, semiDiameter);

            // 顶部水平线
            entities.Add(new Line(
                new Vector2(originX + sag1, originY + semiDiameter),
                new Vector2(originX + thickness + sag2, originY + semiDiameter)
            ));

            // 底部水平线
            entities.Add(new Line(
                new Vector2(originX + sag1, originY - semiDiameter),
                new Vector2(originX + thickness + sag2, originY - semiDiameter)
            ));

            // 4. 生成超半球垂直线（如果需要）
            // 对于超半球透镜，弧线端点不会延伸到semiDiameter，需要垂直线连接
            if (Math.Abs(radius1) > 1e-10 && semiDiameter > Math.Abs(radius1))
            {
                // Surface1是超半球，需要垂直线
                var arc1 = entities[0] as Arc;
                if (arc1 != null)
                {
                    // 上方垂直线
                    var topY = Math.Max(arc1.startPoint.Y, arc1.endPoint.Y);
                    var bottomY = Math.Min(arc1.startPoint.Y, arc1.endPoint.Y);
                    var xPos = originX + sag1;

                    entities.Add(new Line(
                        new Vector2(xPos, topY),
                        new Vector2(xPos, originY + semiDiameter)
                    ));

                    // 下方垂直线
                    entities.Add(new Line(
                        new Vector2(xPos, bottomY),
                        new Vector2(xPos, originY - semiDiameter)
                    ));
                }
            }

            if (Math.Abs(radius2) > 1e-10 && semiDiameter > Math.Abs(radius2))
            {
                // Surface2是超半球，需要垂直线
                var arc2Index = Math.Abs(radius1) > 1e-10 ? 1 : 0;
                var arc2 = entities[arc2Index] as Arc;
                if (arc2 != null)
                {
                    // 上方垂直线
                    var topY = Math.Max(arc2.startPoint.Y, arc2.endPoint.Y);
                    var bottomY = Math.Min(arc2.startPoint.Y, arc2.endPoint.Y);
                    var xPos = originX + thickness + sag2;

                    entities.Add(new Line(
                        new Vector2(xPos, topY),
                        new Vector2(xPos, originY + semiDiameter)
                    ));

                    // 下方垂直线
                    entities.Add(new Line(
                        new Vector2(xPos, bottomY),
                        new Vector2(xPos, originY - semiDiameter)
                    ));
                }
            }

            // 5. 生成中心线（虚线）
            var centerLine = new Line(
                new Vector2(originX - semiDiameter * 0.3, originY),
                new Vector2(originX + thickness + semiDiameter * 0.3, originY)
            );
            centerLine.lineType = LineType.DashDot;
            entities.Add(centerLine);

            return entities;
        }

        /// <summary>
        /// 根据参数生成表面弧线
        /// </summary>
        private static Arc GenerateSurfaceArcFromParams(double radius, double semiDiameter, double centerX, double centerY)
        {
            if (Math.Abs(radius) < 1e-10)
                return null;

            // 计算弧的参数
            double angle;
            double startAngle, endAngle;

            if (semiDiameter < Math.Abs(radius))
            {
                // 正常球面
                // 注意：当radius为负时，直接用带符号的radius进行计算
                angle = Math.Asin(semiDiameter / radius);
            }
            else
            {
                // 超半球
                angle = radius < 0 ? -Math.PI / 2.0 : Math.PI / 2.0;
            }

            // Arc的中心点：从表面基点偏移一个半径的距离
            double arcCenterX = centerX + radius;

            if (radius > 0)
            {
                // 凸面
                startAngle = Math.PI - angle;
                endAngle = Math.PI + angle;
            }
            else
            {
                // 凹面
                // 当radius<0时，angle是负值
                startAngle = angle;  // 负值
                endAngle = -angle;   // 正值
            }

            return new Arc
            {
                center = new Vector2(arcCenterX, centerY),
                radius = Math.Abs(radius),
                startAngle = startAngle,
                endAngle = endAngle
            };
        }

        /// <summary>
        /// 计算表面矢高（轮廓生成器专用）
        /// </summary>
        private static double CalculateOutlineSag(double radius, double semiDiameter)
        {
            if (Math.Abs(radius) < 1e-10)
                return 0;

            if (semiDiameter >= Math.Abs(radius))
            {
                // 超半球
                return radius > 0 ? Math.Abs(radius) : -Math.Abs(radius);
            }

            var sag = Math.Abs(radius) - Math.Sqrt(radius * radius - semiDiameter * semiDiameter);
            return radius > 0 ? sag : -sag;
        }

        /// <summary>
        /// 生成轮廓子实体列表（从Element）
        /// </summary>
        private static List<Entity> GenerateOutlineChildren(Element element)
        {
            var entities = new List<Entity>();

            if (element == null || element.Surface1 == null || element.Surface2 == null)
                return entities;

            var originX = element.OriginalX;
            var originY = element.OriginalY;

            // 1. 生成Surface1的弧线
            var surface1Arc = GenerateSurfaceArc(
                element.Surface1,
                originX,
                originY
            );
            if (surface1Arc != null)
                entities.Add(surface1Arc);

            // 2. 生成Surface2的弧线
            var surface2Arc = GenerateSurfaceArc(
                element.Surface2,
                originX + element.Surface1.Thickness,
                originY
            );
            if (surface2Arc != null)
                entities.Add(surface2Arc);

            // 3. 生成连接线（顶部和底部）
            entities.AddRange(GenerateConnectingLines(element));

            // 4. 生成中心线
            var centerLine = GenerateCenterLine(element);
            if (centerLine != null)
                entities.Add(centerLine);

            return entities;
        }

        /// <summary>
        /// 生成表面弧线
        /// </summary>
        private static Arc GenerateSurfaceArc(IElementSurface surface, double baseX, double baseY)
        {
            if (surface == null || Math.Abs(surface.Radius) < 1e-10)
                return null;

            surface.BasePoint1 = new Vector2(baseX, baseY);
            surface.GenEntity();

            if (surface.ProfileEntity is Arc arc)
            {
                return new Arc
                {
                    center = arc.center,
                    radius = arc.radius,
                    startAngle = arc.startAngle,
                    endAngle = arc.endAngle
                };
            }

            return null;
        }

        /// <summary>
        /// 生成连接线（顶部和底部水平线）
        /// </summary>
        private static List<Entity> GenerateConnectingLines(Element element)
        {
            var lines = new List<Entity>();

            var originX = element.OriginalX;
            var originY = element.OriginalY;

            // 计算实际直径
            var maxDiameter = Math.Max(
                element.Surface1?.RealDiameter ?? element.SemiDiameter,
                element.Surface2?.RealDiameter ?? element.SemiDiameter
            );

            // 计算Surface1和Surface2的sag点
            var surface1SagX = originX + (element.Surface1?.SagPoint.X - element.Surface1?.BasePoint1.X ?? 0);
            var surface2SagX = originX + element.Thickness + (element.Surface2?.SagPoint.X - element.Surface2?.BasePoint1.X ?? 0);

            // 顶部水平线
            var topLine = new Line(
                new Vector2(surface1SagX, originY + maxDiameter),
                new Vector2(surface2SagX, originY + maxDiameter)
            );
            lines.Add(topLine);

            // 底部水平线
            var bottomLine = new Line(
                new Vector2(surface1SagX, originY - maxDiameter),
                new Vector2(surface2SagX, originY - maxDiameter)
            );
            lines.Add(bottomLine);

            // 如果直径不等，添加垂直连接线
            var surface1Diameter = element.Surface1?.RealDiameter ?? element.SemiDiameter * 2;
            var surface2Diameter = element.Surface2?.RealDiameter ?? element.SemiDiameter * 2;

            if (Math.Abs(surface1Diameter - surface2Diameter) > 1e-6)
            {
                var minDiameter = Math.Min(surface1Diameter, surface2Diameter) / 2;
                var vLineX = surface1Diameter < surface2Diameter ? surface1SagX : surface2SagX;

                // 顶部垂直线
                var topVLine = new Line(
                    new Vector2(vLineX, originY + minDiameter),
                    new Vector2(vLineX, originY + maxDiameter)
                );
                lines.Add(topVLine);

                // 底部垂直线
                var bottomVLine = new Line(
                    new Vector2(vLineX, originY - minDiameter),
                    new Vector2(vLineX, originY - maxDiameter)
                );
                lines.Add(bottomVLine);
            }

            return lines;
        }

        /// <summary>
        /// 生成中心线
        /// </summary>
        private static Line GenerateCenterLine(Element element)
        {
            const double CENTER_LINE_OFFSET = 10.0; // 中心线延伸偏移

            var centerLine = new Line(
                new Vector2(element.OriginalX - CENTER_LINE_OFFSET, element.OriginalY),
                new Vector2(element.OriginalX + element.Thickness + CENTER_LINE_OFFSET, element.OriginalY)
            );

            // 设置为点划线
            centerLine.lineType = LineType.DashDot;

            return centerLine;
        }

        /// <summary>
        /// 计算表面的实际sag值
        /// </summary>
        private static double CalculateSag(double radius, double semiDiameter)
        {
            if (Math.Abs(radius) < 1e-10)
                return 0;

            if (semiDiameter >= Math.Abs(radius))
            {
                // 超半球情况
                return radius > 0 ? Math.Abs(radius) : -Math.Abs(radius);
            }

            // 标准球面情况
            var sag = Math.Abs(radius) - Math.Sqrt(radius * radius - semiDiameter * semiDiameter);
            return radius > 0 ? sag : -sag;
        }
    }
}