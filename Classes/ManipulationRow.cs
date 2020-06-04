using Microsoft.Kinect;
using System;
using System.Data;
using System.Globalization;

namespace LSL_Kinect
{
    public static class ManipulationRow
    {
        public static string[] ReworkRow(this DataRow row, string rowName)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            Object[] temp_2 = new object[25];
            decimal[] temp_3 = new decimal[25];
            string[] temp = new string[25];
            for (int i = 0; i < 25; i++)
            {
                temp_2[i] = row[rowName + ((JointType)i).ToString()];
                temp_3[i] = Convert.ToDecimal(temp_2[i]);
                temp_3[i] = Math.Round(temp_3[i], 3);
                temp[i] = temp_3[i].ToString(nfi);
            }

            return temp;
        }

        public static string Coma_To_Dot(this DataRow row, string rowName)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            double value = (double)row[rowName];
            string result = value.ToString(nfi);

            return result;
        }
    }
}