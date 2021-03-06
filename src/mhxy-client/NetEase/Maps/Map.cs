﻿// FileName:  Map.cs
// Author:  guodp <guodp9u0@gmail.com>
// Create Date:  20180202 08:53
// Description:   

#region

using System;
using System.Drawing;
using System.IO;
using System.Text;
using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using mhxy.Utils;

#endregion

namespace mhxy.NetEase.Maps {

    /// <summary>
    ///     地图对象
    /// </summary>
    public class Map : ResourceBase {

        /// <summary>
        ///     使用文件名构造Map对象
        /// </summary>
        /// <param name="fileName">地图文件名</param>
        public Map(string fileName) {
            if (!File.Exists(fileName)) {
                throw new FileNotFoundException(fileName);
            }

            _fileName = fileName;
            Logger.Info($"Create Map:{fileName}");
        }

        /// <summary>
        ///     加载资源
        /// </summary>
        public override void Load() {
            if (_loaded) {
                return;
            }

            Logger.Info($"Begin Load Map : {_fileName}");
            try {
                var buffer4 = new byte[4];
                using (var fs = new FileStream(_fileName, FileMode.Open)) {
                    //1.Read Header
                    //1.1.Read Width Height
                    fs.Read(buffer4, 0, 4);
                    _flag = BitConverter.ToString(buffer4);
                    fs.Read(buffer4, 0, 4);
                    _width = BitConverter.ToInt32(buffer4, 0);
                    MaxX = _width - Global.Width;
                    fs.Read(buffer4, 0, 4);
                    _height = BitConverter.ToInt32(buffer4, 0);
                    MaxY = _height - Global.Height;
                    _unitColumns = (int) Math.Ceiling((double) Width / Global.ImageWidthPerMapUnit);
                    _unitRows = (int) Math.Ceiling((double) Height / Global.ImageHeightPerMapUnit);
                    _unitSize = _unitColumns * _unitRows;
                    _unitOffsets = new int[_unitSize];
                    //_units = new Unit[_unitSize];
                    Logger.Info($"Flag(302E314D):{_flag},Width:{Width}" +
                                $",Height:{Height},Unit Columns:{_unitColumns}" +
                                $",Unit Rows:{_unitRows},Unit Size:{_unitSize}");
                    //1.2.Read Unit Indexes
                    for (var i = 0; i < _unitSize; i++) {
                        fs.Read(buffer4, 0, 4);
                        _unitOffsets[i] = BitConverter.ToInt32(buffer4, 0);
                        //Logger.Debug($"Unit:{i},Offset:{_unitOffsets[i]}");
                    }

                    //1.3.Read Mask Header
                    fs.Read(buffer4, 0, 4);
                    _maskFlag = BitConverter.ToString(buffer4);
                    fs.Read(buffer4, 0, 4);
                    _maskSize = BitConverter.ToInt32(buffer4, 0);
                    _maskOffsets = new int[_maskSize];
                    //_masks = new Mask[_maskSize];
                    Logger.Info($"Mask Head Flag(70030000):{_maskFlag},Mask Size:{_maskSize}");
                    for (var i = 0; i < _maskSize; i++) {
                        fs.Read(buffer4, 0, 4);
                        _maskOffsets[i] = BitConverter.ToInt32(buffer4, 0);
                    }

                    Bitmap = new Bitmap(Width, Height);
                    MaskBitmap = new Bitmap(Width, Height);
                    Grid = new byte[_unitRows * Global.CellHeightPerMapUnit, _unitColumns * Global.CellWidthPerMapUnit];
                    _masks = new byte[Height, Width];
                    fs.Seek(0, SeekOrigin.Begin);
                    //2.Read Map Image And Grid
                    Logger.Info("Begin Read Map Image And Grid");
                    using (ImageFactory factory = new ImageFactory()) {
                        for (var rowIndex = 0; rowIndex < _unitRows; rowIndex++) {
                            for (var colIndex = 0; colIndex < _unitColumns; colIndex++) {
                                var index = colIndex + _unitColumns * rowIndex;
                                var unit = ReadUnit(fs, _unitOffsets[index]);
                                if (!unit.Decoded) {
                                    continue;
                                }

                                // 复制Cell
                                for (int i = 0; i < unit.Cell.Data.Length; i++) {
                                    int x = i / Global.CellWidthPerMapUnit;
                                    int y = i % Global.CellWidthPerMapUnit;
                                    int indexX = x + Global.CellHeightPerMapUnit * rowIndex;
                                    int indexY = y + Global.CellWidthPerMapUnit * colIndex;
                                    Grid[indexX, indexY] = (byte) (unit.Cell.Data[i] == 0 ? 1 : 0);
                                }

                                // 复制Image
                                if (factory.Load(unit.RealImage)
                                    .Image is Bitmap unitBitmap) {
                                    FastBitmap.CopyRegion(unitBitmap, Bitmap,
                                        new Rectangle(0, 0, unitBitmap.Width, unitBitmap.Height),
                                        new Rectangle(colIndex * 320, rowIndex * 240, unitBitmap.Width
                                            , unitBitmap.Height));
                                }
                            }
                        }
                    }

                    if (Global.ShowMapMask) {
                        Logger.Info("End Read Map Image And Grid");
                        //3.Read Mask
                        FastBitmap bitmap = new FastBitmap(MaskBitmap);
                        bitmap.Lock();
                        Logger.Info("Begin Read Mask");
                        for (int i = 0; i < _maskSize; i++) {
                            var mask = ReadMask(fs, _maskOffsets[i]);
                            for (int h = 0; h < mask.Height; h++) {
                                for (int w = 0; w < mask.Width; w++) {
                                    int maskIndex = (h * mask.AlignWidth + w) * 2; //单位:位
                                    byte maskValue1 = mask.DecodedData[maskIndex / 8]; //定位到字节
                                    var maskValue = (byte) (maskValue1 >> (maskIndex % 8)); //定位到位
                                    var index0 = mask.StartY + h;
                                    var index1 = mask.StartX + w;
                                    byte value = (byte) ((maskValue & 3) == 3 ? 1 : 0);
                                    _masks[index0, index1] = value;
                                    if (value == 1) {
                                        var pixel = Bitmap.GetPixel(index1, index0);
                                        bitmap.SetPixel(index1, index0, pixel);
                                    }
                                }
                            }
                        }

                        bitmap.Unlock();
                        Logger.Info("End Read Mask");
                    }
                }

                _loaded = true;
            } catch (Exception e) {
                Logger.Error($"Error In Load Map : {_fileName}", e);
                throw;
            }

            Logger.Info($"End Load Map : {_fileName}");
        }

        private void DecodeMask(byte[] input, byte[] output) {
            int t = 0;
            int pos = 0;
            int inputIndex = 0;
            int outputIndex = 0;
            bool gotoMatch = false;
            bool gotoMatchDone = false;
            bool gotoCopyMatch = false;
            bool gotoMatchNext = false;
            bool gotoFirstLiteralRun = false;
            if (input[inputIndex] > 17) {
                t = input[inputIndex++] - 17;
                if (t < 4) {
                    gotoMatchNext = true;
                }

                do {
                    output[outputIndex++] = input[inputIndex++];
                } while (--t > 0);

                gotoFirstLiteralRun = true;
            }

            while (true) {
                if (gotoMatchNext) {
                    goto matchLoop;
                }

                if (gotoFirstLiteralRun) {
                    goto first_literal_run;
                }

                t = input[inputIndex++];
                if (t >= 16) {
                    gotoMatch = true;
                    goto matchLoop;
                }

                if (t == 0) {
                    while (input[inputIndex] == 0) {
                        t += 255;
                        inputIndex++;
                    }

                    t += 15 + input[inputIndex++];
                }

                output[outputIndex++] = input[inputIndex++];
                output[outputIndex++] = input[inputIndex++];
                output[outputIndex++] = input[inputIndex++];
                output[outputIndex++] = input[inputIndex++];
                //outputIndex += 4;
                //inputIndex += 4;
                if (--t > 0) {
                    if (t >= 4) {
                        do {
                            output[outputIndex++] = input[inputIndex++];
                            output[outputIndex++] = input[inputIndex++];
                            output[outputIndex++] = input[inputIndex++];
                            output[outputIndex++] = input[inputIndex++];
                            //outputIndex += 4;
                            //inputIndex += 4;
                            t -= 4;
                        } while (t >= 4);

                        if (t > 0)
                            do {
                                output[outputIndex++] = input[inputIndex++];
                            } while (--t > 0);
                    } else
                        do {
                            output[outputIndex++] = input[inputIndex++];
                        } while (--t > 0);
                }

                first_literal_run:
                t = input[inputIndex++];
                if (t >= 16) {
                    gotoMatch = true;
                    goto matchLoop;
                }

                pos = outputIndex - 0x0801;
                pos -= t >> 2;
                pos -= input[inputIndex++] << 2;
                output[outputIndex++] = output[pos++];
                output[outputIndex++] = output[pos++];
                output[outputIndex++] = output[pos];
                gotoMatchDone = true;
                matchLoop:
                while (true) {
                    if (gotoCopyMatch) {
                        // gotoCopyMatch = false;
                        goto copy_match;
                    }

                    if (gotoMatch) {
                        gotoMatch = false;
                        goto match;
                    }

                    if (gotoMatchDone) {
                        gotoMatchDone = false;
                        goto match_done;
                    }

                    if (gotoMatchNext) {
                        gotoMatchNext = false;
                        goto match_next;
                    }

                    match:
                    if (t >= 64) {
                        pos = outputIndex - 1;
                        pos -= (t >> 2) & 7;
                        pos -= input[inputIndex++] << 3;
                        t = (t >> 5) - 1;
                        gotoCopyMatch = true;
                        goto matchLoop;
                    }

                    if (t >= 32) {
                        t &= 31;
                        if (t == 0) {
                            while (input[inputIndex] == 0) {
                                t += 255;
                                inputIndex++;
                            }

                            t += 31 + input[inputIndex++];
                        }

                        pos = outputIndex - 1;
                        var offset = BitConverter.ToUInt16(input, inputIndex);
                        pos -= offset >> 2;
                        inputIndex += 2;
                    } else if (t >= 16) {
                        pos = outputIndex;
                        pos -= (t & 8) << 11;
                        t &= 7;
                        if (t == 0) {
                            while (input[inputIndex] == 0) {
                                t += 255;
                                inputIndex++;
                            }

                            t += 7 + input[inputIndex++];
                        }

                        var offset = BitConverter.ToUInt16(input, inputIndex);
                        pos -= offset >> 2;
                        inputIndex += 2;
                        if (pos == outputIndex) {
                            goto eof_found;
                        }

                        pos -= 0x4000;
                    } else {
                        pos = outputIndex - 1;
                        pos -= t >> 2;
                        pos -= input[inputIndex++] << 2;
                        output[outputIndex++] = output[pos++];
                        output[outputIndex++] = output[pos];
                        goto match_done;
                    }

                    copy_match:
                    if (t >= 6 && outputIndex - pos >= 4 && !gotoCopyMatch) {
                        output[outputIndex++] = output[pos++];
                        output[outputIndex++] = output[pos++];
                        output[outputIndex++] = output[pos++];
                        output[outputIndex++] = output[pos++];
                        //outputIndex += 4;
                        //pos += 4;
                        t -= 2;
                        do {
                            output[outputIndex++] = output[pos++];
                            output[outputIndex++] = output[pos++];
                            output[outputIndex++] = output[pos++];
                            output[outputIndex++] = output[pos++];
                            //outputIndex += 4;
                            //pos += 4;
                            t -= 4;
                        } while (t >= 4);

                        if (t > 0)
                            do {
                                output[outputIndex++] = output[pos++];
                            } while (--t > 0);
                    } else {
                        if (gotoCopyMatch) {
                            gotoCopyMatch = false;
                        }

                        output[outputIndex++] = output[pos++];
                        output[outputIndex++] = output[pos++];
                        do {
                            output[outputIndex++] = output[pos++];
                        } while (--t > 0);
                    }

                    match_done:
                    t = input[inputIndex - 2] & 3;
                    if (t == 0) {
                        break;
                    }

                    match_next:
                    do {
                        output[outputIndex++] = input[inputIndex++];
                    } while (--t > 0);

                    t = input[inputIndex++];
                }
            }

            eof_found:;
        }

        /// <summary>
        ///     将map文件另存为jpg图像
        /// </summary>
        public override void Save() {
            if (!_loaded) {
                return;
            }

            Logger.Info($"Begin Save Map : {_fileName}");
            var fileName = _fileName + ".jpg";
            try {
                using (var imageFactory = new ImageFactory()) {
                    imageFactory.Load(Bitmap).Format(new JpegFormat()).Save(fileName);
                }
            } catch (Exception e) {
                Logger.Error($"Save Map : {fileName}", e);
            }

            string fileName2 = _fileName + ".cell.txt";
            try {
                var sb = new StringBuilder();
                for (int i = 0; i < _unitRows * Global.CellHeightPerMapUnit; i++) {
                    for (int j = 0; j < _unitColumns * Global.CellWidthPerMapUnit; j++) {
                        sb.Append((int) Grid[i, j]);
                        sb.Append(" ");
                    }

                    sb.AppendLine();
                }

                File.WriteAllText(fileName2, sb.ToString());
            } catch (Exception e) {
                Logger.Error($"Save Cell : {fileName2}", e);
            }

            Logger.Info($"End Save Map : {_fileName}");
        }

        /// <summary>
        ///     从文件中读取Mask数据
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="offSet"></param>
        /// <returns></returns>
        private Mask ReadMask(FileStream fs, int offSet) {
            var buffer4 = new byte[4];
            fs.Seek(offSet, SeekOrigin.Begin);
            fs.Read(buffer4, 0, 4);
            int startX = BitConverter.ToInt32(buffer4, 0);
            fs.Read(buffer4, 0, 4);
            int startY = BitConverter.ToInt32(buffer4, 0);
            fs.Read(buffer4, 0, 4);
            int width = BitConverter.ToInt32(buffer4, 0);
            fs.Read(buffer4, 0, 4);
            int height = BitConverter.ToInt32(buffer4, 0);
            fs.Read(buffer4, 0, 4);
            int size = BitConverter.ToInt32(buffer4, 0);
            var mask = new Mask(startX, startY, width, height, size) {Data = new byte[size]};
            fs.Read(mask.Data, 0, size);
            mask.AlignWidth = (mask.Width / 4 + (mask.Width % 4 != 0 ? 1 : 0)) * 4; // 以4对齐的宽度
            mask.DecodedData = new byte[mask.AlignWidth * mask.Height / 4];
            try {
                DecodeMask(mask.Data, mask.DecodedData);
            } catch {
                // ignored
            }

            return mask;
        }

        /// <summary>
        ///     从文件中读取unit数据
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="offSet"></param>
        /// <returns></returns>
        private Unit ReadUnit(FileStream fs, int offSet) {
            fs.Seek(offSet, SeekOrigin.Begin);
            var buffer4 = new byte[4];
            fs.Read(buffer4, 0, 4);
            uint realOffset = BitConverter.ToUInt32(buffer4, 0);
            fs.Seek(realOffset * 4, SeekOrigin.Current);
            UnitData img = ReadUnitData(fs);
            UnitData cell = ReadUnitData(fs);
            //UnitData brig = ReadUnitData(fs);
            var unit = new Unit(realOffset) {Cell = cell};
            if (string.Equals(img.Flag, "47-45-50-4A")) {
                // JPEG
                unit.Decoded = DecodeJpeg(img.Data, out byte[] realImage);
                unit.RealImage = realImage;
            } else if (string.Equals(img.Flag, "32-47-50-4A")) {
                // JPG
                unit.Decoded = true;
                unit.RealImage = img.Data;
            }

            //Logger.Debug($"Read Unit({index}:{offSet}):RealOffset:{realOffset}");
            //Logger.Debug($"UnitData(jpeg):Flag(47-45-50-4A):{jpeg.Flag},Size:{jpeg.Size}"); //也可能是 32-47-50-4A,JPG
            //Logger.Debug($"UnitData(cell):Flag(4C-4C-45-43):{cell.Flag},Size:{cell.Size}");
            //Logger.Debug($"UnitData(brig):Flag(47-49-52-42):{brig.Flag},Size:{brig.Size}");
            return unit;
        }

        private bool DecodeJpeg(byte[] origin, out byte[] result) {
            int inSize = origin.Length;
            int incrementSize = 0;
            int originIndex = 0;
            int resultIndex = 0;
            int processedCount = 0;
            var tempResult = new byte[inSize * 2];
            byte[] byte2 = new byte[2];
            result = null;
            try {
                // 当已读取数据的长度小于总长度时继续
                while (processedCount < inSize && origin[originIndex++] == 0xFF) {
                    tempResult[resultIndex++] = 0xFF;
                    processedCount++;
                    ushort tempTimes; // 临时变量，表示循环的次数
                    switch (origin[originIndex]) {
                        case 0xD8:
                            tempResult[resultIndex++] = 0xD8;
                            originIndex++;
                            processedCount++;
                            break;
                        case 0xA0:
                            originIndex++;
                            resultIndex--;
                            processedCount++;
                            break;
                        case 0xC0:
                            tempResult[resultIndex++] = 0xC0;
                            originIndex++;
                            processedCount++;
                            byte2[1] = origin[originIndex];
                            byte2[0] = origin[originIndex + 1];
                            tempTimes = BitConverter.ToUInt16(byte2, 0);
                            for (int i = 0; i < tempTimes; i++) {
                                tempResult[resultIndex++] = origin[originIndex++];
                                processedCount++;
                            }

                            break;
                        case 0xC4:
                            tempResult[resultIndex++] = 0xC4;
                            originIndex++;
                            processedCount++;
                            byte2[1] = origin[originIndex];
                            byte2[0] = origin[originIndex + 1];
                            tempTimes = BitConverter.ToUInt16(byte2, 0);
                            for (int i = 0; i < tempTimes; i++) {
                                tempResult[resultIndex++] = origin[originIndex++];
                                processedCount++;
                            }

                            break;
                        case 0xDB:
                            tempResult[resultIndex++] = 0xDB;
                            originIndex++;
                            processedCount++;
                            byte2[1] = origin[originIndex];
                            byte2[0] = origin[originIndex + 1];
                            tempTimes = BitConverter.ToUInt16(byte2, 0);
                            for (int i = 0; i < tempTimes; i++) {
                                tempResult[resultIndex++] = origin[originIndex++];
                                processedCount++;
                            }

                            break;
                        case 0xDA:
                            tempResult[resultIndex++] = 0xDA;
                            tempResult[resultIndex++] = 0x00;
                            tempResult[resultIndex++] = 0x0C;
                            originIndex++;
                            processedCount++;
                            byte2[1] = origin[originIndex];
                            byte2[0] = origin[originIndex + 1];
                            tempTimes = BitConverter.ToUInt16(byte2, 0);
                            originIndex++;
                            processedCount++;
                            originIndex++;
                            for (int i = 2; i < tempTimes; i++) {
                                tempResult[resultIndex++] = origin[originIndex++];
                                processedCount++;
                            }

                            tempResult[resultIndex++] = 0x00;
                            tempResult[resultIndex++] = 0x3F;
                            tempResult[resultIndex++] = 0x00;
                            // 循环处理0xFFDA到0xFFD9之间所有的0xFF替换为0xFF00
                            for (; processedCount < inSize - 2;) {
                                if (origin[originIndex] == 0xFF) {
                                    tempResult[resultIndex++] = 0xFF;
                                    tempResult[resultIndex++] = 0x00;
                                    originIndex++;
                                    processedCount++;
                                    incrementSize++;
                                } else {
                                    tempResult[resultIndex++] = origin[originIndex++];
                                    processedCount++;
                                }
                            }

                            // 直接在这里写上了0xFFD9结束Jpeg图片.
                            incrementSize--; // 这里多了一个字节，所以减去。
                            resultIndex--;
                            tempResult[resultIndex--] = 0xD9;
                            break;
                        case 0xD9:
                            // 算法问题，这里不会被执行，但结果一样。
                            tempResult[resultIndex++] = 0xD9;
                            processedCount++;
                            break;
                    }
                }

                result = new byte[incrementSize + inSize];
                Array.Copy(tempResult, 0, result, 0, incrementSize + inSize);
            } catch {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     读取Unit数据
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        private static UnitData ReadUnitData(FileStream fs) {
            var buffer4 = new byte[4];
            fs.Read(buffer4, 0, 4);
            var flag = BitConverter.ToString(buffer4);
            fs.Read(buffer4, 0, 4);
            int size = BitConverter.ToInt32(buffer4, 0);
            var unitData = new UnitData(flag, size);
            fs.Read(unitData.Data, 0, unitData.Data.Length);
            return unitData;
        }

        /// <summary>
        ///     卸载资源
        /// </summary>
        public override void Unload() {
            Dispose();
        }

        #region Fields And Properties

        #region Map Data

        /// <summary>
        ///     地图文件名
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        ///     地图是否加载
        /// </summary>
        private bool _loaded;

        /// <summary>
        ///     地图标识
        /// </summary>
        private string _flag;

        /// <summary>
        ///     宽度
        /// </summary>
        private int _width;

        /// <summary>
        ///     高度
        /// </summary>
        private int _height;

        /// <summary>
        ///     unit列数量
        /// </summary>
        private int _unitColumns;

        /// <summary>
        ///     unit行数量
        /// </summary>
        private int _unitRows;

        /// <summary>
        ///     unit数量
        /// </summary>
        private int _unitSize;

        /// <summary>
        ///     unit偏移值
        /// </summary>
        private int[] _unitOffsets;

        /// <summary>
        ///     mask 标识
        /// </summary>
        private string _maskFlag;

        /// <summary>
        ///     mask 大小
        /// </summary>
        private int _maskSize;

        /// <summary>
        ///     mask 偏移值
        /// </summary>
        private int[] _maskOffsets;

        /// <summary>
        ///     遮罩层数据
        /// </summary>
        private byte[,] _masks;

        /// <summary>
        ///     地图可到达区域
        /// </summary>
        public byte[,] Grid;

        /// <summary>
        ///     地图图像
        /// </summary>
        public Bitmap Bitmap { get; private set; }

        /// <summary>
        ///     遮罩层数据
        /// </summary>
        public Bitmap MaskBitmap { get; private set; }

        /// <summary>
        ///     宽度
        /// </summary>
        public int Width => _width;

        /// <summary>
        ///     高度
        /// </summary>
        public int Height => _height;

        /// <summary>
        ///     为了显示
        /// </summary>
        public int MaxX { get; private set; }

        /// <summary>
        ///     为了显示
        /// </summary>
        public int MaxY { get; private set; }


        ///// <summary>
        /////     地图图像
        ///// </summary>
        //private Bitmap _bitmap;

        #endregion

        #endregion

    }

}

/************************************************************************
Map File Format
======================= Begin Map Head =======================
4字节     标识,值为:0x302E314D
4字节     地图的宽度
4字节     地图的高度
4*n字节   地图单元的索引,n=(地图的宽度/640*2) * (地图高度/480*2)
4字节     Mask 标识
4字节     Mask 大小
4*m字节   Mask 索引，m大小为：Mask 大小
======================= End Map Head =========================
======================= Begin Mask Data ======================
======================= Begin Mask ===========================
4字节     StartX
4字节     StartY
4字节     Width
4字节     Height
4字节     数据大小
4*s字节   s大小为：Size 大小
======================= End Mask =============================
======================= Begin Mask ===========================
...
======================= End Mask =============================
======================= End Mask Data ========================
======================= Begin Unit Data ======================
======================= Begin Unit ===========================
4字节 地图单元引索的开始位置。
n*4字节 n为上面的值，n为0时不存在。

4字节 GEPJ (JPEG)
4字节 大小
n字节 数据

4字节 LLEC (CELL)
4字节 大小
n字节 数据

4字节 BRIG (GIRB)
4字节 大小
n字节 数据

4字节 结束单元(0x00 0x00 0x00 0x00)。
======================= End Unit =============================
======================= Begin Unit ===========================
...
======================= End Unit =============================
======================= End Unit Data ========================
************************************************************************/