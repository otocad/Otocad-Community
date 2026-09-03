using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using lcdb;
using lcdb.Annotation;
using LitMath;
using OtoCAD.Avalonia.Commands;
using OtoCAD.Avalonia.Rendering;
using OtoCAD.Avalonia.Snap;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Point = Avalonia.Point;  // 消歧: lcdb 也有 Point 实体类型

namespace OtoCAD.Avalonia;

/// <summary>
/// CAD 画布 — POC-02 升级版.
/// 使用 lcdb.Entity 直接渲染 (替换 POC-01 的 SimpleEntity),
/// 通过 SkiaGraphicsDraw : IGraphicsDraw 包装, 复用 lcdb 各实体自身的 Draw 方法.
/// </summary>
public class CadCanvas : Control, ICadCommandHost
{
    private readonly List<Entity> _entities = new();

    /// <summary>工作区底色 (图纸外) — 浅灰, 与图纸白形成区分.</summary>
    private static readonly IBrush WorkspaceBackground =
        new SolidColorBrush(global::Avalonia.Media.Color.FromRgb(0xE4, 0xE7, 0xEB));
    /// <summary>
    /// Entity → Layer 映射 (sidecar). 因为 Avalonia 流程下实体没有走 db.AddEntity,
    /// entity.layer getter 返回 "" 取不到 Layer 记录, 所以这里维护一份显式映射.
    /// 默认 entity 加入时分到 "0" 图层; 命令可调 AssignEntityToLayer 改归属.
    /// </summary>
    private readonly Dictionary<Entity, Layer> _entityLayer = new();
    private Database? _loadedDatabase;  // 持有数据库引用 (避免 entity.database 为 null)

    // Phase 1.7: 撤销/重做栈 (action-based).
    // Undo action: 撤销当前操作并把"redo"动作压入 _redoStack.
    // 场景加载/Round-trip 会清空两栈 (无法跨场景撤销).
    private readonly Stack<IUndoableOp> _undoStack = new();
    private readonly Stack<IUndoableOp> _redoStack = new();

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    // T2: 文件状态 — 当前文件路径 + 脏标记 (任意编辑 → IsDirty=true; 保存/新建 → false)
    private string? _currentFilePath;
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set { if (_currentFilePath != value) { _currentFilePath = value; FilePathChanged?.Invoke(value); } }
    }
    public event Action<string?>? FilePathChanged;

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        private set { if (_isDirty != value) { _isDirty = value; DirtyChanged?.Invoke(value); } }
    }
    public event Action<bool>? DirtyChanged;
    public void MarkDirty() => IsDirty = true;

    // Phase 1A: Selection 模型 — bbox-based hit-test, 单选 + Shift 加选 + ESC/Del 清选删
    private readonly HashSet<Entity> _selectedEntities = new();
    public IReadOnlyCollection<Entity> SelectedEntities => _selectedEntities;
    public int SelectionCount => _selectedEntities.Count;
    public event Action? SelectionChanged;

    // 模板图框属性区: 点中(锁定)图框的值格 → 编辑该格(不移动图框)。命中源 = 图框 IPropertyZoneFrame.HitTestCell。
    public event Action<lcdb.DrawingFrame.DrawingFrame, lcdb.DrawingFrame.PropertyCellHit>? PropertyCellSelected;
    private lcdb.DrawingFrame.DrawingFrame? _selectedCellFrame;
    private lcdb.DrawingFrame.PropertyCellHit? _selectedCellHit;
    private lcdb.DrawingFrame.DrawingFrame? _hoverCellFrame;
    private lcdb.DrawingFrame.PropertyCellHit? _hoverCellHit;

    private void ClearCellSelectionState()
    {
        _selectedCellFrame = null;
        _selectedCellHit = null;
    }

    /// <summary>扫描图框(锁定也可编辑其值格,不可见才跳过),命中属性区值格则返回。</summary>
    private (lcdb.DrawingFrame.DrawingFrame frame, lcdb.DrawingFrame.PropertyCellHit cell)? HitFrameCell(Vector2 p)
    {
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            if (_entities[i] is lcdb.DrawingFrame.IPropertyZoneFrame pz &&
                _entities[i] is lcdb.DrawingFrame.DrawingFrame fr)
            {
                if (!IsEntityVisible(fr)) continue;   // 锁定不跳过(值格仍可编辑),不可见才跳过
                if (pz.HitTestCell(p) is { } cell) return (fr, cell);
            }
        }
        return null;
    }

    private static bool CellEquals(lcdb.DrawingFrame.PropertyCellHit? a, lcdb.DrawingFrame.PropertyCellHit? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Value.ColIndex == b.Value.ColIndex && a.Value.RowIndex == b.Value.RowIndex
            && a.Value.FieldKey == b.Value.FieldKey;
    }

    /// <summary>给当前选中的属性区(列)加一指标行,并选中新行以便立即编辑。</summary>
    public void AddRowToSelectedZone()
    {
        if (_selectedCellFrame is lcdb.DrawingFrame.IPropertyZoneFrame pz && _selectedCellHit is { } cell)
        {
            int newRow = pz.AddRow(cell.ColIndex, "");
            if (newRow >= 0 && pz.GetCell(cell.ColIndex, newRow) is { } nh)
            {
                _selectedCellHit = nh;                              // 选中新行
                PropertyCellSelected?.Invoke(_selectedCellFrame, nh); // 属性面板改绑新行
            }
            InvalidateVisual();
        }
    }

    /// <summary>删除当前选中的属性区指标行(仅当选中的是行而非区)。</summary>
    public void DeleteSelectedFrameRow()
    {
        if (_selectedCellFrame is lcdb.DrawingFrame.IPropertyZoneFrame pz &&
            _selectedCellHit is { } cell && cell.RowIndex >= 0)
        {
            if (pz.RemoveRow(cell.ColIndex, cell.RowIndex))
            {
                ClearCellSelectionState();
                SelectionChanged?.Invoke();   // 面板回到无选中
                InvalidateVisual();
            }
        }
    }

    // Grid 显示开关 — 默认关 (CAD 软件惯例: 网格干扰精细几何观察)
    // 按 F7 / 视图组 "网格" 按钮 / View.ToggleGrid 命令可开启
    private bool _gridVisible = false;
    public bool GridVisible
    {
        get => _gridVisible;
        set { if (_gridVisible != value) { _gridVisible = value; InvalidateVisual(); } }
    }
    public void ToggleGrid() => GridVisible = !_gridVisible;

    // Phase 1E: Snap 引擎 (默认开 端点/中点/圆心)
    private readonly SnapEngine _snapEngine = new();
    private SnapResult? _currentSnap;
    private bool _snapVisible = true;

    // Phase 1F: Marquee 框选 (拖动空白处选多个实体)
    private Vector2? _marqueeStart;     // 模型坐标
    private Vector2? _marqueeEnd;
    private bool _marqueeAdditive;      // Shift 持有 → 加入既有选择

    // Phase 1F: 选中实体的直接拖动 (无需 Move 命令)
    private Vector2? _dragStart;        // 模型坐标
    private Vector2 _dragLastApplied;   // 累计已 Translate 的点 (回滚预览用)
    private bool _isDragging;
    private readonly List<Entity> _dragTargets = new();

    // Phase 1F+: Grip 持久化"已激活" (两步式 UX: 一击选, 再击 + 拖 才动)
    private Entity? _selectedGripEntity;
    private int _selectedGripIndex = -1;
    private GripPoint? _selectedGripPoint;

    // Phase 1F+: Grip 拖动中状态 (按下"已选 grip"时进入)
    private Entity? _gripEntity;
    private int _gripIndex = -1;
    private GripPoint? _gripPoint;
    private Vector2 _gripOriginalPos;
    private Vector2 _gripClickOffset;
    private bool _gripDragStarted;
    // Phase 1F+: Grip hover (鼠标悬停 grip 时高亮提示)
    private Entity? _hoverGripEntity;
    private int _hoverGripIndex = -1;
    public bool SnapEnabled
    {
        get => _snapVisible;
        set { if (_snapVisible != value) { _snapVisible = value; _currentSnap = null; InvalidateVisual(); } }
    }
    public void ToggleSnap() => SnapEnabled = !_snapVisible;

    // Phase 1E (实做): Frame 模式 — 新建实体 EditMode 标记, 显示时可过滤
    private EntityEditMode _currentEditMode = EntityEditMode.Drawing;
    public EntityEditMode CurrentEditMode
    {
        get => _currentEditMode;
        set
        {
            if (_currentEditMode == value) return;
            _currentEditMode = value;
            CurrentEditModeChanged?.Invoke(value);
            InvalidateVisual();
        }
    }
    public void ToggleFrameMode() => CurrentEditMode = _currentEditMode == EntityEditMode.Drawing
        ? EntityEditMode.Frame : EntityEditMode.Drawing;
    public event Action<EntityEditMode>? CurrentEditModeChanged;

    // Phase 1E (实做): Layer CRUD — Database.layerTable wrapper
    public IEnumerable<string> ListLayers()
    {
        if (_loadedDatabase is null) return Array.Empty<string>();
        return _loadedDatabase.layerTable.GetAll().Select(l => l.name);
    }

    /// <summary>新增图层 (默认命名 "图层 N"). 返回新图层名, 或 null 表示数据库不可用.</summary>
    public string? AddLayer()
    {
        if (_loadedDatabase is null) return null;
        var existing = new HashSet<string>(_loadedDatabase.layerTable.GetAll().Select(l => l.name));
        int n = 1;
        string name;
        do { name = $"图层 {n++}"; } while (existing.Contains(name));
        var layer = new Layer(name) { color = lcdb.Colors.Color.White };
        _loadedDatabase.layerTable.Add(layer);
        return name;
    }

    /// <summary>删除指定名称的图层. "0" 不可删. 返回是否成功.</summary>
    public bool RemoveLayer(string name)
    {
        if (_loadedDatabase is null || name == "0") return false;
        var layer = _loadedDatabase.layerTable.GetAll().FirstOrDefault(l => l.name == name);
        if (layer is null) return false;
        _loadedDatabase.layerTable.Remove(layer);
        // 删到的恰是当前层 → 回落 "0"
        if (_currentLayerName == name) SetCurrentLayer("0");
        return true;
    }

    // -------- 当前图层 (新建实体的归属层) --------
    private string _currentLayerName = "0";

    /// <summary>当前绘图图层名 — 新建实体默认归属此层 (Ribbon「图层▾」可切换).</summary>
    public string CurrentLayerName => _currentLayerName;

    /// <summary>当前图层变化通知 (供 Ribbon「图层▾」刷新显示).</summary>
    public event Action<string>? CurrentLayerChanged;

    /// <summary>设置当前图层. 不存在则按名新建. 后续新建实体归属此层.</summary>
    public void SetCurrentLayer(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        GetOrCreateLayer(name);            // 确保存在
        if (_currentLayerName == name) { CurrentLayerChanged?.Invoke(name); return; }
        _currentLayerName = name;
        CurrentLayerChanged?.Invoke(name);
    }

    private float _scale = 1.0f;
    private float _offsetX;
    private float _offsetY;

    private bool _panning;
    private Point _lastPanPoint;

    private readonly Queue<long> _frameTimes = new();
    private long _lastFpsReportMs;
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();

    public int EntityCount => _entities.Count;
    public string LoadDiagnostic { get; private set; } = "";
    public event Action<(double X, double Y)>? MouseModelPositionChanged;
    public event Action<double>? ScaleChanged;
    public event Action<double>? FpsUpdated;

    /// <summary>Phase 1F: 鼠标悬停实体变化 (无命令时, 用于 status bar 提示).</summary>
    public event Action<Entity?>? HoverEntityChanged;
    private Entity? _lastHover;

    /// <summary>POC-04: 暴露给 PropertyPanel — 返回当前 scene 第一个 Line, 没有则 null.</summary>
    public Line? GetFirstLine() => _entities.OfType<Line>().FirstOrDefault();

    /// <summary>POC-04: 外部触发画布重绘 (PropertyPanel 修改属性后调用).</summary>
    public void RequestRedraw() => InvalidateVisual();

    /// <summary>POC-04: 实体变化后通知外部 (e.g. PropertyPanel 长度刷新).</summary>
    public event Action? EntitiesChanged;

    // -------- POC-05: Command 多步交互 --------
    private ICadCommand? _activeCommand;
    private Entity? _previewEntity;
    private Vector2 _lastRawCursorModel;  // 最近一次原始光标 (未 snap) 模型坐标 — 供贴面标记取法线方向

    /// <summary>POC-05: 命令提示文字变化 (供外部状态栏订阅).</summary>
    public event Action<string>? PromptChanged;

    /// <summary>Phase 1F: 当前命令的最新提示文本 (供外部判断是否在命令中).</summary>
    public string? PromptText { get; private set; }

    /// <summary>POC-05: 启动命令. 若有正在运行的命令, 先取消.</summary>
    public void StartCommand(ICadCommand cmd)
    {
        _activeCommand?.Cancel();
        _activeCommand = cmd;
        Focus();  // 确保后续键盘事件能被本控件捕获 (ESC)
        cmd.Start(this);
    }

    // ICadCommandHost 实现
    void ICadCommandHost.AddEntity(Entity entity)
    {
        entity.EditMode = _currentEditMode;  // Phase 1E: 新实体标当前模式
        _entities.Add(entity);
        // 归到当前图层 (命令可后续 AssignEntityToLayer 覆盖)
        AssignEntityToLayer(entity, GetOrCreateLayer(_currentLayerName));
        // 新用户操作 → 推 undo + 清 redo (经典编辑器语义)
        _undoStack.Push(new AddEntityOp(this, entity));
        _redoStack.Clear();
        IsDirty = true;
        InvalidateVisual();
        EntitiesChanged?.Invoke();
    }

    /// <summary>
    /// 批量加入实体 — 整个批次作为单条 undo 记录 (单次 Ctrl+Z 全部回退).
    /// 给 "一键自动标注" / "插入光学图框" 这种一次性产出多个实体的命令用.
    /// </summary>
    public void AddEntitiesBatch(System.Collections.Generic.IReadOnlyList<Entity> entities)
    {
        if (entities is null || entities.Count == 0) return;
        var defaultLayer = GetOrCreateLayer(_currentLayerName);
        foreach (var entity in entities)
        {
            entity.EditMode = _currentEditMode;
            _entities.Add(entity);
            AssignEntityToLayer(entity, defaultLayer);
        }
        _undoStack.Push(new CompositeAddEntitiesOp(this, entities));
        _redoStack.Clear();
        IsDirty = true;
        InvalidateVisual();
        EntitiesChanged?.Invoke();
    }

    /// <summary>
    /// Associativity: 删除指定 source (e.g., OpticalLens) 的所有关联 auto-dim, 重新生成.
    /// 用于 PropertyPanel 改 lens 参数后让自动标注跟随更新 (R1/R2/d/φ 显示与几何同步).
    /// 不入 undo (派生状态), 不 emit EntitiesChanged (避免 PropertyPanel 重建导致编辑焦点丢失).
    /// </summary>
    public void RefreshAutoDimensionsFor(Entity source) => RebuildAutoDims(source, createIfMissing: false);

    /// <summary>
    /// 一键重出: 为 source 重建自动标注 — 与 <see cref="RefreshAutoDimensionsFor"/> 区别在于
    /// 即使当前无任何关联标注也会新建 (出图清单"一键重出"用)。返回生成的实体数。
    /// </summary>
    public int RegenerateAutoDimensionsFor(Entity source) => RebuildAutoDims(source, createIfMissing: true);

    /// <param name="createIfMissing">true=无关联标注时也新建 (重出); false=无则跳过 (编辑联动)。</param>
    private int RebuildAutoDims(Entity source, bool createIfMissing)
    {
        if (source is null) return 0;

        // 先生成, 再按"这次真产出的类型"删旧 —— 删除白名单自动跟随 Build 的实现。
        //
        // Owner==source 的实体分两类: 派生显示物 (尺寸/镀膜标记, Build 会重新产出) 与
        // 数据载体 (非球面数据块 ISO 10110-12 / 粗糙度要求, Build 不产出)。旧码按 Owner
        // 一律删光, 数据载体没有重建来源 → 一键重出即静默永久丢失; 而数据块缺失恰是清单的
        // error 级阻塞项, 修复动作反而制造新的阻塞问题。
        // 不能只过滤 DimensionBase, 否则镀膜标记每次刷新都不被清除 → 累积重复。
        var rebuilt = OtoCAD.Avalonia.Commands.AutoDimensionLensCmd.Build(source);
        var rebuildableTypes = new HashSet<Type>(rebuilt.Select(e => e.GetType()));

        var toRemove = _entities
            .Where(e => ReferenceEquals(e.Owner, source) && rebuildableTypes.Contains(e.GetType()))
            .ToList();
        if (toRemove.Count == 0 && !createIfMissing) return 0;
        foreach (var d in toRemove) _entities.Remove(d);
        // 重新生成
        var defaultLayer = GetOrCreateLayer("0");
        int n = 0;
        foreach (var newDim in rebuilt)
        {
            newDim.Owner = source;
            newDim.EditMode = _currentEditMode;
            _entities.Add(newDim);
            AssignEntityToLayer(newDim, defaultLayer);
            n++;
        }
        IsDirty = true;
        InvalidateVisual();
        return n;
    }

    /// <summary>命令调用: 把实体显式分到指定图层 (覆盖 AddEntity 默认的 "0").</summary>
    public void AssignEntityToLayer(Entity entity, Layer layer)
    {
        if (entity is null || layer is null) return;
        _entityLayer[entity] = layer;
    }

    /// <summary>查实体所在图层 (返回 null 若未注册, 视为 "0").</summary>
    public Layer? GetEntityLayer(Entity entity) =>
        _entityLayer.TryGetValue(entity, out var l) ? l : null;

    /// <summary>实体所在图层是否锁定 (锁定时拒绝 grip 拖动 / 拾取).</summary>
    public bool IsEntityLocked(Entity entity) => GetEntityLayer(entity)?.IsLocked == true;

    /// <summary>实体所在图层是否可见 (隐藏/冻结时跳过渲染).</summary>
    public bool IsEntityVisible(Entity entity)
    {
        var l = GetEntityLayer(entity);
        return l is null || (l.IsVisible && !l.IsFrozen);
    }

    /// <summary>给外部命令/UI 只读迭代 _entities (例如 StartFrameAsync 找最新放的 frame).</summary>
    public IReadOnlyList<Entity> GetAllEntities() => _entities;

    /// <summary>取 / 建图层 (按名). 若不存在则在 db 里建一个白色默认层.</summary>
    public Layer GetOrCreateLayer(string name)
    {
        if (_loadedDatabase is null) return new Layer(name);
        if (_loadedDatabase.layerTable[name] is Layer existing) return existing;
        var fresh = new Layer(name);
        _loadedDatabase.layerTable.Add(fresh);
        return fresh;
    }

    /// <summary>Phase 1.7: 撤销上一次操作; 无可撤销则 no-op 返回 false.</summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;
        var op = _undoStack.Pop();
        op.Undo();
        _redoStack.Push(op);
        IsDirty = true;
        InvalidateVisual();
        EntitiesChanged?.Invoke();
        return true;
    }

    /// <summary>Phase 1.7: 重做被撤销的操作; 无可重做则 no-op 返回 false.</summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;
        var op = _redoStack.Pop();
        op.Redo();
        _undoStack.Push(op);
        IsDirty = true;
        InvalidateVisual();
        EntitiesChanged?.Invoke();
        return true;
    }

    private void ClearUndoHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private interface IUndoableOp
    {
        void Undo();
        void Redo();
    }

    private sealed class AddEntityOp : IUndoableOp
    {
        private readonly CadCanvas _canvas;
        private readonly Entity _entity;
        public AddEntityOp(CadCanvas canvas, Entity entity) { _canvas = canvas; _entity = entity; }
        public void Undo() => _canvas._entities.Remove(_entity);
        public void Redo() => _canvas._entities.Add(_entity);
    }

    /// <summary>批量添加 — undo 一次性移除所有, redo 一次性加回 (顺序保留).</summary>
    private sealed class CompositeAddEntitiesOp : IUndoableOp
    {
        private readonly CadCanvas _canvas;
        private readonly System.Collections.Generic.List<Entity> _entities;
        public CompositeAddEntitiesOp(CadCanvas canvas, System.Collections.Generic.IReadOnlyList<Entity> entities)
        {
            _canvas = canvas;
            _entities = new System.Collections.Generic.List<Entity>(entities);
        }
        public void Undo() { foreach (var e in _entities) _canvas._entities.Remove(e); }
        public void Redo() { foreach (var e in _entities) _canvas._entities.Add(e); }
    }

    /// <summary>Phase 1A: 删除实体的 Undo 操作 (Undo=重新加入, Redo=移除).</summary>
    private sealed class DeleteEntityOp : IUndoableOp
    {
        private readonly CadCanvas _canvas;
        private readonly Entity _entity;
        public DeleteEntityOp(CadCanvas canvas, Entity entity) { _canvas = canvas; _entity = entity; }
        public void Undo() => _canvas._entities.Add(_entity);
        public void Redo() => _canvas._entities.Remove(_entity);
    }

    /// <summary>Phase 1A: 整体位移多个实体 (Undo=反向位移, Redo=正向位移).</summary>
    private sealed class MoveEntitiesOp : IUndoableOp
    {
        private readonly System.Collections.Generic.List<Entity> _targets;
        private readonly Vector2 _delta;
        public MoveEntitiesOp(System.Collections.Generic.IEnumerable<Entity> targets, Vector2 delta)
        {
            _targets = new System.Collections.Generic.List<Entity>(targets);
            _delta = delta;
        }
        public void Undo() { foreach (var e in _targets) e.Translate(new Vector2(-_delta.X, -_delta.Y)); }
        public void Redo() { foreach (var e in _targets) e.Translate(_delta); }
    }

    /// <summary>Phase 1C: 通用矩阵变换 op (Rotate/Scale/Mirror).</summary>
    private sealed class TransformEntitiesOp : IUndoableOp
    {
        private readonly System.Collections.Generic.List<Entity> _targets;
        private readonly Matrix3 _fwd, _inv;
        public TransformEntitiesOp(System.Collections.Generic.IEnumerable<Entity> targets, Matrix3 fwd, Matrix3 inv)
        {
            _targets = new System.Collections.Generic.List<Entity>(targets);
            _fwd = fwd; _inv = inv;
        }
        public void Undo() { foreach (var e in _targets) e.TransformBy(_inv); }
        public void Redo() { foreach (var e in _targets) e.TransformBy(_fwd); }
    }

    /// <summary>Phase 1F+: Grip 拖动 op (Undo=恢复原位置, Redo=应用新位置).</summary>
    private sealed class GripDragOp : IUndoableOp
    {
        private readonly Entity _entity;
        private readonly int _index;
        private readonly Vector2 _oldPos, _newPos;
        public GripDragOp(Entity entity, int index, Vector2 oldPos, Vector2 newPos)
        {
            _entity = entity; _index = index; _oldPos = oldPos; _newPos = newPos;
        }
        public void Undo() => Apply(_oldPos);
        public void Redo() => Apply(_newPos);
        private void Apply(Vector2 target)
        {
            var grips = _entity.GetGripPoints();
            if (grips is null || _index < 0 || _index >= grips.Count) return;
            var g = grips[_index];
            _entity.SetGripPointAt(_index, g, target);
            _entity.SetGripPointAtFinished(_index, g, target);
        }
    }

    /// <summary>Phase 1C: 复制实体 op (Undo=移除克隆, Redo=重新加入).</summary>
    private sealed class CopyEntitiesOp : IUndoableOp
    {
        private readonly CadCanvas _canvas;
        private readonly System.Collections.Generic.List<Entity> _clones;
        public CopyEntitiesOp(CadCanvas canvas, System.Collections.Generic.IEnumerable<Entity> clones)
        {
            _canvas = canvas;
            _clones = new System.Collections.Generic.List<Entity>(clones);
        }
        public void Undo() { foreach (var e in _clones) _canvas._entities.Remove(e); }
        public void Redo() { foreach (var e in _clones) _canvas._entities.Add(e); }
    }

    /// <summary>Phase 1A: 提交一次 Move 操作 (Translate + push undo). 供 MoveCmd 调用.</summary>
    public void CommitMove(System.Collections.Generic.IReadOnlyList<Entity> targets, Vector2 delta)
    {
        if (targets.Count == 0) return;
        foreach (var e in targets) e.Translate(delta);
        _undoStack.Push(new MoveEntitiesOp(targets, delta));
        _redoStack.Clear();
        IsDirty = true;
        EntitiesChanged?.Invoke();
        InvalidateVisual();
    }

    /// <summary>
    /// Phase 1C: 提交一次任意矩阵变换 (Mirror/Rotate/Scale 公用).
    /// 调用方传入 forward 矩阵 + 对应 inverse 矩阵 (用于 Undo).
    /// 自镜像类型 (Mirror) 时 fwd=inv.
    /// </summary>
    public void CommitTransform(System.Collections.Generic.IReadOnlyList<Entity> targets,
                                Matrix3 forward, Matrix3 inverse)
    {
        if (targets.Count == 0) return;
        foreach (var e in targets) e.TransformBy(forward);
        _undoStack.Push(new TransformEntitiesOp(targets, forward, inverse));
        _redoStack.Clear();
        IsDirty = true;
        EntitiesChanged?.Invoke();
        InvalidateVisual();
    }

    /// <summary>
    /// Phase 1C: 提交一次 Copy (深拷贝原始 + Translate + 加入场景).
    /// 返回创建的克隆列表 (调用方可继续选中等操作).
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Entity> CommitCopy(
        System.Collections.Generic.IReadOnlyList<Entity> originals, Vector2 delta)
    {
        var clones = new System.Collections.Generic.List<Entity>(originals.Count);
        foreach (var orig in originals)
        {
            var c = (Entity)orig.Clone();
            c.Translate(delta);
            _entities.Add(c);
            clones.Add(c);
        }
        if (clones.Count > 0)
        {
            _undoStack.Push(new CopyEntitiesOp(this, clones));
            _redoStack.Clear();
            IsDirty = true;
            EntitiesChanged?.Invoke();
            InvalidateVisual();
        }
        return clones;
    }

    // -------- Selection API (Phase 1A) --------

    /// <summary>
    /// Phase 1F: 精确命中测试 — 几何实体走距离判定, 其它 (Mark) 走 bbox.
    /// 从顶层向底搜索, 返回第一个命中实体或 null.
    /// tolerance 单位为模型坐标 (调用方应传入 像素阈值/_scale).
    /// </summary>
    public Entity? HitTest(Vector2 modelPoint, double tolerance)
    {
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            var e = _entities[i];
            // 锁定 / 不可见图层上的实体不可被单击选中
            if (IsEntityLocked(e) || !IsEntityVisible(e)) continue;
            try
            {
                if (HitTestEntity(e, modelPoint, tolerance)) return e;
            }
            catch { /* 无 database 上下文等情况跳过 */ }
        }
        return null;
    }

    /// <summary>
    /// Phase 1F+: 在选中集合的实体上查 grip 命中 (优先 grip 而非实体)。
    /// 取容差内**最近**的 grip (而非第一个): 文字夹点常与尺寸线中点夹点重叠在中点附近,
    /// 取最近 + 平局让后者(文字, 下标更大)胜出 → 点在文字上即可抓文字夹点单独拖 (沿尺寸线挪文字)。
    /// </summary>
    private (Entity entity, int index, GripPoint grip)? TryHitGrip(Vector2 modelPoint, double tolerance)
    {
        (Entity entity, int index, GripPoint grip)? best = null;
        double bestD2 = tolerance * tolerance;
        foreach (var entity in _selectedEntities)
        {
            // 锁定图层上的实体即使被"选中"也不允许 grip 拖动
            if (IsEntityLocked(entity)) continue;
            List<GripPoint>? grips;
            try { grips = entity.GetGripPoints(); } catch { continue; }
            if (grips is null) continue;
            for (int i = 0; i < grips.Count; i++)
            {
                var g = grips[i];
                var dx = modelPoint.X - g.position.X;
                var dy = modelPoint.Y - g.position.Y;
                double d2 = dx * dx + dy * dy;
                if (d2 <= bestD2)   // <= : 平局时后一个(文字夹点 index 更大)覆盖前一个(尺寸线中点)
                {
                    bestD2 = d2;
                    best = (entity, i, g);
                }
            }
        }
        return best;
    }

    private static bool HitTestEntity(Entity e, Vector2 p, double tol)
    {
        switch (e)
        {
            case Line line:
                return DistancePointToSegment(p, line.startPoint, line.endPoint) <= tol;

            case Circle c:
                {
                    var dx = p.X - c.center.X; var dy = p.Y - c.center.Y;
                    var d = System.Math.Sqrt(dx * dx + dy * dy);
                    return System.Math.Abs(d - c.radius) <= tol;  // 只命中圆环
                }

            case Arc arc:
                {
                    var dx = p.X - arc.center.X; var dy = p.Y - arc.center.Y;
                    var d = System.Math.Sqrt(dx * dx + dy * dy);
                    if (System.Math.Abs(d - arc.radius) > tol) return false;
                    var angle = System.Math.Atan2(dy, dx);
                    // 归一化角度到 [0, 2π) 区间, 检查是否在 [startAngle, endAngle] 之间
                    return AngleInRange(angle, arc.startAngle, arc.endAngle);
                }

            case Polyline pl:
                {
                    for (int i = 0; i + 1 < pl.NumberOfVertices; i++)
                    {
                        var a = pl.Vertices[i]; var b = pl.Vertices[i + 1];
                        if (DistancePointToSegment(p, new Vector2(a.X, a.Y), new Vector2(b.X, b.Y)) <= tol)
                            return true;
                    }
                    return false;
                }

            case lcdb.Point pt:
                {
                    var dx = p.X - pt.position.X; var dy = p.Y - pt.position.Y;
                    return System.Math.Sqrt(dx * dx + dy * dy) <= tol * 2;
                }

            default:
                // Mark / 其它复杂实体走 bbox 兜底
                var (minX, minY, maxX, maxY) = ExtractBoundingBox(e.bounding);
                return p.X >= minX - tol && p.X <= maxX + tol
                    && p.Y >= minY - tol && p.Y <= maxY + tol;
        }
    }

    private static double DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var lenSq = dx * dx + dy * dy;
        if (lenSq < 1e-12) // a == b 退化为点
        {
            var ddx = p.X - a.X; var ddy = p.Y - a.Y;
            return System.Math.Sqrt(ddx * ddx + ddy * ddy);
        }
        var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
        t = System.Math.Max(0, System.Math.Min(1, t));
        var px = a.X + t * dx; var py = a.Y + t * dy;
        var rx = p.X - px; var ry = p.Y - py;
        return System.Math.Sqrt(rx * rx + ry * ry);
    }

    private static bool AngleInRange(double angle, double startAngle, double endAngle)
    {
        // 角度归一化到 [0, 2π)
        double Norm(double a) { while (a < 0) a += 2 * System.Math.PI; while (a >= 2 * System.Math.PI) a -= 2 * System.Math.PI; return a; }
        double na = Norm(angle), ns = Norm(startAngle), ne = Norm(endAngle);
        if (ns <= ne) return na >= ns && na <= ne;
        return na >= ns || na <= ne;  // 跨 0 弧度的情况
    }

    /// <summary>选中实体. additive=true 时加入已有选择集, false 时先清空.</summary>
    public void Select(Entity entity, bool additive = false)
    {
        ClearCellSelectionState();
        if (!additive) _selectedEntities.Clear();
        _selectedEntities.Add(entity);
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    /// <summary>清空选择集.</summary>
    public void ClearSelection()
    {
        bool hadCell = _selectedCellFrame is not null;
        ClearCellSelectionState();
        if (_selectedEntities.Count == 0)
        {
            if (hadCell) { SelectionChanged?.Invoke(); InvalidateVisual(); }
            return;
        }
        _selectedEntities.Clear();
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    /// <summary>切换选择状态 (已选 → 取消; 未选 → 加入).</summary>
    public void ToggleSelect(Entity entity)
    {
        ClearCellSelectionState();
        if (!_selectedEntities.Add(entity))
            _selectedEntities.Remove(entity);
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    /// <summary>Phase 1F: 全选场景中所有实体.</summary>
    public void SelectAll()
    {
        ClearCellSelectionState();
        _selectedEntities.Clear();
        // 跳过锁定/不可见图层上的实体 (图框层锁定 → Ctrl+A 不选中图框)
        foreach (var e in _entities)
        {
            if (IsEntityLocked(e) || !IsEntityVisible(e)) continue;
            _selectedEntities.Add(e);
        }
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    /// <summary>删除选中实体 (push undo). 锁定图层上的实体不删. 返回是否实际删除.</summary>
    public bool DeleteSelected()
    {
        if (_selectedEntities.Count == 0) return false;
        // 防御: 即便选区里混入了锁定实体 (例如其它路径选中), 也不删除锁定图层上的实体
        var deletable = _selectedEntities.Where(e => !IsEntityLocked(e)).ToList();
        if (deletable.Count == 0) return false;
        foreach (var entity in deletable)
        {
            _entities.Remove(entity);
            _undoStack.Push(new DeleteEntityOp(this, entity));
        }
        _redoStack.Clear();
        _selectedEntities.Clear();
        IsDirty = true;
        SelectionChanged?.Invoke();
        EntitiesChanged?.Invoke();
        InvalidateVisual();
        return true;
    }

    void ICadCommandHost.SetPreview(Entity? previewEntity)
    {
        _previewEntity = previewEntity;
        InvalidateVisual();
    }

    void ICadCommandHost.SetPrompt(string prompt) { PromptText = prompt; PromptChanged?.Invoke(prompt); }

    Vector2 ICadCommandHost.RawCursorModel => _lastRawCursorModel;

    void ICadCommandHost.FinishCommand()
    {
        _activeCommand = null;
        _previewEntity = null;
        PromptText = null;
        InvalidateVisual();
    }

    /// <summary>
    /// 找 modelPoint 附近 tolerance 内最近的实体. 复用 SnapEngine 的
    /// NearestPointOn 距离算法保证与 snap 行为一致.
    /// 命中多个实体时 Circle/Arc 优先 (因为半径/直径标注最常对它们)
    /// </summary>
    Entity? ICadCommandHost.PickEntityAt(Vector2 modelPoint, double tolerance, bool pierce)
    {
        Entity? best = null;
        double bestDistSq = tolerance * tolerance;
        bool bestIsCircular = false;
        var tolModel = tolerance;
        foreach (var e in _entities)
        {
            // 锁定/不可见 图层上的实体不可被命令拾取
            if (IsEntityLocked(e) || !IsEntityVisible(e)) continue;
            var np = SnapEngine.NearestPointOnPublic(e, modelPoint);
            // 复合实体 (透镜/图框) 自身无最近点 → 用其最近子实体距离代表它, 否则下面 pierce 永远进不去。
            if (np is null && e is IPierceable pc)
                np = NearestSubPoint(pc, modelPoint);
            if (np is not { } pt) continue;
            var dx = pt.X - modelPoint.X;
            var dy = pt.Y - modelPoint.Y;
            var d2 = dx * dx + dy * dy;
            if (d2 > tolModel * tolModel) continue;
            bool isCircular = e is Circle || e is Arc;
            // Circle/Arc 优先: 命中圆 +圆 时只更新 best 若更近; 命中非圆时只在 best 也非圆且更近时更新
            if (best is null || (isCircular && (!bestIsCircular || d2 < bestDistSq)) || (!isCircular && !bestIsCircular && d2 < bestDistSq))
            {
                best = e; bestDistSq = d2; bestIsCircular = isCircular;
            }
        }

        // pierce: 若命中是复合实体 (IPierceable), 下钻到子实体, 返回更深的 hit
        if (pierce && best is IPierceable pierceable)
        {
            Entity? deepest = null;
            double deepestD2 = tolModel * tolModel;
            bool deepestIsCircular = false;
            foreach (var sub in pierceable.GetPierceableSubEntities())
            {
                var np = SnapEngine.NearestPointOnPublic(sub, modelPoint);
                if (np is not { } pt) continue;
                var dx = pt.X - modelPoint.X;
                var dy = pt.Y - modelPoint.Y;
                var d2 = dx * dx + dy * dy;
                if (d2 > tolModel * tolModel) continue;
                bool isCircular = sub is Circle || sub is Arc;
                if (deepest is null
                    || (isCircular && (!deepestIsCircular || d2 < deepestD2))
                    || (!isCircular && !deepestIsCircular && d2 < deepestD2))
                {
                    deepest = sub; deepestD2 = d2; deepestIsCircular = isCircular;
                }
            }
            if (deepest is not null) return deepest;
        }
        return best;
    }

    /// <summary>
    /// 找 target 附近 maxDist 内最近的"面"(Line/Circle/Arc, 含复合实体下钻的弧/线; 排除 exclude 自身、
    /// 锁定/隐藏实体)。返回该面上的最近点 + 外法线 (指向 target 一侧); 无命中返回 null。
    /// 供贴面标记拖夹点时"粘住"弧面。
    /// </summary>
    private (Vector2 Point, Vector2 Normal)? FindNearestSurfacePoint(Vector2 target, Entity exclude, double maxDist)
    {
        Vector2? best = null;
        double bestD2 = maxDist * maxDist;
        void Consider(Entity e)
        {
            if (SnapEngine.NearestPointOnPublic(e, target) is not { } pt) return;
            var dx = pt.X - target.X; var dy = pt.Y - target.Y;
            var d2 = dx * dx + dy * dy;
            if (d2 < bestD2) { bestD2 = d2; best = pt; }
        }
        foreach (var e in _entities)
        {
            if (ReferenceEquals(e, exclude)) continue;
            if (IsEntityLocked(e) || !IsEntityVisible(e)) continue;
            Consider(e);
            if (e is IPierceable pc)
                foreach (var sub in pc.GetPierceableSubEntities()) Consider(sub);
        }
        if (best is not { } sp) return null;
        var n = target - sp;
        var normal = n.length > 1e-6 ? n.normalized : new Vector2(0, 1);
        return (sp, normal);
    }

    /// <summary>复合实体内部子实体里离 modelPoint 最近的点 (用于让透镜/图框等参与拾取)。</summary>
    private static Vector2? NearestSubPoint(IPierceable p, Vector2 modelPoint)
    {
        Vector2? best = null;
        double bestD2 = double.MaxValue;
        foreach (var sub in p.GetPierceableSubEntities())
        {
            if (SnapEngine.NearestPointOnPublic(sub, modelPoint) is not { } pt) continue;
            var dx = pt.X - modelPoint.X;
            var dy = pt.Y - modelPoint.Y;
            var d2 = dx * dx + dy * dy;
            if (d2 < bestD2) { bestD2 = d2; best = pt; }
        }
        return best;
    }

    public CadCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    // -------- 公共 API --------

    /// <summary>
    /// 默认场景 — 一张 GB/T 13323-2009 风格的光学零件图样例:
    /// OpticalDrawingFrame (含对材料/对零件双表) + 1 个 N-BK7 双凸透镜 (光轴双点画线)
    /// + 自动 R1/R2/d/φ 尺寸标注. 启动即看到典型产出, 比"矩形+圆"的几何 demo
    /// 更说明 OtoCAD 的用途.
    /// </summary>
    public void LoadTestScene()
    {
        // 启动样例统一走出图引擎 (按惯例图框轴出 GB/ISO 样例); LoadGeneratedSheet 自动采纳 GB 框属性包为文档级真值源。
        LoadGeneratedSheet(OtoCAD.Avalonia.Templating.StandardSheetEngine.SampleSheet());
        IsDirty = false;   // 初始演示场景不算"已修改"
    }

    /// <summary>
    /// 用生成器产出的整套图纸实体替换当前场景 (清空 → 加入 → 锁图框层 → 适配视图).
    /// "快速构建标准图纸" 入口用.
    /// </summary>
    public void LoadGeneratedSheet(IEnumerable<Entity> entities)
    {
        _entities.Clear();
        ClearUndoHistory();
        ClearSelection();
        _loadedDatabase = new Database();

        var frameLayer = GetOrCreateLayer("图框");
        frameLayer.IsLocked = true;
        foreach (var ent in entities)
        {
            _entities.Add(ent);
            if (ent is lcdb.DrawingFrame.DrawingFrame) AssignEntityToLayer(ent, frameLayer);
            // 属性包驱动图框 (GB 中文框 / 数据驱动自定义框) 自带属性包 → 采纳为文档级单一真值源
            // (改格回写它, 存盘序列化它, 重出从它重建)
            if (ent is lcdb.DrawingFrame.IPropertyBagFrame pbf && pbf.Bag is { } bag)
                _loadedDatabase.DrawingProperties = bag;
        }

        ResetView();
        IsDirty = true;
        InvalidateVisual();
    }

    /// <summary>
    /// 一键重出(属性驱动图框): 把文档级属性包重新套到在场属性包驱动图框 (GB 中文框 / 数据驱动框),
    /// 反映改格/外部改包并重绘。真值标注由出图引擎(AppendScaledLensView)管, 故不在此加自动标注。返回是否有框被重套。
    /// </summary>
    public bool ReapplyDocumentProperties()
    {
        bool any = false;
        foreach (var e in _entities)
            if (e is lcdb.DrawingFrame.IPropertyBagFrame pbf)
            {
                pbf.ApplyBag(_loadedDatabase.DrawingProperties);
                any = true;
            }
        if (any) { IsDirty = true; InvalidateVisual(); }
        return any;
    }

    public void LoadBenchmarkScene(int count)
    {
        _entities.Clear();
        ClearUndoHistory();
        _loadedDatabase = new Database();

        var rand = new Random(42);
        for (int i = 0; i < count; i++)
        {
            _entities.Add(new Line(
                new Vector2(rand.NextDouble() * 500, rand.NextDouble() * 500),
                new Vector2(rand.NextDouble() * 500, rand.NextDouble() * 500)));
        }
        ResetView();
    }

    /// <summary>
    /// 通过 lcdb 加载 .otocad / .json 文件.
    /// 返回 (实体总数, 诊断信息).
    /// </summary>
    /// <param name="asRecovery">true = 从自动备份恢复: 不绑定该备份为当前文件 (置空路径 + 标脏, 强制用户另存到真实位置).</param>
    public int LoadOtocadFile(string path, bool asRecovery = false)
    {
        _entities.Clear();
        ClearUndoHistory();
        LoadDiagnostic = "";

        try
        {
            var db = new Database();
            db.Open(path);
            _loadedDatabase = db;

            var loaded = db.GetEntities("ModelSpace").ToList();
            _entities.AddRange(loaded);

            // 按类型分类统计 — 写入诊断信息
            var typeBuckets = loaded
                .GroupBy(e => e.GetType().Name)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}={g.Count()}");
            LoadDiagnostic = $"加载 {loaded.Count} 个实体 [" + string.Join(", ", typeBuckets) + "]";

            // T2: 关联文件路径, 清脏标记; 恢复模式则不绑定备份路径 (空路径+标脏 → 强制另存)
            if (asRecovery)
            {
                CurrentFilePath = null;
                IsDirty = true;
            }
            else
            {
                CurrentFilePath = path;
                IsDirty = false;
            }
        }
        catch (Exception ex)
        {
            LoadDiagnostic = $"加载失败: {ex.GetType().Name}: {ex.Message}";
            throw;
        }

        ResetView();
        return _entities.Count;
    }

    /// <summary>
    /// T2: 新建空场景 — 清实体/历史/路径/脏标记.
    /// </summary>
    public void NewScene()
    {
        _entities.Clear();
        ClearUndoHistory();
        ClearSelection();
        _loadedDatabase = null;
        CurrentFilePath = null;
        IsDirty = false;
        LoadDiagnostic = "新场景";
        ResetView();
        InvalidateVisual();
    }

    /// <summary>
    /// Round-trip 自测: 构造测试场景 → 保存到临时文件 → 重新加载 → 对比实体数.
    /// POC-02 的核心证据 — 证明 lcdb 的序列化/反序列化在 Avalonia 进程内闭环.
    /// </summary>
    public string RoundtripSelfTest()
    {
        // 1. 先确保有内容
        LoadTestScene();
        var originalCount = _entities.Count;
        var originalTypes = _entities.Select(e => e.GetType().Name).OrderBy(s => s).ToList();

        // 2. 保存到临时文件
        var tmp = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"poc02-roundtrip-{System.Guid.NewGuid():N}.json");

        long fileSize;
        try
        {
            SaveCurrentScene(tmp);
            fileSize = new System.IO.FileInfo(tmp).Length;
        }
        catch (System.Exception ex)
        {
            return $"Round-trip 失败 (Save 阶段): {ex.GetType().Name}: {ex.Message}";
        }

        // 3. 清空 + 重新加载
        _entities.Clear();
        ClearUndoHistory();
        int reloadedCount;
        try
        {
            reloadedCount = LoadOtocadFile(tmp);
        }
        catch (System.Exception ex)
        {
            return $"Round-trip 失败 (Load 阶段, 文件 {fileSize}B): {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            try { System.IO.File.Delete(tmp); } catch { /* ignore */ }
        }

        var reloadedTypes = _entities.Select(e => e.GetType().Name).OrderBy(s => s).ToList();

        // 4. 对比
        var typeMatch = originalTypes.SequenceEqual(reloadedTypes);
        var verdict = (originalCount == reloadedCount && typeMatch) ? "✅" : "⚠️";

        return $"{verdict} Round-trip: 原始 {originalCount} 个 [{string.Join(",", originalTypes)}] " +
               $"→ 文件 {fileSize}B → 加载 {reloadedCount} 个 [{string.Join(",", reloadedTypes)}]";
    }

    /// <summary>
    /// Phase 1F: 当前视图导出 PNG (用当前 _scale/_offset/_entities + 网格).
    /// 离屏 SKBitmap 渲染 → encode PNG → 写文件.
    /// 返回保存后的实际路径.
    /// </summary>
    public string ExportToPng(string path, int width = 1920, int height = 1080)
    {
        if (System.IO.Path.GetExtension(path).Length == 0)
            path = System.IO.Path.ChangeExtension(path, ".png");

        using var bitmap = new SKBitmap(width, height);
        using var skCanvas = new SKCanvas(bitmap);
        RenderSceneCentered(skCanvas, width, height);

        // encode
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        using var fs = System.IO.File.OpenWrite(path);
        data.SaveTo(fs);
        return path;
    }

    /// <summary>
    /// T11: 导出场景为 PDF (A4 横向, 1 页, 跨平台).
    /// 用 SkiaSharp SKDocument.CreatePdf — 矢量, 不会因放大失真.
    /// </summary>
    public string ExportToPdf(string path, float pageWidthPt = 842, float pageHeightPt = 595)
    {
        if (System.IO.Path.GetExtension(path).Length == 0)
            path = System.IO.Path.ChangeExtension(path, ".pdf");

        using var fs = System.IO.File.OpenWrite(path);
        using var doc = SKDocument.CreatePdf(fs);
        var skCanvas = doc.BeginPage(pageWidthPt, pageHeightPt);
        RenderSceneCentered(skCanvas, (int)pageWidthPt, (int)pageHeightPt);
        doc.EndPage();
        doc.Close();
        return path;
    }

    /// <summary>
    /// 公共渲染逻辑 — 把场景按 bounding 居中铺到指定画布尺寸, 留 40px margin.
    /// </summary>
    private void RenderSceneCentered(SKCanvas skCanvas, int width, int height)
    {
        skCanvas.Clear(SKColors.White);

        var (minX, minY, maxX, maxY) = GetSceneBounding();
        var modelW = System.Math.Max(1e-3, maxX - minX);
        var modelH = System.Math.Max(1e-3, maxY - minY);
        float exportScale = (float)System.Math.Min((width - 40) / modelW, (height - 40) / modelH);
        float offsetX = (float)(width / 2 - (minX + maxX) / 2 * exportScale);
        float offsetY = (float)(height / 2 + (minY + maxY) / 2 * exportScale);

        skCanvas.Translate(offsetX, offsetY);
        skCanvas.Scale(exportScale, -exportScale);

        using var gd = new SkiaGraphicsDraw(skCanvas, exportScale, SKColors.Black);
        RefreshAssociations(_entities);
        var dangling = ComputeDanglingDims(_entities);
        foreach (var ent in _entities)
        {
            gd.CurrentSkColor = ResolveEntitySkColor(ent, dangling);
            try { ent.Draw(gd); } catch { /* skip broken entities */ }
        }
    }

    private (double, double, double, double) GetSceneBounding()
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var e in _entities)
        {
            try
            {
                var (bMinX, bMinY, bMaxX, bMaxY) = ExtractBoundingBox(e.bounding);
                if (bMinX <= bMaxX && bMinY <= bMaxY)
                {
                    minX = System.Math.Min(minX, bMinX);
                    minY = System.Math.Min(minY, bMinY);
                    maxX = System.Math.Max(maxX, bMaxX);
                    maxY = System.Math.Max(maxY, bMaxY);
                }
            }
            catch { }
        }
        if (minX == double.MaxValue) return (0, 0, 100, 100);
        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// 把当前内存中的 _entities 持久化到磁盘 (lcdb 主格式 .json).
    /// 用于验证 Save → Load round-trip — POC-02 的关键证据.
    /// </summary>
    public string SaveCurrentScene(string path)
    {
        if (System.IO.Path.GetExtension(path).Length == 0)
            path = System.IO.Path.ChangeExtension(path, ".json");

        var db = new Database();
        foreach (var ent in _entities)
        {
            try { db.AddEntity(ent); }
            catch { /* 跳过添加失败的实体 (可能因为已属于其他 db) */ }
        }
        db.SaveAs(path);

        // T2: 关联文件路径 + 清脏标记
        CurrentFilePath = path;
        IsDirty = false;
        return path;
    }

    /// <summary>
    /// T2 AutoSave: 静默保存到指定路径, 不更新 CurrentFilePath / IsDirty.
    /// 用于 30s 定时自动保存到 .autosave.json (奔溃恢复用).
    /// </summary>
    public string SaveAutoBackup(string path)
    {
        var db = new Database();
        foreach (var ent in _entities)
        {
            try { db.AddEntity(ent); }
            catch { }
        }
        db.SaveAs(path);
        return path;
    }

    /// <summary>
    /// T10: 导出 DXF — 走 lcdb.Database.SaveAs(*.dxf) 路径.
    /// 与 SaveCurrentScene 不同, 不更新 CurrentFilePath / IsDirty (DXF 是导出格式不是主格式).
    ///
    /// 图层随行: 画布用 _entityLayer 旁路字典记实体归属 (Entity.layer 依赖 entity.database),
    /// 只把实体塞进临时 db 会让 DXF 里的一切落到 "0" 层 — 层色/锁定/图层组织全丢.
    /// </summary>
    public string ExportToDxf(string path)
    {
        if (!path.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
            path += ".dxf";

        var db = new Database();

        if (_loadedDatabase is not null)
        {
            foreach (var src in _loadedDatabase.layerTable.GetAll())
            {
                if (string.IsNullOrEmpty(src.name) || db.layerTable.Has(src.name)) continue;
                // 复制而非复用实例 — 加入另一个 db 会改写 id/owner, 污染活动文档的图层表
                db.layerTable.Add(new Layer(src.name)
                {
                    color = src.color,
                    description = src.description,
                    lineWeight = src.lineWeight,
                    lineType = src.lineType,
                });
            }
        }

        foreach (var ent in _entities)
        {
            try
            {
                db.AddEntity(ent);
                if (GetEntityLayer(ent) is Layer lay) ent.layer = lay.name;
            }
            catch { }
        }
        db.SaveAs(path);
        return path;
    }

    /// <summary>Phase 1A: 当前缩放比例 (用于状态栏显示).</summary>
    public float CurrentScale => _scale;

    /// <summary>Phase 1A: 以视口中心为锚点缩放 factor 倍.</summary>
    public void ZoomBy(float factor)
    {
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        var anchor = new Point(w / 2, h / 2);
        var oldScale = _scale;
        _scale = Math.Clamp(_scale * factor, 0.001f, 100000f);
        _offsetX = (float)(anchor.X - (anchor.X - _offsetX) * (_scale / oldScale));
        _offsetY = (float)(anchor.Y - (anchor.Y - _offsetY) * (_scale / oldScale));
        ScaleChanged?.Invoke(_scale);
        InvalidateVisual();
    }

    /// <summary>
    /// 缩放到 1:1 (1 模型单位 = 1 像素). 给"按真实尺寸看"的场景用 — 打印前预览有用.
    /// 中心点保持在窗口中心.
    /// </summary>
    public void ZoomToOneToOne()
    {
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        if (w <= 0 || h <= 0) return;
        // 保持当前 view 中心的模型坐标不变 → 反算 offset
        var oldCenterModelX = (w / 2 - _offsetX) / _scale;
        var oldCenterModelY = -(h / 2 - _offsetY) / _scale;  // Y 翻转
        _scale = 1.0f;
        _offsetX = (float)(w / 2 - oldCenterModelX * _scale);
        _offsetY = (float)(h / 2 + oldCenterModelY * _scale);
        ScaleChanged?.Invoke(_scale);
        InvalidateVisual();
    }

    /// <summary>
    /// 缩放到选中实体的 bounding (留 40px 边距). 没选中则 fallback 到 ResetView (全部).
    /// </summary>
    public void ZoomToSelected()
    {
        if (_selectedEntities.Count == 0) { ResetView(); return; }
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var e in _selectedEntities)
        {
            try
            {
                var (bMinX, bMinY, bMaxX, bMaxY) = ExtractBoundingBox(e.bounding);
                if (bMinX > bMaxX || bMinY > bMaxY) continue;
                minX = Math.Min(minX, bMinX); minY = Math.Min(minY, bMinY);
                maxX = Math.Max(maxX, bMaxX); maxY = Math.Max(maxY, bMaxY);
            }
            catch { }
        }
        if (minX == double.MaxValue) { ResetView(); return; }

        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        var modelW = Math.Max(1e-3, maxX - minX);
        var modelH = Math.Max(1e-3, maxY - minY);
        _scale = (float)Math.Min((w - 40) / modelW, (h - 40) / modelH);
        var cx = (minX + maxX) / 2;
        var cy = (minY + maxY) / 2;
        _offsetX = (float)(w / 2 - cx * _scale);
        _offsetY = (float)(h / 2 + cy * _scale);
        ScaleChanged?.Invoke(_scale);
        InvalidateVisual();
    }

    public void ResetView()
    {
        // 早期调用 (MainWindow ctor 里 LoadTestScene → ResetView 发生在 layout 前,
        // Bounds.Width/Height 还是 0) → 注册 LayoutUpdated 一次性回调, 拿到真实
        // 尺寸再 fit. 避免按 fallback 1200×720 算出偏离的缩放.
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            EventHandler? once = null;
            once = (_, _) =>
            {
                if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
                LayoutUpdated -= once;
                ResetView();
            };
            LayoutUpdated += once;
            return;
        }

        if (_entities.Count == 0)
        {
            _scale = 1.0f;
            _offsetX = 0;
            _offsetY = 0;
        }
        else
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var e in _entities)
            {
                try
                {
                    var b = e.bounding;
                    // Bounding 的 API 在 lcdb 中通常有 minPnt/maxPnt 或类似
                    // POC 阶段: 反射读取常见属性, 找不到则跳过
                    var (bMinX, bMinY, bMaxX, bMaxY) = ExtractBoundingBox(b);
                    if (bMinX <= bMaxX && bMinY <= bMaxY)
                    {
                        minX = Math.Min(minX, bMinX);
                        minY = Math.Min(minY, bMinY);
                        maxX = Math.Max(maxX, bMaxX);
                        maxY = Math.Max(maxY, bMaxY);
                    }
                }
                catch
                {
                    // 实体可能因为缺少 database 上下文抛异常, 跳过
                }
            }

            if (minX == double.MaxValue)
            {
                // 所有实体 bounding 都失败 — 用默认视图
                _scale = 1.0f;
                _offsetX = 0;
                _offsetY = 0;
            }
            else
            {
                var w = (float)Bounds.Width;
                var h = (float)Bounds.Height;
                if (w <= 0 || h <= 0) { w = 1200; h = 720; }

                var modelW = Math.Max(1e-3, maxX - minX);
                var modelH = Math.Max(1e-3, maxY - minY);
                var sx = (w - 40) / modelW;
                var sy = (h - 40) / modelH;
                _scale = (float)Math.Min(sx, sy);

                // Y 翻转: 屏幕 Y 向下, 模型 Y 向上.
                // 把模型 bbox 中心放到画布中心.
                var modelCenterX = (minX + maxX) / 2;
                var modelCenterY = (minY + maxY) / 2;
                _offsetX = (float)(w / 2 - modelCenterX * _scale);
                _offsetY = (float)(h / 2 + modelCenterY * _scale);  // + 因为 Y 翻转
            }
        }

        ScaleChanged?.Invoke(_scale);
        InvalidateVisual();
    }

    /// <summary>
    /// 顶层实体的默认绘制色: 仅当实体显式指定了 RGB (ByColor / ByEntity) 才用其颜色,
    /// 否则退化为黑色 (ByLayer 默认是白色, 白底上不可见; 且解析图层色需 DB 上下文).
    /// 子图元 (Line/Arc/MText) 仍会在各自 Draw 中按自身显式颜色覆盖此默认值.
    /// </summary>
    /// <summary>
    /// 悬空尺寸集合: 线性标注两端点均未吸附到任一零件(透镜/双胶合)角点 = 悬空 → 灰显。
    /// 渲染前算一次 (两条渲染路径各自调用), 静态以便嵌套的 CadDrawOperation 也能用。
    /// </summary>
    /// <summary>
    /// 全关联刷新: 渲染前让所有"锚定到某对象"的标注/标记跟随其锚定对象重derive。
    /// - 线性标注: 走 DimAnchor 数据绑定 (RefreshFromAnchors, 支持夹点改锚)。
    /// - 标记(粗糙度/镀膜)/径向标注等: 走布局层注入的 AssociativeRefresh 闭包 (重算贴面点/球心)。
    /// 各实现内部做变化门控, 无变化零开销 (不会每帧重建几何)。
    /// </summary>
    private static void RefreshAssociations(IReadOnlyList<Entity> entities)
    {
        foreach (var e in entities)
        {
            if (e is LinearDimension d) d.RefreshFromAnchors();
            e.AssociativeRefresh?.Invoke();
        }
    }

    /// <summary>在宿主捕捉点列表里找与给定点重合的索引 (供拖动端点吸到角点后建立关联绑定)。无匹配返回 -1。</summary>
    private static int FindSnapIndex(Entity host, LitMath.Vector2 pt)
    {
        List<lcdb.ObjectSnapPoint> sps;
        try { sps = host.GetSnapPoints(); } catch { return -1; }
        if (sps is null) return -1;
        for (int i = 0; i < sps.Count; i++)
        {
            double dx = sps[i].position.X - pt.X, dy = sps[i].position.Y - pt.Y;
            if (dx * dx + dy * dy < 1e-6) return i;   // 吸附点即来自 GetSnapPoints, 应几乎重合
        }
        return -1;
    }

    private static HashSet<Entity> ComputeDanglingDims(IReadOnlyList<Entity> entities)
    {
        var dangling = new HashSet<Entity>();
        var corners = new List<LitMath.Vector2>();
        foreach (var e in entities)
            if (e is lcdb.Optic.OpticalLens or lcdb.Optic.CementedLens)
                try { foreach (var sp in e.GetSnapPoints()) corners.Add(sp.position); } catch { }
        if (corners.Count == 0) return dangling;   // 无零件 → 不判定 (避免全灰)

        const double tol2 = 0.6 * 0.6;
        bool OnCorner(LitMath.Vector2 p)
        {
            foreach (var c in corners)
            {
                double dx = p.X - c.X, dy = p.Y - c.Y;
                if (dx * dx + dy * dy <= tol2) return true;
            }
            return false;
        }
        foreach (var e in entities)
            if (e is LinearDimension d && (!OnCorner(d.firstReferencePoint) || !OnCorner(d.secondReferencePoint)))
                dangling.Add(e);   // 任一端点未贴角点 = 悬空 → 灰
        return dangling;
    }

    private static SKColor ResolveEntitySkColor(Entity e, HashSet<Entity>? dangling = null)
    {
        if (dangling != null && dangling.Contains(e)) return new SKColor(0x9A, 0x9A, 0x9A);   // 悬空尺寸: 灰
        var c = e.color;
        if (c.colorMethod == lcdb.Colors.ColorMethod.ByColor
            || c.colorMethod == lcdb.Colors.ColorMethod.ByEntity)
        {
            return new SKColor(c.r, c.g, c.b);
        }
        return SKColors.Black;
    }

    private static (double, double, double, double) ExtractBoundingBox(object boundingObj)
    {
        // lcdb.Bounding 的真实 API 通过反射读取, POC 阶段最稳妥
        var type = boundingObj.GetType();
        double minX = 0, minY = 0, maxX = 0, maxY = 0;
        bool ok = false;

        // 尝试 min/max 命名 (lcdb.Bounding 实际用 minPoint / maxPoint)
        var minProp = type.GetProperty("minPoint") ?? type.GetProperty("MinPoint")
                   ?? type.GetProperty("min") ?? type.GetProperty("Min")
                   ?? type.GetProperty("minPnt") ?? type.GetProperty("MinPnt");
        var maxProp = type.GetProperty("maxPoint") ?? type.GetProperty("MaxPoint")
                   ?? type.GetProperty("max") ?? type.GetProperty("Max")
                   ?? type.GetProperty("maxPnt") ?? type.GetProperty("MaxPnt");

        if (minProp != null && maxProp != null)
        {
            var minV = minProp.GetValue(boundingObj);
            var maxV = maxProp.GetValue(boundingObj);
            if (minV is Vector2 mn && maxV is Vector2 mx)
            {
                minX = mn.X; minY = mn.Y;
                maxX = mx.X; maxY = mx.Y;
                ok = true;
            }
        }

        if (!ok)
        {
            // 再尝试 center + width/height
            var centerProp = type.GetProperty("center") ?? type.GetProperty("Center");
            var widthProp = type.GetProperty("width") ?? type.GetProperty("Width");
            var heightProp = type.GetProperty("height") ?? type.GetProperty("Height");
            if (centerProp != null && widthProp != null && heightProp != null)
            {
                var c = centerProp.GetValue(boundingObj);
                var w = Convert.ToDouble(widthProp.GetValue(boundingObj));
                var h = Convert.ToDouble(heightProp.GetValue(boundingObj));
                if (c is Vector2 cc)
                {
                    minX = cc.X - w / 2; minY = cc.Y - h / 2;
                    maxX = cc.X + w / 2; maxY = cc.Y + h / 2;
                    ok = true;
                }
            }
        }

        return ok ? (minX, minY, maxX, maxY) : (0, 0, 0, 0);
    }

    // -------- 渲染 --------

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        // 工作区底色 (浅灰) — 与图纸白区分; 图纸矩形在 CadDrawOperation 中铺白
        context.FillRectangle(WorkspaceBackground, bounds);
        // Phase 1F+: active grip = 拖动中 OR 已选 (持久态)
        var renderActiveGripEntity = _gripEntity ?? _selectedGripEntity;
        var renderActiveGripIndex = _gripEntity is not null ? _gripIndex : _selectedGripIndex;
        context.Custom(new CadDrawOperation(
            bounds, _entities, _previewEntity, _selectedEntities, _gridVisible, _currentSnap,
            _marqueeStart, _marqueeEnd,
            renderActiveGripEntity, renderActiveGripIndex, _hoverGripEntity, _hoverGripIndex,
            _scale, _offsetX, _offsetY, this));

        // 模板图框属性区: hover / selected 值格高亮(模型→屏幕直接画)
        if (_hoverCellFrame is not null && _hoverCellHit is { } hc)
            DrawCellHighlight(context, hc.Rect, CellHoverFill, null);
        if (_selectedCellFrame is not null && _selectedCellHit is { } sc)
            DrawCellHighlight(context, sc.Rect, CellSelectFill, CellSelectPen);
    }

    private static readonly IBrush CellHoverFill = new SolidColorBrush(Color.FromArgb(40, 0x3A, 0x7B, 0xD5));
    private static readonly IBrush CellSelectFill = new SolidColorBrush(Color.FromArgb(70, 0x3A, 0x7B, 0xD5));
    private static readonly IPen CellSelectPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 0x1E, 0x5F, 0xB0)), 1.2);

    private void DrawCellHighlight(DrawingContext ctx, LitMath.Rectangle2 r, IBrush fill, IPen? pen)
    {
        double left = _offsetX + r.location.X * _scale;
        double right = _offsetX + (r.location.X + r.width) * _scale;
        double top = _offsetY - (r.location.Y + r.height) * _scale;   // 模型上沿(较大 Y)→ 较小屏幕 Y
        double bottom = _offsetY - r.location.Y * _scale;
        var rect = new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        ctx.FillRectangle(fill, rect);
        if (pen is not null) ctx.DrawRectangle(null, pen, rect);
    }

    internal void OnFrameRendered(long elapsedMs)
    {
        _frameTimes.Enqueue(elapsedMs);
        while (_frameTimes.Count > 30) _frameTimes.Dequeue();

        var now = _frameTimer.ElapsedMilliseconds;
        if (now - _lastFpsReportMs > 250)
        {
            _lastFpsReportMs = now;
            long sum = 0;
            foreach (var t in _frameTimes) sum += t;
            double avgMs = sum / (double)Math.Max(1, _frameTimes.Count);
            double fps = avgMs > 0.1 ? 1000.0 / avgMs : 999.0;
            Dispatcher.UIThread.Post(() => FpsUpdated?.Invoke(fps));
        }
    }

    // -------- 交互 --------

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);

        // POC-05: 左键 + active command → 路由到命令
        if (pt.Properties.IsLeftButtonPressed && _activeCommand is not null)
        {
            var mp = ScreenToModel(pt.Position);
            var modelPoint = new Vector2(mp.X, mp.Y);
            _lastRawCursorModel = modelPoint;  // 原始(未 snap)光标, 供贴面标记取外法线方向
            // Phase 1E: 若有 snap 命中, 用 snap 点替代原始光标点
            if (_snapVisible && _currentSnap is not null) modelPoint = _currentSnap.Point;
            _activeCommand.OnMouseClick(modelPoint);
            return;
        }

        // Phase 1A/1F: 左键 + 无命令 → grip 拖动 / 拾取选择 / 拖动选中 / marquee
        if (pt.Properties.IsLeftButtonPressed && _activeCommand is null)
        {
            Focus();
            var mp = ScreenToModel(pt.Position);
            var modelPoint = new Vector2(mp.X, mp.Y);
            var tolerance = 5.0 / _scale;
            var gripTol = 8.0 / _scale;

            // Phase 1F+: grip 命中 — 单步式: 按下即"待拖动" + 选中。
            // 防抖交给移动阈值 (OnPointerMoved 里超过阈值才真移动): 按下后小幅抖动 → 视为点击/选中不动;
            // 超过阈值 → 进入拖动, 之后移动多少都算重新放置。无需先点一下再拖。
            var gripHit = TryHitGrip(modelPoint, gripTol);
            if (gripHit is { } g)
            {
                _selectedGripEntity = g.entity;
                _selectedGripIndex = g.index;
                _selectedGripPoint = g.grip;
                _gripEntity = g.entity;
                _gripIndex = g.index;
                _gripPoint = g.grip;
                _gripOriginalPos = g.grip.position;
                _gripClickOffset = new Vector2(g.grip.position.X - modelPoint.X, g.grip.position.Y - modelPoint.Y);
                _gripDragStarted = false;
                e.Pointer.Capture(this);
                InvalidateVisual();
                return;
            }
            else
            {
                // 点其他地方 → 清掉 grip 选中
                if (_selectedGripEntity is not null)
                {
                    _selectedGripEntity = null;
                    _selectedGripIndex = -1;
                    _selectedGripPoint = null;
                    InvalidateVisual();
                }
            }

            var hit = HitTest(modelPoint, tolerance);
            bool shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

            // Phase 1F: 命中已选中的实体 → 准备拖动 (释放时区分单击重选 vs 拖动)
            if (hit is not null && _selectedEntities.Contains(hit) && !shift)
            {
                _dragStart = modelPoint;
                _dragLastApplied = modelPoint;
                _isDragging = false;
                _dragTargets.Clear();
                _dragTargets.AddRange(_selectedEntities);
                // 关联生成物 (Owner ∈ 选中) 跟随移动: 尺寸标注/贴面标记/光轴标签随零件一起走, 锚点保持对齐。
                foreach (var en in _entities)
                    if (en.Owner is not null && !_selectedEntities.Contains(en)
                        && _selectedEntities.Any(s => ReferenceEquals(en.Owner, s)))
                        _dragTargets.Add(en);
                e.Pointer.Capture(this);
                return;
            }

            if (hit is not null)
            {
                if (shift) ToggleSelect(hit);
                else Select(hit, additive: false);
            }
            else
            {
                // 模板图框属性区: 普通拾取落空时(锁定图框被跳过),尝试命中其值格 → 编辑该格
                if (!shift && HitFrameCell(modelPoint) is { } fc)
                {
                    _selectedEntities.Clear();          // 放下普通选择
                    ClearCellSelectionState();
                    _selectedCellFrame = fc.frame;
                    _selectedCellHit = fc.cell;
                    SelectionChanged?.Invoke();         // 面板先清(count=0),随后 PropertyCellSelected 覆盖为该格
                    PropertyCellSelected?.Invoke(fc.frame, fc.cell);
                    InvalidateVisual();
                    return;
                }

                // 空白处开始 marquee
                _marqueeStart = modelPoint;
                _marqueeEnd = modelPoint;
                _marqueeAdditive = shift;
                if (!shift) ClearSelection();
                e.Pointer.Capture(this);
                InvalidateVisual();
            }
            return;
        }

        // Phase 1B: 右键在 active command 期间 → OnRightClick (Polyline/Spline 完成); 否则 pan
        if (pt.Properties.IsRightButtonPressed && _activeCommand is not null)
        {
            var mp = ScreenToModel(pt.Position);
            _activeCommand.OnRightClick(new Vector2(mp.X, mp.Y));
            return;
        }

        if (pt.Properties.IsRightButtonPressed || pt.Properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _lastPanPoint = pt.Position;
            e.Pointer.Capture(this);
        }
    }

    /// <summary>POC-05: 屏幕坐标 → 模型坐标 (考虑 Y 翻转).</summary>
    private (double X, double Y) ScreenToModel(Point screenPoint)
    {
        double mx = (screenPoint.X - _offsetX) / _scale;
        double my = -(screenPoint.Y - _offsetY) / _scale;
        return (mx, my);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // POC-05: ESC 取消当前命令; 无命令时清空选择集; marquee 期间取消框选; grip 选中清除
        if (e.Key == Key.Escape)
        {
            if (_marqueeStart is not null)
            {
                _marqueeStart = null; _marqueeEnd = null;
                InvalidateVisual();
                e.Handled = true;
            }
            else if (_activeCommand is not null) { _activeCommand.Cancel(); e.Handled = true; }
            else if (_selectedGripEntity is not null)
            {
                _selectedGripEntity = null; _selectedGripIndex = -1; _selectedGripPoint = null;
                InvalidateVisual();
                e.Handled = true;
            }
            else if (_selectedEntities.Count > 0 || _selectedCellFrame is not null) { ClearSelection(); e.Handled = true; }
        }
        // Phase 1A: Del 删除选中实体
        else if (e.Key == Key.Delete && _activeCommand is null)
        {
            if (DeleteSelected()) e.Handled = true;
        }
        // Phase 1B: Enter 确认 (Polyline/Spline 完成)
        else if (e.Key == Key.Enter && _activeCommand is not null)
        {
            _activeCommand.OnConfirm();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
        }

        // Phase 1F+: grip 拖动释放 — SetGripPointAtFinished + push undo (仅当真拖动了)
        // 释放后保持 grip 选中状态 (用户下次可再次按住拖)
        if (_gripEntity is not null && _gripPoint is not null)
        {
            if (_gripDragStarted)
            {
                var newPos = _gripPoint.position;  // SetGripPointAt 已应用
                _gripEntity.SetGripPointAtFinished(_gripIndex, _gripPoint, newPos);
                _undoStack.Push(new GripDragOp(_gripEntity, _gripIndex, _gripOriginalPos, newPos));
                _redoStack.Clear();
                EntitiesChanged?.Invoke();
                // grip 拖到新位置后, 仍标记为选中 (用户可继续编辑)
                _selectedGripPoint = _gripPoint;  // position 已变
            }
            _gripEntity = null;
            _gripPoint = null;
            _gripIndex = -1;
            _gripDragStarted = false;
            _currentSnap = null;   // 清掉夹点拖动期间的吸附标记
            e.Pointer.Capture(null);
            InvalidateVisual();
        }

        // Phase 1F: 拖动释放 — commit 移动 (push undo) 或单击重选 (无位移)
        if (_dragStart is not null)
        {
            if (_isDragging)
            {
                var totalDelta = new Vector2(_dragLastApplied.X - _dragStart.Value.X, _dragLastApplied.Y - _dragStart.Value.Y);
                // 抵消已应用的预览, 由 CommitMove 重做一次以注入 undo
                var revert = new Vector2(-totalDelta.X, -totalDelta.Y);
                foreach (var ent in _dragTargets) ent.Translate(revert);
                CommitMove(_dragTargets, totalDelta);
            }
            else
            {
                // 没拖动 = 单击保持现选 (相当于 noop)
            }
            _dragStart = null;
            _isDragging = false;
            _dragTargets.Clear();
            e.Pointer.Capture(null);
        }

        // Phase 1F: marquee 释放 — 选中范围内实体
        if (_marqueeStart is not null && _marqueeEnd is not null)
        {
            var s = _marqueeStart.Value;
            var en = _marqueeEnd.Value;
            double minX = System.Math.Min(s.X, en.X), maxX = System.Math.Max(s.X, en.X);
            double minY = System.Math.Min(s.Y, en.Y), maxY = System.Math.Max(s.Y, en.Y);
            // 左→右 = 窗选 (bbox 完全在内); 右→左 = 框选 (bbox 与矩形相交)
            bool crossing = en.X < s.X;
            int hitCount = 0;
            foreach (var ent in _entities)
            {
                // 锁定/不可见图层上的实体不参与 marquee 选择
                if (IsEntityLocked(ent) || !IsEntityVisible(ent)) continue;
                try
                {
                    var (bMinX, bMinY, bMaxX, bMaxY) = ExtractBoundingBox(ent.bounding);
                    bool match = crossing
                        ? !(bMaxX < minX || bMinX > maxX || bMaxY < minY || bMinY > maxY)
                        : (bMinX >= minX && bMaxX <= maxX && bMinY >= minY && bMaxY <= maxY);
                    if (match) { _selectedEntities.Add(ent); hitCount++; }
                }
                catch { /* ignore */ }
            }
            _marqueeStart = null; _marqueeEnd = null;
            e.Pointer.Capture(null);
            if (hitCount > 0) SelectionChanged?.Invoke();
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);

        if (_panning)
        {
            var dx = (float)(pt.Position.X - _lastPanPoint.X);
            var dy = (float)(pt.Position.Y - _lastPanPoint.Y);
            _offsetX += dx;
            _offsetY += dy;
            _lastPanPoint = pt.Position;
            InvalidateVisual();
        }

        // 模型坐标 (考虑 Y 翻转)
        var mx = (pt.Position.X - _offsetX) / _scale;
        var my = -(pt.Position.Y - _offsetY) / _scale;
        MouseModelPositionChanged?.Invoke((mx, my));

        var modelPoint = new Vector2(mx, my);
        _lastRawCursorModel = modelPoint;  // 记录未 snap 的原始光标, 供贴面标记取外法线方向

        // Phase 1F: 鼠标悬停实体提示 (仅无命令 + 不在 marquee/drag 时)
        if (_activeCommand is null && _marqueeStart is null && _dragStart is null && _gripEntity is null)
        {
            var tol = 5.0 / _scale;
            var hover = HitTest(modelPoint, tol);
            if (!ReferenceEquals(hover, _lastHover))
            {
                _lastHover = hover;
                HoverEntityChanged?.Invoke(hover);
            }

            // 模板图框属性区: 悬停值格高亮(命中源 = 图框 HitTestCell,穿透锁定)
            var fcHover = HitFrameCell(modelPoint);
            var newCellFrame = fcHover?.frame;
            lcdb.DrawingFrame.PropertyCellHit? newCellHit = fcHover?.cell;
            if (!ReferenceEquals(newCellFrame, _hoverCellFrame) || !CellEquals(newCellHit, _hoverCellHit))
            {
                _hoverCellFrame = newCellFrame;
                _hoverCellHit = newCellHit;
                InvalidateVisual();
            }

            // Phase 1F+: grip 悬停高亮 (光标在 grip 范围内时变色)
            var gripTol = 8.0 / _scale;
            var gripHit = TryHitGrip(modelPoint, gripTol);
            var newGripEnt = gripHit?.entity;
            var newGripIdx = gripHit?.index ?? -1;
            if (!ReferenceEquals(newGripEnt, _hoverGripEntity) || newGripIdx != _hoverGripIndex)
            {
                _hoverGripEntity = newGripEnt;
                _hoverGripIndex = newGripIdx;
                Cursor = _hoverGripEntity is not null
                    ? new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.SizeAll)
                    : global::Avalonia.Input.Cursor.Default;
                InvalidateVisual();
            }
        }

        // Phase 1F: marquee 拖动中 — 更新结束点 + 重绘
        if (_marqueeStart is not null)
        {
            _marqueeEnd = modelPoint;
            InvalidateVisual();
        }

        // Phase 1F+: grip 拖动中 — 超阈值才真移动, 移动时保留 click offset (无抖动)
        if (_gripEntity is not null && _gripPoint is not null)
        {
            if (!_gripDragStarted)
            {
                // 判断鼠标是否移动超过阈值 (3px / scale)
                var threshold = 3.0 / _scale;
                var dx = modelPoint.X - (_gripOriginalPos.X - _gripClickOffset.X);
                var dy = modelPoint.Y - (_gripOriginalPos.Y - _gripClickOffset.Y);
                if (System.Math.Sqrt(dx * dx + dy * dy) < threshold) return;  // 未超阈值, 不动
                _gripDragStarted = true;
                // 用户手动调整径向标注 → 脱离自动跟随, 否则关联重锚会把球心/弧锚拉回(强制重绘半径)。
                // (标记/线性各有自己的拖动保持机制: 标记走 AttachState.Height, 线性走 DimAnchor 重绑。)
                if (_gripEntity is RadialDimension rdGrip) rdGrip.AssociativeRefresh = null;
            }
            // 应用 click offset 保持鼠标与 grip 相对位置不变
            var target = new Vector2(modelPoint.X + _gripClickOffset.X, modelPoint.Y + _gripClickOffset.Y);

            // 贴面标记: 拖夹点时"粘住"最近的面 (沿弧相切), 仅当拖离超过 ~3×标记尺寸才脱离。
            bool stuck = false;
            if (_gripEntity is lcdb.Annotation.ISurfaceAttachable attachable)
            {
                var b = _gripEntity.bounding;
                double markSpan = System.Math.Max(b.maxPoint.X - b.minPoint.X, b.maxPoint.Y - b.minPoint.Y);
                double stickDist = markSpan * 1.5;   // bounding 宽≈2×Size, ×1.5 ≈ 3×标记尺寸
                var hit = FindNearestSurfacePoint(target, _gripEntity, stickDist);
                if (hit is { } h) { attachable.AttachToSurface(h.Point, h.Normal); stuck = true; }
            }
            if (!stuck)
            {
                // 线性标注端点夹点: 吸附到零件角点(顶点/口径端/剖面角)或曲面(弯月面最近点),
                // 并同步更新关联绑定 — 吸到离散角点=重绑该角点(随后跟随), 否则=脱离。
                // (必须更新绑定: 否则 ReanchorDims 每帧把端点拉回旧角点, 拖动看不到预览。)
                if (_gripEntity is LinearDimension ld && (_gripIndex == 0 || _gripIndex == 1))
                {
                    double tol = 12.0 / _scale;
                    var others = new List<Entity>(_entities.Count);
                    foreach (var en in _entities) if (!ReferenceEquals(en, _gripEntity)) others.Add(en);
                    var snap = _snapEngine.FindSnap(target, others, tol,
                        SnapType.Endpoint | SnapType.Center | SnapType.Nearest);
                    DimAnchor? newAnchor = null;
                    if (snap is not null)
                    {
                        target = snap.Point; _currentSnap = snap;
                        if (snap.Type != SnapType.Nearest)   // 离散角点才可索引绑定; Nearest(曲面最近点)无固定索引
                        {
                            int idx = FindSnapIndex(snap.Source, snap.Point);
                            if (idx >= 0) newAnchor = new DimAnchor(snap.Source, idx);
                        }
                    }
                    else _currentSnap = null;
                    if (_gripIndex == 0) ld.FirstAnchor = newAnchor; else ld.SecondAnchor = newAnchor;
                }
                _gripEntity.SetGripPointAt(_gripIndex, _gripPoint, target);
            }

            InvalidateVisual();
            EntitiesChanged?.Invoke();
            return;
        }

        // Phase 1F: 选中拖动中 — 增量 Translate (覆盖上次预览)
        if (_dragStart is not null && _dragTargets.Count > 0)
        {
            // 判定: 是否超过 dragThreshold (3px / scale) → 升级为真拖动
            if (!_isDragging)
            {
                var dx0 = modelPoint.X - _dragStart.Value.X;
                var dy0 = modelPoint.Y - _dragStart.Value.Y;
                var moveModel = System.Math.Sqrt(dx0 * dx0 + dy0 * dy0);
                var threshold = 3.0 / _scale;
                if (moveModel > threshold) _isDragging = true;
            }
            if (_isDragging)
            {
                var delta = new Vector2(modelPoint.X - _dragLastApplied.X, modelPoint.Y - _dragLastApplied.Y);
                foreach (var ent in _dragTargets) ent.Translate(delta);
                _dragLastApplied = modelPoint;
                InvalidateVisual();
            }
        }

        // Phase 1E: snap 检测 — 命令活跃时才查 (避免空闲时 CPU 浪费)
        // Phase 1F+: 命令可通过 PreferredSnapTypes 覆盖 (Mark cmd → Nearest only)
        if (_snapVisible && _activeCommand is not null)
        {
            var snapTolModel = 12.0 / _scale;  // 12 px
            var prev = _currentSnap;
            _currentSnap = _snapEngine.FindSnap(modelPoint, _entities, snapTolModel, _activeCommand.PreferredSnapTypes);
            if (!ReferenceEquals(prev, _currentSnap)) InvalidateVisual();
            if (_currentSnap is not null) modelPoint = _currentSnap.Point;
        }
        else if (_currentSnap is not null)
        {
            _currentSnap = null;
            InvalidateVisual();
        }

        // POC-05: 路由到 active command (用于橡皮筋预览)
        _activeCommand?.OnMouseMove(modelPoint);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pt = e.GetPosition(this);
        var oldScale = _scale;
        var zoomFactor = e.Delta.Y > 0 ? 1.2f : 1.0f / 1.2f;
        _scale = Math.Clamp(_scale * zoomFactor, 0.001f, 100000f);

        _offsetX = (float)(pt.X - (pt.X - _offsetX) * (_scale / oldScale));
        _offsetY = (float)(pt.Y - (pt.Y - _offsetY) * (_scale / oldScale));

        ScaleChanged?.Invoke(_scale);
        InvalidateVisual();
    }

    // -------- 内部嵌套类 --------

    private sealed class CadDrawOperation : ICustomDrawOperation
    {
        private readonly List<Entity> _entities;
        private readonly Entity? _previewEntity;
        private readonly HashSet<Entity> _selected;
        private readonly bool _gridVisible;
        private readonly SnapResult? _snap;
        private readonly Vector2? _marqueeStart, _marqueeEnd;
        private readonly Entity? _activeGripEntity;
        private readonly int _activeGripIndex;
        private readonly Entity? _hoverGripEntity;
        private readonly int _hoverGripIndex;
        private readonly float _scale, _offsetX, _offsetY;
        private readonly CadCanvas _owner;

        public CadDrawOperation(Rect bounds,
                                List<Entity> entities,
                                Entity? previewEntity,
                                HashSet<Entity> selected,
                                bool gridVisible,
                                SnapResult? snap,
                                Vector2? marqueeStart, Vector2? marqueeEnd,
                                Entity? activeGripEntity, int activeGripIndex,
                                Entity? hoverGripEntity, int hoverGripIndex,
                                float scale, float offsetX, float offsetY,
                                CadCanvas owner)
        {
            Bounds = bounds;
            _entities = entities;
            _previewEntity = previewEntity;
            _selected = selected;
            _gridVisible = gridVisible;
            _snap = snap;
            _marqueeStart = marqueeStart; _marqueeEnd = marqueeEnd;
            _activeGripEntity = activeGripEntity; _activeGripIndex = activeGripIndex;
            _hoverGripEntity = hoverGripEntity; _hoverGripIndex = hoverGripIndex;
            _scale = scale;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _owner = owner;
        }

        public Rect Bounds { get; }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var sw = Stopwatch.StartNew();

            var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()?.Lease();
            if (lease is null) return;

            try
            {
                var canvas = lease.SkCanvas;
                canvas.Save();
                canvas.Translate(_offsetX, _offsetY);
                canvas.Scale(_scale, -_scale);  // Y 翻转: 数学坐标系 → 屏幕坐标系

                // Phase 1A: Grid (在模型坐标系下绘制, Y 翻转已应用)
                if (_gridVisible) DrawGrid(canvas);

                // 图纸: 在工作区底色之上铺每个图框的纸张白底 (盖住其下网格), 实体再画在白纸上
                using (var paperPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White, IsAntialias = false })
                {
                    foreach (var ent in _entities)
                    {
                        if (ent is not lcdb.DrawingFrame.DrawingFrame) continue;
                        var (pnx, pny, pxx, pxy) = ExtractBoundingBox(ent.bounding);
                        if (pnx <= pxx && pny <= pxy)
                            canvas.DrawRect(new SKRect((float)pnx, (float)pny, (float)pxx, (float)pxy), paperPaint);
                    }
                }

                var gd = new SkiaGraphicsDraw(canvas, _scale, SKColors.Black);
                RefreshAssociations(_entities);                   // 全关联: 标记/标注锚点跟随锚定对象
                var dangling = ComputeDanglingDims(_entities);   // 悬空尺寸 → 灰显
                try
                {
                    foreach (var ent in _entities)
                    {
                        // 图层可见性 — 隐藏/冻结的图层跳过渲染
                        if (!_owner.IsEntityVisible(ent)) continue;
                        // 顶层实体默认色: 显式 RGB (ByColor/ByEntity) 用其颜色, 否则黑色.
                        // (ByLayer 解析需 DB 上下文且默认是白色 → 白底不可见, 故退化为黑.)
                        // 子图元 (Line/Arc/MText) 会在各自 Draw 里按自身显式颜色再覆盖.
                        gd.CurrentSkColor = ResolveEntitySkColor(ent, dangling);
                        try
                        {
                            ent.Draw(gd);
                        }
                        catch
                        {
                            // 单个实体绘制失败不影响其他实体
                        }
                    }

                    // POC-05: 渲染橡皮筋预览实体 (灰色, 与正式实体区分)
                    if (_previewEntity is not null)
                    {
                        gd.CurrentSkColor = new SKColor(0x88, 0x88, 0x88);
                        try { _previewEntity.Draw(gd); } catch { /* 忽略 */ }
                    }

                    // Phase 1A/1F+: 选中实体 overlay — 红色虚线 bbox + 真实 grip 点 (可拖)
                    if (_selected.Count > 0)
                    {
                        var sel = new SKPaint
                        {
                            Color = new SKColor(0xE0, 0x40, 0x40),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1.5f / _scale,
                            PathEffect = SKPathEffect.CreateDash(new[] { 6f / _scale, 4f / _scale }, 0)
                        };
                        var gripFill = new SKPaint
                        {
                            Color = new SKColor(0xFF, 0xFF, 0xFF),
                            Style = SKPaintStyle.Fill,
                        };
                        var gripStroke = new SKPaint
                        {
                            Color = new SKColor(0xE0, 0x40, 0x40),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1.0f / _scale,
                        };
                        float gripHalf = 5f / _scale;
                        foreach (var e in _selected)
                        {
                            try
                            {
                                // bbox 虚线框 (视觉提示选中范围)
                                var (minX, minY, maxX, maxY) = ExtractBoundingBox(e.bounding);
                                canvas.DrawRect(
                                    new SKRect((float)minX, (float)minY, (float)maxX, (float)maxY), sel);

                                // 真实 grip 点 — 来自 entity.GetGripPoints() (可拖)
                                // 状态: 默认 = 白底红框; hover = 浅蓝底; active = 实心蓝
                                List<GripPoint>? grips = null;
                                try { grips = e.GetGripPoints(); } catch { }
                                if (grips is not null)
                                {
                                    for (int gi = 0; gi < grips.Count; gi++)
                                    {
                                        var g = grips[gi];
                                        var r = new SKRect(
                                            (float)g.position.X - gripHalf, (float)g.position.Y - gripHalf,
                                            (float)g.position.X + gripHalf, (float)g.position.Y + gripHalf);
                                        var isActive = ReferenceEquals(e, _activeGripEntity) && gi == _activeGripIndex;
                                        var isHover = !isActive && ReferenceEquals(e, _hoverGripEntity) && gi == _hoverGripIndex;
                                        if (isActive)
                                        {
                                            // 按住中 = 实心蓝, 无边
                                            using var fill = new SKPaint { Color = new SKColor(0x20, 0x60, 0xD0), Style = SKPaintStyle.Fill, IsAntialias = true };
                                            canvas.DrawRect(r, fill);
                                        }
                                        else if (isHover)
                                        {
                                            // 悬停 = 浅蓝底 + 蓝边
                                            using var fill = new SKPaint { Color = new SKColor(0xB8, 0xD8, 0xF8), Style = SKPaintStyle.Fill };
                                            using var stroke = new SKPaint { Color = new SKColor(0x20, 0x60, 0xD0), Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f / _scale };
                                            canvas.DrawRect(r, fill);
                                            canvas.DrawRect(r, stroke);
                                        }
                                        else
                                        {
                                            canvas.DrawRect(r, gripFill);
                                            canvas.DrawRect(r, gripStroke);
                                        }
                                    }
                                }
                                else
                                {
                                    // fallback: 没有 grip 的 entity 用 bbox 4 角
                                    foreach (var (gx, gy) in new[]
                                    {
                                        ((float)minX, (float)minY), ((float)maxX, (float)minY),
                                        ((float)maxX, (float)maxY), ((float)minX, (float)maxY)
                                    })
                                    {
                                        var r = new SKRect(gx - gripHalf, gy - gripHalf, gx + gripHalf, gy + gripHalf);
                                        canvas.DrawRect(r, gripFill);
                                        canvas.DrawRect(r, gripStroke);
                                    }
                                }
                            }
                            catch { /* ignore */ }
                        }
                        sel.Dispose(); gripFill.Dispose(); gripStroke.Dispose();
                    }

                    // Phase 1E: Snap 指示器 (端点=绿方块, 中点=黄三角, 圆心=蓝圆)
                    if (_snap is not null)
                    {
                        float size = 8f / _scale;
                        var fill = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
                        var stroke = new SKPaint
                        {
                            Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f / _scale,
                            Color = SKColors.Black, IsAntialias = true
                        };
                        try
                        {
                            float x = (float)_snap.Point.X, y = (float)_snap.Point.Y;
                            switch (_snap.Type)
                            {
                                case SnapType.Endpoint:
                                    fill.Color = new SKColor(0x40, 0xC0, 0x40);
                                    var r = new SKRect(x - size, y - size, x + size, y + size);
                                    canvas.DrawRect(r, fill); canvas.DrawRect(r, stroke);
                                    break;
                                case SnapType.Midpoint:
                                    fill.Color = new SKColor(0xE0, 0xC0, 0x20);
                                    using (var path = new SKPath())
                                    {
                                        path.MoveTo(x, y + size);
                                        path.LineTo(x + size, y - size);
                                        path.LineTo(x - size, y - size);
                                        path.Close();
                                        canvas.DrawPath(path, fill);
                                        canvas.DrawPath(path, stroke);
                                    }
                                    break;
                                case SnapType.Center:
                                    fill.Color = new SKColor(0x40, 0x80, 0xE0);
                                    canvas.DrawCircle(x, y, size, fill);
                                    canvas.DrawCircle(x, y, size, stroke);
                                    break;
                                case SnapType.Nearest:
                                    fill.Color = new SKColor(0xB0, 0x40, 0xC0);  // 紫色菱形 (区分于其他)
                                    using (var path = new SKPath())
                                    {
                                        path.MoveTo(x, y - size);
                                        path.LineTo(x + size, y);
                                        path.LineTo(x, y + size);
                                        path.LineTo(x - size, y);
                                        path.Close();
                                        canvas.DrawPath(path, fill);
                                        canvas.DrawPath(path, stroke);
                                    }
                                    break;
                            }
                        }
                        finally { fill.Dispose(); stroke.Dispose(); }
                    }

                    // Phase 1F: Marquee 矩形 — 左→右 蓝色实线/淡蓝填充, 右→左 绿色虚线/淡绿填充
                    if (_marqueeStart is not null && _marqueeEnd is not null)
                    {
                        var s = _marqueeStart.Value; var en = _marqueeEnd.Value;
                        bool crossing = en.X < s.X;
                        float minX = (float)System.Math.Min(s.X, en.X), maxX = (float)System.Math.Max(s.X, en.X);
                        float minY = (float)System.Math.Min(s.Y, en.Y), maxY = (float)System.Math.Max(s.Y, en.Y);
                        var rect = new SKRect(minX, minY, maxX, maxY);
                        using var fillM = new SKPaint
                        {
                            Style = SKPaintStyle.Fill,
                            Color = crossing
                                ? new SKColor(0x40, 0xC0, 0x40, 0x30)
                                : new SKColor(0x40, 0x80, 0xE0, 0x30)
                        };
                        using var strokeM = new SKPaint
                        {
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1.0f / _scale,
                            Color = crossing ? new SKColor(0x40, 0xA0, 0x40) : new SKColor(0x40, 0x80, 0xE0),
                            PathEffect = crossing ? SKPathEffect.CreateDash(new[] { 4f / _scale, 3f / _scale }, 0) : null
                        };
                        canvas.DrawRect(rect, fillM);
                        canvas.DrawRect(rect, strokeM);
                    }
                }
                finally
                {
                    gd.Dispose();
                }

                canvas.Restore();
            }
            finally
            {
                lease.Dispose();
            }

            sw.Stop();
            _owner.OnFrameRendered(sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Phase 1A: 网格 + 坐标轴. 在已应用 (Translate offset + Scale + Y翻转) 的 canvas 上工作.
        /// 主网格随缩放自适应步长 (1 / 10 / 100 / 1000 mm).
        /// </summary>
        private void DrawGrid(SKCanvas canvas)
        {
            // 通过屏幕→模型反变换计算可见区域
            // canvas 矩阵已是 [Translate offset; Scale(s,-s)], 模型 (mx,my) 屏幕 (mx*s+ox, -my*s+oy)
            // 屏幕原点 (0,0) → 模型 ((0-ox)/s, -(0-oy)/s) = (-ox/s, oy/s)
            // 屏幕 (w,h) → 模型 ((w-ox)/s, -(h-oy)/s)
            float w = (float)Bounds.Width;
            float h = (float)Bounds.Height;
            float mLeft = (0 - _offsetX) / _scale;
            float mRight = (w - _offsetX) / _scale;
            // Y 翻转
            float mTop = -(0 - _offsetY) / _scale;
            float mBottom = -(h - _offsetY) / _scale;
            if (mTop < mBottom) (mTop, mBottom) = (mBottom, mTop);

            // 主网格步长 ~80 像素一格
            double targetPixel = 80.0;
            double rawStep = targetPixel / _scale;
            double mag = System.Math.Pow(10, System.Math.Floor(System.Math.Log10(rawStep)));
            double step = mag;
            if (rawStep / mag >= 5) step = mag * 5;
            else if (rawStep / mag >= 2) step = mag * 2;
            double minorStep = step / 5.0;

            var minorPaint = new SKPaint
            {
                Color = new SKColor(0xF0, 0xF0, 0xF0),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.5f / _scale,
            };
            var majorPaint = new SKPaint
            {
                Color = new SKColor(0xD8, 0xD8, 0xD8),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.0f / _scale,
            };
            var axisXPaint = new SKPaint
            {
                Color = new SKColor(0xD0, 0x40, 0x40),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f / _scale,
            };
            var axisYPaint = new SKPaint
            {
                Color = new SKColor(0x40, 0xA0, 0x40),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f / _scale,
            };
            try
            {
                // 次网格
                if (_scale > 0.05f)
                {
                    for (double x = System.Math.Ceiling(mLeft / minorStep) * minorStep; x <= mRight; x += minorStep)
                        canvas.DrawLine((float)x, (float)mBottom, (float)x, (float)mTop, minorPaint);
                    for (double y = System.Math.Ceiling(mBottom / minorStep) * minorStep; y <= mTop; y += minorStep)
                        canvas.DrawLine((float)mLeft, (float)y, (float)mRight, (float)y, minorPaint);
                }
                // 主网格
                for (double x = System.Math.Ceiling(mLeft / step) * step; x <= mRight; x += step)
                    canvas.DrawLine((float)x, (float)mBottom, (float)x, (float)mTop, majorPaint);
                for (double y = System.Math.Ceiling(mBottom / step) * step; y <= mTop; y += step)
                    canvas.DrawLine((float)mLeft, (float)y, (float)mRight, (float)y, majorPaint);
                // 原点轴
                if (mLeft <= 0 && mRight >= 0)
                    canvas.DrawLine(0, (float)mBottom, 0, (float)mTop, axisYPaint);
                if (mBottom <= 0 && mTop >= 0)
                    canvas.DrawLine((float)mLeft, 0, (float)mRight, 0, axisXPaint);
            }
            finally
            {
                minorPaint.Dispose(); majorPaint.Dispose();
                axisXPaint.Dispose(); axisYPaint.Dispose();
            }
        }
    }
}
