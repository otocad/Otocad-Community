using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;
using OtoCAD;
using lcdb;

namespace lcdb
{
    /// <summary>
    /// ??????????? (NURBS Non-Uniform Rational B-Splines)
    /// </summary>
    public class Spline : Entity
    {
        /// <summary>
        /// ????
        /// </summary>
        public override string className => "Spline";

        #region ??????

        private List<Vector2> _controlPoints = new List<Vector2>();
        private List<double> _weights = new List<double>();
        private List<double> _knots = new List<double>();
        private short _degree = 3;
        private bool _isClosed = false;
        private bool _isPeriodic = false;
        private List<Vector2> _fitPoints = new List<Vector2>();

        #endregion

        #region ????

        /// <summary>
        /// ??????��?
        /// </summary>
        public List<Vector2> ControlPoints
        {
            get { return _controlPoints; }
            set { _controlPoints = value ?? new List<Vector2>(); }
        }

        /// <summary>
        /// ????��?
        /// </summary>
        public List<double> Weights
        {
            get { return _weights; }
            set { _weights = value ?? new List<double>(); }
        }

        /// <summary>
        /// ??????
        /// </summary>
        public List<double> Knots
        {
            get { return _knots; }
            set { _knots = value ?? new List<double>(); }
        }

        /// <summary>
        /// ???????? (1-10)
        /// </summary>
        public short Degree
        {
            get { return _degree; }
            set 
            { 
                if (value < 1 || value > 10)
                    throw new ArgumentOutOfRangeException(nameof(value), "??????????????1-10???");
                _degree = value; 
            }
        }

        /// <summary>
        /// ?????
        /// </summary>
        public bool IsClosed
        {
            get { return _isClosed; }
            set { _isClosed = value; }
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public bool IsPeriodic
        {
            get { return _isPeriodic; }
            set { _isPeriodic = value; }
        }

        /// <summary>
        /// ?????��?
        /// </summary>
        public List<Vector2> FitPoints
        {
            get { return _fitPoints; }
            set { _fitPoints = value ?? new List<Vector2>(); }
        }

        /// <summary>
        /// ??��???
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_controlPoints == null || _controlPoints.Count == 0)
                    return new Bounding(Vector2.Zero, 0, 0);

                double minX = _controlPoints.Min(p => p.X);
                double maxX = _controlPoints.Max(p => p.X);
                double minY = _controlPoints.Min(p => p.Y);
                double maxY = _controlPoints.Max(p => p.Y);

                return new Bounding(
                    new Vector2((minX + maxX) / 2, (minY + maxY) / 2),
                    maxX - minX,
                    maxY - minY
                );
            }
        }

        #endregion

        #region ??????

        /// <summary>
        /// ????????
        /// </summary>
        public Spline()
        {
            InitializeDefault();
        }

        /// <summary>
        /// ??????????????????
        /// </summary>
        /// <param name="controlPoints">??????��?</param>
        public Spline(IEnumerable<Vector2> controlPoints)
            : this(controlPoints, null, 3, false)
        {
        }

        /// <summary>
        /// ???????????????????????
        /// </summary>
        /// <param name="controlPoints">??????��?</param>
        /// <param name="weights">????��?</param>
        public Spline(IEnumerable<Vector2> controlPoints, IEnumerable<double> weights)
            : this(controlPoints, weights, 3, false)
        {
        }

        /// <summary>
        /// ????????????????????????????
        /// </summary>
        /// <param name="controlPoints">控制点列表</param>
        /// <param name="weights">权重列表</param>
        /// <param name="degree">阶数</param>
        /// <param name="isClosed">?????</param>
        public Spline(IEnumerable<Vector2> controlPoints, IEnumerable<double> weights, short degree, bool isClosed)
        {
            // ???????
            if (controlPoints == null)
                throw new ArgumentNullException(nameof(controlPoints));

            var ctrlPts = controlPoints.ToList();
            if (ctrlPts.Count < 2)
                throw new ArgumentException($"???????????????????2????????{ctrlPts.Count}???????");

            if (degree < 1 || degree > 10)
                throw new ArgumentOutOfRangeException(nameof(degree), "??????????????1-10???");

            if (ctrlPts.Count < degree + 1)
            {
                degree = (short)(ctrlPts.Count - 1);
            }

            // ????????
            _controlPoints = ctrlPts;
            _degree = degree;
            _isClosed = isClosed;

            // ???????
            if (weights == null)
            {
                _weights = Enumerable.Repeat(1.0, _controlPoints.Count).ToList();
            }
            else
            {
                var weightList = weights.ToList();
                if (weightList.Count != _controlPoints.Count)
                    throw new ArgumentException("????????????????????????");
                _weights = weightList;
            }

            // ?????????
            GenerateKnotVector();
        }

        /// <summary>
        /// ?????????
        /// </summary>
        private void InitializeDefault()
        {
            _controlPoints = new List<Vector2>();
            _weights = new List<double>();
            _knots = new List<double>();
            _fitPoints = new List<Vector2>();
            _degree = 3;
            _isClosed = false;
            _isPeriodic = false;
        }

        /// <summary>
        /// ���캯�� - ָ�����Ƶ����Դ
        /// </summary>
        /// <param name="controlPoints">���Ƶ��б�</param>
        /// <param name="source">ʵ����Դ</param>
        /// <param name="parentId">�����ID</param>
        public Spline(IEnumerable<Vector2> controlPoints, EntitySource source, ObjectId? parentId = null)
            : this(controlPoints)
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        #endregion

        #region ???????????

        /// <summary>
        /// ???????
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (_controlPoints == null || _controlPoints.Count < 2)
                return;

            // ????????????3????????????
            if (_controlPoints.Count == 2)
            {
                gd.DrawLine(_controlPoints[0], _controlPoints[1]);
                return;
            }

            // ??????????????????
            var samplePoints = GenerateSamplePoints(50); // 50????????
            
            // ?????????????????????????
            for (int i = 0; i < samplePoints.Count - 1; i++)
            {
                gd.DrawLine(samplePoints[i], samplePoints[i + 1]);
            }

            // ????????????????????????
            if (_isClosed && samplePoints.Count > 2)
            {
                gd.DrawLine(samplePoints[samplePoints.Count - 1], samplePoints[0]);
            }
        }

        /// <summary>
        /// ???????
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new Spline();
        }

        /// <summary>
        /// ???????
        /// </summary>
        public override object Clone()
        {
            Spline spline = base.Clone() as Spline;
            spline._controlPoints = new List<Vector2>(_controlPoints);
            spline._weights = new List<double>(_weights);
            spline._knots = new List<double>(_knots);
            spline._fitPoints = new List<Vector2>(_fitPoints);
            spline._degree = _degree;
            spline._isClosed = _isClosed;
            spline._isPeriodic = _isPeriodic;
            return spline;
        }

        /// <summary>
        /// ???
        /// </summary>
        public override void Translate(Vector2 translation)
        {
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                _controlPoints[i] += translation;
            }
            
            for (int i = 0; i < _fitPoints.Count; i++)
            {
                _fitPoints[i] += translation;
            }
        }

        /// <summary>
        /// ???
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                _controlPoints[i] = Vector2.RotateInRadian(_controlPoints[i], center, angle);
            }
            
            for (int i = 0; i < _fitPoints.Count; i++)
            {
                _fitPoints[i] = Vector2.RotateInRadian(_fitPoints[i], center, angle);
            }
        }

        /// <summary>
        /// ????��
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                _controlPoints[i] = transform * _controlPoints[i];
            }
            
            for (int i = 0; i < _fitPoints.Count; i++)
            {
                _fitPoints[i] = transform * _fitPoints[i];
            }
        }

        #endregion

        #region ????????

        /// <summary>
        /// ????��?
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                gripPoints.Add(new GripPoint(GripPointType.Center, _controlPoints[i]));
            }
            
            return gripPoints;
        }

        /// <summary>
        /// ???????
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPoints = new List<ObjectSnapPoint>();
            
            foreach (var point in _controlPoints)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.Node, point));
            }
            
            if (_controlPoints.Count >= 2)
            {
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _controlPoints[0]));
                snapPoints.Add(new ObjectSnapPoint(ObjectSnapMode.End, _controlPoints[_controlPoints.Count - 1]));
            }
            
            return snapPoints;
        }

        /// <summary>
        /// ???����?
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            if (index >= 0 && index < _controlPoints.Count)
            {
                _controlPoints[index] = newPosition;
            }
        }

        #endregion

        #region XML???��?

        /// <summary>
        /// ��XML
        /// </summary>

        /// <summary>
        /// ??XML
        /// </summary>

        #endregion

        #region ????????

        /// <summary>
        /// ?????????
        /// </summary>
        private void GenerateKnotVector()
        {
            if (_controlPoints == null || _controlPoints.Count == 0)
                return;

            int n = _controlPoints.Count - 1; // ?????????????
            int m = n + _degree + 1; // ??????????

            _knots = new List<double>();

            // ????????????
            for (int i = 0; i <= m; i++)
            {
                double knot;
                if (i <= _degree)
                {
                    knot = 0.0;
                }
                else if (i >= n + 1)
                {
                    knot = 1.0;
                }
                else
                {
                    knot = (double)(i - _degree) / (n - _degree + 1);
                }
                _knots.Add(knot);
            }
        }

        /// <summary>
        /// ?????????????????
        /// </summary>
        /// <param name="sampleCount">??????????</param>
        /// <returns>???????��?</returns>
        private List<Vector2> GenerateSamplePoints(int sampleCount)
        {
            List<Vector2> samplePoints = new List<Vector2>();
            
            if (_controlPoints == null || _controlPoints.Count < 2)
                return samplePoints;

            // ????????????????????????
            // ???NURBS?????????????B??????????????
            for (int i = 0; i <= sampleCount; i++)
            {
                double t = (double)i / sampleCount;
                Vector2 point = EvaluateAt(t);
                samplePoints.Add(point);
            }
            
            return samplePoints;
        }

        /// <summary>
        /// ?????t???????????????
        /// </summary>
        /// <param name="t">????? (0-1)</param>
        /// <returns>????????</returns>
        private Vector2 EvaluateAt(double t)
        {
            if (_controlPoints == null || _controlPoints.Count == 0)
                return Vector2.Zero;

            if (_controlPoints.Count == 1)
                return _controlPoints[0];

            if (_controlPoints.Count == 2)
            {
                // ??????
                return _controlPoints[0] * (1 - t) + _controlPoints[1] * t;
            }

            // ????B??????????t??????????
            // ???????B??zier????????????????????????????NURBS??
            return EvaluateBezierAt(t);
        }

        /// <summary>
        /// ???B??zier??????????
        /// </summary>
        private Vector2 EvaluateBezierAt(double t)
        {
            // ???De Casteljau??????B??zier????
            var tempPoints = new List<Vector2>(_controlPoints);
            
            for (int r = 1; r < _controlPoints.Count; r++)
            {
                for (int i = 0; i < _controlPoints.Count - r; i++)
                {
                    tempPoints[i] = tempPoints[i] * (1 - t) + tempPoints[i + 1] * t;
                }
            }
            
            return tempPoints[0];
        }

        /// <summary>
        /// 添加控制点
        /// </summary>
        /// <param name="point">?????</param>
        public void AddControlPoint(Vector2 point)
        {
            _controlPoints.Add(point);
            _weights.Add(1.0);
            GenerateKnotVector();
        }

        /// <summary>
        /// ????????
        /// </summary>
        /// <param name="index">????��??</param>
        /// <param name="point">?????</param>
        public void InsertControlPoint(int index, Vector2 point)
        {
            if (index < 0 || index > _controlPoints.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _controlPoints.Insert(index, point);
            _weights.Insert(index, 1.0);
            GenerateKnotVector();
        }

        /// <summary>
        /// ????????
        /// </summary>
        /// <param name="index">??????????????</param>
        public void RemoveControlPoint(int index)
        {
            if (index < 0 || index >= _controlPoints.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_controlPoints.Count <= 2)
                throw new InvalidOperationException("???????????????2???????");

            _controlPoints.RemoveAt(index);
            _weights.RemoveAt(index);
            GenerateKnotVector();
        }

        #endregion
    }
}