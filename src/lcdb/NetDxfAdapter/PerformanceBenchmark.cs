using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using LitMath;
using lcdb;

namespace lcdb.NetDxfAdapter
{
    /// <summary>
    /// 性能基准测试类，用于比较传统OtoCAD实体和NetDxf适配器的性能
    /// </summary>
    public class PerformanceBenchmark
    {
        private const int ITERATIONS = 10000;
        private const int WARMUP_ITERATIONS = 1000;

        /// <summary>
        /// 基准测试结果
        /// </summary>
        public class BenchmarkResult
        {
            public string TestName { get; set; }
            public string Implementation { get; set; }
            public long ElapsedMilliseconds { get; set; }
            public long MemoryUsed { get; set; }
            public double OperationsPerSecond { get; set; }
            public string Notes { get; set; }
        }

        /// <summary>
        /// 运行所有基准测试
        /// </summary>
        public static List<BenchmarkResult> RunAllBenchmarks()
        {
            var results = new List<BenchmarkResult>();
            
            Console.WriteLine("开始运行NetDxf适配器性能基准测试...");
            
            // 实体创建测试
            results.AddRange(BenchmarkEntityCreation());
            
            // 绘制性能测试
            results.AddRange(BenchmarkDrawing());
            
            // 夹点操作测试
            results.AddRange(BenchmarkGripPoints());
            
            // 变换操作测试
            results.AddRange(BenchmarkTransformations());
            
            // 边界计算测试
            results.AddRange(BenchmarkBoundingCalculation());
            
            // 内存使用测试
            results.AddRange(BenchmarkMemoryUsage());
            
            Console.WriteLine("基准测试完成。");
            return results;
        }

        /// <summary>
        /// 实体创建性能测试
        /// </summary>
        private static List<BenchmarkResult> BenchmarkEntityCreation()
        {
            var results = new List<BenchmarkResult>();
            
            // 传统Text实体创建
            var sw = Stopwatch.StartNew();
            var startMemory = GC.GetTotalMemory(false);
            
            // 预热
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                var traditionalText = new Text();
                traditionalText.SetText("测试文本");
                traditionalText.SetHeight(10.0);
                traditionalText.SetPosition(new Vector3(i, i));
            }
            
            GC.Collect();
            startMemory = GC.GetTotalMemory(false);
            sw.Restart();
            
            // 实际测试
            for (int i = 0; i < ITERATIONS; i++)
            {
                var traditionalText = new Text();
                traditionalText.SetText("测试文本");
                traditionalText.SetHeight(10.0);
                traditionalText.SetPosition(new Vector3(i, i));
            }
            
            sw.Stop();
            var endMemory = GC.GetTotalMemory(false);
            
            results.Add(new BenchmarkResult
            {
                TestName = "实体创建",
                Implementation = "传统Text",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                MemoryUsed = endMemory - startMemory,
                OperationsPerSecond = ITERATIONS / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"创建{ITERATIONS}个Text实体"
            });
            
            // NetDxf适配器Text实体创建
            GC.Collect();
            startMemory = GC.GetTotalMemory(false);
            sw.Restart();
            
            // 预热
            for (int i = 0; i < WARMUP_ITERATIONS; i++)
            {
                var adapterText = new TextEntityAdapter(new Vector2(i, i), "测试文本", 10.0);
            }
            
            GC.Collect();
            startMemory = GC.GetTotalMemory(false);
            sw.Restart();
            
            // 实际测试
            for (int i = 0; i < ITERATIONS; i++)
            {
                var adapterText = new TextEntityAdapter(new Vector2(i, i), "测试文本", 10.0);
            }
            
            sw.Stop();
            endMemory = GC.GetTotalMemory(false);
            
            results.Add(new BenchmarkResult
            {
                TestName = "实体创建",
                Implementation = "NetDxf适配器Text",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                MemoryUsed = endMemory - startMemory,
                OperationsPerSecond = ITERATIONS / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"创建{ITERATIONS}个TextEntityAdapter"
            });
            
            return results;
        }

        /// <summary>
        /// 绘制性能测试
        /// </summary>
        private static List<BenchmarkResult> BenchmarkDrawing()
        {
            var results = new List<BenchmarkResult>();
            var mockGraphics = new MockGraphicsDraw();
            
            // 准备测试数据
            var traditionalEntities = new List<Entity>();
            var adapterEntities = new List<Entity>();
            
            for (int i = 0; i < 1000; i++)
            {
                var traditionalCircle = new Circle();
                traditionalCircle.SetCenter(new Vector2(i, i));
                traditionalCircle.SetRadius(10.0);
                traditionalEntities.Add(traditionalCircle);
                
                var adapterCircle = new CircleEntityAdapter(new Vector2(i, i), 10.0);
                adapterEntities.Add(adapterCircle);
            }
            
            // 测试传统绘制
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                foreach (var entity in traditionalEntities)
                {
                    entity.Draw(mockGraphics);
                }
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "绘制性能",
                Implementation = "传统Circle",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = (1000 * 100) / (sw.ElapsedMilliseconds / 1000.0),
                Notes = "绘制1000个圆形实体100次"
            });
            
            // 测试适配器绘�?
            sw.Restart();
            
            for (int i = 0; i < 100; i++)
            {
                foreach (var entity in adapterEntities)
                {
                    entity.Draw(mockGraphics);
                }
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "绘制性能",
                Implementation = "NetDxf适配器Circle",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = (1000 * 100) / (sw.ElapsedMilliseconds / 1000.0),
                Notes = "绘制1000个圆形适配器100次"
            });
            
            return results;
        }

        /// <summary>
        /// 夹点操作性能测试
        /// </summary>
        private static List<BenchmarkResult> BenchmarkGripPoints()
        {
            var results = new List<BenchmarkResult>();
            
            // 准备测试实体
            var traditionalLine = new Line();
            traditionalLine.startPoint = new Vector2(0, 0);
            traditionalLine.endPoint = new Vector2(100, 100);
            
            var adapterLine = new LineEntityAdapter(new Vector2(0, 0), new Vector2(100, 100));
            
            // 测试传统夹点获取
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < ITERATIONS; i++)
            {
                var gripPoints = traditionalLine.GetGripPoints();
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "夹点操作",
                Implementation = "传统Line - 获取夹点",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = ITERATIONS / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"获取夹点{ITERATIONS}次"
            });
            
            // 测试适配器夹点获�?
            sw.Restart();
            
            for (int i = 0; i < ITERATIONS; i++)
            {
                var gripPoints = adapterLine.GetGripPoints();
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "夹点操作",
                Implementation = "NetDxf适配器Line - 获取夹点",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = ITERATIONS / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"获取夹点{ITERATIONS}次"
            });
            
            return results;
        }

        /// <summary>
        /// 变换操作性能测试
        /// </summary>
        private static List<BenchmarkResult> BenchmarkTransformations()
        {
            var results = new List<BenchmarkResult>();
            
            // 准备测试实体
            var traditionalCircle = new Circle();
            traditionalCircle.SetCenter(new Vector2(50, 50));
            traditionalCircle.SetRadius(25.0);
            
            var adapterCircle = new CircleEntityAdapter(new Vector2(50, 50), 25.0);
            
            // 测试传统变换
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < ITERATIONS; i++)
            {
                traditionalCircle.Translate(new Vector2(1, 1));
                traditionalCircle.Rotate(new Vector2(0, 0), 0.1);           
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "变换操作",
                Implementation = "传统Circle",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = (ITERATIONS * 3) / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"执行{ITERATIONS}次变换操作(平移+旋转+缩放)"
            });
            
            // 测试适配器变�?
            sw.Restart();
            
            for (int i = 0; i < ITERATIONS; i++)
            {
                adapterCircle.Translate(new Vector2(1, 1));
                adapterCircle.Rotate(new Vector2(0, 0), 0.1);
                adapterCircle.Scale(new Vector2(0, 0), 1.1);
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "变换操作",
                Implementation = "NetDxf适配器Circle",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = (ITERATIONS * 3) / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"执行{ITERATIONS}次变换操作(平移+旋转+缩放)"
            });
            
            return results;
        }

        /// <summary>
        /// 边界计算性能测试
        /// </summary>
        private static List<BenchmarkResult> BenchmarkBoundingCalculation()
        {
            var results = new List<BenchmarkResult>();
            
            // 准备测试实体
            var traditionalText = new Text();
            traditionalText.SetText("测试文本边界计算");
            traditionalText.SetHeight(12.0);
            traditionalText.SetPosition(new Vector3(100, 100));
            
            var adapterText = new TextEntityAdapter(new Vector2(100, 100), "测试文本边界计算", 12.0);
            
            // 测试传统边界计算
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < ITERATIONS; i++)
            {
                var bounding = traditionalText.bounding;
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "边界计算",
                Implementation = "传统Text",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = ITERATIONS / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"计算边界{ITERATIONS}次"
            });
            
            // 测试适配器边界计�?
            sw.Restart();
            
            for (int i = 0; i < ITERATIONS; i++)
            {
                var bounding = adapterText.bounding;
            }
            
            sw.Stop();
            
            results.Add(new BenchmarkResult
            {
                TestName = "边界计算",
                Implementation = "NetDxf适配器Text",
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                OperationsPerSecond = ITERATIONS / (sw.ElapsedMilliseconds / 1000.0),
                Notes = $"计算边界{ITERATIONS}次"
            });
            
            return results;
        }

        /// <summary>
        /// 内存使用测试
        /// </summary>
        private static List<BenchmarkResult> BenchmarkMemoryUsage()
        {
            var results = new List<BenchmarkResult>();
            
            GC.Collect();
            var startMemory = GC.GetTotalMemory(false);
            
            // 创建大量传统实体
            var traditionalEntities = new List<Entity>();
            for (int i = 0; i < 10000; i++)
            {
                var circle = new Circle();
                circle.SetCenter(new Vector2(i, i));
                circle.SetRadius(10.0);
                traditionalEntities.Add(circle);
            }
            
            GC.Collect();
            var traditionalMemory = GC.GetTotalMemory(false);
            
            results.Add(new BenchmarkResult
            {
                TestName = "内存使用",
                Implementation = "传统Circle",
                MemoryUsed = traditionalMemory - startMemory,
                Notes = "创建10000个圆形实体的内存使用"
            });
            
            // 清理
            traditionalEntities.Clear();
            GC.Collect();
            startMemory = GC.GetTotalMemory(false);
            
            // 创建大量适配器实体
            var adapterEntities = new List<Entity>();
            for (int i = 0; i < 10000; i++)
            {
                var circle = new CircleEntityAdapter(new Vector2(i, i), 10.0);
                adapterEntities.Add(circle);
            }
            
            GC.Collect();
            var adapterMemory = GC.GetTotalMemory(false);
            
            results.Add(new BenchmarkResult
            {
                TestName = "内存使用",
                Implementation = "NetDxf适配器Circle",
                MemoryUsed = adapterMemory - startMemory,
                Notes = "创建10000个圆形适配器的内存使用"
            });
            
            return results;
        }

        /// <summary>
        /// 打印测试结果
        /// </summary>
        public static void PrintResults(List<BenchmarkResult> results)
        {
            Console.WriteLine("\n=== NetDxf适配器性能基准测试结果 ===\n");
            
            foreach (var group in GroupResultsByTest(results))
            {
                Console.WriteLine($"测试: {group.Key}");
                Console.WriteLine(new string('-', 80));
                
                foreach (var result in group.Value)
                {
                    Console.WriteLine($"实现: {result.Implementation}");
                    if (result.ElapsedMilliseconds > 0)
                        Console.WriteLine($"  耗时: {result.ElapsedMilliseconds}ms");
                    if (result.OperationsPerSecond > 0)
                        Console.WriteLine($"  操作/秒: {result.OperationsPerSecond:F2}");
                    if (result.MemoryUsed > 0)
                        Console.WriteLine($"  内存使用: {result.MemoryUsed / 1024.0:F2} KB");
                    Console.WriteLine($"  说明: {result.Notes}");
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }

        private static Dictionary<string, List<BenchmarkResult>> GroupResultsByTest(List<BenchmarkResult> results)
        {
            var groups = new Dictionary<string, List<BenchmarkResult>>();
            
            foreach (var result in results)
            {
                if (!groups.ContainsKey(result.TestName))
                    groups[result.TestName] = new List<BenchmarkResult>();
                
                groups[result.TestName].Add(result);
            }
            
            return groups;
        }
    }

    /// <summary>
    /// 模拟绘制类，用于测试
    /// </summary>
    public class MockGraphicsDraw : OtoCAD.IGraphicsDraw
    {
        public OtoCAD.RenderMode CurrentMode => OtoCAD.RenderMode.Normal;
        public void DrawPoint(LitMath.Vector2 endPoint) { }
        public void DrawLine(LitMath.Vector2 startPoint, LitMath.Vector2 endPoint) { }
        public void DrawLine(netDxf.Vector2 startPoint, netDxf.Vector2 endPoint) { }
        public void DrawLineDimension(LitMath.Vector2 startPoint, LitMath.Vector2 endPoint) { }
        public void DrawSolidArrow(netDxf.Entities.Solid solid) { }
        public void DrawXLine(LitMath.Vector2 basePoint, LitMath.Vector2 direction) { }
        public void DrawRay(LitMath.Vector2 basePoint, LitMath.Vector2 direction) { }
        public void DrawCircle(LitMath.Vector2 center, double radius) { }
        public void DrawEllipse(LitMath.Vector2 center, double radiusX, double radiusY) { }
        public void DrawArc(LitMath.Vector2 center, double radius, double startAngle, double endAngle) { }
        public void DrawArc(netDxf.Vector2 center, double radius, double startAngle, double endAngle) { }
        public void DrawRectangle(LitMath.Vector2 position, double width, double height) { }
        public void DrawTriangle(LitMath.Vector2 vertex1, LitMath.Vector2 vertex2, LitMath.Vector2 vertex3) { }
        public void DrawQuadrilateral(LitMath.Vector2 vertex1, LitMath.Vector2 vertex2, LitMath.Vector2 vertex3, LitMath.Vector2 vertex4) { }
        public LitMath.Vector2 DrawText(LitMath.Vector2 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle) { return position; }
        public LitMath.Vector2 DrawText(LitMath.Vector3 position, string text, double height, string font, lcdb.TextAlignment textAlign, double angle) { return new LitMath.Vector2(position.X, position.Y); }
    }
}