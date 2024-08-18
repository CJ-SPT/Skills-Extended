using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EFT;
using Newtonsoft.Json;
using SkillsExtended.Buffs;
using SkillsExtended.Models;
using UnityEngine;

namespace SkillsExtended.Controllers;

public class BuffController : MonoBehaviour
{
    private static Dictionary<AbstractBuff, Coroutine> _activeBuffs = [];
    public static BuffsModel Buffs { get; set; }

    public BuffController()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var path = Path.Combine(directory, "Buffs.json");
        var text = File.ReadAllText(path);
        Buffs = JsonConvert.DeserializeObject<BuffsModel>(text);
        Plugin.Log.LogWarning("Buffs loaded from disk.");
    }
    
    public static void ApplyBuff(AbstractBuff buff)
    {
        var routine = StaticManager.BeginCoroutine(StartBuff(buff));
        
        _activeBuffs.Add(buff, routine);
    }

    private static IEnumerator StartBuff(AbstractBuff buff)
    {
        buff.ApplyBuff();
        
        yield return new WaitUntil(() => buff.IsExpired);

        buff.RemoveBuff();
    }
}