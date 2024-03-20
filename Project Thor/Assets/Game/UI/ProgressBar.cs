using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public Image FillImage;

    public void UpdateProgressBar(float fillpercent)
    {
        FillImage.fillAmount = fillpercent;
    }

    public Image GetFillImage()
    {
        return FillImage;
    }
}
