using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FFT  {

    public static float[] orderCoefficients(float[] coefficients)
    {
        float[] result = new float[coefficients.Length / 2];

        for (int i=0;i<coefficients.Length;i++)
        {
            //In example:
            //We have calculated 8 coefficients with k = [-3, -2, -1, 0, 1, 2, 3, 4]
            //We are iterating over it with i = [0, 1, 2, 3, 4, 5, 6, 7]
            //The general offset is length/2-1
            int c = Math.Abs(i - (coefficients.Length / 2 - 1));
            
            //Ignoring the last coefficient as its irrelevant
            if (c < result.Length)
            result[c] += coefficients[i];
        }
        return result;
    }

    public static float[] forward(float[] samples)
    {
        double[] asDouble = new double[samples.Length];
        for (int i=0;i<asDouble.Length;i++)
        {
            asDouble[i] = (double)samples[i];
        }
        double[] result = forward(asDouble);

        float[] asFloats = new float[samples.Length];
        for (int i=0;i<asFloats.Length;i++)
        {
            asFloats[i] = (float) result[i];
        }

        return asFloats;
    }

    public static double[] forward(double[] samples)
    {
        double logValue = Math.Log(samples.Length) / Math.Log(2.0);
        if (logValue != (int)logValue) throw new ArgumentException("Samples.Length should be a power of two but is " + samples.Length);


        ComplexNumber[] complexValues = doubleArrayToComplex(samples);

        ComplexNumber[] result = calculateMatrixMultiplication(samples.Length, 0, complexValues);


        //Matters if we care for the angularCoordinate of the numbers
        /*for (int i=0;i<result.Length;i++)
        {
            int k = i - result.Length / 2 + 1;
            if (k % 2 != 0) result[i] = -result[i];
        }*/


        return complexArrayToDouble(result);
    }


    private static ComplexNumber[] calculateMatrixMultiplication(int matrixSize, int recursionDepth, ComplexNumber[] samples)
    {

        if (samples.Length == 1)
        {
            return samples;
        }

        ComplexNumber[] dataForFirstCalculation = new ComplexNumber[samples.Length / 2];
        ComplexNumber[] dataForSecondCalculation = new ComplexNumber[samples.Length / 2];

        ComplexNumber unaryRoot = ComplexNumber.unaryRoot(matrixSize);

        for (int i=0;i<dataForFirstCalculation.Length;i++)
        {
            int secIndex = dataForFirstCalculation.Length / 2 + i;
            dataForFirstCalculation[i] = samples[i] + samples[secIndex];
            dataForSecondCalculation[i] = (samples[i] - samples[secIndex]) * unaryRoot.pow( -( (int) Math.Pow(2, recursionDepth)) * i);
            if (dataForFirstCalculation[i] == null) throw new ArgumentNullException("For first calculation null in depth " + recursionDepth);
            if (dataForSecondCalculation[i] == null) throw new ArgumentNullException("For second calculation null in depth " + recursionDepth);
        }

        ComplexNumber[] firstResult = calculateMatrixMultiplication(matrixSize, recursionDepth + 1, dataForFirstCalculation);
        ComplexNumber[] secondResult = calculateMatrixMultiplication(matrixSize, recursionDepth + 1, dataForSecondCalculation);

        ComplexNumber[] wholeResult = new ComplexNumber[samples.Length];
        for (int i=0;i<wholeResult.Length;i++)
        {
            if (i % 2 == 0) wholeResult[i] = firstResult[i / 2];
            else wholeResult[i] = secondResult[(i - 1) / 2];
        }

        return wholeResult;

    }

    public static ComplexNumber[] doubleArrayToComplex(double[] arr)
    {
        ComplexNumber[] newValues = new ComplexNumber[arr.Length];
        for (int i=0;i<arr.Length;i++)
        {
            newValues[i] = new ComplexNumber(0, arr[i]);
        }
        return newValues;
    }

    public static double[] complexArrayToDouble(ComplexNumber[] arr)
    {
        double[] newValues = new double[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            newValues[i] = arr[i].radial;
        }
        return newValues;
    }

	public class ComplexNumber
    {
        public readonly double angular;
        public readonly double radial;
        public ComplexNumber(double pAng, double pRad)
        {
            angular = pAng;
            radial = pRad;
        }

        public double getReal()
        {
            return Math.Cos(angular) * radial;
        }
        public double getImaginary()
        {
            return Math.Sin(angular) * radial;
        }

        public static ComplexNumber byValues(double x, double y)
        {
            double radial = Math.Sqrt(x * x + y * y);
            double angular;
            if (radial == 0) angular = 0;
            else
            {
                if (y >= 0)
                {
                    angular = Math.Acos(x / radial);
                } else
                {
                    angular = -Math.Acos(x / radial);
                }
            }
            angular += 2 * Math.PI;
            angular %= 2 * Math.PI;
            return new ComplexNumber(angular, radial);
        }

        public static ComplexNumber operator+ (ComplexNumber first, ComplexNumber second)
        {
            return byValues(first.getReal() + second.getReal(), first.getImaginary() + second.getImaginary());
        }

        public static ComplexNumber operator- (ComplexNumber first, ComplexNumber second)
        {
            return byValues(first.getReal() - second.getReal(), first.getImaginary() - second.getImaginary());
        }

        public static ComplexNumber operator- (ComplexNumber num)
        {
            return new ComplexNumber(num.angular + Math.PI, num.radial);
        }

        public static ComplexNumber operator* (ComplexNumber first, ComplexNumber second)
        {
            return new ComplexNumber((first.angular + second.angular) % (2 * Math.PI), first.radial * second.radial);
        }

        public static ComplexNumber operator* (double factor, ComplexNumber number)
        {
            ComplexNumber newNum = new ComplexNumber(number.angular, number.radial * Math.Abs(factor));
            return factor >= 0 ? newNum : -newNum;
        }

        public static ComplexNumber operator/ (ComplexNumber first, ComplexNumber second)
        {
            return new ComplexNumber((first.angular - second.angular + 2 * Math.PI) % (2 * Math.PI), first.radial / second.radial);
        }

        public ComplexNumber pow(int exponent)
        {
            return new ComplexNumber((angular * exponent) % (2 * Math.PI), Math.Pow(radial, exponent));
        }

        public static ComplexNumber unaryRoot(int n)
        {
            return new ComplexNumber(2 * Math.PI / n, 1);
        }




    }
}
