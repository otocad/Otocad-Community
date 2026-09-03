using lcdb;
using OtoCAD.OpticEntity;
using System;
using System.Collections.Generic;
using netDxf.Entities;
using netDxf;
using System.ComponentModel;
using LitMath;

namespace OtoCAD.OpticEntity
{
    public class SurfaceBinary2 : IElementSurface
    {
        [Category("Zemax")]
        [DisplayName("面1序号")]
        public int surface1No { get; set; } = 2;

        [Category("Zemax")]
        [DisplayName("面2序号")]
        public int surface2No { get; set; } = 3;

        public SurfaceType SurfaceType => SurfaceType.Binary2;

        public string Name { get; set; }

        public double Radius { get; set; } = 59.93;
        public double SemiDiameter { get; set; } = 42.0;
        public double Thickness { get; set; } // To next surface

        public double Conic { get; set; }
        public double[] Par { get; set; }
        public double UniRadius { get; set; }
        public int MaxParInt { get; set; }
        public List<double> ExtraDoubles { get; set; }
        public double Sag { get; set; }
        public event DataUpdateEvent OnDataUpdate;
        public SurfaceParameter SurfaceParameter { get; set; }
        public SurfaceBinary2()
        {
            Par = new double[8];
            ExtraDoubles = new List<double>();
        }

        public double[] GetSag(double[] x)
        {
            double[] y = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                y[i] = GetSag1(x[i]);
            }

            return y;
        }

        public double GetSag1(double x)
        {
            double C = 1.0 / Radius;
            double H = x;

            double Z1 = C * H * H / (1.0 + Math.Sqrt(1.0 - (1.0 + Conic) * C * C * H * H));
            double Zrest = 0.0;
            for (int i = 0; i < 8; i++)
            {
                Zrest += Par[i] * Math.Pow(H, (i + 1) * 2);
            }

            return Z1 + Zrest;
        }
        private Polyline polyline;
        public Entity ProfileEntity => polyline;

        public LitMath.Vector2 SagPoint { get; set; }

        public double RealDiameter { get; set; }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public LitMath.Vector2 BasePoint1 { get; set; }

        public void GenEntity()
        {
            SemiDiameter = Math.Abs(SemiDiameter);

            GetSurfacePar(Radius, SemiDiameter, out double RealDia, out double signedSag, out double AngStart,
                 out double AngEnd);

            RealDiameter = RealDia;


            var OriginalX = BasePoint1.X;
            var OriginalY = BasePoint1.Y;

            SagPoint = new LitMath.Vector2(signedSag + OriginalX, RealDiameter);





            if (SurfaceParameter != null)
            {
                double Dia = SemiDiameter;
                double step = 0.1;
                int N = (int)(Dia / step) + 1;
                double[] FroceSag2X = new double[(int)(Dia / step) + 1];
                for (int i = 0; i < N - 1; i++)
                {
                    FroceSag2X[i] = i * step;
                }
                FroceSag2X[N - 1] = Dia;

                double[] FroceSag2Y = SurfaceParameter.GetSag(FroceSag2X);

                // profile arc
                Polyline2D polyline2D = polyline.Polyline2D;
                if (polyline2D == null)
                    polyline2D = new netDxf.Entities.Polyline2D();


                for (var index = FroceSag2X.Length - 1; index > 0; index--)
                {
                    var x = FroceSag2X[index];
                    var y = FroceSag2Y[index];
                    polyline2D.Vertexes.Add(new Polyline2DVertex(y + Thickness + OriginalX, -x + OriginalY));
                }

                for (var index = 0; index < FroceSag2X.Length; index++)
                {
                    var x = FroceSag2X[index];
                    var y = FroceSag2Y[index];
                    polyline2D.Vertexes.Add(new Polyline2DVertex(y + Thickness + OriginalX, x + OriginalY));
                }           
            }

        }


        public void GetSurfacePar(double SignedR1, double SemiDiameter1, out double RealDia, out double UnsigndSag, out double AngStart, out double AngEnd)
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
