using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LPComponents : MonoBehaviour
{
    [SerializeField]
    public RectTransform Cylinder;

    [SerializeField]
    public RectTransform LockPick;

    [SerializeField]
    public AudioSource Reset;

    [SerializeField]
    public AudioSource Stuck;

    [SerializeField]
    public AudioSource Turn;

    [SerializeField]
    public AudioSource Unlock;

    [SerializeField]
    public Animator Animator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}