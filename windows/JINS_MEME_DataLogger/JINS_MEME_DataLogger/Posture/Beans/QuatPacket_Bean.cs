using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace JINS_MEME_DataLogger
{
    public class QuatPacket_Bean
    {
        private const float CURSOR_UPDATE_GAIN_DRAWING  =0.8f;
        private const float CURSOR_UPDATE_GAIN_MOVING =  0.8f;
        private const float CURSOR_LEAKAGE_FACTOR = 0.1f;



        private long[] mlQuaternion=new long[4];
        private float[] quat=new float[4];
        private float[] quatPrev=new float[4];
        private float[] angle=new float[3];
        private float[,] grid=new float[3,2];
        private float[] gridChange=new float[3];
        private float[] gridLinear=new float[3];
        private float[] gridLinearAvg=new float[3];
        private float[] gridGain=new float[3];
        private float pitchSin=0;
        private byte button=0;
        private byte packetCnt=0;
        private byte packetCntPrev=0;
        private byte buttonTopBit=0;
        private byte buttonTopBitPrev=0;
        private CursorData_Bean cursor=new CursorData_Bean();

        public long[] MlQuaternion{get{return mlQuaternion;}}
        public float[] Quat{get{return quat;}}
        public float[] QuatPrev { get { return quatPrev; } }
        public float[] Angle { get { return angle; } }
        public float[,] Grid { get { return grid; } }
        public float[] GridChange { get { return gridChange; } }
        public float[] GridLinear { get { return gridLinear; } }
        public float[] GridLinearAvg { get { return gridLinearAvg; } }
        public float[] GridGain { get { return gridGain; } }
        float PitchSin { get { return pitchSin; } set { pitchSin = value; } }
        public byte Button { get { return button; } set { button = value; } }
        public byte PacketCnt { get { return packetCnt; } set { packetCnt = value; } }
        public byte PacketCntPrev { get { return packetCntPrev; } set { packetCntPrev = value; } }
        public byte ButtonTopBit{get{return buttonTopBit;} set{buttonTopBit=value;}}
        public byte ButtonTopBitPrev { get { return buttonTopBitPrev; } set { buttonTopBitPrev = value; } }
        private CursorData_Bean Cursor { get { return cursor; } }

        static byte[] mlButton = new byte[2];

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public QuatPacket_Bean() 
        {
            IMUquaternionInit();
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="sensData">センサーデータ保存ベースクラス</param>
        public QuatPacket_Bean(long[] qt) 
        {
            IMUquaternionInit();

            this.mlQuaternion[0] = (long)((qt[0] & 0xFFFF0000) >> 16);
            if (this.mlQuaternion[0] > 0x7FFF)
            {
                this.mlQuaternion[0] = this.mlQuaternion[0] - 0xFFFF;
            }

            this.mlQuaternion[1] = (long)((qt[1] & 0xFFFF0000) >> 16);
            if (this.mlQuaternion[1] > 0x7FFF)
            {
                this.mlQuaternion[1] = this.mlQuaternion[1] - 0xFFFF;
            }

            this.mlQuaternion[2] = (long)((qt[2] & 0xFFFF0000) >> 16);
            if (this.mlQuaternion[2] > 0x7FFF)
            {
                this.mlQuaternion[2] = this.mlQuaternion[2] - 0xFFFF;
            }

            this.mlQuaternion[3] = (long)((qt[3] & 0xFFFF0000) >> 16);
            if (this.mlQuaternion[3] > 0x7FFF)
            {
                this.mlQuaternion[3] = this.mlQuaternion[3] - 0xFFFF;
            }

            this.quat[0] = (float)(this.mlQuaternion[0] / 16384.0f);
            this.quat[1] = (float)(this.mlQuaternion[1] / 16384.0f);
            this.quat[2] = (float)(this.mlQuaternion[2] / 16384.0f);
            this.quat[3] = (float)(this.mlQuaternion[3] / 16384.0f);

            this.button = 0;  // 用途不明
            if( this.mlQuaternion[1] != 0 )
            {
                IMUgetQuaternion();
            }


        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: IMUquaternionInit                                   */
        /*                                                                    */
        /*   Description: Data structure initialization                       */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void IMUquaternionInit()
        {
            int i;
            for (i = 0; i < 3; i++)
            {
                this.gridLinear[i] = 0;
                this.gridGain[i] = CURSOR_UPDATE_GAIN_MOVING;
                this.gridLinearAvg[i] = 0;
            }
            for (i = 0; i < 2; i++)
            {
                this.cursor.Grid[i] = 0;
            }

            this.cursor.State =CursorData_Bean.CURSOR_STATE.CURSOR_INIT;
            this.cursor.ButtonPushTimeThreshold = 20; //5;   // delay 700 sample count for cursor display
            this.cursor.DoubleClickTimeThreshold = 100; //20;   // delay 500 sample count for cursor display
            this.cursor.PointingDetectTimeThreshold = 100;
            this.cursor.HandJitterEnterThreshold = 0.003f; // radius
            this.cursor.HandJitterExitThreshold = 0.0006f; // radius
            this.cursor.SampleCount = 0;

            this.buttonTopBit = 0;
            this.buttonTopBitPrev = 0;
        }

        private float  mEULAR_ANGLE_Z_MOUSE(float[] q)	
        { 
            return (float)Math.Atan2( 1-2*(q[1]*q[1]+q[2]*q[2]),  2*(q[1]*q[3]-q[0]*q[2]));
        }
        private float  mEULAR_ANGLE_Y_MOUSE(float[] q)	
        { 
            return (float)Math.Asin( 2*(q[0]*q[1]+q[2]*q[3]) );
        }
        private float mEULAR_ANGLE_X_MOUSE(float[] q)
        {
            return (float)Math.Atan2( 1-2*(q[1]*q[1]+q[3]*q[3]), 2*(q[1]*q[2]-q[0]*q[3]) ) ;
        }

        private void IMUgetQuaternion()
        {
            int i;


            // Euler angle from quaternion
            this.angle[0] = (float) mEULAR_ANGLE_X_MOUSE(this.quat);
            this.pitchSin = 2* (this.quat[0]*this.quat[1]+this.quat[2]*this.quat[3]);
            if (this.pitchSin>1.0f)
	        {
                this.pitchSin=1.0f;
	        }
            if (this.pitchSin<-1.0f)
	        {
                this.pitchSin=-1.0f;
	        }

            this.angle[1] = (float)Math.Asin(this.pitchSin);
            this.angle[2] = (float )mEULAR_ANGLE_Z_MOUSE(this.quat);

            // grid calculation for Yaw
            if (Math.Abs(this.Angle[1]) <= 1.0f)
            {
                CalcLinearGridByEuler();
            }
            else
            {
                CalcLinearGridByQuaternion();
            }

            for (i=0; i<3; i++)
            {
                this.gridLinearAvg[i] = (2*this.gridLinearAvg[i] + this.gridLinear[i])/3;
            }
            //MouseFrictionProcess();
            //UpdateMouseButton();
        //#ifdef ML_DATA_LOG
        //    mlDataLogProcess();
        //#endif

        //#if 0

        //    printf("state: %2d  x y:%f %f  changes: %f %f\n",
        //    this.cursor.state,
        //    this.gridLinear[0],
        //    this.gridLinear[1],
        //    this.gridChange[0],
        //    this.gridChange[1]);
        //#endif

            for (i=0; i<4; i++)
            {
                this.quatPrev[i] = this.quat[i];
            }

        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: CalcLinearGridByEuler                               */
        /*                                                                    */
        /*   Description: Calculate grid movement by Euler angle              */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void CalcLinearGridByEuler()
        {
            this.grid[0, 0] = this.angle[0];
            this.gridChange[0] = this.grid[0, 0] - this.grid[0, 1];
            this.grid[0, 1] = this.grid[0, 0];
            if (this.gridChange[0] > 6.0)
            {
                this.gridLinear[0] += (float)(this.gridChange[0] - 6.28);
            }
            else if (this.gridChange[0] < -6.0)
            {
                this.gridLinear[0] += (float)(this.gridChange[0] + 6.28);
            }
            else
            {
                this.gridLinear[0] += this.gridChange[0];
            }

            // grid calculation for Pitch
            this.grid[1, 0] = this.angle[1];
            this.gridChange[1] = this.grid[1, 0] - this.grid[1, 1];
            this.grid[1, 1] = this.grid[1, 0];
            this.gridLinear[1] = this.angle[1];

            // grid calculation for Roll
            this.grid[2, 0] = this.angle[2];
            gridChange[2] = this.grid[2, 0] - this.grid[2, 1];
            this.grid[2, 1] = this.grid[2, 0];
            if (this.gridChange[2] > 6.0)
            {
                this.gridLinear[2] += (float)(this.gridChange[2] - 6.28);
            }
            else if (this.gridChange[1] < -6.0)
            {
                this.gridLinear[2] += (float)(this.gridChange[2] + 6.28);
            }
            else
            {
                this.gridLinear[2] += this.gridChange[2];
            }
        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: CalcLinearGridByQuaternion                          */
        /*                                                                    */
        /*   Description: Calculate grid movement by Quaternion               */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        void CalcLinearGridByQuaternion()
        {
            float[] quatDelta = new float[4];
            float[] quatInv = new float[4];

            MLQInvert(this.quatPrev, ref quatInv);
            MLQMult(quatInv, this.quat, ref quatDelta);

            this.gridChange[0] = (float)mEULAR_ANGLE_X_MOUSE(quatDelta) - 1.5707963267948966192313216916398f;
            this.pitchSin = 2 * (quatDelta[0] * quatDelta[1] + quatDelta[2] * quatDelta[3]);
            if (this.pitchSin > 1.0f)
            {
                this.pitchSin = 1.0f;
            }
            if (this.pitchSin < -1.0f)
            {
                this.pitchSin = -1.0f;
            }

            this.gridChange[1] = (float)Math.Asin(this.pitchSin);
            this.gridChange[2] = (float)mEULAR_ANGLE_Z_MOUSE(quatDelta);

            this.gridLinear[0] += this.gridChange[0];
            this.gridLinear[1] += this.gridChange[1];
            this.gridLinear[2] += this.gridChange[2];
        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: MLQInvert                                           */
        /*                                                                    */
        /*   Description: Calculate inverse of Quaternion                     */
        /*                                                                    */
        /*    Parameters: q - original quaternion                             */
        /*                qInverted - inverse of quaternion                   */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        void MLQInvert(float[] q, ref float[] qInverted)
        {
            qInverted[0] = q[0];
            qInverted[1] = -q[1];
            qInverted[2] = -q[2];
            qInverted[3] = -q[3];
        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: MLQMult                                             */
        /*                                                                    */
        /*   Description: Quaternion multiplication                           */
        /*                                                                    */
        /*    Parameters: q1 - multiplicand                                   */
        /*                q2 - multiplicator                                  */
        /*                qProd - multiplication result                       */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void MLQMult(float[] q1, float[] q2, ref float[] qProd)
        {
            qProd[0] = (q1[0] * q2[0] - q1[1] * q2[1] - q1[2] * q2[2] - q1[3] * q2[3]);
            qProd[1] = (q1[0] * q2[1] + q1[1] * q2[0] + q1[2] * q2[3] - q1[3] * q2[2]);
            qProd[2] = (q1[0] * q2[2] - q1[1] * q2[3] + q1[2] * q2[0] + q1[3] * q2[1]);
            qProd[3] = (q1[0] * q2[3] + q1[1] * q2[2] - q1[2] * q2[1] + q1[3] * q2[0]);
        }

        /**********************************************************************/
        /*                                                                    */
        /* Function Name: MouseFrictionProcess                                */
        /*                                                                    */
        /*   Description: Mouse movement process                              */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        //private void MouseFrictionProcess()
        //{
        //    int i;
        //    for( i = 0; i < 2; i++ )
        //    {
        //        this.cursor.GridChange[i, 0] = this.gridLinearAvg[i] - this.cursor.Grid[i];
        //        this.cursor.Output[i] = this.cursor.GridChange[i,0];
        //        this.cursor.GridChange[i, 1] = this.cursor.GridChange[i,0];
        //        this.cursor.OutFx[i] = 0;
        //    }

        //    this.cursor.SampleCount++;

        //    switch (this.cursor.State)
        //    {
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_INIT:
        //            if( GetCursorMovingStatusDuringMoving() ==1)
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_MOVING;
        //            }
        //            else
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_POINTING;
        //            }
        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_POINTING:
        //            // add some friction
        //            UpdateMouseCursorPointing(CURSOR_LEAKAGE_FACTOR);
        //            if( this.button == 1)
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_POINTING_BUTTON;
        //                this.cursor.SampleCount = 0;
        //                this.cursor.ButtonPrev = this.button;
        //            }

        //            if( GetCursorMovingStatusDuringPointing()==1 )
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_MOVING;
        //            }
        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_POINTING_BUTTON:
        //            // add some friction
        //            UpdateMouseCursorPointing(1.0f);

        //            if( (this.button & 0x01) == 0x01 )
        //            {
        //                if( this.cursor.SampleCount >= this.cursor.ButtonPushTimeThreshold )
        //                {
        //                    this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_DRAWING;
        //                    this.cursor.SampleCount = 0;
        //                    for( i = 0; i < 2; i++ )
        //                    {
        //                        this.gridGain[i] = CURSOR_UPDATE_GAIN_DRAWING;
        //                    }
        //                }
        //            }
        //            else if( this.button == 0 )
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_POINTING_DOUBLE_CLICK;
        //                this.cursor.SampleCount = 0;
        //            }
        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_POINTING_DOUBLE_CLICK:
        //            // add some friction
        //            UpdateMouseCursorPointing(1.0f);
        //            if( this.cursor.SampleCount >= this.cursor.DoubleClickTimeThreshold )
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_INIT;
        //            }
        //            else
        //            {
        //                if( this.button == this.cursor.ButtonPrev )
        //                {
        //                    this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_INIT;
        //                    this.cursor.SampleCount = 0;
        //                }
        //            }
        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_MOVING:
        //            if( GetCursorMovingStatusDuringMoving() != 1)
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_MOVING_READY;
        //                this.cursor.SampleCount = 0;

        //            }
        //            if( (this.button & 0x01) == 0x01 )
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_DRAWING;
        //                this.cursor.SampleCount = 0;
        //                for( i = 0; i < 2; i++ )
        //                {
        //                    this.gridGain[i] = CURSOR_UPDATE_GAIN_DRAWING;
        //                }

        //            }
        //            UpdateMouseCursorDrawing();
        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_MOVING_READY:
        //            if( GetCursorMovingStatusDuringMoving() ==1)
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_MOVING;
        //            }
        //            else if( this.cursor.SampleCount >= this.cursor.PointingDetectTimeThreshold )
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_POINTING;
        //            }
        //            if( this.button ==1)
        //            {
        //                this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_POINTING_BUTTON;
        //                this.cursor.SampleCount = 0;
        //            }

        //            UpdateMouseCursorDrawing();

        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_DRAWING:
        //            if( (this.button & 0x01) == 0 )
        //            {
        //                for( i = 0; i < 2; i++ )
        //                {
        //                    this.gridGain[i] = CURSOR_UPDATE_GAIN_MOVING;
        //                    this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_POINTING;
        //                }
        //            }
        //            UpdateMouseCursorDrawing();
        //            break;
        //        case CursorData_Bean.CURSOR_STATE.CURSOR_IDLE:
        //            break;
        //        default:
        //            this.cursor.State = CursorData_Bean.CURSOR_STATE.CURSOR_INIT;
        //    }
        //}
        /**********************************************************************/
        /*                                                                    */
        /* *Function Name: GetCursorMovingStatusDuringMoving                   */
        /*                                                                    */
        /*   Description: Check cursor moving or not on moving mode           */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns: 1: moving 0: still                                  */
        /*                                                                    */
        /**********************************************************************/
        //private byte GetCursorMovingStatusDuringMoving()
        //{
        //    byte status;

        //    if( (Math.Abs(this.gridChange[0]) >= this.cursor.HandJitterEnterThreshold)
        //        || (Math.Abs(this.gridChange[1]) >= this.cursor.HandJitterEnterThreshold) )
        //    {
        //        status = 1;
        //    }
        //    else
        //    {
        //        status = 0;
        //    }

        //    return status;
        //}
        /**********************************************************************/
        /*                                                                    */
        /* *Function Name: GetCursorMovingStatusDuringPointing                 */
        /*                                                                    */
        /*   Description: Check cursor moving or not on point mode            */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns: 1: moving 0: still                                  */
        /*                                                                    */
        /**********************************************************************/
        //private byte GetCursorMovingStatusDuringPointing()
        //{
        //    byte status;

        //    if( (Math.Abs(this.cursor.Output[0]) >= this.cursor.HandJitterExitThreshold)
        //        || (Math.Abs(this.cursor.Output[1]) >= this.cursor.HandJitterExitThreshold) )
        //    {
        //        status = 1;
        //    }
        //    else
        //    {
        //        status = 0;
        //    }

        //    return status;
        //}


        /**********************************************************************/
        /*                                                                    */
        /* *Function Name: UpdateMouseCursorDrawing                            */
        /*                                                                    */
        /*   Description: Output mouse cursor movement                        */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        //private void UpdateMouseCursorDrawing()
        //{
        //    int i;

        //    //Get current mouse position
        //    Point tmpPt ={ 0, 0};
        //    GetCursorPos(&tmpPt);
        //    for (i = 0; i < 2; i++)
        //    {
        //        // final output in fixed point grid change format
        //        this.cursor.OutFx[i] = (short)(this.cursor.Output[i] * this.GridGain[i] * 2048.0f);
        //        this.cursor.Grid[i] += ((float)this.cursor.OutFx[i]) / (this.gridGain[i] * 2048.0f);
        //    }

        //    SetCursorPos(-this.cursor.OutFx[0] + tmpPt.X, -this.cursor.OutFx[1]  + tmpPt.Y);
        //}
        /**********************************************************************/
        /*                                                                    */
        /* *Function Name: UpdateMouseCursorPointing                           */
        /*                                                                    */
        /*   Description: Calculate grid changing on point mode               */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                leakFactor - facter to coontrol grid changing       */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        //private void UpdateMouseCursorPointing(float leakFactor)
        //{
        //    int i;

        //    // saturate the leakFactor
        //    if (leakFactor > 1.0f)
        //        leakFactor = 1.0f;
        //    else if (leakFactor < 0)
        //        leakFactor = 0;

        //    for (i = 0; i < 2; i++)
        //    {
        //        this.cursor.Grid[i] += (float)(this.cursor.Output[i] * leakFactor);
        //    }

        //}
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: UpdateMouseButton                                   */
        /*                                                                    */
        /*   Description: Handle mouse buttons                                */
        /*                                                                    */
        /*    Parameters: qData - data structure pointer                      */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        //private void UpdateMouseButton()
        //{
        //    mlButton[0] = 0;
        //    mlButton[1] = 0;
        //    Point tmpPt = new Point( 0, 0 );
        //    GetCursorPos(&tmpPt);

        //    //Button 1
        //    if( mlButton[0] !=1 && (this.button & 0x01)==1 )
        //    {
        //        // Simulate a left button press
        //        mouse_event(MOUSEEVENTF_LEFTDOWN, tmpPt.X, tmpPt.Y, 0, 0);
        //        mlButton[0] = 1;

        //    }
        //    else if( mlButton[0]==1  && (this.button & 0x01) !=1 )
        //    {
        //        // Simulate a left button release
        //        mouse_event(MOUSEEVENTF_LEFTUP, tmpPt.X, tmpPt.Y, 0, 0);
        //        mlButton[0] = 0;
        //    }

        //    //Button 2
        //    if( mlButton[1]!=1 && (this.button & 0x02)==2 )
        //    {
        //        // Simulate a right button press
        //        mouse_event(MOUSEEVENTF_RIGHTDOWN, tmpPt.X, tmpPt.Y, 0, 0);
        //        mlButton[1] = 1;
        //    }
        //    else if( mlButton[1]==1 && (this.button & 0x02)!=2 )
        //    {
        //        // Simulate a right button release
        //        mouse_event(MOUSEEVENTF_RIGHTUP, tmpPt.X, tmpPt.Y, 0, 0);
        //        mlButton[1] = 0;
        //    }
        //}

    }
}
