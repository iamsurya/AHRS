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
        enum Devices {Invensense, Actigraph, iPhone};

        double sampleFreq = 15.0f;
        double beta = 100;
        double q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;	// quaternion of sensor frame relative to auxiliary frame
        double Gx = 0, Gy = 0, Gz = 0;
        double QT0, QT1, QT2, QT3;

        double InvGravX, InvGravY, InvGravZ, CalcGravX, CalcGravY, CalcGravZ; /* GravX[0] -> Invensense, GravX[1] -> Calculated */

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbDevices.DataSource = System.Enum.GetValues(typeof(Devices));
        }

        double InvRoll, InvPitch, InvYaw, CalcPitch, CalcRoll, CalcYaw;
        int AXCOL, AYCOL, AZCOL, GXCOL, GYCOL, GZCOL, MXCOL, MYCOL, MZCOL, Q0COL, Q1COL, Q2COL, Q3COL; /* Column Numbers. Switch between Actigraph and Shimmer */

        Devices CurrentDevice;

        StreamWriter QuatDebugger;

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


        private void OrientAxes(Devices DeviceID, ref double lx, ref double ly, ref double lz, ref double gx, ref double gy, ref double gz)
        {
            double temp = 0.00;

            switch (DeviceID)
            {
                case Devices.Invensense:
                    // Rotate Accelerations
                    lz = -lz;
                    temp = -lx;
                    lx = -ly;
                    ly = temp;
                    // Rotate Gyroscope
                    gz = -gz;
                    temp = -gx;
                    gx = gy;
                    gy = temp;
                    break;
                case Devices.Actigraph: 
                    // Rotate Acceleration
                        lz = lz * -1;
                        temp = -lx;
                        lx = ly;
                        ly = temp;
                    // Rotate Angular Velocities
                        gz = gz * -1;
                        temp = -gx;
                        gx = gy;
                        gy = temp;
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Calculates Linear Acceleration from Values provided from a CSV file
        /// </summary>
        /// <param name="values">String Array containing different values</param>
        /// <param name="lx">Output lx</param>
        /// <param name="ly">Output lx</param>
        /// <param name="lz">Output lx</param>
        /// <param name="CalculateQuaternion">Calculate Quaternion from values or use Invensense ones? 1 = Calculate them, 0 = Use Invensense </param>
        private void CalculateLinear(String[] values, out double lx, out double ly, out double lz, Int16 CalculateQuaternion)
        {

            Gx = double.Parse(values[GXCOL]);
            Gy = double.Parse(values[GYCOL]);
            Gz = double.Parse(values[GZCOL]);

            double Ax = double.Parse(values[AXCOL]);
            double Ay = double.Parse(values[AYCOL]);
            double Az = double.Parse(values[AZCOL]);

            double Mx = double.Parse(values[MXCOL]);
            double My = double.Parse(values[MYCOL]);
            double Mz = double.Parse(values[MZCOL]);

            if (CalculateQuaternion == 1)
            {
                MadgwickAHRSupdate(Gx, Gy, Gz, Ax, Ay, Az, Mx, My, Mz);
                GetGravity(q0, q1, q2, q3, out CalcGravX, out CalcGravY, out CalcGravZ);
            }
            else
            {
                GetGravity(double.Parse(values[Q0COL]), double.Parse(values[Q1COL]), double.Parse(values[Q2COL]), double.Parse(values[Q3COL]), out CalcGravX, out CalcGravY, out CalcGravZ);// out InvGravX, out InvGravY, out InvGravZ);
            }

            /* linear acceleration */
            lx = Ax - CalcGravX;
            ly = Ay - CalcGravY;
            lz = Az - CalcGravZ;
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

                recipNorm = (double)1.00 / (double)Math.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                //QuatDebugger.Write(beta + "\t" + qDot1 + "\t" + s0 + "\t" + qDot2 + "\t" + s1 + "\t" + qDot3 + "\t" + s2 + "\t" + qDot4 + "\t" + s3 + "\t");
                
                // Apply feedback step
                qDot1 -= beta * s0;
                qDot2 -= beta * s1;
                qDot3 -= beta * s2;
                qDot4 -= beta * s3;

                //QuatDebugger.Write(qDot1 + "\t" + qDot2 + "\t" + qDot3 + "\t" + qDot4 + "\t");

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

            GetEuler(q0, q1, q2, q3, out CalcRoll, out CalcPitch, out CalcYaw);
            QuatDebugger.WriteLine(CalcRoll + "\t" + CalcPitch + "\t" + CalcYaw);

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


        char SplitChar;
        int DEBUGLEVEL;
        double lx, ly, lz;
        String OutputFileName;
        StreamWriter QuatWriter;
        StreamReader reader;
        BinaryWriter DataWriter;

        private void button2_Click(object sender, EventArgs e)
        {
            DEBUGLEVEL = cmbDebugLevel.SelectedIndex;
            int LineNumber = 0;
            CurrentDevice = (Devices)System.Enum.Parse(typeof(Devices), cmbDevices.SelectedValue.ToString());
            double[] Betas = { 0.8, 0.9, 1.0, 1.1, 1.2 };
            label1.Text = "Waiting";
            var values = "Banana,Lemon,Pie".Split(SplitChar);

            OutputFileName = "C:\\temp\\P2001.txt";
            QuatWriter = new StreamWriter("C:\\temp\\quats.txt");// Users\\Surya\\Dropbox\\Education\\Eating Detection\\Quats" + beta.ToString("N4") + ".txt");
//            QuatDebugger = new StreamWriter("C:\\temp\\quatdebug.txt");// Users\\Surya\\Dropbox\\Education\\Eating Detection\\Quats" + beta.ToString("N4") + ".txt");
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            String InputFileName;
            openFileDialog1.Filter = "CSV Files (.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            switch (CurrentDevice)
            {
                case Devices.Invensense:
                    /* Setup Columns for Shimmer */
                    AXCOL = 10; AYCOL = 11; AZCOL = 12; GXCOL = 7; GYCOL = 8; GZCOL = 9; MXCOL = 13; MYCOL = 14; MZCOL = 15; Q0COL = 3; Q1COL = 4; Q2COL = 5; Q3COL = 6; QT1 = QT2 = QT3 = QT0 = 0;
                    /* Setup the split character */
                    SplitChar = '\t';
                    break;
                case Devices.Actigraph:
                    /* Setup Columns for Actigraph */
                    AXCOL = 1; AYCOL = 2; AZCOL = 3; GXCOL = 4; GYCOL = 5; GZCOL = 6; MXCOL = 7; MYCOL = 8; MZCOL = 9; Q3COL = Q2COL = Q1COL = Q0COL = 0; QT1 = QT2 = QT3 = QT0 = 0;
                    SplitChar = ',';
                    break;
                case Devices.iPhone:
                    break;
            }

            //QuatDebugger.WriteLine("Beta\tQ0\tS0\tQ1\tS1\tQ2\tS2\tQ3\tS3\tYaw\tPitch\tRoll");

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == DialogResult.OK)
            {
                InputFileName = openFileDialog1.FileName;


                reader = new StreamReader(File.OpenRead(InputFileName));

                /* Read first line with column titles */
                var line = reader.ReadLine();

                reader.Close();

                if (CurrentDevice == Devices.Invensense)
                {
                    QT0 = double.Parse(values[Q0COL]);
                    QT1 = double.Parse(values[Q1COL]);
                    QT2 = double.Parse(values[Q2COL]);
                    QT3 = double.Parse(values[Q3COL]);
                }

                for(int i = 0; i < Betas.Length; i++)
                {
                    q0 = 1;
                    q1 = q2 = q3 = 0;
                    beta = Betas[i];
                    QuatDebugger = new StreamWriter("C:\\temp\\ActiGraphYPR" + beta.ToString("N4") +".txt");
                    QuatDebugger.WriteLine("Roll\tPitch\tYaw");
                    reader = new StreamReader(File.OpenRead(InputFileName));

                    /* Read first line with column units */
                    for(int j = 0; j<12; j++)
                        line = reader.ReadLine();

                    /* Read actual lines */
                    while (true)
                    {
                        line = reader.ReadLine();
                        if (reader.EndOfStream)
                            break;

                        if ((((LineNumber % 7) == 0) && (CurrentDevice == Devices.Actigraph)) || (CurrentDevice == Devices.Invensense))  /* Read only the 7th sample to resample from 100Hz to 15 Hz */
                        {
                            values = line.Split(SplitChar);

                            CalculateLinear(values, out lx, out ly, out lz, 1);
                        }


                        if (LineNumber == 99) LineNumber = 0;
                        else LineNumber++;
                    }
                    QuatDebugger.Close();
                    reader.Close();
                }

            }

            QuatWriter.Close();
            
            label1.Text = "Finished";


        }

        private void button1_Click(object sender, EventArgs e)
        {
            var values = "Banana,Lemon,Pie".Split(SplitChar);

            label1.Text = "Waiting";
            CurrentDevice = (Devices) System.Enum.Parse(typeof(Devices), cmbDevices.SelectedValue.ToString());
            SplitChar = '\t';
            DEBUGLEVEL = 0; /* 0 - Nothing to quats, 1 - only costheta, 2 - costheta and quaternions */
            double CosTheta = 0;
            beta = double.Parse(tbBetaVal.Text);
            int LineNumber = 0;
            
            OutputFileName = "C:\\temp\\P2001.txt";
            QuatWriter = new StreamWriter("C:\\temp\\quats.txt");// Users\\Surya\\Dropbox\\Education\\Eating Detection\\Quats" + beta.ToString("N4") + ".txt");
            QuatDebugger = new StreamWriter(tbDebugFile.Text);// Users\\Surya\\Dropbox\\Education\\Eating Detection\\Quats" + beta.ToString("N4") + ".txt");
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DataWriter = new BinaryWriter(File.OpenWrite(OutputFileName));

            String InputFileName;
            openFileDialog1.Filter = "CSV Files (.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            switch (CurrentDevice)
            {
                case Devices.Invensense:
                    /* Setup Columns for Shimmer */
                    AXCOL = 10; AYCOL = 11; AZCOL = 12; GXCOL = 7; GYCOL = 8; GZCOL = 9; MXCOL = 13; MYCOL = 14; MZCOL = 15; Q0COL = 3; Q1COL = 4; Q2COL = 5; Q3COL = 6; QT1 = QT2 = QT3 = QT0 = 0;
                    /* Setup the split character */
                    SplitChar = '\t';
                    break;
                case Devices.Actigraph:
                    /* Setup Columns for Actigraph */
                    AXCOL = 1; AYCOL = 2; AZCOL = 3; GXCOL = 4; GYCOL = 5; GZCOL = 6; MXCOL = 7; MYCOL = 8; MZCOL = 9; Q3COL = Q2COL = Q1COL = Q0COL = 0; QT1 = QT2 = QT3 = QT0 = 0;
                    SplitChar = ',';
                    break;
                case Devices.iPhone:
                    break;
            }
            
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

            QuatDebugger.WriteLine("tQ0\tS0\tQ1\tS1\tQ2\tS2\tQ3\tS3\tYaw\tPitch\tRoll");

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

                if (CurrentDevice == Devices.Invensense)
                {
                    QT0 = double.Parse(values[Q0COL]);
                    QT1 = double.Parse(values[Q1COL]);
                    QT2 = double.Parse(values[Q2COL]);
                    QT3 = double.Parse(values[Q3COL]);
                }
                
                /* Read actual lines */
                while (true)
                {
                    line = reader.ReadLine();
                    if (reader.EndOfStream)
                        break;

                    if ( ( ( ( LineNumber % 7) == 0) && (CurrentDevice == Devices.Actigraph)) || (CurrentDevice == Devices.Invensense))  /* Read only the 7th sample to resample from 100Hz to 15 Hz */
                    {
                        values = line.Split(SplitChar);

                        if (CurrentDevice == Devices.Invensense)
                        {
                            QT0 = double.Parse(values[Q0COL]);
                            QT1 = double.Parse(values[Q1COL]);
                            QT2 = double.Parse(values[Q2COL]);
                            QT3 = double.Parse(values[Q3COL]);
                        }

                        CalculateLinear(values, out lx, out ly, out lz, 1);

                        OrientAxes(CurrentDevice, ref lx, ref ly, ref lz, ref Gx, ref Gy, ref Gz);

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
            QuatDebugger.Close();
            label1.Text = "Finished";
            }
    }
}
