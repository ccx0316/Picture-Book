using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CHARACTERS;
using System.Linq;

namespace COMMANDS
{
    public class CMD_DatabaseExtension_Characters : CMD_DataBaseExtension
    {
        private static string[] PARAM_ENABLE => new string[] { "-e", "-enable" };
        private static string[] PARAM_IMMEDIATE => new string[] { "-i", "-immediate" };
        private static string PARAM_XPOS => "-x";
        private static string PARAM_YPOS => "-y";
        private static string[] PARAM_SPEED => new string[] { "-spd", "-speed" };
        private static string[] PARAM_SMOOTH => new string[] { "-sm", "-smooth" };
        new public static void Extend(CommandDataBase database)
        {
            database.AddCommand("createcharacter", new Action<string[]>(CreateCharacter));
            database.AddCommand("movecharacter", new Func<string[],IEnumerator>(MoveCharacter));
            database.AddCommand("show", new Func<string[], IEnumerator>(ShowAll));
            database.AddCommand("hide", new Func<string[], IEnumerator>(HideAll));
            database.AddCommand("setcolor", new Func<string[], IEnumerator>(SetColor));
            database.AddCommand("sort", new Action<string[]>(Sort));
            CommandDataBase baseCommands = CommandManager.instance.CreatSubDatabase(CommandManager.DATABASE_CHARACTERS_BASE);
            baseCommands.AddCommand("move",new Func<string[], IEnumerator>(MoveCharacter));
            baseCommands.AddCommand("show",new Func<string[], IEnumerator>(Show));
            baseCommands.AddCommand("hide",new Func<string[], IEnumerator>(Hide));
            baseCommands.AddCommand("setpriority",new Action<string[]>(SetPriority));
        }
        public static void CreateCharacter(string[] data)
        {
            string characterName = data[0];
            bool enable = false;
            bool immediate = false;
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(PARAM_ENABLE, out enable, defaultValue: false);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);
            Character character = CharacterManager.instance.CreateCharacter(characterName, revealAfterCreation: (!immediate ? enable : false));
            if(!enable)
            {
                return;
            }
            if (immediate)
                character.isVisible = true;
            else
                character.Show();
        }
        private static IEnumerator MoveCharacter(string[] data)
        {
            string characterName = data[0];
            Character character = CharacterManager.instance.GetCharacter(characterName);
            if (characterName == null)
                yield break;
            float x = 0, y = 0;
            float speed = 1;
            bool smooth = false;
            bool immediate = false;
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(PARAM_XPOS, out x);
            parameters.TryGetValue(PARAM_YPOS, out y);
            parameters.TryGetValue(PARAM_SPEED, out speed, defaultValue: 1);
            parameters.TryGetValue(PARAM_SMOOTH, out smooth, defaultValue: false);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);
            Vector2 position = new Vector2((float)x, (float)y);
            if (immediate)
                character.SetPosition(position);
            else
            {
                CommandManager.instance.AddTerminationActionToCurrentProcess(() => { character?.SetPosition(position); });   
                yield return character.MoveToPosition(position, speed, smooth);
            }
        }
        public static IEnumerator ShowAll(string[] data)
        {
            List<Character> characters = new List<Character>();
            bool immediate = false;
            foreach(string s in data)
            {
                Character character = CharacterManager.instance.GetCharacter(s, createIfDoesNotExist: false);
                if(character != null) 
                    characters.Add(character);
            }
            if(characters.Count == 0)
            {
                yield break;
            }
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);
            foreach(Character character in characters)
            {
                if (immediate)
                    character.isVisible = true;
                else
                    character.Show();
            }
            if(!immediate)
            {
                CommandManager.instance.AddTerminationActionToCurrentProcess(() =>
                {
                    foreach (Character character in characters)
                        character.isVisible = true;
                });
                while (characters.Any(c => c.isRevealing))
                    yield return null;
            }
        }
        public static IEnumerator HideAll(string[] data)
        {
            List<Character> characters = new List<Character>();
            bool immediate = false;
            foreach (string s in data)
            {
                Character character = CharacterManager.instance.GetCharacter(s, createIfDoesNotExist: false);
                if (character != null)
                    characters.Add(character);
            }
            if (characters.Count == 0)
            {
                yield break;
            }
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] { "-i", "-immediate" }, out immediate, defaultValue: false);
            foreach (Character character in characters)
            {
                if (immediate)
                    character.isVisible = false;
                else
                    character.Hide();
            }
            if (!immediate)
            {
                CommandManager.instance.AddTerminationActionToCurrentProcess(() =>
                {
                    foreach (Character character in characters)
                        character.isVisible = false;
                });
                while (characters.Any(c => c.isHiding))
                    yield return null;
            }
        }
        public static IEnumerator SetColor(string[] data) 
        {
            Character character = CharacterManager.instance.GetCharacter(data[0], createIfDoesNotExist: false);
            string colorName;
            float speed;
            bool immediate;
            if(character==null||data.Length<2)
                yield break;
            var parameters = ConvertDataToParameters(data, startingIndex: 1);
            parameters.TryGetValue(new string[] { "-c", "-color" }, out colorName);
            bool specifiedSpeed = parameters.TryGetValue(new string[] { "-spd", "speed" }, out speed, defaultValue: 1f);
            if (!specifiedSpeed)
                parameters.TryGetValue(new string[] { "-i", "-immediate" }, out immediate, defaultValue: true);
            else
                immediate = false;
            Color color = Color.white;
            color=color.GetColorFromName(colorName);
            if(immediate)
                character.SetColor(color);
            else
            {
                CommandManager.instance.AddTerminationActionToCurrentProcess(() => { character?.SetColor(color); });
                character.TransitionColor(color, speed);
            }
            yield break;
        }
        private static void Sort(string[] data)
        {
            CharacterManager.instance.SortCharacters(data);
        }
        private static IEnumerator Show(string[] data)
        {
            Character character = CharacterManager.instance.GetCharacter(data[0]);
            if(character==null)
            {
                yield break;
            }
            bool immediate = false;
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] {"-i","-immediate"},out immediate, defaultValue: false);
            if (immediate)
                character.isVisible = true;
            else
            {
                CommandManager.instance.AddTerminationActionToCurrentProcess(() => { if (character != null) character.isVisible = true; });
                yield return character.Show();
            }
        }
        private static IEnumerator Hide(string[] data)
        {
            Character character = CharacterManager.instance.GetCharacter(data[0]);
            if (character == null)
            {
                yield break;
            }
            bool immediate = false;
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] { "-i", "-immediate" }, out immediate, defaultValue: false);
            if (immediate)
                character.isVisible = false;
            else
            {
                CommandManager.instance.AddTerminationActionToCurrentProcess(() => { if (character != null) character.isVisible = false; });
                yield return character.Hide();
            }
        }
        public static void SetPriority(string[] data)
        {
            Character character = CharacterManager.instance.GetCharacter(data[0], createIfDoesNotExist: false);
            int priority;
            if (character == null || data.Length < 2)
                return;
            if (!int.TryParse(data[1], out priority))
                priority = 0;
            character.SetPriority(priority);
        }
    }
}