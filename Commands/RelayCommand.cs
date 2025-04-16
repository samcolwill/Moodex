using System.Windows.Input;

namespace SamsGameLauncher.Commands
{
    // A simple ICommand implementation you can use for all your buttons/menu items.
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        // exec: the action to run; canExec: optional predicate for when the command is enabled
        public RelayCommand(Action<object?> exec, Func<object?, bool>? canExec = null)
        {
            _execute = exec;
            _canExecute = canExec;
        }

        // If no predicate was provided, defaults to always enabled.
        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter) ?? true;

        // Run the action
        public void Execute(object? parameter) =>
            _execute(parameter);

        // Call this when something changes that affects CanExecute.
        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
