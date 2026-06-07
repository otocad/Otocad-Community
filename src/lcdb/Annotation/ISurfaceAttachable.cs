using LitMath;

namespace lcdb.Annotation
{
    /// <summary>
    /// 可"贴面放置"的标记。放置命令在用户把标记移到某条被测面 (Line/Arc/Circle, 含透镜表面)
    /// 上方时调用本方法,标记据此把自己贴到面上 — 参考点落在面上的吸附点、符号沿外法线方向竖立。
    ///
    /// 设计意图: 命令只负责"找到面 + 算出该点的外法线", 各标记自己决定怎么贴
    /// (锚点字段 Center/Position、旋转单位 弧度/度、沿法线的偏移量都各不相同),
    /// 避免放置命令去了解每种标记的几何细节。参考实现见 SurfaceRoughnessMark。
    /// </summary>
    public interface ISurfaceAttachable
    {
        /// <summary>
        /// 把标记贴到被测面上。
        /// </summary>
        /// <param name="surfacePoint">被测面上的吸附点 (符号的"接触点"应落在这里)。</param>
        /// <param name="outwardNormal">该点处指向材料外侧的单位法线 (符号朝这个方向竖立, 已归一化)。</param>
        void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal);
    }
}
