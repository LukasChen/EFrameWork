using System.Collections.Generic;
using UnityEngine;

namespace EFrame.Runtime.Utils
{
    /// <summary>
    /// 岛屿检测器，用于检测2D网格中的连通区域
    /// </summary>
    public static class IslandDetector
    {
        #region 常量定义

        /// <summary>
        /// 四方向连接向量：上、下、左、右
        /// </summary>
        private static readonly Vector2Int[] DIRECTIONS =
        {
            Vector2Int.up,    // (0, 1)
            Vector2Int.down,  // (0, -1)
            Vector2Int.left,  // (-1, 0) 
            Vector2Int.right  // (1, 0)
        };

        #endregion

        #region 公共接口

        /// <summary>
        /// 使用深度优先搜索检测岛屿
        /// </summary>
        /// <param name="cells">需要检测的格子坐标列表</param>
        /// <returns>检测到的岛屿列表，按面积降序排列</returns>
        public static List<Island> FindIslands(List<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return new List<Island>();
            }

            var cellSet = new HashSet<Vector2Int>(cells);
            var visited = new HashSet<Vector2Int>();
            var islands = new List<Island>();

            foreach (var cell in cells)
            {
                if (visited.Contains(cell))
                    continue;

                // 发现新岛屿，开始DFS搜索
                var island = new Island();
                DepthFirstSearch(cell, cellSet, visited, island.Cells);
                island.CalculateBounds();
                islands.Add(island);
            }

            // 按面积降序排列
            islands.Sort((a, b) => b.Area.CompareTo(a.Area));
            return islands;
        }

        /// <summary>
        /// 查找包含指定格子的岛屿
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public static Island FindIslandAt(List<Vector2Int> cells, Vector2Int position)
        {
            var islands = FindIslands(cells);
            foreach (var island in islands)
            {
                if (island.Contains(position))
                    return island;
            }

            return null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 深度优先搜索连通区域
        /// </summary>
        /// <param name="current">当前搜索位置</param>
        /// <param name="cellSet">所有可用格子的集合</param>
        /// <param name="visited">已访问格子的集合</param>
        /// <param name="islandCells">当前岛屿的格子列表</param>
        private static void DepthFirstSearch(Vector2Int current, HashSet<Vector2Int> cellSet,
            HashSet<Vector2Int> visited, List<Vector2Int> islandCells)
        {
            // 使用栈实现非递归DFS，避免栈溢出
            var stack = new Stack<Vector2Int>();
            stack.Push(current);

            while (stack.Count > 0)
            {
                var pos = stack.Pop();

                if (visited.Contains(pos))
                    continue;

                visited.Add(pos);
                islandCells.Add(pos);

                // 检查四个方向的邻居
                foreach (var direction in DIRECTIONS)
                {
                    var neighbor = pos + direction;
                    if (cellSet.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 岛屿信息类
        /// </summary>
        public class Island
        {
            #region 属性

            /// <summary>
            /// 岛屿包含的所有格子坐标
            /// </summary>
            public List<Vector2Int> Cells { get; private set; } = new List<Vector2Int>();

            /// <summary>
            /// 边界框的最小坐标
            /// </summary>
            public Vector2Int MinBounds { get; private set; }

            /// <summary>
            /// 边界框的最大坐标
            /// </summary>
            public Vector2Int MaxBounds { get; private set; }

            /// <summary>
            /// 岛屿面积（格子数量）
            /// </summary>
            public int Area => Cells.Count;

            /// <summary>
            /// 相对于左下角的坐标列表
            /// </summary>
            public List<Vector2Int> RelativeCells { get; private set; } = new List<Vector2Int>();

            /// <summary>
            /// 岛屿尺寸（宽度 × 高度）
            /// </summary>
            public Vector2Int Size => new Vector2Int(
                MaxBounds.x - MinBounds.x + 1,
                MaxBounds.y - MinBounds.y + 1
            );

            /// <summary>
            /// 岛屿中心点坐标
            /// </summary>
            public Vector2 Center => new Vector2(
                (MinBounds.x + MaxBounds.x) * 0.5f,
                (MinBounds.y + MaxBounds.y) * 0.5f
            );

            #endregion

            #region 公共方法

            /// <summary>
            /// 计算岛屿边界和相对坐标
            /// </summary>
            public void CalculateBounds()
            {
                if (Cells.Count == 0)
                {
                    RelativeCells.Clear();
                    return;
                }

                // 计算边界框
                MinBounds = MaxBounds = Cells[0];
                foreach (var cell in Cells)
                {
                    MinBounds = Vector2Int.Min(MinBounds, cell);
                    MaxBounds = Vector2Int.Max(MaxBounds, cell);
                }

                // 计算相对坐标
                CalculateRelativeCoordinates();
            }

            /// <summary>
            /// 检查指定坐标是否在岛屿内
            /// </summary>
            /// <param name="position">要检查的坐标</param>
            /// <returns>是否在岛屿内</returns>
            public bool Contains(Vector2Int position)
            {
                return Cells.Contains(position);
            }

            /// <summary>
            /// 获取岛屿的描述信息
            /// </summary>
            /// <returns>描述字符串</returns>
            public override string ToString()
            {
                return $"Island: Area={Area}, Size={Size}, Center={Center}";
            }

            #endregion

            #region 私有方法

            /// <summary>
            /// 计算相对于左下角的坐标
            /// </summary>
            private void CalculateRelativeCoordinates()
            {
                RelativeCells.Clear();

                foreach (var cell in Cells)
                {
                    // 减去最小边界作为偏移，得到相对于左下角的坐标
                    var relativePos = cell - MinBounds;
                    RelativeCells.Add(relativePos);
                }

                // 按坐标排序，便于调试和显示
                RelativeCells.Sort(CompareVector2Int);
            }

            /// <summary>
            /// Vector2Int 比较函数，先按Y轴后按X轴排序
            /// </summary>
            /// <param name="a">第一个向量</param>
            /// <param name="b">第二个向量</param>
            /// <returns>比较结果</returns>
            private static int CompareVector2Int(Vector2Int a, Vector2Int b)
            {
                if (a.y != b.y)
                    return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            }

            #endregion
        }

        #endregion
    }
}
