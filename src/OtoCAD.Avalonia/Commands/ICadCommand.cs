using LitMath;
using lcdb;

namespace OtoCAD.Avalonia.Commands;

/// <summary>
/// POC-05: 多步 CAD 命令接口.
///
/// 典型生命周期:
///   1. host.StartCommand(new XxxCmd()) → Start()
///   2. 用户在画布上点击 → OnMouseClick(modelPoint)
///   3. 鼠标移动 → OnMouseMove(modelPoint) (用于橡皮筋预览)
///   4. 完成 → 命令内部调用 host.FinishCommand()
///   5. 用户按 ESC → Cancel()
/// </summary>
public interface ICadCommand
{
    string Name { get; }
    void Start(ICadCommandHost host);
    void OnMouseClick(Vector2 modelPoint);
    void OnMouseMove(Vector2 modelPoint);
    void Cancel();

    /// <summary>右键确认 (默认空, Polyline/Spline 等多点命令用于"完成").</summary>
    void OnRightClick(Vector2 modelPoint) { }

    /// <summary>回车确认 (默认空, 同 OnRightClick 的语义).</summary>
    void OnConfirm() { }

    /// <summary>
    /// 命令偏好的 snap 类型集合 (null = 用 canvas 默认).
    /// 标记/注释类命令应返回 SnapType.Nearest (只 snap 到实体边, 不 snap 到端点/中点),
    /// 几何画图命令返回 null 用默认 (端点/中点/圆心).
    /// </summary>
    OtoCAD.Avalonia.Snap.SnapType? PreferredSnapTypes => null;
}

/// <summary>
/// 命令宿主接口, 由 CadCanvas 实现.
/// 命令通过宿主与画布交互, 不直接持有 canvas 引用.
/// </summary>
public interface ICadCommandHost
{
    /// <summary>把实体加入画布场景, 触发重绘.</summary>
    void AddEntity(Entity entity);

    /// <summary>设置橡皮筋预览实体 (null 清空), 触发重绘.</summary>
    void SetPreview(Entity? previewEntity);

    /// <summary>显示提示文字 (状态栏).</summary>
    void SetPrompt(string prompt);

    /// <summary>结束当前命令, 清空 active command + 预览.</summary>
    void FinishCommand();

    /// <summary>
    /// 在模型坐标 modelPoint 附近 tolerance (模型单位) 内拾取最近的实体, 找不到返回 null.
    /// 用于"选实体而非选点"的命令 (如半径/直径标注先选圆/弧).
    /// </summary>
    /// <param name="pierce">true = 命中复合实体 (IPierceable) 时下钻到内部子实体,
    /// 返回最深的具体几何 (Arc/Line). 标注类命令应传 true 让用户点 lens 表面就标注那条 Arc.</param>
    Entity? PickEntityAt(Vector2 modelPoint, double tolerance, bool pierce = false) => null;

    /// <summary>
    /// 当前光标的「原始」模型坐标 (未经 snap 吸附). 与 OnMouseMove/OnMouseClick 收到的
    /// (已吸附) 点之差正好是沿被测面法线方向的偏移 — 贴面标记据此判断用户所在的一侧/外法线方向。
    /// </summary>
    Vector2 RawCursorModel => default;
}
