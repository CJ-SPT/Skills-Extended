// Minimal external API substitutes for running the production patch bodies without
// starting Unity or an SPT server. Full solution builds validate the real API surface.
using System.Reflection;

public class ObjectInHandsAnimator
{
    public void SetMeleeSpeed(float speed) { }
}

namespace SkillsExtended.Utils
{
    public static class GameUtils
    {
        public static EFT.SkillManager SkillManager;
        public static EFT.SkillManager GetSkillManager() => SkillManager;
        public static bool IsScav() => false;
        public static EFT.Profile GetProfile(EFT.EPlayerSide side) => new();
    }
}

namespace HarmonyLib
{
    public static class AccessTools
    {
        public static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        public static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        public static MethodInfo PropertyGetter(Type type, string name) => type.GetProperty(name)?.GetMethod;
    }
    public class HarmonyAfterAttribute(string[] ids) : Attribute { public string[] Ids { get; } = ids; }
}
namespace SPT.Reflection.Patching
{
    public abstract class ModulePatch
    {
        protected abstract MethodBase GetTargetMethod();
        protected static TestLogger Logger = new();
    }
    public class PatchPrefixAttribute : Attribute;
    public class PatchPostfixAttribute : Attribute;
    public class TestLogger { public void LogDebug(string message) { } }
}
namespace SPTarkov.Reflection.Patching
{
    public abstract class AbstractPatch { protected abstract MethodBase GetTargetMethod(); }
    public class PatchPrefixAttribute : Attribute;
    public class PatchPostfixAttribute : Attribute;
}
namespace SPTarkov.DI.Annotations { public class Injectable : Attribute; }
namespace UnityEngine
{
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Clamp(value, 0, 1);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);
    }
}
namespace SkillsExtended
{
    public static class SkillsExtendedPlugin { public static Config.SkillsConfig SkillData; }
}
namespace JsonType
{
    public sealed class DamageEffectSpecification
    {
        public int Cost, HealthPenaltyMin, HealthPenaltyMax;
        public float Delay, Duration, FadeOut;
    }
}
namespace EFT.HealthSystem
{
    public interface IHealthEffect;
    public interface IBleeding : IHealthEffect;
    public interface IPain : IHealthEffect;
    public interface IIntoxication : IHealthEffect;
    public class EffectsSettings
    {
        public class StimulatorSettings
        {
            public class StimulatorBuffSettings
            {
                public bool IsBuff = true;
                public float Duration, Chance;
                public object Clone() => MemberwiseClone();
            }
            public void GetPersonalBuffSettings() { }
        }
    }
}
namespace EFT.InventoryLogic
{
    public enum EDamageEffectType { LightBleeding, HeavyBleeding, Fracture, DestroyedPart }
    public class MedKitComponent;
    public class Item
    {
        public object Owner;
        public bool IsMedKit = true;
        public T GetItemComponent<T>() where T : class, new() => IsMedKit ? new T() : null;
    }
    public class InventoryController { public InventoryProfile Profile = new(); }
    public class InventoryProfile { public object SkillsInfo; }
    public class HealthEffectsComponent
    {
        public Item Item = new();
        public float UseTime { get; set; }
        public Dictionary<EDamageEffectType, JsonType.DamageEffectSpecification> DamageEffects { get; set; }
    }
}
namespace EFT.Interactive
{
    public class WorldInteractiveObject
    {
        public string Id;
        public string KeyId;
        public bool Destroyed;
        public static implicit operator bool(WorldInteractiveObject value) => value is not null && !value.Destroyed;
        public object InteractingPlayer;
        public void PlaySound() { }
    }
}
namespace EFT
{
    public static class FloatExtensions { public static bool ApproxEquals(this float a, float b) => Math.Abs(a - b) < 0.00001f; }
    [Flags]
    public enum EPhysicalCondition { None = 0, ProneDisabled = 1, ProneMovementDisabled = 2, SprintDisabled = 4, JumpDisabled = 8 }
    public enum ESkillClass { Physical }
    public class BaseSkill { public virtual void OnTrigger(SkillManager.SkillAction skillAction, float val) { } }
    public class Mastering : BaseSkill;
    public class Profile { public object BonusController = new(); }
    public enum EPlayerSide { Savage, Usec }
    public class Skill : BaseSkill
    {
        public ESkillId Id;
        public bool IsEliteLevel;
        public SkillManager SkillManager;
        public Skill() { }
        public Skill(SkillManager manager, ESkillId id, ESkillClass cls, SkillManager.SkillAction[] actions, SkillManager.Buff[] buffs) { Id = id; }
    }
    public class Player
    {
        public class MedsController
        {
            public InventoryLogic.Item Item = new();
            public void Spawn(float animationSpeed, Action callback) { }
        }
        public enum ESpeedLimit { Swamp }
        public bool IsYourPlayer;
        public SkillManager Skills = new();
        public void ExecuteSkill(Action action) => action();
    }
    public class ProneMovePlayerState;
    public class ObstacleCollider
    {
        public EPhysicalCondition ConditionsMask;
        public bool HasSwampSpeedLimit;
    }
    public class MovementContext
    {
        public Player _player;
        public object CurrentState;
        public float CovertMovementVolume => 0.8f;
        public float StateSpeedLimit = 1f;
        public float? SwampLimit;
        public bool Sprint = true;
        public EPhysicalCondition Conditions;
        public List<ObstacleCollider> _enteredObstacles = [];
        public void ClampSpeed() { }
        public void RefreshObstacleRestrictions() { }
        public bool PhysicalConditionIs(EPhysicalCondition condition) => (Conditions & condition) != 0;
        public void SetPhysicalCondition(EPhysicalCondition condition, bool value) => Conditions = value ? Conditions | condition : Conditions & ~condition;
        public void EnableSprint(bool value) => Sprint = value;
        public void AddStateSpeedLimit(float limit, Player.ESpeedLimit type) => SwampLimit = limit;
        public void RemoveStateSpeedLimit(Player.ESpeedLimit type) => SwampLimit = null;
    }
    public partial class SkillManager
    {
        public SkillsExtended.SkillsExtendedManager SkillsExtendedManager;
        public enum EBuffType { Elite }
        public class Buff { public EBuffId Id; public EBuffType BuffType; }
        public class FloatBuff : Buff
        {
            public float Value, PerLevelValue, MaxValue, EliteValue;
            public static implicit operator float(FloatBuff buff) => buff.Value;
            public FloatBuff PerLevel(float value) { PerLevelValue = value; return this; }
            public FloatBuff Max(float value) { MaxValue = value; return this; }
            public FloatBuff Elite(float value) { EliteValue = value; return this; }
            public FloatBuff Default(float value) { Value = value; return this; }
        }
        public class LossBuff : FloatBuff;
        public class BooleanBuff : Buff { public bool Value; }
        public class SkillAction
        {
            public SkillAction Factor(float value) => this;
            public SkillAction Factor(Func<MovementParams, float> factor) => this;
            public void Complete(float value) { }
        }
        public class SkillAction<T> : SkillAction
        {
            public SkillAction<T> Where(Func<T, bool> predicate) => this;
        }
        public class MovementParams { public float Overweight, Fatigue; }
        public class SkillSettings
        {
            public float SprintAction, MovementAction, GainPerFatigueStack;
            public float SprintActionMin, SprintActionMax, MovementActionMin, MovementActionMax, PushUpMin, PushUpMax;
            public float SkillProgress, HealthNegativeEffect, StimulatorNegativeBuff, HydrationRecoveryRate, EnergyRecoveryRate, LowHPDuration, DamageTakenAction;
        }
        public class SettingsData
        {
            public SkillSettings Endurance = new(), Strength = new(), Health = new(), Vitality = new(), Metabolism = new(), Immunity = new(), StressResistance = new();
        }
        public SettingsData Settings = new();
        public Skill ProneMovement = new();
    }
}
