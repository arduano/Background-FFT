using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BasicBarsWindow
{
    class View
    {
        public Vector2 position;
        public double rotation;
        public double zoom;

        public View(Vector2 pos = new Vector2(), double rot = 0, double z = 0.5)
        {
            position = pos;
            rotation = rot;
            zoom = z;
        }

        public void ApplyTransform(int clientWidth, int clientHeight)
        {
            Matrix4 transform = Matrix4.Identity;

            float ratio = (float)clientWidth / (float)clientHeight;
            transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-position.X, -position.Y, 0));
            transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(-(float)rotation));
            transform = Matrix4.Mult(transform, Matrix4.CreateScale((float)zoom, (float)zoom * ratio, 1.0f));

            GL.LoadMatrix(ref transform);
        }
    }
}
