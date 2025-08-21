namespace Aritmetris.Pages;

public partial class MenuPage : ContentPage
{
    public MenuPage() => InitializeComponent();
    private async void OnPlayClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//GamePage");
}