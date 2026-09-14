using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class QuickInputWrapper
{ 
    public static bool GetKeyPress(string keyName) {
        return Keyboard.current[Enum.Parse<Key>(keyName)].isPressed;
    }
}
