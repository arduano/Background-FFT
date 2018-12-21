using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FancyVisualizer
{
    public class Slider
    {
        public Vector2 Start;
        public Vector2 End;
        public double startVal;
        public double endVal;

        public float lineSize = 0.005f;
        public float headSize = 0.015f;

        public bool hovered = false;
        public bool dragging = false;
        public double dragOffset = 0;

        private double currVal;
        public double CurrVal
        {
            get { return currVal; }
            set
            {
                currVal = value;
                if (currVal < startVal) currVal = startVal;
                if (currVal > endVal) currVal = endVal;
                headPos = (currVal - startVal) / (endVal - startVal);
            }
        }
        private double headPos;
        public double HeadPos
        { get { return headPos; } }

        public Slider(Vector2 start, Vector2 end, double startVal, double endVal, double currVal)
        {
            Start = start;
            End = end;
            this.startVal = startVal;
            this.endVal = endVal;
            this.CurrVal = currVal;
        }

        public void startDrag(double x, double y)
        {
            var pos = Start + (End - Start) * (float)HeadPos;
            x -= pos.X;
            y += pos.Y;
            var dir = (End - Start).Normalized();
            var x2 = -dir.X * x + dir.Y * y;
            var y2 = -dir.Y * x - dir.X * y;
            dragging = true;
            dragOffset = x2 * 2;
        }

        public void setDrag(double x, double y)
        {
            x -= Start.X;
            y -= Start.Y;
            var dir = (End - Start).Normalized();
            var x2 = -dir.X * x + dir.Y * y;
            headPos = -x2 / (Start - End).Length + dragOffset;
            if (headPos < 0) headPos = 0;
            if (headPos > 1) headPos = 1;
            currVal = startVal + (endVal - startVal) * headPos;
        }

        public bool isInHead(double x, double y)
        {
            var pos = Start + (End - Start) * (float)HeadPos;
            x -= pos.X;
            y += pos.Y;
            var dir = (End - Start).Normalized();
            var x2 = dir.X * x - dir.Y * y;
            var y2 = dir.Y * x + dir.X * y;

            if (Math.Abs(x2) < headSize && Math.Abs(y2) < headSize)
            {return true; }
            return false;
        }

    }
}