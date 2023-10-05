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
        private ConfigWindow ConfigWindow { get; init; }

        private Dictionary<string, int> renderStates = new Dictionary<string, int>();


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

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (Configuration.RenderedSettings.Count == 0) return;

            var players = Objects.Where(o => o is PlayerCharacter);
            var mods = penumbraService.GetMods();
            foreach (var setting in Configuration.RenderedSettings)
            {
                if (setting.IsValidSetting() != true) continue;

                var modKvp = mods.FirstOrDefault(x => x.Mod.Name.ToLower() == setting.Mod.ToLower());
                var mod = modKvp.Mod;
                if (setting.IsEnabled != true || mod == null)
                {
                    continue;
                }

                var settingsPlayers = setting.Players.Split(';');
                bool anyPlayerMatches = players.Any(player => settingsPlayers.Any(settingsPlayer => settingsPlayer.Trim() == player.Name.TextValue));

                var stateFound = renderStates.ContainsKey(mod.Name);

                if (!stateFound)
                {
                    renderStates.Add(mod.Name, -1);
                }

                var state = renderStates[mod.Name];

                if (anyPlayerMatches && (state != 0))
                {
                    
                    penumbraService.SetModSetting(mod, setting.Collection, setting.ModOption, setting.RenderedOption);
                    renderStates[mod.Name] = 0;
                } else if (!anyPlayerMatches && state != 1)
                {
                    penumbraService.SetModSetting(mod, setting.Collection, setting.ModOption, setting.NotRenderedOption);
                    renderStates[mod.Name] = 1;
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
