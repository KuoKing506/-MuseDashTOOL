using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MdModManager.Models;
using MdModManager.Services;
using NAudio.Vorbis;
using NAudio.Wave;

namespace MdModManager.ViewModels;

public partial class ChartDownloadViewModel : ObservableObject, IDisposable
{
    private readonly IChartDownloadService _downloadService;
    private readonly IConfigService _configService;
    private readonly INotificationService _notificationService;
    private readonly IDownloadManagerService _downloadManagerService;
    private static readonly HttpClient _coverHttp = new() { Timeout = TimeSpan.FromSeconds(15) };

    // ── Charts list ───────────────────────────────────────────────────────────
    public ObservableCollection<MdmcChart> Charts { get; } = new();

    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isLoadingMore = false;
    [ObservableProperty] private string _statusMessage = "正在初始化…";
    [ObservableProperty] private bool _isEmpty = true;

    // ── Search ────────────────────────────────────────────────────────────────
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CurrentPage = 1; // 搜索时重置回第一页
                _ = ReloadDebouncedAsync();
            }
        }
    }

    /// <summary>是否启用谱面名称滚动</summary>
    public bool EnableMarquee => _configService.Config.EnableChartNameMarquee;

    private CancellationTokenSource? _searchCts;
    private async Task ReloadDebouncedAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            await Task.Delay(500, ct); // 500ms 延迟，避免输入过快频繁请求
            if (!ct.IsCancellationRequested)
            {
                await ReloadAsync(false, ct);
            }
        }
        catch (TaskCanceledException) { /* ignored */ }
    }

    // ── Sort ──────────────────────────────────────────────────────────────────
    /// <summary>排序方式列表，显示中文，Value 是 API 参数</summary>
    public (string Label, string Value)[] SortOptions { get; } = new[]
    {
        ("点赞数", "likes"),
        ("最新上传", "latest"),
        ("难度", "difficulty"),
    };

    private int _selectedSortIndex = 0;
    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                OnPropertyChanged(nameof(IsSortByLikes));
                OnPropertyChanged(nameof(IsSortByLatest));
                OnPropertyChanged(nameof(IsSortByDifficulty));
                CurrentPage = 1;
                _ = ReloadAsync();
            }
        }
    }

    public bool IsSortByLikes => SelectedSortIndex == 0;
    public bool IsSortByLatest => SelectedSortIndex == 1;
    public bool IsSortByDifficulty => SelectedSortIndex == 2;

    [ObservableProperty] private bool _isAscending = false;   // 默认倒序（最多点赞在前）
    [ObservableProperty] private bool _showUnranked = true;   // 显示未评级

    // ── Pagination ────────────────────────────────────────────────────────────
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;

    public bool CanLoadMore => CurrentPage < TotalPages && !IsLoading && !IsLoadingMore;

    // ── Audio playback ────────────────────────────────────────────────────────
    private WaveOutEvent? _waveOut;
    private MdmcChart? _playingChart;
    private CancellationTokenSource? _stopCts;
    private CancellationTokenSource? _loadCts;  // 用于取消正在下载的音频
    private bool _isLoadingPreview = false;      // 防止重复点击的加载锁

    // ─────────────────────────────────────────────────────────────────────────
    public ChartDownloadViewModel(
        IChartDownloadService downloadService,
        IConfigService configService,
        INotificationService notificationService,
        IDownloadManagerService downloadManagerService)
    {
        _downloadService = downloadService;
        _configService = configService;
        _notificationService = notificationService;
        _downloadManagerService = downloadManagerService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await ReloadAsync(false, ct);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task Refresh() => await ReloadAsync(false);

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        CurrentPage = 1;
        _ = ReloadAsync(false);
    }

    [RelayCommand]
    private void ToggleSortOrder()
    {
        IsAscending = !IsAscending;
        CurrentPage = 1;
        _ = ReloadAsync(false);
    }

    [RelayCommand]
    private async Task LoadNextPage()
    {
        if (CanLoadMore)
        {
            CurrentPage++;
            await ReloadAsync(true);
        }
    }

    /// <summary>切换试听状态（带加载锁，防止重复点击）</summary>
    [RelayCommand]
    private async Task TogglePreview(MdmcChart chart)
    {
        // 如果点击的就是正在播放的那一首，停止播放
        if (_playingChart == chart)
        {
            StopPlayback();
            return;
        }

        // 如果正在加载音频，忽略这次点击（防止厠加播放）
        if (_isLoadingPreview) return;

        StopPlayback();
        await PlayDemoAsync(chart);
    }

    /// <summary>加入下载队列</summary>
    [RelayCommand]
    private void DownloadChart(MdmcChart chart)
    {
        _downloadManagerService.EnqueueDownload(chart);
        _notificationService.ShowSuccess($"已添加到下载列表: 《{chart.Title}》");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task ReloadAsync(bool append = false, CancellationToken ct = default)
    {
        if (!append) StopPlayback();
        
        if (append) IsLoadingMore = true;
        else IsLoading = true;

        StatusMessage = append ? "正在加载更多…" : "正在加载谱面列表…";
        
        if (!append)
        {
            Charts.Clear();
            IsEmpty = true;
        }

        try
        {
            var sort  = SortOptions[SelectedSortIndex].Value;
            var order = IsAscending ? "asc" : "desc";

            var (charts, totalPages) = await _downloadService.FetchChartsAsync(
                CurrentPage, sort, order, SearchText.Trim(), !ShowUnranked, ct);

            TotalPages = Math.Max(1, totalPages);
            OnPropertyChanged(nameof(CanLoadMore));

            foreach (var c in charts)
            {
                Charts.Add(c);
            }

            IsEmpty = Charts.Count == 0;
            StatusMessage = IsEmpty
                ? "没有找到符合条件的谱面"
                : $"第 {CurrentPage} / {TotalPages} 页，共 {Charts.Count} 张";
        }
        catch (Exception ex)
        {
            StatusMessage = "加载失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
            IsLoadingMore = false;
            // Start lazy loading covers after list is updated
            _ = LoadCoversAsync();
        }
    }

    /// <summary>异步逐一加载封面图</summary>
    private async Task LoadCoversAsync()
    {
        for (int i = 0; i < Charts.Count; i++)
        {
            var chart = Charts[i];
            try
            {
                var bytes = await _coverHttp.GetByteArrayAsync(chart.CoverUrl);
                using var ms = new MemoryStream(bytes);
                var bmp = new Bitmap(ms);
                Avalonia.Threading.Dispatcher.UIThread.Post(() => chart.CoverImage = bmp);
            }
            catch { /* cover unavailable */ }
        }
    }

    private async Task PlayDemoAsync(MdmcChart chart)
    {
        // 取消之前的加载请求
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        _isLoadingPreview = true;
        try
        {
            var bytes = await _coverHttp.GetByteArrayAsync(chart.DemoUrl, ct);

            // 如果加载期间用户已取消或切换到其他谱面，无需继续播放
            if (ct.IsCancellationRequested) return;

            var ms = new MemoryStream(bytes);
            var vorbis = new VorbisWaveReader(ms);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(vorbis);
            _waveOut.Volume = (float)_configService.Config.ChartPreviewVolume;

            _stopCts = new CancellationTokenSource();
            var cts = _stopCts;

            _waveOut.PlaybackStopped += (_, _) =>
            {
                if (!cts.IsCancellationRequested)
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (_playingChart == chart) _playingChart = null;
                        chart.IsPlaying = false;
                    });
                vorbis.Dispose();
                ms.Dispose();
            };

            _waveOut.Play();
            _playingChart = chart;
            chart.IsPlaying = true;
        }
        catch (OperationCanceledException) { /* 用户取消，忽略 */ }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChartDownloadVM] Preview error: {ex.Message}");
        }
        finally
        {
            _isLoadingPreview = false;
        }
    }

    private void StopPlayback()
    {
        // 如果正在加载音频，取消加载
        _loadCts?.Cancel();
        _loadCts = null;
        _isLoadingPreview = false;

        _stopCts?.Cancel();
        _stopCts = null;

        if (_playingChart != null)
        {
            _playingChart.IsPlaying = false;
            _playingChart = null;
        }

        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
    }

    public void Dispose()
    {
        StopPlayback();
        Charts.Clear(); // 释放内存：移除对所有谱面对象极其封面图片的引用
    }
}
