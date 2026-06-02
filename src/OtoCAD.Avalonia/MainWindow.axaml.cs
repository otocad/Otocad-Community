using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using LitMath;
using OtoCAD.Avalonia.Commands;
using OtoCAD.Avalonia.PropertyPanel;
using OtoCAD.Avalonia.Ribbon;
using OtoCAD.Avalonia.Services;
using lcdb;
using lcdb.Host;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OtoCAD.Avalonia;

// IAppHost: 免费内核给付费模块 OtoCAD.Cloud 的扩展宿主 (open-core, 见 docs/开发文档/开源-付费隔离设计.md).
public partial class MainWindow : Window, IAppHost
{
    private ToggleButton? reg_sbGridToggle;
    private ToggleButton? reg_sbSnapToggle;
    private readonly RecentFiles _recentFiles = new();
    private AutoSaveService? _autoSave;
    private const string BaseTitle = "OtoCAD Avalonia v0.3.1 — 光学 CAD";

    // MDI: 当前活动画布 + 接到 UI 的 per-canvas 订阅 (切 tab 时 Detach 旧 / Attach 新).
    private CadCanvas _activeCanvas = null!;
    public CadCanvas ActiveCanvas => _activeCanvas;
    private TabControl _docTabs = null!;
    private int _docCounter;
    private TextBlock _status = null!, _entityCount = null!, _mouseCoord = null!,
                      _scaleLabel = null!, _fpsLabel = null!, _selectionCount = null!, _modeText = null!;
    private PropertyPanelControl _propertyPanel = null!;
    private Border _frameModeBanner = null!;
    private readonly System.Collections.Generic.List<Action> _canvasSubs = new();
    // 当前活动画布的命令注册表; 切 tab 时整表重建 (闭包自然指向新画布), ribbon/快捷键经 ResolveDynamic 动态派发.
    private CommandRegistry _currentRegistry = null!;
    // 付费模块(OtoCAD.Cloud)经 IAppHost.RegisterCommand 注入的命令; 每次重建注册表时重新追加.
    private readonly List<Action<CommandRegistry>> _cmdProviders = new();

    public MainWindow()
    {
        InitializeComponent();

        var bootSettings = AppSettings.Load();
        DimensionStandardService.SetCurrent(bootSettings.DimensionStandard);

        _docTabs         = this.FindControl<TabControl>("DocTabs")!;
        _activeCanvas    = CreateDocumentTab("文档 1", select: true);
        _status          = this.FindControl<TextBlock>("StatusText")!;
        _entityCount     = this.FindControl<TextBlock>("EntityCountText")!;
        _mouseCoord      = this.FindControl<TextBlock>("MouseCoordText")!;
        _scaleLabel      = this.FindControl<TextBlock>("ScaleText")!;
        _fpsLabel        = this.FindControl<TextBlock>("FpsText")!;
        _selectionCount  = this.FindControl<TextBlock>("SelectionCountText")!;
        _modeText        = this.FindControl<TextBlock>("ModeText")!;
        _propertyPanel   = this.FindControl<PropertyPanelControl>("MainPropertyPanel")!;
        _frameModeBanner = this.FindControl<Border>("FrameModeBanner")!;
        var ribbon = this.FindControl<RibbonControl>("MainRibbon")!;

        // 命令注册 / ribbon 暂以初始画布为目标 (多画布命令重定向 = 后续 increment)
        var canvas = _activeCanvas;
        var status = _status;
        var entityCount = _entityCount;
        var propertyPanel = _propertyPanel;

        // 窗口级兜底: 画布无键盘焦点时 (如刚误点 ribbon「模式」进图框), ESC 仍能退出图框模式.
        // 画布有焦点时 CadCanvas.OnKeyDown 先处理并置 Handled, 这里 !e.Handled 自动跳过, 不重复.
        this.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Escape && !e.Handled && canvas.CurrentEditMode == lcdb.EntityEditMode.Frame)
            {
                canvas.CurrentEditMode = lcdb.EntityEditMode.Drawing;
                e.Handled = true;
            }
        }, global::Avalonia.Interactivity.RoutingStrategies.Bubble);

        _currentRegistry = BuildCommandRegistry(canvas, status, entityCount, propertyPanel);

        // open-core: 必须在 BuildRibbon 之前加载付费模块 OtoCAD.Cloud — 它经 RegisterCommand 注入
        // "Optic.ImportDesign" 等命令; ribbon 在构建时即解析命令名 (ResolveDynamic), 晚注册的命令
        // 不会被已生成的按钮接线 → 点击无反应. 免费版无该 dll 则静默跳过.
        TryLoadCloudModule();

        BuildRibbon(ribbon, _currentRegistry, canvas, status);
        WireKeyboardShortcuts();
        WireFloatingViewBar();
        WireStatusBarToggles(status);

        // 把当前画布接到 UI (状态栏/属性面板/标题/自动保存). 多 tab 切换时 Detach 旧 / Attach 新.
        AttachCanvas(_activeCanvas);

        // 默认场景 (仅初始画布)
        _activeCanvas.LoadTestScene();
        _entityCount.Text = $"实体: {_activeCanvas.EntityCount}";
        BindFirstLineToPropertyPanel(_activeCanvas, _propertyPanel);

        // tab 切换 → 切活动画布 (退订旧 / 重建注册表指向新 / 接线新). 初始 tab 已选, 此处后绑不会误触发.
        _docTabs.SelectionChanged += (_, _) =>
        {
            if (_docTabs.SelectedItem is TabItem ti && ti.Content is CadCanvas c)
                SwitchActiveCanvas(c);
        };

        // 启动后若发现自动备份, 提示恢复 (窗口显示后弹, 不阻塞构造)
        Opened += async (_, _) => await MaybeOfferRecoveryAsync();

        // 干净退出: 停 autosave + 清掉备份 (没崩就无需恢复 → 下次启动不再提示).
        // 仅正常关窗会走到这里; 进程崩溃不触发 → 备份留存 → 下次启动提示恢复.
        Closed += (_, _) => { DetachCanvas(); AutoSaveService.PurgeBackups(); };
    }

    private bool _recoveryOffered;

    /// <summary>启动时若 autosave 目录有备份, 提示是否恢复最新一份 (恢复到当前画布, 不绑定备份路径).</summary>
    private async Task MaybeOfferRecoveryAsync()
    {
        if (_recoveryOffered) return;
        _recoveryOffered = true;

        var backups = AutoSaveService.FindBackups();
        if (backups.Count == 0) return;

        var newest = backups[0];
        var choice = await Dialogs.ConfirmDialog.ShowAsync(this, "恢复自动备份",
            $"发现 {backups.Count} 份自动备份。要恢复最新的一份吗？\n\n{Path.GetFileName(newest)}",
            "恢复最新", "忽略");
        if (choice != "恢复最新") return;

        try
        {
            _activeCanvas.LoadOtocadFile(newest, asRecovery: true);
            _entityCount.Text = $"实体: {_activeCanvas.EntityCount}";
            _status.Text = $"已从自动备份恢复 (请另存到正式文件): {Path.GetFileName(newest)}";
        }
        catch (Exception ex)
        {
            _status.Text = $"恢复失败: {ex.Message}";
        }
    }

    /// <summary>新建一个文档 tab (独立 CadCanvas), 可选立即切到它. 返回该画布.</summary>
    private CadCanvas CreateDocumentTab(string title, bool select)
    {
        _docCounter++;
        var c = new CadCanvas();
        var item = new TabItem
        {
            Content = c,
            FontSize = 12,                                   // Fluent 默认 tab 头偏大, 调小
            Padding = new global::Avalonia.Thickness(10, 4),
            MinHeight = 28,
        };
        item.Header = BuildTabHeader(title, item);
        _docTabs.Items.Add(item);
        if (select) _docTabs.SelectedItem = item;
        return c;
    }

    /// <summary>tab 头: 标题 + 关闭按钮 (✕). 至少保留一个文档时才允许关闭.</summary>
    private Control BuildTabHeader(string title, TabItem item)
    {
        var text = new TextBlock
        {
            Text = title,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        };
        var close = new Button
        {
            Content = "✕",
            FontSize = 10,
            Padding = new global::Avalonia.Thickness(3, 0),
            Margin = new global::Avalonia.Thickness(6, 0, 0, 0),
            Background = global::Avalonia.Media.Brushes.Transparent,
            BorderThickness = new global::Avalonia.Thickness(0),
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            MinWidth = 0,
            MinHeight = 0,
        };
        close.Click += async (_, e) => { e.Handled = true; await CloseTabAsync(item); };
        var sp = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal };
        sp.Children.Add(text);
        sp.Children.Add(close);
        return sp;
    }

    /// <summary>
    /// 关闭一个文档 tab; 有未保存改动先确认 (保存/不保存/取消); 关的是当前 tab 则先切到邻居 (右优先),
    /// 始终保留至少一个文档.
    /// </summary>
    private async Task CloseTabAsync(TabItem item)
    {
        if (_docTabs.Items.Count <= 1) return;            // 保留至少一个文档

        if (item.Content is CadCanvas c && c.IsDirty)
        {
            var name = c.CurrentFilePath is string p ? Path.GetFileName(p) : "未命名文档";
            var choice = await Dialogs.ConfirmDialog.ShowAsync(this, "关闭文档",
                $"“{name}”有未保存的更改。\n要保存吗？", "保存", "不保存", "取消");
            if (choice is null or "取消") return;
            if (choice == "保存")
            {
                await SaveOrSaveAsAsync(c, _status, forceSaveAs: false);
                if (c.IsDirty) return;                    // 保存被取消/失败 → 不关闭
            }
        }

        int idx = _docTabs.Items.IndexOf(item);
        if (idx < 0) return;
        if (ReferenceEquals(_docTabs.SelectedItem, item))
        {
            int neighbor = idx < _docTabs.Items.Count - 1 ? idx + 1 : idx - 1;
            _docTabs.SelectedItem = _docTabs.Items[neighbor];   // 触发 SwitchActiveCanvas → Detach 旧/Attach 新
        }
        _docTabs.Items.Remove(item);
        (item.Content as IDisposable)?.Dispose();
    }

    private void UpdateTitle()
    {
        var name = ActiveCanvas.CurrentFilePath is string p ? Path.GetFileName(p) : "未命名";
        var dirty = ActiveCanvas.IsDirty ? " *" : "";
        Title = $"{name}{dirty} — {BaseTitle}";
    }

    /// <summary>把一个画布接到 UI (状态栏/属性面板/标题/自动保存); 订阅记进 _canvasSubs 供 Detach 退订.</summary>
    private void AttachCanvas(CadCanvas c)
    {
        Action<string?> onFile = _ => UpdateTitle();
        c.FilePathChanged += onFile; _canvasSubs.Add(() => c.FilePathChanged -= onFile);
        Action<bool> onDirty = _ => UpdateTitle();
        c.DirtyChanged += onDirty; _canvasSubs.Add(() => c.DirtyChanged -= onDirty);

        // 改属性后: 重绘; 选中透镜则刷新其关联 auto-dim (associativity)
        _propertyPanel.SetChangeNotifier(() =>
        {
            var sel = c.SelectedEntities.FirstOrDefault();
            if (sel is lcdb.Optic.OpticalLens or lcdb.Optic.CementedLens)
                c.RefreshAutoDimensionsFor(sel);
            c.RequestRedraw();
        });

        Action onSel = () =>
        {
            var first = c.SelectedEntities.FirstOrDefault();
            _propertyPanel.SetEntity(first, c.SelectionCount);
            _selectionCount.Text = $"选中: {c.SelectionCount}";
        };
        c.SelectionChanged += onSel; _canvasSubs.Add(() => c.SelectionChanged -= onSel);

        // 实体被外部修改 (grip/undo/redo/命令) 时重建属性面板反映新值
        Action onEnt = () =>
        {
            var first = c.SelectedEntities.FirstOrDefault();
            if (first is not null) _propertyPanel.SetEntity(first, c.SelectionCount);
        };
        c.EntitiesChanged += onEnt; _canvasSubs.Add(() => c.EntitiesChanged -= onEnt);

        Action<(double X, double Y)> onMouse = mp => _mouseCoord.Text = $"鼠标: {mp.X:F2}, {mp.Y:F2}";
        c.MouseModelPositionChanged += onMouse; _canvasSubs.Add(() => c.MouseModelPositionChanged -= onMouse);

        Action<double> onScale = s => _scaleLabel.Text = $"缩放: {s * 100:F0}%";
        c.ScaleChanged += onScale; _canvasSubs.Add(() => c.ScaleChanged -= onScale);

        Action<double> onFps = fps => _fpsLabel.Text = $"FPS: {fps:F0}";
        c.FpsUpdated += onFps; _canvasSubs.Add(() => c.FpsUpdated -= onFps);

        Action<string> onPrompt = text => _status.Text = text;
        c.PromptChanged += onPrompt; _canvasSubs.Add(() => c.PromptChanged -= onPrompt);

        Action<lcdb.Entity?> onHover = e =>
        {
            if (c.PromptText is null && e is not null) _status.Text = $"↳ 悬停: {e.GetType().Name}";
        };
        c.HoverEntityChanged += onHover; _canvasSubs.Add(() => c.HoverEntityChanged -= onHover);

        Action<lcdb.EntityEditMode> onMode = m =>
        {
            bool isFrame = m == lcdb.EntityEditMode.Frame;
            _modeText.Text = isFrame ? "模式: 图框" : "模式: 绘图";
            _frameModeBanner.IsVisible = isFrame;
        };
        c.CurrentEditModeChanged += onMode; _canvasSubs.Add(() => c.CurrentEditModeChanged -= onMode);

        // 每画布独立 AutoSave (30s, 仅 IsDirty 时写)
        _autoSave = new AutoSaveService(
            isDirty: () => c.IsDirty,
            currentFilePath: () => c.CurrentFilePath,
            save: path => c.SaveAutoBackup(path),
            onStatusReport: msg => _status.Text = msg);
        _autoSave.Start();

        UpdateTitle();
    }

    /// <summary>退订当前活动画布的全部 UI 订阅 + 停其 AutoSave (切 tab / 关窗调用).</summary>
    private void DetachCanvas()
    {
        foreach (var unsub in _canvasSubs) unsub();
        _canvasSubs.Clear();
        _autoSave?.Dispose();
        _autoSave = null;
    }

    /// <summary>
    /// ribbon/快捷键的稳定命令派发: 已知命令返回一个在点击时才查 _currentRegistry 的包装,
    /// 这样切 tab 重建注册表后, 同一个 ribbon 按钮自动指向新活动画布的命令.
    /// 未知命令返回 null (交由 onUnknown 显示占位).
    /// </summary>
    private Action? ResolveDynamic(string id)
        => _currentRegistry.Resolve(id) is null
            ? null
            : () => _currentRegistry.Resolve(id)?.Invoke();

    /// <summary>切到另一个画布作为活动文档: 退订旧 → 重建注册表(指向新) → 接线新 → 同步状态栏.</summary>
    private void SwitchActiveCanvas(CadCanvas newCanvas)
    {
        if (ReferenceEquals(newCanvas, _activeCanvas)) return;
        DetachCanvas();
        _activeCanvas = newCanvas;
        _currentRegistry = BuildCommandRegistry(_activeCanvas, _status, _entityCount, _propertyPanel);
        AttachCanvas(_activeCanvas);
        if (reg_sbGridToggle is not null) reg_sbGridToggle.IsChecked = _activeCanvas.GridVisible;
        if (reg_sbSnapToggle is not null) reg_sbSnapToggle.IsChecked = _activeCanvas.SnapEnabled;
        _entityCount.Text = $"实体: {_activeCanvas.EntityCount}";
        BindFirstLineToPropertyPanel(_activeCanvas, _propertyPanel);
    }

    /// <summary>
    /// POC-04: 取 scene 第一个 Line, 绑定到 PropertyPanel. Phase 1F 改走 SetEntity 通用路径.
    /// </summary>
    private void BindFirstLineToPropertyPanel(CadCanvas canvas, PropertyPanelControl propertyPanel)
    {
        propertyPanel.SetEntity(canvas.GetFirstLine());
    }

    /// <summary>Phase 1E: 打开 URL 跨平台 (Win/Mac/Linux 均使用系统默认浏览器).</summary>
    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { /* 无浏览器或权限不足时静默 */ }
    }

    private CommandRegistry BuildCommandRegistry(CadCanvas canvas, TextBlock status, TextBlock entityCount, PropertyPanelControl propertyPanel)
    {
        var reg = new CommandRegistry();

        // 绘图命令 (ICadCommand 多步交互, 经 starter 注入 canvas.StartCommand)
        reg.RegisterCadCommand("Draw.Line", () => new LineCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Circle", () => new CircleCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Arc", () => new ArcCmd(), canvas.StartCommand);
        // Phase 1B: 9 个绘图命令
        reg.RegisterCadCommand("Draw.Point", () => new PointCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Rectangle", () => new RectangleCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Polyline", () => new PolylineCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Polygon", () => new PolygonCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Ellipse", () => new EllipseCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Ray", () => new RayCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Xline", () => new XlineCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Spline", () => new SplineCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Text", () => new TextCmd(), canvas.StartCommand);

        // Phase 1D: 标注命令 (6 个核心)
        reg.RegisterCadCommand("Dim.Aligned", () => new AlignedDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Linear", () => new LinearDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Radial", () => new RadialDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Diametric", () => new DiametricDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Angular3", () => new Angular3PointDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Ordinate", () => new OrdinateDimensionCmd(), canvas.StartCommand);

        // Phase 1E: 文本扩展
        reg.RegisterCadCommand("Draw.MText", () => new MTextCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Draw.Leader", () => new LeaderCmd(), canvas.StartCommand);

        // Phase 1E: Frame 模式 + Layer/Block/Image/Wipeout/Symbol/Tolerance/Lens 占位
        // 这些功能需要专门面板/向导/数据结构, v0.1 仅注册命令名占位, Phase 2 接入完整实现
        void Stub(string id, string label) =>
            reg.Register(id, () => status.Text = $"[{label}] Phase 2 接入 (需专用面板/向导, MVP 占位)");

        // Phase 1E 实做: Frame 模式
        reg.Register("Frame.ToggleMode", () =>
        {
            canvas.ToggleFrameMode();
            status.Text = canvas.CurrentEditMode == lcdb.EntityEditMode.Frame
                ? "已切到 图框 模式 (新实体标 Frame)"
                : "已切到 绘图 模式";
        });
        reg.Register("Frame.NewFrame", () => _ = StartFrameAsync(canvas, status));

        // T5: 光学图框 — 自带 "对材料的要求" + "对零件的要求" 双表 (GB/T 13323-2009)
        // 简化版: 默认 A4 横向, 模板克隆即放置 (避免再加一个 wizard 拖时间)
        reg.Register("Frame.NewOpticalFrame", () =>
        {
            var frameLayer = canvas.GetOrCreateLayer("图框");
            frameLayer.IsLocked = true;
            var template = lcdb.DrawingFrame.DrawingFrameTemplates.CreateOptical(
                lcdb.DrawingFrame.DrawingFrameTemplates.PaperSize.A4,
                lcdb.DrawingFrame.DrawingFrameTemplates.Orientation.Landscape);
            template.DrawingTitle = "光学零件图";
            var cmd = new Commands.PlaceMarkCmd("光学图框", p =>
            {
                var f = (lcdb.DrawingFrame.OpticalDrawingFrame)template.Clone();
                f.Origin = p;
                return f;
            });
            void OnPlaced()
            {
                canvas.EntitiesChanged -= OnPlaced;
                var lastFrame = canvas.GetAllEntities().OfType<lcdb.DrawingFrame.OpticalDrawingFrame>().LastOrDefault();
                if (lastFrame is not null)
                {
                    canvas.AssignEntityToLayer(lastFrame, frameLayer);
                    canvas.RequestRedraw();
                }
            }
            canvas.EntitiesChanged += OnPlaced;
            canvas.StartCommand(cmd);
            status.Text = "[光学图框] A4 横向, 含 GB/T 13323-2009 双表; 在画布上单击放置";
        });

        // T6: 关联当前选中的 OpticalLens / CementedLens 到光学图框 — 双表自动填透镜数据
        reg.Register("Frame.LinkLens", () =>
        {
            var sel = canvas.SelectedEntities.ToList();
            var lens = sel.FirstOrDefault(e =>
                e is lcdb.Optic.OpticalLens || e is lcdb.Optic.CementedLens);
            var frame = sel.OfType<lcdb.DrawingFrame.OpticalDrawingFrame>().FirstOrDefault()
                     ?? canvas.GetAllEntities().OfType<lcdb.DrawingFrame.OpticalDrawingFrame>().LastOrDefault();
            if (lens is null || frame is null)
            {
                status.Text = "[关联透镜→图框] 请选中一个透镜 + 一个光学图框 (或已有光学图框)";
                return;
            }
            frame.LinkedLens = lens;
            frame.InvalidateRenderCache();
            canvas.RequestRedraw();
            status.Text = $"[关联透镜→图框] 已关联 {lens.GetType().Name} → 双表数据自动填充";
        });

        // Phase 1E 实做: Layer CRUD
        reg.Register("Layer.Add", () =>
        {
            var name = canvas.AddLayer();
            var total = canvas.ListLayers().Count();
            status.Text = name is null
                ? "[图层] 数据库不可用"
                : $"[图层] 已新增 '{name}' (共 {total} 个图层)";
        });
        reg.Register("Layer.Remove", () =>
        {
            // 简化: 删除最后一个 (非 0 层) — 完整 UI 留 Phase 2
            var last = canvas.ListLayers().LastOrDefault(n => n != "0");
            if (last is null) { status.Text = "[图层] 没有可删除的图层"; return; }
            if (canvas.RemoveLayer(last))
                status.Text = $"[图层] 已删除 '{last}'";
            else
                status.Text = $"[图层] 删除 '{last}' 失败";
        });
        Stub("Layer.Modify", "修改图层 (需层管理面板)");

        // 光学元素 (需配置向导)
        // 光学元件: 单透镜 / 双胶合透镜 / 通用透镜系统 / 棱镜 (T8)
        // 快速构建: 一键生成 ISO 10110 竖版标准图纸. 选中双胶合 → 双胶合图纸;
        // 选中单透镜 → 按其参数; 都没选 → 平凸样例.
        reg.Register("Optic.StandardSheet", () =>
        {
            var cemented = canvas.SelectedEntities.FirstOrDefault(e => e is lcdb.Optic.CementedLens) as lcdb.Optic.CementedLens;
            if (cemented is not null)
            {
                canvas.LoadGeneratedSheet(IsoSingleLensSheet.BuildDoublet(
                    cemented, $"{cemented.Material1}+{cemented.Material2} Doublet", "OtoCAD", "1:1",
                    "Cemented doublet; protect cement joint from solvents."));
                status.Text = "[标准图纸] 已按选中双胶合透镜生成 ISO 10110 标准图纸";
                return;
            }
            var sel = canvas.SelectedEntities.FirstOrDefault(e => e is lcdb.Optic.OpticalLens) as lcdb.Optic.OpticalLens;
            var spec = sel is null
                ? new IsoSingleLensSheet.Spec()
                : IsoSingleLensSheet.SpecFromLens(sel);
            canvas.LoadGeneratedSheet(IsoSingleLensSheet.Build(spec));
            status.Text = sel is null
                ? "[标准图纸] 已生成 ISO 10110 单透镜样例图纸 (平凸 PCX-001)"
                : "[标准图纸] 已按选中透镜生成 ISO 10110 标准图纸";
        });

        reg.Register("Optic.SingleLens", () => _ = StartSingleLensAsync(canvas, status));
        reg.Register("Optic.DoubletLens", () => _ = StartDoubletLensAsync(canvas, status));
        reg.Register("Optic.Lens", () => _ = StartAssemblyAsync(canvas, status));
        reg.Register("Optic.NewPrism", () => _ = StartPrismAsync(canvas, status));
        // Optic.ImportDesign 由付费模块 OtoCAD.Cloud 经 IAppHost.RegisterCommand 注入 (免费版无此命令).

        // T2: 自动尺寸标注 — 对当前选中的 OpticalLens / CementedLens 一键生成 R/d/φ
        reg.Register("Optic.AutoDim", () =>
        {
            var sel = canvas.SelectedEntities.FirstOrDefault(e =>
                e is lcdb.Optic.OpticalLens || e is lcdb.Optic.CementedLens);
            if (sel is null)
            {
                status.Text = "[自动标注] 请先选中一个透镜 (OpticalLens / CementedLens)";
                return;
            }
            var dims = AutoDimensionLensCmd.Build(sel);
            if (dims.Count == 0)
            {
                status.Text = "[自动标注] 当前实体类型暂不支持";
                return;
            }
            // 标记 Owner = 源 lens, 让 PropertyPanel 改 lens 参数时能找到这批 dim 重生成 (associative)
            foreach (var d in dims) d.Owner = sel;
            // 批量添加 — 单次 Ctrl+Z 全部回退 (而非 N 次)
            canvas.AddEntitiesBatch(dims);
            status.Text = $"[自动标注] 已添加 {dims.Count} 个尺寸标注 - 当前标准: {DimensionStandardService.GetDisplayName(DimensionStandardService.CurrentKey)}";
        });

        void SetDimStandard(string key)
        {
            var current = AppSettings.Load();
            current.DimensionStandard = DimensionStandardService.NormalizeKey(key);
            current.Save();
            DimensionStandardService.SetCurrent(current.DimensionStandard);
            status.Text = $"[标注标准] 已切换到 {DimensionStandardService.GetDisplayName(current.DimensionStandard)}";
        }
        reg.Register("Dim.Standard.Standard", () => SetDimStandard(DimensionStandardService.Standard));
        reg.Register("Dim.Standard.ISO25", () => SetDimStandard(DimensionStandardService.Iso25));
        reg.Register("Dim.Standard.GB", () => SetDimStandard(DimensionStandardService.Gb));
        reg.Register("Dim.Standard.GBOptical", () => SetDimStandard(DimensionStandardService.GbOptical));
        reg.Register("Dim.Standard.Company", () => SetDimStandard(DimensionStandardService.Company));

        // Insert.Image / Insert.Wipeout 已在上面实做; 其他留占位
        reg.RegisterCadCommand("Insert.Wipeout", () => new WipeoutCmd(), canvas.StartCommand);
        Stub("Insert.Block", "插入块");
        Stub("Insert.Symbol", "插入符号");

        // 公差 — 新增: 选中尺寸标注 → 弹对话框设公称/偏差 → 画布单击放置 ToleranceAnnotation
        reg.Register("Tolerance.Add", () => _ = StartAddToleranceAsync(canvas, status));
        Stub("Tolerance.Quick", "快速公差");
        Stub("Tolerance.Edit", "编辑公差");

        // 其他 dimension — 几何驱动 (选实体多步交互) 走 ICadCommand
        reg.RegisterCadCommand("Dim.Angular2Line", () => new Angular2LineDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Sagitta", () => new SagittaDimensionCmd(), canvas.StartCommand);
        reg.RegisterCadCommand("Dim.Leader", () => new LeaderCmd(), canvas.StartCommand);

        // 透镜厚度标注 — 选中 OpticalLens/CementedLens 一键生成 (同 Optic.AutoDim 取选中实体)
        void DimSelectedLens(string label, Func<lcdb.Entity, lcdb.LinearDimension?> build)
        {
            var sel = canvas.SelectedEntities.FirstOrDefault(e =>
                e is lcdb.Optic.OpticalLens || e is lcdb.Optic.CementedLens);
            if (sel is null) { status.Text = $"[{label}] 请先选中一个透镜 (OpticalLens / CementedLens)"; return; }
            var dim = build(sel);
            if (dim is null) { status.Text = $"[{label}] 当前实体类型暂不支持"; return; }
            dim.Owner = sel;  // associative: 改 lens 参数时可重生成
            canvas.AddEntitiesBatch(new lcdb.Entity[] { dim });
            status.Text = $"[{label}] 已添加 (拖 grip 可调位置)";
        }
        reg.Register("Dim.CenterThickness", () => DimSelectedLens("中心厚度", AutoDimensionLensCmd.BuildCenterThickness));
        reg.Register("Dim.EdgeThickness", () => DimSelectedLens("边缘厚度", AutoDimensionLensCmd.BuildEdgeThickness));

        // Phase 1D: 额外 Annotation Mark 模板单点放置
        void RegMark(string id, string name, System.Func<Vector2, lcdb.Entity> factory) =>
            reg.RegisterCadCommand($"Mark.{id}",
                () => new PlaceMarkCmd(name, factory),
                canvas.StartCommand);

        // 贴面型标记: 走 PlaceSurfaceMarkCmd, 移到面上时自动贴面。
        // 工艺/符号类符号坐在面上、沿外法线竖立; 面形/表面质量类代码框保持水平、贴到面外侧。
        // 标记实体需实现 ISurfaceAttachable; 否则退化为普通单点放置。
        void RegSurfaceMark(string id, string name, System.Func<Vector2, lcdb.Entity> factory) =>
            reg.RegisterCadCommand($"Mark.{id}",
                () => new PlaceSurfaceMarkCmd(name, factory),
                canvas.StartCommand);

        // 工艺类 (贴面: 符号坐在面上沿外法线竖立)
        RegSurfaceMark("Blackening", "涂墨", p => new lcdb.Annotation.BlackeningMark(p, 3.0));
        RegSurfaceMark("Polishing", "抛光", p => new lcdb.Annotation.PolishingMark(p, 10.0));
        RegSurfaceMark("Coating", "镀膜", p => new lcdb.Annotation.CoatingMark(p, lcdb.Annotation.CoatingType.AR, 10.0) { ShowText = false });
        RegSurfaceMark("Sandblasting", "喷砂", p => new lcdb.Annotation.SandblastingMark(p, 10.0));
        RegSurfaceMark("DiamondTurning", "金刚石车削", p => new lcdb.Annotation.DiamondTurningMark(p, 10.0));
        RegSurfaceMark("Grinding", "研磨", p => new lcdb.Annotation.GrindingMark(p, 10.0));
        RegSurfaceMark("SurfaceRoughness", "表面粗糙度", p => new lcdb.Annotation.SurfaceRoughnessMark(p, 10.0));

        // 多参数 (Position) — 用默认值
        RegMark("EffectiveAperture", "有效孔径", p => new lcdb.Annotation.EffectiveApertureMark(p, 18.0));
        RegMark("FocalPoint", "焦点", p => new lcdb.Annotation.FocalPointMark(p));
        RegMark("CenterDeviation", "中心偏差", p => new lcdb.Annotation.CenterDeviationMark(p, 0.0, 0.0));
        RegMark("CenteringTolerance", "对中公差", p => new lcdb.Annotation.CenteringToleranceMark(p, 0.0, 0.0));
        // 面形/表面质量类 (贴面: 代码框保持水平、贴到面外侧)
        RegSurfaceMark("FormAccuracy", "面形精度", p => new lcdb.Annotation.FormAccuracyMark(p, "0", "", "632.8"));
        RegSurfaceMark("SurfaceForm", "面形", p => new lcdb.Annotation.SurfaceFormMark(p, "0", 0.0));
        RegSurfaceMark("SurfaceImperfection", "表面缺陷", p => new lcdb.Annotation.SurfaceImperfectionMark(p, 0.0, 0.0, 0.0));
        RegSurfaceMark("SurfaceQuality", "表面质量", p => new lcdb.Annotation.SurfaceQualityMark(p, "0", "0"));
        RegSurfaceMark("SurfaceTexture", "表面纹理", p => new lcdb.Annotation.SurfaceTextureMark(p, 0.0, 0.0));

        // ISO10110 系列
        RegMark("ISO2", "ISO10110-2 双折射", p => new lcdb.Annotation.ISO10110_2Mark(p, 0.0));
        RegMark("ISO3", "ISO10110-3 气泡", p => new lcdb.Annotation.ISO10110_3Mark(p, 0, 0.0));
        RegMark("ISO4", "ISO10110-4 不均匀性", p => new lcdb.Annotation.ISO10110_4Mark(p, 0, ""));
        RegMark("ISO12", "ISO10110-12 非球面", p => new lcdb.Annotation.ISO10110_12Mark(p, 0.0, 0.0));
        RegMark("ISO14", "ISO10110-14 波前", p => new lcdb.Annotation.ISO10110_14Mark(p, 0.0));
        RegMark("LaserDamage", "激光损伤阈值", p => new lcdb.Annotation.LaserDamageThresholdMark(p, 0.0, 1064.0, 0.0));

        // 复杂枚举/字符串 → noargs ctor + Position 反射赋值
        RegMark("Assembly", "装配", p => SetPositionVia(new lcdb.Annotation.AssemblyMark(), p));
        RegMark("Inspection", "检验", p => SetPositionVia(new lcdb.Annotation.InspectionMark(), p));
        RegMark("MaterialDefect", "材料缺陷", p => SetPositionVia(new lcdb.Annotation.MaterialDefectMark(), p));
        RegMark("Processing", "加工", p => SetPositionVia(new lcdb.Annotation.ProcessingMark(), p));
        RegMark("OpticalAxis", "光轴", p =>
        {
            var m = new lcdb.Annotation.OpticalAxisMark();
            // 默认沿 X 正方向 50 长度的光轴
            var t = m.GetType();
            t.GetProperty("StartPosition")?.SetValue(m, p);
            t.GetProperty("EndPosition")?.SetValue(m, new Vector2(p.X + 50, p.Y));
            return m;
        });

        // 场景
        reg.Register("Scene.LoadTest", () =>
        {
            canvas.LoadTestScene();
            entityCount.Text = $"实体: {canvas.EntityCount}";
            status.Text = "已加载测试场景";
            BindFirstLineToPropertyPanel(canvas, propertyPanel);
        });
        reg.Register("Scene.Benchmark1k", () =>
        {
            canvas.LoadBenchmarkScene(1000);
            entityCount.Text = $"实体: {canvas.EntityCount}";
            status.Text = "已加载基准场景 (1000 线)";
            BindFirstLineToPropertyPanel(canvas, propertyPanel);
        });
        reg.Register("Scene.Benchmark10k", () =>
        {
            canvas.LoadBenchmarkScene(10000);
            entityCount.Text = $"实体: {canvas.EntityCount}";
            status.Text = "已加载压力场景 (10000 线)";
            BindFirstLineToPropertyPanel(canvas, propertyPanel);
        });

        // 视图
        reg.Register("View.Reset", () =>
        {
            canvas.ResetView();
            status.Text = "视图已重置";
        });
        reg.Register("View.RoundtripSelfTest", () =>
        {
            try
            {
                var result = canvas.RoundtripSelfTest();
                entityCount.Text = $"实体: {canvas.EntityCount}";
                status.Text = result;
            }
            catch (Exception ex)
            {
                status.Text = $"Round-trip 失败: {ex.Message}";
            }
        });

        // 编辑 (Phase 1.7 Undo/Redo)
        reg.Register("Edit.Undo", () =>
        {
            if (canvas.Undo())
            {
                entityCount.Text = $"实体: {canvas.EntityCount}";
                status.Text = $"已撤销 (剩 {canvas.UndoCount} 步可撤销, {canvas.RedoCount} 步可重做)";
            }
            else
                status.Text = "无可撤销操作";
        });
        reg.Register("Edit.Redo", () =>
        {
            if (canvas.Redo())
            {
                entityCount.Text = $"实体: {canvas.EntityCount}";
                status.Text = $"已重做 (剩 {canvas.UndoCount} 步可撤销, {canvas.RedoCount} 步可重做)";
            }
            else
                status.Text = "无可重做操作";
        });

        // Phase 1A: Move 命令 (基点 → 目标点 移动选中)
        reg.RegisterCadCommand("Edit.Move",
            () => new MoveCmd(canvas.SelectedEntities, canvas.CommitMove),
            canvas.StartCommand);
        // Phase 1C: Copy / Rotate / Scale / Mirror / Offset
        reg.RegisterCadCommand("Edit.Copy",
            () => new CopyCmd(canvas.SelectedEntities, canvas.CommitCopy),
            canvas.StartCommand);
        reg.RegisterCadCommand("Edit.Rotate",
            () => new RotateCmd(canvas.SelectedEntities, canvas.CommitTransform),
            canvas.StartCommand);
        reg.RegisterCadCommand("Edit.Scale",
            () => new ScaleCmd(canvas.SelectedEntities, canvas.CommitTransform),
            canvas.StartCommand);
        reg.RegisterCadCommand("Edit.Mirror",
            () => new MirrorCmd(canvas.SelectedEntities, canvas.CommitTransform),
            canvas.StartCommand);
        reg.RegisterCadCommand("Edit.Offset",
            () => new OffsetCmd(canvas.SelectedEntities, canvas.CommitCopy),
            canvas.StartCommand);

        // Phase 1A: 删除选中
        reg.Register("Edit.Delete", () =>
        {
            if (canvas.DeleteSelected())
            {
                entityCount.Text = $"实体: {canvas.EntityCount}";
                status.Text = "已删除选中";
            }
            else
                status.Text = "无选中实体";
        });
        reg.Register("Edit.SelectNone", () =>
        {
            canvas.ClearSelection();
            status.Text = "已取消选择";
        });
        reg.Register("Edit.SelectAll", () =>
        {
            canvas.SelectAll();
            status.Text = $"已全选 ({canvas.SelectionCount} 个实体)";
        });

        // Grid 开关 — ribbon 按钮 / F7 / 状态栏 ToggleButton 三处入口共享
        reg.Register("View.ToggleGrid", () =>
        {
            canvas.ToggleGrid();
            status.Text = canvas.GridVisible ? "网格已显示" : "网格已隐藏";
            // 同步状态栏 Toggle (反过来 Toggle 自己触发时会跳过这条调用 — 由
            // WireStatusBarToggles 的 if 防回环)
            if (reg_sbGridToggle is not null && reg_sbGridToggle.IsChecked != canvas.GridVisible)
                reg_sbGridToggle.IsChecked = canvas.GridVisible;
        });

        // Snap 开关 — 同上三处入口
        reg.Register("View.ToggleSnap", () =>
        {
            canvas.ToggleSnap();
            status.Text = canvas.SnapEnabled ? "对象捕捉已开" : "对象捕捉已关";
            if (reg_sbSnapToggle is not null && reg_sbSnapToggle.IsChecked != canvas.SnapEnabled)
                reg_sbSnapToggle.IsChecked = canvas.SnapEnabled;
        });

        // Phase 1A: View zoom
        reg.Register("View.ZoomExtents", () =>
        {
            canvas.ResetView();
            status.Text = "已缩放到全部";
        });
        reg.Register("View.ZoomIn", () =>
        {
            canvas.ZoomBy(1.25f);
            status.Text = $"放大 (缩放 {canvas.CurrentScale * 100:F0}%)";
        });
        reg.Register("View.ZoomOut", () =>
        {
            canvas.ZoomBy(1.0f / 1.25f);
            status.Text = $"缩小 (缩放 {canvas.CurrentScale * 100:F0}%)";
        });
        reg.Register("View.ZoomToSelected", () =>
        {
            if (canvas.SelectionCount == 0)
                status.Text = "[缩放到选中] 未选中实体 — 已 fallback 到全部";
            else
                status.Text = $"[缩放到选中] {canvas.SelectionCount} 个实体";
            canvas.ZoomToSelected();
        });
        reg.Register("View.ZoomOneToOne", () =>
        {
            canvas.ZoomToOneToOne();
            status.Text = "缩放 1:1 (实际尺寸, 1 模型单位 = 1 像素)";
        });

        // 文件
        reg.Register("File.New", () =>
        {
            canvas.NewScene();
            entityCount.Text = $"实体: {canvas.EntityCount}";
            status.Text = "新场景";
        });
        reg.Register("File.Open", () => _ = LoadFileAsync(canvas, status, entityCount));
        reg.Register("File.Save", () => _ = SaveOrSaveAsAsync(canvas, status, forceSaveAs: false));
        reg.Register("File.SaveAs", () => _ = SaveOrSaveAsAsync(canvas, status, forceSaveAs: true));
        // Phase 1F: 导出 PNG
        reg.Register("File.ExportPng", () => _ = ExportPngAsync(canvas, status));
        // T10/T11: 导出 DXF / PDF
        reg.Register("File.ExportDxf", () => _ = ExportDxfAsync(canvas, status));
        reg.Register("File.ExportPdf", () => _ = ExportPdfAsync(canvas, status));
        // Week 14: 打印 (走 PDF + ShellExecute "print" verb, 跨平台)
        reg.Register("File.Print", () => _ = PrintAsync(canvas, status));

        // T2: Recent Files (按 index, 0=最近, 1=次近, ... 4)
        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            reg.Register($"File.OpenRecent{idx}", () => _ = OpenRecentAsync(canvas, status, entityCount, idx));
        }

        // Phase 1E 实做: 帮助 (打开浏览器)
        reg.Register("Help.About", () => _ = OpenAboutAsync());
        reg.Register("Help.Docs", () => OpenUrl("https://github.com/otocad/Otocad-Community/blob/master/README.md"));
        reg.Register("Help.Feedback", () => OpenUrl("https://github.com/otocad/Otocad-Community/issues/new"));

        // T8: 设置对话框
        reg.Register("Tools.Settings", () => _ = OpenSettingsAsync(status));

        // Phase 1E 实做: 插入图像
        reg.RegisterCadCommand("Insert.Image", () => new InsertImageCmd(this), canvas.StartCommand);

        // 付费模块(OtoCAD.Cloud)注入的命令 — 每次重建注册表都重新追加, 切 tab 后仍有效.
        foreach (var p in _cmdProviders) p(reg);

        return reg;
    }

    private void BuildRibbon(RibbonControl ribbon, CommandRegistry registry, CadCanvas canvas, TextBlock status)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "RibbonLayout.json");

        if (File.Exists(configPath))
        {
            try
            {
                var layout = RibbonConfigLoader.LoadFromFile(configPath);
                RibbonConfigLoader.Apply(
                    ribbon,
                    layout,
                    commandResolver: ResolveDynamic,
                    onUnknown: name => status.Text = $"[Ribbon] 未实现: {name} (Phase 1 待接入)");
                return;
            }
            catch (Exception ex)
            {
                status.Text = $"[Ribbon] 配置加载失败, 回退默认布局: {ex.Message}";
            }
        }

        // 回退: 单 Tab 单组单按钮, 仅保证 app 能启动
        var homeTab = ribbon.AddTab("主页");
        var drawGroup = homeTab.AddGroup("绘图");
        drawGroup.AddButton("直线", "📏", registry.Resolve("Draw.Line")!);
        ribbon.RefreshActiveTab();
        status.Text = "[Ribbon] 回退布局 (Config/RibbonLayout.json 缺失)";
    }

    /// <summary>
    /// Phase 1.7: 快捷键 (Ctrl+Z / Ctrl+Y) 路由到 CommandRegistry.
    /// 复用 Ribbon 同一组命令, 避免双写.
    /// </summary>
    /// <summary>Phase 1D: 给标记实体反射设置 Position 属性 (避免每个 Mark 写 noargs+ctor 包装).</summary>
    private static lcdb.Entity SetPositionVia(lcdb.Entity entity, Vector2 p)
    {
        var t = entity.GetType();
        // 优先 Position, 再 Center, 再 StartPosition
        var prop = t.GetProperty("Position") ?? t.GetProperty("Center") ?? t.GetProperty("StartPosition");
        prop?.SetValue(entity, p);
        return entity;
    }

    /// <summary>
    /// 画布顶部居中浮动视图按钮 (⛶ ▣ 1:1 ＋ −).
    /// 同 ribbon 命令路径, registry 单一来源.
    /// </summary>
    private void WireFloatingViewBar()
    {
        void Wire(string ctrlName, string commandId)
        {
            var btn = this.FindControl<Button>(ctrlName);
            if (btn is null) return;
            btn.Click += (_, _) => _currentRegistry.Resolve(commandId)?.Invoke();
        }
        Wire("FvbZoomExtents",  "View.ZoomExtents");
        Wire("FvbZoomSelected", "View.ZoomToSelected");
        Wire("FvbZoomOneToOne", "View.ZoomOneToOne");
        Wire("FvbZoomIn",       "View.ZoomIn");
        Wire("FvbZoomOut",      "View.ZoomOut");
    }

    /// <summary>
    /// 状态栏 ToggleButton (网格/捕捉) ↔ canvas 状态双向同步:
    /// - 启动时 IsChecked = canvas 当前状态
    /// - IsCheckedChanged 时调 ToggleGrid/Snap (避免无限循环: 检测 if !=)
    /// 视觉: 开 = 蓝底白字 (:checked pseudo-class), 关 = 灰字
    /// </summary>
    private void WireStatusBarToggles(TextBlock status)
    {
        reg_sbGridToggle = this.FindControl<ToggleButton>("SbGridToggle");
        reg_sbSnapToggle = this.FindControl<ToggleButton>("SbSnapToggle");

        if (reg_sbGridToggle is not null)
        {
            reg_sbGridToggle.IsChecked = ActiveCanvas.GridVisible;
            reg_sbGridToggle.IsCheckedChanged += (_, _) =>
            {
                var want = reg_sbGridToggle.IsChecked == true;
                if (ActiveCanvas.GridVisible != want) ActiveCanvas.ToggleGrid();
                status.Text = want ? "网格已显示" : "网格已隐藏";
            };
        }
        if (reg_sbSnapToggle is not null)
        {
            reg_sbSnapToggle.IsChecked = ActiveCanvas.SnapEnabled;
            reg_sbSnapToggle.IsCheckedChanged += (_, _) =>
            {
                var want = reg_sbSnapToggle.IsChecked == true;
                if (ActiveCanvas.SnapEnabled != want) ActiveCanvas.ToggleSnap();
                status.Text = want ? "对象捕捉已开" : "对象捕捉已关";
            };
        }
    }

    private void WireKeyboardShortcuts()
    {
        KeyDown += (_, e) =>
        {
            // 无修饰键的 F-key 单键快捷 (符合 AutoCAD 习惯)
            if (e.KeyModifiers == KeyModifiers.None)
            {
                string? singleKey = e.Key switch
                {
                    Key.F1 => "Help.Docs",
                    Key.F2 => "Help.Feedback",
                    Key.F7 => "View.ToggleGrid",   // AutoCAD: F7 网格
                    Key.F9 => "View.ToggleSnap",   // AutoCAD: F9 捕捉
                    _ => null,
                };
                if (singleKey is not null)
                {
                    _currentRegistry.Resolve(singleKey)?.Invoke();
                    e.Handled = true;
                    return;
                }
            }

            // Ctrl+Shift+S → 另存为
            if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.S)
            {
                _currentRegistry.Resolve("File.SaveAs")?.Invoke();
                e.Handled = true;
                return;
            }

            if (e.KeyModifiers != KeyModifiers.Control) return;
            string? command = e.Key switch
            {
                Key.Z => "Edit.Undo",
                Key.Y => "Edit.Redo",
                Key.A => "Edit.SelectAll",
                Key.N => "File.New",
                Key.S => "File.Save",
                Key.O => "File.Open",
                Key.OemPlus or Key.Add => "View.ZoomIn",
                Key.OemMinus or Key.Subtract => "View.ZoomOut",
                Key.D0 or Key.NumPad0 => "View.ZoomExtents",
                Key.P => "File.Print",      // Ctrl+P 打印 (AutoCAD/Office 通用)
                _ => null
            };
            if (command is null) return;
            var action = _currentRegistry.Resolve(command);
            if (action is null) return;
            action();
            e.Handled = true;
        };
    }

    private async Task LoadFileAsync(CadCanvas canvas, TextBlock status, TextBlock entityCount)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 .otocad / .json 文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("OtoCAD") { Patterns = new[] { "*.otocad", "*.json" } }
            }
        });
        var file = files.FirstOrDefault();
        if (file is null) return;
        await LoadPathAsync(canvas, status, entityCount, file.Path.LocalPath);
    }

    /// <summary>T2: 公用加载逻辑 — 路径已知 (来自 Picker 或 Recent 菜单).</summary>
    private async Task LoadPathAsync(CadCanvas canvas, TextBlock status, TextBlock entityCount, string path)
    {
        await Task.Yield();
        try
        {
            canvas.LoadOtocadFile(path);
            entityCount.Text = $"实体: {canvas.EntityCount}";
            status.Text = canvas.LoadDiagnostic;
            _recentFiles.Push(path);
        }
        catch (System.Exception ex)
        {
            status.Text = $"加载失败: {ex.Message}";
            _recentFiles.Remove(path);
        }
    }

    /// <summary>T2: 打开最近文件 (按 index).</summary>
    private async Task OpenRecentAsync(CadCanvas canvas, TextBlock status, TextBlock entityCount, int index)
    {
        var items = _recentFiles.ExistingItems();
        if (index >= items.Count) { status.Text = "最近文件列表为空"; return; }
        await LoadPathAsync(canvas, status, entityCount, items[index]);
    }

    /// <summary>
    /// 光学单透镜: 先弹 wizard 填参数, 用户确认后进 PlaceMarkCmd 单击画布放置.
    /// </summary>
    private async Task StartSingleLensAsync(CadCanvas canvas, TextBlock status)
    {
        var wizard = new Dialogs.LensWizardDialog();
        await wizard.ShowDialog(this);
        if (wizard.Result is not lcdb.Optic.OpticalLens template)
        {
            status.Text = "[单透镜] 已取消";
            return;
        }

        // 用模板克隆 + 设位置, 每次预览/放置都是新实例 (避免共享 _markEntities 缓存)
        var cmd = new Commands.PlaceMarkCmd("单透镜", p =>
        {
            var lens = (lcdb.Optic.OpticalLens)template.Clone();
            lens.Position = p;
            return lens;
        });
        canvas.StartCommand(cmd);
    }

    // ============ IAppHost: open-core 扩展宿主 (OtoCAD.Cloud 反射注入导入等付费功能) ============

    void IAppHost.RegisterCommand(string id, Action handler)
    {
        _cmdProviders.Add(reg => reg.Register(id, handler));
        // 立即重建当前注册表纳入新命令 (ribbon/快捷键经 ResolveDynamic 派发到它)
        _currentRegistry = BuildCommandRegistry(_activeCanvas, _status, _entityCount, _propertyPanel);
    }

    async Task<string?> IAppHost.PickFileAsync(string title, params string[] patterns)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("光学设计") { Patterns = patterns } },
        });
        return files.FirstOrDefault()?.Path?.LocalPath;
    }

    async Task<IImportSelection?> IAppHost.ShowImportSelectionAsync(IReadOnlyList<Entity> elements, string summary)
    {
        var dlg = new Dialogs.ImportSelectionDialog(elements, summary);
        await dlg.ShowDialog(this);
        return dlg.Result is null ? null
            : new SelectionWrapper(dlg.Result, dlg.MakeLayoutTab, dlg.MakePerElementTabs);
    }

    void IAppHost.AddLayoutTab(string title, IReadOnlyList<Entity> elements)
    {
        var c = CreateDocumentTab(title, select: true);
        c.LoadGeneratedSheet(elements);
    }

    void IAppHost.AddElementSheetTab(string title, Entity element)
    {
        var c = CreateDocumentTab(title, select: false);
        IEnumerable<Entity> sheet = element switch
        {
            lcdb.Optic.OpticalLens l => IsoSingleLensSheet.Build(IsoSingleLensSheet.SpecFromLens(l)),
            lcdb.Optic.CementedLens d => IsoSingleLensSheet.BuildDoublet(
                d, $"{d.Material1}+{d.Material2} Doublet", "OtoCAD", "1:1", ""),
            _ => new[] { (Entity)element.Clone() },
        };
        c.LoadGeneratedSheet(sheet);
    }

    void IAppHost.AddToActiveDocument(IReadOnlyList<Entity> elements)
    {
        _activeCanvas.AddEntitiesBatch(elements);
        _activeCanvas.ResetView();
    }

    void IAppHost.SetStatus(string text) => _status.Text = text;

    private sealed class SelectionWrapper : IImportSelection
    {
        public IReadOnlyList<Entity> Selected { get; }
        public bool MakeLayoutTab { get; }
        public bool MakePerElementTabs { get; }
        public SelectionWrapper(IReadOnlyList<Entity> sel, bool layout, bool perElement)
        { Selected = sel; MakeLayoutTab = layout; MakePerElementTabs = perElement; }
    }

    /// <summary>启动时反射加载付费模块 OtoCAD.Cloud(若 dll 存在)并注入其功能. 免费版无该 dll → 静默跳过.</summary>
    private void TryLoadCloudModule()
    {
        // ⚠️ 必须按显式路径 LoadFrom: 免费内核不编译引用 OtoCAD.Cloud, 故它不在 deps.json/TPA 里,
        //    Assembly.Load("OtoCAD.Cloud") (按名) 即使 dll 就在 exe 旁也会 FileNotFound. LoadFrom 直接按文件加载.
        var dllPath = System.IO.Path.Combine(AppContext.BaseDirectory, "OtoCAD.Cloud.dll");
        if (!System.IO.File.Exists(dllPath)) return;   // 免费版无该 dll — 正常, 无导入等付费功能.
        try
        {
            var asm = System.Reflection.Assembly.LoadFrom(dllPath);
            var t = asm.GetType("OtoCAD.Cloud.CloudModule");
            if (t is null) { _status.Text = "[Cloud] 已加载 dll 但无 CloudModule 类型"; return; }
            t.GetMethod("Register")?.Invoke(null, new object[] { this });
        }
        catch (Exception ex)
        {
            // dll 存在但加载/注册失败 (依赖缺失/版本不匹配等) — 付费版应可见, 报到状态栏便于诊断.
            _status.Text = $"[Cloud] 加载失败: {(ex.InnerException ?? ex).Message}";
        }
    }

    /// <summary>
    /// 新增公差: 需先选中一个尺寸标注 (DimensionBase) → 弹对话框设公称/偏差/等级
    /// → 复用 PlaceMarkCmd 让用户在画布单击放置 ToleranceAnnotation.
    /// </summary>
    private async Task StartAddToleranceAsync(CadCanvas canvas, TextBlock status)
    {
        var dim = canvas.SelectedEntities.OfType<lcdb.DimensionBase>().FirstOrDefault();
        if (dim is null)
        {
            status.Text = "[新增公差] 请先选中一个尺寸标注 (DimensionBase)";
            return;
        }

        var dlg = new Dialogs.ToleranceInputDialog(Math.Abs(dim.measurement));
        await dlg.ShowDialog(this);
        if (dlg.Result is not lcdb.BasicTolerance basic)
        {
            status.Text = "[新增公差] 已取消";
            return;
        }
        bool showFrame = dlg.ShowFrame;

        // 每次预览/放置都 Clone 公差, 避免多个标注共享同一 BasicTolerance 实例
        var cmd = new Commands.PlaceMarkCmd("公差",
            p => new lcdb.ToleranceAnnotation(p, (lcdb.BasicTolerance)basic.Clone()) { ShowFrame = showFrame });
        canvas.StartCommand(cmd);
        status.Text = $"[新增公差] {basic.GetFormattedString()} — 在画布上单击放置";
    }

    /// <summary>
    /// 通用透镜系统 (OpticalAssembly): 弹 wizard 配置多片镜 → 单击放置 AxisStart.
    /// </summary>
    private async Task StartAssemblyAsync(CadCanvas canvas, TextBlock status)
    {
        var wizard = new Dialogs.AssemblyWizardDialog();
        await wizard.ShowDialog(this);
        if (wizard.Result is not lcdb.Optic.OpticalAssembly template)
        {
            status.Text = "[通用透镜] 已取消";
            return;
        }

        var cmd = new Commands.PlaceMarkCmd("通用透镜", p =>
        {
            var asm = (lcdb.Optic.OpticalAssembly)template.Clone();
            asm.AxisStart = p;
            return asm;
        });
        canvas.StartCommand(cmd);
    }

    /// <summary>
    /// 图框: wizard → PlaceMarkCmd 放置. 放好后自动归到 "图框" 图层 + 默认锁定.
    /// 锁定通过 Layer.IsLocked 通用机制 (非 Frame 专属 bool), 用户可在图层管理里解锁.
    /// </summary>
    private async Task StartFrameAsync(CadCanvas canvas, TextBlock status)
    {
        var wizard = new Dialogs.FrameWizardDialog();
        await wizard.ShowDialog(this);
        if (wizard.Result is not lcdb.DrawingFrame.DrawingFrame template)
        {
            status.Text = "[图框] 已取消";
            return;
        }

        // 确保 "图框" 图层存在 + 默认锁定 (用户改完属性后通常不再动)
        var frameLayer = canvas.GetOrCreateLayer("图框");
        frameLayer.IsLocked = true;

        var cmd = new Commands.PlaceMarkCmd("图框", p =>
        {
            var f = (lcdb.DrawingFrame.DrawingFrame)template.Clone();
            f.Origin = p;
            return f;
        });
        // PlaceMarkCmd 放完会 AddEntity → 默认归到 "0" 层; 监听 EntitiesChanged 把它改到 "图框" 层
        // (走一次性的 placement-then-reassign, 因为 PlaceMarkCmd 不知道 layer 概念)
        void OnPlaced()
        {
            canvas.EntitiesChanged -= OnPlaced;
            var lastFrame = canvas.GetAllEntities().OfType<lcdb.DrawingFrame.DrawingFrame>().LastOrDefault();
            if (lastFrame is not null)
            {
                canvas.AssignEntityToLayer(lastFrame, frameLayer);
                canvas.RequestRedraw();
            }
        }
        canvas.EntitiesChanged += OnPlaced;
        canvas.StartCommand(cmd);
    }

    /// <summary>
    /// 双胶合透镜: wizard → PlaceMarkCmd 模板克隆放置.
    /// </summary>
    private async Task StartDoubletLensAsync(CadCanvas canvas, TextBlock status)
    {
        var wizard = new Dialogs.DoubletWizardDialog();
        await wizard.ShowDialog(this);
        if (wizard.Result is not lcdb.Optic.CementedLens template)
        {
            status.Text = "[双胶合] 已取消";
            return;
        }

        var cmd = new Commands.PlaceMarkCmd("双胶合", p =>
        {
            var d = (lcdb.Optic.CementedLens)template.Clone();
            d.Position = p;
            return d;
        });
        canvas.StartCommand(cmd);
    }

    /// <summary>T8: 棱镜 — wizard → PlaceMarkCmd 模板克隆放置.</summary>
    private async Task StartPrismAsync(CadCanvas canvas, TextBlock status)
    {
        var wizard = new Dialogs.PrismWizardDialog();
        await wizard.ShowDialog(this);
        if (wizard.Result is not lcdb.Optic.Prism template)
        {
            status.Text = "[棱镜] 已取消";
            return;
        }
        var cmd = new Commands.PlaceMarkCmd("棱镜", p =>
        {
            var pr = (lcdb.Optic.Prism)template.Clone();
            pr.Position = p;
            return pr;
        });
        canvas.StartCommand(cmd);
    }

    /// <summary>Week 13: 打开"关于"对话框 (代替原 status.Text 一行字).</summary>
    private async Task OpenAboutAsync()
    {
        var dialog = new Dialogs.AboutDialog();
        await dialog.ShowDialog(this);
    }

    /// <summary>T8: 打开设置对话框.</summary>
    private async Task OpenSettingsAsync(TextBlock status)
    {
        var current = AppSettings.Load();
        var dialog = new Dialogs.SettingsDialog(current, _recentFiles);
        await dialog.ShowDialog(this);
        if (dialog.Result is { } updated)
        {
            updated.Save();
            DimensionStandardService.SetCurrent(updated.DimensionStandard);
            status.Text = $"设置已保存 (主题: {updated.Theme}, 标准: {DimensionStandardService.GetDisplayName(updated.DimensionStandard)}, AutoSave: {updated.AutoSaveSeconds}s)";
        }
    }

    /// <summary>T10: 导出 DXF (AutoCAD 2000+ 兼容, 走 lcdb 业务).</summary>
    private async Task ExportDxfAsync(CadCanvas canvas, TextBlock status)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出为 DXF",
            SuggestedFileName = canvas.CurrentFilePath is string p
                ? Path.GetFileNameWithoutExtension(p) + ".dxf"
                : "scene.dxf",
            DefaultExtension = "dxf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("AutoCAD DXF") { Patterns = new[] { "*.dxf" } }
            }
        });
        if (file is null) return;
        try
        {
            var saved = canvas.ExportToDxf(file.Path.LocalPath);
            status.Text = $"已导出 DXF: {Path.GetFileName(saved)}";
        }
        catch (System.Exception ex)
        {
            status.Text = $"导出失败: {ex.Message}";
        }
    }

    /// <summary>T11: 导出 PDF — 用 SkiaSharp 渲染到 PDF 文档 (跨平台).</summary>
    /// <summary>
    /// Week 14: 打印 — 复用 ExportToPdf 渲染到临时 PDF, 再 ShellExecute "print" verb
    /// 触发系统默认 PDF handler 的打印路径 (Win: Edge/Acrobat / Mac: Preview / Linux: lp).
    /// 跨平台简单可靠, 无需自管 PrintDocument 对话框.
    /// </summary>
    private async Task PrintAsync(CadCanvas canvas, TextBlock status)
    {
        await Task.Yield();
        try
        {
            var temp = Path.Combine(Path.GetTempPath(),
                $"otocad-print-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            canvas.ExportToPdf(temp);

            // ShellExecute 走 "print" 动词 — Windows 走系统默认 PDF reader 的 print
            // (Edge/Acrobat 一般直接弹打印对话框). Mac/Linux fallback "open" + 用户手动.
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = temp,
                UseShellExecute = true,
                Verb = OperatingSystem.IsWindows() ? "print" : null,
            };
            try
            {
                System.Diagnostics.Process.Start(psi);
                status.Text = $"已发送到打印机 (经 PDF: {Path.GetFileName(temp)})";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Win11 部分 PDF reader 不实现 print verb → fallback open verb
                psi.Verb = null;
                System.Diagnostics.Process.Start(psi);
                status.Text = $"已用 PDF 阅读器打开 (按 Ctrl+P 打印): {Path.GetFileName(temp)}";
            }
        }
        catch (System.Exception ex)
        {
            status.Text = $"打印失败: {ex.Message}";
        }
    }

    private async Task ExportPdfAsync(CadCanvas canvas, TextBlock status)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出为 PDF",
            SuggestedFileName = canvas.CurrentFilePath is string p
                ? Path.GetFileNameWithoutExtension(p) + ".pdf"
                : "scene.pdf",
            DefaultExtension = "pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
            }
        });
        if (file is null) return;
        try
        {
            var saved = canvas.ExportToPdf(file.Path.LocalPath);
            status.Text = $"已导出 PDF: {Path.GetFileName(saved)} (A4 横向)";
        }
        catch (System.Exception ex)
        {
            status.Text = $"导出失败: {ex.Message}";
        }
    }

    /// <summary>Phase 1F: 导出当前场景为 PNG.</summary>
    private async Task ExportPngAsync(CadCanvas canvas, TextBlock status)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出为 PNG",
            SuggestedFileName = "scene.png",
            DefaultExtension = "png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG image") { Patterns = new[] { "*.png" } }
            }
        });
        if (file is null) return;
        try
        {
            var saved = canvas.ExportToPng(file.Path.LocalPath);
            status.Text = $"已导出: {System.IO.Path.GetFileName(saved)} (1920×1080)";
        }
        catch (System.Exception ex)
        {
            status.Text = $"导出失败: {ex.Message}";
        }
    }

    /// <summary>
    /// T2: 保存 — 当 forceSaveAs=false 且 CurrentFilePath 已知, 直接覆盖; 否则弹 SaveFilePicker.
    /// </summary>
    private async Task SaveOrSaveAsAsync(CadCanvas canvas, TextBlock status, bool forceSaveAs)
    {
        string? targetPath = null;

        if (!forceSaveAs && canvas.CurrentFilePath is string current && File.Exists(Path.GetDirectoryName(current) ?? ""))
        {
            targetPath = current;
        }
        else
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = forceSaveAs ? "另存为 .json" : "保存为 .json (lcdb 主格式)",
                SuggestedFileName = canvas.CurrentFilePath is string p
                    ? Path.GetFileName(p)
                    : "scene.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("OtoCAD JSON") { Patterns = new[] { "*.json" } }
                }
            });
            if (file is null) return;
            targetPath = file.Path.LocalPath;
        }

        try
        {
            var savedPath = canvas.SaveCurrentScene(targetPath);
            status.Text = $"已保存: {Path.GetFileName(savedPath)}";
            _recentFiles.Push(savedPath);
        }
        catch (System.Exception ex)
        {
            status.Text = $"保存失败: {ex.Message}";
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
