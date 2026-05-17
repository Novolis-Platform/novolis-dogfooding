using Novolis.Raylib.Game;
using RtsLite.Game;

var game = new RtsLiteGame();
RayGame.Run("RTS Lite", 1280, 720, game.Initialize, game.Update);
