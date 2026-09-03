using System;
using System.Collections.Generic;
using System.Text;

using OtoCAD;
namespace lcdb
{
    public class Ellipse : Entity
    {
        /// <summary>
        /// ����
        /// </summary>
        public override string className
        {
            get { return "Ellipse"; }
        }

        /// <summary>
        /// ��Բ��
        /// </summary>
        private LitMath.Vector2 _center = new LitMath.Vector2(0, 0);
        public LitMath.Vector2 center
        {
            get { return _center; }
            set { _center = value; }
        }

        /// <summary>
        /// �뾶X
        /// </summary>
        private double _radiusX = 0.0;
        public double radiusX
        {
            get { return _radiusX; }
            set { _radiusX = value; }
        }

        /// <summary>
        /// �뾶Y
        /// </summary>
        private double _radiusY = 0.0;
        public double radiusY
        {
            get { return _radiusY; }
            set { _radiusY = value; }
        }

        /// <summary>
        /// ֱ��X
        /// </summary>
        public double diameterX
        {
            get { return _radiusX * 2; }
        }

        /// <summary>
        /// ֱ��Y
        /// </summary>
        public double diameterY
        {
            get { return _radiusY * 2; }
        }

        /// <summary>
        /// ��ת�Ƕȣ����ȣ�
        /// </summary>
        private double _rotation = 0.0;
        public double rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        /// <summary>
        /// ��ʼ�Ƕȣ����ȣ�
        /// </summary>
        private double _startAngle = 0.0;
        public double startAngle
        {
            get { return _startAngle; }
            set { _startAngle = value; }
        }

        /// <summary>
        /// �����Ƕȣ����ȣ�
        /// </summary>
        private double _endAngle = 2 * Math.PI;
        public double endAngle
        {
            get { return _endAngle; }
            set { _endAngle = value; }
        }

        /// <summary>
        /// �����������������ԣ�
        /// </summary>
        public LitMath.Vector2 majorAxis
        {
            get
            {
                double cos = Math.Cos(_rotation);
                double sin = Math.Sin(_rotation);
                return new LitMath.Vector2(cos * _radiusX, sin * _radiusX);
            }
        }

        /// <summary>
        /// ���᳤�ȣ��������ԣ�
        /// </summary>
        public double minorAxis
        {
            get { return _radiusY; }
        }

        /// <summary>
        /// ��Χ�߿�
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                return new Bounding(_center, this.diameterX, this.diameterY);
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public Ellipse()
        {
        }

        public Ellipse(LitMath.Vector2 center, double radiusX, double radiusY)
        {
            _center = center;
            _radiusX = radiusX;
            _radiusY = radiusY;
        }

        /// <summary>
        /// 构造函数 - 指定椭圆中心、半径和来源
        /// </summary>
        /// <param name="center">椭圆中心</param>
        /// <param name="radiusX">X轴半径</param>
        /// <param name="radiusY">Y轴半径</param>
        /// <param name="source">实体来源</param>
        /// <param name="parentId">父组件ID</param>
        public Ellipse(LitMath.Vector2 center, double radiusX, double radiusY, EntitySource source, ObjectId? parentId = null)
            : this(center, radiusX, radiusY)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            gd.DrawEllipse(_center, _radiusX, _radiusY);
        }

        /// <summary>
        /// ��¡����
        /// </summary>
        public override object Clone()
        {
            Ellipse ellipse = base.Clone() as Ellipse;
            ellipse._center = _center;
            ellipse._radiusX = _radiusX;
            ellipse._radiusY = _radiusY;
            return ellipse;
        }

        /// <summary>
        /// ������Բʵ��
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Ellipse();
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
            LitMath.Vector2 pntX = _center + new LitMath.Vector2(_radiusX, 0);
            LitMath.Vector2 pntY = _center + new LitMath.Vector2(0, _radiusY);

            _center = transform * _center;
            _radiusX = (transform * pntX - _center).length;
            _radiusY = (transform * pntY - _center).length;
        }

        /// <summary>
        /// ����׽��
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, _center));

            return snapPnts;
        }

        /// <summary>
        /// ��ȡ�е�
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.Center, _center));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(_radiusX, 0)));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(0, _radiusY)));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(-_radiusX, 0)));
            gripPnts.Add(new GripPoint(GripPointType.Quad, _center + new LitMath.Vector2(0, -_radiusY)));

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
                _radiusX = (newPosition - _center).length;
            }
        }
    }
}
