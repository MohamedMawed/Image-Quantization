using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace ImageQuantization
{
    
    class Edge : IComparable<Edge>
    {
        public int from, to;
        public double weight;
        public Edge(int from, int to, double weight)
        {
            this.from = from;
            this.to = to;
            this.weight = weight;
        }

        public int CompareTo(Edge other)
        {
            if (weight == other.weight) return 0;
            else if (weight > other.weight) return -1;
            else return 1;
        }
     
    }

    class Graph
    {
        public long vertices, edges;
        public Edge[] MST;
        public List<int>[] AdjList;
        public List<RGBPixel> Distinct;
        public RGBPixelD[] Colors;
        public int[] Colors_count;
        public int[] ArrVisited;
        public int cluster = 1;
        private int[] componentRoot;
        private int[] subtreeSize;
        private List<int>[] edgeList;
        private bool[] deleted;

        public Graph()
        {
            vertices = 0;
            edges = 0;
            MST = new Edge[0];
            AdjList = new List<int>[0];
            ArrVisited = new int[0];
            Distinct = new List<RGBPixel>(0);

        }
        private bool WeightEQUALzero(Edge e)
        {
            return e.weight == 0;
        }
        private void BFS(int node)
        {
            ArrVisited[node] = cluster;
            Queue<int> Neighbor = new Queue<int>();
            Neighbor.Enqueue(node);
            while (Neighbor.Count > 0)
            {
                int NextNode = Neighbor.Dequeue();
                Colors[cluster].red += Distinct[NextNode].red;
                Colors[cluster].green += Distinct[NextNode].green;
                Colors[cluster].blue += Distinct[NextNode].blue;
                Colors_count[cluster]++;
                for (int i = 0; i < AdjList[NextNode].Count; i++)
                {
                    if (ArrVisited[AdjList[NextNode][i]] == 0)
                    {
                        ArrVisited[AdjList[NextNode][i]] = cluster;
                        Neighbor.Enqueue(AdjList[NextNode][i]);
                    }
                }
            }
            cluster++;
        }
        private void dfs(int node, int parent,int root)
        {
            subtreeSize[node] = 1;
            componentRoot[node] = root;
            for(int i = 0;i < AdjList[node].Count;i++)
            {
                int toNode = AdjList[node][i];
                if (toNode == parent)
                    continue;
                if (deleted[edgeList[node][i]])
                    continue;
                dfs(toNode, node, root);
                subtreeSize[node] += subtreeSize[toNode];
            }
        }
        public void Clustering(int k, ref RGBPixel[,] ImageMatrix)
        {

            int[,,] Histogram = new int[256, 256, 256];

            
            ArrVisited = new int[Distinct.Count];

            componentRoot = new int[Distinct.Count];
            subtreeSize = new int[Distinct.Count];
            edgeList = new List<int>[Distinct.Count];
            
            AdjList = new List<int>[Distinct.Count];
            for (int i = 0; i < Distinct.Count; i++)
            {
                Histogram[Distinct[i].red, Distinct[i].green, Distinct[i].blue] = i;
                AdjList[i] = new List<int>(0);
                edgeList[i] = new List<int>(0);
            }
            for (int i = 0; i < MST.Length; i++)
            {
                
                AdjList[MST[i].from].Add(MST[i].to);
                AdjList[MST[i].to].Add(MST[i].from);
                edgeList[MST[i].from].Add(i);
                edgeList[MST[i].to].Add(i);
            }
            {
                Array.Sort(MST, delegate (Edge e1, Edge e2)
                 {
                     return e1.CompareTo(e2);

                 });

                Edge[] MST_ = new Edge[MST.Length - k + 1];
                deleted = new bool[MST.Length];
                for(int i = 0,current = 0;i < k-1;i++)
                {
                    while (deleted[current])
                        current++;
                    int next = current;
                    while (deleted[next])
                        next++;
                    if (MST[current].weight > MST[next].weight)
                    {
                        deleted[current] = true;
                        continue;
                    }
                    subtreeSize = new int[Distinct.Count];
                    componentRoot = new int[Distinct.Count];
                    for (int node = 0;node < Distinct.Count;node++)
                    {
                        if(subtreeSize[node] == 0)
                        {
                            dfs(node, -1, node);
                        }
                    }
                    int maximumDiff = int.MinValue;
                    int bestEdge = current;
                    for(int edge = current; edge < MST.Length && MST[edge].weight == MST[current].weight;edge++)
                    {
                        if (deleted[edge])
                            continue;
                        int currentDiff = 0;
                        if(subtreeSize[MST[edge].from] < subtreeSize[MST[edge].to])
                        {
                            currentDiff = Math.Min(subtreeSize[componentRoot[MST[edge].from]] - subtreeSize[MST[edge].from], subtreeSize[MST[edge].from]);
                        }else
                        {
                            currentDiff = Math.Min(subtreeSize[componentRoot[MST[edge].to]] - subtreeSize[MST[edge].to], subtreeSize[MST[edge].to]);
                        }
                        if(currentDiff >= maximumDiff)
                        {
                            bestEdge = edge;
                            maximumDiff = currentDiff;
                        }
                    }
                    deleted[bestEdge] = true;
                }
                int j = 0;
                for (int i = 0; i < MST.Length; i++)
                {
                    if (deleted[i])
                        continue;
                    MST_[j] = MST[i];
                    j++;
                }
                MST = MST_;
            }

            Colors = new RGBPixelD[k + 1];
            Colors_count = new int[k + 1];

            AdjList = new List<int>[Distinct.Count];
            for (int i = 0; i < Distinct.Count; i++)
            {
                AdjList[i] = new List<int>(0);
            }
            for (int i = 0; i < MST.Length; i++)
            {
                AdjList[MST[i].from].Add(MST[i].to);
                AdjList[MST[i].to].Add(MST[i].from);
            }
            MST = new Edge[0];
            for (int i = 0; i < Distinct.Count; i++)
            {
                if (ArrVisited[i] == 0)
                {
                    BFS(i);

                }
            }

            for (int i = 1; i < cluster; i++)
            {
                Colors[i].red /= Colors_count[i];
                Colors[i].green /= Colors_count[i];
                Colors[i].blue /= Colors_count[i];
            }

           
            
            for (int i = 0; i < ImageMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    RGBPixel NewColor = new RGBPixel();
                    NewColor.red = Convert.ToByte(Colors[ArrVisited[Histogram[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue]]].red);
                    NewColor.green = Convert.ToByte(Colors[ArrVisited[Histogram[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue]]].green);
                    NewColor.blue = Convert.ToByte(Colors[ArrVisited[Histogram[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue]]].blue);
                    ImageMatrix[i, j] = NewColor;

                }
            }

           // MessageBox.Show(DateTime.Now.ToLongTimeString());

        }
        public void Costruct_MST(RGBPixel[,] ImageMatrix)
        {
            
            Find_Distinct_colors(ImageMatrix);
            int current_Node = 0;
            int n = Distinct.Count;
            int[] Prev = new int[n];
            int mini;
            double mind;
            bool[,,] Visited = new bool[256, 256, 256];


            double[] Distance = new double[n];

            double minCost = 0;
            MST = new Edge[Distinct.Count - 1];
            int MST_index = 0;
            for (int k = 0; k < n - 1; k++)
            {
                Visited[Distinct[current_Node].red, Distinct[current_Node].green, Distinct[current_Node].blue] = true;
                mind = 9999999999999999999;
                mini = -1;
                for (int i = 0; i < n; i++)
                {
                    if (k == 0) Distance[i] = 9999999999999999999;
                    if (current_Node == i) continue;
                    if (Visited[Distinct[i].red, Distinct[i].green, Distinct[i].blue]) continue;
                    double Weight = Convert.ToDouble((Distinct[current_Node].red - Distinct[i].red) * (Distinct[current_Node].red - Distinct[i].red)
                             + (Distinct[current_Node].green - Distinct[i].green) * (Distinct[current_Node].green - Distinct[i].green)
                             + (Distinct[current_Node].blue - Distinct[i].blue) * (Distinct[current_Node].blue - Distinct[i].blue));
                    if (Weight < Distance[i])
                    {
                        Distance[i] = Weight;
                        Prev[i] = current_Node;
                    }
                    if (Distance[i] < mind)
                    {
                        mind = Distance[i];
                        mini = i;
                    }
                }
                MST[MST_index] = new Edge(Prev[mini], mini, Math.Sqrt(Distance[mini]));
                MST_index++;
                minCost += MST[MST_index - 1].weight;
                current_Node = mini;


            }
           
          //  MessageBox.Show(minCost.ToString());

        }

        public void Find_Distinct_colors(RGBPixel[,] ImageMatrix)
        {
            
            bool[,,] Histogram = new bool[256, 256, 256];

            for (int i = 0; i < ImageMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    if (!Histogram[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue]) Distinct.Add(ImageMatrix[i, j]);
                    Histogram[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue] = true;

                }

            }
            MessageBox.Show(Distinct.Count.ToString());
           
        }
    }


}
