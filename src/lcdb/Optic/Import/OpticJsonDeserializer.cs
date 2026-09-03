using System;
using System.Text.Json;

namespace lcdb.Optic.Import
{
    /// <summary>
    /// Optiland <c>Optic.to_dict()</c> JSON 解析器 — 把 <c>surface_group</c> 转成中性
    /// <see cref="ZmxSystemData"/> (与 <see cref="ZmxParser"/> 输出同一模型, 供
    /// PrescriptionToLensBuilder 共用). 不消费 fields/solves/pickups/ray_tracer (2D 制图无关).
    ///
    /// 契约见 docs/开发文档/光学设计导入开发计划.md §8:
    /// - radius/厚度等非有限值线格式统一为 null ⇒ 此处映射为 ∞/0.
    /// - material_post = 该面之后的材料 (Optiland/Zemax 惯例): 牌号玻璃 / IdealMaterial(index) / 空气.
    /// </summary>
    public sealed class OpticJsonDeserializer
    {
        /// <summary>解析 Optic JSON 文本. 失败信息写入返回对象的 Errors/Warnings (不抛).</summary>
        public ZmxSystemData Parse(string json)
        {
            var data = new ZmxSystemData { Mode = "SEQ" };
            if (string.IsNullOrWhiteSpace(json))
            {
                data.AddError("JSON 内容为空");
                return data;
            }

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (Exception ex) { data.AddError($"JSON 解析失败: {ex.Message}"); return data; }

            using (doc)
            {
                try { ParseRoot(doc.RootElement, data); }
                catch (Exception ex) { data.AddError($"Optic 结构解析失败: {ex.Message}"); }
            }
            return data;
        }

        private void ParseRoot(JsonElement root, ZmxSystemData data)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                data.AddError("根节点不是 JSON 对象");
                return;
            }

            if (root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                data.Title = name.GetString();

            // version 校验 (契约 §8.3): 目前只验证 1.x, 不匹配仅告警
            if (root.TryGetProperty("version", out var ver) && ver.ValueKind == JsonValueKind.Number)
            {
                double v = ver.GetDouble();
                if (v < 1.0 || v >= 2.0)
                    data.AddWarning($"Optic schema 版本 {v} 未经验证, 字段可能不兼容");
            }

            // aperture { type, value }
            if (root.TryGetProperty("aperture", out var ap) && ap.ValueKind == JsonValueKind.Object)
            {
                if (ap.TryGetProperty("type", out var at) && at.ValueKind == JsonValueKind.String)
                    data.ApertureType = at.GetString();
                if (TryGetNumber(ap, "value", out double apv))
                    data.SystemAperture = apv;
            }

            // wavelengths.wavelengths[].value (um)
            if (root.TryGetProperty("wavelengths", out var wg) && wg.ValueKind == JsonValueKind.Object
                && wg.TryGetProperty("wavelengths", out var wl) && wl.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in wl.EnumerateArray())
                    if (w.ValueKind == JsonValueKind.Object && TryGetNumber(w, "value", out double wv))
                        data.Wavelengths.Add(wv);
            }

            // surface_group.surfaces[]
            if (!root.TryGetProperty("surface_group", out var sg) || sg.ValueKind != JsonValueKind.Object
                || !sg.TryGetProperty("surfaces", out var surfs) || surfs.ValueKind != JsonValueKind.Array)
            {
                data.AddError("缺少 surface_group.surfaces");
                return;
            }

            int idx = 0;
            foreach (var s in surfs.EnumerateArray())
            {
                data.Surfaces.Add(ParseSurface(s, idx, data));
                idx++;
            }

            if (data.Surfaces.Count < 2)
                data.AddError("表面数量不足 (至少需要 OBJ + IMG)");

            if (data.Wavelengths.Count == 0)
            {
                data.Wavelengths.Add(0.55);
                data.AddWarning("未指定波长, 使用默认 0.55um");
            }
        }

        private ZmxSurface ParseSurface(JsonElement s, int index, ZmxSystemData data)
        {
            var surf = new ZmxSurface { SurfaceNumber = index };

            if (s.TryGetProperty("geometry", out var g) && g.ValueKind == JsonValueKind.Object)
            {
                string gtype = (g.TryGetProperty("type", out var gt) && gt.ValueKind == JsonValueKind.String
                    ? gt.GetString() : null) ?? "StandardGeometry";
                surf.Type = MapGeometryType(gtype);

                // radius: null/缺失 ⇒ 平面 (∞)
                if (TryGetNumber(g, "radius", out double radius))
                {
                    surf.Radius = radius;
                    surf.Curvature = Math.Abs(radius) < 1e-15 ? 0.0 : 1.0 / radius;
                }
                else
                {
                    surf.Radius = double.PositiveInfinity;
                    surf.Curvature = 0.0;
                }

                if (TryGetNumber(g, "conic", out double conic))
                    surf.Conic = conic;

                // 非球面系数 best-effort (字段名待定, 见 §8.3): 尝试 "coefficients"
                if (g.TryGetProperty("coefficients", out var coeffs) && coeffs.ValueKind == JsonValueKind.Array)
                {
                    int i = 0;
                    foreach (var cf in coeffs.EnumerateArray())
                    {
                        if (i >= surf.AsphericCoefficients.Length) break;
                        if (cf.ValueKind == JsonValueKind.Number) surf.AsphericCoefficients[i] = cf.GetDouble();
                        i++;
                    }
                }

                if (!IsKnownGeometry(gtype))
                    data.AddWarning($"面 {index}: 未识别几何类型 '{gtype}', 按球面/圆锥近似");
            }
            else
            {
                surf.Radius = double.PositiveInfinity;
            }

            // thickness (到下一面距离); 物面 null ⇒ 0
            if (TryGetNumber(s, "thickness", out double thickness))
                surf.Thickness = thickness;

            if (s.TryGetProperty("material_post", out var m) && m.ValueKind == JsonValueKind.Object)
                ParseMaterial(m, surf);

            if (s.TryGetProperty("is_stop", out var st) && st.ValueKind == JsonValueKind.True)
                surf.IsStop = true;

            if (s.TryGetProperty("comment", out var cm) && cm.ValueKind == JsonValueKind.String)
                surf.Comment = cm.GetString();

            // 径向 aperture (Optiland RadialAperture, 来自 Zemax CLAP) = 净口径; r_max=净口径半径.
            // 注: Zemax 的机械 Semi-Diameter(DIAM) 不进 Optic to_dict (见 §8.3 缺口 2), 故此处只得净口径.
            if (s.TryGetProperty("aperture", out var sap) && sap.ValueKind == JsonValueKind.Object)
            {
                if (TryGetNumber(sap, "r_max", out double rmax)) surf.ClearSemiDiameter = rmax;
            }

            return surf;
        }

        private static void ParseMaterial(JsonElement m, ZmxSurface surf)
        {
            string mtype = (m.TryGetProperty("type", out var mt) && mt.ValueKind == JsonValueKind.String
                ? mt.GetString() : null) ?? "";

            // 牌号玻璃 (type=="Material")
            if (m.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(nm.GetString()))
            {
                surf.Glass = nm.GetString();
            }

            // 折射率 (IdealMaterial.index; index≈1.0 视为空气, 不记)
            if (TryGetNumber(m, "index", out double n) && n > 1.0 + 1e-6)
                surf.RefractiveIndexPost = n;

            if (TryGetNumber(m, "abbe", out double abbe))
                surf.AbbeNumberPost = abbe;

            // Mirror / 反射面: P1 优先折射镜组, 仅记标记
            if (mtype.IndexOf("Mirror", StringComparison.OrdinalIgnoreCase) >= 0)
                surf.ExtraParameters["reflective"] = true;
        }

        private static bool TryGetNumber(JsonElement obj, string prop, out double value)
        {
            value = 0.0;
            if (obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number)
            {
                value = p.GetDouble();
                return true;
            }
            return false;
        }

        /// <summary>Optiland 几何类型名 → ZmxSurface.Type token (与 ZmxParser 对齐).</summary>
        private static string MapGeometryType(string optilandType)
        {
            switch (optilandType)
            {
                case "Plane":
                case "StandardGeometry": return "STANDARD";
                case "EvenAsphere": return "EVENASPH";
                case "OddAsphere": return "ODDASPHE";
                default: return optilandType.ToUpperInvariant();
            }
        }

        private static bool IsKnownGeometry(string optilandType)
            => optilandType == "Plane" || optilandType == "StandardGeometry"
            || optilandType == "EvenAsphere" || optilandType == "OddAsphere";
    }
}
