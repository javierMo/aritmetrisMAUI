using Microsoft.Maui.Controls;
using Aritmetris.GameCore;

#if ANDROID && DEBUG
// Alias para evitar choque con Microsoft.Maui.Controls.View
using AView = Android.Views.View;
using AKeyEventArgs = Android.Views.View.KeyEventArgs;
using AKeycode = Android.Views.Keycode;
using AKeyAction = Android.Views.KeyEventActions;
#endif

#if WINDOWS && DEBUG
using WinWindow = Microsoft.UI.Xaml.Window;
using WinFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WinFocusState = Microsoft.UI.Xaml.FocusState;
using WinKeyRoutedEventArgs = Microsoft.UI.Xaml.Input.KeyRoutedEventArgs;
using WinVirtualKey = Windows.System.VirtualKey;
#endif

namespace Aritmetris.Pages;

public partial class GamePage : ContentPage
{
    private IDispatcherTimer? _timer;
    private readonly GameState _game = new();
    private bool _softDrop = false;

    public GamePage()
    {
        InitializeComponent();

        BoardView.BindGame(_game);
        NextPieceView.BindGame(_game);

        _game.OnScoreChanged += (_, s) => { ScoreLabel.Text = s.ToString(); ReqProgressLabel.Text = $"{s} / {_game.Config.Req}"; };
        _game.OnLevelChanged += (_, l) =>
        {
            LevelLabel.Text = l.ToString();
            if (!_softDrop) SetTimerInterval(_game.Config.DropMs);
        };
        _game.OnLevelUpDiff += (_, diff) =>
        {
            // Pausar timer
            _timer?.Stop();
            // Limpiar lista
            LevelUpList.Clear();
            // Añadir diffs
            if (diff.AddedNumbers != null && diff.AddedNumbers.Length > 0)
            {
                foreach (var n in diff.AddedNumbers)
                    LevelUpList.Add(new Label { Text = $"¡nuevo valor! [{n}]", TextColor = Colors.White, FontSize = 18, HorizontalTextAlignment = TextAlignment.Center });
            }
            if (diff.AddedOps != null && diff.AddedOps.Length > 0)
            {
                foreach (var op in diff.AddedOps)
                    LevelUpList.Add(new Label { Text = $"¡nueva operación! [{op}]", TextColor = Colors.White, FontSize = 18, HorizontalTextAlignment = TextAlignment.Center });
            }
            // Mostrar overlay
            LevelUpOverlay.IsVisible = true;
        };
    
        _game.OnTargetChanged += (_, t) =>
        {
            TargetLabel.Text = t.ToString();
            ObjectiveLabel.Text = $"Objetivo: {t}";
        };
        _game.OnGameOver += (_, __) => ShowGameOver();

#if ANDROID && DEBUG
        // Conecta teclado del emulador Android a la vista nativa
        BoardView.HandlerChanged += (_, __) =>
        {
            if (BoardView.Handler?.PlatformView is AView native)
            {
                native.Focusable = true;
                native.FocusableInTouchMode = true;
                native.RequestFocus();
                native.KeyPress += OnAndroidKeyEvent; // único handler (DOWN/UP)
            }
        };
#endif

#if WINDOWS && DEBUG
        // Teclado en Windows (debug)
        this.Loaded += (_, __) =>
        {
            var win = this.Window?.Handler?.PlatformView as WinWindow;
            if (win?.Content is WinFrameworkElement root)
            {
                root.IsTabStop = true;
                root.KeyDown += OnWinKeyDown;
                root.KeyUp += OnWinKeyUp;
                _ = root.Focus(WinFocusState.Programmatic);
            }
        };
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartNewGame();

        // En Android, asegúrate de que el BoardView tenga foco (emulador)
        MainThread.BeginInvokeOnMainThread(() => BoardView.Focus());
    }

    void StartNewGame()
    {
        GameOverOverlay.IsVisible = false;
        _game.Reset();

        LevelLabel.Text = _game.Level.ToString();
        ScoreLabel.Text = _game.Score.ToString(); ReqProgressLabel.Text = $"{_game.Score} / {_game.Config.Req}";
        TargetLabel.Text = _game.Target.ToString();
        ObjectiveLabel.Text = $"Objetivo: {_game.Target}";

        BoardView.InvalidateSurface();
        NextPieceView.InvalidateSurface();

        _timer?.Stop();
        _timer = Microsoft.Maui.Controls.Application.Current!.Dispatcher.CreateTimer();
        SetTimerInterval(_game.Config.DropMs);
        _timer.Tick += (_, __) =>
        {
            _game.Tick();
            BoardView.InvalidateSurface();
            NextPieceView.InvalidateSurface();
        };
        _timer.Start();
    }

    private void OnContinueAfterLevelUp(object sender, EventArgs e)
    {
        LevelUpOverlay.IsVisible = false;
        // Reanudar timer con el drop del nivel actual
        SetTimerInterval(_game.Config.DropMs);
        _timer?.Start();
    }

    void SetTimerInterval(int ms)
    {
        if (_timer != null)
            _timer.Interval = TimeSpan.FromMilliseconds(ms);
    }

    void ShowGameOver()
    {
        _timer?.Stop();
        GameOverOverlay.IsVisible = true;
    }

    private async void OnBackToMenu(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MenuPage");
    }

    // =======================
    // GESTOS TÁCTILES (Android+iOS)
    // =======================
    private void OnTapRotate(object? sender, TappedEventArgs e)
    {
        _game.Rotate();
        BoardView.InvalidateSurface();
    }

    private void OnDoubleTapHardDrop(object? sender, TappedEventArgs e)
    {
        _game.HardDrop();
        BoardView.InvalidateSurface();
        NextPieceView.InvalidateSurface();
    }

    private void OnSwipeLeft(object? sender, SwipedEventArgs e)
    {
        _game.MoveLeft();
        BoardView.InvalidateSurface();
    }

    private void OnSwipeRight(object? sender, SwipedEventArgs e)
    {
        _game.MoveRight();
        BoardView.InvalidateSurface();
    }

    // =======================
    // TECLADO (Android emulador) — solo DEBUG
    // =======================
#if ANDROID && DEBUG
    private void OnAndroidKeyEvent(object? sender, AKeyEventArgs e)
    {
        if (e?.Event == null) return;

        var action = e.Event.Action;   // Down / Up
        var key    = e.KeyCode;

        if (action == AKeyAction.Down)
        {
            switch (key)
            {
                case AKeycode.DpadLeft:
                    if (e.Event.RepeatCount == 0) { _game.MoveLeft(); BoardView.InvalidateSurface(); }
                    e.Handled = true; break;

                case AKeycode.DpadRight:
                    if (e.Event.RepeatCount == 0) { _game.MoveRight(); BoardView.InvalidateSurface(); }
                    e.Handled = true; break;

                case AKeycode.DpadUp:
                    if (e.Event.RepeatCount == 0) { _game.HardDrop(); BoardView.InvalidateSurface(); NextPieceView.InvalidateSurface(); }
                    e.Handled = true; break;

                case AKeycode.Space:
                    if (e.Event.RepeatCount == 0) { _game.Rotate(); BoardView.InvalidateSurface(); }
                    e.Handled = true; break;

                case AKeycode.DpadDown:
                    if (!_softDrop)
                    {
                        _softDrop = true;
                        SetTimerInterval(Math.Max(50, _game.Config.DropMs / 8)); // acelera
                    }
                    e.Handled = true; break;
            }
        }
        else if (action == AKeyAction.Up)
        {
            if (key == AKeycode.DpadDown)
            {
                _softDrop = false;
                SetTimerInterval(_game.Config.DropMs); // normal
                e.Handled = true;
            }
        }
    }
#endif

    // =======================
    // TECLADO (Windows) — solo DEBUG
    // =======================
#if WINDOWS && DEBUG
    private void OnWinKeyUp(object sender, WinKeyRoutedEventArgs e)
    {
        if (e.Key == WinVirtualKey.Down)
        {
            _softDrop = false;
            SetTimerInterval(_game.Config.DropMs);
            e.Handled = true;
        }
    }

    private void OnWinKeyDown(object sender, WinKeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case WinVirtualKey.Left:
                _game.MoveLeft();
                BoardView.InvalidateSurface();
                e.Handled = true; break;

            case WinVirtualKey.Right:
                _game.MoveRight();
                BoardView.InvalidateSurface();
                e.Handled = true; break;

            case WinVirtualKey.Up:
                _game.HardDrop();
                BoardView.InvalidateSurface();
                NextPieceView.InvalidateSurface();
                e.Handled = true; break;

            case WinVirtualKey.Space:
                _game.Rotate();
                BoardView.InvalidateSurface();
                e.Handled = true; break;

            case WinVirtualKey.Down:
                if (!_softDrop)
                {
                    _softDrop = true;
                    SetTimerInterval(Math.Max(50, _game.Config.DropMs / 8));
                }
                e.Handled = true; break;
        }
    }
#endif
}


    