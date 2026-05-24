using Novolis.Rendering.Backends.TwoD.Silk;
using RtsLiteTwoD.Game;

var game = new RtsLiteTwoDGame();
SilkTwoDGame.Run("RTS Lite TwoD", 1280, 720, game.Initialize, game.Update);
