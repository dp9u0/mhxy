﻿// FileName:  IClientEngine.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180201 12:34
// Description:   

using mhxy.Common;

namespace mhxy.Client {

    /// <summary>
    /// 客户端引擎
    /// 负责数据初始化加载
    /// 数据与UI同步
    /// 界面跳转等
    /// </summary>
    public interface IClientEngine : IService {

        /// <summary>
        /// 前往界面
        /// </summary>
        /// <param name="interType">界面类型</param>
        void Goto(InterfaceType interType);

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pwd"></param>
        bool SignIn(string name, string pwd);

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pwd"></param>
        bool SignUp(string name, string pwd);

        /// <summary>
        /// 登出
        /// </summary>
        bool SignOut();

        /// <summary>
        /// 加载角色存档
        /// </summary>
        /// <param name="id"></param>
        bool LoadProfile(int id);

        /// <summary>
        /// 保存当前存档
        /// </summary>
        /// <returns></returns>
        bool SaveProfile();

    }

}