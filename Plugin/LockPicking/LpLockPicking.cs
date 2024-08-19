using System;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.InputSystem;
using EFT.Communications;
using SkillsExtended.Helpers;
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
    // Holds all pieces of the safe lock for quicker access
    public RectTransform cylinder;
    internal float cylinderSize;
    public RectTransform lockpick;
    
    /// <summary>
    /// How accurately close we need to be to the sweet spot.
    /// If set to 1, we need to be exactly at the sweetspot position,
    /// but if set to a higher number we can be farther from the center position of the sweetspot.
    /// </summary>
    [Range(1, 90)]
    public float sweetSpotRange = 20; 

    /// <summary>
    /// The button that rotates the cylinder. The cylinder will only rotate if we are in the sweet spot.
    /// </summary>
    public string rotateButton = "a";

    /// <summary>
    /// How fast the cylinder rotates
    /// </summary>
    public float rotateSpeed = 90;

    /// <summary>
    /// How much we need to rotate the cylinder in order to win
    /// </summary>
    public float rotateToWin = 330;

    /// <summary>
    /// How long to wait before deactivating the lock object when we exit the game, either after winning/losing
    /// </summary>
    public float deactivateDelay = 1;

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
    private Action<bool> _onUnlocked;
    
    // Is the cylinder rotating
    private static bool _isRotating;

    // If in the sweet spot, the cylinder can be rotated. If not, the cylinder will return to its original angle
    private static bool _inSweetSpot;

    // If the lock is unlocked, we win
    private static bool _isUnlocked = false;

    //The sweet spot angle that we must reach with the lock pick
    private static float _sweetSpotAngle = 0;
    
    public void Start()
    {
        animator = GetComponent<Animator>();

        abortButton.onClick.AddListener(() => Deactivate());
        _sweetSpotAngle = Random.Range(0, 180);
    }

    public void Update()
    {
        if (_isUnlocked) return;

        MoveLockPick();
        
        _isRotating = Input.GetKey(rotateButton);

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
    public void Activate(Action<bool> action)
    {
        _sweetSpotAngle = Random.Range(0, 180);
        _onUnlocked = action;
        _isUnlocked = false;
        
        _player.CurrentManagedState.ChangePose(-1f);
        
        CursorSettings.SetCursor(ECursorType.Idle);
        Cursor.lockState = CursorLockMode.None;
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuContextMenu);
    }

    /// <summary>
    /// Deactivates the lock and stops the lock game. This is when we press the abort button
    /// </summary>
    private void Deactivate()
    {
        animator.Play("Deactivate");
        
        _player.CurrentManagedState.ChangePose(1f);
        
        CursorSettings.SetCursor(ECursorType.Invisible);
        Cursor.lockState = CursorLockMode.Locked;
        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuDropdown);
        
        cylinder!.eulerAngles = Vector3.zero;
        gameObject.SetActive(false);
    }

    private void MoveLockPick()
    {
        lockpick.eulerAngles = 
            Vector3.forward * 180 * Mathf.Clamp((Input.mousePosition.x / Screen.width), 0.01f, 0.99f);

        lockpick.eulerAngles = Vector3.forward * Mathf.Clamp(lockpick.eulerAngles.z, 0, 180);
            
        _inSweetSpot = Mathf.Abs(_sweetSpotAngle - lockpick.eulerAngles.z) < sweetSpotRange;
    }

    private void MoveCylinder()
    {
        // If the lock pick is in the sweet spot, the cylinder can rotate
        if (!_inSweetSpot) return;
        
        // Play the cylinder sound
        if (audioSource.isPlaying == false) 
            audioSource.PlayOneShot(rotateSound);

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

            // Play the lock pick wiggle sound
            if (audioSource.isPlaying == false) 
                audioSource.PlayOneShot(clickSound);
        }
        else
        {
            // Reset the lock pick position
            lockpick!.localPosition = Vector3.zero;

            // Play the reset sound
            audioSource.Stop();
        }
    }
    
    private void HandleWin()
    {
        // If the cylinder rotates beyond this angle, we win
        if (cylinder!.eulerAngles.z < rotateToWin) return;
                
        _isUnlocked = true;
                
        animator.Play("Win");
        
        audioSource.PlayOneShot(winSound);

        // Unselect any buttons on the cylinder, so we don't press them when pressing 'SPACE' after closing the lock
        if (EventSystem.current) 
            EventSystem.current.SetSelectedGameObject(null);

        cylinder!.eulerAngles = Vector3.zero;
        gameObject.SetActive(false);
        _onUnlocked.Invoke(true);
    }
}


    
        
        
