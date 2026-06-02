using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using lcdb.Optic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OtoCAD.Avalonia.Dialogs;

public partial class AssemblyWizardDialog : Window
{
    public OpticalAssembly? Result { get; private set; }

    private readonly ObservableCollection<LensRow> _items = new();
    private ItemsControl _itemsList = null!;

    public AssemblyWizardDialog()
    {
        InitializeComponent();

        _itemsList = this.FindControl<ItemsControl>("ItemsList")!;
        _itemsList.ItemsSource = _items;

        // 默认起步: Cooke triplet 预设
        ApplyCookePreset();

        this.FindControl<Button>("PresetSingleBtn")!.Click    += (_, _) => ApplySinglePreset();
        this.FindControl<Button>("PresetDoubletBtn")!.Click   += (_, _) => ApplyDoubletPreset();
        this.FindControl<Button>("PresetTripletBtn")!.Click   += (_, _) => ApplyCookePreset();
        this.FindControl<Button>("PresetObjectiveBtn")!.Click += (_, _) => ApplyObjectivePreset();
        this.FindControl<Button>("PresetClearBtn")!.Click     += (_, _) => { _items.Clear(); Reindex(); };

        this.FindControl<Button>("AddItemBtn")!.Click += (_, _) =>
        {
            _items.Add(new LensRow
            {
                Index = _items.Count + 1,
                GapBefore = 5, Diameter = 25.4, Thickness = 4,
                R1 = 50, R2 = -50, Material = "N-BK7",
            });
            Reindex();
        };

        this.FindControl<Button>("OkBtn")!.Click += (_, _) =>
        {
            Result = Build();
            Close();
        };
        this.FindControl<Button>("CancelBtn")!.Click += (_, _) => { Result = null; Close(); };
    }

    // 模板中删除按钮 (来自 XAML Click 绑定)
    public void OnRemoveItem(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is LensRow row)
        {
            _items.Remove(row);
            Reindex();
        }
    }

    private void Reindex()
    {
        for (int i = 0; i < _items.Count; i++) _items[i].Index = i + 1;
    }

    private void ApplySinglePreset()
    {
        _items.Clear();
        _items.Add(new LensRow { Index = 1, GapBefore = 5, Diameter = 25.4, Thickness = 5, R1 = 50, R2 = -50, Material = "N-BK7" });
    }

    private void ApplyDoubletPreset()
    {
        _items.Clear();
        _items.Add(new LensRow { Index = 1, GapBefore = 5, Diameter = 25.4, Thickness = 4, R1 = 60,  R2 = -40,  Material = "N-BK7" });
        _items.Add(new LensRow { Index = 2, GapBefore = 0, Diameter = 25.4, Thickness = 2.5, R1 = -40, R2 = -120, Material = "F2" });
    }

    private void ApplyCookePreset()
    {
        _items.Clear();
        _items.Add(new LensRow { Index = 1, GapBefore = 5, Diameter = 30, Thickness = 4, R1 = 80, R2 = -80, Material = "N-BK7" });
        _items.Add(new LensRow { Index = 2, GapBefore = 6, Diameter = 25, Thickness = 3, R1 = -50, R2 = 50, Material = "F2" });
        _items.Add(new LensRow { Index = 3, GapBefore = 6, Diameter = 30, Thickness = 4, R1 = 80, R2 = -80, Material = "N-BK7" });
    }

    private void ApplyObjectivePreset()
    {
        _items.Clear();
        _items.Add(new LensRow { Index = 1, GapBefore = 5, Diameter = 30, Thickness = 5, R1 = 60,  R2 = -40,  Material = "N-BK7" });
        _items.Add(new LensRow { Index = 2, GapBefore = 0, Diameter = 30, Thickness = 3, R1 = -40, R2 = -120, Material = "F2" });
        _items.Add(new LensRow { Index = 3, GapBefore = 15, Diameter = 25, Thickness = 4, R1 = 50, R2 = double.PositiveInfinity, Material = "N-BK7" });
    }

    private OpticalAssembly Build()
    {
        var asm = new OpticalAssembly();
        foreach (var row in _items)
        {
            var lens = new OpticalLens
            {
                Diameter = row.Diameter,
                Thickness = row.Thickness,
                R1 = row.R1,
                R2 = row.R2,
                MaterialName = row.Material ?? "N-BK7",
            };
            lens.ApplyMaterial(row.Material ?? "N-BK7");
            asm.Items.Add(new AssemblyItem { GapBefore = row.GapBefore, Lens = lens });
        }
        return asm;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public sealed class LensRow : System.ComponentModel.INotifyPropertyChanged
    {
        private int _index;
        private double _gap = 5;
        private double _diameter = 25.4;
        private double _thickness = 4;
        private double _r1 = 50;
        private double _r2 = -50;
        private string? _material = "N-BK7";

        public int Index { get => _index; set { _index = value; Notify(nameof(Index)); } }
        public double GapBefore { get => _gap; set { _gap = value; Notify(nameof(GapBefore)); } }
        public double Diameter { get => _diameter; set { _diameter = value; Notify(nameof(Diameter)); } }
        public double Thickness { get => _thickness; set { _thickness = value; Notify(nameof(Thickness)); } }
        public double R1 { get => _r1; set { _r1 = value; Notify(nameof(R1)); } }
        public double R2 { get => _r2; set { _r2 = value; Notify(nameof(R2)); } }
        public string? Material { get => _material; set { _material = value; Notify(nameof(Material)); } }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(n));
    }
}
