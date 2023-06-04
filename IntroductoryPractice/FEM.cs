using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseProjectFEM;

public class FEM
{
   private SparseMatrix _globalMatrix;
   private Vector _globalVector;
   private Vector _solution;

   public Mesh _mesh;
   private Solver _solver;
   private Vector _localVector;
   private Matrix _localStiffness;
   private Matrix _localMassSigma, _localMassHee;

   private double _timeLayersNorm;
   private int _currentTimeLayerI => 2;
   private int _prevTimeLayerI => 1;
   private int _prevPrevTimeLayerI => 0;

   private double _currentTimeLayer;
   private double _prevTimeLayer;
   private double _prevPrevTimeLayer;
   private double _t, _t0, _t1;
   private Vector[] _qLayers;

   public static int NodesPerElement => 8;

   public FEM()
   {
      _qLayers = new Vector[3];
      _localStiffness = new(NodesPerElement);
      _localMassSigma = new(NodesPerElement);
      _localMassHee = new(NodesPerElement);
      _localVector = new(NodesPerElement);

      _solver = new BCG();
      _mesh = new Mesh();

      _globalMatrix = new SparseMatrix(0, 0);
      _globalVector = new(0);
      _solution = new(0);
   }

   public void SetSolver(Solver solver)
      => _solver = solver;
   public void SetMesh(Mesh mesh)
   => _mesh = mesh;


   public void Compute()
   {
      BuildPortrait();

      for (int i = 0; i < _qLayers[0].Size; i++)
         _qLayers[0][i] = Parameters.U_t0(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _mesh.TimeLayers[0]);

      //for (int i = 0; i < _qLayers[1].Size; i++)
      //   _qLayers[1][i] = Parameters.U(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _mesh.TimeLayers[1]);

      BuildSecondTimeLayer();

      //// DEBUG: решение, которое должно быть найдено в ходе поиска третьего слоя.
      //for (int i = 0; i < _qLayers[2].Size; i++)
      //   Console.WriteLine($"{i}:  {Parameters.U(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _mesh.TimeLayers[2])}");

      for (int i = 2; i < _mesh.TimeLayers.Count; i++)
      {

         _currentTimeLayer = _mesh.TimeLayers[i];
         _prevTimeLayer = _mesh.TimeLayers[i - 1];
         _prevPrevTimeLayer = _mesh.TimeLayers[i - 2];

         _t = _currentTimeLayer - _prevPrevTimeLayer;
         _t0 = _currentTimeLayer - _prevTimeLayer;
         _t1 = _prevTimeLayer - _prevPrevTimeLayer;

         AssemblySLAE();
         AccountSecondConditions();
         AccountFirstConditions();
         ExcludeFictiveNodes();

         _solver.SetSLAE(_globalVector, _globalMatrix);
         _qLayers[2] = _solver.Solve();

         // DEBUG: результат третьего слоя
         //if (i == 2)
         //{
         //   for (int j = 0; j < _qLayers[2].Size; j++)
         //      Console.WriteLine($"{j}:  {_qLayers[2][j]:e2}");
         //}
         //// DEBUG: погрешность на третьем слое
         //if (i == 2)
         //{
         //   for (int j = 0; j < _qLayers[2].Size; j++)
         //      Console.WriteLine($"{j}:  {Math.Abs(Parameters.U(_mesh.Points[j].X, _mesh.Points[j].Y, _mesh.Points[j].Z, _mesh.TimeLayers[2]) - _qLayers[2][j]):e2}");
         //}

         _qLayers[0] = _qLayers[1];
         _qLayers[1] = _qLayers[2];

         //PrintCurrentLayerErrorNorm();
      }
      //PrintTimeLayersNorm();


      using (var sr = new StreamWriter("LayerWeights.dat"))
      {
         for (int j = 0; j < _qLayers[2].Size; j++)
            sr.WriteLine(_qLayers[2][j]);
      }
   }

   public void BuildPortrait()
   {
      var list = new HashSet<int>[_mesh.NodesCount].Select(_ => new HashSet<int>()).ToList();
      foreach (var element in _mesh.Elements)
         foreach (var position in element)
            foreach (var node in element)
               if (position > node)
                  list[position].Add(node);

      int offDiagonalElementsCount = list.Sum(childList => childList.Count);

      _globalMatrix = new(_mesh.NodesCount, offDiagonalElementsCount);
      _globalVector = new(_mesh.NodesCount);

      _globalMatrix._ia[0] = 0;

      for (int i = 0; i < list.Count; i++)
         _globalMatrix._ia[i + 1] = _globalMatrix._ia[i] + list[i].Count;

      int k = 0;
      foreach (var childList in list)
         foreach (var value in childList.Order())
            _globalMatrix._ja[k++] = value;

      for (int i = 0; i < _qLayers.Length; i++)
         _qLayers[i] = new Vector(_mesh.NodesCount);
   }

   private bool IsElementFictive(int ielem)
   {
      for (int i = 0; i < _mesh.Elements[ielem].Length; i++)
         if (_mesh.FictiveNodes.Contains(_mesh.Elements[ielem][i]))
            return true;

      return false;
   }

   public void BuildSecondTimeLayer()
   {
      _currentTimeLayer = _mesh.TimeLayers[1];
      _prevTimeLayer = _mesh.TimeLayers[0];
      _t0 = _currentTimeLayer - _prevTimeLayer;

      AssemblySLAE();
      AccountSecondConditions();
      AccountFirstConditions();
      ExcludeFictiveNodes();

      _solver.SetSLAE(_globalVector, _globalMatrix);
      _qLayers[1] = _solver.Solve();
   }

   private void AssemblySLAE()
   {
      _globalVector.Fill(0);
      _globalMatrix.Clear();

      for (int ielem = 0; ielem < _mesh.ElementsCount; ielem++)
      {
         //if (IsElementFictive(ielem))
         //   continue;

         AssemblyLocalSLAE(ielem);
         AddLocalMatrixToGlobal(ielem);
         AddLocalVectorToGlobal(ielem);

         _localStiffness.Clear();
         _localMassSigma.Clear();
         _localVector.Clear();
      }

      Array.Copy(_globalMatrix._al, _globalMatrix._au, _globalMatrix._al.Length);
   }

   private void AssemblyLocalSLAE(int ielem)
   {
      int Mu(int i) => i % 2;
      int Nu(int i) => i / 2 % 2;
      int Theta(int i) => i / 4;

      double hx = Math.Abs(_mesh.Points[_mesh.Elements[ielem][7]].X - _mesh.Points[_mesh.Elements[ielem][0]].X);
      double hy = Math.Abs(_mesh.Points[_mesh.Elements[ielem][7]].Y - _mesh.Points[_mesh.Elements[ielem][0]].Y);
      double hz = Math.Abs(_mesh.Points[_mesh.Elements[ielem][7]].Z - _mesh.Points[_mesh.Elements[ielem][0]].Z);

      double[,] matrixG =
      {
         { 1.0, -1.0 },
         { -1.0, 1.0 }
      };

      double[,] matrixM =
      {
         { 2.0 / 6.0, 1.0 / 6.0 },
         { 1.0 / 6.0, 2.0 / 6.0 }
      };

      for (int i = 0; i < NodesPerElement; i++)
      {
         for (int j = 0; j < NodesPerElement; j++)
         {
            _localStiffness[i, j] =
               matrixG[Mu(i), Mu(j)] / hx * matrixM[Nu(i), Nu(j)] * hy * matrixM[Theta(i), Theta(j)] * hz +
               matrixM[Mu(i), Mu(j)] * hx * matrixG[Nu(i), Nu(j)] / hy * matrixM[Theta(i), Theta(j)] * hz +
               matrixM[Mu(i), Mu(j)] * hx * matrixM[Nu(i), Nu(j)] * hy * matrixG[Theta(i), Theta(j)] / hz;

            _localMassSigma[i, j] = matrixM[Mu(i), Mu(j)] * hx * matrixM[Nu(i), Nu(j)] * hy * matrixM[Theta(i), Theta(j)] * hz;
         }
      }
      var _tempMass = _localMassSigma;

      _localMassSigma = Parameters.Sigma() * _localMassSigma;
      // В матрицу жёсткости запишу всю локальную А.

      _localStiffness = _prevTimeLayer != 0 ?  Parameters.Lambda() * _localStiffness + (_t + _t0) / _t / _t0 * _localMassSigma
         : Parameters.Lambda() * _localStiffness + 1 / _t0 * _localMassSigma;


      for (int i = 0; i < NodesPerElement; i++)
         _localVector[i] = Parameters.F(_mesh.Points[_mesh.Elements[ielem][i]].X, _mesh.Points[_mesh.Elements[ielem][i]].Y, _mesh.Points[_mesh.Elements[ielem][i]].Z, _currentTimeLayer);

      Vector qLocalPrevPrev = new(NodesPerElement);
      Vector qLocalPrev = new(NodesPerElement);

      for (int i = 0; i < NodesPerElement; i++)
      {
         qLocalPrevPrev[i] = _qLayers[_prevPrevTimeLayerI][_mesh.Elements[ielem][i]];

         qLocalPrev[i] = _prevTimeLayer != 0 ?_qLayers[_prevTimeLayerI][_mesh.Elements[ielem][i]]
            : _qLayers[0][_mesh.Elements[ielem][i]];
      }

      // Вектор правой части d (тоже локальный)
      _localVector = _prevTimeLayer != 0 ?
         _tempMass * _localVector
         - (_t0 / _t / _t1) * _localMassSigma * qLocalPrevPrev
         + _t / _t1 / _t0 * _localMassSigma * qLocalPrev

         : _tempMass * _localVector + 1 / _t0 * _localMassSigma * qLocalPrev;
   }

   private void AddLocalMatrixToGlobal(int ielem)
   {
      for (int i = 0; i < NodesPerElement; i++)
      {
         for (int j = 0; j < NodesPerElement; j++)
         {
            if (_mesh.Elements[ielem][i] == _mesh.Elements[ielem][j])
            {
               _globalMatrix._di[_mesh.Elements[ielem][i]] += _localStiffness[i, j];
               continue;
            }

            if (_mesh.Elements[ielem][i] > _mesh.Elements[ielem][j])
            {
               for (int icol = _globalMatrix._ia[_mesh.Elements[ielem][i]]; icol < _globalMatrix._ia[_mesh.Elements[ielem][i] + 1]; icol++)
               {
                  if (_globalMatrix._ja[icol] == _mesh.Elements[ielem][j])
                  {
                     _globalMatrix._al[icol] += _localStiffness[i, j];
                     break;
                  }
               }
            }
         }
      }
   }

   private void AddLocalVectorToGlobal(int ielem)
   {
      for (int i = 0; i < NodesPerElement; i++)
         _globalVector[_mesh.Elements[ielem][i]] += _localVector[i];
   }

   public void AccountSecondConditions()
   {
      for (int i = 0; i < _mesh.BoundaryFaces2.Count; i++)
      {
         double eps = 1e-14;
         double hx = 0, hy = 0;

         if(Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].X - _mesh.Points[_mesh.BoundaryFaces2[i][1]].X) < eps)
         {
            hx = Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].Y - _mesh.Points[_mesh.BoundaryFaces2[i][1]].Y);
            hy = Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].Z - _mesh.Points[_mesh.BoundaryFaces2[i][2]].Z);
         }
         else if (Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].Y - _mesh.Points[_mesh.BoundaryFaces2[i][2]].Y) < eps)
         {
            hx = Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].X - _mesh.Points[_mesh.BoundaryFaces2[i][1]].X);
            hy = Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].Z - _mesh.Points[_mesh.BoundaryFaces2[i][2]].Z);
         }
         else if (Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].Z - _mesh.Points[_mesh.BoundaryFaces2[i][2]].Z) < eps)
         {
            hx = Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].X - _mesh.Points[_mesh.BoundaryFaces2[i][1]].X);
            hy = Math.Abs(_mesh.Points[_mesh.BoundaryFaces2[i][0]].Y - _mesh.Points[_mesh.BoundaryFaces2[i][2]].Y);
         }

         double coeffM = hx * hy / 36;
         var mass = new Matrix(4);
         mass[0, 0] = 4.0; mass[0, 1] = 2.0; mass[0, 2] = 2.0; mass[0, 3] = 1.0;
         mass[1, 0] = 2.0; mass[1, 1] = 4.0; mass[1, 2] = 1.0; mass[1, 3] = 2.0;
         mass[2, 0] = 2.0; mass[2, 1] = 1.0; mass[2, 2] = 4.0; mass[2, 3] = 2.0;
         mass[3, 0] = 1.0; mass[3, 1] = 2.0; mass[3, 2] = 2.0; mass[3, 3] = 4.0;


         var Theta = new Vector(4);
         Theta[0] = Parameters.dU_dn(_mesh.Points[_mesh.BoundaryFaces2[i][0]].X, _mesh.Points[_mesh.BoundaryFaces2[i][0]].Y, _mesh.Points[_mesh.BoundaryFaces2[i][0]].Z, _currentTimeLayer);
         Theta[1] = Parameters.dU_dn(_mesh.Points[_mesh.BoundaryFaces2[i][1]].X, _mesh.Points[_mesh.BoundaryFaces2[i][1]].Y, _mesh.Points[_mesh.BoundaryFaces2[i][1]].Z, _currentTimeLayer);
         Theta[2] = Parameters.dU_dn(_mesh.Points[_mesh.BoundaryFaces2[i][2]].X, _mesh.Points[_mesh.BoundaryFaces2[i][2]].Y, _mesh.Points[_mesh.BoundaryFaces2[i][2]].Z, _currentTimeLayer);
         Theta[3] = Parameters.dU_dn(_mesh.Points[_mesh.BoundaryFaces2[i][3]].X, _mesh.Points[_mesh.BoundaryFaces2[i][3]].Y, _mesh.Points[_mesh.BoundaryFaces2[i][3]].Z, _currentTimeLayer);

         var localBoundaryAccount = coeffM * mass * Theta;

         for (int j = 0; j < _mesh.BoundaryFaces2[i].Length; j++)
            _globalVector[_mesh.BoundaryFaces2[i][j]] += localBoundaryAccount[j];
               
      }
   }

   public void AccountFirstConditions()
   {
      foreach (var node in _mesh.BoundaryNodes1)
      {
         int row = node;

         // На диагонали единица.
         _globalMatrix._di[row] = 1;
         
         // В векторе правой части значение краевого.
         _globalVector[row] = Parameters.U(_mesh.Points[node].X, _mesh.Points[node].Y, _mesh.Points[node].Z, _currentTimeLayer);

         // Вся остальная строка 0. 
         for (int i = _globalMatrix._ia[row]; i < _globalMatrix._ia[row + 1]; i++)
            _globalMatrix._al[i] = 0;

         for (int col = row + 1; col < _globalMatrix.Size; col++)
         {
            for (int j = _globalMatrix._ia[col]; j < _globalMatrix._ia[col + 1]; j++)
            {
               if (_globalMatrix._ja[j] == row)
               {
                  _globalMatrix._au[j] = 0;
                  break;
               }
            }
         }
      }
   }

   public void ExcludeFictiveNodes()
   {
      foreach (var node in _mesh.FictiveNodes)
      {
         int row = node;

         // На диагонали единица.
         _globalMatrix._di[row] = 1;

         // В векторе правой части 0.
         _globalVector[row] = 0;

         // Вся остальная строка 0. 
         for (int i = _globalMatrix._ia[row]; i < _globalMatrix._ia[row + 1]; i++)
            _globalMatrix._al[i] = 0;

         for (int col = row + 1; col < _globalMatrix.Size; col++)
         {
            for (int j = _globalMatrix._ia[col]; j < _globalMatrix._ia[col + 1]; j++)
            {
               if (_globalMatrix._ja[j] == row)
               {
                  _globalMatrix._au[j] = 0;
                  break;
               }
            }
         }
      }
   }

   public void PrintCurrentLayerErrorNorm()
   {
      double errorNorm = 0;
      double realSolutionNorm = 0;

      // Без узлов с первыми краевыми условиями.
      for (int i = 0; i < _qLayers[2].Size; i++)
         if (!_mesh.BoundaryNodes1.Contains(i) && !_mesh.FictiveNodes.Contains(i))
         {
            errorNorm += (_qLayers[2][i] - Parameters.U(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _currentTimeLayer))
               * (_qLayers[2][i] - Parameters.U(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _currentTimeLayer));
            realSolutionNorm += Parameters.U(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _currentTimeLayer)
               * Parameters.U(_mesh.Points[i].X, _mesh.Points[i].Y, _mesh.Points[i].Z, _currentTimeLayer);
         }

      var res = Math.Sqrt(errorNorm) / Math.Sqrt(realSolutionNorm);
      _timeLayersNorm += res * res;
      Console.WriteLine($"{res:e2}");
   }

   public void PrintTimeLayersNorm()
   {
      Console.WriteLine($"{Math.Sqrt(_timeLayersNorm):e2}");
   }
}
