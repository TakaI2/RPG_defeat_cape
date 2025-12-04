using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGDefete.Story
{
    /// <summary>
    /// ScriptableObject that holds story data with commands
    /// </summary>
    [CreateAssetMenu(fileName = "NewStory", menuName = "RPG Defete/Story/Story Data")]
    public class StoryData : ScriptableObject
    {
        [SerializeField]
        private string storyId;

        [SerializeField]
        private List<StoryCommandData> commands = new List<StoryCommandData>();

        /// <summary>
        /// Unique identifier for this story
        /// </summary>
        public string StoryId
        {
            get => storyId;
            set => storyId = value;
        }

        /// <summary>
        /// Read-only access to commands
        /// </summary>
        public IReadOnlyList<StoryCommandData> Commands => commands;

        /// <summary>
        /// Number of commands in this story
        /// </summary>
        public int CommandCount => commands.Count;

        /// <summary>
        /// Import story data from JSON string
        /// </summary>
        /// <param name="json">JSON string in the standard story format</param>
        public void ImportFromJson(string json)
        {
            var jsonData = JsonUtility.FromJson<StoryJsonData>(json);
            if (jsonData == null)
            {
                Debug.LogError("[StoryData] Failed to parse JSON");
                return;
            }

            storyId = jsonData.id;
            commands.Clear();

            if (jsonData.script == null) return;

            foreach (var cmd in jsonData.script)
            {
                var commandData = new StoryCommandData
                {
                    op = cmd.op
                };

                switch (cmd.op)
                {
                    case "say":
                        commandData.characterName = cmd.name;
                        commandData.portrait = cmd.portrait;
                        commandData.portraitPosition = ParsePortraitPosition(cmd.portraitPosition);
                        commandData.portraitScale = cmd.portraitScale > 0 ? cmd.portraitScale : 1f;
                        commandData.lines = cmd.lines != null ? new List<string>(cmd.lines) : new List<string>();
                        break;

                    case "bg":
                        commandData.backgroundName = cmd.name;
                        commandData.fadeDuration = cmd.fade;
                        break;

                    case "bgm.play":
                        commandData.bgmName = cmd.name;
                        commandData.loop = cmd.loop;
                        commandData.volume = cmd.volume > 0 ? cmd.volume : 1f;
                        commandData.fadeInDuration = cmd.fade;
                        break;

                    case "bgm.stop":
                        commandData.fadeOutDuration = cmd.fade;
                        break;

                    case "se.play":
                        commandData.seName = cmd.name;
                        commandData.seVolume = cmd.volume > 0 ? cmd.volume : 1f;
                        break;

                    case "wait":
                        commandData.waitDuration = cmd.duration;
                        break;

                    case "end":
                        commandData.returnTo = cmd.returnTo;
                        break;
                }

                commands.Add(commandData);
            }
        }

        /// <summary>
        /// Export story data to JSON string
        /// </summary>
        /// <returns>JSON string in the standard story format</returns>
        public string ExportToJson()
        {
            var jsonData = new StoryJsonData
            {
                id = storyId,
                script = new List<StoryJsonCommand>()
            };

            foreach (var cmd in commands)
            {
                var jsonCmd = new StoryJsonCommand
                {
                    op = cmd.op
                };

                switch (cmd.op)
                {
                    case "say":
                        jsonCmd.name = cmd.characterName;
                        jsonCmd.portrait = cmd.portrait;
                        jsonCmd.portraitPosition = cmd.portraitPosition.ToString().ToLower();
                        jsonCmd.portraitScale = cmd.portraitScale;
                        jsonCmd.lines = cmd.lines?.ToArray();
                        break;

                    case "bg":
                        jsonCmd.name = cmd.backgroundName;
                        jsonCmd.fade = cmd.fadeDuration;
                        break;

                    case "bgm.play":
                        jsonCmd.name = cmd.bgmName;
                        jsonCmd.loop = cmd.loop;
                        jsonCmd.volume = cmd.volume;
                        jsonCmd.fade = cmd.fadeInDuration;
                        break;

                    case "bgm.stop":
                        jsonCmd.fade = cmd.fadeOutDuration;
                        break;

                    case "se.play":
                        jsonCmd.name = cmd.seName;
                        jsonCmd.volume = cmd.seVolume;
                        break;

                    case "wait":
                        jsonCmd.duration = cmd.waitDuration;
                        break;

                    case "end":
                        jsonCmd.returnTo = cmd.returnTo;
                        break;
                }

                jsonData.script.Add(jsonCmd);
            }

            return JsonUtility.ToJson(jsonData, true);
        }

        private PortraitPosition ParsePortraitPosition(string position)
        {
            if (string.IsNullOrEmpty(position)) return PortraitPosition.Center;

            return position.ToLower() switch
            {
                "left" => PortraitPosition.Left,
                "right" => PortraitPosition.Right,
                _ => PortraitPosition.Center
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// Add a command to the story (Editor only)
        /// </summary>
        public void AddCommand(StoryCommandData command)
        {
            commands.Add(command);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Insert a command at the specified index (Editor only)
        /// </summary>
        public void InsertCommand(int index, StoryCommandData command)
        {
            commands.Insert(index, command);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Remove a command at the specified index (Editor only)
        /// </summary>
        public void RemoveCommand(int index)
        {
            if (index >= 0 && index < commands.Count)
            {
                commands.RemoveAt(index);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>
        /// Move a command from one index to another (Editor only)
        /// </summary>
        public void MoveCommand(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= commands.Count) return;
            if (toIndex < 0 || toIndex >= commands.Count) return;

            var command = commands[fromIndex];
            commands.RemoveAt(fromIndex);
            commands.Insert(toIndex, command);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Get command at index for editing (Editor only)
        /// </summary>
        public StoryCommandData GetCommandForEdit(int index)
        {
            if (index >= 0 && index < commands.Count)
            {
                return commands[index];
            }
            return null;
        }

        /// <summary>
        /// Clear all commands (Editor only)
        /// </summary>
        public void ClearCommands()
        {
            commands.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// JSON serialization structure for story data
    /// </summary>
    [Serializable]
    internal class StoryJsonData
    {
        public string id;
        public List<StoryJsonCommand> script;
    }

    /// <summary>
    /// JSON serialization structure for a single command
    /// </summary>
    [Serializable]
    internal class StoryJsonCommand
    {
        public string op;
        public string name;
        public string portrait;
        public string portraitPosition;
        public float portraitScale;
        public string[] lines;
        public int fade;
        public bool loop;
        public float volume;
        public int duration;
        public string returnTo;
    }
}
