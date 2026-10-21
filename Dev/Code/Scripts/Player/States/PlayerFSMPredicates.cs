using System;
using Godot;
using NosyCore.FSM;

public class InputPressedPredicate : FuncPredicate
{
    public InputPressedPredicate(string actionName) : base(() => Input.IsActionJustPressed(actionName))
    {
    }
}