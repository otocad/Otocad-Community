namespace OtoCAD
{
    /// <summary>
    /// 渲染模式(AD-1, Story 10-1)。
    ///
    /// 用于切换屏幕预览效果,模拟黑白打印 / WCAG AA 验证场景。
    /// Presenter 持有当前模式,通过 IGraphicsDraw.CurrentMode 暴露给 Entity.Draw 实现。
    /// </summary>
    public enum RenderMode
    {
        /// <summary>正常模式 — 实体按自有 color 渲染(默认)</summary>
        Normal = 0,

        /// <summary>
        /// 黑白预览模式 — CoatingMark 颜色覆盖为黑色(本期 scope:仅 CoatingMark,
        /// 其它实体本期不强制黑色,留 Epic 10 v1.1 backlog)。
        /// </summary>
        Monochrome = 1,
    }
}
