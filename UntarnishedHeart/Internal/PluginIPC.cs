using Dalamud.Plugin.Ipc;
using OmenTools;
using UntarnishedHeart.Execution.Managers;

namespace UntarnishedHeart.Internal;

internal static class PluginIPC
{
    public const string PREFIX = "UntarnishedHeart";

    private static ICallGateProvider<bool>?                           isRunning;
    private static ICallGateProvider<string>?                         mode;
    private static ICallGateProvider<string>?                         name;
    private static ICallGateProvider<int>?                            round;
    private static ICallGateProvider<int>?                            maxRound;
    private static ICallGateProvider<int>?                            step;
    private static ICallGateProvider<int>?                            stepCount;
    private static ICallGateProvider<string>?                         message;
    private static ICallGateProvider<bool>?                           stopAfterDuty;
    private static ICallGateProvider<int>?                            presetCount;
    private static ICallGateProvider<int, string>?                    presetName;
    private static ICallGateProvider<int>?                            routeCount;
    private static ICallGateProvider<int, string>?                    routeName;
    private static ICallGateProvider<string, int, string>?            startPreset;
    private static ICallGateProvider<string, string>?                 startRoute;
    private static ICallGateProvider<bool>?                           stop;
    private static ICallGateProvider<bool>?                           requestStopAfterDuty;
    private static ICallGateProvider<bool>?                           cancelStopAfterDuty;
    private static ICallGateProvider<string>?                         status;

    public static void Init()
    {
        var pi = DService.Instance().PI;

        isRunning = pi.GetIpcProvider<bool>($"{PREFIX}.IsRunning");
        mode      = pi.GetIpcProvider<string>($"{PREFIX}.GetMode");
        name      = pi.GetIpcProvider<string>($"{PREFIX}.GetName");
        round     = pi.GetIpcProvider<int>($"{PREFIX}.GetRound");
        maxRound  = pi.GetIpcProvider<int>($"{PREFIX}.GetMaxRound");
        step      = pi.GetIpcProvider<int>($"{PREFIX}.GetStep");
        stepCount = pi.GetIpcProvider<int>($"{PREFIX}.GetStepCount");
        message   = pi.GetIpcProvider<string>($"{PREFIX}.GetMessage");
        stopAfterDuty = pi.GetIpcProvider<bool>($"{PREFIX}.IsStopAfterDutyCompletionRequested");

        presetCount = pi.GetIpcProvider<int>($"{PREFIX}.GetPresetCount");
        presetName  = pi.GetIpcProvider<int, string>($"{PREFIX}.GetPresetNameAt");
        routeCount  = pi.GetIpcProvider<int>($"{PREFIX}.GetRouteCount");
        routeName   = pi.GetIpcProvider<int, string>($"{PREFIX}.GetRouteNameAt");

        startPreset           = pi.GetIpcProvider<string, int, string>($"{PREFIX}.StartPreset");
        startRoute            = pi.GetIpcProvider<string, string>($"{PREFIX}.StartRoute");
        stop                  = pi.GetIpcProvider<bool>($"{PREFIX}.Stop");
        requestStopAfterDuty  = pi.GetIpcProvider<bool>($"{PREFIX}.RequestStopAfterDutyCompletion");
        cancelStopAfterDuty   = pi.GetIpcProvider<bool>($"{PREFIX}.CancelStopAfterDutyCompletion");
        status                = pi.GetIpcProvider<string>($"{PREFIX}.GetStatus");

        isRunning.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().IsRunning);
        mode.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().Mode);
        name.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().Name);
        round.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().Round);
        maxRound.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().MaxRound);
        step.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().Step);
        stepCount.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().StepCount);
        message.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().Message);
        stopAfterDuty.RegisterFunc(() => ExecutionControlService.CaptureSnapshot().StopAfterDutyCompletionRequested);

        presetCount.RegisterFunc(() => PluginConfig.Instance().Presets.Count);
        presetName.RegisterFunc(index => GetNameAt(PluginConfig.Instance().Presets, index, static preset => preset.Name));
        routeCount.RegisterFunc(() => PluginConfig.Instance().Routes.Count);
        routeName.RegisterFunc(index => GetNameAt(PluginConfig.Instance().Routes, index, static route => route.Name));

        startPreset.RegisterFunc((presetNameArg, rounds) => ExecutionControlService.StartPreset(presetNameArg, rounds) ?? string.Empty);
        startRoute.RegisterFunc(routeNameArg => ExecutionControlService.StartRoute(routeNameArg) ?? string.Empty);
        stop.RegisterFunc(ExecutionControlService.Stop);
        requestStopAfterDuty.RegisterFunc(ExecutionManager.RequestStopAfterDutyCompletion);
        cancelStopAfterDuty.RegisterFunc(ExecutionManager.CancelStopAfterDutyCompletionRequest);
        status.RegisterFunc(ExecutionControlService.BuildStatusText);
    }
    public static void Uninit()
    {
        isRunning?.UnregisterFunc();
        mode?.UnregisterFunc();
        name?.UnregisterFunc();
        round?.UnregisterFunc();
        maxRound?.UnregisterFunc();
        step?.UnregisterFunc();
        stepCount?.UnregisterFunc();
        message?.UnregisterFunc();
        stopAfterDuty?.UnregisterFunc();

        presetCount?.UnregisterFunc();
        presetName?.UnregisterFunc();
        routeCount?.UnregisterFunc();
        routeName?.UnregisterFunc();

        startPreset?.UnregisterFunc();
        startRoute?.UnregisterFunc();
        stop?.UnregisterFunc();
        requestStopAfterDuty?.UnregisterFunc();
        cancelStopAfterDuty?.UnregisterFunc();
        status?.UnregisterFunc();

        isRunning            = null;
        mode                 = null;
        name                 = null;
        round                = null;
        maxRound             = null;
        step                 = null;
        stepCount            = null;
        message              = null;
        stopAfterDuty        = null;
        presetCount          = null;
        presetName           = null;
        routeCount           = null;
        routeName            = null;
        startPreset          = null;
        startRoute           = null;
        stop                 = null;
        requestStopAfterDuty = null;
        cancelStopAfterDuty  = null;
        status               = null;
    }

    private static string GetNameAt<T>
    (
        List<T>         items,
        int             index,
        Func<T, string> nameSelector
    ) =>
        index >= 0 && index < items.Count ?
            nameSelector(items[index]) :
            string.Empty;
}
