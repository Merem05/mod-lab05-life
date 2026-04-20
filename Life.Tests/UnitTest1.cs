using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace cli_life.Tests
{
    public class LifeTests
    {

        [Fact]
        public void Test_Cell_InitialState_IsDead()
        {
            var cell = new Cell();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void Test_Cell_Advance_ChangesState()
        {
            var cell = new Cell();
            cell.IsAlive = true;
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }


        [Fact]
        public void Test_Board_Creation_CorrectDimensions()
        {
            var board = new Board(50, 20, 1, 0.5);
            Assert.Equal(50, board.Columns);
            Assert.Equal(20, board.Rows);
            Assert.Equal(0, board.Generation);
        }

        [Fact]
        public void Test_Board_SetCell_WorksCorrectly()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(10, 10, true);
            Assert.True(board.GetCell(10, 10));
            board.SetCell(10, 10, false);
            Assert.False(board.GetCell(10, 10));
        }

        [Fact]
        public void Test_Board_GetAliveCount_ReturnsCorrectCount()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(6, 6, true);
            board.SetCell(7, 7, true);
            Assert.Equal(3, board.GetAliveCount());
        }

        [Fact]
        public void Test_Board_Advance_IncreasesGeneration()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(6, 5, true);
            board.SetCell(6, 6, true);

            int initialGen = board.Generation;
            board.Advance();
            Assert.Equal(initialGen + 1, board.Generation);
        }

        [Fact]
        public void Test_Board_Rule_Block_Stable()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(6, 5, true);
            board.SetCell(6, 6, true);

            int aliveCount = board.GetAliveCount();
            for (int i = 0; i < 10; i++)
            {
                board.Advance();
                Assert.Equal(aliveCount, board.GetAliveCount());
            }
        }

        [Fact]
        public void Test_Board_Rule_Blinker_Periodic()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 4, true);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);

            board.Advance();
            Assert.True(board.GetCell(4, 5));
            Assert.True(board.GetCell(5, 5));
            Assert.True(board.GetCell(6, 5));
            Assert.False(board.GetCell(5, 4));
            Assert.False(board.GetCell(5, 6));

            board.Advance();
            Assert.True(board.GetCell(5, 4));
            Assert.True(board.GetCell(5, 5));
            Assert.True(board.GetCell(5, 6));
        }

        [Fact]
        public void Test_Board_Rule_Glider_Moves()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(6, 6, true);
            board.SetCell(7, 5, true);
            board.SetCell(7, 6, true);
            board.SetCell(7, 7, true);

            Assert.Equal(5, board.GetAliveCount());
            board.Advance();
            Assert.True(board.GetAliveCount() > 0);
        }

        [Fact]
        public void Test_Board_CheckStable_DetectsStability()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(6, 5, true);
            board.SetCell(6, 6, true);

            int aliveCount = board.GetAliveCount();
            for (int i = 0; i < 5; i++)
            {
                board.Advance();
                Assert.Equal(aliveCount, board.GetAliveCount());
            }
        }

        [Fact]
        public void Test_Board_SaveAndLoad_PreservesState()
        {
            var original = new Board(10, 10, 1, 0);
            original.SetCell(2, 3, true);
            original.SetCell(4, 5, true);
            original.SetCell(7, 8, true);

            string filename = "test_save.txt";

            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int row = 0; row < original.Rows; row++)
                {
                    for (int col = 0; col < original.Columns; col++)
                        writer.Write(original.GetCell(col, row) ? '*' : ' ');
                    writer.WriteLine();
                }
            }

            var loaded = new Board(10, 10, 1, 0);
            using (StreamReader reader = new StreamReader(filename))
            {
                for (int row = 0; row < loaded.Rows; row++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    for (int col = 0; col < loaded.Columns && col < line.Length; col++)
                        loaded.SetCell(col, row, line[col] == '*');
                }
            }

            Assert.Equal(original.GetAliveCount(), loaded.GetAliveCount());
            Assert.True(loaded.GetCell(2, 3));
            Assert.True(loaded.GetCell(4, 5));
            Assert.True(loaded.GetCell(7, 8));

            if (File.Exists(filename))
                File.Delete(filename);
        }

        [Fact]
        public void Test_Board_LoadFigure_LoadsCorrectly()
        {
            string filename = "test_figure.txt";
            File.WriteAllText(filename, "**\n**");

            var board = new Board(50, 20, 1, 0);
            board.LoadFigure(filename);

            Assert.Equal(4, board.GetAliveCount());

            if (File.Exists(filename))
                File.Delete(filename);
        }


        [Fact]
        public void Test_Settings_SaveAndLoad_PreservesValues()
        {
            var settings = new Settings
            {
                Width = 100,
                Height = 50,
                CellSize = 2,
                DelayMs = 200,
                LiveDensity = 0.7,
                MaxGenerations = 1000
            };

            string filename = "test_settings.json";
            settings.Save(filename);
            var loaded = Settings.Load(filename);

            Assert.Equal(settings.Width, loaded.Width);
            Assert.Equal(settings.Height, loaded.Height);
            Assert.Equal(settings.CellSize, loaded.CellSize);
            Assert.Equal(settings.DelayMs, loaded.DelayMs);
            Assert.Equal(settings.LiveDensity, loaded.LiveDensity);
            Assert.Equal(settings.MaxGenerations, loaded.MaxGenerations);

            if (File.Exists(filename))
                File.Delete(filename);
        }

        [Fact]
        public void Test_Settings_Load_ReturnsDefaultWhenFileNotFound()
        {
            var settings = Settings.Load("nonexistent.json");
            Assert.Equal(50, settings.Width);
            Assert.Equal(20, settings.Height);
            Assert.Equal(1, settings.CellSize);
        }


        [Fact]
        public void Test_Block_CreatedCorrectly()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(10, 10, true);
            board.SetCell(11, 10, true);
            board.SetCell(10, 11, true);
            board.SetCell(11, 11, true);

            Assert.Equal(4, board.GetAliveCount());
            Assert.True(board.GetCell(10, 10));
            Assert.True(board.GetCell(11, 10));
            Assert.True(board.GetCell(10, 11));
            Assert.True(board.GetCell(11, 11));
        }

        [Fact]
        public void Test_Beehive_CreatedCorrectly()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(11, 10, true);
            board.SetCell(12, 10, true);
            board.SetCell(10, 11, true);
            board.SetCell(13, 11, true);
            board.SetCell(11, 12, true);
            board.SetCell(12, 12, true);

            Assert.Equal(6, board.GetAliveCount());
        }

        [Fact]
        public void Test_Boat_CreatedCorrectly()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(6, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(7, 6, true);
            board.SetCell(6, 7, true);

            Assert.Equal(5, board.GetAliveCount());
        }

        [Fact]
        public void Test_SingleCell_Dies()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(10, 10, true);

            board.Advance();

            Assert.False(board.GetCell(10, 10));
            Assert.Equal(0, board.GetAliveCount());
        }


        [Fact]
        public void Test_Graph_CountAlive_ReturnsDictionary()
        {
            var result = Graph.CountAlive(0.3, 50, 20);
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }


        [Fact]
        public void Test_Board_Generation_IncreasesOnEachAdvance()
        {
            var board = new Board(50, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(6, 5, true);
            board.SetCell(6, 6, true);

            for (int i = 1; i <= 5; i++)
            {
                board.Advance();
                Assert.Equal(i, board.Generation);
            }
        }

        [Fact]
        public void Test_Board_EdgeCells_HaveCorrectNeighbors()
        {
            var board = new Board(10, 10, 1, 0);
            var cell = board.Cells[0, 0];
            Assert.Equal(8, cell.neighbors.Count);
        }

        [Fact]
        public void Test_Board_Randomize_CreatesLiveCells()
        {
            var board = new Board(50, 20, 1, 0.5);
            int aliveCount = board.GetAliveCount();
            Assert.True(aliveCount > 0);
            Assert.True(aliveCount < 50 * 20);
        }

        [Fact]
        public void Test_FindPattern_Block_DetectsCorrectly()
        {
            var board = new Board(20, 20, 1, 0);
            board.SetCell(5, 5, true);
            board.SetCell(6, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(6, 6, true);

            var pattern = new Pattern("Блок", "****", 2);
            board.FindPattern(pattern);

            Assert.Equal(1, pattern.count);
        }

        [Fact]
        public void Test_FindPattern_Beehive_DetectsCorrectly()
        {
            var board = new Board(20, 20, 1, 0);
            board.SetCell(6, 5, true);
            board.SetCell(7, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(8, 6, true);
            board.SetCell(6, 7, true);
            board.SetCell(7, 7, true);

            var pattern = new Pattern("Улей", ".**.*..*.**.....", 4);
            board.FindPattern(pattern);

            Assert.Equal(1, pattern.count);
        }
    }
}
