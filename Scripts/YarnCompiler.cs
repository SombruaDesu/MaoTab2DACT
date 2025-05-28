/*
 * @Author: MaoT
 * @Description: Yarn 编译器
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Google.Protobuf;
using Yarn;
using Yarn.Compiler;
using YarnSpinnerGodot;

namespace MaoTab.Scripts;

public class YarnCompiler
{
    public YarnProject Compile(string script)
    {
        YarnProject project = new(){ isRuntimeCompiler = true };
        // ReSharper disable once CollectionNeverUpdated.Local
        List<FunctionInfo> newFunctionList = [];
        var library = new Library();
        byte[] compiledBytes;
        // ReSharper disable once RedundantAssignment
        CompilationResult compilationResult = new();
        
        var job = CompilationJob.CreateFromString("A",script);
        job.Library = library;
        compilationResult = Compiler.Compile(job);

        #region -- 异常处理 --
        var errors = compilationResult.Diagnostics.Where(d =>
            d.Severity == Diagnostic.DiagnosticSeverity.Error);

        var diagnostics = errors as Diagnostic[] ?? errors.ToArray();
        if (diagnostics.Length > 0)
        {
            var errorGroups = diagnostics.GroupBy(e => e.FileName);
            foreach (var errorGroup in errorGroups)
            {
                var errorMessages = errorGroup.Select(e => e.ToString());

                foreach (var message in errorMessages) 
                {
                    GD.PushError($"Error compiling: {message}");
                }
            }
            return null;
        }
        
        if (compilationResult.Program == null)
        {
            GD.PushError(
                "public error: Failed to compile: resulting program was null, but compiler did not report errors.");
            return null;
        }
        #endregion == 异常处理 ==
        
        // 存储 _所有_ 声明 - 包括这个 .yarnproject 文件中的声明，以及 .yarn 文件中的声明。
        // 在这里，我们过滤掉任何以我们的 Yarn 公共前缀开头的声明。这些是合成变量，
        // 是编译结果产生的，并不是用户声明的。
        var newDeclarations = new List<Declaration>() //localDeclarations
            .Concat(compilationResult.Declarations)
            .Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
            .Where(decl => !(decl.Type is FunctionType))
            .Select(decl =>
            {
                SerializedDeclaration existingDeclaration = null;
                // 尽量重用已有的声明，以避免对.tres文件进行过多更改
                foreach (var existing in project.SerializedDeclarations)
                {
                    if (existing.name == decl.Name)
                    {
                        existingDeclaration = existing;
                        break;
                    }
                }

                var serialized = existingDeclaration ?? new SerializedDeclaration();
                serialized.SetDeclaration(decl);
                return serialized;
            }).ToArray();
        project.SerializedDeclarations = newDeclarations;
        
        CreateYarnInternalLocalizationAssets(project, compilationResult);

        using (var memoryStream = new MemoryStream())
        using (var outputStream = new CodedOutputStream(memoryStream))
        {
            // 将编译后的程序序列化到内存中
            compilationResult.Program.WriteTo(outputStream);
            outputStream.Flush();

            compiledBytes = memoryStream.ToArray();
        }
        
        project.ListOfFunctions = newFunctionList.ToArray();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        project.CompiledYarnProgramBase64 = compiledBytes == null ? "" : Convert.ToBase64String(compiledBytes);
        /*var saveErr = ResourceSaver.Save(project, project.ImportPath,
            ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
        if (saveErr != Error.Ok)
        {
            GD.PushError($"Failed to save updated {nameof(YarnProject)}: {saveErr}");
        }*/
        
        return project;
    }
    
    // ReSharper disable HeuristicUnreachableCode
    private static void CreateYarnInternalLocalizationAssets(YarnProject project,
        CompilationResult compilationResult)
    {
        // 我们需要创建一个默认本地化吗？
        // 如果我们在 languagesToSourceAssets 中配置的语言之一是默认语言，这个变量将被设置为 false。
        var shouldAddDefaultLocalization = true;
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (project.JSONProject.Localisation == null)
        {
            project.JSONProject.Localisation =
                new Dictionary<string, Project.LocalizationInfo>();
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (shouldAddDefaultLocalization)
        {
            // 我们没有为默认语言添加本地化。
            // 现在为它创建一个。
            var stringTableEntries = GetStringTableEntries(project, compilationResult);

            var developmentLocalization = project.baseLocalization ?? new Localization();
            developmentLocalization.Clear();
            developmentLocalization.ResourceName = $"Default ({project.defaultLanguage})";
            developmentLocalization.LocaleCode = project.defaultLanguage;

            // 将这些新行添加到开发本地化的资产中
            foreach (var entry in stringTableEntries)
            {
                developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry);
            }

            project.baseLocalization = developmentLocalization;

            // 由于这是默认语言，因此也填充行元数据。
            project.LineMetadata ??= new LineMetadata();
            project.LineMetadata.Clear();
            project.LineMetadata.AddMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
        }
    }
    
    private static IEnumerable<LineMetadataTableEntry> LineMetadataTableEntriesFromCompilationResult(
        CompilationResult result)
    {
        if (result.StringTable == null)
        {
            return [];
        }

        return result.StringTable.Select(x => new LineMetadataTableEntry
        {
            ID = x.Key,
            File = ProjectSettings.LocalizePath(x.Value.fileName),
            Node = x.Value.nodeName,
            LineNumber = x.Value.lineNumber.ToString(),
            Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray()
        }).Where(x => x.Metadata.Length > 0);
    }
    
    private static IEnumerable<StringTableEntry> GetStringTableEntries(YarnProject project,
        CompilationResult result)
    {
        if (result.StringTable == null)
        {
            return [];
        }

        return result.StringTable.Select(x => new StringTableEntry
        {
            ID = x.Key,
            Language = project.defaultLanguage,
            Text = x.Value.text,
            File = ProjectSettings.LocalizePath(x.Value.fileName),
            Node = x.Value.nodeName,
            LineNumber = x.Value.lineNumber.ToString(),
            Lock = x.Value.text == null ? "" : GetHashString(x.Value.text, 8),
            Comment = GenerateCommentWithLineMetadata(x.Value.metadata)
        });
    }
    

    /// <summary>
    /// 返回包含 <paramref name="inputString"/> 的 SHA-256 哈希的字节数组。
    /// </summary>
    /// <param name="inputString">要生成哈希值的字符串。</param>
    /// <returns><paramref name="inputString"/> 的哈希。</returns>
    private static byte[] GetHash(string inputString)
    {
        using HashAlgorithm algorithm = SHA256.Create();
        return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
    }
    
    /// <summary>
    /// 返回一个字符串，包含<paramref name="inputString"/>的SHA-256哈希的十六进制表示。
    /// </summary>
    /// <param name="inputString">要生成哈希的字符串。</param>
    /// <param name="limitCharacters">要返回的字符串的长度。返回的字符串最多为<paramref name="limitCharacters"/>个字符。如果设置为-1，则会返回整个字符串。</param>
    /// <returns>哈希的字符串版本。</returns>
    private static string GetHashString(string inputString, int limitCharacters = -1)
    {
        var sb = new StringBuilder();
        foreach (byte b in GetHash(inputString))
        {
            sb.Append(b.ToString("x2"));
        }

        if (limitCharacters == -1)
        {
            // Return the entire string
            return sb.ToString();
        }
        else
        {
            // Return a substring (or the entire string, if
            // limitCharacters is longer than the string)
            return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
        }
    }

    /// <summary>
    /// 生成包含行元数据的字符串。此字符串旨在
    /// 用于字符串表 CSV 的“注释”列。因此，
    /// 如果存在行 ID（它也是行元数据的一部分），
    /// 则会忽略它。
    /// </summary>
    /// <param name="metadata">给定行的元数据。</param>
    /// <returns>一个以“行元数据: ”为前缀的字符串，后跟每个
    /// 元数据部分，之间用空格分隔。如果不存在元数据或
    /// 仅行 ID 是元数据的一部分，则返回一个空字符串
    /// 代替。</returns>
    private static string GenerateCommentWithLineMetadata(string[] metadata)
    {
        var cleanedMetadata = RemoveLineIDFromMetadata(metadata);
        var enumerable = cleanedMetadata as string[] ?? cleanedMetadata.ToArray();
        return enumerable.Length == 0 ? string.Empty : $"Line metadata: {string.Join(" ", enumerable)}";
    }
    
    /// <summary>
    /// 从行元数据数组中删除任何行ID条目。
    /// 如果设置了行ID条目，行元数据将始终包含一个行ID条目。
    /// 例如，如果一行包含“#line:1eaf1e55”，则其行元数据
    /// 将始终包含“line:1eaf1e55”条目。
    /// </summary>
    /// <param name="metadata">包含行元数据的数组。</param>
    /// <returns>移除任何行ID条目的IEnumerable。</returns>
    // ReSharper disable once InconsistentNaming
    private static IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
    {
        return metadata.Where(x => !x.StartsWith("line:"));
    }
}