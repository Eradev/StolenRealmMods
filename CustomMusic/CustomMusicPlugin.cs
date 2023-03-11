using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using MapNodeSystem;
using UnityEngine;
using UnityEngine.Networking;

namespace eradev.stolenrealm.CustomMusic
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class CustomMusicPlugin : BaseUnityPlugin
    {
        private static readonly List<AudioClip> _townMusicFiles = new();
        private static readonly Dictionary<string, List<AudioClip>> _battleMusicFiles = new();
        private static readonly Dictionary<string, List<AudioClip>> _explorationMusicFiles = new();
        private static readonly List<AudioClip> _genericBattleMusicFiles = new();
        private static readonly List<AudioClip> _genericExplorationMusicFiles = new();
        private static readonly List<AudioClip> _victoryMusicFiles = new();
        private static readonly List<AudioClip> _defeatMusicFiles = new();

        private static ManualLogSource _log;

        private static Coroutine _currentCoroutine;
        private static BgmType _currentBgmType;
        private static string _currentTerrainTimeKey;

        [UsedImplicitly]
        private async void Awake()
        {
            _log = Logger;

            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            await LoadAllAudioClipsAsync(GetFolderPath("Town"), _townMusicFiles);

            foreach (var terrainType in (TerrainType[])Enum.GetValues(typeof(TerrainType)))
            {
                foreach (var timeOfDay in (TimeOfDay[])Enum.GetValues(typeof(TimeOfDay)))
                {
                    var key = $"{terrainType}_{timeOfDay}";

                    var battleMusicList = new List<AudioClip>();
                    _battleMusicFiles.Add(key, battleMusicList);
                    await LoadAllAudioClipsAsync(GetFolderPath("Battle", $"{terrainType}", $"{timeOfDay}"), battleMusicList);

                    var explorationMusicList = new List<AudioClip>();
                    _explorationMusicFiles.Add(key, explorationMusicList);
                    await LoadAllAudioClipsAsync(GetFolderPath("Exploration", $"{terrainType}", $"{timeOfDay}"), explorationMusicList);
                }
            }

            await LoadAllAudioClipsAsync(GetFolderPath("Battle", "Generic"), _genericBattleMusicFiles);
            await LoadAllAudioClipsAsync(GetFolderPath("Exploration", "Generic"), _genericExplorationMusicFiles);
            await LoadAllAudioClipsAsync(GetFolderPath("Victory"), _victoryMusicFiles);
            await LoadAllAudioClipsAsync(GetFolderPath("Defeat"), _defeatMusicFiles);
        }

        [HarmonyPatch(typeof(SoundManager), "ExecuteFadeAudioIn")]
        public class SoundManagerExecuteFadeAudioInPatch
        {
            [UsedImplicitly]
            private static void Postfix(
                SoundManager __instance,
                AudioSource source,
                AudioClip audioClip)
            {
                _log.LogInfo($"Playing audio file {audioClip.name}");

                if (!source.loop && _currentBgmType is BgmType.Town or BgmType.Battle or BgmType.Exploration)
                {
                    if (_currentCoroutine != null)
                    {
                        __instance.StopCoroutine(_currentCoroutine);
                    }

                    _currentCoroutine = __instance.StartCoroutine(PlayRandomSongNext(source, audioClip));
                }
            }
        }

        [HarmonyPatch(typeof(SoundManager), "StartTownMusic")]
        public class SoundManagerStartTownMusicPatch
        {
            [UsedImplicitly]
            private static bool Prefix(SoundManager __instance)
            {
                _currentBgmType = BgmType.Town;

                var randomAudioFile = GetRandomTownMusic();

                if (randomAudioFile == null)
                {
                    _log.LogDebug("No custom town music found.");

                    return true;
                }

                __instance.SetBGMusicAudio(randomAudioFile, 1f, loop: false);

                return false;
            }
        }

        [HarmonyPatch(typeof(SoundManager), "StartBattleMusic")]
        public class SoundManagerStartBattleMusicPatch
        {
            [UsedImplicitly]
            private static bool Prefix(SoundManager __instance, TerrainType terrainType, TimeOfDay timeOfDay)
            {
                _currentBgmType = BgmType.Battle;
                _currentTerrainTimeKey = $"{terrainType}_{timeOfDay}";

                var randomAudioFile = GetRandomBattleMusic();

                if (randomAudioFile == null)
                {
                    _log.LogDebug("No custom battle music found.");

                    return true;
                }

                __instance.SetBGMusicAudio(randomAudioFile, 1f, loop: false);

                return false;
            }
        }

        [HarmonyPatch(typeof(SoundManager), "StartExplorationMusic")]
        public class SoundManagerStartExplorationMusicPatch
        {
            [UsedImplicitly]
            private static bool Prefix(SoundManager __instance, TerrainType terrainType, TimeOfDay timeOfDay)
            {
                _currentBgmType = BgmType.Exploration;
                _currentTerrainTimeKey = $"{terrainType}_{timeOfDay}";

                var randomAudioFile = GetRandomExplorationMusic();

                if (randomAudioFile == null)
                {
                    _log.LogDebug("No custom exploration music found.");

                    return true;
                }

                __instance.SetBGMusicAudio(randomAudioFile, 1f, loop: false);

                return false;
            }
        }

        [HarmonyPatch(typeof(SoundManager), "StartVictoryMusic")]
        public class SoundManagerStartVictoryMusicPatch
        {
            [UsedImplicitly]
            private static bool Prefix(SoundManager __instance)
            {
                _currentBgmType = BgmType.Victory;

                if (!_victoryMusicFiles.Any())
                {
                    _log.LogDebug("No custom victory music found.");

                    return true;
                }

                __instance.SetBGMusicAudio(_victoryMusicFiles.Random(), 0.3f, false);

                __instance.SetAmbientAudio(null);

                return false;
            }
        }

        [HarmonyPatch(typeof(SoundManager), "StartDefeatMusic")]
        public class SoundManagerStartDefeatMusicPatch
        {
            [UsedImplicitly]
            private static bool Prefix(SoundManager __instance)
            {
                _currentBgmType = BgmType.Defeat;

                if (!_defeatMusicFiles.Any())
                {
                    _log.LogDebug("No custom defeat music found.");

                    return true;
                }

                __instance.SetBGMusicAudio(_defeatMusicFiles.Random(), 0.3f, false);
                __instance.SetAmbientAudio(null);

                return false;
            }
        }

        private string GetFolderPath(params string[] paths)
        {
            var defaultPath = new[]
            {
                Paths.PluginPath,
                "CustomMusic"
            };

            return defaultPath.AddRangeToArray(paths).Aggregate(Path.Combine);
        }

        private async Task LoadAllAudioClipsAsync(string path, List<AudioClip> list)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

                return;
            }

            foreach (var file in Directory.GetFiles(path).Where(x => x.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase))
                         .ToList())
            {
                await GetAudioClipAsync(file, list);
            }
        }

        private async Task GetAudioClipAsync(string path, List<AudioClip> list)
        {
            var www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG);

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                _log.LogError($"Failed to load audio file ${path}");
            }
            else
            {
                new Thread(delegate()
                {
                    var audioClip = DownloadHandlerAudioClip.GetContent(www);
                    www.Dispose();

                    var tagLib = TagLib.File.Create(path);
                    audioClip.name =
                        $"{string.Join(", ", tagLib.Tag.Performers)} - {tagLib.Tag.Title} // {tagLib.Properties.Duration:mm':'ss}";
                    tagLib.Dispose();

                    list.Add(audioClip);

                    _log.LogDebug($"Successfully loaded audio file {path}");
                }).Start();
            }
        }

        private static IEnumerator PlayRandomSongNext(AudioSource source, AudioClip currentClip)
        {
            var randomAudioFile = _currentBgmType switch
            {
                BgmType.Town => GetRandomTownMusic(currentClip),
                BgmType.Exploration => GetRandomExplorationMusic(currentClip),
                BgmType.Battle => GetRandomBattleMusic(currentClip),
                _ => null
            };

            if (randomAudioFile == null)
            {
                source.loop = true;

                yield break;
            }

            _log.LogDebug($"Queued audio file {randomAudioFile.name}");

            yield return new WaitForSeconds(currentClip.length);

            AccessTools
                .Method(typeof(SoundManager), "FadeAudio")
                .Invoke(SoundManager.instance, new object[] {source, randomAudioFile, source.volume});
        }

        private static AudioClip GetRandomTownMusic(AudioClip current = null)
        {
            var computedList = _townMusicFiles.Where(x => x != current).ToList();

            return computedList.Any() 
                ? computedList.Random()
                : null;
        }

        private static AudioClip GetRandomBattleMusic(AudioClip current = null)
        {
            var computedList = _battleMusicFiles[_currentTerrainTimeKey].Where(x => x != current).ToList();
            var genericComputedList = _genericBattleMusicFiles.Where(x => x != current).ToList();

            if (computedList.Any() || genericComputedList.Any())
            {
                return computedList.Any()
                    ? computedList.Random()
                    : genericComputedList.Random();
            }

            return null;
        }

        private static AudioClip GetRandomExplorationMusic(AudioClip current = null)
        {
            var computedList = _explorationMusicFiles[_currentTerrainTimeKey].Where(x => x != current).ToList();
            var genericComputedList = _genericExplorationMusicFiles.Where(x => x != current).ToList();

            if (computedList.Any() || genericComputedList.Any())
            {
                return computedList.Any()
                    ? computedList.Random()
                    : genericComputedList.Random();
            }

            return null;
        }
    }
}