using System;
using System.Collections.Generic;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// ������
    /// </summary>
    public class Xline : Entity
    {
        /// <summary>
        /// ����
        /// </summary>
        public override string className
        {
            get { return "Xline"; }
        }

        /// <summary>
        /// ����
        /// </summary>
        private LitMath.Vector2 _basePoint = new LitMath.Vector2(0, 0);
        public LitMath.Vector2 basePoint
        {
            get { return _basePoint; }
            set { _basePoint = value; }
        }

        /// <summary>
        /// ����
        /// </summary>
        private LitMath.Vector2 _direction = new LitMath.Vector2(1, 0);
        public LitMath.Vector2 direction
        {
            get { return _direction; }
            set { _direction = value.normalized; }
        }

        /// <summary>
        /// ��Χ�߿�
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                return new Bounding(
                    new LitMath.Vector2(double.MinValue, double.MinValue),
                    new LitMath.Vector2(double.MaxValue, double.MaxValue));
            }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        public Xline()
        {
        }

        public Xline(LitMath.Vector2 basePoint, LitMath.Vector2 direction)
        {
            _basePoint = basePoint;
            _direction = direction;
        }

        /// <summary>
        /// ���ƺ���
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            gd.DrawXLine(_basePoint, _direction);
        }

        /// <summary>
        /// ��¡����
        /// </summary>
        public override object Clone()
        {
            Xline xline = base.Clone() as Xline;
            xline._basePoint = _basePoint;
            xline._direction = _direction;
            return xline;
        }

        protected override DBObject CreateInstance()
        {
            return new Xline();
        }

        /// <summary>
        /// ƽ��
        /// </summary>
        public override void Translate(LitMath.Vector2 translation)
        {
            _basePoint += translation;
        }
        public override void Rotate(LitMath.Vector2 center, double angle)
        {
            double ang = LitMath.Vector2.Angle(new LitMath.Vector2(0, 0), _direction);

            LitMath.Vector2 p1 = LitMath.Vector2.RotateInRadian(_basePoint, center, angle);
            LitMath.Vector2 p3 = LitMath.Vector2.Polar(new LitMath.Vector2(0, 0), 1, ang + angle);

            _basePoint = p1;
            _direction = p3.normalized;
        }
        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(LitMath.Matrix3 transform)
        {
            LitMath.Vector2 refPnt = _basePoint + _direction;
            _basePoint = transform * _basePoint;
            refPnt = transform * refPnt;
            _direction = (refPnt - _basePoint).normalized;
        }

        /// <summary>
        /// ����׽��
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            return null;
        }

        /// <summary>
        /// ��ȡ�е�
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.End, _basePoint));
            gripPnts.Add(new GripPoint(GripPointType.End, _basePoint + 10 * _direction));
            gripPnts.Add(new GripPoint(GripPointType.End, _basePoint - 10 * _direction));

            return gripPnts;
        }

        /// <summary>
        /// ���üе�
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            if (index == 0)
            {
                _basePoint = newPosition;
            }
            else if (index == 1 || index == 2)
            {
                LitMath.Vector2 dir = (newPosition - _basePoint).normalized;
                if (!dir.Equals(new LitMath.Vector2(0, 0)))
                {
                    _direction = dir;
                }
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
