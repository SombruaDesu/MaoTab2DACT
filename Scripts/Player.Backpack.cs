/*
 * @Author: MaoT
 * @Description: 玩家对象，背包部分
 */

#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts;

public partial class Player
{
    public int Width;
    public int Height;

    /// <summary>
    /// 堆放起始点
    /// </summary>
    [Export] private Node2D _startTile;

    // 网格: 如果该格子为空, 为 null; 否则指向所在 ItemInstance
    private ItemInstance?[,] _cells;

    // 列高度缓存, columnHeights[x] = 该列已堆叠的高度(下一件能放置的 y)
    private int[] _nextY;

    // 方便枚举实例
    private HashSet<ItemInstance> _items = new();

    public void InitBackpack(int width, int height)
    {
        Width  = width;
        Height = height;
        _cells = new ItemInstance?[width + 1, height + 1]; // +1 让索引 1..n 有效
        _nextY = new int[width + 1];                       // 0 列不用
        for (int x = 1; x <= width; ++x) _nextY[x] = 1;
    }

    // 判断 item(带旋转) 是否能以 “左下角 = (x, y?)” 放下，
// 如果能，返回 true 并把找到的最小合法 y 存入 out y
    public bool CanPlace(ItemDefinition def, bool rotated, int x, out int y)
    {
        y = -1;

        var sz = rotated ? new Size(def.Size.H, def.Size.W) : def.Size;

        // 水平合法性
        if (x < 1 || x + sz.W - 1 > Width) return false;

        // 从地面往上找第一个满足条件的 y
        // 最大起点 = Height - sz.H + 1
        for (int candY = 1; candY <= Height - sz.H + 1; ++candY)
        {
            // 1) 该矩形内必须全空
            bool collided = false;
            for (int dx = 0; dx < sz.W && !collided; ++dx)
                for (int dy = 0; dy < sz.H && !collided; ++dy)
                    if (_cells[x + dx, candY + dy] != null)
                        collided = true;

            if (collided) continue;

            // 2) 底边必须全部有支撑
            bool supported = true;
            for (int dx = 0; dx < sz.W && supported; ++dx)
            {
                if (candY == 1) continue;              // 地面直接支撑
                if (_cells[x + dx, candY - 1] == null) // 悬空
                    supported = false;
            }

            if (!supported) continue;

            // 找到了
            y = candY;
            return true;
        }

        // 没找到
        return false;
    }

    public async Task<bool> Place(ItemInstance item, bool rotated, int x)
    {
        var def = item.Def;
        if (!CanPlace(def, rotated, x, out int y)) return false;
        await item.PickedUp(_startTile, x, y, rotated);

        var sz = item.GetSize();

        // 写入网格
        for (int dx = 0; dx < sz.W; ++dx)
            for (int dy = 0; dy < sz.H; ++dy)
                _cells[x + dx, y + dy] = item;

        // 更新 _nextY
        for (int dx = 0; dx < sz.W; ++dx)
        {
            int col = x + dx;
            _nextY[col] = Mathf.Max(_nextY[col], y + sz.H);
        }
        
        _items.Add(item);
        return true;
    }

    /// <summary>
    /// 尝试自动放入一件物品。成功返回 true，并输出实例；失败返回 false。
    /// 策略：优先贴地，若同一高度则越靠左越好。
    /// </summary>
    public Task<bool> AutoPlace(ItemInstance item)
    {
        var  def     = item.Def;
        bool found   = false;
        int  bestX   = -1, bestY = int.MaxValue;
        bool bestRot = false;

        foreach (bool rot in def.CanRotate ? new[] { false, true } : new[] { false })
        {
            var sz = rot ? new Size(def.Size.H, def.Size.W) : def.Size;
            for (int x = 1; x <= Width - sz.W + 1; ++x)
            {
                if (CanPlace(def, rot, x, out int y))
                {
                    if (y < bestY || (y == bestY && x < bestX))
                    {
                        bestY   = y;
                        bestX   = x;
                        bestRot = rot;
                        found   = true;
                    }
                }
            }
        }

        if (!found) return Task.FromResult(false); // 哪儿都放不下

        // ④ 真正落位
        return Place(item, bestRot, bestX);
    }
    
    // 列高重新计算（仅针对一列）
    private void RecalcColumn(int col)
    {
        int ny = 1;
        for (int y = Height; y >= 1; --y)
        {
            if (_cells[col, y] != null)
            {
                ny = y + 1;
                break;
            }
        }
        _nextY[col] = ny;
    }

// 该物品是否仍有底部支撑
    private bool IsSupported(ItemInstance item)
    {
        var sz      = item.GetSize();     // (W,H)
        int yBottom = item.Y;

        for (int dx = 0; dx < sz.W; ++dx)
        {
            int cx = item.X + dx;

            // 地面直接算支撑
            if (yBottom == 1) continue;

            // 底下一格必须被其他物品占用
            if (_cells[cx, yBottom - 1] == null)
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// 把指定物品从背包里移除；如果导致其他物品失去支撑，则继续连锁移除
    /// </summary>
    public void RemoveCascade(ItemInstance start,Vector2 power)
    {
        // 用队列进行 BFS，避免递归深度
        Queue<ItemInstance> q = new();
        q.Enqueue(start);

        while (q.Count > 0)
        {
            ItemInstance it = q.Dequeue();
            if (!_items.Contains(it))            // 可能已经被前面移掉
                continue;

            // 1) 从网格里清空
            var sz = it.GetSize();
            for (int dx = 0; dx < sz.W; ++dx)
                for (int dy = 0; dy < sz.H; ++dy)
                {
                    int cx = it.X + dx;
                    int cy = it.Y + dy;
                    if (_cells[cx, cy] == it)
                        _cells[cx, cy] = null;
                }

            // 2) 从集合删除
            _items.Remove(it);

            // 3) 更新列高缓存
            for (int dx = 0; dx < sz.W; ++dx)
                RecalcColumn(it.X + dx);

            // 4) 把失去支撑的物品加入队列
            List<ItemInstance> toDrop = new();
            foreach (var other in _items)
                if (!IsSupported(other))
                    toDrop.Add(other);

            foreach (var d in toDrop)
                q.Enqueue(d);

            // 5) 这里可以触发表现层，例如丢到地上
            it.DroppedFromBackpack(power);   // 你自己的函数，可选
            power = new Vector2(power.X * 0.75f, 20);
        }
    }
}