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
        ["Edit.Rotate"] = "edit_rotate.svg",
        ["Edit.Scale"]  = "edit_scale.svg",
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
        ["Dim.Ordinate"]       = "dim_ordinate.svg",

        // ---- 视图 ----
        ["View.ZoomExtents"] = "view_zoomextend.svg",
        ["View.ZoomIn"]      = "view_zoomin.svg",
        ["View.ZoomOut"]     = "view_zoomout.svg",
        ["View.ToggleGrid"]  = "view_grid.svg",
        ["View.ToggleSnap"]  = "view_snap.svg",

        // ---- 透镜 / 光学 ----
        ["Optic.SingleLens"]    = "single_lens.svg",
        ["Optic.DoubletLens"]   = "doublet_lens.svg",
        ["Optic.Lens"]          = "lens_template.svg",
        ["Optic.StandardSheet"] = "standard_sheet.svg",
        ["Optic.NewPrism"]      = "optic_prism.svg",
        ["Optic.AutoDim"]       = "optic_autodim.svg",
        ["Optic.ImportDesign"]  = "zmx_import.svg",   // 复用已有 Zemax 导入图标
        ["Frame.NewFrame"]        = "frame_optical.svg",
        ["Frame.NewOpticalFrame"] = "frame_optical.svg",
        ["Frame.NewCustomFrame"]  = "frame_optical.svg",

        // ---- 表面处理标记 ----
        ["Mark.Coating"]          = "draw_coating_mark.svg",
        ["Mark.Blackening"]       = "blackening_mark.svg",
        ["Mark.Polishing"]        = "polishing_mark.svg",
        ["Mark.Sandblasting"]     = "sandblasting_mark.svg",
        ["Mark.DiamondTurning"]   = "diamond_turning_mark.svg",
        ["Mark.Grinding"]         = "grinding_mark.svg",
        ["Mark.SurfaceRoughness"] = "roughness_mark.svg",

        // ---- 精度公差标记 ----
        // 注: 面型精度(Mark.FormAccuracy)已不作独立可放置标记(归面属性区表格行), 故无图标映射。
        ["Mark.CenterDeviation"]     = "center_deviation.svg",
        ["Mark.CenteringTolerance"]  = "centering_tolerance.svg",
        ["Mark.SurfaceForm"]         = "surface_form.svg",
        ["Mark.SurfaceImperfection"] = "surface_imperfection.svg",
        ["Mark.SurfaceQuality"]      = "surface_quality.svg",
        // 注: 表面纹理(Mark.SurfaceTexture)已不作独立可放置标记(归面属性区 Surface texture 行), 故无图标映射。

        // ---- 光学元素标记 ----
        // 注: 有效孔径(Mark.EffectiveAperture)已不作独立可放置标记(归面属性区 Φe 行), 故无图标映射。
        ["Mark.FocalPoint"]        = "focal_point.svg",
        ["Mark.OpticalAxis"]       = "optical_axis.svg",
        ["Mark.Assembly"]          = "assembly_mark.svg",
        ["Mark.Inspection"]        = "inspection_mark.svg",
        ["Mark.Processing"]        = "processing_mark.svg",
        ["Mark.MaterialDefect"]    = "material_defect.svg",
        ["Mark.LaserDamage"]       = "laser_damage.svg",

        // ---- 公差 / 插入 / 文件 ----
        ["Tolerance.Add"]  = "tolerance_add.svg",
        ["Insert.Image"]   = "draw_image.svg",
        ["Insert.Block"]   = "draw_insert.svg",
        ["File.Save"]      = "save_optic_doc.svg",
        ["File.Print"]     = "file_print.svg",
        ["File.ExportPng"] = "export_png.svg",
        ["File.ExportDxf"] = "export_dxf.svg",
        ["File.ExportPdf"] = "export_pdf.svg",
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
