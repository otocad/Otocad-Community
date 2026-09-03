using LitMath;
using System;
using System.Collections.Generic;

using OtoCAD;
namespace lcdb
{
    public class Circle : Entity
    {
        /// <summary>
        /// ����
        /// </summary>
        public override string className
        {
            get { return "Circle"; }
        }

        /// <summary>
        /// Բ��
        /// </summary>
        private LitMath.Vector2 _center = new LitMath.Vector2(0, 0);
        public LitMath.Vector2 center
        {
            get { return _center; }
            set { _center = value; }
        }

        /// <summary>
        /// �뾶
        /// </summary>
        private double _radius = 0.0;
        public double radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        /// <summary>
        /// ֱ��
        /// </summary>
        public double diameter
        {
            get { return _radius * 2; }
        }

        /// <summary>
        /// ��Χ�߿�
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                return new Bounding(_center, this.diameter, this.diameter);
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public Circle()
        {
        }

        public Circle(LitMath.Vector2 center, double radius)
        {
            _center = center;
            _radius = radius;
        }

        /// <summary>
        /// 构造函数 - 指定圆心、半径和来源
        /// </summary>
        public Circle(LitMath.Vector2 center, double radius, EntitySource source, ObjectId? parentId = null)
            : this(center, radius)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            // 与 Line.Draw 同约定: 仅当自身显式指定 RGB (ByColor/ByEntity) 时覆盖颜色, 否则沿用外部默认.
            // 否则镀膜符号等复合实体里的圆会落到环境色, 与同体的线/弧颜色不一致 (出图保真 bug).
            var prevColor = gd.CurrentColor;
            try
            {
                if (color.colorMethod == Colors.ColorMethod.ByColor
                    || color.colorMethod == Colors.ColorMethod.ByEntity)
                    gd.CurrentColor = color.ToDrawingColor();
                gd.DrawCircle(_center, _radius);
            }
            finally
            {
                gd.CurrentColor = prevColor;
            }
        }

        /// <summary>
        /// ��¡����
        /// </summary>
        public override object Clone()
        {
            Circle circle = base.Clone() as Circle;
            circle._center = _center;
            circle._radius = _radius;
            return circle;
        }

        /// <summary>
        /// ����Բʵ��
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Circle();
        }

        /// <summary>
        /// ƽ��
        /// </summary>
        public override void Translate(LitMath.Vector2 translation)
        {
            _center += translation;
        }
        public override void Rotate(LitMath.Vector2 center, double angle)
        {
            _center = LitMath.Vector2.RotateInRadian(_center, center, angle);
        }

        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(LitMath.Matrix3 transform)
        {
            LitMath.Vector2 pnt = _center + new LitMath.Vector2(_radius, 0);

            _center = transform * _center;
            _radius = (transform * pnt - _center).length;
        }

        /// <summary>
        /// ����׽��
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, _center));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, _center + new LitMath.Vector2(_radius, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, _center + new LitMath.Vector2(0, _radius)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, _center + new LitMath.Vector2(-_radius, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, _center + new LitMath.Vector2(0, -_radius)));
            return snapPnts;
        }

        /// <summary>
        /// ��ȡ�е�
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.Center, _center));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(_radius, 0)));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(0, _radius)));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(-_radius, 0)));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(0, -_radius)));
            
            return gripPnts;
        }

        /// <summary>
        /// ���üе�
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            if (index == 0)
            {
                _center = newPosition;
            }
            else if (index >= 1 && index <= 4)
            {
                _radius = (newPosition - _center).length;
            }
        }

        internal void SetRadius(double v)
        {
            radius = v;
        }

        internal void SetCenter(Vector2 vector2)
        {
            center = vector2;
        }
    }
}
