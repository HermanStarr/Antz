using System;
using System.Collections.Generic;
using System.Text;

namespace AntColony
{
    class NewFloat
    {
        public float _float { get; set; }
        public int _to { get; set; }
        public float _distance { get; set; }
        public override string ToString()
        {
            return "To: " + _to + ", weight: " + _float + ", d: " + _distance;
        }
        public NewFloat()
        {

        }

    }
}
