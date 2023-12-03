using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuizmoNET;
using Newtonsoft.Json;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace PlayerSpy.Services;

using CurrentSettings = ValueTuple<PenumbraApiEc, (bool, int, IDictionary<string, IList<string>>, bool)?>;

public readonly record struct Mod(string Name, string DirectoryName) : IComparable<Mod>
{
    public int CompareTo(Mod other)
    {
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(DirectoryName, other.DirectoryName, StringComparison.Ordinal);
    }
}

public readonly record struct ModSettings(IDictionary<string, IList<string>> Settings, int Priority, bool Enabled)
{
    public ModSettings()
        : this(new Dictionary<string, IList<string>>(), 0, false)
    { }

    public static ModSettings Empty
        => new();
}


public class RedrawData
{
    public enum RedrawType
    {
        Redraw,
        AfterGPose,
    }

    public string Name { get; set; } = string.Empty;
    public int ObjectTableIndex { get; set; } = -1;
    public RedrawType Type { get; set; } = RedrawType.Redraw;
}



public unsafe class PenumbraService : IDisposable
{
    public const int RequiredPenumbraBreakingVersion = 4;
    public const int RequiredPenumbraFeatureVersion = 15;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly EventSubscriber<ModSettingChange, string, string, bool> _modSettingChanged;
    private FuncSubscriber<int, int> _cutsceneParent;
    private FuncSubscriber<int, (bool, bool, string)> _objectCollection;
    private FuncSubscriber<IList<(string, string)>> _getMods;
    private FuncSubscriber<ApiCollectionType, string> _currentCollection;
    private FuncSubscriber<string, string, string, bool, CurrentSettings> _getCurrentSettings;
    private FuncSubscriber<string, string, string, bool, PenumbraApiEc> _setMod;
    private FuncSubscriber<string, string, string, int, PenumbraApiEc> _setModPriority;
    private FuncSubscriber<string, string, string, string, string, PenumbraApiEc> _setModSetting;
    private FuncSubscriber<string, string, string, string, IReadOnlyList<string>, PenumbraApiEc> _setModSettings;
    private ActionSubscriber<int, RedrawType> _redrawSubscriber;
    private readonly EventSubscriber _initializedEvent;
    private readonly EventSubscriber _disposedEvent;

    public bool Available { get; private set; }

    public PenumbraService(DalamudPluginInterface pi)
    {
        _pluginInterface = pi;
        _initializedEvent = Ipc.Initialized.Subscriber(pi, Reattach);
        _disposedEvent = Ipc.Disposed.Subscriber(pi, Unattach);
        _modSettingChanged = Ipc.ModSettingChanged.Subscriber(pi);
        Reattach();
    }

    public event Action<ModSettingChange, string, string, bool> ModSettingChanged
    {
        add => _modSettingChanged.Event += value;
        remove => _modSettingChanged.Event -= value;
    }

    public void SetModSetting(Mod mod, string collection, string setting, string value)
    {
        if (Available)
        {
            _setModSetting.Invoke(collection, mod.DirectoryName, mod.Name, setting, value);
        }
    }


    /// <summary> Try to redraw the given actor. </summary>
    /// NOTE: I should probably write this properly, but I don't give a fuck since this isn't for public use anyway /shrug
    public void Redraw()
    {
        if (!Available)
            return;


        var redrawData = new RedrawData()
        {
            Name = Plugin.ClientState?.LocalPlayer?.Name.TextValue,
            Type = RedrawData.RedrawType.Redraw
        };

        string json = JsonConvert.SerializeObject(redrawData);

        using HttpClient client = new HttpClient();
        var buffer = Encoding.UTF8.GetBytes(json);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = client.PostAsync("http://localhost:42069/api/redraw", byteContent);
        response.Wait();
       var responseData = response.GetAwaiter().GetResult();

        if (responseData.IsSuccessStatusCode)
        {
            PluginLog.Information("Redrew character due to status change.");
        } else
        {
            PluginLog.Warning("Could not redraw character.");
        }


    }

    public IReadOnlyList<(Mod Mod, ModSettings Settings)> GetMods()
    {
        if (!Available)
            return Array.Empty<(Mod Mod, ModSettings Settings)>();

        try
        {
            var allMods = _getMods.Invoke();
            var collection = _currentCollection.Invoke(ApiCollectionType.Current);
            return allMods
                .Select(m => (m.Item1, m.Item2, _getCurrentSettings.Invoke(collection, m.Item1, m.Item2, true)))
                .Where(t => t.Item3.Item1 is PenumbraApiEc.Success)
                .Select(t => (new Mod(t.Item2, t.Item1),
                    !t.Item3.Item2.HasValue
                        ? ModSettings.Empty
                        : new ModSettings(t.Item3.Item2!.Value.Item3, t.Item3.Item2!.Value.Item2, t.Item3.Item2!.Value.Item1)))
                .OrderByDescending(p => p.Item2.Enabled)
                .ThenBy(p => p.Item1.Name)
                .ThenBy(p => p.Item1.DirectoryName)
                .ThenByDescending(p => p.Item2.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Error fetching mods from Penumbra:\n{ex}");
            return Array.Empty<(Mod Mod, ModSettings Settings)>();
        }
    }


    /// <summary>
    /// Try to set all mod settings as desired. Only sets when the mod should be enabled.
    /// If it is disabled, ignore all other settings.
    /// </summary>
    public string SetMod(Mod mod, ModSettings settings)
    {
        if (!Available)
            return "Penumbra is not available.";

        var sb = new StringBuilder();
        try
        {
            var collection = _currentCollection.Invoke(ApiCollectionType.Current);
            var ec = _setMod.Invoke(collection, mod.DirectoryName, mod.Name, settings.Enabled);
            if (ec is PenumbraApiEc.ModMissing)
                return $"The mod {mod.Name} [{mod.DirectoryName}] could not be found.";

            Debug.Assert(ec is not PenumbraApiEc.CollectionMissing, "Missing collection should not be possible.");

            if (!settings.Enabled)
                return string.Empty;

            ec = _setModPriority.Invoke(collection, mod.DirectoryName, mod.Name, settings.Priority);
            Debug.Assert(ec is PenumbraApiEc.Success or PenumbraApiEc.NothingChanged, "Setting Priority should not be able to fail.");

            foreach (var (setting, list) in settings.Settings)
            {
                ec = list.Count == 1
                    ? _setModSetting.Invoke(collection, mod.DirectoryName, mod.Name, setting, list[0])
                    : _setModSettings.Invoke(collection, mod.DirectoryName, mod.Name, setting, (IReadOnlyList<string>)list);
                switch (ec)
                {
                    case PenumbraApiEc.OptionGroupMissing:
                        sb.AppendLine($"Could not find the option group {setting} in mod {mod.Name}.");
                        break;
                    case PenumbraApiEc.OptionMissing:
                        sb.AppendLine($"Could not find all desired options in the option group {setting} in mod {mod.Name}.");
                        break;
                }

                Debug.Assert(ec is PenumbraApiEc.Success or PenumbraApiEc.NothingChanged,
                    "Missing Mod or Collection should not be possible here.");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return sb.AppendLine(ex.Message).ToString();
        }
    }

    /// <summary> Obtain the name of the collection currently assigned to the player. </summary>
    public string GetCurrentPlayerCollection()
    {
        if (!Available)
            return string.Empty;

        var (valid, _, name) = _objectCollection.Invoke(0);
        return valid ? name : string.Empty;
    }


    /// <summary> Obtain the parent of a cutscene actor if it is known. </summary>
    public int CutsceneParent(int idx)
        => Available ? _cutsceneParent.Invoke(idx) : -1;



    /// <summary> Reattach to the currently running Penumbra IPC provider. Unattaches before if necessary. </summary>
    public void Reattach()
    {
        try
        {
            Unattach();

            var (breaking, feature) = Ipc.ApiVersions.Subscriber(_pluginInterface).Invoke();
            if (breaking != RequiredPenumbraBreakingVersion || feature < RequiredPenumbraFeatureVersion)
                throw new Exception(
                    $"Invalid Version {breaking}.{feature:D4}, required major Version {RequiredPenumbraBreakingVersion} with feature greater or equal to {RequiredPenumbraFeatureVersion}.");

            _modSettingChanged.Enable();
            _cutsceneParent = Ipc.GetCutsceneParentIndex.Subscriber(_pluginInterface);
            _objectCollection = Ipc.GetCollectionForObject.Subscriber(_pluginInterface);
            _getMods = Ipc.GetMods.Subscriber(_pluginInterface);
            _currentCollection = Ipc.GetCollectionForType.Subscriber(_pluginInterface);
            _getCurrentSettings = Ipc.GetCurrentModSettings.Subscriber(_pluginInterface);
            _setMod = Ipc.TrySetMod.Subscriber(_pluginInterface);
            _setModPriority = Ipc.TrySetModPriority.Subscriber(_pluginInterface);
            _setModSetting = Ipc.TrySetModSetting.Subscriber(_pluginInterface);
            _setModSettings = Ipc.TrySetModSettings.Subscriber(_pluginInterface);
            Available = true;
            PluginLog.Debug("Glamourer attached to Penumbra.");
        }
        catch (Exception e)
        {
            PluginLog.Debug($"Could not attach to Penumbra:\n{e}");
        }
    }

    /// <summary> Unattach from the currently running Penumbra IPC provider. </summary>
    public void Unattach()
    {
        _modSettingChanged.Disable();
        if (Available)
        {
            Available = false;
            PluginLog.Debug("Glamourer detached from Penumbra.");
        }
    }

    public void Dispose()
    {
        Unattach();
        _initializedEvent.Dispose();
        _disposedEvent.Dispose();
        _modSettingChanged.Dispose();
    }
}
