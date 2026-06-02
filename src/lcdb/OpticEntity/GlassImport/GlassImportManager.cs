using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OtoCAD.OpticEntity;

namespace OtoCAD.OpticEntity.GlassImport
{
    /// <summary>
    /// 玻璃库导入管理器
    /// </summary>
    public class GlassImportManager
    {
        private readonly List<IGlassImporter> _importers;
        private static GlassImportManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static GlassImportManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GlassImportManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private GlassImportManager()
        {
            _importers = new List<IGlassImporter>
            {
                new CDGMGlassImporter(),
                new SchottGlassImporter()
            };
        }

        /// <summary>
        /// 注册新的导入器
        /// </summary>
        public void RegisterImporter(IGlassImporter importer)
        {
            if (importer != null && !_importers.Any(i => i.GetType() == importer.GetType()))
            {
                _importers.Add(importer);
            }
        }

        /// <summary>
        /// 获取所有支持的文件扩展名
        /// </summary>
        public string[] GetSupportedExtensions()
        {
            return _importers.SelectMany(i => i.SupportedExtensions).Distinct().ToArray();
        }

        /// <summary>
        /// 获取文件过滤器字符串（用于文件对话框）
        /// </summary>
        public string GetFileFilter()
        {
            var filters = new List<string>();
            
            // 添加所有支持的格式
            var allExtensions = string.Join(";", GetSupportedExtensions().Select(ext => $"*{ext}"));
            filters.Add($"所有支持的玻璃库文件|{allExtensions}");
            
            // 添加各个厂商的过滤器
            foreach (var importer in _importers)
            {
                var extensions = string.Join(";", importer.SupportedExtensions.Select(ext => $"*{ext}"));
                filters.Add($"{importer.Name}|{extensions}");
            }
            
            filters.Add("所有文件|*.*");
            
            return string.Join("|", filters);
        }

        /// <summary>
        /// 从文件导入玻璃材料
        /// </summary>
        public ImportResult ImportFromFile(string filePath)
        {
            var result = new ImportResult
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath
            };

            try
            {
                // 找到合适的导入器
                var importer = _importers.FirstOrDefault(i => i.CanImport(filePath));
                
                if (importer == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "未找到支持该文件格式的导入器";
                    return result;
                }

                result.ImporterName = importer.Name;
                result.Manufacturer = importer.Manufacturer;
                
                // 执行导入
                var materials = importer.Import(filePath);
                result.Materials = materials;
                result.Success = true;
                result.ImportedCount = materials.Count;
                
                // 添加导入时间戳和版本信息
                foreach (var material in materials)
                {
                    if (material.Remarks == null)
                        material.Remarks = "";
                    
                    material.Remarks += $"\n导入自: {Path.GetFileName(filePath)}";
                    material.Remarks += $"\n导入时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    material.CreateTime = DateTime.Now;
                    material.UpdateTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// 批量导入多个文件
        /// </summary>
        public List<ImportResult> ImportFromFiles(string[] filePaths)
        {
            var results = new List<ImportResult>();
            
            foreach (var filePath in filePaths)
            {
                results.Add(ImportFromFile(filePath));
            }
            
            return results;
        }
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ImporterName { get; set; }
        public string Manufacturer { get; set; }
        public List<GlassMaterial> Materials { get; set; }
        public int ImportedCount { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        public ImportResult()
        {
            Materials = new List<GlassMaterial>();
        }
    }
}