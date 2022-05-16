using Godot;
using System;

public class UIPopupMessage : WindowDialog
{
    [Export] public readonly NodePath NodePathMessage;

    private string _message;
    private string _title;
    private PopupManager _popupManager;

    public void PreInit(PopupManager popupManager, string message, string title = "") 
    {
        _popupManager = popupManager;
        _message = message;
        if (!string.IsNullOrWhiteSpace(title))
            _title = title;
        else
            _title = "";
    }

    public override void _Ready()
    {
        WindowTitle = _title;
        GetNode<Label>(NodePathMessage).Text = _message;
    }

    private void _on_UIPopupMessage_popup_hide() 
    {
        _popupManager.SpawnNextPopup();
        QueueFree();
    }

    private void _on_Ok_pressed() => Hide();
}
