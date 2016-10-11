using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// 
    /// </summary>
    public class VertexInfoListConta
    {
        private List<VertexInfo_Bean> vertexInfoDim = new List<VertexInfo_Bean>();
        /// <summary>
        /// 頂点配列リスト
        /// </summary>
        private List<VertexInfo_Bean> vertexInfoList = new List<VertexInfo_Bean>();

        /// <summary>
        ///  頂点座標リスト
        /// </summary>
        private float[] vertexList = null;
        /// <summary>
        /// 法線ベクトルリスト
        /// </summary>
        private float[] normalList = null;
        /// <summary>
        /// 色リスト(３色)
        /// </summary>
        private float[] colorList3 = null;
        /// <summary>
        /// 色リスト(４色)
        /// </summary>
        private float[] colorList4 = null;
        /// <summary>
        /// インデックスリスト
        /// </summary>
        private byte[] indexnList = null;

        /// <summary>
        ///  頂点座標リスト
        /// </summary>
        public float[] VertexList { get { return vertexList; } }
        /// <summary>
        /// 法線ベクトルリスト
        /// </summary>
        public float[] NormalList { get { return normalList; } }
        /// <summary>
        /// 色リスト(３色)
        /// </summary>
        public float[] ColorList3 { get { return colorList3; } }
        /// <summary>
        /// 色リスト(４色)
        /// </summary>
        public float[] ColorList4 { get { return colorList4; } }
        /// <summary>
        /// インデックスリスト
        /// </summary>
        public byte[] IndexnList { get { return indexnList; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public　VertexInfoListConta() { }

        /// <summary>
        /// データ追加
        /// </summary>
        /// <param name="data">追加データ</param>
        /// <param name="clearFlag">リストクリアフラグ true:データクリア</param>
        public void AddItem(VertexInfo_Bean data,bool clearFlag)
        {
            if (clearFlag == true) 
            {
                vertexInfoList.Clear();
            }
            vertexInfoList.Add(data);
        }
        /// <summary>
        /// 色セット
        /// </summary>
        /// <param name="color"></param>
        public void SetColorAll(OpenGLColor_Bean color) 
        {
            foreach (VertexInfo_Bean item in vertexInfoDim) 
            {
                item.ColorR = color.Red;
                item.ColorG = color.Green;
                item.ColorB = color.Blue;
                item.ColorW = color.Alpha;
            }
            colorList3 = GetColor3List();
            colorList4 = GetColor4List();

        }
        /// <summary>
        /// 頂点番号リスト設定
        /// </summary>
        /// <param name="noList">頂点番号リスト</param>
        /// <param name="clearFlag">リストクリアフラグ true:データクリア</param>
        public void SeVertexNoList(int[] noList, bool clearFlag)
        {
            if (clearFlag == true)
            {
                vertexInfoDim.Clear();
            }
            for (int i = 0; i < noList.Length; i++) 
            {
                vertexInfoDim.Add(vertexInfoList[noList[i]]);
            }
            vertexList = GetPosList();
            normalList=GetNormalList();
            colorList3 = GetColor3List();
            colorList4 = GetColor4List();
            indexnList = GetIndexList();

        }

        /// <summary>
        /// 頂点座標リスト取得
        /// </summary>
        /// <returns>頂点座標リスト</returns>
        public float[] GetPosList() 
        {
            float[] result = new float[vertexInfoDim.Count * 3];
            int ix = 0;
            foreach (VertexInfo_Bean item in vertexInfoDim) 
            {
                result[ix] = item.Pos.X;
                result[ix+1] = item.Pos.Y;
                result[ix+2] = item.Pos.Z;
                ix += 3;
            }
            return result;
        }
        /// <summary>
        /// 法線ベクトルリスト取得
        /// </summary>
        /// <returns>法線ベクトルリスト</returns>
        public float[] GetNormalList()
        {
            float[] result = new float[vertexInfoDim.Count * 3];
            int ix = 0;
            foreach (VertexInfo_Bean item in vertexInfoDim)
            {
                result[ix] = item.Normal.X;
                result[ix + 1] = item.Normal.Y;
                result[ix + 2] = item.Normal.Z;
                ix += 3;
            }
            return result;
        }
        /// <summary>
        /// 色リスト（３色）取得
        /// </summary>
        /// <returns>色リスト</returns>
        public float[] GetColor3List()
        {
            float[] result = new float[vertexInfoDim.Count * 3];
            int ix = 0;
            foreach (VertexInfo_Bean item in vertexInfoDim)
            {
                result[ix] = item.ColorR;
                result[ix + 1] = item.ColorG;
                result[ix + 2] = item.ColorB;
                ix += 3;
            }
            return result;
        }
        /// <summary>
        /// 色リスト（４色）取得
        /// </summary>
        /// <returns>色リスト</returns>
        public float[] GetColor4List()
        {
            float[] result = new float[vertexInfoDim.Count * 4];
            int ix = 0;
            foreach (VertexInfo_Bean item in vertexInfoDim)
            {
                result[ix] = item.ColorR;
                result[ix + 1] = item.ColorG;
                result[ix + 2] = item.ColorB;
                result[ix + 3] = item.ColorW;
                ix += 4;
            }
            return result;
        }
        /// <summary>
        /// インデックスリスト取得
        /// </summary>
        /// <returns>インデックスリスト</returns>
        public byte[] GetIndexList()
        {
            byte[] result = new byte[vertexInfoDim.Count];
            for (int ix = 0; ix < vertexInfoDim.Count; ix++)
            {
                result[ix] = (byte)ix;
            }
            return result;
        }
        

    }
}
