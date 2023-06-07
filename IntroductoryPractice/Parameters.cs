using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseProjectFEM;

public class Parameters
{
   public static double Lambda(double area = 0)
   {
      switch (area)
      {
         default: return 1;
      }
   }

   public static double Sigma(double area = 0)
   {
      switch (area)
      {
         default: return 2;
      }
   }

   public static double F(double x, double y, double z, double t)
   {
      return 2;
   }

   public static double U_t0(double x, double y, double z, double t)
   {
      return 0;
   }

   public static double U(double x, double y, double z, double t)
   {
      return t;
   }

   public static double dU_dn(double x, double y, double z, double t)
   {
      return 0;
   }
}