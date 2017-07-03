using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace wondev {

    class Point {
        public int x;
        public int y;

        public Point() { }

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    class Unit : Point {

        public Unit() { }
        public Unit(Unit originalToCopy)
        {
            x = originalToCopy.x;
            y = originalToCopy.y;
        }
        public override string ToString()
        {
            return String.Format("({0},{1})", x, y);
        }

    }

    class Board
    {
        public List<List<Unit>> Units;
        public int[,] Cells = null;
        public int Size;
        public int[] Scores;

        public static List<string> MOVES = new List<string>() { "NW", "N", "NE", "E", "SE", "S", "SW", "W" };
        private static string MOVE = "MOVE&BUILD";
        private static string PUSH = "PUSH&BUILD";

        private static int NULLINT = int.MinValue;

        public Board(Board originalToCopy)
        {
            Units = new List<List<Unit>>();
            foreach (var list in originalToCopy.Units)
            {
                Units.Add(list.Select(unitToCopy => new Unit(unitToCopy)).ToList());
            }
            Cells = originalToCopy.Cells.Clone() as int[,];
            Size = originalToCopy.Size;
            Scores = originalToCopy.Scores.Clone() as int[];
        }

        public Board()
        {
            Units = new List<List<Unit>>();
            Units.Add(new List<Unit>());
            Units.Add(new List<Unit>());
            Scores = new int[2] { 0, 0 };
        }

        public List<Unit> PlayerUnits(int player)
        {
            return Units[player];
        }
        public List<Unit> EnemyUnits(int player)
        {
            return Units[(player + 1) % 2];
        }

        public Unit UnitInPosition(int x, int y)
        {
            foreach (var list in Units)
            {
                foreach (var unit in list)
                {
                    if (unit.x == x && unit.y == y) return unit;
                }
            }
            return null;
        }
        public bool IsOccupiedByEnemy(int x, int y, int player)
        {
            return EnemyUnits(player).Exists(unit => unit.x == x && unit.y == y);
        }

        public bool WithinBounds(int x, int y)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size;
        }

        public bool IsValid(int player, string type, int index, string dir1, string dir2)
        {
            var players = PlayerUnits(player);
            var enemies = EnemyUnits(player);
            var curPlayer = players[index];

            if (type == MOVE)
            {
                var finalPosition = PositionMove(players[index].x, players[index].y, dir1);
                if (finalPosition == null) return false;
                if (!WithinBounds(finalPosition.x,finalPosition.y)) return false;
                if (Cells[finalPosition.x, finalPosition.y] < 0) return false;
                if (UnitInPosition(finalPosition.x, finalPosition.y) != null) return false;
                if (Cells[finalPosition.x, finalPosition.y] > Cells[curPlayer.x, curPlayer.y] + 1) return false;

                var buildingPosition = PositionMove(finalPosition.x, finalPosition.y, dir2);
                if (buildingPosition == null) return false;
                if (!WithinBounds(buildingPosition.x, buildingPosition.y)) return false;
                if (Cells[buildingPosition.x, buildingPosition.y] < 0) return false;
                var unitInPos = UnitInPosition(buildingPosition.x, buildingPosition.y);
                if (unitInPos!= null && unitInPos != players[index]) return false;

                return true;
            }
            else if (type == PUSH)
            {
                var dirIndex = MOVES.IndexOf(dir1);
                if (dir2 != dir1 && dir2 != MOVES[(dirIndex-1).mod(MOVES.Count)] && dir2 != MOVES[(dirIndex+1).mod(MOVES.Count)]) return false;

                var buildingPosition = PositionMove(players[index].x, players[index].y, dir1);
                if (buildingPosition == null) return false;
                var finalPosition = PositionMove(buildingPosition.x, buildingPosition.y, dir2);
                if (finalPosition == null) return false;

                if (!WithinBounds(buildingPosition.x, buildingPosition.y)) return false;
                if (!IsOccupiedByEnemy(buildingPosition.x, buildingPosition.y, player)) return false;

                if (!WithinBounds(finalPosition.x, finalPosition.y)) return false;
                if (Cells[finalPosition.x, finalPosition.y] < 0) return false;
                if (UnitInPosition(finalPosition.x, finalPosition.y)!=null) return false;
                if (Cells[finalPosition.x, finalPosition.y] > Cells[buildingPosition.x, buildingPosition.y] + 1) return false;

                return true;
            }

            return false;
        }

        public void Run(int player, string type, int index, string dir1, string dir2)
        {
            if (!IsValid(player, type, index, dir1, dir2))
            {
                throw new Exception("No debería intentar movimientos malos.");
            }
            
            if (type == MOVE)
            {
                var finalPosition = PositionMove(PlayerUnits(player)[index].x, PlayerUnits(player)[index].y, dir1);
                var buildingPosition = PositionMove(finalPosition.x, finalPosition.y, dir2);
                PlayerUnits(player)[index].x = finalPosition.x;
                PlayerUnits(player)[index].y = finalPosition.y;
                if (Cells[finalPosition.x, finalPosition.y] == 3) Scores[player] += 1;
                Cells[buildingPosition.x, buildingPosition.y] += 1;
                if (Cells[buildingPosition.x, buildingPosition.y] > 3) Cells[buildingPosition.x, buildingPosition.y] = -1;
            }
            else
            {
                var buildingPosition = PositionMove(PlayerUnits(player)[index].x, PlayerUnits(player)[index].y, dir1);
                var finalPosition = PositionMove(buildingPosition.x, buildingPosition.y, dir2);
                Cells[buildingPosition.x, buildingPosition.y] += 1;
                if (Cells[buildingPosition.x, buildingPosition.y] > 3) Cells[buildingPosition.x, buildingPosition.y] = -1;
                var pushedUnit = UnitInPosition(buildingPosition.x, buildingPosition.y);
                pushedUnit.x = finalPosition.x;
                pushedUnit.y = finalPosition.y;
            }
        }

        int currentAddRow = 0;
        public void AddRow(string row)
        {
            if (Cells == null)
            {
                Size = row.Length;
                Cells = new int[Size, Size];
            }
            for (int i = 0; i < Size; i++)
            {
                int val = row[i] == '.' ? -1 : int.Parse(row[i].ToString());
                if (val > 3) val = -1;
                Cells[i, currentAddRow] = val;
            }
            currentAddRow += 1;
        }

        public Point PositionMove(int x, int y, string movement)
        {
            if (movement.Contains("N")) y -= 1;
            if (movement.Contains("S")) y += 1;
            if (movement.Contains("W")) x -= 1;
            if (movement.Contains("E")) x += 1;
            if (x >= Size || y >= Size || x < 0 || y < 0) return null;
            return new Point() { x = x, y = y };
        }

        public List<Move> GetValidMoves(int player)
        {
            List<Move> ret = new List<Move>();
            List<string> possibleActions = new List<string>() { "MOVE&BUILD", "PUSH&BUILD" };

            foreach (var move in possibleActions)
            {
                for (int index = 0; index <= 1; index++)
                {
                    foreach (var dir1 in MOVES)
                    {
                        foreach (var dir2 in MOVES)
                        {
                            if (IsValid(player, move, index, dir1, dir2))
                            {
                                ret.Add(new Move() { playerNumber = player, index = index, atype = move, dir1 = dir1, dir2 = dir2 });
                            }
                        }
                    }
                }
            }
            return ret;

        }

        public int[,] ReachableCells(int x, int y, int turns)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            int[,] ret = new int[4, turns + 1];
            if (x == -1) return ret;

            Queue<Point> q = new Queue<Point>();
            q.Enqueue(new Point() { x = x, y = y });
            var cells = Cells.Clone() as int[,];

            foreach (var tmpUnit in Units[0].Union(Units[1])) if (tmpUnit.x != x || tmpUnit.y != y) cells[tmpUnit.x, tmpUnit.y] = -1;

            var depth = new int[cells.GetLength(0), cells.GetLength(1)];
            var donotvisit = new bool[cells.GetLength(0), cells.GetLength(1)];

            HashSet<Point> thisGeneration = new HashSet<Point>();
            thisGeneration.Add(new Point(x, y));

            int curGeneration = 1;
            while (true)
            {
                HashSet<Point> nextGeneration = new HashSet<Point>();
                HashSet<Point> grow = new HashSet<Point>();

                foreach (var cur in thisGeneration)
                {
                    ret[cells[cur.x, cur.y], curGeneration-1] += 1;
                    //Console.Error.WriteLine("{0},{1} adds to {2},{3}", cur.x, cur.y, cells[cur.x, cur.y], curGeneration - 1);
                }

                foreach (var cur in thisGeneration)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            var neighX = cur.x + dx;
                            var neighY = cur.y + dy;
                            if (!WithinBounds(neighX, neighY) || cells[neighX, neighY] < 0) continue;

                            if (curGeneration > 1 && cells[neighX, neighY] <= cells[cur.x, cur.y] && cells[neighX, neighY] < 3 && !grow.Any(p => p.x == neighX && p.y == neighY))
                            {
                                grow.Add(new Point(neighX, neighY));
                            }
                            if (cells[neighX, neighY] <= cells[cur.x, cur.y]+1)
                            {
                                if (!nextGeneration.Any(p=>p.x==neighX && p.y==neighY)) {
                                    nextGeneration.Add(new Point(neighX, neighY));
                                }
                            }
                        }
                    }
                }
                curGeneration += 1;
                if (curGeneration > turns+1) break;
                foreach (var p in grow) { cells[p.x, p.y] += 1; }
                thisGeneration = nextGeneration;
                if (thisGeneration.Count == 0) break;
            }
           // Console.Error.WriteLine("STOPWATCH {0}", s.ElapsedMilliseconds);
            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int y=0; y < Size; y++)
            {
                for (int x=0; x< Size; x++)
                {
                    sb.Append(Cells[x, y] == -1 ? "." : Cells[x, y].ToString());
                }
                sb.Append("\n");
            }
            sb.Append(Units[0][0].ToString());
            sb.Append(",");
            sb.Append(Units[0][1].ToString());
            sb.Append(" vs ");
            if (Units[1].Count>0) sb.Append(Units[1][0].ToString());
            sb.Append(",");
            if (Units[1].Count > 1) sb.Append(Units[1][1].ToString());
            return sb.ToString();
        }

    }

    static class IO
    {
        public const bool VERBOSE = false;
        public const bool DUMP_READ = false;
        public const bool SIMULATE_READ = false;

        public static void Verbose(string format, params object[] param)
        {
            if (!VERBOSE) return;
            Console.Error.WriteLine(format, param);
        }

        public static void VerboseShort(string format, params object[] param)
        {
            if (!VERBOSE) return;
            Console.Error.Write(format, param);
        }

        public static string Read()
        {
            if (SIMULATE_READ) return FakeRead();
            var str = Console.ReadLine();
            if (DUMP_READ) Console.Error.WriteLine(str);
            return str;
        }

        static int readlinecounter = -1;
        public static string FakeRead()
        {
            readlinecounter += 1;
            var strs = new[] { "" };
            return strs[readlinecounter];
        }
    }

    public class Move
    {
        public int playerNumber;
        public int index;
        public string dir1;
        public string dir2;
        public string atype;

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", atype, index, dir1, dir2);
        }
    }

    abstract class Bot
    {
        public int playerNumber;
        public Bot(int playerNumber)
        {
            this.playerNumber = playerNumber;
        }
        public abstract Move Run(Board boardStatus);
    }

    class SimulationBot : Bot
    {
        public SimulationBot(int playerNumber) : base(playerNumber) { }

        public override Move Run(Board boardStatus)
        {
            var moves = boardStatus.GetValidMoves(this.playerNumber);
            if (moves.Count == 0) return null;

            int bestScore = -1;
            Move best = null;

            foreach (var move in moves)
            {
                Board tmpBoard = new Board(boardStatus);
                tmpBoard.Run(move.playerNumber, move.atype, move.index, move.dir1, move.dir2);
                int score = ScoreState(tmpBoard, this.playerNumber);
                IO.Verbose("BOT #{2}. Move {0} - {1}", move.ToString(), score, playerNumber);
                if (best == null || score > bestScore)
                {
                    best = move;
                    bestScore = score;
                }
            }

            return best;
        }


        public int ScoreState(Board board, int player)
        {
            // Evaluation function hidden.
        }

    }

    class WondevWoman
    {

        static void Main(string[] args)
        {
            string[] inputs;
            int size = int.Parse(IO.Read());
            int unitsPerPlayer = int.Parse(IO.Read());

            SimulationBot myBot = new SimulationBot(0);

            Stopwatch sw = new Stopwatch();
            while (true)
            {
                sw.Restart();

                Board board = new Board();

                for (int i = 0; i < size; i++)
                {
                    string row = IO.Read();
                    board.AddRow(row);
                }
                for (int i = 0; i < unitsPerPlayer; i++)
                {
                    inputs = IO.Read().Split(' ');
                    int unitX = int.Parse(inputs[0]);
                    int unitY = int.Parse(inputs[1]);

                    board.Units[0].Add(new Unit() { x = unitX, y = unitY });
                }
                for (int i = 0; i < unitsPerPlayer; i++)
                {
                    inputs = IO.Read().Split(' ');
                    int otherX = int.Parse(inputs[0]);
                    int otherY = int.Parse(inputs[1]);

                    if (otherX>-1) board.Units[1].Add(new Unit() { x = otherX, y = otherY });
                }

                int legalActions = int.Parse(IO.Read());
                for (int i = 0; i < legalActions; i++)
                {
                    string fullInput = IO.Read();
                }

                //for (int i = 0; i<2; i++) IO.Verbose("Reachable cells from {1}:\n{0}", MatrixPrinter(board.ReachableCells(board.PlayerUnits(0)[i].x, board.PlayerUnits(0)[i].y, 2), ","), board.PlayerUnits(0)[i].ToString());

                var move = myBot.Run(board);
                board.Run(move.playerNumber,move.atype,move.index,move.dir1,move.dir2);

                Console.Error.WriteLine("Deltatime: {0}", sw.ElapsedMilliseconds);
                Console.WriteLine(move);
                
            }
        }

        public static string MatrixPrinter<T>(T[,] matrix, string sep)
        {
            StringBuilder sb = new StringBuilder();
            for (int d1 = 0; d1 < matrix.GetLength(1); d1++)
            {
                for (int d0 = 0; d0 < matrix.GetLength(0); d0++)
                {
                    sb.Append(matrix[d0, d1].ToString());
                    sb.Append(sep);
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

    }

    public static class Extensions
    {
        public static int mod(this int x,int m)
        {
            return (x % m + m) % m;
        }
    }
}