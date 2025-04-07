namespace IntegrationTests
{
    public struct Point
    {
        private int _x;
        private int _y;

        public Point(int x, int y)
        {
            _x = x;
            _y = y;
        }


        public bool IsInRect(int left, int top, int right, int bottom)
        {
            if (_x > left && _x < right && _y > bottom && _y < top)
            {
                return true;
            }

            return false;
        }


        public void ThrowIfNotOnXAxis()
        {
            if (_y != 0)
            {
                throw new ArgumentException();
            }
        }


        public static int StaticMethod(Point p)
        {
            if (p._x > 0)
            {
                return 0;
            }

            return 1;
        }
    }
}