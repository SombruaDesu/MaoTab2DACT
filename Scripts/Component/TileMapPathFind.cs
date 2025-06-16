using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using MaoTab.Scripts;

public partial class TileMapPathFind : TileMapLayer
{
    
    [Export] public bool ShowDebugGraph = true; 
    [Export] public int JumpDistance = 5; // 跳跃距离
    [Export] public int JumpHeight = 4; // 跳跃高度
    private const int CELL_IS_EMPTY = -1; // TileMap将空白空间定义为-1
    private const int MAX_TILE_FALL_SCAN_DEPTH = 500; // 固定瓷砖向下扫描的最大瓷砖数量

    private AStar2D _astarGraph = new(); // 寻路图
    private Array<Vector2I> _usedTiles; // TileMap中使用的瓷砖
    private PackedScene _graphpoint; // Debug绘制点

    private List<PointInfo> _pointInfoList = new();
    
    [Export] public int StepHeight = 1;   // 最多能走上多高的台阶（格子数）
    
    public override void _Ready()
    {
        _graphpoint = ResourceLoader.Load<PackedScene>("res://ScenePacked/Component/GraphPoint.tscn");
        _usedTiles = GetUsedCells();
        
        BuildGraph();
    }

    private void BuildGraph()
    {
        AddGraphPoints();

        // 如果不应该显示调试图
        if (!ShowDebugGraph)
        {
            ConnectPoints(); // 连接点
        }
    }

    private PointInfo GetPointInfoAtPosition(Vector2 position)
    {
        var newInfoPoint = new PointInfo(-10000, position); // 创建一个新的PointInfo，位置为给定位置
        newInfoPoint.IsPositionPoint = true; // 标记为位置点
        var tile = LocalToMap(position); // 获取瓷砖位置		

        // 如果在下面找到一个瓷砖
        if (GetCellSourceId(new Vector2I(tile.X, tile.Y + 1)) != CELL_IS_EMPTY)
        {
            // 如果左边有瓷砖
            if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y)) != CELL_IS_EMPTY)
            {
                newInfoPoint.IsLeftWall = true; // 标记为左墙
            }

            // 如果右边有瓷砖
            if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y)) != CELL_IS_EMPTY)
            {
                newInfoPoint.IsRightWall = true; // 标记为右墙
            }

            // 如果左下方没有瓷砖
            if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y + 1)) != CELL_IS_EMPTY)
            {
                newInfoPoint.IsLeftEdge = true; // 标记为左边缘
            }

            // 如果右下方没有瓷砖
            if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y + 1)) != CELL_IS_EMPTY)
            {
                newInfoPoint.IsRightEdge = true; // 标记为右边缘
            }
        }

        return newInfoPoint;
    }

    private Stack<PointInfo> ReversePathStack(
        Stack<PointInfo> pathStack)
    {
        Stack<PointInfo> pathStackReversed =
            new Stack<PointInfo>();
        // 反转路径栈。这就像将一桶石头从一个桶倒入另一个桶。
        // 顶部的石头将位于另一个桶的底部。
        while (pathStack.Count != 0)
        {
            pathStackReversed.Push(pathStack.Pop());
        }

        return pathStackReversed;
    }

    public Stack<PointInfo> GetPlanform2DPath(Vector2 startPos, Vector2 endPos)
    {
        Stack<PointInfo> pathStack = new Stack<PointInfo>();
        // 查找起始位置和结束位置之间的路径
        var idPath = _astarGraph.GetIdPath(_astarGraph.GetClosestPoint(startPos), _astarGraph.GetClosestPoint(endPos));

        if (idPath.Count() <= 0)
        {
            return pathStack;
        } // 如果路径已到达目标，返回空路径栈

        var startPoint = GetPointInfoAtPosition(startPos); // 创建起始位置的点
        var endPoint = GetPointInfoAtPosition(endPos); // 创建结束位置的点
        var numPointsInPath = idPath.Count(); // 获取astar路径中的点数

        // 遍历路径中的所有点
        for (int i = 0; i < numPointsInPath; ++i)
        {
            var currPoint = GetInfoPointByPointId(idPath[i]); // 获取idPath中的当前点

            // 如果路径中只有一个点
            if (numPointsInPath == 1)
            {
                continue; // 跳过astar路径中的点，结束点将在最后作为唯一路径点添加。
            }

            // 如果是astar路径中的第一个点
            if (i == 0 && numPointsInPath >= 2)
            {
                // 获取astar路径中的下一个第二个路径点
                var secondPathPoint = GetInfoPointByPointId(idPath[i + 1]);

                // 如果起始点距离第二个路径点比当前点更近
                if (startPoint.Position.DistanceTo(secondPathPoint.Position) <
                    currPoint.Position.DistanceTo(secondPathPoint.Position))
                {
                    pathStack.Push(startPoint); // 将起始点添加到路径中
                    continue; // 跳过添加当前点，转到路径中的下一个点
                }
            }
            // 如果是路径中的最后一个点 
            else if (i == numPointsInPath - 1 && numPointsInPath >= 2)
            {
                // 获取astar路径列表中的倒数第二个点
                var penultimatePoint = GetInfoPointByPointId(idPath[i - 1]);

                // 如果endPoint比astar路径中的最后一个点更近
                if (endPoint.Position.DistanceTo(penultimatePoint.Position) <
                    currPoint.Position.DistanceTo(penultimatePoint.Position))
                {
                    continue; // 跳过将最后一个点添加到路径栈
                }
                // 如果最后一个点更近
                else
                {
                    pathStack.Push(currPoint); // 将当前点添加到路径栈
                    break; // 跳出for循环
                }
            }

            pathStack.Push(currPoint); // 添加当前点			
        }

        pathStack.Push(endPoint); // 将结束点添加到路径中		
        return ReversePathStack(pathStack); // 返回反转的路径栈		
    }

    private PointInfo GetInfoPointByPointId(long pointId)
    {
        // 查找并返回在_pointInfoList中第一个与给定pointId相同的点
        return _pointInfoList.Where(p => p.PointID == pointId).FirstOrDefault();
    }

    private void DrawDebugLine(Vector2 to, Vector2 from, Color color)
    {
        // 如果调试图应该可见
        if (ShowDebugGraph)
        {
            DrawLine(to, from, color); // 用给定颜色在点之间绘制一条线
        }
    }

    private void AddGraphPoints()
    {
        // 遍历瓷砖地图中所有已使用的瓷砖
        foreach (var tile in _usedTiles)
        {
            AddLeftEdgePoint(tile);
            AddRightEdgePoint(tile);
            AddLeftWallPoint(tile);
            AddRightWallPoint(tile);
            AddFallPoint(tile);
        }
    }

    public long TileAlreadyExistInGraph(Vector2I tile)
    {
        var localPos = MapToLocal(tile); // 将位置映射到屏幕坐标

        // 如果图中包含点
        if (_astarGraph.GetPointCount() > 0)
        {
            var pointId = _astarGraph.GetClosestPoint(localPos); // 查找图中最近的点

            // 如果这些点具有相同的局部坐标
            if (_astarGraph.GetPointPosition(pointId) == localPos)
            {
                return pointId; // 返回点id，瓷砖已经存在
            }
        }

        // 如果未找到节点，返回-1
        return -1;
    }

    private void AddVisualPoint(Vector2I tile, Color? color = null, float scale = 1.0f)
    {
        // 如果不应显示图形，则返回
        if (!ShowDebugGraph)
        {
            return;
        }

        // 实例化一个新的可视点
        Sprite2D visualPoint = _graphpoint.Instantiate() as Sprite2D;

        // 如果传入了自定义颜色
        if (color != null)
        {
            visualPoint.Modulate = (Color)color; // 将可视点的颜色更改为自定义颜色
        }

        // 如果传入了自定义缩放，并且在有效范围内
        if (scale != 1.0f && scale > 0.1f)
        {
            visualPoint.Scale = new Vector2(scale, scale); // 更新可视点缩放
        }

        visualPoint.Position = MapToLocal(tile); // 将可视点的位置映射到局部坐标
        AddChild(visualPoint); // 将可视点作为子节点添加到场景中
    }

    private PointInfo GetPointInfo(Vector2I tile)
    {
        // 遍历点信息列表
        foreach (var pointInfo in _pointInfoList)
        {
            // 如果瓷砖已添加到点列表中
            if (pointInfo.Position == MapToLocal(tile))
            {
                return pointInfo; // 返回PointInfo
            }
        }

        return null; // 如果未找到瓷砖，返回null
    }

    public override void _Draw()
    {
        // 如果调试图应该可见
        if (ShowDebugGraph)
        {
            ConnectPoints(); // 连接点并绘制图形及其连接
        }
    }

    #region 连接图形点

    private void ConnectPoints()
    {
        // 遍历点信息列表中的所有点
        foreach (var p1 in _pointInfoList)
        {
            ConnectHorizontalPoints(p1); // 连接图中的水平点
            ConnectJumpPoints(p1); // 连接图中的跳跃点
            ConnectFallPoint(p1); // 连接图中的下落点
            ConnectStepPoints(p1); // 新增：连接步上点，允许角色直接“踩”上相邻高台
        }
    }

    /* 新增：步上连接处理 */
    private void ConnectStepPoints(PointInfo p1)
    {
        // 计算 p1 实际行走平台的瓷砖坐标（记得节点位置是 tileAbove）
        Vector2I floor1 = LocalToMap(p1.Position) + new Vector2I(0, 1);

        foreach (var p2 in _pointInfoList)
        {
            if (p1.PointID == p2.PointID)
                continue;

            // 同理，计算 p2 对应的平台瓷砖位置
            Vector2I floor2 = LocalToMap(p2.Position) + new Vector2I(0, 1);

            // 横向差值
            int dx = floor2.X - floor1.X;
            // 计算相对高度差：如果 p2 平台在 p1 平台之上，则 dy > 0
            int dy = floor1.Y - floor2.Y;

            // 如果横向相差正好 1 格，
            // 且平台 p2 位于 p1 之上（dy > 0）但高度差不超过 StepHeight，
            // 则可以建立“步上”连接
            if (Math.Abs(dx) == 1 && dy > 0 && dy <= StepHeight)
            {
                _astarGraph.ConnectPoints(p1.PointID, p2.PointID);
                // 用紫色绘制步上连接（调试用）
                DrawDebugLine(p1.Position, p2.Position, new Color(1, 0, 1, 1));
            }
        }
    }
    
    private void ConnectFallPoint(PointInfo p1)
    {
        if (p1.IsLeftEdge || p1.IsRightEdge)
        {
            var tilePos = LocalToMap(p1.Position);
            // FindFallPoint期望精确的瓷砖坐标。图中的点位于地面上方一块瓷砖：y-1			
            // 因此我们将y位置调整为：Y += 1
            tilePos.Y += 1;

            Vector2I? fallPoint = FindFallPoint(tilePos);
            if (fallPoint != null)
            {
                var pointInfo = GetPointInfo((Vector2I)fallPoint);
                Vector2 p2Map = LocalToMap(p1.Position);
                Vector2 p1Map = LocalToMap(pointInfo.Position);

                if (p1Map.DistanceTo(p2Map) <= JumpHeight)
                {
                    _astarGraph.ConnectPoints(p1.PointID, pointInfo.PointID); // 连接点
                    DrawDebugLine(p1.Position, pointInfo.Position,
                        new Color(0, 1, 0, 1)); // 在点之间绘制绿色线
                }
                else
                {
                    _astarGraph.ConnectPoints(p1.PointID, pointInfo.PointID,
                        bidirectional: false); // 只允许边 -> fallTile方向
                    DrawDebugLine(p1.Position, pointInfo.Position,
                        new Color(1, 1, 0, 1)); // 在点之间绘制黄色线									
                }
            }
        }
    }

    private void ConnectJumpPoints(PointInfo p1)
    {
        foreach (var p2 in _pointInfoList)
        {
            ConnectHorizontalPlatformJumps(p1, p2);
            ConnectDiagonalJumpRightEdgeToLeftEdge(p1, p2);
            ConnectDiagonalJumpLeftEdgeToRightEdge(p1, p2);
            ConnectDiagonalJumpRightEdgeToRightEdge(p1, p2);
        }
    }

    /// <summary>
    /// 返回 true 表示在起跳到落点之间的矩形空间里发现方块，
    /// 角色上跳高度不足，飞行途中会撞顶。
    /// p1 必须位于较低的平台（Y 值更大）
    /// p2 必须位于较高的平台（Y 值更小），且 needRise = p1.Y - p2.Y > 0
    /// </summary>
    private bool JumpPathBlockedByRoof(Vector2I p1, Vector2I p2)
    {
        // 需要上升的格数
        int needRise = p1.Y - p2.Y;          // 注意 Godot Y 轴向下，故为正值
        if (needRise <= 0) return false;     // 水平或下落，不用检查

        // 扫描起落两点之间（包含落点列，为保险多扫 1 列）
        int xStart = Mathf.Min(p1.X, p2.X);
        int xEnd   = Mathf.Max(p1.X, p2.X);

        // 在每一列，检查从 p1 上方 1 格到 p1 上方 needRise 格 —— 角色头顶的全部空间
        for (int x = xStart; x <= xEnd; ++x)
        {
            for (int y = p1.Y - 1; y >= p1.Y - needRise; --y)
            {
                if (GetCellSourceId(new Vector2I(x, y)) != CELL_IS_EMPTY)
                    return true;      // 有砖 => 会撞顶
            }
        }
        return false;                 // 没砖 => 通过
    }
    
    // 从 tile 开始，一直向上数，直到遇到非空砖或达到 maxCheck。
    // 返回能腾空的高度（单位：格子）
    private int GetVerticalClearance(Vector2I tile, int maxCheck = 20)
    {
        int clearance = 0;
        for (int y = tile.Y - 1; y >= tile.Y - maxCheck; --y)
        {
            if (GetCellSourceId(new Vector2I(tile.X, y)) != CELL_IS_EMPTY)
                break;
            clearance++;
        }
        return clearance;
    }
    
    private void ConnectDiagonalJumpRightEdgeToLeftEdge(PointInfo p1, PointInfo p2)
    {
        if (!p1.IsRightEdge || !p2.IsLeftEdge) return;

        Vector2I p1Map = LocalToMap(p1.Position);
        Vector2I p2Map = LocalToMap(p2.Position);
        if (!(p2.Position.X > p1.Position.X && p2.Position.Y > p1.Position.Y)) return;
        if (p1Map.DistanceTo(p2Map) > JumpDistance) return;
        
        _astarGraph.ConnectPoints(p1.PointID, p2.PointID);
        DrawDebugLine(p1.Position, p2.Position, new Color(0,1,0));
    }

    private void ConnectDiagonalJumpLeftEdgeToRightEdge(PointInfo p1, PointInfo p2)
    {
        if (p1.IsLeftEdge)
        {
            Vector2I p1Map = LocalToMap(p1.Position);
            Vector2I p2Map = LocalToMap(p2.Position);
            if (p2.IsRightEdge // 如果p2瓷砖是右边缘
                && p2.Position.X < p1.Position.X // 并且p2瓷砖在p1瓷砖的左边
                && p2.Position.Y > p1.Position.Y // 并且p2瓷砖在p1瓷砖的下方
                && p2Map.DistanceTo(p1Map) <
                JumpDistance) // 并且p2和p1地图位置之间的距离在跳跃范围内
            {
                _astarGraph.ConnectPoints(p1.PointID, p2.PointID); // 连接点
                DrawDebugLine(p1.Position, p2.Position, new Color(0, 1, 0, 1)); // 在点之间绘制绿色线
            }
        }
    }

    private void ConnectHorizontalPlatformJumps(PointInfo p1, PointInfo p2)
    {
        if (p1.PointID == p2.PointID)
        {
            return;
        } // 如果点相同，则返回

        // 如果点在同一高度，且p1是右边缘，p2是左边缘	
        if (p2.Position.Y == p1.Position.Y && p1.IsRightEdge && p2.IsLeftEdge)
        {
            // 如果p2位置在p1位置的右边
            if (p2.Position.X > p1.Position.X)
            {
                Vector2 p2Map = LocalToMap(p2.Position); // 获取p2瓷砖位置
                Vector2 p1Map = LocalToMap(p1.Position); // 获取p1瓷砖位置				

                // 如果p2和p1地图位置之间的距离在跳跃范围内
                if (p2Map.DistanceTo(p1Map) < JumpDistance + 1)
                {
                    _astarGraph.ConnectPoints(p1.PointID, p2.PointID); // 连接点
                    DrawDebugLine(p1.Position, p2.Position,
                        new Color(0, 1, 0, 1)); // 在点之间绘制绿色线
                }
            }
        }
    }
    
    // 右边缘(或右墙) → 右边缘(或右墙) 允许“斜向或垂直向下”跳
    private void ConnectDiagonalJumpRightEdgeToRightEdge(PointInfo p1, PointInfo p2)
    {
        if (!p1.IsRightEdge && !p1.IsRightWall)  // 起点必须在平台右侧
            return;

        if (!p2.IsRightEdge && !p2.IsRightWall)  // 落点也必须在右侧
            return;

        Vector2I p1Map = LocalToMap(p1.Position);
        Vector2I p2Map = LocalToMap(p2.Position);

        // 只能往下跳
        if (p2Map.Y <= p1Map.Y)      
            return;

        // X 方向可以相同或略向左/右，这里你只关心幅度
        if (Mathf.Abs(p2Map.X - p1Map.X) > JumpDistance)
            return;

        // 斜距/曼哈顿距任选，下面用欧几里得距
        if (p1Map.DistanceTo(p2Map) > JumpDistance)
            return;

        // 头顶与落脚空间检测 ---------------------------------
        // ① 落脚瓦片上方必须为空（避免撞头）
        if (GetCellSourceId(new Vector2I(p2Map.X, p2Map.Y - 1)) != CELL_IS_EMPTY)
            return;
        // ② 落脚点脚下必须是地面
        if (GetCellSourceId(new Vector2I(p2Map.X, p2Map.Y + 1)) == CELL_IS_EMPTY)
            return;
        
        // 建边 & 画线
        _astarGraph.ConnectPoints(p1.PointID, p2.PointID);
        DrawDebugLine(p1.Position, p2.Position, new Color(0, 1, 0, 1)); // 绿色
    }
    
    private void ConnectStepHorizontal(PointInfo p1)
    {
        foreach (var p2 in _pointInfoList)
        {
            if (p1.PointID == p2.PointID) continue;

            // p2 必须在同一方向（水平方向向右）——
            if (p2.Position.X <= p1.Position.X) continue;

            // 计算“格子高度差”
            int yDiffTiles = LocalToMap(p1.Position).Y - LocalToMap(p2.Position).Y;

            // 只有高度差绝对值 ≤ StepHeight 才算台阶
            if (Math.Abs(yDiffTiles) == 0 ||
                Math.Abs(yDiffTiles) > StepHeight) continue;

            // ---- 路径检测：确保行走区间不被方块挡住 ----
            if (StepConnectionBlocked(p1, p2, yDiffTiles)) continue;

            _astarGraph.ConnectPoints(p1.PointID, p2.PointID);
            DrawDebugLine(p1.Position, p2.Position, new Color(0.25f, 0.9f, 0.25f));
        }
    }
    
    private bool StepConnectionBlocked(PointInfo p1, PointInfo p2, int yDiffTiles)
    {
        // 起点/终点的 tile 坐标
        Vector2I start = LocalToMap(p1.Position);
        Vector2I end   = LocalToMap(p2.Position);

        // 只需把两点间每一列的“角色头顶与脚下”检查一下
        for (int x = start.X; x <= end.X; ++x)
        {
            // 角色脚下必须有地面
            if (GetCellSourceId(new Vector2I(x,       start.Y + 1)) == CELL_IS_EMPTY)
                return true;

            // 角色身体所在行必须为空
            if (GetCellSourceId(new Vector2I(x, start.Y)) != CELL_IS_EMPTY)
                return true;

            // 如果在爬台阶，角色脑袋可能高 1 格，也要保证为空
            int extraCheckY = start.Y - Math.Sign(yDiffTiles);
            if (GetCellSourceId(new Vector2I(x, extraCheckY)) != CELL_IS_EMPTY)
                return true;
        }
        return false;
    }

    private void ConnectFlatHorizontal(PointInfo p1)
    {
        PointInfo closest = null;

        // 遍历点信息列表
        foreach (var p2 in _pointInfoList)
        {
            if (p1.PointID == p2.PointID)
            {
                continue;
            } // 如果点相同，转到下一个点

            // 如果点是右边缘或右墙，并且高度（Y位置）相同，且p2位置在p1点的右边
            if ((p2.IsRightEdge || p2.IsRightWall || p2.IsFallTile) && p2.Position.Y == p1.Position.Y &&
                p2.Position.X > p1.Position.X)
            {
                // 如果最近的点尚未初始化
                if (closest == null)
                {
                    closest = new PointInfo(p2.PointID, p2.Position); // 初始化为p2点
                }

                // 如果p2点比当前最近的点更近
                if (p2.Position.X < closest.Position.X)
                {
                    closest.Position = p2.Position; // 更新最近点位置
                    closest.PointID = p2.PointID; // 更新pointId
                }
            }
        }

        // 如果找到最近的点
        if (closest != null)
        {
            // 如果无法建立水平连接
            if (!HorizontalConnectionCannotBeMade((Vector2I)p1.Position, (Vector2I)closest.Position))
            {
                _astarGraph.ConnectPoints(p1.PointID, closest.PointID); // 连接点
                DrawDebugLine(p1.Position, closest.Position,
                    new Color(0, 1, 0, 1)); // 在点之间绘制绿色线
            }
        }
    }
    
    private void ConnectHorizontalPoints(PointInfo p1)
    {
        // ① 先做原来的“纯同一高度”水平连线 -----------------
        if (p1.IsLeftEdge || p1.IsLeftWall || p1.IsFallTile)
            ConnectFlatHorizontal(p1);

        // ② 再做“允许微小高度差”的台阶连线 ------------------
        if (p1.IsLeftEdge || p1.IsLeftWall || p1.IsFallTile)
            ConnectStepHorizontal(p1);
        
        /*if (p1.IsLeftEdge || p1.IsLeftWall || p1.IsFallTile)
        {
            PointInfo closest = null;

            // 遍历点信息列表
            foreach (var p2 in _pointInfoList)
            {
                if (p1.PointID == p2.PointID)
                {
                    continue;
                } // 如果点相同，转到下一个点

                // 如果点是右边缘或右墙，并且高度（Y位置）相同，且p2位置在p1点的右边
                if ((p2.IsRightEdge || p2.IsRightWall || p2.IsFallTile) && p2.Position.Y == p1.Position.Y &&
                    p2.Position.X > p1.Position.X)
                {
                    // 如果最近的点尚未初始化
                    if (closest == null)
                    {
                        closest = new PointInfo(p2.PointID, p2.Position); // 初始化为p2点
                    }

                    // 如果p2点比当前最近的点更近
                    if (p2.Position.X < closest.Position.X)
                    {
                        closest.Position = p2.Position; // 更新最近点位置
                        closest.PointID = p2.PointID; // 更新pointId
                    }
                }
            }

            // 如果找到最近的点
            if (closest != null)
            {
                // 如果无法建立水平连接
                if (!HorizontalConnectionCannotBeMade((Vector2I)p1.Position, (Vector2I)closest.Position))
                {
                    _astarGraph.ConnectPoints(p1.PointID, closest.PointID); // 连接点
                    DrawDebugLine(p1.Position, closest.Position,
                        new Color(0, 1, 0, 1)); // 在点之间绘制绿色线
                }
            }
        }*/
    }

    private bool HorizontalConnectionCannotBeMade(Vector2I p1, Vector2I p2)
    {
        // 将位置转换为瓷砖坐标
        Vector2I startScan = LocalToMap(p1);
        Vector2I endScan = LocalToMap(p2);

        // 遍历点之间的所有瓷砖
        for (int i = startScan.X; i < endScan.X; ++i)
        {
            if (GetCellSourceId(new Vector2I(i, startScan.Y)) != CELL_IS_EMPTY // 如果单元格不为空（墙）
                || GetCellSourceId(new Vector2I(i, startScan.Y + 1)) ==
                CELL_IS_EMPTY) // 或者下面的单元格为空（边缘瓷砖）
            {
                return true; // 返回true，连接无法建立
            }
        }

        return false;
    }

    #endregion

    #region 瓷砖下落点

    private Vector2I? GetStartScanTileForFallPoint(Vector2I tile)
    {
        var tileAbove = new Vector2I(tile.X, tile.Y - 1);
        var point = GetPointInfo(tileAbove);

        // 如果点在点信息列表中不存在
        if (point == null)
        {
            return null;
        } // 返回null

        var tileScan = Vector2I.Zero;

        // 如果点是左边缘
        if (point.IsLeftEdge)
        {
            tileScan = new Vector2I(tile.X - 1,
                tile.Y - 1); // 设置开始位置为向左扫描一块瓷砖
            return tileScan; // 返回瓷砖扫描位置
        }
        // 如果点是右边缘
        else if (point.IsRightEdge)
        {
            tileScan = new Vector2I(tile.X + 1,
                tile.Y - 1); // 设置开始位置为向右扫描一块瓷砖
            return tileScan; // 返回瓷砖扫描位置
        }

        return null; // 返回null			
    }

    private Vector2I? FindFallPoint(Vector2 tile)
    {
        var scan = GetStartScanTileForFallPoint((Vector2I)tile); // 获取开始扫描瓷砖位置
        if (scan == null)
        {
            return null;
        } // 如果未找到，返回

        var tileScan = (Vector2I)scan; // 将可空的Vector2I?类型转换为Vector2I
        Vector2I? fallTile = null; // 初始化fallTile为null

        // 循环，开始寻找坚固的瓷砖
        for (int i = 0; i < MAX_TILE_FALL_SCAN_DEPTH; ++i)
        {
            // 如果下面的瓷砖单元格是坚固的
            if (GetCellSourceId(new Vector2I(tileScan.X, tileScan.Y + 1)) != CELL_IS_EMPTY)
            {
                fallTile = tileScan; // 找到下落瓷砖
                break; // 跳出for循环
            }

            // 如果未找到坚固的瓷砖，扫描当前瓷砖下面的下一个瓷砖
            tileScan.Y++;
        }

        return fallTile; // 返回下落瓷砖结果
    }

    private void AddFallPoint(Vector2I tile)
    {
        Vector2I? fallTile = FindFallPoint(tile); // 查找下落瓷砖点
        if (fallTile == null)
        {
            return;
        } // 如果未找到下落瓷砖，则返回

        var fallTileLocal = (Vector2I)MapToLocal((Vector2I)fallTile); // 获取下落瓷砖的局部坐标

        long existingPointId = TileAlreadyExistInGraph((Vector2I)fallTile); // 检查点是否已经添加

        // 如果瓷砖在图中尚不存在
        if (existingPointId == -1)
        {
            long pointId = _astarGraph.GetAvailablePointId(); // 获取下一个可用的点id
            var pointInfo =
                new PointInfo(pointId, fallTileLocal); // 创建点信息，并传入pointId和瓷砖
            pointInfo.IsFallTile = true; // 标记该瓷砖为下落瓷砖
            _pointInfoList.Add(pointInfo); // 将瓷砖添加到点信息列表
            _astarGraph.AddPoint(pointId, fallTileLocal); // 将点添加到Astar图中，使用局部坐标
            AddVisualPoint((Vector2I)fallTile, new Color(1, 0.35f, 0.1f, 1),
                scale: 0.35f); // 将点可视化添加到地图中（如果ShowDebugGraph = true）
        }
        else
        {
            _pointInfoList.Single(x => x.PointID == existingPointId).IsFallTile =
                true; // 标记为下落点			
            var updateInfo = _pointInfoList.Find(x => x.PointID == existingPointId); // 查找现有点信息
            updateInfo.IsFallTile = true; // 标记为下落瓷砖				
            AddVisualPoint((Vector2I)fallTile, new Color("#ef7d57"),
                scale: 0.30f); // 将点可视化添加到地图中（如果ShowDebugGraph = true）
        }
    }

    #endregion

    #region 瓷砖边缘和墙壁图形点

    private void AddLeftEdgePoint(Vector2I tile)
    {
        // 如果上方有瓷砖，则不是边缘
        if (TileAboveExist(tile))
        {
            return; // 返回
        }

        // 如果左边的瓷砖（X - 1）为空
        if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y)) == CELL_IS_EMPTY)
        {
            var tileAbove =
                new Vector2I(tile.X, tile.Y - 1); // 图形点跟随的瓷砖是在地面上方一块瓷砖			

            long existingPointId = TileAlreadyExistInGraph(tileAbove); // 检查点是否已经添加		
            // 如果点尚未添加
            if (existingPointId == -1)
            {
                long pointId = _astarGraph.GetAvailablePointId(); // 获取下一个可用的点id
                var pointInfo =
                    new PointInfo(pointId,
                        (Vector2I)MapToLocal(tileAbove)); // 创建一个新的点信息，并传入pointId
                pointInfo.IsLeftEdge = true; // 标记该瓷砖为左边缘
                _pointInfoList.Add(pointInfo); // 将瓷砖添加到点信息列表
                _astarGraph.AddPoint(pointId,
                    (Vector2I)MapToLocal(tileAbove)); // 将点添加到Astar图中，使用局部坐标
                AddVisualPoint(tileAbove); // 将点可视化添加到地图中（如果ShowDebugGraph = true）				
            }
            else
            {
                _pointInfoList.Single(x => x.PointID == existingPointId).IsLeftEdge =
                    true; // 标记为左边缘
                AddVisualPoint(tileAbove, new Color("#73eff7")); // 将点可视化添加到地图中					
            }
        }
    }

    private void AddRightEdgePoint(Vector2I tile)
    {
        // 如果上方有瓷砖，则不是边缘
        if (TileAboveExist(tile))
        {
            return; // 返回
        }

        // 如果右边的瓷砖（X + 1）为空
        if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y)) == CELL_IS_EMPTY)
        {
            var tileAbove =
                new Vector2I(tile.X, tile.Y - 1); // 图形点跟随的瓷砖是在地面上方一块瓷砖			

            long existingPointId = TileAlreadyExistInGraph(tileAbove); // 检查点是否已经添加		
            // 如果点尚未添加
            if (existingPointId == -1)
            {
                long pointId = _astarGraph.GetAvailablePointId(); // 获取下一个可用的点id
                var pointInfo =
                    new PointInfo(pointId,
                        (Vector2I)MapToLocal(tileAbove)); // 创建一个新的点信息，并传入pointId
                pointInfo.IsRightEdge = true; // 标记该瓷砖为右边缘
                _pointInfoList.Add(pointInfo); // 将瓷砖添加到点信息列表
                _astarGraph.AddPoint(pointId,
                    (Vector2I)MapToLocal(tileAbove)); // 将点添加到Astar图中，使用局部坐标
                AddVisualPoint(tileAbove,
                    new Color("#94b0c2")); // 将点可视化添加到地图中（如果ShowDebugGraph = true）			
            }
            else
            {
                _pointInfoList.Single(x => x.PointID == existingPointId).IsRightEdge =
                    true; // 标记为右边缘
                AddVisualPoint(tileAbove,
                    new Color("#ffcd75")); // 将点可视化添加到地图中（如果ShowDebugGraph = true）			
            }
        }
    }

    private void AddLeftWallPoint(Vector2I tile)
    {
        // 如果上方有瓷砖，则不是边缘
        if (TileAboveExist(tile))
        {
            return; // 返回
        }

        // 如果左上方的瓷砖（X - 1, Y -1）不为空
        if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y - 1)) != CELL_IS_EMPTY)
        {
            var tileAbove =
                new Vector2I(tile.X, tile.Y - 1); // 图形点跟随的瓷砖是在地面上方一块瓷砖			

            long existingPointId = TileAlreadyExistInGraph(tileAbove); // 检查点是否已经添加		
            // 如果点尚未添加
            if (existingPointId == -1)
            {
                long pointId = _astarGraph.GetAvailablePointId(); // 获取下一个可用的点id
                var pointInfo =
                    new PointInfo(pointId,
                        (Vector2I)MapToLocal(tileAbove)); // 创建一个新的点信息，并传入pointId
                pointInfo.IsLeftWall = true; // 标记该瓷砖为左墙	
                _pointInfoList.Add(pointInfo); // 将瓷砖添加到点信息列表
                _astarGraph.AddPoint(pointId,
                    (Vector2I)MapToLocal(tileAbove)); // 将点添加到Astar图中，使用局部坐标
                AddVisualPoint(tileAbove,
                    new Color(0, 0, 0, 1)); // 将黑色点添加到地图中（如果ShowDebugGraph = true）
            }
            else
            {
                _pointInfoList.Single(x => x.PointID == existingPointId).IsLeftWall =
                    true; // 标记为左墙
                AddVisualPoint(tileAbove, new Color(0, 0, 1, 1),
                    0.45f); // 在地图上相同位置添加一个小蓝点
            }
        }
    }

    private void AddRightWallPoint(Vector2I tile)
    {
        // 如果上方有瓷砖，则不是边缘
        if (TileAboveExist(tile))
        {
            return; // 返回
        }

        // 如果右上方的瓷砖（X + 1, Y -1）不为空
        if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y - 1)) != CELL_IS_EMPTY)
        {
            var tileAbove =
                new Vector2I(tile.X, tile.Y - 1); // 图形点跟随的瓷砖是在地面上方一块瓷砖			

            long existingPointId = TileAlreadyExistInGraph(tileAbove); // 检查点是否已经添加		
            // 如果点尚未添加
            if (existingPointId == -1)
            {
                long pointId = _astarGraph.GetAvailablePointId(); // 获取下一个可用的点id
                var pointInfo =
                    new PointInfo(pointId,
                        (Vector2I)MapToLocal(tileAbove)); // 创建一个新的点信息，并传入pointId
                pointInfo.IsRightWall = true; // 标记该瓷砖为右墙	
                _pointInfoList.Add(pointInfo); // 将瓷砖添加到点信息列表
                _astarGraph.AddPoint(pointId,
                    (Vector2I)MapToLocal(tileAbove)); // 将点添加到Astar图中，使用局部坐标
                AddVisualPoint(tileAbove,
                    new Color(0, 0, 0, 1)); // 将黑色点添加到地图中（如果ShowDebugGraph = true）
            }
            else
            {
                _pointInfoList.Single(x => x.PointID == existingPointId).IsLeftEdge =
                    true; // 标记为左边缘
                AddVisualPoint(tileAbove, new Color("566c86"),
                    0.65f); // 在地图上相同位置添加一个小紫点
            }
        }
    }

    private bool TileAboveExist(Vector2I tile)
    {
        // 如果上方没有瓷砖（Y - 1）
        if (GetCellSourceId(new Vector2I(tile.X, tile.Y - 1)) == CELL_IS_EMPTY)
        {
            return false; // 如果为空，则返回false
        }

        return true;
    }

    #endregion
}