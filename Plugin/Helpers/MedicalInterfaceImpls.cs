﻿using EFT.HealthSystem;
using EFT.InventoryLogic;
using System.Collections.Generic;

namespace SkillsExtended.Helpers
{
    public sealed class MedKitValues : IMedkitResource
    {
        public int MaxHpResource { set; get; }
        public float HpResourceRate { set; get; }
    }

    public sealed class HealthEffectValues : IHealthEffect
    {
        public float UseTime { set; get; }

        public KeyValuePair<EBodyPart, float>[] BodyPartTimeMults { set; get; }

        public Dictionary<EHealthFactorType, GClass1236> HealthEffects { set; get; }

        public Dictionary<EDamageEffectType, GClass1235> DamageEffects { set; get; }

        public string StimulatorBuffs { set; get; }
    }
}