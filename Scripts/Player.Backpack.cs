/*
 * @Author: MaoT
 * @Description: 玩家对象，背包部分
 */

#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    [Export] private Node2D _instanceLayer;
    
    [Export] private AnimationPlayer _backpackTopAnimationPlayer;
    [Export] private AnimationPlayer _backpackBottomAnimationPlayer;

    // 网格: 如果该格子为空, 为 null; 否则指向所在 ItemInstance
    private ItemInstance?[,] _cells;

    // 列高度缓存, columnHeights[x] = 该列已堆叠的高度(下一件能放置的 y)
    private int[] _nextY;

    // 方便枚举实例
    private HashSet<ItemInstance> _items = new();

    public ItemDefinition? GetItem(string id)
    {
        foreach (var item in _items)
        {
            if (item.Def.Id == id)
            {
                return item.Def;
            }
        }
        
        return null;
    }
    
    public int MaxUsedY => _nextY.Max() - 1; // 最大行
    public int MaxUsedX                      // 最大列
    {
        get{
            for (int x = Width; x >= 1; --x)
                if (_nextY[x] > 1) return x;
            return 0;                                 // 背包为空
        }
    }
    
    public void InitBackpack(int width, int height)
    {
        Width  = width;
        Height = height;
        _cells = new ItemInstance?[width + 1, height + 1]; // +1 让索引 1..n 有效
        _nextY = new int[width + 1];                       // 0 列不用
        for (int x = 1; x <= width; ++x) _nextY[x] = 1;
    }

    /// <summary>
    /// 将某个物品放置到目的地，完成后调用回调
    /// </summary>
    /// <param name="item">物品参数</param>
    /// <param name="position">目的地位置</param>
    /// <param name="onFinish">回调</param>
    public void PlaceItemTo(ItemDefinition item,Vector2 position,Callable onFinish)
    {
       item.Instance.PlaceTo(position,0,1f,onFinish);
       RemoveItem(item.Instance);
    }
    
    // 判断 item(带旋转) 是否能以 “左下角 = (x, y?)” 放下，
    // 如果能，返回 true 并把找到的最小合法 y 存入 out y
    public bool CanPlace(ItemDefinition def, bool rotated, int x, out int y)
    {
        y = -1;

        var sz = rotated ? new Vector2I(def.Size.Y, def.Size.X) : def.Size;

        // 水平范围
        if (x < 1 || x + sz.X - 1 > Width) return false;

        // 自底向上找可行 y
        for (int candY = 1; candY <= Height - sz.Y + 1; ++candY)
        {
            // 区域必须全空
            bool collided = false;
            for (int dx = 0; dx < sz.X && !collided; ++dx)
                for (int dy = 0; dy < sz.Y && !collided; ++dy)
                    if (_cells[x + dx, candY + dy] != null)
                        collided = true;
            if (collided) continue;

            // 至少 1 个底边格子有支撑
            bool supported = candY == 1;  // 接地直接算有支撑
            if (!supported)
            {
                for (int dx = 0; dx < sz.X && !supported; ++dx)
                    if (_cells[x + dx, candY - 1] != null) // 找到一格支撑即可
                        supported = true;
            }
            if (!supported) continue;

            // 找到了
            y = candY;
            return true;
        }
        return false;
    }

    private void RefreshBackpackAnimation()
    {
        DebugDumpBackpack();
        
        if (MaxUsedX >= 3)
        {
            _backpackBottomAnimationPlayer.Play("Bottom_2");
        }
        else
        {
            _backpackBottomAnimationPlayer.Play("Bottom_1");
        }

        switch (MaxUsedY)
        {
            case 4:
                _backpackTopAnimationPlayer.Play("Top_2");
                break;
            case >= 5:
                _backpackTopAnimationPlayer.Play("Top_3");
                break;
            default:
                _backpackTopAnimationPlayer.Play("Top_1");
                break;
        }
    }
    
    public async Task<bool> Place(ItemInstance item, bool rotated, int x)
    {
        var def = item.Def;
        if (!CanPlace(def, rotated, x, out int y)) return false;

        // 必须在调用 PickedUp 之前记录尺寸
        Vector2I sz = rotated
            ? new Vector2I(def.Size.Y, def.Size.X)
            : def.Size;

        // 再通知表现层
        await item.PickedUp(_instanceLayer, x, y, rotated);

        // 把 sz 用来写网格
        for (int dx = 0; dx < sz.X; ++dx)
            for (int dy = 0; dy < sz.Y; ++dy)
                _cells[x + dx, y + dy] = item;

        for (int dx = 0; dx < sz.X; ++dx)
            _nextY[x + dx] = Mathf.Max(_nextY[x + dx], y + sz.Y);

        _items.Add(item);
    
        RefreshBackpackAnimation();
        
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
            var sz = rot ? new Vector2I(def.Size.Y, def.Size.X) : def.Size;
            for (int x = 1; x <= Width - sz.X + 1; ++x)
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

        // 真正落位
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
        // 在地面上天然稳定
        if (item.Y == 1) return true;

        var sz = item.GetSize();   // W,H

        for (int dx = 0; dx < sz.X; ++dx)
        {
            int cx = item.X + dx;
            if (_cells[cx, item.Y - 1] != null)        // 发现一格有支撑
                return true;
        }
        return false;                                   // 整条底边都悬空
    }

    /// <summary>
    /// 移除指定物品；只把因此"完全悬空"的连锁物品拿出来重新整理。
    /// 如果整理后仍然放不下，就把它们丢到背包外（Dropped）。
    /// </summary>
    public async Task RemoveItem(ItemInstance target)
    {
        if (!_items.Contains(target))
            return;                         // 背包里没有

        // 先把目标物品从网格、集合里移除
        void EraseFromGrid(ItemInstance it)
        {
            var sz = it.GetSize();
            for (int dx = 0; dx < sz.X; ++dx)
                for (int dy = 0; dy < sz.Y; ++dy)
                    if (_cells[it.X + dx, it.Y + dy] == it)
                        _cells[it.X + dx, it.Y + dy] = null;

            for (int dx = 0; dx < sz.X; ++dx)
                RecalcColumn(it.X + dx);
        }

        EraseFromGrid(target);
        _items.Remove(target);
        // target.OnRemovedFromBackpack();     // 移除接口

        // 找到所有完全悬空的连锁物品
        List<ItemInstance>  floating = new();
        Queue<ItemInstance> q        = new();

        // 先把第一批悬空物加进去
        foreach (var it in _items)
            if (!IsSupported(it))
                q.Enqueue(it);

        while (q.Count > 0)
        {
            var it = q.Dequeue();
            if (floating.Contains(it)) continue;

            floating.Add(it);

            EraseFromGrid(it);
            _items.Remove(it);                 // 暂时移出

            // 这一列高度变化后，可能导致更多悬空
            foreach (var other in _items)
                if (!IsSupported(other) && !floating.Contains(other))
                    q.Enqueue(other);
        }

        if (floating.Count == 0) return;       // 没有需要整理的

        // 只整理 floating 里的物品
        // 大件先放
        floating.Sort((a, b) =>
        {
            var sa = a.GetSize(); var sb = b.GetSize();
            return (sb.X * sb.Y).CompareTo(sa.X * sa.Y);
        });

        foreach (var it in floating)
        {
            bool placed = await AutoPlace(it);          // 先试原朝向
            if (!placed && it.Def.CanRotate)
                placed = await AutoPlace(it);           // AutoPlace 内部会自己去看旋转

            if (placed)
                _items.Add(it);
            else
            {
                // 实在放不下就从背包里掉出来
                it.Dropped(Vector2.Zero);
            }
        }

        RefreshBackpackAnimation();
    }

    /// <summary>
    /// 把指定物品从背包里抛出；如果导致其他物品失去支撑，则继续连锁移除
    /// </summary>
    /// <param name="item">物品</param>
    /// <param name="power">抛出的力量</param>
    public void DropItem(ItemInstance item, Vector2 power)
    {
        // 用队列进行 BFS，避免递归深度
        Queue<ItemInstance> q = new();
        q.Enqueue(item);

        while (q.Count > 0)
        {
            ItemInstance it = q.Dequeue();
            if (!_items.Contains(it)) // 可能已经被前面移掉
                continue;

            // 从网格里清空
            var sz = it.GetSize();
            for (int dx = 0; dx < sz.X; ++dx)
                for (int dy = 0; dy < sz.Y; ++dy)
                {
                    int cx = it.X + dx;
                    int cy = it.Y + dy;
                    if (_cells[cx, cy] == it)
                        _cells[cx, cy] = null;
                }

            // 从集合删除
            _items.Remove(it);

            // 更新列高缓存
            for (int dx = 0; dx < sz.X; ++dx)
                RecalcColumn(it.X + dx);

            // 把失去支撑的物品加入队列
            List<ItemInstance> toDrop = new();
            foreach (var other in _items)
                if (!IsSupported(other))
                    toDrop.Add(other);

            foreach (var d in toDrop)
                q.Enqueue(d);

            // 这里可以触发表现层，例如丢到地上
            it.Dropped(power); // 你自己的函数，可选
            power = new Vector2(power.X * 0.75f, 20);
        }

        RefreshBackpackAnimation();
    }
    
    public void DebugDumpBackpack()
    {
        if (_cells == null)
        {
            GD.Print("Backpack not initialised.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Backpack {Width} × {Height}");

        // 从最高行往下打印，使“地面”出现在最下面一行
        for (int y = Height; y >= 1; --y)
        {
            for (int x = 1; x <= Width; ++x)
            {
                sb.Append(_cells[x, y] == null ? '*' : '#');
            }
            sb.AppendLine();
        }

        GD.Print(sb.ToString());
    }
}