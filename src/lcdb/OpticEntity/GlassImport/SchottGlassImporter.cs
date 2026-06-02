using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OtoCAD.OpticEntity;

namespace OtoCAD.OpticEntity.GlassImport
{
    /// <summary>
    /// 肖特（Schott）玻璃库导入器
    /// 支持 .agf (Zemax格式) 和 .xml 格式
    /// </summary>
    public class SchottGlassImporter : GlassImporterBase
    {
        public override string Name => "肖特玻璃库导入器";
        public override string[] SupportedExtensions => new[] { ".agf", ".xml", ".sch" };
        public override string Manufacturer => "Schott";

        public override List<GlassMaterial> Import(Stream stream)
        {
            var materials = new List<GlassMaterial>();

            // 检测文件格式
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                var firstLine = reader.ReadLine();
                stream.Position = 0; // 重置流位置

                if (firstLine != null && firstLine.IndexOf("<?xml", StringComparison.Ordinal) >= 0)
                {
                    return ImportFromXml(stream);
                }
                else
                {
                    return ImportFromAgf(stream);
                }
            }
        }

        /// <summary>
        /// 从AGF格式导入
        /// </summary>
        private List<GlassMaterial> ImportFromAgf(Stream stream)
        {
            var materials = new List<GlassMaterial>();

            using (var reader = new StreamReader(stream, Encoding.Default))
            {
                string line;
                GlassMaterial currentMaterial = null;
                List<double> coefficients = new List<double>();
                DispersionFormula formula = DispersionFormula.Sellmeier;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // 解析玻璃名称行
                    if (line.StartsWith("NM "))
                    {
                        if (currentMaterial != null && coefficients.Count > 0)
                        {
                            // 计算标准波长的折射率
                            CalculateStandardIndices(currentMaterial, coefficients.ToArray(), formula);
                            materials.Add(currentMaterial);
                        }

                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            currentMaterial = new GlassMaterial
                            {
                                Name = parts[1],
                                Code = parts[1],
                                Manufacturer = Manufacturer,
                                Type = DetermineGlassType(parts[1])
                            };
                            
                            // 解析阿贝数和折射率（如果提供）
                            if (parts.Length >= 4)
                            {
                                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double nd);
                                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double vd);
                                currentMaterial.Nd = nd;
                            }
                            coefficients.Clear();
                        }
                    }
                    // 解析色散公式类型
                    else if (line.StartsWith("FD ") && currentMaterial != null)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            int formulaType;
                            if (int.TryParse(parts[1], out formulaType))
                            {
                                // Schott 通常使用 Sellmeier 公式
                                formula = formulaType == 2 ? DispersionFormula.Sellmeier : DispersionFormula.Schott;
                            }
                        }
                    }
                    // 解析色散系数
                    else if (line.StartsWith("CD ") && currentMaterial != null)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 1; i < parts.Length; i++)
                        {
                            double coeff;
                            if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out coeff))
                            {
                                coefficients.Add(coeff);
                            }
                        }
                    }
                    // 解析热学数据
                    else if (line.StartsWith("TD ") && currentMaterial != null)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 7)
                        {
                            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double thermalExpansion);
                            double.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out double density);
                            currentMaterial.ThermalExpansion = thermalExpansion;
                            currentMaterial.Density = density;
                        }
                    }
                    // 解析机械数据
                    else if (line.StartsWith("MD ") && currentMaterial != null)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double youngsModulus);
                            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double poissonRatio);
                            currentMaterial.YoungsModulus = youngsModulus;
                            currentMaterial.PoissonRatio = poissonRatio;
                        }
                    }
                    // 解析化学耐久性
                    else if (line.StartsWith("CE ") && currentMaterial != null)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            int.TryParse(parts[1], out int waterResistance);
                            int.TryParse(parts[2], out int acidResistance);
                            int.TryParse(parts[3], out int alkaliResistance);
                            currentMaterial.WaterResistance = waterResistance;
                            currentMaterial.AcidResistance = acidResistance;
                            currentMaterial.AlkaliResistance = alkaliResistance;
                        }
                    }
                }

                // 添加最后一个材料
                if (currentMaterial != null && coefficients.Count > 0)
                {
                    CalculateStandardIndices(currentMaterial, coefficients.ToArray(), formula);
                    materials.Add(currentMaterial);
                }
            }

            return materials;
        }

        /// <summary>
        /// 从XML格式导入（Schott的官方格式）
        /// </summary>
        private List<GlassMaterial> ImportFromXml(Stream stream)
        {
            var materials = new List<GlassMaterial>();

            try
            {
                var doc = XDocument.Load(stream);
                var glassElements = doc.Descendants("glass");

                foreach (var glassElement in glassElements)
                {
                    var material = new GlassMaterial
                    {
                        Manufacturer = Manufacturer
                    };

                    // 基本信息
                    material.Code = glassElement.Attribute("name")?.Value ?? "";
                    material.Name = material.Code;
                    material.Type = DetermineGlassType(material.Code);

                    // 折射率数据
                    var refractiveIndex = glassElement.Element("refractive_index");
                    if (refractiveIndex != null)
                    {
                        material.Nd = ParseDouble(refractiveIndex.Element("nd")?.Value);
                        material.NF = ParseDouble(refractiveIndex.Element("nF")?.Value);
                        material.NC = ParseDouble(refractiveIndex.Element("nC")?.Value);
                    }

                    // 物理属性
                    var properties = glassElement.Element("properties");
                    if (properties != null)
                    {
                        material.Density = ParseDouble(properties.Element("density")?.Value);
                        material.ThermalExpansion = ParseDouble(properties.Element("thermal_expansion")?.Value);
                        material.YoungsModulus = ParseDouble(properties.Element("youngs_modulus")?.Value);
                        material.PoissonRatio = ParseDouble(properties.Element("poisson_ratio")?.Value);
                    }

                    // 化学耐久性
                    var durability = glassElement.Element("chemical_durability");
                    if (durability != null)
                    {
                        material.WaterResistance = ParseInt(durability.Element("water_resistance")?.Value);
                        material.AcidResistance = ParseInt(durability.Element("acid_resistance")?.Value);
                        material.AlkaliResistance = ParseInt(durability.Element("alkali_resistance")?.Value);
                    }

                    materials.Add(material);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse Schott XML format: {ex.Message}", ex);
            }

            return materials;
        }

        /// <summary>
        /// 计算标准波长的折射率
        /// </summary>
        private void CalculateStandardIndices(GlassMaterial material, double[] coefficients, DispersionFormula formula)
        {
            // 如果已经有nd值，检查是否需要计算其他值
            if (material.Nd > 0 && material.NF == 0)
            {
                // d光 (587.56nm)
                if (material.Nd == 0)
                    material.Nd = CalculateRefractiveIndex(587.56, coefficients, formula);
                // F光 (486.13nm)
                material.NF = CalculateRefractiveIndex(486.13, coefficients, formula);
                // C光 (656.27nm)
                material.NC = CalculateRefractiveIndex(656.27, coefficients, formula);
            }
            else if (material.Nd == 0)
            {
                // 计算所有值
                material.Nd = CalculateRefractiveIndex(587.56, coefficients, formula);
                material.NF = CalculateRefractiveIndex(486.13, coefficients, formula);
                material.NC = CalculateRefractiveIndex(656.27, coefficients, formula);
            }
        }

        /// <summary>
        /// 根据玻璃名称判断类型
        /// </summary>
        private GlassType DetermineGlassType(string glassName)
        {
            glassName = glassName.ToUpper();

            // Schott 玻璃命名规则
            if (glassName.StartsWith("N-"))
            {
                if (glassName.IndexOf("K", StringComparison.Ordinal) >= 0)
                    return GlassType.Crown;
                else if (glassName.IndexOf("F", StringComparison.Ordinal) >= 0 || glassName.IndexOf("SF", StringComparison.Ordinal) >= 0)
                    return GlassType.Flint;
                else if (glassName.IndexOf("SK", StringComparison.Ordinal) >= 0)
                    return GlassType.DenseCrown;
                else if (glassName.IndexOf("LAK", StringComparison.Ordinal) >= 0 || glassName.IndexOf("LAF", StringComparison.Ordinal) >= 0)
                    return GlassType.Special;
            }
            else if (glassName.IndexOf("FK", StringComparison.Ordinal) >= 0 || glassName.IndexOf("PK", StringComparison.Ordinal) >= 0)
                return GlassType.Crown;
            else if (glassName.IndexOf("F", StringComparison.Ordinal) >= 0 || glassName.IndexOf("SF", StringComparison.Ordinal) >= 0)
                return GlassType.Flint;
            else if (glassName.IndexOf("SK", StringComparison.Ordinal) >= 0 || glassName.IndexOf("SSK", StringComparison.Ordinal) >= 0)
                return GlassType.DenseCrown;
            else if (glassName.IndexOf("LASF", StringComparison.Ordinal) >= 0)
                return GlassType.DenseFlint;

            return GlassType.Other;
        }

        private double ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            double result;
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            return result;
        }

        private int ParseInt(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            int result;
            int.TryParse(value, out result);
            return result;
        }
    }
}