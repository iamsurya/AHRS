using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AHRSTEST
{
    public partial class Form1 : Form
    {
        double sampleFreq = 15.0f;
        double beta = 0.041;
        double q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;	// quaternion of sensor frame relative to auxiliary frame
        double Gx = 0, Gy = 0, Gz = 0;
        double QT0, QT1, QT2, QT3;

        double InvGravX, InvGravY, InvGravZ, CalcGravX, CalcGravY, CalcGravZ; /* GravX[0] -> Invensense, GravX[1] -> Calculated */
        double InvRoll, InvPitch, InvYaw, CalcPitch, CalcRoll, CalcYaw;
        int AXCOL, AYCOL, AZCOL, GXCOL, GYCOL, GZCOL, MXCOL, MYCOL, MZCOL, Q0COL, Q1COL, Q2COL, Q3COL; /* Column Numbers. Switch between Actigraph and Shimmer */

        public Form1()
        {
            InitializeComponent();
        }

        /* Convert Quaternion to Euler https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Euler_Angles_from_Quaternion */
        void GetEuler(double _q0, double _q1, double _q2, double _q3, out double Roll, out double Pitch, out double Yaw)
        {
            Roll = Math.Atan2(2 * (_q0 * _q1 + _q2 * _q3), 1 - (2 * (_q1 * _q1 + _q2 * _q2))) * 180 / Math.PI;
            Pitch = Math.Asin(2 * (_q0 * _q2 - _q3 * _q1)) * 180 / Math.PI;
            Yaw = Math.Atan2(2 * (_q0 * _q3 + _q2 * _q1), 1 - (2 * (_q3 * _q3 + _q2 * _q2))) * 180 / Math.PI;
        }

        void GetGravity(double _q0, double _q1, double _q2, double _q3, out double GravX, out double GravY, out double GravZ)
        {
            double[,] R = new double[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
            double sq__q1 = 2 * _q1 * _q1;
            double sq__q2 = 2 * _q2 * _q2;
            double sq__q3 = 2 * _q3 * _q3;
            double _q1__q2 = 2 * _q1 * _q2;
            double _q3__q0 = 2 * _q3 * _q0;
            double _q1__q3 = 2 * _q1 * _q3;
            double _q2__q0 = 2 * _q2 * _q0;
            double _q2__q3 = 2 * _q2 * _q3;
            double _q1__q0 = 2 * _q1 * _q0;

            R[0, 0] = 1 - sq__q2 - sq__q3;
            R[0, 1] = _q1__q2 - _q3__q0;
            R[0, 2] = _q1__q3 + _q2__q0;
            R[1, 0] = _q1__q2 + _q3__q0;
            R[1, 1] = 1 - sq__q1 - sq__q3;
            R[1, 2] = _q2__q3 - _q1__q0;
            R[2, 0] = _q1__q3 - _q2__q0;
            R[2, 1] = _q2__q3 + _q1__q0;
            R[2, 2] = 1 - sq__q1 - sq__q2;

            /* Seperating gravity */
            GravX = R[2, 0];
            GravY = R[2, 1];
            GravZ = R[2, 2];
        }

        private void CalculateLinear(String[] values, out double lx, out double ly, out double lz)
        {

            Gx = double.Parse(values[GXCOL]);// * Math.PI / 180;
            Gy = double.Parse(values[GYCOL]);// * Math.PI / 180;
            Gz = double.Parse(values[GZCOL]);// * Math.PI / 180;

            double Ax = double.Parse(values[AXCOL]);// * -1 ;
            double Ay = double.Parse(values[AYCOL]);// * -1 ;
            double Az = double.Parse(values[AZCOL]);// * -1 ;

            double Mx = double.Parse(values[MXCOL]);
            double My = double.Parse(values[MYCOL]);
            double Mz = double.Parse(values[MZCOL]);

            MadgwickAHRSupdate(Gx, Gy, Gz, Ax, Ay, Az, Mx, My, Mz);

            //GetGravity(q0, q1, q2, q3, out CalcGravX, out CalcGravY, out CalcGravZ);

            GetGravity(double.Parse(values[Q0COL]), double.Parse(values[Q1COL]), double.Parse(values[Q2COL]), double.Parse(values[Q3COL]), out CalcGravX, out CalcGravY, out CalcGravZ);// out InvGravX, out InvGravY, out InvGravZ);


            /* Gravity for Actigraph */
            lx = Ax - CalcGravX;
            ly = Ay - CalcGravY;
            lz = Az - CalcGravZ;

            /* Axis manipulation for Actigraph */
            //lz = lz * -1;
            //double temp = -lx;
            //lx = ly;
            //ly = temp;

            /* This is old rotation between Shimmer and iPhone */
            ///* Removing gravity from Smoothed Signal */
            ///* Data Port facing inside */
            ///* RawData[0][Total_Data] = -(RawData[0][Total_Data] - gx);
            //RawData[1][Total_Data] = -(RawData[1][Total_Data] - gy);
            //RawData[2][Total_Data] = -(RawData[2][Total_Data] - gz); */

            ///* Data Port Facing Outside */
            //lx = -(Ax - CalcGravX);
            //ly = -(Ay - CalcGravY);
            //lz = -(Az - CalcGravZ);

            /* move data around for correct axes orientation in shimmer vs the iphone*/
            // if data port pointing away from hand. swap x and y
            lz = -lz;
            double temp = -lx;
            lx = -ly;
            ly = temp;
        }

        void MadgwickAHRSupdate(double gx, double gy, double gz, double ax, double ay, double az, double mx, double my, double mz)
        {
            double recipNorm;
            double s0, s1, s2, s3;
            double qDot1, qDot2, qDot3, qDot4;
            double hx, hy;// _8bx, _8bz;
            double _2q0mx, _2q0my, _2q0mz, _2q1mx, _2bx, _2bz, _4bx, _4bz, _2q0, _2q1, _2q2, _2q3, _2q0q2, _2q2q3, q0q0, q0q1, q0q2, q0q3, q1q1, q1q2, q1q3, q2q2, q2q3, q3q3;

            /* Convert gyro data from deg/sec to rad/sec */
            gx = gx * Math.PI / 180;
            gy = gy * Math.PI / 180;
            gz = gz * Math.PI / 180;

            // Use IMU algorithm if magnetometer measurement invalid (avoids NaN in magnetometer normalisation)
            if ((mx == 0.0f) && (my == 0.0f) && (mz == 0.0f))
            {
                MadgwickAHRSupdateIMU(gx, gy, gz, ax, ay, az);
                return;
            }

            // Rate of change of quaternion from gyroscope
            qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
            qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
            qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
            qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

            // Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
            if (!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f)))
            {

                // Normalise accelerometer measurement
                recipNorm = 1 / Math.Sqrt(ax * ax + ay * ay + az * az);
                ax *= recipNorm;
                ay *= recipNorm;
                az *= recipNorm;

                // Normalise magnetometer measurement
                recipNorm = 1 / Math.Sqrt(mx * mx + my * my + mz * mz);
                mx *= recipNorm;
                my *= recipNorm;
                mz *= recipNorm;

                // Auxiliary variables to avoid repeated arithmetic
                _2q0mx = 2.0f * q0 * mx;
                _2q0my = 2.0f * q0 * my;
                _2q0mz = 2.0f * q0 * mz;
                _2q1mx = 2.0f * q1 * mx;
                _2q0 = 2.0f * q0;
                _2q1 = 2.0f * q1;
                _2q2 = 2.0f * q2;
                _2q3 = 2.0f * q3;
                _2q0q2 = 2.0f * q0 * q2;
                _2q2q3 = 2.0f * q2 * q3;
                q0q0 = q0 * q0;
                q0q1 = q0 * q1;
                q0q2 = q0 * q2;
                q0q3 = q0 * q3;
                q1q1 = q1 * q1;
                q1q2 = q1 * q2;
                q1q3 = q1 * q3;
                q2q2 = q2 * q2;
                q2q3 = q2 * q3;
                q3q3 = q3 * q3;

                // Reference direction of Earth's magnetic field
                hx = mx * q0q0 - _2q0my * q3 + _2q0mz * q2 + mx * q1q1 + _2q1 * my * q2 + _2q1 * mz * q3 - mx * q2q2 - mx * q3q3;
                hy = _2q0mx * q3 + my * q0q0 - _2q0mz * q1 + _2q1mx * q2 - my * q1q1 + my * q2q2 + _2q2 * mz * q3 - my * q3q3;
                _2bx = Math.Sqrt(hx * hx + hy * hy);
                _2bz = -_2q0mx * q2 + _2q0my * q1 + mz * q0q0 + _2q1mx * q3 - mz * q1q1 + _2q2 * my * q3 - mz * q2q2 + mz * q3q3;
                _4bx = 2.0f * _2bx;
                _4bz = 2.0f * _2bz;
                
                // Gradient decent algorithm corrective step
                s0 = -_2q2 * (2.0f * q1q3 - _2q0q2 - ax) + _2q1 * (2.0f * q0q1 + _2q2q3 - ay) - _2bz * q2 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q3 + _2bz * q1) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q2 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                s1 = _2q3 * (2.0f * q1q3 - _2q0q2 - ax) + _2q0 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q1 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + _2bz * q3 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q2 + _2bz * q0) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q3 - _4bz * q1) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                s2 = -_2q0 * (2.0f * q1q3 - _2q0q2 - ax) + _2q3 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q2 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + (-_4bx * q2 - _2bz * q0) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q1 + _2bz * q3) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q0 - _4bz * q2) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                s3 = _2q1 * (2.0f * q1q3 - _2q0q2 - ax) + _2q2 * (2.0f * q0q1 + _2q2q3 - ay) + (-_4bx * q3 + _2bz * q1) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q0 + _2bz * q2) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q1 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);

                recipNorm = 1 / Math.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                // Apply feedback step
                qDot1 -= beta * s0;
                qDot2 -= beta * s1;
                qDot3 -= beta * s2;
                qDot4 -= beta * s3;
            }

            // Integrate rate of change of quaternion to yield quaternion
            q0 += qDot1 * (1.0f / sampleFreq);
            q1 += qDot2 * (1.0f / sampleFreq);
            q2 += qDot3 * (1.0f / sampleFreq);
            q3 += qDot4 * (1.0f / sampleFreq);

            // Normalise quaternion
            recipNorm = 1 / Math.Sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
            q0 *= recipNorm;
            q1 *= recipNorm;
            q2 *= recipNorm;
            q3 *= recipNorm;
        }

        //---------------------------------------------------------------------------------------------------
        // IMU algorithm update

        void MadgwickAHRSupdateIMU(double gx, double gy, double gz, double ax, double ay, double az)
        {
            double recipNorm;
            double s0, s1, s2, s3;
            double qDot1, qDot2, qDot3, qDot4;
            double _2q0, _2q1, _2q2, _2q3, _4q0, _4q1, _4q2, _8q1, _8q2, q0q0, q1q1, q2q2, q3q3;

            // Rate of change of quaternion from gyroscope
            qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
            qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
            qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
            qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

            // Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
            if (!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f)))
            {

                // Normalise accelerometer measurement
                recipNorm = 1 / Math.Sqrt(ax * ax + ay * ay + az * az);
                ax *= recipNorm;
                ay *= recipNorm;
                az *= recipNorm;

                // Auxiliary variables to avoid repeated arithmetic
                _2q0 = 2.0f * q0;
                _2q1 = 2.0f * q1;
                _2q2 = 2.0f * q2;
                _2q3 = 2.0f * q3;
                _4q0 = 4.0f * q0;
                _4q1 = 4.0f * q1;
                _4q2 = 4.0f * q2;
                _8q1 = 8.0f * q1;
                _8q2 = 8.0f * q2;
                q0q0 = q0 * q0;
                q1q1 = q1 * q1;
                q2q2 = q2 * q2;
                q3q3 = q3 * q3;

                // Gradient decent algorithm corrective step
                s0 = _4q0 * q2q2 + _2q2 * ax + _4q0 * q1q1 - _2q1 * ay;
                s1 = _4q1 * q3q3 - _2q3 * ax + 4.0f * q0q0 * q1 - _2q0 * ay - _4q1 + _8q1 * q1q1 + _8q1 * q2q2 + _4q1 * az;
                s2 = 4.0f * q0q0 * q2 + _2q0 * ax + _4q2 * q3q3 - _2q3 * ay - _4q2 + _8q2 * q1q1 + _8q2 * q2q2 + _4q2 * az;
                s3 = 4.0f * q1q1 * q3 - _2q1 * ax + 4.0f * q2q2 * q3 - _2q2 * ay;
                recipNorm = 1 / Math.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                // Apply feedback step
                qDot1 -= beta * s0;
                qDot2 -= beta * s1;
                qDot3 -= beta * s2;
                qDot4 -= beta * s3;
            }

            // Integrate rate of change of quaternion to yield quaternion
            q0 += qDot1 * (1.0f / sampleFreq);
            q1 += qDot2 * (1.0f / sampleFreq);
            q2 += qDot3 * (1.0f / sampleFreq);
            q3 += qDot4 * (1.0f / sampleFreq);

            // Normalise quaternion
            recipNorm = 1 / Math.Sqrt((q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3));
            q0 *= recipNorm;
            q1 *= recipNorm;
            q2 *= recipNorm;
            q3 *= recipNorm;
        }


        private void button1_Click(object sender, EventArgs e)
        {

            label1.Text = "Waiting";
            int DEBUGLEVEL = 0; /* 0 - Nothing to quats, 1 - only costheta, 2 - costheta and quaternions */

            /* Setup Columns for Actigraph */
            AXCOL = 1; AYCOL = 2; AZCOL = 3; GXCOL = 4; GYCOL = 5; GZCOL = 6; MXCOL = 7; MYCOL = 8; MZCOL = 9; Q3COL = Q2COL = Q1COL = Q0COL = 0; QT1 = QT2 = QT3 = QT0 = 0;

            /* Setup Columns for Shimmer */
            AXCOL = 10; AYCOL = 11; AZCOL = 12; GXCOL = 7; GYCOL = 8; GZCOL = 9; MXCOL = 13; MYCOL = 14; MZCOL = 15; Q0COL = 3; Q1COL = 4; Q2COL = 5; Q3COL = 6; QT1 = QT2 = QT3 = QT0 = 0;

            /* Setup the split character */
            char SplitChar = '\t';
            
            double CosTheta = 0;
            double lx, ly, lz;
            int LineNumber = 0;
            var values = "Banana,Lemon,Pie".Split(SplitChar);

            String OutputFileName = "C:\\temp\\P2001.txt";
            StreamWriter QuatWriter = new StreamWriter("C:\\temp\\quats.txt");// Users\\Surya\\Dropbox\\Education\\Eating Detection\\Quats" + beta.ToString("N4") + ".txt");
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            StreamReader reader;
            BinaryWriter DataWriter = new BinaryWriter(File.OpenWrite(OutputFileName));

            String InputFileName;
            // Set filter options and filter index.
            openFileDialog1.Filter = "CSV Files (.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            switch(DEBUGLEVEL)
            {
                case 1: QuatWriter.WriteLine("CosTheta");
                    break;
                case 2: QuatWriter.WriteLine("CosTheta\tInvQ0\tCalcQ0\tInvQ1\tCalcQ1\tInvQ2\tCalcQ2\tInvQ3\tCalcQ3\tInvGravX\tInvGravY\tInvGravZ\tCalcGravX\tCalcGravV\tCalcGravZ");
                    break;
                case 3:
                    QuatWriter.WriteLine("InvRoll\tInvPitch\tInvYaw\tCalcRoll\tCalcPitch\tCalcYaw");
                    break;
                default: break;
            }
            

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == DialogResult.OK)
            {
                InputFileName = openFileDialog1.FileName;
                
                
                reader = new StreamReader(File.OpenRead(InputFileName));

                /* Read first line with column titles */
                var line = reader.ReadLine();

                /* Read first line with column units */
                line = reader.ReadLine();
                line = reader.ReadLine();
                line = reader.ReadLine();
                line = reader.ReadLine();
                line = reader.ReadLine();

                /* Actigraph needs 10 */
                line = reader.ReadLine();
                line = reader.ReadLine();
                line = reader.ReadLine();
                line = reader.ReadLine();
                line = reader.ReadLine();

                values = line.Split(SplitChar);

                if (Q0COL != 0)
                {
                    QT0 = double.Parse(values[Q0COL]);
                    QT1 = double.Parse(values[Q1COL]);
                    QT2 = double.Parse(values[Q2COL]);
                    QT3 = double.Parse(values[Q3COL]);
                }


                bool condition = true; // Shimmmer
                
                /* Read actual lines */
                while (true)
                {
                    line = reader.ReadLine();
                    if (reader.EndOfStream)
                        break;

                    //condition = ((LineNumber % 7) == 0); // Actigraph

                    if (condition)  /* Read only the 7th sample to resample from 100Hz to 15 Hz */
                    {
                        values = line.Split(SplitChar);

                        if (Q0COL != 0)
                        {
                            QT0 = double.Parse(values[Q0COL]);
                            QT1 = double.Parse(values[Q1COL]);
                            QT2 = double.Parse(values[Q2COL]);
                            QT3 = double.Parse(values[Q3COL]);
                        }

                        CalculateLinear(values, out lx, out ly, out lz);

                        /* Actigraph rotation axis manipulation */
                        //Gz = Gz * -1;
                        //CosTheta = -Gx;
                        //Gx = Gy;
                        //Gy = CosTheta;

                        if(DEBUGLEVEL == 2) CosTheta = (q0 * QT0) + (q1 * QT1) + (q2 * QT2) + (q3 * QT3);
                        

                        DataWriter.Write((float)lx);
                        DataWriter.Write((float)ly);
                        DataWriter.Write((float)lz);
                        DataWriter.Write((float)Gx);
                        DataWriter.Write((float)Gy);
                        DataWriter.Write((float)Gz);

                        switch (DEBUGLEVEL)
                        {
                            case 1:
                                QuatWriter.WriteLine(CosTheta.ToString());
                                break;
                            case 2:
                                GetGravity(QT0, QT1, QT2, QT3, out InvGravX, out InvGravY, out InvGravZ);
                                QuatWriter.WriteLine(CosTheta + "\t" + QT0.ToString() + "\t" + q0.ToString() + "\t" + QT1.ToString() + "\t" + q1.ToString() + "\t" + QT2.ToString() + "\t" + q2.ToString() + "\t" +
                            QT3.ToString() + "\t" + q3.ToString() + "\t" + InvGravX.ToString() + "\t" + InvGravY.ToString() + "\t" + InvGravZ.ToString() + "\t" + CalcGravX.ToString() + "\t" + CalcGravY.ToString() + "\t" + CalcGravZ.ToString());
                                break;
                            case 3:
                                GetEuler(q0, q1, q2, q3, out CalcRoll, out CalcPitch, out CalcYaw);
                                GetEuler(QT0, QT1, QT2, QT3, out InvRoll, out InvPitch, out InvYaw);
                                // InvRoll\tInvPitch\tInvYaw\tCalcRoll\tCalcPitch\tCalcYaw");
                                QuatWriter.WriteLine(InvRoll.ToString() + "\t" + InvPitch.ToString() + "\t" + InvYaw.ToString() + "\t" + CalcRoll.ToString() + "\t" + CalcPitch.ToString() + "\t" + CalcYaw.ToString());
                                break;
                            default: break;
                        }                        
                    }

                    if (LineNumber == 99) LineNumber = 0;
                    else LineNumber++;
                }

            }

            DataWriter.Close();
            QuatWriter.Close();
            label1.Text = "Finished";
            }
    }
}
