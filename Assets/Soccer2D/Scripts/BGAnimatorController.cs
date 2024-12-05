using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGAnimatorController : MonoBehaviour {

    public static BGAnimatorController instance;

    private Animator animator;

	 void Start () {
        Debug.Assert(instance == null);
        instance = this;

        animator = GetComponent<Animator>();
    }

    public void Goal()
    {
        animator.SetTrigger("Flash");
    }
}
