/*
 * @Author: MaoT
 * @Description: 资源加载器
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace MaoTab.Scripts.System;

public static class ResourceHelper
{
    /// <summary>
    /// 检测路径是否存在
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>true 存在 | false 不存在</returns>
    private static bool PathCheck(string path)
    {
        if (ResourceLoader.Exists(path)) return true;
        
        GD.PushWarning("未能找到：" + path + "跳过加载");
        return false;
    }

    #region PackedScene
    public static Task<T> LoadPacked<T>(string path ,Node ownerNode) where T : class
    {
        if (!PathCheck(path)) return null;
        var packedScene = ResourceLoader.Load<PackedScene>(path);
        var node = packedScene?.Instantiate();
        
        if (node is T target)
        {
            ownerNode.AddChild(node);
            return Task.FromResult(target);
        }
        
        return null;
    }
    #endregion

    public static Task<T> LoadTres<T>(string path) where T : class
    {
        if (!PathCheck(path)) return null;
        
        var tres = ResourceLoader.Load<T>(path);
        if (tres is { } target)
        {
            return Task.FromResult(target);
        }
        
        return null;
    }
    
    public static List<string> ReadFolder(string path)
    {
        using var dir = DirAccess.Open(path);
        var paths = new List<string>();
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (dir.CurrentIsDir())
                {
                    
                }
                else
                {
                    paths.Add(path + fileName);
                }
                fileName = dir.GetNext();
            }
        }
        else
        {
            GD.Print($"访问{path}路径时出现错误");
        }
        
        return paths;
    }
    
    
    public static Task<CompressedTexture2D> LoadTexture(string path)
    {
        return !PathCheck(path) ? 
            Task.FromResult<CompressedTexture2D>(null) : 
            Task.FromResult(ResourceLoader.Load<CompressedTexture2D>(path));
    }

    public static Task<string> LoadText(string path)
    {
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var luaContent = file.GetAsText();
        return Task.FromResult(luaContent);
    }
}