using CUE4Parse;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using ImGuiNET;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tigris
{
public class Func
    {
        internal string wwisePath = "Z:\\Wwise2019.1.11.7296";
        internal string wwiseProjectPath = "Z:\\Wwise2019.1.11.7296\\Sample";
        internal string wavInputPath = "";
        private ConfigManager _configManager;
        private float _masterVolume = 1.0f;
        public ExportType exportType = ExportType.Wem;
        public ExportNameType exportNameType = ExportNameType.Id;

        internal bool soundsLoaded = false;
        internal bool ConsoleLog = false;
        internal readonly object gameDirectoryLock = new object();
        internal string activeLanguageTab = "All";
        internal readonly List<SoundItem> soundItems = new();
        internal DefaultFileProvider provider;

        internal string currentSubtitleLanguage = "en";
        internal const string _aesKey = "0x613E92E0F3CE880FC652EC86254E2581126AE86D63BA46550FB2CE0EC2EDA439";
        //internal const string _targetFolder = "OPP/Content/WwiseAudio";
        //internal const string _targetFolderLocres = "OPP/Content/Localization";
        internal const string _replaceDirectory = "utils/repak/put-ur-files-here";
        internal const string _targetFolder = "OPP/Content/WwiseAudio/Windows";
        internal const string _targetFolderMedia = "OPP/Content/WwiseAudio/Media";
        internal const string _targetFolderLocres = "OPP/Content/Localization";
        internal string searchQuery = "";
        internal bool showWindow = true;

        internal string lastSearchQuery = "";
        internal string lastLanguageTab = "";
        internal List<SoundItem> filteredSoundsCache = new();
        //   internal readonly Dictionary<string, WaveOutEvent> playingSounds = new Dictionary<string, WaveOutEvent>();
        internal string converterLanguage = "All";
        internal readonly Dictionary<string, PlayingSound> playingSounds = new Dictionary<string, PlayingSound>();
        internal string currentPlayingKey = null;

        internal string projectPath = "Z:\\Wwise2019.1.11.7296\\Sample";


        // internal WwiseConverterWrapper wwiseConverter = new WwiseConverterWrapper();
        internal Action<string> append_conversion_log;

        public event Action<string, string> StatusUpdated;
        public event Action<int> ProgressUpdated;

        public Func()
        {
            _configManager = new ConfigManager();

            _masterVolume = _configManager.Config.Volume;

            gameDirectory = _configManager.Config.GameDirectory;



            append_conversion_log = Program.AddConversionLog;

            if (!string.IsNullOrEmpty(gameDirectory))
            {
                if (IsValidPaksStructure(gameDirectory))
                {
                    Console.WriteLine("Auto-initializing from saved Paks path...");
                    if (TryAutoInitialize())
                    {
                        Console.WriteLine("Auto-initialization successful");
                    }
                    else
                    {
                        Console.WriteLine("Auto-initialization failed");
                    }
                }
                else
                {
                    string paksPath = GetPaksPath(gameDirectory);
                    if (paksPath != null)
                    {
                        gameDirectory = paksPath;
                        Console.WriteLine($"Auto corrected path: {gameDirectory}");

                        if (TryAutoInitialize())
                        {
                            Console.WriteLine("Auto initialization success");
                        }
                    }
                }
            }
        }
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Math.Clamp(value, 0.0f, 1.0f);
                _configManager.Config.Volume = _masterVolume;
                _configManager.SaveConfig();
                UpdateAllVolumes();
            }
        }
        public string gameDirectory
        {
            get => _configManager.Config.GameDirectory;
            set
            {
                _configManager.Config.GameDirectory = value;
                _configManager.SaveConfig();
            }
        }
        public string WemFolder
        {
            get => _configManager.Config.WemFolder;
            set
            {
                _configManager.Config.WemFolder = value;
                _configManager.SaveConfig();
            }
        }
        public bool exportAsWav
        {
            get => _configManager.Config.ExportAsWav;
            set
            {
                _configManager.Config.ExportAsWav = value;
                _configManager.SaveConfig();
            }
        }
        public bool DarkTheme
        {
            get => _configManager.Config.DarkTheme;
            set
            {
                _configManager.Config.DarkTheme = value;
                _configManager.SaveConfig();
            }
        }
        public ExportType ExportType
        {
            get => _configManager.Config.ExportType;
            set
            {
                _configManager.Config.ExportType = value;
                _configManager.SaveConfig();
            }
        }
        public ExportNameType ExportNameType
        {
            get => _configManager.Config.ExportNameType;
            set
            {
                _configManager.Config.ExportNameType = value;
                _configManager.SaveConfig();
            }
        }
        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
        }
        /* private void UpdateAllVolumes()
         {
             float finalVolume = _masterVolume;

             foreach (var waveOut in playingSounds)
             {
                 try
                 {
                     if (waveOut != null)
                     {
                         waveOut.Volume = finalVolume;
                     }
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"Error volume: {ex.Message}");
                 }
             }
         } */
        private void UpdateAllVolumes()
        {
            float finalVolume = _masterVolume;

            foreach (var ps in playingSounds.Values)
            {
                try
                {
                    ps.Output.Volume = finalVolume;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error volume: {ex.Message}");
                }
            }
        }

        internal void UpdateFilteredSounds(string language)
        {
            Console.WriteLine($"UpdateFilteredSounds: language='{language}', soundItems={soundItems.Count}");

            if (language != lastLanguageTab || searchQuery != lastSearchQuery)
            {
                Console.WriteLine($"  Conditions changed, updating cache...");

                filteredSoundsCache = soundItems
                    .Where(s =>
                        (language == "All" || string.Equals(s.Language, language, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(searchQuery) || MatchesSearch(s, searchQuery)))
                    .ToList();

                lastSearchQuery = searchQuery;
                lastLanguageTab = language;

                Console.WriteLine($"  Filtered to {filteredSoundsCache.Count} sounds");

                for (int i = 0; i < Math.Min(3, filteredSoundsCache.Count); i++)
                {
                    var sound = filteredSoundsCache[i];
                    Console.WriteLine($"    {i}: {sound.DisplayName} ({sound.Language})");
                }
            }
        }
        internal bool MatchesSearch(SoundItem sound, string query)
        {
            if (sound.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            string soundId = Path.GetFileNameWithoutExtension(sound.FilePath);
            if (soundId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            foreach (var subtitle in sound.Subtitles.Values)
            {
                if (subtitle.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
        internal void RenderSoundList(string language)
        {
            int itemsCount = filteredSoundsCache.Count;

            ImGui.BeginChild("SoundList", new System.Numerics.Vector2(0, 0));

            if (itemsCount == 0)
            {
                if (soundItems.Count == 0)
                {
                    ImGui.Text("No sounds loaded. Please select a valid game folder.");
                }
                else if (!string.IsNullOrEmpty(searchQuery))
                {
                    ImGui.Text($"No sounds found matching '{searchQuery}' in {language}");
                }
                else
                {
                    ImGui.Text($"No sounds found for language: {language}");
                    ImGui.Spacing();
                    ImGui.Text("Available languages:");
                    var availableLangs = soundItems.Select(s => s.Language).Distinct().OrderBy(l => l);
                    foreach (var lang in availableLangs)
                    {
                        ImGui.Text($"  - {lang}");
                    }
                }
                ImGui.EndChild();
                return;
            }

            var style = ImGui.GetStyle();
            float lineH = ImGui.GetTextLineHeightWithSpacing();
            int maxSubtitleLines = 2;
            float rowHeight = lineH * maxSubtitleLines + style.CellPadding.Y * 2f;

            if (ImGui.BeginTable("SoundTable", 6,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings))
            {
                ImGui.TableSetupColumn("Play", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Short Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Language", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Subtitle", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();
                unsafe
                {
                    var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                    clipper.Begin(itemsCount, rowHeight);
                    while (clipper.Step())
                    {
                        for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        {
                            var sound = filteredSoundsCache[i];
                            string uniqueId = $"{language}_{sound.DisplayName}_{i}";
                            string soundId = Path.GetFileNameWithoutExtension(sound.FilePath);

                            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);


                            ImGui.TableSetColumnIndex(0);
                            bool isPlaying = playingSounds.ContainsKey(sound.DisplayName);
                            bool isCurrent = currentPlayingKey == sound.DisplayName;

                            playingSounds.TryGetValue(sound.DisplayName, out var playing);

                            if (!isCurrent)
                            {
                                if (ImGui.Button($"Play##{uniqueId}"))
                                    Task.Run(() => PlaySound(sound.FilePath, sound.DisplayName));
                            }
                            else if (playing != null)
                            {
                                if (playing.State == PlaybackStateEx.Playing)
                                {
                                    if (ImGui.Button($"Pause##{uniqueId}"))
                                        PauseSound(sound.DisplayName);

                                    ImGui.SameLine();
                                    if (ImGui.Button($"Stop##{uniqueId}"))
                                        StopSound(sound.DisplayName);
                                }
                                else // Paused
                                {
                                    if (ImGui.Button($"Resume##{uniqueId}"))
                                        ResumeSound(sound.DisplayName);

                                    ImGui.SameLine();
                                    if (ImGui.Button($"Stop##{uniqueId}"))
                                        StopSound(sound.DisplayName);
                                }
                            }


                            //if (ImGui.Button($"{(isPlaying ? "Stop" : "Play")}##btn_{uniqueId}"))
                            //{
                            //     if (isPlaying)
                            //         StopSound(sound.DisplayName);
                            //     else
                            //         Task.Run(() => PlaySound(sound.FilePath, sound.DisplayName));
                            //}
                            //if (ImGui.Button($"{(isPlaying ? "Stop" : "Play")}##btn_{uniqueId}"))
                            //{
                            //    if (isPlaying) StopSound(sound.DisplayName);
                            //    else Task.Run(() => PlaySound(sound.FilePath, sound.DisplayName));
                            //}

                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(soundId);
                            if (ImGui.BeginPopupContextItem($"id_ctx_{soundId}"))
                            {
                                if (ImGui.MenuItem("Copy ID"))
                                    ImGui.SetClipboardText(soundId);
                                ImGui.EndPopup();
                            }

                            ImGui.TableSetColumnIndex(2);
                            bool selected = sound.Selected;
                            if (ImGui.Checkbox($"##chk_{uniqueId}", ref selected))
                                sound.Selected = selected;
                            ImGui.SameLine();
                            ImGui.Text(sound.DisplayName);
                            if (ImGui.BeginPopupContextItem($"id_ctx_{sound.DisplayName}"))
                            {
                                if (ImGui.MenuItem("Copy Short Name"))
                                    ImGui.SetClipboardText(sound.DisplayName);
                                ImGui.EndPopup();
                            }
                            if (playingSounds.TryGetValue(sound.DisplayName, out var ps))
                            {
                                var reader = ps.Reader;

                                float current = (float)reader.CurrentTime.TotalSeconds;
                                float total = (float)reader.TotalTime.TotalSeconds;

                                if (total > 0)
                                {
                                    float progress = current / total;

                                    ImGui.SetNextItemWidth(200);
                                    if (ImGui.SliderFloat($"##seek_{uniqueId}", ref progress, 0f, 1f, ""))
                                    {
                                        reader.CurrentTime = TimeSpan.FromSeconds(progress * total);
                                    }

                                    ImGui.SameLine();
                                    ImGui.Text($"{reader.CurrentTime:mm\\:ss} / {reader.TotalTime:mm\\:ss}");
                                }
                            }
                            ImGui.TableSetColumnIndex(3);
                            ImGui.Text(sound.FormattedSize);

                            ImGui.TableSetColumnIndex(4);
                            ImGui.Text(sound.Language);

                            ImGui.TableSetColumnIndex(5);
                            if (sound.Subtitles.TryGetValue(currentSubtitleLanguage, out var subtitle) && !string.IsNullOrEmpty(subtitle))
                            {
                                float wrapWidth = ImGui.GetColumnWidth();
                                var size = ImGui.CalcTextSize(subtitle, false, wrapWidth);

                                if (size.Y <= lineH * maxSubtitleLines)
                                {
                                    ImGui.TextWrapped(subtitle);
                                }
                                else
                                {
                                    string shown = subtitle;
                                    int cutPos = subtitle.Length;
                                    while (cutPos > 0)
                                    {
                                        string probe = subtitle.Substring(0, cutPos) + "…";
                                        var s = ImGui.CalcTextSize(probe, false, wrapWidth);
                                        if (s.Y <= lineH * maxSubtitleLines)
                                        {
                                            shown = probe;
                                            break;
                                        }
                                        cutPos -= 10;
                                    }
                                    ImGui.TextWrapped(shown);
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 40);
                                        ImGui.TextUnformatted(subtitle);
                                        ImGui.PopTextWrapPos();
                                        ImGui.EndTooltip();
                                    }


                                }
                            }
                            else
                            {
                                ImGui.Text("");
                            }
                            if (ImGui.BeginPopupContextItem($"id_ctx_{subtitle}"))
                            {
                                if (ImGui.MenuItem("Copy Subtitle"))
                                {
                                    ImGui.SetClipboardText(subtitle);
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }

                    clipper.End();
                    ImGuiNative.ImGuiListClipper_destroy(clipper.NativePtr);
                }

                ImGui.EndTable();
            }

            ImGui.EndChild();
        }

        internal void ExportSelected(ExportType exportType, ExportNameType nameType)
        {
            foreach (var sound in soundItems.Where(s => s.Selected))
            {
                try
                {
                    var fileData = provider.SaveAsset(provider.Files[sound.FilePath]);

                    string fileId = Path.GetFileNameWithoutExtension(sound.FilePath);

                    string fileName = nameType == ExportNameType.Id ?
                        fileId :
                        GetSafeFileName(sound.DisplayName);

                    string exportDir = "Export";
                    Directory.CreateDirectory(exportDir);

                    switch (exportType)
                    {
                        case ExportType.Wem:
                            string wemPath = Path.Combine(exportDir, fileName + ".wem");
                            File.WriteAllBytes(wemPath, fileData);
                            Console.WriteLine($"Exported WEM: {fileName}");
                            break;

                        case ExportType.Wav:
                            string wavPath = Path.Combine(exportDir, fileName + ".wav");
                            string tempWem = Path.GetTempFileName() + ".wem";
                            File.WriteAllBytes(tempWem, fileData);

                            var proc = new Process();
                            proc.StartInfo.FileName = Path.Combine("utils", "vgm", "vgmstream-cli.exe");
                            proc.StartInfo.Arguments = $"\"{tempWem}\" -o \"{wavPath}\"";
                            proc.StartInfo.CreateNoWindow = true;
                            proc.StartInfo.UseShellExecute = false;
                            proc.Start();
                            proc.WaitForExit();


                            try { File.Delete(tempWem); } catch { }

                            Console.WriteLine($"Exported WAV: {fileName}");
                            break;

                        case ExportType.WemAndWav:

                            string wemPath2 = Path.Combine(exportDir, fileName + ".wem");
                            File.WriteAllBytes(wemPath2, fileData);
                            Console.WriteLine($"Exported WEM: {fileName}");


                            string wavPath2 = Path.Combine(exportDir, fileName + ".wav");
                            string tempWem2 = Path.GetTempFileName() + ".wem";
                            File.WriteAllBytes(tempWem2, fileData);

                            var proc2 = new Process();
                            proc2.StartInfo.FileName = Path.Combine("utils", "vgm", "vgmstream-cli.exe");
                            proc2.StartInfo.Arguments = $"\"{tempWem2}\" -o \"{wavPath2}\"";
                            proc2.StartInfo.CreateNoWindow = true;
                            proc2.StartInfo.UseShellExecute = false;
                            proc2.Start();
                            proc2.WaitForExit();


                            try { File.Delete(tempWem2); } catch { }

                            Console.WriteLine($"Exported WAV: {fileName}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error exporting {sound.DisplayName}: {ex.Message}");
                }
            }
        }

        internal void ExportAllFiltered(ExportType exportType, ExportNameType nameType)
        {
            if (filteredSoundsCache == null || filteredSoundsCache.Count == 0)
                return;

            var previousSelection = soundItems
                .Where(s => s.Selected)
                .ToHashSet();
            try
            {
                foreach (var sound in soundItems)
                    sound.Selected = false;
                foreach (var sound in filteredSoundsCache)
                    sound.Selected = true;
                ExportSelected(exportType, nameType);
            }
            finally
            {
                foreach (var sound in soundItems)
                    sound.Selected = previousSelection.Contains(sound);
            }
        }

        private string GetSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
        }
        /*internal void PlaySound(string filePath, string displayName)
        {
            if (provider.Files.TryGetValue(filePath, out var file))
            {
                var wemData = provider.SaveAsset(file);
                string tempWem = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
                File.WriteAllBytes(tempWem, wemData);

                string tempWav = Path.ChangeExtension(tempWem, ".wav");
                var proc = new Process();
                proc.StartInfo.FileName = Path.Combine("utils", "vgm", "vgmstream-cli.exe");
                proc.StartInfo.Arguments = $"\"{tempWem}\" -o \"{tempWav}\"";
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
                proc.WaitForExit();

                var outputDevice = new WaveOutEvent();

                outputDevice.Volume = _masterVolume;

                var reader = new MediaFoundationReader(tempWav);
                outputDevice.Init(reader);
                outputDevice.Play();

                playingSounds[displayName] = outputDevice;

                outputDevice.PlaybackStopped += (s, e) =>
                {
                    reader.Dispose();
                    outputDevice.Dispose();
                    playingSounds.Remove(displayName);
                    try
                    {
                        if (File.Exists(tempWem)) File.Delete(tempWem);
                        if (File.Exists(tempWav)) File.Delete(tempWav);
                    }
                    catch { }
                };
            }
        } */
        internal void PlaySound(string filePath, string displayName)
        {
            if (currentPlayingKey != null)
            {
                StopSound(currentPlayingKey);
            }

            if (!provider.Files.TryGetValue(filePath, out var file))
                return;

            var wemData = provider.SaveAsset(file);

            string tempWem = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
            File.WriteAllBytes(tempWem, wemData);

            string tempWav = Path.ChangeExtension(tempWem, ".wav");

            var proc = new Process
            {
                StartInfo =
        {
            FileName = Path.Combine("utils", "vgm", "vgmstream-cli.exe"),
            Arguments = $"\"{tempWem}\" -o \"{tempWav}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        }
            };
            proc.Start();
            proc.WaitForExit();

            var reader = new MediaFoundationReader(tempWav);
            var output = new WaveOutEvent
            {
                Volume = _masterVolume
            };

            output.Init(reader);
            output.Play();

            var ps = new PlayingSound
            {
                Output = output,
                Reader = reader,
                State = PlaybackStateEx.Playing
            };

            playingSounds[displayName] = ps;
            currentPlayingKey = displayName;

            output.PlaybackStopped += (s, e) =>
            {
                reader.Dispose();
                output.Dispose();
                playingSounds.Remove(displayName);

                if (currentPlayingKey == displayName)
                    currentPlayingKey = null;

                try
                {
                    File.Delete(tempWem);
                    File.Delete(tempWav);
                }
                catch { }
            };
        }


        //   internal void StopSound(string displayName)
        //   {
        //       if (playingSounds.TryGetValue(displayName, out var outputDevice))
        //           outputDevice.Stop();
        //   }
        internal void StopSound(string displayName)
        {
            if (playingSounds.TryGetValue(displayName, out var ps))
            {
                ps.Output.Stop();
                ps.State = PlaybackStateEx.Playing;
            }
        }

        internal void PauseSound(string displayName)
        {
            if (playingSounds.TryGetValue(displayName, out var ps) &&
                ps.State == PlaybackStateEx.Playing)
            {
                ps.Output.Pause();
                ps.State = PlaybackStateEx.Paused;
            }
        }
        internal void ResumeSound(string displayName)
        {
            if (playingSounds.TryGetValue(displayName, out var ps) &&
                ps.State == PlaybackStateEx.Paused)
            {
                ps.Output.Play();
                ps.State = PlaybackStateEx.Playing;
            }
        }
        internal Dictionary<string, Dictionary<string, string>> LoadAllSubtitles()
        {
            var allSubtitles = new Dictionary<string, Dictionary<string, string>>();
            var exeDir = AppContext.BaseDirectory;
            var locBase = Path.Combine(exeDir, "LOC");

            if (!Directory.Exists(locBase))
                return allSubtitles;

            foreach (var jsonFile in Directory.GetFiles(locBase, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    string langFolder = new DirectoryInfo(Path.GetDirectoryName(jsonFile)).Name;
                    if (!allSubtitles.ContainsKey(langFolder))
                        allSubtitles[langFolder] = new Dictionary<string, string>();

                    string json = File.ReadAllText(jsonFile, Encoding.UTF8);
                    var jo = JObject.Parse(json);

                    if (jo.TryGetValue("Subtitles", out var subObj) && subObj is JObject subDict)
                    {
                        foreach (var prop in subDict.Properties())
                        {
                            string key = prop.Name;
                            string value = prop.Value.ToString();
                            if (!allSubtitles[langFolder].ContainsKey(key))
                                allSubtitles[langFolder][key] = value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading {jsonFile}: {ex.Message}");
                }
            }

            return allSubtitles;
        }
        

        internal void BuildSoundMapAndList()
        {
            soundItems.Clear();
            var uniqueSounds = new Dictionary<string, SoundItem>();
            var soundMap = new Dictionary<string, string>();
            var languageMap = new Dictionary<string, string>();

            Console.WriteLine("=== BuildSoundMapAndList started ===");

            var allSubtitles = LoadAllSubtitles();
            Console.WriteLine($"Loaded subtitles: {allSubtitles.Count} languages");

            var jsonFiles = provider.Files
               .Where(f => f.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                          f.Key.Contains("/Windows/"))
               .ToList();

            Console.WriteLine($"Found {jsonFiles.Count} JSON files in Windows folders");

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    var jsonData = provider.SaveAsset(jsonFile.Value);
                    if (jsonData != null && jsonData.Length > 0)
                    {
                        string jsonContent;
                        if (jsonData.Length >= 3 && jsonData[0] == 0xEF && jsonData[1] == 0xBB && jsonData[2] == 0xBF)
                        {
                            jsonContent = Encoding.UTF8.GetString(jsonData, 3, jsonData.Length - 3);
                        }
                        else
                        {
                            jsonContent = Encoding.UTF8.GetString(jsonData);
                        }

                        string language = "Unknown";
                        var parts = jsonFile.Key.Split('/');
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (parts[i].Equals("Windows", StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length)
                            {
                                language = parts[i + 1];
                                break;
                            }
                        }

                        Console.WriteLine($"Parsing JSON: {Path.GetFileName(jsonFile.Key)} (Language: {language})");
                        ParseJsonForSoundNames(jsonContent, soundMap, languageMap, language);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing JSON {jsonFile.Key}: {ex.Message}");
                }
            }

            Console.WriteLine($"Sound map size: {soundMap.Count} entries");

            int foundFiles = 0;

            var wemFilesByLanguage = new Dictionary<string, List<(string Id, string Path)>>();


            wemFilesByLanguage["English(US)"] = new List<(string, string)>();
            wemFilesByLanguage["Francais"] = new List<(string, string)>();
            wemFilesByLanguage["SFX"] = new List<(string, string)>();

            foreach (var file in provider.Files)
            {
                if (file.Key.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file.Key);

                    if (file.Key.Contains("/Windows/Media/English(US)/"))
                    {
                        wemFilesByLanguage["English(US)"].Add((fileName, file.Key));
                    }
                    else if (file.Key.Contains("/Windows/Media/Francais/"))
                    {
                        wemFilesByLanguage["Francais"].Add((fileName, file.Key));
                    }
                    else if (file.Key.Contains("/Windows/Media/"))
                    {
                        wemFilesByLanguage["SFX"].Add((fileName, file.Key));
                    }
                }
            }

            Console.WriteLine($"WEM files in English(US): {wemFilesByLanguage["English(US)"].Count}");
            Console.WriteLine($"WEM files in Francais: {wemFilesByLanguage["Francais"].Count}");

            foreach (var language in wemFilesByLanguage.Keys)
            { 
               foreach (var (fileId, filePath) in wemFilesByLanguage[language])
               {
                   try
                   {
                       if (provider.Files.TryGetValue(filePath, out var fileEntry))
                       {
                           string displayName = fileId;
            
                           if (soundMap.TryGetValue(fileId, out var shortName))
                           {
                               displayName = shortName;
                           }

                            long fileSize = fileEntry.Size;
                            string formattedSize = FormatFileSize(fileSize);

                            var soundItem = new SoundItem
                            {
                                DisplayName = displayName,
                                FilePath = filePath,
                                Language = language,
                                Size = fileSize,
                                FormattedSize = formattedSize
                            };
                            foreach (var langPair in allSubtitles)
                            {
                                if (langPair.Value.TryGetValue(soundItem.DisplayName, out var subText))
                                    soundItem.Subtitles[langPair.Key] = subText;
                            }

                            string uniqueKey = $"{fileId}_{language}";

                            if (!uniqueSounds.ContainsKey(uniqueKey))
                            {
                                uniqueSounds[uniqueKey] = soundItem;
                                foundFiles++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {filePath}: {ex.Message}");
                    }
                }
            }

            soundItems.Clear();
            soundItems.AddRange(uniqueSounds.Values);
            soundsLoaded = true;

            Console.WriteLine($"=== Summary ===");
            Console.WriteLine($"Total sounds loaded: {soundItems.Count}");

            var langGroups = soundItems.GroupBy(s => s.Language)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count);

            foreach (var group in langGroups)
            {
                Console.WriteLine($"  {group.Language}: {group.Count} sounds");
            }
        }
        internal string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B"; 
            }
            else if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024} KB";
            }
            else if (bytes < 1024 * 1024 * 1024)
            {
                double mb = bytes / (1024.0 * 1024.0);
                return mb % 1 == 0 ? $"{mb:F0} MB" : $"{mb:F1} MB";
            }
            else
            {
                double gb = bytes / (1024.0 * 1024.0 * 1024.0);
                return gb % 1 == 0 ? $"{gb:F0} GB" : $"{gb:F1} GB";
            }
        }
        internal static void ParseJsonForSoundNames(string jsonContent, Dictionary<string, string> soundMap,
    Dictionary<string, string> languageMap, string defaultLanguage = "Unknown")
        {
            try
            {
                Console.WriteLine($"Parsing JSON, length: {jsonContent.Length}");

                using var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("SoundBanksInfo", out var soundBanksInfo))
                {
                    Console.WriteLine("Found SoundBanksInfo");

                    if (soundBanksInfo.TryGetProperty("SoundBanks", out var soundBanks))
                    {
                        Console.WriteLine($"Found {soundBanks.GetArrayLength()} sound banks");
                        int totalMediaFiles = 0;

                        foreach (var soundBank in soundBanks.EnumerateArray())
                        {
                            string bankLanguage = defaultLanguage;

                            if (soundBank.TryGetProperty("Language", out var languageElement))
                            {
                                string lang = languageElement.GetString();
                                if (!string.IsNullOrEmpty(lang))
                                {
                                    bankLanguage = lang;
                                    Console.WriteLine($"Bank language: {bankLanguage}");
                                }
                            }

                            if (soundBank.TryGetProperty("Media", out var mediaArray))
                            {
                                int mediaCount = mediaArray.GetArrayLength();
                                Console.WriteLine($"Found 'Media' array with {mediaCount} entries");

                                int parsedCount = ParseMediaArray(mediaArray, soundMap, languageMap, bankLanguage);
                                totalMediaFiles += parsedCount;
                                Console.WriteLine($"  Parsed {parsedCount} media files from this bank");
                            }
                            else
                            {
                                Console.WriteLine("No 'Media' array found in this bank");

                                string[] possibleArrays = { "MediaFiles", "IncludedMemoryFiles", "ExcludedMemoryFiles", "Files" };
                                foreach (var propName in possibleArrays)
                                {
                                    if (soundBank.TryGetProperty(propName, out var altArray))
                                    {
                                        Console.WriteLine($"Found '{propName}' array with {altArray.GetArrayLength()} entries");
                                        int parsedCount = ParseMediaArray(altArray, soundMap, languageMap, bankLanguage);
                                        totalMediaFiles += parsedCount;
                                    }
                                }
                            }
                        }

                        Console.WriteLine($"Total media files parsed from JSON: {totalMediaFiles}");
                    }
                    else
                    {
                        Console.WriteLine("No 'SoundBanks' property in SoundBanksInfo");
                    }
                }
                else
                {
                    Console.WriteLine("No SoundBanksInfo property found in JSON");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
                if (jsonContent.Length > 200)
                {
                    Console.WriteLine($"First 200 chars: {jsonContent.Substring(0, 200)}...");
                }
            }
        }

        private static int ParseMediaArray(JsonElement mediaArray, Dictionary<string, string> soundMap,
            Dictionary<string, string> languageMap, string defaultLanguage)
        {
            int parsedCount = 0;
            int displayedCount = 0;

            foreach (var mediaItem in mediaArray.EnumerateArray())
            {
                try
                {
                    string fileId = "";
                    string shortName = "";
                    string language = defaultLanguage;

                    if (mediaItem.TryGetProperty("Id", out var idElement))
                    {
                        if (idElement.ValueKind == JsonValueKind.String)
                            fileId = idElement.GetString() ?? "";
                        else if (idElement.ValueKind == JsonValueKind.Number)
                            fileId = idElement.GetInt64().ToString();
                    }

                    if (mediaItem.TryGetProperty("ShortName", out var nameElement))
                        shortName = nameElement.GetString() ?? "";


                    if (mediaItem.TryGetProperty("Language", out var langElement))
                    {
                        string lang = langElement.GetString();
                        if (!string.IsNullOrEmpty(lang))
                            language = lang;
                    }


                    if (!string.IsNullOrEmpty(fileId) && !string.IsNullOrEmpty(shortName))
                    {
                        string cleanName = shortName;
                        if (cleanName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                            cleanName = cleanName.Substring(0, cleanName.Length - 4);


                        if (cleanName.EndsWith(".wem", StringComparison.OrdinalIgnoreCase))
                            cleanName = cleanName.Substring(0, cleanName.Length - 4);


                        if (!soundMap.ContainsKey(fileId))
                        {
                            soundMap[fileId] = cleanName;
                            languageMap[fileId] = language;
                            parsedCount++;

                            if (displayedCount < 5)
                            {
                                Console.WriteLine($"  Media: {fileId} -> {cleanName} ({language})");
                                displayedCount++;
                            }
                        }
                        else
                        {
                            if (language != "Unknown" && languageMap[fileId] == "Unknown")
                            {
                                soundMap[fileId] = cleanName;
                                languageMap[fileId] = language;
                                Console.WriteLine($"  Updated: {fileId} -> {cleanName} ({language})");
                            }
                        }
                    }
                    else
                    {
                        if (displayedCount < 3)
                        {
                            Console.WriteLine($"  Skipping media item - missing Id or ShortName");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing media item: {ex.Message}");
                }
            }

            if (parsedCount > 5)
            {
                Console.WriteLine($"  ... and {parsedCount - 5} more media files");
            }

            return parsedCount;
        }
        internal void InitLocres()
        {
            var exeDir = AppContext.BaseDirectory;
            var baseOutPath = Path.Combine(exeDir, "LOC");
            Directory.CreateDirectory(baseOutPath);
            var filesToList = provider.Files
                .Where(f => f.Key.StartsWith(_targetFolderLocres, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Value)
                .ToList();
            //   foreach (var f in provider.Files.Values)
            foreach (var f in filesToList)
            {
                if (f.Name.EndsWith(".locres", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = f.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var langFolder = parts.Length >= 5 ? parts[4] : "unknown";
                    var outPath = Path.Combine(baseOutPath, langFolder);
                    Directory.CreateDirectory(outPath);

                    var fileName = Path.GetFileNameWithoutExtension(f.Name) + ".json";
                    var targetFile = Path.Combine(outPath, fileName);

                    bool needExport = true;

                    //    f.TryCreateReader(out var archive);
                    if (File.Exists(targetFile))
                    {
                        var archive = f.CreateReader();
                        var locres = new FTextLocalizationResource(archive);
                        var locJson = JsonConvert.SerializeObject(locres, Formatting.Indented);
                        var newHash = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create()
                            .ComputeHash(Encoding.UTF8.GetBytes(locJson)));
                        var oldHash = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create()
                            .ComputeHash(File.ReadAllBytes(targetFile)));
                    
                        if (newHash == oldHash)
                            needExport = false;
                    }
                    
                    if (needExport)
                    {
                        var archive = f.CreateReader();
                        var locres = new FTextLocalizationResource(archive);
                        var locJson = JsonConvert.SerializeObject(locres, Formatting.Indented);
                        File.WriteAllText(targetFile, locJson);
                        Console.WriteLine($"Exported {langFolder}/{fileName}");
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {langFolder}/{fileName}");
                    }
                }
            }
        }
        internal void RunRepakBat()
        {
            Process process = null;
            try
            {
                string exeDir = AppContext.BaseDirectory;
                string repakExePath = Path.Combine(exeDir, "utils", "repak", "repak.exe");

                Console.WriteLine($"Looking for repak.exe at: {repakExePath}");

                if (!File.Exists(repakExePath))
                {
                    Console.WriteLine($"repak.exe not found: {repakExePath}");

                    string[] possiblePaths = {
                Path.Combine(exeDir, "utils", "repak", "repak.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "utils", "repak", "repak.exe"),
                Path.Combine("utils", "repak", "repak.exe"),
                "utils\\repak\\repak.exe"
            };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            repakExePath = path;
                            Console.WriteLine($"Found repak.exe at: {repakExePath}");
                            break;
                        }
                    }

                    if (!File.Exists(repakExePath))
                    {
                        Console.WriteLine("repak.exe not found in any of the searched locations");
                        return;
                    }
                }

                string scriptDir = Path.GetDirectoryName(repakExePath);
                string inputDir = Path.Combine(scriptDir, "put-ur-files-here");
                string outputDir = Path.Combine(gameDirectory);
                //          string outputDir = Path.Combine(scriptDir, "output-modifiedpak");
                string outputFile = Path.Combine(gameDirectory, "TigrisAudio_P.pak");
                //     string outputFile = Path.Combine(outputDir, "YourMod_P.pak");

                if (!Directory.Exists(inputDir))
                {
                    Directory.CreateDirectory(inputDir);
                    Console.WriteLine($"Created input directory: {inputDir}");
                }

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"Created output directory: {outputDir}");
                }
                string arguments = $"pack --compression Zlib --version V11 \"{inputDir}\" \"{outputFile}\"";
                Program.AddConversionLog($"Running repak with arguments: {arguments}");
                Console.WriteLine($"Running repak with arguments: {arguments}");

                process = new Process();
                process.StartInfo.FileName = repakExePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = scriptDir;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = false;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Program.AddConversionLog($"REPAK: {e.Data}");
                        Console.WriteLine($"REPAK: {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Program.AddConversionLog($"REPAK ERROR: {e.Data}");
                        Console.WriteLine($"REPAK ERROR: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                Console.WriteLine($"Repak process completed with exit code: {process.ExitCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running repak: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            finally
            {
                if (process != null && !process.HasExited)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error killing process: {ex.Message}");
                    }
                }
                if (process != null)
                {
                    process.Dispose();
                }
            }
        } 
        public string GetPaksPath(string selectedPath)
        {
            if (string.IsNullOrEmpty(selectedPath) || !Directory.Exists(selectedPath))
                return null;


            selectedPath = selectedPath.TrimEnd('\\', '/');

            string currentDir = selectedPath;

            while (!string.IsNullOrEmpty(currentDir))
            {
                if (Path.GetFileName(currentDir)?.Equals("Paks", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (IsValidPaksStructure(currentDir))
                    {
                        return currentDir; 
                    }
                }

                string potentialPaksPath = Path.Combine(currentDir, "Paks");
                if (Directory.Exists(potentialPaksPath) && IsValidPaksStructure(potentialPaksPath))
                {
                    return potentialPaksPath;
                }

                string fullPaksPath = Path.Combine(currentDir, "OPP", "Content", "Paks");
                if (Directory.Exists(fullPaksPath) && IsValidPaksStructure(fullPaksPath))
                {
                    return fullPaksPath;
                }

                string[] possiblePaksPaths = {
                Path.Combine(currentDir, "Content", "Paks", "OPP-WindowsClient.pak"),
                Path.Combine(currentDir, "OPP", "Paks", "OPP-WindowsClient.pak"),
                Path.Combine(currentDir, "The Outlast Trials", "OPP", "Content", "Paks", "OPP-WindowsClient.pak")
            };

                foreach (var path in possiblePaksPaths)
                {
                    if (Directory.Exists(path) && IsValidPaksStructure(path))
                    {
                        return path;
                    }
                }

                string parentDir = Path.GetDirectoryName(currentDir);
                if (parentDir == currentDir)
                    break;

                currentDir = parentDir;
            }

            return null;
        }
        private bool IsValidPaksStructure(string paksPath)
        {
            if (string.IsNullOrEmpty(paksPath) || !Directory.Exists(paksPath))
                return false;

            if (Path.GetFileName(paksPath)?.Equals("Paks", StringComparison.OrdinalIgnoreCase) != true)
                return false;

            var pakFiles = Directory.GetFiles(paksPath, "*.pak");
            if (pakFiles.Length == 0)
                return false;

            return pakFiles.Length > 0;
        }
        public bool IsValidGameDirectory(string path)
        {
            return GetPaksPath(path) != null;
        }
        public bool TryAutoInitialize()
        {
            if (string.IsNullOrEmpty(gameDirectory) || !IsValidPaksStructure(gameDirectory))
                return false;

            try
            {
                provider = new DefaultFileProvider(gameDirectory, SearchOption.TopDirectoryOnly, true,
                    new VersionContainer(EGame.GAME_OutlastTrials));
                provider.Initialize();
                provider.SubmitKey(new FGuid(), new FAesKey(_aesKey));
                string dllPath = Path.Combine(AppContext.BaseDirectory, "oo2core_9_win64.dll");
                if (!File.Exists(dllPath))
                {
                    OodleHelper.DownloadOodleDll();
                }
                OodleHelper.Initialize(@"utils/oo2core_9_win64.dll");

                Console.WriteLine("Provider auto-initialized successfully with Paks path");

                InitLocres();
                BuildSoundMapAndList();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auto-initialization failed: {ex.Message}");
                provider = null;
                return false;
            }
        }
        internal void ConvertWavFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                Program.AddConversionLog("Replace folder not found.");
                return;
            }


            var wavFiles = Directory.GetFiles(folderPath, "*.wav");
            if (wavFiles.Length == 0)
            {
                Program.AddConversionLog("No WAV files found in folder.");
                return;
            }


            var filesByName = new Dictionary<string, string>();
            var filesById = new Dictionary<string, string>();

            foreach (var wavFile in wavFiles)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(wavFile);
                filesByName[fileNameWithoutExt] = wavFile;

                if (long.TryParse(fileNameWithoutExt, out _))
                {
                    filesById[fileNameWithoutExt] = wavFile;
                }
            }

            int convertedCount = 0;
            int skippedCount = 0;

            foreach (var sound in soundItems)
            {
                string soundId = Path.GetFileNameWithoutExtension(sound.FilePath);
                string soundName = sound.DisplayName;

                string wavFile = null;


                if (filesById.TryGetValue(soundId, out wavFile))
                { }
                else if (filesByName.TryGetValue(soundName, out wavFile))
                { }
                else
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    string targetPath = Path.Combine(_replaceDirectory, sound.FilePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    string tempWem = Path.GetTempFileName() + ".wem";

                    var proc = new Process();
                    proc.StartInfo.FileName = Path.Combine("utils", "vgm", "vgmstream-cli.exe");
                    proc.StartInfo.Arguments = $"\"{wavFile}\" -o \"{tempWem}\"";
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.Start();
                    proc.WaitForExit();

                    if (File.Exists(tempWem))
                    {
                        File.Copy(tempWem, targetPath, true);
                        File.Delete(tempWem);
                        Program.AddConversionLog($"{Path.GetFileName(wavFile)} -> {Path.GetFileName(sound.FilePath)}");
                        convertedCount++;
                    }
                    else
                    {
                        Program.AddConversionLog($"Failed: {Path.GetFileName(wavFile)}");
                    }
                }
                catch (Exception ex)
                {
                    Program.AddConversionLog($"Error {Path.GetFileName(wavFile)}: {ex.Message}");
                }
            }

            Program.AddConversionLog($"---- Conversion finished ----");
            Program.AddConversionLog($"Converted: {convertedCount}, Skipped: {skippedCount}, Total WAVs: {wavFiles.Length}");
        }
        
        private string GetBaseFileName(string fileName)
        {
            int lastUnderscoreIndex = fileName.LastIndexOf('_');
            if (lastUnderscoreIndex > 0 && lastUnderscoreIndex < fileName.Length - 1)
            {
                string potentialGuid = fileName.Substring(lastUnderscoreIndex + 1);

                if (potentialGuid.Length == 8 && IsHexString(potentialGuid))
                {
                    return fileName.Substring(0, lastUnderscoreIndex);
                }
            }
            return fileName;
        }
        private bool IsHexString(string input)
        {
            foreach (char c in input)
            {
                if (!Uri.IsHexDigit(c))
                {
                    return false;
                }
            }
            return true;
        }
        internal void ProcessWemFolderToReplace(string wemFolderPath, bool matchById = false, bool matchByName = true, bool adjustSizes = true)
        {
            if (string.IsNullOrEmpty(wemFolderPath) || !Directory.Exists(wemFolderPath))
            {
                Program.AddConversionLog("WEM folder not found or path is invalid");
                return;
            }

            Program.AddConversionLog($"Language filter: {converterLanguage}");
            var wemFiles = Directory.GetFiles(wemFolderPath, "*.wem");
            if (wemFiles.Length == 0)
            {
                Program.AddConversionLog("No WEM files found in the folder");
                return;
            }

            Program.AddConversionLog($"Processing {wemFiles.Length} WEM files from: {wemFolderPath}");
            if (adjustSizes)
            {
                Program.AddConversionLog("Size adjustment: ENABLED");
            }

            int processedCount = 0;
            int copiedCount = 0;
            int adjustedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var filesById = new Dictionary<string, string>();
            var filesByName = new Dictionary<string, string>();
            var filesByBaseName = new Dictionary<string, string>();

            foreach (var wemFile in wemFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(wemFile);
                string baseFileName = GetBaseFileName(fileName);

                if (matchById && long.TryParse(fileName, out _))
                {
                    filesById[fileName] = wemFile;
                }

                if (matchByName)
                {
                    filesByName[fileName] = wemFile;
                }

                filesByBaseName[baseFileName] = wemFile;
            }

            string tempProcessingFolder = Path.Combine(Path.GetTempPath(), "WemProcessing");
            Directory.CreateDirectory(tempProcessingFolder);

            try
            {
                foreach (var sound in GetConverterSounds())
                {
                    try
                    {
                        string soundId = Path.GetFileNameWithoutExtension(sound.FilePath);
                        string soundName = sound.DisplayName;
                        long targetSize = sound.Size;

                        string sourceWemFile = null;
                        string foundBy = "";

                        if (matchById && filesById.TryGetValue(soundId, out sourceWemFile))
                        {
                            foundBy = $"ID: {soundId}";
                        }
                        else if (matchByName && filesByName.TryGetValue(soundName, out sourceWemFile))
                        {
                            foundBy = $"full name: {soundName}";
                        }
                        else if (filesByBaseName.TryGetValue(soundName, out sourceWemFile))
                        {
                            foundBy = $"base name: {soundName}";
                        }
                        else
                        {
                            skippedCount++;
                            Program.AddConversionLog($"Skipped: {FormatSoundLog(sound)} - no matching WEM file found");
                            continue;
                        }

                        Program.AddConversionLog($"Found by ID: {FormatSoundLog(sound)}");

                        string tempFile = Path.Combine(tempProcessingFolder, Path.GetFileName(sourceWemFile));
                        File.Copy(sourceWemFile, tempFile, true);

                        if (adjustSizes)
                        {
                            var fileInfo = new FileInfo(tempFile);
                            long currentSize = fileInfo.Length;

                            if (currentSize != targetSize)
                            {
                                if (currentSize > targetSize)
                                {
                                    Program.AddConversionLog($"File {Path.GetFileName(sourceWemFile)} is larger ({currentSize} bytes) than target ({targetSize} bytes). Size won't be reduced.");
                                    errorCount++;
                                    skippedCount++;
                                }
                                else
                                {
                                    long bytesToAdd = targetSize - currentSize;
                                    using (var fileStream = new FileStream(tempFile, FileMode.Append, FileAccess.Write))
                                    {
                                        byte[] zeroBytes = new byte[bytesToAdd];
                                        fileStream.Write(zeroBytes, 0, zeroBytes.Length);
                                    }
                                    Program.AddConversionLog($"Adjusted: {Path.GetFileName(sourceWemFile)} from {currentSize} to {targetSize} bytes (+{bytesToAdd} bytes)");
                                    adjustedCount++;
                                }
                            }
                        }

                        string relativePath = ExtractMediaRelativePath(sound.FilePath);
                        string targetPath = Path.Combine(_replaceDirectory, "OPP/Content/WwiseAudio/Windows/Media/", relativePath);
                        string targetDirectory = Path.GetDirectoryName(targetPath);

                        Directory.CreateDirectory(targetDirectory);

                        File.Copy(tempFile, targetPath, true);

                        Program.AddConversionLog($"Copied: {Path.GetFileName(sourceWemFile)} -> {relativePath}");
                        copiedCount++;
                        processedCount++;

                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        Program.AddConversionLog($"Error processing file for {sound.DisplayName}: {ex.Message}");
                        errorCount++;
                    }
                }
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempProcessingFolder))
                    {
                        Directory.Delete(tempProcessingFolder, true);
                    }
                }
                catch { }
            }

            Program.AddConversionLog($"---- Processing completed ----");
            Program.AddConversionLog($"Processed: {processedCount}");
            Program.AddConversionLog($"Copied: {copiedCount}");
            if (adjustSizes)
            {
                Program.AddConversionLog($"Adjusted: {adjustedCount}");
            }
            Program.AddConversionLog($"Skipped: {skippedCount}");
            Program.AddConversionLog($"Errors: {errorCount}");
        }
        private string ExtractMediaRelativePath(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals("Media", StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length)
                {
                    return string.Join("/", parts.Skip(i + 1));
                }
            }

            return Path.GetFileName(fullPath);
        }
        internal void AdjustWemSizesInReplaceFolder(bool matchById = true, bool matchByName = false)
        {
            string replaceMediaPath = Path.Combine(_replaceDirectory, "OPP/Content/WwiseAudio/Media");

            if (!Directory.Exists(replaceMediaPath))
            {
                Program.AddConversionLog("Replace directory not found");
                return;
            }

            Program.AddConversionLog($"Language filter: {converterLanguage}");
            var wemFiles = Directory.GetFiles(replaceMediaPath, "*.wem", SearchOption.AllDirectories);
            if (wemFiles.Length == 0)
            {
                Program.AddConversionLog("No WEM files found in replace directory");
                return;
            }

            int adjustedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var filesById = new Dictionary<string, string>();
            var filesByName = new Dictionary<string, string>();
            var filesByBaseName = new Dictionary<string, string>();

            foreach (var wemFile in wemFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(wemFile);
                string baseFileName = GetBaseFileName(fileName);

                if (matchById && long.TryParse(fileName, out _))
                {
                    filesById[fileName] = wemFile;
                }

                if (matchByName)
                {
                    filesByName[fileName] = wemFile;
                }

                filesByBaseName[baseFileName] = wemFile;
            }

            foreach (var sound in GetConverterSounds())
            {
                try
                {
                    string soundId = Path.GetFileNameWithoutExtension(sound.FilePath);
                    string soundName = sound.DisplayName;
                    long targetSize = sound.Size;

                    string wemFileToAdjust = null;

                    if (matchById && filesById.TryGetValue(soundId, out wemFileToAdjust))
                    {
                        Program.AddConversionLog($"Found by ID: {FormatSoundLog(sound)}");
                    }
                    else if (matchByName && filesByName.TryGetValue(soundName, out wemFileToAdjust))
                    {
                        Program.AddConversionLog($"Found by name: {soundName}");
                    }
                    else if (filesByBaseName.TryGetValue(soundName, out wemFileToAdjust))
                    {
                        Program.AddConversionLog($"Found by base name: {soundName}");
                    }
                    else
                    {
                        skippedCount++;
                        continue;
                    }

                    var fileInfo = new FileInfo(wemFileToAdjust);
                    long currentSize = fileInfo.Length;

                    if (currentSize == targetSize)
                    {
                        Program.AddConversionLog($"Size already matches: {Path.GetFileName(wemFileToAdjust)}");
                        skippedCount++;
                        continue;
                    }

                    if (currentSize > targetSize)
                    {
                        Program.AddConversionLog($"File {Path.GetFileName(wemFileToAdjust)} is larger ({currentSize} bytes) than target ({targetSize} bytes). Cannot reduce size.");
                        errorCount++;
                        continue;
                    }

                    long bytesToAdd = targetSize - currentSize;
                    using (var fileStream = new FileStream(wemFileToAdjust, FileMode.Append, FileAccess.Write))
                    {
                        byte[] zeroBytes = new byte[bytesToAdd];
                        fileStream.Write(zeroBytes, 0, zeroBytes.Length);
                    }

                    Program.AddConversionLog($"Adjusted: {FormatSoundLog(sound)} from {currentSize} to {targetSize} bytes");
                    adjustedCount++;
                }
                catch (Exception ex)
                {
                    Program.AddConversionLog($"Error adjusting file for {FormatSoundLog(sound)}: {ex.Message}");
                    errorCount++;
                }
            }

            Program.AddConversionLog($"---- Size adjustment completed ----");
            Program.AddConversionLog($"Adjusted: {adjustedCount}");
            Program.AddConversionLog($"Skipped: {skippedCount}");
            Program.AddConversionLog($"Errors: {errorCount}");
        }
        internal IEnumerable<SoundItem> GetConverterSounds()
        {
            if (converterLanguage == "All")
                return soundItems;

            return soundItems.Where(s => s.Language == converterLanguage);
        }
        private string FormatSoundLog(SoundItem sound)
        {
            string id = Path.GetFileNameWithoutExtension(sound.FilePath);
            return $"{id} ({sound.DisplayName})";
        }

    }
}