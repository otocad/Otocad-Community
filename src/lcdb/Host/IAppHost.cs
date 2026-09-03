#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OtoCAD;   // Entity

namespace lcdb.Host
{
    /// <summary>
    /// 免费内核 → 付费模块(OtoCAD.Cloud)的扩展宿主接口 (open-core).
    /// 付费模块只依赖本接口 + lcdb 实体, **不碰任何 UI 类型** —— 所有 UI(文件选择 / 对话框 /
    /// 建文档 tab / 出 ISO 图纸 / 状态栏)由免费内核(OtoCAD.Avalonia)实现, 故无循环引用,
    /// OtoCAD.Cloud.dll 可被免费内核反射加载; 免费版无该 dll 即无导入入口.
    /// </summary>
    public interface IAppHost
    {
        /// <summary>注册命令 (id → 点击回调). 内核保证切 tab 重建命令表后仍有效.</summary>
        void RegisterCommand(string id, Action handler);

        /// <summary>弹文件选择, 返回本地路径; null = 取消.</summary>
        Task<string?> PickFileAsync(string title, params string[] patterns);

        /// <summary>弹元件选择 + 预览对话框, 返回选择结果; null = 取消.</summary>
        Task<IImportSelection?> ShowImportSelectionAsync(IReadOnlyList<Entity> elements, string summary);

        /// <summary>新建文档 tab, 放入整套元件 (光路图总览).</summary>
        void AddLayoutTab(string title, IReadOnlyList<Entity> elements);

        /// <summary>新建文档 tab, 为单个元件生成 ISO 加工图.</summary>
        void AddElementSheetTab(string title, Entity element);

        /// <summary>把元件加进当前活动文档.</summary>
        void AddToActiveDocument(IReadOnlyList<Entity> elements);

        /// <summary>状态栏提示.</summary>
        void SetStatus(string text);
    }

    /// <summary>导入选择对话框返回的结果.</summary>
    public interface IImportSelection
    {
        IReadOnlyList<Entity> Selected { get; }
        bool MakeLayoutTab { get; }
        bool MakePerElementTabs { get; }
    }
}
