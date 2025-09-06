using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Tigris
{
    public class Style
    {
        internal void LoadFont()
        {
            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.Fonts.Clear();


            //    string fontPath = "C:/Windows/Fonts/arial.ttf";
            string fontPath = Path.Combine(AppContext.BaseDirectory, "utils", "font", "arial_unicode.otf");
            if (File.Exists(fontPath))
            {
                float fontSize = 18.0f;
                unsafe
                {
                    ImFontConfigPtr config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
                    ImFontGlyphRangesBuilderPtr builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
                    builder.BuildRanges(out ImVector ranges);
                    ImFontPtr font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config);
                    config.MergeMode = true;
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesCyrillic());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesJapanese());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesChineseFull());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesGreek());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesKorean());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesDefault());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesThai());
                    font = io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config, io.Fonts.GetGlyphRangesVietnamese());
                }
            }
            else
            {
                io.Fonts.AddFontDefault();
                Console.WriteLine("Font not found");
            }

            io.Fonts.Build();
        }
        internal void StyleLight()
        {
            var style = ImGui.GetStyle();
            style.FrameRounding = 12;
            style.WindowPadding.X = 4; style.WindowPadding.Y = 8;
            style.TabBorderSize = 1;
            style.ScrollbarSize = 10;
            style.WindowBorderSize = 0;
            style.FrameBorderSize = 1;
            style.GrabRounding = 12;
            style.Colors[(int)ImGuiCol.Text] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new System.Numerics.Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            style.Colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.94f, 0.94f, 0.94f, 1.00f);
            style.Colors[(int)ImGuiCol.ChildBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 0.98f);
            style.Colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.30f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.40f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.67f);
            style.Colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4(0.96f, 0.96f, 0.96f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4(0.82f, 0.82f, 0.82f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 0.51f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4(0.86f, 0.86f, 0.86f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new System.Numerics.Vector4(0.98f, 0.98f, 0.98f, 0.53f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new System.Numerics.Vector4(0.69f, 0.69f, 0.69f, 0.80f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new System.Numerics.Vector4(0.49f, 0.49f, 0.49f, 0.80f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new System.Numerics.Vector4(0.49f, 0.49f, 0.49f, 1.00f);
            style.Colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.78f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new System.Numerics.Vector4(0.46f, 0.54f, 0.80f, 0.60f);
            style.Colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.40f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.06f, 0.53f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.Header] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.31f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.80f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.Separator] = new System.Numerics.Vector4(0.39f, 0.39f, 0.39f, 0.62f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = new System.Numerics.Vector4(0.14f, 0.44f, 0.80f, 0.78f);
            style.Colors[(int)ImGuiCol.SeparatorActive] = new System.Numerics.Vector4(0.14f, 0.44f, 0.80f, 1.00f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new System.Numerics.Vector4(0.35f, 0.35f, 0.35f, 0.17f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.67f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.95f);
            style.Colors[(int)ImGuiCol.Tab] = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.Header));
            style.Colors[(int)ImGuiCol.PlotLines] = new System.Numerics.Vector4(0.39f, 0.39f, 0.39f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new System.Numerics.Vector4(1.00f, 0.43f, 0.35f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new System.Numerics.Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new System.Numerics.Vector4(1.00f, 0.45f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.TableHeaderBg] = new System.Numerics.Vector4(0.78f, 0.87f, 0.98f, 1.00f);
            style.Colors[(int)ImGuiCol.TableBorderStrong] = new System.Numerics.Vector4(0.57f, 0.57f, 0.64f, 1.00f);
            style.Colors[(int)ImGuiCol.TableBorderLight] = new System.Numerics.Vector4(0.68f, 0.68f, 0.74f, 1.00f);
            style.Colors[(int)ImGuiCol.TableRowBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt] = new System.Numerics.Vector4(0.30f, 0.30f, 0.30f, 0.09f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.35f);
            style.Colors[(int)ImGuiCol.DragDropTarget] = new System.Numerics.Vector4(0.26f, 0.59f, 0.98f, 0.95f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new System.Numerics.Vector4(0.70f, 0.70f, 0.70f, 0.70f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new System.Numerics.Vector4(0.20f, 0.20f, 0.20f, 0.20f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new System.Numerics.Vector4(0.20f, 0.20f, 0.20f, 0.35f);
            style.Colors[(int)ImGuiCol.TabHovered] = style.Colors[(int)ImGuiCol.HeaderHovered];
        }
        internal void StyleEarlyEagle()
        {
            var style = ImGui.GetStyle();
            style.FrameRounding = 12;
            style.WindowPadding.X = 4; style.WindowPadding.Y = 8;
            style.TabBorderSize = 1;
            style.ScrollbarSize = 10;
            style.WindowBorderSize = 0;
            style.FrameBorderSize = 1;
            style.GrabRounding = 12;
            Vector4 yellow = new Vector4(0.75f, 0.62f, 0.23f, 1.00f);
            Vector4 yellowHover = new Vector4(0.65f, 0.62f, 0.23f, 0.80f);
            Vector4 yellowActive = new Vector4(0.80f, 0.65f, 0.10f, 1.00f);
            Vector4 yellowTransparent = new Vector4(0.95f, 0.75f, 0.15f, 0.40f);
            style.Colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new System.Numerics.Vector4(0.50f, 0.50f, 0.50f, 1.00f);
            style.Colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.10f, 0.10f, 0.10f, 1.00f);
            style.Colors[(int)ImGuiCol.ChildBg] = new System.Numerics.Vector4(0.15f, 0.15f, 0.15f, 0.00f);
            style.Colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.12f, 0.12f, 0.12f, 0.94f);
            style.Colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0.35f, 0.35f, 0.35f, 0.50f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.20f, 0.20f, 0.20f, 0.54f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4(0.30f, 0.30f, 0.30f, 0.40f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4(0.35f, 0.35f, 0.35f, 0.67f);
            style.Colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4(0.08f, 0.08f, 0.08f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4(0.12f, 0.12f, 0.12f, 1.00f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new System.Numerics.Vector4(0.08f, 0.08f, 0.08f, 0.51f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4(0.14f, 0.14f, 0.14f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new System.Numerics.Vector4(0.15f, 0.15f, 0.15f, 0.53f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new System.Numerics.Vector4(0.40f, 0.40f, 0.40f, 0.80f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = yellowTransparent;
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = yellow;
            style.Colors[(int)ImGuiCol.CheckMark] = yellow;
            style.Colors[(int)ImGuiCol.SliderGrab] = yellow;
            style.Colors[(int)ImGuiCol.SliderGrabActive] = yellowActive;
            style.Colors[(int)ImGuiCol.Button] = yellowTransparent;
            style.Colors[(int)ImGuiCol.ButtonHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.ButtonActive] = yellowActive;
            style.Colors[(int)ImGuiCol.Header] = yellowTransparent;
            style.Colors[(int)ImGuiCol.HeaderHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.HeaderActive] = yellowActive;
            style.Colors[(int)ImGuiCol.Separator] = new System.Numerics.Vector4(0.43f, 0.43f, 0.50f, 0.50f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.SeparatorActive] = yellowActive;
            style.Colors[(int)ImGuiCol.ResizeGrip] = yellowTransparent;
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.ResizeGripActive] = yellowActive;
            style.Colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.18f, 0.18f, 0.18f, 0.86f);
            style.Colors[(int)ImGuiCol.TabHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.TabSelected] = yellowHover;
            style.Colors[(int)ImGuiCol.PlotLines] = new System.Numerics.Vector4(0.61f, 0.61f, 0.61f, 1.00f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.PlotHistogram] = yellow;
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = yellowHover;
            style.Colors[(int)ImGuiCol.TableHeaderBg] = new System.Numerics.Vector4(0.19f, 0.19f, 0.20f, 1.00f);
            style.Colors[(int)ImGuiCol.TableBorderStrong] = new System.Numerics.Vector4(0.31f, 0.31f, 0.35f, 1.00f);
            style.Colors[(int)ImGuiCol.TableBorderLight] = new System.Numerics.Vector4(0.23f, 0.23f, 0.25f, 1.00f);
            style.Colors[(int)ImGuiCol.TableRowBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 0.06f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = yellowTransparent;
            style.Colors[(int)ImGuiCol.DragDropTarget] = new System.Numerics.Vector4(1.00f, 0.85f, 0.00f, 0.90f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new System.Numerics.Vector4(0.80f, 0.80f, 0.80f, 0.20f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
        }
    }
}
