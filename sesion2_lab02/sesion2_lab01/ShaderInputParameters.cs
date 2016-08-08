using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;

namespace Sesion2_Lab01 {
    public struct ShaderInputParameters {
        public static ShaderInputParameters EMPTY = new ShaderInputParameters();

        public Matrix transformation;
    }
}
