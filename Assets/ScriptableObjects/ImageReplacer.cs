using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageReplacer : MonoBehaviour {

    public Image Image;

    public SpriteVariable Variable;
    public bool PreserveAspectRatio = false;
    public bool AlwaysUpdate;

    private void OnEnable()
    {
        Image.sprite = Variable.Value;
        Image.preserveAspect = true;
    }

    private void Update()
    {
        if (AlwaysUpdate)
        {
            Image.sprite = Variable.Value;
        }
    }
}
