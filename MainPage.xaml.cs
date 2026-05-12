using MinesweeperApp.Models;
using MinesweeperApp.ViewModels;

namespace MinesweeperApp
{
	public partial class MainPage : ContentPage
	{
		private readonly Dictionary<Button, CancellationTokenSource> _cellPresses = new();
		private readonly HashSet<Button> _handledLongPresses = new();

		public MainPage()
		{
			InitializeComponent();
		}

		private void CellButton_Pressed(object? sender, EventArgs e)
		{
			if (sender is not Button button || button.BindingContext is not GameCell cell)
				return;

			var tokenSource = new CancellationTokenSource();
			_cellPresses[button] = tokenSource;
			_ = HandleLongPressAsync(button, cell, tokenSource.Token);
		}

		private void CellButton_Released(object? sender, EventArgs e)
		{
			if (sender is not Button button)
				return;

			if (_cellPresses.Remove(button, out var tokenSource))
			{
				tokenSource.Cancel();
				tokenSource.Dispose();
			}

			if (_handledLongPresses.Remove(button))
				return;

			if (BindingContext is GameViewModel viewModel && button.BindingContext is GameCell cell)
				viewModel.RevealCommand.Execute(cell);
		}

		private async Task HandleLongPressAsync(Button button, GameCell cell, CancellationToken cancellationToken)
		{
			try
			{
				await Task.Delay(550, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!_cellPresses.ContainsKey(button) || BindingContext is not GameViewModel viewModel)
					return;

				_handledLongPresses.Add(button);
				viewModel.FlagCommand.Execute(cell);
			});
		}
	}
}
