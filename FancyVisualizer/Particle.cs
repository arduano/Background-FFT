using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace FancyVisualizer
{
    struct Particle
    {
        public Vector2 location;
        public Vector2 velocity;

        public float size;
        public float opacity;

        public bool exists;

        public Particle(Vector2 l, Vector2 v, float s)
        {
            location = l;
            velocity = v;
            size = s;
            opacity = 0;
            exists = true;
        }

        public void Step(float strength)
        {
            location += velocity * strength;
            if (opacity < 1) opacity += .05f;
        }
    }
}
