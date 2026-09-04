namespace SimpleWPFGame.Rendering3D;

public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly int _maxSize;

    public int Count => _pool.Count;
    public int TotalCreated { get; private set; }

    public ObjectPool(Func<T> factory, Action<T>? reset = null, int initialSize = 16, int maxSize = 256)
    {
        _factory = factory;
        _reset = reset;
        _maxSize = maxSize;

        for (int i = 0; i < initialSize; i++)
        {
            _pool.Push(_factory());
            TotalCreated++;
        }
    }

    public T Get()
    {
        T item = _pool.Count > 0 ? _pool.Pop() : _factory();
        TotalCreated++;
        return item;
    }

    public void Return(T item)
    {
        _reset?.Invoke(item);
        if (_pool.Count < _maxSize)
            _pool.Push(item);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}

public class HitboxPool
{
    private static HitboxPool? _instance;
    public static HitboxPool Instance => _instance ??= new HitboxPool();

    private readonly ObjectPool<List<Hitbox3D>> _listPool;
    private readonly ObjectPool<Queue<double>> _timingPool;

    private HitboxPool()
    {
        _listPool = new ObjectPool<List<Hitbox3D>>(
            () => new List<Hitbox3D>(),
            list => list.Clear(),
            32, 128);

        _timingPool = new ObjectPool<Queue<double>>(
            () => new Queue<double>(),
            q => q.Clear(),
            16, 64);
    }

    public List<Hitbox3D> GetHitboxList() => _listPool.Get();
    public void ReturnHitboxList(List<Hitbox3D> list) => _listPool.Return(list);

    public Queue<double> GetTimingQueue() => _timingPool.Get();
    public void ReturnTimingQueue(Queue<double> q) => _timingPool.Return(q);
}

public class CombatFrameTimer
{
    public double FrameDuration { get; set; }
    public double Accumulator { get; private set; }
    public int CurrentFrame { get; private set; }
    public bool FrameAdvanced { get; private set; }

    public CombatFrameTimer(double fps = 60)
    {
        FrameDuration = 1.0 / fps;
    }

    public void Reset()
    {
        Accumulator = 0;
        CurrentFrame = 0;
        FrameAdvanced = false;
    }

    public void Advance(double deltaTime)
    {
        FrameAdvanced = false;
        Accumulator += deltaTime;

        while (Accumulator >= FrameDuration)
        {
            Accumulator -= FrameDuration;
            CurrentFrame++;
            FrameAdvanced = true;
        }
    }

    public bool IsInWindow(int startFrame, int endFrame)
    {
        return CurrentFrame >= startFrame && CurrentFrame <= endFrame;
    }

    public double GetFrameProgress(int startFrame, int endFrame)
    {
        if (CurrentFrame < startFrame) return 0;
        if (CurrentFrame > endFrame) return 1;
        return (double)(CurrentFrame - startFrame) / Math.Max(1, endFrame - startFrame);
    }
}
