using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
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
