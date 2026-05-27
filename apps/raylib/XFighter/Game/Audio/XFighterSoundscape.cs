using Novolis.Audio.Core;
using Novolis.Audio.Effects;
using Novolis.Audio.Playback;

namespace XFighter.Game.Audio;

/// <summary>Engine loop + fire-and-forget one-shots via Novolis.Audio.Playback.</summary>
internal sealed class XFighterSoundscape : IDisposable
{
    private readonly IAudioPlayback _playback = new NaudioPcmPlayback();
    private readonly RadioHissEffect _radioHiss = new(0.006f);
    private CancellationTokenSource? _engineCts;
    private Task? _engineTask;
    private float _throttle;
    private bool _enabled = true;
    private bool _disposed;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void Start()
    {
        if (!_enabled || _engineCts is not null)
            return;

        try
        {
            _engineCts = new CancellationTokenSource();
            _engineTask = Task.Run(() => EngineLoopAsync(_engineCts.Token));
        }
        catch
        {
            _enabled = false;
            StopEngine();
        }
    }

    public void UpdateEngine(float throttle01) =>
        _throttle = Math.Clamp(throttle01, 0f, 1f);

    public void PlayLaser() => PlayOneShot(ProceduralSfx.LaserPew());

    public void PlayExplosion() => PlayOneShot(ProceduralSfx.ExplosionBoom());

    public void PlayRadio() => PlayOneShot(_radioHiss.Apply(ProceduralSfx.RadioSquelch()));

    public void PlayShieldHit() => PlayOneShot(ProceduralSfx.ShieldHit());

    public void PlayEnemyBolt() => PlayOneShot(ProceduralSfx.EnemyBolt());

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        StopEngine();
        if (_playback is IDisposable d)
            d.Dispose();
    }

    private async Task EngineLoopAsync(CancellationToken cancellationToken)
    {
        var phase = 0f;
        while (!cancellationToken.IsCancellationRequested && _enabled)
        {
            var buffer = ProceduralSfx.EngineSegment(_throttle, phase);
            phase += (float)buffer.Duration.TotalSeconds;
            try
            {
                await _playback.PlayAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                _enabled = false;
                break;
            }
        }
    }

    private void PlayOneShot(PcmBuffer buffer)
    {
        if (!_enabled)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await _playback.PlayAsync(buffer).ConfigureAwait(false);
            }
            catch
            {
                _enabled = false;
            }
        });
    }

    private void StopEngine()
    {
        if (_engineCts is null)
            return;

        _engineCts.Cancel();
        try
        {
            _engineTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Best-effort shutdown.
        }

        _engineCts.Dispose();
        _engineCts = null;
        _engineTask = null;
    }
}
