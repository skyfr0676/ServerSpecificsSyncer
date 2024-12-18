﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NorthwoodLib.Pools;
using PluginAPI.Core;
using ServerSpecificSyncer.Features;
using UserSettings.ServerSpecific;
using static HarmonyLib.AccessTools;

namespace ServerSpecificSyncer.Patchs.ComptabiliserPatchs;

[HarmonyPatch(typeof(ServerSpecificSettingsSync), nameof(ServerSpecificSettingsSync.SendToPlayer), typeof(ReferenceHub), typeof(ServerSpecificSettingBase[]), typeof(int?))]
public class SendToPlayerPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> transpiler,
        ILGenerator generator)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent();

        Label continueLabel = generator.DefineLabel();
        Label removePlayerLabel = generator.DefineLabel();

        LocalBuilder assembly = generator.DeclareLocal(typeof(Assembly));
        LocalBuilder menu = generator.DeclareLocal(typeof(AssemblyMenu));

        newInstructions.AddRange(new[]
        {
            // Assembly assembly = Assembly.GetCallingAssembly();
            new(OpCodes.Call, Method(typeof(Assembly), nameof(Assembly.GetCallingAssembly))),

            // Menu menu = Features.Utils.GetMenu(assembly);
            new(OpCodes.Stloc_S, assembly.LocalIndex),
            new(OpCodes.Call, Method(typeof(Features.Utils), nameof(Features.Utils.GetMenu))),
            new(OpCodes.Stloc_S, menu.LocalIndex),
            
            // if (menu == null)
            new(OpCodes.Ldloc_S, menu.LocalIndex),
            new(OpCodes.Ldnull),
            new(OpCodes.Brfalse_S, continueLabel),
            
            // [menu == null]
            // Log.Warning($"assembly {assembly.GetName().Name} tried to send a couple of {collection.Length} settings but doesn't have a valid/registered menu! creating new one...");
            new(OpCodes.Ldstr, "assembly {0} tried to send a couple of {1} settings but doesn't have a valid/registered menu! creating new one..."),
            new(OpCodes.Ldloc_S, assembly.LocalIndex),
            new(OpCodes.Callvirt, Method(typeof(Assembly), nameof(Assembly.GetName))),
            new(OpCodes.Callvirt, PropertyGetter(typeof(AssemblyName), nameof(AssemblyName.Name))),
            new(OpCodes.Ldarg_1),
            new(OpCodes.Callvirt, PropertyGetter(typeof(Array), nameof(Array.Length))),
            new(OpCodes.Callvirt, Method(typeof(int), nameof(int.ToString))),
            new(OpCodes.Call, Method(typeof(string),nameof(string.Format))),
            new(OpCodes.Call, Method(typeof(Log), nameof(Log.Warning))),
            
            // Comptabilisater.Load(assembly);
            new(OpCodes.Ldloc_S, assembly.LocalIndex),
            new(OpCodes.Ldarg_1),
            new(OpCodes.Call, Method(typeof(Comptabilisater), nameof(Comptabilisater.Load))),
            
            // [menu != null]
            // if (collection == null) menu.AuthorizedPlayers.Remove(hub);
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(continueLabel),
            new(OpCodes.Ldnull),
            new(OpCodes.Brtrue_S, removePlayerLabel),
            new(OpCodes.Ldloc_S, menu.LocalIndex),
            new(OpCodes.Callvirt, PropertyGetter(typeof(AssemblyMenu), nameof(AssemblyMenu.AuthorizedPlayers))),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Callvirt, Method(typeof(HashSet<ReferenceHub>), nameof(HashSet<ReferenceHub>.Add))),
            new(OpCodes.Ret),
            
            // else menu.AuthorizedPlayers.Add(hub);
            new CodeInstruction(OpCodes.Ldloc_S, menu.LocalIndex).WithLabels(removePlayerLabel),
            new(OpCodes.Callvirt, PropertyGetter(typeof(AssemblyMenu), nameof(AssemblyMenu.AuthorizedPlayers))),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Callvirt, Method(typeof(HashSet<ReferenceHub>), nameof(HashSet<ReferenceHub>.Remove))),
            new(OpCodes.Ret),
        });

        foreach (CodeInstruction z in newInstructions)
            yield return z;

        ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }
}