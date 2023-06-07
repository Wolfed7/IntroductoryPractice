using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseProjectFEM;

public class Point3D
{
   public double X { get; init; }
   public double Y { get; init; }
   public double Z { get; init; }

   public Point3D(double x, double y, double z) 
   {
      X = x;
      Y = y;
      Z = z;
   }

   public override string ToString()
   {
      return $"{X:e15} {Y:e15} {Z:e15}";
   }

   public static Point3D Parse(string input)
   {
      var data = input.Split().Select(double.Parse).ToList(); 
      return new Point3D(data[0], data[1], data[2]);
   }
}
