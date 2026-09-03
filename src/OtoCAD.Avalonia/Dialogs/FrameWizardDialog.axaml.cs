using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using lcdb.DrawingFrame;
using FrameTpl = lcdb.DrawingFrame.DrawingFrameTemplates;

namespace OtoCAD.Avalonia.Dialogs;

public partial class FrameWizardDialog : Window
{
    /// <summary>用户点了确定 → 已填充的 DrawingFrame 模板 (Origin 还需 PlaceFrameCmd 设).</summary>
    public DrawingFrame? Result { get; private set; }

    public FrameWizardDialog()
    {
        InitializeComponent();
        this.FindControl<Button>("OkBtn")!.Click += (_, _) => { Result = Build(); Close(); };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) => { Result = null; Close(); };

        // 默认日期 = 今天
        this.FindControl<TextBox>("DateBox")!.Text = System.DateTime.Today.ToString("yyyy-MM-dd");
    }

    private DrawingFrame Build()
    {
        // ComboBox 顺序与 PaperCombo XAML 严格匹配 — 0..4 横, 5..9 纵
        var idx = this.FindControl<ComboBox>("PaperCombo")!.SelectedIndex;
        var withTb = this.FindControl<CheckBox>("TitleBlockCheck")!.IsChecked == true;

        var size = idx switch
        {
            0 or 5 => FrameTpl.PaperSize.A0,
            1 or 6 => FrameTpl.PaperSize.A1,
            2 or 7 => FrameTpl.PaperSize.A2,
            3 or 8 => FrameTpl.PaperSize.A3,
            _      => FrameTpl.PaperSize.A4,
        };
        var orient = idx < 5 ? FrameTpl.Orientation.Landscape : FrameTpl.Orientation.Portrait;

        var f = FrameTpl.Create(size, orient, withTb);
        f.ProjectName     = this.FindControl<TextBox>("ProjectBox")!.Text ?? "";
        f.CompanyName     = this.FindControl<TextBox>("CompanyBox")!.Text ?? "";
        f.DrawingNumber   = this.FindControl<TextBox>("DrawingNumberBox")!.Text ?? "";
        f.DrawingTitle    = this.FindControl<TextBox>("TitleBox")!.Text ?? "";
        f.Designer        = this.FindControl<TextBox>("DesignerBox")!.Text ?? "";
        f.DesignDate      = this.FindControl<TextBox>("DateBox")!.Text ?? "";
        f.Material        = this.FindControl<TextBox>("MaterialBox")!.Text ?? "";
        f.Scale           = this.FindControl<TextBox>("ScaleBox")!.Text ?? "1:1";
        return f;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
