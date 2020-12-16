using HarmonyLib;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RerollArtifact
{
    [HarmonyPatch(typeof(EffectActions), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(string) })]
    class GlobalLuaFunctions
    {
        static void Postfix()
        {
            Traverse.Create(Traverse.Create<EffectActions>().Field("_Instance").GetValue<EffectActions>()).Field("myLuaScript").GetValue<Script>().Globals["RegisterRerollArtifact"] = (Action<string>)Registry.Register;
            Traverse.Create(Traverse.Create<EffectActions>().Field("_Instance").GetValue<EffectActions>()).Field("myLuaScript").GetValue<Script>().Globals["SetBattlesForReroll"] = (Action<int>)Registry.SetBattlesForReroll;
        }
    }
}

