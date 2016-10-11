using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    public class VertexInfo_Bean
    {
        /// <summary>
        /// 頂点座標
        /// </summary>
        private OpenGL3D_Bean pos = new OpenGL3D_Bean();
        /// <summary>
        /// 法線ベクトル
        /// </summary>
        private OpenGL3D_Bean normal = new OpenGL3D_Bean();

        /// <summary>
        /// 色(R)
        /// </summary>
        private float colorR = 0;
        /// <summary>
        /// 色(G)
        /// </summary>
        private float colorG = 0;
        /// <summary>
        /// 色(B)
        /// </summary>
        private float colorB = 0;
        /// <summary>
        /// 色(w　透明度)
        /// </summary>
        private float colorW = 0;

        /// <summary>
        /// 頂点座標
        /// </summary>
        public OpenGL3D_Bean Pos { get { return pos; } }
        /// <summary>
        /// 法線ベクトル
        /// </summary>
        public OpenGL3D_Bean Normal { get { return normal; } }

        /// <summary>
        /// 色(R)
        /// </summary>
        public float ColorR { get { return colorR; } set { colorR = value; } }
        /// <summary>
        /// 色(G)
        /// </summary>
        public float ColorG { get { return colorG; } set { colorG = value; } }
        /// <summary>
        /// 色(B)
        /// </summary>
        public float ColorB { get { return colorB; } set { colorB = value; } }
        /// <summary>
        /// 色(w　透明度)
        /// </summary>
        public float ColorW { get { return colorW; } set { colorW = value; } }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public VertexInfo_Bean() { }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="posZ"></param>
        /// <param name="normalX"></param>
        /// <param name="normalY"></param>
        /// <param name="normalZ"></param>
        /// <param name="colorR"></param>
        /// <param name="colorG"></param>
        /// <param name="colorB"></param>
        /// <param name="colorW"></param>
        public VertexInfo_Bean(float posX, float posY, float posZ, float normalX, float normalY, float normalZ, float colorR, float colorG, float colorB, float colorW) 
        {
            this.pos.X = posX;
            this.pos.Y = posY;
            this.pos.Z = posZ;
            this.normal.X = normalX;
            this.normal.Y = normalY;
            this.normal.Z = normalZ;
            this.colorR = colorR;
            this.colorG = colorG;
            this.colorB = colorB;
            this.colorW = colorW;
        }
        

    }
}
