using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;

namespace lcdb
{
    /// <summary>
    /// 表面类型枚举
    /// </summary>
    public enum SurfaceType
    {
        Standard,       // 标准面
        Spherical,      // 球面
        Aspherical,     // 非球面
        Flat,           // 平面
        Cylindrical,    // 柱面
        Toroidal,       // 环面
        Freeform,       // 自由曲面
        Binary,         // 二元光学面
        Binary2,        // 二元光学面2
        Diffractive     // 衍射面
    }

    /// <summary>
    /// 光学表面接口
    /// </summary>
    public interface IOpticalSurface
    {
        SurfaceType Type { get; }
        double Radius { get; set; }
        double Conic { get; set; }
        double[] AsphericalCoefficients { get; set; }
        Vector3 GetSurfaceNormal(Vector2 point);
        double GetSag(double radialDistance);
        Annotation.CoatingType CoatingType { get; set; }
        List<Vector2> GetCoatingMarkPoints();
    }

    /// <summary>
    /// 光学性能数据
    /// </summary>
    public class OpticalPerformanceData
    {
        public double FocalLength { get; set; }
        public double BackFocalLength { get; set; }
        public double EffectiveFocalLength { get; set; }
        public double NumericalAperture { get; set; }
        public double WorkingDistance { get; set; }
        public double FieldOfView { get; set; }
        public double Distortion { get; set; }
        public double ChromaticAberration { get; set; }
        public double SphericalAberration { get; set; }
        public double Coma { get; set; }
        public double Astigmatism { get; set; }
        public double FieldCurvature { get; set; }
        public double MTF { get; set; } // 调制传递函数
        public double FNumber { get; set; }
        public Vector3 PrincipalPoint { get; set; }
        public Vector3 NodalPoint { get; set; }
        public Dictionary<string, double> AberrationCoefficients { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public ValidationSeverity Severity { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<string> ErrorMessages => Errors.Select(e => e.Message).ToList();
        public List<string> WarningMessages => Warnings.Select(w => w.Message).ToList();

        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsValid = true,
                Severity = ValidationSeverity.None,
                ValidationTime = DateTime.Now
            };
        }

        public static ValidationResult Failure(string error)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError> { new ValidationError(error) },
                Severity = ValidationSeverity.Error,
                ValidationTime = DateTime.Now
            };
        }

        public void AddError(string message, string propertyName = null)
        {
            Errors.Add(new ValidationError(message, propertyName));
            IsValid = false;
            if (Severity < ValidationSeverity.Error)
                Severity = ValidationSeverity.Error;
        }

        public void AddWarning(string message, string propertyName = null)
        {
            Warnings.Add(new ValidationWarning(message, propertyName));
            if (Severity < ValidationSeverity.Warning)
                Severity = ValidationSeverity.Warning;
        }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string Message { get; set; }
        public string PropertyName { get; set; }
        public string ErrorCode { get; set; }

        public ValidationError(string message, string propertyName = null)
        {
            Message = message;
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        public string Message { get; set; }
        public string PropertyName { get; set; }
        public string WarningCode { get; set; }

        public ValidationWarning(string message, string propertyName = null)
        {
            Message = message;
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity
    {
        None,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 光线追迹类
    /// </summary>
    public class RayTrace
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }
        public double Wavelength { get; set; }
        public double Intensity { get; set; }
        public int MaxBounces { get; set; } = 10;
        public List<RaySegment> Path { get; set; } = new List<RaySegment>();

        public RayTrace(Vector3 origin, Vector3 direction, double wavelength = 550)
        {
            Origin = origin;
            Direction = direction.normalized;
            Wavelength = wavelength;
            Intensity = 1.0;
        }

        public void AddSegment(Vector3 start, Vector3 end, double intensity)
        {
            Path.Add(new RaySegment
            {
                Start = start,
                End = end,
                Intensity = intensity,
                SegmentIndex = Path.Count
            });
        }

        public RayTraceResult Trace(IOpticalElement element)
        {
            // 简化的光线追迹实现
            return new RayTraceResult
            {
                Success = true,
                FinalPosition = Origin + Direction * 100,
                FinalDirection = Direction,
                TotalPathLength = 100,
                Segments = Path
            };
        }
    }

    /// <summary>
    /// 光线段
    /// </summary>
    public class RaySegment
    {
        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }
        public double Intensity { get; set; }
        public int SegmentIndex { get; set; }
        public string MediumName { get; set; }
        public double RefractiveIndex { get; set; }
    }

    /// <summary>
    /// 光线追迹结果
    /// </summary>
    public class RayTraceResult
    {
        public bool Success { get; set; }
        public Vector3 FinalPosition { get; set; }
        public Vector3 FinalDirection { get; set; }
        public double TotalPathLength { get; set; }
        public double FinalIntensity { get; set; }
        public List<RaySegment> Segments { get; set; }
        public string ErrorMessage { get; set; }
    }
}