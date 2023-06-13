using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    private static InputActionAsset inputConfig;

    public static InputActionMap gameplay;

    public static InputAction move;
    public static InputAction confirm;
    public static InputAction cancel;
    public static InputAction scroll;
    public static InputAction cursor;
    public static InputAction speedup;

    public static void Init(InputActionAsset config)
    {
        inputConfig = config;

        string rebinds = PlayerPrefs.GetString("rebinds");
        if (!string.IsNullOrEmpty(rebinds))
            inputConfig.LoadBindingOverridesFromJson(rebinds);

        gameplay = inputConfig.FindActionMap("gameplay");

        move = gameplay.FindAction("move");
        confirm = gameplay.FindAction("confirm");
        cancel = gameplay.FindAction("cancel");
        scroll = gameplay.FindAction("scroll");
        cursor = gameplay.FindAction("cursor");
        speedup = gameplay.FindAction("speedup");
    }
    public static void SaveRebinds()
    {
        string rebinds = inputConfig.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
    }
}