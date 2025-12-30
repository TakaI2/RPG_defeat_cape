using System.Collections;
using UnityEngine;

namespace RPGDefete.Story.Commands
{
    /// <summary>
    /// Command to change VRM character expression
    /// </summary>
    public class ExpressionCommand : IStoryCommand
    {
        public IEnumerator Execute(StoryContext context, StoryCommandData data)
        {
            // Validate target character
            if (string.IsNullOrEmpty(data.targetCharacter))
            {
                Debug.LogWarning("[ExpressionCommand] targetCharacter is not specified");
                yield break;
            }

            // Validate expression name
            if (string.IsNullOrEmpty(data.expressionName))
            {
                Debug.LogWarning("[ExpressionCommand] expressionName is not specified");
                yield break;
            }

            // Get character controller
            if (!context.TryGetCharacter(data.targetCharacter, out var controller))
            {
                Debug.LogWarning($"[ExpressionCommand] Character not found: {data.targetCharacter}");
                yield break;
            }

            // Check if controller is valid
            if (!controller.IsValid)
            {
                Debug.LogWarning($"[ExpressionCommand] Character controller not valid: {data.targetCharacter}");
                yield break;
            }

            // Convert duration from milliseconds to seconds
            float durationSeconds = data.expressionDuration / 1000f;

            // Execute expression change
            if (data.waitForCompletion && durationSeconds > 0)
            {
                // Wait for transition to complete
                yield return controller.SetExpressionWithTransition(
                    data.expressionName,
                    data.expressionWeight,
                    durationSeconds
                );
            }
            else if (durationSeconds > 0)
            {
                // Start transition but don't wait
                controller.StartExpressionTransition(
                    data.expressionName,
                    data.expressionWeight,
                    durationSeconds
                );
            }
            else
            {
                // Immediate change
                controller.SetExpression(data.expressionName, data.expressionWeight);
            }
        }
    }
}
