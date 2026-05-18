using Novolis.Raylib.Game;
using RandoriFight.Game;

var game = new RandoriFightGame();
RayGame.Run("Randori Fight", 1280, 720, game.Initialize, game.Update);
