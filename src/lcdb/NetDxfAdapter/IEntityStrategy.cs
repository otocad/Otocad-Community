using System;
using System.Collections.Generic;
using System.Xml;
using LitMath;
using netDxf.Entities;
using OtoCAD;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// 实体策略接口，定义不同类型netDxf实体的OtoCAD操作策略
    /// </summary>
    /// <typeparam name="T">netDxf实体类型</typeparam>
    public interface IEntityStrategy<T> where T : EntityObject
    {
        #region 核心绘制和编辑功能

        /// <summary>
        /// 获取实体的夹点列表
        /// </summary>
        List<GripPoint> GetGripPoints(T entity);

        /// <summary>
        /// 设置指定索引的夹点位置
        /// </summary>
        void SetGripPointAt(T entity, int index, Vector2 newPosition);

        /// <summary>
        /// 绘制实体
        /// </summary>
        void Draw(OtoCAD.IGraphicsDraw gd, T entity);

        /// <summary>
        /// 获取实体边界框
        /// </summary>
        Bounding GetBounding(T entity);

        #endregion

        #region 几何变换

        /// <summary>
        /// 平移实体
        /// </summary>
        void Translate(T entity, Vector2 translation);

        /// <summary>
        /// 绕指定点旋转实体
        /// </summary>
        void Rotate(T entity, Vector2 center, double angle);

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        void TransformBy(T entity, Matrix3 transform);

        /// <summary>
        /// 镜像实体
        /// </summary>
        void Mirror(T entity, Vector2 p1, Vector2 p2);

        /// <summary>
        /// 缩放实体
        /// </summary>
        void Scale(T entity, Vector2 center, double scale);

        #endregion

        #region 序列化支持

        /// <summary>
        /// 序列化实体到XML
        /// </summary>
        void XmlOut(XmlWriter xmlWriter, T entity);

        /// <summary>
        /// 从XML反序列化实体
        /// </summary>
        void XmlIn(XmlReader xmlReader, T entity);

        #endregion

        #region 捕捉点支持

        /// <summary>
        /// 获取实体的捕捉点
        /// </summary>
        List<Vector2> GetSnapPoints(T entity);

        #endregion
    }

    /// <summary>
    /// 实体策略基类，提供通用实现
    /// </summary>
    /// <typeparam name="T">netDxf实体类型</typeparam>
    public abstract class EntityStrategyBase<T> : IEntityStrategy<T> where T : EntityObject
    {
        #region 抽象方法

        public abstract List<GripPoint> GetGripPoints(T entity);
        public abstract void SetGripPointAt(T entity, int index, Vector2 newPosition);
        public abstract void Draw(IGraphicsDraw gd, T entity);
        public abstract Bounding GetBounding(T entity);
        public abstract List<Vector2> GetSnapPoints(T entity);

        #endregion

        #region 默认几何变换实现

        public virtual void Translate(T entity, Vector2 translation)
        {
            var transform = CoordinateConverter.CreateTranslationMatrix(translation);
            ApplyTransform(entity, netDxf.Matrix3.Identity, transform);
        }

        public virtual void Rotate(T entity, Vector2 center, double angle)
        {
            var transform = CoordinateConverter.CreateRotationMatrix(center, angle);
            entity.TransformBy(transform, netDxf.Vector3.Zero);
        }

        public virtual void TransformBy(T entity, Matrix3 transform)
        {
            var netDxfTransform = CoordinateConverter.ToNetDxfMatrix3(transform);
            ApplyTransform(entity, netDxfTransform, netDxf.Vector3.Zero);
        }

        public virtual void Mirror(T entity, Vector2 p1, Vector2 p2)
        {
            var transform = CoordinateConverter.CreateMirrorMatrix(p1, p2);
            ApplyTransform(entity, transform, netDxf.Vector3.Zero);
        }

        public virtual void Scale(T entity, Vector2 center, double scale)
        {
            var transform = CoordinateConverter.CreateScaleMatrix(center, scale);
            ApplyTransform(entity, transform, netDxf.Vector3.Zero);
        }

        /// <summary>
        /// 应用变换矩阵到实体
        /// </summary>
        protected virtual void ApplyTransform(T entity, netDxf.Matrix3 transform, netDxf.Vector3 translation)
        {
            entity.TransformBy(transform, translation);
        }

        #endregion

        #region 默认序列化实现

        [System.Obsolete("XML serialization is deprecated, use JSON format", false)]
        public virtual void XmlOut(XmlWriter xmlWriter, T entity)
        {
            // 基础属性序列化
            xmlWriter.WriteAttributeString("color", entity.Color.ToString());
            xmlWriter.WriteAttributeString("layer", entity.Layer?.Name ?? "0");
            xmlWriter.WriteAttributeString("lineType", entity.Linetype?.Name ?? "Continuous");
            xmlWriter.WriteAttributeString("lineweight", entity.Lineweight.ToString());
            
            // 调用子类特定序列化
            XmlOutSpecific(xmlWriter, entity);
        }

        [System.Obsolete("XML serialization is deprecated, use JSON format", false)]
        public virtual void XmlIn(XmlReader xmlReader, T entity)
        {
            // 基础属性反序列化
            if (xmlReader.GetAttribute("color") != null)
            {
                // 颜色处理
            }
            
            if (xmlReader.GetAttribute("layer") != null)
            {
                // 图层处理
            }
            
            // 调用子类特定反序列化
            XmlInSpecific(xmlReader, entity);
        }

        /// <summary>
        /// 子类特定的序列化
        /// </summary>
        protected virtual void XmlOutSpecific(XmlWriter xmlWriter, T entity)
        {
            // 子类重写此方法来处理特定属性
        }

        /// <summary>
        /// 子类特定的反序列化
        /// </summary>
        protected virtual void XmlInSpecific(XmlReader xmlReader, T entity)
        {
            // 子类重写此方法来处理特定属性
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建夹点
        /// </summary>
        protected GripPoint CreateGripPoint(Vector2 position, GripPointType type)
        {
            return new GripPoint( type, position);
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        protected double Distance(Vector2 p1, Vector2 p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        /// <summary>
        /// 计算角度
        /// </summary>
        protected double Angle(Vector2 center, Vector2 point)
        {
            return Math.Atan2(point.Y - center.Y, point.X - center.X);
        }

        #endregion
    }
}