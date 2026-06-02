using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LitMath;
using lcdb;
using lcdb.Common;

namespace OtoCAD.Avalonia.PropertyPanel;

/// <summary>
/// Phase 1F: 通用属性面板构建器.
/// 输入: 任意 Entity, Action onChanged (修改后画布重绘)
/// 输出: 一组可放进 ScrollViewer/StackPanel 的 Avalonia 控件
///
/// 策略:
/// 1. Line / Circle / Arc / Polyline / Point — 手写专用编辑器 (几何最常用)
/// 2. 任何 Mark (有 Position 或 Center 属性) — 通用 Position X/Y + Size (如有)
/// 3. 兜底 — 反射列出 public 可读写属性, double/string/bool/enum 自动生成编辑器
/// </summary>
public static class GenericPropertyBuilder
{
    /// <summary>属性名 → 中文显示标签 (只影响显示, 未列出的回退原名).</summary>
    private static readonly Dictionary<string, string> ZhLabels = new(StringComparer.Ordinal)
    {
        // 光学件几何
        ["Diameter"] = "净口径 Ø",
        ["MechanicalDiameter"] = "机械外径 Ø",
        ["Thickness"] = "中心厚度 d",
        ["R1"] = "前表面 R1",
        ["R2"] = "后表面 R2",
        ["RContact"] = "胶合面 Rc",
        ["R3"] = "后表面 R3",
        ["T1"] = "片1 厚度 T1",
        ["T2"] = "片2 厚度 T2",
        ["Rotation"] = "旋转角 (°)",
        ["Pattern"] = "玻璃填充",
        ["ShowOpticalAxis"] = "显示光轴",
        ["AxisPadding"] = "光轴延伸",
        // 材料
        ["MaterialName"] = "材料",
        ["RefractiveIndex"] = "折射率 nd",
        ["AbbeNumber"] = "阿贝数 vd",
        ["Material1"] = "片1 材料",
        ["Nd1"] = "片1 nd",
        ["Vd1"] = "片1 vd",
        ["Material2"] = "片2 材料",
        ["Nd2"] = "片2 nd",
        ["Vd2"] = "片2 vd",
        // 公差
        ["R1Tolerance"] = "R1 公差",
        ["R2Tolerance"] = "R2 公差",
        ["R3Tolerance"] = "R3 公差",
        ["RContactTolerance"] = "Rc 公差",
        ["DiameterTolerance"] = "口径公差",
        ["ThicknessTolerance"] = "厚度公差",
        ["T1Tolerance"] = "T1 公差",
        ["T2Tolerance"] = "T2 公差",
    };

    /// <summary>属性名 → 显示标签 (有中文映射用中文, 否则原名).</summary>
    private static string Label(string propName)
        => ZhLabels.TryGetValue(propName, out var zh) ? zh : propName;

    public static IReadOnlyList<Control> Build(Entity entity, Action onChanged)
    {
        var sections = new List<Control>();
        sections.Add(MakeHeader($"类型: {entity.GetType().Name}"));

        // 包装: 改属性后先让实体清缓存 (OpticalLens 等派生几何要重算),
        // 再让 canvas 重绘. 普通 Line/Circle 的 InvalidateRenderCache 是
        // no-op, 无副作用.
        var rawOnChanged = onChanged;
        onChanged = () => { entity.InvalidateRenderCache(); rawOnChanged(); };

        // 专用编辑器
        switch (entity)
        {
            case Line line:
                sections.Add(MakeLineEditor(line, onChanged));
                return sections;
            case Circle c:
                sections.Add(MakeCircleEditor(c, onChanged));
                return sections;
            case Arc arc:
                sections.Add(MakeArcEditor(arc, onChanged));
                return sections;
            case lcdb.Point pt:
                sections.Add(MakePointEditor(pt, onChanged));
                return sections;
        }

        // 反射通用 — Position/Center + 简单标量
        sections.Add(MakeReflectionEditor(entity, onChanged));
        return sections;
    }

    private static Control MakeHeader(string title)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
            Padding = new Thickness(8, 4),
            Margin = new Thickness(0, 0, 0, 4),
            Child = new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, FontSize = 11 }
        };
    }

    private static Control MakeSection(string title, params Control[] children)
    {
        var stack = new StackPanel { Spacing = 4 };
        foreach (var c in children) stack.Children.Add(c);
        return new Expander
        {
            Header = title,
            IsExpanded = true,
            Padding = new Thickness(8, 4),
            Content = stack
        };
    }

    private static Control MakeDoubleRow(string label, double initialValue, Action<double> onSet)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        var edit = new NumericUpDown
        {
            Value = (decimal)initialValue,
            Increment = 1,
            FormatString = "F3",
            Margin = new Thickness(0, 2),
            FontSize = 11
        };
        Grid.SetColumn(edit, 1);
        edit.ValueChanged += (_, e) => { if (e.NewValue.HasValue) onSet((double)e.NewValue.Value); };
        grid.Children.Add(edit);
        return grid;
    }

    private static Control MakeTextRow(string label, string initialValue, Action<string> onSet)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        var edit = new TextBox { Text = initialValue, Margin = new Thickness(0, 2), FontSize = 11 };
        Grid.SetColumn(edit, 1);
        edit.LostFocus += (_, _) => onSet(edit.Text ?? "");
        grid.Children.Add(edit);
        return grid;
    }

    private static Control MakeBoolRow(string label, bool initialValue, Action<bool> onSet)
    {
        var cb = new CheckBox { Content = label, IsChecked = initialValue, FontSize = 11 };
        cb.IsCheckedChanged += (_, _) => onSet(cb.IsChecked == true);
        return cb;
    }

    private static Control MakeEnumRow(string label, Type enumType, object initialValue, Action<object> onSet)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        var values = Enum.GetValues(enumType);
        var combo = new ComboBox { Margin = new Thickness(0, 2), FontSize = 11 };
        foreach (var v in values) combo.Items.Add(v!);
        combo.SelectedItem = initialValue;
        Grid.SetColumn(combo, 1);
        combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is not null) onSet(combo.SelectedItem); };
        grid.Children.Add(combo);
        return grid;
    }

    private static Control MakeReadonlyRow(string label, string value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) });
        var tb = new TextBlock { Text = value, VerticalAlignment = VerticalAlignment.Center, FontSize = 11, FontStyle = global::Avalonia.Media.FontStyle.Italic, Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)) };
        Grid.SetColumn(tb, 1);
        grid.Children.Add(tb);
        return grid;
    }

    /// <summary>
    /// ToleranceValue 行 — GB/T 13323-2009 §1.1 三类值编辑器.
    /// 三行布局: 类型 ComboBox / +Plus -Minus 双 NumericUpDown / 平面 CheckBox (仅 R 字段).
    /// 实体若原本字段为 null, 编辑首次提交时自动 new 一个 ToleranceValue (Kind=Nominal, 无偏差),
    /// 显示等价于无公差 → 行为对老文件透明.
    /// </summary>
    private static Control MakeToleranceRow(string label, ToleranceValue? initial, Action<ToleranceValue?> onSet)
    {
        var tol = initial ?? new ToleranceValue { Kind = ValueKind.Nominal };
        void Commit() => onSet(tol);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Top,
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 0),
        });

        var container = new StackPanel { Spacing = 2 };
        Grid.SetColumn(container, 1);
        grid.Children.Add(container);

        // Row A: 类型 ComboBox
        var rowA = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        rowA.Children.Add(new TextBlock
        {
            Text = "类型",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 10,
            Margin = new Thickness(0, 0, 4, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
        });
        var kindCombo = new ComboBox { FontSize = 11, ItemsSource = Enum.GetValues<ValueKind>(), SelectedItem = tol.Kind };
        Grid.SetColumn(kindCombo, 1);
        kindCombo.SelectionChanged += (_, _) =>
        {
            if (kindCombo.SelectedItem is ValueKind k) { tol.Kind = k; Commit(); }
        };
        rowA.Children.Add(kindCombo);
        container.Children.Add(rowA);

        // Row B: +Plus / -Minus
        var rowB = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,*") };
        rowB.Children.Add(new TextBlock { Text = "+", VerticalAlignment = VerticalAlignment.Center, FontSize = 11, Margin = new Thickness(0, 0, 2, 0) });
        var plusEdit = new NumericUpDown { Value = (decimal)tol.Plus, Increment = 0.01M, FormatString = "F3", FontSize = 11 };
        plusEdit.ValueChanged += (_, e) => { if (e.NewValue.HasValue) { tol.Plus = (double)e.NewValue.Value; Commit(); } };
        Grid.SetColumn(plusEdit, 1);
        rowB.Children.Add(plusEdit);
        rowB.Children.Add(new TextBlock { Text = "  -", VerticalAlignment = VerticalAlignment.Center, FontSize = 11, Margin = new Thickness(6, 0, 2, 0), [Grid.ColumnProperty] = 2 });
        var minusEdit = new NumericUpDown { Value = (decimal)tol.Minus, Increment = 0.01M, FormatString = "F3", FontSize = 11 };
        minusEdit.ValueChanged += (_, e) => { if (e.NewValue.HasValue) { tol.Minus = (double)e.NewValue.Value; Commit(); } };
        Grid.SetColumn(minusEdit, 3);
        rowB.Children.Add(minusEdit);
        container.Children.Add(rowB);

        // Row C: 平面 (R∞) checkbox — 仅对 R 类公差有意义
        if (label.IndexOf("R", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var planarCb = new CheckBox { Content = "平面 (R∞)", IsChecked = tol.IsPlanar, FontSize = 11 };
            planarCb.IsCheckedChanged += (_, _) => { tol.IsPlanar = planarCb.IsChecked == true; Commit(); };
            container.Children.Add(planarCb);
        }

        return grid;
    }

    // -------- 专用编辑器 --------

    private static Control MakeLineEditor(Line line, Action onChanged) => MakeSection("几何",
        MakeDoubleRow("起点 X", line.startPoint.X, v => { line.startPoint = new Vector2(v, line.startPoint.Y); onChanged(); }),
        MakeDoubleRow("起点 Y", line.startPoint.Y, v => { line.startPoint = new Vector2(line.startPoint.X, v); onChanged(); }),
        MakeDoubleRow("终点 X", line.endPoint.X,   v => { line.endPoint   = new Vector2(v, line.endPoint.Y);   onChanged(); }),
        MakeDoubleRow("终点 Y", line.endPoint.Y,   v => { line.endPoint   = new Vector2(line.endPoint.X, v);   onChanged(); }),
        MakeReadonlyRow("长度", line.length.ToString("F3"))
    );

    private static Control MakeCircleEditor(Circle c, Action onChanged) => MakeSection("几何",
        MakeDoubleRow("圆心 X", c.center.X, v => { c.center = new Vector2(v, c.center.Y); onChanged(); }),
        MakeDoubleRow("圆心 Y", c.center.Y, v => { c.center = new Vector2(c.center.X, v); onChanged(); }),
        MakeDoubleRow("半径",   c.radius,   v => { c.radius = System.Math.Max(0.0001, v); onChanged(); }),
        MakeReadonlyRow("直径", c.diameter.ToString("F3"))
    );

    private static Control MakeArcEditor(Arc a, Action onChanged) => MakeSection("几何",
        MakeDoubleRow("圆心 X", a.center.X, v => { a.center = new Vector2(v, a.center.Y); onChanged(); }),
        MakeDoubleRow("圆心 Y", a.center.Y, v => { a.center = new Vector2(a.center.X, v); onChanged(); }),
        MakeDoubleRow("半径",   a.radius,   v => { a.radius = System.Math.Max(0.0001, v); onChanged(); }),
        MakeDoubleRow("起角(°)", a.startAngle * 180 / System.Math.PI, v => { a.startAngle = v * System.Math.PI / 180; onChanged(); }),
        MakeDoubleRow("终角(°)", a.endAngle   * 180 / System.Math.PI, v => { a.endAngle   = v * System.Math.PI / 180; onChanged(); })
    );

    private static Control MakePointEditor(lcdb.Point pt, Action onChanged) => MakeSection("几何",
        MakeDoubleRow("X", pt.position.X, v => { pt.position = new Vector2(v, pt.position.Y); onChanged(); }),
        MakeDoubleRow("Y", pt.position.Y, v => { pt.position = new Vector2(pt.position.X, v); onChanged(); })
    );

    // -------- 反射通用 (适配 Mark + 任意 Entity) --------

    private static Control MakeReflectionEditor(Entity e, Action onChanged)
    {
        var rows = new List<Control>();
        var t = e.GetType();

        // 1. Position / Center (Vector2) — 多数 Mark 用这个
        foreach (var posPropName in new[] { "Position", "Center", "StartPosition" })
        {
            var prop = t.GetProperty(posPropName);
            if (prop is not null && prop.PropertyType == typeof(Vector2) && prop.CanWrite)
            {
                var pos = (Vector2)prop.GetValue(e)!;
                rows.Add(MakeDoubleRow($"{posPropName} X", pos.X, v =>
                {
                    var cur = (Vector2)prop.GetValue(e)!;
                    prop.SetValue(e, new Vector2(v, cur.Y));
                    onChanged();
                }));
                rows.Add(MakeDoubleRow($"{posPropName} Y", pos.Y, v =>
                {
                    var cur = (Vector2)prop.GetValue(e)!;
                    prop.SetValue(e, new Vector2(cur.X, v));
                    onChanged();
                }));
                break;  // 只取第一个找到的
            }
        }

        // 2. 其他 public 标量属性 (double/int/string/bool/enum), 跳过 base 类已处理的 Entity 内部字段
        var skipProps = new HashSet<string>(StringComparer.Ordinal)
        {
            "Position", "Center", "StartPosition", "EndPosition",  // 上面已处理 / 复杂
            "className", "bounding", "color", "colorValue", "resolvedColor", "lineType", "lineWeight",
            "layerId", "layer", "linetype", "id", "Source", "ParentBlockId", "ParentComponentId",
            "EditMode", "IsGenerated", "IsLocked", "Owner", "database", "parent",
            "MarkShape"  // CoatingMark 旧版兼容形状 (三角/方/菱); 类型应由 CoatingType 决定, 不暴露给用户
        };

        foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (skipProps.Contains(prop.Name)) continue;
            if (!prop.CanRead) continue;

            var pt = prop.PropertyType;
            try
            {
                if (pt == typeof(double) && prop.CanWrite)
                {
                    rows.Add(MakeDoubleRow(Label(prop.Name), (double)prop.GetValue(e)!, v => { prop.SetValue(e, v); onChanged(); }));
                }
                else if (pt == typeof(double?) && prop.CanWrite)
                {
                    // 可空 double (如 MechanicalDiameter 机械外径): null 显示 0, 编辑后写入具体值.
                    var cur = (double?)prop.GetValue(e);
                    rows.Add(MakeDoubleRow(Label(prop.Name), cur ?? 0.0, v => { prop.SetValue(e, (double?)v); onChanged(); }));
                }
                else if (pt == typeof(int) && prop.CanWrite)
                {
                    rows.Add(MakeDoubleRow(Label(prop.Name), (int)prop.GetValue(e)!, v => { prop.SetValue(e, (int)v); onChanged(); }));
                }
                else if (pt == typeof(string) && prop.CanWrite)
                {
                    rows.Add(MakeTextRow(Label(prop.Name), (string?)prop.GetValue(e) ?? "", v => { prop.SetValue(e, v); onChanged(); }));
                }
                else if (pt == typeof(bool) && prop.CanWrite)
                {
                    rows.Add(MakeBoolRow(Label(prop.Name), (bool)prop.GetValue(e)!, v => { prop.SetValue(e, v); onChanged(); }));
                }
                else if (pt.IsEnum && prop.CanWrite)
                {
                    var current = prop.GetValue(e)!;
                    rows.Add(MakeEnumRow(Label(prop.Name), pt, current, v => { prop.SetValue(e, v); onChanged(); }));
                }
                else if (pt == typeof(ToleranceValue) && prop.CanWrite)
                {
                    var initial = prop.GetValue(e) as ToleranceValue;
                    rows.Add(MakeToleranceRow(Label(prop.Name), initial, v => { prop.SetValue(e, v); onChanged(); }));
                }
            }
            catch { /* 属性 getter 抛异常的跳过 */ }
        }

        return MakeSection("属性", rows.ToArray());
    }
}
