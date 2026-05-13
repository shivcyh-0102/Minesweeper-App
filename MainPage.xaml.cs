using System.ComponentModel;
using MinesweeperApp.Models;
using MinesweeperApp.ViewModels;

namespace MinesweeperApp
{
	public partial class MainPage : ContentPage
	{
		private readonly Dictionary<Button, CancellationTokenSource> _cellPresses = new();
		private readonly Dictionary<GameCell, Button> _visibleCells = new();
		private readonly HashSet<Button> _handledLongPresses = new();

		public MainPage()
		{
			InitializeComponent();

			if (BindingContext is GameViewModel viewModel)
				viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		private void CellButton_Loaded(object? sender, EventArgs e)
		{
			if (sender is Button button && button.BindingContext is GameCell cell)
				_visibleCells[cell] = button;
		}

		private void CellButton_Unloaded(object? sender, EventArgs e)
		{
			if (sender is not Button button || button.BindingContext is not GameCell cell)
				return;

			if (_visibleCells.TryGetValue(cell, out var currentButton) && currentButton == button)
				_visibleCells.Remove(cell);

			if (_cellPresses.Remove(button, out var tokenSource))
			{
				tokenSource.Cancel();
				tokenSource.Dispose();
			}

			_handledLongPresses.Remove(button);
		}

		private void CellButton_Pressed(object? sender, EventArgs e)
		{
			if (sender is not Button button || button.BindingContext is not GameCell cell)
				return;

			button.AbortAnimation(nameof(AnimatePressAsync));
			_ = AnimatePressAsync(button);

			var tokenSource = new CancellationTokenSource();
			_cellPresses[button] = tokenSource;
			_ = HandleLongPressAsync(button, cell, tokenSource.Token);
		}

		private void CellButton_Released(object? sender, EventArgs e)
		{
			if (sender is not Button button || button.BindingContext is not GameCell cell)
				return;

			bool wasRevealed = cell.IsRevealed;
			bool wasFlagged = cell.IsFlagged;

			if (_cellPresses.Remove(button, out var tokenSource))
			{
				tokenSource.Cancel();
				tokenSource.Dispose();
			}

			if (_handledLongPresses.Remove(button))
			{
				_ = AnimateReleaseAsync(button);
				return;
			}

			if (BindingContext is not GameViewModel viewModel)
				return;

			viewModel.RevealCommand.Execute(cell);
			_ = AnimateCellChangeAsync(button, cell, wasRevealed, wasFlagged);
		}

		private async Task HandleLongPressAsync(Button button, GameCell cell, CancellationToken cancellationToken)
		{
			try
			{
				await Task.Delay(520, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!_cellPresses.ContainsKey(button) || BindingContext is not GameViewModel viewModel)
					return;

				bool wasFlagged = cell.IsFlagged;
				_handledLongPresses.Add(button);
				viewModel.FlagCommand.Execute(cell);

				if (wasFlagged != cell.IsFlagged)
					_ = AnimateFlagAsync(button);
			});
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(GameViewModel.ShowResultDialog)
				&& BindingContext is GameViewModel { ShowResultDialog: true } viewModel)
			{
				_ = AnimateResultAsync(viewModel);
			}
		}

		private async Task AnimateCellChangeAsync(Button button, GameCell cell, bool wasRevealed, bool wasFlagged)
		{
			if (wasFlagged != cell.IsFlagged)
			{
				await AnimateFlagAsync(button);
				return;
			}

			if (wasRevealed || !cell.IsRevealed)
			{
				await AnimateReleaseAsync(button);
				return;
			}

			if (cell.IsMine)
			{
				await AnimateMineAsync(button);
				_ = AnimateVisibleMinesAsync(button);
				return;
			}

			await AnimateRevealAsync(button);
		}

		private static async Task AnimatePressAsync(Button button)
		{
			try
			{
				await button.ScaleToAsync(0.94, 70, Easing.CubicOut);
			}
			catch
			{
				// Animations can be cancelled when cells are recycled.
			}
		}

		private static async Task AnimateReleaseAsync(Button button)
		{
			try
			{
				await button.ScaleToAsync(1, 90, Easing.CubicOut);
			}
			catch
			{
				// Animations can be cancelled when cells are recycled.
			}
		}

		private static async Task AnimateRevealAsync(Button button)
		{
			try
			{
				button.AbortAnimation(nameof(AnimateRevealAsync));
				button.Opacity = 0.55;
				button.Scale = 0.86;
				await Task.WhenAll(
					button.FadeToAsync(1, 150, Easing.CubicOut),
					button.ScaleToAsync(1.03, 150, Easing.CubicOut));
				await button.ScaleToAsync(1, 80, Easing.CubicInOut);
			}
			catch
			{
				// Animations can be cancelled when cells are recycled.
			}
		}

		private static async Task AnimateFlagAsync(Button button)
		{
			try
			{
				button.AbortAnimation(nameof(AnimateFlagAsync));
				await button.ScaleToAsync(1.18, 95, Easing.SpringOut);
				await button.RotateToAsync(-8, 45, Easing.CubicOut);
				await button.RotateToAsync(8, 70, Easing.CubicInOut);
				await Task.WhenAll(
					button.RotateToAsync(0, 65, Easing.CubicOut),
					button.ScaleToAsync(1, 90, Easing.CubicOut));
			}
			catch
			{
				// Animations can be cancelled when cells are recycled.
			}
		}

		private static async Task AnimateMineAsync(Button button)
		{
			try
			{
				button.AbortAnimation(nameof(AnimateMineAsync));
				await button.ScaleToAsync(1.24, 80, Easing.CubicOut);
				await button.TranslateToAsync(-6, 0, 28);
				await button.TranslateToAsync(6, 0, 28);
				await button.TranslateToAsync(-4, 0, 26);
				await button.TranslateToAsync(4, 0, 26);
				await Task.WhenAll(
					button.TranslateToAsync(0, 0, 45, Easing.CubicOut),
					button.ScaleToAsync(1, 110, Easing.CubicIn));
			}
			catch
			{
				// Animations can be cancelled when cells are recycled.
			}
		}

		private async Task AnimateVisibleMinesAsync(Button sourceButton)
		{
			int delay = 0;
			foreach (var pair in _visibleCells.ToArray())
			{
				if (!pair.Key.IsMine || pair.Value == sourceButton)
					continue;

				await Task.Delay(delay);
				_ = AnimateMineAsync(pair.Value);
				delay = 22;
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
				// Result animation is decorative; never block the dialog.
			}
		}
	}
}
