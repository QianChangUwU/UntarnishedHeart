using OmenTools.OmenService;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Internal;

internal static class PluginCommand
{
    public const string MAIN_COMMAND = "/uth";

    public static void Init()
    {
        var manager = CommandManager.Instance();

        manager.MainCommand = new(MAIN_COMMAND, new(OnMainCommand) { HelpMessage = "打开主界面" });

        manager.AddSubCommand("autointeract", new(OnAutoInteract) { HelpMessage    = "尝试交互最近可交互物体" });
        manager.AddSubCommand("newround",     new(OnEnqueueNewRound) { HelpMessage = "若当前正在运行某一预设, 则立刻退出副本并开始新一轮执行" });

        manager.AddSubCommand("start",         new(OnStart) { HelpMessage         = "start <预设名称> [轮数] → 启动指定预设, 省略轮数时使用配置值, -1 表示无限" });
        manager.AddSubCommand("startroute",    new(OnStartRoute) { HelpMessage    = "startroute <路线名称> → 启动指定路线" });
        manager.AddSubCommand("stop",          new(OnStop) { HelpMessage          = "停止当前执行" });
        manager.AddSubCommand("stopafterduty", new(OnStopAfterDuty) { HelpMessage = "在当前或下一次副本完成并退出后停止执行" });
        manager.AddSubCommand("cancelstop",    new(OnCancelStop) { HelpMessage    = "取消完成副本后结束的请求" });
        manager.AddSubCommand("status",        new(OnStatus) { HelpMessage        = "查看当前执行状态" });
        manager.AddSubCommand("list",          new(OnList) { HelpMessage          = "列出全部预设与路线" });
    }

    public static void Uninit()
    {
        var manager = CommandManager.Instance();

        manager.RemoveSubCommand("autointeract");
        manager.RemoveSubCommand("newround");
        manager.RemoveSubCommand("start");
        manager.RemoveSubCommand("startroute");
        manager.RemoveSubCommand("stop");
        manager.RemoveSubCommand("stopafterduty");
        manager.RemoveSubCommand("cancelstop");
        manager.RemoveSubCommand("status");
        manager.RemoveSubCommand("list");
        manager.MainCommand = null;
    }

    private static void OnMainCommand
    (
        string command,
        string arguments
    )
    {
        if (WindowManager.Instance().Get<MainWindow>() is { } main)
            main.IsOpen ^= true;
    }

    private static void OnAutoInteract
    (
        string command,
        string args
    )
    {
        if (ExecutionManager.TryRequestNearestInteract())
            return;

        NotifyHelper.Instance().Chat("当前没有正在运行的执行器，无法执行最近交互");
    }

    private static void OnEnqueueNewRound
    (
        string command,
        string args
    ) =>
        ExecutionManager.ManualEnqueueNewRound();

    private static void OnStart
    (
        string command,
        string args
    )
    {
        var (name, rounds) = SplitTrailingRounds(args);

        if (string.IsNullOrWhiteSpace(name))
        {
            NotifyHelper.Instance().ChatError("用法: /uth start <预设名称> [轮数]");
            return;
        }

        if (rounds != null && ExecutionControlService.CountPresetByName(name) == 0)
        {
            name   = args.Trim();
            rounds = null;
        }

        ReportStartResult(ExecutionControlService.StartPreset(name, rounds), "预设", name);
    }

    private static void OnStartRoute
    (
        string command,
        string args
    )
    {
        var name = args.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            NotifyHelper.Instance().ChatError("用法: /uth startroute <路线名称>");
            return;
        }

        ReportStartResult(ExecutionControlService.StartRoute(name), "路线", name);
    }

    private static void OnStop
    (
        string command,
        string args
    )
    {
        if (ExecutionControlService.Stop())
        {
            NotifyHelper.Instance().Chat("已停止当前执行");
            return;
        }

        NotifyHelper.Instance().Chat("当前没有正在运行的预设或路线");
    }

    private static void OnStopAfterDuty
    (
        string command,
        string args
    )
    {
        if (ExecutionManager.RequestStopAfterDutyCompletion())
        {
            NotifyHelper.Instance().Chat("已请求完成副本后结束，等待当前或下一次副本完成并退出");
            return;
        }

        NotifyHelper.Instance().Chat("当前没有正在运行的预设或路线");
    }

    private static void OnCancelStop
    (
        string command,
        string args
    )
    {
        if (ExecutionManager.CancelStopAfterDutyCompletionRequest())
        {
            NotifyHelper.Instance().Chat("已取消完成副本后结束的请求");
            return;
        }

        NotifyHelper.Instance().Chat("当前没有待取消的结束请求");
    }

    private static void OnStatus
    (
        string command,
        string args
    ) =>
        NotifyHelper.Instance().Chat(ExecutionControlService.BuildStatusText());

    private static void OnList
    (
        string command,
        string args
    )
    {
        foreach (var line in ExecutionControlService.BuildListLines())
            NotifyHelper.Instance().Chat(line);
    }

    private static (string Name, int? Rounds) SplitTrailingRounds
    (
        string args
    )
    {
        var trimmed   = args.Trim();
        var lastSpace = trimmed.LastIndexOf(' ');

        if (lastSpace <= 0)
            return (trimmed, null);

        return int.TryParse(trimmed[(lastSpace + 1)..], out var rounds) ?
                   (trimmed[..lastSpace].Trim(), rounds) :
                   (trimmed, null);
    }

    private static void ReportStartResult
    (
        string? error,
        string  kind,
        string  name
    )
    {
        if (error != null)
        {
            NotifyHelper.Instance().ChatError(error);
            return;
        }

        NotifyHelper.Instance().Chat($"已开始执行{kind}: {name}");
    }
}
