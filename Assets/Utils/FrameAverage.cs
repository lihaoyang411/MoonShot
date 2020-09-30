using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TeamBlack.Util
{
    public class FrameAverage
    {
        private float[] _buffer;
        private int index = 0;
        public FrameAverage(int frames)
        {
            _buffer = new float[frames];
        }

        public void Update(float v)
        {
            _buffer[index] = v;
            index = (index + 1) % _buffer.Length;
        }

        public float Average => _buffer.Sum() / _buffer.Length;
    }
}
