using System.Collections;
using EFT;
using EFT.Interactive;
using SkillsExtended.Skills.LockPicking.Actions;
using SkillsExtended.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SkillsExtended.Skills.LockPicking.Alternative;

public sealed class AlternativeLockpick: MonoBehaviour
{
    private static AlternativeLockpick _instance;

    private AudioSource _lockpickingSfx;
    private AudioSource _failureSfx;
    
    private bool _inProgress;
    public static bool IsInProgress => _instance != null && _instance._inProgress;

    private AlternativeLockpickUI _ui;

    public static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var go = new GameObject("SkillsExtended.AlternativeLockpickController");
        DontDestroyOnLoad(go);

        _instance = go.AddComponent<AlternativeLockpick>();
        _instance._ui = AlternativeLockpickUI.EnsureInstance();
        
        _instance._lockpickingSfx = go.AddComponent<AudioSource>();
        _instance._lockpickingSfx.playOnAwake = false;
        _instance._lockpickingSfx.loop = true;
        _instance._lockpickingSfx.spatialBlend = 0f;
        _instance._lockpickingSfx.ignoreListenerPause = true;
        _instance._lockpickingSfx.ignoreListenerVolume = false;

        _instance._failureSfx = go.AddComponent<AudioSource>();
        _instance._failureSfx.playOnAwake = false;
        _instance._failureSfx.loop = false;
        _instance._failureSfx.spatialBlend = 0f;
        _instance._lockpickingSfx.ignoreListenerPause = true;
        _instance._failureSfx.ignoreListenerVolume = false;
    }

    public static void StartPick(GamePlayerOwner owner, WorldInteractiveObject door, LockPickActionHandler handler, int doorLevel)
    {
        EnsureInstance();

        if (_instance._inProgress)
        {
            return;
        }

        if (owner == null || owner.Player == null || door == null || handler == null)
        {
            return;
        }

        _instance.StartCoroutine(_instance.PickRoutine(owner, handler, doorLevel));
    }

    private IEnumerator PickRoutine(GamePlayerOwner owner, LockPickActionHandler handler, int doorLevel)
    {
        _inProgress = true;

        var player = owner?.Player;
        
        if (player == null || !player.IsYourPlayer)
        {
            _ui.Hide();
            yield break;
        }
        
        float durationSec = CalculateDurationSeconds(doorLevel);
        float failChance = CalculateFailChance(doorLevel);

        bool? result = null;

        _ui.Show(durationSec);
        
        var lp = LockPickingHelpers.LockPickingGame?.GetComponent<LockPickingGame>();

        var stuck = lp?.clickSound;
        var reset = lp?.resetSound;

        if (stuck != null)
        {
            _lockpickingSfx.clip = stuck;
            _lockpickingSfx.Play();
        }

        try
        {
            player.MovementContext.ToggleBlockInputPlayerRotation(true);
            player.CurrentManagedState.ChangePose(-1f);
            GamePlayerOwner.IgnoreInputWithKeepResetLook = true;
            GamePlayerOwner.IgnoreInputInNPCDialog = true;

            float elapsed = 0f;

            while (elapsed < durationSec)
            {
                if (ShouldClose())
                {
                    result = false;
                    handler.PickLockAction(false);
                    break;
                }

                elapsed += Time.unscaledDeltaTime;

                _ui.SetProgress(elapsed / durationSec);
                _ui.SetSeconds(Mathf.Max(0f, durationSec - elapsed));

                yield return null;
            }
            
            if (!result.HasValue)
            {
                bool success = Random.value >= failChance;
                result = success;
                handler.PickLockAction(success);
            }
        }
        finally
        {
            if (_lockpickingSfx.isPlaying)
            {
                _lockpickingSfx.Stop();
                _lockpickingSfx.clip = null;
            }
            
            player.MovementContext.ToggleBlockInputPlayerRotation(false);
            player.CurrentManagedState.ChangePose(1f);
            GamePlayerOwner.IgnoreInputWithKeepResetLook = false;
            GamePlayerOwner.IgnoreInputInNPCDialog = false;

            _inProgress = false;

            if (result.HasValue)
            {
                if (!result.Value && reset != null)
                {
                    _failureSfx.PlayOneShot(reset);
                }
                
                _ui.ShowResult(result.Value);
            }
            else
            {
                _ui.Hide();
            }
        }
    }

    private static float CalculateDurationSeconds(int doorLevel)
    {
        var cfg = SkillsExtendedPlugin.SkillData.LockPicking;
        
        float baseTime = cfg.BaseLockpickDurationSeconds <= 0f ? 5f : cfg.BaseLockpickDurationSeconds;
        float doorTimeMult = Mathf.Clamp(1f + (doorLevel - 1) * 0.25f, 1f, 2f);
        
        float timeReduction = 0f;
        
        var sm = GameUtils.GetSkillManager();
        if (sm?.SkillManagerExtended != null)
        {
            timeReduction = sm.SkillManagerExtended.LockPickingTimeBuff.Value;
        }
        
        float timeMultFromSkill = Mathf.Clamp(timeReduction, 0f, 0.75f);

        float duration = baseTime * doorTimeMult * (1f - timeMultFromSkill);
        return Mathf.Max(duration, 0.5f);
    }

    private static float CalculateFailChance(int doorLevel)
    {
        var cfg = SkillsExtendedPlugin.SkillData.LockPicking;
        
        float baseFail = Mathf.Clamp01(cfg.BaseFailureChance <= 0f ? 0.25f : cfg.BaseFailureChance);
        
        float doorFailMult = Mathf.Clamp(1f + (doorLevel - 1) * 0.2f, 1f, 1.8f);

        float failReduction = 0f;
        var sm = GameUtils.GetSkillManager();
        if (sm?.SkillManagerExtended != null)
        {
            failReduction = sm.SkillManagerExtended.LockPickingForgiveness.Value;
        }
        
        float failChance = baseFail * doorFailMult * (1f - failReduction);
        
        return Mathf.Clamp(failChance, 0f, 1f);
    }
    
    private bool ShouldClose()
    {
        return Input.GetMouseButtonDown(0) 
               || Input.GetMouseButtonDown(1) 
               || Input.GetKey(KeyCode.Escape); 
    }
}
