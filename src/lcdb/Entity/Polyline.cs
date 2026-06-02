using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;
using LitMath;
using netDxf;

using OtoCAD;
namespace lcdb
{
    /// <summary>
    /// �����
    /// </summary>
    public class Polyline : Entity
    {
        /// <summary>
        /// ����
        /// </summary>
        public override string className
        {
            get { return "Polyline"; }
        }

        [JsonIgnore]
        public netDxf.Entities.Polyline2D Polyline2D { get; set; } = new netDxf.Entities.Polyline2D();

        private List<LitMath.Vector2b> _vertices = new List<LitMath.Vector2b>();
        private bool _closed = false;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Polyline()
        {
        }

        /// <summary>
        /// 构造函数 - 指定来源
        /// </summary>
        /// <param name="source">实体来源</param>
        /// <param name="parentId">父组件ID</param>
        public Polyline(EntitySource source, ObjectId? parentId = null)
            : this()
        {
            this.Source = source;
            this.ParentComponentId = parentId;
        }

        public int NumberOfVertices => _vertices.Count;
        public List<LitMath.Vector2b> Vertices
        {
            get { return _vertices; }
            set { _vertices = value ?? new List<LitMath.Vector2b>(); }
        }

        /// <summary>
        /// �Ƿ�պ�
        /// </summary>
        public bool closed
        {
            get { return _closed; }
            set { _closed = value; }
        }

        /// <summary>
        /// ���ƺ���
        /// </summary>
        //public override void Draw(IGraphicsDraw gd)
        //{
        //    int numOfVertices = NumberOfVertices;
        //    for (int i = 0; i < numOfVertices - 1; ++i)
        //    {
        //        gd.DrawLine(GetPointAt(i), GetPointAt(i + 1));
        //    }

        //    if (closed
        //        && numOfVertices > 2)
        //    {
        //        gd.DrawLine(GetPointAt(numOfVertices - 1), GetPointAt(0));
        //    }
        //}
        public override void Draw(IGraphicsDraw gd)
        {
            int numOfVertices = NumberOfVertices;
            System.Diagnostics.Debug.WriteLine($"[Polyline.Draw] 开始绘制, 顶点数: {numOfVertices}, 颜色: {this.color.colorMethod}");
            for (int i = 0; i < numOfVertices - 1; i++)
            {
                var vt = _vertices[i];
                if (vt.B == 0.0)
                {
                    var p1 = new netDxf.Vector2(vt.X, vt.Y);
                    var p2 = new netDxf.Vector2(_vertices[i + 1].X, _vertices[i + 1].Y);
                    System.Diagnostics.Debug.WriteLine($"[Polyline.Draw] 绘制线段 {i}: ({p1.X},{p1.Y}) -> ({p2.X},{p2.Y})");
                    gd.DrawLine(new LitMath.Vector2(p1.X, p1.Y), new LitMath.Vector2(p2.X, p2.Y));
                }
                else
                {
                    var p1 = new netDxf.Vector2(vt.X, vt.Y);
                    var p2 = new netDxf.Vector2(_vertices[i + 1].X, _vertices[i + 1].Y);
                    var bulge = vt.B;
                    
                    double length = netDxf.Vector2.Distance(p1, p2);
                    double alpha = 4 * System.Math.Atan(bulge); // full angle
                    double radius = length / (2.0 * System.Math.Abs(System.Math.Sin(alpha * 0.5d)));
                    double bulgeSign = System.Math.Sign(bulge);
                    var delta = p2 - p1;
                    length = delta.Modulus();
                    var lnormalized = delta;
                    lnormalized.Normalize();
                    var lnormal = new netDxf.Vector2(-lnormalized.Y, lnormalized.X) * bulgeSign;
                    var arcCenter = ((p1 + p2) * 0.5) + lnormal * System.Math.Cos(alpha * 0.5d) * radius;

                    double startAngle = netDxf.Vector2.Angle(arcCenter, p1);
   
                    double endAngle = netDxf.Vector2.Angle(arcCenter, p2);
      
                    if (bulgeSign == 1)
                        gd.DrawArc(new LitMath.Vector2(arcCenter.X, arcCenter.Y), radius, startAngle*MathHelper.DegToRad, endAngle * MathHelper.DegToRad);
                    else
                        gd.DrawArc(new LitMath.Vector2(arcCenter.X, arcCenter.Y), radius, endAngle * MathHelper.DegToRad, startAngle * MathHelper.DegToRad);

                }

            }
            
            if (closed && numOfVertices > 2)
            {
                var vt = _vertices[numOfVertices - 1];
                if (vt.B == 0.0)
                {
                    var p1 = new LitMath.Vector2(vt.X, vt.Y);
                    var p2 = new LitMath.Vector2(_vertices[0].X, _vertices[0].Y);
                    gd.DrawLine(p1, p2);
                }
                else
                {
                    // 处理闭合时的圆弧
                    var p1 = new netDxf.Vector2(vt.X, vt.Y);
                    var p2 = new netDxf.Vector2(_vertices[0].X, _vertices[0].Y);
                    var bulge = vt.B;
                    
                    double length = netDxf.Vector2.Distance(p1, p2);
                    double alpha = 4 * System.Math.Atan(bulge);
                    double radius = length / (2.0 * System.Math.Abs(System.Math.Sin(alpha * 0.5d)));
                    double bulgeSign = System.Math.Sign(bulge);
                    var delta = p2 - p1;
                    var lnormalized = delta;
                    lnormalized.Normalize();
                    var lnormal = new netDxf.Vector2(-lnormalized.Y, lnormalized.X) * bulgeSign;
                    var arcCenter = ((p1 + p2) * 0.5) + lnormal * System.Math.Cos(alpha * 0.5d) * radius;

                    double startAngle = netDxf.Vector2.Angle(arcCenter, p1);
                    double endAngle = netDxf.Vector2.Angle(arcCenter, p2);
                    
                    if (bulgeSign == 1)
                        gd.DrawArc(new LitMath.Vector2(arcCenter.X, arcCenter.Y), radius, startAngle * MathHelper.DegToRad, endAngle * MathHelper.DegToRad);
                    else
                        gd.DrawArc(new LitMath.Vector2(arcCenter.X, arcCenter.Y), radius, endAngle * MathHelper.DegToRad, startAngle * MathHelper.DegToRad);
                }
            }
        }
        public void AddVertexAt(int index, LitMath.Vector2b point)
        {
            _vertices.Insert(index, point);
            Polyline2D.Vertexes.Insert(index, new netDxf.Entities.Polyline2DVertex(point.X, point.Y) { Bulge = point.B });
        }

        public void AddVertexAt(int index, LitMath.Vector2 point)
        {
            _vertices.Insert(index, new Vector2b(point.X, point.Y, 0));
            Polyline2D.Vertexes.Insert(index, new netDxf.Entities.Polyline2DVertex(point.X, point.Y));
        }

        public void RemoveVertexAt(int index)
        {
            _vertices.RemoveAt(index);
            if (index < Polyline2D.Vertexes.Count)
            {
                Polyline2D.Vertexes.RemoveAt(index);
            }
        }

        public LitMath.Vector2b GetPointAt(int index)
        {
            return _vertices[index];
        }
        public LitMath.Vector2 Get2PointAt(int index)
        {
            var point = _vertices[index];
            return new LitMath.Vector2(point.X, point.Y);
        }
        public void SetPointAt(int index, LitMath.Vector2b point)
        {
            _vertices[index] = point;
        }
        public void SetPointAt(int index, LitMath.Vector2 point)
        {
            _vertices[index] = new Vector2b(point.X, point.Y, 0);
        }

        public void SetBulgeAt(int index, double bulge)
        {
            _vertices[index] = new Vector2b(_vertices[index].X, _vertices[index].Y, bulge);
            if (index < Polyline2D.Vertexes.Count)
            {
                Polyline2D.Vertexes[index].Bulge = bulge;
            }
        }

        /// <summary>
        /// ���Ӷ��㣨֧��Vector2��bulge��
        /// </summary>
        public void AddVertexAt(LitMath.Vector2 point, double bulge = 0.0)
        {
            _vertices.Add(new Vector2b(point.X, point.Y, bulge));
            Polyline2D.Vertexes.Add(new netDxf.Entities.Polyline2DVertex(point.X, point.Y) { Bulge = bulge });
        }

        /// <summary>
        /// ��ȡ���ж��㣨������bulge��
        /// </summary>
        public IEnumerable<LitMath.Vector2> GetVertices()
        {
            foreach (var vertex in _vertices)
            {
                yield return new LitMath.Vector2(vertex.X, vertex.Y);
            }
        }

        /// <summary>
        /// ��ȡ����bulgeֵ
        /// </summary>
        public IEnumerable<double> GetBulges()
        {
            foreach (var vertex in _vertices)
            {
                yield return vertex.B;
            }
        }

        /// <summary>
        /// �Ƿ�պϣ����Ա������������д��룩
        /// </summary>
        public bool IsClosed
        {
            get { return _closed; }
            set { _closed = value; }
        }

        /// <summary>
        /// ��Χ�߿�
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                if (_vertices.Count > 0)
                {
                    double minX = double.MaxValue;
                    double minY = double.MaxValue;
                    double maxX = double.MinValue;
                    double maxY = double.MinValue;

                    foreach (LitMath.Vector2b point in _vertices)
                    {
                        minX = point.X < minX ? point.X : minX;
                        minY = point.Y < minY ? point.Y : minY;

                        maxX = point.X > maxX ? point.X : maxX;
                        maxY = point.Y > maxY ? point.Y : maxY;
                    }

                    return new Bounding(new LitMath.Vector2(minX, minY), new LitMath.Vector2(maxX, maxY));
                }
                else
                {
                    return new Bounding();
                }
            }
        }

        /// <summary>
        /// ��¡����
        /// </summary>
        public override object Clone()
        {
            Polyline polyline = base.Clone() as Polyline;
            polyline._vertices.AddRange(_vertices);
            polyline._closed = _closed;

            return polyline;
        }

        protected override DBObject CreateInstance()
        {
            return new Polyline();
        }

        /// <summary>
        /// ƽ��
        /// </summary>
        public override void Translate(LitMath.Vector2 translation)
        {
            for (int i = 0; i < this.NumberOfVertices; ++i)
            {
                LitMath.Vector2b p1 = new LitMath.Vector2b(_vertices[i].X + translation.X, _vertices[i].Y + translation.Y, _vertices[i].B);
                _vertices[i] = p1;
            }
        }

        public override void Rotate(LitMath.Vector2 center, double angle)
        {
            for (int i = 0; i < this.NumberOfVertices; ++i)
            {
                LitMath.Vector2b p1 = LitMath.Vector2b.RotateInRadian(_vertices[i], center, angle);
                p1.B = _vertices[i].B;

                _vertices[i] = p1;
            }

        }

        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(LitMath.Matrix3 transform)
        {
            for (int i = 0; i < this.NumberOfVertices; ++i)
            {
                LitMath.Vector2 p1 = new LitMath.Vector2(_vertices[i].X, _vertices[i].Y);
                LitMath.Vector2 p2 = transform * p1;
                LitMath.Vector2b p3 = new LitMath.Vector2b(p2.X, p2.Y, _vertices[i].B);
                _vertices[i] = p3;

                //_vertices[i] = transform * _vertices[i];
            }
        }

        /// <summary>
        /// ����׽��
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            int numOfVertices = this.NumberOfVertices;
            for (int i = 0; i < numOfVertices; ++i)
            {
                LitMath.Vector2b p1 = GetPointAt(i);

                snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.End, new LitMath.Vector2(p1.X, p1.Y)));

                if (i < numOfVertices - 1)
                {
                    LitMath.Vector2b p2 = GetPointAt(i + 1);

                    snapPnts.Add(
                        new ObjectSnapPoint(ObjectSnapMode.Mid, (new LitMath.Vector2(p1.X, p1.Y) + new LitMath.Vector2(p2.X, p2.Y)) / 2));
                }

                
            }

            //if (_closed && numOfVertices > 1)
            //{
            //    snapPnts.Add(
            //        new ObjectSnapPoint(ObjectSnapMode.Mid, (GetPointAt(0) + GetPointAt(numOfVertices - 1)) / 2));
            //}

            return snapPnts;
        }

        /// <summary>
        /// ��ȡ�е�
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            int numOfVertices = NumberOfVertices;
            for (int i = 0; i < numOfVertices; ++i)
            {
                LitMath.Vector2b p1 = _vertices[i];

                gripPnts.Add(new GripPoint(GripPointType.End, new LitMath.Vector2(p1.X, p1.Y)));
            }
            for (int i = 0; i < numOfVertices - 1; ++i)
            {
                LitMath.Vector2b p1 = _vertices[i];
                LitMath.Vector2b p2 = _vertices[i + 1];

                GripPoint midGripPnt = new GripPoint(GripPointType.Mid, (new LitMath.Vector2(p1.X, p1.Y) + new LitMath.Vector2(p2.X, p2.Y)) / 2);
                midGripPnt.xData1 = _vertices[i];
                midGripPnt.xData2 = _vertices[i + 1];
                gripPnts.Add(midGripPnt);
            }
            if (_closed && numOfVertices > 2)
            {
                LitMath.Vector2b p1 = _vertices[0];
                LitMath.Vector2b p2 = _vertices[numOfVertices - 1];

                GripPoint midGripPnt = new GripPoint(GripPointType.Mid, (new LitMath.Vector2(p1.X, p1.Y) + new LitMath.Vector2(p2.X, p2.Y)) / 2);
                midGripPnt.xData1 = _vertices[0];
                midGripPnt.xData2 = _vertices[numOfVertices - 1];
                gripPnts.Add(midGripPnt);
            }

            return gripPnts;
        }

        /// <summary>
        /// ���üе�
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            switch (gripPoint.type)
            {
                case GripPointType.End:
                    {
                        this.SetPointAt(index, new Vector2b(newPosition.X, newPosition.Y, 0));
                    }
                    break;

                case GripPointType.Mid:
                    {
                        int numOfVertices = NumberOfVertices;
                        int i = index - numOfVertices;
                        if (i >= 0 && i <= numOfVertices-1)
                        {
                            int vIndex1st = i;
                            int vIndex2nd = i + 1;
                            if (vIndex2nd == numOfVertices)
                            {
                                vIndex2nd = 0;
                            }
                            LitMath.Vector2b t = new Vector2b(newPosition.X, newPosition.Y, 0) - new Vector2b(gripPoint.position.X, gripPoint.position.Y, 0);
                            this.SetPointAt(vIndex1st, (LitMath.Vector2b)gripPoint.xData1 + t);
                            this.SetPointAt(vIndex2nd, (LitMath.Vector2b)gripPoint.xData2 + t);
                        }
                    }
                    break;

                default:
                    break;
            }
        }
        public double GetBulgeAt(int index)
        {
            return _vertices[index].B;
        }
        /// <summary>
        /// дXML
        /// </summary>

        /// <summary>
        /// ��XML
        /// </summary>
    }
}
