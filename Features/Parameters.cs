#if DEBUG
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MEC;
using PluginAPI.Core;
using ServerSpecificSyncer.Features.Interfaces;
using ServerSpecificSyncer.Features.Wrappers;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace ServerSpecificSyncer.Features
{
    public static class Parameters
    {
        public static TSs GetParameter<TMenu, TSs>(this ReferenceHub hub, int settingId)
            where TMenu : Menu
            where TSs : ServerSpecificSettingBase
        {
            foreach (Menu menu in Menu.Menus.Where(x => x is TMenu))
            {
                if (!menu.SettingsSync.TryGetValue(hub, out List<ServerSpecificSettingBase> settings)) continue;
                
                ServerSpecificSettingBase t = settings.Where(x => x is TSs).FirstOrDefault(x => x.SettingId == settingId);
                return t as TSs;
            }

            return default;
        }


        public static IEnumerator<float> SyncAll(ReferenceHub hub)
        {
            SyncCache.Add(hub, new());
            var sendSettings = new List<ServerSpecificSettingBase>();
            float timeout = 0;
            foreach (var menu in Menu.Menus.ToList())
            {
                Log.Debug($"syncing {hub.nicknameSync.MyNick} registered parameters for menu {menu.Name}");
                foreach (var t in menu.Settings)
                {
                    if (t.ResponseMode != ServerSpecificSettingBase.UserResponseMode.AcquisitionAndChange)
                        continue;
                    ServerSpecificSettingBase @base = null;
                    if (t is ISetting setting)
                        @base = setting.Base;
                    else
                        @base = t;

                    if (@base.SettingId < menu.Hash)
                        @base.SettingId += menu.Hash;

                    sendSettings.Add(@base);
                    
                    Log.Error(@base.SettingId.ToString());
                }
                ServerSpecificSettingsSync.SendToPlayer(hub, sendSettings.ToArray());
                while (SyncCache[hub].Count < sendSettings.Count && timeout < 10)
                {
                    timeout += Time.deltaTime;
                    yield return Timing.WaitForOneFrame;
                }

                if (SyncCache[hub].Count < sendSettings.Count || timeout >= 10)
                {
                    Log.Error($"timeout exceeded to sync value for hub {hub.nicknameSync.MyNick} menu {menu.Name}. Stopping the process.");
                    break;
                }

                foreach (var setting in SyncCache[hub])
                {
                    if (sendSettings.Any(s => s.SettingId == setting.SettingId))
                    {
                        var set = sendSettings.First(s => s.SettingId == setting.SettingId);
                        Log.Debug(set.SettingId.ToString());
                        setting.Label = set.Label;
                        //setting.SettingId -= menu.Hash;
                        setting.HintDescription = set.HintDescription;
                    }
                }
                menu.InternalSettingsSync[hub] = new(SyncCache[hub]);
                SyncCache[hub].Clear();
                sendSettings.Clear();
                Log.Debug($"synced settings for {hub.nicknameSync.MyNick} to the menu {menu.Name}. {menu.InternalSettingsSync[hub].Count} settings have been synced.");
            }
            SyncCache.Remove(hub);
            if (Menu.Menus.Where(x => x.CheckAccess(hub)).IsEmpty())
                yield break;

#if DEBUG
            if (Plugin.StaticConfig.ForceMainMenuEventIfOnlyOne || Menu.Menus.Count(x => x.CheckAccess(hub)) > 1)
                Menu.LoadForPlayer(hub, null);
            else
#endif
                Menu.LoadForPlayer(hub, Menu.Menus.First());
        }
        
        internal static readonly Dictionary<ReferenceHub, List<ServerSpecificSettingBase>> SyncCache = new();

        public static List<ServerSpecificSettingBase> GetAllSyncedParameters(ReferenceHub referenceHub)
        {
            List<ServerSpecificSettingBase> toReturn = new();
            foreach (var menu in Menu.Menus.Where(x => x.InternalSettingsSync.ContainsKey(referenceHub)))
                toReturn.AddRange(menu.InternalSettingsSync[referenceHub]);
            return toReturn;
        }
        
        public static List<ServerSpecificSettingBase> GetMenuSpecificSyncedParameters<T>(ReferenceHub referenceHub) where T : Menu
        {
            List<ServerSpecificSettingBase> toReturn = new();
            foreach (var menu in Menu.Menus.Where(x => x.InternalSettingsSync.ContainsKey(referenceHub) && x is T))
                toReturn.AddRange(menu.InternalSettingsSync[referenceHub]);
            return toReturn;
        }
        
#if DEBUG
        public static void WaitUntilDone(ReferenceHub hub, List<ServerSpecificSettingBase> sendSettings)
        {
            Timing.RunCoroutine(EnumWaitUntilDone(hub, sendSettings));
        }

        private static IEnumerator<float> EnumWaitUntilDone(ReferenceHub hub, List<ServerSpecificSettingBase> sendSettings)
        {
            Menu menu = Menu.TryGetCurrentPlayerMenu(hub);
            if (menu == null)
                yield break;
            
            float ping = 0;
            while (SyncCache[hub].Count < sendSettings.Count && ping <= 10)
            {
                ping += Time.deltaTime;
                yield return Timing.WaitForOneFrame;
            }

            if (ping >= 10 && SyncCache[hub].Count < sendSettings.Count)
            {
                Log.Error($"timeout exceeded to sync value for hub {hub.nicknameSync.MyNick} menu {menu.Name}. Stopping the process.");
                goto finish;
            }
            
            foreach (var setting in SyncCache[hub])
            {
                if (sendSettings.Any(s => s.SettingId == setting.SettingId))
                {
                    var set = sendSettings.First(s => s.SettingId == setting.SettingId);
                    setting.Label = set.Label;
                    setting.HintDescription = set.HintDescription;
                }
            }
            menu.InternalSettingsSync[hub] = new(SyncCache[hub]);
            SyncCache[hub].Clear();
            Log.Debug($"synced settings for {hub.nicknameSync.MyNick} to the menu {menu.Name}. {menu.InternalSettingsSync[hub].Count} settings have been synced.");
finish:
            SyncCache.Remove(hub);
            if (Menu.Menus.Where(x => x.CheckAccess(hub)).IsEmpty())
                yield break;
#if DEBUG
            if (Plugin.StaticConfig.ForceMainMenuEventIfOnlyOne || Menu.Menus.Count(x => x.CheckAccess(hub)) > 1)
                Menu.LoadForPlayer(hub, null);
#endif
            else
                Menu.LoadForPlayer(hub, Menu.Menus.First());
        }
#endif
    }
}
#endif