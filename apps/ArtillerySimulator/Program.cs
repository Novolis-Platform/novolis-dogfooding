using ArtillerySimulator.Game;
using Novolis.Raylib.Game;

var game = new ArtillerySimulatorGame();
RayGame.Run("Artillery Simulator", 1280, 720, game.Initialize, game.Update);
