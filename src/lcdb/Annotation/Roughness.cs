using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LitMath;

using OtoCAD;
namespace lcdb.Annotation
{

    public class Roughness : Entity
    {
         public override string className => "Roughness";

         /// <summary>
         /// 中心
         /// </summary>
         public LitMath.Vector2 Center = new LitMath.Vector2(0, 0);
         public LitMath.Vector2 ControlPoint = new LitMath.Vector2(0, 0);
         /// <summary>
         /// 边长
         /// </summary>
         public double EdgeLen = 4.0;

         // 3 lines
        public List<Line> lines = new List<Line>();


        public Text Text = null;
        /// <summary>
        /// 范围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                List<LitMath.Vector2> pnts = new List<LitMath.Vector2>();
                foreach (var l in lines)
                {
                    pnts.Add(l.startPoint);
                    pnts.Add(l.endPoint);
                }

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;
                foreach (LitMath.Vector2 pnt in pnts)
                {
                    minX = pnt.X < minX ? pnt.X : minX;
                    minY = pnt.Y < minY ? pnt.Y : minY;

                    maxX = pnt.X > maxX ? pnt.X : maxX;
                    maxY = pnt.Y > maxY ? pnt.Y : maxY;
                }

                return new Bounding(new LitMath.Vector2(minX, minY),
                    new LitMath.Vector2(maxX, maxY));
            }
        }

        public string AnnotationStyle { get; set; } = "Standard";
        public double FontSize { get; set; } = 2.0;


        protected void Generate()
        {
            lines.Clear();
            var drawx = ControlPoint.X - 10.0;
            if (ControlPoint.X < Center.X)
            {
                drawx = ControlPoint.X - 10.0;
            }
            else
            {
                drawx = ControlPoint.X + 10.0;
            }

            lines.AddRange(new Line[]
            {
                new Line(new LitMath.Vector2(Center.X, Center.Y),
                    new LitMath.Vector2(drawx, Center.Y)),
                new Line(new LitMath.Vector2(ControlPoint.X - EdgeLen * 0.5, Center.Y + EdgeLen * 0.866),
                    new LitMath.Vector2(ControlPoint.X, Center.Y)),
                new Line(new LitMath.Vector2(ControlPoint.X, Center.Y),
                    new LitMath.Vector2(ControlPoint.X+ EdgeLen , Center.Y  + EdgeLen * 1.732)),
                new Line(new LitMath.Vector2(ControlPoint.X- EdgeLen * 0.5, Center.Y + EdgeLen * 0.866),
                    new LitMath.Vector2(ControlPoint.X+ EdgeLen * 0.5, Center.Y + EdgeLen* 0.866)),
            });

            Text = new Text();
          
            Text.Value = "1.6";
            Text.Height = FontSize;
            Text.TextStyle = "Standard";
            Text.Position = new LitMath.Vector3(100, 100);
            Text.alignment = TextAlignment.CenterBottom;
            Text.Position = new LitMath.Vector3(ControlPoint.X, Center.Y + EdgeLen * 0.866);
            Text.angle = 0.0;

            
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Roughness()
        {
        }

    
        /// <summary>
        /// 绘制函数
        /// </summary>
        public override void Draw(IGraphicsDraw gd)
        {
            if (lines == null)
                return;

            Generate();

            foreach (var l in lines)
            {
                gd.DrawLine(l.startPoint, l.endPoint);
            }

            if (Text != null)
            {
                gd.DrawText(new LitMath.Vector2(Text.Position.X, Text.Position.Y), Text.Value, Text.Height, Text.TextStyle, (lcdb.TextAlignment)Text.alignment, Text.angle);
            }
          
         }
        protected override DBObject CreateInstance()
        {
            return new Roughness();
        }


        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            Roughness arc = base.Clone() as Roughness;
            arc.Center = Center;
            arc.EdgeLen = EdgeLen;
           
            if (lines != null)
            {
                arc.lines = new List<Line>();
                arc.lines.AddRange(lines);
            }

            return arc;
        }



        /// <summary>
        /// 平移
        /// </summary>
        public override void Translate(LitMath.Vector2 translation)
        {
            Center += translation;
        }
        public override void Rotate(LitMath.Vector2 center, double angle)
        {
           // _position = LitMath.Vector2.RotateInRadian(_position, center, angle);
        }
        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(LitMath.Matrix3 transform)
        {
            LitMath.Vector2 pnt = Center + new LitMath.Vector2(EdgeLen, 0);

            Center = transform * Center;
            EdgeLen = (transform * pnt - Center).length;
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPnts = new List<GripPoint>();
            gripPnts.Add(new GripPoint(GripPointType.Center, Center));
            gripPnts.Add(new GripPoint(GripPointType.End, ControlPoint));
            

            return gripPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            if (index == 0)
            {
                Center = newPosition;
            }
            else
            {
                ControlPoint = newPosition;
            }
        }
    }
}
