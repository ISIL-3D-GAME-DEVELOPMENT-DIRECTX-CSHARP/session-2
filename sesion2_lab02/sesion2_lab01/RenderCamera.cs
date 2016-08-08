using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;

namespace Sesion2_Lab01 {
    public class RenderCamera {

        private Matrix mViewNMatrix;
        private Matrix mProjectionNMatrix;
        private Matrix mWorld;

        private Matrix mTransformed;

        public Matrix world         { get { return mWorld; } }
        public Matrix projection    { get { return mProjectionNMatrix; } }
        public Matrix view          { get { return mViewNMatrix; } }

        public Matrix transformed   { get { return mTransformed; } }

        public RenderCamera(int width, int height) {
            mWorld = SimpleMatrix.Identity;
            mProjectionNMatrix = SimpleMatrix.CreateOrthographicOffCenter(0, -width, height, 0, 1.0f, 100.0f);
            mViewNMatrix = SimpleMatrix.CreateLookAt(new Vector3(0, 0, -1.0f), Vector3.Zero, Vector3.UnitY);
        }

        public void Update() {
            Matrix temp = Matrix.Identity;

            Matrix.Multiply(ref mWorld, ref mViewNMatrix, out temp);
            Matrix.Multiply(ref temp, ref mProjectionNMatrix, out mTransformed);
        }
    }
}
