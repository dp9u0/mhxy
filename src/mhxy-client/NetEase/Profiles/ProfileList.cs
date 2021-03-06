﻿// FileName:  ProfileList.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180202 08:53
// Description:   

#region

using System.Collections.Generic;

#endregion

namespace mhxy.NetEase.Profiles {

    /// <summary>
    ///     存档简介列表
    /// </summary>
    public class ProfileList {

        public ProfileList() {
            ProfileBreifs = new List<ProfileBrief>();
        }

        /// <summary>
        ///     存档数量
        /// </summary>
        public int Count => ProfileBreifs.Count;

        /// <summary>
        ///     ProfileBreif列表
        /// </summary>
        public List<ProfileBrief> ProfileBreifs { get; set; }

    }

}