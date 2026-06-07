namespace lcdb
{
    /// <summary>
    /// 线型 (Entity 与 IGraphicsDraw 共享).
    ///
    /// 注: 该枚举原位于 lcdb 项目, 为让 lcinterface.IGraphicsDraw.CurrentLineType
    /// 引用同一类型 (避免 lcinterface→lcdb 反向依赖), 已移至 lcinterface 项目 —
    /// 命名空间不变 (lcdb), 旧引用代码无需改动.
    /// </summary>
    public enum LineType
    {
        // ByLineTypeDefault = -3,
        ByBlock = -2,
        ByLayer = -1,
        Solid = 0,      //     Specifies a solid line.
        Dash,           //     Specifies a line consisting of dashes.
        Dot,            //     Specifies a line consisting of dots.
        DashDot,        //     Specifies a line consisting of a repeating pattern of dash-dot.
        DashDotDot,     //     Specifies a line consisting of a repeating pattern of dash-dot-dot (GB/T 13323-2009 §2.1 光轴).
        Custom          //     Specifies a user-defined custom dash style.
    }
}
