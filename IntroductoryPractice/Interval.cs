using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroductoryPractice;

public class Interval
{
   public double LeftEnd { get; }
   public double RightEnd { get; }
   public double Center => (LeftEnd + RightEnd) / 2;
   public double Length => Math.Abs(RightEnd - LeftEnd);

   public Interval(double leftBoundary = 0, double rightBoundary = 1)
   {
      if (leftBoundary <= rightBoundary)
      {
         LeftEnd = leftBoundary;
         RightEnd = rightBoundary;
      }
      else
      {
         throw new ArgumentException("Неверно задан интервал.");
      }
   }

   public static Interval Parse(string str)
   {
      var data = str.Split();
      return new Interval(double.Parse(data[0]), double.Parse(data[1]));
   }

   public bool Contains(double point)
    => (point >= LeftEnd && point <= RightEnd);

   public override string ToString()
   {
      return $"[{LeftEnd}; {RightEnd}]";
   }
}