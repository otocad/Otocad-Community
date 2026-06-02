using System;
using System.Collections.Generic;
using System.IO;

namespace OtoCAD.Avalonia.Ribbon;

/// <summary>
/// 命令名 → SVG 矢量图标文件解析.
///
/// SVG 来源: Assets/Icons/ (随输出复制到 exe 同级目录, 见 .csproj).
/// 仅映射含义明确的图标; 模糊匹配宁缺毋滥. 未映射或文件缺失返回 null,
/// 调用方 (RibbonControl.BuildButtonView) 回退到 RibbonButton.Icon (emoji/文字).
/// </summary>
public static class IconResolver
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        // ---- 绘图 ----
        ["Draw.Line"]      = "draw_line.svg",
        ["Draw.Circle"]    = "draw_circle.svg",
        ["Draw.Arc"]       = "draw_arc_cse.svg",
        ["Draw.Rectangle"] = "draw_rectangle.svg",
        ["Draw.Polyline"]  = "draw_polyline.svg",
        ["Draw.Polygon"]   = "draw_polygon.svg",
        ["Draw.Ellipse"]   = "draw_ellipse.svg",
        ["Draw.Spline"]    = "draw_spline.svg",
        ["Draw.Point"]     = "draw_dot.svg",
        ["Draw.Ray"]       = "draw_ray.svg",
        ["Draw.Xline"]     = "draw_xline.svg",
        ["Draw.Text"]      = "draw_text.svg",
        ["Draw.MText"]     = "draw_mtext.svg",
        ["Draw.Leader"]    = "draw_leader.svg",

        // ---- 编辑 ----
        ["Edit.Undo"]   = "edit_undo.svg",
        ["Edit.Redo"]   = "edit_redo.svg",
        ["Edit.Move"]   = "edit_move.svg",
        ["Edit.Copy"]   = "edit_copy.svg",
        ["Edit.Mirror"] = "edit_mirror.svg",
        ["Edit.Offset"] = "edit_offset.svg",
        ["Edit.Delete"] = "edit_delete.svg",

        // ---- 标注尺寸 ----
        ["Dim.Linear"]         = "dim_linear.svg",
        ["Dim.Aligned"]        = "dim_aligned.svg",
        ["Dim.Radial"]         = "dim_radius.svg",
        ["Dim.Diametric"]      = "dim_diameter.svg",
        ["Dim.Angular3"]       = "dim_angular.svg",
        ["Dim.Angular2Line"]   = "dim_angular.svg",
        ["Dim.CenterThickness"]= "center_thickness.svg",
        ["Dim.EdgeThickness"]  = "edge_thickness.svg",
        ["Dim.Sagitta"]        = "sagitta_dim.svg",

        // ---- 视图 ----
        ["View.ZoomExtents"] = "view_zoomextend.svg",
        ["View.ZoomIn"]      = "view_zoomin.svg",
        ["View.ZoomOut"]     = "view_zoomout.svg",
        ["View.ToggleGrid"]  = "view_grid.svg",
        ["View.ToggleSnap"]  = "view_snap.svg",

        // ---- 透镜 ----
        ["Optic.SingleLens"]    = "single_lens.svg",
        ["Optic.DoubletLens"]   = "doublet_lens.svg",
        ["Optic.Lens"]          = "lens_template.svg",
        ["Optic.StandardSheet"] = "standard_sheet.svg",

        // ---- 表面处理标记 ----
        ["Mark.Coating"]          = "draw_coating_mark.svg",
        ["Mark.Blackening"]       = "blackening_mark.svg",
        ["Mark.Polishing"]        = "polishing_mark.svg",
        ["Mark.Sandblasting"]     = "sandblasting_mark.svg",
        ["Mark.SurfaceRoughness"] = "roughness_mark.svg",

        // ---- 精度公差标记 ----
        ["Mark.FormAccuracy"]   = "form_accuracy.svg",
        ["Mark.CenterDeviation"]= "center_deviation.svg",
        ["Mark.SurfaceQuality"] = "surface_quality.svg",

        // ---- 光学元素标记 ----
        ["Mark.EffectiveAperture"] = "effective_aperture.svg",
        ["Mark.FocalPoint"]        = "focal_point.svg",
        ["Mark.OpticalAxis"]       = "optical_axis.svg",
        ["Mark.Assembly"]          = "assembly_mark.svg",
        ["Mark.Inspection"]        = "inspection_mark.svg",
        ["Mark.Processing"]        = "processing_mark.svg",
        ["Mark.MaterialDefect"]    = "material_defect.svg",
        ["Mark.LaserDamage"]       = "laser_damage.svg",

        // ---- 公差 / 插入 / 文件 ----
        ["Tolerance.Add"] = "tolerance_add.svg",
        ["Insert.Image"]  = "draw_image.svg",
        ["Insert.Block"]  = "draw_insert.svg",
        ["File.Save"]     = "save_optic_doc.svg",
    };

    private static readonly string IconDir =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

    /// <summary>返回 SVG 绝对路径; 未映射或文件不存在返回 null.</summary>
    public static string? Resolve(string? command)
    {
        if (string.IsNullOrEmpty(command)) return null;
        if (!Map.TryGetValue(command, out var file)) return null;
        var path = Path.Combine(IconDir, file);
        return File.Exists(path) ? path : null;
    }
}
