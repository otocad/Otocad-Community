using System.Collections.Generic;

namespace lcdb;

/// <summary>
/// 可穿透拾取的复合实体 — 实现此接口的实体在 PickEntityAt(pierce: true) 时,
/// 拾取算法会下钻到其内部子实体, 返回深层的具体几何 (Arc / Line / ...)
/// 而不是返回复合体本身.
///
/// 典型用例:
///   - <c>OpticalLens</c>: 前/后表面 Arc + 上下边线 Line + 光轴 Line
///     → 用户点前弧时, 拾取返回那条 Arc 而非 lens
///   - <c>CementedLens</c>: R1/RContact/R3 三条 Arc + 上下边线 + 光轴
///   - <c>OpticalDrawingFrame</c>: 内部表格 Line / Text
///
/// 注意子实体生命周期:
///   - 子实体由复合体的 Generate() 维护, 调用方不应单独持久化对它们的引用
///     (复合体参数变 → 子实体被重建; 若有持久引用 → 失效)
///   - 标注 / mark 若需要锚定子实体, 应通过"角色路径" (e.g., "FrontArc")
///     在每次访问时 ResolveSubEntity 取最新引用
/// </summary>
public interface IPierceable
{
    /// <summary>返回当前可被穿透拾取到的子实体快照. 顺序无关.</summary>
    IEnumerable<Entity> GetPierceableSubEntities();
}
