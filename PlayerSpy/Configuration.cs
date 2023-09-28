using Dalamud.Configuration;
using Dalamud.Plugin;
using PlayerSpy.Data;
using System;
using System.Collections.Generic;

namespace PlayerSpy
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public List<RenderedSetting> RenderedSettings { get; set; } = new List<RenderedSetting>();

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
