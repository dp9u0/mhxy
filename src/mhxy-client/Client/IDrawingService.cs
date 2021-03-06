﻿// FileName:  IDrawingService.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180205 13:58
// Description:   

#region

using System;
using mhxy.Common;

#endregion

namespace mhxy.Client {

    /// <summary>
    ///     绘制服务
    /// </summary>
    public interface IDrawingService : IService {

        /// <summary>
        ///     添加可绘制对象到绘制层
        /// </summary>
        /// <param name="drawableObj"></param>
        void Add(IDrawable drawableObj);

        /// <summary>
        ///     移除可绘制对象
        /// </summary>
        /// <param name="drawableObj"></param>
        void Remove(IDrawable drawableObj);

        /// <summary>
        ///     移除全部可绘制对象
        /// </summary>
        void RemoveAll();

        /// <summary>
        ///     获取当前画布
        /// </summary>
        /// <returns></returns>
        Canvas GetCurrentCanvas();

        /// <summary>
        ///     绘制并返回绘制完成的画布
        /// </summary>
        Canvas Draw();

        /// <summary>
        ///     更新一帧绘制内容
        /// </summary>
        void UpdateFrame();

        event EventHandler BeforeFrame;

    }

}