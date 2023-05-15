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
      switch(area)
      {
         case 1: return 1;
         case 2: return 2;
         default: return 3;
      }
   }

   public static double Sigma(double area = 0)
   {
      switch (area)
      {
         case 1: return 1;
         case 2: return 2;
         default: return 1;
      }
   }

   public static double F(double x, double y, double z, double t)
   {
      return 2 * t;
   }

   public static double U(double x, double y, double z, double t)
   {
      return x + y + z + t * t;
   }

   public static double dU_dn(double x, double y, double z, double t)
   {
      return -2;
   }
}
