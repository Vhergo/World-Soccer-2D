using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RateUsStar : MonoBehaviour
{

    private RateUsWindow rateUsWindow;
    private GameObject starSelectedSprite;

    void Start()
    {
        rateUsWindow = GetComponentInParent<RateUsWindow>();
        starSelectedSprite = transform.GetChild(0).gameObject;
        SetStarState(false);
    }

    public void OnClick()
    {
        rateUsWindow.OnRateClick(transform.GetSiblingIndex());
    }

    public void SetStarState(bool state)
    {
        starSelectedSprite.SetActive(state);
    }
}
