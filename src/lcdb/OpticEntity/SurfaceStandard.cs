using lcdb;
using OtoCAD.OpticEntity;
using System;
using netDxf.Entities;
using netDxf;
using System.ComponentModel;
using LitMath;

namespace OtoCAD.OpticEntity
{
    public class SurfaceStandard : IElementSurface
    {


        [Category("Zemax")]
        [DisplayName("Zemax面序号")]
        public int ZemaxSurfaceNo { get; set; } = 3;

        public bool OverrideFromZemax { get; set; } = false;

        [ReadOnly(true)]
        public SurfaceType SurfaceType => SurfaceType.Standard;
        [ReadOnly(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public LitMath.Vector2 BasePoint1 { get; set; }
        public string Name { get; set; }

        public double Radius { get; set; } = 59.93;
        public double SemiDiameter { get; set; } = 42.0;
        [ReadOnly(true)]
        public double RealDiameter { get; set; }
        public double Thickness { get; set; } = 9.0;// To next surface
        public string Glass { get; set; } = "BK7"; // 添加玻璃材料属性
        private lcdb.Arc arc;
        [Browsable(false)]
        public Entity ProfileEntity => arc;
        [ReadOnly(true)]
        public LitMath.Vector2 SagPoint { get; set; }
        public event DataUpdateEvent OnDataUpdate;


        public SurfaceStandard() { }
        public void GenEntity()
        {
            SemiDiameter = Math.Abs(SemiDiameter);

            GetSurfacePar(Radius, SemiDiameter, out double RealDia, out double signedSag, out double AngStart,
                out double AngEnd);

            RealDiameter = RealDia;

            var OriginalX = BasePoint1.X;
            var OriginalY = BasePoint1.Y;

            SagPoint = new LitMath.Vector2(signedSag + OriginalX, RealDiameter + OriginalY);

            // profile arc
            arc = new lcdb.Arc();
            arc.radius = Math.Abs(Radius);
            arc.center = new LitMath.Vector2(Radius + OriginalX, OriginalY);
            arc.startAngle = AngStart;
            arc.endAngle = AngEnd;
        }
        private void GetSurfacePar(double SignedR1, double SemiDiameter1, out double RealDia, out double UnsigndSag, out double AngStart, out double AngEnd)
        {

            UnsigndSag = 0.0;
            RealDia = SemiDiameter1;
            AngStart = 0.0;
            AngEnd = 0.0;

            var anglea = 0.0;
            if (SemiDiameter1 < Math.Abs(SignedR1))
            {
                anglea = Math.Asin(SemiDiameter1 / SignedR1);
                UnsigndSag = Math.Abs(SignedR1) - Math.Sqrt(SignedR1 * SignedR1 - SemiDiameter1 * SemiDiameter1);
            }
            else
            {
                // super sphere
                RealDia = Math.Abs(SignedR1);
                anglea = SignedR1 < 0 ? -Math.PI / 2.0 : Math.PI / 2.0;
                UnsigndSag = Math.Abs(SignedR1);
            }


            if (SignedR1 > 0.0)
            {
                AngStart = Math.PI - anglea;
                AngEnd = Math.PI + anglea;

            }
            else
            {

                AngStart = anglea;
                AngEnd = -anglea;
                UnsigndSag = -UnsigndSag;
            }


        }

       
    }

}
