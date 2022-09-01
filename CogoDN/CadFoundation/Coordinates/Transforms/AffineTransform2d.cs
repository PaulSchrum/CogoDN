using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics.LinearAlgebra;


namespace CadFoundation.Coordinates.Transforms
{
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
    public class AffineTransform2d
    {
        public Matrix<double> M { get; protected set; }
        private double[,] MasArray;

        public AffineTransform2d()
        {
            M = makeIdentityMatrix();

            MasArray = default;
        }

        private Matrix<double> makeIdentityMatrix()
        {
            MasArray = new double[3, 3];
            /*    {{ 1.0, 0.0, 0.0, 0.0},
                   { 0.0, 1.0, 0.0, 0.0},
                   { 0.0, 0.0, 1.0, 0.0},
                   { 0.0, 0.0, 0.0, 1.0} }; */

            // Make it the identify matrix.
            MasArray[0, 0] = 1.0;
            MasArray[1, 1] = 1.0;
            MasArray[2, 2] = 1.0;
            return Matrix<double>.Build.DenseOfArray(MasArray);
        }

        public void AddTranslation(double dx, double dy)
        {
            var localMatrix = makeIdentityMatrix();
            localMatrix[0, 2] = dx;
            localMatrix[1, 2] = dy;

            M = localMatrix * M;
        }

        public void AddScale(double scaleX, double scaleY)
        {
            var localMatrix = makeIdentityMatrix();
            localMatrix[0, 0] = scaleX;
            localMatrix[1, 1] = scaleY;

            M = localMatrix * M;
        }

        /// <summary>
        /// Posititve rotation is clockwise.
        /// </summary>
        /// <param name="rotationInDegrees"></param>
        public void AddRotationDegrees(double rotationInDegrees)
        {
            var rotationInRadians = rotationInDegrees * 180.0 / Math.PI;
            AddRotationRadians(rotationInRadians);
        }

        /// <summary>
        /// Posititve rotation is clockwise.
        /// </summary>
        /// <param name="rotationInRadians"></param>
        public void AddRotationRadians(double rotationInRadians)
        {
            var cos = Math.Cos(rotationInRadians);
            var sin = Math.Sin(rotationInRadians);

            var localMatrix = makeIdentityMatrix();
            localMatrix[0, 0] = cos;
            localMatrix[1, 1] = cos;
            localMatrix[1, 0] = sin;
            localMatrix[0, 1] = -sin;

            M = localMatrix * M;
        }

        public void AddShear(double shearX, double shearY)
        {
            var localMatrix = makeIdentityMatrix();
            localMatrix[0, 1] = shearX;
            localMatrix[1, 0] = shearY;

            M = localMatrix * M;
        }

        public AffineTransform2d NewFromInverse()
        {
            AffineTransform2d returnXform = new AffineTransform2d();
            returnXform.M = this.M.Clone();
            returnXform.M.Inverse();
            return returnXform;
        }

        public AffineTransform2d NewFromTranspose()
        {
            AffineTransform2d returnXform = new AffineTransform2d();
            returnXform.M = this.M.Clone();
            returnXform.M.Transpose();
            return returnXform;
        }

        public Point TransformToNewPoint(Point oldPoint)
        {
            double[,] ptVector = new double[3, 1];
            ptVector[0, 0] = oldPoint.x;
            ptVector[1, 0] = oldPoint.y;
            ptVector[2, 0] = 1.0;
            var ptMatrix = Matrix<double>.Build.DenseOfArray(ptVector);
            var newPtMatrix = M * ptMatrix;
            return new Point(newPtMatrix[0, 0], newPtMatrix[1, 0]);
        }

        public void TransformPointInPlace(ref Point oldPoint)
        {
            double[,] ptVector = new double[3, 1];
            ptVector[0, 0] = oldPoint.x;
            ptVector[1, 0] = oldPoint.y;
            ptVector[2, 0] = 1.0;
            var ptMatrix = Matrix<double>.Build.DenseOfArray(ptVector);
            var newPtMatrix = ptMatrix * M;
            oldPoint.x = newPtMatrix[0, 0];
            oldPoint.y = newPtMatrix[1, 0];
        }

        public override string ToString()
        {
            return $"{M[0, 0]:f2} {M[0, 1]:f2} {M[0, 2]:f2}  +  " +
                $"{M[1, 0]:f2} {M[1, 1]:f2} {M[1, 2]:f2}  +  " +
                $"{M[2, 0]:f2} {M[2, 1]:f2} {M[2, 2]:f2}" +
                "";
        }

    }
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
}
