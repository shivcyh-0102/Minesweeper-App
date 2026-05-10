using MinesweeperApp.ViewModels;

namespace MinesweeperApp
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
			(BindingContext as GameViewModel).PropertyChanged += OnViewModelPropertyChanged;
			UpdateScreenVisibility();
		}

		private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(GameViewModel.IsGameStarted))
			{
				UpdateScreenVisibility();
			}
		}

		private void UpdateScreenVisibility()
		{
			if (BindingContext is GameViewModel vm)
			{
				StartScreen.IsVisible = !vm.IsGameStarted;
				GameScreen.IsVisible = vm.IsGameStarted;
			}
		}
	}
}
