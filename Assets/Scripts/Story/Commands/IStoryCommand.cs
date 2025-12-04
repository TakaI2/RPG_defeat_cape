using System.Collections;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Interface for story command execution
    /// </summary>
    public interface IStoryCommand
    {
        /// <summary>
        /// Execute the command with the given context and data
        /// </summary>
        /// <param name="context">Runtime context with UI and resource references</param>
        /// <param name="data">Command data with parameters</param>
        /// <returns>Coroutine enumerator for async execution</returns>
        IEnumerator Execute(StoryContext context, StoryCommandData data);
    }
}
