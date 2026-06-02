using System;
using System.Collections.Generic;
using System.Text;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// ��
    /// </summary>
    public class XPoint : Entity
    {
        /// <summary>
        /// ����
        /// </summary>
        public override string className
        {
            get { return "XPoint"; }
        }

        /// <summary>
        /// �յ�
        /// </summary>
        private LitMath.Vector2 _endPoint = new LitMath.Vector2();
        public LitMath.Vector2 endPoint
        {
            get { return _endPoint; }
            set { _endPoint = value; }
        }

        /// <summary>
        /// ��Χ�߿�
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                return new Bounding(_endPoint, 0, 0);
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public XPoint()
        {
        }

        public XPoint(LitMath.Vector2 endPnt)
        {
            _endPoint = endPnt;
        }

        /// <summary>
        /// ���ƺ���
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            gd.DrawPoint(_endPoint);
        }

        /// <summary>
        /// ��¡����
        /// </summary>
        public override object Clone()
        {
            XPoint xPoint = base.Clone() as XPoint;
            xPoint._endPoint = _endPoint;
            return xPoint;
        }

        protected override DBObject CreateInstance()
        {
            return new XPoint();
        }

        public override void Translate(LitMath.Vector2 translation)
        {
            _endPoint += translation;
        }
        public override void Rotate(LitMath.Vector2 center, double angle)
        {
            _endPoint = LitMath.Vector2.RotateInRadian(_endPoint, center, angle);
        }
        public override void TransformBy(LitMath.Matrix3 transform)
        {
            _endPoint = transform * _endPoint;
        }

        /// <summary>
        /// ����׽��
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, _endPoint));

            return snapPnts;
        }

        /// <summary>
        /// ��ȡ�е�
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.End, _endPoint));

            return gripPnts;
        }

        /// <summary>
        /// ���üе�
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            if (index == 0)
            {
                _endPoint = newPosition;
            }
        }

        /// <summary>
        /// дXML
        /// </summary>

        /// <summary>
        /// ��XML
        /// </summary>
    }
}
