using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InputDebugger : MonoBehaviour
{
    public List<KeyCode> keysToCheck;
    public List<TextMeshPro> keyDisplays;
    public List<GameObject> keyDisplayParents;

    private string GetKeyCodeName(KeyCode keyCode)
    {
        return Enum.GetName(typeof(KeyCode), keyCode);
    }

    void Update()
    {
        var displayIndex = 0;
        foreach (var key in keysToCheck)
        {
            if (displayIndex > keyDisplays.Count - 1) break;
            if (Input.GetKey(key))
            {
                keyDisplays[displayIndex].text = GetKeyCodeName(key);
                keyDisplayParents[displayIndex].SetActive(true);
                displayIndex += 1;
            }
        }

        for (; displayIndex < keyDisplays.Count; displayIndex++)
        {
            keyDisplays[displayIndex].text = "";
            keyDisplayParents[displayIndex].SetActive(false);
        }
    }
    
    
}
