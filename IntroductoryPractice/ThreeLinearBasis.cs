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

   public static double PsiI(int i, Point3D point, Interval intervalX, Interval intervalY, Interval intervalZ)
   {
      switch (i) 
      {
         case 0: return Psi1(point, intervalX, intervalY, intervalZ);
         case 1: return Psi2(point, intervalX, intervalY, intervalZ);
         case 2: return Psi3(point, intervalX, intervalY, intervalZ);
         case 3: return Psi4(point, intervalX, intervalY, intervalZ);
         case 4: return Psi5(point, intervalX, intervalY, intervalZ);
         case 5: return Psi6(point, intervalX, intervalY, intervalZ);
         case 6: return Psi7(point, intervalX, intervalY, intervalZ);
         case 7: return Psi8(point, intervalX, intervalY, intervalZ);
         default: return 0.0;
      }
   }

   public static double Basis1(double variable, Interval interval)
      => (interval.RightEnd - variable) / interval.Length;

   public static double Basis2(double variable, Interval interval)
      => (variable - interval.LeftEnd) / interval.Length;
}
