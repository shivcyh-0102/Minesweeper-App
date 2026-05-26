using System.ComponentModel;
using MinesweeperApp.ViewModels;

namespace MinesweeperApp
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			if (BindingContext is GameViewModel viewModel)
				viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(GameViewModel.ShowResultDialog)
				&& BindingContext is GameViewModel { ShowResultDialog: true } viewModel)
			{
				_ = AnimateResultAsync(viewModel);
			}
		}

		private async Task AnimateResultAsync(GameViewModel viewModel)
		{
			try
			{
				ResultCard.Opacity = 0;
				ResultCard.Scale = viewModel.ResultTitle.Contains("Win", StringComparison.OrdinalIgnoreCase) ? 0.92 : 1.04;
				await Task.WhenAll(
					ResultCard.FadeToAsync(1, 170, Easing.CubicOut),
					ResultCard.ScaleToAsync(1, 210, Easing.SpringOut));

				if (viewModel.ResultTitle.Contains("Win", StringComparison.OrdinalIgnoreCase))
				{
					await ResultCard.ScaleToAsync(1.025, 130, Easing.CubicOut);
					await ResultCard.ScaleToAsync(1, 160, Easing.CubicInOut);
				}
			}
			catch
			{
				
			}
		}
	}
}
