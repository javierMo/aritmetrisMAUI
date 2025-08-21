using Aritmetris.GameCore;
using Aritmetris.Rendering;

namespace Aritmetris.Pages;

public partial class GamePage : ContentPage
{
    private IDispatcherTimer? _timer;
    private readonly GameState _game = new();

    public GamePage()
    {
        InitializeComponent();
        BoardView.BindGame(_game);
        NextPieceView.BindGame(_game);

        _game.OnScoreChanged += (_, s) => ScoreLabel.Text = s.ToString();
        _game.OnLevelChanged += (_, l) => { LevelLabel.Text = l.ToString(); if (_timer!=null) _timer.Interval = TimeSpan.FromMilliseconds(_game.Config.DropMs); };
        _game.OnTargetChanged += (_, t) => { TargetLabel.Text = t.ToString(); ObjectiveLabel.Text = $"Objetivo: {t}"; };
        _game.OnGameOver += (_, __) => ShowGameOver();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartNewGame();
    }

    void StartNewGame()
    {
        GameOverOverlay.IsVisible = false;
        _game.Reset();
        BoardView.InvalidateSurface();
        NextPieceView.InvalidateSurface();
        _timer?.Stop();
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(_game.Config.DropMs);
        _timer.Tick += (_, __) => { _game.Tick(); BoardView.InvalidateSurface(); NextPieceView.InvalidateSurface(); };
        _timer.Start();
    }

    void ShowGameOver()
    {
        _timer?.Stop();
        GameOverOverlay.IsVisible = true;
    }

    private async void OnBackToMenu(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MenuPage");
}