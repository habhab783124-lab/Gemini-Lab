#nullable enable
namespace GeminiLab.Core.Events
{
    /// <summary>
    /// Represents an executable command unit.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Human-readable command name for diagnostics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes command logic.
        /// </summary>
        void Execute();
    }
}
