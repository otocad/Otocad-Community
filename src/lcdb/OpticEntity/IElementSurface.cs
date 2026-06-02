using lcdb;
using OtoCAD.OpticEntity;
using System.ComponentModel;
using System.Text.Json.Serialization;
using LitMath;

namespace OtoCAD.OpticEntity
{
    public  interface IElementSurface : IEditableProperty
    {
        SurfaceType SurfaceType { get;  }
         double Radius { get; set; }
        double Thickness { get; set; }
        Vector2 SagPoint { get; }
        double RealDiameter { get; set; }
        double SemiDiameter { get; set; }

        new void  GenEntity();

        new event DataUpdateEvent OnDataUpdate;
        Entity ProfileEntity { get;  }
        [TypeConverter(typeof(ExpandableObjectConverter))]
        Vector2 BasePoint1 { get; set; }


    }

}
