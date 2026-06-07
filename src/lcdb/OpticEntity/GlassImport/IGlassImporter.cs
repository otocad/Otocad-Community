using System;
using System.Collections.Generic;
using System.IO;

namespace OtoCAD.OpticEntity.GlassImport
{
    /// <summary>
    /// 玻璃库导入器接口
    /// </summary>
    public interface IGlassImporter
    {
        /// <summary>
        /// 导入器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// 制造商名称
        /// </summary>
        string Manufacturer { get; }

        /// <summary>
        /// 检查文件是否可以被此导入器处理
        /// </summary>
        bool CanImport(string filePath);

        /// <summary>
        /// 从文件导入玻璃材料
        /// </summary>
        List<GlassMaterial> Import(string filePath);

        /// <summary>
        /// 从流导入玻璃材料
        /// </summary>
        List<GlassMaterial> Import(Stream stream);
    }

    /// <summary>
    /// 玻璃库导入器基类
    /// </summary>
    public abstract class GlassImporterBase : IGlassImporter
    {
        public abstract string Name { get; }
        public abstract string[] SupportedExtensions { get; }
        public abstract string Manufacturer { get; }

        public virtual bool CanImport(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLower();
            foreach (var supportedExt in SupportedExtensions)
            {
                if (extension == supportedExt.ToLower())
                    return true;
            }
            return false;
        }

        public virtual List<GlassMaterial> Import(string filePath)
        {
            if (!CanImport(filePath))
                throw new InvalidOperationException($"Cannot import file: {filePath}");

            using (var stream = File.OpenRead(filePath))
            {
                return Import(stream);
            }
        }

        public abstract List<GlassMaterial> Import(Stream stream);

        /// <summary>
        /// 解析色散公式计算折射率
        /// </summary>
        protected double CalculateRefractiveIndex(double wavelength, double[] coefficients, DispersionFormula formula)
        {
            double lambda = wavelength / 1000.0; // 转换为微米
            double lambda2 = lambda * lambda;
            double n2 = 0;

            switch (formula)
            {
                case DispersionFormula.Schott:
                    // n² = A0 + A1*λ² + A2/λ² + A3/λ⁴ + A4/λ⁶ + A5/λ⁸
                    n2 = coefficients[0] + coefficients[1] * lambda2;
                    n2 += coefficients[2] / lambda2;
                    n2 += coefficients[3] / (lambda2 * lambda2);
                    n2 += coefficients[4] / (lambda2 * lambda2 * lambda2);
                    n2 += coefficients[5] / (lambda2 * lambda2 * lambda2 * lambda2);
                    break;

                case DispersionFormula.Sellmeier:
                    // n² - 1 = B1*λ²/(λ² - C1) + B2*λ²/(λ² - C2) + B3*λ²/(λ² - C3)
                    n2 = 1.0;
                    for (int i = 0; i < 3; i++)
                    {
                        n2 += coefficients[i] * lambda2 / (lambda2 - coefficients[i + 3]);
                    }
                    break;

                case DispersionFormula.Conrady:
                    // n = A + B/λ + C/λ³·⁵
                    double n = coefficients[0] + coefficients[1] / lambda + coefficients[2] / Math.Pow(lambda, 3.5);
                    return n;
            }

            return Math.Sqrt(n2);
        }
    }

    /// <summary>
    /// 色散公式类型
    /// </summary>
    public enum DispersionFormula
    {
        Schott,
        Sellmeier,
        Conrady,
        Laurent,
        Handbook
    }
}