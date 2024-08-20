using System;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.InputSystem;
using EFT.Communications;
using EFT.Console.Core;
using EFT.Interactive;
using SkillsExtended.Config;
using SkillsExtended.Helpers;
using SkillsExtended.Patches.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace SkillsExtended.LockPicking;

/// <summary>
/// This class defines a lockpicking system that behaves similar to games such as Skyrim, Fallout, and Dying Light.
/// Here you must rotate the lockpick until you find the sweetspot, and then press a button to rotate the cylinder and unlock it
/// </summary>
public class LpLockPicking : MonoBehaviour
{
    #region FIELDS

    // Holds all pieces of the safe lock for quicker access
    public RectTransform cylinder;
    public RectTransform lockpick;

    /// <summary>
    /// How accurately close we need to be to the sweet spot.
    /// If set to 1, we need to be exactly at the sweet spot position,
    /// but if set to a higher number we can be farther from the center position of the sweet spot.
    /// </summary>
    private static float _sweetSpotRange = 0f;

    /// <summary>
    /// The button that rotates the cylinder. The cylinder will only rotate if we are in the sweet spot.
    /// </summary>
    private KeyCode _rotateButton => ConfigManager.LpMiniGameTurnKey.Value;

    /// <summary>
    /// How fast the cylinder rotates
    /// </summary>
    public float rotateSpeed = 75;

    /// <summary>
    /// How much we need to rotate the cylinder in order to win
    /// </summary>
    public float rotateToWin = 95;
    
    /// <summary>
    /// The sound that plays when we rotate the cylinder
    /// </summary>
    public AudioClip rotateSound;

    /// <summary>
    /// The sound that plays when we reach a correct spot in one direction
    /// </summary>
    public AudioClip clickSound;

    /// <summary>
    /// The sound that plays when the sequence resets
    /// </summary>
    public AudioClip resetSound;
    
    /// <summary>
    /// The sound that plays when we win the lock game
    /// </summary>
    public AudioClip winSound;

    public AudioSource audioSource;

    public Animator animator;

    public Button abortButton;

    private static Player _player => Singleton<GameWorld>.Instance?.MainPlayer;
    
    // Callback action
    private Action<bool> _onUnlocked;
    
    // Is the cylinder rotating
    private static bool _isRotating;

    // If in the sweet spot, the cylinder can be rotated. If not, the cylinder will return to its original angle
    private static bool _inSweetSpot;

    // If the lock is unlocked, we win
    private static bool _isUnlocked = false;

    // The sweet spot angle that we must reach with the lock pick
    private static float _lockPickSetAngle = 0;
    
    // Time fields to measure when a pick should break
    private static float _wiggleTimeLimit = 1f;
    private static float _timeSpentWiggling = 0;

    private static bool _disabled = true;
    
    #endregion
    
    public void OnEnable()
    {
        _disabled = false;
        
        if (_player is null) return;
        if (!_player.IsYourPlayer) return;
        
        _player.MovementContext.ToggleBlockInputPlayerRotation(true);
                
        _player.CurrentManagedState.ChangePose(-1f);
        
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
    }

    public void OnDisable()
    {
        _disabled = true;
        
        if (_player is null) return;
        if (!_player.IsYourPlayer) return;
        
        _player.MovementContext.ToggleBlockInputPlayerRotation(false);
        
        _player.CurrentManagedState.ChangePose(1f);

        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
        
        CursorSettings.SetCursor(ECursorType.Invisible);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
            
        if (GamePlayerOwner.MyPlayer is not null)
        {
            GamePlayerOwner.IgnoreInputWithKeepResetLook = false;
            GamePlayerOwner.IgnoreInputInNPCDialog = false;
        }
        
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdown);
    }
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        
        _lockPickSetAngle = Random.Range(0, 180);
    }
    
    public void Update()
    {
        if (_isUnlocked || _disabled) return;

        if (ShouldClose())
        {
            HandleWin(false);
            return;
        }
        
        MoveLockPick();
        
        CursorSettings.SetCursor(ECursorType.Idle);
        Cursor.lockState = CursorLockMode.None;

        if (GamePlayerOwner.MyPlayer is not null)
        {
            GamePlayerOwner.IgnoreInputWithKeepResetLook = true;
            GamePlayerOwner.IgnoreInputInNPCDialog = true;
        }
        
        _isRotating = Input.GetKey(_rotateButton);
        
        if (_isRotating)
        {
            // Rotate the cylinder object in the direction we chose
            cylinder?.Rotate(Time.deltaTime * rotateSpeed * Vector3.forward, Space.World);

            MoveCylinder();
            ResetCylinder(true);
            return;
        }
        
        ResetCylinder();
    }

    /// <summary>
    /// Activates the lock and starts the lock game
    /// </summary>
    public void Activate(GamePlayerOwner owner, WorldInteractiveObject interactiveObject, Action<bool> action)
    {
        _lockPickSetAngle = Random.Range(0, 180);
        _isUnlocked = false;
        _timeSpentWiggling = 0f;
        
        _onUnlocked = action;
        
        var doorLevel = LpHelpers.GetLevelForDoor(owner.Player.Location, interactiveObject.Id);
        
        SetSweetSpotRange(doorLevel);
        SetTimeLimit(doorLevel);
        
        Plugin.Log.LogDebug($"LEVEL:        {doorLevel}");
        Plugin.Log.LogDebug($"SWEET SPOT:   {_sweetSpotRange}");
        Plugin.Log.LogDebug($"ROTATE SPEED: {rotateSpeed}");
        Plugin.Log.LogDebug($"TIME LIMIT:   {_wiggleTimeLimit}");
        Plugin.Log.LogDebug($"WIN ANGLE:    {rotateToWin}");
    }
    
    public void ActivatePractice(int doorLevel)
    {
        _lockPickSetAngle = Random.Range(0, 180);
        _isUnlocked = false;
        _timeSpentWiggling = 0f;
        
        SetSweetSpotRange(doorLevel);
        SetTimeLimit(doorLevel);
        
        Plugin.Log.LogDebug($"LEVEL:        {doorLevel}");
        Plugin.Log.LogDebug($"SWEET SPOT:   {_sweetSpotRange}");
        Plugin.Log.LogDebug($"ROTATE SPEED: {rotateSpeed}");
        Plugin.Log.LogDebug($"TIME LIMIT:   {_wiggleTimeLimit}");
        Plugin.Log.LogDebug($"WIN ANGLE:    {rotateToWin}");
    }

    private bool ShouldClose()
    {
        return Input.GetMouseButtonDown(0) 
               || Input.GetMouseButtonDown(1) 
               || Input.GetKey(KeyCode.Escape); 
    }
    
    private void MoveLockPick()
    {
        lockpick.eulerAngles = 
            Mathf.Clamp((Input.mousePosition.x / Screen.width), 0.01f, 0.99f) * 180 * Vector3.forward;

        lockpick.eulerAngles = Vector3.forward * Mathf.Clamp(lockpick.eulerAngles.z, 0, 180);
            
        _inSweetSpot = Mathf.Abs(_lockPickSetAngle - lockpick.eulerAngles.z) < _sweetSpotRange;
    }

    private void MoveCylinder()
    {
        // If the lock pick is in the sweet spot, the cylinder can rotate
        if (!_inSweetSpot) return;
        
        // Play the cylinder sound
        if (!audioSource.isPlaying) 
            audioSource.PlayOneShot(rotateSound);
        
        // If the cylinder rotates beyond this angle, we win
        if (cylinder!.eulerAngles.z < rotateToWin) return;
        
        HandleWin();
    }

    private void ResetCylinder(bool wiggle = false)
    {
        if (_inSweetSpot) return;
        
        // Return to original rotation
        cylinder!.eulerAngles = 
            Vector3.Slerp(cylinder.eulerAngles, Vector3.zero, Time.deltaTime * 10);

        if (wiggle)
        {
            lockpick!.localPosition = 
                new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0);

            _timeSpentWiggling += Time.deltaTime;
            
            if (_timeSpentWiggling > _wiggleTimeLimit)
            {
                Plugin.Log.LogDebug($"Time limit reached");
                HandleWin(false);
            }
            
            // Play the lock pick wiggle sound
            if (!audioSource.isPlaying) 
                audioSource.PlayOneShot(clickSound);

            return;
        }
        
        // Reset the lock pick position
        lockpick!.localPosition = Vector3.zero;

        // Play the reset sound
        audioSource.Stop();
    }
    
    private void HandleWin(bool won = true)
    {
        _isUnlocked = true;
        
        if (won)
        {
            animator.Play("Win");
            audioSource.PlayOneShot(winSound);
        }
        
        // Unselect any buttons on the cylinder, so we don't press them when pressing 'SPACE' after closing the lock
        if (EventSystem.current) 
            EventSystem.current.SetSelectedGameObject(null);
        
        cylinder!.eulerAngles = Vector3.zero;
        
        if (_player is not null)
        {
            GamePlayerOwner.SetIgnoreInputWithKeepResetLook(false);
            _player.MovementContext.ToggleBlockInputPlayerRotation(false);
            
            Cursor.visible = false;
            CursorSettings.SetCursor(ECursorType.Invisible);
            Cursor.lockState = CursorLockMode.Locked;

            if (won)
            {
                _onUnlocked.Invoke(true);
            }
        }
        
        gameObject.SetActive(false);
    }

    private static void SetSweetSpotRange(int doorLevel)
    {
        var skillMod = 1 + Plugin.PlayerSkillManagerExt.LockPickingForgiveness;
        var doorMod = Mathf.Clamp(doorLevel / 35f, 0.05f, 1.5f);

        Plugin.Log.LogWarning($"SKILL: {skillMod}");
        Plugin.Log.LogWarning($"DOOR: {doorMod}");
        
        var configVal = Plugin.SkillData.LockPicking.SweetSpotRange;
        
        Plugin.Log.LogWarning($"CONFIG: {configVal}");
        
        _sweetSpotRange = Mathf.Clamp((configVal - doorMod) * skillMod, 0f, 20f);
    }
    
    private static void SetTimeLimit(int doorLevel)
    {
        var skillMod = 1 + Plugin.PlayerSkillManagerExt.LockPickingTimeBuff;
        var doorMod = Mathf.Clamp(doorLevel / 50f, 0.05f, 1f);
        
        var configVal = Plugin.SkillData.LockPicking.PickStrength;
        
        _wiggleTimeLimit = Mathf.Clamp((configVal - doorMod) * skillMod, 1f, 20f);
    }
}