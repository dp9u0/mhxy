﻿// FileName:  ControlBase.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180202 08:53
// Description:   

#region

using System;

#endregion

namespace mhxy.Client.Controls {

    /// <summary>
    ///     界面控件基类
    /// </summary>
    public abstract class ControlBase : IDrawable {

        public bool Static { get; set; }

        public void NextFrame() {
            throw new NotImplementedException();
        }

        public void Frame(int frame) {
            throw new NotImplementedException();
        }

        public void Draw(Canvas cancas) {
            throw new NotImplementedException();
        }

    }

}