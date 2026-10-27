using System.Text;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Execution.Managers;

internal static class ExecutionControlService
{
    public const string MODE_PRESET = "Preset";
    public const string MODE_ROUTE  = "Route";

    private const int MAX_LIST_ENTRIES = 30;

    public static ExecutionSnapshot CaptureSnapshot()
    {
        if (ExecutionManager.PresetExecutor is { IsDisposed: false, Completion.IsCompleted: false } presetExecutor)
        {
            return new
            (
                true,
                MODE_PRESET,
                presetExecutor.ExecutorPreset?.Name ?? string.Empty,
                (int)presetExecutor.CurrentRound,
                presetExecutor.MaxRound,
                -1,
                0,
                presetExecutor.RunningMessage,
                presetExecutor.IsStopAfterDutyCompletionRequested
            );
        }

        if (ExecutionManager.RouteExecutor is { IsRunning: true } routeExecutor)
        {
            return new
            (
                true,
                MODE_ROUTE,
                routeExecutor.SourceRoute.Name,
                -1,
                -1,
                routeExecutor.CurrentStepIndex + 1,
                routeExecutor.Steps.Count,
                routeExecutor.RunningMessage,
                routeExecutor.IsStopAfterDutyCompletionRequested
            );
        }

        return ExecutionSnapshot.Idle;
    }

    /// <returns>成功时返回 <see langword="null" />, 否则返回失败原因</returns>
    public static string? StartPreset
    (
        string? name,
        int?    rounds
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return "预设名称不能为空";

        var config   = PluginConfig.Instance();
        var maxRound = rounds ?? config.RunTimes;

        if (maxRound != -1 && maxRound < 1)
            return $"轮数无效: {maxRound}, 仅支持 -1 (无限) 或正整数";

        var (matchIndex, matchCount) = FindByName(config.Presets, name, static preset => preset.Name);

        switch (matchCount)
        {
            case 0:
                return $"找不到预设: {name}";
            case > 1:
                return $"存在 {matchCount} 个同名预设, 请先改为唯一名称: {name}";
        }

        var preset = config.Presets[matchIndex];
        if (!preset.IsValid)
            return $"预设无效, 请检查区域与步骤: {preset.Name}";

        ExecutionManager.DisposePresetExecutor();
        ExecutionManager.StopRouteExecutor();
        ExecutionManager.StartSimpleExecution(preset, config.CreatePresetRunOptions(maxRound, null));

        config.CurrentExecutionMode = ExecutionMode.Preset;
        config.SelectedPresetIndex  = matchIndex;
        config.Save();

        return null;
    }

    /// <returns>成功时返回 <see langword="null" />, 否则返回失败原因</returns>
    public static string? StartRoute
    (
        string? name
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return "路线名称不能为空";

        var config                   = PluginConfig.Instance();
        var (matchIndex, matchCount) = FindByName(config.Routes, name, static route => route.Name);

        switch (matchCount)
        {
            case 0:
                return $"找不到路线: {name}";
            case > 1:
                return $"存在 {matchCount} 个同名路线, 请先改为唯一名称: {name}";
        }

        var route = config.Routes[matchIndex];
        if (!route.IsValid)
            return $"路线无效, 请检查名称与步骤: {route.Name}";

        ExecutionManager.DisposePresetExecutor();
        ExecutionManager.StopRouteExecutor();
        ExecutionManager.StartRouteExecution(route);

        config.CurrentExecutionMode = ExecutionMode.Route;
        config.SelectedRouteIndex   = matchIndex;
        config.Save();

        return null;
    }

    public static bool Stop()
    {
        var stopped = false;

        if (ExecutionManager.PresetExecutor is { IsDisposed: false, Completion.IsCompleted: false })
        {
            ExecutionManager.DisposePresetExecutor();
            stopped = true;
        }

        if (ExecutionManager.RouteExecutor is { IsRunning: true })
        {
            ExecutionManager.StopRouteExecutor();
            stopped = true;
        }

        if (stopped)
            ExecutionUIHelper.CancelDutyQueueIfNeeded();

        return stopped;
    }

    public static int CountPresetByName
    (
        string name
    ) => FindByName(PluginConfig.Instance().Presets, name, static preset => preset.Name).Count;

    public static string BuildStatusText()
    {
        var snapshot = CaptureSnapshot();
        if (!snapshot.IsRunning)
            return "当前没有正在运行的预设或路线";

        var builder = new StringBuilder();
        builder.Append(snapshot.Mode == MODE_PRESET ? "预设" : "路线").Append(": ").Append(snapshot.Name);

        if (snapshot.Mode == MODE_PRESET)
        {
            builder.Append(", 轮次 ").Append(snapshot.Round);
            builder.Append(snapshot.MaxRound == -1 ? " / 无限" : $" / {snapshot.MaxRound}");
        }
        else
            builder.Append(", 步骤 ").Append(snapshot.Step).Append(" / ").Append(snapshot.StepCount);

        if (snapshot.StopAfterDutyCompletionRequested)
            builder.Append(", 已请求完成副本后结束");

        if (!string.IsNullOrWhiteSpace(snapshot.Message))
            builder.Append(", ").Append(snapshot.Message);

        return builder.ToString();
    }

    public static IEnumerable<string> BuildListLines()
    {
        var config      = PluginConfig.Instance();
        var presetCount = config.Presets.Count;
        var routeCount  = config.Routes.Count;

        yield return $"预设 {presetCount} 个, 路线 {routeCount} 个";

        for (var index = 0; index < presetCount && index < MAX_LIST_ENTRIES; index++)
            yield return $"预设 [{index}] {config.Presets[index].Name}";

        if (presetCount > MAX_LIST_ENTRIES)
            yield return $"预设列表过长, 仅显示前 {MAX_LIST_ENTRIES} 个";

        for (var index = 0; index < routeCount && index < MAX_LIST_ENTRIES; index++)
            yield return $"路线 [{index}] {config.Routes[index].Name}";

        if (routeCount > MAX_LIST_ENTRIES)
            yield return $"路线列表过长, 仅显示前 {MAX_LIST_ENTRIES} 个";
    }

    private static (int Index, int Count) FindByName<T>
    (
        List<T>         items,
        string          name,
        Func<T, string> nameSelector
    )
    {
        var matchIndex = -1;
        var matchCount = 0;

        for (var index = 0; index < items.Count; index++)
        {
            if (!string.Equals(nameSelector(items[index]), name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (matchIndex < 0)
                matchIndex = index;

            matchCount++;
        }

        return (matchIndex, matchCount);
    }
}

internal readonly record struct ExecutionSnapshot
(
    bool   IsRunning,
    string Mode,
    string Name,
    int    Round,
    int    MaxRound,
    int    Step,
    int    StepCount,
    string Message,
    bool   StopAfterDutyCompletionRequested
)
{
    public static ExecutionSnapshot Idle => new(false, string.Empty, string.Empty, -1, -1, -1, 0, string.Empty, false);
}
