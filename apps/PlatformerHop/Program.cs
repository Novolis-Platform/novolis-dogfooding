using Novolis.Raylib.Game;
using PlatformerHop.Game;

var game = new PlatformerHopGame();
RayGame.Run("Platformer Hop", 960, 540, game.Initialize, game.Update);
