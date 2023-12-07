using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game;
using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using static FFXIVClientStructs.FFXIV.Client.UI.UI3DModule;
using System.Collections.Generic;
using PlayerSpy.Windows;
using PlayerSpy.Services;
using Dalamud.Plugin.Services;
using PlayerSpy.Data;
using ImGuizmoNET;

namespace PlayerSpy
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Player Spy";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("PlayerSpy");
        private PenumbraService penumbraService { get; init; }
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IObjectTable Objects { get; private set; } = null!;

        [PluginService] public static IClientState ClientState { get; private set; } = null;

        private ConfigWindow ConfigWindow { get; init; }

        private Dictionary<string, string> renderStates = new Dictionary<string, string>();

        private DateTime _lastCheckTime;

        public string? CurrentPlayerName()
        {
            return ClientState.LocalPlayer?.Name.TextValue;
        }


        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            penumbraService = new PenumbraService(pluginInterface);
            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);

            Framework.Update += OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        }


        private void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        /// <summary>
        /// TODO: We're already checking if a player is found when pulling the highest priority list, perhaps it is best to send this function
        /// a isPlayerActive propertty or something so we aren't checking twice
        /// </summary>
        /// <param name="framework"></param>
        private void OnFrameworkUpdate(IFramework framework)
        {
            var now = DateTime.UtcNow;
            if (_lastCheckTime > now) return;

            var settings = GetHighestPrioritySettings();
            var mods = penumbraService.GetMods();
            var players = Objects.Where(o => o is PlayerCharacter);

            foreach (var setting in settings)
            {
                if (setting == null || setting.IsValidSetting() != true) continue;

                var modKvp = mods.FirstOrDefault(x => x.Mod.Name.ToLower() == setting.Mod.ToLower());
                var mod = modKvp.Mod;
                var modSettings = modKvp.Settings;
                if (setting.IsEnabled != true || mod == null)
                {
                    continue;
                }


                var stateFound = renderStates.ContainsKey(mod.Name);

                if (!stateFound)
                {
                    renderStates.Add(mod.Name, "");
                }
                var state = renderStates[mod.Name];
                var settingsPlayers = setting.Players.Split(';');
                bool anyPlayerMatches = players.Any(player => settingsPlayers.Any(settingsPlayer => settingsPlayer.Trim() == player.Name.TextValue));
                if (anyPlayerMatches && state != (setting.IsNotRenderedModDisabled ? "true" :setting.RenderedOption))
                {


                    if (!setting.IsNotRenderedModDisabled)
                    {

                        penumbraService.SetModSetting(mod, setting.Collection, setting.ModOption, setting.RenderedOption);
                    }
                    else
                    {
                        penumbraService.SetMod(mod, new ModSettings(modSettings.Settings, modSettings.Priority, true));
                    }
                    penumbraService.Redraw();
                    renderStates[mod.Name] = setting.IsNotRenderedModDisabled ? "true" : setting.RenderedOption;
                } else if (!anyPlayerMatches && state != (setting.IsNotRenderedModDisabled? "false" :setting.NotRenderedOption))
                {
                    if (!setting.IsNotRenderedModDisabled)
                    {

                        penumbraService.SetModSetting(mod, setting.Collection, setting.ModOption, setting.NotRenderedOption);
                    }
                    else
                    {
                        penumbraService.SetMod(mod, new ModSettings(modSettings.Settings, modSettings.Priority, false));
                    }
                    penumbraService.Redraw();
                    renderStates[mod.Name] = setting.IsNotRenderedModDisabled ? "false" : setting.NotRenderedOption;
                }

            }
                _lastCheckTime = now.AddMilliseconds(200);

        }

        private List<RenderedSetting> GetHighestPrioritySettings()
        {
            List<RenderedSetting> list = new List<RenderedSetting> ();

            var dic = Configuration.RenderedSettings.GroupBy(x => x.Mod).ToDictionary(group => group.Key, group => group.ToList());
            var players = Objects.Where(o => o is PlayerCharacter);
            foreach (var kvp in dic)
            {
                var renderSettingsList = kvp.Value;
                var orderedSettings = renderSettingsList.OrderByDescending(x => x.Priority);

                // Select the setting with the highest priority
                var highestPrioritySetting = orderedSettings.FirstOrDefault(x => x.IsEnabled);

                foreach (var setting in orderedSettings)
                {
                    var settingsPlayers = setting.Players.Split(';');
                    bool anyPlayerMatches = players.Any(player => settingsPlayers.Any(settingsPlayer => settingsPlayer.Trim() == player.Name.TextValue));
                    if (anyPlayerMatches && setting.IsEnabled)
                    {
                        highestPrioritySetting = setting; break;
                    }
                }

                list.Add(highestPrioritySetting);

            }

            return list;
        }


        public void Unattach()
        {

        }

        public void Reattach()
        {

        }

        public void Dispose()
        {
            renderStates.Clear();
            this.WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            penumbraService.Dispose();
            Framework.Update -= OnFrameworkUpdate;
        }

    }
}
