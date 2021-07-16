using System;
using System.Collections.Generic;
using System.Linq;

namespace FitMiAndroid
{
    public class PuckPacket
    {
        public bool Touch = false;
        public bool Connected = false;

        public List<int> Accelerometer = new List<int>() { 0, 0, 0 };
        public List<int> Gyrometer = new List<int>() { 0, 0, 0 };
        public List<double> Magnetometer = new List<double>() { 0, 0, 0 };
        public List<int> Velocity = new List<int>() { 0, 0, 0 };
        public List<double> Quat = new List<double>() { 0, 0, 0, 0 };
        public List<double> Rpy = new List<double>() { 0, 0, 0 };
        public int Loadcell = 0;
        public int Battery = 0;
        public bool Charging = false;
        public int imuok = 0;
        public int velmd = 0;
        public int state = 0;
        public int resv5 = 0;

        public string full_packet = string.Empty;

        public PuckPacket()
        {
            //empty
        }

        public void Parse(byte[] input_byte_array)
        {
            full_packet = BitConverter.ToString(input_byte_array);
            //full_packet = Encoding.ASCII.GetString(input_byte_array);

            List<int> vel_or_mag = new List<int>() { 0, 0, 0 };

            //The following code is how the FitMi folks wrote their code:
            /*
            Accelerometer[0] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 0);
            Accelerometer[1] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 1);
            Accelerometer[2] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 2);

            Gyrometer[0] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 3);
            Gyrometer[1] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 4);
            Gyrometer[2] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 5);
            */

            //The following segment of code is how I think it actually should be:
            Accelerometer[0] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 3);
            Accelerometer[1] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 4);
            Accelerometer[2] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 5);

            Gyrometer[0] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 0);
            Gyrometer[1] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 1);
            Gyrometer[2] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 2);
            //End of code segment

            vel_or_mag[0] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 6);
            vel_or_mag[1] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 7);
            vel_or_mag[2] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 8);

            Quat[0] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 9);
            Quat[1] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 10);
            Quat[2] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 11);
            Quat[3] = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 12);
            Quat = Quat.Select(x => x / 10000.0).ToList();

            Loadcell = BitConverter.ToInt16(input_byte_array, sizeof(Int16) * 13);

            var battery_position = (sizeof(Int16) * 13) + sizeof(byte);
            Battery = input_byte_array[battery_position];

            byte status = input_byte_array[battery_position + sizeof(byte)];
            ParseStatus(status);

            if (velmd > 0)
            {
                Velocity = vel_or_mag;
            }
            else
            {
                Magnetometer = vel_or_mag.Select(x => x / 100.0).ToList();
            }

            GetRpy();
        }

        public void ParseStatus(byte status)
        {
            Connected = (status & 0b0000_0001) > 0 ? true : false;
            imuok = (status & 0b0000_0010) >> 1;
            Touch = ((status & 0b0000_0100) >> 2) > 0 ? true : false;
            velmd = (status & 0b0000_1000) >> 3;
            state = (status & 0b0111_0000) >> 4;
            resv5 = (status & 0b1000_0000) >> 7;
        }

        public void GetRpy()
        {
            var q0 = Quat[0];
            var q1 = Quat[1];
            var q2 = Quat[2];
            var q3 = Quat[3];

            Rpy[2] = Math.Atan2(2.0 * (q1 * q2 + q0 * q3), q0 * q0 + q1 * q1 - q2 * q2 - q3 * q3) * 180.0 / Math.PI;
            Rpy[1] = -Math.Asin(2.0 * (q1 * q3 - q0 * q2)) * 180.0 / Math.PI;
            Rpy[0] = Math.Atan2(2.0 * (q0 * q1 + q2 * q3), q0 * q0 - q1 * q1 - q2 * q2 + q3 * q3) * 180.0 / Math.PI;
        }

        public double GetVerticalAngle()
        {
            List<double> v1 = new List<double>() { 0, 0, 1 };
            var vt = Quaternion.qv_mult(Quat, v1);

            //np.arccos(np.linalg.norm(vt[0:2]))*180.0/np.pi * np.sign(vt[2])
            return Math.Acos(LinearAlgebra.Norm(vt.GetRange(0, 2).ToList())) * 180.0 / Math.PI * Math.Sign(vt[2]);
        }

        public double GetAngle(List<double> v1)
        {
            var vt = Quaternion.qv_mult(Quat, v1);
            var nvt = LinearAlgebra.Norm(vt);
            if (nvt > 0)
            {
                vt = vt.Select(x => x / nvt).ToList();
            }

            //np.arccos(np.linalg.norm(vt[0:2]))*180.0/np.pi * np.sign(vt[2])
            var angle = Math.Acos(LinearAlgebra.Norm(vt.GetRange(0, 2))) * 180 / Math.PI * Math.Sign(vt[2]);
            return angle;
        }

        public double GetZAngle()
        {
            List<double> v1 = new List<double>() { 0, 0, 1 };
            return GetAngle(v1);
        }

        public double GetXAngle()
        {
            List<double> v1 = new List<double>() { 1, 0, 0 };
            return GetAngle(v1);
        }

        public double GetYAngle()
        {
            List<double> v1 = new List<double>() { 0, 1, 0 };
            return GetAngle(v1);
        }
    }
}
