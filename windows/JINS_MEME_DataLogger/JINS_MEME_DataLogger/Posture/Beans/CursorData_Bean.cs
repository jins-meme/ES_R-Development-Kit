using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JINS_MEME_DataLogger
{
    public class CursorData_Bean
    {
        public enum CURSOR_STATE
        {
            CURSOR_INIT = 0,
            CURSOR_IDLE = 1,
            CURSOR_POINTING = 2,
            CURSOR_POINTING_BUTTON = 21,
            CURSOR_POINTING_DOUBLE_CLICK = 22,
            CURSOR_MOVING_READY = 32,
            CURSOR_MOVING = 30,
            CURSOR_DRAWING = 50,
        };


        private float[] grid=new float[2];
        private float[,] gridChange= new float[2,2];
        private float[] output=new float[2];
        private short[] outFx=new short[2];
        private CURSOR_STATE state = 0;
        private byte buttonPrev = 0;
        private uint sampleCount = 0;
        private uint buttonPushTimeThreshold = 0;
        private uint doubleClickTimeThreshold = 0;
        private uint pointingDetectTimeThreshold = 0;
        private float handJitterEnterThreshold = 0;
        private float handJitterExitThreshold = 0;

        public float[] Grid { get { return grid; } }
        public float[,] GridChange { get { return gridChange; } }
        public float[] Output { get { return output; } }
        public short[] OutFx { get { return outFx; } }
        public CURSOR_STATE State { get { return state; } set { state = value; } }
        public byte ButtonPrev { get { return buttonPrev; } set { buttonPrev = value; } }
        public uint SampleCount { get { return sampleCount; } set { sampleCount = value; } }
        public uint ButtonPushTimeThreshold { get { return buttonPushTimeThreshold; } set { buttonPushTimeThreshold = value; } }
        public uint DoubleClickTimeThreshold { get { return doubleClickTimeThreshold; } set { doubleClickTimeThreshold = value; } }
        public uint PointingDetectTimeThreshold { get { return pointingDetectTimeThreshold; } set { pointingDetectTimeThreshold = value; } }
        public float HandJitterEnterThreshold { get { return handJitterEnterThreshold; } set { handJitterEnterThreshold = value; } }
        public float HandJitterExitThreshold { get { return handJitterExitThreshold; } set { handJitterExitThreshold = value; } }


    }
}
