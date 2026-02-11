using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Compression;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using static Tigris.Func;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Pak;
using SharpGLTF.Collections;
using System.Reflection.Metadata;
using static System.Windows.Forms.LinkLabel;

namespace Tigris
{
    public class Program
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;
        private static Func _func;
        private static Style _style;
        internal static string replaceFolder = "";
        private static string pendingFolder = null;
        private static List<string> conversionLogs = new List<string>();

        private static string logFilePath;
        private static readonly object logFileLock = new object();
       

        public static void AddConversionLog(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";

            lock (conversionLogs)
            {
                conversionLogs.Add(msg);
                if (conversionLogs.Count > 200)
                    conversionLogs.RemoveAt(0);
            }
            if (!string.IsNullOrEmpty(logFilePath))
            {
                lock (logFileLock)
                {
                    File.AppendAllText(logFilePath, line + Environment.NewLine);
                }
            }
        }

        static void Main(string[] args)
        {
            string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);

            logFilePath = Path.Combine(
                logDir,
                $"ConverterLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
            );

            File.AppendAllText(logFilePath,
                $"=== Converter Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n"
            );
            _func = new Func();
            _style = new Style();
            _style.LoadFont();
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(200, 100, 1400, 900, WindowState.Normal, "Tigris"),
                new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);

            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };

            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            var stopwatch = Stopwatch.StartNew();
            float deltaTime = 0f;

            while (_window.Exists)
            {
                deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _controller.Update(deltaTime, snapshot);

                ImUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(0f, 0f, 0f, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        private static void ImUI()
        {
            string[] exportOptions = new[] { "Export WEM", "Export WAV", "Export WEM and WAV" };
            int currentExportType = (int)_func.ExportType;

            string[] nameOptions = new[] { "Use ID", "Use Short Name" };
            int currentNameType = (int)_func.ExportNameType;

            if (_func.DarkTheme) 
                _style.StyleEarlyEagle();
            else 
                _style.StyleLight();

            var windowSize = new Vector2(_window.Width, _window.Height);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(windowSize);

            if (ImGui.Begin("EAGLE", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text("Game Folder:");

                ImGui.SameLine();

                ImGui.Text(_func.gameDirectory);
                if (_func.provider != null)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(0.18f, 0.72f, 0.23f, 1), "OK");
                }
                ImGui.Separator();

                ImGui.InputText("Search", ref _func.searchQuery, 100);

                var allLanguages = _func.soundItems?.SelectMany(s => s.Subtitles.Keys).Distinct().OrderBy(l => l).ToList() ?? new List<string>();
                if (allLanguages.Count > 0)
                {
                    int currentIndex = allLanguages.IndexOf(_func.currentSubtitleLanguage);
                    if (currentIndex < 0) currentIndex = 0;
                    if (ImGui.Combo("Subtitle Language", ref currentIndex, allLanguages.ToArray(), allLanguages.Count))
                    {
                        _func.currentSubtitleLanguage = allLanguages[currentIndex];
                    }
                }
                if (ImGui.Button("Browse Folder"))
                {
                    var t = new System.Threading.Thread(() =>
                    {
                        using var fbd = new FolderBrowserDialog();
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            string selectedPath = fbd.SelectedPath;
                            string paksPath = _func.GetPaksPath(selectedPath);

                            if (paksPath != null)
                            {
                                _func.gameDirectory = paksPath;
                                string SkornemPak = _func.gameDirectory + "OPP-WindowsClient.pak";
                                try
                                {
                                    _func.provider = new DefaultFileProvider(_func.gameDirectory, SearchOption.TopDirectoryOnly, true, new VersionContainer(EGame.GAME_OutlastTrials));
                                 //   _func.provider.MappingsContainer = new FileUsmapTypeMappingsProvider("Z:\\Dumper-7\\4.27.1-197800+release-4-1-OPP\\Mappings\\4.27.1-197800+release-4-1-OPP.usmap");
                                    _func.provider.Initialize();
                                    _func.provider.SubmitKey(new FGuid(), new FAesKey(_aesKey));
                                    string dllPath = Path.Combine(AppContext.BaseDirectory, "oo2core_9_win64.dll");
                                    //OodleHelper.DownloadOodleDll();
                                    if (!File.Exists(dllPath))
                                    {
                                        OodleHelper.DownloadOodleDll();
                                    }
                                    OodleHelper.Initialize(@"utils\oo2core_9_win64.dll");
                                 //   OodleHelper.Initialize(@"oo2core_9_win64.dll");
                                   
                                    Console.WriteLine($"Provider initialized: {_func.gameDirectory}");

                                   _func.InitLocres();
                                   _func.BuildSoundMapAndList();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error initializing provider: {ex.Message}");
                                    MessageBox.Show($"Error initializing: {ex.Message}",
                                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Could not find a valid Paks folder\n\n" +
                                              "Select a folder that contains:\n" +
                                              ".pak files\n" +
                                              "like The Outlast Trials or OPP",
                                              "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    });
                    t.SetApartmentState(System.Threading.ApartmentState.STA);
                    t.Start();
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(170f);
                if (ImGui.BeginCombo("Export Format", exportOptions[currentExportType]))
                {
                    for (int i = 0; i < exportOptions.Length; i++)
                    {
                        bool isSelected = (currentExportType == i);
                        if (ImGui.Selectable(exportOptions[i], isSelected))
                        {
                            _func.ExportType = (ExportType)i;
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                ImGui.SameLine();

                ImGui.SetNextItemWidth(160f);
                if (ImGui.BeginCombo("File Name", nameOptions[currentNameType]))
                {
                    for (int i = 0; i < nameOptions.Length; i++)
                    {
                        bool isSelected = (currentNameType == i);
                        if (ImGui.Selectable(nameOptions[i], isSelected))
                        {
                            _func.ExportNameType = (ExportNameType)i;
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                ImGui.SameLine();

                if (ImGui.Button("Export Selected"))
                {
                    _func.ExportSelected(_func.ExportType, _func.ExportNameType);
                }
                ImGui.SameLine();
               

                if (ImGui.Button("Export All On Page"))
                {
                    _func.ExportAllFiltered(
                        _func.ExportType,
                        _func.ExportNameType
                    );
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150);
                float masterVol = _func.MasterVolume;
                if (ImGui.SliderFloat("Volume", ref masterVol, 0.0f, 1.0f, "%.2f"))
                {
                    _func.SetMasterVolume(masterVol);
                }
                ImGui.SameLine();

                bool DarkTheme = _func.DarkTheme;
                if (ImGui.Checkbox("Dark Theme", ref DarkTheme))
                    _func.DarkTheme = DarkTheme;

                ImGui.Separator();

                if (_func.soundsLoaded)
                {
                    var lang = _func.soundItems.Select(s => s.Language).Distinct().OrderBy(l => l).ToList();
                    lang.Insert(0, "All");
                    if (ImGui.BeginTabBar("LanguageTabs"))
                    {
                        foreach (var language in lang)
                        {
                            if (ImGui.BeginTabItem(language))
                            {
                                _func.UpdateFilteredSounds(language);
                                _func.RenderSoundList(language);
                                ImGui.EndTabItem();
                            }
                        }
                        ImGui.EndTabBar();
                    }
                }
                
                /*Sound Tabs*/
                 if (_func.soundsLoaded)
                 {
                     if (ImGui.BeginTabBar("LanguageTabs"))
                     {
                         if (ImGui.BeginTabItem("Converter"))
                         {
                             var converterLanguages = _func.soundItems
                             .Select(s => s.Language)
                             .Where(l => !string.IsNullOrEmpty(l))
                             .Distinct()
                             .OrderBy(l => l)
                             .ToList();
                
                             converterLanguages.Insert(0, "All");
                
                             int langIndex = converterLanguages.IndexOf(_func.converterLanguage);
                             if (langIndex < 0) langIndex = 0;
                
                             ImGui.SetNextItemWidth(200);
                             if (ImGui.Combo("Language", ref langIndex,
                                 converterLanguages.ToArray(),
                                 converterLanguages.Count))
                             {
                                 _func.converterLanguage = converterLanguages[langIndex];
                             }
                
                             ImGui.Separator();
                             if (ImGui.Button("WEM Folder"))
                               {
                                   var t = new System.Threading.Thread(() =>
                                   {
                                       using var fbd = new FolderBrowserDialog();
                                       fbd.ShowNewFolderButton = false;
                                       fbd.RootFolder = Environment.SpecialFolder.Desktop;
                
                                       if (fbd.ShowDialog() == DialogResult.OK)
                                       {
                                           pendingFolder = fbd.SelectedPath;
                                       }
                                   });
                                   t.SetApartmentState(System.Threading.ApartmentState.STA);
                                   t.Start();
                               }
                
                             if (!string.IsNullOrEmpty(pendingFolder))
                               {
                                   _func.WemFolder = pendingFolder;
                                   pendingFolder = null;
                
                                   if (Directory.Exists(_func.WemFolder))
                                   {
                                       var wavFiles = Directory.GetFiles(_func.WemFolder, "*.wem");
                                       if (wavFiles.Length > 0)
                                       {
                                           AddConversionLog($"Selected WEM folder: {_func.WemFolder}");
                                           AddConversionLog($"Found {wavFiles.Length} WEM files");
                
                                       }
                                       else
                                       {
                                           AddConversionLog($"No WEM files found in: {_func.WemFolder}");
                                           _func.WemFolder = null;
                                       }
                                   }
                                   else
                                   {
                                       AddConversionLog($"Folder does not exist: {_func.WemFolder}");
                                       _func.WemFolder = null;
                                   }
                               }
                
                             if (_func.WemFolder != null)
                               {
                                   ImGui.SameLine();
                                   ImGui.Text(_func.WemFolder != "" ? _func.WemFolder : "Not set");
                               }
                             else
                             {
                
                                 ImGui.SameLine();
                                 ImGui.Text("Not set");
                             }
                
                             ImGui.Separator();
                             ImGui.Spacing();
                
                             if (ImGui.Button("Convert File To Match Size"))
                             {
                                 _func.ProcessWemFolderToReplace(_func.WemFolder, true, true);
                             }
                             ImGui.SameLine();
                             if (ImGui.Button("Make Mod"))
                             {
                                 Task.Run(() => _func.RunRepakBat());
                             }
                             ImGui.Separator();
                             ImGui.Text("Log:");
                
                             if (ImGui.BeginChild("ConversionLog", new Vector2(0, 500)))
                             {
                                 lock (conversionLogs)
                                 {
                                     foreach (var log in conversionLogs)
                                     {
                                         if (log.StartsWith(""))
                                             ImGui.Text(log);
                                         else
                                             ImGui.TextUnformatted(log);
                                     }
                                 }
                                 ImGui.EndChild();
                             }
                             ImGui.EndTabItem();
                         }
                         ImGui.EndTabBar();
                     }
                  } /*Converter Tab*/
                 // ImGui.ShowDemoWindow();
                ImGui.End();
            }
        }
    }
}