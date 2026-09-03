using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OtoCAD.OpticEntity;

namespace OtoCAD.OpticEntity.GlassImport
{
    /// <summary>
    /// 成都光明（CDGM）玻璃库导入器
    /// 支持 .agf (Zemax格式) 和 .xml 格式
    /// </summary>
    public class CDGMGlassImporter : GlassImporterBase
    {
        public override string Name => "成都光明玻璃库导入器";
        public override string[] SupportedExtensions => new[] { ".agf", ".xml", ".cdgm" };
        public override string Manufacturer => "CDGM";

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
                DispersionFormula formula = DispersionFormula.Schott;
                
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
                        if (parts.Length >= 2)
                        {
                            currentMaterial = new GlassMaterial
                            {
                                Name = parts[1],
                                Code = parts[1],
                                Manufacturer = Manufacturer,
                                Type = DetermineGlassType(parts[1])
                            };
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
                                formula = (DispersionFormula)(formulaType - 1);
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
                            if (double.TryParse(parts[i], out coeff))
                            {
                                coefficients.Add(coeff);
                            }
                        }
                    }
                    // 解析其他属性
                    else if (line.StartsWith("TD ") && currentMaterial != null)
                    {
                        // 热学数据
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 7)
                        {
                            double.TryParse(parts[6], out double density);
                            currentMaterial.Density = density;
                        }
                    }
                    else if (line.StartsWith("OD ") && currentMaterial != null)
                    {
                        // 其他光学数据
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            double.TryParse(parts[2], out double partialDispersion);
                            currentMaterial.PartialDispersion = partialDispersion;
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
        /// 从XML格式导入
        /// </summary>
        private List<GlassMaterial> ImportFromXml(Stream stream)
        {
            var materials = new List<GlassMaterial>();
            
            // 这里可以使用 XmlDocument 或 XDocument 来解析
            // 实现XML格式的解析逻辑
            
            return materials;
        }

        /// <summary>
        /// 计算标准波长的折射率
        /// </summary>
        private void CalculateStandardIndices(GlassMaterial material, double[] coefficients, DispersionFormula formula)
        {
            // d光 (587.56nm)
            material.Nd = CalculateRefractiveIndex(587.56, coefficients, formula);
            // F光 (486.13nm)
            material.NF = CalculateRefractiveIndex(486.13, coefficients, formula);
            // C光 (656.27nm)
            material.NC = CalculateRefractiveIndex(656.27, coefficients, formula);
        }

        /// <summary>
        /// 根据玻璃名称判断类型
        /// </summary>
        private GlassType DetermineGlassType(string glassName)
        {
            glassName = glassName.ToUpper();
            
            if (glassName.IndexOf("K", StringComparison.Ordinal) >= 0 && glassName.IndexOf("ZK", StringComparison.Ordinal) < 0 && glassName.IndexOf("FK", StringComparison.Ordinal) < 0)
                return GlassType.Crown;
            else if (glassName.IndexOf("F", StringComparison.Ordinal) >= 0 && glassName.IndexOf("ZF", StringComparison.Ordinal) < 0)
                return GlassType.Flint;
            else if (glassName.IndexOf("ZK", StringComparison.Ordinal) >= 0)
                return GlassType.DenseCrown;
            else if (glassName.IndexOf("ZF", StringComparison.Ordinal) >= 0)
                return GlassType.DenseFlint;
            else if (glassName.IndexOf("D-", StringComparison.Ordinal) >= 0)
                return GlassType.ED;
            else if (glassName.IndexOf("H-", StringComparison.Ordinal) >= 0)
                return GlassType.Special;
            else
                return GlassType.Other;
        }
    }
}