using System;
using System.CodeDom;

namespace LSL_Kinect.Tools
{
    class Tools
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// The parameter t is clamped to the range[0, 1].
        /// </summary>
        /// <param name="from">Min value of the range</param>
        /// <param name="to">Max value of the range</param>
        /// <param name="time">The interpolation value between the two double</param>
        /// <returns>The interpolated double result between the two double values.</returns>
        public static double Lerp(double from, double to, double time)
        {
            double value = UnclampedLerp(from, to, time);

            if (value < from)
                value = from;
            else if (value > to)
                value = to;

            return value;
        }

        /// <summary>
        /// Linearly interpolates between a and b by t with no limit to t.
        /// The parameter t is not clamped and a value based on a and b is supported.
        /// If t is less than zero, or greater than one, then LerpUnclamped will result in a return value outside the range a to b.
        /// </summary>
        /// <param name="from">Min value of the range</param>
        /// <param name="to">Max value of the range</param>
        /// <param name="time">The interpolation value between the two double</param>
        /// <returns>The float value as a result from the linear interpolation.</returns>
        public static double UnclampedLerp(double from, double to, double time)
        {
            return from + (time * (to - from));
        }

        /// <summary>
        /// Calculates the linear parameter t that produces the interpolant value within the range [a, b].
        /// </summary>
        /// <param name="from">Min value of the range</param>
        /// <param name="to">Max value of the range</param>
        /// <param name="value"> Value is a location between a and b </param>
        /// <returns>Percentage of value between min and max</returns>
        public static double InverseLerp(double from, double to, double value)
        {
            return (value - from) / (to - from);
        }

    }
}
