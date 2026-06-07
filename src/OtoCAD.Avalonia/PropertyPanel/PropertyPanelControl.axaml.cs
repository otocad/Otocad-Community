using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using lcdb;

namespace OtoCAD.Avalonia.PropertyPanel;

/// <summary>
/// Phase 1F: 通用属性面板 dispatcher.
/// 选中实体后, 用 GenericPropertyBuilder 动态构建编辑器 UI.
/// </summary>
public partial class PropertyPanelControl : UserControl
{
    private TextBlock _titleText = null!;
    private StackPanel _contentPanel = null!;
    private Action? _onChanged;

    public PropertyPanelControl()
    {
        InitializeComponent();
        _titleText = this.FindControl<TextBlock>("TitleText")!;
        _contentPanel = this.FindControl<StackPanel>("ContentPanel")!;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// 设置 onChanged 回调 (修改属性后画布重绘). MainWindow 启动时注入一次.
    /// </summary>
    public void SetChangeNotifier(Action onChanged) => _onChanged = onChanged;

    /// <summary>
    /// 设置当前编辑目标. 传 null 清空.
    /// 兼容旧 SetTarget(LineViewModel) 调用 (POC-04 时代), 新代码请用 SetEntity.
    /// </summary>
    public void SetTarget(LineViewModel? vm)
    {
        if (vm is null) { ClearContent("属性: (无选中)"); return; }
        // 旧 VM 路径已废弃, 但保持兼容
        SetEntityInternal(GetLineFromVm(vm), 1);
    }

    /// <summary>新 API: 直接传 Entity (Phase 1F). totalCount 用于多选时友好提示.</summary>
    public void SetEntity(Entity? entity, int totalCount = 1)
    {
        if (entity is null) { ClearContent("属性: (无选中)"); return; }
        SetEntityInternal(entity, totalCount);
    }

    private void SetEntityInternal(Entity entity, int totalCount)
    {
        _titleText.Text = totalCount > 1
            ? $"属性: {entity.GetType().Name} (+ 其他 {totalCount - 1} 个)"
            : $"属性: {entity.GetType().Name}";
        _contentPanel.Children.Clear();
        var onChanged = _onChanged ?? (() => { });
        var controls = GenericPropertyBuilder.Build(entity, onChanged);
        foreach (var c in controls) _contentPanel.Children.Add(c);
    }

    /// <summary>
    /// 绑定模板图框属性区某值格进行编辑(三列式)。区/指标只读显示,值可改。
    /// onSet 由调用方提供:写回该格文本(IPropertyZoneFrame.SetCellText)+ 重绘。
    /// </summary>
    public void SetFrameCell(string zone, string label, string currentText, Action<string> onSet,
        Action? onAddRow = null, Action? onDeleteRow = null)
    {
        _titleText.Text = string.IsNullOrEmpty(label)
            ? $"属性区: {zone}"
            : $"属性区: {zone} · {label}";
        _contentPanel.Children.Clear();

        if (!string.IsNullOrEmpty(label))
            _contentPanel.Children.Add(MakeReadonlyRow("指标", label));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock { Text = "值", VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        var box = new TextBox { Text = currentText, Margin = new Thickness(0, 2), FontSize = 11 };
        Grid.SetColumn(box, 1);
        box.TextChanged += (_, _) => onSet(box.Text ?? "");
        grid.Children.Add(box);
        _contentPanel.Children.Add(grid);

        if (onAddRow is not null)
        {
            var add = new Button { Content = "＋ 添加指标行", Margin = new Thickness(0, 6, 0, 0), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Stretch };
            add.Click += (_, _) => onAddRow();
            _contentPanel.Children.Add(add);
        }
        if (onDeleteRow is not null)
        {
            var del = new Button { Content = "－ 删除此行", Margin = new Thickness(0, 6, 0, 0), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Stretch };
            del.Click += (_, _) => onDeleteRow();
            _contentPanel.Children.Add(del);
        }
    }

    /// <summary>
    /// B3/UX: 点中属性区(区/列)→ 顶部可编辑"区标题"输入框 + 该区"可加指标"复选框清单。
    /// onRenameZone(新标题):改区标题(不重绑面板,保持输入焦点)。
    /// onAddIndicator(指标) / onRemoveRow(行下标):增删后由调用方重绑本面板。
    /// </summary>
    public void SetFrameZone(string zoneTitle, IReadOnlyList<string> currentRows,
        Action<string> onRenameZone,
        Action<PropertyZoneCatalog.Indicator> onAddIndicator, Action<int> onRemoveRow)
    {
        _titleText.Text = $"属性区: {zoneTitle}";
        _contentPanel.Children.Clear();

        // 区标题(可编辑)
        var titleGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        titleGrid.Children.Add(new TextBlock { Text = "区标题", VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        var titleBox = new TextBox { Text = zoneTitle, Margin = new Thickness(0, 2), FontSize = 11 };
        Grid.SetColumn(titleBox, 1);
        titleBox.TextChanged += (_, _) => onRenameZone(titleBox.Text ?? "");
        titleGrid.Children.Add(titleBox);
        _contentPanel.Children.Add(titleGrid);

        _contentPanel.Children.Add(new TextBlock
        {
            Text = "勾选添加指标到该区(取消则移除):",
            FontSize = 11,
            Margin = new Thickness(0, 8, 0, 6),
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
        });

        foreach (var ind in PropertyZoneCatalog.For(zoneTitle))
        {
            int existingIdx = -1;
            for (int i = 0; i < currentRows.Count; i++)
                if (MatchesIndicator(currentRows[i], ind.Prefix)) { existingIdx = i; break; }

            var cb = new CheckBox
            {
                Content = ind.Label,
                IsChecked = existingIdx >= 0,     // 在附事件前设初值 → 不触发回调
                FontSize = 11,
                Margin = new Thickness(0, 1)
            };
            int capturedIdx = existingIdx;
            var capturedInd = ind;
            cb.IsCheckedChanged += (_, _) =>
            {
                bool nowChecked = cb.IsChecked == true;
                if (nowChecked && capturedIdx < 0) onAddIndicator(capturedInd);
                else if (!nowChecked && capturedIdx >= 0) onRemoveRow(capturedIdx);
            };
            _contentPanel.Children.Add(cb);
        }
    }

    private static bool MatchesIndicator(string row, string prefix)
        => !string.IsNullOrEmpty(prefix) &&
           (row ?? "").TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static Control MakeReadonlyRow(string label, string value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("70,*") };
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        var v = new TextBlock
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold
        };
        Grid.SetColumn(v, 1);
        grid.Children.Add(v);
        return grid;
    }

    private void ClearContent(string title)
    {
        _titleText.Text = title;
        _contentPanel.Children.Clear();
    }

    private static Entity GetLineFromVm(LineViewModel vm)
    {
        // 反射拿 _line 私有字段 (兼容旧 API, 仅在 BindFirstLineToPropertyPanel 调用)
        var fld = typeof(LineViewModel).GetField("_line", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (Entity)fld!.GetValue(vm)!;
    }
}
