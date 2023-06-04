using CourseProjectFEM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductoryPractice;

public static class ThreeLinearBasis
{
   public static double Psi1(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis1(point.X, intervalX) * Basis1(point.Y, intervalY) * Basis1(point.Z, intervalZ);

   public static double Psi2(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis2(point.X, intervalX) * Basis1(point.Y, intervalY) * Basis1(point.Z, intervalZ);

   public static double Psi3(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis1(point.X, intervalX) * Basis2(point.Y, intervalY) * Basis1(point.Z, intervalZ);

   public static double Psi4(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis2(point.X, intervalX) * Basis2(point.Y, intervalY) * Basis1(point.Z, intervalZ);

   public static double Psi5(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis1(point.X, intervalX) * Basis1(point.Y, intervalY) * Basis2(point.Z, intervalZ);

   public static double Psi6(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis2(point.X, intervalX) * Basis1(point.Y, intervalY) * Basis2(point.Z, intervalZ);

   public static double Psi7(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis1(point.X, intervalX) * Basis2(point.Y, intervalY) * Basis2(point.Z, intervalZ);

   public static double Psi8(Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
      => Basis2(point.X, intervalX) * Basis2(point.Y, intervalY) * Basis2(point.Z, intervalZ);

   public static double Basis1(double variable, Interval interval)
      => (interval.RightEnd - variable) / interval.Length;

   public static double Basis2(double variable, Interval interval)
      => (variable - interval.LeftEnd) / interval.Length;
}
