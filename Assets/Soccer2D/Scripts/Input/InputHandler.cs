using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    [SerializeField] private KeyCode player1Jump1;
    [SerializeField] private KeyCode player1Jump2;

    [Space(10)]
    [SerializeField] private KeyCode player2Jump1;
    [SerializeField] private KeyCode player2Jump2;

    public static Action OnPlayer1JumpPress;
    public static Action OnPlayer1JumpRelease;

    public static Action OnPlayer2JumpPress;
    public static Action OnPlayer2JumpRelease;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(player1Jump1) || Input.GetKeyDown(player1Jump2))
            OnPlayer1JumpPress?.Invoke();

        if (Input.GetKeyUp(player1Jump1) || Input.GetKeyUp(player1Jump2))
            OnPlayer1JumpRelease?.Invoke();

        if (Input.GetKeyDown(player2Jump1) || Input.GetKeyDown(player2Jump2))
            OnPlayer2JumpPress?.Invoke();

        if (Input.GetKeyUp(player2Jump1) || Input.GetKeyUp(player2Jump2))
            OnPlayer2JumpRelease?.Invoke();
    }
}
