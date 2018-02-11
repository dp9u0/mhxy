﻿// FileName:  ClientEngine.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180202 08:53
// Description:   

#region

using System.Collections.Generic;
using System.Drawing;
using mhxy.Client.Interfaces;
using mhxy.Common;
using mhxy.Common.Model;
using mhxy.NetEase.Profiles;
using mhxy.Utils;

#endregion

namespace mhxy.Client {

    /// <summary>
    /// </summary>
    public class ClientEngine : ServiceBase, IClientEngine {

        public ClientEngine() {
            InitializeInterfaces();
        }

        /// <summary>
        ///     界面处理器 用来实际控制哪些界面显示什么内容
        /// </summary>
        private readonly Dictionary<InterfaceType, InterfaceBase> _interfaces =
            new Dictionary<InterfaceType, InterfaceBase>();

        private InterfaceBase _currentInterface; //当前界面处理器

        //登陆相关
        private bool _signedIn;
        private string _currentName;
        private string _currentPwd;

        //存档相关
        private bool _profileLoaded;
        private int _currentProfileId;
        private Profile _currentProfile;

        //场景 角色相关
        private Scene _currentScene = new Scene();
        private CurrentPlayer _currentPlayer = CurrentPlayer.None;

        /// <summary>
        ///     前往某个界面
        /// </summary>
        /// <param name="interType"></param>
        public void Goto(InterfaceType interType) {
            Logger.Info($"Goto : {interType}");
            _currentInterface?.Close();
            _currentInterface = _interfaces[interType];
            _currentInterface?.Show();
        }

        /// <summary>
        /// 保存存档
        /// </summary>
        /// <returns>是否保存成功</returns>
        public bool SaveProfile() {
            Logger.Info("SaveProfile");
            if (!_signedIn) {
                Logger.Warn("用户尚未登录");
                return false;
            }
            if (!_profileLoaded) {
                Logger.Warn("存档尚未加载");
                return false;
            }
            _currentProfile.InitCreate = false;
            return ServiceLocator.ProfileService.TrySaveProfile(_currentName, _currentPwd, _currentProfileId, _currentProfile);
        }

        /// <summary>
        /// 卸载当前存档
        /// </summary>
        /// <returns></returns>
        public bool UnloadProfile() {
            if (!_signedIn) {
                Logger.Warn("用户尚未登录");
                return false;
            }
            if (!_profileLoaded) {
                Logger.Warn("存档尚未加载");
                return false;
            }
            _currentProfileId = 0;
            _profileLoaded = false;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CreateProfile(int id) {
            Logger.Info($"CreateProfile : {_currentName} {id}");
            if (!_signedIn) {
                Logger.Warn("用户尚未登录");
                return false;
            }
            _currentProfileId = id;
            _currentProfile = new Profile { InitCreate = true };
            _profileLoaded = true;
            return true;
        }

        /// <summary>
        /// 加载存档
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool LoadProfile(int id) {
            Logger.Info($"LoadProfile : {_currentName} {id}");
            if (!_signedIn) {
                Logger.Warn("用户尚未登录");
                return false;
            }
            if (!_profileLoaded && ServiceLocator.ProfileService.TryReadProfile(_currentName, _currentPwd, id, out Profile profile)) {
                var scene = new Scene {
                    MapId = _currentProfile.MapId
                };
                var player = new CurrentPlayer {
                    At = _currentProfile.PlayerAt,
                    FaceTo = Direction.Down,
                    Role = _currentProfile.Role,
                    Level = _currentPlayer.Level
                };
                _currentScene = scene;
                _currentPlayer = player;
                _currentProfileId = id;
                _currentProfile = profile;
                _profileLoaded = true;
                return true;
            }
            return false;
        }

        /// <summary>
        ///     注册用户
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public bool SignUp(string name, string pwd) {
            Logger.Info($"SignUp : {name} {pwd}");
            return !ServiceLocator.ProfileService.Has(name) && ServiceLocator.ProfileService.TryCreate(name, pwd.Md532());
        }

        /// <summary>
        ///     登录
        /// </summary>
        /// <param name="name">用户名</param>
        /// <param name="pwd">密码</param>
        /// <returns></returns>
        public bool SignIn(string name, string pwd) {
            Logger.Info($"SignIn : {name} {pwd}");
            if (!_signedIn && ServiceLocator.ProfileService.Has(name)) {
                pwd = pwd.Md532();
                if (ServiceLocator.ProfileService.TryRead(name, out string content) && string.Equals(pwd, content)) {
                    _currentName = name;
                    _currentPwd = pwd;
                    _signedIn = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     登出
        /// </summary>
        /// <returns></returns>
        public bool SignOut() {
            Logger.Info("SignOut");
            if (!_signedIn) {
                Logger.Warn("用户尚未登录");
                return false;
            }
            _currentProfileId = 0;
            _currentProfile = null;
            _currentName = string.Empty;
            _currentPwd = string.Empty;
            _signedIn = false;
            return true;
        }

        /// <summary>
        ///     当前场景
        /// </summary>
        /// <returns></returns>
        public Scene GetCurrentScene() {
            return _currentScene;
        }

        /// <summary>
        ///     当前玩家
        /// </summary>
        /// <returns></returns>
        public CurrentPlayer GetCurrentPlayer() {
            return _currentPlayer;
        }

        public void FlyTo(string sceneId, Point point) {

        }

        public void WalkTo(Point point) {

        }

        private void InitializeInterfaces() {
            _interfaces[InterfaceType.Start] = new StartInterface();
            _interfaces[InterfaceType.SignIn] = new SignInInterface();
            _interfaces[InterfaceType.SignUp] = new SignUpInterface();
            _interfaces[InterfaceType.Profile] = new ProfileInterface();
            _interfaces[InterfaceType.Create] = new CreateInterface();
            _interfaces[InterfaceType.Main] = new MainInterface();
            _interfaces[InterfaceType.Fight] = new FightInterface();
            _interfaces[InterfaceType.Loading] = new LoadingInterface();
            _interfaces[InterfaceType.Monolog] = new MonologInterface();
        }

    }

    public static class Externtions {

        /// <summary>
        /// </summary>
        /// <param name="engine"></param>
        public static void Start(this IClientEngine engine) {
            if (Global.Profile) {
                engine.Goto(InterfaceType.Start);
            } else {
                engine.SignUp(Global.DevelopName, Global.DevelopPwd.Md532());
                engine.SignIn(Global.DevelopName, Global.DevelopPwd.Md532());
                if (!engine.LoadProfile(Global.DevelopProfileId)) {
                    engine.CreateProfile(Global.DevelopProfileId);
                    engine.SaveProfile();
                    engine.UnloadProfile();
                    engine.LoadProfile(Global.DevelopProfileId);
                }
                ServiceLocator.ClientEngine.GetCurrentPlayer().At = new Point(2000, 1500);
                engine.Goto(InterfaceType.Main);
            }
        }

    }

}