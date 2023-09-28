using System;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Components;
using OtterGui.Raii;
using PlayerSpy;
using PlayerSpy.Data;

namespace PlayerSpy.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "A Wonderful Configuration Window",
         ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(900, 600);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }


    public override void Draw()
    {
        ImGui.TextUnformatted("Mod Settings");
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "Add a new Mod Setting.\nThese are triggered when the desired player is rendered or not.");
        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();

        if (ImGui.Button("Add New"))
        {
            Configuration.RenderedSettings.Add(new RenderedSetting());
        }

        var settings = Configuration.RenderedSettings;

        if (ImGui.BeginTable("#modsettings", 9, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
        {
            ImGui.TableSetupColumn("#");
            ImGui.TableSetupColumn("Mod Name");
            ImGui.TableSetupColumn("Collection");
            ImGui.TableSetupColumn("Option Name");
            ImGui.TableSetupColumn("Rendered Option");
            ImGui.TableSetupColumn("Unrendered Option");
            ImGui.TableSetupColumn("Players");
            ImGui.TableSetupColumn("Enabled");
            ImGui.TableSetupColumn("Delete");
            ImGui.TableHeadersRow();

            for (var i = 0; i < settings.Count; i++)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(i.ToString());

                // Mod
                ImGui.TableSetColumnIndex(1);

                var name = settings[i].Mod;
                if (ImGui.InputText("##mod", ref name, 128))
                {
                    settings[i].Mod = name;
                }

                // Collction
                ImGui.TableSetColumnIndex(2);

                var collection = settings[i].Collection;
                if (ImGui.InputText("##collection", ref collection, 128))
                {
                    settings[i].Collection = collection;
                }

                // Option
                ImGui.TableSetColumnIndex(3);

                var option = settings[i].ModOption;
                if (ImGui.InputText("##option", ref option, 128))
                {
                    settings[i].ModOption = option;
                }


                // Rendered Option
                ImGui.TableSetColumnIndex(4);

                var rendered = settings[i].RenderedOption;
                if (ImGui.InputText("##renderedOption", ref rendered, 128))
                {
                    settings[i].RenderedOption = rendered;
                }



                // Unrendered Option
                ImGui.TableSetColumnIndex(5);

                var unrendered = settings[i].NotRenderedOption;
                if (ImGui.InputText("##notRenderedOption", ref unrendered, 128))
                {
                    settings[i].NotRenderedOption = unrendered;
                }

                // Players
                ImGui.TableSetColumnIndex(6);

                var players = settings[i].Players;
                if (ImGui.InputText("##players", ref players, 512))
                {
                    settings[i].Players = players;
                }


                // Enabled
                ImGui.TableSetColumnIndex(7);

                var enabled = settings[i].IsEnabled;
                if (ImGui.Checkbox("##enabled", ref enabled))
                {
                    settings[i].IsEnabled = enabled;
                }

                ImGui.TableSetColumnIndex(8);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                {
                    Configuration.RenderedSettings.RemoveAt(i);
                }

            }

            ImGui.EndTable();

            var windowSize = ImGui.GetWindowSize();

            ImGui.SetCursorPos(windowSize - ImGuiHelpers.ScaledVector2(70));

            if (ImGui.BeginChild("###settingsFinishButton"))
            {

                using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 100f))
                {

                    if (ImGui.Button(FontAwesomeIcon.Save.ToIconString(), new Vector2(40)))
                    {
                        Configuration.RenderedSettings = settings;
                        Configuration.Save();

                        if (!ImGui.IsKeyDown(ImGuiKey.ModShift))
                            IsOpen = false;
                    }
                }
            }
            ImGui.EndChild();

        }

    }
}
