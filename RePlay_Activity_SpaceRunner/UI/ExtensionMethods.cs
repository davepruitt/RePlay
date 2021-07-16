namespace RePlay_Activity_SpaceRunner.UI
{
    public static class ExtensionMethods
	{
        //Helper method. Restricts value to range [min, max]
        private static float _Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        //Helper method. Maps value from range [fromSource, toSource] -> [fromTarget, toTarget]. Optionally clamps values to range.
        private static float _Map(float value, float fromSource, float toSource, float fromTarget, float toTarget, bool clamp)
        {
            if (clamp)
            {
                value = _Clamp(value, fromSource, toSource);
            }
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        //Map extension method for float
		public static float Map(this float value, float fromSource, float toSource, float fromTarget, float toTarget, bool clamp=false)
		{
            return _Map(value, fromSource, toSource, fromTarget, toTarget, clamp);
		}

        //Map extension method for int
        public static float Map(this int value, float fromSource, float toSource, float fromTarget, float toTarget, bool clamp = false)
		{
            return _Map((float)value, fromSource, toSource, fromTarget, toTarget, clamp);
        }

        //Map extension method for double
        public static float Map(this double value, float fromSource, float toSource, float fromTarget, float toTarget, bool clamp = false)
		{
            return _Map((float)value, fromSource, toSource, fromTarget, toTarget, clamp);
        }
	}
}
