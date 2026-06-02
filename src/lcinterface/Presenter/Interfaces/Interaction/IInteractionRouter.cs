#if WINDOWS
using System;
using System.Windows.Forms;
using LitMath;
using OtoCAD.Interfaces; // switched from OtoCAD.Presenter.Interfaces.Layer to unified interfaces
using OtoCAD.Presenter.Interfaces.Rendering;

namespace OtoCAD.Presenter.Interfaces.Interaction
{
    /// <summary>
    /// 交互路由器接口
    /// 负责路由和分发用户输入事件
    /// </summary>
    public interface IInteractionRouter
    {
        #region 事件路由

        /// <summary>
        /// 路由鼠标事件
        /// </summary>
        /// <param name="e">鼠标事件参数</param>
        /// <param name="context">交互上下文</param>
        /// <returns>事件是否已处理</returns>
        bool RouteMouseEvent(MouseEventArgs e, InteractionContext context);

        /// <summary>
        /// 路由键盘事件
        /// </summary>
        /// <param name="e">键盘事件参数</param>
        /// <param name="context">交互上下文</param>
        /// <returns>事件是否已处理</returns>
        bool RouteKeyEvent(KeyEventArgs e, InteractionContext context);

        #endregion

        #region 输入处理器管理

        /// <summary>
        /// 添加输入处理器
        /// </summary>
        void AddInputHandler(IInputHandler handler);

        /// <summary>
        /// 移除输入处理器
        /// </summary>
        void RemoveInputHandler(IInputHandler handler);

        /// <summary>
        /// 清除所有输入处理器
        /// </summary>
        void ClearInputHandlers();

        /// <summary>
        /// 设置输入处理器的优先级
        /// </summary>
        void SetHandlerPriority(IInputHandler handler, int priority);

        #endregion

        #region 状态查询

        /// <summary>
        /// 是否有活动的交互
        /// </summary>
        bool HasActiveInteraction { get; }

        /// <summary>
        /// 获取当前活动的处理器
        /// </summary>
        IInputHandler ActiveHandler { get; }

        #endregion
    }

    /// <summary>
    /// 输入处理器接口
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// 处理器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 优先级（数值越小优先级越高）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 是否启用
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 判断是否可以处理该事件
        /// </summary>
        bool CanHandle(EventArgs e, InteractionContext context);

        /// <summary>
        /// 处理鼠标事件
        /// </summary>
        InputResult HandleMouseEvent(MouseEventArgs e, InteractionContext context);

        /// <summary>
        /// 处理键盘事件
        /// </summary>
        InputResult HandleKeyEvent(KeyEventArgs e, InteractionContext context);

        /// <summary>
        /// 处理器激活时调用
        /// </summary>
        void OnActivated();

        /// <summary>
        /// 处理器停用时调用
        /// </summary>
        void OnDeactivated();
    }

    /// <summary>
    /// 交互上下文
    /// </summary>
    public class InteractionContext
    {
        /// <summary>
        /// 变换管理器
        /// </summary>
        public ITransformManager Transform { get; set; }

        /// <summary>
        /// 当前鼠标位置（画布坐标）
        /// </summary>
        public Vector2 MousePosition { get; set; }

        /// <summary>
        /// 当前鼠标位置（模型坐标）
        /// </summary>
        public Vector2 MousePositionInModel { get; set; }

        /// <summary>
        /// 是否启用捕捉
        /// </summary>
        public bool SnapEnabled { get; set; }

        /// <summary>
        /// 当前捕捉点
        /// </summary>
        public SnapPoint CurrentSnapPoint { get; set; }

        /// <summary>
        /// 是否启用正交模式
        /// </summary>
        public bool OrthoMode { get; set; }

        /// <summary>
        /// 选择集（使用 object 类型以避免接口依赖）
        /// </summary>
        public object Selection { get; set; }

        /// <summary>
        /// 当前图层（使用统一接口契约）
        /// </summary>
        public ILayer CurrentLayer { get; set; }

        /// <summary>
        /// 自定义数据
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> CustomData { get; set; }
    }

    /// <summary>
    /// 输入处理结果
    /// </summary>
    public class InputResult
    {
        public bool Handled { get; set; }
        public bool NeedRepaint { get; set; }
        public bool ContinuePropagation { get; set; }

        public static InputResult CreateHandled(bool needRepaint = true)
        {
            return new InputResult { Handled = true, NeedRepaint = needRepaint, ContinuePropagation = false };
        }

        public static InputResult CreateNotHandled()
        {
            return new InputResult { Handled = false, NeedRepaint = false, ContinuePropagation = true };
        }
    }

    public class SnapPoint
    {
        public Vector2 Position { get; set; }
        public SnapType Type { get; set; }
        public object OwnerEntity { get; set; }
    }

    public enum SnapType
    {
        None,
        EndPoint,
        MidPoint,
        Center,
        Quadrant,
        Intersection,
        Perpendicular,
        Tangent,
        Nearest,
        Grid
    }

}
#endif