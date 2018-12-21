using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace FancyVisualizer
{
    class View
    {
        public Vector2 position;
        public double rotation;
        public double zoom;

        public RectangleF bounds = new Rectangle(0, 0, 0, 0);


        public bool hMirror = false;
        public bool vMirror = false;

        public View(Vector2 pos = new Vector2(), double rot = 0, double z = 0.5)
        {
            position = pos;
            rotation = rot;
            zoom = z;
        }

        public void ApplyTransform(int clientWidth, int clientHeight)
        {
            Matrix4 transform = Matrix4.Identity;

            float h = 1;
            if (hMirror) h = -1;
            float v = 1;
            if (vMirror) v = -1;

            transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-position.X, -position.Y, 0));
            transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(-(float)rotation));
            float ratio = (float)clientWidth / (float)clientHeight;
            if (ratio > 1)
            {
                ratio = 1 / ratio;
                transform = Matrix4.Mult(transform, Matrix4.CreateScale((float)zoom * ratio * h, (float)zoom * v, 1.0f));
                float hwidth = 1 / ((float)zoom * ratio);
                float hheight = 1 / (float)zoom;
                bounds.X = position.X - hwidth;
                bounds.Y = position.Y - hheight;
                bounds.Width = hwidth * 2;
                bounds.Height = hheight * 2;
            }
            else
            {
                transform = Matrix4.Mult(transform, Matrix4.CreateScale((float)zoom * h, (float)zoom * ratio * v, 1.0f));
                float hwidth = 1 / (float)zoom;
                float hheight = 1 / ((float)zoom * ratio);
                bounds.X = position.X - hwidth;
                bounds.Y = position.Y - hheight;
                bounds.Width = hwidth * 2;
                bounds.Height = hheight * 2;
            }
            GL.LoadMatrix(ref transform);
        }
    }
}
