﻿// FileName:  PathNode.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180211 12:50
// Description:   

#region

using System.Drawing;
using mhxy.Common.Model;

#endregion

namespace mhxy.Client.AStar {

    /// <summary>
    /// 路径节点
    /// </summary>
    public class PathNode {

        /// <summary>
        ///     坐标
        /// </summary>
        public Point Point { get; set; }

        /// <summary>
        ///     方向
        /// </summary>
        public Direction Direction { get; set; }

    }

}