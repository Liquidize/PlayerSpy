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

namespace PlayerSpy
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Player Spy";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("PlayerSpy");
        private PenumbraService penumbraService { get; init; }
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static ObjectTable Objects { get; private set; } = null!;
        private ConfigWindow ConfigWindow { get; init; }

        private Dictionary<string, bool> renderStates = new Dictionary<string, bool>();


        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
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

        private void OnFrameworkUpdate(Framework framework)
        {
            if (Configuration.RenderedSettings.Count == 0) return;

            var players = Objects.Where(o => o is PlayerCharacter);
            var mods = penumbraService.GetMods();
            foreach (var setting in Configuration.RenderedSettings)
            {
                var mod = mods.FirstOrDefault(x => x.Mod.Name.ToLower() == setting.Mod.ToLower()).Mod;

                if (setting.IsEnabled != true || mod == null)
                {
                    continue;
                }

                var settingsPlayers = setting.Players.Split(';');
                bool anyPlayerMatches = players.Any(player => settingsPlayers.Any(settingsPlayer => settingsPlayer.Trim() == player.Name.TextValue));

                var stateFound = renderStates.ContainsKey(mod.Name);
                var state = stateFound ? renderStates[mod.Name] : false;


                if (anyPlayerMatches && (!state || !stateFound))
                {
                    if (!stateFound)
                    {
                        renderStates.Add(mod.Name, true);
                    } else
                    {
                        renderStates[mod.Name] = true;
                    }

                    penumbraService.SetModSetting(mod, setting.Collection, setting.ModOption, setting.RenderedOption);
                } else if (!anyPlayerMatches && (state || !stateFound))
                {
                    if (!stateFound)
                    {
                        renderStates.Add(mod.Name, false);
                    }
                    else
                    {
                        renderStates[mod.Name] = false;
                    }

                    penumbraService.SetModSetting(mod, setting.Collection, setting.ModOption, setting.NotRenderedOption);
                }

            }

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
