using System;
using System.Collections.Generic;
using System.Text;
using LitMath;

namespace OtoCAD
{
    /// <summary>
    /// 画布接口
    /// </summary>
    public interface ICanvas
    {
        // 画布宽
        double width { get; }
        // 画布高
        double height { get; }

#if WINDOWS
        /// <summary>
        /// 设置Presenter (WinForms 专用)
        /// </summary>
        void SetPresenter(IPresenter controller);
#endif

        /// <summary>
        /// 添加子元素
        /// </summary>
        void AddChild(object child);

        /// <summary>
        /// 删除子元素
        /// </summary>
        void RemoveChild(object child);

        /// <summary>
        /// 重绘
        /// </summary>
        void Repaint();
        void Repaint(double x, double y, double width, double height);

        /// <summary>
        /// 获取与设置鼠标位置
        /// </summary>
        Vector2 GetMousePosition();
        void SetMousePosition(Vector2 postion);
        void ExportImage(string spath);
    }
}
