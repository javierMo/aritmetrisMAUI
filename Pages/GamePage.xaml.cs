using Microsoft.Maui.Controls;
using Aritmetris.GameCore;

#if ANDROID
using Android.Views;
using Java.Lang;
using AView = Android.Views.View;
using AKeycode = Android.Views.Keycode;
using AKeyAction = Android.Views.KeyEventActions;
// Para Platform.CurrentActivity
using Platform = Microsoft.Maui.ApplicationModel.Platform;
#endif

#if WINDOWS
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
    private GameState _game;
    private bool _softDrop = false;

#if ANDROID
    // Guardamos el listener para poder desengancharlo en OnDisappearing
    private GameKeyListener? _activityKeyListener;
#endif

    public GamePage()
    {
        InitializeComponent();
        _game = new GameState();
        WireGameToViewsAndEvents(_game);

#if WINDOWS
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

        // 1) Foco UI
        MainThread.BeginInvokeOnMainThread(() => BoardView.Focus());

#if ANDROID
        // 2) Listener a nivel de Window/DecorView (más robusto que en el BoardView)
        var activity = Platform.CurrentActivity;
        var decor = activity?.Window?.DecorView;
        if (decor is not null)
        {
            decor.Focusable = true;
            decor.FocusableInTouchMode = true;
            decor.RequestFocus();

            // Limpia listener previo por si reentramos
            decor.SetOnKeyListener(null);
            _activityKeyListener = new GameKeyListener(this);
            decor.SetOnKeyListener(_activityKeyListener);
        }
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TeardownTimer();
        _softDrop = false;
        LevelUpOverlay.IsVisible = false;
        GameOverOverlay.IsVisible = false;

#if ANDROID
        // Quita el listener del DecorView para no filtrar teclas fuera de esta página
        var activity = Platform.CurrentActivity;
        var decor = activity?.Window?.DecorView;
        if (decor is not null)
        {
            decor.SetOnKeyListener(null);
        }
        _activityKeyListener = null;
#endif
    }

    // =======================
    // CICLO DE JUEGO
    // =======================
    private void StartNewGame()
    {
        _game = new GameState();
        WireGameToViewsAndEvents(_game);

        LevelUpOverlay.IsVisible = false;
        GameOverOverlay.IsVisible = false;

        LevelLabel.Text = _game.Level.ToString();
        ScoreLabel.Text = _game.Score.ToString();
        ReqProgressLabel.Text = $"{_game.Score} / {_game.Config.Req}";
        TargetLabel.Text = _game.Target.ToString();
        ObjectiveLabel.Text = $"Objetivo: {_game.Target}";

        BoardView.InvalidateSurface();
        NextPieceView.InvalidateSurface();

        TeardownTimer();
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

    private void WireGameToViewsAndEvents(GameState game)
    {
        BoardView.BindGame(game);
        NextPieceView.BindGame(game);

        game.OnScoreChanged += null;
        game.OnLevelChanged += null;
        game.OnLevelUpDiff += null;
        game.OnTargetChanged += null;
        game.OnGameOver += null;

        game.OnScoreChanged += (_, s) =>
        {
            ScoreLabel.Text = s.ToString();
            ReqProgressLabel.Text = $"{s} / {game.Config.Req}";
        };
        game.OnLevelChanged += (_, l) =>
        {
            LevelLabel.Text = l.ToString();
            if (!_softDrop) SetTimerInterval(game.Config.DropMs);
        };
        game.OnLevelUpDiff += (_, diff) =>
        {
            _timer?.Stop();
            LevelUpList.Clear();
            if (diff.AddedNumbers is { Length: > 0 })
                foreach (var n in diff.AddedNumbers)
                    LevelUpList.Add(new Label { Text = $"¡nuevo valor! [{n}]", TextColor = Colors.White, FontSize = 18, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center });
            if (diff.AddedOps is { Length: > 0 })
                foreach (var op in diff.AddedOps)
                    LevelUpList.Add(new Label { Text = $"¡nueva operación! [{op}]", TextColor = Colors.White, FontSize = 18, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center });
            LevelUpOverlay.IsVisible = true;
        };
        game.OnTargetChanged += (_, t) =>
        {
            TargetLabel.Text = t.ToString();
            ObjectiveLabel.Text = $"Objetivo: {t}";
        };
        game.OnGameOver += (_, __) => ShowGameOver();
    }

    private void TeardownTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    private void OnContinueAfterLevelUp(object sender, EventArgs e)
    {
        LevelUpOverlay.IsVisible = false;
        SetTimerInterval(_game.Config.DropMs);
        _timer?.Start();
        MainThread.BeginInvokeOnMainThread(() => BoardView.Focus());
    }

    private void SetTimerInterval(int ms)
    {
        if (_timer != null)
            _timer.Interval = TimeSpan.FromMilliseconds(ms);
    }

    private void ShowGameOver()
    {
        _timer?.Stop();
        GameOverOverlay.IsVisible = true;
    }

    private async void OnBackToMenu(object sender, EventArgs e)
    {
        TeardownTimer();
        LevelUpOverlay.IsVisible = false;
        GameOverOverlay.IsVisible = false;
        _softDrop = false;
        await Shell.Current.GoToAsync("//MenuPage");
    }

    // =======================
    // GESTOS TÁCTILES (Android+iOS)
    // =======================
    private void OnTapRotate(object? sender, TappedEventArgs e) { _game.Rotate(); BoardView.InvalidateSurface(); }
    private void OnDoubleTapHardDrop(object? sender, TappedEventArgs e) { _game.HardDrop(); BoardView.InvalidateSurface(); NextPieceView.InvalidateSurface(); }
    private void OnSwipeLeft(object? sender, SwipedEventArgs e) { _game.MoveLeft(); BoardView.InvalidateSurface(); }
    private void OnSwipeRight(object? sender, SwipedEventArgs e) { _game.MoveRight(); BoardView.InvalidateSurface(); }

    // =======================
    // MÉTODOS UNIFICADOS DE INPUT
    // =======================
    private void KeyMoveLeft() { _game.MoveLeft(); BoardView.InvalidateSurface(); }
    private void KeyMoveRight() { _game.MoveRight(); BoardView.InvalidateSurface(); }
    private void KeyRotate() { _game.Rotate(); BoardView.InvalidateSurface(); }
    private void KeyHardDrop() { _game.HardDrop(); BoardView.InvalidateSurface(); NextPieceView.InvalidateSurface(); }
    private void KeySoftDropStart()
    {
        if (!_softDrop) { _softDrop = true; SetTimerInterval(System.Math.Max(50, _game.Config.DropMs / 8)); }
    }
    private void KeySoftDropStop()
    {
        if (_softDrop) { _softDrop = false; SetTimerInterval(_game.Config.DropMs); }
    }

#if WINDOWS
    // =======================
    // TECLADO (Windows)
    // =======================
    private void OnWinKeyUp(object sender, WinKeyRoutedEventArgs e)
    {
        if (e.Key == WinVirtualKey.Down) { KeySoftDropStop(); e.Handled = true; }
    }
    private void OnWinKeyDown(object sender, WinKeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case WinVirtualKey.Left:  KeyMoveLeft();  e.Handled = true; break;
            case WinVirtualKey.Right: KeyMoveRight(); e.Handled = true; break;
            case WinVirtualKey.Up:    KeyHardDrop();  e.Handled = true; break;
            case WinVirtualKey.Space: KeyRotate();    e.Handled = true; break;
            case WinVirtualKey.Down:  KeySoftDropStart(); e.Handled = true; break;
        }
    }
#endif

#if ANDROID
    // =======================
    // TECLADO (Android) — a nivel de DecorView
    // =======================
    private sealed class GameKeyListener : Java.Lang.Object, AView.IOnKeyListener
    {
        private readonly GamePage _page;
        public GameKeyListener(GamePage page) => _page = page;

        public bool OnKey(AView? v, AKeycode keyCode, KeyEvent? e)
        {
            if (e is null) return false;

            if (e.Action == AKeyAction.Down)
            {
                bool firstPress = e.RepeatCount == 0;
                switch (keyCode)
                {
                    case AKeycode.DpadLeft: if (firstPress) _page.KeyMoveLeft(); return true;
                    case AKeycode.DpadRight: if (firstPress) _page.KeyMoveRight(); return true;
                    case AKeycode.DpadUp: if (firstPress) _page.KeyHardDrop(); return true;
                    case AKeycode.Space:
                    case AKeycode.ButtonA: if (firstPress) _page.KeyRotate(); return true;
                    case AKeycode.DpadDown: _page.KeySoftDropStart(); return true;
                }
            }
            else if (e.Action == AKeyAction.Up && keyCode == AKeycode.DpadDown)
            {
                _page.KeySoftDropStop();
                return true;
            }
            return false;
        }
    }
#endif
}
