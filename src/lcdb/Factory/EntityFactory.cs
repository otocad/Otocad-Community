using System;
using System.Collections.Generic;
using LitMath;
using lcdb;

namespace lcdb.Factory
{
    /// <summary>
    /// 实体创建工厂类
    /// 提供创建带有来源标记的实体的统一接口
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// 创建带有来源标记的直线
        /// </summary>
        public static Line CreateLine(Vector2 startPoint, Vector2 endPoint, EntitySource source, ObjectId? parentId = null)
        {
            return new Line(startPoint, endPoint, source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的圆
        /// </summary>
        public static Circle CreateCircle(Vector2 center, double radius, EntitySource source, ObjectId? parentId = null)
        {
            return new Circle(center, radius, source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的圆弧
        /// </summary>
        public static Arc CreateArc(Vector2 center, double radius, double startAngle, double endAngle, EntitySource source, ObjectId? parentId = null)
        {
            return new Arc(center, radius, startAngle, endAngle, source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的多段线
        /// </summary>
        public static Polyline CreatePolyline(EntitySource source, ObjectId? parentId = null)
        {
            return new Polyline(source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的多段线（带顶点）
        /// </summary>
        public static Polyline CreatePolyline(IEnumerable<Vector2> vertices, EntitySource source, ObjectId? parentId = null)
        {
            var polyline = new Polyline(source, parentId);
            foreach (var vertex in vertices)
            {
                polyline.AddVertexAt(polyline.NumberOfVertices, vertex);
            }
            return polyline;
        }

        /// <summary>
        /// 创建带有来源标记的文本
        /// </summary>
        public static Text CreateText(string value, Vector3 position, double height, EntitySource source, ObjectId? parentId = null)
        {
            return new Text(value, position, height, source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的线性标注
        /// </summary>
        public static LinearDimension CreateLinearDimension(Vector2 firstPoint, Vector2 secondPoint, double offset, double rotation, EntitySource source, ObjectId? parentId = null)
        {
            return new LinearDimension(firstPoint, secondPoint, offset, rotation, source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的椭圆
        /// </summary>
        public static Ellipse CreateEllipse(Vector2 center, double radiusX, double radiusY, EntitySource source, ObjectId? parentId = null)
        {
            return new Ellipse(center, radiusX, radiusY, source, parentId);
        }

        /// <summary>
        /// 创建带有来源标记的样条曲线
        /// </summary>
        public static Spline CreateSpline(IEnumerable<Vector2> controlPoints, EntitySource source, ObjectId? parentId = null)
        {
            return new Spline(controlPoints, source, parentId);
        }

        /// <summary>
        /// 批量创建光学元件生成的实体
        /// </summary>
        public static List<Entity> CreateOpticalElementEntities(ObjectId elementId)
        {
            var entities = new List<Entity>();
            // 这里可以根据光学元件的具体需求创建多个实体
            return entities;
        }

        /// <summary>
        /// 批量创建光学框架生成的实体
        /// </summary>
        public static List<Entity> CreateOpticalFrameEntities(ObjectId frameId)
        {
            var entities = new List<Entity>();
            // 这里可以根据框架的具体需求创建多个实体
            return entities;
        }

        /// <summary>
        /// 标记一组实体为生成的实体
        /// </summary>
        public static void MarkEntitiesAsGenerated(IEnumerable<Entity> entities, EntitySource source, ObjectId parentId)
        {
            foreach (var entity in entities)
            {
                EntitySourceHelper.MarkAsGenerated(entity, source, parentId);
            }
        }

        /// <summary>
        /// 克隆实体并保持来源信息
        /// </summary>
        public static T CloneWithSource<T>(T entity) where T : Entity
        {
            var cloned = entity.Clone() as T;
            // Clone方法已经会复制Source和ParentComponentId属性
            return cloned;
        }

        /// <summary>
        /// 创建实体的手动副本（清除生成标记）
        /// </summary>
        public static T CreateManualCopy<T>(T entity) where T : Entity
        {
            var copy = entity.Clone() as T;
            EntitySourceHelper.ClearGeneratedMark(copy);
            return copy;
        }
    }
}