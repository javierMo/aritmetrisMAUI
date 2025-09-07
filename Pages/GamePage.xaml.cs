using Microsoft.Maui.Controls;
using Aritmetris.GameCore;

#if ANDROID
using Android.Views;
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
    // MainActivity llamará a este delegado para pasar las teclas Android aquí.
    internal static Func<KeyEvent, bool>? HandleAndroidKeyEventStatic;
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

#if ANDROID
        // Registramos el manejador estático para que MainActivity nos reenvíe las teclas
        HandleAndroidKeyEventStatic = HandleAndroidKeyEvent;
#endif

        MainThread.BeginInvokeOnMainThread(() => BoardView.Focus());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TeardownTimer();
        _softDrop = false;
        LevelUpOverlay.IsVisible = false;
        GameOverOverlay.IsVisible = false;

#if ANDROID
        // Muy importante limpiar el delegado para no filtrar teclas fuera de esta página
        HandleAndroidKeyEventStatic = null;
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
    // Recibe las teclas desde MainActivity a nivel global (sin depender de foco).
    private bool HandleAndroidKeyEvent(KeyEvent e)
    {
        if (e is null) return false;

        if (e.Action == KeyEventActions.Down)
        {
            bool firstPress = e.RepeatCount == 0;
            switch (e.KeyCode)
            {
                case Keycode.DpadLeft: if (firstPress) KeyMoveLeft(); return true;
                case Keycode.DpadRight: if (firstPress) KeyMoveRight(); return true;
                case Keycode.DpadUp: if (firstPress) KeyHardDrop(); return true;
                case Keycode.Space:
                case Keycode.ButtonA: if (firstPress) KeyRotate(); return true;
                case Keycode.DpadDown: KeySoftDropStart(); return true;
            }
        }
        else if (e.Action == KeyEventActions.Up && e.KeyCode == Keycode.DpadDown)
        {
            KeySoftDropStop();
            return true;
        }

        return false;
    }
#endif
}
