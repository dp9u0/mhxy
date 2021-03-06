﻿// FileName:  SpFrame.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180207 14:10
// Description:   

#region

using System.Drawing;

#endregion

namespace mhxy.NetEase.Wass {

    /// <summary>
    ///     帧数据
    /// </summary>
    public class SpFrame {

        /// <summary>
        ///     透明色
        /// </summary>
        public Color ColorA0 = Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        ///     图片的关键位X
        /// </summary>
        public int KeyX { get; set; }

        /// <summary>
        ///     图片的关键位Y
        /// </summary>
        public int KeyY { get; set; }

        /// <summary>
        ///     原始图片的宽度，单位像素
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        ///     原始图片的高度，单位像素
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        ///     图片的宽度，单位像素
        /// </summary>
        public int BitmapWidth { get; set; }

        /// <summary>
        ///     图片的高度，单位像素
        /// </summary>
        public int BitmapHeight { get; set; }

        ///// <summary>
        ///// 原始数据
        ///// </summary>
        //public byte[] Data { get; set; }

        /// <summary>
        ///     解析后的数据
        /// </summary>
        public Bitmap Bitmap { get; set; }

    }

}