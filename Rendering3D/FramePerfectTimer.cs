using System.Diagnostics;

namespace SimpleWPFGame.Rendering3D;

public class FramePerfectTimer
{
    private static FramePerfectTimer? _instance;
    public static FramePerfectTimer Instance => _instance ??= new FramePerfectTimer();

    public double FixedTimestep { get; set; } = 1.0 / 60.0;
    public double Accumulator { get; private set; }
    public int FixedFrame { get; private set; }
    public double Alpha { get; private set; }
    public double ElapsedTime { get; private set; }
    public double DeltaTime { get; private set; }
    public bool FrameAdvanced { get; private set; }
    public int FrameSkipCount { get; private set; }

    private readonly Stopwatch _stopwatch = new();
    private long _lastTimestamp;
    private double _maxFrameTime;
    private int _maxFrameSkips;

    private readonly List<FrameEvent> _pendingEvents = new();
    private readonly List<FrameEvent> _activeEvents = new();

    public FramePerfectTimer(double fixedFps = 60, double maxFrameTime = 0.25, int maxFrameSkips = 5)
    {
        FixedTimestep = 1.0 / fixedFps;
        _maxFrameTime = maxFrameTime;
        _maxFrameSkips = maxFrameSkips;
    }

    public void Start()
    {
        _stopwatch.Start();
        _lastTimestamp = _stopwatch.ElapsedMilliseconds;
    }

    public void Stop() => _stopwatch.Stop();

    public void Reset()
    {
        Accumulator = 0;
        FixedFrame = 0;
        Alpha = 0;
        ElapsedTime = 0;
        DeltaTime = 0;
        FrameAdvanced = false;
        FrameSkipCount = 0;
        _pendingEvents.Clear();
        _activeEvents.Clear();
    }

    public bool Tick()
    {
        FrameAdvanced = false;
        FrameSkipCount = 0;

        long now = _stopwatch.ElapsedMilliseconds;
        DeltaTime = (now - _lastTimestamp) / 1000.0;
        _lastTimestamp = now;

        DeltaTime = Math.Min(DeltaTime, _maxFrameTime);
        ElapsedTime += DeltaTime;
        Accumulator += DeltaTime;

        while (Accumulator >= FixedTimestep && FrameSkipCount < _maxFrameSkips)
        {
            AdvanceFixedStep();
            Accumulator -= FixedTimestep;
            FrameSkipCount++;
            FrameAdvanced = true;
        }

        Alpha = Accumulator / FixedTimestep;
        ProcessEvents();
        return FrameAdvanced;
    }

    private void AdvanceFixedStep()
    {
        FixedFrame++;

        for (int i = _activeEvents.Count - 1; i >= 0; i--)
        {
            var evt = _activeEvents[i];
            evt.ElapsedFrames++;

            if (evt.ElapsedFrames >= evt.TotalFrames)
            {
                evt.OnComplete?.Invoke();
                _activeEvents.RemoveAt(i);
            }
            else
            {
                evt.OnTick?.Invoke(evt.ElapsedFrames, evt.TotalFrames);
                _activeEvents[i] = evt;
            }
        }
    }

    private void ProcessEvents()
    {
        for (int i = _pendingEvents.Count - 1; i >= 0; i--)
        {
            var evt = _pendingEvents[i];
            if (ElapsedTime >= evt.TriggerTime)
            {
                _activeEvents.Add(evt);
                _pendingEvents.RemoveAt(i);
                evt.OnStart?.Invoke();
            }
        }
    }

    public void ScheduleEvent(double delaySeconds, Action<int, int>? onTick, Action? onComplete, Action? onStart = null, int durationFrames = 1)
    {
        _pendingEvents.Add(new FrameEvent
        {
            TriggerTime = ElapsedTime + delaySeconds,
            TotalFrames = durationFrames,
            OnTick = onTick,
            OnComplete = onComplete,
            OnStart = onStart
        });
    }

    public void CancelAllEvents()
    {
        _pendingEvents.Clear();
        _activeEvents.Clear();
    }

    public double FrameProgress(int startFrame, int endFrame)
    {
        if (FixedFrame < startFrame) return 0;
        if (FixedFrame > endFrame) return 1;
        return (double)(FixedFrame - startFrame) / Math.Max(1, endFrame - startFrame);
    }

    public bool IsInFrameWindow(int startFrame, int endFrame)
    {
        return FixedFrame >= startFrame && FixedFrame <= endFrame;
    }

    public bool IsFrameExact(int frame)
    {
        return FixedFrame == frame;
    }

    public double Interpolate(double from, double to, double alpha)
    {
        return from + (to - from) * alpha;
    }
}

public struct FrameEvent
{
    public double TriggerTime;
    public int TotalFrames;
    public int ElapsedFrames;
    public Action<int, int>? OnTick;
    public Action? OnComplete;
    public Action? OnStart;
}

public class CombatTimingSync
{
    private static CombatTimingSync? _instance;
    public static CombatTimingSync Instance => _instance ??= new CombatTimingSync();

    private readonly Dictionary<int, EntityTimingData> _entityTimings = new();
    private readonly FramePerfectTimer _timer = FramePerfectTimer.Instance;

    private CombatTimingSync() { }

    public void Register(int entityId)
    {
        if (!_entityTimings.ContainsKey(entityId))
            _entityTimings[entityId] = new EntityTimingData();
    }

    public void Unregister(int entityId)
    {
        _entityTimings.Remove(entityId);
    }

    public void BeginAttack(int entityId, int startupFrames, int activeFrames, int recoveryFrames)
    {
        if (!_entityTimings.TryGetValue(entityId, out var data)) return;

        data.AttackStartFrame = _timer.FixedFrame;
        data.StartupEnd = data.AttackStartFrame + startupFrames;
        data.ActiveEnd = data.StartupEnd + activeFrames;
        data.RecoveryEnd = data.ActiveEnd + recoveryFrames;
        data.IsAttacking = true;
        data.HitRegistered = false;
    }

    public void RegisterHit(int attackerId, int targetId)
    {
        if (_entityTimings.TryGetValue(attackerId, out var data))
        {
            data.HitRegistered = true;
            data.LastHitFrame = _timer.FixedFrame;
            data.LastHitTarget = targetId;
        }
    }

    public bool CanHit(int entityId)
    {
        if (!_entityTimings.TryGetValue(entityId, out var data)) return false;
        if (!data.IsAttacking) return false;
        return _timer.FixedFrame >= data.StartupEnd && _timer.FixedFrame <= data.ActiveEnd;
    }

    public bool IsStartup(int entityId)
    {
        if (!_entityTimings.TryGetValue(entityId, out var data)) return false;
        return data.IsAttacking && _timer.FixedFrame < data.StartupEnd;
    }

    public bool IsActive(int entityId)
    {
        if (!_entityTimings.TryGetValue(entityId, out var data)) return false;
        return data.IsAttacking && _timer.FixedFrame >= data.StartupEnd && _timer.FixedFrame <= data.ActiveEnd;
    }

    public bool IsRecovery(int entityId)
    {
        if (!_entityTimings.TryGetValue(entityId, out var data)) return false;
        return data.IsAttacking && _timer.FixedFrame > data.ActiveEnd && _timer.FixedFrame <= data.RecoveryEnd;
    }

    public double GetActiveProgress(int entityId)
    {
        if (!_entityTimings.TryGetValue(entityId, out var data)) return 0;
        if (!data.IsAttacking) return 0;
        int activeLen = data.ActiveEnd - data.StartupEnd;
        if (activeLen <= 0) return 0;
        return Math.Clamp((double)(_timer.FixedFrame - data.StartupEnd) / activeLen, 0, 1);
    }

    public EntityTimingData? GetTiming(int entityId)
    {
        return _entityTimings.TryGetValue(entityId, out var data) ? data : null;
    }

    public void Clear()
    {
        _entityTimings.Clear();
    }
}

public class EntityTimingData
{
    public int AttackStartFrame;
    public int StartupEnd;
    public int ActiveEnd;
    public int RecoveryEnd;
    public bool IsAttacking;
    public bool HitRegistered;
    public int LastHitFrame;
    public int LastHitTarget;
}
