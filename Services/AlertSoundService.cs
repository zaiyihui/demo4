using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ComputerCompanion.Services;

public interface IAlertSoundService
{
    void PlayAlertSound();
    void PlayCustomSound(string soundPath);
    void SetVolume(int volume);
    int GetVolume();
    void StopSound();
}

public class AlertSoundService : IAlertSoundService, IDisposable
{
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _reader;
    private int _volume = 70;
    private readonly string _defaultSoundPath;

    public AlertSoundService()
    {
        _defaultSoundPath = Path.Combine(AppContext.BaseDirectory, "Assets", "alert.wav");
        EnsureDefaultSoundExists();
    }

    private void EnsureDefaultSoundExists()
    {
        var dir = Path.GetDirectoryName(_defaultSoundPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        if (!File.Exists(_defaultSoundPath))
        {
            CreateDefaultSound();
        }
    }

    private void CreateDefaultSound()
    {
        try
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new WaveFileWriter(stream, new WaveFormat(44100, 1)))
                {
                    double duration = 0.5;
                    double freq = 800;
                    int samples = (int)(duration * 44100);
                    
                    for (int i = 0; i < samples; i++)
                    {
                        double t = (double)i / 44100;
                        double sample = Math.Sin(2 * Math.PI * freq * t) * 0.5;
                        writer.WriteSample((float)sample);
                    }
                }
                
                stream.Position = 0;
                using (var fileStream = File.Create(_defaultSoundPath))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
        catch
        {
        }
    }

    public void PlayAlertSound()
    {
        StopSound();
        
        try
        {
            if (File.Exists(_defaultSoundPath))
            {
                _reader = new AudioFileReader(_defaultSoundPath);
                _waveOut = new WaveOutEvent();
                _waveOut.Volume = _volume / 100.0f;
                _waveOut.Init(_reader);
                _waveOut.Play();
                _waveOut.PlaybackStopped += OnPlaybackStopped;
            }
        }
        catch
        {
        }
    }

    public void PlayCustomSound(string soundPath)
    {
        StopSound();
        
        try
        {
            if (File.Exists(soundPath))
            {
                _reader = new AudioFileReader(soundPath);
                _waveOut = new WaveOutEvent();
                _waveOut.Volume = _volume / 100.0f;
                _waveOut.Init(_reader);
                _waveOut.Play();
                _waveOut.PlaybackStopped += OnPlaybackStopped;
            }
        }
        catch
        {
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        StopSound();
    }

    public void SetVolume(int volume)
    {
        _volume = Math.Clamp(volume, 0, 100);
        
        if (_waveOut != null)
        {
            _waveOut.Volume = _volume / 100.0f;
        }
    }

    public int GetVolume()
    {
        return _volume;
    }

    public void StopSound()
    {
        try
        {
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        StopSound();
    }
}
