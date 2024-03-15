using System;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Components;
using PlayerSpy;
using PlayerSpy.Data;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Interface.Utility;

namespace PlayerSpy.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    private Plugin _plugin;

    public ConfigWindow(Plugin plugin) : base(
        "Player Spy - Configuration",
         ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(1200, 600);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
        _plugin = plugin;
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

        var settings = new List<RenderedSetting>(Configuration.RenderedSettings);

        if (ImGui.BeginTable("#modsettings", 11, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
        {
            ImGui.TableSetupColumn("#");
            ImGui.TableSetupColumn("Mod Name");
            ImGui.TableSetupColumn("Collection");
            ImGui.TableSetupColumn("Option Name");
            ImGui.TableSetupColumn("Rendered Option");
            ImGui.TableSetupColumn("Unrendered Option");
            ImGui.TableSetupColumn("Players");
            ImGui.TableSetupColumn("Simply Disable Mode");
            ImGui.TableSetupColumn("Priority");
            ImGui.TableSetupColumn("Enabled");
            ImGui.TableSetupColumn("Delete");
            ImGui.TableHeadersRow();

            for (var i = 0; i < settings.Count; i++)
            {
                ImGui.TableNextRow();

                var row = ImGui.TableGetRowIndex() - 1;
                var setting = settings[row];
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(i.ToString());
               

                // Mod
                ImGui.TableSetColumnIndex(1);

                var name = setting.Mod;
                if (ImGui.InputText("##mod" + row, ref name, 128))
                {
                    setting.Mod = name;
                }

                // Collction
                ImGui.TableSetColumnIndex(2);

                var collection = setting.Collection;
                if (ImGui.InputText("##collection" + row, ref collection, 128))
                {
                    setting.Collection = collection;
                }

                // Option
                ImGui.TableSetColumnIndex(3);

                var option = setting.ModOption;
                if (ImGui.InputText("##option" + row, ref option, 128))
                {
                    setting.ModOption = option;
                }


                // Rendered Option
                ImGui.TableSetColumnIndex(4);

                var rendered = setting.RenderedOption;
                if (ImGui.InputText("##renderedOption" + row, ref rendered, 128))
                {
                    setting.RenderedOption = rendered;
                }   



                // Unrendered Option
                ImGui.TableSetColumnIndex(5);

                var unrendered = setting.NotRenderedOption;
                if (ImGui.InputText("##notRenderedOption" + row, ref unrendered, 128))
                {
                    setting.NotRenderedOption = unrendered;
                }

                // Players
                ImGui.TableSetColumnIndex(6);

                var players = setting.Players;
                if (ImGui.InputText("##players" + row, ref players, 512))
                {
                    setting.Players = players;
                }


                ImGui.TableSetColumnIndex(7);
                var isnotrenderedmoddisable = setting.IsNotRenderedModDisabled;
                if (ImGui.Checkbox("##isnotrenderedmoddisable" + row, ref isnotrenderedmoddisable))
                {
                    setting.IsNotRenderedModDisabled = isnotrenderedmoddisable;
                }



                ImGui.TableSetColumnIndex(8);
                var priority = setting.Priority;
                if (ImGui.InputInt("##priority" + row, ref priority))
                {
                    setting.Priority = priority;
                }

                // Enabled
                ImGui.TableSetColumnIndex(9);

                var enabled = setting.IsEnabled;
                if (ImGui.Checkbox("##enabled" + row, ref enabled))
                {
                    setting.IsEnabled = enabled;
                }

                ImGui.TableSetColumnIndex(10);
                if (ImGuiComponents.IconButton("##trashCan" + row, FontAwesomeIcon.Trash))
                {
                    // Temp list to remove entries
                    var newSettings = new List<RenderedSetting>(Configuration.RenderedSettings);
                    newSettings.RemoveAt(row);
                    Configuration.RenderedSettings = newSettings;
                }

            }

            ImGui.EndTable();

            var windowSize = ImGui.GetWindowSize();

            ImGui.SetCursorPos(windowSize - ImGuiHelpers.ScaledVector2(70));

            if (ImGui.BeginChild("###settingsFinishButton"))
            {

                    if (ImGui.Button(FontAwesomeIcon.Save.ToIconString(), new Vector2(40)))
                    {
                        Configuration.RenderedSettings = settings;
                        Configuration.Save();


                        if (!ImGui.IsKeyDown(ImGuiKey.ModShift))
                            IsOpen = false;
                    }
            }
            ImGui.EndChild();

        }

    }
}
