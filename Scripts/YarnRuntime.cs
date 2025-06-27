/*
 * @Author: MaoT
 * @Description: Yarn 运行时
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Yarn;
using Yarn.Markup;
using YarnSpinnerGodot;

namespace MaoTab.Scripts;

/// <summary>
/// Yarn 运行时
/// </summary>
public partial class YarnRuntime : IActionRegistration
{
    private Dialogue _dialogue;
    private YarnProject _yarnProject;
    public string TextLanguageCode = "zh-Hans"; // System.Globalization.CultureInfo.CurrentCulture.Name;
    private LineParser _lineParser = new();

    public Task<LocalizedLine> GetLocalizedLineAsync(string lineId)
    {
        return GetLocalizedLineAsync( new Line(lineId, []));
    }
    
    public Task<LocalizedLine> GetLocalizedLineAsync(Line line)
    {
        string text;

        string sourceLineID = line.ID;

        string[] metadata = [];
        // 检查当前行是否遮蔽了另一条，如果是的话，我们将使用另一条的文本和资产
        if (_yarnProject != null)
        {
            metadata = _yarnProject.LineMetadata?.GetMetadata(line.ID) ?? [];

            var shadowLineSource = _yarnProject.LineMetadata?.GetShadowLineSource(line.ID);

            if (shadowLineSource != null)
            {
                sourceLineID = shadowLineSource;
            }
        }

        // 默认情况下，此提供程序将把“en”视为匹配“en-UK”、“en-US”等
        // 可以根据需要重新映射语言代码
        if (_yarnProject != null && TextLanguageCode.ToLower().StartsWith(_yarnProject.baseLocalization.LocaleCode.ToLower()))
        {
            text = _yarnProject.baseLocalization.GetLocalizedString(sourceLineID);
        }
        else
        {
            text = Root.Instance.Tr($"{line.ID}");
            // 回退到基本区域设置
            if (text.Equals(line.ID))
            {
                if (_yarnProject != null) text = _yarnProject.baseLocalization.GetLocalizedString(sourceLineID);
            }
        }


        if (text == null)
        {
            // 没有可用的行时异常处理
            GD.PushWarning($"Can't locate the text for the line: {line.ID}", this);
            return Task.FromResult(LocalizedLine.InvalidLine);
        }

        var parseResult = _lineParser.ParseString(LineParser.ExpandSubstitutions(text, line.Substitutions),
            TextLanguageCode);

        return Task.FromResult(new LocalizedLine
        {
            TextID = line.ID,
            Text = parseResult,
            RawText = text,
            Substitutions = line.Substitutions,
            Metadata = metadata,
        });
    }

    public void SelectedOption(int optionId)
    {
        _dialogue.SetSelectedOption(optionId);
        Continue();
    }
    
    public void PlayNode(string node)
    {
        if(IsRunning) return;
        _dialogue.SetNode(node);
        Continue();
    }
    
    private ICommandDispatcher CommandDispatcher { get; set; } = null!;
    
    private async Task OnCommandReceivedAsync(Command command)
    {
        CommandDispatchResult dispatchResult = CommandDispatcher!.DispatchCommand(command.Text, null);

        var parts = SplitCommandText(command.Text);
        var enumerable = parts as string[] ?? parts.ToArray();
        string commandName = enumerable.ElementAtOrDefault(0) ?? string.Empty;

        switch (dispatchResult.Status)
        {
            case CommandDispatchResult.StatusType.Succeeded:
                // 命令成功。等待其完成。（对于同步完成的命令，此任务将是 Task.Completed，因此此 'await' 将立即返回）
                await dispatchResult.Task;
                break;
            case CommandDispatchResult.StatusType.NoTargetFound:
                GD.PushError(
                    $"Can't call command {commandName}: failed to find a node named {enumerable.ElementAtOrDefault(1)}",
                    this);
                break;
            case CommandDispatchResult.StatusType.TargetMissingComponent:
                GD.PushError(
                    $"Can't call command {commandName}, because {enumerable.ElementAtOrDefault(1)} doesn't have the correct component");
                break;
            case CommandDispatchResult.StatusType.InvalidParameterCount:
                GD.PushError($"Can't call command {commandName}: incorrect number of parameters");
                break;
            case CommandDispatchResult.StatusType.CommandUnknown:
                // 异常处理
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    $"Internal error: Unknown command dispatch result status {dispatchResult}");
        }
    }
    
    public static IEnumerable<string> SplitCommandText(string input)
    {
        var reader = new StringReader(input.Normalize());

        int c;

        var results = new List<string>();
        var currentComponent = new StringBuilder();

        while ((c = reader.Read()) != -1)
        {
            if (char.IsWhiteSpace((char) c))
            {
                if (currentComponent.Length > 0)
                {
                    // 我们已经到达可见字符的结尾。  
                    // 将此字符序列添加到结果列表中，并准备下一次。
                    results.Add(currentComponent.ToString());
                    currentComponent.Clear();
                }
                
                // 遇到了一个空格字符，但没有任何字符在队列中。则跳过这个字符。
            }
            else if (c == '\"')
            {
               // 进入了一个引号字符串
                while (true)
                {
                    c = reader.Read();
                    if (c == -1)
                    {
                        // 哎呀，我们在解析一个带引号的字符串时结束了输入！立即转储我们当前的单词并返回。
                        results.Add(currentComponent.ToString());
                        return results;
                    }

                    if (c == '\\')
                    {
                        // 可能是一个转义字符！
                        var next = reader.Peek();
                        if (next == '\\' || next == '\"')
                        {
                           // 是的！跳过 \ 并使用它后面的字符。
                            reader.Read();
                            currentComponent.Append((char) next);
                        }
                        else
                        {
                            // 哎呀，无效的转义。添加 \ 和
                            // 它后面的内容。
                            currentComponent.Append((char) c);
                        }
                    }
                    else if (c == '\"')
                    {
                        // 字符串的结尾！
                        break;
                    }
                    else
                    {
                        // 任何其他字符。将其添加到缓冲区。
                        currentComponent.Append((char) c);
                    }
                }

                results.Add(currentComponent.ToString());
                currentComponent.Clear();
            }
            else
            {
                currentComponent.Append((char) c);
            }
        }

        if (currentComponent.Length > 0)
        {
            results.Add(currentComponent.ToString());
        }

        return results;
    }
    
    private InMemoryVariableStorage  _variableStorage;

    public void Continue()
    {
        _dialogue.Continue();
    }

    public Action<LocalizedLine>    OnLineArrival;
    public Action<DialogueOption[]> OnOptionsArrival;
    
    /// <summary>
    /// 捕获模式开关，开启时自动跳过行，并将跳过的行收集起来，直到此标签关闭时停止自动跳过
    /// </summary>
    /// <para>收集的行存放于captureLine</para>
    private bool captureMode;
    private List<LocalizedLine> captureLines = new();

    /// <summary>
    /// 启动捕获模式
    /// </summary>
    public void StartCaptureMode()
    {
        captureLines.Clear();
        captureMode = true;
    }

    
    /// <summary>
    /// 停止捕获模式
    /// </summary>
    public void StopCaptureMode()
    {
        captureMode = false;
    }
    
    public List<LocalizedLine> GetCaptureLines()
    {
        return captureLines;
    }

    public bool IsRunning;
    
    public void Init(YarnProject yarnProject)
    {
        _yarnProject = yarnProject;
        _variableStorage =  new InMemoryVariableStorage();
        _dialogue = new Dialogue(_variableStorage);

        var actions = new Actions(this,_dialogue.Library);
        CommandDispatcher = actions;
        actions.RegisterActions();
        
        // 执行-指令
        _dialogue.CommandHandler += command =>
        {
            IsRunning = true;
            _         = OnCommandReceivedAsync(command);
        };
        
        // 执行-行
        _dialogue.LineHandler += async void (line) =>
        {
            IsRunning = true;
            var localisedLine = await GetLocalizedLineAsync(line);
            if (captureMode)
            {
                captureLines.Add(localisedLine);
                Continue();
                return;
            }
            
            OnLineArrival?.Invoke(localisedLine);
            
            // UIRoot.DialoguePanel.Refresh(localisedLine.TextWithoutCharacterName.Text,null);
            // Continue();
        };
        
        // 执行-选项
        _dialogue.OptionsHandler += async void (data) =>
        {
            IsRunning = true;
            DialogueOption[] localisedOptions = new DialogueOption[data.Options.Length];
            for (int i = 0; i < data.Options.Length; i++)
            {
                var opt = data.Options[i];
                LocalizedLine localizedLine = await GetLocalizedLineAsync(opt.Line);

                if (localizedLine == LocalizedLine.InvalidLine)
                {
                    GD.PushError($"无法获取本地化行 {opt.Line.ID} (option {i + 1})!");
                }

                localisedOptions[i] = new DialogueOption
                {
                    DialogueOptionID = opt.ID,
                    IsAvailable = opt.IsAvailable,
                    Line = localizedLine,
                    TextID = opt.Line.ID,
                };
            }

            OnOptionsArrival?.Invoke(localisedOptions);
            
            foreach (var option in localisedOptions)
            {
                GD.Print("->" + option.Line.TextWithoutCharacterName.Text);
            }
        };
        
        BindCommand();
        
        _dialogue.SetProgram(yarnProject.Program);
    }
    
    /// <inheritdoc />
    public void AddCommandHandler(string commandName, Delegate handler) =>
        CommandDispatcher.AddCommandHandler(commandName, handler);

    /// <inheritdoc />
    public void AddCommandHandler(string commandName, MethodInfo method) =>
        CommandDispatcher.AddCommandHandler(commandName, method);

    /// <inheritdoc />
    public void RemoveCommandHandler(string commandName) => CommandDispatcher.RemoveCommandHandler(commandName);


    /// <inheritdoc />
    public void AddFunction(string name, Delegate implementation) =>
        CommandDispatcher.AddFunction(name, implementation);

    /// <inheritdoc />
    public void RemoveFunction(string name) => CommandDispatcher.RemoveFunction(name);
}