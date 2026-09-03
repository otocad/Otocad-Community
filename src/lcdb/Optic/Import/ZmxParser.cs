using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace lcdb.Optic.Import
{
    /// <summary>
    /// Zemax .zmx 文件解析器
    /// 独立解析zmx文本格式，不依赖ZOSAPI
    /// </summary>
    public class ZmxParser
    {
        #region Constants

        private static readonly HashSet<string> KnownSurfaceTypes = new HashSet<string>
        {
            "STANDARD", "EVENASPH", "ODDASPHE", "BINARY2", "TOROIDAL",
            "COORDBRK", "PARAXIAL", "DGRATING", "GRINSUR", "TILTSURF"
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// 解析.zmx文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>解析结果</returns>
        public ZmxSystemData Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var result = new ZmxSystemData { FilePath = filePath };
                result.AddError($"文件不存在: {filePath}");
                return result;
            }

            try
            {
                // Zemax文件可能使用不同编码，尝试多种编码
                string content = ReadFileWithEncoding(filePath);
                var data = ParseContent(content);
                data.FilePath = filePath;
                return data;
            }
            catch (Exception ex)
            {
                var result = new ZmxSystemData { FilePath = filePath };
                result.AddError($"读取文件失败: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 解析zmx文件内容
        /// </summary>
        /// <param name="content">文件内容</param>
        /// <returns>解析结果</returns>
        public ZmxSystemData ParseContent(string content)
        {
            var data = new ZmxSystemData();

            if (string.IsNullOrWhiteSpace(content))
            {
                data.AddError("文件内容为空");
                return data;
            }

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            ZmxSurface currentSurface = null;
            int surfaceCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // 检测表面定义开始
                    if (line.StartsWith("SURF"))
                    {
                        // 保存前一个表面
                        if (currentSurface != null)
                        {
                            data.Surfaces.Add(currentSurface);
                        }

                        // 创建新表面
                        currentSurface = new ZmxSurface();
                        var match = Regex.Match(line, @"SURF\s+(\d+)");
                        if (match.Success)
                        {
                            currentSurface.SurfaceNumber = int.Parse(match.Groups[1].Value);
                        }
                        else
                        {
                            currentSurface.SurfaceNumber = surfaceCount;
                        }
                        surfaceCount++;
                        continue;
                    }

                    // 解析系统级参数
                    if (ParseSystemParameter(line, data)) continue;

                    // 解析表面参数
                    if (currentSurface != null)
                    {
                        ParseSurfaceParameter(line, currentSurface, data);
                    }
                }
                catch (Exception ex)
                {
                    data.AddWarning($"行 {i + 1} 解析失败: {line} ({ex.Message})");
                }
            }

            // 添加最后一个表面
            if (currentSurface != null)
            {
                data.Surfaces.Add(currentSurface);
            }

            // 后处理
            PostProcess(data);

            return data;
        }

        /// <summary>
        /// 验证文件格式
        /// </summary>
        public bool ValidateFormat(string filePath, out List<string> errors)
        {
            errors = new List<string>();

            if (!File.Exists(filePath))
            {
                errors.Add($"文件不存在: {filePath}");
                return false;
            }

            try
            {
                string content = ReadFileWithEncoding(filePath);

                // 检查是否包含基本的zmx结构
                if (!content.Contains("SURF"))
                {
                    errors.Add("文件不包含表面定义 (SURF)");
                    return false;
                }

                // 检查是否为序列模式
                if (content.Contains("MODE NSC"))
                {
                    errors.Add("不支持非序列模式 (NSC)，仅支持序列模式");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errors.Add($"验证失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 尝试多种编码读取文件
        /// </summary>
        private string ReadFileWithEncoding(string filePath)
        {
            // 尝试UTF-8
            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                if (IsValidZmxContent(content)) return content;
            }
            catch { }

            // 尝试ANSI (Windows默认)
            try
            {
                string content = File.ReadAllText(filePath, Encoding.Default);
                if (IsValidZmxContent(content)) return content;
            }
            catch { }

            // 尝试UTF-16
            try
            {
                string content = File.ReadAllText(filePath, Encoding.Unicode);
                if (IsValidZmxContent(content)) return content;
            }
            catch { }

            // 最后尝试ASCII
            return File.ReadAllText(filePath, Encoding.ASCII);
        }

        /// <summary>
        /// 检查内容是否为有效的zmx格式
        /// </summary>
        private bool IsValidZmxContent(string content)
        {
            return content.Contains("SURF") || content.Contains("VERS") || content.Contains("MODE");
        }

        /// <summary>
        /// 解析系统级参数
        /// </summary>
        private bool ParseSystemParameter(string line, ZmxSystemData data)
        {
            // 版本
            if (line.StartsWith("VERS"))
            {
                data.Version = line.Substring(4).Trim();
                return true;
            }

            // 模式
            if (line.StartsWith("MODE"))
            {
                data.Mode = line.Substring(4).Trim();
                return true;
            }

            // 标题
            if (line.StartsWith("NAME") || line.StartsWith("TITL"))
            {
                data.Title = line.Substring(4).Trim().Trim('"');
                return true;
            }

            // 波长
            if (line.StartsWith("WAVM"))
            {
                var match = Regex.Match(line, @"WAVM\s+\d+\s+([\d.E+-]+)");
                if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double wavelength))
                {
                    data.Wavelengths.Add(wavelength);
                }
                return true;
            }

            // 系统孔径
            if (line.StartsWith("ENPD"))
            {
                var match = Regex.Match(line, @"ENPD\s+([\d.E+-]+)");
                if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double aperture))
                {
                    data.SystemAperture = aperture;
                    data.ApertureType = "EPD";
                }
                return true;
            }

            if (line.StartsWith("FLOA"))
            {
                var match = Regex.Match(line, @"FLOA\s+([\d.E+-]+)");
                if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double fNumber))
                {
                    data.SystemAperture = fNumber;
                    data.ApertureType = "F/#";
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解析表面参数
        /// </summary>
        private void ParseSurfaceParameter(string line, ZmxSurface surface, ZmxSystemData data)
        {
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            string key = parts[0].ToUpper();
            string value = parts.Length > 1 ? parts[1] : "";

            switch (key)
            {
                case "TYPE":
                    surface.Type = value;
                    if (!KnownSurfaceTypes.Contains(value))
                    {
                        data.AddWarning($"表面 {surface.SurfaceNumber}: 未知表面类型 '{value}'");
                    }
                    break;

                case "CURV":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double curv))
                    {
                        surface.Curvature = curv;
                        // 计算曲率半径
                        if (Math.Abs(curv) < 1e-15)
                        {
                            surface.Radius = double.PositiveInfinity;
                        }
                        else
                        {
                            surface.Radius = 1.0 / curv;
                        }
                    }
                    break;

                case "DISZ":
                    if (value.ToUpper() == "INFINITY")
                    {
                        surface.Thickness = double.PositiveInfinity;
                    }
                    else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double thick))
                    {
                        surface.Thickness = thick;
                    }
                    break;

                case "GLAS":
                    surface.Glass = value;
                    break;

                case "DIAM":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double diam))
                    {
                        surface.SemiDiameter = diam;
                    }
                    break;

                // 机械半口径 (Mechanical Semi-Diameter): MEMA <value> ... ; > DIAM 时净口径外有平肩.
                case "MEMA":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double mema))
                    {
                        surface.MechanicalSemiDiameter = mema;
                    }
                    break;

                // 圆形通光裁剪: CLAP(circular) / FLAP(floating, 自动随光束). 格式 <r_min> <r_max>, r_max=裁剪半径.
                // (真实 Zemax 样本里 FLAP 远多于 CLAP; 二者语义对 2D 净口径都取 r_max.)
                case "CLAP":
                case "FLAP":
                    if (parts.Length >= 3 &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double clap))
                    {
                        surface.ClearSemiDiameter = clap;
                    }
                    break;

                case "CONI":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double conic))
                    {
                        surface.Conic = conic;
                    }
                    break;

                case "COMM":
                    surface.Comment = string.Join(" ", parts.Skip(1));
                    break;

                case "STOP":
                    surface.IsStop = true;
                    break;

                // 非球面系数
                case "PARM":
                    ParseAsphericParameter(parts, surface);
                    break;
            }

            // 解析PARM n value格式
            var parmMatch = Regex.Match(line, @"PARM\s+(\d+)\s+([\d.E+-]+)");
            if (parmMatch.Success)
            {
                int index = int.Parse(parmMatch.Groups[1].Value) - 1;
                if (index >= 0 && index < 8 && double.TryParse(parmMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double coef))
                {
                    surface.AsphericCoefficients[index] = coef;
                }
            }
        }

        /// <summary>
        /// 解析非球面参数
        /// </summary>
        private void ParseAsphericParameter(string[] parts, ZmxSurface surface)
        {
            if (parts.Length < 3) return;

            if (int.TryParse(parts[1], out int index) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                // PARM索引从1开始，数组索引从0开始
                index--;
                if (index >= 0 && index < surface.AsphericCoefficients.Length)
                {
                    surface.AsphericCoefficients[index] = value;
                }
            }
        }

        /// <summary>
        /// 后处理 - 补充和验证数据
        /// </summary>
        private void PostProcess(ZmxSystemData data)
        {
            // 验证表面数量
            if (data.Surfaces.Count < 2)
            {
                data.AddError("表面数量不足 (至少需要OBJ和IMG)");
                return;
            }

            // 检查是否有光学元件
            bool hasElement = false;
            for (int i = 1; i < data.Surfaces.Count - 1; i++)
            {
                if (data.Surfaces[i].HasMaterial)
                {
                    hasElement = true;
                    break;
                }
            }

            if (!hasElement)
            {
                data.AddWarning("未检测到光学元件 (所有表面都是空气间隔)");
            }

            // 设置默认波长
            if (data.Wavelengths.Count == 0)
            {
                data.Wavelengths.Add(0.55); // 默认可见光波长 (um)
                data.AddWarning("未指定波长，使用默认值 0.55um");
            }

            // 验证元件数据
            foreach (var element in data.Elements)
            {
                if (element.CenterThickness <= 0)
                {
                    data.AddWarning($"元件 ({element.GlassMaterial}): 中心厚度为零或负数");
                }
                if (element.Diameter <= 0)
                {
                    data.AddWarning($"元件 ({element.GlassMaterial}): 直径为零或负数");
                }
            }
        }

        #endregion
    }
}
