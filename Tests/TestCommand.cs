#if DEBUG
using System;
using CommandSystem;
using PluginAPI.Core;
using ServerSpecificSyncer.Features;
using ServerSpecificSyncer.Features.Wrappers;
using UserSettings.ServerSpecific;

namespace ServerSpecificSyncer.Tests
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class TestCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            ReferenceHub hub = Player.Get(sender).ReferenceHub;
            foreach (ServerSpecificSettingBase mrc in Parameters.GetMenuSpecificSyncedParameters<Test>(hub))
                sender.Respond($"PING: {mrc.Label} ({mrc.SettingId}) => {mrc.DebugValue}");
            if (!arguments.IsEmpty())
            {
                ServerSpecificSettingBase get = hub.GetParameter<Test, ServerSpecificSettingBase>(int.Parse(arguments.At(0)));
                if (get.Equals(default))
                {
                    response = "null";
                    return false;
                }
                
                response = $"PING: {get.Label} ({get.SettingId}) => {get.DebugValue}";
                return true;
            }
            response = "ok";
            return true;
        }

        public string Command => "get_parameter";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "[DEBUG COMMAND] get one of yours synced parameters.";
    }
}
#endif