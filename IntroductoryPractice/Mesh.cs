using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CourseProjectFEM;

public class Mesh
{
   private double _timeStart;
   private double _timeEnd;
   private int _timeSplits;
   private double _timeDischarge;
   private List<double> _timeLayers;

   private double[] _linesX;
   private double[] _linesY;
   private double[] _linesZ;

   private List<double> _meshLinesX;
   private List<double> _meshLinesY;
   private List<double> _meshLinesZ;

   private int[] _splitsX;
   private int[] _splitsY;
   private int[] _splitsZ;

   private double[] _dischargeX;
   private double[] _dischargeY;
   private double[] _dischargeZ;

   // Номер формул, IXнач, IXкон, IYнач, IYкон, IZнач, IZкон.
   private List<int[]> _areas;

   // Номер краевого (1 | 2 | 3), номер формул, IXнач, IXкон, IYнач, IYкон, IZнач, IZкон.
   private List<int[]> _boundaryConditions;

   private List<List<int>> _allFaces;

   // Кринж.
   private List<Point3D> _points;
   private List<int[]> _elements;
   private IDictionary<int, int> _areaNodes;
   private HashSet<int> _boundaryNodes1;
   private HashSet<int> _boundaryNodes2;
   private IDictionary<int, int[]> _boundaryFaces2;
   private HashSet<int> _fictiveNodes;

   public int NodesCount => _points.Count;
   public int ElementsCount => _elements.Count;

   public ImmutableList<double> TimeLayers => _timeLayers.ToImmutableList();
   public ImmutableList<Point3D> Points => _points.ToImmutableList();
   public ImmutableList<int[]> Elements => _elements.ToImmutableList();
   public ImmutableDictionary<int, int> AreaNodes => _areaNodes.ToImmutableDictionary();
   public ImmutableHashSet<int> BoundaryNodes1 => _boundaryNodes1.ToImmutableHashSet();
   public ImmutableDictionary<int, int[]>  BoundaryFaces2 => _boundaryFaces2.ToImmutableDictionary();
   public ImmutableHashSet<int> FictiveNodes => _fictiveNodes.ToImmutableHashSet();


   public Mesh()
   {
      _allFaces = new();
      _boundaryFaces2 = new Dictionary<int, int[]>();
      _areaNodes = new Dictionary<int, int>();
      _boundaryConditions = new();
      _boundaryNodes1 = new();
      _boundaryNodes2 = new();
      _fictiveNodes = new();

      _timeLayers = new();

      _linesX = Array.Empty<double>();
      _linesY = Array.Empty<double>();
      _linesZ = Array.Empty<double>();

      _meshLinesX = new();
      _meshLinesY = new();
      _meshLinesZ = new();

      _splitsX = Array.Empty<int>();
      _splitsY = Array.Empty<int>();
      _splitsZ = Array.Empty<int>();

      _dischargeX = Array.Empty<double>();
      _dischargeY = Array.Empty<double>();
      _dischargeZ = Array.Empty<double>();

      _points = new();
      _areas = new();
      _elements = new();
   }

   public void Input(string meshSettingsPath, string splitsPath, string boundaryConditionsPath, string timeSettingsPath)
   {
      try
      {
         using (var sr = new StreamReader(meshSettingsPath))
         {
            sr.ReadLine();
            _linesX = sr.ReadLine().Split().Select(double.Parse).ToArray();

            sr.ReadLine();
            _linesY = sr.ReadLine().Split().Select(double.Parse).ToArray();

            sr.ReadLine();
            _linesZ = sr.ReadLine().Split().Select(double.Parse).ToArray();

            sr.ReadLine();
            while(!sr.EndOfStream)
               _areas.Add(sr.ReadLine().Split().Select(val => int.Parse(val) - 1).ToArray());
         }

         using (var sr = new StreamReader(splitsPath))
         {
            _splitsX = new int[_linesX.Length - 1];
            _dischargeX = new double[_splitsX.Length];

            _splitsY = new int[_linesY.Length - 1];
            _dischargeY = new double[_splitsY.Length];

            _splitsZ = new int[_linesZ.Length - 1];
            _dischargeZ = new double[_splitsZ.Length];

            var lineX = sr.ReadLine().Split();
            for (int i = 0; i < lineX.Length / 2; i++)
            {
               _splitsX[i] = int.Parse(lineX[2 * i]);
               _dischargeX[i] = double.Parse(lineX[2 * i + 1]);
            }

            var lineY = sr.ReadLine().Split();
            for (int i = 0; i < lineY.Length / 2; i++)
            {
               _splitsY[i] = int.Parse(lineY[2 * i]);
               _dischargeY[i] = double.Parse(lineY[2 * i + 1]);
            }

            var lineZ = sr.ReadLine().Split();
            for (int i = 0; i < lineZ.Length / 2; i++)
            {
               _splitsZ[i] = int.Parse(lineZ[2 * i]);
               _dischargeZ[i] = double.Parse(lineZ[2 * i + 1]);
            }
         }


         using (var sr = new StreamReader(boundaryConditionsPath))
         {
            while (!sr.EndOfStream)
               _boundaryConditions.Add(sr.ReadLine().Split().Select(val => int.Parse(val) - 1).ToArray());

            for (int i = 0; i < _boundaryConditions.Count; i++)
               _boundaryConditions[i][0]++;
         }

         using (var sr = new StreamReader(timeSettingsPath))
         {
            var data = sr.ReadLine().Split().Select(double.Parse).ToList();
            _timeStart = data[0];
            _timeEnd = data[1];

            _timeSplits = int.Parse(sr.ReadLine());
            _timeDischarge = double.Parse(sr.ReadLine());
         }
      }
      catch (Exception ex)
      {
         Console.WriteLine(ex.Message);
      }
   }

   public void Build()
   {
      // Разбиение каждой области в соответствии с её параметрами:
      // количеством разбиений;
      // коэффициенте разрядки.

      // По времени
      {
         double h;
         double sum = 0;
         double lenght = _timeEnd - _timeStart;

         for (int k = 0; k < _timeSplits; k++)
            sum += Math.Pow(_timeDischarge, k);

         h = lenght / sum;

         _timeLayers.Add(_timeStart);

         while (Math.Round(_timeLayers.Last() + h, 1) < _timeEnd)
         {
            _timeLayers.Add(_timeLayers.Last() + h);
            h *= _timeDischarge;
         }

         _timeLayers.Add(_timeEnd);
      }

      // По оси X
      for (int i = 0; i < _linesX.Length - 1; i++)
      {
         double h;
         double sum = 0;
         double lenght = _linesX[i + 1] - _linesX[i];

         for (int k = 0; k < _splitsX[i]; k++)
            sum += Math.Pow(_dischargeX[i], k);

         h = lenght / sum;

         _meshLinesX.Add(_linesX[i]);

         while (Math.Round(_meshLinesX.Last() + h, 1) < _linesX[i + 1])
         {
            _meshLinesX.Add(_meshLinesX.Last() + h);
            h *= _dischargeX[i];
         }
      }
      _meshLinesX.Add(_linesX.Last());

      // По оси Y
      for (int i = 0; i < _linesY.Length - 1; i++)
      {
         double h;
         double sum = 0;
         double lenght = _linesY[i + 1] - _linesY[i];

         for (int k = 0; k < _splitsY[i]; k++)
            sum += Math.Pow(_dischargeY[i], k);

         h = lenght / sum;

         _meshLinesY.Add(_linesY[i]);

         while (Math.Round(_meshLinesY.Last() + h, 1) < _linesY[i + 1])
         {
            _meshLinesY.Add(_meshLinesY.Last() + h);
            h *= _dischargeY[i];
         }
      }
      _meshLinesY.Add(_linesY.Last());

      // По оси Z
      for (int i = 0; i < _linesZ.Length - 1; i++)
      {
         double h;
         double sum = 0;
         double lenght = _linesZ[i + 1] - _linesZ[i];

         for (int k = 0; k < _splitsZ[i]; k++)
            sum += Math.Pow(_dischargeZ[i], k);

         h = lenght / sum;

         _meshLinesZ.Add(_linesZ[i]);

         while (Math.Round(_meshLinesZ.Last() + h, 1) < _linesZ[i + 1])
         {
            _meshLinesZ.Add(_meshLinesZ.Last() + h);
            h *= _dischargeZ[i];
         }
      }
      _meshLinesZ.Add(_linesZ.Last());

      // Сборка списка узлов.
      // Узлы нумеруются слева направо, снизу вверх.
      for (int k = 0; k < _meshLinesZ.Count; k++)
         for (int i = 0; i < _meshLinesY.Count; i++)
            for (int j = 0; j < _meshLinesX.Count; j++)
               _points.Add(new(_meshLinesX[j], _meshLinesY[i], _meshLinesZ[k]));

      // Сборка списка элементов.
      int splitsX = _splitsX.Sum();
      int splitsY = _splitsY.Sum();
      int splitsZ = _splitsZ.Sum();

      for (int k = 0; k < splitsZ; k++)
      {
         for (int i = 0; i < splitsY; i++)
         {
            for (int j = 0; j < splitsX; j++)
            {
               _elements.Add(
               new int[8]
               {
                  k * (splitsY + 1) * (splitsX + 1) + i * (splitsX + 1) + j,
                  k * (splitsY + 1) * (splitsX + 1) + i * (splitsX + 1) + j + 1,
                  k * (splitsY + 1) * (splitsX + 1) + (i + 1) * (splitsX + 1) + j,
                  k * (splitsY + 1) * (splitsX + 1) + (i + 1) * (splitsX + 1) + j + 1,

                  (k + 1) * (splitsY + 1) * (splitsX + 1) + i * (splitsX + 1) + j,
                  (k + 1) * (splitsY + 1) * (splitsX + 1) + i * (splitsX + 1) + j + 1,
                  (k + 1) * (splitsY + 1) * (splitsX + 1) + (i + 1) * (splitsX + 1) + j,
                  (k + 1) * (splitsY + 1) * (splitsX + 1) + (i + 1) * (splitsX + 1) + j + 1,
               }
               );
            }
         }
      }

      // TODO: Ща будет кринж с определением краевых нодов и фэйковых.
      for (int i = 0; i < _points.Count; i++)
      {
         for (int j = 0; j < _boundaryConditions.Count; j++)
         {
            if (_linesX[_boundaryConditions[j][2]] <= _points[i].X && _points[i].X <= _linesX[_boundaryConditions[j][3]])
            {
               if (_linesY[_boundaryConditions[j][4]] <= _points[i].Y && _points[i].Y <= _linesY[_boundaryConditions[j][5]])
               {
                  if (_linesZ[_boundaryConditions[j][6]] <= _points[i].Z && _points[i].Z <= _linesZ[_boundaryConditions[j][7]])
                  {
                     if (_boundaryConditions[j][0] == 1)
                        _boundaryNodes1.Add(i);
                     if (_boundaryConditions[j][0] == 2)
                        _boundaryNodes2.Add(i);
                  }
               }
            }
         }


         // Это чё ваще?
         for (int j = 0; j < _areas.Count; j++)
         {
            if (_linesX[_areas[j][1]] <= _points[i].X && _points[i].X <= _linesX[_areas[j][2]])
            {
               if (_linesY[_areas[j][3]] <= _points[i].Y && _points[i].Y <= _linesY[_areas[j][4]])
               {
                  if (_linesZ[_areas[j][5]] <= _points[i].Z && _points[i].Z <= _linesZ[_areas[j][6]])
                  {
                     _areaNodes.Add(i, j);
                     break;
                  }
               }
            }
         }

         if (!_areaNodes.ContainsKey(i))
            _fictiveNodes.Add(i);
      }


      //// TODO: Кринж со списком рёбер.
      //_boundaryFaces2 = new List<int>[_points.Count].Select(_ => new List<int>()).ToList();
      //_allFaces = new List<int>[_points.Count].Select(_ => new List<int>()).ToList();
      //foreach (var element in Elements)
      //   foreach (var position in element)
      //      foreach (var node in element)
      //         if (position < node)
      //            // Нестабильная штука.
      //            if (_points[position].X == _points[node].X && _points[position].Y == _points[node].Y
      //               || _points[position].X == _points[node].X && _points[position].Z == _points[node].Z
      //               || _points[position].Y == _points[node].Y && _points[position].Z == _points[node].Z)
      //            {
      //               if(!_fictiveNodes.Contains(position) && !_fictiveNodes.Contains(node) && !_allFaces[position].Contains(node))
      //                  _allFaces[position].Add(node);
      //               if (_boundaryNodes2.Contains(position) && _boundaryNodes2.Contains(node) && !_boundaryFaces2[position].Contains(node))
      //                  _boundaryFaces2[position].Add(node);
      //            }

      double eps = 1e-14;
      _allFaces = new List<int>[_points.Count].Select(_ => new List<int>()).ToList();
      foreach (var element in Elements)
         foreach (var position in element)
            foreach (var node in element)
               if (position < node)
               {

                  if (
                     _points[position].X - _points[node].X < eps && _points[position].Y - _points[node].Y < eps
                     || _points[position].X - _points[node].X < eps && _points[position].Z - _points[node].Z < eps
                     || _points[position].Y - _points[node].Y < eps && _points[position].Z - _points[node].Z < eps
                     )
                  {
                     if (!_fictiveNodes.Contains(position) && !_fictiveNodes.Contains(node) && !_allFaces[position].Contains(node))
                        _allFaces[position].Add(node);
                  }
               }



      /*
       
                        back
                 6┌────────────┐7       
                    top        │      
              4┌───────────┐5  │      
               │           │   │ right
         left  │           │   │      
               │   front   │   │      
               │           │   ┘3     
              0└───────────┘1
                  bottom
      

         2┌───────────┐3      4┌───────────┐5      6┌───────────┐7
          │           │        │           │        │           │ 
          │           │        │           │        │           │ 
          │  bottom   │        │   front   │        │    top    │ 
          │           │        │           │        │           │ 
         0└───────────┘1      0└───────────┘1      4└───────────┘5


         4┌───────────┐6      6┌───────────┐7      5┌───────────┐7
          │           │        │           │        │           │ 
          │           │        │           │        │           │ 
          │   left    │        │   back    │        │   right   │ 
          │           │        │           │        │           │ 
         0└───────────┘2      2└───────────┘3      1└───────────┘3

       */

      static bool IsFaceBoundary(HashSet<int> set, int[] i)
         => set.Contains(i[0]) && set.Contains(i[1]) && set.Contains(i[2]) && set.Contains(i[3]);

      int[] bottom = new int[4] { 0, 1, 2, 3 };
      int[] front = new int[4] { 0, 1, 4, 5 };
      int[] top = new int[4] { 4, 5, 6, 7 };
      int[] left = new int[4] { 0, 2, 4, 6 };
      int[] back = new int[4] { 2, 3, 6, 7 };
      int[] right = new int[4] { 1, 3, 5, 7 };

      IDictionary<int, int[]> faces = new Dictionary<int, int[]>()
      {
         {0, bottom},
         {1, front},
         {2, top},
         {3, left},
         {4, back},
         {5, right}
      };

      _boundaryFaces2 = new Dictionary<int, int[]>();
      for (int i = 0; i < _elements.Count; i++)
         foreach (var face in faces)
         {
            var faceglobal = new int[4] { _elements[i][face.Value[0]], _elements[i][face.Value[1]], _elements[i][face.Value[2]], _elements[i][face.Value[3]] };
            if (IsFaceBoundary(_boundaryNodes2, faceglobal))
               _boundaryFaces2.Add(_boundaryFaces2.Count, faceglobal);
         }


   }

   public void Output(string pointsPath, string ribsPath)
   {
      try
      {
         using (var sw = new StreamWriter(pointsPath))
         {
            foreach (var point in _points)
            sw.WriteLine(point.ToString());
         }

         using (var sw = new StreamWriter(ribsPath))
         {
            for (int i = 0; i < _allFaces.Count; i++)
               for (int j = 0; j < _allFaces[i].Count; j++)
                  sw.WriteLine($"{_points[i]} {_points[_allFaces[i][j]]}");
         }
      }
      catch (Exception ex)
      {
         Console.WriteLine(ex.Message);
      }
   }
}