using System;
using System.Collections.Generic;
using System.Linq;

namespace lcdb.Optic.Import
{
    /// <summary>
    /// Zemax光学系统数据模型
    /// </summary>
    public class ZmxSystemData
    {
        #region Properties

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 系统标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Zemax版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 系统模式 (SEQ = 序列, NSC = 非序列)
        /// </summary>
        public string Mode { get; set; } = "SEQ";

        /// <summary>
        /// 波长列表 (um)
        /// </summary>
        public List<double> Wavelengths { get; set; } = new List<double>();

        /// <summary>
        /// 系统孔径值
        /// </summary>
        public double SystemAperture { get; set; }

        /// <summary>
        /// 孔径类型 (EPD, F/#, NAO, etc.)
        /// </summary>
        public string ApertureType { get; set; }

        /// <summary>
        /// 表面列表
        /// </summary>
        public List<ZmxSurface> Surfaces { get; set; } = new List<ZmxSurface>();

        /// <summary>
        /// 解析警告
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 解析错误
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 原始未解析的行
        /// </summary>
        public List<string> UnparsedLines { get; set; } = new List<string>();

        /// <summary>
        /// 扩展系统数据
        /// </summary>
        public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Calculated Properties

        /// <summary>
        /// 解析是否成功 (无错误)
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// 光学表面数量 (不含OBJ和IMG)
        /// </summary>
        public int OpticalSurfaceCount => Surfaces.Count > 2 ? Surfaces.Count - 2 : 0;

        /// <summary>
        /// 获取所有元件 (连续的材料组)
        /// </summary>
        public List<ZmxElement> Elements => ExtractElements();

        /// <summary>
        /// 系统总长度 (OBJ到IMG的距离)
        /// </summary>
        public double TotalLength => Surfaces.Sum(s => s.Thickness);

        /// <summary>
        /// 光阑位置
        /// </summary>
        public int? StopSurfaceNumber => Surfaces.FirstOrDefault(s => s.IsStop)?.SurfaceNumber;

        #endregion

        #region Methods

        /// <summary>
        /// 提取光学元件 (每个元件由前后两个表面定义)
        /// </summary>
        private List<ZmxElement> ExtractElements()
        {
            var elements = new List<ZmxElement>();
            if (Surfaces.Count < 3) return elements;

            // 跳过OBJ面 (index 0)
            int i = 1;
            while (i < Surfaces.Count - 1) // 跳过IMG面
            {
                var surf = Surfaces[i];

                // 如果当前面有材料，则这是一个元件的开始
                if (surf.HasMaterial)
                {
                    var element = new ZmxElement
                    {
                        StartSurfaceIndex = i,
                        FrontSurface = surf.Clone(),
                        GlassMaterial = surf.Glass
                    };

                    // 查找元件的结束面 (下一个空气间隔或最后一个面)
                    i++;
                    while (i < Surfaces.Count - 1)
                    {
                        var nextSurf = Surfaces[i];
                        if (!nextSurf.HasMaterial)
                        {
                            // 找到了后表面
                            element.EndSurfaceIndex = i;
                            element.BackSurface = Surfaces[i - 1].Clone();
                            element.CenterThickness = element.FrontSurface.Thickness;
                            break;
                        }
                        i++;
                    }

                    // 如果没找到后表面，使用最后一个面
                    if (element.BackSurface == null && i > element.StartSurfaceIndex)
                    {
                        element.EndSurfaceIndex = i;
                        element.BackSurface = Surfaces[i - 1].Clone();
                        element.CenterThickness = element.FrontSurface.Thickness;
                    }

                    if (element.BackSurface != null)
                    {
                        elements.Add(element);
                    }
                }
                i++;
            }

            return elements;
        }

        /// <summary>
        /// 获取指定序号的表面
        /// </summary>
        public ZmxSurface GetSurface(int surfaceNumber)
        {
            return Surfaces.FirstOrDefault(s => s.SurfaceNumber == surfaceNumber);
        }

        /// <summary>
        /// 添加警告
        /// </summary>
        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(string message)
        {
            Errors.Add(message);
        }

        public override string ToString()
        {
            return $"ZmxSystem: {Title ?? FilePath}, {Surfaces.Count} surfaces, {Elements.Count} elements";
        }

        #endregion
    }

    /// <summary>
    /// Zemax光学元件数据 (前后表面组成的单个元件)
    /// </summary>
    public class ZmxElement
    {
        /// <summary>
        /// 前表面在Surfaces列表中的索引
        /// </summary>
        public int StartSurfaceIndex { get; set; }

        /// <summary>
        /// 后表面在Surfaces列表中的索引
        /// </summary>
        public int EndSurfaceIndex { get; set; }

        /// <summary>
        /// 前表面数据
        /// </summary>
        public ZmxSurface FrontSurface { get; set; }

        /// <summary>
        /// 后表面数据
        /// </summary>
        public ZmxSurface BackSurface { get; set; }

        /// <summary>
        /// 玻璃材料
        /// </summary>
        public string GlassMaterial { get; set; }

        /// <summary>
        /// 中心厚度
        /// </summary>
        public double CenterThickness { get; set; }

        #region Calculated Properties

        /// <summary>
        /// 前表面曲率半径 (R1)
        /// </summary>
        public double R1 => FrontSurface?.Radius ?? double.PositiveInfinity;

        /// <summary>
        /// 后表面曲率半径 (R2)
        /// </summary>
        public double R2 => BackSurface?.Radius ?? double.PositiveInfinity;

        /// <summary>
        /// 元件直径 (取较大的口径)
        /// </summary>
        public double Diameter => Math.Max(
            FrontSurface?.Diameter ?? 0,
            BackSurface?.Diameter ?? 0);

        /// <summary>
        /// 元件半口径
        /// </summary>
        public double SemiDiameter => Diameter / 2;

        /// <summary>
        /// 前表面边缘厚度
        /// </summary>
        public double FrontEdgeSag => FrontSurface?.EdgeSag ?? 0;

        /// <summary>
        /// 后表面边缘厚度
        /// </summary>
        public double BackEdgeSag => BackSurface?.EdgeSag ?? 0;

        /// <summary>
        /// 边缘厚度
        /// </summary>
        public double EdgeThickness => CenterThickness - FrontEdgeSag + BackEdgeSag;

        /// <summary>
        /// 是否为非球面元件
        /// </summary>
        public bool IsAspheric => (FrontSurface?.IsAspheric ?? false) ||
                                   (BackSurface?.IsAspheric ?? false);

        #endregion

        public override string ToString()
        {
            return $"Element: R1={R1:F3}, R2={R2:F3}, T={CenterThickness:F3}, Glass={GlassMaterial}, D={Diameter:F3}";
        }
    }
}
