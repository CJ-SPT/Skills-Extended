using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// This class defines a lockpicking system that behaves similar to games such as Skyrim, Fallout, and Dying Light.
/// Here you must rotate the lockpick until you find the sweetspot, and then press a button to rotate the cylinder and unlock it
/// </summary>
public class LPLockpicking : MonoBehaviour
{
    // Holds all pieces of the safe lock for quicker access
    public RectTransform cylinder;
    internal float cylinderSize;
    public RectTransform lockpick;

    [Tooltip("How accurately close we need to be to the sweetspot. If set to 1, we need to be exactly at the sweetspot position, but if set to a higher number we can be farther from the center position of the sweetspot.")]
    [Range(1, 90)]
    public float sweetspotRange = 20; 

    [Tooltip("The button that rotates the cylinder. The cylinder will only rotate if we are in the sweetspot.")]
    public string rotateButton = "a";

    [Tooltip("How fast the cylinder rotates")]
    public float rotateSpeed = 90;

    [Tooltip("How much we need to rotate the cylinder in order to win")]
    public float rotateToWin = 330;

    [Tooltip("How long to wait before deactivating the lock object when we exit the game, either after winning/losing")]
    public float deactivateDelay = 1;

    [Tooltip("The sound that plays when we rotate the cylinder")]
    public AudioClip rotateSound;

    [Tooltip("The sound that plays when we reach a correct spot in one direction")]
    public AudioClip clickSound;

    [Tooltip("The sound that plays when the sequence resets")]
    public AudioClip resetSound;

    [Tooltip("The sound that plays when we win the lock game")]
    public AudioClip winSound;

    public AudioSource audioSource;

    public Animator animator;
    
    internal int index;

    // Is the cylinder rotating
    internal bool isRotating = false;

    // If in the sweetspot, the cylinder can be rotated. If not, the cylinder will return to its original angle
    internal bool inSweetspot = false;

    // If the lock is unlocked, we win
    internal bool isUnlocked = false;

    //The sweetspot angle that we must reach with the lockpick
    internal float sweetspotAngle = 0;
    
    public void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        if (isUnlocked) return;

        MoveLockPick();
        
        isRotating = Input.GetKey(rotateButton);

        if (isRotating)
        {
            // Rotate the cylinder object in the direction we chose
            cylinder?.Rotate(Time.deltaTime * rotateSpeed * Vector3.forward, Space.World);

            MoveCylinder();
            ResetCylinder(true);
            return;
        }
        
        ResetCylinder();
    }

    public void StartRotate()
    {
        isRotating = true;
    }

    public void StopRotate()
    {
        isRotating = false;
    }
    
    /// <summary>
    /// Activates the lock and starts the lock game
    /// </summary>
    public void Activate()
    {
        // Set a random sweetspot center, 
        sweetspotAngle = Random.Range(0, 180);
    }

    /// <summary>
    /// Deactivates the lock and stops the lock game. This is when we press the abort button
    /// </summary>
    public void Deactivate()
    {
        animator.Play("Deactivate");
    }

    private void MoveLockPick()
    {
        lockpick.eulerAngles = 
            Vector3.forward * 180 * Mathf.Clamp((Input.mousePosition.x / Screen.width), 0.01f, 0.99f);

        lockpick.eulerAngles = Vector3.forward * Mathf.Clamp(lockpick.eulerAngles.z, 0, 180);
            
        inSweetspot = Mathf.Abs(sweetspotAngle - lockpick.eulerAngles.z) < sweetspotRange;
    }

    private void MoveCylinder()
    {
        // If the lock pick is in the sweet spot, the cylinder can rotate
        if (!inSweetspot) return;
        
        // Play the cylinder sound
        if (audioSource.isPlaying == false) 
            audioSource.PlayOneShot(rotateSound);

        HandleWin();
    }

    private void ResetCylinder(bool wiggle = false)
    {
        if (inSweetspot) return;
        
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
                
        isUnlocked = true;
                
        animator.Play("Win");
                
        audioSource.PlayOneShot(winSound);

        // Unselect any buttons on the cylinder, so we don't press them when pressing 'SPACE' after closing the lock
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }
}


    
        
        
