global using GodotModules;
global using GodotModules.Netcode;
global using System;
global using System.Collections.Generic;
global using System.Collections.Concurrent;
global using System.Diagnostics;
global using System.Runtime.CompilerServices;
global using System.Threading;
global using System.Text.RegularExpressions;
global using System.Threading.Tasks;
global using System.Linq;

using Godot;

namespace GodotModules
{
    public class Game : Node
    {
        [Export] public readonly NodePath NodePathAudioStreamPlayer;
        [Export] public readonly NodePath NodePathWebRequestList;
        [Export] public readonly NodePath NodePathScenes;
        [Export] public readonly NodePath NodePathConsole;
        [Export] public readonly NodePath NodePathErrorNotifierManager;
        [Export] public readonly NodePath NodePathPopupManager;

        private Managers _managers;

        public override async void _Ready()
        {
            _managers = new(GetNode<Node>(NodePathWebRequestList), GetNode<AudioStreamPlayer>(NodePathAudioStreamPlayer), 
                GetNode<ErrorNotifierManager>(NodePathErrorNotifierManager), GetNode<PopupManager>(NodePathPopupManager),
                GetNode<ConsoleManager>(NodePathConsole));
            await _managers.InitSceneManager(GetNode<Control>(NodePathScenes), _managers.Hotkey);
            
            // how else would you pass this information to Logger?
            Logger.UIConsole = _managers.Console;
            Logger.ErrorNotifierManager = _managers.ErrorNotifier;

            _managers.Music.LoadTrack("Menu", "Audio/Music/Unsolicited trailer music loop edit.wav");
            _managers.Music.PlayTrack("Menu");

            _managers.Network.StartServer(25565, 100);
            _managers.Network.StartClient("127.0.0.1", 25565);

            await _managers.Web.CheckConnectionAsync();
            if (_managers.Web.ConnectionAlive)
                await _managers.Web.GetExternalIpAsync();
        }

        public override async void _Process(float delta)
        {
            Logger.Update();
            await _managers.Network.Update();
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed("ui_cancel"))
                if (_managers.Console.Visible)
                    _managers.Console.ToggleVisibility();
                else if (_managers.Scene.EscPressed.ContainsKey(_managers.Scene.CurScene))
                    _managers.Scene.EscPressed[_managers.Scene.CurScene]();

            if (Input.IsActionJustPressed("ui_console"))
                _managers.Console.ToggleVisibility();
        }

        public override async void _Notification(int what)
        {
            if (what == MainLoop.NotificationWmQuitRequest)
            {
                GetTree().SetAutoAcceptQuit(false);
                await Cleanup();
            }
        }

        private async Task Cleanup()
        {
            _managers.Options.SaveOptions();
            await _managers.Network.Cleanup();
            _managers.Token.Cleanup();
            GetTree().Quit();
        }
    }

    public class Managers 
    {
        public OptionsManager Options { get; private set; }
        public TokenManager Token;
        public NetworkManager Network;
        public SceneManager Scene;
        public WebManager Web;
        public MusicManager Music;
        public ErrorNotifierManager ErrorNotifier;
        public PopupManager Popup;
        public HotkeyManager Hotkey;
        public ConsoleManager Console;

        public Managers(Node webRequestList, AudioStreamPlayer audioStreamPlayer, ErrorNotifierManager errorNotifierManager, PopupManager popupManager, ConsoleManager consoleManager)
        {
            var systemFileManager = new SystemFileManager();
            Hotkey = new HotkeyManager(systemFileManager, new List<string>() {"UI", "Player"});
            Options = new(systemFileManager, Hotkey);
            Token = new();
            Web = new(new WebRequests(webRequestList), Token, Options.Options.WebServerAddress);
            Music = new(audioStreamPlayer, Options);
            
            ErrorNotifier = errorNotifierManager;
            Popup = popupManager;
            Network = new(Popup);
            Console = consoleManager;
        }

        public async Task InitSceneManager(Control sceneList, HotkeyManager hotkeyManager)
        {
            Scene = new(sceneList, new GodotFileManager(), hotkeyManager);

            // Pre Initialization
            Scene.PreInit[GodotModules.Scene.Menu] = (scene) =>
            {
                var menu = (UIMenu)scene;
                menu.PreInit(Scene, Network, Popup);
            };
            Scene.PreInit[GodotModules.Scene.Options] = (scene) =>
            {
                var options = (UIOptions)scene;
                options.PreInit(hotkeyManager, Options, Music, Web, Scene, Token);
            };
            Scene.PreInit[GodotModules.Scene.Credits] = (scene) =>
            {
                var credits = (UICredits)scene;
                credits.PreInit(Scene);
            };

            // Esc Pressed
            Scene.EscPressed[GodotModules.Scene.Credits] = async () => await Scene.ChangeScene(GodotModules.Scene.Menu);
            Scene.EscPressed[GodotModules.Scene.GameServers] = async () => await Scene.ChangeScene(GodotModules.Scene.Menu);
            Scene.EscPressed[GodotModules.Scene.Mods] = async () => await Scene.ChangeScene(GodotModules.Scene.Menu);
            Scene.EscPressed[GodotModules.Scene.Options] = async () => {
                Token.Cancel("check_connection");
                await Scene.ChangeScene(GodotModules.Scene.Menu);
            };
            Scene.EscPressed[GodotModules.Scene.Lobby] = async () => await Scene.ChangeScene(GodotModules.Scene.GameServers);
            Scene.EscPressed[GodotModules.Scene.Game] = async () => await Scene.ChangeScene(GodotModules.Scene.Menu);

            await Scene.InitAsync();
        }
    }
}
